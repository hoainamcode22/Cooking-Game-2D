#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MayAnim.EditorTools
{
    [InitializeOnLoad]
    public static class ProcessFeedMillSpriteTool
    {
        private const string SourceJpg = @"C:\Users\acer\.gemini\antigravity\brain\2d1f16d5-9cd0-4d9f-83d8-086396818890\feed_mill_spritesheet_1789720413100.jpg";
        private const string TargetPng = "Assets/Assetsgame/Nhà/BUIDING_ANIM/maylamthucan.png";
        private const string BackupPng = "Assets/Assetsgame/Nhà/BUIDING_ANIM/maylamthucan_old_backup.png";

        static ProcessFeedMillSpriteTool()
        {
            EditorApplication.delayCall += AutoRunOnce;
        }

        private static void AutoRunOnce()
        {
            if (File.Exists(SourceJpg) && !File.Exists(BackupPng))
            {
                ProcessAndSetup();
            }
        }

        [MenuItem("Tools/Farm/Process New Feed Mill (12 Frames) & Setup Anim", false, 199)]
        public static void ProcessAndSetup()
        {
            if (!File.Exists(SourceJpg))
            {
                Debug.LogError("[ProcessFeedMill] Không tìm thấy file source jpg: " + SourceJpg);
                return;
            }

            if (File.Exists(TargetPng) && !File.Exists(BackupPng))
            {
                File.Copy(TargetPng, BackupPng, true);
                Debug.Log("[ProcessFeedMill] Đã backup maylamthucan.png cũ vào " + BackupPng);
            }

            byte[] jpgBytes = File.ReadAllBytes(SourceJpg);
            var srcTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!srcTex.LoadImage(jpgBytes))
            {
                Debug.LogError("[ProcessFeedMill] Không load được Texture2D từ jpg");
                return;
            }

            int srcW = srcTex.width;
            int srcH = srcTex.height;

            // Source has 4 columns and 4 rows (16 frames).
            // We take 3 rows (Row 0, Row 1, Row 3 from top to bottom) -> 12 frames.
            int rowH = srcH / 4;
            int outW = srcW;
            int outH = rowH * 3;

            var outTex = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            Color[] outPixels = new Color[outW * outH];

            // In Unity Texture2D: y=0 is bottom, y=srcH-1 is top.
            // Row 0 (top): y from srcH - rowH to srcH - 1 -> mapped to outH - rowH to outH - 1
            // Row 1 (2nd from top): y from srcH - rowH*2 to srcH - rowH - 1 -> mapped to outH - rowH*2 to outH - rowH - 1
            // Row 3 (bottom): y from 0 to rowH - 1 -> mapped to 0 to rowH - 1

            // Copy Row 0 (top)
            CopyRow(srcTex, outPixels, srcW, srcH, srcH - rowH, outH - rowH, rowH);
            // Copy Row 1
            CopyRow(srcTex, outPixels, srcW, srcH, srcH - rowH * 2, outH - rowH * 2, rowH);
            // Copy Row 3 (bottom of source -> bottom of out)
            CopyRow(srcTex, outPixels, srcW, srcH, 0, 0, rowH);

            // Key out white background with flood fill from borders & grid dividers
            bool[] isBg = new bool[outW * outH];
            bool[] visited = new bool[outW * outH];
            var queue = new System.Collections.Generic.Queue<int>();

            for (int x = 0; x < outW; x++)
            {
                queue.Enqueue(0 * outW + x);
                queue.Enqueue((outH - 1) * outW + x);
            }
            for (int y = 0; y < outH; y++)
            {
                queue.Enqueue(y * outW + 0);
                queue.Enqueue(y * outW + (outW - 1));
            }

            // Cell dividers
            int colW = outW / 4;
            for (int c = 1; c < 4; c++)
            {
                int cx = c * colW;
                for (int y = 0; y < outH; y++)
                    queue.Enqueue(y * outW + cx);
            }
            for (int r = 1; r < 3; r++)
            {
                int ry = r * rowH;
                for (int x = 0; x < outW; x++)
                    queue.Enqueue(ry * outW + x);
            }

            while (queue.Count > 0)
            {
                int idx = queue.Dequeue();
                if (visited[idx]) continue;
                visited[idx] = true;

                int px = idx % outW;
                int py = idx / outW;

                Color c = outPixels[idx];
                float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                float sat = max - min;
                float bright = (c.r + c.g + c.b) / 3f;

                if (sat <= 0.10f && bright >= 0.88f)
                {
                    isBg[idx] = true;
                    if (px + 1 < outW && !visited[py * outW + (px + 1)]) queue.Enqueue(py * outW + (px + 1));
                    if (px - 1 >= 0 && !visited[py * outW + (px - 1)]) queue.Enqueue(py * outW + (px - 1));
                    if (py + 1 < outH && !visited[(py + 1) * outW + px]) queue.Enqueue((py + 1) * outW + px);
                    if (py - 1 >= 0 && !visited[(py - 1) * outW + px]) queue.Enqueue((py - 1) * outW + px);
                }
            }

            for (int y = 0; y < outH; y++)
            {
                for (int x = 0; x < outW; x++)
                {
                    int idx = y * outW + x;
                    if (isBg[idx])
                    {
                        outPixels[idx] = new Color(0, 0, 0, 0);
                    }
                    else
                    {
                        bool nearBg = (x > 0 && isBg[y * outW + (x - 1)]) ||
                                      (x + 1 < outW && isBg[y * outW + (x + 1)]) ||
                                      (y > 0 && isBg[(y - 1) * outW + x]) ||
                                      (y + 1 < outH && isBg[(y + 1) * outW + x]);
                        if (nearBg)
                        {
                            Color c = outPixels[idx];
                            float bright = (c.r + c.g + c.b) / 3f;
                            if (bright > 0.82f)
                            {
                                float alpha = Mathf.Clamp01(1f - (bright - 0.82f) * 5f);
                                outPixels[idx] = new Color(c.r, c.g, c.b, alpha);
                            }
                        }
                    }
                }
            }

            outTex.SetPixels(outPixels);
            outTex.Apply();

            File.WriteAllBytes(TargetPng, outTex.EncodeToPNG());
            Object.DestroyImmediate(srcTex);
            Object.DestroyImmediate(outTex);

            var importer = AssetImporter.GetAtPath(TargetPng) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ProcessFeedMill] Đã tạo và lưu maylamthucan.png 12 frame mới xóa phông!");

            // Run animation setup
            MayAnimSetupTool.LamTatCa();
        }

        private static void CopyRow(Texture2D srcTex, Color[] outPixels, int w, int srcH, int srcStartY, int dstStartY, int rowH)
        {
            for (int dy = 0; dy < rowH; dy++)
            {
                int sy = srcStartY + dy;
                if (sy < 0 || sy >= srcH) continue;
                for (int x = 0; x < w; x++)
                {
                    Color c = srcTex.GetPixel(x, sy);
                    int dstIdx = (dstStartY + dy) * w + x;
                    outPixels[dstIdx] = c;
                }
            }
        }
    }
}
#endif
