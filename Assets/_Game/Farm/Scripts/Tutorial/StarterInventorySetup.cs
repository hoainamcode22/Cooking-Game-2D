using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cấp vật phẩm starter cho người chơi mới — chạy đúng 1 lần duy nhất.
///
/// Setup:
///   1. Attach component này lên Tutorial_Manager hoặc một GameObject tồn tại lâu dài.
///   2. Thêm các mục vào starterItems (itemId, displayName, sprite, amount).
///   3. Chạy Tools/Farm Game/Setup Tutorial L1-L2 để tự cấu hình mặc định.
///
/// Logic:
///   - Kiểm tra PlayerPrefs key "STARTER_ITEMS_GIVEN"
///   - Nếu chưa có: gọi WarehouseManager.AddItem cho từng entry, set flag
///   - Nếu đã có: bỏ qua (không cấp lại)
/// </summary>
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

        if (PlayerPrefs.HasKey(PREF_KEY)) return;

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

        int given = 0;
        foreach (var entry in starterItems)
        {
            if (string.IsNullOrEmpty(entry.itemId)) continue;
            WarehouseManager.Instance.AddItem(entry.itemId, entry.displayName, entry.icon, entry.amount);
            Debug.Log($"[StarterInventory] +{entry.amount}x {entry.displayName} ({entry.itemId})");
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
