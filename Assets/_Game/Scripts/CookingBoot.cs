using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Khởi tạo panel trái từ KitchenTransferManager.
/// Logic OnDrop (từ CookingDropZone) được gộp vào đây dưới dạng inner class.
/// </summary>
public class CookingBoot : MonoBehaviour
{
    [Header("Refs")]
    public CookingSelectionManager selection;
    public LeftPanelController     leftPanel;

    [Header("Cooking Item Database")]
    public List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

    private readonly Dictionary<string, InventoryItemData> _inventoryLookup = new Dictionary<string, InventoryItemData>();

    private IEnumerator Start()
    {
        yield return null;

        if (selection == null || leftPanel == null)
            yield break;

        BuildInventoryLookup();
        FillOldCardsFromTransferredItems();

        selection.RegisterAllLeftCards(
            leftPanel.IngredientsContent,
            leftPanel.SeasoningsContent
        );
    }

    private void BuildInventoryLookup()
    {
        _inventoryLookup.Clear();
        foreach (var item in cookingInventoryItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (!_inventoryLookup.ContainsKey(item.itemId))
                _inventoryLookup.Add(item.itemId, item);
        }
    }

    private void FillOldCardsFromTransferredItems()
    {
        var ingredientItems = new List<InventoryItemData>();
        var seasoningItems  = new List<InventoryItemData>();

        if (KitchenTransferManager.Instance != null)
        {
            foreach (var kv in KitchenTransferManager.Instance.GetTransferredItems())
            {
                if (!_inventoryLookup.TryGetValue(kv.Key, out var invItem)) continue;
                if (invItem?.cookingData == null) continue;

                if (invItem.cookingData.kind == IngredientKind.Seasoning)
                    seasoningItems.Add(invItem);
                else
                    ingredientItems.Add(invItem);
            }
        }

        ApplyToCardGroup(leftPanel.IngredientsContent, ingredientItems, false);
        ApplyToCardGroup(leftPanel.SeasoningsContent,  seasoningItems,  true);
    }

    private void ApplyToCardGroup(Transform contentRoot, List<InventoryItemData> items, bool isSeasoning)
    {
        if (contentRoot == null) return;

        var cards = new List<SelectableIngredientCard>();
        foreach (Transform child in contentRoot)
        {
            var card = child.GetComponent<SelectableIngredientCard>();
            if (card != null) cards.Add(card);
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
            Debug.LogWarning($"[CookingBoot] Thiếu slot {(isSeasoning ? "gia vị" : "nguyên liệu")}. Dư: {items.Count - cards.Count}");
    }

    private void SetupCard(SelectableIngredientCard card, InventoryItemData invItem, bool isSeasoning)
    {
        if (card == null || invItem?.cookingData == null) return;

        IngredientData ing = invItem.cookingData;
        var ui = card.GetComponent<IngredientItemUI>();
        if (ui != null)
        {
            string displayName = !string.IsNullOrEmpty(invItem.displayName) ? invItem.displayName : ing.displayName;
            Sprite mainIcon    = invItem.icon != null ? invItem.icon : ing.icon;
            ui.Setup(displayName, mainIcon, null, ing.stars, false);
        }

        card.SetIngredientData(ing);
        card.Init(selection, isSeasoning);
        card.SetSelected(false);
    }

    // ── Drop Zone (gộp từ CookingDropZone) ───────────────────────────────────
    // Gắn component này lên cùng GameObject với vùng drop, set isSeasoning đúng loại.

    [System.Serializable]
    public class DropZoneHandler : MonoBehaviour, IDropHandler
    {
        [Tooltip("Gán CookingSelectionManager vào đây")]
        public CookingSelectionManager manager;

        [Tooltip("true = vùng thả gia vị, false = vùng thả nguyên liệu")]
        public bool isSeasoning;

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;

            var card = eventData.pointerDrag.GetComponent<SelectableIngredientCard>();
            if (card == null)
            {
                Debug.LogWarning("[DropZone] Item kéo không có SelectableIngredientCard.");
                return;
            }

            if (card.isSeasoning != isSeasoning)
            {
                Debug.Log("[DropZone] Sai loại item — không thả vào vùng này.");
                return;
            }

            manager.TrySelect(card);
        }
    }
}
