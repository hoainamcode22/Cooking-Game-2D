using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Tools > Farm Data Sync > 5 - Auto Create & Link Cooking Data
///
/// Làm 3 việc tự động, không sờ vào logic gameplay:
///   1. Gán SEA_Sugar làm cookingData của Item_sugarcane
///   2. Tạo ING_Cabbage.asset và ING_Corn.asset (nếu chưa có)
///   3. Gán chúng vào cookingData của Item_bapcai và Item_ngo
/// </summary>
public static class CookingDataLinker
{
    private const string IngDataFolder    = "Assets/_Game/Data/Data_cooking";
    private const string KitchenItemFolder = "Assets/_Game/Farm/data/Item_Kho_Cook";

    [MenuItem("Tools/Farm Data Sync/5 - Auto Create & Link Cooking Data", priority = 5)]
    static void Run()
    {
        bool ok = true;

        // ── 1. Liên kết Mía ──────────────────────────────────────────────────
        ok &= LinkExisting(
            inventoryAssetName : "Item_sugarcane",
            ingredientAssetName: "SEA_Sugar",
            searchFolder       : IngDataFolder);

        // ── 2 & 3. Tạo + liên kết Bắp Cải ──────────────────────────────────
        IngredientData cabbage = GetOrCreateIngredientData(
            fileName   : "ING_Cabbage",
            id         : "bapcai",
            displayName: "Bắp Cải",
            kind       : IngredientKind.Ingredient,
            tier       : IngredientTier.Basic);

        if (cabbage != null)
            ok &= LinkTo("Item_bapcai", cabbage);

        // ── 2 & 3. Tạo + liên kết Ngô ───────────────────────────────────────
        IngredientData corn = GetOrCreateIngredientData(
            fileName   : "ING_Corn",
            id         : "ngo",
            displayName: "Ngô",
            kind       : IngredientKind.Ingredient,
            tier       : IngredientTier.Basic);

        if (corn != null)
            ok &= LinkTo("Item_ngo", corn);

        // ── Kết thúc ─────────────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (ok)
        {
            Debug.Log("[CookingDataLinker] Đã tạo và liên kết thành công!");
            EditorUtility.DisplayDialog("Hoàn tất", "Đã tạo và liên kết thành công!\nXem Console để kiểm tra chi tiết.", "OK");
        }
        else
        {
            Debug.LogWarning("[CookingDataLinker] Hoàn tất nhưng có một số bước bị bỏ qua. Xem Console để biết chi tiết.");
            EditorUtility.DisplayDialog("Hoàn tất (có cảnh báo)", "Một số bước bị bỏ qua.\nXem Console để biết chi tiết.", "OK");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// Tìm IngredientData theo tên file trong folder chỉ định, gán vào cookingData của InventoryItemData.
    static bool LinkExisting(string inventoryAssetName, string ingredientAssetName, string searchFolder)
    {
        IngredientData ing = FindIngredientData(ingredientAssetName, searchFolder);
        if (ing == null)
        {
            Debug.LogWarning($"[CookingDataLinker] Không tìm thấy IngredientData '{ingredientAssetName}' trong '{searchFolder}'.");
            return false;
        }
        return LinkTo(inventoryAssetName, ing);
    }

    /// Gán ingredientData vào trường cookingData của InventoryItemData có tên asset = inventoryAssetName.
    static bool LinkTo(string inventoryAssetName, IngredientData ingredientData)
    {
        InventoryItemData item = FindInventoryItemData(inventoryAssetName);
        if (item == null)
        {
            Debug.LogWarning($"[CookingDataLinker] Không tìm thấy InventoryItemData '{inventoryAssetName}'.");
            return false;
        }

        if (item.cookingData == ingredientData)
        {
            Debug.Log($"[CookingDataLinker] [{inventoryAssetName}] cookingData đã đúng — bỏ qua.");
            return true;
        }

        item.cookingData = ingredientData;
        EditorUtility.SetDirty(item);
        Debug.Log($"[CookingDataLinker] [{inventoryAssetName}] cookingData → '{ingredientData.id}' ({ingredientData.displayName}) ✓");
        return true;
    }

    /// Tạo mới IngredientData nếu chưa tồn tại; trả về asset (mới hoặc cũ).
    static IngredientData GetOrCreateIngredientData(
        string fileName,
        string id,
        string displayName,
        IngredientKind kind,
        IngredientTier tier)
    {
        string path = $"{IngDataFolder}/{fileName}.asset";

        // Nếu đã tồn tại: trả về luôn, không ghi đè để giữ nguyên Flavor Vector
        IngredientData existing = AssetDatabase.LoadAssetAtPath<IngredientData>(path);
        if (existing != null)
        {
            Debug.Log($"[CookingDataLinker] '{fileName}.asset' đã tồn tại — giữ nguyên, không ghi đè.");
            return existing;
        }

        // Tạo mới
        IngredientData data = ScriptableObject.CreateInstance<IngredientData>();
        data.id          = id;
        data.displayName = displayName;
        data.kind        = kind;
        data.tier        = tier;
        data.stars       = 3;
        // vector và icon để mặc định (all-zero) — điền trong Inspector sau

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[CookingDataLinker] Đã tạo '{fileName}.asset' (id='{id}', kind={kind}) tại {path}");
        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Asset lookup helpers
    // ─────────────────────────────────────────────────────────────────────────

    static InventoryItemData FindInventoryItemData(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:InventoryItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // So khớp chính xác tên file (không lấy nhầm Item_bapcai2, v.v.)
            if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                return AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
        }
        return null;
    }

    static IngredientData FindIngredientData(string assetName, string folder)
    {
        string[] guids = AssetDatabase.FindAssets($"{assetName} t:IngredientData", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                return AssetDatabase.LoadAssetAtPath<IngredientData>(path);
        }
        return null;
    }
}
