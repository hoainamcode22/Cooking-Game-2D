#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HouseSetupBatchTool
{
    private const string SpritesBase = "Assets/Assetsgame/Nhà/House_Sprites";
    private const string PrefabsBase = "Assets/_Game/Farm/CÔNG TRÌNH";
    private const string ShopDataBase = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding";

    private struct HouseConfig
    {
        public string HouseName;
        public string PrefabFile;
        public string DataAssetFile;
        public string ItemName;
        public int GoldPrice;
        public float BuildTime;

        public HouseConfig(string hName, string pFile, string dFile, string iName, int gold, float time)
        {
            HouseName = hName;
            PrefabFile = pFile;
            DataAssetFile = dFile;
            ItemName = iName;
            GoldPrice = gold;
            BuildTime = time;
        }
    }

    private static readonly HouseConfig[] Houses = new HouseConfig[]
    {
        new HouseConfig("House_01", "House_01.prefab", "Home1.asset", "Nhà Dân 1", 100, 60f),
        new HouseConfig("House_02", "House_02.prefab", "Home2.asset", "Nhà Dân 2", 350, 180f),
        new HouseConfig("House_03", "House_03.prefab", "Home3.asset", "Nhà Dân 3", 750, 360f),
        new HouseConfig("House_04", "House_04.prefab", "Home4.asset", "Nhà Dân 4", 1500, 600f),
        new HouseConfig("House_05", "House_05.prefab", "Home5.asset", "Nhà Dân 5", 3000, 900f)
    };

    [MenuItem("Tools/Farm/Setup Toan Bo 5 Nha Moi & DataShop", false, 1)]
    public static void SetupAllHousesAndShop()
    {
        Debug.Log("[HouseSetupBatchTool] Bắt đầu thiết lập toàn bộ 5 nhà mới và cập nhật DataShop...");

        foreach (var h in Houses)
        {
            // 1. Load 6 Stage Sprites
            Sprite[] stages = new Sprite[6];
            for (int s = 1; s <= 6; s++)
            {
                string spritePath = $"{SpritesBase}/{h.HouseName}/stage_{s}.png";
                stages[s - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (stages[s - 1] == null)
                {
                    Debug.LogWarning($"[HouseSetupBatchTool] Chưa load được sprite: {spritePath}");
                }
            }

            // 2. Update Prefab
            string prefabPath = $"{PrefabsBase}/{h.PrefabFile}";
            GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabGO != null)
            {
                string pPath = AssetDatabase.GetAssetPath(prefabGO);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(pPath);

                SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr == null) sr = prefabRoot.AddComponent<SpriteRenderer>();

                // Xóa toàn bộ prefab đèn / Light_Windows cũ được gắn vào nhà
                for (int c = prefabRoot.transform.childCount - 1; c >= 0; c--)
                {
                    Transform child = prefabRoot.transform.GetChild(c);
                    string childName = child.name.ToLower();
                    if (childName.Contains("light") || childName.Contains("den") || childName.Contains("lamp") || child.GetComponent<UnityEngine.Rendering.Universal.Light2D>() != null)
                    {
                        Debug.Log($"[HouseSetupBatchTool] Đã xóa đèn '{child.name}' khỏi {h.PrefabFile}");
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                // Gán ảnh hoàn thành (stage 4) làm default
                if (stages[3] != null)
                {
                    sr.sprite = stages[3];
                }

                // Gán hoặc cập nhật HouseGrowthController
                HouseGrowthController growth = prefabRoot.GetComponent<HouseGrowthController>();
                if (growth == null) growth = prefabRoot.AddComponent<HouseGrowthController>();

                growth.houseId = h.HouseName;
                growth.stage1_Frame = stages[0];
                growth.stage2_Foundation = stages[1];
                growth.stage3_HalfBuilt = stages[2];
                growth.stage4_Complete = stages[3];
                growth.stage5_GiftBox = stages[4];
                growth.stage6_BoxOpen = stages[5];
                growth.defaultBuildDuration = h.BuildTime;

                // Gán trực tiếp prefab Pháo Hoa Lana
                GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/VFX/LevelUp_Confetti_Lana02.prefab")
                                    ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Resources/VFX/Confetti_blast_multicolor.prefab");
                growth.fireworksVfxPrefab = vfxPrefab;

                // Cập nhật collider
                BoxCollider2D col = prefabRoot.GetComponent<BoxCollider2D>();
                if (col == null) col = prefabRoot.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                if (sr.sprite != null)
                {
                    col.size = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
                    col.offset = new Vector2(0, col.size.y * 0.5f);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, pPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                Debug.Log($"[HouseSetupBatchTool] Đã cập nhật Prefab: {prefabPath}");
            }
            else
            {
                Debug.LogError($"[HouseSetupBatchTool] Không tìm thấy Prefab: {prefabPath}");
            }

            // 3. Update BuildingData ScriptableObject
            string dataPath = $"{ShopDataBase}/{h.DataAssetFile}";
            BuildingData bData = AssetDatabase.LoadAssetAtPath<BuildingData>(dataPath);
            if (bData != null)
            {
                bData.itemName = h.ItemName;
                bData.goldPrice = h.GoldPrice;
                bData.buildTimeSeconds = h.BuildTime;
                if (stages[3] != null)
                {
                    bData.itemIcon = stages[3]; // Gán ảnh hoàn thành Stage 4 vào Icon Shop
                }
                bData.prefabToBuild = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorUtility.SetDirty(bData);
                Debug.Log($"[HouseSetupBatchTool] Đã cập nhật DataShop: {dataPath}");
            }
            else
            {
                Debug.LogError($"[HouseSetupBatchTool] Không tìm thấy DataShop: {dataPath}");
            }
        }

        // 4. Dọn dẹp đèn và cập nhật nhà có sẵn trên Scene
        CleanSceneHouses();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HouseSetupBatchTool] HOÀN TẤT thiết lập 5 nhà mới và DataShop thành công 100%!");
    }

    private static void CleanSceneHouses()
    {
        var sceneHouses = Object.FindObjectsByType<HouseGrowthController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in sceneHouses)
        {
            // Xóa đèn con nếu có
            for (int c = h.transform.childCount - 1; c >= 0; c--)
            {
                Transform child = h.transform.GetChild(c);
                string cName = child.name.ToLower();
                if (cName.Contains("light") || cName.Contains("den") || cName.Contains("lamp") || child.GetComponent<UnityEngine.Rendering.Universal.Light2D>() != null)
                {
                    Debug.Log($"[HouseSetupBatchTool] Đã xóa đèn '{child.name}' khỏi scene object '{h.name}'");
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            // Nhà có sẵn trong scene mặc định là Completed
            PlayerPrefs.SetString(h.GetSaveKey(), "Completed");
            EditorUtility.SetDirty(h.gameObject);
        }
        PlayerPrefs.Save();
    }
}
#endif
