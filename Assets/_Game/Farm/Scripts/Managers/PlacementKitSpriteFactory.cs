using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPRITE THỦ TỤC CHO BỘ KIT ĐẶT CÔNG TRÌNH.
///
/// ══════════════════════════════════════════════════════════════════════════
///  VÌ SAO VẼ BẰNG CODE
/// ══════════════════════════════════════════════════════════════════════════
/// Chủ dự án sẽ tự vẽ art rồi gắn đè vào sau. Cho tới lúc đó, mọi ô chờ art phải
/// có sẵn HÌNH TẠM để nhìn thấy được bố cục và chỉnh vị trí — nếu để `sprite = null`
/// thì SpriteRenderer im lặng không vẽ gì, và không ai biết khung đang đặt đúng chưa.
///
/// ══════════════════════════════════════════════════════════════════════════
///  CỐ Ý VẼ KHÁC BẢN THAM CHIẾU
/// ══════════════════════════════════════════════════════════════════════════
/// Bản tham chiếu dùng: hình thoi TÔ ĐẶC nửa trong suốt + 4 NÊM TAM GIÁC ĐẶC đặt ở
/// GIỮA CẠNH. Bộ này cố tình đi hướng khác về mặt hình học:
///
///   • Thảm nền   → hình thoi BO GÓC, RỖNG RUỘT, sáng dần ra mép (gradient viền)
///                  thay vì tô đặc phẳng.
///   • Dấu góc    → NGOẶC CHỮ L ôm 4 GÓC (kiểu khung ngắm máy ảnh),
///                  không phải nêm tam giác ở giữa cạnh.
///   • Viền       → NÉT ĐỨT chạy dọc cạnh, không phải đường liền.
///   • Bảng màu   → xanh ngọc (#5FD9A8) / san hô (#FF7A66),
///                  không phải xanh lá chanh của bản tham chiếu.
///
/// Bốn khác biệt đó nằm ở hình dạng và bảng màu — phần được bảo hộ — trong khi vẫn
/// giữ nguyên CHỨC NĂNG (báo vùng chiếm, báo hợp lệ/không hợp lệ), thứ không ai độc
/// quyền được.
///
/// Mọi sprite đều là ẢNH TRẮNG/XÁM để `SpriteRenderer.color` nhuộm được. Vẽ sẵn màu
/// vào texture thì lúc chuyển xanh↔đỏ sẽ ra màu bùn.
/// </summary>
public static class PlacementKitSpriteFactory
{
    private const int PPU = 100;
    private static readonly Dictionary<string, Sprite> _kho = new Dictionary<string, Sprite>();

    // ═════════════════════════════════════════════════════════════════════════
    //  API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Thảm hình thoi bo góc, rỗng ruột, sáng dần ra mép.</summary>
    public static Sprite ThamHinhThoi() => LayHoacVe("tham_thoi", 256, VeThamHinhThoi);

    /// <summary>Ngoặc chữ L ôm một góc. Hướng mặc định: góc trên-trái.</summary>
    public static Sprite NgoacGoc() => LayHoacVe("ngoac_goc", 128, VeNgoacGoc);

    /// <summary>Một vạch của viền nét đứt (viên thuốc bo tròn hai đầu).</summary>
    public static Sprite VachNetDut() => LayHoacVe("vach_dut", 64, VeVachNetDut);

    /// <summary>Chấm tròn mềm — dùng cho hạt nhấp nháy ở 4 góc.</summary>
    public static Sprite ChamTron() => LayHoacVe("cham_tron", 64, VeChamTron);

    /// <summary>Chip "nắm để kéo" nổi trên nóc công trình khi vào Edit Mode.</summary>
    public static Sprite ChipNamKeo() => LayHoacVe("chip_nam", 128, VeChipNamKeo);

