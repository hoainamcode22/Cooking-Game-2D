using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FarmUIManager : MonoBehaviour
{
    public static FarmUIManager Instance { get; private set; }

    [Header("Top Bar")]
    [SerializeField] private TMP_Text txtDay;
    [SerializeField] private TMP_Text txtGold;
    [SerializeField] private TMP_Text txtGem;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtHint;

    [Header("Popup Root")]
    [SerializeField] private GameObject popupSeed;
    [SerializeField] private GameObject popupSeedFlower;

    [Header("Drag Icon")]
    [SerializeField] private FloatingDragIcon floatingDragIcon;

    [Header("Harvest Tool")]
    [SerializeField] private GameObject sickleToolRoot;
    [SerializeField] private SickleController sickleController;

    [Header("Scene Names")]
    [SerializeField] private string cookingSceneName = "SampleScene";

    [Header("Cooking Mode - Hide/Disable")]
    [SerializeField] private GameObject canvasHudRoot;
    [SerializeField] private GameObject canvasPopupRoot;
    [SerializeField] private GameObject[] popupObjectsToForceClose;
    [SerializeField] private Behaviour[] behavioursToDisableInCooking;
    [SerializeField] private AudioListener farmAudioListener;
    [SerializeField] private Camera farmCamera;

    private bool isCookingMode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (farmCamera == null)
            farmCamera = Camera.main;

        if (farmAudioListener == null && farmCamera != null)
            farmAudioListener = farmCamera.GetComponent<AudioListener>();
    }

    private void Start()
    {
        // Subscribe sau khi tất cả Awake() đã chạy xong — Instance đảm bảo không null
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;

        HideAllPopups();
        HideSickleTool();
        RefreshTopBar();
    }

    private void OnDestroy()
    {
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    private void HandleCurrencyChanged(int gold, int gems)
    {
        RefreshTopBar();
    }

    private void HandleLevelChanged(int level)
    {
        RefreshTopBar();
    }

    public void RefreshTopBar()
    {
        if (txtDay != null)
            txtDay.text = "Ngày 1";

        if (txtGold != null)
        {
            txtGold.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gold.ToString()
                : "0";
        }

        if (txtGem != null)
        {
            txtGem.text = FarmEconomyManager.Instance != null
                ? FarmEconomyManager.Instance.Gems.ToString()
                : "0";
        }

        if (txtLevel != null)
        {
            txtLevel.text = FarmLevelManager.Instance != null
                ? $"Lv.{FarmLevelManager.Instance.CurrentLevel}"
                : "Lv.1";
        }
    }

    public void ShowHint(string message)
    {
        if (txtHint != null)
            txtHint.text = message;
    }

    public void HideAllPopups()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);

        if (popupSeedFlower != null)
            popupSeedFlower.SetActive(false);

        // Clear seed-related input locks whenever all popups close.
        FarmInputLock.IsSeedPopupOpen = false;
        FarmInputLock.IsDraggingSeed  = false;

        if (popupObjectsToForceClose != null)
        {
            for (int i = 0; i < popupObjectsToForceClose.Length; i++)
            {
                if (popupObjectsToForceClose[i] == null) continue;

                // Bỏ qua building cố định của map — chúng luôn phải hiện.
                if (popupObjectsToForceClose[i].GetComponentInChildren<PermanentBuilding>(true) != null
                 || popupObjectsToForceClose[i].GetComponentInParent<PermanentBuilding>()    != null)
                {
                    Debug.LogWarning($"[FarmUI] Bỏ qua HideAllPopups cho '{popupObjectsToForceClose[i].name}' — đây là PermanentBuilding.");
                    continue;
                }

                // Bỏ qua Train popup (kể cả khi object trong mảng là Canvas parent chứa chúng).
                if (popupObjectsToForceClose[i].GetComponentInChildren<TrainLoadPopupUI>(true)    != null) continue;
                if (popupObjectsToForceClose[i].GetComponentInChildren<TrainProcessPopupUI>(true) != null) continue;

                popupObjectsToForceClose[i].SetActive(false);
            }
        }
    }

    // Bước 1: click ô chín → chỉ hiện khay (tray), chưa bắt đầu harvest
    public void ShowSickleTray()
    {
        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);
    }

    // Bước 2: player nhấn giữ icon liềm trong tray → bắt đầu harvest mode
    public void ShowSickleTool(Vector3 startWorldPos)
    {
        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);

        if (sickleController != null)
            sickleController.BeginHarvestMode(startWorldPos);
    }

    public void HideSickleTool()
    {
        if (sickleController != null)
            sickleController.EndHarvestMode();

        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(false);
    }

    /// <summary>
    /// Open seed popup near the clicked plot world position.
    /// Converts world → screen → canvas local once, then popup stays fixed.
    /// </summary>
    public void ShowPlantSelectForPlot(PlotController plot)
    {
        if (isCookingMode)
            return;

        Debug.Log("[FarmUI] ShowPlantSelectForPlot CALLED");

        HideAllPopups();

        if (popupSeed == null)
        {
            Debug.LogError("[FarmUI] popupSeed is NULL");
            return;
        }

        // Đảm bảo toàn bộ parent chain của popupSeed đều active
        Transform p = popupSeed.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
            {
                Debug.LogWarning($"[FarmUI] Parent bị tắt, bật lại: {p.name}");
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }

        // Reset popup về giữa màn hình để đảm bảo luôn hiển thị
        RectTransform popupRect = popupSeed.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.anchoredPosition = Vector2.zero;
            Debug.Log($"[FarmUI] popup anchoredPosition reset to (0,0)");
        }

        popupSeed.SetActive(true);
        Debug.Log($"[FarmUI] popupSeed.SetActive(true) | activeInHierarchy={popupSeed.activeInHierarchy}");
        FarmInputLock.IsSeedPopupOpen = true;

        if (plot != null)
            ShowHint($"Kéo hạt giống để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống để trồng.");
    }

    /// <summary>Close seed popup (cả 2 loại) và clear input locks.</summary>
    public void HidePlantSelectPopup()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);

        if (popupSeedFlower != null)
            popupSeedFlower.SetActive(false);

        FarmInputLock.IsSeedPopupOpen = false;
        FarmInputLock.IsDraggingSeed  = false;
    }

    /// <summary>Hiện floating icon theo chuột khi bắt đầu kéo hạt giống.</summary>
    public void ShowFloatingDragIcon(Sprite icon) => floatingDragIcon?.Show(icon);

    /// <summary>Ẩn floating icon khi kết thúc drag.</summary>
    public void HideFloatingDragIcon() => floatingDragIcon?.Hide();

    public void OnClick_CloseAllPopups()
    {
        HideAllPopups();
    }

    public void ForceCloseAllPopups()
    {
        HideAllPopups();
        FarmInputLock.ResetAll();
    }

    public void OnClick_GoCooking()
    {
        if (SceneManager.GetSceneByName(cookingSceneName).isLoaded)
            return;

        EnterCookingMode();
        SceneManager.LoadScene(cookingSceneName, LoadSceneMode.Additive);
    }

    public void OnClick_OpenInventory()
    {
        if (isCookingMode) return;
        ShowHint("Mở túi đồ.");
    }

    public void OnClick_OpenWarehouse()
    {
        if (isCookingMode) return;
        ShowHint("Mở kho.");
    }

    public void OnClick_OpenMarket()
    {
        if (isCookingMode) return;
        ShowHint("Mở chợ.");
    }

    public void OnClick_OpenRanking()
    {
        if (isCookingMode) return;
        ShowHint("Mở bảng xếp hạng.");
    }

    public void EnterCookingMode()
    {
        if (isCookingMode)
            return;

        isCookingMode = true;

        HideAllPopups();
        HideSickleTool();

        if (canvasHudRoot != null)
            canvasHudRoot.SetActive(false);

        if (canvasPopupRoot != null)
            canvasPopupRoot.SetActive(false);

        if (behavioursToDisableInCooking != null)
        {
            for (int i = 0; i < behavioursToDisableInCooking.Length; i++)
            {
                if (behavioursToDisableInCooking[i] != null)
                    behavioursToDisableInCooking[i].enabled = false;
            }
        }

        if (farmAudioListener != null)
            farmAudioListener.enabled = false;

        if (farmCamera != null)
            farmCamera.enabled = false;
    }

    public void ExitCookingMode()
    {
        if (!isCookingMode)
            return;

        isCookingMode = false;

        if (canvasHudRoot != null)
            canvasHudRoot.SetActive(true);

        if (canvasPopupRoot != null)
            canvasPopupRoot.SetActive(true);

        if (behavioursToDisableInCooking != null)
        {
            for (int i = 0; i < behavioursToDisableInCooking.Length; i++)
            {
                if (behavioursToDisableInCooking[i] != null)
                    behavioursToDisableInCooking[i].enabled = true;
            }
        }

        if (farmAudioListener != null)
            farmAudioListener.enabled = true;

        if (farmCamera != null)
            farmCamera.enabled = true;

        HideAllPopups();
        HideSickleTool();
        RefreshTopBar();
    }
}
