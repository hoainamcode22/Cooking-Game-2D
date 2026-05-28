using UnityEngine;
using UnityEngine.EventSystems;

public class UIRaycastBlocker : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnPointerClick(PointerEventData eventData) => Consume(eventData);
    public void OnPointerDown(PointerEventData eventData) => Consume(eventData);
    public void OnPointerUp(PointerEventData eventData) => Consume(eventData);
    public void OnBeginDrag(PointerEventData eventData) => Consume(eventData);
    public void OnDrag(PointerEventData eventData) => Consume(eventData);
    public void OnEndDrag(PointerEventData eventData) => Consume(eventData);

    private void Consume(PointerEventData eventData)
    {
        if (!isActiveAndEnabled) return;
        if (eventData == null) return;
        eventData.Use();
    }
}
