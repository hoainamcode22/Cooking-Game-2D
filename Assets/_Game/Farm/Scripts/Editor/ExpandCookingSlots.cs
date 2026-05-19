using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Farm Data Sync > 6 - Expand Cooking Slots (Auto-Add Missing Ingredients)
///
/// Vấn đề: LeftPanelSpawner.ingredients và .seasonings là danh sách cấu hình tay
/// trong Inspector. Mỗi khi có IngredientData mới (ING_Cabbage, ING_Corn...) cần
/// tự thêm vào đây để slot xuất hiện trong giao diện Bếp.
///
/// Tool này quét toàn bộ IngredientData trong Data_cooking, phân loại theo kind,
/// rồi tự append phần còn thiếu vào LeftPanelSpawner (trong SampleScene).
/// Không xóa dữ liệu cũ, không chạm vào gameplay logic.
/// </summary>
public static class ExpandCookingSlots
{
    private const string CookingScenePath  = "Assets/_Game/Scenes/SampleScene.unity";
    private const string IngDataFolder     = "Assets/_Game/Data/Data_cooking";
    private const string KitchenItemFolder = "Assets/_Game/Farm/data/Item_Kho_Cook";

    // ─── Menu entries ────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm Data Sync/6 - Preview: Cooking Slot Gaps", priority = 6)]
    static void Preview() => Run(dryRun: true);

