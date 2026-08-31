using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BUBBLE MÓN ĂN world-space trên đầu khách du lịch (GDD BOAT-002 §3.3).
///
/// Component nằm trên PREFAB khách (NPCAnimationSetupTool gắn sẵn).
/// Chuỗi Bong Bóng Suy Nghĩ (Comic Thought Cloud Bubble):
///   • Chấm nhỏ (Dot 1) gần đỉnh đầu -> Chấm vừa (Dot 2) -> Đám mây lớn (Frame) chứa món ăn / biểu cảm.
///
/// 3 trạng thái:
///   • Requesting — đám mây suy nghĩ + sprite MÓN ăn khách muốn.
///   • Happy      — mặt cười 0.5s trước khi TouristSmileyFlyFX bay lên HUD.
///   • Angry      — mặt TỨC GIẬN 2s rồi khách bỏ về (hết kiên nhẫn).
/// </summary>
public class TouristRequestBubble : MonoBehaviour
{
    /// <summary>Trạng thái hiển thị của bubble.</summary>
    public enum BubbleState { Hidden, Requesting, Happy, Angry }

    [Header("Art (tool wire — thiếu thì tự vẽ placeholder)")]
    [Tooltip("Sprite khung đám mây to. Trống → vẽ đám mây cartoon viền nâu procedural.")]
    [SerializeField] private Sprite frameSprite;

    [Tooltip("Sprite cho chấm nhỏ / vừa. Trống → vẽ tròn viền nâu procedural.")]
    [SerializeField] private Sprite dotSprite;

    [Tooltip("Sprite mặt cười (trạng thái Happy). Trống → mặt cười VÀNG procedural.")]
    [SerializeField] private Sprite smileySprite;

    [Tooltip("Sprite mặt TỨC GIẬN (trạng thái Angry — hết kiên nhẫn). Trống → mặt cau mày ĐỎ procedural.")]
    [SerializeField] private Sprite angryFaceSprite;

