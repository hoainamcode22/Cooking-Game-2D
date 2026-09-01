using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V2] BỘ HIỆU ỨNG ÁNH SÁNG THUẦN CODE cho popup Lên Cấp — KHÔNG cần art:
///
///   (i)   HAI LỚP TIA SÁNG (sun-ray) quay chậm NGƯỢC CHIỀU nhau sau lưng badge sao,
///         alpha thấp tạo cảm giác "additive glow" (UI mặc định không có blend additive,
///         nên dùng màu sáng + alpha thấp chồng 2 lớp là gần nhất).
///   (ii)  SPARKLE 4 CÁNH trắng/vàng nhấp nháy liên tục, spawn ngẫu nhiên trong vùng
///         RectTransform chỉ định (thường là ContentPanel) — scale-in twinkle rồi tắt.
///         Pool ~12 cái, không Instantiate/Destroy mỗi lần.
///   (iii) GLOW TRÒN pulse (phồng-xẹp) ngay sau ngôi sao.
///
/// Mọi sprite được VẼ RUNTIME bằng Texture2D và CACHE STATIC (cùng pattern
/// <c>CoinFlyFX.GetFallbackSprite</c>) — cả scene chỉ tốn 3 texture nhỏ.
///
/// Vòng đời: <see cref="Play"/> khi popup mở, <see cref="Stop"/> khi đóng.
/// Toàn bộ dùng Time.unscaledDeltaTime + coroutine thuần (KHÔNG DOTween).
/// </summary>
public class LevelUpSparkleFX : MonoBehaviour
{
    [Header("Neo vị trí")]
    [Tooltip("Badge sao vàng — tia sáng + glow được dựng làm SIBLING đứng NGAY TRƯỚC " +
             "object này trong Hierarchy nên vẽ SAU LƯNG badge. Null → dùng chính transform này.")]
    [SerializeField] private RectTransform badgeAnchor;

    [Tooltip("Vùng spawn sparkle 4 cánh (thường là ContentPanel). Null → dùng cha của badge.")]
    [SerializeField] private RectTransform sparkleArea;

    [Header("Tia sáng quay (sau badge)")]
    [Tooltip("Đường kính lớp tia ngoài (px).")]
    [SerializeField] private float raySize = 430f;
    [Tooltip("Tốc độ quay lớp ngoài (độ/giây). Lớp trong quay ngược chiều ~70% tốc độ.")]
    [SerializeField] private float rayRotateSpeed = 16f;
    [Tooltip("Alpha mỗi lớp tia — giữ thấp (0.15–0.3) cho ra chất 'ánh sáng'.")]
    [Range(0f, 1f)]
    [SerializeField] private float rayAlpha = 0.22f;

    [Header("Glow tròn pulse (sau sao)")]
    [SerializeField] private float glowSize = 300f;
    [Tooltip("Nhịp phồng-xẹp mỗi giây.")]
    [SerializeField] private float glowPulseSpeed = 1.5f;

    [Header("Sparkle 4 cánh")]
    [Tooltip("Số sparkle trong pool (chạy xoay vòng, không Instantiate thêm).")]
    [SerializeField] private int sparklePoolSize = 12;
    [Tooltip("Khoảng cách giữa 2 lần loé (giây).")]
    [SerializeField] private float sparkleInterval = 0.14f;
    [Tooltip("Kích thước sparkle min–max (px).")]
    [SerializeField] private Vector2 sparkleSizeRange = new Vector2(22f, 54f);
    [Tooltip("Thời gian sống 1 lần loé min–max (giây).")]
    [SerializeField] private Vector2 sparkleLifeRange = new Vector2(0.45f, 0.85f);

    // ── Sprite vẽ runtime, cache static dùng chung toàn game ─────────────────
    private static Sprite _raySprite;
    private static Sprite _glowSprite;
    private static Sprite _sparkleSprite;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private RectTransform _fxRoot;      // chứa 2 lớp ray + glow, sibling trước badge
    private RectTransform _rayOuter;
    private RectTransform _rayInner;
    private RectTransform _glow;
    private Image         _rayOuterImg;
    private Image         _rayInnerImg;
    private Image         _glowImg;

    private RectTransform[] _sparkles;      // pool
    private Image[]         _sparkleImgs;
    private float[]         _sparkleLife;   // < 0 = đang rảnh
    private float[]         _sparkleDur;
    private float[]         _sparkleSpin;

