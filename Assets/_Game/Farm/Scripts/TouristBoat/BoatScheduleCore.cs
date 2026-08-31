using System;

// ═══════════════════════════════════════════════════════════════════════════
//  BoatScheduleCore — LÕI THỜI GIAN THUẦN C# của hệ thống Bến Tàu Du Lịch.
//
//  TUYỆT ĐỐI KHÔNG using UnityEngine trong file này: tester biên dịch và chạy
//  unit test bằng mcs + mono thuần, không cần mở Unity. Mọi thứ dính tới Unity
//  (Transform, PlayerPrefs, Debug.Log...) nằm ở BoatDockManager /
//  TouristBoatController — hai lớp đó chỉ là wrapper mỏng quanh lõi này.
//
//  ── V2 (BOAT-002, event-driven) ─────────────────────────────────────────
//  BỎ mô hình "modulo chu kỳ cố định" của V1 (tàu không còn đậu đúng 40 phút).
//  Mỗi bến giờ là MÁY TRẠNG THÁI TƯỜNG MINH, persist được (GDD V2 §3.1):
//
//      WaitingNext(nextArrivalUtc)  — tàu núp, đã biết giờ cập bến kế tiếp
//        → Arriving(travelSeconds)  — tiến vào bến, tiến độ 0-1 suy từ UTC
//        → Docked (VÔ HẠN)          — đậu chờ LỆNH: khách được phục vụ xong,
//                                     TouristVisitorManager (Dev B) gọi
//                                     ReportVisitorsAllAboard trên manager
//        → Departing(travelSeconds) — lùi ra, đồng thời đã lên lịch chuyến kế
//        → WaitingNext(...)         — lặp lại
//
//  Tính chất giữ nguyên từ V1: mọi truy vấn đều SUY RA từ
//  (state, anchorUtc, nowUtc) — thuần, deterministic, idempotent khi reload.
//  Tắt game 3 tiếng mở lại: ResolveDock "tua" chuỗi trạng thái offline trong
//  1 lần gọi (Departing xong → WaitingNext → arrival đã qua → Docked) và báo
//  JustDocked đúng 1 lần — KHÔNG lặp vô hạn vì Docked là trạng thái hấp thụ
//  (chỉ thoát bằng lệnh, không bằng thời gian).
//
//  debugTimeScale KHÔNG vào lõi này: manager tự chia mọi duration (gap, travel,
//  stagger) cho scale trước khi truyền vào — lõi chỉ biết giây "thật".
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Trạng thái vòng đời tàu du lịch của MỘT bến (mỗi bến 1 tàu riêng).
/// Locked là trạng thái "chưa mở bến" — không nằm trong máy trạng thái chuyến.
///
/// V2: WaitingNext là TÊN MỚI của Hidden (cùng giá trị enum) — code V1 /
/// diagnostic tool dùng BoatState.Hidden vẫn biên dịch và chạy đúng;
/// code V2 nên dùng WaitingNext cho đúng ngữ nghĩa "đang chờ chuyến kế".
/// </summary>
public enum BoatState
{
    Locked      = 0, // Bến chưa mở khóa — không có tàu
    Hidden      = 1, // (tên V1) Tàu núp ở điểm mù ngoài khơi
    WaitingNext = Hidden, // (tên V2) Chờ chuyến kế — anchor = arrivalUtc đã lên lịch
    Arriving    = 2, // Tàu chạy theo waypoint từ điểm mù vào bến
    Docked      = 3, // V2: đậu VÔ HẠN — chờ ReportVisitorsAllAboard, không countdown
    Departing   = 4, // Tàu LÙI ngược waypoint ra khỏi bến (chuyến kế đã lên lịch)
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
/// lõi KHÔNG gọi manager nào cả, giữ testable như V1).
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
/// an toàn gọi mỗi frame từ Update. Giữ nguyên layout V1 cho code cũ
/// (TouristBoatController, TouristBoatDiagnosticTool) biên dịch không đổi.
/// </summary>
public struct BoatPhaseInfo
{
    /// <summary>Trạng thái hiện tại (WaitingNext/Arriving/Docked/Departing).</summary>
    public BoatState State;

