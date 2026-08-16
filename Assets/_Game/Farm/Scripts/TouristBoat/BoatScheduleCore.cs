using System;

// ═══════════════════════════════════════════════════════════════════════════
//  BoatScheduleCore — LÕI THỜI GIAN THUẦN C# của hệ thống Bến Tàu Du Lịch.
//
//  TUYỆT ĐỐI KHÔNG using UnityEngine trong file này: tester biên dịch và chạy
//  unit test bằng .NET thuần, không cần mở Unity. Mọi thứ dính tới Unity
//  (Transform, PlayerPrefs, Debug.Log...) nằm ở BoatDockManager /
//  TouristBoatController — hai lớp đó chỉ là wrapper mỏng quanh lõi này.
//
//  Ý tưởng trung tâm (GDD §4): mỗi bến có 1 mốc neo `anchorUtc` (ticks UTC).
//  Trạng thái tàu KHÔNG lưu — luôn SUY RA từ (now - anchor) modulo chu kỳ:
//
//      cycle = hide + travel + dock + travel
//      phase = ((now - anchor) giây) mod cycle
//        [0, hide)                       → Hidden    (núp ở điểm mù)
//        [hide, hide+travel)             → Arriving  (tiến vào bến)
//        [hide+travel, hide+travel+dock) → Docked    (đậu, có countdown)
//        còn lại                         → Departing (LÙI ra, về điểm mù)
//
//  Nhờ modulo, offline catch-up là TỰ NHIÊN: tắt game 3 tiếng mở lại, phase
//  vẫn tính đúng từ đồng hồ UTC — không cần "tua" từng giây nào cả (GDD §3.4).
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Trạng thái vòng đời tàu du lịch của MỘT bến (mỗi bến 1 tàu riêng).
/// Locked là trạng thái "chưa mở bến" — không nằm trong chu kỳ modulo,
/// 4 trạng thái còn lại lặp vô hạn theo GDD §3.2.
/// </summary>
public enum BoatState
{
    Locked,     // Bến chưa mở khóa — không có tàu
    Hidden,     // Tàu núp ở điểm mù ngoài khơi (SetActive(false))
    Arriving,   // Tàu chạy theo waypoint từ điểm mù vào bến, mũi hướng bến
    Docked,     // Tàu đậu ở berth, hiện countdown
    Departing,  // Tàu LÙI ngược waypoint ra khỏi bến rồi về điểm mù
}

/// <summary>
/// Lý do từ chối mở bến — dạng enum để lõi không dính chuỗi UI.
/// BoatDockManager chịu trách nhiệm dịch sang tiếng Việt cho người chơi.
/// </summary>
public enum UnlockDenyReason
{
    None,            // Đủ điều kiện — cho mở
    InvalidDock,     // dockIndex ngoài [0..2]
    AlreadyUnlocked, // Bến đã mở rồi
    LevelTooLow,     // Chưa đạt level yêu cầu
    NotEnoughGold,   // Không đủ vàng
    NotEnoughGems,   // Không đủ gem
}

/// <summary>
/// Điều kiện mở 1 bến (đọc từ TouristBoatConfig, truyền vào lõi dạng tham số —
/// lõi KHÔNG gọi manager nào cả, GDD §3.1 / yêu cầu testable).
/// </summary>
public struct DockUnlockRequirement
{
    /// <summary>Level tối thiểu phải đạt.</summary>
    public int RequiredLevel;
    /// <summary>Giá vàng (0 = không tốn vàng).</summary>
    public int GoldCost;
    /// <summary>Giá gem (0 = không tốn gem).</summary>
    public int GemCost;
}

/// <summary>
/// Kết quả tính pha của 1 tàu tại 1 thời điểm — struct thuần, không alloc heap,
/// an toàn gọi mỗi frame từ Update.
/// </summary>
public struct BoatPhaseInfo
{
    /// <summary>Trạng thái hiện tại (Hidden/Arriving/Docked/Departing).</summary>
    public BoatState State;

    /// <summary>
    /// Tiến độ 0-1 TRONG pha hiện tại. Với Arriving/Departing đây chính là
    /// tỉ lệ quãng đường trên path — controller dùng để snap đúng vị trí
    /// khi vào game giữa chừng (GDD §5 edge case 2).
    /// Arriving: 0 = điểm mù, 1 = berth. Departing: 0 = berth, 1 = điểm mù.
    /// </summary>
    public double Progress;

