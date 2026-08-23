#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HouseAssetExtractorTool
{
    private const string SourceDir = @"E:\Game2\Cooking-Game-2D\Assets\Assetsgame\Nhà\Village_Home";
    private const string TargetDir = @"Assets/Assetsgame/Nhà/House_Sprites";

    private struct HouseInfo
    {
        public string Name;
        public string File;
        public HouseInfo(string name, string file) { Name = name; File = file; }
    }

    private static readonly HouseInfo[] Houses = new HouseInfo[]
    {
        new HouseInfo("House_01", "ChatGPT Image 00_37_45 22 thg 8, 2026.png"),
        new HouseInfo("House_02", "ChatGPT Image 00_37_48 22 thg 8, 2026.png"),
        new HouseInfo("House_03", "ChatGPT Image 00_37_55 22 thg 8, 2026.png"),
        new HouseInfo("House_04", "ChatGPT Image 00_38_20 22 thg 8, 2026.png"),
        new HouseInfo("House_05", "ChatGPT Image 00_38_27 22 thg 8, 2026.png"),
    };

    [MenuItem("Tools/Farm/Boc Tach 5 Nha 6 Stages", false, 10)]
    public static void ExtractAllHouses()
    {
        Debug.Log("[HouseAssetExtractorTool] Bắt đầu bóc tách 5 ngôi nhà x 6 giai đoạn...");

        foreach (var h in Houses)
        {
            string srcPath = Path.Combine(SourceDir, h.File);
            if (!File.Exists(srcPath))
            {
                Debug.LogError("[HouseAssetExtractorTool] Không tìm thấy file: " + srcPath);
                continue;
            }

            ProcessHouse(h.Name, srcPath);
        }

        AssetDatabase.Refresh();

        // Configure Sprite Importers
        ConfigureSpriteImporters();

        Debug.Log("[HouseAssetExtractorTool] HOÀN TẤT bóc tách toàn bộ 30 sprite nhà mới!");
    }

    private static void ProcessHouse(string houseName, string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D sheet = new Texture2D(1536, 1024, TextureFormat.RGBA32, false);
        sheet.LoadImage(bytes);

        Texture2D cleanSheet = ChromaKey(sheet);

        // 6 stages in 3 cols x 2 rows
        // Stage 1: (0,0), Stage 2: (1,0), Stage 3: (2,0)
        // Stage 4: (0,1), Stage 5: (1,1), Stage 6: (2,1)
        // Note: in Texture2D, y=0 is BOTTOM, so row 0 (top in visual) is y=512, row 1 (bottom in visual) is y=0.
        Vector2Int[] stageGrid = new Vector2Int[]
        {
            new Vector2Int(0, 512), // Stage 1: col 0, top row
            new Vector2Int(512, 512), // Stage 2: col 1, top row
            new Vector2Int(1024, 512), // Stage 3: col 2, top row
            new Vector2Int(0, 0), // Stage 4: col 0, bottom row
            new Vector2Int(512, 0), // Stage 5: col 1, bottom row
            new Vector2Int(1024, 0) // Stage 6: col 2, bottom row
        };

        // 1. Khử sạch text 'Stage X' ở đáy ô (y trong Texture2D: y=0 là đáy mỗi ô)
        // Trong Texture2D: y từ 0 đến 88 là khu vực text 'Stage X' ở đáy mỗi ô visual
        for (int s = 0; s < 6; s++)
        {
            int startX = stageGrid[s].x;
            int startY = stageGrid[s].y;

            // Xóa vùng text ở đáy ô (y từ 0 đến 88)
            for (int y = 0; y < 88; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    cleanSheet.SetPixel(startX + x, startY + y, Color.clear);
                }
            }

            // Với Stage 5 (s=4, col 1, row bottom), xóa cạnh phải (x >= 435) để khử confetti của Stage 6
            if (s == 4)
            {
                for (int y = 0; y < 512; y++)
                {
                    for (int x = 435; x < 512; x++)
                    {
                        cleanSheet.SetPixel(startX + x, startY + y, Color.clear);
                    }
                }
            }

            // Xóa mép biên
            for (int y = 0; y < 512; y++)
            {
                for (int x = 0; x < 10; x++) cleanSheet.SetPixel(startX + x, startY + y, Color.clear);
                for (int x = 502; x < 512; x++) cleanSheet.SetPixel(startX + x, startY + y, Color.clear);
            }
        }
        cleanSheet.Apply();

        // 2. Tìm Shared Bounding Box
        int globalMinX = 512, globalMaxX = 0, globalMinY = 512, globalMaxY = 0;

        for (int s = 0; s < 6; s++)
        {
            int startX = stageGrid[s].x;
            int startY = stageGrid[s].y;

            Color[] cellPixels = cleanSheet.GetPixels(startX, startY, 512, 512);

            for (int y = 88; y < 512; y++)
            {
                for (int x = 0; x < 512; x++)
                {
                    if (cellPixels[y * 512 + x].a > 0.1f)
                    {
                        if (x < globalMinX) globalMinX = x;
                        if (x > globalMaxX) globalMaxX = x;
                        if (y < globalMinY) globalMinY = y;
                        if (y > globalMaxY) globalMaxY = y;
                    }
                }
            }
        }

        int halfW = Mathf.Max(Mathf.Abs(256 - globalMinX), Mathf.Abs(globalMaxX - 256)) + 4;
        int cropMinX = Mathf.Clamp(256 - halfW, 0, 511);
        int cropMaxX = Mathf.Clamp(256 + halfW, 0, 511);
        int cropMinY = Mathf.Clamp(globalMinY - 2, 88, 511);
        int cropMaxY = Mathf.Clamp(globalMaxY + 2, 88, 511);

        int cropW = cropMaxX - cropMinX + 1;
        int cropH = cropMaxY - cropMinY + 1;

        string outDir = Path.Combine(TargetDir, houseName);
        Directory.CreateDirectory(outDir);

        for (int s = 0; s < 6; s++)
        {
            int stageNum = s + 1;
            int startX = stageGrid[s].x + cropMinX;
            int startY = stageGrid[s].y + cropMinY;

            Color[] cropped = cleanSheet.GetPixels(startX, startY, cropW, cropH);
            Texture2D stageTex = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
            stageTex.SetPixels(cropped);
            stageTex.Apply();

            byte[] png = stageTex.EncodeToPNG();
            string outPath = Path.Combine(outDir, $"stage_{stageNum}.png");
            File.WriteAllBytes(outPath, png);
        }

        Debug.Log($"[HouseAssetExtractorTool] Đã xuất 6 stage sạch cho {houseName} ({cropW}x{cropH}px)");
    }

    private static Texture2D ChromaKey(Texture2D src)
    {
        int w = src.width;
        int h = src.height;
        Texture2D dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] srcPix = src.GetPixels();
        Color[] dstPix = new Color[srcPix.Length];

        for (int i = 0; i < srcPix.Length; i++)
        {
            Color c = srcPix[i];
            bool isMagenta = (c.r > 0.62f && c.b > 0.62f && c.g < 0.38f) ||
                             ((c.r - c.g) > 0.32f && (c.b - c.g) > 0.32f);

            if (isMagenta)
            {
                dstPix[i] = Color.clear;
            }
            else
            {
                // De-spill
                if (c.g < 0.45f && c.r > c.g && c.b > c.g)
                {
                    float spill = Mathf.Min(c.r, c.b) - c.g;
                    if (spill > 0.15f)
                    {
                        float alpha = Mathf.Clamp01(1f - (spill - 0.15f) / 0.35f);
                        c.a *= alpha;
                    }
                }
                dstPix[i] = c;
            }
        }

        dst.SetPixels(dstPix);
        dst.Apply();
        return dst;
    }

    private static void ConfigureSpriteImporters()
    {
        foreach (var h in Houses)
        {
            string houseDir = Path.Combine(TargetDir, h.Name);
            for (int s = 1; s <= 6; s++)
            {
                string path = Path.Combine(houseDir, $"stage_{s}.png").Replace('\\', '/');
                TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = 100;
                    imp.spritePivot = new Vector2(0.5f, 0f); // Bottom Center
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;

                    TextureImporterSettings settings = new TextureImporterSettings();
                    imp.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    settings.spritePivot = new Vector2(0.5f, 0f);
                    imp.SetTextureSettings(settings);

                    imp.SaveAndReimport();
                }
            }
        }
    }
}
#endif
