using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bàn tay gợi ý "chạm vào đây" kiểu casual game (Hay Day) — tự dựng 100% runtime,
/// không cần prefab/asset/scene setup:
///   • GuideTapHintFX.ShowAtWorld(worldPos, duration) — trỏ vào object world (chuồng, nhà, toa tàu…)
///   • GuideTapHintFX.ShowAtRect(rectTransform, duration) — trỏ vào nút UI
///   • GuideTapHintFX.Hide() — tắt sớm
/// Visual: con trỏ chạm trắng viền sẫm (vòng tròn đầu ngón + đuôi teardrop, Texture2D
/// vẽ bằng code, cache static) + vòng tap pulse. Tay bob nhẹ ±6px, mỗi 0.9s gõ 1 nhịp
/// (scale 1→0.8→1 trong 0.18s + ring 0.4→1.4 fade trong 0.3s). Fade in/out 0.15s,
/// tự ẩn sau duration (duration &lt;= 0 → hiện tới khi gọi Hide()).
/// World-mode: mỗi frame re-project Camera.main.WorldToScreenPoint → vị trí canvas
/// (bám theo camera pan/zoom; camera null thì giữ vị trí cũ, không lỗi).
/// KHÔNG bao giờ chặn input: mọi Image raycastTarget=false, CanvasGroup.blocksRaycasts=false.
/// Tự gắn vào Canvas_HUD (hoặc canvas screen-space bất kỳ) qua EnsureInstance().
/// </summary>
public class GuideTapHintFX : MonoBehaviour
{
    private static GuideTapHintFX _instance;
    private static Sprite _handSprite;
    private static Sprite _ringSprite;
    private static bool _warnedNoCanvas;

    // ── Tuning ───────────────────────────────────────────────────────────────
    private const float FadeTime       = 0.15f;
    private const float FirstTapDelay  = 0.45f;
    private const float TapInterval    = 0.9f;
    private const float TapDuration    = 0.18f;
    private const float TapSquashScale = 0.8f;
    private const float RingDuration   = 0.3f;
    private const float RingStartScale = 0.4f;
    private const float RingEndScale   = 1.4f;
    private const float BobAmplitude   = 6f;
    private const float BobPeriod      = 1.2f;
    private const float SpriteSize     = 96f;

    // Điểm chạm trên texture tay 96×96 = tâm vòng tròn đầu ngón (pixel 40, 66)
    private const float HandPivotX = 40f / 96f;
    private const float HandPivotY = 66f / 96f;

    private static readonly Color RingColor   = new Color(1f, 0.84f, 0.35f, 1f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.22f);

    private enum TargetMode { None, World, Rect }

    // ── Runtime refs ─────────────────────────────────────────────────────────
    private Canvas        _canvas;
    private RectTransform _canvasRect;
    private CanvasGroup   _group;
    private RectTransform _hand;
    private RectTransform _ring;
    private Image         _ringImage;

    private TargetMode    _mode = TargetMode.None;
    private Vector3       _worldPos;
    private RectTransform _rectTarget;
    private Canvas        _rectTargetCanvas;

    // ── Static API ───────────────────────────────────────────────────────────

    /// <summary>Tìm/tạo instance dưới Canvas_HUD (hoặc canvas screen-space bất kỳ). Có thể trả null nếu chưa có canvas.</summary>
    public static GuideTapHintFX EnsureInstance()
    {
        if (_instance != null)
        {
            _instance.ReattachIfCanvasLost();
            return _instance;
        }

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
        {
            if (!_warnedNoCanvas)
            {
                _warnedNoCanvas = true;
                Debug.LogWarning("[GuideTapHintFX] Không tìm thấy Canvas screen-space — tap hint sẽ bị bỏ qua.");
            }
            return null;
        }

        var go = new GameObject("GuideTapHintFX", typeof(RectTransform));
        go.layer = canvas.gameObject.layer;
        go.transform.SetParent(canvas.transform, false);

        var fx = go.AddComponent<GuideTapHintFX>(); // Awake gán _instance
        fx._canvas = canvas;
        fx._canvasRect = canvas.GetComponent<RectTransform>();
        fx.BuildVisuals();
        return fx;
    }

