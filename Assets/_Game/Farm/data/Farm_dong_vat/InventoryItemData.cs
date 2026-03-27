using UnityEngine;

[CreateAssetMenu(fileName = "Item_", menuName = "Farm/Inventory Item Data")]
public class InventoryItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string displayName;
    public Sprite icon;

    [Header("Cooking Link")]
    public IngredientData cookingData;
}