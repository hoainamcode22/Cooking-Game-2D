using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tool đồng bộ dữ liệu giữa Farm, Warehouse và Kitchen.
///
/// Menu: Tools/Farm Data Sync/
///   1 - Preview All Gaps          → Xem trước toàn bộ vấn đề (không thay đổi gì)
///   2 - Fix: Warehouse item_taulua → Thêm item vật liệu tàu hỏa vào Kho
///   3 - Sync: CropData → Kitchen  → Tạo InventoryItemData còn thiếu + add vào CookingBoot
///   4 - Run All Fixes              → Chạy cả 2 và 3 cùng lúc
/// </summary>
public static class FarmDataSyncTool
{
    // ─── Đường dẫn cố định — chỉnh nếu bạn đổi vị trí scene/folder ──────────
    private const string FarmScenePath       = "Assets/_Game/Scenes/SCN_Farm.unity";
    private const string CookingScenePath    = "Assets/_Game/Scenes/SampleScene.unity";
    private const string ItemTauluaFolder    = "Assets/_Game/Farm/data/item_taulua";
    private const string CropDataFolder      = "Assets/_Game/Farm/data/Hat_giong";
    private const string KitchenItemFolder   = "Assets/_Game/Farm/data/Item_Kho_Cook";

    // ─────────────────────────────────────────────────────────────────────────
    //  MENU ENTRIES
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm Data Sync/1 - Preview All Gaps", priority = 1)]
    static void Preview() => RunPreview();

    [MenuItem("Tools/Farm Data Sync/2 - Fix: Warehouse item_taulua Icons", priority = 2)]
    static void FixWarehouse()
    {
        if (!ConfirmApply("Thêm toàn bộ InventoryItemData từ 'item_taulua' vào WarehousePopupUI.extraItemDatabase trong SCN_Farm."))
            return;
        FixWarehouseExtraItems();
    }

    [MenuItem("Tools/Farm Data Sync/3 - Sync: CropData → Kitchen Assets + CookingBoot", priority = 3)]
    static void SyncCropToKitchen()
    {
        if (!ConfirmApply("Tạo InventoryItemData còn thiếu trong Item_Kho_Cook và thêm vào CookingBoot trong SampleScene."))
            return;
        SyncCropDataToKitchen();
    }

