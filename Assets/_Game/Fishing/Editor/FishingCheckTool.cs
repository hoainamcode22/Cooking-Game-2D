using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tools/Farm Game/Test/Check Fishing — PASS/FAIL từng dòng cho module Hồ Câu. CHỦ FILE: Dev D.
    /// Kiểm: database ở Resources · config.enabled · 4 rod tier 1..4 · rod có itemID · fishId bắt đầu "fish_" · không trùng id ·
    /// 24 PNG · 2 prefab + controller đủ 4 param · scene trong Build Settings · mỗi .cs trong Scripts/ chỉ 1 type top-level, tên = tên file.
    /// [Dev B 09/09] thêm: (1b) 3 asset công cụ tab Shop (rìu/búa/kéo) — CHỈ kiểm khi thư mục Data/Tools đã có (tool 7 đã chạy), chưa có thì PASS "bỏ qua";
    /// (2b) Sheet_Fishing.png từng nhân vật — có thì controller phải có 4 state Fishing_{dir} (chạy lại menu 2), chưa có thì PASS "chờ art".
    /// In tổng PASS/FAIL; Debug.LogError nếu có FAIL.
    /// </summary>
    public static class FishingCheckTool
    {
        private const string Menu = "Tools/Farm Game/Test/Check Fishing";
        private const string ScriptsRoot = FishingIds.ModuleRoot + "/Scripts";

        /// <summary>Khai báo type top-level: indent ≤ 4 khoảng trắng (type lồng trong class có indent ≥ 8).</summary>
        private static readonly Regex TypeDecl = new Regex(@"^ {0,4}(public\s+|internal\s+)?(static\s+|abstract\s+|sealed\s+|partial\s+)*(class|struct|enum|interface)\s+(\w+)", RegexOptions.Multiline);

        private static readonly string[] Characters = { FishingIds.CharacterF, FishingIds.CharacterM };
        private static readonly string[] Directions = { "down", "left", "right", "up" };

        [MenuItem(Menu, false, 200)]
        public static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;

            // 1. Database
            var db = AssetDatabase.LoadAssetAtPath<FishingDatabase>(FishingDataSetupTool.DatabasePath);
            Check(sb, db != null, "FishingDatabase tồn tại ở " + FishingDataSetupTool.DatabasePath, ref pass, ref fail);
            if (db != null)
            {
                Check(sb, db.config != null, "database.config đã gán", ref pass, ref fail);
                Check(sb, db.config != null && db.config.enabled, "config.enabled = true", ref pass, ref fail);

                // Rod
                for (int tier = 1; tier <= 4; tier++)
                {
                    Check(sb, db.FindRodByTier(tier) != null, "Có cần tier " + tier.ToString(CultureInfo.InvariantCulture), ref pass, ref fail);
                }
                var rodIds = new HashSet<string>();
                bool rodMissingId = false, rodDupId = false;
                for (int i = 0; i < db.rods.Count; i++)
                {
                    var r = db.rods[i];
                    if (r == null) { rodMissingId = true; continue; }
                    if (string.IsNullOrEmpty(r.itemID)) { rodMissingId = true; continue; }
                    if (!rodIds.Add(r.itemID)) { rodDupId = true; }
                }
                Check(sb, !rodMissingId, "Không rod thiếu itemID / null (" + db.rods.Count + " rod)", ref pass, ref fail);
                Check(sb, !rodDupId, "Không trùng itemID cần", ref pass, ref fail);

                // Fish
                var fishIds = new HashSet<string>();
                var badPrefix = new List<string>();
                var dup = new List<string>();
                int nullFish = 0;
                for (int i = 0; i < db.fishes.Count; i++)
                {
                    var f = db.fishes[i];
                    if (f == null) { nullFish++; continue; }
                    if (string.IsNullOrEmpty(f.fishId) || !f.fishId.StartsWith("fish_", System.StringComparison.Ordinal) || f.fishId == "fish_") { badPrefix.Add(f.name); }
                    if (!string.IsNullOrEmpty(f.fishId) && !fishIds.Add(f.fishId)) { dup.Add(f.fishId); }
                }
                Check(sb, nullFish == 0, "Không FishData null trong database (" + db.fishes.Count + " cá)", ref pass, ref fail);
                Check(sb, badPrefix.Count == 0, "Mọi fishId bắt đầu \"fish_\"" + (badPrefix.Count > 0 ? " — sai: " + string.Join(", ", badPrefix) : ""), ref pass, ref fail);
                Check(sb, dup.Count == 0, "Không trùng fishId" + (dup.Count > 0 ? " — trùng: " + string.Join(", ", dup) : ""), ref pass, ref fail);
                Check(sb, db.fishes.Count >= 10, "≥ 10 loài cá (" + db.fishes.Count + ")", ref pass, ref fail);

                // Characters
                for (int c = 0; c < Characters.Length; c++)
                {
                    var def = db.FindCharacter(Characters[c]);
                    Check(sb, def != null && def.prefab != null, "database.characters[" + Characters[c] + "].prefab đã wire", ref pass, ref fail);
                    Check(sb, def != null && def.previewSprite != null, "database.characters[" + Characters[c] + "].previewSprite đã wire", ref pass, ref fail);
                }
            }

            // 1b. [Dev B 09/09] Công cụ tab Shop (rìu/búa/kéo) — chỉ kiểm khi tool 7 đã tạo thư mục Data/Tools
            CheckToolAssets(sb, ref pass, ref fail);

            // 2. 24 PNG
            for (int c = 0; c < Characters.Length; c++)
            {
                var missing = new List<string>();
                for (int d = 0; d < Directions.Length; d++)
                {
                    for (int f = 1; f <= 3; f++)
                    {
                        string p = FishingPlayerAnimSetupTool.PngPath(Characters[c], Directions[d], f);
                        if (!File.Exists(p)) { missing.Add(Path.GetFileName(p)); }
                    }
                }
                Check(sb, missing.Count == 0, "12 PNG " + Characters[c] + (missing.Count > 0 ? " — thiếu: " + string.Join(", ", missing) : ""), ref pass, ref fail);
            }

            // 2b. [Dev B 09/09] Sheet cầm cần — có sheet thì controller phải có 4 state Fishing_{dir}; chưa có thì "chờ art" (PASS)
            for (int c = 0; c < Characters.Length; c++)
            {
                string sheet = FishingPlayerAnimSetupTool.SheetPath(Characters[c]);
                if (!File.Exists(sheet))
                {
                    Check(sb, true, FishingPlayerAnimSetupTool.FishingSheetFileName + " " + Characters[c] + ": chưa có — chờ art (animator giữ 8 state)", ref pass, ref fail);
                    continue;
                }
                string thieuFishing;
                bool coState = FishingPlayerAnimSetupTool.ControllerCoStateFishing(FishingPlayerAnimSetupTool.ControllerPath(Characters[c]), out thieuFishing);
                Check(sb, coState, FishingPlayerAnimSetupTool.FishingSheetFileName + " " + Characters[c] + " có → controller có 4 state Fishing_*" + (coState ? "" : " — thiếu: " + thieuFishing + " (chạy lại menu 2)"), ref pass, ref fail);
            }

            // 3. Prefab + controller
            for (int c = 0; c < Characters.Length; c++)
            {
                string prefabPath = FishingPlayerAnimSetupTool.PrefabPath(Characters[c]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Check(sb, prefab != null, "Prefab " + prefabPath, ref pass, ref fail);
                if (prefab != null)
                {
                    Check(sb, prefab.GetComponent<FishingPlayerController>() != null, "  " + Characters[c] + ": có FishingPlayerController", ref pass, ref fail);
                    Check(sb, prefab.GetComponent<FishingYSort>() != null, "  " + Characters[c] + ": có FishingYSort", ref pass, ref fail);
                    Check(sb, prefab.GetComponent<Rigidbody2D>() != null && prefab.GetComponent<Collider2D>() != null, "  " + Characters[c] + ": có Rigidbody2D + Collider2D", ref pass, ref fail);
                    Check(sb, prefab.transform.Find("HeadAnchor") != null && prefab.transform.Find("HandAnchor") != null, "  " + Characters[c] + ": có HeadAnchor + HandAnchor", ref pass, ref fail);
                    var anim = prefab.GetComponent<Animator>();
                    Check(sb, anim != null && anim.runtimeAnimatorController != null, "  " + Characters[c] + ": Animator có controller", ref pass, ref fail);
                }
                string ctrlPath = FishingPlayerAnimSetupTool.ControllerPath(Characters[c]);
                string loi;
                Check(sb, FishingPlayerAnimSetupTool.ControllerHopLe(ctrlPath, out loi), "Controller " + ctrlPath + " hợp lệ" + (string.IsNullOrEmpty(loi) ? "" : " — " + loi), ref pass, ref fail);
                string thieu;
                Check(sb, FishingPlayerAnimSetupTool.ControllerCoDuParam(ctrlPath, out thieu), "  " + Characters[c] + ": controller đủ 4 param DirX/DirY/IsMoving/IsFishing" + (string.IsNullOrEmpty(thieu) ? "" : " — thiếu: " + thieu), ref pass, ref fail);
            }

            // 4. Scene + Build Settings
            Check(sb, File.Exists(FishingIds.FishingScenePath), "File scene " + FishingIds.FishingScenePath, ref pass, ref fail);
            bool inBuild = false;
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++) { if (scenes[i].path == FishingIds.FishingScenePath && scenes[i].enabled) { inBuild = true; break; } }
            Check(sb, inBuild, "Scene có trong Build Settings (enabled)", ref pass, ref fail);

            // 5. 1 type / file, tên type = tên file
            CheckOneTypePerFile(sb, ref pass, ref fail);

            string tong = "Check Fishing: " + pass.ToString(CultureInfo.InvariantCulture) + " PASS / " + fail.ToString(CultureInfo.InvariantCulture) + " FAIL";
            sb.Insert(0, tong + "\n");
            if (fail == 0) { Debug.Log(FishingIds.SetupLogTag + " " + sb); }
            else { Debug.LogError(FishingIds.SetupLogTag + " " + sb); }
            EditorUtility.DisplayDialog("Check Fishing", tong + (fail > 0 ? "\n\nXem Console (lọc FishingSetup) để biết dòng FAIL." : "\n\nTất cả OK."), "OK");
        }

        /// <summary>
        /// [Dev B 09/09] 3 asset ToolData của tab CÔNG CỤ (Tool_axe / Tool_hammer / Tool_scissors).
        /// Thư mục Data/Tools chưa có = tool 7 chưa chạy → 1 dòng PASS "bỏ qua" (tab Shop là phần tuỳ chọn, không ép).
        /// Có thư mục → từng asset phải tồn tại, itemID đúng bảng, bán bằng VÀNG (goldPrice &gt; 0, diamondPrice = 0), unlockLevel ≥ 1, không trùng itemID.
        /// </summary>
        private static void CheckToolAssets(StringBuilder sb, ref int pass, ref int fail)
        {
            if (!AssetDatabase.IsValidFolder(FishingShopToolTabSetupTool.ToolFolder))
            {
                Check(sb, true, "Công cụ tab Shop: chưa chạy tool 7 (" + FishingShopToolTabSetupTool.ToolFolder + " chưa có) — bỏ qua", ref pass, ref fail);
                return;
            }
            var ids = new HashSet<string>();
            bool trungId = false;
            for (int i = 0; i < FishingShopToolTabSetupTool.ToolCount; i++)
            {
                string path = FishingShopToolTabSetupTool.ToolAssetPathAt(i);
                string idMuon = FishingShopToolTabSetupTool.ToolItemIdAt(i);
                var t = AssetDatabase.LoadAssetAtPath<ToolData>(path);
                Check(sb, t != null, "Công cụ " + Path.GetFileName(path) + " tồn tại" + (t == null ? " — chạy lại tool 7 (7. Thêm tab CÔNG CỤ)" : ""), ref pass, ref fail);
                if (t == null) { continue; }
                Check(sb, t.itemID == idMuon, "  " + t.name + ": itemID = \"" + idMuon + "\"" + (t.itemID == idMuon ? "" : " — đang là \"" + t.itemID + "\""), ref pass, ref fail);
                Check(sb, t.goldPrice > 0 && t.diamondPrice == 0, "  " + t.name + ": bán bằng VÀNG (gold " + t.goldPrice.ToString(CultureInfo.InvariantCulture) + ", gem " + t.diamondPrice.ToString(CultureInfo.InvariantCulture) + ")", ref pass, ref fail);
                Check(sb, t.unlockLevel >= 1, "  " + t.name + ": unlockLevel " + t.unlockLevel.ToString(CultureInfo.InvariantCulture) + " ≥ 1", ref pass, ref fail);
                if (!string.IsNullOrEmpty(t.itemID) && !ids.Add(t.itemID)) { trungId = true; }
            }
            Check(sb, !trungId, "Không trùng itemID giữa các công cụ", ref pass, ref fail);
        }

        private static void CheckOneTypePerFile(StringBuilder sb, ref int pass, ref int fail)
        {
            if (!Directory.Exists(ScriptsRoot)) { Check(sb, false, "Thư mục " + ScriptsRoot + " tồn tại", ref pass, ref fail); return; }
            string[] files = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories);
            int bad = 0;
            var chiTiet = new StringBuilder();
            for (int i = 0; i < files.Length; i++)
            {
                string text = File.ReadAllText(files[i]);
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                var names = new List<string>();
                foreach (Match m in TypeDecl.Matches(text)) { names.Add(m.Groups[4].Value); }
                if (names.Count != 1)
                {
                    bad++;
                    chiTiet.AppendLine("    ✖ " + files[i].Replace('\\', '/') + ": " + names.Count.ToString(CultureInfo.InvariantCulture) + " type top-level (" + string.Join(", ", names) + ")");
                }
                else if (names[0] != fileName)
                {
                    bad++;
                    chiTiet.AppendLine("    ✖ " + files[i].Replace('\\', '/') + ": type " + names[0] + " ≠ tên file " + fileName);
                }
            }
            Check(sb, bad == 0, "Mỗi .cs trong Scripts/ có đúng 1 type top-level, tên = tên file (" + files.Length.ToString(CultureInfo.InvariantCulture) + " file)", ref pass, ref fail);
            if (bad > 0) { sb.Append(chiTiet); }
        }

        private static void Check(StringBuilder sb, bool ok, string label, ref int pass, ref int fail)
        {
            if (ok) { pass++; } else { fail++; }
            sb.AppendLine((ok ? "PASS " : "FAIL ") + label);
        }
    }
}
