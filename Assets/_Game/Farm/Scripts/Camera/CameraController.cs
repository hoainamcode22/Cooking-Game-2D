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
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // ── Tham số công khai ───────────────────────────────────────────────
    [Header("Pan / Zoom")]
    public float panSpeed  = 3f;   // Tốc độ kéo map
    [Tooltip("GIỮ LẠI cho tương thích Inspector cũ — KHÔNG còn dùng cho zoom.\n" +
             "Zoom nay dùng zoomStepPercent / pinchSensitivity ở dưới.")]
    public float zoomSpeed = 0.5f;

    // ── ZOOM (viết lại: nhân theo % thay vì cộng tuyến tính) ─────────────
    // LÝ DO SỬA: code cũ dùng `targetSize -= scroll / zoomSpeed` (cộng tuyến tính).
    //   • Chuột New Input : scroll/120 = 1.0/nấc → 1.0/0.5 = 2 unit/nấc
    //                       → cần ~275 nấc để đi từ 950 lên 1500.
    //   • Chuột Legacy    : GetAxis = 0.1/nấc → 0.2 unit/nấc → ~2.750 nấc (lệch 10× so với trên).
    //   • Pinch           : delta * 0.5 * 0.01 = delta * 0.005
    //                       → cần ~110.000 pixel vuốt ngón. Bất khả thi.
    // CÁCH MỚI: zoom nhân (multiplicative) — mỗi nấc đổi size theo TỈ LỆ %,
    //   giống Township/Hay Day. Cảm giác zoom đều ở mọi mức, và số nấc để đi
    //   hết tầm là hằng số, không phụ thuộc dải min/max.
    [Header("Zoom — Tốc độ")]
    [Tooltip("Mỗi nấc lăn chuột đổi bao nhiêu % kích thước. 0.12 = 12%/nấc (~8-12 nấc là hết tầm).")]
    [Range(0.02f, 0.40f)]
    public float zoomStepPercent = 0.12f;

    [Tooltip("Độ nhạy pinch 2 ngón. 1 = chuẩn. Tăng nếu thấy pinch chậm.")]
    [Range(0.2f, 4f)]
    public float pinchSensitivity = 1f;

    [Tooltip("Zoom hướng về con trỏ / tâm 2 ngón thay vì tâm màn hình (giống Township).")]
    public bool zoomTowardCursor = true;

    [Header("Zoom Limits — PLAYER (giới hạn người chơi thật)")]
    public float minSize     = 400f;  // Camera orthographic size nhỏ nhất
    public float maxSize     = 1500f; // Camera orthographic size lớn nhất
    public float defaultSize = 750f;  // Size mặc định khi khởi động

    // ── DEV MODE ─────────────────────────────────────────────────────────
    // Khi bật: nới rộng dải zoom để dev quan sát/ debug toàn bản đồ.
    // Người chơi thật KHÔNG bao giờ chạm tới dải này.
    [Header("Zoom Limits — DEV MODE (F2 để bật/tắt)")]
    [Tooltip("Bật dev mode ngay từ khi chạy. NHỚ TẮT trước khi build bản phát hành.")]
    public bool  devModeOnStart = false;
    public float devMinSize     = 200f;
    public float devMaxSize     = 6000f;

    [Tooltip("Phím bật/tắt Dev Mode.")]
    public Key devModeToggleKey = Key.F2;
    [Tooltip("Phím zoom ra xem toàn bản đồ.")]
    public Key fitMapKey        = Key.F1;

    private bool _devMode;
    /// <summary>Dev mode đang bật? (nới dải zoom)</summary>
    public bool IsDevMode => _devMode;

    /// <summary>Giới hạn zoom hiện hành — đổi theo dev mode.</summary>
    public float ActiveMinSize => _devMode ? Mathf.Min(devMinSize, minSize) : minSize;
    public float ActiveMaxSize => _devMode ? Mathf.Max(devMaxSize, maxSize) : maxSize;

    [Header("Smooth Damp")]
    [SerializeField] private float panSmoothTime  = 0.08f; // Thời gian giảm tốc khi thả tay (pan mượt mà)
    [SerializeField] private float zoomSmoothTime = 0.1f;  // Thời gian giảm tốc khi thả tay (zoom)

    [Header("Cinematic (Tutorial)")]
    [SerializeField] private float cinematicSmoothTime = 0.45f; // Mượt khi tutorial lia/zoom camera
    private bool _cinematicActive;                              // True khi tutorial đang điều khiển camera
    public  bool IsCinematic => _cinematicActive;

    [Header("Drag Detection")]
    [SerializeField] private float dragThreshold = 8f; // Pixel tối thiểu phải di chuyển để tính là drag (nhạy bén mượt mà)

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
        cam            = GetComponent<Camera>();
        _devMode       = devModeOnStart;
        targetPosition = transform.position;

        // Kẹp defaultSize vào dải hợp lệ phòng khi Inspector đặt sai
        float startSize      = Mathf.Clamp(defaultSize, ActiveMinSize, ActiveMaxSize);
        cam.orthographicSize = startSize;
        targetSize           = startSize;

        if (_devMode)
            Debug.LogWarning("[CameraController] DEV MODE đang BẬT (devModeOnStart). Nhớ TẮT trước khi build phát hành!");
    }

    private void Start()
    {
        // Đặt vị trí ban đầu của camera
        transform.position = new Vector3(1550f, 690f, -10f);
        targetPosition     = transform.position;
    }

    private void Update()
    {
        // Phím tắt dev chạy cả trong cinematic để không bao giờ bị kẹt
        HandleDevHotkeys();

        // Khi tutorial đang điều khiển camera (cinematic) → bỏ qua input người chơi,
        // chỉ chạy smooth movement để camera tự lia tới target. Tránh tranh chấp.
        if (!_cinematicActive)
        {
            // Runtime detection: nếu có touch thật (mobile/simulator) → dùng touch path.
            // Nếu không → dùng mouse path (Editor desktop / standalone).
            if (Touchscreen.current != null && InputTouch.activeTouches.Count > 0)
                HandleTouchInput();
            else
                HandleMouseInput();
        }

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
            ApplyZoomStep(ReadMouseScrollSteps(), mouse.position.ReadValue());

            return;
        }

        // ────── CHECK: Đang giữ vật thể mới từ Shop → block pan ──────
        if (PlacementManager.IsPlacingNewObject)
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
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
                ApplyZoomStep(ReadMouseScrollSteps(), mouse.position.ReadValue());

            return;
        }

        // Zoom bằng scroll wheel (zoom nhân theo %, hướng về con trỏ)
        ApplyZoomStep(ReadMouseScrollSteps(), mouse.position.ReadValue());

        // ── BƯỚC 1: Nhấn chuột xuống → lưu vị trí screen, chưa drag ────
        // Không bắt đầu drag nếu con trỏ đang ở trên UI element
        if (ConTroDangTrenUI(mouse.position.ReadValue()))
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
            return;
        }

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
            ApplyZoomStep(ReadLegacyScrollSteps(), (Vector2)Input.mousePosition);

            return;
        }

        // ────── CHECK: Đang giữ vật thể mới từ Shop → block pan ──────
        if (PlacementManager.IsPlacingNewObject)
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
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
                ApplyZoomStep(ReadLegacyScrollSteps(), (Vector2)Input.mousePosition);

            return;
        }

        // Zoom bằng scroll wheel (cùng công thức với đường New Input)
        ApplyZoomStep(ReadLegacyScrollSteps(), (Vector2)Input.mousePosition);

        // ── BƯỚC 1: Nhấn chuột xuống → lưu vị trí screen, chưa drag ────
        if (ConTroDangTrenUI((Vector2)Input.mousePosition))
        {
            isDragging          = false;
            pressHeld           = false;
            pressStartScreenPos = Vector2.zero;
            return;
        }

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

        // ────── CHECK: Đang giữ vật thể mới từ Shop → block pan ──────
        if (PlacementManager.IsPlacingNewObject)
        {
            isDragging          = false;
            touchHeld           = false;
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

                // Bảo vệ: 2 ngón chập vào nhau → tỉ lệ nổ vô cực
                const float MIN_PINCH_DIST = 20f;
                if (!FarmInputLock.BlockMapZoom &&
                    lastPinchDist > MIN_PINCH_DIST && currentDist > MIN_PINCH_DIST)
                {
                    // PINCH THEO TỈ LỆ, không theo pixel.
                    // Code cũ: delta_pixel * 0.005 → cần ~110.000 pixel vuốt = bất khả thi.
                    // Giờ: ngón dang rộng gấp đôi ⇒ zoom vào đúng gấp đôi. Độc lập DPI màn hình.
                    float ratio = lastPinchDist / currentDist;   // >1 = 2 ngón chụm lại = zoom ra

                    // Quy tỉ lệ về "số nấc" để dùng chung ApplyZoomStep với chuột.
                    // steps = -log(ratio) / log(1 + zoomStepPercent)
                    float steps = -Mathf.Log(ratio) / Mathf.Log(1f + zoomStepPercent);

                    // Tâm 2 ngón làm điểm neo zoom (giống Township)
                    Vector2 pinchCenter = (t0.screenPosition + t1.screenPosition) * 0.5f;

                    ApplyZoomStep(steps * pinchSensitivity, pinchCenter);
                }
                lastPinchDist = currentDist;
            }
        }
        else
        {
            // 0 ngón, hoặc 3+ ngón → dừng kéo, reset.
            // PHẢI reset lastPinchDist: nếu không, khi nhấc ngón thứ 3 xuống còn 2 ngón
            // sẽ không có phase Began → dùng lastPinchDist cũ → zoom giật một nhịp.
            isDragging    = false;
            touchHeld     = false;
            lastPinchDist = 0f;
        }
    }

    // ── SMOOTH MOVEMENT ──────────────────────────────────────────────────

    private void ApplySmoothMovement()
    {
        float posTime  = _cinematicActive ? cinematicSmoothTime : panSmoothTime;
        float zoomTime = _cinematicActive ? cinematicSmoothTime : zoomSmoothTime;

        // Smooth damp vị trí camera về targetPosition
        transform.position = Vector3.SmoothDamp(
            transform.position, targetPosition, ref panVelocity, posTime);

        // Smooth damp zoom về targetSize
        float prevSize = cam.orthographicSize;
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, targetSize, ref zoomVelocity, zoomTime);

        if (prevSize > 550f && cam.orthographicSize <= 480f)
        {
            CheckAndTriggerNearbyCharacterGreeting();
        }
    }

    private float _lastZoomGreetTime = -999f;
    private void CheckAndTriggerNearbyCharacterGreeting()
    {
        if (Time.unscaledTime - _lastZoomGreetTime < 3.0f) return;
        _lastZoomGreetTime = Time.unscaledTime;

        var charReactions = Object.FindObjectsByType<CharacterVoiceReaction>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (charReactions != null && charReactions.Length > 0)
        {
            Vector3 camPos = transform.position;
            foreach (var cr in charReactions)
            {
                if (Vector2.Distance(camPos, cr.transform.position) < 350f)
                {
                    cr.TryGreet();
                    break;
                }
            }
        }
        else
        {
            AudioManager.Instance?.PlayCharacterGreet();
        }
    }

    // ── PUBLIC API ───────────────────────────────────────────────────────

    /// <summary>Cập nhật giới hạn vùng di chuyển của camera.</summary>
    public void SetBounds(float minX, float maxX, float minY, float maxY)
    {
        bounds = new Vector4(minX, maxX, minY, maxY);
        // Kẹp lại vị trí đích nếu đang nằm ngoài bounds mới
        targetPosition = ClampToBounds(targetPosition);
    }

    /// <summary>Vị trí camera hiện tại (cho tutorial save/restore).</summary>
    public Vector3 CurrentPosition => transform.position;

    /// <summary>Orthographic size hiện tại.</summary>
    public float CurrentSize => cam != null ? cam.orthographicSize : defaultSize;

    /// <summary>
    /// Tutorial gọi: lia + zoom camera tới 1 điểm world. Khi lockInput=true sẽ khoá
    /// pan/zoom của người chơi cho tới khi EndCinematic(). CameraController là CHỦ DUY
    /// NHẤT điều khiển camera → không còn tranh chấp với script tutorial.
    /// </summary>
    public void CinematicFocus(Vector3 worldPos, float orthoSize, bool lockInput = true)
    {
        targetPosition   = ClampToBounds(new Vector3(worldPos.x, worldPos.y, transform.position.z));
        targetSize       = Mathf.Clamp(orthoSize, ActiveMinSize, ActiveMaxSize);
        _cinematicActive = lockInput;
    }

    /// <summary>Kết thúc cinematic, trả quyền pan/zoom cho người chơi.</summary>
    public void EndCinematic() => _cinematicActive = false;

    // ── ĐỌC SCROLL → SỐ NẤC (chuẩn hoá 2 hệ input về cùng đơn vị) ────────

    /// <summary>
    /// New Input System: `scroll.ReadValue().y` = 120 mỗi nấc trên Windows.
    /// (Comment cũ trong file ghi "±0.1/notch" là SAI — đó là giá trị của Legacy.)
    /// </summary>
    private float ReadMouseScrollSteps()
    {
        if (Mouse.current == null) return 0f;
        float raw = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(raw) < 0.01f) return 0f;

        // Đa nền tảng: Windows trả 120/nấc, còn macOS/WebGL/trackpad trả giá trị nhỏ (~0.1–1).
        // Nếu chia 120 cứng thì trên mac/trackpad zoom sẽ đứng im.
        float notches = Mathf.Abs(raw) > 10f ? raw / 120f : raw;

        // Kẹp ±3 nấc/frame chống chuột free-spin nhảy vọt.
        return Mathf.Clamp(notches, -3f, 3f);
    }

    /// <summary>
    /// Legacy Input (Unity Simulator): `GetAxis("Mouse ScrollWheel")` = ±0.1 mỗi nấc.
    /// Nhân 10 để ra cùng đơn vị "nấc" như ReadMouseScrollSteps().
    /// → Sửa được lỗi cũ: 2 đường chuột lệch nhau đúng 10 lần.
    /// </summary>
    private float ReadLegacyScrollSteps()
    {
        float raw = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(raw) < 0.0001f) return 0f;
        return Mathf.Clamp(raw * 10f, -3f, 3f);
    }

    // ── ZOOM CORE (dùng chung cho chuột + pinch) ─────────────────────────

    /// <summary>
    /// Zoom theo TỈ LỆ (multiplicative). Đây là hàm zoom DUY NHẤT — cả 3 đường
    /// input (chuột New, chuột Legacy, pinch) đều gọi vào đây nên hành vi giống hệt nhau.
    ///
    /// steps > 0 = zoom VÀO (size nhỏ lại) · steps < 0 = zoom RA (size to lên).
    /// Mỗi step đổi size đúng zoomStepPercent %.
    ///
    /// Ví dụ zoomStepPercent = 0.12:
    ///   950 → 1500 cần log(1500/950) / log(1.12) ≈ 4 nấc  (code cũ: 275 nấc)
    ///   950 → 400  cần log(950/400)  / log(1.12) ≈ 8 nấc
    /// </summary>
    /// <param name="steps">Số nấc zoom (có thể là số thực).</param>
    /// <param name="screenFocus">Điểm màn hình để zoom hướng về. Vector2.negativeInfinity = zoom vào tâm.</param>
    private void ApplyZoomStep(float steps, Vector2 screenFocus)
    {
        if (Mathf.Abs(steps) < 0.0001f) return;

        float minS = ActiveMinSize;
        float maxS = ActiveMaxSize;

        // Ghi lại world point dưới con trỏ TRƯỚC khi zoom, để giữ nguyên sau khi zoom.
        bool    useFocus  = zoomTowardCursor && !float.IsNegativeInfinity(screenFocus.x);
        Vector3 worldBefore = Vector3.zero;
        if (useFocus)
            worldBefore = ScreenToWorldAtSize(screenFocus, targetSize);

        // Zoom nhân: mỗi step nhân/chia cho (1 + zoomStepPercent).
        float newSize = targetSize * Mathf.Pow(1f + zoomStepPercent, -steps);
        newSize       = Mathf.Clamp(newSize, minS, maxS);

        if (Mathf.Approximately(newSize, targetSize)) return; // đã chạm giới hạn

        targetSize = newSize;

        // Zoom-to-cursor: dịch camera sao cho world point dưới con trỏ đứng yên.
        if (useFocus)
        {
            Vector3 worldAfter = ScreenToWorldAtSize(screenFocus, targetSize);
            Vector3 shift      = worldBefore - worldAfter;
            targetPosition    += new Vector3(shift.x, shift.y, 0f);
            targetPosition     = ClampToBounds(targetPosition);
        }
    }

    /// <summary>
    /// Tính world position của 1 điểm màn hình GIẢ SỬ camera đang ở orthographic size cho trước.
    /// Cần thiết cho zoom-to-cursor: phải biết điểm đó sẽ nằm đâu SAU khi zoom.
    /// </summary>
    private Vector3 ScreenToWorldAtSize(Vector2 screenPos, float orthoSize)
    {
        if (cam == null || Screen.height == 0) return Vector3.zero;

        // Ortho: 1 pixel = (2 * orthoSize / Screen.height) world unit
        float unitsPerPixel = (2f * orthoSize) / Screen.height;

        // Offset từ tâm màn hình, quy ra world
        Vector2 fromCenter = new Vector2(
            screenPos.x - Screen.width  * 0.5f,
            screenPos.y - Screen.height * 0.5f) * unitsPerPixel;

        // Dùng targetPosition (đích) chứ không phải transform.position (đang lerp),
        // nếu không zoom-to-cursor sẽ bị trôi khi zoom nhanh.
        return new Vector3(targetPosition.x + fromCenter.x,
                           targetPosition.y + fromCenter.y, 0f);
    }

    // ── DEV TOOLS ────────────────────────────────────────────────────────

    /// <summary>
    /// Xử lý phím tắt dev (F1 xem toàn map, F2 bật/tắt dev mode).
    /// CHỈ chạy trong Editor / Development Build — người chơi bản release
    /// không thể bấm F1/F2 để zoom ra ngoài giới hạn.
    /// </summary>
    private void HandleDevHotkeys()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var kb = Keyboard.current;
        if (kb == null) return;

        if (IsValidKey(devModeToggleKey) && kb[devModeToggleKey].wasPressedThisFrame)
            SetDevMode(!_devMode);

        if (IsValidKey(fitMapKey) && kb[fitMapKey].wasPressedThisFrame)
            FitMapToView();
