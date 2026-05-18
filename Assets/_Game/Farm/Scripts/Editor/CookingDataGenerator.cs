using UnityEditor;
using UnityEngine;
using System.IO;

public static class CookingDataGenerator
{
    private const string OUTPUT_PATH = "Assets/_Game/Farm/data/Farm_Cooking";

    [MenuItem("Tools/Generate 20 Dishes")]
    public static void Generate()
    {
        if (!Directory.Exists(Application.dataPath + "/_Game/Farm/data/Farm_Cooking"))
            Directory.CreateDirectory(Application.dataPath + "/_Game/Farm/data/Farm_Cooking");

        // (id, tên món, phụ đề nguyên liệu, độ khó)
        var dishes = new (string id, string name, string subtitle, DishDifficulty diff)[]
        {
            // ── Món chính ──────────────────────────────────────────────────────
            ("pho_bo_tai",              "Phở bò tái",               "Lúa · Thịt bò · Chanh · Ớt",             DishDifficulty.Normal),
            ("trung_chien_ca_chua",     "Trứng chiên cà chua",      "Trứng gà · Cà chua",                      DishDifficulty.Easy),
            ("ga_xao_ot",               "Gà xào ớt",                "Thịt gà · Ớt",                            DishDifficulty.Easy),
            ("bo_xao_tieu",             "Bò xào tiêu",              "Thịt bò · Tiêu",                          DishDifficulty.Easy),
            ("salad_bap_cai_chanh",     "Salad bắp cải chanh",      "Bắp cải · Chanh",                         DishDifficulty.Easy),
            ("canh_chua_ca",            "Canh chua cá",             "Cá · Cà chua · Chanh · Ớt",               DishDifficulty.Normal),
            ("ca_nuong_tieu",           "Cá nướng tiêu",            "Cá · Tiêu",                               DishDifficulty.Easy),
            ("thit_heo_luoc_cuon_rau",  "Thịt heo luộc cuốn rau",  "Thịt heo · Rau",                          DishDifficulty.Easy),
            ("suon_heo_xao_chua_ngot",  "Sườn heo xào chua ngọt",  "Thịt heo · Cà chua · Chanh · Ớt",        DishDifficulty.Normal),
            ("ga_nuong_lu",             "Gà nướng lu mật mía",      "Thịt gà · Mía",                           DishDifficulty.Normal),

            // ── Món rau & chay ─────────────────────────────────────────────────
            ("sup_ngo_nam",             "Súp ngô nấm",              "Ngô · Nấm · Trứng gà",                    DishDifficulty.Easy),
            ("khoai_tay_chien",         "Khoai tây chiên",          "Khoai tây",                               DishDifficulty.Easy),
            ("canh_khoai_tay_thit_heo", "Canh khoai tây thịt heo", "Khoai tây · Cà rốt · Thịt heo",          DishDifficulty.Normal),
            ("salad_nam_rau",           "Salad nấm và rau",         "Nấm · Rau",                               DishDifficulty.Easy),
            ("bap_cai_xao_nam",         "Bắp cải xào nấm",         "Bắp cải · Nấm",                           DishDifficulty.Easy),

            // ── Món nâng cao ───────────────────────────────────────────────────
            ("nam_xao_thit_bo",         "Nấm xào thịt bò",         "Nấm · Thịt bò",                           DishDifficulty.Normal),
            ("trung_op_la_bo_ne",       "Trứng ốp la bò né",       "Trứng gà · Thịt bò · Cà chua",           DishDifficulty.Normal),
            ("com_chien_trung",         "Cơm chiên trứng",          "Lúa · Trứng gà",                          DishDifficulty.Easy),
            ("nuoc_mia_chanh",          "Nước mía chanh",           "Mía · Chanh",                             DishDifficulty.Easy),
            ("bo_ham_ca_rot",           "Bò hầm cà rốt",           "Thịt bò · Cà rốt",                        DishDifficulty.Hard),
        };

        int created = 0;
        foreach (var d in dishes)
        {
            var asset = ScriptableObject.CreateInstance<DishData>();
            asset.dishId       = d.id;
            asset.dishName     = d.name;
            asset.dishSubTitle = d.subtitle;
            asset.difficulty   = d.diff;
            // dishSprite và flavor/hints để null — Dev kéo thả sau trong Inspector

            string filePath = $"{OUTPUT_PATH}/Dish_{d.id}.asset";
            AssetDatabase.CreateAsset(asset, filePath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CookingDataGenerator] Đã tạo {created} DishData tại {OUTPUT_PATH}");
        EditorUtility.DisplayDialog("Hoàn tất", $"Đã tạo {created} DishData tại:\n{OUTPUT_PATH}", "OK");
    }
}
