using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CowPenPopupUI : MonoBehaviour
{
    private enum CowState
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
        Corn,
        Premium
    }

    [Serializable]
    private class CowRuntimeData
    {
        public bool isActive;
        public CowState state;
        public FeedType feedType;
        public float timer;
        public float phaseDuration;
    }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;   // kéo Panel
    [SerializeField] private Button btnClose;

    [Header("Header")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtLevel;

    [Header("Left Info")]
    [SerializeField] private TMP_Text txtCowStatus;  // sẽ ẩn
    [SerializeField] private TMP_Text txtCowCount;
    [SerializeField] private Image imgCowIcon;

    [Header("Cow Slots")]
    [SerializeField] private List<CowSlotUI> cowSlots = new List<CowSlotUI>();

    [Header("Feed Section")]
    [SerializeField] private TMP_Text txtFeedTitle;

    [SerializeField] private TMP_Text txtRiceAmount;
    [SerializeField] private Button btnFeedRice;

    [SerializeField] private TMP_Text txtCornAmount;
    [SerializeField] private Button btnFeedCorn;

    [SerializeField] private TMP_Text txtPremiumAmount;
    [SerializeField] private Button btnFeedPremium;

    [Header("Collect Section")]
    [SerializeField] private TMP_Text txtCollectTitle;
    [SerializeField] private TMP_Text txtCollectAmount;
    [SerializeField] private Button btnCollect;

    [Header("Upgrade Placeholder")]
    [SerializeField] private Button btnUpgradePen;
    [SerializeField] private TMP_Text txtUpgradeButton;

    [Header("Gameplay")]
    [SerializeField] private int startActiveCowCount = 4;
    [SerializeField] private int maxCowCount = 4;

    [Header("Feed IDs")]
    [SerializeField] private string riceItemId = "rice";
    [SerializeField] private string beefItemId = "beef";

    [Header("Feed Cost")]
    [SerializeField] private int riceCostPerCow = 1;
    [SerializeField] private int cornCostPerCow = 1;
    [SerializeField] private int premiumCostPerCow = 1;

    [Header("Debug Feed Stock")]
    [SerializeField] private int cornStockDebug = 5;
    [SerializeField] private int premiumStockDebug = 5;

    [Header("Durations - Rice (slow)")]
    [SerializeField] private float riceGrowthSeconds = 900f;    // 15 phút
    [SerializeField] private float riceHarvestSeconds = 300f;   // 5 phút

    [Header("Durations - Corn (faster)")]
    [SerializeField] private float cornGrowthSeconds = 300f;    // 5 phút
    [SerializeField] private float cornHarvestSeconds = 120f;   // 2 phút

    [Header("Durations - Premium (fastest)")]
    [SerializeField] private float premiumGrowthSeconds = 120f; // 2 phút
    [SerializeField] private float premiumHarvestSeconds = 60f; // 1 phút

    [Header("Yield")]
    [SerializeField] private int meatPerCow = 1;

    private readonly List<CowRuntimeData> runtimeCows = new List<CowRuntimeData>();

    private void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);

        if (btnFeedRice != null)
            btnFeedRice.onClick.AddListener(OnClickFeedRice);

        if (btnFeedCorn != null)
            btnFeedCorn.onClick.AddListener(OnClickFeedCorn);

        if (btnFeedPremium != null)
            btnFeedPremium.onClick.AddListener(OnClickFeedPremium);

        if (btnCollect != null)
            btnCollect.onClick.AddListener(OnClickCollect);

        if (btnUpgradePen != null)
            btnUpgradePen.interactable = false;

        BuildInitialCowData();

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (txtCowStatus != null)
            txtCowStatus.gameObject.SetActive(false); // ẩn hẳn
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

        for (int i = 0; i < runtimeCows.Count; i++)
        {
            CowRuntimeData cow = runtimeCows[i];
            if (!cow.isActive) continue;

            if (cow.state == CowState.Growing || cow.state == CowState.Harvesting)
            {
                cow.timer += Time.deltaTime;

                if (cow.timer >= cow.phaseDuration)
                {
                    cow.timer = 0f;

                    if (cow.state == CowState.Growing)
                    {
                        cow.state = CowState.Harvesting;
                        cow.phaseDuration = GetHarvestDuration(cow.feedType);
                    }
                    else if (cow.state == CowState.Harvesting)
                    {
                        cow.state = CowState.Ready;
                        cow.phaseDuration = 0f;
                    }
                }

                changed = true;
            }
        }

        if (changed)
            RefreshUI();
    }

    private void BuildInitialCowData()
    {
        runtimeCows.Clear();

        int total = Mathf.Max(maxCowCount, cowSlots.Count);
        int activeCount = Mathf.Clamp(startActiveCowCount, 0, total);

        for (int i = 0; i < total; i++)
        {
            runtimeCows.Add(new CowRuntimeData
            {
                isActive = i < activeCount,
                state = CowState.Idle,
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
        FeedIdleCows(FeedType.Rice);
    }

    private void OnClickFeedCorn()
    {
        FeedIdleCows(FeedType.Corn);
    }

    private void OnClickFeedPremium()
    {
        FeedIdleCows(FeedType.Premium);
    }

    private void FeedIdleCows(FeedType feedType)
    {
        int idleCount = GetIdleCowCount();
        if (idleCount <= 0)
            return;

        int availableFood = GetAvailableFood(feedType);
        int costPerCow = GetFeedCost(feedType);

        if (availableFood < costPerCow)
            return;

        int feedableCount = Mathf.Min(idleCount, availableFood / costPerCow);
        if (feedableCount <= 0)
            return;

        bool success = ConsumeFood(feedType, feedableCount * costPerCow);
        if (!success)
            return;

        int fed = 0;
        for (int i = 0; i < runtimeCows.Count; i++)
        {
            if (fed >= feedableCount)
                break;

            CowRuntimeData cow = runtimeCows[i];
            if (!cow.isActive) continue;
            if (cow.state != CowState.Idle) continue;

            cow.state = CowState.Growing;
            cow.feedType = feedType;
            cow.timer = 0f;
            cow.phaseDuration = GetGrowthDuration(feedType);
            fed++;
        }

        RefreshUI();
    }

    private void OnClickCollect()
    {
        if (FarmInventoryManager.Instance == null)
            return;

        int readyCount = GetReadyCowCount();
        if (readyCount <= 0)
            return;

        int totalMeat = readyCount * meatPerCow;
        FarmInventoryManager.Instance.AddItem(beefItemId, totalMeat);

        for (int i = 0; i < runtimeCows.Count; i++)
        {
            CowRuntimeData cow = runtimeCows[i];
            if (!cow.isActive) continue;

            if (cow.state == CowState.Ready)
            {
                cow.state = CowState.Idle;
                cow.feedType = FeedType.None;
                cow.timer = 0f;
                cow.phaseDuration = 0f;
            }
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshHeader();
        RefreshFeedSection();
        RefreshCollectSection();
        RefreshCowSlots();
    }

    private void RefreshHeader()
    {
        if (txtTitle != null)
            txtTitle.text = "CHUỒNG BÒ";

        if (txtLevel != null)
            txtLevel.text = "Cấp 1";

        if (txtCowCount != null)
            txtCowCount.text = $"{GetActiveCowCount()}/{maxCowCount}";
    }

    private void RefreshFeedSection()
    {
        if (txtFeedTitle != null)
            txtFeedTitle.text = "Cho ăn";

        int riceAmount = FarmInventoryManager.Instance != null
            ? FarmInventoryManager.Instance.GetAmount(riceItemId)
            : 0;

        if (txtRiceAmount != null) txtRiceAmount.text = "x" + riceAmount;
        if (txtCornAmount != null) txtCornAmount.text = "x" + cornStockDebug;
        if (txtPremiumAmount != null) txtPremiumAmount.text = "x" + premiumStockDebug;

        bool hasIdleCow = GetIdleCowCount() > 0;

        if (btnFeedRice != null)
            btnFeedRice.interactable = hasIdleCow && riceAmount >= riceCostPerCow;

        if (btnFeedCorn != null)
            btnFeedCorn.interactable = hasIdleCow && cornStockDebug >= cornCostPerCow;

        if (btnFeedPremium != null)
            btnFeedPremium.interactable = hasIdleCow && premiumStockDebug >= premiumCostPerCow;
    }

    private void RefreshCollectSection()
    {
        if (txtCollectTitle != null)
            txtCollectTitle.text = "Thu thập";

        int readyCount = GetReadyCowCount();
        int totalMeat = readyCount * meatPerCow;

        if (txtCollectAmount != null)
            txtCollectAmount.text = "x" + totalMeat;

        if (btnCollect != null)
            btnCollect.interactable = readyCount > 0;

        if (txtUpgradeButton != null)
            txtUpgradeButton.text = "NÂNG CẤP CHUỒNG";
    }

    private void RefreshCowSlots()
    {
        for (int i = 0; i < cowSlots.Count; i++)
        {
            CowSlotUI slotUI = cowSlots[i];
            if (slotUI == null) continue;

            if (i >= runtimeCows.Count)
            {
                slotUI.SetInactive();
                continue;
            }

            CowRuntimeData cow = runtimeCows[i];

            if (!cow.isActive)
            {
                slotUI.SetInactive();
                continue;
            }

            switch (cow.state)
            {
                case CowState.Idle:
                    slotUI.SetIdle();
                    break;

                case CowState.Growing:
                    float growProgress = cow.phaseDuration > 0f ? cow.timer / cow.phaseDuration : 0f;
                    float growLeft = Mathf.Max(0f, cow.phaseDuration - cow.timer);
                    slotUI.SetGrowing(growProgress, growLeft);
                    break;

                case CowState.Harvesting:
                    float harvestProgress = cow.phaseDuration > 0f ? cow.timer / cow.phaseDuration : 0f;
                    float harvestLeft = Mathf.Max(0f, cow.phaseDuration - cow.timer);
                    slotUI.SetHarvesting(harvestProgress, harvestLeft);
                    break;

                case CowState.Ready:
                    slotUI.SetReady();
                    break;
            }
        }
    }

    private int GetActiveCowCount()
    {
        int count = 0;
        for (int i = 0; i < runtimeCows.Count; i++)
            if (runtimeCows[i].isActive) count++;
        return count;
    }

    private int GetIdleCowCount()
    {
        return CountByState(CowState.Idle);
    }

    private int GetReadyCowCount()
    {
        return CountByState(CowState.Ready);
    }

    private int CountByState(CowState state)
    {
        int count = 0;
        for (int i = 0; i < runtimeCows.Count; i++)
        {
            CowRuntimeData cow = runtimeCows[i];
            if (!cow.isActive) continue;
            if (cow.state == state) count++;
        }
        return count;
    }

    private float GetGrowthDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceGrowthSeconds;
            case FeedType.Corn: return cornGrowthSeconds;
            case FeedType.Premium: return premiumGrowthSeconds;
            default: return riceGrowthSeconds;
        }
    }

    private float GetHarvestDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceHarvestSeconds;
            case FeedType.Corn: return cornHarvestSeconds;
            case FeedType.Premium: return premiumHarvestSeconds;
            default: return riceHarvestSeconds;
        }
    }

    private int GetFeedCost(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Rice: return riceCostPerCow;
            case FeedType.Corn: return cornCostPerCow;
            case FeedType.Premium: return premiumCostPerCow;
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
            case FeedType.Corn:
                return cornStockDebug;
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

            case FeedType.Corn:
                if (cornStockDebug < amount) return false;
                cornStockDebug -= amount;
                return true;

            case FeedType.Premium:
                if (premiumStockDebug < amount) return false;
                premiumStockDebug -= amount;
                return true;
        }

        return false;
    }
}