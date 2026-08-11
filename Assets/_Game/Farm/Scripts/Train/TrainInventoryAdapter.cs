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

    /// <summary>Kho còn nhận được vật phẩm này không (F8 — sức chứa đã enforce thật).</summary>
    public static bool CanAddItem(string itemId)
    {
        if (FarmInventoryManager.Instance == null) return false;
        return FarmInventoryManager.Instance.CanAddItem(itemId);
    }

    /// <summary>
    /// Thêm vật phẩm vào kho. Trả FALSE khi kho hết slot — người gọi PHẢI xử lý,
    /// nếu không thì thưởng tàu bốc hơi mà toa đã đánh dấu "đã thu".
    ///
    /// VỀ `displayName` / `icon` (F5 — "TrainInventoryAdapter vứt icon đi"):
    /// `FarmInventoryManager` cố ý CHỈ lưu (itemId, amount) — nó là kho, không phải
    /// bảng hiển thị. Tên và icon của 5 vật liệu tàu (da/go/dinh/son/kinh) được
    /// `WarehousePopupUI.extraItemDatabase` tra ra từ 5 asset `InventoryItemData` trong
    /// `Assets/_Game/Farm/data/item_taulua` — cả 5 asset đó ĐÃ nằm trong danh sách đó.
    /// Nên hai tham số này không bị "vứt đi" một cách vô tình: chúng dùng cho FX bay
    /// (TrainManager.SpawnItemFlyFX) và được giữ trong chữ ký để nơi gọi không phải tra
    /// lại asset. Chỉ ghi cảnh báo khi itemId lạ, vì đó mới là trường hợp icon THẬT SỰ
    /// biến mất (kho có hàng mà popup vẽ ô trắng).
    /// </summary>
    public static bool AddItem(string itemId, string displayName, Sprite icon, int amount)
    {
        if (FarmInventoryManager.Instance == null) return false;

        if (icon == null)
            Debug.LogWarning($"[Train] Thưởng '{itemId}' ({displayName}) không có icon — " +
                             $"kiểm tra asset InventoryItemData tương ứng và extraItemDatabase của kho.");

        return FarmInventoryManager.Instance.AddItem(itemId, amount);
    }
}