    // ═════════════════════════════════════════════════════════════════════════
    //  VÒNG 10 — CARD BO GÓC CHO THANH XÁC NHẬN LÚC ĐẶT CÔNG TRÌNH
    //
    //  Ba sprite dưới đây là NỀN DỰ PHÒNG, không phải để thay art của Sếp. Đường chính
    //  vẫn là `UIStandardSprites.CardOuter` / `CardInner` (art thật, đã có bản copy trong
    //  Assets/Resources/UI/Standard nên build không bị null). Chỉ khi hai ô đó trả null
    //  thì mới rơi xuống đây.
    //
    //  KHÁC GÌ `ConstructionSpriteFactory.Panel()`: Panel() là chữ nhật bo góc TÔ PHẲNG
    //  MỘT MÀU — tint xong ra một khối trơn, đúng thứ Sếp nói là "trơn, viền cứng". Bộ
    //  này BAKE ĐỘ SÁNG vào texture: vành ngoài xám 0.55, ruột trắng 1.0. `Image.color`
    //  NHÂN vào texture, nên CHỈ MỘT Image + MỘT màu tint là đã ra "nền + viền" cùng
    //  tông — khỏi phải xếp 2 Image lồng nhau, khỏi bắt Sếp gán thêm màu thứ hai.
    //  (Cùng mẹo với `ConstructionSpriteFactory.Balloon`.)
    //
    //  MỌI SPRITE Ở ĐÂY ĐỀU `SpriteMeshType.FullRect`: mesh Tight (mặc định của
    //  `Sprite.Create`) làm `Image.Type.Sliced` chạy sai VÀ cắt mất dải khử răng cưa ở
    //  mép. Card còn phải có `border` khác 0, nếu không Sliced tụt về kéo giãn phẳng và
    //  góc bo bị bóp méo — chính xác lỗi đang thấy ở `btn_CloseRanking.png`
    //  (spriteBorder 0/0/0/0) mà ConstructionArtKit đang gán vào ô `priceBarBg`.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// CARD BO GÓC CÓ VIỀN — nền thanh xác nhận "MUA VỚI GIÁ …".
    /// Vành ngoài tối hơn ruột; 9-slice nên kéo rộng/hẹp bao nhiêu góc vẫn tròn đều.
    /// </summary>
    public static Sprite TheBoGocCoVien(int rong = 160, int cao = 128,
                                        int banKinh = 34, int dayVien = 9)
    {
        // Kẹp SÀN 32 px TRƯỚC khi suy bán kính: nếu để rong/cao nhỏ hơn thế thì
        // `Min(rong,cao)/2 - 2` tụt xuống dưới 2 và Mathf.Clamp (min > max) trả về MAX,
        // tức bk = 0 rồi dv = −1 → card mất hẳn góc bo, im lặng không báo gì.
        rong = Mathf.Max(32, rong);
        cao  = Mathf.Max(32, cao);

        int bk = Mathf.Clamp(banKinh, 2, Mathf.Min(rong, cao) / 2 - 2);
        int dv = Mathf.Clamp(dayVien, 1, Mathf.Max(1, bk - 1));
        float v = bk + dv + 2;

        return LayHoacVeCoVien($"the_vien_{rong}_{cao}_{bk}_{dv}", rong, cao,
                               (tex, w, h) => VeTheCoVien(tex, w, h, bk, dv),
                               new Vector4(v, v, v, v));
    }

    /// <summary>
    /// Nút TRÒN nổi — dùng cho ✗ HUỶ.
    /// Vẽ vuông và tô đúng tỉ lệ 1:1 nên dùng `Image.Type.Simple` + `preserveAspect`,
    /// KHÔNG cần border (đốm sáng bị 9-slice kéo giãn thì nhìn ra vết bẩn).
    /// </summary>
    public static Sprite NutTronNoi(int kichThuoc = 128)
    {
        int n = Mathf.Max(16, kichThuoc);
        return LayHoacVeCoVien($"nut_tron_{n}", n, n,
                               (tex, w, h) => VeNutTronNoi(tex, w),
                               Vector4.zero);
    }

