using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Village;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Village Orders L1-L6
///
/// Batch-set unlockLevel (và reward values) cho tất cả OrderItemDefinition assets
/// trong thư mục Assets/_Game/Farm/data/Village_data/ theo đúng thiết kế Phase 1.
///
/// An toàn:
///   - Chỉ sửa unlockLevel, goldPerUnit, expPerUnit, minAmount, maxAmount
///   - Không xóa asset nào
///   - Hỗ trợ Undo qua AssetDatabase
///   - Có thể chạy lại nhiều lần (idempotent)
/// </summary>
public static class VillageOrdersL1L6SetupTool
{
    private const string MENU_APPLY  = "Tools/Farm Game/Setup Village Orders L1-L6/Apply Phase 1 Data";
    private const string MENU_REPORT = "Tools/Farm Game/Setup Village Orders L1-L6/Report Current State";

    // ── Phase 1 Data Design ───────────────────────────────────────────────────
    // Key = itemId trong asset (lowercase, trimmed).
    // Cooking dishes (Order_Item_*) bắt đầu bằng prefix "dish_" hoặc chứa "_" đặc trưng.

    private struct ItemConfig
    {
        public int  unlockLevel;
        public int  goldPerUnit;
        public int  expPerUnit;
        public int  minAmount;
        public int  maxAmount;
        public int  weight;
    }

    private static readonly Dictionary<string, ItemConfig> RawItemConfigs = new()
    {
        // Level 1 — Đầu game (chỉ farm)
        ["rice"]         = new ItemConfig { unlockLevel=1, goldPerUnit=15, expPerUnit=3, minAmount=3,  maxAmount=8,  weight=20 },
        ["cabbage"]      = new ItemConfig { unlockLevel=1, goldPerUnit=18, expPerUnit=3, minAmount=2,  maxAmount=6,  weight=18 },

        // Level 2 — Mở ngô, cà chua
        ["corn"]         = new ItemConfig { unlockLevel=2, goldPerUnit=16, expPerUnit=3, minAmount=3,  maxAmount=7,  weight=16 },
        ["tomato"]       = new ItemConfig { unlockLevel=2, goldPerUnit=20, expPerUnit=4, minAmount=2,  maxAmount=5,  weight=15 },

        // Level 3 — Mở chuồng gà
        ["egg"]          = new ItemConfig { unlockLevel=3, goldPerUnit=18, expPerUnit=4, minAmount=3,  maxAmount=8,  weight=14 },
        ["chickenmeat"]  = new ItemConfig { unlockLevel=3, goldPerUnit=22, expPerUnit=5, minAmount=2,  maxAmount=5,  weight=12 },
        ["chicken_meat"] = new ItemConfig { unlockLevel=3, goldPerUnit=22, expPerUnit=5, minAmount=2,  maxAmount=5,  weight=12 },
        ["sunflower"]    = new ItemConfig { unlockLevel=3, goldPerUnit=15, expPerUnit=3, minAmount=2,  maxAmount=5,  weight=10 },

        // Level 4 — Mở hoa/cây phụ
        ["rose"]         = new ItemConfig { unlockLevel=4, goldPerUnit=18, expPerUnit=4, minAmount=2,  maxAmount=5,  weight=10 },
        ["lavender"]     = new ItemConfig { unlockLevel=4, goldPerUnit=18, expPerUnit=4, minAmount=2,  maxAmount=5,  weight=10 },

        // Level 5 — Mở chuồng heo, bếp nấu
        ["pork"]         = new ItemConfig { unlockLevel=5, goldPerUnit=25, expPerUnit=6, minAmount=2,  maxAmount=4,  weight=12 },

        // Level 6 — Mở nấm, khoai tây
        ["mushroom"]     = new ItemConfig { unlockLevel=6, goldPerUnit=22, expPerUnit=5, minAmount=2,  maxAmount=5,  weight=10 },
        ["potato"]       = new ItemConfig { unlockLevel=6, goldPerUnit=16, expPerUnit=3, minAmount=3,  maxAmount=6,  weight=10 },

        // Level 8+ — Ngoài scope Phase 1 (vẫn set để không lọt vào L1-6)
        ["beef"]         = new ItemConfig { unlockLevel=8, goldPerUnit=30, expPerUnit=7, minAmount=1,  maxAmount=4,  weight=8  },
        ["sugarcane"]    = new ItemConfig { unlockLevel=8, goldPerUnit=14, expPerUnit=3, minAmount=3,  maxAmount=7,  weight=8  },
        ["tulip"]        = new ItemConfig { unlockLevel=9, goldPerUnit=20, expPerUnit=4, minAmount=2,  maxAmount=5,  weight=8  },
    };

