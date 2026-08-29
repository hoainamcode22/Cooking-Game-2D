#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bộ công cụ TỐI ƯU TEXTURE — Sếp duyệt 2026-08-29 (mục 8 trong danh sách tối ưu).
///
/// VÌ SAO: quét 570 file PNG trong Assets/ thấy 569 file để maxTextureSize = 2048 và
/// 324 file có platform setting KHÔNG NÉN. Cộng lại, nếu nạp hết thì card đồ hoạ phải
/// giữ khoảng 433 MB texture ở dạng RGBA32. Nén chuẩn còn khoảng 108 MB (1/4).
/// Trên máy dùng card tích hợp hoặc card 2 GB thì riêng con số này đã đủ gây giật.
///
/// NGUYÊN TẮC AN TOÀN của bộ tool này:
///   1. Menu "1." chỉ ĐỌC, không sửa gì — chạy trước để biết đang mất bao nhiêu.
///   2. Menu "2." và "3." đều hỏi xác nhận, làm theo TỪNG THƯ MỤC, và in ra danh sách
///      đúng những file đã đổi ⇒ muốn quay lại thì biết chính xác phải sửa file nào.
///   3. KHÔNG đụng tới sprite đã cấu hình tay (có platform override riêng) — bỏ qua và
///      liệt kê để Sếp tự quyết.
///   4. Toàn bộ .meta đã được sao lưu ở production/backup_train_2026-08-29/ trước khi
///      chạy — hỏng chỗ nào chép đè lại là xong.
///
/// LƯU Ý VỀ CHẤT LƯỢNG: nén DXT/BC có thể làm loang nhẹ ở vùng chuyển màu mượt và
/// viền alpha mềm. Vì vậy nên làm theo THỨ TỰ: nén thư mục art thế giới (cây cối, nhà,
/// đất) trước, nhìn game, ưng thì mới nén tiếp thư mục UI/popup. ĐỪNG nén tất cả một lần.
/// </summary>
public static class ToiUuTexture_2026_08_29
{
    private const string Root = "Tools/Tối ưu/";

