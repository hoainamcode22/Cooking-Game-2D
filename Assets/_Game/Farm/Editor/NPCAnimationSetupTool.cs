using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Editor Tool: Tools/Farm Game/Tourist Boat/Setup NPC Animations (BOAT-002 §3.3)
/// — pattern bắt chước TouristBoatSetupTool: find-or-create, idempotent, Undo,
/// report Debug.Log + dialog + ping folder.
///
/// LÀM GÌ (một nút, chạy lại bao nhiêu lần cũng không nhân đôi):
///   1. Quét ảnh nhân vật ở <c>Assets/NV_NPC/NVGAME/Processed/NV01..NV11/</c>
///      (đặt tên chuẩn <c>NVxx_{down|left|right|up}_{1|2|3}.png</c>, cao 256px,
///      nền trong suốt, chân chạm đáy canvas ⇒ pivot Bottom-Center là chính xác).
///   2. Đặt TextureImporter cho từng file: Sprite (Single) · pivot Bottom-Center ·
///      PPU = <see cref="PixelsPerUnit"/> · mipmap OFF · alphaIsTransparency ·
///      nén Compressed (đồng bộ sprite khác trong dự án).
///   3. Mỗi nhân vật sinh 8 AnimationClip:
///      • 4 clip ĐI (walk_down/left/right/up): 3 frame chạy ping-pong 1-2-3-2 ở 8fps, LOOP.
///      • 4 clip ĐỨNG (idle_*): giữ frame 2 của hướng đó.
///   4. Mỗi nhân vật sinh 1 AnimatorController: 8 state + transition từ AnyState theo
///      param <c>DirX</c>/<c>DirY</c> (float) và <c>IsMoving</c> (bool).
///      CHỌN CÁCH NÀY chứ không BlendTree 2D: TouristAgent đã SNAP hướng về 4 hướng
///      chính nên điều kiện so sánh 1 trục là đủ, và 4-state + AnyState là cấu trúc
///      chắc chạy nhất qua các bản Unity (BlendTree 2D dựng bằng API dễ lệch thiết lập).
///   5. Sinh prefab <c>Assets/_Game/Farm/Prefabs/Tourists/Tourist_NVxx.prefab</c>:
///      SpriteRenderer + Animator + SortingGroup + BoxCollider2D (vùng tap) +
///      TouristAgent + TouristRequestBubble, scale canh theo <see cref="TouristWorldHeight"/>.
///
/// CLIP + CONTROLLER là ARTIFACT SINH RA: mỗi lần chạy tool sẽ ghi đè (xoá + tạo lại)
/// để kết quả luôn khớp ảnh mới nhất. PREFAB thì CẬP NHẬT TẠI CHỖ — giữ nguyên mọi
/// chỉnh tay của Sếp (scale, offset collider, field trên TouristAgent).
/// </summary>
public static class NPCAnimationSetupTool
{
    private const string MenuRoot  = "Tools/Farm Game/Tourist Boat/";
    private const string MenuSetup = MenuRoot + "Setup NPC Animations";

    // ─── Hằng số CHỈNH ĐƯỢC ────────────────────────────────────────────

    /// <summary>PPU của sprite nhân vật. 100 = mặc định Unity; đổi ở đây rồi chạy lại tool.</summary>
    private const float PixelsPerUnit = 100f;

    /// <summary>
    /// Chiều cao nhân vật trong WORLD (unit). Map của game dùng toạ độ RẤT lớn
    /// (3 bến cách nhau ~740 unit, sprite tàu rộng ~300) nên người phải ~150-180 unit
    /// mới cân với tàu. Ảnh 256px ở PPU 100 = 2.56 unit ⇒ scale ≈ 66.
    /// Thấy khách to/nhỏ quá thì sửa ĐÚNG số này rồi chạy lại tool.
    /// </summary>
    private const float TouristWorldHeight = 170f;

    /// <summary>Số khung hình/giây của clip đi bộ (GDD: ~8fps).</summary>
    private const float WalkFps = 8f;

    /// <summary>
    /// Số state mong đợi trong AnimatorController: 4 hướng đi + 4 hướng đứng.
    /// Dùng để XÁC MINH controller sau khi tạo (bug "Statemachine is missing" 2026-08-29).
    /// </summary>
    public const int ExpectedStateCount = 8;

