// ============================================================================
//  Tools > Farm Game > Cho (Market) > 1 / 2   (2026-09-25)
//  1. Bang cho gon 5 the / hang, nho hon, KHONG loi ra mep man hinh.
//     Sua THANG trong Hierarchy (SCN_Farm: Canvas_MarketPopup/Panel_Dim/Popup_Board) de Sep keo chinh tiep:
//       - Popup_Board 1920x900 scale 0.95  ->  1580x900 scale 0.90
//       - Content_Listings (GridLayoutGroup) 6 cot 254x264  ->  5 cot 250x260, cach 16
//       - MarketBoardUI.tiLeToiDaBang 0.88 -> 1 (bang dung dung co Sep dat; chi tu co khi man hinh nho hon)
//     Luu gia tri cu vao EditorPrefs -> muc 2 tra lai. Co Undo. Xong Ctrl+S.
//  2. Tra lai bang cho nhu cu.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MarketBoardGonTool
{
    private const string Goc = "Tools/Farm Game/Cho (Market)/";
    private const string KhoaCu = "MarketBoardGonTool_Cu";

    [System.Serializable] private class Cu { public Vector2 size; public Vector3 scale; public Vector2 cell, spacing; public int cot; public float tiLe; }

    [MenuItem(Goc + "1. Bang cho gon 5 the moi hang (Hierarchy)", false, 10)]
    private static void Gon()
    {
        if (!Tim(out var bang, out var grid, out var ui)) return;
        var cu = new Cu
        {
            size = bang.sizeDelta, scale = bang.localScale,
            cell = grid.cellSize, spacing = grid.spacing, cot = grid.constraintCount,
            tiLe = DocTiLe(ui),
        };
        if (!EditorPrefs.HasKey(KhoaCu)) EditorPrefs.SetString(KhoaCu, JsonUtility.ToJson(cu));

        Undo.RecordObject(bang, "Bang cho gon");
        Undo.RecordObject(grid, "Bang cho gon");
        bang.sizeDelta = new Vector2(1580f, 900f);
        bang.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.cellSize = new Vector2(250f, 260f);
        grid.spacing = new Vector2(16f, 16f);
        GhiTiLe(ui, 1f);
        EditorSceneManager.MarkSceneDirty(bang.gameObject.scene);
        Selection.activeTransform = bang;
        Debug.Log($"[ChoGon] Popup_Board {cu.size}x{cu.scale.x} -> 1580x900 x0.9, luoi {cu.cot} cot -> 5 cot 250x260. Ctrl+S de luu. Chinh tiep: Popup_Board (Size/Scale) + Content_Listings (Grid Layout Group).");
        EditorUtility.DisplayDialog("Bang cho", "Xong: 5 the / hang, bang nho hon.\nBam Ctrl+S roi Play xem.\n\nChinh tay: Popup_Board (Width/Scale), Content_Listings > Grid Layout Group (Cell Size, Constraint Count).\nTra lai: muc 2.", "OK");
    }

    [MenuItem(Goc + "2. Tra lai bang cho nhu cu", false, 11)]
    private static void TraLai()
    {
        if (!EditorPrefs.HasKey(KhoaCu)) { EditorUtility.DisplayDialog("Bang cho", "Chua co gia tri cu (chua bam muc 1).", "OK"); return; }
        if (!Tim(out var bang, out var grid, out var ui)) return;
        var cu = JsonUtility.FromJson<Cu>(EditorPrefs.GetString(KhoaCu));
        Undo.RecordObject(bang, "Tra lai bang cho");
        Undo.RecordObject(grid, "Tra lai bang cho");
        bang.sizeDelta = cu.size; bang.localScale = cu.scale;
        grid.cellSize = cu.cell; grid.spacing = cu.spacing; grid.constraintCount = cu.cot;
        GhiTiLe(ui, cu.tiLe);
        EditorPrefs.DeleteKey(KhoaCu);
        EditorSceneManager.MarkSceneDirty(bang.gameObject.scene);
        Debug.Log("[ChoGon] Da tra lai bang cho nhu cu. Ctrl+S de luu.");
    }

    private static bool Tim(out RectTransform bang, out GridLayoutGroup grid, out MarketBoardUI ui)
    {
        bang = null; grid = null;
        ui = Object.FindFirstObjectByType<MarketBoardUI>(FindObjectsInactive.Include);
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Bang cho", "Thoat Play mode truoc.", "OK"); return false; }
        if (ui == null) { EditorUtility.DisplayDialog("Bang cho", "Khong thay MarketBoardUI (mo SCN_Farm).", "OK"); return false; }
        bang = ui.transform as RectTransform;
        var t = FindDeep(ui.transform, "Content_Listings");
        if (t != null) grid = t.GetComponent<GridLayoutGroup>();
        if (bang == null || grid == null) { EditorUtility.DisplayDialog("Bang cho", "Khong thay Popup_Board / Content_Listings (GridLayoutGroup).", "OK"); return false; }
        return true;
    }

    private static Transform FindDeep(Transform t, string ten)
    {
        if (t.name == ten) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindDeep(t.GetChild(i), ten); if (r != null) return r; }
        return null;
    }

    private static float DocTiLe(MarketBoardUI ui)
    {
        var so = new SerializedObject(ui);
        var p = so.FindProperty("tiLeToiDaBang");
        return p != null ? p.floatValue : 0.88f;
    }

    private static void GhiTiLe(MarketBoardUI ui, float v)
    {
        var so = new SerializedObject(ui);
        var p = so.FindProperty("tiLeToiDaBang");
        if (p == null) return;
        p.floatValue = v;
        so.ApplyModifiedProperties();
    }
}
