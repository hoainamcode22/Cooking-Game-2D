using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Chuyển tiếp toàn bộ sự kiện Drag & Scroll lên ScrollRect cha gần nhất.
/// Giúp người chơi có thể dùng chuột (PC) hoặc vuốt ngón tay (Mobile/Touch) kéo cuộn mượt mà
/// ngay cả khi chạm vào bất kỳ thẻ card, hình ảnh, hay nút bấm nào bên trong ScrollView.
/// </summary>
public class UIDragScrollForwarder : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private ScrollRect _parentScrollRect;

    private ScrollRect ParentScrollRect
    {
        get
        {
            if (_parentScrollRect == null)
                _parentScrollRect = GetComponentInParent<ScrollRect>();
            return _parentScrollRect;
        }
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null)
            ParentScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null)
            ParentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null)
            ParentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ParentScrollRect != null)
            ParentScrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (ParentScrollRect != null)
            ParentScrollRect.OnScroll(eventData);
    }
}
