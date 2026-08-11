using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  DEV-B · F1 — SOÁT & CẤP LẠI plotId DUY NHẤT
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO cần tool này thay vì sửa tay: `PlotController.SaveKey` là
/// "PLOT_NORMAL_{plotId}" (KHÔNG chứa category). Hai ô đất trùng plotId sẽ ghi/đọc
/// CÙNG một khoá PlayerPrefs — trồng ô này, thoát vào lại thì ô kia hiện cây. Đó là
/// lỗi MẤT DỮ LIỆU, và nó đã từng xảy ra với 8 cặp trong `SCN_Farm`.
///
/// Trùng id rất dễ tái phát: chỉ cần copy-paste một ô đất trong Hierarchy là xong.
/// Nên phải có một nút soát lại chạy trong 1 giây, thay vì đọc tay 38 component.
///
/// ⚠️ NÚT "CẤP LẠI ID" chỉ đổi những ô BỊ TRÙNG và cấp id mới từ 101 trở lên —
/// KHÔNG đánh số lại toàn bộ. Vì đổi plotId = ĐỔI KHOÁ LƯU: ô nào giữ được id cũ thì
/// người chơi giữ nguyên cây đang trồng, không cần chuyển đổi gì.
/// Sau khi cấp lại, PHẢI thêm cặp (id mới → id cũ) vào `PlotController.LegacyPlotIdMap`,
/// nếu không người chơi đang có save sẽ mất cây ở những ô vừa đổi.
/// </summary>
public static class PlotIdAuditTool
{
    private const int NewIdStart = 101;

    [MenuItem("Tools/Farm/Ô đất/1 · Soát plotId trùng", priority = 300)]
    private static void Audit()
    {
        List<PlotController> plots = CollectPlots();
        Dictionary<int, List<PlotController>> byId = GroupById(plots);

        var sb = new StringBuilder();
        sb.AppendLine($"Tìm thấy {plots.Count} PlotController · {byId.Count} plotId khác nhau.");

        int dupGroups = 0;
        foreach (var kv in byId)
        {
            if (kv.Value.Count <= 1) continue;
            dupGroups++;
            sb.AppendLine($"  🔴 plotId {kv.Key} — {kv.Value.Count} ô dùng chung khoá PLOT_*_{kv.Key}:");
            foreach (PlotController p in kv.Value)
                sb.AppendLine($"       · {PathOf(p)}  (category {p.Category})");
        }

        if (dupGroups == 0)
            sb.AppendLine("  ✅ Không có id trùng.");

        Debug.Log("[PlotIdAudit]\n" + sb);
    }

    [MenuItem("Tools/Farm/Ô đất/2 · Cấp lại id cho ô TRÙNG (từ 101)", priority = 301)]
    private static void Reassign()
    {
        List<PlotController> plots = CollectPlots();
        Dictionary<int, List<PlotController>> byId = GroupById(plots);

        var used = new HashSet<int>(byId.Keys);
        var changes = new List<string>();

        foreach (var kv in byId)
        {
            // Ô ĐẦU TIÊN trong nhóm GIỮ id cũ — nhờ vậy đa số người chơi không phải
            // chuyển đổi save. Chỉ những ô còn lại mới nhận id mới.
            for (int i = 1; i < kv.Value.Count; i++)
            {
                int newId = NewIdStart;
                while (used.Contains(newId)) newId++;
                used.Add(newId);

                PlotController p = kv.Value[i];
                Undo.RecordObject(p, "Cấp lại plotId");
                changes.Add($"{PathOf(p)}: {kv.Key} → {newId}");
                p.SetPlotId(newId);
                EditorUtility.SetDirty(p);
            }
        }

        if (changes.Count == 0)
        {
            EditorUtility.DisplayDialog("Ô đất", "Không có id trùng — không phải sửa gì.", "OK");
            return;
        }

        var sb = new StringBuilder("Đã cấp lại id cho " + changes.Count + " ô:\n");
        foreach (string c in changes) sb.AppendLine("  " + c);
        sb.AppendLine();
        sb.AppendLine("⚠️ BẮT BUỘC: thêm các cặp (id mới → id cũ) vào PlotController.LegacyPlotIdMap,");
        sb.AppendLine("   nếu không người chơi đang có save sẽ mất cây ở những ô này.");
        Debug.LogWarning("[PlotIdAudit]\n" + sb);
        EditorUtility.DisplayDialog("Ô đất", sb.ToString(), "Đã hiểu");
    }

    private static List<PlotController> CollectPlots()
    {
        var plots = new List<PlotController>(
            Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        // Sắp theo đường dẫn Hierarchy để kết quả ỔN ĐỊNH giữa các lần chạy.
        // Không có bước này thì "ô đầu tiên giữ id cũ" mỗi lần lại là một ô khác,
        // và mỗi lần bấm tool là save của người chơi lại nhảy sang ô khác.
        plots.Sort((a, b) => string.CompareOrdinal(PathOf(a), PathOf(b)));
        return plots;
    }

    private static Dictionary<int, List<PlotController>> GroupById(List<PlotController> plots)
    {
        var byId = new Dictionary<int, List<PlotController>>();
        foreach (PlotController p in plots)
        {
            if (p == null) continue;
            if (!byId.TryGetValue(p.PlotId, out List<PlotController> list))
                byId[p.PlotId] = list = new List<PlotController>();
            list.Add(p);
        }
        return byId;
    }

    private static string PathOf(Component c)
    {
        if (c == null) return "(null)";

        var sb = new StringBuilder(c.name);
        Transform t = c.transform.parent;
        while (t != null)
        {
            sb.Insert(0, t.name + "/");
            t = t.parent;
        }
        return sb.ToString();
    }
}
