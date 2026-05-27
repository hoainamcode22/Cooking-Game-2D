using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CookingSelectionManager : MonoBehaviour
{
    [Header("Current Flavor UI")]
    [SerializeField] private CurrentFlavorBoxUI currentFlavor;

    [Header("Current Total Flavor")]
    [SerializeField] private FlavorVector currentFlavorVector = FlavorVector.Zero;
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

    private readonly Dictionary<string, int> potIngredientAmounts = new();
    private readonly Dictionary<string, int> potSeasoningAmounts = new();
    private bool canSelectIngredient = false;

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
    public void EnableIngredientSelection()
    {
        canSelectIngredient = true;
        Debug.Log("[CookingSelectionManager] Đã cho phép chọn nguyên liệu.");
    }

    public void DisableIngredientSelection()
    {
        canSelectIngredient = false;
        Debug.Log("[CookingSelectionManager] Đã khóa chọn nguyên liệu.");
    }

    public void TrySelect(SelectableIngredientCard card)
    {
        if(!canSelectIngredient)
            return;
        if (card == null) return;

        int quantity = card.GetQuantity();

        if (quantity <= 0)
        {
            Debug.Log("Không còn " + card.GetItemName() + " trong kho.");
            return;
        }

        Debug.Log("Đang chạy TrySelect mới");

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

            AddFlavor(card.GetIngredientData());
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

            // Nguyên liệu cũng có hương vị nên cũng phải cộng vector
            AddFlavor(card.GetIngredientData());
        }

        card.SetQuantityFromKitchen(quantity - 1);
        card.SetSelected(true);

        RebuildPot();
        UpdateCounts();
    }

    public void TryDeselect(SelectableIngredientCard card)
    {
        if(!canSelectIngredient)
            return;
        if (card == null) return;

        int quantity = card.GetQuantity();

        if (card.isSeasoning)
        {
            if (!selectedSeasonings.Contains(card)) return;

            selectedSeasonings.Remove(card);
            Debug.Log("Đã bỏ gia vị: " + card.GetItemName());

            RemoveFlavor(card.GetIngredientData());
        }
        else
        {
            if (!selectedIngredients.Contains(card)) return;

            selectedIngredients.Remove(card);
            Debug.Log("Đã bỏ nguyên liệu: " + card.GetItemName());

            // Bỏ nguyên liệu thì phải trừ vector hương vị
            RemoveFlavor(card.GetIngredientData());
        }

        card.SetQuantityFromKitchen(quantity + 1);
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
            card.SetQuantityFromKitchen(card.GetQuantity() + 1);
        }

        foreach (var card in selectedSeasonings)
        {
            if (card != null)
                card.SetSelected(false);
            card.SetQuantityFromKitchen(card.GetQuantity() + 1);
        }
        
        selectedIngredients.Clear();
        selectedSeasonings.Clear();
        RebuildPot();
        UpdateCounts();
        ResetFlavor();

        Debug.Log("Đã reset toàn bộ lựa chọn.");
    }
    public void ResetUIAfterCooking()
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
        ResetFlavor();
        Debug.Log("Đã reset toàn bộ lựa chọn sau khi nấu xong.");
    }


    public List<SelectableIngredientCard> GetSelectedIngredientCards()
    {
        return new List<SelectableIngredientCard>(selectedIngredients);
    }

    public List<SelectableIngredientCard> GetSelectedSeasoningCards()
    {
        return new List<SelectableIngredientCard>(selectedSeasonings);
    }


    private int GetTotalAmount(Dictionary<string, int> dict)
    {
        int total = 0;

        foreach (var kv in dict)
            total += kv.Value;

        return total;
    }


    private void AddFlavor(IngredientData data)
    {
        if (data == null) return;

        currentFlavorVector += data.vector;
        UpdateCurrentFlavorUI();
    }

    private void RemoveFlavor(IngredientData data)
    {
        if (data == null) return;

        currentFlavorVector -= data.vector;
        UpdateCurrentFlavorUI();
    }

    private void UpdateCurrentFlavorUI()
    {
        if (currentFlavor != null)
            currentFlavor.SetFlavor(currentFlavorVector);
    }

    public void ResetFlavor()
    {
        currentFlavorVector = FlavorVector.Zero;
        UpdateCurrentFlavorUI();
    }
}