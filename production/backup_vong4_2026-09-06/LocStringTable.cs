using System.Collections.Generic;

/// <summary>
/// BẢNG DỊCH VIỆT → ANH. Khoá là CHÍNH CÂU TIẾNG VIỆT trong game.
/// ══════════════════════════════════════════════════════════════════════════════
///
/// ĐỢT 1 (Sếp duyệt 04/09): chỉ dịch UI người chơi gặp nhiều nhất — HUD, Cài đặt, Hồ sơ,
/// Shop, Kho, Lên cấp, Nhiệm vụ, Bếp, Tàu, Tutorial.
/// ĐỢT SAU: tên món ăn, tên nguyên liệu, 315 tên đơn hàng (OrderNameBank), mô tả nhiệm vụ.
///
/// LUẬT THÊM CÂU MỚI:
///   • Khoá phải khớp TỪNG KÝ TỰ với câu trong code/scene, kể cả dấu và khoảng trắng.
///   • Chưa có câu nào đó ⇒ game tự hiện tiếng Việt, KHÔNG lỗi. Cứ bổ sung dần.
///   • Không xoá khoá cũ khi chưa chắc không còn ai dùng.
///
/// [Localization]
/// </summary>
public static class LocStringTable
{
    public static readonly Dictionary<string, string> EN = new Dictionary<string, string>
    {
        // ── HUD & điều hướng chính ──────────────────────────────────────────
        { "Cửa hàng",        "Shop" },
        { "CỬA HÀNG",        "SHOP" },
        { "Kho",             "Storage" },
        { "KHO",             "STORAGE" },
        { "Bảng tin chợ",    "Market Board" },
        { "BẢNG TIN CHỢ",    "MARKET BOARD" },
        { "Nấu ăn",          "Cooking" },
        { "NẤU ĂN",          "COOKING" },
        { "Về nông trại",    "Back to Farm" },
        { "VỀ NÔNG TRẠI",    "BACK TO FARM" },
        { "Sửa",             "Edit" },
        { "SỬA",             "EDIT" },

        // ── Cài đặt ─────────────────────────────────────────────────────────
        { "CÀI ĐẶT",         "SETTINGS" },
        { "Cài đặt",         "Settings" },
        { "Âm thanh game",   "Music" },
        { "Âm thanh VFX",    "Sound Effects" },
        { "Ngôn ngữ",        "Language" },
        { "Tiếng Việt",      "Tiếng Việt" },   // tên ngôn ngữ giữ nguyên bản ngữ — chuẩn quốc tế
        { "English",         "English" },
        { "BẬT",             "ON" },
        { "TẮT",             "OFF" },
        { "ĐÓNG",            "CLOSE" },
        { "Đóng",            "Close" },
        { "CHƠI LẠI TỪ ĐẦU", "RESET PROGRESS" },
        { "Chơi lại từ đầu", "Reset Progress" },
        { "Xoá dữ liệu",     "Reset Data" },
        { "Bạn có chắc muốn xoá toàn bộ dữ liệu và chơi lại từ đầu không?", "Are you sure you want to reset all data and restart from Level 1?" },

        // ── Hồ sơ người chơi ────────────────────────────────────────────────
        { "HỒ SƠ",           "PROFILE" },
        { "Hồ sơ",           "Profile" },
        { "Tên nông trại",   "Farm Name" },
        { "Cấp độ",          "Level" },
        { "Cấp",             "Lv" },
        { "Chọn avatar",     "Choose Avatar" },
        { "LƯU HỒ SƠ",       "SAVE PROFILE" },
        { "Sức chứa kho",    "Storage Capacity" },
        { "Điểm nấu ăn",     "Recipes Cooked" },
        { "Tiền vàng",       "Gold" },
        { "Nhiệm vụ",        "Quests" },
        { "đã xong",         "done" },
        { "ô",               "slots" },
        { "món",             "dishes" },

        // ── Lên cấp ─────────────────────────────────────────────────────────
        { "LÊN CẤP!",        "LEVEL UP!" },
        { "Lên cấp!",        "Level Up!" },
        { "Bắt đầu nào",     "Let's Go" },
        { "BẮT ĐẦU NÀO",     "LET'S GO" },
        { "MỚI",             "NEW" },
        { "Phần thưởng",     "Rewards" },
        { "Đã mở khoá",      "Unlocked" },

        // ── Bảng đơn hàng ───────────────────────────────────────────────────
        { "BẢNG ĐƠN HÀNG",   "ORDER BOARD" },
        { "GIAO HÀNG",       "DELIVER" },
        { "Giao hàng",       "Deliver" },

        // ── Bếp ─────────────────────────────────────────────────────────────
        { "ĐƠN CỦA KHÁCH",   "CUSTOMER ORDERS" },
        { "BẢNG CÔNG THỨC",  "RECIPE BOOK" },
        { "Tất cả",          "All" },
        { "Dễ",              "Easy" },
        { "Vừa",             "Medium" },
        { "Khó",             "Hard" },
        { "Nguyên liệu",     "Ingredients" },
        { "Gia vị",          "Seasonings" },
        { "Bàn sơ chế",      "Prep Table" },
        { "Trình bày",       "Plating" },
        { "Bỏ hết",          "Clear All" },
        { "VÀO KHO",         "TO STORAGE" },
        { "Lò chưa nhóm",    "Oven not lit" },
        { "CHỌN NGUYÊN LIỆU","PICK INGREDIENTS" },
        { "chạm khay bên dưới", "tap the tray below" },
        { "MÓN HÔM NAY (+vàng)", "TODAY'S SPECIALS (+gold)" },

        // ── Tàu chở hàng ────────────────────────────────────────────────────
        { "TÀU CHỞ HÀNG",    "CARGO TRAIN" },
        { "NẠP HÀNG",        "LOAD CARGO" },
        { "GA HÀNG",         "STATION" },
        { "THÊM HÀNG",       "ADD GOODS" },
        { "NẠP TẤT CẢ",      "LOAD ALL" },
        { "Trong kho:",      "In storage:" },
        { "Chạm vào toa để nạp hàng — nạp đủ các toa yêu cầu, tàu sẽ khởi hành!",
          "Tap a wagon to load it — fill every wagon and the train departs!" },

        // ── Chung ───────────────────────────────────────────────────────────
        { "Xác nhận",        "Confirm" },
        { "Huỷ",             "Cancel" },
        { "Hủy",             "Cancel" },
        { "Mua",             "Buy" },
        { "Bán",             "Sell" },
        { "Tiếp tục",        "Continue" },
        { "Bỏ qua",          "Skip" },
        { "Không đủ vàng!",  "Not enough gold!" },
        { "Kho đã đầy!",     "Storage is full!" },
    };
}
