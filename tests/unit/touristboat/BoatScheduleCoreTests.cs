using System;
using System.Globalization;

// ═══════════════════════════════════════════════════════════════════════════
//  BoatScheduleCoreTests — QA harness cho BOAT-001 (chạy .NET thuần, không Unity)
//
//  Biên dịch:  mcs BoatScheduleCore.cs BoatScheduleCoreTests.cs -out:tests.exe
//  Chạy:       mono tests.exe
//
//  Cấu hình test khớp GDD §4 default: dockMinutes=40 (2400s), hideMinutes=15 (900s),
//  staggerMinutes=12 (720s). travelSeconds GIẢ ĐỊNH = 60s (core nhận travel qua
//  tham số — manager tính từ pathLength/speed, không ảnh hưởng logic thời gian).
//
//  Cycle = 900 + 60 + 2400 + 60 = 3420s. Các mốc pha:
//    [0,900)      Hidden
//    [900,960)    Arriving
//    [960,3360)   Docked
//    [3360,3420)  Departing
// ═══════════════════════════════════════════════════════════════════════════
public static class BoatScheduleCoreTests
{
    // ── Hằng số test (GDD default) ──────────────────────────────────────
    const double Hide    = 15 * 60;   // 900s
    const double Dock    = 40 * 60;   // 2400s
    const double Travel  = 60;        // giả định (path 18.000 unit / 300 u/s)
    const double Stagger = 12 * 60;   // 720s
    const double Cycle   = Hide + Travel + Dock + Travel; // 3420s
    const long   Tps     = BoatScheduleCore.TicksPerSecond;

    // Anchor gốc cố định — 2026-01-01 00:00:00 UTC
    static readonly long Anchor = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    // Requirement theo TouristBoatConfig.GetDockRequirement (giá GDD §4)
    static DockUnlockRequirement Req(int dock)
    {
        switch (dock)
        {
            case 0:  return new DockUnlockRequirement { RequiredLevel = 10, GoldCost = 0,    GemCost = 0  };
            case 1:  return new DockUnlockRequirement { RequiredLevel = 12, GoldCost = 2000, GemCost = 0  };
            case 2:  return new DockUnlockRequirement { RequiredLevel = 14, GoldCost = 0,    GemCost = 25 };
            default: return new DockUnlockRequirement { RequiredLevel = int.MaxValue, GoldCost = 0, GemCost = 0 };
        }
    }

    static int _pass, _fail;

    static void Check(string id, string what, object expected, object actual)
    {
        bool ok = Equals(expected, actual);
        if (ok) _pass++; else _fail++;
        Console.WriteLine("{0} [{1}] {2}  | expected={3}  actual={4}",
            ok ? "PASS" : "FAIL", id, what, expected, actual);
    }

    static void Close(string id, string what, double expected, double actual, double tol)
    {
        bool ok = Math.Abs(expected - actual) <= tol;
        if (ok) _pass++; else _fail++;
        Console.WriteLine("{0} [{1}] {2}  | expected={3:0.######} (±{4})  actual={5:0.######}",
            ok ? "PASS" : "FAIL", id, what, expected, tol, actual);
    }

    static BoatPhaseInfo Phase(double elapsedSeconds, double scale = 1.0)
    {
        long now = Anchor + (long)Math.Round(elapsedSeconds * Tps);
        return BoatScheduleCore.ComputePhase(now, Anchor, Dock, Hide, Travel, scale);
    }

    public static int Main()
    {
        Console.WriteLine("═══ BOAT-001 QA — BoatScheduleCore unit tests (GDD §8.8) ═══");
        Console.WriteLine("Config: hide=900s dock=2400s travel=60s stagger=720s cycle=3420s");
        Console.WriteLine();

        TestA_PlayerJourney_L1_L30();
        TestB_Lifecycle_And_Offline();
        TestC_Stagger();
        TestD_EdgeCases();
        TestE_Round2_Regression();

        Console.WriteLine();
        Console.WriteLine("═══ KẾT QUẢ: {0} PASS / {1} FAIL (tổng {2}) ═══", _pass, _fail, _pass + _fail);
        return _fail == 0 ? 0 : 1;
    }

