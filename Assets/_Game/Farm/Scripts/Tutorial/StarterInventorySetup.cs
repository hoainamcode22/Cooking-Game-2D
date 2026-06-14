using System;
using System.Collections.Generic;
using UnityEngine;


public class StarterInventorySetup : MonoBehaviour
{
    private const string PREF_KEY = "STARTER_ITEMS_GIVEN";

    [Serializable]
    public class StarterItemEntry
    {
        [Tooltip("ItemId trong WarehouseManager (ví dụ: seed_rice, seed_huong_duong)")]
        public string itemId;

        [Tooltip("Tên hiển thị trong kho")]
        public string displayName;

        [Tooltip("Icon hạt giống — kéo sprite từ CropData vào đây")]
        public Sprite icon;

        [Tooltip("Số lượng ban đầu")]
        public int amount = 10;
    }

    [Header("Starter Items (chỉ cấp 1 lần cho save mới)")]
    [SerializeField] private List<StarterItemEntry> starterItems = new List<StarterItemEntry>();

    [Header("Debug")]
    [SerializeField] private bool forceResetOnPlay = false;

    private void Start()
    {
#if UNITY_EDITOR
        if (forceResetOnPlay)
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            Debug.Log("[StarterInventory] DEBUG: Reset starter flag");
        }
#endif

        GiveStarterItems();
    }

    private void GiveStarterItems()
    {
        if (WarehouseManager.Instance == null)
        {
            Debug.LogWarning("[StarterInventory] WarehouseManager.Instance chua san sang. Se thu lai sau.");
            Invoke(nameof(GiveStarterItems), 0.5f);
            return;
        }

        if (FarmLevelManager.Instance != null && FarmLevelManager.Instance.CurrentLevel > 1)
            return;

        int given = 0;
        foreach (var entry in starterItems)
        {
            if (string.IsNullOrEmpty(entry.itemId)) continue;
            int current = WarehouseManager.Instance.GetAmount(entry.itemId);
            int missing = Mathf.Max(0, entry.amount - current);
            if (missing <= 0) continue;

            WarehouseManager.Instance.AddItem(entry.itemId, entry.displayName, entry.icon, missing);
            Debug.Log($"[StarterInventory] +{missing}x {entry.displayName} ({entry.itemId})");
            given++;
        }

        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log($"[StarterInventory] Da cap {given} loai vat pham starter. Flag da set.");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Reset Starter Flag (cho test)")]
    private void DebugReset()
    {
        PlayerPrefs.DeleteKey(PREF_KEY);
        Debug.Log("[StarterInventory] Flag da reset — lan chay toi se cap lai starter items.");
    }
#endif
}
