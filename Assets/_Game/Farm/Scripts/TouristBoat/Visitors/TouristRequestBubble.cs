using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BUBBLE MÓN ĂN world-space trên đầu khách du lịch (GDD BOAT-002 §3.3).
///
/// Component nằm trên PREFAB khách (NPCAnimationSetupTool gắn sẵn) — các renderer
/// con (khung + icon) dựng LƯỜI lúc runtime nên prefab không cần child nào trước.
///
/// 3 trạng thái:
///   • Requesting — khung + sprite MÓN ăn khách muốn.
///   • Happy      — mặt cười 0.5s trước khi TouristSmileyFlyFX bay lên HUD.
///   • Angry      — mặt TỨC GIẬN 2s rồi khách bỏ về (hết kiên nhẫn).
///
/// [SẾP CHỐT 2026-08-29] Hết kiên nhẫn hiện **mặt TỨC GIẬN** (angry), KHÔNG phải mặt
/// buồn — state cũ `Sad` đã đổi tên thành <see cref="BubbleState.Angry"/>, field sprite
/// là <c>angryFaceSprite</c>.
///
/// [SẾP CHỐT 2026-08-29] Bubble mở cho MỌI khách trong hàng (lần lượt, cách nhau
/// <c>bubbleStaggerDelay</c>), không còn "chỉ khách đầu hàng" — người chơi nhìn thấy
/// toàn bộ đơn của chuyến để biết cần nấu gì. Thứ tự mở do TouristVisitorManager điều phối.
///
/// Mở bubble: scale-in 0→1 với ease out-back nhẹ, thời gian = config.bubbleScaleInTime.
///
/// Sprite khung / mặt cười / mặt tức giận là field serialize — tool wire nếu tìm thấy
/// art. THIẾU thì fallback tự vẽ placeholder **có màu và nét mặt phân biệt được**
/// (QA m-4): món = tròn TRẮNG · mặt cười = VÀNG cười · mặt tức giận = ĐỎ cau mày —
/// nghiệm thu AC §8.2/§8.5 bằng mắt được ngay khi chưa có art.
/// </summary>
public class TouristRequestBubble : MonoBehaviour
{
    /// <summary>Trạng thái hiển thị của bubble.</summary>
    public enum BubbleState { Hidden, Requesting, Happy, Angry }

    [Header("Art (tool wire — thiếu thì tự vẽ placeholder)")]
    [Tooltip("Sprite khung bubble (nền thoại). Trống → tròn trắng placeholder.")]
    [SerializeField] private Sprite frameSprite;

    [Tooltip("Sprite mặt cười (trạng thái Happy). Trống → mặt cười VÀNG procedural.")]
    [SerializeField] private Sprite smileySprite;

    [Tooltip("Sprite mặt TỨC GIẬN (trạng thái Angry — hết kiên nhẫn). Trống → mặt cau mày ĐỎ procedural.")]
    [SerializeField] private Sprite angryFaceSprite;

