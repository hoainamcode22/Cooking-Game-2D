using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quáº£n lÃ½ cháº¿ Ä‘á»™ sáº¯p xáº¿p (Edit Mode).
/// Khi báº­t: hiá»‡n gridOverlay + overlay vÃ ng, cho phÃ©p click cÃ´ng trÃ¬nh.
/// Logic di chuyá»ƒn Ä‘Æ°á»£c xá»­ lÃ½ bá»Ÿi PlacementManager (reuse Placement_Ghost).
/// </summary>
public class EditModeManager : MonoBehaviour
{
    // â”€â”€ Singleton â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public static EditModeManager Instance { get; private set; }

    // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>True khi Edit Mode Ä‘ang báº­t</summary>
    public bool isEditMode;

    /// <summary>Backward compat vá»›i ObjectDragHandler / CameraController</summary>
    public static bool IsEditMode => Instance != null && Instance.isEditMode;

    // â”€â”€ Event â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public static event System.Action<bool> OnEditModeChanged;

    // â”€â”€ Inspector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Header("Grid")]
    /// <summary>GameObject lÆ°á»›i hiá»ƒn thá»‹ khi Edit Mode báº­t</summary>
    public GameObject gridOverlay;

    [Header("Visuals")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private Color overlayActiveColor = new Color(1f, 1f, 0f, 0.1f);
    [SerializeField] private GameObject editModeLabel;

    // Danh sÃ¡ch bong bÃ³ng Ä‘ang hiá»‡n lÃºc vÃ o Edit Mode â€” Ä‘á»ƒ khÃ´i phá»¥c khi thoÃ¡t
    private readonly List<GameObject> _hiddenBubbles = new();

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        // PhÃ­m E Ä‘á»ƒ toggle (tiá»‡n test trong Editor) â€” dÃ¹ng New Input System
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ToggleEditMode();
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Gáº¯n vÃ o Btn_EditMode.OnClick() trong Inspector</summary>
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
            // Táº¯t Edit Mode Ä‘á»™t ngá»™t trong lÃºc Ä‘ang kÃ©o nhÃ  â†’ cancel ngay, tráº£ nhÃ  vá» chá»— cÅ©
            if (PlacementManager.Instance != null && PlacementManager.Instance.IsEditingBuilding)
                PlacementManager.Instance.CancelPlacement();

            RestoreBubbles();
        }

        // Báº­t/táº¯t tháº£m xanh cá»§a táº¥t cáº£ cÃ´ng trÃ¬nh trÃªn map
        ToggleAllFootprints(isEditMode);

        // DEV-1 / V3: vao hoac ra Edit Mode deu phai dung lai bang O DA CHIEM.
        // Ly do: nguoi choi co the vua keo cong trinh bang ObjectDragHandler, hoac
        // scene vua spawn them nha tu save. Neu khong refresh thi lan dat ke tiep
        // se doi chieu voi du lieu cu -> cho da co nha van bao "trong".
        PlacementManager.Instance?.RefreshOccupancy();

        ApplyVisuals(isEditMode);
        OnEditModeChanged?.Invoke(isEditMode);
    }

    // â”€â”€ Bubble Management â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void HideBubbles()
    {
        _hiddenBubbles.Clear();

        // ÄÃ³ng popup náº¿u Ä‘ang má»Ÿ
        Village.HouseOrderPopupUI.Instance?.Close();

        // Thu tháº­p táº¥t cáº£ bong bÃ³ng Ä‘ang active vÃ  áº©n chÃºng
        var bubbles = FindObjectsByType<Village.HouseOrderBubble>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var b in bubbles)
        {
            _hiddenBubbles.Add(b.gameObject);
            b.gameObject.SetActive(false);
        }

    }

    private void RestoreBubbles()
    {
        foreach (var go in _hiddenBubbles)
            if (go != null) go.SetActive(true);

        _hiddenBubbles.Clear();
    }

    // â”€â”€ Footprint Management â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ToggleAllFootprints(bool active)
    {
        // Báº­t/táº¯t tháº£m xanh cá»§a táº¥t cáº£ cÃ´ng trÃ¬nh Ä‘á»©ng yÃªn trÃªn map
        var buildings = FindObjectsByType<EditableBuilding>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var b in buildings)
            b.SetFootprintActive(active);

        // Báº­t/táº¯t tháº£m xanh cá»§a Ghost Ä‘ang hoáº¡t Ä‘á»™ng (náº¿u cÃ³)
        PlacementManager.Instance?.SetGhostFootprintActive(active);

    }

    public void EnableEditMode()  { if (!isEditMode) ToggleEditMode(); }
    public void DisableEditMode() { if (isEditMode)  ToggleEditMode(); }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ApplyVisuals(bool active)
    {
        if (overlayImage != null)
        {
            overlayImage.color = active ? overlayActiveColor : Color.clear;
            // Overlay lÃ  visual thuáº§n â€” KHÃ”NG Ä‘Æ°á»£c cháº·n Raycast xuá»‘ng world/building bÃªn dÆ°á»›i
            overlayImage.raycastTarget = false;
        }

        if (editModeLabel != null)
            editModeLabel.SetActive(active);
    }
}
