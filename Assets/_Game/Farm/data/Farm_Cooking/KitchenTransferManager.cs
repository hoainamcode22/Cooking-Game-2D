using System;
using System.Collections.Generic;
using UnityEngine;

public class KitchenTransferManager : MonoBehaviour
{
    public static KitchenTransferManager Instance { get; private set; }

    private readonly Dictionary<string, int> transferredItems = new Dictionary<string, int>();

    public Action OnTransferredItemsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddTransferredItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        if (!transferredItems.ContainsKey(itemId))
            transferredItems[itemId] = 0;

        transferredItems[itemId] += amount;
        OnTransferredItemsChanged?.Invoke();
    }

    public int GetTransferredAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        return transferredItems.TryGetValue(itemId, out int value) ? value : 0;
    }

    public List<KeyValuePair<string, int>> GetTransferredItems()
    {
        List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();

        foreach (var kv in transferredItems)
        {
            if (kv.Value > 0)
                result.Add(kv);
        }

        return result;
    }

    public void ClearTransferredItems()
    {
        transferredItems.Clear();
        OnTransferredItemsChanged?.Invoke();
    }

    public bool HasTransferredItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        return transferredItems.TryGetValue(itemId, out int value) && value >= amount;
    }

    public bool RemoveTransferredItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!transferredItems.TryGetValue(itemId, out int current))
            return false;

        if (current < amount)
            return false;

        current -= amount;

        if (current <= 0)
            transferredItems.Remove(itemId);
        else
            transferredItems[itemId] = current;

        OnTransferredItemsChanged?.Invoke();
        return true;
    }
}