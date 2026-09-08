using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Tool 2: nhân vật người chơi (PlayerF / PlayerM) — importer + 8 clip + AnimatorController + prefab. CHỦ FILE: Dev D.
    /// Nguồn: Assets/_Game/Fishing/Art/Characters/{Char}/{Char}_{down|left|right|up}_{1..3}.png (canvas F 294x590, M 314x568).
    /// Idle theo hướng: down/up = frame 1, left/right = frame 2. Walk = 4 khoá [bướcA, idle, bướcB, idle] + khoá chốt, 8 fps, loop.
    /// Controller: param DirX/DirY (float), IsMoving/IsFishing (bool); AnyState transitions, IsFishing=true → Idle hướng đó (ưu tiên trước Walk).
    /// Prefab Player_{Char}: SpriteRenderer, Animator, Rigidbody2D, CapsuleCollider2D chân, FishingPlayerController, FishingYSort, con HeadAnchor/HandAnchor.
    /// Idempotent: clip/controller là artifact (ghi đè), prefab cập nhật tại chỗ (chỉ bổ sung thiếu). Wire database.characters nếu trống.
    /// Bẫy đã tránh: SetTextureSettings ghi đè spriteMode (DecorStageArtTool 589-599); StartAssetEditing bao controller (NPCAnimationSetupTool).
    /// </summary>
    public static class FishingPlayerAnimSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuSetup = MenuRoot + "2. Nhân vật: importer + anim + prefab";

        private const float PixelsPerUnit = 100f;
        private const float WalkFps = 8f;
        private const int MinMaxTextureSize = 1024;
        public const int ExpectedStateCount = 8;

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

            // ── GIAI ĐOẠN B: clip · controller · prefab (ngoài khối đóng băng) ──
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
                }, true);

                float s = 1f / WalkFps;
                walk[d] = WriteClip(animFolder + "/Walk_" + Directions[d] + ".anim", new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = steps[0] },
                    new ObjectReferenceKeyframe { time = s, value = idleSprite },
                    new ObjectReferenceKeyframe { time = s * 2f, value = steps[1] },
                    new ObjectReferenceKeyframe { time = s * 3f, value = idleSprite },
                    new ObjectReferenceKeyframe { time = s * 4f, value = steps[0] },   // khoá chốt độ dài + nối vòng lặp
                }, true);
            }

            // Controller
            string ctrlPath = animFolder + "/" + ch + ".controller";
            AnimatorController ctrl = BuildController(ctrlPath, idle, walk);
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

            report.AppendLine("✔ " + ch + ": 4 Idle + 4 Walk + controller (4 param, " + ExpectedStateCount + " state) · prefab " + (prefabNew ? "TẠO MỚI" : "cập nhật tại chỗ") + ".");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Importer
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
        //  Clip
        // ─────────────────────────────────────────────────────────────────

        private static AnimationClip WriteClip(string path, ObjectReferenceKeyframe[] keys, bool loop)
        {
            var clip = new AnimationClip { frameRate = WalkFps };
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

        /// <summary>Controller là artifact: có sẵn thì XOÁ rồi tạo lại (khỏi dồn state trùng); prefab gán lại ngay sau.</summary>
        private static AnimatorController BuildController(string path, AnimationClip[] idle, AnimationClip[] walk)
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
            for (int d = 0; d < 4; d++)
            {
                idleStates[d] = sm.AddState("Idle_" + Directions[d]);
                idleStates[d].motion = idle[d];
                walkStates[d] = sm.AddState("Walk_" + Directions[d]);
                walkStates[d].motion = walk[d];
            }
            sm.defaultState = idleStates[0];

            // Thứ tự thêm = độ ưu tiên AnyState: IsFishing → Idle hướng đó TRƯỚC, rồi Walk, rồi Idle.
            for (int d = 0; d < 4; d++) { AddTransition(sm, idleStates[d], d, moving: false, fishing: true); }
            for (int d = 0; d < 4; d++) { AddTransition(sm, walkStates[d], d, moving: true, fishing: false); }
            for (int d = 0; d < 4; d++) { AddTransition(sm, idleStates[d], d, moving: false, fishing: false); }

            EditorUtility.SetDirty(ctrl);
            return ctrl;
        }

        /// <summary>
        /// AnyState → state. down: DirY &lt; -0.5 · up: DirY &gt; 0.5 · left: DirX &lt; -0.5 · right: DirX &gt; 0.5 (ngang thêm chặn |DirY| &lt; 0.5).
        /// fishing=true: chỉ điều kiện IsFishing + hướng (bỏ qua IsMoving). fishing=false: IsFishing == false + IsMoving đúng chiều.
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

        /// <summary>Xác minh controller dùng được (statemachine Base Layer tồn tại, đủ state). FishingCheckTool dùng chung.</summary>
        public static bool ControllerHopLe(string path, out string loi)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null) { loi = "không load được controller"; return false; }
            if (ctrl.layers == null || ctrl.layers.Length == 0) { loi = "không có layer"; return false; }
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            if (sm == null) { loi = "Base Layer THIẾU statemachine"; return false; }
            int n = sm.states != null ? sm.states.Length : 0;
            if (n != ExpectedStateCount) { loi = "có " + n + " state, cần " + ExpectedStateCount; return false; }
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
