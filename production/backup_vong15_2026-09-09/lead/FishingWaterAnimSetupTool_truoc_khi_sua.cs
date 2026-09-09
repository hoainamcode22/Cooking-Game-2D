using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FarmGame.Fishing
{
    /// <summary>
    /// TOOL 10 — NƯỚC CHẠY ĐỘNG cho SCN_Fishing. CHỦ FILE: Lead.
    ///
    /// Sếp báo 09/09: ô nước đang vẽ trên lớp đá nên "lộ phần đá", và nước đứng im.
    /// Nguyên nhân: Grid_Iso45 chép từ farm KHÔNG có tilemap nước (farm không có hồ), nên nước
    /// buộc phải vẽ chung lớp với đá/vách.
    ///
    /// Tool này làm 4 việc:
    ///   1. Đặt import setting cho 4 sheet nước động ở Assets/maptitle/Map45Iso/WaterAnim/
    ///      (Multiple · PPU 128 · pivot custom (0.5, 0.6) — khớp y hệt Sheet_IsoWater45 của Sếp),
    ///      cắt mỗi sheet thành 8 frame 128x80.
    ///   2. Tạo 4 AnimatedTile (com.unity.2d.tilemap.extras) ở Assets/maptitle/Map45Iso/WaterAnim/Tiles/.
    ///   3. Tạo tilemap RIÊNG "Tilemap_IsoWaterAnim" dưới Grid_Iso45, order THẤP HƠN đá/vách,
    ///      để nước nằm dưới và không còn cảnh vá nước lên mặt đá.
    ///   4. Thêm 4 tile vào Palette_Iso45 để Sếp bấm là vẽ được.
    ///
    /// Idempotent: chạy lại chỉ bổ sung thứ còn thiếu, không đè tile Sếp đã vẽ, không tự lưu SCN_Farm.
    /// </summary>
    public static class FishingWaterAnimSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuWater = MenuRoot + "10. Nước chạy động (AnimatedTile) + lớp nước riêng";
        private const string UndoLabel = "Hồ Câu — Nước động";

        private const string WaterFolder = "Assets/maptitle/Map45Iso/WaterAnim";
        private const string TileFolder = WaterFolder + "/Tiles";
        private const string PalettePath = "Assets/maptitle/Map45Iso/Palette_Iso45.prefab";
        private const string GridIsoName = "Grid_Iso45";
        private const string WaterTilemapName = "Tilemap_IsoWaterAnim";

        private const int FrameCount = 8;
        private const int FrameW = 128;
        private const int FrameH = 80;
        private const float PixelsPerUnit = 128f;
        private static readonly Vector2 SpritePivot = new Vector2(0.5f, 0.6f);   // đo từ Sheet_IsoWater45.png.meta

        /// <summary>Nước phải nằm DƯỚI mọi lớp đất/đá/vách (Cliff order 5, Elev 10/20/30).</summary>
        private const int WaterSortingOrder = 1;

        /// <summary>Tốc độ mặc định: 8 frame / 1,4 giây ≈ 5,7 fps — sóng trôi chậm, không rối mắt.</summary>
        private const float AnimFps = 5.7f;

        private static readonly string[] SheetNames =
        {
            "Sheet_WaterAnim45_fill",     // giữa hồ, thoi đầy
            "Sheet_WaterAnim45_fill_b",   // biến thể sóng khác, xen kẽ cho đỡ lặp mắt
            "Sheet_WaterAnim45_deep",     // nước sâu, tông đậm hơn
            "Sheet_WaterAnim45_edge",     // mép nước lởm chởm (viền hồ)
        };

        [MenuItem(MenuWater, false, 82)]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Hồ Câu", "Thoát Play Mode rồi chạy tool.", "OK"); return; }

            if (!AssetDatabase.IsValidFolder(WaterFolder))
            {
                EditorUtility.DisplayDialog("Hồ Câu — Nước động",
                    "Chưa có thư mục " + WaterFolder + ".\n\n" +
                    "Cần 4 file sheet (mỗi file 1024x80 = 8 frame 128x80):\n" +
                    "  Sheet_WaterAnim45_fill.png\n  Sheet_WaterAnim45_fill_b.png\n" +
                    "  Sheet_WaterAnim45_deep.png\n  Sheet_WaterAnim45_edge.png", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("10. Nước chạy động",
                "Tool sẽ:\n" +
                "1. Cắt 4 sheet nước thành 8 frame mỗi sheet (PPU 128, pivot 0.5/0.6 — khớp tile iso của Sếp)\n" +
                "2. Tạo 4 AnimatedTile ở " + TileFolder + "\n" +
                "3. Tạo lớp " + WaterTilemapName + " dưới " + GridIsoName + " (order " + WaterSortingOrder + ", NẰM DƯỚI đá và vách)\n" +
                "4. Thêm 4 tile vào Palette_Iso45\n\n" +
                "Không xoá tile Sếp đã vẽ. Scene SCN_Fishing sẽ được lưu.", "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var thieu = new List<string>();
            report.AppendLine("── TOOL 10: NƯỚC CHẠY ĐỘNG ──");

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            var tiles = new List<TileBase>();
            try
            {
                // ── Bước 1 + 2: importer + AnimatedTile ──
                EnsureFolder(TileFolder);
                for (int i = 0; i < SheetNames.Length; i++)
                {
                    string png = WaterFolder + "/" + SheetNames[i] + ".png";
                    if (!File.Exists(png))
                    {
                        report.AppendLine("✖ Thiếu " + png);
                        thieu.Add("Thiếu file " + SheetNames[i] + ".png");
                        continue;
                    }
                    if (!SliceSheet(png, report)) { thieu.Add("Cắt sheet lỗi: " + SheetNames[i]); continue; }
                }
                AssetDatabase.Refresh();   // PHẢI Refresh trước khi LoadAllAssetsAtPath lấy sprite con

                for (int i = 0; i < SheetNames.Length; i++)
                {
                    string png = WaterFolder + "/" + SheetNames[i] + ".png";
                    if (!File.Exists(png)) { continue; }
                    TileBase t = BuildAnimatedTile(png, SheetNames[i], report, thieu);
                    if (t != null) { tiles.Add(t); }
                }
                AssetDatabase.SaveAssets();

                // ── Bước 3: lớp nước riêng trong scene ──
                BuildWaterLayer(report, thieu);

                // ── Bước 4: nạp vào palette ──
                AddToPalette(tiles, report, thieu);
            }
            catch (Exception e)
            {
                report.AppendLine("✖ LỖI NGOÀI DỰ KIẾN: " + e.Message);
                Debug.LogException(e);
            }
            finally
            {
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            var head = new StringBuilder();
            head.AppendLine(thieu.Count == 0
                ? "✔ Xong — " + tiles.Count.ToString(CultureInfo.InvariantCulture) + " tile nước động đã sẵn sàng."
                : "⚠ Xong nhưng còn thiếu:");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            head.AppendLine();
            head.AppendLine("SẾP LÀM:");
            head.AppendLine("1) Chọn lớp " + GridIsoName + "/" + WaterTilemapName + " rồi vẽ nước (Tile Palette → Palette_Iso45, 4 ô nước ở cuối).");
            head.AppendLine("2) Xoá ô nước cũ đang nằm trên lớp đá: chọn lớp đá đó, dùng Eraser xoá mấy ô vũng nước.");
            head.AppendLine("3) Nước chỉ chạy khi bấm Play (Scene view đứng yên là bình thường).");
            head.AppendLine("4) Ctrl+S.");
            Debug.Log("[FishingSetup] Tool 10 — nước động:\n" + report);
            EditorUtility.DisplayDialog("Hồ Câu — Nước động", head.ToString(), "OK");
        }

        // ─────────────────────────────────────────────────────────────────
        //  1. Importer: Multiple + 8 frame + pivot khớp tile iso
        // ─────────────────────────────────────────────────────────────────

        private static bool SliceSheet(string png, StringBuilder report)
        {
            var importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null) { report.AppendLine("✖ Không đọc được importer: " + png); return false; }

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 2048);

            // BẪY (DecorStageArtTool:589): ts đọc TRƯỚC khi sửa importer nên còn giữ giá trị cũ,
            // SetTextureSettings sẽ ghi đè ngược lại textureType/spriteMode → phải đặt lại tường minh.
            ts.textureType = TextureImporterType.Sprite;
            ts.spriteMode = (int)SpriteImportMode.Multiple;
            ts.spriteAlignment = (int)SpriteAlignment.Custom;
            ts.spritePivot = SpritePivot;
            ts.spriteMeshType = SpriteMeshType.Tight;
            ts.spriteExtrude = 1;
            ts.spritePixelsPerUnit = PixelsPerUnit;
            importer.SetTextureSettings(ts);

            string baseName = Path.GetFileNameWithoutExtension(png);
            var rects = new List<SpriteRect>();
            for (int i = 0; i < FrameCount; i++)
            {
                rects.Add(new SpriteRect
                {
                    name = baseName + "_" + i.ToString("00", CultureInfo.InvariantCulture),
                    rect = new Rect(i * FrameW, 0f, FrameW, FrameH),
                    alignment = SpriteAlignment.Custom,
                    pivot = SpritePivot,
                    border = Vector4.zero,
                    spriteID = GUID.Generate(),
                });
            }

            // Unity 6: SpriteRect đi qua SpriteDataProvider, KHÔNG dùng importer.spritesheet (đã lỗi thời và bị bỏ qua).
            var factory = new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) { report.AppendLine("✖ Không lấy được SpriteDataProvider: " + png); return false; }
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();

            importer.SaveAndReimport();
            report.AppendLine("✓ Cắt " + baseName + ": " + FrameCount.ToString(CultureInfo.InvariantCulture) + " frame " + FrameW + "x" + FrameH + ", pivot (0.5, 0.6), PPU 128");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  2. AnimatedTile
        // ─────────────────────────────────────────────────────────────────

        private static TileBase BuildAnimatedTile(string png, string baseName, StringBuilder report, List<string> thieu)
        {
            var sprites = new List<Sprite>();
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(png);
            for (int i = 0; i < all.Length; i++)
            {
                var sp = all[i] as Sprite;
                if (sp != null) { sprites.Add(sp); }
            }
            if (sprites.Count == 0)
            {
                report.AppendLine("✖ " + baseName + ": chưa thấy sprite con (import chưa xong?) — chạy lại tool lần nữa.");
                thieu.Add(baseName + ": chưa nạp được sprite, chạy lại menu 10");
                return null;
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            string tilePath = TileFolder + "/" + baseName.Replace("Sheet_", "Tile_") + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(tilePath);
            bool moi = tile == null;
            if (moi) { tile = ScriptableObject.CreateInstance<AnimatedTile>(); }
            else { Undo.RecordObject(tile, UndoLabel); }

            tile.m_AnimatedSprites = sprites.ToArray();
            tile.m_MinSpeed = AnimFps;
            tile.m_MaxSpeed = AnimFps;
            tile.m_AnimationStartTime = 0f;
            tile.m_TileColliderType = Tile.ColliderType.None;   // nước không chặn người chơi

            if (moi) { AssetDatabase.CreateAsset(tile, tilePath); }
            EditorUtility.SetDirty(tile);
            report.AppendLine((moi ? "+ TẠO " : "· cập nhật ") + tilePath + " (" + sprites.Count.ToString(CultureInfo.InvariantCulture) + " frame, " + AnimFps.ToString("0.#", CultureInfo.InvariantCulture) + " fps)");
            return tile;
        }

        // ─────────────────────────────────────────────────────────────────
        //  3. Lớp nước riêng trong SCN_Fishing
        // ─────────────────────────────────────────────────────────────────

        private static void BuildWaterLayer(StringBuilder report, List<string> thieu)
        {
            if (!File.Exists(FishingIds.FishingScenePath))
            {
                report.AppendLine("✖ Chưa có " + FishingIds.FishingScenePath + " — bỏ qua bước lớp nước.");
                thieu.Add("Chưa có scene SCN_Fishing (chạy ★ SETUP trước)");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != FishingIds.FishingScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    report.AppendLine("✖ Người dùng huỷ lưu scene đang mở — bỏ qua bước lớp nước.");
                    thieu.Add("Chưa tạo được lớp nước (huỷ lưu scene)");
                    return;
                }
                scene = EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
            }

            GameObject grid = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == GridIsoName) { grid = roots[i]; break; }
                if (roots[i] != null)
                {
                    Transform t = roots[i].transform.Find(GridIsoName);
                    if (t != null) { grid = t.gameObject; break; }
                }
            }
            if (grid == null)
            {
                report.AppendLine("✖ Không thấy " + GridIsoName + " trong scene — chạy menu 6 trước.");
                thieu.Add("Chưa có " + GridIsoName + " (chạy menu 6)");
                return;
            }

            Transform exist = grid.transform.Find(WaterTilemapName);
            GameObject go;
            if (exist == null)
            {
                go = new GameObject(WaterTilemapName);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(grid.transform, false);
                report.AppendLine("+ TẠO " + GridIsoName + "/" + WaterTilemapName);
            }
            else
            {
                go = exist.gameObject;
                report.AppendLine("· " + WaterTilemapName + " đã có — giữ nguyên tile đã vẽ.");
            }

            if (go.GetComponent<Tilemap>() == null) { Undo.AddComponent<Tilemap>(go); }
            var r = go.GetComponent<TilemapRenderer>();
            if (r == null)
            {
                r = Undo.AddComponent<TilemapRenderer>(go);
                r.mode = TilemapRenderer.Mode.Individual;
                r.sortingLayerName = TouristSortingLayers.ResolveOrOverride("Bottom", new[] { "Bottom", "Default" });
                r.sortingOrder = WaterSortingOrder;
                report.AppendLine("  ✓ TilemapRenderer: layer Bottom, order " + WaterSortingOrder.ToString(CultureInfo.InvariantCulture) + " (nằm DƯỚI đá/vách nên không lộ đá lên trên nước)");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene)) { report.AppendLine("✔ Đã lưu " + FishingIds.FishingScenePath); }
        }

        // ─────────────────────────────────────────────────────────────────
        //  4. Palette
        // ─────────────────────────────────────────────────────────────────

        private static void AddToPalette(List<TileBase> tiles, StringBuilder report, List<string> thieu)
        {
            if (tiles.Count == 0) { return; }
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (palette == null)
            {
                report.AppendLine("· Không thấy " + PalettePath + " — bỏ qua bước palette (Sếp kéo tile vào palette tay).");
                thieu.Add("Không thấy Palette_Iso45 — kéo 4 tile vào palette bằng tay");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                Tilemap map = root.GetComponentInChildren<Tilemap>(true);
                if (map == null) { report.AppendLine("✖ Palette không có Tilemap."); thieu.Add("Palette hỏng"); return; }

                // Đặt 4 ô nước vào hàng TRỐNG phía dưới cùng của palette, không đè ô đang có.
                BoundsInt b = map.cellBounds;
                int y = b.yMin - 2;
                int added = 0;
                for (int i = 0; i < tiles.Count; i++)
                {
                    var pos = new Vector3Int(b.xMin + i, y, 0);
                    if (map.GetTile(pos) != null) { continue; }
                    map.SetTile(pos, tiles[i]);
                    added++;
                }
                if (added > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
                    report.AppendLine("✓ Palette_Iso45: thêm " + added.ToString(CultureInfo.InvariantCulture) + " ô nước ở hàng y=" + y.ToString(CultureInfo.InvariantCulture) + " (dưới cùng).");
                }
                else { report.AppendLine("· Palette đã có sẵn ô nước ở chỗ đó — không thêm lại."); }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) { return; }
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) { EnsureFolder(parent); }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
