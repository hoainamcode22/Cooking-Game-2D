#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TownshipHUDSpriteGenerator
{
    public const string SourceFolder = "Assets/Assetsgame/popup/ui_township_exact_bases";
    public const string TargetFolder = "Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites";

    [MenuItem("Tools/Farm/HUD/1. Tạo Sprites HUD 9-Slice Sắc Nét")]
    public static void GenerateAllSprites()
    {
        if (!Directory.Exists(TargetFolder))
        {
            Directory.CreateDirectory(TargetFolder);
        }

        // 1. Avatar Base (120x120, 9-slice: 24, 24, 24, 24)
        CreateTexture("hud_avatar_base.png", 120, 120, new Vector4(24, 24, 24, 24), (tex) =>
        {
            // Drop Shadow
            FillRoundedRect(tex, 4, 0, 112, 112, 20, new Color(0f, 0f, 0f, 0.35f));
            // Outer Brown Frame
            FillRoundedRect(tex, 4, 6, 112, 112, 20, new Color(0.37f, 0.24f, 0.14f, 1f)); // #5E3C23
            FillRoundedRect(tex, 4, 12, 112, 106, 20, new Color(0.46f, 0.31f, 0.20f, 1f)); // #754F34
            // Inner Cutout / Inset Stroke
            DrawRoundedStroke(tex, 10, 18, 100, 96, 15, 3, new Color(0.29f, 0.18f, 0.10f, 1f)); // #4A2E1A
        });

        // 2. Currency Base / EXP Track Pill (200x44, 9-slice: 22, 22, 22, 22)
        CreateTexture("hud_currency_base.png", 200, 44, new Vector4(22, 22, 22, 22), (tex) =>
        {
            // Dark Translucent Pill rgba(36, 20, 13, 0.78)
            FillRoundedRect(tex, 0, 0, 200, 44, 22, new Color(0.14f, 0.08f, 0.05f, 0.78f));
            // Subtle top highlight
            FillTopRoundedRect(tex, 10, 36, 180, 5, 2, new Color(1f, 1f, 1f, 0.15f));
        });

        // 3. EXP Fill Bar (300x36, 9-slice: 18, 18, 18, 18)
        CreateTexture("hud_exp_fill.png", 300, 36, new Vector4(18, 18, 18, 18), (tex) =>
        {
            // Blue Gradient (#0277BD -> #4FC3F7)
            FillVerticalGradientRoundedRect(tex, 0, 0, 300, 36, 18, new Color(0.01f, 0.47f, 0.74f, 1f), new Color(0.31f, 0.76f, 0.97f, 1f));
            // Top Shine Highlight (#B3E5FC)
            FillTopRoundedRect(tex, 6, 22, 288, 10, 5, new Color(0.70f, 0.90f, 0.99f, 0.55f));
        });

        // 4. Bottom Tab Base (100x100, 9-slice: 16, 16, 16, 16)
        CreateTexture("hud_bottom_tab_base.png", 100, 100, new Vector4(16, 16, 16, 16), (tex) =>
        {
            // Drop Shadow
            FillRoundedRect(tex, 2, 0, 96, 92, 16, new Color(0f, 0f, 0f, 0.28f));
            // Outer Brown Wood Frame (#A88164 -> #C9A385)
            FillRoundedRect(tex, 2, 4, 96, 94, 16, new Color(0.66f, 0.51f, 0.39f, 1f)); // #A88164
            FillVerticalGradientRoundedRect(tex, 2, 8, 96, 90, 16, new Color(0.66f, 0.51f, 0.39f, 1f), new Color(0.79f, 0.64f, 0.52f, 1f)); // #C9A385
            // Inner Beige Card (#F4E8D7 with #E3CDA9 border)
            FillRoundedRect(tex, 6, 12, 88, 82, 12, new Color(0.89f, 0.80f, 0.66f, 1f)); // #E3CDA9 stroke
            FillRoundedRect(tex, 7, 13, 86, 80, 11, new Color(0.96f, 0.91f, 0.84f, 1f)); // #F4E8D7
        });

        // 5. Level Star Badge (80x80)
        CreateTexture("hud_level_star.png", 80, 80, Vector4.zero, (tex) =>
        {
            // Star polygon vertices for 5-pointed star
            Vector2 center = new Vector2(40f, 40f);
            float rOuter = 36f;
            float rInner = 17f;

            // Shadow
            DrawStar(tex, center + new Vector2(1f, -2f), rOuter, rInner, new Color(0f, 0f, 0f, 0.35f));
            // Gold Outline
            DrawStar(tex, center, rOuter, rInner, new Color(1.0f, 0.76f, 0.03f, 1f)); // #FFC107
            // Blue Body
            DrawStar(tex, center, rOuter - 3.5f, rInner - 2f, new Color(0.01f, 0.66f, 0.96f, 1f)); // #03A9F4
            // Top Shine
            DrawTopStarShine(tex, center, rOuter - 3.5f, rInner - 2f, new Color(0.51f, 0.83f, 0.98f, 0.7f)); // #81D4FA
        });

        // 6. Plus Button (44x44)
        CreateTexture("hud_btn_plus.png", 44, 44, Vector4.zero, (tex) =>
        {
            // Drop Shadow
            FillCircle(tex, 22, 20, 20, new Color(0f, 0f, 0f, 0.35f));
            // Green Circle (#43A047 -> #7CB342)
            FillVerticalGradientCircle(tex, 22, 22, 20, new Color(0.26f, 0.63f, 0.28f, 1f), new Color(0.49f, 0.70f, 0.26f, 1f));
            // White '+' symbol
            DrawPlusSymbol(tex, 22, 22, 10, 3, Color.white);
        });

        // 7. Red Alert Badge (!) (40x40)
        CreateTexture("hud_badge_alert.png", 40, 40, Vector4.zero, (tex) =>
        {
            // Drop shadow
            FillCircle(tex, 20, 18, 18, new Color(0f, 0f, 0f, 0.38f));
            // Red Circle (#E53935 -> #FF5252)
            FillVerticalGradientCircle(tex, 20, 20, 18, new Color(0.85f, 0.15f, 0.15f, 1f), new Color(1.0f, 0.32f, 0.32f, 1f));
            // White stroke ring
            DrawCircleStroke(tex, 20, 20, 18, 2, Color.white);
            // White '!' mark
            DrawExclamationMark(tex, 20, 20, Color.white);
        });

        // 8. Callout Arrow Left (24x28)
        CreateTexture("hud_arrow_left.png", 24, 28, Vector4.zero, (tex) =>
        {
            DrawLeftArrow(tex, 24, 28, new Color(0.66f, 0.51f, 0.39f, 1f), new Color(0.96f, 0.91f, 0.84f, 1f));
        });

        // 9. Callout Card Panel (120x120, 9-slice: 24, 24, 24, 24)
        CreateTexture("hud_callout_panel.png", 120, 120, new Vector4(24, 24, 24, 24), (tex) =>
        {
            // Drop Shadow
            FillRoundedRect(tex, 3, 0, 114, 112, 22, new Color(0f, 0f, 0f, 0.30f));
            // Outer Wood Frame (#A88164 -> #C9A385)
            FillRoundedRect(tex, 3, 4, 114, 114, 22, new Color(0.66f, 0.51f, 0.39f, 1f));
            FillVerticalGradientRoundedRect(tex, 3, 8, 114, 110, 22, new Color(0.66f, 0.51f, 0.39f, 1f), new Color(0.79f, 0.64f, 0.52f, 1f));
            // Inner Beige Body (#F4E8D7)
            FillRoundedRect(tex, 8, 13, 104, 100, 16, new Color(0.89f, 0.80f, 0.66f, 1f));
            FillRoundedRect(tex, 9, 14, 102, 98, 15, new Color(0.97f, 0.93f, 0.87f, 1f));
        });

        AssetDatabase.Refresh();
        Debug.Log("[TownshipHUD] Tạo thành công 9 sprites 9-slice sắc nét cho HUD & Mission Widget!");
    }

    // ── Drawing Helpers ────────────────────────────────────────────────────────

    private static void CreateTexture(string fileName, int width, int height, Vector4 border, System.Action<Texture2D> drawAction)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        tex.SetPixels(clear);

        drawAction(tex);
        tex.Apply();

        string path = Path.Combine(TargetFolder, fileName).Replace("\\", "/");
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }

    private static void FillRoundedRect(Texture2D tex, int rx, int ry, int w, int h, int radius, Color color)
    {
        for (int y = ry; y < ry + h; y++)
        {
            for (int x = rx; x < rx + w; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, w, h, radius))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void FillVerticalGradientRoundedRect(Texture2D tex, int rx, int ry, int w, int h, int radius, Color botCol, Color topCol)
    {
        for (int y = ry; y < ry + h; y++)
        {
            float t = (float)(y - ry) / Mathf.Max(1, h - 1);
            Color col = Color.Lerp(botCol, topCol, t);
            for (int x = rx; x < rx + w; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, w, h, radius))
                    BlendPixel(tex, x, y, col);
            }
        }
    }

    private static void FillTopRoundedRect(Texture2D tex, int rx, int ry, int w, int h, int radius, Color color)
    {
        for (int y = ry; y < ry + h; y++)
        {
            for (int x = rx; x < rx + w; x++)
            {
                if (IsInsideRoundedRect(x, y, rx, ry, w, h, radius))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void DrawRoundedStroke(Texture2D tex, int rx, int ry, int w, int h, int radius, int thickness, Color color)
    {
        for (int y = ry; y < ry + h; y++)
        {
            for (int x = rx; x < rx + w; x++)
            {
                bool outer = IsInsideRoundedRect(x, y, rx, ry, w, h, radius);
                bool inner = IsInsideRoundedRect(x, y, rx + thickness, ry + thickness, w - thickness * 2, h - thickness * 2, Mathf.Max(1, radius - thickness));
                if (outer && !inner)
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static bool IsInsideRoundedRect(int x, int y, int rx, int ry, int w, int h, int radius)
    {
        if (x < rx || x >= rx + w || y < ry || y >= ry + h) return false;
        int left = rx + radius;
        int right = rx + w - radius;
        int bottom = ry + radius;
        int top = ry + h - radius;

        if (x < left && y < bottom)
            return (x - left) * (x - left) + (y - bottom) * (y - bottom) <= radius * radius;
        if (x > right && y < bottom)
            return (x - right) * (x - right) + (y - bottom) * (y - bottom) <= radius * radius;
        if (x < left && y > top)
            return (x - left) * (x - left) + (y - top) * (y - top) <= radius * radius;
        if (x > right && y > top)
            return (x - right) * (x - right) + (y - top) * (y - top) <= radius * radius;

        return true;
    }

    private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2)
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void FillVerticalGradientCircle(Texture2D tex, int cx, int cy, int radius, Color botCol, Color topCol)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            float t = (float)(y - (cy - radius)) / (radius * 2f);
            Color col = Color.Lerp(botCol, topCol, t);
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r2)
                    BlendPixel(tex, x, y, col);
            }
        }
    }

    private static void DrawPlusSymbol(Texture2D tex, int cx, int cy, int length, int thickness, Color color)
    {
        int halfThick = thickness / 2;
        // Horizontal bar
        for (int y = cy - halfThick; y <= cy + halfThick; y++)
            for (int x = cx - length; x <= cx + length; x++)
                BlendPixel(tex, x, y, color);

        // Vertical bar
        for (int y = cy - length; y <= cy + length; y++)
            for (int x = cx - halfThick; x <= cx + halfThick; x++)
                BlendPixel(tex, x, y, color);
    }

    private static void DrawStar(Texture2D tex, Vector2 center, float rOuter, float rInner, Color color)
    {
        Vector2[] points = GetStarPoints(center, rOuter, rInner);
        for (int y = (int)(center.y - rOuter); y <= (int)(center.y + rOuter); y++)
        {
            for (int x = (int)(center.x - rOuter); x <= (int)(center.x + rOuter); x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), points))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void DrawTopStarShine(Texture2D tex, Vector2 center, float rOuter, float rInner, Color color)
    {
        Vector2[] points = GetStarPoints(center, rOuter, rInner);
        for (int y = (int)center.y; y <= (int)(center.y + rOuter); y++)
        {
            for (int x = (int)(center.x - rOuter); x <= (int)center.x; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), points))
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static Vector2[] GetStarPoints(Vector2 center, float rOuter, float rInner)
    {
        Vector2[] points = new Vector2[10];
        float angleStep = Mathf.PI / 5f;
        float startAngle = Mathf.PI / 2f; // Top point

        for (int i = 0; i < 10; i++)
        {
            float r = (i % 2 == 0) ? rOuter : rInner;
            float a = startAngle + i * angleStep;
            points[i] = new Vector2(center.x + Mathf.Cos(a) * r, center.y + Mathf.Sin(a) * r);
        }
        return points;
    }

    private static bool IsPointInPolygon(Vector2 p, Vector2[] poly)
    {
        int j = poly.Length - 1;
        bool inside = false;
        for (int i = 0; i < poly.Length; j = i++)
        {
            if (((poly[i].y <= p.y && p.y < poly[j].y) || (poly[j].y <= p.y && p.y < poly[i].y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                inside = !inside;
        }
        return inside;
    }

    private static void DrawCircleStroke(Texture2D tex, int cx, int cy, int radius, int thickness, Color color)
    {
        int rOuter2 = radius * radius;
        int rInner2 = (radius - thickness) * (radius - thickness);
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (d2 <= rOuter2 && d2 >= rInner2)
                    BlendPixel(tex, x, y, color);
            }
        }
    }

    private static void DrawExclamationMark(Texture2D tex, int cx, int cy, Color color)
    {
        // Top vertical bar (thick at top, slightly tapered)
        for (int y = cy - 2; y <= cy + 9; y++)
        {
            int halfW = (y >= cy + 5) ? 2 : 1;
            for (int x = cx - halfW; x <= cx + halfW; x++)
                BlendPixel(tex, x, y, color);
        }
        // Bottom dot
        FillCircle(tex, cx, cy - 7, 2, color);
    }

    private static void DrawLeftArrow(Texture2D tex, int width, int height, Color strokeCol, Color fillCol)
    {
        Vector2 p1 = new Vector2(2f, height / 2f);
        Vector2 p2 = new Vector2(width - 2f, height - 3f);
        Vector2 p3 = new Vector2(width - 2f, 3f);
        Vector2[] poly = new Vector2[] { p1, p2, p3 };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), poly))
                {
                    if (x <= 5 || y <= 5 || y >= height - 6 || x >= width - 4)
                        BlendPixel(tex, x, y, strokeCol);
                    else
                        BlendPixel(tex, x, y, fillCol);
                }
            }
        }
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
