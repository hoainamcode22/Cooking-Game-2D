using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmInventoryManager : MonoBehaviour
{
    [Serializable]
    private class InventoryEntry
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    private class InventorySaveData
    {
        public List<InventoryEntry> entries = new List<InventoryEntry>();
        public List<string> itemOrder = new List<string>();
    }

    public static FarmInventoryManager Instance { get; private set; }

    private const string SaveKey = "FARM_INVENTORY_SAVE";

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
        DontDestroyOnLoad(gameObject);

        LoadInventory();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveInventory();
    }

    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        bool isNewItem = !items.ContainsKey(itemId);

        if (isNewItem)
        {
            items[itemId] = 0;
            itemOrder.Add(itemId);
        }

        items[itemId] += amount;

        SaveInventory();
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
        SaveInventory();
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

        SaveInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    private void SaveInventory()
    {
        InventorySaveData data = new InventorySaveData();

        for (int i = 0; i < itemOrder.Count; i++)
        {
            string id = itemOrder[i];
            if (!items.TryGetValue(id, out int amount))
                continue;

            if (amount <= 0)
                continue;

            data.entries.Add(new InventoryEntry
            {
                itemId = id,
                amount = amount
            });

            data.itemOrder.Add(id);
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        items.Clear();
        itemOrder.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
            return;

        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data == null || data.entries == null)
            return;

        for (int i = 0; i < data.entries.Count; i++)
        {
            InventoryEntry entry = data.entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.amount <= 0)
                continue;

            items[entry.itemId] = entry.amount;

            if (!itemOrder.Contains(entry.itemId))
                itemOrder.Add(entry.itemId);
        }

        if (data.itemOrder != null)
        {
            List<string> reordered = new List<string>();

            for (int i = 0; i < data.itemOrder.Count; i++)
            {
                string id = data.itemOrder[i];
                if (items.ContainsKey(id) && !reordered.Contains(id))
                    reordered.Add(id);
            }

            foreach (var kv in items)
            {
                if (!reordered.Contains(kv.Key))
                    reordered.Add(kv.Key);
            }

            itemOrder.Clear();
            itemOrder.AddRange(reordered);
        }
    }
}