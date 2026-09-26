// ============================================================================
//  Edric Tools > Bep (Kitchen) > 1. Thay lua lo bang lua moi ve   (2026-09-25)
//                              > 2. Tra lai lua cu
//  Mo SampleScene (bep) roi bam. Tim trong scene cac Image lua cua lo: Oven_Fire, Fx_ComboFire,
//  Fx_LuaNho_0..2 -> gan UIFireFrames chay 6 frame lua moi (Restaurant_v3/stove_fire_sheet da cat san
//  thanh Restaurant_v3/_Opt/kitchen_fire_0..5.png, day lua thang hang).
//  KHONG dong vao anh sang (Fx_ComboGlow), bat/tat, scale, mau: KitchenJuiceFX van lo nhu cu.
//  Co Undo. Xong Ctrl+S.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class KitchenLuaMoiTool
{
    private const string GOC = "Edric Tools/Bep (Kitchen)/";
    private const string THU_MUC = "Assets/Art/Buildings/Restaurant_v3/_Opt/";
    private static readonly string[] TEN = { "Oven_Fire", "Fx_ComboFire", "Fx_LuaNho_0", "Fx_LuaNho_1", "Fx_LuaNho_2" };

    [MenuItem(GOC + "1. Thay lua lo bang lua moi ve", false, 1)]
    public static void Thay()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var frames = new List<Sprite>();
        for (int i = 0; i < 6; i++)
        {
            string p = THU_MUC + $"kitchen_fire_{i}.png";
            if (!File.Exists(p)) { Bao("Thieu " + p); return; }
            Nhap(p);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (sp != null) frames.Add(sp);
        }
        if (frames.Count == 0) { Bao("Khong nap duoc frame lua."); return; }

        var imgs = Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                         .Where(im => im != null && !EditorUtility.IsPersistent(im) && TEN.Contains(im.gameObject.name)).ToList();
        if (imgs.Count == 0) { Bao("Khong thay Oven_Fire / Fx_ComboFire trong scene dang mo. Mo SampleScene (bep) roi bam lai."); return; }

        Undo.SetCurrentGroupName("Thay lua lo");
        int nhom = Undo.GetCurrentGroup();
        foreach (var im in imgs)
        {
            var ff = im.GetComponent<UIFireFrames>();
            if (ff == null)
            {
                ff = Undo.AddComponent<UIFireFrames>(im.gameObject);
                ff.spriteCu = im.sprite;
                ff.giuTiLeCu = im.preserveAspect;
            }
            Undo.RecordObject(ff, "Thay lua lo");
            ff.frames = frames.ToArray();
            ff.fps = 10f;
            Undo.RecordObject(im, "Thay lua lo");
            im.sprite = frames[0];
            im.preserveAspect = true;
            EditorUtility.SetDirty(im);
            EditorSceneManager.MarkSceneDirty(im.gameObject.scene);
        }
        Undo.CollapseUndoOperations(nhom);
        Bao($"Da thay lua cho {imgs.Count} Image:\n{string.Join("\n", imgs.Select(i => i.gameObject.name))}\n\nAnh sang lo giu nguyen. Bam Ctrl+S roi Play vao bep.\nHong: muc 2 hoac Ctrl+Z.");
    }

    [MenuItem(GOC + "2. Tra lai lua cu", false, 2)]
    public static void TraLai()
    {
        if (Application.isPlaying) { Bao("Thoat Play mode truoc."); return; }
        var ds = Object.FindObjectsByType<UIFireFrames>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                       .Where(f => f != null && !EditorUtility.IsPersistent(f)).ToList();
        if (ds.Count == 0) { Bao("Scene khong co lua moi."); return; }
        Undo.SetCurrentGroupName("Tra lai lua cu");
        int nhom = Undo.GetCurrentGroup();
        foreach (var f in ds)
        {
            var im = f.GetComponent<Image>();
            if (im != null) { Undo.RecordObject(im, "Tra lai lua"); im.sprite = f.spriteCu; im.preserveAspect = f.giuTiLeCu; }
            var sc = f.gameObject.scene;
            Undo.DestroyObjectImmediate(f);
            EditorSceneManager.MarkSceneDirty(sc);
        }
        Undo.CollapseUndoOperations(nhom);
        Bao($"Da tra lai lua cu cho {ds.Count} Image. Bam Ctrl+S.");
    }

    private static void Nhap(string path)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 256;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = new Vector2(0.5f, 0.05f);
        ti.SetTextureSettings(st);
        ti.SaveAndReimport();
    }

    private static void Bao(string s) => EditorUtility.DisplayDialog("Lua bep", s, "OK");
}
