using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panel debug camera cho DEV — hiện thông số camera thời gian thực + nút zoom preset.
/// Gắn lên cùng GameObject với CameraController (hoặc bất kỳ object nào trong scene).
///
/// PHÍM TẮT:
///   F1  — Zoom ra xem toàn bản đồ
///   F2  — Bật/tắt Dev Mode (nới dải zoom 200..6000)
///   F3  — Ẩn/hiện panel này
///
/// Panel TỰ HUỶ trong bản phát hành (chỉ sống ở Editor / Development Build)
/// nên an toàn khi để lại trong scene.
/// Dùng OnGUI (IMGUI) nên KHÔNG cần prefab/Canvas — không đụng vào UI game.
///
/// CÁCH GẮN: chọn Main Camera trong scene → Add Component → Camera Dev Panel.
/// </summary>
[RequireComponent(typeof(CameraController))]
public class CameraDevPanel : MonoBehaviour
{
    [Header("Hiển thị")]
    [Tooltip("Hiện panel khi bắt đầu chạy.")]
    public bool showOnStart = true;

    [Tooltip("Phím ẩn/hiện panel.")]
    public Key toggleKey = Key.F3;

    [Tooltip("Góc màn hình đặt panel.")]
    public Corner corner = Corner.TopLeft;

    public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

    [Header("Nút zoom preset (orthographic size)")]
    public float[] zoomPresets = { 300f, 500f, 750f, 950f, 1500f, 3000f };

    // ── Nội bộ ──────────────────────────────────────────────────────────
    private CameraController _controller;
    private Camera           _cam;
    private bool             _visible;

    // FPS
    private float _fpsAccum;
    private int   _fpsFrames;
    private float _fpsDisplay;
    private float _fpsTimer;

    private GUIStyle _boxStyle, _labelStyle, _btnStyle, _titleStyle;
    private bool     _stylesReady;

    private const float PANEL_W = 260f;
    private const float MARGIN  = 10f;

    private void Awake()
    {
        // Gán vô điều kiện — nếu để trong #else thì bản release sẽ báo
        // warning CS0649 "field never assigned".
        _controller = GetComponent<CameraController>();
        _cam        = GetComponent<Camera>();
        // Không dùng `?? Camera.main`: Unity override toán tử == nên
        // null-coalescing bỏ qua "fake null" của UnityEngine.Object (UNT0007).
        if (_cam == null) _cam = Camera.main;
        _visible = showOnStart;

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // Bản phát hành: tự huỷ, người chơi không bao giờ thấy panel dev.
        // Không đặt `return;` ở đây — sẽ sinh CS0162 unreachable code.
        _visible = false;
        Destroy(this);
#else
        if (_cam == null)
            Debug.LogWarning("[CameraDevPanel] Không tìm thấy Camera trên object này và cũng " +
                             "không có Camera.main → panel sẽ không hiện. Hãy gắn vào Main Camera.");
#endif
    }

