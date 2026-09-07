#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool bảo trì và nâng cấp Cửa Hàng (Shop & Store Maintenance):
/// 1. Cập nhật icon Ổ Khóa Vàng sắc nét cho tất cả các ô bị khóa.
/// 2. Xóa 3 máy sản xuất (Máy Xay Bột, Máy Ép Mía, Máy Phô Mai) khỏi tab Công Trình trong Shop.
/// 3. Đóng gói và sắp xếp toàn bộ vật phẩm Trang Trí (Đèn, Củi Lửa, Cây, Bụi Cây, Hoa, Hàng Rào...) vào tab Trang Trí theo hàng riêng gọn gàng.
/// </summary>
public static class ShopMaintenanceAndDecorTool
{
    private const string MENU_ALL = "Tools/Farm Game/Shop/★ DỌN DẸP & NÂNG CẤP SHOP TOÀN DIỆN (1-CLICK)";

    [MenuItem(MENU_ALL, false, 1)]
    public static void ExecuteAll()
    {
        var sb = new StringBuilder("===== DỌN DẸP & NÂNG CẤP SHOP =====\n\n");

        // 1. Tự động sinh lại bộ sprite với Ổ Khóa Vàng mới
        ShopSpriteGenerator.GenerateAllSprites();
        sb.AppendLine("✔ 1. Đã sinh lại bộ sprite Shop với Ổ Khóa Vàng sắc nét (shop_lock_badge.png).");

        // 2. Tìm ShopManager trong scene
        var shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            EditorUtility.DisplayDialog("Shop Tool", "Không tìm thấy ShopManager trong scene hiện tại. Hãy mở scene SCN_Farm!", "OK");
            return;
        }

        Undo.RecordObject(shop, "Dọn dẹp và sắp xếp Shop");

        // 3. Xóa 3 máy khỏi buildingList
        int removedMachines = 0;
        if (shop.buildingList != null)
        {
            var cleanBuildingList = new List<BaseItemData>();
            foreach (var b in shop.buildingList)
            {
                if (b == null) continue;
                string nameLower = b.itemName.ToLower();
                string id = b.itemID;

                bool isMachine = id == "120" || id == "121" || id == "122" ||
                                 nameLower.Contains("máy xay") || nameLower.Contains("máy ép") || nameLower.Contains("máy phô mai");

                if (isMachine)
                {
                    removedMachines++;
                    sb.AppendLine($"  - Đã xóa khỏi Shop: {b.itemName} (ID: {b.itemID})");
                }
                else
                {
                    cleanBuildingList.Add(b);
                }
            }
            shop.buildingList = cleanBuildingList;
        }
        sb.AppendLine($"✔ 2. Đã gỡ bỏ {removedMachines} máy sản xuất khỏi tab Công Trình.");

        // 4. Tìm và đóng gói toàn bộ Decor items vào decorList theo thứ tự khoa học
        if (shop.decorList == null) shop.decorList = new List<BaseItemData>();

        // Quét toàn bộ DecorData asset trong project
        string[] decorGuids = AssetDatabase.FindAssets("t:PlaceableItemData", new[] { "Assets/_Game/Farm/CÔNG TRÌNH", "Assets/_Game/Farm/data" });
        var foundDecors = new List<PlaceableItemData>();

        foreach (var guid in decorGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (item != null && !(item is BuildingData))
            {
                if (!foundDecors.Contains(item))
                    foundDecors.Add(item);
            }
        }

        // Sắp xếp decor theo thứ tự ưu tiên: Đèn, Lửa, Cây, Bụi, Hàng Rào, Hoa, Tượng, v.v.
        foundDecors.Sort((a, b) =>
        {
            int scoreA = GetDecorPriorityScore(a.itemName);
            int scoreB = GetDecorPriorityScore(b.itemName);
            if (scoreA != scoreB) return scoreA.CompareTo(scoreB);
            return a.unlockLevel.CompareTo(b.unlockLevel);
        });