    /// <summary>
    /// Tiến độ 0-1 TRONG pha hiện tại. Với Arriving/Departing đây chính là
    /// tỉ lệ quãng đường trên path — controller dùng để snap đúng vị trí
    /// khi vào game giữa chừng. Arriving: 0 = điểm mù, 1 = berth.
    /// Departing: 0 = berth, 1 = điểm mù. WaitingNext/Docked: 0.
    /// </summary>
    public double Progress;

    /// <summary>
    /// V1: giây còn lại của pha Docked (countdown). V2: Docked là VÔ HẠN nên
    /// luôn trả -1 khi Docked (không có deadline) và 0 khi không Docked —
    /// giữ contract cũ "&lt;= 0 nếu không Docked", UI không được hiện countdown nữa.
    /// </summary>
    public double DockedRemainingSeconds;

    /// <summary>
    /// V2 (debug): số giây "có nghĩa" của pha hiện tại —
    /// WaitingNext: giây CÒN LẠI tới arrival; các pha khác: giây ĐÃ TRÔI trong pha.
    /// </summary>
    public double PhaseSeconds;

    /// <summary>V1 legacy: độ dài chu kỳ. V2 không còn chu kỳ cố định → 0.</summary>
    public double CycleSeconds;
}

/// <summary>
/// V2 — TOÀN BỘ dữ liệu persist của 1 bến (ngoài cờ unlocked): máy trạng thái
/// tường minh. BoatDockManager lưu 3 giá trị này vào PlayerPrefs mỗi khi đổi.
///
/// Ý nghĩa anchor theo state:
///   WaitingNext / Arriving : AnchorUtcTicks = arrivalUtc (giờ CHẠM BẾN dự kiến;
///                            Arriving bắt đầu tại arrival − travel)
///   Docked                 : AnchorUtcTicks = giờ đã chạm bến (dockedAtUtc)
///   Departing              : AnchorUtcTicks = giờ bắt đầu rời bến;
///                            NextArrivalUtcTicks = arrival chuyến kế ĐÃ lên lịch
/// NextArrivalUtcTicks chỉ có nghĩa khi Departing — các state khác để 0.
/// </summary>
public struct DockScheduleState
{
    /// <summary>Trạng thái persist (Locked/WaitingNext/Arriving/Docked/Departing).</summary>
    public BoatState State;
    /// <summary>Mốc UTC ticks — ý nghĩa tùy state, xem doc của struct.</summary>
    public long AnchorUtcTicks;
    /// <summary>Arrival chuyến kế (chỉ khi Departing). 0 = không có.</summary>
    public long NextArrivalUtcTicks;
}

/// <summary>
/// Kết quả 1 lần "tua" máy trạng thái tới nowUtc (ResolveDock) — thuần, không
/// side effect; manager chịu trách nhiệm persist State mới và bắn event.
/// </summary>
public struct DockResolveResult
{
    /// <summary>Trạng thái persist MỚI sau khi tua (== input nếu không đổi).</summary>
    public DockScheduleState State;
    /// <summary>
    /// true đúng 1 lần khi lần tua này CHẠM BẾN (WaitingNext/Arriving → Docked,
    /// kể cả chuỗi offline Departing → WaitingNext → Docked). Manager bắn
    /// OnBoatDocked theo cờ này — vì State mới đã là Docked (hấp thụ) nên gọi
    /// ResolveDock lại lần nữa KHÔNG thể ra JustDocked lần hai (chống double-fire).
    /// </summary>
    public bool JustDocked;
    /// <summary>true nếu State đổi so với input — manager cần persist lại.</summary>
    public bool Changed;
}

/// <summary>
/// Toàn bộ logic thời gian của hệ Tourist Boat — static, thuần, deterministic.
/// Không trạng thái, không side effect: cùng input luôn cho cùng output,
/// unit test chỉ cần bơm ticks giả (tests/unit/touristboat, chạy mcs + mono).
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

    /// <summary>
    /// V2: độ trễ arrival khi (a) migrate save V1 → V2, (b) reset vì đồng hồ lùi —
    /// "tàu vào ngay lần đầu" sau 30 giây, đủ để popup báo trước kịp hiện.
    /// Đây là hằng KỸ THUẬT của luật migrate (chốt trong design V2), không phải tuning knob.
    /// </summary>
    public const double FreshArrivalDelaySeconds = 30.0;

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

