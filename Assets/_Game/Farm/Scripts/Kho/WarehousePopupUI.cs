using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WarehousePopupUI : MonoBehaviour
{
    [System.Serializable]
    private class WarehouseViewItem
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int amount;
    }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;   // kéo Frame vào đây

    [Header("Close Button")]
    [SerializeField] private Button btnClose;

    [Header("Search")]
    [SerializeField] private TMP_InputField inputSearch;
    [SerializeField] private Button btnSearch;

    [Header("Slots")]
    [SerializeField] private List<WarehouseSlotUI> slots = new List<WarehouseSlotUI>();

    [Header("Crop Database")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();

    [Header("Extra Item Database")]
    [SerializeField] private List<InventoryItemData> extraItemDatabase = new List<InventoryItemData>();

    private Dictionary<string, CropData> cropLookup = new Dictionary<string, CropData>();
    private Dictionary<string, InventoryItemData> extraItemLookup = new Dictionary<string, InventoryItemData>();

    private void Awake()
    {
        // build lookup crop
        BuildCropLookup();

        // build lookup item ngoài crop
        BuildExtraItemLookup();

        if (btnClose != null)
            btnClose.onClick.AddListener(ClosePopup);

        if (btnSearch != null)
            btnSearch.onClick.AddListener(RefreshUI);

        if (inputSearch != null)
            inputSearch.onSubmit.AddListener(_ => RefreshUI());

        if (popupRoot != null)
            popupRoot.SetActive(false);
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

    // build crop lookup theo harvestItemId
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

    // build lookup cho item động vật / item đặc biệt
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

    public void OpenPopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshUI();
        Debug.Log("[WarehousePopupUI] OpenPopup");
    }

    public void ClosePopup()
    {
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
                slots[i].SetData(items[i].icon, items[i].amount);
        }
    }

    // lấy item trong kho rồi lọc theo ô search
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

            // ưu tiên crop trước
            CropData crop = GetCropByItemId(itemId);
            if (crop != null)
            {
                displayName = GetDisplayName(crop);
                icon = crop.icon;
            }
            else
            {
                // nếu không phải crop thì tìm trong item data riêng
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

    // tìm crop theo itemId
    private CropData GetCropByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (cropLookup.TryGetValue(itemId, out CropData crop))
            return crop;

        return null;
    }

    // tìm item ngoài crop theo itemId
    private InventoryItemData GetExtraItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        if (extraItemLookup.TryGetValue(itemId, out InventoryItemData item))
            return item;

        return null;
    }

    // lấy id harvest của crop
    private string GetHarvestItemId(CropData crop)
    {
        if (crop == null)
            return "";

        return string.IsNullOrEmpty(crop.harvestItemId) ? crop.cropId : crop.harvestItemId;
    }

    // lấy tên hiển thị của crop
    private string GetDisplayName(CropData crop)
    {
        if (crop == null)
            return "";

        if (!string.IsNullOrEmpty(crop.displayName))
            return crop.displayName;

        return GetHarvestItemId(crop);
    }

    // bỏ dấu để search dễ hơn
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
}