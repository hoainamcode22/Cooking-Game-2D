using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChickenPenPopupUI : MonoBehaviour
{
    private enum ChickenState
    {
        Idle,
        Growing,
        Harvesting,
        Ready
    }

    private enum FeedType
    {
        None,
        Rice,
        MixedFeed,
        Premium
    }

    [Serializable]
    private class ChickenRuntimeData
    {
        public bool isActive;
        public ChickenState state;
        public FeedType feedType;
        public float timer;
        public float phaseDuration;

        public bool meatCollected;
        public bool eggCollected;
    }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Header")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtLevel;

    [Header("Left Info")]
    [SerializeField] private TMP_Text txtChickenStatus;
    [SerializeField] private TMP_Text txtChickenCount;
    [SerializeField] private Image imgChickenIcon;

    [Header("Chicken Slots")]
    [SerializeField] private List<ChickenSlotUI> chickenSlots = new List<ChickenSlotUI>();

    [Header("Feed Section")]
    [SerializeField] private TMP_Text txtFeedTitle;

    [SerializeField] private TMP_Text txtRiceAmount;
    [SerializeField] private Button btnFeedRice;

    [SerializeField] private TMP_Text txtMixedAmount;
    [SerializeField] private Button btnFeedMixed;

    [SerializeField] private TMP_Text txtPremiumAmount;
    [SerializeField] private Button btnFeedPremium;

    [Header("Collect Meat")]
    [SerializeField] private TMP_Text txtCollectMeatAmount;
    [SerializeField] private Button btnCollectMeat;

    [Header("Collect Egg")]
    [SerializeField] private TMP_Text txtCollectEggAmount;
    [SerializeField] private Button btnCollectEgg;

    [Header("Upgrade Placeholder")]
    [SerializeField] private Button btnUpgradePen;
    [SerializeField] private TMP_Text txtUpgradeButton;

    [Header("Gameplay")]
    [SerializeField] private int startActiveChickenCount = 4;
    [SerializeField] private int maxChickenCount = 4;

    [Header("Feed IDs")]
    [SerializeField] private string riceItemId = "rice";
    [SerializeField] private string chickenMeatItemId = "chicken_meat";
    [SerializeField] private string eggItemId = "egg";

    [Header("Feed Cost")]
    [SerializeField] private int riceCostPerChicken = 1;
    [SerializeField] private int mixedCostPerChicken = 1;
    [SerializeField] private int premiumCostPerChicken = 1;

    [Header("Debug Feed Stock")]
    [SerializeField] private int mixedStockDebug = 10;
    [SerializeField] private int premiumStockDebug = 10;

    [Header("Durations - Rice")]
    [SerializeField] private float riceGrowthSeconds = 900f;
    [SerializeField] private float riceHarvestSeconds = 300f;

    [Header("Durations - Mixed")]
    [SerializeField] private float mixedGrowthSeconds = 300f;
    [SerializeField] private float mixedHarvestSeconds = 120f;

    [Header("Durations - Premium")]
    [SerializeField] private float premiumGrowthSeconds = 120f;
    [SerializeField] private float premiumHarvestSeconds = 60f;

    [Header("Yield")]
    [SerializeField] private int meatPerChicken = 1;
    [SerializeField] private int eggPerChicken = 1;

    private readonly List<ChickenRuntimeData> runtimeChickens = new List<ChickenRuntimeData>();

    private void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);

        if (btnFeedRice != null)
            btnFeedRice.onClick.AddListener(OnClickFeedRice);

        if (btnFeedMixed != null)
            btnFeedMixed.onClick.AddListener(OnClickFeedMixed);

        if (btnFeedPremium != null)
            btnFeedPremium.onClick.AddListener(OnClickFeedPremium);

        if (btnCollectMeat != null)
            btnCollectMeat.onClick.AddListener(OnClickCollectMeat);

        if (btnCollectEgg != null)
            btnCollectEgg.onClick.AddListener(OnClickCollectEgg);

        if (btnUpgradePen != null)
            btnUpgradePen.interactable = false;

        BuildInitialChickenData();

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (txtChickenStatus != null)
            txtChickenStatus.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    private void Update()
    {
        bool changed = false;

        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;

            if (chicken.state == ChickenState.Growing || chicken.state == ChickenState.Harvesting)
            {
                chicken.timer += Time.deltaTime;

                if (chicken.timer >= chicken.phaseDuration)
                {
                    chicken.timer = 0f;

                    if (chicken.state == ChickenState.Growing)
                    {
                        chicken.state = ChickenState.Harvesting;
                        chicken.phaseDuration = GetHarvestDuration(chicken.feedType);
                    }
                    else if (chicken.state == ChickenState.Harvesting)
                    {
                        chicken.state = ChickenState.Ready;
                        chicken.phaseDuration = 0f;
                    }
                }

                changed = true;
            }
        }

        if (changed)
            RefreshUI();
    }

    private void BuildInitialChickenData()
    {
        runtimeChickens.Clear();

        int total = Mathf.Max(maxChickenCount, chickenSlots.Count);
        int activeCount = Mathf.Clamp(startActiveChickenCount, 0, total);

        for (int i = 0; i < total; i++)
        {
            runtimeChickens.Add(new ChickenRuntimeData
            {
                isActive = i < activeCount,
                state = ChickenState.Idle,
                feedType = FeedType.None,
                timer = 0f,
                phaseDuration = 0f
            });
        }
    }

    public void OpenPopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshUI();
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnClickFeedRice()
    {
        FeedIdleChickens(FeedType.Rice);
    }

    private void OnClickFeedMixed()
    {
        FeedIdleChickens(FeedType.MixedFeed);
    }

    private void OnClickFeedPremium()
    {
        FeedIdleChickens(FeedType.Premium);
    }

    private void FeedIdleChickens(FeedType feedType)
    {
        int idleCount = GetIdleChickenCount();
        if (idleCount <= 0)
            return;

        int availableFood = GetAvailableFood(feedType);
        int costPerChicken = GetFeedCost(feedType);

        if (availableFood < costPerChicken)
            return;

        int feedableCount = Mathf.Min(idleCount, availableFood / costPerChicken);
        if (feedableCount <= 0)
            return;

        bool success = ConsumeFood(feedType, feedableCount * costPerChicken);
        if (!success)
            return;

        int fed = 0;
        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            if (fed >= feedableCount)
                break;

            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;
            if (chicken.state != ChickenState.Idle) continue;

            chicken.state = ChickenState.Growing;
            chicken.feedType = feedType;
            chicken.timer = 0f;
            chicken.phaseDuration = GetGrowthDuration(feedType);
            fed++;
        }

        RefreshUI();
    }
    // hàm click thu hoạch thịt gà, tính tổng số thịt thu hoạch được từ những con gà đang ở trạng thái Ready, cộng vào kho và đánh dấu đã thu hoạch thịt để tránh thu hoạch lại
    private void OnClickCollectMeat()
    {
        if (FarmInventoryManager.Instance == null)
            return;

        int totalMeat = 0;

        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;
            if (chicken.state != ChickenState.Ready) continue;
            if (chicken.meatCollected) continue;

            totalMeat += meatPerChicken;
            chicken.meatCollected = true;
        }

        if (totalMeat <= 0)
            return;

        FarmInventoryManager.Instance.AddItem(chickenMeatItemId, totalMeat);

        RefreshChickenReadyReset();
        RefreshUI();
    }
    // hàm click thu hoạch trứng, tính tổng số trứng thu hoạch được từ những con gà đang ở trạng thái Ready, cộng vào kho và đánh dấu đã thu hoạch trứng để tránh thu hoạch lại
    private void OnClickCollectEgg()
    {
        if (FarmInventoryManager.Instance == null)
            return;

        int totalEgg = 0;

        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;
            if (chicken.state != ChickenState.Ready) continue;
            if (chicken.eggCollected) continue;

            totalEgg += eggPerChicken;
            chicken.eggCollected = true;
        }

        if (totalEgg <= 0)
            return;

        FarmInventoryManager.Instance.AddItem(eggItemId, totalEgg);

        RefreshChickenReadyReset();
        RefreshUI();
    }

    // resert thịt và gà 
    private void RefreshChickenReadyReset()
    {
        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;
            if (chicken.state != ChickenState.Ready) continue;

            if (chicken.meatCollected && chicken.eggCollected)
            {
                chicken.state = ChickenState.Idle;
                chicken.feedType = FeedType.None;
                chicken.timer = 0f;
                chicken.phaseDuration = 0f;
                chicken.meatCollected = false;
                chicken.eggCollected = false;
            }
        }
    }
    public void RefreshUI()
    {
        RefreshHeader();
        RefreshFeedSection();
        RefreshCollectSection();
        RefreshChickenSlots();
    }

    private void RefreshHeader()
    {
        if (txtTitle != null)
            txtTitle.text = "CHUỒNG GÀ";

        if (txtLevel != null)
            txtLevel.text = "Cấp 1";

        if (txtChickenCount != null)
            txtChickenCount.text = $"{GetActiveChickenCount()}/{maxChickenCount}";
    }

    private void RefreshFeedSection()
    {
        if (txtFeedTitle != null)
            txtFeedTitle.text = "Cho ăn";

        int riceAmount = FarmInventoryManager.Instance != null
            ? FarmInventoryManager.Instance.GetAmount(riceItemId)
            : 0;

        if (txtRiceAmount != null) txtRiceAmount.text = "x" + riceAmount;
        if (txtMixedAmount != null) txtMixedAmount.text = "x" + mixedStockDebug;
        if (txtPremiumAmount != null) txtPremiumAmount.text = "x" + premiumStockDebug;

        bool hasIdleChicken = GetIdleChickenCount() > 0;

        if (btnFeedRice != null)
            btnFeedRice.interactable = hasIdleChicken && riceAmount >= riceCostPerChicken;

        if (btnFeedMixed != null)
            btnFeedMixed.interactable = hasIdleChicken && mixedStockDebug >= mixedCostPerChicken;

        if (btnFeedPremium != null)
            btnFeedPremium.interactable = hasIdleChicken && premiumStockDebug >= premiumCostPerChicken;
    }

    private void RefreshCollectSection()
    {
        int readyCount = GetReadyChickenCount();

        if (txtCollectMeatAmount != null)
            txtCollectMeatAmount.text = readyCount * meatPerChicken + "/" + (GetActiveChickenCount() * meatPerChicken);

        if (txtCollectEggAmount != null)
            txtCollectEggAmount.text = readyCount * eggPerChicken + "/" + (GetActiveChickenCount() * eggPerChicken);

        if (btnCollectMeat != null)
            btnCollectMeat.interactable = readyCount > 0;

        if (btnCollectEgg != null)
            btnCollectEgg.interactable = readyCount > 0;

        if (txtUpgradeButton != null)
            txtUpgradeButton.text = "NÂNG CẤP CHUỒNG";
    }

    private void RefreshChickenSlots()
    {
        for (int i = 0; i < chickenSlots.Count; i++)
        {
            ChickenSlotUI slotUI = chickenSlots[i];
            if (slotUI == null) continue;

            if (i >= runtimeChickens.Count)
            {
                slotUI.SetInactive();
                continue;
            }

            ChickenRuntimeData chicken = runtimeChickens[i];

            if (!chicken.isActive)
            {
                slotUI.SetInactive();
                continue;
            }

            switch (chicken.state)
            {
                case ChickenState.Idle:
                    slotUI.SetIdle();
                    break;

                case ChickenState.Growing:
                    float growProgress = chicken.phaseDuration > 0f ? chicken.timer / chicken.phaseDuration : 0f;
                    float growLeft = Mathf.Max(0f, chicken.phaseDuration - chicken.timer);
                    slotUI.SetGrowing(growProgress, growLeft);
                    break;

                case ChickenState.Harvesting:
                    float harvestProgress = chicken.phaseDuration > 0f ? chicken.timer / chicken.phaseDuration : 0f;
                    float harvestLeft = Mathf.Max(0f, chicken.phaseDuration - chicken.timer);
                    slotUI.SetHarvesting(harvestProgress, harvestLeft);
                    break;

                case ChickenState.Ready:
                    slotUI.SetReady();
                    break;
            }
        }
    }

    private int GetActiveChickenCount()
    {
        int count = 0;
        for (int i = 0; i < runtimeChickens.Count; i++)
            if (runtimeChickens[i].isActive) count++;
        return count;
    }

    private int GetIdleChickenCount()
    {
        return CountByState(ChickenState.Idle);
    }

    private int GetReadyChickenCount()
    {
        return CountByState(ChickenState.Ready);
    }

    private int CountByState(ChickenState state)
    {
        int count = 0;
        for (int i = 0; i < runtimeChickens.Count; i++)
        {
            ChickenRuntimeData chicken = runtimeChickens[i];
            if (!chicken.isActive) continue;
            if (chicken.state == state) count++;
        }
        return count;
    }

    private float GetGrowthDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceGrowthSeconds;
            case FeedType.MixedFeed: return mixedGrowthSeconds;
            case FeedType.Premium: return premiumGrowthSeconds;
            default: return riceGrowthSeconds;
        }
    }

    private float GetHarvestDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceHarvestSeconds;
            case FeedType.MixedFeed: return mixedHarvestSeconds;
            case FeedType.Premium: return premiumHarvestSeconds;
            default: return riceHarvestSeconds;
        }
    }

    private int GetFeedCost(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceCostPerChicken;
            case FeedType.MixedFeed: return mixedCostPerChicken;
            case FeedType.Premium: return premiumCostPerChicken;
            default: return 1;
        }
    }

    private int GetAvailableFood(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice:
                return FarmInventoryManager.Instance != null
                    ? FarmInventoryManager.Instance.GetAmount(riceItemId)
                    : 0;
            case FeedType.MixedFeed:
                return mixedStockDebug;
            case FeedType.Premium:
                return premiumStockDebug;
            default:
                return 0;
        }
    }

    private bool ConsumeFood(FeedType feedType, int amount)
    {
        if (amount <= 0)
            return false;

        switch (feedType)
        {
            case FeedType.Rice:
                return FarmInventoryManager.Instance != null &&
                       FarmInventoryManager.Instance.RemoveItem(riceItemId, amount);

            case FeedType.MixedFeed:
                if (mixedStockDebug < amount) return false;
                mixedStockDebug -= amount;
                return true;

            case FeedType.Premium:
                if (premiumStockDebug < amount) return false;
                premiumStockDebug -= amount;
                return true;
        }

        return false;
    }
}