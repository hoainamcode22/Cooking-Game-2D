using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gắn lên GameObject gataulua (nhà ga, world-space, KHÔNG phải UI).
/// Khi người chơi click vào nhà ga trong lúc tàu đang đi/về → toggle Popup_train.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(PermanentBuilding))]
public class TrainStationBuilding : MonoBehaviour
{
    [SerializeField] private TrainProcessPopupUI processPopup;

    private BoxCollider2D _col;

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();

        if (processPopup == null)
            processPopup = FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);
        if (processPopup == null)
            Debug.LogError("[Station] Không tìm thấy TrainProcessPopupUI! Kéo Popup_train vào Inspector.");
    }

    // ─── World-space click (BoxCollider2D + Physics2D.OverlapPoint) ─────────────
    // Giống hệt TrainWagonSlot.cs — phải đặt Z = nearClipPlane trước ScreenToWorldPoint.

    void Update()
    {
        bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        if (!clicked) return;
        if (FarmInputLock.BlockMapPan) return;
        if (Camera.main == null) return;

        Vector2 screenPos = InputBridge.PointerPosition;
        Vector2 worldPos  = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        if (_col == null || !_col.OverlapPoint(worldPos)) return;

        // Không mở khi Edit Mode đang bật
        if (EditModeManager.IsEditMode) return;

        // Không mở khi đang có popup khác mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        HandleClick();
    }

  private void HandleClick()
{
    Debug.Log("[Station] HandleClick called!");
    
    if (processPopup == null)
    {
        Debug.LogError("[Station] processPopup == null!");
        return;
    }

    if (processPopup.IsVisible)
        processPopup.Hide();
    else
    {
        float remaining = TrainManager.Instance != null
            ? TrainManager.Instance.TripRemainingTime
            : 0f;
        processPopup.Show(remaining);
    }
}
}