    private void Update()
    {
        // Toggle panel — chặn Key.None / giá trị ngoài dải vì Keyboard indexer sẽ ném exception
        var kb = Keyboard.current;
        if (kb != null && CameraController.IsValidKey(toggleKey) && kb[toggleKey].wasPressedThisFrame)
            _visible = !_visible;

        // FPS trung bình mỗi 0.25s (mượt hơn 1/deltaTime tức thời)
        _fpsAccum  += Time.unscaledDeltaTime;
        _fpsFrames += 1;
        _fpsTimer  += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.25f)
        {
            _fpsDisplay = _fpsAccum > 0f ? _fpsFrames / _fpsAccum : 0f;
            _fpsAccum   = 0f;
            _fpsFrames  = 0;
            _fpsTimer   = 0f;
        }
    }

    private void BuildStyles()
    {
        if (_stylesReady) return;

        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 8, 10),
            alignment = TextAnchor.UpperLeft
        };

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 13,
            normal    = { textColor = new Color(0.55f, 0.95f, 0.55f) }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal   = { textColor = Color.white },
            wordWrap = false
        };

        _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 11 };

        _stylesReady = true;
    }

    private void OnGUI()
    {
        if (!_visible || _controller == null || _cam == null) return;

        BuildStyles();

        // Panel cao động theo số hàng preset
        int   presetRows = Mathf.CeilToInt((zoomPresets?.Length ?? 0) / 3f);
        float panelH     = 232f + presetRows * 26f;

        Rect r = corner switch
        {
            Corner.TopRight    => new Rect(Screen.width - PANEL_W - MARGIN, MARGIN, PANEL_W, panelH),
            Corner.BottomLeft  => new Rect(MARGIN, Screen.height - panelH - MARGIN, PANEL_W, panelH),
            Corner.BottomRight => new Rect(Screen.width - PANEL_W - MARGIN, Screen.height - panelH - MARGIN, PANEL_W, panelH),
            _                  => new Rect(MARGIN, MARGIN, PANEL_W, panelH),
        };

        GUI.Box(r, GUIContent.none, _boxStyle);
        GUILayout.BeginArea(new Rect(r.x + 10f, r.y + 8f, r.width - 20f, r.height - 16f));

        // ── Tiêu đề ──
        GUILayout.Label("CAMERA DEV  (F3 ẩn/hiện)", _titleStyle);
        GUILayout.Space(2f);

        // ── Thông số ──
        float size    = _cam.orthographicSize;
        float viewH   = size * 2f;
        float viewW   = viewH * _cam.aspect;
        Vector3 pos   = transform.position;
        bool  devOn   = _controller.IsDevMode;

        GUI.color = _fpsDisplay < 30f ? new Color(1f, 0.5f, 0.5f) : Color.white;
        GUILayout.Label($"FPS      : {_fpsDisplay:F0}", _labelStyle);
        GUI.color = Color.white;

        GUILayout.Label($"Ortho    : {size:F0}   ({_controller.ActiveMinSize:F0}–{_controller.ActiveMaxSize:F0})", _labelStyle);
        GUILayout.Label($"Viewport : {viewW:F0} x {viewH:F0} unit", _labelStyle);
        GUILayout.Label($"Cam pos  : {pos.x:F0}, {pos.y:F0}", _labelStyle);

        // Bản đồ + cảnh báo che phủ
        Vector4 b     = _controller.bounds;
        float   mapW  = b.y - b.x;
        float   mapH  = b.w - b.z;
        GUILayout.Label($"Bounds   : {mapW:F0} x {mapH:F0} unit", _labelStyle);

        if (mapH > 0f)
        {
            float coverage = Mathf.Clamp01(viewH / mapH) * 100f;
            GUI.color = coverage < 40f ? new Color(1f, 0.75f, 0.4f) : Color.white;
            GUILayout.Label($"Thấy được: {coverage:F0}% chiều cao map", _labelStyle);
            GUI.color = Color.white;
        }

        // ── Dev mode ──
        GUILayout.Space(4f);
        GUI.color = devOn ? new Color(0.5f, 1f, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
        if (GUILayout.Button(devOn ? "DEV MODE: BẬT  (F2)" : "DEV MODE: TẮT  (F2)", _btnStyle, GUILayout.Height(22f)))
            _controller.SetDevMode(!devOn);
        GUI.color = Color.white;

        if (GUILayout.Button("Xem toàn bản đồ  (F1)", _btnStyle, GUILayout.Height(22f)))
            _controller.FitMapToView();

        // ── Preset zoom ──
        GUILayout.Space(4f);
        GUILayout.Label("Zoom preset:", _labelStyle);

        if (zoomPresets != null)
        {
            for (int i = 0; i < zoomPresets.Length; i++)
            {
                if (i % 3 == 0) GUILayout.BeginHorizontal();

                float preset      = zoomPresets[i];
                bool  outOfRange  = preset < _controller.ActiveMinSize || preset > _controller.ActiveMaxSize;
                bool  isCurrent   = Mathf.Abs(size - preset) < 1f;

                GUI.color = outOfRange ? new Color(1f, 1f, 1f, 0.35f)
                          : isCurrent  ? new Color(0.5f, 1f, 0.5f)
                                       : Color.white;

                if (GUILayout.Button($"{preset:F0}", _btnStyle, GUILayout.Height(20f)))
                    _controller.SetZoom(preset);

                GUI.color = Color.white;

                if (i % 3 == 2 || i == zoomPresets.Length - 1) GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndArea();
    }
}