    // ═════════════════════════════════════════════════════════════════════
    //  V2 — MÁY TRẠNG THÁI EVENT-DRIVEN (GDD V2 §3.1 / §3.2)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trạng thái "mới tinh": WaitingNext với arrival = now + delaySeconds.
    /// Dùng khi (a) migrate save V1 (chỉ có anchor cũ) → tàu vào ngay lần đầu,
    /// (b) reset vì đồng hồ lùi, (c) dữ liệu prefs hỏng.
    /// </summary>
    public static DockScheduleState MakeFreshWaiting(long nowUtcTicks, double delaySeconds)
    {
        DockScheduleState s;
        s.State               = BoatState.WaitingNext;
        s.AnchorUtcTicks      = nowUtcTicks + SecondsToTicks(Math.Max(0.0, delaySeconds));
        s.NextArrivalUtcTicks = 0L;
        return s;
    }

    /// <summary>
    /// HÀM TRUNG TÂM V2: "tua" máy trạng thái của 1 bến tới nowUtc.
    /// Thuần — không side effect; manager persist State mới khi Changed và bắn
    /// OnBoatDocked khi JustDocked (đúng 1 lần, kể cả resolve khi load —
    /// xem doc của <see cref="DockResolveResult.JustDocked"/>).
    ///
    /// Các bước tua có thể xảy ra trong MỘT lần gọi (offline dài):
    ///   WaitingNext → Arriving (now vượt arrival − travel)
    ///   WaitingNext/Arriving → Docked (now vượt arrival) → JustDocked
    ///   Departing → WaitingNext (now vượt departStart + travel)
    ///             → (đệ quy 1 tầng) có thể chạm luôn Docked nếu arrival kế đã qua
    ///   Docked → KHÔNG BAO GIỜ tự thoát — chỉ thoát qua TryBeginDeparture (lệnh).
    ///
    /// Đồng hồ lùi NHẸ (trong phạm vi hợp lệ): Arriving mà now tụt về trước
    /// arrival − travel → quay về WaitingNext êm ái, không reset lịch.
    /// Đồng hồ lùi NẶNG do manager phát hiện qua IsScheduleImplausiblyFuture.
    /// </summary>
    public static DockResolveResult ResolveDock(DockScheduleState state, long nowUtcTicks, double travelSeconds)
    {
        double travel      = Math.Max(MinTravelSeconds, travelSeconds);
        long   travelTicks = SecondsToTicks(travel);

        DockResolveResult r;
        r.State      = state;
        r.JustDocked = false;
        r.Changed    = false;

        switch (state.State)
        {
            case BoatState.WaitingNext: // (== Hidden)
            case BoatState.Arriving:
            {
                long arrival     = state.AnchorUtcTicks;
                long arriveStart = arrival - travelTicks;

                if (nowUtcTicks >= arrival)
                {
                    // CHẠM BẾN — Docked vô hạn, anchor = giờ chạm bến thật (arrival).
                    r.State.State               = BoatState.Docked;
                    r.State.AnchorUtcTicks      = arrival;
                    r.State.NextArrivalUtcTicks = 0L;
                    r.JustDocked                = true;
                    r.Changed                   = true;
                }
                else if (nowUtcTicks >= arriveStart)
                {
                    if (state.State != BoatState.Arriving)
                    {
                        r.State.State = BoatState.Arriving; // anchor giữ nguyên = arrivalUtc
                        r.Changed     = true;
                    }
                }
                else if (state.State == BoatState.Arriving)
                {
                    // Đồng hồ lùi nhẹ (vẫn trong horizon hợp lệ) — lùi về WaitingNext,
                    // giữ nguyên arrival: tàu sẽ vào lại đúng lịch, không phạt ai.
                    r.State.State = BoatState.WaitingNext;
                    r.Changed     = true;
                }
                return r;
            }

            case BoatState.Docked:
                // Trạng thái HẤP THỤ — chờ lệnh ReportVisitorsAllAboard từ Dev B.
                // Load vào giữa pha Docked thì cứ ở Docked (GDD V2 §5 edge 1).
                return r;

            case BoatState.Departing:
            {
                long departEnd = state.AnchorUtcTicks + travelTicks;
                if (nowUtcTicks < departEnd)
                    return r; // vẫn đang lùi ra

                // Rời bến xong → WaitingNext với arrival ĐÃ lên lịch từ lúc rời bến.
                // NextArrival hỏng/thiếu (0 — save lỗi) → phòng thủ: hẹn tạm sau 2×travel
                // để tàu KHÔNG quay đầu vào bến ngay trong cùng frame; chuyến sau manager
                // lên lịch lại đúng gap. Không dùng gap ở đây vì lõi không biết số bến mở.
                long next = state.NextArrivalUtcTicks > 0L
                    ? state.NextArrivalUtcTicks
                    : nowUtcTicks + travelTicks * 2L;

                r.State.State               = BoatState.WaitingNext;
                r.State.AnchorUtcTicks      = next;
                r.State.NextArrivalUtcTicks = 0L;
                r.Changed                   = true;

                // Tua tiếp chuỗi offline: arrival kế có thể đã qua từ lâu → Docked luôn.
                // Đệ quy TỐI ĐA 1 tầng có nghĩa: nhánh WaitingNext không quay lại Departing.
                DockResolveResult chained = ResolveDock(r.State, nowUtcTicks, travel);
                if (chained.Changed)
                {
                    r.State      = chained.State;
                    r.JustDocked = chained.JustDocked;
                }
                return r;
            }

            default: // Locked — không có tàu, không có gì để tua
                return r;
        }
    }

