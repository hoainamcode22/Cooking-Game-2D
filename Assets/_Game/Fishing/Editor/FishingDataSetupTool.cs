using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tool 1: tạo/cập nhật DATA Hồ Câu — FishingConfig, 10 FishData, 4 RodData, FishingDatabase (Resources/Fishing).
    /// CHỦ FILE: Dev D. Menu Tools/Farm Game/Hồ Câu/1. ...
    ///
    /// LUẬT: asset MỚI → ghi số ĐỀ XUẤT (Sếp duyệt). Asset ĐÃ CÓ → chỉ bổ sung field rỗng (id/tên), KHÔNG đổi số Sếp đã chỉnh.
    /// Muốn ép về số đề xuất: menu "1b/1c. Ghi đè số đề xuất (DRY-RUN / APPLY)". Icon giữ nguyên nếu đã gán.
    /// Database luôn được wire lại: config, fishes (thêm thiếu, bỏ null), rods (sort tier), characters 2 entry.
    /// </summary>
    public static class FishingDataSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuSetup = MenuRoot + "1. Tạo/cập nhật Data (config, 10 cá, 4 cần, database)";
        private const string MenuOverrideDry = MenuRoot + "1b. Ghi đè số đề xuất lên asset đã có (DRY-RUN)";
        private const string MenuOverrideApply = MenuRoot + "1c. Ghi đè số đề xuất lên asset đã có (APPLY)";

        private const string FishFolder = FishingIds.DataRoot + "/Fish";
        private const string RodFolder = FishingIds.DataRoot + "/Rods";
        private const string ConfigPath = FishingIds.DataRoot + "/FishingConfig.asset";
        public const string DatabasePath = FishingIds.ResourcesFolder + "/FishingDatabase.asset";

        // ─────────────────────────────────────────────────────────────────
        //  BẢNG ĐỀ XUẤT — Sếp duyệt (sửa số trên asset, tool KHÔNG ghi đè trừ khi bấm 1c)
        // ─────────────────────────────────────────────────────────────────

        private struct RodDef
        {
            public int tier; public string name; public string itemId; public int gold; public int gems; public int dur;
            public float bonus; public float biteWait; public float rare; public float windowBonus; public int unlock;
        }

        private struct FishDef
        {
            public string id; public string name; public int price; public FishRarity rarity; public int minTier; public float weight; public int unlock;
        }

        // ĐỀ XUẤT — Sếp duyệt
        private static readonly RodDef[] Rods =
        {
            new RodDef { tier = 1, name = "Cần tre",    itemId = "rod_tre",    gold = 300,  gems = 0,  dur = 20,  bonus = 0.00f, biteWait = 1.00f, rare = 1.0f, windowBonus = 0.0f, unlock = 3 },
            new RodDef { tier = 2, name = "Cần gỗ",     itemId = "rod_go",     gold = 900,  gems = 0,  dur = 40,  bonus = 0.10f, biteWait = 0.85f, rare = 1.3f, windowBonus = 0.0f, unlock = 5 },
            new RodDef { tier = 3, name = "Cần carbon", itemId = "rod_carbon", gold = 2500, gems = 0,  dur = 80,  bonus = 0.20f, biteWait = 0.70f, rare = 1.8f, windowBonus = 0.0f, unlock = 8 },
            new RodDef { tier = 4, name = "Cần vàng",   itemId = "rod_vang",   gold = 0,    gems = 30, dur = 150, bonus = 0.30f, biteWait = 0.55f, rare = 2.5f, windowBonus = 0.5f, unlock = 10 },
        };

        // ĐỀ XUẤT — Sếp duyệt (fishId, tên, giá, rarity, minRodTier, weight, unlock)
        private static readonly FishDef[] Fishes =
        {
            new FishDef { id = "fish_ro",        name = "Cá rô",        price = 18,  rarity = FishRarity.Common,   minTier = 1, weight = 30f, unlock = 1 },
            new FishDef { id = "fish_diec",      name = "Cá diếc",      price = 24,  rarity = FishRarity.Common,   minTier = 1, weight = 26f, unlock = 1 },
            new FishDef { id = "fish_chep",      name = "Cá chép",      price = 45,  rarity = FishRarity.Common,   minTier = 1, weight = 18f, unlock = 1 },
            new FishDef { id = "fish_tre",       name = "Cá trê",       price = 60,  rarity = FishRarity.Uncommon, minTier = 1, weight = 12f, unlock = 3 },
            new FishDef { id = "fish_loc",       name = "Cá lóc",       price = 90,  rarity = FishRarity.Uncommon, minTier = 2, weight = 10f, unlock = 5 },
            new FishDef { id = "fish_tram",      name = "Cá trắm",      price = 140, rarity = FishRarity.Uncommon, minTier = 2, weight = 8f,  unlock = 6 },
            new FishDef { id = "fish_lang",      name = "Cá lăng",      price = 220, rarity = FishRarity.Rare,     minTier = 3, weight = 5f,  unlock = 8 },
            new FishDef { id = "fish_tai_tuong", name = "Cá tai tượng", price = 320, rarity = FishRarity.Rare,     minTier = 3, weight = 4f,  unlock = 9 },
            new FishDef { id = "fish_hoi",       name = "Cá hồi",       price = 480, rarity = FishRarity.Epic,     minTier = 4, weight = 2f,  unlock = 10 },
            new FishDef { id = "fish_koi_vang",  name = "Cá koi vàng",  price = 800, rarity = FishRarity.Epic,     minTier = 4, weight = 1f,  unlock = 12 },
        };

        // Config ĐỀ XUẤT — Sếp duyệt: enabled = true, unlockLevel = 3, còn lại mặc định trong FishingConfig.
        private const int ConfigUnlockLevel = 3;

        // ─────────────────────────────────────────────────────────────────
        //  MENU
        // ─────────────────────────────────────────────────────────────────

        [MenuItem(MenuSetup, false, 10)]
        private static void MenuRun()
        {
            string report = RunSetup(false);
            EditorUtility.DisplayDialog("Hồ Câu — Data", TomTat(report), "OK");
        }

        [MenuItem(MenuOverrideDry, false, 11)]
        private static void MenuOverrideDryRun() { OverrideNumbers(false); }

        [MenuItem(MenuOverrideApply, false, 12)]
        private static void MenuOverrideApplyRun()
        {
            if (!EditorUtility.DisplayDialog("Ghi đè số đề xuất", "Sẽ GHI ĐÈ giá/độ bền/trọng số... của 4 cần + 10 cá + config về bảng ĐỀ XUẤT trong tool.\nSố Sếp đã chỉnh tay sẽ MẤT. Chạy DRY-RUN trước để xem khác biệt.", "Ghi đè", "Huỷ")) { return; }
            OverrideNumbers(true);
        }

        /// <summary>Lõi chạy được từ tool ★. quiet = true: không dialog, không đổi Selection. Trả báo cáo text.</summary>
        public static string RunSetup(bool quiet)
        {
            var report = new StringBuilder();
            report.AppendLine("── DATA HỒ CÂU ──");

            EnsureFolder(FishingIds.DataRoot);
            EnsureFolder(FishFolder);
            EnsureFolder(RodFolder);
            EnsureFolder(FishingIds.ResourcesFolder);

            int created = 0, updated = 0;

            // 1. Config
            bool cfgNew;
            var cfg = LoadOrCreate<FishingConfig>(ConfigPath, out cfgNew);
            if (cfgNew)
            {
                cfg.enabled = true;               // ĐỀ XUẤT — Sếp duyệt
                cfg.unlockLevel = ConfigUnlockLevel;
                created++;
            }
            else { updated++; }
            EditorUtility.SetDirty(cfg);
            report.AppendLine((cfgNew ? "+ TẠO " : "· giữ ") + ConfigPath + " (enabled=" + cfg.enabled + ", unlockLevel=" + cfg.unlockLevel.ToString(CultureInfo.InvariantCulture) + ")");

            // 2. Cần
            var rods = new List<RodData>();
            for (int i = 0; i < Rods.Length; i++)
            {
                bool isNew;
                string path = RodFolder + "/Rod_" + Rods[i].itemId + ".asset";
                var rod = LoadOrCreate<RodData>(path, out isNew);
                if (isNew) { ApplyRod(rod, Rods[i]); created++; }
                else { FillEmptyRod(rod, Rods[i]); updated++; }
                EditorUtility.SetDirty(rod);
                rods.Add(rod);
                report.AppendLine((isNew ? "+ TẠO " : "· giữ ") + path);
            }

            // 3. Cá
            var fishes = new List<FishData>();
            for (int i = 0; i < Fishes.Length; i++)
            {
                bool isNew;
                string path = FishFolder + "/Fish_" + Fishes[i].id + ".asset";
                var fish = LoadOrCreate<FishData>(path, out isNew);
                if (isNew) { ApplyFish(fish, Fishes[i]); created++; }
                else { FillEmptyFish(fish, Fishes[i]); updated++; }
                EditorUtility.SetDirty(fish);
                fishes.Add(fish);
                report.AppendLine((isNew ? "+ TẠO " : "· giữ ") + path);
            }

            // 4. Database
            bool dbNew;
            var db = LoadOrCreate<FishingDatabase>(DatabasePath, out dbNew);
            if (dbNew) { created++; } else { updated++; }
            WireDatabase(db, cfg, fishes, rods, report);
            EditorUtility.SetDirty(db);
            report.AppendLine((dbNew ? "+ TẠO " : "· cập nhật ") + DatabasePath);

            AssetDatabase.SaveAssets();
            FishingDatabase.ResetCache();

            report.AppendLine("Tổng: " + created.ToString(CultureInfo.InvariantCulture) + " asset mới, " + updated.ToString(CultureInfo.InvariantCulture) + " asset giữ/cập nhật wire.");
            report.AppendLine("Số trên asset đã có KHÔNG bị đổi. Muốn về số đề xuất: menu 1b (DRY-RUN) → 1c (APPLY).");
            Debug.Log(FishingIds.SetupLogTag + " Data:\n" + report);

            if (!quiet)
            {
                var dbAsset = AssetDatabase.LoadAssetAtPath<FishingDatabase>(DatabasePath);
                if (dbAsset != null) { Selection.activeObject = dbAsset; EditorGUIUtility.PingObject(dbAsset); }
            }
            return report.ToString();
        }

        /// <summary>Ghi đè số đề xuất lên asset ĐÃ CÓ. dryRun = chỉ liệt kê khác biệt.</summary>
        public static string OverrideNumbers(bool apply)
        {
            var report = new StringBuilder();
            report.AppendLine(apply ? "── GHI ĐÈ SỐ ĐỀ XUẤT (APPLY) ──" : "── GHI ĐÈ SỐ ĐỀ XUẤT (DRY-RUN, không ghi) ──");
            int diff = 0;

            var cfg = AssetDatabase.LoadAssetAtPath<FishingConfig>(ConfigPath);
            if (cfg != null)
            {
                if (!cfg.enabled) { diff++; report.AppendLine("Config.enabled false → true"); if (apply) { cfg.enabled = true; } }
                if (cfg.unlockLevel != ConfigUnlockLevel) { diff++; report.AppendLine("Config.unlockLevel " + cfg.unlockLevel + " → " + ConfigUnlockLevel); if (apply) { cfg.unlockLevel = ConfigUnlockLevel; } }
                if (apply) { EditorUtility.SetDirty(cfg); }
            }
            else { report.AppendLine("! Chưa có " + ConfigPath + " — chạy menu 1 trước."); }

            for (int i = 0; i < Rods.Length; i++)
            {
                string path = RodFolder + "/Rod_" + Rods[i].itemId + ".asset";
                var rod = AssetDatabase.LoadAssetAtPath<RodData>(path);
                if (rod == null) { report.AppendLine("! Thiếu " + path); continue; }
                var d = Rods[i];
                int before = diff;
                diff += Cmp(report, rod.name, "goldPrice", rod.goldPrice, d.gold);
                diff += Cmp(report, rod.name, "diamondPrice", rod.diamondPrice, d.gems);
                diff += Cmp(report, rod.name, "durabilityCasts", rod.durabilityCasts, d.dur);
                diff += Cmp(report, rod.name, "catchChanceBonus", rod.catchChanceBonus, d.bonus);
                diff += Cmp(report, rod.name, "biteWaitMultiplier", rod.biteWaitMultiplier, d.biteWait);
                diff += Cmp(report, rod.name, "rareWeightMultiplier", rod.rareWeightMultiplier, d.rare);
                diff += Cmp(report, rod.name, "biteWindowBonusSeconds", rod.biteWindowBonusSeconds, d.windowBonus);
                diff += Cmp(report, rod.name, "unlockLevel", rod.unlockLevel, d.unlock);
                diff += Cmp(report, rod.name, "tier", rod.tier, d.tier);
                if (apply && diff > before) { ApplyRod(rod, d); EditorUtility.SetDirty(rod); }
            }

            for (int i = 0; i < Fishes.Length; i++)
            {
                string path = FishFolder + "/Fish_" + Fishes[i].id + ".asset";
                var fish = AssetDatabase.LoadAssetAtPath<FishData>(path);
                if (fish == null) { report.AppendLine("! Thiếu " + path); continue; }
                var d = Fishes[i];
                int before = diff;
                diff += Cmp(report, fish.name, "sellPrice", fish.sellPrice, d.price);
                diff += Cmp(report, fish.name, "rarity", (int)fish.rarity, (int)d.rarity);
                diff += Cmp(report, fish.name, "minRodTier", fish.minRodTier, d.minTier);
                diff += Cmp(report, fish.name, "spawnWeight", fish.spawnWeight, d.weight);
                diff += Cmp(report, fish.name, "unlockLevel", fish.unlockLevel, d.unlock);
                if (apply && diff > before) { ApplyFish(fish, d); EditorUtility.SetDirty(fish); }
            }

            if (apply) { AssetDatabase.SaveAssets(); FishingDatabase.ResetCache(); }
            report.AppendLine(diff == 0 ? "Không có khác biệt — asset đang đúng bảng đề xuất." : (apply ? "Đã ghi đè " : "Sẽ ghi đè ") + diff.ToString(CultureInfo.InvariantCulture) + " giá trị.");
            Debug.Log(FishingIds.SetupLogTag + " " + report);
            EditorUtility.DisplayDialog("Hồ Câu — Ghi đè số đề xuất", TomTat(report.ToString()), "OK");
            return report.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Áp bảng
        // ─────────────────────────────────────────────────────────────────

        private static void ApplyRod(RodData rod, RodDef d)
        {
            rod.itemID = d.itemId;
            rod.itemName = d.name;
            rod.goldPrice = d.gold;
            rod.diamondPrice = d.gems;
            rod.tier = d.tier;
            rod.unlockLevel = d.unlock;
            rod.durabilityCasts = d.dur;
            rod.catchChanceBonus = d.bonus;
            rod.biteWaitMultiplier = d.biteWait;
            rod.rareWeightMultiplier = d.rare;
            rod.biteWindowBonusSeconds = d.windowBonus;
        }

        /// <summary>Asset đã có: chỉ điền id/tên nếu rỗng, tier nếu 0. Không đụng số.</summary>
        private static void FillEmptyRod(RodData rod, RodDef d)
        {
            if (string.IsNullOrEmpty(rod.itemID)) { rod.itemID = d.itemId; }
            if (string.IsNullOrEmpty(rod.itemName)) { rod.itemName = d.name; }
            if (rod.tier <= 0) { rod.tier = d.tier; }
        }

        private static void ApplyFish(FishData fish, FishDef d)
        {
            fish.fishId = d.id;
            fish.displayName = d.name;
            fish.sellPrice = d.price;
            fish.rarity = d.rarity;
            fish.minRodTier = d.minTier;
            fish.spawnWeight = d.weight;
            fish.unlockLevel = d.unlock;
        }

        private static void FillEmptyFish(FishData fish, FishDef d)
        {
            if (string.IsNullOrEmpty(fish.fishId) || fish.fishId == "fish_") { fish.fishId = d.id; }
            if (string.IsNullOrEmpty(fish.displayName)) { fish.displayName = d.name; }
        }

        private static void WireDatabase(FishingDatabase db, FishingConfig cfg, List<FishData> fishes, List<RodData> rods, StringBuilder report)
        {
            if (db.config == null) { db.config = cfg; report.AppendLine("  ✓ database.config ← FishingConfig"); }

            if (db.fishes == null) { db.fishes = new List<FishData>(); }
            db.fishes.RemoveAll(f => f == null);
            int addedFish = 0;
            for (int i = 0; i < fishes.Count; i++)
            {
                if (!db.fishes.Contains(fishes[i])) { db.fishes.Add(fishes[i]); addedFish++; }
            }

            if (db.rods == null) { db.rods = new List<RodData>(); }
            db.rods.RemoveAll(r => r == null);
            int addedRod = 0;
            for (int i = 0; i < rods.Count; i++)
            {
                if (!db.rods.Contains(rods[i])) { db.rods.Add(rods[i]); addedRod++; }
            }
            db.rods.Sort((a, b) => a.tier.CompareTo(b.tier));

            if (db.characters == null) { db.characters = new List<FishingCharacterDef>(); }
            db.characters.RemoveAll(c => c == null);
            int addedChar = 0;
            addedChar += EnsureCharacter(db, FishingIds.CharacterF, "Cô gái") ? 1 : 0;
            addedChar += EnsureCharacter(db, FishingIds.CharacterM, "Cậu bé") ? 1 : 0;

            report.AppendLine("  ✓ database: +" + addedFish.ToString(CultureInfo.InvariantCulture) + " cá, +" + addedRod.ToString(CultureInfo.InvariantCulture) + " cần, +" + addedChar.ToString(CultureInfo.InvariantCulture) + " nhân vật (tổng " + db.fishes.Count + "/" + db.rods.Count + "/" + db.characters.Count + ")");
        }

        private static bool EnsureCharacter(FishingDatabase db, string id, string displayName)
        {
            if (db.FindCharacter(id) != null) { return false; }
            db.characters.Add(new FishingCharacterDef { characterId = id, displayName = displayName });
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private static T LoadOrCreate<T>(string path, out bool isNew) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static int Cmp(StringBuilder report, string owner, string field, int cur, int want)
        {
            if (cur == want) { return 0; }
            report.AppendLine("  " + owner + "." + field + ": " + cur.ToString(CultureInfo.InvariantCulture) + " → " + want.ToString(CultureInfo.InvariantCulture));
            return 1;
        }

        private static int Cmp(StringBuilder report, string owner, string field, float cur, float want)
        {
            if (Mathf.Approximately(cur, want)) { return 0; }
            report.AppendLine("  " + owner + "." + field + ": " + cur.ToString("0.###", CultureInfo.InvariantCulture) + " → " + want.ToString("0.###", CultureInfo.InvariantCulture));
            return 1;
        }

        private static string TomTat(string report)
        {
            return report.Length > 1400 ? report.Substring(0, 1400) + "\n… (xem Console)" : report;
        }

        /// <summary>Tạo folder lồng nhau trong Assets nếu chưa có (mẫu DecorStageArtTool.EnsureFolder).</summary>
        public static bool EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder)) { return false; }
            if (AssetDatabase.IsValidFolder(folder)) { return true; }
            string parent = Path.GetDirectoryName(folder);
            if (parent != null) { parent = parent.Replace('\\', '/'); }
            string leaf = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) { return false; }
            if (!EnsureFolder(parent)) { return false; }
            string guid = AssetDatabase.CreateFolder(parent, leaf);
            return !string.IsNullOrEmpty(guid) && AssetDatabase.IsValidFolder(folder);
        }
    }
}
