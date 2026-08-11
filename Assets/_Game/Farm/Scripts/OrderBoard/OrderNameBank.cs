using System.Collections.Generic;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  KHO TÊN ĐƠN HÀNG — 315 tên, 7 chủ đề (mục 5.2 file TEAM)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO tên đơn quan trọng đến mức có hẳn một file riêng:
/// nội dung đơn thì lặp lại không tránh được — cả game chỉ có ~50 vật phẩm giao được,
/// nên "5 Lúa" sẽ hiện ra hàng trăm lần trong một lượt chơi. Thứ làm người chơi thấy
/// đơn này KHÁC đơn kia là CÁI TÊN. Một dòng chữ rẻ hơn mọi hệ thống nội dung khác
/// mà đổi lại được cảm giác "làng này có người sống trong đó".
///
/// VÌ SAO không phải 300 file `.asset`:
/// dự án đã dính đúng cái bẫy đó với 218 asset nhiệm vụ — không ai mở nổi để sửa,
/// một nửa hỏng mà không ai biết. Chuỗi nằm trong code thì sửa bằng Ctrl+F, và
/// review được bằng mắt trong một lần cuộn.
///
/// VÌ SAO chọn chủ đề theo NỘI DUNG đơn chứ không bốc ngẫu nhiên:
/// một đơn toàn hoa mà tên là "Bữa cơm nhà bác Heo" thì người chơi thấy ngay là
/// máy sinh bừa, và mọi cái tên còn lại lập tức mất giá trị. Thà ít chủ đề mà đúng.
/// </summary>
public static class OrderNameBank
{
    /// <summary>
    /// 12 mã khách hàng. DEV-B đặt tên sprite avatar theo đúng các mã này là map được ngay;
    /// chưa có art thì `OrderBoardIconResolver.TintFromId` cho mỗi khách một màu riêng.
    /// </summary>
    public static readonly string[] CustomerIds =
    {
        "heo", "cun", "meo", "tho", "gau", "cuu",
        "bo",  "vit", "ga",  "soc", "nai", "chuot",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  1 · BỮA CƠM GIA ĐÌNH — nông sản cơ bản
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] BuaComGiaDinh =
    {
        "Bữa cơm nhà bác Heo",
        "Cơm chiều nhà Gấu",
        "Mâm cơm ngày mùa",
        "Bữa trưa vội của bác Trâu",
        "Cơm nắm mang ra đồng",
        "Nồi canh chiều thứ Bảy",
        "Bữa cơm đoàn viên",
        "Mâm cơm đãi ông bà",
        "Cơm nhà, rau vườn",
        "Bữa sáng nhà Thỏ",
        "Cơm tối nhà cô Cừu",
        "Giỏ rau cho bà ngoại",
        "Bữa cơm sau buổi cày",
        "Mâm cơm ngày rằm",
        "Nồi cháo cho bé Sóc",
        "Bữa cơm thợ gặt",
        "Rổ rau nhà bên",
        "Cơm hộp cho bố đi làm",
        "Bữa cơm ngày mưa",
        "Mâm cơm cuối tuần",
        "Cơm dẻo canh ngọt",
        "Bữa cơm nhà cô Vịt",
        "Giỏ củ quả nhà Chuột",
        "Bữa cơm mừng con biết đi",
        "Nồi canh rau tập tàng",
        "Cơm chiều bên bếp lửa",
        "Bữa cơm nhà anh Nai",
        "Mâm cơm tiễn khách",
        "Rổ rau sau vườn",
        "Bữa cơm ngày gió mùa",
        "Cơm trưa nhà bác Ngan",
        "Bữa cơm bà cháu",
        "Mâm cơm ngày giỗ",
        "Nồi canh cà chua chiều muộn",
        "Bữa cơm thợ xây",
        "Giỏ rau cho lớp mẫu giáo",
        "Cơm chiều nhà chị Ngỗng",
        "Bữa cơm mừng lợp lại mái nhà",
        "Mâm cơm ngày trở trời",
        "Rổ rau tươi buổi sớm",
        "Bữa cơm nhà cậu Cún",
        "Cơm tối muộn nhà bác Bò",
        "Mâm cơm ngày trăng tròn",
        "Bữa cơm sau cơn mưa rào",
        "Nồi canh chiều nhà Mèo",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  2 · TIỆC MỪNG — nhiều món, giá trị cao
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] TiecMung =
    {
        "Tiệc mừng nhà mới",
        "Liên hoan cuối mùa",
        "Tiệc thôi nôi bé Cún",
        "Tiệc mừng thọ cụ Rùa",
        "Tiệc cưới nhà Sóc",
        "Liên hoan mừng được mùa",
        "Tiệc mừng bé Mèo vào lớp một",
        "Bữa tiệc đêm hội làng",
        "Tiệc mừng thuyền về bến",
        "Liên hoan đội gặt",
        "Tiệc mừng bác Gấu khỏi ốm",
        "Tiệc tất niên xóm Đông",
        "Tiệc mừng nhà Thỏ sinh đôi",
        "Liên hoan mừng cầu mới",
        "Tiệc mừng ngày lập làng",
        "Tiệc đầy tháng bé Vịt",
        "Liên hoan mừng lúa vào bồ",
        "Tiệc mừng bác Trâu về hưu",
        "Bữa tiệc dưới gốc đa",
        "Tiệc mừng chợ mới khai trương",
        "Liên hoan mừng đàn gà nở",
        "Tiệc mừng cô Cừu thắng hội thi",
        "Tiệc mừng trường làng khai giảng",
        "Bữa tiệc đêm rằm tháng Tám",
        "Liên hoan mừng đường mới đổ",
        "Tiệc mừng nhà Nai dựng cổng",
        "Tiệc chia tay chú Ngựa lên tỉnh",
        "Liên hoan mừng giếng làng có nước",
        "Tiệc mừng bé Chuột biết đọc",
        "Bữa tiệc mừng mùa hoa nở rộ",
        "Tiệc mừng nhà Ngỗng đón dâu",
        "Liên hoan hội đua thuyền",
        "Tiệc mừng đàn bò về chuồng đủ",
        "Tiệc mừng bác Heo trúng mùa",
        "Bữa tiệc dưới ánh đèn lồng",
        "Liên hoan mừng kho thóc đầy",
        "Tiệc mừng cụ Voi tròn trăm tuổi",
        "Tiệc mừng anh Cáo lấy vợ",
        "Liên hoan mừng vụ đông thắng lợi",
        "Tiệc mừng đội bóng làng vô địch",
        "Bữa tiệc mừng bé Nhím vào đội",
        "Tiệc mừng nhà Sóc lợp ngói mới",
        "Liên hoan mừng máy xay về làng",
        "Tiệc mừng ngày hội hoa xuân",
        "Bữa tiệc mừng cầu vồng sau mưa",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  3 · QUÁN ĂN — đơn có món nấu
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] QuanAn =
    {
        "Đơn quán Cô Ba",
        "Bếp nhà hàng Bốn Mùa",
        "Quán cơm Bà Tám",
        "Bếp quán Gió Đồng",
        "Đơn quán phở Ông Gấu",
        "Nhà hàng Vườn Trăng",
        "Quán ăn Bên Cầu",
        "Bếp trưởng quán Hạt Dẻ",
        "Đơn quán nhậu Chú Cáo",
        "Quán cơm bình dân đầu chợ",
        "Bếp nhà hàng Sao Mai",
        "Quán bún Cô Vịt",
        "Đơn bếp tiệc cưới",
        "Quán cháo khuya Bác Cú",
        "Nhà hàng Đồng Xanh",
        "Bếp quán Lửa Hồng",
        "Đơn quán ăn ga tàu",
        "Quán cơm thợ mỏ",
        "Bếp nhà hàng Mây Trắng",
        "Quán lẩu Nhà Gấu",
        "Đơn bếp trường nội trú",
        "Quán ăn vặt Cổng Trường",
        "Bếp nhà hàng Suối Reo",
        "Quán nướng Ba Con Sóc",
        "Đơn bếp bệnh viện làng",
        "Quán chay Sen Nở",
        "Bếp nhà hàng Hoàng Hôn",
        "Quán cơm gà Chị Mèo",
        "Đơn bếp hội làng",
        "Quán mì Bến Đò",
        "Bếp nhà hàng Ngọn Đồi",
        "Quán ăn khuya Xóm Chài",
        "Đơn bếp trại hè",
        "Quán bánh Cô Thỏ",
        "Bếp nhà hàng Trăng Non",
        "Quán cơm niêu Bác Trâu",
        "Đơn bếp đội cứu hộ",
        "Quán canh chua Dì Ngỗng",
        "Bếp nhà hàng Gác Chuông",
        "Quán xào Chú Chuột",
        "Đơn bếp chợ đêm",
        "Quán ăn sáng Ngã Ba",
        "Bếp nhà hàng Rừng Thông",
        "Quán cỗ chay Bà Rùa",
        "Đơn bếp lễ hội mùa gặt",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  4 · BÓ HOA — đơn toàn hoa
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] BoHoa =
    {
        "Bó hoa tặng mẹ",
        "Hoa cưới nhà Thỏ",
        "Giỏ hoa ngày lễ",
        "Bó hoa mừng sinh nhật",
        "Hoa cho hiệu ảnh đầu làng",
        "Giỏ hoa bàn tiệc",
        "Bó hoa tiễn bạn đi xa",
        "Hoa cài áo cô dâu",
        "Giỏ hoa cho phòng khám",
        "Bó hoa mừng khai trương",
        "Hoa trang trí sân khấu hội làng",
        "Bó hoa tặng cô giáo",
        "Giỏ hoa đặt bàn thờ",
        "Hoa cho quán cà phê Góc Phố",
        "Bó hoa xin lỗi",
        "Giỏ hoa mừng em bé chào đời",
        "Hoa cho tiệm bánh Cô Sóc",
        "Bó hoa ngày của mẹ",
        "Hoa kết cổng cưới",
        "Giỏ hoa gửi bưu điện",
        "Bó hoa tặng bác sĩ thú y",
        "Hoa cho lễ tốt nghiệp",
        "Giỏ hoa treo hiên nhà",
        "Bó hoa hẹn hò lần đầu",
        "Hoa cho gian hàng chợ phiên",
        "Bó hoa mừng nhà Nai tân gia",
        "Giỏ hoa cho đám rước đèn",
        "Hoa trang trí xe hoa",
        "Bó hoa cảm ơn hàng xóm",
        "Giỏ hoa cho lớp học vẽ",
        "Hoa cho lễ hội mùa xuân",
        "Bó hoa tặng người thắng cuộc",
        "Giỏ hoa bên cửa sổ",
        "Hoa cho tiệm cắt tóc Chị Mèo",
        "Bó hoa ngày kỷ niệm cưới",
        "Giỏ hoa gửi lên tỉnh",
        "Hoa cho buổi hoà nhạc làng",
        "Bó hoa chúc mau khỏi ốm",
        "Giỏ hoa cho quán trà Bà Rùa",
        "Hoa kết vòng đội đầu",
        "Bó hoa mừng em Sóc đạt giải",
        "Giỏ hoa cho nhà nguyện nhỏ",
        "Hoa cho tiệm ảnh cưới Ánh Trăng",
        "Bó hoa tặng bác đưa thư",
        "Giỏ hoa đón khách phương xa",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  5 · CHỢ PHIÊN — hỗn hợp, số lượng lớn
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] ChoPhien =
    {
        "Hàng chợ phiên",
        "Gánh hàng rong",
        "Sạp rau chợ sớm",
        "Chuyến hàng lên tỉnh",
        "Gánh hàng ra bến",
        "Sạp hàng cuối chợ",
        "Chuyến xe thồ đầu ngày",
        "Hàng cho phiên chợ Tết",
        "Gánh hàng qua đò",
        "Sạp hàng nhà Chuột",
        "Chuyến hàng đi chợ huyện",
        "Hàng gửi tàu sớm",
        "Gánh hàng bà Ngỗng",
        "Sạp hàng góc chợ Đông",
        "Chuyến hàng đường xa",
        "Hàng cho chợ nổi",
        "Gánh hàng chiều muộn",
        "Sạp rau củ nhà Thỏ",
        "Chuyến hàng vượt đèo",
        "Hàng cho hội chợ mùa gặt",
        "Gánh hàng theo mẹ",
        "Sạp hàng dưới gốc đa",
        "Chuyến hàng ra cảng",
        "Hàng gửi xe khách",
        "Gánh hàng mưa phùn",
        "Sạp hàng phiên chợ rằm",
        "Chuyến hàng cho làng bên",
        "Hàng cho quầy hợp tác xã",
        "Gánh hàng qua cầu tre",
        "Sạp hàng nhà bác Heo",
        "Chuyến hàng đêm",
        "Hàng cho chợ đầu mối",
        "Gánh hàng đội mưa",
        "Sạp hàng chợ Tây",
        "Chuyến hàng lên núi",
        "Hàng cho phiên chợ cuối năm",
        "Gánh hàng sương sớm",
        "Sạp hàng nhà chị Cừu",
        "Chuyến hàng theo đoàn",
        "Hàng cho chợ ven sông",
        "Gánh hàng ngày nắng gắt",
        "Sạp hàng phiên đầu tháng",
        "Chuyến hàng gửi tàu hoả",
        "Hàng cho chợ phiên vùng cao",
        "Gánh hàng của bà cụ Rùa",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  6 · TRANG TRẠI BẠN — sản phẩm chăn nuôi
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] TrangTraiBan =
    {
        "Trại gà nhà Vịt cần hàng",
        "Chuồng bò bác Trâu cần tiếp tế",
        "Trại heo nhà Gấu đặt hàng",
        "Nông trại Đồi Cỏ cần hàng",
        "Chuồng cừu nhà Thỏ",
        "Trại vịt bên sông",
        "Nhà kho trang trại Sáng Sớm",
        "Trại ong bác Ngựa",
        "Chuồng gà mái đẻ nhà Sóc",
        "Nông trại Bốn Mùa gọi hàng",
        "Trại bò sữa Cô Mèo",
        "Chuồng ngựa nhà Cáo",
        "Trại gia cầm đầu làng",
        "Nông trại Sương Mai",
        "Trại thỏ nhà bé Chuột",
        "Chuồng dê trên sườn đồi",
        "Trại giống nhà bác Ngỗng",
        "Nông trại Nắng Vàng",
        "Trại gà tre nhà Nhím",
        "Chuồng bò cuối xóm",
        "Trại vịt cỏ ven đầm",
        "Nông trại Chân Đồi",
        "Trại lợn nái nhà Bò",
        "Chuồng gà nhà cô Nai",
        "Trại sữa Gió Mát",
        "Nông trại Bờ Suối",
        "Trại chăn nuôi hợp tác xã",
        "Chuồng ngan nhà Rùa",
        "Trại bò thịt Đồng Xa",
        "Nông trại Sao Đêm",
        "Trại gà ta nhà chú Cún",
        "Chuồng heo con mới tách mẹ",
        "Trại thỏ giống miền ngược",
        "Nông trại Mưa Rào",
        "Trại cừu lông dài",
        "Chuồng bê con nhà bác Voi",
        "Trại gia súc bên kia đồi",
        "Nông trại Cỏ Non",
        "Trại vịt đẻ nhà chị Ngỗng",
        "Chuồng gà làng bên",
        "Trại bò nghé nhà Trâu con",
        "Nông trại Hạt Sương",
        "Trại dê núi bác Sơn Dương",
        "Chuồng lợn mán nhà Nhím",
        "Nông trại Bình Minh cần hàng",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  7 · ĐƠN GẤP — thưởng cao, khó
    // ══════════════════════════════════════════════════════════════════════
    private static readonly string[] DonGap =
    {
        "Đơn gấp — trả hậu",
        "Khách quý đặt riêng",
        "Đơn hoả tốc trong ngày",
        "Đặt gấp cho đoàn khách",
        "Đơn khẩn của quan huyện",
        "Khách sang đặt vội",
        "Đơn gấp cho tiệc tối nay",
        "Đặt riêng, trả gấp đôi",
        "Đơn hoả tốc chuyến tàu 5 giờ",
        "Khách lạ trả tiền trước",
        "Đơn gấp cứu bếp cháy hàng",
        "Đặt vội cho đám cưới chiều mai",
        "Đơn ưu tiên của hội đồng làng",
        "Khách quen đặt gấp",
        "Đơn gấp — thiếu hàng đột xuất",
        "Đặt riêng cho nhà giàu xóm Trên",
        "Đơn hoả tốc gửi lên tỉnh",
        "Khách phương xa trả hậu",
        "Đơn gấp cho đoàn hát rong",
        "Đặt vội trước giờ chợ tan",
        "Đơn khẩn cấp — bù cho chuyến hỏng",
        "Khách quý từ kinh thành",
        "Đơn gấp cho lễ khai trương sáng mai",
        "Đặt riêng — không hỏi giá",
        "Đơn hoả tốc trước cơn bão",
        "Khách sộp đặt nguyên lô",
        "Đơn gấp cho bếp bệnh viện",
        "Đặt vội cho đoàn cứu trợ",
        "Đơn ưu tiên — trả bằng vàng ròng",
        "Khách bí ẩn đặt hàng đêm",
        "Đơn gấp cho tiệc chia tay",
        "Đặt riêng của chủ tiệm lớn",
        "Đơn hoả tốc — xe đã chờ ngoài cổng",
        "Khách quý đặt trọn gói",
        "Đơn gấp trước giờ tàu chạy",
        "Đặt vội cho hội thi nấu ăn",
        "Đơn khẩn — làng bên mất mùa",
        "Khách sang trọng đặt hàng hiếm",
        "Đơn gấp cho đoàn làm phim",
        "Đặt riêng — giữ kín tên khách",
        "Đơn hoả tốc trước giờ đóng chợ",
        "Khách quý đi cùng đoàn sứ",
        "Đơn gấp — bếp trưởng gọi ba lần",
        "Đặt vội cho lễ cầu mưa",
        "Đơn ưu tiên của nhà buôn lớn",
    };

    // ══════════════════════════════════════════════════════════════════════
    //  TRA CỨU
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bao nhiêu tên vừa dùng thì cấm dùng lại. Bảng có 9 ô nên phải lớn hơn 9 —
    /// nếu bằng 9 thì lấp đầy bảng xong là hàng cấm hết hiệu lực, đơn thứ 10 có thể
    /// trùng tên với đơn còn đang treo ngay trước mắt. 24 ≈ hai vòng bảng rưỡi.
    /// </summary>
    private const int RecentMemory = 24;

    private static readonly List<string> Recent = new List<string>(RecentMemory);

    /// <summary>Tổng số tên trong kho — Editor tool và test đếm bằng hàm này.</summary>
    public static int TotalNameCount =>
        BuaComGiaDinh.Length + TiecMung.Length + QuanAn.Length + BoHoa.Length +
        ChoPhien.Length + TrangTraiBan.Length + DonGap.Length;

    private static string[] PoolOf(OrderTheme theme)
    {
        switch (theme)
        {
            case OrderTheme.BuaComGiaDinh: return BuaComGiaDinh;
            case OrderTheme.TiecMung:      return TiecMung;
            case OrderTheme.QuanAn:        return QuanAn;
            case OrderTheme.BoHoa:         return BoHoa;
            case OrderTheme.ChoPhien:      return ChoPhien;
            case OrderTheme.TrangTraiBan:  return TrangTraiBan;
            case OrderTheme.DonGap:        return DonGap;
            default:                       return BuaComGiaDinh;
        }
    }

    /// <summary>
    /// Bốc một tên thuộc chủ đề, tránh những tên vừa dùng.
    ///
    /// Cách tránh trùng: thử tối đa 12 lần rồi CHẤP NHẬN tên trùng.
    /// Cố ý không quét cả mảng tìm tên chưa dùng: mỗi chủ đề 45 tên mà hàng cấm chỉ 24,
    /// nên xác suất 12 lần đều trượt là (24/45)^12 ≈ 0.03% — nhỏ đến mức không đáng đổi
    /// lấy một vòng lặp có thể chạy dài. Và kể cả có trùng thì đó là phiền, không phải lỗi.
    /// </summary>
    public static string PickTitle(OrderTheme theme, System.Random rng)
    {
        string[] pool = PoolOf(theme);
        if (pool == null || pool.Length == 0) return "Đơn hàng";

        string picked = pool[rng.Next(pool.Length)];
        for (int attempt = 0; attempt < 12 && Recent.Contains(picked); attempt++)
            picked = pool[rng.Next(pool.Length)];

        Remember(picked);
        return picked;
    }

    public static string PickCustomerId(System.Random rng) => CustomerIds[rng.Next(CustomerIds.Length)];

    private static void Remember(string title)
    {
        Recent.Add(title);
        if (Recent.Count > RecentMemory) Recent.RemoveAt(0);
    }

    /// <summary>
    /// Nạp lại hàng cấm từ save. Không có bước này thì vừa vào lại game là bộ sinh
    /// có thể đẻ ra đúng tên của đơn đang nằm trên bảng — người chơi thấy hai phiếu
    /// trùng tên cạnh nhau và nghĩ game lỗi.
    /// </summary>
    public static void RestoreRecent(IEnumerable<string> titles)
    {
        Recent.Clear();
        if (titles == null) return;

        foreach (string t in titles)
        {
            if (string.IsNullOrEmpty(t)) continue;
            Recent.Add(t);
            if (Recent.Count > RecentMemory) Recent.RemoveAt(0);
        }
    }

    public static List<string> SnapshotRecent() => new List<string>(Recent);

    public static void ClearRecent() => Recent.Clear();
}
