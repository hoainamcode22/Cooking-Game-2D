using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

/// <summary>
/// Drag-drop kiểu Hay Day cho công trình di chuyển được (chuồng bò, bếp, kho...).
/// Hoạt động khi EditModeManager.IsEditMode == true.
///
/// Dùng InputBridge để thống nhất Mouse + Touch — không còn #if UNITY_EDITOR.
/// Flow: nhấn object → kéo > dragThreshold px → StartDragging → theo con trỏ
///       → nhả → EndDragging (đặt hợp lệ hoặc bounce về cũ).
///
/// Yêu cầu: Collider2D trên object.
/// </summary>
public class ObjectDragHandler : MonoBehaviour
{
    [Header("Drag Detection")]
    [SerializeField] private float dragThreshold = 15f;

    [Header("Drag Visual")]
    [SerializeField] private float          dragScaleMultiplier = 1.1f;
    [SerializeField] private SpriteRenderer shadowSprite;
    [SerializeField] private float          shadowAlphaActive   = 0.6f;

    // GRID SNAP: KHÔNG còn field gridSize riêng.
    // Trước đây script này snap theo 50 còn PlacementManager snap theo 100 → hai hệ lưới
    // lệch nhau, kéo lại một công trình đã đặt là nó rơi vào mốc nửa ô (lỗi L4 §1).
    // Giờ cả hai dùng chung hằng số PlacementManager.CELL.

    [Header("Placement Validation (dự phòng khi thiếu PlacementManager)")]
    [Tooltip("Chỉ dùng khi scene không có PlacementManager. Đường chính là kiểm tra ô lưới.")]
    [SerializeField] private Vector2    collisionCheckSize    = new Vector2(PlacementManager.CELL, PlacementManager.CELL);
    [SerializeField] private LayerMask  obstacleLayerMask;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer placementIndicator;
    [SerializeField] private Color          validPlacementColor   = Color.green;
    [SerializeField] private Color          invalidPlacementColor = Color.red;

    [Header("Bounce Animation")]
    [SerializeField] private float bounceReturnDuration = 0.3f;
    [SerializeField] private float bounceHeight         = 0.2f;

    public static bool IsDraggingObject { get; private set; }

    private Camera        _cam;
    private Collider2D    _col;
    private SpriteRenderer _sprite;

    private Vector3 _originalPos;
    private Vector3 _originalScale;
    private Color   _originalColor;

    private bool    _pressHeld;
    private bool    _isDragging;
    private Vector2 _pressStartScreen;
    private Vector3 _dragWorldPos;
    private Vector2Int _gridSizeCells = Vector2Int.one;

