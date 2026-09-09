using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Camera orthographic bám theo Target (SmoothDamp ở LateUpdate — vị trí người chơi đã chốt), kẹp trong BoxCollider2D
    /// "CameraBounds" (trừ nửa khung nhìn để mép map không lộ). Không có CameraBounds thì không kẹp.
    /// Bounds nhỏ hơn 1.5× khung nhìn theo CẢ 2 trục → coi là không hợp lệ (camera sẽ không đi theo được) → bám tự do, log 1 lần.
    /// ZOOM: pinch 2 ngón (Touchscreen) · cuộn chuột (Editor) · 2 nút +/− trên HUD. Kẹp trong [cfg.cameraZoomMin, cfg.cameraZoomMax],
    /// lưu PlayerPrefs FISHING_ZOOM (InvariantCulture) để lần sau vào scene giữ nguyên tầm nhìn Sếp thích.
    /// </summary>
    public class FishingCameraFollow : MonoBehaviour
    {
        /// <summary>Khoá PlayerPrefs lưu mức zoom người chơi tự chỉnh (menu 9 của tool xoá khoá này).</summary>
        public const string PrefsZoom = "FISHING_ZOOM";

        private const float UnityDefaultOrthoSize = 5f;
        /// <summary>
        /// Bounds nhỏ hơn tỉ lệ này × khung nhìn theo CẢ 2 trục thì bỏ kẹp, bám tự do.
        /// [Lead vòng 16] 1.5 → 1.0: Sếp muốn "tầm nhìn chỉ tới vòng map"; menu 8 đã fit bounds + zoom theo map thật, nên chỉ bỏ kẹp
        /// khi bounds thật sự NHỎ HƠN khung nhìn (không thể kẹp), còn bounds vừa/lớn hơn thì kẹp (trục hẹp đứng giữa, trục dài kẹp mép).
        /// Public để menu 8 tính fit zoom cùng một luật.
        /// </summary>
        public const float MinBoundsViewRatio = 1.0f;
        private const float MinPinchDistance = 4f;      // px, dưới mức này coi như 2 ngón chồng nhau
        private const float ZoomEpsilon = 0.0005f;

        [SerializeField] private Transform target;
        [Tooltip("Tên object chứa BoxCollider2D giới hạn camera. Mặc định FishingIds.CameraBoundsName.")]
        [SerializeField] private string boundsObjectName = FishingIds.CameraBoundsName;
        [Tooltip("Kẹp camera trong CameraBounds. Tắt = bám tự do.")]
        [SerializeField] private bool clampToBounds = true;
        [Tooltip("Cho phép pinch 2 ngón / cuộn chuột đổi zoom. Tắt = chỉ đổi được bằng 2 nút HUD.")]
        [SerializeField] private bool pinchZoomEnabled = true;

        public Transform Target { get { return target; } set { target = value; } }

        /// <summary>Mức zoom hiện tại = orthographicSize của camera.</summary>
        public float Zoom { get { return _cam != null ? _cam.orthographicSize : _zoom; } }

        /// <summary>Bắn mỗi lần zoom đổi (HUD có thể hiện số, làm mờ nút khi chạm biên).</summary>
        public event Action<float> OnZoomChanged;

        private Camera _cam;
        private BoxCollider2D _bounds;
        private bool _boundsSearched;
        private bool _boundsTooSmallLogged;
        private Vector3 _velocity;
        private FishingConfig _cfg;
        private bool _initialized;

        private float _zoom = UnityDefaultOrthoSize;
        private float _baseZoom = UnityDefaultOrthoSize;    // mốc để bù scale nhân vật
        private float _pinchPrevDistance = -1f;
        private bool _inputWarned;
        private Transform _scaledPlayer;
        private Vector3 _scaledPlayerBase = Vector3.one;

        private void Start()
        {
            EnsureInit();
            LoadZoom();
        }

        private void EnsureInit()
        {
            if (_initialized) { return; }
            _initialized = true;
            _cfg = FishingDatabase.ConfigOrDefault;
            _cam = GetComponent<Camera>();
            if (_cam == null) { _cam = Camera.main; }
            if (_cam != null)
            {
                _cam.orthographic = true;
                // Chỉ đặt size khi còn giá trị mặc định 5 của Unity — tôn trọng nếu Sếp đã chỉnh tay.
                if (Mathf.Approximately(_cam.orthographicSize, UnityDefaultOrthoSize)) { _cam.orthographicSize = _cfg.cameraOrthoSize; }
                // Iso: renderer cùng order thì y cao vẽ trước (xa hơn).
                _cam.transparencySortMode = TransparencySortMode.CustomAxis;
                _cam.transparencySortAxis = Vector3.up;
                _zoom = _cam.orthographicSize;
            }
            else
            {
                Debug.LogWarning(FishingIds.LogTag + " FishingCameraFollow không tìm thấy Camera.");
            }
            _baseZoom = _cfg != null ? Mathf.Max(0.5f, _cfg.cameraOrthoSize) : UnityDefaultOrthoSize;
        }

        private void FindBoundsOnce()
        {
            if (_boundsSearched) { return; }
            _boundsSearched = true;
            string n = string.IsNullOrEmpty(boundsObjectName) ? FishingIds.CameraBoundsName : boundsObjectName;
            var go = GameObject.Find(n);
            if (go != null) { _bounds = go.GetComponent<BoxCollider2D>(); }
            if (_bounds == null) { Debug.Log(FishingIds.LogTag + " Không có '" + n + "' (BoxCollider2D) — camera không kẹp biên."); }
        }

        private void Update()
        {
            if (!pinchZoomEnabled) { return; }
            EnsureInit();
            HandleZoomInput();
        }

        private void LateUpdate()
        {
            if (target == null) { return; }
            EnsureInit();
            FindBoundsOnce();
            Vector3 desired = ClampToBounds(target.position);
            desired.z = transform.position.z;
            float smooth = _cfg != null ? _cfg.cameraSmoothTime : 0.12f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smooth);
        }

        /// <summary>Nhảy thẳng tới Target (lúc spawn) để không thấy camera trôi từ gốc toạ độ.</summary>
        public void SnapToTarget()
        {
            if (target == null) { return; }
            EnsureInit();
            FindBoundsOnce();
            Vector3 p = ClampToBounds(target.position);
            p.z = transform.position.z;
            transform.position = p;
            _velocity = Vector3.zero;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ZOOM
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Đặt mức zoom (orthographicSize), kẹp min/max theo config, lưu PlayerPrefs và bắn OnZoomChanged.</summary>
        public void SetZoom(float orthoSize)
        {
            EnsureInit();
            float min = _cfg != null ? Mathf.Max(0.5f, _cfg.cameraZoomMin) : 3.5f;
            float max = _cfg != null ? Mathf.Max(min + 0.5f, _cfg.cameraZoomMax) : 11f;
            float v = Mathf.Clamp(orthoSize, min, max);
            if (Mathf.Abs(v - _zoom) < ZoomEpsilon && _cam != null && Mathf.Abs(_cam.orthographicSize - v) < ZoomEpsilon) { return; }
            _zoom = v;
            if (_cam != null) { _cam.orthographicSize = v; }
            ApplyPlayerScaleCompensation();
            SaveZoom(v);
            if (OnZoomChanged != null) { OnZoomChanged(v); }
        }

        /// <summary>Zoom vào 1 bước (khung nhìn nhỏ lại, nhân vật to lên).</summary>
        public void ZoomIn()
        {
            EnsureInit();
            float step = _cfg != null ? Mathf.Clamp(_cfg.zoomStep, 0.05f, 1f) : 0.25f;
            SetZoom(Zoom / (1f + step));
        }

        /// <summary>Zoom xa 1 bước (thấy nhiều map hơn, tile nhỏ lại).</summary>
        public void ZoomOut()
        {
            EnsureInit();
            float step = _cfg != null ? Mathf.Clamp(_cfg.zoomStep, 0.05f, 1f) : 0.25f;
            SetZoom(Zoom * (1f + step));
        }

        /// <summary>Start: nạp mức zoom đã lưu; chưa lưu lần nào thì dùng cfg.cameraOrthoSize.</summary>
        private void LoadZoom()
        {
            float wanted = _cfg != null ? _cfg.cameraOrthoSize : UnityDefaultOrthoSize;
            string saved = PlayerPrefs.GetString(PrefsZoom, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                float parsed;
                if (float.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) && parsed > 0.1f) { wanted = parsed; }
                else { Debug.Log(FishingIds.LogTag + " PlayerPrefs " + PrefsZoom + " hỏng ('" + saved + "') — dùng cameraOrthoSize " + wanted.ToString("0.##", CultureInfo.InvariantCulture) + "."); }
            }
            SetZoom(wanted);
        }

        private void SaveZoom(float v)
        {
            PlayerPrefs.SetString(PrefsZoom, v.ToString("0.###", CultureInfo.InvariantCulture));
            LuuGopPrefs.Hen();
        }

        /// <summary>
        /// Pinch 2 ngón (mobile) + cuộn chuột (Editor/PC). API Input System khác bản → bọc try/catch, lỗi thì tắt hẳn và log 1 lần.
        /// </summary>
        private void HandleZoomInput()
        {
            try
            {
                // Touchscreen.touches LUÔN có 10 ô (kể cả ngón chưa chạm) → phải lọc theo press.isPressed, lấy 2 ngón đầu đang chạm.
                var ts = Touchscreen.current;
                Vector2 p0 = Vector2.zero, p1 = Vector2.zero;
                int down = 0;
                if (ts != null)
                {
                    var touches = ts.touches;
                    for (int i = 0; i < touches.Count && down < 2; i++)
                    {
                        var tc = touches[i];
                        if (tc == null || !tc.press.isPressed) { continue; }
                        if (down == 0) { p0 = tc.position.ReadValue(); } else { p1 = tc.position.ReadValue(); }
                        down++;
                    }
                }
                if (down < 2) { _pinchPrevDistance = -1f; }
                else
                {
                    float d = Vector2.Distance(p0, p1);
                    if (d >= MinPinchDistance)
                    {
                        // Ngón dang ra (d lớn hơn trước) → tỉ lệ < 1 → ortho nhỏ lại → zoom VÀO.
                        if (_pinchPrevDistance >= MinPinchDistance) { SetZoom(Zoom * (_pinchPrevDistance / d)); }
                        _pinchPrevDistance = d;
                    }
                }

                // Cuộn chuột (Editor/PC). Bỏ qua khi trỏ đang trên UI để không zoom lúc cuộn danh sách chat/giỏ cá.
                var mouse = Mouse.current;
                if (mouse != null && !InputBridge.IsPointerOverUI())
                {
                    float scrollY = mouse.scroll.ReadValue().y;
                    if (scrollY > 0.01f) { ZoomIn(); }
                    else if (scrollY < -0.01f) { ZoomOut(); }
                }
            }
            catch (Exception e)
            {
                pinchZoomEnabled = false;
                if (!_inputWarned)
                {
                    _inputWarned = true;
                    Debug.LogWarning(FishingIds.LogTag + " Đọc pinch/cuộn chuột lỗi (" + e.Message + ") — tắt zoom bằng cử chỉ, vẫn dùng được 2 nút +/− trên HUD.");
                }
            }
        }

        /// <summary>
        /// Bù nhân vật khi zoom xa: cfg.playerScaleZoomCompensation 0 = không đụng tới nhân vật (mặc định),
        /// 1 = phóng nhân vật đúng tỉ lệ zoom (nhìn to như cũ dù thấy rộng hơn).
        /// </summary>
        private void ApplyPlayerScaleCompensation()
        {
            float k = _cfg != null ? Mathf.Clamp01(_cfg.playerScaleZoomCompensation) : 0f;
            if (k <= 0.001f) { return; }
            var player = FishingPlayerController.Local;
            if (player == null) { return; }
            Transform t = player.transform;
            if (_scaledPlayer != t)
            {
                _scaledPlayer = t;
                _scaledPlayerBase = t.localScale;
            }
            float baseZoom = Mathf.Max(0.5f, _baseZoom);
            float factor = Mathf.Lerp(1f, Mathf.Max(0.01f, _zoom) / baseZoom, k);
            t.localScale = new Vector3(_scaledPlayerBase.x * factor, _scaledPlayerBase.y * factor, _scaledPlayerBase.z);
        }

        private Vector3 ClampToBounds(Vector3 p)
        {
            if (!clampToBounds || _bounds == null || _cam == null) { return p; }
            Bounds b = _bounds.bounds;
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            // Bounds nhỏ hơn 1.5× khung nhìn theo CẢ 2 trục → camera hầu như không lắc được → bỏ kẹp, bám tự do (log 1 lần).
            if (b.size.x < halfW * 2f * MinBoundsViewRatio && b.size.y < halfH * 2f * MinBoundsViewRatio)
            {
                if (!_boundsTooSmallLogged)
                {
                    _boundsTooSmallLogged = true;
                    Debug.Log(FishingIds.LogTag + " CameraBounds quá nhỏ (" + b.size.x.ToString("0.#", CultureInfo.InvariantCulture) + "x" + b.size.y.ToString("0.#", CultureInfo.InvariantCulture) + " so với khung nhìn " + (halfW * 2f).ToString("0.#", CultureInfo.InvariantCulture) + "x" + (halfH * 2f).ToString("0.#", CultureInfo.InvariantCulture) + "), camera bám tự do. Phóng BoxCollider2D CameraBounds theo map để kẹp lại (menu 8 của tool tự làm).");
                }
                return p;
            }

            // Bounds hẹp hơn khung nhìn thì đứng giữa bounds theo trục đó, không nhảy qua lại.
            p.x = b.size.x <= halfW * 2f ? b.center.x : Mathf.Clamp(p.x, b.min.x + halfW, b.max.x - halfW);
            p.y = b.size.y <= halfH * 2f ? b.center.y : Mathf.Clamp(p.y, b.min.y + halfH, b.max.y - halfH);
            return p;
        }
    }
}
