using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingBoot : MonoBehaviour
{
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

        BuildInventoryLookup();
        FillOldCardsFromTransferredItems();

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
        List<InventoryItemData> ingredientItems = new List<InventoryItemData>();
        List<InventoryItemData> seasoningItems = new List<InventoryItemData>();

        if (KitchenTransferManager.Instance != null)
        {
            List<KeyValuePair<string, int>> transferred = KitchenTransferManager.Instance.GetTransferredItems();

            foreach (var kv in transferred)
            {
                if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData inventoryItem))
                    continue;

                if (inventoryItem == null || inventoryItem.cookingData == null)
                    continue;

                if (inventoryItem.cookingData.kind == IngredientKind.Seasoning)
                    seasoningItems.Add(inventoryItem);
                else
                    ingredientItems.Add(inventoryItem);
            }
        }

        ApplyToCardGroup(leftRefs.ingredientsContent, ingredientItems, false);
        ApplyToCardGroup(leftRefs.seasoningsContent, seasoningItems, true);
    }

    private void ApplyToCardGroup(Transform contentRoot, List<InventoryItemData> items, bool isSeasoning)
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
                SetupCard(cards[i], items[i], isSeasoning);
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

    private void SetupCard(SelectableIngredientCard card, InventoryItemData inventoryItem, bool isSeasoning)
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
    }
}