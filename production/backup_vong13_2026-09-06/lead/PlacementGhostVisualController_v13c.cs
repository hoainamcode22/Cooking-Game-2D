using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime visual polish for Placement_Ghost.
/// Keeps all placement math in PlacementManager; this class only draws the map-footprint frame,
/// glow, and optional lift arrow effect.
/// </summary>
public class PlacementGhostVisualController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite tileSprite;

    [Header("Lop to kin vung o (V10 - Sep chot TAT)")]
    // 🔴 V10 — LỚP NHUỘM KÍN.
    // `Tile_Fill` là một hình thoi phóng đúng bằng hộp bao VÙNG Ô, tô màu mờ
    // (validFillColor xanh a=0.16 / invalidFillColor ĐỎ a=0.18). Nó nằm ở
    // sortingLayer "CongTrinh"/"Objects" order 1600 ⇒ VẼ TRÊN mọi công trình, nên khi
    // vùng ô phình to nó nhuộm kín màn hình và công trình bên dưới chỉ còn mờ mờ —
    // đúng thứ Sếp thấy trong ảnh. Sếp chốt TẮT để tự vẽ art lưới riêng.
    // KHÔNG Destroy: renderer vẫn còn nguyên, chỉ `enabled = false`. Tick lại là về như cũ.
    [Tooltip("Bat = to kin ca vung o bang lop mau mo (xanh khi hop le / DO khi khong). " +
             "Sep chot TAT vi lop nay nhuom kin man hinh. Tick lai la ve nhu cu, khong Destroy gi.")]
    [SerializeField] private bool showTileFill = false;

    [Tooltip("Bong mo duoi lop to kin. Di kem showTileFill.")]
    [SerializeField] private bool showSoftShadow = false;

    [Header("Colors")]
    [SerializeField] private Color validFillColor = new Color(0.10f, 0.95f, 0.30f, 0.16f);
    [SerializeField] private Color validEdgeColor = new Color(0.13f, 1f, 0.34f, 0.96f);
    [SerializeField] private Color validEdgeDarkColor = new Color(0.00f, 0.48f, 0.08f, 0.78f);
    [SerializeField] private Color validEdgeHighlightColor = new Color(0.70f, 1f, 0.42f, 0.95f);
    [SerializeField] private Color invalidFillColor = new Color(1f, 0.08f, 0.08f, 0.18f);
    [SerializeField] private Color invalidEdgeColor = new Color(1f, 0.18f, 0.18f, 0.96f);
    [SerializeField] private Color invalidEdgeDarkColor = new Color(0.62f, 0f, 0f, 0.92f);
    [SerializeField] private Color invalidEdgeHighlightColor = new Color(1f, 0.42f, 0.35f, 0.9f);
    [SerializeField] private Color shadowColor = new Color(0f, 0.28f, 0f, 0.22f);
    [SerializeField] private Color arrowColor = new Color(1f, 0.84f, 0.12f, 1f);
    [SerializeField] private Color arrowRimColor = new Color(0.70f, 0.36f, 0.02f, 1f);
    [SerializeField] private Color arrowHighlightColor = new Color(1f, 0.98f, 0.5f, 1f);
    [SerializeField] private Color arrowShadowColor = new Color(0.45f, 0.23f, 0.02f, 0.42f);

    [Header("Custom Sprites — gắn art CỦA BẠN để ra i hệt mẫu (assets tự gắn)")]
    [Tooltip("Sprite GÓC VUÔNG XANH (corner bracket). Gắn để 4 góc dùng đúng art của bạn.")]
    [SerializeField] private Sprite cornerBracketSprite;
    [Tooltip("Chỉ hiện 4 góc vuông, ẩn các cạnh viền — gọn giống mẫu.")]
    [SerializeField] private bool cornerBracketsOnly = true;

    // ══════════════════════════════════════════════════════════════════════
    // ╔══════════════════════════════════════════════════════════════════╗
    // ║ V13 — THU NHỎ CARD LẦN 2 (Sếp: "tab V X và thùng rác to quá")    ║
    // ╚══════════════════════════════════════════════════════════════════╝
    // Đây là lần thu nhỏ THỨ HAI. Vòng 11 đã hạ 560x302 -> 392x244; Sếp test lại
    // vẫn thấy to, nên vòng 13 hạ tiếp -> 312x204. Ghi lại TOÀN BỘ phép tính ở
    // một chỗ để lần sau khỏi phải suy lại từ đầu:
    //
    //   HỆ SỐ QUY ĐỔI world -> pixel (đã bù cả zoom LẪN DPI, xem caoManThamChieu):
    //     px = S x 1080/1500 = 0.72 x S      — HẰNG SỐ, độc lập zoom và chiều cao màn.
    //
    //   CỤM NÚT (hàng ngang, con của Button_Row, tâm y = 0):
    //     nút ✗ / 🗑 : 106  -> 76.3 px       (cũ 128 -> 92.2)
    //     nút ✓      : 126  -> 90.7 px       (cũ 148 -> 106.6)  ⚠ GIỮ TRÊN SÀN 88 px
    //     khe        : 26   -> 18.7 px hở    (cũ 44 -> 31.7)
    //     bề rộng cụm = 106 + 26 + 126 = 258
    //
    //   TẠI SAO ✓ DỪNG Ở 126 CHỨ KHÔNG NHỎ HƠN: 88 px là sàn đầu ngón tay.
    //     S_min = 88 / 0.72 = 122.3  ⇒ 126 chừa 3.7 px biên. Hạ tiếp là ĐI DƯỚI SÀN,
    //     và nút ✓ vừa mới được vòng 12 sửa cho bấm được (guard trong EditableBuilding)
    //     — làm nó khó bấm lại là tự phá bản fix đó. Nút ✗ và 🗑 hạ sâu hơn được vì
    //     bấm trượt chúng chỉ mất một lần thao tác, không mất tiền.
    //
    //   CHIỀU CAO (tính hết trong hệ Button_Row rồi trừ theTamY):
    //     nút ✓ 126   -> y thuộc [-63, +63]
    //     hàng giá 48 -> tâm 93, y thuộc [69, 117]      (hở 6 trên nút)
    //     card        -> y thuộc [-75, +129] = cao 204, tâm +27 (= theTamY)
    //     ⇒ hangGiaY (hệ CARD) = 93 - 27 = 66. Lề 12 px hai đầu card.
    //
    //   BỀ RỘNG: 258 (cụm nút) + 2 x 27 lề = 312. Vẫn tự nới theo chữ (LayoutPriceRow),
    //     và leNutTrongThe 44 bảo đảm card không bao giờ hẹp hơn cụm nút + lề.
    //
    //   HAI BẤT BIẾN PHẢI GIỮ (đã kiểm lại với số mới):
    //     (1) hangGiaY + hangGiaCao/2 <= theCao/2 - 8   ->  66 + 24 = 90 <= 94  ✓
    //     (2) hangGiaY + theTamY - hangGiaCao/2 > coNutXacNhan/2 -> 69 > 63     ✓
    //
    //   KẾT QUẢ TRÊN MÀN: card 225 x 147 px (cũ 282 x 176) — nhỏ hơn 20 % mỗi chiều,
    //   diện tích còn 69 %. kheHoDuoiVungO 30 -> 24 (21.6 -> 17.3 px) để card BÁM
    //   SÁT xuống dưới chân công trình đúng như Sếp yêu cầu, mà card thấp đi 29 world
    //   nên ngưỡng phải-trườn-lên cũng tụt theo ⇒ card ít bị đẩy che công trình hơn.
    //
    //   ⚠ Placement_Ghost.prefab KHÔNG serialize field nào của script này (đã kiểm:
    //   block 1010101010101010101 chỉ có m_Script) nên mọi giá trị ở đây là giá trị
    //   THẬT lúc chạy. Sửa ở đây là ăn ngay, không cần chỉnh prefab.
    // ══════════════════════════════════════════════════════════════════════

    [Header("V6 — THANH XÁC NHẬN KIỂU TOWNSHIP")]

    [Tooltip("Màu NHÂN vào nút HUỶ (✗).\n" +
             "🔴 V11 ĐỂ TRẮNG — nền nút giờ là btn_red_small, ĐÃ BAKE đỏ (221,76,69).\n" +
             "Giá trị cũ (0.93,0.26,0.24) nhân vào đỏ ⇒ (206,20,17) đỏ tối đục.\n" +
             "Chỉ đổi field này khi art nút là ảnh TRẮNG/XÁM.")]
    [SerializeField] private Color cancelButtonColor = Color.white;

    [Tooltip("Màu NHÂN vào nút XOAY (↻). V11 ĐỂ TRẮNG — nền là btn_yellow_3d, đã bake\n" +
             "vàng cam (245,169,59). Nút này đang bị BindGhostButtons SetActive(false) vì\n" +
             "Sếp đã bỏ tính năng xoay, giữ field để bật lại không phải sửa code.")]
    [SerializeField] private Color rotateButtonColor = Color.white;

    [Tooltip("Màu NHÂN vào nút XÁC NHẬN (✓).\n" +
             "🔴 V11 ĐỂ TRẮNG — ĐÂY LÀ MỘT NỬA LÝ DO NÚT ✓ TRONG ẢNH TRÔNG NHƯ BỊ DISABLE.\n" +
             "Nền mới là check_badge_green, ĐÃ BAKE xanh (110,195,45). Giá trị cũ\n" +
             "(0.27,0.78,0.30) nhân vào đó ⇒ (30,152,14) xanh sẫm đục; đứng cạnh nút ✗ đỏ\n" +
             "TƯƠI thì mắt đọc ra NÚT NÀY ĐANG TẮT. Ở vòng 10 nó còn nhân thêm độ sáng\n" +
             "0.88 bake trong sprite thủ tục ⇒ tối thêm một tầng nữa.\n" +
             "Nút VẪN tự đổi màu khi không đặt được — PlacementManager gán\n" +
             "btnConfirm.interactable, ColorTint nhân mauNutKhiTat vào đây. Đừng sửa tay.")]
    [SerializeField] private Color confirmButtonColor = Color.white;

    [Tooltip("Màu NHÂN vào nút XOÁ (🗑). ĐÂY LÀ NÚT DUY NHẤT CÒN TINT.\n" +
             "Nó DÙNG CHUNG nền btn_red_small với nút ✗ HUỶ, nên bắt buộc phải nhân cho\n" +
             "đỏ tươi (221,76,69) thành ĐỎ GẠCH SẪM (155,44,40): hai nút nằm cạnh nhau mà\n" +
             "hậu quả khác hẳn — Huỷ = trả công trình về chỗ cũ · Xoá = MẤT HẲN.\n" +
             "V11: 0.70/0.58/0.58 (cũ 0.62/0.13/0.16 ⇒ (137,10,11) gần như đen, mất hình nút).\n" +
             "Nút này CHỈ TỒN TẠI ở Editor / DEVELOPMENT_BUILD.")]
    [SerializeField] private Color deleteButtonColor = new Color(0.70f, 0.58f, 0.58f, 1f);

    [Tooltip("Chữ khi ĐANG DI CHUYỂN vật đã có trên map — không mất tiền.\n" +
             "Township: 'KOSTENLOS PLATZIEREN'. Người chơi biết ngay lần này không bị trừ.")]
    [SerializeField] private string freeMoveLabel = "ĐẶT MIỄN PHÍ";

    [Tooltip("Chữ khi ĐẶT MỚI (mất tiền). Icon xu/kim cương + số hiện ngay sau chữ này.\n" +
             "Township: 'KAUFEN FÜR 🪙 30'.")]
    [SerializeField] private string buyLabel = "MUA VỚI GIÁ";

    // ══════════════════════════════════════════════════════════════════════
    [Header("V10 — CARD BO GÓC + NEO VÀO WORLD")]

    [Tooltip("V11 THON GỌN — bề rộng TỐI THIỂU của card, px UI = world unit.\n" +
             "392 = nút ✗ 128 + khe 44 + nút ✓ 148 + lề 36 mỗi bên (bản phát hành chỉ hiện 2 nút:\n" +
             "Btn_Rotate bị BindGhostButtons SetActive(false), Btn_Delete chỉ có ở Editor build).\n" +
             "Card VẪN TỰ NỚI theo chữ VÀ theo bề rộng thật của hàng nút — xem LayoutPriceRow.\n" +
             "Cũ 560 ⇒ 403 px trên màn 1080 = 21 % bề ngang màn, đúng chỗ Sếp nói to bè.")]
    [SerializeField] private float theRongToiThieu = 312f;

    [Tooltip("V11 THON GỌN — chiều cao card.\n" +
             "244 = nút ✓ cao 148 (y ∈ [−74,+74]) + hàng giá cao 60 ở y 112 (y ∈ [82,142]) + lề 14 hai đầu\n" +
             "⇒ card phủ y ∈ [−88,+156], nhịp 244, tâm lệch +34 (= theTamY).\n" +
             "Cũ 302 ⇒ 217 px trên màn 1080; mới 244 ⇒ 176 px, thấp hơn 19 %.")]
    [SerializeField] private float theCao = 204f;

    [Tooltip("Tâm card so với Button_Row. Nút ở tâm y = 0, hàng giá ở TRÊN nên card phải dịch lên.\n" +
             "V11: 34 = ((−88) + 156) / 2 — suy trực tiếp từ theCao, đừng đặt tay.")]
    [SerializeField] private float theTamY = 27f;

    [Tooltip("Lề ngang cộng thêm khi card tự nới theo độ dài chữ. V11: 56 vì cỡ chữ đã hạ 64→46.")]
    [SerializeField] private float theLeNgang = 40f;

    [Tooltip("Khung ngoài lồi ra bao nhiêu px mỗi phía so với nền trong. Đây là 'khung' Sếp yêu cầu.\n" +
             "V11: 9 (cũ 14). shop_card_outer có vành nâu bake sẵn ~30/160 ⇒ khung đã dày trong art,\n" +
             "cộng thêm 14 px nữa là viền dày gấp đôi và card phình.")]
    [SerializeField] private float dayVienThe = 7f;

    [Tooltip("Màu nhân vào KHUNG NGOÀI card.\n" +
             "🔴 V11 ĐỂ TRẮNG — shop_card_outer ĐÃ BAKE nâu (184,127,67). Giá trị cũ\n" +
             "(0.42,0.27,0.14) = (107,69,36) nhân vào nâu ⇒ (77,34,9) MÀU BÙN. Đó chính là\n" +
             "cái Sếp gọi là xấu. Chỉ đổi field này khi art khung là ảnh TRẮNG/XÁM.")]
    [SerializeField] private Color mauVienThe = Color.white;

    [Tooltip("Màu nhân vào NỀN GIẤY trong card.\n" +
             "🔴 V11 ĐỂ TRẮNG ĐỤC HẲN — shop_card_inner ĐÃ BAKE kem (254,245,226). Alpha cũ 0.97\n" +
             "cho dải xanh nhạt của Button_Row hắt qua; giờ Button_Row đã bị ép trong suốt nên\n" +
             "không cần hở, và đục hẳn thì chữ sẫm đọc rõ hơn.")]
    [SerializeField] private Color mauNenThe = Color.white;

    [Tooltip("Màu chữ giá. NỀN KEM nên chữ phải SẪM. Muốn card nền TỐI thì đổi mauNenThe về " +
             "(0.10,0.10,0.12,0.92), field này về trắng, và vienChuTrang về 0.26.")]
    [SerializeField] private Color mauChuGia = new Color(0.29f, 0.17f, 0.05f, 1f);

    [Tooltip("Độ dày viền chữ. Chữ SẪM trên nền KEM cần viền TRẮNG mảnh (0.18), khác bản cũ " +
             "là chữ trắng viền nâu dày 0.26.")]
    [SerializeField] private float vienChuTrang = 0.18f;

    [Tooltip("Cỡ chữ 'MUA VỚI GIÁ'. V11: 46 (cũ 64) ⇒ 33 px trên màn ở MỌI mức zoom VÀ mọi\n" +
             "chiều cao màn (xem heSoManHinhToiDa). 33 px chữ IN HOA đậm là ngưỡng đọc thoải mái\n" +
             "cho trẻ; 64 chỉ để bù cho việc bản cũ không có bù DPI.")]
    [SerializeField] private float coChuNhan = 38f;

    [Tooltip("Cỡ chữ SỐ GIÁ. To hơn nhãn vì đây là thứ mắt phải đọc trước tiên. V11: 56 ⇒ 40 px.")]
    [SerializeField] private float coChuSo = 46f;

    [Tooltip("Cỡ icon xu / kim cương. Bằng chiều cao số để đọc thành MỘT cụm. V11: 52 ⇒ 37 px.")]
    [SerializeField] private float coIconTien = 42f;

    [Tooltip("Y của hàng giá — 🔴 SO VỚI TÂM CARD, KHÔNG PHẢI so với Button_Row.\n" +
             "ĐÂY LÀ MỘT LỖI THẬT CỦA VÒNG 10, đã đo: hàng giá là con của Confirm_Bar_Panel\n" +
             "(anchorMin = anchorMax = 0.5) nên anchoredPosition tính từ TÂM CARD; còn 3 nút\n" +
             "là con của Button_Row. Card lại lệch lên theTamY so với Button_Row, nên số này\n" +
             "bị cộng thêm theTamY một lần nữa.\n" +
             "Số cũ 132 với theCao 302 / theTamY 49: nửa cao card = 151, hàng giá phủ\n" +
             "y ∈ [90, 174] trong hệ card ⇒ TRÀN RA NGOÀI ĐỈNH CARD 23 px. Chữ 'ĐẶT MIỄN PHÍ'\n" +
             "vì thế dính sát mép trên tờ giấy, đúng cái nhìn Sếp gửi.\n" +
             "\n" +
             "V11 QUY VỀ MỘT HỆ, tất cả tính trong hệ Button_Row rồi trừ theTamY:\n" +
             "  nút ✓ 148  → y ∈ [−74, +74]\n" +
             "  hàng giá 60 → tâm 112, y ∈ [82, 142]      (hở 8 px trên nút)\n" +
             "  card       → y ∈ [−88, +156] = cao 244, tâm +34 (= theTamY)\n" +
             "  ⇒ hangGiaY (hệ CARD) = 112 − 34 = 78. Lề 14 px ở CẢ hai đầu card.\n" +
             "BẤT BIẾN PHẢI GIỮ: hangGiaY + hangGiaCao/2 ≤ theCao/2 − 8\n" +
             "              VÀ  hangGiaY + theTamY − hangGiaCao/2 > coNutXacNhan/2")]
    [SerializeField] private float hangGiaY = 66f;

    [Tooltip("Chiều cao ô chữ hàng giá. V11: 60 (cũ 84) — vừa đủ bọc số cỡ 56.")]
    [SerializeField] private float hangGiaCao = 48f;

    [Tooltip("Khe giữa nhãn / icon / số trong hàng giá. V11: 14 theo cỡ chữ mới.")]
    [SerializeField] private float kheHangGia = 10f;

    [Tooltip("Cạnh nút ✗ HUỶ (và ↻ XOAY, 🗑 XOÁ). V11: 128.\n" +
             "SÀN CỨNG 123 — dưới mức đó là dưới 88 px đầu ngón tay trẻ. Suy ra:\n" +
             "px = S × buZoom × H / (2·ortho); với bù DPI thì buZoom·H/(2·ortho) = 1080/1500 = 0.72\n" +
             "⇒ S ≥ 88 / 0.72 = 122.3. Chọn 128 ⇒ 92.2 px, chừa 4 px biên.")]
    [SerializeField] private float coNutHuy = 106f;

    [Tooltip("Đường kính nút ✓ XÁC NHẬN. V11: 148 ⇒ 106.6 px trên màn.\n" +
             "TO HƠN nút huỷ 15.6 % (148/128) — đây là hành động chính, và cỡ khác nhau là\n" +
             "KÊNH PHÂN BIỆT THỨ BA sau hình (✓ đĩa TRÒN · ✗ VUÔNG bo góc) và màu.")]
    [SerializeField] private float coNutXacNhan = 126f;

    [Tooltip("Cỡ glyph ✗ trong nút. V11: 66 = 0.52 × 128, giữ đúng tỉ lệ cũ (84/152 = 0.55).")]
    [SerializeField] private float coGlyphHuy = 56f;

    [Tooltip("Cỡ glyph ✓ trong nút. V11: 82 = 0.55 × 148 — TO HƠN glyph ✗ (66) để kênh CỠ\n" +
             "còn đọc được cả khi người chơi chỉ nhìn dấu, không nhìn nền nút.")]
    [SerializeField] private float coGlyphXacNhan = 70f;

    [Tooltip("Khe giữa 2 nút. V11: 44 ⇒ 31.7 px hở, tâm cách tâm (128+148)/2 + 44 = 182 ⇒ 131 px.\n" +
             "Vẫn xa gấp 4 lần mức 8 px của hướng dẫn cảm ứng, mà card hẹp đi 28 px.\n" +
             "GHI ĐÈ m_Spacing = 20 của prefab MỘT LẦN lúc dựng, không ghi mỗi frame.")]
    [SerializeField] private float kheGiuaHaiNut = 26f;

    [Tooltip("Khe hở giữa MÉP DƯỚI VÙNG Ô và ĐỈNH CARD, world unit ở zoom mốc (1 ô = 300 x 150).\n" +
             "NHÂN THEO buZoom lúc chạy nên luôn cùng số px trên màn.\n" +
             "V11: 30 ⇒ 21.6 px. Càng nhỏ card càng BÁM SÁT công trình, và quan trọng hơn:\n" +
             "khe nhỏ + card thấp ⇒ ngưỡng phải-tránh-mép-màn tụt từ 313 px xuống 280 px,\n" +
             "tức card ít bị đẩy đi hơn nhiều.")]
    [SerializeField] private float kheHoDuoiVungO = 24f;

    [Tooltip("Ortho size mốc để bù zoom. Đúng CameraController.defaultSize = 750.")]
    [SerializeField] private float orthoThamChieu = 750f;

    [Tooltip("Sàn hệ số bù zoom. 0.75 ⇒ zoom sát (ortho 400) card không phình che công trình.")]
    [SerializeField] private float buZoomMin = 0.75f;

    [Tooltip("Trần hệ số bù zoom. V11: 3.0 (cũ 2.0).\n" +
             "VÌ SAO PHẢI NỚI: buZoom giờ nhân thêm bù DPI (caoManThamChieu / Screen.height).\n" +
             "Màn cao 1080 trở lên ⇒ bù DPI = 1 ⇒ buZoom tối đa vẫn đúng 2.0 như cũ,\n" +
             "KHÔNG ĐỔI GÌ. Chỉ màn 720 mới cần tới 1.5 × 2.0 = 3.0; nếu vẫn kẹp 2.0 thì\n" +
             "máy 720p zoom hết ra nút chỉ còn 0.48 × cỡ = 61 px, dưới mức 88 px.")]
    [SerializeField] private float buZoomMax = 3f;

    [Tooltip("Lề an toàn hai bên màn, PIXEL.")]
    [SerializeField] private float leAnToanNganPx = 40f;

    [Tooltip("Lề an toàn ĐÁY màn, PIXEL. Canvas_HUD là Screen Space nên nó VẼ ĐÈ lên card bất kể " +
             "sortingOrder — đây là thứ duy nhất giữ nút ✓ khỏi chui xuống dưới nút NẤU ĂN.\n" +
             "170 = 120 đo thật (4 nút ~100 px, đáy cách đáy màn 20) + 50 biên: bóng đổ 8 px, " +
             "nút nảy 1.1 lần ≈ 12 px, và 30 px dự phòng cho màn hẹp (CanvasScaler match 0.5).")]
    [SerializeField] private float leAnToanDuoiPx = 170f;

    [Tooltip("Lề an toàn ĐỈNH màn, PIXEL. 210 = 195 đo thật (avatar cách đỉnh 25.4 + khung cao 168) " +
             "+ 15 dư. Lề này gần như không dùng tới vì card neo DƯỚI vùng ô.")]
    [SerializeField] private float leAnToanTrenPx = 210f;

    [Tooltip("BẬT = card LẬT SANG CẠNH khi công trình sát đáy màn.\n" +
             "🔴 V11 CHỐT TẮT — ĐÂY LÀ THỦ PHẠM CHÍNH của 'không bám sát công trình'.\n" +
             "Đo trên ô 1x1 (vùng ô 300 x 150): dx = nửa rộng vùng ô 150 + khe 48 + nửa rộng\n" +
             "card 280 = 478 world = 344 px ⇒ card NHẢY 344 px sang bên, rời hẳn công trình.\n" +
             "Mà nhánh này nổ RẤT DỄ: nó nổ khi đáy card < 170 px, tức khi mép dưới vùng ô\n" +
             "chưa cao quá 313 px so với đáy màn — nửa dưới màn hình là vùng đất chính.\n" +
             "TẮT ⇒ card LUÔN ĐỨNG TRÊN TRỤC X CỦA CÔNG TRÌNH, chỉ TRƯỜN LÊN theo trục Y\n" +
             "(tối đa tới TÂM vùng ô) khi thiếu chỗ. Trườn lên chỉ phủ phần chân công trình,\n" +
             "còn nhảy sang cạnh là đứt liên hệ thị giác.")]
    [SerializeField] private bool luonLatSangCanh = false;

    // ══════════════════════════════════════════════════════════════════════
    [Header("V11 — BÙ DPI + TRẠNG THÁI TẮT CỦA NÚT ✓")]

    [Tooltip("Chiều cao màn MỐC để bù DPI, PIXEL.\n" +
             "VÌ SAO CẦN: card nằm trên canvas WORLD SPACE nên cỡ px của nó =\n" +
             "  px = S × buZoom × Screen.height / (2 × ortho)\n" +
             "Bản cũ buZoom chỉ tính theo ortho ⇒ px TỈ LỆ THUẬN với Screen.height. Con số\n" +
             "'nút 152 = 109 px' của vòng 10 CHỈ ĐÚNG trên màn cao 1080; máy 720p thì nó là\n" +
             "73 px, đã dưới mức 88 px mà không ai biết.\n" +
             "Nhân buZoom thêm (caoManThamChieu / Screen.height) là px thành HẰNG SỐ:\n" +
             "  px = S × (ortho/750) × (1080/H) × H / (2·ortho) = S × 1080/1500 = 0.72 × S\n" +
             "— độc lập cả zoom LẪN chiều cao màn.")]
    [SerializeField] private float caoManThamChieu = 1080f;

    [Tooltip("TRẦN của riêng hệ số bù DPI. 1.6 ⇒ đỡ được tới màn cao 675 px.\n" +
             "SÀN LUÔN LÀ 1.0 (kẹp trong code, không mở ra field): màn CAO HƠN 1080 thì\n" +
             "hệ số < 1 sẽ THU NHỎ card — đúng về px nhưng làm card bé tí so với công trình,\n" +
             "và trên màn nhiều pixel thì 0.72 × S đã dư sức đọc. Kẹp sàn 1.0 ⇒ máy 1080\n" +
             "trở lên KHÔNG ĐỔI MỘT PIXEL so với vòng 10.")]
    [SerializeField] private float heSoManHinhToiDa = 1.6f;

    [Tooltip("Lề trong card cộng thêm quanh CỤM NÚT khi tự nới. Card không được hẹp hơn\n" +
             "cụm nút, nếu không nút thò ra ngoài giấy.")]
    [SerializeField] private float leNutTrongThe = 44f;

    [Tooltip("Màu nhân vào NỀN GIẤY card khi vị trí KHÔNG ĐẶT ĐƯỢC. Hồng đất nhạt.\n" +
             "ĐÂY LÀ KÊNH THỨ BA của trạng thái tắt: nút ✓ nói 'chưa bấm được', còn TỜ GIẤY\n" +
             "nói 'vì CHỖ NÀY không được'. Không có kênh này thì người chơi chỉ thấy một nút\n" +
             "mờ và kết luận là UI hỏng — đúng thứ Sếp đọc ra từ ảnh.")]
    [SerializeField] private Color mauNenTheKhongHopLe = new Color(1f, 0.80f, 0.76f, 1f);

    [Tooltip("Màu TRẠNG THÁI TẮT của nút (Button.colors.disabledColor), ghi lúc dựng.\n" +
             "🔴 ALPHA PHẢI = 1. Prefab Placement_Ghost đang serialize (0.784,0.784,0.784, α 0.502)\n" +
             "⇒ đĩa xanh (110,195,45) × cái đó = (68,121,28) MÀ CHỈ ĐỤC 50 % ⇒ nhìn xuyên thấy\n" +
             "mặt đất qua nút. Mắt đọc ra 'nút bị lỗi/chưa load', không đọc ra 'chưa bấm được'.\n" +
             "0.58 đục hẳn ⇒ (64,113,26) xanh ô liu sẫm: vẫn là NÚT, chỉ đang ngủ.\n" +
             "Ghi qua code chứ không sửa prefab: đây là runtime assignment, không phải\n" +
             "trông chờ giá trị mặc định (giá trị serialize sẵn thì code không đè được).")]
    [SerializeField] private Color mauNutKhiTat = new Color(0.58f, 0.60f, 0.58f, 1f);

    [Tooltip("Alpha của GLYPH khi nút bị tắt. V11: 0.90 (cũ 0.45).\n" +
             "0.45 làm dấu ✓ gần như tan biến ⇒ người chơi không còn biết nút đó là nút gì.\n" +
             "0.90 giữ dấu ✓ rõ nguyên hình, việc 'đang tắt' để cho nền nút nói.")]
    [SerializeField] private float doMoGlyphKhiTat = 0.90f;

    // ══════════════════════════════════════════════════════════════════════
    [Header("V7 — 4 CHEVRON ÔM 4 GÓC VÙNG Ô")]

    [Tooltip("BẬT = dùng 4 chevron đặt theo PlacementManager.CurrentRect (đúng Township) và " +
             "TẮT 4 nêm cũ vốn suy từ bounds SPRITE.\n" +
             "VÌ SAO KHÁC NHAU: mái nhà nhô ra ngoài footprint, nên góc sprite ≠ góc vùng ô. " +
             "Người chơi cần thấy đúng vùng SẼ BỊ CHIẾM.")]
    [SerializeField] private bool useRectChevrons = true;

    [Tooltip("Cạnh của mỗi chevron, WORLD unit (1 ô lưới = 100). 46 ≈ nửa ô.")]
    [SerializeField] private float chevronWorldSize = 46f;

    [Tooltip("Chu kỳ nhấp nháy, giây. Township ≈ 1s.")]
    [SerializeField] private float chevronBlinkPeriod = 1f;

    [Tooltip("Đỉnh scale khi nhấp nháy. Township: 1.0 ↔ 1.08. Rất nhẹ là chủ ý.")]
    [SerializeField] private float chevronBlinkPeak = 1.08f;

    private Transform _frameRoot;
    private Transform _arrowRoot;
    private SpriteRenderer _fill;
    private SpriteRenderer _softShadow;      // V10: giữ tham chiếu để bật/tắt theo showSoftShadow
    private SpriteRenderer[] _edges;
    private SpriteRenderer[] _edgeShadows;
    private SpriteRenderer[] _edgeHighlights;
    private SpriteRenderer[] _corners;
    private SpriteRenderer[] _arrowDots;
    private Sprite _diamondSprite;
    private Sprite _markerSprite;
    private Sprite _arrowSprite;
    private Sprite _circleSprite;
    private Sprite _bracketSprite;
    private Coroutine _arrowPulse;
    private Coroutine _framePulse;
    private Coroutine _spawnPop;
    private Coroutine _invalidPulse;
    private Vector3 _arrowBaseLocalPosition;
    private bool _lastValid = true;
    private bool _suppressFramePulse;
    private bool _isBuildingVisuals;
    private bool _built;                    // EnsureBuilt đã chạy xong ít nhất một lần

    // ── V6: thanh xác nhận ───────────────────────────────────────────────────
    private RectTransform   _barPanel;
    private Image           _barRowBg;      // nền xanh nhạt PlacementManager gắn lên Button_Row
    private TextMeshProUGUI _barLabel;
    private Image           _barCoin;
    private TextMeshProUGUI _barNumber;
    private bool            _barBuilt;
    private Button          _deleteButton;   // chỉ hiện khi đang SỬA vật có sẵn
    private string          _barLastLabel;
    private string          _barLastNumber;
    private bool            _barLastGem;
    private bool            _barLastMoney;
    private Button[]        _barButtons;    // 0 = ✕, 1 = ↻, 2 = ✓
    private Image[]         _barGlyphs;
    private bool[]          _barGlyphDim;

    // ── V11 ──────────────────────────────────────────────────────────────────
    private Image           _barNenGiay;    // tờ giấy trong card — nhuộm hồng khi không đặt được
    private Image           _barRuyBang;    // ruy băng vàng THẬT sau hàng giá
    private bool            _theDangBaoLoi; // cổng chặn: chỉ ghi màu giấy khi ĐỔI trạng thái
    private float           _rongCumNutCuoi = -1f;  // ép xếp lại khi số nút hiện thay đổi

    // ── V10: neo card vào world ──────────────────────────────────────────────
    private RectTransform _uiRoot;                    // Placement_UI, cache một lần
    private Vector3       _tamVungOLocal;             // ConfigureFromLocalBounds ghi vào
    private float         _rongVungOLocal = 1.35f;
    private float         _caoVungOLocal  = 0.95f;
    private float         _theRongHienTai = 392f;     // LayoutPriceRow ghi vào (V11: theo theRongToiThieu mới)

    // ── V7: 4 chevron theo vùng ô ────────────────────────────────────────────
    private Transform        _chevronRoot;
    private SpriteRenderer[] _chevrons;
    private Sprite           _chevronSprite;
    private readonly Vector3[] _chevronCorners = new Vector3[4];

    private const string VisualRootName = "Designed_Placement_Frame";
    private const string ArrowRootName = "Lift_Arrow_Effect";
    private const string ChevronRootName = "Rect_Chevrons";
    private const string PreferredSortingLayerName = "CongTrinh";
    private const string FallbackSortingLayerName = "Objects";
    private static string _resolvedSortingLayerName;
    private static string SortingLayerName
    {
        get
        {
            if (string.IsNullOrEmpty(_resolvedSortingLayerName))
                _resolvedSortingLayerName = ResolveSortingLayerName(PreferredSortingLayerName, FallbackSortingLayerName);
            return _resolvedSortingLayerName;
        }
    }
    public const int BaseOrder = 1600;
    public const int BuildingOrder = BaseOrder + 80;

    public void SetTileSprite(Sprite sprite)
    {
        if (sprite != null)
            tileSprite = sprite;
    }

    public void EnsureBuilt()
    {
        if (_isBuildingVisuals)
            return;

        EnsureRuntimeSprites();

        _isBuildingVisuals = true;

        _frameRoot = transform.Find(VisualRootName);
        if (_frameRoot == null)
        {
            GameObject root = new GameObject(VisualRootName);
            root.layer = gameObject.layer;
            _frameRoot = root.transform;
            _frameRoot.SetParent(transform, false);
            _frameRoot.localPosition = Vector3.zero;
            _frameRoot.localRotation = Quaternion.identity;
            _frameRoot.localScale = Vector3.one;
        }

        _fill = CreateOrGetRenderer(_frameRoot, "Tile_Fill", _diamondSprite, BaseOrder + 1);
        _fill.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        SpriteRenderer shadow = CreateOrGetRenderer(_frameRoot, "Soft_Shadow", _diamondSprite, BaseOrder);
        _softShadow = shadow;
        shadow.color = shadowColor;
        shadow.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        shadow.transform.localPosition = new Vector3(0f, -0.07f, 0f);

        _edgeShadows = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Top", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Right", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Bottom", _markerSprite, BaseOrder + 2),
            CreateOrGetRenderer(_frameRoot, "Marker_Shadow_Left", _markerSprite, BaseOrder + 2)
        };

        _edges = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Top", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Right", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Bottom", _markerSprite, BaseOrder + 3),
            CreateOrGetRenderer(_frameRoot, "Marker_Left", _markerSprite, BaseOrder + 3)
        };

        _edgeHighlights = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Top", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Right", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Bottom", _markerSprite, BaseOrder + 4),
            CreateOrGetRenderer(_frameRoot, "Marker_Highlight_Left", _markerSprite, BaseOrder + 4)
        };

        _corners = new[]
        {
            CreateOrGetRenderer(_frameRoot, "Corner_Top", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Right", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Bottom", _diamondSprite, BaseOrder + 5),
            CreateOrGetRenderer(_frameRoot, "Corner_Left", _diamondSprite, BaseOrder + 5)
        };

        // Góc vuông: nếu có gắn sprite tuỳ chỉnh thì dùng, KHÔNG thì dùng L-bracket VẼ BẰNG CODE.
        Sprite cornerSpr = cornerBracketSprite != null ? cornerBracketSprite : _bracketSprite;
        if (cornerSpr != null)
            foreach (var c in _corners) if (c != null) c.sprite = cornerSpr;

        // V7: 4 nêm cũ suy từ bounds SPRITE → tắt khi đã dùng chevron theo VÙNG Ô.
        ToggleArray(_corners, !useRectChevrons);

        BuildArrow();
        EnsureChevrons();
        EnsureConfirmBar();
        ApplyVisualState(_lastValid);

        if (_arrowRoot != null)
            _arrowRoot.gameObject.SetActive(false);

        if (_framePulse == null)
            _framePulse = StartCoroutine(PulseFrame());

        _isBuildingVisuals = false;
        _built = true;
    }

    /// <summary>
    /// Cập nhật mỗi frame: nội dung thanh xác nhận + vị trí 4 chevron.
    ///
    /// VÌ SAO PHẢI Ở Update() CHỨ KHÔNG DỰNG MỘT LẦN:
    ///   • `IsFreeMove` / giá / `CurrentRect` / `IsCurrentValid` là trạng thái ĐỘNG của
    ///     PlacementManager — `CurrentRect` được ghi lại MỖI FRAME trong Update() của nó.
    ///   • Bản cũ đọc giá đúng MỘT LẦN trong EnsureBuilt qua `ConstructionBridge.GetGhostItem()`,
    ///     mà EnsureBuilt (SetupGhostVisualController) chạy TRƯỚC khi Ghost được cấu hình
    ///     xong → có lượt bắt được null và dải giá không hiện. Đọc mỗi frame là hết cửa lỗi.
    ///   • Ba hàm dưới đều có cổng chặn "không đổi thì không ghi", nên không dirty canvas.
    /// </summary>
    private void Update()
    {
        if (!_built) return;

        EnsureConfirmBar();     // thử lại tới khi Button_Row + Canvas sẵn sàng
        RefreshConfirmBar();
    }

    /// <summary>
    /// 🔴 V11 — NEO CARD VÀ 4 CHEVRON CHUYỂN TỪ Update() SANG LateUpdate().
    ///
    /// ĐÂY LÀ NGUYÊN NHÂN THỨ HAI của "không bám sát công trình", và là nguyên nhân
    /// KHÔNG NHÌN RA ĐƯỢC bằng cách đọc riêng file này.
    ///
    /// `PlacementManager.Update()` làm HAI việc mỗi frame khi người chơi ĐANG GIỮ chuột:
    ///     currentGhost.transform.position = GetSnappedMousePos(size);   // dòng ~778
    ///     currentRect = CurrentRectOf(size);                            // dòng ~785
    /// Cả vị trí ghost LẪN `CurrentRect` đều được ghi TRONG Update.
    ///
    /// ProjectSettings KHÔNG CÓ MonoManager.asset ⇒ project này CHƯA đặt Script Execution
    /// Order ⇒ thứ tự Update giữa PlacementManager và class này là KHÔNG XÁC ĐỊNH. Nếu
    /// class này chạy trước, card và 4 chevron đọc vị trí CỦA FRAME TRƯỚC. Mà ghost snap
    /// theo lưới iso nên mỗi bước nhảy trọn MỘT Ô = 300 x 150 world; trễ một frame là card
    /// lệch cả một ô suốt thời gian kéo — đúng cảm giác "card trôi theo sau công trình".
    ///
    /// LateUpdate LUÔN chạy sau MỌI Update trong cùng frame (bảo đảm của Unity, không phụ
    /// thuộc thứ tự script) ⇒ hết trễ, và KHÔNG cần sửa PlacementManager hay thêm
    /// MonoManager.asset (hai thứ đều ngoài phạm vi file này).
    /// </summary>
    private void LateUpdate()
    {
        if (!_built) return;

        NeoCardVaoWorld();      // V10 — đặt card dưới vùng ô + bù zoom + tránh mép màn
        UpdateChevrons();
    }

    public void ConfigureFromFootprintScale(Vector3 footprintScale)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        ConfigureFromLocalSize(Mathf.Abs(footprintScale.x), Mathf.Abs(footprintScale.y));
    }

    public void ConfigureFromWorldSize(float worldWidth, float worldHeight)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        ConfigureFromLocalBounds(Vector3.zero, worldWidth / scaleX, worldHeight / scaleY);
    }

    public void ConfigureFromWorldBounds(Bounds worldBounds, float paddingMultiplier = 1.12f)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        if (worldBounds.size.x <= 0.01f || worldBounds.size.y <= 0.01f)
        {
            ConfigureFromWorldSize(1.5f, 1.0f);
            return;
        }

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        localCenter.z = 0f;
        ConfigureFromLocalBounds(
            localCenter,
            worldBounds.size.x * paddingMultiplier / scaleX,
            worldBounds.size.y * paddingMultiplier / scaleY);
    }

    private void ConfigureFromLocalSize(float localWidth, float localHeight)
    {
        ConfigureFromLocalBounds(Vector3.zero, localWidth, localHeight);
    }

    private void ConfigureFromLocalBounds(Vector3 localCenter, float localWidth, float localHeight)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        float width = Mathf.Max(1.35f, localWidth);
        float height = Mathf.Max(0.95f, localHeight);

        // V10: hai số này đang bị tính rồi BỎ ĐI. NeoCardVaoWorld() cần chúng để đặt card
        // dưới MÉP DƯỚI VÙNG Ô thật, chứ không phải dưới gốc ghost như bản cũ.
        _tamVungOLocal  = localCenter;
        _rongVungOLocal = width;
        _caoVungOLocal  = height;
        float edgeThickness = Mathf.Clamp(Mathf.Min(width, height) * 0.11f, 0.12f, 0.28f);

        _frameRoot.localPosition = localCenter;

        Transform shadow = _frameRoot.Find("Soft_Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(width * 0.82f, height * 0.78f, 1f);

        if (_fill != null)
            _fill.transform.localScale = new Vector3(width * 0.78f, height * 0.74f, 1f);

        float sideMarkerW = Mathf.Clamp(width * 0.44f, 0.55f, 1.45f);
        float sideMarkerH = Mathf.Clamp(height * 0.44f, 0.40f, 1.05f);
        float markerThickness = edgeThickness * 1.15f;
        Vector3[] markerPositions =
        {
            new(0f, height * 0.50f, 0f),
            new(width * 0.50f, 0f, 0f),
            new(0f, -height * 0.50f, 0f),
            new(-width * 0.50f, 0f, 0f)
        };

        Vector3[] markerScales =
        {
            new(sideMarkerW, markerThickness, 1f),
            new(sideMarkerH, markerThickness, 1f),
            new(sideMarkerW, markerThickness, 1f),
            new(sideMarkerH, markerThickness, 1f)
        };

        float[] markerRotations = { 0f, -90f, 180f, 90f };

        for (int i = 0; i < 4; i++)
        {
            SetMarker(_edgeShadows[i], markerPositions[i] + new Vector3(0.04f, -0.05f, 0f), markerScales[i] * 1.08f, markerRotations[i]);
            SetMarker(_edges[i], markerPositions[i], markerScales[i], markerRotations[i]);
            SetMarker(_edgeHighlights[i], markerPositions[i] + new Vector3(0f, markerThickness * 0.16f, 0f), new Vector3(markerScales[i].x * 0.82f, markerScales[i].y * 0.28f, 1f), markerRotations[i]);
        }

        // 4 GÓC VUÔNG ở 4 góc khung chữ nhật, xoay ôm vào trong (kiểu corner-bracket như mẫu).
        float cornerX = width * 0.44f;
        float cornerY = height * 0.47f;
        float cornerW = Mathf.Clamp(width * 0.30f, 0.46f, 1.25f);
        float cornerH = Mathf.Clamp(height * 0.22f, 0.18f, 0.52f);
        SetCornerMarker(_corners[0], new Vector3(-cornerX,  cornerY, 0f), cornerW, cornerH, -35f);
        SetCornerMarker(_corners[1], new Vector3( cornerX,  cornerY, 0f), cornerW, cornerH, -145f);
        SetCornerMarker(_corners[2], new Vector3(-cornerX, -cornerY, 0f), cornerW, cornerH,  35f);
        SetCornerMarker(_corners[3], new Vector3( cornerX, -cornerY, 0f), cornerW, cornerH, 145f);

        SetEdgesVisible(!cornerBracketsOnly);

        // Giữ 4 nêm cũ TẮT sau mỗi lần cấu hình lại (hàm này được gọi lại mỗi lần xoay).
        ToggleArray(_corners, !useRectChevrons);

        if (_arrowRoot != null)
        {
            _arrowBaseLocalPosition = localCenter + new Vector3(0f, height * 0.62f + 0.60f, 0f);
            _arrowRoot.localPosition = _arrowBaseLocalPosition;
        }
    }

    public void SetValid(bool valid)
    {
        EnsureBuilt();
        bool changed = _lastValid != valid;
        _lastValid = valid;
        ApplyVisualState(valid);

        if (changed && !valid && _invalidPulse == null)
            _invalidPulse = StartCoroutine(InvalidNudge());
    }

    private void ApplyVisualState(bool valid)
    {
        Color fill = valid ? validFillColor : invalidFillColor;
        Color edge = valid ? validEdgeColor : invalidEdgeColor;

        // V10: áp cờ MỖI LẦN đổi trạng thái, không chỉ lúc EnsureBuilt.
        // Ghost được tái sử dụng qua nhiều lượt đặt và ConfigureFromLocalBounds có thể
        // chạy lại giữa lượt, nên đặt một lần lúc dựng là có ngày bị bật lại lặng lẽ.
        if (_fill != null)
        {
            _fill.enabled = showTileFill;
            _fill.color   = fill;
        }

        if (_softShadow != null)
            _softShadow.enabled = showSoftShadow;

        if (_edges != null)
            foreach (SpriteRenderer sr in _edges)
                if (sr != null) sr.color = edge;

        Color dark = valid ? validEdgeDarkColor : invalidEdgeDarkColor;
        if (_edgeShadows != null)
            foreach (SpriteRenderer sr in _edgeShadows)
                if (sr != null) sr.color = dark;

        Color highlight = valid ? validEdgeHighlightColor : invalidEdgeHighlightColor;
        if (_edgeHighlights != null)
            foreach (SpriteRenderer sr in _edgeHighlights)
                if (sr != null) sr.color = highlight;

        if (_corners != null)
            foreach (SpriteRenderer sr in _corners)
                if (sr != null) sr.color = edge;
    }

    public void PlaySpawnPop(bool stronger)
    {
        EnsureBuilt();
        if (_frameRoot == null)
            return;

        if (_spawnPop != null)
            StopCoroutine(_spawnPop);
        _spawnPop = StartCoroutine(SpawnPop(stronger));
    }

    public void ShowLiftArrow(bool show)
    {
        EnsureBuilt();
        if (_arrowRoot == null)
            return;

        _arrowRoot.gameObject.SetActive(show);
        if (show)
            _arrowRoot.localPosition = _arrowBaseLocalPosition;

        if (show && _arrowPulse == null)
            _arrowPulse = StartCoroutine(PulseArrow());
        else if (!show && _arrowPulse != null)
        {
            StopCoroutine(_arrowPulse);
            _arrowPulse = null;
            _arrowRoot.localScale = Vector3.one;
            _arrowRoot.localPosition = _arrowBaseLocalPosition;
        }
    }

    private void BuildArrow()
    {
        _arrowRoot = transform.Find(ArrowRootName);
        if (_arrowRoot == null)
        {
            GameObject root = new GameObject(ArrowRootName);
            root.layer = gameObject.layer;
            _arrowRoot = root.transform;
            _arrowRoot.SetParent(transform, false);
        }

        SpriteRenderer shadow = CreateOrGetRenderer(_arrowRoot, "Arrow_Shadow", _arrowSprite, BaseOrder + 7);
        shadow.color = arrowShadowColor;
        shadow.transform.localPosition = new Vector3(0.04f, -0.05f, 0f);
        shadow.transform.localRotation = Quaternion.identity;
        shadow.transform.localScale = new Vector3(1.26f, 1.10f, 1f);

        SpriteRenderer rim = CreateOrGetRenderer(_arrowRoot, "Arrow_Rim", _arrowSprite, BaseOrder + 8);
        rim.color = arrowRimColor;
        rim.transform.localRotation = Quaternion.identity;
        rim.transform.localScale = new Vector3(1.16f, 1.05f, 1f);

        SpriteRenderer head = CreateOrGetRenderer(_arrowRoot, "Arrow_Head", _arrowSprite, BaseOrder + 9);
        head.color = arrowColor;
        head.transform.localRotation = Quaternion.identity;
        head.transform.localScale = new Vector3(0.98f, 0.86f, 1f);

        SpriteRenderer shine = CreateOrGetRenderer(_arrowRoot, "Arrow_Highlight", _markerSprite, BaseOrder + 10);
        shine.color = arrowHighlightColor;
        shine.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        shine.transform.localRotation = Quaternion.identity;
        shine.transform.localScale = new Vector3(0.56f, 0.10f, 1f);

        _arrowDots = new SpriteRenderer[4];
        for (int i = 0; i < _arrowDots.Length; i++)
        {
            SpriteRenderer dot = CreateOrGetRenderer(_arrowRoot, $"Arrow_Dot_{i + 1}", _circleSprite, BaseOrder + 8);
            dot.color = new Color(1f, 0.72f, 0.04f, 0.95f - i * 0.12f);
            dot.transform.localPosition = new Vector3(0f, -0.46f - i * 0.34f, 0f);
            dot.transform.localRotation = Quaternion.identity;
            dot.transform.localScale = new Vector3(0.26f - i * 0.02f, 0.26f - i * 0.02f, 1f);
            _arrowDots[i] = dot;
        }
    }

    private IEnumerator PulseFrame()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float wave = (Mathf.Sin(t * 4.2f) + 1f) * 0.5f;
            float scale = 1f + wave * 0.035f;

            if (_frameRoot != null && !_suppressFramePulse)
                _frameRoot.localScale = new Vector3(scale, scale, 1f);

            Color fill = _lastValid ? validFillColor : invalidFillColor;
            fill.a *= Mathf.Lerp(0.72f, 1.25f, wave);
            if (_fill != null)
                _fill.color = fill;

            Color edge = _lastValid ? validEdgeColor : invalidEdgeColor;
            edge.a *= Mathf.Lerp(0.82f, 1f, wave);

            if (_edges != null)
                foreach (SpriteRenderer sr in _edges)
                    if (sr != null) sr.color = edge;

            Color dark = _lastValid ? validEdgeDarkColor : invalidEdgeDarkColor;
            dark.a *= Mathf.Lerp(0.78f, 1f, wave);
            if (_edgeShadows != null)
                foreach (SpriteRenderer sr in _edgeShadows)
                    if (sr != null) sr.color = dark;

            Color highlight = _lastValid ? validEdgeHighlightColor : invalidEdgeHighlightColor;
            highlight.a *= Mathf.Lerp(0.45f, 0.92f, wave);
            if (_edgeHighlights != null)
                foreach (SpriteRenderer sr in _edgeHighlights)
                    if (sr != null) sr.color = highlight;

            if (_corners != null)
                foreach (SpriteRenderer sr in _corners)
                    if (sr != null)
                    {
                        Color c = edge;
                        c.a *= Mathf.Lerp(0.78f, 1f, wave);
                        sr.color = c;
                    }

            yield return null;
        }
    }

    private IEnumerator SpawnPop(bool stronger)
    {
        float start = stronger ? 0.55f : 0.72f;
        float overshoot = stronger ? 1.18f : 1.10f;

        _suppressFramePulse = true;
        yield return ScaleFrame(start, overshoot, 0.12f);
        yield return ScaleFrame(overshoot, 1f, 0.14f);
        _suppressFramePulse = false;
        _spawnPop = null;
    }

    private IEnumerator ScaleFrame(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);
            float scale = Mathf.LerpUnclamped(from, to, eased);
            if (_frameRoot != null)
                _frameRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }

    private IEnumerator PulseArrow()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float bob = Mathf.Sin(t * 5.4f);
            float shine = (Mathf.Sin(t * 8f) + 1f) * 0.5f;

            if (_arrowRoot != null)
            {
                _arrowRoot.localScale = Vector3.one * (1f + bob * 0.055f);
                _arrowRoot.localPosition = _arrowBaseLocalPosition + new Vector3(0f, bob * 0.16f, 0f);
            }

            if (_arrowDots != null)
            {
                for (int i = 0; i < _arrowDots.Length; i++)
                {
                    SpriteRenderer dot = _arrowDots[i];
                    if (dot == null) continue;

                    float phase = Mathf.Repeat(shine + i * 0.18f, 1f);
                    Color c = dot.color;
                    c.a = Mathf.Lerp(0.35f, 0.95f, 1f - phase);
                    dot.color = c;
                    float s = (0.26f - i * 0.02f) * Mathf.Lerp(0.85f, 1.18f, phase);
                    dot.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            yield return null;
        }
    }

    private IEnumerator InvalidNudge()
    {
        if (_frameRoot == null)
        {
            _invalidPulse = null;
            yield break;
        }

        Vector3 basePos = _frameRoot.localPosition;
        Vector3 baseScale = _frameRoot.localScale;
        bool wasSuppressingFramePulse = _suppressFramePulse;
        _suppressFramePulse = true;
        const float duration = 0.16f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t);
            _frameRoot.localPosition = basePos + new Vector3(wave * 0.08f, 0f, 0f);
            _frameRoot.localScale = baseScale * (1f + (1f - t) * 0.04f);
            yield return null;
        }

        _frameRoot.localPosition = basePos;
        _frameRoot.localScale = baseScale;
        _suppressFramePulse = wasSuppressingFramePulse;
        _invalidPulse = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // V6 — THANH XÁC NHẬN KIỂU TOWNSHIP
    //
    //   ┌──────────────────────────────┐  ← shop_card_outer (khung nâu, art thật)
    //   │ ╔══════════════════════════╗ │  ← shop_card_inner (giấy kem, art thật)
    //   │ ║ ▓MUA VỚI GIÁ 🪙 30▓      ║ │  ← ribbon_banner_gold (ruy băng vàng, art thật)
    //   │ ║   [✗]          (✓)       ║ │  ← ✗ VUÔNG đỏ 128 · ✓ ĐĨA xanh 148
    //   │ ╚══════════════════════════╝ │
    //   └──────────────────────────────┘
    //
    // V11: ✗ VUÔNG BO GÓC (btn_red_small 9-slice) · ✓ ĐĨA TRÒN (check_badge_green).
    // Nút ↻ XOAY đang bị BindGhostButtons SetActive(false) — Sếp đã bỏ tính năng xoay.
    // Nút 🗑 XOÁ chỉ tồn tại ở Editor / DEVELOPMENT_BUILD.
    //
    // VÌ SAO XÁC NHẬN Ở BÊN PHẢI: thuận tay phải, và quan trọng hơn — nó ĐỨNG XA nút huỷ
    // nhất. Bản cũ để ✓ ngay cạnh ✕ nên bấm trượt là mất luôn lượt đặt.
    // ══════════════════════════════════════════════════════════════════════

    private const string ConfirmPanelName = "Confirm_Bar_Panel";

    // ĐƠN VỊ: canvas `Placement_UI` có localScale 0.01 nằm dưới root scale 100 ⇒ tích = 1,
    // nên 1 "pixel" UI ở đây đúng bằng 1 world unit. Hàng nút cao 126, tâm ở y = 0
    // (xem PlacementManager.StyleGhostActionBar) ⇒ nút chiếm y ∈ [−63, +63].
    // V10: 8 hằng số cứng cũ đã thành [SerializeField] ở đầu file để Sếp tinh chỉnh được.
    // Con số cũ (438/218/35/100/56/44/12/62) là thủ phạm "chữ bé xíu, nút bé": ở zoom hết ra
    // (ortho 1500) chữ 38 chỉ còn 13.7 px và nút 120 chỉ còn 43 px trên máy cao 1080.
    private const float CanvasScaleGoc = 0.01f;   // localScale gốc của Placement_UI trong prefab

    /// <summary>
    /// Dựng NỀN TỐI BO GÓC bọc cả cụm + đổi 3 nút vuông thành 3 nút TRÒN đúng thứ tự.
    ///
    /// DỰNG BẰNG CODE, KHÔNG SỬA PREFAB: prefab `Placement_Ghost` là file YAML dùng chung
    /// với DEV-1 (hàng nút, canvas, footprint đều nằm trong đó) — thêm node bằng tay dễ
    /// đụng độ merge, mà Edric cũng không phải mở prefab chỉnh gì. Chạy runtime thì mọi
    /// công trình đều tự có thanh xác nhận, kể cả prefab ghost sau này bị thay.
    ///
    /// NỀN LÀM CON CỦA `Button_Row` (không phải em ruột của nó) vì hai lý do:
    ///   1. `PlacementManager.AnimateGhostActionBar` scale Button_Row lúc bật lên
    ///      (0.45 → 1.08 → 1). Là CON thì nền pop CÙNG hàng nút; là em ruột thì nút nảy mà
    ///      nền đứng yên, đọc ra hai lớp rời nhau.
    ///   2. Đặt ở SIBLING INDEX 0 thì UGUI vẽ nó TRƯỚC → nằm dưới 3 nút, khỏi phải đụng vào
    ///      sortingOrder của canvas (thứ DEV-1 đang gán trong ConfigureGhostCanvas).
    /// Bắt buộc có `LayoutElement.ignoreLayout = true`, nếu không HorizontalLayoutGroup của
    /// Button_Row coi nền là "nút thứ 4" và xếp nó vào hàng.
    ///
    /// Gọi lại được nhiều lần: có cổng `_barBuilt`, và nếu Button_Row chưa tồn tại thì thoát
    /// im lặng để Update() thử lại frame sau.
    /// </summary>
    private void EnsureConfirmBar()
    {
        if (_barBuilt) return;

        Transform row = FindChildDeep(transform, "Button_Row");
        if (row == null) return;                       // prefab chưa dựng xong → thử lại sau

        RectTransform rowRect = row as RectTransform;
        if (rowRect == null) return;

        // Nền xanh nhạt do PlacementManager.StyleGhostActionBar gắn lên chính Button_Row.
        // Giữ tham chiếu để RefreshConfirmBar() ép nó trong suốt (xem ghi chú ở đó).
        _barRowBg = row.GetComponent<Image>();

        ConstructionArtKit kit = ConstructionManager.Instance != null
                               ? ConstructionManager.Instance.ArtKit : null;

        // ── 1. NỀN TỐI BO GÓC ────────────────────────────────────────────────
        Transform old = row.Find(ConfirmPanelName);
        if (old != null) Destroy(old.gameObject);

        var panelGo = new GameObject(ConfirmPanelName, typeof(RectTransform));
        panelGo.layer = row.gameObject.layer;
        _barPanel = (RectTransform)panelGo.transform;
        _barPanel.SetParent(rowRect, false);
        _barPanel.anchorMin        = new Vector2(0.5f, 0.5f);
        _barPanel.anchorMax        = new Vector2(0.5f, 0.5f);
        _barPanel.pivot            = new Vector2(0.5f, 0.5f);
        _barPanel.anchoredPosition = new Vector2(0f, theTamY);
        _barPanel.sizeDelta        = new Vector2(theRongToiThieu, theCao);
        _barPanel.SetAsFirstSibling();

        var ignore = panelGo.AddComponent<LayoutElement>();
        ignore.ignoreLayout = true;

        // ── CARD BO GÓC: KHUNG NGOÀI + NỀN TRONG ─────────────────────────────
        // HAI Image lồng nhau, không phải một: đó là cách duy nhất cho khung và nền HAI MÀU
        // KHÁC NHAU. Cùng mẫu với popup_frame_wood + popup_panel_paper của popup còn lại.
        //
        // V11 — ART THẬT LÀ ĐƯỜNG CHÍNH. Đã kiểm tay từng file:
        //   shop_card_outer 160x210 border 30/30/30/30 · CÓ trong Resources/UI/Standard
        //   shop_card_inner 140x170 border 28/28/28/28 · CÓ trong Resources/UI/Standard
        // Card 392x244 ⇒ cần 60 và 56 px cho hai vành ⇒ Sliced còn dư chỗ, góc bo KHÔNG méo
        // kể cả khi card tự nới theo chữ.
        // `PlacementKitSpriteFactory.TheKhungNgoai()` bọc sẵn cả nhánh dự phòng thủ tục
        // nên ở đây KHÔNG cần `??` nữa — một chỗ quyết định, khỏi lệch giữa hai file.
        Sprite sprKhung = PlacementKitSpriteFactory.TheKhungNgoai();

        var khungBg = panelGo.AddComponent<Image>();
        khungBg.sprite        = sprKhung;
        khungBg.type          = PlacementKitSpriteFactory.LaSprite9Slice(sprKhung)
                              ? Image.Type.Sliced : Image.Type.Simple;
        khungBg.color         = mauVienThe;   // V11 = TRẮNG, xem tooltip của field
        khungBg.raycastTarget = false;   // KHÔNG chặn tia chuột tới 2 nút nằm trên nó

        var shadow = panelGo.AddComponent<Shadow>();
        shadow.effectColor    = new Color(0f, 0f, 0f, 0.34f);
        shadow.effectDistance = new Vector2(0f, -8f);

        // Nền trong: ô art `PriceBarBg` của ArtKit VẪN được tôn trọng nếu Sếp gán đúng một
        // sprite 9-slice. Trước vòng này nó đang trỏ vào btn_CloseRanking.png (1179x211,
        // spriteBorder 0/0/0/0) tức NÚT CLOSE của bảng xếp hạng — border 0 làm Sliced tụt
        // về kéo giãn phẳng, ảnh 1179 bị bóp về 438 nên góc bo bake trong art méo thành GÓC
        // CỨNG. Đó chính là "hộp nâu chữ nhật trơn, viền cứng".
        ConstructionArtKit.ResolveSafe(kit, ConstructionArtKit.Slot.PriceBarBg,
                                       PlacementKitSpriteFactory.TheNenTrong(),
                                       out Sprite sprNen, out Color boMauNen);
        // `boMauNen` CỐ Ý không dùng: `mauNenThe` là nguồn sự thật duy nhất cho màu nền
        // card. Biến out gán mà không đọc thì C# KHÔNG cảnh báo, nên để tên thật cho dễ
        // đọc thay vì discard `out Color _`.

        // Con ĐẦU TIÊN của _barPanel ⇒ UGUI vẽ nó TRƯỚC 3 node chữ tạo ở bước 2 ⇒ nằm DƯỚI
        // chữ mà khỏi phải đụng sortingOrder.
        var nenGo = new GameObject("The_Nen", typeof(RectTransform));
        nenGo.layer = row.gameObject.layer;
        var nenRt = (RectTransform)nenGo.transform;
        nenRt.SetParent(_barPanel, false);
        nenRt.anchorMin = Vector2.zero;                       // kéo đầy cha rồi thụt vào
        nenRt.anchorMax = Vector2.one;                        // ⇒ card nới thì nền tự nới theo
        nenRt.offsetMin = new Vector2( dayVienThe,  dayVienThe);
        nenRt.offsetMax = new Vector2(-dayVienThe, -dayVienThe);

        var nenBg = nenGo.AddComponent<Image>();
        nenBg.sprite        = sprNen;
        nenBg.type          = PlacementKitSpriteFactory.LaSprite9Slice(sprNen)
                            ? Image.Type.Sliced : Image.Type.Simple;
        nenBg.color         = mauNenThe;
        nenBg.raycastTarget = false;
        _barNenGiay         = nenBg;   // V11: RefreshConfirmBar nhuộm hồng khi không đặt được

        if (ConstructionArtKit.WantLabels(kit))
            ConstructionSiteVisuals.AttachSlotLabel(_barPanel,
                                                    ConstructionArtKit.Slot.PriceBarBg, kit);

        // ── 2. HÀNG GIÁ: RUY BĂNG VÀNG THẬT + chữ + icon tiền + số ───────────
        // Sếp: "gắn assets đã có vào, đừng chỉ dựng khung nền". Ruy băng là art THẬT
        // (ribbon_banner_gold 128x48, border 28/14/28/14, CÓ trong Resources/UI/Standard),
        // dựng TRƯỚC 3 node chữ nên UGUI vẽ nó DƯỚI chữ mà khỏi đụng sortingOrder.
        // Nó làm ba việc: (1) tách hàng giá khỏi hàng nút thành hai tầng đọc rõ ràng,
        // (2) cho chữ nâu sẫm một nền vàng tương phản cao, (3) là chi tiết art thật đầu
        // tiên trên card thay vì thêm một hình chữ nhật vẽ bằng code.
        Sprite sprRuyBang = PlacementKitSpriteFactory.TheRuyBangGia();
        _barRuyBang = MakeBarIcon(_barPanel, "Ruy_Bang_Gia");
        _barRuyBang.sprite         = sprRuyBang;
        _barRuyBang.type           = PlacementKitSpriteFactory.LaSprite9Slice(sprRuyBang)
                                   ? Image.Type.Sliced : Image.Type.Simple;
        _barRuyBang.preserveAspect = false;   // Sliced + preserveAspect là hai thứ đánh nhau
        _barRuyBang.color          = Color.white;   // ribbon đã bake vàng (255,195,60)

        _barLabel  = MakeBarText(_barPanel, "Text_Nhan", buyLabel, coChuNhan);
        _barCoin   = MakeBarIcon(_barPanel, "Icon_Tien");
        _barNumber = MakeBarText(_barPanel, "Text_Gia", "0", coChuSo);

        // ── 3. BỐN NÚT, THỨ TỰ 🗑 → ✗ → ↻ → ✓ (bản phát hành chỉ hiện ✗ và ✓) ─
        // Nền đang ở index 0 nên nút bắt đầu từ 1. Layout group bỏ qua nền (ignoreLayout)
        // và chỉ xếp 3 nút theo thứ tự tương đối 1 < 2 < 3.
        // Nút XOÁ nằm ở NGOÀI CÙNG BÊN TRÁI — xa nút ✓ nhất có thể.
        // Đây là hành động phá hoại, đặt cạnh xác nhận là mời tai nạn.
        EnsureDeleteButton(row);

        // ✗ và ✓ khác nhau BA kênh ĐỘC LẬP, không chỉ khác màu (trẻ chưa đọc chữ + người mù màu):
        //   HÌNH : ✗ VUÔNG BO GÓC (btn_red_small 9-slice) · ✓ ĐĨA TRÒN (check_badge_green)
        //   CỠ   : ✗ 128                                  · ✓ 148 (to hơn 15.6 %)
        //   MÀU  : ✗ đỏ (221,76,69) bake sẵn              · ✓ xanh (110,195,45) bake sẵn
        //
        // 🔴 HÌNH ĐÃ ĐỔI VAI so với vòng 10 (trước: ✗ tròn / ✓ vuông). ART THẬT quyết định:
        // check_badge_green là ĐĨA border 0 (chỉ đúng khi Simple), btn_red_small là THANH
        // 256x96 border 28 (chỉ đúng khi Sliced). Ép ngược là đĩa bị kéo méo, thanh bị bóp dẹt.
        // Đổi vai KHÔNG làm mất kênh nào: vẫn ba kênh, và ✓ tròn xanh là chuẩn Township.
        //
        // TINT = TRẮNG cho ✗ / ✓ / ↻ vì art đã bake màu. Chỉ 🗑 XOÁ mới tint (0.70,0.58,0.58)
        // để đỏ tươi (221,76,69) thành đỏ GẠCH SẪM (155,44,40): hai nút này nằm cạnh nhau mà
        // hậu quả khác hẳn (huỷ = trả về chỗ cũ · xoá = mất hẳn công trình) nên phải khác màu.
        // VỊ TRÍ GIỮ NGUYÊN (✗ trái, ✓ phải) để không phá phản xạ tay của người đã chơi.
        StyleRoundButton(row, "Btn_Delete",  deleteButtonColor,  1,
                         PlacementKitSpriteFactory.GlyphXoa(),
                         PlacementKitSpriteFactory.NenNutHuy(),      coNutHuy,      coGlyphHuy);
        StyleRoundButton(row, "Btn_Cancel",  cancelButtonColor,  2,
                         ConstructionSpriteFactory.CrossMark(),
                         PlacementKitSpriteFactory.NenNutHuy(),      coNutHuy,      coGlyphHuy);
        StyleRoundButton(row, "Btn_Rotate",  rotateButtonColor,  3,
                         ConstructionSpriteFactory.RotateArrow(),
                         PlacementKitSpriteFactory.NenNutXoay(),     coNutHuy,      coGlyphHuy);
        StyleRoundButton(row, "Btn_Confirm", confirmButtonColor, 4,
                         PlacementKitSpriteFactory.GlyphXacNhan(),
                         PlacementKitSpriteFactory.NenNutXacNhan(),  coNutXacNhan,  coGlyphXacNhan);

        // GHI ĐÈ m_Spacing = 20 của prefab. MỘT LẦN lúc dựng, không ghi mỗi frame.
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.spacing = kheGiuaHaiNut;

        // Ghi nhớ cặp (Button, glyph) để RefreshConfirmBar() làm mờ glyph khi nút bị disable.
        _barButtons  = new Button[3];
        _barGlyphs   = new Image[3];
        _barGlyphDim = new bool[3];
        CacheGlyphPair(row, "Btn_Cancel",  0);
        CacheGlyphPair(row, "Btn_Rotate",  1);
        CacheGlyphPair(row, "Btn_Confirm", 2);

        _barBuilt = true;

        // Ép vẽ nội dung ngay frame này, khỏi nháy một frame với chữ mặc định "MUA VỚI GIÁ 0".
        _barLastLabel = null;
        RefreshConfirmBar();
    }

    /// <summary>
    /// Đọc trạng thái ĐỘNG của PlacementManager rồi cập nhật hàng giá.
    ///
    /// HỢP ĐỒNG API §4 (DEV-1 đã chốt): `IsFreeMove`, `CurrentPriceGold`, `CurrentPriceGem`.
    /// Hai giá TỰ TRẢ 0 khi đang di chuyển vật có sẵn, nên chỉ cần một nhánh `IsFreeMove`.
    /// KHÔNG còn dùng `ConstructionBridge.GetGhostItem()` (reflection) — DEV-1 đã mở
    /// property công khai, đọc thẳng vừa nhanh vừa không vỡ khi họ đổi tên field private.
    /// </summary>
    private void RefreshConfirmBar()
    {
        if (!_barBuilt || _barPanel == null) return;

        // Nền xanh nhạt mà `StyleGhostActionBar` gắn lên Button_Row chạy SAU EnsureBuilt
        // (nó nằm trong AnimateGhostActionBar, được StartCoroutine ở CUỐI
        // StartPlacingNewObject / StartEditBuilding) → không thể xử lý một lần lúc dựng.
        //
        // ⚠ Nó còn có thể ADD Image lên Button_Row nếu prefab chưa có, tức lúc EnsureConfirmBar
        // chạy thì GetComponent<Image> trả null. Vì vậy phải tra LẠI ở đây tới khi thấy —
        // nếu không, dải xanh nhạt sẽ hắt lên qua nền tối (nền chỉ đục 88 %).
        if (_barRowBg == null && _barPanel.parent != null)
            _barRowBg = _barPanel.parent.GetComponent<Image>();

        // Ép trong suốt; có cổng chặn nên thực tế chỉ ghi ĐÚNG MỘT LẦN, không dirty canvas
        // mỗi frame.
        if (_barRowBg != null && _barRowBg.color.a > 0.002f)
            _barRowBg.color = new Color(1f, 1f, 1f, 0f);

        // ── NÚT XOÁ: CHỈ hiện khi đang SỬA vật đã có trên map ───────────────
        // Lúc mua mới từ shop thì "xoá" vô nghĩa — nút ✕ đã hoàn tiền và bỏ đi rồi.
        // Hiện thêm nút xoá ở đó chỉ làm người chơi bấm nhầm và mất tiền.
        if (_deleteButton != null)
        {
            bool nenHien = PlacementManager.Instance != null
                        && PlacementManager.Instance.IsEditingBuilding;
            if (_deleteButton.gameObject.activeSelf != nenHien)
            {
                _deleteButton.gameObject.SetActive(nenHien);

                // V11: SỐ NÚT ĐANG HIỆN vừa đổi ⇒ bề rộng cụm nút đổi ⇒ card phải xếp lại,
                // nếu không nút 🗑 thò ra ngoài tờ giấy. Xoá nhãn cũ là cổng chặn của
                // RefreshConfirmBar mở ra và LayoutPriceRow chạy ngay frame này.
                // Chỉ ở Editor / DEVELOPMENT_BUILD mới có nút 🗑 nên đây là đường hiếm,
                // KHÔNG phải việc mỗi frame.
                if (_rongCumNutCuoi != RongCumNutHienTai()) _barLastLabel = null;
            }
        }

        // ── LÀM MỜ GLYPH THEO TRẠNG THÁI NÚT ────────────────────────────────
        // Unity ColorTint chỉ tô lại `targetGraphic` (Image CỦA NÚT), KHÔNG chạm tới graphic
        // con. Nếu không tự đồng bộ thì nút ✓ bị disable sẽ thành "đĩa xanh mờ 50 % + dấu
        // tick TRẮNG CHÓI" — mắt đọc ra UI lỗi, chứ không phải "chưa bấm được".
        // Chỉ nút ✓ thực sự bị disable (PlacementManager gán mỗi frame) nhưng làm cho cả 3
        // để sau này thêm điều kiện cho ↻ / ✕ là tự đúng.
        if (_barButtons != null)
        {
            for (int i = 0; i < _barButtons.Length; i++)
            {
                Button b = _barButtons[i];
                Image  g = _barGlyphs[i];
                if (b == null || g == null) continue;

                bool dim = !b.interactable;
                if (dim == _barGlyphDim[i]) continue;   // không đổi → khỏi dirty canvas

                _barGlyphDim[i] = dim;
                // V11: 0.90 chứ không 0.45. Ở 0.45 dấu ✓ gần như tan biến ⇒ người chơi
                // không còn biết nút đó LÀ nút gì. Việc "đang tắt" đã do NỀN nút nói
                // (mauNutKhiTat, đục hẳn) và do TỜ GIẤY nói (khối ngay dưới đây).
                g.color = dim ? new Color(1f, 1f, 1f, doMoGlyphKhiTat) : Color.white;
            }
        }

        // ── KÊNH THỨ BA CỦA TRẠNG THÁI TẮT: TỜ GIẤY BÁO LÝ DO ───────────────
        // Nút ✓ mờ chỉ nói "chưa bấm được". Tờ giấy chuyển hồng đất nói "vì CHỖ NÀY".
        // Không có kênh này thì người chơi (nhất là trẻ) kết luận nút bị hỏng — đúng thứ
        // Sếp đọc ra từ ảnh. Đọc `_lastValid` (SetValid cấp mỗi frame, cùng nguồn với 4
        // chevron đỏ) chứ KHÔNG đọc `btnConfirm.interactable`: hai thứ cùng gốc isValidPos
        // nhưng `_lastValid` không phụ thuộc việc BindGhostButtons đã chạy chưa.
        if (_barNenGiay != null && _theDangBaoLoi != !_lastValid)
        {
            _theDangBaoLoi     = !_lastValid;
            _barNenGiay.color  = _theDangBaoLoi ? mauNenTheKhongHopLe : mauNenThe;
        }

        PlacementManager pm = PlacementManager.Instance;
        bool free = pm == null || pm.IsFreeMove;
        int  gold = pm != null ? pm.CurrentPriceGold : 0;
        int  gem  = pm != null ? pm.CurrentPriceGem  : 0;

        bool useGem = gold <= 0 && gem > 0;
        int  price  = useGem ? gem : gold;

        // Vật giá 0 (decor tặng, ô đất mở sẵn) cũng hiện "ĐẶT MIỄN PHÍ". Hiện "MUA VỚI GIÁ"
        // rồi để trống số thì người chơi tưởng UI lỗi.
        bool   showMoney = !free && price > 0;
        string label     = showMoney ? buyLabel : freeMoveLabel;
        string number    = showMoney ? price.ToString() : string.Empty;

        // Không đổi gì thì thoát: TMP dựng lại mesh mỗi lần gán text, 60 fps là tốn vô ích.
        if (label == _barLastLabel && number == _barLastNumber &&
            useGem == _barLastGem && showMoney == _barLastMoney)
            return;

        _barLastLabel  = label;
        _barLastNumber = number;
        _barLastGem    = useGem;
        _barLastMoney  = showMoney;

        LayoutPriceRow(label, number, showMoney, useGem);
    }

    /// <summary>
    /// Xếp chữ + icon tiền + số THỦ CÔNG rồi co nền cho vừa.
    ///
    /// VÌ SAO KHÔNG DÙNG HorizontalLayoutGroup + ContentSizeFitter (bản cũ dùng):
    /// ContentSizeFitter ghi `sizeDelta` trong `SetLayoutHorizontal`, còn layout group cha
    /// đọc `sizeDelta` trong `CalculateLayoutInputHorizontal` — hai bước này thuộc HAI PHA
    /// khác nhau của LayoutRebuilder (pha tính chạy từ con lên, pha áp chạy từ cha xuống),
    /// nên bề rộng nền luôn CHẬM MỘT FRAME so với chữ. Đổi giữa "ĐẶT MIỄN PHÍ" và
    /// "MUA VỚI GIÁ 30" là thấy nền giật một nhịp. Tự tính thì đúng ngay trong frame đó.
    ///
    /// `TMP_Text.preferredWidth` đo bằng chiều rộng vô hạn nên KHÔNG phụ thuộc sizeDelta
    /// hiện tại — đọc trước khi đặt kích thước là an toàn.
    /// </summary>
    private void LayoutPriceRow(string label, string number, bool showMoney, bool useGem)
    {
        if (_barLabel == null) return;

        _barLabel.text = label;
        float labelW = Mathf.Max(1f, _barLabel.preferredWidth);
        _barLabel.rectTransform.sizeDelta = new Vector2(labelW + 8f, hangGiaCao);

        float numberW = 0f;
        if (_barNumber != null)
        {
            _barNumber.gameObject.SetActive(showMoney);
            if (showMoney)
            {
                _barNumber.text = number;
                numberW = Mathf.Max(1f, _barNumber.preferredWidth);
                _barNumber.rectTransform.sizeDelta = new Vector2(numberW + 8f, hangGiaCao);
            }
        }

        if (_barCoin != null)
        {
            _barCoin.gameObject.SetActive(showMoney);
            if (showMoney)
                _barCoin.sprite = useGem
                    ? ConstructionSpriteFactory.GemIcon()
                    : ConstructionSpriteFactory.CoinIcon();
        }

        float total = labelW
                    + (showMoney ? kheHangGia + coIconTien + kheHangGia * 0.6f + numberW : 0f);
        float x = -total * 0.5f;

        _barLabel.rectTransform.anchoredPosition = new Vector2(x + labelW * 0.5f, hangGiaY);
        x += labelW;

        if (showMoney)
        {
            x += kheHangGia;
            if (_barCoin != null)
                _barCoin.rectTransform.anchoredPosition =
                    new Vector2(x + coIconTien * 0.5f, hangGiaY);

            x += coIconTien + kheHangGia * 0.6f;
            if (_barNumber != null)
                _barNumber.rectTransform.anchoredPosition =
                    new Vector2(x + numberW * 0.5f, hangGiaY);
        }

        // ── BỀ RỘNG CARD ─────────────────────────────────────────────────────
        // Nền phải bọc được HAI thứ, lấy cái rộng hơn:
        //   (1) hàng chữ dài nhất — tiếng Việt dài hơn tiếng Đức của ảnh mẫu;
        //   (2) CỤM NÚT THẬT ĐANG HIỆN. (2) là điều vòng 10 bỏ sót: theRongToiThieu là
        //       một CON SỐ CỨNG, nên hôm nào Sếp bật lại choPhepXoayCongTrinh (3 nút) hoặc
        //       chạy Editor build (thêm 🗑 = 4 nút) là nút thò ra ngoài tờ giấy. Đo thẳng
        //       hàng nút thì mọi cấu hình đều tự vừa.
        // TÍNH TRƯỚC ruy băng: ruy băng cần biết bề rộng card MỚI, không phải của frame cũ.
        float rongCumNut    = RongCumNutHienTai();
        _rongCumNutCuoi     = rongCumNut;
        _theRongHienTai     = Mathf.Max(theRongToiThieu,
                              Mathf.Max(total + theLeNgang, rongCumNut + leNutTrongThe));
        _barPanel.sizeDelta = new Vector2(_theRongHienTai, theCao);

        // ── RUY BĂNG VÀNG: bọc đúng hàng giá, KHÔNG kéo đầy card ─────────────
        // Bọc `total` + lề 26 mỗi bên, nhưng luôn hẹp hơn card 44 px (22 mỗi bên) để mắt
        // đọc ra HAI TẦNG (giá ở trên · nút ở dưới). Kéo đầy card thì hai tầng dính lại
        // thành một khối và ruy băng mất tác dụng phân tầng.
        // Sàn 120 để 9-slice không bị bóp: ribbon_banner_gold border ngang 28 + 28 = 56.
        if (_barRuyBang != null)
        {
            float rongRuyBang = Mathf.Min(total + 52f, Mathf.Max(120f, _theRongHienTai - 44f));
            _barRuyBang.rectTransform.sizeDelta        = new Vector2(rongRuyBang, hangGiaCao + 6f);
            _barRuyBang.rectTransform.anchoredPosition = new Vector2(0f, hangGiaY);
        }
    }

    /// <summary>
    /// Bề rộng THẬT của cụm nút đang hiện trong Button_Row = tổng sizeDelta.x của các con
    /// ĐANG BẬT (bỏ chính card, vì card mang LayoutElement.ignoreLayout) + khe giữa chúng.
    ///
    /// Đúng công thức HorizontalLayoutGroup đang xếp, nên card không bao giờ hẹp hơn nút.
    /// Vòng qua tối đa 5 con, gọi CHỈ KHI nội dung hàng giá đổi ⇒ không phải việc mỗi frame.
    /// </summary>
    private float RongCumNutHienTai()
    {
        Transform row = _barPanel != null ? _barPanel.parent : null;
        if (row == null) return coNutXacNhan + kheGiuaHaiNut + coNutHuy;

        float tong = 0f;
        int   dem  = 0;
        for (int i = 0; i < row.childCount; i++)
        {
            Transform con = row.GetChild(i);
            if (con == null || con == (Transform)_barPanel) continue;
            if (!con.gameObject.activeSelf) continue;
            if (con is RectTransform rt) { tong += Mathf.Abs(rt.sizeDelta.x); dem++; }
        }

        // Chưa thấy nút nào (EnsureConfirmBar vừa dựng, layout chưa chạy) → lấy cấu hình
        // BẢN PHÁT HÀNH (✗ + ✓) làm mức sàn, đừng trả 0 rồi để card co lại một frame.
        if (dem == 0) return coNutXacNhan + kheGiuaHaiNut + coNutHuy;
        return tong + kheGiuaHaiNut * (dem - 1);
    }

    // ══════════════════════════════════════════════════════════════════════
    // V10 — NEO CARD VÀO WORLD
    //
    // BA THỨ SAI Ở BẢN CŨ (đo trên SCN_Farm, ô lưới 300 x 150 sau khi Dev U sửa IsoGrid):
    //  1. `Placement_UI` đứng CỐ ĐỊNH ở localPosition (-0.04, -2.62); root scale 100 nên card
    //     luôn ở y = -262 world, chiếm y ∈ [-336, -118]. KHÔNG dòng code nào sửa (grep
    //     "Placement_UI" toàn bộ .cs chỉ ra 1 hit và là comment). Nửa cao vùng ô lại lớn dần
    //     theo footprint (IsoGrid.FootprintWorldSize, CellHeight 150): 1x1 = 75, 2x2 = 150.
    //     ⇒ 1x1 hở đúng 43 world (31 px ở zoom mặc định, 15 px khi zoom hết ra — dán vào
    //     chân nhà), còn 2x2 thì ĐÈ 32 world lên vùng ô. 2x2 là CHUỒNG và MÁY, đúng nhóm
    //     công trình đắt nhất mà người chơi ngắm lâu nhất. Đây là "che mất công trình".
    //  2. Canvas world-space có cỡ world CỐ ĐỊNH, camera zoom ortho 400 → 1500
    //     (CameraController.minSize/maxSize, defaultSize 750). Zoom hết ra là mọi thứ teo một
    //     nửa ⇒ chữ 38 còn 13.7 px trên máy cao 1080. Đây là "chữ bé xíu". Tăng cỡ chữ KHÔNG
    //     đủ, phải bù zoom.
    //  3. Không xử lý mép màn: công trình sát đáy màn là card lọt ra ngoài, hoặc chui xuống
    //     dưới thanh HUD (Canvas_HUD là Screen Space nên nó vẽ ĐÈ bất kể sortingOrder).
    //
    // NEO THEO MÉP DƯỚI VÙNG Ô, KHÔNG NEO GÓC MÀN HÌNH: mắt người chơi đang dán vào công
    // trình, card phải đi cùng nó; neo góc màn là bắt mắt đi hai nơi. Chọn phía DƯỚI vì sprite
    // công trình luôn vươn LÊN khỏi footprint (doc DEV-1 §5.1) nên dưới là chỗ chắc trống.
    // Neo theo `_caoVungOLocal` (vùng ô THẬT) ⇒ việc đè thành BẤT KHẢ THI với mọi cỡ footprint,
    // kể cả cỡ Dev U thêm sau này, chứ không phải chọn được một offset may mắn.
    //
    // Bù zoom scale cả Button_Row và 2 nút nằm trong đó, nên
    // `PlacementManager.IsMouseOverRect` (đọc trực tiếp RectTransform) tự đúng theo — KHÔNG
    // cần sửa gì bên PlacementManager. Và nó KHÔNG đụng `AnimateGhostActionBar` (hàm đó scale
    // Button_Row, đây scale Placement_UI ở trên một bậc, hai thứ nhân vào nhau bình thường).
    // ══════════════════════════════════════════════════════════════════════
    private void NeoCardVaoWorld()
    {
        if (!_barBuilt) return;

        if (_uiRoot == null)
            _uiRoot = FindChildDeep(transform, "Placement_UI") as RectTransform;
        if (_uiRoot == null) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        // ── 1. BÙ ZOOM + BÙ DPI ──────────────────────────────────────────
        // Bù DPI kẹp SÀN 1.0 (không kẹp trần bằng field): màn CAO HƠN 1080 thì hệ số < 1
        // sẽ THU NHỎ card — đúng về px nhưng card bé tí so với công trình, mà trên màn nhiều
        // pixel thì 0.72 x cỡ đã dư sức đọc. Kẹp sàn 1.0 ⇒ mọi máy 1080 trở lên chạy y
        // NGUYÊN như vòng 10, KHÔNG lệch một pixel.
        float heSoManHinh = Mathf.Clamp(caoManThamChieu / Mathf.Max(1f, Screen.height),
                                        1f, Mathf.Max(1f, heSoManHinhToiDa));
        float buZoom = Mathf.Clamp(cam.orthographicSize / Mathf.Max(1f, orthoThamChieu) * heSoManHinh,
                                   buZoomMin, buZoomMax);
        _uiRoot.localScale = Vector3.one * (CanvasScaleGoc * buZoom);

        float nuaRongCard = _theRongHienTai * 0.5f * buZoom;
        float nuaCaoCard  = theCao  * 0.5f * buZoom;
        float buTamKhoi   = theTamY * buZoom;

        // Khe hở PHẢI nhân buZoom cùng card. Để world cố định thì zoom hết ra card to gấp
        // đôi mà khe teo còn 17 px ⇒ nút cao 121 px cách công trình 17 px, dán chặt.
        float kheHo       = kheHoDuoiVungO * buZoom;

        // ── 2. VÙNG Ô: ĐỌC LẠI MỖI FRAME TỪ CurrentRect ──────────────────
        // 🔴 V11 — NGUYÊN NHÂN THỨ BA của "không bám sát", và là nguyên nhân sâu nhất.
        //
        // Bản cũ neo theo `_tamVungOLocal` / `_caoVungOLocal`, ba số mà
        // `ConfigureFromLocalBounds` ghi ĐÚNG MỘT LẦN. Đường gọi duy nhất là
        // `PlacementManager.SetupFootprint` → chỉ chạy ở StartPlacingNewObject (dòng ~858),
        // StartEditBuilding (~923) và RotateGhost (~1046) — KHÔNG chạy trong lúc kéo.
        // Đó là một ẢNH CHỤP, còn 4 chevron thì đọc `pm.CurrentRect` MỖI FRAME. Hai nguồn
        // sự thật khác nhau ⇒ khung 4 góc ôm đúng công trình mà card thì không.
        // (Dev V đã ghi đây là mục CHƯA CHẮC của mình — đo ra thì đúng là sai.)
        //
        // V11 lấy CÙNG MỘT NGUỒN với 4 chevron và với thảm xanh của PlacementManager:
        // `PlacementManager.CurrentRect` + `PlacementManager.CellCornerToWorld`.
        // Ảnh chụp cũ tụt xuống hàng DỰ PHÒNG (khi không có PlacementManager, ví dụ ghost
        // dùng cho màn xem trước).
        float nuaCaoVungO, nuaRongVungO;
        Vector3 tamVungO;
        if (!LayVungOTheoRect(out tamVungO, out nuaRongVungO, out nuaCaoVungO))
        {
            tamVungO     = transform.TransformPoint(_tamVungOLocal);
            // Bản cũ nhân CẢ HAI trục bằng lossyScale.y — sai trục cho bề rộng. Root ghost
            // đang scale đều 100 nên chưa lộ, nhưng ai đổi scale không đều là lệch ngay.
            nuaCaoVungO  = _caoVungOLocal  * 0.5f * Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
            nuaRongVungO = _rongVungOLocal * 0.5f * Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        }

        // ── 3. VỊ TRÍ LÝ TƯỞNG: ĐỈNH CARD SÁT DƯỚI MÉP DƯỚI VÙNG Ô ───────
        float yLyTuong = tamVungO.y - nuaCaoVungO - kheHo - buTamKhoi - nuaCaoCard;

        // TRẦN TRƯỜN LÊN: tâm card không được vượt quá TÂM vùng ô. Trườn tới đó thì card
        // chỉ phủ nửa dưới footprint (phần chân), còn sprite công trình vươn LÊN khỏi
        // footprint (doc DEV-1 §5.1) nên phần người chơi đang ngắm vẫn hở.
        float yTranTruonLen = tamVungO.y - buTamKhoi;

        Vector3 pos = new Vector3(tamVungO.x, yLyTuong, _uiRoot.position.z);

        // ── 4. MÉP MÀN: TÍNH THẲNG SANG WORLD, KHÔNG ĐI VÒNG QUA SCREEN ──
        // Camera orthographic + không xoay ⇒ world ↔ screen là phép tuyến tính, world/pixel
        // BẰNG NHAU cả hai trục (nửa cao = ortho, nửa rộng = ortho·aspect, aspect =
        // width/height ⇒ 2·ortho/height cả hai). Tính thẳng thì khỏi 3 lần WorldToScreenPoint
        // mỗi frame và khỏi lệ thuộc z của canvas.
        float wpp  = (2f * cam.orthographicSize) / Mathf.Max(1, Screen.height);
        float camX = cam.transform.position.x;
        float camY = cam.transform.position.y;

        // Mức world tương ứng hai lề an toàn. leAnToanDuoiPx = 170 là thứ DUY NHẤT giữ nút ✓
        // khỏi chui xuống dưới thanh HUD (Canvas_HUD là Screen Space nên nó vẽ ĐÈ bất kể
        // sortingOrder) — bấm trượt ✓ là mất một lượt mua.
        float yDayManWorld  = camY + (leAnToanDuoiPx - Screen.height * 0.5f) * wpp;
        float yDinhManWorld = camY + (Screen.height * 0.5f - leAnToanTrenPx) * wpp;

        // Quy về khoảng cho phép của TÂM _uiRoot (tâm card = pos.y + buTamKhoi).
        float yMin = yDayManWorld  + nuaCaoCard - buTamKhoi;
        float yMax = yDinhManWorld - nuaCaoCard - buTamKhoi;

        if (yMin < yMax)
        {
            // THỨ TỰ ƯU TIÊN, đọc từ trong ra ngoài:
            //   1. Kẹp trong viewport                      → card không bao giờ ra khỏi màn.
            //   2. Min với trần trườn lên                  → không leo quá tâm công trình.
            //   3. Max với yMin                            → lề đáy THẮNG hết: thà phủ công
            //      trình còn hơn để nút ✓ nằm dưới HUD và không bấm được.
            float yKep = Mathf.Clamp(yLyTuong, yMin, yMax);
            pos.y = Mathf.Max(Mathf.Min(yKep, yTranTruonLen), yMin);
        }

        // ── 5. LẬT SANG CẠNH — MẶC ĐỊNH TẮT (xem tooltip luonLatSangCanh) ─
        // Chỉ nổ khi Sếp bật lại VÀ đã trườn lên hết mà đáy card vẫn dưới lề.
        if (luonLatSangCanh && pos.y + buTamKhoi - nuaCaoCard < yDayManWorld - 0.5f)
        {
            bool sangPhai = tamVungO.x < camX;
            float dx = nuaRongVungO + kheHo + nuaRongCard;
            pos.x = tamVungO.x + (sangPhai ? dx : -dx);
            pos.y = tamVungO.y - buTamKhoi;
        }

        // ── 6. KẸP NGANG ─────────────────────────────────────────────────
        // xMin >= xMax nghĩa là card RỘNG HƠN cả viewport trừ hai lề — kẹp lúc đó sẽ giật.
        // Bỏ kẹp trục đó, thà card thò ra còn hơn nhảy loạn.
        float xMin = camX + (leAnToanNganPx - Screen.width * 0.5f) * wpp + nuaRongCard;
        float xMax = camX + (Screen.width * 0.5f - leAnToanNganPx) * wpp - nuaRongCard;
        if (xMin < xMax) pos.x = Mathf.Clamp(pos.x, xMin, xMax);

        _uiRoot.position = pos;
    }

    /// <summary>
    /// VÙNG Ô THẬT của ghost đang cầm, đọc lại MỖI FRAME.
    ///
    /// Nguồn sự thật: HỢP ĐỒNG API §4 — `PlacementManager.CurrentRect` (PlacementManager
    /// ghi nó trong Update mỗi frame) + `PlacementManager.CellCornerToWorld` (hàm CỦA DEV-1,
    /// KHÔNG tự nhân CELL ở đây). Đúng hai thứ mà `UpdateChevrons` đang dùng ⇒ card và 4
    /// chevron KHÔNG THỂ lệch nhau nữa.
    ///
    /// HÌNH HỌC ISO — 4 đỉnh kim cương, KHÔNG phải 4 góc chữ nhật:
    ///   CellCornerToWorld(x,y) = CellFloatToWorld(x−0.5, y−0.5)
    ///   CellFloatToWorld(c)    = ( o.x + (c.x−c.y)·W/2 , o.y + (c.x+c.y)·H/2 )
    /// Đặt a = xMin−0.5, b = yMin−0.5, N = width, M = height:
    ///   NAM   (xMin,yMin) → y THẤP NHẤT   (chân công trình)
    ///   ĐÔNG  (xMax,yMin) → x LỚN NHẤT
    ///   BẮC   (xMax,yMax) → y CAO NHẤT
    ///   TÂY   (xMin,yMax) → x NHỎ NHẤT
    /// ⇒ cao hộp bao  = BẮC.y − NAM.y = (N+M)·H/2   ≡ IsoGrid.FootprintWorldSize().y
    ///   rộng hộp bao = ĐÔNG.x − TÂY.x = (N+M)·W/2  ≡ IsoGrid.FootprintWorldSize().x
    ///   tâm = (NAM + BẮC)/2, khớp CẢ HAI trục với IsoGrid.RectCenterWorld().
    /// Đã kiểm tay bằng đại số, không phải đoán: 1x1 ⇒ 300 x 150, 2x2 ⇒ 600 x 300.
    /// </summary>
    private static bool LayVungOTheoRect(out Vector3 tam, out float nuaRong, out float nuaCao)
    {
        tam = Vector3.zero;
        nuaRong = 0f;
        nuaCao  = 0f;

        PlacementManager pm = PlacementManager.Instance;
        if (pm == null) return false;

        RectInt r = pm.CurrentRect;
        // HỢP ĐỒNG API §4: width == 0 nghĩa là KHÔNG có Ghost nào đang hoạt động.
        if (r.width <= 0 || r.height <= 0) return false;

        Vector3 nam  = PlacementManager.CellCornerToWorld(r.xMin, r.yMin);
        Vector3 dong = PlacementManager.CellCornerToWorld(r.xMax, r.yMin);
        Vector3 bac  = PlacementManager.CellCornerToWorld(r.xMax, r.yMax);
        Vector3 tay  = PlacementManager.CellCornerToWorld(r.xMin, r.yMax);

        tam     = (nam + bac) * 0.5f;
        nuaRong = Mathf.Abs(dong.x - tay.x) * 0.5f;
        nuaCao  = Mathf.Abs(bac.y  - nam.y) * 0.5f;

        // Lưới chưa nạp (CellWidth/Height = 0) thì thà rơi xuống ảnh chụp cũ còn hơn neo
        // card vào một vùng ô rộng 0.
        return nuaRong > 0.01f && nuaCao > 0.01f;
    }

    /// <summary>
    /// Gắn ART THẬT vào một nút của prefab + đặt lại thứ tự + đặt màu trạng thái TẮT.
    ///
    /// V11: tên hàm giữ nguyên (`StyleRoundButton`) để lịch sử git đọc được, nhưng nó KHÔNG
    /// còn ép nút thành hình tròn — HÌNH DO SPRITE QUYẾT ĐỊNH (xem khối chọn kiểu vẽ bên dưới).
    ///
    /// VÌ SAO GLYPH LÀ SPRITE CHỨ KHÔNG PHẢI KÝ TỰ: prefab có sẵn 3 node "Label" chứa ký tự
    /// Unicode nhưng cả 3 đang TẮT (m_IsActive: 0). Bật lên là đánh cược vào việc font TMP
    /// mặc định có đủ ✕ ↻ ✓ — thiếu một cái là hiện ô vuông trống.
    ///
    /// KHÔNG ĐỤNG `Button.interactable` — PlacementManager gán `btnConfirm.interactable =
    /// isValidPos` MỖI FRAME, nó là chủ sở hữu duy nhất của cờ đó.
    ///
    /// NHƯNG V11 CÓ GHI `Button.colors.disabledColor` MỘT LẦN lúc dựng. Đó KHÔNG phải
    /// tranh chấp: ColorTint của Button NHÂN màu trạng thái vào `Image.color` (qua
    /// CanvasRenderer), và màu trạng thái mặc định của prefab có ALPHA 0.502 ⇒ nút ✓ bị tắt
    /// trở thành nửa trong suốt, nhìn xuyên thấy mặt đất. Đó là "nhìn như hỏng", không phải
    /// "chưa bấm được". Xem tooltip của `mauNutKhiTat`.
    /// </summary>
    /// <summary>
    /// Tạo nút XOÁ nếu prefab chưa có. Prefab `Placement_Ghost` chỉ có 3 nút
    /// (Cancel / Rotate / Confirm) nên phải nhân bản một nút sẵn có — cách đó bảo đảm
    /// copy đúng mọi component (Button, Image, UIJuiceFeedback, LayoutElement) mà
    /// không phải dựng lại từ đầu và đoán prefab đang gắn những gì.
    ///
    /// TỰ NỐI onClick tại đây, KHÔNG dựa vào `PlacementManager.BindGhostButtons`:
    /// hàm đó chạy trong `StartEditBuilding` — có thể TRƯỚC khi `EnsureConfirmBar`
    /// tạo ra nút này. Nối tay ở đây là hết cửa đua tranh (race condition).
    /// </summary>
    private void EnsureDeleteButton(Transform row)
    {
        // BẢN PHÁT HÀNH: KHÔNG tạo nút xoá.
        // Đây là công cụ cho dev dọn map lúc test. Người chơi thật KHÔNG được có nó —
        // xoá công trình là hành động mất tiền không hoàn lại, và Township cũng không
        // cho xoá trực tiếp (chỉ "cất vào kho"). Muốn cho người chơi cất công trình thì
        // làm cơ chế riêng, đừng mở nút này ra.
        //
        // Bọc CẢ THÂN HÀM thay vì `return;` sớm: nếu dùng early-return thì ở bản release
        // mọi dòng phía sau thành unreachable → warning CS0162.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Đã có sẵn (prefab, hoặc lần dựng trước) → chỉ cần nắm lại tham chiếu.
        // KHÔNG return trắng: nếu bỏ trống `_deleteButton` thì khối ẩn/hiện trong
        // RefreshConfirmBar() không chạy, nút xoá sẽ hiện cả lúc MUA MỚI.
        Transform coSan = row.Find("Btn_Delete");
        if (coSan != null)
        {
            _deleteButton = coSan.GetComponent<Button>();
            return;
        }

        Transform src = row.Find("Btn_Cancel");
        if (src == null) return;                      // không có gì để nhân bản

        var clone = Instantiate(src.gameObject, row);
        clone.name = "Btn_Delete";

        // Nút gốc có thể đang bị PlacementManager gán listener CancelPlacement —
        // xoá sạch rồi nối lại đúng việc của nó.
        var btn = clone.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var pm = PlacementManager.Instance;
                if (pm != null && pm.IsEditingBuilding) pm.DeleteEditingBuilding();
            });
        }

        _deleteButton = btn;
