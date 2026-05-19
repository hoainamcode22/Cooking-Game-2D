using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chuẩn hóa trường itemId trong tất cả InventoryItemData assets để khớp với
/// định dạng ID mà WarehousePopupUI gửi sang Bếp (chữ thường, không tiền tố).
/// Ví dụ: "Item_CaChua" / "ING_Mushroom" → "cachua" / "mushroom"
/// Menu: Tools > Cooking Item IDs > ...
/// </summary>
public static class NormalizeCookingItemIDs
{
    // Các tiền tố cần bỏ (so sánh không phân biệt hoa/thường)
    private static readonly string[] StripPrefixes = { "ING_", "SEA_", "Item_" };

    // ─────────────────────────────────────────────────────────────
    //  MENU ENTRIES
    // ─────────────────────────────────────────────────────────────

    [MenuItem("Tools/Cooking Item IDs/1 - Preview Changes (Dry Run)", priority = 1)]
    private static void Preview()
    {
        ProcessAssets(dryRun: true);
    }

    [MenuItem("Tools/Cooking Item IDs/2 - Apply: Normalize All itemIds", priority = 2)]
    private static void Apply()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            "Chuẩn hóa Cooking Item IDs",
            "Tool sẽ ghi đè trường 'itemId' của TẤT CẢ InventoryItemData assets trong project.\n\n" +
            "Quy tắc:\n" +
            "  • Xóa tiền tố: ING_ / SEA_ / Item_\n" +
            "  • Chuyển toàn bộ thành chữ thường\n\n" +
            "Hãy chạy 'Preview Changes' trước nếu chưa kiểm tra.\n" +
            "Tiến hành?",
            "Đồng ý, chuẩn hóa ngay", "Hủy");

        if (confirmed)
            ProcessAssets(dryRun: false);
    }

    // ─────────────────────────────────────────────────────────────
    //  CORE LOGIC
    // ─────────────────────────────────────────────────────────────

    private static void ProcessAssets(bool dryRun)
    {
        string[] guids = AssetDatabase.FindAssets("t:InventoryItemData");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[NormalizeCookingItemIDs] Không tìm thấy InventoryItemData asset nào trong project.");
            return;
        }

        var toChange = new List<(string path, string oldId, string newId)>();
        var alreadyOk = new List<string>();
        var emptyId   = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);

            if (data == null)
                continue;

            if (string.IsNullOrEmpty(data.itemId))
            {
                // itemId rỗng → thử đặt tự động từ tên file asset
                string autoId = NormalizeId(data.name);
                emptyId.Add($"  [EMPTY → AUTO] {path}\n             Filename: \"{data.name}\" → sẽ đặt: \"{autoId}\"");

                if (!dryRun)
                {
                    data.itemId = autoId;
                    EditorUtility.SetDirty(data);
                }
                continue;
            }

            string normalized = NormalizeId(data.itemId);

            if (normalized == data.itemId)
            {
                alreadyOk.Add($"  [OK]     {path}  →  \"{data.itemId}\"");
            }
            else
            {
                toChange.Add((path, data.itemId, normalized));

                if (!dryRun)
                {
                    data.itemId = normalized;
                    EditorUtility.SetDirty(data);
                }
            }
        }

        // ─── Lưu nếu Apply ───
        if (!dryRun && (toChange.Count + emptyId.Count) > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ─── In báo cáo ra Console ───
        PrintReport(dryRun, guids.Length, toChange, alreadyOk, emptyId);
    }

    // ─────────────────────────────────────────────────────────────
    //  NORMALIZE RULE
    //  Bỏ đúng 1 tiền tố đầu tiên khớp, sau đó lowercase toàn bộ.
    // ─────────────────────────────────────────────────────────────
    private static string NormalizeId(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        string result = raw.Trim();

        foreach (string prefix in StripPrefixes)
        {
            if (result.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(prefix.Length);
                break;
            }
        }

        return result.ToLowerInvariant();
    }

    // ─────────────────────────────────────────────────────────────
    //  REPORT
    // ─────────────────────────────────────────────────────────────
    private static void PrintReport(
        bool dryRun,
        int totalScanned,
        List<(string path, string oldId, string newId)> toChange,
        List<string> alreadyOk,
        List<string> emptyId)
    {
        var sb = new System.Text.StringBuilder();
        string mode = dryRun ? "DRY RUN (Xem trước)" : "ĐÃ ÁP DỤNG";

        sb.AppendLine($"[NormalizeCookingItemIDs] ── {mode} ──────────────────");
        sb.AppendLine($"  Đã quét  : {totalScanned} assets");
        sb.AppendLine($"  Đã đúng  : {alreadyOk.Count}");
        sb.AppendLine($"  Sẽ/Đã sửa: {toChange.Count}");
        sb.AppendLine($"  ID rỗng  : {emptyId.Count}");
        sb.AppendLine();

        if (toChange.Count > 0)
        {
            sb.AppendLine($"── {(dryRun ? "SẼ THAY ĐỔI" : "ĐÃ THAY ĐỔI")} ({toChange.Count}) ──");
            foreach (var (path, oldId, newId) in toChange)
                sb.AppendLine($"  [CHANGE] {path}\n           \"{oldId}\"  →  \"{newId}\"");
            sb.AppendLine();
        }

        if (emptyId.Count > 0)
        {
            sb.AppendLine($"── ID RỖNG → TỰ ĐẶT TỪ TÊN FILE ({emptyId.Count}) ──");
            foreach (string s in emptyId)
                sb.AppendLine(s);
            sb.AppendLine();
        }

        if (alreadyOk.Count > 0)
        {
            sb.AppendLine($"── ĐÃ ĐÚNG ({alreadyOk.Count}) ──");
            foreach (string s in alreadyOk)
                sb.AppendLine(s);
        }

        sb.AppendLine("────────────────────────────────────────────────────");

        if (toChange.Count + emptyId.Count == 0)
            Debug.Log(sb.ToString());
        else
            Debug.Log(sb.ToString());

        if (!dryRun && (toChange.Count + emptyId.Count) > 0)
        {
            EditorUtility.DisplayDialog(
                "Hoàn tất",
                $"Đã chuẩn hóa {toChange.Count + emptyId.Count} itemId thành công.\n" +
                "Xem Console để biết chi tiết.",
                "OK");
        }
        else if (dryRun)
        {
            EditorUtility.DisplayDialog(
                "Preview xong",
                $"Tìm thấy {toChange.Count} ID cần sửa, {emptyId.Count} ID rỗng.\n\n" +
                "Chạy 'Apply: Normalize All itemIds' để áp dụng.\n" +
                "Xem Console để xem toàn bộ danh sách.",
                "OK");
        }
    }
}
