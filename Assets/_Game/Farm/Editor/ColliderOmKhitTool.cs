// ============================================================================
//  Tools > Farm Game > Collider om khit cong trinh   (2026-09-25)
//  VI SAO: cong trinh dang bam bang 1-5 BoxCollider2D hinh chu nhat -> bam vao khoang trong canh mai / goc anh
//  van mo cong trinh, con bam dung mep hinh lai truot. Tool nay bo collider OM SAT duong vien hinh.
//
//  1. Om khit TAT CA cong trinh trong scene (tu tim, hien danh sach cho Sep duyet truoc khi lam).
//  2. Om khit cac object DANG CHON.
//  3. Tra lai box cu (tat ca).
//
//  Cach lam (co Undo, backup scene truoc):
//    - Them PolygonCollider2D lay tu Physics Shape cua sprite (duong vien alpha, sat hinh; sprite khong co
//      physics shape thi dung bao loi luoi sprite). Dat LEN TRUOC cac box de GetComponent<Collider2D>() tra polygon.
//    - TAT (khong xoa) BoxCollider2D cu. Moi tham chieu trong scene dang tro vao box cu -> tro sang polygon.
//    - Ghi het vao ColliderOmKhitMarker de muc 3 tra lai y nguyen.
//  BO QUA (vi code dang doc rieng BoxCollider2D cua chung): chuong nuoi (pham vi di cua con vat), decor / nha
//  dang lon (DecorGrowthController, HouseGrowthController), toa tau, bang lo dat, ben tau, UI.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ColliderOmKhitTool
{
    private const string GOC = "Tools/Farm Game/Collider om khit cong trinh/";

    private static readonly string[] BoQuaNeuCo =
    {
        "DecorGrowthController", "HouseGrowthController", "TrainWagonSlot", "LandRegionSignBoard", "BoatDockSlot",
        "PenClickDetector", "LivestockAI", "PenController", "PlotController", "WorldClearable",
    };

    [MenuItem(GOC + "1. Om khit TAT CA cong trinh", false, 1)]
    private static void TatCa()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = new List<GameObject>();
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sr == null || EditorUtility.IsPersistent(sr) || !sr.gameObject.scene.IsValid()) continue;
            if (HopLe(sr.gameObject, out _)) ds.Add(sr.gameObject);
        }
        ds = ds.Distinct().OrderBy(g => g.name).ToList();
        if (ds.Count == 0) { Bao("Khong co cong trinh nao can om khit (hoac da om het)."); return; }
        string ten = string.Join("\n", ds.Select(g => "- " + g.name + "  (" + g.GetComponents<BoxCollider2D>().Count(b => b.enabled) + " box)"));
        if (!EditorUtility.DisplayDialog("Collider om khit", $"Se om khit {ds.Count} cong trinh:\n\n{ten}\n\nBackup scene truoc. Tra lai: muc 3 hoac Ctrl+Z.", "Lam", "Huy")) return;
        Lam(ds);
    }

    [MenuItem(GOC + "2. Om khit cac object DANG CHON", false, 2)]
    private static void DangChon()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = new List<GameObject>();
        var loi = new List<string>();
        foreach (var go in Selection.gameObjects)
        {
            if (HopLe(go, out string lyDo)) ds.Add(go);
            else loi.Add(go.name + ": " + lyDo);
        }
        if (ds.Count == 0) { Bao("Khong object nao lam duoc.\n\n" + string.Join("\n", loi)); return; }
        if (loi.Count > 0 && !EditorUtility.DisplayDialog("Collider om khit", "Bo qua:\n" + string.Join("\n", loi) + $"\n\nVan lam {ds.Count} object con lai?", "Lam", "Huy")) return;
        Lam(ds);
    }

    [MenuItem(GOC + "3. Tra lai box cu (tat ca)", false, 3)]
    private static void TraLai()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = Object.FindObjectsByType<ColliderOmKhitMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                       .Where(m => m != null && !EditorUtility.IsPersistent(m)).ToList();
        if (ds.Count == 0) { Bao("Scene khong co cong trinh nao da om khit."); return; }
        Undo.SetCurrentGroupName("Tra lai box cu");
        int nhom = Undo.GetCurrentGroup();
        foreach (var m in ds)
        {
            foreach (var t in m.thamChieu)
            {
                if (t == null || t.chu == null) continue;
                var so = new SerializedObject(t.chu);
                var p = so.FindProperty(t.duong);
                if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) { p.objectReferenceValue = t.cu; so.ApplyModifiedProperties(); }
            }
            foreach (var b in m.boxCu) if (b != null) { Undo.RecordObject(b, "Tra lai box"); b.enabled = true; }
            if (m.poly != null) Undo.DestroyObjectImmediate(m.poly);
            var sc = m.gameObject.scene;
            Undo.DestroyObjectImmediate(m);
            EditorSceneManager.MarkSceneDirty(sc);
        }
        Undo.CollapseUndoOperations(nhom);
        Bao($"Da tra lai box cu cho {ds.Count} cong trinh. Bam Ctrl+S.");
    }

    // =====================================================================
    //  4. TOI UU (2026-09-26): lan om khit truoc bo ca 93 CAY THONG (moi cay 422 diem / 8 duong vien) -> ~40.000 diem
    //  collider, lam Physics2D + EventSystem raycast nang (lag). Muc nay:
    //    - Cay / go / bui / da tu nhien: TRA LAI box cu (cham cay khong can om khit).
    //    - Cong trinh con lai: RUT GON duong vien (bo diem thua, sai lech <= ~1% kich thuoc hinh).
    //  Co Undo (Ctrl+Z). Xong bam Ctrl+S.
    // =====================================================================
    private static readonly string[] TuNhien =
    {
        "prefab_pinetree", "log_horizontal", "caythong", "cayrung", "caydaithu", "caychetkho", "cayganduognra",
        "prefab_bush", "prefab_grass", "prefab_rock", "prefab_tree", "wildrock", "wildbush",
    };

    [MenuItem(GOC + "4. Toi uu (bo om khit cay + rut gon diem)", false, 4)]
    public static void ToiUu()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = Object.FindObjectsByType<ColliderOmKhitMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                       .Where(m => m != null && !EditorUtility.IsPersistent(m)).ToList();
        if (ds.Count == 0) { Bao("Scene khong co collider om khit nao."); return; }
        Undo.SetCurrentGroupName("Toi uu collider om khit");
        int nhom = Undo.GetCurrentGroup();
        int traCay = 0, rutGon = 0, truoc = 0, sau = 0;
        foreach (var m in ds)
        {
            string ten = m.gameObject.name.ToLowerInvariant();
            if (TuNhien.Any(ten.StartsWith))
            {
                if (m.poly != null) truoc += m.poly.GetTotalPointCount();
                TraLaiMot(m);
                traCay++;
                continue;
            }
            if (m.poly == null) continue;
            var sr = m.GetComponent<SpriteRenderer>();
            float kt = sr != null && sr.sprite != null ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) : 1f;
            int n0 = m.poly.GetTotalPointCount();
            truoc += n0;
            Undo.RecordObject(m.poly, "Rut gon collider");
            for (int i = 0; i < m.poly.pathCount; i++) m.poly.SetPath(i, RutGon(m.poly.GetPath(i), kt));
            sau += m.poly.GetTotalPointCount();
            if (m.poly.GetTotalPointCount() < n0) rutGon++;
            EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
        }
        Undo.CollapseUndoOperations(nhom);
        Bao($"Tra lai box cho {traCay} cay/go/da tu nhien.\nRut gon {rutGon} cong trinh.\nTong diem collider: {truoc} -> {sau}.\n\nBam Ctrl+S. Khong ung: Ctrl+Z.");
    }

    /// <summary>Bo diem thua: sai lech toi da ~1% kich thuoc hinh, it nhat 3 diem.</summary>
    private static Vector2[] RutGon(Vector2[] duong, float kichThuoc)
    {
        if (duong == null || duong.Length <= 8) return duong;
        var vao = new List<Vector2>(duong);
        vao.Add(duong[0]);                                    // khep kin de Simplify giu diem dau/cuoi dung
        var ra = new List<Vector2>();
        LineUtility.Simplify(vao, Mathf.Max(0.0001f, kichThuoc * 0.01f), ra);
        if (ra.Count > 1 && (ra[ra.Count - 1] - ra[0]).sqrMagnitude < 1e-8f) ra.RemoveAt(ra.Count - 1);
        return ra.Count >= 3 ? ra.ToArray() : duong;
    }

    private static void TraLaiMot(ColliderOmKhitMarker m)
    {
        foreach (var t in m.thamChieu)
        {
            if (t == null || t.chu == null) continue;
            var so = new SerializedObject(t.chu);
            var p = so.FindProperty(t.duong);
            if (p != null && p.propertyType == SerializedPropertyType.ObjectReference) { p.objectReferenceValue = t.cu; so.ApplyModifiedProperties(); }
        }
        foreach (var b in m.boxCu) if (b != null) { Undo.RecordObject(b, "Tra lai box"); b.enabled = true; }
        if (m.poly != null) Undo.DestroyObjectImmediate(m.poly);
        var sc = m.gameObject.scene;
        Undo.DestroyObjectImmediate(m);
        EditorSceneManager.MarkSceneDirty(sc);
    }

    // =====================================================================
    private static bool HopLe(GameObject go, out string lyDo)
    {
        lyDo = "";
        if (go == null) { lyDo = "null"; return false; }
        if (go.GetComponent<RectTransform>() != null || go.GetComponentInParent<Canvas>(true) != null) { lyDo = "la UI"; return false; }
        if (go.name == "LockUI" || go.name == "SickleTool") { lyDo = "UI tren map"; return false; }
        if (go.GetComponent<ColliderOmKhitMarker>() != null) { lyDo = "da om khit roi"; return false; }
        { string tl = go.name.ToLowerInvariant(); if (TuNhien.Any(tl.StartsWith)) { lyDo = "cay / go / da tu nhien (khong can om khit)"; return false; } }
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) { lyDo = "khong co SpriteRenderer / sprite"; return false; }
        if (sr.drawMode != SpriteDrawMode.Simple) { lyDo = "sprite dang Sliced/Tiled"; return false; }
        if (!go.GetComponents<BoxCollider2D>().Any(b => b.enabled)) { lyDo = "khong co BoxCollider2D dang bat"; return false; }
        foreach (var mb in go.GetComponentsInParent<MonoBehaviour>(true))
            if (mb != null && BoQuaNeuCo.Contains(mb.GetType().Name)) { lyDo = "co " + mb.GetType().Name + " (code doc rieng box)"; return false; }
        return true;
    }

    private static void Lam(List<GameObject> ds)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string bk = Backup(ds[0].scene);

        // Gom moi MonoBehaviour trong scene 1 lan de noi lai tham chieu
        var tatCaMb = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                            .Where(m => m != null && !EditorUtility.IsPersistent(m)).ToList();

        Undo.SetCurrentGroupName("Collider om khit");
        int nhom = Undo.GetCurrentGroup();
        int xong = 0, noiLai = 0;
        var doiBox = new Dictionary<BoxCollider2D, ColliderOmKhitMarker>();
        var baoCao = new List<string>();
        foreach (var go in ds)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var boxes = go.GetComponents<BoxCollider2D>().Where(b => b.enabled).ToList();
            List<Vector2[]> duong = DuongVien(sr);
            if (duong.Count == 0) { baoCao.Add(go.name + ": khong lay duoc duong vien, bo qua"); continue; }

            Undo.RegisterFullObjectHierarchyUndo(go, "Collider om khit");
            var poly = Undo.AddComponent<PolygonCollider2D>(go);
            poly.pathCount = duong.Count;
            float ktHinh = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            for (int i = 0; i < duong.Count; i++) poly.SetPath(i, RutGon(duong[i], ktHinh));   // [2026-09-26] rut gon diem
            poly.isTrigger = boxes[0].isTrigger;
            poly.offset = Vector2.zero;
            // Dua polygon len TRUOC box dau tien
            var comps = go.GetComponents<Component>().ToList();
            int iBox = comps.IndexOf(boxes[0]);
            for (int n = 0; n < 40 && comps.IndexOf(poly) > iBox; n++)
            {
                if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(poly)) break;
                comps = go.GetComponents<Component>().ToList();
                iBox = comps.IndexOf(boxes[0]);
            }

            var mk = Undo.AddComponent<ColliderOmKhitMarker>(go);
            mk.poly = poly;
            mk.boxCu = boxes;
            foreach (var bx in boxes) doiBox[bx] = mk;

            foreach (var b in boxes) { Undo.RecordObject(b, "Collider om khit"); b.enabled = false; }
            EditorUtility.SetDirty(mk);
            EditorSceneManager.MarkSceneDirty(go.scene);
            xong++;
            baoCao.Add(go.name + ": " + duong.Count + " duong vien, tat " + boxes.Count + " box");
        }
        // Noi lai tham chieu box cu -> polygon (1 luot qua moi MonoBehaviour trong scene)
        foreach (var mb in tatCaMb)
        {
            if (mb == null || mb is ColliderOmKhitMarker) continue;
            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            bool doi = false;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                var bx = it.objectReferenceValue as BoxCollider2D;
                ColliderOmKhitMarker mk;
                if (bx == null || !doiBox.TryGetValue(bx, out mk)) continue;
                mk.thamChieu.Add(new ColliderOmKhitMarker.ThamChieu { chu = mb, duong = it.propertyPath, cu = bx });
                it.objectReferenceValue = mk.poly;
                doi = true; noiLai++;
                EditorUtility.SetDirty(mk);
            }
            if (doi) so.ApplyModifiedProperties();
        }

        Undo.CollapseUndoOperations(nhom);
        Debug.Log("[OmKhit] " + string.Join(" | ", baoCao) + $" | noi lai {noiLai} tham chieu. Backup: {Path.GetFileName(bk)}");
        Bao($"Xong {xong}/{ds.Count} cong trinh, noi lai {noiLai} tham chieu.\n\n{string.Join("\n", baoCao)}\n\nChon cong trinh de xem khung xanh collider. Bam Ctrl+S roi Play thu bam.\nHong: muc 3 hoac Ctrl+Z. Backup: {Path.GetFileName(bk)}");
    }

    /// <summary>Duong vien sat hinh (local cua transform). Physics Shape cua sprite; khong co thi bao loi luoi sprite.</summary>
    private static List<Vector2[]> DuongVien(SpriteRenderer sr)
    {
        var kq = new List<Vector2[]>();
        var sp = sr.sprite;
        float fx = sr.flipX ? -1f : 1f, fy = sr.flipY ? -1f : 1f;
        int n = sp.GetPhysicsShapeCount();
        var tam = new List<Vector2>();
        for (int i = 0; i < n; i++)
        {
            tam.Clear();
            sp.GetPhysicsShape(i, tam);
            if (tam.Count < 3) continue;
            kq.Add(tam.Select(v => new Vector2(v.x * fx, v.y * fy)).ToArray());
        }
        if (kq.Count > 0) return kq;
        var bao = BaoLoi(sp.vertices.Select(v => new Vector2(v.x * fx, v.y * fy)).ToList());
        if (bao.Count >= 3) kq.Add(bao.ToArray());
        return kq;
    }

    private static List<Vector2> BaoLoi(List<Vector2> p)
    {
        p = p.Distinct().OrderBy(v => v.x).ThenBy(v => v.y).ToList();
        if (p.Count < 3) return p;
        var h = new List<Vector2>();
        for (int pass = 0; pass < 2; pass++)
        {
            int start = h.Count;
            foreach (var v in pass == 0 ? p : Enumerable.Reverse(p))
            {
                while (h.Count >= start + 2 && Cheo(h[h.Count - 2], h[h.Count - 1], v) <= 0) h.RemoveAt(h.Count - 1);
                h.Add(v);
            }
            h.RemoveAt(h.Count - 1);
        }
        return h;
    }

    private static float Cheo(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    private static void Bao(string s) => EditorUtility.DisplayDialog("Collider om khit", s, "OK");

    private static string Backup(UnityEngine.SceneManagement.Scene scene)
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_OmKhit_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        if (!string.IsNullOrEmpty(scene.path)) File.Copy(Path.Combine(goc, scene.path), Path.Combine(bk, Path.GetFileName(scene.path)), true);
        return bk;
    }
}
