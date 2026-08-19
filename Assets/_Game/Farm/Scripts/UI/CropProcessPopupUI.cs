using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Popup "Painel" hiển thị tiến trình cây đang lớn.
/// Gắn script này lên GameObject Painel — con trực tiếp của ô đất (World Space Canvas).
/// Mỗi ô đất có một instance riêng; không dùng Singleton.
/// </summary>
public class CropProcessPopupUI : MonoBehaviour
{
    [Header("Info")]
    public TMP_Text txtCropName;
    public TMP_Text txtTimeRemaining;

    [Header("Progress Bar")]
    public Image progressFill;

    [Header("Speed Up")]
    public Button btnSpeedUp;
    public TMP_Text txtGemCost;
    public Image imgDiamondIcon;

    /// <summary>
    /// F9 — giá gem KHÔNG còn là số cứng trong Inspector.
    /// Nó phụ thuộc thời gian còn lại của đúng ô đất đang mở popup
    /// (<see cref="PlotController.GetSpeedUpGemCost"/>), nên phải đọc lại mỗi frame:
    /// người chơi mở popup rồi ngồi xem thì con số phải giảm dần theo cây.
    /// </summary>
    public int CurrentGemCost => currentPlot != null ? currentPlot.GetSpeedUpGemCost() : 0;

    public bool IsOpen => gameObject.activeSelf;
    public RectTransform SpeedUpButtonRect =>
        btnSpeedUp != null ? btnSpeedUp.GetComponent<RectTransform>() : null;

    private static readonly System.Collections.Generic.HashSet<CropProcessPopupUI> _openInstances
        = new System.Collections.Generic.HashSet<CropProcessPopupUI>();
    public static bool AnyOpen => _openInstances.Count > 0;

