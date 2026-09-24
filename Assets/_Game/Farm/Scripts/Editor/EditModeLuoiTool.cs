// ============================================================================
//  Tools > Edit Mode  (2026-09-24) — xep map "tung o tung o" nhu video mau
//
//  1. O dat 2x2 khit luoi + keo cong trinh vao luoi
//       • Anh o dat (GroundSprite) duoc nan dung bang 2x2 o luoi = 600 x 300 world, chan hinh
//         thoi dat DUNG goc object (quy uoc neo cua PlacementManager) -> 2 o dat canh nhau
//         khit mep, khung xanh luc keo om sat mat dat.
//       • Ap cho: moi o dat trong scene + prefab o dat mua tu shop (gridSize = 2x2).
//       • Collider, thanh tien do, icon, cay trong... cung duoc co/doi theo (giu dung vi tri
//         tuong doi tren mat dat).
//       • Moi o dat va cong trinh keo duoc trong scene duoc hut vao o luoi gan nhat, khong
//         cho chong len nhau.
//       • Save cu cua nguoi choi tu dich 1 lan luc load (BuildingFootprintKit.PhienBanNeo).
//       • Sao luu scene + prefab ra thu muc _Backup_EditModeTool_<gio> truoc khi sua.
//       Chay trong SCN_Farm. Xong bam Ctrl+S. Khong ung: Ctrl+Z (scene) hoac chep lai backup.
//  2. Kiem tra (chi doc): liet ke o dat / cong trinh lech luoi, o dat sai co.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditModeLuoiTool
{
    private static readonly Vector2Int O_DAT = new Vector2Int(2, 2);

    // ─────────────────────────────────────────────────────────────────────
    // [2026-09-24] TAT menu: lan chay dau lam chau hoa / o dat to bat thuong va chong len nhau (do sai kich thuoc
    // chau hoa). Dung menu 0 de hoan tac. Giu code de tham khao, KHONG hien trong menu nua.
    private static void Chay()
    {
        IsoGrid.ResetCache();
        if (IsoGrid.SceneGrid == null)
        {
            EditorUtility.DisplayDialog("Edit Mode", "Khong thay Grid_Iso45. Hay mo SCN_Farm roi chay lai.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Edit Mode",
                "Nan moi o dat thanh 2x2 o luoi (600x300) va hut o dat + cong trinh vao luoi?\n\n" +
                "Scene + prefab o dat se duoc sao luu truoc. Xong nho Ctrl+S.", "Lam", "Huy"))
            return;

        var scene = EditorSceneManager.GetActiveScene();
        string backup = SaoLuu(scene.path);

        var log = new System.Text.StringBuilder();
        Vector2 wh = IsoGrid.FootprintWorldSize(O_DAT);
        log.AppendLine($"[Edit Mode] O luoi {IsoGrid.CellWidth:0}x{IsoGrid.CellHeight:0} -> o dat 2x2 = {wh.x:0}x{wh.y:0} world. Backup: {backup}");

        // ── 1. Prefab o dat (mua tu shop) ────────────────────────────────────
        int soPrefab = 0;
        var daLam = new HashSet<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data == null || data.prefabToBuild == null) continue;
            if (data.prefabToBuild.GetComponentInChildren<PlotController>(true) == null) continue;

            string pfPath = AssetDatabase.GetAssetPath(data.prefabToBuild);
            if (!string.IsNullOrEmpty(pfPath) && daLam.Add(pfPath))
            {
                var goc = PrefabUtility.LoadPrefabContents(pfPath);
                try
                {
                    if (NanODat(goc.transform, out Vector3 lechGoc, log))
                    {
                        var kit = goc.GetComponent<BuildingFootprintKit>();
                        if (kit == null) kit = goc.AddComponent<BuildingFootprintKit>();
                        kit.SoO = O_DAT;
                        kit.PhienBanNeo = kit.PhienBanNeo + 1;
                        kit.LechNeoCu = goc.transform.TransformVector(lechGoc);
                        PrefabUtility.SaveAsPrefabAsset(goc, pfPath);
                        soPrefab++;
                        log.AppendLine($"  prefab {pfPath}: lech neo cu {kit.LechNeoCu}, phien ban neo {kit.PhienBanNeo}");
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(goc); }
            }
            if (data.gridSize != O_DAT)
            {
                Undo.RecordObject(data, "O dat 2x2");
                data.gridSize = O_DAT;
                EditorUtility.SetDirty(data);
                log.AppendLine($"  data {data.name}: gridSize -> 2x2");
            }
        }

        // ── 2. O dat trong scene ─────────────────────────────────────────────
        var chiem = new List<RectInt>();
        int soODat = 0;
        var plots = Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(plots, (a, b) =>
        {
            int c = b.transform.position.y.CompareTo(a.transform.position.y);
            return c != 0 ? c : a.transform.position.x.CompareTo(b.transform.position.x);
        });
        foreach (var pc in plots)
        {
            if (pc == null || EditorUtility.IsPersistent(pc)) continue;
            Transform goc = pc.transform;
            Undo.RegisterFullObjectHierarchyUndo(goc.gameObject, "O dat 2x2");
            if (!NanODat(goc, out Vector3 lech, log)) continue;
            Vector3 chanMoi = goc.position + goc.TransformVector(lech);
            RectInt r = TimOTrong(chanMoi, O_DAT, chiem);
            chiem.Add(r);
            Vector3 neo = IsoGrid.RectAnchorWorld(r);
            goc.position = new Vector3(neo.x, neo.y, goc.position.z);

            var kit = goc.GetComponent<BuildingFootprintKit>();
            if (kit == null) kit = Undo.AddComponent<BuildingFootprintKit>(goc.gameObject);
            kit.SoO = O_DAT;
            EditorUtility.SetDirty(kit);
            soODat++;
        }

        // ── 3. Cong trinh keo duoc khac trong scene -> hut vao luoi ─────────────
        int soCT = 0;
        foreach (var eb in Object.FindObjectsByType<EditableBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (eb == null || eb.GetComponent<PlotController>() != null) continue;
            var kit = eb.GetComponent<BuildingFootprintKit>();
            Vector2Int soO = kit != null && kit.SoO.x > 0 && kit.SoO.y > 0 ? kit.SoO : Vector2Int.one;
            Transform t = eb.transform;
            // Chi hut vao o gan nhat (dich toi da nua o), KHONG day di cho khac -> giu bo cuc tay cua Sep
            Vector3 neo = IsoGrid.SnapAnchor(t.position, soO);
            if ((new Vector2(neo.x, neo.y) - (Vector2)t.position).sqrMagnitude < 0.01f) continue;
            Undo.RecordObject(t, "Hut vao luoi");
            t.position = new Vector3(neo.x, neo.y, t.position.z);
            soCT++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        log.AppendLine($"[Edit Mode] Xong: {soODat} o dat trong scene, {soPrefab} prefab o dat, {soCT} cong trinh hut vao luoi. Bam Ctrl+S de luu scene.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Edit Mode",
            $"Xong: {soODat} o dat trong scene, {soPrefab} prefab o dat, {soCT} cong trinh da hut vao luoi.\n\nBam Ctrl+S de luu scene. Chi tiet trong Console.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Edit Mode/0. HOAN TAC tool 1 (tra scene + prefab o dat, chau hoa ve nhu cu)", false, 9)]
    private static void HoanTac()
    {
        string goc = Directory.GetParent(Application.dataPath).FullName;
        string moiNhat = null;
        foreach (var d in Directory.GetDirectories(goc, "_Backup_EditModeTool_*"))
            if (moiNhat == null || string.CompareOrdinal(Path.GetFileName(d), Path.GetFileName(moiNhat)) > 0) moiNhat = d;
        if (moiNhat == null) { EditorUtility.DisplayDialog("Edit Mode", "Khong thay thu muc _Backup_EditModeTool_* nao.", "OK"); return; }

        var files = Directory.GetFiles(moiNhat, "*", SearchOption.AllDirectories);
        if (!EditorUtility.DisplayDialog("Edit Mode",
                "Chep lai " + files.Length + " file tu " + Path.GetFileName(moiNhat) + " (scene SCN_Farm + prefab o dat, chau hoa + data).\n\n" +
                "Moi thay doi CHUA LUU trong SCN_Farm se mat. Tiep tuc?", "Hoan tac", "Huy"))
            return;

        string sceneDangMo = EditorSceneManager.GetActiveScene().path;
        bool coScene = false;
        foreach (var f in files)
        {
            string rel = f.Substring(moiNhat.Length + 1).Replace('\\', '/');
            string dich = Path.Combine(goc, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dich));
            File.Copy(f, dich, true);
            if (rel == sceneDangMo) coScene = true;
            Debug.Log("[Edit Mode] Hoan tac: " + rel);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        if (coScene) EditorSceneManager.OpenScene(sceneDangMo, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Edit Mode", "Da tra ve nhu truoc khi chay tool 1.\nNeu da bam Tools > VFX > 7 sau luc do thi bam lai 1 lan roi Ctrl+S.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Edit Mode/2. Kiem tra: o dat / cong trinh lech luoi (chi doc)", false, 11)]
    private static void KiemTra()
    {
        IsoGrid.ResetCache();
        if (IsoGrid.SceneGrid == null) { EditorUtility.DisplayDialog("Edit Mode", "Hay mo SCN_Farm.", "OK"); return; }
        var sb = new System.Text.StringBuilder("[Edit Mode] KIEM TRA LUOI\n");
        int lech = 0;
        foreach (var eb in Object.FindObjectsByType<EditableBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var kit = eb.GetComponent<BuildingFootprintKit>();
            Vector2Int soO = kit != null && kit.SoO.x > 0 ? kit.SoO : Vector2Int.one;
            Vector3 p = eb.transform.position;
            Vector3 s = IsoGrid.SnapAnchor(p, soO);
            float d = Vector2.Distance(p, s);
            if (d > 0.5f) { lech++; sb.AppendLine($"  LECH {d:0} unit: {eb.name} ({soO.x}x{soO.y}) tai ({p.x:0},{p.y:0})"); }
        }
        foreach (var pc in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var g = TimDat(pc.transform);
            if (g == null) continue;
            if (!DoHinhThoi(g, out Vector2 chan, out float rong, out float cao)) continue;
            Vector3 chanW = g.transform.TransformPoint(chan);
            float rongW = rong * Mathf.Abs(g.transform.lossyScale.x);
            sb.AppendLine($"  o dat {pc.name}: mat dat rong {rongW:0} (can {IsoGrid.FootprintWorldSize(O_DAT).x:0}), chan lech goc {Vector2.Distance(chanW, pc.transform.position):0} unit");
        }
        sb.AppendLine($"Tong cong trinh lech luoi: {lech}");
        Debug.Log(sb.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  NAN 1 O DAT: co + doi con sao cho hinh thoi mat dat = 2x2 o, chan o goc
    // ═════════════════════════════════════════════════════════════════════
    /// <param name="lech">vi tri chan mat dat CU trong he toa do cua goc (truoc khi nan).</param>
    private static bool NanODat(Transform goc, out Vector3 lech, System.Text.StringBuilder log)
    {
        lech = Vector3.zero;
        var g = TimDat(goc);
        if (g == null) { log.AppendLine($"  bo qua {goc.name}: khong co GroundSprite"); return false; }
        if (!DoHinhThoi(g, out Vector2 chanSp, out float rongSp, out float caoSp))
        { log.AppendLine($"  bo qua {goc.name}: khong doc duoc anh mat dat"); return false; }

        // Chan + kich thuoc mat dat trong he toa do GOC
        Vector3 B = goc.InverseTransformPoint(g.transform.TransformPoint(chanSp));
        float sxG = Mathf.Abs(g.transform.lossyScale.x) / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.x));
        float syG = Mathf.Abs(g.transform.lossyScale.y) / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.y));
        float rong = rongSp * sxG, cao = caoSp * syG;
        if (rong < 1e-4f || cao < 1e-4f) return false;

        Vector2 dich = IsoGrid.FootprintWorldSize(O_DAT);
        float rongDich = dich.x / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.x));
        float caoDich  = dich.y / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.y));
        float k = rongDich / rong;

        // Da dung co + chan dung goc roi -> khong lam lai (chay tool 2 lan khong phinh them)
        if (Mathf.Abs(k - 1f) < 0.01f && ((Vector2)B).magnitude < 1f / Mathf.Max(1e-5f, Mathf.Abs(goc.lossyScale.x)))
        { log.AppendLine($"  {goc.name}: da dung 2x2, giu nguyen"); return true; }

        // Co + doi TAT CA con truc tiep quanh chan cu (giu vi tri tuong doi cua cay, icon, thanh tien do)
        for (int i = 0; i < goc.childCount; i++)
        {
            var c = goc.GetChild(i);
            if (c.name == "Kit_Nen") continue;
            Vector3 lp = c.localPosition, ls = c.localScale;
            c.localPosition = new Vector3((lp.x - B.x) * k, (lp.y - B.y) * k, lp.z);
            c.localScale = new Vector3(ls.x * k, ls.y * k, ls.z);
        }
        // Collider tren chinh goc
        foreach (var col in goc.GetComponents<Collider2D>())
        {
            Undo.RecordObject(col, "O dat 2x2");
            col.offset = ((Vector2)col.offset - (Vector2)B) * k;
            if (col is BoxCollider2D bx) bx.size = bx.size * k;
            else if (col is CircleCollider2D cc) cc.radius *= k;
            else if (col is CapsuleCollider2D cp) cp.size = cp.size * k;
            else if (col is PolygonCollider2D pg)
            {
                for (int p = 0; p < pg.pathCount; p++)
                {
                    var pts = pg.GetPath(p);
                    for (int j = 0; j < pts.Length; j++) pts[j] = (pts[j] - (Vector2)B) * k;
                    pg.SetPath(p, pts);
                }
            }
            EditorUtility.SetDirty(col);
        }

        // Keo nhe chieu cao mat dat cho dung ti le 2:1 cua o luoi (art 2.1:1 -> khit tren duoi)
        float e = caoDich / (cao * k);
        if (Mathf.Abs(e - 1f) > 0.005f && Mathf.Abs(e - 1f) < 0.2f)
        {
            Transform gt = g.transform;
            Vector3 chanCha = gt.localPosition + Vector3.Scale(gt.localScale, (Vector3)chanSp);
            gt.localScale = new Vector3(gt.localScale.x, gt.localScale.y * e, gt.localScale.z);
            gt.localPosition = new Vector3(gt.localPosition.x, chanCha.y - gt.localScale.y * chanSp.y, gt.localPosition.z);
        }

        lech = B;
        log.AppendLine($"  {goc.name}: mat dat {rong * Mathf.Abs(goc.lossyScale.x):0}x{cao * Mathf.Abs(goc.lossyScale.y):0} -> {dich.x:0}x{dich.y:0} (x{k:0.###}, keo doc x{e:0.###})");
        return true;
    }

    private static SpriteRenderer TimDat(Transform goc)
    {
        var t = goc.Find("GroundSprite");
        if (t == null)
            foreach (var tr in goc.GetComponentsInChildren<Transform>(true))
                if (tr.name == "GroundSprite") { t = tr; break; }
        return t != null ? t.GetComponent<SpriteRenderer>() : null;
    }

    /// <summary>
    /// Do hinh thoi mat dat THAT (bo vien trong suot) trong he toa do sprite:
    /// chan = dinh duoi (giua ngang), rong/cao = hop bao phan anh co mau.
    /// Doc file PNG goc (khong can bat Read/Write cho texture).
    /// </summary>
    private static bool DoHinhThoi(SpriteRenderer g, out Vector2 chan, out float rong, out float cao)
    {
        chan = Vector2.zero; rong = cao = 0f;
        var sp = g.sprite;
        if (sp == null || sp.texture == null) return false;
        string path = AssetDatabase.GetAssetPath(sp.texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!tex.LoadImage(File.ReadAllBytes(path))) return false;
            float ti = tex.width / (float)sp.texture.width;          // anh goc co the lon hon ban import
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
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }
            if (maxX < 0) return false;
            float ppu = sp.pixelsPerUnit;
            Vector2 piv = sp.pivot * ti;
            float cx = (minX + maxX + 1) * 0.5f;
            chan = new Vector2((cx - piv.x) / (ppu * ti), (minY - piv.y) / (ppu * ti));
            if (g.flipX) chan.x = -chan.x;
            rong = (maxX - minX + 1) / (ppu * ti);
            cao  = (maxY - minY + 1) / (ppu * ti);
            return true;
        }
        finally { Object.DestroyImmediate(tex); }
    }

    /// <summary>O luoi gan nhat cho vat NxM, tranh chong len vung da chiem (tim xoay vong toi 5 o).</summary>
    private static RectInt TimOTrong(Vector3 neoWorld, Vector2Int soO, List<RectInt> chiem)
    {
        RectInt goc = IsoGrid.RectFromAnchor(neoWorld, soO);
        if (!Chong(goc, chiem)) return goc;
        RectInt tot = goc; float totD = float.MaxValue;
        for (int ban = 1; ban <= 5; ban++)
        {
            for (int dx = -ban; dx <= ban; dx++)
                for (int dy = -ban; dy <= ban; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != ban) continue;
                    var r = new RectInt(goc.x + dx, goc.y + dy, soO.x, soO.y);
                    if (Chong(r, chiem)) continue;
                    float d = Vector2.Distance(IsoGrid.RectAnchorWorld(r), neoWorld);
                    if (d < totD) { totD = d; tot = r; }
                }
            if (totD < float.MaxValue) return tot;
        }
        return goc;
    }

    private static bool Chong(RectInt r, List<RectInt> ds)
    {
        foreach (var o in ds) if (r.Overlaps(o)) return true;
        return false;
    }

    private static string SaoLuu(string scenePath)
    {
        string goc = Directory.GetParent(Application.dataPath).FullName;
        string thuMuc = Path.Combine(goc, "_Backup_EditModeTool_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
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
        foreach (var guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            string ap = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(ap);
            if (data == null || data.prefabToBuild == null) continue;
            if (data.prefabToBuild.GetComponentInChildren<PlotController>(true) == null) continue;
            Chep(ap);
            Chep(AssetDatabase.GetAssetPath(data.prefabToBuild));
        }
        return thuMuc;
    }
}
