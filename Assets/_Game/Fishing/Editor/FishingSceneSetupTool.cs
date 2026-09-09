using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace FarmGame.Fishing
{
    /// <summary>
    /// ★ TOOL MỘT NÚT Hồ Câu — CHỦ FILE: Dev D (Dev A sửa bước 6 + vá đèn). Menu Tools/Farm Game/Hồ Câu/★ SETUP TẤT CẢ (1 nút).
    /// Chạy: Data (menu 1) → Nhân vật (menu 2) → tạo/mở SCN_Fishing + dựng hierarchy + BuildIfEmpty UI → bước 6 (lấy Ngày-đêm + Mưa + Grid_Iso45 từ farm) → Build Settings → SaveScene(SCN_Fishing).
    /// KHÔNG gắn vào SCN_Farm (menu 5 chạy riêng vì cần mở SCN_Farm). Scene SCN_Fishing do tool sở hữu nên ĐƯỢC lưu; báo rõ trong dialog.
    /// Find-or-create theo tên trong FishingIds, mọi AddComponent kiểm GetComponent trước, chỉ gán field trống.
    /// DestroyImmediate CHỈ với artifact do chính tool tạo (object DayNight tự dựng có DayNightCycleController không thuộc prefab; Grid_Fishing mọi tilemap rỗng).
    /// Mọi lời gọi BuildIfEmpty/EnsureVisuals của Dev A/B/C bọc try/catch để 1 lỗi không dừng tool.
    /// </summary>
    public static class FishingSceneSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuAll = MenuRoot + "★ SETUP TẤT CẢ (1 nút)";
        private const string MenuScene = MenuRoot + "3. Dựng scene SCN_Fishing";
        private const string MenuOpen = MenuRoot + "4. Mở scene SCN_Fishing";
        private const string MenuFarmAssets = MenuRoot + "6. Lấy Ngày-đêm + Mưa + Grid_Iso45 từ farm";
        private const string MenuFitBounds = MenuRoot + "8. Fit CameraBounds theo map đã vẽ";
        private const string MenuResetZoom = MenuRoot + "9. Đặt lại zoom camera mặc định";
        private const string UndoLabel = "Hồ Câu — Setup";

        private const string MainCameraName = "Main Camera";
        private const string EventSystemName = "EventSystem";
        private const string FishingControllerName = "FishingController";
        private const string SceneFolder = "Assets/_Game/Scenes";
        private const string FarmScenePath = "Assets/_Game/Scenes/SCN_Farm.unity";

        // Prefab ngày-đêm + mưa farm đang dùng (1 đèn Global blend 0 + Point, VFX mưa, ambience). Scene câu world 1 unit → scale (1,1,1).
        private const string DayNightWeatherPrefabPath = "Assets/Day_Night/Prefabs/DayNightWeatherSetup.prefab";
        private const string DayNightWeatherPrefabGuid = "c3dbaa3ffd83ea048b4164b33afe5c64";
        private const string DayNightWeatherName = "DayNightWeatherSetup";

        // Grid iso đúng hierarchy SCN_Farm (vẽ bằng Palette_Iso45, tile PPU 128 thoi 128x64 = ô 1x0.5 unit).
        private const string GridIsoName = "Grid_Iso45";
        private static readonly string[] GridIsoLayers =
        {
            "GroundBase_Dirt", "Tilemap_IsoGrass", "Tilemap_IsoDirt", "Tilemap_IsoRock", "Tilemap_IsoStone",
            "Tilemap_IsoDirtPatch", "Tilemap_IsoSand", "Tilemap_IsoDock", "Tilemap_IsoFence"
        };
        // Component được giữ trên bản sao Grid_Iso45 (SpriteRenderer thêm vào vì GroundBase_Dirt ở Map45SetupTool là sprite nền, không phải tilemap).
        private static readonly Type[] KeepOnGridCopy =
        {
            typeof(Transform), typeof(Grid), typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D),
            typeof(CompositeCollider2D), typeof(Rigidbody2D), typeof(SpriteRenderer)
        };

        // ── Bậc núi (Dev F) ──
        // Farm KHÔNG có quy ước tên sẵn cho lớp bậc → đặt mới ở đây. Mỗi lớp tự nhô lên theo trục Y nên nhìn như leo từng bậc đất.
        private const string CliffTilemapName = "Tilemap_Cliff";
        private const int CliffSortingOrder = 5;
        private static readonly string[] ElevTilemapNames = { "Tilemap_Elev1", "Tilemap_Elev2", "Tilemap_Elev3" };
        private static readonly float[] ElevOffsetsY = { 0.25f, 0.50f, 0.75f };
        private static readonly int[] ElevSortingOrders = { 10, 20, 30 };
        private static readonly Vector2 StepZoneSize = new Vector2(1f, 0.5f);          // đúng 1 ô iso
        private static readonly Vector3 StepZoneSamplePos = new Vector3(1.5f, 0f, 0f);
        private const float CameraBoundsMargin = 1f;                                    // lề quanh map khi fit (unit)

        private static readonly Color CameraBackgroundOld = new Color(0.08f, 0.26f, 0.14f, 1f);   // xanh lá đậm (Sếp chê "xanh quá")
        private static readonly Color CameraBackground = new Color(0.47f, 0.66f, 0.38f, 1f);      // cỏ sáng dịu
        private static readonly Vector2 CameraBoundsSizeOld = new Vector2(14f, 9f);              // ortho 3.2 16:9 → chỉ lắc ±1.3 unit
        private static readonly Vector2 CameraBoundsSize = new Vector2(40f, 26f);

        // ─────────────────────────────────────────────────────────────────
        //  MENU
        // ─────────────────────────────────────────────────────────────────

        [MenuItem(MenuAll, false, 0)]
        public static void RunAll()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("★ Setup tất cả Hồ Câu",
                "Tool sẽ chạy 4 bước:\n\n" +
                "1. Tạo/cập nhật Data (FishingConfig, 10 cá, 4 cần, FishingDatabase ở Resources/Fishing)\n" +
                "2. Nhân vật PlayerF/PlayerM: importer + 8 clip + controller + prefab (+ 4 clip cầm cần nếu có Sheet_Fishing.png — chưa có thì bỏ qua êm)\n" +
                "3. Tạo (nếu chưa có) rồi mở scene " + FishingIds.FishingScenePath + ", dựng hierarchy\n" +
                "4. Lấy từ farm: prefab DayNightWeatherSetup (ngày-đêm + mưa, 1 đèn Global — gỡ 5 đèn Global xung đột cũ), Grid_Iso45 (mở SCN_Farm additive ~10-30 s rồi đóng, KHÔNG lưu farm), nới CameraBounds, đổi nền camera; thêm Build Settings và LƯU scene câu.\n\n" +
                "Scene đang mở sẽ được hỏi lưu trước khi chuyển. Scene SCN_Fishing do tool sở hữu nên SẼ được lưu.\n" +
                "Bước gắn vào SCN_Farm (tab HUD, quầy cá, popup) chạy RIÊNG ở menu 5 khi đang mở SCN_Farm.\n" +
                "Chạy lại nhiều lần vẫn an toàn (không nhân đôi, không đè chỉnh tay).", "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var thieu = new List<string>();
            int loi = 0;

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                Buoc(0, 4, "Data…");
                try { report.AppendLine(FishingDataSetupTool.RunSetup(true)); }
                catch (Exception e) { loi++; report.AppendLine("✖ Data lỗi: " + e.Message); Debug.LogException(e); }

                Buoc(1, 4, "Nhân vật (importer + anim + prefab)…");
                try
                {
                    report.AppendLine(FishingPlayerAnimSetupTool.RunSetup(true));
                    if (FishingPlayerAnimSetupTool.LastCharacterCount < 2) { thieu.Add("Nhân vật: chỉ dựng được " + FishingPlayerAnimSetupTool.LastCharacterCount + "/2 — kiểm 24 PNG ở " + FishingIds.ArtCharactersRoot); }
                }
                catch (Exception e) { loi++; report.AppendLine("✖ Nhân vật lỗi: " + e.Message); Debug.LogException(e); }

                Buoc(2, 4, "Scene SCN_Fishing…");
                try
                {
                    if (!BuildScene(report, thieu, true)) { loi++; }
                }
                catch (Exception e) { loi++; report.AppendLine("✖ Scene lỗi: " + e.Message); Debug.LogException(e); }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            // Báo cáo 2 tầng: dialog ngắn + Console đầy đủ
            var head = new StringBuilder();
            if (loi == 0 && thieu.Count == 0)
            {
                head.AppendLine("✔ XONG — Data, nhân vật, scene SCN_Fishing (ngày-đêm + mưa + Grid_Iso45 từ farm) đã dựng và lưu.");
                head.AppendLine();
                AppendNextSteps(head);
            }
            else
            {
                head.AppendLine(loi > 0 ? "⚠ XONG nhưng có " + loi + " bước lỗi." : "⚠ XONG nhưng CÒN THIẾU vài thứ.");
                head.AppendLine();
                for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
                head.AppendLine();
                head.AppendLine("Đọc Console (lọc chữ FishingSetup) để xem chi tiết. Đa số: chạy lại menu này là hết.");
            }
            Debug.Log(FishingIds.SetupLogTag + " ★ SETUP TẤT CẢ — báo cáo đầy đủ:\n" + report);
            EditorUtility.DisplayDialog("★ Setup Hồ Câu — Kết quả", head.ToString(), "OK");

            var root = GameObject.Find(FishingIds.SceneRootName);
            if (root != null) { Selection.activeGameObject = root; EditorGUIUtility.PingObject(root); }
        }

        [MenuItem(MenuScene, false, 30)]
        public static void RunSceneOnly()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Dựng scene SCN_Fishing", "Tạo (nếu chưa có) rồi mở " + FishingIds.FishingScenePath + ", dựng hierarchy, lấy ngày-đêm + Grid_Iso45 từ farm (mở SCN_Farm additive rồi đóng, không lưu farm), thêm Build Settings và LƯU scene này.\nScene đang mở sẽ được hỏi lưu trước.", "Chạy", "Huỷ")) { return; }
            var report = new StringBuilder();
            var thieu = new List<string>();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            bool ok;
            try { ok = BuildScene(report, thieu, false); }
            finally { EditorUtility.ClearProgressBar(); Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); }
            Debug.Log(FishingIds.SetupLogTag + " Scene:\n" + report);
            var head = new StringBuilder(ok ? "✔ Scene đã dựng + lưu.\n\n" : "✖ Scene chưa xong, xem Console.\n");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            if (ok && thieu.Count == 0) { AppendNextSteps(head); }
            EditorUtility.DisplayDialog("Hồ Câu — Scene", head.ToString(), "OK");
        }

        [MenuItem(MenuOpen, false, 31)]
        public static void OpenFishingScene()
        {
            if (!File.Exists(FishingIds.FishingScenePath)) { EditorUtility.DisplayDialog("Hồ Câu", "Chưa có " + FishingIds.FishingScenePath + " — chạy menu 3 hoặc ★ trước.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
        }

        /// <summary>Menu 6 chạy riêng: mở SCN_Fishing (nếu chưa), lấy prefab ngày-đêm + Grid_Iso45 từ farm, nới CameraBounds, đổi nền camera, lưu scene câu.</summary>
        [MenuItem(MenuFarmAssets, false, 60)]
        public static void RunFarmAssetsOnly()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!File.Exists(FishingIds.FishingScenePath)) { EditorUtility.DisplayDialog("Hồ Câu", "Chưa có " + FishingIds.FishingScenePath + " — chạy menu 3 hoặc ★ trước.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("6. Lấy Ngày-đêm + Mưa + Grid_Iso45 từ farm",
                "Trong scene " + FishingIds.FishingSceneName + ":\n" +
                "• Đặt prefab " + DayNightWeatherName + " (giữ liên kết prefab, scale 1) — gỡ object DayNight cũ có 5 đèn Global xung đột (nguyên nhân nhân vật đen thui).\n" +
                "• Chép hierarchy " + GridIsoName + " từ SCN_Farm (mở additive ~10-30 s rồi đóng, KHÔNG lưu farm), xoá tile, scale 1 — Grid_Fishing rỗng cũ sẽ bị gỡ.\n" +
                "• CameraBounds (14x9 mặc định cũ) → 40x26; nền camera xanh đậm → cỏ sáng dịu.\n" +
                "Scene đang mở (nếu không phải SCN_Fishing) sẽ được hỏi lưu trước. SCN_Fishing SẼ được lưu.", "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var thieu = new List<string>();
            var created = new List<string>();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            bool saved = false;
            try
            {
                Scene scene;
                if (!TryOpenFishingScene(report, out scene)) { return; }
                Camera cam = null;
                var camGo = FindRoot(scene, MainCameraName);
                if (camGo != null) { cam = camGo.GetComponent<Camera>(); }
                report.AppendLine("── BƯỚC 6 " + FishingIds.FishingSceneName + " ──");
                ImportFarmAssets(scene, cam, created, report, thieu);
                EditorSceneManager.MarkSceneDirty(scene);
                saved = EditorSceneManager.SaveScene(scene);
                report.AppendLine(saved ? "✔ Đã lưu " + FishingIds.FishingScenePath : "✖ Không lưu được scene (xem Console).");
            }
            catch (Exception e) { report.AppendLine("✖ Bước 6 lỗi: " + e.Message); Debug.LogException(e); thieu.Add("Bước 6 lỗi — " + e.Message); }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }
            Debug.Log(FishingIds.SetupLogTag + " Bước 6 — báo cáo:\n" + report);
            var head = new StringBuilder(saved && thieu.Count == 0 ? "✔ Đã lấy ngày-đêm + mưa + Grid_Iso45 từ farm và lưu scene câu.\n\n" : "⚠ Bước 6 chưa trọn, xem Console (lọc FishingSetup).\n\n");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            if (saved && thieu.Count == 0) { AppendNextSteps(head); }
            EditorUtility.DisplayDialog("Hồ Câu — Bước 6", head.ToString(), "OK");
            var grid = GameObject.Find(GridIsoName);
            if (grid != null) { Selection.activeGameObject = grid; EditorGUIUtility.PingObject(grid); }
        }

        private static void AppendNextSteps(StringBuilder head)
        {
            head.AppendLine("BƯỚC KẾ (Sếp làm):");
            head.AppendLine("1) Vẽ map: chọn " + GridIsoName + "/Tilemap_IsoGrass (cỏ), Tilemap_IsoDirt (đường), Tilemap_IsoSand/Dock/Fence… → Window > 2D > Tile Palette > Palette_Iso45.");
            head.AppendLine("2) Núi có bậc: mặt bậc 1 vẽ ở Tilemap_Elev1, bậc 2 ở Elev2, bậc 3 ở Elev3 (3 lớp tự nhô 0.25 unit nên nhìn như leo bậc); mép vách vẽ ở " + CliffTilemapName + " để chặn đi xuyên; mỗi lối lên đặt 1 bản sao " + ElevationStepZone.RootName + "/" + ElevationStepZone.SampleName + ".");
            head.AppendLine("3) Kéo FishingZones/Zone_01 trùng mép nước, rồi chạy menu 8 để CameraBounds tự ôm map.");
            head.AppendLine("4) Mở SCN_Farm → menu 5 (Gắn vào SCN_Farm) → Ctrl+S.");
            head.AppendLine("5) Play SCN_Farm → tab HỒ CÂU → vào phòng. Zoom bằng 2 nút +/− trên HUD, pinch 2 ngón hoặc cuộn chuột; menu 9 đặt lại zoom mặc định.");
        }

        // ─────────────────────────────────────────────────────────────────
        //  MENU 8: fit CameraBounds theo map đã vẽ
        // ─────────────────────────────────────────────────────────────────

        [MenuItem(MenuFitBounds, false, 80)]
        public static void FitCameraBoundsToMap()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!File.Exists(FishingIds.FishingScenePath)) { EditorUtility.DisplayDialog("Hồ Câu", "Chưa có " + FishingIds.FishingScenePath + " — chạy menu 3 hoặc ★ trước.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("8. Fit CameraBounds theo map đã vẽ",
                "Duyệt mọi Tilemap dưới " + GridIsoName + ", CompressBounds() rồi gộp vùng đã vẽ, đặt BoxCollider2D của " + FishingIds.CameraBoundsName +
                " ôm trọn map + lề " + CameraBoundsMargin.ToString("0.#", CultureInfo.InvariantCulture) + " unit.\n" +
                "Kèm theo (Sếp yêu cầu 09/09 'tầm nhìn chỉ tới vòng này'):\n" +
                "• Kẹp zoom: cameraZoomMax + cameraOrthoSize trong FishingConfig hạ xuống để khung nhìn KHÔNG vượt ra ngoài map; camera scene đặt theo.\n" +
                "• " + FishingIds.SpawnPointName + " nằm ngoài map hoặc trên ô nước → dời vào ô đất/cỏ gần tâm map nhất.\n" +
                "Chưa vẽ ô nào thì KHÔNG đổi gì.\nScene đang mở (nếu không phải SCN_Fishing) sẽ được hỏi lưu trước. SCN_Fishing SẼ được lưu.", "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var head = new StringBuilder();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            try
            {
                Scene scene;
                if (!TryOpenFishingScene(report, out scene)) { return; }
                report.AppendLine("── MENU 8: fit CameraBounds ──");

                GameObject gridIso = FindRoot(scene, GridIsoName);
                if (gridIso == null) { gridIso = FindRoot(scene, FishingIds.GridName); }
                if (gridIso == null)
                {
                    head.AppendLine("✖ Không thấy " + GridIsoName + " trong scene — chạy menu 6 trước.");
                    report.AppendLine("  ✖ Không thấy " + GridIsoName + ".");
                    return;
                }

                var maps = gridIso.GetComponentsInChildren<Tilemap>(true);
                bool coO = false;
                Bounds world = new Bounds(Vector3.zero, Vector3.zero);
                int tongO = 0;
                for (int i = 0; i < maps.Length; i++)
                {
                    var tm = maps[i];
                    if (tm == null) { continue; }
                    tm.CompressBounds();
                    int used = tm.GetUsedTilesCount();
                    tongO += used;
                    if (used <= 0) { report.AppendLine("  · " + tm.name + ": 0 ô"); continue; }
                    Bounds lb = tm.localBounds;
                    Vector3 mn = lb.min;
                    Vector3 mx = lb.max;
                    // 4 góc (2D) đổi sang world qua transform của chính tilemap (đã tính offset Y của lớp bậc).
                    Vector3[] goc =
                    {
                        tm.transform.TransformPoint(new Vector3(mn.x, mn.y, 0f)),
                        tm.transform.TransformPoint(new Vector3(mx.x, mn.y, 0f)),
                        tm.transform.TransformPoint(new Vector3(mn.x, mx.y, 0f)),
                        tm.transform.TransformPoint(new Vector3(mx.x, mx.y, 0f))
                    };
                    for (int g = 0; g < goc.Length; g++)
                    {
                        Vector3 pt = new Vector3(goc[g].x, goc[g].y, 0f);
                        if (!coO && g == 0) { world = new Bounds(pt, Vector3.zero); coO = true; }
                        else { world.Encapsulate(pt); }
                    }
                    report.AppendLine("  · " + tm.name + ": " + used.ToString(CultureInfo.InvariantCulture) + " ô");
                }

                if (!coO)
                {
                    head.AppendLine("⚠ Chưa vẽ ô tile nào dưới " + GridIsoName + " — KHÔNG đổi CameraBounds.");
                    head.AppendLine("Vẽ map bằng Tile Palette (Palette_Iso45) rồi chạy lại menu này.");
                    report.AppendLine("  ⚠ 0 ô trên " + maps.Length.ToString(CultureInfo.InvariantCulture) + " tilemap — không đổi gì.");
                    return;
                }

                GameObject boundsGo = FindRoot(scene, FishingIds.CameraBoundsName);
                if (boundsGo == null)
                {
                    var tao = new List<string>();
                    boundsGo = FindOrCreateRoot(scene, FishingIds.CameraBoundsName, tao);
                    report.AppendLine("  + Tạo mới " + FishingIds.CameraBoundsName);
                }
                var box = boundsGo.GetComponent<BoxCollider2D>();
                if (box == null) { box = Undo.AddComponent<BoxCollider2D>(boundsGo); box.isTrigger = true; }

                Vector3 tam = world.center;
                Vector3 co = world.size;
                Vector3 lossy = boundsGo.transform.lossyScale;
                float sx = Mathf.Abs(lossy.x) < 0.0001f ? 1f : Mathf.Abs(lossy.x);
                float sy = Mathf.Abs(lossy.y) < 0.0001f ? 1f : Mathf.Abs(lossy.y);
                Vector3 tamLocal = boundsGo.transform.InverseTransformPoint(new Vector3(tam.x, tam.y, boundsGo.transform.position.z));

                Undo.RecordObject(box, UndoLabel);
                box.offset = new Vector2(tamLocal.x, tamLocal.y);
                box.size = new Vector2((co.x + CameraBoundsMargin * 2f) / sx, (co.y + CameraBoundsMargin * 2f) / sy);
                EditorUtility.SetDirty(box);

                report.AppendLine("  ✓ Map đã vẽ: " + tongO.ToString(CultureInfo.InvariantCulture) + " ô, vùng world " +
                    co.x.ToString("0.##", CultureInfo.InvariantCulture) + " x " + co.y.ToString("0.##", CultureInfo.InvariantCulture) +
                    " tâm (" + tam.x.ToString("0.##", CultureInfo.InvariantCulture) + ", " + tam.y.ToString("0.##", CultureInfo.InvariantCulture) + ")");
                report.AppendLine("  ✓ CameraBounds.size = " + box.size.x.ToString("0.##", CultureInfo.InvariantCulture) + " x " + box.size.y.ToString("0.##", CultureInfo.InvariantCulture) +
                    ", offset = (" + box.offset.x.ToString("0.##", CultureInfo.InvariantCulture) + ", " + box.offset.y.ToString("0.##", CultureInfo.InvariantCulture) + "), lề " + CameraBoundsMargin.ToString("0.#", CultureInfo.InvariantCulture) + " unit.");

                // [Lead vòng 16] Tầm nhìn không vượt ra ngoài map: kẹp zoom theo cỡ map thật.
                ClampZoomToMap(scene, co, report, head);
                // [Lead vòng 16] Điểm spawn phải nằm TRONG map và KHÔNG trên ô nước (Sếp vẽ hồ lệch khỏi gốc toạ độ → nhân vật rơi ra bãi đất trống).
                FixSpawnInsideMap(scene, gridIso, world, report, head);

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                report.AppendLine(saved ? "✔ Đã lưu " + FishingIds.FishingScenePath : "✖ Không lưu được scene (xem Console).");

                head.AppendLine("✔ CameraBounds đã ôm trọn map (" + tongO.ToString(CultureInfo.InvariantCulture) + " ô đã vẽ).");
                head.AppendLine();
                head.AppendLine("Kích thước: " + box.size.x.ToString("0.##", CultureInfo.InvariantCulture) + " x " + box.size.y.ToString("0.##", CultureInfo.InvariantCulture) + " (đã cộng lề " + CameraBoundsMargin.ToString("0.#", CultureInfo.InvariantCulture) + " unit mỗi bên).");
                head.AppendLine("Số ô từng tilemap xem Console (lọc FishingSetup).");
                Selection.activeGameObject = boundsGo;
                EditorGUIUtility.PingObject(boundsGo);
            }
            catch (Exception e)
            {
                head.AppendLine("✖ Menu 8 lỗi: " + e.Message + " (xem Console).");
                report.AppendLine("✖ Lỗi: " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                Debug.Log(FishingIds.SetupLogTag + " Menu 8 (fit CameraBounds) — báo cáo:\n" + report);
                if (head.Length > 0) { EditorUtility.DisplayDialog("Hồ Câu — Fit CameraBounds", head.ToString(), "OK"); }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  MENU 9: đặt lại zoom camera mặc định
        // ─────────────────────────────────────────────────────────────────

        [MenuItem(MenuResetZoom, false, 81)]
        public static void ResetCameraZoom()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!File.Exists(FishingIds.FishingScenePath)) { EditorUtility.DisplayDialog("Hồ Câu", "Chưa có " + FishingIds.FishingScenePath + " — chạy menu 3 hoặc ★ trước.", "OK"); return; }
            var cfg = LoadConfig();

            float size = cfg != null ? cfg.cameraOrthoSize : 6f;
            bool nangCfg = cfg != null && cfg.cameraOrthoSize <= 3.3f && AssetDatabase.Contains(cfg);
            if (!EditorUtility.DisplayDialog("9. Đặt lại zoom camera mặc định",
                "Đặt Main Camera.orthographicSize = " + (nangCfg ? "6" : size.ToString("0.##", CultureInfo.InvariantCulture)) + " (FishingConfig.cameraOrthoSize)\n" +
                (nangCfg ? "Asset FishingConfig còn 3.2 (số vòng 12) → tool NÂNG LÊN 6 để map bớt to.\n" : string.Empty) +
                "và xoá PlayerPrefs \"" + FishingCameraFollow.PrefsZoom + "\" (mức zoom người chơi đã tự chỉnh).\n\n" +
                "Scene đang mở (nếu không phải SCN_Fishing) sẽ được hỏi lưu trước. SCN_Fishing SẼ được lưu.", "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var head = new StringBuilder();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);

            // Nâng asset SAU khi Sếp bấm Chạy và sau khi mở nhóm Undo (bấm Huỷ thì không đổi gì).
            if (nangCfg)
            {
                Undo.RecordObject(cfg, UndoLabel);
                cfg.cameraOrthoSize = 6f;
                size = 6f;
                EditorUtility.SetDirty(cfg);
                AssetDatabase.SaveAssets();
                report.AppendLine("✓ FishingConfig.cameraOrthoSize 3.2 → 6 (nhìn rộng gấp đôi).");
            }
            try
            {
                Scene scene;
                if (!TryOpenFishingScene(report, out scene)) { return; }
                report.AppendLine("── MENU 9: đặt lại zoom ──");

                GameObject camGo = FindRoot(scene, MainCameraName);
                Camera cam = camGo != null ? camGo.GetComponent<Camera>() : null;
                if (cam == null)
                {
                    head.AppendLine("⚠ Không thấy '" + MainCameraName + "' có Camera — chỉ xoá PlayerPrefs.");
                    report.AppendLine("  ⚠ Không thấy Main Camera.");
                }
                else
                {
                    Undo.RecordObject(cam, UndoLabel);
                    float cu = cam.orthographicSize;
                    cam.orthographic = true;
                    cam.orthographicSize = size;
                    EditorUtility.SetDirty(cam);
                    report.AppendLine("  ✓ Main Camera.orthographicSize " + cu.ToString("0.##", CultureInfo.InvariantCulture) + " → " + size.ToString("0.##", CultureInfo.InvariantCulture));
                    head.AppendLine("✔ Main Camera.orthographicSize = " + size.ToString("0.##", CultureInfo.InvariantCulture) + " (theo FishingConfig).");
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool saved = EditorSceneManager.SaveScene(scene);
                    report.AppendLine(saved ? "✔ Đã lưu " + FishingIds.FishingScenePath : "✖ Không lưu được scene (xem Console).");
                }

                bool coKhoa = PlayerPrefs.HasKey(FishingCameraFollow.PrefsZoom);
                PlayerPrefs.DeleteKey(FishingCameraFollow.PrefsZoom);
                PlayerPrefs.Save();
                report.AppendLine(coKhoa ? "  ✓ Đã xoá PlayerPrefs " + FishingCameraFollow.PrefsZoom : "  · PlayerPrefs " + FishingCameraFollow.PrefsZoom + " chưa từng lưu.");
                head.AppendLine(coKhoa ? "✔ Đã xoá zoom đã lưu (" + FishingCameraFollow.PrefsZoom + ")." : "· Chưa có zoom nào được lưu.");
                head.AppendLine();
                head.AppendLine("Muốn đổi mức mặc định: sửa FishingConfig.cameraOrthoSize rồi chạy lại menu này.");
                if (camGo != null) { Selection.activeGameObject = camGo; EditorGUIUtility.PingObject(camGo); }
            }
            catch (Exception e)
            {
                head.AppendLine("✖ Menu 9 lỗi: " + e.Message + " (xem Console).");
                report.AppendLine("✖ Lỗi: " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                Debug.Log(FishingIds.SetupLogTag + " Menu 9 (đặt lại zoom) — báo cáo:\n" + report);
                if (head.Length > 0) { EditorUtility.DisplayDialog("Hồ Câu — Zoom mặc định", head.ToString(), "OK"); }
            }
        }

        private static void Buoc(int i, int tong, string mo)
        {
            EditorUtility.DisplayProgressBar("★ Setup Hồ Câu", "Bước " + (i + 1) + "/" + tong + ": " + mo, (float)i / tong);
        }

        // ─────────────────────────────────────────────────────────────────
        //  SCENE
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Mở SCN_Fishing nếu chưa là scene active (hỏi lưu scene đang mở). Trả false nếu người dùng huỷ.</summary>
        private static bool TryOpenFishingScene(StringBuilder report, out Scene scene)
        {
            var active = SceneManager.GetActiveScene();
            if (active.path == FishingIds.FishingScenePath) { scene = active; return true; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { report.AppendLine("✖ Người dùng huỷ lưu scene đang mở — dừng."); scene = default(Scene); return false; }
            scene = EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
            report.AppendLine("· mở scene " + FishingIds.FishingScenePath);
            return true;
        }

        /// <summary>Tạo/mở SCN_Fishing, dựng hierarchy, bước 6 (farm assets), Build Settings, SaveScene. Trả false nếu bị huỷ/lỗi nặng.</summary>
        public static bool BuildScene(StringBuilder report, List<string> thieu, bool showProgress)
        {
            report.AppendLine("── SCENE " + FishingIds.FishingSceneName + " ──");
            var cfg = LoadConfig();

            // 1. Tạo hoặc mở
            Scene scene;
            if (!File.Exists(FishingIds.FishingScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { report.AppendLine("✖ Người dùng huỷ lưu scene đang mở — dừng."); return false; }
                FishingDataSetupTool.EnsureFolder(SceneFolder);
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (!EditorSceneManager.SaveScene(scene, FishingIds.FishingScenePath)) { report.AppendLine("✖ Không lưu được scene mới tại " + FishingIds.FishingScenePath); return false; }
                report.AppendLine("+ TẠO scene mới " + FishingIds.FishingScenePath);
            }
            else
            {
                if (!TryOpenFishingScene(report, out scene)) { return false; }
                report.AppendLine("· scene có sẵn " + FishingIds.FishingScenePath);
            }

            // 2. Hierarchy
            var created = new List<string>();
            GameObject sceneRoot = FindOrCreateRoot(scene, FishingIds.SceneRootName, created);
            Ensure<FishingSceneBootstrap>(sceneRoot, report);

            Camera cam = BuildCamera(scene, cfg, created, report);
            BuildGrid(scene, created, report);

            GameObject spawn = FindOrCreateRoot(scene, FishingIds.SpawnPointName, created);
            if (created.Contains(FishingIds.SpawnPointName)) { spawn.transform.position = new Vector3(0f, -0.5f, 0f); }

            BuildZones(scene, created, report);

            GameObject bounds = FindOrCreateRoot(scene, FishingIds.CameraBoundsName, created);
            var box = bounds.GetComponent<BoxCollider2D>();
            if (box == null) { box = Undo.AddComponent<BoxCollider2D>(bounds); box.isTrigger = true; box.size = CameraBoundsSize; box.offset = Vector2.zero; }

            // Ngày-đêm: ưu tiên prefab farm (bước 6). Chỉ dựng tay khi thiếu prefab VÀ chưa có instance.
            if (LoadDayNightWeatherPrefab() == null && FindRoot(scene, DayNightWeatherName) == null) { BuildDayNightFallback(scene, cam, created, report, thieu); }

            GameObject remote = FindOrCreateRoot(scene, FishingIds.RemotePlayersRoot, created);
            Ensure<RemotePlayersManager>(remote, report);
            Ensure<RelationshipLineView>(remote, report);

            GameObject fc = FindOrCreateRoot(scene, FishingControllerName, created);
            var fishingCtrl = Ensure<FishingController>(fc, report);
            TryCall(() => fishingCtrl.EnsureVisuals(), "FishingController.EnsureVisuals()", report, thieu);

            GameObject es = FindOrCreateRoot(scene, EventSystemName, created);
            Ensure<EventSystem>(es, report);
            Ensure<StandaloneInputModule>(es, report);

            BuildHudCanvas(scene, created, report, thieu);
            BuildWorldCanvas(scene, cam, created, report);

            // 3. Bước 6: lấy ngày-đêm + mưa + Grid_Iso45 từ farm, nới CameraBounds, đổi nền camera
            if (showProgress) { Buoc(3, 4, "Lấy Ngày-đêm + Mưa + Grid_Iso45 từ farm (mở SCN_Farm additive ~10-30 s)…"); }
            report.AppendLine("── BƯỚC 6 (farm assets) ──");
            try { ImportFarmAssets(scene, cam, created, report, thieu); }
            catch (Exception e) { report.AppendLine("  ✖ Bước 6 lỗi: " + e.Message); thieu.Add("Bước 6 (farm assets) lỗi — " + e.Message + " (xem Console); chạy lại menu 6"); Debug.LogException(e); }

            // 4. Build Settings
            AddToBuildSettings(report);

            // 5. Lưu scene (tool sở hữu)
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            report.AppendLine(saved ? "✔ Đã lưu " + FishingIds.FishingScenePath : "✖ Không lưu được scene (xem Console).");
            if (created.Count > 0) { report.AppendLine("  Object tạo mới: " + string.Join(", ", created)); }
            else { report.AppendLine("  Không tạo object mới (đã đủ)."); }
            report.AppendLine("  SẾP LÀM: vẽ tile lên " + GridIsoName + "/Tilemap_IsoGrass… (Palette_Iso45), kéo Zone_01 trùng mép nước, chỉnh CameraBounds theo map.");
            return saved;
        }

        // ── Camera ──
        private static Camera BuildCamera(Scene scene, FishingConfig cfg, List<string> created, StringBuilder report)
        {
            GameObject go = FindOrCreateRoot(scene, MainCameraName, created);
            bool isNew = created.Contains(MainCameraName);
            var cam = go.GetComponent<Camera>();
            if (cam == null)
            {
                cam = Undo.AddComponent<Camera>(go);
                cam.orthographic = true;
                cam.orthographicSize = cfg.cameraOrthoSize;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = CameraBackground;
                cam.nearClipPlane = -50f;
                cam.farClipPlane = 50f;
            }
            if (isNew) { go.transform.position = new Vector3(0f, 0f, -10f); }
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.up;
            go.tag = "MainCamera";
            Ensure<AudioListener>(go, report);
            Ensure<FishingCameraFollow>(go, report);
            try
            {
                if (go.GetComponent<UniversalAdditionalCameraData>() == null) { Undo.AddComponent<UniversalAdditionalCameraData>(go); }
            }
            catch (Exception e) { report.AppendLine("  ⚠ Không thêm được UniversalAdditionalCameraData (URP sẽ tự thêm khi chọn camera): " + e.Message); }
            report.AppendLine("  ✓ Main Camera ortho " + cam.orthographicSize.ToString("0.##", CultureInfo.InvariantCulture) + ", sort axis Y, FishingCameraFollow");
            return cam;
        }

        // ── Grid_Fishing (cũ, 3 tilemap) — KHÔNG tạo mới nữa (bước 6 luôn đảm bảo Grid_Iso45); chỉ vá component nếu Sếp còn giữ Grid_Fishing có tile ──
        private static void BuildGrid(Scene scene, List<string> created, StringBuilder report)
        {
            if (FindRoot(scene, FishingIds.GridName) == null) { report.AppendLine("  · Grid: bước 6 đảm bảo " + GridIsoName + " (không tạo Grid_Fishing cũ)"); return; }
            GameObject gridGo = FindOrCreateRoot(scene, FishingIds.GridName, created);
            var grid = gridGo.GetComponent<Grid>();
            if (grid == null)
            {
                grid = Undo.AddComponent<Grid>(gridGo);
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(1f, 0.5f, 1f);
            }
            MakeTilemapLayer(gridGo.transform, "Ground", "Bottom", 0, created);
            MakeTilemapLayer(gridGo.transform, "Water", "Default", 1, created);
            MakeTilemapLayer(gridGo.transform, "Decor", "Objects", 2, created);
            report.AppendLine("  ✓ Grid_Fishing (Isometric 1x0.5) + Ground/Water/Decor (TilemapRenderer Individual) — để trống cho Sếp vẽ");
        }

        private static void MakeTilemapLayer(Transform parent, string name, string layer, int order, List<string> created)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
                created.Add(parent.name + "/" + name);
            }
            else { go = t.gameObject; }
            if (go.GetComponent<Tilemap>() == null) { Undo.AddComponent<Tilemap>(go); }
            var r = go.GetComponent<TilemapRenderer>();
            if (r == null)
            {
                r = Undo.AddComponent<TilemapRenderer>(go);
                r.mode = TilemapRenderer.Mode.Individual;
                r.sortingLayerName = TouristSortingLayers.ResolveOrOverride(layer, new[] { layer, "Default" });
                r.sortingOrder = order;
            }
        }

        // ── Zone ──
        private static void BuildZones(Scene scene, List<string> created, StringBuilder report)
        {
            GameObject zones = FindOrCreateRoot(scene, FishingIds.FishingZonesRoot, created);
            Transform z1 = zones.transform.Find("Zone_01");
            GameObject zoneGo;
            if (z1 == null)
            {
                zoneGo = new GameObject("Zone_01");
                Undo.RegisterCreatedObjectUndo(zoneGo, UndoLabel);
                zoneGo.transform.SetParent(zones.transform, false);
                zoneGo.transform.position = new Vector3(0f, 1.5f, 0f);
                created.Add(FishingIds.FishingZonesRoot + "/Zone_01");
            }
            else { zoneGo = z1.gameObject; }

            var poly = zoneGo.GetComponent<PolygonCollider2D>();
            if (poly == null)
            {
                poly = Undo.AddComponent<PolygonCollider2D>(zoneGo);
                poly.isTrigger = true;
                poly.pathCount = 1;
                // Hình thoi rộng 4, cao 2 quanh tâm (local)
                poly.SetPath(0, new[] { new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(0f, -1f), new Vector2(-2f, 0f) });
            }
            Ensure<FishingZone>(zoneGo, report);
            report.AppendLine("  ✓ FishingZones/Zone_01 (thoi 4x2 quanh (0,1.5), trigger) — Sếp kéo trùng mép nước");
        }

        // ── DayNight FALLBACK (chỉ khi thiếu prefab farm) ──
        // Renderer2D chỉ có blend style 0 dùng được cho Global và mỗi sorting layer chỉ chịu 1 Global/blend → CHỈ 1 đèn Global (AmbientLight).
        // Day/Night/Rim để null: controller bỏ qua (null-check), Ambient vẫn đổi màu/độ sáng theo giờ.
        private static void BuildDayNightFallback(Scene scene, Camera cam, List<string> created, StringBuilder report, List<string> thieu)
        {
            GameObject dn = FindOrCreateRoot(scene, FishingIds.DayNightRootName, created);
            bool ctrlNew = dn.GetComponent<Day_Night.DayNightCycleController>() == null;
            var ctrl = Ensure<Day_Night.DayNightCycleController>(dn, report);
            if (ctrlNew) { TryCall(() => ctrl.ResetToHappyHarvestDefaults(), "DayNightCycleController.ResetToHappyHarvestDefaults()", report, thieu); }

            Undo.RecordObject(ctrl, UndoLabel);
            try
            {
                if (ctrl.AmbientLight == null) { ctrl.AmbientLight = MakeGlobalLight(dn.transform, "AmbientLight", 1f, created); }
            }
            catch (Exception e)
            {
                report.AppendLine("  ✖ Light2D lỗi (API URP?): " + e.Message);
                thieu.Add("DayNight: không tạo được Light2D — thêm tay 1 Light2D Global và kéo vào AmbientLight của DayNightCycleController");
            }

            var weather = Ensure<Day_Night.DayNightWeatherSystem>(dn, report);
            if (ctrl.WeatherSystem == null) { ctrl.WeatherSystem = weather; }
            if (weather.SearchRoot == null) { Undo.RecordObject(weather, UndoLabel); weather.SearchRoot = dn.transform; }

            // Mưa: mesh thủ tục tự follow camera. Số mặc định là cho farm (×150) → scene câu (1 unit) đặt lại khi TẠO MỚI.
            Transform rainT = dn.transform.Find("RainOverlay");
            GameObject rain;
            bool rainNew = rainT == null;
            if (rainNew)
            {
                rain = new GameObject("RainOverlay");
                Undo.RegisterCreatedObjectUndo(rain, UndoLabel);
                rain.transform.SetParent(dn.transform, false);
                created.Add(FishingIds.DayNightRootName + "/RainOverlay");
            }
            else { rain = rainT.gameObject; }
            var overlay = Ensure<Day_Night.DayNightRainOverlay>(rain, report);   // RequireComponent tự thêm MeshFilter/MeshRenderer
            if (rainNew)
            {
                overlay.AreaSize = new Vector2(16f, 10f);
                overlay.DropCount = 160;
                overlay.FallSpeed = 6f;
                overlay.DropLength = 0.45f;
                overlay.DropWidth = 0.015f;
                overlay.DropHeadSize = 0.03f;
                overlay.SortingLayerName = TouristSortingLayers.ResolveOrOverride("Foreground", TouristSortingLayers.Overlay);
                overlay.CameraOverride = cam;
            }
            var elem = Ensure<Day_Night.DayNightWeatherElement>(rain, report);
            if (rainNew) { elem.WeatherType = Day_Night.DayNightWeatherType.Rain; }

            EditorUtility.SetDirty(ctrl);
            report.AppendLine("  ⚠ DayNight FALLBACK (thiếu prefab " + DayNightWeatherPrefabPath + "): controller + 1 Light2D Global (AmbientLight) + WeatherSystem + RainOverlay");
            thieu.Add("Thiếu prefab " + DayNightWeatherPrefabPath + " — dùng DayNight fallback 1 đèn; kéo prefab farm vào rồi chạy lại menu 6");
        }

        private static Light2D MakeGlobalLight(Transform parent, string name, float intensity, List<string> created)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
                created.Add(parent.name + "/" + name);
            }
            else { go = t.gameObject; }

            var light = go.GetComponent<Light2D>();
            if (light != null) { return light; }
            light = Undo.AddComponent<Light2D>(go);
            light.intensity = intensity;
            light.color = Color.white;

            // lightType setter có thể internal tuỳ bản URP → ghi qua SerializedObject cho chắc.
            var so = new SerializedObject(light);
            var pType = so.FindProperty("m_LightType");
            if (pType != null) { pType.intValue = (int)Light2D.LightType.Global; }
            var pBlend = so.FindProperty("m_BlendStyleIndex");
            if (pBlend != null) { pBlend.intValue = 0; }
            var pLayers = so.FindProperty("m_ApplyToSortingLayers");
            if (pLayers != null && pLayers.isArray)
            {
                var layers = SortingLayer.layers;
                pLayers.arraySize = layers.Length;
                for (int i = 0; i < layers.Length; i++) { pLayers.GetArrayElementAtIndex(i).intValue = layers[i].id; }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return light;
        }

        // ─────────────────────────────────────────────────────────────────
        //  BƯỚC 6: farm assets
        // ─────────────────────────────────────────────────────────────────

        /// <summary>a) prefab DayNightWeatherSetup, b) Grid_Iso45 từ SCN_Farm, c) CameraBounds, d) nền camera. Idempotent.</summary>
        private static void ImportFarmAssets(Scene scene, Camera cam, List<string> created, StringBuilder report, List<string> thieu)
        {
            ImportDayNightWeatherPrefab(scene, cam, created, report, thieu);
            ImportGridIso45(scene, created, report, thieu);
            FixCameraBounds(scene, report);
            FixCameraBackground(cam, report);
        }

        // ── a) DayNightWeatherSetup ──
        private static void ImportDayNightWeatherPrefab(Scene scene, Camera cam, List<string> created, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("  (a) " + DayNightWeatherName);
            GameObject prefab = LoadDayNightWeatherPrefab();
            if (prefab == null)
            {
                report.AppendLine("    ✖ Không tìm thấy prefab " + DayNightWeatherPrefabPath + " (guid " + DayNightWeatherPrefabGuid + ") — giữ DayNight hiện có.");
                thieu.Add("Thiếu prefab " + DayNightWeatherPrefabPath + " — chép từ farm vào rồi chạy lại menu 6");
                return;
            }

            // Gỡ artifact "DayNight" tự dựng (5 Light2D Global cùng blend 0 → xung đột, nhân vật đen thui). Điều kiện chặt: đúng tên, không thuộc prefab nào, có DayNightCycleController.
            GameObject oldDn = FindRoot(scene, FishingIds.DayNightRootName);
            if (oldDn != null)
            {
                bool laArtifact = !PrefabUtility.IsPartOfAnyPrefab(oldDn) && oldDn.GetComponent<Day_Night.DayNightCycleController>() != null;
                if (laArtifact)
                {
                    int soDenGlobal = CountGlobalLights(oldDn);
                    Undo.DestroyObjectImmediate(oldDn);
                    report.AppendLine("    ✓ Đã gỡ object DayNight tự dựng (đã gỡ " + soDenGlobal.ToString(CultureInfo.InvariantCulture) + " đèn Global xung đột — nguyên nhân 'More than one global light' + sprite đen).");
                }
                else if (oldDn.activeSelf)
                {
                    oldDn.SetActive(false);
                    EditorUtility.SetDirty(oldDn);
                    report.AppendLine("    ⚠ Object DayNight KHÔNG phải artifact tool (thuộc prefab hoặc không có controller) → chỉ SetActive(false), không xoá. Sếp kiểm tay.");
                }
                else { report.AppendLine("    · Object DayNight cũ đang tắt, để nguyên."); }
            }

            // Find-or-create instance prefab (giữ liên kết prefab)
            GameObject inst = FindRoot(scene, DayNightWeatherName);
            bool moi = false;
            if (inst == null)
            {
                inst = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (inst == null) { report.AppendLine("    ✖ InstantiatePrefab trả null."); thieu.Add(DayNightWeatherName + ": InstantiatePrefab thất bại"); return; }
                Undo.RegisterCreatedObjectUndo(inst, UndoLabel);
                inst.name = DayNightWeatherName;
                created.Add(DayNightWeatherName);
                moi = true;
            }
            else if (!PrefabUtility.IsPartOfAnyPrefab(inst)) { report.AppendLine("    ⚠ " + DayNightWeatherName + " có sẵn nhưng KHÔNG liên kết prefab — giữ nguyên."); }

            // Scene câu world 1 unit (gốc Happy Harvest) → scale (1,1,1). Farm scale 220-240× vì farm world ×150.
            if (moi || inst.transform.localScale != Vector3.one || inst.transform.position != Vector3.zero)
            {
                Undo.RecordObject(inst.transform, UndoLabel);
                inst.transform.position = Vector3.zero;
                inst.transform.localScale = Vector3.one;
            }
            if (!inst.activeSelf) { inst.SetActive(true); }

            // Controller: chỉ nạp preset khi thật sự thiếu gradient/curve (không gọi mù để không tạo override prefab).
            var ctrl = inst.GetComponentInChildren<Day_Night.DayNightCycleController>(true);
            if (ctrl == null)
            {
                report.AppendLine("    ⚠ Prefab không có DayNightCycleController — Bootstrap sẽ không nối được giờ từ farm.");
                thieu.Add(DayNightWeatherName + ": thiếu DayNightCycleController trong prefab");
            }
            else if (ctrl.DayLightGradient == null || ctrl.DayLightIntensityCurve == null || ctrl.DayLightIntensityCurve.length == 0)
            {
                Undo.RecordObject(ctrl, UndoLabel);
                TryCall(() => ctrl.ResetToHappyHarvestDefaults(), "DayNightCycleController.ResetToHappyHarvestDefaults() (prefab thiếu gradient)", report, thieu);
                EditorUtility.SetDirty(ctrl);
            }

            // RainOverlay: chỉ gán CameraOverride khi trống (SerializedObject, không đè).
            var overlays = inst.GetComponentsInChildren<Day_Night.DayNightRainOverlay>(true);
            for (int i = 0; i < overlays.Length; i++)
            {
                var so = new SerializedObject(overlays[i]);
                var pCam = so.FindProperty("CameraOverride");
                if (pCam != null && pCam.objectReferenceValue == null && cam != null)
                {
                    pCam.objectReferenceValue = cam;
                    so.ApplyModifiedProperties();
                    report.AppendLine("    ✓ " + overlays[i].name + ".CameraOverride ← Main Camera");
                }
            }

            // Cảnh báo số đo farm-scale còn sót (chỉ báo, không sửa prefab).
            var followers = inst.GetComponentsInChildren<Day_Night.DayNightRainFollower>(true);
            for (int i = 0; i < followers.Length; i++)
            {
                if (followers[i].Offset.magnitude > 10f) { report.AppendLine("    ⚠ " + followers[i].name + ".Offset = " + followers[i].Offset + " (số farm ×150?) — mưa VFX có thể lệch xa camera ở world 1 unit; Sếp kiểm khi mưa."); }
            }

            report.AppendLine("    ✓ " + DayNightWeatherName + (moi ? " (mới, liên kết prefab, scale 1, vị trí 0)" : " (đã có)"));
            CheckGlobalLightConflicts(report, thieu);
            EnsureLightDimmer(scene, inst, created, report);
        }

        /// <summary>
        /// [Lead vòng 16] Bộ giảm sáng: đặt trên FishingSceneRoot (KHÔNG thêm component vào instance prefab để không tạo override),
        /// trỏ lightsRoot vào DayNightWeatherSetup. Vì sao cần: xem doc FishingLightDimmer (farm ~2.4x + Point 43.59 unit ⇒ cháy trắng).
        /// </summary>
        private static void EnsureLightDimmer(Scene scene, GameObject dayNightInst, List<string> created, StringBuilder report)
        {
            GameObject sceneRoot = FindOrCreateRoot(scene, FishingIds.SceneRootName, created);
            var dimmer = Ensure<FishingLightDimmer>(sceneRoot, report);
            if (dimmer.LightsRoot == null && dayNightInst != null)
            {
                Undo.RecordObject(dimmer, UndoLabel);
                dimmer.LightsRoot = dayNightInst.transform;
                EditorUtility.SetDirty(dimmer);
            }
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            report.AppendLine("    ✓ FishingLightDimmer trên " + FishingIds.SceneRootName + " → nhân " +
                (cfg != null ? cfg.sceneLightMultiplier : 0.5f).ToString("0.##", CultureInfo.InvariantCulture) +
                " vào mọi Light2D của " + DayNightWeatherName + " (chỉnh ở FishingConfig.sceneLightMultiplier, đổi sống trong Play).");
        }

        private static GameObject LoadDayNightWeatherPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DayNightWeatherPrefabPath);
            if (prefab != null) { return prefab; }
            string byGuid = AssetDatabase.GUIDToAssetPath(DayNightWeatherPrefabGuid);
            if (!string.IsNullOrEmpty(byGuid)) { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(byGuid); }
            return prefab;
        }

        private static int CountGlobalLights(GameObject root)
        {
            int n = 0;
            var lights = root.GetComponentsInChildren<Light2D>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].lightType == Light2D.LightType.Global) { n++; }
            }
            return n;
        }

        /// <summary>Đếm Light2D Global trong scene theo blend style; > 1 cùng blend → FAIL rõ tên object (Unity sẽ báo "More than one global light").</summary>
        private static void CheckGlobalLightConflicts(StringBuilder report, List<string> thieu)
        {
            var lights = UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var byBlend = new Dictionary<int, List<string>>();
            for (int i = 0; i < lights.Length; i++)
            {
                var l = lights[i];
                if (l == null || l.lightType != Light2D.LightType.Global) { continue; }
                List<string> names;
                if (!byBlend.TryGetValue(l.blendStyleIndex, out names)) { names = new List<string>(); byBlend[l.blendStyleIndex] = names; }
                names.Add(HierarchyPath(l.transform));
            }
            if (byBlend.Count == 0) { report.AppendLine("    ⚠ Không có Light2D Global nào đang bật trong scene — sprite sẽ tối nếu Renderer2D bật lighting."); return; }
            foreach (var kv in byBlend)
            {
                if (kv.Value.Count > 1)
                {
                    string ds = string.Join(", ", kv.Value);
                    report.AppendLine("    ✖ FAIL: " + kv.Value.Count.ToString(CultureInfo.InvariantCulture) + " Light2D Global cùng blend style " + kv.Key.ToString(CultureInfo.InvariantCulture) + ": " + ds);
                    thieu.Add("Còn " + kv.Value.Count + " đèn Global cùng blend " + kv.Key + " (" + ds + ") — tắt/xoá bớt, chỉ giữ 1");
                }
                else { report.AppendLine("    ✓ 1 Light2D Global blend " + kv.Key.ToString(CultureInfo.InvariantCulture) + ": " + kv.Value[0]); }
            }
        }

        // ── b) Grid_Iso45 ──
        private static void ImportGridIso45(Scene scene, List<string> created, StringBuilder report, List<string> thieu)
        {
            report.AppendLine("  (b) " + GridIsoName);
            GameObject gridIso = FindRoot(scene, GridIsoName);
            if (gridIso != null) { report.AppendLine("    · Đã có " + GridIsoName + " — giữ nguyên (không đè tile Sếp vẽ)."); }
            else
            {
                gridIso = CopyGridIso45FromFarm(scene, report, thieu);
                if (gridIso == null)
                {
                    gridIso = CreateGridIso45Fallback(scene, report);
                    report.AppendLine("    ⚠ Không lấy được từ farm → tạo tay " + GridIsoName + " + 9 tilemap (Isometric 1x0.5, Individual).");
                }
                Undo.RegisterCreatedObjectUndo(gridIso, UndoLabel);
                created.Add(GridIsoName);
            }
            // Đảm bảo Grid iso đúng số đo
            var grid = gridIso.GetComponent<Grid>();
            if (grid == null) { grid = Undo.AddComponent<Grid>(gridIso); }
            if (grid.cellLayout != GridLayout.CellLayout.Isometric || grid.cellSize != new Vector3(1f, 0.5f, 1f))
            {
                Undo.RecordObject(grid, UndoLabel);
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(1f, 0.5f, 1f);
                report.AppendLine("    ✓ Grid → Isometric, cellSize (1, 0.5, 1)");
            }

            // Grid_Fishing cũ: chỉ gỡ khi mọi tilemap con rỗng (artifact rỗng của tool).
            GameObject oldGrid = FindRoot(scene, FishingIds.GridName);
            if (oldGrid != null)
            {
                var maps = oldGrid.GetComponentsInChildren<Tilemap>(true);
                int used = 0;
                for (int i = 0; i < maps.Length; i++) { used += maps[i].GetUsedTilesCount(); }
                if (used == 0)
                {
                    Undo.DestroyObjectImmediate(oldGrid);
                    report.AppendLine("    ✓ Đã gỡ " + FishingIds.GridName + " cũ (mọi tilemap rỗng — artifact tool).");
                }
                else { report.AppendLine("    ⚠ " + FishingIds.GridName + " cũ CÓ " + used.ToString(CultureInfo.InvariantCulture) + " tile → GIỮ; Sếp chuyển tile sang " + GridIsoName + " rồi tự xoá."); }
            }

            BuildElevationLayers(gridIso, created, report);
            BuildStepZoneSample(scene, created, report);
        }

        // ─────────────────────────────────────────────────────────────────
        //  BẬC NÚI (Dev F): 3 lớp mặt bậc + 1 lớp vách chặn
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Find-or-create dưới Grid_Iso45: Tilemap_Cliff (order 5, không offset, CÓ va chạm chặn) và Tilemap_Elev1/2/3
        /// (offset Y +0.25 / +0.50 / +0.75, order 10/20/30, KHÔNG va chạm — chỉ là mặt bậc để đi lên).
        /// Idempotent: tilemap đã có thì chỉ vá component thiếu, KHÔNG đè offset Sếp đã chỉnh tay.
        /// </summary>
        private static void BuildElevationLayers(GameObject gridIso, List<string> created, StringBuilder report)
        {
            report.AppendLine("  (b2) Bậc núi dưới " + GridIsoName);
            if (gridIso == null) { report.AppendLine("    ✖ Không có " + GridIsoName + " — bỏ qua bậc núi."); return; }

            EnsureElevTilemap(gridIso.transform, CliffTilemapName, 0f, CliffSortingOrder, true, created, report);
            for (int i = 0; i < ElevTilemapNames.Length; i++)
            {
                EnsureElevTilemap(gridIso.transform, ElevTilemapNames[i], ElevOffsetsY[i], ElevSortingOrders[i], false, created, report);
            }
            report.AppendLine("    · Cách vẽ: nền ở Tilemap_IsoGrass → mặt bậc 1 ở Tilemap_Elev1 → bậc 2 ở Elev2 → bậc 3 ở Elev3; mép vách vẽ ở " + CliffTilemapName + " (chặn đi xuyên núi).");
        }

        /// <summary>1 lớp bậc: Tilemap + TilemapRenderer (Individual, layer Objects). cliff = true thì thêm bộ va chạm gộp (Static).</summary>
        private static void EnsureElevTilemap(Transform parent, string name, float offsetY, int order, bool cliff, List<string> created, StringBuilder report)
        {
            Transform t = parent.Find(name);
            GameObject go;
            bool moi = t == null;
            if (moi)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0f, offsetY, 0f);
                created.Add(parent.name + "/" + name);
            }
            else { go = t.gameObject; }

            if (go.GetComponent<Tilemap>() == null) { Undo.AddComponent<Tilemap>(go); }
            var r = go.GetComponent<TilemapRenderer>();
            if (r == null)
            {
                r = Undo.AddComponent<TilemapRenderer>(go);
                r.mode = TilemapRenderer.Mode.Individual;
                r.sortingLayerName = TouristSortingLayers.ResolveOrOverride("Objects", TouristSortingLayers.Visitor);
                r.sortingOrder = order;
            }

            if (cliff)
            {
                var col = go.GetComponent<TilemapCollider2D>();
                if (col == null)
                {
                    col = Undo.AddComponent<TilemapCollider2D>(go);
                    col.isTrigger = false;
                }
                // Rigidbody2D phải có TRƯỚC CompositeCollider2D (RequireComponent sẽ tự thêm bản Dynamic nếu thiếu).
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = Undo.AddComponent<Rigidbody2D>(go);
                    rb.bodyType = RigidbodyType2D.Static;
                }
                else if (rb.bodyType != RigidbodyType2D.Static)
                {
                    Undo.RecordObject(rb, UndoLabel);
                    rb.bodyType = RigidbodyType2D.Static;
                    report.AppendLine("    ✓ " + name + ": Rigidbody2D → Static (vách núi không được rơi).");
                }
                if (go.GetComponent<CompositeCollider2D>() == null)
                {
                    var comp = Undo.AddComponent<CompositeCollider2D>(go);
                    comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
                }
                bool gopDuoc = SetUsedByComposite(col);
                if (!gopDuoc) { report.AppendLine("    ⚠ " + name + ": không đặt được 'Used By Composite' bằng SerializedObject — Sếp tick tay ở TilemapCollider2D."); }
            }

            float y = go.transform.localPosition.y;
            string trangThai = moi ? "MỚI" : "đã có";
            bool lechOffset = !moi && Mathf.Abs(y - offsetY) > 0.001f;
            report.AppendLine("    ✓ " + name + " (" + trangThai + ", y = " + y.ToString("0.##", CultureInfo.InvariantCulture) + ", order " + order.ToString(CultureInfo.InvariantCulture) + (cliff ? ", CÓ va chạm chặn" : "") + ")" + (lechOffset ? " — offset khác " + offsetY.ToString("0.##", CultureInfo.InvariantCulture) + " (Sếp đã chỉnh), GIỮ nguyên." : string.Empty));
        }

        /// <summary>usedByComposite đã đổi tên qua các bản Unity (m_UsedByComposite → m_CompositeOperation) → ghi qua SerializedObject cho an toàn.</summary>
        private static bool SetUsedByComposite(Collider2D col)
        {
            if (col == null) { return false; }
            var so = new SerializedObject(col);
            var pOp = so.FindProperty("m_CompositeOperation");    // Unity 2023.1+ / Unity 6: 0 = None, 1 = Merge
            if (pOp != null)
            {
                if (pOp.intValue != 1) { pOp.intValue = 1; so.ApplyModifiedProperties(); }
                return true;
            }
            var pOld = so.FindProperty("m_UsedByComposite");      // bản cũ
            if (pOld != null)
            {
                if (!pOld.boolValue) { pOld.boolValue = true; so.ApplyModifiedProperties(); }
                return true;
            }
            return false;
        }

        /// <summary>Tạo sẵn 1 vùng lên bậc mẫu ElevSteps/Step_01 (BoxCollider2D isTrigger 1x0.5 + ElevationStepZone) để Sếp nhân bản.</summary>
        private static void BuildStepZoneSample(Scene scene, List<string> created, StringBuilder report)
        {
            GameObject root = FindOrCreateRoot(scene, ElevationStepZone.RootName, created);
            Transform t = root.transform.Find(ElevationStepZone.SampleName);
            GameObject go;
            bool moi = t == null;
            if (moi)
            {
                go = new GameObject(ElevationStepZone.SampleName);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = StepZoneSamplePos;
                created.Add(ElevationStepZone.RootName + "/" + ElevationStepZone.SampleName);
            }
            else { go = t.gameObject; }

            var box = go.GetComponent<BoxCollider2D>();
            if (box == null)
            {
                box = Undo.AddComponent<BoxCollider2D>(go);
                box.isTrigger = true;
                box.size = StepZoneSize;
            }
            if (!box.isTrigger) { Undo.RecordObject(box, UndoLabel); box.isTrigger = true; }
            Ensure<ElevationStepZone>(go, report);
            report.AppendLine("    ✓ " + ElevationStepZone.RootName + "/" + ElevationStepZone.SampleName + " (" + (moi ? "MỚI" : "đã có") + ", BoxCollider2D trigger 1x0.5) — Sếp Ctrl+D nhân bản, kéo tới MỖI lối lên bậc.");
        }

        /// <summary>Mở SCN_Farm additive → clone root Grid_Iso45 → chuyển sang scene câu → làm sạch. Luôn đóng farm (try/finally), KHÔNG lưu farm. Trả null nếu không có.</summary>
        private static GameObject CopyGridIso45FromFarm(Scene target, StringBuilder report, List<string> thieu)
        {
            if (!LoadFarmSceneAssetExists()) { report.AppendLine("    ✖ Không thấy " + FarmScenePath); thieu.Add("Thiếu " + FarmScenePath + " — không chép được " + GridIsoName); return null; }
            Scene farm = default(Scene);
            GameObject copy = null;
            try
            {
                EditorUtility.DisplayProgressBar("★ Setup Hồ Câu", "Mở SCN_Farm additive để chép " + GridIsoName + " (16 MB, ~10-30 s)…", 0.8f);
                farm = EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Additive);
                if (!farm.IsValid() || !farm.isLoaded) { report.AppendLine("    ✖ Không mở được SCN_Farm additive."); return null; }
                GameObject src = FindRoot(farm, GridIsoName);
                if (src == null)
                {
                    report.AppendLine("    ✖ SCN_Farm không có root " + GridIsoName + ".");
                    thieu.Add("SCN_Farm không có root " + GridIsoName + " — đã tạo tay thay thế");
                    return null;
                }
                copy = UnityEngine.Object.Instantiate(src);
                if (copy.scene != target) { SceneManager.MoveGameObjectToScene(copy, target); }
                copy.name = GridIsoName;
                if (PrefabUtility.IsPartOfPrefabInstance(copy)) { PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); }

                // Root: scale 1 (farm 150), vị trí 0
                copy.transform.SetParent(null, false);
                copy.transform.position = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;

                // Con: scale 1 (farm 2), xoá tile, gỡ component lạ; giữ sorting layer/order/material của TilemapRenderer.
                int cleared = 0;
                var children = copy.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    var t = children[i];
                    if (t == copy.transform) { continue; }
                    t.localScale = Vector3.one;
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    var tm = t.GetComponent<Tilemap>();
                    if (tm != null) { tm.ClearAllTiles(); cleared++; }
                }
                int removed = StripGridCopy(copy);
                EditorUtility.SetDirty(copy);
                report.AppendLine("    ✓ Chép " + GridIsoName + " từ SCN_Farm: " + (children.Length - 1).ToString(CultureInfo.InvariantCulture) + " con, xoá tile " + cleared.ToString(CultureInfo.InvariantCulture) + " tilemap, gỡ " + removed.ToString(CultureInfo.InvariantCulture) + " component lạ, scale 1.");
                return copy;
            }
            catch (Exception e)
            {
                report.AppendLine("    ✖ Chép từ farm lỗi: " + e.Message);
                Debug.LogException(e);
                if (copy != null) { UnityEngine.Object.DestroyImmediate(copy); }
                return null;
            }
            finally
            {
                if (farm.IsValid() && farm.isLoaded && farm != target) { EditorSceneManager.CloseScene(farm, true); }   // KHÔNG lưu farm
                if (target.IsValid()) { SceneManager.SetActiveScene(target); }
                EditorUtility.ClearProgressBar();
            }
        }

        private static GameObject CreateGridIso45Fallback(Scene scene, StringBuilder report)
        {
            var go = new GameObject(GridIsoName);
            SceneManager.MoveGameObjectToScene(go, scene);
            var grid = go.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            for (int i = 0; i < GridIsoLayers.Length; i++)
            {
                var child = new GameObject(GridIsoLayers[i]);
                child.transform.SetParent(go.transform, false);
                child.AddComponent<Tilemap>();
                var r = child.AddComponent<TilemapRenderer>();
                r.mode = TilemapRenderer.Mode.Individual;
                string layer = i == 0 ? "Bottom" : "Default";
                r.sortingLayerName = TouristSortingLayers.ResolveOrOverride(layer, new[] { layer, "Default" });
                r.sortingOrder = i;
            }
            return go;
        }

        /// <summary>Gỡ component không thuộc KeepOnGridCopy trên bản sao MÌNH VỪA TẠO. Lặp nhiều lượt vì RequireComponent chặn lượt đầu (giống StripCopy ở FishingFarmHookSetupTool).</summary>
        private static int StripGridCopy(GameObject root)
        {
            int removed = 0;
            for (int pass = 0; pass < 6; pass++)
            {
                bool any = false;
                var comps = root.GetComponentsInChildren<Component>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null || IsKeptOnGrid(c.GetType())) { continue; }
                    if (IsRequiredByOther(c)) { continue; }
                    try
                    {
                        UnityEngine.Object.DestroyImmediate(c);
                        if (c == null) { removed++; any = true; }
                    }
                    catch (Exception) { /* thử lại lượt sau */ }
                }
                if (!any) { break; }
            }
            return removed;
        }

        private static bool IsKeptOnGrid(Type t)
        {
            for (int i = 0; i < KeepOnGridCopy.Length; i++)
            {
                if (KeepOnGridCopy[i].IsAssignableFrom(t)) { return true; }
            }
            return false;
        }

        private static bool IsRequiredByOther(Component c)
        {
            var siblings = c.gameObject.GetComponents<Component>();
            Type ct = c.GetType();
            for (int i = 0; i < siblings.Length; i++)
            {
                var s = siblings[i];
                if (s == null || s == c) { continue; }
                var attrs = s.GetType().GetCustomAttributes(typeof(RequireComponent), true);
                for (int a = 0; a < attrs.Length; a++)
                {
                    var rc = attrs[a] as RequireComponent;
                    if (rc == null) { continue; }
                    if (Req(rc.m_Type0, ct) || Req(rc.m_Type1, ct) || Req(rc.m_Type2, ct)) { return true; }
                }
            }
            return false;
        }

        private static bool Req(Type required, Type candidate)
        {
            return required != null && required.IsAssignableFrom(candidate);
        }

        private static bool LoadFarmSceneAssetExists()
        {
            return File.Exists(FarmScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(FarmScenePath) != null;
        }

        // ── c) CameraBounds ──
        private static void FixCameraBounds(Scene scene, StringBuilder report)
        {
            report.AppendLine("  (c) " + FishingIds.CameraBoundsName);
            GameObject bounds = FindRoot(scene, FishingIds.CameraBoundsName);
            var box = bounds != null ? bounds.GetComponent<BoxCollider2D>() : null;
            if (box == null) { report.AppendLine("    · Không có BoxCollider2D CameraBounds — bỏ qua."); return; }
            if (Mathf.Approximately(box.size.x, CameraBoundsSizeOld.x) && Mathf.Approximately(box.size.y, CameraBoundsSizeOld.y))
            {
                Undo.RecordObject(box, UndoLabel);
                box.size = CameraBoundsSize;
                EditorUtility.SetDirty(box);
                report.AppendLine("    ✓ BoxCollider2D 14x9 (mặc định cũ, camera chỉ lắc ±1.3) → 40x26. Sếp co lại theo map sau khi vẽ.");
            }
            else { report.AppendLine("    · BoxCollider2D " + box.size.x.ToString("0.#", CultureInfo.InvariantCulture) + "x" + box.size.y.ToString("0.#", CultureInfo.InvariantCulture) + " (Sếp đã chỉnh) — giữ."); }
        }

        // ── d) Nền camera ──
        private static void FixCameraBackground(Camera cam, StringBuilder report)
        {
            report.AppendLine("  (d) Nền camera");
            if (cam == null) { report.AppendLine("    · Không có Main Camera — bỏ qua."); return; }
            if (ColorNear(cam.backgroundColor, CameraBackgroundOld))
            {
                Undo.RecordObject(cam, UndoLabel);
                cam.backgroundColor = CameraBackground;
                EditorUtility.SetDirty(cam);
                report.AppendLine("    ✓ Nền (0.08,0.26,0.14) xanh đậm → cỏ sáng dịu (0.47,0.66,0.38).");
            }
            else { report.AppendLine("    · Nền camera không phải màu cũ — giữ (Sếp đã chỉnh)."); }
        }

        private static bool ColorNear(Color a, Color b)
        {
            const float eps = 0.02f;
            return Mathf.Abs(a.r - b.r) < eps && Mathf.Abs(a.g - b.g) < eps && Mathf.Abs(a.b - b.b) < eps;
        }

        // ── Canvas HUD ──
        private static void BuildHudCanvas(Scene scene, List<string> created, StringBuilder report, List<string> thieu)
        {
            GameObject go = FindOrCreateRoot(scene, FishingIds.HudCanvasName, created);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = Undo.AddComponent<Canvas>(go);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
            }
            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = Undo.AddComponent<CanvasScaler>(go);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            Ensure<GraphicRaycaster>(go, report);
            var hud = Ensure<FishingHudUI>(go, report);
            TryCall(() => hud.BuildIfEmpty(), "FishingHudUI.BuildIfEmpty()", report, thieu);
            report.AppendLine("  ✓ Canvas_FishingHUD (Overlay 100, 1920x1080) + FishingHudUI");
        }

        // ── Canvas World ──
        private static void BuildWorldCanvas(Scene scene, Camera cam, List<string> created, StringBuilder report)
        {
            GameObject go = FindOrCreateRoot(scene, FishingIds.WorldCanvasName, created);
            go.layer = LayerMask.NameToLayer("UI");
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = Undo.AddComponent<Canvas>(go);
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = cam;
                canvas.sortingLayerName = TouristSortingLayers.ResolveOrOverride("Foreground", TouristSortingLayers.Overlay);
                canvas.sortingOrder = 500;
                go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) { rt.sizeDelta = new Vector2(1600f, 1000f); rt.position = Vector3.zero; }
            }
            if (canvas.worldCamera == null) { canvas.worldCamera = cam; }
            Ensure<GraphicRaycaster>(go, report);
            report.AppendLine("  ✓ Canvas_FishingWorld (WorldSpace, scale 0.01, Foreground/500) — bubble, name tag");
        }

        // ── Build Settings ──
        private static void AddToBuildSettings(StringBuilder report)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == FishingIds.FishingScenePath)
                {
                    if (!scenes[i].enabled) { scenes[i].enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); report.AppendLine("  ✓ Build Settings: bật lại " + FishingIds.FishingScenePath); }
                    else { report.AppendLine("  · Build Settings đã có " + FishingIds.FishingScenePath); }
                    return;
                }
            }
            scenes.Add(new EditorBuildSettingsScene(FishingIds.FishingScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            report.AppendLine("  ✓ Build Settings: thêm " + FishingIds.FishingScenePath);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────────────

        private static FishingConfig LoadConfig()
        {
            var db = AssetDatabase.LoadAssetAtPath<FishingDatabase>(FishingDataSetupTool.DatabasePath);
            if (db != null && db.config != null) { return db.config; }
            var tmp = ScriptableObject.CreateInstance<FishingConfig>();
            tmp.hideFlags = HideFlags.HideAndDontSave;
            return tmp;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            if (!scene.IsValid() || !scene.isLoaded) { return null; }
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == name) { return roots[i]; }
            }
            return null;
        }

        private static GameObject FindOrCreateRoot(Scene scene, string name, List<string> created)
        {
            var found = FindRoot(scene, name);
            if (found != null) { return found; }
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            SceneManager.MoveGameObjectToScene(go, scene);
            created.Add(name);
            return go;
        }


        /// <summary>
        /// Kẹp zoom theo map: khung nhìn (orthoSize × aspect) không được lớn hơn CameraBounds, nếu không camera bị ghim giữa và
        /// người chơi thấy vùng ngoài map. fitOrtho = min(caoMap/2, rộngMap/(2·aspect)). Ghi vào FishingConfig (asset, có Undo) + camera scene.
        /// </summary>
        private static void ClampZoomToMap(Scene scene, Vector3 mapSize, StringBuilder report, StringBuilder head)
        {
            FishingConfig cfg = FishingDatabase.ConfigOrDefault;
            if (cfg == null || cfg.hideFlags == HideFlags.HideAndDontSave) { report.AppendLine("  · Không có FishingConfig thật (chạy menu 1) — bỏ qua kẹp zoom."); return; }

            Camera cam = Camera.main;
            // [Reviewer L3] Không tin Camera.aspect trong Edit Mode (là aspect Game View lần render cuối). Lấy aspect RỘNG NHẤT
            // trong các máy mục tiêu (21:9 ≈ 2.33) để khung nhìn không lộ mép trên máy dài; máy 16:9 chỉ thấy hẹp hơn một chút.
            float aspect = 21f / 9f;
            float fullW = mapSize.x + CameraBoundsMargin * 2f;
            float fullH = mapSize.y + CameraBoundsMargin * 2f;
            // [Reviewer N2] FishingCameraFollow bỏ kẹp khi bounds < MinBoundsViewRatio × khung nhìn theo CẢ 2 trục → chia hệ số đó
            // (nay = 1.0) để ở zoom xa nhất khung nhìn vừa khít trục hẹp, trục dài vẫn kẹp — không bao giờ lộ ngoài map.
            float fitOrtho = Mathf.Min(fullH * 0.5f, fullW * 0.5f / aspect) / FishingCameraFollow.MinBoundsViewRatio;
            fitOrtho = Mathf.Max(2f, Mathf.Floor(fitOrtho * 10f) / 10f);   // [L5] ≥ 2 = [Min(2f)] của cameraZoomMax; làm tròn xuống 0.1

            bool doi = false;
            Undo.RecordObject(cfg, UndoLabel);
            if (cfg.cameraZoomMax > fitOrtho + 0.001f) { cfg.cameraZoomMax = fitOrtho; doi = true; }
            if (cfg.cameraOrthoSize > fitOrtho + 0.001f) { cfg.cameraOrthoSize = fitOrtho; doi = true; }
            if (cfg.cameraZoomMin > cfg.cameraZoomMax - 0.5f) { cfg.cameraZoomMin = Mathf.Max(1f, cfg.cameraZoomMax * 0.6f); doi = true; }
            if (doi) { EditorUtility.SetDirty(cfg); AssetDatabase.SaveAssetIfDirty(cfg); }   // [L2] SaveScene không lưu asset

            if (cam != null && cam.orthographic && cam.orthographicSize > fitOrtho + 0.001f)
            {
                Undo.RecordObject(cam, UndoLabel);
                cam.orthographicSize = cfg.cameraOrthoSize;
                EditorUtility.SetDirty(cam);
                doi = true;
            }
            // Zoom người chơi đã lưu PlayerPrefs có thể lớn hơn trần mới → FishingCameraFollow tự kẹp khi đọc, không cần xoá key.

            report.AppendLine("  ✓ Zoom theo map (aspect " + aspect.ToString("0.##", CultureInfo.InvariantCulture) + "): fitOrtho = " +
                fitOrtho.ToString("0.#", CultureInfo.InvariantCulture) + " → cameraOrthoSize " + cfg.cameraOrthoSize.ToString("0.#", CultureInfo.InvariantCulture) +
                ", zoom [" + cfg.cameraZoomMin.ToString("0.#", CultureInfo.InvariantCulture) + " – " + cfg.cameraZoomMax.ToString("0.#", CultureInfo.InvariantCulture) + "]" + (doi ? " (đã hạ)" : " (đã vừa, không đổi)"));
            if (doi) { head.AppendLine("Zoom đã kẹp theo map: tối đa " + cfg.cameraZoomMax.ToString("0.#", CultureInfo.InvariantCulture) + " — người chơi không nhìn ra ngoài vòng map."); }
        }

        /// <summary>
        /// Spawn phải nằm trong map và không trên ô nước. Ngoài map → ứng viên = tâm map; rồi xoắn ốc tìm ô có tile ở lớp cỏ/đất và
        /// KHÔNG có tile ở lớp nước (Tilemap_IsoWaterAnim) — tối đa 400 ô. Không đổi nếu spawn hiện tại đã hợp lệ.
        /// </summary>
        private static void FixSpawnInsideMap(Scene scene, GameObject gridIso, Bounds mapWorld, StringBuilder report, StringBuilder head)
        {
            GameObject spawn = FindRoot(scene, FishingIds.SpawnPointName);
            if (spawn == null)
            {
                Transform t = FindDeep(scene, FishingIds.SpawnPointName);
                spawn = t != null ? t.gameObject : null;
            }
            if (spawn == null) { report.AppendLine("  · Không thấy " + FishingIds.SpawnPointName + " — bỏ qua."); return; }

            var grid = gridIso.GetComponent<Grid>();
            Tilemap water = FindTilemap(gridIso, "Tilemap_IsoWaterAnim");
            // [L4] Đủ mọi lớp đi được (kể cả bậc Elev) để không dời oan spawn đang đứng trên bậc/đường đá.
            Tilemap[] ground =
            {
                FindTilemap(gridIso, "Tilemap_IsoGrass"), FindTilemap(gridIso, "Tilemap_IsoDirt"), FindTilemap(gridIso, "Tilemap_IsoSand"),
                FindTilemap(gridIso, "Tilemap_IsoDock"), FindTilemap(gridIso, "Tilemap_IsoStone"), FindTilemap(gridIso, "Tilemap_IsoDirtPatch"),
                FindTilemap(gridIso, "Tilemap_Elev1"), FindTilemap(gridIso, "Tilemap_Elev2"), FindTilemap(gridIso, "Tilemap_Elev3")
            };

            Vector3 cur = spawn.transform.position;
            bool trongMap = mapWorld.Contains(new Vector3(cur.x, cur.y, 0f));
            bool trenNuoc = water != null && water.GetTile(water.WorldToCell(cur)) != null;
            bool coDat = OnGround(ground, cur);
            if (trongMap && !trenNuoc && coDat)
            {
                report.AppendLine("  ✓ " + FishingIds.SpawnPointName + " (" + cur.x.ToString("0.##", CultureInfo.InvariantCulture) + ", " + cur.y.ToString("0.##", CultureInfo.InvariantCulture) + ") đã nằm trên đất trong map — giữ.");
                return;
            }

            if (grid == null) { report.AppendLine("  ⚠ " + GridIsoName + " không có Grid — không dời spawn được."); return; }
            Vector3 tam = mapWorld.center;
            Vector3Int c0 = grid.WorldToCell(new Vector3(tam.x, tam.y, 0f));
            Vector3? tot = null;
            // Xoắn ốc vuông quanh tâm: bán kính 0..10 ô.
            for (int r = 0; r <= 10 && tot == null; r++)
            {
                for (int dx = -r; dx <= r && tot == null; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) { continue; }
                        var cell = new Vector3Int(c0.x + dx, c0.y + dy, 0);
                        Vector3 w = grid.GetCellCenterWorld(cell);
                        if (!mapWorld.Contains(new Vector3(w.x, w.y, 0f))) { continue; }
                        if (water != null && water.GetTile(water.WorldToCell(w)) != null) { continue; }
                        if (!OnGround(ground, w)) { continue; }
                        tot = w;
                        break;
                    }
                }
            }
            if (tot == null)
            {
                report.AppendLine("  ⚠ Không tìm được ô đất trống gần tâm map cho spawn — Sếp kéo tay " + FishingIds.SpawnPointName + ".");
                head.AppendLine("⚠ Chưa dời được " + FishingIds.SpawnPointName + " (không thấy ô đất quanh tâm) — kéo tay vào vòng map.");
                return;
            }
            Undo.RecordObject(spawn.transform, UndoLabel);
            Vector3 moi = tot.Value;
            spawn.transform.position = new Vector3(moi.x, moi.y, cur.z);
            EditorUtility.SetDirty(spawn.transform);
            string lyDo = !trongMap ? "nằm ngoài map" : (trenNuoc ? "đứng trên ô nước" : "không có ô đất dưới chân");
            report.AppendLine("  ✓ " + FishingIds.SpawnPointName + " " + lyDo + " → dời tới (" + moi.x.ToString("0.##", CultureInfo.InvariantCulture) + ", " + moi.y.ToString("0.##", CultureInfo.InvariantCulture) + ") (ô đất gần tâm map).");
            head.AppendLine("Điểm spawn " + lyDo + " → đã dời vào trong map.");
        }

        private static bool OnGround(Tilemap[] ground, Vector3 world)
        {
            bool anyMap = false;
            for (int i = 0; i < ground.Length; i++)
            {
                if (ground[i] == null) { continue; }
                anyMap = true;
                if (ground[i].GetTile(ground[i].WorldToCell(world)) != null) { return true; }
            }
            return !anyMap;   // không có lớp đất nào để hỏi → coi như hợp lệ, không dời mù
        }

        private static Tilemap FindTilemap(GameObject gridIso, string name)
        {
            Transform t = gridIso.transform.Find(name);
            return t != null ? t.GetComponent<Tilemap>() : null;
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null) { continue; }
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++) { if (all[j] != null && all[j].name == name) { return all[j]; } }
            }
            return null;
        }

        private static T Ensure<T>(GameObject go, StringBuilder report) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null) { return c; }
            c = Undo.AddComponent<T>(go);
            report.AppendLine("    + " + go.name + " ← " + typeof(T).Name);
            return c;
        }

        private static string HierarchyPath(Transform t)
        {
            var sb = new StringBuilder(t.name);
            var p = t.parent;
            while (p != null) { sb.Insert(0, p.name + "/"); p = p.parent; }
            return sb.ToString();
        }

        /// <summary>Gọi API Dev khác, bắt lỗi để tool không dừng; ghi vào báo cáo + danh sách thiếu.</summary>
        private static void TryCall(Action call, string label, StringBuilder report, List<string> thieu)
        {
            try { call(); report.AppendLine("    ✓ " + label); }
            catch (Exception e)
            {
                report.AppendLine("    ✖ " + label + " lỗi: " + e.Message);
                thieu.Add(label + " lỗi — " + e.Message + " (xem Console)");
                Debug.LogException(e);
            }
        }
    }
}
