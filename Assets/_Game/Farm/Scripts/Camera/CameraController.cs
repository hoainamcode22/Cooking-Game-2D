using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;                      // New Input System
using UnityEngine.InputSystem.EnhancedTouch;        // Enhanced Touch API cho mobile
using InputTouch = UnityEngine.InputSystem.EnhancedTouch.Touch; // alias tránh xung đột tên

/// <summary>
/// Gắn lên Main Camera.
/// Hỗ trợ kéo map (1 ngón / chuột trái) và zoom (pinch / scroll wheel).
/// Giới hạn vùng camera qua bounds, bỏ qua input khi chạm vào UI.
/// Dùng Unity New Input System (UnityEngine.InputSystem).
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── Tham số công khai ───────────────────────────────────────────────
    [Header("Pan / Zoom")]
    public float panSpeed  = 3f;   // Tốc độ kéo map
    public float zoomSpeed = 0.5f; // Tốc độ zoom

    [Header("Zoom Limits")]
    public float minSize     = 400f;  // Camera orthographic size nhỏ nhất
    public float maxSize     = 1500f; // Camera orthographic size lớn nhất
    public float defaultSize = 750f;  // Size mặc định khi khởi động

    [Header("Smooth Damp")]
    [SerializeField] private float panSmoothTime  = 0.12f; // Thời gian giảm tốc khi thả tay (pan)
    [SerializeField] private float zoomSmoothTime = 0.1f;  // Thời gian giảm tốc khi thả tay (zoom)

    [Header("Drag Detection")]
    [SerializeField] private float dragThreshold = 40f; // Pixel tối thiểu phải di chuyển để tính là drag (không phải tap)

    [Header("Bounds (minX, maxX, minY, maxY)")]
    public Vector4 bounds = new Vector4(-50f, 50f, -50f, 50f); // Giới hạn di chuyển camera

    // ── Biến nội bộ ─────────────────────────────────────────────────────
    private Camera cam;

    // Pan
    private Vector3 panVelocity    = Vector3.zero; // Vận tốc hiện tại (smooth damp)
    private Vector3 targetPosition;               // Vị trí đích của camera
    private Vector3 lastPointerWorld;             // Vị trí world của pointer frame trước
    private bool    isDragging;                   // Đang thực sự kéo (sau khi vượt dragThreshold)

    // Mouse drag detection — phân biệt tap (click object) vs drag (pan camera)
    private Vector2 pressStartScreenPos;  // Toạ độ màn hình lúc nhấn chuột xuống
    private bool    pressHeld;            // Chuột đang giữ nhưng chưa đủ pixel để thành drag

    // Touch drag detection — tương tự cho mobile 1 ngón
    private Vector2 touchStartScreenPos; // Toạ độ màn hình lúc ngón tay chạm
    private bool    touchHeld;           // Ngón đang giữ nhưng chưa đủ pixel để thành drag

    // Zoom
    private float targetSize;       // Orthographic size đích
    private float zoomVelocity;     // Vận tốc zoom (smooth damp)
    private float lastPinchDist;    // Khoảng cách 2 ngón frame trước (pinch)

    // ────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Bật Enhanced Touch API — bắt buộc để dùng InputTouch.activeTouches trên mobile
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        // Tắt khi object bị disable để tránh memory leak
        EnhancedTouchSupport.Disable();
    }

    private void Awake()
    {
        cam                  = GetComponent<Camera>();
        cam.orthographicSize = defaultSize;
        targetSize           = defaultSize;
        targetPosition       = transform.position;
    }

    private void Start()
    {
        // Đặt vị trí ban đầu của camera
        transform.position = new Vector3(1550f, 690f, -10f);
        targetPosition     = transform.position;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Editor only: scroll chuột zoom trực tiếp, không qua lock hay condition nào.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            // zoom only, do NOT pan — nhưng vẫn block nếu harvest mode đang active
            if (!FarmInputLock.BlockMapZoom)
                targetSize = Mathf.Clamp(targetSize - scroll * zoomSpeed * 100f, minSize, maxSize);
            return; // prevent pan logic from running on this frame
        }

        HandleMouseInput();
