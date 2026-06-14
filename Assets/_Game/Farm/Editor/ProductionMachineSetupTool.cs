using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sprint 6 — Máy chế biến L11-L15 (tái dụng cơ chế chuồng làm "máy sản xuất").
/// Menu: Tools → Farm Game → Setup Production Machines L11-L15
///
/// Vì PenMiniPanelUI.config được override BÊN TRONG prefab chuồng (nested PF_PenMiniPanel),
/// không thể dùng chung 1 prefab cho nhiều config → tool này:
///   1. Copy Pen_04.prefab → Frefab_home/May_01..03.prefab (giữ nguyên copy nếu đã tồn tại
///      để không mất reskin của artist — chỉ re-apply config/feedItemId).
///   2. Gắn PenMiniPanelConfig máy tương ứng (Config_May01_XayBot...) vào bản copy.
///   3. Đổi feedItemId của các slot kéo-thả sang nguyên liệu máy; tắt slot 2 (máy 1 nguyên liệu).
///   4. Tạo/cập nhật BuildingData shop: Máy Xay Bột (120/L11), Máy Ép Mía (121/L13),
///      Máy Phô Mai (122/L15) trỏ vào prefab mới.
///   5. Nếu scene đang mở có ShopManager/WarehousePopupUI: tự đăng ký vào
///      buildingList / extraItemDatabase (nhớ Save scene sau khi chạy).
/// Idempotent — chạy lại bao nhiêu lần cũng không tạo trùng.
/// </summary>
public static class ProductionMachineSetupTool
{
    private const string SourcePenPath   = "Assets/_Game/Farm/CÔNG TRÌNH/Pen_04.prefab";
    private const string PrefabFolder    = "Assets/_Game/Farm/Frefab_home";
    private const string BuildingFolder  = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding";
    private const string DataFolder      = "Assets/_Game/Farm/data/Farm_May_Che_Bien";

    private struct MachineDef
    {
        public string prefabName;        // May_01
        public string configAssetName;   // Config_May01_XayBot
        public string buildingAssetName; // Máy Xay Bột (tên file .asset + itemName)
        public string itemID;            // "120"
        public int    goldPrice;
        public int    unlockLevel;
        public string inputItemId;       // rice / sugarcane / milk
        public float  duration;          // giây — mirror config, ghi vào DraggableFeedItem
        public string itemAssetName;     // Item_BotGao — để sync kho
    }

    private static readonly MachineDef[] Machines =
    {
        new MachineDef { prefabName = "May_01", configAssetName = "Config_May01_XayBot",
            buildingAssetName = "Máy Xay Bột", itemID = "120", goldPrice = 2500, unlockLevel = 11,
            inputItemId = "rice", duration = 60f, itemAssetName = "Item_BotGao" },
        new MachineDef { prefabName = "May_02", configAssetName = "Config_May02_EpMia",
            buildingAssetName = "Máy Ép Mía", itemID = "121", goldPrice = 3000, unlockLevel = 13,
            inputItemId = "sugarcane", duration = 90f, itemAssetName = "Item_NuocMiaEp" },
        new MachineDef { prefabName = "May_03", configAssetName = "Config_May03_PhoMai",
            buildingAssetName = "Máy Phô Mai", itemID = "122", goldPrice = 3500, unlockLevel = 15,
            inputItemId = "milk", duration = 120f, itemAssetName = "Item_PhoMai" },
    };

