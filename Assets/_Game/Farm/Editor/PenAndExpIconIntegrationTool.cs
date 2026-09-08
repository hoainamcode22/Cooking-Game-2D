#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool hoàn tất 2 yêu cầu của Sếp:
/// 1. Tách nền trắng và đưa 4 icon chuồng gia súc đã vẽ vào game (Gán vào Chuồng Gà, Chuồng Bò, Chuồng Bò Sữa, Chuồng Heo).
/// 2. Thay toàn bộ icon EXP trong Bảng Đơn Hàng (Order Board) thành ngôi sao viền vàng ruột xanh chuẩn HUD (hud_level_star.png), bỏ tint màu xanh.
/// </summary>
public static class PenAndExpIconIntegrationTool
{
    private const string MENU_INTEGRATE = "Tools/Farm Game/Shop/★ ĐƯA 4 CHUỒNG VÀO GAME & ĐỒNG BỘ EXP HUD (1-CLICK)";

    private static readonly string ArtifactsDir = @"C:\Users\acer\.gemini\antigravity\brain\a33db5e9-4cd0-476e-89ad-e7d5e1744879";

    [MenuItem(MENU_INTEGRATE, false, 0)]
    public static void IntegrateAll()
    {
        var sb = new StringBuilder("===== TÍCH HỢP 4 CHUỒNG & ĐỒNG BỘ EXP HUD =====\n\n");

        try
        {
            // ─── BƯỚC 1: XỬ LÝ & ĐƯA 4 ICON CHUỒNG VÀO GAME ─────────────────────
            sb.AppendLine("1. XỬ LÝ 4 ICON CHUỒNG GIA SÚC:");
            ProcessPenIcons(sb);

            // ─── BƯỚC 2: THAY ICON EXP TRONG ĐƠN HÀNG THÀNH HUD LEVEL STAR ──────
            sb.AppendLine("\n2. ĐỒNG BỘ ICON EXP ĐƠN HÀNG VỚI HUD:");
            SyncExpIconsWithHUD(sb);

            // ─── BƯỚC 3: ĐỒNG BỘ SHOP TRONG SCENE HIỆN TẠI ──────────────────────
            sb.AppendLine("\n3. KIỂM TRA & CẬP NHẬT SHOP TRONG SCENE:");
            SyncSceneShop(sb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sb.AppendLine("\n✔ HOÀN TẤT THÀNH CÔNG 100%!");
            Debug.Log(sb.ToString());

            EditorUtility.DisplayDialog("Tích Hợp Hoàn Tất", sb.ToString(), "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PenAndExpIconIntegrationTool] Lỗi: {ex}");
            EditorUtility.DisplayDialog("Lỗi", "Đã xảy ra lỗi:\n" + ex.Message, "OK");
        }
    }

    // =========================================================================
    //  1. XỬ LÝ ICON CHUỒNG
    // =========================================================================

    private struct PenAssetMapping
    {
        public string ArtifactJpgName;
        public string TargetPngPath;
        public string BuildingAssetPath;
        public string DisplayName;
    }

    private static void ProcessPenIcons(StringBuilder sb)
    {
        var mappings = new[]
        {
            new PenAssetMapping
            {
                ArtifactJpgName = "chuong_ga_clean_1788792616973.jpg",
                TargetPngPath = "Assets/Assetsgame/Buiding/icon_chuong_ga_v2.png",
                BuildingAssetPath = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Gà.asset",
                DisplayName = "Chuồng Gà"
            },
            new PenAssetMapping
            {
                ArtifactJpgName = "chuong_bo_clean_1788792646292.jpg",
                TargetPngPath = "Assets/Assetsgame/Buiding/icon_chuong_bo_v2.png",
                BuildingAssetPath = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Bò.asset",
                DisplayName = "Chuồng Bò"
            },
            new PenAssetMapping
            {
                ArtifactJpgName = "chuong_bo_sua_icon_1788792413277.jpg",
                TargetPngPath = "Assets/Assetsgame/Buiding/icon_chuong_bo_sua_v2.png",
                BuildingAssetPath = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Bò Sữa.asset",
                DisplayName = "Chuồng Bò Sữa"
            },
            new PenAssetMapping
            {
                ArtifactJpgName = "chuong_heo_icon_1788792436484.jpg",
                TargetPngPath = "Assets/Assetsgame/Buiding/icon_chuong_heo_v2.png",
                BuildingAssetPath = "Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Heo.asset",
                DisplayName = "Chuồng Heo"
            }
        };

        foreach (var m in mappings)
        {
            string srcPath = Path.Combine(ArtifactsDir, m.ArtifactJpgName);
            if (!File.Exists(srcPath))
            {
                sb.AppendLine($"  ⚠ Không tìm thấy file gốc: {m.ArtifactJpgName}");
                continue;
            }

            // Đọc và tách nền trắng
            byte[] jpgBytes = File.ReadAllBytes(srcPath);
            var rawTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!rawTex.LoadImage(jpgBytes))
            {
                sb.AppendLine($"  ⚠ Không đọc được ảnh JPG: {m.ArtifactJpgName}");
                UnityEngine.Object.DestroyImmediate(rawTex);
                continue;
            }

            Texture2D cutoutTex = CutoutWhiteBackground(rawTex, 512);
            UnityEngine.Object.DestroyImmediate(rawTex);

            byte[] pngBytes = cutoutTex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(cutoutTex);

            string fullTargetPng = Path.Combine(Application.dataPath, m.TargetPngPath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullTargetPng);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(fullTargetPng, pngBytes);
            AssetDatabase.ImportAsset(m.TargetPngPath, ImportAssetOptions.ForceUpdate);

            // Cấu hình TextureImporter
            var importer = AssetImporter.GetAtPath(m.TargetPngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m.TargetPngPath);
            if (newSprite == null)
            {
                sb.AppendLine($"  ⚠ Không tải được Sprite từ: {m.TargetPngPath}");
                continue;
            }

            // Gán vào BuildingData ScriptableObject
            var buildingData = AssetDatabase.LoadAssetAtPath<BuildingData>(m.BuildingAssetPath);
            if (buildingData != null)
            {
                buildingData.itemIcon = newSprite;
                EditorUtility.SetDirty(buildingData);
                sb.AppendLine($"  ✔ Đã gán icon mới vào: {m.DisplayName} ({m.BuildingAssetPath})");
            }
            else
            {
                sb.AppendLine($"  ⚠ Không tìm thấy BuildingData asset: {m.BuildingAssetPath}");
            }
        }
    }

