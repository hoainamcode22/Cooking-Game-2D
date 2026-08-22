using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SINH 4 ICON CÁM RIÊNG BIỆT CHO POPUP MÁY XAY — <b>Tools/Farm/Popup May Xay/</b> mục 7·8·9.
///
/// ══════════════════════════════════════════════════════════════════════════════
///  VÌ SAO CÓ TOOL NÀY
/// ══════════════════════════════════════════════════════════════════════════════
/// Popup máy xay hiện 4 card công thức TRÔNG GIỐNG HỆT NHAU vì đúng MỘT sprite
/// (`baothoc_0` trong `Assets/thietke/…/shop/baothoc.png`) đang được gán vào 16 ô:
///   • `icon` của cả 4 `MillRecipe_*.asset`
///   • `icon` của cả 4 `Item_Kho_Cook/Item_Cam*.asset` + `Item_CoTronBo.asset`
///   • `food1Icon` VÀ `premiumFoodIcon` của cả 4 `PenConfig/Config_Pen0*.asset`
/// Người chơi (và chủ dự án) nhìn vào chỉ thấy "y như cũ" — 4 công thức khác nhau
/// mà cùng một bao thóc nâu.
///
/// ══════════════════════════════════════════════════════════════════════════════
///  VÌ SAO NHUỘM MÀU LẠI CHỨ KHÔNG VẼ MỚI TỪ ĐẦU
/// ══════════════════════════════════════════════════════════════════════════════
/// `baothoc.png` là ART THẬT — vẽ tay, có nếp gấp, có viền, có khối sáng tối. Nếu
/// code tự vẽ 4 cái bao bằng gradient + hình học thì nó sẽ LẠC HẲN khỏi phần art
/// còn lại của game (đây chính là cái làm popup nhiệm vụ hồi trước trông "như đồ
/// placeholder"). Nên tool này:
///   1. Đổi TÔNG MÀU của chính cái bao đã vẽ tay đó — thay hue, co/giãn độ bão hoà,
///      NHƯNG GIỮ NGUYÊN value (độ sáng) và alpha ⇒ nếp gấp, viền, khối sáng tối
///      của hoạ sĩ còn nguyên, chỉ đổi màu.
///   2. Ghép thêm một cái đầu con vật nhỏ ở góc dưới-phải để đọc được ngay là cám
///      của con nào, không cần đọc chữ.
///
/// CẢ BỐN icon đều nhuộm từ cùng `baothoc.png` — kể cả cám heo, dù dự án CÓ sẵn một art
/// vẽ riêng cho nó (`camheoo-removebg-preview.png`). Lý do dài nằm ở ghi chú
/// "VÌ SAO CẢ 4 ĐỀU NHUỘM TỪ BAOTHOC" ngay dưới bảng khai báo; tóm lại: `camheoo` là một
/// VẬT KHÁC (túi giấy có ngôi sao vàng), xếp cạnh 3 cái bao bố thì thành đồ đi lạc, mà
/// mục đích của task là 4 card phải đọc ra là 4 biến thể của MỘT thứ.
///
/// ⚠ ĐÂY LÀ BẢN CHỮA CHÁY (stopgap), KHÔNG PHẢI ART CUỐI.
/// Đúng ra mỗi loại cám cần một bao được vẽ riêng (bao ngô vàng cho gà, bao cỏ khô
/// cho bò…). Hiện tại KHÔNG có bao nào như thế. Khi nào hoạ sĩ vẽ xong bao thật thì
/// gán tay vào 4 asset công thức là xong, tool này thành vô dụng — đó là kết cục mong muốn.
/// Xem mục "CẦN BẠN" trong báo cáo để biết còn thiếu những gì.
///
/// ══════════════════════════════════════════════════════════════════════════════
///  BA LỆNH
/// ══════════════════════════════════════════════════════════════════════════════
///   7. Xem truoc   — chạy khô, in ra sẽ sinh file nào / ghi ô nào / bỏ ô nào. KHÔNG ghi gì.
///   8. Sinh + Gan  — sinh 4 PNG rồi gán vào 20 ô (có hộp xác nhận trước khi ghi).
///   9. Hoan tac    — trả các ô icon về sprite placeholder `baothoc_0`. PNG vẫn để lại trên đĩa.
///
/// (Số 7·8·9 chứ không phải 4·5·6: `MillPopupBuilderTool` đã chiếm 0→4 trong cùng menu
///  `Tools/Farm/Popup May Xay/`, hai mục cùng bắt đầu bằng "4." rất dễ bấm nhầm.)
///
/// ══════════════════════════════════════════════════════════════════════════════
///  HAI CẠM BẪY ĐÃ TRÁNH (đọc trước khi sửa tool)
/// ══════════════════════════════════════════════════════════════════════════════
///  ① MỌI ảnh nguồn ở đây đều `spriteMode: 2` (Multiple) và `isReadable: 0`.
///     ⇒ `LoadAssetAtPath&lt;Texture2D&gt;().GetPixels()` NÉM LỖI, và
///        `LoadAssetAtPath&lt;Sprite&gt;()` TRẢ NULL.
///     Nên: đọc pixel bằng `File.ReadAllBytes` + `ImageConversion.LoadImage`, và
///     nạp Sprite bằng `LoadAllAssetRepresentationsAtPath` (xem
///     <c>UnlockIconFillTool.LoadSprite</c>). KHÔNG sửa import settings của ảnh gốc.
///  ② Sprite `baothoc_0` chỉ chiếm vùng 216×197 trong tấm 563×443 — phần còn lại
///     là khoảng trong suốt. Nếu xuất canvas bằng kích thước CẢ TẤM thì cái bao chỉ
///     to bằng ~38% ô sprite, nhìn ra icon bé tí lệch góc. Nên tool cắt theo HỘP BAO
///     ALPHA của chính pixel (không tin metadata slice) rồi mới ghép.
/// </summary>
public static class MillFeedIconTool
{
    private const string MENU = "Tools/Farm/Popup May Xay/";

    /// <summary>Nơi đổ PNG do tool sinh ra. Cùng gốc với <c>MillSpriteFactory.GenFolder</c>.</summary>
    private const string THU_MUC_RA = "Assets/_Game/GeneratedUI/Mill/Icons";

    private const string DUONG_DAN_BAO_CAO = THU_MUC_RA + "/_bao_cao_icon.txt";

    // ─────────────────────────────────────────────────────────────────────────────
    //  1. ẢNH NGUỒN — đã kiểm tra tồn tại từng file trên đĩa
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bao thóc vẽ tay 563×443, sprite con `baothoc_0`. ĐANG là placeholder dùng chung cho 16 ô.</summary>
    private const string ANH_BAO_THOC = "Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/assets/shop/baothoc.png";

    /// <summary>
    /// Bao "cám heo" 468×533 — art vẽ tay riêng DUY NHẤT đang có cho một loại cám, và đang
    /// mồ côi (0 tham chiếu trong toàn dự án).
    ///
    /// ⚠ TOOL KHÔNG DÙNG ẢNH NÀY. Nó chỉ xuất hiện trong mục "CẦN BẠN" của báo cáo để chủ
    /// dự án biết là có, chọn dùng hay không thì tuỳ. Vì sao không dùng: xem ghi chú
    /// "VÌ SAO CẢ 4 ĐỀU NHUỘM TỪ BAOTHOC" ngay dưới bảng khai báo.
    /// </summary>
    private const string ANH_CAM_HEO = "Assets/Anh/camheoo-removebg-preview.png";

    private const string ANH_CON_GA  = "Assets/Anh/conga-removebg-preview.png";              // 669×373, Pen_03 đang dùng
    private const string ANH_CON_HEO = "Assets/Anh/conheo-removebg-preview.png";             // 669×373, Pen_02 đang dùng
    private const string ANH_CON_BO  = "Assets/Anh/conbotrongchuong-removebg-preview.png";   // 606×412, chưa ai dùng
    private const string ANH_SUA     = "Assets/Assetsgame/suamilk.png";                      // 500×500, Item_Milk đang dùng

    // ─────────────────────────────────────────────────────────────────────────────
    //  2. THÔNG SỐ NHUỘM MÀU
    //
    //  Cách nhuộm: đổi pixel sang HSV → THAY hue, NHÂN saturation & value → đổi về RGB.
    //  Nhân (chứ không GÁN) value là để giữ TƯƠNG QUAN sáng-tối mà hoạ sĩ đã vẽ:
    //  nếp gấp vẫn tối hơn mặt phẳng, đỉnh bao vẫn sáng hơn chân bao.
    //  Alpha giữ nguyên tuyệt đối ⇒ không bị viền răng cưa.
    //
    //  Bốn bộ số dưới đây KHÔNG phải đoán: đã render thử 8 phương án trên chính
    //  `baothoc.png` rồi chọn bộ trông ra "vàng ngô ấm" và "xanh sữa nhạt" nhất.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Bỏ qua pixel gần như trong suốt (0–7) — nhuộm vào đó chỉ tạo viền bẩn.</summary>
    private const byte NGUONG_ALPHA = 8;

    /// <summary>
    /// Pixel có saturation dưới mức này coi như XÁM/TRẮNG/ĐEN → KHÔNG nhuộm.
    /// Giữ trung tính cho viền đen, điểm sáng trắng, chữ trên bao.
    /// </summary>
    private const float NGUONG_BAO_HOA = 0.12f;

    /// <summary>Cám cho gà: 38° = vàng ngô ấm. Bão hoà ×1.45 + sáng ×1.12 để không ra màu ô-liu xỉn.</summary>
    private const float GOC_MAU_CAM_GA = 38f;
    private const float BAO_HOA_CAM_GA = 1.45f;
    private const float DO_SANG_CAM_GA = 1.12f;

    /// <summary>
    /// Cám cho heo: 350° = hồng phấn (màu con heo).
    ///
    /// Bão hoà ×0.80 — GIẢM, không tăng — là chỗ quan trọng nhất của bộ số này. Ở 350°
    /// mà để bão hoà nguyên hay tăng lên thì cái bao ra màu ĐỎ THỊT SỐNG (đã render thử:
    /// ×1.20 nhìn hệt miếng thịt bò). Hạ bão hoà + nâng sáng ×1.10 kéo nó về "bao bố
    /// nhuộm hồng phấn" — vẫn liên tưởng con heo mà không gợi thịt.
    ///
    /// Vì sao KHÔNG chọn 15° (đất nung): 15° nằm sát nguyên bản nâu-cam của baothoc.png
    /// VÀ sát 38° của cám gà — ở cỡ chip 24px hai icon đó lẫn vào nhau. 350° cách cám gà
    /// 48° hue nên nhỏ mấy vẫn phân biệt được.
    /// </summary>
    private const float GOC_MAU_CAM_HEO = 350f;
    private const float BAO_HOA_CAM_HEO = 0.80f;
    private const float DO_SANG_CAM_HEO = 1.10f;

    /// <summary>Cỏ trộn cho bò: 110° = xanh lá tươi (cỏ). Bão hoà ×1.05, giữ nguyên độ sáng.</summary>
    private const float GOC_MAU_CO_TRON_BO = 110f;
    private const float BAO_HOA_CO_TRON_BO = 1.05f;
    private const float DO_SANG_CO_TRON_BO = 1f;

    /// <summary>Cám cho bò sữa: 205° = xanh lơ. Bão hoà ×0.42 + sáng ×1.22 cho ra "trắng sữa pha xanh".</summary>
    private const float GOC_MAU_CAM_BO_SUA = 205f;
    private const float BAO_HOA_CAM_BO_SUA = 0.42f;
    private const float DO_SANG_CAM_BO_SUA = 1.22f;

    // ── Thông số ghép badge con vật ─────────────────────────────────────────────
    //
    //  ⚠ LỖI ĐÃ SỬA Ở ĐÂY — ĐỌC TRƯỚC KHI ĐỔI SỐ
    //
    //  Bản trước dán badge vào GÓC DƯỚI-PHẢI của canvas VUÔNG. Chạy vào game mới thấy
    //  badge TRÀN RA NGOÀI vành đĩa tròn. Nguyên nhân là hình học, không phải art:
    //  UI vẽ sprite icon (ô VUÔNG) lồng trong một cái ĐĨA TRÒN (`shop_circle_plate` cho
    //  card + slot, `circle_preview` cho đĩa thành phẩm 240px). Góc của ô vuông nằm ở
    //  khoảng cách canh×0.707 tính từ tâm, còn vành đĩa chỉ tới canh×0.5 — nên bất cứ
    //  thứ gì đặt sát góc đều nằm NGOÀI đường tròn nội tiếp, tức là tràn ra khỏi đĩa.
    //
    //  Cách sửa: badge CANH GIỮA NGANG, tụt XUỐNG DƯỚI, và vị trí được GIẢI từ ràng buộc
    //  hình học chứ không chỉnh tay. Xem <see cref="TinhKhuon"/> để biết phép giải.
    //
    //  Tỉ lệ icon/đĩa thật trong UI (đọc từ MillDesign): card 98/116 = 0.845 ·
    //  slot 64/74 = 0.865 · thành phẩm 192/240 = 0.800. Chặt nhất là SLOT (0.865).
    //  Ràng buộc dưới đây tính theo đường tròn nội tiếp của chính ô icon nên thoả cả ba.

    /// <summary>
    /// Bề rộng badge = 34% bề rộng CANVAS (không phải bề rộng cái bao).
    /// Lịch sử: 40% (lấn bao) → 32% (bé quá) → 34%. Đo thật ở canvas 225px thì chặn trên
    /// do ràng buộc đường tròn là 124–164px tuỳ tỉ lệ ảnh con vật, còn 34% chỉ ra 76px
    /// ⇒ chặn KHÔNG bao giờ chạm, 34% dùng được cho cả 4 icon.
    /// Đây là số MONG MUỐN — nếu ai nâng lên quá chặn thì <see cref="TinhKhuon"/> tự co
    /// nhỏ lại và báo `daCoBadge`, không bao giờ để tràn.
    /// </summary>
    private const float TI_LE_BADGE = 0.34f;