    [Header("Bố cục Đám mây chính (unit WORLD)")]
    [Tooltip("Vị trí đám mây to so với chân khách. Đặt ở Y ~276 để đám mây to nằm thoáng trên đầu khách.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(30f, 276f, 0f);

    [Tooltip("Cỡ đám mây to (unit world) — to rộng rãi để người chơi nhìn rõ món ăn.")]
    [SerializeField] private float frameWorldSize = 168f;

    [Tooltip("Cỡ icon món/mặt trong khung (unit world) — to rõ nét.")]
    [SerializeField] private float iconWorldSize = 110f;

    [Header("Chuỗi bong bóng nhỏ (Thought Bubble Chain)")]
    [Tooltip("Vị trí chấm nhỏ 1 (sát đỉnh đầu khách).")]
    [SerializeField] private Vector3 dot1Offset = new Vector3(12f, 176f, 0f);

    [Tooltip("Cỡ chấm nhỏ 1 (unit world).")]
    [SerializeField] private float dot1Size = 16f;

    [Tooltip("Vị trí chấm vừa 2 (ở giữa chấm nhỏ và đám mây).")]
    [SerializeField] private Vector3 dot2Offset = new Vector3(20f, 208f, 0f);

    [Tooltip("Cỡ chấm vừa 2 (unit world).")]
    [SerializeField] private float dot2Size = 28f;

    [Header("Sorting (bubble phải nổi TRÊN ĐẦU khách)")]
    [Tooltip("ĐỂ TRỐNG = tự chọn 'Foreground' (khuyến nghị, luôn trên đầu khách).")]
    [SerializeField] private string sortingLayerName = "";
    [SerializeField] private int frameSortingOrder = 20000;

    // ─── Runtime ────────────────────────────────────────────────────────

    public BubbleState State { get; private set; } = BubbleState.Hidden;

    /// <summary>Bubble đang HỎI MÓN (khách sẵn sàng nhận món) — manager/agent kiểm trước khi cho giao.</summary>
    public bool IsRequesting => State == BubbleState.Requesting;

    /// <summary>Sprite mặt cười đang dùng (art hoặc placeholder) — FX bay lên HUD dùng đúng sprite này.</summary>
    public Sprite SmileySpriteResolved =>
        smileySprite != null ? smileySprite : GetPlaceholderFace(FaceKind.Happy);

    private float _scaleInSeconds = 0.35f;

    private Transform      _root;
    private Transform      _dot1Tr;
    private Transform      _dot2Tr;
    private Transform      _frameTr;
    private SpriteRenderer _dot1Sr;
    private SpriteRenderer _dot2Sr;
    private SpriteRenderer _frameSr;
    private SpriteRenderer _iconSr;
    private Coroutine      _animRoutine;

    // Base local positions & scales
    private Vector3 _baseFramePos;
    private Vector3 _baseDot1Pos;
    private Vector3 _baseDot2Pos;

    private float _baseDot1Scale  = 1f;
    private float _baseDot2Scale  = 1f;
    private float _baseFrameScale = 1f;
    private float _baseIconScale  = 1f;

    private float _floatSeed;

    /// <summary>Agent bơm số từ TouristBoatConfig (bubbleScaleInTime) lúc Init.</summary>
    public void Configure(float scaleInSeconds)
    {
        if (scaleInSeconds > 0.01f) _scaleInSeconds = scaleInSeconds;
    }

    // ─── API cho TouristAgent ───────────────────────────────────────────

    /// <summary>Mở chuỗi bubble hiện MÓN khách muốn với animation nảy tuần tự.</summary>
    public void ShowRequest(Sprite dishSprite)
    {
        EnsureBuilt();
        State = BubbleState.Requesting;
        SetIcon(dishSprite != null ? dishSprite : GetPlaceholderFace(FaceKind.Plain));
        PlayChainScaleIn();
    }

    /// <summary>Đổi icon thành MẶT CƯỜI (đã giao món) — agent chờ 0.5s rồi bắn SmileyFlyFX.</summary>
    public void ShowHappy()
    {
        EnsureBuilt();
        State = BubbleState.Happy;
        SetIcon(SmileySpriteResolved);
        SetShown(true);
    }

    /// <summary>Đổi icon thành MẶT TỨC GIẬN (hết kiên nhẫn) — agent giữ 2s rồi cho khách về.</summary>
    public void ShowAngry()
    {
        EnsureBuilt();
        State = BubbleState.Angry;
        SetIcon(angryFaceSprite != null ? angryFaceSprite : GetPlaceholderFace(FaceKind.Angry));
        if (_root == null || !_root.gameObject.activeSelf) PlayChainScaleIn();
        else SetShown(true);
    }

    /// <summary>Đóng bubble (khách rời hàng / despawn).</summary>
    public void Hide()
    {
        State = BubbleState.Hidden;
        if (_animRoutine != null) { StopCoroutine(_animRoutine); _animRoutine = null; }
        if (_root != null) _root.gameObject.SetActive(false);
    }

    // ─── Dựng hierarchy con ─────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_root != null) return;

        var rootGo = new GameObject("Bubble");
        rootGo.transform.SetParent(transform, false);
        rootGo.transform.localPosition = Vector3.zero;
        rootGo.transform.localScale    = Vector3.one;
        _root = rootGo.transform;

        string layer = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Overlay);

        Sprite circleSprite = dotSprite != null ? dotSprite : GetPlaceholderFace(FaceKind.Dot);
        Sprite cloudSprite  = frameSprite != null ? frameSprite : GetPlaceholderFace(FaceKind.Cloud);

        // 1 · Dot 1 (chấm nhỏ)
        var dot1Go = new GameObject("Dot1");
        dot1Go.transform.SetParent(_root, false);
        _dot1Tr = dot1Go.transform;
        _dot1Sr = dot1Go.AddComponent<SpriteRenderer>();
        _dot1Sr.sprite           = circleSprite;
        _dot1Sr.sortingLayerName = layer;
        _dot1Sr.sortingOrder     = frameSortingOrder;

        // 2 · Dot 2 (chấm vừa)
        var dot2Go = new GameObject("Dot2");
        dot2Go.transform.SetParent(_root, false);
        _dot2Tr = dot2Go.transform;
        _dot2Sr = dot2Go.AddComponent<SpriteRenderer>();
        _dot2Sr.sprite           = circleSprite;
        _dot2Sr.sortingLayerName = layer;
        _dot2Sr.sortingOrder     = frameSortingOrder;

        // 3 · Frame (Đám mây lớn)
        var frameGo = new GameObject("Frame");
        frameGo.transform.SetParent(_root, false);
        _frameTr = frameGo.transform;
        _frameSr = frameGo.AddComponent<SpriteRenderer>();
        _frameSr.sprite           = cloudSprite;
        _frameSr.sortingLayerName = layer;
        _frameSr.sortingOrder     = frameSortingOrder;

        // 4 · Icon món / biểu cảm
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(_frameTr, false);
        iconGo.transform.localPosition = Vector3.zero;
        _iconSr = iconGo.AddComponent<SpriteRenderer>();
        _iconSr.sortingLayerName = layer;
        _iconSr.sortingOrder     = frameSortingOrder + 1;

        _floatSeed = Random.Range(0f, 100f);
        LayoutInWorldUnits();
        _root.gameObject.SetActive(false);
    }

