# -*- coding: utf-8 -*-
import io, hashlib, sys

ROOT = sys.argv[1]
P_SLOT  = ROOT + "/Assets/_Game/Farm/Scripts/UI/UnlockSlotUI.cs"
P_POPUP = ROOT + "/Assets/_Game/Farm/Scripts/UI/LevelUpPopupUI.cs"

def crlf(s):
    return s.replace("\r\n", "\n").replace("\n", "\r\n")

def patch(path, pairs):
    with open(path, "rb") as f:
        raw = f.read()
    txt = raw.decode("utf-8")
    for i, (old, new) in enumerate(pairs):
        o, n = crlf(old), crlf(new)
        c = txt.count(o)
        if c != 1:
            print("FAIL %s pair#%d count=%d" % (path, i, c))
            sys.exit(1)
        txt = txt.replace(o, n, 1)
    out = txt.encode("utf-8")
    with open(path, "wb") as f:
        f.write(out)
    print("OK %s md5=%s crlf=%d lf=%d" % (path, hashlib.md5(out).hexdigest(),
          out.count(b"\r\n"), out.count(b"\n")))

# ─────────────────────────────────────────────────────────────────────────────
# 1 · UnlockSlotUI.cs
# ─────────────────────────────────────────────────────────────────────────────

A_OLD = """    public static readonly Color TagColorNew  = new Color32(230,  60,  55, 255);
    public static readonly Color TagColorGift = new Color32(255, 152,   0, 255);
"""

A_NEW = """    public static readonly Color TagColorNew  = new Color32(230,  60,  55, 255);
    public static readonly Color TagColorGift = new Color32(255, 152,   0, 255);

    // [V7 - 2026-09-06] Xanh "SAP MO": dung cho badge "Cap N" tach ra tu nhan kieu
    // "Nha dan moi se mo o cap 3". Muc do CHUA mo o level nay, deo tag "MOI" do la sai nghia.
    public static readonly Color TagColorSoon = new Color32( 70, 130, 200, 255);

    // =========================================================================
    // [V7 - 2026-09-06] KHUON CHU DUOI O - gom MOT CHO, moi o dung chung so nay
    // -------------------------------------------------------------------------
    //  BUG DA SUA (anh Sep chup 06/09): 3 o dau ("Mo khoa hat Ngo",
    //  "Chuong ga da mo ban trong Shop", "Nha dan moi se mo o cap 3") co nhan dai
    //  gap 3-5 lan cac o con lai. Hai nguyen nhan DO DUOC TRONG CODE:
    //
    //   (1) TRAN NGANG - CreateCaptionTextRuntime() cu dat be rong chu = (be rong o + 24)
    //       = 190 + 24 = 214px, trong khi BUOC O cua flow-layout chi la
    //       190 + MERGED_SPACING_X(16) = 206px  =>  hai nhan canh nhau DE LEN NHAU 8px.
    //       Nay dat be rong chu = be rong o - 8 = 182px  =>  con ho 24px, khong the cham.
    //
    //   (2) CHU KHONG DEU - autosize cu 12..18 + maxVisibleLines = 1: nhan ngan
    //       ("Vang", "Ngo") ve o 18, nhan dai bi ep xuong san 12 roi "..." nen hang chu
    //       lo cho cao cho thap. Nay 20..26 + toi da 2 dong: sau khi RutGonNhan() cat
    //       nhan ve dung danh tu, MOI nhan deu vua 1 dong o co 26  =>  hang chu deu tam.
    // =========================================================================
    private const float CAPTION_W_MAC_DINH = 190f;  // be rong o chuan do tool dung (SLOT_SIZE)
    private const float CAPTION_THUT_LE    = 4f;    // thut moi ben => rong 182 < buoc o 206
    private const float CAPTION_H          = 52f;   // du 2 dong o co ~20, hoac 1 dong co 26
    private const float CAPTION_GAP_Y      = 4f;    // cach mep duoi vong vien
    private const float CAPTION_FONT_MIN   = 20f;   // khoang co HEP => moi nhan cung co chu
    private const float CAPTION_FONT_MAX   = 26f;
    private const int   CAPTION_MAX_DONG   = 2;

    // [V7] Tag "MOI" chuyen len DINH o. Vi tri cu trong scene la (0.5,0)+(-32,22) size
    // 104x46 xoay 8 do: nua-chieu-cao sau khi xoay = 52*sin8 + 23*cos8 = 30.0px, tam o
    // y=+22 => day tag cham y=-8, tuc THUT 8px XUONG DUOI mep o, trong khi bang chu bat
    // dau tu y=-4  =>  tag do de len chu (dung nhu anh Sep chup). Neo len dinh la het.
    private const float TAG_TREN_X = -18f;
    private const float TAG_TREN_Y = -22f;
"""

