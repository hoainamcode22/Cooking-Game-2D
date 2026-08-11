#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace NVChef.EditorTools
{
    /// <summary>
    /// TOOL DỰNG NHÂN VẬT ĐẦU BẾP — menu: Tools/Farm/Setup Nhân Vật Đầu Bếp
    ///
    /// Làm trọn gói: cắt sprite căn theo chân -> clip -> Animator -> prefab kéo-thả-là-chạy.
    ///
    /// ══ CHỈ DÙNG 2 HÀNG ĐẦU CỦA SHEET ══
    /// Nhân vật đã rút xuống 2 động tác (Idle, Stir) nhưng PNG vẫn có 4 HÀNG vật lý.
    /// Analyzer VẪN dò cả 4 hàng — CỐ Ý, không phải sót:
    ///   · rect dùng chung (84x74) được tính từ tầm với xa nhất của MỌI hàng. Nếu chỉ phân tích
    ///     2 hàng thì rect có thể co lại -> sprite Idle/Stir đổi kích thước, đổi vị trí nội dung,
    ///     đổi cả scale prefab. Ta KHÔNG muốn Idle/Stir thay đổi một pixel nào.
    /// Vì vậy giới hạn nằm ở TOOL (SoHangDung), không nằm trong analyzer — analyzer vẫn dùng lại
    /// được nguyên vẹn cho nhân vật khác.
    ///
    /// ══ CHỌN API GHI SPRITE RECT ══
    /// Dùng API 2D MỚI: SpriteDataProviderFactories + ISpriteEditorDataProvider
    /// (KHÔNG dùng TextureImporter.spritesheet đã deprecated).
    /// VÌ SAO: đã có tiền lệ chạy tốt trong chính dự án này — Assets/NV_01/Editor/SetupPlayerNV01.cs
    /// dùng đúng bộ API đó, nên chắc chắn package com.unity.2d.sprite đang có mặt và biên dịch được.
    /// Kèm theo ISpriteNameFileIdDataProvider để GIỮ ỔN ĐỊNH fileID theo TÊN sprite: nhờ vậy cắt lại
    /// lần 2 sẽ KHÔNG làm .anim / .prefab bị mất tham chiếu sprite (missing reference).
    /// </summary>
    public class ChefSetupTool : EditorWindow
    {
        // ── Đường dẫn ────────────────────────────────────────────────────────────
        private const string Folder         = "Assets/NV_CHEF";
        private const string AnimFolder     = Folder + "/Animations";
        private const string ControllerPath = Folder + "/Chef.controller";
        private const string PrefabPath     = Folder + "/Chef_NPC.prefab";
        private const string DefaultPng     = Folder + "/preview_all-removebg-preview.png";

        // ── Thông số nhập/xuất ───────────────────────────────────────────────────
        private const float PixelsPerUnit  = 100f;  // theo chuẩn dự án
        private const int   ClipFrameRate  = 10;
        private const string SortLayerName = "Objects";
        private const int   SortBaseOrder  = 500;   // khớp m_SortingOrder: 500 của công trình

        // ── Quy ước tỉ lệ của dự án (đã kiểm chứng trong scene, xem README) ──────
        private const float WorldUnitsPerCell = 100f;  // 1 ô lưới = 100 world unit
        private const float TargetCells       = 1.35f; // đầu bếp cao ~1.35 ô (giữa dải 1.2–1.5)

        // Tên clip theo THỨ TỰ HÀNG TRÊN -> DƯỚI của sheet.
        // CHỈ 2 ĐỘNG TÁC. Hàng 3 (xào lắc) và hàng 4 (hoàn thành) của sheet CỐ Ý BỎ TRỐNG.
        private static readonly string[] AnimNames = { "Idle", "Stir" };
        // Số frame KỲ VỌNG mỗi hàng (chỉ để đối chiếu, tool luôn dùng số dò được thực tế).
        private static readonly int[] ExpectedFrames = { 6, 7 };
        // Clip nào loop. Cả 2 đều LOOP: vòng diễn Idle <-> Stir do coroutine điều khiển thời lượng,
        // không còn động tác kết thúc nào cần hasExitTime để tự thoát.
        private static readonly bool[] LoopFlags = { true, true };

        // Tên các động tác ĐÃ BỎ — chỉ dùng để DỌN RÁC (clip cũ còn nằm trên đĩa).
        private static readonly string[] ClipDaBo = { "Flip", "Finish" };

        [SerializeField] private string pngPath = DefaultPng;
        [SerializeField] private ChefSheetAnalyzer.Settings settings = new ChefSheetAnalyzer.Settings();

        private ChefSheetAnalyzer.Analysis _last;
        private Vector2 _scroll;
        private string _log = "";

        [MenuItem("Tools/Farm/Setup Nhân Vật Đầu Bếp")]
        public static void Open()
        {
            var w = GetWindow<ChefSetupTool>("Đầu Bếp");
            w.minSize = new Vector2(560, 520);
            w.Show();
        }

        // ═════════════════════════════════════════════════════════════════════════
        // GUI
        // ═════════════════════════════════════════════════════════════════════════
        private void OnGUI()
        {
            EditorGUILayout.LabelField("SETUP NHÂN VẬT ĐẦU BẾP (NV_CHEF)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Nhân vật ĐỨNG YÊN, không WASD, chỉ diễn 2 động tác: Idle <-> Stir.\n" +
                "Sheet có 4 hàng nhưng tool CHỈ dùng 2 hàng đầu — 2 hàng dưới bỏ trống (bình thường).\n" +
                "Tool tự dò lưới frame từ ALPHA (không hardcode toạ độ) nên thay PNG khác vẫn chạy.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                pngPath = EditorGUILayout.TextField("Sheet PNG", pngPath);
                if (GUILayout.Button("...", GUILayout.Width(28)))
                {
                    string p = EditorUtility.OpenFilePanel("Chọn sheet đầu bếp", Folder, "png");
                    if (!string.IsNullOrEmpty(p)) pngPath = ToAssetPath(p);
                }
            }

            EditorGUILayout.Space(4);
            settings.alphaThreshold  = (byte)EditorGUILayout.IntSlider(
                new GUIContent("Ngưỡng alpha", "Alpha (0..255) coi là có hình."), settings.alphaThreshold, 1, 128);
            settings.feetSampleRows  = EditorGUILayout.IntSlider(
                new GUIContent("Số dòng lấy tâm chân", "Số dòng pixel dưới cùng dùng tính tâm chân."),
                settings.feetSampleRows, 1, 12);
            settings.marginPx        = EditorGUILayout.IntSlider(
                new GUIContent("Lề an toàn (px)", "Chừa quanh nội dung trong rect."), settings.marginPx, 0, 12);
            settings.snapDriftPx     = EditorGUILayout.Slider(
                new GUIContent("Ép về lưới nếu lệch >", "Lệch tâm chân (px) vượt mức này thì ép về lưới."),
                settings.snapDriftPx, 0.5f, 12f);
            settings.warnDriftPx     = EditorGUILayout.Slider(
                new GUIContent("Cảnh báo nếu lệch >", "Lệch tâm chân (px) vượt mức này thì báo động."),
                settings.warnDriftPx, 1f, 20f);
            settings.pivotChinhXacTuyetDoi = EditorGUILayout.Toggle(
                new GUIContent("Pivot chính xác tuyệt đối",
                    "BẬT = pivot Custom, sai số 0px. TẮT = Bottom-Center chuẩn dự án, sai số <= 0.5px."),
                settings.pivotChinhXacTuyetDoi);

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1. Phân tích sheet", GUILayout.Height(34))) DoAnalyze();
                if (GUILayout.Button("2. CẮT + TẠO TẤT CẢ", GUILayout.Height(34))) DoBuildAll();
            }
            if (GUILayout.Button("3. Xoá và làm lại (xoá clip/controller/prefab rồi dựng lại)", GUILayout.Height(24)))
                DoResetAndRebuild();

            // ── Nút 4: đặt thẳng vào scene ────────────────────────────────────
            // VÌ SAO CÓ NÚT NÀY: nút 2 chỉ tạo PREFAB ASSET trong cửa sổ Project,
            // KHÔNG tự đặt vào Hierarchy. Người dùng dễ tưởng tool chạy thất bại
            // vì "không thấy nhân vật đâu". Nút này đặt luôn vào giữa khung nhìn.
            EditorGUILayout.Space(4);
            bool coPrefab = System.IO.File.Exists(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), PrefabPath));

            using (new EditorGUI.DisabledScope(!coPrefab))
            {
                GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
                if (GUILayout.Button(coPrefab
                        ? "4. ĐẶT ĐẦU BẾP VÀO SCENE NGAY"
                        : "4. Đặt vào scene — chạy bước 2 trước đã", GUILayout.Height(30)))
                    DatVaoScene();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.LabelField(
                coPrefab ? $"Prefab: {PrefabPath}" : "Chưa có prefab.",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(6);
            if (_last != null) DrawAnalysis(_last);

            if (!string.IsNullOrEmpty(_log))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Nhật ký", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(_log, EditorStyles.textArea, GUILayout.Height(90));
            }
        }

        private void DrawAnalysis(ChefSheetAnalyzer.Analysis a)
        {
            EditorGUILayout.LabelField("KẾT QUẢ PHÂN TÍCH", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Ảnh {a.texWidth}x{a.texHeight}  |  {a.rows.Count} hàng (DÙNG {SoHangDung(a)})  |  {a.totalFrames} frame  |  " +
                $"rect chung {a.rectWidth}x{a.rectHeight} (đệm đáy {a.padBottom}px)  |  cao thân {a.bodyHeightPx}px");

            if (a.rows.Count > AnimNames.Length)
                EditorGUILayout.HelpBox(
                    $"Sheet có {a.rows.Count} hàng nhưng chỉ khai báo {AnimNames.Length} động tác " +
                    $"({string.Join(", ", AnimNames)}). ĐÂY LÀ BÌNH THƯỜNG, KHÔNG PHẢI LỖI: " +
                    $"{a.rows.Count - AnimNames.Length} hàng cuối bị BỎ QUA (không cắt sprite, không tạo clip, " +
                    "không tạo state). Rect dùng chung vẫn tính từ CẢ 4 hàng để Idle/Stir không đổi kích thước.",
                    MessageType.Info);

            float scale = TinhScale(a.bodyHeightPx, out string phepTinh);
            EditorGUILayout.LabelField($"Scale prefab đề xuất: {scale:0.##}   ({phepTinh})");

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(210));
            foreach (var row in a.rows)
            {
                int idx = a.rows.IndexOf(row);
                bool coDung = idx < AnimNames.Length;
                string kyVong = idx < ExpectedFrames.Length ? $" (kỳ vọng {ExpectedFrames[idx]})" : "";
                bool lech = idx < ExpectedFrames.Length && row.frames.Count != ExpectedFrames[idx];
                var st = new GUIStyle(EditorStyles.boldLabel);
                if (lech) st.normal.textColor = new Color(1f, 0.55f, 0.1f);
                // Hàng không dùng: tô xám để không ai tưởng tool bỏ sót.
                else if (!coDung) st.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
                string nhan = coDung ? row.animName : row.animName + "  ⟨HÀNG THỪA — KHÔNG DÙNG⟩";
                EditorGUILayout.LabelField(
                    $"HÀNG {idx + 1} — {nhan}: {row.frames.Count} frame{kyVong}   " +
                    $"y {row.yMin}..{row.yMax} (Unity)   bước {row.pitch:0.0}px   mốc đất y={row.groundY}   " +
                    $"cao thân {row.bodyHeightPx}px", st);

                // Hàng thừa: không liệt kê frame để bảng khỏi rối bởi tên tạm "Chef_Row2_*"
                // (những tên đó chỉ tồn tại trong bộ nhớ, KHÔNG bao giờ được ghi thành sprite).
                if (!coDung)
                {
                    EditorGUILayout.LabelField("   (bỏ qua — không cắt sprite, không tạo clip, không tạo state)",
                                               EditorStyles.miniLabel);
                    EditorGUILayout.Space(2);
                    continue;
                }

                foreach (var f in row.frames)
                {
                    string canhBao = "";
                    if (f.contentClipped) canhBao += "  ✖ CẮT MẤT HÌNH";
                    else if (Mathf.Abs(f.driftPx) > settings.warnDriftPx)
                        canhBao += f.snapped ? "  ⚠ lệch lớn → ĐÃ ép về lưới" : "  ⚠ LỆCH LỚN, có thể rung";
                    else if (f.snapped) canhBao += "  · đã ép về lưới";
                    if (f.clamped) canhBao += "  · rect kẹp biên (bù bằng pivot)";

                    var s2 = new GUIStyle(EditorStyles.label);
                    if (f.contentClipped) s2.normal.textColor = new Color(1f, 0.3f, 0.3f);
                    else if (Mathf.Abs(f.driftPx) > settings.warnDriftPx) s2.normal.textColor = new Color(1f, 0.7f, 0.15f);

                    EditorGUILayout.LabelField(
                        $"   {f.spriteName,-18} chân y={f.footY,4} (lệch đất {f.footOffset:+0;-0;0})   " +
                        $"tâm chân đo={f.rawFeetX,6:0.0} lưới={f.fitFeetX,6:0.0} lệch={f.driftPx,5:+0.0;-0.0}px   " +
                        $"rect=({f.rect.x},{f.rect.y}){canhBao}", s2);
                }
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            foreach (var e in a.errors) EditorGUILayout.HelpBox(e, MessageType.Error);
            if (a.warnings.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var w in a.warnings) sb.AppendLine("• " + w);
                EditorGUILayout.HelpBox(sb.ToString().TrimEnd(), MessageType.Warning);
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        // HÀNH ĐỘNG
        // ═════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// SỐ HÀNG THẬT SỰ ĐƯỢC DÙNG = min(số hàng dò được, số tên động tác khai báo).
        /// VÌ SAO PHẢI CÓ: sheet còn 4 hàng vật lý nhưng AnimNames chỉ còn 2. Analyzer đặt tên
        /// 2 hàng thừa là "Row2"/"Row3"; nếu cứ thế xử lý sẽ sinh ra sprite Chef_Row2_*,
        /// clip rác Chef_Row2.anim và state rác trong Animator. Chốt số hàng ở đây rồi áp cho
        /// CẢ 3 khâu (cắt sprite / tạo clip / tạo state) là chặn được tận gốc.
        /// </summary>
        private static int SoHangDung(ChefSheetAnalyzer.Analysis a)
        {
            return Mathf.Min(a.rows.Count, AnimNames.Length);
        }

        /// <summary>Tổng số frame của các hàng ĐƯỢC DÙNG (khác a.totalFrames = tổng cả sheet).</summary>
        private static int SoFrameDung(ChefSheetAnalyzer.Analysis a)
        {
            int n = 0;
            for (int r = 0; r < SoHangDung(a); r++) n += a.rows[r].frames.Count;
            return n;
        }

        private void DoAnalyze()
        {
            _last = ChefSheetAnalyzer.Analyze(pngPath, AnimNames, settings);
            _log = _last.Ok
                ? $"Phân tích OK: {_last.rows.Count} hàng (dùng {SoHangDung(_last)}: {string.Join(", ", AnimNames)}), " +
                  $"{_last.totalFrames} frame cả sheet / {SoFrameDung(_last)} frame sẽ được cắt, " +
                  $"rect {_last.rectWidth}x{_last.rectHeight}." +
                  (_last.rows.Count > AnimNames.Length
                      ? $"\n{_last.rows.Count - AnimNames.Length} hàng cuối BỎ QUA — bình thường, không phải lỗi."
                      : "")
                : "Phân tích LỖI — xem khung đỏ bên trên.";
            Repaint();
        }

        /// <summary>
        /// Đặt prefab đầu bếp vào scene, ngay giữa khung nhìn Scene view hiện tại,
        /// snap về lưới 100 unit cho khớp hệ toạ độ công trình.
        /// </summary>
        private void DatVaoScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Chưa có prefab",
                    "Không tìm thấy:\n" + PrefabPath + "\n\nChạy bước 2 (CẮT + TẠO TẤT CẢ) trước.", "OK");
                return;
            }

            // Vị trí: giữa Scene view đang mở. Không có Scene view thì đặt cạnh camera game.
            Vector3 pos;
            var sv = SceneView.lastActiveSceneView;
            if (sv != null && sv.camera != null) pos = sv.camera.transform.position;
            else if (Camera.main != null)        pos = Camera.main.transform.position;
            else                                 pos = Vector3.zero;

            // Snap về lưới 100 unit (CELL của dự án) để đứng khớp với công trình.
            const float CELL = 100f;
            pos = new Vector3(Mathf.Round(pos.x / CELL) * CELL,
                              Mathf.Round(pos.y / CELL) * CELL, 0f);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;

            Undo.RegisterCreatedObjectUndo(go, "Đặt đầu bếp vào scene");
            Selection.activeGameObject = go;
            if (sv != null) sv.FrameSelected();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            _log = $"Đã đặt '{go.name}' tại ({pos.x:0}, {pos.y:0}).\n" +
                   "Nhấn Ctrl+S để lưu scene. Bấm Play để xem nó xào.\n" +
                   "Kéo nó đi chỗ khác thì Y-sort tự cập nhật (đã bật ExecuteAlways).";
            Debug.Log("[ChefSetup] " + _log, go);
        }

        private void DoResetAndRebuild()
        {
            if (!EditorUtility.DisplayDialog("Xoá và làm lại",
                    "Sẽ XOÁ:\n" + AnimFolder + "  (gồm cả clip cũ Chef_Flip.anim / Chef_Finish.anim)\n" +
                    ControllerPath + "\n" + PrefabPath +
                    "\n\nSprite rect trong PNG cũng bị xoá trắng rồi cắt lại " +
                    "(nên đây là cách CHẮC CHẮN nhất để rect Chef_Flip_* / Chef_Finish_* cũ biến mất).\n" +
                    "Tiếp tục?",
                    "Xoá và làm lại", "Huỷ")) return;

            XoaAssetSinhRa();
            XoaSpriteRects();
            DoBuildAll();
        }

        private void DoBuildAll()
        {
            var log = new StringBuilder();

            // ── 1) PHÂN TÍCH ────────────────────────────────────────────────────
            var a = ChefSheetAnalyzer.Analyze(pngPath, AnimNames, settings);
            _last = a;
            if (!a.Ok)
            {
                _log = "DỪNG: phân tích sheet lỗi.\n" + string.Join("\n", a.errors);
                EditorUtility.DisplayDialog("Đầu Bếp", _log, "OK");
                return;
            }
            int soHang = SoHangDung(a);
            log.AppendLine($"[1] Dò alpha: {a.rows.Count} hàng, {a.totalFrames} frame, rect {a.rectWidth}x{a.rectHeight}.");
            if (a.rows.Count > AnimNames.Length)
                log.AppendLine($"[1] CHÚ Ý (không phải lỗi): sheet có {a.rows.Count} hàng, chỉ khai báo " +
                               $"{AnimNames.Length} động tác -> BỎ QUA {a.rows.Count - AnimNames.Length} hàng cuối. " +
                               "Rect dùng chung vẫn tính từ cả sheet nên Idle/Stir không đổi kích thước.");

            EnsureFolder(AnimFolder);

            // ── 1b) DỌN CLIP MỒ CÔI ─────────────────────────────────────────────
            // Phải làm TRƯỚC khi tạo clip mới: clip của động tác đã bỏ (Chef_Flip/Chef_Finish)
            // vẫn nằm trên đĩa và nút 2 sẽ không bao giờ ghi đè chúng.
            XoaClipMoCoi(log);

            // ── 2) CẮT SPRITE ───────────────────────────────────────────────────
            if (!CatSprite(a, soHang, log)) { _log = log.ToString(); EditorUtility.DisplayDialog("Đầu Bếp", _log, "OK"); return; }

            // ── 3) TẠO CLIP ─────────────────────────────────────────────────────
            var clips = new Dictionary<string, AnimationClip>();
            Sprite spriteDau = null;
            for (int r = 0; r < soHang; r++)   // CHỈ hàng được dùng -> không sinh clip rác Chef_Row2.anim
            {
                var row = a.rows[r];
                List<Sprite> frames = DocSpriteTheoThuTu(pngPath, "Chef_" + row.animName + "_");
                if (frames.Count == 0)
                {
                    log.AppendLine($"[3] LỖI: không đọc được sprite nào cho '{row.animName}'.");
                    _log = log.ToString(); EditorUtility.DisplayDialog("Đầu Bếp", _log, "OK"); return;
                }
                if (frames.Count != row.frames.Count)
                    log.AppendLine($"[3] Cảnh báo: '{row.animName}' dò {row.frames.Count} frame nhưng đọc lại được {frames.Count}.");

                if (spriteDau == null) spriteDau = frames[0];
                bool loop = r < LoopFlags.Length ? LoopFlags[r] : true;
                clips[row.animName] = TaoClip("Chef_" + row.animName, frames, loop);
                log.AppendLine($"[3] Clip Chef_{row.animName}: {frames.Count} frame @ {ClipFrameRate}fps " +
                               $"= {(frames.Count / (float)ClipFrameRate):0.00}s, loop={loop}.");
            }

            // ── 4) ANIMATOR ─────────────────────────────────────────────────────
            var controller = TaoController(clips, log);
            if (controller == null) { _log = log.ToString(); EditorUtility.DisplayDialog("Đầu Bếp", _log, "OK"); return; }

            // ── 5) PREFAB ───────────────────────────────────────────────────────
            float scale = TinhScale(a.bodyHeightPx, out string phepTinh);
            TaoPrefab(controller, spriteDau, scale, log);
            log.AppendLine($"[5] Scale = {scale:0.##}  ({phepTinh})");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine("[6] HOÀN TẤT. Kéo " + PrefabPath + " vào map là chạy.");
            _log = log.ToString();
            Debug.Log("[ChefSetupTool]\n" + _log);
            EditorUtility.DisplayDialog("Đầu Bếp — Hoàn tất",
                $"Đã tạo:\n• {SoFrameDung(a)} sprite (rect {a.rectWidth}x{a.rectHeight}, căn theo chân)\n" +
                $"• {AnimNames.Length} clip trong {AnimFolder}: {string.Join(", ", AnimNames)}\n" +
                $"• {ControllerPath}\n• {PrefabPath}  (scale {scale:0.##})\n\n" +
                "Kéo Chef_NPC.prefab vào map → Play là tự diễn.\n\n" +
                "⚠ LƯU Ý: controller và prefab bị XOÁ rồi TẠO LẠI nên có GUID MỚI. " +
                "Đầu bếp đã đặt trong scene từ trước sẽ mất liên kết prefab (hiện 'Missing') — " +
                "xoá nó đi rồi bấm nút 4 để đặt lại.", "OK");
            Repaint();
        }

        // ═════════════════════════════════════════════════════════════════════════
        // 2) CẮT SPRITE — mọi frame CÙNG rect, điểm chân cùng chỗ trong rect
        // ═════════════════════════════════════════════════════════════════════════
        /// <param name="soHang">Số hàng ĐẦU của sheet được cắt. Hàng ngoài phạm vi này bị bỏ hẳn
        /// (không có rect, không có sprite) — xem SoHangDung().</param>
        private bool CatSprite(ChefSheetAnalyzer.Analysis a, int soHang, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null) { log.AppendLine("[2] LỖI: " + pngPath + " không phải texture."); return false; }

            importer.textureType         = TextureImporterType.Sprite;          // Sprite (2D and UI)
            importer.spriteImportMode    = SpriteImportMode.Multiple;           // Multiple
            importer.spritePixelsPerUnit = PixelsPerUnit;                       // PPU = 100
            importer.textureCompression  = TextureImporterCompression.Uncompressed; // Compression = None
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.sRGBTexture         = true;
            importer.npotScale           = TextureImporterNPOTScale.None;
            importer.maxTextureSize      = Mathf.Max(1024, Mathf.NextPowerOfTwo(Mathf.Max(a.texWidth, a.texHeight)));

            // ── FILTER MODE: BILINEAR ───────────────────────────────────────────
            // VÌ SAO KHÔNG Point: đã mở ảnh xem + đo bằng code. Đây KHÔNG phải pixel-art:
            //   · 29.8% pixel có hình mang alpha TRUNG GIAN (viền khử răng cưa mềm),
            //   · 26.873 màu đục khác nhau trên ảnh 763x327 (pixel-art thường vài chục màu),
            //   · tô bóng chuyển sắc mượt, không có ô pixel vuông rõ.
            // Point filter sẽ biến viền mềm thành răng cưa cứng và làm sọc dải vùng chuyển sắc,
            // nhất là khi prefab phóng scale ~200 (ảnh bị kéo to hơn 1:1).
            importer.filterMode = FilterMode.Bilinear;

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            // FullRect: mesh = đúng khung rect, không cắt gọt theo alpha.
            // VÌ SAO: sprite nhỏ (84x74) nên Tight chẳng tiết kiệm gì, mà Tight tạo mesh KHÁC NHAU
            // mỗi frame -> thêm một nguồn sai lệch không cần thiết.
            ts.spriteMeshType  = SpriteMeshType.FullRect;
            ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(ts);
            importer.SaveAndReimport();

            // Lấy lại importer sau reimport để data provider đọc đúng trạng thái Multiple.
            importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dp == null) { log.AppendLine("[2] LỖI: thiếu package '2D Sprite' (com.unity.2d.sprite)."); return false; }
            dp.InitSpriteEditorDataProvider();

            // GIỮ LẠI GUID cũ theo TÊN sprite: cắt lại lần 2 thì .anim/.prefab không mất tham chiếu.
            var guidCu = new Dictionary<string, GUID>();
            foreach (var old in dp.GetSpriteRects())
                if (!guidCu.ContainsKey(old.name)) guidCu[old.name] = old.spriteID;

            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            var tenMoi = new HashSet<string>();

            // CHỈ soHang hàng đầu. Hàng thừa (analyzer gọi là Row2/Row3) KHÔNG được cắt,
            // nên sẽ không có sprite Chef_Row2_* / Chef_Row3_* nào ra đời.
            for (int r = 0; r < soHang; r++)
            {
                var row = a.rows[r];
                foreach (var f in row.frames)
                {
                    GUID id = guidCu.TryGetValue(f.spriteName, out var g) ? g : GUID.Generate();
                    bool pivotRieng = settings.pivotChinhXacTuyetDoi || f.clamped;

                    var sr = new SpriteRect
                    {
                        name      = f.spriteName,
                        spriteID  = id,
                        rect      = new Rect(f.rect.x, f.rect.y, f.rect.width, f.rect.height),
                        // Bottom-Center = chân đứng đúng một chỗ, và transform.position.y CHÍNH LÀ mặt đất
                        // -> Y-sort dùng trực tiếp position.y, không cần offset.
                        alignment = pivotRieng ? SpriteAlignment.Custom : SpriteAlignment.BottomCenter,
                        pivot     = pivotRieng ? f.pivotNormalized : new Vector2(0.5f, 0f),
                        border    = Vector4.zero,
                    };
                    rects.Add(sr);
                    pairs.Add(new SpriteNameFileIdPair(sr.name, id));
                    tenMoi.Add(sr.name);
                }
            }

            // SetSpriteRects THAY THẾ toàn bộ danh sách (không merge), SetNameFileIdPairs cũng vậy
            // -> rect cũ của động tác đã bỏ (Chef_Flip_*, Chef_Finish_*) tự biến mất khỏi .meta.
            dp.SetSpriteRects(rects.ToArray());
            var nameProv = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProv != null) nameProv.SetNameFileIdPairs(pairs);
            dp.Apply();
            importer.SaveAndReimport();

            log.AppendLine($"[2] Cắt {rects.Count} sprite ({soHang}/{a.rows.Count} hàng), " +
                           $"rect {a.rectWidth}x{a.rectHeight}, " +
                           $"pivot Bottom-Center, PPU {PixelsPerUnit}, filter Bilinear, compression None.");

            // KIỂM CHỨNG sau reimport: đọc lại sprite THẬT trong PNG. Nếu còn tên không thuộc bộ mới
            // thì .meta chưa sạch (rect của Flip/Finish còn sót) -> chỉ đường cho Edric, không đoán.
            var conSot = new List<string>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(pngPath))
                if (o is Sprite sp && !tenMoi.Contains(sp.name)) conSot.Add(sp.name);
            if (conSot.Count > 0)
            {
                conSot.Sort();
                log.AppendLine($"[2] CẢNH BÁO: PNG còn {conSot.Count} sprite CŨ không thuộc bộ mới " +
                               $"({string.Join(", ", conSot.GetRange(0, Mathf.Min(5, conSot.Count)))}" +
                               (conSot.Count > 5 ? ", ..." : "") + "). " +
                               "Bấm nút 3 (Xoá và làm lại) để xoá trắng sprite rect rồi cắt lại.");
            }
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // 3) CLIP
        // ═════════════════════════════════════════════════════════════════════════
        private AnimationClip TaoClip(string ten, List<Sprite> frames, bool loop)
        {
            string p = $"{AnimFolder}/{ten}.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(p) != null) AssetDatabase.DeleteAsset(p);

            var clip = new AnimationClip { frameRate = ClipFrameRate };

            var binding = new EditorCurveBinding
            {
                type         = typeof(SpriteRenderer),
                path         = "",          // SpriteRenderer nằm CÙNG object với Animator (root prefab)
                propertyName = "m_Sprite",
            };


            var keys = new ObjectReferenceKeyframe[frames.Count + 1];
            for (int i = 0; i < frames.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / (float)ClipFrameRate, value = frames[i] };
            keys[frames.Count] = new ObjectReferenceKeyframe
            {
                time  = frames.Count / (float)ClipFrameRate,
                value = frames[frames.Count - 1]
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            // Lấy settings SAU khi gán curve để startTime/stopTime đã đúng, chỉ sửa loopTime.
            var st = AnimationUtility.GetAnimationClipSettings(clip);
            st.loopTime  = loop;
            st.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, st);

            AssetDatabase.CreateAsset(clip, p);
            return clip;
        }

        // ═════════════════════════════════════════════════════════════════════════
        // 4) ANIMATOR CONTROLLER — 2 state, chỉ dùng Trigger
        // ═════════════════════════════════════════════════════════════════════════
        private AnimatorController TaoController(Dictionary<string, AnimationClip> clips, StringBuilder log)
        {
            foreach (var n in AnimNames)
                if (!clips.ContainsKey(n)) { log.AppendLine($"[4] LỖI: thiếu clip '{n}'."); return null; }

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

         
            c.AddParameter(ChefCookLoop.TrigIdle, AnimatorControllerParameterType.Trigger);
            c.AddParameter(ChefCookLoop.TrigStir, AnimatorControllerParameterType.Trigger);

            var sm = c.layers[0].stateMachine;
            sm.entryPosition     = new Vector3(-260, 0);
            sm.anyStatePosition  = new Vector3(-260, 100);
            sm.exitPosition      = new Vector3(520, 0);

            var idle = sm.AddState(ChefCookLoop.StateIdle, new Vector3(0,   0));
            var stir = sm.AddState(ChefCookLoop.StateStir, new Vector3(230, 0));

            idle.motion = clips[ChefCookLoop.StateIdle];
            stir.motion = clips[ChefCookLoop.StateStir];

            sm.defaultState = idle;   // entry = Idle

     
            Trig(idle, stir, ChefCookLoop.TrigStir);
            Trig(stir, idle, ChefCookLoop.TrigIdle);

            EditorUtility.SetDirty(c);
            AssetDatabase.SaveAssets();
            log.AppendLine("[4] Controller: 2 state (entry=Idle), 2 trigger (ToIdle, ToStir), " +
                           "2 transition (Idle→Stir, Stir→Idle). Không còn state/param/transition của Flip & Finish.");
            return c;
        }

        private static void Trig(AnimatorState from, AnimatorState to, string trigger)
        {
            var t = from.AddTransition(to);
            t.hasExitTime         = false;
            t.hasFixedDuration    = true;
            t.duration            = 0f;
            t.offset              = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        // ═════════════════════════════════════════════════════════════════════════
        // 5) PREFAB
        // ═════════════════════════════════════════════════════════════════════════
        private void TaoPrefab(AnimatorController controller, Sprite spriteDau, float scale, StringBuilder log)
        {
            var go = new GameObject("Chef_NPC");
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spriteDau;                       // Chef_Idle_0
            sr.drawMode = SpriteDrawMode.Simple;
            // ĐẶT LAYER TỪ TÊN, KHÔNG copy từ prefab cũ.
            // Dự án có 218 renderer trỏ sorting layer ID 1669604809 ĐÃ BỊ XOÁ -> copy là dính rác.
            if (SortingLayerTonTai(SortLayerName)) sr.sortingLayerName = SortLayerName;
            else log.AppendLine($"[5] Cảnh báo: không có sorting layer '{SortLayerName}'.");
            sr.sortingOrder = SortBaseOrder;             // giá trị hiển thị trước khi Play

            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.updateMode  = AnimatorUpdateMode.Normal;
           
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            go.AddComponent<ChefCookLoop>();

            var ys = go.AddComponent<ChefYSort>();
            ys.sortingLayerName = SortLayerName;
            ys.baseOrder        = SortBaseOrder;
            ys.orderPerUnitY    = 1f;

            // KHÔNG Rigidbody2D, KHÔNG Collider, KHÔNG script input:
            // đây là NPC trang trí đứng yên, thêm physics chỉ tốn CPU và có thể bị đẩy trôi.

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            log.AppendLine($"[5] Prefab {PrefabPath}: SpriteRenderer({SortLayerName}/{SortBaseOrder}) " +
                           $"+ Animator + ChefCookLoop + ChefYSort, scale {scale:0.##}, sprite = {spriteDau.name}.");
        }
        private static float TinhScale(int bodyHeightPx, out string phepTinh)
        {
            if (bodyHeightPx <= 0) { phepTinh = "không đo được cao thân"; return 100f; }

            float caoUnitO1 = bodyHeightPx / PixelsPerUnit;              // world unit khi scale = 1
            float caoMuonUnit = TargetCells * WorldUnitsPerCell;          // world unit mong muốn
            float tho = caoMuonUnit / caoUnitO1;

            // Làm tròn về bội số 5 cho designer dễ nhớ / dễ chỉnh tay.
            float lam = Mathf.Round(tho / 5f) * 5f;
            float oSauLamTron = (caoUnitO1 * lam) / WorldUnitsPerCell;
            // Nếu làm tròn đẩy ra ngoài dải 1.2–1.5 ô thì quay lại số chưa tròn.
            if (oSauLamTron < 1.2f || oSauLamTron > 1.5f) { lam = Mathf.Round(tho); oSauLamTron = (caoUnitO1 * lam) / WorldUnitsPerCell; }

            phepTinh = $"{bodyHeightPx}px / PPU {PixelsPerUnit:0} = {caoUnitO1:0.00} unit; " +
                       $"muốn {TargetCells:0.00} ô x {WorldUnitsPerCell:0} = {caoMuonUnit:0} unit; " +
                       $"{caoMuonUnit:0} / {caoUnitO1:0.00} = {tho:0.0} → làm tròn {lam:0.##} " +
                       $"→ cao thật {caoUnitO1 * lam:0.#} unit = {oSauLamTron:0.00} ô";
            return lam;
        }


        private static List<Sprite> DocSpriteTheoThuTu(string path, string prefix)
        {
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s && s.name.StartsWith(prefix)) list.Add(s);
            // Sort theo SỐ ở đuôi, không sort chuỗi (nếu không "_10" sẽ đứng trước "_2").
            list.Sort((x, y) => SoDuoi(x.name).CompareTo(SoDuoi(y.name)));
            return list;
        }

        private static int SoDuoi(string name)
        {
            int u = name.LastIndexOf('_');
            return (u >= 0 && int.TryParse(name.Substring(u + 1), out int v)) ? v : 0;
        }

        private static void XoaClipMoCoi(StringBuilder log)
        {
            if (!AssetDatabase.IsValidFolder(AnimFolder)) return;

            var hopLe = new HashSet<string>();
            foreach (var n in AnimNames) hopLe.Add("Chef_" + n);

            // Chỉ quét trong AnimFolder, không quét cả project.
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimFolder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (hopLe.Contains(Path.GetFileNameWithoutExtension(p))) continue;

                if (AssetDatabase.DeleteAsset(p))
                    log.AppendLine($"[1b] Đã xoá clip mồ côi (động tác đã bỏ): {p}");
                else
                    log.AppendLine($"[1b] KHÔNG xoá được: {p} — xoá tay giúp (cả file .meta).");
            }
        }

        private void XoaAssetSinhRa()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) AssetDatabase.DeleteAsset(PrefabPath);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) AssetDatabase.DeleteAsset(ControllerPath);
            var dangDung = new HashSet<string>(AnimNames);
            foreach (var ten in ClipDaBo)
            {
                if (dangDung.Contains(ten)) continue;
                string p = $"{AnimFolder}/Chef_{ten}.anim";
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(p) != null) AssetDatabase.DeleteAsset(p);
            }

            if (AssetDatabase.IsValidFolder(AnimFolder)) AssetDatabase.DeleteAsset(AnimFolder);
            AssetDatabase.Refresh();
        }

        private void XoaSpriteRects()
        {
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null) return;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.SaveAndReimport();

            importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dp == null) return;
            dp.InitSpriteEditorDataProvider();
            dp.SetSpriteRects(new SpriteRect[0]);
            var np = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (np != null) np.SetNameFileIdPairs(new List<SpriteNameFileIdPair>());
            dp.Apply();
            importer.SaveAndReimport();
        }
        
        private static bool SortingLayerTonTai(string n)
        {
            foreach (var l in SortingLayer.layers) if (l.name == n) return true;
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf   = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static string ToAssetPath(string absolute)
        {
            absolute = absolute.Replace('\\', '/');
            string root = Application.dataPath.Replace('\\', '/');
            return absolute.StartsWith(root) ? "Assets" + absolute.Substring(root.Length) : absolute;
        }
    }
}
#endif