    private void SetIcon(Sprite sprite)
    {
        if (_iconSr == null) return;
        _iconSr.sprite = sprite;
        _iconSr.color  = Color.white;
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.y);
        _baseIconScale = CalcLocalScale(_iconSr, iconWorldSize / parentScale);
        _iconSr.transform.localScale = Vector3.one * _baseIconScale;
    }

    private void LayoutInWorldUnits()
    {
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.y);

        // Tự động nâng cấp an toàn nếu offset Y cũ còn thấp (< 190f) hoặc size còn nhỏ (< 130f)
        Vector3 mainOffset = worldOffset;
        if (mainOffset.y < 190f || frameWorldSize < 130f)
        {
            mainOffset = new Vector3(30f, 276f, 0f);
        }

        Vector3 d1Offset = dot1Offset.y > 10f ? dot1Offset : new Vector3(12f, 176f, 0f);
        Vector3 d2Offset = dot2Offset.y > 10f ? dot2Offset : new Vector3(20f, 208f, 0f);

        float curFrameSize = Mathf.Max(frameWorldSize, 160f);
        float curIconSize  = Mathf.Max(iconWorldSize, 105f);

        _baseDot1Pos  = d1Offset / parentScale;
        _baseDot2Pos  = d2Offset / parentScale;
        _baseFramePos = mainOffset / parentScale;

        if (_dot1Tr != null)  _dot1Tr.localPosition  = _baseDot1Pos;
        if (_dot2Tr != null)  _dot2Tr.localPosition  = _baseDot2Pos;
        if (_frameTr != null) _frameTr.localPosition = _baseFramePos;

        _baseDot1Scale  = CalcLocalScale(_dot1Sr,  dot1Size     / parentScale);
        _baseDot2Scale  = CalcLocalScale(_dot2Sr,  dot2Size     / parentScale);
        _baseFrameScale = CalcLocalScale(_frameSr, curFrameSize / parentScale);
        _baseIconScale  = CalcLocalScale(_iconSr,  curIconSize  / parentScale);

        SetAllScales(1f, 1f, 1f);
    }

    private static float CalcLocalScale(SpriteRenderer sr, float targetLocalSize)
    {
        if (sr == null || sr.sprite == null) return 1f;
        float native = Mathf.Max(sr.sprite.rect.width, sr.sprite.rect.height) / sr.sprite.pixelsPerUnit;
        if (native <= 0.0001f) return 1f;
        return targetLocalSize / native;
    }

    // ─── Animation Chuỗi Pop Tuần Tự (Staggered Pop + Idle Float) ────────

    private void PlayChainScaleIn()
    {
        SetShown(true);
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        if (!isActiveAndEnabled)
        {
            SetAllScales(1f, 1f, 1f);
            return;
        }
        _animRoutine = StartCoroutine(ChainPopInAndFloatRoutine());
    }

    private void SetShown(bool shown)
    {
        if (_root != null && _root.gameObject.activeSelf != shown)
            _root.gameObject.SetActive(shown);
    }

    private void SetAllScales(float progressDot1, float progressDot2, float progressFrame)
    {
        if (_dot1Tr != null)  _dot1Tr.localScale  = Vector3.one * (_baseDot1Scale * progressDot1);
        if (_dot2Tr != null)  _dot2Tr.localScale  = Vector3.one * (_baseDot2Scale * progressDot2);
        if (_frameTr != null) _frameTr.localScale = Vector3.one * (_baseFrameScale * progressFrame);
    }

    private IEnumerator ChainPopInAndFloatRoutine()
    {
        SetAllScales(0f, 0f, 0f);

        float totalDuration = Mathf.Max(0.2f, _scaleInSeconds);
        float popTimeDot1   = totalDuration * 0.35f;
        float popTimeDot2   = totalDuration * 0.40f;
        float popTimeFrame  = totalDuration * 0.60f;

        float delayDot2     = totalDuration * 0.15f;
        float delayFrame    = totalDuration * 0.30f;

        float timer = 0f;
        float totalPopPhase = delayFrame + popTimeFrame;

        while (timer < totalPopPhase)
        {
            timer += Time.deltaTime;

            // Dot 1 (nhỏ)
            float t1 = Mathf.Clamp01(timer / popTimeDot1);
            float s1 = EaseOutBack(t1, 1.4f);

            // Dot 2 (vừa)
            float t2 = Mathf.Clamp01((timer - delayDot2) / popTimeDot2);
            float s2 = timer >= delayDot2 ? EaseOutBack(t2, 1.35f) : 0f;

            // Frame (Đám mây lớn)
            float tF = Mathf.Clamp01((timer - delayFrame) / popTimeFrame);
            float sF = timer >= delayFrame ? EaseOutBack(tF, 1.25f) : 0f;

            SetAllScales(s1, s2, sF);
            yield return null;
        }

        SetAllScales(1f, 1f, 1f);

        // ─── Floating Loop (nhấp nhô nhẹ nhàng) ─────────────
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.y);
        float bobAmplitude = 2.6f / parentScale;
        float bobSpeed     = 2.5f;

        while (true)
        {
            float timeVal = Time.time * bobSpeed + _floatSeed;
            float offsetF = Mathf.Sin(timeVal) * bobAmplitude;
            float offset2 = Mathf.Sin(timeVal - 0.3f) * (bobAmplitude * 0.5f);
            float offset1 = Mathf.Sin(timeVal - 0.6f) * (bobAmplitude * 0.25f);

            if (_frameTr != null) _frameTr.localPosition = _baseFramePos + new Vector3(0f, offsetF, 0f);
            if (_dot2Tr != null)  _dot2Tr.localPosition  = _baseDot2Pos  + new Vector3(0f, offset2, 0f);
            if (_dot1Tr != null)  _dot1Tr.localPosition  = _baseDot1Pos  + new Vector3(0f, offset1, 0f);

            yield return null;
        }
    }

    private static float EaseOutBack(float x, float s)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return 1f + (s + 1f) * Mathf.Pow(x - 1f, 3f) + s * Mathf.Pow(x - 1f, 2f);
    }

    // ─── Procedural Thought Cloud & Faces ────────────────────────────────

    private enum FaceKind { Plain, Dot, Cloud, Happy, Angry }

    private static readonly Dictionary<FaceKind, Sprite> _placeholderCache =
        new Dictionary<FaceKind, Sprite>();

    /// <summary>
    /// Sinh (và cache) sprite comic thought cloud / dot / faces với viền nâu hoạt hình (#482914).
    /// </summary>
    private static Sprite GetPlaceholderFace(FaceKind kind)
    {
        Sprite cached;
        if (_placeholderCache.TryGetValue(kind, out cached) && cached != null) return cached;

        const int size = 256; // Tăng lên 256x256 để đám mây to rộng mịn màng
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color32 borderDark = new Color32(72, 41, 20, 255);    // Nâu đậm cartoon #482914
        Color32 matDark    = new Color32(56, 32, 16, 255);
        Color32 nen;
        switch (kind)
        {
            case FaceKind.Happy: nen = new Color32(255, 218, 70, 255);  break;
            case FaceKind.Angry: nen = new Color32(238, 78, 62, 255);   break;
            default:             nen = new Color32(255, 253, 248, 255); break; // Trắng kem sữa
        }

        var px = new Color32[size * size];
        const float tamX = size * 0.5f, tamY = size * 0.5f;

        if (kind == FaceKind.Cloud || kind == FaceKind.Plain)
        {
            // ── VẼ ĐÁM MÂY SUY NGHĨ TO RỘNG (THOUGHT CLOUD BUBBLE) ──
            // Thân đám mây rộng rãi ở giữa để món ăn hiện to và rõ nét
            Vector3[] lobes = new Vector3[]
            {
                new Vector3(tamX,        tamY,        86f), // Thân chính giữa rất rộng
                new Vector3(tamX - 60f,  tamY + 30f,  52f), // Múi trên trái
                new Vector3(tamX + 4f,   tamY + 48f,  54f), // Múi trên giữa
                new Vector3(tamX + 60f,  tamY + 30f,  52f), // Múi trên phải
                new Vector3(tamX - 76f,  tamY - 15f,  48f), // Múi hông trái
                new Vector3(tamX + 76f,  tamY - 15f,  48f), // Múi hông phải
                new Vector3(tamX - 36f,  tamY - 42f,  50f), // Múi dưới trái
                new Vector3(tamX + 36f,  tamY - 42f,  50f), // Múi dưới phải
            };

            const float strokeWidth = 8f; // Viền đậm cartoon 8px trên 256px

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float pxF = x + 0.5f, pyF = y + 0.5f;

                    // Tìm khoảng cách nhỏ nhất tới biên đám mây (SDF âm = bên trong)
                    float minSignedDist = float.MaxValue;
                    for (int i = 0; i < lobes.Length; i++)
                    {
                        float d = KhoangCach(pxF, pyF, lobes[i].x, lobes[i].y) - lobes[i].z;
                        if (d < minSignedDist) minSignedDist = d;
                    }

                    if (minSignedDist > 0.8f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float alpha = Mathf.Clamp01(0.8f - minSignedDist);
                    Color32 c = minSignedDist > -strokeWidth ? borderDark : nen;
                    c.a = (byte)(255 * alpha);

                    px[y * size + x] = c;
                }
            }
        }
        else
        {
            // ── VẼ CHẤM TRÒN / MẶT BIỂU CẢM ──
            const float banKinh = 118f;
            const float doDayVien = 11f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float pxF = x + 0.5f, pyF = y + 0.5f;
                    float d = KhoangCach(pxF, pyF, tamX, tamY);

                    if (d > banKinh + 0.8f)
                    {
                        px[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float alphaEdge = Mathf.Clamp01(banKinh + 0.8f - d);
                    Color32 c = d > banKinh - doDayVien ? borderDark : nen;
                    c.a = (byte)(255 * alphaEdge);

                    if (kind == FaceKind.Happy || kind == FaceKind.Angry)
                    {
                        if (KhoangCach(pxF, pyF, 88f, 160f) <= 15f ||
                            KhoangCach(pxF, pyF, 168f, 160f) <= 15f)
                            c = matDark;

                        if (kind == FaceKind.Happy)
                        {
                            float dm = KhoangCach(pxF, pyF, 128f, 184f);
                            if (y <= 160 && dm >= 56f && dm <= 68f) c = matDark;
                        }
                        else
                        {
                            float dm = KhoangCach(pxF, pyF, 128f, 64f);
                            if (y >= 88 && dm >= 56f && dm <= 68f) c = matDark;

                            if (KhoangCachToiDoan(pxF, pyF, 60f, 200f, 108f, 180f) <= 7f ||
                                KhoangCachToiDoan(pxF, pyF, 196f, 200f, 148f, 180f) <= 7f)
                                c = matDark;
                        }
                    }

                    px[y * size + x] = c;
                }
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _placeholderCache[kind] = sprite;
        return sprite;
    }

    private static float KhoangCach(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx, dy = ay - by;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static float KhoangCachToiDoan(float px, float py, float ax, float ay, float bx, float by)
    {
        float abx = bx - ax, aby = by - ay;
        float len2 = abx * abx + aby * aby;
        if (len2 < 0.0001f) return KhoangCach(px, py, ax, ay);

        float t = ((px - ax) * abx + (py - ay) * aby) / len2;
        t = Mathf.Clamp01(t);
        return KhoangCach(px, py, ax + abx * t, ay + aby * t);
    }
}
