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

    // ── Nền & khung ĐỒNG BỘ 100% VỚI KHO & SHOP ──────────────────────────
    /// <summary>Lớp phủ tối phía sau popup.</summary>
    public static readonly Color Dim          = new Color(0f, 0f, 0f, 0.62f);
    /// <summary>Nền panel chính — nâu gỗ ấm.</summary>
    public static readonly Color PanelBase    = Hex("#7C4E22");
    /// <summary>Viền ngoài panel, nâu đậm.</summary>
    public static readonly Color PanelEdge    = Hex("#4A2508");
    /// <summary>Vùng lõm chứa lưới thẻ — giấy kem ấm.</summary>
    public static readonly Color PanelInset   = Hex("#FDF3DA");

    // ── Dải trang trí & ruy băng ──────────────────────────────────────────
    public static readonly Color RibbonBase   = Hex("#F0A32F");   // vàng cam
    public static readonly Color RibbonDot    = Hex("#FFD257");   // vàng ruy băng

    // ── Thẻ hàng ─────────────────────────────────────────────────────────
    public static readonly Color CardBase     = Hex("#FFFDF4");   // kem sáng
    public static readonly Color CardInset    = Hex("#FBECCB");   // ô lõm đựng icon
    public static readonly Color CardSellerBar= Hex("#E8D5B5");   // tầng dưới, người bán

    // ── Nút ──────────────────────────────────────────────────────────────
    public static readonly Color ButtonGold   = Hex("#57A51F");   // nút làm mới / mua (xanh lá)
    public static readonly Color ButtonClose  = Hex("#D2504A");   // nút X
    public static readonly Color ButtonDisabled = Hex("#8C8C8C"); // nút xám khi chạm giới hạn

    // ── Tab danh mục ─────────────────────────────────────────────────────
    public static readonly Color TabIdle      = Hex("#A9743C");   // nâu gỗ sáng
    public static readonly Color TabSelected  = Hex("#FFD257");   // vàng nổi bật
    public static readonly Color TabRail      = Hex("#4A2508");   // thanh dọc gắn tab

    // ── Chữ ──────────────────────────────────────────────────────────────
    public static readonly Color TextOnPanel  = Hex("#FFFBE9");
    public static readonly Color TextOnCard   = Hex("#4A2508");
    public static readonly Color TextMuted    = Hex("#8A6038");
    public static readonly Color TextGold     = Hex("#FFD257");

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
