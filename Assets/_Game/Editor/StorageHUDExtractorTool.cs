using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StorageHUDExtractorTool
{
    private const string TargetFolder = "Assets/_Game/Farm/Sprites/StorageHUD";

    [MenuItem("Tools/Farm Game/Process Storage HUD Assets")]
    public static void ProcessAssets()
    {
        if (!Directory.Exists(TargetFolder))
        {
            Directory.CreateDirectory(TargetFolder);
            AssetDatabase.Refresh();
        }

        string brainDir = @"C:\Users\acer\.gemini\antigravity\brain\ede6543d-b622-470b-805d-2107b326af94";
        string frameGenPath  = Path.Combine(brainDir, "cozy_storage_card_frame_1790007240536.jpg");
        string barnGenPath   = Path.Combine(brainDir, "storage_barn_house_1790001179340.jpg");
        string headerGenPath = Path.Combine(brainDir, "cozy_storage_header_badge_1790007267145.jpg");
        string crateGenPath  = Path.Combine(brainDir, "cozy_storage_crate_icon_1790007289513.jpg");

        // 1. Process Main Wooden Card Frame (Flood-fill from corners so inner cream parchment is 100% preserved)
        if (File.Exists(frameGenPath))
        {
            Texture2D frameTex = LoadReadable(frameGenPath);
            Texture2D cleanFrame = FloodFillRemoveOuterBackground(frameTex, 0.91f, 0.08f);
            Texture2D croppedFrame = AutoCrop(cleanFrame, 10);
            SavePNG(croppedFrame, $"{TargetFolder}/storage_main_frame.png");
        }

        // 2. Process Barn House (3D Red Barn)
        if (File.Exists(barnGenPath))
        {
            Texture2D barnTex = LoadReadable(barnGenPath);
            Texture2D cleanBarn = FloodFillRemoveOuterBackground(barnTex, 0.90f, 0.08f);
            Texture2D croppedBarn = AutoCrop(cleanBarn, 8);
            SavePNG(croppedBarn, $"{TargetFolder}/storage_barn_house.png");
        }

        // 3. Process Header Badge (Wooden Header Badge with empty plank)
        if (File.Exists(headerGenPath))
        {
            Texture2D headerTex = LoadReadable(headerGenPath);
            Texture2D cleanHeader = FloodFillRemoveOuterBackground(headerTex, 0.90f, 0.08f);
            Texture2D croppedHeader = AutoCrop(cleanHeader, 6);
            SavePNG(croppedHeader, $"{TargetFolder}/storage_header_badge.png");
        }

        // 4. Process Crate Icon Badge (Circular Green Badge with 3D Crate)
        if (File.Exists(crateGenPath))
        {
            Texture2D crateTex = LoadReadable(crateGenPath);
            Texture2D cleanCrate = FloodFillRemoveOuterBackground(crateTex, 0.90f, 0.08f);
            Texture2D croppedCrate = AutoCrop(cleanCrate, 6);
            SavePNG(croppedCrate, $"{TargetFolder}/storage_crate_icon.png");
        }

        AssetDatabase.Refresh();
        ConfigureAllSpritesInFolder(TargetFolder);
        Debug.Log("<color=green>[StorageHUD] Successfully processed all Storage HUD sprites with FloodFill Alpha Masking!</color>");
    }

    private static Texture2D LoadReadable(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }

    /// <summary>
    /// Flood-fills from the 4 outer corners so that ONLY the exterior white background is made transparent.
    /// The interior cream parchment and solid details inside the frame are completely protected and preserved.
    /// </summary>
    private static Texture2D FloodFillRemoveOuterBackground(Texture2D src, float whiteThreshold, float feather)
    {
        int w = src.width;
        int h = src.height;
        Color[] srcPixels = src.GetPixels();
        Color[] dstPixels = new Color[srcPixels.Length];
        Array.Copy(srcPixels, dstPixels, srcPixels.Length);

        bool[] visited = new bool[w * h];
        Queue<int> queue = new Queue<int>();

        // Seed 4 corners and borders
        void TrySeed(int x, int y)
        {
            int idx = y * w + x;
            if (!visited[idx] && IsWhiteBackground(srcPixels[idx], whiteThreshold))
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        for (int x = 0; x < w; x++)
        {
            TrySeed(x, 0);
            TrySeed(x, h - 1);
        }
        for (int y = 0; y < h; y++)
        {
            TrySeed(0, y);
            TrySeed(w - 1, y);
        }

        // BFS Flood-fill
        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            int cx = curr % w;
            int cy = curr / w;

            // Make pixel transparent with smooth feather
            Color c = srcPixels[curr];
            float brightness = (c.r + c.g + c.b) / 3f;
            float alpha = Mathf.InverseLerp(whiteThreshold + feather, whiteThreshold, brightness);
            dstPixels[curr] = new Color(c.r, c.g, c.b, alpha <= 0.02f ? 0f : alpha);

            // Neighbors
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];

                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                {
                    int nIdx = ny * w + nx;
                    if (!visited[nIdx])
                    {
                        if (IsWhiteBackground(srcPixels[nIdx], whiteThreshold))
                        {
                            visited[nIdx] = true;
                            queue.Enqueue(nIdx);
                        }
                    }
                }
            }
        }

        Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.SetPixels(dstPixels);
        dst.Apply();
        return dst;
    }

    private static bool IsWhiteBackground(Color c, float threshold)
    {
        float brightness = (c.r + c.g + c.b) / 3f;
        float maxDiff = Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Max(Mathf.Abs(c.r - c.b), Mathf.Abs(c.g - c.b)));
        return brightness >= threshold && maxDiff < 0.09f;
    }

    private static Texture2D AutoCrop(Texture2D src, int padding = 6)
    {
        int w = src.width;
        int h = src.height;
        Color[] pixels = src.GetPixels();

        int minX = w, maxX = 0, minY = h, maxY = 0;
        bool found = false;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = pixels[y * w + x];
                if (c.a > 0.05f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                    found = true;
                }
            }
        }

        if (!found) return src;

        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(w - 1, maxX + padding);
        maxY = Mathf.Min(h - 1, maxY + padding);

        int cw = maxX - minX + 1;
        int ch = maxY - minY + 1;

        Texture2D cropped = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        Color[] cropPixels = src.GetPixels(minX, minY, cw, ch);
        cropped.SetPixels(cropPixels);
        cropped.Apply();
        return cropped;
    }

    private static void SavePNG(Texture2D tex, string path)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Debug.Log($"[StorageHUD] Saved {path} ({tex.width}x{tex.height})");
    }

    private static void ConfigureAllSpritesInFolder(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
    }
}
