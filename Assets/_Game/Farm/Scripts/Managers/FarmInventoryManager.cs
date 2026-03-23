using System.Collections.Generic;
using UnityEngine;

public class FarmInventoryManager : MonoBehaviour
{
    public static FarmInventoryManager Instance { get; private set; }

    private readonly Dictionary<string, int> items = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        if (!items.ContainsKey(itemId))
            items[itemId] = 0;

        items[itemId] += amount;

        Debug.Log($"Inventory Add: {itemId} = {items[itemId]}");
    }

    public int GetAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        return items.TryGetValue(itemId, out int value) ? value : 0;
    }

    public Dictionary<string, int> GetAllItems()
    {
        return new Dictionary<string, int>(items);
    }

    [ContextMenu("Debug Print Inventory")]
    public void DebugPrintInventory()
    {
        if (items.Count == 0)
        {
            Debug.Log("Inventory rỗng.");
            return;
        }

        foreach (var kv in items)
        {
            Debug.Log($"Item: {kv.Key} | Amount: {kv.Value}");
        }
    }
}