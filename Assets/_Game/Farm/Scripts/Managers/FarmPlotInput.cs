using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FarmPlotInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask plotMask = ~0;

    // Cache camera chính nếu chưa gán tay trong Inspector.
    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    // Bắt click/tap ngoài world rồi forward vào PlotController tương ứng.
    private void Update()
    {
        if (!IsPointerDownThisFrame())
            return;

        // Tuyệt đối không mở Seed Popup / logic trồng trọt khi Edit Mode đang bật
        // hoặc đang kéo/đặt công trình — hai hệ thống này không được dẫm chân nhau.
        if (EditModeManager.IsEditMode || PlacementManager.IsPlacingNewObject)
        {
            Debug.Log("[PlotClick] ignored — EditMode or PlacingNewObject active");
            return;
        }

        if (FarmInputLock.BlockMapPan)
        {
            Debug.Log("[PlotClick] ignored — FarmInputLock.BlockMapPan=true");
            return;
        }

        // Không xử lý trong khi đang kéo hạt giống
        if (FarmInputLock.IsDraggingSeed)
        {
            Debug.Log("[PlotClick] ignored — IsDraggingSeed=true");
            return;
        }

        // Không xử lý plot khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            Debug.Log("[PlotClick] ignored — IsAnyPopupOpen=true");
            return;
        }

        // Nếu đang bấm UI thì không xử lý world plot.
        if (IsPointerOverUI())
        {
            Debug.Log("[PlotClick] ignored — IsPointerOverUI=true" +
                      $" | topUI={InputBridge.GetTopUINameUnderPointer()}" +
                      $" | IsSeedPopupOpen={FarmInputLock.IsSeedPopupOpen}" +
                      $" | IsDraggingSeed={FarmInputLock.IsDraggingSeed}");
            return;
        }

        if (mainCamera == null || FarmManager.Instance == null)
            return;

        Vector2 screenPos = GetPointerScreenPosition();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        const float kTouchRadius = 0.08f;
        Collider2D hit = Physics2D.OverlapCircle(worldPos, kTouchRadius, plotMask);
        if (hit == null)
        {
            Debug.Log($"[PlotClick] no collider hit at worldPos={worldPos}");
            return;
        }

        PlotController plot = hit.GetComponent<PlotController>();
        if (plot == null)
            plot = hit.GetComponentInParent<PlotController>();

        if (plot == null)
        {
            Debug.Log($"[PlotClick] hit '{hit.name}' — no PlotController found");
            return;
        }

        Debug.Log($"[PlotClick] opening handler | plot={plot.PlotId} IsEmpty={plot.IsEmpty}" +
                  $" | IsDraggingSeed={FarmInputLock.IsDraggingSeed}" +
                  $" | IsSeedPopupOpen={FarmInputLock.IsSeedPopupOpen}");

        plot.HandlePlotClick();
    }

    // Kiểm tra frame hiện tại có vừa tap/click hay không.
    private bool IsPointerDownThisFrame()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    // Lấy tọa độ con trỏ hiện tại theo touch hoặc mouse.
    private Vector2 GetPointerScreenPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    // Check pointer hiện tại có đang nằm trên UI không.
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.isPressed)
            {
                // EventSystem dùng pointer ID âm cho touch: -(touchId + 1)
                int pointerId = -(touch.touchId.ReadValue() + 1);
                return EventSystem.current.IsPointerOverGameObject(pointerId);
            }
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}
