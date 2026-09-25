using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

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
        if (TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial)
        {
            if (isEditMode)
            {
                ToggleEditMode();
            }
            return;
        }

        // PhÃ­m E Ä‘á»ƒ toggle (tiá»‡n test trong Editor) â€” dÃ¹ng New Input System
        // [2026-09-25] Phim tat CHI trong Editor va phai giu SHIFT (Shift+E). Truoc day bam nham E
        // (ca ban build co ban phim) -> lot vao Edit Mode ma khong biet -> cham o dat khong gat duoc.
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && Keyboard.current.shiftKey.isPressed)
            ToggleEditMode();
#endif
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Gáº¯n vÃ o Btn_EditMode.OnClick() trong Inspector</summary>
    public void ToggleEditMode()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial)
        {
            Debug.Log("[EditModeManager] Đang trong Tutorial — không cho phép bật Edit Mode.");
            if (isEditMode)
            {
                isEditMode = false;
                if (gridOverlay != null) gridOverlay.SetActive(false);
                if (PlacementManager.Instance != null && PlacementManager.Instance.IsEditingBuilding)
                    PlacementManager.Instance.CancelPlacement();
                RestoreBubbles();
                ToggleAllFootprints(false);
                PlacementManager.Instance?.RefreshOccupancy();
                ApplyVisuals(false);
                OnEditModeChanged?.Invoke(false);
            }
            return;
        }

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
        SetOrderBoardMarksVisible(false);
    }

    private void RestoreBubbles()
    {
        SetOrderBoardMarksVisible(true);
    }

    /// <summary>
    /// Ẩn/hiện phiếu ghim trên mặt bảng đơn hàng khi vào/ra Edit Mode, và đóng popup
    /// bảng đơn nếu nó đang mở.
    ///
    /// VÌ SAO không còn danh sách `_hiddenBubbles`: hệ cũ phải nhớ từng bong bóng đã tắt
    /// vì chúng nằm rải trên 12 nhà dân khắp bản đồ, mỗi cái một trạng thái riêng. Bảng
    /// đơn mới chỉ có MỘT object, trạng thái phiếu suy thẳng từ dữ liệu bảng — bật lại là
    /// nó tự vẽ đúng, không cần chụp ảnh trạng thái trước đó. Bớt một danh sách là bớt
    /// một chỗ để rò rỉ tham chiếu tới object đã bị huỷ.
    /// </summary>
    private void SetOrderBoardMarksVisible(bool visible)
    {
        if (!visible)
        {
            // Đang kéo thả công trình mà popup còn mở thì click rơi vào popup, người chơi
            // tưởng game đơ. Đóng trước rồi mới đổi chế độ.
            var popups = FindObjectsByType<OrderBoardPopupUI>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            foreach (var p in popups)
                if (p != null && p.IsOpen) p.ClosePopup();
        }

        var boards = FindObjectsByType<OrderBoardWorldObject>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var b in boards)
            if (b != null) b.SetOrderMarksVisible(visible);
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
        else
            BangBaoEditMode(active);
    }

    // ── [2026-09-25] BANG BAO "EDIT MODE" ─────────────────────────────────────────
    // Dang Edit Mode thi cham o dat / nha chi de SAP XEP, khong gat / khong mo popup. Truoc day
    // khong co gi bao tren man hinh -> tuong game hong. Nay hien 1 bang nho giua mep tren,
    // bam vao la THOAT Edit Mode. Tu dung luc chay (Sep co editModeLabel rieng thi dung cai do).
    private GameObject _bangBao;

    private void BangBaoEditMode(bool hien)
    {
        if (!hien) { if (_bangBao != null) _bangBao.SetActive(false); return; }
        if (_bangBao == null)
        {
            var cvGo = new GameObject("EditMode_Banner", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            // de o GOC scene (khong lam con cua object nao) -> luon la canvas goc, khong bi canvas cha nuot
            var cv = cvGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 460;
            var sc = cvGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight = 0.5f;

            var nen = new GameObject("Btn_ThoatEditMode", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)nen.transform;
            rt.SetParent(cvGo.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -112f);
            rt.sizeDelta = new Vector2(520f, 64f);
            var img = nen.GetComponent<Image>();
            img.color = new Color(0.36f, 0.2f, 0.08f, 0.9f);
            nen.GetComponent<Button>().onClick.AddListener(() => { if (isEditMode) ToggleEditMode(); });

            var chuGo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            var crt = (RectTransform)chuGo.transform;
            crt.SetParent(rt, false);
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = Vector2.zero;
            var chu = chuGo.GetComponent<TextMeshProUGUI>();
            chu.text = "EDIT MODE  \u00b7  Tap here to exit";
            chu.fontSize = 30f;
            chu.fontStyle = FontStyles.Bold;
            chu.alignment = TextAlignmentOptions.Center;
            chu.color = new Color(1f, 0.94f, 0.78f);
            chu.raycastTarget = false;
            _bangBao = cvGo;
        }
        _bangBao.SetActive(true);
    }
}
