using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  BẢNG MÀU BẢNG TIN CHỢ — NỀN CÓ MÀU, CHỜ ART
/// ══════════════════════════════════════════════════════════════════════════
///
/// Đặt ở runtime (không phải Editor) vì cả Editor tool dựng hierarchy lẫn
/// MarketListingCardUI lúc chạy đều cần cùng bộ màu. Hai bản màu tách rời là
/// kiểu gì cũng có ngày lệch nhau mà không ai để ý.
///
/// ── VÌ SAO KHÔNG DÙNG MÀU CỦA VIDEO THAM CHIẾU ──────────────────────────
/// Bố cục thì học theo được, còn trang trí phải khác đi để không đạo ý tưởng.
/// Video dùng: nền CAM ĐẤT + mái hiên SỌC xanh-trắng + khung vé GÓC KHUYẾT +
/// icon danh mục treo DÂY THỪNG.
/// Bản này dùng: nền XANH MÒNG KÉT đậm + dải trang trí CHẤM BI + thẻ BO GÓC
/// TRÒN ĐỀU + tab danh mục dạng VIÊN THUỐC gắn trên thanh dọc.
/// Cùng bố cục, khác hẳn diện mạo.
/// </summary>
public static class MarketBoardPalette
{
    private static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }

    // ── Nền & khung ──────────────────────────────────────────────────────
    /// <summary>Lớp phủ tối phía sau popup.</summary>
    public static readonly Color Dim          = new Color(0f, 0f, 0f, 0.62f);
    /// <summary>Nền panel chính — xanh mòng két đậm.</summary>
    public static readonly Color PanelBase    = Hex("#1E4F52");
    /// <summary>Viền ngoài panel, đậm hơn nền một bậc.</summary>
    public static readonly Color PanelEdge    = Hex("#13383B");
    /// <summary>Vùng lõm chứa lưới thẻ.</summary>
    public static readonly Color PanelInset   = Hex("#173F42");

    // ── Dải trang trí (thay cho mái hiên sọc của video) ──────────────────
    public static readonly Color RibbonBase   = Hex("#7A3F6D");   // tím mận
    public static readonly Color RibbonDot    = Hex("#F2D6A0");   // chấm bi kem

    // ── Thẻ hàng ─────────────────────────────────────────────────────────
    public static readonly Color CardBase     = Hex("#F2E4C9");   // kem
    public static readonly Color CardInset    = Hex("#DCC9A6");   // ô lõm đựng icon
    public static readonly Color CardSellerBar= Hex("#C9B18C");   // tầng dưới, người bán

    // ── Nút ──────────────────────────────────────────────────────────────
    public static readonly Color ButtonGold   = Hex("#E0A233");   // nút làm mới (vàng)
    public static readonly Color ButtonClose  = Hex("#D2504A");   // nút X
    public static readonly Color ButtonDisabled = Hex("#8C8C8C"); // nút xám khi chạm giới hạn

    // ── Tab danh mục ─────────────────────────────────────────────────────
    public static readonly Color TabIdle      = Hex("#2C6A6E");
    public static readonly Color TabSelected  = Hex("#F4C55A");
    public static readonly Color TabRail      = Hex("#143A3D");   // thanh dọc gắn tab

    // ── Chữ ──────────────────────────────────────────────────────────────
    public static readonly Color TextOnPanel  = Hex("#F6EEDC");
    public static readonly Color TextOnCard   = Hex("#3B2E1E");
    public static readonly Color TextMuted    = Hex("#9EB4B2");
    public static readonly Color TextGold     = Hex("#F4C55A");

    // ── Nhãn ─────────────────────────────────────────────────────────────
    public static readonly Color BadgeDeal    = Hex("#4CA64C");   // "HỜI"
    public static readonly Color BadgePlayer  = Hex("#3E7BC4");   // "CỦA BẠN"
    public static readonly Color SoldOutVeil  = new Color(0.08f, 0.10f, 0.10f, 0.72f);

    // ── Kích thước dùng chung giữa Editor tool và runtime ────────────────
    public const float CardWidth   = 210f;
    public const float CardHeight  = 250f;
    public const int   GridColumns = 4;
    public const int   GridRows    = 3;
}
