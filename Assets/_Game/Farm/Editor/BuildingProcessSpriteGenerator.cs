#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildingProcessSpriteGenerator
{
    public const string TargetFolder = "Assets/Assetsgame/popup/ui_building_svg/generated_sprites";

    [MenuItem("Tools/Farm/Process UI/1. Tạo Sprites Process UI Mới (ui_building_svg)")]
    public static void GenerateAllSprites()
    {
        if (!Directory.Exists(TargetFolder))
        {
            Directory.CreateDirectory(TargetFolder);
        }

        // 1. Frame Base (500x120 -> 250x60, 9-slice: 20, 20, 20, 20)
        CreateTexture("proc_frame_bg.png", 250, 60, new Vector4(20, 20, 20, 20), (tex) =>
        {
            // Drop Shadow
            FillRoundedRect(tex, 2, 0, 246, 56, 18, new Color(0f, 0f, 0f, 0.22f));
            // Wood Outer Border (#C99863)
            FillRoundedRect(tex, 2, 3, 246, 56, 18, new Color(0.79f, 0.60f, 0.39f, 1f));
            // Cream Inner Fill (#FEF9E6)
            FillRoundedRect(tex, 4, 5, 242, 52, 16, new Color(0.996f, 0.976f, 0.902f, 1f));
        });

        // 2. Track Background (200x30, 9-slice: 15, 15, 15, 15)
        CreateTexture("proc_track_bg.png", 200, 30, new Vector4(15, 15, 15, 15), (tex) =>
        {
            // Outer stroke / shadow (#D3A77C)
            FillRoundedRect(tex, 0, 0, 200, 30, 15, new Color(0.83f, 0.65f, 0.49f, 1f));
            // Inner track body (#B38152)
            FillRoundedRect(tex, 0, 3, 200, 27, 13, new Color(0.70f, 0.51f, 0.32f, 1f));
        });

        // 3. Green Fill Bar (200x26, unbordered for clean horizontal filled bar)
        CreateTexture("proc_fill_green.png", 200, 26, Vector4.zero, (tex) =>
        {
            // Dark Green Base (#4FAD19)
            FillRoundedRect(tex, 0, 0, 200, 26, 13, new Color(0.31f, 0.68f, 0.10f, 1f));
            // Bright Green Body (#7BDE2A)
            FillVerticalGradientRoundedRect(tex, 1, 2, 198, 23, 11, new Color(0.35f, 0.75f, 0.12f, 1f), new Color(0.48f, 0.87f, 0.16f, 1f));
            // Top Shine Highlight (#A7FA62)
            FillTopRoundedRect(tex, 8, 15, 184, 8, 4, new Color(0.65f, 0.98f, 0.38f, 0.85f));
        });

        // 4. Blue Diamond Button Base (100x70, 9-slice: 20, 20, 20, 20)
        CreateTexture("proc_btn_blue.png", 100, 70, new Vector4(20, 20, 20, 20), (tex) =>
        {
            // Drop Shadow
            FillRoundedRect(tex, 2, 0, 96, 66, 20, new Color(0f, 0f, 0f, 0.28f));
            // Dark Blue Bottom Bevel (#1C5C87)
            FillRoundedRect(tex, 2, 3, 96, 66, 20, new Color(0.11f, 0.36f, 0.53f, 1f));
            // Ocean Blue Body (#2980B9)
            FillVerticalGradientRoundedRect(tex, 2, 7, 96, 62, 20, new Color(0.16f, 0.50f, 0.73f, 1f), new Color(0.20f, 0.58f, 0.82f, 1f));
            // Top Highlight Shine (#5DADE2)
            FillTopRoundedRect(tex, 8, 48, 84, 18, 10, new Color(0.36f, 0.68f, 0.89f, 0.80f));
        });

        AssetDatabase.Refresh();
        Debug.Log("[ProcessUI] Tạo thành công 4 sprites 9-slice theo đúng thiết kế ui_building_svg!");
    }

    private static void CreateTexture(string fileName, int width, int height, Vector4 border, System.Action<Texture2D> drawAction)
    {
        string fullPath = Path.Combine(TargetFolder, fileName);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Clear transparent
        Color[] clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        // Draw
        drawAction(tex);
        tex.Apply();

        // Save PNG
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);

        // Configure TextureImporter
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static void FillRoundedRect(Texture2D tex, int rx, int ry, int rw, int rh, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = ry; y < ry + rh; y++)
        {
            for (int x = rx; x < rx + rw; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, rw, rh, radius, r2))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void FillVerticalGradientRoundedRect(Texture2D tex, int rx, int ry, int rw, int rh, int radius, Color bottomColor, Color topColor)
    {
        int r2 = radius * radius;
        for (int y = ry; y < ry + rh; y++)
        {
            float t = (float)(y - ry) / Mathf.Max(1, rh - 1);
            Color col = Color.Lerp(bottomColor, topColor, t);
            for (int x = rx; x < rx + rw; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, rw, rh, radius, r2))
                    BlendPixel(tex, x, y, col);
            }
        }
    }

    private static void FillTopRoundedRect(Texture2D tex, int rx, int ry, int rw, int rh, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = ry; y < ry + rh; y++)
        {
            for (int x = rx; x < rx + rw; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, rw, rh, radius, r2))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static bool IsInsideRoundedRect(int x, int y, int rx, int ry, int rw, int rh, int radius, int r2)
    {
        if (x < rx || x >= rx + rw || y < ry || y >= ry + rh) return false;
        int left = rx + radius;
        int right = rx + rw - radius - 1;
        int bottom = ry + radius;
        int top = ry + rh - radius - 1;

        if (x < left && y < bottom) return (x - left) * (x - left) + (y - bottom) * (y - bottom) <= r2;
        if (x > right && y < bottom) return (x - right) * (x - right) + (y - bottom) * (y - bottom) <= r2;
        if (x < left && y > top) return (x - left) * (x - left) + (y - top) * (y - top) <= r2;
        if (x > right && y > top) return (x - right) * (x - right) + (y - top) * (y - top) <= r2;

        return true;
    }

    private static void BlendPixel(Texture2D tex, int x, int y, Color src)
    {
        if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) return;
        Color dst = tex.GetPixel(x, y);
        float outA = src.a + dst.a * (1f - src.a);
        if (outA <= 0f) return;
        Color outCol = (src * src.a + dst * dst.a * (1f - src.a)) / outA;
        outCol.a = outA;
        tex.SetPixel(x, y, outCol);
    }
}
#endif