    /// <summary>
    /// Nút VUÔNG BO GÓC nổi — dùng cho ✓ XÁC NHẬN.
    /// Khác nút huỷ về HÌNH DẠNG (vuông bo góc vs tròn), không chỉ khác màu: người mù màu
    /// và trẻ chưa đọc chữ vẫn phân biệt được bằng silhouette.
    /// </summary>
    public static Sprite NutVuongNoi(int kichThuoc = 128, int banKinh = 30)
    {
        int n  = Mathf.Max(32, kichThuoc);      // sàn 32 — xem ghi chú ở TheBoGocCoVien
        int bk = Mathf.Clamp(banKinh, 2, n / 2 - 2);
        return LayHoacVeCoVien($"nut_vuong_{n}_{bk}", n, n,
                               (tex, w, h) => VeNutVuongNoi(tex, w, h, bk),
                               Vector4.zero);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  VÒNG 11 — ART THẬT ĐI TRƯỚC, VẼ THỦ TỤC TỤT XUỐNG HÀNG DỰ PHÒNG
    //
    //  Sếp: "gắn assets đã có vào, đừng chỉ dựng khung nền".
    //  Ba hàm TheBoGocCoVien / NutTronNoi / NutVuongNoi ở TRÊN VẪN CÒN NGUYÊN, KHÔNG xoá
    //  dòng nào — chúng chỉ đổi vai: từ ĐƯỜNG CHÍNH (vòng 10) thành LƯỚI AN TOÀN, chạy
    //  đúng khi art thật trả null.
    //
    //  ĐƯỜNG LẤY SPRITE: CHỈ `UIStandardSprites.Load(path)`.
    //    · Editor : Load → SettingsPopupUI.LoadSprite → AssetDatabase ⇒ THẤY NGAY, khỏi
    //               chờ Sếp copy file.
    //    · BUILD  : Load chỉ thấy file CÓ bản copy trong Assets/Resources/UI/Standard/.
    //               File chưa copy ⇒ trả null ⇒ tự rơi xuống nhánh thủ tục, KHÔNG null
    //               reference, KHÔNG ô trống.
    //  TUYỆT ĐỐI KHÔNG gọi UnityEditor.AssetDatabase ở đây: nó là Editor-only, build vừa
    //  không biên dịch được vừa không có nhánh đỡ.
    //
    //  ĐÃ ĐO TAY TỪNG FILE (tên · px · spriteBorder · có bản copy trong Resources/UI/Standard):
    //    shop_card_outer      160x210  30/30/30/30     CÓ    khung ngoài card
    //    shop_card_inner      140x170  28/28/28/28     CÓ    nền giấy kem trong card
    //    check_badge_green     48x48   0/0/0/0         CÓ    ĐĨA XANH ĐẶC — KHÔNG có dấu ✓ sẵn
    //    btn_red_small        256x96   28/28/28/28     CÓ    THANH ĐỎ bo góc — KHÔNG có dấu ✗ sẵn
    //    btn_yellow_3d         96x48   16/16/16/16     CÓ    nền nút xoay
    //    ribbon_banner_gold   128x48   28/14/28/14     CÓ    ruy băng vàng sau hàng giá
    //    ob_check.png         128x128  0/0/0/0         KHÔNG dấu ✓ TRẮNG thật   → CẦN SẾP
    //    ob_trash.png         128x128  0/0/0/0         KHÔNG thùng rác TRẮNG    → CẦN SẾP
    //
    //  🔴 MỌI SPRITE TRÊN ĐÃ BAKE MÀU THẬT VÀO TEXTURE:
    //       shop_card_outer   = nâu  (184,127,67), vành (163, 92,20)
    //       shop_card_inner   = kem  (254,245,226)
    //       check_badge_green = xanh (110,195, 45), vành (50,120,20)
    //       btn_red_small     = đỏ   (221, 76, 69), vành (110, 20,20)
    //  ⇒ `Image.color` PHẢI LÀ TRẮNG. Đây đúng là thủ phạm "xấu quá": vòng 10 tô
    //  mauVienThe (0.42,0.27,0.14) = (107,69,36) LÊN art nâu ⇒ nhân ra MÀU BÙN, và tô
    //  confirmButtonColor (0.27,0.78,0.30) lên đĩa xanh ⇒ (30,152,14) xanh sẫm đục,
    //  cạnh nút đỏ tươi thì mắt đọc ra "nút ✓ bị disable".
    //  Sprite THỦ TỤC là ảnh TRẮNG/XÁM nên tint là đúng; ART THẬT thì tint là PHÁ.
    //
    //  KHÔNG cache theo cỡ: art thật là 9-slice / preserveAspect nên MỘT sprite dùng cho
    //  mọi kích thước. Chỉ nhánh thủ tục mới cần khoá theo cỡ (đã có trong `_kho`).
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Dấu ✓ TRẮNG thật — CHƯA có bản copy trong Resources/UI/Standard (xem CẦN SẾP).</summary>
    public const string PathGlyphCheck = "Assets/_Game/Farm/Art/UI_OrderBoard/ob_check.png";

    /// <summary>Thùng rác TRẮNG thật — CHƯA có bản copy trong Resources/UI/Standard.</summary>
    public const string PathGlyphTrash = "Assets/_Game/Farm/Art/UI_OrderBoard/ob_trash.png";

    private static Sprite _artKhungNgoai, _artNenTrong, _artRuyBang;
    private static Sprite _artNutXacNhan, _artNutHuy, _artNutXoay;
    private static Sprite _artGlyphCheck, _artGlyphTrash;

    /// <summary>KHUNG NGOÀI card. Chính: shop_card_outer. Dự phòng: TheBoGocCoVien.</summary>
    public static Sprite TheKhungNgoai()
    {
        if (_artKhungNgoai == null)
            _artKhungNgoai = UIStandardSprites.CardOuter ?? TheBoGocCoVien(160, 128, 34, 9);
        return _artKhungNgoai;
    }

    /// <summary>NỀN GIẤY trong card. Chính: shop_card_inner. Dự phòng: TheBoGocCoVien.</summary>
    public static Sprite TheNenTrong()
    {
        if (_artNenTrong == null)
            _artNenTrong = UIStandardSprites.CardInner ?? TheBoGocCoVien(160, 128, 30, 8);
        return _artNenTrong;
    }

    /// <summary>RUY BĂNG sau hàng giá. Chính: ribbon_banner_gold. Dự phòng: TheBoGocCoVien dẹt.</summary>
    public static Sprite TheRuyBangGia()
    {
        if (_artRuyBang == null)
            _artRuyBang = UIStandardSprites.Ribbon ?? TheBoGocCoVien(160, 48, 18, 5);
        return _artRuyBang;
    }

    /// <summary>
    /// NỀN nút ✓ XÁC NHẬN. Chính: check_badge_green — ĐĨA TRÒN xanh, border 0.
    /// Dự phòng: NutTronNoi (cũng tròn) ⇒ HÌNH DẠNG KHÔNG ĐỔI dù rơi nhánh nào.
    /// </summary>
    public static Sprite NenNutXacNhan()
    {
        if (_artNutXacNhan == null)
            _artNutXacNhan = UIStandardSprites.CheckBadge ?? NutTronNoi(128);
        return _artNutXacNhan;
    }

    /// <summary>
    /// NỀN nút ✗ HUỶ. Chính: btn_red_small — thanh đỏ bo góc, border 28 ⇒ 9-slice ra
    /// VUÔNG BO GÓC. Dự phòng: NutVuongNoi (cũng vuông bo góc) ⇒ HÌNH DẠNG KHÔNG ĐỔI.
    ///
    /// 🔴 ĐÃ ĐỔI VAI SO VỚI VÒNG 10 (✗ tròn / ✓ vuông → ✗ VUÔNG / ✓ TRÒN). Lý do: art thật
    /// quyết định hình. check_badge_green là ĐĨA (border 0, chỉ vẽ đúng khi Simple +
    /// preserveAspect), btn_red_small là THANH 256x96 (chỉ vẽ đúng khi Sliced). Ép ngược
    /// lại là đĩa bị 9-slice kéo méo và thanh bị preserveAspect ép thành hình chữ nhật dẹt.
    /// KÊNH PHÂN BIỆT VẪN LÀ BA: hình khác nhau, cỡ khác nhau, màu khác nhau.
    /// </summary>
    public static Sprite NenNutHuy()
    {
        if (_artNutHuy == null)
            _artNutHuy = UIStandardSprites.Close ?? NutVuongNoi(128, 34);
        return _artNutHuy;
    }

    /// <summary>NỀN nút ↻ XOAY. Chính: btn_yellow_3d. Dự phòng: NutTronNoi.</summary>
    public static Sprite NenNutXoay()
    {
        if (_artNutXoay == null)
            _artNutXoay = UIStandardSprites.BtnYellow3D ?? NutTronNoi(128);
        return _artNutXoay;
    }

    /// <summary>Dấu ✓ TRẮNG. Chính: ob_check (chưa vào Resources). Dự phòng: CheckMark thủ tục.</summary>
    public static Sprite GlyphXacNhan()
    {
        if (_artGlyphCheck == null)
            _artGlyphCheck = UIStandardSprites.Load(PathGlyphCheck)
                          ?? ConstructionSpriteFactory.CheckMark();
        return _artGlyphCheck;
    }

    /// <summary>Thùng rác TRẮNG. Chính: ob_trash (chưa vào Resources). Dự phòng: TrashCan thủ tục.</summary>
    public static Sprite GlyphXoa()
    {
        if (_artGlyphTrash == null)
            _artGlyphTrash = UIStandardSprites.Load(PathGlyphTrash)
                          ?? ConstructionSpriteFactory.TrashCan();
        return _artGlyphTrash;
    }

    /// <summary>
    /// Kiểu vẽ ĐÚNG cho một sprite, SUY TỪ CHÍNH SPRITE thay vì gán cứng:
    ///   border ≠ 0 ⇒ Sliced (kéo rộng/hẹp bao nhiêu góc bo vẫn tròn đều).
    ///   border = 0 ⇒ Simple (Sliced với border 0 tụt về kéo giãn phẳng, đĩa tròn méo).
    /// Nhờ suy từ sprite, Sếp thay art khác cỡ / khác border là code TỰ ĐÚNG, khỏi sửa.
    /// </summary>
    public static bool LaSprite9Slice(Sprite s)
        => s != null && s.border.sqrMagnitude > 0.01f;

    // ═════════════════════════════════════════════════════════════════════════
    //  VẼ
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hình thoi bo góc, RỖNG RUỘT: alpha cao ở vành, tắt dần vào giữa.
    ///
    /// Dùng "khoảng cách kiểu kim cương bo góc" |x|^p + |y|^p = 1 với p ≈ 1.35.
    /// p = 1 cho hình thoi nhọn (chính là hình bản tham chiếu dùng), p = 2 cho hình
    /// tròn. Chọn 1.35 để ra hình thoi có góc bo — nhận ra ngay là khác.
    /// </summary>
    private static void VeThamHinhThoi(Texture2D tex, int n)
    {
        const float p = 1.35f;
        float giua = (n - 1) * 0.5f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x - giua) / giua;
            float v = (y - giua) / giua;
            float d = Mathf.Pow(Mathf.Abs(u), p) + Mathf.Pow(Mathf.Abs(v), p);
            d = Mathf.Pow(d, 1f / p);          // 0 ở tâm, 1 ở vành

            float a;
            if (d > 1.0f)
            {
                a = 0f;                         // ngoài hình
            }
            else if (d > 0.90f)
            {
                // Vành ngoài: dày, đây là thứ mắt nhìn thấy rõ nhất.
                a = Mathf.SmoothStep(0f, 1f, (1.0f - d) / 0.10f);
            }
            else
            {
                // Ruột: mờ dần về tâm, giữ lại chút nền để phân biệt với cỏ bên ngoài
                // nhưng KHÔNG che mất mặt đất — khác hẳn kiểu tô đặc.
                a = Mathf.Lerp(0.34f, 0.06f, Mathf.Pow(1f - d / 0.90f, 0.7f));
            }

            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    /// <summary>
    /// Ngoặc chữ L: hai thanh dày vuông góc, đầu bo tròn, chừa một khoảng hở ở đỉnh
    /// góc. Khoảng hở đó là chi tiết làm nó ra dáng "khung ngắm" chứ không phải nêm đặc.
    /// </summary>
    private static void VeNgoacGoc(Texture2D tex, int n)
    {
        float day  = n * 0.20f;      // bề dày thanh
        float dai  = n * 0.74f;      // chiều dài mỗi thanh
        float ho   = n * 0.06f;      // khoảng hở ở đỉnh góc

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;      // gốc toạ độ về góc trên-trái

            // Thanh ngang: chạy từ (ho, ho) sang phải
            float aNgang = ThanhBoTron(fx, fy, ho, ho, ho + dai, ho + day);
            // Thanh dọc: chạy từ (ho, ho) xuống dưới
            float aDoc   = ThanhBoTron(fx, fy, ho, ho, ho + day, ho + dai);

            float a = Mathf.Max(aNgang, aDoc);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    /// <summary>Vạch nét đứt: viên thuốc nằm ngang, bo tròn hai đầu.</summary>
    private static void VeVachNetDut(Texture2D tex, int n)
    {
        float day = n * 0.34f;
        float le  = n * 0.10f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;
            float a = ThanhBoTron(fx, fy, le, (n - day) * 0.5f, n - le, (n + day) * 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    private static void VeChamTron(Texture2D tex, int n)
    {
        float giua = (n - 1) * 0.5f;
        float bk   = giua * 0.86f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Mathf.Sqrt((x - giua) * (x - giua) + (y - giua) * (y - giua));
            float a = Mathf.Clamp01((bk - d) / Mathf.Max(1f, bk * 0.35f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
    }

    /// <summary>
    /// Chip nắm kéo: viên bo góc có 3 vạch ngang ở giữa (biểu tượng "kéo" quen thuộc).
    /// Đây là thứ bản tham chiếu KHÔNG có — thêm vào vì công trình đứng yên trong Edit
    /// Mode cần một dấu hiệu nói "cái này nhấc được", chứ không phải đoán.
    /// </summary>
    private static void VeChipNamKeo(Texture2D tex, int n)
    {
        float le  = n * 0.10f;
        float bk  = n * 0.22f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float fx = x, fy = n - 1 - y;
            float aNen = HinhBoGoc(fx, fy, le, le, n - le, n - le, bk);

            // Ba vạch ngang, màu đục hơn nền chip
            float aVach = 0f;
            for (int i = 0; i < 3; i++)
            {
                float cy = n * (0.34f + i * 0.16f);
                aVach = Mathf.Max(aVach,
                    ThanhBoTron(fx, fy, n * 0.28f, cy - n * 0.035f, n * 0.72f, cy + n * 0.035f));
            }

            // Vạch KHOÉT LỖ trên nền: alpha nền trừ đi alpha vạch → nhìn xuyên qua thấy
            // mặt đất, nên chip không thành một khối đặc che mất nóc nhà.
            float a = Mathf.Max(0f, aNen - aVach * 0.85f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
    }

    /// <summary>
    /// Card: chữ nhật bo góc TÔ ĐẶC, vành ngoài dày `dv` px tối hơn ruột.
    /// Bake ĐỘ SÁNG chứ không bake MÀU — để `Image.color` nhuộm được sang tông nào cũng được.
    /// </summary>
    private static void VeTheCoVien(Texture2D tex, int w, int h, int bk, int dv)
    {
        const float SangVien = 0.55f;   // vành = 55 % độ sáng của ruột
        const float Le       = 1f;      // chừa 1 px cho khử răng cưa

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float sd = KhoangCachBoGoc(fx, fy, Le, Le, w - Le, h - Le, bk);

            float a = Mathf.Clamp01(0.5f - sd);          // trong hình → 1, ngoài → 0
            if (a <= 0.002f) { tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f)); continue; }

            // sd = 0 ở mép, càng âm càng vào sâu. Sâu hơn dv thì đã là ruột.
            float sang = Mathf.Lerp(SangVien, 1f, Mathf.Clamp01((-sd - dv) + 0.5f));
            tex.SetPixel(x, y, new Color(sang, sang, sang, a));
        }
    }

    /// <summary>Đĩa tròn nổi: vành tối, thân hơi tối, đốm sáng lệch LÊN TRÊN.</summary>
    private static void VeNutTronNoi(Texture2D tex, int n)
    {
        const float SangVien = 0.55f;
        const float SangThan = 0.88f;

        float giua = (n - 1) * 0.5f;
        float bk   = giua - 1f;
        float dv   = n * 0.11f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = x - giua, dy = y - giua;
            float sd = Mathf.Sqrt(dx * dx + dy * dy) - bk;

            float a = Mathf.Clamp01(0.5f - sd);
            if (a <= 0.002f) { tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f)); continue; }

            float sang = Mathf.Lerp(SangVien, SangThan, Mathf.Clamp01((-sd - dv) + 0.5f));

            // y LỚN = phía TRÊN ảnh (gốc toạ độ texture ở dưới-trái) → đốm sáng ở đỉnh nút.
            float gx = x - giua;
            float gy = y - (giua + n * 0.20f);
            float gloss = 1f - Mathf.Sqrt(gx * gx + gy * gy) / (n * 0.44f);
            sang = Mathf.Lerp(sang, 1f, Mathf.Clamp01(gloss) * 0.9f);

            tex.SetPixel(x, y, new Color(sang, sang, sang, a));
        }
    }

    /// <summary>Nút vuông bo góc nổi — cùng cách tô với nút tròn, chỉ khác HÌNH.</summary>
    private static void VeNutVuongNoi(Texture2D tex, int w, int h, int bk)
    {
        const float SangVien = 0.55f;
        const float SangThan = 0.88f;
        const float Le       = 1f;

        float dv = Mathf.Min(w, h) * 0.11f;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float sd = KhoangCachBoGoc(fx, fy, Le, Le, w - Le, h - Le, bk);

            float a = Mathf.Clamp01(0.5f - sd);
            if (a <= 0.002f) { tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0f)); continue; }

