// ============================================================================
//  CHO + MAY XAY: lap animation nhieu lop & TOI UU ANH (nen + cat gon)   (2026-09-25)
//
//  Tools > Farm Game > Cho (Market) > 3. Lap cho moi (nhieu lop, co animation)
//      Thay hinh cho cu (chợ.png) cua object "Market" bang bo art moi, GIU vi tri / scale / collider /
//      click mo cho / keo di chuyen. Them lop con MK_Flag, MK_Sign, MK_Lantern, MK_GlowL, MK_GlowR,
//      MK_Produce + MarketLayered. Backup scene + .meta truoc. Co Undo. Xong Ctrl+S.
//  Tools > Farm Game > Cho (Market) > 4. Tra lai cho cu
//
//  Tools > Farm Game > May Xay > 3. Toi uu anh may xay (nen + cat gon)
//  Tools > Farm Game > May Xay > 4. Tra lai anh may xay chua toi uu
//
//  TOI UU (vi sao): strip doi ve de nguyen canvas moi frame, phan lon la trong suot:
//      feedmill_gears 4800x448, feedmill_chute 3600x448, market_flag/sign 4920x820 moi file,
//      lai de KHONG NEN (textureCompression 0) -> ~65 MB RAM GPU cho 2 cong trinh, va rong
//      qua 4096 (nhieu may Android cu khong doc duoc).
//  Ban toi uu (thu muc _Opt, anh goc KHONG dong vao): chi cat dung vung co hinh cua moi frame,
//      xep luoi nho (vd gears 368x180), nen CompressedHQ, tat mipmap -> < 2 MB, vua 4096.
//      Lop con dat lech dung toa do cu nen nhin y het.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class BuildingArtSetupTool
{
    private const string MK = "Assets/Art/Buildings/Market/";
    private const string MK_OPT = "Assets/Art/Buildings/Market/_Opt/";
    private const string FM = "Assets/Art/Buildings/FeedMill/";
    private const string FM_OPT = "Assets/Art/Buildings/FeedMill/_Opt/";
    private const float PPU_CHO = 180f;      // 728 px be rong nha cho = 4.04 don vi ~ bang cho cu (4.02)
    private const float PPU_MAY = 200f;

    // Toa do local (tam nha cho = goc) do tu anh: ((x - 410) / 180, (434 - y) / 180)
    private static readonly Vector3 POS_FLAG = new Vector3(-0.6861f, -0.1222f, 0f);
    private static readonly Vector3 POS_SIGN = new Vector3(-1.2417f, 0.4111f, 0f);
    private static readonly Vector3 POS_LANTERN = new Vector3(-0.4444f, -0.1750f, 0f);
    private static readonly Vector3 POS_GLOW_L = new Vector3(-1.1056f, 0.1278f, 0f);
    private static readonly Vector3 POS_GLOW_R = new Vector3(0.2111f, -0.6222f, 0f);
    // May xay (chan giua duoi = goc): ((x - 300) / 200, (448 - y) / 200)
    private static readonly Vector3 POS_GEARS = new Vector3(0.445f, 0.74f, 0f);
    private static readonly Vector3 POS_CHUTE = new Vector3(0.8225f, 1.0375f, 0f);

    // =====================================================================
    //  CHO
    // =====================================================================
    [MenuItem("Tools/Farm Game/Cho (Market)/3. Lap cho moi (nhieu lop, co animation)", false, 20)]
    private static void LapCho()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var click = Object.FindFirstObjectByType<MarketClickOpen>(FindObjectsInactive.Include);
        if (click == null) { Bao("Khong thay object cho (MarketClickOpen). Mo SCN_Farm roi bam lai."); return; }
        var cho = click.transform;
        if (cho.GetComponent<MarketLayered>() != null) { Bao("Cho da la ban moi. Muon lap lai: bam muc 4 truoc."); return; }
        string[] can = { "market_base_opt", "market_flag_pack", "market_sign_pack", "market_lantern_opt" };
        var thieu = can.Where(t => !File.Exists(MK_OPT + t + ".png")).ToList();
        for (int i = 1; i <= 6; i++) if (!File.Exists(MK + $"market_produce_0{i}.png")) thieu.Add($"market_produce_0{i}");
        if (!File.Exists(MK + "market_lantern_glow.png")) thieu.Add("market_lantern_glow");
        if (thieu.Count > 0) { Bao("Thieu file:\n" + string.Join("\n", thieu)); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Cho", "Thay cho cu bang bo art moi (nha dung yen, co + bang gia + den + hang nay).\n\nBackup scene truoc. Hong: muc 4 hoac Ctrl+Z.", "Lap", "Huy")) return;

        string bk = Backup(cho.gameObject.scene, "_Backup_Market_", MK, MK_OPT);

        Don(MK_OPT + "market_base_opt.png", new Vector2(0.5f, 0.5f), PPU_CHO);
        Don(MK_OPT + "market_lantern_opt.png", new Vector2(0.5f, 0.5f), PPU_CHO);
        Don(MK + "market_lantern_glow.png", new Vector2(0.5f, 0.5f), PPU_CHO);
        for (int i = 1; i <= 6; i++) Don(MK + $"market_produce_0{i}.png", new Vector2(0.5f, 0.1f), PPU_CHO);
        Luoi(MK_OPT + "market_flag_pack.png", 6, 3, 271, 266, PPU_CHO);
        Luoi(MK_OPT + "market_sign_pack.png", 6, 3, 69, 72, PPU_CHO);

        Sprite baseSp = Mot(MK_OPT + "market_base_opt.png");
        Sprite[] flag = Nhieu(MK_OPT + "market_flag_pack.png"), sign = Nhieu(MK_OPT + "market_sign_pack.png");
        if (baseSp == null || flag.Length != 6 || sign.Length != 6) { Bao($"Cat sprite loi (base {(baseSp != null)}, flag {flag.Length}/6, sign {sign.Length}/6). Chua dong vao scene."); return; }

        Undo.SetCurrentGroupName("Lap cho moi");
        int nhom = Undo.GetCurrentGroup();
        var sr = cho.GetComponent<SpriteRenderer>();
        var ml = Undo.AddComponent<MarketLayered>(cho.gameObject);
        ml.spriteCu = sr != null ? sr.sprite : null;
        if (sr != null) { Undo.RecordObject(sr, "Lap cho"); sr.sprite = baseSp; }
        int o = sr != null ? sr.sortingOrder : 0;

        ml.sign = TaoLop(cho, "MK_Sign", sign[0], sr, o + 1, POS_SIGN, 1f);
        TaoLop(cho, "MK_Lantern", Mot(MK_OPT + "market_lantern_opt.png"), sr, o + 1, POS_LANTERN, 1f);
        ml.flag = TaoLop(cho, "MK_Flag", flag[0], sr, o + 2, POS_FLAG, 1f);
        var glow = Mot(MK + "market_lantern_glow.png");
        ml.glowL = TaoLop(cho, "MK_GlowL", glow, sr, o + 3, POS_GLOW_L, 0.6f);
        ml.glowR = TaoLop(cho, "MK_GlowR", glow, sr, o + 3, POS_GLOW_R, 0.6f);
        var goHang = new GameObject("MK_Produce");
        Undo.RegisterCreatedObjectUndo(goHang, "Lap cho");
        goHang.transform.SetParent(cho, false);
        ml.produceRoot = goHang.transform;
        ml.flagFrames = flag; ml.signFrames = sign;
        ml.produceSprites = Enumerable.Range(1, 6).Select(i => Mot(MK + $"market_produce_0{i}.png")).Where(s => s != null).ToArray();

        EditorUtility.SetDirty(ml);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(cho.gameObject.scene);
        Selection.activeTransform = cho;
        Debug.Log($"[Cho] Da lap cho moi. Backup: {Path.GetFileName(bk)}. Ctrl+S de luu.");
        Bao("Xong. Bam Ctrl+S roi Play.\n\nChinh tay: chon Market > MarketLayered (toc do co, den, cho hang nay) hoac keo cac lop MK_*.\nHong: muc 4 hoac Ctrl+Z. Backup: " + Path.GetFileName(bk));
    }

    [MenuItem("Tools/Farm Game/Cho (Market)/4. Tra lai cho cu", false, 21)]
    private static void TraLaiCho()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ml = Object.FindFirstObjectByType<MarketLayered>(FindObjectsInactive.Include);
        if (ml == null) { Bao("Scene khong co cho ban moi."); return; }
        Undo.SetCurrentGroupName("Tra lai cho cu");
        int nhom = Undo.GetCurrentGroup();
        var go = ml.gameObject;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && ml.spriteCu != null) { Undo.RecordObject(sr, "Tra lai cho"); sr.sprite = ml.spriteCu; }
        foreach (var ten in new[] { "MK_Flag", "MK_Sign", "MK_Lantern", "MK_GlowL", "MK_GlowR", "MK_Produce" })
        {
            var c = go.transform.Find(ten);
            if (c != null) Undo.DestroyObjectImmediate(c.gameObject);
        }
        Undo.DestroyObjectImmediate(ml);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Bao("Da tra lai cho cu. Bam Ctrl+S.");
    }

    // =====================================================================
    //  MAY XAY: toi uu anh
    // =====================================================================
    [MenuItem("Tools/Farm Game/May Xay/3. Toi uu anh may xay (nen + cat gon)", false, 12)]
    private static void ToiUuMay()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var fm = Object.FindFirstObjectByType<FeedMillLayered>(FindObjectsInactive.Include);
        if (fm == null) { Bao("Chua lap may xay moi (muc 1)."); return; }
        if (!File.Exists(FM_OPT + "feedmill_gears_pack.png") || !File.Exists(FM_OPT + "feedmill_chute_pack.png")) { Bao("Thieu anh toi uu trong " + FM_OPT); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string bk = Backup(fm.gameObject.scene, "_Backup_FeedMillOpt_", FM, FM_OPT);

        Luoi(FM_OPT + "feedmill_gears_pack.png", 8, 4, 92, 90, PPU_MAY);
        Luoi(FM_OPT + "feedmill_chute_pack.png", 6, 3, 101, 133, PPU_MAY);
        Sprite[] g = Nhieu(FM_OPT + "feedmill_gears_pack.png"), c = Nhieu(FM_OPT + "feedmill_chute_pack.png");
        if (g.Length != 8 || c.Length != 6) { Bao($"Cat sprite loi (gears {g.Length}/8, chute {c.Length}/6). Chua dong vao scene."); return; }
        // Nen cac anh may xay con dung truc tiep
        foreach (var t in new[] { "feedmill_base", "feedmill_crate_empty", "feedmill_crate_full", "feedmill_lamp_glow", "smoke_puff_01", "smoke_puff_02", "smoke_puff_03" })
            NenLai(FM + t + ".png");

        Undo.RecordObject(fm, "Toi uu may xay");
        fm.gearFrames = g; fm.chuteFrames = c;
        if (fm.gears != null) { Undo.RecordObject(fm.gears, "Toi uu"); Undo.RecordObject(fm.gears.transform, "Toi uu"); fm.gears.sprite = g[0]; fm.gears.transform.localPosition = POS_GEARS; }
        if (fm.chute != null) { Undo.RecordObject(fm.chute, "Toi uu"); Undo.RecordObject(fm.chute.transform, "Toi uu"); fm.chute.sprite = c[0]; fm.chute.transform.localPosition = POS_CHUTE; }
        EditorUtility.SetDirty(fm);
        EditorSceneManager.MarkSceneDirty(fm.gameObject.scene);
        Debug.Log($"[MayXay] Da toi uu anh (gears 4800x448 -> 368x180, chute 3600x448 -> 303x266, nen CompressedHQ). Backup: {Path.GetFileName(bk)}. Ctrl+S.");
        Bao("Xong. Bam Ctrl+S roi Play: may phai nhin y het cu.\nTra lai: muc 4.");
    }

    [MenuItem("Tools/Farm Game/May Xay/4. Tra lai anh may xay chua toi uu", false, 13)]
    private static void TraLaiMay()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var fm = Object.FindFirstObjectByType<FeedMillLayered>(FindObjectsInactive.Include);
        if (fm == null) { Bao("Scene khong co may xay ban moi."); return; }
        Sprite[] g = Nhieu(FM + "feedmill_gears.png"), c = Nhieu(FM + "feedmill_chute.png");
        if (g.Length != 8 || c.Length != 6) { Bao("Khong doc duoc strip goc."); return; }
        Undo.RecordObject(fm, "Tra lai anh may");
        fm.gearFrames = g; fm.chuteFrames = c;
        if (fm.gears != null) { Undo.RecordObject(fm.gears, "Tra lai"); Undo.RecordObject(fm.gears.transform, "Tra lai"); fm.gears.sprite = g[0]; fm.gears.transform.localPosition = Vector3.zero; }
        if (fm.chute != null) { Undo.RecordObject(fm.chute, "Tra lai"); Undo.RecordObject(fm.chute.transform, "Tra lai"); fm.chute.sprite = c[0]; fm.chute.transform.localPosition = Vector3.zero; }
        EditorUtility.SetDirty(fm);
        EditorSceneManager.MarkSceneDirty(fm.gameObject.scene);
        Bao("Da tra lai strip goc. Bam Ctrl+S.");
    }

    // =====================================================================
    //  Helpers
    // =====================================================================
    private static void Bao(string s) => EditorUtility.DisplayDialog("Cong trinh", s, "OK");

    private static string Backup(UnityEngine.SceneManagement.Scene scene, string tienTo, params string[] thuMuc)
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, tienTo + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        if (!string.IsNullOrEmpty(scene.path)) File.Copy(Path.Combine(goc, scene.path), Path.Combine(bk, Path.GetFileName(scene.path)), true);
        foreach (var tm in thuMuc)
        {
            if (!Directory.Exists(tm)) continue;
            foreach (var m in Directory.GetFiles(tm, "*.png.meta"))
                File.Copy(m, Path.Combine(bk, Path.GetFileName(tm.TrimEnd('/')) + "__" + Path.GetFileName(m)), true);
        }
        return bk;
    }

    private static SpriteRenderer TaoLop(Transform cha, string ten, Sprite sp, SpriteRenderer mau, int order, Vector3 pos, float scale)
    {
        var go = new GameObject(ten);
        Undo.RegisterCreatedObjectUndo(go, "Lap lop");
        go.transform.SetParent(cha, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp;
        if (mau != null) { r.sharedMaterial = mau.sharedMaterial; r.sortingLayerID = mau.sortingLayerID; }
        r.sortingOrder = order;
        return r;
    }

    private static TextureImporter ChuanBi(string path, SpriteImportMode mode, float ppu)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = mode;
        ti.spritePixelsPerUnit = ppu;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.alphaIsTransparency = true;
        ti.isReadable = false;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.crunchedCompression = false;
        return ti;
    }

    private static void NenLai(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.mipmapEnabled = false;
        ti.isReadable = false;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.crunchedCompression = false;
        if (ti.maxTextureSize > 2048) ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
    }

    private static void Don(string path, Vector2 pivot, float ppu)
    {
        var ti = ChuanBi(path, SpriteImportMode.Single, ppu);
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    /// <summary>Cat anh luoi: n frame, cols cot, moi o cw x ch, frame 0 o goc TREN-TRAI, pivot giua o.</summary>
    private static void Luoi(string path, int n, int cols, int cw, int ch, float ppu)
    {
        var ti = ChuanBi(path, SpriteImportMode.Multiple, ppu);
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        var f = new SpriteDataProviderFactories(); f.Init();
        var dp = f.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var cu = dp.GetSpriteRects();
        string ten = Path.GetFileNameWithoutExtension(path);
        var ds = new SpriteRect[n];
        for (int k = 0; k < n; k++)
        {
            string nm = ten + "_" + k;
            var co = cu.FirstOrDefault(r => r.name == nm);
            int col = k % cols, row = k / cols;
            ds[k] = new SpriteRect
            {
                name = nm,
                rect = new Rect(col * cw, h - (row + 1) * ch, cw, ch),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = co != null ? co.spriteID : GUID.Generate(),
            };
        }
        dp.SetSpriteRects(ds);
        var nf = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nf != null) nf.SetNameFileIdPairs(ds.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
        ti.SaveAndReimport();
    }

    private static Sprite Mot(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static Sprite[] Nhieu(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => { int k = s.name.LastIndexOf('_'); return k >= 0 && int.TryParse(s.name.Substring(k + 1), out int v) ? v : 0; })
            .ToArray();
    }
}
