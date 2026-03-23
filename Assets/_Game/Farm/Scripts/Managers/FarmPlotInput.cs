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

        // Nếu đang bấm UI thì không xử lý world plot.
        if (IsPointerOverUI())
            return;

        if (mainCamera == null || FarmManager.Instance == null)
            return;

        Vector2 screenPos = GetPointerScreenPosition();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(worldPos, plotMask);
        if (hit == null)
            return;

        PlotController plot = hit.GetComponent<PlotController>();
        if (plot == null)
            plot = hit.GetComponentInParent<PlotController>();

        if (plot == null)
            return;

        // Gọi qua handler của plot để toàn bộ luồng click thống nhất một chỗ.
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
        if (Touchscreen.current != null)
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
                return EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue());
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}