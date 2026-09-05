using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TOOL 2 — NẠP ART 5 STAGE CHO 15 MÓN TRANG TRÍ + TẠO 4 ITEM MỚI.
///
/// Menu:
///   Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage (DRY-RUN)
///   Tools/Farm Game/Decor 5 Stage/★ Nạp art 5 stage (APPLY)
///   Tools/Farm Game/Decor 5 Stage/Tạo 4 DecorData item mới (DRY-RUN)
///   Tools/Farm Game/Decor 5 Stage/Tạo 4 DecorData item mới (APPLY)
///
/// NGUỒN ART (Lead xoá phông + bottom-align sẵn, tool KHÔNG tự xử ảnh):
///   Assets/Art/Decor/Stages/&lt;slug&gt;/stage_1.png … stage_5.png
///
/// LÀM GÌ
///   A) Nạp art: đặt import setting đúng (Sprite Single · pivot Bottom-Center ·
///      PPU 100 · alphaIsTransparency · mipmap OFF · FullRect) rồi đổ 15 entry vào
///      Assets/_Game/Resources/DecorGrowthConfig.asset (DEV-A đọc bằng
///      Resources.Load, xem DecorGrowthBootstrap.ConfigResourcePath).
///   B) Tạo 4 item mới (itemID 16..19) chưa có trong shop: prefab + DecorData asset.
///
/// KHÔNG BAO GIỜ TỰ LÀM (§9 CONTRACT + DANH SÁCH DỪNG):
///   • KHÔNG set DecorGrowthConfig.enabled = true — Sếp tự tick trong Inspector.
///   • KHÔNG sửa scene .unity ⇒ KHÔNG tự thêm 4 item mới vào ShopManager.decorList.
///     Tool chỉ in đường dẫn 4 asset để Sếp kéo tay.
///
/// IDEMPOTENT: stageSet tra theo itemID — trùng thì CẬP NHẬT, không thêm bản sao.
/// Prefab đã có thì LoadPrefabContents → sửa → SaveAsPrefabAsset (giữ chỉnh tay
/// ở các field khác). DecorData đã có thì cập nhật tại chỗ, không tạo asset thứ hai.
/// </summary>
public static class DecorStageArtTool
{
    // ─── Menu ────────────────────────────────────────────────────────────
    private const string MenuRoot     = "Tools/Farm Game/Decor 5 Stage/";
    private const string MenuArtDry   = MenuRoot + "★ Nạp art 5 stage (DRY-RUN)";
    private const string MenuArtApply = MenuRoot + "★ Nạp art 5 stage (APPLY)";
    private const string MenuNewDry   = MenuRoot + "Tạo 4 DecorData item mới (DRY-RUN)";
    private const string MenuNewApply = MenuRoot + "Tạo 4 DecorData item mới (APPLY)";

    private const string Tag = "[Tool]";

    // ─── Hằng số CHỈNH ĐƯỢC ──────────────────────────────────────────────
    private const float PixelsPerUnit = 100f;

    /// <summary>Sorting order của prefab trang trí mới — khớp m_SortingOrder 500 của công trình cũ.</summary>
    private const int DecorSortingOrder = 500;

    // ─── Đường dẫn ───────────────────────────────────────────────────────
    private const string StageRoot       = "Assets/Art/Decor/Stages";
    private const string ResourcesFolder = "Assets/_Game/Resources";
    private const string ConfigPath      = ResourcesFolder + "/DecorGrowthConfig.asset";
    private const string DecorFolder     = "Assets/_Game/Farm/CÔNG TRÌNH";

    private const int StageCount = 5;

    // ─────────────────────────────────────────────────────────────────────
    //  BẢNG MAP slug → itemID (Sếp đã duyệt: 10 khớp + 4 mới + 1 khớp ghi chú)
    // ─────────────────────────────────────────────────────────────────────
    private sealed class Entry
    {
        public string     slug;
        public int        itemID;
        public string     tenHienThi;
        public bool       laMoi;          // true = chưa có DecorData trong project
        public string     tenFileAsset;   // chỉ dùng cho item mới
        public int        gem;            // chỉ dùng cho item mới
        public Vector2Int gridSize;       // chỉ dùng cho item mới
        public string     ghiChu;
    }

