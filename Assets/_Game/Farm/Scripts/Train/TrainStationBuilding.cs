using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gáº¯n lÃªn GameObject gataulua (nhÃ  ga, world-space, KHÃ”NG pháº£i UI).
/// Khi ngÆ°á»i chÆ¡i click vÃ o nhÃ  ga trong lÃºc tÃ u Ä‘ang Ä‘i/vá» â†’ toggle Popup_train.
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
            Debug.LogError("[Station] KhÃ´ng tÃ¬m tháº¥y TrainProcessPopupUI! KÃ©o Popup_train vÃ o Inspector.");
    }

    // â”€â”€â”€ World-space click (BoxCollider2D + Physics2D.OverlapPoint) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Giá»‘ng há»‡t TrainWagonSlot.cs â€” pháº£i Ä‘áº·t Z = nearClipPlane trÆ°á»›c ScreenToWorldPoint.

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

        // KhÃ´ng má»Ÿ khi Edit Mode Ä‘ang báº­t
        if (EditModeManager.IsEditMode) return;

        // KhÃ´ng má»Ÿ khi Ä‘ang cÃ³ popup khÃ¡c má»Ÿ
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        HandleClick();
    }

  private void HandleClick()
{
    
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