    // =====================================================================
    //  A. MÔ PHỎNG HÀNH TRÌNH NGƯỜI CHƠI L1 → L30
    // =====================================================================
    static void TestA_PlayerJourney_L1_L30()
    {
        Console.WriteLine("── A. Hành trình người chơi L1→L30 ──");

        // A1: L1–L9 — mọi bến DENY vì level, kể cả 999.999 vàng + 999.999 gem
        bool a1 = true; string a1detail = "";
        for (int lvl = 1; lvl <= 9; lvl++)
            for (int d = 0; d < 3; d++)
            {
                var r = BoatScheduleCore.EvaluateUnlock(Req(d), false, lvl, 999999, 999999);
                if (r != UnlockDenyReason.LevelTooLow) { a1 = false; a1detail = $"L{lvl} dock{d} => {r}"; }
            }
        Check("A1", "L1–L9: 27 tổ hợp level×bến đều LevelTooLow (dù 999.999 vàng/gem)",
              "all LevelTooLow", a1 ? "all LevelTooLow" : a1detail);

        // A2: Lên L10 — bến 1 đủ điều kiện free; bến 2, 3 deny vì level
        Check("A2a", "L10 bến 1 (free)", UnlockDenyReason.None,
              BoatScheduleCore.EvaluateUnlock(Req(0), false, 10, 0, 0));
        Check("A2b", "L10 bến 2 (cần L12)", UnlockDenyReason.LevelTooLow,
              BoatScheduleCore.EvaluateUnlock(Req(1), false, 10, 999999, 0));
        Check("A2c", "L10 bến 3 (cần L14)", UnlockDenyReason.LevelTooLow,
              BoatScheduleCore.EvaluateUnlock(Req(2), false, 10, 0, 999999));

        // A3: bến 2 — ranh giới tiền & level
        Check("A3a", "L12 + 1.999 vàng → bến 2 DENY thiếu tiền", UnlockDenyReason.NotEnoughGold,
              BoatScheduleCore.EvaluateUnlock(Req(1), false, 12, 1999, 0));
        Check("A3b", "L12 + 2.000 vàng → bến 2 PASS", UnlockDenyReason.None,
              BoatScheduleCore.EvaluateUnlock(Req(1), false, 12, 2000, 0));
        Check("A3c", "L11 + 10.000 vàng → bến 2 DENY level (ưu tiên level trước tiền)", UnlockDenyReason.LevelTooLow,
              BoatScheduleCore.EvaluateUnlock(Req(1), false, 11, 10000, 0));

        // A4: bến 3 — ranh giới gem
        Check("A4a", "L14 + 24 gem → bến 3 DENY", UnlockDenyReason.NotEnoughGems,
              BoatScheduleCore.EvaluateUnlock(Req(2), false, 14, 0, 24));
        Check("A4b", "L14 + 25 gem → bến 3 PASS", UnlockDenyReason.None,
              BoatScheduleCore.EvaluateUnlock(Req(2), false, 14, 0, 25));

        // A5: L15→L30 — cả 3 bến đã mở: mọi đường mở lại đều AlreadyUnlocked
        //     (ổn định, không side effect — core static thuần, không giữ state)
        bool a5 = true; string a5detail = "";
        for (int lvl = 15; lvl <= 30; lvl++)
            for (int d = 0; d < 3; d++)
            {
                var r = BoatScheduleCore.EvaluateUnlock(Req(d), true, lvl, 999999, 999999);
                if (r != UnlockDenyReason.AlreadyUnlocked) { a5 = false; a5detail = $"L{lvl} dock{d} => {r}"; }
            }
        Check("A5", "L15→L30: 48 tổ hợp đều AlreadyUnlocked (không unlock/dialogue phát sinh)",
              "all AlreadyUnlocked", a5 ? "all AlreadyUnlocked" : a5detail);
        Console.WriteLine();
    }

