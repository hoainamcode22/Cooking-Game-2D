using UnityEngine;

// ═══════════════════════════════════════════════════════════════════════════
//  [QA B-6] FILE NÀY TÁCH RA TỪ TouristSmileyFlyFX.cs — LÝ DO PHẢI ĐỌC:
//
//  Trước đây TouristSmileyFlyFX.cs chứa HAI class của HAI chủ khác nhau:
//  TouristSmileyFlyFX (Dev B) và TouristRewardCalculator (Dev A). Hai người sửa
//  nửa của mình rồi ship CẢ file ⇒ khi merge, bản copy sau ghi đè bản trước.
//  QA dựng thử: copy A→B thì player build COMPILE SẠCH nhưng chạy công thức
//  thưởng CŨ — mất trắng quyết định cân bằng mà không một dòng cảnh báo.
//  Đó là kiểu lỗi tệ nhất: im lặng, và lộ ra đúng lúc build phát hành.
//
//  Từ nay: mỗi file MỘT CHỦ. TouristSmileyFlyFX.cs = Dev B (hiệu ứng mặt cười),
//  file này = Dev A (công thức thưởng). Đừng gộp lại.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// TÍNH THƯỞNG KHI GIAO MÓN CHO KHÁCH — CÔNG THỨC V2.1 (Lead chốt 2026-08-29).
///
/// <code>
///   vàng = round( sellPrice × diffMult × rarityBonus × touristGoldMultiplier )   [sàn 1]
///   exp  = round( rewardExp × expMult × touristExpMultiplier )                    [sàn 1]
/// </code>
///
/// ── VÌ SAO ĐỔI KHỎI "Σ GIÁ NGUYÊN LIỆU × 2" (đọc kỹ trước khi sửa lại) ────────
/// Bản V2.0 lấy Σ giá nguyên liệu chính × 2 theo chữ GDD §3.4. Sau khi Lead đọc CẢ 38
/// asset DishData thật thì lộ 2 điều:
///   1. Bảng 38 món ĐÃ ĐƯỢC CÂN BẰNG RẤT KỸ theo level (sellPrice 62 → 884 trải đều
///      từ Lv1 tới Lv30). Tái dùng <c>sellPrice</c> là cách DUY NHẤT giữ được đường
///      cong kinh tế đó; mọi công thức tự tính từ nguyên liệu đều vẽ lại một đường
///      cong khác, lệch khỏi thiết kế của Sếp.
///   2. Công thức cũ LỖ HƠN BÁN CHỢ: khoai_tay_chien có 1 nguyên liệu, Σ×2 = 50 vàng
///      trong khi bán chợ được 95 ⇒ phục vụ khách du lịch là lựa chọn TỆ, không ai làm.
///      Đây là lỗi cân bằng nặng hơn cả chuyện "thưởng không theo độ khó".
/// Hệ số độ khó chồng lên trên chỉ để món Hard ăn hơn món Easy CÙNG TẦM GIÁ.
///
/// <c>dish.rewardGold</c> CỐ Ý KHÔNG DÙNG: Lead kiểm được nó luôn bằng đúng
/// <c>round(sellPrice × 0.25)</c> ở cả 38 món ⇒ đó là "vàng khi nấu đạt trong minigame",
/// không phải giá trị món. Dùng nó cho khách du lịch sẽ trả quá bèo (62 vàng → 16).
///
/// <c>rarityBonus</c> là CHỖ DUY NHẤT dùng <see cref="IngredientTier"/>:
/// <c>1 + 0.05×(số nguyên liệu Rare) + 0.12×(số Epic)</c>, kẹp trần
/// <c>config.rarityBonusCap</c> (1.5) để món 5 nguyên liệu Epic không trả gấp đôi.
///
/// ── ĐƯỜNG FALLBACK (món chưa điền data) ──────────────────────────────────────
///   sellPrice ≤ 0  → về đường CŨ: Σ giá nguyên liệu chính (BasePriceBook) ×
///                    config.rewardIngredientMultiplier; tra không được giá thì xuống
///                    BasePriceBook.DefaultBasePrice. [QA B-3] KHÔNG BAO GIỜ trả 0 —
///                    thưởng 0 làm DeliverTo huỷ giao dịch, người chơi không giao được món.
///   rewardExp ≤ 0  → suy round(8 + unlockLevel × 1.5).
/// Mỗi món chỉ log cảnh báo MỘT LẦN mỗi phiên chơi (không spam Console) để Sếp biết
/// đúng asset nào còn thiếu số.
///
/// [QA M-4] Đường fallback chỉ cộng <see cref="IngredientKind.Ingredient"/> — GIA VỊ
/// (Seasoning) bị loại, đúng chữ GDD "Σ giá NGUYÊN LIỆU CHÍNH của món".
///
/// Thứ tự an toàn của bên gọi KHÔNG ĐỔI (TouristVisitorManager §①②③④): tính thưởng
/// TRƯỚC → thiếu điều kiện thì huỷ, KHÔNG trừ kho → mới RemoveItem → mới AddGold/AddExp.
/// </summary>
public static class TouristRewardCalculator
{
    /// <summary>Thưởng thêm cho mỗi nguyên liệu tier Rare (hằng thiết kế, không phải tuning knob).</summary>
    private const float RareBonusPerItem = 0.05f;

