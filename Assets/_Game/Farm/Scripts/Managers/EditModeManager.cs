using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quản lý chế độ sắp xếp (Edit Mode).
/// Khi bật: hiện gridOverlay + overlay vàng, cho phép click công trình.
/// Logic di chuyển được xử lý bởi PlacementManager (reuse Placement_Ghost).
/// </summary>
public class EditModeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static EditModeManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    /// <summary>True khi Edit Mode đang bật</summary>
    public bool isEditMode;

    /// <summary>Backward compat với ObjectDragHandler / CameraController</summary>
    public static bool IsEditMode => Instance != null && Instance.isEditMode;

    // ── Event ─────────────────────────────────────────────────────────────────
    public static event System.Action<bool> OnEditModeChanged;

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grid")]
    /// <summary>GameObject lưới hiển thị khi Edit Mode bật</summary>
    public GameObject gridOverlay;

    [Header("Visuals")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private Color overlayActiveColor = new Color(1f, 1f, 0f, 0.1f);
    [SerializeField] private GameObject editModeLabel;

    // Danh sách bong bóng đang hiện lúc vào Edit Mode — để khôi phục khi thoát
    private readonly List<GameObject> _hiddenBubbles = new();

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

        ApplyVisuals(false);
    }

    private void Update()
    {
        // Phím E để toggle (tiện test trong Editor) — dùng New Input System
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ToggleEditMode();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Gắn vào Btn_EditMode.OnClick() trong Inspector</summary>
    public void ToggleEditMode()
    {
        isEditMode = !isEditMode;

        if (gridOverlay != null)
            gridOverlay.SetActive(isEditMode);

        if (isEditMode)
        {
            HideBubbles();
        }
        else
        {
            // Tắt Edit Mode đột ngột trong lúc đang kéo nhà → cancel ngay, trả nhà về chỗ cũ
            if (PlacementManager.Instance != null && PlacementManager.Instance.IsEditingBuilding)
                PlacementManager.Instance.CancelPlacement();

            RestoreBubbles();
        }

        // Bật/tắt thảm xanh của tất cả công trình trên map
        ToggleAllFootprints(isEditMode);

        ApplyVisuals(isEditMode);
        OnEditModeChanged?.Invoke(isEditMode);
        Debug.Log($"[EditMode] {(isEditMode ? "BẬT" : "TẮT")}");
    }

    // ── Bubble Management ─────────────────────────────────────────────────────

    private void HideBubbles()
    {
        _hiddenBubbles.Clear();

        // Đóng popup nếu đang mở
        Village.HouseOrderPopupUI.Instance?.Close();

        // Thu thập tất cả bong bóng đang active và ẩn chúng
        var bubbles = FindObjectsByType<Village.HouseOrderBubble>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var b in bubbles)
        {
            _hiddenBubbles.Add(b.gameObject);
            b.gameObject.SetActive(false);
        }

        Debug.Log($"[EditMode] Đã ẩn {_hiddenBubbles.Count} bong bóng.");
    }

    private void RestoreBubbles()
    {
        foreach (var go in _hiddenBubbles)
            if (go != null) go.SetActive(true);

        _hiddenBubbles.Clear();
        Debug.Log("[EditMode] Đã hiện lại bong bóng.");
    }

    // ── Footprint Management ──────────────────────────────────────────────────

    private void ToggleAllFootprints(bool active)
    {
        // Bật/tắt thảm xanh của tất cả công trình đứng yên trên map
        var buildings = FindObjectsByType<EditableBuilding>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var b in buildings)
            b.SetFootprintActive(active);

        // Bật/tắt thảm xanh của Ghost đang hoạt động (nếu có)
        PlacementManager.Instance?.SetGhostFootprintActive(active);

        Debug.Log($"[EditMode] Footprint {(active ? "BẬT" : "TẮT")} cho {buildings.Length} công trình + Ghost.");
    }

    public void EnableEditMode()  { if (!isEditMode) ToggleEditMode(); }
    public void DisableEditMode() { if (isEditMode)  ToggleEditMode(); }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void ApplyVisuals(bool active)
    {
        if (overlayImage != null)
        {
            overlayImage.color = active ? overlayActiveColor : Color.clear;
            // Overlay là visual thuần — KHÔNG được chặn Raycast xuống world/building bên dưới
            overlayImage.raycastTarget = false;
        }

        if (editModeLabel != null)
            editModeLabel.SetActive(active);
    }
}
