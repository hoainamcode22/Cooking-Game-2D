using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Missions L1-L10
///
/// Tạo/cập nhật MissionData cho hệ nhiệm vụ chính L1→L10 (23 mission, bảng đã duyệt
/// trong production/session-state/MISSIONS_L1_L10_PROPOSAL.md) + 6 nhiệm vụ ngày.
///   - Idempotent: chạy nhiều lần không tạo duplicate, giá trị luôn ghi đè về bảng chuẩn.
///   - MissionDatabase_Main được GHI ĐÈ danh sách = 23 mission mới
///     (20 asset Mission_&lt;itemId&gt; cũ vẫn nằm trên đĩa nhưng bị gỡ khỏi database).
///   - MissionDatabase_Daily được tạo mới (6 mission isDaily=true, mở từ L6).
///   - Icon: tái dùng icon từ mission cũ / CropData / DishData / InventoryItemData /
///     BaseItemData (chuồng); không tìm thấy → để null (prefab giữ icon mặc định).
///
/// Tools/Farm Game/Test/Check Missions L1-L10: validate eventType hợp lệ,
/// targetAmount &gt; 0, requiredLevel 1-10, không trùng missionId — in PASS/FAIL.
/// </summary>
public static class MissionSetupTool
{
    private const string MENU_SETUP = "Tools/Farm Game/Setup Missions L1-L30";
    private const string MENU_CHECK = "Tools/Farm Game/Test/Check Missions";

    private const string DataEwaFolder    = "Assets/_Game/Farm/data/Data_Ewa";
    private const string MainFolder       = DataEwaFolder + "/Main_L1_L10";
    private const string DailyFolder      = DataEwaFolder + "/Daily_Missions";
    private const string AchievementFolder = DataEwaFolder + "/Achievements";
    private const string MainDbPath        = DataEwaFolder + "/MissionDatabase_Main.asset";
    private const string DailyDbPath       = DataEwaFolder + "/MissionDatabase_Daily.asset";
    private const string AchievementDbPath = DataEwaFolder + "/MissionDatabase_Achievement.asset";

    // ─── Bảng mission đã duyệt ───────────────────────────────────────────────

    private struct MissionDef
    {
        public string           id;
        public string           name;
        public int              level;
        public MissionEventType type;
        public string           target;   // targetItemId ("" = mọi item)
        public int              amount;
        public int              reward;
        public RewardType       rewardType;
        public string           iconHint; // itemId để tìm missionIcon ("" = để null)

        public MissionDef(string id, string name, int level, MissionEventType type,
                          string target, int amount, int reward, RewardType rewardType,
                          string iconHint)
        {
            this.id = id; this.name = name; this.level = level; this.type = type;
            this.target = target; this.amount = amount; this.reward = reward;
            this.rewardType = rewardType; this.iconHint = iconHint;
        }
    }