    // =====================================================================
    //  B. VÒNG ĐỜI TÀU & OFFLINE CATCH-UP
    // =====================================================================
    static void TestB_Lifecycle_And_Offline()
    {
        Console.WriteLine("── B. Vòng đời tàu & offline catch-up ──");

        // B6: chuỗi trạng thái + kiểm tra CHÍNH XÁC tại biên ±1 giây
        Check("B6a",  "t=0        → Hidden (phase 0)",      BoatState.Hidden,    Phase(0).State);
        Check("B6b",  "t=899      → Hidden (biên -1s)",     BoatState.Hidden,    Phase(899).State);
        Check("B6c",  "t=900      → Arriving (đúng biên)",  BoatState.Arriving,  Phase(900).State);
        Close("B6d",  "t=900 progress = 0",                 0.0,  Phase(900).Progress, 1e-9);
        Check("B6e",  "t=901      → Arriving (biên +1s)",   BoatState.Arriving,  Phase(901).State);
        Close("B6f",  "t=930 progress = 0.5 (tuyến tính giữa path)", 0.5, Phase(930).Progress, 1e-9);
        Close("B6g",  "t=915 progress = 0.25 (tuyến tính)", 0.25, Phase(915).Progress, 1e-9);
        Close("B6h",  "t=945 progress = 0.75 (tuyến tính)", 0.75, Phase(945).Progress, 1e-9);
        Check("B6i",  "t=959      → Arriving (biên -1s)",   BoatState.Arriving,  Phase(959).State);
        Check("B6j",  "t=960      → Docked (đúng biên)",    BoatState.Docked,    Phase(960).State);
        Close("B6k",  "t=960 countdown = 2400s (40p đầy)",  2400.0, Phase(960).DockedRemainingSeconds, 1e-6);
        Close("B6l",  "t=961 countdown = 2399s (đếm xuống)",2399.0, Phase(961).DockedRemainingSeconds, 1e-6);
        Close("B6m",  "t=3359 countdown = 1s (biên -1s)",   1.0,    Phase(3359).DockedRemainingSeconds, 1e-6);
        Check("B6n",  "t=3359     → Docked (biên -1s)",     BoatState.Docked,    Phase(3359).State);
        Check("B6o",  "t=3360     → Departing (đúng biên)", BoatState.Departing, Phase(3360).State);
        Close("B6p",  "t=3360 progress = 0 (bắt đầu lùi)",  0.0,    Phase(3360).Progress, 1e-9);
        Check("B6q",  "t=3419     → Departing (biên -1s)",  BoatState.Departing, Phase(3419).State);
        Close("B6r",  "t=3419 progress = 59/60",            59.0/60.0, Phase(3419).Progress, 1e-9);
        Check("B6s",  "t=3420     → Hidden (khép chu kỳ)",  BoatState.Hidden,    Phase(3420).State);
        Close("B6t",  "t=3420 phase quay về 0",             0.0,    Phase(3420).PhaseSeconds, 1e-6);
        Check("B6u",  "t=3421     → Hidden (chu kỳ 2, +1s)",BoatState.Hidden,    Phase(3421).State);
        Check("B6v",  "t=2×3420+960 → Docked (lặp vô hạn, chu kỳ 3)", BoatState.Docked, Phase(2 * Cycle + 960).State);

        // B7: OFFLINE — tắt 3 ngày + 17 phút lẻ; và 5 mốc cố định (chọn trước, "seed cố định")
        //     Expected tính TAY: elapsed mod 3420 → tra bảng mốc pha.
        double off = 3 * 86400 + 17 * 60;              // 260220s ; 260220 mod 3420 = 300
        Check("B7a", "offline 3d+17p: elapsed 260220 mod 3420 = 300 → Hidden", BoatState.Hidden, Phase(off).State);
        Close("B7b", "offline 3d+17p: PhaseSeconds = 300", 300.0, Phase(off).PhaseSeconds, 1e-6);
        Close("B7c", "offline 3d+17p: progress trong Hidden = 300/900", 300.0 / 900.0, Phase(off).Progress, 1e-9);

        // 5 mốc định trước (tính tay, ghi cả phép chia lấy dư):
        //  e=123456 → mod = 336   → Hidden    (prog 336/900)
        //  e=999999 → mod = 1359  → Docked    (countdown 3360-1359 = 2001)
        //  e=345678 → mod = 258   → Hidden
        //  e=86401  → mod = 901   → Arriving  (prog 1/60)
        //  e=27359  → mod = 3419  → Departing (prog 59/60)
        Check("B7d", "e=123456 (mod 336) → Hidden",    BoatState.Hidden,    Phase(123456).State);
        Check("B7e", "e=999999 (mod 1359) → Docked",   BoatState.Docked,    Phase(999999).State);
        Close("B7f", "e=999999 countdown = 2001s",     2001.0, Phase(999999).DockedRemainingSeconds, 1e-6);
        Check("B7g", "e=345678 (mod 258) → Hidden",    BoatState.Hidden,    Phase(345678).State);
        Check("B7h", "e=86401 (mod 901) → Arriving",   BoatState.Arriving,  Phase(86401).State);
        Close("B7i", "e=86401 progress = 1/60",        1.0 / 60.0, Phase(86401).Progress, 1e-9);
        Check("B7j", "e=27359 (mod 3419) → Departing", BoatState.Departing, Phase(27359).State);
        Close("B7k", "e=27359 progress = 59/60",       59.0 / 60.0, Phase(27359).Progress, 1e-9);

        // B8: ĐỒNG HỒ LÙI — anchor nằm ở TƯƠNG LAI so với now
        long past = Anchor - 12345L * Tps; // now TRƯỚC anchor 12345s
        BoatPhaseInfo backInfo;
        bool noThrow = true;
        try { backInfo = BoatScheduleCore.ComputePhase(past, Anchor, Dock, Hide, Travel); }
        catch (Exception ex) { noThrow = false; backInfo = default(BoatPhaseInfo); Console.WriteLine("      EXCEPTION: " + ex.Message); }
        Check("B8a", "anchor>now: ComputePhase không exception", true, noThrow);
        Check("B8b", "anchor>now: state = Hidden (coi như chu kỳ mới)", BoatState.Hidden, backInfo.State);
        Check("B8c", "anchor>now: PhaseSeconds không âm", true, backInfo.PhaseSeconds >= 0.0);
        Check("B8d", "anchor>now: Progress không âm", true, backInfo.Progress >= 0.0);
        Check("B8e", "IsAnchorInFuture(now<anchor) = true", true, BoatScheduleCore.IsAnchorInFuture(past, Anchor));
        Check("B8f", "SanitizeAnchor(now, anchorTươngLai) = now", past, BoatScheduleCore.SanitizeAnchor(past, Anchor));
        Check("B8g", "SanitizeAnchor(now, anchorQuáKhứ) giữ nguyên anchor", Anchor,
              BoatScheduleCore.SanitizeAnchor(Anchor + 100 * Tps, Anchor));

        // B9: debugTimeScale — chu kỳ ngắn lại ĐÚNG TỈ LỆ.
        //     ComputePhase(t thực, scale=60) phải y hệt ComputePhase(t×60, scale=1).
        double[] realSecs = { 0, 5, 14.99, 15, 16, 55.99, 56, 56.5, 57, 100 };
        bool b9 = true; string b9detail = "";
        foreach (double r in realSecs)
        {
            var scaled  = Phase(r, 60.0);
            var normal  = Phase(r * 60.0, 1.0);
            if (scaled.State != normal.State || Math.Abs(scaled.Progress - normal.Progress) > 1e-6)
            { b9 = false; b9detail = $"r={r}: {scaled.State}/{scaled.Progress:0.####} vs {normal.State}/{normal.Progress:0.####}"; }
        }
        Check("B9a", "scale=60: 10 mốc thời gian thực khớp hệt scale=1 ở t×60",
              "identical", b9 ? "identical" : b9detail);
        Check("B9b", "scale=60: 15s thực = 900s game → Arriving", BoatState.Arriving, Phase(15, 60.0).State);
        Check("B9c", "scale=60: 57s thực = 3420s game → Hidden (hết 1 chu kỳ trong 57s thực)",
              BoatState.Hidden, Phase(57, 60.0).State);
        Check("B9d", "scale≤0 → coi như 1 (không chia 0)", Phase(1000, 1.0).State, Phase(1000, 0.0).State);
        Console.WriteLine();
    }

