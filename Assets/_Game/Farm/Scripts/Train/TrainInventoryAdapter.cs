using UnityEngine;

/// <summary>
/// Adapter kết nối hệ thống tàu với FarmInventoryManager.
/// Thay đổi class này nếu muốn dùng inventory khác.
/// </summary>
public static class TrainInventoryAdapter
{
    /// <summary>Kiểm tra kho có đủ số lượng không.</summary>
    public static bool HasItem(string itemId, int amount)
    {
        if (FarmInventoryManager.Instance == null)
        {
            Debug.LogWarning("[TrainInventory] FarmInventoryManager.Instance == null");
            return false;
        }
        return FarmInventoryManager.Instance.HasItem(itemId, amount);
    }

    /// <summary>Trừ vật phẩm khỏi kho. Trả về false nếu không đủ.</summary>
    public static bool RemoveItem(string itemId, int amount)
    {
        if (FarmInventoryManager.Instance == null) return false;
        return FarmInventoryManager.Instance.RemoveItem(itemId, amount);
    }

    /// <summary>Thêm vật phẩm vào kho.</summary>
    public static void AddItem(string itemId, string displayName, Sprite icon, int amount)
    {
        if (FarmInventoryManager.Instance == null) return;
        FarmInventoryManager.Instance.AddItem(itemId, amount);
    }
}
