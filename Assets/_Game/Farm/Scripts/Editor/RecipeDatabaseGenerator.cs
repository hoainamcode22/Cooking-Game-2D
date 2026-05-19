using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Farm Data Sync > 8 - Generate Recipes & Dish Database
///
/// Làm 3 việc:
///   1. Tạo ING_Fish.asset nếu chưa có (cần cho Canh Chua Cá, Cá Nướng Tiêu)
///   2. Gán requiredIngredients + tính targetFlavor (tổng vector nguyên liệu) cho 20 DishData
///   3. Đồng bộ All_Data.asset (ListDishData) — thêm dish còn thiếu, không xóa cái cũ
///
/// Không đụng đến logic gameplay, không thay đổi icon/sprite/dishName đã có.
/// </summary>
public static class RecipeDatabaseGenerator
{
    private const string DishFolder    = "Assets/_Game/Farm/data/Farm_Cooking";
    private const string IngFolderA    = "Assets/_Game/Data/Data_cooking";
    private const string IngFolderB    = "Assets/_Game/ScriptableObjects/Ingredients";
    private const string ListDishAsset = "Assets/_Game/Farm/data/Farm_Cooking/All_Data.asset";

    // ─── Menu ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm Data Sync/8 - Preview: Recipe Gaps", priority = 8)]
    static void Preview() => Run(dryRun: true);

