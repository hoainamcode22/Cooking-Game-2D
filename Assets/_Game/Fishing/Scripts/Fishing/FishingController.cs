using System;
using System.Globalization;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Điều phối một lượt câu của người chơi cục bộ — trên object "FishingController" trong SCN_Fishing.
    /// Nối: FishingStateMachine (nhịp pha) · FishingCatchResolver (random → cá) · FishingGearState (độ bền cần) · FishBasket (giỏ)
    /// · FishingBobber/FishingRodVisual/FishingSplashPool/FishingAudio (hình + tiếng) · FishingPlayerController.Local (khoá đi, báo pha).
    /// Mọi số gameplay đọc từ FishingDatabase.ConfigOrDefault và RodData. Mọi event bắn đúng 1 lần mỗi lượt.
    /// </summary>
    public class FishingController : MonoBehaviour
    {
        private const string BobberChildName = "Bobber";
        private const string RodVisualChildName = "RodVisual";
        // Phao phải rơi hẳn vào nước, không nằm đúng mép: đẩy thêm khoảng này (unit) từ mép vào — số hình ảnh, không phải gameplay.
        private const float WaterInset = 0.4f;

        public static FishingController Local { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Local = null; }

        // Cần vừa về 0 độ bền ở lượt quăng này → báo hỏng khi về Idle.
        private bool _rodBrokeThisCast;

        private FishingStateMachine _sm;
        private FishingConfig _cfg;
        private FishingBobber _bobber;
        private FishingRodVisual _rodVisual;

        // Trạng thái lượt hiện tại
        private RodData _activeRod;
        private FishData _pendingFish;
        private float _pendingKg;
        private string _pendingBasketReason;
        private int _pendingExp;
        private bool _movementLocked;

        // Kết quả kiểm CanCast lần cuối (cache mỗi frame)
        private FishingZone _nearZone;
        private Vector2 _nearPoint;
        private float _nearDist;

        public FishingPhase Phase { get { return _sm != null ? _sm.Phase : FishingPhase.Idle; } }
        public bool CanCast { get; private set; }
        public string BlockReasonVi { get; private set; } = string.Empty;
        public float BiteWindowRemaining01 { get { return _sm != null ? _sm.BiteWindowRemaining01 : 0f; } }

        public event Action<FishingPhase> OnPhaseChanged;
        public event Action<FishData, float> OnCatch;
        public event Action OnEscape;
        public event Action OnNothing;
        public event Action OnRodBroken;
        public event Action<string> OnBlocked;

        private void Awake()
        {
            Local = this;
            _cfg = FishingDatabase.ConfigOrDefault;
            _sm = new FishingStateMachine(_cfg.castDurationSeconds, _cfg.biteWaitSecondsRange.x, _cfg.biteWaitSecondsRange.y,
                                          _cfg.biteWindowSeconds, _cfg.reelDurationSeconds, _cfg.resultShowSeconds);
            _sm.OnPhaseChanged += HandlePhaseChanged;
            EnsureVisuals();
        }

        private void OnDestroy()
        {
            if (_sm != null) { _sm.OnPhaseChanged -= HandlePhaseChanged; }
            SetMovementLocked(false);
            if (Local == this) { Local = null; }
        }

        private void Update()
        {
            if (FishingPlayerController.Local == null) { return; }
            _sm.Tick(Time.deltaTime);
            RefreshCanCast();
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Bấm QUĂNG. Không đủ điều kiện → OnBlocked(BlockReasonVi) + tiếng fail, không đổi pha.</summary>
        public void Cast()
        {
            RefreshCanCast();
            if (!CanCast)
            {
                OnBlocked?.Invoke(BlockReasonVi);
                FishingAudio.PlayFail();
                return;
            }

            FishingGearState gear = FishingGearState.Instance;
            RodData rod = gear.EquippedRod;
            // [Lead chốt 07/09] Lần quăng cuối của cần VẪN được câu: trừ độ bền ngay lúc quăng, nhưng nếu về 0 thì
            // nhớ cờ và chỉ báo "Cần đã hỏng" khi lượt này kết thúc (về Idle). Người chơi không mất lượt đã trả độ bền.
            _rodBrokeThisCast = !gear.ConsumeCast();

            _activeRod = rod;
            _pendingFish = null;
            _pendingKg = 0f;
            _pendingBasketReason = null;
            _pendingExp = 0;
            _sm.SetRodModifiers(rod.biteWaitMultiplier, rod.biteWindowBonusSeconds);

            Vector2 target = ComputeBobberTarget();
            if (!_sm.TryCast(UnityEngine.Random.value)) { return; }

            SetMovementLocked(true);
            FishingPlayerController p = FishingPlayerController.Local;
            Vector2 from = p != null && p.HandAnchor != null ? (Vector2)p.HandAnchor.position : (p != null ? p.Position : target);
            if (_bobber != null) { _bobber.FlyTo(from, target, _cfg.castDurationSeconds); }
            if (_rodVisual != null) { _rodVisual.SetVisible(true); }
            FishingAudio.PlayCast();
            Debug.Log(FishingIds.LogTag + " Quăng: cần=" + rod.itemID + " chờ=" + _sm.BiteWaitTarget.ToString("0.0", CultureInfo.InvariantCulture) + "s còn " + gear.EquippedDurabilityLeft + " lần");
        }

        /// <summary>Bấm THU. Waiting → thu không (OnNothing ở Result); Bite → roll bắt; pha khác bỏ qua.</summary>
        public void Reel()
        {
            if (_sm == null) { return; }
            switch (_sm.Phase)
            {
                case FishingPhase.Waiting:
                    _sm.TryReel();   // → Result(Nothing); event bắn ở HandlePhaseChanged
                    break;

                case FishingPhase.Bite:
                    ResolveBiteReel();
                    break;

                default:
                    break;
            }
        }

        /// <summary>Tạo con "Bobber" (FishingBobber) + "RodVisual" (FishingRodVisual) nếu thiếu. Dùng được cả trong tool Editor.</summary>
        public void EnsureVisuals()
        {
            Transform bt = transform.Find(BobberChildName);
            GameObject bgo = bt != null ? bt.gameObject : new GameObject(BobberChildName);
            bgo.transform.SetParent(transform, false);
            _bobber = bgo.GetComponent<FishingBobber>();
            if (_bobber == null) { _bobber = bgo.AddComponent<FishingBobber>(); }

            Transform rt = transform.Find(RodVisualChildName);
            GameObject rgo = rt != null ? rt.gameObject : new GameObject(RodVisualChildName);
            rgo.transform.SetParent(transform, false);
            _rodVisual = rgo.GetComponent<FishingRodVisual>();
            if (_rodVisual == null) { _rodVisual = rgo.AddComponent<FishingRodVisual>(); }
            _rodVisual.Bind(_bobber);
        }

        // ── Nội bộ ──────────────────────────────────────────────────────────────

        private void ResolveBiteReel()
        {
            RodData rod = _activeRod;
            float bonus = rod != null ? rod.catchChanceBonus : 0f;
            bool caught = FishingCatchResolver.RollCatch(_cfg.baseCatchChance, bonus, UnityEngine.Random.value);

            FishData fish = null;
            if (caught)
            {
                FishingDatabase db = FishingDatabase.Instance;
                int tier = rod != null ? rod.tier : 1;
                float rareMult = rod != null ? rod.rareWeightMultiplier : 1f;
                int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
                fish = db != null ? FishingCatchResolver.PickFish(db.fishes, tier, level, rareMult, UnityEngine.Random.value) : null;
                if (fish == null)
                {
                    // Không có loài hợp lệ với cần/cấp này → coi như thoát, không trao cá ma.
                    caught = false;
                    Debug.Log(FishingIds.LogTag + " PickFish trả null (tier " + tier + ", cấp " + level + ") → coi như cá thoát.");
                }
            }

            _sm.SetReelOutcome(caught);
            _sm.TryReel();   // → Reeling

            if (!caught) { return; }

            _pendingFish = fish;
            _pendingKg = FishingCatchResolver.RollWeightKg(fish, UnityEngine.Random.value);
            // Dữ liệu (giỏ, EXP) đổi ngay lúc THU; event cho HUD bắn ở Result để khớp animation.
            string reason;
            if (!FishBasket.Instance.TryAdd(fish.fishId, 1, out reason))
            {
                _pendingBasketReason = "Giỏ đầy, cá được thả — " + reason;
            }
            if (_cfg.expPerCatch > 0 && PlayerProgressManager.Instance != null)
            {
                PlayerProgressManager.Instance.AddExp(_cfg.expPerCatch);
                _pendingExp = _cfg.expPerCatch;
            }
        }

        private void HandlePhaseChanged(FishingPhase prev, FishingPhase next)
        {
            FishingPlayerController p = FishingPlayerController.Local;
            if (p != null) { p.SetFishingPhase(next); }

            switch (next)
            {
                case FishingPhase.Waiting:
                    // Phao vừa chạm nước (thời gian bay = castDuration).
                    if (_bobber != null)
                    {
                        _bobber.Bob(false);
                        FishingSplashPool.Shared.Play(_bobber.WaterPoint, _bobber.Renderer, 1f);
                    }
                    FishingAudio.PlaySplash();
                    break;

                case FishingPhase.Bite:
                    if (_bobber != null) { _bobber.Bob(true); }
                    FishingAudio.PlayBite();
                    VibrateIfAllowed();
                    break;

                case FishingPhase.Reeling:
                    FishingAudio.PlayReel();
                    if (_bobber != null)
                    {
                        FishingSplashPool.Shared.Play(_bobber.WaterPoint, _bobber.Renderer, 1.3f);
                        _bobber.Hide();
                    }
                    break;

                case FishingPhase.Result:
                    if (prev == FishingPhase.Waiting && _bobber != null)
                    {
                        // Thu sớm: Waiting → Result thẳng, chưa có splash thu.
                        FishingSplashPool.Shared.Play(_bobber.WaterPoint, _bobber.Renderer, 0.8f);
                        FishingAudio.PlayReel();
                    }
                    if (_bobber != null) { _bobber.Hide(); }
                    FireOutcomeEvents();
                    break;

                case FishingPhase.Idle:
                    if (_rodVisual != null) { _rodVisual.SetVisible(false); }
                    if (_bobber != null) { _bobber.Hide(); }
                    SetMovementLocked(false);
                    _activeRod = null;
                    _pendingFish = null;
                    if (_rodBrokeThisCast)
                    {
                        _rodBrokeThisCast = false;
                        BlockReasonVi = "Cần đã hỏng";
                        CanCast = false;
                        OnRodBroken?.Invoke();
                        OnBlocked?.Invoke(BlockReasonVi);
                        FishingAudio.PlayFail();
                        Debug.Log(FishingIds.LogTag + " Cần đã hỏng sau lượt quăng cuối.");
                    }
                    break;
            }

            OnPhaseChanged?.Invoke(next);
        }

        /// <summary>Bắn đúng 1 event kết quả khi vào Result, theo LastOutcome của máy trạng thái.</summary>
        private void FireOutcomeEvents()
        {
            switch (_sm.LastOutcome)
            {
                case FishingOutcome.Caught:
                    FishingAudio.PlayCatch();
                    Debug.Log(FishingIds.LogTag + " Bắt được " + (_pendingFish != null ? _pendingFish.fishId : "?") + " " + _pendingKg.ToString("0.00", CultureInfo.InvariantCulture) + "kg" + (_pendingExp > 0 ? " +" + _pendingExp + " EXP" : ""));
                    OnCatch?.Invoke(_pendingFish, _pendingKg);
                    if (!string.IsNullOrEmpty(_pendingBasketReason)) { OnBlocked?.Invoke(_pendingBasketReason); }
                    break;
                case FishingOutcome.Escaped:
                    FishingAudio.PlayFail();
                    OnEscape?.Invoke();
                    break;
                case FishingOutcome.Nothing:
                    OnNothing?.Invoke();
                    break;
                default:
                    break;
            }
            _pendingBasketReason = null;
            _pendingExp = 0;
        }

        private void RefreshCanCast()
        {
            FishingPlayerController p = FishingPlayerController.Local;
            if (p == null) { CanCast = false; BlockReasonVi = string.Empty; return; }

            if (Phase != FishingPhase.Idle) { CanCast = false; BlockReasonVi = "Đang câu"; return; }

            FishingGearState gear = FishingGearState.Instance;
            if (!gear.HasUsableRod)
            {
                CanCast = false;
                BlockReasonVi = gear.Owned.Count == 0 ? "Chưa có cần câu, mua ở Quầy Cá" : "Cần đã hỏng";
                return;
            }

            if (!FishingZone.TryGetNearest(p.Position, out _nearZone, out _nearPoint, out _nearDist) || _nearDist > _cfg.castRangeFromZone)
            {
                CanCast = false;
                BlockReasonVi = "Đến gần mép nước để quăng";
                return;
            }

            CanCast = true;
            BlockReasonVi = string.Empty;
        }

        /// <summary>
        /// Điểm phao rơi: từ người theo hướng ra mép nước gần nhất, xa ít nhất cfg.castDistance và lọt hẳn vào nước (WaterInset).
        /// Không lọt → mép + inset; vẫn không → điểm ngẫu nhiên trong vùng.
        /// </summary>
        private Vector2 ComputeBobberTarget()
        {
            FishingPlayerController p = FishingPlayerController.Local;
            Vector2 pos = p != null ? p.Position : (Vector2)transform.position;
            if (_nearZone == null) { return pos; }

            Vector2 dir = _nearPoint - pos;
            if (dir.sqrMagnitude < 0.0001f)
            {
                // Đứng ngay trong vùng: quăng theo hướng nhìn.
                dir = FacingVector(p != null ? p.Facing : FacingDir.Down);
            }
            dir.Normalize();

            float reach = Mathf.Max(_cfg.castDistance, _nearDist + WaterInset);
            Vector2 target = pos + dir * reach;
            if (_nearZone.Contains(target)) { return target; }

            target = _nearPoint + dir * WaterInset;
            if (_nearZone.Contains(target)) { return target; }

            return _nearZone.RandomPointInside();
        }

        private static Vector2 FacingVector(FacingDir f)
        {
            switch (f)
            {
                case FacingDir.Up: return Vector2.up;
                case FacingDir.Left: return Vector2.left;
                case FacingDir.Right: return Vector2.right;
                default: return Vector2.down;
            }
        }

        private void SetMovementLocked(bool locked)
        {
            if (_movementLocked == locked) { return; }
            _movementLocked = locked;
            FishingPlayerController p = FishingPlayerController.Local;
            if (p != null) { p.SetMovementLocked(locked); }
        }

        private void VibrateIfAllowed()
        {
            if (!_cfg.vibrateOnBite || !Application.isMobilePlatform) { return; }
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