    // Độ lệch từ ĐIỂM NEO (transform.position, pivot ở ĐÁY sprite) tới TÂM hộp bao.
    // PlacementManager.GetFootprintRect nhận TÂM KHỐI Ô, nên không cộng bù cái này thì
    // vùng ô kiểm tra tụt xuống nửa chiều cao sprite: kéo nhà xuống sát nhà khác vẫn
    // "hợp lệ" (đè mái), còn đất trống phía dưới lại bị báo bận.
    private Vector2 _pivotOffset = Vector2.zero;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void OnEnable()  => EnhancedTouchSupport.Enable();
    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        if (_isDragging) ForceCancelDrag();
    }

    private void Awake()
    {
        _cam    = Camera.main;
        _col    = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();

        // BUG CŨ (L11 §1): `if (_col == null)` bị treo, KHÔNG có {} và KHÔNG có thân lệnh
        // → nó "nuốt" luôn dòng `_originalPos = ...` làm thân của mình. Hậu quả:
        // _originalPos/_originalScale/_originalColor chỉ được gán khi THIẾU collider,
        // tức gần như không bao giờ → bounce-back trả vật về (0,0,0) và scale = 0.
        if (_col == null)
        {
            Debug.LogWarning($"[ObjectDragHandler] '{name}' thiếu Collider2D — không kéo được.", this);
        }

        _originalPos   = transform.position;
        _originalScale = transform.localScale;
        _originalColor = _sprite != null ? _sprite.color : Color.white;

        // Cỡ ô của vật này, suy từ hộp bao visual (vật kéo tay không có PlaceableItemData).
        _gridSizeCells = MeasureGridSize();

        if (shadowSprite != null) SetShadowAlpha(0f);
    }

    /// <summary>
    /// Số ô lưới vật này chiếm, đo từ hộp bao các SpriteRenderer con.
    /// Đồng thời ghi lại <see cref="_pivotOffset"/> — cùng một phép đo, cùng một hộp bao,
    /// nên kích thước và độ lệch không bao giờ nói hai chuyện khác nhau.
    /// </summary>
    private Vector2Int MeasureGridSize()
    {
        Bounds b = default;
        bool found = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            if (!found) { b = sr.bounds; found = true; }
            else b.Encapsulate(sr.bounds);
        }
        if (!found) return Vector2Int.one;

        // sr.bounds là hộp bao WORLD đã tính sẵn pivot + xoay → hiệu số với transform.position
        // chính là độ lệch pivot cần bù. Đo một lần ở Awake vì vật không đổi hình khi kéo.
        _pivotOffset = new Vector2(b.center.x - transform.position.x,
                                   b.center.y - transform.position.y);

        RectInt r = PlacementManager.RectFromWorldBounds(b);
        return new Vector2Int(Mathf.Max(1, r.width), Mathf.Max(1, r.height));
    }

    private void Update()
    {
        if (!EditModeManager.IsEditMode) return;
        if (PlacementManager.IsPlacingNewObject) return;

        HandleInput();
    }

    // =========================================================================
    // Unified Input (Mouse + Touch via InputBridge — runtime, không phải compile-time)
    // =========================================================================

    private void HandleInput()
    {
        // ── Nhấn xuống ──────────────────────────────────────────────────────
        if (InputBridge.IsPointerDownThisFrame && !_pressHeld)
        {
            Vector2 screenPos = InputBridge.PointerPosition;

            // Bỏ qua nếu đang chạm UI (tránh drag xuyên thấu qua nút UI)
            if (InputBridge.IsPointerOverUI()) return;

            if (IsPointerOverThisObject(screenPos))
            {
                _pressHeld        = true;
                _pressStartScreen = screenPos;
            }
        }

        // ── Đang giữ: kiểm tra đã kéo đủ ngưỡng chưa ──────────────────────
        if (_pressHeld && !_isDragging && InputBridge.IsPointerHeld)
        {
            float moved = Vector2.Distance(InputBridge.PointerPosition, _pressStartScreen);
            if (moved > dragThreshold)
            {
                BeginDrag();
            }
        }

        // ── Đang drag: cập nhật vị trí ─────────────────────────────────────
        if (_isDragging && InputBridge.IsPointerHeld)
        {
            _dragWorldPos = ScreenToWorld(InputBridge.PointerPosition);
            UpdateDragPosition();
        }

        // ── Nhả ra ─────────────────────────────────────────────────────────
        if (InputBridge.IsPointerUpThisFrame)
        {
            if (_isDragging)
            {
                EndDrag();
                _isDragging = false;
            }
            _pressHeld        = false;
            _pressStartScreen = Vector2.zero;
        }
    }

    // =========================================================================
    // Drag Logic
    // =========================================================================

    private void BeginDrag()
    {
        IsDraggingObject = true;
        _isDragging      = true;
        _originalPos     = transform.position;

        // Chụp lại bảng ô đã chiếm ở đúng thời điểm này: vùng ô ghi cho chính vật đang
        // kéo sẽ là vùng GỐC, nên luôn thả về chỗ cũ được, còn ô của vật khác vẫn chặn.
        PlacementManager.Instance?.RefreshOccupancy();

        transform.localScale = _originalScale * dragScaleMultiplier;
        if (shadowSprite != null) SetShadowAlpha(shadowAlphaActive);

        FreeCursor();
    }

    private void UpdateDragPosition()
    {
        Vector3 snapped = SnapToGrid(_dragWorldPos);
        transform.position = snapped;

        bool valid = IsValidPlacement(snapped);
        UpdatePlacementIndicator(snapped, valid);

        if (_sprite != null)
            _sprite.color = valid
                ? new Color(0f, 1f, 0f, 0.5f)
                : new Color(1f, 0f, 0f, 0.5f);
    }

    private void EndDrag()
    {
        bool valid = IsValidPlacement(transform.position);

        transform.localScale = _originalScale;
        if (shadowSprite != null) SetShadowAlpha(0f);
        if (placementIndicator != null) placementIndicator.enabled = false;
        if (_sprite != null) _sprite.color = _originalColor;

        if (valid)
            _originalPos = transform.position;
        else
            StartCoroutine(BounceBack(_originalPos));

        IsDraggingObject = false;
        FreeCursor();

        // Vật đã đứng ở chỗ mới → cập nhật lại bảng ô cho lần kéo/đặt kế tiếp.
        PlacementManager.Instance?.RefreshOccupancy();
    }

    /// <summary>Gọi khi script bị disable giữa chừng drag — reset state không bounce.</summary>
    private void ForceCancelDrag()
    {
        transform.localScale = _originalScale;
        transform.position   = _originalPos;
        if (_sprite != null) _sprite.color = _originalColor;
        if (shadowSprite != null) SetShadowAlpha(0f);
        if (placementIndicator != null) placementIndicator.enabled = false;
        IsDraggingObject = false;
        _isDragging      = false;
        _pressHeld       = false;
        FreeCursor();
    }

    // =========================================================================
    // Placement Helpers
    // =========================================================================

    /// <summary>Snap TÂM Ô dùng chung công thức với PlacementManager (CELL = 100).</summary>
    private Vector3 SnapToGrid(Vector3 pos)
    {
        Vector3 snapped = PlacementManager.SnapCenter(pos, _gridSizeCells);
        snapped.z = pos.z;
        return snapped;
    }

    /// <summary>
    /// Hợp lệ = ô lưới còn trống (bỏ qua ô của chính mình) VÀ nằm trong biên bản đồ.
    ///
    /// BỎ Physics2D: obstacleLayer trong scene có m_Bits = 0 và groundLayerMask trỏ vào
    /// layer không có collider nền → OverlapBox luôn null nên hàm cũ hoặc LUÔN đúng
    /// (nhánh obstacle) hoặc LUÔN sai (nhánh ground, bắt buộc phải trúng mới hợp lệ).
    /// Kiểm tra theo ô lưới không phụ thuộc layer/collider nên chính xác tuyệt đối.
    /// </summary>
    private bool IsValidPlacement(Vector3 pos)
    {
        PlacementManager pm = PlacementManager.Instance;
        if (pm != null)
        {
            RectInt rect = PlacementManager.GetFootprintRect(FootprintCenterOf(pos), _gridSizeCells);
            return pm.IsAreaFree(rect, gameObject) && pm.IsRectInsideMap(rect);
        }

        // Dự phòng (scene test không có PlacementManager): giữ lại phép kiểm tra vật cản cũ.
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(pos, collisionCheckSize, 0f, obstacleLayerMask);
        foreach (var c in overlaps)
            if (c.gameObject != gameObject) return false;
        return true;
    }

    /// <summary>Đổi ĐIỂM NEO thành TÂM KHỐI Ô — thứ mà GetFootprintRect thực sự cần.</summary>
    private Vector3 FootprintCenterOf(Vector3 anchor)
        => new Vector3(anchor.x + _pivotOffset.x, anchor.y + _pivotOffset.y, 0f);

    private void UpdatePlacementIndicator(Vector3 pos, bool valid)
    {
        if (placementIndicator == null) return;
        placementIndicator.enabled          = true;
        // Vẽ ở TÂM vùng ô để người chơi thấy đúng chỗ sẽ bị chặn, không phải ở chân nhà.
        Vector3 c = FootprintCenterOf(pos);
        c.z = placementIndicator.transform.position.z;
        placementIndicator.transform.position = c;
        placementIndicator.color            = valid ? validPlacementColor : invalidPlacementColor;
    }

    // =========================================================================
    // Bounce Animation
    // =========================================================================

    private IEnumerator BounceBack(Vector3 target)
    {
        Vector3 start   = transform.position;
        float   elapsed = 0f;

        while (elapsed < bounceReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t   = elapsed / bounceReturnDuration;
            Vector3 p = Vector3.Lerp(start, target, t);
            p.y += Mathf.Sin(t * Mathf.PI) * bounceHeight;
            transform.position = p;
            yield return null;
        }

        transform.position = target;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private bool IsPointerOverThisObject(Vector2 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero, 0f);
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        Vector3 w = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        w.z = transform.position.z;
        return w;
    }

    private void SetShadowAlpha(float alpha)
    {
        if (shadowSprite == null) return;
        Color c = shadowSprite.color;
        c.a = alpha;
        shadowSprite.color = c;
    }

    private static void FreeCursor()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible) Cursor.visible = true;
#endif
    }
}
