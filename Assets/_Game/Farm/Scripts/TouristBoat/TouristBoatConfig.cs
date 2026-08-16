using UnityEngine;

/// <summary>
/// Toàn bộ tuning knob của hệ Bến Tàu Du Lịch (GDD §7) — KHÔNG hardcode
/// gameplay value ở bất kỳ đâu khác, tất cả đọc từ asset này.
///
/// Tạo asset: chuột phải trong Project → Create → Farm Game → Tourist Boat Config.
/// Tool sinh scene của hệ boat sẽ tự tạo 1 asset mặc định nếu chưa có.
///
/// Lưu ý đơn vị: các knob thời gian đặt theo PHÚT (đúng ngôn ngữ GDD:
/// "đậu 40 phút, núp 15 phút, so le 12 phút"); code runtime dùng các property
/// *Seconds bên dưới — đã đổi sẵn sang giây và kẹp không âm.
/// </summary>
[CreateAssetMenu(fileName = "TouristBoatConfig", menuName = "Farm Game/Tourist Boat Config")]
public class TouristBoatConfig : ScriptableObject
{
    // ─── Mở khóa (GDD §3.1) ─────────────────────────────────────────────

    [Header("Mở khóa")]
    [Tooltip("Level mở hội thoại intro + bến 1 miễn phí")]
    public int unlockLevel = 10;

    [Tooltip("Level yêu cầu của bến 2")]
    public int dock2Level = 12;

    [Tooltip("Giá vàng mở bến 2")]
    public int dock2GoldCost = 2000;

    [Tooltip("Level yêu cầu của bến 3")]
    public int dock3Level = 14;

    [Tooltip("Giá gem mở bến 3")]
    public int dock3GemCost = 25;

    // ─── Chu kỳ tàu (GDD §4) ────────────────────────────────────────────

    [Header("Chu kỳ tàu (phút)")]
    [Tooltip("Số phút tàu đậu ở bến (du khách tham quan)")]
    public float dockMinutes = 40f;

    [Tooltip("Số phút tàu núp ở điểm mù giữa 2 chuyến")]
    public float hideMinutes = 15f;

    [Tooltip("Khoảng cách so le tối thiểu giữa 2 lần cập bến của 2 bến bất kỳ")]
    public float staggerMinutes = 12f;

    // ─── Di chuyển & hiệu ứng ───────────────────────────────────────────

    [Header("Di chuyển & hiệu ứng")]
    [Tooltip("Tốc độ tàu (unit/giây) — travelTime = độ dài path / tốc độ")]
    public float boatSpeed = 300f;

    [Tooltip("Giây chạy 1 chiều dùng TẠM khi bến chưa có path hợp lệ (thiếu waypoint) — chỉ là lưới an toàn, không phải giá trị thiết kế")]
    public float fallbackTravelSeconds = 20f;

    [Tooltip("Biên độ dập dềnh của sprite tàu (unit world) — như FerryController")]
    public float bobAmplitude = 8f;

    [Tooltip("Tần số dập dềnh (chu kỳ/giây)")]
    public float bobFrequency = 0.8f;

    // ─── Hội thoại intro (GDD §3.1 — 4 câu trên guide board) ────────────

    [Header("Hội thoại intro (guide board, skip từng câu bằng tap)")]
    // NGUỒN DUY NHẤT của dialogue mặc định (chốt với lead + Dev B): tool sinh scene
    // KHÔNG bơm dialogue nữa — sửa lời thoại thì sửa Ở ĐÂY hoặc trong asset.
    // KHÔNG dùng emoji trong lời thoại: font TMP của dự án có thể thiếu glyph (QA cảnh báo).
    [TextArea(1, 3)]
    public string[] introDialogue = new string[4]
    {
        "Chúc mừng! Nông trại của bạn đã nổi tiếng khắp vùng rồi đó!",
        "Nghe nói du khách phương xa rất muốn ghé thăm... Bến tàu cũ ngoài bãi biển có thể sửa lại được đấy!",
        "Tàu du lịch sẽ cập bến thường xuyên — du khách sẽ dạo chơi, ngắm nông trại và thưởng thức đặc sản của bạn!",
        "Nhìn kìa — chuyến tàu đầu tiên đang tới!",
    };

    // ─── Debug ──────────────────────────────────────────────────────────

    [Header("Debug")]
    [Tooltip("Hệ số tua nhanh thời gian để test (60 = 1 giây thực bằng 1 phút game). CHỈ có tác dụng trong Editor hoặc Development Build — bản release luôn chạy 1.")]
    public float debugTimeScale = 1f;

    // ─── Property đổi đơn vị (dùng trong code runtime) ──────────────────

    /// <summary>Giây tàu đậu bến (đã kẹp không âm).</summary>
    public float DockSeconds => Mathf.Max(0f, dockMinutes) * 60f;

    /// <summary>Giây tàu núp ở điểm mù (đã kẹp không âm).</summary>
    public float HideSeconds => Mathf.Max(0f, hideMinutes) * 60f;

    /// <summary>Giây so le tối thiểu giữa 2 lần cập bến (đã kẹp không âm).</summary>
    public float StaggerSeconds => Mathf.Max(0f, staggerMinutes) * 60f;

    /// <summary>
    /// Điều kiện mở của từng bến, đóng gói cho BoatScheduleCore.EvaluateUnlock.
    /// Bến 1 (index 0): miễn phí, chỉ cần unlockLevel — mở qua hội thoại intro.
    /// dockIndex ngoài [0..2] trả yêu cầu "không thể đạt" (level int.MaxValue)
    /// để mọi đường kiểm tra đều từ chối thay vì nổ exception.
    /// </summary>
    public DockUnlockRequirement GetDockRequirement(int dockIndex)
    {
        var req = new DockUnlockRequirement();
        switch (dockIndex)
        {
            case 0:
                req.RequiredLevel = unlockLevel;
                req.GoldCost      = 0;
                req.GemCost       = 0;
                break;
            case 1:
                req.RequiredLevel = dock2Level;
                req.GoldCost      = Mathf.Max(0, dock2GoldCost);
                req.GemCost       = 0;
                break;
            case 2:
                req.RequiredLevel = dock3Level;
                req.GoldCost      = 0;
                req.GemCost       = Mathf.Max(0, dock3GemCost);
                break;
            default:
                req.RequiredLevel = int.MaxValue;
                req.GoldCost      = 0;
                req.GemCost       = 0;
                break;
        }
        return req;
    }

    private void OnValidate()
    {
        // Kẹp các giá trị vô nghĩa ngay lúc chỉnh trong Inspector —
        // fail sớm ở editor còn hơn NaN/chia 0 lúc runtime.
        dockMinutes           = Mathf.Max(0f, dockMinutes);
        hideMinutes           = Mathf.Max(0f, hideMinutes);
        staggerMinutes        = Mathf.Max(0f, staggerMinutes);
        boatSpeed             = Mathf.Max(1f, boatSpeed);
        fallbackTravelSeconds = Mathf.Max(1f, fallbackTravelSeconds);
        bobFrequency          = Mathf.Max(0f, bobFrequency);
        debugTimeScale        = Mathf.Max(0.01f, debugTimeScale);
        unlockLevel           = Mathf.Max(1, unlockLevel);
        dock2Level            = Mathf.Max(1, dock2Level);
        dock3Level            = Mathf.Max(1, dock3Level);
    }
}
