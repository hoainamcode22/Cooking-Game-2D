using UnityEngine;
using UnityEngine.EventSystems;

public class PotCardClickBack : MonoBehaviour, IPointerClickHandler
{
    private CookingSelectionManager manager;
    private SelectableIngredientCard sourceCard;

    public void Init(CookingSelectionManager m, SelectableIngredientCard card)
    {
        manager = m;
        sourceCard = card;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null && sourceCard != null)
        {
            Debug.Log("Click pot → return");
            manager.TryDeselect(sourceCard);
        }
    }
}