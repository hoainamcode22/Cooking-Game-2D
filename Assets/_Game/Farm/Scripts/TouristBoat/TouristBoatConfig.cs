using UnityEngine;

/// <summary>
/// Toàn bộ tuning knob của hệ Bến Tàu Du Lịch (GDD V2 §7) — KHÔNG hardcode
/// gameplay value ở bất kỳ đâu khác, tất cả đọc từ asset này.
///
/// Tạo asset: chuột phải trong Project → Create → Farm Game → Tourist Boat Config.
/// Tool sinh scene của hệ boat sẽ tự tạo 1 asset mặc định nếu chưa có.
///
/// V2 (BOAT-002): thêm nhóm "Lịch tàu V2" (gap 5p/10p + so le 3p) và nhóm
/// "Khách du lịch" — Dev B đọc các field visitor* qua
/// BoatDockManager.Instance.Config, KHÔNG tự tạo config riêng.
///
/// Lưu ý đơn vị: các knob thời gian lớn đặt theo PHÚT (đúng ngôn ngữ GDD);
/// code runtime dùng các property *Seconds bên dưới — đã đổi sẵn sang giây
/// và kẹp không âm. Knob hiệu ứng ngắn (disembark, bubble...) đặt theo GIÂY.
/// </summary>
[CreateAssetMenu(fileName = "TouristBoatConfig", menuName = "Farm Game/Tourist Boat Config")]
public class TouristBoatConfig : ScriptableObject
{
    // ─── Mở khóa (giữ nguyên luật V1) ───────────────────────────────────

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

    // ─── Lịch tàu V2 — event-driven (GDD V2 §3.2) ───────────────────────

    [Header("Lịch tàu V2 (phút) — tàu đậu tới khi khách xong, KHÔNG còn đậu cố định")]
    [Tooltip("Chỉ 1 bến đang mở: tàu rời bến xong bao nhiêu phút thì cập bến lại")]
    public float gapOneDockMinutes = 5f;

    [Tooltip("ĐÚNG 2 bến mở: gap của mỗi bến. 2 bến × gap 5 phút thì cứ 2,5 phút lại có tàu vào bờ — quá dồn; 7 phút giữ nhịp ~3,5 phút")]
    public float gapTwoDockMinutes = 7f;

    [Tooltip("Đủ 3 bến mở: gap của MỖI bến (arrival kế = lúc rời bến + gap này) — mốc 10 phút Sếp chốt cho giai đoạn mở hết slot")]
    public float gapMultiDockMinutes = 10f;

    [Tooltip("Hai lần cập bến của 2 bến BẤT KỲ phải cách nhau ít nhất bấy nhiêu phút — vi phạm thì dời arrival muộn hơn")]
    public float minStaggerMinutes = 3f;

    // [QA M-7] PHẢI LỚN HƠN patienceMinutes, không được bằng: lưới an toàn đếm từ lúc
    // tàu CHẠM BẾN, còn đồng hồ kiên nhẫn của khách chỉ bắt đầu khi BUBBLE MỞ — tức là
    // sau khi khách xuống tàu (disembarkInterval) + đi bộ tới hàng + tới lượt đứng đầu.
    // Để 2 số bằng nhau (30/30) thì tàu luôn bị ép rời TRƯỚC khi khách kịp hết kiên nhẫn
    // ⇒ nhánh "khách giận rồi tự về tàu" của Dev B thành CODE CHẾT, không bao giờ chạy.
    // 35 vs 30 chừa ~5 phút cho quãng xuống tàu + đi bộ + xếp hàng.
    [Tooltip("Tàu đậu tối đa bao lâu rồi tự rời bến dù khách chưa xong — lưới an toàn chống kẹt.\n" +
             "PHẢI LỚN HƠN patienceMinutes (30): lưới đếm từ lúc tàu CHẠM BẾN, còn kiên nhẫn khách " +
             "chỉ bắt đầu khi bubble mở (sau khi khách xuống tàu và đi bộ tới hàng). Để 2 số bằng nhau " +
             "thì khách không bao giờ kịp hết kiên nhẫn — đường 'khách giận tự về tàu' thành code chết.\n" +
             "Đặt 0 = TẮT lưới (tàu đậu vô hạn, chỉ dùng khi debug).")]
    public float maxDockMinutes = 35f;

    // ─── Khách du lịch (GDD V2 §3.3/§3.4 — Dev B đọc qua BoatDockManager.Instance.Config) ──

