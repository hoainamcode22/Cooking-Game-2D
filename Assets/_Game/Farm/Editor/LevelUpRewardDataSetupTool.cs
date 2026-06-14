using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Level Up Popup/...
///
/// Tự động tạo/cập nhật LevelRewardConfig assets cho Level 2→30.
///   - Idempotent: chạy nhiều lần không tạo duplicate, giá trị luôn được
///     ghi đè về đúng REWARD_TABLE (L2-L10: bảng demo đã duyệt;
///     L11-L30: bảng mở rộng tới cấp tối đa, maxLevel=30)
///   - Tự scan CropData + InventoryItemData để lấy icon thật
///     (displayName dùng đúng bản đã duyệt trong REWARD_TABLE)
///   - Tự gán configs vào LevelUpPopupUI trong scene
///   - Tự tìm và gán VFX Confetti prefab
///   - Report duplicate assets cũ (không xóa tự động)
/// </summary>
public static class LevelUpRewardDataSetupTool
{
    private const string MENU_SETUP = "Tools/Farm Game/Setup Level Up Popup/Setup Reward Data (L2-L30)";
    private const string MENU_SCAN  = "Tools/Farm Game/Setup Level Up Popup/Scan Item Database";
    private const string FOLDER     = "Assets/_Game/Farm/data/Lever Game";

    // ─── Item Info Lookup ─────────────────────────────────────────────────────

    private struct ItemInfo
    {
        public string displayName;
        public Sprite icon;
    }

    private static Dictionary<string, ItemInfo> _cache;

    private static void BuildCache()
    {
        _cache = new Dictionary<string, ItemInfo>(System.StringComparer.OrdinalIgnoreCase);

        // 1. CropData assets — seed + harvest IDs
        foreach (string guid in AssetDatabase.FindAssets("t:CropData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
            if (crop == null) continue;

            var info = new ItemInfo { displayName = crop.itemName, icon = crop.itemIcon };
            Register(crop.itemID,        info);
            Register(crop.seedItemId,    info);
            Register(crop.harvestItemId, info);
            Register(crop.cropId,        info);
        }

        // 2. Generic ScriptableObject scan for animal / cooking items
        //    dùng reflection để không cần cast sang class cụ thể
        string[] extraFolders =
        {
            "Assets/_Game/Farm/data/Farm_dong_vat",
            "Assets/_Game/Farm/data/Item_Kho_Cook",
        };
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string folder in extraFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
            {
                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (so == null) continue;

                var t   = so.GetType();
                var fId = t.GetField("itemId", BF) ?? t.GetField("itemID", BF);
                if (fId == null || fId.FieldType != typeof(string)) continue;

                string id = fId.GetValue(so) as string;
                if (string.IsNullOrEmpty(id)) continue;

                string name = "";
                var fName = t.GetField("displayName", BF) ?? t.GetField("itemName", BF);
                if (fName != null && fName.FieldType == typeof(string))
                    name = fName.GetValue(so) as string ?? "";

                Sprite icon = null;
                var fIcon = t.GetField("icon", BF) ?? t.GetField("itemIcon", BF);
                if (fIcon != null && fIcon.FieldType == typeof(Sprite))
                    icon = fIcon.GetValue(so) as Sprite;

                Register(id, new ItemInfo { displayName = name, icon = icon });
            }
        }

        Debug.Log($"[LevelUpSetup] Item cache built: {_cache.Count} entries.");
    }

    private static void Register(string key, ItemInfo info)
    {
        if (!string.IsNullOrEmpty(key) && !_cache.ContainsKey(key))
            _cache[key] = info;
    }

    private static (string name, Sprite icon, bool found) Lookup(string id, string fallback)
    {
        if (_cache != null && _cache.TryGetValue(id, out var v))
            return (string.IsNullOrEmpty(v.displayName) ? fallback : v.displayName, v.icon, true);
        return (fallback, null, false);
    }

