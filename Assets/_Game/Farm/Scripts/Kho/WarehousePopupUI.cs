using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehousePopupUI : MonoBehaviour
{
    // F8 — ba hằng số này giờ lấy từ FarmInventoryManager (nguồn sự thật của kho).
    // VÌ SAO: trước đây popup tự giữ một bộ hằng số riêng và AddItem không kiểm gì cả,
    // nên con số "12 / 25" chỉ là chữ. Sau khi enforce thật, hai bên lệch nhau một đơn vị
    // là UI báo còn chỗ mà kho từ chối nhận — loại bug người chơi không thể hiểu nổi.
    private const string WarehouseLevelPrefsKey = FarmInventoryManager.WarehouseLevelPrefsKey;
    private const int WarehouseBaseCapacity = FarmInventoryManager.SlotsPerWarehouseLevel;
    private const int WarehouseMaxLevel = FarmInventoryManager.MaxWarehouseLevel;

    // Danh sách itemId của các món ăn đã nấu, sẽ không hiển thị trong kho để tránh nhầm lẫn
    [Header("Cooked Dish Block")]
    [SerializeField] private List<string> cookedDishIds = new List<string>();

    [System.Serializable]
    private class WarehouseViewItem
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int amount;
    }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Close Button")]
    [SerializeField] private Button btnClose;

    [Header("Search")]
    [SerializeField] private TMP_InputField inputSearch;
    [SerializeField] private Button btnSearch;

    [Header("Slots - Runtime Generate")]
    // Kéo prefab item slot vào đây (item_1 prefab)
    [SerializeField] private GameObject slotPrefab;
    // Kéo ItemGrid transform vào đây (container chứa slot)
    [SerializeField] private Transform itemGridContainer;
    // Số slot hiển thị tối đa
    [SerializeField] private int slotCapacity = 25;

    // List slot được tạo runtime, không kéo tay trong Inspector
    private List<WarehouseSlotUI> slots = new List<WarehouseSlotUI>();

    [Header("Crop Database")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();

    [Header("Extra Item Database")]
    [SerializeField] private List<InventoryItemData> extraItemDatabase = new List<InventoryItemData>();

    [Header("Kitchen Transfer UI")]
    [SerializeField] private Button btnSendToKitchen;
    [SerializeField] private Image selectedPreviewIcon;
    [SerializeField] private TMP_Text selectedPreviewAmount;

    [Header("Warehouse Upgrade - Runtime UI")]
    [SerializeField] private TMP_Text txtSlotUsage;
    [SerializeField] private TMP_Text txtSlotLabel;
    [SerializeField] private Button btnOpenUpgrade;

    [SerializeField] private GameObject transferPopupRoot;
    [SerializeField] private Image transferIcon;
    [SerializeField] private TMP_Text transferTitle;
    [SerializeField] private TMP_Text transferItemName;
    [SerializeField] private TMP_Text transferInventoryAmount;
    [SerializeField] private TMP_Text transferQuantityText;
    [SerializeField] private Button btnTransferMinus;
    [SerializeField] private Button btnTransferPlus;
    [SerializeField] private Button btnTransferMax;
    [SerializeField] private Button btnTransferConfirm;
    [SerializeField] private Button btnTransferClose;

    [SerializeField] private GameObject upgradePopupRoot;
    [SerializeField] private TMP_Text upgradeLevelText;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Button btnUpgradeConfirm;
    [SerializeField] private Button btnUpgradeClose;
    [SerializeField] private Image[] upgradeRequirementIcons;
    [SerializeField] private TMP_Text[] upgradeRequirementCounts;
    [SerializeField] private TMP_Text[] upgradeRequirementNames;

    [SerializeField] private GameObject missingPopupRoot;
    [SerializeField] private Image missingItemIcon;
    [SerializeField] private TMP_Text missingMessageText;
    [SerializeField] private Button btnMissingGoTrain;
    [SerializeField] private Button btnMissingClose;

    private Dictionary<string, CropData> cropLookup = new Dictionary<string, CropData>();
    private Dictionary<string, InventoryItemData> extraItemLookup = new Dictionary<string, InventoryItemData>();

    private readonly Dictionary<string, int> pendingSelection = new Dictionary<string, int>();

    private string lastSelectedItemId;
    private bool popupInputLockHeld;

    private int warehouseLevel = 1;
    private string transferItemId;
    private int transferAvailableAmount;
    private int transferQuantity = 1;

    private class UpgradeRequirement
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int requiredAmount;
        public int ownedAmount;
    }

    private void Awake()
    {
        LoadWarehouseProgress();
        InitSlots();
        BuildCropLookup();
        BuildExtraItemLookup();
        EnsureWarehouseExtensionUI();

        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);

        if (btnSearch != null)
            btnSearch.onClick.AddListener(RefreshUI);

        if (inputSearch != null)
            inputSearch.onSubmit.AddListener(_ => RefreshUI());

        if (btnSendToKitchen != null)
        {
            btnSendToKitchen.onClick.AddListener(SendPendingItemsToKitchen);
            btnSendToKitchen.gameObject.SetActive(false);
        }

        if (selectedPreviewIcon != null)
            selectedPreviewIcon.gameObject.SetActive(false);

        if (selectedPreviewAmount != null)
            selectedPreviewAmount.gameObject.SetActive(false);

        WireWarehouseExtensionButtons();
        CloseTransferPopup();
        CloseUpgradePopup();
        CloseMissingPopup();
        RefreshSelectedPreview();
        RefreshWarehouseSlotFrame();
    }

    private void Start()
    {
        // Đảm bảo popup đóng khi scene load — tránh tự mở ở Play Mode
        if (popupRoot != null) popupRoot.SetActive(false);

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDestroy()
    {
        ReleasePopupInputBlock();

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    // B4 — họ save + phiên bản cho khoá `WAREHOUSE_LEVEL` (ghi thẳng số nguyên nên dấu
    // phiên bản nằm ở khoá phụ `SAVE_VER_WAREHOUSE_LEVEL`).
    //
    // v1 = cấp 1..7, mỗi cấp 25 slot (`FarmInventoryManager.SlotsPerWarehouseLevel`).
    // TĂNG SỐ NÀY nếu đổi `SlotsPerWarehouseLevel` hoặc `MaxWarehouseLevel`: sức chứa suy ra
    // TỪ cấp, nên hạ MaxWarehouseLevel mà không kẹp lại là người chơi cấp 7 mất sạch slot dư
    // và kho của họ lập tức "quá đầy" — `AddItem` từ chối mọi loại mới (F8 giờ chặn thật).
    private const string SaveFamily  = "WAREHOUSE_LEVEL";
    private const int    SaveVersion = 1;

    private void LoadWarehouseProgress()
    {
        SaveVersionGuard.Ensure(SaveFamily, SaveVersion, null,
                                PlayerPrefs.HasKey(WarehouseLevelPrefsKey));

        warehouseLevel = Mathf.Clamp(PlayerPrefs.GetInt(WarehouseLevelPrefsKey, 1), 1, WarehouseMaxLevel);
        slotCapacity = GetWarehouseCapacity(warehouseLevel);
        AvatarProfilePopupUI.SetWarehouseLevel(warehouseLevel);
    }

    private void SaveWarehouseProgress()
    {
        warehouseLevel = Mathf.Clamp(warehouseLevel, 1, WarehouseMaxLevel);
        PlayerPrefs.SetInt(WarehouseLevelPrefsKey, warehouseLevel);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        AvatarProfilePopupUI.SetWarehouseLevel(warehouseLevel);
    }

    private int GetWarehouseCapacity(int level)
    {
        return FarmInventoryManager.CapacityForLevel(level);
    }

    private int GetStoredItemKindCount()
    {
        if (FarmInventoryManager.Instance == null)
            return 0;

        return FarmInventoryManager.Instance.GetOrderedItems().Count;
    }

    private void RefreshWarehouseSlotFrame()
    {
        if (txtSlotUsage != null)
            txtSlotUsage.text = GetStoredItemKindCount() + " / " + slotCapacity;

        if (txtSlotLabel != null)
            txtSlotLabel.text = "Slot";
    }

    private void EnsureWarehouseExtensionUI()
    {
        Transform warehouseRoot = popupRoot != null ? popupRoot.transform : transform;
        Transform popupCanvas = GetPopupCanvasTransform();

        EnsureSlotFrame(warehouseRoot);
        EnsureTransferPopup(popupCanvas);
        EnsureUpgradePopup(popupCanvas);
        EnsureMissingPopup(popupCanvas);
    }

#if UNITY_EDITOR
    public void BuildWarehouseExtensionHierarchyForEditor()
    {
        BuildCropLookup();
        BuildExtraItemLookup();
        EnsureWarehouseExtensionUI();
        WireWarehouseExtensionButtons();
        CloseTransferPopup();
        CloseUpgradePopup();
        CloseMissingPopup();
        RefreshWarehouseSlotFrame();
    }
#endif

    private Transform GetPopupCanvasTransform()
    {
        GameObject canvasPopup = GameObject.Find("Canvas_Popup");
        if (canvasPopup != null)
            return canvasPopup.transform;

        Canvas canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null)
            return canvas.transform;

        return transform;
    }

    private void EnsureSlotFrame(Transform parent)
    {
        Transform frame = FindDeepChild(parent, "Warehouse_SlotFrame");
        if (frame == null)
        {
            RectTransform rect = CreateRect(parent, "Warehouse_SlotFrame", new Vector2(190f, 74f), new Vector2(-430f, -250f));
            AddImage(rect.gameObject, new Color(0.32f, 0.17f, 0.07f, 0.82f), true);
            CreateText(rect, "Txt_SlotUsage", "0 / 25", 24f, Color.white, new Vector2(22f, 12f), new Vector2(120f, 32f), TextAlignmentOptions.Center);
            CreateText(rect, "Txt_SlotLabel", "Slot", 18f, Color.white, new Vector2(22f, -18f), new Vector2(120f, 26f), TextAlignmentOptions.Center);

            RectTransform icon = CreateRect(rect, "Img_WarehouseSlotIcon", new Vector2(50f, 50f), new Vector2(-62f, 0f));
            AddImage(icon.gameObject, new Color(0.94f, 0.68f, 0.26f, 0.9f), true);
            frame = rect;
        }

        txtSlotUsage = FindChildComponent<TMP_Text>(frame, "Txt_SlotUsage");
        txtSlotLabel = FindChildComponent<TMP_Text>(frame, "Txt_SlotLabel");

        Transform btn = FindDeepChild(parent, "Btn_WarehouseUpgrade");
        if (btn == null)
        {
            RectTransform rect = CreateRect(parent, "Btn_WarehouseUpgrade", new Vector2(210f, 76f), new Vector2(430f, -250f));
            AddImage(rect.gameObject, new Color(0.82f, 0.52f, 0.11f, 0.95f), true);
            btnOpenUpgrade = rect.gameObject.AddComponent<Button>();
            CreateText(rect, "Txt_ButtonLabel", "Nâng cấp ↑", 26f, Color.white, Vector2.zero, new Vector2(190f, 58f), TextAlignmentOptions.Center);
        }
        else
        {
            btnOpenUpgrade = btn.GetComponent<Button>();
            if (btnOpenUpgrade == null)
                btnOpenUpgrade = btn.gameObject.AddComponent<Button>();
        }
    }

    private void EnsureTransferPopup(Transform parent)
    {
        Transform root = FindDeepChild(parent, "Popup_WarehouseTransferItem");
        if (root == null)
        {
            RectTransform panel = CreateRect(parent, "Popup_WarehouseTransferItem", new Vector2(460f, 540f), Vector2.zero);
            AddImage(panel.gameObject, new Color(0.86f, 0.70f, 0.48f, 0.98f), true);

            CreateText(panel, "Txt_TransferTitle", "CHUYỂN VÀO KHO", 28f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(52f, 205f), new Vector2(280f, 44f), TextAlignmentOptions.Left);

            RectTransform close = CreateRect(panel, "Btn_TransferClose", new Vector2(58f, 58f), new Vector2(198f, 218f));
            AddImage(close.gameObject, new Color(0.70f, 0.22f, 0.10f, 1f), true);
            close.gameObject.AddComponent<Button>();
            CreateText(close, "Txt_X", "X", 32f, Color.white, Vector2.zero, new Vector2(54f, 54f), TextAlignmentOptions.Center);

            RectTransform icon = CreateRect(panel, "Img_TransferIcon", new Vector2(118f, 118f), new Vector2(-125f, 160f));
            AddImage(icon.gameObject, new Color(1f, 0.87f, 0.62f, 0.85f), true);

            CreateText(panel, "Txt_TransferItemName", "Vật phẩm", 23f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(52f, 152f), new Vector2(240f, 34f), TextAlignmentOptions.Left);
            CreateText(panel, "Txt_TransferAmountInWarehouse", "Số lượng trong kho: 0", 20f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(52f, 104f), new Vector2(275f, 32f), TextAlignmentOptions.Left);
            CreateText(panel, "Txt_TransferQuantityLabel", "Số lượng chuyển:", 20f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(0f, 34f), new Vector2(260f, 34f), TextAlignmentOptions.Center);

            RectTransform minus = CreateRect(panel, "Btn_TransferMinus", new Vector2(58f, 52f), new Vector2(-122f, -34f));
            AddImage(minus.gameObject, new Color(0.78f, 0.48f, 0.12f, 1f), true);
            minus.gameObject.AddComponent<Button>();
            CreateText(minus, "Txt_Minus", "-", 34f, Color.white, Vector2.zero, new Vector2(54f, 48f), TextAlignmentOptions.Center);

            CreateText(panel, "Txt_TransferQty", "1", 28f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(-30f, -34f), new Vector2(70f, 48f), TextAlignmentOptions.Center);

            RectTransform plus = CreateRect(panel, "Btn_TransferPlus", new Vector2(58f, 52f), new Vector2(62f, -34f));
            AddImage(plus.gameObject, new Color(0.78f, 0.48f, 0.12f, 1f), true);
            plus.gameObject.AddComponent<Button>();
            CreateText(plus, "Txt_Plus", "+", 34f, Color.white, Vector2.zero, new Vector2(54f, 48f), TextAlignmentOptions.Center);

            RectTransform max = CreateRect(panel, "Btn_TransferMax", new Vector2(74f, 52f), new Vector2(150f, -34f));
            AddImage(max.gameObject, new Color(0.58f, 0.31f, 0.12f, 1f), true);
            max.gameObject.AddComponent<Button>();
            CreateText(max, "Txt_Max", "MAX", 22f, Color.white, Vector2.zero, new Vector2(70f, 48f), TextAlignmentOptions.Center);

            RectTransform confirm = CreateRect(panel, "Btn_TransferConfirm", new Vector2(320f, 68f), new Vector2(0f, -172f));
            AddImage(confirm.gameObject, new Color(0.18f, 0.58f, 0.12f, 1f), true);
            confirm.gameObject.AddComponent<Button>();
            CreateText(confirm, "Txt_Confirm", "Chuyển vào kho", 26f, Color.white, Vector2.zero, new Vector2(300f, 58f), TextAlignmentOptions.Center);

            root = panel;
        }

        transferPopupRoot = root.gameObject;
        transferIcon = FindChildComponent<Image>(root, "Img_TransferIcon");
        transferTitle = FindChildComponent<TMP_Text>(root, "Txt_TransferTitle");
        transferItemName = FindChildComponent<TMP_Text>(root, "Txt_TransferItemName");
        transferInventoryAmount = FindChildComponent<TMP_Text>(root, "Txt_TransferAmountInWarehouse");
        transferQuantityText = FindChildComponent<TMP_Text>(root, "Txt_TransferQty");
        btnTransferMinus = FindOrAddButton(root, "Btn_TransferMinus");
        btnTransferPlus = FindOrAddButton(root, "Btn_TransferPlus");
        btnTransferMax = FindOrAddButton(root, "Btn_TransferMax");
        btnTransferConfirm = FindOrAddButton(root, "Btn_TransferConfirm");
        btnTransferClose = FindOrAddButton(root, "Btn_TransferClose");
    }

    private void EnsureUpgradePopup(Transform parent)
    {
        Transform root = FindDeepChild(parent, "Popup_WarehouseUpgrade");
        if (root == null)
        {
            RectTransform panel = CreateRect(parent, "Popup_WarehouseUpgrade", new Vector2(760f, 560f), Vector2.zero);
            AddImage(panel.gameObject, new Color(0.86f, 0.70f, 0.48f, 0.98f), true);

            CreateText(panel, "Txt_UpgradeTitle", "NÂNG CẤP KHO", 32f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(0f, 235f), new Vector2(420f, 48f), TextAlignmentOptions.Center);

            RectTransform close = CreateRect(panel, "Btn_UpgradeClose", new Vector2(58f, 58f), new Vector2(338f, 238f));
            AddImage(close.gameObject, new Color(0.70f, 0.22f, 0.10f, 1f), true);
            close.gameObject.AddComponent<Button>();
            CreateText(close, "Txt_X", "X", 32f, Color.white, Vector2.zero, new Vector2(54f, 54f), TextAlignmentOptions.Center);

            RectTransform warehouseImage = CreateRect(panel, "Img_UpgradeWarehouse", new Vector2(220f, 180f), new Vector2(-230f, 100f));
            AddImage(warehouseImage.gameObject, new Color(0.65f, 0.42f, 0.20f, 0.82f), true);

            RectTransform levelBox = CreateRect(panel, "Panel_UpgradeLevelInfo", new Vector2(390f, 118f), new Vector2(145f, 108f));
            AddImage(levelBox.gameObject, new Color(1f, 0.83f, 0.56f, 0.72f), true);
            CreateText(levelBox, "Txt_UpgradeLevelInfo", "Cấp 1 / 25 Slot  >>>  Cấp 2 / 50 Slot", 24f, new Color(0.22f, 0.10f, 0.03f, 1f), Vector2.zero, new Vector2(360f, 92f), TextAlignmentOptions.Center);

            CreateText(panel, "Txt_UpgradeRequirementsTitle", "Yêu cầu nâng cấp", 24f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(0f, -32f), new Vector2(340f, 34f), TextAlignmentOptions.Center);

            float startX = -255f;
            for (int i = 0; i < 4; i++)
            {
                RectTransform req = CreateRect(panel, "UpgradeRequirement_" + (i + 1), new Vector2(138f, 150f), new Vector2(startX + i * 170f, -135f));
                AddImage(req.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.75f), true);
                RectTransform icon = CreateRect(req, "Img_UpgradeReqIcon_" + (i + 1), new Vector2(82f, 70f), new Vector2(0f, 38f));
                AddImage(icon.gameObject, new Color(0.78f, 0.58f, 0.32f, 0.85f), true);
                CreateText(req, "Txt_UpgradeReqCount_" + (i + 1), "0/2", 20f, Color.white, new Vector2(0f, -23f), new Vector2(120f, 28f), TextAlignmentOptions.Center);
                CreateText(req, "Txt_UpgradeReqName_" + (i + 1), "Warehouse", 16f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(0f, -54f), new Vector2(120f, 28f), TextAlignmentOptions.Center);
            }

            RectTransform confirm = CreateRect(panel, "Btn_UpgradeConfirm", new Vector2(230f, 68f), new Vector2(0f, -245f));
            AddImage(confirm.gameObject, new Color(0.18f, 0.58f, 0.12f, 1f), true);
            confirm.gameObject.AddComponent<Button>();
            CreateText(confirm, "Txt_UpgradeButton", "Nâng cấp", 26f, Color.white, Vector2.zero, new Vector2(210f, 58f), TextAlignmentOptions.Center);

            root = panel;
        }

        upgradePopupRoot = root.gameObject;
        upgradeLevelText = FindChildComponent<TMP_Text>(root, "Txt_UpgradeLevelInfo");
        upgradeButtonText = FindChildComponent<TMP_Text>(root, "Txt_UpgradeButton");
        btnUpgradeConfirm = FindOrAddButton(root, "Btn_UpgradeConfirm");
        btnUpgradeClose = FindOrAddButton(root, "Btn_UpgradeClose");
        upgradeRequirementIcons = new Image[4];
        upgradeRequirementCounts = new TMP_Text[4];
        upgradeRequirementNames = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            upgradeRequirementIcons[i] = FindChildComponent<Image>(root, "Img_UpgradeReqIcon_" + (i + 1));
            upgradeRequirementCounts[i] = FindChildComponent<TMP_Text>(root, "Txt_UpgradeReqCount_" + (i + 1));
            upgradeRequirementNames[i] = FindChildComponent<TMP_Text>(root, "Txt_UpgradeReqName_" + (i + 1));
        }
    }

    private void EnsureMissingPopup(Transform parent)
    {
        Transform root = FindDeepChild(parent, "Popup_WarehouseMissingItems");
        if (root == null)
        {
            RectTransform panel = CreateRect(parent, "Popup_WarehouseMissingItems", new Vector2(620f, 380f), Vector2.zero);
            AddImage(panel.gameObject, new Color(0.86f, 0.70f, 0.48f, 0.98f), true);
            CreateText(panel, "Txt_MissingTitle", "Thiếu vật phẩm", 30f, Color.white, new Vector2(0f, 144f), new Vector2(360f, 48f), TextAlignmentOptions.Center);

            RectTransform close = CreateRect(panel, "Btn_MissingClose", new Vector2(58f, 58f), new Vector2(278f, 150f));
            AddImage(close.gameObject, new Color(0.70f, 0.22f, 0.10f, 1f), true);
            close.gameObject.AddComponent<Button>();
            CreateText(close, "Txt_X", "X", 32f, Color.white, Vector2.zero, new Vector2(54f, 54f), TextAlignmentOptions.Center);

            RectTransform icon = CreateRect(panel, "Img_MissingIcon", new Vector2(150f, 150f), new Vector2(-190f, 25f));
            AddImage(icon.gameObject, new Color(0.78f, 0.58f, 0.32f, 0.85f), true);
            CreateText(panel, "Txt_MissingMessage", "Bạn hiện đang thiếu vật phẩm để nâng cấp kho!\nHãy giao thêm vật phẩm bằng tàu hỏa.", 22f, new Color(0.22f, 0.10f, 0.03f, 1f), new Vector2(80f, 25f), new Vector2(340f, 150f), TextAlignmentOptions.Left);

            RectTransform goTrain = CreateRect(panel, "Btn_MissingGoTrain", new Vector2(380f, 66f), new Vector2(50f, -130f));
            AddImage(goTrain.gameObject, new Color(0.18f, 0.58f, 0.12f, 1f), true);
            goTrain.gameObject.AddComponent<Button>();
            CreateText(goTrain, "Txt_GoTrain", "Đi lấy thêm vật phẩm", 24f, Color.white, Vector2.zero, new Vector2(350f, 58f), TextAlignmentOptions.Center);

            root = panel;
        }

        missingPopupRoot = root.gameObject;
        missingItemIcon = FindChildComponent<Image>(root, "Img_MissingIcon");
        missingMessageText = FindChildComponent<TMP_Text>(root, "Txt_MissingMessage");
        btnMissingGoTrain = FindOrAddButton(root, "Btn_MissingGoTrain");
        btnMissingClose = FindOrAddButton(root, "Btn_MissingClose");
    }

    private void EnsurePopupRaycastBlock()
    {
        if (popupRoot == null)
            return;

        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        if (popupRoot != null)
            FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    // Tạo slot runtime từ prefab, xóa slot cũ nếu có
    private void InitSlots()
    {
        if (slotPrefab == null || itemGridContainer == null)
        {
            return;
        }

        // Xóa hết child cũ trong container (item_1..item_N còn trong hierarchy)
        for (int i = itemGridContainer.childCount - 1; i >= 0; i--)
            Destroy(itemGridContainer.GetChild(i).gameObject);

        slots.Clear();

        // Tạo đủ slotCapacity slot từ prefab
        for (int i = 0; i < slotCapacity; i++)
        {
            GameObject go = Instantiate(slotPrefab, itemGridContainer);
            go.name = "slot_" + (i + 1);
            WarehouseSlotUI slotUI = go.GetComponent<WarehouseSlotUI>();

            if (slotUI == null)
            {
                continue;
            }

            slots.Add(slotUI);
        }

    }

    private void BuildCropLookup()
    {
        cropLookup.Clear();

        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null) continue;

            string key = GetHarvestItemId(crop);
            if (string.IsNullOrEmpty(key)) continue;

            if (!cropLookup.ContainsKey(key))
                cropLookup.Add(key, crop);
        }
    }

    private void BuildExtraItemLookup()
    {
        extraItemLookup.Clear();

        for (int i = 0; i < extraItemDatabase.Count; i++)
        {
            InventoryItemData item = extraItemDatabase[i];
            if (item == null) continue;
            if (string.IsNullOrEmpty(item.itemId)) continue;

            if (!extraItemLookup.ContainsKey(item.itemId))
                extraItemLookup.Add(item.itemId, item);
        }
    }

    // true khi popup đang thực sự hiển thị
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            EnsurePopupRaycastBlock();
        }

        RefreshUI();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);

    }

    public void RefreshUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
                slots[i].SetEmpty();
        }

        if (FarmInventoryManager.Instance == null)
            return;

        List<WarehouseViewItem> items = BuildFilteredItems();
        int count = Mathf.Min(items.Count, slots.Count);

        for (int i = 0; i < count; i++)
        {
            if (slots[i] != null)
            {
                int visibleAmount = GetVisibleAmount(items[i].itemId, items[i].amount);
                slots[i].SetData(items[i].itemId, items[i].icon, visibleAmount, OnWarehouseSlotClicked);
            }
        }

        RefreshSelectedPreview();
        RefreshWarehouseSlotFrame();
    }

    private List<WarehouseViewItem> BuildFilteredItems()
    {
        List<WarehouseViewItem> result = new List<WarehouseViewItem>();

        string keyword = inputSearch != null ? NormalizeText(inputSearch.text) : "";

        List<KeyValuePair<string, int>> allItems = FarmInventoryManager.Instance.GetOrderedItems();

        foreach (var kv in allItems)
        {
            string itemId = kv.Key;
            int amount = kv.Value;

            if (amount <= 0)
                continue;

            string displayName = itemId;
            Sprite icon = null;

            CropData crop = GetCropByItemId(itemId);
            if (crop != null)
            {
                displayName = GetDisplayName(crop);
                icon = crop.icon;
            }
            else
            {
                InventoryItemData extraItem = GetExtraItemById(itemId);
                if (extraItem != null)
                {
                    displayName = string.IsNullOrEmpty(extraItem.displayName) ? itemId : extraItem.displayName;
                    icon = extraItem.icon;
                }
            }

            string normalizedName = NormalizeText(displayName);
            string normalizedId = NormalizeText(itemId);

            bool pass =
                string.IsNullOrEmpty(keyword) ||
                normalizedName.Contains(keyword) ||
                normalizedId.Contains(keyword);

            if (!pass)
                continue;

            result.Add(new WarehouseViewItem
            {
                itemId = itemId,
                displayName = displayName,
                icon = icon,
                amount = amount
            });
        }

        return result;
    }

    private void OnWarehouseSlotClicked(string itemId)
    {
        //Code Nguyên Thêm: Nếu đây là món ăn đã nấu thì không cho chọn để gửi sang bếp nữa, tránh nhầm lẫn
        if (IsCookedDish(itemId))
        {
            return;
        }
        //End code Nguyên thêm
        if (string.IsNullOrEmpty(itemId))
            return;

        if (FarmInventoryManager.Instance == null)
            return;

        int totalInInventory = FarmInventoryManager.Instance.GetAmount(itemId);
        int alreadyPending = GetPendingAmount(itemId);

        if (alreadyPending >= totalInInventory)
        {
            return;
        }

        OpenTransferPopup(itemId, totalInInventory - alreadyPending);
    }

    private void WireWarehouseExtensionButtons()
    {
        if (btnOpenUpgrade != null)
        {
            btnOpenUpgrade.onClick.RemoveListener(OpenUpgradePopup);
            btnOpenUpgrade.onClick.AddListener(OpenUpgradePopup);
        }

        if (btnTransferMinus != null)
        {
            btnTransferMinus.onClick.RemoveListener(DecreaseTransferQuantity);
            btnTransferMinus.onClick.AddListener(DecreaseTransferQuantity);
        }

        if (btnTransferPlus != null)
        {
            btnTransferPlus.onClick.RemoveListener(IncreaseTransferQuantity);
            btnTransferPlus.onClick.AddListener(IncreaseTransferQuantity);
        }

        if (btnTransferMax != null)
        {
            btnTransferMax.onClick.RemoveListener(SetTransferQuantityToMax);
            btnTransferMax.onClick.AddListener(SetTransferQuantityToMax);
        }

        if (btnTransferConfirm != null)
        {
            btnTransferConfirm.onClick.RemoveListener(ConfirmTransferPopup);
            btnTransferConfirm.onClick.AddListener(ConfirmTransferPopup);
        }

        if (btnTransferClose != null)
        {
            btnTransferClose.onClick.RemoveListener(CloseTransferPopup);
            btnTransferClose.onClick.AddListener(CloseTransferPopup);
        }

        if (btnUpgradeConfirm != null)
        {
            btnUpgradeConfirm.onClick.RemoveListener(TryUpgradeWarehouse);
            btnUpgradeConfirm.onClick.AddListener(TryUpgradeWarehouse);
        }

        if (btnUpgradeClose != null)
        {
            btnUpgradeClose.onClick.RemoveListener(CloseUpgradePopup);
            btnUpgradeClose.onClick.AddListener(CloseUpgradePopup);
        }

        if (btnMissingClose != null)
        {
            btnMissingClose.onClick.RemoveListener(CloseMissingPopup);
            btnMissingClose.onClick.AddListener(CloseMissingPopup);
        }

        if (btnMissingGoTrain != null)
        {
            btnMissingGoTrain.onClick.RemoveListener(GoToTrainFromMissingPopup);
            btnMissingGoTrain.onClick.AddListener(GoToTrainFromMissingPopup);
        }
    }

    private void OpenTransferPopup(string itemId, int availableAmount)
    {
        transferItemId = itemId;
        transferAvailableAmount = Mathf.Max(0, availableAmount);
        transferQuantity = Mathf.Clamp(1, 1, Mathf.Max(1, transferAvailableAmount));
        lastSelectedItemId = itemId;

        WarehouseViewItem viewItem = BuildViewItem(itemId, FarmInventoryManager.Instance != null ? FarmInventoryManager.Instance.GetAmount(itemId) : 0);

        if (transferTitle != null)
            transferTitle.text = "CHUYỂN VÀO KHO";

        if (transferItemName != null)
            transferItemName.text = viewItem != null ? viewItem.displayName : itemId;

        if (transferIcon != null)
        {
            transferIcon.sprite = viewItem != null ? viewItem.icon : null;
            transferIcon.enabled = transferIcon.sprite != null;
        }

        RefreshTransferPopupTexts();

        if (transferPopupRoot != null)
            transferPopupRoot.SetActive(true);

        RefreshSelectedPreview();
    }

    private void RefreshTransferPopupTexts()
    {
        if (transferInventoryAmount != null)
            transferInventoryAmount.text = "Số lượng trong kho: " + transferAvailableAmount;

        if (transferQuantityText != null)
            transferQuantityText.text = transferQuantity.ToString();

        bool hasAmount = transferAvailableAmount > 0;
        if (btnTransferMinus != null)
            btnTransferMinus.interactable = hasAmount && transferQuantity > 1;
        if (btnTransferPlus != null)
            btnTransferPlus.interactable = hasAmount && transferQuantity < transferAvailableAmount;
        if (btnTransferMax != null)
            btnTransferMax.interactable = hasAmount && transferQuantity < transferAvailableAmount;
        if (btnTransferConfirm != null)
            btnTransferConfirm.interactable = hasAmount;
    }

    private void DecreaseTransferQuantity()
    {
        transferQuantity = Mathf.Max(1, transferQuantity - 1);
        RefreshTransferPopupTexts();
    }

    private void IncreaseTransferQuantity()
    {
        transferQuantity = Mathf.Min(transferAvailableAmount, transferQuantity + 1);
        RefreshTransferPopupTexts();
    }

    private void SetTransferQuantityToMax()
    {
        transferQuantity = Mathf.Max(1, transferAvailableAmount);
        RefreshTransferPopupTexts();
    }

    private void ConfirmTransferPopup()
    {
        if (string.IsNullOrEmpty(transferItemId) || transferQuantity <= 0)
            return;

        pendingSelection.Clear();
        pendingSelection[transferItemId] = Mathf.Min(transferQuantity, transferAvailableAmount);
        lastSelectedItemId = transferItemId;

        SendPendingItemsToKitchen();
        CloseTransferPopup();
    }

    private void CloseTransferPopup()
    {
        if (transferPopupRoot != null)
            transferPopupRoot.SetActive(false);
    }

    private void OpenUpgradePopup()
    {
        RefreshUpgradePopup();

        if (upgradePopupRoot != null)
            upgradePopupRoot.SetActive(true);
    }

    private void CloseUpgradePopup()
    {
        if (upgradePopupRoot != null)
            upgradePopupRoot.SetActive(false);
    }

    private void RefreshUpgradePopup()
    {
        bool isMax = warehouseLevel >= WarehouseMaxLevel;
        int nextLevel = Mathf.Min(WarehouseMaxLevel, warehouseLevel + 1);

        if (upgradeLevelText != null)
        {
            if (isMax)
                upgradeLevelText.text = "Cấp " + warehouseLevel + " / " + GetWarehouseCapacity(warehouseLevel) + " Slot\nĐã đạt cấp tối đa";
            else
                upgradeLevelText.text = "Cấp " + warehouseLevel + " / " + GetWarehouseCapacity(warehouseLevel) + " Slot  >>>  Cấp " + nextLevel + " / " + GetWarehouseCapacity(nextLevel) + " Slot";
        }

        if (upgradeButtonText != null)
            upgradeButtonText.text = isMax ? "Đã tối đa" : "Nâng cấp";

        if (btnUpgradeConfirm != null)
            btnUpgradeConfirm.interactable = !isMax;

        List<UpgradeRequirement> requirements = BuildUpgradeRequirements();
        for (int i = 0; i < 4; i++)
        {
            UpgradeRequirement req = i < requirements.Count ? requirements[i] : null;
            if (upgradeRequirementIcons != null && i < upgradeRequirementIcons.Length && upgradeRequirementIcons[i] != null)
            {
                upgradeRequirementIcons[i].sprite = req != null ? req.icon : null;
                upgradeRequirementIcons[i].enabled = req == null || req.icon != null;
            }

            if (upgradeRequirementCounts != null && i < upgradeRequirementCounts.Length && upgradeRequirementCounts[i] != null)
            {
                if (req == null)
                {
                    upgradeRequirementCounts[i].text = "-";
                    upgradeRequirementCounts[i].color = Color.white;
                }
                else
                {
                    upgradeRequirementCounts[i].text = req.ownedAmount + "/" + req.requiredAmount;
                    upgradeRequirementCounts[i].color = req.ownedAmount >= req.requiredAmount ? Color.white : new Color(0.9f, 0.12f, 0.08f, 1f);
                }
            }

            if (upgradeRequirementNames != null && i < upgradeRequirementNames.Length && upgradeRequirementNames[i] != null)
                upgradeRequirementNames[i].text = req != null ? req.displayName : "Warehouse";
        }
    }

    private void TryUpgradeWarehouse()
    {
        if (warehouseLevel >= WarehouseMaxLevel)
        {
            RefreshUpgradePopup();
            return;
        }

        List<UpgradeRequirement> requirements = BuildUpgradeRequirements();
        List<UpgradeRequirement> missing = new List<UpgradeRequirement>();

        for (int i = 0; i < requirements.Count; i++)
        {
            if (requirements[i].ownedAmount < requirements[i].requiredAmount)
                missing.Add(requirements[i]);
        }

        if (missing.Count > 0)
        {
            OpenMissingPopup(missing);
            RefreshUpgradePopup();
            return;
        }

        if (FarmInventoryManager.Instance == null)
            return;

        for (int i = 0; i < requirements.Count; i++)
            FarmInventoryManager.Instance.RemoveItem(requirements[i].itemId, requirements[i].requiredAmount);

        warehouseLevel = Mathf.Clamp(warehouseLevel + 1, 1, WarehouseMaxLevel);
        slotCapacity = GetWarehouseCapacity(warehouseLevel);
        SaveWarehouseProgress();
        InitSlots();
        RefreshUI();
        RefreshUpgradePopup();
    }

    private void OpenMissingPopup(List<UpgradeRequirement> missing)
    {
        if (missing == null || missing.Count == 0)
            return;

        UpgradeRequirement first = missing[0];
        if (missingItemIcon != null)
        {
            missingItemIcon.sprite = first.icon;
            missingItemIcon.enabled = first.icon != null;
        }

        if (missingMessageText != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bạn hiện đang thiếu vật phẩm để nâng cấp kho!");
            for (int i = 0; i < missing.Count; i++)
            {
                int needMore = Mathf.Max(0, missing[i].requiredAmount - missing[i].ownedAmount);
                sb.AppendLine("- " + missing[i].displayName + ": thiếu " + needMore);
            }
            sb.Append("Hãy giao thêm vật phẩm bằng tàu hỏa để hoàn tất nâng cấp.");
            missingMessageText.text = sb.ToString();
        }

        if (missingPopupRoot != null)
            missingPopupRoot.SetActive(true);
    }

    private void CloseMissingPopup()
    {
        if (missingPopupRoot != null)
            missingPopupRoot.SetActive(false);
    }

    private void GoToTrainFromMissingPopup()
    {
        CloseMissingPopup();
        CloseUpgradePopup();
        ClosePopup();
        FocusCameraOnTrain();
    }

    private void FocusCameraOnTrain()
    {
        Camera cam = Camera.main;
        Transform target = null;

        if (TrainManager.Instance != null)
            target = TrainManager.Instance.transform;

        if (target == null)
        {
            TrainStationBuilding station = FindFirstObjectByType<TrainStationBuilding>(FindObjectsInactive.Include);
            if (station != null)
                target = station.transform;
        }

        if (target == null)
        {
            TrainPathFollower follower = FindFirstObjectByType<TrainPathFollower>(FindObjectsInactive.Include);
            if (follower != null)
                target = follower.transform;
        }

        if (target == null || cam == null)
            return;

        Vector3 pos = cam.transform.position;
        pos.x = target.position.x;
        pos.y = target.position.y;
        cam.transform.position = pos;
    }

    private List<UpgradeRequirement> BuildUpgradeRequirements()
    {
        int requiredAmount = Mathf.Max(2, warehouseLevel * 2);
        List<UpgradeRequirement> result = new List<UpgradeRequirement>
        {
            BuildUpgradeRequirement("da", "Đá", requiredAmount),
            BuildUpgradeRequirement("dinh", "Đinh", requiredAmount),
            BuildUpgradeRequirement("go", "Gỗ", requiredAmount),
            BuildUpgradeRequirement("kinh", "Kính", requiredAmount)
        };

        return result;
    }

    private WarehouseViewItem BuildViewItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        string displayName = itemId;
        Sprite icon = null;

        CropData crop = GetCropByItemId(itemId);
        if (crop != null)
        {
            displayName = GetDisplayName(crop);
            icon = crop.icon;
        }
        else
        {
            InventoryItemData extraItem = GetExtraItemById(itemId);
            if (extraItem != null)
            {
                displayName = string.IsNullOrEmpty(extraItem.displayName) ? itemId : extraItem.displayName;
                icon = extraItem.icon;
            }
        }

        return new WarehouseViewItem
        {
            itemId = itemId,
            displayName = displayName,
            icon = icon,
            amount = amount
        };
    }

    private UpgradeRequirement BuildUpgradeRequirement(string itemId, string fallbackName, int requiredAmount)
    {
        InventoryItemData item = GetExtraItemById(itemId);
        int owned = FarmInventoryManager.Instance != null ? FarmInventoryManager.Instance.GetAmount(itemId) : 0;

        return new UpgradeRequirement
        {
            itemId = itemId,
            displayName = item != null && !string.IsNullOrEmpty(item.displayName) ? item.displayName : fallbackName,
            icon = item != null ? item.icon : null,
            requiredAmount = requiredAmount,
            ownedAmount = owned
        };
    }

    private int GetPendingAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        return pendingSelection.TryGetValue(itemId, out int value) ? value : 0;
    }

    private int GetVisibleAmount(string itemId, int totalAmount)
    {
        int pending = GetPendingAmount(itemId);
        return Mathf.Max(0, totalAmount - pending);
    }

    private void RefreshSelectedPreview()
    {
        if (selectedPreviewIcon != null)
        {
            Sprite previewSprite = null;

            if (!string.IsNullOrEmpty(lastSelectedItemId))
            {
                CropData crop = GetCropByItemId(lastSelectedItemId);
                if (crop != null)
                    previewSprite = crop.icon;
                else
                {
                    InventoryItemData extra = GetExtraItemById(lastSelectedItemId);
                    if (extra != null)
                        previewSprite = extra.icon;
                }
            }

            selectedPreviewIcon.sprite = previewSprite;
            selectedPreviewIcon.enabled = previewSprite != null;
        }

        if (selectedPreviewAmount != null)
        {
            int amount = GetPendingAmount(lastSelectedItemId);
            selectedPreviewAmount.text = amount > 0 ? ("x" + amount) : "";
        }

        if (btnSendToKitchen != null)
            btnSendToKitchen.interactable = pendingSelection.Count > 0;
    }

    private void SendPendingItemsToKitchen()
    {
        if (KitchenTransferManager.Instance == null)
        {
            return;
        }

        if (FarmInventoryManager.Instance == null)
        {
            return;
        }


        foreach (var kv in pendingSelection)
        {
            if (kv.Value <= 0)
                continue;

            // chỉ chuyển nếu kho thật còn đủ
            if (!FarmInventoryManager.Instance.HasItem(kv.Key, kv.Value))
            {
                continue;
            }

            // trừ kho thật
            bool removed = FarmInventoryManager.Instance.RemoveItem(kv.Key, kv.Value);
            if (!removed)
            {
                continue;
            }

            // đưa sang bếp
            KitchenTransferManager.Instance.AddTransferredItem(kv.Key, kv.Value);
        }

        pendingSelection.Clear();
        lastSelectedItemId = null;

        RefreshUI();
    }

    private CropData GetCropByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (cropLookup.TryGetValue(itemId, out CropData crop))
            return crop;

        return null;
    }

    private InventoryItemData GetExtraItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (extraItemLookup.TryGetValue(itemId, out InventoryItemData item))
            return item;

        return null;
    }

    private string GetHarvestItemId(CropData crop)
    {
        if (crop == null)
            return "";

        return string.IsNullOrEmpty(crop.harvestItemId) ? crop.cropId : crop.harvestItemId;
    }

    private string GetDisplayName(CropData crop)
    {
        if (crop == null)
            return "";

        if (!string.IsNullOrEmpty(crop.displayName))
            return crop.displayName;

        return GetHarvestItemId(crop);
    }

    private string NormalizeText(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);

            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string result = sb.ToString().Normalize(NormalizationForm.FormC);
        result = result.Replace('đ', 'd').Replace('Đ', 'D');
        return result.ToLowerInvariant().Trim();
    }

    private RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        return rect;
    }

    private Image AddImage(GameObject go, Color color, bool raycastTarget)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name, size, anchoredPosition);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private Button FindOrAddButton(Transform root, string childName)
    {
        Transform child = FindDeepChild(root, childName);
        if (child == null)
            return null;

        Button button = child.GetComponent<Button>();
        if (button == null)
            button = child.gameObject.AddComponent<Button>();

        Image image = child.GetComponent<Image>();
        if (image != null)
            button.targetGraphic = image;

        return button;
    }

    private T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindDeepChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    //Code Nguyên thêm
    private bool IsCookedDish(string itemId)// Kiểm tra nếu itemId thuộc danh sách món ăn đã nấu thì trả về true, sẽ không hiển thị trong kho
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        string key = itemId.Trim().ToLower();

        for (int i = 0; i < cookedDishIds.Count; i++)
        {
            if (string.IsNullOrEmpty(cookedDishIds[i]))
                continue;

            if (cookedDishIds[i].Trim().ToLower() == key)
                return true;
        }

        return false;
    }
}
