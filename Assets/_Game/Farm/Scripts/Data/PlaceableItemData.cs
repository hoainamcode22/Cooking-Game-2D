using UnityEngine;

/// <summary>
/// Base cho các vật phẩm xây dựng và trang trí trong Shop.
/// Kế thừa BaseItemData nên tương thích hoàn toàn với ShopItemUI và ShopManager.
/// </summary>
public class PlaceableItemData : BaseItemData
{
    [Header("Placement")]
    public GameObject prefabToBuild;

    [Header("Unlock (Demo L1-L10)")]
    [Tooltip("Level người chơi cần đạt để mua công trình/trang trí này. ShopLevelLockUI đọc field này để khoá item trong shop.")]
    public int unlockLevel = 1;
}