#else
        HandleTouchInput();
#endif
        ApplySmoothMovement();
    }

    // ── MOUSE INPUT (Editor / Desktop) ──────────────────────────────────

    /// <summary>
    /// Dispatcher: dùng New Input System chỉ khi nó thực sự nhận được button state.
    /// Trong Unity Simulator, Mouse.current != null nhưng các button event không fire
    /// → fallback sang old Input System (Input.GetMouseButton) vẫn hoạt động đúng.
    /// </summary>
    private void HandleMouseInput()
    {
        // Kiểm tra New Input System có thực sự nhận được button state không
        bool newInputWorking = Mouse.current != null &&
            (Mouse.current.leftButton.isPressed ||
             Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.leftButton.wasReleasedThisFrame ||
             Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f);

        if (newInputWorking)
            HandleMouseNew();
        else
            HandleMouseLegacy();
    }

    /// <summary>
    /// Xử lý chuột qua New Input System (Mouse.current).
    /// Hoạt động trong Game tab và build thực.
    /// </summary>
    private void HandleMouseNew()
    {
        var mouse = Mouse.current; // đã kiểm tra != null ở HandleMouseInput

        // ────── CHECK: Edit Mode ON + Đang drag object? Nếu có → skip toàn bộ pan ──────
        // Logic:
        // - Khi EditMode OFF → ObjectDragHandler disabled → IsDraggingObject = false
        // - Khi EditMode ON + drag object → IsDraggingObject = true → skip pan
        // - Khi EditMode ON + không drag → IsDraggingObject = false → pan bình thường
        if (ObjectDragHandler.IsDraggingObject && EditModeManager.IsEditMode)
        {
            // Reset trạng thái pan
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;

            // Vẫn cho zoom khi đang drag object
            float scroll = mouse.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scroll) > 0.001f)
                targetSize = Mathf.Clamp(targetSize - scroll / zoomSpeed, minSize, maxSize);

            return;
        }

        // ────── CHECK: Popup mở hoặc đang kéo seed/sickle → block pan ──────
        if (FarmInputLock.BlockMapPan)
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;

            // Cho zoom trừ khi harvest mode đang active
            if (!FarmInputLock.BlockMapZoom)
            {
                float scrollBlocked = mouse.scroll.ReadValue().y / 120f;
                if (Mathf.Abs(scrollBlocked) > 0.001f)
                    targetSize = Mathf.Clamp(targetSize - scrollBlocked / zoomSpeed, minSize, maxSize);
            }

            return;
        }

        // Zoom bằng scroll wheel
        // scroll.ReadValue().y trả về pixel thô (120/notch trên Windows)
        // Chia 120 để chuẩn hoá về ±0.1/notch
        float scroll_normal = mouse.scroll.ReadValue().y / 120f;
        if (Mathf.Abs(scroll_normal) > 0.001f)
            targetSize = Mathf.Clamp(targetSize - scroll_normal / zoomSpeed, minSize, maxSize);

        // ── BƯỚC 1: Nhấn chuột xuống → lưu vị trí screen, chưa drag ────
        // Không bắt đầu drag ngay: EventSystem/Physics2DRaycaster vẫn
        // nhận được sự kiện này để xử lý click popup bình thường.
        if (mouse.leftButton.wasPressedThisFrame)
        {
            pressStartScreenPos = mouse.position.ReadValue();
            pressHeld           = true;
        }

        // ── BƯỚC 2: Đang giữ — kiểm tra đã di chuyển đủ dragThreshold chưa ──
        if (pressHeld && !isDragging && mouse.leftButton.isPressed)
        {
            float movedPixels = Vector2.Distance(mouse.position.ReadValue(), pressStartScreenPos);
            if (movedPixels > dragThreshold)
            {
                isDragging       = true;
                lastPointerWorld = ScreenToWorld(mouse.position.ReadValue());
            }
        }

        // ── BƯỚC 3: Đang drag → thực hiện pan ──────────────────────────
        if (mouse.leftButton.isPressed && isDragging)
        {
            Vector3 current  = ScreenToWorld(mouse.position.ReadValue());
            Vector3 delta    = lastPointerWorld - current;
            targetPosition  += delta * panSpeed;
            targetPosition   = ClampToBounds(targetPosition);
            lastPointerWorld = ScreenToWorld(mouse.position.ReadValue());
        }

        // ── BƯỚC 4: Thả chuột → reset toàn bộ trạng thái ───────────────
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
        }
    }

    /// <summary>
    /// Xử lý chuột qua old Input System (UnityEngine.Input).
    /// Fallback khi Mouse.current == null, ví dụ Unity Simulator.
    /// Logic pixel-distance giữ nguyên như HandleMouseNew.
    /// </summary>
    private void HandleMouseLegacy()
    {
        // ────── CHECK: Đang drag object không? Nếu có → skip toàn bộ pan ──────
        if (ObjectDragHandler.IsDraggingObject)
        {
            // Reset trạng thái pan
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;

            // Vẫn cho zoom khi đang drag object
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
                targetSize = Mathf.Clamp(targetSize - scroll / zoomSpeed, minSize, maxSize);

            return;
        }

        // ────── CHECK: Popup mở hoặc đang kéo seed/sickle → block pan ──────
        if (FarmInputLock.BlockMapPan)
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;

            // Cho zoom trừ khi harvest mode đang active
            if (!FarmInputLock.BlockMapZoom)
            {
                float scrollBlocked = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scrollBlocked) > 0.001f)
                    targetSize = Mathf.Clamp(targetSize - scrollBlocked / zoomSpeed, minSize, maxSize);
            }

            return;
        }

        // Zoom bằng scroll wheel
        // Input.GetAxis trả về giá trị đã chuẩn hoá (±0.1/notch), không cần chia 120
        float scroll_normal = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll_normal) > 0.001f)
            targetSize = Mathf.Clamp(targetSize - scroll_normal / zoomSpeed, minSize, maxSize);

        // ── BƯỚC 1: Nhấn chuột xuống → lưu vị trí screen, chưa drag ────
        if (Input.GetMouseButtonDown(0))
        {
            pressStartScreenPos = (Vector2)Input.mousePosition;
            pressHeld           = true;
        }

        // ── BƯỚC 2: Đang giữ — kiểm tra đã di chuyển đủ dragThreshold chưa ──
        if (pressHeld && !isDragging && Input.GetMouseButton(0))
        {
            float movedPixels = Vector2.Distance((Vector2)Input.mousePosition, pressStartScreenPos);
            if (movedPixels > dragThreshold)
            {
                isDragging       = true;
                lastPointerWorld = ScreenToWorld(Input.mousePosition);
            }
        }

        // ── BƯỚC 3: Đang drag → thực hiện pan ──────────────────────────
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 current  = ScreenToWorld(Input.mousePosition);
            Vector3 delta    = lastPointerWorld - current;
            targetPosition  += delta * panSpeed;
            targetPosition   = ClampToBounds(targetPosition);
            lastPointerWorld = ScreenToWorld(Input.mousePosition);
        }

        // ── BƯỚC 4: Thả chuột → reset toàn bộ trạng thái ───────────────
        if (Input.GetMouseButtonUp(0))
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
        }
    }

    // ── TOUCH INPUT (Mobile) ─────────────────────────────────────────────

    private void HandleTouchInput()
    {
        // ────── CHECK: Đang drag object không? Nếu có → skip toàn bộ pan ──────
        if (ObjectDragHandler.IsDraggingObject)
        {
            // Reset trạng thái pan
            isDragging = false;
            touchHeld  = false;
            touchStartScreenPos = Vector2.zero;
            return;
        }

        // ────── CHECK: Popup mở hoặc đang kéo seed/sickle → block pan ──────
        if (FarmInputLock.BlockMapPan)
        {
            isDragging          = false;
            touchHeld           = false;
            touchStartScreenPos = Vector2.zero;
            return;
        }

        // Enhanced Touch API: danh sách ngón đang chạm màn hình
        var activeTouches = InputTouch.activeTouches;
        int touchCount    = activeTouches.Count;

        if (touchCount == 1)
        {
            var t     = activeTouches[0];
            var phase = t.phase;

            // ── BƯỚC 1: Ngón chạm xuống → lưu vị trí screen, chưa drag ──
            // Tap ngắn không di chuyển → EventSystem xử lý popup bình thường.
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                touchStartScreenPos = t.screenPosition;
                touchHeld           = true;
                isDragging          = false; // Reset phòng trường hợp ngón mới
            }
            // ── BƯỚC 2 & 3: Ngón di chuyển ─────────────────────────────
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                // Chưa drag: kiểm tra đã di chuyển đủ dragThreshold pixel chưa
                if (touchHeld && !isDragging)
                {
                    float movedPixels = Vector2.Distance(t.screenPosition, touchStartScreenPos);
                    if (movedPixels > dragThreshold)
                    {
                        isDragging       = true;
                        lastPointerWorld = ScreenToWorld(t.screenPosition);
                    }
                }

                // Đang drag → thực hiện pan
                if (isDragging)
                {
                    Vector3 current  = ScreenToWorld(t.screenPosition);
                    Vector3 delta    = lastPointerWorld - current;
                    targetPosition  += delta * panSpeed;
                    targetPosition   = ClampToBounds(targetPosition);
                    lastPointerWorld = ScreenToWorld(t.screenPosition);
                }
            }
            // ── BƯỚC 4: Ngón nhấc lên → reset trạng thái ────────────────
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                isDragging          = false;
                touchHeld           = false;
                touchStartScreenPos = Vector2.zero;
            }
        }
        else if (touchCount == 2)
        {
            // 2 ngón → pinch zoom, huỷ pan đang có
            isDragging = false;
            touchHeld  = false;

            var t0 = activeTouches[0];
            var t1 = activeTouches[1];

            if (t0.phase == UnityEngine.InputSystem.TouchPhase.Began ||
                t1.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                // Lưu khoảng cách ban đầu giữa 2 ngón
                lastPinchDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
            }
            else
            {
                float currentDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
                if (!FarmInputLock.BlockMapZoom)
                {
                    float delta = lastPinchDist - currentDist;
                    targetSize  = Mathf.Clamp(targetSize + delta * zoomSpeed * 0.01f, minSize, maxSize);
                }
                lastPinchDist = currentDist;
            }
        }
        else
        {
            // Không có ngón nào → dừng kéo, reset
            isDragging = false;
            touchHeld  = false;
        }
    }

    // ── SMOOTH MOVEMENT ──────────────────────────────────────────────────

    private void ApplySmoothMovement()
    {
        // Smooth damp vị trí camera về targetPosition
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref panVelocity, panSmoothTime);

        // Smooth damp zoom về targetSize
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime);
    }

    // ── PUBLIC API ───────────────────────────────────────────────────────

    /// <summary>Cập nhật giới hạn vùng di chuyển của camera.</summary>
    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        bounds = new Vector4(minX, maxX, minY, maxY);
        // Kẹp lại vị trí đích nếu đang nằm ngoài bounds mới
        targetPosition = ClampToBounds(targetPosition);
    }

    // ── HELPERS ──────────────────────────────────────────────────────────

    /// <summary>Chuyển toạ độ màn hình sang world space (z = 0).</summary>
    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 pos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        pos.z = 0f;
        return pos;
    }

    /// <summary>Kẹp vị trí camera trong bounds.</summary>
    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, bounds.x, bounds.y);
        pos.y = Mathf.Clamp(pos.y, bounds.z, bounds.w);
        return pos;
    }

    /// <summary>Kiểm tra con trỏ chuột có đang chạm vào UI không.</summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>Kiểm tra ngón tay (theo finger index) có đang chạm vào UI không.</summary>
    private bool IsPointerOverUI(int fingerIndex)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerIndex);
    }
}