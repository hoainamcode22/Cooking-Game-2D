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

// ============================================================================
//  Edric Tools > Toi uu > Tilemap nen sang SRP Batch   (2026-09-26)
//  (dat chung file nay de Unity bien dich ngay, khong phu thuoc viec quet file moi)
//  11 tilemap nen dang Individual (Unity sort + ve TUNG O). Chunk tung bi lo duong luoi (VONG 13) nen lan nay dung
//  SRP Batch (giong Water_Tilemap, khong lo luoi). 1 = doi, 2 = tra lai. Co Undo, khong tu luu scene.
// ============================================================================
#if UNITY_EDITOR
public static class TilemapSrpBatchTool
{
    private const string GOC = "Edric Tools/Toi uu/";
    private const string KHOA = "EDRIC_TM_SRPBATCH_DA_DOI";
    private static readonly string[] LOP =
    {
        "Tilemap_IsoGrass", "Co_Grass", "Tilemap_IsoSand", "Tilemap_IsoDirt", "Tilemap_IsoDock",
        "Tilemap_IsoStone", "Tilemap_IsoDirtPatch", "Tilemap_LockedOverlay",
    };

    private static List<TilemapRenderer> Tim(HashSet<string> ten, TilemapRenderer.Mode dangLa)
    {
        var kq = new List<TilemapRenderer>();
        foreach (var r in Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (r != null && !EditorUtility.IsPersistent(r) && ten.Contains(r.gameObject.name) && r.mode == dangLa) kq.Add(r);
        return kq;
    }

    [MenuItem(GOC + "1. Tilemap nen sang SRP Batch (nhe CPU)", false, 1)]
    public static void Doi()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = Tim(new HashSet<string>(LOP), TilemapRenderer.Mode.Individual);
        if (ds.Count == 0) { Bao("Khong co tilemap nen nao dang Individual (da doi roi?)."); return; }
        var sb = new StringBuilder();
        foreach (var r in ds) sb.Append("- ").Append(r.gameObject.name).Append('\n');
        if (!EditorUtility.DisplayDialog("Tilemap sang SRP Batch",
                "Doi " + ds.Count + " lop nen tu Individual sang SRP Batch:\n" + sb +
                "\nHang rao / da cao / decor GIU Individual.\nCo Undo. Xem map khong loi roi Ctrl+S.", "Doi", "Huy")) return;
        Undo.SetCurrentGroupName("Tilemap sang SRP Batch");
        int nhom = Undo.GetCurrentGroup();
        var daDoi = new List<string>();
        foreach (var r in ds)
        {
            Undo.RecordObject(r, "Tilemap sang SRP Batch");
            r.mode = TilemapRenderer.Mode.SRPBatch;
            EditorUtility.SetDirty(r);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
            daDoi.Add(r.gameObject.name);
        }
        Undo.CollapseUndoOperations(nhom);
        EditorPrefs.SetString(KHOA, string.Join("|", daDoi.ToArray()));
        Bao("Da doi " + daDoi.Count + " lop sang SRP Batch.\n\nBam Play xem ky (mep co, duong dat, ben tau).\nOn thi Ctrl+S. Loi (vien luoi, sai lop) -> Ctrl+Z hoac muc 2.");
    }

    [MenuItem(GOC + "2. Tra lai Individual (cac lop muc 1 da doi)", false, 2)]
    public static void TraLai()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ten = new HashSet<string>(EditorPrefs.GetString(KHOA, string.Join("|", LOP)).Split('|'));
        var ds = Tim(ten, TilemapRenderer.Mode.SRPBatch);
        if (ds.Count == 0) { Bao("Khong co lop nao can tra lai."); return; }
        Undo.SetCurrentGroupName("Tilemap tra lai Individual");
        int nhom = Undo.GetCurrentGroup();
        foreach (var r in ds)
        {
            Undo.RecordObject(r, "Tilemap tra lai Individual");
            r.mode = TilemapRenderer.Mode.Individual;
            EditorUtility.SetDirty(r);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(r.gameObject.scene);
        }
        Undo.CollapseUndoOperations(nhom);
        Bao("Da tra " + ds.Count + " lop ve Individual. Bam Ctrl+S.");
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Toi uu Tilemap", s, "OK");
}
#endif
