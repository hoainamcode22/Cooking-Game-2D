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
                    // Thẻ UI v2 mang id BẾP (IngredientData.id) — kho lưu itemId NÔNG TRẠI.
                    // Dịch ngược trước khi trừ, id trùng thì giữ nguyên. [Sếp 2026-08-27]
                    cookedItemIds.Add(KitchenIdMap.ToFarm(itemId));
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
                    cookedItemIds.Add(KitchenIdMap.ToFarm(itemId)); // như trên [Sếp 2026-08-27]
                }
            }
        }

        if (KitchenTransferManager.Instance != null)
        {
            // [2026-09-24] Moi mon chi tru DUNG 1 cho moi nguyen lieu/gia vi trong noi; phan con lai o khay.
            var ktm = KitchenTransferManager.Instance;
            var truoc = new System.Text.StringBuilder();
            foreach (var id in cookedItemIds) truoc.Append(id).Append(' ').Append(ktm.GetTransferredAmount(id)).Append("->");
            KitchenTransferManager.Instance.SetAfterCooking(cookedItemIds);
            foreach (var id in cookedItemIds) truoc.Append(' ').Append(id).Append('=').Append(ktm.GetTransferredAmount(id));
            Debug.Log("[Bep] Tru nguyen lieu sau khi nau: " + truoc);
        }
        else
        {
            Debug.LogWarning("KitchenTransferManager.Instance is missing.");
        }
    }
}