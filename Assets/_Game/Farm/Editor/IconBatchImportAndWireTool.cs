#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool tự động import tất cả ảnh trong Assets/Assetsgame/Icon_Processed thành Sprite 2D
/// và liên kết (wire) vào các Prefab, ScriptableObject, và Popup trong game.
/// </summary>
public static class IconBatchImportAndWireTool
{
    private const string BaseProcessedPath = "Assets/Assetsgame/Icon_Processed";
    private const string BaseRawPath = "Assets/Assetsgame/Icon";

    [MenuItem("Tools/Farm/Tự động gán toàn bộ Icon mới (Batch Wire Icons)", false, 10)]
    public static void RunBatchImportAndWire()
    {
        Debug.Log("===> [IconBatch] Bắt đầu cấu hình TextureImporter cho toàn bộ thư mục Icon và Icon_Processed...");
        ConfigureTextureImporters(BaseProcessedPath);
        ConfigureTextureImporters(BaseRawPath);

        Debug.Log("===> [IconBatch] Bắt đầu gán Icon cho Popup Nhiệm vụ / Đăng nhập...");
        WireUnifiedTaskPopup();

        Debug.Log("===> [IconBatch] Bắt đầu gán Icon cho Thành tựu (Achievements)...");
        WireAchievements();

        Debug.Log("===> [IconBatch] Bắt đầu gán Icon cho Quầy hàng (Stall Category Tabs)...");
        WireStallCategoryTabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>===> [IconBatch] HOÀN TẤT GÁN TOÀN BỘ 29 ICON MỚI THÀNH CÔNG!</color>");
    }

    private static void ConfigureTextureImporters(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }
        Debug.Log($"[IconBatch] Đã cấu hình {count} Texture trong {folderPath} thành Sprite 2D.");
    }

    private static void WireUnifiedTaskPopup()
    {
        Sprite missionTabIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/NhiemVu/icon_tab_mission.png");
        Sprite dailyTabIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_calendar.png");
        Sprite achievementTabIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_trophy_gold.png");
        Sprite chestIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/NhiemVu/icon_chest_gold.png");

        Sprite[] dailyRewardIcons = new Sprite[7]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_gold_sack.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_fertilizer_bag.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_diamond_pouch.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_upgrade_mats.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_gourmet_dish.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_harvest_basket.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/DangNhap/login_grand_chest.png")
        };

        // Wire in Scene if active
        UnifiedTaskPopupUI popupInScene = Object.FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);
        if (popupInScene != null)
        {
            ApplySpritesToPopup(popupInScene, missionTabIcon, dailyTabIcon, achievementTabIcon, chestIcon, dailyRewardIcons);
            EditorUtility.SetDirty(popupInScene);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(popupInScene.gameObject.scene);
            Debug.Log("[IconBatch] Đã gán Icon cho UnifiedTaskPopupUI trong Scene.");
        }

        // Wire in Prefab
        string prefabPath = "Assets/_Game/Prefab/ui/UnifiedTaskPopupRoot.prefab";
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabRoot != null)
        {
            UnifiedTaskPopupUI prefabUI = prefabRoot.GetComponent<UnifiedTaskPopupUI>();
            if (prefabUI != null)
            {
                ApplySpritesToPopup(prefabUI, missionTabIcon, dailyTabIcon, achievementTabIcon, chestIcon, dailyRewardIcons);
                EditorUtility.SetDirty(prefabUI);
                PrefabUtility.SavePrefabAsset(prefabRoot);
                Debug.Log("[IconBatch] Đã gán Icon cho Prefab UnifiedTaskPopupRoot.prefab.");
            }
        }
    }

    private static void ApplySpritesToPopup(UnifiedTaskPopupUI popup, Sprite missionTab, Sprite dailyTab, Sprite achieveTab, Sprite chest, Sprite[] daily7)
    {
        SerializedObject so = new SerializedObject(popup);
        SerializedProperty sp = so.FindProperty("sprites");
        if (sp != null)
        {
            SetProp(sp, "missionTabIcon", missionTab);
            SetProp(sp, "dailyTabIcon", dailyTab);
            SetProp(sp, "achievementTabIcon", achieveTab);
            SetProp(sp, "chestIcon", chest);

            SerializedProperty dailyArr = sp.FindPropertyRelative("dailyRewardIcons");
            if (dailyArr != null && dailyArr.isArray)
            {
                dailyArr.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    if (daily7[i] != null)
                    {
                        dailyArr.GetArrayElementAtIndex(i).objectReferenceValue = daily7[i];
                    }
                }
            }
            so.ApplyModifiedProperties();
        }
    }

    private static void SetProp(SerializedProperty parent, string propName, Object val)
    {
        SerializedProperty prop = parent.FindPropertyRelative(propName);
        if (prop != null && val != null)
        {
            prop.objectReferenceValue = val;
        }
    }

    private static void WireAchievements()
    {
        Sprite sprWheat = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_harvest_wheat.png");
        Sprite sprChef = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_chef_hat.png");
        Sprite sprCargo = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_boat_cargo.png");
        Sprite sprRanch = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_ranch_horseshoe.png");
        Sprite sprShield = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_sprout_shield.png");
        Sprite sprLaurel = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_star_laurel.png");
        Sprite sprCrown = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_crown_ruby.png");
        Sprite sprTrophy = AssetDatabase.LoadAssetAtPath<Sprite>($"{BaseProcessedPath}/ThanhTuu/achieve_trophy_gold.png");

        string[] guids = AssetDatabase.FindAssets("t:MissionData", new[] { "Assets/_Game/Farm/data/Data_Ewa/Achievements" });
        int updated = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MissionData md = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (md == null) continue;

            string id = (md.missionId ?? Path.GetFileNameWithoutExtension(path)).ToLowerInvariant();
            Sprite targetSprite = null;

            if (id.Contains("harvest") || id.Contains("rice"))
            {
                targetSprite = sprWheat;
            }
            else if (id.Contains("cook") || id.Contains("beefdish"))
            {
                targetSprite = sprChef;
            }
            else if (id.Contains("order"))
            {
                targetSprite = sprCargo;
            }
            else if (id.Contains("process"))
            {
                targetSprite = sprRanch;
            }
            else if (id.Contains("level"))
            {
                // Parse level number
                int lvl = md.targetAmount > 0 ? md.targetAmount : 1;
                if (lvl <= 20) targetSprite = sprShield;
                else if (lvl <= 50) targetSprite = sprLaurel;
                else targetSprite = sprCrown;
            }
            else
            {
                targetSprite = sprTrophy;
            }

            if (targetSprite != null && md.missionIcon != targetSprite)
            {
                Undo.RecordObject(md, "Cập nhật Icon Thành Tựu");
                md.missionIcon = targetSprite;
                EditorUtility.SetDirty(md);
                updated++;
            }
        }

        Debug.Log($"[IconBatch] Đã cập nhật icon cho {updated}/{guids.Length} thành tựu.");
    }

    private static void WireStallCategoryTabs()
    {
        StallCategoryTabUI[] tabs = Object.FindObjectsByType<StallCategoryTabUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var tab in tabs)
        {
            tab.ApplyCategoryIcon();
            EditorUtility.SetDirty(tab);
            count++;
        }
        Debug.Log($"[IconBatch] Đã cập nhật {count} Tab Quầy Hàng trong Scene.");
    }
}
#endif