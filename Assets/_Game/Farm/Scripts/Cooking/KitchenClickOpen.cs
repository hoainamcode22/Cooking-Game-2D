using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phát hiện click chuột/chạm lên tòa nhà Bếp và gọi FarmUIManager.OnClick_GoCooking().
/// Không có guard phức tạp — FarmUIManager tự lo việc kiểm tra trạng thái.
/// </summary>
public class KitchenClickOpen : MonoBehaviour
{
    [SerializeField] private Camera       mainCamera;
    [SerializeField] private Collider2D   targetCollider;

    private void Awake()
    {
        if (mainCamera     == null) mainCamera     = Camera.main;
        if (targetCollider == null) targetCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!GetPointerDown(out Vector2 screenPos)) return;
        if (FarmInputLock.BlockMapPan) return;
        if (mainCamera == null || targetCollider == null)   return;

        Vector2 world = mainCamera.ScreenToWorldPoint(screenPos);
        if (!targetCollider.OverlapPoint(world))            return;

        FarmUIManager.Instance?.OnClick_GoCooking();
    }

    private static bool GetPointerDown(out Vector2 pos)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        pos = default;
        return false;
    }
}