    private static Entry[] BangMap()
    {
        return new[]
        {
            new Entry { slug = "gieng",       itemID = 1,  tenHienThi = "Giếng" },
            new Entry { slug = "bunhin",      itemID = 2,  tenHienThi = "Bù nhìn" },
            new Entry { slug = "chanhoa",     itemID = 4,  tenHienThi = "Chân Hoa" },
            new Entry { slug = "coixaygio",   itemID = 5,  tenHienThi = "Cối Xoay Gió" },
            new Entry { slug = "cotden",      itemID = 6,  tenHienThi = "Cột Đèn" },
            new Entry { slug = "meovuive",    itemID = 9,  tenHienThi = "Heo Vui Vẻ",
                        ghiChu = "Lead ghi nhận sheet này stage-3 vẽ HEO không phải MÈO — đã vào đơn đặt lại art." },
            new Entry { slug = "rom",         itemID = 10, tenHienThi = "Rơm Hoa" },
            new Entry { slug = "vonghoa",     itemID = 11, tenHienThi = "Vòng Hoa" },
            new Entry { slug = "xehoa",       itemID = 13, tenHienThi = "Xe Hoa" },
            new Entry { slug = "dainuoc",     itemID = 14, tenHienThi = "Đài Nước" },
            new Entry { slug = "hoda",        itemID = 15, tenHienThi = "Hồ Đá" },

            new Entry { slug = "chaucaythu",  itemID = 16, tenHienThi = "Chậu Cây Thú",
                        laMoi = true, tenFileAsset = "Chau Cay Thu.asset",  gem = 150, gridSize = new Vector2Int(2, 2) },
            new Entry { slug = "chulun",      itemID = 17, tenHienThi = "Chú Lùn Sân Vườn",
                        laMoi = true, tenFileAsset = "Chu Lun.asset",       gem = 200, gridSize = new Vector2Int(2, 2) },
            new Entry { slug = "giabanrau",   itemID = 18, tenHienThi = "Giá Bán Rau",
                        laMoi = true, tenFileAsset = "Gia Ban Rau.asset",   gem = 250, gridSize = new Vector2Int(3, 3) },
            new Entry { slug = "binhtuoihoa", itemID = 19, tenHienThi = "Bình Tưới Hoa",
                        laMoi = true, tenFileAsset = "Binh Tuoi Hoa.asset", gem = 150, gridSize = new Vector2Int(2, 2) },
        };
    }

    // ═════════════════════════════════════════════════════════════════════
    //  A) NẠP ART 5 STAGE
    // ═════════════════════════════════════════════════════════════════════

    [MenuItem(MenuArtDry, false, 10)]
    public static void ArtDryRun()
    {
        Entry[] ds = BangMap();
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " DecorStageArtTool — NẠP ART 5 STAGE (DRY-RUN, không ghi gì)");
        sb.AppendLine("Nguồn art: " + StageRoot + "/<slug>/stage_1..5.png");
        sb.AppendLine("Đích     : " + ConfigPath);
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        if (!AssetDatabase.IsValidFolder(StageRoot))
            sb.AppendLine("⚠ CHƯA CÓ thư mục " + StageRoot +
                          " — Lead đang xoá phông + tái căn baseline. Bảng dưới sẽ toàn 0/5.");

        int duArt = 0, thieuArt = 0, canhBaoCanvas = 0, thieuAsset = 0;

