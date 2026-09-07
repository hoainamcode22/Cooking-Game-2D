using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime visual polish for Placement_Ghost.
/// Keeps all placement math in PlacementManager; this class only draws the map-footprint frame,
/// glow, and optional lift arrow effect.
/// </summary>
public class PlacementGhostVisualController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite tileSprite;

    [Header("Colors")]
    [SerializeField] private Color validFillColor = new Color(0.10f, 0.95f, 0.30f, 0.16f);
    [SerializeField] private Color validEdgeColor = new Color(0.13f, 1f, 0.34f, 0.96f);
    [SerializeField] private Color validEdgeDarkColor = new Color(0.00f, 0.48f, 0.08f, 0.78f);
    [SerializeField] private Color validEdgeHighlightColor = new Color(0.70f, 1f, 0.42f, 0.95f);
    [SerializeField] private Color invalidFillColor = new Color(1f, 0.08f, 0.08f, 0.18f);
    [SerializeField] private Color invalidEdgeColor = new Color(1f, 0.18f, 0.18f, 0.96f);
    [SerializeField] private Color invalidEdgeDarkColor = new Color(0.62f, 0f, 0f, 0.92f);
    [SerializeField] private Color invalidEdgeHighlightColor = new Color(1f, 0.42f, 0.35f, 0.9f);
    [SerializeField] private Color shadowColor = new Color(0f, 0.28f, 0f, 0.22f);
    [SerializeField] private Color arrowColor = new Color(1f, 0.84f, 0.12f, 1f);
    [SerializeField] private Color arrowRimColor = new Color(0.70f, 0.36f, 0.02f, 1f);
    [SerializeField] private Color arrowHighlightColor = new Color(1f, 0.98f, 0.5f, 1f);
    [SerializeField] private Color arrowShadowColor = new Color(0.45f, 0.23f, 0.02f, 0.42f);

    [Header("Custom Sprites — gắn art CỦA BẠN để ra i hệt mẫu (assets tự gắn)")]
    [Tooltip("Sprite GÓC VUÔNG XANH (corner bracket). Gắn để 4 góc dùng đúng art của bạn.")]
    [SerializeField] private Sprite cornerBracketSprite;
    [Tooltip("Chỉ hiện 4 góc vuông, ẩn các cạnh viền — gọn giống mẫu.")]
    [SerializeField] private bool cornerBracketsOnly = true;

    // ══════════════════════════════════════════════════════════════════════
    [Header("V6 — THANH XÁC NHẬN KIỂU TOWNSHIP")]

    [Tooltip("Màu nút HUỶ (✕). Township dùng ĐỎ.")]
    [SerializeField] private Color cancelButtonColor = new Color(0.93f, 0.26f, 0.24f, 1f);

    [Tooltip("Màu nút XOAY (↻). Township dùng XANH DƯƠNG — bản cũ của game này để CAM.")]
    [SerializeField] private Color rotateButtonColor = new Color(0.20f, 0.52f, 0.95f, 1f);

    [Tooltip("Màu nút XÁC NHẬN (✓). Township dùng XANH LÁ.\n" +
             "Nút này tự XÁM khi không đặt được — do PlacementManager gán " +
             "btnConfirm.interactable, ColorTint của Button nhân vào màu này. Đừng sửa tay.")]
    [SerializeField] private Color confirmButtonColor = new Color(0.27f, 0.78f, 0.30f, 1f);

    [Tooltip("Màu nút XOÁ (🗑). Đỏ SẪM — cố tình khác đỏ tươi của nút Huỷ để mắt " +
             "phân biệt được ngay, vì hai nút này nằm cạnh nhau mà hậu quả khác hẳn nhau:\n" +
             "Huỷ = trả công trình về chỗ cũ · Xoá = mất hẳn công trình.")]
    [SerializeField] private Color deleteButtonColor = new Color(0.62f, 0.13f, 0.16f, 1f);

    [Tooltip("Chữ khi ĐANG DI CHUYỂN vật đã có trên map — không mất tiền.\n" +
             "Township: 'KOSTENLOS PLATZIEREN'. Người chơi biết ngay lần này không bị trừ.")]
    [SerializeField] private string freeMoveLabel = "ĐẶT MIỄN PHÍ";

    [Tooltip("Chữ khi ĐẶT MỚI (mất tiền). Icon xu/kim cương + số hiện ngay sau chữ này.\n" +
             "Township: 'KAUFEN FÜR 🪙 30'.")]
    [SerializeField] private string buyLabel = "MUA VỚI GIÁ";

    // ══════════════════════════════════════════════════════════════════════
    [Header("V7 — 4 CHEVRON ÔM 4 GÓC VÙNG Ô")]

    [Tooltip("BẬT = dùng 4 chevron đặt theo PlacementManager.CurrentRect (đúng Township) và " +
             "TẮT 4 nêm cũ vốn suy từ bounds SPRITE.\n" +
             "VÌ SAO KHÁC NHAU: mái nhà nhô ra ngoài footprint, nên góc sprite ≠ góc vùng ô. " +
             "Người chơi cần thấy đúng vùng SẼ BỊ CHIẾM.")]
    [SerializeField] private bool useRectChevrons = true;

    [Tooltip("Cạnh của mỗi chevron, WORLD unit (1 ô lưới = 100). 46 ≈ nửa ô.")]
    [SerializeField] private float chevronWorldSize = 46f;

    [Tooltip("Chu kỳ nhấp nháy, giây. Township ≈ 1s.")]
    [SerializeField] private float chevronBlinkPeriod = 1f;

    [Tooltip("Đỉnh scale khi nhấp nháy. Township: 1.0 ↔ 1.08. Rất nhẹ là chủ ý.")]
    [SerializeField] private float chevronBlinkPeak = 1.08f;

    private Transform _frameRoot;
    private Transform _arrowRoot;
    private SpriteRenderer _fill;
    private SpriteRenderer[] _edges;
    private SpriteRenderer[] _edgeShadows;
    private SpriteRenderer[] _edgeHighlights;
    private SpriteRenderer[] _corners;
    private SpriteRenderer[] _arrowDots;
    private Sprite _diamondSprite;
    private Sprite _markerSprite;
    private Sprite _arrowSprite;
    private Sprite _circleSprite;
    private Sprite _bracketSprite;
    private Coroutine _arrowPulse;
    private Coroutine _framePulse;
    private Coroutine _spawnPop;
    private Coroutine _invalidPulse;
    private Vector3 _arrowBaseLocalPosition;
    private bool _lastValid = true;
    private bool _suppressFramePulse;
    private bool _isBuildingVisuals;
    private bool _built;                    // EnsureBuilt đã chạy xong ít nhất một lần

    // ── V6: thanh xác nhận ───────────────────────────────────────────────────
    private RectTransform   _barPanel;
    private Image           _barRowBg;      // nền xanh nhạt PlacementManager gắn lên Button_Row
    private TextMeshProUGUI _barLabel;
    private Image           _barCoin;
    private TextMeshProUGUI _barNumber;
    private bool            _barBuilt;
    private Button          _deleteButton;   // chỉ hiện khi đang SỬA vật có sẵn
    private string          _barLastLabel;
    private string          _barLastNumber;
    private bool            _barLastGem;
    private bool            _barLastMoney;
    private Button[]        _barButtons;    // 0 = ✕, 1 = ↻, 2 = ✓
    private Image[]         _barGlyphs;
    private bool[]          _barGlyphDim;

    // ── V7: 4 chevron theo vùng ô ────────────────────────────────────────────
    private Transform        _chevronRoot;
    private SpriteRenderer[] _chevrons;
    private Sprite           _chevronSprite;
    private readonly Vector3[] _chevronCorners = new Vector3[4];

    private const string VisualRootName = "Designed_Placement_Frame";
    private const string ArrowRootName = "Lift_Arrow_Effect";
    private const string ChevronRootName = "Rect_Chevrons";
    private const string PreferredSortingLayerName = "CongTrinh";
    private const string FallbackSortingLayerName = "Objects";
    private static string _resolvedSortingLayerName;
    private static string SortingLayerName
    {
        get
        {
            if (string.IsNullOrEmpty(_resolvedSortingLayerName))
                _resolvedSortingLayerName = ResolveSortingLayerName(PreferredSortingLayerName, FallbackSortingLayerName);
            return _resolvedSortingLayerName;
        }
    }
    public const int BaseOrder = 1600;
    public const int BuildingOrder = BaseOrder + 80;

    public void SetTileSprite(Sprite sprite)
    {
        if (sprite != null)
            tileSprite = sprite;
    }

    public void EnsureBuilt()
    {
        if (_isBuildingVisuals)
            return;

        EnsureRuntimeSprites();

        _isBuildingVisuals = true;

        _frameRoot = transform.Find(VisualRootName);
        if (_frameRoot == null)
        {
            GameObject root = new GameObject(VisualRootName);
            root.layer = gameObject.layer;
            _frameRoot = root.transform;
            _frameRoot.SetParent(transform, false);
            _frameRoot.localPosition = Vector3.zero;
            _frameRoot.localRotation = Quaternion.identity;
            _frameRoot.localScale = Vector3.one;
        }

        _fill = CreateOrGetRenderer(_frameRoot, "Tile_Fill", _diamondSprite, BaseOrder + 1);
        _fill.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        SpriteRenderer shadow = CreateOrGetRenderer(_frameRoot, "Soft_Shadow", _diamondSprite, BaseOrder);
        shadow.color = shadowColor;
        shadow.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        shadow.transform.localPosition = new Vector3(0f, -0.07f, 0f);

        _edgeShadows = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Top", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Right", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Bottom", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Left", _markerSprite, BaseOrder + 2)
        };

        _edges = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Top", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Right", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Bottom", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Left", _markerSprite, BaseOrder + 3)
        };

        _edgeHighlights = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Top", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Right", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Bottom", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Left", _markerSprite, BaseOrder + 4)
        };

        _corners = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Corner_Top", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Right", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Bottom", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Left", _diamondSprite, BaseOrder + 5)
        };

        // Góc vuông: nếu có gắn sprite tuỳ chỉnh thì dùng, KHÔNG thì dùng L-bracket VẼ BẰNG CODE.
        Sprite cornerSpr = cornerBracketSprite != null ? cornerBracketSprite : _bracketSprite;
        if (cornerSpr != null)
            foreach (var c in _corners) if (c != null) c.sprite = cornerSpr;

        // V7: 4 nêm cũ suy từ bounds SPRITE → tắt khi đã dùng chevron theo VÙNG Ô.
        ToggleArray(_corners, !useRectChevrons);

        BuildArrow();
        EnsureChevrons();
        EnsureConfirmBar();
        ApplyVisualState(_lastValid);

        if (_arrowRoot != null)
            _arrowRoot.gameObject.SetActive(false);

        if (_framePulse == null)
            _framePulse = StartCoroutine(PulseFrame());

        _isBuildingVisuals = false;
        _built = true;
    }

    /// <summary>
    /// Cập nhật mỗi frame: nội dung thanh xác nhận + vị trí 4 chevron.
    ///
    /// VÌ SAO PHẢI Ở Update() CHỨ KHÔNG DỰNG MỘT LẦN:
    ///   • `IsFreeMove` / giá / `CurrentRect` / `IsCurrentValid` là trạng thái ĐỘNG của
    ///     PlacementManager — `CurrentRect` được ghi lại MỖI FRAME trong Update() của nó.
    ///   • Bản cũ đọc giá đúng MỘT LẦN trong EnsureBuilt qua `ConstructionBridge.GetGhostItem()`,
    ///     mà EnsureBuilt (SetupGhostVisualController) chạy TRƯỚC khi Ghost được cấu hình
    ///     xong → có lượt bắt được null và dải giá không hiện. Đọc mỗi frame là hết cửa lỗi.
    ///   • Ba hàm dưới đều có cổng chặn "không đổi thì không ghi", nên không dirty canvas.
    /// </summary>
    private void Update()
    {
        if (!_built) return;

        EnsureConfirmBar();     // thử lại tới khi Button_Row + Canvas sẵn sàng
        RefreshConfirmBar();
        UpdateChevrons();
    }

    public void ConfigureFromFootprintScale(Vector3 footprintScale)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        ConfigureFromLocalSize(Mathf.Abs(footprintScale.x), Mathf.Abs(footprintScale.y));
    }

    public void ConfigureFromWorldSize(float worldWidth, float worldHeight)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        ConfigureFromLocalBounds(Vector3.zero, worldWidth / scaleX, worldHeight / scaleY);
    }

    public void ConfigureFromWorldBounds(Bounds worldBounds, float paddingMultiplier = 1.12f)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        if (worldBounds.size.x <= 0.01f || worldBounds.size.y <= 0.01f)
        {
            ConfigureFromWorldSize(1.5f, 1.0f);
            return;
        }

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        localCenter.z = 0f;
        ConfigureFromLocalBounds(
            localCenter,
            worldBounds.size.x * paddingMultiplier / scaleX,
            worldBounds.size.y * paddingMultiplier / scaleY);
    }

    private void ConfigureFromLocalSize(float localWidth, float localHeight)
    {
        ConfigureFromLocalBounds(Vector3.zero, localWidth, localHeight);
    }

    private void ConfigureFromLocalBounds(Vector3 localCenter, float localWidth, float localHeight)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        float width = Mathf.Max(1.35f, localWidth);
        float height = Mathf.Max(0.95f, localHeight);
        float edgeThickness = Mathf.Clamp(Mathf.Min(width, height) * 0.11f, 0.12f, 0.28f);

        _frameRoot.localPosition = localCenter;

        Transform shadow = _frameRoot.Find("Soft_Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(width * 0.82f, height * 0.78f, 1f);

        if (_fill != null)
            _fill.transform.localScale = new Vector3(width * 0.78f, height * 0.74f, 1f);

        float sideMarkerW = Mathf.Clamp(width * 0.44f, 0.55f, 1.45f);
        float sideMarkerH = Mathf.Clamp(height * 0.44f, 0.40f, 1.05f);
        float markerThickness = edgeThickness * 1.15f;
        Vector3[] markerPositions =
        {
            new(0f, height * 0.50f, 0f),
            new(width * 0.50f, 0f, 0f),
            new(0f, -height * 0.50f, 0f),
            new(-width * 0.50f, 0f, 0f)
        };

        Vector3[] markerScales =
        {
            new(sideMarkerW, markerThickness, 1f),
            new(sideMarkerH, markerThickness, 1f),
            new(sideMarkerW, markerThickness, 1f),
            new(sideMarkerH, markerThickness, 1f)
        };

        float[] markerRotations = { 0f, -90f, 180f, 90f };

        for (int i = 0; i < 4; i++)
        {
            SetMarker(_edgeShadows[i], markerPositions[i] + new Vector3(0.04f, -0.05f, 0f), markerScales[i] * 1.08f, markerRotations[i]);
            SetMarker(_edges[i], markerPositions[i], markerScales[i], markerRotations[i]);
            SetMarker(_edgeHighlights[i], markerPositions[i] + new Vector3(0f, markerThickness * 0.16f, 0f), new Vector3(markerScales[i].x * 0.82f, markerScales[i].y * 0.28f, 1f), markerRotations[i]);
        }

        // 4 GÓC VUÔNG ở 4 góc khung chữ nhật, xoay ôm vào trong (kiểu corner-bracket như mẫu).
        float cornerX = width * 0.44f;
        float cornerY = height * 0.47f;
        float cornerW = Mathf.Clamp(width * 0.30f, 0.46f, 1.25f);
        float cornerH = Mathf.Clamp(height * 0.22f, 0.18f, 0.52f);
        SetCornerMarker(_corners[0], new Vector3(-cornerX,  cornerY, 0f), cornerW, cornerH, -35f);
        SetCornerMarker(_corners[1], new Vector3( cornerX,  cornerY, 0f), cornerW, cornerH, -145f);
        SetCornerMarker(_corners[2], new Vector3(-cornerX, -cornerY, 0f), cornerW, cornerH,  35f);
        SetCornerMarker(_corners[3], new Vector3( cornerX, -cornerY, 0f), cornerW, cornerH, 145f);

        SetEdgesVisible(!cornerBracketsOnly);

        // Giữ 4 nêm cũ TẮT sau mỗi lần cấu hình lại (hàm này được gọi lại mỗi lần xoay).
        ToggleArray(_corners, !useRectChevrons);

        if (_arrowRoot != null)
        {
            _arrowBaseLocalPosition = localCenter + new Vector3(0f, height * 0.62f + 0.60f, 0f);
            _arrowRoot.localPosition = _arrowBaseLocalPosition;
        }
    }

    public void SetValid(bool valid)
    {
        EnsureBuilt();
        bool changed = _lastValid != valid;
        _lastValid = valid;
        ApplyVisualState(valid);

        if (changed && !valid && _invalidPulse == null)
            _invalidPulse = StartCoroutine(InvalidNudge());
    }

    private void ApplyVisualState(bool valid)
    {
        Color fill = valid ? validFillColor : invalidFillColor;
        Color edge = valid ? validEdgeColor : invalidEdgeColor;

        if (_fill != null)
            _fill.color = fill;

        if (_edges != null)
            foreach (SpriteRenderer sr in _edges)
                if (sr != null) sr.color = edge;

        Color dark = valid ? validEdgeDarkColor : invalidEdgeDarkColor;
        if (_edgeShadows != null)
            foreach (SpriteRenderer sr in _edgeShadows)
                if (sr != null) sr.color = dark;

        Color highlight = valid ? validEdgeHighlightColor : invalidEdgeHighlightColor;
        if (_edgeHighlights != null)
            foreach (SpriteRenderer sr in _edgeHighlights)
                if (sr != null) sr.color = highlight;

        if (_corners != null)
            foreach (SpriteRenderer sr in _corners)
                if (sr != null) sr.color = edge;
    }

    public void PlaySpawnPop(bool stronger)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        if (_spawnPop != null)
            StopCoroutine(_spawnPop);
        _spawnPop = StartCoroutine(SpawnPop(stronger));
    }

    public void ShowLiftArrow(bool show)
    {
        EnsureBuilt();
        if (_arrowRoot == null)
            return;

        _arrowRoot.gameObject.SetActive(show);
        if (show)
            _arrowRoot.localPosition = _arrowBaseLocalPosition;

        if (show && _arrowPulse == null)
            _arrowPulse = StartCoroutine(PulseArrow());
        else if (!show && _arrowPulse != null)
        {
            StopCoroutine(_arrowPulse);
            _arrowPulse = null;
            _arrowRoot.localScale = Vector3.one;
            _arrowRoot.localPosition = _arrowBaseLocalPosition;
        }
    }

    private void BuildArrow()
    {
        _arrowRoot = transform.Find(ArrowRootName);
        if (_arrowRoot == null)
        {
            GameObject root = new GameObject(ArrowRootName);
            root.layer = gameObject.layer;
            _arrowRoot = root.transform;
            _arrowRoot.SetParent(transform, false);
        }

        SpriteRenderer shadow = CreateOrGetRenderer(_arrowRoot, "Arrow_Shadow", _arrowSprite, BaseOrder + 7);
        shadow.color = arrowShadowColor;
        shadow.transform.localPosition = new Vector3(0.04f, -0.05f, 0f);
        shadow.transform.localRotation = Quaternion.identity;
        shadow.transform.localScale = new Vector3(1.26f, 1.10f, 1f);

        SpriteRenderer rim = CreateOrGetRenderer(_arrowRoot, "Arrow_Rim", _arrowSprite, BaseOrder + 8);
        rim.color = arrowRimColor;
        rim.transform.localRotation = Quaternion.identity;
        rim.transform.localScale = new Vector3(1.16f, 1.05f, 1f);

        SpriteRenderer head = CreateOrGetRenderer(_arrowRoot, "Arrow_Head", _arrowSprite, BaseOrder + 9);
        head.color = arrowColor;
        head.transform.localRotation = Quaternion.identity;
        head.transform.localScale = new Vector3(0.98f, 0.86f, 1f);

        SpriteRenderer shine = CreateOrGetRenderer(_arrowRoot, "Arrow_Highlight", _markerSprite, BaseOrder + 10);
        shine.color = arrowHighlightColor;
        shine.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        shine.transform.localRotation = Quaternion.identity;
        shine.transform.localScale = new Vector3(0.56f, 0.10f, 1f);

        _arrowDots = new SpriteRenderer[4];
        for (int i = 0; i < _arrowDots.Length; i++)
        {
            SpriteRenderer dot = CreateOrGetRenderer(_arrowRoot, $"Arrow_Dot_{i + 1}", _circleSprite, BaseOrder + 8);
            dot.color = new Color(1f, 0.72f, 0.04f, 0.95f - i * 0.12f);
            dot.transform.localPosition = new Vector3(0f, -0.46f - i * 0.34f, 0f);
            dot.transform.localRotation = Quaternion.identity;
            dot.transform.localScale = new Vector3(0.26f - i * 0.02f, 0.26f - i * 0.02f, 1f);
            _arrowDots[i] = dot;
        }
    }

    private IEnumerator PulseFrame()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float wave = (Mathf.Sin(t * 4.2f) + 1f) * 0.5f;
            float scale = 1f + wave * 0.035f;

            if (_frameRoot != null && !_suppressFramePulse)
                _frameRoot.localScale = new Vector3(scale, scale, 1f);

            Color fill = _lastValid ? validFillColor : invalidFillColor;
            fill.a *= Mathf.Lerp(0.72f, 1.25f, wave);
            if (_fill != null)
                _fill.color = fill;

            Color edge = _lastValid ? validEdgeColor : invalidEdgeColor;
            edge.a *= Mathf.Lerp(0.82f, 1f, wave);

            if (_edges != null)
                foreach (SpriteRenderer sr in _edges)
                    if (sr != null) sr.color = edge;

            Color dark = _lastValid ? validEdgeDarkColor : invalidEdgeDarkColor;
            dark.a *= Mathf.Lerp(0.78f, 1f, wave);
            if (_edgeShadows != null)
                foreach (SpriteRenderer sr in _edgeShadows)
                    if (sr != null) sr.color = dark;

            Color highlight = _lastValid ? validEdgeHighlightColor : invalidEdgeHighlightColor;
            highlight.a *= Mathf.Lerp(0.45f, 0.92f, wave);
            if (_edgeHighlights != null)
                foreach (SpriteRenderer sr in _edgeHighlights)
                    if (sr != null) sr.color = highlight;

            if (_corners != null)
                foreach (SpriteRenderer sr in _corners)
                    if (sr != null)
                    {
                        Color c = edge;
                        c.a *= Mathf.Lerp(0.78f, 1f, wave);
                        sr.color = c;
                    }

            yield return null;
        }
    }

    private IEnumerator SpawnPop(bool stronger)
    {
        float start = stronger ? 0.55f : 0.72f;
        float overshoot = stronger ? 1.18f : 1.10f;

        _suppressFramePulse = true;
        yield return ScaleFrame(start, overshoot, 0.12f);
        yield return ScaleFrame(overshoot, 1f, 0.14f);
        _suppressFramePulse = false;
        _spawnPop = null;
    }

    private IEnumerator ScaleFrame(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float scale = Mathf.LerpUnclamped(from, to, eased);
            if (_frameRoot != null)
                _frameRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private IEnumerator PulseArrow()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float bob = Mathf.Sin(t * 5.4f);
            float shine = (Mathf.Sin(t * 8f) + 1f) * 0.5f;

            if (_arrowRoot != null)
            {
                _arrowRoot.localScale = Vector3.one * (1f + bob * 0.055f);
                _arrowRoot.localPosition = _arrowBaseLocalPosition + new Vector3(0f, bob * 0.16f, 0f);
            }

            if (_arrowDots != null)
            {
                for (int i = 0; i < _arrowDots.Length; i++)
                {
                    SpriteRenderer dot = _arrowDots[i];
                    if (dot == null) continue;

                    float phase = Mathf.Repeat(shine + i * 0.18f, 1f);
                    Color c = dot.color;
                    c.a = Mathf.Lerp(0.35f, 0.95f, 1f - phase);
                    dot.color = c;
                    float s = (0.26f - i * 0.02f) * Mathf.Lerp(0.85f, 1.18f, phase);
                    dot.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            yield return null;
        }
    }

    private IEnumerator InvalidNudge()
    {
        if (_frameRoot == null)
        {
            _invalidPulse = null;
            yield break;
        }

        Vector3 basePos = _frameRoot.localPosition;
        Vector3 baseScale = _frameRoot.localScale;
        bool wasSuppressingFramePulse = _suppressFramePulse;
        _suppressFramePulse = true;
        const float duration = 0.16f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t);
            _frameRoot.localPosition = basePos + new Vector3(wave * 0.08f, 0f, 0f);
            _frameRoot.localScale = baseScale * (1f + (1f - t) * 0.04f);
            yield return null;
        }

        _frameRoot.localPosition = basePos;
        _frameRoot.localScale = baseScale;
        _suppressFramePulse = wasSuppressingFramePulse;
        _invalidPulse = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // V6 — THANH XÁC NHẬN KIỂU TOWNSHIP
    //
    //   ┌────────────────────────────┐
    //   │   MUA VỚI GIÁ  🪙 30       │  ← nền tối bo góc, GIÁ Ở HÀNG TRÊN
    //   │    (✕)     (↻)     (✓)     │  ← 3 nút TRÒN, thứ tự huỷ · xoay · xác nhận
    //   └────────────────────────────┘
    //
    // VÌ SAO XÁC NHẬN Ở BÊN PHẢI: thuận tay phải, và quan trọng hơn — nó ĐỨNG XA nút huỷ
    // nhất. Bản cũ để ✓ ngay cạnh ✕ nên bấm trượt là mất luôn lượt đặt.
    // ══════════════════════════════════════════════════════════════════════

    private const string ConfirmPanelName = "Confirm_Bar_Panel";

    // ĐƠN VỊ: canvas `Placement_UI` có localScale 0.01 nằm dưới root scale 100 ⇒ tích = 1,
    // nên 1 "pixel" UI ở đây đúng bằng 1 world unit. Hàng nút cao 126, tâm ở y = 0
    // (xem PlacementManager.StyleGhostActionBar) ⇒ nút chiếm y ∈ [−63, +63].
    private const float PanelMinWidth  = 438f;   // 3 nút 120 + 2 khe 20 + lề 19 mỗi bên
    private const float PanelHeight    = 218f;   // bọc từ dưới nút tới trên hàng giá
    private const float PanelCenterY   = 35f;    // tâm khối (nút + hàng giá) so với Button_Row
    private const float PriceRowY      = 100f;   // hàng giá nằm sát TRÊN hàng nút
    private const float PriceRowHeight = 56f;
    private const float CoinIconSize   = 44f;
    private const float PriceGap       = 12f;
    private const float GlyphSize      = 62f;    // ✕ ↻ ✓ trong nút 120

    /// <summary>
    /// Dựng NỀN TỐI BO GÓC bọc cả cụm + đổi 3 nút vuông thành 3 nút TRÒN đúng thứ tự.
    ///
    /// DỰNG BẰNG CODE, KHÔNG SỬA PREFAB: prefab `Placement_Ghost` là file YAML dùng chung
    /// với DEV-1 (hàng nút, canvas, footprint đều nằm trong đó) — thêm node bằng tay dễ
    /// đụng độ merge, mà Edric cũng không phải mở prefab chỉnh gì. Chạy runtime thì mọi
    /// công trình đều tự có thanh xác nhận, kể cả prefab ghost sau này bị thay.
    ///
    /// NỀN LÀM CON CỦA `Button_Row` (không phải em ruột của nó) vì hai lý do:
    ///   1. `PlacementManager.AnimateGhostActionBar` scale Button_Row lúc bật lên
    ///      (0.45 → 1.08 → 1). Là CON thì nền pop CÙNG hàng nút; là em ruột thì nút nảy mà
    ///      nền đứng yên, đọc ra hai lớp rời nhau.
    ///   2. Đặt ở SIBLING INDEX 0 thì UGUI vẽ nó TRƯỚC → nằm dưới 3 nút, khỏi phải đụng vào
    ///      sortingOrder của canvas (thứ DEV-1 đang gán trong ConfigureGhostCanvas).
    /// Bắt buộc có `LayoutElement.ignoreLayout = true`, nếu không HorizontalLayoutGroup của
    /// Button_Row coi nền là "nút thứ 4" và xếp nó vào hàng.
    ///
    /// Gọi lại được nhiều lần: có cổng `_barBuilt`, và nếu Button_Row chưa tồn tại thì thoát
    /// im lặng để Update() thử lại frame sau.
    /// </summary>
    private void EnsureConfirmBar()
    {
        if (_barBuilt) return;

        Transform row = FindChildDeep(transform, "Button_Row");
        if (row == null) return;                       // prefab chưa dựng xong → thử lại sau

        RectTransform rowRect = row as RectTransform;
        if (rowRect == null) return;

        // Nền xanh nhạt do PlacementManager.StyleGhostActionBar gắn lên chính Button_Row.
        // Giữ tham chiếu để RefreshConfirmBar() ép nó trong suốt (xem ghi chú ở đó).
        _barRowBg = row.GetComponent<Image>();

        ConstructionArtKit kit = ConstructionManager.Instance != null
                               ? ConstructionManager.Instance.ArtKit : null;

        // ── 1. NỀN TỐI BO GÓC ────────────────────────────────────────────────
        Transform old = row.Find(ConfirmPanelName);
        if (old != null) Destroy(old.gameObject);

        var panelGo = new GameObject(ConfirmPanelName, typeof(RectTransform));
        panelGo.layer = row.gameObject.layer;
        _barPanel = (RectTransform)panelGo.transform;
        _barPanel.SetParent(rowRect, false);
        _barPanel.anchorMin        = new Vector2(0.5f, 0.5f);
        _barPanel.anchorMax        = new Vector2(0.5f, 0.5f);
        _barPanel.pivot            = new Vector2(0.5f, 0.5f);
        _barPanel.anchoredPosition = new Vector2(0f, PanelCenterY);
        _barPanel.sizeDelta        = new Vector2(PanelMinWidth, PanelHeight);
        _barPanel.SetAsFirstSibling();

        var ignore = panelGo.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;

        // Ô art `PriceBarBg`: chưa gán thì dùng panel vẽ bằng code, tô MÀU NHẬN DẠNG
        // (đen) để Edric biết chỗ này thả nền thanh giá vào.
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.PriceBarBg,
                                       ConstructionSpriteFactory.Panel(96, 96, 28),
                                       out Sprite barSpr, out Color barCol);

        var barBg = panelGo.AddComponent<Image>();
        barBg.sprite        = barSpr;
        barBg.type          = Image.Type.Sliced;
        barBg.color         = barCol;
        barBg.raycastTarget = false;    // KHÔNG chặn tia chuột tới 3 nút nằm trên nó

        var shadow = panelGo.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.34f);
        shadow.effectDistance = new Vector2(0f, -6f);

        if (ConstructionArtKit.WantLabels(kit))
            ConstructionSiteVisuals.AttachSlotLabel(_barPanel,
                                                    ConstructionArtKit.Slot.PriceBarBg, kit);

        // ── 2. HÀNG GIÁ (chữ + icon tiền + số) ───────────────────────────────
        _barLabel  = MakeBarText(_barPanel, "Text_Nhan", buyLabel, 38f);
        _barCoin   = MakeBarIcon(_barPanel, "Icon_Tien");
        _barNumber = MakeBarText(_barPanel, "Text_Gia", "0", 40f);

        // ── 3. BA NÚT TRÒN, THỨ TỰ ✕ → ↻ → ✓ ────────────────────────────────
        // Nền đang ở index 0 nên nút bắt đầu từ 1. Layout group bỏ qua nền (ignoreLayout)
        // và chỉ xếp 3 nút theo thứ tự tương đối 1 < 2 < 3.
        // Nút XOÁ nằm ở NGOÀI CÙNG BÊN TRÁI — xa nút ✓ nhất có thể.
        // Đây là hành động phá hoại, đặt cạnh xác nhận là mời tai nạn.
        EnsureDeleteButton(row);

        StyleRoundButton(row, "Btn_Delete",  deleteButtonColor,  1,
                         ConstructionSpriteFactory.TrashCan());
        StyleRoundButton(row, "Btn_Cancel",  cancelButtonColor,  2,
                         ConstructionSpriteFactory.CrossMark());
        StyleRoundButton(row, "Btn_Rotate",  rotateButtonColor,  3,
                         ConstructionSpriteFactory.RotateArrow());
        StyleRoundButton(row, "Btn_Confirm", confirmButtonColor, 4,
                         ConstructionSpriteFactory.CheckMark());

        // Ghi nhớ cặp (Button, glyph) để RefreshConfirmBar() làm mờ glyph khi nút bị disable.
        _barButtons  = new Button[3];
        _barGlyphs   = new Image[3];
        _barGlyphDim = new bool[3];
        CacheGlyphPair(row, "Btn_Cancel",  0);
        CacheGlyphPair(row, "Btn_Rotate",  1);
        CacheGlyphPair(row, "Btn_Confirm", 2);

        _barBuilt = true;

        // Ép vẽ nội dung ngay frame này, khỏi nháy một frame với chữ mặc định "MUA VỚI GIÁ 0".
        _barLastLabel = null;
        RefreshConfirmBar();
    }

    /// <summary>
    /// Đọc trạng thái ĐỘNG của PlacementManager rồi cập nhật hàng giá.
    ///
    /// HỢP ĐỒNG API §4 (DEV-1 đã chốt): `IsFreeMove`, `CurrentPriceGold`, `CurrentPriceGem`.
    /// Hai giá TỰ TRẢ 0 khi đang di chuyển vật có sẵn, nên chỉ cần một nhánh `IsFreeMove`.
    /// KHÔNG còn dùng `ConstructionBridge.GetGhostItem()` (reflection) — DEV-1 đã mở
    /// property công khai, đọc thẳng vừa nhanh vừa không vỡ khi họ đổi tên field private.
    /// </summary>
    private void RefreshConfirmBar()
    {
        if (!_barBuilt || _barPanel == null) return;

        // Nền xanh nhạt mà `StyleGhostActionBar` gắn lên Button_Row chạy SAU EnsureBuilt
        // (nó nằm trong AnimateGhostActionBar, được StartCoroutine ở CUỐI
        // StartPlacingNewObject / StartEditBuilding) → không thể xử lý một lần lúc dựng.
        //
        // ⚠ Nó còn có thể ADD Image lên Button_Row nếu prefab chưa có, tức lúc EnsureConfirmBar
        // chạy thì GetComponent<Image> trả null. Vì vậy phải tra LẠI ở đây tới khi thấy —
        // nếu không, dải xanh nhạt sẽ hắt lên qua nền tối (nền chỉ đục 88 %).
        if (_barRowBg == null && _barPanel.parent != null)
            _barRowBg = _barPanel.parent.GetComponent<Image>();

        // Ép trong suốt; có cổng chặn nên thực tế chỉ ghi ĐÚNG MỘT LẦN, không dirty canvas
        // mỗi frame.
        if (_barRowBg != null && _barRowBg.color.a > 0.002f)
            _barRowBg.color = new Color(1f, 1f, 1f, 0f);

        // ── NÚT XOÁ: CHỈ hiện khi đang SỬA vật đã có trên map ───────────────
        // Lúc mua mới từ shop thì "xoá" vô nghĩa — nút ✕ đã hoàn tiền và bỏ đi rồi.
        // Hiện thêm nút xoá ở đó chỉ làm người chơi bấm nhầm và mất tiền.
        if (_deleteButton != null)
        {
            bool nenHien = PlacementManager.Instance != null
                        && PlacementManager.Instance.IsEditingBuilding;
            if (_deleteButton.gameObject.activeSelf != nenHien)
                _deleteButton.gameObject.SetActive(nenHien);
        }

        // ── LÀM MỜ GLYPH THEO TRẠNG THÁI NÚT ────────────────────────────────
        // Unity ColorTint chỉ tô lại `targetGraphic` (Image CỦA NÚT), KHÔNG chạm tới graphic
        // con. Nếu không tự đồng bộ thì nút ✓ bị disable sẽ thành "đĩa xanh mờ 50 % + dấu
        // tick TRẮNG CHÓI" — mắt đọc ra UI lỗi, chứ không phải "chưa bấm được".
        // Chỉ nút ✓ thực sự bị disable (PlacementManager gán mỗi frame) nhưng làm cho cả 3
        // để sau này thêm điều kiện cho ↻ / ✕ là tự đúng.
        if (_barButtons != null)
        {
            for (int i = 0; i < _barButtons.Length; i++)
            {
                Button b = _barButtons[i];
                Image  g = _barGlyphs[i];
                if (b == null || g == null) continue;

                bool dim = !b.interactable;
                if (dim == _barGlyphDim[i]) continue;   // không đổi → khỏi dirty canvas

                _barGlyphDim[i] = dim;
                g.color = dim ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            }
        }

        PlacementManager pm = PlacementManager.Instance;
        bool free = pm == null || pm.IsFreeMove;
        int  gold = pm != null ? pm.CurrentPriceGold : 0;
        int  gem  = pm != null ? pm.CurrentPriceGem  : 0;

        bool useGem = gold <= 0 && gem > 0;
        int  price  = useGem ? gem : gold;

        // Vật giá 0 (decor tặng, ô đất mở sẵn) cũng hiện "ĐẶT MIỄN PHÍ". Hiện "MUA VỚI GIÁ"
        // rồi để trống số thì người chơi tưởng UI lỗi.
        bool   showMoney = !free && price > 0;
        string label     = showMoney ? buyLabel : freeMoveLabel;
        string number    = showMoney ? price.ToString() : string.Empty;

        // Không đổi gì thì thoát: TMP dựng lại mesh mỗi lần gán text, 60 fps là tốn vô ích.
        if (label == _barLastLabel && number == _barLastNumber &&
            useGem == _barLastGem && showMoney == _barLastMoney)
            return;

        _barLastLabel  = label;
        _barLastNumber = number;
        _barLastGem    = useGem;
        _barLastMoney  = showMoney;

        LayoutPriceRow(label, number, showMoney, useGem);
    }

    /// <summary>
    /// Xếp chữ + icon tiền + số THỦ CÔNG rồi co nền cho vừa.
    ///
    /// VÌ SAO KHÔNG DÙNG HorizontalLayoutGroup + ContentSizeFitter (bản cũ dùng):
    /// ContentSizeFitter ghi `sizeDelta` trong `SetLayoutHorizontal`, còn layout group cha
    /// đọc `sizeDelta` trong `CalculateLayoutInputHorizontal` — hai bước này thuộc HAI PHA
    /// khác nhau của LayoutRebuilder (pha tính chạy từ con lên, pha áp chạy từ cha xuống),
    /// nên bề rộng nền luôn CHẬM MỘT FRAME so với chữ. Đổi giữa "ĐẶT MIỄN PHÍ" và
    /// "MUA VỚI GIÁ 30" là thấy nền giật một nhịp. Tự tính thì đúng ngay trong frame đó.
    ///
    /// `TMP_Text.preferredWidth` đo bằng chiều rộng vô hạn nên KHÔNG phụ thuộc sizeDelta
    /// hiện tại — đọc trước khi đặt kích thước là an toàn.
    /// </summary>
    private void LayoutPriceRow(string label, string number, bool showMoney, bool useGem)
    {
        if (_barLabel == null) return;

        _barLabel.text = label;
        float labelW = Mathf.Max(1f, _barLabel.preferredWidth);
        _barLabel.rectTransform.sizeDelta = new Vector2(labelW + 8f, PriceRowHeight);

        float numberW = 0f;
        if (_barNumber != null)
        {
            _barNumber.gameObject.SetActive(showMoney);
            if (showMoney)
            {
                _barNumber.text = number;
                numberW = Mathf.Max(1f, _barNumber.preferredWidth);
                _barNumber.rectTransform.sizeDelta = new Vector2(numberW + 8f, PriceRowHeight);
            }
        }

        if (_barCoin != null)
        {
            _barCoin.gameObject.SetActive(showMoney);
            if (showMoney)
                _barCoin.sprite = useGem
                    ? ConstructionSpriteFactory.GemIcon()
                    : ConstructionSpriteFactory.CoinIcon();
        }

        float total = labelW
                    + (showMoney ? PriceGap + CoinIconSize + PriceGap * 0.6f + numberW : 0f);
        float x = -total * 0.5f;

        _barLabel.rectTransform.anchoredPosition = new Vector2(x + labelW * 0.5f, PriceRowY);
        x += labelW;

        if (showMoney)
        {
            x += PriceGap;
            if (_barCoin != null)
                _barCoin.rectTransform.anchoredPosition =
                    new Vector2(x + CoinIconSize * 0.5f, PriceRowY);

            x += CoinIconSize + PriceGap * 0.6f;
            if (_barNumber != null)
                _barNumber.rectTransform.anchoredPosition =
                    new Vector2(x + numberW * 0.5f, PriceRowY);
        }

        // Nền phải bọc được hàng chữ dài nhất — tiếng Việt dài hơn tiếng Đức của ảnh mẫu.
        _barPanel.sizeDelta = new Vector2(Mathf.Max(PanelMinWidth, total + 56f), PanelHeight);
    }

    /// <summary>
    /// Biến một nút VUÔNG của prefab thành nút TRÒN kiểu Township + đặt lại thứ tự.
    ///
    /// VÌ SAO GLYPH LÀ SPRITE CHỨ KHÔNG PHẢI KÝ TỰ: prefab có sẵn 3 node "Label" chứa ký tự
    /// Unicode nhưng cả 3 đang TẮT (m_IsActive: 0). Bật lên là đánh cược vào việc font TMP
    /// mặc định có đủ ✕ ↻ ✓ — thiếu một cái là hiện ô vuông trống. Sprite thủ tục chắc chắn
    /// hiện, và đi cùng đường với 23 ô art khác (Edric thay sprite thật sau).
    ///
    /// KHÔNG ĐỤNG `Button.interactable` hay `Button.colors`: PlacementManager gán
    /// `btnConfirm.interactable = isValidPos` MỖI FRAME, và ColorTint của Button NHÂN vào
    /// `Image.color` (qua CanvasRenderer) chứ không ghi đè nó — nên ✓ tự xám khi không đặt
    /// được. Đó là thứ đang chạy đúng, giữ nguyên.
    /// </summary>
    /// <summary>
    /// Tạo nút XOÁ nếu prefab chưa có. Prefab `Placement_Ghost` chỉ có 3 nút
    /// (Cancel / Rotate / Confirm) nên phải nhân bản một nút sẵn có — cách đó bảo đảm
    /// copy đúng mọi component (Button, Image, UIJuiceFeedback, LayoutElement) mà
    /// không phải dựng lại từ đầu và đoán prefab đang gắn những gì.
    ///
    /// TỰ NỐI onClick tại đây, KHÔNG dựa vào `PlacementManager.BindGhostButtons`:
    /// hàm đó chạy trong `StartEditBuilding` — có thể TRƯỚC khi `EnsureConfirmBar`
    /// tạo ra nút này. Nối tay ở đây là hết cửa đua tranh (race condition).
    /// </summary>
    private void EnsureDeleteButton(Transform row)
    {
        // BẢN PHÁT HÀNH: KHÔNG tạo nút xoá.
        // Đây là công cụ cho dev dọn map lúc test. Người chơi thật KHÔNG được có nó —
        // xoá công trình là hành động mất tiền không hoàn lại, và Township cũng không
        // cho xoá trực tiếp (chỉ "cất vào kho"). Muốn cho người chơi cất công trình thì
        // làm cơ chế riêng, đừng mở nút này ra.
        //
        // Bọc CẢ THÂN HÀM thay vì `return;` sớm: nếu dùng early-return thì ở bản release
        // mọi dòng phía sau thành unreachable → warning CS0162.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Đã có sẵn (prefab, hoặc lần dựng trước) → chỉ cần nắm lại tham chiếu.
        // KHÔNG return trắng: nếu bỏ trống `_deleteButton` thì khối ẩn/hiện trong
        // RefreshConfirmBar() không chạy, nút xoá sẽ hiện cả lúc MUA MỚI.
        Transform coSan = row.Find("Btn_Delete");
        if (coSan != null)
        {
            _deleteButton = coSan.GetComponent<Button>();
            return;
        }

        Transform src = row.Find("Btn_Cancel");
        if (src == null) return;                      // không có gì để nhân bản

        var clone = Instantiate(src.gameObject, row);
        clone.name = "Btn_Delete";

        // Nút gốc có thể đang bị PlacementManager gán listener CancelPlacement —
        // xoá sạch rồi nối lại đúng việc của nó.
        var btn = clone.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var pm = PlacementManager.Instance;
                if (pm != null && pm.IsEditingBuilding) pm.DeleteEditingBuilding();
            });
        }

        _deleteButton = btn;
