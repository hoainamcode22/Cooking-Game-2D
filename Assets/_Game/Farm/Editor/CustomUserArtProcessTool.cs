using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CustomUserArtProcessTool
{
    private const string OutDir = "Assets/Assetsgame/PopupArt_Custom";

    static CustomUserArtProcessTool()
    {
        EditorApplication.delayCall += AutoProcess;
    }

    private static void AutoProcess()
    {
        ProcessUserArt();
    }

    [MenuItem("Tools/Farm/Process User Uploaded Art", false, 10)]
    public static void ProcessUserArt()
    {
        if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

        string brainDir = @"C:\Users\acer\.gemini\antigravity\brain\2d1f16d5-9cd0-4d9f-83d8-086396818890";
        string userUploadDir = Path.Combine(brainDir, ".user_uploaded");

        string img1 = Path.Combine(userUploadDir, "media_1789719197558.png");
        string img2 = Path.Combine(userUploadDir, "media_1789719221446.png");
        string gen1 = Path.Combine(brainDir, "icon_delivery_runner_1789719552212.jpg");
        string gen2 = Path.Combine(brainDir, "icon_factory_process_1789719573930.jpg");
        string gen3 = Path.Combine(brainDir, "icon_chili_chicken_1789719595259.jpg");
        string gen4 = Path.Combine(brainDir, "icon_feed_chicken_1789719617150.jpg");
        string gen5 = Path.Combine(brainDir, "icon_pencil_edit_1789719642272.jpg");

        bool any = false;
        if (File.Exists(img1)) { ProcessImage(img1, $"{OutDir}/badge_star_ribbon.png", true); any = true; }
        if (File.Exists(img2)) { ProcessImage(img2, $"{OutDir}/truck_delivery_red.png", false); any = true; }
        if (File.Exists(gen1)) { ProcessImage(gen1, $"{OutDir}/icon_delivery_runner.png", false); any = true; }
        if (File.Exists(gen2)) { ProcessImage(gen2, $"{OutDir}/icon_factory_process.png", false); any = true; }
        if (File.Exists(gen3)) { ProcessImage(gen3, $"{OutDir}/icon_chili_chicken.png", false); any = true; }
        if (File.Exists(gen4)) { ProcessImage(gen4, $"{OutDir}/icon_feed_chicken.png", false); any = true; }
        if (File.Exists(gen5)) { ProcessImage(gen5, $"{OutDir}/icon_pencil_edit.png", false); any = true; }

        if (any)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CustomUserArtProcessTool] Đã xử lý xóa phông và lưu toàn bộ sprite vào " + OutDir);
        }
    }

    private static void ProcessImage(string srcPath, string outPath, bool isCheckerboard)
    {
        byte[] bytes = File.ReadAllBytes(srcPath);
        var srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!srcTex.LoadImage(bytes)) return;

        int w = srcTex.width;
        int h = srcTex.height;
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = srcTex.GetPixels();

        // Flood fill background from boundary
        bool[] isBg = new bool[w * h];
        bool[] visited = new bool[w * h];
        var queue = new System.Collections.Generic.Queue<int>();

        for (int x = 0; x < w; x++)
        {
            queue.Enqueue(0 * w + x);
            queue.Enqueue((h - 1) * w + x);
        }
        for (int y = 0; y < h; y++)
        {
            queue.Enqueue(y * w + 0);
            queue.Enqueue(y * w + (w - 1));
        }

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (visited[idx]) continue;
            visited[idx] = true;

            int px = idx % w;
            int py = idx / w;

            Color c = pixels[idx];
            bool matchesBg = false;

            if (isCheckerboard)
            {
                // Checkerboard colors: gray (~0.75-0.85) or white (~0.95-1.0) with low saturation
                float sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float bright = (c.r + c.g + c.b) / 3f;
                if (sat < 0.08f && bright > 0.65f)
                {
                    matchesBg = true;
                }
            }
            else
            {
                // White background
                float bright = (c.r + c.g + c.b) / 3f;
                float sat = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                if (bright > 0.90f && sat < 0.08f)
                {
                    matchesBg = true;
                }
            }

            if (matchesBg)
            {
                isBg[idx] = true;
                if (px + 1 < w && !visited[py * w + (px + 1)]) queue.Enqueue(py * w + (px + 1));
                if (px - 1 >= 0 && !visited[py * w + (px - 1)]) queue.Enqueue(py * w + (px - 1));
                if (py + 1 < h && !visited[(py + 1) * w + px]) queue.Enqueue((py + 1) * w + px);
                if (py - 1 >= 0 && !visited[(py - 1) * w + px]) queue.Enqueue((py - 1) * w + px);
            }
        }

        for (int i = 0; i < pixels.Length; i++)
        {
            if (isBg[i])
            {
                pixels[i] = new Color(0, 0, 0, 0);
            }
        }

        outTex.SetPixels(pixels);
        outTex.Apply();

        File.WriteAllBytes(outPath, outTex.EncodeToPNG());
        Object.DestroyImmediate(srcTex);
        Object.DestroyImmediate(outTex);

        var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
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
