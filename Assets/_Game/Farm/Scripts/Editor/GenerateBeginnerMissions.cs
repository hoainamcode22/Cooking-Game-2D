using UnityEditor;
using UnityEngine;
using System.IO;

public static class GenerateBeginnerMissions
{
    private const string OUTPUT_PATH = "Assets/_Game/Farm/data/Data_Ewa";

    [MenuItem("Tools/Generate Beginner Missions")]
    public static void Generate()
    {
        if (!Directory.Exists(Application.dataPath + "/_Game/Farm/data/Data_Ewa"))
            Directory.CreateDirectory(Application.dataPath + "/_Game/Farm/data/Data_Ewa");

        var missions = new (string name, string id, int target, int reward, RewardType type)[]
        {
            // --- Thu hoạch nông sản ---
            ("Thu thập 10 Lúa",          "rice",          10, 50,  RewardType.Coin),
            ("Thu thập 10 Cà Chua",      "cachua",        10, 50,  RewardType.Coin),
            ("Thu thập 10 Bắp Cải",      "bapcai",        10, 50,  RewardType.Coin),
            ("Thu thập 10 Ngô",          "ngo",           10, 50,  RewardType.Coin),
            ("Thu thập 5 Nấm",           "nam",            5, 40,  RewardType.Coin),
            ("Thu thập 5 Ớt",            "chili",          5, 40,  RewardType.Coin),
            ("Thu thập 5 Tiêu",          "pepper",         5, 40,  RewardType.Coin),
            ("Thu thập 5 Chanh",         "lemon",          5, 40,  RewardType.Coin),
            ("Thu thập 5 Mía",           "sugarcane",      5, 40,  RewardType.Coin),

            // --- Thu hoạch hoa ---
            ("Thu thập 3 Hoa Hồng",      "hoa_hong",       3,  5,  RewardType.Diamond),
            ("Thu thập 3 Hoa Lan",       "hoa_lan",        3,  5,  RewardType.Diamond),
            ("Thu thập 3 Tulip",         "tulip",          3,  5,  RewardType.Diamond),
            ("Thu thập 3 Hướng Dương",   "huong_duong",    3,  5,  RewardType.Diamond),

            // --- Sản phẩm chăn nuôi ---
            ("Thu thập 5 Trứng",         "egg",            5, 60,  RewardType.Coin),
            ("Thu thập 3 Thịt Bò",       "beef",           3, 80,  RewardType.Coin),
            ("Thu thập 3 Thịt Gà",       "chicken_meat",   3, 80,  RewardType.Coin),
            ("Thu thập 3 Thịt Heo",      "pork",           3, 80,  RewardType.Coin),

            // --- Giao món ăn ---
            ("Giao 1 Phở Bò Tái",           "pho_beef",               1, 10, RewardType.Diamond),
            ("Giao 1 Trứng Chiên Cà Chua",  "trung_chien_ca_chua",    1, 80, RewardType.Coin),
            ("Giao 1 Gà Xào Ớt",            "ga_xao_ot",              1, 10, RewardType.Diamond),
        };

        int created = 0;
        foreach (var m in missions)
        {
            var asset = ScriptableObject.CreateInstance<MissionData>();
            asset.missionName  = m.name;
            asset.targetAmount = m.target;
            asset.rewardAmount = m.reward;
            asset.rewardType   = m.type;
            // missionIcon và rewardIcon để trống — kéo thủ công sau

            string filePath = $"{OUTPUT_PATH}/Mission_{m.id}.asset";
            AssetDatabase.CreateAsset(asset, filePath);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateBeginnerMissions] Đã tạo {created} MissionData tại {OUTPUT_PATH}");
        EditorUtility.DisplayDialog("Hoàn tất", $"Đã tạo {created} nhiệm vụ tân thủ tại:\n{OUTPUT_PATH}", "OK");
    }
}
