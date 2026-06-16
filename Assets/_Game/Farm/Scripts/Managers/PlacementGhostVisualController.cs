using System.Collections;
using UnityEngine;

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
    [SerializeField] private Color validFillColor = new Color(0.08f, 1f, 0.18f, 0.10f);
    [SerializeField] private Color validEdgeColor = new Color(0.00f, 1f, 0.12f, 1f);
    [SerializeField] private Color validEdgeDarkColor = new Color(0.00f, 0.55f, 0.08f, 0.92f);
    [SerializeField] private Color validEdgeHighlightColor = new Color(0.58f, 1f, 0.48f, 0.92f);
    [SerializeField] private Color invalidFillColor = new Color(1f, 0.08f, 0.08f, 0.12f);
    [SerializeField] private Color invalidEdgeColor = new Color(1f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color invalidEdgeDarkColor = new Color(0.62f, 0f, 0f, 0.92f);
    [SerializeField] private Color invalidEdgeHighlightColor = new Color(1f, 0.42f, 0.35f, 0.9f);
    [SerializeField] private Color shadowColor = new Color(0f, 0.28f, 0f, 0.22f);
    [SerializeField] private Color arrowColor = new Color(1f, 0.86f, 0.12f, 1f);
    [SerializeField] private Color arrowRimColor = new Color(0.78f, 0.43f, 0.03f, 1f);
    [SerializeField] private Color arrowHighlightColor = new Color(1f, 0.98f, 0.5f, 1f);
    [SerializeField] private Color arrowShadowColor = new Color(0.45f, 0.23f, 0.02f, 0.42f);

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
    private Coroutine _arrowPulse;
    private Coroutine _framePulse;
    private Coroutine _spawnPop;
    private Coroutine _invalidPulse;
    private Vector3 _arrowBaseLocalPosition;
    private bool _lastValid = true;
    private bool _suppressFramePulse;
    private bool _isBuildingVisuals;

    private const string VisualRootName = "Designed_Placement_Frame";
    private const string ArrowRootName = "Lift_Arrow_Effect";
    private const string SortingLayerName = "CongTrinh";
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

        BuildArrow();
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

        ConfigureFromWorldSize(Mathf.Abs(footprintScale.x), Mathf.Abs(footprintScale.y));
    }

    public void ConfigureFromWorldSize(float worldWidth, float worldHeight)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        float width = Mathf.Max(1.35f, worldWidth);
        float height = Mathf.Max(0.95f, worldHeight);
        float edgeThickness = Mathf.Clamp(Mathf.Min(width, height) * 0.11f, 0.12f, 0.28f);
        float cornerSize = Mathf.Clamp(Mathf.Min(width, height) * 0.16f, 0.16f, 0.36f);

        Transform shadow = _frameRoot.Find("Soft_Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(width * 0.96f, height * 0.96f, 1f);

        if (_fill != null)
            _fill.transform.localScale = new Vector3(width * 0.9f, height * 0.9f, 1f);

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

        SetCorner(_corners[0], new Vector3(0f, height * 0.79f, 0f), cornerSize, 45f);
        SetCorner(_corners[1], new Vector3(width * 0.79f, 0f, 0f), cornerSize, 45f);
        SetCorner(_corners[2], new Vector3(0f, -height * 0.79f, 0f), cornerSize, 45f);
        SetCorner(_corners[3], new Vector3(-width * 0.79f, 0f, 0f), cornerSize, 45f);

        if (_arrowRoot != null)
        {
            _arrowBaseLocalPosition = new Vector3(0f, height * 0.72f + 0.55f, 0f);
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