    /// <summary>Tay chỉ vào vị trí world (re-project theo camera mỗi frame). duration &lt;= 0 → hiện tới khi Hide().</summary>
    public static void ShowAtWorld(Vector3 worldPos, float duration)
    {
        GuideTapHintFX fx = EnsureInstance();
        if (fx != null)
            fx.Show(TargetMode.World, worldPos, null, duration);
    }

    /// <summary>Tay chỉ vào 1 RectTransform UI (bám theo nếu UI di chuyển). duration &lt;= 0 → hiện tới khi Hide().</summary>
    public static void ShowAtRect(RectTransform target, float duration)
    {
        if (target == null)
            return;
        GuideTapHintFX fx = EnsureInstance();
        if (fx != null)
            fx.Show(TargetMode.Rect, Vector3.zero, target, duration);
    }

    /// <summary>Tắt hint sớm (fade-out mềm). An toàn khi chưa từng Show.</summary>
    public static void Hide()
    {
        if (_instance != null)
            _instance.HideInternal();
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    // ── Show / Hide ──────────────────────────────────────────────────────────

    private void Show(TargetMode mode, Vector3 worldPos, RectTransform target, float duration)
    {
        ReattachIfCanvasLost();
        if (_canvas == null || _group == null)
            return;

        _mode             = mode;
        _worldPos         = worldPos;
        _rectTarget       = target;
        _rectTargetCanvas = target != null ? target.GetComponentInParent<Canvas>() : null;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            // Canvas đang bị tắt (vd. cooking mode) — bỏ qua, không crash.
            _mode = TargetMode.None;
            gameObject.SetActive(false);
            return;
        }

        transform.SetAsLastSibling();
        StopAllCoroutines();
        StartCoroutine(PlayRoutine(duration));
    }

    private void HideInternal()
    {
        if (!gameObject.activeInHierarchy)
        {
            _mode = TargetMode.None;
            gameObject.SetActive(false);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(FadeOutThenDisable());
    }

    // ── Animation ────────────────────────────────────────────────────────────

    private IEnumerator PlayRoutine(float duration)
    {
        if (duration <= 0f)
            duration = float.PositiveInfinity;

        _hand.localScale = Vector3.one;
        _hand.anchoredPosition = Vector2.zero;
        _ring.localScale = Vector3.one * RingStartScale;
        SetRingAlpha(0f);
        UpdateTargetPosition(); // snap vào vị trí trước khi fade-in

        yield return FadeGroup(_group.alpha, 1f, FadeTime);

        float elapsed = 0f;
        float nextTap = FirstTapDelay;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (!UpdateTargetPosition())
                break; // target UI bị huỷ → kết thúc sớm

            // Bob nhẹ ±6px
            _hand.anchoredPosition = new Vector2(0f,
                Mathf.Sin(elapsed * (Mathf.PI * 2f / BobPeriod)) * BobAmplitude);

            if (elapsed >= nextTap)
            {
                nextTap = elapsed + TapInterval;
                StartCoroutine(TapOnce());
            }
            yield return null;
        }

        yield return FadeGroup(_group.alpha, 0f, FadeTime);
        _mode = TargetMode.None;
        gameObject.SetActive(false);
    }

    private IEnumerator TapOnce()
    {
        float t = 0f;
        while (t < RingDuration)
        {
            t += Time.unscaledDeltaTime;

            // Tay: 1 → 0.8 → 1 (nửa sin, 0.18s)
            float handT  = Mathf.Clamp01(t / TapDuration);
            float squash = 1f - (1f - TapSquashScale) * Mathf.Sin(handT * Mathf.PI);
            _hand.localScale = new Vector3(squash, squash, 1f);

            // Ring: scale 0.4 → 1.4 ease-out, alpha 1 → 0 (0.3s)
            float ringT = Mathf.Clamp01(t / RingDuration);
            float eased = 1f - (1f - ringT) * (1f - ringT);
            _ring.localScale = Vector3.one * Mathf.Lerp(RingStartScale, RingEndScale, eased);
            SetRingAlpha(1f - ringT);

            yield return null;
        }
        _hand.localScale = Vector3.one;
        SetRingAlpha(0f);
    }