        for (int i = 0; i < ds.Length; i++)
        {
            Entry e = ds[i];
            sb.AppendLine();
            sb.AppendLine($"[{i + 1,2}/{ds.Length}] slug={e.slug,-12} itemID={e.itemID,-3} \"{e.tenHienThi}\"" +
                          (e.laMoi ? "  (ITEM MỚI)" : ""));

            int found = 0;
            var kichThuoc = new List<string>();
            int w0 = -1, h0 = -1;
            bool lechCanvas = false;

            for (int s = 1; s <= StageCount; s++)
            {
                string p = StagePath(e.slug, s);
                int w, h;
                if (!TryReadPngSize(p, out w, out h))
                {
                    kichThuoc.Add($"stage_{s}=THIẾU");
                    continue;
                }
                found++;
                kichThuoc.Add($"stage_{s}={w}x{h}");
                if (w0 < 0) { w0 = w; h0 = h; }
                else if (w != w0 || h != h0) lechCanvas = true;
            }

            sb.AppendLine($"        art: {found}/{StageCount} file · " + string.Join(" · ", kichThuoc));

            if (found == StageCount) duArt++; else thieuArt++;

            if (lechCanvas)
            {
                canhBaoCanvas++;
                sb.AppendLine("        ⚠ CẢNH BÁO CANVAS LỆCH: 5 stage KHÔNG cùng kích thước ⇒ " +
                              "đổi stage sẽ GIẬT HÌNH (vật nhảy vị trí). Nhờ Lead xuất lại cùng canvas.");
            }

            // Đối chiếu với DecorData thật trên đĩa.
            string duongDanAsset;
            DecorData data = TimDecorDataTheoItemID(e.itemID, out duongDanAsset);
            if (data != null)
                sb.AppendLine($"        DecorData: ✅ {duongDanAsset} (itemName=\"{data.itemName}\", gem={data.diamondPrice}, grid={data.gridSize.x}x{data.gridSize.y})");
            else
            {
                thieuAsset++;
                sb.AppendLine("        DecorData: ❌ chưa có asset itemID=" + e.itemID +
                              (e.laMoi ? " — đúng như dự kiến, chạy menu \"Tạo 4 DecorData item mới (APPLY)\"."
                                       : " — BẤT THƯỜNG với item khớp, kiểm tra lại bảng map."));
            }

            if (!string.IsNullOrEmpty(e.ghiChu))
                sb.AppendLine("        ghi chú: " + e.ghiChu);
        }

        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine("KHÔNG có art 5 stage (CỐ Ý bỏ, giữ hành vi cũ): Bảng hiệu(3) · Ghế Hoa(7) · " +
                      "Heo thần tài(8) · Vịt vui vẻ(12) · Chậu Hoa1-4(109-112) · Đất(100).");
        sb.AppendLine($"{Tag} TỔNG KẾT DRY-RUN: đủ art {duArt}/{ds.Length} · thiếu art {thieuArt} · " +
                      $"canvas lệch {canhBaoCanvas} · thiếu DecorData {thieuAsset}. " +
                      "Chưa ghi gì lên đĩa.");
        Debug.Log(sb.ToString());
    }

    [MenuItem(MenuArtApply, false, 11)]
    public static void ArtApply()
    {
        Entry[] ds = BangMap();
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " DecorStageArtTool — NẠP ART 5 STAGE (APPLY)");
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        if (!EnsureFolder(ResourcesFolder))
        {
            sb.AppendLine("✖ DỪNG: không tạo được thư mục " + ResourcesFolder + ".");
            sb.AppendLine($"{Tag} TỔNG KẾT APPLY: THẤT BẠI (không có thư mục Resources).");
            Debug.LogError(sb.ToString());
            return;
        }

        var cfg = AssetDatabase.LoadAssetAtPath<DecorGrowthConfig>(ConfigPath);
        bool cfgMoi = cfg == null;
        if (cfgMoi)
        {
            cfg = ScriptableObject.CreateInstance<DecorGrowthConfig>();
            AssetDatabase.CreateAsset(cfg, ConfigPath);
            sb.AppendLine("✔ TẠO MỚI config: " + ConfigPath);
        }
        else
        {
            sb.AppendLine("✔ dùng lại config có sẵn: " + ConfigPath);
        }

        Undo.RecordObject(cfg, "Nap art 5 stage vao DecorGrowthConfig");
        if (cfg.stageSets == null) cfg.stageSets = new List<DecorStageSet>();

        int taoMoi = 0, capNhat = 0, boQua = 0, importDoi = 0, canhBaoCanvas = 0;

        for (int i = 0; i < ds.Length; i++)
        {
            Entry e = ds[i];

            // 1 · Import setting cho 5 PNG (chỉ reimport khi thật sự khác).
            var sprites = new Sprite[StageCount];
            int found = 0;
            int w0 = -1, h0 = -1;
            bool lechCanvas = false;

            for (int s = 1; s <= StageCount; s++)
            {
                string p = StagePath(e.slug, s);
                int w, h;
                if (!TryReadPngSize(p, out w, out h)) continue;

                if (w0 < 0) { w0 = w; h0 = h; }
                else if (w != w0 || h != h0) lechCanvas = true;

                if (ApplyStageImport(p, w, h)) importDoi++;
                sprites[s - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (sprites[s - 1] != null) found++;
            }

            if (lechCanvas) canhBaoCanvas++;

            // DecorStageSet.IsValid cần stage1 + stage3 + stage4. Thiếu ⇒ KHÔNG đổ entry
            // (đổ nửa vời sẽ làm DEV-A bật hệ xây rồi vẽ sprite null = vật vô hình).
            if (sprites[0] == null || sprites[2] == null || sprites[3] == null)
            {
                boQua++;
                sb.AppendLine($"– BỎ QUA {e.slug} (itemID {e.itemID}): chỉ có {found}/{StageCount} sprite, " +
                              "thiếu stage_1 / stage_3 / stage_4 (bộ tối thiểu DecorStageSet.IsValid).");
                sb.AppendLine("  CẦN LÀM: chờ Lead xuất đủ art vào " + StageRoot + "/" + e.slug + "/ rồi chạy lại.");
                continue;
            }

            // 2 · Upsert theo itemID.
            DecorStageSet set = null;
            for (int k = 0; k < cfg.stageSets.Count; k++)
            {
                DecorStageSet cur = cfg.stageSets[k];
                if (cur != null && cur.itemID == e.itemID) { set = cur; break; }
            }

            bool moi = set == null;
            if (moi)
            {
                set = new DecorStageSet();
                cfg.stageSets.Add(set);
            }

            // CHỈ ghi những field art + định danh. buildSecondsOverride và workerCount
            // CỐ Ý KHÔNG chạm: đó là hai ô Sếp tinh chỉnh tay, ghi đè là xoá công của Sếp.
            set.itemID          = e.itemID;
            set.displayName     = e.tenHienThi;
            set.stage1Parts     = sprites[0];
            set.stage2HalfBuilt = sprites[1];
            set.stage3Complete  = sprites[2];
            set.stage4GiftBox   = sprites[3];
            set.stage5BoxOpen   = sprites[4];

            if (moi) taoMoi++; else capNhat++;
            sb.AppendLine($"{(moi ? "✔ THÊM " : "✔ SỬA  ")} itemID {e.itemID,-3} {e.slug,-12} " +
                          $"\"{e.tenHienThi}\" · {found}/{StageCount} sprite" +
                          (sprites[1] == null ? " (thiếu stage_2 — DEV-A tự lùi về stage_1)" : "") +
                          (sprites[4] == null ? " (thiếu stage_5 — DEV-A tự bỏ pha hộp bung)" : "") +
                          (lechCanvas ? " ⚠ CANVAS LỆCH" : ""));
        }

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine($"stageSets sau khi chạy: {cfg.stageSets.Count} entry.");
        sb.AppendLine($"Import setting đã đổi: {importDoi} file PNG.");
        if (canhBaoCanvas > 0)
            sb.AppendLine($"⚠ {canhBaoCanvas} slug có 5 stage KHÁC canvas ⇒ sẽ GIẬT HÌNH lúc đổi stage. Nhờ Lead xuất lại.");
        sb.AppendLine();
        sb.AppendLine("CỐ Ý KHÔNG LÀM: enabled vẫn để FALSE (§9 CONTRACT — feature flag default an toàn).");
        sb.AppendLine("SẾP BẤM TAY: chọn " + ConfigPath + " → tick 'enabled' khi muốn bật hệ 5 stage.");
        sb.AppendLine($"{Tag} TỔNG KẾT APPLY: thêm {taoMoi} · sửa {capNhat} · bỏ qua {boQua} " +
                      $"trên {ds.Length} slug. Config: {(cfgMoi ? "TẠO MỚI" : "cập nhật")}.");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  B) TẠO 4 DecorData ITEM MỚI (itemID 16..19)
    // ═════════════════════════════════════════════════════════════════════

    [MenuItem(MenuNewDry, false, 20)]
    public static void NewItemsDryRun()
    {
        TaoItemMoi(true);
    }

    [MenuItem(MenuNewApply, false, 21)]
    public static void NewItemsApply()
    {
        TaoItemMoi(false);
    }

    private static void TaoItemMoi(bool dryRun)
    {
        Entry[] all = BangMap();
        var moiDs = new List<Entry>();
        for (int i = 0; i < all.Length; i++) if (all[i].laMoi) moiDs.Add(all[i]);

        var sb = new StringBuilder();
        sb.AppendLine(Tag + " DecorStageArtTool — TẠO 4 DecorData ITEM MỚI " + (dryRun ? "(DRY-RUN)" : "(APPLY)"));
        sb.AppendLine("Thư mục asset : " + DecorFolder);
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        if (!AssetDatabase.IsValidFolder(DecorFolder))
        {
            sb.AppendLine("✖ DỪNG: không thấy thư mục " + DecorFolder + ".");
            sb.AppendLine("  CẦN LÀM: kiểm tra lại tên thư mục (có dấu, chữ IN HOA) trong Project.");
            sb.AppendLine($"{Tag} TỔNG KẾT: THẤT BẠI (thiếu thư mục đích).");
            Debug.LogError(sb.ToString());
            return;
        }

        // Hai component tuỳ chọn — dò bằng Type.GetType để KHÔNG phá build nếu ai xoá class.
        System.Type tEditable  = System.Type.GetType("EditableBuilding, Assembly-CSharp");
        if (tEditable == null) tEditable = System.Type.GetType("EditableBuilding");
        System.Type tFootprint = System.Type.GetType("BuildingFootprintKit, Assembly-CSharp");
        if (tFootprint == null) tFootprint = System.Type.GetType("BuildingFootprintKit");

        sb.AppendLine("component tuỳ chọn: EditableBuilding = " + (tEditable != null ? "✅ có" : "❌ KHÔNG có (bỏ qua)") +
                      " · BuildingFootprintKit = " + (tFootprint != null ? "✅ có" : "❌ KHÔNG có (bỏ qua)"));
        if (tEditable == null)
            sb.AppendLine("  ⚠ thiếu EditableBuilding ⇒ 4 món mới sẽ KHÔNG di chuyển được trong Edit Mode.");

        string layer = ResolveSortingLayer("Objects", "ObjectsFront", "Default");
        sb.AppendLine("sorting layer resolve: \"" + layer + "\" · order " + DecorSortingOrder);

        int taoPrefab = 0, suaPrefab = 0, taoAsset = 0, suaAsset = 0, boQua = 0;
        var duongDanAsset = new List<string>();

        for (int i = 0; i < moiDs.Count; i++)
        {
            Entry e = moiDs[i];
            string prefabPath = DecorFolder + "/Decor_" + e.slug + ".prefab";
            string assetPath  = DecorFolder + "/" + e.tenFileAsset;
            string icon3      = StagePath(e.slug, 3);

            sb.AppendLine();
            sb.AppendLine($"[{i + 1}/{moiDs.Count}] itemID {e.itemID} · \"{e.tenHienThi}\" · gem {e.gem} · " +
                          $"grid {e.gridSize.x}x{e.gridSize.y} · unlockLevel 1");
            sb.AppendLine("        prefab : " + prefabPath);
            sb.AppendLine("        asset  : " + assetPath);

            int w3, h3;
            bool coIcon = TryReadPngSize(icon3, out w3, out h3);
            if (!coIcon)
            {
                boQua++;
                sb.AppendLine("        ✖ BỎ QUA: thiếu " + icon3 +
                              " — cần stage_3 làm itemIcon và sprite prefab.");
                sb.AppendLine("          CẦN LÀM: chờ Lead xuất art slug \"" + e.slug + "\" rồi chạy lại.");
                continue;
            }

            if (dryRun)
            {
                sb.AppendLine($"        ✔ SẼ LÀM: import stage_3 ({w3}x{h3}) → " +
                              (File.Exists(prefabPath) ? "CẬP NHẬT prefab" : "TẠO prefab") + " → " +
                              (AssetDatabase.LoadAssetAtPath<DecorData>(assetPath) != null ? "CẬP NHẬT DecorData" : "TẠO DecorData"));
                duongDanAsset.Add(assetPath);
                continue;
            }

            // ── APPLY ──
            ApplyStageImport(icon3, w3, h3);
            Sprite sp3 = AssetDatabase.LoadAssetAtPath<Sprite>(icon3);
            if (sp3 == null)
            {
                boQua++;
                sb.AppendLine("        ✖ BỎ QUA: import xong vẫn không load được Sprite từ " + icon3 + ".");
                continue;
            }

            bool prefabMoi;
            GameObject prefabAsset = BuildOrUpdatePrefab(prefabPath, "Decor_" + e.slug, sp3, layer,
                                                        e.gridSize, tEditable, tFootprint, out prefabMoi);
            if (prefabAsset == null)
            {
                boQua++;
                sb.AppendLine("        ✖ BỎ QUA: không dựng được prefab " + prefabPath + ".");
                continue;
            }
            if (prefabMoi) taoPrefab++; else suaPrefab++;
            sb.AppendLine($"        ✔ prefab {(prefabMoi ? "TẠO MỚI" : "cập nhật")} · sprite=stage_3 · " +
                          $"collider size=({sp3.bounds.size.x:0.#}, {sp3.bounds.size.y:0.#}) offset=(0, {sp3.bounds.size.y * 0.5f:0.#})");

            var data = AssetDatabase.LoadAssetAtPath<DecorData>(assetPath);
            bool assetMoi = data == null;
            if (assetMoi)
            {
                data = ScriptableObject.CreateInstance<DecorData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            Undo.RecordObject(data, "Tao DecorData item moi");

            data.itemID        = e.itemID.ToString();   // BaseItemData.itemID là STRING, DEV-A int.TryParse
            data.itemName      = e.tenHienThi;
            data.itemIcon      = sp3;
            data.diamondPrice  = e.gem;
            data.unlockLevel   = 1;
            data.gridSize      = e.gridSize;
            data.prefabToBuild = prefabAsset;
            // goldPrice giữ 0 (bán bằng kim cương) · buildTimeSeconds giữ 0
            // (DecorGrowthConfig.ResolveBuildSeconds tự tính từ diamondPrice, §8 CONTRACT).

            EditorUtility.SetDirty(data);
            if (assetMoi) taoAsset++; else suaAsset++;
            sb.AppendLine($"        ✔ DecorData {(assetMoi ? "TẠO MỚI" : "cập nhật")} · itemID=\"{data.itemID}\" · " +
                          $"gem={data.diamondPrice} · grid={data.gridSize.x}x{data.gridSize.y}");
            duongDanAsset.Add(assetPath);
        }

        if (!dryRun)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine("CỐ Ý KHÔNG LÀM (DANH SÁCH DỪNG — sửa .unity):");
        sb.AppendLine("  Tool KHÔNG thêm 4 item mới vào ShopManager.decorList trong scene.");
        sb.AppendLine("SẾP BẤM TAY: mở " + "Assets/_Game/Scenes/SCN_Farm.unity" + " → chọn ShopManager →");
        sb.AppendLine("  kéo 4 asset dưới đây vào list \"decorList\" → Ctrl+S:");
        if (duongDanAsset.Count == 0)
            sb.AppendLine("    (chưa có asset nào — thiếu art stage_3)");
        else
            for (int i = 0; i < duongDanAsset.Count; i++) sb.AppendLine("    " + (i + 1) + ". " + duongDanAsset[i]);

        sb.AppendLine();
        sb.AppendLine($"{Tag} TỔNG KẾT {(dryRun ? "DRY-RUN" : "APPLY")}: prefab tạo {taoPrefab}/sửa {suaPrefab} · " +
                      $"DecorData tạo {taoAsset}/sửa {suaAsset} · bỏ qua {boQua} trên {moiDs.Count} item mới." +
                      (dryRun ? " Chưa ghi gì lên đĩa." : ""));
        Debug.Log(sb.ToString());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PREFAB
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dựng/cập nhật prefab trang trí. Prefab đã có ⇒ LoadPrefabContents → sửa →
    /// SaveAsPrefabAsset → UnloadPrefabContents (GIỮ mọi field Sếp chỉnh tay ở
    /// component khác). Trả về asset prefab, null nếu thất bại.
    /// </summary>
    private static GameObject BuildOrUpdatePrefab(string prefabPath, string tenObject, Sprite sprite,
                                                  string sortingLayer, Vector2Int gridSize,
                                                  System.Type tEditable, System.Type tFootprint,
                                                  out bool moi)
    {
        moi = !File.Exists(prefabPath);

        GameObject root = null;
        try
        {
            root = moi ? new GameObject(tenObject) : PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null) return null;

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.sortingLayerName = sortingLayer;
            if (moi) sr.sortingOrder = DecorSortingOrder;   // lần sau KHÔNG ghi đè order Sếp sửa

            // Collider phải có TRƯỚC khi thêm EditableBuilding ([RequireComponent(Collider2D)]).
            var box = root.GetComponent<BoxCollider2D>();
            if (box == null) box = root.AddComponent<BoxCollider2D>();
            Vector2 size = sprite.bounds.size;                 // world unit (PPU 100)
            box.size   = size;
            box.offset = new Vector2(0f, size.y * 0.5f);        // pivot Bottom-Center ⇒ dịch lên nửa chiều cao

            if (tEditable != null && root.GetComponent(tEditable) == null)
                root.AddComponent(tEditable);

            if (tFootprint != null && root.GetComponent(tFootprint) == null)
            {
                Component kit = root.AddComponent(tFootprint);
                // soO là [SerializeField] private ⇒ ghi qua SerializedObject, không reflection tay.
                if (kit != null)
                {
                    var so   = new SerializedObject(kit);
                    var prop = so.FindProperty("soO");
                    if (prop != null && prop.propertyType == SerializedPropertyType.Vector2Int)
                    {
                        prop.vector2IntValue = gridSize;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return saved;
        }
        catch (System.Exception e)
        {
            Debug.LogError(Tag + " lỗi khi dựng prefab " + prefabPath + " — " + e.Message);
            return null;
        }
        finally
        {
            if (root != null)
            {
                if (moi) Object.DestroyImmediate(root);
                else     PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────

    private static string StagePath(string slug, int stage)
    {
        return StageRoot + "/" + slug + "/stage_" + stage + ".png";
    }

    /// <summary>
    /// Đặt import setting cho 1 PNG stage. Trả về true nếu THỰC SỰ phải reimport
    /// (so sánh trước khi ghi ⇒ chạy tool lần 2 không reimport lại 75 file).
    /// </summary>
    private static bool ApplyStageImport(string path, int w, int h)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        int maxSize = Mathf.Max(1024, Mathf.NextPowerOfTwo(Mathf.Max(w, h)));

        var ts = new TextureImporterSettings();
        importer.ReadTextureSettings(ts);

        bool canDoi =
            importer.textureType         != TextureImporterType.Sprite      ||
            importer.spriteImportMode    != SpriteImportMode.Single         ||
            !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) ||
            !importer.alphaIsTransparency                                    ||
            importer.mipmapEnabled                                           ||
            importer.filterMode          != FilterMode.Bilinear             ||
            importer.npotScale           != TextureImporterNPOTScale.None   ||
            importer.maxTextureSize      < maxSize                          ||
            ts.spriteAlignment           != (int)SpriteAlignment.BottomCenter ||
            ts.spriteMeshType            != SpriteMeshType.FullRect;

        if (!canDoi) return false;

        Undo.RecordObject(importer, "Import setting art 5 stage");

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Bilinear;
        importer.sRGBTexture         = true;
        importer.npotScale           = TextureImporterNPOTScale.None;
        importer.maxTextureSize      = Mathf.Max(importer.maxTextureSize, maxSize);

        // BUG DE TRANH: ts được ReadTextureSettings() Ở TRÊN (trước khi sửa importer),
        // nên nó còn giữ spriteMode/textureType CŨ. SetTextureSettings(ts) sẽ GHI ĐÈ ngược
        // lại hai field đó và xoá mất Single/Sprite vừa set. Phải đặt lại tường minh.
        ts.textureType     = TextureImporterType.Sprite;
        ts.spriteMode      = (int)SpriteImportMode.Single;
        ts.spriteAlignment = (int)SpriteAlignment.BottomCenter;   // §2: chân chạm đất
        ts.spriteMeshType  = SpriteMeshType.FullRect;
        ts.spritePixelsPerUnit = PixelsPerUnit;
        importer.SetTextureSettings(ts);

        importer.SaveAndReimport();
        return true;
    }

    private static DecorData TimDecorDataTheoItemID(int itemID, out string duongDan)
    {
        duongDan = null;
        string can = itemID.ToString();

        string[] guids = AssetDatabase.FindAssets("t:DecorData");
        if (guids == null) return null;

        for (int i = 0; i < guids.Length; i++)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[i]);
            var d = AssetDatabase.LoadAssetAtPath<DecorData>(p);
            if (d == null) continue;
            if (d.itemID == can) { duongDan = p; return d; }
        }
        return null;
    }

    /// <summary>
    /// Trả tên sorting layer ĐẦU TIÊN có thật. KHÔNG hardcode "CongTrinh" (layer đó
    /// không tồn tại — bug cũ làm renderer im lặng rơi về Default, xem §7 CONTRACT).
    /// </summary>
    private static string ResolveSortingLayer(params string[] uuTien)
    {
        if (uuTien == null || uuTien.Length == 0) return "Default";

        SortingLayer[] ds = SortingLayer.layers;
        if (ds != null)
        {
            for (int i = 0; i < uuTien.Length; i++)
            {
                if (string.IsNullOrEmpty(uuTien[i])) continue;
                for (int k = 0; k < ds.Length; k++)
                {
                    if (ds[k].name != uuTien[i]) continue;
                    if (i > 0)
                        Debug.LogWarning(Tag + " sorting layer \"" + uuTien[0] + "\" không có trong project — dùng tạm \"" + uuTien[i] + "\".");
                    return uuTien[i];
                }
            }
        }

        Debug.LogWarning(Tag + " không có sorting layer nào trong [" + string.Join(", ", uuTien) +
                         "] — rơi về \"Default\", vật có thể bị che.");
        return "Default";
    }

    private static bool EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return false;
        if (AssetDatabase.IsValidFolder(folder)) return true;

        string parent = Path.GetDirectoryName(folder);
        if (parent != null) parent = parent.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);

        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return false;
        if (!EnsureFolder(parent)) return false;

        string guid = AssetDatabase.CreateFolder(parent, leaf);
        return !string.IsNullOrEmpty(guid) && AssetDatabase.IsValidFolder(folder);
    }

    /// <summary>Đọc kích thước PNG từ IHDR — số THẬT của file, không bị maxTextureSize co.</summary>
    private static bool TryReadPngSize(string assetPath, out int w, out int h)
    {
        w = 0; h = 0;
        if (string.IsNullOrEmpty(assetPath)) return false;

        try
        {
            if (!File.Exists(assetPath)) return false;
            using (var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read))
            {
                var b = new byte[24];
                if (fs.Read(b, 0, 24) < 24) return false;
                if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
                w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return w > 0 && h > 0;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(Tag + " không đọc được header PNG " + assetPath + " — " + e.Message);
            return false;
        }
    }
}