    /// <summary>
    /// Truy vấn HIỂN THỊ (state + tiến độ 0-1) tại nowUtc — thuần, gọi mỗi frame
    /// từ TryGetPhaseInfo. Tự tua nội bộ qua ResolveDock nên kể cả khi manager
    /// chưa kịp persist transition của frame này, hiển thị vẫn đúng tức thì.
    /// </summary>
    public static BoatPhaseInfo QueryPhase(DockScheduleState state, long nowUtcTicks, double travelSeconds)
    {
        double travel = Math.Max(MinTravelSeconds, travelSeconds);
        DockScheduleState cur = ResolveDock(state, nowUtcTicks, travel).State;

        BoatPhaseInfo info;
        info.State                  = cur.State;
        info.Progress               = 0.0;
        info.DockedRemainingSeconds = 0.0;
        info.PhaseSeconds           = 0.0;
        info.CycleSeconds           = 0.0; // V2: không còn chu kỳ cố định

        switch (cur.State)
        {
            case BoatState.WaitingNext: // (== Hidden)
                // PhaseSeconds = giây CÒN LẠI tới arrival (debug/UI "sắp cập bến").
                info.PhaseSeconds = TicksToSeconds(cur.AnchorUtcTicks - nowUtcTicks);
                break;

            case BoatState.Arriving:
            {
                long   arriveStart = cur.AnchorUtcTicks - SecondsToTicks(travel);
                double elapsed     = TicksToSeconds(nowUtcTicks - arriveStart);
                info.Progress      = Clamp01(elapsed / travel);
                info.PhaseSeconds  = elapsed;
                break;
            }

            case BoatState.Docked:
                // V2: Docked VÔ HẠN — không countdown. -1 giữ contract "<=0 nếu
                // không có deadline"; UI hiện "Đang đón khách..." thay số.
                info.DockedRemainingSeconds = -1.0;
                info.PhaseSeconds           = TicksToSeconds(nowUtcTicks - cur.AnchorUtcTicks);
                break;

            case BoatState.Departing:
            {
                double elapsed    = TicksToSeconds(nowUtcTicks - cur.AnchorUtcTicks);
                info.Progress     = Clamp01(elapsed / travel);
                info.PhaseSeconds = elapsed;
                break;
            }
        }

        return info;
    }

    // ─── V2: lên lịch chuyến kế (GDD V2 §3.2) ───────────────────────────

