using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CookingSelectionManager : MonoBehaviour
{
    [Header("Limits")]
    public int maxIngredients = 4;
    public int maxSeasonings = 3;

    [Header("Left UI (counts)")]
    public TMP_Text ingredientsCountText;
    public TMP_Text seasoningsCountText;

    [Header("Old Pot Containers")]
    public Transform potIngredientsContent;
    public Transform potSeasoningsContent;

    [Header("Old Pot Card Prefab (mini)")]
    public IngredientItemUI potCardPrefab;

    [Header("New Left Panels")]
    public Transform leftIngredientsContent;
    public Transform leftSeasoningsContent;

    [Header("New Pot Panels")]
    public Transform newPotIngredientsContent;
    public Transform newPotSeasoningsContent;

    [Header("New Slot Prefab")]
    public CookingStackSlotUI stackSlotPrefab;

    [Header("Cooking Item Database")]
    public List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

    private readonly List<SelectableIngredientCard> selectedIngredients = new();
    private readonly List<SelectableIngredientCard> selectedSeasonings = new();

    private readonly Dictionary<string, InventoryItemData> inventoryLookup = new();
    private readonly Dictionary<string, int> leftIngredientAmounts = new();
    private readonly Dictionary<string, int> leftSeasoningAmounts = new();
    private readonly Dictionary<string, int> potIngredientAmounts = new();
    private readonly Dictionary<string, int> potSeasoningAmounts = new();

    public void RegisterAllLeftCards(Transform ingredientsContent, Transform seasoningsContent)
    {
        selectedIngredients.Clear();
        selectedSeasonings.Clear();

        foreach (Transform t in ingredientsContent)
        {
            if (!t.gameObject.activeSelf) continue;

            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, false);
        }

        foreach (Transform t in seasoningsContent)
        {
            if (!t.gameObject.activeSelf) continue;

            var card = t.GetComponent<SelectableIngredientCard>();
            if (card != null)
                card.Init(this, true);
        }

        RebuildPot();
        UpdateCounts();
    }

    public void TrySelect(SelectableIngredientCard card)
    {
        if (card == null) return;

        if (card.isSeasoning)
        {
            if (selectedSeasonings.Contains(card)) return;
            if (selectedSeasonings.Count >= maxSeasonings)
            {
                Debug.Log("Đã đạt tối đa gia vị.");
                return;
            }

            selectedSeasonings.Add(card);
            Debug.Log("Đã thêm gia vị: " + card.GetItemName());
        }
        else
        {
            if (selectedIngredients.Contains(card)) return;
            if (selectedIngredients.Count >= maxIngredients)
            {
                Debug.Log("Đã đạt tối đa nguyên liệu.");
                return;
            }

            selectedIngredients.Add(card);
            Debug.Log("Đã thêm nguyên liệu: " + card.GetItemName());
        }

        card.SetSelected(true);
        RebuildPot();
        UpdateCounts();
    }

    public void TryDeselect(SelectableIngredientCard card)
    {
        if (card == null) return;

        if (card.isSeasoning)
        {
            selectedSeasonings.Remove(card);
            Debug.Log("Đã bỏ gia vị: " + card.GetItemName());
        }
        else
        {
            selectedIngredients.Remove(card);
            Debug.Log("Đã bỏ nguyên liệu: " + card.GetItemName());
        }

        card.SetSelected(false);
        RebuildPot();
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        int ingredientCount = selectedIngredients.Count;
        int seasoningCount = selectedSeasonings.Count;

        if (ingredientsCountText != null)
            ingredientsCountText.text = $"Chọn {ingredientCount}/{maxIngredients}";

        if (seasoningsCountText != null)
            seasoningsCountText.text = $"Chọn {seasoningCount}/{maxSeasonings}";
    }

    private void RebuildPot()
    {
        ClearChildren(potIngredientsContent);
        ClearChildren(potSeasoningsContent);

        foreach (var c in selectedIngredients)
            SpawnPotCard(potIngredientsContent, c);

        foreach (var c in selectedSeasonings)
            SpawnPotCard(potSeasoningsContent, c);
    }

    private void SpawnPotCard(Transform parent, SelectableIngredientCard fromCard)
    {
        if (potCardPrefab == null || parent == null || fromCard == null) return;

        var newUi = Instantiate(potCardPrefab, parent, false);
        newUi.gameObject.SetActive(true);

        RectTransform rt = newUi.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        newUi.Setup(
            fromCard.GetItemName(),
            fromCard.GetMainSprite(),
            fromCard.GetTopSprite(),
            3,
            true
        );
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    public void ResetSelection()
    {
        foreach (var card in selectedIngredients)
        {
            if (card != null)
                card.SetSelected(false);
        }

        foreach (var card in selectedSeasonings)
        {
            if (card != null)
                card.SetSelected(false);
        }

        selectedIngredients.Clear();
        selectedSeasonings.Clear();

        RebuildPot();
        UpdateCounts();

        Debug.Log("Đã reset toàn bộ lựa chọn.");
    }

    public void Cook()
    {
        int ingredientCount = GetTotalAmount(potIngredientAmounts);
        int seasoningCount = GetTotalAmount(potSeasoningAmounts);

        if (ingredientCount == 0)
        {
            Debug.Log("Chưa chọn nguyên liệu nào.");
            return;
        }

        Debug.Log("===== COOK START =====");
        Debug.Log("Số nguyên liệu: " + ingredientCount);
        Debug.Log("Số gia vị: " + seasoningCount);

        foreach (var kv in potIngredientAmounts)
            Debug.Log("Nguyên liệu: " + kv.Key + " x" + kv.Value);

        foreach (var kv in potSeasoningAmounts)
            Debug.Log("Gia vị: " + kv.Key + " x" + kv.Value);

        Debug.Log("Nấu xong! (tạm thời chưa tính điểm)");
    }

    public List<SelectableIngredientCard> GetSelectedIngredientCards()
    {
        return new List<SelectableIngredientCard>(selectedIngredients);
    }

    public List<SelectableIngredientCard> GetSelectedSeasoningCards()
    {
        return new List<SelectableIngredientCard>(selectedSeasonings);
    }

    // =========================
    // FLOW MỚI
    // =========================

    public void LoadTransferredItemsToLeftPanel()
    {
        BuildInventoryLookup();

        leftIngredientAmounts.Clear();
        leftSeasoningAmounts.Clear();
        potIngredientAmounts.Clear();
        potSeasoningAmounts.Clear();

        if (KitchenTransferManager.Instance == null)
        {
            Debug.LogWarning("Chưa có KitchenTransferManager.");
            RebuildNewUI();
            return;
        }

        List<KeyValuePair<string, int>> items = KitchenTransferManager.Instance.GetTransferredItems();

        foreach (var kv in items)
        {
            if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData inventoryItem))
                continue;

            if (inventoryItem == null || inventoryItem.cookingData == null)
                continue;

            if (inventoryItem.cookingData.kind == IngredientKind.Seasoning)
                leftSeasoningAmounts[kv.Key] = kv.Value;
            else
                leftIngredientAmounts[kv.Key] = kv.Value;
        }

        RebuildNewUI();
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

    private void RebuildNewUI()
    {
        RebuildAmountPanel(leftIngredientsContent, leftIngredientAmounts, OnLeftIngredientClicked);
        RebuildAmountPanel(leftSeasoningsContent, leftSeasoningAmounts, OnLeftSeasoningClicked);
        RebuildAmountPanel(newPotIngredientsContent, potIngredientAmounts, OnPotIngredientClicked);
        RebuildAmountPanel(newPotSeasoningsContent, potSeasoningAmounts, OnPotSeasoningClicked);

        UpdateCounts();
    }

    private void RebuildAmountPanel(Transform parent, Dictionary<string, int> source, System.Action<string> clickAction)
    {
        if (parent == null)
            return;

        ClearChildren(parent);

        foreach (var kv in source)
        {
            if (kv.Value <= 0)
                continue;

            if (!inventoryLookup.TryGetValue(kv.Key, out InventoryItemData itemData))
                continue;

            if (stackSlotPrefab == null)
                continue;

            Sprite iconSprite = itemData.icon;
            if (iconSprite == null && itemData.cookingData != null)
                iconSprite = itemData.cookingData.icon;

            CookingStackSlotUI slot = Instantiate(stackSlotPrefab, parent, false);
            slot.gameObject.SetActive(true);
            slot.Setup(kv.Key, iconSprite, kv.Value, clickAction);
        }
    }

    private void OnLeftIngredientClicked(string itemId)
    {
        if (GetTotalAmount(potIngredientAmounts) >= maxIngredients)
        {
            Debug.Log("Đã đạt tối đa nguyên liệu.");
            return;
        }

        if (!TryMoveOne(leftIngredientAmounts, potIngredientAmounts, itemId))
            return;

        RebuildNewUI();
    }

    private void OnLeftSeasoningClicked(string itemId)
    {
        if (GetTotalAmount(potSeasoningAmounts) >= maxSeasonings)
        {
            Debug.Log("Đã đạt tối đa gia vị.");
            return;
        }

        if (!TryMoveOne(leftSeasoningAmounts, potSeasoningAmounts, itemId))
            return;

        RebuildNewUI();
    }

    private void OnPotIngredientClicked(string itemId)
    {
        if (!TryMoveOne(potIngredientAmounts, leftIngredientAmounts, itemId))
            return;

        RebuildNewUI();
    }

    private void OnPotSeasoningClicked(string itemId)
    {
        if (!TryMoveOne(potSeasoningAmounts, leftSeasoningAmounts, itemId))
            return;

        RebuildNewUI();
    }

    private bool TryMoveOne(Dictionary<string, int> from, Dictionary<string, int> to, string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (!from.TryGetValue(itemId, out int value))
            return false;

        if (value <= 0)
            return false;

        from[itemId] = value - 1;
        if (from[itemId] <= 0)
            from.Remove(itemId);

        if (!to.ContainsKey(itemId))
            to[itemId] = 0;

        to[itemId] += 1;
        return true;
    }

    private void ReturnAllPotItemsToLeft()
    {
        MoveAll(potIngredientAmounts, leftIngredientAmounts);
        MoveAll(potSeasoningAmounts, leftSeasoningAmounts);

        potIngredientAmounts.Clear();
        potSeasoningAmounts.Clear();

        RebuildNewUI();
    }

    private void MoveAll(Dictionary<string, int> from, Dictionary<string, int> to)
    {
        foreach (var kv in from)
        {
            if (!to.ContainsKey(kv.Key))
                to[kv.Key] = 0;

            to[kv.Key] += kv.Value;
        }
    }

    private int GetTotalAmount(Dictionary<string, int> dict)
    {
        int total = 0;

        foreach (var kv in dict)
            total += kv.Value;

        return total;
    }
}