        shop.decorList.Clear();
        foreach (var d in foundDecors)
        {
            shop.decorList.Add(d);
            sb.AppendLine($"  + Trang trí: {d.itemName} (Cấp {d.unlockLevel}, Giá: {d.goldPrice} vàng)");
        }
        sb.AppendLine($"✔ 3. Đã đóng gói & sắp xếp {foundDecors.Count} vật phẩm Trang Trí vào tab Trang Trí.");

        // 4. Cập nhật Lock Badge trên shop.itemPrefab (hỗ trợ cả scene GameObject lẫn Prefab asset)
        if (shop.itemPrefab != null)
        {
            Transform lockBadgeTransform = shop.itemPrefab.transform.Find("Lock_Overlay/Lock_Badge");
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_lock_badge.png");
            if (lockBadgeTransform != null && spr != null)
            {
                var img = lockBadgeTransform.GetComponent<Image>();
                if (img != null)
                {
                    Undo.RecordObject(img, "Update Lock Badge Sprite");
                    img.sprite = spr;
                    img.color = Color.white;
                    EditorUtility.SetDirty(img);
                }
                var rt = lockBadgeTransform.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Undo.RecordObject(rt, "Update Lock Badge Size");
                    rt.sizeDelta = new Vector2(68f, 68f);
                    EditorUtility.SetDirty(rt);
                }
                sb.AppendLine("✔ 4. Đã gắn Ổ Khóa Vàng vào ShopItem_Template.");
            }

            string prefabPath = AssetDatabase.GetAssetPath(shop.itemPrefab);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                var pLockBadge = prefabRoot.transform.Find("Lock_Overlay/Lock_Badge");
                if (pLockBadge != null && spr != null)
                {
                    var img = pLockBadge.GetComponent<Image>();
                    if (img != null) { img.sprite = spr; img.color = Color.white; }
                    var rt = pLockBadge.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(68f, 68f);
                }
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // Dùng SerializedObject để ghi nhận thay đổi vào Scene YAML
        SerializedObject so = new SerializedObject(shop);
        SerializedProperty bListProp = so.FindProperty("buildingList");
        if (bListProp != null)
        {
            bListProp.ClearArray();
            for (int i = 0; i < shop.buildingList.Count; i++)
            {
                bListProp.InsertArrayElementAtIndex(i);
                bListProp.GetArrayElementAtIndex(i).objectReferenceValue = shop.buildingList[i];
            }
        }
        SerializedProperty dListProp = so.FindProperty("decorList");
        if (dListProp != null)
        {
            dListProp.ClearArray();
            for (int i = 0; i < shop.decorList.Count; i++)
            {
                dListProp.InsertArrayElementAtIndex(i);
                dListProp.GetArrayElementAtIndex(i).objectReferenceValue = shop.decorList[i];
            }
        }
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        EditorSceneManager.SaveScene(shop.gameObject.scene);

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Shop Setup Thành Công!",
            "Đã thực hiện hoàn tất 4 yêu cầu:\n\n" +
            "1. Ổ Khóa Vàng đã được vẽ sắc nét và gắn vào các ô tròn khóa trong Cửa Hàng.\n" +
            "2. Đã xóa Máy Xay Bột, Máy Ép Mía, Máy Phô Mai khỏi Cửa Hàng.\n" +
            "3. Đã gom và sắp xếp toàn bộ vật phẩm Trang Trí (Đèn, Lửa, Cây, Bụi, Hoa, Hàng Rào...) theo hàng lối chuẩn đẹp.\n" +
            "4. Đã lưu scene SCN_Farm.", "Tuyệt vời!");
    }

    private static int GetDecorPriorityScore(string name)
    {
        string n = name.ToLower();
        if (n.Contains("đèn") || n.Contains("cột đèn")) return 1;
        if (n.Contains("củi") || n.Contains("lửa") || n.Contains("bếp")) return 2;
        if (n.Contains("cây") || n.Contains("bụi")) return 3;
        if (n.Contains("rào") || n.Contains("hàng rào")) return 4;
        if (n.Contains("hoa") || n.Contains("chậu")) return 5;
        if (n.Contains("ghế") || n.Contains("bàn")) return 6;
        if (n.Contains("giếng") || n.Contains("hồ") || n.Contains("nước")) return 7;
        return 10;
    }
}
#endif
