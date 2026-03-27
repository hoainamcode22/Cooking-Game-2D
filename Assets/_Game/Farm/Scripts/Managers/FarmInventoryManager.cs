using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmInventoryManager : MonoBehaviour
{
    public static FarmInventoryManager Instance { get; private set; }

    private readonly Dictionary<string, int> items = new Dictionary<string, int>();
    private readonly List<string> itemOrder = new List<string>();

    public Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
/*
    // Chỉ để test lúc đầu, test xong thì xóa
    private void Start()
    {
        ClearAll();
    }
*/
    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        bool isNewItem = !items.ContainsKey(itemId);

        if (isNewItem)
        {
            items[itemId] = 0;
            itemOrder.Add(itemId);   // lưu thứ tự xuất hiện lần đầu
        }

        items[itemId] += amount;

        Debug.Log($"[FarmInventory] AddItem: {itemId} = {items[itemId]}");
        OnInventoryChanged?.Invoke();
    }

    public int GetAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        return items.TryGetValue(itemId, out int value) ? value : 0;
    }

    public List<KeyValuePair<string, int>> GetOrderedItems()
    {
        List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();

        for (int i = 0; i < itemOrder.Count; i++)
        {
            string id = itemOrder[i];

            if (items.TryGetValue(id, out int amount) && amount > 0)
                result.Add(new KeyValuePair<string, int>(id, amount));
        }

        return result;
    }

    public void ClearAll()
    {
        items.Clear();
        itemOrder.Clear();
        OnInventoryChanged?.Invoke();
    }

    [ContextMenu("Debug Print Inventory")]
    public void DebugPrintInventory()
    {
        if (items.Count == 0)
        {
            Debug.Log("[FarmInventory] Inventory rỗng.");
            return;
        }

        foreach (var kv in items)
        {
            Debug.Log($"[FarmInventory] Item: {kv.Key} | Amount: {kv.Value}");
        }
    }
    // kiểm tra lúa , trừ lúa khi cho bò ăn
    public bool HasItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        return items.TryGetValue(itemId, out int value) && value >= amount;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!items.TryGetValue(itemId, out int current))
            return false;

        if (current < amount)
            return false;

        current -= amount;

        if (current <= 0)
        {
            items.Remove(itemId);
            itemOrder.Remove(itemId);
        }
        else
        {
            items[itemId] = current;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }
}