    /// <summary>Giây còn lại của pha Docked (countdown). &lt;= 0 nếu không Docked.</summary>
    public double DockedRemainingSeconds;

    /// <summary>Vị trí hiện tại trong chu kỳ, [0, CycleSeconds) — tiện debug/test.</summary>
    public double PhaseSeconds;

    /// <summary>Tổng độ dài 1 chu kỳ (giây) = hide + travel + dock + travel.</summary>
    public double CycleSeconds;
}

/// <summary>
/// Thông số chu kỳ của 1 bến đã mở — dùng cho phép tính so le (GDD §3.3).
/// Mỗi bến có travelSeconds riêng (path dài ngắn khác nhau) nên chu kỳ
/// từng bến có thể lệch nhau chút ít.
/// </summary>
public struct BoatCycleSpec
{
    /// <summary>Mốc neo chu kỳ (ticks UTC).</summary>
    public long AnchorUtcTicks;
    /// <summary>Giây núp ở điểm mù.</summary>
    public double HideSeconds;
    /// <summary>Giây đậu bến.</summary>
    public double DockSeconds;
    /// <summary>Giây chạy 1 chiều điểm mù → berth.</summary>
    public double TravelSeconds;
}

/// <summary>
/// Toàn bộ logic thời gian của hệ Tourist Boat — static, thuần, deterministic.
/// Không trạng thái, không side effect: cùng input luôn cho cùng output,
/// unit test chỉ cần bơm ticks giả (GDD §8 tiêu chí 8).
/// </summary>
public static class BoatScheduleCore
{
    /// <summary>1 giây = 10.000.000 ticks (trùng System.TimeSpan.TicksPerSecond).</summary>
    public const long TicksPerSecond = 10000000L;

    /// <summary>
    /// Sàn cho travelSeconds — tránh chia 0 khi path suy biến (chưa gắn waypoint).
    /// Chỉ là guard kỹ thuật, KHÔNG phải tuning knob gameplay.
    /// </summary>
    private const double MinTravelSeconds = 0.001;

    /// <summary>Số vòng tối đa khi giải so le — 3 bến thì 2-3 vòng là hội tụ, 16 là dư an toàn.</summary>
    private const int MaxStaggerIterations = 16;

    // ─── Đổi đơn vị ──────────────────────────────────────────────────────

    /// <summary>Đổi giây (double) → ticks (long), làm tròn gần nhất.</summary>
    public static long SecondsToTicks(double seconds)
    {
        return (long)Math.Round(seconds * TicksPerSecond);
    }

    /// <summary>Đổi ticks (long) → giây (double).</summary>
    public static double TicksToSeconds(long ticks)
    {
        return (double)ticks / TicksPerSecond;
    }

    // ─── Chu kỳ & pha ────────────────────────────────────────────────────

    /// <summary>
    /// Độ dài 1 chu kỳ đầy đủ (giây): núp + vào + đậu + lùi ra (GDD §4).
    /// Input âm được kẹp về 0 (travel kẹp về sàn kỹ thuật) — không bao giờ trả ≤ 0.
    /// </summary>
    public static double ComputeCycleSeconds(double dockSeconds, double hideSeconds, double travelSeconds)
    {
        double hide   = Math.Max(0.0, hideSeconds);
        double dock   = Math.Max(0.0, dockSeconds);
        double travel = Math.Max(MinTravelSeconds, travelSeconds);
        return hide + travel + dock + travel;
    }