    private Coroutine _animRoutine;
    private Coroutine _spawnRoutine;

    private static readonly Color RayColor     = new Color(1f, 0.93f, 0.55f);
    private static readonly Color GlowColor    = new Color(1f, 0.90f, 0.45f);
    private static readonly Color SparkWhite   = Color.white;
    private static readonly Color SparkGold    = new Color(1f, 0.87f, 0.35f);

    private void OnDisable() => Stop();

    // =========================================================================
    // API
    // =========================================================================

    /// <summary>Bật toàn bộ hiệu ứng. Idempotent — gọi lại khi đang chạy chỉ restart nhịp.</summary>
    public void Play()
    {
        if (!isActiveAndEnabled) return;   // object đang tắt → Unity từ chối coroutine

        EnsureBuilt();
        if (_fxRoot == null) return;       // không dựng được (thiếu anchor hợp lệ)

        _fxRoot.gameObject.SetActive(true);

        if (_animRoutine  != null) StopCoroutine(_animRoutine);
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _animRoutine  = StartCoroutine(AnimLoop());
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>Tắt toàn bộ hiệu ứng và giấu mọi phần tử (giữ pool để lần sau dùng lại).</summary>
    public void Stop()
    {
        if (_animRoutine  != null) { StopCoroutine(_animRoutine);  _animRoutine  = null; }
        if (_spawnRoutine != null) { StopCoroutine(_spawnRoutine); _spawnRoutine = null; }

        if (_fxRoot != null) _fxRoot.gameObject.SetActive(false);

        if (_sparkles != null)
            for (int i = 0; i < _sparkles.Length; i++)
            {
                if (_sparkles[i] != null) _sparkles[i].gameObject.SetActive(false);
                if (_sparkleLife != null && i < _sparkleLife.Length) _sparkleLife[i] = -1f;
            }
    }

    // =========================================================================
    // Dựng hierarchy runtime (1 lần)
    // =========================================================================

    private void EnsureBuilt()
    {
        RectTransform anchor = badgeAnchor != null ? badgeAnchor : transform as RectTransform;
        if (anchor == null) return;

        if (_fxRoot == null)
        {
            // Root nằm CÙNG CHA với badge, sibling index NGAY TRƯỚC badge → vẽ sau lưng.
            var go = new GameObject("SparkleFX_Root", typeof(RectTransform));
            _fxRoot = (RectTransform)go.transform;
            Transform parent = anchor.parent != null ? anchor.parent : anchor;
            _fxRoot.SetParent(parent, false);
            _fxRoot.SetSiblingIndex(Mathf.Max(0, anchor.GetSiblingIndex()));
            _fxRoot.anchorMin = anchor.anchorMin;
            _fxRoot.anchorMax = anchor.anchorMax;
            _fxRoot.pivot     = new Vector2(0.5f, 0.5f);
            _fxRoot.anchoredPosition = anchor.anchoredPosition;
            _fxRoot.sizeDelta = Vector2.zero;

            _rayOuter = CreateFxImage(_fxRoot, "Ray_Outer", GetRaySprite(),
                new Vector2(raySize, raySize), RayColor, rayAlpha, out _rayOuterImg);
            _rayInner = CreateFxImage(_fxRoot, "Ray_Inner", GetRaySprite(),
                new Vector2(raySize * 0.78f, raySize * 0.78f), RayColor, rayAlpha, out _rayInnerImg);
            _rayInner.localRotation = Quaternion.Euler(0f, 0f, 15f); // so le răng cưa 2 lớp
            _glow = CreateFxImage(_fxRoot, "Glow_Pulse", GetGlowSprite(),
                new Vector2(glowSize, glowSize), GlowColor, 0.4f, out _glowImg);
        }

        if (_sparkles == null || _sparkles.Length == 0)
        {
            int n = Mathf.Max(1, sparklePoolSize);
            _sparkles    = new RectTransform[n];
            _sparkleImgs = new Image[n];
            _sparkleLife = new float[n];
            _sparkleDur  = new float[n];
            _sparkleSpin = new float[n];

            RectTransform area = ResolveSparkleArea(anchor);
            for (int i = 0; i < n; i++)
            {
                _sparkles[i] = CreateFxImage(area, $"Sparkle_{i:00}", GetSparkleSprite(),
                    Vector2.one * sparkleSizeRange.y, SparkWhite, 1f, out _sparkleImgs[i]);
                _sparkles[i].gameObject.SetActive(false);
                _sparkleLife[i] = -1f;
            }
        }
    }

    private RectTransform ResolveSparkleArea(RectTransform anchorFallback)
    {
        if (sparkleArea != null) return sparkleArea;
        var p = anchorFallback.parent as RectTransform;
        return p != null ? p : anchorFallback;
    }

    private static RectTransform CreateFxImage(
        RectTransform parent, string name, Sprite sprite,
        Vector2 size, Color color, float alpha, out Image img)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;

        img = go.GetComponent<Image>();
        img.sprite        = sprite;
        img.raycastTarget = false;                 // TUYỆT ĐỐI không chặn nút Nhận Quà
        color.a = alpha;
        img.color = color;
        return rt;
    }

