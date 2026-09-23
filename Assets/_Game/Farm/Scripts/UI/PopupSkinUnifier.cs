using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ĐỒNG BỘ VỎ POPUP THEO BỘ CỦA SHOP (skin pass lúc runtime, không sửa prefab/scene).
///
/// Chuẩn = bộ sprite đang dùng trên popup Shop (dump scene 2026-09-21):
///   • nút đóng      : UI/Standard/btn_red_small        (Sliced, trắng)
///   • nút chính xanh: UI/Standard/shop_btn_buy_gold    (Sliced, trắng)  — Buy/Confirm/OK/mặc định
///   • nút kim cương : UI/Standard/shop_btn_buy_gem     (Sliced, trắng)  — tên chứa Gem/KimCuong
///   • nút vàng      : UI/Standard/btn_yellow_3d        (Sliced, trắng)  — Cancel/Sell/Back/Discard/Refresh
///   • khung ván gỗ  : UI/Standard/WoodBoard_Frame      (Sliced, trắng)  — Board_Border của Shop
///   • giấy trong    : UI/Standard/inner_panel          (Sliced, trắng)  — Inner_PaperContainer của Shop
///   • ruy băng      : UI/Standard/shop_banner_ribbon   (Sliced, trắng)  — Header_Banner của Shop
///   • tiêu đề       : font SkinKit.FontVo (Baloo2 SDF), 46px bold, màu TaskPopupDesign.ChuTieuDe
///
/// Gán theo TÊN object, chỉ đổi sprite / màu / font — KHÔNG đụng RectTransform (kích thước,
/// anchoredPosition) để không phá layout đã clamp (VuaKhungManHinh). Idempotent: gọi bao nhiêu
/// lần cũng cho cùng kết quả. Gọi một lần trong hàm Open của từng popup (không gọi cho Shop).
/// </summary>
public static class PopupSkinUnifier
{
    /// <summary>Tắt = không làm gì cả (giữ nguyên vỏ gốc của từng popup).</summary>
    public static bool Bat = true;

    private const string Res = "UI/Standard/";
    private const float CoChuTieuDe = 46f;
    private const float CoChuX = 26f;

    private static bool _daNap;
    private static Sprite _dong, _nutXanh, _nutGem, _nutVang, _khung, _giay, _ruyBang;

    private static void Nap()
    {
        if (_daNap) return;
        _daNap = true;
        _dong    = Resources.Load<Sprite>(Res + "btn_red_small");
        if (_dong == null) _dong = UIStandardSprites.Close;
        _nutXanh = Resources.Load<Sprite>(Res + "shop_btn_buy_gold");
        _nutGem  = Resources.Load<Sprite>(Res + "shop_btn_buy_gem");
        _nutVang = Resources.Load<Sprite>(Res + "btn_yellow_3d");
        if (_nutVang == null) _nutVang = _nutXanh;
        _khung   = Resources.Load<Sprite>(Res + "WoodBoard_Frame");
        _giay    = Resources.Load<Sprite>(Res + "inner_panel");
        _ruyBang = Resources.Load<Sprite>(Res + "shop_banner_ribbon");
    }

    /// <summary>Cho phép nạp lại sprite (ví dụ sau khi tool copy thêm vào Resources).</summary>
    public static void NapLai() { _daNap = false; }

    // ═════════════════════════════════════════════════════════════════════════

