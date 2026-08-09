using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một dòng trong bảng giá gốc. Struct chứ không phải class:
/// bảng này bị tra hàng trăm lần mỗi lần vẽ bảng tin, tránh cấp phát rác cho GC.
/// </summary>
public struct MarketItemInfo
{
    public string         ItemId;
    public string         DisplayName;
    public MarketCategory Category;
    /// <summary>Giá trị gốc của MỘT đơn vị — số vàng người chơi nhận khi bán thẳng.</summary>
    public int            BasePrice;
    /// <summary>Cấp người chơi tối thiểu để vật phẩm này xuất hiện ở chợ.</summary>
    public int            UnlockLevel;
    /// <summary>Trọng số bốc ngẫu nhiên khi sinh hàng NPC. Càng cao càng hay gặp.</summary>
    public int            Weight;
    /// <summary>
    /// false = có giá nhưng KHÔNG được đưa vào rổ hàng NPC.
    /// Dùng cho vật phẩm chưa có icon hoặc chưa mở khoá trong bản demo — vẫn cần giá
    /// để quầy hàng của DEV-B tính được, nhưng không được hiện ra bảng tin với ô trắng.
    /// </summary>
    public bool           MarketEnabled;
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  BẢNG GIÁ GỐC — NGUỒN SỰ THẬT DUY NHẤT VỀ GIÁ TRỊ VẬT PHẨM
///  (DEV-A cung cấp · DEV-B dùng để tính giá gợi ý ở Quầy Hàng)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO là bảng static trong code chứ không phải ScriptableObject:
///  1. Trước đây chỉ `CropData.sellGold` có giá. Món ăn / sản phẩm chuồng / gia vị
///     nằm rải ở 5 thư mục asset khác nhau và KHÔNG có chỗ nào để điền giá.
///     Thêm field vào từng loại SO là phải sửa 5 class + mở 60 asset gõ tay.
///  2. Bảng giá phải tra được từ MỌI nơi (chợ, quầy, nhiệm vụ) kể cả khi
///     chưa có manager nào tồn tại trong scene. SO cần được ai đó giữ tham chiếu,
///     static thì không.
///  3. Cân bằng số bằng cách đọc một file duy nhất dễ hơn nhiều so với soi 60 asset.
///
/// Giá nông sản/hoa lấy đúng `CropData.sellGold` đang có — KHÔNG được lệch, nếu lệch
/// thì bán ở chợ và bán ở kho ra hai số khác nhau, người chơi phát hiện ngay.
/// Giá hạt giống = 55% `CropData.goldPrice` (giá bán lại luôn thấp hơn giá mua ở Shop).
///
/// QUY ƯỚC BIÊN LỢI NHUẬN:
///   BasePrice                    → người chơi BÁN được bấy nhiêu
///   GetMarketBuyPrice = ×1.5     → NPC BÁN ở chợ (đắt hơn 50%, chừa chỗ cho quầy hàng)
///   GetSuggestedUnitPrice = ×1.3 → giá gợi ý khi người chơi rao bán ở quầy
/// Nhờ khe 1.3 &lt; 1.5 mà hàng người chơi luôn rẻ hơn hàng NPC → có lý do để ghé quầy.
/// </summary>
public static class MarketPriceTable
{
    // Hệ số quy đổi — để hằng số ở đây thay vì rải số ma khắp code
    public const float MarketBuyMultiplier     = 1.5f;
    public const float SuggestedSellMultiplier = 1.3f;
    /// <summary>Người chơi được phép chỉnh giá quanh giá gợi ý trong khoảng này.</summary>
    public const float PlayerPriceMinFactor    = 0.5f;
    public const float PlayerPriceMaxFactor    = 2.0f;

    private static readonly List<MarketItemInfo>              Rows   = new List<MarketItemInfo>(96);
    private static readonly Dictionary<string, MarketItemInfo> Lookup =
        new Dictionary<string, MarketItemInfo>(96);

    /// <summary>Toàn bộ bảng giá, chỉ đọc. Editor tool sinh MarketDatabase.asset từ đây.</summary>
    public static IReadOnlyList<MarketItemInfo> AllItems => Rows;