            float sang = Mathf.Lerp(SangVien, SangThan, Mathf.Clamp01((-sd - dv) + 0.5f));

            float gx = fx - w * 0.5f;
            float gy = fy - h * 0.72f;
            float gloss = 1f - Mathf.Sqrt(gx * gx + gy * gy) / (Mathf.Min(w, h) * 0.46f);
            sang = Mathf.Lerp(sang, 1f, Mathf.Clamp01(gloss) * 0.9f);

            tex.SetPixel(x, y, new Color(sang, sang, sang, a));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HÌNH HỌC
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Thanh chữ nhật bo tròn hai đầu — trả alpha 0..1 có khử răng cưa.</summary>
    private static float ThanhBoTron(float px, float py, float x0, float y0, float x1, float y1)
    {
        float bk = Mathf.Min(x1 - x0, y1 - y0) * 0.5f;
        return HinhBoGoc(px, py, x0, y0, x1, y1, bk);
    }

    /// <summary>Chữ nhật bo góc bán kính bk, khử răng cưa bằng dải chuyển 1 pixel.</summary>
    private static float HinhBoGoc(float px, float py, float x0, float y0, float x1, float y1, float bk)
    {
        float cx = Mathf.Clamp(px, x0 + bk, x1 - bk);
        float cy = Mathf.Clamp(py, y0 + bk, y1 - bk);
        float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        return Mathf.Clamp01((bk - d) + 0.5f);
    }

