using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tool 2: nhân vật người chơi (PlayerF / PlayerM) — importer + 8 clip + AnimatorController + prefab. CHỦ FILE: Dev D (Dev B thêm khung "cầm cần" 09/09).
    /// Nguồn đi bộ: Assets/_Game/Fishing/Art/Characters/{Char}/{Char}_{down|left|right|up}_{1..3}.png (canvas F 294x590, M 314x568).
    /// Idle theo hướng: down/up = frame 1, left/right = frame 2. Walk = 4 khoá [bướcA, idle, bướcB, idle] + khoá chốt, 8 fps, loop.
    /// Controller: param DirX/DirY (float), IsMoving/IsFishing (bool); AnyState transitions, IsFishing=true → Idle hướng đó (ưu tiên trước Walk).
    /// Prefab Player_{Char}: SpriteRenderer, Animator, Rigidbody2D, CapsuleCollider2D chân, FishingPlayerController, FishingYSort, con HeadAnchor/HandAnchor.
    ///
    /// [Dev B 09/09] KHUNG ANIMATION "CẦM CẦN" (art chưa có — tool bỏ qua êm, animator giữ y cũ 8 state):
    ///   Nếu tồn tại Assets/_Game/Fishing/Art/Characters/{Char}/Sheet_Fishing.png thì cắt theo lưới 4 hàng × 3 cột:
    ///     hàng = hướng theo ĐÚNG thứ tự Directions[] (down, left, right, up — khớp FacingDir 0..3 và tên file đi bộ), hàng 0 ở TRÊN CÙNG ảnh;
    ///     cột 0 = giơ cần chuẩn bị · cột 1 = quăng · cột 2 = giữ cần chờ cá.
    ///   Kích thước 1 ô = ĐÚNG canvas frame đi bộ của nhân vật đó (đọc từ header PNG {Char}_down_1.png, KHÔNG hard-code):
    ///     F 294x590 → sheet 882x2360 · M 314x568 → sheet 942x2272. Sheet sai cỡ → báo lỗi, KHÔNG cắt, animator giữ y cũ.
    ///   Clip Fishing_{dir}: 2 frame đầu chạy 1 lần (0 → 0.175 s cột 0, 0.175 → 0.35 s cột 1) rồi giữ cột 2 (không loop, Animator kẹp frame cuối).
    ///   Controller: khi có clip, AnyState → Fishing_{dir} thay cho AnyState → Idle_{dir} ở nhánh IsFishing==true; về Idle khi IsFishing==false
    ///   (transition !IsFishing && !IsMoving → Idle đã có sẵn). Tên tham số animator KHÔNG đổi (FishingIds đã chốt). State count 8 hoặc 12.
    ///
    /// Idempotent: clip/controller là artifact (ghi đè), prefab cập nhật tại chỗ (chỉ bổ sung thiếu). Wire database.characters nếu trống.
    /// Sheet đã cắt đúng (12 sprite đúng tên/rect/pivot) thì KHÔNG reimport lại; spriteID giữ theo tên để clip không Missing.
    /// Bẫy đã tránh: SetTextureSettings ghi đè spriteMode (DecorStageArtTool 589-599); StartAssetEditing bao controller (NPCAnimationSetupTool);
    /// đọc kích thước sheet từ header PNG vì Texture2D.width là cỡ SAU import (maxTextureSize 2048 mặc định sẽ co sheet 2360 px xuống).
    /// </summary>
    public static class FishingPlayerAnimSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuSetup = MenuRoot + "2. Nhân vật: importer + anim + prefab (+ cầm cần nếu có sheet)";

        private const float PixelsPerUnit = 100f;
        private const float WalkFps = 8f;
        private const int MinMaxTextureSize = 1024;
        /// <summary>4 Idle + 4 Walk (chưa có sheet cầm cần).</summary>
        public const int ExpectedStateCount = 8;
        /// <summary>4 Idle + 4 Walk + 4 Fishing (đã có Sheet_Fishing.png).</summary>
        public const int ExpectedStateCountWithFishing = 12;

        // ── [Dev B 09/09] Sheet cầm cần ──
        public const string FishingSheetFileName = "Sheet_Fishing.png";
        private const int FishingSheetCols = 3;
        private const int FishingSheetRows = 4;
        /// <summary>frameRate clip cầm cần: 40 để các mốc 0.175 / 0.35 / 0.5 s rơi đúng bội số 1/40 (7, 14, 20 frame).</summary>
        private const float FishingClipFps = 40f;
        private const float FishingCastKeyTime = 0.175f;   // cột 1 (quăng) bắt đầu hiện
        private const float FishingHoldKeyTime = 0.35f;    // cột 2 (giữ cần) bắt đầu hiện — 2 frame đầu tổng ~0.35 s
        private const float FishingClipEndTime = 0.5f;     // khoá chốt độ dài clip (vẫn cột 2, Animator kẹp frame cuối)
        private static readonly string[] FishingColNames = { "raise", "cast", "hold" };

        private static readonly string[] Characters = { FishingIds.CharacterF, FishingIds.CharacterM };
        private static readonly string[] Directions = { "down", "left", "right", "up" };   // khớp FacingDir 0..3
        /// <summary>Frame idle (1-based) theo hướng: down/up = 1, left/right = 2 (đo thật khi cắt).</summary>
        private static readonly int[] IdleFrame1Based = { 1, 2, 2, 1 };

        /// <summary>Số nhân vật dựng OK ở lần chạy gần nhất (tool ★ đọc).</summary>
        public static int LastCharacterCount { get; private set; }

        [MenuItem(MenuSetup, false, 20)]
        private static void MenuRun()
        {
            string report = RunSetup(false);
            EditorUtility.DisplayDialog("Hồ Câu — Nhân vật", report.Length > 1400 ? report.Substring(0, 1400) + "\n… (xem Console)" : report, "OK");
        }

        /// <summary>Lõi chạy được từ tool ★. Trả báo cáo text.</summary>
        public static string RunSetup(bool quiet)
        {
            LastCharacterCount = 0;
            var report = new StringBuilder();
            report.AppendLine("── NHÂN VẬT (importer + anim + prefab) ──");

            if (!AssetDatabase.IsValidFolder(FishingIds.ArtCharactersRoot))
            {
                report.AppendLine("✖ Không thấy thư mục ảnh " + FishingIds.ArtCharactersRoot + " — cần 2 thư mục PlayerF/PlayerM, mỗi thư mục 12 PNG {Char}_{down|left|right|up}_{1..3}.png");
                Debug.LogWarning(FishingIds.SetupLogTag + " " + report);
                return report.ToString();
            }

            FishingDataSetupTool.EnsureFolder(FishingIds.AnimationsRoot);
            FishingDataSetupTool.EnsureFolder(FishingIds.PrefabsRoot);

            // ── GIAI ĐOẠN A: chỉ importer, được phép đóng băng AssetDatabase ──
            var hopLe = new List<string>();
            int reimported = 0;
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int c = 0; c < Characters.Length; c++)
                {
                    string ch = Characters[c];
                    var thieu = new List<string>();
                    for (int d = 0; d < Directions.Length; d++)
                    {
                        for (int f = 1; f <= 3; f++)
                        {
                            string path = PngPath(ch, Directions[d], f);
                            if (!File.Exists(path)) { thieu.Add(Path.GetFileName(path)); continue; }
                            if (ApplyImportSettings(path)) { reimported++; }
                        }
                    }
                    if (thieu.Count > 0)
                    {
                        report.AppendLine("✖ " + ch + ": thiếu " + thieu.Count.ToString(CultureInfo.InvariantCulture) + " ảnh: " + string.Join(", ", thieu) + " — bỏ qua nhân vật này.");
                        continue;
                    }
                    hopLe.Add(ch);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();   // PHẢI Refresh trước khi LoadAssetAtPath<Sprite>
            }
            report.AppendLine("Ảnh đặt lại import setting: " + reimported.ToString(CultureInfo.InvariantCulture) + " file.");

            // ── GIAI ĐOẠN B: (sheet cầm cần) · clip · controller · prefab (ngoài khối đóng băng) ──
            var db = AssetDatabase.LoadAssetAtPath<FishingDatabase>(FishingDataSetupTool.DatabasePath);
            int ok = 0;
            for (int i = 0; i < hopLe.Count; i++)
            {
                string ch = hopLe[i];
                if (!quiet) { EditorUtility.DisplayProgressBar("Hồ Câu — Nhân vật", "Dựng " + ch + " (" + (i + 1) + "/" + hopLe.Count + ")…", (float)i / Mathf.Max(1, hopLe.Count)); }
                try
                {
                    if (BuildCharacter(ch, db, report)) { ok++; }
                }
                catch (System.Exception e)
                {
                    report.AppendLine("✖ " + ch + ": lỗi ngoài dự kiến — " + e.Message);
                    Debug.LogException(e);
                }
            }
            if (!quiet) { EditorUtility.ClearProgressBar(); }

            if (db != null) { EditorUtility.SetDirty(db); }
            AssetDatabase.SaveAssets();
            FishingDatabase.ResetCache();

            LastCharacterCount = ok;
            report.AppendLine("Xong " + ok.ToString(CultureInfo.InvariantCulture) + "/" + Characters.Length.ToString(CultureInfo.InvariantCulture) + " nhân vật. Clip+controller: " + FishingIds.AnimationsRoot + "/{Char}/ · Prefab: " + FishingIds.PrefabsRoot + "/Player_{Char}.prefab");
            Debug.Log(FishingIds.SetupLogTag + " Nhân vật:\n" + report);

            if (!quiet)
            {
                var folder = AssetDatabase.LoadAssetAtPath<Object>(FishingIds.PrefabsRoot);
                if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
            }
            return report.ToString();
        }

        // ─────────────────────────────────────────────────────────────────
        //  1 nhân vật
        // ─────────────────────────────────────────────────────────────────

        private static bool BuildCharacter(string ch, FishingDatabase db, StringBuilder report)
        {
            // Sprite
            var frames = new Sprite[Directions.Length, 3];
            for (int d = 0; d < Directions.Length; d++)
            {
                for (int f = 0; f < 3; f++)
                {
                    string path = PngPath(ch, Directions[d], f + 1);
                    frames[d, f] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (frames[d, f] == null)
                    {
                        report.AppendLine("✖ " + ch + ": không load được sprite " + Path.GetFileName(path) + " (importer chưa xong? chạy lại menu).");
                        return false;
                    }
                }
            }

            // Clip
            string animFolder = FishingIds.AnimationsRoot + "/" + ch;
            FishingDataSetupTool.EnsureFolder(animFolder);
            var idle = new AnimationClip[4];
            var walk = new AnimationClip[4];
            for (int d = 0; d < Directions.Length; d++)
            {
                int idleIdx = IdleFrame1Based[d] - 1;
                Sprite idleSprite = frames[d, idleIdx];
                // 2 frame còn lại là 2 bước chân
                var steps = new List<Sprite>(2);
                for (int f = 0; f < 3; f++) { if (f != idleIdx) { steps.Add(frames[d, f]); } }

                idle[d] = WriteClip(animFolder + "/Idle_" + Directions[d] + ".anim", new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = idleSprite },
                    new ObjectReferenceKeyframe { time = 1f / WalkFps, value = idleSprite },
                }, true, WalkFps);

                float s = 1f / WalkFps;
                walk[d] = WriteClip(animFolder + "/Walk_" + Directions[d] + ".anim", new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = steps[0] },
                    new ObjectReferenceKeyframe { time = s, value = idleSprite },
                    new ObjectReferenceKeyframe { time = s * 2f, value = steps[1] },
                    new ObjectReferenceKeyframe { time = s * 3f, value = idleSprite },
                    new ObjectReferenceKeyframe { time = s * 4f, value = steps[0] },   // khoá chốt độ dài + nối vòng lặp
                }, true, WalkFps);
            }

            // [Dev B 09/09] Clip cầm cần — chỉ khi có Sheet_Fishing.png đúng cỡ; không có thì fishing = null, animator giữ y cũ.
            AnimationClip[] fishing = null;
            Sprite[,] fishFrames = BuildFishingFrames(ch, frames[0, 0], report);
            if (fishFrames != null)
            {
                fishing = new AnimationClip[4];
                for (int d = 0; d < Directions.Length; d++)
                {
                    fishing[d] = WriteClip(animFolder + "/Fishing_" + Directions[d] + ".anim", new[]
                    {
                        new ObjectReferenceKeyframe { time = 0f, value = fishFrames[d, 0] },                    // giơ cần chuẩn bị
                        new ObjectReferenceKeyframe { time = FishingCastKeyTime, value = fishFrames[d, 1] },    // quăng
                        new ObjectReferenceKeyframe { time = FishingHoldKeyTime, value = fishFrames[d, 2] },    // giữ cần chờ cá
                        new ObjectReferenceKeyframe { time = FishingClipEndTime, value = fishFrames[d, 2] },    // khoá chốt: giữ cột 2 tới hết (không loop)
                    }, false, FishingClipFps);
                }
            }

            // Controller
            string ctrlPath = animFolder + "/" + ch + ".controller";
            AnimatorController ctrl = BuildController(ctrlPath, idle, walk, fishing);
            AssetDatabase.SaveAssets();
            string loiCtrl;
            if (!ControllerHopLe(ctrlPath, out loiCtrl))
            {
                report.AppendLine("✖ " + ch + ": controller hỏng sau khi tạo — " + loiCtrl + " (" + ctrlPath + "). Đóng cửa sổ Animator rồi chạy lại.");
                return false;
            }

            // Prefab
            string prefabPath = FishingIds.PrefabsRoot + "/" + FishingIds.PrefabPrefix + ch + ".prefab";
            Sprite idleDown = frames[0, IdleFrame1Based[0] - 1];
            bool prefabNew = BuildOrUpdatePrefab(prefabPath, ch, ctrl, idleDown, report);

            // Wire database.characters
            if (db != null)
            {
                var def = db.FindCharacter(ch);
                if (def != null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (def.prefab == null && prefab != null) { def.prefab = prefab; report.AppendLine("  ✓ database.characters[" + ch + "].prefab ← " + prefabPath); }
                    if (def.previewSprite == null) { def.previewSprite = idleDown; report.AppendLine("  ✓ database.characters[" + ch + "].previewSprite ← " + idleDown.name); }
                }
                else { report.AppendLine("  ! database chưa có entry " + ch + " — chạy menu 1 (Data) rồi chạy lại menu 2."); }
            }
            else { report.AppendLine("  ! Chưa có FishingDatabase — chạy menu 1 (Data) rồi chạy lại menu 2 để wire prefab."); }

            int stateCount = fishing != null ? ExpectedStateCountWithFishing : ExpectedStateCount;
            report.AppendLine("✔ " + ch + ": 4 Idle + 4 Walk" + (fishing != null ? " + 4 Fishing (cầm cần)" : "") + " + controller (4 param, " + stateCount.ToString(CultureInfo.InvariantCulture) + " state) · prefab " + (prefabNew ? "TẠO MỚI" : "cập nhật tại chỗ") + ".");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Importer (frame đi bộ rời)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Single · BottomCenter · PPU 100 · no mip · Bilinear · alphaIsTransparency · maxTextureSize ≥ 1024. Trả true nếu có thay đổi.</summary>
        private static bool ApplyImportSettings(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { return false; }

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);

            bool doi = importer.textureType != TextureImporterType.Sprite
                       || importer.spriteImportMode != SpriteImportMode.Single
                       || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
                       || importer.mipmapEnabled
                       || !importer.alphaIsTransparency
                       || importer.filterMode != FilterMode.Bilinear
                       || importer.maxTextureSize < MinMaxTextureSize
                       || ts.spriteAlignment != (int)SpriteAlignment.BottomCenter
                       || ts.spriteMeshType != SpriteMeshType.FullRect;
            if (!doi) { return false; }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, MinMaxTextureSize);

            // BẪY: ts đọc TRƯỚC khi sửa importer nên còn giữ textureType/spriteMode CŨ → phải đặt lại tường minh.
            ts.textureType = TextureImporterType.Sprite;
            ts.spriteMode = (int)SpriteImportMode.Single;
            ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ts.spriteMeshType = SpriteMeshType.FullRect;
            ts.spritePixelsPerUnit = PixelsPerUnit;
            importer.SetTextureSettings(ts);

            importer.SaveAndReimport();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  [Dev B 09/09] Sheet cầm cần: đọc cỡ → cắt 4x3 → nạp 12 sprite
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Trả [4 hướng, 3 cột] sprite cầm cần, hoặc null nếu CHƯA có sheet (chờ art) / sheet sai cỡ / cắt lỗi.
        /// Kích thước ô = canvas frame đi bộ (đọc header PNG {Char}_down_1.png; hỏng thì lấy rect sprite đi bộ).
        /// </summary>
        private static Sprite[,] BuildFishingFrames(string ch, Sprite walkDown1, StringBuilder report)
        {
            string sheet = SheetPath(ch);
            if (!File.Exists(sheet))
            {
                report.AppendLine("  · " + ch + ": chưa có " + FishingSheetFileName + " — CHỜ ART (đội vẽ giao theo prompt V4), animator giữ " + ExpectedStateCount.ToString(CultureInfo.InvariantCulture) + " state như cũ.");
                return null;
            }

            int frameW, frameH;
            if (!ReadPngSize(PngPath(ch, Directions[0], 1), out frameW, out frameH))
            {
                frameW = Mathf.RoundToInt(walkDown1.rect.width);
                frameH = Mathf.RoundToInt(walkDown1.rect.height);
            }
            int needW = FishingSheetCols * frameW;
            int needH = FishingSheetRows * frameH;

            int sheetW, sheetH;
            if (!ReadPngSize(sheet, out sheetW, out sheetH))
            {
                report.AppendLine("  ✖ " + ch + ": không đọc được kích thước " + FishingSheetFileName + " (không phải PNG hợp lệ?) — bỏ qua cầm cần, animator giữ y cũ.");
                return null;
            }
            if (sheetW != needW || sheetH != needH)
            {
                report.AppendLine("  ✖ " + ch + ": " + FishingSheetFileName + " " + Kich(sheetW, sheetH) + " SAI CỠ — cần đúng " + Kich(needW, needH) + " (3 cột × 4 hàng, mỗi ô " + Kich(frameW, frameH) + " = canvas frame đi bộ). Trả đội vẽ. Animator giữ y cũ.");
                return null;
            }

            if (SheetDaCatDung(sheet, ch, frameW, frameH))
            {
                report.AppendLine("  · " + ch + ": " + FishingSheetFileName + " đã cắt đúng 12 ô " + Kich(frameW, frameH) + " — không reimport.");
            }
            else
            {
                if (!SliceFishingSheet(sheet, ch, frameW, frameH, sheetW, sheetH, report)) { return null; }
                AssetDatabase.Refresh();   // PHẢI Refresh trước khi LoadAllAssetsAtPath lấy sprite con
            }

            var byName = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(sheet);
            for (int i = 0; i < all.Length; i++)
            {
                var sp = all[i] as Sprite;
                if (sp != null && !byName.ContainsKey(sp.name)) { byName[sp.name] = sp; }
            }

            var result = new Sprite[Directions.Length, FishingSheetCols];
            var thieu = new List<string>();
            for (int d = 0; d < Directions.Length; d++)
            {
                for (int c = 0; c < FishingSheetCols; c++)
                {
                    string n = FishingSpriteName(ch, d, c);
                    Sprite sp;
                    if (byName.TryGetValue(n, out sp)) { result[d, c] = sp; }
                    else { thieu.Add(n); }
                }
            }
            if (thieu.Count > 0)
            {
                report.AppendLine("  ✖ " + ch + ": sau khi cắt thiếu " + thieu.Count.ToString(CultureInfo.InvariantCulture) + " sprite con (" + string.Join(", ", thieu) + ") — import có thể chưa xong, chạy lại menu 2. Animator giữ y cũ.");
                return null;
            }

            report.AppendLine("  ✓ " + ch + ": " + FishingSheetFileName + " " + Kich(sheetW, sheetH) + " → 12 ô " + Kich(frameW, frameH) + " (hàng down/left/right/up · cột giơ/quăng/giữ).");
            return result;
        }

        /// <summary>Sheet đã ở chế độ Multiple với đúng 12 sprite (tên, cỡ ô, pivot BottomCenter) chưa? Đúng thì khỏi reimport.</summary>
        private static bool SheetDaCatDung(string sheet, string ch, int frameW, int frameH)
        {
            var importer = AssetImporter.GetAtPath(sheet) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple) { return false; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)) { return false; }

            var byName = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(sheet);
            int soSprite = 0;
            for (int i = 0; i < all.Length; i++)
            {
                var sp = all[i] as Sprite;
                if (sp == null) { continue; }
                soSprite++;
                if (!byName.ContainsKey(sp.name)) { byName[sp.name] = sp; }
            }
            if (soSprite != FishingSheetRows * FishingSheetCols) { return false; }

            for (int d = 0; d < FishingSheetRows; d++)
            {
                for (int c = 0; c < FishingSheetCols; c++)
                {
                    Sprite sp;
                    if (!byName.TryGetValue(FishingSpriteName(ch, d, c), out sp)) { return false; }
                    Rect muon = FishingCellRect(d, c, frameW, frameH);
                    if (Mathf.RoundToInt(sp.rect.x) != Mathf.RoundToInt(muon.x) || Mathf.RoundToInt(sp.rect.y) != Mathf.RoundToInt(muon.y)) { return false; }
                    if (Mathf.RoundToInt(sp.rect.width) != frameW || Mathf.RoundToInt(sp.rect.height) != frameH) { return false; }
                    // pivot của Sprite tính bằng pixel so với rect: BottomCenter = (W/2, 0)
                    if (Mathf.Abs(sp.pivot.x - frameW * 0.5f) > 0.51f || Mathf.Abs(sp.pivot.y) > 0.51f) { return false; }
                }
            }
            return true;
        }

        /// <summary>
        /// Cắt sheet 4 hàng × 3 cột (chép chuẩn hàm SliceSheet trong FishingWaterAnimSetupTool / CharacterSheetSliceTool):
        /// sửa importer → ReadTextureSettings → SetTextureSettings → SaveAndReimport → lấy LẠI importer → SpriteRect qua ISpriteEditorDataProvider.
        /// Hàng 0 (down) nằm TRÊN CÙNG ảnh; gốc toạ độ rect của Unity ở góc dưới-trái nên y = (Rows-1-hàng) × frameH.
        /// </summary>
        private static bool SliceFishingSheet(string sheet, string ch, int frameW, int frameH, int sheetW, int sheetH, StringBuilder report)
        {
            var importer = AssetImporter.GetAtPath(sheet) as TextureImporter;
            if (importer == null)
            {
                // PNG vừa chép vào Assets/ mà chưa có .meta thì GetAtPath trả null — ép import rồi thử lại.
                AssetDatabase.ImportAsset(sheet, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(sheet) as TextureImporter;
            }
            if (importer == null) { report.AppendLine("  ✖ " + ch + ": không đọc được importer của " + FishingSheetFileName); return false; }

            // THỨ TỰ SỐNG CÒN: sửa importer TRƯỚC → mới ReadTextureSettings → chỉ sửa phần riêng của sprite → SetTextureSettings.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            // maxTextureSize phải >= cạnh lớn nhất (sheet F cao 2360 > 2048 mặc định), không thì Unity co ảnh và rect cắt bị lệch.
            importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(sheetW, sheetH)), MinMaxTextureSize, 8192);

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            ts.textureType = TextureImporterType.Sprite;
            ts.spriteMode = (int)SpriteImportMode.Multiple;
            ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ts.spriteMeshType = SpriteMeshType.FullRect;
            ts.spritePixelsPerUnit = PixelsPerUnit;
            importer.SetTextureSettings(ts);
            importer.SaveAndReimport();

            // Lấy LẠI importer sau reimport: provider gắn vào instance cũ thì reimport nuốt mất SpriteRect.
            importer = AssetImporter.GetAtPath(sheet) as TextureImporter;
            if (importer == null) { report.AppendLine("  ✖ " + ch + ": importer biến mất sau reimport: " + sheet); return false; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                report.AppendLine("  ✖ Thiếu package '2D Sprite' (com.unity.2d.sprite) — không lấy được ISpriteEditorDataProvider: " + sheet);
                return false;
            }
            provider.InitSpriteEditorDataProvider();

            // GIỮ NGUYÊN spriteID theo tên: GUID.Generate() vô điều kiện làm clip Fishing_* đứt tham chiếu mỗi lần chạy lại.
            var oldIds = new Dictionary<string, GUID>(System.StringComparer.Ordinal);
            SpriteRect[] existing = provider.GetSpriteRects();
            if (existing != null)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i] == null || string.IsNullOrEmpty(existing[i].name)) { continue; }
                    if (!oldIds.ContainsKey(existing[i].name)) { oldIds[existing[i].name] = existing[i].spriteID; }
                }
            }

            int total = FishingSheetRows * FishingSheetCols;
            var rects = new List<SpriteRect>(total);
            var pairs = new List<SpriteNameFileIdPair>(total);
            int giuLai = 0;
            for (int d = 0; d < FishingSheetRows; d++)
            {
                for (int c = 0; c < FishingSheetCols; c++)
                {
                    string spriteName = FishingSpriteName(ch, d, c);
                    GUID id;
                    if (!oldIds.TryGetValue(spriteName, out id) || id.Empty()) { id = GUID.Generate(); }
                    else { giuLai++; }
                    rects.Add(new SpriteRect
                    {
                        name = spriteName,
                        rect = FishingCellRect(d, c, frameW, frameH),
                        alignment = SpriteAlignment.BottomCenter,
                        pivot = new Vector2(0.5f, 0f),
                        border = Vector4.zero,
                        spriteID = id,
                    });
                    pairs.Add(new SpriteNameFileIdPair(spriteName, id));
                }
            }

            // Unity 6: SpriteRect đi qua SpriteDataProvider, KHÔNG dùng importer.spritesheet (đã lỗi thời).
            // Phải đặt CẢ bảng tên↔fileID, không thì lần cắt sau internalID đổi ⇒ clip thành Missing.
            provider.SetSpriteRects(rects.ToArray());
            var nameProv = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProv != null) { nameProv.SetNameFileIdPairs(pairs); }
            provider.Apply();

            var applied = provider.targetObject as AssetImporter;
            if (applied != null) { applied.SaveAndReimport(); }
            else { importer.SaveAndReimport(); }

            report.AppendLine("  ✓ Cắt " + ch + "/" + FishingSheetFileName + ": " + total.ToString(CultureInfo.InvariantCulture) + " ô " + Kich(frameW, frameH) + " (ảnh " + Kich(sheetW, sheetH) + "), pivot BottomCenter, PPU " + PixelsPerUnit.ToString("0", CultureInfo.InvariantCulture) + ", giữ lại " + giuLai.ToString(CultureInfo.InvariantCulture) + " spriteID cũ.");
            return true;
        }

        /// <summary>Rect ô (hàng d, cột c) trong toạ độ Unity (gốc dưới-trái). Hàng 0 ở trên cùng ảnh.</summary>
        private static Rect FishingCellRect(int d, int c, int frameW, int frameH)
        {
            return new Rect(c * frameW, (FishingSheetRows - 1 - d) * frameH, frameW, frameH);
        }

        /// <summary>Tên sprite con: {Char}_fishing_{dir}_{raise|cast|hold}.</summary>
        private static string FishingSpriteName(string ch, int d, int c)
        {
            return ch + "_fishing_" + Directions[d] + "_" + FishingColNames[c];
        }

        /// <summary>
        /// Đọc width/height từ header PNG (IHDR, big-endian ở byte 16..23) — KHÔNG phụ thuộc import setting.
        /// Texture2D.width/height là cỡ SAU import: sheet cao 2360 px với maxTextureSize 2048 mặc định sẽ bị co, kiểm cỡ bằng texture là sai.
        /// </summary>
        private static bool ReadPngSize(string path, out int w, out int h)
        {
            w = 0; h = 0;
            try
            {
                if (!File.Exists(path)) { return false; }
                byte[] b;
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    b = new byte[24];
                    int read = 0;
                    while (read < b.Length)
                    {
                        int n = fs.Read(b, read, b.Length - read);
                        if (n <= 0) { break; }
                        read += n;
                    }
                    if (read < 24) { return false; }
                }
                // Chữ ký PNG: 89 50 4E 47 0D 0A 1A 0A; chunk đầu phải là IHDR ("IHDR" ở byte 12..15).
                if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) { return false; }
                if (b[12] != (byte)'I' || b[13] != (byte)'H' || b[14] != (byte)'D' || b[15] != (byte)'R') { return false; }
                w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return w > 0 && h > 0;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static string Kich(int w, int h)
        {
            return w.ToString(CultureInfo.InvariantCulture) + "x" + h.ToString(CultureInfo.InvariantCulture);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Clip
        // ─────────────────────────────────────────────────────────────────

        private static AnimationClip WriteClip(string path, ObjectReferenceKeyframe[] keys, bool loop, float fps)
        {
            var clip = new AnimationClip { frameRate = fps };
            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, s);

            var cu = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (cu != null)
            {
                // Ghi đè NỘI DUNG để controller/prefab đang trỏ tới không bị Missing.
                EditorUtility.CopySerialized(clip, cu);
                Object.DestroyImmediate(clip);
                EditorUtility.SetDirty(cu);
                return cu;
            }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Controller
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Controller là artifact: có sẵn thì XOÁ rồi tạo lại (khỏi dồn state trùng); prefab gán lại ngay sau.
        /// fishing == null (chưa có sheet) → 8 state, nhánh IsFishing==true trỏ về Idle_{dir} như cũ.
        /// fishing != null → thêm 4 state Fishing_{dir}, nhánh IsFishing==true trỏ vào đó; về Idle khi IsFishing==false (transition sẵn có).
        /// </summary>
        private static AnimatorController BuildController(string path, AnimationClip[] idle, AnimationClip[] walk, AnimationClip[] fishing)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) { AssetDatabase.DeleteAsset(path); }

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter(FishingIds.AnimParamDirX, AnimatorControllerParameterType.Float);
            ctrl.AddParameter(FishingIds.AnimParamDirY, AnimatorControllerParameterType.Float);
            ctrl.AddParameter(FishingIds.AnimParamIsMoving, AnimatorControllerParameterType.Bool);
            ctrl.AddParameter(FishingIds.AnimParamIsFishing, AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            var idleStates = new AnimatorState[4];
            var walkStates = new AnimatorState[4];
            AnimatorState[] fishStates = fishing != null ? new AnimatorState[4] : null;
            for (int d = 0; d < 4; d++)
            {
                idleStates[d] = sm.AddState("Idle_" + Directions[d]);
                idleStates[d].motion = idle[d];
                walkStates[d] = sm.AddState("Walk_" + Directions[d]);
                walkStates[d].motion = walk[d];
                if (fishStates != null)
                {
                    fishStates[d] = sm.AddState("Fishing_" + Directions[d]);
                    fishStates[d].motion = fishing[d];
                }
            }
            sm.defaultState = idleStates[0];

            // Thứ tự thêm = độ ưu tiên AnyState: IsFishing → Fishing (hoặc Idle nếu chưa có clip) hướng đó TRƯỚC, rồi Walk, rồi Idle.
            for (int d = 0; d < 4; d++) { AddTransition(sm, fishStates != null ? fishStates[d] : idleStates[d], d, moving: false, fishing: true); }
            for (int d = 0; d < 4; d++) { AddTransition(sm, walkStates[d], d, moving: true, fishing: false); }
            for (int d = 0; d < 4; d++) { AddTransition(sm, idleStates[d], d, moving: false, fishing: false); }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>
        /// AnyState → state. down: DirY &lt; -0.5 · up: DirY &gt; 0.5 · left: DirX &lt; -0.5 · right: DirX &gt; 0.5 (ngang thêm chặn |DirY| &lt; 0.5).
        /// fishing=true: chỉ điều kiện IsFishing + hướng (bỏ qua IsMoving). fishing=false: IsFishing == false + IsMoving đúng chiều.
        /// canTransitionToSelf=false nên state Fishing_{dir} (clip không loop) không bị AnyState kích lại từ đầu mỗi frame.
        /// </summary>
        private static void AddTransition(AnimatorStateMachine sm, AnimatorState state, int dirIndex, bool moving, bool fishing)
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(state);
            t.hasExitTime = false;
            t.duration = 0f;
            t.canTransitionToSelf = false;

            if (fishing)
            {
                t.AddCondition(AnimatorConditionMode.If, 0f, FishingIds.AnimParamIsFishing);
            }
            else
            {
                t.AddCondition(AnimatorConditionMode.IfNot, 0f, FishingIds.AnimParamIsFishing);
                t.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, FishingIds.AnimParamIsMoving);
            }

            switch (dirIndex)
            {
                case 0:
                    t.AddCondition(AnimatorConditionMode.Less, -0.5f, FishingIds.AnimParamDirY);
                    break;
                case 1:
                    t.AddCondition(AnimatorConditionMode.Less, -0.5f, FishingIds.AnimParamDirX);
                    t.AddCondition(AnimatorConditionMode.Less, 0.5f, FishingIds.AnimParamDirY);
                    t.AddCondition(AnimatorConditionMode.Greater, -0.5f, FishingIds.AnimParamDirY);
                    break;
                case 2:
                    t.AddCondition(AnimatorConditionMode.Greater, 0.5f, FishingIds.AnimParamDirX);
                    t.AddCondition(AnimatorConditionMode.Less, 0.5f, FishingIds.AnimParamDirY);
                    t.AddCondition(AnimatorConditionMode.Greater, -0.5f, FishingIds.AnimParamDirY);
                    break;
                default:
                    t.AddCondition(AnimatorConditionMode.Greater, 0.5f, FishingIds.AnimParamDirY);
                    break;
            }
        }

        /// <summary>Xác minh controller dùng được (statemachine Base Layer tồn tại, 8 state chưa có cầm cần hoặc 12 state đã có). FishingCheckTool dùng chung.</summary>
        public static bool ControllerHopLe(string path, out string loi)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { loi = "không load được controller"; return false; }
            if (ctrl.layers == null || ctrl.layers.Length == 0) { loi = "không có layer"; return false; }
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            if (sm == null) { loi = "Base Layer THIẾU statemachine"; return false; }
            int n = sm.states != null ? sm.states.Length : 0;
            if (n != ExpectedStateCount && n != ExpectedStateCountWithFishing) { loi = "có " + n + " state, cần " + ExpectedStateCount + " (chưa có sheet cầm cần) hoặc " + ExpectedStateCountWithFishing + " (đã có)"; return false; }
            loi = string.Empty;
            return true;
        }

        /// <summary>Controller có đủ 4 param DirX/DirY/IsMoving/IsFishing không (FishingCheckTool).</summary>
        public static bool ControllerCoDuParam(string path, out string thieu)
        {
            thieu = string.Empty;
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { thieu = "không load được controller"; return false; }
            string[] can = { FishingIds.AnimParamDirX, FishingIds.AnimParamDirY, FishingIds.AnimParamIsMoving, FishingIds.AnimParamIsFishing };
            var missing = new List<string>();
            for (int i = 0; i < can.Length; i++)
            {
                bool found = false;
                var ps = ctrl.parameters;
                for (int j = 0; j < ps.Length; j++) { if (ps[j].name == can[i]) { found = true; break; } }
                if (!found) { missing.Add(can[i]); }
            }
            thieu = string.Join(", ", missing);
            return missing.Count == 0;
        }

        /// <summary>[Dev B 09/09] Controller đã có đủ 4 state Fishing_{dir} chưa (FishingCheckTool dùng khi Sheet_Fishing.png tồn tại).</summary>
        public static bool ControllerCoStateFishing(string path, out string thieu)
        {
            thieu = string.Empty;
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { thieu = "không load được controller"; return false; }
            if (ctrl.layers == null || ctrl.layers.Length == 0 || ctrl.layers[0].stateMachine == null) { thieu = "không có statemachine"; return false; }
            ChildAnimatorState[] states = ctrl.layers[0].stateMachine.states;
            var missing = new List<string>();
            for (int d = 0; d < Directions.Length; d++)
            {
                string muon = "Fishing_" + Directions[d];
                bool found = false;
                for (int i = 0; i < states.Length; i++) { if (states[i].state != null && states[i].state.name == muon) { found = true; break; } }
                if (!found) { missing.Add(muon); }
            }
            thieu = string.Join(", ", missing);
            return missing.Count == 0;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Prefab
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Tạo mới hoặc cập nhật tại chỗ (chỉ bổ sung thiếu). Trả true nếu vừa TẠO MỚI.</summary>
        private static bool BuildOrUpdatePrefab(string prefabPath, string ch, AnimatorController ctrl, Sprite idleDown, StringBuilder report)
        {
            bool taoMoi = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null;
            GameObject root = taoMoi ? new GameObject(FishingIds.PrefabPrefix + ch) : PrefabUtility.LoadPrefabContents(prefabPath);

            // Kích thước sprite ở scale 1 (unit). Controller sẽ scale theo cfg.playerWorldHeight nên để scale 1 ở prefab.
            float h = idleDown != null ? idleDown.rect.height / Mathf.Max(1f, idleDown.pixelsPerUnit) : 5.9f;
            float w = idleDown != null ? idleDown.rect.width / Mathf.Max(1f, idleDown.pixelsPerUnit) : 2.9f;

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) { sr = root.AddComponent<SpriteRenderer>(); }
            if (sr.sprite == null) { sr.sprite = idleDown; }
            sr.sortingLayerName = TouristSortingLayers.ResolveOrOverride(taoMoi ? "Objects" : sr.sortingLayerName, TouristSortingLayers.Visitor);

            var anim = root.GetComponent<Animator>();
            if (anim == null) { anim = root.AddComponent<Animator>(); }
            anim.runtimeAnimatorController = ctrl;   // controller là artifact vừa tạo lại → luôn gán lại
            anim.applyRootMotion = false;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var rb = root.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = root.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            }

            if (root.GetComponent<CapsuleCollider2D>() == null)
            {
                var cap = root.AddComponent<CapsuleCollider2D>();
                cap.direction = CapsuleDirection2D.Horizontal;   // rộng hơn cao → nằm ngang
                cap.size = new Vector2(0.45f * h, 0.28f * h);
                cap.offset = new Vector2(0f, 0.18f * h);
            }

            // Component Dev A — KIỂU THẬT theo hợp đồng
            var pc = root.GetComponent<FishingPlayerController>();
            if (pc == null) { pc = root.AddComponent<FishingPlayerController>(); }
            var ys = root.GetComponent<FishingYSort>();
            if (ys == null) { ys = root.AddComponent<FishingYSort>(); }

            // Wire field private (chỉ khi trống) qua SerializedObject
            var soPc = new SerializedObject(pc);
            SetRefIfEmpty(soPc, "animator", anim);
            SetRefIfEmpty(soPc, "spriteRenderer", sr);
            soPc.ApplyModifiedProperties();
            var soYs = new SerializedObject(ys);
            SetRefIfEmpty(soYs, "target", sr);
            soYs.ApplyModifiedProperties();

            // Con HeadAnchor / HandAnchor (trống), chỉ tạo nếu thiếu
            EnsureChild(root.transform, "HeadAnchor", new Vector3(0f, h * 1.02f, 0f));
            EnsureChild(root.transform, "HandAnchor", new Vector3(w * 0.22f, h * 0.45f, 0f));

            if (taoMoi)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            report.AppendLine("  · prefab " + prefabPath + " (h=" + h.ToString("0.00", CultureInfo.InvariantCulture) + " unit @scale 1, collider chân " + (0.45f * h).ToString("0.00", CultureInfo.InvariantCulture) + "x" + (0.28f * h).ToString("0.00", CultureInfo.InvariantCulture) + ")");
            return taoMoi;
        }

        private static void EnsureChild(Transform parent, string name, Vector3 localPos)
        {
            if (parent.Find(name) != null) { return; }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }

        private static bool SetRefIfEmpty(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null || p.propertyType != SerializedPropertyType.ObjectReference) { return false; }
            if (p.objectReferenceValue != null) { return false; }
            p.objectReferenceValue = value;
            return true;
        }

        public static string PngPath(string ch, string dir, int frame1Based)
        {
            return FishingIds.ArtCharactersRoot + "/" + ch + "/" + ch + "_" + dir + "_" + frame1Based.ToString(CultureInfo.InvariantCulture) + ".png";
        }

        /// <summary>[Dev B 09/09] Đường dẫn sheet cầm cần của nhân vật (đội vẽ giao theo prompt V4).</summary>
        public static string SheetPath(string ch)
        {
            return FishingIds.ArtCharactersRoot + "/" + ch + "/" + FishingSheetFileName;
        }

        public static string ControllerPath(string ch)
        {
            return FishingIds.AnimationsRoot + "/" + ch + "/" + ch + ".controller";
        }

        public static string PrefabPath(string ch)
        {
            return FishingIds.PrefabsRoot + "/" + FishingIds.PrefabPrefix + ch + ".prefab";
        }
    }
}
