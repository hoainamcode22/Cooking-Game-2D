using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EDITOR TOOL — SETUP HỆ "TIỀN TỆ BAY VỀ HUD" (RewardFlyFX) + ĐỒNG BỘ ICON VÀNG
/// ═══════════════════════════════════════════════════════════════════════════
/// Menu:
///   Tools/Farm Game/Reward FX/★ Setup Reward Fly FX (1 nút)
///       → tạo asset RewardIconLibrary trong Assets/_Game/Resources (tự tạo folder),
///         gán sprite từ Assets/Art/UI/Currency nếu đội art đã bàn giao,
///         gắn component RewardFlyFX lên canvas HUD. Idempotent — chạy lại vô hại.
///   Tools/Farm Game/Reward FX/Đồng bộ icon vàng (DRY-RUN)
///       → CHỈ LIỆT KÊ mọi Image (scene mở + prefab dưới Assets/_Game) đang dùng
///         sprite tên giống icon vàng cũ. Không đổi gì.
///   Tools/Farm Game/Reward FX/Đồng bộ icon vàng (APPLY)
///       → đổi các Image trên sang RewardIconLibrary.goldSprite.
///         TỪ CHỐI chạy nếu goldSprite chưa gán.
///
/// AN TOÀN:
///   - KHÔNG tự save scene (Undo tự đánh dấu scene dirty, Sếp Ctrl+S khi ưng).
///   - Thay đổi trong SCENE có Undo. Thay đổi trong PREFAB được ghi qua
///     LoadPrefabContents/SavePrefabAsset — KHÔNG undo được bằng Ctrl+Z,
///     vì vậy LUÔN chạy DRY-RUN và duyệt danh sách trước khi APPLY.
/// </summary>
public static class RewardFxSetupTool
{
    private const string MENU_ROOT  = "Tools/Farm Game/Reward FX/";
    private const string MENU_SETUP = MENU_ROOT + "★ Setup Reward Fly FX (1 nút)";
    private const string MENU_DRY   = MENU_ROOT + "Đồng bộ icon vàng (DRY-RUN)";
    private const string MENU_APPLY = MENU_ROOT + "Đồng bộ icon vàng (APPLY)";

    // Asset library: dùng Assets/_Game/Resources (KHÔNG dùng Assets/Resources dù nó có
    // tồn tại — mọi asset của game nằm gọn dưới _Game, tránh vãi asset ra ngoài).
    private const string ResourcesFolder = "Assets/_Game/Resources";
    private const string LibraryAssetPath = ResourcesFolder + "/RewardIconLibrary.asset";

    // Bộ icon chính thức đội art bàn giao
    private const string ArtGoldPath = "Assets/Art/UI/Currency/icon_gold.png";
    private const string ArtGemPath  = "Assets/Art/UI/Currency/icon_gem.png";
    private const string ArtExpPath  = "Assets/Art/UI/Currency/icon_exp_star.png";

    /// <summary>
    /// TÊN các sprite VÀNG CŨ cần thay bằng icon chuẩn — SO KHỚP CHÍNH XÁC (phân biệt
    /// hoa/thường). Sếp/đội thấy tên khả nghi khác thì THÊM VÀO ĐÂY rồi chạy lại DRY-RUN.
    /// </summary>
    private static readonly string[] TenIconVangCu =
    {
        "icon_gold", "icon_coin", "gold", "Gold", "coin", "Coin",
        "gold_icon", "coin_icon", "xu", "Xu", "dong_xu", "gold_coin",
        // [V2] TÊN THẬT trong scene SCN_Farm (Lead tra guid 2026-09-01):
        // HUD Icon_Gold + Img_GoldIcon dùng "Icon_vang"; 1 Icon_Gold khác dùng ảnh "vàng" cũ.
        "Icon_vang", "icon_vang", "vang-removebg-preview", "vang"
    };

    // ════════════════════════════════════════════════════════════════════
    // MENU 1 — SETUP 1 NÚT
    // ════════════════════════════════════════════════════════════════════

