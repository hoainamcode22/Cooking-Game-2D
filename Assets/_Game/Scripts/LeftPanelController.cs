using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gộp từ LeftPanelRefs + LeftPanelSpawner.
/// Nắm toàn bộ reference UI và tự xử lý spawn card từ KitchenTransferManager (runtime)
/// hoặc từ danh sách CardData thủ công (editor/fallback).
/// </summary>
public class LeftPanelController : MonoBehaviour
{
    // ── Containers & Headers (từ LeftPanelRefs) ───────────────────────────────
    [Header("Containers")]
    [SerializeField] private Transform ingredientsContent;
    [SerializeField] private Transform seasoningsContent;

    [Header("Headers")]
    [SerializeField] private TMP_Text ingredientsTitleText;
    [SerializeField] private TMP_Text ingredientsCountText;
    [SerializeField] private TMP_Text seasoningsTitleText;
    [SerializeField] private TMP_Text seasoningsCountText;

    [Header("Sample Prefabs")]
    [SerializeField] private IngredientItemUI ingredientCardSample;
    [SerializeField] private IngredientItemUI seasoningCardSample;

    // ── CardData thủ công (từ LeftPanelSpawner — dùng khi KitchenTransfer rỗng) ─
    [System.Serializable]
    public class CardData
    {
        public IngredientData ingredientData;
        public string         itemName;
        public Sprite         mainIcon;
        public Sprite         topIcon;
        [Range(1, 3)] public int starCount = 3;
    }

    [Header("Manual Card Lists (Editor / Fallback)")]
    [SerializeField] private List<CardData> ingredients = new List<CardData>();
    [SerializeField] private List<CardData> seasonings  = new List<CardData>();

    // ── Runtime: KitchenTransferManager ───────────────────────────────────────
    [Header("Runtime")]
    [SerializeField] private CookingSelectionManager selection;
    [SerializeField] private List<InventoryItemData> cookingInventoryItems = new List<InventoryItemData>();

    // Expose cho CookingSelectionManager.RegisterAllLeftCards nếu cần gọi ngoài
    public Transform IngredientsContent => ingredientsContent;
    public Transform SeasoningsContent  => seasoningsContent;

    private readonly Dictionary<string, InventoryItemData> _inventoryLookup = new Dictionary<string, InventoryItemData>();

    // ── Khởi tạo ─────────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        yield return null; // chờ 1 frame để KitchenTransferManager sẵn sàng

        BuildInventoryLookup();

        // Ưu tiên dữ liệu runtime từ KitchenTransferManager; fallback về CardData thủ công
        bool hasTransferredItems = KitchenTransferManager.Instance != null
            && KitchenTransferManager.Instance.GetTransferredItems().Count > 0;

        if (hasTransferredItems)
            FillFromKitchenTransfer();
        else
            SpawnFromCardData();

        if (selection != null)
            selection.RegisterAllLeftCards(ingredientsContent, seasoningsContent);
    }

    // ── Đường dẫn Runtime: KitchenTransferManager ────────────────────────────

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

    private void FillFromKitchenTransfer()
    {
        var ingredientItems = new List<InventoryItemData>();
        var seasoningItems  = new List<InventoryItemData>();

        foreach (var kv in KitchenTransferManager.Instance.GetTransferredItems())
        {
            if (!_inventoryLookup.TryGetValue(kv.Key, out var invItem)) continue;
            if (invItem?.cookingData == null) continue;

            if (invItem.cookingData.kind == IngredientKind.Seasoning)
                seasoningItems.Add(invItem);
            else
                ingredientItems.Add(invItem);
        }

        ApplyToCardGroup(ingredientsContent, ingredientItems, false);
        ApplyToCardGroup(seasoningsContent,  seasoningItems,  true);
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
            Debug.LogWarning($"[LeftPanelController] Thiếu slot {(isSeasoning ? "gia vị" : "nguyên liệu")}. Dư: {items.Count - cards.Count}");
    }

    private void SetupCard(SelectableIngredientCard card, InventoryItemData invItem, bool isSeasoning)
    {
        if (card == null || invItem?.cookingData == null) return;

        IngredientData ing = invItem.cookingData;
        var ui = card.GetComponent<IngredientItemUI>();
        if (ui != null)
        {
            string  displayName = !string.IsNullOrEmpty(invItem.displayName) ? invItem.displayName : ing.displayName;
            Sprite  mainIcon    = invItem.icon != null ? invItem.icon : ing.icon;
            ui.Setup(displayName, mainIcon, null, ing.stars, false);
        }

        card.SetIngredientData(ing);
        card.Init(selection, isSeasoning);
        card.SetSelected(false);
    }

    // ── Đường dẫn Fallback: CardData thủ công (từ LeftPanelSpawner) ──────────

    [ContextMenu("Spawn All (Manual Data)")]
    public void SpawnFromCardData()
    {
        SpawnCardList(ingredients, ingredientsContent, ingredientCardSample);
        SpawnCardList(seasonings,  seasoningsContent,  seasoningCardSample);
    }

    private void SpawnCardList(List<CardData> dataList, Transform parent, IngredientItemUI samplePrefab)
    {
        if (parent == null || samplePrefab == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == samplePrefab.transform) continue;
            Destroy(child.gameObject);
        }

        samplePrefab.gameObject.SetActive(false);

        foreach (var data in dataList)
        {
            var newCard = Instantiate(samplePrefab, parent, false);
            newCard.gameObject.SetActive(true);

            var rt = newCard.GetComponent<RectTransform>();
            if (rt != null) { rt.localScale = Vector3.one; rt.anchoredPosition = Vector2.zero; }

            string displayName = data.itemName;
            Sprite mainSprite  = data.mainIcon;

            if (data.ingredientData != null)
            {
                if (!string.IsNullOrEmpty(data.ingredientData.displayName)) displayName = data.ingredientData.displayName;
                if (data.ingredientData.icon != null)                       mainSprite  = data.ingredientData.icon;
            }

            newCard.Setup(displayName, mainSprite, data.topIcon, data.starCount, false);

            var selectable = newCard.GetComponent<SelectableIngredientCard>();
            if (selectable != null)
                selectable.SetIngredientData(data.ingredientData);
            else
                Debug.LogWarning("[LeftPanelController] Card thiếu SelectableIngredientCard: " + displayName);
        }
    }
}
