using System;
using System.Collections.Generic;
using UnityEngine;

public class KitchenTransferManager : MonoBehaviour
{
    [Serializable]
    private class TransferEntry
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    private class TransferSaveData
    {
        public List<TransferEntry> entries = new List<TransferEntry>();
    }

    public static KitchenTransferManager Instance { get; private set; }

    private const string SaveKey = "KITCHEN_TRANSFER_SAVE";

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
        transform.SetParent(null); // Tách ra root để DontDestroyOnLoad hoạt động (fix warning)
        DontDestroyOnLoad(gameObject);

        LoadTransferData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveTransferData();
    }

    private void OnApplicationQuit()
    {
        SaveTransferData();
    }

    public void AddTransferredItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        if (!transferredItems.ContainsKey(itemId))
            transferredItems[itemId] = 0;

        transferredItems[itemId] += amount;
        SaveTransferData();
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
        SaveTransferData();
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

        SaveTransferData();
        OnTransferredItemsChanged?.Invoke();
        return true;
    }

    private void SaveTransferData()
    {
        TransferSaveData data = new TransferSaveData();

        foreach (var kv in transferredItems)
        {
            if (kv.Value <= 0)
                continue;

            data.entries.Add(new TransferEntry
            {
                itemId = kv.Key,
                amount = kv.Value
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadTransferData()
    {
        transferredItems.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
            return;

        TransferSaveData data = JsonUtility.FromJson<TransferSaveData>(json);
        if (data == null || data.entries == null)
            return;

        for (int i = 0; i < data.entries.Count; i++)
        {
            TransferEntry entry = data.entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.amount <= 0)
                continue;

            transferredItems[entry.itemId] = entry.amount;
        }
    }
    //File nÃ y cá»§a NguyÃªn thÃªm vÃ o
    public void SetAfterCooking(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (!transferredItems.ContainsKey(itemId))
        {
            return;
        }

        transferredItems[itemId] -= 1;


        if (transferredItems[itemId] <= 0)
        {
            transferredItems.Remove(itemId);
        }

        SaveTransferData();
        OnTransferredItemsChanged?.Invoke();
    }

    public void SetAfterCooking(List<string> selectedItemIds)
    {
        if (selectedItemIds == null || selectedItemIds.Count == 0)
            return;

        for (int i = 0; i < selectedItemIds.Count; i++)
        {
            SetAfterCooking(selectedItemIds[i]);
        }
    }
}
