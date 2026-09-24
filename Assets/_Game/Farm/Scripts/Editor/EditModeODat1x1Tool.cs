// ============================================================================
//  Tools > Edit Mode > 3. O dat 1x1 khit luoi   (2026-09-24, buoc 1 cua phuong an Chan_De)
//
//  CHI dung vao O DAT NAU (object ten "Plot_..."). KHONG dung chau hoa, KHONG dung nha.
//    • Mat dat (GroundSprite) nho lai dung 1 o luoi = 300 x 150, dinh duoi nam DUNG goc object.
//      Cay trong / thanh tien do / icon chi DOI VI TRI theo mat dat, KHONG bi thu nho.
//    • Moi o dat giu nguyen TAM cu tren map roi hut vao o luoi gan nhat (khong chong nhau).
//    • Them con "Chan_De" (khung hinh thoi xanh, chi thay trong Scene view) de Sep canh tay.
//    • Prefab o dat mua tu shop cung duoc sua; save cu cua nguoi choi tu dich 1 lan luc load.
//    • Sao luu scene + prefab + data vao _Backup_ODat1x1_<gio> truoc khi sua.
//  Tools > Edit Mode > 4. Hoan tac o dat 1x1 : chep lai ban sao luu moi nhat.
//  Chay trong SCN_Farm, xong bam Ctrl+S.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditModeODat1x1Tool
{
    private static readonly Vector2Int MOT_O = Vector2Int.one;
    private const string TIEN_TO_BACKUP = "_Backup_ODat1x1_";

    private static bool LaODat(Transform t) => t != null && t.name.StartsWith("Plot_");

    [MenuItem("Tools/Edit Mode/3. O dat 1x1 khit luoi (chi o dat nau)", false, 20)]
    private static void Chay()
    {
        IsoGrid.ResetCache();
        if (IsoGrid.SceneGrid == null) { EditorUtility.DisplayDialog("Edit Mode", "Hay mo SCN_Farm roi chay lai.", "OK"); return; }
        Vector2 kt = IsoGrid.FootprintWorldSize(MOT_O);
        if (!EditorUtility.DisplayDialog("Edit Mode",
                $"Thu nho moi O DAT NAU ve 1 o luoi ({kt.x:0}x{kt.y:0}) va hut vao luoi?\n\n" +
                "Khong dung chau hoa, khong dung nha. Co sao luu truoc. Xong nho Ctrl+S.", "Lam", "Huy"))
            return;

        var scene = EditorSceneManager.GetActiveScene();
        var log = new System.Text.StringBuilder();

        // Prefab o dat (tu data shop co PlotController, ten prefab bat dau "Plot_")
        var prefabs = new List<string>();
        var datas = new List<PlaceableItemData>();
        foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d == null || d.prefabToBuild == null || !LaODat(d.prefabToBuild.transform)) continue;
            if (d.prefabToBuild.GetComponentInChildren<PlotController>(true) == null) continue;
            datas.Add(d);
            string p = AssetDatabase.GetAssetPath(d.prefabToBuild);
            if (!prefabs.Contains(p)) prefabs.Add(p);
        }
        string backup = SaoLuu(scene.path, prefabs, datas);
        log.AppendLine($"[O dat 1x1] O luoi {IsoGrid.CellWidth:0}x{IsoGrid.CellHeight:0}. Backup: {backup}");

        // ── A. Do TAM cu cua moi o dat trong scene (truoc khi dung vao prefab) ──
        var plots = new List<Transform>();
        var tamCu = new Dictionary<Transform, Vector3>();
        foreach (var pc in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (pc == null || EditorUtility.IsPersistent(pc) || !LaODat(pc.transform)) continue;
            if (!DoMatDat(pc.transform, out Vector3 chan, out float rong, out float cao)) { log.AppendLine($"  bo qua {pc.name}: khong do duoc mat dat"); continue; }
            plots.Add(pc.transform);
            tamCu[pc.transform] = pc.transform.TransformPoint(chan) + new Vector3(0f, cao * Mathf.Abs(pc.transform.lossyScale.y) * 0.5f, 0f);
        }

        // ── B. Prefab o dat ──────────────────────────────────────────────────
        foreach (var p in prefabs)
        {
            var goc = PrefabUtility.LoadPrefabContents(p);
            try
            {
                if (!DoMatDat(goc.transform, out Vector3 chan, out float rong, out float cao)) { log.AppendLine($"  bo qua prefab {p}"); continue; }
                Vector3 tamGoc = chan + new Vector3(0f, cao * 0.5f, 0f);           // he goc
                Vector3 tamW = goc.transform.TransformVector(tamGoc);              // lech world tu goc toi tam cu
                if (!Nan(goc.transform, log)) continue;
                var kit = goc.GetComponent<BuildingFootprintKit>();
                if (kit == null) kit = goc.AddComponent<BuildingFootprintKit>();
                kit.SoO = MOT_O;
                kit.PhienBanNeo = Mathf.Max(kit.PhienBanNeo, 1) + 1;              // > moi neoV da tung ghi (ke ca lan 2x2 hong)
                kit.LechNeoCu = new Vector2(tamW.x, tamW.y - IsoGrid.HalfDepth(MOT_O));
                DamBaoChanDe(goc.transform);
                PrefabUtility.SaveAsPrefabAsset(goc, p);
                log.AppendLine($"  prefab {p}: neo v{kit.PhienBanNeo}, lech {kit.LechNeoCu}");
            }
            finally { PrefabUtility.UnloadPrefabContents(goc); }
        }
        foreach (var d in datas)
            if (d.gridSize != MOT_O) { Undo.RecordObject(d, "O dat 1x1"); d.gridSize = MOT_O; EditorUtility.SetDirty(d); log.AppendLine($"  data {d.name}: gridSize 1x1"); }

        // ── C. O dat trong scene: nan (neu chua dung) + hut tam cu vao luoi ────
        plots.Sort((a, b) => { int c = tamCu[b].y.CompareTo(tamCu[a].y); return c != 0 ? c : tamCu[a].x.CompareTo(tamCu[b].x); });
        var chiem = new List<RectInt>();
        int n = 0;
        foreach (var t in plots)
        {
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "O dat 1x1");
            if (!Nan(t, log)) continue;
            Vector3 neoMuon = tamCu[t] - new Vector3(0f, IsoGrid.HalfDepth(MOT_O), 0f);
            RectInt r = TimOTrong(neoMuon, chiem);
            chiem.Add(r);
            Vector3 neo = IsoGrid.RectAnchorWorld(r);
            t.position = new Vector3(neo.x, neo.y, t.position.z);
            var kit = t.GetComponent<BuildingFootprintKit>();
            if (kit == null) kit = Undo.AddComponent<BuildingFootprintKit>(t.gameObject);
            kit.SoO = MOT_O;
            EditorUtility.SetDirty(kit);
            DamBaoChanDe(t);
            n++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        log.AppendLine($"[O dat 1x1] Xong {n} o dat trong scene, {prefabs.Count} prefab. Bam Ctrl+S.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Edit Mode", $"Xong: {n} o dat trong scene + {prefabs.Count} prefab o dat.\nBam Ctrl+S. Chi tiet trong Console.", "OK");
    }

    // ── Nan 1 o dat: mat dat = 1 o, dinh duoi o goc. Tra ve true neu da dung (ke ca da dung san). ──
    private static bool Nan(Transform goc, System.Text.StringBuilder log)
    {
        var g = TimDat(goc);
        if (g == null || !DoMatDat(goc, out Vector3 B, out float rong, out float cao)) return false;
        Vector2 dich = IsoGrid.FootprintWorldSize(MOT_O);
        float lsx = Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.x)), lsy = Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.y));
        float k = (dich.x / lsx) / rong;
        if (Mathf.Abs(k - 1f) < 0.01f && ((Vector2)B).magnitude * lsx < 1f) return true;   // da dung roi

        Transform gCon = g.transform;
        while (gCon.parent != null && gCon.parent != goc) gCon = gCon.parent;           // con truc tiep chua mat dat
        for (int i = 0; i < goc.childCount; i++)
        {
            var c = goc.GetChild(i);
            if (c.name == "Kit_Nen" || c.name == "Chan_De") continue;
            Vector3 lp = c.localPosition;
            c.localPosition = new Vector3((lp.x - B.x) * k, (lp.y - B.y) * k, lp.z);
            if (c == gCon) c.localScale = new Vector3(c.localScale.x * k, c.localScale.y * k, c.localScale.z);   // CHI mat dat thu nho
        }
        foreach (var col in goc.GetComponents<Collider2D>())
        {
            Undo.RecordObject(col, "O dat 1x1");
            col.offset = ((Vector2)col.offset - (Vector2)B) * k;
            if (col is BoxCollider2D bx) bx.size *= k;
            else if (col is CircleCollider2D cc) cc.radius *= k;
            else if (col is CapsuleCollider2D cp) cp.size *= k;
            else if (col is PolygonCollider2D pg)
                for (int p = 0; p < pg.pathCount; p++) { var pts = pg.GetPath(p); for (int j = 0; j < pts.Length; j++) pts[j] = (pts[j] - (Vector2)B) * k; pg.SetPath(p, pts); }
            EditorUtility.SetDirty(col);
        }
        // Chinh ti le 2:1 dung o luoi (art 474x226 ~ 2.1:1), giu dinh duoi
        if (DoMatDat(goc, out Vector3 B2, out float r2, out float c2))
        {
            float e = (dich.y / lsy) / c2;
            if (Mathf.Abs(e - 1f) > 0.005f && Mathf.Abs(e - 1f) < 0.2f)
            {
                Vector3 chanCha = gCon.localPosition + new Vector3(0f, B2.y - gCon.localPosition.y, 0f);
                gCon.localScale = new Vector3(gCon.localScale.x, gCon.localScale.y * e, gCon.localScale.z);
                gCon.localPosition = new Vector3(gCon.localPosition.x, chanCha.y - (chanCha.y - gCon.localPosition.y) * e, gCon.localPosition.z);
            }
        }
        log.AppendLine($"  {goc.name}: mat dat {rong * lsx:0} -> {dich.x:0} (x{k:0.###})");
        return true;
    }

    private static void DamBaoChanDe(Transform goc)
    {
        var t = goc.Find("Chan_De");
        if (t == null)
        {
            var go = new GameObject("Chan_De");
            go.layer = goc.gameObject.layer;
            if (!EditorUtility.IsPersistent(goc)) Undo.RegisterCreatedObjectUndo(go, "Chan_De");
            t = go.transform;
            t.SetParent(goc, false);
        }
        t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
        var cd = t.GetComponent<ChanDe>();
        if (cd == null) cd = t.gameObject.AddComponent<ChanDe>();
        cd.soO = MOT_O;
        EditorUtility.SetDirty(cd);
    }

    private static SpriteRenderer TimDat(Transform goc)
    {
        foreach (var tr in goc.GetComponentsInChildren<Transform>(true))
            if (tr.name == "GroundSprite") return tr.GetComponent<SpriteRenderer>();
        return null;
    }

    /// <summary>Hinh thoi mat dat that (bo vien trong suot), trong he toa do GOC: dinh duoi + rong + cao.</summary>
    private static bool DoMatDat(Transform goc, out Vector3 chanGoc, out float rong, out float cao)
    {
        chanGoc = Vector3.zero; rong = cao = 0f;
        var g = TimDat(goc);
        if (g == null || g.sprite == null || g.sprite.texture == null) return false;
        var sp = g.sprite;
        string path = AssetDatabase.GetAssetPath(sp.texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path))) return false;
            float ti = tex.width / (float)sp.texture.width;
            Rect r = sp.rect;
            int x0 = Mathf.FloorToInt(r.x * ti), y0 = Mathf.FloorToInt(r.y * ti);
            int w = Mathf.FloorToInt(r.width * ti), h = Mathf.FloorToInt(r.height * ti);
            var px = tex.GetPixels32();
            int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = (y0 + y) * tex.width;
                for (int x = 0; x < w; x++)
                {
                    if (px[row + x0 + x].a < 40) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return false;
            float ppu = sp.pixelsPerUnit * ti;
            Vector2 piv = sp.pivot * ti;
            Vector2 chanSp = new Vector2(((minX + maxX + 1) * 0.5f - piv.x) / ppu, (minY - piv.y) / ppu);
            if (g.flipX) chanSp.x = -chanSp.x;
            chanGoc = goc.InverseTransformPoint(g.transform.TransformPoint(chanSp));
            Vector3 dinh = goc.InverseTransformPoint(g.transform.TransformPoint(new Vector2(chanSp.x, (maxY + 1 - piv.y) / ppu)));
            Vector3 trai = goc.InverseTransformPoint(g.transform.TransformPoint(new Vector2((minX - piv.x) / ppu, 0f)));
            Vector3 phai = goc.InverseTransformPoint(g.transform.TransformPoint(new Vector2((maxX + 1 - piv.x) / ppu, 0f)));
            rong = Mathf.Abs(phai.x - trai.x);
            cao = Mathf.Abs(dinh.y - chanGoc.y);
            return rong > 1e-4f && cao > 1e-4f;
        }
        finally { Object.DestroyImmediate(tex); }
    }

    private static RectInt TimOTrong(Vector3 neo, List<RectInt> chiem)
    {
        RectInt goc = IsoGrid.RectFromAnchor(neo, MOT_O);
        if (!Chong(goc, chiem)) return goc;
        RectInt tot = goc; float totD = float.MaxValue;
        for (int ban = 1; ban <= 5 && totD == float.MaxValue; ban++)
            for (int dx = -ban; dx <= ban; dx++)
                for (int dy = -ban; dy <= ban; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ban) continue;
                    var r = new RectInt(goc.x + dx, goc.y + dy, 1, 1);
                    if (Chong(r, chiem)) continue;
                    float d = Vector2.Distance(IsoGrid.RectAnchorWorld(r), neo);
                    if (d < totD) { totD = d; tot = r; }
                }
        return tot;
    }

    private static bool Chong(RectInt r, List<RectInt> ds) { foreach (var o in ds) if (r.Overlaps(o)) return true; return false; }

    private static string SaoLuu(string scenePath, List<string> prefabs, List<PlaceableItemData> datas)
    {
        string goc = Directory.GetParent(Application.dataPath).FullName;
        string thuMuc = Path.Combine(goc, TIEN_TO_BACKUP + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(thuMuc);
        void Chep(string p)
        {
            if (string.IsNullOrEmpty(p)) return;
            string nguon = Path.Combine(goc, p);
            if (!File.Exists(nguon)) return;
            string dich = Path.Combine(thuMuc, p);
            Directory.CreateDirectory(Path.GetDirectoryName(dich));
            File.Copy(nguon, dich, true);
        }
        Chep(scenePath);
        foreach (var p in prefabs) Chep(p);
        foreach (var d in datas) Chep(AssetDatabase.GetAssetPath(d));
        return thuMuc;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  5. NHA / CONG TRINH: so o = be ngang CHAN nha (nha dat canh nhau khong de len nhau)
    // ═════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Edit Mode/5. Nha + cong trinh: so o theo be ngang anh (xem truoc roi ap dung)", false, 30)]
    private static void SoONha()
    {
        IsoGrid.ResetCache();
        float W = IsoGrid.CellWidth > 1f ? IsoGrid.CellWidth : 300f;
        var doi = new List<(PlaceableItemData d, Vector2Int cu, Vector2Int moi, float rong)>();
        var sb = new System.Text.StringBuilder();
        foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d == null || d.prefabToBuild == null) continue;
            if (d.prefabToBuild.GetComponentInChildren<PlotController>(true) != null) continue;   // o dat + chau hoa: lam rieng
            if (!DoRongAnh(d.prefabToBuild.transform, out float rong)) continue;
            // Vung N x N o co be ngang (N+N) * W/2 = N*W  ->  N = lam tron(rong / W)
            int n = Mathf.Clamp(Mathf.RoundToInt(rong / W), 1, 6);
            var moi = new Vector2Int(n, n);
            var cu = d.gridSize;
            if (cu == moi) continue;
            // Chi TANG (het de len nhau). Khong tu thu nho cong trinh dang chiem dung.
            if (moi.x * moi.y < cu.x * cu.y) continue;
            doi.Add((d, cu, moi, rong));
            sb.AppendLine($"{d.name}: {cu.x}x{cu.y} -> {n}x{n}  (anh rong {rong:0})");
        }
        if (doi.Count == 0) { EditorUtility.DisplayDialog("Edit Mode", "Khong co cong trinh nao can doi so o.", "OK"); return; }
        Debug.Log("[Edit Mode] DE XUAT so o:\n" + sb);
        string xem = sb.Length > 1400 ? sb.ToString(0, 1400) + "\n... (xem het trong Console)" : sb.ToString();
        if (!EditorUtility.DisplayDialog("Edit Mode", "Doi so o cho " + doi.Count + " cong trinh:\n\n" + xem + "\nAp dung?", "Ap dung", "Huy")) return;

        var prefabs = new List<string>(); var datas = new List<PlaceableItemData>();
        foreach (var x in doi) { datas.Add(x.d); string p = AssetDatabase.GetAssetPath(x.d.prefabToBuild); if (!prefabs.Contains(p)) prefabs.Add(p); }
        string bk = SaoLuu(EditorSceneManager.GetActiveScene().path, prefabs, datas);
        foreach (var x in doi)
        {
            Undo.RecordObject(x.d, "So o nha");
            x.d.gridSize = x.moi;
            EditorUtility.SetDirty(x.d);
            string p = AssetDatabase.GetAssetPath(x.d.prefabToBuild);
            var goc = PrefabUtility.LoadPrefabContents(p);
            try
            {
                var kit = goc.GetComponent<BuildingFootprintKit>();
                if (kit == null) kit = goc.AddComponent<BuildingFootprintKit>();
                kit.SoO = x.moi;
                var t = goc.transform.Find("Chan_De");
                if (t == null) { t = new GameObject("Chan_De").transform; t.SetParent(goc.transform, false); }
                t.localPosition = Vector3.zero; t.localScale = Vector3.one;
                var cd = t.GetComponent<ChanDe>(); if (cd == null) cd = t.gameObject.AddComponent<ChanDe>();
                cd.soO = x.moi;
                PrefabUtility.SaveAsPrefabAsset(goc, p);
            }
            finally { PrefabUtility.UnloadPrefabContents(goc); }
        }
        // Ban dat san trong scene (prefab instance) co the dang ghi de soO -> ep theo prefab
        foreach (var kit in Object.FindObjectsByType<BuildingFootprintKit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(kit.gameObject);
            if (src == null) continue;
            foreach (var x in doi)
                if (x.d.prefabToBuild == src && kit.SoO != x.moi) { Undo.RecordObject(kit, "So o nha"); kit.SoO = x.moi; EditorUtility.SetDirty(kit); }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Edit Mode", "Da doi so o cho " + doi.Count + " cong trinh. Backup: " + Path.GetFileName(bk) +
            "\nBam Ctrl+S. Nha nguoi choi da dat de len nhau se tu tach ra o trong gan nhat khi vao game.", "OK");
    }

    /// <summary>Be ngang WORLD cua phan anh co mau (bo vien trong suot) cua sprite lon nhat trong prefab.</summary>
    private static bool DoRongAnh(Transform goc, out float rong)
    {
        rong = 0f;
        SpriteRenderer lon = null; float dt = 0f;
        foreach (var sr in goc.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            bool kit = false;
            for (var t = sr.transform; t != null && t != goc; t = t.parent) if (t.name == "Kit_Nen" || t.name.Contains("Shadow") || t.name.Contains("Bong")) { kit = true; break; }
            if (kit) continue;
            var b = sr.sprite.bounds.size; float a = b.x * Mathf.Abs(sr.transform.lossyScale.x) * b.y * Mathf.Abs(sr.transform.lossyScale.y);
            if (a > dt) { dt = a; lon = sr; }
        }
        if (lon == null) return false;
        var sp = lon.sprite;
        float rongSp = sp.bounds.size.x;
        string path = AssetDatabase.GetAssetPath(sp.texture);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    float ti = tex.width / (float)sp.texture.width;
                    Rect r = sp.rect;
                    int x0 = Mathf.FloorToInt(r.x * ti), y0 = Mathf.FloorToInt(r.y * ti), w = Mathf.FloorToInt(r.width * ti), h = Mathf.FloorToInt(r.height * ti);
                    var px = tex.GetPixels32();
                    int minX = int.MaxValue, maxX = -1;
                    for (int y = 0; y < h; y++) { int row = (y0 + y) * tex.width; for (int x = 0; x < w; x++) if (px[row + x0 + x].a >= 40) { if (x < minX) minX = x; if (x > maxX) maxX = x; } }
                    if (maxX >= 0) rongSp = (maxX - minX + 1) / (sp.pixelsPerUnit * ti);
                }
            }
            finally { Object.DestroyImmediate(tex); }
        }
        rong = rongSp * Mathf.Abs(lon.transform.lossyScale.x) / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.x)) * Mathf.Abs(goc.lossyScale.x);
        return rong > 1f;
    }

    [MenuItem("Tools/Edit Mode/4. Hoan tac o dat 1x1 (chep lai ban sao luu moi nhat)", false, 21)]
    private static void HoanTac()
    {
        string goc = Directory.GetParent(Application.dataPath).FullName;
        string moiNhat = null;
        foreach (var d in Directory.GetDirectories(goc, TIEN_TO_BACKUP + "*"))
            if (moiNhat == null || string.CompareOrdinal(Path.GetFileName(d), Path.GetFileName(moiNhat)) > 0) moiNhat = d;
        if (moiNhat == null) { EditorUtility.DisplayDialog("Edit Mode", "Chua co ban sao luu o dat 1x1 nao.", "OK"); return; }
        var files = Directory.GetFiles(moiNhat, "*", SearchOption.AllDirectories);
        if (!EditorUtility.DisplayDialog("Edit Mode", $"Chep lai {files.Length} file tu {Path.GetFileName(moiNhat)}?\nThay doi CHUA LUU trong scene se mat.", "Hoan tac", "Huy")) return;
        string sceneDangMo = EditorSceneManager.GetActiveScene().path;
        bool coScene = false;
        foreach (var f in files)
        {
            string rel = f.Substring(moiNhat.Length + 1).Replace('\\', '/');
            string dich = Path.Combine(goc, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dich));
            File.Copy(f, dich, true);
            if (rel == sceneDangMo) coScene = true;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        if (coScene) EditorSceneManager.OpenScene(sceneDangMo, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Edit Mode", "Da tra o dat ve nhu truoc khi chay tool 3.", "OK");
    }
}
