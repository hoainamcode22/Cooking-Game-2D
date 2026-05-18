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

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    [SerializeField] private List<WarehouseItemEntry> items = new List<WarehouseItemEntry>();

    public Action OnWarehouseChanged;

    public List<WarehouseItemEntry> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Thêm vật phẩm trực tiếp bằng id/tên/icon
    /// </summary>
    public void AddItem(string itemId, string displayName, Sprite icon, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);

        if (found != null)
        {
            found.amount += amount;
        }
        else
        {
            items.Add(new WarehouseItemEntry(itemId, displayName, icon, amount));
        }

        Debug.Log($"[Warehouse] AddItem -> {itemId} +{amount}");
        OnWarehouseChanged?.Invoke();
    }

    /// <summary>
    /// Thêm vật phẩm từ CropData sau khi thu hoạch
    /// </summary>
    public void AddHarvest(CropData cropData, int amount)
    {
        if (cropData == null || amount <= 0)
            return;

        string itemId = cropData.harvestItemId;
        string displayName = string.IsNullOrEmpty(cropData.displayName) ? cropData.cropId : cropData.displayName;

        // ưu tiên icon trong crop
        Sprite icon = cropData.icon;

        AddItem(itemId, displayName, icon, amount);
    }

    public int GetAmount(string itemId)
    {
        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);
        return found != null ? found.amount : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetAmount(itemId) >= amount;
    }

    /// <summary>
    /// Trừ vật phẩm khỏi kho. Trả về false nếu không đủ.
    /// </summary>
    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);
        if (found == null || found.amount < amount)
        {
            Debug.LogWarning($"[Warehouse] Không đủ {itemId} để trừ (cần {amount}, có {(found?.amount ?? 0)})");
            return false;
        }

        found.amount -= amount;
        if (found.amount <= 0) items.Remove(found);

        Debug.Log($"[Warehouse] RemoveItem -> {itemId} -{amount}");
        OnWarehouseChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        items.Clear();
        OnWarehouseChanged?.Invoke();
    }
}