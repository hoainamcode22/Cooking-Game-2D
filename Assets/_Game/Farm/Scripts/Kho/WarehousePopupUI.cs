using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehousePopupUI : MonoBehaviour
{
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

    private Dictionary<string, CropData> cropLookup = new Dictionary<string, CropData>();
    private Dictionary<string, InventoryItemData> extraItemLookup = new Dictionary<string, InventoryItemData>();

    private readonly Dictionary<string, int> pendingSelection = new Dictionary<string, int>();

    private string lastSelectedItemId;
    private bool popupInputLockHeld;

    private void Awake()
    {
        InitSlots();
        BuildCropLookup();
        BuildExtraItemLookup();

        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);

        if (btnSearch != null)
            btnSearch.onClick.AddListener(RefreshUI);

        if (inputSearch != null)
            inputSearch.onSubmit.AddListener(_ => RefreshUI());

        if (btnSendToKitchen != null)
            btnSendToKitchen.onClick.AddListener(SendPendingItemsToKitchen);

        RefreshSelectedPreview();
    }

    private void Start()
    {
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
            Debug.LogWarning("[WarehousePopupUI] Chưa gán slotPrefab hoặc itemGridContainer. Bỏ qua generate slot.");
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
                Debug.LogError("[WarehousePopupUI] slotPrefab thiếu component WarehouseSlotUI!");
                continue;
            }

            slots.Add(slotUI);
        }

        Debug.Log($"[WarehousePopupUI] Đã tạo {slots.Count} slot.");
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
        Debug.Log("[WarehousePopupUI] OpenPopup");
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);

        Debug.Log("[WarehousePopupUI] ClosePopup");
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
            Debug.Log("[WarehousePopupUI] Đây là món ăn đã nấu, không thể gửi lại sang Cooking: " + itemId);
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
            Debug.Log("[WarehousePopupUI] Không còn vật phẩm để chọn thêm: " + itemId);
            return;
        }

        if (!pendingSelection.ContainsKey(itemId))
            pendingSelection[itemId] = 0;

        pendingSelection[itemId] += 1;
        lastSelectedItemId = itemId;

        RefreshUI();
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
            Debug.LogWarning("[WarehousePopupUI] Chưa có KitchenTransferManager trong scene.");
            return;
        }

        if (FarmInventoryManager.Instance == null)
        {
            Debug.LogWarning("[WarehousePopupUI] Chưa có FarmInventoryManager.");
            return;
        }

        Debug.Log("[WarehousePopupUI] Số item đang chọn: " + pendingSelection.Count);//Nguyên thêm

        foreach (var kv in pendingSelection)
        {
            Debug.Log($"[WarehousePopupUI] Chuẩn bị gửi: {kv.Key} x{kv.Value}");//Nguyên thêm
            if (kv.Value <= 0)
                continue;

            // chỉ chuyển nếu kho thật còn đủ
            if (!FarmInventoryManager.Instance.HasItem(kv.Key, kv.Value))
            {
                Debug.LogWarning("[WarehousePopupUI] Không đủ vật phẩm trong kho: " + kv.Key);
                continue;
            }

            // trừ kho thật
            bool removed = FarmInventoryManager.Instance.RemoveItem(kv.Key, kv.Value);
            if (!removed)
            {
                Debug.LogWarning("[WarehousePopupUI] Trừ kho thất bại: " + kv.Key);
                continue;
            }

            // đưa sang bếp
            KitchenTransferManager.Instance.AddTransferredItem(kv.Key, kv.Value);
        }

        pendingSelection.Clear();
        lastSelectedItemId = null;

        RefreshUI();
        Debug.Log("[WarehousePopupUI] Đã đưa vật phẩm sang bếp.");
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