    // ─── Default Reward Table (L2-L30) ───────────────────────────────────────
    //     L2-L10: bảng thưởng demo Cấp 1-10 đã duyệt.
    //     L11-L30: bảng mở rộng tới cấp tối đa (maxLevel=30 — PlayerProgressManager).
    //     displayName giữ nguyên như bảng;
    //     icon được tra tự động từ item database khi chạy tool.
    //     Đồng bộ với assets: Assets/_Game/Farm/data/Lever Game/LevelReward_L*.asset

    private class GiftDef
    {
        public readonly string id;
        public readonly string name;
        public readonly int    amount;

        public GiftDef(string id, string name, int amount)
        {
            this.id     = id;
            this.name   = name;
            this.amount = amount;
        }
    }

    private class LevelDef
    {
        public int       level;
        public int       gold;
        public int       gems;
        public GiftDef[] gifts;
        public string[]  unlocks;
        public string    hint;
    }

    private static readonly LevelDef[] REWARD_TABLE =
    {
        new LevelDef
        {
            level = 2, gold = 150, gems = 2,
            gifts   = new[] { new GiftDef("seed_ngo", "Hạt Ngô", 3) },
            unlocks = new[]
            {
                "Mở khóa hạt Ngô",
                "Chuồng gà đã mở bán trong Shop",
                "Nhà dân mới sẽ mở ở cấp 3",
            },
            hint = "Bạn đã lên cấp 2! Hạt giống mới đã sẵn sàng để trồng.",
        },
        new LevelDef
        {
            level = 3, gold = 200, gems = 2,
            gifts   = new[] { new GiftDef("seed_cachua", "Hạt Cà chua", 3) },
            unlocks = new[]
            {
                "Mở khóa Cà chua và Cà rốt",
                "Thêm 1 nhà dân nhận đơn hàng",
            },
            hint = "Trồng Cà chua và Cà rốt để hoàn thành đơn hàng từ nhà dân mới nhé!",
        },
        new LevelDef
        {
            level = 4, gold = 250, gems = 3,
            gifts   = new[] { new GiftDef("seed_hoa_hong", "Hạt Hoa hồng", 2) },
            unlocks = new[]
            {
                "Mở khóa Hoa hồng và Oải hương",
                "Chuồng heo đã mở bán trong Shop",
            },
            hint = "Hoa hồng và Oải hương đã có trong Shop. Ghé xem chuồng heo mới nhé!",
        },
        new LevelDef
        {
            // LƯU Ý: id hạt Khoai tây KHÔNG có prefix seed_ (theo CropData)
            level = 5, gold = 300, gems = 3,
            gifts   = new[] { new GiftDef("khoai_tay", "Khoai tây giống", 5) },
            unlocks = new[]
            {
                "MỞ KHÓA NHÀ BẾP — nấu ăn ngay!",
                "Mở khóa Khoai tây",
                "Thêm 1 nhà dân nhận đơn hàng",
            },
            hint = "Bếp nấu ăn đã mở! Hãy nấu món đầu tiên rồi mang về kho để giao cho dân làng.",
        },
        new LevelDef
        {
            level = 6, gold = 350, gems = 3,
            gifts   = new[] { new GiftDef("seed_nam", "Hạt Nấm", 3) },
            unlocks = new[]
            {
                "Mở khóa Nấm",
                "Chuồng bò đã mở bán trong Shop",
                "Nhiệm vụ hằng ngày đã mở",
            },
            hint = "Nấm đã có trong Shop. Đừng quên làm nhiệm vụ hằng ngày để nhận thêm thưởng!",
        },
        new LevelDef
        {
            level = 7, gold = 400, gems = 4,
            gifts   = new[] { new GiftDef("seed_sugarcane", "Hạt Mía", 3) },
            unlocks = new[]
            {
                "Mở khóa Mía",
                "Mở khóa Hoa lan, Cúc trắng",
                "Thêm 1 nhà dân nhận đơn hàng",
            },
            hint = "Mía ngọt và hoa mới đã sẵn sàng. Thêm nhà dân là thêm đơn hàng mới!",
        },
        new LevelDef
        {
            level = 8, gold = 450, gems = 4,
            gifts   = new[] { new GiftDef("seed_lemon", "Hạt Chanh", 3) },
            unlocks = new[]
            {
                "Mở khóa Chanh",
                "Dân làng bắt đầu đặt món thịt bò",
            },
            hint = "Chanh đã có trong Shop. Dân làng bắt đầu thèm các món thịt bò đấy!",
        },
        new LevelDef
        {
            level = 9, gold = 500, gems = 5,
            gifts   = new[] { new GiftDef("seed_chili", "Hạt Ớt", 2) },
            unlocks = new[]
            {
                "Mở khóa Ớt",
                "Mở khóa Tulip, Cúc vạn thọ",
                "Thêm 1 nhà dân nhận đơn hàng",
            },
            hint = "Ớt đã sẵn sàng để trồng. Tulip và Cúc vạn thọ sẽ tô điểm cho nông trại.",
        },
        new LevelDef
        {
            level = 10, gold = 600, gems = 8,
            gifts   = new[] { new GiftDef("seed_pepper", "Hạt Tiêu", 3) },
            unlocks = new[]
            {
                "Mở khóa Tiêu và các loại hoa còn lại",
                "Bạn đã hoàn thành hành trình Cấp 1-10!",
            },
            hint = "Chúc mừng! Bạn đã hoàn thành hành trình Cấp 1-10. Hãy tiếp tục phát triển nông trại nhé!",
        },
    };