    /// <summary>
    /// Chọn gap (giây) theo số bến đang mở — BA MỨC (Lead chốt 2026-08-29 theo lời Sếp
    /// "sau này user mở hết 3 slot rồi lúc này cứ cách 10 phút sẽ tới so le nhau"):
    ///   1 bến  → gapOne   (5 phút)
    ///   2 bến  → gapTwo   (7 phút)
    ///   ≥3 bến → gapMulti (10 phút)
    ///
    /// Vì sao không nhảy thẳng 5 → 10 ở bến thứ hai: mốc 10 phút là của giai đoạn ĐỦ
    /// 3 SLOT. Còn để 2 bến cùng gap 5 phút thì cứ ~2,5 phút lại có tàu cập bờ, quá dồn
    /// dập. Ba mức giữ nhịp "có tàu vào bến" ổn định ~3,3-5 phút ở mọi giai đoạn:
    ///   1 bến: 5 phút/tàu · 2 bến: 7/2 = 3,5 phút · 3 bến: 10/3 ≈ 3,3 phút.
    ///
    /// Số đọc từ TouristBoatConfig, truyền vào dạng giây (manager đã chia debugTimeScale
    /// nếu đang tua nhanh). Giá trị âm trong config bị kẹp về 0.
    /// </summary>
    public static double SelectGapSeconds(int unlockedDockCount,
                                          double gapOneDockSeconds,
                                          double gapTwoDockSeconds,
                                          double gapMultiDockSeconds)
    {
        if (unlockedDockCount <= 1) return Math.Max(0.0, gapOneDockSeconds);
        if (unlockedDockCount == 2) return Math.Max(0.0, gapTwoDockSeconds);
        return Math.Max(0.0, gapMultiDockSeconds);
    }

    /// <summary>
    /// [V2.0 COMPAT] Bản 2 mức cũ (1 bến / ≥2 bến) — giữ cho code ngoài đã gọi theo
    /// chữ ký này khỏi gãy. Code mới dùng bản 3 mức ở trên.
    /// </summary>
    public static double SelectGapSeconds(int unlockedDockCount, double gapOneDockSeconds, double gapMultiDockSeconds)
    {
        return SelectGapSeconds(unlockedDockCount, gapOneDockSeconds, gapMultiDockSeconds, gapMultiDockSeconds);
    }

    /// <summary>
    /// Tính arrival chuyến kế khi tàu BẮT ĐẦU rời bến tại departUtc:
    ///   arrival = departUtc + gap, sàn kỹ thuật gap ≥ 2×travel + 1s
    ///   (tàu phải kịp lùi ra hết path rồi mới chạy vào lại — gap 5 phút so với
    ///   travel ~20s thì sàn không bao giờ chạm, chỉ là guard config dị),
    /// sau đó ép luật so le: cách MỌI arrival khác ≥ staggerSeconds bằng cách
    /// DỜI MUỘN (không bao giờ kéo sớm hơn) — xem ResolveStaggeredArrival.
    /// </summary>
    /// <param name="departUtcTicks">Mốc tàu bắt đầu rời bến (ticks UTC).</param>
    /// <param name="gapSeconds">Gap đã chọn qua SelectGapSeconds (giây).</param>
    /// <param name="travelSeconds">Giây chạy 1 chiều của bến.</param>
    /// <param name="staggerSeconds">Khoảng cách so le tối thiểu giữa 2 arrival bất kỳ.</param>
    /// <param name="otherArrivalsUtcTicks">Arrival sắp tới của các bến khác (0 = bỏ qua).</param>
    /// <param name="otherCount">Số phần tử hợp lệ trong otherArrivalsUtcTicks.</param>
    public static long ScheduleNextArrival(
        long departUtcTicks,
        double gapSeconds, double travelSeconds, double staggerSeconds,
        long[] otherArrivalsUtcTicks, int otherCount)
    {
        double travel = Math.Max(MinTravelSeconds, travelSeconds);
        double gap    = Math.Max(Math.Max(0.0, gapSeconds), 2.0 * travel + 1.0);

        long desired = departUtcTicks + SecondsToTicks(gap);
        return ResolveStaggeredArrival(desired, staggerSeconds, otherArrivalsUtcTicks, otherCount);
    }

