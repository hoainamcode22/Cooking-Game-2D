#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopTabIconUpdater
{
    static ShopTabIconUpdater()
    {
        EditorApplication.delayCall += AutoUpdateTabIconsInScene;
    }

    [MenuItem("Tools/Farm/Shop/★ Cập nhật Icon 3 Tab Shop (Tự động)")]
    public static void UpdateTabIcons()
    {
        Sprite seedSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/tab_seeds.png");
        Sprite buildSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/tab_buildings.png");
        Sprite decorSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Icons/tab_decorations.png");

        ShopManager shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            Debug.LogWarning("[ShopTabIconUpdater] Không tìm thấy ShopManager trong scene.");
            return;
        }

        Transform root = shop.shopPanel != null ? shop.shopPanel.transform : shop.transform;
        
        // 1. Tab 1 - Hạt giống
        Transform img1Trans = root.Find("Tabs_Row/Tab_Seed/Tab1_Content/Img_Icon");
        if (img1Trans != null && seedSpr != null)
        {
            Image img = img1Trans.GetComponent<Image>();
            if (img != null) { img.sprite = seedSpr; EditorUtility.SetDirty(img); }
        }

        // 2. Tab 2 - Công trình
        Transform img2Trans = root.Find("Tabs_Row/Tab_Building/Tab2_Content/Img_Icon");
        if (img2Trans != null && buildSpr != null)
        {
            Image img = img2Trans.GetComponent<Image>();
            if (img != null) { img.sprite = buildSpr; EditorUtility.SetDirty(img); }
        }

        // 3. Tab 3 - Trang trí
        Transform img3Trans = root.Find("Tabs_Row/Tab_Decor/Tab3_Content/Img_Icon");
        if (img3Trans != null && decorSpr != null)
        {
            Image img = img3Trans.GetComponent<Image>();
            if (img != null) { img.sprite = decorSpr; EditorUtility.SetDirty(img); }
        }

        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        Debug.Log("<color=green>[ShopTabIconUpdater] Đã cập nhật 3 Icon Tab Shop thành công!</color>");
    }

    private static void AutoUpdateTabIconsInScene()
    {
        UpdateTabIcons();
    }
}
#endif