    private static readonly MissionDef[] MAIN =
    {
        // L1
        new MissionDef("main_l1_plant_rice",     "Trồng 8 ô lúa",              1, MissionEventType.PlantCrop,            "rice",            8,  50, RewardType.Coin,    "rice"),
        new MissionDef("main_l1_harvest_rice",   "Thu hoạch 24 lúa",           1, MissionEventType.HarvestItem,          "rice",           24,  80, RewardType.Coin,    "rice"),
        new MissionDef("main_l1_plant_flower",   "Trồng 6 ô hoa hướng dương",  1, MissionEventType.PlantCrop,            "huong_duong",     6,  50, RewardType.Coin,    "huong_duong"),
        new MissionDef("main_l1_reach_level_2",  "Đạt cấp 2",                  1, MissionEventType.ReachLevel,           "",                2,   1, RewardType.Diamond, ""),
        // L2
        new MissionDef("main_l2_feed_chicken",   "Cho gà ăn 1 lần",            2, MissionEventType.FeedAnimal,           "",                1,  50, RewardType.Coin,    ""),
        new MissionDef("main_l2_collect_eggs",   "Thu 4 quả trứng",            2, MissionEventType.CollectAnimalProduct, "egg",             4,  80, RewardType.Coin,    "egg"),
        new MissionDef("main_l2_deliver_1",      "Giao 1 đơn hàng",            2, MissionEventType.DeliverOrder,         "",                1, 100, RewardType.Coin,    ""),
        // L3
        new MissionDef("main_l3_buy_seed",       "Mua 1 hạt giống mới",        3, MissionEventType.BuySeed,              "",                1,  60, RewardType.Coin,    ""),
        new MissionDef("main_l3_deliver_3",      "Giao 3 đơn hàng",            3, MissionEventType.DeliverOrder,         "",                3, 150, RewardType.Coin,    ""),
        new MissionDef("main_l3_harvest_veg",    "Thu hoạch 8 nông sản",       3, MissionEventType.HarvestItem,          "",                8,  70, RewardType.Coin,    ""),
        // L4
        new MissionDef("main_l4_buy_pig_pen",    "Mua chuồng heo",             4, MissionEventType.BuyShopItem,          "108",             1, 200, RewardType.Coin,    "108"),
        new MissionDef("main_l4_harvest_rose",   "Thu hoạch 4 hoa hồng",       4, MissionEventType.HarvestItem,          "hoa_hong",        4, 100, RewardType.Coin,    "hoa_hong"),
        new MissionDef("main_l4_feed_pig",       "Cho heo ăn 2 lần",           4, MissionEventType.FeedAnimal,           "",                2,  80, RewardType.Coin,    ""),
        new MissionDef("main_l4_collect_pork",   "Thu 1 thịt heo",             4, MissionEventType.CollectAnimalProduct, "pork",            1,  90, RewardType.Coin,    "pork"),
        // L5
        new MissionDef("main_l5_cook_first",     "Nấu món ăn đầu tiên",        5, MissionEventType.CookDish,             "",                1, 150, RewardType.Coin,    ""),
        new MissionDef("main_l5_deliver_dish",   "Giao 1 món cơm chiên trứng", 5, MissionEventType.DeliverOrder,         "com_chien_trung", 1, 150, RewardType.Coin,    "com_chien_trung"),
        new MissionDef("main_l5_reach_level_6",  "Đạt cấp 6",                  5, MissionEventType.ReachLevel,           "",                6,   2, RewardType.Diamond, ""),
        // L6
        new MissionDef("main_l6_cook_3",         "Nấu 3 món ăn",               6, MissionEventType.CookDish,             "",                3, 200, RewardType.Coin,    ""),
        new MissionDef("main_l6_buy_cow_pen",    "Mua chuồng bò",              6, MissionEventType.BuyShopItem,          "106",             1, 300, RewardType.Coin,    "106"),
        new MissionDef("main_l6_collect_milk",   "Thu 3 sữa bò",               6, MissionEventType.CollectAnimalProduct, "milk",            3, 140, RewardType.Coin,    "milk"),
        // L7
        new MissionDef("main_l7_deliver_5",      "Giao 5 đơn hàng",            7, MissionEventType.DeliverOrder,         "",                5, 250, RewardType.Coin,    ""),
        new MissionDef("main_l7_harvest_40",     "Thu hoạch 40 nông sản",      7, MissionEventType.HarvestItem,          "",               40, 200, RewardType.Coin,    ""),
        new MissionDef("main_l7_plant_sugarcane","Trồng 4 mía",                7, MissionEventType.PlantCrop,            "sugarcane",       4, 120, RewardType.Coin,    "sugarcane"),
        // L8
        new MissionDef("main_l8_cook_beef",      "Nấu món bò hầm cà rốt",      8, MissionEventType.CookDish,             "bo_ham_ca_rot",   1, 250, RewardType.Coin,    "bo_ham_ca_rot"),
        new MissionDef("main_l8_deliver_beef",   "Giao 1 đơn thịt bò",         8, MissionEventType.DeliverOrder,         "beef",            1, 300, RewardType.Coin,    "beef"),
        new MissionDef("main_l8_combo_2",        "Giao 2 đơn combo",           8, MissionEventType.DeliverOrder,         "",                2, 200, RewardType.Coin,    ""),
        // L9 (thiết kế "đơn 2 món/combo" — tracking hiện đếm mọi đơn giao thành công)
        new MissionDef("main_l9_deliver_3",      "Giao 3 đơn combo",           9, MissionEventType.DeliverOrder,         "",                3, 300, RewardType.Coin,    ""),
        new MissionDef("main_l9_cook_5",         "Nấu 5 món ăn",               9, MissionEventType.CookDish,             "",                5, 250, RewardType.Coin,    ""),
        // L10
        new MissionDef("main_l10_reach_level_10","Đạt cấp 10",                10, MissionEventType.ReachLevel,           "",               10,   5, RewardType.Diamond, ""),
        new MissionDef("main_l10_deliver_20",    "Giao tổng cộng 20 đơn hàng",10, MissionEventType.DeliverOrder,         "",               20, 500, RewardType.Coin,    ""),
        new MissionDef("main_l10_harvest_50",    "Thu hoạch 50 nông sản",     10, MissionEventType.HarvestItem,          "",               50, 350, RewardType.Coin,    ""),

        // ─── L11-L30 (MISSIONS_MASTER_LIST §A, chỉ loại sự kiện TRACK ĐƯỢC) ───
        //   ProcessItem → CollectAnimalProduct (máy chế biến tái dùng hệ chuồng:
        //     mill id 120→bot_gao, press 121→nuoc_mia_ep, cheese 122→pho_mai).
        //   TotalOrders / DeliverComboOrder → DeliverOrder ("DeliverOrder:*" = bộ đếm đơn tích luỹ).
        //   CookBeefDish → CookDish targetItemId = "bo_ham_ca_rot" (tracker đếm theo dishId).
        //   BuyItem máy/đất → BuyShopItem theo itemID số thật (mill 120, press 121, cheese 122, đất 100).
        //   BỎ QUA (chưa có tính năng/eventType): CatchFish, ServeBoat, UpgradeStorage, PlaceDecor,
        //   đơn lọc theo cá (fish). Xem báo cáo cuối để quyết định thêm sau.
        // L11
        new MissionDef("main_l11_buy_mill",      "Mua Máy Xay Bột",           11, MissionEventType.BuyShopItem,          "120",             1, 400, RewardType.Coin,    "120"),
        new MissionDef("main_l11_make_flour",    "Xay 4 bột gạo",             11, MissionEventType.CollectAnimalProduct, "bot_gao",         4, 300, RewardType.Coin,    "bot_gao"),
        new MissionDef("main_l11_deliver_8",     "Giao 8 đơn hàng",           11, MissionEventType.DeliverOrder,         "",                8, 350, RewardType.Coin,    ""),
        // L12 (bỏ m_l12_upgrade_store — UpgradeStorage chưa có)
        new MissionDef("main_l12_harvest_corn",  "Thu hoạch 20 ngô",          12, MissionEventType.HarvestItem,          "ngo",            20, 320, RewardType.Coin,    "ngo"),
        new MissionDef("main_l12_cook_10",       "Nấu 10 món ăn",             12, MissionEventType.CookDish,             "",               10, 400, RewardType.Coin,    ""),
        // L13
        new MissionDef("main_l13_buy_press",     "Mua Máy Ép Mía",            13, MissionEventType.BuyShopItem,          "121",             1, 500, RewardType.Coin,    "121"),
        new MissionDef("main_l13_make_juice",    "Ép 4 nước mía",             13, MissionEventType.CollectAnimalProduct, "nuoc_mia_ep",     4, 350, RewardType.Coin,    "nuoc_mia_ep"),
        new MissionDef("main_l13_deliver_5",     "Giao 5 đơn combo",          13, MissionEventType.DeliverOrder,         "",                5, 450, RewardType.Coin,    ""),
        // L14
        new MissionDef("main_l14_process_8",     "Chế biến 8 sản phẩm",       14, MissionEventType.CollectAnimalProduct, "",                8, 400, RewardType.Coin,    ""),
        new MissionDef("main_l14_orders_40",     "Hoàn thành 40 đơn",         14, MissionEventType.DeliverOrder,         "",               40, 500, RewardType.Coin,    ""),
        new MissionDef("main_l14_cook_beef3",    "Nấu 3 món bò hầm cà rốt",   14, MissionEventType.CookDish,             "bo_ham_ca_rot",   3, 420, RewardType.Coin,    "bo_ham_ca_rot"),
        // L15
        new MissionDef("main_l15_buy_cheese",    "Mua Máy Phô Mai",           15, MissionEventType.BuyShopItem,          "122",             1, 700, RewardType.Coin,    "122"),
        new MissionDef("main_l15_make_cheese",   "Làm 3 phô mai",             15, MissionEventType.CollectAnimalProduct, "pho_mai",         3, 500, RewardType.Coin,    "pho_mai"),
        new MissionDef("main_l15_reach_15",      "Đạt cấp 15",                15, MissionEventType.ReachLevel,           "",               15, 600, RewardType.Coin,    ""),
        // L16: BỎ TOÀN BỘ (pond/catch_fish/cook_fish — fishing chưa có)
        // L17
        new MissionDef("main_l17_harvest_100",   "Thu hoạch 100 nông sản",    17, MissionEventType.HarvestItem,          "",              100, 500, RewardType.Coin,    ""),
        // L18
        new MissionDef("main_l18_expand_land",   "Mở rộng đất 1 khu",         18, MissionEventType.BuyShopItem,          "100",             1, 800, RewardType.Coin,    "100"),
        new MissionDef("main_l18_cook_15",       "Nấu 15 món ăn",             18, MissionEventType.CookDish,             "",               15, 650, RewardType.Coin,    ""),
        new MissionDef("main_l18_orders_60",     "Hoàn thành 60 đơn",         18, MissionEventType.DeliverOrder,         "",               60, 700, RewardType.Coin,    ""),
        // L19 (bỏ m_l19_decor_5 — PlaceDecor chưa có)
        new MissionDef("main_l19_process_20",    "Chế biến 20 sản phẩm",      19, MissionEventType.CollectAnimalProduct, "",               20, 700, RewardType.Coin,    ""),
        new MissionDef("main_l19_deliver_10",    "Giao 10 đơn combo",         19, MissionEventType.DeliverOrder,         "",               10, 750, RewardType.Coin,    ""),
        // L20 (bỏ m_l20_catch_30)
        new MissionDef("main_l20_reach_20",      "Đạt cấp 20",                20, MissionEventType.ReachLevel,           "",               20,   8, RewardType.Diamond, ""),
        new MissionDef("main_l20_orders_100",    "Hoàn thành 100 đơn",        20, MissionEventType.DeliverOrder,         "",              100,1000, RewardType.Coin,    ""),
        // L21 (bỏ m_l21_upgrade_store2)
        new MissionDef("main_l21_cook_25",       "Nấu 25 món ăn",             21, MissionEventType.CookDish,             "",               25, 850, RewardType.Coin,    ""),
        new MissionDef("main_l21_harvest_200",   "Thu hoạch 200 nông sản",    21, MissionEventType.HarvestItem,          "",              200, 800, RewardType.Coin,    ""),
        // L22 (bỏ m_l22_deliver_fish10)
        new MissionDef("main_l22_process_40",    "Chế biến 40 sản phẩm",      22, MissionEventType.CollectAnimalProduct, "",               40, 900, RewardType.Coin,    ""),
        new MissionDef("main_l22_orders_150",    "Hoàn thành 150 đơn",        22, MissionEventType.DeliverOrder,         "",              150,1000, RewardType.Coin,    ""),
        // L23 (bỏ m_l23_unlock_boat / serve_boat)
        new MissionDef("main_l23_cook_beef5",    "Nấu 5 món bò hầm cà rốt",   23, MissionEventType.CookDish,             "bo_ham_ca_rot",   5, 850, RewardType.Coin,    "bo_ham_ca_rot"),
        // L24 (bỏ m_l24_serve_3 / decor_10)
        new MissionDef("main_l24_deliver_20",    "Giao 20 đơn combo",         24, MissionEventType.DeliverOrder,         "",               20, 950, RewardType.Coin,    ""),
        // L25 (bỏ m_l25_serve_5)
        new MissionDef("main_l25_reach_25",      "Đạt cấp 25",                25, MissionEventType.ReachLevel,           "",               25,  10, RewardType.Diamond, ""),
        new MissionDef("main_l25_orders_250",    "Hoàn thành 250 đơn",        25, MissionEventType.DeliverOrder,         "",              250,1300, RewardType.Coin,    ""),
        // L26 (bỏ m_l26_catch_60)
        new MissionDef("main_l26_cook_40",       "Nấu 40 món ăn",             26, MissionEventType.CookDish,             "",               40,1100, RewardType.Coin,    ""),
        new MissionDef("main_l26_process_70",    "Chế biến 70 sản phẩm",      26, MissionEventType.CollectAnimalProduct, "",               70,1150, RewardType.Coin,    ""),
        // L27 (bỏ m_l27_serve_10)
        new MissionDef("main_l27_harvest_350",   "Thu hoạch 350 nông sản",    27, MissionEventType.HarvestItem,          "",              350,1100, RewardType.Coin,    ""),
        new MissionDef("main_l27_orders_350",    "Hoàn thành 350 đơn",        27, MissionEventType.DeliverOrder,         "",              350,1400, RewardType.Coin,    ""),
        // L28 (bỏ m_l28_decor_20)
        new MissionDef("main_l28_cook_55",       "Nấu 55 món ăn",             28, MissionEventType.CookDish,             "",               55,1300, RewardType.Coin,    ""),
        new MissionDef("main_l28_process_100",   "Chế biến 100 sản phẩm",     28, MissionEventType.CollectAnimalProduct, "",              100,1400, RewardType.Coin,    ""),
        // L29 (bỏ m_l29_serve_15)
        new MissionDef("main_l29_orders_450",    "Hoàn thành 450 đơn",        29, MissionEventType.DeliverOrder,         "",              450,1600, RewardType.Coin,    ""),
        new MissionDef("main_l29_deliver_35",    "Giao 35 đơn combo",         29, MissionEventType.DeliverOrder,         "",               35,1500, RewardType.Coin,    ""),
        // L30
        new MissionDef("main_l30_reach_30",      "Đạt cấp 30 — Bậc thầy Nông trại", 30, MissionEventType.ReachLevel,     "",               30,  20, RewardType.Diamond, ""),
        new MissionDef("main_l30_orders_500",    "Hoàn thành 500 đơn",        30, MissionEventType.DeliverOrder,         "",              500,2000, RewardType.Coin,    ""),
        new MissionDef("main_l30_cook_70",       "Nấu 70 món ăn",             30, MissionEventType.CookDish,             "",               70,1800, RewardType.Coin,    ""),
    };