    /// <summary>Bán kính hào quang = nửa đường chéo badge × 1.05 — vừa đủ trùm hết badge.</summary>
    private const float TI_LE_HAO_QUANG = 1.05f;

    /// <summary>Hào quang trắng mờ để badge nổi trên thân bao. 0.85 ⇒ vẫn thấy nếp bao mờ mờ dưới nó.</summary>
    private const float ALPHA_HAO_QUANG = 0.85f;

    /// <summary>
    /// Canvas VUÔNG cạnh = max(rộng, cao) của cái bao × 1.06.
    ///
    /// Vuông vì đường tròn nội tiếp của hình vuông mới có tâm trùng tâm ảnh — ô icon
    /// trong UI cũng luôn vuông (98×98, 64×64, 192×192). Ảnh chữ nhật thì min(W,H)
    /// cho ra đường tròn lệch, khó soát.
    /// Cái bao được đặt ĐÚNG TÂM canvas, vì tâm đó là chỗ khớp với tâm đĩa tròn.
    /// </summary>
    private const float TI_LE_KHUNG = 1.06f;

    /// <summary>
    /// Khoảng an toàn 4px giữa mép ngoài hào quang và đường tròn nội tiếp,
    /// để vành đĩa không bị badge chạm sát.
    /// </summary>
    private const float LE_AN_TOAN = 4f;

    /// <summary>
    /// Thêm 1px DỰ PHÒNG LÀM TRÒN khi giải d.
    ///
    /// Không có nó thì d lấy đúng cực đại ⇒ sau khi làm tròn vị trí về số nguyên, dư phòng
    /// còn đúng 0,00px và dòng soát in ra "TRÀN 0.0px" do sai số dấu phẩy động — báo động
    /// giả. 1px này khiến dư phòng luôn dương rõ ràng, đổi lại badge nhích lên 1px.
    /// </summary>
    private const float DU_LAM_TRON = 1f;

    // ── Chống LEM (ảnh con vật bị nhoè) ─────────────────────────────────────────
    //
    //  Chủ dự án báo "ảnh các con vật nó bị lem". Ba nguyên nhân cộng dồn, sửa cả ba:
    //
    //   ① MIPMAP TẮT. Texture 225px nhưng UI vẽ ở 64px (đĩa slot) và 98px (card) —
    //      thu 3,5 lần mà lấy mẫu bilinear thẳng từ ảnh gốc thì răng cưa, nhìn ra "lem".
    //      UI thường tắt mipmap, nhưng ĐÂY LÀ NGOẠI LỆ: một texture phục vụ 64px→192px.
    //   ② LẤY MẪU HAI LẦN. Bake thu 231px→76px, rồi Unity thu tiếp 225→64. Mỗi lần mất nét.
    //      Không bỏ được lần nào, nên phải làm lần bake cho thật tốt (xem ③).
    //   ③ THU MỘT BƯỚC BẰNG TRUNG BÌNH Ô. Trung bình ô rất "an toàn" nhưng mềm.
    //      Đổi sang hai bước: trung bình ô về ~2× đích (chống răng cưa), rồi Lanczos3 về
    //      đích (giữ nét), cuối cùng làm nét nhẹ (unsharp) để dựng lại mép.

    /// <summary>
    /// Bias mipmap. Số ÂM = kéo về mip nét hơn. −0.4 là mức đã thử: đủ để bớt mềm,
    /// chưa tới mức lôi lại răng cưa của mip gốc.
    /// </summary>
    private const float MIP_BIAS = -0.4f;

    /// <summary>Bán kính Gauss của bước làm nét, tính bằng pixel ở ĐỘ PHÂN GIẢI BAKE.</summary>
    private const float SHARP_BAN_KINH = 0.8f;

    /// <summary>
    /// Lượng làm nét (0.55 = 55%). Đã render thử 4 mức ở đúng 64px và 98px:
    /// 0.35 còn mềm · 0.55 nét mà sạch · 0.75 bắt đầu có quầng viền quanh con vật.
    /// </summary>
    private const float SHARP_LUONG = 0.55f;

    /// <summary>Bậc của nhân Lanczos (a=3) — chuẩn cho ảnh nghệ thuật.</summary>
    private const float LANCZOS_A = 3f;

    // ── KHUNG CẮT ĐẦU CON VẬT ───────────────────────────────────────────────────
    //
    //  VÌ SAO CẮT ĐẦU chứ không dùng cả con:
    //  Badge chỉ được vẽ ~22px trên đĩa slot (xem CẦN BẠN). Nhồi CẢ CON gà vào 22 pixel
    //  thì mọi chi tiết nhận dạng — mào, mỏ, mắt — bé hơn 1 pixel và tan thành một vệt
    //  nâu. Cắt lấy ĐẦU trước khi thu nhỏ thì vẫn 22px đó nhưng chỉ chứa đầu, nên mào đỏ
    //  và mỏ vàng còn đọc được. Cùng bộ lọc, cùng cỡ — khác ở chỗ nhồi bao nhiêu vào.
    //
    //  ⚠ TOẠ ĐỘ: phân số của HỘP BAO ALPHA (ngưỡng alpha ≥ 8, KHÔNG phải bbox của PIL —
    //    lệch ngưỡng từng làm sai cỡ canvas 227 vs 225 một lần rồi).
    //    (x0, yTren0, x1, yTren1) — y tính TỪ TRÊN XUỐNG như CSS, vì "đầu ở phía trên"
    //    dễ ngắm hơn. <see cref="CatKhungTiLe"/> tự đổi sang trục Y từ dưới lên của Unity.
    //
    //  Cả ba con vật đều có đầu ở GÓC TRÊN-TRÁI của hộp bao alpha nên phân số cố định là
    //  đủ, không cần dò tìm gì.

    /// <summary>Đầu gà: nửa trái, 46% trên — khung lấy mào đỏ + mỏ vàng + mắt + yếm.</summary>
    private static readonly Vector4 KHUNG_DAU_GA = new Vector4(0.00f, 0.00f, 0.50f, 0.46f);

    /// <summary>
    /// Đầu heo: 34% trái, từ 2% đến 52% — khung lấy tai + mắt + mũi.
    /// Đã siết từ 0.40×0.62 xuống: mức rộng hơn lôi cả VÒNG CỔ HẠT vào khung, ở 22px nó
    /// thành một dải lốm đốm dưới mõm, nhìn như nhiễu.
    /// </summary>
    private static readonly Vector4 KHUNG_DAU_HEO = new Vector4(0.00f, 0.02f, 0.34f, 0.52f);

    /// <summary>
    /// Đầu bò: 32% trái, 54% trên — khung lấy hai sừng + mắt + mõm.
    /// Đã siết từ 0.35×0.58, nhưng DỪNG ở đây: thử 0.30×0.48 thì đầu to hơn thật nhưng
    /// MÕM BỊ CẮT ở mép dưới khung. Sừng ở trên và mõm ở dưới là hai dấu hiệu "đây là con
    /// bò", mất một cái là hỏng — 0.32×0.54 giữ được cả hai.
    /// </summary>
    private static readonly Vector4 KHUNG_DAU_BO = new Vector4(0.00f, 0.00f, 0.32f, 0.54f);

    /// <summary>Bình sữa: giữ NGUYÊN cả bình — nó không có đầu, và cả bình mới ra "sữa".</summary>
    private static readonly Vector4 KHUNG_CA_ANH = new Vector4(0.00f, 0.00f, 1.00f, 1.00f);

    /// <summary>
    /// Các build target có thể có entry riêng trong .meta. Xem <see cref="ApDatImport"/>
    /// để biết vì sao phải dập nén cho từng entry chứ không chỉ entry Default.
    /// </summary>
    private static readonly string[] NEN_TANG =
    {
        "Standalone", "Android", "iPhone", "WebGL", "Windows Store Apps",
    };

    /// <summary>PPU giống mọi PNG khác trong <c>GeneratedUI/Mill</c> (xem MillSpriteFactory).</summary>
    private const float PPU = 100f;

    // ─────────────────────────────────────────────────────────────────────────────
    //  3. BẢNG KHAI BÁO — cả 3 lệnh đều đọc DUY NHẤT bảng này
    //     (cùng lối viết với TaskPopupSpriteWireTool.BangGan)
    // ─────────────────────────────────────────────────────────────────────────────

    private class DongCam
    {
        public string recipeId;        // khớp MillRecipeData.recipeId VÀ InventoryItemData.itemId
        public string tenFileRa;       // tên PNG sinh ra (không đuôi)
        public string anhNen;          // ảnh cái bao
        public float  gocMau;          // hue đích, độ; ÂM = không nhuộm
        public float  heSoBaoHoa;
        public float  heSoSang;
        public string anhBadge;        // ảnh con vật ghép ở dưới, canh giữa ngang
        public Vector4 khungBadge;     // khung cắt đầu, phân số hộp bao alpha, y từ TRÊN
        public string assetCongThuc;   // MillRecipe_*.asset
        public string assetVatPham;    // Item_Kho_Cook/Item_*.asset
        public string yDoMau;          // mô tả cho báo cáo
    }

    private static readonly DongCam[] BANG =
    {
        new DongCam {
            recipeId      = "cam_ga",
            tenFileRa     = "feed_cam_ga",
            anhNen        = ANH_BAO_THOC,
            gocMau        = GOC_MAU_CAM_GA,
            heSoBaoHoa    = BAO_HOA_CAM_GA,
            heSoSang      = DO_SANG_CAM_GA,
            anhBadge      = ANH_CON_GA,
            khungBadge    = KHUNG_DAU_GA,
            assetCongThuc = "Assets/_Game/Farm/Data/Mill/MillRecipe_CamGa.asset",
            assetVatPham  = "Assets/_Game/Farm/Data/Item_Kho_Cook/Item_CamGa.asset",
            yDoMau        = "vàng ngô ấm (hạt/ngũ cốc)",
        },
        new DongCam {
            recipeId      = "cam_heo",
            tenFileRa     = "feed_cam_heo",
            anhNen        = ANH_BAO_THOC,   // CÙNG cái bao với 3 dòng kia — xem ghi chú dưới bảng
            gocMau        = GOC_MAU_CAM_HEO,
            heSoBaoHoa    = BAO_HOA_CAM_HEO,
            heSoSang      = DO_SANG_CAM_HEO,
            anhBadge      = ANH_CON_HEO,
            khungBadge    = KHUNG_DAU_HEO,
            assetCongThuc = "Assets/_Game/Farm/Data/Mill/MillRecipe_CamHeo.asset",
            assetVatPham  = "Assets/_Game/Farm/Data/Item_Kho_Cook/Item_CamHeo.asset",
            yDoMau        = "hồng phấn (màu con heo)",
        },
        new DongCam {
            recipeId      = "co_tron_bo",
            tenFileRa     = "feed_co_tron_bo",
            anhNen        = ANH_BAO_THOC,
            gocMau        = GOC_MAU_CO_TRON_BO,
            heSoBaoHoa    = BAO_HOA_CO_TRON_BO,
            heSoSang      = DO_SANG_CO_TRON_BO,
            anhBadge      = ANH_CON_BO,
            khungBadge    = KHUNG_DAU_BO,
            assetCongThuc = "Assets/_Game/Farm/Data/Mill/MillRecipe_CoTronBo.asset",
            assetVatPham  = "Assets/_Game/Farm/Data/Item_Kho_Cook/Item_CoTronBo.asset",
            yDoMau        = "xanh lá tươi (cỏ trộn)",
        },
        new DongCam {
            recipeId      = "cam_bo_sua",
            tenFileRa     = "feed_cam_bo_sua",
            anhNen        = ANH_BAO_THOC,
            gocMau        = GOC_MAU_CAM_BO_SUA,
            heSoBaoHoa    = BAO_HOA_CAM_BO_SUA,
            heSoSang      = DO_SANG_CAM_BO_SUA,
            anhBadge      = ANH_SUA,
            khungBadge    = KHUNG_CA_ANH,
            assetCongThuc = "Assets/_Game/Farm/Data/Mill/MillRecipe_CamBoSua.asset",
            assetVatPham  = "Assets/_Game/Farm/Data/Item_Kho_Cook/Item_CamBoSua.asset",
            yDoMau        = "trắng sữa pha xanh lơ (bơ sữa)",
        },
    };

    // ═════════════════════════════════════════════════════════════════════════════
    //  VÌ SAO CẢ 4 ĐỀU NHUỘM TỪ BAOTHOC — kể cả cám heo, dù ĐÃ CÓ art riêng
    //
    //  Bản đầu tiên của tool này dùng `camheoo-removebg-preview.png` làm nền cho cám heo,
    //  lý do "đó là art thật, art thật thì hơn art nhuộm". Soi ở 3× thì thấy sai:
    //    • `camheoo` là MỘT VẬT KHÁC — túi giấy đáy phẳng nắp gập, có NGÔI SAO VÀNG to
    //      giữa mặt túi, tông be/nâu.
    //    • Ba cái kia là bao bố tròn miệng loe, có đống cám đầy trào lên khỏi miệng bao.
    //  Xếp cạnh nhau trong danh sách công thức, card #2 đọc ra là "túi phần thưởng /
    //  túi tiền đi lạc từ popup khác". Tệ hơn: ngôi sao vàng trong game này mang nghĩa
    //  EXP / phần thưởng / hàng premium (xem TaskPopupSpriteWireTool: expIcon và
    //  achievementTabIcon đều là iconsao) — dán nó lên một bao thức ăn gia súc là nói dối
    //  người chơi bằng hình.
    //
    //  ⇒ NHẤT QUÁN CẢ BỘ THẮNG "đây là art thật". Mục đích của cả task này là 4 card phải
    //    đọc ra là 4 BIẾN THỂ CỦA MỘT THỨ. Bốn cái bao bố giống nhau khác màu làm được
    //    việc đó; ba bao bố + một túi giấy có ngôi sao thì không.
    //
    //  `camheoo` KHÔNG bị xoá và KHÔNG bị gán vào đâu — nó được nêu trong mục "CẦN BẠN"
    //  của báo cáo để chủ dự án tự quyết nếu muốn quay lại dùng art vẽ tay.
    // ═════════════════════════════════════════════════════════════════════════════

