// ============================================================================
//  Tools > Farm Game > Edit Mode > 10. Can deu o dat (khit tung mep)   (2026-09-25)
//  Vi sao co o "loi ra": vai o ruong lech khoi luoi vai don vi (keo tay, khong trung tam o),
//  hoac hinh dat (GroundSprite) cua o do bi to / dich khac cac o con lai (vd 1 o scale 43.14 thay vi 42.98).
//  Muc nay:
//    - LUC PLAY: dua MOI o ruong (ke ca o mua) ve DUNG diem neo luoi (luu ngay nhu keo trong game),
//      va dong bo hinh dat cua moi o theo gia tri DA SO cac o.
//    - LUC EDIT (khong Play): dong bo hinh dat cua cac o CO SAN trong scene theo da so (Undo, Ctrl+S).
//      Lech VI TRI cua o thi phai sua luc Play (vi tri that nam trong save), xong thoat Play -> muc 6.
//  Chi dung o ruong thuong (khong dung chau hoa). Console ghi ro o nao lech bao nhieu.
// ============================================================================
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditModeCanDeuODatTool
{
    private const float LECH_VI_TRI = 0.5f;   // world: lech hon muc nay moi tinh la lech

    private struct O { public PlotController p; public Transform goc; public Transform dat; }

    [MenuItem("Tools/Farm Game/Edit Mode/10. Can deu o dat (khit tung mep)", false, 64)]
    private static void CanDeu()
    {
        bool play = Application.isPlaying;
        if (play && EditModeManager.IsEditMode) { EditorUtility.DisplayDialog("Can deu o dat", "Thoat Edit mode cua game truoc.", "OK"); return; }

        var ds = new List<O>();
        foreach (var p in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p == null || p.Category != PlotCategory.Normal || EditorUtility.IsPersistent(p)) continue;
            var eb = p.GetComponentInParent<EditableBuilding>(true);
            ds.Add(new O { p = p, goc = eb != null ? eb.transform : p.transform, dat = p.transform.Find("GroundSprite") });
        }
        if (ds.Count == 0) { EditorUtility.DisplayDialog("Can deu o dat", "Khong thay o ruong nao (mo SCN_Farm).", "OK"); return; }

        // Gia tri DA SO cua hinh dat (lam tron de gom nhom)
        var coDat = ds.Where(o => o.dat != null).ToList();
        Vector3 lpChuan = Vector3.zero, lsChuan = Vector3.one; Vector3 psChuan = Vector3.one;
        if (coDat.Count > 0)
        {
            lpChuan = DaSo(coDat.Select(o => o.dat.localPosition));
            lsChuan = DaSo(coDat.Select(o => o.dat.localScale));
        }
        psChuan = DaSo(ds.Select(o => o.p.transform.localScale));

        var sb = new StringBuilder();
        int lechViTri = 0, lechHinh = 0;
        var pm = play ? PlacementManager.Instance : null;
        if (!play) Undo.SetCurrentGroupName("Can deu o dat");

        foreach (var o in ds)
        {
            string ten = $"{o.goc.name} (id {o.p.PlotId})";

            // 1. Vi tri (chi luc Play, co luu)
            Vector3 neo = IsoGrid.RectAnchorWorld(IsoGrid.RectFromAnchor(o.goc.position, Vector2Int.one));
            float d = Vector2.Distance(o.goc.position, neo);
            if (d > LECH_VI_TRI)
            {
                if (play && pm != null) { pm.DoiChoVaLuu(o.goc.gameObject, neo); lechViTri++; sb.AppendLine($"  {ten}: lech luoi {d:0.0} -> da dua ve dung o"); }
                else sb.AppendLine($"  {ten}: lech luoi {d:0.0} (sua luc Play)");
            }

            // 2. Hinh dat + scale o
            bool sai = false;
            if (o.dat != null && (Khac(o.dat.localPosition, lpChuan) || Khac(o.dat.localScale, lsChuan)))
            {
                if (!play) Undo.RecordObject(o.dat, "Can deu o dat");
                sb.AppendLine($"  {ten}: hinh dat pos {o.dat.localPosition} scale {o.dat.localScale} -> {lpChuan} / {lsChuan}");
                o.dat.localPosition = lpChuan; o.dat.localScale = lsChuan; sai = true;
            }
            if (Khac(o.p.transform.localScale, psChuan))
            {
                if (!play) Undo.RecordObject(o.p.transform, "Can deu o dat");
                sb.AppendLine($"  {ten}: scale o {o.p.transform.localScale} -> {psChuan}");
                o.p.transform.localScale = psChuan; sai = true;
            }
            if (sai)
            {
                lechHinh++;
                if (!play) EditorSceneManager.MarkSceneDirty(o.p.gameObject.scene);
            }
        }

        if (play && pm != null && lechViTri > 0) { pm.RefreshOccupancy(); PlayerPrefs.Save(); }

        string tom = $"{ds.Count} o ruong. Lech luoi: {lechViTri}{(play ? " (da sua + luu)" : "")}. Hinh dat khac da so: {lechHinh} (da dong bo).";
        Debug.Log($"[CanDeuODat] {(play ? "PLAY" : "EDIT")} - {tom}\n{sb}");
        string them = play
            ? (lechHinh > 0 ? "\n\nHinh dat chi sua tam luc Play. Thoat Play, bam lai muc 10 (luc Edit) roi Ctrl+S de giu luon." : "")
              + (lechViTri > 0 ? "\nMuon dau game cung khit: thoat Play -> muc 6." : "")
            : (lechHinh > 0 ? "\n\nBam Ctrl+S de luu scene." : "") + "\nLech vi tri (neu co) sua bang cach bam muc 10 luc Play.";
        EditorUtility.DisplayDialog("Can deu o dat", tom + them + "\n\nChi tiet: Console [CanDeuODat].", "OK");
    }

    private static bool Khac(Vector3 a, Vector3 b)
    {
        for (int i = 0; i < 3; i++)
            if (Mathf.Abs(a[i] - b[i]) > 0.001f * Mathf.Max(1f, Mathf.Abs(b[i]))) return true;
        return false;
    }

    private static Vector3 DaSo(IEnumerable<Vector3> vs)
    {
        var nhom = vs.GroupBy(v => new Vector3(Mathf.Round(v.x * 100f), Mathf.Round(v.y * 100f), Mathf.Round(v.z * 100f)))
                     .OrderByDescending(g => g.Count()).First();
        return nhom.First();
    }
}