    [Header("Bố cục (unit WORLD — tự bù scale của prefab khách)")]
    [Tooltip("Vị trí bubble so với chân khách (pivot Bottom-Center), tính bằng unit world.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 155f, 0f);

    [Tooltip("Cỡ khung bubble (unit world).")]
    [SerializeField] private float frameWorldSize = 90f;

    [Tooltip("Cỡ icon món/mặt trong khung (unit world).")]
    [SerializeField] private float iconWorldSize = 62f;

    [Header("Sorting (nổi trên mọi decor — yêu cầu Sếp)")]
    [SerializeField] private string sortingLayerName = "CongTrinh";
    [SerializeField] private int frameSortingOrder = 20000;

    // ─── Runtime ────────────────────────────────────────────────────────

    public BubbleState State { get; private set; } = BubbleState.Hidden;

    /// <summary>Bubble đang HỎI MÓN (khách sẵn sàng nhận món) — manager/agent kiểm trước khi cho giao.</summary>
    public bool IsRequesting => State == BubbleState.Requesting;

    /// <summary>Sprite mặt cười đang dùng (art hoặc placeholder) — FX bay lên HUD dùng đúng sprite này.</summary>
    public Sprite SmileySpriteResolved =>
        smileySprite != null ? smileySprite : GetPlaceholderFace(FaceKind.Happy);

    private float _scaleInSeconds = 0.25f; // config.bubbleScaleInTime — agent bơm qua Configure

    private Transform      _root;      // node con "Bubble" — scale-in đánh vào đây
    private SpriteRenderer _frameSr;
    private SpriteRenderer _iconSr;
    private Coroutine      _showRoutine;

    /// <summary>Agent bơm số từ TouristBoatConfig (bubbleScaleInTime) lúc Init.</summary>
    public void Configure(float scaleInSeconds)
    {
        if (scaleInSeconds > 0.01f) _scaleInSeconds = scaleInSeconds;
    }

    // ─── API cho TouristAgent ───────────────────────────────────────────

    /// <summary>Mở bubble hiện MÓN khách muốn.</summary>
    public void ShowRequest(Sprite dishSprite)
    {
        EnsureBuilt();
        State = BubbleState.Requesting;
        SetIcon(dishSprite != null ? dishSprite : GetPlaceholderFace(FaceKind.Plain));
        PlayScaleIn();
    }

    /// <summary>Đổi icon thành MẶT CƯỜI (đã giao món) — agent chờ 0.5s rồi bắn SmileyFlyFX.</summary>
    public void ShowHappy()
    {
        EnsureBuilt();
        State = BubbleState.Happy;
        SetIcon(SmileySpriteResolved);
        SetShown(true); // bubble đang mở sẵn — chỉ đổi mặt, không scale-in lại
    }

    /// <summary>
    /// Đổi icon thành MẶT TỨC GIẬN (hết kiên nhẫn) — agent giữ 2s rồi cho khách về,
    /// KHÔNG thưởng. [Sếp chốt: tức giận, không phải buồn.]
    /// </summary>
    public void ShowAngry()
    {
        EnsureBuilt();
        State = BubbleState.Angry;
        SetIcon(angryFaceSprite != null ? angryFaceSprite : GetPlaceholderFace(FaceKind.Angry));
        // Khách bị ép rời (lưới an toàn) có thể chưa từng mở bubble → scale-in cho thấy rõ.
        if (_root == null || !_root.gameObject.activeSelf) PlayScaleIn();
        else SetShown(true);
    }

    /// <summary>Đóng bubble (khách rời hàng / despawn).</summary>
    public void Hide()
    {
        State = BubbleState.Hidden;
        if (_showRoutine != null) { StopCoroutine(_showRoutine); _showRoutine = null; }
        if (_root != null) _root.gameObject.SetActive(false);
    }

    // ─── Dựng hierarchy con (lười, 1 lần) ───────────────────────────────

    private void EnsureBuilt()
    {
        if (_root != null) return;

        var rootGo = new GameObject("Bubble");
        rootGo.transform.SetParent(transform, false);
        _root = rootGo.transform;

        var frameGo = new GameObject("Frame");
        frameGo.transform.SetParent(_root, false);
        _frameSr = frameGo.AddComponent<SpriteRenderer>();
        _frameSr.sprite           = frameSprite != null ? frameSprite : GetPlaceholderFace(FaceKind.Plain);
        _frameSr.sortingLayerName = sortingLayerName;
        _frameSr.sortingOrder     = frameSortingOrder;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(_root, false);
        _iconSr = iconGo.AddComponent<SpriteRenderer>();
        _iconSr.sortingLayerName = sortingLayerName;
        _iconSr.sortingOrder     = frameSortingOrder + 1;

        LayoutInWorldUnits();
        _root.gameObject.SetActive(false);
    }

    /// <summary>Gán icon + canh lại cỡ (sprite mới có thể khác kích thước gốc).</summary>
    private void SetIcon(Sprite sprite)
    {
        if (_iconSr == null) return;
        _iconSr.sprite = sprite;
        _iconSr.color  = Color.white; // màu nằm TRONG texture placeholder, không tint ở đây
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.y);
        ApplyWorldSize(_iconSr, iconWorldSize / parentScale);
    }

    /// <summary>
    /// Đặt vị trí + cỡ theo UNIT WORLD, tự bù lossyScale của prefab khách.
    /// Prefab NV scale ~66 (xem NPCAnimationSetupTool) nên child KHÔNG thể dùng
    /// local unit trực tiếp — cùng bài học ApplySpriteSize của TouristBoatSetupTool.
    /// </summary>
    private void LayoutInWorldUnits()
    {
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.y);

        _root.localPosition = worldOffset / parentScale;
        _root.localScale    = Vector3.one;

        ApplyWorldSize(_frameSr, frameWorldSize / parentScale);
        ApplyWorldSize(_iconSr,  iconWorldSize  / parentScale);
    }

    /// <summary>Scale 1 renderer con để sprite hiện đúng cỡ (theo local unit đã bù scale cha).</summary>
    private static void ApplyWorldSize(SpriteRenderer sr, float targetLocalSize)
    {
        if (sr == null || sr.sprite == null) return;
        float native = Mathf.Max(sr.sprite.rect.width, sr.sprite.rect.height) / sr.sprite.pixelsPerUnit;
        if (native <= 0.0001f) return;
        float k = targetLocalSize / native;
        sr.transform.localScale = new Vector3(k, k, 1f);
    }

    // ─── Scale-in ───────────────────────────────────────────────────────

    private void PlayScaleIn()
    {
        SetShown(true);
        if (_showRoutine != null) StopCoroutine(_showRoutine);
        if (!isActiveAndEnabled) return; // agent đang tắt — hiện thẳng, không tween
        _showRoutine = StartCoroutine(ScaleInRoutine());
    }

    private void SetShown(bool shown)
    {
        if (_root != null && _root.gameObject.activeSelf != shown)
            _root.gameObject.SetActive(shown);
    }

