using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý chế độ sắp xếp (Edit Mode).
/// - Nút Btn_EditMode gọi ToggleEditMode() để bật/tắt.
/// - Khi bật: overlay vàng, hiện label, cho phép nhấc/kéo công trình.
/// - Khi tắt: xóa overlay, ẩn label, cấm kéo.
///
/// Logic kéo thả (nhấc, snap grid, xanh/đỏ, thả) nằm hoàn toàn ở đây.
/// Gắn lên Systems / Managers GameObject trong scene.
/// </summary>
public class EditModeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static EditModeManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    /// <summary>True khi Edit Mode đang bật</summary>
    public static bool IsEditMode { get; private set; }

    /// <summary>True khi đang giữ và kéo một vật thể — CameraController dùng để block pan</summary>
    public static bool IsDragging { get; private set; }

    // ── Event ─────────────────────────────────────────────────────────────────
    public static event System.Action<bool> OnEditModeChanged;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Visuals")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private Color overlayActiveColor = new Color(1f, 1f, 0f, 0.1f);
    [SerializeField] private GameObject editModeLabel; // GameObject chứa Text/TMP "Chế độ sắp xếp"

    [Header("Drag & Drop")]
    [SerializeField] private float gridSize = 50f;
    [SerializeField] private Vector2 collisionCheckSize = new Vector2(48f, 48f); // nhỏ hơn 1 chút để tránh edge
    [SerializeField] private LayerMask obstacleLayerMask; // Layer "Obstacle" của các công trình

    // ── Drag state ────────────────────────────────────────────────────────────
    private Camera mainCamera;
    private GameObject targetObject;     // Công trình đang cầm
    private SpriteRenderer targetSprite; // SpriteRenderer của công trình
    private Vector3 originalPosition;    // Vị trí gốc để trả về nếu đặt sai
    private bool isPlacementValid;       // Ô đứng có hợp lệ không (dùng khi thả)

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main;
        ApplyVisuals(false);
    }

    private void Update()
    {
        // Phím E để toggle (tiện test trong Editor)
        if (Input.GetKeyDown(KeyCode.E))
            ToggleEditMode();

        if (!IsEditMode) return;

        // ── Nhấc vật thể khi click ──
        if (Input.GetMouseButtonDown(0))
            TryPickObject();

        // ── Kéo theo chuột khi đang giữ ──
        if (IsDragging && targetObject != null)
            DragObject();

        // ── Thả khi nhả chuột ──
        if (Input.GetMouseButtonUp(0) && IsDragging)
            DropObject();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Gắn vào Btn_EditMode.OnClick() trong Inspector</summary>
    public void ToggleEditMode()
    {
        IsEditMode = !IsEditMode;
        ApplyVisuals(IsEditMode);
        OnEditModeChanged?.Invoke(IsEditMode);
        Debug.Log($"[EditMode] {(IsEditMode ? "BẬT" : "TẮT")}");
    }

    public void EnableEditMode()  { if (!IsEditMode) ToggleEditMode(); }
    public void DisableEditMode() { if (IsEditMode)  ToggleEditMode(); }

    // ── Drag Logic ────────────────────────────────────────────────────────────

    /// <summary>
    /// Bắn Physics2D.OverlapPoint vào vị trí chuột trên Layer Obstacle.
    /// Nếu trúng → nhặt vật thể đó lên.
    /// </summary>
    private void TryPickObject()
    {
        Vector3 worldPos = GetMouseWorldPos();
        Collider2D hit = Physics2D.OverlapPoint(worldPos, obstacleLayerMask);
        if (hit == null) return;

        targetObject     = hit.gameObject;
        targetSprite     = targetObject.GetComponent<SpriteRenderer>();
        originalPosition = targetObject.transform.position;
        IsDragging       = true;

        Debug.Log($"[EditMode] Nhặt: {targetObject.name}");
    }

    /// <summary>
    /// Object bám theo chuột với snap grid 50x50.
    /// Tô xanh nếu ô trống, đỏ nếu vướng.
    /// </summary>
    private void DragObject()
    {
        Vector3 snapped = SnapToGrid(GetMouseWorldPos());
        targetObject.transform.position = snapped;

        isPlacementValid = IsValidPlacement(snapped);

        if (targetSprite != null)
            targetSprite.color = isPlacementValid
                ? new Color(0f, 1f, 0f, 0.5f)  // xanh = trống
                : new Color(1f, 0f, 0f, 0.5f); // đỏ  = vướng
    }

    /// <summary>
    /// Thả chuột: xanh → chốt vị trí mới; đỏ → trả về vị trí cũ.
    /// Reset màu về trắng sau khi thả.
    /// </summary>
    private void DropObject()
    {
        if (!isPlacementValid)
        {
            targetObject.transform.position = originalPosition;
            Debug.Log($"[EditMode] Trả {targetObject.name} về {originalPosition}");
        }
        else
        {
            Debug.Log($"[EditMode] Đặt {targetObject.name} tại {targetObject.transform.position}");
        }

        // Reset màu về trắng (1,1,1,1)
        if (targetSprite != null)
            targetSprite.color = Color.white;

        targetObject = null;
        targetSprite = null;
        IsDragging   = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Kiểm tra vị trí có trống không (OverlapBoxAll, loại trừ chính targetObject)</summary>
    private bool IsValidPlacement(Vector3 position)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, collisionCheckSize, 0f, obstacleLayerMask);
        foreach (var col in hits)
        {
            if (col.gameObject == targetObject) continue; // bỏ qua chính nó
            return false;
        }
        return true;
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x / gridSize) * gridSize,
            Mathf.Round(pos.y / gridSize) * gridSize,
            pos.z);
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 pos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        pos.z = 0f;
        return pos;
    }

    /// <summary>Bật/tắt overlay và label theo trạng thái Edit Mode</summary>
    private void ApplyVisuals(bool active)
    {
        if (overlayImage != null)
            overlayImage.color = active ? overlayActiveColor : Color.clear;

        if (editModeLabel != null)
            editModeLabel.SetActive(active);
    }
}