    // Các nhóm thư mục, xếp theo THỨ TỰ AN TOÀN GIẢM DẦN.
    private static readonly (string label, string[] folders)[] Groups =
    {
        ("A · Art thế giới (an toàn nhất)", new[]
        {
            "Assets/Assetsgame/hatgiong", "Assets/Assetsgame/Hoa",
            "Assets/Assetsgame/bocaycoitrangtri", "Assets/Assetsgame/Nhà",
            "Assets/Assetsgame/Taulua",
        }),
        ("B · Nhân vật & vật phẩm", new[]
        {
            "Assets/Assetsgame/NPC_Game", "Assets/Assetsgame/Thịt", "Assets/Assetsgame/Bò",
            "Assets/Assetsgame/NguyeenLieu_Giavi", "Assets/Assetsgame/Món ăn",
        }),
        ("C · UI & popup (làm CUỐI, dễ lộ nhất)", new[]
        {
            "Assets/Assetsgame/popup", "Assets/Assetsgame/Fantasy Wooden GUI  Free",
            "Assets/_Game/GeneratedUI", "Assets/_Game/Farm/Art",
        }),
    };

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(Root + "1. Báo cáo texture (CHỈ ĐỌC, không sửa gì)", false, 1)]
    private static void BaoCao()
    {
        var sb = new StringBuilder();
        long grandVram = 0; int grandCount = 0;

        foreach (var g in Groups)
        {
            long vram = 0; int n = 0, uncompressed = 0, oversized = 0;
            foreach (string guid in FindTextures(g.folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (imp == null || tex == null) continue;

                n++;
                vram += (long)tex.width * tex.height * 4;
                if (imp.textureCompression == TextureImporterCompression.Uncompressed) uncompressed++;
                if (Mathf.Max(tex.width, tex.height) > 1024) oversized++;
            }
            grandVram += vram; grandCount += n;
            sb.AppendLine($"{g.label}");
            sb.AppendLine($"    {n} texture · ~{vram / 1048576f:F0} MB nếu không nén " +
                          $"(nén còn ~{vram / 4f / 1048576f:F0} MB)");
            sb.AppendLine($"    chưa nén: {uncompressed} · ảnh quá khổ (>1024px): {oversized}");
        }

        sb.AppendLine();
        sb.AppendLine($"TỔNG {grandCount} texture · ~{grandVram / 1048576f:F0} MB → " +
                      $"nén còn ~{grandVram / 4f / 1048576f:F0} MB");
        sb.AppendLine();
        sb.AppendLine("15 texture ăn VRAM nhất:");
        foreach (var row in TopVram(15)) sb.AppendLine("    " + row);

        Debug.Log("[Tối ưu texture] BÁO CÁO\n" + sb);
        EditorUtility.DisplayDialog("Báo cáo texture",
            $"{grandCount} texture · ~{grandVram / 1048576f:F0} MB\n" +
            $"Nếu bật nén: còn ~{grandVram / 4f / 1048576f:F0} MB\n\n" +
            "Chi tiết đầy đủ đã in ra Console.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(Root + "2. Bật nén texture — nhóm A (art thế giới)", false, 20)]
    private static void NenA() => Nen(0);

    [MenuItem(Root + "2. Bật nén texture — nhóm B (nhân vật, vật phẩm)", false, 21)]
    private static void NenB() => Nen(1);

    [MenuItem(Root + "2. Bật nén texture — nhóm C (UI, popup)", false, 22)]
    private static void NenC() => Nen(2);

    private static void Nen(int groupIndex)
    {
        var g = Groups[groupIndex];
        var targets = new List<string>();
        var skipped = new List<string>();

        foreach (string guid in FindTextures(g.folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            // Có override riêng cho platform = ai đó đã chỉnh tay ⇒ KHÔNG đụng.
            if (imp.GetPlatformTextureSettings("Standalone").overridden)
            { skipped.Add(path); continue; }

            if (imp.textureCompression != TextureImporterCompression.Uncompressed) continue;
            targets.Add(path);
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Tối ưu texture",
                $"{g.label}\n\nKhông có texture nào cần bật nén.\n" +
                $"(bỏ qua {skipped.Count} file đã chỉnh tay)", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Xác nhận bật nén",
                $"{g.label}\n\nSẽ đổi {targets.Count} texture sang NÉN (Normal Quality).\n" +
                $"Bỏ qua {skipped.Count} file đã có override riêng.\n\n" +
                "Unity sẽ import lại — có thể mất vài phút.\n" +
                "Sau khi xong hãy NHÌN GAME. Không ưng thì chép .meta từ\n" +
                "production/backup_train_2026-08-29/ đè lại.",
                "Làm đi", "Huỷ"))
            return;

        int done = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string path in targets)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.textureCompression        = TextureImporterCompression.Compressed;
                imp.compressionQuality        = 50;
                imp.crunchedCompression       = false;
                imp.SaveAndReimport();
                done++;
                EditorUtility.DisplayProgressBar("Bật nén texture", path, done / (float)targets.Count);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[Tối ưu texture] {g.label}: đã bật nén {done} texture.\n" +
                  string.Join("\n", targets));
        EditorUtility.DisplayDialog("Xong", $"{g.label}\nĐã bật nén {done} texture.\n" +
                                            "Danh sách đầy đủ ở Console.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem(Root + "3. Hạ maxTextureSize cho ảnh quá khổ (>1024px)", false, 40)]
    private static void HaKichThuoc()
    {
        var all = new List<string>();
        foreach (var g in Groups) all.AddRange(g.folders);

        var targets = new List<string>();
        foreach (string guid in FindTextures(all.ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (imp == null || tex == null) continue;
            if (Mathf.Max(tex.width, tex.height) <= 1024) continue;
            if (imp.maxTextureSize <= 1024) continue;
            targets.Add($"{path}|{tex.width}x{tex.height}");
        }

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Tối ưu texture", "Không còn ảnh nào quá khổ.", "OK");
            return;
        }

        Debug.Log($"[Tối ưu texture] {targets.Count} ảnh quá khổ:\n" + string.Join("\n", targets));

        if (!EditorUtility.DisplayDialog("Xác nhận hạ kích thước",
                $"{targets.Count} ảnh đang lớn hơn 1024px.\n\n" +
                "Sẽ đặt maxTextureSize = 1024 cho chúng.\n" +
                "ẢNH SẼ HƠI MỀM ĐI nếu nó thật sự được hiển thị to.\n" +
                "Danh sách đã in ra Console — Sếp xem trước rồi hãy bấm.\n\n" +
                "Không ưng thì chép .meta từ backup đè lại.",
                "Làm đi", "Huỷ"))
            return;

        int done = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string row in targets)
            {
                string path = row.Split('|')[0];
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
                done++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Xong", $"Đã hạ {done} ảnh về 1024px.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static string[] FindTextures(string[] folders)
    {
        var real = new List<string>();
        foreach (string f in folders)
            if (AssetDatabase.IsValidFolder(f)) real.Add(f);
        if (real.Count == 0) return new string[0];
        return AssetDatabase.FindAssets("t:Texture2D", real.ToArray());
    }

    private static List<string> TopVram(int count)
    {
        var all = new List<string>();
        foreach (var g in Groups) all.AddRange(g.folders);

        var rows = new List<(long vram, string line)>();
        foreach (string guid in FindTextures(all.ToArray()))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            long v = (long)tex.width * tex.height * 4;
            rows.Add((v, $"{v / 1048576f,6:F1} MB  {tex.width}x{tex.height}  {path}"));
        }
        rows.Sort((a, b) => b.vram.CompareTo(a.vram));

        var outp = new List<string>();
        for (int i = 0; i < Mathf.Min(count, rows.Count); i++) outp.Add(rows[i].line);
        return outp;
    }
}
#endif
