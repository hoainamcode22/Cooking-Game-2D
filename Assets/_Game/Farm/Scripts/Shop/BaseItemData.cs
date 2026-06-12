using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Class gốc cho tất cả item trong Shop.
/// CropData, FlowerData kế thừa class này để ShopItemUI có thể xử lý chung.
/// </summary>
public class BaseItemData : ScriptableObject
{
    [Header("Định danh Kho")]
    // Mã định danh dùng để lưu vào WarehouseManager.AddItem()
    // Với CropData: điền bằng giá trị seedItemId (ví dụ: "lua", "bap"...)
    public string itemID;

    [Header("Thông tin hiển thị")]
    // [FormerlySerializedAs] giúp Unity tự đọc dữ liệu cũ từ field tên "displayName"
    // → CropData assets đã có displayName sẽ tự migrate vào đây, KHÔNG mất data
    [FormerlySerializedAs("displayName")]
    public string itemName;

    // Tương tự: data cũ tên "icon" tự migrate vào itemIcon
    [FormerlySerializedAs("icon")]
    public Sprite itemIcon;

    [Header("Giá cả")]
    // Data cũ "price" (shop items) và "seedBuyGold" (CropData) đều migrate vào goldPrice
    [FormerlySerializedAs("price")]
    [FormerlySerializedAs("seedBuyGold")]
    public int goldPrice;

    // Giá Kim Cương — 0 nghĩa là item này không bán bằng kim cương
    public int diamondPrice;

}
