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
    private bool _priceBarChecked;          // đã thử dựng dải giá chưa (chỉ làm 1 lần / Ghost)

    private const string VisualRootName = "Designed_Placement_Frame";
    private const string ArrowRootName = "Lift_Arrow_Effect";
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

        BuildArrow();
        EnsurePriceBar();
        ApplyVisualState(_lastValid);

        if (_arrowRoot != null)
            _arrowRoot.gameObject.SetActive(false);

        if (_framePulse == null)
            _framePulse = StartCoroutine(PulseFrame());

        _isBuildingVisuals = false;
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
    // N4 — DẢI GIÁ TRONG THANH XÁC NHẬN  (ảnh 1, 3: "KAUFEN FÜR 🪙 <giá>")
    // ══════════════════════════════════════════════════════════════════════

    private const string PriceBarName = "Price_Bar";

    /// <summary>
    /// Dựng dải "MUA VỚI GIÁ 🪙 &lt;giá&gt;" NGAY TRÊN hàng nút ✕ ↻ ✓.
    ///
    /// DỰNG BẰNG CODE, KHÔNG SỬA PREFAB: prefab `Placement_Ghost` là file YAML dùng chung
    /// với DEV-1 (hàng nút, canvas, footprint đều nằm trong đó) — thêm node bằng tay dễ
    /// đụng độ merge, mà Edric cũng không phải mở prefab chỉnh gì. Chạy runtime thì mọi
    /// công trình đều tự có dải giá, kể cả prefab ghost sau này bị thay.
    ///
    /// ĐƠN VỊ: canvas `Placement_UI` có localScale 0.01 nằm dưới root scale 100 ⇒ tích = 1,
    /// nên 1 "pixel" UI ở đây đúng bằng 1 world unit. Hàng nút cao 126, tâm ở y = 0
    /// (xem PlacementManager.StyleGhostActionBar) ⇒ đặt dải giá ở y = 104 là vừa sát trên.
    ///
    /// Giá lấy từ `PlaceableItemData.goldPrice` / `diamondPrice` của item Ghost đang cầm.
    /// Đang SỬA công trình cũ (không mua gì) thì `currentItem` = null ⇒ không hiện dải giá.
    /// </summary>
    private void EnsurePriceBar()
    {
        if (_priceBarChecked) return;
        _priceBarChecked = true;

        PlaceableItemData data = ConstructionBridge.GetGhostItem();
        if (data == null) return;

        bool useGem = data.goldPrice <= 0 && data.diamondPrice > 0;
        int  price  = useGem ? data.diamondPrice : data.goldPrice;
        if (price <= 0) return;   // item miễn phí → không cần dải giá

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null) return;

        Transform existing = canvas.transform.Find(PriceBarName);
        if (existing != null) Destroy(existing.gameObject);

        // ── Nền tối bo góc ───────────────────────────────────────────────
        var barGo = new GameObject(PriceBarName, typeof(RectTransform));
        barGo.transform.SetParent(canvas.transform, false);
        barGo.layer = canvas.gameObject.layer;

        var barRect = (RectTransform)barGo.transform;
        barRect.anchorMin        = new Vector2(0.5f, 0.5f);
        barRect.anchorMax        = new Vector2(0.5f, 0.5f);
        barRect.pivot            = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = new Vector2(0f, 104f);
        barRect.sizeDelta        = new Vector2(360f, 66f);

        // Ô art `PriceBarBg`: chưa gán thì dùng panel vẽ bằng code, tô MÀU NHẬN DẠNG
        // (đen) để Edric biết chỗ này thả nền thanh giá vào.
        ConstructionArtKit kit = ConstructionManager.Instance != null
                               ? ConstructionManager.Instance.ArtKit : null;

        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.PriceBarBg,
                                       ConstructionSpriteFactory.Panel(96, 64, 24),
                                       out Sprite barSpr, out Color barCol);

        var bg = barGo.AddComponent<Image>();
        bg.sprite        = barSpr;
        bg.type          = Image.Type.Sliced;
        bg.color         = barCol;
        bg.raycastTarget = false;

        if (ConstructionArtKit.WantLabels(kit))
            ConstructionSiteVisuals.AttachSlotLabel(barGo.transform,
                                                    ConstructionArtKit.Slot.PriceBarBg, kit);

        // Bề rộng tự co theo độ dài chữ (tiếng Việt dài hơn tiếng Đức trong ảnh mẫu).
        var layout = barGo.AddComponent<HorizontalLayoutGroup>();
        layout.padding                = new RectOffset(26, 26, 6, 6);
        layout.spacing                = 10f;
        layout.childAlignment         = TextAnchor.MiddleCenter;
        layout.childControlWidth      = true;
        layout.childControlHeight     = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        var fitter = barGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

        // ── Chữ + icon tiền + số ─────────────────────────────────────────
        MakePriceLabel(barRect, "Text_MuaVoiGia", "MUA VỚI GIÁ", 38f);

        var iconGo = new GameObject("Icon_Tien", typeof(RectTransform));
        iconGo.transform.SetParent(barRect, false);
        iconGo.layer = barGo.layer;

        var icon = iconGo.AddComponent<Image>();
        icon.sprite        = useGem
            ? ConstructionSpriteFactory.GemIcon()
            : ConstructionSpriteFactory.CoinIcon();
        icon.raycastTarget = false;

        var iconLayout = iconGo.AddComponent<LayoutElement>();
        iconLayout.preferredWidth  = 44f;
        iconLayout.preferredHeight = 44f;
        ((RectTransform)iconGo.transform).sizeDelta = new Vector2(44f, 44f);

        MakePriceLabel(barRect, "Text_Gia", price.ToString(), 40f);
    }

    private static void MakePriceLabel(Transform parent, string name, string content, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

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

        ((RectTransform)go.transform).sizeDelta = new Vector2(tmp.preferredWidth, 52f);

        // Viền đậm giống nhãn Township (cùng cách với LevelUpPopupTownshipTool.AddTextOutline)
        Material mat = tmp.fontMaterial;
        if (mat != null) mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        tmp.outlineColor = new Color(0.07f, 0.05f, 0.02f, 1f);
        tmp.outlineWidth = 0.26f;
        tmp.UpdateMeshPadding();
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
