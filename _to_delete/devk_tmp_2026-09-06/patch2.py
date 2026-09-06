# -*- coding: utf-8 -*-
import hashlib, sys

P = "Assets/_Game/Farm/Scripts/UI/UnlockSlotUI.cs"
raw = open(P, "rb").read()
txt = raw.decode("utf-8")
assert "\r\n" in txt
lines = txt.split("\r\n")          # lines[0] == file line 1

def L(n):                          # 1-indexed file line
    return lines[n-1]

def check(n, anchor):
    if anchor not in L(n):
        print("ANCHOR FAIL line %d: expected %r got %r" % (n, anchor, L(n)))
        sys.exit(1)

check(32,  "TagColorGift")
check(106, "if (newTagRoot != null)")
check(118, "}")
check(124, "if (captionText == null")
check(143, "}")
check(153, "float w = _rt != null")
check(158, "[V6 2026-09-05] Neo (0.5,0)")
check(164, "rt.sizeDelta = new Vector2(w + 24f, 26f);")
check(166, "var txt = go.AddComponent<TextMeshProUGUI>();")
check(178, "}")
print("all anchors OK")

def blk(s):
    return s.split("\n")

# ── E : 166-178  (than ham CreateCaptionTextRuntime + cac ham moi) ──────────
E = blk(u"""        var txt = go.AddComponent<TextMeshProUGUI>();
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
        return c0 + s.Substring(1);
    }""")

# ── D2 : 158-164  (geometry cu -> ApDinhDangCaption lo) ────────────────────
D2 = blk(u"""        // [V7 2026-09-06] Neo/kích thước bảng chữ nay do ApDinhDangCaption() đặt (một chỗ
        // duy nhất, dùng chung với caption do prefab gán sẵn) — xem hàm đó ở cuối file.""")

# ── D1 : 153  (bo bien w khong con dung) -> xoa han ────────────────────────
D1 = []

# ── C : 124-143  (khoi caption trong SetupCore) ────────────────────────────
C = blk(u"""        if (captionText == null && !string.IsNullOrWhiteSpace(nhanNgan))
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
        }""")

# ── B : 106-118  (parse nhan + khoi tag) ───────────────────────────────────
B = blk(u"""        // [V7 — 2026-09-06] RÚT GỌN NHÃN ngay tại đây — SetupCore là CHỐT DUY NHẤT mà mọi
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
        }""")

# ── A : chen sau dong 32 ───────────────────────────────────────────────────
A = blk(u"""
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
    private const float TAG_TREN_Y = -22f;""")

# Splice tu DUOI len TREN de chi so dong phia tren khong bi xe dich
def splice(a, b, new):        # thay lines[a..b] (1-indexed, inclusive)
    lines[a-1:b] = new

splice(166, 178, E)
splice(158, 164, D2)
splice(153, 153, D1)
splice(124, 143, C)
splice(106, 118, B)
lines[32:32] = A              # chen sau dong 32

out = "\r\n".join(lines).encode("utf-8")
open(P, "wb").write(out)
print("WROTE md5=%s bytes=%d crlf=%d lf=%d" % (
      hashlib.md5(out).hexdigest(), len(out), out.count(b"\r\n"), out.count(b"\n")))
