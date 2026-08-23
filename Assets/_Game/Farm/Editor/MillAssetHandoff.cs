#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Farm.EditorTools.Mill;

public static class MillAssetHandoff
{
    private const string ArtifactFolder = @"C:\Users\acer\.gemini\antigravity\brain\a33db5e9-4cd0-476e-89ad-e7d5e1744879";
    private const string MachineJpg = "mill_machine_body_1787470517044.jpg";
    private const string GearsJpg = "mill_mechanical_gears_1787470536233.jpg";

    [MenuItem("Tools/Farm/Popup May Xay/Process AI Art & Rebuild", false, -2)]
    public static void ProcessAndRebuild()
    {
        Debug.Log("[MillAssetHandoff] Bắt đầu bóc tách asset AI và nạp vào Unity UI...");

        string machinePath = Path.Combine(ArtifactFolder, MachineJpg);
        string gearsPath = Path.Combine(ArtifactFolder, GearsJpg);

        if (!File.Exists(machinePath) || !File.Exists(gearsPath))
        {
            Debug.LogError("[MillAssetHandoff] Không tìm thấy file ảnh gốc trong Artifact folder!");
            return;
        }

        // 1. Process Machine Body
        Texture2D rawMachine = LoadTexture(machinePath);
        Texture2D cleanMachine = ChromaKey(rawMachine);
        SaveSprite(cleanMachine, "machine_body.png");

        // 2. Process Gears
        Texture2D rawGears = LoadTexture(gearsPath);
        Texture2D cleanGears = ChromaKey(rawGears);

        // Crop Large Gear (Top-Left 70% of image)
        // Center around (380, 580) with radius 360 in 1024x1024 image
        Texture2D largeGear = CropAndCenterGear(cleanGears, 30, 260, 700, 700);
        SaveSprite(largeGear, "gear_large.png");

        // Crop Small Gear (Bottom-Right 40% of image)
        Texture2D smallGear = CropAndCenterGear(cleanGears, 610, 30, 380, 380);
        SaveSprite(smallGear, "gear_small.png");

        AssetDatabase.Refresh();

        // 3. Rebuild Popup UI
        MillPopupBuilderTool.LamTatCa();

        Debug.Log("[MillAssetHandoff] Hoàn tất bóc tách asset và Rebuild toàn bộ giao diện Máy Xay!");
    }

    private static Texture2D LoadTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }

    private static Texture2D ChromaKey(Texture2D src)
    {
        int w = src.width;
        int h = src.height;
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] srcPixels = src.GetPixels();
        Color[] dstPixels = new Color[srcPixels.Length];

        for (int i = 0; i < srcPixels.Length; i++)
        {
            Color c = srcPixels[i];
            // Magenta detection: High R, Low G, High B
            float magentaDist = Mathf.Sqrt(Mathf.Pow(c.r - 1.0f, 2) + Mathf.Pow(c.g - 0.0f, 2) + Mathf.Pow(c.b - 1.0f, 2));
            bool isMagenta = (c.r > 0.65f && c.g < 0.35f && c.b > 0.65f) || (magentaDist < 0.55f);

            if (isMagenta)
            {
                dstPixels[i] = Color.clear;
            }
            else
            {
                // Soft edge de-spill
                if (c.g < 0.45f && c.r > c.g && c.b > c.g)
                {
                    float spill = Mathf.Min(c.r, c.b) - c.g;
                    if (spill > 0.2f)
                    {
                        float alpha = Mathf.Clamp01(1f - (spill - 0.2f) / 0.3f);
                        c.a *= alpha;
                    }
                }
                dstPixels[i] = c;
            }
        }

        dst.SetPixels(dstPixels);
        dst.Apply();
        return dst;
    }

    private static Texture2D CropAndCenterGear(Texture2D src, int startX, int startY, int cropW, int cropH)
    {
        // Extract bounding area
        Color[] block = src.GetPixels(startX, startY, cropW, cropH);
        
        // Find visible bounds
        int minX = cropW, maxX = 0, minY = cropH, maxY = 0;
        for (int y = 0; y < cropH; y++)
        {
            for (int x = 0; x < cropW; x++)
            {
                if (block[y * cropW + x].a > 0.1f)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (minX > maxX) { minX = 0; maxX = cropW - 1; minY = 0; maxY = cropH - 1; }

        int boundW = maxX - minX + 1;
        int boundH = maxY - minY + 1;
        int targetSize = Mathf.Max(boundW, boundH) + 16;

        Texture2D output = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
        Color[] outPixels = new Color[targetSize * targetSize];
        for (int i = 0; i < outPixels.Length; i++) outPixels[i] = Color.clear;

        int offsetX = (targetSize - boundW) / 2;
        int offsetY = (targetSize - boundH) / 2;

        for (int y = 0; y < boundH; y++)
        {
            for (int x = 0; x < boundW; x++)
            {
                Color c = block[(minY + y) * cropW + (minX + x)];
                outPixels[(offsetY + y) * targetSize + (offsetX + x)] = c;
            }
        }

        output.SetPixels(outPixels);
        output.Apply();
        return output;
    }

    private static void SaveSprite(Texture2D tex, string filename)
    {
        byte[] png = tex.EncodeToPNG();
        string[] dirs = new string[]
        {
            "Assets/Assetsgame/popup/ui_mill_assets/generated_sprites",
            "Assets/_Game/GeneratedUI/Mill"
        };

        foreach (string d in dirs)
        {
            Directory.CreateDirectory(d);
            string fullPath = Path.Combine(d, filename);
            File.WriteAllBytes(fullPath, png);
            Debug.Log("[MillAssetHandoff] Đã ghi sprite: " + fullPath);
        }
    }
}
#endif
