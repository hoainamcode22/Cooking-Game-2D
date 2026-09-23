using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Cham vao lop nen mo (Panel_Dim) thi dong popup — CHI khi diem cham trung chinh lop nen,
/// khong phai click noi len tu con (ban go, o vat pham...). Thay cho Button tren Panel_Dim.
/// </summary>
public class DimClickClose : MonoBehaviour, IPointerClickHandler
{
    public System.Action khiDong;

    public void OnPointerClick(PointerEventData e)
    {
        if (e == null) return;
        var trung = e.pointerPressRaycast.gameObject;
        if (trung == gameObject) khiDong?.Invoke();
    }
}
