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

    /// <summary>
    /// BÍ DANH → TÊN CHUẨN. Cùng một vật phẩm nhưng hai hệ thống gọi hai tên khác nhau.
    ///
    /// VÌ SAO dùng bảng bí danh chứ không thêm một dòng `Add("chicken", ...)` nữa:
    /// thêm dòng là bảng có HAI mục thịt gà. Bộ sinh đơn duyệt <see cref="AllItems"/> sẽ
    /// có ngày ra đơn đòi `chicken` — mà kho (`FarmInventoryManager`) chỉ có khoá
    /// `chicken_meat` do chuồng gà đẻ ra, nên đơn đó KHÔNG BAO GIỜ giao được.
    /// Bảng tin chợ cũng sẽ bày hai thẻ thịt gà cạnh nhau.
    ///
    /// Nguồn của sự lệch này: công thức nấu ăn (`IngredientData.id` trong ING_Chicken)
    /// dùng `chicken`, còn vật phẩm kho (`InventoryItemData.itemId` trong Item_ChickenMeat)
    /// dùng `chicken_meat`. Không sửa được asset nào cả — sửa `chicken` là mọi công thức
    /// gà hỏng, sửa `chicken_meat` là mọi kho đã lưu của người chơi hỏng.
    /// Nên: giữ nguyên hai asset, quy về một tên NGAY TẠI CỬA TRA CỨU.
    /// </summary>
    private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
    {
        { "chicken", "chicken_meat" },
    };

    /// <summary>Toàn bộ bảng giá, chỉ đọc. Editor tool sinh MarketDatabase.asset từ đây.</summary>
    public static IReadOnlyList<MarketItemInfo> AllItems => Rows;

    /// <summary>
    /// Bảng bí danh, chỉ đọc: tên phụ → tên chuẩn.
    ///
    /// LƯU Ý cho người gọi: <see cref="AllItems"/> KHÔNG chứa bí danh (cố ý — xem
    /// <see cref="Aliases"/>). Chỗ nào cần duyệt cả tên phụ, ví dụ tool gán icon cho
    /// nhiệm vụ, thì phải duyệt thêm danh sách này.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ItemAliases => Aliases;

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

    /// <summary>
    /// Tên chuẩn của một vật phẩm sau khi gỡ khoảng trắng, hạ chữ thường và quy bí danh.
    /// Public vì bộ sinh đơn hàng và quầy hàng đều phải ghi ĐÚNG khoá kho vào dữ liệu lưu —
    /// nếu mỗi nơi tự chuẩn hoá một kiểu thì save cũ đọc lên sẽ lệch khoá.
    /// </summary>
    public static string Canonical(string itemId) => Normalize(itemId);

    private static string Normalize(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;

        string key = id.Trim().ToLowerInvariant();
        return Aliases.TryGetValue(key, out string canonical) ? canonical : key;
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
        //
        // ⚠️ CÂN BẰNG LẠI TOÀN BỘ (nhóm D) — sau khi `growSeconds` chuyển sang GIÂY THẬT
        // (50s → 700s) thì giá cũ vô nghĩa: cây cấp 10 lâu gấp 14 lần cây cấp 1 mà chỉ
        // bán được gấp 8, còn ba loại hoa thì BÁN LỖ (Cúc trắng −14, Cẩm tú cầu 0,
        // Anh thảo −2). Công thức mới:
        //     lãi một lượt = harvestAmount(4) × sellGold − goldPrice(giá hạt)
        //     lãi/giây ≈ 0.16 ở cấp 1, tăng đều lên ≈ 0.31 ở cấp 10, KHÔNG bao giờ tụt.
        // Bảng đầy đủ ở mục 6 ▸ DEV-B của `production/TEAM_SUA_TOAN_DIEN.md`.
        Add("rice",              "Lúa",              MarketCategory.NongSan,   7, 1, 100);
        Add("bapcai",            "Bắp Cải",          MarketCategory.NongSan,  10, 1,  95);
        Add("ngo",               "Ngô",              MarketCategory.NongSan,  13, 2,  95);
        Add("carot",             "Cà Rốt",           MarketCategory.NongSan,  17, 3,  90);
        Add("cachua",            "Cà Chua",          MarketCategory.NongSan,  20, 3,  90);
        Add("khoaitay",          "Khoai Tây",        MarketCategory.NongSan,  30, 5,  80);
        Add("mushroom",          "Nấm",              MarketCategory.NongSan,  34, 6,  70);
        Add("sugarcane",         "Mía",              MarketCategory.NongSan,  46, 7,  65);
        Add("lemon",             "Chanh",            MarketCategory.NongSan,  52, 8,  60);
        Add("chili",             "Ớt",               MarketCategory.NongSan,  68, 9,  55);
        Add("pepper",            "Tiêu",             MarketCategory.NongSan,  76, 10, 50);

        // ── HOA ──────────────────────────────────────────────────────────
        // 10 loại hoa trước đây chín CÙNG LÚC (54 giây) nên giá cũ chỉ chênh 12 → 32.
        // Nay thời gian trải từ 55s (Hướng dương) tới 700s (Anh thảo), giá phải trải theo.
        Add("huong_duong",       "Hướng Dương",      MarketCategory.Hoa,       8, 1,  70);
        Add("hoa_hong",          "Hoa Hồng",         MarketCategory.Hoa,      23, 4,  65);
        Add("hoa_oai_huong",     "Hoa Oải Hương",    MarketCategory.Hoa,      27, 4,  60);
        Add("hoa_lan",           "Hoa Lan",          MarketCategory.Hoa,      38, 7,  55);
        Add("hoa_cuc_trang",     "Hoa Cúc Trắng",    MarketCategory.Hoa,      42, 7,  55);
        Add("tulip",             "Tulip",            MarketCategory.Hoa,      57, 9,  50);
        Add("hoa_cuc_van_tho",   "Hoa Cúc Vạn Thọ",  MarketCategory.Hoa,      63, 9,  50);
        Add("hoa_mau_don",       "Hoa Mẫu Đơn",      MarketCategory.Hoa,      81, 10, 45);
        Add("hoa_cam_tu_cau",    "Hoa Cẩm Tú Cầu",   MarketCategory.Hoa,      88, 10, 45);
        Add("hoa_anh_thao",      "Hoa Anh Thảo",     MarketCategory.Hoa,      95, 10, 45);

        // ── HẠT GIỐNG ────────────────────────────────────────────────────
        // BasePrice ≈ 55% goldPrice ở Shop → giá chợ (×1.5) vẫn RẺ HƠN Shop khoảng 18%.
        // Đó là lý do người chơi ghé chợ thay vì mua thẳng ở Shop.
        // ⚠️ `ca_rot` và `khoai_tay` KHÔNG có tiền tố seed_ — đúng như asset gốc, đừng "sửa".
        Add("seed_rice",             "Hạt Lúa",             MarketCategory.HatGiong,  11, 1, 100);
        Add("seed_huong_duong",      "Hạt Hướng Dương",     MarketCategory.HatGiong,  13, 1,  70);
        Add("seed_bapcai",           "Hạt Bắp Cải",         MarketCategory.HatGiong,  15, 1,  95);
        Add("seed_ngo",              "Hạt Ngô",             MarketCategory.HatGiong,  19, 2,  95);
        Add("ca_rot",                "Hạt Cà Rốt",          MarketCategory.HatGiong,  25, 3,  90);
        Add("seed_cachua",           "Hạt Cà Chua",         MarketCategory.HatGiong,  29, 3,  90);
        Add("seed_hoa_hong",         "Hạt Hoa Hồng",        MarketCategory.HatGiong,  31, 4,  65);
        Add("seed_hoa_oai_huong",    "Hạt Hoa Oải Hương",   MarketCategory.HatGiong,  37, 4,  60);
        Add("khoai_tay",             "Hạt Khoai Tây",       MarketCategory.HatGiong,  39, 5,  80);
        Add("seed_nam",              "Hạt Nấm",             MarketCategory.HatGiong,  42, 6,  70);
        Add("seed_hoa_lan",          "Hạt Hoa Lan",         MarketCategory.HatGiong,  44, 7,  55);
        Add("seed_hoa_cuc_trang",    "Hạt Hoa Cúc Trắng",   MarketCategory.HatGiong,  48, 7,  55);
        Add("seed_sugarcane",        "Hạt Mía",             MarketCategory.HatGiong,  53, 7,  65);
        Add("seed_lemon",            "Hạt Chanh",           MarketCategory.HatGiong,  58, 8,  60);
        Add("seed_tulip",            "Hạt Tulip",           MarketCategory.HatGiong,  59, 9,  50);
        Add("seed_hoa_cuc_van_tho",  "Hạt Hoa Cúc Vạn Thọ", MarketCategory.HatGiong,  65, 9,  50);
        Add("seed_chili",            "Hạt Ớt",              MarketCategory.HatGiong,  70, 9,  55);
        Add("seed_pepper",           "Hạt Tiêu",            MarketCategory.HatGiong,  74, 10, 50);
        Add("seed_hoa_mau_don",      "Hạt Hoa Mẫu Đơn",     MarketCategory.HatGiong,  78, 10, 45);
        Add("seed_hoa_cam_tu_cau",   "Hạt Hoa Cẩm Tú Cầu",  MarketCategory.HatGiong,  84, 10, 45);
        Add("seed_hoa_anh_thao",     "Hạt Hoa Anh Thảo",    MarketCategory.HatGiong,  90, 10, 45);

        // ── CHĂN NUÔI ────────────────────────────────────────────────────
        // ⚠️ CÂN BẰNG LẠI (nhóm E). Trước đây 1 hạt lúa (7 vàng) thả vào chuồng gà trả
        // về 4 thịt gà + 4 trứng = 320 vàng trong 30 giây → lãi/giây gấp ~70 lần ruộng
        // tốt nhất, mà chuồng gà chỉ 100 vàng ở cấp 2 ⇒ từ cấp 2 trồng trọt vô nghĩa.
        //
        // Bảng chuồng MỚI (đã chốt, mục 3): gà 90s · 2 ăn · 1 thịt + 1 trứng ·
        // heo 150s · 2 ăn · 1 · bò 240s · 3 ăn · 1 · bò sữa 300s · 3 ăn · 2.
        // Giá dưới đây đặt sao cho lãi/giây của chuồng ≈ 2.2 lần lãi/giây của ruộng
        // ở CÙNG CẤP: vẫn đáng mua (chuồng đòi hai lần tương tác + ăn nông sản) nhưng
        // không còn xoá sổ trồng trọt. Cả game chỉ có 4 chuồng, đối lại 26 ô ruộng.
        //
        // UnlockLevel = ĐÚNG cấp mở chuồng tương ứng, nếu không bộ sinh đơn sẽ ra đơn
        // đòi thứ người chơi chưa có cách nào làm ra.
        Add("egg",           "Trứng",     MarketCategory.ChanNuoi,  20, 2, 85);
        Add("chicken_meat",  "Thịt Gà",   MarketCategory.ChanNuoi,  29, 2, 75);
        // `chicken` (id trong công thức nấu ăn) quy về đúng dòng trên qua bảng Aliases —
        // KHÔNG thêm dòng riêng, xem giải thích tại `Aliases`.
        Add("pork",          "Thịt Heo",  MarketCategory.ChanNuoi,  90, 4, 65);
        // ⚠️ CS-4 — hai dòng dưới đã DỜI CẤP theo `DataShop/Buiding/`: `Chuồng Bò` 6 → 8,
        // `Chuồng Bò Sữa` 8 → 13 (cấp cũ mở sớm hơn khả năng trả tiền 4–6 cấp).
        // UnlockLevel ở đây PHẢI bằng `unlockLevel` của chuồng tương ứng: bảng đơn hàng
        // sinh đơn theo con số này, để thấp hơn là ra đơn đòi thứ người chơi chưa có cách
        // nào làm bằng lao động — chỉ còn nước mua lại ở chợ hoặc bấm Bỏ đơn.
        Add("beef",          "Thịt Bò",   MarketCategory.ChanNuoi, 165,  8, 60);
        Add("milk",          "Sữa",       MarketCategory.ChanNuoi, 115, 13, 75);

        // ── CHẾ BIẾN (máy) ───────────────────────────────────────────────
        // MarketEnabled = false: ba asset Item_BotGao / Item_NuocMiaEp / Item_PhoMai
        // đang để icon = None. Cho lên bảng tin bây giờ là ra thẻ icon trắng —
        // đúng thứ mục 8 BÀN GIAO bắt phải không có. Gán icon xong thì bật lại
        // và chạy Tools/Farm/Chợ/Sinh lại MarketDatabase.
        //
        // UnlockLevel sửa 5/8/9 → 11/13/15 cho khớp DataShop, rồi ở vòng 2 sửa tiếp
        // 11/13/15 → 17/21/24 khi ba máy được dời cấp theo khả năng chi trả (CS-4).
        // Giá tính theo cùng luật với chuồng: máy 360s/420s/480s, ăn 1 phần, ra 2 sản phẩm.
        Add("bot_gao",       "Bột Gạo",      MarketCategory.CheBien, 130, 17, 60, false);
        Add("nuoc_mia_ep",   "Nước Mía Ép",  MarketCategory.CheBien, 185, 21, 50, false);
        Add("pho_mai",       "Phô Mai",      MarketCategory.CheBien, 260, 24, 45, false);

        // ── GIA VỊ ───────────────────────────────────────────────────────
        Add("salt",       "Muối",        MarketCategory.GiaVi, 12, 1, 90);
        Add("herbs",      "Rau Thơm",    MarketCategory.GiaVi, 18, 3, 85);
        Add("soysauce",   "Nước Tương",  MarketCategory.GiaVi, 26, 4, 80);
        Add("fishsauce",  "Nước Mắm",    MarketCategory.GiaVi, 28, 4, 80);

        // ── MÓN ĂN ───────────────────────────────────────────────────────
        // ⚠️ SÀN LỢI NHUẬN NẤU ĂN (CS-1) — ĐỌC TRƯỚC KHI SỬA MỘT CON SỐ NÀO Ở ĐÂY.
        //
        // Người chơi thu về từ một đĩa = `DishData.sellPrice` (bán ở kho/chợ, ĐÚNG BẰNG
        // BasePrice dưới đây) + `DishData.rewardGold` (thưởng khi qua minigame).
        // Chi phí = tổng `GetBasePrice()` của MỌI nguyên liệu + gia vị trong công thức
        // (kể cả gia vị: chúng cũng tốn tiền mua/trồng như nhau).
        //
        // VÌ SAO PHẢI CÓ SÀN: nấu ăn tốn THÊM một vòng thao tác + minigame so với bán
        // thẳng nguyên liệu. Nếu lãi nấu ≤ lãi bán thô thì cả hệ bếp — cổng cấp 5, 18 món,
        // 20 thẻ nguyên liệu — trở thành trò vô nghĩa, người chơi tối ưu sẽ bán thẳng.
        // Trước đợt này `trung_op_la_bo_ne` LỖ 100 vàng/lượt (thu 181, nguyên liệu 281)
        // vì thịt bò được nâng 65 → 165 mà giá món không nâng theo.
        //
        // SÀN THEO ĐỘ KHÓ (`DishData.difficulty`), lãi = tổng thu − tổng giá nguyên liệu:
        //     difficulty 0 (dễ)  ≥ 35 %      difficulty 1 (vừa) ≥ 45 %      difficulty 2 (khó) ≥ 60 %
        // Tăng dần theo độ khó vì món khó đòi nhiều thao tác canh lửa/đảo hơn.
        // Quy ước phụ: `rewardGold = round(sellPrice × 0,25)` — giữ đúng một tỉ lệ cho
        // cả 18 món để sau này đổi giá chỉ phải nhớ MỘT con số.
        //
        // Sáu món đã nâng ở đợt này (đều dính thịt bò 165 hoặc tiêu 76):
        //   trung_op_la_bo_ne 145→305 · nam_xao_thit_bo 225→265 · bo_xao_tieu 270→315
        //   bo_ham_ca_rot 280→350 · pho_bo_tai 320→400 · suon_heo_xao_chua_ngot 295→300
        // Sửa giá bất kỳ nguyên liệu nào ở các khối trên thì phải chạy lại
        // `production/tools/mo_phong_cap1_cap30.py` (mục T5) để xem còn món nào thủng sàn.
        //
        // Hệ quả có chủ ý: một đĩa ăn ở chợ (BasePrice × 1,5) nay ĐẮT HƠN tự gom nguyên
        // liệu — đúng lẽ, vì tiền công nấu nằm trong đó. Ai lười thì trả thêm để mua sẵn.
        Add("khoai_tay_chien",         "Khoai Tây Chiên",          MarketCategory.MonAn,  95, 5, 60);
        Add("com_chien_trung",         "Cơm Chiên Trứng",          MarketCategory.MonAn, 110, 5, 60);
        // ĐÃ BẬT LẠI (A1/A2). Trước đây tắt vì `Dish_nuoc_mia_chanh` khai cả hai
        // `requiredIngredients` là `kind: 1` (Seasoning) ⇒ `ScoreRequiredIngredients` trả 0
        // ⇒ điểm trần 30, không bao giờ chạm ngưỡng đạt 70. Nguyên nhân gốc: `Item_sugarcane`
        // (mía trong kho) trỏ `cookingData` vào `SEA_Sugar` — nên "mía" thật ra là "đường".
        // Đã tạo `ING_Sugarcane` (`kind: 0`) và đổi cả `Item_sugarcane` lẫn công thức sang nó.
        Add("nuoc_mia_chanh",          "Nước Mía Chanh",           MarketCategory.MonAn, 120, 8, 45);
        Add("trung_chien_ca_chua",     "Trứng Chiên Cà Chua",      MarketCategory.MonAn, 125, 5, 55);
        Add("salad_bap_cai_chanh",     "Salad Bắp Cải Chanh",      MarketCategory.MonAn, 130, 8, 45);
        Add("bap_cai_xao_nam",         "Bắp Cải Xào Nấm",          MarketCategory.MonAn, 160, 6, 50);
        Add("sup_ngo_nam",             "Súp Ngô Nấm",              MarketCategory.MonAn, 165, 6, 50);
        Add("salad_nam_rau",           "Salad Nấm Và Rau",         MarketCategory.MonAn, 175, 7, 45);
        Add("thit_heo_luoc_cuon_rau",  "Thịt Heo Luộc Cuốn Rau",   MarketCategory.MonAn, 185, 7, 45);
        Add("canh_khoai_tay_thit_heo", "Canh Khoai Tây Thịt Heo",  MarketCategory.MonAn, 190, 6, 45);
        Add("ga_nuong_lu",             "Gà Nướng Lu Mật Mía",      MarketCategory.MonAn, 195, 7, 40);
        Add("ga_xao_ot",               "Gà Xào Ớt",                MarketCategory.MonAn, 240, 9, 35);
        // ── Sáu món dưới đây đã NÂNG GIÁ ở vòng 2 để thoát sàn lợi nhuận (xem ghi chú
        //    đầu khối). Mỗi con số phải TRÙNG `DishData.sellPrice` của asset cùng tên,
        //    nếu lệch thì bán ở kho và bán ở chợ ra hai số khác nhau.
        Add("nam_xao_thit_bo",         "Nấm Xào Thịt Bò",          MarketCategory.MonAn, 265, 8, 40);   // NL 225 → +47 %
        Add("suon_heo_xao_chua_ngot",  "Sườn Heo Xào Chua Ngọt",   MarketCategory.MonAn, 300, 9, 30);   // NL 230 → +63 %
        Add("trung_op_la_bo_ne",       "Trứng Ốp La Bò Né",        MarketCategory.MonAn, 305, 8, 45);   // NL 281 → +36 % (trước: LỖ −36 %)
        Add("bo_xao_tieu",             "Bò Xào Tiêu",              MarketCategory.MonAn, 315, 10, 30);  // NL 267 → +48 %
        Add("bo_ham_ca_rot",           "Bò Hầm Cà Rốt",            MarketCategory.MonAn, 350, 8, 35);   // NL 270 → +62 %
        Add("pho_bo_tai",              "Phở Bò Tái",               MarketCategory.MonAn, 400, 9, 30);   // NL 310 → +61 %
        // Hai món cá (`canh_chua_ca`, `ca_nuong_tieu`) đã bị XOÁ SẠCH khỏi dự án — không
        // còn DishData, InventoryItemData, IngredientData cá, và cũng không có hệ hồ cá.
        // Không để lại dòng giá "để dành": còn dòng ở đây là bộ sinh đơn hàng còn nhìn
        // thấy chúng qua `AllItems` và có ngày sinh ra đơn không bao giờ giao được.
        // Làm hồ cá thì thêm lại cả chuỗi asset, lúc đó mới thêm dòng giá.

        // ── VẬT LIỆU (hàng tàu) ──────────────────────────────────────────
        // TrainInventoryAdapter đọc/ghi qua FarmInventoryManager nên mua ở chợ dùng được ngay.
        Add("da",    "Đá",    MarketCategory.VatLieu, 40, 6, 55);
        Add("go",    "Gỗ",    MarketCategory.VatLieu, 45, 6, 55);
        Add("dinh",  "Đinh",  MarketCategory.VatLieu, 55, 7, 50);
        Add("son",   "Sơn",   MarketCategory.VatLieu, 60, 8, 45);
        Add("kinh",  "Kính",  MarketCategory.VatLieu, 70, 8, 45);
    }
}