    private IEnumerator FadeOutThenDisable()
    {
        yield return FadeGroup(_group != null ? _group.alpha : 0f, 0f, FadeTime);
        _mode = TargetMode.None;
        gameObject.SetActive(false);
    }

    private IEnumerator FadeGroup(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            if (_group == null)
                yield break;
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (_group != null)
            _group.alpha = to;
    }

    // ── Positioning ──────────────────────────────────────────────────────────

    /// <summary>Đưa root về đúng vị trí target trên canvas. False = target không còn → nên kết thúc.</summary>
    private bool UpdateTargetPosition()
    {
        Vector2 screenPoint;

        if (_mode == TargetMode.World)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return true; // giữ vị trí cũ, chờ camera quay lại
            Vector3 sp = cam.WorldToScreenPoint(_worldPos);
            if (sp.z < 0f)
                return true; // sau lưng camera (hiếm với 2D) — giữ nguyên
            screenPoint = sp;
        }
        else if (_mode == TargetMode.Rect)
        {
            if (_rectTarget == null)
                return false; // nút bị huỷ → fade-out

            if (_rectTargetCanvas == null)
                _rectTargetCanvas = _rectTarget.GetComponentInParent<Canvas>();

            Camera targetCam = null;
            if (_rectTargetCanvas != null)
            {
                Canvas rootC = _rectTargetCanvas.rootCanvas;
                if (rootC.renderMode != RenderMode.ScreenSpaceOverlay)
                    targetCam = rootC.worldCamera;
            }

            Vector3 wp = _rectTarget.TransformPoint(_rectTarget.rect.center);
            screenPoint = RectTransformUtility.WorldToScreenPoint(targetCam, wp);
        }
        else
        {
            return false;
        }

        if (_canvas == null || _canvasRect == null)
            return true;

