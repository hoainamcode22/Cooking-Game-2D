// ============================================================================
//  Tools > Farm Game > May Xay > 1 / 2   (2026-09-25)
//  1. Lap may thuc an moi (nhieu lop): thay hinh may cu (MayThucAn_Anim, sheet maylamthucan
//     ve lai ca nha moi frame nen bi rung) bang bo art moi trong Assets/Art/Buildings/FeedMill:
//       - cat strip feedmill_gears (8 frame) / feedmill_chute (6 frame), dat PPU 200, chan giua duoi
//         -> may moi CUNG KICH THUOC tren map voi may cu (600x448 px / 200 = 3 x 2.24 don vi)
//       - SpriteRenderer goc -> feedmill_base (nha dung yen), TAT Animator cu (khong xoa)
//       - them lop con FM_Gears / FM_Chute / FM_LampGlow / FM_Smoke / FM_Crates + FeedMillLayered
//       - GIU NGUYEN vi tri, scale, collider, click mo popup
//     Truoc khi lam: backup file scene + .meta cua art vao _Backup_FeedMill_<gio>. Co Undo. Xong Ctrl+S.
//  2. Tra lai may cu: go het lop moi, tra sprite + Animator cu.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class FeedMillSetupTool
{
    private const string Goc = "Tools/Farm Game/May Xay/";
    private const string ThuMuc = "Assets/Art/Buildings/FeedMill/";
    private const float PPU = 200f;

    // Toa do tren canvas 600x448 cua feedmill_base (goc tren-trai) -> doi ra don vi local (chan giua duoi)
    private static Vector3 TuCanvas(float x, float y) => new Vector3((x - 300f) / PPU, (448f - y) / PPU, 0f);
    private static readonly Vector3 VI_TRI_DEN = TuCanvas(165f, 276f);     // den tuong ben trai cua
    private static readonly Vector3 VI_TRI_KHOI = TuCanvas(217f, 58f);     // mieng ong khoi

    // =====================================================================
    [MenuItem(Goc + "1. Lap may thuc an moi (nhieu lop)", false, 10)]
    private static void Lap()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("May xay", "Thoat Play mode truoc.", "OK"); return; }
        var may = TimMay();
        if (may == null) { EditorUtility.DisplayDialog("May xay", "Khong thay may thuc an (MillBuildingClick) trong scene. Mo SCN_Farm roi bam lai.", "OK"); return; }
        if (may.GetComponent<FeedMillLayered>() != null)
        { EditorUtility.DisplayDialog("May xay", "May da la ban moi roi. Muon lap lai: bam muc 2 truoc.", "OK"); return; }
        string[] can = { "feedmill_base", "feedmill_gears", "feedmill_chute", "feedmill_crate_empty", "feedmill_crate_full", "feedmill_lamp_glow", "smoke_puff_01", "smoke_puff_02", "smoke_puff_03" };
        var thieu = can.Where(t => !File.Exists(ThuMuc + t + ".png")).ToList();
        if (thieu.Count > 0) { EditorUtility.DisplayDialog("May xay", "Thieu file trong " + ThuMuc + ":\n" + string.Join("\n", thieu), "OK"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("May xay",
            $"Thay may '{may.name}' bang bo art moi (nha dung yen, chi bo phan chuyen dong).\n\nSe backup scene + .meta truoc. Hong thi bam muc 2 (hoac Ctrl+Z).", "Lap", "Huy")) return;

        // ── Backup ──
        string gocDuAn = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(gocDuAn, "_Backup_FeedMill_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        var scene = may.gameObject.scene;
        if (!string.IsNullOrEmpty(scene.path)) File.Copy(Path.Combine(gocDuAn, scene.path), Path.Combine(bk, Path.GetFileName(scene.path)), true);
        foreach (var t in can)
        {
            string m = ThuMuc + t + ".png.meta";
            if (File.Exists(m)) File.Copy(m, Path.Combine(bk, t + ".png.meta"), true);
        }

        // ── Import ──
        Don(ThuMuc + "feedmill_base.png", new Vector2(0.5f, 0f));
        Strip(ThuMuc + "feedmill_gears.png", 8, new Vector2(0.5f, 0f));
        Strip(ThuMuc + "feedmill_chute.png", 6, new Vector2(0.5f, 0f));
        Don(ThuMuc + "feedmill_crate_empty.png", new Vector2(0.5f, 0.06f));
        Don(ThuMuc + "feedmill_crate_full.png", new Vector2(0.5f, 0.06f));
        Don(ThuMuc + "feedmill_lamp_glow.png", new Vector2(0.5f, 0.5f));
        for (int i = 1; i <= 3; i++) Don(ThuMuc + $"smoke_puff_0{i}.png", new Vector2(0.5f, 0.5f));

        Sprite baseSp = Mot("feedmill_base");
        Sprite[] gear = Nhieu("feedmill_gears"), chute = Nhieu("feedmill_chute");
        if (baseSp == null || gear.Length != 8 || chute.Length != 6)
        { EditorUtility.DisplayDialog("May xay", $"Cat sprite loi (base {(baseSp != null)}, gears {gear.Length}/8, chute {chute.Length}/6). Chua dong vao scene. Xem Console.", "OK"); return; }

        // ── Scene ──
        Undo.SetCurrentGroupName("Lap may thuc an moi");
        int nhom = Undo.GetCurrentGroup();
        var sr = may.GetComponent<SpriteRenderer>();
        var anim = may.GetComponent<Animator>();
        var fm = Undo.AddComponent<FeedMillLayered>(may.gameObject);
        fm.spriteCu = sr != null ? sr.sprite : null;
        fm.animatorCuBat = anim != null && anim.enabled;
        if (anim != null) { Undo.RecordObject(anim, "Lap may"); anim.enabled = false; }
        if (sr != null) { Undo.RecordObject(sr, "Lap may"); sr.sprite = baseSp; }
        int order = sr != null ? sr.sortingOrder : 0;

        fm.gears = TaoLop(may, "FM_Gears", gear[0], sr, order + 1, Vector3.zero, 1f);
        fm.chute = TaoLop(may, "FM_Chute", chute[0], sr, order + 2, Vector3.zero, 1f);
        fm.lampGlow = TaoLop(may, "FM_LampGlow", Mot("feedmill_lamp_glow"), sr, order + 3, VI_TRI_DEN, 0.6f);
        var goThung = new GameObject("FM_Crates");
        Undo.RegisterCreatedObjectUndo(goThung, "Lap may");
        goThung.transform.SetParent(may, false);
        fm.crateRoot = goThung.transform;
        fm.gearFrames = gear; fm.chuteFrames = chute;
        fm.crateEmpty = Mot("feedmill_crate_empty"); fm.crateFull = Mot("feedmill_crate_full");
        fm.smoke = TaoKhoi(may, sr, order + 5);
        if (fm.chute != null) fm.chute.enabled = false;

        EditorUtility.SetDirty(fm);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeTransform = may;
        Debug.Log($"[FeedMill] Da lap may moi cho '{may.name}'. Backup: {Path.GetFileName(bk)}. Bam Ctrl+S. " +
                  "Xem may chay lien tuc: tick FeedMillLayered > Xem Thu Luon Chay.");
        EditorUtility.DisplayDialog("May xay", "Xong. Bam Ctrl+S de luu scene roi Play.\n\n" +
            "Nha dung yen; banh rang, hat, khoi, thung chi chay manh khi may DANG XAY.\n" +
            "Muon xem ngay: chon may > FeedMillLayered > tick 'Xem Thu Luon Chay'.\n\nHong: muc 2 hoac Ctrl+Z. Backup: " + Path.GetFileName(bk), "OK");
    }

    // =====================================================================
    [MenuItem(Goc + "2. Tra lai may cu", false, 11)]
    private static void TraLai()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("May xay", "Thoat Play mode truoc.", "OK"); return; }
        var fm = Object.FindFirstObjectByType<FeedMillLayered>(FindObjectsInactive.Include);
        if (fm == null) { EditorUtility.DisplayDialog("May xay", "Scene khong co may ban moi.", "OK"); return; }
        Undo.SetCurrentGroupName("Tra lai may cu");
        int nhom = Undo.GetCurrentGroup();
        var go = fm.gameObject;
        var sr = go.GetComponent<SpriteRenderer>();
        var anim = go.GetComponent<Animator>();
        if (sr != null && fm.spriteCu != null) { Undo.RecordObject(sr, "Tra lai"); sr.sprite = fm.spriteCu; }
        if (anim != null) { Undo.RecordObject(anim, "Tra lai"); anim.enabled = fm.animatorCuBat; }
        foreach (var ten in new[] { "FM_Gears", "FM_Chute", "FM_LampGlow", "FM_Smoke", "FM_Crates" })
        {
            var c = go.transform.Find(ten);
            if (c != null) Undo.DestroyObjectImmediate(c.gameObject);
        }
        Undo.DestroyObjectImmediate(fm);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[FeedMill] Da tra lai may cu. Ctrl+S de luu.");
        EditorUtility.DisplayDialog("May xay", "Da tra lai may cu. Bam Ctrl+S.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    private static Transform TimMay()
    {
        var click = Object.FindFirstObjectByType<MillBuildingClick>(FindObjectsInactive.Include);
        return click != null ? click.transform : null;
    }

    private static SpriteRenderer TaoLop(Transform cha, string ten, Sprite sp, SpriteRenderer mau, int order, Vector3 pos, float scale)
    {
        var go = new GameObject(ten);
        Undo.RegisterCreatedObjectUndo(go, "Lap may");
        go.transform.SetParent(cha, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp;
        if (mau != null) { r.sharedMaterial = mau.sharedMaterial; r.sortingLayerID = mau.sortingLayerID; }
        r.sortingOrder = order;
        return r;
    }

    private static ParticleSystem TaoKhoi(Transform cha, SpriteRenderer mau, int order)
    {
        var go = new GameObject("FM_Smoke");
        Undo.RegisterCreatedObjectUndo(go, "Lap may");
        go.transform.SetParent(cha, false);
        go.transform.localPosition = VI_TRI_KHOI;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 5f; main.loop = true; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.26f, 0.38f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.40f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 1f, 1f, 1f);
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 24;

        var em = ps.emission; em.enabled = true; em.rateOverTime = 0.45f;

        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 10f; sh.radius = 0.04f; sh.rotation = new Vector3(-90f, 0f, 0f);

        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(0.10f); vel.y = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);

        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 1.7f)));

        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 1f, 1f), 0f), new GradientColorKey(new Color(0.88f, 0.88f, 0.9f), 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.12f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

        var tsa = ps.textureSheetAnimation; tsa.enabled = true; tsa.mode = ParticleSystemAnimationMode.Sprites;
        for (int i = 1; i <= 3; i++) { var s = Mot($"smoke_puff_0{i}"); if (s != null) tsa.AddSprite(s); }
        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
        tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 2.99f);
        tsa.cycleCount = 1;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.sharedMaterial = VatLieuKhoi();
        if (mau != null) pr.sortingLayerID = mau.sortingLayerID;
        pr.sortingOrder = order;
        return ps;
    }

    private static Material VatLieuKhoi()
    {
        string p = ThuMuc + "Mat_FeedMill_Smoke.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null) return m;
        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(ThuMuc + "smoke_puff_01.png");
        if (t != null) m.mainTexture = t;
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    // ── Import ──
    private static TextureImporter ChuanBi(string path, SpriteImportMode mode)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = mode;
        ti.spritePixelsPerUnit = PPU;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.alphaIsTransparency = true;
        ti.isReadable = false;
        return ti;
    }

    private static void Don(string path, Vector2 pivot)
    {
        var ti = ChuanBi(path, SpriteImportMode.Single);
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static void Strip(string path, int soFrame, Vector2 pivot)
    {
        var ti = ChuanBi(path, SpriteImportMode.Multiple);
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        int fw = w / soFrame;
        var f = new SpriteDataProviderFactories(); f.Init();
        var dp = f.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        var cu = dp.GetSpriteRects();
        string ten = Path.GetFileNameWithoutExtension(path);
        var ds = new SpriteRect[soFrame];
        for (int i = 0; i < soFrame; i++)
        {
            string n = ten + "_" + i;
            var co = cu.FirstOrDefault(r => r.name == n);
            ds[i] = new SpriteRect
            {
                name = n,
                rect = new Rect(i * fw, 0, fw, h),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = co != null ? co.spriteID : GUID.Generate(),
            };
        }
        dp.SetSpriteRects(ds);
        var nf = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nf != null) nf.SetNameFileIdPairs(ds.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
        ti.SaveAndReimport();
    }

    private static Sprite Mot(string ten) => AssetDatabase.LoadAllAssetsAtPath(ThuMuc + ten + ".png").OfType<Sprite>().FirstOrDefault();

    private static Sprite[] Nhieu(string ten)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ThuMuc + ten + ".png").OfType<Sprite>()
            .OrderBy(s => { int k = s.name.LastIndexOf('_'); return k >= 0 && int.TryParse(s.name.Substring(k + 1), out int n) ? n : 0; })
            .ToArray();
    }
}
