using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;


public static class InputBridge
{
    // =========================================================================
    public static Vector2 PointerPosition
    {
        get
        {
            if (IsTouchActive)
                return Touchscreen.current.primaryTouch.position.ReadValue();

            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();

            return Vector2.zero;
        }
    }

    // =========================================================================
    // Press State (1 frame)
    // =========================================================================

    /// <summary>True đúng frame đầu tiên ngón/chuột chạm xuống.</summary>
    public static bool IsPointerDownThisFrame
    {
        get
        {
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return true;

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            return false;
        }
    }

    /// <summary>True khi đang giữ ngón/chuột xuống.</summary>
    public static bool IsPointerHeld
    {
        get
        {
            if (IsTouchActive) return true;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            return false;
        }
    }

    /// <summary>True đúng frame ngón/chuột nhấc lên.</summary>
    public static bool IsPointerUpThisFrame
    {
        get
        {
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
                return true;

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasReleasedThisFrame)
                return true;

            return false;
        }
    }

    // =========================================================================
    // UI Overlap — an toàn cho cả Mouse và Touch
    // =========================================================================

    /// <summary>
    /// True khi con trỏ hiện tại đang nằm trên UI element có raycasts bật.
    /// Dùng pointer ID chính xác để EventSystem không bị nhầm giữa mouse và touch.
    /// </summary>
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Touch path: EventSystem dùng ID âm cho touch → -(touchId + 1)
        if (IsTouchActive)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            return EventSystem.current.IsPointerOverGameObject(-(touchId + 1));
        }

        // Mouse path: pointer ID mặc định (-1)
        return EventSystem.current.IsPointerOverGameObject();
    }

    // =========================================================================
    // World Space Conversion
    // =========================================================================

    /// <summary>Chuyển PointerPosition sang world-space 2D. Cần truyền camera vào.</summary>
    public static Vector2 PointerWorldPosition(Camera cam)
    {
        if (cam == null) return Vector2.zero;
        Vector3 world = cam.ScreenToWorldPoint(
            new Vector3(PointerPosition.x, PointerPosition.y, 0f));
        return new Vector2(world.x, world.y);
    }

    // =========================================================================
    // Private helpers
    // =========================================================================

    /// <summary>True khi có ngón đang chạm màn hình (touch, không phải hover).</summary>
    private static bool IsTouchActive =>
        Touchscreen.current != null &&
        Touchscreen.current.primaryTouch.press.isPressed;
}
