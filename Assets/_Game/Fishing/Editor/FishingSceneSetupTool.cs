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
    /// ★ TOOL MỘT NÚT Hồ Câu — CHỦ FILE: Dev D. Menu Tools/Farm Game/Hồ Câu/★ SETUP TẤT CẢ (1 nút).
    /// Chạy: Data (menu 1) → Nhân vật (menu 2) → tạo/mở SCN_Fishing + dựng hierarchy + BuildIfEmpty UI + Build Settings → SaveScene(SCN_Fishing).
    /// KHÔNG gắn vào SCN_Farm (menu 5 chạy riêng vì cần mở SCN_Farm). Scene SCN_Fishing do tool sở hữu nên ĐƯỢC lưu; báo rõ trong dialog.
    /// Find-or-create theo tên trong FishingIds, mọi AddComponent kiểm GetComponent trước, chỉ gán field trống, không DestroyImmediate object có sẵn.
    /// Mọi lời gọi BuildIfEmpty/EnsureVisuals của Dev A/B/C bọc try/catch để 1 lỗi không dừng tool.
    /// </summary>
    public static class FishingSceneSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuAll = MenuRoot + "★ SETUP TẤT CẢ (1 nút)";
        private const string MenuScene = MenuRoot + "3. Dựng scene SCN_Fishing";
        private const string MenuOpen = MenuRoot + "4. Mở scene SCN_Fishing";
        private const string UndoLabel = "Hồ Câu — Setup";

        private const string MainCameraName = "Main Camera";
        private const string EventSystemName = "EventSystem";
        private const string FishingControllerName = "FishingController";
        private const string SceneFolder = "Assets/_Game/Scenes";

        private static readonly Color CameraBackground = new Color(0.08f, 0.26f, 0.14f, 1f);   // xanh lá đậm

        // ─────────────────────────────────────────────────────────────────
        //  MENU
        // ─────────────────────────────────────────────────────────────────

        [MenuItem(MenuAll, false, 0)]
        public static void RunAll()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("★ Setup tất cả Hồ Câu",
                "Tool sẽ chạy 3 bước:\n\n" +
                "1. Tạo/cập nhật Data (FishingConfig, 10 cá, 4 cần, FishingDatabase ở Resources/Fishing)\n" +
                "2. Nhân vật PlayerF/PlayerM: importer + 8 clip + controller + prefab\n" +
                "3. Tạo (nếu chưa có) rồi mở scene " + FishingIds.FishingScenePath + ", dựng hierarchy, thêm vào Build Settings và LƯU scene này.\n\n" +
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
                Buoc(0, 3, "Data…");
                try { report.AppendLine(FishingDataSetupTool.RunSetup(true)); }
                catch (Exception e) { loi++; report.AppendLine("✖ Data lỗi: " + e.Message); Debug.LogException(e); }

                Buoc(1, 3, "Nhân vật (importer + anim + prefab)…");
                try
                {
                    report.AppendLine(FishingPlayerAnimSetupTool.RunSetup(true));
                    if (FishingPlayerAnimSetupTool.LastCharacterCount < 2) { thieu.Add("Nhân vật: chỉ dựng được " + FishingPlayerAnimSetupTool.LastCharacterCount + "/2 — kiểm 24 PNG ở " + FishingIds.ArtCharactersRoot); }
                }
                catch (Exception e) { loi++; report.AppendLine("✖ Nhân vật lỗi: " + e.Message); Debug.LogException(e); }

                Buoc(2, 3, "Scene SCN_Fishing…");
                try
                {
                    if (!BuildScene(report, thieu)) { loi++; }
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
                head.AppendLine("✔ XONG — Data, nhân vật, scene SCN_Fishing đã dựng và lưu.");
                head.AppendLine();
                head.AppendLine("BƯỚC KẾ (Sếp làm):");
                head.AppendLine("1) Vẽ map: chọn Grid_Fishing/Ground|Water|Decor, Tile Palette Palette_Iso45.");
                head.AppendLine("2) Kéo FishingZones/Zone_01 trùng mép nước.");
                head.AppendLine("3) Mở SCN_Farm → menu 5 (Gắn vào SCN_Farm) → Ctrl+S.");
                head.AppendLine("4) Play SCN_Farm → tab HỒ CÂU → vào phòng.");
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
            if (!EditorUtility.DisplayDialog("Dựng scene SCN_Fishing", "Tạo (nếu chưa có) rồi mở " + FishingIds.FishingScenePath + ", dựng hierarchy, thêm Build Settings và LƯU scene này.\nScene đang mở sẽ được hỏi lưu trước.", "Chạy", "Huỷ")) { return; }
            var report = new StringBuilder();
            var thieu = new List<string>();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            bool ok;
            try { ok = BuildScene(report, thieu); }
            finally { Undo.CollapseUndoOperations(Undo.GetCurrentGroup()); }
            Debug.Log(FishingIds.SetupLogTag + " Scene:\n" + report);
            var head = new StringBuilder(ok ? "✔ Scene đã dựng + lưu.\n" : "✖ Scene chưa xong, xem Console.\n");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            EditorUtility.DisplayDialog("Hồ Câu — Scene", head.ToString(), "OK");
        }

        [MenuItem(MenuOpen, false, 31)]
        public static void OpenFishingScene()
        {
            if (!File.Exists(FishingIds.FishingScenePath)) { EditorUtility.DisplayDialog("Hồ Câu", "Chưa có " + FishingIds.FishingScenePath + " — chạy menu 3 hoặc ★ trước.", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { return; }
            EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
        }

        private static void Buoc(int i, int tong, string mo)
        {
            EditorUtility.DisplayProgressBar("★ Setup Hồ Câu", "Bước " + (i + 1) + "/" + tong + ": " + mo, (float)i / tong);
        }

        // ─────────────────────────────────────────────────────────────────
        //  SCENE
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Tạo/mở SCN_Fishing, dựng hierarchy, Build Settings, SaveScene. Trả false nếu bị huỷ/lỗi nặng.</summary>
        public static bool BuildScene(StringBuilder report, List<string> thieu)
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
                var active = SceneManager.GetActiveScene();
                if (active.path != FishingIds.FishingScenePath)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { report.AppendLine("✖ Người dùng huỷ lưu scene đang mở — dừng."); return false; }
                    scene = EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
                }
                else { scene = active; }
                report.AppendLine("· mở scene có sẵn " + FishingIds.FishingScenePath);
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
            if (box == null) { box = Undo.AddComponent<BoxCollider2D>(bounds); box.isTrigger = true; box.size = new Vector2(14f, 9f); box.offset = Vector2.zero; }

            BuildDayNight(scene, cam, created, report, thieu);

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

            // 3. Build Settings
            AddToBuildSettings(report);

            // 4. Lưu scene (tool sở hữu)
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            report.AppendLine(saved ? "✔ Đã lưu " + FishingIds.FishingScenePath : "✖ Không lưu được scene (xem Console).");
            if (created.Count > 0) { report.AppendLine("  Object tạo mới: " + string.Join(", ", created)); }
            else { report.AppendLine("  Không tạo object mới (đã đủ)."); }
            report.AppendLine("  SẾP LÀM: vẽ tile lên Grid_Fishing/Ground|Water|Decor (Palette_Iso45), kéo Zone_01 trùng mép nước, chỉnh CameraBounds theo map.");
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

        // ── Grid + 3 tilemap ──
        private static void BuildGrid(Scene scene, List<string> created, StringBuilder report)
        {
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
                created.Add(FishingIds.GridName + "/" + name);
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

        // ── DayNight ──
        private static void BuildDayNight(Scene scene, Camera cam, List<string> created, StringBuilder report, List<string> thieu)
        {
            GameObject dn = FindOrCreateRoot(scene, FishingIds.DayNightRootName, created);
            bool ctrlNew = dn.GetComponent<Day_Night.DayNightCycleController>() == null;
            var ctrl = Ensure<Day_Night.DayNightCycleController>(dn, report);
            if (ctrlNew) { TryCall(() => ctrl.ResetToHappyHarvestDefaults(), "DayNightCycleController.ResetToHappyHarvestDefaults()", report, thieu); }

            Undo.RecordObject(ctrl, UndoLabel);
            Transform lightsRoot = FindOrCreateChild(dn.transform, "LightsRoot", created);
            if (ctrl.LightsRoot == null) { ctrl.LightsRoot = lightsRoot; }

            // 5 Light2D — tất cả Global (đèn nền), Sun/Moon rim nằm dưới LightsRoot (controller xoay theo giờ).
            // Controller không ép loại đèn; chọn Global vì scene câu không có normal map + không cần đèn điểm.
            try
            {
                if (ctrl.DayLight == null) { ctrl.DayLight = MakeGlobalLight(dn.transform, "DayLight", 1f, created, report); }
                if (ctrl.NightLight == null) { ctrl.NightLight = MakeGlobalLight(dn.transform, "NightLight", 0f, created, report); }
                if (ctrl.AmbientLight == null) { ctrl.AmbientLight = MakeGlobalLight(dn.transform, "AmbientLight", 0.8f, created, report); }
                if (ctrl.SunRimLight == null) { ctrl.SunRimLight = MakeGlobalLight(lightsRoot, "SunRimLight", 0.3f, created, report); }
                if (ctrl.MoonRimLight == null) { ctrl.MoonRimLight = MakeGlobalLight(lightsRoot, "MoonRimLight", 0f, created, report); }
            }
            catch (Exception e)
            {
                report.AppendLine("  ✖ Light2D lỗi (API URP?): " + e.Message);
                thieu.Add("DayNight: không tạo được Light2D — thêm tay 5 Light2D Global và kéo vào DayNightCycleController");
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
            report.AppendLine("  ✓ DayNight: controller (preset HappyHarvest) + LightsRoot + 5 Light2D Global + WeatherSystem + RainOverlay(Rain)");
        }

        private static Light2D MakeGlobalLight(Transform parent, string name, float intensity, List<string> created, StringBuilder report)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(parent, false);
                created.Add(FishingIds.DayNightRootName + "/" + name);
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

        private static GameObject FindOrCreateRoot(Scene scene, string name, List<string> created)
        {
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == name) { return roots[i]; }
            }
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            SceneManager.MoveGameObjectToScene(go, scene);
            created.Add(name);
            return go;
        }

        private static Transform FindOrCreateChild(Transform parent, string name, List<string> created)
        {
            Transform t = parent.Find(name);
            if (t != null) { return t; }
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            go.transform.SetParent(parent, false);
            created.Add(parent.name + "/" + name);
            return go.transform;
        }

        private static T Ensure<T>(GameObject go, StringBuilder report) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c != null) { return c; }
            c = Undo.AddComponent<T>(go);
            report.AppendLine("    + " + go.name + " ← " + typeof(T).Name);
            return c;
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