    [Header("Khách du lịch (Dev B đọc qua BoatDockManager.Instance.Config)")]
    [Tooltip("Số khách ít nhất mỗi chuyến (random visitorsMin..visitorsMax)")]
    public int visitorsMin = 3;

    [Tooltip("Số khách nhiều nhất mỗi chuyến")]
    public int visitorsMax = 6;

    [Tooltip("Khách chờ tối đa bấy nhiêu phút (UTC tuyệt đối từ lúc bubble mở, offline vẫn chạy) — hết giờ buồn bã về tàu, không thưởng")]
    public float patienceMinutes = 30f;

    // [Lead chốt 2026-08-29] Công thức thưởng V2.1 — xem doc của TouristRewardCalculator.
    // Đường CHÍNH: vàng = round(sellPrice × diffMult × rarityBonus × touristGoldMultiplier).
    // rewardIngredientMultiplier bên dưới CHỈ còn dùng cho đường FALLBACK (món chưa điền sellPrice).
    [Tooltip("[Chỉ dùng cho đường FALLBACK] Món chưa điền sellPrice: vàng = tổng giá nguyên liệu chính × hệ số này")]
    public int rewardIngredientMultiplier = 2;

    [Tooltip("Núm chỉnh chung độ hào phóng của khách du lịch. 1.0 = đúng thiết kế; thấy lạm phát vàng thì hạ xuống 0.9/0.8, thấy khách trả bèo thì nâng 1.1. Nhân vào TẤT CẢ vàng khách trả")]
    public float touristGoldMultiplier = 1.0f;

    // [QA M-9] CHỐNG LẠM PHÁT EXP — đừng nâng lên trên 1.0 nếu chưa đo lại đường cong level.
    // Nấu xong trong minigame ĐÃ cộng rewardExp × hệ số điểm (CookingChallengeManager),
    // phục vụ khách cộng THÊM một lần nữa ⇒ mỗi món cho ~2× EXP thiết kế. Trần level 30,
    // tổng L10→L30 chỉ 5.619 EXP mà một chuyến khách cho 128-306 EXP ⇒ ở L10 lên
    // 0,9-2,2 level MỘT CHUYẾN, hết nội dung game sau 1,2-3,7 giờ.
    // 0.4 = nấu ăn (phần chơi chính) giữ trọn EXP của nó, phục vụ khách là thưởng THÊM
    // ~40%: đủ khích lệ mà tổng chỉ còn ~1,4× thiết kế thay vì 2,25×.
    [Tooltip("Hệ số EXP khách du lịch trả — chống lạm phát cấp độ (QA M-9).\n" +
             "Nấu xong trong bếp ĐÃ cộng EXP của món; phục vụ khách cộng thêm lần nữa nên phải hãm lại. " +
             "0.4 = khách trả 40% EXP món (tổng ~1,4× thiết kế). ĐỪNG đặt > 1.0: người chơi sẽ lên hết " +
             "cấp trần chỉ trong 1-2 giờ. Muốn khách cho nhiều EXP hơn thì nâng từng bước 0.1 rồi đo lại.")]
    public float touristExpMultiplier = 0.4f;

    [Tooltip("Hệ số vàng cho món Easy (nhân với sellPrice). 1.00 = trả đúng giá bán chợ")]
    public float diffMultEasy = 1.00f;

    [Tooltip("Hệ số vàng cho món Normal — cao hơn Easy để nấu món khó có lời hơn")]
    public float diffMultNormal = 1.15f;

    [Tooltip("Hệ số vàng cho món Hard")]
    public float diffMultHard = 1.35f;

    [Tooltip("Trần của thưởng thêm theo nguyên liệu quý (Rare +5%, Epic +12% mỗi loại). 1.5 = tối đa +50%, tránh món 5 nguyên liệu Epic trả gấp đôi")]
    public float rarityBonusCap = 1.5f;

    [Tooltip("Giây giãn cách giữa 2 khách lần lượt xuống tàu (gangplank)")]
    public float disembarkInterval = 0.8f;

    [Tooltip("Tốc độ đi bộ của khách (unit world/giây — map dùng toạ độ lớn, ~740 unit giữa 2 bến, chỉnh theo scale scene)")]
    public float visitorWalkSpeed = 150f;

    [Tooltip("Khoảng cách giữa 2 khách đứng xếp hàng trước nhà hàng (unit world)")]
    public float queueSpacing = 120f;

