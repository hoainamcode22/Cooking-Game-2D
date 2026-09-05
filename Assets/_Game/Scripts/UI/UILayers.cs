/// <summary>
/// BẢNG LỚP UI CHUẨN CỦA DỰ ÁN — MỘT NƠI DUY NHẤT.
///
/// Mọi Canvas trong game phải lấy sortingOrder từ đây, KHÔNG gõ số tay rải rác
/// trong code/scene nữa. Trước đây mỗi chỗ tự đặt một con số nên sinh ra 4 nhóm
/// Canvas TRÙNG order (100 · 120 · 300 · 400) và một Canvas lạc loài ở 999.
///
/// ─────────────────────────────────────────────────────────────────────────────
/// NGUYÊN TẮC QUAN TRỌNG NHẤT: TUTORIAL NẰM **DƯỚI** POPUP HỆ THỐNG.
/// ─────────────────────────────────────────────────────────────────────────────
/// Lớp phủ hướng dẫn (Tutorial_Canvas) phải nổi TRÊN thế giới, TRÊN HUD và TRÊN
/// các panel thường, để người chơi thấy vùng highlight + mũi tên chỉ dẫn.
/// NHƯNG nó phải nằm **DƯỚI** popup hệ thống: khi game bật một popup thật
/// (lên cấp, hết năng lượng, xác nhận mua, mất kết nối…) thì popup đó là thứ
/// người chơi BẮT BUỘC phải xử lý trước — lớp phủ tutorial mà đè lên trên sẽ
/// che nút bấm của popup và làm người chơi kẹt cứng.
///
/// Vì vậy bảng số phải thoả: HUD  &lt;  Panel  &lt;  Tutorial  &lt;  Popup.
/// Đó là lý do Tutorial = 250 (KHÔNG phải 350). Con số 350 từng được đề xuất là
/// SAI: 350 &gt; 300 nghĩa là tutorial vẫn đè lên Canvas_Popup, đúng y cái lỗi
/// mà đợt dọn dẹp này muốn diệt. Tutorial_Canvas hiện đang ở 999 — cao hơn mọi
/// thứ trong game — chính là biểu hiện nặng nhất của lỗi đó.
///
/// ─────────────────────────────────────────────────────────────────────────────
/// CÁCH GIÃN SỐ TRONG CÙNG MỘT LỚP
/// ─────────────────────────────────────────────────────────────────────────────
/// Mỗi lớp cách nhau 100 để còn chỗ chèn. Nhiều Canvas cùng một lớp thì cộng
/// thêm bội số của <see cref="BuocTrongLop"/> (10): 200 · 210 · 220 …
/// KHÔNG BAO GIỜ để hai Canvas trùng đúng một con số — thứ tự vẽ khi trùng là
/// không xác định, chạy máy này đúng máy kia sai.
/// </summary>
public static class UILayers
{
    /// <summary>Khoảng cách giữa hai Canvas nằm CÙNG một lớp (200 · 210 · 220…).</summary>
    public const int BuocTrongLop = 10;

    /// <summary>
    /// 0 — Thế giới: Canvas World Space gắn vào cảnh vật (biển tên toà nhà, thanh
    /// tiến trình nổi trên ruộng…). Luôn nằm dưới cùng của mọi UI màn hình.
    /// </summary>
    public const int World = 0;

    /// <summary>
    /// 100 — HUD thường trực: thanh tiền/kim cương/EXP, thanh tab đáy màn hình.
    /// Luôn hiển thị, không bao giờ che popup.
    /// </summary>
    public const int HUD = 100;

    /// <summary>
    /// 200 — Panel/bảng phụ mở từ HUD: menu, kho, bảng tin chợ.
    /// Nằm trên HUD nhưng vẫn DƯỚI tutorial, vì tutorial cần chỉ dẫn được vào
    /// chính các panel này.
    /// </summary>
    public const int Panel = 200;

    /// <summary>
    /// 250 — Lớp phủ hướng dẫn (Tutorial_Canvas).
    /// TRÊN HUD và Panel — để highlight/mũi tên hiện rõ.
    /// DƯỚI Popup — để popup hệ thống luôn cắt ngang được tutorial.
    /// Đặt ở 250 (giữa Panel 200 và Popup 300) để cả hai điều kiện cùng đúng.
    /// </summary>
    public const int Tutorial = 250;

    /// <summary>
    /// 300 — Popup hệ thống tiêu chuẩn: Canvas_Popup và toàn bộ popup con của nó
    /// (shop, xác nhận, thông báo…). Đè lên tutorial theo đúng nguyên tắc trên.
    /// </summary>
    public const int Popup = 300;

    /// <summary>
    /// 400 — Popup ưu tiên cao: phần thưởng, lên cấp, tàu du lịch, máy xay…
    /// Những popup phải nổi trên cả popup thường.
    ///
    /// GHI CHÚ ĐẶT TÊN: bản nháp đầu tiên gọi lớp này là "PopupTrenTutorial".
    /// Tên đó nay đã vô nghĩa — sau khi hạ tutorial xuống 250 thì MỌI popup
    /// (300 và 400) đều đã nằm trên tutorial. Nên đổi thành PopupCaoCap cho đúng
    /// ý nghĩa thật: "popup ưu tiên cao hơn popup thường".
    /// </summary>
    public const int PopupCaoCap = 400;

    /// <summary>
    /// 9999 — Màn chuyển cảnh / fade đen / màn hình loading.
    /// Cao nhất tuyệt đối: không thứ gì được phép ló ra trong lúc chuyển cảnh.
    /// </summary>
    public const int ChuyenCanh = 9999;

    /// <summary>Danh sách lớp, sắp sẵn theo order tăng dần — dùng cho <see cref="MoTa"/> và cho tool.</summary>
    private static readonly (int Order, string Ten)[] BangLop =
    {
        (World,       "World"),
        (HUD,         "HUD"),
        (Panel,       "Panel"),
        (Tutorial,    "Tutorial"),
        (Popup,       "Popup"),
        (PopupCaoCap, "PopupCaoCap"),
        (ChuyenCanh,  "ChuyenCanh"),
    };

    /// <summary>
    /// Trả về tên lớp GẦN NHẤT (lớp có mốc lớn nhất mà vẫn &lt;= <paramref name="order"/>),
    /// kèm phần lệch nếu không khớp đúng mốc. Dùng cho log và cho Editor tool.
    ///
    /// Ví dụ: 100 → "HUD"; 210 → "Panel +10"; 999 → "PopupCaoCap +599"; -5 → "duoi World (-5)".
    /// </summary>
    public static string MoTa(int order)
    {
        if (order < BangLop[0].Order)
            return $"duoi {BangLop[0].Ten} ({order})";

        // Duyệt ngược để lấy mốc lớn nhất còn nhỏ hơn hoặc bằng order.
        for (int i = BangLop.Length - 1; i >= 0; i--)
        {
            if (order < BangLop[i].Order) continue;

            int lech = order - BangLop[i].Order;
            return lech == 0 ? BangLop[i].Ten : $"{BangLop[i].Ten} +{lech}";
        }

        return $"khong xac dinh ({order})";
    }

    /// <summary>True nếu <paramref name="order"/> rơi đúng vào một mốc lớp chuẩn.</summary>
    public static bool LaMocChuan(int order)
    {
        for (int i = 0; i < BangLop.Length; i++)
            if (BangLop[i].Order == order) return true;

        return false;
    }
}