    // ─── L11-L30: sinh tự động (bảng mở rộng tới maxLevel=30) ─────────────────
    //     Gold 700 → 2600 (+100/cấp) · Gems tăng theo band · quà xoay vòng hạt cấp cao.
    //     Unlock text = teaser roadmap các tính năng sắp ra mắt.

    private static List<LevelDef> GetFullTable()
    {
        var table = new List<LevelDef>(REWARD_TABLE);

        var seedCycle = new (string id, string name)[]
        {
            ("seed_chili", "Hạt Ớt"), ("seed_pepper", "Hạt Tiêu"), ("seed_lemon", "Hạt Chanh"),
            ("seed_sugarcane", "Hạt Mía"), ("seed_nam", "Hạt Nấm"),
        };

        for (int lv = 11; lv <= 30; lv++)
        {
            var seed   = seedCycle[(lv - 11) % seedCycle.Length];
            int amount = lv <= 12 ? 3 : (lv <= 17 ? 4 : 5);
            int gems   = lv <= 13 ? 5 : lv <= 16 ? 6 : lv <= 19 ? 7 : lv <= 22 ? 8 : lv <= 25 ? 9 : lv <= 29 ? 10 : 15;

            string teaser;
            switch (lv)
            {
                case 14: teaser = "Máy chế biến nông sản (sắp ra mắt)"; break;
                case 15: teaser = "Mở rộng nông trại (sắp ra mắt)"; break;
                case 17: teaser = "Hồ cá (sắp ra mắt)"; break;
                case 18: teaser = "Sẵn sàng cho 2 món cá khi có hồ cá"; break;
                case 20: teaser = "Bến tàu du lịch (sắp ra mắt)"; break;
                case 25: teaser = "Sự kiện mùa vụ (sắp ra mắt)"; break;
                case 30: teaser = "Bạn đã đạt cấp tối đa — cảm ơn bạn đã chơi!"; break;
                default: teaser = "Phần thưởng cấp cao"; break;
            }

            table.Add(new LevelDef
            {
                level   = lv,
                gold    = 700 + (lv - 11) * 100,
                gems    = gems,
                gifts   = new[] { new GiftDef(seed.id, seed.name, amount) },
                unlocks = new[] { teaser },
                hint    = lv == 30
                    ? "Cấp tối đa! Nông trại của bạn thật tuyệt vời."
                    : "Tiếp tục thu hoạch và giao đơn để mở các tính năng sắp ra mắt nhé!",
            });
        }
        return table;
    }