    /// <summary>
    /// Khoảng cách CÓ DẤU tới mép chữ nhật bo góc: ÂM = bên trong, 0 = đúng mép.
    ///
    /// `HinhBoGoc` chỉ trả alpha ĐÃ KẸP về 0..1 nên không đo được "sâu bao nhiêu px" —
    /// mà đó chính là thứ cần để tách vành khỏi ruột. Giá trị bão hoà ở −bk khi điểm nằm
    /// sâu trong lòng; không sao vì vành luôn được kẹp mỏng hơn bk.
    /// </summary>
    private static float KhoangCachBoGoc(float px, float py,
                                         float x0, float y0, float x1, float y1, float bk)
    {
        float cx = Mathf.Clamp(px, x0 + bk, x1 - bk);
        float cy = Mathf.Clamp(py, y0 + bk, y1 - bk);
        float d  = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        return d - bk;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HẠ TẦNG
    // ═════════════════════════════════════════════════════════════════════════

    private static Sprite LayHoacVe(string ten, int kichThuoc, System.Action<Texture2D, int> ve)
    {
        // Cache theo tên: mỗi sprite chỉ vẽ MỘT LẦN cho cả phiên chơi. Không cache thì
        // mỗi công trình lại dựng một texture 256×256 riêng — 34 công trình là 8MB rác.
        if (_kho.TryGetValue(ten, out Sprite co) && co != null) return co;

        var tex = new Texture2D(kichThuoc, kichThuoc, TextureFormat.RGBA32, false)
        {
            name = "KitDat_" + ten,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        ve(tex, kichThuoc);
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, kichThuoc, kichThuoc),
                                new Vector2(0.5f, 0.5f), PPU);
        spr.name = "KitDat_" + ten;
        spr.hideFlags = HideFlags.HideAndDontSave;

        _kho[ten] = spr;
        return spr;
    }

