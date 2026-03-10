using UnityEngine;
using UnityEngine.EventSystems;

public class CookingDropZone : MonoBehaviour, IDropHandler
{
    public CookingSelectionManager manager;
    public bool isSeasoning;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DraggableItem dragged = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (dragged == null) return;

        SelectableIngredientCard card = eventData.pointerDrag.GetComponent<SelectableIngredientCard>();
        if (card == null)
        {
            Debug.LogWarning("Item kéo không có SelectableIngredientCard");
            return;
        }

        // kiểm tra đúng loại
        if (card.isSeasoning != isSeasoning)
        {
            Debug.Log("Sai loại item. Không thả vào vùng này.");
            return;
        }

        // nếu chưa chọn thì add vào nồi
        manager.TrySelect(card);
    }
}