#endif
    }

    /// <summary>
    /// Keyboard indexer làm `(int)key - 1` rồi throw nếu âm.
    /// Dropdown Inspector CÓ hiện `None` → chọn nhầm sẽ ném
    /// ArgumentOutOfRangeException mỗi frame. Phải chặn trước.
    ///
    /// Input System 1.18 đã BỎ `Key.Count`; phím hợp lệ cuối cùng là
    /// <see cref="Key.MediaForward"/>. Dùng nó làm cận trên thay cho `Key.Count`.
    /// </summary>
    public static bool IsValidKey(Key k) => k > Key.None && k <= Key.MediaForward;

    /// <summary>Bật/tắt dev mode. Khi tắt, kẹp zoom về lại dải của người chơi.</summary>
    public void SetDevMode(bool on)
    {
        _devMode = on;
        targetSize = Mathf.Clamp(targetSize, ActiveMinSize, ActiveMaxSize);
        Debug.Log($"[CameraController] Dev Mode: {(on ? "BẬT" : "TẮT")} — zoom {ActiveMinSize}..{ActiveMaxSize}");
    }

    /// <summary>
    /// Zoom ra vừa đủ nhìn toàn bộ NỘI DUNG THẬT + đưa camera về giữa nội dung.
    /// Dev bấm F1 để xem tổng thể nông trại.
    ///
    /// ⚠️ KHÔNG dùng `bounds` để tính: `bounds` là vùng kẹp VỊ TRÍ CAMERA (hiện ±5000),
    /// không phải biên bản đồ. `MapBoundary` còn tự nới nó ra vô hạn. Nếu dùng `bounds`
    /// thì F1 sẽ lia camera về (0,0) — chỗ đất trống, không thấy nông trại đâu cả.
    /// Thay vào đó quét Renderer thật trong scene.
    /// </summary>
    public void FitMapToView()
    {
        if (!TryGetContentBounds(out Bounds content))
        {
            Debug.LogWarning("[CameraController] Fit map: không tìm thấy nội dung nào để đo.");
            return;
        }

        float aspect = (cam != null && cam.aspect > 0.0001f) ? cam.aspect : 16f / 9f;

        // orthographicSize = NỬA chiều cao viewport
        float needed = Mathf.Max(content.size.y * 0.5f,
                                 (content.size.x * 0.5f) / aspect);
        needed *= 1.06f; // chừa viền 6%

        // Vượt dải người chơi → tự bật dev mode để nhìn được
        if (needed > maxSize && !_devMode) SetDevMode(true);

        targetSize = Mathf.Clamp(needed, ActiveMinSize, ActiveMaxSize);

        // Lia thẳng tới tâm nội dung, KHÔNG qua ClampToBounds
        // (bounds có thể chưa bao trùm nội dung → sẽ kéo camera lệch đi)
        targetPosition = new Vector3(content.center.x, content.center.y, transform.position.z);

        bool clipped = needed > ActiveMaxSize + 0.5f;
        Debug.Log($"[CameraController] Fit map → size={targetSize:F0} " +
                  $"| nội dung {content.size.x:F0}x{content.size.y:F0} tại ({content.center.x:F0},{content.center.y:F0})" +
                  (clipped ? $" | ⚠ cần {needed:F0} nhưng trần là {ActiveMaxSize:F0} — tăng devMaxSize để thấy hết" : ""));
    }

    /// <summary>
    /// Đo hộp bao của toàn bộ nội dung nhìn thấy được trong scene (Renderer đang bật).
    /// Kết quả được cache — bấm F1 nhiều lần không quét lại.
    /// </summary>
    public bool TryGetContentBounds(out Bounds result)
    {
        if (_contentBoundsCached)
        {
            result = _contentBounds;
            return true;
        }

        bool found = false;
        var all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var r in all)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (r is ParticleSystemRenderer) continue;          // particle phình bừa bãi
            if (r.GetComponentInParent<Canvas>() != null) continue; // bỏ UI world-space

            if (!found) { _contentBounds = r.bounds; found = true; }
            else        { _contentBounds.Encapsulate(r.bounds); }
        }

        _contentBoundsCached = found;
        result = _contentBounds;
        return found;
    }

    /// <summary>Xoá cache hộp bao — gọi sau khi xây/xoá công trình làm map đổi kích thước.</summary>
    public void InvalidateContentBounds() => _contentBoundsCached = false;

    private Bounds _contentBounds;
    private bool   _contentBoundsCached;

    /// <summary>Đặt zoom trực tiếp (cho nút preset trong dev panel).</summary>
    public void SetZoom(float orthoSize)
        => targetSize = Mathf.Clamp(orthoSize, ActiveMinSize, ActiveMaxSize);

    // ── HELPERS ──────────────────────────────────────────────────────────

    /// <summary>Chuyển toạ độ màn hình sang world space (z = 0).</summary>
    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 pos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        pos.z = 0f;
        return pos;
    }

    /// <summary>Kẹp vị trí camera trong bounds.</summary>
    // ═══════════════════════════════════════════════════════════════════════
    //  [FIX 2026-09-04] "Tới vùng bến tàu là map cứng đơ, không kéo đi đâu được"
    //
    //  GỐC RỄ: Main Camera có component Physics2DRaycaster (Main Camera.prefab:101).
    //  Vì vậy EventSystem.IsPointerOverGameObject() trả TRUE khi con trỏ nằm trên
    //  BẤT KỲ Collider2D nào trong thế giới — KHÔNG chỉ riêng UI. Mà
    //  BoatSystem/Dock_0X/LockUI có BoxCollider2D 180x90 phủ kín vùng bến tàu
    //  ⇒ đứng ở đó là không bắt đầu kéo map được. Bằng chứng đo tại chỗ (UiBlockerProbe):
    //      "KÉO MAP BỊ CHẶN — 1. BoatSystem/Dock_02/LockUI · nút bấm thật=KHÔNG"
    //  và F9 xác nhận BlockMapPan=False, IsAnyPopupOpen=False ⇒ không phải khoá input.
    //
    //  CÁCH CHỮA: chỉ chặn kéo map khi con trỏ nằm trên UI THẬT (Graphic trên Canvas,
    //  tức hit đến từ GraphicRaycaster). Va chạm world do Physics2DRaycaster bắt được
    //  thì BỎ QUA — chúng vốn đã có đường xử lý riêng (OnMouseDown của LockUI,
    //  ObjectDragHandler, PlacementManager...), không liên quan tới việc kéo map.
    //
    //  Revert: bỏ tick "chiChanBoiUiThat" trên Inspector ⇒ quay lại hành vi cũ.
    // ═══════════════════════════════════════════════════════════════════════
    [Header("Chặn kéo map")]
    [Tooltip("BẬT (khuyến nghị): chỉ UI thật (nút/panel trên Canvas) mới chặn kéo map. " +
             "TẮT: quay lại hành vi cũ — mọi Collider2D dưới con trỏ cũng chặn (gây kẹt ở bến tàu).")]
    [SerializeField] private bool chiChanBoiUiThat = true;

    private static readonly System.Collections.Generic.List<RaycastResult> _uiHits =
        new System.Collections.Generic.List<RaycastResult>(16);

    /// <summary>True khi con trỏ đang nằm trên UI ĐÁNG ĐỂ chặn kéo map.</summary>
    private bool ConTroDangTrenUI(Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        if (!chiChanBoiUiThat) return es.IsPointerOverGameObject();

        var data = new PointerEventData(es) { position = screenPos };
        _uiHits.Clear();
        es.RaycastAll(data, _uiHits);

        for (int i = 0; i < _uiHits.Count; i++)
        {
            // Chỉ hit đến từ GraphicRaycaster mới là UI thật (Canvas + Graphic).
            // Hit từ Physics2DRaycaster là va chạm world ⇒ bỏ qua.
            if (_uiHits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
        }
        return false;
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, bounds.x, bounds.y);
        pos.y = Mathf.Clamp(pos.y, bounds.z, bounds.w);
        return pos;
    }

    // IsPointerOverUI() đã chuyển sang InputBridge.IsPointerOverUI() — dùng trực tiếp ở nơi cần.
}