    [MenuItem(MENU_SETUP)]
    public static void SetupRewardFlyFX()
    {
        var report = new StringBuilder();
        report.AppendLine("═══ [RewardFxSetupTool] BÁO CÁO SETUP ═══");

        // 1) Folder Resources — kiểm tra cả hai vị trí, nhưng CHỦ ĐÍCH dùng Assets/_Game/Resources
        bool coAssetsResources = AssetDatabase.IsValidFolder("Assets/Resources");
        bool coGameResources   = AssetDatabase.IsValidFolder(ResourcesFolder);
        report.AppendLine($"• Assets/Resources tồn tại: {coAssetsResources} · {ResourcesFolder} tồn tại: {coGameResources}");

        if (!coGameResources)
        {
            AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            report.AppendLine($"• Đã TẠO folder {ResourcesFolder} (Resources.Load nhận mọi folder tên 'Resources', chọn chỗ này để asset nằm gọn dưới _Game).");
        }

        // 2) Asset RewardIconLibrary — tạo nếu chưa có (idempotent)
        var lib = AssetDatabase.LoadAssetAtPath<RewardIconLibrary>(LibraryAssetPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<RewardIconLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryAssetPath);
            report.AppendLine($"• Đã TẠO asset {LibraryAssetPath}");
        }
        else
        {
            report.AppendLine($"• Asset {LibraryAssetPath} đã có — giữ nguyên.");
        }

        // 3) Gán sprite từ bộ icon art bàn giao (chỉ gán vào ô đang TRỐNG — không đè lựa chọn tay)
        bool libDoi = false;
        libDoi |= GanSpriteNeuTrong(report, ref lib.goldSprite, ArtGoldPath, "goldSprite");
        libDoi |= GanSpriteNeuTrong(report, ref lib.gemSprite,  ArtGemPath,  "gemSprite");
        libDoi |= GanSpriteNeuTrong(report, ref lib.expSprite,  ArtExpPath,  "expSprite");
        if (libDoi)
        {
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
        }

        // 4) Gắn RewardFlyFX lên canvas HUD
        var daCo = Object.FindFirstObjectByType<RewardFlyFX>(FindObjectsInactive.Include);
        if (daCo != null)
        {
            report.AppendLine($"• RewardFlyFX đã có sẵn trên '{daCo.gameObject.name}' — không gắn thêm.");
        }
        else
        {
            Canvas hudCanvas = TimCanvasHUD();
            if (hudCanvas != null)
            {
                Undo.AddComponent<RewardFlyFX>(hudCanvas.gameObject);
                report.AppendLine($"• Đã GẮN RewardFlyFX vào canvas '{hudCanvas.gameObject.name}' (Undo được, scene CHƯA save — Ctrl+S khi ưng).");
            }
            else
            {
                report.AppendLine("• ⚠ KHÔNG tìm thấy canvas HUD (không có 'Gold_Container' hay TownshipHUDController trong scene).");
                report.AppendLine("  → Mở scene farm chính (SCN_Farm) rồi chạy lại menu này, hoặc tự AddComponent RewardFlyFX lên Canvas_HUD.");
            }
        }

        report.AppendLine("• Lưu ý: RewardFlyFX lúc chạy sẽ TỰ TẮT CoinFlyFX/GemFlyFX (enabled=false, không destroy) để tránh FX nhân đôi.");
        report.AppendLine("═══ HẾT BÁO CÁO ═══");
        Debug.Log(report.ToString());

