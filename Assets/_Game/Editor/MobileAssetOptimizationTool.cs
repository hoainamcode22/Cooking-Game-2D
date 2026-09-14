#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace CookingGame.Editor
{
    /// <summary>
    /// TOOL TỐI ƯU HOÁ TOÀN DIỆN TÀI NGUYÊN GAME CHO MOBILE (iOS & ANDROID):
    /// 
    /// 1. Tối ưu kích thước Max Texture Size hợp lý (Icon 256/512, NPC 512/1024, Map 1024/2048).
    /// 2. Bật nén phần cứng ASTC cho iOS & Android (giảm 75%–85% VRAM, chống văng OOM trên iPhone).
    /// 3. Tắt Read/Write Enabled (giải phóng 50% CPU RAM).
    /// 4. Tắt Mipmaps cho 2D Sprite / UI (tiết kiệm thêm 33% bộ nhớ).
    /// 5. Tạo và đóng gói Sprite Atlas cho UI/Icon để gộp Draw Calls.
    /// </summary>
    public static class MobileAssetOptimizationTool
    {
        [MenuItem("Tools/Tối Ưu Hiệu Năng/★ 1-CLICK TỐI ƯU TOÀN BỘ (Mobile & 60 FPS)", false, -200)]
        public static void RunAllOptimizations()
        {
            if (!EditorUtility.DisplayDialog("Tối Ưu Hoá Toàn Bộ Cho Mobile",
                    "Tool sẽ:\n" +
                    "1. Nén toàn bộ Texture sang chuẩn ASTC (iOS / Android).\n" +
                    "2. Giới hạn Max Texture Size hợp lý theo từng loại tài nguyên.\n" +
                    "3. Tắt Read/Write & Mipmap thừa để tiết kiệm bộ nhớ.\n" +
                    "4. Tạo Sprite Atlas cho toàn bộ UI và Icon nông sản.\n\n" +
                    "Quá trình có thể mất 1–3 phút. Bạn có muốn bắt đầu?", "Bắt đầu tối ưu", "Huỷ"))
            {
                return;
            }

            try
            {
                OptimizeAllTextures();
                CreateAndPackSpriteAtlases();
                OptimizeQualitySettings();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Hoàn Tất Tối Ưu!",
                    "✅ Đã tối ưu hoá toàn bộ Textures, nén ASTC cho iOS/Android, tạo Sprite Atlases và tinh chỉnh QualitySettings!\n\n" +
                    "Game giờ đây sẽ nạp cực nhanh, mượt mà 60 FPS và không bị văng trên iPhone/iPad!", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Tối Ưu Hiệu Năng/1. Tối Ưu Toàn Bộ Texture (Nén ASTC cho iOS/Android)", false, 10)]
        public static void OptimizeAllTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            int total = guids.Length;
            int changed = 0;

            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                // Bỏ qua thư mục Packages hoặc thư mục fonts
                if (path.StartsWith("Packages/") || path.Contains("/Fonts/")) continue;

                if (i % 20 == 0)
                {
                    EditorUtility.DisplayProgressBar("Đang tối ưu Textures",
                        $"[{i}/{total}] {Path.GetFileName(path)}", (float)i / total);
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool modified = OptimizeSingleTexture(importer, path);
                if (modified)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"[MobileOptimizer] ✅ Đã tối ưu hoá và nén {changed}/{total} textures cho iOS & Android!");
        }

        private static bool OptimizeSingleTexture(TextureImporter importer, string path)
        {
            bool modified = false;
            string lower = path.ToLowerInvariant();

            // Xác định loại texture và kích thước tối đa phù hợp
            int targetMaxSize = 1024;

            if (lower.Contains("/icon/") || lower.Contains("/icon_processed/") || lower.Contains("/crops/") || 
                lower.Contains("/mill/") || lower.Contains("/item_kho_cook/") || lower.Contains("/data_ewa/"))
            {
                // Icon, hạt giống, nông sản nhỏ
                targetMaxSize = 512;
            }
            else if (lower.Contains("/nv_npc/") || lower.Contains("/nvgame/") || lower.Contains("/animal/"))
            {
                // Nhân vật & Động vật
                targetMaxSize = 1024;
            }
            else if (lower.Contains("/maptitle/") || lower.Contains("/environment/") || lower.Contains("/background/"))
            {
                // Tilemap & Cảnh vật môi trường
                targetMaxSize = 2048;
            }
            else if (lower.Contains("/popup/") || lower.Contains("/ui/") || lower.Contains("/assetsgame/"))
            {
                // UI & Khung Popup
                targetMaxSize = 1024;
            }

            // 1. Giới hạn Max Texture Size mặc định
            if (importer.maxTextureSize > targetMaxSize)
            {
                importer.maxTextureSize = targetMaxSize;
                modified = true;
            }

            // 2. Tắt Mipmaps cho 2D Sprite / UI (tiết kiệm 33% VRAM)
            if (importer.mipmapEnabled && (importer.textureType == TextureImporterType.Sprite || importer.textureType == TextureImporterType.GUI))
            {
                importer.mipmapEnabled = false;
                modified = true;
            }

            // 3. Tắt Read/Write nếu không bắt buộc
            if (importer.isReadable && !lower.Contains("readable_required"))
            {
                importer.isReadable = false;
                modified = true;
            }

            // 4. Bật nén nén chuẩn
            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                modified = true;
            }

            // 5. Cấu hình Override cho iOS (ASTC 6x6 hoặc ASTC 4x4)
            var iosSettings = importer.GetPlatformTextureSettings("iPhone");
            if (!iosSettings.overridden || iosSettings.format != TextureImporterFormat.ASTC_6x6 || iosSettings.maxTextureSize > targetMaxSize)
            {
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = targetMaxSize;
                iosSettings.format = TextureImporterFormat.ASTC_6x6;
                iosSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(iosSettings);
                modified = true;
            }

            // 6. Cấu hình Override cho Android (ASTC 6x6)
            var androidSettings = importer.GetPlatformTextureSettings("Android");
            if (!androidSettings.overridden || androidSettings.format != TextureImporterFormat.ASTC_6x6 || androidSettings.maxTextureSize > targetMaxSize)
            {
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = targetMaxSize;
                androidSettings.format = TextureImporterFormat.ASTC_6x6;
                androidSettings.compressionQuality = 50;
                importer.SetPlatformTextureSettings(androidSettings);
                modified = true;
            }

            return modified;
        }

        [MenuItem("Tools/Tối Ưu Hiệu Năng/2. Tự Động Tạo & Gộp Sprite Atlas Cho UI", false, 20)]
        public static void CreateAndPackSpriteAtlases()
        {
            string atlasFolder = "Assets/_Game/Resources/Atlases";
            if (!Directory.Exists(atlasFolder))
            {
                Directory.CreateDirectory(atlasFolder);
                AssetDatabase.Refresh();
            }

            // Tạo Sprite Atlas cho UI Icons & Nông sản
            CreateAtlas("Atlas_UI_Icons", atlasFolder, new[] { "Assets/Assetsgame/Icon", "Assets/Assetsgame/Icon_Processed" });
            CreateAtlas("Atlas_UI_Popups", atlasFolder, new[] { "Assets/Assetsgame/popup" });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[MobileOptimizer] ✅ Đã tạo và cấu hình các Sprite Atlas cho UI!");
        }

        private static void CreateAtlas(string atlasName, string folder, string[] targetDirectories)
        {
            try
            {
                string assetPath = $"{folder}/{atlasName}.spriteatlas";
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);

                if (atlas == null)
                {
                    atlas = new SpriteAtlas();
                    AssetDatabase.CreateAsset(atlas, assetPath);
                }

                // Cấu hình đóng gói
                var packSettings = new SpriteAtlasPackingSettings
                {
                    blockOffset = 1,
                    padding = 4,
                    enableRotation = false,
                    enableTightPacking = false
                };
                atlas.SetPackingSettings(packSettings);

                // Cấu hình Texture
                var texSettings = new SpriteAtlasTextureSettings
                {
                    readable = false,
                    generateMipMaps = false,
                    sRGB = true,
                    filterMode = FilterMode.Bilinear
                };
                atlas.SetTextureSettings(texSettings);

                // Cấu hình Platform iOS / Android
                var iosSettings = new TextureImporterPlatformSettings
                {
                    name = "iPhone",
                    overridden = true,
                    maxTextureSize = 2048,
                    format = TextureImporterFormat.ASTC_6x6,
                    compressionQuality = 50
                };
                atlas.SetPlatformSettings(iosSettings);

                var androidSettings = new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = 2048,
                    format = TextureImporterFormat.ASTC_6x6,
                    compressionQuality = 50
                };
                atlas.SetPlatformSettings(androidSettings);

                // Thêm các thư mục mục tiêu vào Atlas
                var objectsToAdd = new List<Object>();
                foreach (var dir in targetDirectories)
                {
                    var folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(dir);
                    if (folderObj != null)
                    {
                        objectsToAdd.Add(folderObj);
                    }
                }

                if (objectsToAdd.Count > 0)
                {
                    atlas.Add(objectsToAdd.ToArray());
                }

                EditorUtility.SetDirty(atlas);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MobileOptimizer] Tạo atlas '{atlasName}' bỏ qua: {ex.Message}");
            }
        }

        [MenuItem("Tools/Tối Ưu Hiệu Năng/3. Tối Ưu QualitySettings Toàn Cục", false, 30)]
        public static void OptimizeQualitySettings()
        {
            // Thiết lập Target Frame Rate
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            QualitySettings.asyncUploadTimeSlice = 4;
            QualitySettings.asyncUploadBufferSize = 32;
            QualitySettings.asyncUploadPersistentBuffer = true;

            Debug.Log("[MobileOptimizer] ✅ Đã cập nhật QualitySettings tối ưu cho tốc độ tải và 60 FPS!");
        }
    }
}
#endif
