#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// ⛔ [VÒNG 13 — 04/09/2026] ĐÃ TẮT TỰ CHẠY THEO LỆNH LEAD.
// Trước đây attribute [InitializeOnLoad] khiến static constructor chạy MỖI LẦN Unity biên dịch
// lại, kéo theo EditorApplication.delayCall → tool tự sửa scene rồi TỰ LƯU. Hậu quả: mọi thứ
// Sếp kéo tay trong scene (vị trí prefab tàu, nút HUD, reference nhân vật popup) đều bị ghi đè
// âm thầm sau mỗi lần compile — đây chính là nguyên nhân của chuỗi lỗi "tự nhiên hỏng".
// Menu trong Tools/... VẪN CÒN — muốn chạy thì bấm tay, chủ động và kiểm soát được.
// Muốn bật lại: bỏ dấu // ở dòng dưới.
// [InitializeOnLoad]
public static class ShopTabIconUpdater
{
    static ShopTabIconUpdater()
    {
        // ⛔ [VÒNG 14] ĐÃ TẮT — dòng dưới từng khiến tool tự chạy + tự lưu scene mỗi lần compile.
        // Comment [InitializeOnLoad] ở vòng 13 là CHƯA ĐỦ: chỉ cần code khác chạm vào bất kỳ
        // member nào của class là static constructor vẫn chạy, và dòng này vẫn đăng ký.
        // Muốn chạy: bấm menu trong Tools/... (chủ động, kiểm soát được).
        // EditorApplication.delayCall += AutoUpdateTabIconsInScene;
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