B_OLD = """        if (newTagRoot != null)
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
"""

B_NEW = """        // [V7 - 2026-09-06] RUT GON NHAN ngay tai day - CHOT DUY NHAT cua moi duong ve
        // (o mo khoa cua scene lan o qua dung runtime deu di qua SetupCore), nen khong the
        // co duong nao lot nhan dai ra man hinh. Xem RutGonNhan() ben duoi.
        string tagCap;
        string nhanNgan = RutGonNhan(caption, out tagCap);

        if (nhanNgan != caption)
        {
            Debug.Log("[UnlockSlot] Rut gon nhan: '" + caption + "' -> '" + nhanNgan + "'" + (tagCap != null ? " | badge=" + tagCap : ""));
        }

        // Nhan kieu "... se mo o cap N" la LOI HEN, khong phai vua mo. Doi tag do "MOI"
        // thanh badge xanh "Cap N": vua ngan hon, vua dung nghia. Chi doi khi caller dang
        // dung tag mac dinh MOI/NEW - o qua ("x3", "+150") giu nguyen tag cua no.
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
            DatTagLenDinhO();   // [V7] keo tag ra khoi bang chu
        }
"""

C_OLD = """        if (captionText == null && !string.IsNullOrWhiteSpace(caption))
            captionText = CreateCaptionTextRuntime();

        if (captionText != null)
        {
            bool has = !string.IsNullOrWhiteSpace(caption);
            captionText.gameObject.SetActive(has);
            if (has)
            {
                captionText.text = caption;
                // [V6 2026-09-05] Ep kieu chu ten: 1 dong, tu co 12-18, dai qua thi "..."
                // (ap ca cho caption do tool/prefab gan san, khong chi caption tu tao).
                captionText.enableAutoSizing = true;
                captionText.fontSizeMin      = 12;
                captionText.fontSizeMax      = 18;
                captionText.maxVisibleLines  = 1;
                captionText.textWrappingMode = TextWrappingModes.Normal;
                captionText.overflowMode     = TextOverflowModes.Ellipsis;
            }
        }
"""

C_NEW = """        if (captionText == null && !string.IsNullOrWhiteSpace(nhanNgan))
            captionText = CreateCaptionTextRuntime();

        if (captionText != null)
        {
            bool has = !string.IsNullOrWhiteSpace(nhanNgan);
            captionText.gameObject.SetActive(has);
            if (has)
            {
                captionText.text = nhanNgan;
                ApDinhDangCaption(captionText);   // [V7] khuon chu dung MOT cho, xem ham do
            }
        }
"""

D_OLD = """    private TextMeshProUGUI CreateCaptionTextRuntime()
    {
        if (_rt == null) _rt = transform as RectTransform;
        float w = _rt != null ? _rt.rect.width : 190f;

        var go = new GameObject("Caption_TuTao", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
"""

D_NEW = """    private TextMeshProUGUI CreateCaptionTextRuntime()
    {
        if (_rt == null) _rt = transform as RectTransform;

        var go = new GameObject("Caption_TuTao", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
"""