    /// <summary>Scale-in 0→1 với ease OUT-BACK nhẹ (vọt quá ~1.07 rồi lún về) — "mở mượt" GDD §3.3.</summary>
    private IEnumerator ScaleInRoutine()
    {
        float t = 0f;
        float duration = Mathf.Max(0.05f, _scaleInSeconds);
        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duration);
            // Out-back: f(x) = 1 + (s+1)(x-1)^3 + s(x-1)^2, s nhỏ cho nảy nhẹ
            const float s = 1.2f;
            float e = 1f + (s + 1f) * Mathf.Pow(x - 1f, 3f) + s * Mathf.Pow(x - 1f, 2f);
            _root.localScale = Vector3.one * e;
            yield return null;
        }
        _root.localScale = Vector3.one;
        _showRoutine = null;
    }

    // ─── Placeholder procedural (QA m-4) ────────────────────────────────

    /// <summary>Loại mặt placeholder — mỗi loại một MÀU + NÉT MẶT riêng để phân biệt bằng mắt.</summary>
    private enum FaceKind { Plain, Happy, Angry }

    private static readonly Dictionary<FaceKind, Sprite> _placeholderCache =
        new Dictionary<FaceKind, Sprite>();
    private static bool _warnedPlaceholder;

    /// <summary>
    /// Sinh (và cache) sprite placeholder cho từng loại mặt.
    ///   Plain = tròn TRẮNG (khung bubble / món chưa có sprite)
    ///   Happy = tròn VÀNG + 2 mắt + miệng cười
    ///   Angry = tròn ĐỎ  + 2 mắt + miệng cau + 2 lông mày chéo xuống
    /// QA m-4: bản trước bỏ qua tham số màu nên 3 trạng thái trông y hệt nhau
    /// ⇒ không nghiệm thu được AC §8.2/§8.5 bằng mắt khi chưa có art.
    /// Cảnh báo Console chỉ in ĐÚNG 1 LẦN cho cả phiên chơi.
    /// </summary>
    private static Sprite GetPlaceholderFace(FaceKind kind)
    {
        Sprite cached;
        if (_placeholderCache.TryGetValue(kind, out cached) && cached != null) return cached;

        if (!_warnedPlaceholder)
        {
            _warnedPlaceholder = true;
            Debug.LogWarning("[TouristVisitor] Bubble chưa được wire đủ sprite (khung / mặt cười / mặt tức giận) — " +
                             "dùng mặt placeholder tự vẽ (trắng / vàng cười / đỏ cau mày). " +
                             "Gắn art vào prefab khách rồi chạy lại tool là hết cảnh báo.");
        }

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color32 mat  = new Color32(40, 32, 28, 255);   // mắt / miệng / lông mày
        Color32 nen;
        switch (kind)
        {
            case FaceKind.Happy: nen = new Color32(255, 214, 64, 255);  break; // VÀNG
            case FaceKind.Angry: nen = new Color32(232, 72, 56, 255);   break; // ĐỎ
            default:             nen = new Color32(255, 255, 255, 255); break; // TRẮNG
        }
        Color32 vien   = new Color32((byte)(nen.r * 0.72f), (byte)(nen.g * 0.72f), (byte)(nen.b * 0.72f), 255);
        Color32 trong  = new Color32(0, 0, 0, 0);

        var px = new Color32[size * size];
        const float tamX = size * 0.5f, tamY = size * 0.5f;
        const float banKinh = 30f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float pxF = x + 0.5f, pyF = y + 0.5f;
                float d = KhoangCach(pxF, pyF, tamX, tamY);

                if (d > banKinh) { px[y * size + x] = trong; continue; }

                Color32 c = d > banKinh - 3f ? vien : nen;

                if (kind != FaceKind.Plain)
                {
                    // Hai mắt
                    if (KhoangCach(pxF, pyF, 22f, 40f) <= 4f ||
                        KhoangCach(pxF, pyF, 42f, 40f) <= 4f)
                        c = mat;

                    if (kind == FaceKind.Happy)
                    {
                        // Miệng CƯỜI: cung DƯỚI của đường tròn có tâm nằm TRÊN miệng
                        float dm = KhoangCach(pxF, pyF, 32f, 46f);
                        if (y <= 40 && dm >= 14f && dm <= 16.5f) c = mat;
                    }
                    else
                    {
                        // Miệng CAU: cung TRÊN của đường tròn có tâm nằm DƯỚI miệng
                        float dm = KhoangCach(pxF, pyF, 32f, 16f);
                        if (y >= 22 && dm >= 14f && dm <= 16.5f) c = mat;

                        // Hai lông mày chéo xuống giữa — nét "tức giận" (Sếp chốt)
                        if (KhoangCachToiDoan(pxF, pyF, 15f, 50f, 27f, 45f) <= 1.9f ||
                            KhoangCachToiDoan(pxF, pyF, 49f, 50f, 37f, 45f) <= 1.9f)
                            c = mat;
                    }
                }

                px[y * size + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false, true);

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _placeholderCache[kind] = sprite;
        return sprite;
    }

    /// <summary>Khoảng cách 2 điểm — toán vô hướng thuần, không cấp phát struct.</summary>
    private static float KhoangCach(float ax, float ay, float bx, float by)
    {
        float dx = ax - bx, dy = ay - by;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Khoảng cách từ điểm tới đoạn thẳng — dùng vẽ lông mày dày ~2px.</summary>
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