    // =====================================================================
    //  C. LỊCH SO LE (GDD §3.3)
    // =====================================================================
    static void TestC_Stagger()
    {
        Console.WriteLine("── C. Lịch so le ──");
        long staggerTicks = BoatScheduleCore.SecondsToTicks(Stagger);

        // Bối cảnh: bến 1 đã mở. now = thời điểm bấm mở bến 2.
        // Manager đặt desiredAnchor = now - hide → arrival dự kiến = now + travel (60s).
        long now = Anchor + 50000L * Tps;
        long desired = now - BoatScheduleCore.SecondsToTicks(Hide);

        // C10-case1: tàu bến 1 cập bến trong 3 phút nữa (<12p, đẩy nhẹ)
        {
            long arr1 = now + 180L * Tps; // bến 1 cập bến sau 3p
            var other = new[] { new BoatCycleSpec {
                AnchorUtcTicks = arr1 - BoatScheduleCore.SecondsToTicks(Hide + Travel),
                HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel } };

            long resolved = BoatScheduleCore.ResolveStaggeredAnchor(desired, Dock, Hide, Travel, Stagger, other, 1);
            long arr2 = BoatScheduleCore.FirstArrivalUtcTicks(new BoatCycleSpec {
                AnchorUtcTicks = resolved, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel });

            Check("C10a", "bến1 cập sau 3p: anchor bến 2 BỊ ĐẨY (resolved != desired)", true, resolved != desired);
            Close("C10b", "khoảng cách 2 lần cập bến = stagger (900s = 3p+12p sau now)",
                  Stagger, BoatScheduleCore.TicksToSeconds(Math.Abs(arr2 - arr1)), 0.001);
            Check("C10c", "anchor sau đẩy vẫn ≤ now (đẩy 12p−(12p−3p)=... ≤ hide 15p)", true, resolved <= now);
        }

        // C10-case2: tàu bến 1 cập bến trong 8 phút nữa (<12p, đẩy sâu)
        //   Core giải ĐÚNG (gap = 12p) nhưng anchor rơi vào TƯƠNG LAI (now+4p) —
        //   sau đó mô phỏng guard "đồng hồ lùi" của BoatDockManager.Update:
        //   IsAnchorInFuture → SanitizeAnchor(anchor=now) → so le BỊ PHÁ.
        {
            long arr1 = now + 480L * Tps; // bến 1 cập bến sau 8p
            var other = new[] { new BoatCycleSpec {
                AnchorUtcTicks = arr1 - BoatScheduleCore.SecondsToTicks(Hide + Travel),
                HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel } };

            long resolved = BoatScheduleCore.ResolveStaggeredAnchor(desired, Dock, Hide, Travel, Stagger, other, 1);
            long arr2 = BoatScheduleCore.FirstArrivalUtcTicks(new BoatCycleSpec {
                AnchorUtcTicks = resolved, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel });

            Close("C10d", "CORE: gap sau giải so le = đúng 12p (720s)",
                  Stagger, BoatScheduleCore.TicksToSeconds(Math.Abs(arr2 - arr1)), 0.001);
            Check("C10e", "CORE: anchor bị đẩy tới TƯƠNG LAI (now+240s) — về toán là hợp lệ",
                  true, BoatScheduleCore.IsAnchorInFuture(now, resolved));
            Close("C10f", "CORE: anchor tương lai đúng 240s (đẩy 19p > hide 15p)",
                  240.0, BoatScheduleCore.TicksToSeconds(resolved - now), 0.001);

            // ── MÔ PHỎNG BoatDockManager.Update frame kế — GUARD MỚI sau fix B-1
            //    (BoatDockManager.cs dòng ~156): IsClockRolledBack(now, anchor, cycle)
            //    có DUNG SAI 1 chu kỳ → anchor tương lai +240s KHÔNG bị coi là đồng hồ lùi.
            long nowNextFrame = now + Tps / 50; // +1 frame (20ms)
            long anchorAfterManagerGuard = BoatScheduleCore.IsClockRolledBack(nowNextFrame, resolved, Cycle)
                ? nowNextFrame
                : resolved;
            long arr2AfterGuard = BoatScheduleCore.FirstArrivalUtcTicks(new BoatCycleSpec {
                AnchorUtcTicks = anchorAfterManagerGuard, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel });
            double gapAfterGuard = BoatScheduleCore.TicksToSeconds(Math.Abs(arr2AfterGuard - arr1));

            Check("C10g", "MANAGER-SIM (guard mới B-1): anchor tương lai ≤ 1 cycle được GIỮ NGUYÊN",
                  true, anchorAfterManagerGuard == resolved);
            Close("C10h", "MANAGER-SIM (guard mới B-1): gap thực ≥ stagger 720s (AC §8.4)",
                  Stagger, gapAfterGuard, 0.001);
            Console.WriteLine("      → gap thực tế sau guard mới = {0:0}s ({1:0.#}p) — so le được BẢO TOÀN (vòng 1 FAIL: 480s)",
                              gapAfterGuard, gapAfterGuard / 60.0);
        }

        // C11: arrival bến 1 cách XA (20 phút > 12p) → anchor giữ nguyên, không đẩy oan
        {
            long arr1 = now + 1200L * Tps; // 20 phút
            var other = new[] { new BoatCycleSpec {
                AnchorUtcTicks = arr1 - BoatScheduleCore.SecondsToTicks(Hide + Travel),
                HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel } };

            long resolved = BoatScheduleCore.ResolveStaggeredAnchor(desired, Dock, Hide, Travel, Stagger, other, 1);
            Check("C11", "bến1 cập sau 20p (>12p): anchor GIỮ NGUYÊN (không đẩy oan)", desired, resolved);
        }

        // C12: mở cả 3 bến sát nhau (cách 30s) — mô phỏng đúng thứ tự manager:
        //      mỗi lần mở đưa các bến ĐÃ MỞ (anchor đã resolve) vào otherDocks.
        {
            long t0 = Anchor + 90000L * Tps;
            var specs = new BoatCycleSpec[3];

            long a0 = BoatScheduleCore.ResolveStaggeredAnchor(
                t0 - BoatScheduleCore.SecondsToTicks(Hide), Dock, Hide, Travel, Stagger, specs, 0);
            specs[0] = new BoatCycleSpec { AnchorUtcTicks = a0, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };

            long t1 = t0 + 30L * Tps;
            long a1 = BoatScheduleCore.ResolveStaggeredAnchor(
                t1 - BoatScheduleCore.SecondsToTicks(Hide), Dock, Hide, Travel, Stagger, specs, 1);
            specs[1] = new BoatCycleSpec { AnchorUtcTicks = a1, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };

            long t2 = t1 + 30L * Tps;
            long a2 = BoatScheduleCore.ResolveStaggeredAnchor(
                t2 - BoatScheduleCore.SecondsToTicks(Hide), Dock, Hide, Travel, Stagger, specs, 2);
            specs[2] = new BoatCycleSpec { AnchorUtcTicks = a2, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };

            long f0 = BoatScheduleCore.FirstArrivalUtcTicks(specs[0]);
            long f1 = BoatScheduleCore.FirstArrivalUtcTicks(specs[1]);
            long f2 = BoatScheduleCore.FirstArrivalUtcTicks(specs[2]);

            double g01 = BoatScheduleCore.TicksToSeconds(Math.Abs(f1 - f0));
            double g02 = BoatScheduleCore.TicksToSeconds(Math.Abs(f2 - f0));
            double g12 = BoatScheduleCore.TicksToSeconds(Math.Abs(f2 - f1));

            Check("C12a", "3 bến mở cách nhau 30s: gap(1,2) ≥ 720s", true, g01 >= Stagger - 0.001);
            Check("C12b", "3 bến mở cách nhau 30s: gap(1,3) ≥ 720s", true, g02 >= Stagger - 0.001);
            Check("C12c", "3 bến mở cách nhau 30s: gap(2,3) ≥ 720s", true, g12 >= Stagger - 0.001);
            Console.WriteLine("      → arrival lần đầu: bến1 +{0:0}s, bến2 +{1:0}s, bến3 +{2:0}s (so với lúc mở bến 1)",
                BoatScheduleCore.TicksToSeconds(f0 - t0), BoatScheduleCore.TicksToSeconds(f1 - t0),
                BoatScheduleCore.TicksToSeconds(f2 - t0));

            // Hội tụ / không vòng lặp vô hạn: gọi lại với input "kẹt" (stagger cực lớn hơn nửa chu kỳ)
            // → phải trả về trong thời gian hữu hạn (MaxStaggerIterations chặn), không treo.
            long unsat = BoatScheduleCore.ResolveStaggeredAnchor(
                t2 - BoatScheduleCore.SecondsToTicks(Hide), Dock, Hide, Travel,
                Cycle * 0.6 /* stagger 2052s > cycle/2 → vô nghiệm */, specs, 2);
            Check("C12d", "stagger > cycle/2 (vô nghiệm): hàm vẫn trả về (không treo/không loop vô hạn)",
                  true, unsat >= 0 || unsat < 0);

            // NOTE (không tính pass/fail): bến khác travelSeconds → chu kỳ lệch →
            // arrival trôi dần, sau N chu kỳ gap < stagger dù lúc mở đã giải đúng.
            double cyc60 = Hide + 60 + Dock + 60, cyc90 = Hide + 90 + Dock + 90; // lệch 60s/chu kỳ
            double driftCycles = (Stagger) / Math.Abs(cyc90 - cyc60);
            Console.WriteLine("      NOTE: nếu travel bến khác nhau (60s vs 90s) chu kỳ lệch {0}s → sau ~{1:0} chu kỳ "
                              + "(~{2:0.#} giờ chơi liên tục) khoảng so le bị bào mòn < 12p. Luật §3.3 chỉ enforce lúc MỞ BẾN.",
                              Math.Abs(cyc90 - cyc60), driftCycles, driftCycles * cyc90 / 3600.0);
        }
        Console.WriteLine();
    }

