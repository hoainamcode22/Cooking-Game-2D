using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExportTrainUIPackage.EditorTools
{
    /// <summary>
    /// Tool gắn 3 asset world tàu hỏa (sprite-forge bàn giao 2026-08-26).
    /// - Ép importer 3 PNG về Sprite/Single (đội vẽ để nhầm spriteMode=Multiple → load fail).
    /// - Gán arrivedBubbleSprite vào TrainStationBuilding trong scene (persist, chạy được trong build).
    /// - Menu riêng (tuỳ chọn) thay sprite ga world bằng station_building_world.png — có Undo.
    /// </summary>
    public static class TrainWorldAssetSetupTool
    {
        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";
        private static readonly string[] NewAssets =
        {
            SpritesDir + "/world_bubble_train_arrived.png",
            SpritesDir + "/station_building_world.png",
            SpritesDir + "/icon_speedup_wing.png",
        };

        [MenuItem("Tools/Farm Game/Train/Setup Train World Assets")]
        public static void SetupTrainWorldAssets()
        {
            int fixedImport = 0;

            // 1. Chuẩn hoá importer: Sprite (2D and UI) + Single + alpha transparency
            foreach (var path in NewAssets)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"[TrainWorldAssets] Không thấy file: {path}");
                    continue;
                }

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    fixedImport++;
                    Debug.Log($"[TrainWorldAssets] Đã sửa importer → Sprite/Single: {path}");
                }
            }

            // 2. Gán bubble sprite vào TrainStationBuilding (persist vào scene)
            var station = Object.FindFirstObjectByType<TrainStationBuilding>(FindObjectsInactive.Include);
            if (station == null)
            {
                Debug.LogError("[TrainWorldAssets] Không tìm thấy TrainStationBuilding trong scene — mở SCN_Farm rồi chạy lại.");
                return;
            }

            var bubble = AssetDatabase.LoadAssetAtPath<Sprite>(NewAssets[0]);
            if (bubble == null)
            {
                Debug.LogError("[TrainWorldAssets] Load bubble sprite fail — kiểm tra file world_bubble_train_arrived.png.");
                return;
            }

            var so = new SerializedObject(station);
            var prop = so.FindProperty("arrivedBubbleSprite");
            if (prop == null)
            {
                Debug.LogError("[TrainWorldAssets] Không thấy field arrivedBubbleSprite — script TrainStationBuilding chưa compile bản mới?");
                return;
            }

            prop.objectReferenceValue = bubble;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(station);
            EditorSceneManager.MarkSceneDirty(station.gameObject.scene);

            EditorGUIUtility.PingObject(station);
            Debug.Log($"[TrainWorldAssets] XONG ✔ Sửa importer: {fixedImport} file · Đã gán bubble vào '{station.gameObject.name}'. NHỚ SAVE SCENE (Ctrl+S).");
        }

        [MenuItem("Tools/Farm Game/Train/Apply Station World Sprite (tuỳ chọn — thay hình ga ngoài map)")]
        public static void ApplyStationWorldSprite()
        {
            var station = Object.FindFirstObjectByType<TrainStationBuilding>(FindObjectsInactive.Include);
            if (station == null)
            {
                Debug.LogError("[TrainWorldAssets] Không tìm thấy TrainStationBuilding trong scene.");
                return;
            }

            var sr = station.GetComponent<SpriteRenderer>();
            if (sr == null) sr = station.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogError("[TrainWorldAssets] Ga không có SpriteRenderer — gắn tay trong Inspector.");
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/station_building_world.png");
            if (sprite == null)
            {
                Debug.LogError("[TrainWorldAssets] Load station_building_world.png fail — chạy Setup Train World Assets trước.");
                return;
            }

            Undo.RecordObject(sr, "Apply Station World Sprite");
            var oldSprite = sr.sprite;
            sr.sprite = sprite;
            EditorUtility.SetDirty(sr);
            EditorSceneManager.MarkSceneDirty(sr.gameObject.scene);
            Debug.Log($"[TrainWorldAssets] Đã thay sprite ga: '{(oldSprite ? oldSprite.name : "null")}' → '{sprite.name}'. Không ưng thì Ctrl+Z (Undo). NHỚ SAVE SCENE.");
        }

        // ═════════════════════════════════════════════════════════════════════
        // BƯỚC 2 — chạy SAU KHI đội vẽ giao frame vào Sprites/WorldTrain/
        // ═════════════════════════════════════════════════════════════════════

        private const string WorldTrainDir = SpritesDir + "/WorldTrain";

        [MenuItem("Tools/Farm Game/Train/Setup World Train Frames (sau khi đội vẽ giao)")]
        public static void SetupWorldTrainFrames()
        {
            if (!AssetDatabase.IsValidFolder(WorldTrainDir))
            {
                Debug.LogError($"[TrainWorldFrames] Chưa có thư mục {WorldTrainDir} — đội vẽ chưa giao hàng?");
                return;
            }

            // 1. Chuẩn hoá importer mọi PNG trong WorldTrain: Sprite/Single + pivot bottom-center
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { WorldTrainDir });
            int fixedImport = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                var tis = new TextureImporterSettings();
                imp.ReadTextureSettings(tis);
                bool dirty = false;
                if (tis.textureType != TextureImporterType.Sprite) { tis.textureType = TextureImporterType.Sprite; dirty = true; }
                if (tis.spriteMode != (int)SpriteImportMode.Single) { tis.spriteMode = (int)SpriteImportMode.Single; dirty = true; }
                if (tis.spriteAlignment != (int)SpriteAlignment.BottomCenter) { tis.spriteAlignment = (int)SpriteAlignment.BottomCenter; dirty = true; }
                if (tis.mipmapEnabled) { tis.mipmapEnabled = false; dirty = true; }
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; dirty = true; }
                if (dirty)
                {
                    imp.SetTextureSettings(tis);
                    imp.SaveAndReimport();
                    fixedImport++;
                }
            }

            // 2. Load 4 nhóm frame theo prefix, sort theo tên (_01 → _06)
            Sprite[] locoFL = LoadFrames("world_loco_frontleft");
            Sprite[] locoUR = LoadFrames("world_loco_upright");
            Sprite[] wagFL  = LoadFrames("world_wagon_frontleft");
            Sprite[] wagUR  = LoadFrames("world_wagon_upright");

            if (locoFL.Length == 0 && locoUR.Length == 0)
            {
                Debug.LogError("[TrainWorldFrames] Không tìm thấy frame đầu tàu (world_loco_*). Kiểm tra tên file đội vẽ.");
                return;
            }
            if (wagFL.Length == 0 && wagUR.Length == 0)
                Debug.LogWarning("[TrainWorldFrames] Không thấy frame toa (world_wagon_*) — toa giữ ảnh cũ.");

            var smoke = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/train_smoke_puff.png")
                     ?? AssetDatabase.LoadAssetAtPath<Sprite>(SpritesDir + "/steam_smoke_cloud.png");
            if (smoke == null)
                Debug.LogWarning("[TrainWorldFrames] Không load được sprite khói — đầu tàu sẽ không phun khói.");

            // 3. Gắn vào cả 2 tàu: Locomotive/Locomotive2 + Wagon_01..04
            int locoCount = 0, wagonCount = 0, animOff = 0;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var tr in all)
            {
                string n = tr.gameObject.name;
                bool isLoco  = n == "Locomotive" || n == "Locomotive2";
                bool isWagon = n.StartsWith("Wagon_0");
                if (!isLoco && !isWagon) continue;
                if (tr.GetComponent<SpriteRenderer>() == null) continue; // bỏ qua object UI trùng tên

                if (ApplyVisual(tr.gameObject,
                        isLoco ? locoFL : wagFL,
                        isLoco ? locoUR : wagUR,
                        isLoco, isLoco ? smoke : null, ref animOff))
                {
                    if (isLoco) locoCount++; else wagonCount++;
                }
            }

            if (locoCount == 0)
            {
                Debug.LogError("[TrainWorldFrames] Không tìm thấy Locomotive/Locomotive2 có SpriteRenderer trong scene — mở SCN_Farm rồi chạy lại.");
                return;
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[TrainWorldFrames] XONG ✔ importer sửa: {fixedImport} · đầu tàu gắn: {locoCount} · toa gắn: {wagonCount} · Animator cũ đã tắt: {animOff}. NHỚ SAVE SCENE (Ctrl+S) rồi vào Play xem tàu chạy + khói.");
        }

        private static Sprite[] LoadFrames(string prefix)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { WorldTrainDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                // Chỉ nhận đúng định dạng frame <prefix>_NN (bỏ qua file phụ như _single, @2x...)
                if (System.Text.RegularExpressions.Regex.IsMatch(file, "^" + prefix + @"_\d\d$"))
                    list.Add(path);
            }
            list.Sort(System.StringComparer.Ordinal);

            var frames = new System.Collections.Generic.List<Sprite>();
            foreach (var path in list)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) frames.Add(sp);
            }
            return frames.ToArray();
        }

        private static bool ApplyVisual(GameObject go, Sprite[] framesFL, Sprite[] framesUR,
                                        bool isLoco, Sprite smoke, ref int animOff)
        {
            if ((framesFL == null || framesFL.Length == 0) && (framesUR == null || framesUR.Length == 0))
                return false;

            // Tắt Animator frame cũ (sheet khói bake) — nếu không nó sẽ giành sprite mỗi frame
            var animator = go.GetComponent<Animator>();
            if (animator != null && animator.enabled) { animator.enabled = false; animOff++; EditorUtility.SetDirty(animator); }

            var visual = go.GetComponent<TrainWorldVisual>();
            if (visual == null) visual = Undo.AddComponent<TrainWorldVisual>(go);

            // Hướng mặc định theo tàu (feedback Sếp 2026-08-26: tàu thưởng phải QUAY VỀ GA):
            //   TrainVisualRoot  (tàu CŨ/thưởng)  → chạy hầm→ga hướng trái-xuống → initialDir 0
            //   TrainVisualRoot2 (tàu MỚI/giao)   → chạy ga→hầm hướng phải-lên  → initialDir 1
            int initialDir = 1;
            for (var pTr = go.transform; pTr != null; pTr = pTr.parent)
            {
                if (pTr.name == "TrainVisualRoot")  { initialDir = 0; break; }
                if (pTr.name == "TrainVisualRoot2") { initialDir = 1; break; }
            }

            var so = new SerializedObject(visual);
            FillArray(so.FindProperty("framesFrontLeft"), framesFL);
            FillArray(so.FindProperty("framesUpRight"),  framesUR);
            so.FindProperty("emitSmoke").boolValue = isLoco && smoke != null;
            so.FindProperty("smokePuffSprite").objectReferenceValue = smoke;
            var dirProp = so.FindProperty("initialDir");
            if (dirProp != null) dirProp.intValue = initialDir;

            // Sorting CỐ ĐỊNH theo thứ tự đoàn tàu (Sếp chốt): đầu tàu 660, Wagon_01 659 ... Wagon_04 656
            int order = 660;
            if (!isLoco)
            {
                string wagonName = go.name; // "Wagon_0N"
                int idx = 1;
                if (wagonName.Length > 0 && char.IsDigit(wagonName[wagonName.Length - 1]))
                    idx = Mathf.Clamp(wagonName[wagonName.Length - 1] - '0', 1, 9);
                order = 660 - idx;
            }
            var orderProp = so.FindProperty("sortingOrder");
            if (orderProp != null) orderProp.intValue = order;
            so.ApplyModifiedProperties();

            // Đặt frame nghỉ ngay để nhìn thấy art mới đúng hướng trong Scene View
            var sr = go.GetComponent<SpriteRenderer>();
            var first = (initialDir == 0)
                ? ((framesFL != null && framesFL.Length > 0) ? framesFL[0] : framesUR[0])
                : ((framesUR != null && framesUR.Length > 0) ? framesUR[0] : framesFL[0]);
            if (sr != null && first != null)
            {
                Undo.RecordObject(sr, "Apply World Train Frame");
                sr.sprite = first;
                sr.flipX = false;
                sr.sortingOrder = order; // preview thứ tự đè ngay trong Scene view
                EditorUtility.SetDirty(sr);
            }

            // FIT SCALE (feedback Sếp 2026-08-26: "tàu quá to, dồn 1 cục"):
            // co art mới 1024px về ĐÚNG bề rộng art cũ (taulua/toatau) — giữ nguyên spacing,
            // waypoint, collider của scene. Idempotent: scale gốc lưu vào fitBaseScale, chạy lại không co dồn.
            FitScaleToLegacyArt(go, visual, first, isLoco);

            EditorUtility.SetDirty(visual);
            return true;
        }

        private static void FitScaleToLegacyArt(GameObject go, TrainWorldVisual visual, Sprite newFrame, bool isLoco)
        {
            if (go == null || visual == null || newFrame == null) return;

            Sprite oldRef = LoadFirstSprite(isLoco ? "Assets/Taulua/taulua.png" : "Assets/Taulua/toatau.png");
            if (oldRef == null)
            {
                Debug.LogWarning($"[TrainWorldFrames] Không load được art cũ để fit scale cho '{go.name}' — chỉnh tay localScale nếu tàu quá to.");
                return;
            }

            var so = new SerializedObject(visual);
            var baseProp = so.FindProperty("fitBaseScale");
            Vector3 baseScale = baseProp != null ? baseProp.vector3Value : Vector3.zero;
            if (baseScale == Vector3.zero)
            {
                baseScale = go.transform.localScale; // lần đầu: scale hiện tại chính là scale thời art cũ
                if (baseProp != null) { baseProp.vector3Value = baseScale; so.ApplyModifiedProperties(); }
            }

            float oldW = oldRef.bounds.size.x;   // world unit @scale 1 (PPU đã tính)
            float newW = newFrame.bounds.size.x;
            if (oldW <= 0.001f || newW <= 0.001f) return;

            float k = oldW / newW;
            Undo.RecordObject(go.transform, "Fit World Train Scale");
            go.transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, 1f);
            EditorUtility.SetDirty(go.transform);
            Debug.Log($"[TrainWorldFrames] Fit '{go.name}': art cũ {oldW:F2}u / mới {newW:F2}u → scale ×{k:F3}");
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null) return direct;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is Sprite sp) return sp;
            return null;
        }

        private static void FillArray(SerializedProperty prop, Sprite[] frames)
        {
            if (prop == null) return;
            int n = frames != null ? frames.Length : 0;
            prop.arraySize = n;
            for (int i = 0; i < n; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

    }
}
