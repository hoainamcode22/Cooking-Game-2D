#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FixMobileIconsAndShopSizeTool
{
    static FixMobileIconsAndShopSizeTool()
    {
        EditorApplication.delayCall += () =>
        {
            FixAllIconsAndData(false);
        };
    }

    [MenuItem("Tools/Farm/Sửa lỗi Icon Mobile & Kích thước Shop", false, 1)]
    public static void RunManual()
    {
        FixAllIconsAndData(true);
    }

    public static void FixAllIconsAndData(bool showDialog)
    {
        int textureFixedCount = 0;

        // 1. Sửa Texture Import Settings cho UI_MarketBoard và UI_Stall
        string[] folders = { "Assets/_Game/Resources/UI_MarketBoard", "Assets/_Game/Resources/UI_Stall" };
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;
            string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                string assetPath = file.Replace("\\", "/");
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        dirty = true;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        dirty = true;
                    }

                    if (dirty)
                    {
                        importer.alphaIsTransparency = true;
                        importer.mipmapEnabled = false;
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        textureFixedCount++;
                    }
                }
            }
        }

        // 2. Nạp toàn bộ CropData & InventoryItemData vào StallItemCatalog trong Scene và Prefab
        List<CropData> crops = new List<CropData>();
        string[] cropGuids = AssetDatabase.FindAssets("t:CropData");
        for (int i = 0; i < cropGuids.Length; i++)
        {
            CropData c = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(cropGuids[i]));
            if (c != null) crops.Add(c);
        }

        List<InventoryItemData> items = new List<InventoryItemData>();
        string[] itemGuids = AssetDatabase.FindAssets("t:InventoryItemData");
        for (int i = 0; i < itemGuids.Length; i++)
        {
            InventoryItemData item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(AssetDatabase.GUIDToAssetPath(itemGuids[i]));
            if (item != null) items.Add(item);
        }

        // Cập nhật trong Scene
        StallItemCatalog[] sceneCatalogs = Object.FindObjectsByType<StallItemCatalog>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var cat in sceneCatalogs)
        {
            cat.EditorSetDatabases(crops, items);
            EditorUtility.SetDirty(cat);
        }

        // Cập nhật ShopManager trong Scene
        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop != null && shop.shopPanel != null)
        {
            RectTransform rt = shop.shopPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(1500f, 880f);
                EditorUtility.SetDirty(rt);
            }
        }

        // Cập nhật kích thước Liềm gặt (SickleController & Sickle_Bottom_Tray)
        SickleController sickle = Object.FindFirstObjectByType<SickleController>(FindObjectsInactive.Include);
        if (sickle != null)
        {
            var field = typeof(SickleController).GetField("sickleScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(sickle, 12.5f);
                EditorUtility.SetDirty(sickle);
            }
        }

        GameObject sickleTray = GameObject.Find("Sickle_Bottom_Tray");
        if (sickleTray != null)
        {
            RectTransform trayRT = sickleTray.GetComponent<RectTransform>();
            if (trayRT != null)
            {
                trayRT.sizeDelta = new Vector2(250f, 150f);
                EditorUtility.SetDirty(trayRT);
            }

            Transform iconTrans = sickleTray.transform.Find("Sickle_Icon");
            if (iconTrans != null)
            {
                RectTransform iconRT = iconTrans.GetComponent<RectTransform>();
                if (iconRT != null)
                {
                    iconRT.sizeDelta = new Vector2(130f, 130f);
                    EditorUtility.SetDirty(iconRT);
                }
            }
        }

        if (sceneCatalogs.Length > 0 || shop != null || sickle != null)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Farm UI Fixer",
                $"Đã tối ưu:\n- Sửa {textureFixedCount} textures sang Sprite (Single)\n- Nạp {crops.Count} Crops & {items.Count} Items vào StallItemCatalog\n- Đồng bộ kích thước ShopPanel (1500x880)\n- Tăng kích thước Liềm gặt (12.5 scale, tray icon 130x130).", "OK");
        }
    }
}
#endif