    [Tooltip("Giây scale-in của bubble món ăn khi khách đầu hàng mở bubble")]
    public float bubbleScaleInTime = 0.25f;

    [Tooltip("Giây mặt cười bay từ khách lên HUD (nhỏ → to dần, fade)")]
    public float smileyFlyTime = 1.2f;

    // ─── Chu kỳ tàu V1 — LEGACY (GDD V1 §4) ─────────────────────────────

    [Header("Chu kỳ tàu V1 — LEGACY, V2 không dùng")]
    // [V2 OBSOLETE] dockMinutes: V1 tàu đậu đúng bấy nhiêu phút rồi tự rời bến.
    // V2 event-driven: tàu đậu TỚI KHI khách được phục vụ xong (Dev B gọi
    // ReportVisitorsAllAboard) — field này KHÔNG còn được runtime đọc.
    // GIỮ LẠI để asset serialize cũ không mất data + diagnostic tool cũ còn hiển thị.
    [Tooltip("[V1 — KHÔNG dùng ở V2] Số phút tàu đậu ở bến theo mô hình chu kỳ cũ")]
    public float dockMinutes = 40f;

    // [V2 OBSOLETE] hideMinutes: V1 núp ở điểm mù theo chu kỳ. V2 thời gian chờ
    // suy từ gapOneDock/gapMultiDock — giữ field cho serialize + diagnostic tool cũ.
    [Tooltip("[V1 — KHÔNG dùng ở V2] Số phút tàu núp ở điểm mù giữa 2 chuyến")]
    public float hideMinutes = 15f;

    // [V2 OBSOLETE] staggerMinutes (12p): thay bằng minStaggerMinutes (3p) ở trên.
    [Tooltip("[V1 — KHÔNG dùng ở V2] Khoảng so le cũ — V2 dùng minStaggerMinutes")]
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

    // ─── Hội thoại intro (giữ nguyên V1 — 4 câu trên guide board) ───────

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

    // ─── Cỡ tàu trong world ─────────────────────────────────────────────
    [Header("Cỡ tàu — unit world")]
    [Tooltip("Chiều rộng sprite tàu. 3 bến cách nhau ~740 unit nên tàu ~300 là vừa 1 ô đậu. Đặt 0 để KHÔNG can thiệp (giữ nguyên cỡ bạn tự chỉnh).")]
    public float boatVisualWidth = 300f;

    [Tooltip("Chiều cao sprite tàu. Đặt 0 để suy theo tỉ lệ gốc của ảnh.")]
    public float boatVisualHeight = 0f;

    // ─── Cỡ bảng khóa (LockUI) trong world ──────────────────────────────
    // Map của game dùng hệ toạ độ RẤT lớn (3 bến cách nhau ~740-840 unit) nên
    // bảng khóa phải tính theo unit world, không phải pixel UI. Số mặc định dưới
    // đây canh theo khoảng cách bến thật: bảng 620 rộng để 3 bảng không chạm nhau.
    [Header("Cỡ bảng khóa (LockUI) — unit world")]
    [Tooltip("Chiều rộng bảng khóa. Nên nhỏ hơn khoảng cách giữa 2 bến (~740) để 2 bảng không chạm nhau.")]
    public float lockPanelWidth = 520f;

    [Tooltip("Chiều cao bảng khóa.")]
    public float lockPanelHeight = 250f;

    [Tooltip("Đường kính icon ổ khóa.")]
    public float lockIconSize = 100f;

    [Tooltip("Cỡ chữ teaser ('Mở ở Lv12 · 2.000 vàng'). Đây là chiều cao chữ tính bằng unit world — 96 đọc rõ ở mức zoom thường.")]
    public float lockTeaserFontSize = 80f;

    // ─── Debug ──────────────────────────────────────────────────────────

    [Header("Debug")]
    [Tooltip("Hệ số tua nhanh thời gian để test (60 = 1 giây thực bằng 1 phút game). CHỈ có tác dụng trong Editor hoặc Development Build — bản release luôn chạy 1.")]
    public float debugTimeScale = 1f;

    // ─── Property đổi đơn vị (dùng trong code runtime) ──────────────────

    /// <summary>V2: giây gap khi chỉ 1 bến mở (đã kẹp không âm).</summary>
    public float GapOneDockSeconds => Mathf.Max(0f, gapOneDockMinutes) * 60f;

