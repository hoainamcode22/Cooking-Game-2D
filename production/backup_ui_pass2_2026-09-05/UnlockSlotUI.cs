using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một ô icon "vừa mở khoá" trong popup lên cấp (phong cách Township):
/// khung tròn viền kem + icon bên trong + nhãn NEW đỏ nghiêng ở góc dưới-trái.
///
/// Prefab do <c>LevelUpPopupTownshipTool</c> sinh ra. Dùng lại được cho bất kỳ
/// danh sách phần thưởng / vật phẩm mở khoá nào.
/// </summary>
public class UnlockSlotUI : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private Image           iconImage;
    [SerializeField] private Image           ringImage;
    [SerializeField] private GameObject      newTagRoot;
    [SerializeField] private TextMeshProUGUI captionText;

    [Header("Animation xuất hiện")]
    [Tooltip("Độ trễ giữa các ô khi hiện lần lượt (giây).")]
    public float staggerDelay = 0.06f;
    [Tooltip("Thời gian phóng to vào chỗ (giây).")]
    public float popDuration  = 0.32f;

    private RectTransform _rt;
    private Coroutine     _popRoutine;

    // [R2 GỘP Q1 — 2026-09-05] Màu tag chuẩn dùng chung: đỏ "MỚI/NEW" cho ô mở khoá,
    // cam "×N" cho ô quà — Sếp chốt "mọi ô đều có tag", chỉ khác chữ/màu theo nguồn data.
    public static readonly Color TagColorNew  = new Color32(230,  60,  55, 255);
    public static readonly Color TagColorGift = new Color32(255, 152,   0, 255);

    // [R2 GỘP] Scale "nền" do khu phần thưởng gộp quyết định (co ô khi đông cell).
    // Animation PopRoutine NHÂN với scale này thay vì ghi đè cứng Vector3.one —
    // nếu không, ô mở khoá sẽ phình về 190px giữa lúc cả dải đã co còn 0.8x.
    private Vector3 _baseScale = Vector3.one;

    private void Awake() => _rt = transform as RectTransform;

    /// <summary>
    /// [R2 GỘP] LevelUpPopupUI gọi khi xếp khu phần thưởng gộp: đặt scale chuẩn của ô.
    /// Đang KHÔNG chạy animation thì áp ngay; đang pop thì PopRoutine tự nhân theo.
    /// </summary>
    public void SetBaseScale(float k)
    {
        _baseScale = new Vector3(k, k, 1f);
        if (_rt == null) _rt = transform as RectTransform;
        if (_popRoutine == null && _rt != null) _rt.localScale = _baseScale;
    }

    /// <summary>Sprite icon đang gắn (null = ô chưa có icon). Dùng cho tool kiểm tra / báo cáo QA.</summary>
    public Sprite CurrentIcon => iconImage != null ? iconImage.sprite : null;

    /// <summary>TRUE khi ô thật sự đang vẽ được một icon (có sprite VÀ Image đang bật).</summary>
    public bool HasIcon => iconImage != null && iconImage.sprite != null && iconImage.enabled;

    /// <summary>Gán nội dung cho ô. Tự suy tagText = "MỚI"/"NEW" (theo ngôn ngữ), màu đỏ chuẩn.</summary>
    /// <param name="icon">Sprite hiển thị. Null → ẩn icon, chỉ còn khung.</param>
    /// <param name="showNewTag">Hiện nhãn NEW đỏ.</param>
    /// <param name="caption">Chữ dưới ô. Để trống thì ẩn.</param>
    public void Setup(Sprite icon, bool showNewTag = true, string caption = "")
    {
        string tagText = showNewTag ? (LocalizationManager.DangTiengAnh ? "NEW" : "MỚI") : null;
        SetupCore(icon, tagText, TagColorNew, caption);
    }

    /// <summary>
    /// [R2 GỘP Q1 — 2026-09-05] Overload đầy đủ: tự chọn chữ/màu tag — dùng khi ô KHÔNG PHẢI
    /// luôn là "MỚI" (vd ô quà cần "×N" màu cam thay vì NEW đỏ). Lệnh Sếp 05/09: "mọi ô đều
    /// có pop + bob + tag + tên", chỉ khác NỘI DUNG tag theo nguồn data (unlockEntries vs giftItems).
    /// </summary>
    /// <param name="icon">Sprite hiển thị. Null → ẩn icon, chỉ còn khung.</param>
    /// <param name="tagText">Chữ trên tag (vd "MỚI", "NEW", "×3"). Null/rỗng → ẩn tag.</param>
    /// <param name="tagColor">Màu nền tag.</param>
    /// <param name="caption">Chữ dưới ô (tên vật phẩm). Để trống thì ẩn.</param>
    public void Setup(Sprite icon, string tagText, Color tagColor, string caption)
    {
        SetupCore(icon, tagText, tagColor, caption);
    }

    private void SetupCore(Sprite icon, string tagText, Color tagColor, string caption)
    {
        // ── CHỐT: trả kích thước về 1 trước mọi thứ ──────────────────────────
        // Nếu ô bị SetActive(false) ĐANG GIỮA animation PopRoutine, Unity huỷ coroutine
        // ngay tại chỗ → localScale kẹt ở ~0. Lần popup sau bật lại ô đó, nếu PlayPop
        // không chạy được thì ô VÔ HÌNH dù đã bật. Reset ở đây bảo đảm ô luôn nhìn thấy.
        if (_rt == null) _rt = transform as RectTransform;
        if (_popRoutine != null) { StopCoroutine(_popRoutine); _popRoutine = null; }
        if (_rt != null) _rt.localScale = _baseScale;   // [R2 GỘP] tôn trọng scale khu gộp

        if (ringImage != null)
            ringImage.enabled = true;   // khung luôn hiện, kể cả khi chưa có icon

        if (iconImage != null)
        {
            iconImage.sprite  = icon;
            iconImage.enabled = icon != null;
            // preserveAspect tránh icon chữ nhật bị bóp méo trong khung tròn
            iconImage.preserveAspect = true;
            // Trả màu về trắng: nếu ai đó tint ô này ở lần dùng trước (hoặc trong prefab)
            // thì icon thật sẽ bị nhuộm sai màu / trong suốt.
            iconImage.color = Color.white;
        }

        if (newTagRoot != null)
        {
            bool showTag = !string.IsNullOrEmpty(tagText);
            newTagRoot.SetActive(showTag);
            if (showTag)
            {
                var txt = newTagRoot.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) txt.text = tagText;

                var tagBg = newTagRoot.GetComponent<Image>() ?? newTagRoot.GetComponentInChildren<Image>(true);
                if (tagBg != null) tagBg.color = tagColor;
            }
        }

        // [R2 GỘP Q1] Tool dựng scene (LevelUpPopupTownshipTool.BuildUnlockSlot) hiện gọi
        // EditorBind(icon, ring, tag, null) — captionText LUÔN null cho 9 ô đã dựng sẵn trong
        // scene cũ. Không được sửa tool/scene (ngoài phạm vi file cho phép) → TỰ TẠO 1 TMP nhỏ
        // dưới ô ngay lần đầu cần hiện caption, rồi cache lại để dùng cho các lần sau.
        if (captionText == null && !string.IsNullOrWhiteSpace(caption))
            captionText = CreateCaptionTextRuntime();

        if (captionText != null)
        {
            bool has = !string.IsNullOrWhiteSpace(caption);
            captionText.gameObject.SetActive(has);
            if (has) captionText.text = caption;
        }

        StartBobbing();
    }

    /// <summary>[R2 GỘP Q1] Tự tạo caption TMP runtime khi prefab/scene chưa có sẵn — xem chú
    /// thích ở SetupCore(). Neo giữa-dưới, ngay dưới vòng viền tròn; chỉ tạo 1 lần rồi cache.</summary>
    private TextMeshProUGUI CreateCaptionTextRuntime()
    {
        if (_rt == null) _rt = transform as RectTransform;
        float w = _rt != null ? _rt.rect.width  : 190f;
        float h = _rt != null ? _rt.rect.height : 190f;

        var go = new GameObject("Caption_TuTao", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -(h * 0.5f) - 4f);
        rt.sizeDelta = new Vector2(w + 24f, 30f);

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize          = 20;
        txt.color             = new Color32(70, 45, 20, 255);
        txt.alignment         = TextAlignmentOptions.Center;
        txt.textWrappingMode  = TextWrappingModes.Normal;
        txt.overflowMode      = TextOverflowModes.Ellipsis;
        txt.raycastTarget     = false;
        return txt;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // [R2 GỘP Q1 / B4 — 2026-09-05] Ô DỰNG RUNTIME cho vàng / kim cương / quà
    // Sếp chốt: MỌI ô phần thưởng đều đi chung một đường vẽ (pop + bob + tag + tên).
    // LevelUpPopupUI không thể gán field private của ô → cung cấp hàm dựng ở đây.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>TRUE nếu ô do LevelUpPopupUI tự dựng runtime (vàng/gem/quà) — KHÔNG phải
    /// 9 ô mở khoá của scene. ResolveUnlockSlots phải bỏ qua các ô này để không nạp nhầm.</summary>
    public bool IsRuntimeCell { get; private set; }

    /// <summary>Icon null mà vẫn muốn ô có "đĩa màu" theo id (thay ô trống câm).</summary>
    public void ShowPlaceholderTint(Color color)
    {
        if (iconImage == null) return;
        iconImage.sprite  = null;
        iconImage.color   = color;
        iconImage.enabled = true;
    }

    /// <summary>
    /// Dựng một ô phần thưởng runtime dưới <paramref name="parent"/>, giống hệt ô mở khoá của
    /// scene (sao chép sprite/màu vòng viền + hình dáng tag từ <paramref name="mau"/> nếu có;
    /// không có mẫu thì dùng UIStandardSprites.SlotNormal). Caption để null — SetupCore tự tạo.
    /// Gọi <see cref="Setup(Sprite,string,Color,string)"/> ngay sau để đổ nội dung.
    /// </summary>
    public static UnlockSlotUI CreateRuntimeCell(Transform parent, string name, UnlockSlotUI mau, float size = 190f)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);

        // 1 · Vòng viền tròn phủ cả ô
        var ringGO = new GameObject("Vong_Vien", typeof(RectTransform));
        var ringRT = (RectTransform)ringGO.transform;
        ringRT.SetParent(rt, false);
        ringRT.anchorMin = Vector2.zero; ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = ringRT.offsetMax = Vector2.zero;
        var ring = ringGO.AddComponent<Image>();
        ring.raycastTarget = false;
        if (mau != null && mau.ringImage != null)
        {
            ring.sprite         = mau.ringImage.sprite;
            ring.color          = mau.ringImage.color;
            ring.type           = mau.ringImage.type;
            ring.preserveAspect = mau.ringImage.preserveAspect;
        }
        else
        {
            ring.sprite = UIStandardSprites.SlotNormal;
            ring.type   = Image.Type.Sliced;
            ring.color  = new Color32(252, 246, 235, 255);
        }

        // 2 · Icon giữa ô
        var iconGO = new GameObject("Icon", typeof(RectTransform));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.pivot     = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = new Vector2(0f, 6f);
        iconRT.sizeDelta = new Vector2(size - 56f, size - 56f);
        var icon = iconGO.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget  = false;

        // 3 · Tag (pill) — mặc định giữa-dưới; có mẫu thì sao chép đúng vị trí/độ nghiêng
        var tagGO = new GameObject("Nhan_Tag", typeof(RectTransform));
        var tagRT = (RectTransform)tagGO.transform;
        tagRT.SetParent(rt, false);
        tagRT.anchorMin = tagRT.anchorMax = new Vector2(0.5f, 0f);
        tagRT.pivot     = new Vector2(0.5f, 0.5f);
        tagRT.anchoredPosition = new Vector2(0f, 18f);
        tagRT.sizeDelta = new Vector2(106f, 38f);
        var tagBg = tagGO.AddComponent<Image>();
        tagBg.raycastTarget = false;
        tagBg.type          = Image.Type.Sliced;
        if (mau != null && mau.newTagRoot != null)
        {
            var mauRT  = mau.newTagRoot.transform as RectTransform;
            var mauImg = mau.newTagRoot.GetComponent<Image>() ?? mau.newTagRoot.GetComponentInChildren<Image>(true);
            if (mauRT != null)
            {
                tagRT.anchorMin = mauRT.anchorMin; tagRT.anchorMax = mauRT.anchorMax;
                tagRT.pivot     = mauRT.pivot;
                tagRT.anchoredPosition = mauRT.anchoredPosition;
                tagRT.sizeDelta = mauRT.sizeDelta;
                tagRT.localEulerAngles = mauRT.localEulerAngles;
            }
            if (mauImg != null) { tagBg.sprite = mauImg.sprite; tagBg.type = mauImg.type; }
        }
        if (tagBg.sprite == null)
            tagBg.sprite = Resources.Load<Sprite>("UI_LevelUp/spr_white_round") ?? Resources.Load<Sprite>("spr_white_round");

        var tagTxtGO = new GameObject("Chu", typeof(RectTransform));
        var tagTxtRT = (RectTransform)tagTxtGO.transform;
        tagTxtRT.SetParent(tagRT, false);
        tagTxtRT.anchorMin = Vector2.zero; tagTxtRT.anchorMax = Vector2.one;
        tagTxtRT.offsetMin = tagTxtRT.offsetMax = Vector2.zero;
        var tagTxt = tagTxtGO.AddComponent<TextMeshProUGUI>();
        tagTxt.fontSize      = 22;
        tagTxt.fontStyle     = FontStyles.Bold;
        tagTxt.color         = Color.white;
        tagTxt.alignment     = TextAlignmentOptions.Center;
        tagTxt.raycastTarget = false;

        var slot = go.AddComponent<UnlockSlotUI>();
        slot.iconImage   = icon;
        slot.ringImage   = ring;
        slot.newTagRoot  = tagGO;
        slot.captionText = null;            // SetupCore tự tạo caption khi có chữ
        slot.IsRuntimeCell = true;
        slot._rt = rt;
        return slot;
    }

    private Coroutine _bobRoutine;
    private Vector3 _iconBasePos = Vector3.zero;

    private void StartBobbing()
    {
        if (_bobRoutine != null) StopCoroutine(_bobRoutine);
        if (iconImage != null && isActiveAndEnabled)
        {
            _iconBasePos = iconImage.transform.localPosition;
            _bobRoutine = StartCoroutine(BobbingRoutine());
        }
    }

    private System.Collections.IEnumerator BobbingRoutine()
    {
        float offset = Random.Range(0f, Mathf.PI * 2f);
        while (true)
        {
            if (iconImage == null) yield break;
            float bob = Mathf.Sin(Time.unscaledTime * 3.5f + offset) * 6f;
            iconImage.transform.localPosition = _iconBasePos + new Vector3(0f, bob, 0f);
            yield return null;
        }
    }

    private void OnDisable()
    {
        if (_bobRoutine != null) { StopCoroutine(_bobRoutine); _bobRoutine = null; }
        if (iconImage != null) iconImage.transform.localPosition = _iconBasePos;
    }

    /// <summary>Cho ô "bật" ra với hiệu ứng nảy, trễ theo thứ tự index.</summary>
    public void PlayPop(int index)
    {
        if (_rt == null) _rt = transform as RectTransform;

        if (!isActiveAndEnabled)
        {
            if (_rt != null) _rt.localScale = _baseScale;   // [R2 GỘP]
            return;
        }

        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopRoutine(index * staggerDelay));
    }

    private System.Collections.IEnumerator PopRoutine(float delay)
    {
        _rt.localScale = Vector3.zero;

        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        t = 0f;
        while (t < popDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            _rt.localScale = _baseScale * EaseOutBack(k);   // [R2 GỘP] nhân với scale khu gộp
            yield return null;
        }
        _rt.localScale = _baseScale;                        // [R2 GỘP]
        _popRoutine    = null;
        StartBobbing();
    }

    /// <summary>Nảy quá đà rồi ổn định — tạo cảm giác "bung ra" đã tay.</summary>
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float m = x - 1f;
        return 1f + c3 * m * m * m + c1 * m * m;
    }

#if UNITY_EDITOR
    /// <summary>Tool dựng hierarchy gọi để gán tham chiếu lúc sinh prefab.</summary>
    public void EditorBind(Image icon, Image ring, GameObject tag, TextMeshProUGUI caption)
    {
        iconImage   = icon;
        ringImage   = ring;
        newTagRoot  = tag;
        captionText = caption;
    }
#endif
}