    /// <summary>
    /// Ép luật so le trên MỘT arrival: nếu cách arrival nào đó của bến khác
    /// &lt; staggerSeconds thì DỜI MUỘN (candidate = arrivalKia + stagger),
    /// lặp tới khi hết xung đột (≤ MaxStaggerIterations vòng — 3 bến hội tụ ngay).
    /// Chỉ đẩy về SAU → không bao giờ làm tàu đến sớm hơn lịch tự nhiên.
    /// Phần tử ≤ 0 trong otherArrivals bị bỏ qua (bến không có arrival sắp tới).
    /// </summary>
    public static long ResolveStaggeredArrival(
        long desiredArrivalUtcTicks, double staggerSeconds,
        long[] otherArrivalsUtcTicks, int otherCount)
    {
        if (otherArrivalsUtcTicks == null || otherCount <= 0 || staggerSeconds <= 0.0)
            return desiredArrivalUtcTicks;
        if (otherCount > otherArrivalsUtcTicks.Length)
            otherCount = otherArrivalsUtcTicks.Length;

        long staggerTicks = SecondsToTicks(staggerSeconds);
        long candidate    = desiredArrivalUtcTicks;

        for (int iter = 0; iter < MaxStaggerIterations; iter++)
        {
            bool moved = false;
            for (int i = 0; i < otherCount; i++)
            {
                long other = otherArrivalsUtcTicks[i];
                if (other <= 0L) continue; // bến đó không có arrival sắp tới
                if (Math.Abs(candidate - other) < staggerTicks)
                {
                    candidate = other + staggerTicks; // dời muộn cho đủ khoảng cách
                    moved = true;
                }
            }
            if (!moved) break;
        }

        return candidate;
    }

    /// <summary>
    /// LỆNH duy nhất thoát pha Docked (V2 event-driven): Dev B báo "khách cuối
    /// đã lên tàu" → Docked → Departing(anchor = now) + lên lịch arrival chuyến kế
    /// (gap + so le) ghi vào NextArrivalUtcTicks.
    ///
    /// Trả false nếu state hiện tại KHÔNG phải Docked (gọi trùng / gọi sai pha)
    /// — result giữ nguyên input, manager bỏ qua êm: đây chính là guard
    /// chống double-fire khi Dev B lỡ gọi ReportVisitorsAllAboard 2 lần.
    /// </summary>
    public static bool TryBeginDeparture(
        DockScheduleState state, long nowUtcTicks,
        double gapSeconds, double travelSeconds, double staggerSeconds,
        long[] otherArrivalsUtcTicks, int otherCount,
        out DockScheduleState result)
    {
        result = state;
        if (state.State != BoatState.Docked)
            return false;

        long arrival = ScheduleNextArrival(
            nowUtcTicks, gapSeconds, travelSeconds, staggerSeconds,
            otherArrivalsUtcTicks, otherCount);

        result.State               = BoatState.Departing;
        result.AnchorUtcTicks      = nowUtcTicks;
        result.NextArrivalUtcTicks = arrival;
        return true;
    }

    // ─── V2: lưới an toàn chống kẹt tàu (QA B-1, Sếp duyệt 2026-08-29) ──

    /// <summary>
    /// Số giây tàu đã đậu ở bến (tính bằng UTC tuyệt đối từ mốc chạm bến — offline
    /// vẫn đếm). Không ở pha Docked → 0.
    /// </summary>
    public static double DockedElapsedSeconds(DockScheduleState state, long nowUtcTicks)
    {
        if (state.State != BoatState.Docked) return 0.0;
        double elapsed = TicksToSeconds(nowUtcTicks - state.AnchorUtcTicks);
        return elapsed > 0.0 ? elapsed : 0.0;
    }

    /// <summary>
    /// LƯỚI AN TOÀN: tàu đã đậu quá maxDockSeconds chưa (mốc kiên nhẫn khách —
    /// config maxDockMinutes, mặc định 35 phút — cố ý LỚN HƠN patienceMinutes 30, xem
    /// [QA M-7] trong TouristBoatConfig)? Quá hạn thì manager TỰ ép tàu rời
    /// bến y như khi nhận ReportVisitorsAllAboard, để hệ không bao giờ kẹt nếu
    /// TouristVisitorManager (Dev B) lỡ không báo (bug, khách kẹt pathfinding, scene
    /// bếp không chạy hệ khách...).
    ///
    /// maxDockSeconds ≤ 0 → TẮT lưới an toàn (tàu đậu vô hạn, đúng tinh thần
    /// event-driven thuần) — chỉ nên dùng khi debug.
    /// Không ở pha Docked → luôn false.
    /// </summary>
    public static bool IsDockTimedOut(DockScheduleState state, long nowUtcTicks, double maxDockSeconds)
    {
        if (maxDockSeconds <= 0.0) return false;
        if (state.State != BoatState.Docked) return false;
        return DockedElapsedSeconds(state, nowUtcTicks) >= maxDockSeconds;
    }

