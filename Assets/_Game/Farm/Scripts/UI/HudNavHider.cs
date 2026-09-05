using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ẨN / HIỆN HÀNG NÚT ĐIỀU HƯỚNG HUD (<c>Canvas_HUD/BottomLeft_Nav_Group</c>) THEO ĐẾM THAM CHIẾU.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO CẦN: hàng nút Tab_Shop / Tab_Warehouse / Tab_Market / Tab_Cooking nằm góc trái-dưới
/// (x 31→722, y 22→180 trên 1920×1080). Khay hạt giống <c>Popup_seed</c> / khay hoa <c>Popup_hoa</c>
/// (Canvas_Popup, order 300) khi mở trải ngang đáy màn hình y 0→240 ⇒ ĐÈ LÊN 4 nút này; khay
/// bán trong suốt nên nút vẫn lộ ra và có thể NUỐT TAP của người chơi. Card hội thoại tutorial
/// cũng cần làm mờ hàng nút để người chơi không bấm lạc sang Shop/Kho giữa bước hướng dẫn.
///
/// CÁCH DÙNG: mỗi bên muốn ẩn gọi <see cref="An"/> với <c>this</c> làm "chủ sở hữu", xong việc gọi
/// <see cref="Hien"/>. Nhiều chủ cùng ẩn ⇒ alpha hiệu lực = MIN các alpha; chủ CUỐI CÙNG nhả ra
/// ⇒ alpha 1, nhận raycast lại. Không ai phải biết đến ai (khay hạt + card tutorial chồng nhau vẫn đúng).
///
/// KHÔNG phụ thuộc TutorialManager hay bất kỳ manager nào — chỉ tìm object theo tên, thêm
/// <see cref="CanvasGroup"/> nếu thiếu. Object bị huỷ (đổi scene) ⇒ tự dò lại lần gọi sau.
/// </summary>
public static class HudNavHider
{
    private const string TenNhomNav   = "BottomLeft_Nav_Group";
    private const string TenCanvasHud = "Canvas_HUD";

    // Chủ sở hữu → alpha mà chủ đó muốn. Dictionary so theo tham chiếu (UnityEngine.Object dùng instanceID).
    private static readonly Dictionary<object, float> _chuSoHuu = new Dictionary<object, float>();
    private static readonly List<object> _bufXoa = new List<object>();

    private static CanvasGroup _cg;

    /// <summary>Hàng nút HUD hiện đang bị ẩn/mờ (còn ít nhất một chủ giữ) hay không.</summary>
    public static bool DangAn
    {
        get
        {
            DonChuDaChet();
            return _chuSoHuu.Count > 0;
        }
    }

    /// <summary>
    /// Yêu cầu ẩn hàng nút. <paramref name="alpha"/> = 0 là ẩn hẳn, 0.35 là làm mờ.
    /// Gọi lại với cùng chủ chỉ cập nhật alpha (không nhân đôi tham chiếu).
    /// Dưới 0.99 thì <c>blocksRaycasts</c> = <c>interactable</c> = false ⇒ nút không nhận tap.
    /// </summary>
    public static void An(object chuSoHuu, float alpha = 0f)
    {
        if (chuSoHuu == null) return;
        _chuSoHuu[chuSoHuu] = Mathf.Clamp01(alpha);
        ApDung();
    }

    /// <summary>Chủ này không cần ẩn nữa. Chủ cuối cùng nhả ⇒ hàng nút về alpha 1, nhận raycast.</summary>
    public static void Hien(object chuSoHuu)
    {
        if (chuSoHuu == null) return;
        if (!_chuSoHuu.Remove(chuSoHuu)) return;   // chưa từng ẩn ⇒ không đụng gì (an toàn khi OnDisable gọi thừa)
        ApDung();
    }

    // ═══════════════════════════════════════════════════════════════════════

    private static void ApDung()
    {
        DonChuDaChet();

        CanvasGroup cg = LayCanvasGroup();
        if (cg == null) return;   // HUD chưa có / scene khác — không có gì để ẩn, im lặng

        float alpha = 1f;
        foreach (var kv in _chuSoHuu)
            if (kv.Value < alpha) alpha = kv.Value;

        bool nhanTap = alpha >= 0.99f;
        cg.alpha          = alpha;
        cg.blocksRaycasts = nhanTap;
        cg.interactable   = nhanTap;
    }

    /// <summary>Chủ là UnityEngine.Object đã bị Destroy (đổi scene, huỷ popup) mà quên gọi Hien ⇒ tự loại.</summary>
    private static void DonChuDaChet()
    {
        if (_chuSoHuu.Count == 0) return;

        _bufXoa.Clear();
        foreach (var kv in _chuSoHuu)
        {
            // So với null qua toán tử của UnityEngine.Object để bắt cả trạng thái "đã Destroy".
            if (kv.Key is Object uo && uo == null) _bufXoa.Add(kv.Key);
        }
        for (int i = 0; i < _bufXoa.Count; i++) _chuSoHuu.Remove(_bufXoa[i]);
        _bufXoa.Clear();
    }

    /// <summary>Tìm (và cache) CanvasGroup trên BottomLeft_Nav_Group; thêm mới nếu chưa có.</summary>
    private static CanvasGroup LayCanvasGroup()
    {
        if (_cg != null) return _cg;   // == null cũng bắt được trường hợp object đã bị huỷ ⇒ dò lại

        GameObject nav = TimNhomNav();
        if (nav == null) return null;

        _cg = nav.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = nav.AddComponent<CanvasGroup>();
        return _cg;
    }

    private static GameObject TimNhomNav()
    {
        // 1) Nhanh nhất — nav bình thường luôn active nên GameObject.Find đủ dùng.
        GameObject go = GameObject.Find(TenNhomNav);
        if (go != null) return go;

        // 2) Nav đang tắt (ai đó SetActive(false)) ⇒ quét đệ quy dưới Canvas_HUD, kể cả con inactive.
        GameObject hud = GameObject.Find(TenCanvasHud);
        if (hud == null) return null;

        foreach (Transform t in hud.GetComponentsInChildren<Transform>(true))
            if (t.name == TenNhomNav) return t.gameObject;

        return null;
    }

    /// <summary>
    /// Editor tắt Domain Reload ⇒ static còn sống giữa 2 lần Play. Xoá sạch để lần Play sau
    /// không kế thừa chủ sở hữu ma từ phiên trước.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTinhTrang()
    {
        _chuSoHuu.Clear();
        _bufXoa.Clear();
        _cg = null;
    }
}