    // ─────────────────────────────────────────────────────────────────────────────
    //  4. MỘT Ô SPRITE CẦN GHI
    // ─────────────────────────────────────────────────────────────────────────────

    private class ODich
    {
        public ScriptableObject asset;
        public string duongDanAsset;
        public string tenO;             // "icon" / "animalBadgeIcon" / "food1Icon" / "premiumFoodIcon"
        public string nhomO;            // "công thức" / "vật phẩm" / "chuồng"
        public Sprite spriteCu;
        public string duongDanMoi;      // PNG/ảnh sẽ gán (có thể chưa tồn tại lúc chạy khô)
        public string bangChung;        // vì sao ô này thuộc dòng cám này
        public string lyDoBoQua;        // rỗng = được ghi
        public bool   trungRoi;         // đã trỏ đúng chỗ, khỏi ghi
        public bool   laSuaLoi;         // đang trỏ bản sao trong Assets/thietke → repoint là SỬA LỖI

        /// <summary>Đóng gói phép ghi vào đúng field, khỏi phải reflection hay SerializedObject.</summary>
        public System.Action<Sprite> ghi;

        /// <summary>Đóng gói phép đọc lại field (dùng cho lệnh hoàn tác).</summary>
        public System.Func<Sprite> doc;
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  LỆNH 4 — CHẠY KHÔ
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// CHẠY KHÔ: in ra sẽ sinh PNG nào (kèm kích thước tính được từ pixel thật),
    /// sẽ ghi ô nào (asset → field → sprite mới), sẽ BỎ QUA ô nào và vì sao.
    /// KHÔNG ghi file, KHÔNG sửa asset, KHÔNG tạo thư mục.
    /// </summary>
    [MenuItem(MENU + "7. Icon Cam — Xem truoc (khong ghi)", false, 7)]
    public static void XemTruoc()
    {
        var bc = new StringBuilder();
        bc.AppendLine("═══════════════════════════════════════════════════════════════");
        bc.AppendLine(" ICON CÁM — XEM TRƯỚC (KHÔNG GHI GÌ)");
        bc.AppendLine("═══════════════════════════════════════════════════════════════");

        KiemTraAnhNguon(bc);

        // ── Sẽ sinh PNG nào, to bao nhiêu ────────────────────────────────────────
        bc.AppendLine();
        bc.AppendLine("── SẼ SINH " + BANG.Length + " PNG VÀO " + THU_MUC_RA + " ──");
        for (int i = 0; i < BANG.Length; i++)
        {
            DongCam d = BANG[i];
            bc.Append("  ").Append(d.tenFileRa).AppendLine(".png");
            bc.AppendLine("      nền   : " + d.anhNen);
            bc.AppendLine("      màu   : " + (d.gocMau < 0f
                ? "giữ nguyên"
                : "hue " + d.gocMau.ToString("0") + "°  bão hoà ×" + d.heSoBaoHoa.ToString("0.00") +
                  "  sáng ×" + d.heSoSang.ToString("0.00")) + "   → " + d.yDoMau);
            bc.AppendLine("      badge : " + d.anhBadge);

            KhuonAnh khuon = TinhKhuonAnh(d, bc);
            if (khuon == null)
            {
                bc.AppendLine("      cỡ ra : KHÔNG TÍNH ĐƯỢC (thiếu ảnh nguồn)");
            }
            else
            {
                bc.AppendLine("      cỡ ra : " + khuon.canh + "×" + khuon.canh + " px (vuông)" +
                              "   (bao cắt " + khuon.rongBao + "×" + khuon.caoBao +
                              ", badge " + khuon.rongBadge + "×" + khuon.caoBadge +
                              (khuon.daCoBadge ? " ĐÃ CO" : "") + ")");
                bc.AppendLine("      " + SoatTron(khuon));
            }
        }

        List<ODich> ds = DungKeHoach(bc);
        InKeHoach(ds, bc);

        InCanBan(bc);
        bc.AppendLine();
        bc.AppendLine("Chưa ghi gì cả. Chạy mục 8 để sinh PNG và gán.");
        Debug.Log(bc.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  LỆNH 5 — SINH + GÁN
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sinh 4 PNG icon cám rồi gán vào các ô Sprite của công thức / vật phẩm / chuồng.
    /// Có hộp xác nhận điểm danh số lượng TRƯỚC khi ghi bất cứ thứ gì.
    /// Ô nào đang giữ art KHÁC placeholder thì BỎ QUA, không ghi đè.
    /// </summary>
    [MenuItem(MENU + "8. Icon Cam — Sinh + Gan", false, 8)]
    public static void SinhVaGan()
    {
        var bc = new StringBuilder();
        bc.AppendLine("═══════════════════════════════════════════════════════════════");
        bc.AppendLine(" ICON CÁM — SINH + GÁN");
        bc.AppendLine("═══════════════════════════════════════════════════════════════");

        KiemTraAnhNguon(bc);

        List<ODich> ds = DungKeHoach(bc);

        int seGhi = 0, seBoQua = 0, seSua = 0;
        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            if (o.trungRoi) continue;
            if (o.lyDoBoQua.Length > 0) { seBoQua++; continue; }
            seGhi++;
            if (o.laSuaLoi) seSua++;
        }

        if (!EditorUtility.DisplayDialog(
                "Icon Cám máy xay",
                "SẼ SINH " + BANG.Length + " file PNG vào:\n" + THU_MUC_RA + "\n\n" +
                "SẼ GHI " + seGhi + " ô Sprite (trong đó " + seSua + " ô là SỬA LỖI: đang trỏ " +
                "bản sao trong Assets/thietke).\n" +
                "SẼ BỎ QUA " + seBoQua + " ô (đang giữ art khác placeholder).\n\n" +
                "Đây là art CHỮA CHÁY: nhuộm lại màu chính cái bao thóc vẽ tay + ghép " +
                "đầu con vật. Không phải art cuối.\n\nTiếp tục?",
                "SINH + GÁN", "Huỷ"))
        {
            Debug.Log("[IconCam] Người dùng bấm Huỷ — không ghi gì.");
            return;
        }

        // ── Bước 1: sinh PNG ────────────────────────────────────────────────────
        BaoDamThuMuc();

        bc.AppendLine();
        bc.AppendLine("── PNG ĐÃ SINH ──");
        var kho = new Dictionary<string, string>();   // recipeId → đường dẫn PNG
        for (int i = 0; i < BANG.Length; i++)
        {
            DongCam d = BANG[i];
            string duongDan = SinhMotIcon(d, bc);
            if (duongDan.Length > 0) kho[d.recipeId] = duongDan;
        }

        // ── Bước 2: gán ─────────────────────────────────────────────────────────
        // Nạp sprite MỘT LẦN cho mỗi đường dẫn, không nạp lại trong vòng lặp ô.
        var khoSprite = new Dictionary<string, Sprite>();
        for (int i = 0; i < ds.Count; i++)
        {
            string p = ds[i].duongDanMoi;
            if (p.Length == 0 || khoSprite.ContainsKey(p)) continue;
            khoSprite[p] = NapSprite(p);
        }

        bc.AppendLine();
        bc.AppendLine("── Ô SPRITE ──");

        int daGhi = 0, daBoQua = 0, daTrung = 0, loi = 0;
        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            string nhan = "  " + Path.GetFileName(o.duongDanAsset) + " · " + o.tenO;

            if (o.trungRoi)
            {
                daTrung++;
                bc.AppendLine(nhan + "  = đã đúng, khỏi ghi");
                continue;
            }

            if (o.lyDoBoQua.Length > 0)
            {
                daBoQua++;
                bc.AppendLine(nhan + "  ✘ BỎ QUA — " + o.lyDoBoQua);
                continue;
            }

            Sprite moi;
            if (!khoSprite.TryGetValue(o.duongDanMoi, out moi) || moi == null)
            {
                loi++;
                bc.AppendLine(nhan + "  ✘ LỖI — không nạp được sprite " + o.duongDanMoi);
                continue;
            }

            string tenCu = o.spriteCu == null ? "(trống)" : o.spriteCu.name;

            Undo.RecordObject(o.asset, "Gan icon cam may xay");
            o.ghi(moi);
            EditorUtility.SetDirty(o.asset);

            daGhi++;
            bc.AppendLine(nhan + "  " + tenCu + "  →  " + moi.name +
                          (o.laSuaLoi ? "   ★ SỬA LỖI: " + o.bangChung : ""));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bc.AppendLine();
        bc.AppendLine("── TỔNG ──");
        bc.AppendLine("  ghi " + daGhi + " ô · trùng sẵn " + daTrung + " ô · bỏ qua " +
                      daBoQua + " ô · lỗi " + loi + " ô");

        long tongByte = 0L;
        foreach (KeyValuePair<string, string> kv in kho)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(kv.Value);
            if (t == null) continue;
            tongByte += ByteIcon(Mathf.Max(t.width, t.height), true);
        }
        bc.AppendLine("  bộ nhớ 4 icon (RGBA32 không nén + mipmap): " +
                      (tongByte / 1024L) + " KiB ≈ " +
                      (tongByte / 1048576.0).ToString("0.00") + " MiB");
        bc.AppendLine("  ĐÂY LÀ QUYẾT ĐỊNH CÓ CHỦ Ý: đổi ~1 MiB để icon hết lem. Xem ghi chú");
        bc.AppendLine("  trong ApDatImport nếu cần siết lại bộ nhớ.");

        InCanBan(bc);
        XuatBaoCao(bc);

        EditorUtility.DisplayDialog("Icon Cám máy xay",
            "Đã sinh " + kho.Count + "/" + BANG.Length + " PNG.\n" +
            "Đã ghi " + daGhi + " ô Sprite.\n" +
            "Bỏ qua " + daBoQua + " ô, lỗi " + loi + " ô.\n\n" +
            "Báo cáo đầy đủ: Console và\n" + DUONG_DAN_BAO_CAO,
            "OK");
    }

    [MenuItem(MENU + "8. Icon Cam — Sinh + Gan", true)]
    private static bool ChoPhepSinhVaGan() { return !EditorApplication.isPlaying; }

    // ═════════════════════════════════════════════════════════════════════════════
    //  LỆNH 6 — HOÀN TÁC
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// HOÀN TÁC: trả các ô icon (`icon`, `food1Icon`, `premiumFoodIcon`) về sprite
    /// placeholder dùng chung `baothoc_0`. PNG sinh ra VẪN NẰM trên đĩa — muốn bỏ hẳn
    /// thì xoá thư mục <see cref="THU_MUC_RA"/> bằng tay.
    ///
    /// Chỉ hoàn ô nào đang trỏ vào PNG do CHÍNH tool này sinh; ô đang giữ art khác
    /// (kể cả art hoạ sĩ mới gán tay) thì để yên.
    ///
    /// ⚠ Ô `animalBadgeIcon` KHÔNG bị trả về placeholder — trả một cái bao thóc vào ô
    /// "icon con vật" là vô nghĩa. Bản sửa badge (trỏ về `Assets/Anh/`) được giữ lại;
    /// muốn về đúng trạng thái cũ (bản sao trong `Assets/thietke`) thì bấm Ctrl+Z.
    /// </summary>
    [MenuItem(MENU + "9. Icon Cam — Hoan tac gan", false, 9)]
    public static void HoanTacGan()
    {
        Sprite chan = NapSprite(ANH_BAO_THOC);
        if (chan == null)
        {
            EditorUtility.DisplayDialog("Icon Cám máy xay",
                "Không nạp được sprite placeholder:\n" + ANH_BAO_THOC + "\n\n" +
                "File này ở trong Assets/thietke — có thể đã bị lệnh " +
                "\"Dọn thư mục Assets/thietke\" xoá. Khi đó không còn gì để hoàn tác về; " +
                "hãy dùng Ctrl+Z hoặc gán tay.", "OK");
            return;
        }

        var bc = new StringBuilder();
        bc.AppendLine("═══════════════════════════════════════════════════════════════");
        bc.AppendLine(" ICON CÁM — HOÀN TÁC GÁN");
        bc.AppendLine("═══════════════════════════════════════════════════════════════");

        List<ODich> ds = DungKeHoach(bc);

        int seHoan = 0;
        for (int i = 0; i < ds.Count; i++)
            if (CanHoanTac(ds[i])) seHoan++;

        if (!EditorUtility.DisplayDialog("Icon Cám máy xay",
                "Trả " + seHoan + " ô icon về placeholder `baothoc_0`?\n\n" +
                "PNG trong " + THU_MUC_RA + " VẪN ĐƯỢC GIỮ trên đĩa.\n" +
                "Ô `animalBadgeIcon` không bị đụng tới (xem chú thích trong tool).",
                "HOÀN TÁC", "Huỷ"))
        {
            Debug.Log("[IconCam] Người dùng bấm Huỷ — không hoàn tác.");
            return;
        }

        bc.AppendLine();
        bc.AppendLine("── Ô ĐÃ TRẢ VỀ PLACEHOLDER ──");

        int daHoan = 0, daBo = 0;
        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            string nhan = "  " + Path.GetFileName(o.duongDanAsset) + " · " + o.tenO;

            if (!CanHoanTac(o))
            {
                daBo++;
                Sprite dang = o.doc();
                bc.AppendLine(nhan + "  — để yên (" +
                              (dang == null ? "trống" : "đang giữ " + dang.name) + ")");
                continue;
            }

            Sprite cu = o.doc();
            Undo.RecordObject(o.asset, "Hoan tac icon cam may xay");
            o.ghi(chan);
            EditorUtility.SetDirty(o.asset);
            daHoan++;
            bc.AppendLine(nhan + "  " + (cu == null ? "(trống)" : cu.name) + "  →  " + chan.name);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        bc.AppendLine();
        bc.AppendLine("── TỔNG ── hoàn " + daHoan + " ô · để yên " + daBo + " ô");
        bc.AppendLine("PNG vẫn còn ở " + THU_MUC_RA + " (xoá tay nếu muốn dọn hẳn).");

        XuatBaoCao(bc);
        EditorUtility.DisplayDialog("Icon Cám máy xay",
            "Đã trả " + daHoan + " ô về placeholder.\nĐể yên " + daBo + " ô.", "OK");
    }

