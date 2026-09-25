// ============================================================================
//  Tools > Farm Game > Map45 > Khit tile > 3. Nap lai tile nen moi   (2026-09-25)
//  Anh Sheet_IsoGrass45 / Sand45 / Dirt45 da doi tren dia nhung man hinh van hien hinh cu
//  (atlas Atlas_GroundTiles con giu ban dong goi cu). Muc nay:
//    1. Import lai BAT BUOC 3 anh nen
//    2. Dong goi lai toan bo Sprite Atlas
//    3. Refresh moi Tilemap trong scene dang mo
//    4. Ghi Console: anh tren dia co phai ban moi khong, sprite dang dung texture nao
//  Khong sua scene, khong sua anh. Bam luc KHONG Play.
// ============================================================================
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

public static class TilemapNapLaiTool
{
    private static readonly string[] Anh =
    {
        "Assets/maptitle/Map45Iso/Sheet_IsoGrass45.png",
        "Assets/maptitle/Map45Iso/Sheet_IsoSand45.png",
        "Assets/maptitle/Map45Iso/Sheet_IsoDirt45.png",
    };

    [MenuItem("Tools/Farm Game/Map45/Khit tile/3. Nap lai tile nen moi (reimport + atlas)", false, 42)]
    private static void NapLai()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Nap lai tile", "Thoat Play mode truoc roi bam lai.", "OK"); return; }
        var sb = new StringBuilder("[NapLaiTile]\n");

        // 1. Import lai bat buoc
        foreach (var p in Anh)
        {
            if (!File.Exists(p)) { sb.AppendLine("  THIEU file: " + p); continue; }
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var kich = new FileInfo(p).Length;
            var sp = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().Count();
            sb.AppendLine($"  {Path.GetFileName(p)}: {kich / 1024} KB tren dia, {sp} sprite");
        }

        // 2. Dong goi lai atlas
        var dsAtlas = AssetDatabase.FindAssets("t:SpriteAtlas")
            .Select(g => AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null).ToArray();
        try
        {
            SpriteAtlasUtility.PackAtlases(dsAtlas, EditorUserBuildSettings.activeBuildTarget, false);
            sb.AppendLine($"  Da dong goi lai {dsAtlas.Length} atlas (mode Sprite Packer = {EditorSettings.spritePackerMode}).");
        }
        catch (System.Exception e) { sb.AppendLine("  Loi dong goi atlas: " + e.Message); }

        // 3. Refresh tilemap
        int n = 0;
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tm == null || EditorUtility.IsPersistent(tm)) continue;
            tm.RefreshAllTiles(); n++;
        }
        sb.AppendLine($"  Refresh {n} tilemap.");

        // 4. Chan doan: atlas nao dang chua sprite cat
        var sand = AssetDatabase.LoadAllAssetsAtPath(Anh[1]).OfType<Sprite>().FirstOrDefault();
        if (sand != null)
            foreach (var a in dsAtlas)
                if (a.CanBindTo(sand)) sb.AppendLine($"  Sprite cat nam trong atlas: {a.name}");

        AssetDatabase.Refresh();
        SceneView.RepaintAll();
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Nap lai tile", "Xong. Bam Play de xem.\nChi tiet trong Console ([NapLaiTile]).", "OK");
    }
}