    // Nhiệm vụ ngày — pool đầy đủ (MISSIONS_MASTER_LIST §B), mở từ L6, tiến độ tự reset mỗi ngày.
    //   Chỉ giữ loại sự kiện TRACK ĐƯỢC. Bỏ: d_fish (CatchFish chưa có),
    //   d_gem ("hoàn thành mọi daily" — phần thưởng tổng hợp, không phải MissionEventType).
    //   d_process requiredLevel=11 (cần máy chế biến mới hoàn thành được).
    private static readonly MissionDef[] DAILY =
    {
        new MissionDef("d_harvest",   "Thu hoạch 15 nông sản",  6, MissionEventType.HarvestItem,          "",            15, 60, RewardType.Coin, ""),
        new MissionDef("d_plant",     "Trồng 10 cây bất kỳ",    6, MissionEventType.PlantCrop,            "",            10, 50, RewardType.Coin, ""),
        new MissionDef("d_deliver",   "Giao 3 đơn hàng",        6, MissionEventType.DeliverOrder,         "",             3, 80, RewardType.Coin, ""),
        new MissionDef("d_cook",      "Nấu 2 món ăn",           6, MissionEventType.CookDish,             "",             2, 70, RewardType.Coin, ""),
        new MissionDef("d_feed",      "Cho thú ăn 4 lần",       6, MissionEventType.FeedAnimal,           "",             4, 60, RewardType.Coin, ""),
        new MissionDef("d_egg",       "Thu 6 quả trứng",        6, MissionEventType.CollectAnimalProduct, "egg",          6, 60, RewardType.Coin, "egg"),
        new MissionDef("d_buy_seed",  "Mua 3 hạt giống",        6, MissionEventType.BuySeed,              "",             3, 50, RewardType.Coin, ""),
        new MissionDef("d_flower",    "Thu hoạch 4 hoa hướng dương", 6, MissionEventType.HarvestItem,     "huong_duong",  4, 60, RewardType.Coin, "huong_duong"),
        new MissionDef("d_combo",     "Giao 1 đơn combo",       6, MissionEventType.DeliverOrder,         "",             1, 70, RewardType.Coin, ""),
        new MissionDef("d_process",   "Chế biến 3 sản phẩm",   11, MissionEventType.CollectAnimalProduct, "",             3, 80, RewardType.Coin, ""),
    };

