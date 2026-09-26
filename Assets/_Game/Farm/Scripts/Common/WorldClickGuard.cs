// ============================================================================
//  WorldClickGuard — chan cham XUYEN popup xuong cong trinh tren map (2026-09-25)
//  Vd: dang mo Settings, bam vao nut / thanh truot ma chuong bo phia sau cung mo panel.
//  Cung quy tac voi MillBuildingClick: CHI chan khi tia UI trung mot Canvas co ten chua "Popup"
//  (Canvas_Popup, Canvas_MarketPopup, Canvas_OrderBoardPopup...) hoac PopupManager dang mo popup.
//  KHONG chan HUD (HUD phu gan kin man hinh, chan theo HUD la mat het cu cham len map).
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class WorldClickGuard
{
    private static readonly List<RaycastResult> _kq = new List<RaycastResult>(16);
    private static PointerEventData _ped;
    private static EventSystem _esCu;

    /// <summary>true = diem cham nam tren 1 popup dang mo -> cong trinh tren map KHONG duoc nhan cu cham nay.</summary>
    public static bool ConTroTrenPopup(Vector2 manHinh)
    {
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return true;
        var es = EventSystem.current;
        if (es == null) return false;
        if (_ped == null || _esCu != es) { _ped = new PointerEventData(es); _esCu = es; }
        _ped.position = manHinh;
        _kq.Clear();
        es.RaycastAll(_ped, _kq);
        for (int i = 0; i < _kq.Count; i++)
        {
            var go = _kq[i].gameObject;
            if (go == null) continue;
            var cv = go.GetComponentInParent<Canvas>();
            if (cv == null) continue;
            var goc = cv.rootCanvas != null ? cv.rootCanvas : cv;
            if (goc.name.IndexOf("Popup", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            // popup tu dung canvas rieng (ten object chua Popup / Panel_Dim)
            for (Transform t = go.transform; t != null && t != goc.transform; t = t.parent)
            {
                string n = t.name;
                if (n.StartsWith("Panel_Dim") || n.StartsWith("Popup_") || n.EndsWith("Popup")) return true;
            }
        }
        return false;
    }
}
