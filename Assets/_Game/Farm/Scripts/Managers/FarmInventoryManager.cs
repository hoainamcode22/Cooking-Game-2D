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
        public List<InventoryEntry> entries   = new List<InventoryEntry>();
        public List<string>         itemOrder = new List<string>();
    }

    public static FarmInventoryManager Instance { get; private set; }

    private const string SaveKey = "FARM_INVENTORY_SAVE";

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
        if (pauseStatus) SaveInventory();
    }

    private void OnApplicationQuit() => SaveInventory();

    // ── Key Normalization ─────────────────────────────────────────────────────
    // BUG-FIX (key mismatch): all keys are stored and queried lowercase + trimmed.
    // This makes itemId in OrderItemDefinition case-insensitive vs. keys added by harvesting.
    private static string NormalizeKey(string key) =>
        key?.Trim().ToLower() ?? string.Empty;

    // ── Public API ────────────────────────────────────────────────────────────

    public void AddItem(string itemId, int amount)
    {
        string key = NormalizeKey(itemId);
        if (string.IsNullOrEmpty(key) || amount <= 0) return;

        bool isNew = !items.ContainsKey(key);
        if (isNew)
        {
            items[key] = 0;
            itemOrder.Add(key);
        }

        items[key] += amount;

        SaveInventory();
        OnInventoryChanged?.Invoke();
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
        var data = new InventorySaveData();
        foreach (string id in itemOrder)
        {
            if (!items.TryGetValue(id, out int amount) || amount <= 0) continue;
            data.entries.Add(new InventoryEntry { itemId = id, amount = amount });
            data.itemOrder.Add(id);
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
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
    }
}
