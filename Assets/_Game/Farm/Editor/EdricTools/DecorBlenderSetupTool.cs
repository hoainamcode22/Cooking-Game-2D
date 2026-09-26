// ============================================================================
//  Edric Tools > Decor (Blender) > 1. Tao prefab cay an qua + hai dang   (2026-09-26)
//  Hinh render tu Blender (Claude outputs/blender/farm_assets_v1.blend) nam o Assets/Art/Decor_Blender/.
//  Tao prefab o Assets/_Game/Farm/Prefabs/Decor_Blender/ de Sep keo vao map:
//   - Prefab_FruitTree_Apple / Orange / Lemon / Palm : than + tan (lac), trai roi, chat bang riu nhu cay thong
//   - Prefab_Lighthouse : hai dang + tia sang quay 360 do + quang den
//  Chay lai = tao lai prefab (ghi de). Khong dong vao scene.
// ============================================================================
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DecorBlenderSetupTool
{
    private const string GOC = "Edric Tools/Decor (Blender)/";
    private const string ART = "Assets/Art/Decor_Blender/";
    private const string PF = "Assets/_Game/Farm/Prefabs/Decor_Blender/";
    private const string PINE = "Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Art/Environment/Pinetree/Prefab_Pinetree.prefab";

    private class Cay
    {
        public string ten, key; public Vector2 day, ngon; public Vector4 vungQua; public Vector3 vungDat; public float nhipMin, nhipMax;
    }
    // so lieu do tu render 512x512 (px, goc tren-trai)
    private static readonly Cay[] CAY =
    {
        new Cay { ten = "Apple",  key = "apple",  day = new Vector2(256, 404.52f), ngon = new Vector2(256, 286.28f), vungQua = new Vector4(-0.9f, 0.9f, 1.4f, 2.6f), vungDat = new Vector3(0.8f, -0.12f, 0.12f), nhipMin = 8, nhipMax = 18 },
        new Cay { ten = "Orange", key = "orange", day = new Vector2(256, 404.52f), ngon = new Vector2(256, 286.28f), vungQua = new Vector4(-0.9f, 0.9f, 1.4f, 2.6f), vungDat = new Vector3(0.8f, -0.12f, 0.12f), nhipMin = 8, nhipMax = 18 },
        new Cay { ten = "Lemon",  key = "lemon",  day = new Vector2(256, 404.52f), ngon = new Vector2(256, 286.28f), vungQua = new Vector4(-0.9f, 0.9f, 1.4f, 2.6f), vungDat = new Vector3(0.8f, -0.12f, 0.12f), nhipMin = 8, nhipMax = 18 },
        new Cay { ten = "Palm",   key = "palm",   day = new Vector2(231.86f, 398.37f), ngon = new Vector2(274.89f, 132.78f), vungQua = new Vector4(0.25f, 0.75f, 2.1f, 2.4f), vungDat = new Vector3(0.9f, -0.12f, 0.12f), nhipMin = 14, nhipMax = 28 },
    };
    private const float SCALE_CAY = 125f;
    private const float SCALE_HD = 110f;
    private static readonly Vector2 HD_DAY = new Vector2(384f, 665.72f), HD_DEN = new Vector2(384f, 152.85f);

    [MenuItem(GOC + "1. Tao prefab cay an qua + hai dang", false, 1)]
    public static void Tao()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var thieu = new System.Collections.Generic.List<string>();
        foreach (var c in CAY)
            foreach (var f in new[] { $"tree_{c.key}_trunk", $"tree_{c.key}_canopy", $"fruit_{c.key}" })
                if (!File.Exists(ART + f + ".png")) thieu.Add(f);
        foreach (var f in new[] { "lighthouse", "lighthouse_beam", "lighthouse_glow" }) if (!File.Exists(ART + f + ".png")) thieu.Add(f);
        if (thieu.Count > 0) { Bao("Thieu hinh:\n" + string.Join("\n", thieu)); return; }
        Directory.CreateDirectory(PF);

        var pine = AssetDatabase.LoadAssetAtPath<GameObject>(PINE);
        var pineSr = pine != null ? pine.GetComponentInChildren<SpriteRenderer>() : null;
        Material matLit = pineSr != null ? pineSr.sharedMaterial : null;
        Material matUnlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat") ?? matLit;
        int lop = SortingLayer.NameToID("Objects");
        int lopTruoc = SortingLayer.NameToID("Foreground");

        var bao = new System.Collections.Generic.List<string>();
        foreach (var c in CAY)
        {
            Vector2 piv = new Vector2(c.day.x / 512f, 1f - c.day.y / 512f);
            Nhap(ART + $"tree_{c.key}_trunk.png", 100f, piv, 512);
            Nhap(ART + $"tree_{c.key}_canopy.png", 100f, piv, 512);
            Nhap(ART + $"fruit_{c.key}.png", 100f, new Vector2(0.5f, 0.5f), 128);

            var root = new GameObject("Prefab_FruitTree_" + c.ten);
            root.transform.localScale = Vector3.one * SCALE_CAY;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + $"tree_{c.key}_trunk.png");
            sr.sortingLayerID = lop; sr.sortingOrder = 0; sr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (matLit != null) sr.sharedMaterial = matLit;

            var tan = new GameObject("Tan");
            tan.transform.SetParent(root.transform, false);
            var tsr = tan.AddComponent<SpriteRenderer>();
            tsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + $"tree_{c.key}_canopy.png");
            tsr.sortingLayerID = lop; tsr.sortingOrder = 1; tsr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (matLit != null) tsr.sharedMaterial = matLit;

            var fx = root.AddComponent<FruitTreeFX>();
            fx.tan = tan.transform;
            fx.diemXoay = new Vector2((c.ngon.x - c.day.x) / 100f, (c.day.y - c.ngon.y) / 100f);
            fx.quaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + $"fruit_{c.key}.png");
            fx.vungQua = c.vungQua; fx.vungDat = c.vungDat;
            fx.nhipRoi = new Vector2(c.nhipMin, c.nhipMax);
            if (c.key == "palm") { fx.bienDoXoay = 1.8f; fx.tocDoXoay = 0.9f; fx.coQua = 0.6f; }

            string path = PF + root.name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            bao.Add(path);
        }

        // ---- Hai dang
        Nhap(ART + "lighthouse.png", 100f, new Vector2(HD_DAY.x / 768f, 1f - HD_DAY.y / 768f), 1024);
        Nhap(ART + "lighthouse_beam.png", 100f, new Vector2(0f, 0.5f), 512);
        Nhap(ART + "lighthouse_glow.png", 100f, new Vector2(0.5f, 0.5f), 128);
        {
            var root = new GameObject("Prefab_Lighthouse");
            root.transform.localScale = Vector3.one * SCALE_HD;
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + "lighthouse.png");
            sr.sortingLayerID = lop; sr.sortingOrder = 0; sr.spriteSortPoint = SpriteSortPoint.Pivot;
            if (matLit != null) sr.sharedMaterial = matLit;
            Vector3 den = new Vector3((HD_DEN.x - HD_DAY.x) / 100f, (HD_DAY.y - HD_DEN.y) / 100f, 0f);

            var piv = new GameObject("BeamPivot");
            piv.transform.SetParent(root.transform, false);
            piv.transform.localPosition = den;
            piv.transform.localScale = new Vector3(1f, 0.5f, 1f);        // goc iso: quet thanh elip
            var tia = new GameObject("Beam");
            tia.transform.SetParent(piv.transform, false);
            tia.transform.localScale = new Vector3(1.95f, 1.4f, 1f);
            var tsr = tia.AddComponent<SpriteRenderer>();
            tsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + "lighthouse_beam.png");
            tsr.sortingLayerID = lopTruoc; tsr.sortingOrder = 5;
            if (matUnlit != null) tsr.sharedMaterial = matUnlit;

            var q = new GameObject("Glow");
            q.transform.SetParent(root.transform, false);
            q.transform.localPosition = den;
            q.transform.localScale = Vector3.one * 1.3f;
            var qsr = q.AddComponent<SpriteRenderer>();
            qsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART + "lighthouse_glow.png");
            qsr.sortingLayerID = lopTruoc; qsr.sortingOrder = 6;
            if (matUnlit != null) qsr.sharedMaterial = matUnlit;

            var lb = root.AddComponent<LighthouseBeam>();
            lb.xoay = tia.transform; lb.tia = tsr; lb.quang = qsr;
            string path = PF + "Prefab_Lighthouse.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            bao.Add(path);
        }

        // ---- Cho riu chat duoc cay an qua (tien to ten)
        string them = "";
        var cfg = Resources.Load<WorldClearConfig>("WorldClearConfig");
        if (cfg != null && cfg.loai != null)
        {
            var riu = cfg.loai.FirstOrDefault(l => l != null && l.kind == WorldClearKind.Riu);
            if (riu != null && (riu.tienToTen == null || !riu.tienToTen.Contains("prefab_fruittree")))
            {
                riu.tienToTen = (riu.tienToTen ?? new string[0]).Concat(new[] { "prefab_fruittree" }).ToArray();
                EditorUtility.SetDirty(cfg);
                them = "\nDa them 'prefab_fruittree' vao danh sach cay chat bang riu.";
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[DecorBlender] " + string.Join(" | ", bao));
        Bao("Xong " + bao.Count + " prefab o " + PF + them + "\n\nKeo prefab vao map (Hierarchy). Cay an qua chat bang riu nhu cay thong.");
    }

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
        ti.textureCompression = TextureImporterCompression.Compressed;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        st.spriteMeshType = SpriteMeshType.Tight;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Decor (Blender)", s, "OK");
}
