// ============================================================================
//  Tools > Farm Game > Edit Mode > 8 / 9   (2026-09-25)
//  8. XEP GON O DAT thanh khoi DEU (bam trong luc PLAY): moi o ruong (ke ca o mua them) duoc dua ve
//     dung luoi iso, xep thanh hang - cot thang tap, KHONG con so le bac thang. Khoi moi nam gon trong
//     khung cac o dang chiem (khong lan sang nha / duong). Luu ngay nhu khi keo trong Edit mode cua game.
//     Muon DAU GAME cung gon nhu vay: thoat Play roi bam Edit Mode > 6 (chot vao scene).
//  9. Hoan tac lan xep gon gan nhat (luc Play).
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class EditModeXepGonODatTool
{
    private const string Goc = "Tools/Farm Game/Edit Mode/";
    private const string TienTo = "_Backup_XepGonODat_";

    [System.Serializable] private class Cu { public int plotId; public float x, y; }
    [System.Serializable] private class GoiCu { public List<Cu> ds = new List<Cu>(); }

    private struct O { public Transform t; public PlotController p; public Vector2Int c; }

    [MenuItem(Goc + "8. Xep gon o dat thanh khoi deu (luc Play)", false, 62)]
    private static void XepGon()
    {
        if (!Application.isPlaying) { EditorUtility.DisplayDialog("Xep gon o dat", "Bam Play truoc (tool xep ca o da mua), roi chay lai.", "OK"); return; }
        var pm = PlacementManager.Instance;
        if (pm == null) { EditorUtility.DisplayDialog("Xep gon o dat", "Khong thay PlacementManager.", "OK"); return; }
        if (EditModeManager.IsEditMode) { EditorUtility.DisplayDialog("Xep gon o dat", "Thoat Edit mode cua game truoc.", "OK"); return; }

        var ds = new List<O>();
        foreach (var p in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p == null || p.Category != PlotCategory.Normal) continue;
            var eb = p.GetComponentInParent<EditableBuilding>(true);
            Transform t = eb != null ? eb.transform : p.transform;
            ds.Add(new O { t = t, p = p, c = IsoGrid.RectFromAnchor(t.position, Vector2Int.one).position });
        }
        if (ds.Count < 2) { EditorUtility.DisplayDialog("Xep gon o dat", "Can it nhat 2 o ruong.", "OK"); return; }

        int minU = ds.Min(o => o.c.x), maxU = ds.Max(o => o.c.x), minV = ds.Min(o => o.c.y), maxV = ds.Max(o => o.c.y);
        int U = maxU - minU + 1, V = maxV - minV + 1, n = ds.Count;

        // So cot / hang gan dung hinh dang hien tai va nam GON trong khung dang chiem
        int cot = -1; float lech = float.MaxValue;
        for (int c = 1; c <= n; c++)
        {
            int h = (n + c - 1) / c;
            if (c > U || h > V) continue;
            float d = Mathf.Abs(c / (float)h - U / (float)V) + (c * h - n) * 0.05f;   // uu tien it o trong
            if (d < lech) { lech = d; cot = c; }
        }
        bool lan = false;
        if (cot < 0) { cot = Mathf.Clamp(U, 1, n); lan = true; }
        int hang = (n + cot - 1) / cot;

        // Giu thu tu: hang theo truc V, cot theo truc U (o nao dang o dau van o gan cho do)
        ds.Sort((a, b) => a.c.y != b.c.y ? a.c.y.CompareTo(b.c.y) : a.c.x.CompareTo(b.c.x));
        var dich = new Vector2Int[n];
        for (int k = 0; k < n; k++) dich[k] = new Vector2Int(minU + k % cot, minV + k / cot);

        int doi = 0;
        for (int k = 0; k < n; k++) if (CanDoi(ds[k], dich[k])) doi++;
        if (doi == 0) { EditorUtility.DisplayDialog("Xep gon o dat", "Cac o da xep deu roi.", "OK"); return; }

        var sb = new StringBuilder();
        sb.AppendLine($"{n} o ruong -> khoi {cot} x {hang} (khung hien tai {U} x {V} o).");
        sb.AppendLine($"Se doi cho {doi} o.");
        if (lan) sb.AppendLine("\nCANH BAO: khong xep vua trong khung cu, khoi se lan them ra 1 phia. Kiem tra lai sau khi xep (muc 9 de hoan tac).");
        if (!EditorUtility.DisplayDialog("Xep gon o dat", sb.ToString(), "Xep", "Huy")) return;

        // Backup vi tri cu cho muc 9
        var cu = new GoiCu();
        foreach (var o in ds) cu.ds.Add(new Cu { plotId = o.p.PlotId, x = o.t.position.x, y = o.t.position.y });
        string bk = Path.Combine(Path.GetDirectoryName(Application.dataPath), TienTo + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        File.WriteAllText(Path.Combine(bk, "vi_tri_cu.json"), JsonUtility.ToJson(cu, true));

        for (int k = 0; k < n; k++)
        {
            if (!CanDoi(ds[k], dich[k])) continue;
            Vector3 neo = IsoGrid.RectAnchorWorld(new RectInt(dich[k], Vector2Int.one));
            pm.DoiChoVaLuu(ds[k].t.gameObject, neo);
        }
        pm.RefreshOccupancy();
        PlayerPrefs.Save();
        Debug.Log($"[XepGon] Da xep {n} o thanh khoi {cot}x{hang} (doi cho {doi} o). Backup: {Path.GetFileName(bk)}. " +
                  "Muon dau game cung gon: thoat Play -> Tools > Farm Game > Edit Mode > 6.");
    }

    // Doi khi khac o luoi HOAC cung o nhung lech khoi diem neo (o "loi ra" vai don vi)
    private static bool CanDoi(O o, Vector2Int dich)
    {
        if (o.c != dich) return true;
        Vector3 neo = IsoGrid.RectAnchorWorld(new RectInt(dich, Vector2Int.one));
        return Vector2.Distance(o.t.position, neo) > 0.5f;
    }

    [MenuItem(Goc + "9. Hoan tac xep gon o dat (luc Play)", false, 63)]
    private static void HoanTac()
    {
        if (!Application.isPlaying) { EditorUtility.DisplayDialog("Xep gon o dat", "Bam Play truoc.", "OK"); return; }
        var pm = PlacementManager.Instance;
        string goc = Path.GetDirectoryName(Application.dataPath);
        var bks = Directory.GetDirectories(goc, TienTo + "*").OrderBy(x => x).ToArray();
        string f = bks.Length > 0 ? Path.Combine(bks[bks.Length - 1], "vi_tri_cu.json") : null;
        if (pm == null || f == null || !File.Exists(f)) { EditorUtility.DisplayDialog("Xep gon o dat", "Chua co lan xep nao de hoan tac.", "OK"); return; }
        var cu = JsonUtility.FromJson<GoiCu>(File.ReadAllText(f));
        var theoId = new Dictionary<int, Transform>();
        foreach (var p in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var eb = p.GetComponentInParent<EditableBuilding>(true);
            theoId[p.PlotId] = eb != null ? eb.transform : p.transform;
        }
        int n = 0;
        foreach (var c in cu.ds)
            if (theoId.TryGetValue(c.plotId, out var t)) { pm.DoiChoVaLuu(t.gameObject, new Vector3(c.x, c.y, 0f)); n++; }
        pm.RefreshOccupancy();
        PlayerPrefs.Save();
        Debug.Log($"[XepGon] Da tra {n} o ve vi tri truoc lan xep {Path.GetFileName(bks[bks.Length - 1])}.");
    }
}
