using System;

// ═══════════════════════════════════════════════════════════════════════════
//  BoatScheduleCoreTests — bộ unit test CONSOLE cho lõi lịch tàu V2 (BOAT-002).
//
//  Chạy KHÔNG cần Unity (lõi thuần C#, không using UnityEngine):
//
//      cd <repo>
//      mcs -out:/tmp/boattests.exe \
//          Assets/_Game/Farm/Scripts/TouristBoat/BoatScheduleCore.cs \
//          tests/unit/touristboat/BoatScheduleCoreTests.cs
//      mono /tmp/boattests.exe
//
//  Exit code 0 = tất cả PASS, 1 = có FAIL (dùng được trong CI/QA gate).
//
//  Phủ theo yêu cầu QA V2:
//    A. Gap 5 phút (1 bến) / 10 phút (≥2 bến)
//    B. Luật so le ≥3 phút (dời MUỘN, không kéo sớm)
//    C. Resolve offline ở MỌI pha (WaitingNext / Arriving / Docked / Departing)
//    D. Guard đồng hồ lùi
//    E. ReportVisitorsAllAboard chuyển pha Docked → Departing + lên lịch chuyến kế
//    F. Chống double-fire (JustDocked đúng 1 lần, TryBeginDeparture gọi 2 lần)
//    H. Lưới an toàn chống kẹt tàu: đậu quá maxDockMinutes → ép rời bến (QA B-1)
//    G. Linh tinh: tiến độ 0-1, phút làm tròn cho popup, migrate/reset chuyến mới
// ═══════════════════════════════════════════════════════════════════════════

public static class BoatScheduleCoreTests
{
    // ─── Mini test framework ─────────────────────────────────────────────

    private static int _pass;
    private static int _fail;
    private static string _currentGroup = "";

    private static void Group(string name)
    {
        _currentGroup = name;
        Console.WriteLine();
        Console.WriteLine("── " + name + " " + new string('─', Math.Max(0, 60 - name.Length)));
    }

    private static void Check(bool condition, string what)
    {
        if (condition)
        {
            _pass++;
            Console.WriteLine("  [PASS] " + what);
        }
        else
        {
            _fail++;
            Console.WriteLine("  [FAIL] " + _currentGroup + " → " + what);
        }
    }

    private static void CheckEqual(long actual, long expected, string what)
    {
        Check(actual == expected, what + $" (mong đợi {expected}, thực tế {actual})");
    }

    private static void CheckNear(double actual, double expected, double tolerance, string what)
    {
        Check(Math.Abs(actual - expected) <= tolerance,
              what + $" (mong đợi {expected:0.###} ±{tolerance:0.###}, thực tế {actual:0.###})");
    }

    private static void CheckState(BoatState actual, BoatState expected, string what)
    {
        Check(actual == expected, what + $" (mong đợi {expected}, thực tế {actual})");
    }

    // ─── Tiện ích dựng dữ liệu test ──────────────────────────────────────

    /// <summary>Mốc giờ cố định cho test (2026-08-29 08:00:00 UTC) — deterministic, không dùng DateTime.UtcNow.</summary>
    private static readonly long T0 = new DateTime(2026, 8, 29, 8, 0, 0, DateTimeKind.Utc).Ticks;

    private const double Travel   = 20.0;   // giây chạy 1 chiều (fallbackTravelSeconds mặc định)
    private const double GapOne   = 300.0;  // 5 phút — config gapOneDockMinutes
    private const double GapTwo   = 420.0;  // 7 phút  — config gapTwoDockMinutes (đúng 2 bến mở)
    private const double GapMulti = 600.0;  // 10 phút — config gapMultiDockMinutes (đủ 3 bến)
    private const double Stagger  = 180.0;  // 3 phút — config minStaggerMinutes
    private const double MaxDock  = 1800.0; // 30 phút — giá trị TEST cho lưới an toàn (config thật
                                            // maxDockMinutes = 35 phút; lõi nhận tham số nên số nào cũng chạy)

    private static long Sec(double s) => BoatScheduleCore.SecondsToTicks(s);

    private static DockScheduleState Waiting(long arrivalTicks)
    {
        DockScheduleState s;
        s.State               = BoatState.WaitingNext;
        s.AnchorUtcTicks      = arrivalTicks;
        s.NextArrivalUtcTicks = 0L;
        return s;
    }

    private static DockScheduleState Arriving(long arrivalTicks)
    {
        DockScheduleState s = Waiting(arrivalTicks);
        s.State = BoatState.Arriving;
        return s;
    }

    private static DockScheduleState Docked(long dockedAtTicks)
    {
        DockScheduleState s;
        s.State               = BoatState.Docked;
        s.AnchorUtcTicks      = dockedAtTicks;
        s.NextArrivalUtcTicks = 0L;
        return s;
    }

    private static DockScheduleState Departing(long departAtTicks, long nextArrivalTicks)
    {
        DockScheduleState s;
        s.State               = BoatState.Departing;
        s.AnchorUtcTicks      = departAtTicks;
        s.NextArrivalUtcTicks = nextArrivalTicks;
        return s;
    }

    // ─── Main ────────────────────────────────────────────────────────────