    static MarketPriceTable()
    {
        BuildTable();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  API CHÍNH — phần DEV-B gọi
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Tra một dòng. Trả false nếu itemId chưa có trong bảng.</summary>
    public static bool TryGet(string itemId, out MarketItemInfo info)
    {
        return Lookup.TryGetValue(Normalize(itemId), out info);
    }

    public static bool Has(string itemId) => Lookup.ContainsKey(Normalize(itemId));

    /// <summary>Giá trị gốc 1 đơn vị. Trả 0 khi không biết — người gọi phải tự chặn.</summary>
    public static int GetBasePrice(string itemId)
    {
        return Lookup.TryGetValue(Normalize(itemId), out MarketItemInfo info) ? info.BasePrice : 0;
    }

    /// <summary>Giá NPC bán ở Bảng Tin Chợ cho 1 đơn vị.</summary>
    public static int GetMarketBuyPrice(string itemId)
    {
        int basePrice = GetBasePrice(itemId);
        return basePrice <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(basePrice * MarketBuyMultiplier));
    }

    /// <summary>Giá gợi ý khi NGƯỜI CHƠI rao bán 1 đơn vị ở Quầy Hàng.</summary>
    public static int GetSuggestedUnitPrice(string itemId)
    {
        int basePrice = GetBasePrice(itemId);
        return basePrice <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(basePrice * SuggestedSellMultiplier));
    }

    /// <summary>
    /// Giá gợi ý cho cả lô. Tách hàm riêng vì DEV-B cần cập nhật giá mỗi khi người chơi
    /// bấm +/− số lượng — nhân ở phía UI dễ quên làm tròn rồi lệch 1 vàng.
    /// </summary>
    public static int GetSuggestedTotalPrice(string itemId, int quantity)
    {
        if (quantity <= 0) return 0;
        return GetSuggestedUnitPrice(itemId) * quantity;
    }

