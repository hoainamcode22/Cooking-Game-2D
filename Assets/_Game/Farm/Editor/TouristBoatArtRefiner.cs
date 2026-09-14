#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmGame.EditorTools
{
    /// <summary>
    /// Sinh các Sprite chất lượng cao (bo tròn góc mềm mại, viền vàng dập nổi, 9-slice):
    /// 1. dock_unlock_plaque.png: Huy hiệu bo tròn cho 3 bến tàu du lịch.
    /// 2. boat_announce_card_bg.png: Khung toast báo tàu du lịch cập bến.
    /// 3. boat_announce_btn.png: Khung nút bấm bo tròn màu hổ phách "ĐÃ RÕ".
    /// </summary>
    public static class TouristBoatArtRefiner
    {
        public const string TargetFolder = "Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites";

        [MenuItem("Tools/Farm/Tourist Boat/Tạo Sprite Bo Tròn (Bến Tàu & Toast Hint)")]
        public static void GenerateAllSprites()
        {
            if (!Directory.Exists(TargetFolder))
            {
                Directory.CreateDirectory(TargetFolder);
            }

            // 1. Huy hiệu mở bến tàu (240x110, 9-slice: 32, 32, 32, 32)
            CreateTexture("dock_unlock_plaque.png", 240, 110, new Vector4(32, 32, 32, 32), (tex) =>
            {
                // Drop shadow
                FillRoundedRect(tex, 4, 0, 232, 102, 26, new Color(0f, 0f, 0f, 0.45f));
                // Outer dark wood border (#3E2413)
                FillRoundedRect(tex, 4, 6, 232, 102, 26, new Color(0.24f, 0.14f, 0.07f, 1f));
                // Rich golden frame (#F5B041 -> #D68910)
                FillVerticalGradientRoundedRect(tex, 8, 10, 224, 94, 22, new Color(0.84f, 0.54f, 0.06f, 1f), new Color(0.96f, 0.76f, 0.25f, 1f));
                // Inner dark wood bevel
                FillRoundedRect(tex, 12, 14, 216, 86, 18, new Color(0.32f, 0.18f, 0.10f, 1f));
                // Warm parchment/mahogany body (#4A2C18 -> #6B4226)
                FillVerticalGradientRoundedRect(tex, 14, 16, 212, 82, 16, new Color(0.28f, 0.16f, 0.09f, 0.95f), new Color(0.42f, 0.26f, 0.15f, 0.95f));
                // Decorative gold corner rivets
                FillCircle(tex, 24, 86, 3, new Color(1f, 0.85f, 0.35f, 1f));
                FillCircle(tex, 216, 86, 3, new Color(1f, 0.85f, 0.35f, 1f));
                FillCircle(tex, 24, 26, 3, new Color(1f, 0.85f, 0.35f, 1f));
                FillCircle(tex, 216, 26, 3, new Color(1f, 0.85f, 0.35f, 1f));
                // Top subtle shine highlight
                FillTopRoundedRect(tex, 28, 90, 184, 4, 2, new Color(1f, 1f, 1f, 0.25f));
            });

            // 2. Khung Toast Báo Tàu (520x150, 9-slice: 32, 32, 32, 32)
            CreateTexture("boat_announce_card_bg.png", 520, 150, new Vector4(32, 32, 32, 32), (tex) =>
            {
                // Drop shadow
                FillRoundedRect(tex, 6, 0, 508, 142, 28, new Color(0f, 0f, 0f, 0.50f));
                // Outer gold trim (#F39C12 -> #F1C40F)
                FillVerticalGradientRoundedRect(tex, 6, 6, 508, 142, 28, new Color(0.85f, 0.55f, 0.08f, 1f), new Color(0.98f, 0.78f, 0.22f, 1f));
                // Inner dark navy ocean frame (#1A2530)
                FillRoundedRect(tex, 10, 10, 500, 134, 24, new Color(0.10f, 0.15f, 0.20f, 1f));
                // Deep navy parchment gradient (#1B2A38 -> #283E52)
                FillVerticalGradientRoundedRect(tex, 13, 13, 494, 128, 21, new Color(0.11f, 0.16f, 0.22f, 0.97f), new Color(0.18f, 0.27f, 0.36f, 0.97f));
                // Top rim gloss
                FillTopRoundedRect(tex, 25, 133, 470, 5, 2, new Color(1f, 1f, 1f, 0.20f));
                // Golden accent horizontal ribbon line
                FillRoundedRect(tex, 25, 88, 470, 2, 1, new Color(0.95f, 0.75f, 0.20f, 0.45f));
            });

            // 3. Khung Nút Bấm Toast "ĐÃ RÕ" (140x50, 9-slice: 18, 18, 18, 18)
            CreateTexture("boat_announce_btn.png", 140, 50, new Vector4(18, 18, 18, 18), (tex) =>
            {
                // Drop shadow
                FillRoundedRect(tex, 2, 0, 136, 46, 18, new Color(0f, 0f, 0f, 0.40f));
                // Outer white/gold outline (#F9E79F)
                FillRoundedRect(tex, 2, 4, 136, 46, 18, new Color(0.98f, 0.90f, 0.62f, 1f));
                // Vibrant Amber Button Body (#E67E22 -> #F39C12)
                FillVerticalGradientRoundedRect(tex, 4, 6, 132, 42, 16, new Color(0.85f, 0.42f, 0.05f, 1f), new Color(0.98f, 0.65f, 0.12f, 1f));
                // Top shine highlight
                FillTopRoundedRect(tex, 10, 38, 120, 6, 3, new Color(1f, 1f, 1f, 0.38f));
            });

            AssetDatabase.Refresh();
            Debug.Log("[TouristBoatArtRefiner] Đã tạo thành công bộ sprite bo tròn cao cấp cho Bến Tàu và Toast Hint!");
        }

        private static void CreateTexture(string filename, int width, int height, Vector4 border, System.Action<Texture2D> drawAction)
        {
            string fullPath = Path.Combine(TargetFolder, filename);
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
            tex.SetPixels(pixels);

            drawAction(tex);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(fullPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.spritePixelsPerUnit = 100f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }

        private static void FillRoundedRect(Texture2D tex, int x, int y, int width, int height, int radius, Color col)
        {
            int xMax = x + width - 1;
            int yMax = y + height - 1;

            for (int py = y; py <= yMax; py++)
            {
                for (int px = x; px <= xMax; px++)
                {
                    if (IsInsideRoundedRect(px, py, x, y, xMax, yMax, radius))
                    {
                        BlendPixel(tex, px, py, col);
                    }
                }
            }
        }

        private static void FillVerticalGradientRoundedRect(Texture2D tex, int x, int y, int width, int height, int radius, Color bottomCol, Color topCol)
        {
            int xMax = x + width - 1;
            int yMax = y + height - 1;

            for (int py = y; py <= yMax; py++)
            {
                float t = (float)(py - y) / Mathf.Max(1, height - 1);
                Color rowCol = Color.Lerp(bottomCol, topCol, t);

                for (int px = x; px <= xMax; px++)
                {
                    if (IsInsideRoundedRect(px, py, x, y, xMax, yMax, radius))
                    {
                        BlendPixel(tex, px, py, rowCol);
                    }
                }
            }
        }

        private static void FillTopRoundedRect(Texture2D tex, int x, int y, int width, int height, int radius, Color col)
        {
            FillRoundedRect(tex, x, y, width, height, radius, col);
        }

        private static bool IsInsideRoundedRect(int px, int py, int xMin, int yMin, int xMax, int yMax, int radius)
        {
            if (px >= xMin + radius && px <= xMax - radius) return true;
            if (py >= yMin + radius && py <= yMax - radius) return true;

            int cx = (px < xMin + radius) ? xMin + radius : xMax - radius;
            int cy = (py < yMin + radius) ? yMin + radius : yMax - radius;
            int dx = px - cx;
            int dy = py - cy;
            return (dx * dx + dy * dy) <= (radius * radius);
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color col)
        {
            int r2 = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2)
                    {
                        BlendPixel(tex, x, y, col);
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
}
#endif
