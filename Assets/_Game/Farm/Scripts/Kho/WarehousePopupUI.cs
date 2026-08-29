using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WarehouseCategory
{
    NongSan = 0,    // 🌾 Nông sản (Crops)
    ChanNuoi = 1,   // 🐔 Chăn nuôi (Animal products)
    MonAn = 2       // 🍲 Món ăn & Chế biến (Cooked dishes & Processed goods)
}

public class WarehousePopupUI : MonoBehaviour
{
    private const string WarehouseLevelPrefsKey = FarmInventoryManager.WarehouseLevelPrefsKey;
    private const int WarehouseBaseCapacity = FarmInventoryManager.SlotsPerWarehouseLevel;
    private const int WarehouseMaxLevel = FarmInventoryManager.MaxWarehouseLevel;

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Category Tabs")]
    [SerializeField] private Button btnTabNongSan;
    [SerializeField] private Button btnTabChanNuoi;
    [SerializeField] private Button btnTabMonAn;
    [SerializeField] private Image imgTabNongSan;
    [SerializeField] private Image imgTabChanNuoi;
    [SerializeField] private Image imgTabMonAn;
    [SerializeField] private TMP_Text txtTabNongSan;
    [SerializeField] private TMP_Text txtTabChanNuoi;
    [SerializeField] private TMP_Text txtTabMonAn;
    [SerializeField] private RectTransform rectTabNongSan;
    [SerializeField] private RectTransform rectTabChanNuoi;
    [SerializeField] private RectTransform rectTabMonAn;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabInactiveSprite;

    [Header("Slots Grid")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform itemGridContainer;
    [SerializeField] private int minDisplaySlots = 8; // 4x2 slots view

    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotNormalSprite;
    [SerializeField] private Sprite slotSelectedSprite;
    [SerializeField] private Sprite slotEmptySprite;

    [Header("Capacity Bar")]
    [SerializeField] private Image imgCapacityFill;
    [SerializeField] private TMP_Text txtCapacity;

    [Header("Right Detail Panel")]
    [SerializeField] private GameObject detailPanelRoot;
    [SerializeField] private Image imgDetailIcon;
    [SerializeField] private TMP_Text txtDetailTitle;
    [SerializeField] private TMP_Text txtDetailDesc;
    [SerializeField] private TMP_Text txtTransferCount;
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button btnMax;
    [SerializeField] private Button btnTransferKitchen;

    [Header("Upgrade Footer Box")]
    [SerializeField] private TMP_Text txtUpgradeInfo;
    [SerializeField] private Button btnUpgrade;

    [Header("Databases")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();
    [SerializeField] private List<InventoryItemData> extraItemDatabase = new List<InventoryItemData>();
    [SerializeField] private List<string> cookedDishIds = new List<string>();

    private List<WarehouseSlotUI> slots = new List<WarehouseSlotUI>();
    private Dictionary<string, CropData> cropLookup = new Dictionary<string, CropData>();
    private Dictionary<string, InventoryItemData> extraItemLookup = new Dictionary<string, InventoryItemData>();

    private WarehouseCategory currentCategory = WarehouseCategory.NongSan;
    private string selectedItemId;
    private int transferQuantity = 1;
    private int warehouseLevel = 1;
    private int slotCapacity = 25;
    private bool popupInputLockHeld;

    private static readonly HashSet<string> AnimalItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "egg", "milk", "chickenmeat", "beef", "pork", "long_vu", "thit_bo", "thit_heo", "thit_ga", "trung", "sua"
    };

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            parentCanvas.gameObject.SetActive(true);

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (slotPrefab != null && slotPrefab.transform.parent == itemGridContainer)
            slotPrefab.SetActive(false);

        LoadWarehouseProgress();
        BuildLookups();
        WireButtons();
    }

