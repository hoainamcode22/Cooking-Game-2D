using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PigPenPopupUI : MonoBehaviour
{
    private enum PigState
    {
        Idle,
        Growing,
        Harvesting,
        Ready
    }

    private enum FeedType
    {
        None,
        Corn,
        Vegetable,
        Premium
    }

    [Serializable]
    private class PigRuntimeData
    {
        public bool isActive;
        public PigState state;
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
    [SerializeField] private TMP_Text txtPigStatus;
    [SerializeField] private TMP_Text txtPigCount;
    [SerializeField] private Image imgPigIcon;

    [Header("Pig Slots")]
    [SerializeField] private List<PigSlotUI> pigSlots = new List<PigSlotUI>();

    [Header("Feed Section")]
    [SerializeField] private TMP_Text txtFeedTitle;

    [SerializeField] private TMP_Text txtCornAmount;
    [SerializeField] private Button btnFeedCorn;

    [SerializeField] private TMP_Text txtVegetableAmount;
    [SerializeField] private Button btnFeedVegetable;

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
    [SerializeField] private int startActivePigCount = 4;
    [SerializeField] private int maxPigCount = 4;

    [Header("Feed IDs")]
    [SerializeField] private string cornItemId = "corn";
    [SerializeField] private string porkItemId = "pork";

    [Header("Feed Cost")]
    [SerializeField] private int cornCostPerPig = 1;
    [SerializeField] private int vegetableCostPerPig = 1;
    [SerializeField] private int premiumCostPerPig = 1;

    [Header("Debug Feed Stock")]
    [SerializeField] private int vegetableStockDebug = 10;
    [SerializeField] private int premiumStockDebug = 10;

    [Header("Durations - Corn")]
    [SerializeField] private float cornGrowthSeconds = 900f;
    [SerializeField] private float cornHarvestSeconds = 300f;

    [Header("Durations - Vegetable")]
    [SerializeField] private float vegetableGrowthSeconds = 300f;
    [SerializeField] private float vegetableHarvestSeconds = 120f;

    [Header("Durations - Premium")]
    [SerializeField] private float premiumGrowthSeconds = 120f;
    [SerializeField] private float premiumHarvestSeconds = 60f;

    [Header("Yield")]
    [SerializeField] private int porkPerPig = 1;

    private readonly List<PigRuntimeData> runtimePigs = new List<PigRuntimeData>();

    private void Awake()
    {
        // gắn nút đóng popup
        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);
        // gắn nút cho ăn bắp
        if (btnFeedCorn != null)
            btnFeedCorn.onClick.AddListener(OnClickFeedCorn);
        // gắn nút cho ăn rau củ
        if (btnFeedVegetable != null)
            btnFeedVegetable.onClick.AddListener(OnClickFeedVegetable);
        // gắn nút cho ăn thức ăn cao cấp
        if (btnFeedPremium != null)
            btnFeedPremium.onClick.AddListener(OnClickFeedPremium);
        // gắn nút thu hoạch
        if (btnCollect != null)
            btnCollect.onClick.AddListener(OnClickCollect);
        // nút nâng cấp chuồng 
        if (btnUpgradePen != null)
            btnUpgradePen.interactable = false;

        BuildInitialPigData();

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (txtPigStatus != null)
            txtPigStatus.gameObject.SetActive(false);
    }
    // đăng ký sự kiện thay đổi kho để cập nhật UI khi thức ăn thay đổi
    private void Start()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }
    // hủy đăng ký sự kiện khi popup bị hủy để tránh lỗi tham chiếu sau này
    private void OnDestroy()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }
    // cập nhật trạng thái của từng con heo mỗi khung hình
    private void Update()
    {
        bool changed = false;

        for (int i = 0; i < runtimePigs.Count; i++)
        {
            PigRuntimeData pig = runtimePigs[i];
            if (!pig.isActive) continue;

            if (pig.state == PigState.Growing || pig.state == PigState.Harvesting)
            {
                pig.timer += Time.deltaTime;

                if (pig.timer >= pig.phaseDuration)
                {
                    pig.timer = 0f;

                    if (pig.state == PigState.Growing)
                    {
                        pig.state = PigState.Harvesting;
                        pig.phaseDuration = GetHarvestDuration(pig.feedType);
                    }
                    else if (pig.state == PigState.Harvesting)
                    {
                        pig.state = PigState.Ready;
                        pig.phaseDuration = 0f;
                    }
                }

                changed = true;
            }
        }

        if (changed)
            RefreshUI();
    }
    // khởi tạo dữ liệu heo ban đầu dựa trên số lượng bắt đầu và tối đa, mặc định là 4 con heo đang chờ ăn
    private void BuildInitialPigData()
    {
        runtimePigs.Clear();

        int total = Mathf.Max(maxPigCount, pigSlots.Count);
        int activeCount = Mathf.Clamp(startActivePigCount, 0, total);

        for (int i = 0; i < total; i++)
        {
            runtimePigs.Add(new PigRuntimeData
            {
                isActive = i < activeCount,
                state = PigState.Idle,
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

    private void OnClickFeedCorn()
    {
        FeedIdlePigs(FeedType.Corn);
    }

    private void OnClickFeedVegetable()
    {
        FeedIdlePigs(FeedType.Vegetable);
    }

    private void OnClickFeedPremium()
    {
        FeedIdlePigs(FeedType.Premium);
    }
    // hàm dùng chung để cho ăn heo, sẽ kiểm tra số lượng heo đang chờ ăn, thức ăn có đủ không, sau đó cập nhật trạng thái của heo và trừ thức ăn
    private void FeedIdlePigs(FeedType feedType)
    {
        int idleCount = GetIdlePigCount();
        if (idleCount <= 0)
            return;

        int availableFood = GetAvailableFood(feedType);
        int costPerPig = GetFeedCost(feedType);

        if (availableFood < costPerPig)
            return;

        int feedableCount = Mathf.Min(idleCount, availableFood / costPerPig);
        if (feedableCount <= 0)
            return;

        bool success = ConsumeFood(feedType, feedableCount * costPerPig);
        if (!success)
            return;

        int fed = 0;
        for (int i = 0; i < runtimePigs.Count; i++)
        {
            if (fed >= feedableCount)
                break;

            PigRuntimeData pig = runtimePigs[i];
            if (!pig.isActive) continue;
            if (pig.state != PigState.Idle) continue;

            pig.state = PigState.Growing;
            pig.feedType = feedType;
            pig.timer = 0f;
            pig.phaseDuration = GetGrowthDuration(feedType);
            fed++;
        }

        RefreshUI();
    }
    // hàm xử lý khi nhấn nút thu hoạch, sẽ tính số lượng heo đã sẵn sàng, cộng tổng số thịt thu được vào kho, sau đó đặt lại trạng thái của những con heo đó về chờ ăn
    private void OnClickCollect()
    {
        if (FarmInventoryManager.Instance == null)
            return;

        int readyCount = GetReadyPigCount();
        if (readyCount <= 0)
            return;

        int totalPork = readyCount * porkPerPig;
        FarmInventoryManager.Instance.AddItem(porkItemId, totalPork);

        for (int i = 0; i < runtimePigs.Count; i++)
        {
            PigRuntimeData pig = runtimePigs[i];
            if (!pig.isActive) continue;

            if (pig.state == PigState.Ready)
            {
                pig.state = PigState.Idle;
                pig.feedType = FeedType.None;
                pig.timer = 0f;
                pig.phaseDuration = 0f;
            }
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshHeader();
        RefreshFeedSection();
        RefreshCollectSection();
        RefreshPigSlots();
    }

    private void RefreshHeader()
    {
        if (txtTitle != null)
            txtTitle.text = "CHUỒNG HEO";

        if (txtLevel != null)
            txtLevel.text = "Cấp 1";

        if (txtPigCount != null)
            txtPigCount.text = $"{GetActivePigCount()}/{maxPigCount}";
    }
    // hàm cập nhật phần cho ăn, sẽ hiển thị số lượng thức ăn hiện có, và bật/tắt tương tác của các nút cho ăn dựa trên việc có heo nào đang chờ ăn hay không và thức ăn có đủ không
    private void RefreshFeedSection()
    {
        if (txtFeedTitle != null)
            txtFeedTitle.text = "Cho ăn";

        int cornAmount = FarmInventoryManager.Instance != null
            ? FarmInventoryManager.Instance.GetAmount(cornItemId)
            : 0;

        if (txtCornAmount != null) txtCornAmount.text = "x" + cornAmount;
        if (txtVegetableAmount != null) txtVegetableAmount.text = "x" + vegetableStockDebug;
        if (txtPremiumAmount != null) txtPremiumAmount.text = "x" + premiumStockDebug;

        bool hasIdlePig = GetIdlePigCount() > 0;

        if (btnFeedCorn != null)
            btnFeedCorn.interactable = hasIdlePig && cornAmount >= cornCostPerPig;

        if (btnFeedVegetable != null)
            btnFeedVegetable.interactable = hasIdlePig && vegetableStockDebug >= vegetableCostPerPig;

        if (btnFeedPremium != null)
            btnFeedPremium.interactable = hasIdlePig && premiumStockDebug >= premiumCostPerPig;
    }

    private void RefreshCollectSection()
    {
        if (txtCollectTitle != null)
            txtCollectTitle.text = "Thu thập";

        int readyCount = GetReadyPigCount();
        int totalPork = readyCount * porkPerPig;

        if (txtCollectAmount != null)
            txtCollectAmount.text = "x" + totalPork;

        if (btnCollect != null)
            btnCollect.interactable = readyCount > 0;

        if (txtUpgradeButton != null)
            txtUpgradeButton.text = "NÂNG CẤP CHUỒNG";
    }

    private void RefreshPigSlots()
    {
        for (int i = 0; i < pigSlots.Count; i++)
        {
            PigSlotUI slotUI = pigSlots[i];
            if (slotUI == null) continue;

            if (i >= runtimePigs.Count)
            {
                slotUI.SetInactive();
                continue;
            }

            PigRuntimeData pig = runtimePigs[i];

            if (!pig.isActive)
            {
                slotUI.SetInactive();
                continue;
            }

            switch (pig.state)
            {
                case PigState.Idle:
                    slotUI.SetIdle();
                    break;

                case PigState.Growing:
                    float growProgress = pig.phaseDuration > 0f ? pig.timer / pig.phaseDuration : 0f;
                    float growLeft = Mathf.Max(0f, pig.phaseDuration - pig.timer);
                    slotUI.SetGrowing(growProgress, growLeft);
                    break;

                case PigState.Harvesting:
                    float harvestProgress = pig.phaseDuration > 0f ? pig.timer / pig.phaseDuration : 0f;
                    float harvestLeft = Mathf.Max(0f, pig.phaseDuration - pig.timer);
                    slotUI.SetHarvesting(harvestProgress, harvestLeft);
                    break;

                case PigState.Ready:
                    slotUI.SetReady();
                    break;
            }
        }
    }

    private int GetActivePigCount()
    {
        int count = 0;
        for (int i = 0; i < runtimePigs.Count; i++)
            if (runtimePigs[i].isActive) count++;
        return count;
    }

    private int GetIdlePigCount()
    {
        return CountByState(PigState.Idle);
    }

    private int GetReadyPigCount()
    {
        return CountByState(PigState.Ready);
    }

    private int CountByState(PigState state)
    {
        int count = 0;
        for (int i = 0; i < runtimePigs.Count; i++)
        {
            PigRuntimeData pig = runtimePigs[i];
            if (!pig.isActive) continue;
            if (pig.state == state) count++;
        }
        return count;
    }

    private float GetGrowthDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Corn: return cornGrowthSeconds;
            case FeedType.Vegetable: return vegetableGrowthSeconds;
            case FeedType.Premium: return premiumGrowthSeconds;
            default: return cornGrowthSeconds;
        }
    }

    private float GetHarvestDuration(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Corn: return cornHarvestSeconds;
            case FeedType.Vegetable: return vegetableHarvestSeconds;
            case FeedType.Premium: return premiumHarvestSeconds;
            default: return cornHarvestSeconds;
        }
    }

    private int GetFeedCost(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Corn: return cornCostPerPig;
            case FeedType.Vegetable: return vegetableCostPerPig;
            case FeedType.Premium: return premiumCostPerPig;
            default: return 1;
        }
    }

    private int GetAvailableFood(FeedType feedType)
    {
        switch (feedType)
        {
            case FeedType.Corn:
                return FarmInventoryManager.Instance != null
                    ? FarmInventoryManager.Instance.GetAmount(cornItemId)
                    : 0;
            case FeedType.Vegetable:
                return vegetableStockDebug;
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
            case FeedType.Corn:
                return FarmInventoryManager.Instance != null &&
                       FarmInventoryManager.Instance.RemoveItem(cornItemId, amount);

            case FeedType.Vegetable:
                if (vegetableStockDebug < amount) return false;
                vegetableStockDebug -= amount;
                return true;

            case FeedType.Premium:
                if (premiumStockDebug < amount) return false;
                premiumStockDebug -= amount;
                return true;
        }

        return false;
    }
}