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

    // [V7 — 2026-09-06] Xanh "SẮP MỞ": dùng cho badge "Cấp N" tách ra từ nhãn kiểu
    // "Nhà dân mới sẽ mở ở cấp 3". Mục đó CHƯA mở ở level này, đeo tag "MỚI" đỏ là sai nghĩa.
    public static readonly Color TagColorSoon = new Color32( 70, 130, 200, 255);

    // ═════════════════════════════════════════════════════════════════════════
    // [V7 — 2026-09-06] KHUÔN CHỮ DƯỚI Ô — gom MỘT CHỖ, mọi ô dùng chung số này
    // ─────────────────────────────────────────────────────────────────────────
    //  BUG ĐÃ SỬA (ảnh Sếp chụp 06/09): 3 ô đầu ("Mở khóa hạt Ngô",
    //  "Chuồng gà đã mở bán trong Shop", "Nhà dân mới sẽ mở ở cấp 3") có nhãn dài gấp
    //  3–5 lần các ô còn lại. Hai nguyên nhân ĐO ĐƯỢC trong code:
    //
    //   (1) TRÀN NGANG — CreateCaptionTextRuntime() cũ đặt bề rộng chữ = (bề rộng ô + 24)
    //       = 190 + 24 = 214px, trong khi BƯỚC Ô của flow-layout chỉ là
    //       190 + MERGED_SPACING_X(16) = 206px  →  hai nhãn cạnh nhau ĐÈ LÊN NHAU 8px.
    //       Nay bề rộng chữ = bề rộng ô − 8 = 182px  →  còn hở 24px, không thể chạm.
    //
    //   (2) CHỮ KHÔNG ĐỀU — autosize cũ 12–18 kèm maxVisibleLines = 1: nhãn ngắn
    //       ("Vàng", "Ngô") nằm ở cỡ 18, nhãn dài bị ép xuống sàn 12 rồi "…", nên hàng chữ
    //       lỗ chỗ cao thấp. Nay 20–26 và tối đa 2 dòng: sau khi RutGonNhan() cắt nhãn về
    //       đúng danh từ, MỌI nhãn đều vừa 1 dòng ở cỡ 26  →  hàng chữ đều tăm tắp.
    // ═════════════════════════════════════════════════════════════════════════
    private const float CAPTION_W_MAC_DINH = 190f;  // bề rộng ô chuẩn do tool dựng (SLOT_SIZE)
    private const float CAPTION_THUT_LE    = 4f;    // thụt mỗi bên → rộng 182 < bước ô 206
    private const float CAPTION_H          = 52f;   // đủ 2 dòng cỡ ~20, hoặc 1 dòng cỡ 26
    private const float CAPTION_GAP_Y      = 4f;    // cách mép dưới vòng viền
    private const float CAPTION_FONT_MIN   = 20f;   // khoảng cỡ HẸP → mọi nhãn cùng cỡ chữ
    private const float CAPTION_FONT_MAX   = 26f;
    private const int   CAPTION_MAX_DONG   = 2;

    // [V7] Tag "MỚI" chuyển lên ĐỈNH ô. Vị trí cũ trong scene là neo (0.5,0) + (−32, 22),
    // size 104×46, xoay 8°: nửa-chiều-cao sau khi xoay = 52·sin8° + 23·cos8° = 30.0px, tâm
    // ở y = +22 → đáy tag chạm y = −8, tức THÒ 8px XUỐNG DƯỚI mép ô, trong khi bảng chữ bắt
    // đầu từ y = −4  →  tag đỏ đè lên chữ (đúng như ảnh Sếp chụp). Neo lên đỉnh là hết.
    private const float TAG_TREN_X = -18f;
    private const float TAG_TREN_Y = -22f;

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

        // [V7 — 2026-09-06] RÚT GỌN NHÃN ngay tại đây — SetupCore là CHỐT DUY NHẤT mà mọi
        // đường vẽ đều đi qua (ô mở khoá của scene lẫn ô quà dựng runtime), nên không có
        // đường nào lọt được nhãn dài ra màn hình. Xem RutGonNhan() ở cuối file.
        string tagCap;
        string nhanNgan = RutGonNhan(caption, out tagCap);

        if (nhanNgan != caption)
        {
            Debug.Log("[UnlockSlot] Rút gọn nhãn: '" + caption + "' → '" + nhanNgan + "'" + (tagCap != null ? " | badge=" + tagCap : ""));
        }

        // Nhãn kiểu "... sẽ mở ở cấp N" là LỜI HẸN, không phải vừa mở. Đổi tag đỏ "MỚI"
        // thành badge xanh "Cấp N": vừa ngắn hơn, vừa đúng nghĩa. Chỉ đổi khi caller đang
        // dùng tag mặc định MỚI/NEW — ô quà ("×3", "+150") giữ nguyên tag của nó.
        if (tagCap != null && LaTagMoiMacDinh(tagText))
        {
            tagText  = tagCap;
            tagColor = TagColorSoon;
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
            DatTagLenDinhO();   // [V7] kéo tag ra khỏi bảng chữ ở dưới ô
        }

        // [R2 GỘP Q1] Tool dựng scene (LevelUpPopupTownshipTool.BuildUnlockSlot) hiện gọi
        // EditorBind(icon, ring, tag, null) — captionText LUÔN null cho 9 ô đã dựng sẵn trong
        // scene cũ. Không được sửa tool/scene (ngoài phạm vi file cho phép) → TỰ TẠO 1 TMP nhỏ
        // dưới ô ngay lần đầu cần hiện caption, rồi cache lại để dùng cho các lần sau.
        if (captionText == null && !string.IsNullOrWhiteSpace(nhanNgan))
            captionText = CreateCaptionTextRuntime();

        if (captionText != null)
        {
            bool has = !string.IsNullOrWhiteSpace(nhanNgan);
            captionText.gameObject.SetActive(has);
            if (has)
            {
                captionText.text = nhanNgan;
                ApDinhDangCaption(captionText);   // [V7] khuôn chữ dùng MỘT chỗ, xem hàm đó
            }
        }

        StartBobbing();
    }

    /// <summary>[R2 GỘP Q1] Tự tạo caption TMP runtime khi prefab/scene chưa có sẵn — xem chú
    /// thích ở SetupCore(). Neo giữa-dưới, ngay dưới vòng viền tròn; chỉ tạo 1 lần rồi cache.</summary>
    private TextMeshProUGUI CreateCaptionTextRuntime()
    {
        if (_rt == null) _rt = transform as RectTransform;

        var go = new GameObject("Caption_TuTao", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        // [V7 2026-09-06] Neo/kích thước bảng chữ nay do ApDinhDangCaption() đặt (một chỗ
        // duy nhất, dùng chung với caption do prefab gán sẵn) — xem hàm đó ở cuối file.

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.color = new Color32(70, 45, 20, 255);
        ApDinhDangCaption(txt);                // [V7] khuôn chữ + khung chữ đều nằm ở đó
        return txt;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // [V7 — 2026-09-06] KHUÔN CHỮ + VỊ TRÍ TAG — MỘT CHỖ DUY NHẤT
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Áp khuôn chữ chuẩn cho caption — dùng cho CẢ caption tự tạo lẫn caption do
    /// prefab/tool gán sẵn, nên 10 ô trong popup CHẮC CHẮN cùng cỡ chữ, cùng bề rộng,
    /// cùng kiểu tràn. Chỉ nắn lại RectTransform khi caption là con TRỰC TIẾP của ô —
    /// không đụng vào caption ai đó cố tình đặt chỗ khác.
    /// </summary>
    private void ApDinhDangCaption(TextMeshProUGUI t)
    {
        if (t == null) return;

        var rt = t.rectTransform;
        if (rt != null && rt.parent == transform)
        {
            if (_rt == null) _rt = transform as RectTransform;
            float w = (_rt != null && _rt.rect.width > 1f) ? _rt.rect.width : CAPTION_W_MAC_DINH;

            // Neo (0.5,0) = mép DƯỚI ô, pivot đỉnh → bảng chữ treo ngay dưới vòng viền.
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -CAPTION_GAP_Y);
            rt.sizeDelta = new Vector2(Mathf.Max(60f, w - CAPTION_THUT_LE * 2f), CAPTION_H);
        }

        t.enableAutoSizing = true;
        t.fontSizeMin      = CAPTION_FONT_MIN;
        t.fontSizeMax      = CAPTION_FONT_MAX;
        t.maxVisibleLines  = CAPTION_MAX_DONG;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode     = TextOverflowModes.Ellipsis;
        // Tràn TRÊN-GIỮA (không phải giữa-giữa): nhãn 1 dòng và nhãn 2 dòng đều bắt đầu ở
        // cùng một cao độ → hàng chữ dưới đáy ô thẳng tắp.
        t.alignment        = TextAlignmentOptions.Top;
        t.raycastTarget    = false;
    }

    /// <summary>
    /// [V7] Kéo tag ("MỚI" / "Cấp 3" / "×3") lên ĐỈNH ô icon. Vị trí cũ (giữa-dưới, xoay 8°)
    /// thò xuống dưới mép ô 8px nên đè thẳng lên bảng chữ. Neo đỉnh là hết chồng lấn, và
    /// đây cũng là chỗ quen thuộc của badge "mới" trong game nông trại.
    /// </summary>
    private void DatTagLenDinhO()
    {
        if (newTagRoot == null) return;
        var tagRT = newTagRoot.transform as RectTransform;
        if (tagRT == null || tagRT.parent != transform) return;

        tagRT.anchorMin = tagRT.anchorMax = new Vector2(0.5f, 1f);
        tagRT.pivot     = new Vector2(0.5f, 0.5f);
        tagRT.anchoredPosition = new Vector2(TAG_TREN_X, TAG_TREN_Y);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // [V7 — 2026-09-06] RÚT GỌN NHÃN MỞ KHOÁ
    // ─────────────────────────────────────────────────────────────────────────
    //  Nhãn dài KHÔNG sinh ra trong code — nó nằm trong DỮ LIỆU asset
    //  (LevelRewardConfig.unlockEntries[].label, do LevelUpRewardDataSetupTool.cs
    //  ghi vào, xem dòng 150–151 của tool đó). Code cũ bưng nguyên chuỗi ấy vào caption:
    //  LevelUpPopupUI.ApplyUnlockSlots() → slot.Setup(icon, true, entry.label).
    //
    //  KHÔNG sửa .asset được (ngoài quyền), và cũng KHÔNG NÊN: chuỗi dài vẫn còn ích cho
    //  dòng chữ "Mở khóa: ..." ở thân popup (LevelRewardConfig.GetUnlockLabels()).
    //  Nên rút gọn ở TẦNG HIỂN THỊ, bằng LUẬT chung chứ không phải bảng tra từng món —
    //  thế thì Chuồng heo / Chuồng bò / Máy Xay Bột / Máy Ép Mía ... ở các level sau tự
    //  động gọn theo, không phải thêm dòng nào.
    //
    //  Mọi chuỗi tiếng Việt gom hết vào cụm const ngay dưới → sau này dịch hoặc sửa chỉ
    //  đụng vào một chỗ.
    // ═════════════════════════════════════════════════════════════════════════

    private const string CUM_SE_MO_O_CAP = " sẽ mở ở cấp ";
    private const string CUM_MO_BAN_SHOP = " đã mở bán trong shop";
    private const string CUM_HAU_TO_MOI  = " mới";
    private const string CUM_NHAN_CAP    = "Cấp ";
    private const string CUM_TAG_MOI_VI  = "MỚI";
    private const string CUM_TAG_MOI_EN  = "NEW";

    /// <summary>Hai cách gõ "mở khóa" / "mở khoá" — asset của game có cả hai kiểu bỏ dấu.</summary>
    private static readonly string[] CUM_TIEN_TO_MO_KHOA = { "mở khóa", "mở khoá" };

    /// <summary>TRUE khi tagText vẫn là tag "vừa mở khoá" mặc định (caller chưa đổi).</summary>
    private static bool LaTagMoiMacDinh(string tagText)
    {
        return tagText == CUM_TAG_MOI_VI || tagText == CUM_TAG_MOI_EN;
    }

    /// <summary>
    /// Cắt nhãn mở khoá dài về đúng DANH TỪ, và tách phần "sẽ mở ở cấp N" ra thành badge.
    ///
    ///   "Mở khóa hạt Ngô"                → "Hạt Ngô"
    ///   "Chuồng gà đã mở bán trong Shop" → "Chuồng gà"
    ///   "Nhà dân mới sẽ mở ở cấp 3"      → "Nhà dân"  + tagCap = "Cấp 3"
    ///
    /// Ý đồ: phần ngữ nghĩa "vừa mở" đã có badge đỏ "MỚI" gánh rồi, nhồi lại vào nhãn là
    /// thừa chữ. Chuỗi nào KHÔNG khớp luật nào thì TRẢ NGUYÊN BẢN — không đoán, không cắt bừa.
    /// </summary>
    /// <param name="nhanGoc">Nhãn thô lấy từ asset.</param>
    /// <param name="tagCap">Badge phụ suy ra được (vd "Cấp 3"); null nếu không có.</param>
    public static string RutGonNhan(string nhanGoc, out string tagCap)
    {
        tagCap = null;
        if (string.IsNullOrWhiteSpace(nhanGoc)) return nhanGoc;

        const System.StringComparison KTC = System.StringComparison.OrdinalIgnoreCase;
        string s = nhanGoc.Trim();

        // 1 — "... sẽ mở ở cấp N": cắt đuôi, đẩy "Cấp N" sang badge.
        int iCap = s.IndexOf(CUM_SE_MO_O_CAP, KTC);
        if (iCap >= 0)
        {
            string so = s.Substring(iCap + CUM_SE_MO_O_CAP.Length).Trim();
            if (so.Length > 0 && so.Length <= 4) tagCap = CUM_NHAN_CAP + so;
            s = s.Substring(0, iCap).Trim();

            // "Nhà dân mới" → "Nhà dân" (chữ "mới" đã nằm trong badge rồi)
            if (s.EndsWith(CUM_HAU_TO_MOI, KTC))
                s = s.Substring(0, s.Length - CUM_HAU_TO_MOI.Length).Trim();
        }

        // 2 — "... đã mở bán trong Shop": cắt đuôi.
        int iShop = s.IndexOf(CUM_MO_BAN_SHOP, KTC);
        if (iShop >= 0) s = s.Substring(0, iShop).Trim();

        // 3 — "Mở khóa X" / "Mở khóa: X": cắt đầu.
        for (int i = 0; i < CUM_TIEN_TO_MO_KHOA.Length; i++)
        {
            if (s.StartsWith(CUM_TIEN_TO_MO_KHOA[i], KTC))
            {
                s = s.Substring(CUM_TIEN_TO_MO_KHOA[i].Length).Trim();
                break;
            }
        }
        if (s.StartsWith(":")) s = s.Substring(1).Trim();

        // Cắt quá tay (chuỗi chỉ gồm đúng phần bị cắt) → trả nguyên bản cho an toàn.
        if (s.Length == 0) { tagCap = null; return nhanGoc.Trim(); }

        return VietHoaChuDau(s);
    }

    /// <summary>Viết hoa ĐÚNG chữ cái đầu (vd "hạt Ngô" → "Hạt Ngô"). KHÔNG đụng các chữ
    /// còn lại: tên trong asset đã có kiểu viết riêng, sửa thêm là sai ý designer.</summary>
    private static string VietHoaChuDau(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        char c0 = char.ToUpperInvariant(s[0]);
        if (c0 == s[0]) return s;
        return c0.ToString() + s.Substring(1);
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
