using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Danh mục vật phẩm dùng chung cho Bảng Tin Chợ và Quầy Hàng.
///
/// VÌ SAO tách enum ra file riêng: cả DEV-A (bảng tin) lẫn DEV-B (quầy hàng) đều
/// cần cùng một bộ danh mục để tab lọc hai bên khớp nhau. Nếu mỗi bên tự khai một
/// enum thì lúc gộp dữ liệu sẽ phải viết bảng ánh xạ — thừa và dễ sai.
///
/// KHÔNG được đổi thứ tự giá trị đã có: enum này được serialize xuống
/// MarketDatabase.asset dưới dạng SỐ, đổi thứ tự là toàn bộ 60+ dòng dữ liệu
/// nhảy sai danh mục mà không có lỗi biên dịch nào báo cho biết.
/// </summary>
public enum MarketCategory
{
    All        = 0,   // chỉ dùng cho tab lọc, không gán cho vật phẩm
    NongSan    = 1,   // nông sản thu hoạch: lúa, ngô, cà chua...
    HatGiong   = 2,   // hạt giống — LƯU Ý: mua vào WarehouseManager, không phải kho thường
    Hoa        = 3,   // hoa trang trí
    ChanNuoi   = 4,   // sản phẩm chuồng: trứng, sữa, thịt
    CheBien    = 5,   // sản phẩm máy: bột gạo, nước mía ép, phô mai
    MonAn      = 6,   // món ăn nấu xong
    GiaVi      = 7,   // muối, nước mắm, nước tương, rau thơm
    VatLieu    = 8    // vật liệu xây dựng / hàng tàu: gỗ, đá, đinh, kính, sơn
}

/// <summary>
/// Tiện ích hiển thị cho <see cref="MarketCategory"/>.
/// Để riêng khỏi enum vì enum phải nằm ở phạm vi toàn cục cho DEV-B dùng chung.
/// </summary>
public static class MarketCategoryUtil
{
    /// <summary>
    /// Thứ tự hiện tab lọc trên dải dọc — cố định để người chơi quen tay.
    ///
    /// CHỈ 8 TAB, cố ý: dải dọc cao 650px, mỗi tab 70px + 8px cách nhau ⇒ 9 tab là tràn
    /// ra ngoài panel. CheBien bị bỏ vì ba sản phẩm máy (bot_gao, nuoc_mia_ep, pho_mai)
    /// chưa có icon nên chưa lên chợ. Khi gán icon xong: thêm CheBien vào đây RỒI
    /// giảm TabHeight trong MarketBoardUIBuilder xuống 62, nếu không tab cuối bị cắt.
    /// </summary>
    public static readonly MarketCategory[] FilterOrder =
    {
        MarketCategory.All,
        MarketCategory.NongSan,
        MarketCategory.HatGiong,
        MarketCategory.Hoa,
        MarketCategory.ChanNuoi,
        MarketCategory.MonAn,
        MarketCategory.GiaVi,
        MarketCategory.VatLieu
    };

    private static readonly Dictionary<MarketCategory, string> DisplayNames =
        new Dictionary<MarketCategory, string>
        {
            { MarketCategory.All,      "Tất cả"    },
            { MarketCategory.NongSan,  "Nông sản"  },
            { MarketCategory.HatGiong, "Hạt giống" },
            { MarketCategory.Hoa,      "Hoa"       },
            { MarketCategory.ChanNuoi, "Chăn nuôi" },
            { MarketCategory.CheBien,  "Chế biến"  },
            { MarketCategory.MonAn,    "Món ăn"    },
            { MarketCategory.GiaVi,    "Gia vị"    },
            { MarketCategory.VatLieu,  "Vật liệu"  }
        };

    /// <summary>
    /// Màu nhận dạng của từng danh mục. Đây là NỀN CÓ MÀU tạm thời — khi chủ dự án
    /// gắn icon thật vào tab thì màu này lùi xuống làm viền nhấn, không phải bỏ đi.
    /// </summary>
    private static readonly Dictionary<MarketCategory, Color> AccentColors =
        new Dictionary<MarketCategory, Color>
        {
            { MarketCategory.All,      new Color(0.60f, 0.62f, 0.72f) },
            { MarketCategory.NongSan,  new Color(0.42f, 0.72f, 0.38f) },
            { MarketCategory.HatGiong, new Color(0.78f, 0.62f, 0.32f) },
            { MarketCategory.Hoa,      new Color(0.86f, 0.46f, 0.66f) },
            { MarketCategory.ChanNuoi, new Color(0.94f, 0.72f, 0.34f) },
            { MarketCategory.CheBien,  new Color(0.52f, 0.68f, 0.86f) },
            { MarketCategory.MonAn,    new Color(0.92f, 0.48f, 0.36f) },
            { MarketCategory.GiaVi,    new Color(0.68f, 0.54f, 0.86f) },
            { MarketCategory.VatLieu,  new Color(0.56f, 0.60f, 0.64f) }
        };

    public static string GetDisplayName(MarketCategory category)
    {
        return DisplayNames.TryGetValue(category, out string name) ? name : category.ToString();
    }

    public static Color GetAccentColor(MarketCategory category)
    {
        return AccentColors.TryGetValue(category, out Color color) ? color : Color.gray;
    }

    /// <summary>
    /// Chữ viết tắt 2 ký tự cho tab khi chưa có art. Dùng TMP nên phải là chuỗi ngắn,
    /// không phải icon — để trống sẽ ra ô màu vô nghĩa, người test không biết tab nào là tab nào.
    /// </summary>
    public static string GetShortLabel(MarketCategory category)
    {
        switch (category)
        {
            case MarketCategory.All:      return "TC";
            case MarketCategory.NongSan:  return "NS";
            case MarketCategory.HatGiong: return "HG";
            case MarketCategory.Hoa:      return "HO";
            case MarketCategory.ChanNuoi: return "CN";
            case MarketCategory.CheBien:  return "CB";
            case MarketCategory.MonAn:    return "MA";
            case MarketCategory.GiaVi:    return "GV";
            case MarketCategory.VatLieu:  return "VL";
            default:                      return "??";
        }
    }
}
