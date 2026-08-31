using System;
using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════════════════
//  TouristRewardCalculatorTests — test CONSOLE cho công thức thưởng V2.1.
//
//  Chạy KHÔNG cần Unity (stub nằm trong _TestStubs_Unity.cs, ngoài Assets/ nên
//  Unity không compile):
//
//      mcs -define:UNITY_EDITOR -out:/tmp/rewardtests.exe \
//          Assets/_Game/Farm/Scripts/TouristBoat/TouristBoatConfig.cs \
//          Assets/_Game/Farm/Scripts/TouristBoat/Visitors/TouristRewardCalculator.cs \
//          tests/unit/touristboat/_TestStubs_Unity.cs \
//          tests/unit/touristboat/TouristRewardCalculatorTests.cs
//      mono /tmp/rewardtests.exe
//
//  CẦN cờ -define:UNITY_EDITOR: test nhóm 7 gọi EditorResetWarningCache() —
//  hàm nằm trong #if UNITY_EDITOR (bản build player không có, đúng thiết kế).
//
//  Exit code 0 = tất cả PASS, 1 = có FAIL.
//
//  Test gọi ĐÚNG file calculator của game (không copy lại công thức) — số nào
//  in ra ở đây là số người chơi thật sẽ nhận.
//
//  Phủ: 7 món có data thật · hệ số độ khó · rarityBonus + trần · [QA M-9]
//  touristExpMultiplier · touristGoldMultiplier · fallback sellPrice/rewardExp ·
//  sàn 1 · loại gia vị (QA M-4) · log cảnh báo chỉ 1 lần mỗi món.
// ═══════════════════════════════════════════════════════════════════════════

public static class TouristRewardCalculatorTests
{
    private static int _pass, _fail;
    private static string _group = "";

    private static void Group(string name)
    {
        _group = name;
        Console.WriteLine();
        Console.WriteLine("── " + name + " " + new string('─', Math.Max(0, 60 - name.Length)));
    }