    /// <summary>
    /// Hàm TRUNG TÂM: suy trạng thái + tiến độ của tàu từ mốc neo và thời điểm hiện tại.
    ///
    /// timeScale là hệ số tua thời gian để test (debugTimeScale trong config):
    /// elapsed nhân thẳng với scale trước khi modulo — scale 60 nghĩa là 1 giây
    /// thực = 1 phút game. Bản build release luôn truyền 1.
    ///
    /// Chống đồng hồ lùi ở mức phòng thủ: elapsed âm bị kẹp về 0 (coi như chu kỳ
    /// vừa bắt đầu) — KHÔNG crash, không pha âm. Việc reset anchor bền vững
    /// (persist lại) là trách nhiệm của manager, xem <see cref="IsAnchorInFuture"/>.
    /// </summary>
    public static BoatPhaseInfo ComputePhase(
        long nowUtcTicks, long anchorUtcTicks,
        double dockSeconds, double hideSeconds, double travelSeconds,
        double timeScale = 1.0)
    {
        double hide   = Math.Max(0.0, hideSeconds);
        double dock   = Math.Max(0.0, dockSeconds);
        double travel = Math.Max(MinTravelSeconds, travelSeconds);
        double scale  = timeScale > 0.0 ? timeScale : 1.0;
        double cycle  = hide + travel + dock + travel;

        double elapsed = TicksToSeconds(nowUtcTicks - anchorUtcTicks) * scale;
        if (elapsed < 0.0)
            elapsed = 0.0; // đồng hồ máy bị chỉnh lùi — coi như chu kỳ mới bắt đầu (GDD §5 edge 4)

        // Modulo chu kỳ → offline catch-up tự nhiên (GDD §3.4)
        double phase = elapsed % cycle;

        BoatPhaseInfo info;
        info.PhaseSeconds           = phase;
        info.CycleSeconds           = cycle;
        info.DockedRemainingSeconds = 0.0;

        double arriveStart = hide;                  // mốc bắt đầu Arriving
        double dockStart   = hide + travel;         // mốc cập bến (dùng cho so le)
        double departStart = hide + travel + dock;  // mốc bắt đầu Departing

        if (phase < arriveStart)
        {
            info.State    = BoatState.Hidden;
            info.Progress = hide > 0.0 ? phase / hide : 0.0;
        }
        else if (phase < dockStart)
        {
            info.State    = BoatState.Arriving;
            info.Progress = (phase - arriveStart) / travel;
        }
        else if (phase < departStart)
        {
            info.State    = BoatState.Docked;
            info.Progress = dock > 0.0 ? (phase - dockStart) / dock : 1.0;
            info.DockedRemainingSeconds = departStart - phase;
        }
        else
        {
            info.State    = BoatState.Departing;
            info.Progress = (phase - departStart) / travel;
        }

        return info;
    }

    // ─── Chống đồng hồ lùi ──────────────────────────────────────────────

    /// <summary>
    /// Anchor nằm trong TƯƠNG LAI so với now → đồng hồ máy đã bị chỉnh lùi
    /// (hoặc dữ liệu hỏng). Manager phát hiện qua hàm này rồi persist anchor mới.
    /// </summary>
    public static bool IsAnchorInFuture(long nowUtcTicks, long anchorUtcTicks)
    {
        return anchorUtcTicks > nowUtcTicks;
    }

    /// <summary>
    /// Trả anchor hợp lệ: nếu anchor &gt; now thì reset anchor = now
    /// (chu kỳ bắt đầu lại, không phạt người chơi — GDD §3.4).
    /// CẢNH BÁO: đây là phép kẹp THÔ (mọi anchor tương lai đều bị kẹp) — KHÔNG dùng
    /// làm guard đồng hồ lùi ở manager, xem <see cref="IsClockRolledBack"/> (QA B-1).
    /// </summary>
    public static long SanitizeAnchor(long nowUtcTicks, long anchorUtcTicks)
    {
        return anchorUtcTicks > nowUtcTicks ? nowUtcTicks : anchorUtcTicks;
    }