#endif
    }

    private void StyleRoundButton(Transform row, string name, Color color,
                                  int siblingIndex, Sprite glyph,
                                  Sprite nenNut, float coNut, float coGlyph)
    {
        Transform t = row.Find(name);
        if (t == null) return;

        t.SetSiblingIndex(siblingIndex);

        // GHI sizeDelta vì prefab đang cứng 120x120 (chỉ 43 px màn khi zoom hết ra).
        // VÙNG BẤM RỘNG RA, không hẹp đi ⇒ PlacementManager.IsMouseOverRect(confirmRect)
        // vẫn đúng. TÊN Btn_Confirm / Btn_Cancel và THỨ TỰ CHA CON KHÔNG ĐỔI ⇒
        // BindGhostButtons còn nguyên liên kết.
        var trt = t as RectTransform;
        if (trt != null) trt.sizeDelta = new Vector2(coNut, coNut);

        Image img = t.GetComponent<Image>();
        if (img != null)
        {
            // V11 — KIỂU VẼ SUY TỪ CHÍNH SPRITE, không gán cứng nữa:
            //   border ≠ 0 (btn_red_small 28, btn_yellow_3d 16) ⇒ Sliced, preserveAspect TẮT.
            //     Sliced + preserveAspect là hai thứ đánh nhau: preserveAspect ép cả rect về
            //     tỉ lệ ảnh gốc (256x96 ⇒ dẹt 2.67:1) rồi Sliced mới chia vành ⇒ nút không
            //     bao giờ ra vuông.
            //   border = 0 (check_badge_green 48x48) ⇒ Simple + preserveAspect, đĩa tròn đều.
            // Nhờ suy từ sprite, Sếp thay art khác border là code tự đúng, khỏi sửa dòng nào.
            bool chinNhat      = PlacementKitSpriteFactory.LaSprite9Slice(nenNut);
            img.sprite         = nenNut;
            img.type           = chinNhat ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = !chinNhat;
            img.color          = color;
        }

        // ── TRẠNG THÁI TẮT: ĐỤC HẲN, KHÔNG TRONG SUỐT ────────────────────────
        // Prefab serialize m_DisabledColor = (0.784,0.784,0.784, α 0.502). Nút ✓ bị tắt là
        // đĩa xanh × cái đó ⇒ (68,121,28) MÀ CHỈ ĐỤC 50 % ⇒ nhìn xuyên thấy cỏ qua nút.
        // Đó là lý do trong ảnh Sếp gửi nút ✓ đọc ra "hỏng" chứ không phải "chưa bấm được".
        // Ghi Button.colors MỘT LẦN lúc dựng. KHÔNG đụng `Button.interactable` —
        // PlacementManager gán nó mỗi frame, đó là chủ sở hữu duy nhất của cờ đó.
        var btnNay = t.GetComponent<Button>();
        if (btnNay != null)
        {
            ColorBlock cb    = btnNay.colors;
            cb.disabledColor = mauNutKhiTat;
            btnNay.colors    = cb;
        }

        if (glyph == null) return;

        Transform found = t.Find("Glyph");
        GameObject go = found != null
            ? found.gameObject
            : new GameObject("Glyph", typeof(RectTransform));
        go.layer = t.gameObject.layer;

        var grt = (RectTransform)go.transform;
        grt.SetParent(t, false);
        grt.anchorMin        = new Vector2(0.5f, 0.5f);
        grt.anchorMax        = new Vector2(0.5f, 0.5f);
        grt.pivot            = new Vector2(0.5f, 0.5f);
        grt.anchoredPosition = Vector2.zero;
        grt.sizeDelta        = new Vector2(coGlyph, coGlyph);

        Image gi = go.GetComponent<Image>();
        if (gi == null) gi = go.AddComponent<Image>();
        gi.sprite         = glyph;
        gi.color          = Color.white;
        gi.preserveAspect = true;
        gi.raycastTarget  = false;   // để click luôn rơi vào Button ở lớp cha
    }

    private void CacheGlyphPair(Transform row, string buttonName, int index)
    {
        Transform t = row.Find(buttonName);
        if (t == null) return;

        _barButtons[index] = t.GetComponent<Button>();

        Transform g = t.Find("Glyph");
        if (g != null) _barGlyphs[index] = g.GetComponent<Image>();
    }

    private TextMeshProUGUI MakeBarText(RectTransform parent, string name,
                                        string content, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, hangGiaCao);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text          = content;
        tmp.fontSize      = size;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.color         = mauChuGia;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.overflowMode  = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        // Viền đậm giống nhãn Township (cùng cách với LevelUpPopupTownshipTool.AddTextOutline)
        Material mat = tmp.fontMaterial;
        if (mat != null) mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        // ĐẢO so với bản cũ (chữ trắng viền nâu dày): chữ SẪM trên nền KEM cần viền
        // TRẮNG MẢNH, nếu không chữ biến mất trên card giấy.
        tmp.outlineColor = new Color(1f, 1f, 1f, 0.92f);
        tmp.outlineWidth = vienChuTrang;
        tmp.UpdateMeshPadding();

        return tmp;
    }

    private Image MakeBarIcon(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(coIconTien, coIconTien);

        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget  = false;
        return img;
    }

    /// <summary>Tìm con theo tên ở MỌI độ sâu (Transform.Find chỉ tìm con trực tiếp).</summary>
    private static Transform FindChildDeep(Transform parent, string childName)
    {
        if (parent == null) return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;

            Transform found = FindChildDeep(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // V7 — 4 CHEVRON ÔM 4 GÓC VÙNG Ô
    //
    // Township đặt 4 chevron ở 4 góc VÙNG Ô, không phải 4 góc SPRITE. Hai thứ này khác
    // nhau vì mái nhà nhô ra ngoài footprint (DEV-1 §5.1: "sprite vươn cao hơn vùng ô là
    // bình thường"). Người chơi cần thấy đúng vùng SẼ BỊ CHIẾM, nếu không họ tưởng công
    // trình ăn nhiều đất hơn thực tế và không dám xếp sát nhau.
    // ══════════════════════════════════════════════════════════════════════

    private void EnsureChevrons()
    {
        if (!useRectChevrons) return;
        if (_chevrons != null && _chevronRoot != null) return;

        Transform existing = transform.Find(ChevronRootName);
        if (existing != null)
        {
            _chevronRoot = existing;
        }
        else
        {
            var go = new GameObject(ChevronRootName);
            go.layer = gameObject.layer;
            _chevronRoot = go.transform;
            _chevronRoot.SetParent(transform, false);
        }

        _chevronRoot.localPosition = Vector3.zero;
        _chevronRoot.localRotation = Quaternion.identity;

        // CHUẨN HOÁ SCALE: root Ghost có scale 100 (quy ước "1 unit sprite = 1 ô" của dự án).
        // Chia ngược để BÊN TRONG _chevronRoot, 1 đơn vị = 1 WORLD unit. Nhờ vậy
        // `chevronWorldSize` đọc thẳng ra world unit và InverseTransformPoint(gócWorld) cho
        // ra đúng offset — không phải rải phép chia 100 khắp nơi.
        float sx = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        _chevronRoot.localScale = new Vector3(1f / sx, 1f / sy, 1f);

        _chevrons = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
            _chevrons[i] = CreateOrGetRenderer(_chevronRoot, $"Chevron_{i}",
                                               _chevronSprite, BaseOrder + 6);
    }

    private void UpdateChevrons()
    {
        if (_chevrons == null || _chevronRoot == null) return;

        PlacementManager pm = PlacementManager.Instance;
        RectInt rect = pm != null ? pm.CurrentRect : new RectInt(0, 0, 0, 0);

        // HỢP ĐỒNG API §4: width == 0 nghĩa là KHÔNG có Ghost nào đang hoạt động → ẩn hết.
        if (rect.width <= 0 || rect.height <= 0)
        {
            ToggleArray(_chevrons, false);
            return;
        }

        // Lấy 4 góc bằng HÀM CỦA DEV-1, KHÔNG tự nhân CELL: họ vừa đổi hệ neo sang mép dưới
        // vùng ô (V8), tự tính lại là mời lỗi "lệch nửa ô" quay về đúng chỗ vừa sửa xong.
        _chevronCorners[0] = PlacementManager.CellCornerToWorld(rect.xMin, rect.yMin); // dưới-trái
        _chevronCorners[1] = PlacementManager.CellCornerToWorld(rect.xMax, rect.yMin); // dưới-phải
        _chevronCorners[2] = PlacementManager.CellCornerToWorld(rect.xMax, rect.yMax); // trên-phải
        _chevronCorners[3] = PlacementManager.CellCornerToWorld(rect.xMin, rect.yMax); // trên-trái

        // Nhấp nháy scale 1.0 ↔ 1.08, chu kỳ ~1 s (thông số V7).
        float k = Mathf.LerpUnclamped(1f, chevronBlinkPeak,
                      FxEase.Sin01(Time.time / Mathf.Max(0.05f, chevronBlinkPeriod)));
        float size = chevronWorldSize * k;

        // Xanh khi đặt được, ĐỎ khi chồng lấn / ra ngoài biên. `_lastValid` do
        // PlacementManager.SetValid() cấp mỗi frame nên luôn khớp với nút ✓ bị xám.
        Color c = _lastValid ? validEdgeColor : invalidEdgeColor;

        for (int i = 0; i < 4; i++)
        {
            SpriteRenderer sr = _chevrons[i];
            if (sr == null) continue;

            sr.enabled = true;
            if (sr.sprite == null) sr.sprite = _chevronSprite;

            Vector3 local = _chevronRoot.InverseTransformPoint(_chevronCorners[i]);
            local.z = 0f;
            sr.transform.localPosition = local;

            // Sprite chevron có PIVOT ĐÚNG TẠI GÓC và hai cánh vươn theo +X/+Y, nên xoay
            // đúng i·90° là ôm sang góc kế tiếp — khỏi phải tính offset riêng cho từng góc.
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
            sr.transform.localScale    = new Vector3(size, size, 1f);
            sr.color = c;
        }
    }

    private SpriteRenderer CreateOrGetRenderer(Transform parent, string name, Sprite sprite, int order)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.sortingLayerName = SortingLayerName;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Simple;
        return sr;
    }

    private static string ResolveSortingLayerName(string preferred, string fallback)
    {
        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == preferred)
                return preferred;

        foreach (SortingLayer layer in SortingLayer.layers)
            if (layer.name == fallback)
                return fallback;

        return "Default";
    }

    private static void SetMarker(SpriteRenderer sr, Vector3 position, Vector3 scale, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = scale;
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void SetCorner(SpriteRenderer sr, Vector3 position, float size, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = new Vector3(size, size * 0.42f, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static void SetCornerMarker(SpriteRenderer sr, Vector3 position, float width, float height, float rotation)
    {
        if (sr == null) return;
        sr.transform.localPosition = position;
        sr.transform.localScale = new Vector3(width, height, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    // Bật/tắt các cạnh viền (để chế độ "chỉ 4 góc" giống mẫu).
    private void SetEdgesVisible(bool on)
    {
        ToggleArray(_edges, on);
        ToggleArray(_edgeShadows, on);
        ToggleArray(_edgeHighlights, on);
    }

    private static void ToggleArray(SpriteRenderer[] arr, bool on)
    {
        if (arr == null) return;
        foreach (var r in arr) if (r != null) r.enabled = on;
    }

    private static Sprite FindAnyFootprintSprite()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null && sr.name == "Grid_Footprint" && sr.sprite != null)
                return sr.sprite;
        }
        return null;
    }

    private void EnsureRuntimeSprites()
    {
        if (tileSprite == null)
            tileSprite = FindAnyFootprintSprite();

        if (_diamondSprite == null)
        {
            _diamondSprite = CreatePolygonSprite(
                "Placement_Diamond",
                64,
                new[]
                {
                    new Vector2(0.5f, 1f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 0.5f)
                });
        }

        if (_markerSprite == null)
        {
            _markerSprite = CreatePolygonSprite(
                "Placement_Marker",
                96,
                new[]
                {
                    new Vector2(0.06f, 0.18f),
                    new Vector2(0.94f, 0.18f),
                    new Vector2(0.78f, 0.82f),
                    new Vector2(0.22f, 0.82f)
                });
        }

        if (_arrowSprite == null)
        {
            _arrowSprite = CreatePolygonSprite(
                "Placement_Lift_Arrow",
                96,
                new[]
                {
                    new Vector2(0.50f, 0.98f),
                    new Vector2(0.95f, 0.54f),
                    new Vector2(0.72f, 0.54f),
                    new Vector2(0.72f, 0.08f),
                    new Vector2(0.28f, 0.08f),
                    new Vector2(0.28f, 0.54f),
                    new Vector2(0.05f, 0.54f)
                });
        }

        if (_circleSprite == null)
            _circleSprite = CreateCircleSprite("Placement_Arrow_Dot", 64);

        if (_bracketSprite == null)
        {
            _bracketSprite = CreatePolygonSprite(
                "Placement_Corner_Wedge",
                64,
                new[]
                {
                    new Vector2(0.04f, 0.20f),
                    new Vector2(0.78f, 0.20f),
                    new Vector2(0.98f, 0.50f),
                    new Vector2(0.78f, 0.80f),
                    new Vector2(0.04f, 0.80f),
                    new Vector2(0.24f, 0.50f)
                });
        }

        if (_chevronSprite == null)
            _chevronSprite = CreateCornerChevronSprite("Placement_Rect_Chevron", 64);
    }

    /// <summary>
    /// CHEVRON GÓC hình chữ L, PIVOT ĐÚNG TẠI GÓC (0,0), hai cánh vươn theo +X và +Y.
    ///
    /// VÌ SAO PIVOT Ở GÓC: đặt xong chỉ cần `localPosition` = đúng góc vùng ô rồi xoay
    /// 0/90/180/270° là ra cả 4 góc. Pivot ở giữa thì mỗi góc phải cộng thêm một offset
    /// riêng theo chiều xoay — bốn công thức song song là bốn chỗ để sai (đúng loại lỗi
    /// DEV-1 vừa dọn ở §5.1).
    ///
    /// `pixelsPerUnit = size` ⇒ 1 sprite = 1 unit ⇒ `localScale = size` chính là CẠNH
    /// chevron tính bằng world unit. Đọc số là biết ngay nó to bằng bao nhiêu phần của ô.
    ///
    /// Có LẤY MẪU BỘI 3×3 để khử răng cưa: chevron nằm ngay dưới con trỏ suốt lượt đặt nên
    /// viền nhảy bậc thang là thứ đầu tiên mắt bắt được (CreatePolygonSprite ở trên cắt
    /// cứng, dùng cho mảnh nhỏ thì không sao, dùng cho chevron thì thấy rõ).
    /// </summary>
    private static Sprite CreateCornerChevronSprite(string name, int size)
    {
        const float arm   = 0.92f;   // chiều dài mỗi cánh (tỉ lệ theo ô sprite)
        const float thick = 0.26f;   // độ dày cánh
        const int   ss    = 3;       // số mẫu mỗi chiều

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < ss; sy++)
                {
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float u = (x + (sx + 0.5f) / ss) / size;
                        float v = (y + (sy + 0.5f) / ss) / size;
                        if ((u <= thick && v <= arm) || (v <= thick && u <= arm)) hit++;
                    }
                }

                float a = hit / (float)(ss * ss);
                pixels[y * size + x] = a <= 0.001f ? clear : new Color(1f, 1f, 1f, a);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.zero, size);
        sprite.name = name;
        return sprite;
    }

    private static Sprite CreatePolygonSprite(string name, int size, Vector2[] points)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                if (PointInPolygon(p, points))
                    pixels[y * size + x] = fill;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        return sprite;
    }

    private static Sprite CreateCircleSprite(string name, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = d <= radius ? fill : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = name;
        return sprite;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            if ((pi.y > point.y) != (pj.y > point.y) &&
                point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }
}
