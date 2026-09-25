// ============================================================================
//  Tools > Farm Game > Nong Dan  (2026-09-24)
//   1. Cat 3 sheet + tao FarmerConfig   (backup .meta truoc khi cat -> muc 4 hoan tac duoc)
//   2. Tao Farmer_Crew trong Hierarchy  (tuy chon: de chinh tham so bang tay; khong tao thi luc Play tu sinh)
//   3. Kiem tra (chi doc)
//   4. Hoan tac cat sprite (tra .meta tu ban backup moi nhat)
//  Sheet (Assets/Art/Characters/Farmer):
//   walk  3 cot x 4 hang o 450 (DOWN / LEFT / RIGHT / UP), PPU 100,  pivot chan (0.5, 40/450)
//   hoe   4 cot x 2 hang o 500,                               PPU 111.1, pivot chan (0.5, 40/500)
//   water 4 cot x 2 hang o 500,                               PPU 111.1, pivot chan (0.5, 40/500)
//  PPU 111.1 cho o 500 -> nhan vat 3 sheet CUNG CO tren man hinh.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class FarmerSetupTool
{
    private const string Goc = "Tools/Farm Game/Nong Dan/";
    private const string ThuMuc = "Assets/Art/Characters/Farmer/";
    private const string PathConfig = "Assets/_Game/Resources/FarmerConfig.asset";
    private const string TienToBackup = "_Backup_NongDan_";

    private class Sheet
    {
        public string file, tien;
        public int cot, hang, o;
        public float ppu, chanPx;
    }

    private static readonly Sheet[] Sheets =
    {
        new Sheet { file = "farmer_walk_spritesheet.png",  tien = "walk",  cot = 3, hang = 4, o = 450, ppu = 100f,   chanPx = 40f },
        new Sheet { file = "farmer_hoe_spritesheet.png",   tien = "hoe",   cot = 4, hang = 2, o = 500, ppu = 111.1f, chanPx = 40f },
        new Sheet { file = "farmer_water_spritesheet.png", tien = "water", cot = 4, hang = 2, o = 500, ppu = 111.1f, chanPx = 40f },
    };

    // =====================================================================
    [MenuItem(Goc + "1. Cat 3 sheet + tao FarmerConfig", false, 10)]
    private static void CatVaTaoConfig()
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, TienToBackup + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var sb = new StringBuilder();

        // Backup .meta
        foreach (var s in Sheets)
        {
            string meta = Path.Combine(goc, ThuMuc + s.file + ".meta");
            if (!File.Exists(meta)) { EditorUtility.DisplayDialog("Nong Dan", "Khong thay " + ThuMuc + s.file, "OK"); return; }
            string dich = Path.Combine(bk, ThuMuc + s.file + ".meta");
            Directory.CreateDirectory(Path.GetDirectoryName(dich));
            File.Copy(meta, dich, true);
        }
        sb.AppendLine("Backup .meta -> " + Path.GetFileName(bk));

        foreach (var s in Sheets)
        {
            string loi;
            int n = CatSheet(s, out loi);
            sb.AppendLine(loi == null ? $"  {s.file}: {n} sprite (PPU {s.ppu}, pivot chan)" : $"  {s.file}: LOI {loi}");
        }

        // Config
        var cfg = AssetDatabase.LoadAssetAtPath<FarmerConfig>(PathConfig);
        if (cfg == null)
        {
            Directory.CreateDirectory(Path.Combine(goc, Path.GetDirectoryName(PathConfig)));
            cfg = ScriptableObject.CreateInstance<FarmerConfig>();
            AssetDatabase.CreateAsset(cfg, PathConfig);
        }
        Undo.RecordObject(cfg, "Gan frame nong dan");
        cfg.walkFrames = LaySprites(ThuMuc + Sheets[0].file, "walk_");
        cfg.hoeFrames = LaySprites(ThuMuc + Sheets[1].file, "hoe_");
        cfg.waterFrames = LaySprites(ThuMuc + Sheets[2].file, "water_");
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();
        sb.AppendLine($"FarmerConfig: walk {cfg.walkFrames.Length}/12, hoe {cfg.hoeFrames.Length}/8, water {cfg.waterFrames.Length}/8");
        sb.AppendLine("Luc Play: gieo hat -> ong nong dan tu hien ra. Muon chinh tham so: muc 2.");
        Selection.activeObject = cfg;
        Debug.Log("[NongDan] " + sb);
        EditorUtility.DisplayDialog("Nong Dan", sb.ToString(), "OK");
    }

    private static int CatSheet(Sheet s, out string loi)
    {
        loi = null;
        string path = ThuMuc + s.file;
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { loi = "khong phai texture"; return 0; }

        int w = s.cot * s.o, h = s.hang * s.o;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = s.ppu;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.sRGBTexture = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.textureCompression = TextureImporterCompression.Compressed;   // nhe bo nho (Unity tu lui ve RGBA neu kich thuoc khong chia 4)
        imp.maxTextureSize = 2048;
        var ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        ts.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(ts);
        imp.SaveAndReimport();

        imp = AssetImporter.GetAtPath(path) as TextureImporter;
        var f = new SpriteDataProviderFactories();
        f.Init();
        var dp = f.GetSpriteEditorDataProviderFromObject(imp);
        if (dp == null) { loi = "thieu package 2D Sprite"; return 0; }
        dp.InitSpriteEditorDataProvider();

        var idCu = new Dictionary<string, GUID>();
        var cu = dp.GetSpriteRects();
        if (cu != null) foreach (var r in cu) if (r != null && !idCu.ContainsKey(r.name)) idCu[r.name] = r.spriteID;

        var moi = new List<SpriteRect>();
        var cap = new List<SpriteNameFileIdPair>();
        int k = 0;
        for (int hang = 0; hang < s.hang; hang++)
            for (int cot = 0; cot < s.cot; cot++)
            {
                k++;
                string ten = s.tien + "_" + k.ToString("00");
                GUID id;
                if (!idCu.TryGetValue(ten, out id) || id.Empty()) id = GUID.Generate();
                moi.Add(new SpriteRect
                {
                    name = ten,
                    spriteID = id,
                    rect = new Rect(cot * s.o, h - (hang + 1) * s.o, s.o, s.o),   // hang 0 = tren cung anh
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, s.chanPx / s.o),
                    border = Vector4.zero,
                });
                cap.Add(new SpriteNameFileIdPair(ten, id));
            }
        dp.SetSpriteRects(moi.ToArray());
        var np = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (np != null) np.SetNameFileIdPairs(cap);
        dp.Apply();
        imp.SaveAndReimport();
        return moi.Count;
    }

    private static Sprite[] LaySprites(string path, string tien)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Where(x => x.name.StartsWith(tien))
            .OrderBy(x => x.name, System.StringComparer.Ordinal)
            .ToArray();
    }

    // =====================================================================
    [MenuItem(Goc + "2. Tao Farmer_Crew trong Hierarchy", false, 11)]
    private static void TaoTrongHierarchy()
    {
        var co = Object.FindFirstObjectByType<FarmerCrew>(FindObjectsInactive.Include);
        if (co != null) { Selection.activeGameObject = co.gameObject; EditorUtility.DisplayDialog("Nong Dan", "Scene da co Farmer_Crew (da chon cho Sep).", "OK"); return; }
        if (Object.FindFirstObjectByType<PlotController>(FindObjectsInactive.Include) == null)
        {
            EditorUtility.DisplayDialog("Nong Dan", "Scene nay khong co o dat. Hay mo SCN_Farm.", "OK");
            return;
        }
        var cfg = AssetDatabase.LoadAssetAtPath<FarmerConfig>(PathConfig);
        var go = new GameObject("Farmer_Crew");
        Undo.RegisterCreatedObjectUndo(go, "Tao Farmer_Crew");
        var crew = go.AddComponent<FarmerCrew>();
        var so = new SerializedObject(crew);
        so.FindProperty("config").objectReferenceValue = cfg;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log("[NongDan] Da tao Farmer_Crew" + (cfg == null ? " (CHUA co FarmerConfig: chay muc 1 truoc)" : "") + ". Ctrl+S de luu scene.");
    }

    // =====================================================================
    [MenuItem(Goc + "3. Kiem tra (chi doc)", false, 12)]
    private static void KiemTra()
    {
        var sb = new StringBuilder();
        var cfg = AssetDatabase.LoadAssetAtPath<FarmerConfig>(PathConfig);
        if (cfg == null) sb.AppendLine("CHUA co FarmerConfig -> chay muc 1.");
        else sb.AppendLine($"FarmerConfig: walk {Dem(cfg.walkFrames)}/12, hoe {Dem(cfg.hoeFrames)}/8, water {Dem(cfg.waterFrames)}/8, cao {cfg.chieuCaoNhinThay}, toc do {cfg.tocDoDi}/s, {cfg.soODatMoiNguoi} o/ong");
        foreach (var s in Sheets)
        {
            var imp = AssetImporter.GetAtPath(ThuMuc + s.file) as TextureImporter;
            int n = AssetDatabase.LoadAllAssetsAtPath(ThuMuc + s.file).OfType<Sprite>().Count();
            sb.AppendLine($"  {s.file}: {n} sprite, PPU {(imp != null ? imp.spritePixelsPerUnit : 0)}");
        }
        int ruong = 0, chau = 0, trong = 0, trongChau = 0;
        foreach (var p in Object.FindObjectsByType<PlotController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (p.Category == PlotCategory.Flower) { chau++; if (Application.isPlaying && p.IsPlanted) trongChau++; }
            else { ruong++; if (Application.isPlaying && p.IsPlanted) trong++; }
        }
        sb.AppendLine($"Scene: {ruong} o ruong, {chau} chau hoa.");
        if (Application.isPlaying)
        {
            int per = cfg != null ? Mathf.Max(1, cfg.soODatMoiNguoi) : 4;
            sb.AppendLine($"Dang trong: {trong} o ruong -> {(trong + per - 1) / per} ong; {trongChau} chau -> {(trongChau > 0 ? 1 : 0)} ong tuoi chau.");
        }
        else sb.AppendLine("(Bam Play roi chay lai muc nay de xem so ong theo o dang trong.)");
        sb.AppendLine("Farmer_Crew trong Hierarchy: " + (Object.FindFirstObjectByType<FarmerCrew>(FindObjectsInactive.Include) != null ? "co" : "chua (luc Play tu sinh)"));
        Debug.Log("[NongDan] " + sb);
        EditorUtility.DisplayDialog("Nong Dan - Kiem tra", sb.ToString(), "OK");
    }

    private static int Dem(Sprite[] a) { int n = 0; if (a != null) foreach (var x in a) if (x != null) n++; return n; }

    // =====================================================================
    [MenuItem(Goc + "4. Hoan tac cat sprite (tu backup moi nhat)", false, 30)]
    private static void HoanTac()
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        var bks = Directory.GetDirectories(goc, TienToBackup + "*").OrderBy(x => x).ToArray();
        if (bks.Length == 0) { EditorUtility.DisplayDialog("Nong Dan", "Chua co ban backup nao.", "OK"); return; }
        string bk = bks[bks.Length - 1];
        if (!EditorUtility.DisplayDialog("Nong Dan", "Tra .meta 3 sheet ve ban " + Path.GetFileName(bk) + "?", "Hoan tac", "Huy")) return;
        int n = 0;
        foreach (var s in Sheets)
        {
            string tu = Path.Combine(bk, ThuMuc + s.file + ".meta");
            if (!File.Exists(tu)) continue;
            File.Copy(tu, Path.Combine(goc, ThuMuc + s.file + ".meta"), true);
            AssetDatabase.ImportAsset(ThuMuc + s.file, ImportAssetOptions.ForceUpdate);
            n++;
        }
        Debug.Log($"[NongDan] Da hoan tac {n} .meta tu {Path.GetFileName(bk)}. FarmerConfig giu nguyen (xoa tay neu muon).");
    }
}
