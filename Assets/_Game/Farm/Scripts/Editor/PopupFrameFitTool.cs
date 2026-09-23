// ============================================================================
//  POPUP: CAN KHUNG GO BAO TRON NOI DUNG — Tools > Popup > Can khung ...
// ----------------------------------------------------------------------------
//  Loi Sep thay: noi dung (panel, luoi o) lon hon hoac lech khoi khung go
//  Board_Border -> panel lo ra mep khung, vach Board_Grain thò ra 2 ben, nut X va
//  dinh goc khong nam dung goc.
//  Tool nay, voi MOI popup co "Board_Border":
//    1. Do hop bao (bounding box) cua tat ca noi dung dang bat (tru khung, dinh,
//       ruy bang, nut X, lop mo nen).
//    2. Dat Board_Border = hop bao + le (giay kem cua khung bat dau ~56px vao trong).
//    3. Tat Board_Fill_* (tam nau de len khung) va Board_Grain_* (vach thò ra ngoai).
//    4. Dat 4 dinh vao 4 goc khung, ruy bang giua mep tren, nut X vao goc tren-phai.
//  Chi doi RectTransform cua khung/dinh/ruy bang/nut X — KHONG dong vao noi dung.
//  Co Undo + tu sao luu file scene. Shop (popup_Menu) bo qua vi Sep noi da dep.
// ============================================================================
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PopupFrameFitTool
{
    const float LE_NGANG = 56f, LE_DUOI = 56f, LE_TREN = 70f;   // le trong khung go de noi dung nam tren giay kem
    const float DINH_VAO = 26f;                                  // dinh cach goc khung
    static readonly string[] BO_QUA_GOC = { "popup_Menu" };      // Shop: giu nguyen

    [MenuItem("Tools/Popup/Can khung go bao tron noi dung (tat ca popup)")]
    public static void CanTatCa()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Popup", "Thoat Play roi bam lai.", "OK"); return; }
        var scene = EditorSceneManager.GetActiveScene();
        if (!LuuVaSaoLuu(scene)) return;

        var khung = new List<RectTransform>();
        foreach (var go in scene.GetRootGameObjects())
            foreach (var t in go.GetComponentsInChildren<RectTransform>(true))
                if (t.name == "Board_Border" && !TrongGocBoQua(t)) khung.Add(t);

        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Can khung popup"); int g = Undo.GetCurrentGroup();
        var log = new System.Text.StringBuilder();
        foreach (var bb in khung) log.AppendLine(CanMot(bb));
        Undo.CollapseUndoOperations(g);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[PopupFrameFit]\n" + log);
        EditorUtility.DisplayDialog("Popup", "Da can " + khung.Count + " popup:\n\n" + log + "\nCtrl+S de luu. Khong ung thi Ctrl+Z.", "OK");
    }

    static bool TrongGocBoQua(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            foreach (var n in BO_QUA_GOC) if (p.name == n) return true;
        return false;
    }

    static bool LaVo(string n)
    {
        return n.StartsWith("Board_") || n.StartsWith("Stud_") || n == "Header_Banner" ||
               n.Contains("Dim") || n.Contains("Overlay") || n.Contains("Blocker") || LaNutDong(n);
    }
    static bool LaNutDong(string n) { var s = n.ToLowerInvariant(); return s == "btn_close" || s == "btnclose" || s == "button_close" || s == "close"; }

    static string CanMot(RectTransform bb)
    {
        var P = bb.parent as RectTransform;
        if (P == null) return bb.name + ": khong co cha";
        string tenPopup = P.name + (P.parent != null ? " (" + P.parent.name + ")" : "");

        // 1. hop bao noi dung trong khong gian local cua P
        var mn = new Vector2(float.MaxValue, float.MaxValue); var mx = new Vector2(float.MinValue, float.MinValue);
        var goc = new Vector3[4]; int dem = 0;
        for (int i = 0; i < P.childCount; i++)
        {
            var c = P.GetChild(i) as RectTransform;
            if (c == null || !c.gameObject.activeSelf || LaVo(c.name)) continue;
            if (c.rect.width > 2000f || c.rect.height > 1500f) continue;
            c.GetWorldCorners(goc);
            for (int k = 0; k < 4; k++) { Vector2 l = P.InverseTransformPoint(goc[k]); mn = Vector2.Min(mn, l); mx = Vector2.Max(mx, l); }
            dem++;
        }
        if (dem == 0) return tenPopup + ": khong thay noi dung, bo qua";

        float trai = mn.x - LE_NGANG, phai = mx.x + LE_NGANG, duoi = mn.y - LE_DUOI, tren = mx.y + LE_TREN;
        Vector2 tam = new Vector2((trai + phai) * 0.5f, (duoi + tren) * 0.5f);
        Vector2 co  = new Vector2(phai - trai, tren - duoi);

        DatTamCo(bb, tam, co);

        // 3. tat lop de len khung / vach thò ra
        for (int i = 0; i < P.childCount; i++)
        {
            var c = P.GetChild(i); string n = c.name;
            if (n == "Board_Fill_Bottom" || n == "Board_Fill_Top" || n.StartsWith("Board_Grain") ||
                (n.StartsWith("Stud_") && (n.EndsWith("_Base") || n.EndsWith("_Shine"))))
            { if (c.gameObject.activeSelf) { Undo.RecordObject(c.gameObject, "tat"); c.gameObject.SetActive(false); } }
        }

        // 4. dinh goc: xep theo goc phan tu dang dung
        for (int i = 0; i < P.childCount; i++)
        {
            var c = P.GetChild(i) as RectTransform;
            if (c == null || !c.name.StartsWith("Stud_") || !c.name.EndsWith("_Rim")) continue;
            Vector2 cur = c.localPosition;
            float sx = cur.x >= tam.x ? 1f : -1f, sy = cur.y >= tam.y ? 1f : -1f;
            DatTam(c, new Vector2(tam.x + sx * (co.x * 0.5f - DINH_VAO), tam.y + sy * (co.y * 0.5f - DINH_VAO)));
        }
        // ruy bang: giua mep tren, lui xuong 18 nhu popup Kho
        var rb = P.Find("Header_Banner") as RectTransform;
        if (rb != null) DatTam(rb, new Vector2(tam.x, tren - 18f));
        // nut X: goc tren-phai, tam nam tren duong vien
        for (int i = 0; i < P.childCount; i++)
        {
            var c = P.GetChild(i) as RectTransform;
            if (c != null && LaNutDong(c.name)) DatTam(c, new Vector2(phai - 14f, tren - 14f));
        }
        return $"{tenPopup}: khung {co.x:0}x{co.y:0}, bao {dem} khoi noi dung";
    }

    static void DatTamCo(RectTransform rt, Vector2 tam, Vector2 co)
    {
        Undo.RecordObject(rt, "can khung");
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = co;
        rt.localPosition = new Vector3(tam.x, tam.y, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
    }

    static void DatTam(RectTransform rt, Vector2 tam)
    {
        Undo.RecordObject(rt, "can khung");
        var pv = rt.pivot; var s = rt.rect.size;
        // localPosition la vi tri pivot; doi tam -> pivot
        rt.localPosition = new Vector3(tam.x + (pv.x - 0.5f) * s.x * rt.localScale.x, tam.y + (pv.y - 0.5f) * s.y * rt.localScale.y, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rt);
    }

    static bool LuuVaSaoLuu(UnityEngine.SceneManagement.Scene scene)
    {
        if (scene.isDirty)
        {
            int c = EditorUtility.DisplayDialogComplex("Popup", "Scene chua luu. Luu truoc de sao luu?", "Luu roi chay", "Huy", "Chay luon");
            if (c == 1) return false;
            if (c == 0) EditorSceneManager.SaveScene(scene);
        }
        if (string.IsNullOrEmpty(scene.path)) return true;
        try
        {
            Directory.CreateDirectory("_Backup_Scene_TruocKhiVa");
            File.Copy(scene.path, Path.Combine("_Backup_Scene_TruocKhiVa", Path.GetFileNameWithoutExtension(scene.path) + "_KhungPopup_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity"), true);
        }
        catch (System.Exception e) { return EditorUtility.DisplayDialog("Popup", "Khong sao luu duoc: " + e.Message + "\nVan chay?", "Chay", "Huy"); }
        return true;
    }
}