    [MenuItem(MENU + "9. Icon Cam — Hoan tac gan", true)]
    private static bool ChoPhepHoanTac() { return !EditorApplication.isPlaying; }

    /// <summary>Chỉ hoàn ô icon đang trỏ vào PNG do tool sinh; không đụng badge.</summary>
    private static bool CanHoanTac(ODich o)
    {
        if (o.tenO == "animalBadgeIcon") return false;
        Sprite dang = o.doc();
        if (dang == null) return false;
        string p = AssetDatabase.GetAssetPath(dang);
        return p != null && p.StartsWith(THU_MUC_RA + "/", System.StringComparison.Ordinal);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  5. DỰNG KẾ HOẠCH GHI (dùng chung cho cả 3 lệnh)
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dựng danh sách mọi ô Sprite sẽ bị ghi, kèm lý do bỏ qua nếu có.
    /// Bảng chuồng KHÔNG hardcode: đọc `food1ItemId` / `premiumFoodItemId` của từng
    /// `PenMiniPanelConfig` rồi khớp với `recipeId` — nên đổi tên asset chuồng vẫn chạy đúng.
    /// </summary>
    private static List<ODich> DungKeHoach(StringBuilder bc)
    {
        var ds = new List<ODich>();

        // Nạp sẵn toàn bộ config chuồng, tránh FindAssets trong vòng lặp 4 dòng cám.
        List<PenMiniPanelConfig> chuong = NapChuong();

        bc.AppendLine();
        bc.AppendLine("── BẢNG CHUỒNG → CÁM (suy ra từ dữ liệu, không hardcode) ──");
        for (int i = 0; i < chuong.Count; i++)
        {
            PenMiniPanelConfig c = chuong[i];
            bc.AppendLine("  " + c.name +
                          "   penId=" + Chuoi(c.penId) +
                          "   food1ItemId=" + Chuoi(c.food1ItemId) +
                          "   premiumFoodItemId=" + Chuoi(c.premiumFoodItemId) +
                          "   productItemId=" + Chuoi(c.productItemId));
        }

        for (int i = 0; i < BANG.Length; i++)
        {
            DongCam d = BANG[i];
            string pngMoi = THU_MUC_RA + "/" + d.tenFileRa + ".png";

            // ── a) MillRecipe_*.asset : icon + animalBadgeIcon ───────────────────
            var ct = AssetDatabase.LoadAssetAtPath<MillRecipeData>(d.assetCongThuc);
            if (ct == null)
            {
                bc.AppendLine("  ⚠ không nạp được công thức " + d.assetCongThuc);
            }
            else
            {
                MillRecipeData ctCuc = ct;   // biến cục bộ cho closure, tránh bắt biến vòng lặp
                ds.Add(TaoO(ctCuc, d.assetCongThuc, "icon", "công thức", ctCuc.icon, pngMoi,
                            "recipeId = " + Chuoi(ctCuc.recipeId),
                            s => ctCuc.icon = s, () => ctCuc.icon));

                ds.Add(TaoO(ctCuc, d.assetCongThuc, "animalBadgeIcon", "công thức",
                            ctCuc.animalBadgeIcon, d.anhBadge,
                            "animalTag = " + Chuoi(ctCuc.animalTag),
                            s => ctCuc.animalBadgeIcon = s, () => ctCuc.animalBadgeIcon));
            }

            // ── b) Item_*.asset : icon (kho + bảng đơn dùng chung icon với máy xay) ──
            var vp = AssetDatabase.LoadAssetAtPath<InventoryItemData>(d.assetVatPham);
            if (vp == null)
            {
                bc.AppendLine("  ⚠ không nạp được vật phẩm " + d.assetVatPham);
            }
            else
            {
                InventoryItemData vpCuc = vp;
                ds.Add(TaoO(vpCuc, d.assetVatPham, "icon", "vật phẩm", vpCuc.icon, pngMoi,
                            "itemId = " + Chuoi(vpCuc.itemId),
                            s => vpCuc.icon = s, () => vpCuc.icon));
            }

            // ── c) Config_Pen0*.asset : food1Icon + premiumFoodIcon ──────────────
            for (int j = 0; j < chuong.Count; j++)
            {
                PenMiniPanelConfig cCuc = chuong[j];
                string duongDanChuong = AssetDatabase.GetAssetPath(cCuc);

                if (BangNhau(cCuc.food1ItemId, d.recipeId))
                    ds.Add(TaoO(cCuc, duongDanChuong, "food1Icon", "chuồng",
                                cCuc.food1Icon, pngMoi,
                                "food1ItemId = " + Chuoi(cCuc.food1ItemId),
                                s => cCuc.food1Icon = s, () => cCuc.food1Icon));

                if (BangNhau(cCuc.premiumFoodItemId, d.recipeId))
                    ds.Add(TaoO(cCuc, duongDanChuong, "premiumFoodIcon", "chuồng",
                                cCuc.premiumFoodIcon, pngMoi,
                                "premiumFoodItemId = " + Chuoi(cCuc.premiumFoodItemId),
                                s => cCuc.premiumFoodIcon = s, () => cCuc.premiumFoodIcon));
            }
        }

        return ds;
    }

    /// <summary>
    /// Tạo một ô và QUYẾT ĐỊNH có được ghi hay không.
    ///
    /// Luật (mượn ý "đã có icon thì không ghi đè" của TaskPopupSpriteWireTool):
    ///   • ô trống                            → ghi
    ///   • đang giữ placeholder `baothoc`     → ghi
    ///   • đang giữ bản sao trong Assets/thietke → ghi, và ĐÁNH DẤU SỬA LỖI
    ///     (thư mục đó là bản sao của bản thiết kế, có lệnh riêng xoá nó — trỏ vào
    ///      đấy là bom hẹn giờ: xoá thư mục là icon mất trắng)
    ///   • đang giữ đúng thứ cần gán          → trùng rồi, khỏi ghi
    ///   • đang giữ art KHÁC                  → BỎ QUA, không ghi đè công của hoạ sĩ
    /// </summary>
    private static ODich TaoO(ScriptableObject asset, string duongDanAsset, string tenO,
                              string nhomO, Sprite spriteCu, string duongDanMoi,
                              string bangChung,
                              System.Action<Sprite> ghi, System.Func<Sprite> doc)
    {
        var o = new ODich
        {
            asset         = asset,
            duongDanAsset = duongDanAsset,
            tenO          = tenO,
            nhomO         = nhomO,
            spriteCu      = spriteCu,
            duongDanMoi   = duongDanMoi,
            bangChung     = bangChung,
            lyDoBoQua     = string.Empty,
            trungRoi      = false,
            laSuaLoi      = false,
            ghi           = ghi,
            doc           = doc,
        };

        if (spriteCu == null) return o;   // ô trống → cứ ghi

        string duongDanCu = AssetDatabase.GetAssetPath(spriteCu);
        if (string.IsNullOrEmpty(duongDanCu)) return o;

        if (duongDanCu == duongDanMoi) { o.trungRoi = true; return o; }

        if (duongDanCu == ANH_BAO_THOC) return o;   // placeholder dùng chung → thay được

        if (duongDanCu.StartsWith("Assets/thietke/", System.StringComparison.Ordinal))
        {
            o.laSuaLoi  = true;
            o.bangChung = bangChung + "; đang trỏ bản sao " + duongDanCu +
                          " (thư mục thiết kế, sắp bị dọn)";
            return o;
        }

        // Đang giữ art riêng → KHÔNG ghi đè.
        o.lyDoBoQua = "đang giữ art riêng '" + spriteCu.name + "' (" + duongDanCu +
                      "), không phải placeholder → không ghi đè";
        return o;
    }

    private static void InKeHoach(List<ODich> ds, StringBuilder bc)
    {
        bc.AppendLine();
        bc.AppendLine("── Ô SẼ GHI ──");
        int ghi = 0, bo = 0, trung = 0;

        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            if (o.trungRoi || o.lyDoBoQua.Length > 0) continue;
            ghi++;
            bc.AppendLine("  " + o.duongDanAsset + "   (" + o.nhomO + ")");
            bc.AppendLine("      ." + o.tenO + "   " +
                          (o.spriteCu == null ? "(trống)" : o.spriteCu.name) +
                          "  →  " + Path.GetFileNameWithoutExtension(o.duongDanMoi) +
                          "   [" + o.bangChung + "]" + (o.laSuaLoi ? "   ★ SỬA LỖI" : ""));
        }

        bc.AppendLine();
        bc.AppendLine("── Ô TRÙNG SẴN (khỏi ghi) ──");
        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            if (!o.trungRoi) continue;
            trung++;
            bc.AppendLine("  " + Path.GetFileName(o.duongDanAsset) + " · " + o.tenO);
        }
        if (trung == 0) bc.AppendLine("  (không có)");

        bc.AppendLine();
        bc.AppendLine("── Ô BỎ QUA ──");
        for (int i = 0; i < ds.Count; i++)
        {
            ODich o = ds[i];
            if (o.trungRoi || o.lyDoBoQua.Length == 0) continue;
            bo++;
            bc.AppendLine("  " + o.duongDanAsset + " · " + o.tenO);
            bc.AppendLine("      ✘ " + o.lyDoBoQua);
        }
        if (bo == 0) bc.AppendLine("  (không có)");

        bc.AppendLine();
        bc.AppendLine("── TỔNG Ô ── ghi " + ghi + " · trùng sẵn " + trung + " · bỏ qua " + bo);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  6. SINH PNG
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Kích thước tính được cho một icon — dùng cho cả chạy khô và chạy thật.</summary>
    private class KhuonAnh
    {
        public int canh;                  // canvas VUÔNG canh×canh
        public int rongBao, caoBao;       // cái bao sau khi cắt hộp alpha
        public int datBaoX, datBaoY;      // góc dưới-trái để dán cái bao (bao nằm ĐÚNG TÂM)
        public int rongBadge, caoBadge;
        public int datBadgeX, datBadgeY;  // góc dưới-trái để dán badge
        public float tamBadgeX, tamBadgeY; // tâm badge THỰC (sau làm tròn) — hào quang dùng số này
        public float banKinhTron;         // r  = canh/2, bán kính đường tròn nội tiếp
        public float banKinhHaoQuang;     // rb = nửa đường chéo badge × 1.05 (đã gồm hào quang)
        public float khoangLech;          // d  đã GIẢI ra (trước khi làm tròn vị trí)
        public float khoangLechThuc;     // d  ĐO LẠI từ tâm badge thực — số dùng để soát
        public float duPhong;             // r − (d + rb): còn dư bao nhiêu px trước khi tràn
        public bool  daCoBadge;           // đã phải co badge nhỏ hơn TI_LE_BADGE để vừa vòng tròn
    }

    /// <summary>
    /// Tính trước kích thước canvas mà KHÔNG ghi file — lệnh 7 dùng để báo cáo.
    /// Trả null nếu thiếu ảnh nguồn.
    /// </summary>
    private static KhuonAnh TinhKhuonAnh(DongCam d, StringBuilder bc)
    {
        int rongNen, caoNen;
        Color32[] pxNen = NapPixel(d.anhNen, out rongNen, out caoNen, bc);
        if (pxNen == null) return null;

        int bx, by, bw, bh;
        if (!TimHopBao(pxNen, rongNen, caoNen, out bx, out by, out bw, out bh))
        {
            bc.AppendLine("      ⚠ " + d.anhNen + " toàn pixel trong suốt?");
            return null;
        }

        int gw, gh;
        string moTa;
        Color32[] badge = NapBadge(d, bc, out gw, out gh, out moTa);
        if (badge == null) return null;

        return TinhKhuon(bw, bh, gw, gh);
    }

