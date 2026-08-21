#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SỬA POPUP CHỢ TÀNG HÌNH — vụ 21/08.
///
/// ══ CHUỖI NGUYÊN NHÂN (đã xác minh bằng F10 + file scene) ══
/// 1. FarmInputLock.SetPopupRaycastBlock() từng gắn CanvasGroup lên `Canvas_MarketPopup`
///    (đúng thiết kế — nó tạo với alpha = 1, về sau chỉ bật/tắt blocksRaycasts).
/// 2. Trong một lần chỉnh scene bằng tay, thanh Alpha của CanvasGroup đó bị kéo về 0
///    và Ctrl+S — file scene lưu cứng `m_Alpha: 0` (component &1803445294).
/// 3. CanvasGroup alpha = 0 làm CẢ CÂY chợ không vẽ nhưng VẪN nhận raycast
///    ⇒ popup mở được, MUA HÀNG được, mà màn hình trống trơn.
///
/// Tool này đặt alpha về 1 và LƯU SCENE. Chạy nhiều lần an toàn; đồng thời rà mọi
/// canvas gốc khác xem còn CanvasGroup nào alpha 0 tương tự không (báo, không tự sửa,
/// vì TransitionCanvas/Tutorial cố ý alpha 0).
/// </summary>
public static class FixMarketAlphaTool
{
    [MenuItem("Tools/Farm/Sua Popup Cho Tang Hinh (alpha 0 - 1)")]
    public static void Sua()
    {
        int daSua = 0;

        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                             FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;

            CanvasGroup cg = c.GetComponent<CanvasGroup>();
            if (cg == null || cg.alpha >= 0.99f) continue;

            if (c.name == "Canvas_MarketPopup")
            {
                Undo.RecordObject(cg, "Sửa alpha popup chợ");
                Debug.Log("[FixMarketAlpha] Canvas_MarketPopup.CanvasGroup.alpha " +
                          cg.alpha.ToString("0.00") + " → 1. Đây chính là lý do popup chợ " +
                          "mở được, mua được, nhưng không hiện gì.");
                cg.alpha = 1f;
                EditorUtility.SetDirty(cg);
                daSua++;
            }
            else
            {
                // Chỉ BÁO: các canvas như TransitionCanvas cố ý để alpha thấp.
                Debug.LogWarning("[FixMarketAlpha] Canvas gốc '" + c.name + "' cũng có " +
                                 "CanvasGroup alpha=" + cg.alpha.ToString("0.00") +
                                 " — kiểm tra xem có cố ý không (tool KHÔNG tự sửa canvas này).");
            }
        }

        if (daSua > 0)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[FixMarketAlpha] ĐÃ SỬA " + daSua + " canvas và LƯU SCENE. " +
                      "Vào Play bấm quầy chợ — popup hiện lại bình thường.");
        }
        else
        {
            Debug.Log("[FixMarketAlpha] Không thấy Canvas_MarketPopup nào alpha < 1 trong scene " +
                      "đang mở — hoặc đã sửa rồi, hoặc bạn đang mở nhầm scene.");
        }
    }
}
#endif
