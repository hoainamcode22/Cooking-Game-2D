using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bảng đo lực quăng: card bo tròn (CardOuter/CardInner) neo bottom-center của Canvas_FishingHUD, con "Meter_CastPower".
    /// Thanh ngang chia 3 vùng Gần / Vừa / Xa theo cfg.castZoneNearEnd / castZoneMidEnd, vùng HOÀN HẢO từ cfg.perfectCastThreshold → 1.
    /// Con trượt chạy ping-pong 0→1→0 với tốc độ cfg.castChargeSpeed (lượt/giây) khi đang giữ nút QUĂNG; Begin() bắt đầu từ 0,
    /// End() trả lực 0..1 lúc thả và ẩn card; thả trong vùng hoàn hảo → tự bắn PerfectCastFX (HUD KHÔNG bắn FX thêm để không double).
    /// Giữ quá MaxHoldSeconds không thả → tự End() và bắn OnAutoEnd(power) cho HUD gọi Cast (chống kẹt ngón tay).
    /// Ẩn mặc định; dựng bằng BuildIfEmpty() find-or-create, không huỷ con có sẵn.
    /// </summary>
    public class CastPowerMeterUI : MonoBehaviour
    {
        public static CastPowerMeterUI Instance { get; private set; }

        public const string ObjectName = "Meter_CastPower";
        public static readonly Vector2 CardSize = new Vector2(620f, 150f);

        private const float MaxHoldSeconds = 4f;
        private const float BarWidth = 540f;
        private const float BarHeight = 26f;
        private const float BarY = -6f;
        private const float MarkerWidth = 14f;
        private const float ZoneInset = 3f;

        private static readonly Color ZoneNearColor = new Color32(150, 220, 130, 255);
        private static readonly Color ZoneMidColor = new Color32(250, 210, 80, 255);
        private static readonly Color ZoneFarColor = new Color32(245, 140, 60, 255);
        private static readonly Color ZonePerfectColor = new Color32(142, 31, 59, 255);   // #8E1F3B burgundy
        private static readonly Color PerfectGlowColor = new Color32(255, 230, 150, 255);
        private static readonly Color TrackFallback = new Color32(70, 50, 40, 230);

        [Header("Tham chiếu (BuildIfEmpty tự gán nếu trống)")]
        [SerializeField] private Image imgOuter;
        [SerializeField] private Image imgInner;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Image imgTrack;
        [SerializeField] private Image imgZoneNear;
        [SerializeField] private Image imgZoneMid;
        [SerializeField] private Image imgZoneFar;
        [SerializeField] private Image imgZonePerfect;
        [SerializeField] private TextMeshProUGUI txtNear;
        [SerializeField] private TextMeshProUGUI txtMid;
        [SerializeField] private TextMeshProUGUI txtFar;
        [SerializeField] private TextMeshProUGUI txtPerfect;
        [SerializeField] private Image imgMarker;

        private bool _charging;
        private float _pingPongT;
        private float _holdSeconds;
        private float _current;
        private float _speed = 1.4f;
        private float _perfectThreshold = 0.88f;

        /// <summary>Đang giữ nút (con trượt đang chạy).</summary>
        public bool IsCharging { get { return _charging; } }

        /// <summary>Vị trí con trượt 0..1 hiện tại (giữ nguyên giá trị lúc thả sau End()).</summary>
        public float Current01 { get { return _current; } }

        /// <summary>Giữ quá lâu → meter tự End(); HUD nghe để gọi FishingController.Cast(power).</summary>
        public event Action<float> OnAutoEnd;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Debug.LogWarning(FishingIds.LogTag + " CastPowerMeterUI trùng tại '" + name + "' — tự ẩn, không huỷ."); gameObject.SetActive(false); return; }
            Instance = this;
            BuildIfEmpty();
        }

        private void OnDestroy() { if (Instance == this) { Instance = null; } }

        private void OnDisable()
        {
            // Bị tắt giữa lúc giữ (đổi scene, HUD ẩn) → coi như huỷ, không quăng.
            _charging = false;
        }

        private void Update()
        {
            if (!_charging) { return; }
            float dt = Time.deltaTime;   // deltaTime để pause game thì con trượt cũng dừng
            _holdSeconds += dt;
            _pingPongT += dt * _speed;
            _current = Mathf.PingPong(_pingPongT, 1f);
            PlaceMarker(_current);
            if (_holdSeconds >= MaxHoldSeconds)
            {
                float p = End();
                Debug.Log(FishingIds.LogTag + " Giữ QUĂNG quá " + MaxHoldSeconds + "s → tự thả, lực=" + p.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                OnAutoEnd?.Invoke(p);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Bắt đầu giữ: con trượt về 0, hiện card. Gọi lại khi đang giữ thì bỏ qua.</summary>
        public void Begin()
        {
            if (_charging) { return; }
            BuildIfEmpty();
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            _speed = Mathf.Max(0.05f, cfg.castChargeSpeed);
            _perfectThreshold = Mathf.Clamp01(cfg.perfectCastThreshold);
            LayoutZones(cfg);
            _pingPongT = 0f;
            _holdSeconds = 0f;
            _current = 0f;
            PlaceMarker(0f);
            _charging = true;
            FishingUiKit.ActivateUpToCanvas(transform);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            JuicyPulseFX.Play(transform, 1.06f, 0.18f);
        }

        /// <summary>Thả: trả lực 0..1 tại lúc thả, ẩn card. Vùng hoàn hảo → bắn PerfectCastFX tại con trượt. Không đang giữ → 0.</summary>
        public float End()
        {
            if (!_charging) { return 0f; }
            _charging = false;
            float power = Mathf.Clamp01(_current);
            bool perfect = power >= _perfectThreshold;
            if (perfect && imgMarker != null) { PerfectCastFX.Play(imgMarker.rectTransform); }
            gameObject.SetActive(false);
            return power;
        }

        /// <summary>Huỷ giữ (đổi pha, mở panel...): ẩn card, không FX, không quăng.</summary>
        public void Cancel()
        {
            _charging = false;
            if (gameObject.activeSelf) { gameObject.SetActive(false); }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DỰNG (find-or-create, không huỷ con)
        // ─────────────────────────────────────────────────────────────────────

        public void BuildIfEmpty()
        {
            var selfRt = transform as RectTransform;
            if (selfRt != null && selfRt.sizeDelta == Vector2.zero) { selfRt.sizeDelta = CardSize; }

            // Card bo tròn: ngoài + trong.
            Image outer = FishingUiKit.Panel(transform, "Img_CardOuter", CardSize, UIStandardSprites.CardOuter, Vector2.zero, FishingUiKit.FrameFallback);
            if (outer.sprite == null) { outer.sprite = FishingUiKit.Rounded(28f); outer.type = Image.Type.Sliced; outer.color = FishingUiKit.FrameFallback; }
            outer.raycastTarget = false;
            if (imgOuter == null) { imgOuter = outer; }

            Image inner = FishingUiKit.Panel(transform, "Img_CardInner", CardSize - new Vector2(16f, 16f), UIStandardSprites.CardInner, Vector2.zero, FishingUiKit.PanelFallback);
            if (inner.sprite == null) { inner.sprite = FishingUiKit.Rounded(22f); inner.type = Image.Type.Sliced; inner.color = FishingUiKit.PanelFallback; }
            inner.raycastTarget = false;
            if (imgInner == null) { imgInner = inner; }

            Transform body = inner.transform;

            // Tiêu đề nhỏ.
            TextMeshProUGUI title = FishingUiKit.Label(body, "Txt_Title", Loc.T("LỰC QUĂNG"), 22f, TextAlignmentOptions.Center, new Vector2(300f, 30f), new Vector2(0f, 46f), FishingUiKit.TextMuted, true);
            if (txtTitle == null) { txtTitle = title; }

            // Thanh nền.
            Image track = FishingUiKit.Panel(body, "Img_Track", new Vector2(BarWidth, BarHeight), UIStandardSprites.BarTrack, new Vector2(0f, BarY), TrackFallback);
            if (track.sprite == null) { track.sprite = FishingUiKit.Rounded(12f); track.type = Image.Type.Sliced; track.color = TrackFallback; }
            track.raycastTarget = false;
            if (imgTrack == null) { imgTrack = track; }

            // 3 vùng + vùng hoàn hảo (anchor theo % chiều ngang thanh, đặt lại ở LayoutZones).
            Image near = Zone(track.transform, "Img_ZoneNear", ZoneNearColor);
            if (imgZoneNear == null) { imgZoneNear = near; }
            Image mid = Zone(track.transform, "Img_ZoneMid", ZoneMidColor);
            if (imgZoneMid == null) { imgZoneMid = mid; }
            Image far = Zone(track.transform, "Img_ZoneFar", ZoneFarColor);
            if (imgZoneFar == null) { imgZoneFar = far; }
            Image perfect = Zone(track.transform, "Img_ZonePerfect", ZonePerfectColor);
            if (imgZonePerfect == null) { imgZonePerfect = perfect; }
            Outline glow = FishingUiKit.GetOrAdd<Outline>(perfect.gameObject);
            if (glow != null) { glow.effectColor = PerfectGlowColor; glow.effectDistance = new Vector2(2f, 2f); glow.useGraphicAlpha = true; }

            // Nhãn dưới thanh + nhãn HOÀN HẢO trên vùng đỏ.
            TextMeshProUGUI n = ZoneLabel(track.transform, "Txt_Near", Loc.T("Gần"), -1f);
            if (txtNear == null) { txtNear = n; }
            TextMeshProUGUI m = ZoneLabel(track.transform, "Txt_Mid", Loc.T("Vừa"), -1f);
            if (txtMid == null) { txtMid = m; }
            TextMeshProUGUI f = ZoneLabel(track.transform, "Txt_Far", Loc.T("Xa"), -1f);
            if (txtFar == null) { txtFar = f; }
            bool perfectLabelCreated = FishingUiKit.FindChild(track.transform, "Txt_Perfect") == null;
            TextMeshProUGUI pf = ZoneLabel(track.transform, "Txt_Perfect", Loc.T("HOÀN HẢO"), 1f);
            if (perfectLabelCreated) { pf.color = ZonePerfectColor; pf.fontSize = 15f; }
            if (txtPerfect == null) { txtPerfect = pf; }

            // Con trượt: cao hơn thanh 10 px, trắng viền tối.
            bool markerCreated;
            RectTransform mk = FishingUiKit.Child(track.transform, "Img_Marker", new Vector2(MarkerWidth, BarHeight + 10f), Vector2.zero, out markerCreated);
            Image marker = FishingUiKit.GetOrAdd<Image>(mk.gameObject);
            if (markerCreated || marker.sprite == null)
            {
                // Rounded(5) thay BarFill: sprite bar 9-slice rộng hơn 14 px sẽ bị bóp méo ở con trượt mảnh.
                FishingUiKit.SetSlicedOrSimple(marker, FishingUiKit.Rounded(5f), Color.white);
                marker.color = Color.white;
            }
            marker.raycastTarget = false;
            if (markerCreated)
            {
                FishingUiKit.AddShadow(marker, new Color(0f, 0f, 0f, 0.55f), new Vector2(1f, -2f));
                Outline mo = FishingUiKit.GetOrAdd<Outline>(mk.gameObject);
                if (mo != null) { mo.effectColor = new Color(0.3f, 0.18f, 0.1f, 0.9f); mo.effectDistance = new Vector2(1.5f, 1.5f); }
            }
            if (imgMarker == null) { imgMarker = marker; }

            LayoutZones(FishingDatabase.ConfigOrDefault);
            PlaceMarker(_current);
        }

        /// <summary>Image vùng màu trong thanh: bo góc, không chặn raycast. Anchor đặt ở LayoutZones.</summary>
        private static Image Zone(Transform track, string name, Color color)
        {
            bool created;
            RectTransform rt = FishingUiKit.Child(track, name, Vector2.zero, Vector2.zero, out created);
            Image img = FishingUiKit.GetOrAdd<Image>(rt.gameObject);
            if (created || img.sprite == null) { FishingUiKit.SetSlicedOrSimple(img, FishingUiKit.Rounded(5f), color); }
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Nhãn nhỏ neo dưới (side = -1) hoặc trên (side = +1) thanh; x đặt ở LayoutZones.</summary>
        private static TextMeshProUGUI ZoneLabel(Transform track, string name, string text, float side)
        {
            bool created = FishingUiKit.FindChild(track, name) == null;
            TextMeshProUGUI tmp = FishingUiKit.Label(track, name, text, 18f, TextAlignmentOptions.Center, new Vector2(140f, 24f), Vector2.zero, FishingUiKit.TextDark, true);
            RectTransform rt = tmp.rectTransform;
            if (created)
            {
                float ay = side > 0f ? 1f : 0f;
                rt.anchorMin = new Vector2(0.5f, ay); rt.anchorMax = new Vector2(0.5f, ay); rt.pivot = new Vector2(0.5f, side > 0f ? 0f : 1f);
                rt.anchoredPosition = new Vector2(0f, side > 0f ? 4f : -4f);
                tmp.overflowMode = TextOverflowModes.Overflow;
            }
            return tmp;
        }

        /// <summary>Đặt anchor 4 vùng + x của nhãn theo config (data-driven, gọi lại mỗi Begin để đổi số trong Inspector thấy ngay).</summary>
        private void LayoutZones(FishingConfig cfg)
        {
            float nearEnd = Mathf.Clamp01(cfg.castZoneNearEnd);
            float midEnd = Mathf.Clamp(cfg.castZoneMidEnd, nearEnd, 1f);
            float perfectStart = Mathf.Clamp(cfg.perfectCastThreshold, midEnd, 1f);
            SetZone(imgZoneNear, 0f, nearEnd);
            SetZone(imgZoneMid, nearEnd, midEnd);
            SetZone(imgZoneFar, midEnd, perfectStart);
            SetZone(imgZonePerfect, perfectStart, 1f);
            SetLabelX(txtNear, 0.5f * nearEnd);
            SetLabelX(txtMid, 0.5f * (nearEnd + midEnd));
            SetLabelX(txtFar, 0.5f * (midEnd + perfectStart));
            SetLabelX(txtPerfect, 0.5f * (perfectStart + 1f));
        }

        private static void SetZone(Image img, float start, float end)
        {
            if (img == null) { return; }
            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(start, 0f);
            rt.anchorMax = new Vector2(end, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(ZoneInset, ZoneInset);
            rt.offsetMax = new Vector2(-ZoneInset, -ZoneInset);
            img.gameObject.SetActive(end - start > 0.001f);
        }

        private static void SetLabelX(TextMeshProUGUI tmp, float x01)
        {
            if (tmp == null) { return; }
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(x01, rt.anchorMin.y);
            rt.anchorMax = new Vector2(x01, rt.anchorMax.y);
        }

        private void PlaceMarker(float p01)
        {
            if (imgMarker == null) { return; }
            RectTransform rt = imgMarker.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((Mathf.Clamp01(p01) - 0.5f) * BarWidth, 0f);
        }
    }
}
