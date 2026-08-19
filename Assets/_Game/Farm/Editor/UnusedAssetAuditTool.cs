#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Asset Audit — tìm và CÁCH LY ảnh thừa trong Assets/.
///
/// NGUYÊN TẮC AN TOÀN TUYỆT ĐỐI: tool KHÔNG BAO GIỜ XOÁ file. Ảnh bị nghi thừa chỉ được
/// MOVE (AssetDatabase.MoveAsset — GUID GIỮ NGUYÊN, reference nếu có sót vẫn tự theo)
/// vào Assets/_UNUSED_QUARANTINE/ giữ nguyên cây thư mục, kèm map hoàn tác. Muốn quay
/// đầu bất cứ lúc nào → menu "3. Restore ALL From Quarantine".
///
///   1. Scan Unused Images (Dry-Run)  — chỉ quét + xuất report CSV, không đụng file nào.
///   2. Quarantine Unused Images      — scan lại rồi MOVE ảnh thừa vào quarantine (có xác nhận).
///   3. Restore ALL From Quarantine   — đọc _restore_map.txt, move ngược từng ảnh về chỗ cũ.
///   4. Open Report CSV               — mở file report trong Finder/Explorer.
///
/// CÁCH TÍNH "THỪA": tập GỐC SỐNG = scene bật trong Build Settings + mọi asset trong
/// Resources/ + icon/cursor/splash của ProjectSettings + mọi SpriteAtlas. REACHABLE =
/// AssetDatabase.GetDependencies(gốc, đệ quy). Ảnh (png/jpg/jpeg/psd/tga) trong Assets/
/// KHÔNG thuộc REACHABLE và không dính loại trừ cứng → ứng viên thừa.
///
/// GIỚI HẠN PHẢI NHỚ: tool KHÔNG nhìn thấy ảnh được load bằng CODE
/// (Resources.Load / Addressables / ghép tên string). Resources/ đã loại trừ sẵn, nhưng
/// nếu có chỗ load sprite theo TÊN từ folder thường thì phải Play-test kỹ sau khi cách ly.
///
/// File này phải nằm trong một folder tên "Editor" (vd: Assets/_Game/Farm/Editor/).
/// </summary>
public static class UnusedAssetAuditTool
{
    // ─── Vùng user tự sửa ────────────────────────────────────────────────────

    /// <summary>
    /// Folder GIỮ LẠI THÊM ngoài các loại trừ cứng — mọi ảnh dưới các folder này KHÔNG
    /// BAO GIỜ bị đánh dấu thừa. Mặc định giữ nguyên Assets/_Game vì art gameplay đang
    /// được load bằng code ở nhiều chỗ (theo tên/id) mà tool không dò ra được.
    /// Khi đã tự tin (đã Play-test đủ), có thể bỏ dần từng dòng để quét sâu hơn.
    /// </summary>
    private static readonly string[] EXTRA_KEEP_FOLDERS =
    {
        "Assets/_Game",
    };

    // ─── Hằng số ─────────────────────────────────────────────────────────────

    private const string MENU_ROOT       = "Tools/Farm Game/Asset Audit/";
    private const string MENU_SCAN       = MENU_ROOT + "1. Scan Unused Images (Dry-Run)";
    private const string MENU_QUARANTINE = MENU_ROOT + "2. Quarantine Unused Images (MOVE — có hoàn tác)";
    private const string MENU_RESTORE    = MENU_ROOT + "3. Restore ALL From Quarantine";
    private const string MENU_OPEN       = MENU_ROOT + "4. Open Report CSV";

    private const string QUARANTINE_FOLDER = "Assets/_UNUSED_QUARANTINE";
    private const string RESTORE_MAP_PATH  = QUARANTINE_FOLDER + "/_restore_map.txt";
    private const string REPORT_CSV_PATH   = "Assets/_UNUSED_AUDIT_REPORT.csv";
    private const string LOG                = "[AssetAudit] ";
    private const string PROGRESS_TITLE     = "Asset Audit";

