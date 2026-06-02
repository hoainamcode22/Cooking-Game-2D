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
        
        List<CookingTransferredItem> ingredientItems = new List<CookingTransferredItem>();
        List<CookingTransferredItem> seasoningItems = new List<CookingTransferredItem>();
        

        if (KitchenTransferManager.Instance != null)
        {
            List<KeyValuePair<string, int>> transferred = KitchenTransferManager.Instance.GetTransferredItems();

            foreach (var kv in transferred)
            {
                
                if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData inventoryItem))
                    continue;

                if (inventoryItem == null || inventoryItem.cookingData == null)
                    continue;

                int quantity = kv.Value;
                    if (quantity <= 0)
                    {
                        continue;
                    }

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

            ui.Setup(displayName, mainIcon, topIcon, false);
        }

        card.SetIngredientData(ing);// Thiết lập IngredientData cho card
        card.Init(selection, isSeasoning);// Khởi tạo card với manager và loại (gia vị hay nguyên liệu)
        card.SetSelected(false);// Đảm bảo card không bị chọn khi khởi tạo lại
        card.setIdItem(inventoryItem.itemId);// Thiết lập ID item để có thể đối chiếu sau này
        card.SetQuantityFromKitchen(quantity);// Hiển thị số lượng từ KitchenTransferManager lên card
    }
    public void RefreshTransferredItemCards()
    {
        if (selection == null || leftRefs == null)
            return;

        BuildInventoryLookup();
        FillOldCardsFromTransferredItems();

        selection.RegisterAllLeftCards(
            leftRefs.ingredientsContent,
            leftRefs.seasoningsContent
        );

    }
}