    /// <summary>Thưởng thêm cho mỗi nguyên liệu tier Epic.</summary>
    private const float EpicBonusPerItem = 0.12f;

    /// <summary>[QA M-9] Hệ số EXP khách trả dùng khi không lấy được config — bằng default của config.</summary>
    private const float ExpTouristDefault = 0.75f;

    /// <summary>EXP suy ra khi asset chưa điền rewardExp: round(8 + unlockLevel × 1.5).</summary>
    private const float ExpFallbackBase    = 8f;
    private const float ExpFallbackPerLevel = 1.5f;

    // Hệ số EXP theo độ khó — cố ý nhẹ hơn hệ số vàng (EXP đã tự tăng theo level món).
    private const float ExpMultEasy   = 1.00f;
    private const float ExpMultNormal = 1.25f;
    private const float ExpMultHard   = 1.50f;

    // Chống spam Console: mỗi dishId chỉ cảnh báo fallback 1 lần/phiên.
    private static readonly System.Collections.Generic.HashSet<string> _daCanhBaoVang
        = new System.Collections.Generic.HashSet<string>();
    private static readonly System.Collections.Generic.HashSet<string> _daCanhBaoExp
        = new System.Collections.Generic.HashSet<string>();

    // ─────────────────────────────────────────────────────────────────────
    //  VÀNG
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Vàng khách trả cho 1 món (công thức V2.1). <paramref name="usedFallback"/> = true
    /// khi món chưa điền sellPrice nên phải rơi về đường cũ (Σ giá nguyên liệu).
    /// Món null → 0 (bên gọi tự huỷ giao dịch, không trừ kho).
    /// </summary>
    /// <param name="dish">Asset món khách yêu cầu.</param>
    /// <param name="config">Config boat (lấy các hệ số). Null → dùng số mặc định theo thiết kế.</param>
    /// <param name="usedFallback">true nếu đã dùng đường dự phòng — bên gọi log cho Sếp biết.</param>
    public static int ComputeGold(DishData dish, TouristBoatConfig config, out bool usedFallback)
    {
        usedFallback = false;
        if (dish == null) return 0;

        // ── Đường CHÍNH: theo sellPrice đã cân bằng sẵn trên asset ──────
        if (dish.sellPrice > 0)
        {
            float diff   = HeSoDoKhoVang(dish.difficulty, config);
            float rarity = TinhRarityBonus(dish, config);
            float chung  = config != null ? Mathf.Max(0.01f, config.touristGoldMultiplier) : 1.8f;

            return Mathf.Max(1, LamTron(dish.sellPrice * (double)diff * rarity * chung));
        }

        // ── Đường FALLBACK: món chưa điền sellPrice ─────────────────────
        usedFallback = true;
        CanhBaoMotLan(_daCanhBaoVang, dish,
            $"chưa điền sellPrice (= {dish.sellPrice}) — thưởng vàng tạm tính theo Σ giá nguyên liệu " +
            "(đường dự phòng V2.0). Điền sellPrice cho asset món này để số thưởng đúng thiết kế.");

        return VangTheoNguyenLieu(dish, config);
    }

    /// <summary>
    /// [TƯƠNG THÍCH V2.0] Chữ ký cũ mà TouristVisitorManager (Dev B) đang gọi —
    /// GIỮ NGUYÊN để không phải sửa file của Dev B. Tự lấy config qua
    /// <c>BoatDockManager.Instance.Config</c>; <paramref name="multiplier"/> chỉ còn
    /// dùng cho đường fallback (đúng vai trò rewardIngredientMultiplier).
    /// </summary>
    public static int ComputeGold(DishData dish, float multiplier, out bool usedFallback)
    {
        TouristBoatConfig cfg = BoatDockManager.Instance != null ? BoatDockManager.Instance.Config : null;
        int vang = ComputeGold(dish, cfg, out usedFallback);

        // Fallback + không lấy được config: tôn trọng multiplier bên gọi truyền vào.
        if (usedFallback && cfg == null && multiplier > 0.01f && dish != null)
            vang = Mathf.Max(1, LamTron(TongGiaNguyenLieuChinh(dish, out bool ok) * (double)multiplier));

        return vang;
    }

