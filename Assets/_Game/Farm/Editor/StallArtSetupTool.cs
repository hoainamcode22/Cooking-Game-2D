// ============================================================================
//  Tools > Farm Game > Quay Hang (Stall)   (2026-09-25)
//  1. Lap quay moi: thay hinh quay cu (Assetsgame/Nhà/quayhang.png) cua object "Stall_WorldObject" bang
//     art FarmStand (ban da xoa nen trang: FarmStand/_Opt/stand_base_opt.png), GIU vi tri / scale / collider /
//     sorting / script mo popup. Them StallLayered + con ST_Produce: nong san (icon CropData cua game) nay len
//     khoi sot giong cho. Backup scene + .meta truoc. Co Undo. Xong Ctrl+S.
//  2. Tra lai quay cu.
//  So lieu (do tu anh, khong uoc luong):
//    - Quay cu: sprite 398x373 PPU 100 pivot giua; phan co hinh rong 311 px = 3.11 local, day cach tam -1.855 local.
//    - Quay moi 820x820: phan co hinh rong 697 px, day o y=796 (tu tren), tam ngang x=409.5.
//      -> PPU 224 (697/3.11) de CUNG BE NGANG, pivot (0.506, 0.536) de CUNG CHAN va cung tam ngang.
// ============================================================================
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StallArtSetupTool
{
    private const string GOC = "Tools/Farm Game/Quay Hang (Stall)/";
    private const string ANH = "Assets/Art/Buildings/FarmStand/_Opt/stand_base_opt.png";
    private const float PPU = 224f;
    private static readonly Vector2 PIVOT = new Vector2(0.506f, 0.536f);
    // Thu tu khop StallLayered.choNay (sot bi do, ca rot, bap cai, ca chua, ngo, khoai tay tren art)
    private static readonly string[] CROP = { "pumpkin", "carot", "bapcai", "cachua", "ngo", "khoaitay" };

    [MenuItem(GOC + "1. Lap quay moi (nong san nay)", false, 1)]
    private static void Lap()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var sw = Object.FindFirstObjectByType<StallWorldObject>(FindObjectsInactive.Include);
        if (sw == null) { Bao("Khong thay Stall_WorldObject (mo SCN_Farm roi bam lai)."); return; }
        var go = sw.gameObject;
        if (go.GetComponent<StallLayered>() != null) { Bao("Quay da la ban moi. Muon lap lai: bam muc 2 truoc."); return; }
        if (!File.Exists(ANH)) { Bao("Thieu file " + ANH); return; }
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) { Bao("Stall_WorldObject khong co SpriteRenderer."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Quay hang", "Thay hinh quay cu bang art FarmStand moi + nong san nay.\nGiu vi tri, collider, popup.\n\nBackup scene truoc. Hong: muc 2 hoac Ctrl+Z.", "Lap", "Huy")) return;

        string bk = Backup(go.scene);

        // Import anh: sprite don, PPU + pivot da tinh, nen CompressedHQ, toi da 1024 (anh 820)
        var ti = (TextureImporter)AssetImporter.GetAtPath(ANH);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = PPU;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.isReadable = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.maxTextureSize = 1024;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.crunchedCompression = false;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = PIVOT;
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
        var baseSp = AssetDatabase.LoadAssetAtPath<Sprite>(ANH);
        if (baseSp == null) { Bao("Khong nap duoc sprite " + ANH + ". Chua dong vao scene."); return; }

        // Icon nong san cua game
        var icons = new Sprite[CROP.Length];
        foreach (var g in AssetDatabase.FindAssets("t:CropData"))
        {
            var cd = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(g));
            if (cd == null) continue;
            int i = System.Array.IndexOf(CROP, cd.cropId);
            if (i < 0 || icons[i] != null) continue;
            icons[i] = cd.harvestIcon != null ? cd.harvestIcon : cd.icon;
        }
        int co = icons.Count(s => s != null);
        if (co == 0) { Bao("Khong tim thay icon nong san (CropData). Chua dong vao scene."); return; }

        Undo.SetCurrentGroupName("Lap quay moi");
        int nhom = Undo.GetCurrentGroup();
        var sl = Undo.AddComponent<StallLayered>(go);
        sl.spriteCu = sr.sprite;
        Undo.RecordObject(sr, "Lap quay");
        sr.sprite = baseSp;

        var root = new GameObject("ST_Produce");
        Undo.RegisterCreatedObjectUndo(root, "Lap quay");
        root.transform.SetParent(go.transform, false);
        sl.produceRoot = root.transform;
        // Giu cap (icon, diem) khop nhau; mon thieu icon thi bo ca diem cua no
        var sp = new System.Collections.Generic.List<Sprite>();
        var diem = new System.Collections.Generic.List<Vector2>();
        for (int i = 0; i < CROP.Length; i++)
            if (icons[i] != null && i < sl.choNay.Length) { sp.Add(icons[i]); diem.Add(sl.choNay[i]); }
        sl.produceSprites = sp.ToArray();
        sl.choNay = diem.ToArray();

        EditorUtility.SetDirty(sl);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        Debug.Log($"[QuayHang] Da lap quay moi ({co}/6 icon nong san). Backup: {Path.GetFileName(bk)}. Ctrl+S de luu.");
        Bao($"Xong ({co}/6 icon nong san). Bam Ctrl+S roi Play.\n\nChinh tay: Stall_WorldObject > StallLayered (nhip nay, do cao, co icon, diem nay).\nHong: muc 2 hoac Ctrl+Z. Backup: " + Path.GetFileName(bk));
    }

    [MenuItem(GOC + "2. Tra lai quay cu", false, 2)]
    private static void TraLai()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var sl = Object.FindFirstObjectByType<StallLayered>(FindObjectsInactive.Include);
        if (sl == null) { Bao("Scene khong co quay ban moi."); return; }
        Undo.SetCurrentGroupName("Tra lai quay cu");
        int nhom = Undo.GetCurrentGroup();
        var go = sl.gameObject;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sl.spriteCu != null) { Undo.RecordObject(sr, "Tra lai quay"); sr.sprite = sl.spriteCu; }
        var c = go.transform.Find("ST_Produce");
        if (c != null) Undo.DestroyObjectImmediate(c.gameObject);
        Undo.DestroyObjectImmediate(sl);
        Undo.CollapseUndoOperations(nhom);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Bao("Da tra lai quay cu. Bam Ctrl+S.");
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Quay hang", s, "OK");

    private static string Backup(UnityEngine.SceneManagement.Scene scene)
    {
        string goc = Path.GetDirectoryName(Application.dataPath);
        string bk = Path.Combine(goc, "_Backup_Stall_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(bk);
        if (!string.IsNullOrEmpty(scene.path)) File.Copy(Path.Combine(goc, scene.path), Path.Combine(bk, Path.GetFileName(scene.path)), true);
        if (File.Exists(ANH + ".meta")) File.Copy(ANH + ".meta", Path.Combine(bk, "stand_base_opt.png.meta"), true);
        return bk;
    }
}