    // ─── Thành tựu (MISSIONS_MASTER_LIST §C) — dài hạn, nhận 1 lần, requiredLevel=1 ───
    //   Chỉ loại track được: thu hoạch / giao đơn (tổng) / nấu / chế biến / đạt cấp.
    //   Bỏ: a_fish_100 (CatchFish), a_boat_25 (ServeBoat), a_decor_30 (PlaceDecor),
    //   a_daily_7 / a_daily_30 (streak đăng nhập — không phải MissionEventType).
    //   Phần thưởng "Coin + Diamond": lấy Coin làm thưởng chính (schema 1 reward/asset);
    //   phần Diamond được liệt kê trong báo cáo để bạn quyết định (thêm secondReward sau nếu muốn).
    private static readonly MissionDef[] ACHIEVEMENTS =
    {
        new MissionDef("a_harvest_100",  "Nông dân tập sự — thu 100 nông sản",   1, MissionEventType.HarvestItem,  "",  100,  200, RewardType.Coin, ""),
        new MissionDef("a_harvest_500",  "Nông dân lành nghề — thu 500 nông sản", 1, MissionEventType.HarvestItem, "",  500,  600, RewardType.Coin, ""),
        new MissionDef("a_harvest_2000", "Nông dân huyền thoại — thu 2000 nông sản",1, MissionEventType.HarvestItem,"", 2000, 2000, RewardType.Coin, ""),
        new MissionDef("a_orders_50",    "Người giao hàng — 50 đơn",             1, MissionEventType.DeliverOrder, "",   50,  300, RewardType.Coin, ""),
        new MissionDef("a_orders_300",   "Thương lái — 300 đơn",                 1, MissionEventType.DeliverOrder, "",  300, 1000, RewardType.Coin, ""),
        new MissionDef("a_cook_30",      "Đầu bếp nhỏ — nấu 30 món",             1, MissionEventType.CookDish,     "",   30,  400, RewardType.Coin, ""),
        new MissionDef("a_cook_150",     "Bếp trưởng — nấu 150 món",             1, MissionEventType.CookDish,     "",  150, 1200, RewardType.Coin, ""),
        new MissionDef("a_process_50",   "Thợ chế biến — 50 sản phẩm",           1, MissionEventType.CollectAnimalProduct, "", 50, 500, RewardType.Coin, ""),
        // Thành tựu LÊN CẤP "Nông dân cấp 2..30" được sinh tự động trong SetupMissions()
        // (thưởng tăng dần theo cấp — tính trong UnifiedTaskPopupUI.GetAchievementRewards).
    };

