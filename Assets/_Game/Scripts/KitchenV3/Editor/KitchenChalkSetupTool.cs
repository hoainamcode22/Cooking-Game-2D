// ============================================================================
//  Tools > Kitchen V3 > 17. Bang phan don khach vao Hierarchy (2026-09-25)
//  Dung san trong Edit mode (mo SampleScene) cac object cua bang phan "don khach du lich":
//    Kitchen_UI_v3/Chalkboard/DonKhach_NoiDung/{Img_MonKhach, Txt_TenMonKhach, Txt_ThuTuKhach, Txt_MuiTrai, Txt_MuiPhai}
//  + gan component KitchenChalkDonKhach, an Txt_Chalk (3 dong mon hom nay cu) -> Edit mode nhin giong Play.
//  Sep keo / doi co / doi mau tuy y roi Ctrl+S. Luc Play code DUNG LAI dung cac object nay, chi thay icon + chu.
//  Co Undo (Ctrl+Z). Bam lai nhieu lan khong tao trung.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KitchenChalkSetupTool
{
    [MenuItem("Tools/Kitchen V3/17. Bang phan don khach vao Hierarchy (chinh tay)", false, 117)]
    private static void Chay()
    {
        Transform chalk = null;
        foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Chalkboard" && t.parent != null && t.parent.name == "Kitchen_UI_v3") { chalk = t; break; }
        if (chalk == null)
        {
            EditorUtility.DisplayDialog("Bang phan", "Khong thay Kitchen_UI_v3/Chalkboard. Hay mo scene bep (SampleScene) roi bam lai.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(chalk.gameObject, "Bang phan don khach");
        var dk = chalk.GetComponent<KitchenChalkDonKhach>();
        if (dk == null) dk = Undo.AddComponent<KitchenChalkDonKhach>(chalk.gameObject);
        bool daCo = chalk.Find("DonKhach_NoiDung") != null;
        dk.DungKhung();
        if (!daCo && dk.NoiDung != null) Undo.RegisterCreatedObjectUndo(dk.NoiDung.gameObject, "Bang phan don khach");

        // Noi dung mau de canh bo cuc (luc Play tu thay bang don that)
        Sprite mau = null; string ten = "Mushroom Salad";
        foreach (var g in AssetDatabase.FindAssets("t:DishData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<DishData>(AssetDatabase.GUIDToAssetPath(g));
            if (d != null && d.dishSprite != null) { mau = d.dishSprite; ten = d.dishName; break; }
        }
        dk.DienMau(mau, ten, "1/3");

        EditorSceneManager.MarkSceneDirty(chalk.gameObject.scene);
        Selection.activeTransform = dk.NoiDung != null ? dk.NoiDung : chalk;
        Debug.Log("[BangPhan] Da dung bang phan don khach trong Hierarchy (Chalkboard/DonKhach_NoiDung). Chinh tay roi Ctrl+S.");
    }
}
