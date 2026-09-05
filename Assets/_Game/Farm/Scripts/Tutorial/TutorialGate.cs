using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Cổng chặn tutorial: tutorial chỉ được chạy tiếp khi KHÔNG còn popup hệ thống nào mở.
///
/// Vì sao cần: các popup như LevelUp hay báo tàu tự bật theo sự kiện / đồng hồ, có thể
/// nhảy ra ngay giữa một bước tutorial. Nếu tutorial cứ chạy, người chơi thấy hai lớp UI
/// chồng nhau và tay chỉ dẫn trỏ vào nút bị popup che.
///
/// Cách dùng phía TutorialManager:
/// <code>
/// yield return TutorialGate.ChoPopupDongHet(AnUITutorial, HienUITutorial);
/// </code>
///
/// Ghi chú kỹ thuật:
/// - Toàn bộ thời gian dùng unscaled (<see cref="Time.unscaledTime"/> /
///   <see cref="WaitForSecondsRealtime"/>) vì tutorial có lúc đặt Time.timeScale = 0.
/// - Poll thưa 0.1s/lần, không kiểm mỗi frame cho nhẹ.
/// - Static thuần, không MonoBehaviour, không giữ state nào ngoài hằng số.
/// </summary>
public static class TutorialGate
{
    /// <summary>Nhịp poll trạng thái popup (giây, unscaled) — thưa vừa đủ nhạy.</summary>
    private const float NHIP_POLL_GIAY = 0.1f;

    /// <summary>Nhịp thở sau khi popup đóng hết, cho anim đóng kịp kết thúc rồi mới hiện lại UI tutorial.</summary>
    private const float NHIP_THO_GIAY = 0.25f;

    /// <summary>Tên hiển thị khi có popup mở nhưng không tra ra được tên cụ thể.</summary>
    private const string TEN_KHONG_RO = "khong-ro";

    /// <summary>Có popup hệ thống nào đang mở không.</summary>
    public static bool CoPopupDangMo()
    {
        // Scene chưa dựng xong (Instance chưa Awake) thì coi như chưa có popup nào —
        // không được ném NullReference làm chết coroutine tutorial.
        PopupManager pm = PopupManager.Instance;
        if (pm == null) return false;

        return pm.IsAnyPopupOpen();
    }

    /// <summary>Tên popup đang mở, chuỗi rỗng nếu không có. Dùng để log.</summary>
    public static string TenPopupDangMo()
    {
        if (PopupManager.Instance == null) return string.Empty;

        string ten = PopupManager.TenPopupDangMo();
        return string.IsNullOrEmpty(ten) ? string.Empty : ten;
    }

    /// <summary>
    /// Chờ tới khi KHÔNG còn popup nào mở.
    /// Nếu lúc gọi đã không có popup nào -> trả về ngay, KHÔNG gọi anUI/hienUI (không nhấp nháy).
    /// Nếu có popup: gọi anUI() một lần, chờ popup đóng hết, chờ thêm nhịp thở 0.25s
    /// (WaitForSecondsRealtime) rồi gọi hienUI() một lần.
    /// hetHanGiay: hết hạn thì thoát và vẫn gọi hienUI() để tutorial không kẹt vĩnh viễn.
    /// </summary>
    public static IEnumerator ChoPopupDongHet(Action anUI, Action hienUI, float hetHanGiay = 60f)
    {
        // Đường nhanh: không có popup nào -> không đụng gì tới UI tutorial.
        // Nếu vẫn gọi anUI rồi hienUI ở đây, UI sẽ tắt-bật trong cùng một frame và nháy.
        if (!CoPopupDangMo()) yield break;

        string tenPopup = LayTenAnToan();
        Debug.Log($"[TutorialGate] Tạm dừng — popup '{tenPopup}' đang mở.");

        if (anUI != null) anUI.Invoke();

        float mocBatDau = Time.unscaledTime;
        float hanChot   = mocBatDau + Mathf.Max(0f, hetHanGiay);
        bool  hetHan    = false;

        while (CoPopupDangMo())
        {
            if (Time.unscaledTime >= hanChot)
            {
                hetHan = true;
                break;
            }

            // Nhớ tên popup mới nhất còn mở — popup này đóng, popup khác mở lên thì
            // log hết hạn phải nêu đúng thủ phạm cuối cùng.
            string tenHienTai = TenPopupDangMo();
            if (!string.IsNullOrEmpty(tenHienTai)) tenPopup = tenHienTai;

            yield return new WaitForSecondsRealtime(NHIP_POLL_GIAY);
        }

        if (hetHan)
        {
            Debug.LogWarning(
                $"[TutorialGate] Hết hạn {hetHanGiay:0.#}s mà popup '{tenPopup}' vẫn chưa đóng — " +
                "chạy tiếp để tutorial không kẹt vĩnh viễn.");
        }
        else
        {
            // Nhịp thở: chờ anim đóng của popup chạy nốt rồi mới trả UI tutorial về.
            yield return new WaitForSecondsRealtime(NHIP_THO_GIAY);
            Debug.Log($"[TutorialGate] Popup đã đóng — chạy tiếp sau {(Time.unscaledTime - mocBatDau):0.0}s.");
        }

        // Cả hai nhánh (đóng bình thường và hết hạn) đều phải trả UI tutorial về,
        // nếu không tutorial sẽ nằm im với UI đang ẩn.
        if (hienUI != null) hienUI.Invoke();
    }

    /// <summary>Tên popup để log, không bao giờ rỗng (đỡ ra chuỗi log cụt).</summary>
    private static string LayTenAnToan()
    {
        string ten = TenPopupDangMo();
        return string.IsNullOrEmpty(ten) ? TEN_KHONG_RO : ten;
    }
}
