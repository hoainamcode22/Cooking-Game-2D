// ============================================================================
//  Tools > Cho > 1. Gian khung go ngang: noi dung nam GON trong khung (2026-09-24)
//  Bang cho (Canvas_MarketPopup/Panel_Dim/Popup_Board): khung go rong them 2 x "gian" ma
//  noi dung (hang danh muc, luoi hang, chip vang / dong ho / nut lam moi) GIU NGUYEN cho
//  tren man hinh -> khung go om ra ngoai, khong con de len the hang.
//  Co Undo. Chinh tay tiep trong Hierarchy roi Ctrl+S.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ChoBoCucTool
{
    private const float GIAN = 36f;   // moi ben

    [MenuItem("Tools/Cho/1. Gian khung go ngang (noi dung nam gon trong khung)", false, 10)]
    private static void Chay()
    {
        RectTransform bang = null;
        foreach (var rt in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (rt.name == "Popup_Board" && rt.parent != null && rt.parent.name == "Panel_Dim" && rt.root.name == "Canvas_MarketPopup") { bang = rt; break; }
        if (bang == null) { EditorUtility.DisplayDialog("Cho", "Khong thay Canvas_MarketPopup/Panel_Dim/Popup_Board. Hay mo SCN_Farm.", "OK"); return; }

        Undo.RegisterFullObjectHierarchyUndo(bang.gameObject, "Gian khung go cho");
        bang.sizeDelta = new Vector2(bang.sizeDelta.x + GIAN * 2f, bang.sizeDelta.y);

        int n = 0;
        for (int i = 0; i < bang.childCount; i++)
        {
            var c = bang.GetChild(i) as RectTransform;
            if (c == null) continue;
            if (c.name == "Header_Banner" || c.name == "Btn_Close" || c.name == "Panel_Toast") continue;   // nam tren khung / giua
            bool neoTrai = c.anchorMax.x <= 0.001f;
            bool neoPhai = c.anchorMin.x >= 0.999f;
            bool gianNgang = c.anchorMin.x <= 0.001f && c.anchorMax.x >= 0.999f;
            if (gianNgang)      c.sizeDelta = new Vector2(c.sizeDelta.x - GIAN * 2f, c.sizeDelta.y);     // giu nguyen be rong that
            else if (neoTrai)   c.anchoredPosition += new Vector2(GIAN, 0f);
            else if (neoPhai)   c.anchoredPosition -= new Vector2(GIAN, 0f);
            else continue;
            n++;
        }
        EditorSceneManager.MarkSceneDirty(bang.gameObject.scene);
        Selection.activeTransform = bang;
        Debug.Log($"[Cho] Khung go rong them {GIAN * 2f:0} (moi ben {GIAN:0}); giu nguyen cho {n} khoi noi dung. Bam them lan nua neu muon rong hon. Ctrl+S de luu.");
    }
}