    /// <summary>
    /// QUY TẮC BỐ CỤC — GIẢI RÀNG BUỘC HÌNH HỌC, KHÔNG CHỈNH TAY.
    /// Tách riêng để lệnh 7 và lệnh 8 chắc chắn tính GIỐNG NHAU.
    ///
    /// ══ RÀNG BUỘC PHẢI THOẢ ══
    ///     d + rb ≤ r − 4        (LE_AN_TOAN = 4px)
    /// với
    ///     r  = canh / 2                              bán kính đường tròn nội tiếp canvas vuông
    ///     rb = √(bw² + bh²) / 2 × 1.05               nửa đường chéo badge, ĐÃ GỒM hào quang
    ///     d  = |tâm ảnh − tâm badge|                 badge canh giữa ngang ⇒ d thuần theo trục Y
    ///
    /// ══ CÁCH GIẢI ══
    /// Đặt a = bh/bw (tỉ lệ ảnh badge, cố định theo art). Khi đó
    ///     rb = bw × 0.5 × √(1 + a²) × 1.05 = bw × heSoRb
    /// nên rb TỈ LỆ THUẬN với bw. Hai bước:
    ///
    ///   ① BỀ RỘNG BADGE. Muốn bw = canh × 0.32. Trường hợp XẤU NHẤT là d = 0 (badge nằm
    ///      giữa ảnh), lúc đó ràng buộc thu về rb ≤ r − 4, tức
    ///          bw ≤ (r − 4) / heSoRb
    ///      Lấy bw = min(mong muốn, chặn trên đó) ⇒ badge KHÔNG BAO GIỜ to quá vòng tròn,
    ///      dù ai đó sau này nâng TI_LE_BADGE lên 0.9.
    ///
    ///   ② ĐỘ TỤT XUỐNG. Có bw rồi thì rb là số cụ thể, giải ngược ra d lớn nhất còn hợp lệ:
    ///          d = r − rb − 4          (kẹp ≥ 0)
    ///      Lấy d = đúng cực đại đó, nên badge tụt SÂU NHẤT mà vẫn nằm trong vòng tròn.
    ///      Hệ quả đẹp: mép dưới hào quang = (r − d) − rb = 4 px với MỌI icon ⇒ badge của
    ///      cả 4 icon chạm cùng một đường ngang, dù tỉ lệ ảnh con vật khác nhau.
    ///
    /// ══ VÌ SAO KHÔNG PHẢI GÓC DƯỚI-PHẢI NỮA ══
    /// Ở góc thì d ≈ √2/2 × (canh/2 − …) ≈ 0.707 r, cộng thêm rb là vượt r ngay ⇒ tràn đĩa.
    /// Canh giữa ngang thì d chỉ tiêu tốn theo MỘT trục, ngân sách r đủ cho cả rb lẫn d.
    /// </summary>
    private static KhuonAnh TinhKhuon(int rongBao, int caoBao, int rongBadgeNguon, int caoBadgeNguon)
    {
        var k = new KhuonAnh { rongBao = rongBao, caoBao = caoBao };

        // Canvas VUÔNG, cái bao nằm đúng tâm.
        k.canh = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(rongBao, caoBao) * TI_LE_KHUNG));
        k.banKinhTron = k.canh * 0.5f;

        k.datBaoX = Mathf.RoundToInt(k.banKinhTron - rongBao * 0.5f);
        k.datBaoY = Mathf.RoundToInt(k.banKinhTron - caoBao  * 0.5f);

        // ① Bề rộng badge: mong muốn, rồi kẹp bằng chặn trên giải từ ràng buộc.
        float a       = (float)caoBadgeNguon / rongBadgeNguon;
        float heSoRb  = 0.5f * Mathf.Sqrt(1f + a * a) * TI_LE_HAO_QUANG;   // rb = bw × heSoRb
        float bwMuon  = k.canh * TI_LE_BADGE;
        float bwToiDa = (k.banKinhTron - LE_AN_TOAN) / heSoRb;
        float bw      = Mathf.Min(bwMuon, bwToiDa);

        k.daCoBadge = bwToiDa < bwMuon;
        k.rongBadge = Mathf.Max(1, Mathf.RoundToInt(bw));
        k.caoBadge  = Mathf.Max(1, Mathf.RoundToInt(k.rongBadge * a));

        // rb tính LẠI từ số nguyên thực tế, không dùng số thực ở trên — làm tròn có thể
        // đẩy badge to hơn 0,5px, phải tính ngân sách theo cái sẽ vẽ thật.
        k.banKinhHaoQuang = Mathf.Sqrt(k.rongBadge * (float)k.rongBadge +
                                       k.caoBadge  * (float)k.caoBadge) * 0.5f * TI_LE_HAO_QUANG;

        // ② Độ tụt xuống: cực đại còn hợp lệ, trừ 1px dự phòng làm tròn.
        k.khoangLech = Mathf.Max(0f, k.banKinhTron - k.banKinhHaoQuang
                                     - LE_AN_TOAN - DU_LAM_TRON);

        // Badge CANH GIỮA NGANG, tụt xuống dưới. Trục Y tính TỪ DƯỚI LÊN nên "xuống" là TRỪ.
        //
        // ⚠ CeilToInt cho trục Y, KHÔNG phải RoundToInt: làm tròn phải luôn đẩy badge LÊN
        //   (về phía tâm, tức d NHỎ đi), không bao giờ đẩy xuống. Ceil ⇒ datBadgeY ≥ giá trị
        //   thực ⇒ tamBadgeY ≥ r − d ⇒ d thực ≤ d đã giải ⇒ ràng buộc luôn còn thoả.
        //   Dùng RoundToInt thì nửa số trường hợp bị đẩy xuống 0,5px và ăn mất lề an toàn
        //   (đo thật: dư 3,6px thay vì 4px — chưa tràn nhưng đã mất ý nghĩa của hằng số).
        //   Trục X làm tròn thường vì lệch X cộng theo bình phương, 0,5px chỉ góp <0,01px.
        k.datBadgeX = Mathf.RoundToInt(k.banKinhTron - k.rongBadge * 0.5f);
        k.datBadgeY = Mathf.CeilToInt(k.banKinhTron - k.khoangLech - k.caoBadge * 0.5f);

        // Tâm THỰC sau làm tròn — hào quang phải đồng tâm với badge đã dán.
        k.tamBadgeX = k.datBadgeX + k.rongBadge * 0.5f;
        k.tamBadgeY = k.datBadgeY + k.caoBadge  * 0.5f;

