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

    [Header("Grid Snap")]
    [SerializeField] private float gridSize = 50f;

    [Header("Placement Validation")]
    [SerializeField] private Vector2    collisionCheckSize    = new Vector2(50f, 50f);
    [SerializeField] private LayerMask  obstacleLayerMask;
    [SerializeField] private LayerMask  groundLayerMask;

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

        if (_col == null)
            Debug.LogError($"[ObjectDragHandler] {name} thiếu Collider2D!");

        _originalPos   = transform.position;
        _originalScale = transform.localScale;
        _originalColor = _sprite != null ? _sprite.color : Color.white;

        if (shadowSprite != null) SetShadowAlpha(0f);
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

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / gridSize) * gridSize,
            Mathf.Round(pos.y / gridSize) * gridSize,
            pos.z);
    }

    private bool IsValidPlacement(Vector3 pos)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(pos, collisionCheckSize, 0f, obstacleLayerMask);
        foreach (var c in overlaps)
            if (c.gameObject != gameObject) return false;

        return Physics2D.OverlapBox(pos, collisionCheckSize, 0f, groundLayerMask) != null;
    }

    private void UpdatePlacementIndicator(Vector3 pos, bool valid)
    {
        if (placementIndicator == null) return;
        placementIndicator.enabled          = true;
        placementIndicator.transform.position = pos;
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