    // Tất cả cooking dish (Order_Item_*) → Level 5
    private static readonly ItemConfig CookingDishConfig = new ItemConfig
    {
        unlockLevel=5, goldPerUnit=0, expPerUnit=10, minAmount=1, maxAmount=2, weight=8
    };

    // goldPerUnit cho từng dish (key = filename partial)
    private static readonly Dictionary<string, int> DishGoldOverride = new()
    {
        ["trung_chien_ca_chua"]      = 120,
        ["trung_op_la_bo_ne"]        = 110,
        ["com_chien_trung"]          = 130,
        ["ga_xao_ot"]                = 140,
        ["sup_ngo_nam"]              = 135,
        ["nam_xao_thit_bo"]          = 155,
        ["bap_cai_xao_nam"]          = 125,
        ["thit_heo_luoc_cuon_rau"]   = 145,
        ["suon_heo_xao_chua_ngot"]   = 150,
        ["salad_nam_rau"]            = 120,
        ["pho_bo_tai"]               = 170,
        ["ga_nuong_lu"]              = 160,
        ["canh_chua_ca"]             = 140,
        ["ca_nuong_tieu"]            = 145,
        ["bo_xao_tieu"]              = 155,
        ["bo_ham_ca_rot"]            = 165,
        ["khoai_tay_chien"]          = 115,
        ["canh_khoai_tay_thit_heo"]  = 135,
        ["nuoc_mia_chanh"]           = 100,
    };

    // ── Apply ────────────────────────────────────────────────────────────────

