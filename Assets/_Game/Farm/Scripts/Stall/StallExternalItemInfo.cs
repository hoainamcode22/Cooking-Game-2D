using UnityEngine;

/// <summary>
/// Mô tả một mặt hàng của KHO NGOÀI (xem <see cref="IStallExternalStore"/>) để quầy hàng hiện được
/// icon + tên + danh mục và tra được giá gốc mà không cần biết kiểu dữ liệu của module sở hữu kho.
/// <see cref="StallItemCatalog"/> đọc struct này để dựng dòng tra cứu; <see cref="BasePriceBook"/>
/// đọc <see cref="baseSellGold"/> làm bậc dự phòng khi các nguồn giá farm không biết mặt hàng.
/// </summary>
public struct StallExternalItemInfo
{
    /// <summary>Icon hiển thị. Null → catalog/UI tự fallback (ô màu), không lỗi.</summary>
    public Sprite icon;

    /// <summary>Tên tiếng Việt hiện trên lưới chọn, ô quầy, hint bán được.</summary>
    public string displayName;

    /// <summary>Tab danh mục ở panel chọn vật phẩm.</summary>
    public StallItemCategory category;

    /// <summary>Giá gốc 1 đơn vị (&gt; 0). 0 = kho không biết giá → quầy rơi về bảng dự phòng.</summary>
    public int baseSellGold;
}
