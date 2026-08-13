using UnityEditor;
using UnityEngine;

/// <summary>
/// Gắn vỏ Shop mẫu 3a. Tách menu riêng khỏi bộ "Thay Áo" cũ vì đây là cách tiếp cận
/// khác hẳn: DỰNG LẠI BỐ CỤC THẺ theo spec (di chuyển object có sẵn), không phải chỉ
/// đổi màu bề mặt.
/// </summary>
public static class ShopSkinTool
{
    [MenuItem("Tools/Farm/Thay Áo Popup/5 · Shop — vỏ mẫu 3a (dựng lại bố cục thẻ)", false, 13)]
    public static void Gan()
    {
        var shop = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            EditorUtility.DisplayDialog("Vỏ Shop", "Không thấy ShopManager trong scene.", "OK");
            return;
        }

        // Gỡ applier màu cũ trên shop nếu còn — hai lớp vỏ chồng nhau chỉ tổ rối.
        var cu = shop.GetComponent<PopupSkinApplier>();
        if (cu != null) Undo.DestroyObjectImmediate(cu);

        if (shop.GetComponent<ShopSkin>() == null)
            Undo.AddComponent<ShopSkin>(shop.gameObject);

        EditorUtility.SetDirty(shop);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        Debug.Log("[VỏShop] ✅ Đã gắn ShopSkin lên '" + shop.gameObject.name + "'.\n" +
                  "→ Ctrl+S, Play, mở Shop: khung gỗ + tab + thẻ mẫu 3a (tên trên, đĩa tròn " +
                  "kem, − SỐ +, nút giá xanh/xanh dương). Bỏ tick 'Bật Áo' là về vỏ cũ.");
    }
}
