using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Công thức thuần (static, không đọc .Instance) cho lượt câu và quầy cá — mọi random truyền vào dạng roll01
    /// để test được bằng số cố định. Chỉ dùng Mathf (chạy được trong Edit Mode test).
    /// </summary>
    public static class FishingCatchResolver
    {
        /// <summary>Bắt được khi roll01 &lt; baseChance + rodBonus (kẹp 0..1). roll01 = 1 luôn thất bại, chance = 1 luôn thành công với roll &lt; 1.</summary>
        public static bool RollCatch(float baseChance, float rodBonus, float roll01)
        {
            float chance = Mathf.Clamp01(baseChance + rodBonus);
            return roll01 < chance;
        }

        /// <summary>
        /// Rút 1 loài theo trọng số. Lọc: minRodTier ≤ rodTier, unlockLevel ≤ playerLevel, spawnWeight &gt; 0.
        /// Rare/Epic nhân rareWeightMultiplier (cần tốt ra cá hiếm nhiều hơn). Pool rỗng/không loài hợp lệ → null.
        /// </summary>
        public static FishData PickFish(IReadOnlyList<FishData> pool, int rodTier, int playerLevel, float rareWeightMultiplier, float roll01)
        {
            if (pool == null || pool.Count == 0) { return null; }
            float mult = rareWeightMultiplier > 0f ? rareWeightMultiplier : 1f;

            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                total += WeightOf(pool[i], rodTier, playerLevel, mult);
            }
            if (total <= 0f) { return null; }

            // Rút theo tích luỹ: roll01 = 0 → loài hợp lệ đầu tiên; roll01 → 1 → loài cuối.
            float target = Mathf.Clamp01(roll01) * total;
            float acc = 0f;
            FishData last = null;
            for (int i = 0; i < pool.Count; i++)
            {
                float w = WeightOf(pool[i], rodTier, playerLevel, mult);
                if (w <= 0f) { continue; }
                last = pool[i];
                acc += w;
                if (target < acc) { return pool[i]; }
            }
            // roll01 == 1 (hoặc sai số float) rơi khỏi vòng → trả loài hợp lệ cuối.
            return last;
        }

        /// <summary>Trọng số hiệu lực của 1 loài với cần/cấp hiện tại; 0 = không thể ra.</summary>
        public static float WeightOf(FishData f, int rodTier, int playerLevel, float rareWeightMultiplier)
        {
            if (f == null) { return 0f; }
            if (f.spawnWeight <= 0f) { return 0f; }
            if (f.minRodTier > rodTier) { return 0f; }
            if (f.unlockLevel > playerLevel) { return 0f; }
            float w = f.spawnWeight;
            if (f.rarity == FishRarity.Rare || f.rarity == FishRarity.Epic) { w *= rareWeightMultiplier; }
            return w;
        }

        /// <summary>Cân nặng trang trí (kg) trong weightKgRange, làm tròn 2 chữ số. Cá null → 0.</summary>
        public static float RollWeightKg(FishData f, float roll01)
        {
            if (f == null) { return 0f; }
            float lo = Mathf.Min(f.weightKgRange.x, f.weightKgRange.y);
            float hi = Mathf.Max(f.weightKgRange.x, f.weightKgRange.y);
            float kg = Mathf.Lerp(lo, hi, Mathf.Clamp01(roll01));
            return Mathf.Round(kg * 100f) / 100f;
        }

        /// <summary>Tổng vàng khi bán cả giỏ: Σ amount × sellPrice. Loài không có trong database bị bỏ qua (không trả tiền cho id lạ).</summary>
        public static int SellValue(IReadOnlyList<FishStack> items, FishingDatabase db)
        {
            if (items == null || db == null) { return 0; }
            long total = 0;
            for (int i = 0; i < items.Count; i++)
            {
                FishStack s = items[i];
                if (s == null || s.amount <= 0) { continue; }
                FishData f = FindFishLoose(db, s.fishId);
                if (f == null) { continue; }
                total += (long)s.amount * Mathf.Max(0, f.sellPrice);
            }
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// Tra cá theo id: thử đúng chữ trước, rồi so không phân biệt hoa/thường + trim.
        /// Vì FishBasket lưu khoá đã lower() còn FishData.fishId do Sếp gõ tay có thể lẫn chữ hoa.
        /// </summary>
        public static FishData FindFishLoose(FishingDatabase db, string fishId)
        {
            if (db == null || string.IsNullOrEmpty(fishId)) { return null; }
            FishData exact = db.FindFish(fishId);
            if (exact != null) { return exact; }
            string key = fishId.Trim();
            for (int i = 0; i < db.fishes.Count; i++)
            {
                FishData f = db.fishes[i];
                if (f == null || string.IsNullOrEmpty(f.fishId)) { continue; }
                if (string.Equals(f.fishId.Trim(), key, System.StringComparison.OrdinalIgnoreCase)) { return f; }
            }
            return null;
        }

        /// <summary>EXP khi bán = max(1, round(gold / divisor)) nếu gold &gt; 0, ngược lại 0 (giống quầy hàng, divisor 10).</summary>
        public static int ExpForSale(int gold, int divisor)
        {
            if (gold <= 0) { return 0; }
            int d = divisor < 1 ? 1 : divisor;
            return Mathf.Max(1, Mathf.RoundToInt(gold / (float)d));
        }
    }
}
