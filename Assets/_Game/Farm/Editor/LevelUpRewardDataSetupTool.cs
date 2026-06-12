using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Level Up Popup/...
///
/// Tự động tạo/cập nhật LevelRewardConfig assets cho Level 2→6.
///   - Idempotent: chạy nhiều lần không tạo duplicate
///   - Tự scan CropData + InventoryItemData để lấy displayName / icon thật
///   - Tự gán configs vào LevelUpPopupUI trong scene
///   - Tự tìm và gán VFX Confetti prefab
///   - Report duplicate assets cũ (không xóa tự động)
/// </summary>
public static class LevelUpRewardDataSetupTool
{
    private const string MENU_SETUP = "Tools/Farm Game/Setup Level Up Popup/Setup Reward Data (L2-L6)";
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

    // ─── Main Entry ──────────────────────────────────────────────────────────

    [MenuItem(MENU_SETUP)]
    public static void SetupRewardData()
    {
        BuildCache();
        EnsureFolder(FOLDER);

        var log     = new System.Text.StringBuilder();
        var configs = new List<LevelRewardConfig>();

        log.AppendLine("[LevelUpSetup] ════════════════════════════════════════");

        // ── Level 2 ───────────────────────────────────────────────────────────
        configs.Add(Build("LevelReward_L2", 2, 50, 0,
            new[] { G("seed_ngo", "Ngô", 5, log) },
            new[]
            {
                "Mở khóa hạt ngô",
                "Mở khóa cà chua",
                "Đơn hàng mới từ dân làng",
            },
            "Bạn đã lên cấp 2! Hạt giống mới đã sẵn sàng để trồng.",
            log));

        // ── Level 3 ───────────────────────────────────────────────────────────
        // chicken_coop là building, không có trong item database → fallback + log warning
        var coop = new LevelRewardConfig.ItemGift
        {
            itemId      = "chicken_coop",
            displayName = "Chuồng gà",
            icon        = null,
            amount      = 1,
        };
        log.AppendLine("  gift: chicken_coop       | Chuồng gà    | x1 | icon missing ⚠ (building — gán tay sau)");
        configs.Add(Build("LevelReward_L3", 3, 100, 0,
            new[] { coop },
            new[]
            {
                "Mở khóa chuồng gà",
                "Hướng dẫn cho gà ăn",
                "Có thể thu trứng từ gà",
            },
            "Từ cấp 3, bạn có thể bắt đầu chăm sóc gà và thu hoạch trứng.",
            log));

        // ── Level 4 ───────────────────────────────────────────────────────────
        configs.Add(Build("LevelReward_L4", 4, 50, 0,
            new[] { G("seed_cachua", "Cà Chua", 5, log) },
            new[]
            {
                "Mở khóa cà chua",
                "Mở thêm đơn hàng combo đơn giản",
                "Mở thêm vật phẩm trang trí/hoa",
            },
            "Cấp 4 mở thêm nguyên liệu mới để giao đơn hàng kiếm nhiều vàng hơn.",
            log));

        // ── Level 5 — BIG UNLOCK (Cooking) ───────────────────────────────────
        configs.Add(Build("LevelReward_L5", 5, 100, 10,
            new[]
            {
                G("rice",  "Lúa",   5, log),
                G("ngo",   "Ngô",   5, log),
                G("egg",   "Trứng", 3, log),
            },
            new[]
            {
                "Mở khóa bếp nấu ăn",
                "Mở 10 món ăn đầu tiên",
                "Có thể nấu món rồi đem giao đơn hàng",
            },
            "Bếp nấu ăn đã mở! Hãy nấu món đầu tiên rồi mang về kho để giao cho dân làng.",
            log));

        // ── Level 6 ───────────────────────────────────────────────────────────
        configs.Add(Build("LevelReward_L6", 6, 50, 0,
            new[] { G("mushroom", "Nấm", 5, log) },
            new[]
            {
                "Mở thêm nguyên liệu nấu ăn mới",
                "Mở thêm đơn hàng món ăn dễ",
                "Tutorial chính kết thúc, người chơi có thể tự cày đơn hàng",
            },
            "Bạn đã nắm được cách chơi cơ bản. Hãy tiếp tục trồng trọt, nấu ăn và giao đơn hàng!",
            log));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Assign configs to LevelUpPopupUI ─────────────────────────────────
        var popup = Object.FindFirstObjectByType<LevelUpPopupUI>();
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
            $"• 5 LevelRewardConfig (L2-L6): tạo / cập nhật\n" +
            $"• LevelUpPopupUI: {(popup != null ? "✅ gán configs xong" : "❌ không tìm thấy\n  → Chạy Setup Level Up Popup trước")}\n" +
            $"• VFX Confetti: {(vfx != null ? "✅ tự gán" : "⚠ không tìm thấy — gán tay sau")}\n\n" +
            "Còn thủ công:\n" +
            "• Kiểm tra icon gift items trong Inspector (chỉ gán nếu icon chưa đúng)\n" +
            "• Gán icon Chuồng gà (LevelReward_L3) — building không có trong item DB\n\n" +
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

    // Shorthand: make a gift item with lookup
    private static LevelRewardConfig.ItemGift G(
        string itemId, string fallbackName, int amount, System.Text.StringBuilder log)
    {
        var (name, icon, found) = Lookup(itemId, fallbackName);
        log.AppendLine($"  gift: {itemId,-20} | {name,-15} | x{amount,2} | icon {(found && icon != null ? "found ✅" : "missing ⚠")}");
        return new LevelRewardConfig.ItemGift
        {
            itemId      = itemId,
            displayName = name,
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