    /// <summary>
    /// Arrival sắp tới của 1 bến (ticks UTC) để bến khác so le / UI hiện popup.
    /// WaitingNext/Arriving → anchor (arrival đã lên lịch);
    /// Departing → NextArrival; Docked/Locked → 0 (không có arrival sắp tới).
    /// </summary>
    public static long UpcomingArrivalUtcTicks(DockScheduleState state)
    {
        switch (state.State)
        {
            case BoatState.WaitingNext: // (== Hidden)
            case BoatState.Arriving:
                return state.AnchorUtcTicks;
            case BoatState.Departing:
                return state.NextArrivalUtcTicks;
            default:
                return 0L;
        }
    }

    // ─── V2: chống đồng hồ lùi (giữ luật V1 §5 edge: reset khi lùi quá 1 gap) ──

    /// <summary>
    /// Mốc UTC nào đó của state nằm ở TƯƠNG LAI vượt quá horizon cho phép →
    /// đồng hồ máy đã bị chỉnh lùi nặng (hoặc dữ liệu hỏng) → manager reset về
    /// MakeFreshWaiting. Horizon do manager tính = gap lớn nhất + dự phòng so le
    /// (arrival hợp lệ không bao giờ được lên lịch xa hơn thế — "lùi quá 1 gap").
    /// Anchor tương lai TRONG horizon là hợp lệ (WaitingNext luôn có arrival tương lai).
    /// </summary>
    public static bool IsScheduleImplausiblyFuture(DockScheduleState state, long nowUtcTicks, double horizonSeconds)
    {
        long horizon = SecondsToTicks(Math.Max(0.0, horizonSeconds));

        switch (state.State)
        {
            case BoatState.WaitingNext: // (== Hidden)
            case BoatState.Arriving:
                return state.AnchorUtcTicks - nowUtcTicks > horizon;
            case BoatState.Docked:
                // dockedAt lẽ ra ở quá khứ — vượt tương lai quá horizon là hỏng.
                return state.AnchorUtcTicks - nowUtcTicks > horizon;
            case BoatState.Departing:
                return state.AnchorUtcTicks - nowUtcTicks > horizon
                    || state.NextArrivalUtcTicks - nowUtcTicks > horizon;
            default:
                return false;
        }
    }

    /// <summary>
    /// Số phút chờ tới arrival, làm tròn gần nhất, sàn 0 — cho popup
    /// "Tàu số 0X sẽ cập bến sau X phút" (tham số thứ 3 của OnNextTripScheduled).
    /// </summary>
    public static int RoundedWaitMinutes(long nowUtcTicks, long arrivalUtcTicks)
    {
        double minutes = TicksToSeconds(arrivalUtcTicks - nowUtcTicks) / 60.0;
        int rounded = (int)Math.Round(minutes, MidpointRounding.AwayFromZero);
        return rounded > 0 ? rounded : 0;
    }

