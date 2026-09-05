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
/// [VÒNG 17] Con số 122 đã LỖI THỜI — bảng lớp UI nay tập trung ở UILayers.cs
/// (World 0 · HUD 100 · Panel 200 · Tutorial 250 · Popup 300 · PopupCaoCap 400).
/// Tool này giữ lại để sửa nhanh một mình Canvas_MarketPopup, nhưng lấy số TỪ BẢNG
/// (UILayers.Panel + 20 = 220) để không phá kết quả của UILayerApplyTool.
/// Chạy nhiều lần an toàn.
/// </summary>
public static class FixMarketLayerTool
{
    // [VÒNG 17] Lấy từ bảng lớp chung, KHÔNG hardcode nữa.
    private static int OrderMoi => UILayers.Panel + 2 * UILayers.BuocTrongLop;   // = 220

    [MenuItem("Tools/Farm/UI/Cho: Dua ve dung lop (UILayers.Panel + 20)")]
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
