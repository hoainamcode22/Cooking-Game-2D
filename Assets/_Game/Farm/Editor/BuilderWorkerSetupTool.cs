using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TOOL 3 — SETUP THỢ BÚA (1 nút).
///
/// Menu:
///   Tools/Farm Game/Worker/★ SETUP thợ búa (1 nút)
///   Tools/Farm Game/Worker/Hoàn tác setup thợ búa
///
/// GHI CHÚ TÊN FILE: §10 CONTRACT ghi "DecorGrowthSetupTool.cs". File này đổi tên
/// thành BuilderWorkerSetupTool.cs cho rõ nghĩa (nó dựng THỢ BÚA, còn cấu hình
/// 5 stage do DecorStageArtTool.cs lo). Đã ghi nhận trong báo cáo gửi Lead.
///
/// LÀM GÌ
///   1. Đọc 12 sprite hammer_01..12 + 12 sprite celebrate_01..12 từ 2 sheet đã slice.
///      CHƯA SLICE ⇒ ABORT, chỉ Sếp chạy CharacterSheetSliceTool trước.
///   2. Tạo/cập nhật Assets/_Game/Resources/BuilderWorkerConfig.asset
///      (tên file PHẢI khớp HouseWorkerBridge.RESOURCE_NAME = "BuilderWorkerConfig"
///      vì DEV-B nạp bằng Resources.Load).
///   3. Dựng 3 prefab thợ trong Assets/_Game/Farm/Prefabs/Workers/:
///        Worker_Builder_01  bình thường
///        Worker_Builder_02  flipX = true       (nhìn sang trái)
///        Worker_Builder_03  scale × 0.94       (thợ nhỏ hơn, đỡ giống clone)
///      Mỗi prefab: SpriteRenderer + SpriteSequencePlayer + BuilderWorker.
///      KHÔNG Animator — DEV-B cảnh báo Animator sẽ ghi đè sprite của
///      SpriteSequencePlayer mỗi frame (2 hệ đá nhau).
///   4. Gán 3 prefab vào workerPrefabs[0..2] rồi gọi
///      HouseWorkerBridge.InvalidateConfigCache() để cache null cũ không giữ lại.
///
/// CỐ Ý KHÔNG LÀM: KHÔNG set enabled = true (§9 CONTRACT — Sếp tự tick).
/// IDEMPOTENT: prefab đã có ⇒ LoadPrefabContents → sửa → SaveAsPrefabAsset,
/// config đã có ⇒ cập nhật tại chỗ. Chạy 10 lần vẫn đúng 3 prefab + 1 asset.
/// </summary>
public static class BuilderWorkerSetupTool
{
    // ─── Menu ────────────────────────────────────────────────────────────
    private const string MenuRoot   = "Tools/Farm Game/Worker/";
    private const string MenuSetup  = MenuRoot + "★ SETUP thợ búa (1 nút)";
    private const string MenuUndo   = MenuRoot + "Hoàn tác setup thợ búa";

    private const string Tag = "[Tool]";

    // ─── Hằng số CHỈNH ĐƯỢC ──────────────────────────────────────────────

    /// <summary>Chiều cao thợ trong world (unit). Khớp BuilderWorkerConfig.workerWorldHeight mặc định.</summary>
    private const float WorkerWorldHeight = 170f;

    /// <summary>Order gốc §2 CONTRACT (y-sort động cộng/trừ quanh mốc này lúc chạy).</summary>
    private const int WorkerSortingOrder = 5000;

    /// <summary>Thợ thứ 3 nhỏ hơn cho đỡ giống clone.</summary>
    private const float Worker03ScaleMul = 0.94f;

    private const int WorkerPrefabCount = 3;

    // ─── Đường dẫn ───────────────────────────────────────────────────────
    private const string ResourcesFolder = "Assets/_Game/Resources";
    private const string ConfigPath      = ResourcesFolder + "/BuilderWorkerConfig.asset";
    private const string PrefabFolder    = "Assets/_Game/Farm/Prefabs/Workers";