    [MenuItem("Tools/Farm Data Sync/4 - Run All Fixes", priority = 4)]
    static void RunAll()
    {
        if (!ConfirmApply("Chạy toàn bộ: sửa Warehouse + đồng bộ Kitchen. Hãy chạy Preview trước để kiểm tra."))
            return;
        FixWarehouseExtraItems();
        SyncCropDataToKitchen();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREVIEW — DRY RUN
    // ─────────────────────────────────────────────────────────────────────────

    static void RunPreview()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[FarmDataSyncTool] ═══════ PREVIEW ═══════");

        // ── Part 1: item_taulua vs extraItemDatabase ──
        sb.AppendLine("\n── VẤN ĐỀ 1: Warehouse extraItemDatabase ──");

        List<InventoryItemData> tauluaItems = LoadAllInventoryItemDataFrom(ItemTauluaFolder);
        List<InventoryItemData> currentExtra = GetWarehouseExtraItemDatabase(out _, out _);

        var currentExtraIds = new HashSet<string>();
        foreach (var item in currentExtra)
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                currentExtraIds.Add(item.itemId);

        int missingFromWarehouse = 0;
        foreach (var item in tauluaItems)
        {
            if (item == null) continue;
            bool exists = currentExtraIds.Contains(item.itemId);
            string status = exists ? "[OK]" : "[THIẾU]";
            if (!exists) missingFromWarehouse++;
            sb.AppendLine($"  {status} itemId='{item.itemId}' ({item.displayName}) — {AssetDatabase.GetAssetPath(item)}");
        }
        sb.AppendLine($"  → Cần thêm vào extraItemDatabase: {missingFromWarehouse} item(s)");

        // ── Part 2: CropData vs Item_Kho_Cook ──
        sb.AppendLine("\n── VẤN ĐỀ 2: CropData → Kitchen mapping ──");

        List<CropData> crops = LoadAllCropData(CropDataFolder);
        Dictionary<string, InventoryItemData> kitchenLookup = BuildKitchenLookup(KitchenItemFolder);
        List<InventoryItemData> cookingBootItems = GetCookingBootItems(out _, out _);

        var cookingBootIds = new HashSet<string>();
        foreach (var item in cookingBootItems)
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                cookingBootIds.Add(item.itemId);

        int missingAssets  = 0;
        int missingInBoot  = 0;

        foreach (var crop in crops)
        {
            if (crop == null) continue;
            string hid = GetHarvestId(crop);
            if (string.IsNullOrEmpty(hid)) continue;

            bool hasAsset = kitchenLookup.ContainsKey(hid);
            bool inBoot   = cookingBootIds.Contains(hid);

            string assetStatus = hasAsset ? "[Asset OK]" : "[Asset THIẾU]";
            string bootStatus  = inBoot   ? "[Boot OK]"  : "[Boot THIẾU]";

            if (!hasAsset) missingAssets++;
            if (!inBoot)   missingInBoot++;

            sb.AppendLine($"  {assetStatus} {bootStatus} harvestId='{hid}' (từ {crop.itemName})");
        }
        sb.AppendLine($"  → Asset cần tạo mới: {missingAssets}");
        sb.AppendLine($"  → Cần thêm vào CookingBoot: {missingInBoot} item(s)");

        sb.AppendLine("\n═══════════════════════════════════════════");
        Debug.Log(sb.ToString());

        int totalIssues = missingFromWarehouse + missingAssets + missingInBoot;
        EditorUtility.DisplayDialog(
            "FarmDataSync — Preview",
            $"Tìm thấy {totalIssues} vấn đề cần sửa:\n\n" +
            $"• Warehouse thiếu icon: {missingFromWarehouse} item\n" +
            $"• Kitchen asset còn thiếu: {missingAssets}\n" +
            $"• CookingBoot chưa đăng ký: {missingInBoot} item\n\n" +
            "Xem Console để biết chi tiết từng item.\n" +
            "Chạy 'Run All Fixes' để tự động sửa tất cả.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FIX 1 — Thêm item_taulua vào WarehousePopupUI.extraItemDatabase
    // ─────────────────────────────────────────────────────────────────────────

    static void FixWarehouseExtraItems()
    {
        List<InventoryItemData> tauluaItems = LoadAllInventoryItemDataFrom(ItemTauluaFolder);
        if (tauluaItems.Count == 0)
        {
            Debug.LogWarning("[FarmDataSyncTool] Không tìm thấy InventoryItemData nào trong item_taulua.");
            return;
        }

        bool sceneWasLoaded = IsSceneLoaded(FarmScenePath);
        Scene scene = sceneWasLoaded
            ? GetLoadedScene(FarmScenePath)
            : EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Additive);

        if (!scene.IsValid())
        {
            Debug.LogError($"[FarmDataSyncTool] Không mở được scene: {FarmScenePath}");
            return;
        }

        WarehousePopupUI popup = FindComponentInScene<WarehousePopupUI>(scene);
        if (popup == null)
        {
            Debug.LogError("[FarmDataSyncTool] Không tìm thấy WarehousePopupUI trong SCN_Farm.");
            if (!sceneWasLoaded) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        SerializedObject so   = new SerializedObject(popup);
        SerializedProperty sp = so.FindProperty("extraItemDatabase");

        // Lấy tập ID đã có để không thêm trùng
        var existingIds = new HashSet<string>();
        for (int i = 0; i < sp.arraySize; i++)
        {
            var elem = sp.GetArrayElementAtIndex(i).objectReferenceValue as InventoryItemData;
            if (elem != null && !string.IsNullOrEmpty(elem.itemId))
                existingIds.Add(elem.itemId);
        }

        int added = 0;
        var report = new List<string>();

        foreach (var item in tauluaItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (existingIds.Contains(item.itemId))
            {
                report.Add($"  [OK đã có] {item.itemId} ({item.displayName})");
                continue;
            }

            sp.arraySize++;
            sp.GetArrayElementAtIndex(sp.arraySize - 1).objectReferenceValue = item;
            existingIds.Add(item.itemId);
            added++;
            report.Add($"  [THÊM MỚI] {item.itemId} ({item.displayName})");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(popup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!sceneWasLoaded)
            EditorSceneManager.CloseScene(scene, true);

        AssetDatabase.Refresh();

        string log = $"[FarmDataSyncTool] Warehouse fix xong — thêm {added} item:\n" +
                     string.Join("\n", report);
        Debug.Log(log);

        EditorUtility.DisplayDialog("Warehouse Fix — Xong",
            $"Đã thêm {added} InventoryItemData vào WarehousePopupUI.extraItemDatabase.\n" +
            "Mở SCN_Farm và kiểm tra Inspector của WarehousePopupUI để xác nhận.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FIX 2 — Tạo InventoryItemData còn thiếu + thêm vào CookingBoot
    // ─────────────────────────────────────────────────────────────────────────

    static void SyncCropDataToKitchen()
    {
        List<CropData> crops              = LoadAllCropData(CropDataFolder);
        Dictionary<string, InventoryItemData> kitchenLookup = BuildKitchenLookup(KitchenItemFolder);

        // Đảm bảo thư mục output tồn tại
        if (!AssetDatabase.IsValidFolder(KitchenItemFolder))
        {
            Debug.LogError($"[FarmDataSyncTool] Thư mục không tồn tại: {KitchenItemFolder}");
            return;
        }

        // Bước 1: Tạo asset còn thiếu
        var newlyCreated = new List<InventoryItemData>();
        var creationReport = new List<string>();

        foreach (var crop in crops)
        {
            if (crop == null) continue;
            string hid = GetHarvestId(crop);
            if (string.IsNullOrEmpty(hid)) continue;

            if (kitchenLookup.ContainsKey(hid))
            {
                creationReport.Add($"  [OK đã có] {hid} ({crop.itemName})");
                continue;
            }

            // Tạo mới InventoryItemData
            InventoryItemData newItem = ScriptableObject.CreateInstance<InventoryItemData>();
            newItem.itemId      = hid;
            newItem.displayName = crop.itemName;
            newItem.icon        = crop.itemIcon;
            // cookingData để null — gán thủ công sau nếu cần dùng trong công thức nấu

            string safeName  = SanitizeFileName(hid);
            string assetPath = $"{KitchenItemFolder}/Item_{safeName}.asset";

            // Tránh đè file đã tồn tại (khác harvestId)
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(newItem, assetPath);
            AssetDatabase.SaveAssets();

            kitchenLookup[hid] = newItem;
            newlyCreated.Add(newItem);
            creationReport.Add($"  [TẠO MỚI] {hid} ({crop.itemName}) → {assetPath}");
        }

        // Bước 2: Đăng ký vào CookingBoot
        int addedToBoot = RegisterInCookingBoot(crops, kitchenLookup);

        AssetDatabase.Refresh();

        string fullLog =
            $"[FarmDataSyncTool] Sync CropData → Kitchen xong.\n" +
            $"Assets tạo mới: {newlyCreated.Count} | Thêm vào CookingBoot: {addedToBoot}\n\n" +
            string.Join("\n", creationReport);
        Debug.Log(fullLog);

        string note = newlyCreated.Count > 0
            ? "\n\nLưu ý: Các asset mới có cookingData = null.\n" +
              "Nếu nguyên liệu này cần dùng trong công thức nấu, hãy gán cookingData thủ công trong Inspector."
            : "";

        EditorUtility.DisplayDialog("Kitchen Sync — Xong",
            $"Đã tạo {newlyCreated.Count} InventoryItemData mới.\n" +
            $"Đã thêm {addedToBoot} item vào CookingBoot.cookingInventoryItems." +
            note, "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPER: Đăng ký assets vào CookingBoot.cookingInventoryItems
    // ─────────────────────────────────────────────────────────────────────────

    static int RegisterInCookingBoot(
        List<CropData> crops,
        Dictionary<string, InventoryItemData> kitchenLookup)
    {
        bool sceneWasLoaded = IsSceneLoaded(CookingScenePath);
        Scene scene = sceneWasLoaded
            ? GetLoadedScene(CookingScenePath)
            : EditorSceneManager.OpenScene(CookingScenePath, OpenSceneMode.Additive);

        if (!scene.IsValid())
        {
            Debug.LogError($"[FarmDataSyncTool] Không mở được scene: {CookingScenePath}");
            return 0;
        }

        CookingBoot boot = FindComponentInScene<CookingBoot>(scene);
        if (boot == null)
        {
            Debug.LogError("[FarmDataSyncTool] Không tìm thấy CookingBoot trong SampleScene.");
            if (!sceneWasLoaded) EditorSceneManager.CloseScene(scene, true);
            return 0;
        }

        SerializedObject so   = new SerializedObject(boot);
        SerializedProperty sp = so.FindProperty("cookingInventoryItems");

        // Tập hợp ID đã đăng ký để tránh trùng
        var registeredIds = new HashSet<string>();
        for (int i = 0; i < sp.arraySize; i++)
        {
            var elem = sp.GetArrayElementAtIndex(i).objectReferenceValue as InventoryItemData;
            if (elem != null && !string.IsNullOrEmpty(elem.itemId))
                registeredIds.Add(elem.itemId);
        }

        int added = 0;

        foreach (var crop in crops)
        {
            if (crop == null) continue;
            string hid = GetHarvestId(crop);
            if (string.IsNullOrEmpty(hid)) continue;
            if (registeredIds.Contains(hid)) continue;

            if (!kitchenLookup.TryGetValue(hid, out InventoryItemData item) || item == null)
            {
                Debug.LogWarning($"[FarmDataSyncTool] Không tìm thấy asset cho harvestId='{hid}', bỏ qua.");
                continue;
            }

            sp.arraySize++;
            sp.GetArrayElementAtIndex(sp.arraySize - 1).objectReferenceValue = item;
            registeredIds.Add(hid);
            added++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(boot);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!sceneWasLoaded)
            EditorSceneManager.CloseScene(scene, true);

        return added;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UTILITIES
    // ─────────────────────────────────────────────────────────────────────────

    /// Tải tất cả InventoryItemData trong một folder (không đệ quy vào sub-folder)
    static List<InventoryItemData> LoadAllInventoryItemDataFrom(string folder)
    {
        var result = new List<InventoryItemData>();
        string[] guids = AssetDatabase.FindAssets("t:InventoryItemData", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Chỉ lấy file trực tiếp trong folder này, không đệ quy
            if (Path.GetDirectoryName(path)?.Replace('\\', '/') != folder.Replace('\\', '/'))
                continue;
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (item != null) result.Add(item);
        }
        return result;
    }

    /// Tải tất cả CropData trong folder
    static List<CropData> LoadAllCropData(string folder)
    {
        var result = new List<CropData>();
        string[] guids = AssetDatabase.FindAssets("t:CropData", new[] { folder });
        foreach (string guid in guids)
        {
            var crop = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid));
            if (crop != null) result.Add(crop);
        }
        return result;
    }

    /// Xây dictionary itemId → InventoryItemData cho toàn bộ Item_Kho_Cook
    static Dictionary<string, InventoryItemData> BuildKitchenLookup(string folder)
    {
        var dict  = new Dictionary<string, InventoryItemData>();
        string[] guids = AssetDatabase.FindAssets("t:InventoryItemData", new[] { folder });
        foreach (string guid in guids)
        {
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (!dict.ContainsKey(item.itemId))
                dict.Add(item.itemId, item);
        }
        return dict;
    }

    /// Lấy List<InventoryItemData> hiện tại của WarehousePopupUI (dry-run, không modify)
    static List<InventoryItemData> GetWarehouseExtraItemDatabase(out Scene outScene, out bool wasLoaded)
    {
        var result = new List<InventoryItemData>();
        wasLoaded = IsSceneLoaded(FarmScenePath);
        outScene  = wasLoaded
            ? GetLoadedScene(FarmScenePath)
            : EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Additive);

        if (!outScene.IsValid()) return result;

        var popup = FindComponentInScene<WarehousePopupUI>(outScene);
        if (popup == null) return result;

        SerializedObject so = new SerializedObject(popup);
        SerializedProperty sp = so.FindProperty("extraItemDatabase");
        for (int i = 0; i < sp.arraySize; i++)
        {
            var item = sp.GetArrayElementAtIndex(i).objectReferenceValue as InventoryItemData;
            result.Add(item);
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(outScene, true);
        return result;
    }

    /// Lấy List<InventoryItemData> hiện tại của CookingBoot (dry-run)
    static List<InventoryItemData> GetCookingBootItems(out Scene outScene, out bool wasLoaded)
    {
        var result = new List<InventoryItemData>();
        wasLoaded = IsSceneLoaded(CookingScenePath);
        outScene  = wasLoaded
            ? GetLoadedScene(CookingScenePath)
            : EditorSceneManager.OpenScene(CookingScenePath, OpenSceneMode.Additive);

        if (!outScene.IsValid()) return result;

        var boot = FindComponentInScene<CookingBoot>(outScene);
        if (boot == null) return result;

        SerializedObject so = new SerializedObject(boot);
        SerializedProperty sp = so.FindProperty("cookingInventoryItems");
        for (int i = 0; i < sp.arraySize; i++)
        {
            var item = sp.GetArrayElementAtIndex(i).objectReferenceValue as InventoryItemData;
            result.Add(item);
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(outScene, true);
        return result;
    }

    /// harvestItemId nếu có, fallback về cropId
    static string GetHarvestId(CropData crop)
    {
        if (!string.IsNullOrEmpty(crop.harvestItemId)) return crop.harvestItemId.Trim().ToLower();
        if (!string.IsNullOrEmpty(crop.cropId))        return crop.cropId.Trim().ToLower();
        return "";
    }

    static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            T comp = root.GetComponentInChildren<T>(includeInactive: true);
            if (comp != null) return comp;
        }
        return null;
    }

    static bool IsSceneLoaded(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).path == scenePath)
                return true;
        return false;
    }

    static Scene GetLoadedScene(string scenePath)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.path == scenePath) return s;
        }
        return default;
    }

    static string SanitizeFileName(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');
        return input;
    }

    static bool ConfirmApply(string detail)
    {
        return EditorUtility.DisplayDialog(
            "FarmDataSync — Xác nhận",
            detail + "\n\nHành động này sẽ ghi trực tiếp vào scene và tạo asset.\n" +
            "Hãy chạy Preview trước nếu chưa kiểm tra.\nTiếp tục?",
            "Đồng ý", "Hủy");
    }
}