    /// <summary>Hệ số vàng theo độ khó món, đọc từ config (null → số thiết kế 1.25/1.50/1.85).</summary>
    private static float HeSoDoKhoVang(DishDifficulty difficulty, TouristBoatConfig config)
    {
        switch (difficulty)
        {
            case DishDifficulty.Easy:   return config != null ? Mathf.Max(0.01f, config.diffMultEasy)   : 1.25f;
            case DishDifficulty.Normal: return config != null ? Mathf.Max(0.01f, config.diffMultNormal) : 1.50f;
            case DishDifficulty.Hard:   return config != null ? Mathf.Max(0.01f, config.diffMultHard)   : 1.85f;
            default:                    return 1f;
        }
    }

    /// <summary>
    /// rarityBonus = 1 + 0.05×(số nguyên liệu Rare) + 0.12×(số Epic), kẹp trần
    /// <c>config.rarityBonusCap</c> (1.5). Chỉ tính nguyên liệu CHÍNH (bỏ gia vị).
    /// Món không khai nguyên liệu → 1 (không thưởng thêm, không phạt).
    /// </summary>
    private static float TinhRarityBonus(DishData dish, TouristBoatConfig config)
    {
        var list = dish.requiredIngredients;
        if (list == null || list.Count == 0) return 1f;

        int soRare = 0, soEpic = 0;
        for (int i = 0; i < list.Count; i++)
        {
            IngredientData ing = list[i];
            if (ing == null) continue;
            if (ing.kind == IngredientKind.Seasoning) continue; // [QA M-4] gia vị không tính

            if (ing.tier == IngredientTier.Rare)      soRare++;
            else if (ing.tier == IngredientTier.Epic) soEpic++;
        }

        float bonus = 1f + RareBonusPerItem * soRare + EpicBonusPerItem * soEpic;
        float cap   = config != null ? Mathf.Max(1f, config.rarityBonusCap) : 1.5f;
        return Mathf.Min(bonus, cap);
    }

    /// <summary>
    /// Đường dự phòng V2.0: Σ giá nguyên liệu CHÍNH × rewardIngredientMultiplier.
    /// Tra giá qua <see cref="BasePriceBook.TryGetBasePrice"/> — sổ giá duy nhất của dự
    /// án, GDD §3.4 cấm bịa bảng giá mới. Không tra được / món toàn gia vị → dùng
    /// <see cref="BasePriceBook.DefaultBasePrice"/>. [QA B-3] luôn ≥ 1.
    /// </summary>
    private static int VangTheoNguyenLieu(DishData dish, TouristBoatConfig config)
    {
        float mul = config != null ? Mathf.Max(1, config.rewardIngredientMultiplier) : 2f;

        bool traDuGia;
        int tong = TongGiaNguyenLieuChinh(dish, out traDuGia);

        if (!traDuGia || tong <= 0)
        {
            CanhBaoMotLan(_daCanhBaoVang, dish,
                "không tra đủ giá nguyên liệu chính (hoặc món toàn gia vị) — tạm thưởng theo giá " +
                $"mặc định {BasePriceBook.DefaultBasePrice}. Bổ sung giá vào MarketPriceTable/StallItemCatalog.");
            tong = BasePriceBook.DefaultBasePrice;
        }

        return Mathf.Max(1, LamTron(tong * (double)mul));
    }

