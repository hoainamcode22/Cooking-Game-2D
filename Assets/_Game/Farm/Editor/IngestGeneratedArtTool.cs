#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool xử lý nền trong suốt (Alpha Cutout) và nạp trực tiếp toàn bộ ảnh vừa vẽ vào Game:
/// - Rau thơm (ing_rau.png)
/// - Nước mắm (ing_nuoc_mam.png)
/// - Nước tương (ing_nuoc_tuong.png)
/// - Muối (ing_muoi.png)
/// - Chuồng gà (icon_chuong_ga.png)
/// - Chuồng bò (icon_chuong_bo.png)
/// </summary>
public static class IngestGeneratedArtTool
{
    private struct ImageJob
    {
        public string sourcePath;
        public string destPath;
    }

    [MenuItem("Tools/Farm Game/Art/★ NẠP ASSETS VỪA VẼ VÀO GAME (TỰ ĐỘNG XÓA NỀN)", false, 1)]
    public static void IngestAll()
    {
        string brainDir = @"C:\Users\acer\.gemini\antigravity\brain\a33db5e9-4cd0-476e-89ad-e7d5e1744879";

        var jobs = new[]
        {
            new ImageJob { sourcePath = Path.Combine(brainDir, "rau_thom_clean_1788792547309.jpg"), destPath = "Assets/Art/UI/Ingredients/ing_rau.png" },
            new ImageJob { sourcePath = Path.Combine(brainDir, "nuoc_mam_clean_1788792572085.jpg"), destPath = "Assets/Art/UI/Ingredients/ing_nuoc_mam.png" },
            new ImageJob { sourcePath = Path.Combine(brainDir, "nuoc_tuong_clean_1788792593961.jpg"), destPath = "Assets/Art/UI/Ingredients/ing_nuoc_tuong.png" },
            new ImageJob { sourcePath = Path.Combine(brainDir, "muoi_icon_1788792334875.jpg"), destPath = "Assets/Art/UI/Ingredients/ing_muoi.png" },
            new ImageJob { sourcePath = Path.Combine(brainDir, "chuong_ga_clean_1788792616973.jpg"), destPath = "Assets/Assetsgame/Buiding/icon_chuong_ga.png" },
            new ImageJob { sourcePath = Path.Combine(brainDir, "chuong_bo_clean_1788792646292.jpg"), destPath = "Assets/Assetsgame/Buiding/icon_chuong_bo.png" },
        };

        int count = 0;
        foreach (var job in jobs)
        {
            if (!File.Exists(job.sourcePath))
            {
                Debug.LogWarning("[IngestArt] Không tìm thấy file nguồn: " + job.sourcePath);
                continue;
            }

            byte[] bytes = File.ReadAllBytes(job.sourcePath);
            Texture2D srcTex = new Texture2D(2, 2);
            if (!srcTex.LoadImage(bytes))
            {
                Debug.LogError("[IngestArt] Không nạp được texture: " + job.sourcePath);
                continue;
            }

            int w = srcTex.width;
            int h = srcTex.height;
            Color[] pixels = srcTex.GetPixels();

            // Flood-fill / chroma-cutout từ viền trắng
            bool[] isBg = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                // Nền trắng sáng (> 0.94)
                if (c.r > 0.93f && c.g > 0.93f && c.b > 0.93f)
                {
                    pixels[i] = Color.clear;
                }
                else if (c.r > 0.88f && c.g > 0.88f && c.b > 0.88f)
                {
                    // Feathering viền mềm mại
                    float alpha = 1f - Mathf.InverseLerp(0.88f, 0.93f, (c.r + c.g + c.b) / 3f);
                    pixels[i] = new Color(c.r, c.g, c.b, alpha);
                }
            }

            Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            outTex.SetPixels(pixels);
            outTex.Apply();

            byte[] pngBytes = outTex.EncodeToPNG();
            string destFull = Path.Combine(Application.dataPath, "..", job.destPath).Replace("\\", "/");
            string dir = Path.GetDirectoryName(destFull);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(destFull, pngBytes);
            count++;
            Debug.Log($"[IngestArt] Đã lưu sprite sạch: {job.destPath}");
        }

        AssetDatabase.Refresh();

        // Cấu hình import settings thành Sprite (2D and UI)
        foreach (var job in jobs)
        {
            if (File.Exists(job.destPath))
            {
                TextureImporter importer = AssetImporter.GetAtPath(job.destPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        EditorUtility.DisplayDialog("Nạp Assets Thành Công!",
            $"Đã tự động cắt nền trong suốt và nạp thành công {count} assets mới (HOÀN TOÀN KHÔNG CÓ CHỮ) vào dự án!\n\n" +
            "1. Rau Thơm -> Assets/Art/UI/Ingredients/ing_rau.png\n" +
            "2. Nước Mắm -> Assets/Art/UI/Ingredients/ing_nuoc_mam.png\n" +
            "3. Nước Tương -> Assets/Art/UI/Ingredients/ing_nuoc_tuong.png\n" +
            "4. Muối -> Assets/Art/UI/Ingredients/ing_muoi.png\n" +
            "5. Chuồng Gà -> Assets/Assetsgame/Buiding/icon_chuong_ga.png\n" +
            "6. Chuồng Bò -> Assets/Assetsgame/Buiding/icon_chuong_bo.png\n\n" +
            "Sếp đã có thể dùng ngay trong game!", "Tuyệt vời!");
    }
}
#endif
