using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CookingTransferredItem
{
    public InventoryItemData itemData;
    public int quantity;

    public CookingTransferredItem(InventoryItemData itemData, int quantity)
    {
        this.itemData = itemData;
        this.quantity = quantity;
    }
}
public class CookingBoot : MonoBehaviour
{
    [Header("Test Mode")]
    public bool useTestData = true;

    [Header("Refs")]
    public CookingSelectionManager selection;
    public LeftPanelRefs leftRefs;

    [Header("Cooking Item Database")]
    public List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

    private readonly Dictionary<string, InventoryItemData> inventoryLookup = new Dictionary<string, InventoryItemData>();

    private IEnumerator Start()
    {
        yield return null;

        if (selection == null || leftRefs == null)
            yield break;

        if (!useTestData)
        {
            BuildInventoryLookup();
            FillOldCardsFromTransferredItems();
        }
        else
        {
            Debug.Log("[CookingBoot] Test Mode bật: không lọc theo kho, giữ toàn bộ card đã spawn.");
        }

        selection.RegisterAllLeftCards(
            leftRefs.ingredientsContent,
            leftRefs.seasoningsContent
        );
    }

    private void BuildInventoryLookup()
    {
        inventoryLookup.Clear();

        for (int i = 0; i < cookingInventoryItems.Count; i++)
        {
            InventoryItemData item = cookingInventoryItems[i];
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            if (!inventoryLookup.ContainsKey(item.itemId))
                inventoryLookup.Add(item.itemId, item);
        }
    }

    private void FillOldCardsFromTransferredItems()
    {
        Debug.Log("[CookingBoot] Điền dữ liệu từ KitchenTransferManager vào card cũ.");
        
        List<CookingTransferredItem> ingredientItems = new List<CookingTransferredItem>();
        List<CookingTransferredItem> seasoningItems = new List<CookingTransferredItem>();
        

        if (KitchenTransferManager.Instance != null)
        {
            List<KeyValuePair<string, int>> transferred = KitchenTransferManager.Instance.GetTransferredItems();

            foreach (var kv in transferred)
            {
                Debug.Log($"[CookingBoot] Transferred Item: {kv.Key} x{kv.Value}");
                
                if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData inventoryItem))
                    continue;

                if (inventoryItem == null || inventoryItem.cookingData == null)
                    continue;

                int quantity = kv.Value;

                CookingTransferredItem transferredItem = new CookingTransferredItem(inventoryItem, quantity);

                if (inventoryItem.cookingData.kind == IngredientKind.Seasoning)
                    seasoningItems.Add(transferredItem);
                else
                    ingredientItems.Add(transferredItem);
            }
        }

        ApplyToCardGroup(leftRefs.ingredientsContent, ingredientItems, false);
        ApplyToCardGroup(leftRefs.seasoningsContent, seasoningItems, true);
    }

    private void ApplyToCardGroup(Transform contentRoot, List<CookingTransferredItem> items, bool isSeasoning)
    {
        if (contentRoot == null)
            return;

        List<SelectableIngredientCard> cards = new List<SelectableIngredientCard>();

        foreach (Transform child in contentRoot)
        {
            SelectableIngredientCard card = child.GetComponent<SelectableIngredientCard>();
            if (card != null)
                cards.Add(card);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < items.Count)
            {
                SetupCard(cards[i], items[i].itemData, items[i].quantity, isSeasoning);
                cards[i].gameObject.SetActive(true);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        if (items.Count > cards.Count)
        {
            Debug.LogWarning($"[CookingBoot] Không đủ slot {(isSeasoning ? "gia vị" : "nguyên liệu")} để hiển thị. Dư: {items.Count - cards.Count}");
        }
    }

    private void SetupCard(SelectableIngredientCard card, InventoryItemData inventoryItem, int quantity, bool isSeasoning)
    {
        if (card == null || inventoryItem == null || inventoryItem.cookingData == null)
            return;

        IngredientData ing = inventoryItem.cookingData;

        IngredientItemUI ui = card.GetComponent<IngredientItemUI>();
        if (ui != null)
        {
            string displayName = !string.IsNullOrEmpty(inventoryItem.displayName)
                ? inventoryItem.displayName
                : ing.displayName;

            Sprite mainIcon = inventoryItem.icon != null ? inventoryItem.icon : ing.icon;
            Sprite topIcon = null;
            int stars = ing.stars;

            ui.Setup(displayName, mainIcon, topIcon, stars, false);
        }

        card.SetIngredientData(ing);
        card.Init(selection, isSeasoning);
        card.SetSelected(false);
        card.setIdItem(inventoryItem.itemId);
        card.SetQuantityFromKitchen(quantity);
    }
}