    private PlotController currentPlot;
    private bool popupInputLockHeld;
    // Thanh XANH thật = Image con của progressFill (type Filled).
    private Image _fillImage;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        AutoBindComponents();
        bool startOpen = gameObject.activeSelf;
        if (!startOpen) gameObject.SetActive(false);
    }

    private void Start()
    {
        AutoBindComponents();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (currentPlot != null)
        {
            if (currentPlot.IsGrowing)
            {
                UpdatePositionToCurrentPlot();
                RefreshDisplay();
            }
            else
            {
                // Cây đã chín tự nhiên trong lúc popup đang mở → đóng popup
                ClosePopup();
                return;
            }
        }

        // Click ra ngoài popup → đóng
        if (Input.GetMouseButtonDown(0) && !IsPointerOverPopupUI(Input.mousePosition))
            ClosePopup();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Bật popup và bind dữ liệu từ ô đất đang growing.</summary>
    public void OpenForPlot(PlotController plot)
    {
        if (plot == null || !plot.IsGrowing) return;

        currentPlot = plot;

        AutoBindComponents();
        RefreshDisplay();
        UpdatePositionToCurrentPlot();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
        TutorialManager.Instance?.NotifyOpenCropProcess();
    }

    private void UpdatePositionToCurrentPlot()
    {
        if (currentPlot == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = currentPlot.transform.position + new Vector3(0f, 0.7f, 0f);

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) return;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) return;

            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos))
            {
                rootRect.anchoredPosition = localPos;
            }
        }
        else if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) return;

            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, parentCanvas.worldCamera, out Vector2 localPos))
            {
                rootRect.anchoredPosition = localPos;
            }
        }
        else
        {
            rootRect.position = worldPos;
        }
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
        currentPlot = null;
    }

    /// <summary>
    /// Gán hàm này vào btn_RutNang_TGCay trên Prefab UI (OnClick → CropProcessPopupUI.OnGemClick).
    /// </summary>
    public void OnGemClick()
    {
        if (currentPlot == null)
        {
            Debug.LogError("[CropProcessPopup] OnGemClick: currentPlot là NULL — OpenForPlot chưa được gọi hoặc popup bị mở sai.");
            return;
        }

        if (!currentPlot.IsGrowing)
        {
            Debug.LogWarning("[CropProcessPopup] OnGemClick: ô đất không đang Growing, bỏ qua.");
            ClosePopup();
            return;
        }

        if (FarmEconomyManager.Instance == null)
        {
            Debug.LogError("[CropProcessPopup] OnGemClick: FarmEconomyManager.Instance NULL.");
            return;
        }

        int cost = CurrentGemCost;
        if (FarmEconomyManager.Instance.Gems < cost)
        {
            FarmUIManager.Instance?.ShowHint($"Cần {cost} kim cương để tăng tốc.");
            return;
        }

        // InstantGrow tự trừ gem + ép trạng thái Ready
        currentPlot.InstantGrow();
        TutorialManager.Instance?.NotifySpeedUp();
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
        currentPlot = null;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    public void AutoBindComponents()
    {
        if (progressFill == null)
        {
            var fillTr = transform.Find("Track_Bar/Progress_Fill") ?? transform.Find("Progress_Fill");
            if (fillTr != null) progressFill = fillTr.GetComponent<Image>();
        }

        if (txtTimeRemaining == null)
        {
            var timeTr = transform.Find("Track_Bar/Txt_TimeRemaining") ?? transform.Find("Txt_TimeRemaining") ?? transform.Find("Text_Time");
            if (timeTr != null) txtTimeRemaining = timeTr.GetComponent<TMP_Text>();
        }

        if (txtCropName == null)
        {
            var nameTr = transform.Find("Txt_CropName") ?? transform.Find("Text_CropName");
            if (nameTr != null) txtCropName = nameTr.GetComponent<TMP_Text>();
        }

        if (btnSpeedUp == null)
        {
            var btnTr = transform.Find("Btn_SpeedUp") ?? transform.Find("Btn_gem");
            if (btnTr != null) btnSpeedUp = btnTr.GetComponent<Button>();
        }

        if (btnSpeedUp != null)
        {
            if (txtGemCost == null)
            {
                var costTr = btnSpeedUp.transform.Find("Txt_GemCost") ?? btnSpeedUp.transform.Find("Text_Gia");
                if (costTr != null) txtGemCost = costTr.GetComponent<TMP_Text>();
                else txtGemCost = btnSpeedUp.GetComponentInChildren<TMP_Text>(true);
            }

            if (imgDiamondIcon == null)
            {
                var diaTr = btnSpeedUp.transform.Find("Icon_Diamond") ?? btnSpeedUp.transform.Find("Icon_Tien") ?? btnSpeedUp.transform.Find("img_kimcuong");
                if (diaTr != null) imgDiamondIcon = diaTr.GetComponent<Image>();
            }

            btnSpeedUp.onClick.RemoveAllListeners();
            btnSpeedUp.onClick.AddListener(OnGemClick);
        }

        if (progressFill != null)
        {
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    private void RefreshDisplay()
    {
        if (currentPlot == null) return;

        if (txtCropName == null || progressFill == null || txtTimeRemaining == null || btnSpeedUp == null)
        {
            AutoBindComponents();
        }

        if (txtCropName != null)
        {
            txtCropName.text = currentPlot.CurrentCrop != null
                ? currentPlot.CurrentCrop.displayName
                : "Đang lớn...";
        }

        if (txtTimeRemaining != null)
        {
            txtTimeRemaining.text = currentPlot.GetRemainingTimeText();
        }

        if (txtGemCost != null)
        {
            txtGemCost.text = CurrentGemCost.ToString();
        }

        if (progressFill != null)
        {
            progressFill.fillAmount = currentPlot.GetGrowProgress01();
        }
    }

    private void OnSpeedUpClicked()
    {
        if (currentPlot == null || !currentPlot.IsGrowing) return;

        if (FarmEconomyManager.Instance == null)
        {
            Debug.LogWarning("[CropProcessPopup] FarmEconomyManager NULL");
            return;
        }

        if (!FarmEconomyManager.Instance.SpendGems(CurrentGemCost))
        {
            FarmUIManager.Instance?.ShowHint("Không đủ kim cương để tăng tốc.");
            return;
        }

        currentPlot.CompleteInstantly();
        ClosePopup();
    }

    // ── UI Raycast ────────────────────────────────────────────────────────────

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
        _openInstances.Add(this);
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
        _openInstances.Remove(this);
    }

    private void OnDestroy()
    {
        _openInstances.Remove(this);
    }

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }
}