    private static double Clamp01(double v)
    {
        if (v < 0.0) return 0.0;
        if (v > 1.0) return 1.0;
        return v;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  V1 LEGACY — chu kỳ modulo cố định (BOAT-001)
    //
    //  V2 KHÔNG dùng các hàm dưới đây cho lịch tàu nữa. Giữ lại vì:
    //   • TouristBoatDiagnosticTool.cs (Editor) còn gọi ComputePhase/Compute-
    //     CycleSeconds để chẩn đoán ở Edit Mode — xóa là gãy compile.
    //   • Bộ test V1 cũ (nếu QA còn chạy) vẫn pass nguyên trạng.
    //  Khi tool chẩn đoán được nâng V2 thì dọn cả khối này một lần.
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [V1 LEGACY] Độ dài 1 chu kỳ đầy đủ (giây): núp + vào + đậu + lùi ra.
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
    /// [V1 LEGACY — chỉ còn TouristBoatDiagnosticTool dùng ở Edit Mode]
    /// Suy trạng thái + tiến độ từ mốc neo theo mô hình modulo chu kỳ V1.
    /// V2 runtime KHÔNG đi qua đây — xem <see cref="ResolveDock"/> / <see cref="QueryPhase"/>.
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
            elapsed = 0.0; // đồng hồ máy bị chỉnh lùi — coi như chu kỳ mới bắt đầu

        double phase = elapsed % cycle;

        BoatPhaseInfo info;
        info.PhaseSeconds           = phase;
        info.CycleSeconds           = cycle;
        info.DockedRemainingSeconds = 0.0;

        double arriveStart = hide;
        double dockStart   = hide + travel;
        double departStart = hide + travel + dock;

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

    /// <summary>[V1 LEGACY] Anchor nằm trong TƯƠNG LAI so với now?</summary>
    public static bool IsAnchorInFuture(long nowUtcTicks, long anchorUtcTicks)
    {
        return anchorUtcTicks > nowUtcTicks;
    }

    /// <summary>
    /// [V1 LEGACY] Kẹp thô anchor tương lai về now. V2 còn dùng cho Docked/Departing
    /// khi đồng hồ lùi nhẹ (anchor quá khứ bị đẩy thành tương lai gần) — vô hại.
    /// </summary>
    public static long SanitizeAnchor(long nowUtcTicks, long anchorUtcTicks)
    {
        return anchorUtcTicks > nowUtcTicks ? nowUtcTicks : anchorUtcTicks;
    }

    /// <summary>
    /// [V1 LEGACY] Guard đồng hồ lùi theo chu kỳ V1 (dung sai 1 cycle — QA B-1).
    /// V2 dùng <see cref="IsScheduleImplausiblyFuture"/> thay thế.
    /// </summary>
    public static bool IsClockRolledBack(long nowUtcTicks, long anchorUtcTicks, double cycleSeconds)
    {
        return anchorUtcTicks - nowUtcTicks > SecondsToTicks(Math.Max(0.0, cycleSeconds));
    }

    /// <summary>[V1 LEGACY] Lần cập bến ĐẦU TIÊN của chu kỳ modulo.</summary>
    public static long FirstArrivalUtcTicks(BoatCycleSpec spec)
    {
        double hide   = Math.Max(0.0, spec.HideSeconds);
        double travel = Math.Max(MinTravelSeconds, spec.TravelSeconds);
        return spec.AnchorUtcTicks + SecondsToTicks(hide + travel);
    }

    /// <summary>[V1 LEGACY] Lần cập bến KẾ TIẾP tại-hoặc-sau nowUtcTicks (mô hình chu kỳ).</summary>
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

    /// <summary>[V1 LEGACY] Lần cập bến GẦN NHẤT so với thời điểm tham chiếu (mô hình chu kỳ).</summary>
    public static long NearestArrivalUtcTicks(long referenceUtcTicks, BoatCycleSpec spec)
    {
        long first = FirstArrivalUtcTicks(spec);
        if (referenceUtcTicks <= first)
            return first;

        double cycle = ComputeCycleSeconds(spec.DockSeconds, spec.HideSeconds, spec.TravelSeconds);
        long k = (long)Math.Round(TicksToSeconds(referenceUtcTicks - first) / cycle);
        if (k < 0) k = 0;

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

    /// <summary>[V1 LEGACY] Giải luật so le anchor theo mô hình chu kỳ V1.</summary>
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
                    candidate = nearest + staggerTicks;
                    moved = true;
                }
            }
            if (!moved)
                break;
        }

        return desiredAnchorUtcTicks + (candidate - firstArrival);
    }

    // ─── Điều kiện mở bến (dùng cả V1 lẫn V2 — không đổi) ───────────────

    /// <summary>
    /// Kiểm tra điều kiện mở bến — nhận toàn bộ dữ kiện dạng THAM SỐ
    /// (level hiện tại, số dư vàng/gem), không gọi manager nào → test được bằng .NET thuần.
    /// Thứ tự ưu tiên lý do từ chối: đã mở → thiếu level → thiếu vàng → thiếu gem.
    /// Hàm này CHỈ kiểm tra, không trừ tiền — trừ tiền là việc của
    /// FarmEconomyManager.SpendGold/SpendGems (API tự từ chối nếu không đủ).
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

/// <summary>
/// [V1 LEGACY] Thông số chu kỳ modulo của 1 bến — chỉ còn code chẩn đoán cũ dùng.
/// V2 persist qua <see cref="DockScheduleState"/> thay thế.
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
