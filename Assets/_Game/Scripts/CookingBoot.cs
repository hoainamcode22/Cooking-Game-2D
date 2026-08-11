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

    // Khuôn thẻ đã chốt cho từng cột. Chốt MỘT LẦN ở lần nạp đầu rồi giữ luôn:
    // sau khi `ApplyToCardGroup` chạy, thẻ con đầu tiên trong container có thể đang
    // bị SetActive(false) (dư slot) — lấy lại nó làm khuôn ở lần refresh sau thì
    // `Instantiate` ra thẻ TẮT, thẻ mới không bao giờ hiện lên.
    private SelectableIngredientCard cachedIngredientTemplate;
    private SelectableIngredientCard cachedSeasoningTemplate;

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

            if (item.cookingData == null)
            {
                Debug.LogWarning(
                    $"[CookingBoot] '{item.itemId}' nằm trong cookingInventoryItems nhưng " +
                    $"cookingData để trống → sẽ KHÔNG bao giờ hiện được trong bếp. " +
                    $"Gán IngredientData cho asset '{item.name}', hoặc bỏ nó khỏi danh sách này.");
                continue;
            }

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
                    continue;

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

        SelectableIngredientCard template = ResolveTemplate(cards, isSeasoning);

        for (int i = 0; i < items.Count; i++)
        {
            SelectableIngredientCard card;

            if (i < cards.Count)
            {
                card = cards[i];
            }
            else
            {
                if (template == null)
                {
                    // Không có khuôn thì không thể sinh thêm. Báo rõ chứ không im lặng
                    // như bản cũ — im lặng chính là lý do lỗi này sống sót lâu vậy.
                    Debug.LogError(
                        $"[CookingBoot] Cột {(isSeasoning ? "gia vị" : "nguyên liệu")} cần " +
                        $"{items.Count} ô nhưng chỉ có {cards.Count} và KHÔNG có khuôn thẻ để sinh thêm. " +
                        $"Gán '{(isSeasoning ? "seasoningCardPrefab" : "ingredientCardPrefab")}' " +
                        $"trong LeftPanelRefs, hoặc để lại ít nhất một thẻ mẫu trong container.");
                    break;
                }

                card = Instantiate(template, contentRoot, false);
                card.name = $"{template.name}_Auto_{i}";

                RectTransform rt = card.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            SetupCard(card, items[i].itemData, items[i].quantity, isSeasoning);
            card.gameObject.SetActive(true);
        }

        // Ô dư: tắt, KHÔNG Destroy. Giữ lại để lần sau người chơi gửi nhiều hàng hơn
        // thì tái dùng ngay, khỏi cấp phát lại giữa lúc đang chơi.
        for (int i = items.Count; i < cards.Count; i++)
            cards[i].gameObject.SetActive(false);
    }


    private SelectableIngredientCard ResolveTemplate(List<SelectableIngredientCard> existingCards, bool isSeasoning)
    {
        SelectableIngredientCard fromInspector = isSeasoning
            ? leftRefs.seasoningCardPrefab
            : leftRefs.ingredientCardPrefab;

        if (fromInspector != null)
            return fromInspector;

        SelectableIngredientCard cached = isSeasoning ? cachedSeasoningTemplate : cachedIngredientTemplate;
        if (cached != null)
            return cached;

        if (existingCards.Count == 0)
            return null;

        cached = existingCards[0];

        if (isSeasoning) cachedSeasoningTemplate = cached;
        else cachedIngredientTemplate = cached;

        return cached;
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
