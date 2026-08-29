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

    // ═════════════════════════════════════════════════════════════════════════
    //  KHO ĐẦY → BÁO CHO NGƯỜI CHƠI  (CS-6 · hoàn tất yêu cầu F8)
    // ═════════════════════════════════════════════════════════════════════════
    //
    // `FarmInventoryManager.OnAddRejectedByCapacity` bắn mỗi lần kho phải TỪ CHỐI một
    // loại vật phẩm MỚI vì hết slot. Chú thích nơi khai báo sự kiện ghi "UI nào muốn hiện
    // popup 'kho đầy' thì nghe ở đây" — nhưng grep toàn dự án ra ĐÚNG 0 chỗ `+=`.
    // Sự kiện chết ngay khi vừa sinh ra, nên F8 mới chỉ làm được một nửa: CHẶN đúng
    // nhưng KHÔNG BÁO. Người chơi bấm cây chín mà không thu được thì chỉ thấy game hỏng.
    //
    // VÌ SAO nghe ở FarmUIManager chứ không ở kho: kho là `DontDestroyOnLoad`, sống qua
    // cả scene bếp nơi không có UI nông trại — nó không được phép biết gì về UI. Ngược
    // lại FarmUIManager là thứ đã nắm đường thông báo duy nhất (`ShowHint`).
    //
    // VÌ SAO OnEnable/OnDisable chứ không Awake/OnDestroy: sự kiện là STATIC. Đăng ký ở
    // Awake mà quên gỡ là kho giữ tham chiếu tới một FarmUIManager đã chết qua mỗi lần
    // đổi scene — rò rỉ dần và bắn vào object hỏng. Cặp OnEnable/OnDisable luôn cân nhau.

    private void OnEnable()
    {
        FarmInventoryManager.OnAddRejectedByCapacity += HandleKhoTuChoiViDay;
    }

    private void OnDisable()
    {
        FarmInventoryManager.OnAddRejectedByCapacity -= HandleKhoTuChoiViDay;
    }

    private void HandleKhoTuChoiViDay(string itemId)
    {
        // Tên hiển thị tiếng Việt; `GetDisplayName` tự trả lại chính itemId nếu bảng giá
        // chưa có dòng đó, nên không bao giờ ra chuỗi rỗng.
        string ten = MarketPriceTable.GetDisplayName(itemId);

        // Kèm số ô đang dùng — người chơi cần biết "đầy" là đầy bao nhiêu thì mới biết
        // nên bán bớt hay nâng cấp kho.
        FarmInventoryManager kho = FarmInventoryManager.Instance;
        string sucChua = kho != null ? $" ({kho.UsedSlots}/{kho.SlotCapacity} ô)" : string.Empty;

        ShowHint($"Kho đầy{sucChua} — chưa nhận được \"{ten}\". Bán bớt hoặc nâng cấp kho.");
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

    // ═════════════════════════════════════════════════════════════════════════
    //  THÔNG BÁO NGẮN CHO NGƯỜI CHƠI
    // ═════════════════════════════════════════════════════════════════════════
    //
    // 🔴 VÌ SAO PHẢI CÓ ĐƯỜNG DỰ PHÒNG: `txtHint` trong `SCN_Farm` đang là `{fileID: 0}`
    // (chưa gán). Bản cũ chỉ có đúng `if (txtHint != null)` nên **36 lời gọi ShowHint
    // trong toàn dự án đều CÂM** — người chơi bấm cây chín mà không thu được, kho đầy mà
    // không mua được, vào bếp khi chưa đủ cấp… đều không có một chữ giải thích nào.
    //
    // Hai thứ dựa vào đường này để hoạt động đúng:
    //   • A6 — cổng bếp khoá tới cấp 5, "kèm thông báo Cần cấp 5"
    //   • F8 — kho đầy thì chặn "VÀ BÁO RÕ"
    //
    // Nên ở đây: tìm lại ô chữ lúc chạy, không thấy thì tự dựng một dòng chữ tối giản.
    // Dựng UI lúc chạy vốn là thứ dự án này cấm, nhưng đây là LƯỚI AN TOÀN cho thông báo
    // lỗi — thà xấu còn hơn người chơi không hiểu vì sao thao tác của mình không ăn.

    private TMP_Text _hintFallback;
    private float    _hintClearAt;

    public void ShowHint(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        TMP_Text dich = txtHint != null ? txtHint : LayHoacDungOChuDuPhong();
        if (dich == null)
        {
            // Cùng đường cũng không dựng nổi (không có Canvas) — ít nhất đừng im lặng.
            Debug.LogWarning($"[FarmUI] Không hiển thị được thông báo: {message}");
            return;
        }

        dich.text = message;
        dich.gameObject.SetActive(true);
        _hintClearAt = Time.unscaledTime + 2.5f;
    }

    private bool _hintSearched;

    private TMP_Text LayHoacDungOChuDuPhong()
    {
        if (_hintFallback != null) return _hintFallback;
        if (_hintSearched) return null;
        _hintSearched = true;

        if (txtHint != null)
        {
            _hintFallback = txtHint;
            return _hintFallback;
        }

        Canvas hud = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        if (hud == null)
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c == null || !c.isRootCanvas) continue;
                if (hud == null || c.sortingOrder > hud.sortingOrder) hud = c;
            }
        }
        if (hud == null) return null;

        var go = new GameObject("Txt_Hint_DuPhong", typeof(RectTransform));
        go.transform.SetParent(hud.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 170f);
        rt.sizeDelta        = new Vector2(900f, 70f);

        _hintFallback                = go.AddComponent<TextMeshProUGUI>();
        _hintFallback.fontSize       = 34f;
        _hintFallback.alignment      = TextAlignmentOptions.Center;
        _hintFallback.color          = Color.white;
        _hintFallback.raycastTarget  = false;   // không được nuốt click của người chơi
        return _hintFallback;
    }

    private void LateUpdate()
    {
        // Tự tắt sau vài giây, nếu không dòng chữ nằm lì trên màn hình mãi mãi.
        if (_hintClearAt <= 0f || Time.unscaledTime < _hintClearAt) return;

        _hintClearAt = 0f;
        if (_hintFallback != null) _hintFallback.gameObject.SetActive(false);
        else if (txtHint  != null) txtHint.text = string.Empty;
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
                    Debug.LogWarning($"[FarmUI] Bỏ qua HideAllPopups cho '{popupObjectsToForceClose[i].name}' — đây là PermanentBuilding.");
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
                Debug.LogWarning($"[FarmUI] Parent bị tắt, bật lại: {p.name}");
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
            ShowHint($"Kéo hạt giống để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống để trồng.");
    }

    public void ShowPlantSelectForFlower(PlotController plot)
    {
        if (isCookingMode) return;

        HideAllPopups();

        if (popupSeedFlower == null)
        {
            Debug.LogError("[FarmUI] popupSeedFlower is NULL — kéo popup hoa vào Inspector.");
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
            ShowHint($"Kéo hạt giống hoa để trồng vào ô {plot.PlotId}");
        else
            ShowHint("Kéo hạt giống hoa để trồng.");
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
        // A6 — chốt cuối cùng của cổng bếp. `BuildingInteractable` đã kiểm một lần cho
        // đường click cổng ngoài world, nhưng hàm này còn được các nút HUD wire trực tiếp
        // vào (`AnimalGuideController` dò đúng listener "OnClick_GoCooking"). Chặn ở đây
        // thì KHÔNG đường nào lọt, kể cả nút mới thêm sau này.
        if (!CookingGateAccess.CanEnterOrWarn())
            return;

        if (SceneManager.GetSceneByName(cookingSceneName).isLoaded)
            return;

        EnterCookingMode();
        SceneTransitionManager.Instance.LoadScene(cookingSceneName, SceneTransitionManager.TransitionType.CloudWipe, LoadSceneMode.Additive);
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
