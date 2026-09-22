using System.IO;
using UnityEditor;
using UnityEngine;

namespace KitchenCozyV3.Editor
{
    public static class CozyKitchenAssetProcessorTool
    {
        private const string FolderPath = "Assets/Art/UI/KitchenCozyV3";
        private const string ProcessedFolderPath = "Assets/Art/UI/KitchenCozyV3/Processed";

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/Process & Slice Sprites (Chroma-Key Magenta)")]
        public static void ProcessAndSliceAll()
        {
            if (!AssetDatabase.IsValidFolder(ProcessedFolderPath))
            {
                AssetDatabase.CreateFolder(FolderPath, "Processed");
            }

            ProcessCatChef();
            ProcessAppliances();
            ProcessUIFrames();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[CozyKitchen] All sprites successfully chroma-keyed and sliced into '" + ProcessedFolderPath + "'!</color>");
        }

        private static void ProcessCatChef()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "cat_chef_dance_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            int cols = 4;
            int rows = 3;
            int cellW = src.width / cols;
            int cellH = src.height / rows;

            int frameIndex = 1;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x = c * cellW;
                    int y = (rows - 1 - r) * cellH; // Texture coordinates start bottom-left

                    Texture2D cell = ExtractAndChromaKey(src, x, y, cellW, cellH);
                    string outPath = $"{ProcessedFolderPath}/cat_chef_frame_{frameIndex:D2}.png";
                    SaveTextureAsPNG(cell, outPath);
                    frameIndex++;
                }
            }
        }

        private static void ProcessAppliances()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "kitchen_appliances_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            int cols = 3;
            int rows = 2;
            int cellW = src.width / cols;
            int cellH = src.height / rows;

            string[] names = {
                "cutting_board", "gas_stove_pan", "fried_rice_pan",
                "stone_pizza_oven", "storage_basket", "btn_auto_cook_badge"
            };

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (idx >= names.Length) break;
                    int x = c * cellW;
                    int y = (rows - 1 - r) * cellH;

                    Texture2D cell = ExtractAndChromaKey(src, x, y, cellW, cellH);
                    string outPath = $"{ProcessedFolderPath}/{names[idx]}.png";
                    SaveTextureAsPNG(cell, outPath);
                    idx++;
                }
            }
        }

        private static void ProcessUIFrames()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "cozy_kitchen_ui_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            int cols = 3;
            int rows = 2;
            int cellW = src.width / cols;
            int cellH = src.height / rows;

            string[] names = {
                "frame_recipe_book", "frame_dish_card", "frame_chalkboard",
                "shelf_ingredients", "btn_cook_pill", "tabs_category_wood"
            };

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (idx >= names.Length) break;
                    int x = c * cellW;
                    int y = (rows - 1 - r) * cellH;

                    Texture2D cell = ExtractAndChromaKey(src, x, y, cellW, cellH);
                    string outPath = $"{ProcessedFolderPath}/{names[idx]}.png";
                    SaveTextureAsPNG(cell, outPath);
                    idx++;
                }
            }
        }

        private static Texture2D LoadReadableTexture(string fullPath)
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }

        private static Texture2D ExtractAndChromaKey(Texture2D src, int startX, int startY, int width, int height)
        {
            Texture2D dst = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = src.GetPixels(startX, startY, width, height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float r = c.r;
                float g = c.g;
                float b = c.b;

                // Chroma key threshold for magenta #FF00FF
                if (r > 0.65f && b > 0.65f && g < 0.35f && Mathf.Abs(r - b) < 0.25f)
                {
                    pixels[i] = Color.clear;
                }
                else if (r > 0.55f && b > 0.55f && g < 0.45f)
                {
                    float alpha = Mathf.Clamp01((g - 0.25f) / 0.2f);
                    pixels[i] = new Color(r, g, b, alpha);
                }
            }

            dst.SetPixels(pixels);
            dst.Apply();
            return dst;
        }

        private static void SaveTextureAsPNG(Texture2D tex, string assetPath)
        {
            byte[] pngData = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(fullPath, pngData);
        }
    }
}