    private void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void OnDestroy()
    {
        ReleasePopupInputBlock();

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    private void LoadWarehouseProgress()
    {
        warehouseLevel = Mathf.Clamp(PlayerPrefs.GetInt(WarehouseLevelPrefsKey, 1), 1, WarehouseMaxLevel);
        slotCapacity = FarmInventoryManager.CapacityForLevel(warehouseLevel);
    }

    private void SaveWarehouseProgress()
    {
        warehouseLevel = Mathf.Clamp(warehouseLevel, 1, WarehouseMaxLevel);
        PlayerPrefs.SetInt(WarehouseLevelPrefsKey, warehouseLevel);
        LuuGopPrefs.Hen();
        slotCapacity = FarmInventoryManager.CapacityForLevel(warehouseLevel);
    }

    private void BuildLookups()
    {
        cropLookup.Clear();
        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null) continue;
            string key = GetHarvestItemId(crop);
            if (!string.IsNullOrEmpty(key) && !cropLookup.ContainsKey(key))
                cropLookup.Add(key, crop);
            if (!string.IsNullOrEmpty(crop.cropId) && !cropLookup.ContainsKey(crop.cropId))
                cropLookup.Add(crop.cropId, crop);
        }

        extraItemLookup.Clear();
        for (int i = 0; i < extraItemDatabase.Count; i++)
        {
            InventoryItemData item = extraItemDatabase[i];
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (!extraItemLookup.ContainsKey(item.itemId))
                extraItemLookup.Add(item.itemId, item);
        }
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePopup();
            }
        }
    }

    private void WireButtons()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (popupRoot != null)
        {
            Transform dim = popupRoot.transform.Find("Panel_Dim");
            if (dim != null)
            {
                Button dimBtn = dim.GetComponent<Button>();
                if (dimBtn == null) dimBtn = dim.gameObject.AddComponent<Button>();
                dimBtn.transition = Selectable.Transition.None;
                dimBtn.onClick.RemoveAllListeners();
                dimBtn.onClick.AddListener(ClosePopup);
            }
        }

        if (btnTabNongSan != null) btnTabNongSan.onClick.AddListener(() => SetCategory(WarehouseCategory.NongSan));
        if (btnTabChanNuoi != null) btnTabChanNuoi.onClick.AddListener(() => SetCategory(WarehouseCategory.ChanNuoi));
        if (btnTabMonAn != null) btnTabMonAn.onClick.AddListener(() => SetCategory(WarehouseCategory.MonAn));

        if (btnMinus != null) btnMinus.onClick.AddListener(OnMinusClicked);
        if (btnPlus != null) btnPlus.onClick.AddListener(OnPlusClicked);
        if (btnMax != null) btnMax.onClick.AddListener(OnMaxClicked);
        if (btnTransferKitchen != null) btnTransferKitchen.onClick.AddListener(OnTransferKitchenClicked);

        if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
    }

    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            Transform p = popupRoot.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                    p.gameObject.SetActive(true);
                p = p.parent;
            }

            popupRoot.SetActive(true);
            EnsurePopupRaycastBlock();
        }

        LoadWarehouseProgress();
        BuildLookups();
        SetCategory(currentCategory);
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void SetCategory(WarehouseCategory category)
    {
        currentCategory = category;
        UpdateTabVisuals();
        selectedItemId = null;
        transferQuantity = 1;

        if (itemGridContainer != null)
        {
            ScrollRect sr = itemGridContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        RefreshUI();
        AutoSelectFirstItem();
    }

    private void UpdateTabVisuals()
    {
        Color activeTextColor = new Color(0.36f, 0.20f, 0.09f, 1f);   // #5B3417 bold dark brown
        Color inactiveTextColor = new Color(0.43f, 0.25f, 0.08f, 1f); // #6E4014 warm brown

        UpdateSingleTabVisual(imgTabNongSan, txtTabNongSan, rectTabNongSan, currentCategory == WarehouseCategory.NongSan, activeTextColor, inactiveTextColor);
        UpdateSingleTabVisual(imgTabChanNuoi, txtTabChanNuoi, rectTabChanNuoi, currentCategory == WarehouseCategory.ChanNuoi, activeTextColor, inactiveTextColor);
        UpdateSingleTabVisual(imgTabMonAn, txtTabMonAn, rectTabMonAn, currentCategory == WarehouseCategory.MonAn, activeTextColor, inactiveTextColor);
    }

    private void UpdateSingleTabVisual(Image img, TMP_Text txt, RectTransform rect, bool isActive, Color activeColor, Color inactiveColor)
    {
        if (img != null && tabActiveSprite != null && tabInactiveSprite != null)
            img.sprite = isActive ? tabActiveSprite : tabInactiveSprite;

        if (txt != null)
            txt.color = isActive ? activeColor : inactiveColor;

        if (rect != null)
        {
            Vector2 pos = rect.anchoredPosition;
            pos.y = isActive ? 0f : -6f; // Tab active nhô cao hơn tab inactive
            rect.anchoredPosition = pos;
        }
    }

    public void RefreshUI()
    {
        RefreshSlots();
        RefreshCapacityBar();
        RefreshDetailPanel();
        RefreshUpgradeBox();
    }

    private void EnsureSlotPool(int totalSlotsToRender)
    {
        if (itemGridContainer == null) return;

        if (slotPrefab != null && slotPrefab.transform.parent == itemGridContainer)
        {
            slotPrefab.SetActive(false);
        }

        if (slots.Count == 0)
        {
            var existing = itemGridContainer.GetComponentsInChildren<WarehouseSlotUI>(true);
            foreach (var s in existing)
            {
                if (slotPrefab != null && s.gameObject == slotPrefab) continue;
                s.SetSprites(slotNormalSprite, slotSelectedSprite, slotEmptySprite);
                slots.Add(s);
            }
        }

        while (slots.Count < totalSlotsToRender)
        {
            GameObject slotGO = null;
            if (slotPrefab != null)
            {
                slotGO = Instantiate(slotPrefab, itemGridContainer);
                slotGO.SetActive(true);
            }
            else
            {
                slotGO = new GameObject("slot_" + (slots.Count + 1), typeof(RectTransform));
                slotGO.transform.SetParent(itemGridContainer, false);
                slotGO.AddComponent<WarehouseSlotUI>();
            }

            WarehouseSlotUI slotUI = slotGO.GetComponent<WarehouseSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSprites(slotNormalSprite, slotSelectedSprite, slotEmptySprite);
                slots.Add(slotUI);
            }
        }
    }

    private void RefreshSlots()
    {
        if (itemGridContainer == null) return;

        List<WarehouseViewItem> categoryItems = GetItemsForCategory(currentCategory);
        int totalSlotsToRender = Mathf.Max(categoryItems.Count, minDisplaySlots);

        EnsureSlotPool(totalSlotsToRender);

        for (int i = 0; i < slots.Count; i++)
        {
            WarehouseSlotUI slotUI = slots[i];
            if (slotUI == null) continue;

            if (i < categoryItems.Count)
            {
                WarehouseViewItem item = categoryItems[i];
                bool isSelected = !string.IsNullOrEmpty(selectedItemId) &&
                                  string.Equals(selectedItemId, item.itemId, StringComparison.OrdinalIgnoreCase);
                slotUI.SetData(item.itemId, item.icon, item.amount, isSelected, OnSlotClicked);
                slotUI.gameObject.SetActive(true);
            }
            else if (i < totalSlotsToRender)
            {
                slotUI.SetEmpty();
                slotUI.gameObject.SetActive(true);
            }
            else
            {
                slotUI.gameObject.SetActive(false);
            }
        }
    }

    private void RefreshCapacityBar()
    {
        int storedKinds = 0;
        if (FarmInventoryManager.Instance != null)
            storedKinds = FarmInventoryManager.Instance.GetOrderedItems().Count;

        if (txtCapacity != null)
            txtCapacity.text = $"{storedKinds}/{slotCapacity} Slot";

        if (imgCapacityFill != null)
        {
            float fill = slotCapacity > 0 ? Mathf.Clamp01((float)storedKinds / slotCapacity) : 0f;
            imgCapacityFill.fillAmount = fill;
        }
    }

    private void RefreshDetailPanel()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null)
        {
            if (detailPanelRoot != null) detailPanelRoot.SetActive(true);
            if (txtDetailTitle != null) txtDetailTitle.text = "Chọn vật phẩm";
            if (txtDetailDesc != null) txtDetailDesc.text = "Nhấp vào vật phẩm ở danh sách bên trái để xem thông tin chi tiết và chuyển sang bếp.";
            if (imgDetailIcon != null) { imgDetailIcon.sprite = null; imgDetailIcon.enabled = false; }
            if (txtTransferCount != null) txtTransferCount.text = "0";
            if (btnMinus != null) btnMinus.interactable = false;
            if (btnPlus != null) btnPlus.interactable = false;
            if (btnMax != null) btnMax.interactable = false;
            if (btnTransferKitchen != null) btnTransferKitchen.interactable = false;
            return;
        }

        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        if (available <= 0)
        {
            selectedItemId = null;
            RefreshDetailPanel();
            return;
        }

        transferQuantity = Mathf.Clamp(transferQuantity, 1, available);

        string displayName = GetItemDisplayName(selectedItemId);
        Sprite icon = GetItemIcon(selectedItemId);
        string description = GetItemDescription(selectedItemId);

        if (txtDetailTitle != null)
            txtDetailTitle.text = $"{displayName} · x{available}";

        if (imgDetailIcon != null)
        {
            imgDetailIcon.sprite = icon;
            imgDetailIcon.enabled = icon != null;
        }

        if (txtDetailDesc != null)
            txtDetailDesc.text = description;

        if (txtTransferCount != null)
            txtTransferCount.text = transferQuantity.ToString();

        bool canTransfer = IsTransferrableToKitchen(selectedItemId);
        if (btnMinus != null) btnMinus.interactable = canTransfer && transferQuantity > 1;
        if (btnPlus != null) btnPlus.interactable = canTransfer && transferQuantity < available;
        if (btnMax != null) btnMax.interactable = canTransfer && transferQuantity < available;
        if (btnTransferKitchen != null)
        {
            btnTransferKitchen.interactable = canTransfer && available > 0;

            // Sếp 2026-08-27: nút phải TỰ NÓI lý do khi không gửi được (hạt giống/món ăn/đồ
            // linh tinh) — tránh hiểu nhầm "gửi bếp bị hỏng" khi thực ra item không nấu được.
            var lbl = btnTransferKitchen.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (lbl != null) lbl.text = canTransfer ? "CHUYỂN BẾP" : "KHÔNG PHẢI ĐỒ NẤU";
        }
        Debug.Log($"[WarehousePopupUI] Chọn '{selectedItemId}' → gửi bếp: {(canTransfer ? "ĐƯỢC" : "KHÔNG (không phải nguyên liệu nấu)")}");
    }

    private void RefreshUpgradeBox()
    {
        if (txtUpgradeInfo != null)
        {
            if (warehouseLevel < WarehouseMaxLevel)
            {
                int nextCap = FarmInventoryManager.CapacityForLevel(warehouseLevel + 1);
                txtUpgradeInfo.text = $"Cấp {warehouseLevel} · Sức chứa: {slotCapacity} Slot (Nâng cấp: +25 Slot)";
            }
            else
            {
                txtUpgradeInfo.text = $"Cấp Tối Đa ({warehouseLevel}) · Sức chứa: {slotCapacity} Slot";
            }
        }

        if (btnUpgrade != null)
            btnUpgrade.interactable = warehouseLevel < WarehouseMaxLevel;
    }

    private void AutoSelectFirstItem()
    {
        List<WarehouseViewItem> items = GetItemsForCategory(currentCategory);
        if (items.Count > 0)
        {
            selectedItemId = items[0].itemId;
            transferQuantity = 1;
        }
        else
        {
            selectedItemId = null;
            transferQuantity = 1;
        }
        RefreshUI();
    }

    private void OnSlotClicked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        selectedItemId = itemId;
        transferQuantity = 1;
        RefreshUI();
    }

    private void OnMinusClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        transferQuantity = Mathf.Max(1, transferQuantity - 1);
        RefreshDetailPanel();
    }

    private void OnPlusClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        transferQuantity = Mathf.Min(available, transferQuantity + 1);
        RefreshDetailPanel();
    }

    private void OnMaxClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        transferQuantity = Mathf.Max(1, available);
        RefreshDetailPanel();
    }

    private void OnTransferKitchenClicked()
    {
        if (string.IsNullOrWhiteSpace(selectedItemId) || transferQuantity <= 0) return; // WhiteSpace: chặn cả id ' ' (Sếp 2026-08-27)
        if (!IsTransferrableToKitchen(selectedItemId))
        {
            Debug.LogWarning($"[WarehousePopupUI] '{selectedItemId}' không phải là nguyên liệu nấu ăn, không thể chuyển sang Bếp!");
            return;
        }
        if (FarmInventoryManager.Instance == null) return;

        // ══ BUG GỐC (có từ trước, tìm ra 2026-08-27): RemoveItem bắn sự kiện "kho đổi" →
        // RefreshUI chạy NGAY GIỮA hàm này → khi chuyển HẾT SẠCH một món (bấm MAX), món đó
        // biến khỏi danh sách → selectedItemId bị reset thành null TRƯỚC khi kịp cộng vào
        // bếp → AddTransferredItem(null) lặng lẽ bỏ qua → "Đã chuyển Nx ''" và hàng bốc hơi
        // (kho nông trại đã trừ, bếp không nhận). Gửi 1 cái thì không sao vì món chưa hết.
        // FIX: CHỐT id + số lượng vào biến cục bộ TRƯỚC khi đụng RemoveItem.
        string rawId = selectedItemId;
        string kitchenId = KitchenIdMap.NormalizeFarmId(rawId);

        int available = FarmInventoryManager.Instance.GetAmount(rawId);
        int amountToTransfer = Mathf.Min(transferQuantity, available);
        if (amountToTransfer <= 0) return;

        // Deduct from farm inventory (có thể gọi ngược RefreshUI — từ đây chỉ dùng biến cục bộ)
        bool removed = FarmInventoryManager.Instance.RemoveItem(rawId, amountToTransfer);
        if (removed)
        {
            // Add to kitchen transfer — lưu id CHUẨN (itemId của InventoryItemData) để bếp
            // nhận diện được cả các món có id kho khác id item (vd 'nam' → 'mushroom').
            if (KitchenTransferManager.Instance != null)
                KitchenTransferManager.Instance.AddTransferredItem(kitchenId, amountToTransfer);

            Debug.Log($"[WarehousePopupUI] Đã chuyển {amountToTransfer}x '{rawId}' (id bếp '{kitchenId}') sang Bếp thành công!");
        }

        // Refresh UI
        int remain = FarmInventoryManager.Instance.GetAmount(rawId);
        if (remain <= 0)
            AutoSelectFirstItem();
        else
        {
            transferQuantity = Mathf.Clamp(transferQuantity, 1, remain);
            RefreshUI();
        }
    }

    private void OnUpgradeClicked()
    {
        if (warehouseLevel >= WarehouseMaxLevel) return;

        // Perform warehouse level upgrade
        warehouseLevel++;
        SaveWarehouseProgress();
        Debug.Log($"[WarehousePopupUI] Nâng cấp kho lên Cấp {warehouseLevel} (Sức chứa: {slotCapacity} Slot)!");

        RefreshUI();
    }

    // ── Item Classification & Data Helpers ────────────────────────────────────

    private List<WarehouseViewItem> GetItemsForCategory(WarehouseCategory category)
    {
        List<WarehouseViewItem> result = new List<WarehouseViewItem>();
        if (FarmInventoryManager.Instance == null) return result;

        var allItems = FarmInventoryManager.Instance.GetOrderedItems();

        foreach (var kv in allItems)
        {
            string id = kv.Key;
            int amount = kv.Value;
            if (amount <= 0) continue;
            if (string.IsNullOrWhiteSpace(id))
            {
                // Entry hỏng trong save (id rỗng) — không hiển thị để khỏi bấm gửi nhầm nữa.
                Debug.LogWarning($"[WarehousePopupUI] Kho có entry id RỖNG (x{amount}) trong save — đã ẩn khỏi danh sách. [Sếp 2026-08-27]");
                continue;
            }

            WarehouseCategory itemCat = ClassifyItem(id);
            if (itemCat == category)
            {
                result.Add(new WarehouseViewItem
                {
                    itemId = id,
                    displayName = GetItemDisplayName(id),
                    icon = GetItemIcon(id),
                    amount = amount
                });
            }
        }

        return result;
    }

    private WarehouseCategory ClassifyItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return WarehouseCategory.NongSan;

        string key = itemId.Trim().ToLowerInvariant();

        // 1. Check StallItemCatalog
        if (StallItemCatalog.Instance != null)
        {
            StallItemCategory cat = StallItemCatalog.Instance.GetCategory(key);
            if (cat == StallItemCategory.NongSan || cat == StallItemCategory.Hoa || cat == StallItemCategory.HatGiong)
                return WarehouseCategory.NongSan;
        }

        // 2. Check Animal product
        if (AnimalItemIds.Contains(key) || key.Contains("egg") || key.Contains("milk") ||
            key.Contains("beef") || key.Contains("pork") || key.Contains("chicken") ||
            key.Contains("trung") || key.Contains("sua") || key.Contains("thit") || key.Contains("long_vu"))
        {
            return WarehouseCategory.ChanNuoi;
        }

        // 3. Check Cooked Dish or Processed Good
        if (IsCookedDish(key) || key.StartsWith("item_") || key.Contains("xao") || key.Contains("ham") ||
            key.Contains("nuoc_mia") || key.Contains("bot_gao") || key.Contains("pho_mai") ||
            key.Contains("salad") || key.Contains("sup_") || key.Contains("chien") || key.Contains("pho_") ||
            key.Contains("banh") || key.Contains("nuoc") || key.Contains("che_bien"))
        {
            return WarehouseCategory.MonAn;
        }

        // 4. Check Crop Database
        if (cropLookup.ContainsKey(key))
        {
            return WarehouseCategory.NongSan;
        }

        // Default to NongSan
        return WarehouseCategory.NongSan;
    }

    private bool IsCookedDish(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        string key = itemId.Trim().ToLowerInvariant();

        for (int i = 0; i < cookedDishIds.Count; i++)
        {
            if (string.IsNullOrEmpty(cookedDishIds[i])) continue;
            if (string.Equals(cookedDishIds[i].Trim(), key, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private string GetItemDisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "";

        string key = itemId.Trim().ToLowerInvariant();

        if (StallItemCatalog.Instance != null)
        {
            string catName = StallItemCatalog.Instance.GetDisplayName(key);
            if (!string.IsNullOrEmpty(catName)) return catName;
        }

        if (cropLookup.TryGetValue(key, out CropData crop) && crop != null)
        {
            if (!string.IsNullOrEmpty(crop.displayName)) return crop.displayName;
            if (!string.IsNullOrEmpty(crop.cropId)) return crop.cropId;
        }

        if (extraItemLookup.TryGetValue(key, out InventoryItemData extra) && extra != null)
        {
            if (!string.IsNullOrEmpty(extra.displayName)) return extra.displayName;
            if (!string.IsNullOrEmpty(extra.itemId)) return extra.itemId;
        }

        return FormatFallbackName(itemId);
    }

    private Sprite GetItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        string key = itemId.Trim().ToLowerInvariant();

        // 1. Master StallItemCatalog
        if (StallItemCatalog.Instance != null)
        {
            Sprite catIcon = StallItemCatalog.Instance.GetIcon(key);
            if (catIcon != null) return catIcon;
        }

        // 2. Crop Lookup (seed -> itemIcon; harvest -> harvestIcon > readySprite > itemIcon)
        if (cropLookup.TryGetValue(key, out CropData crop) && crop != null)
        {
            if (key.StartsWith("seed_") || key.StartsWith("seed"))
            {
                if (crop.itemIcon != null) return crop.itemIcon;
            }
            if (crop.harvestIcon != null) return crop.harvestIcon;
            if (crop.readySprite != null) return crop.readySprite;
            if (crop.itemIcon != null) return crop.itemIcon;
        }

        // 3. Extra Item Lookup
        if (extraItemLookup.TryGetValue(key, out InventoryItemData extra) && extra != null)
        {
            if (extra.icon != null) return extra.icon;
        }

        // 4. OrderBoard resolver
        Sprite obIcon = OrderBoardIconResolver.GetIcon(key);
        if (obIcon != null) return obIcon;

        return null;
    }

    /// <summary>
    /// Bảng id NẤU ĐƯỢC — đã xác minh từng asset InventoryItemData (itemId + cookingData
    /// không rỗng, 2026-08-27). Đây là FALLBACK khi extraItemDatabase trong scene chưa nạp
    /// đủ (Editor giữ scene cũ trong RAM khi file bị sửa ngoài — bug Sếp gặp: 'bapcai' bị
    /// từ chối dù id đúng). Luật Sếp chốt: nông sản trồng ruộng + đồ chăn nuôi (trứng, thịt,
    /// sữa, cá) + nguyên liệu mua chợ (nước mắm...) đều gửi bếp được; hạt giống, vật liệu,
    /// công trình, món ăn thành phẩm thì KHÔNG. Thêm nguyên liệu mới → thêm id vào đây HOẶC
    /// chỉ cần gán cookingData cho asset (nhánh tra database phía dưới tự nhận).
    /// </summary>
    private static readonly HashSet<string> CookableIdsVerified = new HashSet<string>
    {
        // nông sản trồng ruộng
        "bapcai", "cachua", "khoaitay", "carot", "ngo", "sugarcane", "rice", "chili",
        "pepper", "lemon", "mushroom",
        // chăn nuôi
        "beef", "pork", "chicken_meat", "egg", "milk",
        // gia vị / nguyên liệu mua chợ
        "fishsauce", "salt", "soysauce", "herbs",
        // quả to (2026-08-27) — đã có IngredientData nên bếp nấu được
        "pumpkin", "watermelon",
    };

    private bool IsTransferrableToKitchen(string itemId)
    {
        // Sếp 2026-08-27 — DANH SÁCH CHO PHÉP (cổng cũ chỉ cấm công trình/hoa nên hạt giống
        // và entry id rỗng lọt qua, item "bốc hơi"). Điều kiện: item có cookingData (tra
        // database) HOẶC nằm trong bảng đã xác minh ở trên (miễn nhiễm scene cũ trong RAM).
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        string raw = KitchenIdMap.NormalizeFarmId(itemId); // alias đã xác minh: 'nam' → 'mushroom'

        InventoryItemData item;
        if (!extraItemLookup.TryGetValue(raw, out item) || item == null)
            extraItemLookup.TryGetValue(raw.ToLowerInvariant(), out item);

        if (item != null && item.cookingData != null)
            return true;

        return CookableIdsVerified.Contains(raw.ToLowerInvariant());
    }

    private string GetItemDescription(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "";

        string key = itemId.Trim().ToLowerInvariant();

        if (!IsTransferrableToKitchen(itemId))
        {
            if (key.StartsWith("seed_"))
                return "Hạt giống dùng để gieo trồng tại các ô đất nông trại. Không dùng làm nguyên liệu nấu ăn.";
            if (key.StartsWith("hoa_") || key.Contains("flower") || (cropLookup.TryGetValue(key, out var c) && c.cropCategory == CropCategory.Flower))
                return "Hoa trang trí và cảnh quan nông trại. Không chuyển vào bếp nấu ăn.";
            if (key.StartsWith("building_") || key.StartsWith("deco_"))
                return "Công trình / Trang trí nông trại. Không chuyển vào bếp.";
        }

        if (cropLookup.TryGetValue(key, out CropData crop) && crop != null)
        {
            int sellGold = crop.sellGold > 0 ? crop.sellGold : 12;
            return $"Nguyên liệu nông sản tươi ngon. Dùng để nấu ăn tại bếp hoặc bán tại quầy. Giá tham khảo {sellGold} vàng/cái.";
        }

        if (AnimalItemIds.Contains(key))
        {
            return "Nông phẩm chăn nuôi chất lượng cao. Cần thiết cho các món ăn dinh dưỡng tại bếp.";
        }

        if (IsCookedDish(key))
        {
            return "Món ăn đã được chế biến thơm ngon. Dùng để phục vụ thực khách tại nhà hàng.";
        }

        return "Vật phẩm lưu trữ trong kho nông trại. Dùng cho chế biến và hoàn thành đơn hàng.";
    }

    private string FormatFallbackName(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        string cleaned = id.Replace("seed_", "").Replace("item_", "").Replace("_", " ");
        if (cleaned.Length > 0)
            return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        return id;
    }

    private string GetHarvestItemId(CropData crop)
    {
        if (crop == null) return "";
        return string.IsNullOrEmpty(crop.harvestItemId) ? crop.cropId : crop.harvestItemId;
    }

    private void EnsurePopupRaycastBlock()
    {
        if (popupRoot == null) return;
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

    private class WarehouseViewItem
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int amount;
    }
}
