using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WarehouseItemEntry
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int amount;

    public WarehouseItemEntry(string itemId, string displayName, Sprite icon, int amount)
    {
        this.itemId = itemId;
        this.displayName = displayName;
        this.icon = icon;
        this.amount = amount;
    }
}

/// <summary>
/// HỆ THỐNG KHO THỐNG NHẤT (Unified Warehouse Bridge):
/// Toàn bộ dữ liệu vật phẩm (hạt giống, nông sản, chăn nuôi, chế biến, nấu nướng)
/// đều được quản lý tập trung 100% tại FarmInventoryManager.
/// Lớp này đóng vai trò cầu nối tương thích để mọi hệ thống (gieo hạt, shop, market)
/// truy xuất cùng một kho dữ liệu duy nhất mà không bị phân mảnh.
/// </summary>
public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    public Action OnWarehouseChanged;

    public IReadOnlyList<WarehouseItemEntry> Items
    {
        get
        {
            var list = new List<WarehouseItemEntry>();
            if (FarmInventoryManager.Instance == null) return list;

            var ordered = FarmInventoryManager.Instance.GetOrderedItems();
            foreach (var kv in ordered)
            {
                string id = kv.Key;
                int amt = kv.Value;
                if (amt <= 0) continue;

                Sprite ic = StallItemCatalog.Instance != null ? StallItemCatalog.Instance.GetIcon(id) : null;
                string name = StallItemCatalog.Instance != null ? StallItemCatalog.Instance.GetDisplayName(id) : id;
                list.Add(new WarehouseItemEntry(id, name, ic, amt));
            }
            return list;
        }
    }

    private const string LegacySaveKey = "FARM_WAREHOUSE";

    public static bool DaCoSaveKho =>
        PlayerPrefs.HasKey(FarmInventoryManager.WarehouseLevelPrefsKey) || PlayerPrefs.HasKey("FARM_INVENTORY_SAVE");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        MigrateLegacySave();
    }

    private void Start()
    {
        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
            FarmInventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;

        if (Instance == this) Instance = null;
    }

    private void HandleInventoryChanged()
    {
        OnWarehouseChanged?.Invoke();
    }

    private void MigrateLegacySave()
    {
        if (!PlayerPrefs.HasKey(LegacySaveKey)) return;

        string json = PlayerPrefs.GetString(LegacySaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var data = JsonUtility.FromJson<LegacySaveData>(json);
            if (data != null && data.list != null && FarmInventoryManager.Instance != null)
            {
                foreach (var e in data.list)
                {
                    if (e != null && !string.IsNullOrEmpty(e.itemId) && e.amount > 0)
                    {
                        FarmInventoryManager.Instance.AddItem(e.itemId, e.amount);
                    }
                }
                Debug.Log($"[WarehouseManager] Đã chuyển đổi thành công {data.list.Count} loại hạt giống cũ vào Kho Thống Nhất.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WarehouseManager] Không thể giải mã save kho cũ ({e.Message}), bỏ qua.");
        }

        // Xóa key cũ sau khi chuyển đổi
        PlayerPrefs.DeleteKey(LegacySaveKey);
        PlayerPrefs.Save();
    }

    public void AddItem(string itemId, string displayName, Sprite icon, int amount)
    {
        AddItem(itemId, amount);
    }

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.AddItem(itemId, amount);
        }
    }

    public int GetAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return FarmInventoryManager.Instance != null ? FarmInventoryManager.Instance.GetAmount(itemId) : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        return FarmInventoryManager.Instance != null && FarmInventoryManager.Instance.HasItem(itemId, amount);
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        return FarmInventoryManager.Instance != null && FarmInventoryManager.Instance.RemoveItem(itemId, amount);
    }

    public void ClearAll()
    {
        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.ClearAll();
    }

    public void XoaSaveVaLamTrongKho()
    {
        ClearAll();
        PlayerPrefs.DeleteKey(LegacySaveKey);
        PlayerPrefs.Save();
    }

    public void GhiSaveNgay()
    {
        PlayerPrefs.Save();
    }

    [Serializable]
    private class LegacySaveEntry
    {
        public string itemId;
        public string displayName;
        public int amount;
    }

    [Serializable]
    private class LegacySaveData
    {
        public int saveVersion;
        public List<LegacySaveEntry> list = new List<LegacySaveEntry>();
    }
}