    /// <summary>
    /// Loại trừ CỨNG — path chứa bất kỳ mảnh nào dưới đây thì KHÔNG BAO GIỜ bị đánh dấu:
    ///   /Editor/          — icon/gizmo của tool editor, không dependency từ scene.
    ///   /Resources/       — load bằng code theo tên, GetDependencies không thấy.
    ///   /StreamingAssets/ — copy nguyên vào build, ngoài hệ dependency.
    ///   /TextMesh Pro/    — asset gói TMP, đụng vào là hỏng font.
    ///   /Settings/        — URP/render settings tham chiếu texture riêng.
    ///   /Plugins/         — asset của SDK bên thứ ba.
    /// </summary>
    private static readonly string[] EXCLUDE_PATH_PARTS =
    {
        "/Editor/",
        "/Resources/",
        "/StreamingAssets/",
        "/TextMesh Pro/",
        "/Settings/",
        "/Plugins/",
    };

    /// <summary>Đuôi file ảnh được xét — chỉ file ảnh nguồn, không đụng asset khác.</summary>
    private static readonly HashSet<string> IMAGE_EXTENSIONS = new HashSet<string>
    {
        ".png", ".jpg", ".jpeg", ".psd", ".tga",
    };

    private struct UnusedImage
    {
        public string path;       // đường dẫn asset (Assets/...)
        public long   sizeBytes;  // dung lượng file nguồn trên đĩa
        public string rootFolder; // folder gốc ngay dưới Assets/ (thietke, maptitle...)
    }

    // ─── 1. Scan (Dry-Run) ───────────────────────────────────────────────────

    [MenuItem(MENU_SCAN, false, 2001)]
    public static void ScanUnusedImages()
    {
        List<UnusedImage> unused = RunScan(writeCsv: true);

        long totalBytes = 0;
        foreach (UnusedImage img in unused) totalBytes += img.sizeBytes;

        EditorUtility.DisplayDialog(
            "Asset Audit — Dry-Run",
            $"Tìm thấy {unused.Count} ảnh không thấy tham chiếu ({ToMB(totalBytes):0.0} MB).\n\n" +
            $"CHƯA move/xoá gì cả. Mở report ({REPORT_CSV_PATH}) duyệt kỹ cột root_folder " +
            "trước khi chạy bước 2 (Quarantine).",
            "OK");
    }

    /// <summary>
    /// Quét toàn bộ project, trả về danh sách ảnh không thấy tham chiếu (sắp xếp nặng
    /// trước cho dễ duyệt). Bước Quarantine cũng gọi lại hàm này — KHÔNG tin cache của
    /// lần scan trước, vì giữa 2 lần bấm menu user có thể đã đổi asset.
    /// </summary>
    private static List<UnusedImage> RunScan(bool writeCsv)
    {
        var result = new List<UnusedImage>();

        try
        {
            // B1 — gom tập GỐC SỐNG
            EditorUtility.DisplayProgressBar(PROGRESS_TITLE,
                "Đang gom GỐC SỐNG (scene Build Settings + Resources + ProjectSettings + SpriteAtlas)...", 0.05f);
            List<string> roots = CollectRootPaths();

            if (roots.Count == 0)
                Debug.LogWarning(LOG + "Không gom được gốc sống nào (Build Settings trống?). " +
                                 "Kết quả scan sẽ đánh dấu GẦN NHƯ MỌI ảnh — kiểm tra lại trước khi tin.");

            // B2 — REACHABLE = mọi asset với tới được từ gốc (đệ quy)
            EditorUtility.DisplayProgressBar(PROGRESS_TITLE,
                $"Đang tính REACHABLE từ {roots.Count} gốc (GetDependencies đệ quy — hơi lâu với project lớn)...", 0.15f);
            var reachable = new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true));
            reachable.UnionWith(roots); // phòng hờ: gốc luôn phải nằm trong reachable

            // B3 — lọc ứng viên: mọi Texture2D trong Assets/ không reachable, không dính loại trừ
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            var seen = new HashSet<string>(); // FindAssets có thể trả trùng — chặn đếm đôi

