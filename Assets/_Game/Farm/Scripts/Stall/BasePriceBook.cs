using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HỢP ĐỒNG BẢNG GIÁ GỐC giữa DEV-A và DEV-B (chốt ở mục 7 file
/// `production/TEAM_CHO_BANG_TIN_QUAY_HANG.md`).
///
/// DEV-A implement interface này trên lớp bảng giá của mình rồi gọi
/// <see cref="BasePriceBook.Register"/> một lần ở Awake.
/// </summary>
public interface IBasePriceProvider
{
    /// <summary>
    /// Trả false nếu itemId không có trong bảng — bên gọi sẽ tự rơi về nguồn giá dự phòng.
    /// KHÔNG được trả true kèm giá 0: giá 0 làm mọi phép tính giá gợi ý sập về 0
    /// và người chơi bán hàng lấy 0 vàng.
    /// </summary>
    bool TryGetBasePrice(string itemId, out int basePrice);
}

/// <summary>
/// SỔ GIÁ GỐC — điểm tra giá duy nhất của quầy hàng.
///
/// VÌ SAO file này thuộc về DEV-B chứ không phải DEV-A, dù DEV-A mới là bên cung cấp
/// dữ liệu: nếu DEV-B gọi thẳng vào lớp của DEV-A thì quầy hàng KHÔNG BIÊN DỊCH ĐƯỢC
/// cho tới khi DEV-A ship xong A1 — hai người chặn nhau. Đảo chiều phụ thuộc bằng
/// interface + hàm đăng ký thì mỗi bên biên dịch độc lập, và ngày DEV-A cắm bảng giá
/// thật vào thì giá gợi ý tự đổi mà DEV-B không sửa dòng nào.
///
/// Thứ tự tra (dừng ở nguồn đầu tiên cho ra số > 0):
///   1. Provider cắm ngoài qua <see cref="Register"/> (nếu có)
///   2. <c>MarketPriceTable</c> của DEV-A — BẢNG GIÁ CHÍNH THỨC
///   3. <see cref="StallItemCatalog"/> — đọc `CropData.sellGold` / `goldPrice` từ asset THẬT
///   4. Bảng dự phòng cứng bên dưới
///   5. <see cref="DefaultBasePrice"/>
///
/// Bậc 4 và 5 giờ hầu như không bao giờ chạm tới vì DEV-A đã ship bảng giá. Giữ lại
/// để quầy hàng vẫn dùng được nếu ai đó tách nó sang dự án khác chưa có MarketPriceTable.
/// </summary>
public static class BasePriceBook
{
    /// <summary>Giá cho vật phẩm hoàn toàn không biết. Cố tình để thấp — thà bán rẻ còn hơn lạm phát.</summary>
    public const int DefaultBasePrice = 10;

    private static IBasePriceProvider _provider;

    public static bool HasProvider => _provider != null;

    public static void Register(IBasePriceProvider provider)
    {
        if (provider == null) return;
        _provider = provider;
        Debug.Log("[BasePriceBook] DEV-A đã cắm bảng giá gốc — giá gợi ý ở quầy hàng dùng số của DEV-A.");
    }

    /// <summary>
    /// Chỉ gỡ đúng provider đang giữ. Không so sánh mà cứ gán null sẽ gây lỗi khi hai
    /// đối tượng cùng loại lần lượt bị huỷ: cái cũ chết SAU cái mới sẽ xoá mất cái mới.
    /// </summary>
    public static void Unregister(IBasePriceProvider provider)
    {
        if (provider != null && !ReferenceEquals(_provider, provider)) return;
        _provider = null;
    }

    public static int GetBasePrice(string itemId)
    {
        TryGetBasePrice(itemId, out int price);
        return price;
    }

    /// <summary>
    /// Trả về true khi tra được giá từ nguồn có thật (provider / asset / bảng dự phòng).
    /// Kể cả khi trả false thì <paramref name="basePrice"/> vẫn LUÔN là số dương hợp lệ,
    /// nên bên gọi không cần phòng bị chia cho 0.
    /// </summary>
    public static bool TryGetBasePrice(string itemId, out int basePrice)
    {
        basePrice = DefaultBasePrice;

        string key = Normalize(itemId);
        if (string.IsNullOrEmpty(key)) return false;

        // 1 · Provider cắm ngoài (chừa cửa cho bản đặc biệt / thử nghiệm cân bằng)
        if (_provider != null && _provider.TryGetBasePrice(key, out int fromProvider) && fromProvider > 0)
        {
            basePrice = fromProvider;
            return true;
        }

        // 2 · BẢNG GIÁ GỐC CỦA DEV-A (`MarketPriceTable`) — nguồn chính thức.
        //     Hỏi nó TRƯỚC cả asset: bảng tin chợ tính giá bằng bảng này, quầy hàng mà
        //     lấy số ở chỗ khác thì cùng một món sẽ hiện hai giá và người chơi phát hiện ngay.
        int fromDevA = MarketPriceTable.GetBasePrice(key);
        if (fromDevA > 0)
        {
            basePrice = fromDevA;
            return true;
        }

        // 3 · Asset thật trong dự án (CropData.sellGold / BaseItemData.goldPrice)
        StallItemCatalog catalog = StallItemCatalog.Instance;
        if (catalog != null && catalog.TryGetSellGold(key, out int fromAsset) && fromAsset > 0)
        {
            basePrice = fromAsset;
            return true;
        }

        // 4 · Bảng dự phòng
        if (Fallback.TryGetValue(key, out int fromTable) && fromTable > 0)
        {
            basePrice = fromTable;
            return true;
        }

        return false;
    }

    private static string Normalize(string itemId)
        => string.IsNullOrEmpty(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();

    // ─────────────────────────────────────────────────────────────────────────
    //  BẢNG DỰ PHÒNG
    // ─────────────────────────────────────────────────────────────────────────
    //  Đây KHÔNG phải bảng giá chính thức — A1 của DEV-A mới là. Nó tồn tại để quầy
    //  hàng dùng được ngay hôm nay: theo mục 3 của file TEAM, món ăn / sản phẩm chuồng /
    //  sản phẩm máy / gia vị hiện KHÔNG có trường giá bán nào trong dự án. Không có bảng
    //  này thì mọi thứ ngoài nông sản đều bán với giá 10 và cả tính năng vô nghĩa.
    //
    //  Nông sản & hoa: chép đúng `sellGold` đã có trong asset (mục 3 file TEAM), để
    //  bảng này không bao giờ mâu thuẫn với dữ liệu thật.
    //  Nhóm chưa có giá: đặt theo bậc chế biến — nguyên liệu thô < sản phẩm chuồng
    //  < sản phẩm máy < món ăn — để chuỗi chế biến luôn có lãi, không ai bán nguyên
    //  liệu thô lại lời hơn nấu thành món.
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly Dictionary<string, int> Fallback = new Dictionary<string, int>
    {
        // ── Nông sản (bằng đúng CropData.sellGold) ──
        { "rice", 7 }, { "ngo", 13 }, { "bapcai", 15 }, { "carot", 16 }, { "cachua", 20 },
        { "khoaitay", 25 }, { "nam", 30 }, { "mushroom", 30 }, { "sugarcane", 36 },
        { "lemon", 38 }, { "chili", 48 }, { "pepper", 55 },

        // ── Hoa (bằng đúng CropData.sellGold) ──
        { "huong_duong", 12 }, { "tulip", 20 }, { "hoa_lan", 22 }, { "hoa_hong", 24 },
        { "hoa_cuc_trang", 24 }, { "hoa_cuc_van_tho", 26 }, { "hoa_mau_don", 28 },
        { "hoa_oai_huong", 30 }, { "hoa_cam_tu_cau", 30 }, { "hoa_anh_thao", 32 },

        // ── Hạt giống — bán lại bằng ~50% giá mua, để mua đi bán lại không thành cỗ máy in tiền ──
        { "seed_rice", 10 }, { "seed_bapcai", 22 }, { "seed_cachua", 32 },
        { "ca_rot", 25 },        // ⚠ hạt giống nhưng KHÔNG có tiền tố seed_
        { "khoai_tay", 40 },     // ⚠ hạt giống nhưng KHÔNG có tiền tố seed_
        { "seed_nam", 50 }, { "seed_sugarcane", 60 }, { "seed_lemon", 65 },
        { "seed_ngo", 20 }, { "seed_chili", 75 }, { "seed_pepper", 95 },
        { "seed_huong_duong", 18 }, { "seed_tulip", 30 }, { "seed_hoa_lan", 33 },
        { "seed_hoa_hong", 36 }, { "seed_hoa_cuc_trang", 36 }, { "seed_hoa_cuc_van_tho", 39 },
        { "seed_hoa_mau_don", 42 }, { "seed_hoa_oai_huong", 45 },
        { "seed_hoa_cam_tu_cau", 45 }, { "seed_hoa_anh_thao", 48 },

        // ── Sản phẩm chuồng (dự án chưa có giá — đặt mới) ──
        { "egg", 18 }, { "milk", 22 }, { "chicken_meat", 40 }, { "pork", 50 }, { "beef", 60 },

        // ── Sản phẩm máy (dự án chưa có giá — đặt mới) ──
        { "bot_gao", 30 }, { "nuoc_mia_ep", 45 }, { "pho_mai", 70 },

        // ── Gia vị (dự án chưa có giá — rẻ vì mua được thoải mái) ──
        { "salt", 8 }, { "sugar", 10 }, { "fishsauce", 12 }, { "soysauce", 12 }, { "herbs", 14 },

        // ── Vật liệu tàu ──
        { "da", 10 }, { "dinh", 12 }, { "go", 15 }, { "kinh", 25 }, { "son", 30 },

        // ── Món ăn (dự án chưa có giá) — chia 3 bậc theo độ phức tạp công thức ──
        { "khoai_tay_chien", 70 }, { "trung_op_la_bo_ne", 75 }, { "com_chien_trung", 80 },
        { "trung_chien_ca_chua", 80 }, { "nuoc_mia_chanh", 70 },
        { "salad_bap_cai_chanh", 85 }, { "salad_nam_rau", 90 }, { "sup_ngo_nam", 95 },
        { "bap_cai_xao_nam", 100 }, { "canh_khoai_tay_thit_heo", 110 },
        // `canh_chua_ca` / `ca_nuong_tieu` đã xoá khỏi dự án (A4) — không giữ giá "để dành",
        // còn khoá ở đây là quầy hàng vẫn gợi ý được giá cho món không tồn tại.
        { "ga_xao_ot", 115 }, { "thit_heo_luoc_cuon_rau", 115 },
        { "bo_xao_tieu", 135 }, { "nam_xao_thit_bo", 140 },
        { "suon_heo_xao_chua_ngot", 145 }, { "bo_ham_ca_rot", 150 },
        { "ga_nuong_lu", 155 }, { "pho_bo_tai", 160 },
    };
}
