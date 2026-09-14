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

        if (FarmInputLock.IsCookingMode)
            return;

        // Tuyệt đối không mở Seed Popup / logic trồng trọt khi Edit Mode đang bật
        // hoặc đang kéo/đặt công trình — hai hệ thống này không được dẫm chân nhau.
        if (EditModeManager.IsEditMode || PlacementManager.IsPlacingNewObject)
        {
            return;
        }

        // Không xử lý trong khi đang kéo hạt giống hoặc liềm
        if (FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle)
        {
            return;
        }

        // Không xử lý plot khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
        {
            return;
        }

        if (OrderBoardPopupUI.AnyOpen)
        {
            return;
        }

        // Nếu đang bấm UI thì không xử lý world plot.
        if (IsPointerOverUI())
        {
            return;
        }

        if (mainCamera == null || FarmManager.Instance == null)
            return;

        Vector2 screenPos = GetPointerScreenPosition();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;

        // Quét tất cả collider tại điểm bấm để tìm PlotController,
        // tránh bị các collider khác (nhà, nhân vật, cây cối) đè lên nuốt mất click
        PlotController plot = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, 25f, plotMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            var p = hits[i].GetComponent<PlotController>() ?? hits[i].GetComponentInParent<PlotController>();
            if (p != null)
            {
                plot = p;
                break;
            }
        }

        if (plot == null)
        {
            return;
        }

        plot.HandlePlotClick();
    }

    // Kiểm tra frame hiện tại có vừa tap/click hay không.
    private bool IsPointerDownThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (Input.GetMouseButtonDown(0))
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        return false;
    }

    // Lấy tọa độ con trỏ hiện tại theo touch hoặc mouse.
    private Vector2 GetPointerScreenPosition()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return Mouse.current.position.ReadValue();

        if (Input.GetMouseButtonDown(0))
            return Input.mousePosition;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Input.mousePosition;
    }

    // Check pointer hiện tại có đang nằm trên UI thật (GraphicRaycaster) không.
    // TUYỆT ĐỐI KHÔNG dùng EventSystem.IsPointerOverGameObject() vì Physics2DRaycaster
    // trên Main Camera bắt luôn Collider2D của chính ô đất => tự chặn mình.
    private bool IsPointerOverUI()
    {
        return FarmInputLock.ConTroTrenUiThat();
    }
}
