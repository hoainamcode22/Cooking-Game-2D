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
    public static CropProcessPopupUI Instance { get; private set; }

    [Header("Info")]
    public TMP_Text txtCropName;
    public TMP_Text txtTimeRemaining;

    [Header("Progress Bar")]
    public Image progressFill;

    [Header("Speed Up")]
    public Button btnSpeedUp;
    public TMP_Text txtGemCost;
    public Image imgDiamondIcon;

    public int CurrentGemCost => currentPlot != null
        ? currentPlot.GetSpeedUpGemCost()
        : (currentPen != null ? currentPen.SpeedUpGemCost : (currentHouse != null ? currentHouse.SpeedUpGemCost : 0));

    public bool IsOpen => gameObject.activeSelf;
    public RectTransform SpeedUpButtonRect =>
        btnSpeedUp != null ? btnSpeedUp.GetComponent<RectTransform>() : null;

    private static readonly System.Collections.Generic.HashSet<CropProcessPopupUI> _openInstances
        = new System.Collections.Generic.HashSet<CropProcessPopupUI>();
    public static bool AnyOpen => _openInstances.Count > 0;

    private PlotController currentPlot;
    private PenMiniPanelUI currentPen;
    private HouseGrowthController currentHouse;
    private bool popupInputLockHeld;
    private int _openedAtFrame = -999; // Guard: tránh đóng ngay frame vừa mở

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        AutoBindComponents();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (Instance == null) Instance = this;
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
                UpdatePosition();
                RefreshDisplay();
            }
            else
            {
                ClosePopup();
                return;
            }
        }
        else if (currentPen != null)
        {
            if (currentPen.CurrentState == PenMiniPanelUI.PenState.Processing)
            {
                UpdatePosition();
                RefreshDisplay();
            }
            else
            {
                ClosePopup();
                return;
            }
        }
        else if (currentHouse != null)
        {
            if (currentHouse.State == HouseGrowthController.GrowthState.Building)
            {
                UpdatePosition();
                RefreshDisplay();
            }
            else
            {
                ClosePopup();
                return;
            }
        }

        // Click ra ngoài popup → đóng
        // Guard: bỏ qua frame vừa mở để tránh click-to-open đóng ngay lập tức
        if (Time.frameCount > _openedAtFrame + 2
            && Input.GetMouseButtonDown(0)
            && !IsPointerOverPopupUI(Input.mousePosition))
        {
            ClosePopup();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Bật popup và bind dữ liệu từ ô đất đang growing.</summary>
    public void OpenForPlot(PlotController plot)
    {
        if (plot == null || !plot.IsGrowing) return;

        currentPlot = plot;
        currentPen = null;
        currentHouse = null;
        _openedAtFrame = Time.frameCount;

        AutoBindComponents();
        RefreshDisplay();
        UpdatePosition();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
        TutorialManager.Instance?.NotifyOpenCropProcess();
    }

    /// <summary>Bật popup cho Chuồng Gia Súc / Máy Chế Biến đang nuôi/sản xuất (Processing).</summary>
    public void OpenForPen(PenMiniPanelUI pen)
    {
        if (pen == null || pen.CurrentState != PenMiniPanelUI.PenState.Processing) return;

        currentPen = pen;
        currentPlot = null;
        currentHouse = null;
        _openedAtFrame = Time.frameCount;

        AutoBindComponents();
        RefreshDisplay();
        UpdatePosition();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
    }

    /// <summary>Bật popup cho Ngôi Nhà đang xây dựng (Stage 1..3).</summary>
    public void OpenForHouse(HouseGrowthController house)
    {
        if (house == null || house.State != HouseGrowthController.GrowthState.Building) return;

        currentHouse = house;
        currentPlot = null;
        currentPen = null;
        _openedAtFrame = Time.frameCount;

        AutoBindComponents();
        RefreshDisplay();
        UpdatePosition();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
    }

    private void UpdatePosition()
    {
        Vector3 worldPos = Vector3.zero;
        if (currentPlot != null)
            worldPos = currentPlot.transform.position + new Vector3(0f, 0.7f, 0f);
        else if (currentPen != null)
            worldPos = currentPen.transform.position + new Vector3(0f, 1.85f, 0f);
        else if (currentHouse != null)
            worldPos = currentHouse.transform.position + new Vector3(0f, 2.6f, 0f);
        else
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

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
        currentPen = null;
        currentHouse = null;
    }

    /// <summary>
    /// Gán hàm này vào btn_RutNang_TGCay trên Prefab UI (OnClick → CropProcessPopupUI.OnGemClick).
    /// </summary>
    public void OnGemClick()
    {
        if (currentPlot != null)
        {
            if (!currentPlot.IsGrowing)
            {
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
            ClosePopup();
        }
        else if (currentPen != null)
        {
            if (currentPen.CurrentState != PenMiniPanelUI.PenState.Processing)
            {
                ClosePopup();
                return;
            }

            currentPen.TrySpeedUpGem();
            ClosePopup();
        }
        else if (currentHouse != null)
        {
            if (currentHouse.State != HouseGrowthController.GrowthState.Building)
            {
                ClosePopup();
                return;
            }

            currentHouse.TrySpeedUpWithGem();
            ClosePopup();
        }
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
        if (currentPlot == null && currentPen == null && currentHouse == null) return;

        if (txtCropName == null || progressFill == null || txtTimeRemaining == null || btnSpeedUp == null)
        {
            AutoBindComponents();
        }

        if (currentPlot != null)
        {
            if (txtCropName != null)
            {
                txtCropName.text = currentPlot.CurrentCrop != null
                    ? currentPlot.CurrentCrop.displayName.ToUpper()
                    : "ĐANG TRỒNG...";
                txtCropName.color = Color.white;
            }

            if (txtTimeRemaining != null)
            {
                txtTimeRemaining.text = currentPlot.GetRemainingTimeText();
                txtTimeRemaining.color = Color.white;
            }

            if (txtGemCost != null)
            {
                txtGemCost.text = CurrentGemCost.ToString();
                txtGemCost.color = Color.white;
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = currentPlot.GetGrowProgress01();
            }
        }
        else if (currentPen != null)
        {
            if (txtCropName != null)
            {
                txtCropName.text = currentPen.GetPenDisplayName();
                txtCropName.color = Color.white;
            }

            if (txtTimeRemaining != null)
            {
                float remaining = currentPen.GetRemainingSeconds();
                int m = Mathf.FloorToInt(remaining / 60f);
                int s = Mathf.FloorToInt(remaining % 60f);
                txtTimeRemaining.text = $"{m}:{s:D2}";
                txtTimeRemaining.color = Color.white;
            }

            if (txtGemCost != null)
            {
                txtGemCost.text = currentPen.SpeedUpGemCost.ToString();
                txtGemCost.color = Color.white;
            }

            if (progressFill != null)
            {
                float remaining = currentPen.GetRemainingSeconds();
                float total = Mathf.Max(1f, currentPen.EffectiveFeedSeconds);
                progressFill.fillAmount = Mathf.Clamp01(1f - remaining / total);
            }
        }
        else if (currentHouse != null)
        {
            if (txtCropName != null)
            {
                txtCropName.text = currentHouse.HouseName.ToUpper();
                txtCropName.color = Color.white;
            }

            if (txtTimeRemaining != null)
            {
                float rem = currentHouse.RemainingSeconds;
                int min = Mathf.FloorToInt(rem / 60f);
                int sec = Mathf.FloorToInt(rem % 60f);
                txtTimeRemaining.text = $"{min:00}:{sec:00}";
                txtTimeRemaining.color = Color.white;
            }

            if (txtGemCost != null)
            {
                txtGemCost.text = currentHouse.SpeedUpGemCost.ToString();
                txtGemCost.color = Color.white;
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = currentHouse.Progress;
            }
        }
    }

    private void OnSpeedUpClicked()
    {
        OnGemClick();
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
