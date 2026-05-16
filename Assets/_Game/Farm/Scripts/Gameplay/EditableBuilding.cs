using UnityEngine;

/// <summary>
/// Gắn lên công trình cần cho phép di chuyển trong Edit Mode.
/// Yêu cầu giữ ngón tay / chuột 0.3s mới kích hoạt — tránh nhầm với tap thường.
/// Nếu ngón tay di chuyển quá 15px trong lúc giữ thì xem là scroll → huỷ.
/// Hoạt động trên cả Mobile (Touch) và PC (Mouse) vì Unity ánh xạ touch → mouse.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EditableBuilding : MonoBehaviour
{
    [Header("Footprint")]
    /// <summary>Thảm xanh nằm dưới chân công trình — gắn prefab con vào đây trong Inspector.</summary>
    public GameObject footprintVisual;

    /// <summary>EditModeManager gọi hàm này để bật/tắt thảm xanh đồng loạt.</summary>
    public void SetFootprintActive(bool state)
    {
        if (footprintVisual != null)
            footprintVisual.SetActive(state);
    }

    private const float HoldThreshold     = 0.3f;   // giây cần giữ để nhấc
    private const float DragCancelPixels  = 15f;    // pixel lệch để coi là scroll

    private float   holdTimer;
    private bool    isPressing;
    private bool    alreadyTriggered;
    private Vector2 pressStartScreenPos;

    // ── Ghi nhận bắt đầu nhấn ────────────────────────────────────────────────
    private void OnMouseDown()
    {
        // BẮT BUỘC: chỉ bắt đầu đếm giờ khi Edit Mode đang bật
        if (!EditModeManager.IsEditMode) return;

        isPressing          = true;
        alreadyTriggered    = false;
        holdTimer           = 0f;
        pressStartScreenPos = Input.mousePosition;
    }

    // ── Nhả tay → reset ───────────────────────────────────────────────────────
    private void OnMouseUp()
    {
        isPressing = false;
        holdTimer  = 0f;
    }

    // ── Đếm giờ giữ, huỷ nếu ngón tay lướt ──────────────────────────────────
    private void Update()
    {
        if (!isPressing || alreadyTriggered) return;

        // Edit Mode bị tắt giữa chừng → huỷ ngay, không nhấc nhà
        if (!EditModeManager.IsEditMode)
        {
            isPressing = false;
            holdTimer  = 0f;
            return;
        }

        // Huỷ nếu ngón tay / chuột đã di chuyển (scroll hoặc swipe)
        if (Vector2.Distance((Vector2)Input.mousePosition, pressStartScreenPos) > DragCancelPixels)
        {
            isPressing = false;
            holdTimer  = 0f;
            return;
        }

        holdTimer += Time.deltaTime;
        if (holdTimer >= HoldThreshold)
        {
            alreadyTriggered = true;
            isPressing       = false;
            PlacementManager.Instance?.StartEditBuilding(this);
        }
    }
}
