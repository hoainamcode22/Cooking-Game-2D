using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;//mới
public class CookingItemConsumer : MonoBehaviour
{    
    [Header("Cooking Selection Manager")]
    [SerializeField] private CookingSelectionManager cookingSelectionManager; // Tham chiếu đến CookingSelectionManager để lấy thông tin về nguyên liệu và gia vị đã chọn

    
    public void ConsumeSelectedCookingItems()
    {
        if (cookingSelectionManager == null)
        {
            Debug.LogWarning("CookingSelectionManager is missing.");
            return;
        }

        List<SelectableIngredientCard> selectedIngredients = cookingSelectionManager.GetSelectedIngredientCards();
        List<SelectableIngredientCard> selectedSeasonings = cookingSelectionManager.GetSelectedSeasoningCards();
        List<string> cookedItemIds = new List<string>();

        if (selectedIngredients != null)
        {
            foreach (var card in selectedIngredients)
            {
                if (card == null) continue;

                string itemId = card.GetItemId();

                if (!string.IsNullOrEmpty(itemId))
                {
                    cookedItemIds.Add(itemId);
                }
            }
        }

        if (selectedSeasonings != null)
        {
            foreach (var card in selectedSeasonings)
            {
                if (card == null) continue;

                string itemId = card.GetItemId();

                if (!string.IsNullOrEmpty(itemId))
                {
                    cookedItemIds.Add(itemId);
                }
            }
        }

        if (KitchenTransferManager.Instance != null)
        {
            KitchenTransferManager.Instance.SetAfterCooking(cookedItemIds);
        }
        else
        {
            Debug.LogWarning("KitchenTransferManager.Instance is missing.");
        }
    }
}