    /// <summary>
    /// Khử nền trắng bằng BFS flood-fill từ 4 cạnh viền và tạo dải chuyển alpha mượt mà.
    /// </summary>
    private static Texture2D CutoutWhiteBackground(Texture2D src, int targetSize)
    {
        int w = src.width;
        int h = src.height;
        Color32[] pixels = src.GetPixels32();

        bool[] visited = new bool[w * h];
        var queue = new Queue<int>(w * 4);

        void TryEnqueue(int x, int y)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            int idx = y * w + x;
            if (visited[idx]) return;

            Color32 c = pixels[idx];
            // Ngưỡng trắng
            if (c.r >= 235 && c.g >= 235 && c.b >= 235)
            {
                visited[idx] = true;
                queue.Enqueue(idx);
            }
        }

        // 4 cạnh viền
        for (int x = 0; x < w; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, h - 1);
        }
        for (int y = 0; y < h; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(w - 1, y);
        }

        // BFS loang hết phần nền trắng
        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            pixels[idx].a = 0;

            int cx = idx % w;
            int cy = idx / w;

            TryEnqueue(cx + 1, cy);
            TryEnqueue(cx - 1, cy);
            TryEnqueue(cx, cy + 1);
            TryEnqueue(cx, cy - 1);
        }

        // Làm mịn viền (Feathering các pixel sáng giáp ranh nền trong suốt)
        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                int idx = y * w + x;
                if (!visited[idx])
                {
                    Color32 c = pixels[idx];
                    int minC = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    if (minC > 210)
                    {
                        bool hasTransNeighbor = visited[idx - 1] || visited[idx + 1] ||
                                                visited[idx - w] || visited[idx + w];
                        if (hasTransNeighbor)
                        {
                            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                            if (lum >= 250f)
                            {
                                pixels[idx].a = 0;
                            }
                            else
                            {
                                pixels[idx].a = (byte)Mathf.Clamp((250f - lum) * 7f, 0f, 255f);
                            }
                        }
                    }
                }
            }
        }

        // Tìm bounding box
        int minX = w, maxX = 0, minY = h, maxY = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 20)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        int cropW = Mathf.Max(1, maxX - minX + 1);
        int cropH = Mathf.Max(1, maxY - minY + 1);
        int maxDim = Mathf.Max(cropW, cropH);

        // Render vào Texture2D kích thước targetSize x targetSize
        var result = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
        Color32 clearCol = new Color32(0, 0, 0, 0);
        Color32[] resPixels = new Color32[targetSize * targetSize];
        for (int i = 0; i < resPixels.Length; i++) resPixels[i] = clearCol;

        float destScale = (targetSize * 0.90f) / maxDim;
        int destW = Mathf.RoundToInt(cropW * destScale);
        int destH = Mathf.RoundToInt(cropH * destScale);
        int startX = (targetSize - destW) / 2;
        int startY = (targetSize - destH) / 2;

        for (int dy = 0; dy < destH; dy++)
        {
            int sy = minY + Mathf.FloorToInt(dy / destScale);
            if (sy >= h) sy = h - 1;

            for (int dx = 0; dx < destW; dx++)
            {
                int sx = minX + Mathf.FloorToInt(dx / destScale);
                if (sx >= w) sx = w - 1;

                Color32 c = pixels[sy * w + sx];
                int targetIdx = (startY + dy) * targetSize + (startX + dx);
                if (targetIdx >= 0 && targetIdx < resPixels.Length)
                {
                    resPixels[targetIdx] = c;
                }
            }
        }

        result.SetPixels32(resPixels);
        result.Apply();
        return result;
    }

    // =========================================================================
    //  2. ĐỒNG BỘ ICON EXP TRONG ĐƠN HÀNG VỚI HUD
    // =========================================================================

    private static void SyncExpIconsWithHUD(StringBuilder sb)
    {
        // 1. Tải sprite ngôi sao HUD chuẩn (exp_0)
        string hudStarPath = "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/exp.png";
        var hudStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(hudStarPath);
        if (hudStarSprite == null)
        {
            sb.AppendLine($"  ⚠ Không tìm thấy sprite {hudStarPath}!");
            return;
        }

        sb.AppendLine($"  ✔ Đã lấy sprite ngôi sao HUD thật (exp_0): {hudStarPath}");

        // 2. Cập nhật PF_OrderTicket.prefab
        string ticketPrefabPath = "Assets/_Game/Prefab/ui/OrderBoard/PF_OrderTicket.prefab";
        if (File.Exists(ticketPrefabPath))
        {
            var ticketRoot = PrefabUtility.LoadPrefabContents(ticketPrefabPath);
            int replacedCount = 0;

            var images = ticketRoot.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                // Thay thế các icon trong Row_Exp
                if (img.gameObject.name == "IMG_Icon" && img.transform.parent != null && img.transform.parent.name == "Row_Exp")
                {
                    img.sprite = hudStarSprite;
                    img.color = Color.white; // Bỏ tint xanh cũ (#3B82D9)
                    replacedCount++;
                }
                else if (img.sprite != null && img.sprite.name.StartsWith("ob_star"))
                {
                    img.sprite = hudStarSprite;
                    img.color = Color.white;
                    replacedCount++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(ticketRoot, ticketPrefabPath);
            PrefabUtility.UnloadPrefabContents(ticketRoot);
            sb.AppendLine($"  ✔ Đã cập nhật PF_OrderTicket.prefab: đổi {replacedCount} icon EXP thành HUD Level Star (màu trắng thuần).");
        }

        // 3. Cập nhật Canvas_OrderBoardPopup.prefab
        string popupPrefabPath = "Assets/_Game/Prefab/ui/OrderBoard/Canvas_OrderBoardPopup.prefab";
        if (File.Exists(popupPrefabPath))
        {
            var popupRoot = PrefabUtility.LoadPrefabContents(popupPrefabPath);
            int replacedCount = 0;

            var images = popupRoot.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.sprite != null && img.sprite.name.StartsWith("ob_star"))
                {
                    img.sprite = hudStarSprite;
                    img.color = Color.white; // Bỏ tint xanh cũ (#7FB5F0)
                    replacedCount++;
                }
                else if (img.gameObject.name == "IMG_Icon" && img.transform.parent != null && img.transform.parent.name == "Row_Exp")
                {
                    img.sprite = hudStarSprite;
                    img.color = Color.white;
                    replacedCount++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(popupRoot, popupPrefabPath);
            PrefabUtility.UnloadPrefabContents(popupRoot);
            sb.AppendLine($"  ✔ Đã cập nhật Canvas_OrderBoardPopup.prefab: đổi {replacedCount} icon EXP thành HUD Level Star.");
        }
    }

    // =========================================================================
    //  3. ĐỒNG BỘ SHOP TRONG SCENE
    // =========================================================================

    private static void SyncSceneShop(StringBuilder sb)
    {
        var shop = UnityEngine.Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shop == null)
        {
            sb.AppendLine("  (Không tìm thấy ShopManager trong scene đang mở - bỏ qua)");
            return;
        }

        Undo.RecordObject(shop, "Cập nhật Shop 4 Chuồng");

        // Đảm bảo 4 chuồng có trong buildingList
        var pens = new[]
        {
            AssetDatabase.LoadAssetAtPath<BaseItemData>("Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Gà.asset"),
            AssetDatabase.LoadAssetAtPath<BaseItemData>("Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Bò.asset"),
            AssetDatabase.LoadAssetAtPath<BaseItemData>("Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Bò Sữa.asset"),
            AssetDatabase.LoadAssetAtPath<BaseItemData>("Assets/_Game/Farm/CÔNG TRÌNH/DataShop/Buiding/Chuồng Heo.asset")
        };

        if (shop.buildingList == null) shop.buildingList = new List<BaseItemData>();

        // Lọc bỏ null và máy sản xuất (nếu còn)
        var cleanList = new List<BaseItemData>();
        foreach (var item in shop.buildingList)
        {
            if (item == null) continue;
            string id = item.itemID;
            if (id == "120" || id == "121" || id == "122") continue; // 3 máy
            cleanList.Add(item);
        }

        // Bổ sung 4 chuồng nếu thiếu
        foreach (var pen in pens)
        {
            if (pen != null && !cleanList.Contains(pen))
            {
                cleanList.Add(pen);
                sb.AppendLine($"  + Đã thêm {pen.itemName} vào buildingList của Shop.");
            }
        }

        shop.buildingList = cleanList;
        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        EditorSceneManager.SaveScene(shop.gameObject.scene);

        sb.AppendLine("  ✔ Đã lưu scene SCN_Farm với dữ liệu Shop mới nhất.");
    }
}
#endif