    /// <summary>
    /// Bản CÓ BORDER 9-slice và nhận ảnh CHỮ NHẬT (bản gốc chỉ nhận ảnh vuông, border 0).
    ///
    /// BẮT BUỘC `SpriteMeshType.FullRect`: mesh Tight (mặc định của `Sprite.Create`) cắt
    /// sát vành alpha nên (1) `Image.Type.Sliced` chạy sai và (2) dải khử răng cưa ở mép
    /// bị cắt mất, góc bo quay lại răng cưa. Cùng lý do `ConstructionSpriteFactory.Make`
    /// cũng truyền FullRect.
    ///
    /// Dùng CHUNG `_kho` với bản gốc: khoá đã mang đủ kích thước + bán kính + dày vành
    /// nên không thể trùng tên với sprite của bản kia.
    /// </summary>
    private static Sprite LayHoacVeCoVien(string ten, int rong, int cao,
                                          System.Action<Texture2D, int, int> ve, Vector4 vien)
    {
        if (_kho.TryGetValue(ten, out Sprite co) && co != null) return co;

        var tex = new Texture2D(rong, cao, TextureFormat.RGBA32, false)
        {
            name = "KitDat_" + ten,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        ve(tex, rong, cao);
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, rong, cao), new Vector2(0.5f, 0.5f),
                                PPU, 0, SpriteMeshType.FullRect, vien);
        spr.name = "KitDat_" + ten;
        spr.hideFlags = HideFlags.HideAndDontSave;

        _kho[ten] = spr;
        return spr;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Dọn cache khi vào Play Mode. Texture mang `HideAndDontSave` không tự bị thu hồi
    /// giữa các lần chạy; giữ tham chiếu cũ thì sprite trỏ vào texture đã bị Unity huỷ
    /// và mọi khung đặt biến thành ô trắng.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DonCache()
    {
        _kho.Clear();
        // Vòng 11: 8 ô art cũng phải trả về null. Ô nào đang giữ SPRITE THỦ TỤC thì
        // texture của nó mang HideAndDontSave — Unity huỷ giữa hai lần Play, giữ lại là
        // trỏ vào texture đã chết và nút biến thành ô trắng.
        _artKhungNgoai = _artNenTrong = _artRuyBang = null;
        _artNutXacNhan = _artNutHuy = _artNutXoay = null;
        _artGlyphCheck = _artGlyphTrash = null;
    }
#endif
}
