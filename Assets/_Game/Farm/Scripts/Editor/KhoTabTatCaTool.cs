// ============================================================================
//  Tools > Kho > Can bo cuc kho: khung ngang rong + 4 tab (Edit mode)
// ----------------------------------------------------------------------------
//  Chay TRONG Unity (khong sua file scene tu ben ngoai — Unity dang mo scene se ghi de).
//  Lam 1 lan la xong, Ctrl+S luu. Play dung dung bo cuc nay (WarehousePopupUI chi dat
//  vi tri X cua tab theo cung hang so TAB_RONG/TAB_CACH).
//
//  So do (don vi canvas 1920x1080, tam Panel_Dim):
//    Khung go  : rong 1680 (truoc 1457), tam x = 2.5  -> mep -837.5 .. 842.5
//    Le trong  : 56 moi ben
//    Cot phai  : rong 520  -> 266.5 .. 786.5
//    Khe giua  : 40
//    Cot trai  : rong 1008 -> -781.5 .. 226.5  (tab + luoi 5 cot + thanh suc chua)
//    4 tab     : rong 236, cach 14 (tong 986) nam gon trong cot trai
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class KhoTabTatCaTool
{
    const float KHUNG_RONG = 1680f, KHUNG_X = 2.5f, LE = 56f, KHE = 40f, PHAI_RONG = 520f;

    [MenuItem("Tools/Kho/Can bo cuc kho: khung ngang rong + 4 tab", false, 10)]
    private static void Chay()
    {
        var ui = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (ui == null) { EditorUtility.DisplayDialog("Kho", "Khong thay WarehousePopupUI. Hay mo SCN_Farm.", "OK"); return; }
        var so = new SerializedObject(ui);
        var root = so.FindProperty("popupRoot").objectReferenceValue as GameObject;
        var dim = root != null ? root.transform.Find("Panel_Dim") : null;
        if (dim == null) { EditorUtility.DisplayDialog("Kho", "Khong thay popupRoot/Panel_Dim.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(dim.gameObject, "Can bo cuc kho");

        float mepTrai = KHUNG_X - KHUNG_RONG * 0.5f, mepPhai = KHUNG_X + KHUNG_RONG * 0.5f;
        float phaiX = mepPhai - LE - PHAI_RONG * 0.5f;
        float traiRong = (mepPhai - LE - PHAI_RONG - KHE) - (mepTrai + LE);
        float traiX = (mepTrai + LE) + traiRong * 0.5f;

        Rong(dim, "Board_Border", KHUNG_RONG);
        DatX(dim, "Btn_Close", mepPhai - 14f);
        DatX(dim, "Stud_0_Rim", mepTrai + 26f); DatX(dim, "Stud_2_Rim", mepTrai + 26f);
        DatX(dim, "Stud_1_Rim", mepPhai - 26f); DatX(dim, "Stud_3_Rim", mepPhai - 26f);
        DatY(dim, "Header_Banner", 404f);          // ruy bang cao hon, khong de len khung phai

        DatX(dim, "Right_DetailPanel", phaiX); Rong(dim, "Right_DetailPanel", PHAI_RONG);

        var trai = dim.Find("Left_Container");
        if (trai != null)
        {
            DatX(dim, "Left_Container", traiX); Rong(dim, "Left_Container", traiRong);
            Rong(trai, "Tabs_Row", traiRong);
            Rong(trai, "Inner_GridBox", traiRong);
            // Hang tab nam TREN mep luoi do: day tab (ke ca tab chua chon ha -6) cach mep luoi 4px
            var boxRt = trai.Find("Inner_GridBox") as RectTransform;
            var hangRt = trai.Find("Tabs_Row") as RectTransform;
            if (boxRt != null && hangRt != null)
            {
                float mepTren = boxRt.anchoredPosition.y + boxRt.sizeDelta.y * 0.5f;
                DatY(trai, "Tabs_Row", mepTren + hangRt.sizeDelta.y * 0.5f + 10f);
                hangRt.SetAsLastSibling();   // ve SAU luoi => tab luon nam tren, khong bi khung luoi de
            }
            var box = trai.Find("Inner_GridBox");
            if (box != null) Rong(box, "Scroll_Items", traiRong - 10f);
            var bar = trai.Find("CapacityBar_Root");
            if (bar != null)
            {
                Rong(trai, "CapacityBar_Root", traiRong); DatX(trai, "CapacityBar_Root", 0f);
                float track = traiRong - 230f;
                Rong(bar, "Progress_Track", track); DatX(bar, "Progress_Track", -traiRong * 0.5f + 20f + track * 0.5f);
                var tr = bar.Find("Progress_Track"); if (tr != null) Rong(tr, "Progress_Fill", track - 3f);
                DatX(bar, "Txt_Capacity", traiRong * 0.5f - 100f);
            }
            // Luoi do: 5 cot vua kin be ngang
            var grid = so.FindProperty("itemGridContainer").objectReferenceValue as Transform;
            var gl = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
            if (gl != null)
            {
                Undo.RecordObject(gl, "Luoi kho");
                float trong = traiRong - 10f - 32f;              // tru padding 16x2
                const int COT = 5; const float CACH = 18f;
                float o = Mathf.Floor((trong - CACH * (COT - 1)) / COT);
                gl.cellSize = new Vector2(o, 160f);
                gl.spacing = new Vector2(CACH, 18f);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gl.constraintCount = COT;
                gl.childAlignment = TextAnchor.UpperCenter;
            }
        }

        // 4 tab
        var a = so.FindProperty("rectTabNongSan").objectReferenceValue as RectTransform;
        var b = so.FindProperty("rectTabChanNuoi").objectReferenceValue as RectTransform;
        var c = so.FindProperty("rectTabMonAn").objectReferenceValue as RectTransform;
        var pIcon = so.FindProperty("iconTabTatCa");
        var icon = pIcon != null ? pIcon.objectReferenceValue as Sprite : null;
        RectTransform tab = null;
        if (a != null)
        {
            bool daCo = a.parent.Find("Tab_All") != null;
            tab = WarehousePopupUI.DamBaoTabTatCa(a, b, c, icon);
            if (!daCo && tab != null) Undo.RegisterCreatedObjectUndo(tab.gameObject, "Tab Tat ca");
        }

        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        if (tab != null) Selection.activeTransform = tab;
        Debug.Log("[Kho] Khung " + KHUNG_RONG + " | cot trai " + traiRong + " | cot phai " + PHAI_RONG +
                  " | 4 tab " + WarehousePopupUI.TAB_RONG + "/" + WarehousePopupUI.TAB_CACH + ". Bam Ctrl+S de luu.");
    }

    [MenuItem("Tools/Kho/Dung thanh kho (toast) gon: 1 khung + 1 icon + 1 fill bar", false, 11)]
    private static void DungToast()
    {
        var t = Object.FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include);
        if (t == null) { EditorUtility.DisplayDialog("Kho", "Khong thay WarehouseGainToastUI trong scene. Hay mo SCN_Farm.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Toast kho gon");
        var panel = t.transform.Find("Panel_WarehouseToast");
        int xoa = 0;
        if (panel != null)
            foreach (var ten in new[] { "Img_BarnHouse", "Badge_Header", "Progress_Container", "Img_CrateBadge", "Txt_Plus" })
            {
                Transform x;
                while ((x = panel.Find(ten)) != null) { Undo.DestroyObjectImmediate(x.gameObject); xoa++; }
            }
        t.EnsureBuilt(true);
        panel = t.transform.Find("Panel_WarehouseToast");
        if (panel != null)
        {
            panel.gameObject.SetActive(true);                 // de Sep thay va chinh; Play tu an lai
            var cg = panel.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = 1f;
            Selection.activeTransform = panel;
        }
        EditorUtility.SetDirty(t);
        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        Debug.Log("[Kho] Thanh kho gon: xoa " + xoa + " lop cu chong nhau; con Panel (khung) + Img_Icon + Bar_Track/Bar_Fill/Txt_Count. Chinh tay xong bam Ctrl+S.");
    }

    [MenuItem("Tools/Kho/Hien thanh kho tren HUD de chinh tay (khong doi bo cuc)", false, 12)]
    private static void HienToastDeChinh()
    {
        var t = Object.FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include);
        if (t == null) { EditorUtility.DisplayDialog("Kho", "Khong thay WarehouseGainToastUI. Hay mo SCN_Farm.", "OK"); return; }
        var panel = t.transform.Find("Panel_WarehouseToast");
        if (panel == null) { EditorUtility.DisplayDialog("Kho", "Chua co Panel_WarehouseToast - bam 'Dung thanh kho (toast) gon' truoc.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Hien thanh kho");
        var glow = panel.GetComponent<UIGlowPulseFX>();
        if (glow != null) Undo.DestroyObjectImmediate(glow);            // vong sang cam
        var halo = panel.Find("FX_GlowHalo");
        if (halo != null) Undo.DestroyObjectImmediate(halo.gameObject);
        panel.gameObject.SetActive(true);
        var cg = panel.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = 1f;
        var so = new SerializedObject(t);
        var p = so.FindProperty("boCucGon"); if (p != null) { p.boolValue = true; so.ApplyModifiedProperties(); }
        EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        Selection.activeTransform = panel;
        EditorGUIUtility.PingObject(panel.gameObject);
        Debug.Log("[Kho] Thanh kho dang hien o Canvas_HUD/WarehouseGainToast/Panel_WarehouseToast: keo tha Img_Icon, Bar_Track, Bar_Fill, Txt_Count tuy y roi Ctrl+S. Play se giu nguyen (boCucGon = true), tu an/hien khi thu hoach.");
    }

    private static RectTransform Lay(Transform cha, string ten)
    {
        var t = cha.Find(ten) as RectTransform;
        if (t != null) Undo.RecordObject(t, "Can bo cuc kho");
        return t;
    }
    private static void Rong(Transform cha, string ten, float w) { var t = Lay(cha, ten); if (t != null) t.sizeDelta = new Vector2(w, t.sizeDelta.y); }
    private static void DatX(Transform cha, string ten, float x) { var t = Lay(cha, ten); if (t != null) t.anchoredPosition = new Vector2(x, t.anchoredPosition.y); }
    private static void DatY(Transform cha, string ten, float y) { var t = Lay(cha, ten); if (t != null) t.anchoredPosition = new Vector2(t.anchoredPosition.x, y); }
}