    /// <summary>V2: giây gap khi ĐÚNG 2 bến mở (đã kẹp không âm).</summary>
    public float GapTwoDockSeconds => Mathf.Max(0f, gapTwoDockMinutes) * 60f;

    /// <summary>V2: giây gap khi đủ 3 bến mở (đã kẹp không âm).</summary>
    public float GapMultiDockSeconds => Mathf.Max(0f, gapMultiDockMinutes) * 60f;

    /// <summary>V2: giây so le tối thiểu giữa 2 arrival bất kỳ (đã kẹp không âm).</summary>
    public float MinStaggerSeconds => Mathf.Max(0f, minStaggerMinutes) * 60f;

    /// <summary>
    /// V2: giây đậu TỐI ĐA trước khi lưới an toàn ép tàu rời bến (0 = tắt lưới).
    /// Đây KHÔNG phải mô hình đậu cố định của V1 — bình thường tàu rời bến sớm hơn,
    /// ngay khi Dev B báo khách cuối đã lên tàu.
    /// </summary>
    public float MaxDockSeconds => Mathf.Max(0f, maxDockMinutes) * 60f;

    /// <summary>V2: giây kiên nhẫn của khách (Dev B dùng, đã kẹp không âm).</summary>
    public float PatienceSeconds => Mathf.Max(0f, patienceMinutes) * 60f;

    /// <summary>[V1 LEGACY] Giây tàu đậu bến theo chu kỳ cũ — V2 không dùng, giữ cho diagnostic tool.</summary>
    public float DockSeconds => Mathf.Max(0f, dockMinutes) * 60f;

    /// <summary>[V1 LEGACY] Giây tàu núp ở điểm mù theo chu kỳ cũ — V2 không dùng, giữ cho diagnostic tool.</summary>
    public float HideSeconds => Mathf.Max(0f, hideMinutes) * 60f;

    /// <summary>[V1 LEGACY] Giây so le cũ — V2 dùng MinStaggerSeconds thay thế.</summary>
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

        // V2 — lịch tàu: gap tối thiểu 0.5 phút để không spam tàu liên tục,
        // stagger không âm (0 = tắt luật so le, chỉ nên dùng khi debug).
        gapOneDockMinutes   = Mathf.Max(0.5f, gapOneDockMinutes);
        gapTwoDockMinutes   = Mathf.Max(0.5f, gapTwoDockMinutes);
        gapMultiDockMinutes = Mathf.Max(0.5f, gapMultiDockMinutes);
        minStaggerMinutes   = Mathf.Max(0f, minStaggerMinutes);
        maxDockMinutes      = Mathf.Max(0f, maxDockMinutes); // 0 = tắt lưới an toàn

        // V2 — khách du lịch: min ≥ 1, max ≥ min; các knob hiệu ứng có sàn nhỏ
        // để tránh chia 0 / vòng lặp 0 giây trong tween của Dev B.
        visitorsMin                = Mathf.Max(1, visitorsMin);
        visitorsMax                = Mathf.Max(visitorsMin, visitorsMax);
        patienceMinutes            = Mathf.Max(1f, patienceMinutes);
        rewardIngredientMultiplier = Mathf.Max(0, rewardIngredientMultiplier);

        // Thưởng V2.1: hệ số phải > 0 (0 làm khách trả 1 vàng — sàn của calculator),
        // cap không được nhỏ hơn 1 (nhỏ hơn 1 thành hình phạt cho nguyên liệu quý).
        touristGoldMultiplier      = Mathf.Max(0.01f, touristGoldMultiplier);
        touristExpMultiplier       = Mathf.Max(0.01f, touristExpMultiplier); // [QA M-9] 0 sẽ làm EXP về sàn 1
        diffMultEasy               = Mathf.Max(0.01f, diffMultEasy);
        diffMultNormal             = Mathf.Max(0.01f, diffMultNormal);
        diffMultHard               = Mathf.Max(0.01f, diffMultHard);
        rarityBonusCap             = Mathf.Max(1f,    rarityBonusCap);
        disembarkInterval          = Mathf.Max(0.05f, disembarkInterval);
        visitorWalkSpeed           = Mathf.Max(1f, visitorWalkSpeed);
        queueSpacing               = Mathf.Max(1f, queueSpacing);
        bubbleScaleInTime          = Mathf.Max(0.01f, bubbleScaleInTime);
        smileyFlyTime              = Mathf.Max(0.01f, smileyFlyTime);
    }
}