    // =========================================================================
    // Vòng lặp animation
    // =========================================================================

    /// <summary>Quay 2 lớp tia ngược chiều + glow pulse + cập nhật twinkle của pool sparkle.</summary>
    private IEnumerator AnimLoop()
    {
        float t = 0f;
        while (true)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            // (i) 2 lớp tia quay ngược chiều — tốc độ khác nhau cho cảm giác lung linh
            if (_rayOuter != null) _rayOuter.Rotate(0f, 0f, rayRotateSpeed * dt);
            if (_rayInner != null) _rayInner.Rotate(0f, 0f, -rayRotateSpeed * 0.7f * dt);

            // Alpha tia "thở" nhẹ quanh mức gốc → chớp chớp chứ không tĩnh
            float breathe = 0.85f + 0.15f * Mathf.Sin(t * Mathf.PI * 2f * 0.8f);
            if (_rayOuterImg != null) SetAlpha(_rayOuterImg, rayAlpha * breathe);
            if (_rayInnerImg != null) SetAlpha(_rayInnerImg, rayAlpha * (1.7f - breathe));

            // (iii) glow pulse: phồng 1 → 1.18 và alpha 0.28 → 0.5
            if (_glow != null && _glowImg != null)
            {
                float p = (Mathf.Sin(t * Mathf.PI * 2f * glowPulseSpeed) + 1f) * 0.5f; // 0..1
                _glow.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, p);
                SetAlpha(_glowImg, Mathf.Lerp(0.28f, 0.5f, p));
            }