    /// <summary>Cận dưới hợp lệ khi người chơi tự chỉnh giá 1 đơn vị.</summary>
    public static int GetMinPlayerUnitPrice(string itemId)
    {
        int suggested = GetSuggestedUnitPrice(itemId);
        return suggested <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(suggested * PlayerPriceMinFactor));
    }

    /// <summary>Cận trên hợp lệ khi người chơi tự chỉnh giá 1 đơn vị.</summary>
    public static int GetMaxPlayerUnitPrice(string itemId)
    {
        int suggested = GetSuggestedUnitPrice(itemId);
        return suggested <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(suggested * PlayerPriceMaxFactor));
    }

    public static string GetDisplayName(string itemId)
    {
        return Lookup.TryGetValue(Normalize(itemId), out MarketItemInfo info) && !string.IsNullOrEmpty(info.DisplayName)
            ? info.DisplayName
            : itemId;
    }

    public static MarketCategory GetCategory(string itemId)
    {
        return Lookup.TryGetValue(Normalize(itemId), out MarketItemInfo info) ? info.Category : MarketCategory.All;
    }

    public static int GetUnlockLevel(string itemId)
    {
        return Lookup.TryGetValue(Normalize(itemId), out MarketItemInfo info) ? info.UnlockLevel : 1;
    }

    /// <summary>
    /// TRUE khi itemId là hạt giống. Dùng để quyết định bỏ vào kho nào sau khi mua.
    ///
    /// ⚠️ KHÔNG được thay bằng itemId.StartsWith("seed"): `ca_rot` và `khoai_tay`
    /// là hạt giống nhưng không có tiền tố đó. Đây chính là LỖI 3 của bản cũ —
    /// hạt mua ở chợ rơi vào kho nông sản nên trồng không được.
    /// </summary>
    public static bool IsSeed(string itemId)
    {
        return GetCategory(itemId) == MarketCategory.HatGiong;
    }

    private static string Normalize(string id)
    {
        return string.IsNullOrEmpty(id) ? string.Empty : id.Trim().ToLowerInvariant();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DỮ LIỆU
    // ══════════════════════════════════════════════════════════════════════

    private static void Add(string itemId, string displayName, MarketCategory category,
                            int basePrice, int unlockLevel, int weight, bool marketEnabled = true)
    {
        string key = Normalize(itemId);
        if (string.IsNullOrEmpty(key) || Lookup.ContainsKey(key))
            return;   // trùng id = lỗi gõ nhầm, bỏ qua dòng sau để bảng không có hai giá

        MarketItemInfo info = new MarketItemInfo
        {
            ItemId        = key,
            DisplayName   = displayName,
            Category      = category,
            BasePrice     = basePrice,
            UnlockLevel   = unlockLevel,
            Weight        = weight,
            MarketEnabled = marketEnabled
        };

        Rows.Add(info);
        Lookup.Add(key, info);
    }

    private static void BuildTable()
    {
        // ── NÔNG SẢN ─────────────────────────────────────────────────────
        // Giá = CropData.sellGold, KHÔNG được sửa lệch khỏi asset gốc.
        Add("rice",              "Lúa",              MarketCategory.NongSan,   7, 1, 100);
        Add("ngo",               "Ngô",              MarketCategory.NongSan,  13, 2,  95);
        Add("bapcai",            "Bắp Cải",          MarketCategory.NongSan,  15, 1,  95);
        Add("carot",             "Cà Rốt",           MarketCategory.NongSan,  16, 3,  90);
        Add("cachua",            "Cà Chua",          MarketCategory.NongSan,  20, 3,  90);
        Add("khoaitay",          "Khoai Tây",        MarketCategory.NongSan,  25, 5,  80);
        Add("mushroom",          "Nấm",              MarketCategory.NongSan,  30, 6,  70);
        Add("sugarcane",         "Mía",              MarketCategory.NongSan,  36, 7,  65);
        Add("lemon",             "Chanh",            MarketCategory.NongSan,  38, 8,  60);
        Add("chili",             "Ớt",               MarketCategory.NongSan,  48, 9,  55);
        Add("pepper",            "Tiêu",             MarketCategory.NongSan,  55, 10, 50);

        // ── HOA ──────────────────────────────────────────────────────────
        Add("huong_duong",       "Hướng Dương",      MarketCategory.Hoa,      12, 1,  70);
        Add("tulip",             "Tulip",            MarketCategory.Hoa,      20, 9,  50);
        Add("hoa_lan",           "Hoa Lan",          MarketCategory.Hoa,      22, 7,  55);
        Add("hoa_hong",          "Hoa Hồng",         MarketCategory.Hoa,      24, 4,  65);
        Add("hoa_cuc_trang",     "Hoa Cúc Trắng",    MarketCategory.Hoa,      24, 7,  55);
        Add("hoa_cuc_van_tho",   "Hoa Cúc Vạn Thọ",  MarketCategory.Hoa,      26, 9,  50);
        Add("hoa_mau_don",       "Hoa Mẫu Đơn",      MarketCategory.Hoa,      28, 10, 45);
        Add("hoa_oai_huong",     "Hoa Oải Hương",    MarketCategory.Hoa,      30, 4,  60);
        Add("hoa_cam_tu_cau",    "Hoa Cẩm Tú Cầu",   MarketCategory.Hoa,      30, 10, 45);
        Add("hoa_anh_thao",      "Hoa Anh Thảo",     MarketCategory.Hoa,      32, 10, 45);

        // ── HẠT GIỐNG ────────────────────────────────────────────────────
        // BasePrice ≈ 55% goldPrice ở Shop → giá chợ (×1.5) vẫn RẺ HƠN Shop khoảng 18%.
        // Đó là lý do người chơi ghé chợ thay vì mua thẳng ở Shop.
        // ⚠️ `ca_rot` và `khoai_tay` KHÔNG có tiền tố seed_ — đúng như asset gốc, đừng "sửa".
        Add("seed_rice",             "Hạt Lúa",             MarketCategory.HatGiong,  11, 1, 100);
        Add("seed_huong_duong",      "Hạt Hướng Dương",     MarketCategory.HatGiong,  19, 1,  70);
        Add("seed_ngo",              "Hạt Ngô",             MarketCategory.HatGiong,  22, 2,  95);
        Add("seed_bapcai",           "Hạt Bắp Cải",         MarketCategory.HatGiong,  25, 1,  95);
        Add("ca_rot",                "Hạt Cà Rốt",          MarketCategory.HatGiong,  28, 3,  90);
        Add("seed_tulip",            "Hạt Tulip",           MarketCategory.HatGiong,  33, 9,  50);
        Add("seed_cachua",           "Hạt Cà Chua",         MarketCategory.HatGiong,  36, 3,  90);
        Add("seed_hoa_lan",          "Hạt Hoa Lan",         MarketCategory.HatGiong,  39, 7,  55);
        Add("khoai_tay",             "Hạt Khoai Tây",       MarketCategory.HatGiong,  44, 5,  80);
        Add("seed_hoa_hong",         "Hạt Hoa Hồng",        MarketCategory.HatGiong,  44, 4,  65);
        Add("seed_hoa_mau_don",      "Hạt Hoa Mẫu Đơn",     MarketCategory.HatGiong,  50, 10, 45);
        Add("seed_nam",              "Hạt Nấm",             MarketCategory.HatGiong,  55, 6,  70);
        Add("seed_hoa_cuc_van_tho",  "Hạt Hoa Cúc Vạn Thọ", MarketCategory.HatGiong,  55, 9,  50);
        Add("seed_hoa_oai_huong",    "Hạt Hoa Oải Hương",   MarketCategory.HatGiong,  55, 4,  60);
        Add("seed_hoa_cuc_trang",    "Hạt Hoa Cúc Trắng",   MarketCategory.HatGiong,  61, 7,  55);
        Add("seed_sugarcane",        "Hạt Mía",             MarketCategory.HatGiong,  66, 7,  65);
        Add("seed_hoa_cam_tu_cau",   "Hạt Hoa Cẩm Tú Cầu",  MarketCategory.HatGiong,  66, 10, 45);
        Add("seed_lemon",            "Hạt Chanh",           MarketCategory.HatGiong,  72, 8,  60);
        Add("seed_hoa_anh_thao",     "Hạt Hoa Anh Thảo",    MarketCategory.HatGiong,  72, 10, 45);
        Add("seed_chili",            "Hạt Ớt",              MarketCategory.HatGiong,  94, 9,  55);
        Add("seed_pepper",           "Hạt Tiêu",            MarketCategory.HatGiong, 105, 10, 50);

        // ── CHĂN NUÔI ────────────────────────────────────────────────────
        // Cao hơn nông sản vì tốn thức ăn + thời gian chuồng (feedDurationSeconds).
        Add("egg",           "Trứng",     MarketCategory.ChanNuoi, 35, 4, 85);
        Add("milk",          "Sữa",       MarketCategory.ChanNuoi, 40, 6, 75);
        Add("chicken_meat",  "Thịt Gà",   MarketCategory.ChanNuoi, 45, 5, 75);
        Add("pork",          "Thịt Heo",  MarketCategory.ChanNuoi, 55, 6, 65);
        Add("beef",          "Thịt Bò",   MarketCategory.ChanNuoi, 65, 7, 60);

        // ── CHẾ BIẾN (máy) ───────────────────────────────────────────────
        // MarketEnabled = false: ba asset Item_BotGao / Item_NuocMiaEp / Item_PhoMai
        // đang để icon = None. Cho lên bảng tin bây giờ là ra thẻ icon trắng —
        // đúng thứ mục 8 BÀN GIAO bắt phải không có. Gán icon xong thì bật lại
        // và chạy Tools/Farm/Chợ/Sinh lại MarketDatabase.
        Add("bot_gao",       "Bột Gạo",      MarketCategory.CheBien, 30, 5, 60, false);
        Add("nuoc_mia_ep",   "Nước Mía Ép",  MarketCategory.CheBien, 60, 8, 50, false);
        Add("pho_mai",       "Phô Mai",      MarketCategory.CheBien, 85, 9, 45, false);

        // ── GIA VỊ ───────────────────────────────────────────────────────
        Add("salt",       "Muối",        MarketCategory.GiaVi, 12, 1, 90);
        Add("herbs",      "Rau Thơm",    MarketCategory.GiaVi, 18, 3, 85);
        Add("soysauce",   "Nước Tương",  MarketCategory.GiaVi, 26, 4, 80);
        Add("fishsauce",  "Nước Mắm",    MarketCategory.GiaVi, 28, 4, 80);

        // ── MÓN ĂN ───────────────────────────────────────────────────────
        // Giá theo độ khó + cấp mở khoá trong DishData. Món ăn là hàng đắt nhất chợ:
        // mua một đĩa phở rẻ hơn nhiều so với tự gom đủ nguyên liệu — đó là giá trị của chợ.
        Add("khoai_tay_chien",         "Khoai Tây Chiên",          MarketCategory.MonAn,  95, 5, 60);
        Add("com_chien_trung",         "Cơm Chiên Trứng",          MarketCategory.MonAn, 110, 5, 60);
        Add("nuoc_mia_chanh",          "Nước Mía Chanh",           MarketCategory.MonAn, 120, 8, 45);
        Add("trung_chien_ca_chua",     "Trứng Chiên Cà Chua",      MarketCategory.MonAn, 125, 5, 55);
        Add("salad_bap_cai_chanh",     "Salad Bắp Cải Chanh",      MarketCategory.MonAn, 130, 8, 45);
        Add("trung_op_la_bo_ne",       "Trứng Ốp La Bò Né",        MarketCategory.MonAn, 145, 8, 45);
        Add("bap_cai_xao_nam",         "Bắp Cải Xào Nấm",          MarketCategory.MonAn, 160, 6, 50);
        Add("sup_ngo_nam",             "Súp Ngô Nấm",              MarketCategory.MonAn, 165, 6, 50);
        Add("salad_nam_rau",           "Salad Nấm Và Rau",         MarketCategory.MonAn, 175, 7, 45);
        Add("thit_heo_luoc_cuon_rau",  "Thịt Heo Luộc Cuốn Rau",   MarketCategory.MonAn, 185, 7, 45);
        Add("canh_khoai_tay_thit_heo", "Canh Khoai Tây Thịt Heo",  MarketCategory.MonAn, 190, 6, 45);
        Add("ga_nuong_lu",             "Gà Nướng Lu Mật Mía",      MarketCategory.MonAn, 195, 7, 40);
        Add("nam_xao_thit_bo",         "Nấm Xào Thịt Bò",          MarketCategory.MonAn, 225, 8, 40);
        Add("ga_xao_ot",               "Gà Xào Ớt",                MarketCategory.MonAn, 240, 9, 35);
        Add("bo_xao_tieu",             "Bò Xào Tiêu",              MarketCategory.MonAn, 270, 10, 30);
        Add("bo_ham_ca_rot",           "Bò Hầm Cà Rốt",            MarketCategory.MonAn, 280, 8, 35);
        Add("suon_heo_xao_chua_ngot",  "Sườn Heo Xào Chua Ngọt",   MarketCategory.MonAn, 295, 9, 30);
        Add("pho_bo_tai",              "Phở Bò Tái",               MarketCategory.MonAn, 320, 9, 30);
        // Hai món cá: DishData để unlockLevel 99 vì trong farm CHƯA có nguyên liệu cá.
        // Vẫn ghi giá để sau này mở khoá không phải cân bằng lại, nhưng không cho lên chợ.
        Add("canh_chua_ca",            "Canh Chua Cá",             MarketCategory.MonAn, 290, 99, 20, false);
        Add("ca_nuong_tieu",           "Cá Nướng Tiêu",            MarketCategory.MonAn, 300, 99, 20, false);

        // ── VẬT LIỆU (hàng tàu) ──────────────────────────────────────────
        // TrainInventoryAdapter đọc/ghi qua FarmInventoryManager nên mua ở chợ dùng được ngay.
        Add("da",    "Đá",    MarketCategory.VatLieu, 40, 6, 55);
        Add("go",    "Gỗ",    MarketCategory.VatLieu, 45, 6, 55);
        Add("dinh",  "Đinh",  MarketCategory.VatLieu, 55, 7, 50);
        Add("son",   "Sơn",   MarketCategory.VatLieu, 60, 8, 45);
        Add("kinh",  "Kính",  MarketCategory.VatLieu, 70, 8, 45);
    }
}
