#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ĐƯA POPUP CHỢ LÊN TRÊN HUD — vụ 21/08, phần 2 (phần 1 là FixMarketAlphaTool).
///
/// Bảng tầng canvas của scene (đo bằng F10):
///     Canvas_MarketPopup   50   ← chợ: DUY NHẤT nằm DƯỚI HUD
///     Canvas_HUD          100
///     Canvas_StallPopup   120
///     Canvas_OrderBoardPopup 121
///     Canvas_Popup        150
/// Hậu quả order 50: mở chợ thì avatar/chip vàng/nút cài đặt của HUD đè LÊN popup
/// (nút ✕ chui sau chip vàng), và nền mờ Panel_Dim (đen 62%, có sẵn, đang bật)
/// không phủ được HUD nên nhìn như "không có nền xám" trong khi các popup khác có.
///
/// Tool đặt order = 122 — cùng nhóm popup thường (120-121), trên HUD, dưới Canvas_Popup
/// (150) và popup máy xay (400). Chạy nhiều lần an toàn.
/// </summary>
public static class FixMarketLayerTool
{
    private const int OrderMoi = 122;

    [MenuItem("Tools/Farm/Cho: Dua Len Tren HUD (order 122)")]
    public static void Sua()
    {
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                             FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas || c.name != "Canvas_MarketPopup") continue;

            if (c.sortingOrder == OrderMoi)
            {
                Debug.Log("[FixMarketLayer] Canvas_MarketPopup đã ở order " + OrderMoi + " — không cần sửa.");
                return;
            }

            Undo.RecordObject(c, "Nâng tầng popup chợ");
            int cu = c.sortingOrder;
            c.sortingOrder = OrderMoi;
            EditorUtility.SetDirty(c);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[FixMarketLayer] Canvas_MarketPopup: order " + cu + " → " + OrderMoi +
                      ", ĐÃ LƯU SCENE. Mở chợ giờ sẽ nổi trên HUD, nền đen 62% của Panel_Dim " +
                      "phủ toàn màn hình như các popup khác.");
            return;
        }

        Debug.LogError("[FixMarketLayer] Không tìm thấy Canvas_MarketPopup trong scene đang mở.");
    }
}
#endif