    // ═════════════════════════════════════════════════════════════════════
    //  MENU CHÍNH
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuSetup, false, 10)]
    public static void Setup()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Tag + " BuilderWorkerSetupTool — SETUP THỢ BÚA");
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");

        // ── 1 · Nạp frame từ 2 sheet đã slice ────────────────────────────
        string[] tenHammer    = CharacterSheetSliceTool.WorkerFrameNames("hammer_");
        string[] tenCelebrate = CharacterSheetSliceTool.WorkerFrameNames("celebrate_");

        Sprite[] hammer    = CharacterSheetSliceTool.LoadFramesByName(CharacterSheetSliceTool.PathHammer,    tenHammer);
        Sprite[] celebrate = CharacterSheetSliceTool.LoadFramesByName(CharacterSheetSliceTool.PathCelebrate, tenCelebrate);

        if (hammer == null || celebrate == null)
        {
            sb.AppendLine("✖ ABORT: chưa có đủ sprite con trên 2 sheet thợ.");
            sb.AppendLine("   hammer_01..12    : " + (hammer    == null ? "❌ THIẾU" : "✅ đủ 12"));
            sb.AppendLine("   celebrate_01..12 : " + (celebrate == null ? "❌ THIẾU" : "✅ đủ 12"));
            sb.AppendLine("   " + CharacterSheetSliceTool.PathHammer);
            sb.AppendLine("   " + CharacterSheetSliceTool.PathCelebrate);
            sb.AppendLine();
            sb.AppendLine("SẾP BẤM TRƯỚC: Tools/Farm Game/Characters/★ Slice 3 spritesheet nhân vật (APPLY)");
            sb.AppendLine("               rồi chạy lại menu này.");
            sb.AppendLine($"{Tag} TỔNG KẾT: ABORT — chưa slice spritesheet, không ghi gì lên đĩa.");
            Debug.LogError(sb.ToString());
            return;
        }
        sb.AppendLine("✔ nạp frame: hammer 12/12 · celebrate 12/12 (đọc theo TÊN, thứ tự ordinal 01→12).");

        // ── 2 · Thư mục ──────────────────────────────────────────────────
        if (!EnsureFolder(ResourcesFolder) || !EnsureFolder(PrefabFolder))
        {
            sb.AppendLine("✖ ABORT: không tạo được thư mục " + ResourcesFolder + " hoặc " + PrefabFolder + ".");
            sb.AppendLine($"{Tag} TỔNG KẾT: THẤT BẠI (thiếu thư mục đích).");
            Debug.LogError(sb.ToString());
            return;
        }

        // ── 3 · Config ───────────────────────────────────────────────────
        var cfg = AssetDatabase.LoadAssetAtPath<BuilderWorkerConfig>(ConfigPath);
        bool cfgMoi = cfg == null;
        if (cfgMoi)
        {
            cfg = ScriptableObject.CreateInstance<BuilderWorkerConfig>();
            AssetDatabase.CreateAsset(cfg, ConfigPath);
        }
        Undo.RecordObject(cfg, "Setup thợ búa");

        cfg.hammerFrames    = hammer;
        cfg.celebrateFrames = celebrate;
        // hammerFps · celebrateFps · celebrateIdleFrameIndex · hammerImpactFrames ·
        // workerWorldHeight · fade… CỐ Ý KHÔNG chạm: đó là các ô Sếp/DEV-B tinh chỉnh.
        // Chỉ vá khi giá trị VÔ NGHĨA (asset mới hoặc bị xoá trắng).
        if (cfg.hammerFps    <= 0.01f) cfg.hammerFps    = 10f;
        if (cfg.celebrateFps <= 0.01f) cfg.celebrateFps = 12f;
        if (cfg.workerWorldHeight <= 1f) cfg.workerWorldHeight = WorkerWorldHeight;
        if (cfg.workerPrefabs == null || cfg.workerPrefabs.Length != WorkerPrefabCount)
            cfg.workerPrefabs = new GameObject[WorkerPrefabCount];

        sb.AppendLine($"✔ config {(cfgMoi ? "TẠO MỚI" : "cập nhật")}: {ConfigPath}");
        sb.AppendLine($"   hammerFps={cfg.hammerFps:0.##} · celebrateFps={cfg.celebrateFps:0.##} · " +
                      $"celebrateIdleFrameIndex={cfg.celebrateIdleFrameIndex} · workerWorldHeight={cfg.workerWorldHeight:0.##}");

        // ── 4 · Scale thật ───────────────────────────────────────────────
        float caoSprite = hammer[0] != null ? hammer[0].bounds.size.y : 0f;
        float scaleGoc  = caoSprite > 0.0001f ? cfg.workerWorldHeight / caoSprite : 1f;

        sb.AppendLine($"✔ tính scale: hammer_01.bounds.size.y = {caoSprite:0.####} world unit " +
                      $"(PPU 100) ⇒ scale = {cfg.workerWorldHeight:0.##} / {caoSprite:0.####} = {scaleGoc:0.####}");
        if (caoSprite <= 0.0001f)
            sb.AppendLine("   ⚠ bounds.size.y = 0 (sprite lỗi?) ⇒ tạm dùng scale 1. Kiểm tra lại slice.");

        // ── 5 · Sorting layer ────────────────────────────────────────────
        string layer = ResolveSortingLayer("ObjectsFront", "Objects", "Default");
        sb.AppendLine($"✔ sorting layer resolve: \"{layer}\" · order {WorkerSortingOrder} " +
                      "(runtime BuilderWorker.PlaceAt sẽ resolve lại + y-sort động).");

        // ── 6 · 3 prefab ─────────────────────────────────────────────────
        var prefabs = new GameObject[WorkerPrefabCount];
        int taoMoi = 0, capNhat = 0, loi = 0;

        for (int i = 0; i < WorkerPrefabCount; i++)
        {
            string ten  = "Worker_Builder_" + (i + 1).ToString("00");
            string path = PrefabFolder + "/" + ten + ".prefab";

            bool flipX   = (i == 1);
            float scale  = (i == 2) ? scaleGoc * Worker03ScaleMul : scaleGoc;

            bool moi;
            string loiPrefab;
            prefabs[i] = BuildOrUpdateWorkerPrefab(path, ten, hammer[0], layer, WorkerSortingOrder,
                                                   cfg, flipX, scale, out moi, out loiPrefab);
            if (prefabs[i] == null)
            {
                loi++;
                sb.AppendLine($"✖ {ten}: {loiPrefab}");
                continue;
            }

            if (moi) taoMoi++; else capNhat++;
            sb.AppendLine($"✔ {ten} {(moi ? "TẠO MỚI" : "cập nhật"),-9} · flipX={flipX,-5} · " +
                          $"scale={scale:0.####} · sprite=hammer_01 · SpriteSequencePlayer + BuilderWorker · KHÔNG Animator");
        }

        // ── 7 · Gán prefab vào config ────────────────────────────────────
        int ganDu = 0;
        for (int i = 0; i < WorkerPrefabCount; i++)
        {
            if (prefabs[i] == null) continue;
            cfg.workerPrefabs[i] = prefabs[i];
            ganDu++;
        }
        sb.AppendLine($"✔ gán workerPrefabs[0..2]: {ganDu}/{WorkerPrefabCount} slot có prefab.");

        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 8 · Xoá cache config cũ của DEV-B ────────────────────────────
        HouseWorkerBridge.InvalidateConfigCache();
        sb.AppendLine("✔ gọi HouseWorkerBridge.InvalidateConfigCache() — cache null cũ đã bị xoá.");

        // ── REPORT ───────────────────────────────────────────────────────
        sb.AppendLine();
        sb.AppendLine("──────────────────────────────────────────────────────────────────────");
        sb.AppendLine("CỐ Ý KHÔNG LÀM:");
        sb.AppendLine("  • KHÔNG set enabled = true (§9 CONTRACT — feature flag default an toàn).");
        sb.AppendLine("  • KHÔNG thêm Animator vào prefab (DEV-B: Animator ghi đè sprite mỗi frame,");
        sb.AppendLine("    sẽ đá nhau với SpriteSequencePlayer).");
        sb.AppendLine("  • KHÔNG chạm scene .unity (DANH SÁCH DỪNG).");
        sb.AppendLine("SẾP BẤM TAY: chọn " + ConfigPath + " → tick \"enabled\".");
        sb.AppendLine($"{Tag} TỔNG KẾT SETUP: prefab tạo {taoMoi} · cập nhật {capNhat} · lỗi {loi} · " +
                      $"config {(cfgMoi ? "TẠO MỚI" : "cập nhật")} · 24 sprite đã gán · scale gốc {scaleGoc:0.####}.");

        if (loi > 0) Debug.LogError(sb.ToString()); else Debug.Log(sb.ToString());

        Selection.activeObject = cfg;
        EditorGUIUtility.PingObject(cfg);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MENU HOÀN TÁC
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem(MenuUndo, false, 11)]
    public static void HoanTac()
    {
        var canXoa = new List<string>();
        for (int i = 0; i < WorkerPrefabCount; i++)
        {
            string path = PrefabFolder + "/Worker_Builder_" + (i + 1).ToString("00") + ".prefab";
            if (File.Exists(path)) canXoa.Add(path);
        }
        if (File.Exists(ConfigPath)) canXoa.Add(ConfigPath);

        if (canXoa.Count == 0)
        {
            Debug.Log(Tag + " BuilderWorkerSetupTool — HOÀN TÁC: không có gì để xoá (chưa setup bao giờ).");
            EditorUtility.DisplayDialog("Hoàn tác setup thợ búa",
                "Không tìm thấy prefab hay config nào để xoá.", "OK");
            return;
        }

        bool dongY = EditorUtility.DisplayDialog(
            "Hoàn tác setup thợ búa",
            "SẼ XOÁ VĨNH VIỄN " + canXoa.Count + " file:\n\n" + string.Join("\n", canXoa) +
            "\n\nMọi tinh chỉnh tay trên các file này sẽ MẤT.\n" +
            "Sprite con trên spritesheet KHÔNG bị xoá (chạy lại tool slice không cần thiết).",
            "XOÁ", "Thôi");

        if (!dongY)
        {
            Debug.Log(Tag + " BuilderWorkerSetupTool — HOÀN TÁC: Sếp đã bấm Thôi, không xoá gì.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Tag + " BuilderWorkerSetupTool — HOÀN TÁC");
        int daXoa = 0, khongXoaDuoc = 0;
        for (int i = 0; i < canXoa.Count; i++)
        {
            if (AssetDatabase.DeleteAsset(canXoa[i])) { daXoa++; sb.AppendLine("✔ đã xoá " + canXoa[i]); }
            else { khongXoaDuoc++; sb.AppendLine("✖ KHÔNG xoá được " + canXoa[i]); }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        HouseWorkerBridge.InvalidateConfigCache();

        sb.AppendLine($"{Tag} TỔNG KẾT HOÀN TÁC: xoá {daXoa}/{canXoa.Count} file · lỗi {khongXoaDuoc}. " +
                      "Đã gọi InvalidateConfigCache().");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  DỰNG 1 PREFAB THỢ
    // ═════════════════════════════════════════════════════════════════════
    private static GameObject BuildOrUpdateWorkerPrefab(string path, string tenObject, Sprite spriteDau,
                                                        string sortingLayer, int sortingOrder,
                                                        BuilderWorkerConfig cfg, bool flipX, float scale,
                                                        out bool moi, out string loi)
    {
        moi = !File.Exists(path);
        loi = null;

        GameObject root = null;
        try
        {
            root = moi ? new GameObject(tenObject) : PrefabUtility.LoadPrefabContents(path);
            if (root == null) { loi = "không mở/tạo được prefab."; return null; }

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite           = spriteDau;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder     = sortingOrder;
            sr.flipX            = flipX;

            var player = root.GetComponent<SpriteSequencePlayer>();
            if (player == null) player = root.AddComponent<SpriteSequencePlayer>();
            player.target          = sr;
            player.frames          = cfg != null ? cfg.hammerFrames : null;
            player.fps             = cfg != null ? cfg.hammerFps : 10f;
            player.loop            = true;
            player.pingPong        = false;
            // playOnEnable = false: BuilderWorker.Setup() bắt đầu ở Hidden rồi Crew mới ra
            // lệnh mode thật. Bật playOnEnable sẽ cho thợ đập búa vài frame TRƯỚC khi
            // được điều phối — thấy rõ lúc Instantiate.
            player.playOnEnable    = false;
            player.useUnscaledTime = false;

            if (root.GetComponent<BuilderWorker>() == null) root.AddComponent<BuilderWorker>();

            // KHÔNG thêm Animator. Nếu prefab cũ có Animator (tay ai đó thêm) thì CẢNH BÁO
            // chứ không tự xoá — xoá component là sửa chỉnh tay của Sếp.
            var anim = root.GetComponent<Animator>();
            if (anim != null)
                Debug.LogWarning(Tag + " " + tenObject + " ĐANG CÓ Animator — DEV-B cảnh báo Animator " +
                                 "ghi đè sprite mỗi frame và sẽ đá nhau với SpriteSequencePlayer. " +
                                 "Gỡ tay component Animator trên prefab này.");

            if (scale > 0.0001f) root.transform.localScale = new Vector3(scale, scale, 1f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null) loi = "SaveAsPrefabAsset trả về null.";
            return saved;
        }
        catch (System.Exception e)
        {
            loi = "ngoại lệ — " + e.Message;
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

    // ═════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trả tên sorting layer ĐẦU TIÊN có thật. KHÔNG hardcode "CongTrinh" (layer đó
    /// không tồn tại — §7 CONTRACT: Unity im lặng đẩy renderer về Default).
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
                         "] — rơi về \"Default\", thợ có thể bị nhà che.");
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
}
