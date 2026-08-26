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
    }
}
