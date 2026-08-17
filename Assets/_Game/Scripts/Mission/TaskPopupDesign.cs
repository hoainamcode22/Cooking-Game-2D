using UnityEngine;

/// <summary>
/// BẢNG SỐ ĐO & MÀU CỦA BẢN THIẾT KẾ POPUP NHIỆM VỤ.
///
/// Chép NGUYÊN VĂN từ CSS của bản thiết kế "Bảng gỗ nông trại · juicy"
/// (`Assets/thietke/anh/UnifiedTaskPopup_Redesign/TaskPopup_standalone.html`).
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO TÁCH RA FILE RIÊNG
/// ══════════════════════════════════════════════════════════════════════════
/// `UnifiedTaskPopupUI.cs` đang rải hàng trăm `new Color32(...)` và toạ độ thẳng vào
/// từng lời gọi `CreateImage`. Sửa một sắc độ phải mò khắp 2.200 dòng, và sửa sót một
/// chỗ là hai thành phần cạnh nhau lệch màu — đúng lỗi đã gặp ở lần chỉnh trước.
///
/// Giờ mọi giá trị nằm ở đây. Bản thiết kế đổi màu thì sửa một dòng.
///
/// ══════════════════════════════════════════════════════════════════════════
///  HỆ TOẠ ĐỘ
/// ══════════════════════════════════════════════════════════════════════════
/// Thiết kế vẽ trong khung 1920×1080 — trùng `CanvasScaler.referenceResolution` của
/// game, nên số pixel trong CSS dùng được TRỰC TIẾP, không cần quy đổi.
///
/// CSS đo từ mép trên-trái, Unity đo từ TÂM. Mọi hằng dưới đây đã quy đổi sẵn sang
/// hệ Unity; công thức quy đổi ghi kèm để đối chiếu lại được với CSS gốc.
/// </summary>
public static class TaskPopupDesign
{
    // ═════════════════════════════════════════════════════════════════════════
    //  MÀU
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Đổi mã hex CSS thành Color. Nhận "#rrggbb" hoặc "#rrggbbaa".</summary>
    public static Color Hex(string hex, float alpha = 1f)
    {
        if (string.IsNullOrEmpty(hex)) return Color.magenta;
        if (hex[0] == '#') hex = hex.Substring(1);

        int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);
        float a = hex.Length >= 8 ? System.Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : alpha;

