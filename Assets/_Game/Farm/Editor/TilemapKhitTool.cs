// ============================================================================
//  Tools > Farm Game > Map45 > Khit tile   (2026-09-25)
//  Vi sao co khe ho: tile co / dat ve tay co MEP MO (alpha 4-150 o 2-3 px ngoai cung). Hai tile canh
//  nhau cung mo mep -> cho giap noi khong du dac, lo mau nen phia sau -> thay duong ke ô vuong.
//  Cach chua (KHONG sua anh, KHONG doi luoi): phong moi tile to them vai % quanh tam cua no bang
//  Orientation = Custom cua TUNG Tilemap -> tile ke nhau DE LEN nhau 1 chut, mep mo bi phu kin.
//  Luoi / vi tri cong trinh / o dat khong doi. Co Undo. Xong Ctrl+S.
//   1. Khit tile co + dat (xoa khe ho)
//   2. Tra lai tile nhu cu
// ============================================================================
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapKhitTool
{
    private const string Goc = "Tools/Farm Game/Map45/Khit tile/";
    // Tilemap nen (mang lien). KHONG gom hoa / bui (Co_Grass), nuoc, cau cang, hang rao, da.
    private static readonly string[] TenTilemap =
        { "Tilemap_IsoGrass", "Móng", "Tilemap_IsoDirt", "Tilemap_IsoSand", "Tilemap_IsoStone", "Tilemap_IsoDirtPatch" };
    private const float HE_SO = 1.04f;   // to them 4%: tile 128px de len nhau ~2-3px moi mep

    [MenuItem(Goc + "1. Khit tile co + dat (xoa khe ho)", false, 40)]
    private static void Khit() => Ap(HE_SO, "Khit tile");

    [MenuItem(Goc + "2. Tra lai tile nhu cu", false, 41)]
    private static void TraLai() => Ap(1f, "Tra lai tile");

    private static void Ap(float k, string ten)
    {
        var ds = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t != null && !EditorUtility.IsPersistent(t) && TenTilemap.Contains(t.name)).ToList();
        if (ds.Count == 0) { EditorUtility.DisplayDialog("Khit tile", "Khong thay tilemap nen nao (mo SCN_Farm).", "OK"); return; }
        var sb = new StringBuilder();
        foreach (var tm in ds)
        {
            Undo.RecordObject(tm, ten);
            if (Mathf.Approximately(k, 1f))
            {
                tm.orientation = Tilemap.Orientation.XY;
            }
            else
            {
                tm.orientation = Tilemap.Orientation.Custom;
                tm.orientationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(k, k, 1f));
            }
            tm.RefreshAllTiles();
            EditorUtility.SetDirty(tm);
            EditorSceneManager.MarkSceneDirty(tm.gameObject.scene);
            sb.Append(tm.name).Append(", ");
        }
        Debug.Log($"[KhitTile] {ten} x{k:0.00}: {sb}. Ctrl+S de luu. Van thay khe: chon Tilemap > Inspector > Orientation Custom > Scale, tang 1.05-1.06.");
    }
}