    // =====================================================================
    //  D. EDGE CASES GDD §5 còn lại
    // =====================================================================
    static void TestD_EdgeCases()
    {
        Console.WriteLine("── D. Edge cases §5 ──");

        // D1: nhảy cóc level qua 10 (L9 → L11, không bao giờ đứng ở đúng L10)
        Check("D1a", "L9 → bến 1 DENY", UnlockDenyReason.LevelTooLow,
              BoatScheduleCore.EvaluateUnlock(Req(0), false, 9, 0, 0));
        Check("D1b", "nhảy cóc L9→L11: bến 1 PASS (điều kiện là ≥, không phải ==)", UnlockDenyReason.None,
              BoatScheduleCore.EvaluateUnlock(Req(0), false, 11, 0, 0));
        Check("D1c", "L11 đã mở bến 1 → AlreadyUnlocked (intro không lặp về mặt điều kiện)",
              UnlockDenyReason.AlreadyUnlocked,
              BoatScheduleCore.EvaluateUnlock(Req(0), true, 11, 0, 0));

        // D2: determinism sau reload — cùng anchor + now cho kết quả Y HỆT
        //     (mô phỏng manager persist anchor dạng string invariant rồi parse lại)
        long now = Anchor + 987654321L; // lệch lẻ ticks (không tròn giây)
        var first = BoatScheduleCore.ComputePhase(now, Anchor, Dock, Hide, Travel);
        string persisted = Anchor.ToString(CultureInfo.InvariantCulture);           // như SaveDock
        long reloaded = long.Parse(persisted, NumberStyles.Integer, CultureInfo.InvariantCulture); // như LoadFromPrefs
        var second = BoatScheduleCore.ComputePhase(now, reloaded, Dock, Hide, Travel);
        bool identical = first.State == second.State
                      && first.Progress == second.Progress
                      && first.PhaseSeconds == second.PhaseSeconds
                      && first.DockedRemainingSeconds == second.DockedRemainingSeconds
                      && first.CycleSeconds == second.CycleSeconds;
        Check("D2a", "reload (persist→parse anchor): BoatPhaseInfo y hệt bit-một-bit", true, identical);

        bool deterministic = true;
        for (int i = 0; i < 1000; i++)
        {
            long t = Anchor + (long)(i * 7919L) * 1000003L; // trải nhiều mốc lẻ
            var p1 = BoatScheduleCore.ComputePhase(t, Anchor, Dock, Hide, Travel);
            var p2 = BoatScheduleCore.ComputePhase(t, Anchor, Dock, Hide, Travel);
            if (p1.State != p2.State || p1.Progress != p2.Progress || p1.PhaseSeconds != p2.PhaseSeconds)
            { deterministic = false; break; }
        }
        Check("D2b", "1000 mốc ngẫu-nhiên-định-trước: gọi 2 lần cho kết quả y hệt (stateless)", true, deterministic);

        // D3: travel suy biến (path chưa gắn waypoint) — không chia 0, không exception
        bool noThrow = true; BoatPhaseInfo degen = default(BoatPhaseInfo);
        try { degen = BoatScheduleCore.ComputePhase(now, Anchor, Dock, Hide, 0.0); }
        catch (Exception) { noThrow = false; }
        Check("D3a", "travelSeconds=0 (path suy biến): không exception (kẹp sàn 0.001s)", true, noThrow);
        Check("D3b", "travelSeconds=0: cycle vẫn > hide+dock", true, degen.CycleSeconds > Hide + Dock);
        Check("D3c", "input âm toàn bộ: ComputeCycleSeconds không trả ≤ 0", true,
              BoatScheduleCore.ComputeCycleSeconds(-5, -5, -5) > 0.0);

        // D4: dockIndex ngoài [0..2] — requirement kiểu int.MaxValue phải luôn từ chối
        Check("D4", "dock index sai (req level int.MaxValue): DENY LevelTooLow, không exception",
              UnlockDenyReason.LevelTooLow,
              BoatScheduleCore.EvaluateUnlock(Req(99), false, 30, 999999, 999999));

        // D5: NextArrival/NearestArrival — đứng ĐÚNG trên mốc arrival
        var spec = new BoatCycleSpec { AnchorUtcTicks = Anchor, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };
        long firstArr = BoatScheduleCore.FirstArrivalUtcTicks(spec);
        Check("D5a", "FirstArrival = anchor + 960s", Anchor + BoatScheduleCore.SecondsToTicks(Hide + Travel), firstArr);
        Check("D5b", "NextArrival(đúng-trên-mốc) = chính mốc đó", firstArr,
              BoatScheduleCore.NextArrivalUtcTicks(firstArr, spec));
        Check("D5c", "NextArrival(mốc+1s) = mốc + 1 chu kỳ", firstArr + BoatScheduleCore.SecondsToTicks(Cycle),
              BoatScheduleCore.NextArrivalUtcTicks(firstArr + Tps, spec));
        Check("D5d", "NearestArrival(trước firstArrival 10 ngày) = firstArrival (k không âm)", firstArr,
              BoatScheduleCore.NearestArrivalUtcTicks(firstArr - 864000L * Tps, spec));
        long mid = firstArr + BoatScheduleCore.SecondsToTicks(Cycle * 7.49);
        long near = BoatScheduleCore.NearestArrivalUtcTicks(mid, spec);
        Check("D5e", "NearestArrival(giữa 2 mốc, lệch 0.49 chu kỳ) chọn mốc gần hơn (k=7)",
              firstArr + BoatScheduleCore.SecondsToTicks(Cycle * 7), near);
        Console.WriteLine();
    }

