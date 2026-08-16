#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class WarehouseSpriteGenerator
{
    private const string AssetFolder = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";

    [MenuItem("Tools/Farm/Warehouse/Generate UI Sprites (Crisp 9-Slice)")]
    public static void GenerateAllSprites()
    {
        if (!Directory.Exists(AssetFolder))
        {
            Directory.CreateDirectory(AssetFolder);
        }

        // 1. Outer Wood Board (1220x760 Design -> 420x280 Sliceable with 4 Corner Studs & Wood Grain)
        CreateTexture("panel_outer.png", 420, 280, new Vector4(60, 60, 60, 60), (tex) =>
        {
            // Viền ngoài ván gỗ
            FillRoundedRect(tex, 0, 0, 420, 280, 40, new Color(0.29f, 0.15f, 0.03f, 1f)); // #4A2508 viền tối
            // Nền ván gỗ gradient #7C4E22 -> #A9743C
            FillVerticalGradientRoundedRect(tex, 6, 6, 408, 268, 34, new Color(0.49f, 0.31f, 0.13f, 1f), new Color(0.66f, 0.45f, 0.24f, 1f));

            // Thớ ván gỗ ngang
            for (int gy = 70; gy < 270; gy += 60)
            {
                FillRoundedRect(tex, 14, gy, 392, 4, 2, new Color(0.23f, 0.11f, 0.02f, 0.25f));
            }

            // Highlight viền trên
            FillTopRoundedRect(tex, 14, 258, 392, 12, 6, new Color(1.0f, 0.90f, 0.70f, 0.30f));

            // 4 đinh sắt ở 4 góc (3 lớp: vành tối, thân đinh, chấm sáng lệch)
            DrawIronStud(tex, 32, 248, 12);
            DrawIronStud(tex, 388, 248, 12);
            DrawIronStud(tex, 32, 32, 12);
            DrawIronStud(tex, 388, 32, 12);
        });

        // 2. Banner Header ("KHO VẬT PHẨM")
        CreateTexture("banner_header.png", 430, 96, new Vector4(30, 24, 30, 24), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 430, 92, 24, new Color(0.64f, 0.36f, 0.08f, 1f)); // #A35C14 border & shadow
            FillVerticalGradientRoundedRect(tex, 5, 6, 420, 84, 20, new Color(0.94f, 0.64f, 0.18f, 1f), new Color(1.0f, 0.82f, 0.34f, 1f)); // #F0A32F -> #FFD257
            FillTopRoundedRect(tex, 14, 76, 402, 10, 5, new Color(1.0f, 1.0f, 1.0f, 0.55f)); // Top highlight
        });

        // 3. Tab Active (Cream Sáng #FFFBE9 -> #FDF0D3)
        CreateTexture("tab_active.png", 180, 64, new Vector4(24, 10, 24, 26), (tex) =>
        {
            FillTopRoundedRect(tex, 0, 0, 180, 64, 18, new Color(0.43f, 0.25f, 0.08f, 1f)); // #6E4014 border
            FillVerticalGradientTopRoundedRect(tex, 4, 0, 172, 60, 14, new Color(0.99f, 0.94f, 0.83f, 1f), new Color(1.0f, 0.98f, 0.91f, 1f)); // #FDF0D3 -> #FFFBE9 cream
        });

        // 4. Tab Inactive (Nâu Gỗ #C48538 -> #E2A75F)
        CreateTexture("tab_inactive.png", 180, 64, new Vector4(24, 10, 24, 26), (tex) =>
        {
            FillTopRoundedRect(tex, 0, 0, 180, 58, 18, new Color(0.43f, 0.25f, 0.08f, 1f)); // #6E4014 border
            FillVerticalGradientTopRoundedRect(tex, 4, 0, 172, 54, 14, new Color(0.77f, 0.52f, 0.22f, 1f), new Color(0.89f, 0.65f, 0.37f, 1f)); // #C48538 -> #E2A75F warm brown
        });

        // 5. Inner Panel (Cream #FDF3DA -> #FBECCB with #6E4014 border)
        CreateTexture("inner_panel.png", 140, 140, new Vector4(24, 24, 24, 24), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 140, 140, 20, new Color(0.43f, 0.25f, 0.08f, 1f)); // #6E4014 border
            FillRoundedRect(tex, 4, 4, 132, 132, 16, new Color(0.95f, 0.87f, 0.69f, 1f)); // #F3DDB0 inner inset line
            FillVerticalGradientRoundedRect(tex, 7, 7, 126, 126, 13, new Color(0.98f, 0.93f, 0.80f, 1f), new Color(0.99f, 0.95f, 0.85f, 1f)); // #FBECCB -> #FDF3DA
        });

        // 6. Slot Selected (Viền vàng sáng #FFCE3D + background #FFF4C2 -> #FFE9A8)
        CreateTexture("slot_selected.png", 130, 130, new Vector4(22, 22, 22, 22), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 130, 130, 20, new Color(1.0f, 0.81f, 0.24f, 0.45f)); // Outer glow
            FillRoundedRect(tex, 3, 3, 124, 124, 18, new Color(1.0f, 0.81f, 0.24f, 1f)); // #FFCE3D border
            FillVerticalGradientRoundedRect(tex, 6, 6, 118, 118, 15, new Color(1.0f, 0.91f, 0.66f, 1f), new Color(1.0f, 0.96f, 0.76f, 1f)); // #FFE9A8 -> #FFF4C2
        });

        // 7. Slot Normal (Viền kem nâu #ECD09C + background #FFFDF4 -> #FDF6E3)
        CreateTexture("slot_normal.png", 130, 130, new Vector4(22, 22, 22, 22), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 130, 130, 20, new Color(0.93f, 0.82f, 0.61f, 1f)); // #ECD09C border
            FillVerticalGradientRoundedRect(tex, 3, 3, 124, 124, 17, new Color(0.99f, 0.96f, 0.89f, 1f), new Color(1.0f, 0.99f, 0.96f, 1f)); // #FDF6E3 -> #FFFDF4
        });

        // 8. Slot Empty (Nét đứt #D9C49A trên nền kem nhạt)
        CreateTexture("slot_empty.png", 130, 130, new Vector4(22, 22, 22, 22), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 130, 130, 20, new Color(0.43f, 0.25f, 0.08f, 0.06f));
            DrawDashedRoundedRect(tex, 3, 3, 124, 124, 17, new Color(0.85f, 0.77f, 0.60f, 0.95f), 8, 5);
        });

        // 9. Badge Count (Pill nâu #6E4014)
        CreateTexture("badge_count.png", 54, 30, new Vector4(15, 10, 15, 10), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 54, 30, 14, new Color(0.43f, 0.25f, 0.08f, 1f)); // #6E4014 dark brown
        });

        // 10. Stepper Box (Dashed container in detail panel)
        CreateTexture("stepper_box.png", 220, 110, new Vector4(20, 20, 20, 20), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 220, 110, 16, new Color(0.43f, 0.25f, 0.08f, 0.08f));
            DrawDashedRoundedRect(tex, 2, 2, 216, 106, 14, new Color(0.79f, 0.60f, 0.36f, 0.9f), 6, 4); // #C99A5C
        });

        // 11. Button Green ("CHUYỂN BẾP")
        CreateTexture("btn_green.png", 260, 60, new Vector4(24, 16, 24, 16), (tex) =>
        {
            FillRoundedRect(tex, 2, 0, 256, 56, 18, new Color(0.25f, 0.54f, 0.07f, 1f)); // #3F8A12 border & 3D shadow
            FillVerticalGradientRoundedRect(tex, 2, 5, 256, 52, 16, new Color(0.34f, 0.65f, 0.12f, 1f), new Color(0.65f, 0.88f, 0.37f, 1f)); // #57A51F -> #A5E05E
            FillTopRoundedRect(tex, 12, 47, 236, 7, 3, new Color(1.0f, 1.0f, 1.0f, 0.45f));
        });

        // 12. Button Upgrade ("NÂNG CẤP")
        CreateTexture("btn_upgrade.png", 140, 48, new Vector4(16, 12, 16, 12), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 140, 46, 13, new Color(0.75f, 0.47f, 0.09f, 1f)); // #C07818 border
            FillVerticalGradientRoundedRect(tex, 3, 3, 134, 42, 11, new Color(0.95f, 0.65f, 0.21f, 1f), new Color(1.0f, 0.85f, 0.47f, 1f)); // #F2A636 -> #FFD977
        });

        // 13. Button Minus (Orange stepper circle)
        CreateTexture("btn_minus.png", 52, 52, Vector4.zero, (tex) =>
        {
            FillCircle(tex, 26, 24, 24, new Color(0.66f, 0.28f, 0.07f, 1f)); // #A84812
            FillVerticalGradientCircle(tex, 26, 27, 22, new Color(0.89f, 0.44f, 0.12f, 1f), new Color(1.0f, 0.69f, 0.40f, 1f)); // #E2701F -> #FFB066
            FillRoundedRect(tex, 15, 24, 22, 6, 3, Color.white); // '-'
        });

        // 14. Button Plus (Green stepper circle)
        CreateTexture("btn_plus.png", 52, 52, Vector4.zero, (tex) =>
        {
            FillCircle(tex, 26, 24, 24, new Color(0.25f, 0.54f, 0.07f, 1f)); // #3F8A12
            FillVerticalGradientCircle(tex, 26, 27, 22, new Color(0.38f, 0.71f, 0.15f, 1f), new Color(0.65f, 0.88f, 0.37f, 1f)); // #61B527 -> #A5E05E
            FillRoundedRect(tex, 15, 24, 22, 6, 3, Color.white); // '+' H
            FillRoundedRect(tex, 23, 16, 6, 22, 3, Color.white); // '+' V
        });

        // 15. Button MAX
        CreateTexture("btn_max.png", 78, 44, new Vector4(12, 10, 12, 10), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 78, 42, 12, new Color(0.69f, 0.49f, 0.23f, 1f)); // #B07C3A
            FillRoundedRect(tex, 2, 2, 74, 38, 10, new Color(0.89f, 0.65f, 0.37f, 1f)); // #E2A75F
        });

        // 16. Circle Preview Avatar (#FAF0D6 -> #F1DFB4)
        CreateTexture("circle_preview.png", 140, 140, Vector4.zero, (tex) =>
        {
            FillCircle(tex, 70, 70, 68, new Color(0.95f, 0.87f, 0.71f, 1f)); // #F1DFB4
            FillCircle(tex, 70, 70, 64, new Color(0.98f, 0.94f, 0.84f, 1f)); // #FAF0D6
        });

        // 17. Progress Bar Track & Fill
        CreateTexture("progress_track.png", 100, 24, new Vector4(12, 6, 12, 6), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 100, 24, 11, new Color(0.77f, 0.60f, 0.38f, 1f));
            FillRoundedRect(tex, 2, 2, 96, 20, 9, new Color(0.91f, 0.82f, 0.64f, 1f)); // #E8D0A4
        });

        CreateTexture("progress_fill.png", 60, 24, new Vector4(8, 4, 8, 4), (tex) =>
        {
            FillVerticalGradientRoundedRect(tex, 0, 0, 60, 24, 9, new Color(0.41f, 0.74f, 0.17f, 1f), new Color(0.66f, 0.89f, 0.44f, 1f)); // #68BD2B -> #A9E470
        });

        // 18. Upgrade Footer Box (#F5B94E -> #FFE2A0)
        CreateTexture("upgrade_box.png", 260, 68, new Vector4(16, 12, 16, 12), (tex) =>
        {
            FillRoundedRect(tex, 0, 0, 260, 68, 15, new Color(0.75f, 0.49f, 0.14f, 1f)); // #C07D24 border
            FillVerticalGradientRoundedRect(tex, 3, 3, 254, 62, 13, new Color(0.96f, 0.73f, 0.31f, 1f), new Color(1.0f, 0.89f, 0.63f, 1f)); // #F5B94E -> #FFE2A0
        });

        AssetDatabase.Refresh();
        Debug.Log("[WarehouseSpriteGenerator] Đã cập nhật khung ván gỗ và bộ UI Sprite đồng bộ 100% với Popup Nhiệm Vụ!");
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

    private static void DrawIronStud(Texture2D tex, float cx, float cy, float radius)
    {
        FillCircle(tex, cx, cy, radius + 2, new Color(0.18f, 0.12f, 0.08f, 1f)); // #2F1E14 vành tối
        FillCircle(tex, cx, cy, radius, new Color(0.29f, 0.21f, 0.16f, 1f)); // #4A3528 thân đinh
        FillCircle(tex, cx - radius * 0.25f, cy + radius * 0.25f, radius * 0.4f, new Color(0.56f, 0.46f, 0.39f, 0.9f)); // #8E7564 chấm sáng
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

    private static void FillVerticalGradientTopRoundedRect(Texture2D tex, float x, float y, float w, float h, float radius, Color bottomCol, Color topCol)
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
                float d = DistanceToTopRoundedRect(px + 0.5f, py + 0.5f, x, y, w, h, radius);
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

    private static void DrawDashedRoundedRect(Texture2D tex, float x, float y, float w, float h, float radius, Color col, float dashLen, float gapLen)
    {
        int x0 = Mathf.Max(0, (int)x);
        int y0 = Mathf.Max(0, (int)y);
        int x1 = Mathf.Min(tex.width - 1, (int)(x + w));
        int y1 = Mathf.Min(tex.height - 1, (int)(y + h));

        for (int py = y0; py <= y1; py++)
        {
            for (int px = x0; px <= x1; px++)
            {
                float d = Mathf.Abs(DistanceToRoundedRect(px + 0.5f, py + 0.5f, x, y, w, h, radius));
                if (d <= 1.5f)
                {
                    float perimeterPos = (px + py);
                    if ((perimeterPos % (dashLen + gapLen)) < dashLen)
                    {
                        BlendPixel(tex, px, py, col);
                    }
                }
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
