using UnityEngine;

/// <summary>
/// TIỆN ÍCH RECTTRANSFORM cho popup máy xay — hai phép biến đổi mà cả 3 file hiệu ứng đều
/// cần, tách ra để không copy-paste ba lần.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO CẦN — CẠM BẪY PIVOT GÓC
/// ══════════════════════════════════════════════════════════════════════════
/// `MillPopupBuilderTool` neo node bằng các helper `TL/TR/BL/BR`, và những helper đó đặt
/// **pivot vào đúng góc** đó (TL ⇒ pivot (0,1), BR ⇒ pivot (1,0)). Điều này sinh ra hai lỗi
/// rất khó thấy, cả hai đều IM LẶNG:
///
/// 1. `rt.position` trả về vị trí world của PIVOT, không phải tâm hình.
///    ⇒ Bay từ `Slot_3` (pivot góc trên-trái) thì icon xuất phát từ mép trên-trái của thẻ
///      slot, lệch ~(59, +90) so với chỗ người chơi đang nhìn. Dùng
///      <see cref="TamWorld"/> thay cho `.position`.
///
/// 2. `localScale` phóng/co quanh PIVOT.
///    ⇒ Phóng `Output_Bubble` (pivot góc dưới-phải) lên 1.18 thì bao KHÔNG phồng tại chỗ mà
///      lao chéo lên trái 14px — nhìn như bao bị kéo đi chứ không phải nhịp thở. Gọi
///      <see cref="DoiPivotVeGiua"/> một lần trong Awake là hết.
///
/// `DoiPivotVeGiua` KHÔNG làm node xê dịch, và KHÔNG làm các node con xê dịch: khi pivot đi
/// từ p0 sang p1 thì gốc toạ độ cục bộ của node dịch +(p1−p0)×size, còn toạ độ cục bộ của
/// mọi node con dịch −(p1−p0)×size ⇒ triệt tiêu nhau. Đã kiểm bằng tay, xem chứng minh trong
/// phần thân hàm.
/// </summary>
public static class MillRectUtil
{
    private static readonly Vector2 GIUA = new Vector2(0.5f, 0.5f);

    /// <summary>
    /// Vị trí WORLD của TÂM hình chữ nhật — dùng thay cho <c>rt.position</c> mọi khi cần
    /// "chỗ người chơi đang nhìn". Xem cạm bẫy #1 ở đầu file.
    /// </summary>
    public static Vector3 TamWorld(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;

        // rect.center là (0,0) khi pivot ở giữa, và là offset tới tâm khi pivot ở góc.
        return rt.TransformPoint(rt.rect.center);
    }

    /// <summary>
    /// Đưa pivot của <paramref name="rt"/> về giữa (0.5, 0.5) mà KHÔNG làm hình xê dịch,
    /// để <c>localScale</c> phóng/co quanh tâm. Xem cạm bẫy #2 ở đầu file.
    ///
    /// Gọi được nhiều lần — pivot đã ở giữa thì không làm gì.
    /// </summary>
    public static void DoiPivotVeGiua(RectTransform rt)
    {
        if (rt == null) return;

        Vector2 p0 = rt.pivot;
        if (Mathf.Abs(p0.x - 0.5f) < 0.0001f && Mathf.Abs(p0.y - 0.5f) < 0.0001f) return;

        Vector2 size = rt.rect.size;

        // Layout chưa chạy (size = 0) thì đổi pivot sẽ tính bù bằng 0 và hình nhảy chỗ.
        // Thà không đổi còn hơn làm lệch — hiệu ứng chỉ hơi lệch tâm, không vỡ layout.
        if (size.x <= 0.01f || size.y <= 0.01f) return;

        // Vị trí mép min trong hệ toạ độ CHA:  anchorPoint + anchoredPosition − pivot×size.
        // Muốn mép min không đổi khi pivot p0 → p1 thì:
        //     aPos1 − p1×size = aPos0 − p0×size
        // ⇒   aPos1 = aPos0 + (p1 − p0)×size
        Vector2 bu = new Vector2((GIUA.x - p0.x) * size.x,
                                (GIUA.y - p0.y) * size.y);

        rt.pivot            = GIUA;
        rt.anchoredPosition = rt.anchoredPosition + bu;
    }

    /// <summary>
    /// Cho <paramref name="con"/> dùng hệ neo TRÙNG PIVOT của <paramref name="cha"/>.
    ///
    /// ⚠ VÌ SAO KHÔNG NEO (0.5, 0.5) CHO XONG:
    /// `RectTransformUtility.ScreenPointToLocalPointInRectangle` trả về toạ độ tính từ PIVOT
    /// của cha. Còn `anchoredPosition` tính từ ĐIỂM NEO. Hai gốc này chỉ trùng nhau khi
    /// anchor = pivot. Cha là `AnimationBox` (pivot góc trên-trái) mà con neo giữa thì mọi
    /// hạt bị lệch nửa khung — (314, −125) với khung 629×250.
    /// </summary>
    public static void DatNeoTheoPivotCha(RectTransform con, RectTransform cha)
    {
        if (con == null || cha == null) return;

        Vector2 p = cha.pivot;
        con.anchorMin = p;
        con.anchorMax = p;
        con.pivot     = GIUA;   // pivot của CHÍNH nó vẫn ở giữa để phóng/xoay quanh tâm
    }

    /// <summary>
    /// Quy một điểm world về toạ độ cục bộ của <paramref name="cha"/>, đúng hệ mà
    /// <c>anchoredPosition</c> dùng KHI con đã được
    /// <see cref="DatNeoTheoPivotCha"/>.
    /// </summary>
    public static Vector2 QuyVeCucBo(RectTransform cha, Vector3 diemWorld, Canvas canvas)
    {
        if (cha == null) return Vector2.zero;

        // ScreenSpaceOverlay ⇒ camera PHẢI là null. Canvas lồng nhau trả về renderMode của
        // canvas gốc nên phép so này đúng cả với MillPopup_Root (overrideSorting).
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector2 manHinh = RectTransformUtility.WorldToScreenPoint(cam, diemWorld);

        Vector2 cucBo;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(cha, manHinh, cam, out cucBo))
            return cucBo;

        return Vector2.zero;
    }
}
