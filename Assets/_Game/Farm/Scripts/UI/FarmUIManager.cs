using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Village;

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

    [Header("Sickle Bottom Tray")]
    [SerializeField] private GameObject sickleBottomTray;

    [Header("Scene Names")]
    [SerializeField] private string cookingSceneName = "SampleScene";

    [Header("Cooking Mode - Hide/Disable")]
    [SerializeField] private GameObject topBarHUD;
    [SerializeField] private GameObject canvasHudRoot;
    [SerializeField] private GameObject canvasPopupRoot;
    [SerializeField] private GameObject[] popupObjectsToForceClose;
    [SerializeField] private Behaviour[] behavioursToDisableInCooking;
    [SerializeField] private AudioListener farmAudioListener;
    [SerializeField] private Camera farmCamera;

    private bool isCookingMode;
    private CanvasGroup topBarCanvasGroup;

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

        ForceCloseAllPopups();
        StartCoroutine(ForceCloseAllPopupsNextFrame());
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

        // Khay liềm cũng là một popup — ẩn khi HideAllPopups được gọi
        HideSickleTray();

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

    /// <summary>Hard reset every farm popup and any invisible blocker that could swallow map input.</summary>
    public void ForceCloseAllPopups()
    {
        HideAllPopups();

        CloseAllOfType<WarehousePopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<MarketPopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<HouseOrderPopupUI>(popup => popup.Close());
        CloseAllOfType<PigPenPopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<ChickenPenPopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<CowPenPopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<ShopManager>(popup => popup.CloseShop());
        CloseAllOfType<PopupEwarManager>(popup => popup.ClosePopup());
        CloseAllOfType<AttendanceManager>(popup => popup.ClosePopup());
        CloseAllOfType<WelfareEventManager>(popup => popup.ClosePopup());
        CloseAllOfType<TrainLoadPopupUI>(popup => popup.ClosePopup());
        CloseAllOfType<TrainProcessPopupUI>(popup => popup.Hide());
        CloseAllOfType<CropProcessPopupUI>(popup => popup.ClosePopup());

        FarmInputLock.ResetAll();
    }

    private IEnumerator ForceCloseAllPopupsNextFrame()
    {
        yield return null;
        ForceCloseAllPopups();
    }

    private static void CloseAllOfType<T>(Action<T> closeAction) where T : Component
    {
        T[] popups = FindSceneObjects<T>();

        for (int i = 0; i < popups.Length; i++)
        {
            T popup = popups[i];
            if (popup == null)
                continue;

            try
            {
                closeAction(popup);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FarmUI] ForceCloseAllPopups could not close '{popup.name}' ({typeof(T).Name}): {ex.Message}", popup);
            }
        }
    }

    private static T[] FindSceneObjects<T>() where T : Component
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<T>(true);
#endif
    }

    public void ShowSickleTray()
    {
        if (sickleBottomTray != null)
            sickleBottomTray.SetActive(true);
    }

    /// <summary>Ẩn khay liềm và giải phóng mọi trạng thái liên quan.</summary>
    public void HideSickleTray()
    {
        if (sickleBottomTray != null)
            sickleBottomTray.SetActive(false);
    }

    public void ShowSickleTool(Vector3 startWorldPos)
    {
        if (sickleController != null)
            sickleController.BeginHarvestMode(startWorldPos);
        else if (sickleToolRoot != null)
            sickleToolRoot.SetActive(true);
    }

    public void HideSickleTool()
    {
        if (sickleController != null)
            sickleController.EndHarvestMode();

        if (sickleToolRoot != null)
            sickleToolRoot.SetActive(false);

        HideSickleTray();
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

        popupSeed.SetActive(true);
        Debug.Log($"[FarmUI] popupSeed.SetActive(true) | activeInHierarchy={popupSeed.activeInHierarchy}");
        FarmInputLock.IsSeedPopupOpen = true;

        if (plot != null)
            ShowHint($"Kéo hạt giống để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống để trồng.");
    }

    /// <summary>Open flower popup near the clicked flower pot world position.</summary>
    public void ShowFlowerSelectForPlot(PlotController plot)
    {
        if (isCookingMode)
            return;

        HideAllPopups();

        if (popupSeedFlower == null)
        {
            Debug.LogError("[FarmUI] popupSeedFlower is NULL");
            return;
        }

        Transform p = popupSeedFlower.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
                p.gameObject.SetActive(true);
            p = p.parent;
        }

        popupSeedFlower.SetActive(true);
        FarmInputLock.IsSeedPopupOpen = true;

        if (plot != null)
            ShowHint($"Kéo hạt giống để trồng vào chậu hoa {plot.PlotId}");
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

    // ── Cooking Transition (tự xử lý, không phụ thuộc SceneTransitionManager) ──

    public void OnClick_GoCooking()
    {
        if (isCookingMode) return;
        if (SceneManager.GetSceneByName(cookingSceneName).isLoaded) return;
        StartCoroutine(GoToCookingRoutine());
    }

    private IEnumerator GoToCookingRoutine()
    {
        Debug.Log("Đang load SampleScene...");

        // Ẩn Farm ngay lập tức trước khi load
        EnterCookingMode();

        // Load scene Bếp chồng lên Farm
        yield return SceneManager.LoadSceneAsync(cookingSceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// Được gọi từ CookingSceneUI khi bấm nút "Về Farm".
    /// FarmUIManager chạy coroutine unload trên chính nó nên không bị mất khi scene Bếp unload.
    /// </summary>
    public void ReturnFromCooking()
    {
        if (!isCookingMode) return;
        StartCoroutine(ReturnToFarmRoutine());
    }

    private IEnumerator ReturnToFarmRoutine()
    {
        // Unload scene Bếp — chờ hoàn tất
        Scene cookingScene = SceneManager.GetSceneByName(cookingSceneName);
        if (cookingScene.IsValid() && cookingScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(cookingScene);

        yield return null; // 1 frame để Unity dọn dẹp

        // Bật lại Farm
        ExitCookingMode();
        ForceCloseAllPopups();
        FarmInputLock.ResetAll();
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

        SetTopBarActive(false);
        if (canvasHudRoot != null)   canvasHudRoot.SetActive(false);
        if (canvasPopupRoot != null) canvasPopupRoot.SetActive(false);
        if (farmCamera != null)      farmCamera.gameObject.SetActive(false);
        if (farmAudioListener != null) farmAudioListener.enabled = false;
    }

    public void ExitCookingMode()
    {
        if (!isCookingMode)
            return;

        isCookingMode = false;

        // 1. Bật lại Camera Farm — bắt buộc để OnMouseDown trên Building hoạt động
        if (farmCamera != null)        farmCamera.gameObject.SetActive(true);
        if (farmAudioListener != null) farmAudioListener.enabled = true;

        // 2. Bật lại Canvas + đảm bảo CanvasGroup không còn block Raycast
        SetTopBarActive(true);

        if (canvasHudRoot != null)
        {
            canvasHudRoot.SetActive(true);
            var cg = canvasHudRoot.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }
        }

        if (canvasPopupRoot != null)
        {
            canvasPopupRoot.SetActive(true);
            var cg = canvasPopupRoot.GetComponent<CanvasGroup>();
            if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; cg.alpha = 1f; }
        }

        // 3. Đảm bảo EventSystem và TimeScale không bị kẹt
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.enabled = true;
        Time.timeScale = 1f;

        HideAllPopups();
        HideSickleTool();
        RefreshTopBar();
    }

    public void SetTopBarActive(bool isActive)
    {
        CanvasGroup cg = EnsureTopBarCanvasGroup();
        if (cg == null)
            return;

        cg.alpha = isActive ? 1f : 0f;
        cg.interactable = isActive;
        cg.blocksRaycasts = isActive;
    }

    private CanvasGroup EnsureTopBarCanvasGroup()
    {
        if (topBarHUD == null)
            return null;

        if (topBarCanvasGroup != null && topBarCanvasGroup.gameObject == topBarHUD)
            return topBarCanvasGroup;

        topBarCanvasGroup = topBarHUD.GetComponent<CanvasGroup>();
        if (topBarCanvasGroup == null)
            topBarCanvasGroup = topBarHUD.AddComponent<CanvasGroup>();

        return topBarCanvasGroup;
    }
}
