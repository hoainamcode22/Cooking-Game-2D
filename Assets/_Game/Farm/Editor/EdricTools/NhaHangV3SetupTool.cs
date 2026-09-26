// ============================================================================
//  Edric Tools > Nha hang (Restaurant) > 1. Lap nha hang moi v3   (2026-09-25)
//                                       > 2. Tra lai nha hang cu
//  Object "CookingGate" (co KitchenClickOpen, bam vao mo bep): doi hinh sang Restaurant_v3, GIU vi tri,
//  scale, collider, script mo bep. Them 4 lop con: lua 2 bep, lua lo pizza, bang treo dung dua + diem khoi.
//  So lieu (do tu anh, khong uoc luong):
//    Nha cu nhahang.png: phan co hinh rong 307 px / PPU 100 = 3.07, day o local y -1.655, tam x 0.92.
//    Nha moi 1024x1024: phan co hinh x 95..946 (851 px), day y 977 -> PPU 851/3.07 = 277.5,
//    pivot (265.2, 517.7 px tu tren) de day + tam ngang trung cho cu -> dung dung o luoi cu, cung be ngang.
//    Lop con dat theo px tren anh nha moi (xem LOP). Backup scene truoc. Co Undo. Xong Ctrl+S.
// ============================================================================
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NhaHangV3SetupTool
{
    private const string GOC = "Edric Tools/Nha hang (Restaurant)/";
    private const string OPT = "Assets/Art/Buildings/Restaurant_v3/_Opt/";
    private const float PPU = 277.5f;
    private static readonly Vector2 PIVOT_PX = new Vector2(265.2f, 517.7f);

    // ten, file frame (khong so), px neo tren anh nha, ti le (px frame -> px nha), pivot frame
    private static readonly (string ten, string file, Vector2 px, float tiLe, Vector2 pivot)[] LOP =
    {
        ("RS_Stove1", "rest_stove_fire_", new Vector2(222, 668), 0.35f, new Vector2(0.5f, 0.05f)),
        ("RS_Stove2", "rest_stove_fire_", new Vector2(318, 640), 0.35f, new Vector2(0.5f, 0.05f)),
        ("RS_Oven",   "oven_fire_",    new Vector2(759, 651), 0.37f, new Vector2(0.5f, 0f)),
        ("RS_Sign",   "rest_sign_",    new Vector2(62, 455),  0.5f,  new Vector2(0.5f, 1f)),
    };
    private static readonly Vector2 KHOI_PX = new Vector2(672, 50);

    [MenuItem(GOC + "1. Lap nha hang moi v3", false, 1)]
    public static void Lap()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var click = Object.FindFirstObjectByType<KitchenClickOpen>(FindObjectsInactive.Include);
        if (click == null) { Bao("Khong thay nha hang (KitchenClickOpen / CookingGate). Mo SCN_Farm roi bam lai."); return; }
        var nha = click.transform;
        if (nha.GetComponent<RestaurantLayered>() != null) { Bao("Nha hang da la ban moi. Muon lap lai: bam muc 2 truoc."); return; }
        var sr = nha.GetComponent<SpriteRenderer>();
        if (sr == null) { Bao("CookingGate khong co SpriteRenderer."); return; }
        var thieu = new System.Collections.Generic.List<string>();
        if (!File.Exists(OPT + "restaurant_base_opt.png")) thieu.Add("restaurant_base_opt");
        foreach (var l in LOP) for (int i = 0; i < 6; i++) if (!File.Exists(OPT + l.file + i + ".png")) thieu.Add(l.file + i);
        if (thieu.Count > 0) { Bao("Thieu file:\n" + string.Join("\n", thieu.Distinct())); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Nha hang v3", "Thay hinh nha hang bang ban v3 (bep mo) + lua bep, lua lo pizza, bang treo, khoi ong khoi.\nGiu vi tri, collider, bam mo bep.\n\nBackup scene truoc. Hong: muc 2 hoac Ctrl+Z.", "Lap", "Huy")) return;

        string gocDuAn = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(gocDuAn, "_Backup_NhaHangV3_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var sc = nha.gameObject.scene;
        if (!string.IsNullOrEmpty(sc.path)) File.Copy(Path.Combine(gocDuAn, sc.path), Path.Combine(bk, Path.GetFileName(sc.path)), true);

        Nhap(OPT + "restaurant_base_opt.png", PPU, new Vector2(PIVOT_PX.x / 1024f, (1024f - PIVOT_PX.y) / 1024f), 1024);
        var baseSp = AssetDatabase.LoadAssetAtPath<Sprite>(OPT + "restaurant_base_opt.png");
        if (baseSp == null) { Bao("Khong nap duoc restaurant_base_opt. Chua dong vao scene."); return; }

        Undo.SetCurrentGroupName("Lap nha hang v3");
        int nhom = Undo.GetCurrentGroup();
        var rl = Undo.AddComponent<RestaurantLayered>(nha.gameObject);
        rl.spriteCu = sr.sprite;
        Undo.RecordObject(sr, "Lap nha hang");
        sr.sprite = baseSp;

        foreach (var l in LOP)
        {
            var fr = new Sprite[6];
            for (int i = 0; i < 6; i++)
            {
                string p = OPT + l.file + i + ".png";
                Nhap(p, PPU / l.tiLe, l.pivot, 256);
                fr[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            }
            var go = new GameObject(l.ten);
            Undo.RegisterCreatedObjectUndo(go, "Lap nha hang");
            go.transform.SetParent(nha, false);
            go.transform.localPosition = PxSangLocal(l.px);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = fr[0];
            r.sharedMaterial = sr.sharedMaterial;
            r.sortingLayerID = sr.sortingLayerID;
            r.sortingOrder = sr.sortingOrder + (l.ten == "RS_Sign" ? 3 : 1);
            if (l.ten == "RS_Stove1") { rl.stove1 = r; rl.stoveFrames = fr; }
            else if (l.ten == "RS_Stove2") rl.stove2 = r;
            else if (l.ten == "RS_Oven") { rl.oven = r; rl.ovenFrames = fr; }
            else { rl.sign = r; rl.signFrames = fr; }
        }
        var khoi = new GameObject("RS_SmokePoint");
        Undo.RegisterCreatedObjectUndo(khoi, "Lap nha hang");
        khoi.transform.SetParent(nha, false);
        khoi.transform.localPosition = PxSangLocal(KHOI_PX);
        rl.smokePoint = khoi.transform;

        EditorUtility.SetDirty(rl);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(sc);
        Selection.activeTransform = nha;
        Debug.Log($"[NhaHangV3] Da lap. Backup: {Path.GetFileName(bk)}. Ctrl+S de luu.");
        Bao("Xong. Bam Ctrl+S roi Play.\n\nChinh tay: keo RS_Stove1/2, RS_Oven, RS_Sign, RS_SmokePoint; toc do trong RestaurantLayered.\nBam trung dung hinh: chay Edric Tools > Collider om khit (chon CookingGate).\nHong: muc 2 hoac Ctrl+Z. Backup: " + Path.GetFileName(bk));
    }

    // Dau bep dung SAU quay go (chan bi quay che), giua cot truoc va lo pizza. px tren anh nha moi.
    private static readonly Vector2 CHEF_CHAN_PX = new Vector2(640, 752);
    private const float CHEF_CAO_PX = 150f;         // dau bep cao ~150 px anh nha -> dau nam duoi mep mai bat

    [MenuItem(GOC + "3. Dua dau bep vao bep (dung sau quay)", false, 3)]
    public static void DuaDauBep()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var rl = Object.FindFirstObjectByType<RestaurantLayered>(FindObjectsInactive.Include);
        if (rl == null) { Bao("Chua lap nha hang v3. Bam muc 1 truoc."); return; }
        var nha = rl.transform;
        var sr = nha.GetComponent<SpriteRenderer>();
        Transform chef = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && !EditorUtility.IsPersistent(t) && t.name == "Chef_NPC" && t.GetComponent<SpriteRenderer>() != null) { chef = t; break; }
        if (chef == null) { Bao("Khong thay Chef_NPC trong scene."); return; }
        string p = OPT + "restaurant_counter_front.png";
        if (!File.Exists(p)) { Bao("Thieu " + p); return; }
        Nhap(p, PPU, new Vector2(PIVOT_PX.x / 1024f, (1024f - PIVOT_PX.y) / 1024f), 1024);
        var spTruoc = AssetDatabase.LoadAssetAtPath<Sprite>(p);
        if (spTruoc == null) { Bao("Khong nap duoc restaurant_counter_front."); return; }

        Undo.SetCurrentGroupName("Dua dau bep vao bep");
        int nhom = Undo.GetCurrentGroup();
        var csr = chef.GetComponent<SpriteRenderer>();
        var ys = chef.GetComponent<ChefYSort>();
        Undo.RecordObject(rl, "Dau bep");
        if (rl.chef == null)
        {
            rl.chef = chef; rl.chefChaCu = chef.parent; rl.chefPosCu = chef.position; rl.chefScaleCu = chef.localScale;
            rl.chefOrderCu = csr.sortingOrder; rl.chefLayerCu = csr.sortingLayerID; rl.chefYSortCu = ys != null && ys.enabled;
        }

        // Lop quay truoc (cat tu anh nha): ve tren dau bep
        var t0 = nha.Find("RS_CounterFront");
        GameObject go = t0 != null ? t0.gameObject : new GameObject("RS_CounterFront");
        if (t0 == null) { Undo.RegisterCreatedObjectUndo(go, "Dau bep"); go.transform.SetParent(nha, false); }
        go.transform.localPosition = new Vector3(0f, 0f, -0.02f);
        go.transform.localScale = Vector3.one;
        var q = go.GetComponent<SpriteRenderer>(); if (q == null) q = Undo.AddComponent<SpriteRenderer>(go);
        q.sprite = spTruoc; q.sharedMaterial = sr.sharedMaterial;
        q.sortingLayerID = sr.sortingLayerID; q.sortingOrder = sr.sortingOrder + 5;
        rl.quayTruoc = q;

        // Dau bep: con cua nha hang (di chuyen nha thi di theo), dung sau quay, sort giua nha va quay
        Undo.SetTransformParent(chef, nha, "Dau bep");
        Undo.RecordObject(chef, "Dau bep");
        chef.localPosition = PxSangLocal(CHEF_CHAN_PX) + new Vector3(0f, 0f, -0.005f);
        float caoHinh = csr.sprite != null ? csr.sprite.bounds.size.y : 0.74f;
        float s = (CHEF_CAO_PX / PPU) / Mathf.Max(0.01f, caoHinh);
        chef.localScale = new Vector3(s * Mathf.Sign(rl.chefScaleCu.x == 0 ? 1 : rl.chefScaleCu.x), s, 1f);
        Undo.RecordObject(csr, "Dau bep");
        csr.sortingLayerID = sr.sortingLayerID;
        csr.sortingOrder = sr.sortingOrder + 3;
        if (ys != null)
        {
            // Tat Y-sort (no keo dau bep ra truoc quay). Awake cua no van chay khi tat -> doi layer cua no
            // trung layer nha hang de khong bi dat lai sang "Objects" luc vao game.
            Undo.RecordObject(ys, "Dau bep");
            if (string.IsNullOrEmpty(rl.chefYSortLayerCu)) rl.chefYSortLayerCu = ys.sortingLayerName ?? "";
            ys.sortingLayerName = SortingLayer.IDToName(sr.sortingLayerID) ?? "";
            ys.enabled = false;
        }

        EditorUtility.SetDirty(rl);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(nha.gameObject.scene);
        Selection.activeTransform = chef;
        Bao("Xong. Dau bep dung sau quay go trong nha hang (chan bi quay che).\nKeo Chef_NPC de chinh cho dung. Bam Ctrl+S.\nHong: muc 4 hoac Ctrl+Z.");
    }

    [MenuItem(GOC + "4. Dua dau bep ve cho cu", false, 4)]
    public static void TraDauBep()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var rl = Object.FindFirstObjectByType<RestaurantLayered>(FindObjectsInactive.Include);
        if (rl == null || rl.chef == null) { Bao("Dau bep chua duoc dua vao bep."); return; }
        TraDauBepNoi(rl);
        Bao("Da dua dau bep ve cho cu. Bam Ctrl+S.");
    }

    private static void TraDauBepNoi(RestaurantLayered rl)
    {
        Undo.SetCurrentGroupName("Tra dau bep");
        var chef = rl.chef;
        if (chef != null)
        {
            Undo.SetTransformParent(chef, rl.chefChaCu, "Tra dau bep");
            Undo.RecordObject(chef, "Tra dau bep");
            chef.position = rl.chefPosCu; chef.localScale = rl.chefScaleCu;
            var csr = chef.GetComponent<SpriteRenderer>();
            if (csr != null) { Undo.RecordObject(csr, "Tra dau bep"); csr.sortingLayerID = rl.chefLayerCu; csr.sortingOrder = rl.chefOrderCu; }
            var ys = chef.GetComponent<ChefYSort>();
            if (ys != null) { Undo.RecordObject(ys, "Tra dau bep"); ys.enabled = rl.chefYSortCu; if (!string.IsNullOrEmpty(rl.chefYSortLayerCu)) ys.sortingLayerName = rl.chefYSortLayerCu; }
        }
        var q = rl.transform.Find("RS_CounterFront");
        if (q != null) Undo.DestroyObjectImmediate(q.gameObject);
        Undo.RecordObject(rl, "Tra dau bep");
        rl.chef = null; rl.quayTruoc = null; rl.chefYSortLayerCu = "";
        EditorSceneManager.MarkSceneDirty(rl.gameObject.scene);
    }

    [MenuItem(GOC + "2. Tra lai nha hang cu", false, 2)]
    public static void TraLai()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var rl = Object.FindFirstObjectByType<RestaurantLayered>(FindObjectsInactive.Include);
        if (rl == null) { Bao("Scene khong co nha hang ban moi."); return; }
        Undo.SetCurrentGroupName("Tra lai nha hang cu");
        int nhom = Undo.GetCurrentGroup();
        if (rl.chef != null) TraDauBepNoi(rl);        // dau bep dang la con nha hang -> dua ve truoc
        var go = rl.gameObject;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && rl.spriteCu != null) { Undo.RecordObject(sr, "Tra lai nha hang"); sr.sprite = rl.spriteCu; }
        foreach (var ten in LOP.Select(l => l.ten).Concat(new[] { "RS_SmokePoint" }))
        {
            var c = go.transform.Find(ten);
            if (c != null) Undo.DestroyObjectImmediate(c.gameObject);
        }
        Undo.DestroyObjectImmediate(rl);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Bao("Da tra lai nha hang cu. Bam Ctrl+S.");
    }

    private static Vector3 PxSangLocal(Vector2 px) => new Vector3((px.x - PIVOT_PX.x) / PPU, (PIVOT_PX.y - px.y) / PPU, -0.01f);

    private static void Nhap(string path, float ppu, Vector2 pivot, int maxSize)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = ppu;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = maxSize;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.crunchedCompression = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Nha hang v3", s, "OK");
}