    [MenuItem("Tools/Farm Game/Setup Production Machines L11-L15", false, 30)]
    public static void Setup()
    {
        var log = new StringBuilder("===== SETUP PRODUCTION MACHINES L11-L15 =====\n");
        var buildings = new BuildingData[Machines.Length];
        bool anyError = false;

        for (int i = 0; i < Machines.Length; i++)
        {
            MachineDef def = Machines[i];

            string configPath = $"{DataFolder}/{def.configAssetName}.asset";
            var config = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(configPath);
            if (config == null)
            {
                log.AppendLine($"  ✘ THIẾU config: {configPath} — bỏ qua {def.buildingAssetName}");
                anyError = true;
                continue;
            }

            GameObject prefab = CreateOrUpdateMachinePrefab(def, config, log);
            if (prefab == null) { anyError = true; continue; }

            buildings[i] = CreateOrUpdateBuildingData(def, prefab, log);
        }

        bool sceneDirty = false;
        sceneDirty |= SyncShopBuildingList(buildings, log);
        sceneDirty |= SyncWarehouseExtraItems(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine("-----------------------------------");
        log.AppendLine("Kinh tế chuỗi chế biến (1 nguyên liệu/lần — cơ chế chuồng):");
        log.AppendLine("  • Lúa (7g)      → Máy Xay Bột 60s  → 2 Bột gạo     (đơn 70g/cái,  +12 EXP)");
        log.AppendLine("  • Mía (36g)     → Máy Ép Mía 90s   → 2 Nước mía ép (đơn 95g/cái,  +14 EXP)");
        log.AppendLine("  • Sữa (chuồng)  → Máy Phô Mai 120s → 2 Phô mai     (đơn 130g/cái, +16 EXP)");
        if (sceneDirty)
            log.AppendLine("⚠ Scene đã thay đổi (Shop/Kho) — nhớ SAVE SCENE (Ctrl+S).");
        log.AppendLine("Icon sản phẩm/máy đang để trống — artist gắn sau vào Item_*.asset, Config_May*.asset và BuildingData.");

        if (anyError) Debug.LogError(log.ToString());
        else Debug.Log(log.ToString());
    }

    // ── Prefab: copy Pen_04 → May_0X, gắn config máy ─────────────────────────
    private static GameObject CreateOrUpdateMachinePrefab(MachineDef def, PenMiniPanelConfig config, StringBuilder log)
    {
        string targetPath = $"{PrefabFolder}/{def.prefabName}.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null;

        // Đã tồn tại → sửa tại chỗ (giữ guid + reskin); chưa có → copy từ Pen_04
        string loadPath = exists ? targetPath : SourcePenPath;
        if (!exists && AssetDatabase.LoadAssetAtPath<GameObject>(SourcePenPath) == null)
        {
            log.AppendLine($"  ✘ KHÔNG thấy prefab nguồn {SourcePenPath} — bỏ qua {def.prefabName}");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(loadPath);
        try
        {
            root.name = def.prefabName;

            var ui = root.GetComponentInChildren<PenMiniPanelUI>(true);
            if (ui == null)
            {
                log.AppendLine($"  ✘ {loadPath} không có PenMiniPanelUI — bỏ qua {def.prefabName}");
                return null;
            }

            var so = new SerializedObject(ui);
            so.FindProperty("config").objectReferenceValue = config;

            // Máy chỉ nhận 1 nguyên liệu → tắt slot thức ăn 2 và gỡ tham chiếu
            // (RefreshUI bật lại slot2Root mỗi lần Idle nếu còn tham chiếu).
            var slot2Prop = so.FindProperty("slot2Root");
            var slot2Go = slot2Prop != null ? slot2Prop.objectReferenceValue as GameObject : null;
            if (slot2Go != null)
            {
                slot2Go.SetActive(false);
                slot2Prop.objectReferenceValue = null;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Mọi slot kéo-thả (kể cả slot 2 đã tắt — phòng khi bật lại) nhận nguyên liệu máy
            foreach (var feed in root.GetComponentsInChildren<DraggableFeedItem>(true))
            {
                feed.feedItemId = def.inputItemId;
                feed.feedDuration = def.duration;
            }

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        log.AppendLine(exists
            ? $"  ✔ cập nhật prefab {targetPath} (config {def.configAssetName}, input '{def.inputItemId}')"
            : $"  ✔ tạo prefab {targetPath} từ Pen_04 (config {def.configAssetName}, input '{def.inputItemId}')");
        return AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
    }

    // ── BuildingData shop asset ───────────────────────────────────────────────
    private static BuildingData CreateOrUpdateBuildingData(MachineDef def, GameObject prefab, StringBuilder log)
    {
        string path = $"{BuildingFolder}/{def.buildingAssetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
        bool created = false;

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BuildingData>();
            AssetDatabase.CreateAsset(data, path);
            created = true;
        }

        data.itemID        = def.itemID;
        data.itemName      = def.buildingAssetName;
        data.goldPrice     = def.goldPrice;
        data.diamondPrice  = 0;
        data.unlockLevel   = def.unlockLevel;
        data.prefabToBuild = prefab;
        // itemIcon: giữ nguyên nếu artist đã gắn — không ghi đè về null
        EditorUtility.SetDirty(data);

        log.AppendLine($"  ✔ {(created ? "tạo" : "cập nhật")} BuildingData '{def.buildingAssetName}' " +
                       $"(itemID {def.itemID}, {def.goldPrice} vàng, mở L{def.unlockLevel})");
        return data;
    }

    // ── Đăng ký vào ShopManager.buildingList (scene) ──────────────────────────
    private static bool SyncShopBuildingList(BuildingData[] buildings, StringBuilder log)
    {
        var shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            log.AppendLine("  ⚠ ShopManager không có trong scene đang mở — mở SCN_Farm rồi chạy lại để máy hiện trong Shop.");
            return false;
        }

        bool changed = false;
        foreach (var b in buildings)
        {
            if (b == null) continue;
            if (shop.buildingList == null)
            {
                log.AppendLine("  ⚠ ShopManager.buildingList null — kiểm tra tay trong Inspector.");
                return false;
            }
            if (!shop.buildingList.Contains(b))
            {
                shop.buildingList.Add(b);
                changed = true;
                log.AppendLine($"  ✔ thêm '{b.itemName}' vào ShopManager.buildingList");
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(shop);
            EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        }
        else log.AppendLine("  ✔ ShopManager.buildingList đã đủ 3 máy");
        return changed;
    }

    // ── Đăng ký item mới vào WarehousePopupUI.extraItemDatabase (scene) ───────
    private static bool SyncWarehouseExtraItems(StringBuilder log)
    {
        var warehouse = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (warehouse == null)
        {
            log.AppendLine("  ⚠ WarehousePopupUI không có trong scene đang mở — kho sẽ hiện itemId thô thay vì tên/icon.");
            return false;
        }

        var so = new SerializedObject(warehouse);
        var listProp = so.FindProperty("extraItemDatabase");
        if (listProp == null)
        {
            log.AppendLine("  ⚠ Không tìm thấy field extraItemDatabase trên WarehousePopupUI.");
            return false;
        }

        bool changed = false;
        foreach (MachineDef def in Machines)
        {
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>($"{DataFolder}/{def.itemAssetName}.asset");
            if (item == null)
            {
                log.AppendLine($"  ⚠ thiếu {DataFolder}/{def.itemAssetName}.asset — bỏ qua sync kho.");
                continue;
            }

            bool found = false;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == item) { found = true; break; }
            }
            if (!found)
            {
                listProp.arraySize++;
                listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = item;
                changed = true;
                log.AppendLine($"  ✔ thêm '{item.displayName}' vào WarehousePopupUI.extraItemDatabase");
            }
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(warehouse);
            EditorSceneManager.MarkSceneDirty(warehouse.gameObject.scene);
        }
        else log.AppendLine("  ✔ WarehousePopupUI.extraItemDatabase đã đủ 3 item chế biến");
        return changed;
    }
}