#endif
    }

    private void StyleRoundButton(Transform row, string name, Color color,
                                  int siblingIndex, Sprite glyph)
    {
        Transform t = row.Find(name);
        if (t == null) return;

        t.SetSiblingIndex(siblingIndex);

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            // Circle() của ConstructionSpriteFactory có khử răng cưa; CreateCircleSprite
            // trong file này thì cắt cứng theo bán kính → viền nút 120 px sẽ răng cưa.
            img.sprite         = ConstructionSpriteFactory.Circle(96);
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            img.color          = color;
        }

        if (glyph == null) return;

        Transform found = t.Find("Glyph");
        GameObject go = found != null
            ? found.gameObject
            : new GameObject("Glyph", typeof(RectTransform));
        go.layer = t.gameObject.layer;

        var grt = (RectTransform)go.transform;
        grt.SetParent(t, false);
        grt.anchorMin        = new Vector2(0.5f, 0.5f);
        grt.anchorMax        = new Vector2(0.5f, 0.5f);
        grt.pivot            = new Vector2(0.5f, 0.5f);
        grt.anchoredPosition = Vector2.zero;
        grt.sizeDelta        = new Vector2(GlyphSize, GlyphSize);

        Image gi = go.GetComponent<Image>();
        if (gi == null) gi = go.AddComponent<Image>();
        gi.sprite         = glyph;
        gi.color          = Color.white;
        gi.preserveAspect = true;
        gi.raycastTarget  = false;   // để click luôn rơi vào Button ở lớp cha
    }

    private void CacheGlyphPair(Transform row, string buttonName, int index)
    {
        Transform t = row.Find(buttonName);
        if (t == null) return;

        _barButtons[index] = t.GetComponent<Button>();

        Transform g = t.Find("Glyph");
        if (g != null) _barGlyphs[index] = g.GetComponent<Image>();
    }

    private static TextMeshProUGUI MakeBarText(RectTransform parent, string name,
                                               string content, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, PriceRowHeight);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text          = content;
        tmp.fontSize      = size;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.overflowMode  = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        // Viền đậm giống nhãn Township (cùng cách với LevelUpPopupTownshipTool.AddTextOutline)
        Material mat = tmp.fontMaterial;
        if (mat != null) mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        tmp.outlineColor = new Color(0.07f, 0.05f, 0.02f, 1f);
        tmp.outlineWidth = 0.26f;
        tmp.UpdateMeshPadding();

        return tmp;
    }

    private static Image MakeBarIcon(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CoinIconSize, CoinIconSize);

        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget  = false;
        return img;
    }

    /// <summary>Tìm con theo tên ở MỌI độ sâu (Transform.Find chỉ tìm con trực tiếp).</summary>
    private static Transform FindChildDeep(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;

            Transform found = FindChildDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // V7 — 4 CHEVRON ÔM 4 GÓC VÙNG Ô
    //
    // Township đặt 4 chevron ở 4 góc VÙNG Ô, không phải 4 góc SPRITE. Hai thứ này khác
    // nhau vì mái nhà nhô ra ngoài footprint (DEV-1 §5.1: "sprite vươn cao hơn vùng ô là
    // bình thường"). Người chơi cần thấy đúng vùng SẼ BỊ CHIẾM, nếu không họ tưởng công
    // trình ăn nhiều đất hơn thực tế và không dám xếp sát nhau.
    // ══════════════════════════════════════════════════════════════════════

    private void EnsureChevrons()
    {
        if (!useRectChevrons) return;
        if (_chevrons != null && _chevronRoot != null) return;

        Transform existing = transform.Find(ChevronRootName);
        if (existing != null)
        {
            _chevronRoot = existing;
        }
        else
        {
            var go = new GameObject(ChevronRootName);
            go.layer = gameObject.layer;
            _chevronRoot = go.transform;
            _chevronRoot.SetParent(transform, false);
        }

        _chevronRoot.localPosition = Vector3.zero;
        _chevronRoot.localRotation = Quaternion.identity;

        // CHUẨN HOÁ SCALE: root Ghost có scale 100 (quy ước "1 unit sprite = 1 ô" của dự án).
        // Chia ngược để BÊN TRONG _chevronRoot, 1 đơn vị = 1 WORLD unit. Nhờ vậy
        // `chevronWorldSize` đọc thẳng ra world unit và InverseTransformPoint(gócWorld) cho
        // ra đúng offset — không phải rải phép chia 100 khắp nơi.
        float sx = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        _chevronRoot.localScale = new Vector3(1f / sx, 1f / sy, 1f);

        _chevrons = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
            _chevrons[i] = CreateOrGetRenderer(_chevronRoot, $"Chevron_{i}",
                                               _chevronSprite, BaseOrder + 6);
    }

    private void UpdateChevrons()
    {
        if (_chevrons == null || _chevronRoot == null) return;

        PlacementManager pm = PlacementManager.Instance;
        RectInt rect = pm != null ? pm.CurrentRect : new RectInt(0, 0, 0, 0);

        // HỢP ĐỒNG API §4: width == 0 nghĩa là KHÔNG có Ghost nào đang hoạt động → ẩn hết.
        if (rect.width <= 0 || rect.height <= 0)
        {
            ToggleArray(_chevrons, false);
            return;
        }

        // Lấy 4 góc bằng HÀM CỦA DEV-1, KHÔNG tự nhân CELL: họ vừa đổi hệ neo sang mép dưới
        // vùng ô (V8), tự tính lại là mời lỗi "lệch nửa ô" quay về đúng chỗ vừa sửa xong.
        _chevronCorners[0] = PlacementManager.CellCornerToWorld(rect.xMin, rect.yMin); // dưới-trái
        _chevronCorners[1] = PlacementManager.CellCornerToWorld(rect.xMax, rect.yMin); // dưới-phải
        _chevronCorners[2] = PlacementManager.CellCornerToWorld(rect.xMax, rect.yMax); // trên-phải
        _chevronCorners[3] = PlacementManager.CellCornerToWorld(rect.xMin, rect.yMax); // trên-trái

        // Nhấp nháy scale 1.0 ↔ 1.08, chu kỳ ~1 s (thông số V7).
        float k = Mathf.LerpUnclamped(1f, chevronBlinkPeak,
                      FxEase.Sin01(Time.time / Mathf.Max(0.05f, chevronBlinkPeriod)));
        float size = chevronWorldSize * k;

        // Xanh khi đặt được, ĐỎ khi chồng lấn / ra ngoài biên. `_lastValid` do
        // PlacementManager.SetValid() cấp mỗi frame nên luôn khớp với nút ✓ bị xám.
        Color c = _lastValid ? validEdgeColor : invalidEdgeColor;

        for (int i = 0; i < 4; i++)
        {
            SpriteRenderer sr = _chevrons[i];
            if (sr == null) continue;

            sr.enabled = true;
            if (sr.sprite == null) sr.sprite = _chevronSprite;

            Vector3 local = _chevronRoot.InverseTransformPoint(_chevronCorners[i]);
            local.z = 0f;
            sr.transform.localPosition = local;

            // Sprite chevron có PIVOT ĐÚNG TẠI GÓC và hai cánh vươn theo +X/+Y, nên xoay
            // đúng i·90° là ôm sang góc kế tiếp — khỏi phải tính offset riêng cho từng góc.
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            sr.transform.localScale    = new Vector3(size, size, 1f);
            sr.color = c;
        }
    }

    private SpriteRenderer CreateOrGetRenderer(Transform parent, string name, Sprite sprite, int order)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.sortingLayerName = SortingLayerName;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Simple;
        return sr;
    }

    private static string ResolveSortingLayerName(string preferred, string fallback)
    {
        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == preferred)
                return preferred;

        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == fallback)
                return fallback;

        return "Default";
    }

    private static void SetMarker(SpriteRenderer sr, Vector3 position, Vector3 scale, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = scale;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void SetCorner(SpriteRenderer sr, Vector3 position, float size, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = new Vector3(size, size * 0.42f, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void SetCornerMarker(SpriteRenderer sr, Vector3 position, float width, float height, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = new Vector3(width, height, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    // Bật/tắt các cạnh viền (để chế độ "chỉ 4 góc" giống mẫu).
    private void SetEdgesVisible(bool on)
    {
        ToggleArray(_edges, on);
        ToggleArray(_edgeShadows, on);
        ToggleArray(_edgeHighlights, on);
    }

    private static void ToggleArray(SpriteRenderer[] arr, bool on)
    {
        if (arr == null) return;
        foreach (var r in arr) if (r != null) r.enabled = on;
    }

    private static Sprite FindAnyFootprintSprite()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null && sr.name == "Grid_Footprint" && sr.sprite != null)
                return sr.sprite;
        }
        return null;
    }

    private void EnsureRuntimeSprites()
    {
        if (tileSprite == null)
            tileSprite = FindAnyFootprintSprite();

        if (_diamondSprite == null)
        {
            _diamondSprite = CreatePolygonSprite(
                "Placement_Diamond",
                64,
                new[]
                {
                    new Vector2(0.5f, 1f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 0.5f)
                });
        }

        if (_markerSprite == null)
        {
            _markerSprite = CreatePolygonSprite(
                "Placement_Marker",
                96,
                new[]
                {
                    new Vector2(0.06f, 0.18f),
                    new Vector2(0.94f, 0.18f),
                    new Vector2(0.78f, 0.82f),
                    new Vector2(0.22f, 0.82f)
                });
        }

        if (_arrowSprite == null)
        {
            _arrowSprite = CreatePolygonSprite(
                "Placement_Lift_Arrow",
                96,
                new[]
                {
                    new Vector2(0.50f, 0.98f),
                    new Vector2(0.95f, 0.54f),
                    new Vector2(0.72f, 0.54f),
                    new Vector2(0.72f, 0.08f),
                    new Vector2(0.28f, 0.08f),
                    new Vector2(0.28f, 0.54f),
                    new Vector2(0.05f, 0.54f)
                });
        }

        if (_circleSprite == null)
            _circleSprite = CreateCircleSprite("Placement_Arrow_Dot", 64);

        if (_bracketSprite == null)
        {
            _bracketSprite = CreatePolygonSprite(
                "Placement_Corner_Wedge",
                64,
                new[]
                {
                    new Vector2(0.04f, 0.20f),
                    new Vector2(0.78f, 0.20f),
                    new Vector2(0.98f, 0.50f),
                    new Vector2(0.78f, 0.80f),
                    new Vector2(0.04f, 0.80f),
                    new Vector2(0.24f, 0.50f)
                });
        }

        if (_chevronSprite == null)
            _chevronSprite = CreateCornerChevronSprite("Placement_Rect_Chevron", 64);
    }

    /// <summary>
    /// CHEVRON GÓC hình chữ L, PIVOT ĐÚNG TẠI GÓC (0,0), hai cánh vươn theo +X và +Y.
    ///
    /// VÌ SAO PIVOT Ở GÓC: đặt xong chỉ cần `localPosition` = đúng góc vùng ô rồi xoay
    /// 0/90/180/270° là ra cả 4 góc. Pivot ở giữa thì mỗi góc phải cộng thêm một offset
    /// riêng theo chiều xoay — bốn công thức song song là bốn chỗ để sai (đúng loại lỗi
    /// DEV-1 vừa dọn ở §5.1).
    ///
    /// `pixelsPerUnit = size` ⇒ 1 sprite = 1 unit ⇒ `localScale = size` chính là CẠNH
    /// chevron tính bằng world unit. Đọc số là biết ngay nó to bằng bao nhiêu phần của ô.
    ///
    /// Có LẤY MẪU BỘI 3×3 để khử răng cưa: chevron nằm ngay dưới con trỏ suốt lượt đặt nên
    /// viền nhảy bậc thang là thứ đầu tiên mắt bắt được (CreatePolygonSprite ở trên cắt
    /// cứng, dùng cho mảnh nhỏ thì không sao, dùng cho chevron thì thấy rõ).
    /// </summary>
    private static Sprite CreateCornerChevronSprite(string name, int size)
    {
        const float arm   = 0.92f;   // chiều dài mỗi cánh (tỉ lệ theo ô sprite)
        const float thick = 0.26f;   // độ dày cánh
        const int   ss    = 3;       // số mẫu mỗi chiều

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                {
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = (x + (sx + 0.5f) / ss) / size;
                        float v = (y + (sy + 0.5f) / ss) / size;
                        if ((u <= thick && v <= arm) || (v <= thick && u <= arm)) hit++;
                    }
                }

                float a = hit / (float)(ss * ss);
                pixels[y * size + x] = a <= 0.001f ? clear : new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.zero, size);
        sprite.name = name;
        return sprite;
    }

    private static Sprite CreatePolygonSprite(string name, int size, Vector2[] points)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                if (PointInPolygon(p, points))
                    pixels[y * size + x] = fill;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        return sprite;
    }

    private static Sprite CreateCircleSprite(string name, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = d <= radius ? fill : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        return sprite;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            if ((pi.y > point.y) != (pj.y > point.y) &&
                point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