    [MenuItem("Tools/Farm Data Sync/9 - Apply: Generate Recipes & Dish Database", priority = 9)]
    static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Generate Recipes",
            "Tool sẽ:\n" +
            "• Tạo ING_Fish.asset nếu chưa có\n" +
            "• Gán requiredIngredients + tính targetFlavor cho 20 món ăn\n" +
            "• Đồng bộ All_Data.asset\n\n" +
            "Dữ liệu dishSprite / dishName / dishId được giữ nguyên.\nTiếp tục?",
            "Đồng ý", "Hủy"))
            return;

        Run(dryRun: false);
    }

    // ─── Recipe Table ────────────────────────────────────────────────────────

    // Mỗi entry: dishId → (DishDifficulty, ingredient ids...)
    // id của IngredientData như đã xác nhận từ .asset files
    private static readonly RecipeEntry[] Recipes = new[]
    {
        new RecipeEntry("bap_cai_xao_nam",          DishDifficulty.Normal, "bapcai",  "mushroom", "fishsauce"),
        new RecipeEntry("bo_ham_ca_rot",             DishDifficulty.Hard,   "beef",    "carot",    "pepper",   "salt"),
        new RecipeEntry("bo_xao_tieu",               DishDifficulty.Normal, "beef",    "pepper",   "soysauce"),
        new RecipeEntry("ca_nuong_tieu",             DishDifficulty.Hard,   "ca",      "pepper",   "salt"),
        new RecipeEntry("canh_chua_ca",              DishDifficulty.Hard,   "ca",      "cachua",   "lemon",    "fishsauce"),
        new RecipeEntry("canh_khoai_tay_thit_heo",  DishDifficulty.Hard,   "pork",    "khoaitay", "salt"),
        new RecipeEntry("com_chien_trung",           DishDifficulty.Easy,   "rice",    "egg",      "soysauce"),
        new RecipeEntry("ga_nuong_lu",               DishDifficulty.Normal, "chicken", "pepper",   "salt"),
        new RecipeEntry("ga_xao_ot",                 DishDifficulty.Normal, "chicken", "chili",    "fishsauce"),
        new RecipeEntry("khoai_tay_chien",           DishDifficulty.Easy,   "khoaitay","salt"),
        new RecipeEntry("nam_xao_thit_bo",           DishDifficulty.Normal, "mushroom","beef",     "soysauce"),
        new RecipeEntry("nuoc_mia_chanh",            DishDifficulty.Easy,   "sugar",   "lemon"),
        new RecipeEntry("pho_bo_tai",                DishDifficulty.Hard,   "beef",    "rice",     "fishsauce","herbs"),
        new RecipeEntry("salad_bap_cai_chanh",       DishDifficulty.Easy,   "bapcai",  "lemon",    "herbs"),
        new RecipeEntry("salad_nam_rau",             DishDifficulty.Normal, "mushroom","herbs",    "lemon"),
        new RecipeEntry("suon_heo_xao_chua_ngot",   DishDifficulty.Hard,   "pork",    "sugar",    "lemon",    "cachua"),
        new RecipeEntry("sup_ngo_nam",               DishDifficulty.Normal, "ngo",     "mushroom", "salt"),
        new RecipeEntry("thit_heo_luoc_cuon_rau",   DishDifficulty.Normal, "pork",    "herbs",    "fishsauce"),
        new RecipeEntry("trung_chien_ca_chua",       DishDifficulty.Normal, "egg",     "cachua",   "fishsauce"),
        new RecipeEntry("trung_op_la_bo_ne",         DishDifficulty.Easy,   "egg",     "beef",     "pepper"),
    };

    private class RecipeEntry
    {
        public string   dishId;
        public DishDifficulty difficulty;
        public string[] ingredientIds;

        public RecipeEntry(string id, DishDifficulty diff, params string[] ids)
        {
            dishId        = id;
            difficulty    = diff;
            ingredientIds = ids;
        }
    }

    // ─── Core ────────────────────────────────────────────────────────────────

    static void Run(bool dryRun)
    {
        // 1. Đảm bảo ING_Fish tồn tại
        if (!dryRun)
            EnsureFishIngredient();

        // 2. Xây bảng tra IngredientData theo id
        var ingMap = BuildIngredientMap();

        // 3. Xử lý từng recipe
        int updated   = 0;
        int missingIng = 0;

        foreach (var recipe in Recipes)
        {
            string path = $"{DishFolder}/Dish_{recipe.dishId}.asset";
            var dish = AssetDatabase.LoadAssetAtPath<DishData>(path);
            if (dish == null)
            {
                Debug.LogWarning($"[RecipeGen] Không tìm thấy DishData: {path}");
                continue;
            }

            var ings = new List<IngredientData>();
            foreach (string id in recipe.ingredientIds)
            {
                if (ingMap.TryGetValue(id, out var ing))
                    ings.Add(ing);
                else
                {
                    Debug.LogWarning($"[RecipeGen] [{recipe.dishId}] Không tìm thấy IngredientData id='{id}'");
                    missingIng++;
                }
            }

            FlavorVector flavor = ComputeFlavor(ings);

            if (dryRun)
            {
                Debug.Log($"[RecipeGen] SẼ CẬP NHẬT '{dish.dishId}': " +
                          $"difficulty={recipe.difficulty}  " +
                          $"ingredients=[{string.Join(", ", recipe.ingredientIds)}]  " +
                          $"flavor=({flavor.sweet},{flavor.spicy},{flavor.sour},{flavor.umami},{flavor.texture})");
            }
            else
            {
                var so = new SerializedObject(dish);
                so.Update();

                so.FindProperty("difficulty").enumValueIndex = (int)recipe.difficulty;

                var listProp = so.FindProperty("requiredIngredients");
                listProp.arraySize = ings.Count;
                for (int i = 0; i < ings.Count; i++)
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = ings[i];

                var fl = so.FindProperty("targetFlavor");
                fl.FindPropertyRelative("sweet").intValue   = flavor.sweet;
                fl.FindPropertyRelative("spicy").intValue   = flavor.spicy;
                fl.FindPropertyRelative("sour").intValue    = flavor.sour;
                fl.FindPropertyRelative("umami").intValue   = flavor.umami;
                fl.FindPropertyRelative("texture").intValue = flavor.texture;

                // hint slots: lấy 2 ingredient đầu tiên loại Ingredient (non-seasoning)
                var ingOnly = ings.FindAll(x => x.kind == IngredientKind.Ingredient);
                SetHint(so, "required1", ingOnly.Count > 0 ? ingOnly[0] : null);
                SetHint(so, "required2", ingOnly.Count > 1 ? ingOnly[1] : null);

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(dish);
                updated++;
            }
        }

        // 4. Đồng bộ All_Data.asset
        int addedToList = 0;
        if (!dryRun)
            addedToList = SyncListDishData();

        // 5. Lưu
        if (!dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // 6. Báo cáo
        string mode = dryRun ? "PREVIEW" : "ĐÃ ÁP DỤNG";
        string msg  = $"[RecipeGen] ── {mode} ──\n" +
                      $"  Tổng recipe: {Recipes.Length}\n" +
                      $"  {(dryRun ? "Sẽ cập nhật" : "Đã cập nhật")} DishData: {(dryRun ? Recipes.Length : updated)}\n" +
                      $"  IngredientData thiếu: {missingIng}\n" +
                      $"  {(dryRun ? "—" : "Đã thêm")} vào All_Data: {addedToList}";
        Debug.Log(msg);

        string dlg = dryRun
            ? $"Preview:\n• Sẽ cập nhật {Recipes.Length} DishData\n• {missingIng} ingredient ID chưa có\n\nXem Console để chi tiết.\nChạy 'Apply' để áp dụng."
            : $"Hoàn tất!\n• Đã cập nhật {updated} DishData\n• Đã thêm {addedToList} vào All_Data";
        EditorUtility.DisplayDialog(dryRun ? "Preview" : "Generate Recipes — Xong", dlg, "OK");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    static void EnsureFishIngredient()
    {
        string path = $"{IngFolderA}/ING_Fish.asset";
        if (AssetDatabase.LoadAssetAtPath<IngredientData>(path) != null)
        {
            Debug.Log("[RecipeGen] ING_Fish.asset đã tồn tại — giữ nguyên.");
            return;
        }

        var fish = ScriptableObject.CreateInstance<IngredientData>();
        fish.id          = "ca";
        fish.displayName = "Cá";
        fish.kind        = IngredientKind.Ingredient;
        fish.tier        = IngredientTier.Basic;
        fish.stars       = 3;
        fish.vector      = new FlavorVector { umami = 2, texture = 2 };

        AssetDatabase.CreateAsset(fish, path);
        Debug.Log($"[RecipeGen] Đã tạo ING_Fish.asset (id='ca') tại {path}");
    }

    static Dictionary<string, IngredientData> BuildIngredientMap()
    {
        var map   = new Dictionary<string, IngredientData>();
        var guids = AssetDatabase.FindAssets("t:IngredientData", new[] { IngFolderA, IngFolderB });

        foreach (string g in guids)
        {
            var ing = AssetDatabase.LoadAssetAtPath<IngredientData>(AssetDatabase.GUIDToAssetPath(g));
            if (ing == null || string.IsNullOrEmpty(ing.id)) continue;

            string key = ing.id.Trim().ToLowerInvariant();
            if (!map.ContainsKey(key))
                map[key] = ing;
        }

        return map;
    }

    static FlavorVector ComputeFlavor(List<IngredientData> ings)
    {
        var v = new FlavorVector();
        foreach (var ing in ings)
        {
            v.sweet   += ing.vector.sweet;
            v.spicy   += ing.vector.spicy;
            v.sour    += ing.vector.sour;
            v.umami   += ing.vector.umami;
            v.texture += ing.vector.texture;
        }
        return v;
    }

    static void SetHint(SerializedObject so, string propName, IngredientData ing)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;

        prop.FindPropertyRelative("displayName").stringValue =
            ing != null ? ing.displayName : string.Empty;
        prop.FindPropertyRelative("icon").objectReferenceValue =
            ing != null ? ing.icon : null;
    }

    static int SyncListDishData()
    {
        var listData = AssetDatabase.LoadAssetAtPath<ListDishData>(ListDishAsset);
        if (listData == null)
        {
            Debug.LogWarning($"[RecipeGen] Không tìm thấy All_Data.asset tại {ListDishAsset}");
            return 0;
        }

        var so = new SerializedObject(listData);
        so.Update();
        var listProp = so.FindProperty("allDishes");

        // GUID của các dish đã có
        var existing = new HashSet<string>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            var elem = listProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (elem != null)
                existing.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(elem)));
        }

        // Quét toàn bộ DishData trong folder
        string[] dishGuids = AssetDatabase.FindAssets("t:DishData", new[] { DishFolder });
        int added = 0;
        foreach (string g in dishGuids)
        {
            if (existing.Contains(g)) continue;

            string assetPath = AssetDatabase.GUIDToAssetPath(g);
            var dish = AssetDatabase.LoadAssetAtPath<DishData>(assetPath);
            if (dish == null) continue;

            int idx = listProp.arraySize;
            listProp.arraySize = idx + 1;
            listProp.GetArrayElementAtIndex(idx).objectReferenceValue = dish;
            existing.Add(g);
            added++;
            Debug.Log($"[RecipeGen] [All_Data] THÊM: '{dish.dishId}' ({dish.dishName})");
        }

        if (added > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(listData);
        }
        else
        {
            Debug.Log("[RecipeGen] [All_Data] Tất cả dish đã có — không cần thêm.");
        }

        return added;
    }
}
