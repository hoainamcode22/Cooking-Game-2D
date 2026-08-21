using UnityEngine;

/// <summary>
/// CẤU HÌNH MÁY XAY THỨC ĂN — MỌI con số của popup nằm ở đây, không có số nào hardcode
/// trong logic. Tạo asset: chuột phải → Create → Farm → Mill → Config.
///
/// ══ NGUỒN SỐ ANIMATION ══
/// Toàn bộ nhóm [Animation] được TRÍCH TRỰC TIẾP từ bản thiết kế gốc
/// `Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html` — chính là file HTML/CSS mà
/// video demo được render ra. Giá trị mặc định dưới đây ĐÃ ĐÚNG; sửa chúng là lệch video.
/// Bảng đối chiếu đầy đủ ghi trong tooltip từng field.
/// </summary>
[CreateAssetMenu(fileName = "MillConfig", menuName = "Farm/Mill/Config")]
public class MillConfig : ScriptableObject
{
    [Header("Chung")]
    [Tooltip("Chữ trên ruy băng đầu popup. Video: MÁY XAY THỨC ĂN")]
    public string title = "MÁY XAY THỨC ĂN";

    [Tooltip("Danh sách công thức, THEO THỨ TỰ hiện trong danh sách bên trái. " +
             "Công thức chưa đủ cấp vẫn phải để trong danh sách — nó hiện dạng card khoá.")]
    public MillRecipeData[] recipes;

    [Header("Slot xay")]
    [Tooltip("Tổng số slot của máy. Video: 5.")]
    public int slotCount = 5;

    [Tooltip("Số slot mở sẵn khi người chơi lần đầu bấm vào máy. Video: 3.")]
    public int slotsUnlockedAtStart = 3;

    [Tooltip("Giá kim cương để mở thêm MỘT slot. Video: 15.")]
    public int gemCostUnlockSlot = 15;

    [Tooltip("Slot CUỐI CÙNG không mua được bằng kim cương — phải đạt cấp này. " +
             "Video: slot #5 ghi 'Chưa đủ cấp / Cấp 18'.")]
    public int levelRequiredLastSlot = 18;

    // ─────────────────────────────────────────────────────────────────────────
    [Header("Animation — số lấy từ full_mill_ui.html")]

    [Tooltip("Bánh răng LỚN, độ/giây.\n" +
             "HTML: <animateTransform type=\"rotate\" from=\"0 0 0\" to=\"360 0 0\" dur=\"4s\">\n" +
             "⇒ 360° / 4s = 90 °/s, chiều DƯƠNG của SVG = thuận kim đồng hồ trên màn hình.")]
    public float gearLargeDegPerSec = 90f;

    [Tooltip("Bánh răng NHỎ, độ/giây.\n" +
             "HTML: to=\"-360 0 0\" dur=\"2.5s\"\n" +
             "⇒ 360° / 2.5s = 144 °/s, dấu ÂM = NGƯỢC kim đồng hồ (hai bánh ăn khớp nhau).")]
    public float gearSmallDegPerSec = 144f;

    [Tooltip("Băng tải trôi sang TRÁI, pixel/giây.\n" +
             "HTML: @keyframes scrollBelt { to { transform: translateX(-42px) } } + animation 1s linear\n" +
             "⇒ 42 px mỗi giây.")]
    public float beltScrollPxPerSec = 42f;

    [Tooltip("Chu kỳ hoa văn sọc của băng tải, pixel.\n" +
             "HTML: repeating-linear-gradient(-45deg, transparent 0→15px, #2A1D15 15px→30px)\n" +
             "⇒ hoa văn lặp lại mỗi 30px. Dùng để tính uvRect khi texture sọc rộng khác 30px.")]
    public float beltStripePeriodPx = 30f;

    [Tooltip("Một chu kỳ bó cỏ chạy trên băng, giây.\n" +
             "HTML: .moving-item { animation: moveItem 3s linear infinite }")]
    public float itemCycleSeconds = 3f;

    [Tooltip("Khoảng chạy ngang của bó cỏ tại mốc 80% chu kỳ, pixel.\n" +
             "HTML: @keyframes moveItem { 80% { transform: translateX(230px) } }")]
    public float itemTravelPx = 230f;

    [Tooltip("Độ lệch pha giữa hai bó cỏ, giây.\n" +
             "HTML: .mi-1 { animation-delay: 0s }  .mi-2 { animation-delay: 1.5s }\n" +
             "⇒ item thứ n lệch n × 1.5s. ConveyorItem tự cộng dồn theo chỉ số.")]
    public float itemStaggerSeconds = 1.5f;

    [Header("Tăng tốc bằng kim cương")]
    [Tooltip("Số kim cương cho MỖI PHÚT còn lại khi bấm nút xanh trên slot đang xay. " +
             "Video slot #3 còn 1p56 → nút ghi 'x6'; con số đó do designer chốt riêng, " +
             "công thức ở đây là: ceil(giây còn lại / 60) × giá trị này.")]
    public int gemPerMinuteSpeedUp = 1;

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Số kim cương cần để hoàn thành NGAY một slot còn <paramref name="giayConLai"/> giây.
    /// Luôn ≥ 1 để không bao giờ có nút "0 kim cương".
    /// </summary>
    public int TinhGiaTangToc(float giayConLai)
    {
        int phut = Mathf.CeilToInt(Mathf.Max(0f, giayConLai) / 60f);
        return Mathf.Max(1, phut * Mathf.Max(1, gemPerMinuteSpeedUp));
    }

    /// <summary>
    /// Kiểm tra cấu hình, gọi lúc popup Open() để lỗi setup lộ ra ngay chứ không âm thầm.
    /// Trả về false nếu config không dùng được.
    /// </summary>
    public bool KiemTraHopLe(out string loi)
    {
        if (slotCount <= 0)
        {
            loi = "slotCount phải > 0";
            return false;
        }

        if (slotsUnlockedAtStart < 0 || slotsUnlockedAtStart > slotCount)
        {
            loi = "slotsUnlockedAtStart (" + slotsUnlockedAtStart + ") phải nằm trong [0, slotCount=" + slotCount + "]";
            return false;
        }

        if (recipes == null || recipes.Length == 0)
        {
            loi = "chưa gán công thức nào vào mảng recipes";
            return false;
        }

        loi = null;
        return true;
    }
}
