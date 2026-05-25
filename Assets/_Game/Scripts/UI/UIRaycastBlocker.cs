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

        CanvasGroup[] groups = GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup group = groups[i];
            if (!group.blocksRaycasts || !group.interactable || group.alpha <= 0.001f)
                return;
        }

        eventData.Use();
    }
}
