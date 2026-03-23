using System;
using System.Collections.Generic;
using UnityEngine;

public class WarehouseInventory : MonoBehaviour
{
    public static WarehouseInventory Instance { get; private set; }

    [Serializable]
    public class ItemEntry
    {
        public string itemId;
        public int amount;
    }

    [Header("Starting Items For Test")]
    [SerializeField]
    private List<ItemEntry> startingItems = new List<ItemEntry>()
    {
        new ItemEntry { itemId = "lua", amount = 20 },
        new ItemEntry { itemId = "beef", amount = 0 }
    };

    private readonly Dictionary<string, int> itemMap = new Dictionary<string, int>();

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        itemMap.Clear();
        for (int i = 0; i < startingItems.Count; i++)
        {
            ItemEntry entry = startingItems[i];
            if (string.IsNullOrWhiteSpace(entry.itemId))
                continue;

            if (!itemMap.ContainsKey(entry.itemId))
                itemMap.Add(entry.itemId, 0);

            itemMap[entry.itemId] += Mathf.Max(0, entry.amount);
        }
    }

    public int GetAmount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return 0;

        return itemMap.TryGetValue(itemId, out int amount) ? amount : 0;
    }

    public bool HasEnough(string itemId, int amount)
    {
        return GetAmount(itemId) >= amount;
    }

    public bool TryConsume(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return false;

        int current = GetAmount(itemId);
        if (current < amount)
        {
            Debug.Log($"[Warehouse] Không đủ item '{itemId}'. Cần {amount}, hiện có {current}");
            return false;
        }

        itemMap[itemId] = current - amount;
        Debug.Log($"[Warehouse] Consume {itemId} x{amount} | Còn lại: {itemMap[itemId]}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return;

        if (!itemMap.ContainsKey(itemId))
            itemMap.Add(itemId, 0);

        itemMap[itemId] += amount;

        Debug.Log($"[Warehouse] Add {itemId} x{amount} | Tổng: {itemMap[itemId]}");
        OnInventoryChanged?.Invoke();
    }
}