    /// <summary>
    /// Đồng hồ máy có THỰC SỰ bị chỉnh lùi không — anchor được PHÉP nằm ở tương lai
    /// trong phạm vi 1 chu kỳ, vì luật so le (§3.3) có thể đẩy anchor vượt quá
    /// hideSeconds khi mở bến mới lúc bến khác đang hoạt động. ComputePhase xử lý
    /// anchor tương lai đúng nghĩa: elapsed âm kẹp về 0 → tàu Hidden chờ tới lượt.
    /// Chỉ khi anchor vượt now QUÁ 1 × cycleDuration mới chắc chắn là đồng hồ
    /// lùi / dữ liệu hỏng → manager reset anchor = now.
    ///
    /// [QA B-1 — test-comment] Bug cũ: guard dùng anchor &gt; now (IsAnchorInFuture).
    /// Kịch bản tái hiện: bến 1 đang hoạt động, mở bến 2 → so le đẩy anchor bến 2
    /// lên tương lai → frame kế guard tưởng đồng hồ lùi, reset anchor = now → tàu 2
    /// xuất phát sớm, gap cập bến thực đo 8' &lt; staggerMinutes 12' (~16% số lần mở).
    /// Guard mới: dung sai 1 chu kỳ — so le không bao giờ đẩy xa đến thế
    /// (tối đa ≈ cycle/2 + 2×stagger − hide &lt; 1 cycle với config mặc định).
    /// </summary>
    public static bool IsClockRolledBack(long nowUtcTicks, long anchorUtcTicks, double cycleSeconds)
    {
        return anchorUtcTicks - nowUtcTicks > SecondsToTicks(Math.Max(0.0, cycleSeconds));
    }

    // ─── Thời điểm cập bến (phục vụ so le) ──────────────────────────────

    /// <summary>
    /// Lần cập bến ĐẦU TIÊN của chu kỳ (ticks UTC): anchor + hide + travel.
    /// </summary>
    public static long FirstArrivalUtcTicks(BoatCycleSpec spec)
    {
        double hide   = Math.Max(0.0, spec.HideSeconds);
        double travel = Math.Max(MinTravelSeconds, spec.TravelSeconds);
        return spec.AnchorUtcTicks + SecondsToTicks(hide + travel);
    }

    /// <summary>
    /// Lần cập bến KẾ TIẾP tại-hoặc-sau thời điểm nowUtcTicks.
    /// Các lần cập bến của 1 bến là: firstArrival + k * cycle, k = 0, 1, 2, ...
    /// </summary>
    public static long NextArrivalUtcTicks(long nowUtcTicks, BoatCycleSpec spec)
    {
        long first = FirstArrivalUtcTicks(spec);
        if (nowUtcTicks <= first)
            return first;

        double cycle  = ComputeCycleSeconds(spec.DockSeconds, spec.HideSeconds, spec.TravelSeconds);
        double behind = TicksToSeconds(nowUtcTicks - first);
        long k = (long)Math.Ceiling(behind / cycle);
        if (k < 0) k = 0;
        return first + SecondsToTicks(k * cycle);
    }

    /// <summary>
    /// Lần cập bến GẦN NHẤT (trước hoặc sau) so với thời điểm tham chiếu —
    /// dùng cho luật so le "cách lần cập bến gần nhất của bến khác" (GDD §3.3).
    /// Không có lần cập bến nào trước firstArrival (k không âm).
    /// </summary>
    public static long NearestArrivalUtcTicks(long referenceUtcTicks, BoatCycleSpec spec)
    {
        long first = FirstArrivalUtcTicks(spec);
        if (referenceUtcTicks <= first)
            return first;

        double cycle = ComputeCycleSeconds(spec.DockSeconds, spec.HideSeconds, spec.TravelSeconds);
        long k = (long)Math.Round(TicksToSeconds(referenceUtcTicks - first) / cycle);
        if (k < 0) k = 0;

        // Round có thể lệch 1 chu kỳ do sai số double — so cả 2 hàng xóm cho chắc.
        long best     = first + SecondsToTicks(k * cycle);
        long bestDiff = Math.Abs(referenceUtcTicks - best);

        long after     = first + SecondsToTicks((k + 1) * cycle);
        long afterDiff = Math.Abs(referenceUtcTicks - after);
        if (afterDiff < bestDiff) { best = after; bestDiff = afterDiff; }

        if (k > 0)
        {
            long before     = first + SecondsToTicks((k - 1) * cycle);
            long beforeDiff = Math.Abs(referenceUtcTicks - before);
            if (beforeDiff < bestDiff) { best = before; }
        }

        return best;
    }

    // ─── Luật so le anchor (GDD §3.3) ───────────────────────────────────

