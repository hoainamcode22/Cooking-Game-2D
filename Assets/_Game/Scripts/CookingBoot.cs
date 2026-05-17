using System.Collections;
using System.Collections.Generic;


// using System.Diagnostics;
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
        {
            yield break;
        }

        BuildInventoryLookup();
        FillOldCardsFromTransferredItems();

        selection.RegisterAllLeftCards(
            leftRefs.ingredientsContent,
            leftRefs.seasoningsContent
        );
    }

    private void BuildInventoryLookup()// xây dựng lại lookup từ list, để dễ tìm kiếm khi cần thiết
    {
        inventoryLookup.Clear();

        for (int i = 0; i < cookingInventoryItems.Count; i++)
        {
            InventoryItemData item = cookingInventoryItems[i];
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            if (!inventoryLookup.ContainsKey(item.itemId))// không nên có trùng id, nhưng nếu có thì lấy cái đầu tiên, bỏ qua các cái sau
                inventoryLookup.Add(item.itemId, item);
        }
    }

    private void FillOldCardsFromTransferredItems()// điền dữ liệu cho các card đã có sẵn trong scene, dựa trên những item đã được chuyển từ scene trước (nếu có)
    {
        List<KeyValuePair<InventoryItemData, int>> ingredientItems = new List<KeyValuePair<InventoryItemData, int>>();
        List<KeyValuePair<InventoryItemData, int>> seasoningItems = new List<KeyValuePair<InventoryItemData, int>>();
        Debug.Log("Lấy dữ liệu đã chuyển từ KitchenTransferManager...");
        //IngredientAmountUI amountUI = card.GetComponent<IngredientAmountUI>();

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
                    seasoningItems.Add(new KeyValuePair<InventoryItemData, int>(inventoryItem, 1));// hiện tại chưa có trường amount riêng cho gia vị, nên tạm thời cứ cho amount = 1, và sau này nếu có thay đổi thì sẽ điều chỉnh sau. Còn nguyên liệu thì chắc chắn sẽ có amount, nên mới để là int ở đây
                else
                    ingredientItems.Add(new KeyValuePair<InventoryItemData, int>(inventoryItem, kv.Value));

                Debug.Log($"Transferred item: {inventoryItem.displayName} x{kv.Value}");
            }
        }

        ApplyToCardGroup(leftRefs.ingredientsContent, ingredientItems, false);
        ApplyToCardGroup(leftRefs.seasoningsContent, seasoningItems, true);
    }

    private void ApplyToCardGroup(Transform contentRoot, List<KeyValuePair<InventoryItemData, int>> items, bool isSeasoning)// áp dụng dữ liệu item vào các card con của contentRoot, dựa trên loại (gia vị hay nguyên liệu)
    {
        if (contentRoot == null)
            return;

        List<SelectableIngredientCard> cards = new List<SelectableIngredientCard>();

        foreach (Transform child in contentRoot)
        {
            SelectableIngredientCard card = child.GetComponent<SelectableIngredientCard>();// chỉ lấy những child có component SelectableIngredientCard, bỏ qua những cái không phải card
            if (card != null)
                cards.Add(card);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < items.Count)// nếu còn item để điền, thì điền vào card, và bật card lên. Nếu không còn item nào để điền, thì tắt card đi (ẩn khỏi UI)
            {
                SetupCard(cards[i], items[i].Key, items[i].Value, isSeasoning);;// điền dữ liệu item vào card, và khởi tạo card với selection manager, để card có thể tương tác được
                cards[i].gameObject.SetActive(true);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        if (items.Count > cards.Count)
        {
            Debug.Log($"[CookingBoot] Không đủ slot {(isSeasoning ? "gia vị" : "nguyên liệu")} để hiển thị. Dư: {items.Count - cards.Count}");
        }
    }

    private void SetupCard(SelectableIngredientCard card, InventoryItemData inventoryItem, int amount, bool isSeasoning)// điền dữ liệu item vào card, và khởi tạo card với selection manager, để card có thể tương tác được
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

        card.SetInventoryItem(inventoryItem);
        card.SetIngredientData(ing);
        card.Init(selection, isSeasoning);
        card.SetSelected(false);
        var amountUI = card.GetComponent<IngredientAmountUI>();
        if (amountUI != null)
        {
            amountUI.SetAmount(amount);
        }
    }
}