E_OLD = """        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize          = 18;
        txt.enableAutoSizing  = true;          // [V6] ten dai tu co 18 -> 12 roi moi "..."
        txt.fontSizeMin       = 12;
        txt.fontSizeMax       = 18;
        txt.maxVisibleLines   = 1;             // [V6] dung 1 dong
        txt.color             = new Color32(70, 45, 20, 255);
        txt.alignment         = TextAlignmentOptions.Center;
        txt.textWrappingMode  = TextWrappingModes.Normal;
        txt.overflowMode      = TextOverflowModes.Ellipsis;
        txt.raycastTarget     = false;
        return txt;
    }
"""

E_NEW = """        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.color = new Color32(70, 45, 20, 255);
        ApDinhDangCaption(txt);                // [V7] khuon chu + khung chu deu o day
        return txt;
    }

    // =========================================================================
    // [V7 - 2026-09-06] KHUON CHU + VI TRI TAG - MOT CHO DUY NHAT
    // =========================================================================

    /// <summary>
    /// Ap khuon chu chuan cho caption (ca caption tu tao lan caption do prefab/tool gan san,
    /// nen 10 o trong popup CHAC CHAN cung co chu, cung be rong, cung kieu tran).
    /// Chi nan lai RectTransform khi caption la con truc tiep cua o - khong dung vao
    /// caption ai do co tinh dat cho khac.
    /// </summary>
    private void ApDinhDangCaption(TextMeshProUGUI t)
    {
        if (t == null) return;

        var rt = t.rectTransform;
        if (rt != null && rt.parent == transform)
        {
            if (_rt == null) _rt = transform as RectTransform;
            float w = (_rt != null && _rt.rect.width > 1f) ? _rt.rect.width : CAPTION_W_MAC_DINH;

            // Neo (0.5,0) = mep DUOI o, pivot dinh => bang chu treo ngay duoi vong vien.
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
        // Tran TREN-GIUA (khong phai giua-giua): nhan 1 dong va nhan 2 dong deu bat dau o
        // cung mot cao do => hang chu duoi day o thang tap.
        t.alignment        = TextAlignmentOptions.Top;
        t.raycastTarget    = false;
    }

    /// <summary>
    /// [V7] Keo tag ("MOI" / "Cap 3" / "x3") len DINH o icon. Vi tri cu (giua-duoi, xoay 8 do)
    /// thut xuong duoi mep o 8px nen de thang len bang chu. Neo dinh la het chong lan, va
    /// day cung la cho quen thuoc cua badge "moi" trong game nong trai.
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

    // =========================================================================
    // [V7 - 2026-09-06] RUT GON NHAN MO KHOA
    // -------------------------------------------------------------------------
    //  Nhan dai KHONG sinh ra trong code - no nam trong DU LIEU asset
    //  (LevelRewardConfig.unlockEntries[].label, do LevelUpRewardDataSetupTool.cs
    //  ghi vao, vd dong 150-151). Code cu bung nguyen chuoi do vao caption:
    //  LevelUpPopupUI.ApplyUnlockSlots() -> slot.Setup(icon, true, entry.label).
    //
    //  KHONG sua .asset duoc (ngoai quyen), va cung KHONG NEN: chuoi dai van con ich cho
    //  dong chu "Mo khoa: ..." o than popup (LevelRewardConfig.GetUnlockLabels()).
    //  Nen rut gon o TANG HIEN THI, bang LUAT chung chu khong phai bang bang tra tung mon
    //  - the la Chuong heo / Chuong bo / May Xay Bot / May Ep Mia ... o cac level sau
    //  tu dong gon theo, khong phai them dong nao.
    //
    //  Moi chuoi tieng Viet gom het vao cum const ngay duoi => sau nay dich hoac sua
    //  chi dong vao mot cho.
    // =========================================================================

    private const string CUM_SE_MO_O_CAP = " se mo o cap ";
    private const string CUM_MO_BAN_SHOP = " da mo ban trong shop";
    private const string CUM_HAU_TO_MOI  = " moi";
    private const string CUM_NHAN_CAP    = "Cap ";
    private const string CUM_TAG_MOI_VI  = "MOI";
    private const string CUM_TAG_MOI_EN  = "NEW";

    private static readonly string[] CUM_TIEN_TO_MO_KHOA = { "mo khoa", "mo khoa" };

    /// <summary>TRUE khi tagText van dang la tag "vua mo khoa" mac dinh (chua bi caller doi).</summary>
    private static bool LaTagMoiMacDinh(string tagText)
    {
        return tagText == CUM_TAG_MOI_VI || tagText == CUM_TAG_MOI_EN;
    }

    /// <summary>
    /// Cat nhan mo khoa dai ve dung DANH TU, va tach phan "se mo o cap N" ra thanh badge.
    ///
    ///   "Mo khoa hat Ngo"                 -> "Hat Ngo"
    ///   "Chuong ga da mo ban trong Shop"  -> "Chuong ga"
    ///   "Nha dan moi se mo o cap 3"       -> "Nha dan"   + tagCap = "Cap 3"
    ///
    /// Y do: phan ngu nghia "vua mo" da co badge do "MOI" ganh roi, nhoi lai vao nhan la
    /// thua chu. Chuoi nao khong khop luat nao thi TRA NGUYEN BAN - khong doan, khong cat bua.
    /// </summary>
    /// <param name="nhanGoc">Nhan tho lay tu asset.</param>
    /// <param name="tagCap">Badge phu suy ra duoc (vd "Cap 3"), null neu khong co.</param>
    public static string RutGonNhan(string nhanGoc, out string tagCap)
    {
        tagCap = null;
        if (string.IsNullOrWhiteSpace(nhanGoc)) return nhanGoc;

        const System.StringComparison KTC = System.StringComparison.OrdinalIgnoreCase;
        string s = nhanGoc.Trim();

        // 1 - "... se mo o cap N": cat duoi, day "Cap N" sang badge.
        int iCap = s.IndexOf(CUM_SE_MO_O_CAP, KTC);
        if (iCap >= 0)
        {
            string so = s.Substring(iCap + CUM_SE_MO_O_CAP.Length).Trim();
            if (so.Length > 0 && so.Length <= 4) tagCap = CUM_NHAN_CAP + so;
            s = s.Substring(0, iCap).Trim();

            // "Nha dan moi" -> "Nha dan" (chu "moi" da nam trong badge roi)
            if (s.EndsWith(CUM_HAU_TO_MOI, KTC))
                s = s.Substring(0, s.Length - CUM_HAU_TO_MOI.Length).Trim();
        }

        // 2 - "... da mo ban trong Shop": cat duoi.
        int iShop = s.IndexOf(CUM_MO_BAN_SHOP, KTC);
        if (iShop >= 0) s = s.Substring(0, iShop).Trim();

        // 3 - "Mo khoa X" / "Mo khoa: X": cat dau.
        for (int i = 0; i < CUM_TIEN_TO_MO_KHOA.Length; i++)
        {
            if (s.StartsWith(CUM_TIEN_TO_MO_KHOA[i], KTC))
            {
                s = s.Substring(CUM_TIEN_TO_MO_KHOA[i].Length).Trim();
                break;
            }
        }
        if (s.StartsWith(":")) s = s.Substring(1).Trim();

        // Cat qua tay (chuoi chi co dung phan bi cat) -> tra nguyen ban cho an toan.
        if (s.Length == 0) { tagCap = null; return nhanGoc.Trim(); }

        return VietHoaChuDau(s);
    }

    /// <summary>Viet hoa dung chu cai dau (vd "hat Ngo" -> "Hat Ngo"). KHONG dung den
    /// cac chu con lai: ten trong asset da co kieu viet rieng, sua them la sai y designer.</summary>
    private static string VietHoaChuDau(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        char c0 = char.ToUpperInvariant(s[0]);
        if (c0 == s[0]) return s;
        return c0 + s.Substring(1);
    }
"""

patch(P_SLOT, [(A_OLD, A_NEW), (B_OLD, B_NEW), (C_OLD, C_NEW), (D_OLD, D_NEW), (E_OLD, E_NEW)])