    private const string SourceRoot   = "Assets/NV_NPC/NVGAME/Processed";
    private const string AnimRoot     = "Assets/_Game/Farm/Animations/Tourists";
    private const string PrefabRoot   = "Assets/_Game/Farm/Prefabs/Tourists";
    private const int    CharacterCount = 11;

    private static readonly string[] Directions = { "down", "left", "right", "up" };

    /// <summary>
    /// Sorting order gốc của prefab khách. TÊN LAYER không hardcode ở đây nữa —
    /// giải lúc chạy tool qua <see cref="TouristSortingLayers.Visitor"/>
    /// (ObjectsFront → Objects → Default).
    ///
    /// [BUG Sếp gặp 2026-08-29] Bản đầu ghi cứng "CongTrinh" (chép từ LivestockAI) —
    /// layer đó KHÔNG có trong project, Unity im lặng đẩy prefab về Default (id 0)
    /// ⇒ 11 prefab sinh ra đều bị cây/nhà che. Prefab thật đọc được:
    /// m_SortingLayerID: 0 · m_SortingOrder: 0.
    /// </summary>
    private const int VisitorSortingOrder = 5000;

    // ─────────────────────────────────────────────────────────────────────
    //  MENU
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Số nhân vật dựng thành công ở lần chạy gần nhất — tool 1 nút đọc để tự kiểm.</summary>
    public static int LastCharacterCount { get; private set; }

    [MenuItem(MenuSetup, false, 20)]
    public static void SetupAll()
    {
        RunSetup(false);
    }

    [MenuItem("Tools/Farm Game/Tourist Boat/Update Tourist Bubble Settings", false, 21)]
    public static void UpdateBubbleSettingsAll()
    {
        if (!AssetDatabase.IsValidFolder(PrefabRoot))
        {
            EditorUtility.DisplayDialog("Update Bubble", "Chưa có thư mục prefab " + PrefabRoot, "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab Tourist_NV", new[] { PrefabRoot });
        int count = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var root = PrefabUtility.LoadPrefabContents(path);
            var bub = root.GetComponent<TouristRequestBubble>();
            if (bub == null) bub = root.AddComponent<TouristRequestBubble>();

            var so = new SerializedObject(bub);
            var propWorldOffset = so.FindProperty("worldOffset");
            if (propWorldOffset != null) propWorldOffset.vector3Value = new Vector3(30f, 276f, 0f);

            var propDot1Offset = so.FindProperty("dot1Offset");
            if (propDot1Offset != null) propDot1Offset.vector3Value = new Vector3(12f, 176f, 0f);

            var propDot1Size = so.FindProperty("dot1Size");
            if (propDot1Size != null) propDot1Size.floatValue = 16f;

            var propDot2Offset = so.FindProperty("dot2Offset");
            if (propDot2Offset != null) propDot2Offset.vector3Value = new Vector3(20f, 208f, 0f);

            var propDot2Size = so.FindProperty("dot2Size");
            if (propDot2Size != null) propDot2Size.floatValue = 28f;

            var propFrameSize = so.FindProperty("frameWorldSize");
            if (propFrameSize != null) propFrameSize.floatValue = 168f;

            var propIconSize = so.FindProperty("iconWorldSize");
            if (propIconSize != null) propIconSize.floatValue = 110f;

            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TouristVisitor] Đã cập nhật thiết lập Bubble Thought Cloud Chain cho {count} prefab du khách.");
        EditorUtility.DisplayDialog("Update Bubble", $"Đã cập nhật thiết lập Bubble Thought Cloud Chain cho {count} prefab du khách.", "OK");
    }

