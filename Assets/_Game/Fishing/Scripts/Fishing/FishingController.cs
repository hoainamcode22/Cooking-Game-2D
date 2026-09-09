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
    /// Lực quăng: Dev C (HUD) chạy thanh đo rồi gọi Cast(power01); Cast() = không thanh đo. Hoàn hảo (≥ perfectCastThreshold):
    /// IsPerfectCast / OnPerfectCast / LastCastPower01 — bonus xác suất + cá hiếm, phao bay cao hơn, splash to hơn.
    /// </summary>
    public class FishingController : MonoBehaviour
    {
        private const string BobberChildName = "Bobber";
        private const string RodVisualChildName = "RodVisual";
        // Phao phải rơi hẳn vào nước, không nằm đúng mép: đẩy thêm khoảng này (unit) từ mép vào — số hình ảnh, không phải gameplay.
        private const float WaterInset = 0.4f;
        // Bước lùi (unit) khi điểm rơi theo lực quăng vượt ra ngoài vùng nước — số hình học, không phải gameplay.
        private const float ReachBackStep = 0.1f;
        // Quăng hoàn hảo: phao bay cao hơn / splash to hơn (thuần hình ảnh).
        private const float PerfectArcMultiplier = 1.4f;
        private const float PerfectSplashScale = 1.3f;

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

        /// <summary>
        /// Lực của lần quăng gần nhất (0..1) khi quăng qua thanh đo (Cast(power01)); -1 = quăng không qua thanh đo (Cast()).
        /// Giữ nguyên qua cả lượt (kể cả sau khi về Idle) để HUD hiện "lần trước".
        /// </summary>
        public float LastCastPower01 { get; private set; } = -1f;

        /// <summary>
        /// Lượt ĐANG câu là quăng hoàn hảo (power01 ≥ cfg.perfectCastThreshold): cộng cfg.perfectCatchBonus vào xác suất bắt,
        /// nhân cfg.perfectRareMultiplier vào trọng số cá hiếm. Đặt lúc Cast, về false khi lượt kết thúc (Idle).
        /// </summary>
        public bool IsPerfectCast { get; private set; }

        public event Action<FishingPhase> OnPhaseChanged;
        public event Action<FishData, float> OnCatch;
        public event Action OnEscape;
        public event Action OnNothing;
        public event Action OnRodBroken;
        public event Action<string> OnBlocked;
        /// <summary>Bắn ngay trong Cast(power01) khi power01 ≥ cfg.perfectCastThreshold, sau OnPhaseChanged(Casting) cùng frame. HUD hiện "HOÀN HẢO!".</summary>
        public event Action OnPerfectCast;

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

        /// <summary>Bấm QUĂNG không qua thanh đo (khoảng cách cfg.castDistance, không bao giờ "hoàn hảo"). = Cast(-1f).</summary>
        public void Cast()
        {
            Cast(-1f);
        }

        /// <summary>
        /// Bấm QUĂNG với lực từ thanh đo (Dev C tự chạy con trượt rồi gọi hàm này lúc thả nút).
        /// power01 trong [0,1] → khoảng cách = Lerp(cfg.castDistanceMin, cfg.castDistanceMax, power01), điểm rơi luôn kẹp vào trong FishingZone;
        /// power01 ≥ cfg.perfectCastThreshold → IsPerfectCast = true, OnPerfectCast bắn ngay, phao bay cao hơn, splash to hơn, roll bắt được
        /// cộng cfg.perfectCatchBonus, cá hiếm × cfg.perfectRareMultiplier. power01 &lt; 0 → như Cast() cũ (cfg.castDistance).
        /// Không đủ điều kiện (CanCast == false) → OnBlocked(BlockReasonVi) + tiếng fail, không đổi pha.
        /// </summary>
        public void Cast(float power01)
        {
            RefreshCanCast();
            if (!CanCast)
            {
                OnBlocked?.Invoke(BlockReasonVi);
                FishingAudio.PlayFail();
                return;
            }

            bool useMeter = power01 >= 0f;
            float power = useMeter ? Mathf.Clamp01(power01) : -1f;
            bool perfect = useMeter && power >= _cfg.perfectCastThreshold;

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

            float reach = useMeter ? Mathf.Lerp(_cfg.castDistanceMin, _cfg.castDistanceMax, power) : _cfg.castDistance;
            Vector2 target = ComputeBobberTarget(reach);
            // Đặt TRƯỚC TryCast để HUD đọc được ngay trong OnPhaseChanged(Casting). CanCast đã bảo đảm Idle nên TryCast không thất bại.
            LastCastPower01 = power;
            IsPerfectCast = perfect;
            if (!_sm.TryCast(UnityEngine.Random.value)) { IsPerfectCast = false; return; }

            SetMovementLocked(true);
            FishingPlayerController p = FishingPlayerController.Local;
            Vector2 from = p != null && p.HandAnchor != null ? (Vector2)p.HandAnchor.position : (p != null ? p.Position : target);
            if (_rodVisual != null) { _rodVisual.SetVisible(true); }
            if (_bobber != null)
            {
                _bobber.FlyTo(from, target, _cfg.castDurationSeconds, perfect ? PerfectArcMultiplier : 1f, perfect ? PerfectSplashScale : 1f);
            }
            FishingAudio.PlayCast();
            if (perfect) { OnPerfectCast?.Invoke(); }
            Debug.Log(FishingIds.LogTag + " Quăng: cần=" + rod.itemID + " lực=" + (useMeter ? power.ToString("0.00", CultureInfo.InvariantCulture) : "mặc định") + (perfect ? " HOÀN HẢO" : "") + " xa=" + reach.ToString("0.00", CultureInfo.InvariantCulture) + " chờ=" + _sm.BiteWaitTarget.ToString("0.0", CultureInfo.InvariantCulture) + "s còn " + gear.EquippedDurabilityLeft + " lần");
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
            // Quăng hoàn hảo: cộng xác suất + nhân trọng số cá hiếm (số từ FishingConfig).
            float bonus = (rod != null ? rod.catchChanceBonus : 0f) + (IsPerfectCast ? _cfg.perfectCatchBonus : 0f);
            bool caught = FishingCatchResolver.RollCatch(_cfg.baseCatchChance, bonus, UnityEngine.Random.value);

            FishData fish = null;
            if (caught)
            {
                FishingDatabase db = FishingDatabase.Instance;
                int tier = rod != null ? rod.tier : 1;
                float rareMult = (rod != null ? rod.rareWeightMultiplier : 1f) * (IsPerfectCast ? _cfg.perfectRareMultiplier : 1f);
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
            if (_rodVisual != null) { _rodVisual.SetPhase(next); }

            switch (next)
            {
                case FishingPhase.Waiting:
                    // Splash to + tiếng "tủm" do FishingBobber tự phát đúng lúc hết FlyTo (thời gian bay = castDuration).
                    if (_bobber != null) { _bobber.Bob(false); }
                    break;

                case FishingPhase.Bite:
                    // Phao tự giật xuống + ring nhỏ lặp 0.4 s trong Bob(true).
                    if (_bobber != null) { _bobber.Bob(true); }
                    FishingAudio.PlayBite();
                    VibrateIfAllowed();
                    break;

                case FishingPhase.Reeling:
                    FishingAudio.PlayReel();
                    if (_bobber != null)
                    {
                        // Thu: splash nhỏ (1 ring + 4 giọt).
                        FishingSplashPool.Shared.PlaySmall(_bobber.WaterPoint, _bobber.Renderer, 1f);
                        _bobber.Hide();
                    }
                    break;

                case FishingPhase.Result:
                    if (prev == FishingPhase.Waiting && _bobber != null)
                    {
                        // Thu sớm: Waiting → Result thẳng, chưa có splash thu.
                        FishingSplashPool.Shared.PlaySmall(_bobber.WaterPoint, _bobber.Renderer, 0.8f);
                        FishingAudio.PlayReel();
                    }
                    if (_bobber != null) { _bobber.Hide(); }
                    FireOutcomeEvents();
                    break;

                case FishingPhase.Idle:
                    if (_rodVisual != null) { _rodVisual.SetVisible(false); }
                    if (_bobber != null) { _bobber.Hide(); }
                    SetMovementLocked(false);
                    IsPerfectCast = false;
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
        /// Điểm phao rơi: từ người theo hướng ra mép nước gần nhất, xa desiredReach (kẹp sàn = mép + WaterInset để lọt hẳn vào nước).
        /// Vượt mép xa của vùng → lùi dần ReachBackStep tới khi Contains (tối thiểu mép + inset); vẫn không → điểm gần nhất trên vùng
        /// (ClosestPoint) đẩy vào trong 1 inset; vẫn không → điểm ngẫu nhiên trong vùng.
        /// </summary>
        private Vector2 ComputeBobberTarget(float desiredReach)
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

            float minReach = _nearDist + WaterInset;
            float reach = Mathf.Max(desiredReach, minReach);
            Vector2 target = pos + dir * reach;
            if (_nearZone.Contains(target)) { return target; }

            // Quá xa (bay qua bờ bên kia / ra ngoài góc): lùi dần về phía người tới khi lọt vùng.
            int steps = Mathf.CeilToInt((reach - minReach) / ReachBackStep);
            for (int i = 1; i <= steps; i++)
            {
                float r = Mathf.Max(minReach, reach - ReachBackStep * i);
                target = pos + dir * r;
                if (_nearZone.Contains(target)) { return target; }
            }

            // Kẹp về điểm gần nhất trên vùng rồi đẩy vào trong theo hướng quăng.
            Vector2 cp = _nearZone.ClosestPoint(pos + dir * reach);
            target = cp + dir * WaterInset;
            if (_nearZone.Contains(target)) { return target; }
            target = cp - dir * WaterInset;   // mép xa: đẩy ngược về phía người
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
