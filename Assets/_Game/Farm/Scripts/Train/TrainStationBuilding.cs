using UnityEngine;

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
    }

    // ─── World-space click (BoxCollider2D + Physics2D.OverlapPoint) ─────────────
    // Giống hệt TrainWagonSlot.cs — phải đặt Z = nearClipPlane trước ScreenToWorldPoint.

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z       = Camera.main.nearClipPlane;
        Vector2 worldPos    = Camera.main.ScreenToWorldPoint(mouseScreen);

        if (_col == null || !_col.OverlapPoint(worldPos)) return;

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