        // Đo LẠI d từ tâm badge THỰC (khoảng cách 2 chiều, gồm cả lệch X do làm tròn)
        // rồi tính dư phòng so với r − 4. Số này phải ≥ 0; báo cáo in ra để soát được.
        float lechX = k.tamBadgeX - k.banKinhTron;
        float lechY = k.tamBadgeY - k.banKinhTron;
        k.khoangLechThuc = Mathf.Sqrt(lechX * lechX + lechY * lechY);
        k.duPhong = (k.banKinhTron - LE_AN_TOAN) - (k.khoangLechThuc + k.banKinhHaoQuang);
        return k;
    }

    /// <summary>
    /// Sinh MỘT PNG icon cám. Trả về đường dẫn asset, hoặc chuỗi rỗng nếu thất bại.
    /// Ghi chi tiết từng bước vào báo cáo.
    /// </summary>
    private static string SinhMotIcon(DongCam d, StringBuilder bc)
    {
        int rongNen, caoNen;
        Color32[] pxNen = NapPixel(d.anhNen, out rongNen, out caoNen, bc);
        if (pxNen == null) return string.Empty;

        int bx, by, bw, bh;
        if (!TimHopBao(pxNen, rongNen, caoNen, out bx, out by, out bw, out bh))
        {
            bc.AppendLine("  ✘ " + d.tenFileRa + " — ảnh nền toàn trong suốt: " + d.anhNen);
            return string.Empty;
        }

        Color32[] bao = CatVung(pxNen, rongNen, bx, by, bw, bh);

        if (d.gocMau >= 0f) NhuomMau(bao, d.gocMau, d.heSoBaoHoa, d.heSoSang);

        int gw, gh;
        string moTaBadge;
        Color32[] badgeGoc = NapBadge(d, bc, out gw, out gh, out moTaBadge);
        if (badgeGoc == null) return string.Empty;

        KhuonAnh k = TinhKhuon(bw, bh, gw, gh);
        Color32[] badge = ThuNhoHaiBuoc(badgeGoc, gw, gh, k.rongBadge, k.caoBadge);

        // ── Dựng canvas VUÔNG. Trục Y của Color32[] tính TỪ DƯỚI LÊN (đúng SetPixels32),
        //    nên badge "tụt xuống dưới" = y NHỎ hơn tâm.
        var canvas = new Color32[k.canh * k.canh];
        for (int i = 0; i < canvas.Length; i++) canvas[i] = new Color32(0, 0, 0, 0);

        Dan(canvas, k.canh, k.canh, bao, bw, bh, k.datBaoX, k.datBaoY);
        VeHaoQuang(canvas, k.canh, k.canh, k.tamBadgeX, k.tamBadgeY, k.banKinhHaoQuang);
        Dan(canvas, k.canh, k.canh, badge, k.rongBadge, k.caoBadge, k.datBadgeX, k.datBadgeY);

        string duongDan = THU_MUC_RA + "/" + d.tenFileRa + ".png";
        if (!GhiPng(duongDan, canvas, k.canh, k.canh, bc)) return string.Empty;

        bc.AppendLine("  ✔ " + d.tenFileRa + ".png   " + k.canh + "×" + k.canh + " px (vuông)");
        bc.AppendLine("      nền " + Path.GetFileName(d.anhNen) + " " + rongNen + "×" + caoNen +
                      " → cắt hộp alpha (" + bx + "," + by + ") " + bw + "×" + bh +
                      ", dán tâm tại (" + k.datBaoX + "," + k.datBaoY + ")");
        bc.AppendLine("      màu " + (d.gocMau < 0f
                      ? "GIỮ NGUYÊN (" + d.yDoMau + ")"
                      : "hue→" + d.gocMau.ToString("0") + "° sat×" + d.heSoBaoHoa.ToString("0.00") +
                        " val×" + d.heSoSang.ToString("0.00") + " (" + d.yDoMau + ")"));
        bc.AppendLine("      badge " + moTaBadge +
                      " → " + k.rongBadge + "×" + k.caoBadge +
                      (k.daCoBadge ? " (ĐÃ CO cho vừa vòng tròn)" : "") +
                      ", canh giữa ngang + tụt xuống");
        bc.AppendLine("      lấy mẫu: trung bình ô → " +
                      Mathf.Clamp(k.rongBadge * 2, k.rongBadge, gw) + "px → Lanczos3 → " +
                      k.rongBadge + "px → làm nét r=" + SHARP_BAN_KINH.ToString("0.0") +
                      " lượng=" + SHARP_LUONG.ToString("0.00"));
        bc.AppendLine("      " + SoatTron(k));
        bc.AppendLine("      bộ nhớ: RGBA32 không nén + mipmap = " +
                      (ByteIcon(k.canh, true) / 1024L) + " KiB (không mip: " +
                      (ByteIcon(k.canh, false) / 1024L) + " KiB), mipBias=" +
                      MIP_BIAS.ToString("0.0") + " aniso=1");
        return duongDan;
    }

    /// <summary>
    /// Một dòng soát ràng buộc <c>d + rb ≤ r − 4</c> cho báo cáo — in ra đủ số để người đọc
    /// tự cộng lại được, và nói thẳng THOẢ hay TRÀN.
    /// </summary>
    private static string SoatTron(KhuonAnh k)
    {
        bool thoa = k.duPhong >= 0f;
        return "vòng tròn: r=" + k.banKinhTron.ToString("0.0") +
               "  rb=" + k.banKinhHaoQuang.ToString("0.0") +
               "  d=" + k.khoangLechThuc.ToString("0.0") +
               "  ⇒ d+rb=" + (k.khoangLechThuc + k.banKinhHaoQuang).ToString("0.0") +
               " ≤ r−" + LE_AN_TOAN.ToString("0") + "=" +
               (k.banKinhTron - LE_AN_TOAN).ToString("0.0") +
               (thoa ? "  ✔ THOẢ (dư " + k.duPhong.ToString("0.0") + "px)"
                     : "  ✘ TRÀN " + (-k.duPhong).ToString("0.0") + "px — BÁO LẠI NGAY");
    }

    /// <summary>
    /// NẠP + CẮT ĐẦU CON VẬT, sẵn sàng đưa vào chuỗi thu nhỏ.
    ///
    /// Thứ tự BẮT BUỘC (làm sai là mất đúng cái độ phân giải đang cố mua):
    ///   ① hộp bao alpha của ảnh gốc      — bỏ khoảng trong suốt quanh con vật
    ///   ② cắt khung đầu theo phân số      — <see cref="DongCam.khungBadge"/>
    ///   ③ hộp bao alpha LẦN HAI           — khung có thể chừa lại viền trong suốt mới
    ///   ④ (nơi gọi) mới thu nhỏ
    /// Cắt SAU khi thu nhỏ thì vô nghĩa: lúc đó chi tiết đã mất rồi.
    ///
    /// Tách thành hàm dùng chung cho lệnh 7 và lệnh 8 để hai lệnh KHÔNG THỂ tính lệch nhau.
    /// </summary>
    private static Color32[] NapBadge(DongCam d, StringBuilder bc,
                                      out int rong, out int cao, out string moTa)
    {
        rong = 0; cao = 0; moTa = string.Empty;

        int rongGoc, caoGoc;
        Color32[] pxGoc = NapPixel(d.anhBadge, out rongGoc, out caoGoc, bc);
        if (pxGoc == null) return null;

        // ① hộp bao alpha
        int ax, ay, aw, ah;
        if (!TimHopBao(pxGoc, rongGoc, caoGoc, out ax, out ay, out aw, out ah))
        {
            bc.AppendLine("      ⚠ " + d.anhBadge + " toàn pixel trong suốt?");
            return null;
        }
        Color32[] hopBao = CatVung(pxGoc, rongGoc, ax, ay, aw, ah);

        // ② cắt khung đầu
        int kw, kh;
        Color32[] khung = CatKhungTiLe(hopBao, aw, ah, d.khungBadge, out kw, out kh);

        // ③ hộp bao alpha lần hai
        int bx2, by2, bw2, bh2;
        if (!TimHopBao(khung, kw, kh, out bx2, out by2, out bw2, out bh2))
        {
            bc.AppendLine("      ⚠ khung cắt đầu của " + d.anhBadge +
                          " không còn pixel nào — kiểm lại toạ độ khung.");
            return null;
        }
        Color32[] ra = CatVung(khung, kw, bx2, by2, bw2, bh2);

        rong = bw2; cao = bh2;
        bool coCat = kw != aw || kh != ah;
        moTa = Path.GetFileName(d.anhBadge) + " " + rongGoc + "×" + caoGoc +
               " → bao alpha " + aw + "×" + ah +
               (coCat
                 ? " → khung đầu (" + d.khungBadge.x.ToString("0.00") + "," +
                   d.khungBadge.y.ToString("0.00") + "," + d.khungBadge.z.ToString("0.00") + "," +
                   d.khungBadge.w.ToString("0.00") + ") " + kw + "×" + kh +
                   " → bao alpha lần 2 " + bw2 + "×" + bh2
                 : " → GIỮ CẢ ẢNH " + bw2 + "×" + bh2);
        return ra;
    }

    /// <summary>
    /// Cắt theo khung PHÂN SỐ. <paramref name="khung"/> = (x0, yTrên0, x1, yTrên1) với
    /// y tính TỪ TRÊN XUỐNG (như CSS), còn mảng Color32[] ở đây y tính TỪ DƯỚI LÊN
    /// (quy ước SetPixels32).
    ///
    /// ⚠ CHÍNH CHỖ NÀY DỄ SAI NHẤT CỦA CẢ TOOL: nhầm chiều Y thì tool cắt lấy CHÂN con vật
    ///   thay vì ĐẦU, mà nó vẫn chạy êm không báo lỗi gì. Phép đổi:
    ///       hàng dưới-lên  = [ cao × (1 − yTrên1) , cao × (1 − yTrên0) )
    /// </summary>
    private static Color32[] CatKhungTiLe(Color32[] px, int rong, int cao, Vector4 khung,
                                          out int rongRa, out int caoRa)
    {
        int xa = Mathf.Clamp(Mathf.RoundToInt(khung.x * rong), 0, rong - 1);
        int xb = Mathf.Clamp(Mathf.RoundToInt(khung.z * rong), xa + 1, rong);

        // Đổi chiều Y: phân số tính từ TRÊN, mảng đánh số từ DƯỚI.
        int ya = Mathf.Clamp(Mathf.RoundToInt((1f - khung.w) * cao), 0, cao - 1);
        int yb = Mathf.Clamp(Mathf.RoundToInt((1f - khung.y) * cao), ya + 1, cao);

        rongRa = xb - xa;
        caoRa  = yb - ya;
        return CatVung(px, rong, xa, ya, rongRa, caoRa);
    }

    // ── Đọc pixel ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc pixel của một PNG trong project.
    ///
    /// ⚠ KHÔNG dùng <c>LoadAssetAtPath&lt;Texture2D&gt;().GetPixels()</c>: mọi ảnh nguồn ở
    /// đây đều `isReadable: 0` nên hàm đó NÉM LỖI. Và KHÔNG được bật Read/Write rồi tắt lại —
    /// sửa importer của art gốc là sửa thứ không phải của mình.
    /// Cách chắc ăn: đọc byte thô rồi <c>ImageConversion.LoadImage</c> vào texture tạm.
    /// </summary>
    private static Color32[] NapPixel(string duongDanAsset, out int rong, out int cao, StringBuilder bc)
    {
        rong = 0; cao = 0;

        string tuyetDoi = Path.Combine(Directory.GetCurrentDirectory(), duongDanAsset);
        if (!File.Exists(tuyetDoi))
        {
            bc.AppendLine("      ✘ không thấy file: " + duongDanAsset);
            return null;
        }

        byte[] byteAnh;
        try { byteAnh = File.ReadAllBytes(tuyetDoi); }
        catch (System.Exception e)
        {
            bc.AppendLine("      ✘ đọc không được " + duongDanAsset + " — " + e.Message);
            return null;
        }

        var tam = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!ImageConversion.LoadImage(tam, byteAnh, false))
        {
            UnityEngine.Object.DestroyImmediate(tam);
            bc.AppendLine("      ✘ giải mã PNG thất bại: " + duongDanAsset);
            return null;
        }

        rong = tam.width;
        cao  = tam.height;
        Color32[] px = tam.GetPixels32();
        UnityEngine.Object.DestroyImmediate(tam);
        return px;
    }

    /// <summary>
    /// Hộp bao của phần KHÔNG trong suốt. Tự tính từ pixel chứ không đọc metadata slice
    /// trong .meta — có ảnh chưa slice, và slice có thể lệch sau khi ai đó sửa importer.
    /// </summary>
    private static bool TimHopBao(Color32[] px, int rong, int cao,
                                  out int x0, out int y0, out int w, out int h)
    {
        int minX = rong, minY = cao, maxX = -1, maxY = -1;

        for (int y = 0; y < cao; y++)
        {
            int hang = y * rong;
            for (int x = 0; x < rong; x++)
            {
                if (px[hang + x].a < NGUONG_ALPHA) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0)
        {
            x0 = 0; y0 = 0; w = 0; h = 0;
            return false;
        }

        x0 = minX; y0 = minY;
        w  = maxX - minX + 1;
        h  = maxY - minY + 1;
        return true;
    }

    private static Color32[] CatVung(Color32[] px, int rongNguon, int x0, int y0, int w, int h)
    {
        var ra = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            int nguon = (y0 + y) * rongNguon + x0;
            int dich  = y * w;
            for (int x = 0; x < w; x++) ra[dich + x] = px[nguon + x];
        }
        return ra;
    }

    // ── Nhuộm màu ───────────────────────────────────────────────────────────────

    /// <summary>
    /// THAY hue, NHÂN saturation và value, GIỮ NGUYÊN alpha.
    ///
    /// Vì sao nhân value chứ không gán: gán value = mọi pixel sáng bằng nhau ⇒ cái bao
    /// bẹt như miếng giấy dán, mất hết nếp gấp. Nhân thì giữ nguyên TỈ LỆ sáng-tối.
    /// Vì sao bỏ qua pixel bão hoà thấp: đó là viền đen, điểm sáng trắng và chữ trên bao —
    /// nhuộm chúng thì viền chuyển màu, hình mất nét.
    /// </summary>
    private static void NhuomMau(Color32[] px, float gocMau, float heSoBaoHoa, float heSoSang)
    {
        float hue = Mathf.Repeat(gocMau, 360f) / 360f;

        for (int i = 0; i < px.Length; i++)
        {
            Color32 c = px[i];
            if (c.a < NGUONG_ALPHA) continue;

            float h, s, v;
            Color.RGBToHSV(new Color(c.r / 255f, c.g / 255f, c.b / 255f), out h, out s, out v);
            if (s < NGUONG_BAO_HOA) continue;   // xám/trắng/đen → để nguyên

            Color moi = Color.HSVToRGB(hue,
                                       Mathf.Clamp01(s * heSoBaoHoa),
                                       Mathf.Clamp01(v * heSoSang));

            px[i] = new Color32((byte)(moi.r * 255f + 0.5f),
                                (byte)(moi.g * 255f + 0.5f),
                                (byte)(moi.b * 255f + 0.5f),
                                c.a);
        }
    }

    // ── Thu nhỏ badge ───────────────────────────────────────────────────────────

    /// <summary>
    /// CHUỖI THU NHỎ BADGE 2 BƯỚC + LÀM NÉT — cách chữa "ảnh con vật bị lem".
    ///
    /// ══ VÌ SAO KHÔNG THU MỘT BƯỚC ══
    /// Bản trước thu thẳng 231px → 76px bằng trung bình ô. Trung bình ô là bộ lọc hộp:
    /// chống răng cưa tốt nhưng làm mềm, vì nó cân đều mọi pixel trong ô, kể cả pixel
    /// ở rìa ô đáng ra phải nhẹ hơn. Thu 3× một nhát thì mất hết mép.
    ///
    /// ══ CHUỖI MỚI ══
    ///   ① TRUNG BÌNH Ô về ~2× đích (231→152). Bước này gánh phần chống răng cưa —
    ///      giảm tần số cao xuống dưới ngưỡng Nyquist của đích trước khi lọc nét.
    ///   ② LANCZOS3 về đúng đích (152→76). Nhân Lanczos có thuỳ âm nên giữ mép,
    ///      chạy ở tỉ lệ 2× thì gần như không sinh quầng.
    ///   ③ LÀM NÉT NHẸ (unsharp) dựng lại mép đã mất qua hai lần lấy mẫu.
    ///
    /// Cả ba bước chạy trên màu ĐÃ NHÂN ALPHA: trộn màu mà không nhân alpha trước thì
    /// pixel trong suốt (đen trong suốt) rỉ màu đen ra viền ⇒ con vật bị quầng đen.
    ///
    /// ⚠ Chỉ dùng cho THU NHỎ. Nguồn badge 231–385px, đích ~76px nên luôn thu.
    /// </summary>
    private static Color32[] ThuNhoHaiBuoc(Color32[] nguon, int rongNguon, int caoNguon,
                                           int rongDich, int caoDich)
    {
        // ① Trung bình ô về ~2× đích (không bao giờ phóng to, không bao giờ nhỏ hơn đích).
        int trungRong = Mathf.Clamp(rongDich * 2, rongDich, rongNguon);
        int trungCao  = Mathf.Clamp(caoDich  * 2, caoDich,  caoNguon);

        Color32[] buoc1;
        if (trungRong == rongNguon && trungCao == caoNguon)
        {
            buoc1 = nguon;                     // nguồn đã nhỏ hơn 2× đích → khỏi bước ①
        }
        else
        {
            buoc1 = ThuNhoTrungBinh(nguon, rongNguon, caoNguon, trungRong, trungCao);
        }

        // ② Lanczos3 về đúng đích.
        Color32[] buoc2 = LanczosThuNho(buoc1, trungRong, trungCao, rongDich, caoDich);

        // ③ Làm nét nhẹ.
        LamNet(buoc2, rongDich, caoDich, SHARP_BAN_KINH, SHARP_LUONG);
        return buoc2;
    }

    /// <summary>Nhân Lanczos bậc <see cref="LANCZOS_A"/>: sinc(x) · sinc(x/a).</summary>
    private static float NhanLanczos(float x)
    {
        if (x < 0f) x = -x;
        if (x < 1e-6f) return 1f;
        if (x >= LANCZOS_A) return 0f;
        float pix = Mathf.PI * x;
        return (Mathf.Sin(pix) / pix) * (Mathf.Sin(pix / LANCZOS_A) / (pix / LANCZOS_A));
    }

    /// <summary>Trọng số Lanczos đã chuẩn hoá cho MỘT trục — tính 1 lần, dùng cho mọi hàng/cột.</summary>
    private class BoLoc
    {
        public int[]   dau;      // chỉ số mẫu nguồn đầu tiên của mỗi pixel đích
        public int[]   dem;      // số mẫu của mỗi pixel đích
        public float[] tr;       // trọng số, gộp phẳng theo bước `oMax`
        public int     oMax;
    }

    private static BoLoc DungBoLoc(int nguon, int dich)
    {
        float tiLe = (float)dich / nguon;             // < 1 khi thu nhỏ
        float coGian = tiLe < 1f ? 1f / tiLe : 1f;    // 1 pixel đích trải bao nhiêu pixel nguồn
        float doPhu = LANCZOS_A * coGian;
        int oMax = Mathf.CeilToInt(doPhu * 2f) + 2;

        var b = new BoLoc
        {
            dau  = new int[dich],
            dem  = new int[dich],
            tr   = new float[dich * oMax],
            oMax = oMax,
        };

        for (int i = 0; i < dich; i++)
        {
            float tam = (i + 0.5f) * coGian - 0.5f;             // tâm trong toạ độ nguồn
            int i0 = Mathf.Max(0, Mathf.CeilToInt(tam - doPhu));
            int i1 = Mathf.Min(nguon - 1, Mathf.FloorToInt(tam + doPhu));
            if (i1 < i0) { i0 = Mathf.Clamp(Mathf.RoundToInt(tam), 0, nguon - 1); i1 = i0; }

            int n = Mathf.Min(i1 - i0 + 1, oMax);
            b.dau[i] = i0;
            b.dem[i] = n;

            int nen = i * oMax;
            float tong = 0f;
            for (int j = 0; j < n; j++)
            {
                float w = NhanLanczos((i0 + j - tam) / coGian);
                b.tr[nen + j] = w;
                tong += w;
            }

            if (Mathf.Abs(tong) < 1e-6f)
            {
                for (int j = 0; j < n; j++) b.tr[nen + j] = j == 0 ? 1f : 0f;
            }
            else
            {
                float nghichDao = 1f / tong;
                for (int j = 0; j < n; j++) b.tr[nen + j] *= nghichDao;
            }
        }
        return b;
    }

    /// <summary>
    /// Thu nhỏ bằng Lanczos3 tách trục (ngang rồi dọc), chạy trên màu ĐÃ NHÂN ALPHA.
    /// Kẹp về 0..255 sau mỗi trục vì thuỳ âm của Lanczos có thể cho giá trị âm / quá 255.
    /// </summary>
    private static Color32[] LanczosThuNho(Color32[] nguon, int rongNguon, int caoNguon,
                                           int rongDich, int caoDich)
    {
        // Nhân alpha, đổi sang float 4 kênh.
        var pmNguon = new float[rongNguon * caoNguon * 4];
        for (int i = 0; i < rongNguon * caoNguon; i++)
        {
            Color32 c = nguon[i];
            float a = c.a / 255f;
            pmNguon[i * 4 + 0] = c.r * a;
            pmNguon[i * 4 + 1] = c.g * a;
            pmNguon[i * 4 + 2] = c.b * a;
            pmNguon[i * 4 + 3] = c.a;
        }

        // Trục ngang: (rongNguon × caoNguon) → (rongDich × caoNguon)
        BoLoc bx = DungBoLoc(rongNguon, rongDich);
        var giua = new float[rongDich * caoNguon * 4];
        for (int y = 0; y < caoNguon; y++)
        {
            int hangN = y * rongNguon;
            int hangG = y * rongDich;
            for (int x = 0; x < rongDich; x++)
            {
                int nen = x * bx.oMax;
                int i0 = bx.dau[x], n = bx.dem[x];
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int j = 0; j < n; j++)
                {
                    float w = bx.tr[nen + j];
                    int src = (hangN + i0 + j) * 4;
                    r += pmNguon[src + 0] * w;
                    g += pmNguon[src + 1] * w;
                    b += pmNguon[src + 2] * w;
                    a += pmNguon[src + 3] * w;
                }
                int dst = (hangG + x) * 4;
                giua[dst + 0] = r; giua[dst + 1] = g; giua[dst + 2] = b; giua[dst + 3] = a;
            }
        }

        // Trục dọc: (rongDich × caoNguon) → (rongDich × caoDich)
        BoLoc by = DungBoLoc(caoNguon, caoDich);
        var ra = new Color32[rongDich * caoDich];
        for (int y = 0; y < caoDich; y++)
        {
            int nen = y * by.oMax;
            int j0 = by.dau[y], n = by.dem[y];
            for (int x = 0; x < rongDich; x++)
            {
                float r = 0f, g = 0f, b = 0f, a = 0f;
                for (int j = 0; j < n; j++)
                {
                    float w = by.tr[nen + j];
                    int src = ((j0 + j) * rongDich + x) * 4;
                    r += giua[src + 0] * w;
                    g += giua[src + 1] * w;
                    b += giua[src + 2] * w;
                    a += giua[src + 3] * w;
                }

                // Bỏ nhân alpha.
                if (a <= 0.5f) { ra[y * rongDich + x] = new Color32(0, 0, 0, 0); continue; }
                float k = 255f / a;
                ra[y * rongDich + x] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(r * k), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(g * k), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(b * k), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(a),     0, 255));
            }
        }
        return ra;
    }

    /// <summary>
    /// LÀM NÉT (unsharp mask) tại chỗ: ra = goc + luong × (goc − mờ(goc)).
    ///
    /// Chỉ tác động RGB, KHÔNG chạm alpha: làm nét alpha thì mép silhouette thành bậc thang
    /// cứng và sinh quầng, mà độ nét của mép đã do Lanczos lo.
    /// Mờ bằng Gauss tách trục, bán kính nhỏ (0,8px ở độ phân giải bake).
    /// </summary>
    private static void LamNet(Color32[] px, int rong, int cao, float banKinh, float luong)
    {
        if (luong <= 0f || banKinh <= 0f) return;

        // Nhân Gauss 1 chiều.
        int bk = Mathf.Max(1, Mathf.CeilToInt(banKinh * 3f));
        var nhan = new float[bk * 2 + 1];
        float hai = 2f * banKinh * banKinh;
        float tong = 0f;
        for (int i = -bk; i <= bk; i++)
        {
            float w = Mathf.Exp(-(i * i) / hai);
            nhan[i + bk] = w;
            tong += w;
        }
        for (int i = 0; i < nhan.Length; i++) nhan[i] /= tong;

        int n = rong * cao;

        // Mờ ngang → tạm, rồi mờ dọc → mo. Chỉ 3 kênh RGB.
        var tam = new float[n * 3];
        for (int y = 0; y < cao; y++)
        {
            int hang = y * rong;
            for (int x = 0; x < rong; x++)
            {
                float r = 0f, g = 0f, b = 0f;
                for (int i = -bk; i <= bk; i++)
                {
                    int xx = Mathf.Clamp(x + i, 0, rong - 1);
                    Color32 c = px[hang + xx];
                    float w = nhan[i + bk];
                    r += c.r * w; g += c.g * w; b += c.b * w;
                }
                int d = (hang + x) * 3;
                tam[d] = r; tam[d + 1] = g; tam[d + 2] = b;
            }
        }

        var mo = new float[n * 3];
        for (int y = 0; y < cao; y++)
        {
            for (int x = 0; x < rong; x++)
            {
                float r = 0f, g = 0f, b = 0f;
                for (int i = -bk; i <= bk; i++)
                {
                    int yy = Mathf.Clamp(y + i, 0, cao - 1);
                    int s = (yy * rong + x) * 3;
                    float w = nhan[i + bk];
                    r += tam[s] * w; g += tam[s + 1] * w; b += tam[s + 2] * w;
                }
                int d = (y * rong + x) * 3;
                mo[d] = r; mo[d + 1] = g; mo[d + 2] = b;
            }
        }

        for (int i = 0; i < n; i++)
        {
            Color32 c = px[i];
            int m = i * 3;
            px[i] = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.r + luong * (c.r - mo[m])),     0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.g + luong * (c.g - mo[m + 1])), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(c.b + luong * (c.b - mo[m + 2])), 0, 255),
                c.a);
        }
    }

    /// <summary>
    /// BƯỚC ① của <see cref="ThuNhoHaiBuoc"/>: thu nhỏ bằng TRUNG BÌNH Ô (box filter)
    /// trên màu ĐÃ NHÂN ALPHA. Dùng để hạ tần số cao trước khi Lanczos, không dùng một mình.
    /// </summary>
    private static Color32[] ThuNhoTrungBinh(Color32[] nguon, int rongNguon, int caoNguon,
                                             int rongDich, int caoDich)
    {
        var ra = new Color32[rongDich * caoDich];
        float buocX = (float)rongNguon / rongDich;
        float buocY = (float)caoNguon  / caoDich;

        for (int y = 0; y < caoDich; y++)
        {
            int y1 = Mathf.Clamp(Mathf.FloorToInt(y * buocY), 0, caoNguon - 1);
            int y2 = Mathf.Clamp(Mathf.CeilToInt((y + 1) * buocY), y1 + 1, caoNguon);

            for (int x = 0; x < rongDich; x++)
            {
                int x1 = Mathf.Clamp(Mathf.FloorToInt(x * buocX), 0, rongNguon - 1);
                int x2 = Mathf.Clamp(Mathf.CeilToInt((x + 1) * buocX), x1 + 1, rongNguon);

                float tongR = 0f, tongG = 0f, tongB = 0f, tongA = 0f;
                int dem = 0;

                for (int sy = y1; sy < y2; sy++)
                {
                    int hang = sy * rongNguon;
                    for (int sx = x1; sx < x2; sx++)
                    {
                        Color32 c = nguon[hang + sx];
                        float a = c.a / 255f;
                        tongR += c.r * a;
                        tongG += c.g * a;
                        tongB += c.b * a;
                        tongA += a;
                        dem++;
                    }
                }

                if (dem == 0 || tongA <= 0.0001f) { ra[y * rongDich + x] = new Color32(0, 0, 0, 0); continue; }

                // Chia lại cho tổng alpha để bỏ phép nhân alpha (un-premultiply).
                byte r = (byte)Mathf.Clamp(Mathf.RoundToInt(tongR / tongA), 0, 255);
                byte g = (byte)Mathf.Clamp(Mathf.RoundToInt(tongG / tongA), 0, 255);
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(tongB / tongA), 0, 255);
                byte a2 = (byte)Mathf.Clamp(Mathf.RoundToInt(tongA / dem * 255f), 0, 255);
                ra[y * rongDich + x] = new Color32(r, g, b, a2);
            }
        }
        return ra;
    }

    // ── Ghép ảnh ────────────────────────────────────────────────────────────────

    /// <summary>Dán lớp trên vào canvas theo phép trộn src-over chuẩn.</summary>
    private static void Dan(Color32[] canvas, int rongCanvas, int caoCanvas,
                            Color32[] lop, int rongLop, int caoLop, int datX, int datY)
    {
        for (int y = 0; y < caoLop; y++)
        {
            int cy = datY + y;
            if (cy < 0 || cy >= caoCanvas) continue;

            int hangLop    = y * rongLop;
            int hangCanvas = cy * rongCanvas;

            for (int x = 0; x < rongLop; x++)
            {
                int cx = datX + x;
                if (cx < 0 || cx >= rongCanvas) continue;

                Color32 tren = lop[hangLop + x];
                if (tren.a == 0) continue;

                int chiSo = hangCanvas + cx;
                canvas[chiSo] = tren.a == 255 ? tren : Tron(canvas[chiSo], tren);
            }
        }
    }

    /// <summary>
    /// Hào quang trắng mờ hình tròn, đặt DƯỚI badge và TRÊN thân bao, để badge đọc được
    /// dù thân bao cùng tông sáng (rõ nhất ở icon bò sữa: bình sữa trắng trên bao xanh nhạt).
    /// </summary>
    private static void VeHaoQuang(Color32[] canvas, int rongCanvas, int caoCanvas,
                                   float tamX, float tamY, float banKinh)
    {
        byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(ALPHA_HAO_QUANG * 255f), 0, 255);
        var trang = new Color32(255, 255, 255, alpha);

        int x1 = Mathf.Max(0, Mathf.FloorToInt(tamX - banKinh));
        int x2 = Mathf.Min(rongCanvas - 1, Mathf.CeilToInt(tamX + banKinh));
        int y1 = Mathf.Max(0, Mathf.FloorToInt(tamY - banKinh));
        int y2 = Mathf.Min(caoCanvas - 1, Mathf.CeilToInt(tamY + banKinh));
        float r2 = banKinh * banKinh;

        for (int y = y1; y <= y2; y++)
        {
            int hang = y * rongCanvas;
            float dy = y + 0.5f - tamY;
            for (int x = x1; x <= x2; x++)
            {
                float dx = x + 0.5f - tamX;
                if (dx * dx + dy * dy > r2) continue;
                canvas[hang + x] = Tron(canvas[hang + x], trang);
            }
        }
    }

    /// <summary>Trộn alpha src-over trên alpha THẲNG (không nhân trước).</summary>
    private static Color32 Tron(Color32 duoi, Color32 tren)
    {
        float at = tren.a / 255f;
        if (at >= 1f) return tren;
        if (at <= 0f) return duoi;

        float ad = duoi.a / 255f;
        float ar = at + ad * (1f - at);
        if (ar <= 0.0001f) return new Color32(0, 0, 0, 0);

        float k = ad * (1f - at);
        byte r = (byte)Mathf.Clamp(Mathf.RoundToInt((tren.r * at + duoi.r * k) / ar), 0, 255);
        byte g = (byte)Mathf.Clamp(Mathf.RoundToInt((tren.g * at + duoi.g * k) / ar), 0, 255);
        byte b = (byte)Mathf.Clamp(Mathf.RoundToInt((tren.b * at + duoi.b * k) / ar), 0, 255);
        byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(ar * 255f), 0, 255);
        return new Color32(r, g, b, a);
    }

    // ── Ghi PNG + import settings ───────────────────────────────────────────────

    /// <summary>
    /// Ghi PNG rồi import ĐỒNG BỘ và áp import settings.
    /// Chỉ ghi khi byte KHÁC file cũ, để không làm git nhận diff vô nghĩa.
    /// KHÔNG bọc StartAssetEditing — import bị hoãn thì LoadAssetAtPath ngay sau trả null
    /// (đúng cạm bẫy đã ghi trong MillSpriteFactory).
    /// </summary>
    private static bool GhiPng(string duongDanAsset, Color32[] px, int rong, int cao, StringBuilder bc)
    {
        var tex = new Texture2D(rong, cao, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        byte[] moi = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        string tuyetDoi = Path.Combine(Directory.GetCurrentDirectory(), duongDanAsset);

        bool giongCu = false;
        if (File.Exists(tuyetDoi))
        {
            byte[] cu = File.ReadAllBytes(tuyetDoi);
            giongCu = cu.Length == moi.Length;
            if (giongCu)
                for (int i = 0; i < cu.Length; i++)
                    if (cu[i] != moi[i]) { giongCu = false; break; }
        }

        if (!giongCu)
        {
            try { File.WriteAllBytes(tuyetDoi, moi); }
            catch (System.Exception e)
            {
                bc.AppendLine("  ✘ ghi không được " + duongDanAsset + " — " + e.Message);
                return false;
            }
            AssetDatabase.ImportAsset(duongDanAsset,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        var imp = AssetImporter.GetAtPath(duongDanAsset) as TextureImporter;
        if (imp == null)
        {
            bc.AppendLine("  ✘ không lấy được TextureImporter cho " + duongDanAsset);
            return false;
        }
        ApDatImport(imp);
        return true;
    }

    /// <summary>
    /// ÁP IMPORT SETTINGS CHO PNG DO TOOL SINH.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    ///  VÌ SAO BẬT MIPMAP CHO MỘT TEXTURE UI (nghe như sai, nhưng đúng ở đây)
    /// ══════════════════════════════════════════════════════════════════════════════
    /// Quy ước chung là UI thì TẮT mipmap, vì sprite UI thường được vẽ đúng 1:1 nên mip
    /// chỉ tốn thêm 33% bộ nhớ mà không dùng. Icon cám KHÔNG ở trường hợp đó: MỘT texture
    /// 225px phục vụ BA cỡ vẽ khác nhau —
    ///     đĩa slot   64px   (MillDesign.SlotIconImg)
    ///     card       98px   (MillDesign.CardIconImg)
    ///     thành phẩm 192px  (MillDesign.OutIcon)
    /// Ở 64px là thu 3,5 lần. Không có mip thì GPU lấy mẫu bilinear 2×2 trực tiếp từ ảnh
    /// 225px: mỗi pixel màn hình chỉ "nhìn" 4 trong ~12 pixel texture mà nó phủ, phần còn
    /// lại bị bỏ ⇒ răng cưa và nhảy pixel khi scroll, mắt đọc ra là "lem".
    /// Có mip thì GPU lấy mip đã lọc sẵn đúng cỡ. Đây chính là ca mipmap sinh ra để giải.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    ///  VÌ SAO PHẢI DẬP NÉN CHO TỪNG BUILD TARGET, KHÔNG CHỈ ENTRY DEFAULT
    /// ══════════════════════════════════════════════════════════════════════════════
    /// `imp.textureCompression` CHỈ ghi vào entry `DefaultTexturePlatform`. File .meta còn
    /// entry riêng cho từng build target. Đọc .meta của bản đã deploy thấy đúng như vậy:
    ///     buildTarget: DefaultTexturePlatform → textureCompression: 0   (Uncompressed ✔)
    ///     buildTarget: Standalone             → textureCompression: 1   (Normal ✘)
    /// Entry Standalone đang `overridden: 0` nên CHƯA có hiệu lực — nhưng nó là bom hẹn giờ:
    /// chỉ cần ai đó tick "Override for PC" một lần là 4 icon bị nén block ngay, mà nén
    /// block trên ảnh nhiều chi tiết thì phá mép theo ô 4×4. Nên set Uncompressed cho MỌI
    /// entry đang tồn tại, không chỉ Default.
    ///
    /// ══════════════════════════════════════════════════════════════════════════════
    ///  GIÁ PHẢI TRẢ VỀ BỘ NHỚ — QUYẾT ĐỊNH CÓ CHỦ Ý, KHÔNG PHẢI TAI NẠN
    /// ══════════════════════════════════════════════════════════════════════════════
    /// RGBA32 không nén, canvas 225×225:
    ///     225 × 225 × 4 B            = 202.500 B ≈ 198 KiB / icon
    ///     × 4 icon                   = 810.000 B ≈ 791 KiB
    ///     + chuỗi mip (thêm ~1/3)    ≈ 1.077.504 B ≈ 1,03 MiB TỔNG
    /// Đổi 1 MiB để 4 icon hết lem, trên một game farm 2D mà texture atlas đã hàng chục MiB.
    /// Nếu sau này cần siết: hạ <see cref="TI_LE_KHUNG"/> hoặc bake icon 160px thay vì 225px
    /// sẽ tiết kiệm nhiều hơn là bật nén lại.
    ///
    /// ⚠ GỌI SaveAndReimport() VÔ ĐIỀU KIỆN. Bản trước chỉ gọi khi phát hiện "có thay đổi",
    ///   mà phép so sánh đó không kiểm entry nén theo từng nền tảng lẫn mipMapBias ⇒ có
    ///   trường hợp gán xong rồi mất vì không lưu. 4 texture thì reimport thừa không đáng kể;
    ///   mất setting mà không báo lỗi thì đáng kể.
    /// </summary>
    private static void ApDatImport(TextureImporter imp)
    {
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PPU;
        imp.alphaIsTransparency = true;
        imp.spriteBorder        = Vector4.zero;
        imp.wrapMode            = TextureWrapMode.Clamp;
        imp.filterMode          = FilterMode.Bilinear;
        imp.anisoLevel          = 1;
        imp.mipmapEnabled       = true;
        imp.mipMapBias          = MIP_BIAS;
        imp.textureCompression  = TextureImporterCompression.Uncompressed;

        // Entry Default — set tường minh qua API platform settings cho chắc.
        TextureImporterPlatformSettings mac = imp.GetDefaultPlatformTextureSettings();
        if (mac != null)
        {
            mac.textureCompression = TextureImporterCompression.Uncompressed;
            mac.format             = TextureImporterFormat.Automatic;
            mac.crunchedCompression = false;
            imp.SetPlatformTextureSettings(mac);
        }

        // Từng build target có entry riêng — dập nén ở đó luôn.
        for (int i = 0; i < NEN_TANG.Length; i++)
        {
            TextureImporterPlatformSettings ps = imp.GetPlatformTextureSettings(NEN_TANG[i]);
            if (ps == null) continue;
            ps.textureCompression  = TextureImporterCompression.Uncompressed;
            ps.format              = TextureImporterFormat.Automatic;
            ps.crunchedCompression = false;
            imp.SetPlatformTextureSettings(ps);
        }

        // FullRect: mesh chữ nhật đầy, khỏi bị cắt góc.
        var cd = new TextureImporterSettings();
        imp.ReadTextureSettings(cd);
        if (cd.spriteMeshType != SpriteMeshType.FullRect)
        {
            cd.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(cd);
        }

        imp.SaveAndReimport();
    }

    /// <summary>Bộ nhớ RGBA32 không nén của một icon vuông, kèm chuỗi mip — để in vào báo cáo.</summary>
    private static long ByteIcon(int canh, bool comMip)
    {
        long tong = 0L;
        int c = canh;
        while (c >= 1)
        {
            tong += (long)c * c * 4L;
            if (!comMip) break;
            if (c == 1) break;
            c = Mathf.Max(1, c / 2);
        }
        return tong;
    }

    private static void BaoDamThuMuc()
    {
        string tuyetDoi = Path.Combine(Directory.GetCurrentDirectory(), THU_MUC_RA);
        if (Directory.Exists(tuyetDoi)) return;
        Directory.CreateDirectory(tuyetDoi);
        AssetDatabase.Refresh();
    }

    // ═════════════════════════════════════════════════════════════════════════════
    //  7. TIỆN ÍCH
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Nạp Sprite từ một texture.
    ///
    /// ⚠ Mọi ảnh nguồn ở đây đều Sprite Mode = Multiple ⇒
    /// <c>LoadAssetAtPath&lt;Sprite&gt;</c> TRẢ NULL. Phải quét sub-asset.
    /// (Chép đúng cách làm của <c>UnlockIconFillTool.LoadSprite</c>.)
    /// </summary>
    private static Sprite NapSprite(string duongDan)
    {
        var truc = AssetDatabase.LoadAssetAtPath<Sprite>(duongDan);
        if (truc != null) return truc;

        return AssetDatabase.LoadAllAssetRepresentationsAtPath(duongDan)
                            .OfType<Sprite>()
                            .FirstOrDefault();
    }

    private static List<PenMiniPanelConfig> NapChuong()
    {
        var ds = new List<PenMiniPanelConfig>();
        string[] guid = AssetDatabase.FindAssets("t:PenMiniPanelConfig");
        for (int i = 0; i < guid.Length; i++)
        {
            var c = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(
                        AssetDatabase.GUIDToAssetPath(guid[i]));
            if (c != null) ds.Add(c);
        }
        ds.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return ds;
    }

    /// <summary>So id vật phẩm: bỏ khoảng trắng, không phân biệt hoa thường.</summary>
    private static bool BangNhau(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private static string Chuoi(string s) { return string.IsNullOrEmpty(s) ? "(trống)" : s; }

    /// <summary>
    /// Điểm danh ảnh nguồn BẮT BUỘC — thiếu một cái là icon tương ứng không sinh được.
    /// <see cref="ANH_CAM_HEO"/> KHÔNG có trong danh sách này: tool không dùng nó nên
    /// thiếu cũng không sao (nó chỉ được nhắc trong mục "CẦN BẠN").
    /// </summary>
    private static void KiemTraAnhNguon(StringBuilder bc)
    {
        string[] can =
        {
            ANH_BAO_THOC, ANH_CON_GA, ANH_CON_HEO, ANH_CON_BO, ANH_SUA,
        };

        bc.AppendLine();
        bc.AppendLine("── ẢNH NGUỒN (bắt buộc) ──");
        for (int i = 0; i < can.Length; i++)
        {
            string tuyetDoi = Path.Combine(Directory.GetCurrentDirectory(), can[i]);
            bool co = File.Exists(tuyetDoi);
            bc.AppendLine("  " + (co ? "có   " : "THIẾU") + "  " + can[i]);
        }
    }

    /// <summary>Những thứ tool KHÔNG tự làm được, cần người thật.</summary>
    private static void InCanBan(StringBuilder bc)
    {
        bc.AppendLine();
        bc.AppendLine("═══ CẦN BẠN ═══");
        bc.AppendLine("  1. VẼ BAO CÁM THẬT cho CẢ 4 sản phẩm — cả 4 hiện chỉ là MỘT cái bao thóc");
        bc.AppendLine("     nhuộm 4 màu khác nhau, không phải 4 cái bao được vẽ riêng:");
        bc.AppendLine("       cam_ga      (vàng ngô)    ← nhuộm từ baothoc.png");
        bc.AppendLine("       cam_heo     (hồng phấn)   ← nhuộm từ baothoc.png");
        bc.AppendLine("       co_tron_bo  (xanh cỏ)     ← nhuộm từ baothoc.png");
        bc.AppendLine("       cam_bo_sua  (trắng sữa)   ← nhuộm từ baothoc.png");
        bc.AppendLine("     Vẽ xong: gán tay vào MillRecipe_*.icon + Item_*.icon +");
        bc.AppendLine("     Config_Pen0*.food1Icon/premiumFoodIcon, rồi xoá tool này.");
        bc.AppendLine();
        bc.AppendLine("  2. CÓ MỘT ART VẼ TAY CHO CÁM HEO mà tool KHÔNG dùng, bạn tự quyết:");
        bc.AppendLine("       " + ANH_CAM_HEO);
        bc.AppendLine("     File này tồn tại, 468×533, và hiện KHÔNG asset nào trong dự án trỏ tới");
        bc.AppendLine("     (mồ côi hoàn toàn). Tool cố tình không gán nó vì nó là một VẬT KHÁC —");
        bc.AppendLine("     túi giấy đáy phẳng có ngôi sao vàng, không phải bao bố tròn như 3 cái");
        bc.AppendLine("     kia, và ngôi sao vàng trong game này mang nghĩa EXP/phần thưởng nên dán");
        bc.AppendLine("     lên bao thức ăn gia súc là sai nghĩa. Nếu bạn vẫn thích art vẽ tay hơn");
        bc.AppendLine("     bản nhuộm thì gán tay file này vào MillRecipe_CamHeo.icon +");
        bc.AppendLine("     Item_CamHeo.icon + Config_Pen02_Heo.food1Icon/premiumFoodIcon.");
        bc.AppendLine();
        bc.AppendLine("  3. BADGE LÀ ẢNH CẮT LẤY ĐẦU con vật, không phải cả con.");
        bc.AppendLine("     Lý do: cỡ vẽ thật trên máy rất nhỏ —");
        bc.AppendLine("        đĩa slot 64px    → badge ~22px");
        bc.AppendLine("        card     98px    → badge ~33px");
        bc.AppendLine("        thành phẩm 192px → badge ~65px");
        bc.AppendLine("     Ở 22px, nhồi CẢ CON gà vào thì mào/mỏ/mắt đều nhỏ hơn 1 pixel và tan");
        bc.AppendLine("     thành một vệt nâu. Cắt lấy đầu trước khi thu nhỏ thì vẫn 22px đó mà");
        bc.AppendLine("     mào đỏ, mỏ vàng, mõm, sừng còn đọc được.");
        bc.AppendLine();
        bc.AppendLine("     ⇒ NẾU BẠN ĐỔI ART CON VẬT thì phải NGẮM LẠI KHUNG CẮT, nếu không tool");
        bc.AppendLine("       sẽ cắt vào giữa thân con vật mới. Khung nằm ở 4 hằng đầu file:");
        bc.AppendLine("          KHUNG_DAU_GA   = " + KHUNG_DAU_GA);
        bc.AppendLine("          KHUNG_DAU_HEO  = " + KHUNG_DAU_HEO);
        bc.AppendLine("          KHUNG_DAU_BO   = " + KHUNG_DAU_BO);
        bc.AppendLine("          KHUNG_CA_ANH   = " + KHUNG_CA_ANH + "  (bình sữa: giữ cả ảnh)");
        bc.AppendLine("       Toạ độ là PHÂN SỐ của hộp bao alpha, thứ tự (x0, yTrên0, x1, yTrên1),");
        bc.AppendLine("       y tính TỪ TRÊN xuống. Chạy lệnh 7 để xem cỡ khung cắt ra bao nhiêu px.");
        bc.AppendLine();
        bc.AppendLine("  4. baothoc.png nằm trong Assets/thietke — thư mục này có lệnh xoá riêng");
        bc.AppendLine("     (Tools/Farm/Popup Nhiệm Vụ/4). PNG do tool sinh KHÔNG phụ thuộc nó lúc");
        bc.AppendLine("     chạy game (đã bake ra pixel), nhưng lệnh 9 (hoàn tác) và việc SINH LẠI");
        bc.AppendLine("     thì cần. Đừng xoá Assets/thietke trước khi có bao cám thật.");
        bc.AppendLine();
        bc.AppendLine("  5. Kiểm mắt trong game: mở popup máy xay xem 4 card có phân biệt được");
        bc.AppendLine("     ở kích thước THẬT trên máy điện thoại không, không chỉ trong Inspector.");
    }

    /// <summary>In báo cáo ra Console (một khối) và ghi ra file cạnh PNG.</summary>
    private static void XuatBaoCao(StringBuilder bc)
    {
        Debug.Log(bc.ToString());

        BaoDamThuMuc();
        string tuyetDoi = Path.Combine(Directory.GetCurrentDirectory(), DUONG_DAN_BAO_CAO);
        try
        {
            File.WriteAllText(tuyetDoi, bc.ToString(), new UTF8Encoding(true));
            AssetDatabase.ImportAsset(DUONG_DAN_BAO_CAO, ImportAssetOptions.ForceSynchronousImport);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[IconCam] Không ghi được báo cáo " + DUONG_DAN_BAO_CAO + " — " + e.Message);
        }
    }
}