        EditorUtility.DisplayDialog("Reward FX Setup",
            "Xong! Xem bao cao chi tiet trong Console.\n\n" +
            "Nho: tool KHONG tu save scene — Ctrl+S neu ung.",
            "OK");
    }

    [MenuItem(MENU_SETUP, true)]
    private static bool ValidateSetup() => !EditorApplication.isPlaying;

    private static bool GanSpriteNeuTrong(StringBuilder report, ref Sprite field, string path, string tenField)
    {
        if (field != null)
        {
            report.AppendLine($"• {tenField} đã được gán ('{field.name}') — giữ nguyên.");
            return false;
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            field = sprite;
            report.AppendLine($"• Đã gán {tenField} ← {path}");
            return true;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
            report.AppendLine($"• ⚠ {path} tồn tại nhưng KHÔNG phải Sprite — đổi Texture Type = 'Sprite (2D and UI)' rồi chạy lại.");
        else
            report.AppendLine($"• {tenField}: chưa thấy {path} (đội art chưa bàn giao?) — RewardFlyFX sẽ dùng fallback, gán tay sau cũng được.");
        return false;
    }

    private static Canvas TimCanvasHUD()
    {
        // Ưu tiên: canvas chứa Gold_Container (đúng cụm tiền tệ HUD)
        var goldContainer = GameObject.Find("Gold_Container");
        if (goldContainer != null)
        {
            var c = goldContainer.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }

        // Kế đến: canvas chứa TownshipHUDController
        var hud = Object.FindFirstObjectByType<FarmGame.UI.TownshipHUDController>(FindObjectsInactive.Include);
        if (hud != null)
        {
            var c = hud.GetComponentInParent<Canvas>();
            if (c != null) return c.rootCanvas != null ? c.rootCanvas : c;
        }

        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    // MENU 2/3 — ĐỒNG BỘ ICON VÀNG
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Một chỗ đang dùng sprite vàng cũ: Image trong scene, hoặc trong prefab.</summary>
    private struct MucCanDoi
    {
        public string moTa;          // đường dẫn dễ đọc cho report
        public Image imageScene;     // != null nếu là object trong scene
        public string prefabPath;    // != null nếu nằm trong prefab
        public string duongDanTrongPrefab; // transform path bên trong prefab
    }

    [MenuItem(MENU_DRY)]
    public static void DongBoIconVangDryRun()
    {
        var lib = AssetDatabase.LoadAssetAtPath<RewardIconLibrary>(LibraryAssetPath);
        List<MucCanDoi> ds = QuetIconVangCu(lib);

        var report = new StringBuilder();
        report.AppendLine($"═══ [RewardFxSetupTool] ĐỒNG BỘ ICON VÀNG — DRY-RUN (không đổi gì) ═══");
        report.AppendLine($"Tên sprite bị coi là 'vàng cũ' (khớp chính xác): {string.Join(", ", TenIconVangCu)}");
        report.AppendLine($"goldSprite chuẩn hiện tại: {(lib != null && lib.goldSprite != null ? lib.goldSprite.name : "(CHƯA GÁN — APPLY sẽ từ chối chạy)")}");
        report.AppendLine($"Tìm thấy {ds.Count} chỗ cần đổi:");
        for (int i = 0; i < ds.Count; i++)
            report.AppendLine($"  {i + 1,3}. {ds[i].moTa}");
        if (ds.Count == 0)
            report.AppendLine("  (sạch — không chỗ nào dùng icon vàng cũ)");
        report.AppendLine("Duyệt xong danh sách thì chạy: " + MENU_APPLY);
        Debug.Log(report.ToString());
    }

    [MenuItem(MENU_APPLY)]
    public static void DongBoIconVangApply()
    {
        var lib = AssetDatabase.LoadAssetAtPath<RewardIconLibrary>(LibraryAssetPath);

        // RÀO CHẮN: chưa có goldSprite chuẩn thì không có gì để đổi SANG — từ chối.
        if (lib == null || lib.goldSprite == null)
        {
            Debug.LogError("[RewardFxSetupTool] TỪ CHỐI APPLY: RewardIconLibrary.goldSprite CHƯA được gán. " +
                           $"Chạy '{MENU_SETUP}' (sau khi art bàn giao icon_gold.png) hoặc gán tay vào {LibraryAssetPath} rồi thử lại.");
            EditorUtility.DisplayDialog("Đồng bộ icon vàng",
                "TU CHOI: goldSprite trong RewardIconLibrary chua duoc gan.\n\n" +
                "Gan icon vang chuan truoc roi chay lai.", "OK");
            return;
        }

        List<MucCanDoi> ds = QuetIconVangCu(lib);
        if (ds.Count == 0)
        {
            Debug.Log("[RewardFxSetupTool] APPLY: không có gì để đổi — mọi Image đã dùng icon chuẩn.");
            return;
        }

        bool dongY = EditorUtility.DisplayDialog("Đồng bộ icon vàng (APPLY)",
            $"Se doi {ds.Count} Image sang goldSprite chuan '{lib.goldSprite.name}'.\n\n" +
            "Thay doi trong SCENE: undo duoc (Ctrl+Z), khong tu save scene.\n" +
            "Thay doi trong PREFAB: GHI THANG VAO FILE, KHONG undo duoc.\n\n" +
            "Da duyet DRY-RUN chua?",
            "Doi luon", "Huy");
        if (!dongY) return;

        int doiScene = 0, doiPrefab = 0;
        var report = new StringBuilder();
        report.AppendLine("═══ [RewardFxSetupTool] ĐỒNG BỘ ICON VÀNG — APPLY ═══");

        // 1) Scene: đổi với Undo, không save scene
        foreach (var muc in ds)
        {
            if (muc.imageScene == null) continue;
            Undo.RecordObject(muc.imageScene, "Đồng bộ icon vàng");
            muc.imageScene.sprite = lib.goldSprite;
            EditorUtility.SetDirty(muc.imageScene);
            doiScene++;
            report.AppendLine($"  [scene]  {muc.moTa}");
        }

        // 2) Prefab: gom theo file, sửa qua LoadPrefabContents/SavePrefabAsset
        var theoPrefab = new Dictionary<string, List<string>>();
        foreach (var muc in ds)
        {
            if (muc.prefabPath == null) continue;
            if (!theoPrefab.TryGetValue(muc.prefabPath, out var paths))
                theoPrefab[muc.prefabPath] = paths = new List<string>();
            paths.Add(muc.duongDanTrongPrefab);
        }

        foreach (var kv in theoPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(kv.Key);
            try
            {
                bool daDoi = false;
                var images = root.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.sprite == null || img.sprite == lib.goldSprite) continue;
                    if (!LaTenVangCu(img.sprite.name)) continue;

                    img.sprite = lib.goldSprite;
                    daDoi = true;
                    doiPrefab++;
                    report.AppendLine($"  [prefab] {kv.Key} :: {DuongDanTransform(img.transform, root.transform)}");
                }

                if (daDoi)
                    PrefabUtility.SaveAsPrefabAsset(root, kv.Key);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        report.AppendLine($"TỔNG: {doiScene} Image trong scene (undo được, scene chưa save) + {doiPrefab} Image trong prefab (đã ghi file).");
        Debug.Log(report.ToString());
    }

    [MenuItem(MENU_DRY, true)]
    [MenuItem(MENU_APPLY, true)]
    private static bool ValidateSync() => !EditorApplication.isPlaying;

    // ─────────────────────────── QUÉT ───────────────────────────

    private static bool LaTenVangCu(string tenSprite)
    {
        for (int i = 0; i < TenIconVangCu.Length; i++)
            if (string.Equals(tenSprite, TenIconVangCu[i], System.StringComparison.Ordinal))
                return true;
        return false;
    }

    private static List<MucCanDoi> QuetIconVangCu(RewardIconLibrary lib)
    {
        var ds = new List<MucCanDoi>();
        Sprite goldChuan = lib != null ? lib.goldSprite : null;

        // 1) Mọi Image trong scene đang mở (kể cả object đang tắt)
        var imagesScene = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var img in imagesScene)
        {
            if (img.sprite == null || (goldChuan != null && img.sprite == goldChuan)) continue;
            if (!LaTenVangCu(img.sprite.name)) continue;

            ds.Add(new MucCanDoi
            {
                imageScene = img,
                moTa = $"[scene] {DuongDanTransform(img.transform, null)} — sprite hiện tại: '{img.sprite.name}'"
            });
        }

        // 2) Mọi prefab dưới Assets/_Game (chỉ ĐỌC — không sửa gì ở bước quét)
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var images = prefab.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.sprite == null || (goldChuan != null && img.sprite == goldChuan)) continue;
                if (!LaTenVangCu(img.sprite.name)) continue;

                string trong = DuongDanTransform(img.transform, prefab.transform);
                ds.Add(new MucCanDoi
                {
                    prefabPath = path,
                    duongDanTrongPrefab = trong,
                    moTa = $"[prefab] {path} :: {trong} — sprite hiện tại: '{img.sprite.name}'"
                });
            }
        }

        return ds;
    }

    private static string DuongDanTransform(Transform t, Transform root)
    {
        var sb = new StringBuilder(t.name);
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }
}