    // =====================================================================
    //  E. REGRESSION VÒNG 2 — verify fix B-1 / m-3 theo yêu cầu lead
    // =====================================================================
    static void TestE_Round2_Regression()
    {
        Console.WriteLine("── E. Regression vòng 2 (fix B-1, m-3) ──");
        long now = Anchor + 500000L * Tps;

        // E1: biên IsClockRolledBack — dung sai ĐÚNG 1 chu kỳ
        Check("E1a", "anchor = now + đúng 1 cycle (3420s): KHÔNG coi là đồng hồ lùi (dung sai chạm biên)",
              false, BoatScheduleCore.IsClockRolledBack(now, now + BoatScheduleCore.SecondsToTicks(Cycle), Cycle));
        Check("E1b", "anchor = now + 1 cycle + 1s: LÀ đồng hồ lùi",
              true, BoatScheduleCore.IsClockRolledBack(now, now + BoatScheduleCore.SecondsToTicks(Cycle + 1), Cycle));
        Check("E1c", "anchor = now + 1 cycle + 1 tick: LÀ đồng hồ lùi (biên chặt)",
              true, BoatScheduleCore.IsClockRolledBack(now, now + BoatScheduleCore.SecondsToTicks(Cycle) + 1, Cycle));
        Check("E1d", "anchor quá khứ: không phải đồng hồ lùi",
              false, BoatScheduleCore.IsClockRolledBack(now, now - 12345L * Tps, Cycle));
        Check("E1e", "anchor tương lai 240s (so le đẩy, kịch bản B-1): KHÔNG coi là đồng hồ lùi",
              false, BoatScheduleCore.IsClockRolledBack(now, now + 240L * Tps, Cycle));
        Check("E1f", "cycleSeconds âm (rác): kẹp về 0 — anchor tương lai 1 tick vẫn bị coi là lùi, không exception",
              true, BoatScheduleCore.IsClockRolledBack(now, now + 1, -100.0));

        // E2: kịch bản B-1 đầy đủ ở tầng schedule — mở bến 2 khi bến 1 sắp cập
        //     bến ở NHIỀU mốc trong cửa sổ <12p; sau resolve + guard mới: gap ≥ stagger
        //     và anchor không bao giờ bị guard đụng tới.
        {
            long desired = now - BoatScheduleCore.SecondsToTicks(Hide);
            double[] arriveInMinutes = { 1, 3, 5, 8, 10, 11.9 }; // phủ cả nhánh đẩy nhẹ lẫn đẩy sâu
            bool allOk = true; string detail = "";
            foreach (double m in arriveInMinutes)
            {
                long arr1 = now + BoatScheduleCore.SecondsToTicks(m * 60.0);
                var other = new[] { new BoatCycleSpec {
                    AnchorUtcTicks = arr1 - BoatScheduleCore.SecondsToTicks(Hide + Travel),
                    HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel } };

                long resolved = BoatScheduleCore.ResolveStaggeredAnchor(desired, Dock, Hide, Travel, Stagger, other, 1);

                // Guard mới của manager (Update + LoadFromPrefs đều dùng IsClockRolledBack)
                if (BoatScheduleCore.IsClockRolledBack(now, resolved, Cycle))
                { allOk = false; detail = $"arriveIn={m}p: anchor bị guard reset oan"; break; }

                long arr2 = BoatScheduleCore.FirstArrivalUtcTicks(new BoatCycleSpec {
                    AnchorUtcTicks = resolved, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel });
                double gap = BoatScheduleCore.TicksToSeconds(Math.Abs(arr2 - arr1));
                if (gap < Stagger - 0.001)
                { allOk = false; detail = $"arriveIn={m}p: gap={gap:0}s < 720s"; break; }
            }
            Check("E2a", "B-1 fix: 6 mốc 'bến 1 sắp cập trong <12p' — gap luôn ≥ 720s, guard không reset oan",
                  "all >= stagger", allOk ? "all >= stagger" : detail);
        }

        // E2b: chặn trên độ đẩy so le — worst case 2 bến khác (chu kỳ đồng nhất m-3):
        //      anchor sau resolve không bao giờ vượt now quá 1 cycle → guard mới
        //      KHÔNG BAO GIỜ đụng anchor hợp lệ do so le tạo ra.
        {
            long desired = now - BoatScheduleCore.SecondsToTicks(Hide);
            bool allOk = true; string detail = "";
            // quét cặp arrival của 2 bến khác quanh cửa sổ xung đột, bước 90s
            for (double m1 = 0.5; m1 <= 12 && allOk; m1 += 1.5)
            for (double m2 = m1; m2 <= 36 && allOk; m2 += 1.5)
            {
                var others = new[] {
                    new BoatCycleSpec { AnchorUtcTicks = now + BoatScheduleCore.SecondsToTicks(m1*60) - BoatScheduleCore.SecondsToTicks(Hide+Travel), HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel },
                    new BoatCycleSpec { AnchorUtcTicks = now + BoatScheduleCore.SecondsToTicks(m2*60) - BoatScheduleCore.SecondsToTicks(Hide+Travel), HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel },
                };
                long resolved = BoatScheduleCore.ResolveStaggeredAnchor(desired, Dock, Hide, Travel, Stagger, others, 2);
                if (BoatScheduleCore.IsClockRolledBack(now, resolved, Cycle))
                { allOk = false; detail = $"m1={m1} m2={m2}: anchor vượt now quá 1 cycle"; }
            }
            Check("E2b", "worst-case so le với 2 bến khác (quét ~200 tổ hợp): anchor ≤ now + 1 cycle (guard an toàn)",
                  "never flagged", allOk ? "never flagged" : detail);
        }

        // E3: m-3 — chu kỳ ĐỒNG NHẤT (schedule travel = max 3 bến): so le giữ VĨNH VIỄN.
        //     2 bến mở cách 30s, cùng travel → kiểm gap tại arrival thứ k = 0, 100, 1000:
        //     phải y hệt gap ban đầu (không trôi 1 giây nào).
        {
            long t0 = now;
            var specs = new BoatCycleSpec[2];
            long a0 = t0 - BoatScheduleCore.SecondsToTicks(Hide);
            specs[0] = new BoatCycleSpec { AnchorUtcTicks = a0, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };
            long a1 = BoatScheduleCore.ResolveStaggeredAnchor(
                t0 + 30L * Tps - BoatScheduleCore.SecondsToTicks(Hide), Dock, Hide, Travel, Stagger, specs, 1);
            specs[1] = new BoatCycleSpec { AnchorUtcTicks = a1, HideSeconds = Hide, DockSeconds = Dock, TravelSeconds = Travel };

            long f0 = BoatScheduleCore.FirstArrivalUtcTicks(specs[0]);
            long f1 = BoatScheduleCore.FirstArrivalUtcTicks(specs[1]);
            long cycleTicks = BoatScheduleCore.SecondsToTicks(Cycle);
            double gap0    = BoatScheduleCore.TicksToSeconds(Math.Abs(f1 - f0));
            double gap100  = BoatScheduleCore.TicksToSeconds(Math.Abs((f1 + 100 * cycleTicks) - (f0 + 100 * cycleTicks)));
            double gap1000 = BoatScheduleCore.TicksToSeconds(Math.Abs((f1 + 1000 * cycleTicks) - (f0 + 1000 * cycleTicks)));
            Check("E3a", "m-3: gap ban đầu ≥ 720s", true, gap0 >= Stagger - 0.001);
            Close("E3b", "m-3: gap tại chu kỳ 100 = gap ban đầu (không trôi)", gap0, gap100, 1e-9);
            Close("E3c", "m-3: gap tại chu kỳ 1000 = gap ban đầu (vĩnh viễn)", gap0, gap1000, 1e-9);
        }

        // E4: fix B-1 không phá hành vi đồng hồ lùi THẬT — anchor vượt now 2 chu kỳ
        //     (đồng hồ máy lùi ~2 tiếng) vẫn bị phát hiện và ComputePhase vẫn an toàn.
        {
            long badAnchor = now + BoatScheduleCore.SecondsToTicks(Cycle * 2);
            Check("E4a", "đồng hồ lùi thật (anchor now+2 cycle): IsClockRolledBack = true",
                  true, BoatScheduleCore.IsClockRolledBack(now, badAnchor, Cycle));
            var info = BoatScheduleCore.ComputePhase(now, badAnchor, Dock, Hide, Travel);
            Check("E4b", "trước khi manager kịp reset: ComputePhase(anchor tương lai xa) vẫn Hidden an toàn",
                  BoatState.Hidden, info.State);
        }
        Console.WriteLine();
    }
}
