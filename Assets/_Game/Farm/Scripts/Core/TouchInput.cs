using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lớp đọc input DÙNG CHUNG cho toàn game — game phát hành cho Android/iOS nên
/// NGÓN TAY là đường chính, chuột chỉ để dev test trong Editor.
///
/// Thứ tự ưu tiên của mọi hàm: <b>Touchscreen → Mouse → Input legacy</b>.
///  • Touchscreen trước: trên điện thoại đây là nguồn thật, và <c>Mouse.current</c>
///    thường là NULL nên mọi code chỉ hỏi Mouse sẽ chết câm (đúng lỗi failsafe của
///    PlantDragController).
///  • Mouse thứ hai: Editor/PC của Sếp.
///  • Input legacy cuối: lưới an toàn cho Unity Simulator và cấu hình
///    activeInputHandler = Both (dự án đang dùng Both nên nhánh này luôn có hiệu lực).
///
/// ── QUAN HỆ VỚI InputBridge (ĐỌC TRƯỚC KHI THÊM CODE MỚI) ──────────────────
/// Dự án ĐÃ CÓ <see cref="InputBridge"/> (Managers/InputBridge.cs) làm đúng việc
/// Touchscreen → Mouse, kèm <c>IsPointerOverUI()</c> xử lý pointerId của touch rất
/// đúng. Lớp này KHÔNG viết lại phần đó — nó GỌI THẲNG InputBridge cho mọi đường
/// Input System, và chỉ THÊM 3 thứ InputBridge còn thiếu:
///   1. Tầng dự phòng <c>Input</c> legacy (dự án đặt activeInputHandler = Both):
///      InputBridge trả false/zero khi cả Touchscreen và Mouse đều null — hay gặp
///      trong Device Simulator và vài máy Android đời cũ.
///   2. <c>TouchPhase.Canceled</c>: hệ điều hành huỷ touch (có cuộc gọi đến, kéo
///      notification xuống) KHÔNG bắn "released" — thiếu nhánh này là thao tác kéo
///      kẹt vĩnh viễn, đúng loại bug khó tái hiện nhất trên mobile.
///   3. <see cref="HasTouchscreen"/> để code khác biết đang chạy mobile.
/// ⇒ Có MỘT nguồn sự thật cho phần Input System (InputBridge). Code MỚI nên dùng
/// TouchInput; code cũ đang gọi InputBridge KHÔNG cần sửa (xem RA_SOAT_INPUT_MOBILE.md).
///
/// KHÔNG dùng EventSystem ở đây: lớp này dành cho input THẾ GIỚI (world/gameplay).
/// Nút UI vẫn nên đi qua Button.onClick / IPointerHandler như bình thường
/// (cần biết "ngón có đang trên UI không" thì dùng InputBridge.IsPointerOverUI()).
///
/// Mọi hàm an toàn khi không có thiết bị nào (trả false / Vector2.zero), không NRE.
/// </summary>
public static class TouchInput
{
    /// <summary>Máy có màn hình cảm ứng đang hoạt động? (code khác dùng để biết đang chạy mobile.)</summary>
    public static bool HasTouchscreen
    {
        get
        {
            if (Touchscreen.current != null) return true;
            return Input.touchSupported && Application.isMobilePlatform;
        }
    }

    /// <summary>
    /// Vừa CHẠM XUỐNG / nhấn chuột trong frame này (một lần duy nhất mỗi nhịp chạm).
    /// </summary>
    public static bool TapDownThisFrame()
    {
        if (InputBridge.IsPointerDownThisFrame) return true; // Touchscreen → Mouse

        // Legacy: touch trước, rồi mới chuột (trên mobile Input.GetMouseButtonDown
        // cũng được Unity mô phỏng từ touch — kiểm touch trước cho rõ ràng ý định).
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began) return true;
        if (Input.GetMouseButtonDown(0)) return true;

        return false;
    }

    /// <summary>Vừa NHẤC NGÓN / nhả chuột trong frame này (Ended hoặc Canceled).</summary>
    public static bool TapUpThisFrame()
    {
        if (InputBridge.IsPointerUpThisFrame) return true; // Touchscreen → Mouse

        if (Input.touchCount > 0)
        {
            UnityEngine.TouchPhase phase = Input.GetTouch(0).phase;
            // Canceled cũng phải tính: hệ điều hành hủy touch (gọi đến, notification)
            // mà không tính là "nhấc tay" thì thao tác kéo sẽ KẸT vĩnh viễn.
            if (phase == UnityEngine.TouchPhase.Ended || phase == UnityEngine.TouchPhase.Canceled) return true;
        }
        if (Input.GetMouseButtonUp(0)) return true;

        return false;
    }

    /// <summary>Đang GIỮ ngón/chuột (không tính frame vừa nhấn hay vừa nhả).</summary>
    public static bool IsHolding()
    {
        if (InputBridge.IsPointerHeld) return true; // Touchscreen → Mouse

        if (Input.touchCount > 0)
        {
            UnityEngine.TouchPhase phase = Input.GetTouch(0).phase;
            if (phase == UnityEngine.TouchPhase.Began || phase == UnityEngine.TouchPhase.Moved ||
                phase == UnityEngine.TouchPhase.Stationary) return true;
        }
        if (Input.GetMouseButton(0)) return true;

        return false;
    }

    /// <summary>
    /// Vị trí con trỏ trên MÀN HÌNH (pixel). Không có thiết bị nào → Vector2.zero.
    /// LƯU Ý: sau khi nhấc tay, giá trị này giữ vị trí chạm CUỐI (đúng như
    /// Input.mousePosition trên mobile) — đừng dùng nó để suy "ngón đang ở đâu"
    /// khi không giữ; hỏi <see cref="IsHolding"/> trước.
    /// </summary>
    public static Vector2 PointerScreen()
    {
        // InputBridge trả Vector2.zero khi KHÔNG có thiết bị nào — dùng nó làm dấu
        // hiệu để rơi xuống tầng legacy (toạ độ (0,0) thật sự là góc màn hình, gần
        // như không bao giờ là điểm chạm hợp lệ nên phép kiểm này an toàn).
        Vector2 p = InputBridge.PointerPosition;
        if (p != Vector2.zero) return p;

        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    /// <summary>
    /// Vị trí con trỏ trong THẾ GIỚI (z = 0). cam = null → Camera.main.
    /// Không có camera → Vector2.zero (không NRE, giống GetMouseWorld cũ).
    /// </summary>
    public static Vector2 PointerWorld(Camera cam = null)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector2.zero;

        Vector3 world = cam.ScreenToWorldPoint(PointerScreen());
        return new Vector2(world.x, world.y);
    }
}
