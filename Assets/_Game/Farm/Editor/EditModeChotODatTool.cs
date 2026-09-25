// ============================================================================
//  Tools > Farm Game > Edit Mode > 6 / 7   (2026-09-25)
//  6. CHOT vi tri o dat BAN DAU: lay dung cho Sep da keo KHIT trong game (luc Play, Edit mode cua game)
//     roi GHI VAO SCENE -> choi lai tu dau (xoa save) van khit y nhu vay.
//     Nguon: save "FARM_SCENE_MOVES_V1" (moi lan keo o dat CO SAN trong scene, game ghi "plot:<plotId>").
//     O Sep MUA them (luu rieng trong danh sach cong trinh) KHONG bi dong toi -> dau game van dung so o ban dau.
//     Truoc khi ghi: backup file scene + luu vi tri cu (muc 7 tra lai duoc). Co Undo. Xong bam Ctrl+S.
//  7. Hoan tac: tra cac o ve vi tri truoc lan chot gan nhat.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditModeChotODatTool
{
    private const string Goc = "Tools/Farm Game/Edit Mode/";
    private const string TienTo = "_Backup_ChotODat_";

    [System.Serializable] private class DiChuyen { public string id; public float x, y; public int rot; }
    [System.Serializable] private class Goi { public List<DiChuyen> ds = new List<DiChuyen>(); }
    [System.Serializable] private class ViTriCu { public string duong; public int plotId; public float x, y, z; public Quaternion rot; }
    [System.Serializable] private class GoiCu { public List<ViTriCu> ds = new List<ViTriCu>(); }

    // =====================================================================
    [MenuItem(Goc + "6. Chot vi tri o dat ban dau (lay tu luc Play)", false, 60)]
    private static void Chot()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Chot o dat", "Thoat Play mode truoc roi bam lai.", "OK"); return; }

        string json = PlayerPrefs.GetString(PlacementManager.SceneMovesKey, "");
        Goi goi = null;
        try { goi = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Goi>(json); } catch { }
        var map = new Dictionary<int, DiChuyen>();
        if (goi != null)
            foreach (var d in goi.ds)
                if (d != null && d.id != null && d.id.StartsWith("plot:") && int.TryParse(d.id.Substring(5), out int id)) map[id] = d;
        if (map.Count == 0)
        {
            EditorUtility.DisplayDialog("Chot o dat",
                "Save chua co vi tri o dat nao Sep keo trong game.\n\nBam Play, vao Edit mode cua game, keo nhe 1 o dat roi tha (de game ghi lai), thoat Play roi chay lai muc nay.", "OK");
            return;
        }

        // O dat CO SAN trong scene (khong tinh o mua: o mua sinh luc chay, khong nam trong scene luc Edit)
        var tatCa = Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p != null && !EditorUtility.IsPersistent(p) && p.gameObject.scene.IsValid()).ToList();
        int dangBat = tatCa.Count(p => p.gameObject.activeInHierarchy && p.Category == PlotCategory.Normal);
        int chauBat = tatCa.Count(p => p.gameObject.activeInHierarchy && p.Category == PlotCategory.Flower);

        var viec = new List<(Transform t, DiChuyen d, PlotController p)>();
        foreach (var p in tatCa)
        {
            if (!p.gameObject.activeInHierarchy) continue;                   // o cu dang tat trong scene: khong dong vao
            if (!map.TryGetValue(p.PlotId, out var d)) continue;
            var eb = p.GetComponentInParent<EditableBuilding>(true);
            Transform t = eb != null ? eb.transform : p.transform;
            if (Mathf.Abs(t.position.x - d.x) < 0.01f && Mathf.Abs(t.position.y - d.y) < 0.01f) continue;   // da dung cho
            viec.Add((t, d, p));
        }
        int oDat = viec.Count(v => v.p.Category == PlotCategory.Normal), chau = viec.Count - oDat;

        var sb = new StringBuilder();
        sb.AppendLine($"Scene dang co {dangBat} o dat + {chauBat} chau hoa ban dau (dang bat).");
        sb.AppendLine($"Save co vi tri cua {map.Count} o/chau Sep da keo trong game.");
        sb.AppendLine($"Se doi cho: {oDat} o dat, {chau} chau hoa.");
        sb.AppendLine();
        foreach (var v in viec.Take(14))
            sb.AppendLine($"  {v.t.name} (id {v.p.PlotId}): ({v.t.position.x:0},{v.t.position.y:0}) -> ({v.d.x:0},{v.d.y:0})");
        if (viec.Count > 14) sb.AppendLine($"  ... va {viec.Count - 14} o nua");
        if (viec.Count == 0) { EditorUtility.DisplayDialog("Chot o dat", sb + "\nScene da dung y nhu trong game, khong can doi.", "OK"); return; }

        int chon = EditorUtility.DisplayDialogComplex("Chot o dat ban dau", sb.ToString(), "O dat + chau hoa", "Huy", "Chi o dat");
        if (chon == 1) return;
        if (chon == 2) viec = viec.Where(v => v.p.Category == PlotCategory.Normal).ToList();

        // Backup: file scene tren dia + vi tri cu (cho muc 7)
        string gocDuAn = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(gocDuAn, TienTo + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var scene = viec[0].t.gameObject.scene;
        if (!string.IsNullOrEmpty(scene.path) && File.Exists(Path.Combine(gocDuAn, scene.path)))
            File.Copy(Path.Combine(gocDuAn, scene.path), Path.Combine(bk, Path.GetFileName(scene.path)), true);
        var cu = new GoiCu();
        foreach (var v in viec)
            cu.ds.Add(new ViTriCu { duong = DuongDan(v.t), plotId = v.p.PlotId, x = v.t.position.x, y = v.t.position.y, z = v.t.position.z, rot = v.t.rotation });
        File.WriteAllText(Path.Combine(bk, "vi_tri_cu.json"), JsonUtility.ToJson(cu, true));

        foreach (var v in viec)
        {
            Undo.RecordObject(v.t, "Chot o dat ban dau");
            v.t.position = new Vector3(v.d.x, v.d.y, v.t.position.z);
            v.t.rotation = PlacementManager.RotationOf(v.d.rot);
            EditorSceneManager.MarkSceneDirty(v.t.gameObject.scene);
        }
        Debug.Log($"[ChotODat] Da ghi vi tri trong game vao scene cho {viec.Count} o. Backup: {Path.GetFileName(bk)}. Bam Ctrl+S de luu. Choi lai tu dau se khit y nhu vay.");
        EditorUtility.DisplayDialog("Chot o dat", $"Xong {viec.Count} o.\nBam Ctrl+S de luu scene.\n\nBackup: {Path.GetFileName(bk)} (muc 7 de tra lai).", "OK");
    }

    // =====================================================================
    [MenuItem(Goc + "7. Hoan tac chot o dat (lan gan nhat)", false, 61)]
    private static void HoanTac()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Chot o dat", "Thoat Play mode truoc.", "OK"); return; }
        string gocDuAn = Path.GetDirectoryName(Application.dataPath);
        var bks = Directory.GetDirectories(gocDuAn, TienTo + "*").OrderBy(x => x).ToArray();
        string file = bks.Length > 0 ? Path.Combine(bks[bks.Length - 1], "vi_tri_cu.json") : null;
        if (file == null || !File.Exists(file)) { EditorUtility.DisplayDialog("Chot o dat", "Chua co lan chot nao de hoan tac.", "OK"); return; }
        var cu = JsonUtility.FromJson<GoiCu>(File.ReadAllText(file));
        if (!EditorUtility.DisplayDialog("Chot o dat", $"Tra {cu.ds.Count} o ve vi tri truoc lan chot {Path.GetFileName(bks[bks.Length - 1])}?", "Hoan tac", "Huy")) return;
        var theoId = Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p != null && !EditorUtility.IsPersistent(p)).GroupBy(p => p.PlotId).ToDictionary(g => g.Key, g => g.First());
        int n = 0;
        foreach (var v in cu.ds)
        {
            if (!theoId.TryGetValue(v.plotId, out var p)) continue;
            var eb = p.GetComponentInParent<EditableBuilding>(true);
            Transform t = eb != null ? eb.transform : p.transform;
            Undo.RecordObject(t, "Hoan tac chot o dat");
            t.position = new Vector3(v.x, v.y, v.z);
            t.rotation = v.rot;
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            n++;
        }
        Debug.Log($"[ChotODat] Da tra {n} o ve vi tri cu. Ctrl+S de luu.");
    }

    private static string DuongDan(Transform t)
    {
        var ds = new List<string>();
        for (; t != null; t = t.parent) ds.Add(t.name);
        ds.Reverse();
        return string.Join("/", ds);
    }
}
