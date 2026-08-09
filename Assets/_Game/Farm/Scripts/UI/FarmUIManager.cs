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

    public RectTransform SickleTrayRect =>
        sickleToolRoot != null ? sickleToolRoot.GetComponent<RectTransform>() : null;

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
        // Subscribe sau khi táº¥t cáº£ Awake() Ä‘Ã£ cháº¡y xong â€” Instance Ä‘áº£m báº£o khÃ´ng null
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
            txtDay.text = "NgÃ y 1";

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

                // Bá» qua building cá»‘ Ä‘á»‹nh cá»§a map â€” chÃºng luÃ´n pháº£i hiá»‡n.
                if (popupObjectsToForceClose[i].GetComponentInChildren<PermanentBuilding>(true) != null
                 || popupObjectsToForceClose[i].GetComponentInParent<PermanentBuilding>()    != null)
                {
                    Debug.LogWarning($"[FarmUI] Bá» qua HideAllPopups cho '{popupObjectsToForceClose[i].name}' â€” Ä‘Ã¢y lÃ  PermanentBuilding.");
                    continue;
                }

                // Bá» qua Train popup (ká»ƒ cáº£ khi object trong máº£ng lÃ  Canvas parent chá»©a chÃºng).
                if (popupObjectsToForceClose[i].GetComponentInChildren<TrainLoadPopupUI>(true)    != null) continue;
                if (popupObjectsToForceClose[i].GetComponentInChildren<TrainProcessPopupUI>(true) != null) continue;

                popupObjectsToForceClose[i].SetActive(false);
            }
        }
    }

    // BÆ°á»›c 1: click Ã´ chÃ­n â†’ chá»‰ hiá»‡n khay (tray), chÆ°a báº¯t Ä‘áº§u harvest
    public void ShowSickleTray()
    {
        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);

        TutorialManager.Instance?.NotifySickleShown();
    }

    // BÆ°á»›c 2: player nháº¥n giá»¯ icon liá»m trong tray â†’ báº¯t Ä‘áº§u harvest mode
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
    /// Converts world â†’ screen â†’ canvas local once, then popup stays fixed.
    /// </summary>
    public void ShowPlantSelectForPlot(PlotController plot)
    {
        if (isCookingMode)
            return;


        HideAllPopups();

        if (popupSeed == null)
        {
            Debug.LogError("[FarmUI] popupSeed is NULL");
            return;
        }

        // Äáº£m báº£o toÃ n bá»™ parent chain cá»§a popupSeed Ä‘á»u active
        Transform p = popupSeed.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
            {
                Debug.LogWarning($"[FarmUI] Parent bá»‹ táº¯t, báº­t láº¡i: {p.name}");
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }

        // Reset popup vá» giá»¯a mÃ n hÃ¬nh Ä‘á»ƒ Ä‘áº£m báº£o luÃ´n hiá»ƒn thá»‹
        RectTransform popupRect = popupSeed.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.anchoredPosition = Vector2.zero;
        }

        popupSeed.SetActive(true);
        FarmInputLock.IsSeedPopupOpen = true;
        TutorialManager.Instance?.NotifySeedPanelOpened();

        if (plot != null)
            ShowHint($"KÃ©o háº¡t giá»‘ng Ä‘á»ƒ trá»“ng vÃ o Ã´ {plot.PlotId}");
        else
            ShowHint("KÃ©o háº¡t giá»‘ng Ä‘á»ƒ trá»“ng.");
    }

    public void ShowPlantSelectForFlower(PlotController plot)
    {
        if (isCookingMode) return;

        HideAllPopups();

        if (popupSeedFlower == null)
        {
            Debug.LogError("[FarmUI] popupSeedFlower is NULL â€” kÃ©o popup hoa vÃ o Inspector.");
            return;
        }

        Transform p = popupSeedFlower.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p = p.parent;
        }

        RectTransform rt = popupSeedFlower.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        popupSeedFlower.SetActive(true);
        FarmInputLock.IsSeedPopupOpen = true;
        TutorialManager.Instance?.NotifySeedPanelOpened();

        if (plot != null)
            ShowHint($"KÃ©o háº¡t giá»‘ng hoa Ä‘á»ƒ trá»“ng vÃ o Ã´ {plot.PlotId}");
        else
            ShowHint("KÃ©o háº¡t giá»‘ng hoa Ä‘á»ƒ trá»“ng.");
    }

    /// <summary>Close seed popup (cáº£ 2 loáº¡i) vÃ  clear input locks.</summary>
    public void HidePlantSelectPopup()
    {
        if (popupSeed != null)
            popupSeed.SetActive(false);

        if (popupSeedFlower != null)
            popupSeedFlower.SetActive(false);

        FarmInputLock.IsSeedPopupOpen = false;
        FarmInputLock.IsDraggingSeed  = false;
    }

    /// <summary>Hiá»‡n floating icon theo chuá»™t khi báº¯t Ä‘áº§u kÃ©o háº¡t giá»‘ng.</summary>
    public void ShowFloatingDragIcon(Sprite icon) => floatingDragIcon?.Show(icon);

    /// <summary>áº¨n floating icon khi káº¿t thÃºc drag.</summary>
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
        SceneTransitionManager.Instance.LoadScene(cookingSceneName, SceneTransitionManager.TransitionType.CloudWipe, LoadSceneMode.Additive);
    }

    public void OnClick_OpenInventory()
    {
        if (isCookingMode) return;
        ShowHint("Má»Ÿ tÃºi Ä‘á»“.");
    }

    public void OnClick_OpenWarehouse()
    {
        if (isCookingMode) return;
        ShowHint("Má»Ÿ kho.");
    }

    public void OnClick_OpenMarket()
    {
        if (isCookingMode) return;
        ShowHint("Má»Ÿ chá»£.");
    }

    public void OnClick_OpenRanking()
    {
        if (isCookingMode) return;
        ShowHint("Má»Ÿ báº£ng xáº¿p háº¡ng.");
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
