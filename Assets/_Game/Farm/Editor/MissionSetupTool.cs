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
///   - Icon: tái dùng icon từ mission cũ / CropData / DishData / OrderItemDefinition /
///     BaseItemData (chuồng); không tìm thấy → để null (prefab giữ icon mặc định).
///
/// Tools/Farm Game/Test/Check Missions L1-L10: validate eventType hợp lệ,
/// targetAmount &gt; 0, requiredLevel 1-10, không trùng missionId — in PASS/FAIL.
/// </summary>
public static class MissionSetupTool
{
    private const string MENU_SETUP = "Tools/Farm Game/Setup Missions L1-L10";
    private const string MENU_CHECK = "Tools/Farm Game/Test/Check Missions L1-L10";

    private const string DataEwaFolder = "Assets/_Game/Farm/data/Data_Ewa";
    private const string MainFolder    = DataEwaFolder + "/Main_L1_L10";
    private const string DailyFolder   = DataEwaFolder + "/Daily_Missions";
    private const string MainDbPath    = DataEwaFolder + "/MissionDatabase_Main.asset";
    private const string DailyDbPath   = DataEwaFolder + "/MissionDatabase_Daily.asset";

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
        new MissionDef("main_l1_plant_rice",     "Trồng 6 ô lúa",              1, MissionEventType.PlantCrop,            "rice",            6,  50, RewardType.Coin,    "rice"),
        new MissionDef("main_l1_harvest_rice",   "Thu hoạch 24 lúa",           1, MissionEventType.HarvestItem,          "rice",           24,  80, RewardType.Coin,    "rice"),
        new MissionDef("main_l1_plant_flower",   "Trồng 2 ô hoa hướng dương",  1, MissionEventType.PlantCrop,            "huong_duong",     2,  50, RewardType.Coin,    "huong_duong"),
        new MissionDef("main_l1_reach_level_2",  "Đạt cấp 2",                  1, MissionEventType.ReachLevel,           "",                2,   1, RewardType.Diamond, ""),
        // L2
        new MissionDef("main_l2_feed_chicken",   "Cho gà ăn 1 lần",            2, MissionEventType.FeedAnimal,           "",                1,  50, RewardType.Coin,    ""),
        new MissionDef("main_l2_collect_eggs",   "Thu 4 quả trứng",            2, MissionEventType.CollectAnimalProduct, "egg",             4,  80, RewardType.Coin,    "egg"),
        new MissionDef("main_l2_deliver_1",      "Giao 1 đơn hàng",            2, MissionEventType.DeliverOrder,         "",                1, 100, RewardType.Coin,    ""),
        // L3
        new MissionDef("main_l3_buy_seed",       "Mua 1 hạt giống mới",        3, MissionEventType.BuySeed,              "",                1,  60, RewardType.Coin,    ""),
        new MissionDef("main_l3_deliver_3",      "Giao 3 đơn hàng",            3, MissionEventType.DeliverOrder,         "",                3, 150, RewardType.Coin,    ""),
        // L4
        new MissionDef("main_l4_buy_pig_pen",    "Mua chuồng heo",             4, MissionEventType.BuyShopItem,          "108",             1, 200, RewardType.Coin,    "108"),
        new MissionDef("main_l4_harvest_rose",   "Thu hoạch 4 hoa hồng",       4, MissionEventType.HarvestItem,          "hoa_hong",        4, 100, RewardType.Coin,    "hoa_hong"),
        // L5
        new MissionDef("main_l5_cook_first",     "Nấu món ăn đầu tiên",        5, MissionEventType.CookDish,             "",                1, 150, RewardType.Coin,    ""),
        new MissionDef("main_l5_deliver_dish",   "Giao 1 món cơm chiên trứng", 5, MissionEventType.DeliverOrder,         "com_chien_trung", 1, 150, RewardType.Coin,    "com_chien_trung"),
        new MissionDef("main_l5_reach_level_6",  "Đạt cấp 6",                  5, MissionEventType.ReachLevel,           "",                6,   2, RewardType.Diamond, ""),
        // L6
        new MissionDef("main_l6_cook_3",         "Nấu 3 món ăn",               6, MissionEventType.CookDish,             "",                3, 200, RewardType.Coin,    ""),
        new MissionDef("main_l6_buy_cow_pen",    "Mua chuồng bò",              6, MissionEventType.BuyShopItem,          "106",             1, 300, RewardType.Coin,    "106"),
        // L7
        new MissionDef("main_l7_deliver_5",      "Giao 5 đơn hàng",            7, MissionEventType.DeliverOrder,         "",                5, 250, RewardType.Coin,    ""),
        new MissionDef("main_l7_harvest_40",     "Thu hoạch 40 nông sản",      7, MissionEventType.HarvestItem,          "",               40, 200, RewardType.Coin,    ""),
        // L8
        new MissionDef("main_l8_cook_beef",      "Nấu món bò hầm cà rốt",      8, MissionEventType.CookDish,             "bo_ham_ca_rot",   1, 250, RewardType.Coin,    "bo_ham_ca_rot"),
        new MissionDef("main_l8_deliver_beef",   "Giao 1 đơn thịt bò",         8, MissionEventType.DeliverOrder,         "beef",            1, 300, RewardType.Coin,    "beef"),
        // L9 (thiết kế "đơn 2 món" — tracking hiện đếm mọi đơn giao thành công)
        new MissionDef("main_l9_deliver_3",      "Giao 3 đơn hàng",            9, MissionEventType.DeliverOrder,         "",                3, 350, RewardType.Coin,    ""),
        // L10
        new MissionDef("main_l10_reach_level_10","Đạt cấp 10",                10, MissionEventType.ReachLevel,           "",               10,   5, RewardType.Diamond, ""),
        new MissionDef("main_l10_deliver_20",    "Giao tổng cộng 20 đơn hàng",10, MissionEventType.DeliverOrder,         "",               20, 500, RewardType.Coin,    ""),
    };

    // Nhiệm vụ ngày — mở từ L6 (theo proposal §7), tiến độ tự reset mỗi ngày
    private static readonly MissionDef[] DAILY =
    {
        new MissionDef("daily_deliver_3",  "Giao 3 đơn hàng",           6, MissionEventType.DeliverOrder,         "",  3, 100, RewardType.Coin, ""),
        new MissionDef("daily_harvest_20", "Thu hoạch 20 nông sản",     6, MissionEventType.HarvestItem,          "", 20, 100, RewardType.Coin, ""),
        new MissionDef("daily_cook_2",     "Nấu 2 món ăn",              6, MissionEventType.CookDish,             "",  2, 120, RewardType.Coin, ""),
        new MissionDef("daily_feed_2",     "Cho vật nuôi ăn 2 lần",     6, MissionEventType.FeedAnimal,           "",  2,  80, RewardType.Coin, ""),
        new MissionDef("daily_buy_seed",   "Mua 1 hạt giống",           6, MissionEventType.BuySeed,              "",  1,  80, RewardType.Coin, ""),
        new MissionDef("daily_collect_8",  "Thu 8 sản phẩm vật nuôi",   6, MissionEventType.CollectAnimalProduct, "",  8, 120, RewardType.Coin, "egg"),
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

        // 4. OrderItemDefinition — itemId → icon (egg, beef, com_chien_trung...)
        foreach (string guid in AssetDatabase.FindAssets("t:OrderItemDefinition"))
        {
            var def = AssetDatabase.LoadAssetAtPath<Village.OrderItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (def == null || def.icon == null) continue;
            RegisterIcon(def.itemId, def.icon);
        }

        // 5. BaseItemData (gồm PlaceableItemData chuồng 106/108) — itemID → itemIcon
        foreach (string guid in AssetDatabase.FindAssets("t:BaseItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<BaseItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || item.itemIcon == null) continue;
            RegisterIcon(item.itemID, item.itemIcon);
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
        BuildIconCache();

        int created = 0, updated = 0;

        var mainAssets = new List<MissionData>();
        foreach (var def in MAIN)
            mainAssets.Add(CreateOrUpdate(def, MainFolder, false, ref created, ref updated));

        var dailyAssets = new List<MissionData>();
        foreach (var def in DAILY)
            dailyAssets.Add(CreateOrUpdate(def, DailyFolder, true, ref created, ref updated));

        // Database chính: GHI ĐÈ list = 23 mission mới (gỡ 20 mission mẫu cũ khỏi DB,
        // asset cũ vẫn giữ trên đĩa để tái dùng icon)
        var mainDb = LoadOrCreateDatabase(MainDbPath);
        mainDb.missions.Clear();
        mainDb.missions.AddRange(mainAssets);
        EditorUtility.SetDirty(mainDb);

        var dailyDb = LoadOrCreateDatabase(DailyDbPath);
        dailyDb.missions.Clear();
        dailyDb.missions.AddRange(dailyAssets);
        EditorUtility.SetDirty(dailyDb);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MissionSetup] Hoàn tất: {MAIN.Length} mission chính (L1-L10) + {DAILY.Length} mission ngày. " +
                  $"Tạo mới {created}, cập nhật {updated}. " +
                  $"MissionDatabase_Main = list mới (mission mẫu cũ đã gỡ khỏi DB), MissionDatabase_Daily = {DailyDbPath}");
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

        CheckDatabase(MainDbPath,  "Main",  seenIds, ref checkedCount, ref fails);
        CheckDatabase(DailyDbPath, "Daily", seenIds, ref checkedCount, ref fails);

        if (checkedCount == 0)
        {
            Debug.LogError("[MissionCheck] FAIL — không tìm thấy mission nào. Chạy '" + MENU_SETUP + "' trước.");
            return;
        }

        if (fails == 0)
            Debug.Log($"[MissionCheck] PASS — {checkedCount} mission hợp lệ (eventType OK, targetAmount > 0, requiredLevel 1-10, không trùng missionId).");
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

            if (m.requiredLevel < 1 || m.requiredLevel > 10)
            {
                Debug.LogError($"[MissionCheck] FAIL — '{id}': requiredLevel phải trong 1-10 (hiện {m.requiredLevel}).");
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
