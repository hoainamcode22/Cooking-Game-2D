using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LivestockPenPopupUI : MonoBehaviour
{
    public static LivestockPenPopupUI Instance { get; private set; }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private GameObject dimBG;
    [SerializeField] private GameObject panel;

    [Header("Header")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private Button btnClose;
    [SerializeField] private Image imgAnimal;

    [Header("Upgrade UI")]
    [SerializeField] private GameObject updateRoot;
    [SerializeField] private TMP_Text txtNangCap;
    [SerializeField] private Image iconGem;
    [SerializeField] private Image iconGold;
    [SerializeField] private Button btnUpdate;
    [SerializeField] private TMP_Text txtUpdate;

    [Header("Progress UI")]
    [SerializeField] private Image milkProgressFill;
    [SerializeField] private TMP_Text txtProgress;

    [Header("Feed Button")]
    [SerializeField] private Button btnChoAn;
    [SerializeField] private TMP_Text txtChoAn;

    [Header("Collect Button")]
    [SerializeField] private Button btnThuThap;
    [SerializeField] private TMP_Text txtThuThap;

    private LivestockPenController currentPen;
    private CanvasGroup collectCanvasGroup;

    private void Awake()
    {
        Instance = this;
        Debug.Log("LivestockPenPopupUI Awake OK");

        if (btnClose != null)
            btnClose.onClick.AddListener(Close);

        if (btnChoAn != null)
            btnChoAn.onClick.AddListener(OnClickFeed);

        if (btnThuThap != null)
            btnThuThap.onClick.AddListener(OnClickCollect);

        if (btnUpdate != null)
            btnUpdate.onClick.AddListener(OnClickUpgrade);

        if (btnThuThap != null)
        {
            collectCanvasGroup = btnThuThap.GetComponent<CanvasGroup>();
            if (collectCanvasGroup == null)
                collectCanvasGroup = btnThuThap.gameObject.AddComponent<CanvasGroup>();
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);

        SetProgressVisual(0f, 0);
    }
    private void Update()
    {
        if (popupRoot == null || !popupRoot.activeSelf || currentPen == null)
            return;

        currentPen.RefreshStateByTime();
        RefreshRuntimeUI();
        UpdateCollectBlink();
    }

    public void Open(LivestockPenController pen)
    {
        currentPen = pen;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshAll();
    }

    public void Close()
    {
        currentPen = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void RefreshAll()
    {
        if (currentPen == null)
            return;

        if (txtTitle != null)
            txtTitle.text = currentPen.GetTitleText();

        if (imgAnimal != null)
            imgAnimal.sprite = currentPen.AnimalSprite;

        if (txtNangCap != null)
            txtNangCap.text = currentPen.GetNextLevelText();

        if (txtUpdate != null)
            txtUpdate.text = "Nâng cấp";

        if (btnUpdate != null)
            btnUpdate.interactable = false; // Tạm khóa, chưa làm logic nâng cấp thật

        if (txtChoAn != null)
            txtChoAn.text = "Cho ăn";

        if (txtThuThap != null)
            txtThuThap.text = "Thu thập";

        RefreshRuntimeUI();
        UpdateCollectBlink();
    }

    private void RefreshRuntimeUI()
    {
        if (currentPen == null)
            return;

        float progress01 = currentPen.GetProgress01();
        int progressPercent = currentPen.GetProgressPercent();
        SetProgressVisual(progress01, progressPercent);

        if (btnChoAn != null)
            btnChoAn.interactable = currentPen.CanFeed();

        if (btnThuThap != null)
            btnThuThap.interactable = currentPen.CanCollect();

        if (txtChoAn != null)
        {
            if (currentPen.IsFeeding)
                txtChoAn.text = "Đang ăn...";
            else if (currentPen.ReadyToCollect)
                txtChoAn.text = "Đã no";
            else
                txtChoAn.text = "Cho ăn";
        }

        if (txtThuThap != null)
            txtThuThap.text = currentPen.CanCollect() ? "Thu thập x4" : "Thu thập";
    }

    private void SetProgressVisual(float fill01, int percent)
    {
        if (milkProgressFill != null)
            milkProgressFill.fillAmount = Mathf.Clamp01(fill01);

        if (txtProgress != null)
            txtProgress.text = $"{Mathf.Clamp(percent, 0, 100)}%";
    }
    // Hiệu ứng nhấp nháy khi có thể thu thập
    private void UpdateCollectBlink()
    {
        if (collectCanvasGroup == null || currentPen == null)
            return;

        if (currentPen.CanCollect())
        {
            float alpha = 0.55f + Mathf.PingPong(Time.unscaledTime * 1.8f, 0.45f);
            collectCanvasGroup.alpha = alpha;

            Vector3 scale = Vector3.one * (1f + Mathf.PingPong(Time.unscaledTime * 0.25f, 0.08f));
            btnThuThap.transform.localScale = scale;
        }
        else
        {
            collectCanvasGroup.alpha = 1f;
            btnThuThap.transform.localScale = Vector3.one;
        }
    }
    //btn cho ăn
    private void OnClickFeed()
    {
        Debug.Log("[Popup] BtnChoAn CLICK");

        if (currentPen == null)
        {
            Debug.LogError("[Popup] currentPen NULL");
            return;
        }

        bool success = currentPen.TryFeed();
        Debug.Log("[Popup] TryFeed result = " + success);

        RefreshAll();
    }

    private void OnClickCollect()
    {
        if (currentPen == null)
            return;

        bool success = currentPen.TryCollect();
        if (!success)
        {
            Debug.Log("[Popup] Thu thập thất bại. Chưa đủ 100%.");
        }

        RefreshAll();
    }

    private void OnClickUpgrade()
    {
        Debug.Log("[Popup] Nút nâng cấp đang để UI trước, chưa nối logic gold/gem.");
    }
}