using UnityEngine;

/// <summary>
/// Base cho các vật phẩm xây dựng và trang trí trong Shop.
/// Kế thừa BaseItemData nên tương thích hoàn toàn với ShopItemUI và ShopManager.
/// </summary>
public class PlaceableItemData : BaseItemData
{
    [Header("Placement")]
    public GameObject prefabToBuild;
}