    /// <summary>
    /// Giải luật so le khi mở bến mới: nhận anchor MONG MUỐN (thường đặt sao cho
    /// tàu Arriving ngay lập tức — dopamine GDD §3.1), kiểm tra lần cập bến dự kiến
    /// so với các lần cập bến của những bến ĐÃ MỞ; nếu khoảng cách với lần cập bến
    /// gần nhất của bất kỳ bến nào &lt; staggerSeconds thì ĐẨY LÙI (anchor += phần
    /// thiếu) cho đủ khoảng cách, lặp đến khi hết xung đột.
    ///
    /// Chỉ đẩy về SAU, không kéo về trước → mở bến không bao giờ làm tàu đến sớm
    /// hơn tự nhiên. Lặp tối đa MaxStaggerIterations vòng (3 bến hội tụ rất nhanh).
    /// </summary>
    /// <param name="desiredAnchorUtcTicks">Anchor mong muốn cho bến mới.</param>
    /// <param name="dockSeconds">Giây đậu bến của bến mới.</param>
    /// <param name="hideSeconds">Giây núp của bến mới.</param>
    /// <param name="travelSeconds">Giây chạy 1 chiều của bến mới.</param>
    /// <param name="staggerSeconds">Khoảng cách so le tối thiểu giữa 2 lần cập bến bất kỳ.</param>
    /// <param name="otherDocks">Chu kỳ của các bến đã mở khác (chỉ đọc otherCount phần tử đầu).</param>
    /// <param name="otherCount">Số phần tử hợp lệ trong otherDocks.</param>
    /// <returns>Anchor đã dời (== desired nếu không xung đột).</returns>
    public static long ResolveStaggeredAnchor(
        long desiredAnchorUtcTicks,
        double dockSeconds, double hideSeconds, double travelSeconds,
        double staggerSeconds,
        BoatCycleSpec[] otherDocks, int otherCount)
    {
        if (otherDocks == null || otherCount <= 0 || staggerSeconds <= 0.0)
            return desiredAnchorUtcTicks;
        if (otherCount > otherDocks.Length)
            otherCount = otherDocks.Length;

        var newSpec = new BoatCycleSpec
        {
            AnchorUtcTicks = desiredAnchorUtcTicks,
            HideSeconds    = hideSeconds,
            DockSeconds    = dockSeconds,
            TravelSeconds  = travelSeconds,
        };

        long firstArrival = FirstArrivalUtcTicks(newSpec);
        long candidate    = firstArrival;
        long staggerTicks = SecondsToTicks(staggerSeconds);

        for (int iter = 0; iter < MaxStaggerIterations; iter++)
        {
            bool moved = false;
            for (int i = 0; i < otherCount; i++)
            {
                long nearest = NearestArrivalUtcTicks(candidate, otherDocks[i]);
                if (Math.Abs(candidate - nearest) < staggerTicks)
                {
                    candidate = nearest + staggerTicks; // anchor += phần thiếu (GDD §4)
                    moved = true;
                }
            }
            if (!moved)
                break;
        }

        return desiredAnchorUtcTicks + (candidate - firstArrival);
    }

    // ─── Điều kiện mở bến (GDD §3.1) ────────────────────────────────────

    /// <summary>
    /// Kiểm tra điều kiện mở bến — nhận toàn bộ dữ kiện dạng THAM SỐ
    /// (level hiện tại, số dư vàng/gem), không gọi manager nào → test được bằng .NET thuần.
    /// Thứ tự ưu tiên lý do từ chối: đã mở → thiếu level → thiếu vàng → thiếu gem.
    /// Hàm này CHỈ kiểm tra, không trừ tiền — trừ tiền là việc của
    /// FarmEconomyManager.SpendGold/SpendGems (API tự từ chối nếu không đủ, GDD §3.1).
    /// </summary>
    public static UnlockDenyReason EvaluateUnlock(
        DockUnlockRequirement requirement,
        bool alreadyUnlocked,
        int currentLevel, long gold, long gems)
    {
        if (alreadyUnlocked)                          return UnlockDenyReason.AlreadyUnlocked;
        if (currentLevel < requirement.RequiredLevel) return UnlockDenyReason.LevelTooLow;
        if (gold < requirement.GoldCost)              return UnlockDenyReason.NotEnoughGold;
        if (gems < requirement.GemCost)               return UnlockDenyReason.NotEnoughGems;
        return UnlockDenyReason.None;
    }
}