    // ─── FIXED 2026-08-19: pho_beef không tồn tại ────────────────────────────
    //   12 mission proc_c_* sinh tự động từng nhận dish "pho_beef" (dish thật là
    //   "pho_bo_tai") → mission chết, không hoàn thành được. Bảng override ĐÃ DUYỆT:
    //   missionId → (targetItemId mới, tên hiển thị mới). 3 mission cấp thấp L4/L6/L8
    //   đổi sang món cấp thấp; 9 mission còn lại giữ ý đồ "phở bò" → pho_bo_tai.
    //   targetAmount không đổi (công thức 1 + lvl/2 vốn đã khớp bảng duyệt).
    private static readonly Dictionary<string, (string target, string name)> PROC_COOK_OVERRIDES =
        new Dictionary<string, (string target, string name)>
    {
        { "proc_c_4_1",   ("com_chien_trung", "Nấu 3 món Cơm chiên trứng") },  // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_6_3",   ("bap_cai_xao_nam", "Nấu 4 món Bắp cải xào nấm") },  // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_8_5",   ("nam_xao_thit_bo", "Nấu 5 món Nấm xào thịt bò") },  // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_15_2",  ("pho_bo_tai",      "Nấu 8 món Phở bò tái") },       // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_17_4",  ("pho_bo_tai",      "Nấu 9 món Phở bò tái") },       // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_19_6",  ("pho_bo_tai",      "Nấu 10 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_21_8",  ("pho_bo_tai",      "Nấu 11 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_23_10", ("pho_bo_tai",      "Nấu 12 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_24_1",  ("pho_bo_tai",      "Nấu 13 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_26_3",  ("pho_bo_tai",      "Nấu 14 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_28_5",  ("pho_bo_tai",      "Nấu 15 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
        { "proc_c_30_7",  ("pho_bo_tai",      "Nấu 16 món Phở bò tái") },      // FIXED 2026-08-19: pho_beef không tồn tại
    };

    // ─── Icon lookup ─────────────────────────────────────────────────────────

    private static Dictionary<string, Sprite> _iconCache;
    private static Sprite _coinIcon;
    private static Sprite _diamondIcon;

    private static void BuildIconCache()
    {
        _iconCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

        // 1. Mission cũ Mission_<itemId>.asset — icon đã chuẩn cho rice/egg/beef/hoa...
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData", new[] { DataEwaFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (m == null || m.missionIcon == null) continue;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            const string prefix = "Mission_";
            if (fileName.StartsWith(prefix))
                RegisterIcon(fileName.Substring(prefix.Length), m.missionIcon);

            // Icon thưởng Coin/Diamond tái dùng từ asset cũ
            if (m.rewardIcon != null)
            {
                if (m.rewardType == RewardType.Coin    && _coinIcon    == null) _coinIcon    = m.rewardIcon;
                if (m.rewardType == RewardType.Diamond && _diamondIcon == null) _diamondIcon = m.rewardIcon;
            }
        }