            for (int i = 0; i < guids.Length; i++)
            {
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar(PROGRESS_TITLE,
                        $"Đang lọc ảnh ({i}/{guids.Length})...",
                        0.2f + 0.75f * i / Mathf.Max(1, guids.Length));

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !seen.Add(path)) continue;
                if (!IMAGE_EXTENSIONS.Contains(Path.GetExtension(path).ToLowerInvariant())) continue;
                if (IsExcluded(path)) continue;
                if (reachable.Contains(path)) continue;

                result.Add(new UnusedImage
                {
                    path       = path,
                    sizeBytes  = GetFileSizeBytes(path),
                    rootFolder = GetRootFolder(path),
                });
            }

            result.Sort((a, b) => b.sizeBytes.CompareTo(a.sizeBytes));

            if (writeCsv)
                WriteCsvReport(result);
            LogSummary(result, roots.Count, reachable.Count);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        return result;
    }

    // ─── Gốc sống ────────────────────────────────────────────────────────────

    private static List<string> CollectRootPaths()
    {
        var roots = new HashSet<string>();

        // 1. Mọi scene ĐANG BẬT trong Build Settings — dependency của scene là
        //    nguồn tham chiếu chính (prefab, sprite kéo vào Inspector, material...).
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled && !string.IsNullOrEmpty(scene.path) && File.Exists(scene.path))
                roots.Add(scene.path);

        // 2. TẤT CẢ asset trong bất kỳ folder Resources/ nào — load bằng code theo tên,
        //    dependency từ scene không thấy được nên phải coi là gốc sống hết.
        foreach (string path in AssetDatabase.GetAllAssetPaths())
            if (path.StartsWith("Assets/") && path.Contains("/Resources/"))
                roots.Add(path);

        // 3. ProjectSettings — icon app, cursor, splash screen. Các API PlayerSettings
        //    bọc try/catch: target group lạ có thể ném exception, tool audit KHÔNG được
        //    chết giữa chừng vì chuyện phụ.
        try
        {
            AddTextureRoot(roots, PlayerSettings.defaultCursor);

            // Icon mặc định (Default Icon): target Unknown CHỈ hỗ trợ IconKind.Application
            AddIcons(roots, NamedBuildTarget.Unknown, IconKind.Application);

            PlayerSettings.SplashScreenLogo[] logos = PlayerSettings.SplashScreen.logos;
            if (logos != null)
                foreach (PlayerSettings.SplashScreenLogo logo in logos)
                    if (logo.logo != null)
                        AddTextureRoot(roots, logo.logo.texture);

            if (PlayerSettings.SplashScreen.background != null)
                AddTextureRoot(roots, PlayerSettings.SplashScreen.background.texture);
            if (PlayerSettings.SplashScreen.backgroundPortrait != null)
                AddTextureRoot(roots, PlayerSettings.SplashScreen.backgroundPortrait.texture);

            // Icon của platform đang chọn — để CUỐI khối try: FromBuildTargetGroup ném
            // ArgumentException với group lạ, không được kéo sập các mục phía trên.
            AddIcons(roots, NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup),
                     IconKind.Any);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(LOG + "Không đọc được hết ProjectSettings (bỏ qua, vẫn scan tiếp): " + e.Message);
        }

        // 4. Mọi SpriteAtlas — atlas hay được load bằng code (SpriteAtlas.GetSprite theo
        //    tên), nên texture nào atlas gom vào đều phải coi là ĐANG DÙNG. Thà giữ thừa
        //    còn hơn cách ly nhầm nguồn của atlas.
        foreach (string guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" }))
            roots.Add(AssetDatabase.GUIDToAssetPath(guid));

        return new List<string>(roots);
    }

    private static void AddIcons(HashSet<string> roots, NamedBuildTarget target, IconKind kind)
    {
        Texture2D[] icons = PlayerSettings.GetIcons(target, kind);
        if (icons == null) return;
        foreach (Texture2D icon in icons)
            AddTextureRoot(roots, icon);
    }

    private static void AddTextureRoot(HashSet<string> roots, Texture texture)
    {
        if (texture == null) return;
        string path = AssetDatabase.GetAssetPath(texture);
        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/"))
            roots.Add(path);
    }

    // ─── Loại trừ ────────────────────────────────────────────────────────────

    private static bool IsExcluded(string path)
    {
        if (!path.StartsWith("Assets/")) return true;               // Packages/, ProjectSettings/...
        if (path.EndsWith(".meta")) return true;
        if (path.StartsWith(QUARANTINE_FOLDER + "/")) return true;  // đã cách ly rồi — đừng đụng nữa

        foreach (string part in EXCLUDE_PATH_PARTS)
            if (path.Contains(part)) return true;

        foreach (string keep in EXTRA_KEEP_FOLDERS)
        {
            if (string.IsNullOrEmpty(keep)) continue;
            string prefix = keep.EndsWith("/") ? keep : keep + "/";
            if (path.StartsWith(prefix)) return true;
        }

        return false;
    }

    // ─── Report ──────────────────────────────────────────────────────────────

    private static void WriteCsvReport(List<UnusedImage> unused)
    {
        var sb = new StringBuilder();
        sb.AppendLine("path,size_KB,root_folder");
        foreach (UnusedImage img in unused)
        {
            // Bọc ngoặc kép + escape: path có thể chứa dấu phẩy/khoảng trắng ("Test nước"...)
            string kb = (img.sizeBytes / 1024f).ToString("0.0", CultureInfo.InvariantCulture);
            sb.Append('"').Append(img.path.Replace("\"", "\"\"")).Append("\",")
              .Append(kb).Append(",\"").Append(img.rootFolder.Replace("\"", "\"\"")).Append('"')
              .AppendLine();
        }

        // UTF-8 CÓ BOM — Excel mở trực tiếp mới hiện đúng tên folder tiếng Việt
        File.WriteAllText(ToAbsolutePath(REPORT_CSV_PATH), sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.ImportAsset(REPORT_CSV_PATH);
    }

    private static void LogSummary(List<UnusedImage> unused, int rootCount, int reachableCount)
    {
        long totalBytes = 0;
        foreach (UnusedImage img in unused) totalBytes += img.sizeBytes;

        Debug.Log(LOG + $"Scan xong: {unused.Count} ảnh KHÔNG thấy tham chiếu / {ToMB(totalBytes):0.0} MB " +
                  $"(gốc sống: {rootCount} asset, reachable: {reachableCount} asset). Report: {REPORT_CSV_PATH}");

        // Thống kê theo folder gốc — nhìn nhanh "đống thừa" nằm ở đâu (thietke? maptitle?)
        var folderBytes = new Dictionary<string, long>();
        var folderCount = new Dictionary<string, int>();
        foreach (UnusedImage img in unused)
        {
            folderBytes.TryGetValue(img.rootFolder, out long bytes);
            folderBytes[img.rootFolder] = bytes + img.sizeBytes;
            folderCount.TryGetValue(img.rootFolder, out int count);
            folderCount[img.rootFolder] = count + 1;
        }

        if (folderCount.Count > 0)
        {
            var keys = new List<string>(folderBytes.Keys);
            keys.Sort((a, b) => folderBytes[b].CompareTo(folderBytes[a]));
            var line = new StringBuilder(LOG + "Theo folder gốc: ");
            foreach (string key in keys)
                line.Append($"{key}: {folderCount[key]} ảnh ({ToMB(folderBytes[key]):0.0} MB); ");
            Debug.Log(line.ToString());
        }

        Debug.LogWarning(LOG + "LƯU Ý AN TOÀN: tool KHÔNG nhìn thấy ảnh được load bằng CODE " +
                         "(Resources.Load / Addressables / ghép tên string). Folder Resources/ đã được " +
                         "loại trừ sẵn, nhưng nếu project có chỗ load sprite theo TÊN từ folder thường " +
                         "thì phải Play-test kỹ sau khi cách ly. Thiếu ảnh → chạy '3. Restore ALL From Quarantine'.");
    }

    // ─── 2. Quarantine (MOVE — có hoàn tác) ──────────────────────────────────

    [MenuItem(MENU_QUARANTINE, false, 2002)]
    public static void QuarantineUnusedImages()
    {
        // Scan LẠI ngay tại chỗ — không tin kết quả cũ, asset có thể đã đổi từ lần scan trước
        List<UnusedImage> unused = RunScan(writeCsv: true);
        if (unused.Count == 0)
        {
            EditorUtility.DisplayDialog("Asset Audit", "Scan ra 0 kết quả — không có ảnh nào để cách ly.", "OK");
            return;
        }

        long totalBytes = 0;
        foreach (UnusedImage img in unused) totalBytes += img.sizeBytes;

        bool confirmed = EditorUtility.DisplayDialog(
            "Cách ly ảnh thừa (KHÔNG XOÁ)",
            $"Sẽ MOVE {unused.Count} ảnh ({ToMB(totalBytes):0.0} MB) vào:\n{QUARANTINE_FOLDER}/\n" +
            "(giữ nguyên cây thư mục gốc)\n\n" +
            "- KHÔNG xoá file nào. GUID không đổi nên reference nếu có sót vẫn tự theo.\n" +
            "- Hoàn tác bất cứ lúc nào: menu '3. Restore ALL From Quarantine'.\n\n" +
            "Khuyên: commit git TRƯỚC khi bấm.",
            "Cách ly (Move)", "Huỷ");
        if (!confirmed) return;

        // Tạo TRƯỚC toàn bộ folder đích — gọi CreateFolder khi đang StartAssetEditing
        // dễ dính lỗi folder chưa import xong làm MoveAsset fail hàng loạt.
        EnsureFolder(QUARANTINE_FOLDER);
        foreach (UnusedImage img in unused)
            EnsureFolder(GetParentFolder(BuildQuarantinePath(img.path)));

        var movedLines = new List<string>();
        int failed = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < unused.Count; i++)
            {
                string oldPath = unused[i].path;
                string newPath = BuildQuarantinePath(oldPath);
                EditorUtility.DisplayProgressBar(PROGRESS_TITLE + " — Quarantine",
                    $"({i + 1}/{unused.Count}) {oldPath}", (i + 1) / (float)unused.Count);

                string error = AssetDatabase.MoveAsset(oldPath, newPath);
                if (string.IsNullOrEmpty(error))
                {
                    movedLines.Add(newPath + "|" + oldPath);
                }
                else
                {
                    failed++;
                    Debug.LogError(LOG + $"Move FAIL '{oldPath}' → '{newPath}': {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        // Map hoàn tác: mỗi dòng "newPath|oldPath". APPEND chứ không ghi đè —
        // giữ được map của những lần cách ly trước chưa restore.
        if (movedLines.Count > 0)
            File.AppendAllLines(ToAbsolutePath(RESTORE_MAP_PATH), movedLines);

        AssetDatabase.Refresh();

        Debug.Log(LOG + $"Cách ly xong: {movedLines.Count} moved, {failed} fail. Map hoàn tác: {RESTORE_MAP_PATH}");
        EditorUtility.DisplayDialog(
            "Asset Audit",
            $"Đã cách ly {movedLines.Count} ảnh vào {QUARANTINE_FOLDER}/ (fail: {failed}).\n\n" +
            "Bây giờ Play-test các scene chính. Thấy sprite trắng/None/hồng → " +
            "chạy '3. Restore ALL From Quarantine'.",
            "OK");
    }

    /// <summary>Assets/thietke/a/b.png → Assets/_UNUSED_QUARANTINE/thietke/a/b.png.</summary>
    private static string BuildQuarantinePath(string assetPath)
        => QUARANTINE_FOLDER + "/" + assetPath.Substring("Assets/".Length);

    // ─── 3. Restore ──────────────────────────────────────────────────────────

    [MenuItem(MENU_RESTORE, false, 2003)]
    public static void RestoreAllFromQuarantine()
    {
        string mapFullPath = ToAbsolutePath(RESTORE_MAP_PATH);
        if (!File.Exists(mapFullPath))
        {
            EditorUtility.DisplayDialog("Asset Audit",
                $"Không tìm thấy map hoàn tác:\n{RESTORE_MAP_PATH}\n\nChưa từng cách ly, hoặc đã restore hết.", "OK");
            return;
        }

        // Parse map — dòng hỏng thì cảnh báo rồi bỏ qua, không để 1 dòng lỗi chặn cả đợt restore
        var entries = new List<string[]>();
        foreach (string line in File.ReadAllLines(mapFullPath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split('|');
            if (parts.Length == 2 && parts[0].StartsWith("Assets/") && parts[1].StartsWith("Assets/"))
                entries.Add(parts);
            else
                Debug.LogWarning(LOG + "Bỏ qua dòng map không hợp lệ: " + line);
        }

        if (entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Asset Audit", "Map hoàn tác rỗng — không có gì để restore.", "OK");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Restore từ Quarantine",
            $"Sẽ move {entries.Count} ảnh từ {QUARANTINE_FOLDER}/ về đúng vị trí cũ.",
            "Restore", "Huỷ");
        if (!confirmed) return;

        // Tạo trước folder đích cũ — phòng khi user đã dọn/đổi tên folder gốc
        foreach (string[] entry in entries)
            EnsureFolder(GetParentFolder(entry[1]));

        var remaining = new List<string>();
        int restored = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < entries.Count; i++)
            {
                string newPath = entries[i][0]; // đang nằm trong quarantine
                string oldPath = entries[i][1]; // vị trí gốc
                EditorUtility.DisplayProgressBar(PROGRESS_TITLE + " — Restore",
                    $"({i + 1}/{entries.Count}) {oldPath}", (i + 1) / (float)entries.Count);

                string error = AssetDatabase.MoveAsset(newPath, oldPath);
                if (string.IsNullOrEmpty(error))
                {
                    restored++;
                }
                else
                {
                    // Chưa về được (vd: chỗ cũ đã có file trùng tên) → GIỮ dòng trong map
                    remaining.Add(newPath + "|" + oldPath);
                    Debug.LogError(LOG + $"Restore FAIL '{newPath}' → '{oldPath}': {error}");
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        // Ghi lại map: dòng restore xong thì xoá khỏi map, dòng fail giữ lại cho lần sau
        File.WriteAllLines(mapFullPath, remaining);
        AssetDatabase.Refresh();

        Debug.Log(LOG + $"Restore xong: {restored} ảnh về chỗ cũ, {remaining.Count} dòng còn kẹt trong map.");
        if (remaining.Count == 0)
            Debug.Log(LOG + $"Map đã rỗng — {QUARANTINE_FOLDER}/ giờ chỉ còn vỏ folder. " +
                      "Muốn dọn thì tự xoá tay (tool KHÔNG tự xoá bất cứ thứ gì).");
    }

    // ─── 4. Open Report ──────────────────────────────────────────────────────

    [MenuItem(MENU_OPEN, false, 2004)]
    public static void OpenReportCsv()
    {
        string fullPath = ToAbsolutePath(REPORT_CSV_PATH);
        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Asset Audit",
                "Chưa có report. Chạy '1. Scan Unused Images (Dry-Run)' trước.", "OK");
            return;
        }
        EditorUtility.RevealInFinder(fullPath);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static float ToMB(long bytes) => bytes / (1024f * 1024f);

    private static long GetFileSizeBytes(string assetPath)
    {
        string fullPath = ToAbsolutePath(assetPath);
        return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
    }

    /// <summary>"Assets/x.png" → đường dẫn tuyệt đối trên đĩa (dataPath = &lt;proj&gt;/Assets).</summary>
    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        return Path.Combine(projectRoot, assetPath);
    }

    /// <summary>Folder gốc ngay dưới Assets/ — dùng cho cột root_folder trong CSV.</summary>
    private static string GetRootFolder(string assetPath)
    {
        string rel = assetPath.Substring("Assets/".Length);
        int slash = rel.IndexOf('/');
        return slash >= 0 ? rel.Substring(0, slash) : "(Assets gốc)";
    }

    private static string GetParentFolder(string assetPath)
    {
        int slash = assetPath.LastIndexOf('/');
        return slash > 0 ? assetPath.Substring(0, slash) : "Assets";
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

        string parent = GetParentFolder(folder);
        string leaf   = folder.Substring(folder.LastIndexOf('/') + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
