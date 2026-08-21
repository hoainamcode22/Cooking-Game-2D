using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// F9  = in trạng thái mọi CỔNG CHẶN CLICK world (popup nào mở, khoá nào bật, UI nào đè).
/// F10 = soi VÌ SAO UI TÀNG HÌNH: liệt kê mọi canvas gốc + mổ xẻ cây Canvas_MarketPopup,
///       node nào alpha 0 / bị cull / sorting lệch là hiện nguyên hình.
///
/// Ca thật 21/08: popup chợ MỞ, mua hàng được, nhưng KHÔNG VẼ gì lên màn hình.
/// "Nhận click mà không vẽ" chỉ có mấy thủ phạm: CanvasGroup.alpha=0 (alpha không tắt
/// raycast), CanvasRenderer bị cull, Canvas con overrideSorting chui xuống dưới, hoặc
/// scale 0 theo một trục. F10 in đủ cả bốn cho từng node — khỏi đoán.
/// </summary>
[DefaultExecutionOrder(9999)]
public class PopupGateDebugF9 : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PopupGateDebugF9>() != null) return;
        var go = new GameObject("~PopupGateDebugF9");
        go.AddComponent<PopupGateDebugF9>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.f9Key.wasPressedThisFrame)  InTrangThai();
        if (Keyboard.current.f10Key.wasPressedThisFrame) SoiTangHinh();
    }

    // ═══════════════════════ F10 — VÌ SAO TÀNG HÌNH ═══════════════════════

    private static void SoiTangHinh()
    {
        var sb = new StringBuilder("═══ F10 · SOI UI TÀNG HÌNH ═══\n");

        // 1. Mọi canvas GỐC: cái nào vẽ trên cái nào là ở đây.
        sb.Append("── CANVAS GỐC (vẽ từ số nhỏ tới số lớn, số lớn ĐÈ số nhỏ) ──\n");
        Canvas marketCv = null;
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!c.isRootCanvas) continue;
            sb.Append("  ").Append(c.name)
              .Append("  order=").Append(c.sortingOrder)
              .Append("  mode=").Append(c.renderMode)
              .Append(c.enabled ? "" : "  ⚠ CANVAS TẮT")
              .Append(c.gameObject.activeInHierarchy ? "" : "  (object TẮT)")
              .Append('\n');
            if (c.name == "Canvas_MarketPopup") marketCv = c;
        }

        // 2. Mổ xẻ cây chợ.
        if (marketCv == null)
        {
            sb.Append("Không tìm thấy Canvas_MarketPopup trong scene!\n");
            Debug.Log(sb.ToString());
            return;
        }

        sb.Append("── CÂY Canvas_MarketPopup (chỉ node có Canvas/CanvasGroup/Graphic) ──\n");
        int soDong = 0;
        Duyet(marketCv.transform, 0, sb, ref soDong);
        if (soDong >= GioiHanDong)
            sb.Append("  … cắt bớt ở ").Append(GioiHanDong).Append(" dòng.\n");

        sb.Append("ĐỌC KẾT QUẢ: tìm dòng có ⚠ — \"ALPHA THỪA KẾ 0\" nghĩa là một CanvasGroup " +
                  "phía trên đã hạ alpha cả nhánh; \"CULL\" là CanvasRenderer bị tắt vẽ; " +
                  "\"SCALE 0\" là bị bóp dẹt; \"CHUI XUỐNG order\" là canvas con vẽ dưới canvas khác.\n");
        Debug.Log(sb.ToString());
    }

    private const int GioiHanDong = 90;

    private static void Duyet(Transform t, int sau, StringBuilder sb, ref int soDong)
    {
        if (soDong >= GioiHanDong) return;

        var cv  = t.GetComponent<Canvas>();
        var cg  = t.GetComponent<CanvasGroup>();
        var gr  = t.GetComponent<Graphic>();
        var cr  = t.GetComponent<CanvasRenderer>();

        if (cv != null || cg != null || gr != null || !t.gameObject.activeSelf)
        {
            sb.Append(new string(' ', 2 + sau * 2)).Append(t.name);

            if (!t.gameObject.activeSelf) sb.Append("  [object TẮT — cả nhánh dưới không vẽ]");

            if (cv != null)
            {
                sb.Append("  [Canvas");
                if (!cv.enabled) sb.Append(" ⚠ TẮT");
                if (cv.overrideSorting) sb.Append(" overrideSorting order=").Append(cv.sortingOrder);
                sb.Append(']');
            }

            if (cg != null)
            {
                sb.Append("  [CanvasGroup alpha=").Append(cg.alpha.ToString("0.00"));
                if (cg.alpha < 0.01f) sb.Append(" ⚠ ALPHA 0 — THỦ PHẠM TÀNG HÌNH");
                if (!cg.blocksRaycasts) sb.Append(" (không chặn raycast)");
                sb.Append(']');
            }

            if (gr != null)
            {
                sb.Append("  [").Append(gr.GetType().Name)
                  .Append(gr.enabled ? "" : " ⚠ TẮT")
                  .Append(" màu.a=").Append(gr.color.a.ToString("0.00"));
                if (gr.color.a < 0.01f) sb.Append(" ⚠ MÀU TRONG SUỐT");
                if (cr != null)
                {
                    float thuaKe = cr.GetInheritedAlpha();
                    if (thuaKe < 0.01f) sb.Append(" ⚠ ALPHA THỪA KẾ 0 (CanvasGroup cha nào đó = 0)");
                    if (cr.cull) sb.Append(" ⚠ CULL — bị tắt vẽ");
                }
                sb.Append(']');
            }

            Vector3 s = t.lossyScale;
            if (Mathf.Abs(s.x) < 0.001f || Mathf.Abs(s.y) < 0.001f)
                sb.Append("  ⚠ SCALE 0 (").Append(s.x.ToString("0.###")).Append(", ")
                  .Append(s.y.ToString("0.###")).Append(')');

            sb.Append('\n');
            soDong++;
        }

        for (int i = 0; i < t.childCount; i++)
            Duyet(t.GetChild(i), sau + 1, sb, ref soDong);
    }

    // ═══════════════════════ F9 — CỔNG CHẶN CLICK ═══════════════════════

    private static void InTrangThai()
    {
        var sb = new StringBuilder("═══ F9 · TRẠNG THÁI CỔNG CHẶN CLICK ═══\n");

        sb.Append("EditModeManager.IsEditMode      = ").Append(EditModeManager.IsEditMode).Append('\n');
        sb.Append("FarmInputLock.BlockMapPan       = ").Append(FarmInputLock.BlockMapPan).Append('\n');

        PopupManager pm = PopupManager.Instance;
        sb.Append("PopupManager.IsAnyPopupOpen()   = ")
          .Append(pm != null ? pm.IsAnyPopupOpen().ToString() : "PopupManager NULL").Append('\n');

        sb.Append("  CropProcessPopupUI.AnyOpen    = ").Append(CropProcessPopupUI.AnyOpen).Append('\n');
        sb.Append("  OrderBoardPopupUI.AnyOpen     = ").Append(OrderBoardPopupUI.AnyOpen).Append('\n');
        sb.Append("  MillPopupUI.AnyOpen           = ").Append(MillPopupUI.AnyOpen).Append('\n');
        sb.Append("  UnifiedTaskPopupUI.IsOpenStatic = ").Append(UnifiedTaskPopupUI.IsOpenStatic).Append('\n');
        sb.Append("  MarketManager.IsOpen (fallback) = ")
          .Append(MarketManager.Instance != null ? MarketManager.Instance.IsOpen.ToString() : "Instance null")
          .Append('\n');

        if (pm != null)
        {
            foreach (string ten in new[] { "warehousePopup", "marketPopup", "trainProcessPopup",
                                           "trainLoadPopup", "shopPopup", "ewarPopup" })
            {
                FieldInfo f = typeof(PopupManager).GetField(ten,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                object v = f != null ? f.GetValue(pm) : null;
                var comp = v as Component;

                if (comp == null) { sb.Append("  ").Append(ten).Append(" = null/chưa gán\n"); continue; }

                PropertyInfo pIsOpen = comp.GetType().GetProperty("IsOpen");
                object isOpen = pIsOpen != null ? pIsOpen.GetValue(comp) : "(không có IsOpen)";
                sb.Append("  ").Append(ten).Append(".IsOpen = ").Append(isOpen)
                  .Append(comp.gameObject.activeInHierarchy ? "" : "  (object đang TẮT)").Append('\n');
            }
        }

        Vector2 vt = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        sb.Append("Con trỏ tại ").Append(vt).Append('\n');

        if (EventSystem.current == null)
            sb.Append("EventSystem = NULL — mọi UI đều không nhận input!\n");
        else
        {
            var data = new PointerEventData(EventSystem.current) { position = vt };
            var kq = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, kq);

            if (kq.Count == 0) sb.Append("RaycastAll: không trúng graphic nào (world sạch).\n");
            for (int i = 0; i < kq.Count; i++)
            {
                Canvas c = kq[i].gameObject.GetComponentInParent<Canvas>();
                sb.Append("  UI đè #").Append(i + 1).Append(": ").Append(DuongDan(kq[i].gameObject.transform))
                  .Append("   [canvas: ").Append(c != null ? c.name : "?").Append("]\n");
            }
        }

        Debug.Log(sb.ToString());
    }

    private static string DuongDan(Transform tr)
    {
        string s = tr.name;
        while (tr.parent != null) { tr = tr.parent; s = tr.name + "/" + s; }
        return s;
    }
}
