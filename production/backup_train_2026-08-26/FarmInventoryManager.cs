using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmInventoryManager : MonoBehaviour
{
    [Serializable]
    private class InventoryEntry
    {
        public string itemId;
        public int    amount;
    }

    [Serializable]
    private class InventorySaveData
    {
        /// <summary>
        /// v0 = save của bản chưa enforce sức chứa (không có khoá này → JsonUtility để 0).
        /// v1 = đã enforce. Cần đóng dấu version để lần sau đổi cấu trúc còn biết đường lùi;
        /// riêng v0 → v1 không phải dịch dữ liệu, chỉ cần CẮT phần vượt sức chứa (xem
        /// <see cref="LoadInventory"/>) vì save cũ có thể đang giữ nhiều loại hơn số slot.
        /// </summary>
        public int saveVersion;

        public List<InventoryEntry> entries   = new List<InventoryEntry>();
        public List<string>         itemOrder = new List<string>();
    }

    public static FarmInventoryManager Instance { get; private set; }

    private const string SaveKey = "FARM_INVENTORY_SAVE";
    private const int    CurrentSaveVersion = 1;

    // ── SỨC CHỨA KHO (F8) ─────────────────────────────────────────────────────
    // Sức chứa tính theo SỐ LOẠI vật phẩm (số slot), KHÔNG theo tổng số lượng — đúng như
    // `WarehousePopupUI` vẫn hiển thị "12 / 25". Một slot giữ bao nhiêu đơn vị cũng được.
    //
    // VÌ SAO chọn "số loại" chứ không phải "tổng số lượng": một lần thu hoạch trả về 4 đơn
    // vị và có 26 ô ruộng; nếu chặn theo tổng số lượng thì kho 25 đầy sau 7 lần thu hoạch
    // và người chơi không thu hoạch nổi ruộng của mình — tự khoá game. Chặn theo số LOẠI
    // thì luôn còn đường ra: loại nào đã có trong kho vẫn cộng thêm được bình thường.
    //
    // Ba hằng số này phải TRÙNG với `WarehousePopupUI`; để ở đây vì kho là nguồn sự thật,
    // popup chỉ là màn hình. WarehousePopupUI gọi CapacityForLevel() thay vì tự tính.
    public const string WarehouseLevelPrefsKey = "WAREHOUSE_LEVEL";
    public const int    SlotsPerWarehouseLevel = 25;
    public const int    MaxWarehouseLevel      = 7;

    public static int CapacityForLevel(int level) =>
        Mathf.Clamp(level, 1, MaxWarehouseLevel) * SlotsPerWarehouseLevel;

    /// <summary>Số slot kho hiện tại, đọc theo cấp kho đã nâng.</summary>
    public int SlotCapacity =>
        CapacityForLevel(PlayerPrefs.GetInt(WarehouseLevelPrefsKey, 1));

    /// <summary>Số slot đang bị chiếm = số LOẠI vật phẩm đang có trong kho.</summary>
    public int UsedSlots => items.Count;

    public bool IsFull => UsedSlots >= SlotCapacity;

    /// <summary>
    /// Bắn khi <see cref="AddItem"/> phải TỪ CHỐI vì kho hết slot.
    /// Tham số = itemId bị từ chối. UI nào muốn hiện popup "kho đầy" thì nghe ở đây;
    /// bản thân kho không được phụ thuộc UI vì nó là DontDestroyOnLoad và sống qua cả
    /// scene bếp (nơi không có FarmUIManager).
    /// </summary>
    public static Action<string> OnAddRejectedByCapacity;

    private readonly Dictionary<string, int> items     = new Dictionary<string, int>();
    private readonly List<string>            itemOrder = new List<string>();

    public Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null); // Tách ra root để DontDestroyOnLoad hoạt động (fix warning)
        DontDestroyOnLoad(gameObject);
        LoadInventory();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveInventory();
            LuuGopPrefs.LuuNgay();
            PlayerPrefs.Save();
        }
    }

    private void OnApplicationQuit()
    {
        SaveInventory();
        LuuGopPrefs.LuuNgay();
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        SaveInventory();
        LuuGopPrefs.LuuNgay();
        PlayerPrefs.Save();
    }

    // ── Key Normalization ─────────────────────────────────────────────────────
    // BUG-FIX (key mismatch): all keys are stored and queried lowercase + trimmed.
    // This makes itemId in OrderItemDefinition case-insensitive vs. keys added by harvesting.
    private static string NormalizeKey(string key) =>
        key?.Trim().ToLower() ?? string.Empty;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// TRUE nếu kho còn nhận được <paramref name="itemId"/>.
    /// Loại đã có trong kho thì LUÔN nhận thêm được (chỉ cộng vào slot cũ).
    /// </summary>
    public bool CanAddItem(string itemId)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key)) return false;

        return items.ContainsKey(key) || UsedSlots < SlotCapacity;
    }

    /// <summary>
    /// F8 — Cộng vật phẩm vào kho. Trả FALSE khi kho hết slot và đây là LOẠI MỚI.
    ///
    /// VÌ SAO đổi từ `void` sang `bool`: trước đây `WarehousePopupUI` ghi "12 / 25" mà
    /// AddItem không kiểm một dòng nào — thu hoạch bao nhiêu cũng vào hết, con số sức chứa
    /// chỉ là chữ trang trí. Người gọi nào cần biết kết quả (thu hoạch, thu chuồng, mua ở
    /// chợ) phải xử lý false, không thì vật phẩm bốc hơi trong im lặng.
    /// Các lời gọi cũ bỏ qua giá trị trả về vẫn biên dịch bình thường.
    /// </summary>
    public bool AddItem(string itemId, int amount)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key) || amount <= 0) return false;

        bool isNew = !items.ContainsKey(key);

        if (isNew && UsedSlots >= SlotCapacity)
        {
            Debug.LogWarning($"[FarmInventory] Kho ĐẦY ({UsedSlots}/{SlotCapacity} slot) — " +
                             $"từ chối nhận loại mới '{key}' x{amount}. Nâng cấp kho hoặc bán bớt.");
            OnAddRejectedByCapacity?.Invoke(key);
            return false;
        }

        if (isNew)
        {
            items[key] = 0;
            itemOrder.Add(key);
        }

        items[key] += amount;

        SaveInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetAmount(string itemId)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key)) return 0;
        return items.TryGetValue(key, out int value) ? value : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key) || amount <= 0) return false;
        return items.TryGetValue(key, out int value) && value >= amount;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key) || amount <= 0) return false;

        if (!items.TryGetValue(key, out int current)) return false;
        if (current < amount) return false;

        current -= amount;

        if (current <= 0)
        {
            items.Remove(key);
            itemOrder.Remove(key);
        }
        else
        {
            items[key] = current;
        }

        SaveInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public List<KeyValuePair<string, int>> GetOrderedItems()
    {
        var result = new List<KeyValuePair<string, int>>();
        foreach (string id in itemOrder)
        {
            if (items.TryGetValue(id, out int amount) && amount > 0)
                result.Add(new KeyValuePair<string, int>(id, amount));
        }
        return result;
    }

    public void ClearAll()
    {
        items.Clear();
        itemOrder.Clear();
        SaveInventory();
        OnInventoryChanged?.Invoke();
    }

    [ContextMenu("Debug Print Inventory")]
    public void DebugPrintInventory()
    {
        if (items.Count == 0) { Debug.Log("[FarmInventory] Inventory rỗng."); return; }
        foreach (var kv in items) ;
        
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private void SaveInventory()
    {
        var data = new InventorySaveData { saveVersion = CurrentSaveVersion };
        foreach (string id in itemOrder)
        {
            if (!items.TryGetValue(id, out int amount) || amount <= 0) continue;
            data.entries.Add(new InventoryEntry { itemId = id, amount = amount });
            data.itemOrder.Add(id);
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    private void LoadInventory()
    {
        items.Clear();
        itemOrder.Clear();

        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data?.entries == null) return;

        foreach (var entry in data.entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.amount <= 0) continue;
            // Normalize on load so old saves with mixed-case keys are fixed automatically.
            string key = NormalizeKey(entry.itemId);
            items[key] = items.ContainsKey(key) ? items[key] + entry.amount : entry.amount;
            if (!itemOrder.Contains(key)) itemOrder.Add(key);
        }

        // Preserve saved order where possible.
        if (data.itemOrder != null)
        {
            var reordered = new List<string>();
            foreach (string id in data.itemOrder)
            {
                string key = NormalizeKey(id);
                if (items.ContainsKey(key) && !reordered.Contains(key)) reordered.Add(key);
            }
            foreach (var kv in items)
            {
                if (!reordered.Contains(kv.Key)) reordered.Add(kv.Key);
            }
            itemOrder.Clear();
            itemOrder.AddRange(reordered);
        }

        // ── CHUYỂN ĐỔI save v0 → v1 (F8) ─────────────────────────────────────
        // Save cũ được ghi khi KHÔNG có ai kiểm sức chứa, nên có thể đang giữ nhiều loại
        // hơn số slot của cấp kho hiện tại. Nếu để nguyên thì `UsedSlots > SlotCapacity`
        // vĩnh viễn và người chơi không bao giờ nhận được loại mới nào nữa, kể cả sau khi
        // nâng cấp kho — bế tắc không có cách thoát.
        //
        // Cách xử lý: KHÔNG xoá vật phẩm của người chơi (họ đã bỏ công làm ra). Chỉ ghi
        // cảnh báo và đóng dấu version; phần vượt sẽ tự tiêu biến khi họ bán/dùng bớt,
        // và trong lúc đó `CanAddItem` vẫn cho cộng vào những loại đã có.
        if (data.saveVersion < CurrentSaveVersion && items.Count > SlotCapacity)
        {
            Debug.LogWarning($"[FarmInventory] Save cũ (v{data.saveVersion}) đang giữ {items.Count} loại " +
                             $"trong kho {SlotCapacity} slot. Giữ nguyên hàng của người chơi, " +
                             $"chỉ tạm không nhận thêm LOẠI MỚI cho tới khi dùng bớt.");
        }

        if (data.saveVersion < CurrentSaveVersion)
            SaveInventory();   // đóng dấu v1, lần sau không chạy nhánh này nữa
    }
}