        return new Color(r / 255f, g / 255f, b / 255f, a);
    }

    // ── Ván gỗ ───────────────────────────────────────────────────────────────
    public static readonly Color VanGoTren   = Hex("#a9743c");   // gradient 0%
    public static readonly Color VanGoGiua   = Hex("#8a5a2e");   // gradient 14%
    public static readonly Color VanGoDuoi   = Hex("#7c4e22");   // gradient 100%
    public static readonly Color VanGoVien   = Hex("#4a2508");   // border 8px
    public static readonly Color VanGoTho    = Hex("#3a1c04", 0.32f);  // thớ ván ngang
    public static readonly Color DinhSatSang = Hex("#ffe9b8");   // đinh, tâm sáng
    public static readonly Color DinhSatToi  = Hex("#7a4a1a");   // đinh, mép tối
    public static readonly Color DinhSatVien = Hex("#5a3210");

    // ── Ribbon tiêu đề ───────────────────────────────────────────────────────
    public static readonly Color RibbonTren  = Hex("#ffd257");
    public static readonly Color RibbonDuoi  = Hex("#f0a32f");
    public static readonly Color RibbonVien  = Hex("#a35c14");   // border 5px
    public static readonly Color DuoiRibbonTren = Hex("#d8641f");
    public static readonly Color DuoiRibbonDuoi = Hex("#a84812");
    public static readonly Color ChuTieuDe   = Hex("#fffbe9");
    public static readonly Color VienChuTieuDe = Hex("#96540f");

    // ── Tab ──────────────────────────────────────────────────────────────────
    public static readonly Color TabChonTren    = Hex("#fffbe9");
    public static readonly Color TabChonDuoi    = Hex("#fdf0d3");
    public static readonly Color TabThuongTren  = Hex("#e2a75f");
    public static readonly Color TabThuongDuoi  = Hex("#c48538");
    public static readonly Color TabVien        = Hex("#6e4014");   // border 4px
    public static readonly Color TabDiaIcon     = Hex("#ffffff", 0.45f);
    public static readonly Color TabDiaVien     = Hex("#a06928", 0.35f);
    public static readonly Color TabChuChon     = Hex("#5b3417");
    public static readonly Color TabChuThuong   = Hex("#fff6de");

    // ── Giấy ─────────────────────────────────────────────────────────────────
    public static readonly Color GiayTren    = Hex("#fdf3da");
    public static readonly Color GiayDuoi    = Hex("#fbeccb");
    public static readonly Color GiayVien    = Hex("#6e4014");   // border 4px
    public static readonly Color GiayVienTrong = Hex("#f3ddb0"); // inset ring 3px

    // ── Hàng nhiệm vụ ────────────────────────────────────────────────────────
    public static readonly Color HangTren    = Hex("#fffdf4");
    public static readonly Color HangDuoi    = Hex("#fdf6e3");
    public static readonly Color HangVien    = Hex("#ecd09c");   // border 3px
    public static readonly Color HangDoCanh  = Hex("#be8c46", 0.35f);  // 0 5px 0
    public static readonly Color KhungIconTren = Hex("#ffe9bd");
    public static readonly Color KhungIconDuoi = Hex("#ffd98f");
    public static readonly Color KhungIconVien = Hex("#d99a4e");  // border 3px
    public static readonly Color TenBinhThuong = Hex("#5b3417");
    public static readonly Color TenMoNhat     = Hex("#93876a");  // khoá / đã nhận

    /// <summary>Độ mờ cả hàng — thiết kế: khoá 0.55, đã nhận 0.68, thường 1.</summary>
    public const float MoKhoa   = 0.55f;
    public const float MoDaNhan = 0.68f;

    // ── Thanh tiến độ ────────────────────────────────────────────────────────
    public static readonly Color TdMang      = Hex("#e8d0a4");
    public static readonly Color TdRuotTren  = Hex("#a9e470");
    public static readonly Color TdRuotDuoi  = Hex("#68bd2b");
    public static readonly Color TdRuotXong  = Hex("#c9bd9f");   // đã nhận → xám
    public static readonly Color TdGloss     = Hex("#ffffff", 0.42f);
    public static readonly Color TdChu       = Hex("#ffffff");
    public static readonly Color TdChuVien   = Hex("#5a320f", 0.55f);

    // ── Ô phần thưởng ────────────────────────────────────────────────────────
    public static readonly Color OThuongTren = Hex("#fff6de");
    public static readonly Color OThuongDuoi = Hex("#ffe9bd");
    public static readonly Color OThuongVien = Hex("#e0b26a");   // border 3px
    public static readonly Color OThuongChu  = Hex("#7a4a10");

    // ── Bốn trạng thái nút ───────────────────────────────────────────────────
    public struct KieuNut
    {
        public Color nen, nenDuoi, vien, chu;
        public string nhan;

        public KieuNut(string nenTren, string nenDuoi, string vien, string chu, string nhan)
        {
            this.nen = Hex(nenTren);
            this.nenDuoi = Hex(nenDuoi);
            this.vien = Hex(vien);
            this.chu = Hex(chu);
            this.nhan = nhan;
        }
    }

    public static readonly KieuNut NutNhan   = new KieuNut("#a5e05e", "#57a51f", "#3f8a12", "#ffffff", "Nhận");
    public static readonly KieuNut NutDiLam  = new KieuNut("#ffd977", "#f2a636", "#c07818", "#7a4a10", "Đi làm");
    public static readonly KieuNut NutDaNhan = new KieuNut("#ded4bd", "#ded4bd", "#c9bd9f", "#93876a", "Đã nhận");
    public static readonly KieuNut NutKhoa   = new KieuNut("#cfc7b4", "#cfc7b4", "#b8ae95", "#8d8266", "Khoá");

    public static readonly Color NutDoCanh = Hex("#000000", 0.28f);   // 0 6px 0

    // ── Chấm đỏ "có thể nhận" ────────────────────────────────────────────────
    public static readonly Color ChamDoSang = Hex("#ff8a6e");
    public static readonly Color ChamDoGiua = Hex("#ef4b33");
    public static readonly Color ChamDoToi  = Hex("#c22c18");

    // ── Chân trang mốc ───────────────────────────────────────────────────────
    public static readonly Color MocTren     = Hex("#ffe2a0");
    public static readonly Color MocDuoi     = Hex("#f5b94e");
    public static readonly Color MocVien     = Hex("#c07d24");   // border 4px
    public static readonly Color MocChiMay   = Hex("#8c5a14", 0.35f);  // dashed
    public static readonly Color MocChu      = Hex("#6e3d12");
    public static readonly Color MocTdMang   = Hex("#d99f4b");
    public static readonly Color MocTdTren   = Hex("#ffe9ae");
    public static readonly Color MocTdDuoi   = Hex("#f5a93b");
    public static readonly Color MocDoCanh   = Hex("#8c5a14", 0.4f);

    // ── Chip mốc chuỗi thành tựu ─────────────────────────────────────────────
    public static readonly Color ChipMoc     = Hex("#8a63d2");

    // ── Nền mờ sau popup ─────────────────────────────────────────────────────
    public static readonly Color NenMo       = Hex("#060e03", 0.45f);

    // ═════════════════════════════════════════════════════════════════════════
    //  SỐ ĐO — QUY ĐỔI SẴN SANG HỆ TÂM CỦA UNITY
    // ═════════════════════════════════════════════════════════════════════════
    //  Khung thiết kế 1920×1080. Bảng gỗ 1300×850 đặt giữa khung.
    //  Gốc toạ độ mọi hằng dưới đây = TÂM BẢNG GỖ.

    public const float BangRong = 1500f;
    public const float BangCao  = 880f;
    private const float NuaRong = BangRong * 0.5f;   // 750
    private const float NuaCao  = BangCao * 0.5f;    // 440

    public const float BangBoGoc = 42f;
    public const float BangVienDay = 8f;

    // Đinh sắt: CSS top/bottom 22px, left/right 24px, 22×22
    public const float DinhKichThuoc = 22f;
    public static readonly Vector2 DinhTrenTrai  = new Vector2(-NuaRong + 24f + 11f,  NuaCao - 22f - 11f);
    public static readonly Vector2 DinhTrenPhai  = new Vector2( NuaRong - 24f - 11f,  NuaCao - 22f - 11f);
    public static readonly Vector2 DinhDuoiTrai  = new Vector2(-NuaRong + 24f + 11f, -NuaCao + 22f + 11f);
    public static readonly Vector2 DinhDuoiPhai  = new Vector2( NuaRong - 24f - 11f, -NuaCao + 22f + 11f);

    /// <summary>Khoảng cách giữa hai thớ ván ngang (CSS: repeating mỗi 158px).</summary>
    public const float ThoVanBuoc = 158f;
    public const float ThoVanDay  = 5f;

    // ── Ribbon: CSS top -54, width 680, height 134 ───────────────────────────
    //  Tấm biển: left/right 76, top 0, bottom 14  →  528 × 120
    public const float RibbonVungRong = 680f;
    public const float RibbonVungCao  = 134f;
    public static readonly Vector2 RibbonVungTam = new Vector2(0f, NuaCao + 54f - RibbonVungCao * 0.5f);

    public static readonly Vector2 RibbonTamKichThuoc = new Vector2(RibbonVungRong - 152f, 120f);
    public static readonly Vector2 RibbonTamTam = new Vector2(0f, RibbonVungTam.y + 7f);
    public const float RibbonBoGoc = 24f;
    public const int   CoChuTieuDe = 54;

    /// <summary>Hai đuôi ribbon: CSS 120×80, top 30 trong vùng ribbon.</summary>
    public static readonly Vector2 DuoiRibbonKichThuoc = new Vector2(120f, 80f);
    public static readonly Vector2 DuoiRibbonTrai = new Vector2(-RibbonVungRong * 0.5f + 60f, RibbonVungTam.y + RibbonVungCao * 0.5f - 30f - 40f);
    public static readonly Vector2 DuoiRibbonPhai = new Vector2( RibbonVungRong * 0.5f - 60f, RibbonVungTam.y + RibbonVungCao * 0.5f - 30f - 40f);

    // ── Nút đóng: CSS top -34, right -32, 100×100 ────────────────────────────
    public const float NutDongKichThuoc = 100f;
    public static readonly Vector2 NutDongTam = new Vector2(NuaRong + 32f - 50f, NuaCao + 34f - 50f);

    // ── Tab: CSS top 76, left/right 48, height 86, gap 14 ────────────────────
    public const float TabCao = 86f;
    public const float TabKheHo = 14f;
    public const float TabLunXuong = 14f;          // inactive margin-top
    public const float TabBoGoc = 22f;
    public const int   CoChuTab = 26;
    public const float TabDiaKichThuoc = 54f;
    public const float TabIconKichThuoc = 38f;

    private const float LeNgang = 48f;
    public const float VungRong = BangRong - LeNgang * 2f;              // 1404
    public static readonly float TabRong = (VungRong - TabKheHo * 2f) / 3f;

    /// <summary>Tâm X của tab thứ i (0..2).</summary>
    public static float TabTamX(int i)
        => -VungRong * 0.5f + TabRong * 0.5f + i * (TabRong + TabKheHo);

    /// <summary>Tâm Y của tab. Tab thường lún xuống 14px.</summary>
    public static float TabTamY(bool dangChon)
        => NuaCao - 76f - (dangChon ? 0f : TabLunXuong) - TabCao * 0.5f;

    // ── Giấy: CSS top 162, left/right 48, bottom 42 ──────────────────────────
    private const float GiayTrenY = NuaCao - 162f;      //  263
    private const float GiayDuoiY = -NuaCao + 42f;      // -383
    public const float GiayRong = VungRong;                        // 1204
    public static readonly float GiayCao = GiayTrenY - GiayDuoiY;  //  646
    public static readonly Vector2 GiayTam = new Vector2(0f, (GiayTrenY + GiayDuoiY) * 0.5f);   // (0, -60)
    public const float GiayBoGoc = 26f;

    /// <summary>Padding trong giấy: CSS 22px trên/dưới, 26px hai bên.</summary>
    public const float GiayLeDoc = 22f;
    public const float GiayLeNgang = 26f;
    public static readonly float VungTrongRong = GiayRong - GiayLeNgang * 2f;   // 1152
    public static readonly float VungTrongCao  = GiayCao - GiayLeDoc * 2f;      //  602

    // ── Hàng nhiệm vụ ────────────────────────────────────────────────────────
    public const float HangRong = 1340f;
    public const float HangCao  = 100f;          // padding 12 + icon 76 + padding 12
    public const float HangCaoTT = 92f;          // hàng thành tựu gọn hơn
    public const float HangBoGoc = 22f;
    public const float HangKheHo = 13f;          // CSS gap giữa các hàng

    private const float HangLe = 18f;            // CSS padding-left/right
    private const float HangNuaRong = HangRong * 0.5f;

    public const float IconKhungKichThuoc = 76f;
    public const float IconKichThuoc = 56f;
    public const float IconBoGoc = 20f;
    public const float IconNghieng = -3f;        // CSS rotate(-3deg)

    public const float CotChuRong = 480f;
    public const int   CoChuTen = 25;
    public const float TdCao = 28f;
    public const float TdBoGoc = 14f;
    public const int   CoChuTd = 17;

    // 112 → 134: ô rộng 112 chỉ còn 46px cho chữ sau khi trừ padding 12 và icon 36.
    // "x200" ở cỡ 20 Bold cần ~48px nên bị `Ellipsis` cắt thành "x2…" — thấy rõ trên
    // ảnh chụp của chủ dự án. 134 cho 68px, đủ cả "x1000".
    public const float OThuongRong = 134f;
    public const float OThuongCao  = 52f;
    public const float OThuongKheHo = 10f;
    public const float OThuongBoGoc = 16f;
    public const float OThuongIcon = 36f;
    public const int   CoChuOThuong = 20;

    public const float NutRong = 156f;
    public const float NutCao  = 60f;
    public const float NutBoGoc = 18f;
    public const int   CoChuNut = 25;
    public const float ChamDoKichThuoc = 24f;

    /// <summary>Tâm X khung icon.</summary>
    public static readonly float XKhungIcon = -HangNuaRong + HangLe + IconKhungKichThuoc * 0.5f;

    /// <summary>Tâm X cột "tên + thanh tiến độ".</summary>
    public static readonly float XCotChu = -HangNuaRong + HangLe + IconKhungKichThuoc + 18f + CotChuRong * 0.5f;

    /// <summary>Tâm X ô thưởng thứ i.</summary>
    public static float XOThuong(int i)
        => -HangNuaRong + HangLe + IconKhungKichThuoc + 18f + CotChuRong + 18f
           + OThuongRong * 0.5f + i * (OThuongRong + OThuongKheHo);

    /// <summary>Tâm X nút hành động — dán mép phải.</summary>
    public static readonly float XNut = HangNuaRong - HangLe - NutRong * 0.5f;

    // ── Chân trang mốc ───────────────────────────────────────────────────────
    public const float MocCao = 92f;
    public const float MocBoGoc = 22f;
    public const int   CoChuMoc = 24;
    public const float MocTdCao = 24f;
    public const float MocTdRong = 430f;
    public const float MocRuongKichThuoc = 100f;

    // ── Vùng cuộn: chiếm phần còn lại của giấy sau khi trừ chân mốc ──────────
    public static readonly float CuonCao = VungTrongCao - MocCao - 14f;              // 496
    public static readonly Vector2 CuonTam =
        new Vector2(0f, GiayTam.y + VungTrongCao * 0.5f - CuonCao * 0.5f);           // (0, -7)
    public static readonly Vector2 MocTam =
        new Vector2(0f, GiayTam.y - VungTrongCao * 0.5f + MocCao * 0.5f);            // (0, -315)
}