    // ─── Main Entry ──────────────────────────────────────────────────────────

    [MenuItem(MENU_SETUP)]
    public static void SetupRewardData()
    {
        BuildCache();
        EnsureFolder(FOLDER);

        var log       = new System.Text.StringBuilder();
        var configs   = new List<LevelRewardConfig>();
        var fullTable = GetFullTable();

        log.AppendLine("[LevelUpSetup] ════════════════════════════════════════");

        // ── L2 → L30 từ bảng đầy đủ ──────────────────────────────────────────
        foreach (var def in fullTable)
        {
            var gifts = new LevelRewardConfig.ItemGift[def.gifts.Length];
            for (int i = 0; i < def.gifts.Length; i++)
                gifts[i] = G(def.gifts[i].id, def.gifts[i].name, def.gifts[i].amount, log);

            configs.Add(Build($"LevelReward_L{def.level}", def.level, def.gold, def.gems,
                gifts, def.unlocks, def.hint, log));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Assign configs to LevelUpPopupUI ─────────────────────────────────
        // FindObjectsInactive.Include: popup thường inactive trong scene — bản cũ tìm không ra nên không gán config.
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup != null)
        {
            var so   = new SerializedObject(popup);
            var prop = so.FindProperty("levelRewardConfigs");
            prop.ClearArray();
            for (int i = 0; i < configs.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(popup);
            log.AppendLine($"[LevelUpSetup] Assigned {configs.Count} configs to LevelUpPopupUI '{popup.gameObject.name}'");
        }
        else
        {
            log.AppendLine("[LevelUpSetup] ⚠ LevelUpPopupUI không tìm thấy trong scene — configs chưa gán.");
            log.AppendLine("[LevelUpSetup]   Chạy Setup Level Up Popup trước, rồi chạy lại tool này.");
        }

        // ── Find and assign VFX Confetti ─────────────────────────────────────
        var vfx = FindConfetti(log);
        if (vfx != null && popup != null)
        {
            var so = new SerializedObject(popup);
            so.FindProperty("vfxConfettiPrefab").objectReferenceValue = vfx;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(popup);
        }

        // ── Report duplicate assets ───────────────────────────────────────────
        ReportDuplicates(log);

        log.AppendLine("[LevelUpSetup] DONE ════════════════════════════════════");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("Level Up Reward Setup",
            $"✅ Setup xong!\n\n" +
            $"• {fullTable.Count} LevelRewardConfig (L2-L30): tạo / cập nhật\n" +
            $"• LevelUpPopupUI: {(popup != null ? "✅ gán configs xong" : "❌ không tìm thấy\n  → Chạy Setup Level Up Popup trước")}\n" +
            $"• VFX Confetti: {(vfx != null ? "✅ tự gán" : "⚠ không tìm thấy — gán tay sau")}\n\n" +
            "Còn thủ công:\n" +
            "• Kiểm tra icon gift items trong Inspector (icon tra tự động từ CropData/ItemData — gán tay nếu thiếu)\n\n" +
            "Xem Console để biết chi tiết từng item.",
            "OK");
    }

    // ─── Scan Report (debug) ─────────────────────────────────────────────────

    [MenuItem(MENU_SCAN)]
    public static void ScanItems()
    {
        BuildCache();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[LevelUpSetup] ═══ Item Database ({_cache.Count} entries) ═══");
        foreach (var kv in _cache)
            sb.AppendLine($"  {kv.Key,-30} → \"{kv.Value.displayName,-20}\"  icon={(kv.Value.icon != null ? "✅" : "❌")}");
        sb.AppendLine("[LevelUpSetup] ═══════════════════════════════════════════");
        Debug.Log(sb.ToString());
    }

    // ─── Build Helper ─────────────────────────────────────────────────────────

    private static LevelRewardConfig Build(
        string assetName,
        int lvl, int gold, int gems,
        LevelRewardConfig.ItemGift[] gifts,
        string[] unlocks,
        string hint,
        System.Text.StringBuilder log)
    {
        var cfg = LoadOrCreate(assetName);
        cfg.levelReached       = lvl;
        cfg.giftGold           = gold;
        cfg.giftGems           = gems;
        cfg.giftItems          = new List<LevelRewardConfig.ItemGift>(gifts);
        cfg.unlockDescriptions = new List<string>(unlocks);
        cfg.hintText           = hint;
        EditorUtility.SetDirty(cfg);
        log.AppendLine($"[LevelUpSetup] Loaded/Created {assetName}.asset  (L{lvl}  +{gold}g  +{gems}💎)");
        return cfg;
    }

    // Shorthand: make a gift item.
    // displayName dùng đúng bản đã duyệt trong REWARD_TABLE (không bị DB ghi đè);
    // chỉ icon được tra từ item database.
    private static LevelRewardConfig.ItemGift G(
        string itemId, string displayName, int amount, System.Text.StringBuilder log)
    {
        var (_, icon, found) = Lookup(itemId, displayName);
        log.AppendLine($"  gift: {itemId,-20} | {displayName,-15} | x{amount,2} | icon {(found && icon != null ? "found ✅" : "missing ⚠")}");
        return new LevelRewardConfig.ItemGift
        {
            itemId      = itemId,
            displayName = displayName,
            icon        = icon,
            amount      = amount,
        };
    }

    private static LevelRewardConfig LoadOrCreate(string assetName)
    {
        string path     = $"{FOLDER}/{assetName}.asset";
        var    existing = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(path);
        if (existing != null) return existing;

        var cfg = ScriptableObject.CreateInstance<LevelRewardConfig>();
        AssetDatabase.CreateAsset(cfg, path);
        return cfg;
    }

    // ─── VFX ─────────────────────────────────────────────────────────────────

    private static GameObject FindConfetti(System.Text.StringBuilder log)
    {
        string[] terms = { "Confetti_blast_multicolor", "Confetti_blast", "Confetti", "Firework", "Sparkle" };
        foreach (string term in terms)
        {
            string[] guids = AssetDatabase.FindAssets($"{term} t:prefab");
            if (guids.Length == 0) continue;
            string path   = AssetDatabase.GUIDToAssetPath(guids[0]);
            var    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                log.AppendLine($"[LevelUpSetup] Confetti prefab assigned: {path} ✅");
                return prefab;
            }
        }
        log.AppendLine("[LevelUpSetup] ⚠ Confetti prefab không tìm thấy — gán tay vào vfxConfettiPrefab.");
        return null;
    }

    // ─── Duplicate Report ────────────────────────────────────────────────────

    private static void ReportDuplicates(System.Text.StringBuilder log)
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelRewardConfig", new[] { FOLDER });
        var      dups  = new List<string>();

        foreach (string g in guids)
        {
            string fname  = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g));
            string[] parts = fname.Split(' ');
            if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out _))
                dups.Add(AssetDatabase.GUIDToAssetPath(g));
        }

        if (dups.Count > 0)
        {
            log.AppendLine($"[LevelUpSetup] ⚠ Duplicate assets ({dups.Count} — chưa xóa, chỉ report):");
            foreach (string d in dups)
                log.AppendLine($"  {d}");
            log.AppendLine("[LevelUpSetup]   → Xóa tay trong Project window nếu không cần.");
        }
        else
        {
            log.AppendLine("[LevelUpSetup] Không có duplicate assets.");
        }
    }

    // ─── Folder Helper ────────────────────────────────────────────────────────

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int    sep    = path.LastIndexOf('/');
        string parent = path.Substring(0, sep);
        string child  = path.Substring(sep + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }

    [MenuItem(MENU_SETUP, true)]
    [MenuItem(MENU_SCAN,  true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