        Camera myCam = null;
        Canvas myRoot = _canvas.rootCanvas;
        if (myRoot.renderMode != RenderMode.ScreenSpaceOverlay)
            myCam = myRoot.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, myCam, out Vector2 local))
            ((RectTransform)transform).anchoredPosition = local;
        return true;
    }

    // ── Build UI (runtime, không prefab) ─────────────────────────────────────

    private void BuildVisuals()
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;

        _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.blocksRaycasts = false;
        _group.interactable   = false;

        // Ring pulse (vẽ dưới tay)
        var ringGo = new GameObject("TapRing", typeof(RectTransform));
        ringGo.layer = gameObject.layer;
        _ring = (RectTransform)ringGo.transform;
        _ring.SetParent(rt, false);
        _ring.anchorMin = new Vector2(0.5f, 0.5f);
        _ring.anchorMax = new Vector2(0.5f, 0.5f);
        _ring.pivot     = new Vector2(0.5f, 0.5f);
        _ring.sizeDelta = new Vector2(SpriteSize, SpriteSize);
        _ring.anchoredPosition = Vector2.zero;
        _ringImage = ringGo.AddComponent<Image>();
        _ringImage.sprite        = GetRingSprite();
        _ringImage.raycastTarget = false;
        SetRingAlpha(0f);

        // Hand group — pivot tại điểm chạm (tâm vòng tròn đầu ngón)
        var handGo = new GameObject("Hand", typeof(RectTransform));
        handGo.layer = gameObject.layer;
        _hand = (RectTransform)handGo.transform;
        _hand.SetParent(rt, false);
        _hand.anchorMin = new Vector2(0.5f, 0.5f);
        _hand.anchorMax = new Vector2(0.5f, 0.5f);
        _hand.pivot     = new Vector2(HandPivotX, HandPivotY);
        _hand.sizeDelta = new Vector2(SpriteSize, SpriteSize);
        _hand.anchoredPosition = Vector2.zero;

        CreateHandImage("Shadow", new Vector2(5f, -5f), ShadowColor);
        CreateHandImage("Fill",   Vector2.zero,         Color.white);

        gameObject.SetActive(false);
    }

    private void CreateHandImage(string childName, Vector2 offset, Color color)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.layer = gameObject.layer;
        var crt = (RectTransform)go.transform;
        crt.SetParent(_hand, false);
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = offset;
        crt.offsetMax = offset;

        var img = go.AddComponent<Image>();
        img.sprite        = GetHandSprite();
        img.color         = color;
        img.raycastTarget = false;
    }

    private void SetRingAlpha(float a)
    {
        if (_ringImage == null)
            return;
        Color c = RingColor;
        c.a = a;
        _ringImage.color = c;
    }

    private void ReattachIfCanvasLost()
    {
        if (_canvas != null && _canvas.isActiveAndEnabled)
            return;

        Canvas canvas = FindHudCanvas();
        if (canvas == null)
            return;

        _canvas     = canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();

        var rt = (RectTransform)transform;
        rt.SetParent(canvas.transform, false);

        gameObject.layer = canvas.gameObject.layer;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = gameObject.layer;
    }

    private static Canvas FindHudCanvas()
    {
        Canvas named = null, rootCanvas = null, any = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!c.isActiveAndEnabled || c.renderMode == RenderMode.WorldSpace)
                continue;
            if (named == null && c.name == "Canvas_HUD") named = c;
            if (rootCanvas == null && c.transform.parent == null) rootCanvas = c;
            if (any == null) any = c;
        }
        if (named != null) return named;
        if (rootCanvas != null) return rootCanvas;
        return any;
    }

    // ── Procedural sprites (cache static, sống qua scene reload) ────────────

    /// <summary>Con trỏ chạm 96×96: vòng tròn trắng r≈26 phía trên-trái + đuôi teardrop
    /// xuôi xuống dưới-phải, viền sẫm ~2px, AA mềm.</summary>
    private static Sprite GetHandSprite()
    {
        if (_handSprite != null)
            return _handSprite;

        const int size = 96;
        Vector2 pad = new Vector2(40f, 66f); // tâm đầu ngón = điểm chạm
        const float padRadius = 26f;
        Vector2 tailStart = new Vector2(52f, 52f);
        Vector2 tailEnd   = new Vector2(82f, 12f);
        const float tailStartRadius = 17f;
        const float tailEndRadius   = 5f;
        const int   tailSamples     = 14;

        Color outline = new Color(0.23f, 0.16f, 0.13f, 1f);
        Color fill    = Color.white;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name       = "GuideTapHintHand",
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                // SDF = hợp của vòng tròn đầu ngón + chuỗi tròn nhỏ dần (teardrop)
                float sdf = Vector2.Distance(p, pad) - padRadius;
                for (int i = 0; i <= tailSamples; i++)
                {
                    float t = i / (float)tailSamples;
                    Vector2 c = Vector2.Lerp(tailStart, tailEnd, t);
                    float r = Mathf.Lerp(tailStartRadius, tailEndRadius, t);
                    float d = Vector2.Distance(p, c) - r;
                    if (d < sdf) sdf = d;
                }

                float shapeAlpha = Mathf.Clamp01(0.5f - sdf); // toàn hình (gồm viền)
                if (shapeAlpha <= 0f)
                {
                    pixels[y * size + x] = default;
                    continue;
                }

                float whiteCore = Mathf.Clamp01(-sdf - 1.5f); // lõi trắng, viền sẫm ~2px
                Color col = Color.Lerp(outline, fill, whiteCore);
                col.a = shapeAlpha;
                pixels[y * size + x] = col;
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        _handSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(HandPivotX, HandPivotY), 100f);
        _handSprite.name = "GuideTapHintHand";
        return _handSprite;
    }

    /// <summary>Vòng tap 96×96: đường tròn rỗng r≈38, dày ~9px, AA mềm (tint màu lúc runtime).</summary>
    private static Sprite GetRingSprite()
    {
        if (_ringSprite != null)
            return _ringSprite;

        const int size = 96;
        const float center        = size * 0.5f;
        const float ringRadius    = 38f;
        const float halfThickness = 4.5f;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name       = "GuideTapHintRing",
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float sdf  = Mathf.Abs(dist - ringRadius) - halfThickness;
                float a    = Mathf.Clamp01(0.5f - sdf);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        _ringSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f);
        _ringSprite.name = "GuideTapHintRing";
        return _ringSprite;
    }
}