    [MenuItem("Tools/Farm Data Sync/7 - Apply: Expand Cooking Slots to All Ingredients", priority = 7)]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Expand Cooking Slots",
            "Tool sẽ tự động thêm các IngredientData còn thiếu vào " +
            "LeftPanelSpawner.ingredients/seasonings trong SampleScene.\n\n" +
            "Dữ liệu cũ được giữ nguyên, chỉ append thêm.\n" +
            "Tiếp tục?", "Đồng ý", "Hủy"))
            return;

        Run(dryRun: false);
    }

    // ─── Core ────────────────────────────────────────────────────────────────

    static void Run(bool dryRun)
    {
        // 1. Thu thập tất cả IngredientData trong Data_cooking
        var allIngData  = new List<IngredientData>();
        string[] guids = AssetDatabase.FindAssets("t:IngredientData", new[] { IngDataFolder });
        foreach (string g in guids)
        {
            var d = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null) allIngData.Add(d);
        }

        // 2. Phân loại theo kind
        var allIngredients = new List<IngredientData>();
        var allSeasonings  = new List<IngredientData>();
        foreach (var d in allIngData)
        {
            if (d.kind == IngredientKind.Seasoning) allSeasonings.Add(d);
            else                                     allIngredients.Add(d);
        }

        // 3. Mở SampleScene
        bool wasLoaded = IsSceneLoaded(CookingScenePath);
        Scene scene    = wasLoaded
            ? GetLoadedScene(CookingScenePath)
            : EditorSceneManager.OpenScene(CookingScenePath, OpenSceneMode.Additive);

        if (!scene.IsValid())
        {
            Debug.LogError("[ExpandCookingSlots] Không mở được SampleScene.");
            return;
        }

        LeftPanelSpawner spawner = FindInScene<LeftPanelSpawner>(scene);
        if (spawner == null)
        {
            Debug.LogError("[ExpandCookingSlots] Không tìm thấy LeftPanelSpawner trong SampleScene.");
            if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        // 4. Xử lý ingredients list
        var so = new SerializedObject(spawner);
        so.Update();

        int addedIng = ProcessList(so, "ingredients", allIngredients, dryRun, "Ingredient");
        int addedSea = ProcessList(so, "seasonings",  allSeasonings,  dryRun, "Seasoning");

        // 5. Lưu
        if (!dryRun && (addedIng + addedSea) > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);

        // 6. Cũng cập nhật CookingBoot.cookingInventoryItems
        int addedBoot = 0;
        if (!dryRun)
            addedBoot = SyncCookingBoot(scene, wasLoaded);

        // 7. Report
        PrintReport(dryRun, addedIng, addedSea, addedBoot,
                    allIngredients.Count, allSeasonings.Count);
    }

    // ─── Xử lý từng list (ingredients hoặc seasonings) ──────────────────────

    static int ProcessList(
        SerializedObject so,
        string listPropName,
        List<IngredientData> candidates,
        bool dryRun,
        string label)
    {
        SerializedProperty listProp = so.FindProperty(listPropName);

        // Lấy set GUID đã có để kiểm tra trùng
        var existingGuids = new HashSet<string>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i)
                               .FindPropertyRelative("ingredientData")
                               .objectReferenceValue;
            if (elem != null)
                existingGuids.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(elem)));
        }

        int added = 0;
        foreach (var ing in candidates)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(ing));
            if (existingGuids.Contains(guid)) continue;

            if (dryRun)
            {
                Debug.Log($"[ExpandCookingSlots] [{label}] SẼ THÊM: id='{ing.id}' ({ing.displayName})");
            }
            else
            {
                int idx = listProp.arraySize;
                listProp.arraySize = idx + 1;

                SerializedProperty newElem = listProp.GetArrayElementAtIndex(idx);
                newElem.FindPropertyRelative("ingredientData").objectReferenceValue = ing;
                newElem.FindPropertyRelative("itemName").stringValue = ing.displayName;

                // icon: lấy từ IngredientData.icon
                newElem.FindPropertyRelative("mainIcon").objectReferenceValue = ing.icon;

                existingGuids.Add(guid);
                Debug.Log($"[ExpandCookingSlots] [{label}] THÊM: id='{ing.id}' ({ing.displayName})");
            }
            added++;
        }

        if (added == 0 && !dryRun)
            Debug.Log($"[ExpandCookingSlots] [{label}] Tất cả đã có đủ — không cần thêm.");

        return added;
    }

    // ─── Sync CookingBoot.cookingInventoryItems ──────────────────────────────

    static int SyncCookingBoot(Scene cookingScene, bool cookingWasLoaded)
    {
        // Mở lại scene nếu đã bị đóng
        bool open = IsSceneLoaded(CookingScenePath);
        Scene scene = open
            ? GetLoadedScene(CookingScenePath)
            : EditorSceneManager.OpenScene(CookingScenePath, OpenSceneMode.Additive);

        CookingBoot boot = FindInScene<CookingBoot>(scene);
        if (boot == null)
        {
            if (!open) EditorSceneManager.CloseScene(scene, true);
            return 0;
        }

        // Lấy tất cả InventoryItemData có cookingData != null
        var so  = new SerializedObject(boot);
        var sp  = so.FindProperty("cookingInventoryItems");
        so.Update();

        var existing = new HashSet<string>();
        for (int i = 0; i < sp.arraySize; i++)
        {
            var elem = sp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (elem != null)
                existing.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(elem)));
        }

        // Tất cả InventoryItemData trong Item_Kho_Cook có cookingData
        string[] guids = AssetDatabase.FindAssets("t:InventoryItemData", new[] { KitchenItemFolder });
        int added = 0;
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (item == null || item.cookingData == null) continue;
            if (existing.Contains(g)) continue;

            int idx = sp.arraySize;
            sp.arraySize = idx + 1;
            sp.GetArrayElementAtIndex(idx).objectReferenceValue = item;
            existing.Add(g);
            added++;
            Debug.Log($"[ExpandCookingSlots] [CookingBoot] THÊM: itemId='{item.itemId}' ({item.displayName})");
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (!open) EditorSceneManager.CloseScene(scene, true);
        return added;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            T c = root.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    static bool IsSceneLoaded(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).path == path) return true;
        return false;
    }

    static Scene GetLoadedScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.path == path) return s;
        }
        return default;
    }

    static void PrintReport(bool dryRun, int addedIng, int addedSea, int addedBoot,
                            int totalIng, int totalSea)
    {
        string mode = dryRun ? "PREVIEW" : "ĐÃ ÁP DỤNG";
        string body = $"[ExpandCookingSlots] ── {mode} ──\n" +
                      $"  IngredientData Ingredient trong Data_cooking: {totalIng}\n" +
                      $"  IngredientData Seasoning   trong Data_cooking: {totalSea}\n" +
                      $"  {(dryRun ? "Sẽ thêm" : "Đã thêm")} vào LeftPanelSpawner.ingredients: {addedIng}\n" +
                      $"  {(dryRun ? "Sẽ thêm" : "Đã thêm")} vào LeftPanelSpawner.seasonings : {addedSea}\n" +
                      $"  {(dryRun ? "—" : "Đã thêm")} vào CookingBoot.cookingInventoryItems: {addedBoot}";
        Debug.Log(body);

        string dialogMsg = dryRun
            ? $"Preview xong:\n• Sẽ thêm {addedIng} ingredient slot\n• Sẽ thêm {addedSea} seasoning slot\n\n" +
              "Chạy 'Apply' để áp dụng.\nXem Console để biết tên từng item."
            : $"Hoàn tất:\n• Đã thêm {addedIng} ingredient slot\n• Đã thêm {addedSea} seasoning slot\n" +
              $"• Đã thêm {addedBoot} item vào CookingBoot\n\nXem Console để biết chi tiết.";

        EditorUtility.DisplayDialog(dryRun ? "Preview" : "Expand Cooking Slots — Xong", dialogMsg, "OK");
    }
}