    /// <summary>
    /// Lõi chạy được từ ngoài (TouristBoatOneClickSetup gọi trực tiếp — KHÔNG dùng
    /// ExecuteMenuItem vì nó không chờ và không bắt được lỗi).
    /// <paramref name="quiet"/> = true: không bật dialog riêng, không đổi Selection —
    /// tool 1 nút chỉ hiện ĐÚNG MỘT bảng tổng kết ở cuối.
    /// Trả về report dạng text (đã gồm cả dòng lỗi từng nhân vật).
    /// </summary>
    public static string RunSetup(bool quiet)
    {
        LastCharacterCount = 0;

        if (!AssetDatabase.IsValidFolder(SourceRoot))
        {
            string loi = "Không thấy thư mục ảnh: " + SourceRoot +
                         " — cần 11 thư mục con NV01..NV11, mỗi thư mục 12 file " +
                         "NVxx_{down|left|right|up}_{1|2|3}.png";
            Debug.LogError("[TouristVisitor] " + loi);
            if (!quiet) EditorUtility.DisplayDialog("NPC Animations", loi, "OK");
            return loi;
        }

        EnsureFolder(AnimRoot);
        EnsureFolder(PrefabRoot);

        var report   = new StringBuilder();
        int okChar   = 0, skipChar = 0, importedTex = 0;
        var madePrefabs = new List<string>();

        // ═════════════════════════════════════════════════════════════════
        //  GIAI ĐOẠN A — CHỈ IMPORT TEXTURE (được phép đóng băng AssetDatabase)
        // ═════════════════════════════════════════════════════════════════
        //
        // [BUG Sếp gặp lúc Play test 2026-08-29 — nguyên nhân gốc của
        //  "Statemachine for layer 'Base Layer' is missing"]
        // Bản trước bọc CẢ vòng lặp 11 nhân vật trong StartAssetEditing/StopAssetEditing,
        // nên AnimatorController.CreateAnimatorControllerAtPath() chạy TRONG lúc
        // AssetDatabase đang đóng băng: file .controller được tạo nhưng sub-asset
        // AnimatorStateMachine của Base Layer KHÔNG được ghi vào file ⇒ 11 controller
        // hỏng, Animator không khởi tạo được, nhân vật đứng đơ và Console spam
        // "Animator has not been initialized".
        // AssetDatabase.DeleteAsset() trong khối đó cũng không đáng tin.
        //
        // Nay: khối đóng băng CHỈ bao phần TextureImporter (chỗ thật sự lợi tốc độ —
        // 132 file). Mọi thao tác tạo/xoá asset của clip · controller · prefab đều nằm
        // NGOÀI, sau StopAssetEditing + Refresh.

        var nvHopLe = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int n = 1; n <= CharacterCount; n++)
            {
                string nv     = $"NV{n:00}";
                string folder = $"{SourceRoot}/{nv}";

                if (!AssetDatabase.IsValidFolder(folder))
                {
                    report.AppendLine($"- {nv}: THIẾU thư mục {folder} — bỏ qua.");
                    skipChar++;
                    continue;
                }

                bool thieuAnh = false;
                for (int d = 0; d < Directions.Length; d++)
                {
                    for (int f = 0; f < 3; f++)
                    {
                        string path = $"{folder}/{nv}_{Directions[d]}_{f + 1}.png";
                        if (!File.Exists(path))
                        {
                            report.AppendLine($"- {nv}: thiếu file {Path.GetFileName(path)}.");
                            thieuAnh = true;
                            continue;
                        }
                        if (ApplyImportSettings(path)) importedTex++;
                    }
                }

                if (thieuAnh)
                {
                    report.AppendLine($"- {nv}: BỎ QUA vì thiếu ảnh (xem dòng trên).");
                    skipChar++;
                    continue;
                }

                nvHopLe.Add(nv);
            }
        }
        finally
        {
            // Mở băng + flush đĩa TRƯỚC khi động tới clip/controller/prefab.
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ═════════════════════════════════════════════════════════════════
        //  GIAI ĐOẠN B — CLIP · CONTROLLER · PREFAB (ngoài khối đóng băng)
        // ═════════════════════════════════════════════════════════════════

        for (int i = 0; i < nvHopLe.Count; i++)
        {
            string nv     = nvHopLe[i];
            string folder = $"{SourceRoot}/{nv}";

            if (!quiet)
                EditorUtility.DisplayProgressBar("Setup NPC Animations",
                    $"Dựng animation {nv} ({i + 1}/{nvHopLe.Count})…",
                    (float)i / Mathf.Max(1, nvHopLe.Count));

            // 1 · Load sprite (đã import xong ở giai đoạn A)
            var frames = new Sprite[Directions.Length, 3];
            bool loiSprite = false;
            for (int d = 0; d < Directions.Length; d++)
            {
                for (int f = 0; f < 3; f++)
                {
                    string path = $"{folder}/{nv}_{Directions[d]}_{f + 1}.png";
                    frames[d, f] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (frames[d, f] == null)
                    {
                        report.AppendLine($"✖ {nv}: không load được sprite {Path.GetFileName(path)}.");
                        loiSprite = true;
                    }
                }
            }
            if (loiSprite) { skipChar++; continue; }

            // 2 · Clip
            string charAnimFolder = $"{AnimRoot}/{nv}";
            EnsureFolder(charAnimFolder);

            var walkClips = new AnimationClip[Directions.Length];
            var idleClips = new AnimationClip[Directions.Length];
            for (int d = 0; d < Directions.Length; d++)
            {
                walkClips[d] = BuildWalkClip(charAnimFolder, nv, Directions[d],
                                             frames[d, 0], frames[d, 1], frames[d, 2]);
                idleClips[d] = BuildIdleClip(charAnimFolder, nv, Directions[d], frames[d, 1]);
            }

            // 3 · Controller + XÁC MINH (không để hỏng âm thầm rồi lộ ra lúc Play)
            string ctrlPath = $"{charAnimFolder}/{nv}_Tourist.controller";
            AnimatorController controller = BuildController(ctrlPath, walkClips, idleClips);

            AssetDatabase.SaveAssets();

            string loiCtrl;
            if (!ControllerHopLe(ctrlPath, out loiCtrl))
            {
                report.AppendLine($"✖ {nv}: CONTROLLER HỎNG sau khi tạo — {loiCtrl}");
                report.AppendLine($"   File: {ctrlPath}");
                report.AppendLine("   Khắc phục: đóng cửa sổ Animator nếu đang mở rồi chạy lại menu này.");
                Debug.LogError($"[TouristVisitor] {nv}: controller hỏng — {loiCtrl} ({ctrlPath})");
                skipChar++;
                continue;
            }

            // 4 · Prefab (cập nhật tại chỗ, giữ chỉnh tay)
            string prefabPath = $"{PrefabRoot}/Tourist_{nv}.prefab";
            bool moi = BuildOrUpdatePrefab(prefabPath, nv, controller, frames[0, 1]);
            madePrefabs.Add(prefabPath);

            report.AppendLine($"✔ {nv}: 4 clip đi + 4 clip đứng + controller ({ExpectedStateCount} state, đã xác minh) " +
                              $"· prefab {(moi ? "TẠO MỚI" : "cập nhật")}.");
            okChar++;
        }

        if (!quiet) EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();

        // ── REPORT ──────────────────────────────────────────────────────
        var head = new StringBuilder();
        head.AppendLine($"Xong {okChar}/{CharacterCount} nhân vật" + (skipChar > 0 ? $" ({skipChar} bỏ qua)." : "."));
        head.AppendLine($"Ảnh đã đặt lại import setting: {importedTex} file.");
        head.AppendLine();
        head.AppendLine("ĐÃ SINH:");
        head.AppendLine("• Clip + AnimatorController: " + AnimRoot + "/NVxx/");
        head.AppendLine("• Prefab khách: " + PrefabRoot + "/Tourist_NVxx.prefab");
        head.AppendLine();
        head.AppendLine("THÔNG SỐ (sửa trong NPCAnimationSetupTool.cs rồi chạy lại):");
        head.AppendLine($"• PPU = {PixelsPerUnit:0}  ·  chiều cao khách = {TouristWorldHeight:0} unit world");
        head.AppendLine($"• Clip đi: 3 frame ping-pong 1-2-3-2 @ {WalkFps:0}fps, loop");
        head.AppendLine($"• Animator param: DirX (float) · DirY (float) · IsMoving (bool)");
        head.AppendLine($"• Sorting layer prefab khách: \"{TouristSortingLayers.Resolve(TouristSortingLayers.Visitor)}\" " +
                        $"order {VisitorSortingOrder} (bubble/mặt cười nằm layer cao hơn lúc chạy)");
        head.AppendLine();
        head.AppendLine("BƯỚC KẾ: chạy menu \"Setup Tourist Visitors (Scene)\" để dựng hệ trong scene.");
        head.Append("Chi tiết từng nhân vật đã in ra Console.");

        LastCharacterCount = okChar;
        Debug.Log("[TouristVisitor] Setup NPC Animations:\n" + report);

        if (!quiet)
        {
            EditorUtility.DisplayDialog("NPC Animations — Xong", head.ToString(), "OK");

            var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(PrefabRoot);
            if (folderAsset != null)
            {
                Selection.activeObject = folderAsset;
                EditorGUIUtility.PingObject(folderAsset);
            }
        }

        return head.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1 · IMPORT SETTINGS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt Sprite(Single) · pivot Bottom-Center · PPU · mipmap off · alpha transparency ·
    /// nén Compressed. Trả true nếu CÓ THAY ĐỔI (đỡ reimport thừa khi chạy lại).
    /// </summary>
    private static bool ApplyImportSettings(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        bool doi = false;

        if (importer.textureType != TextureImporterType.Sprite)
        { importer.textureType = TextureImporterType.Sprite; doi = true; }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        { importer.spriteImportMode = SpriteImportMode.Single; doi = true; }

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
        { importer.spritePixelsPerUnit = PixelsPerUnit; doi = true; }

        if (importer.mipmapEnabled)        { importer.mipmapEnabled = false; doi = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; doi = true; }

        if (importer.textureCompression != TextureImporterCompression.Compressed)
        { importer.textureCompression = TextureImporterCompression.Compressed; doi = true; }

        // Pivot Bottom-Center: chân nhân vật chạm đáy canvas ⇒ đặt chân đúng vào
        // toạ độ waypoint, và sorting theo Y (LivestockAI) mới đúng nghĩa.
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteAlignment != (int)SpriteAlignment.BottomCenter)
        {
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            doi = true;
        }

        if (doi)
        {
            importer.SaveAndReimport();
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2 · CLIP
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clip đi bộ: 4 bước 1-2-3-2 (ping-pong bằng cách THÊM KEY, không cần curve đặc biệt)
    /// ở <see cref="WalkFps"/> fps, LOOP. Key cuối lặp lại frame 1 để độ dài clip đúng
    /// 4/fps giây — nếu không, bước cuối bị hiển thị 0 giây và chân đi bị giật.
    /// </summary>
    private static AnimationClip BuildWalkClip(string folder, string nv, string dir,
                                               Sprite f1, Sprite f2, Sprite f3)
    {
        float step = 1f / WalkFps;
        var keys = new[]
        {
            new ObjectReferenceKeyframe { time = 0f,        value = f1 },
            new ObjectReferenceKeyframe { time = step,      value = f2 },
            new ObjectReferenceKeyframe { time = step * 2f, value = f3 },
            new ObjectReferenceKeyframe { time = step * 3f, value = f2 },
            new ObjectReferenceKeyframe { time = step * 4f, value = f1 }, // chốt độ dài + nối vòng lặp
        };
        return WriteClip($"{folder}/{nv}_walk_{dir}.anim", keys, loop: true);
    }

    /// <summary>Clip đứng yên: giữ frame 2 (tư thế trung tính) của hướng đó.</summary>
    private static AnimationClip BuildIdleClip(string folder, string nv, string dir, Sprite f2)
    {
        float step = 1f / WalkFps;
        var keys = new[]
        {
            new ObjectReferenceKeyframe { time = 0f,   value = f2 },
            new ObjectReferenceKeyframe { time = step, value = f2 },
        };
        return WriteClip($"{folder}/{nv}_idle_{dir}.anim", keys, loop: true);
    }

    /// <summary>Ghi clip ra asset (ghi đè nếu đã có — clip là artifact sinh ra).</summary>
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
            // Ghi đè NỘI DUNG asset cũ thay vì xoá-tạo: mọi controller/prefab đang
            // trỏ tới clip này giữ nguyên tham chiếu, không bị "Missing".
            EditorUtility.CopySerialized(clip, cu);
            Object.DestroyImmediate(clip);
            EditorUtility.SetDirty(cu);
            return cu;
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3 · ANIMATOR CONTROLLER
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// XÁC MINH một AnimatorController trên đĩa có DÙNG ĐƯỢC không.
    ///
    /// Đây là hàng rào chống lại đúng lỗi Sếp gặp: file .controller tồn tại, Unity vẫn
    /// load được asset, nhưng sub-asset AnimatorStateMachine của Base Layer KHÔNG được
    /// ghi ⇒ Console báo "Statemachine for layer 'Base Layer' is missing" và
    /// "Animator has not been initialized" — chỉ lộ ra lúc Play.
    ///
    /// Trả false + <paramref name="loi"/> mô tả ngắn nếu: không load được asset ·
    /// không có layer nào · stateMachine null · số state khác <see cref="ExpectedStateCount"/>.
    /// Tool ★ (bước 7) gọi chung hàm này để báo đỏ.
    /// </summary>
    public static bool ControllerHopLe(string path, out string loi)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (ctrl == null)
        {
            loi = "không load được file controller";
            return false;
        }
        return ControllerHopLe(ctrl, out loi);
    }

    /// <summary>Bản nhận thẳng asset — xem <see cref="ControllerHopLe(string, out string)"/>.</summary>
    public static bool ControllerHopLe(AnimatorController ctrl, out string loi)
    {
        if (ctrl == null)                       { loi = "controller null";                     return false; }
        if (ctrl.layers == null || ctrl.layers.Length == 0)
                                                { loi = "không có layer nào";                  return false; }

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        if (sm == null)
        {
            loi = "Base Layer THIẾU statemachine (sub-asset không được ghi)";
            return false;
        }

        int soState = sm.states != null ? sm.states.Length : 0;
        if (soState != ExpectedStateCount)
        {
            loi = $"có {soState} state, cần {ExpectedStateCount} (4 đi + 4 đứng)";
            return false;
        }

        loi = string.Empty;
        return true;
    }

    /// <summary>
    /// 8 state (4 đi + 4 đứng) + transition từ AnyState. TouristAgent snap hướng về
    /// 4 hướng chính nên chỉ cần so sánh ±0.5 trên MỘT trục cho mỗi hướng.
    ///
    /// Controller là artifact sinh ra: có sẵn thì XOÁ rồi tạo lại (khỏi dồn state trùng
    /// khi chạy tool nhiều lần); prefab được gán lại controller ngay sau đó.
    /// </summary>
    private static AnimatorController BuildController(string path,
                                                      AnimationClip[] walk, AnimationClip[] idle)
    {
        // TỰ CHỮA controller hỏng sẵn: 11 controller trong project của Sếp đều thiếu
        // statemachine (do bug StartAssetEditing của bản trước). Luôn XOÁ rồi tạo lại,
        // KHÔNG bao giờ dùng lại file cũ — và ghi rõ lý do vào Console để truy được.
        var ctrlCu = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (ctrlCu != null)
        {
            string loiCu;
            if (!ControllerHopLe(ctrlCu, out loiCu))
                Debug.LogWarning($"[TouristVisitor] Controller cũ HỎNG ({loiCu}) — xoá và tạo lại: {path}");
            AssetDatabase.DeleteAsset(path);
        }

        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("DirX",     AnimatorControllerParameterType.Float);
        ctrl.AddParameter("DirY",     AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        // index 0=down 1=left 2=right 3=up (khớp mảng Directions)
        AnimatorState[] idleStates = new AnimatorState[4];
        AnimatorState[] walkStates = new AnimatorState[4];

        for (int d = 0; d < 4; d++)
        {
            idleStates[d] = sm.AddState($"Idle_{Directions[d]}");
            idleStates[d].motion = idle[d];

            walkStates[d] = sm.AddState($"Walk_{Directions[d]}");
            walkStates[d].motion = walk[d];
        }

        sm.defaultState = idleStates[0]; // đứng nhìn xuống (về phía người chơi)

        for (int d = 0; d < 4; d++)
        {
            AddDirectionTransition(sm, walkStates[d], d, moving: true);
            AddDirectionTransition(sm, idleStates[d], d, moving: false);
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    /// <summary>
    /// Transition AnyState → state, điều kiện: IsMoving đúng chiều + trục hướng.
    ///   down  : DirY &lt; -0.5   ·  up    : DirY &gt; 0.5
    ///   left  : DirX &lt; -0.5   ·  right : DirX &gt; 0.5
    /// Hướng ngang thêm chặn |DirY| nhỏ để không đấu nhau khi agent đổi trục giữa chừng.
    /// </summary>
    private static void AddDirectionTransition(AnimatorStateMachine sm, AnimatorState state,
                                               int dirIndex, bool moving)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.hasExitTime          = false;
        t.duration             = 0f;
        t.canTransitionToSelf  = false;

        t.AddCondition(moving ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsMoving");

        switch (dirIndex)
        {
            case 0: // down
                t.AddCondition(AnimatorConditionMode.Less, -0.5f, "DirY");
                break;
            case 1: // left
                t.AddCondition(AnimatorConditionMode.Less,    -0.5f, "DirX");
                t.AddCondition(AnimatorConditionMode.Less,     0.5f, "DirY");
                t.AddCondition(AnimatorConditionMode.Greater, -0.5f, "DirY");
                break;
            case 2: // right
                t.AddCondition(AnimatorConditionMode.Greater,  0.5f, "DirX");
                t.AddCondition(AnimatorConditionMode.Less,     0.5f, "DirY");
                t.AddCondition(AnimatorConditionMode.Greater, -0.5f, "DirY");
                break;
            default: // up
                t.AddCondition(AnimatorConditionMode.Greater, 0.5f, "DirY");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4 · PREFAB
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo prefab khách nếu chưa có; đã có thì CẬP NHẬT TẠI CHỖ (bổ sung component
    /// thiếu + gán lại controller/sprite) — giữ nguyên scale/collider Sếp đã chỉnh.
    /// Trả true nếu vừa TẠO MỚI.
    /// </summary>
    private static bool BuildOrUpdatePrefab(string prefabPath, string nv,
                                            AnimatorController controller, Sprite defaultSprite)
    {
        bool taoMoi = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null;

        GameObject root = taoMoi ? new GameObject($"Tourist_{nv}")
                                 : PrefabUtility.LoadPrefabContents(prefabPath);

        // SpriteRenderer
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr == null) sr = root.AddComponent<SpriteRenderer>();
        if (sr.sprite == null || taoMoi) sr.sprite = defaultSprite;

        // Ghi ĐÚNG layer có thật vào prefab (không để Unity im lặng rơi về Default).
        sr.sortingLayerName = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
        sr.sortingOrder     = VisitorSortingOrder;

        // Animator
        var anim = root.GetComponent<Animator>();
        if (anim == null) anim = root.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.updateMode      = AnimatorUpdateMode.Normal;
        anim.cullingMode     = AnimatorCullingMode.CullUpdateTransforms;

        // SortingGroup — TouristAgent [RequireComponent] cần, và gom renderer con thành 1 khối
        if (root.GetComponent<SortingGroup>() == null) root.AddComponent<SortingGroup>();

        // Component gameplay
        if (root.GetComponent<TouristAgent>()         == null) root.AddComponent<TouristAgent>();
        if (root.GetComponent<TouristRequestBubble>() == null) root.AddComponent<TouristRequestBubble>();

        // Scale + collider tap: chỉ áp khi TẠO MỚI để không đè chỉnh tay của Sếp
        if (taoMoi)
        {
            float cao = defaultSprite != null
                ? defaultSprite.rect.height / Mathf.Max(1f, defaultSprite.pixelsPerUnit)
                : 2.56f;
            float k = cao > 0.0001f ? TouristWorldHeight / cao : 1f;
            root.transform.localScale = new Vector3(k, k, 1f);

            var col = root.GetComponent<BoxCollider2D>();
            if (col == null) col = root.AddComponent<BoxCollider2D>();
            float rong = defaultSprite != null
                ? defaultSprite.rect.width / Mathf.Max(1f, defaultSprite.pixelsPerUnit)
                : 2.56f;
            // Pivot Bottom-Center ⇒ hộp tap nằm TRÊN gốc toạ độ.
            col.size   = new Vector2(rong * 0.7f, cao * 0.9f);
            col.offset = new Vector2(0f, cao * 0.45f);
        }
        else if (root.GetComponent<BoxCollider2D>() == null)
        {
            // Prefab cũ thiếu collider thì bổ sung (không thì tap khách không ăn).
            var col = root.AddComponent<BoxCollider2D>();
            col.size   = new Vector2(1.8f, 2.3f);
            col.offset = new Vector2(0f, 1.15f);
        }

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
        return taoMoi;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Tạo folder lồng nhau trong Assets nếu chưa có (copy pattern TouristBoatSetupTool).</summary>
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf   = Path.GetFileName(folder);

        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