    public static void ApDung(Transform popupRoot)
    {
        if (!Bat || popupRoot == null) return;
        Nap();

        try
        {
            ApKhungVaRuyBang(popupRoot);
            ApKieuKho(popupRoot);
            ApNut(popupRoot);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PopupSkinUnifier] Lỗi khi đồng bộ vỏ '" + popupRoot.name + "': " + e.Message);
        }
    }

    public static void ApDung(GameObject popupRoot)
    {
        if (popupRoot != null) ApDung(popupRoot.transform);
    }

    // ── KIEU KHO (2026-09-23): moi popup co Board_Border dung dung vo cua popup Kho ────────
    // Kho/Shop dep vi: Board_Border = WoodBoard_Frame (Sliced, da gom vien go bo tron + giay kem),
    // cac lop Board_Fill_* TAT, dinh goc = WoodBoard_Stud, lop Stud Base/Shine TAT.
    // Order Board / Quay hang / Nhiem vu co cung ten object nhung Board_Fill_* van BAT, day dac, ve
    // DE LEN khung go -> nhin thanh tam nau phang goc nhon; dinh goc la 3 o vuong mau phang.
    // Chi doi sprite/mau/bat-tat, KHONG dong RectTransform (khong pha layout).
    private static Sprite _dinh;
    private static void ApKieuKho(Transform goc)
    {
        if (_dinh == null) _dinh = Resources.Load<Sprite>(Res + "WoodBoard_Stud");
        foreach (var t in goc.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t.Find("Board_Border") == null) continue;   // t = cha cua bo khung
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i); string n = c.name;
                if (n == "Board_Fill_Bottom" || n == "Board_Fill_Top" || n == "Img_WoodBoard")
                { if (c.gameObject.activeSelf) c.gameObject.SetActive(false); continue; }
                if (n.StartsWith("Stud_") && (n.EndsWith("_Base") || n.EndsWith("_Shine")))
                { if (c.gameObject.activeSelf) c.gameObject.SetActive(false); continue; }
                if (n.StartsWith("Stud_") && n.EndsWith("_Rim"))
                {
                    var im = c.GetComponent<Image>();
                    if (im != null && _dinh != null) { im.sprite = _dinh; im.type = Image.Type.Simple; im.color = Color.white; im.preserveAspect = true; }
                    continue;
                }
                // Nhiem vu: 5 lop giay chong nhau -> giu Paper_Fill, tat phan con lai
                if (n == "Paper_Border" || n == "Paper_Fill_Top" || n == "Paper_InnerRing" || n == "Paper_Fill_Inner")
                { if (c.gameObject.activeSelf) c.gameObject.SetActive(false); continue; }
            }
            var bb = t.Find("Board_Border");
            var bim = bb != null ? bb.GetComponent<Image>() : null;
            if (bim != null && _khung != null)
            { bim.sprite = _khung; bim.type = Image.Type.Sliced; bim.pixelsPerUnitMultiplier = 1f; bim.color = Color.white; bim.preserveAspect = false; }
        }
    }

    // ── KHUNG / GIẤY / RUY BĂNG ──────────────────────────────────────────────

    private static void ApKhungVaRuyBang(Transform goc)
    {
        float dtGoc = DienTich(goc as RectTransform);
        if (dtGoc < 1f)
        {
            var cv = goc.GetComponentInParent<Canvas>();
            if (cv != null) dtGoc = DienTich(cv.transform as RectTransform);
        }

        foreach (var img in goc.GetComponentsInChildren<Image>(true))
        {
            if (img == null) continue;
            Transform t = img.transform;
            string ten = t.name;
            if (ten.StartsWith("Skin_")) continue;
            string tenThuong = ten.ToLowerInvariant();

            // Không đụng Image thuộc về nút (nút lo ở ApNut).
            if (img.GetComponent<Button>() != null) continue;
            if (TrongNut(t, goc)) continue;

            // (d) Ruy băng tiêu đề.
            if (LaRuyBang(tenThuong) && !CoConLaRuyBang(t))
            {
                if (_ruyBang != null)
                {
                    img.sprite = _ruyBang;
                    var r = ((RectTransform)t).rect;
                    // Border của ribbon là 90/24 — quá hẹp thì Sliced sẽ lộn, dùng Simple.
                    img.type = (r.width >= 200f && r.height >= 60f) ? Image.Type.Sliced : Image.Type.Simple;
                    img.pixelsPerUnitMultiplier = 1f;
                    img.color = Color.white;
                    img.preserveAspect = false;
                }
                ApTieuDe(t, ((RectTransform)t).rect.height);
                continue;
            }

            int sau = DoSau(t, goc);
            if (sau > 3) continue;

            // (c) Giấy kem bên trong.
            if (LaGiay(tenThuong))
            {
                if (_giay != null)
                {
                    img.sprite = _giay;
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = 1f;
                    img.color = Color.white;
                }
                continue;
            }

            // (c) Khung / ván gỗ ngoài: phải là bề mặt LỚN (tránh chip, ô, icon).
            if (LaKhung(tenThuong))
            {
                float dt = DienTich((RectTransform)t);
                bool lon = dt >= 300000f || (dtGoc > 1f && dt >= dtGoc * 0.25f);
                if (!lon) continue;
                if (_khung != null)
                {
                    img.sprite = _khung;
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = 1f;
                    img.color = Color.white;
                    img.preserveAspect = false;
                }
            }
        }
    }

    private static bool LaRuyBang(string s)
    {
        if (s.Contains("ribbon") || s.Contains("banner")) return true;
        if (s.Contains("header") && !s.Contains("header_bar")) return true;
        if (s.StartsWith("title") || s.EndsWith("_title")) return true;   // Image tên Title (không phải Txt_Title)
        return false;
    }

    /// <summary>Bảng mang con là ruy băng (vd Order_Banner/Ribbon của Kitchen) → bảng đó là KHUNG, không phải ruy băng.</summary>
    private static bool CoConLaRuyBang(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var c = t.GetChild(i);
            if (c.GetComponent<Image>() != null && LaRuyBang(c.name.ToLowerInvariant())) return true;
        }
        return false;
    }

    private static bool LaGiay(string s)
    {
        if (s.Contains("dim") || s.Contains("overlay") || s.Contains("mask") || s.Contains("viewport")) return false;
        return s.Contains("inner") || s.Contains("paper") || s.Contains("detailpanel") || s.Contains("listingarea");
    }

    private static bool LaKhung(string s)
    {
        if (s.Contains("dim") || s.Contains("overlay") || s.Contains("mask") || s.Contains("viewport")
            || s.Contains("content") || s.Contains("scroll") || s.Contains("toast") || s.Contains("chip")
            || s.Contains("fill") || s.Contains("grain") || s.Contains("stud") || s.Contains("shadow")
            || s.Contains("tab") || s.Contains("card") || s.Contains("slot") || s.Contains("cell")
            || s.Contains("icon") || s.Contains("empty") || s.Contains("inner") || s.Contains("paper"))
            return false;
        return s.Contains("board_border") || s.Contains("frame") || s.Contains("window")
            || s.Contains("popup_board") || s.Contains("popup_main") || s.Contains("panel_main")
            || s == "panel" || s == "bg" || s.EndsWith("_bg") || s.StartsWith("bg_")
            || s == "board" || s.EndsWith("_board") || s == "main";
    }

    // (e)+(f) Tiêu đề = TMP con trực tiếp (hoặc cháu gần) của ruy băng.
    private static void ApTieuDe(Transform ruyBang, float caoRuyBang)
    {
        TMP_Text tieuDe = null;
        for (int i = 0; i < ruyBang.childCount && tieuDe == null; i++)
            tieuDe = ruyBang.GetChild(i).GetComponent<TMP_Text>();
        if (tieuDe == null) tieuDe = ruyBang.GetComponentInChildren<TMP_Text>(true);
        if (tieuDe == null) return;

        var f = SkinKit.FontVo;
        if (f != null && tieuDe.font != f)
        {
            tieuDe.font = f;
            if (f.material != null) tieuDe.fontSharedMaterial = f.material;
        }
        tieuDe.color = TaskPopupDesign.ChuTieuDe;
        tieuDe.fontStyle |= FontStyles.Bold;

        // Ruy băng cỡ Shop (126 cao) → 46px như Shop; ruy băng nhỏ (Kitchen) giữ cỡ chữ cũ.
        if (caoRuyBang >= 100f)
        {
            tieuDe.enableAutoSizing = true;
            tieuDe.fontSizeMin = 20f;
            tieuDe.fontSizeMax = CoChuTieuDe;
            tieuDe.fontSize = CoChuTieuDe;
        }
    }

    // ── NÚT ─────────────────────────────────────────────────────────────────

    private static void ApNut(Transform goc)
    {
        foreach (var nut in goc.GetComponentsInChildren<Button>(true))
        {
            if (nut == null) continue;
            string ten = nut.name;
            if (ten.StartsWith("Skin_")) continue;
            string s = ten.ToLowerInvariant();

            Image nen = nut.image != null ? nut.image : nut.GetComponent<Image>();
            if (nen == null) continue;

            // (b) Nút đóng X.
            if (LaNutDong(s))
            {
                if (_dong != null)
                {
                    nen.sprite = _dong;
                    nen.type = Image.Type.Sliced;
                    nen.pixelsPerUnitMultiplier = 1f;
                    nen.color = Color.white;
                }
                foreach (var chu in nut.GetComponentsInChildren<TMP_Text>(true))
                {
                    chu.color = Color.white;
                    chu.fontStyle |= FontStyles.Bold;
                    if (chu.text == "X" || chu.text == "x" || chu.text == "✕") chu.fontSize = CoChuX;
                }
                continue;
            }

            // (a) Nút chính: phải mang tên nút, có chữ con, không phải tab/stepper/icon.
            if (!(s.Contains("btn") || s.Contains("button") || s.Contains("nut_"))) continue;
            if (s.Contains("tab") || s.Contains("minus") || s.Contains("plus") || s.Contains("giam")
                || s.Contains("tang") || s.Contains("arrow") || s.Contains("prev") || s.Contains("next")
                || s.Contains("toggle") || s.Contains("check") || s.Contains("slot") || s.Contains("cell")
                || s.Contains("icon") || s.Contains("avatar") || s.Contains("card")) continue;
            var chuNut = nut.GetComponentInChildren<TMP_Text>(true);
            if (chuNut == null) continue;
            // Nút icon (sprite art giữ tỉ lệ) → không đè.
            if (nen.preserveAspect && nen.sprite != null && nen.sprite != _nutXanh && nen.sprite != _nutGem && nen.sprite != _nutVang) continue;

            Sprite spr = ChonSpriteNut(s);
            if (spr == null) continue;
            nen.sprite = spr;
            nen.type = Image.Type.Sliced;
            nen.pixelsPerUnitMultiplier = 1f;
            nen.color = Color.white;

            // Chữ trên nút: trắng như Txt_Price của Shop (chỉ TMP con trực tiếp của nút).
            for (int i = 0; i < nut.transform.childCount; i++)
            {
                var c = nut.transform.GetChild(i).GetComponent<TMP_Text>();
                if (c != null) c.color = Color.white;
            }
        }
    }

    private static bool LaNutDong(string s)
    {
        return s.Contains("close") || s.Contains("btnx") || s == "btn_x" || s.Contains("nutdong")
            || s.Contains("btn_dong") || s.Contains("nut_dong") || s.Contains("thoat") || s.Contains("exit");
    }

    private static Sprite ChonSpriteNut(string s)
    {
        if (s.Contains("gem") || s.Contains("kimcuong") || s.Contains("diamond") || s.Contains("speed") || s.Contains("rutnang"))
            return _nutGem != null ? _nutGem : _nutXanh;
        if (s.Contains("cancel") || s.Contains("sell") || s.Contains("ban") || s.Contains("back")
            || s.Contains("discard") || s.Contains("huy") || s.Contains("refresh") || s.Contains("lammoi")
            || s.Contains("quaylai") || s.Contains("skip"))
            return _nutVang;
        // Buy / Confirm / OK / Upgrade / Deliver / Claim / mặc định → xanh lá.
        return _nutXanh;
    }

    // ── tiện ích ────────────────────────────────────────────────────────────

    private static float DienTich(RectTransform rt)
    {
        if (rt == null) return 0f;
        var r = rt.rect;
        return Mathf.Abs(r.width * r.height);
    }

    private static int DoSau(Transform t, Transform goc)
    {
        int d = 0;
        while (t != null && t != goc) { t = t.parent; d++; }
        return t == null ? int.MaxValue : d;
    }

    private static bool TrongNut(Transform t, Transform goc)
    {
        for (var p = t.parent; p != null && p != goc; p = p.parent)
            if (p.GetComponent<Button>() != null) return true;
        return false;
    }
}