    /// <summary>
    /// Σ giá nguyên liệu CHÍNH của món. <paramref name="traDuGia"/> = false nếu có bất kỳ
    /// nguyên liệu nào không tra được giá (bên gọi coi như cả món hỏng, theo GDD §3.4).
    /// </summary>
    private static int TongGiaNguyenLieuChinh(DishData dish, out bool traDuGia)
    {
        traDuGia = true;
        if (dish == null) return 0;

        var list = dish.requiredIngredients;
        if (list == null || list.Count == 0) { traDuGia = false; return 0; }

        int tong = 0, soChinh = 0;
        for (int i = 0; i < list.Count; i++)
        {
            IngredientData ing = list[i];
            if (ing == null || string.IsNullOrEmpty(ing.id)) { traDuGia = false; return 0; }
            if (ing.kind == IngredientKind.Seasoning) continue; // [QA M-4]

            int gia;
            if (!BasePriceBook.TryGetBasePrice(ing.id, out gia)) { traDuGia = false; return 0; }
            tong += gia;
            soChinh++;
        }

        if (soChinh == 0) traDuGia = false;
        return tong;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  EXP
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EXP khách trả = round(rewardExp × expMult theo độ khó × config.touristExpMultiplier), sàn 1.
    ///
    /// [QA M-9] touristExpMultiplier (0.4) là NÚM HÃM LẠM PHÁT, không phải trang trí:
    /// nấu xong trong minigame đã cộng rewardExp × hệ số điểm rồi (CookingChallengeManager),
    /// phục vụ khách cộng thêm lần nữa ⇒ mỗi món ~2× EXP thiết kế, người chơi lên hết
    /// cấp trần 30 trong 1,2-3,7 giờ kể từ lúc mở bến. Nhân 0.4 kéo tổng về ~1,4× thiết kế.
    /// Config null → dùng 0.4 (KHÔNG dùng 1.0: mặc định an toàn phải là mặc định đã hãm).
    ///
    /// Món chưa điền rewardExp (≤ 0) → suy (8 + unlockLevel × 1.5) + log 1 lần.
    /// CHỈ làm tròn MỘT LẦN ở cuối (nhân hết hệ số rồi mới round) — round 2 lần làm
    /// lệch tới 1 EXP ở món nhỏ, và số trong bảng cân bằng sẽ không tái lập được.
    /// </summary>
    public static int ComputeExp(DishData dish, TouristBoatConfig config)
    {
        if (dish == null) return 0;

        double mult    = HeSoDoKhoExp(dish.difficulty);
        double hamLai  = config != null ? Mathf.Max(0.01f, config.touristExpMultiplier) : ExpTouristDefault;

        double expGoc;
        if (dish.rewardExp > 0)
        {
            expGoc = dish.rewardExp;
        }
        else
        {
            expGoc = ExpFallbackBase + Mathf.Max(1, dish.unlockLevel) * (double)ExpFallbackPerLevel;
            CanhBaoMotLan(_daCanhBaoExp, dish,
                $"chưa điền rewardExp — tạm suy {LamTron(expGoc)} EXP gốc theo unlockLevel ({dish.unlockLevel}). " +
                "Điền rewardExp cho asset món này.");
        }

        return Mathf.Max(1, LamTron(expGoc * mult * hamLai));
    }

    /// <summary>[TƯƠNG THÍCH V2.0] Chữ ký cũ Dev B đang gọi — tự lấy config từ BoatDockManager.</summary>
    public static int ComputeExp(DishData dish)
    {
        return ComputeExp(dish, BoatDockManager.Instance != null ? BoatDockManager.Instance.Config : null);
    }

    /// <summary>
    /// Hệ số EXP theo độ khó — HẰNG trong code (không mở knob): EXP đã tự tăng theo level
    /// món nên chỉ cần nhấn nhẹ, mở thêm knob chỉ làm bảng config rối.
    /// </summary>
    private static float HeSoDoKhoExp(DishDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DishDifficulty.Easy:   return ExpMultEasy;
            case DishDifficulty.Normal: return ExpMultNormal;
            case DishDifficulty.Hard:   return ExpMultHard;
            default:                    return 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Làm tròn "nửa lên" (MidpointRounding.AwayFromZero) — KHÔNG dùng Mathf.RoundToInt
    /// vì nó làm tròn về SỐ CHẴN (67.5 → 68 nhưng 66.5 → 66), lệch khỏi chữ "round()"
    /// trong bảng cân bằng Lead đã tính tay.
    /// </summary>
    private static int LamTron(double giaTri)
    {
        return (int)System.Math.Round(giaTri, System.MidpointRounding.AwayFromZero);
    }

    /// <summary>Log cảnh báo fallback ĐÚNG 1 LẦN cho mỗi món trong 1 phiên chơi.</summary>
    private static void CanhBaoMotLan(System.Collections.Generic.HashSet<string> daBao, DishData dish, string noiDung)
    {
        if (dish == null) return;
        string id = !string.IsNullOrEmpty(dish.dishId) ? dish.dishId : dish.name;
        if (!daBao.Add(id)) return; // đã cảnh báo món này rồi

        Debug.LogWarning($"[TouristVisitor] Món '{id}': {noiDung}");
    }

#if UNITY_EDITOR
    /// <summary>(Editor/QA) Xoá bộ nhớ "đã cảnh báo" — tool xuất bảng thưởng gọi trước khi quét 38 món.</summary>
    public static void EditorResetWarningCache()
    {
        _daCanhBaoVang.Clear();
        _daCanhBaoExp.Clear();
    }
#endif
}