    private static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Console.WriteLine("  [PASS] " + what); }
        else    { _fail++; Console.WriteLine("  [FAIL] " + _group + " → " + what); }
    }

    private static void CheckEqual(int actual, int expected, string what)
    {
        Check(actual == expected, what + $" (mong đợi {expected}, thực tế {actual})");
    }

    // ─── Dựng dữ liệu ────────────────────────────────────────────────────

    private static DishData Mon(string id, DishDifficulty diff, int level, int sellPrice, int rewardExp)
    {
        var d = new DishData
        {
            dishId = id, difficulty = diff, unlockLevel = level,
            sellPrice = sellPrice, rewardExp = rewardExp,
            requiredIngredients = new List<IngredientData>(),
        };
        return d;
    }

    private static void ThemNguyenLieu(DishData dish, IngredientTier tier, IngredientKind kind = IngredientKind.Ingredient)
    {
        dish.requiredIngredients.Add(new IngredientData
        {
            id = "ing_" + dish.requiredIngredients.Count, tier = tier, kind = kind,
        });
    }

    /// <summary>Config mặc định = đúng default trong TouristBoatConfig (không set tay số nào).</summary>
    private static TouristBoatConfig CfgMacDinh() => new TouristBoatConfig();

    // ─── Main ────────────────────────────────────────────────────────────

    public static int Main()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  TouristRewardCalculator V2.1 — unit test (BOAT-002)         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

        TestBayMonDataThat();
        TestHeSoDoKho();
        TestRarityBonus();
        TestKnobExp();       // [QA M-9]
        TestKnobVang();
        TestFallback();
        TestLogMotLan();

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        Console.WriteLine($"  TỔNG KẾT: {_pass} PASS · {_fail} FAIL");
        Console.WriteLine("══════════════════════════════════════════════════════════════");
        return _fail == 0 ? 0 : 1;
    }

    // ─── 1. Bảy món có số liệu THẬT (Lead parse từ project của Sếp) ──────

    private static void TestBayMonDataThat()
    {
        Group("1. Bảy món data thật — vàng & EXP người chơi thật sự nhận");

        string[] ids = { "com_chien_bap_cai", "sup_ngo_vang", "khoai_tay_chien", "bo_xao_tieu",
                         "pho_bo_tai", "bo_ham_bi_do_kem", "salad_dua_hau_bo_ap_chao" };
        DishDifficulty[] diffs = { DishDifficulty.Easy, DishDifficulty.Easy, DishDifficulty.Easy,
                                   DishDifficulty.Normal, DishDifficulty.Hard, DishDifficulty.Hard, DishDifficulty.Hard };
        int[] levels    = { 1, 2, 5, 10, 9, 26, 30 };
        int[] sells     = { 62, 76, 95, 315, 400, 823, 884 };
        int[] expGoc    = { 3, 6, 15, 45, 54, 156, 180 };
        int[] vangMong  = { 62, 76, 95, 362, 540, 1111, 1193 };   // Lead tính tay 4 mốc: 62 · 362 · 540 · 1193
        int[] expMong   = { 1, 2, 6, 20, 27, 78, 90 };            // sau khi nhân touristExpMultiplier 0.4 (QA M-9)

        var cfg = CfgMacDinh();
        for (int i = 0; i < ids.Length; i++)
        {
            DishData dish = Mon(ids[i], diffs[i], levels[i], sells[i], expGoc[i]);
            bool fallback;
            int vang = TouristRewardCalculator.ComputeGold(dish, cfg, out fallback);
            int exp  = TouristRewardCalculator.ComputeExp(dish, cfg);

            CheckEqual(vang, vangMong[i], $"{ids[i]}: vàng");
            CheckEqual(exp,  expMong[i],  $"{ids[i]}: EXP (đã hãm 0.4)");
            Check(!fallback, $"{ids[i]}: dùng đường CHÍNH (sellPrice), không fallback");

            // Cột quan trọng nhất của bảng cân bằng: không được LỖ hơn bán chợ.
            Check(vang >= sells[i], $"{ids[i]}: vàng khách trả ({vang}) >= giá bán chợ ({sells[i]}) — không lỗ");
        }
    }

    // ─── 2. Hệ số độ khó ────────────────────────────────────────────────

    private static void TestHeSoDoKho()
    {
        Group("2. Hệ số độ khó — Hard phải hơn Easy CÙNG tầm giá");

        var cfg = CfgMacDinh();
        bool fb;
        int easy   = TouristRewardCalculator.ComputeGold(Mon("e", DishDifficulty.Easy,   10, 400, 50), cfg, out fb);
        int normal = TouristRewardCalculator.ComputeGold(Mon("n", DishDifficulty.Normal, 10, 400, 50), cfg, out fb);
        int hard   = TouristRewardCalculator.ComputeGold(Mon("h", DishDifficulty.Hard,   10, 400, 50), cfg, out fb);

        CheckEqual(easy,   400, "Easy sell 400 → 400 vàng (×1.00, bằng giá chợ)");
        CheckEqual(normal, 460, "Normal sell 400 → 460 vàng (×1.15)");
        CheckEqual(hard,   540, "Hard sell 400 → 540 vàng (×1.35)");
        Check(hard > normal && normal > easy, "cùng sellPrice: Hard > Normal > Easy");

        int expEasy   = TouristRewardCalculator.ComputeExp(Mon("e", DishDifficulty.Easy,   10, 400, 100), cfg);
        int expNormal = TouristRewardCalculator.ComputeExp(Mon("n", DishDifficulty.Normal, 10, 400, 100), cfg);
        int expHard   = TouristRewardCalculator.ComputeExp(Mon("h", DishDifficulty.Hard,   10, 400, 100), cfg);
        CheckEqual(expEasy,   40, "EXP Easy: 100 × 1.00 × 0.4 = 40");
        CheckEqual(expNormal, 44, "EXP Normal: 100 × 1.10 × 0.4 = 44");
        CheckEqual(expHard,   50, "EXP Hard: 100 × 1.25 × 0.4 = 50");
    }

    // ─── 3. rarityBonus + trần ──────────────────────────────────────────

    private static void TestRarityBonus()
    {
        Group("3. rarityBonus theo IngredientTier + trần rarityBonusCap");

        var cfg = CfgMacDinh();
        bool fb;

        DishData basic = Mon("basic", DishDifficulty.Easy, 10, 100, 10);
        ThemNguyenLieu(basic, IngredientTier.Basic);
        ThemNguyenLieu(basic, IngredientTier.Basic);
        CheckEqual(TouristRewardCalculator.ComputeGold(basic, cfg, out fb), 100,
                   "toàn nguyên liệu Basic → không thưởng thêm (bonus 1.00)");

        DishData rare = Mon("rare", DishDifficulty.Easy, 10, 100, 10);
        ThemNguyenLieu(rare, IngredientTier.Rare);
        ThemNguyenLieu(rare, IngredientTier.Rare);
        CheckEqual(TouristRewardCalculator.ComputeGold(rare, cfg, out fb), 110,
                   "2 Rare → 1 + 0.05×2 = 1.10");

        DishData mix = Mon("mix", DishDifficulty.Easy, 10, 100, 10);
        ThemNguyenLieu(mix, IngredientTier.Epic);
        ThemNguyenLieu(mix, IngredientTier.Epic);
        ThemNguyenLieu(mix, IngredientTier.Rare);
        ThemNguyenLieu(mix, IngredientTier.Epic, IngredientKind.Seasoning); // [QA M-4] gia vị KHÔNG tính
        CheckEqual(TouristRewardCalculator.ComputeGold(mix, cfg, out fb), 129,
                   "2 Epic + 1 Rare + 1 Epic GIA VỊ → 1.29 (gia vị bị loại đúng QA M-4)");

        DishData cap = Mon("cap", DishDifficulty.Easy, 10, 100, 10);
        for (int i = 0; i < 5; i++) ThemNguyenLieu(cap, IngredientTier.Epic);
        CheckEqual(TouristRewardCalculator.ComputeGold(cap, cfg, out fb), 150,
                   "5 Epic (1.60) → bị kẹp trần 1.50");

        var cfgCap = CfgMacDinh();
        cfgCap.rarityBonusCap = 1.2f;
        CheckEqual(TouristRewardCalculator.ComputeGold(cap, cfgCap, out fb), 120,
                   "hạ rarityBonusCap = 1.2 → kẹp còn 1.20");

        DishData khongKhai = Mon("trong", DishDifficulty.Easy, 10, 100, 10);
        CheckEqual(TouristRewardCalculator.ComputeGold(khongKhai, cfg, out fb), 100,
                   "món không khai nguyên liệu → bonus 1.00, KHÔNG bị phạt");
    }

    // ─── 4. [QA M-9] touristExpMultiplier — núm hãm lạm phát EXP ────────

    private static void TestKnobExp()
    {
        Group("4. [QA M-9] touristExpMultiplier — hãm lạm phát cấp độ");

        var cfg = CfgMacDinh();
        Check(Math.Abs(cfg.touristExpMultiplier - 0.4f) < 0.0001f,
              "default của config = 0.4 (mặc định phải là mặc định ĐÃ HÃM)");

        DishData pho = Mon("pho_bo_tai", DishDifficulty.Hard, 9, 400, 54);
        CheckEqual(TouristRewardCalculator.ComputeExp(pho, cfg), 27,
                   "pho_bo_tai: 54 × 1.25 × 0.4 = 27 EXP (trước khi hãm là 68)");

        var cfg1 = CfgMacDinh(); cfg1.touristExpMultiplier = 1.0f;
        CheckEqual(TouristRewardCalculator.ComputeExp(pho, cfg1), 68,
                   "đặt knob = 1.0 → về đúng số cũ 68 (chứng minh knob ăn thẳng vào EXP)");

        var cfgHalf = CfgMacDinh(); cfgHalf.touristExpMultiplier = 0.5f;
        CheckEqual(TouristRewardCalculator.ComputeExp(pho, cfgHalf), 34,
                   "knob 0.5 → 34 EXP");

        // Sàn 1: món EXP nhỏ nhân hệ số nhỏ vẫn phải trả ít nhất 1 (0 EXP nhìn như bug).
        DishData nho = Mon("com_chien_bap_cai", DishDifficulty.Easy, 1, 62, 3);
        CheckEqual(TouristRewardCalculator.ComputeExp(nho, cfg), 1,
                   "món 3 EXP × 0.4 = 1.2 → 1 EXP (sàn 1, không bao giờ 0)");

        var cfgTiny = CfgMacDinh(); cfgTiny.touristExpMultiplier = 0.01f;
        CheckEqual(TouristRewardCalculator.ComputeExp(nho, cfgTiny), 1,
                   "knob cực nhỏ 0.01 → vẫn 1 EXP nhờ sàn");

        // Config null (Dev B gọi trước khi manager kịp Awake) → dùng 0.4, KHÔNG phải 1.0.
        CheckEqual(TouristRewardCalculator.ComputeExp(pho, null), 27,
                   "config null → dùng mặc định 0.4 (an toàn), không rơi về 1.0");

        // Làm tròn MỘT LẦN ở cuối: 45 × 1.10 × 0.4 = 19.8 → 20.
        // Nếu round 2 lần (round(45×1.10)=50 rồi ×0.4=20) trùng nhau ở ca này,
        // nên dùng ca lệch rõ: 7 × 1.25 × 0.4 = 3.5 → 4; round 2 lần: round(8.75)=9 ×0.4=3.6→4.
        DishData r1 = Mon("r1", DishDifficulty.Normal, 5, 100, 45);
        CheckEqual(TouristRewardCalculator.ComputeExp(r1, cfg), 20, "45 × 1.10 × 0.4 = 19.8 → 20");
        DishData r2 = Mon("r2", DishDifficulty.Hard, 5, 100, 33);
        CheckEqual(TouristRewardCalculator.ComputeExp(r2, cfg), 17, "33 × 1.25 × 0.4 = 16.5 → 17 (tròn NỬA LÊN)");
    }

    // ─── 5. touristGoldMultiplier ───────────────────────────────────────

    private static void TestKnobVang()
    {
        Group("5. touristGoldMultiplier — núm chống lạm phát vàng");

        bool fb;
        DishData pho = Mon("pho_bo_tai", DishDifficulty.Hard, 9, 400, 54);

        var cfg08 = CfgMacDinh(); cfg08.touristGoldMultiplier = 0.8f;
        CheckEqual(TouristRewardCalculator.ComputeGold(pho, cfg08, out fb), 432, "knob 0.8 → 540 thành 432");

        var cfg12 = CfgMacDinh(); cfg12.touristGoldMultiplier = 1.2f;
        CheckEqual(TouristRewardCalculator.ComputeGold(pho, cfg12, out fb), 648, "knob 1.2 → 648");

        var cfgTiny = CfgMacDinh(); cfgTiny.touristGoldMultiplier = 0.001f;
        Check(TouristRewardCalculator.ComputeGold(pho, cfgTiny, out fb) >= 1,
              "knob cực nhỏ → vẫn >= 1 vàng (thưởng 0 sẽ làm DeliverTo huỷ giao dịch — QA B-3)");

        CheckEqual(TouristRewardCalculator.ComputeGold(null, CfgMacDinh(), out fb), 0,
                   "dish null → 0 (bên gọi tự huỷ, không trừ kho)");
    }

    // ─── 6. Fallback khi asset thiếu data ───────────────────────────────

    private static void TestFallback()
    {
        Group("6. Fallback — món chưa điền sellPrice / rewardExp");

        var cfg = CfgMacDinh();
        bool fb;

        // sellPrice = 0 → đường cũ (Σ giá nguyên liệu chính × rewardIngredientMultiplier).
        // Stub BasePriceBook luôn "không tra được" ⇒ dùng DefaultBasePrice 10 × 2 = 20.
        DishData khongGia = Mon("mon_thieu_sell", DishDifficulty.Normal, 7, 0, 0);
        ThemNguyenLieu(khongGia, IngredientTier.Basic);
        int vang = TouristRewardCalculator.ComputeGold(khongGia, cfg, out fb);
        Check(fb, "sellPrice = 0 → báo usedFallback = true cho bên gọi log");
        CheckEqual(vang, 20, "fallback: DefaultBasePrice 10 × rewardIngredientMultiplier 2 = 20");
        Check(vang >= 1, "fallback KHÔNG BAO GIỜ trả 0 (QA B-3)");

        // rewardExp = 0 → suy (8 + level × 1.5) rồi mới nhân hệ số.
        // Lv7 Normal: (8 + 10.5) = 18.5 × 1.10 × 0.4 = 8.14 → 8.
        CheckEqual(TouristRewardCalculator.ComputeExp(khongGia, cfg), 8,
                   "rewardExp = 0, Lv7 Normal → (8 + 7×1.5) × 1.10 × 0.4 = 8 EXP");

        DishData lv30 = Mon("mon_lv30", DishDifficulty.Hard, 30, 0, 0);
        CheckEqual(TouristRewardCalculator.ComputeExp(lv30, cfg), 27,
                   "rewardExp = 0, Lv30 Hard → (8 + 45) × 1.25 × 0.4 = 26.5 → 27");

        // Overload tương thích V2.0 mà Dev B đang gọi (không có config) vẫn phải chạy.
        BoatDockManager.Instance = new BoatDockManager { Config = cfg };
        DishData pho = Mon("pho_bo_tai", DishDifficulty.Hard, 9, 400, 54);
        bool fb2;
        CheckEqual(TouristRewardCalculator.ComputeGold(pho, 2f, out fb2), 540,
                   "[compat] ComputeGold(dish, multiplier, out fb) tự lấy config → 540");
        CheckEqual(TouristRewardCalculator.ComputeExp(pho), 27,
                   "[compat] ComputeExp(dish) tự lấy config → 27 (đã hãm 0.4)");
        BoatDockManager.Instance = null;
    }

    // ─── 7. Log cảnh báo chỉ 1 lần mỗi món ──────────────────────────────

    private static void TestLogMotLan()
    {
        Group("7. Log cảnh báo fallback chỉ 1 lần / món / phiên");

        TouristRewardCalculator.EditorResetWarningCache();
        UnityEngine.Debug.Warnings.Clear();

        var cfg = CfgMacDinh();
        DishData thieu = Mon("mon_thieu_data", DishDifficulty.Easy, 3, 0, 0);
        ThemNguyenLieu(thieu, IngredientTier.Basic);

        bool fb;
        for (int i = 0; i < 5; i++)
        {
            TouristRewardCalculator.ComputeGold(thieu, cfg, out fb);
            TouristRewardCalculator.ComputeExp(thieu, cfg);
        }

        int soLanVang = 0, soLanExp = 0;
        foreach (string w in UnityEngine.Debug.Warnings)
        {
            if (w.Contains("sellPrice")) soLanVang++;
            if (w.Contains("rewardExp")) soLanExp++;
        }
        CheckEqual(soLanVang, 1, "gọi 5 lần → cảnh báo thiếu sellPrice đúng 1 lần (không spam Console)");
        CheckEqual(soLanExp,  1, "gọi 5 lần → cảnh báo thiếu rewardExp đúng 1 lần");

        TouristRewardCalculator.EditorResetWarningCache();
        UnityEngine.Debug.Warnings.Clear();
        TouristRewardCalculator.ComputeGold(thieu, cfg, out fb);
        Check(UnityEngine.Debug.Warnings.Count >= 1, "sau EditorResetWarningCache → cảnh báo lại (tool xuất bảng cần)");
    }
}
