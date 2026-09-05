#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ★ CÔNG CỤ 2026-09-03 — Sếp báo "tới vùng bến tàu là map cứng đơ, FPS 10".
///
/// CHẨN ĐOÁN: 12/14 TilemapRenderer trong SCN_Farm đang để Mode = Individual.
/// Ở chế độ này Unity SẮP XẾP VÀ VẼ TỪNG Ô MỘT (không gộp chunk) — với ~27.000 ô
/// đang ở Individual, camera zoom xa là CPU phải sort vài chục nghìn sprite mỗi frame.
/// Vùng bến tàu nặng nhất vì nước + móng + cát + cầu tàu chồng lên nhau ở đó.
///
/// Mode = Chunk gộp ô thành mảng lớn ⇒ nhanh gấp nhiều lần. Chỉ lớp nào cần
/// tile xen kẽ Y-sort với nhân vật (hàng rào, đá cao) mới cần giữ Individual.
///
/// AN TOÀN: DRY-RUN in báo cáo trước · APPLY có Undo (Ctrl+Z) · idempotent ·
/// KHÔNG tự lưu scene (Sếp tự Ctrl+S sau khi thấy ổn).
/// </summary>
public static class TilemapRenderModeTool
{
    private const string MenuRoot  = "Tools/Farm Game/Hiệu năng/";
    private const string MenuDry   = MenuRoot + "★ Tilemap: đổi Individual → Chunk (DRY-RUN)";
    private const string MenuApply = MenuRoot + "★ Tilemap: đổi Individual → Chunk (APPLY)";
    private const string MenuBack  = MenuRoot + "Hoàn tác: đưa TẤT CẢ về Individual";

    /// <summary>Lớp cần GIỮ Individual vì tile phải xen kẽ Y-sort với nhân vật.</summary>
    private static readonly HashSet<string> GiuIndividual = new HashSet<string>
    {
        "Tilemap_IsoFence",
        "Tilemap_IsoRock",
    };

    [MenuItem(MenuDry, false, 10)]
    private static void DryRun() { Chay(false); }

    [MenuItem(MenuApply, false, 11)]
    private static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Đổi Tilemap sang Chunk",
                "Sẽ đổi Mode của các tilemap NỀN từ Individual sang Chunk để tăng FPS.\n\n" +
                "• Có Undo (Ctrl+Z)\n" +
                "• KHÔNG tự lưu scene — Sếp xem ổn rồi mới Ctrl+S\n" +
                "• Hàng rào / đá giữ nguyên Individual\n\n" +
                "Tiếp tục?", "Tiếp tục", "Huỷ")) return;
        Chay(true);
    }

    [MenuItem(MenuBack, false, 12)]
    private static void TraVeIndividual()
    {
        if (!EditorUtility.DisplayDialog("Hoàn tác",
                "Đưa TẤT CẢ TilemapRenderer về Individual (trạng thái trước khi tối ưu). Tiếp tục?",
                "Đưa về Individual", "Huỷ")) return;

        TilemapRenderer[] all = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        int n = 0;
        foreach (TilemapRenderer r in all)
        {
            if (r == null || r.mode == TilemapRenderer.Mode.Individual) continue;
            Undo.RecordObject(r, "Tilemap ve Individual");
            r.mode = TilemapRenderer.Mode.Individual;
            EditorUtility.SetDirty(r);
            n++;
        }
        Debug.Log($"[TilemapPerf] Đã đưa {n} TilemapRenderer về Individual. Nhớ Ctrl+S nếu muốn giữ.");
    }

    private static void Chay(bool apply)
    {
        TilemapRenderer[] all = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("[TilemapPerf] Không tìm thấy TilemapRenderer nào trong scene đang mở.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(apply ? "═══ TILEMAP PERF — APPLY ═══" : "═══ TILEMAP PERF — DRY-RUN (chưa đổi gì) ═══");
        sb.AppendLine();
        sb.AppendLine("TILEMAP                     SỐ Ô  MODE CŨ     MODE MỚI    GHI CHÚ");

        int doi = 0, tongO = 0, oDuocToiUu = 0;
        foreach (TilemapRenderer r in all)
        {
            if (r == null) continue;
            var tm = r.GetComponent<Tilemap>();
            int soO = 0;
            if (tm != null)
            {
                BoundsInt bi = tm.cellBounds;
                TileBase[] arr = tm.GetTilesBlock(bi);
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null) soO++;
            }
            tongO += soO;

            string ten = r.gameObject.name;
            bool giu  = GiuIndividual.Contains(ten);
            var cu    = r.mode;
            var moi   = giu ? cu : TilemapRenderer.Mode.Chunk;
            string note = giu ? "GIỮ Individual (tile xen kẽ nhân vật)"
                       : cu == TilemapRenderer.Mode.Chunk ? "đã là Chunk rồi"
                       : "→ Chunk (nhanh hơn)";

            if (cu != moi)
            {
                oDuocToiUu += soO;
                if (apply)
                {
                    Undo.RecordObject(r, "Tilemap sang Chunk");
                    r.mode = moi;
                    EditorUtility.SetDirty(r);
                }
                doi++;
            }
            sb.AppendLine($"{ten,-26}{soO,7} {cu,-12}{moi,-12}{note}");
        }

        sb.AppendLine();
        sb.AppendLine($"Tổng ô tile trong scene : {tongO}");
        sb.AppendLine($"Số tilemap sẽ đổi       : {doi}");
        sb.AppendLine($"Số ô thoát khỏi Individual: {oDuocToiUu}  ← đây là phần CPU tiết kiệm được mỗi frame");
        sb.AppendLine();
        sb.AppendLine(apply
            ? "ĐÃ ĐỔI. Bấm Play thử — nếu FPS lên là trúng. Ưng thì Ctrl+S lưu scene; không ưng thì Ctrl+Z."
            : "Chưa đổi gì. Chạy bản (APPLY) để áp dụng.");
        sb.AppendLine("Nếu sau khi đổi thấy nhân vật bị tile che sai chỗ ⇒ thêm tên tilemap đó vào");
        sb.AppendLine("danh sách GiuIndividual trong TilemapRenderModeTool.cs rồi chạy lại.");

        Debug.Log(sb.ToString());
    }
}
#endif
