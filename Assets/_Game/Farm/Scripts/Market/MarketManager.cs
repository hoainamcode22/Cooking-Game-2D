using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private MarketDatabase_SO marketDatabase;
    [SerializeField] private int itemCountPerRefresh = 10;

    [Header("Visual Databases")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();
    [SerializeField] private List<InventoryItemData> itemDatabase = new List<InventoryItemData>();

    [Header("Refresh")]
    [SerializeField] private float refreshDurationSeconds = 300f;
    [SerializeField] private int gemRefreshCost = 1;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private MarketShopItemUI shopItemPrefab;
    [SerializeField] private Text textTimer;
    [SerializeField] private Image fillBarTimer;
    [SerializeField] private Button buttonRefreshFree;
    [SerializeField] private Button buttonRefreshGem;
    [SerializeField] private Button buttonClose;
    [SerializeField] private GameObject popupRoot;

    private readonly List<MarketShopItemUI> spawnedItems = new List<MarketShopItemUI>();
    private readonly Dictionary<string, MarketItemVisual> visualLookup = new Dictionary<string, MarketItemVisual>();
    private Coroutine refreshCoroutine;
    private Coroutine openAnimationCoroutine;
    private float timeRemaining;
    private bool hasLoadedOnce;
    private bool popupInputLockHeld;

    private struct MarketItemVisual
    {
        public string DisplayName;
        public Sprite Icon;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (buttonRefreshFree != null)
        {
            buttonRefreshFree.onClick.RemoveAllListeners();
            buttonRefreshFree.onClick.AddListener(RefreshNowFree);
        }

        if (buttonRefreshGem != null)
        {
            buttonRefreshGem.onClick.RemoveAllListeners();
            buttonRefreshGem.onClick.AddListener(RefreshNowWithGems);
        }

        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveAllListeners();
            buttonClose.onClick.AddListener(CloseMarketPopup);
        }

        BuildVisualLookup();
    }

    private void OnEnable()
    {
        if (IsOpen)
            AcquirePopupInputBlock();

        if (!hasLoadedOnce)
            StartRefreshCycle(true);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();

        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
            refreshCoroutine = null;
        }
    }

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    public void OpenMarketPopup()
    {
        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        AcquirePopupInputBlock();
        PlayOpenAnimation();

        if (!hasLoadedOnce)
            StartRefreshCycle(true);
    }

    public void CloseMarketPopup()
    {
        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void LoadData()
    {
        ClearItems();

        if (content == null || shopItemPrefab == null)
        {
            return;
        }

        List<MarketItemDef> source = GetValidItems();
        Shuffle(source);
        BuildVisualLookup();

        int count = Mathf.Min(itemCountPerRefresh, source.Count);
        for (int i = 0; i < count; i++)
        {
            MarketItemDef def = source[i];
            int minQuantity = Mathf.Max(1, def.MinQuantity);
            int maxQuantity = Mathf.Max(minQuantity, def.MaxQuantity);
            int quantity = Random.Range(minQuantity, maxQuantity + 1);
            MarketItemVisual visual = ResolveVisual(def.ItemID);

            MarketShopItemUI itemUI = Instantiate(shopItemPrefab, content);
            itemUI.gameObject.SetActive(true);
            itemUI.Setup(this, def, quantity, visual.Icon, visual.DisplayName);
            spawnedItems.Add(itemUI);
        }

        hasLoadedOnce = true;
    }

    public void TryBuy(MarketShopItemUI itemUI, string itemID, int quantity, int totalPrice)
    {
        if (itemUI == null || string.IsNullOrEmpty(itemID) || quantity <= 0)
            return;

        if (!CanSpendGold(totalPrice))
        {
            return;
        }

        if (FarmInventoryManager.Instance == null)
        {
            return;
        }

        SpendGold(totalPrice);
        FarmInventoryManager.Instance.AddItem(itemID, quantity);

        // Tiến độ nhiệm vụ: chợ hiện bán nông sản/nguyên liệu; nếu sau này bán hạt giống
        // (itemID dạng "seed_*") thì tự tính vào BuySeed
        if (itemID.StartsWith("seed", System.StringComparison.OrdinalIgnoreCase))
            MissionProgressTracker.ReportEvent(MissionEventType.BuySeed, itemID, quantity);

        itemUI.MarkSoldOut();
    }

    public void RefreshNowFree()
    {
        StartRefreshCycle(true);
    }

    public void RefreshNowWithGems()
    {
        if (!CanSpendGems(gemRefreshCost))
        {
            return;
        }

        SpendGems(gemRefreshCost);
        StartRefreshCycle(true);
    }

    private void StartRefreshCycle(bool reloadItems)
    {
        if (reloadItems)
            LoadData();

        timeRemaining = Mathf.Max(1f, refreshDurationSeconds);
        UpdateTimerUI();

        if (refreshCoroutine != null)
            StopCoroutine(refreshCoroutine);

        refreshCoroutine = StartCoroutine(RefreshCountdown());
    }

    private IEnumerator RefreshCountdown()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        StartRefreshCycle(true);
    }

    private void UpdateTimerUI()
    {
        float clampedTime = Mathf.Max(0f, timeRemaining);
        int minutes = Mathf.FloorToInt(clampedTime / 60f);
        int seconds = Mathf.FloorToInt(clampedTime % 60f);

        if (textTimer != null)
            textTimer.text = minutes.ToString("00") + ":" + seconds.ToString("00");

        if (fillBarTimer != null)
            fillBarTimer.fillAmount = refreshDurationSeconds <= 0f ? 0f : clampedTime / refreshDurationSeconds;
    }

    private List<MarketItemDef> GetValidItems()
    {
        List<MarketItemDef> result = new List<MarketItemDef>();
        if (marketDatabase == null)
            return result;

        IReadOnlyList<MarketItemDef> items = marketDatabase.Items;
        for (int i = 0; i < items.Count; i++)
        {
            MarketItemDef item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.ItemID))
                continue;

            if (item.BuyPrice < 0 || item.MaxQuantity <= 0)
                continue;

            result.Add(item);
        }

        return result;
    }

    private void ClearItems()
    {
        spawnedItems.Clear();

        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private MarketItemVisual ResolveVisual(string itemID)
    {
        string key = NormalizeKey(itemID);
        if (!string.IsNullOrEmpty(key) && visualLookup.TryGetValue(key, out MarketItemVisual visual))
            return visual;

        return new MarketItemVisual
        {
            DisplayName = itemID,
            Icon = null
        };
    }

    private bool CanSpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (FarmEconomyManager.Instance != null)
            return FarmEconomyManager.Instance.Gold >= amount;

        return true;
    }

    private void SpendGold(int amount)
    {
        if (amount <= 0)
            return;

        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.SpendGold(amount);
    }

    private bool CanSpendGems(int amount)
    {
        if (amount <= 0)
            return true;

        if (FarmEconomyManager.Instance != null)
            return FarmEconomyManager.Instance.Gems >= amount;

        return true;
    }

    private void SpendGems(int amount)
    {
        if (amount <= 0)
            return;

        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.SpendGems(amount);
    }

    private void ClosePopup()
    {
        CloseMarketPopup();
    }

    private void EnsurePopupRaycastBlock()
    {
        if (popupRoot == null)
            return;

        Canvas parentCanvas = popupRoot.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, true);

        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
    }

    private void AcquirePopupInputBlock()
    {
        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        FarmInputLock.IsMarketPopupOpen = true;
        EnsurePopupRaycastBlock();

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.IsMarketPopupOpen = false;
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        Canvas parentCanvas = popupRoot != null ? popupRoot.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, false);

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }

    private void BuildVisualLookup()
    {
        visualLookup.Clear();

        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null)
                continue;

            MarketItemVisual visual = new MarketItemVisual
            {
                DisplayName = string.IsNullOrEmpty(crop.displayName) ? crop.cropId : crop.displayName,
                Icon = crop.icon != null ? crop.icon : crop.harvestIcon
            };

            AddVisual(crop.itemID, visual);
            AddVisual(crop.seedItemId, visual);
            AddVisual(crop.harvestItemId, visual);
            AddVisual(crop.cropId, visual);
        }

        for (int i = 0; i < itemDatabase.Count; i++)
        {
            InventoryItemData item = itemDatabase[i];
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            AddVisual(item.itemId, new MarketItemVisual
            {
                DisplayName = string.IsNullOrEmpty(item.displayName) ? item.itemId : item.displayName,
                Icon = item.icon
            });
        }
    }

    private void AddVisual(string itemID, MarketItemVisual visual)
    {
        string key = NormalizeKey(itemID);
        if (string.IsNullOrEmpty(key) || visualLookup.ContainsKey(key))
            return;

        visualLookup.Add(key, visual);
    }

    private void PlayOpenAnimation()
    {
        if (openAnimationCoroutine != null)
            StopCoroutine(openAnimationCoroutine);

        openAnimationCoroutine = StartCoroutine(OpenScaleRoutine(transform));
    }

    private IEnumerator OpenScaleRoutine(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 startScale = Vector3.one * 0.92f;
        Vector3 endScale = Vector3.one;
        float duration = 0.12f;
        float elapsed = 0f;

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
        openAnimationCoroutine = null;
    }

    private static string NormalizeKey(string key)
    {
        return key == null ? string.Empty : key.Trim().ToLower();
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