            // (ii) twinkle từng sparkle đang sống: scale-in → đỉnh → tắt (đường sin nửa chu kỳ)
            if (_sparkles != null)
            {
                for (int i = 0; i < _sparkles.Length; i++)
                {
                    if (_sparkleLife[i] < 0f || _sparkles[i] == null) continue;

                    _sparkleLife[i] += dt;
                    float k = _sparkleLife[i] / Mathf.Max(0.05f, _sparkleDur[i]);
                    if (k >= 1f)
                    {
                        _sparkles[i].gameObject.SetActive(false);
                        _sparkleLife[i] = -1f;
                        continue;
                    }

                    float tw = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);   // 0 → 1 → 0
                    _sparkles[i].localScale = Vector3.one * tw;
                    _sparkles[i].Rotate(0f, 0f, _sparkleSpin[i] * dt);
                    if (_sparkleImgs[i] != null) SetAlpha(_sparkleImgs[i], tw);
                }
            }

            yield return null;
        }
    }

    /// <summary>Cứ mỗi <see cref="sparkleInterval"/> giây lại loé 1 sparkle ở vị trí ngẫu nhiên.</summary>
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOneSparkle();
            float wait = Mathf.Max(0.03f, sparkleInterval * Random.Range(0.7f, 1.4f));
            float t = 0f;
            while (t < wait) { t += Time.unscaledDeltaTime; yield return null; }
        }
    }

    private void SpawnOneSparkle()
    {
        if (_sparkles == null) return;

        for (int i = 0; i < _sparkles.Length; i++)
        {
            if (_sparkleLife[i] >= 0f || _sparkles[i] == null) continue;   // tìm con đang rảnh

            RectTransform area = _sparkles[i].parent as RectTransform;
            Rect r = area != null ? area.rect : new Rect(-200f, -200f, 400f, 400f);
            _sparkles[i].anchoredPosition = new Vector2(
                Random.Range(r.xMin, r.xMax),
                Random.Range(r.yMin, r.yMax));

            float size = Random.Range(sparkleSizeRange.x, sparkleSizeRange.y);
            _sparkles[i].sizeDelta  = new Vector2(size, size);
            _sparkles[i].localScale = Vector3.zero;
            _sparkles[i].localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));

            _sparkleDur[i]  = Random.Range(sparkleLifeRange.x, sparkleLifeRange.y);
            _sparkleSpin[i] = Random.Range(-70f, 70f);
            _sparkleLife[i] = 0f;

            if (_sparkleImgs[i] != null)
            {
                Color c = Random.value < 0.5f ? SparkWhite : SparkGold;
                c.a = 0f;
                _sparkleImgs[i].color = c;
            }

            _sparkles[i].gameObject.SetActive(true);
            return;   // mỗi lần chỉ loé 1 con
        }
    }

    private static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color;
        c.a = Mathf.Clamp01(a);
        g.color = c;
    }

    // =========================================================================
    // Sprite vẽ runtime — cache static (pattern CoinFlyFX.GetFallbackSprite)
    // =========================================================================

    /// <summary>Đĩa tia sáng 12 nan hoa, sáng ở tâm mờ dần ra mép — dùng cho 2 lớp ray.</summary>
    private static Sprite GetRaySprite()
    {
        if (_raySprite != null) return _raySprite;

        const int size = 128;
        const int rayCount = 12;
        var tex = NewFxTexture(size);

        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - c, dy = y - c;
            float dist01 = Mathf.Sqrt(dx * dx + dy * dy) / c;
            if (dist01 > 1f) { tex.SetPixel(x, y, Color.clear); continue; }

            // Nan hoa: sin theo góc — dương là "trong tia", âm là khe tối
            float ang  = Mathf.Atan2(dy, dx);
            float spoke = Mathf.Sin(ang * rayCount);
            spoke = Mathf.Clamp01(spoke * 2.2f);                 // nan sắc cạnh vừa phải

            float falloff = Mathf.Pow(1f - dist01, 1.6f);        // mờ dần ra mép
            float core    = Mathf.Pow(Mathf.Max(0f, 1f - dist01 * 3.4f), 2f); // lõi sáng giữa

            float a = Mathf.Clamp01(spoke * falloff + core);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        _raySprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _raySprite;
    }

    /// <summary>Đốm glow tròn — gradient mềm từ tâm ra mép.</summary>
    private static Sprite GetGlowSprite()
    {
        if (_glowSprite != null) return _glowSprite;

        const int size = 64;
        var tex = NewFxTexture(size);

        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - c, dy = y - c;
            float d01 = Mathf.Sqrt(dx * dx + dy * dy) / c;
            float a = d01 >= 1f ? 0f : Mathf.Pow(1f - d01, 2.2f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _glowSprite;
    }

    /// <summary>Sparkle 4 cánh cổ điển: 2 lưỡi sáng dọc-ngang + lõi tròn nhỏ.</summary>
    private static Sprite GetSparkleSprite()
    {
        if (_sparkleSprite != null) return _sparkleSprite;

        const int size = 48;
        var tex = NewFxTexture(size);

        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = Mathf.Abs(x - c) / c;   // 0 tại tâm → 1 ở mép
            float v = Mathf.Abs(y - c) / c;

            // Cánh ngang: rất mảnh theo v, kéo dài theo u (và ngược lại cho cánh dọc)
            float armH = Mathf.Pow(Mathf.Max(0f, 1f - u), 1.2f) * Mathf.Pow(Mathf.Max(0f, 1f - v), 9f);
            float armV = Mathf.Pow(Mathf.Max(0f, 1f - v), 1.2f) * Mathf.Pow(Mathf.Max(0f, 1f - u), 9f);

            // Lõi tròn sáng ở giữa
            float d01  = Mathf.Sqrt(u * u + v * v);
            float core = Mathf.Pow(Mathf.Max(0f, 1f - d01 * 2.6f), 2f);

            float a = Mathf.Clamp01(armH + armV + core);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        _sparkleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _sparkleSprite;
    }

    private static Texture2D NewFxTexture(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags  = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp
        };
    }
}