    public static int Main()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  BoatScheduleCore V2 — unit test (event-driven, BOAT-002)    ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        TestGapSelection();
        TestScheduleNextArrival();
        TestStagger();
        TestResolveWaitingAndArriving();
        TestResolveDockedIsAbsorbing();
        TestResolveDepartingOffline();
        TestOfflineLongChain();
        TestClockRollback();
        TestReportVisitorsAllAboard();
        TestDoubleFireGuards();
        TestDockTimeoutSafetyNet();
        TestQueryPhaseProgress();
        TestMiscHelpers();

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  TỔNG KẾT: {_pass} PASS · {_fail} FAIL");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        return _fail == 0 ? 0 : 1;
    }

    // ─── A. Gap 5 phút / 10 phút ─────────────────────────────────────────

    private static void TestGapSelection()
    {
        Group("A. Chọn gap theo số bến đang mở — BA MỨC 5/7/10 (Lead chốt 2026-08-29)");

        CheckNear(BoatScheduleCore.SelectGapSeconds(1, GapOne, GapTwo, GapMulti), GapOne, 0.001,
                  "1 bến mở → gap 5 phút");
        CheckNear(BoatScheduleCore.SelectGapSeconds(0, GapOne, GapTwo, GapMulti), GapOne, 0.001,
                  "0 bến (biên) → vẫn dùng gap 1 bến");
        CheckNear(BoatScheduleCore.SelectGapSeconds(2, GapOne, GapTwo, GapMulti), GapTwo, 0.001,
                  "ĐÚNG 2 bến mở → gap 7 phút (KHÔNG nhảy thẳng lên 10)");
        CheckNear(BoatScheduleCore.SelectGapSeconds(3, GapOne, GapTwo, GapMulti), GapMulti, 0.001,
                  "đủ 3 bến mở → gap 10 phút (mốc Sếp chốt)");
        CheckNear(BoatScheduleCore.SelectGapSeconds(4, GapOne, GapTwo, GapMulti), GapMulti, 0.001,
                  "hơn 3 bến (biên, không xảy ra) → vẫn gap 10 phút");
        CheckNear(BoatScheduleCore.SelectGapSeconds(1, -5.0, GapTwo, GapMulti), 0.0, 0.001,
                  "gap âm trong config bị kẹp về 0");
        CheckNear(BoatScheduleCore.SelectGapSeconds(2, GapOne, -5.0, GapMulti), 0.0, 0.001,
                  "gap 2 bến âm cũng bị kẹp về 0");

        // Nhịp "có tàu cập bến" trên toàn bờ = gap / số bến — phải ổn định, không dồn dập.
        CheckNear(GapOne / 1.0, 300.0, 0.001, "1 bến: cứ 5 phút có 1 tàu vào bờ");
        CheckNear(GapTwo / 2.0, 210.0, 0.001, "2 bến: cứ 3,5 phút có 1 tàu vào bờ (để 5 phút thì chỉ 2,5 phút — quá dồn)");
        CheckNear(GapMulti / 3.0, 200.0, 0.001, "3 bến: cứ ~3,3 phút có 1 tàu vào bờ");

        // Overload 2 mức cũ vẫn phải chạy (code ngoài có thể còn gọi).
        CheckNear(BoatScheduleCore.SelectGapSeconds(2, GapOne, GapMulti), GapMulti, 0.001,
                  "[compat] overload 2 mức: ≥2 bến → gap multi");
    }

    private static void TestScheduleNextArrival()
    {
        Group("A2. arrival kế = lúc rời bến + gap");

        long arrival1 = BoatScheduleCore.ScheduleNextArrival(T0, GapOne, Travel, Stagger, null, 0);
        CheckEqual(arrival1, T0 + Sec(GapOne), "1 bến: arrival = departure + 5 phút chính xác");

        long arrival2 = BoatScheduleCore.ScheduleNextArrival(T0, GapMulti, Travel, Stagger, null, 0);
        CheckEqual(arrival2, T0 + Sec(GapMulti), "nhiều bến: arrival = departure + 10 phút chính xác");

        // Sàn kỹ thuật: gap phải ≥ 2×travel + 1s để tàu kịp lùi ra rồi chạy vào lại.
        long arrivalTiny = BoatScheduleCore.ScheduleNextArrival(T0, 1.0, Travel, 0.0, null, 0);
        Check(arrivalTiny >= T0 + Sec(2.0 * Travel),
              "config gap dị (1 giây) bị kẹp lên sàn 2×travel — tàu không teleport");
    }

    // ─── B. Luật so le ───────────────────────────────────────────────────

    private static void TestStagger()
    {
        Group("B. So le ≥3 phút giữa 2 arrival bất kỳ (dời MUỘN)");

        long other = T0 + Sec(600.0);            // bến khác cập bến ở phút thứ 10
        long[] others = { other, 0L, 0L };

        // Trường hợp xung đột: mong muốn cập bến chỉ sau bến kia 60 giây.
        long desired  = other + Sec(60.0);
        long resolved = BoatScheduleCore.ResolveStaggeredArrival(desired, Stagger, others, 1);
        CheckEqual(resolved, other + Sec(Stagger), "xung đột phía sau → dời tới đúng mốc + 3 phút");
        Check(resolved > desired, "chỉ dời MUỘN, không bao giờ kéo sớm hơn");

        // Xung đột phía trước (mong muốn tới TRƯỚC bến kia 60 giây) → cũng dời muộn.
        long desiredBefore  = other - Sec(60.0);
        long resolvedBefore = BoatScheduleCore.ResolveStaggeredArrival(desiredBefore, Stagger, others, 1);
        CheckEqual(resolvedBefore, other + Sec(Stagger), "xung đột phía trước → vẫn dời ra SAU 3 phút");

        // Không xung đột → giữ nguyên.
        long far = other + Sec(Stagger + 30.0);
        CheckEqual(BoatScheduleCore.ResolveStaggeredArrival(far, Stagger, others, 1), far,
                   "cách nhau đủ 3 phút → giữ nguyên arrival");

        // Đúng 3 phút (biên) → hợp lệ, không dời.
        long exact = other + Sec(Stagger);
        CheckEqual(BoatScheduleCore.ResolveStaggeredArrival(exact, Stagger, others, 1), exact,
                   "cách đúng 3 phút (biên) → hợp lệ, giữ nguyên");

        // Hai bến khác cùng chen: phải né CẢ HAI (lặp tới khi hội tụ).
        long[] two = { other, other + Sec(Stagger), 0L };
        long crowded = BoatScheduleCore.ResolveStaggeredArrival(other + Sec(30.0), Stagger, two, 2);
        Check(Math.Abs(crowded - two[0]) >= Sec(Stagger) && Math.Abs(crowded - two[1]) >= Sec(Stagger),
              "chen giữa 2 bến → arrival cuối cùng cách CẢ HAI ≥ 3 phút");

        // Phần tử 0 = bến không có arrival sắp tới (đang Docked) → bỏ qua, không kéo lịch.
        long[] withZero = { 0L, 0L, 0L };
        CheckEqual(BoatScheduleCore.ResolveStaggeredArrival(desired, Stagger, withZero, 3), desired,
                   "bến đang đậu (arrival = 0) không tham gia so le");

        // 3 bến thật: tất cả cách nhau ≥ 3 phút sau khi giải.
        long a0 = T0 + Sec(GapMulti);
        long a1 = BoatScheduleCore.ResolveStaggeredArrival(a0 + Sec(30.0), Stagger, new[] { a0, 0L, 0L }, 1);
        long a2 = BoatScheduleCore.ResolveStaggeredArrival(a0 + Sec(45.0), Stagger, new[] { a0, a1, 0L }, 2);
        Check(Math.Abs(a1 - a0) >= Sec(Stagger) &&
              Math.Abs(a2 - a0) >= Sec(Stagger) &&
              Math.Abs(a2 - a1) >= Sec(Stagger),
              "3 bến mở cùng lúc → mọi cặp arrival cách nhau ≥ 3 phút (AC §8.4)");
    }

    // ─── C. Resolve offline mọi pha ──────────────────────────────────────

    private static void TestResolveWaitingAndArriving()
    {
        Group("C1. Resolve pha WaitingNext / Arriving");

        long arrival = T0 + Sec(GapOne);
        DockScheduleState waiting = Waiting(arrival);

        // Còn xa arrival → vẫn WaitingNext, không đổi gì.
        var r1 = BoatScheduleCore.ResolveDock(waiting, T0, Travel);
        CheckState(r1.State.State, BoatState.WaitingNext, "trước giờ chạy vào → giữ WaitingNext");
        Check(!r1.Changed, "không đổi state → Changed = false (không cần persist)");
        Check(!r1.JustDocked, "chưa chạm bến → JustDocked = false");

        // Chạm mốc arrival − travel → chuyển Arriving.
        var r2 = BoatScheduleCore.ResolveDock(waiting, arrival - Sec(Travel), Travel);
        CheckState(r2.State.State, BoatState.Arriving, "đúng mốc arrival − travel → Arriving");
        Check(r2.Changed, "chuyển sang Arriving → Changed = true");
        CheckEqual(r2.State.AnchorUtcTicks, arrival, "anchor giữ nguyên = arrivalUtc khi Arriving");

        // Giữa đường vào bến.
        var r3 = BoatScheduleCore.ResolveDock(waiting, arrival - Sec(Travel / 2.0), Travel);
        CheckState(r3.State.State, BoatState.Arriving, "giữa đường vào bến → Arriving");

        // Vượt arrival (offline lúc tàu đang chạy vào) → Docked + JustDocked.
        var r4 = BoatScheduleCore.ResolveDock(waiting, arrival + Sec(3.0), Travel);
        CheckState(r4.State.State, BoatState.Docked, "vượt arrival → Docked");
        Check(r4.JustDocked, "chạm bến trong lần resolve này → JustDocked = true");
        CheckEqual(r4.State.AnchorUtcTicks, arrival, "anchor Docked = đúng giờ chạm bến (arrival), không phải now");

        // Resolve từ pha Arriving đang lưu (tắt game giữa lúc tàu chạy vào).
        var r5 = BoatScheduleCore.ResolveDock(Arriving(arrival), arrival + Sec(3600.0), Travel);
        CheckState(r5.State.State, BoatState.Docked, "offline 1 tiếng khi đang Arriving → Docked");
        Check(r5.JustDocked, "cú chạm bến bị lỡ lúc offline vẫn báo JustDocked đúng 1 lần");

        // Đồng hồ lùi NHẸ khi đang Arriving → quay về WaitingNext, giữ nguyên lịch.
        var r6 = BoatScheduleCore.ResolveDock(Arriving(arrival), arrival - Sec(Travel + 30.0), Travel);
        CheckState(r6.State.State, BoatState.WaitingNext, "đồng hồ lùi nhẹ khi Arriving → lùi về WaitingNext");
        CheckEqual(r6.State.AnchorUtcTicks, arrival, "lùi nhẹ KHÔNG đổi giờ cập bến đã hẹn");
    }

    private static void TestResolveDockedIsAbsorbing()
    {
        Group("C2. Docked là pha VÔ HẠN — chỉ thoát bằng lệnh");

        DockScheduleState docked = Docked(T0);

        var r1 = BoatScheduleCore.ResolveDock(docked, T0 + Sec(60.0), Travel);
        CheckState(r1.State.State, BoatState.Docked, "sau 1 phút vẫn Docked");
        Check(!r1.Changed && !r1.JustDocked, "Docked resolve lại: không đổi, không bắn JustDocked lần hai");

        var r2 = BoatScheduleCore.ResolveDock(docked, T0 + Sec(86400.0), Travel);
        CheckState(r2.State.State, BoatState.Docked, "offline 24 tiếng ở pha Docked → VẪN Docked (chờ Dev B)");
        Check(!r2.JustDocked, "load vào giữa pha Docked → KHÔNG bắn OnBoatDocked lần nữa (chống nhân đôi khách)");
    }

    private static void TestResolveDepartingOffline()
    {
        Group("C3. Resolve pha Departing");

        long depart  = T0;
        long nextArr = T0 + Sec(GapOne);
        DockScheduleState departing = Departing(depart, nextArr);

        // Đang lùi ra.
        var r1 = BoatScheduleCore.ResolveDock(departing, depart + Sec(Travel / 2.0), Travel);
        CheckState(r1.State.State, BoatState.Departing, "giữa lúc lùi ra → vẫn Departing");
        Check(!r1.Changed, "chưa lùi xong → không đổi state");

        // Lùi xong → WaitingNext với arrival đã hẹn.
        var r2 = BoatScheduleCore.ResolveDock(departing, depart + Sec(Travel + 1.0), Travel);
        CheckState(r2.State.State, BoatState.WaitingNext, "lùi xong → WaitingNext");
        CheckEqual(r2.State.AnchorUtcTicks, nextArr, "WaitingNext mang đúng arrival đã lên lịch lúc rời bến");
        CheckEqual(r2.State.NextArrivalUtcTicks, 0L, "NextArrival được dọn về 0 khi rời khỏi pha Departing");
        Check(!r2.JustDocked, "mới lùi xong, chưa tới giờ → chưa JustDocked");

        // NextArrival hỏng (0) → phòng thủ, không kẹt tàu vĩnh viễn.
        var r3 = BoatScheduleCore.ResolveDock(Departing(depart, 0L), depart + Sec(Travel + 1.0), Travel);
        CheckState(r3.State.State, BoatState.WaitingNext, "NextArrival hỏng → vẫn thoát Departing (phòng thủ)");
        Check(r3.State.AnchorUtcTicks > 0L, "NextArrival hỏng → tự đặt arrival hợp lệ thay vì 0");
    }

    private static void TestOfflineLongChain()
    {
        Group("C4. Tắt game lâu — tua chuỗi nhiều pha trong 1 lần resolve");

        long depart  = T0;
        long nextArr = T0 + Sec(GapOne);

        // Tắt game lúc tàu vừa rời bến, mở lại sau 2 tiếng: Departing → WaitingNext → Docked.
        var r = BoatScheduleCore.ResolveDock(Departing(depart, nextArr), depart + Sec(7200.0), Travel);
        CheckState(r.State.State, BoatState.Docked, "offline 2 tiếng từ pha Departing → tàu đã cập bến, đang đậu");
        Check(r.JustDocked, "chuỗi offline vẫn báo JustDocked ĐÚNG 1 LẦN");
        CheckEqual(r.State.AnchorUtcTicks, nextArr, "giờ chạm bến = arrival đã hẹn (không trôi theo lúc mở game)");

        // Resolve lại lần nữa (frame kế) — không được bắn lần hai.
        var again = BoatScheduleCore.ResolveDock(r.State, depart + Sec(7200.0), Travel);
        Check(!again.JustDocked, "resolve lại ngay sau đó → KHÔNG bắn JustDocked lần hai");
        Check(!again.Changed, "state đã ổn định → không cần persist lại");
    }

    // ─── D. Đồng hồ lùi ──────────────────────────────────────────────────

    private static void TestClockRollback()
    {
        Group("D. Guard đồng hồ lùi (reset khi lùi quá 1 gap)");

        // Horizon manager dùng: gap + stagger×3 + travel×2 + 60s
        double horizon = GapOne + Stagger * 3.0 + Travel * 2.0 + 60.0;

        DockScheduleState waiting = Waiting(T0 + Sec(GapOne));
        Check(!BoatScheduleCore.IsScheduleImplausiblyFuture(waiting, T0, horizon),
              "arrival trong tương lai 5 phút = HỢP LỆ (WaitingNext luôn hẹn tương lai)");

        // Người chơi chỉnh đồng hồ lùi 2 tiếng → mọi mốc vọt lên tương lai xa.
        long nowRolledBack = T0 - Sec(7200.0);
        Check(BoatScheduleCore.IsScheduleImplausiblyFuture(waiting, nowRolledBack, horizon),
              "đồng hồ lùi 2 tiếng → phát hiện là bất thường, manager reset lịch");

        // Lùi ít hơn horizon → coi như hợp lệ, không reset (không phạt oan người chơi).
        long nowSlight = T0 - Sec(60.0);
        Check(!BoatScheduleCore.IsScheduleImplausiblyFuture(waiting, nowSlight, horizon),
              "lệch nhẹ 1 phút → KHÔNG reset (dung sai trong horizon)");

        // Docked có anchor ở quá khứ → luôn hợp lệ.
        Check(!BoatScheduleCore.IsScheduleImplausiblyFuture(Docked(T0 - Sec(600.0)), T0, horizon),
              "Docked với mốc quá khứ → hợp lệ");
        Check(BoatScheduleCore.IsScheduleImplausiblyFuture(Docked(T0 + Sec(99999.0)), T0, horizon),
              "Docked mà mốc chạm bến ở tương lai xa → dữ liệu hỏng, cần reset");

        // Departing: cả anchor lẫn NextArrival đều bị soi.
        Check(BoatScheduleCore.IsScheduleImplausiblyFuture(Departing(T0, T0 + Sec(99999.0)), T0, horizon),
              "Departing có NextArrival tương lai xa → bất thường");

        // Sau reset: WaitingNext, tàu vào sau 30 giây.
        DockScheduleState fresh = BoatScheduleCore.MakeFreshWaiting(T0, BoatScheduleCore.FreshArrivalDelaySeconds);
        CheckState(fresh.State, BoatState.WaitingNext, "MakeFreshWaiting → WaitingNext");
        CheckEqual(fresh.AnchorUtcTicks, T0 + Sec(30.0), "chuyến mới cập bến sau đúng 30 giây (luật migrate V1→V2)");
        Check(!BoatScheduleCore.IsScheduleImplausiblyFuture(fresh, T0, horizon),
              "trạng thái sau reset là hợp lệ ngay (không reset lặp vô hạn)");
    }

    // ─── E. ReportVisitorsAllAboard → chuyển pha ─────────────────────────

    private static void TestReportVisitorsAllAboard()
    {
        Group("E. Khách lên tàu hết → Docked chuyển Departing + lên lịch chuyến kế");

        long dockedAt = T0;
        long allAboard = T0 + Sec(420.0); // 7 phút sau khi cập bến (khách được phục vụ xong)

        // 1 bến mở → gap 5 phút.
        DockScheduleState after;
        bool ok = BoatScheduleCore.TryBeginDeparture(
            Docked(dockedAt), allAboard,
            BoatScheduleCore.SelectGapSeconds(1, GapOne, GapTwo, GapMulti), Travel, Stagger,
            null, 0, out after);

        Check(ok, "đang Docked → nhận lệnh rời bến");
        CheckState(after.State, BoatState.Departing, "state chuyển Departing");
        CheckEqual(after.AnchorUtcTicks, allAboard, "mốc rời bến = đúng lúc khách cuối lên tàu");
        CheckEqual(after.NextArrivalUtcTicks, allAboard + Sec(GapOne), "1 bến: chuyến kế = rời bến + 5 phút");

        // 2 bến mở → gap 7 phút + né arrival bến khác.
        long otherArrival = allAboard + Sec(GapTwo) + Sec(60.0); // bến khác cập bến gần đó
        long[] others = { otherArrival, 0L, 0L };
        DockScheduleState after2;
        BoatScheduleCore.TryBeginDeparture(
            Docked(dockedAt), allAboard,
            BoatScheduleCore.SelectGapSeconds(2, GapOne, GapTwo, GapMulti), Travel, Stagger,
            others, 1, out after2);

        Check(Math.Abs(after2.NextArrivalUtcTicks - otherArrival) >= Sec(Stagger),
              "2 bến: chuyến kế bị dời cho cách arrival bến khác ≥ 3 phút");
        Check(after2.NextArrivalUtcTicks >= allAboard + Sec(GapTwo),
              "2 bến: chuyến kế không bao giờ SỚM hơn rời bến + 7 phút");

        // 3 bến mở → gap 10 phút, vẫn né arrival của CẢ HAI bến còn lại.
        long arr3a = allAboard + Sec(GapMulti) + Sec(45.0);
        long arr3b = allAboard + Sec(GapMulti) + Sec(200.0);
        long[] others3 = { arr3a, arr3b, 0L };
        DockScheduleState after3;
        BoatScheduleCore.TryBeginDeparture(
            Docked(dockedAt), allAboard,
            BoatScheduleCore.SelectGapSeconds(3, GapOne, GapTwo, GapMulti), Travel, Stagger,
            others3, 2, out after3);

        Check(after3.NextArrivalUtcTicks >= allAboard + Sec(GapMulti),
              "3 bến: chuyến kế không bao giờ SỚM hơn rời bến + 10 phút");
        Check(Math.Abs(after3.NextArrivalUtcTicks - arr3a) >= Sec(Stagger) &&
              Math.Abs(after3.NextArrivalUtcTicks - arr3b) >= Sec(Stagger),
              "3 bến: chuyến kế cách arrival của CẢ HAI bến kia ≥ 3 phút");

        // Vòng đời khép kín: Departing → WaitingNext → Arriving → Docked lại.
        long t = allAboard + Sec(Travel + 1.0);
        var back = BoatScheduleCore.ResolveDock(after, t, Travel);
        CheckState(back.State.State, BoatState.WaitingNext, "vòng đời: lùi xong về WaitingNext");

        var back2 = BoatScheduleCore.ResolveDock(back.State, after.NextArrivalUtcTicks - Sec(1.0), Travel);
        CheckState(back2.State.State, BoatState.Arriving, "vòng đời: tới giờ thì chạy vào bến");

        var back3 = BoatScheduleCore.ResolveDock(back2.State, after.NextArrivalUtcTicks, Travel);
        CheckState(back3.State.State, BoatState.Docked, "vòng đời: cập bến chuyến kế");
        Check(back3.JustDocked, "vòng đời: chuyến kế bắn JustDocked cho Dev B spawn khách mới");
    }

    // ─── F. Chống double-fire ────────────────────────────────────────────

    private static void TestDoubleFireGuards()
    {
        Group("F. Guard chống double-fire / gọi sai pha");

        DockScheduleState result;

        // Gọi ReportVisitorsAllAboard khi tàu KHÔNG đậu → từ chối, state nguyên vẹn.
        DockScheduleState waiting = Waiting(T0 + Sec(GapOne));
        Check(!BoatScheduleCore.TryBeginDeparture(waiting, T0, GapOne, Travel, Stagger, null, 0, out result),
              "gọi lúc WaitingNext → từ chối");
        Check(result.State == BoatState.WaitingNext && result.AnchorUtcTicks == waiting.AnchorUtcTicks,
              "bị từ chối thì state KHÔNG bị sửa");

        Check(!BoatScheduleCore.TryBeginDeparture(Arriving(T0 + Sec(10.0)), T0, GapOne, Travel, Stagger, null, 0, out result),
              "gọi lúc Arriving → từ chối");

        // Gọi 2 lần cho cùng 1 chuyến (Dev B lỡ gọi trùng): lần 2 phải bị chặn.
        DockScheduleState first;
        bool ok1 = BoatScheduleCore.TryBeginDeparture(Docked(T0), T0 + Sec(60.0), GapOne, Travel, Stagger, null, 0, out first);
        bool ok2 = BoatScheduleCore.TryBeginDeparture(first, T0 + Sec(61.0), GapOne, Travel, Stagger, null, 0, out result);
        Check(ok1, "lần gọi ĐẦU khi đang Docked → chấp nhận");
        Check(!ok2, "lần gọi THỨ HAI (đã Departing) → từ chối, không lên lịch chồng chuyến");
        CheckEqual(result.NextArrivalUtcTicks, first.NextArrivalUtcTicks,
                   "gọi trùng không làm đổi giờ chuyến kế");

        // JustDocked chỉ bắn 1 lần cho 1 cú chạm bến, dù resolve nhiều lần liên tiếp.
        long arrival = T0 + Sec(GapOne);
        DockScheduleState s = Waiting(arrival);
        int firedCount = 0;
        for (int frame = 0; frame < 10; frame++)
        {
            var r = BoatScheduleCore.ResolveDock(s, arrival + Sec(frame), Travel);
            s = r.State;
            if (r.JustDocked) firedCount++;
        }
        CheckEqual(firedCount, 1, "10 frame liên tiếp sau khi cập bến → JustDocked bắn đúng 1 lần");
    }

    // ─── H. Lưới an toàn chống kẹt tàu (QA B-1, Sếp duyệt) ───────────────

    private static void TestDockTimeoutSafetyNet()
    {
        Group("H. Lưới an toàn: đậu quá maxDockMinutes → ép rời bến");

        long dockedAt = T0;
        DockScheduleState docked = Docked(dockedAt);

        // Đếm giờ đậu bằng UTC tuyệt đối (offline vẫn chạy).
        CheckNear(BoatScheduleCore.DockedElapsedSeconds(docked, dockedAt + Sec(600.0)), 600.0, 0.001,
                  "đậu 10 phút → DockedElapsedSeconds = 600 giây");
        CheckNear(BoatScheduleCore.DockedElapsedSeconds(Waiting(T0), T0 + Sec(600.0)), 0.0, 0.001,
                  "không ở pha Docked → elapsed = 0");

        // Chưa quá hạn.
        Check(!BoatScheduleCore.IsDockTimedOut(docked, dockedAt + Sec(MaxDock - 1.0), MaxDock),
              "đậu 29:59 → CHƯA quá hạn, tàu vẫn chờ khách");
        // Đúng mốc 30 phút → quá hạn (biên tính là quá hạn).
        Check(BoatScheduleCore.IsDockTimedOut(docked, dockedAt + Sec(MaxDock), MaxDock),
              "đậu đúng 30 phút (biên) → quá hạn, kích lưới an toàn");
        Check(BoatScheduleCore.IsDockTimedOut(docked, dockedAt + Sec(86400.0), MaxDock),
              "offline cả ngày ở pha Docked → quá hạn ngay lúc load");

        // Pha khác không bao giờ bị lưới an toàn đụng tới.
        Check(!BoatScheduleCore.IsDockTimedOut(Waiting(T0 + Sec(GapOne)), T0 + Sec(86400.0), MaxDock),
              "WaitingNext → lưới an toàn không áp dụng");
        Check(!BoatScheduleCore.IsDockTimedOut(Arriving(T0 + Sec(10.0)), T0 + Sec(86400.0), MaxDock),
              "Arriving → lưới an toàn không áp dụng");
        Check(!BoatScheduleCore.IsDockTimedOut(Departing(T0, T0 + Sec(GapOne)), T0 + Sec(86400.0), MaxDock),
              "Departing → lưới an toàn không áp dụng");

        // Config đặt 0 = tắt lưới (tàu đậu vô hạn, đúng tinh thần event-driven thuần).
        Check(!BoatScheduleCore.IsDockTimedOut(docked, dockedAt + Sec(86400.0), 0.0),
              "maxDockMinutes = 0 → TẮT lưới an toàn, đậu bao lâu cũng được");
        Check(!BoatScheduleCore.IsDockTimedOut(docked, dockedAt + Sec(86400.0), -5.0),
              "maxDock âm (config dị) → coi như tắt, không ép rời");

        // Ép rời bến đi đúng đường TryBeginDeparture như khi khách lên tàu hết.
        long forcedAt = dockedAt + Sec(MaxDock);
        DockScheduleState forced;
        bool ok = BoatScheduleCore.TryBeginDeparture(
            docked, forcedAt, GapOne, Travel, Stagger, null, 0, out forced);
        Check(ok, "quá hạn → ép rời bến thành công");
        CheckState(forced.State, BoatState.Departing, "sau khi ép: state = Departing");
        CheckEqual(forced.NextArrivalUtcTicks, forcedAt + Sec(GapOne),
                   "chuyến kế sau khi ép rời vẫn = lúc rời bến + gap (không mất lịch)");

        // IDEMPOTENT: Dev B gọi ReportVisitorsAllAboard SAU khi đã bị ép rời → bỏ qua êm.
        DockScheduleState afterLate;
        Check(!BoatScheduleCore.TryBeginDeparture(forced, forcedAt + Sec(2.0), GapOne, Travel, Stagger, null, 0, out afterLate),
              "Dev B báo muộn sau khi bị ép rời → từ chối êm (idempotent)");
        CheckEqual(afterLate.NextArrivalUtcTicks, forced.NextArrivalUtcTicks,
                   "báo muộn KHÔNG dời giờ chuyến kế");
        CheckState(afterLate.State, BoatState.Departing, "báo muộn KHÔNG đổi pha");

        // Sau khi ép rời, không còn quá hạn nữa → manager không ép lần hai (không double-fire).
        Check(!BoatScheduleCore.IsDockTimedOut(forced, forcedAt + Sec(86400.0), MaxDock),
              "đã ép rời → không bao giờ kích lưới lần hai cho cùng chuyến");

        // Vòng đời tiếp tục bình thường sau khi bị ép: về WaitingNext rồi cập bến lại.
        var back = BoatScheduleCore.ResolveDock(forced, forcedAt + Sec(Travel + 1.0), Travel);
        CheckState(back.State.State, BoatState.WaitingNext, "sau khi bị ép rời: vòng đời chạy tiếp bình thường");
        var back2 = BoatScheduleCore.ResolveDock(back.State, forced.NextArrivalUtcTicks, Travel);
        CheckState(back2.State.State, BoatState.Docked, "chuyến kế sau lần bị ép vẫn cập bến đúng giờ");
        Check(back2.JustDocked, "chuyến kế bắn JustDocked đúng 1 lần → Dev B spawn khách mới");

        // Chuyến mới đếm lại từ đầu (mốc đậu mới), không bị ép ngay lập tức.
        Check(!BoatScheduleCore.IsDockTimedOut(back2.State, forced.NextArrivalUtcTicks + Sec(60.0), MaxDock),
              "chuyến mới đếm giờ đậu lại từ 0, không kế thừa quá hạn của chuyến trước");
    }

    // ─── G. Tiến độ hiển thị + helper ────────────────────────────────────

    private static void TestQueryPhaseProgress()
    {
        Group("G1. QueryPhase — tiến độ 0-1 cho controller");

        long arrival = T0 + Sec(GapOne);
        DockScheduleState waiting = Waiting(arrival);

        BoatPhaseInfo p0 = BoatScheduleCore.QueryPhase(waiting, arrival - Sec(Travel), Travel);
        CheckState(p0.State, BoatState.Arriving, "đúng lúc bắt đầu chạy vào → Arriving");
        CheckNear(p0.Progress, 0.0, 0.001, "Arriving đầu path → progress 0 (ở điểm mù)");

        BoatPhaseInfo pHalf = BoatScheduleCore.QueryPhase(waiting, arrival - Sec(Travel / 2.0), Travel);
        CheckNear(pHalf.Progress, 0.5, 0.01, "Arriving giữa đường → progress 0.5");

        BoatPhaseInfo pDock = BoatScheduleCore.QueryPhase(waiting, arrival, Travel);
        CheckState(pDock.State, BoatState.Docked, "đúng giờ arrival → Docked");
        CheckNear(pDock.DockedRemainingSeconds, -1.0, 0.001,
                  "V2: Docked vô hạn → DockedRemainingSeconds = -1 (UI không hiện countdown)");

        BoatPhaseInfo pWait = BoatScheduleCore.QueryPhase(waiting, T0, Travel);
        CheckState(pWait.State, BoatState.WaitingNext, "còn xa → WaitingNext");
        CheckNear(pWait.PhaseSeconds, GapOne, 0.01, "WaitingNext.PhaseSeconds = giây CÒN LẠI tới arrival");

        // Departing: progress 0 = berth, 1 = điểm mù.
        DockScheduleState dep = Departing(T0, T0 + Sec(GapOne));
        CheckNear(BoatScheduleCore.QueryPhase(dep, T0, Travel).Progress, 0.0, 0.001,
                  "Departing bắt đầu → progress 0 (còn ở berth)");
        CheckNear(BoatScheduleCore.QueryPhase(dep, T0 + Sec(Travel / 2.0), Travel).Progress, 0.5, 0.01,
                  "Departing giữa chừng → progress 0.5");

        // Vào game giữa pha bất kỳ: QueryPhase idempotent (gọi 2 lần cùng now → cùng kết quả).
        BoatPhaseInfo a = BoatScheduleCore.QueryPhase(waiting, arrival - Sec(5.0), Travel);
        BoatPhaseInfo b = BoatScheduleCore.QueryPhase(waiting, arrival - Sec(5.0), Travel);
        Check(a.State == b.State && Math.Abs(a.Progress - b.Progress) < 1e-9,
              "QueryPhase thuần: cùng input → cùng output (idempotent khi reload)");
    }

    private static void TestMiscHelpers()
    {
        Group("G2. Helper: arrival sắp tới + phút làm tròn cho popup");

        long arrival = T0 + Sec(GapOne);
        CheckEqual(BoatScheduleCore.UpcomingArrivalUtcTicks(Waiting(arrival)), arrival,
                   "WaitingNext → arrival sắp tới = anchor");
        CheckEqual(BoatScheduleCore.UpcomingArrivalUtcTicks(Arriving(arrival)), arrival,
                   "Arriving → arrival sắp tới = anchor");
        CheckEqual(BoatScheduleCore.UpcomingArrivalUtcTicks(Docked(T0)), 0L,
                   "Docked → chưa có chuyến kế (đang chờ khách) = 0");
        CheckEqual(BoatScheduleCore.UpcomingArrivalUtcTicks(Departing(T0, arrival)), arrival,
                   "Departing → arrival sắp tới = NextArrival đã hẹn");

        CheckEqual(BoatScheduleCore.RoundedWaitMinutes(T0, T0 + Sec(300.0)), 5,
                   "popup 1 bến: 'cập bến sau 5 phút'");
        CheckEqual(BoatScheduleCore.RoundedWaitMinutes(T0, T0 + Sec(600.0)), 10,
                   "popup nhiều bến: 'cập bến sau 10 phút'");
        CheckEqual(BoatScheduleCore.RoundedWaitMinutes(T0, T0 + Sec(89.0)), 1,
                   "89 giây → làm tròn 1 phút");
        CheckEqual(BoatScheduleCore.RoundedWaitMinutes(T0, T0 + Sec(91.0)), 2,
                   "91 giây → làm tròn 2 phút");
        CheckEqual(BoatScheduleCore.RoundedWaitMinutes(T0, T0 - Sec(60.0)), 0,
                   "arrival đã qua → 0 phút (không ra số âm)");

        // Đổi đơn vị.
        CheckEqual(BoatScheduleCore.SecondsToTicks(1.0), 10000000L, "1 giây = 10 triệu ticks");
        CheckNear(BoatScheduleCore.TicksToSeconds(Sec(42.5)), 42.5, 0.0001, "đổi ticks → giây khớp");

        // Điều kiện mở bến (giữ nguyên từ V1 — hồi quy).
        var req = new DockUnlockRequirement { RequiredLevel = 12, GoldCost = 2000, GemCost = 0 };
        Check(BoatScheduleCore.EvaluateUnlock(req, false, 12, 2000, 0) == UnlockDenyReason.None,
              "đủ level + đủ vàng → cho mở bến 2");
        Check(BoatScheduleCore.EvaluateUnlock(req, false, 11, 9999, 0) == UnlockDenyReason.LevelTooLow,
              "thiếu level → từ chối đúng lý do");
        Check(BoatScheduleCore.EvaluateUnlock(req, false, 12, 100, 0) == UnlockDenyReason.NotEnoughGold,
              "thiếu vàng → từ chối đúng lý do");
        Check(BoatScheduleCore.EvaluateUnlock(req, true, 99, 99999, 99) == UnlockDenyReason.AlreadyUnlocked,
              "bến đã mở → AlreadyUnlocked");
    }
}
