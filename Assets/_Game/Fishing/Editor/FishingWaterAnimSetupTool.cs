using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
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
    /// buộc phải vẽ chung lớp với đá — mà ô đó ở Sheet_IsoCliff45 hàng 5 vốn chỉ 35-49% nước,
    /// 42-53% đá. Sheet_IsoWater45 hàng 0/3/5 mới là nước 100%.
    ///
    /// Tool này làm 4 việc:
    ///   1. Đặt import setting cho 4 sheet nước động ở Assets/maptitle/Map45Iso/WaterAnim/
    ///      (Multiple · PPU 128 · pivot custom (0.5, 0.6) · extrude 1 · mesh Tight — SAO Y
    ///      Sheet_IsoWater45.png.meta của Sếp), cắt mỗi sheet thành 8 frame 128x80.
    ///   2. Tạo 4 AnimatedTile (com.unity.2d.tilemap.extras) ở Assets/maptitle/Map45Iso/WaterAnim/Tiles/.
    ///   3. Tạo tilemap RIÊNG "Tilemap_IsoWaterAnim" dưới Grid_Iso45, sorting layer "Default" order 3.
    ///   4. Thêm 4 tile vào Palette_Iso45 để Sếp bấm là vẽ được.
    ///
    /// Idempotent: chạy lại chỉ bổ sung/sửa thứ sai, không đè tile Sếp đã vẽ, không tự lưu SCN_Farm.
    /// </summary>
    public static class FishingWaterAnimSetupTool
    {
        private const string MenuRoot = "Tools/Farm Game/Hồ Câu/";
        private const string MenuWater = MenuRoot + "10. Nước chạy động (AnimatedTile) + lớp nước riêng";
        private const string UndoLabel = "Hồ Câu — Nước động";

        private const string WaterFolder = "Assets/maptitle/Map45Iso/WaterAnim";
        private const string TileFolder = WaterFolder + "/Tiles";
        private const string PalettePath = "Assets/maptitle/Map45Iso/Palette_Iso45.prefab";
        private const string WaterPalettePath = "Assets/maptitle/Map45Iso/Palette_Water45.prefab";
        private const string GridIsoName = "Grid_Iso45";
        private const string WaterTilemapName = "Tilemap_IsoWaterAnim";
        private const string GrassTilemapName = "Tilemap_IsoGrass";

        private const int FrameCount = 8;
        private const int FrameW = 128;
        private const int FrameH = 80;
        private const float PixelsPerUnit = 128f;

        /// <summary>
        /// ĐO THẬT từ Sheet_IsoWater45.png.meta của Sếp: mỗi sprite con rect 128x80,
        /// alignment 9 (Custom), spritePivot {x: 0.5, y: 0.6}, spriteExtrude 1, spriteMeshType 1 (Tight).
        /// Sheet nước động cũng là 128x80 nên dùng nguyên con số 0.6 là khớp lưới tuyệt đối.
        /// </summary>
        private static readonly Vector2 SpritePivot = new Vector2(0.5f, 0.6f);
        private const int SpriteExtrude = 1;

        /// <summary>
        /// [Lead vòng 15 — SỬA theo Reviewer] Bản đồ lưu ở "Default":
        /// GroundBase_Dirt (SpriteRenderer) order −10 · IsoGrass 1 · IsoDirt 2 · IsoRock 5
        /// (Map45SetupTool.cs hàm CreateIsoGridInScene); Cliff/Elev1-3 ở "Objects".
        /// • KHÔNG dùng layer "Bottom": Bottom(1161173501) ĐỨNG TRƯỚC Default ⇒ nước chui xuống
        ///   dưới cả nền dirt phủ kín map, Sếp sẽ không thấy gì.
        /// • Order 3 (không phải 0): mặt hồ là lớp PHỦ, phải đè lên cỏ 1 và đất 2 mà Sếp đã vẽ kín
        ///   khu hồ — nếu để 0 thì cỏ che mất nước và Sếp lại báo đúng bug cũ. Vẫn nằm dưới đá 5
        ///   nên mép đá/vách quanh hồ vẫn vẽ đè lên nước.
        /// </summary>
        private const string WaterSortingLayer = "Default";
        private const int WaterSortingOrder = 3;

        /// <summary>
        /// AnimatedTile.m_MinSpeed/m_MaxSpeed là HỆ SỐ NHÂN với Tilemap.animationFrameRate (mặc định 1),
        /// nên hai số này chính là fps. CỐ Ý ĐẶT BẰNG NHAU: 8 frame của sheet được vẽ bằng sóng có
        /// chu kỳ đúng bằng ô lưới iso, nên khi MỌI Ô CÙNG MỘT FRAME thì cả hồ là một mặt nước liền
        /// mạch, sóng chạy xuyên qua ranh giới ô. Nếu để min != max thì AnimatedTile bốc
        /// Random.Range(min, max) cho TỪNG Ô, mỗi ô trôi một tốc độ ⇒ lộ đường ghép giữa các ô.
        /// Muốn hồ đỡ đơn điệu thì xen kẽ 4 loại tile (fill / fill_b / deep / edge), đừng lệch tốc độ.
        /// 8 frame / 1,4 giây ≈ 5,7 fps.
        /// </summary>
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
                "3. Tạo lớp " + WaterTilemapName + " dưới " + GridIsoName + " + VÙNG CÂU tự động theo ô nước (tắt Zone_01 mẫu khi đã có nước)" +
                    " (sorting layer \"" + WaterSortingLayer + "\" order " + WaterSortingOrder.ToString(CultureInfo.InvariantCulture) +
                    " — ĐÈ LÊN cỏ/đất, nằm DƯỚI đá/vách)\n" +
                "4. Thêm 4 tile vào Palette_Iso45\n\n" +
                "Tool KHÔNG xoá tile Sếp đã vẽ và KHÔNG đụng SCN_Farm. Scene SCN_Fishing sẽ được lưu.",
                "Chạy", "Huỷ"))
            { return; }

            var report = new StringBuilder();
            var thieu = new List<string>();
            report.AppendLine("── TOOL 10: NƯỚC CHẠY ĐỘNG ──");

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(UndoLabel);
            var tiles = new List<TileBase>();
            var catDuoc = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                // ── Bước 1: importer ──
                EnsureFolder(TileFolder);
                for (int i = 0; i < SheetNames.Length; i++)
                {
                    string png = WaterFolder + "/" + SheetNames[i] + ".png";
                    EditorUtility.DisplayProgressBar("Hồ Câu — Nước động", "Cắt " + SheetNames[i], 0.1f + 0.4f * i / SheetNames.Length);
                    if (!File.Exists(png))
                    {
                        report.AppendLine("✖ Thiếu " + png);
                        thieu.Add("Thiếu file " + SheetNames[i] + ".png");
                        continue;
                    }
                    if (SliceSheet(png, report)) { catDuoc.Add(SheetNames[i]); }
                    else { thieu.Add("Cắt sheet lỗi: " + SheetNames[i]); }
                }
                AssetDatabase.Refresh();   // PHẢI Refresh trước khi LoadAllAssetsAtPath lấy sprite con

                // ── Bước 2: AnimatedTile ──
                EditorUtility.DisplayProgressBar("Hồ Câu — Nước động", "Tạo AnimatedTile…", 0.55f);
                for (int i = 0; i < SheetNames.Length; i++)
                {
                    // Sheet cắt hỏng thì KHÔNG dựng tile: LoadAllAssetsAtPath sẽ nhặt sprite cũ còn sót
                    // (sai tên, sai số lượng, sai thứ tự) rồi nhồi vào tile mà báo cáo vẫn xanh.
                    if (!catDuoc.Contains(SheetNames[i])) { continue; }
                    string png = WaterFolder + "/" + SheetNames[i] + ".png";
                    TileBase t = BuildAnimatedTile(png, SheetNames[i], report, thieu);
                    if (t != null) { tiles.Add(t); }
                }
                AssetDatabase.SaveAssets();

                // ── Bước 3: palette ──
                // [Lead vòng 15b] LÀM PALETTE TRƯỚC, SCENE SAU. Lần chạy 09/09 của Sếp dừng ngay sau
                // bước scene: tile + lớp nước có, palette không có ô nào. Bước scene có thể mở lại
                // scene (OpenScene Single) và cắt ngang phần còn lại, nên mọi việc trên ASSET phải
                // xong trước khi đụng tới scene.
                EditorUtility.DisplayProgressBar("Hồ Câu — Nước động", "Nạp tile vào palette…", 0.72f);
                bool vaoPaletteChung = AddToPalette(tiles, report, thieu);
                if (!vaoPaletteChung) { BuildWaterPalette(tiles, report, thieu); }

                // ── Bước 4: lớp nước riêng trong scene ──
                EditorUtility.DisplayProgressBar("Hồ Câu — Nước động", "Tạo lớp nước trong scene…", 0.9f);
                BuildWaterLayer(report, thieu);
            }
            catch (Exception e)
            {
                report.AppendLine("✖ LỖI NGOÀI DỰ KIẾN: " + e.Message);
                thieu.Add("Lỗi ngoài dự kiến — xem Console");
                Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            }

            var head = new StringBuilder();
            head.AppendLine(thieu.Count == 0
                ? "✔ Xong — " + tiles.Count.ToString(CultureInfo.InvariantCulture) + " tile nước động đã sẵn sàng."
                : "⚠ Xong nhưng còn thiếu:");
            for (int i = 0; i < thieu.Count; i++) { head.AppendLine("• " + thieu[i]); }
            head.AppendLine();
            head.AppendLine("SẾP LÀM:");
            head.AppendLine("1) Chọn lớp " + GridIsoName + "/" + WaterTilemapName + " ở Hierarchy → Window > 2D > Tile Palette → ô chọn palette lấy Palette_Iso45 (4 ô nước nằm CHÉO phía dưới-trái vì palette là lưới iso). Nếu không thấy thì đổi sang Palette_Water45 — 4 ô nằm thẳng hàng ngang.");
            head.AppendLine("2) Xoá ô nước cũ đang nằm trên lớp đá: chọn " + GridIsoName + "/Tilemap_IsoRock, dùng Eraser xoá mấy ô vũng nước. Cỏ/đất bên dưới KHÔNG cần xoá — nước order " + WaterSortingOrder.ToString(CultureInfo.InvariantCulture) + " đã đè lên rồi.");
            head.AppendLine("3) Nước chỉ chạy khi bấm Play (Scene view đứng yên là bình thường). Vẽ nước ở đâu là câu được ở đó — chạy lại menu 10 sau khi vẽ xong để cập nhật vùng câu (Zone_01 mẫu sẽ tự tắt).");
            head.AppendLine("4) Ctrl+Z chỉ hoàn tác được lớp nước trong scene — sprite đã cắt, AnimatedTile và palette đã ghi thẳng ra đĩa.");
            Debug.Log("[FishingSetup] Tool 10 — nước động:\n" + report);
            EditorUtility.DisplayDialog("Hồ Câu — Nước động", head.ToString(), "OK");
        }

        // ─────────────────────────────────────────────────────────────────
        //  1. Importer: Multiple + 8 frame + pivot khớp tile iso
        // ─────────────────────────────────────────────────────────────────

        private static bool SliceSheet(string png, StringBuilder report)
        {
            var importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null)
            {
                // PNG vừa chép vào Assets/ mà chưa có .meta thì GetAtPath trả null — ép import rồi thử lại.
                AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(png) as TextureImporter;
            }
            if (importer == null) { report.AppendLine("✖ Không đọc được importer: " + png); return false; }

            // [Lead vòng 15 — SỬA theo Reviewer] ĐỌC KÍCH THƯỚC THẬT, không tin hằng số.
            // Sheet sai cỡ mà vẫn cắt theo 8x128x80 thì Unity không ném lỗi, nó lặng lẽ cho ra
            // sprite rỗng/méo còn báo cáo vẫn xanh.
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            if (tex == null) { report.AppendLine("✖ Không nạp được texture: " + png); return false; }
            if (tex.width < FrameCount * FrameW || tex.height < FrameH)
            {
                report.AppendLine("✖ " + png + ": " + tex.width.ToString(CultureInfo.InvariantCulture) + "x" +
                    tex.height.ToString(CultureInfo.InvariantCulture) + ", cần tối thiểu " +
                    (FrameCount * FrameW).ToString(CultureInfo.InvariantCulture) + "x" +
                    FrameH.ToString(CultureInfo.InvariantCulture) + " — bỏ qua, không cắt.");
                return false;
            }

            // [Lead vòng 15 — SỬA theo Reviewer] THỨ TỰ SỐNG CÒN (chép chuẩn CharacterSheetSliceTool):
            // sửa importer TRƯỚC → mới ReadTextureSettings → chỉ sửa phần riêng của sprite → SetTextureSettings.
            // Nếu đọc ts trước thì SetTextureSettings ghi đè NGƯỢC filterMode/mipmap/alpha/npot vừa đặt.
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            // maxTextureSize phải >= cạnh lớn nhất, không thì Unity co ảnh và rect cắt bị lệch.
            importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(Mathf.Max(tex.width, tex.height)));

            var ts = new TextureImporterSettings();
            importer.ReadTextureSettings(ts);
            ts.spriteMode = (int)SpriteImportMode.Multiple;
            ts.spriteAlignment = (int)SpriteAlignment.Custom;
            ts.spritePivot = SpritePivot;
            ts.spriteMeshType = SpriteMeshType.Tight;
            ts.spriteExtrude = SpriteExtrude;
            ts.spritePixelsPerUnit = PixelsPerUnit;
            importer.SetTextureSettings(ts);
            importer.SaveAndReimport();

            // Lấy LẠI importer sau reimport: provider gắn vào instance cũ thì reimport nuốt mất SpriteRect.
            importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null) { report.AppendLine("✖ Importer biến mất sau reimport: " + png); return false; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                report.AppendLine("✖ Thiếu package '2D Sprite' (com.unity.2d.sprite) — không lấy được ISpriteEditorDataProvider: " + png);
                return false;
            }
            provider.InitSpriteEditorDataProvider();

            // [Lead vòng 15 — SỬA theo Reviewer] GIỮ NGUYÊN spriteID theo tên. GUID.Generate() vô điều kiện
            // làm mọi tham chiếu Sprite (AnimatedTile, prefab, Animation clip) đứt mỗi lần chạy lại tool.
            var oldIds = new Dictionary<string, GUID>(StringComparer.Ordinal);
            SpriteRect[] existing = provider.GetSpriteRects();
            if (existing != null)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i] == null || string.IsNullOrEmpty(existing[i].name)) { continue; }
                    if (!oldIds.ContainsKey(existing[i].name)) { oldIds[existing[i].name] = existing[i].spriteID; }
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(png);
            var rects = new List<SpriteRect>(FrameCount);
            var pairs = new List<SpriteNameFileIdPair>(FrameCount);
            int giuLai = 0;
            for (int i = 0; i < FrameCount; i++)
            {
                string spriteName = baseName + "_" + i.ToString("00", CultureInfo.InvariantCulture);
                GUID id;
                if (!oldIds.TryGetValue(spriteName, out id) || id.Empty()) { id = GUID.Generate(); }
                else { giuLai++; }
                rects.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(i * FrameW, 0f, FrameW, FrameH),
                    alignment = SpriteAlignment.Custom,
                    pivot = SpritePivot,
                    border = Vector4.zero,
                    spriteID = id,
                });
                pairs.Add(new SpriteNameFileIdPair(spriteName, id));
            }

            // Unity 6: SpriteRect đi qua SpriteDataProvider, KHÔNG dùng importer.spritesheet (đã lỗi thời).
            // Phải đặt CẢ bảng tên↔fileID, không thì lần cắt sau internalID đổi ⇒ AnimatedTile thành Missing.
            provider.SetSpriteRects(rects.ToArray());
            var nameProv = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProv != null) { nameProv.SetNameFileIdPairs(pairs); }
            provider.Apply();

            var applied = provider.targetObject as AssetImporter;
            if (applied != null) { applied.SaveAndReimport(); }
            else { importer.SaveAndReimport(); }

            report.AppendLine("✓ Cắt " + baseName + ": " + FrameCount.ToString(CultureInfo.InvariantCulture) + " frame " +
                FrameW.ToString(CultureInfo.InvariantCulture) + "x" + FrameH.ToString(CultureInfo.InvariantCulture) +
                " (ảnh " + tex.width.ToString(CultureInfo.InvariantCulture) + "x" + tex.height.ToString(CultureInfo.InvariantCulture) +
                "), pivot (0.5, 0.6), PPU 128, giữ lại " + giuLai.ToString(CultureInfo.InvariantCulture) + " spriteID cũ");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  2. AnimatedTile
        // ─────────────────────────────────────────────────────────────────

        private static TileBase BuildAnimatedTile(string png, string baseName, StringBuilder report, List<string> thieu)
        {
            var sprites = new List<Sprite>();
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(png);   // Object phải ghi rõ: có cả using System
            for (int i = 0; i < all.Length; i++)
            {
                var sp = all[i] as Sprite;
                if (sp != null) { sprites.Add(sp); }
            }
            if (sprites.Count != FrameCount)
            {
                report.AppendLine("✖ " + baseName + ": có " + sprites.Count.ToString(CultureInfo.InvariantCulture) +
                    " sprite con, cần " + FrameCount.ToString(CultureInfo.InvariantCulture) +
                    " — BỎ QUA, không ghi đè tile cũ. Chạy lại menu 10 lần nữa (import có thể chưa xong).");
                thieu.Add(baseName + ": số frame sai (" + sprites.Count.ToString(CultureInfo.InvariantCulture) + "/" + FrameCount.ToString(CultureInfo.InvariantCulture) + ")");
                return null;
            }
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            string tilePath = TileFolder + "/" + baseName.Replace("Sheet_", "Tile_") + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<AnimatedTile>(tilePath);
            bool moi = tile == null;
            if (moi)
            {
                tile = ScriptableObject.CreateInstance<AnimatedTile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            // [Lead vòng 15 — SỬA theo Reviewer] Gán qua SerializedObject: tên field của AnimatedTile
            // thuộc package com.unity.2d.tilemap.extras, đổi tên giữa các bản là thất bại MỀM (log cảnh báo)
            // chứ không phải lỗi biên dịch đỏ cả project.
            var so = new SerializedObject(tile);
            bool okSprites = SetSpriteArray(so, "m_AnimatedSprites", sprites);
            bool okSpeed = SetFloat(so, "m_MinSpeed", AnimFps) & SetFloat(so, "m_MaxSpeed", AnimFps);
            SetFloat(so, "m_AnimationStartTime", 0f);
            SetEnumInt(so, "m_TileColliderType", (int)Tile.ColliderType.Grid);   // [vòng 16] Grid = hình thoi ô iso → TilemapCollider2D (isTrigger) dựng VÙNG CÂU tự động theo ô nước; không chặn người chơi vì là trigger
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!okSprites || !okSpeed)
            {
                report.AppendLine("⚠ " + baseName + ": AnimatedTile đổi tên field (bản package khác) — mở asset gán tay 8 frame + Min/Max Speed " +
                    AnimFps.ToString("0.#", CultureInfo.InvariantCulture) + ".");
                thieu.Add(baseName + ": gán tay frame/Speed cho AnimatedTile");
            }

            EditorUtility.SetDirty(tile);
            report.AppendLine((moi ? "+ TẠO " : "· cập nhật ") + tilePath + " (" +
                sprites.Count.ToString(CultureInfo.InvariantCulture) + " frame, " +
                AnimFps.ToString("0.#", CultureInfo.InvariantCulture) + " fps, mọi ô chạy đồng bộ)");
            return tile;
        }

        private static bool SetSpriteArray(SerializedObject so, string field, List<Sprite> sprites)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null || !p.isArray) { return false; }
            p.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++) { p.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i]; }
            return true;
        }

        private static bool SetFloat(SerializedObject so, string field, float value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null || p.propertyType != SerializedPropertyType.Float) { return false; }
            p.floatValue = value;
            return true;
        }

        private static bool SetEnumInt(SerializedObject so, string field, int value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { return false; }
            if (p.propertyType != SerializedPropertyType.Enum && p.propertyType != SerializedPropertyType.Integer) { return false; }
            p.intValue = value;
            return true;
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
                EditorUtility.ClearProgressBar();   // hộp thoại lưu scene sắp hiện — tắt progress bar kẻo Editor trông như treo
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    report.AppendLine("✖ Người dùng huỷ lưu scene đang mở — bỏ qua bước lớp nước.");
                    thieu.Add("Chưa tạo được lớp nước (huỷ lưu scene)");
                    return;
                }
                scene = EditorSceneManager.OpenScene(FishingIds.FishingScenePath, OpenSceneMode.Single);
                EditorUtility.DisplayProgressBar("Hồ Câu — Nước động", "Tạo lớp nước trong scene…", 0.8f);
            }

            GameObject grid = FindInScene(scene, GridIsoName);
            if (grid == null)
            {
                report.AppendLine("✖ Không thấy " + GridIsoName + " trong scene — chạy menu 6 trước.");
                thieu.Add("Chưa có " + GridIsoName + " (chạy menu 6)");
                return;
            }

            bool doi = false;
            Transform exist = grid.transform.Find(WaterTilemapName);
            GameObject go;
            if (exist == null)
            {
                go = new GameObject(WaterTilemapName);
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                go.transform.SetParent(grid.transform, false);
                report.AppendLine("+ TẠO " + GridIsoName + "/" + WaterTilemapName);
                doi = true;
            }
            else
            {
                go = exist.gameObject;
                report.AppendLine("· " + WaterTilemapName + " đã có — giữ nguyên tile đã vẽ, chỉ soát lại sorting.");
            }

            var map = go.GetComponent<Tilemap>();
            if (map == null) { map = Undo.AddComponent<Tilemap>(go); doi = true; }

            // Chép tileAnchor/orientation của lớp cỏ để ô nước khớp lưới y hệt các lớp Sếp đang vẽ.
            Transform grassT = grid.transform.Find(GrassTilemapName);
            var grassMap = grassT != null ? grassT.GetComponent<Tilemap>() : null;
            if (grassMap != null && (map.tileAnchor != grassMap.tileAnchor || map.orientation != grassMap.orientation))
            {
                Undo.RecordObject(map, UndoLabel);
                map.tileAnchor = grassMap.tileAnchor;
                map.orientation = grassMap.orientation;
                doi = true;
                report.AppendLine("  ✓ tileAnchor " + map.tileAnchor.ToString("0.##", CultureInfo.InvariantCulture) + " chép từ " + GrassTilemapName);
            }
            // m_MinSpeed/m_MaxSpeed của AnimatedTile là HỆ SỐ NHÂN với animationFrameRate. Mặc định vốn là 1,
            // đặt lại ở đây chỉ để vá trường hợp ai đó chỉnh tay.
            if (!Mathf.Approximately(map.animationFrameRate, 1f))
            {
                Undo.RecordObject(map, UndoLabel);
                map.animationFrameRate = 1f;
                doi = true;
                report.AppendLine("  ✓ animationFrameRate về 1 (trước đó bị chỉnh tay)");
            }

            var r = go.GetComponent<TilemapRenderer>();
            if (r == null) { r = Undo.AddComponent<TilemapRenderer>(go); doi = true; }

            // [Lead vòng 15 — SỬA theo Reviewer] Soát LẠI mỗi lần chạy, không chỉ lúc mới tạo:
            // lớp nước đã tạo sai layer ở bản tool cũ thì chạy lại phải vá được.
            string layer = TouristSortingLayers.ResolveOrOverride(WaterSortingLayer, new[] { "Default" });
            if (r.mode != TilemapRenderer.Mode.Individual || r.sortingLayerName != layer || r.sortingOrder != WaterSortingOrder)
            {
                Undo.RecordObject(r, UndoLabel);
                r.mode = TilemapRenderer.Mode.Individual;
                r.sortingLayerName = layer;
                r.sortingOrder = WaterSortingOrder;
                doi = true;
                report.AppendLine("  ✓ TilemapRenderer: layer \"" + layer + "\" order " +
                    WaterSortingOrder.ToString(CultureInfo.InvariantCulture) +
                    " (ĐÈ LÊN IsoGrass 1 / IsoDirt 2, DƯỚI IsoRock 5, TRÊN GroundBase_Dirt −10)");
            }
            else { report.AppendLine("  · TilemapRenderer đã đúng layer/order — không sửa."); }

            // [Lead vòng 16] VÙNG CÂU TỰ ĐỘNG THEO Ô NƯỚC: Sếp vẽ hồ lệch khỏi gốc, Zone_01 mặc định (thoi 4x2 quanh (0,1.5)) nằm trên cỏ
            // → nút QUĂNG xám vì "xa vùng câu". FishingZone chỉ cần một Collider2D bất kỳ (OverlapPoint/ClosestPoint), nên gắn
            // TilemapCollider2D(trigger) + CompositeCollider2D(trigger, Polygons) + Rigidbody2D Static + FishingZone lên chính lớp nước:
            // vẽ nước ở đâu là câu được ở đó, không kéo tay gì nữa.
            if (BuildWaterZone(go, map, scene, report, thieu)) { doi = true; }

            if (!doi) { report.AppendLine("· Lớp nước không có gì phải đổi — KHÔNG lưu lại scene."); return; }

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(scene);
            if (EditorSceneManager.SaveScene(scene)) { report.AppendLine("✔ Đã lưu " + FishingIds.FishingScenePath); }
            else
            {
                report.AppendLine("✖ SaveScene thất bại — Sếp bấm Ctrl+S tay.");
                thieu.Add("Chưa lưu được SCN_Fishing — bấm Ctrl+S");
            }
        }


        /// <summary>Gắn collider trigger + FishingZone lên lớp nước; nếu lớp nước đã có tile thì tắt Zone_01 mẫu (không xoá). Trả true khi có thay đổi.</summary>
        private static bool BuildWaterZone(GameObject go, Tilemap map, Scene scene, StringBuilder report, List<string> thieu)
        {
            bool doi = false;
            var col = go.GetComponent<TilemapCollider2D>();
            if (col == null) { col = Undo.AddComponent<TilemapCollider2D>(go); doi = true; }
            if (!col.isTrigger) { Undo.RecordObject(col, UndoLabel); col.isTrigger = true; doi = true; }

            // Rigidbody2D phải có TRƯỚC CompositeCollider2D (RequireComponent sẽ tự thêm bản Dynamic nếu thiếu → nước "rơi").
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) { rb = Undo.AddComponent<Rigidbody2D>(go); rb.bodyType = RigidbodyType2D.Static; doi = true; }
            else if (rb.bodyType != RigidbodyType2D.Static) { Undo.RecordObject(rb, UndoLabel); rb.bodyType = RigidbodyType2D.Static; doi = true; }

            var comp = go.GetComponent<CompositeCollider2D>();
            if (comp == null)
            {
                comp = Undo.AddComponent<CompositeCollider2D>(go);
                comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
                doi = true;
            }
            if (!comp.isTrigger) { Undo.RecordObject(comp, UndoLabel); comp.isTrigger = true; doi = true; }

            // "Used By Composite" đổi tên qua các bản Unity (m_UsedByComposite → m_CompositeOperation) → ghi qua SerializedObject.
            var soCol = new SerializedObject(col);
            SerializedProperty pOp = soCol.FindProperty("m_CompositeOperation");
            if (pOp != null) { if (pOp.intValue != 1) { pOp.intValue = 1; soCol.ApplyModifiedProperties(); doi = true; } }
            else
            {
                SerializedProperty pOld = soCol.FindProperty("m_UsedByComposite");
                if (pOld != null) { if (!pOld.boolValue) { pOld.boolValue = true; soCol.ApplyModifiedProperties(); doi = true; } }
                else { report.AppendLine("  ⚠ Không đặt được 'Used By Composite' cho lớp nước — Sếp tick tay trên TilemapCollider2D."); thieu.Add("Tick 'Used By Composite' trên " + WaterTilemapName); }
            }

            var zone = go.GetComponent<FishingZone>();
            if (zone == null)
            {
                zone = Undo.AddComponent<FishingZone>(go);
                doi = true;
            }
            // FishingZone tự lấy Collider2D trên cùng object; GetComponent<Collider2D> có thể trả TilemapCollider2D (đã gộp vào composite
            // → không có hình riêng). Ghi tường minh CompositeCollider2D qua SerializedObject (field private zoneCollider).
            var soZone = new SerializedObject(zone);
            SerializedProperty pZc = soZone.FindProperty("zoneCollider");
            if (pZc != null && pZc.objectReferenceValue != comp) { pZc.objectReferenceValue = comp; soZone.ApplyModifiedProperties(); doi = true; }

            map.RefreshAllTiles();   // [Reviewer L1] tile data cũ còn ColliderType None → làm mới để TilemapCollider2D dựng hình ngay, không chờ reload scene
            map.CompressBounds();
            int soO = map.GetUsedTilesCount();
            report.AppendLine("  ✓ Vùng câu tự động: TilemapCollider2D(trigger) + Composite + FishingZone trên " + WaterTilemapName +
                " — " + soO.ToString(CultureInfo.InvariantCulture) + " ô nước đang vẽ" + (soO == 0 ? " (chưa vẽ → chưa câu được ở đâu; Zone_01 mẫu giữ bật)" : string.Empty));

            // Có ô nước thật rồi → tắt Zone_01 mẫu để không câu được trên cỏ. Không xoá, Sếp bật lại được.
            if (soO > 0)
            {
                Transform zonesRoot = null;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++) { if (roots[i] != null && roots[i].name == FishingIds.FishingZonesRoot) { zonesRoot = roots[i].transform; break; } }
                Transform z1 = zonesRoot != null ? zonesRoot.Find("Zone_01") : null;
                if (z1 != null && z1.gameObject.activeSelf)
                {
                    Undo.RecordObject(z1.gameObject, UndoLabel);
                    z1.gameObject.SetActive(false);
                    doi = true;
                    report.AppendLine("  ✓ Tắt FishingZones/Zone_01 mẫu (thoi 4x2 quanh (0,1.5), không trùng hồ Sếp vẽ) — vùng câu giờ là chính ô nước.");
                }
            }
            return doi;
        }

        /// <summary>Tìm theo tên trong toàn scene (kể cả nằm sâu), không chỉ 1 cấp như Transform.Find.</summary>
        private static GameObject FindInScene(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null) { continue; }
                if (roots[i].name == name) { return roots[i]; }
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j] != null && all[j].name == name) { return all[j].gameObject; }
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  4. Palette
        // ─────────────────────────────────────────────────────────────────

        private static bool AddToPalette(List<TileBase> tiles, StringBuilder report, List<string> thieu)
        {
            if (tiles.Count == 0) { return false; }
            var paletteAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (paletteAsset == null)
            {
                report.AppendLine("· Không thấy " + PalettePath + " — sẽ dựng palette nước riêng thay thế.");
                return false;
            }

            // [Lead vòng 15 — SỬA theo Reviewer] Palette_Iso45.prefab có sub-asset GridPalette
            // (Map45SetupTool tạo bằng AddObjectToAsset). SaveAsPrefabAsset ghi đè file prefab và có thể
            // cuốn phăng sub-asset đó ⇒ Tile Palette window không mở được palette nữa.
            // Nhớ setting trước, kiểm tra sau, thiếu thì dựng lại. Dò bằng LoadAllAssetsAtPath —
            // LoadAllAssetRepresentationsAtPath không chắc trả về ScriptableObject gắn kèm prefab.
            GridPalette cu = TimGridPalette(PalettePath);
            bool hadGridPalette = cu != null;
            // Chụp NGUYÊN VĂN mọi field bằng EditorJsonUtility thay vì chép tay cellSizing /
            // transparencySortMode / transparencySortAxis: không gọi tên field nào nên bản Unity sau
            // có thêm field cũng chép đủ, và không có nguy cơ lỗi biên dịch vì tên field đổi.
            string gridPaletteJson = hadGridPalette ? EditorJsonUtility.ToJson(cu) : string.Empty;

            GameObject root = PrefabUtility.LoadPrefabContents(PalettePath);
            bool saved = false;
            try
            {
                Tilemap map = root.GetComponentInChildren<Tilemap>(true);
                if (map == null) { report.AppendLine("✖ Palette_Iso45 không có Tilemap — dựng palette nước riêng thay thế."); return false; }

                // [Lead vòng 15 — SỬA theo Reviewer] Idempotent theo THAM CHIẾU TILE, không theo toạ độ:
                // dò theo yMin thì lần chạy sau CompressBounds tính cả hàng vừa thêm ⇒ hàng mới lại rỗng
                // ⇒ palette mọc thêm 4 ô nước mỗi lần chạy, phình vô hạn.
                map.CompressBounds();
                var daCo = new HashSet<TileBase>();
                foreach (Vector3Int p in map.cellBounds.allPositionsWithin)
                {
                    TileBase t = map.GetTile(p);
                    if (t != null) { daCo.Add(t); }
                }
                var canThem = new List<TileBase>();
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (!daCo.Contains(tiles[i])) { canThem.Add(tiles[i]); }
                }
                if (canThem.Count == 0)
                {
                    report.AppendLine("· Palette_Iso45 đã có đủ " + tiles.Count.ToString(CultureInfo.InvariantCulture) + " ô nước — không thêm lại.");
                    return true;
                }

                BoundsInt b = map.cellBounds;
                int y = b.yMin - 2;
                int x = b.xMin;
                for (int i = 0; i < canThem.Count; i++)
                {
                    while (map.GetTile(new Vector3Int(x, y, 0)) != null) { x++; }
                    map.SetTile(new Vector3Int(x, y, 0), canThem[i]);
                    x++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
                saved = true;
                report.AppendLine("✓ Palette_Iso45: thêm " + canThem.Count.ToString(CultureInfo.InvariantCulture) +
                    " ô nước ở hàng y=" + y.ToString(CultureInfo.InvariantCulture) + " (chéo phía dưới-trái vì palette là lưới iso).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (!saved) { return false; }

            // Kiểm THẬT: đọc lại prefab từ đĩa xem 4 tile có nằm trong đó không.
            // Chạy 09/09 SaveAsPrefabAsset báo êm mà palette vẫn trống, nên không tin kết quả trả về.
            if (!PaletteChuaDuTile(tiles))
            {
                report.AppendLine("⚠ Ghi Palette_Iso45 xong nhưng đọc lại KHÔNG thấy ô nước — dựng palette nước riêng thay thế.");
                return false;
            }
            if (!hadGridPalette) { return true; }
            if (TimGridPalette(PalettePath) != null) { return true; }

            var rebuilt = ScriptableObject.CreateInstance<GridPalette>();
            if (!string.IsNullOrEmpty(gridPaletteJson)) { EditorJsonUtility.FromJsonOverwrite(gridPaletteJson, rebuilt); }
            rebuilt.name = "Palette Settings";
            AssetDatabase.AddObjectToAsset(rebuilt, PalettePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(PalettePath);
            report.AppendLine("  ✓ Dựng lại sub-asset GridPalette (SaveAsPrefabAsset đã cuốn mất) — Tile Palette mở lại bình thường.");
            return true;
        }

        /// <summary>Đọc LẠI prefab palette từ đĩa, đếm xem đủ 4 tile nước chưa. Không tin giá trị trả về của SaveAsPrefabAsset.</summary>
        private static bool PaletteChuaDuTile(List<TileBase> tiles)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (root == null) { return false; }
            Tilemap map = root.GetComponentInChildren<Tilemap>(true);
            if (map == null) { return false; }
            var co = new HashSet<TileBase>();
            foreach (Vector3Int p in map.cellBounds.allPositionsWithin)
            {
                TileBase t = map.GetTile(p);
                if (t != null) { co.Add(t); }
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!co.Contains(tiles[i])) { return false; }
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  4b. Palette nước RIÊNG — đường lui khi không chen được vào Palette_Iso45
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Dựng MỚI HOÀN TOÀN Assets/maptitle/Map45Iso/Palette_Water45.prefab chỉ chứa 4 ô nước.
        /// An toàn hơn hẳn việc sửa Palette_Iso45: đây là asset của riêng tool, xoá dựng lại không
        /// đụng gì tới palette Sếp đang dùng. Sếp chỉ cần đổi palette ở ô chọn trên cửa sổ Tile Palette.
        /// Cách dựng chép y Map45SetupTool (Grid Isometric 1x0.5 + GridPalette sub-asset).
        /// </summary>
        private static void BuildWaterPalette(List<TileBase> tiles, StringBuilder report, List<string> thieu)
        {
            if (tiles.Count == 0) { return; }
            try
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(WaterPalettePath) != null)
                {
                    AssetDatabase.DeleteAsset(WaterPalettePath);
                }

                var go = new GameObject("Palette_Water45", typeof(Grid));
                var grid = go.GetComponent<Grid>();
                grid.cellLayout = GridLayout.CellLayout.Isometric;
                grid.cellSize = new Vector3(1f, 0.5f, 1f);

                var layer = new GameObject("Layer1", typeof(Tilemap), typeof(TilemapRenderer));
                layer.transform.SetParent(go.transform, false);
                var tm = layer.GetComponent<Tilemap>();
                for (int i = 0; i < tiles.Count; i++) { tm.SetTile(new Vector3Int(i, 0, 0), tiles[i]); }
                layer.GetComponent<TilemapRenderer>().enabled = false;

                PrefabUtility.SaveAsPrefabAsset(go, WaterPalettePath);
                UnityEngine.Object.DestroyImmediate(go);

                var settings = ScriptableObject.CreateInstance<GridPalette>();
                settings.name = "Palette Settings";
                settings.cellSizing = GridPalette.CellSizing.Manual;
                AssetDatabase.AddObjectToAsset(settings, WaterPalettePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(WaterPalettePath);

                report.AppendLine("✓ Dựng " + WaterPalettePath + " — palette riêng, " +
                    tiles.Count.ToString(CultureInfo.InvariantCulture) + " ô nước nằm THẲNG HÀNG NGANG cho dễ nhìn.");
                thieu.Add("Palette_Iso45 không nhận ô nước — dùng palette riêng Palette_Water45 (đổi ở ô chọn palette)");
            }
            catch (Exception e)
            {
                report.AppendLine("✖ Dựng palette nước riêng lỗi: " + e.Message);
                thieu.Add("Không dựng được palette nước — kéo 4 file ở " + TileFolder + " thả vào cửa sổ Tile Palette");
                Debug.LogException(e);
            }
        }

        /// <summary>Dò sub-asset GridPalette đúng cách Unity dò (LoadAllAssetsAtPath), tránh gắn trùng cái thứ hai.</summary>
        private static GridPalette TimGridPalette(string path)
        {
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < all.Length; i++)
            {
                var gp = all[i] as GridPalette;
                if (gp != null) { return gp; }
            }
            return null;
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
