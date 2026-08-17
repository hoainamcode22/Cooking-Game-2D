#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ShopSpriteGenerator
{
    private const string AssetFolder = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";

    [MenuItem("Tools/Farm/Shop/Generate Shop UI Sprites (Crisp 9-Slice)")]
    public static void GenerateAllSprites()
    {
        if (!Directory.Exists(AssetFolder))
        {
            Directory.CreateDirectory(AssetFolder);
        }

        // 1. Banner Ribbon (480x120 - Swallowtail red tails & 3D Gold Pill Plaque matching shop_banner_ribbon.svg 100%)
        CreateTexture("shop_banner_ribbon.png", 480, 120, new Vector4(90, 24, 90, 24), (tex) =>
        {
            // Left Swallowtail tail (#C55627 with #873010 border)
            DrawSwallowTailLeft(tex, 10, 24, 90, 72, new Color(0.53f, 0.19f, 0.06f, 1f), new Color(0.77f, 0.34f, 0.15f, 1f));
            // Right Swallowtail tail
            DrawSwallowTailRight(tex, 380, 24, 90, 72, new Color(0.53f, 0.19f, 0.06f, 1f), new Color(0.77f, 0.34f, 0.15f, 1f));

            // Central Pill Plaque (x = 75 to 405, width = 330, height = 96, radius = 48)
            FillRoundedRect(tex, 75, 8, 330, 96, 48, new Color(0.57f, 0.30f, 0.10f, 1f)); // #914D1A dark shadow
            FillRoundedRect(tex, 75, 14, 330, 96, 48, new Color(0.76f, 0.47f, 0.13f, 1f)); // #C37822 border
            FillVerticalGradientRoundedRect(tex, 80, 19, 320, 86, 43, new Color(0.95f, 0.62f, 0.17f, 1f), new Color(0.99f, 0.88f, 0.41f, 1f)); // #F29F2B -> #FDE168

            // Top Shine Curve (#FEECA2)
            FillTopRoundedRect(tex, 110, 78, 260, 18, 9, new Color(1.0f, 0.93f, 0.64f, 0.75f));
        });

        // 2. Search Box Background (#F3E2BB with #D9B478 border)
        CreateTexture("shop_search_box.png", 140, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 140, 56, 18, new Color(0.85f, 0.71f, 0.47f, 1f)); // #D9B478 border
            FillRoundedRect(tex, 3, 3, 134, 50, 15, new Color(0.95f, 0.89f, 0.73f, 1f)); // #F3E2BB fill
        });

        // 3. Currency Chip (#FFF6DE -> #FFE9BD with #E0B26A border)
        CreateTexture("shop_currency_chip.png", 140, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 140, 56, 18, new Color(0.88f, 0.70f, 0.42f, 1f)); // #E0B26A border
            FillVerticalGradientRoundedRect(tex, 3, 3, 134, 50, 15, new Color(1.0f, 0.91f, 0.74f, 1f), new Color(1.0f, 0.96f, 0.87f, 1f)); // #FFE9BD -> #FFF6DE
            FillTopRoundedRect(tex, 8, 43, 124, 7, 3, new Color(1.0f, 1.0f, 1.0f, 0.6f)); // Top shine
        });

        // 4. Item Card Outer Wood (Thẻ mẫu 3a: #C98F52 -> #A96F36)
        CreateTexture("shop_card_outer.png", 160, 210, new Vector4(26, 26, 26, 26), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 160, 204, 24, new Color(0.35f, 0.20f, 0.06f, 0.45f)); // Bottom shadow
            FillRoundedRect(tex, 0, 6, 160, 204, 24, new Color(0.64f, 0.36f, 0.08f, 1f)); // #A35C14 border
            FillVerticalGradientRoundedRect(tex, 4, 10, 152, 196, 20, new Color(0.66f, 0.44f, 0.21f, 1f), new Color(0.79f, 0.56f, 0.32f, 1f)); // #A96F36 -> #C98F52
            FillTopRoundedRect(tex, 10, 194, 140, 8, 4, new Color(1.0f, 1.0f, 1.0f, 0.35f)); // Top highlight
        });

        // 5. Item Card Inner Paper (#FFFAF0 -> #FDF0D3 with #ECD4A5 border)
        CreateTexture("shop_card_inner.png", 140, 170, new Vector4(20, 20, 20, 20), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 140, 170, 18, new Color(0.93f, 0.83f, 0.65f, 1f)); // #ECD4A5 border
            FillVerticalGradientRoundedRect(tex, 3, 3, 134, 164, 15, new Color(0.99f, 0.94f, 0.83f, 1f), new Color(1.0f, 0.98f, 0.94f, 1f)); // #FDF0D3 -> #FFFAF0
        });

        // 6. Circle Plate Avatar (#FAF0D6 -> #F1DFB4)
        CreateTexture("shop_circle_plate.png", 120, 120, Vector4.zero, (tex) =>
        {
            FillCircle(tex, 60, 60, 58, new Color(0.85f, 0.72f, 0.52f, 0.4f)); // Outer shadow rim
            FillVerticalGradientCircle(tex, 60, 60, 56, new Color(0.95f, 0.87f, 0.71f, 1f), new Color(0.98f, 0.94f, 0.84f, 1f)); // #F1DFB4 -> #FAF0D6
            FillCircle(tex, 60, 60, 52, new Color(0.97f, 0.92f, 0.80f, 0.6f)); // Inset ring
        });

        // 7. Buy Button Gold / Green (#A5E05E -> #57A51F with #3F8A12 border)
        CreateTexture("shop_btn_buy_gold.png", 160, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 160, 52, 16, new Color(0.25f, 0.54f, 0.07f, 1f)); // #3F8A12 border & shadow
            FillVerticalGradientRoundedRect(tex, 3, 4, 154, 48, 14, new Color(0.34f, 0.65f, 0.12f, 1f), new Color(0.65f, 0.88f, 0.37f, 1f)); // #57A51F -> #A5E05E
            FillTopRoundedRect(tex, 10, 44, 140, 6, 3, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        });

        // 8. Buy Button Gem / Blue (#7CC9F0 -> #3486C2 with #2E6FA3 border)
        CreateTexture("shop_btn_buy_gem.png", 160, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 160, 52, 16, new Color(0.18f, 0.44f, 0.64f, 1f)); // #2E6FA3 border & shadow
            FillVerticalGradientRoundedRect(tex, 3, 4, 154, 48, 14, new Color(0.20f, 0.53f, 0.76f, 1f), new Color(0.49f, 0.79f, 0.94f, 1f)); // #3486C2 -> #7CC9F0
            FillTopRoundedRect(tex, 10, 44, 140, 6, 3, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        });

        // 9. Buy Button Locked / Gray (#B8AE95 with #9C927C border)
        CreateTexture("shop_btn_buy_locked.png", 160, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 160, 52, 16, new Color(0.61f, 0.57f, 0.49f, 1f)); // #9C927C
            FillRoundedRect(tex, 3, 4, 154, 48, 14, new Color(0.72f, 0.68f, 0.58f, 1f)); // #B8AE95
        });

        // 10. Toast Notification (#A5E05E -> #61B527 with #3F8A12 border)
        CreateTexture("shop_toast.png", 220, 56, new Vector4(20, 16, 20, 16), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 220, 56, 18, new Color(0.25f, 0.54f, 0.07f, 1f)); // #3F8A12
            FillVerticalGradientRoundedRect(tex, 3, 3, 214, 50, 15, new Color(0.38f, 0.71f, 0.15f, 1f), new Color(0.65f, 0.88f, 0.37f, 1f)); // #61B527 -> #A5E05E
            FillTopRoundedRect(tex, 12, 45, 196, 6, 3, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        });

        // 11. Lock Badge Background (#5B4226 with #8A6A42 border)
        CreateTexture("shop_lock_badge.png", 64, 64, Vector4.zero, (tex) =>
        {
            FillCircle(tex, 32, 32, 30, new Color(0.54f, 0.42f, 0.26f, 1f)); // #8A6A42
            FillCircle(tex, 32, 32, 26, new Color(0.36f, 0.26f, 0.15f, 1f)); // #5B4226
        });

        // 12. Panel Khung Gỗ 9-Slice (chuẩn 100% shop_panel.svg)
        CreateTexture("shop_panel.png", 200, 200, new Vector4(36, 36, 36, 36), (tex) =>
        {
            // Outer shadow #42250F
            FillRoundedRect(tex, 0, 0, 200, 196, 26, new Color(0.26f, 0.15f, 0.06f, 1f));
            // Wood border #995D28
            FillRoundedRect(tex, 0, 4, 200, 196, 26, new Color(0.60f, 0.36f, 0.16f, 1f));
            // Planks base #CC9351
            FillRoundedRect(tex, 6, 10, 188, 184, 18, new Color(0.80f, 0.58f, 0.32f, 1f));
            // Inner shadow #6C3E14
            FillRoundedRect(tex, 8, 12, 184, 180, 16, new Color(0.72f, 0.50f, 0.26f, 0.4f));
            // Top highlight #F0C389
            FillTopRoundedRect(tex, 16, 186, 168, 6, 3, new Color(0.94f, 0.76f, 0.54f, 0.6f));
            // 4 Corner Rivets (#FFD494 -> #CA9751)
            int[,] rivets = { { 24, 24 }, { 176, 24 }, { 24, 176 }, { 176, 176 } };
            for (int i = 0; i < 4; i++)
            {
                FillCircle(tex, rivets[i, 0], rivets[i, 1], 10, new Color(0.33f, 0.17f, 0.05f, 1f)); // border
                FillVerticalGradientCircle(tex, rivets[i, 0], rivets[i, 1], 8, new Color(0.79f, 0.59f, 0.32f, 1f), new Color(1.0f, 0.83f, 0.58f, 1f));
            }
        });

        AssetDatabase.Refresh();
        Debug.Log("[ShopSpriteGenerator] Đã tạo toàn bộ UI Sprites 9-slice độ nét cao cho Shop (bao gồm shop_banner_ribbon & shop_panel chuẩn mockup 100%)!");
    }

    private static void CreateTexture(string fileName, int width, int height, Vector4 border, System.Action<Texture2D> painter)
    {
        string fullPath = Path.Combine(AssetFolder, fileName).Replace("\\", "/");
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] clearPixels = new Color[width * height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.clear;
        tex.SetPixels(clearPixels);

        painter(tex);
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    // ── Drawing Utilities ───────────────────────────────────────────────────

    private static void DrawSwallowTailLeft(Texture2D tex, float x, float y, float w, float h, Color borderCol, Color fillCol)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                // Triangle cut on left: notch at (x + w*0.35, y + h*0.5)
                float ny = Mathf.Abs((py - (y + h * 0.5f)) / (h * 0.5f));
                float notchX = x + (1f - ny) * (w * 0.35f);

                if (px >= notchX && px <= x + w)
                {
                    bool isBorder = (px <= notchX + 4f || px >= x + w - 3f || py <= y + 3f || py >= y + h - 3f);
                    BlendPixel(tex, px, py, isBorder ? borderCol : fillCol);
                }
            }
        }
    }

    private static void DrawSwallowTailRight(Texture2D tex, float x, float y, float w, float h, Color borderCol, Color fillCol)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                float ny = Mathf.Abs((py - (y + h * 0.5f)) / (h * 0.5f));
                float notchX = (x + w) - (1f - ny) * (w * 0.35f);

                if (px <= notchX && px >= x)
                {
                    bool isBorder = (px >= notchX - 4f || px <= x + 3f || py <= y + 3f || py >= y + h - 3f);
                    BlendPixel(tex, px, py, isBorder ? borderCol : fillCol);
                }
            }
        }
    }

    private static void FillRoundedRect(Texture2D tex, float x, float y, float w, float h, float radius, Color col)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                float d = DistanceToRoundedRect(px + 0.5f, py + 0.5f, x, y, w, h, radius);
                if (d <= 0f) BlendPixel(tex, px, py, col);
                else if (d < 1f) { Color c = col; c.a *= (1f - d); BlendPixel(tex, px, py, c); }
            }
        }
    }

    private static void FillTopRoundedRect(Texture2D tex, float x, float y, float w, float h, float radius, Color col)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                float d = DistanceToTopRoundedRect(px + 0.5f, py + 0.5f, x, y, w, h, radius);
                if (d <= 0f) BlendPixel(tex, px, py, col);
                else if (d < 1f) { Color c = col; c.a *= (1f - d); BlendPixel(tex, px, py, c); }
            }
        }
    }

    private static void FillVerticalGradientRoundedRect(Texture2D tex, float x, float y, float w, float h, float radius, Color bottomCol, Color topCol)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            float t = Mathf.Clamp01((py - y) / h);
            Color col = Color.Lerp(bottomCol, topCol, t);

            for (int px = x0; px <= x1; px++)
            {
                float d = DistanceToRoundedRect(px + 0.5f, py + 0.5f, x, y, w, h, radius);
                if (d <= 0f) BlendPixel(tex, px, py, col);
                else if (d < 1f) { Color c = col; c.a *= (1f - d); BlendPixel(tex, px, py, c); }
            }
        }
    }

    private static void FillCircle(Texture2D tex, float cx, float cy, float radius, Color col)
    {
        int x0 = Mathf.Max(0, (int)(cx - radius - 1));
        int y0 = Mathf.Max(0, (int)(cy - radius - 1));
        int x1 = Mathf.Min(tex.width - 1, (int)(cx + radius + 1));
        int y1 = Mathf.Min(tex.height - 1, (int)(cy + radius + 1));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                float dist = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(cx, cy));
                if (dist <= radius - 0.5f) BlendPixel(tex, px, py, col);
                else if (dist < radius + 0.5f) { Color c = col; c.a *= Mathf.Clamp01(radius + 0.5f - dist); BlendPixel(tex, px, py, c); }
            }
        }
    }

    private static void FillVerticalGradientCircle(Texture2D tex, float cx, float cy, float radius, Color bottomCol, Color topCol)
    {
        int x0 = Mathf.Max(0, (int)(cx - radius - 1));
        int y0 = Mathf.Max(0, (int)(cy - radius - 1));
        int x1 = Mathf.Min(tex.width - 1, (int)(cx + radius + 1));
        int y1 = Mathf.Min(tex.height - 1, (int)(cy + radius + 1));

        for (int py = y0; py <= y1; py++)
        {
            float t = Mathf.Clamp01((py - (cy - radius)) / (radius * 2f));
            Color col = Color.Lerp(bottomCol, topCol, t);

            for (int px = x0; px <= x1; px++)
            {
                float dist = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(cx, cy));
                if (dist <= radius - 0.5f) BlendPixel(tex, px, py, col);
                else if (dist < radius + 0.5f) { Color c = col; c.a *= Mathf.Clamp01(radius + 0.5f - dist); BlendPixel(tex, px, py, c); }
            }
        }
    }

    private static float DistanceToRoundedRect(float px, float py, float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(px - (x + w * 0.5f)) - (w * 0.5f - r);
        float qy = Mathf.Abs(py - (y + h * 0.5f)) - (h * 0.5f - r);
        return Vector2.Max(new Vector2(qx, qy), Vector2.zero).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0.0f) - r;
    }

    private static float DistanceToTopRoundedRect(float px, float py, float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(px - (x + w * 0.5f)) - (w * 0.5f - r);
        float qy = (py - y) > (h - r) ? (py - (y + h - r)) : 0f;
        if (py < y) return y - py;
        if (py > y + h) return py - (y + h);
        if (px < x) return x - px;
        if (px > x + w) return px - (x + w);

        if (py > y + h - r)
        {
            return Vector2.Max(new Vector2(qx, qy), Vector2.zero).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0.0f) - r;
        }
        return 0f;
    }

    private static void BlendPixel(Texture2D tex, int x, int y, Color col)
    {
        Color current = tex.GetPixel(x, y);
        float srcA = col.a;
        float dstA = current.a * (1f - srcA);
        float outA = srcA + dstA;
        if (outA <= 0f) return;

        Color outCol = (col * srcA + current * dstA) / outA;
        outCol.a = outA;
        tex.SetPixel(x, y, outCol);
    }
}
#endif
