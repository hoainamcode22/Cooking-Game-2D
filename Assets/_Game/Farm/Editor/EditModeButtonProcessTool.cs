using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditModeButtonProcessTool
{
    private const string FolderPath = "Assets/Assetsgame/btn_EditMode";

    [MenuItem("Tools/Farm Game/Process Edit Mode Buttons", false, 30)]
    public static void ProcessButtons()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            AssetDatabase.Refresh();
        }

        string[] jpgFiles = Directory.GetFiles(FolderPath, "*.jpg");
        if (jpgFiles.Length == 0)
        {
            Debug.LogWarning("[EditModeButton] Không tìm thấy file .jpg nào trong " + FolderPath);
            return;
        }

        int count = 0;
        for (int i = 0; i < jpgFiles.Length; i++)
        {
            string srcPath = jpgFiles[i].Replace('\\', '/');
            byte[] bytes = File.ReadAllBytes(srcPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) continue;

            int w = tex.width;
            int h = tex.height;
            var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = tex.GetPixels();

            float cx = w * 0.5f;
            float cy = h * 0.5f;

            // Tìm bán kính của nút tròn
            float maxRadius = Mathf.Min(cx, cy) * 0.90f; // Bán kính vòng ngoài của nút

            // Đo vùng ngoài để lấy màu viền trắng xóa phông
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    float dx = (x + 0.5f) - cx;
                    float dy = (y + 0.5f) - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    Color c = pixels[idx];

                    // 1. Nhận diện phông Magenta (#FF00FF) chuẩn sprite-forge
                    bool isMagenta = (c.r > 0.75f && c.b > 0.75f && c.g < 0.35f);
                    if (isMagenta)
                    {
                        c.a = 0f;
                    }
                    else if (c.r > 0.65f && c.b > 0.65f && c.g < 0.45f)
                    {
                        // Anti-alias viền tím magenta
                        float magScore = (c.r + c.b) * 0.5f - c.g;
                        if (magScore > 0.3f) c.a = Mathf.Clamp01(1f - (magScore - 0.3f) / 0.4f);
                    }

                    // 2. Xóa phông ngoài bán kính nút
                    if (dist > maxRadius + 2f)
                    {
                        c = new Color(0, 0, 0, 0);
                    }
                    else if (dist > maxRadius - 2f)
                    {
                        float alpha = Mathf.Clamp01((maxRadius + 2f - dist) / 4f);
                        c.a = Mathf.Min(c.a, alpha);
                    }

                    // 3. Xóa phông trắng ở ngoài rìa
                    if (dist > maxRadius * 0.82f)
                    {
                        float whiteness = (c.r + c.g + c.b) / 3f;
                        if (whiteness > 0.92f && Mathf.Abs(c.r - c.g) < 0.05f && Mathf.Abs(c.g - c.b) < 0.05f)
                        {
                            c.a = 0f;
                        }
                    }

                    pixels[idx] = c;
                }
            }

            outTex.SetPixels(pixels);
            outTex.Apply();

            string fileName = Path.GetFileNameWithoutExtension(srcPath);
            string outPath = $"{FolderPath}/{fileName}_transparent.png";
            File.WriteAllBytes(outPath, outTex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(outTex);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Cập nhật TextureImporter
        string[] pngFiles = Directory.GetFiles(FolderPath, "*_transparent.png");
        for (int i = 0; i < pngFiles.Length; i++)
        {
            string p = pngFiles[i].Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(p) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"[EditModeButton] Đã xử lý và tạo {count} sprite nút Edit Mode trong suốt tại {FolderPath}");
    }
}