        // 2. CropData — seed/cropId/harvestItemId
        foreach (string guid in AssetDatabase.FindAssets("t:CropData"))
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid));
            if (crop == null || crop.itemIcon == null) continue;
            RegisterIcon(crop.itemID,        crop.itemIcon);
            RegisterIcon(crop.seedItemId,    crop.itemIcon);
            RegisterIcon(crop.harvestItemId, crop.itemIcon);
            RegisterIcon(crop.cropId,        crop.itemIcon);
        }

        // 3. DishData — dishId → dishSprite
        foreach (string guid in AssetDatabase.FindAssets("t:DishData"))
        {
            var dish = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(guid));
            if (dish == null || dish.dishSprite == null) continue;
            RegisterIcon(dish.dishId, dish.dishSprite);
        }

        // 4. InventoryItemData — itemId → icon (egg, beef, chicken_meat, milk...)
        //
        //    VÌ SAO đổi nguồn khỏi `OrderItemDefinition`: 37 asset đó đã bị xoá cùng hệ
        //    đơn hàng nhà dân cũ. `InventoryItemData` là nguồn ĐÚNG HƠN cho việc này —
        //    nó chính là asset mô tả vật phẩm trong kho, còn `OrderItemDefinition` chỉ
        //    chép lại icon từ đây. Chép lại nên cũng lệch được: `Order_item_salad_nam_rau`
        //    từng điền nhầm `itemId = salad_bap_cai_chanh` mà không ai phát hiện.
        foreach (string guid in AssetDatabase.FindAssets("t:InventoryItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || item.icon == null) continue;
            RegisterIcon(item.itemId, item.icon);
        }

        // 5. BaseItemData (gồm PlaceableItemData chuồng 106/108) — itemID → itemIcon
        foreach (string guid in AssetDatabase.FindAssets("t:BaseItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<BaseItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || item.itemIcon == null) continue;
            RegisterIcon(item.itemID, item.itemIcon);
        }

        // 6. BÍ DANH — chạy CUỐI CÙNG, khi mọi nguồn icon ở trên đã nạp xong.
        //
        //    `chicken` (id trong công thức nấu ăn) và `chicken_meat` (id trong kho) là cùng
        //    một vật phẩm. Mission nào nhắm vào tên phụ mà không có bước này sẽ hiện ô icon
        //    trắng — đúng thứ mục 8 BÀN GIAO bắt phải không có.
        //
        //    Duyệt `ItemAliases` chứ KHÔNG phải `AllItems`: bảng giá cố ý chỉ giữ tên chuẩn,
        //    tên phụ không có dòng riêng nào để mà duyệt.
        foreach (var pair in MarketPriceTable.ItemAliases)
        {
            if (_iconCache.TryGetValue(pair.Key, out Sprite already) && already != null) continue;
            if (_iconCache.TryGetValue(pair.Value, out Sprite canonicalIcon) && canonicalIcon != null)
                RegisterIcon(pair.Key, canonicalIcon);
        }
    }

    private static void RegisterIcon(string id, Sprite icon)
    {
        if (string.IsNullOrEmpty(id) || icon == null) return;
        if (!_iconCache.ContainsKey(id))
            _iconCache[id] = icon;
    }

    private static Sprite FindIcon(string id)
        => !string.IsNullOrEmpty(id) && _iconCache.TryGetValue(id, out var s) ? s : null;

    // ─── Setup ───────────────────────────────────────────────────────────────

    [MenuItem(MENU_SETUP)]
    public static void SetupMissions()
    {
        EnsureFolder(MainFolder);
        EnsureFolder(DailyFolder);
        EnsureFolder(AchievementFolder);
        BuildIconCache();

        int created = 0, updated = 0;

        var mainAssets = new List<MissionData>();
        foreach (var def in MAIN)
            mainAssets.Add(CreateOrUpdate(def, MainFolder, false, ref created, ref updated));

        // TỰ ĐỘNG SINH HÀNG TRĂM NHIỆM VỤ DÀY ĐẶC (SCALING DENSITY) CHO L1-L30
        string[] crops = { "rice", "huong_duong", "sugarcane", "ngo", "hoa_hong", "cachua", "carot", "lemon" };
        string[] dishes = { "com_chien_trung", "bo_ham_ca_rot", "ga_xao_ot", "trung_chien_ca_chua", "pho_bo_tai" }; // FIXED 2026-08-19: pho_beef không tồn tại (dish thật: pho_bo_tai)
        
        for (int lvl = 1; lvl <= 30; lvl++)
        {
            // Mật độ nhiệm vụ tăng dần theo cấp: L1 có 3 nhiệm vụ, L30 có khoảng 12 nhiệm vụ
            int numQuests = 3 + (lvl / 3); 
            for (int q = 1; q <= numQuests; q++)
            {
                int r = (lvl * 13) + (q * 7); // Giả ngẫu nhiên
                string crop = crops[r % crops.Length];
                string dish = dishes[r % dishes.Length];
                
                int typeRand = (lvl + q) % 4; // Chia 4 loại hành động
                MissionDef procDef;
                
                if (typeRand == 0) 
                {
                    procDef = new MissionDef($"proc_h_{lvl}_{q}", $"Thu hoạch {5 * lvl} {crop}", lvl, MissionEventType.HarvestItem, crop, 5 * lvl, 50 * lvl, RewardType.Coin, crop);
                } 
                else if (typeRand == 1) 
                {
                    procDef = new MissionDef($"proc_c_{lvl}_{q}", $"Nấu {1 + lvl/2} món {dish}", lvl, MissionEventType.CookDish, dish, 1 + lvl/2, 100 * lvl, RewardType.Coin, dish);
                    // FIXED 2026-08-19: pho_beef không tồn tại — áp bảng override đã duyệt
                    // (target + tên hiển thị tiếng Việt) cho 12 mission proc_c từng trỏ pho_beef.
                    if (PROC_COOK_OVERRIDES.TryGetValue(procDef.id, out var fix))
                    {
                        procDef.name     = fix.name;
                        procDef.target   = fix.target;
                        procDef.iconHint = fix.target;
                    }
                } 
                else if (typeRand == 2)
                {
                    procDef = new MissionDef($"proc_d_{lvl}_{q}", $"Giao {2 * lvl} đơn hàng", lvl, MissionEventType.DeliverOrder, "", 2 * lvl, 80 * lvl, RewardType.Coin, "");
                }
                else
                {
                    procDef = new MissionDef($"proc_p_{lvl}_{q}", $"Chế biến {3 * lvl} sản phẩm", lvl, MissionEventType.CollectAnimalProduct, "", 3 * lvl, 70 * lvl, RewardType.Coin, "");
                }
                
                mainAssets.Add(CreateOrUpdate(procDef, MainFolder, false, ref created, ref updated));
            }
        }

        var dailyAssets = new List<MissionData>();
        foreach (var def in DAILY)
            dailyAssets.Add(CreateOrUpdate(def, DailyFolder, true, ref created, ref updated));

        var achievementAssets = new List<MissionData>();
        foreach (var def in ACHIEVEMENTS)
            achievementAssets.Add(CreateOrUpdate(def, AchievementFolder, false, ref created, ref updated));

        // TỰ ĐỘNG SINH HÀNG TRĂM THÀNH TỰU THEO MỐC (MICRO-TIERS)
        // 1. Thu hoạch nông sản (30 Mốc)
        for (int i = 1; i <= 30; i++) {
            int target = i * 150;
            var def = new MissionDef($"a_proc_harvest_{i}", $"Nông dân Siêu Hạng (Mốc {i}) — Thu hoạch {target} nông sản", 1, MissionEventType.HarvestItem, "", target, i * 300, RewardType.Coin, "");
            achievementAssets.Add(CreateOrUpdate(def, AchievementFolder, false, ref created, ref updated));
        }
        
        // 2. Giao hàng (30 Mốc)
        for (int i = 1; i <= 30; i++) {
            int target = i * 40;
            var def = new MissionDef($"a_proc_order_{i}", $"Thương lái tài ba (Mốc {i}) — Giao {target} đơn hàng", 1, MissionEventType.DeliverOrder, "", target, i * 400, RewardType.Coin, "");
            achievementAssets.Add(CreateOrUpdate(def, AchievementFolder, false, ref created, ref updated));
        }
        
        // 3. Nấu ăn (30 Mốc)
        for (int i = 1; i <= 30; i++) {
            int target = i * 25;
            var def = new MissionDef($"a_proc_cook_{i}", $"Vua Đầu Bếp (Mốc {i}) — Nấu {target} món ăn", 1, MissionEventType.CookDish, "", target, i * 350, RewardType.Coin, "");
            achievementAssets.Add(CreateOrUpdate(def, AchievementFolder, false, ref created, ref updated));
        }
        
        // 4. Các mốc vật phẩm đặc thù (Lúa, Cà chua, Bò hầm...)
        for (int i = 1; i <= 15; i++) {
            int targetRice = i * 100;
            var defRice = new MissionDef($"a_proc_rice_{i}", $"Vua Lúa Nước (Mốc {i}) — Gặt {targetRice} bó lúa", 1, MissionEventType.HarvestItem, "rice", targetRice, i * 200, RewardType.Coin, "rice");
            achievementAssets.Add(CreateOrUpdate(defRice, AchievementFolder, false, ref created, ref updated));
            
            int targetBeef = i * 15;
            var defBeef = new MissionDef($"a_proc_beefdish_{i}", $"Chuyên gia Bò Hầm (Mốc {i}) — Nấu {targetBeef} món bò", 1, MissionEventType.CookDish, "bo_ham_ca_rot", targetBeef, i * 500, RewardType.Coin, "bo_ham_ca_rot");
            achievementAssets.Add(CreateOrUpdate(defBeef, AchievementFolder, false, ref created, ref updated));
        }

        // Thành tựu LÊN CẤP: Chỉ tới Cấp 30 theo đúng game design.
        for (int lvl = 2; lvl <= 30; lvl++)
        {
            var def = new MissionDef($"a_reach_level_{lvl}", $"Nông dân cấp {lvl}", 1,
                MissionEventType.ReachLevel, "", lvl, lvl * 100, RewardType.Coin, "");
            achievementAssets.Add(CreateOrUpdate(def, AchievementFolder, false, ref created, ref updated));
        }

        // Database chính: GHI ĐÈ list (gỡ mission mẫu cũ khỏi DB,
        // asset cũ vẫn giữ trên đĩa để tái dùng icon)
        var mainDb = LoadOrCreateDatabase(MainDbPath);
        mainDb.missions.Clear();
        mainDb.missions.AddRange(mainAssets);
        EditorUtility.SetDirty(mainDb);

        var dailyDb = LoadOrCreateDatabase(DailyDbPath);
        dailyDb.missions.Clear();
        dailyDb.missions.AddRange(dailyAssets);
        EditorUtility.SetDirty(dailyDb);

        var achievementDb = LoadOrCreateDatabase(AchievementDbPath);
        achievementDb.missions.Clear();
        achievementDb.missions.AddRange(achievementAssets);
        EditorUtility.SetDirty(achievementDb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Tự gán achievement DB vào scene (giống LevelUpRewardDataSetupTool gán configs):
        //   - PopupEwarManager.achievementMissionDatabase (nguồn resolve chính, luôn có trong scene)
        //   - UnifiedTaskPopupUI.achievementDatabase (nếu có instance đặt sẵn trong scene)
        int wired = AssignAchievementDatabase(achievementDb);

        Debug.Log($"[MissionSetup] Hoàn tất: {MAIN.Length} mission chính (L1-L30) + {DAILY.Length} mission ngày + " +
                  $"{ACHIEVEMENTS.Length} thành tựu. Tạo mới {created}, cập nhật {updated}. " +
                  $"DB: Main/Daily/Achievement đã ghi đè. Gán achievement DB vào {wired} component scene " +
                  $"(0 = chưa có component trong scene, sẽ tự resolve runtime nếu popup được tạo lúc chạy).");
    }

    /// <summary>Gán MissionDatabase_Achievement vào các component trong scene (idempotent).
    /// Trả về số component đã gán.</summary>
    private static int AssignAchievementDatabase(MissionDatabase achievementDb)
    {
        int wired = 0;

        var ewar = Object.FindFirstObjectByType<PopupEwarManager>(FindObjectsInactive.Include);
        if (ewar != null)
        {
            var so   = new SerializedObject(ewar);
            var prop = so.FindProperty("achievementMissionDatabase");
            if (prop != null)
            {
                prop.objectReferenceValue = achievementDb;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ewar);
                wired++;
            }
        }

        var popup = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            var so   = new SerializedObject(popup);
            var prop = so.FindProperty("achievementDatabase");
            if (prop != null)
            {
                prop.objectReferenceValue = achievementDb;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(popup);
                wired++;
            }
        }

        return wired;
    }

    private static MissionData CreateOrUpdate(MissionDef def, string folder, bool isDaily,
                                              ref int created, ref int updated)
    {
        string path  = $"{folder}/Mission_{def.id}.asset";
        var    asset = AssetDatabase.LoadAssetAtPath<MissionData>(path);

        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(asset, path);
            created++;
        }
        else
        {
            updated++;
        }

        asset.missionId     = def.id;
        asset.missionName   = def.name;
        asset.requiredLevel = def.level;
        asset.eventType     = def.type;
        asset.targetItemId  = def.target;
        asset.targetAmount  = def.amount;
        asset.rewardAmount  = def.reward;
        asset.rewardType    = def.rewardType;
        asset.isDaily       = isDaily;

        // Icon: chỉ gán khi đang trống — giữ icon nếu designer đã chỉnh tay
        if (asset.missionIcon == null)
            asset.missionIcon = FindIcon(def.iconHint);
        if (asset.rewardIcon == null)
            asset.rewardIcon = def.rewardType == RewardType.Coin ? _coinIcon : _diamondIcon;

        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static MissionDatabase LoadOrCreateDatabase(string path)
    {
        var db = AssetDatabase.LoadAssetAtPath<MissionDatabase>(path);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MissionDatabase>();
            AssetDatabase.CreateAsset(db, path);
            Debug.Log($"[MissionSetup] Tạo database mới: {path}");
        }
        if (db.missions == null)
            db.missions = new List<MissionData>();
        return db;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf   = System.IO.Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    // ─── Check ───────────────────────────────────────────────────────────────

    [MenuItem(MENU_CHECK)]
    public static void CheckMissions()
    {
        int fails = 0, checkedCount = 0;
        var seenIds = new HashSet<string>();

        CheckDatabase(MainDbPath,        "Main",        seenIds, ref checkedCount, ref fails);
        CheckDatabase(DailyDbPath,       "Daily",       seenIds, ref checkedCount, ref fails);
        CheckDatabase(AchievementDbPath, "Achievement", seenIds, ref checkedCount, ref fails);

        if (checkedCount == 0)
        {
            Debug.LogError("[MissionCheck] FAIL — không tìm thấy mission nào. Chạy '" + MENU_SETUP + "' trước.");
            return;
        }

        if (fails == 0)
            Debug.Log($"[MissionCheck] PASS — {checkedCount} mission hợp lệ (eventType OK, targetAmount > 0, requiredLevel 1-30, không trùng missionId).");
        else
            Debug.LogError($"[MissionCheck] FAIL — {fails}/{checkedCount} mission lỗi. Xem log phía trên.");
    }

    private static void CheckDatabase(string dbPath, string label, HashSet<string> seenIds,
                                      ref int checkedCount, ref int fails)
    {
        var db = AssetDatabase.LoadAssetAtPath<MissionDatabase>(dbPath);
        if (db == null)
        {
            Debug.LogError($"[MissionCheck] FAIL — thiếu database {label}: {dbPath}");
            fails++;
            return;
        }

        if (db.missions == null || db.missions.Count == 0)
        {
            Debug.LogError($"[MissionCheck] FAIL — database {label} rỗng: {dbPath}");
            fails++;
            return;
        }

        for (int i = 0; i < db.missions.Count; i++)
        {
            var m = db.missions[i];
            checkedCount++;

            if (m == null)
            {
                Debug.LogError($"[MissionCheck] FAIL — {label}[{i}] là null reference.");
                fails++;
                continue;
            }

            string id = m.MissionId;

            if (!System.Enum.IsDefined(typeof(MissionEventType), m.eventType))
            {
                Debug.LogError($"[MissionCheck] FAIL — '{id}': eventType không hợp lệ ({(int)m.eventType}).");
                fails++;
            }

            if (m.targetAmount <= 0)
            {
                Debug.LogError($"[MissionCheck] FAIL — '{id}': targetAmount phải > 0 (hiện {m.targetAmount}).");
                fails++;
            }

            if (m.requiredLevel < 1 || m.requiredLevel > 30)
            {
                Debug.LogError($"[MissionCheck] FAIL — '{id}': requiredLevel phải trong 1-30 (hiện {m.requiredLevel}).");
                fails++;
            }

            if (!seenIds.Add(id))
            {
                Debug.LogError($"[MissionCheck] FAIL — missionId trùng lặp: '{id}' (key claimed/tiến độ sẽ đụng nhau).");
                fails++;
            }

            if (label == "Daily" && !m.isDaily)
                Debug.LogWarning($"[MissionCheck] WARN — '{id}' nằm trong database Daily nhưng isDaily=false.");
        }
    }
}
