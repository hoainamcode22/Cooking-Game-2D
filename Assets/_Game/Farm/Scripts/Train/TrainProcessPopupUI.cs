using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrainProcessPopupUI : MonoBehaviour
{
    [Header("Info")]
    [SerializeField] private TMP_Text txtStatus;
    [SerializeField] private TMP_Text txt_time;

    [Header("Progress Bar")]
    [SerializeField] private Image progressFill;

    [Header("Speed Up / Close")]
    [SerializeField] private Button btnSpeedUp;
    [SerializeField] private TMP_Text txtGemCost;
    [SerializeField] private Image imgDiamondIcon;
    [SerializeField] private Button Btn_close;

    private bool popupInputLockHeld;
    private float initialDuration = 60f;

    public bool IsVisible => gameObject.activeSelf;
    public bool IsOpen    => gameObject.activeSelf;  // alias dùng chung với PopupManager

    private void Awake()
    {
        AutoBindComponents();
        if (Btn_close != null) Btn_close.onClick.AddListener(Hide);
        if (btnSpeedUp != null) btnSpeedUp.onClick.AddListener(OnSpeedUpClick);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;

        UpdatePosition();

        if (TrainManager.Instance != null && TrainManager.Instance.State == TrainState.Processing)
        {
            UpdateTimer(TrainManager.Instance.TripRemainingTime);
        }

        // Click ngoài popup → đóng
        if (Input.GetMouseButtonDown(0) && !IsPointerOverPopupUI(Input.mousePosition))
        {
            Hide();
        }
    }

    public void Show(float totalTime)
    {
        AutoBindComponents();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
        if (totalTime > 0f) initialDuration = totalTime;

        if (txtStatus != null)
        {
            txtStatus.text = "GA TÀU HOẢ";
            txtStatus.color = Color.white;
        }

        UpdateTimer(totalTime);
        UpdatePosition();
    }

    public void ShowArrived()
    {
        AutoBindComponents();
        gameObject.SetActive(true);
        AcquirePopupInputBlock();
        if (txtStatus != null)
        {
            txtStatus.text = "TÀU ĐÃ VỀ!";
            txtStatus.color = Color.white;
        }
        if (txt_time != null)
        {
            txt_time.text = "00:00";
            txt_time.color = Color.white;
        }
        if (progressFill != null) progressFill.fillAmount = 1f;
        UpdatePosition();
    }

    public void UpdateTimer(float remaining)
    {
        if (txt_time != null)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            txt_time.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
            txt_time.color = Color.white;
        }

        if (progressFill != null && initialDuration > 0f)
        {
            progressFill.fillAmount = Mathf.Clamp01(1f - remaining / initialDuration);
        }

        if (txtGemCost != null)
        {
            int cost = ConstructionManager.RushCostFor(remaining);
            txtGemCost.text = Mathf.Max(1, cost).ToString();
            txtGemCost.color = Color.white;
        }
    }

    public void OnSpeedUpClick()
    {
        Hide();
    }

    private void UpdatePosition()
    {
        var station = FindFirstObjectByType<TrainStationBuilding>();
        if (station == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = station.transform.position + new Vector3(0f, 1.8f, 0f);

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
    }

    public void Hide()
    {
        ReleasePopupInputBlock();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
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

    public void AutoBindComponents()
    {
        if (progressFill == null)
        {
            var fillTr = transform.Find("Track_Bar/Progress_Fill") ?? transform.Find("Progress_Fill");
            if (fillTr != null) progressFill = fillTr.GetComponent<Image>();
        }

        if (txt_time == null)
        {
            var timeTr = transform.Find("Track_Bar/Txt_TimeRemaining") ?? transform.Find("Txt_TimeRemaining") ?? transform.Find("Text_Time");
            if (timeTr != null) txt_time = timeTr.GetComponent<TMP_Text>();
        }

        if (txtStatus == null)
        {
            var nameTr = transform.Find("Txt_Status") ?? transform.Find("Txt_CropName") ?? transform.Find("Text_CropName");
            if (nameTr != null) txtStatus = nameTr.GetComponent<TMP_Text>();
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
            btnSpeedUp.onClick.AddListener(OnSpeedUpClick);
        }

        if (progressFill != null)
        {
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }
}