    [MenuItem(MENU_APPLY)]
    public static void ApplyPhase1Data()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Setup Village Orders L1-L6",
            "Tool này sẽ batch-update unlockLevel, goldPerUnit, expPerUnit, minAmount, maxAmount\n" +
            "cho tất cả OrderItemDefinition assets trong:\n\n" +
            "Assets/_Game/Farm/data/Village_data/\n\n" +
            "Dữ liệu cũ sẽ bị ghi đè. Bạn có chắc không?",
            "Áp dụng Phase 1 Data", "Huỷ");

        if (!confirm) return;

        string searchFolder = "Assets/_Game/Farm/data/Village_data";
        string[] guids = AssetDatabase.FindAssets("t:OrderItemDefinition", new[] { searchFolder });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"[VillageOrdersSetupTool] Không tìm thấy OrderItemDefinition nào trong {searchFolder}");
            return;
        }

        int updated = 0, skipped = 0;
        var log = new System.Text.StringBuilder();
        log.AppendLine($"[VillageOrdersSetupTool] Xử lý {guids.Length} assets:");

        foreach (string guid in guids)
        {
            string path   = AssetDatabase.GUIDToAssetPath(guid);
            var    asset  = AssetDatabase.LoadAssetAtPath<OrderItemDefinition>(path);
            if (asset == null) { skipped++; continue; }

            string assetNameLower = asset.name.ToLower();
            string itemIdLower    = (asset.itemId ?? "").ToLower().Trim();

            // Phân loại: raw item hay cooking dish?
            bool isCookingDish = assetNameLower.StartsWith("order_item_") ||
                                 assetNameLower.StartsWith("dish_") ||
                                 ContainsCookingKeyword(assetNameLower);

            ItemConfig cfg;
            if (isCookingDish)
            {
                cfg = CookingDishConfig;
                // Override gold nếu có mapping cụ thể
                int dishGold = LookupDishGold(assetNameLower);
                if (dishGold > 0) cfg.goldPerUnit = dishGold;
            }
            else
            {
                // Tìm theo itemId trước, fallback theo tên asset
                if (!RawItemConfigs.TryGetValue(itemIdLower, out cfg) &&
                    !RawItemConfigs.TryGetValue(assetNameLower.Replace("orderitem_", "").Replace("order_item_", ""), out cfg))
                {
                    log.AppendLine($"  ⚠ SKIP  {asset.name,-45} (không có mapping — itemId='{asset.itemId}')");
                    skipped++;
                    continue;
                }
            }

            // Ghi data
            Undo.RecordObject(asset, "Phase1 Village Order Setup");

            asset.unlockLevel = cfg.unlockLevel;
            asset.goldPerUnit = cfg.goldPerUnit;
            asset.expPerUnit  = cfg.expPerUnit;
            asset.minAmount   = cfg.minAmount;
            asset.maxAmount   = cfg.maxAmount;
            asset.weight      = cfg.weight;

            EditorUtility.SetDirty(asset);

            log.AppendLine($"  ✓      {asset.name,-45} → L{cfg.unlockLevel,2}  {cfg.goldPerUnit}g/u  {cfg.expPerUnit}xp  " +
                           $"[{cfg.minAmount}-{cfg.maxAmount}]  w={cfg.weight}");
            updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine($"\nKết quả: ✓ {updated} cập nhật   ⚠ {skipped} bỏ qua");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("Setup Village Orders L1-L6",
            $"✅ Cập nhật xong!\n\n" +
            $"• {updated} asset được cập nhật\n" +
            $"• {skipped} asset bỏ qua\n\n" +
            "Xem Console để biết chi tiết từng item.",
            "OK");
    }

    // ── Report ───────────────────────────────────────────────────────────────

    [MenuItem(MENU_REPORT)]
    public static void ReportCurrentState()
    {
        string searchFolder = "Assets/_Game/Farm/data/Village_data";
        string[] guids = AssetDatabase.FindAssets("t:OrderItemDefinition", new[] { searchFolder });

        Debug.Log($"═══ Village Order State ({guids.Length} assets) ═══");
        foreach (string guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            var    asset = AssetDatabase.LoadAssetAtPath<OrderItemDefinition>(path);
            if (asset == null) continue;

            string warning = asset.unlockLevel >= 5 && !ContainsCookingKeyword(asset.name.ToLower()) &&
                             !asset.name.ToLower().StartsWith("order_item_")
                ? "" : (asset.unlockLevel >= 5 ? " [cooking]" : "");

            Debug.Log($"  {asset.name,-50} L{asset.unlockLevel,2}  {asset.goldPerUnit}g  {asset.expPerUnit}xp  [{asset.minAmount}-{asset.maxAmount}]{warning}");
        }
        Debug.Log("═══════════════════════════════════════════");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool ContainsCookingKeyword(string name) =>
        name.Contains("_xao_") || name.Contains("_nuong_") || name.Contains("_chien") ||
        name.Contains("_ham_") || name.Contains("_chua_") || name.Contains("pho_") ||
        name.Contains("_canh_") || name.Contains("trung_chien") || name.Contains("trung_op_") ||
        name.Contains("com_chien") || name.Contains("suon_") || name.Contains("nuoc_mia") ||
        name.Contains("salad_") || name.Contains("sup_") || name.Contains("_luoc_");

    private static int LookupDishGold(string assetNameLower)
    {
        foreach (var kv in DishGoldOverride)
            if (assetNameLower.Contains(kv.Key))
                return kv.Value;
        return 0;
    }

    [MenuItem(MENU_APPLY,  true)]
    [MenuItem(MENU_REPORT, true)]
    private static bool Validate() => !EditorApplication.isPlaying;
}
