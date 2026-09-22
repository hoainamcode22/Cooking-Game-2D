// ============================================================================
//  KITCHEN: DONG BANG UI THANH HIERARCHY
//  Tools ▸ Farm Game ▸ Kitchen: Dong bang UI thanh Hierarchy
// ----------------------------------------------------------------------------
//  VAN DE SEP GAP: UI bep dung bang CODE luc chay. Chinh tay trong Scene xong
//  bam Play la code dung lai tu dau, mat het chinh tay.
//
//  TOOL NAY LAM 3 VIEC, MOT LAN, VINH VIEN:
//    B1. Don V3 (neu con) + bat lai canvas V2 bi an.
//    B2. Goi BuildEditorPreview() NGAY TRONG EDIT MODE => toan bo UI vat chat hoa
//        thanh GameObject that trong Hierarchy (khong phai luc chay moi co).
//    B3. Bat co "Khoa layout" => tu day code KHONG duoc ghi vi tri/kich thuoc nua.
//        Code chi con doi CHU, SO, SPRITE, mau, bat/tat object.
//  Sep Ctrl+S mot lan. Tu do keo tha thoai mai, Play khong mat gi.
//
//  MUON QUAY LAI DUNG BANG CODE: bo tick "Khoa layout" tren component
//  KitchenSceneV2UI trong Inspector, roi bam nut "Dung lai bang code" duoi day.
// ============================================================================
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenFreezeToHierarchyTool : EditorWindow
{
    private const string MENU = "Tools/Farm Game/Kitchen: Dong bang UI thanh Hierarchy";

    private Vector2 _sc;
    private readonly List<string> _log = new List<string>();

    [MenuItem(MENU, false, 40)]
    public static void Open() => GetWindow<KitchenFreezeToHierarchyTool>("Dong bang UI Bep");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Mo scene BEP (SampleScene) roi bam nut xanh.\n\n" +
            "Sau khi chay: moi thanh phan UI la GameObject that trong Hierarchy. " +
            "Sep keo tha chinh tay, Ctrl+S, bam Play — vi tri GIU NGUYEN.\n" +
            "Code chi con doi chu / so / sprite, khong duoc dat lai vi tri nua.",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "NUT DUNG LAI DA BI GO BO (2026-09-21).\n" +
            "Ly do: no goi BuildEditorPreview() -> RebuildNow() XOA SACH con roi dung lai bang code. " +
            "Trong Edit Mode khong co du lieu runtime (CookingSelectionManager, Loc chua khoi tao) nen " +
            "dung ra THIEU khay nguyen lieu, thieu danh sach mon, va chu quay ve tieng Viet.\n\n" +
            "DUNG TOOL NAY THAY THE:  Tools > Farm Game > Kitchen: Chup UI luc Play thanh Hierarchy\n" +
            "No chup dung trang thai Play (day du, tieng Anh) roi dan nguoc vao scene.",
            MessageType.Warning);
        if (GUILayout.Button("Mo tool CHUP UI luc Play (nen dung cai nay)", GUILayout.Height(30)))
            EditorApplication.ExecuteMenuItem("Tools/Farm Game/Kitchen: Chup UI luc Play thanh Hierarchy");

        GUILayout.Space(8);
        EditorGUILayout.LabelField("Tien ich", EditorStyles.boldLabel);
        if (GUILayout.Button("Chi BAT LAI canvas bep bi an (khong dung lai)")) BatLaiCanvas(true);
        if (GUILayout.Button("Kiem tra trang thai hien tai")) KiemTra();

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(1f, 0.8f, 0.7f);
        if (GUILayout.Button("Dung lai bang CODE (bo khoa, xoa chinh tay)")) DungLaiBangCode();
        GUI.backgroundColor = Color.white;

        _sc = EditorGUILayout.BeginScrollView(_sc);
        foreach (var d in _log) EditorGUILayout.LabelField(d);
        EditorGUILayout.EndScrollView();
    }

    // ── tim component UI bep du dang bi an hay doi ten ───────────────────────
    private static MonoBehaviour TimUIBep(out System.Type kieu)
    {
        kieu = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
            .FirstOrDefault(t => t.Name == "KitchenSceneV2UI");
        if (kieu == null) return null;

        var tat = Resources.FindObjectsOfTypeAll(kieu).OfType<MonoBehaviour>()
            .Where(m => m != null && m.gameObject.scene.IsValid()).ToArray();
        return tat.Length > 0 ? tat[0] : null;
    }

    private void DongBang()
    {
        _log.Clear();
        var scene = SceneManager.GetActiveScene();
        _log.Add("Scene: " + scene.name);

        // ── B1a. don V3 neu con sot ──
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Canvas_KitchenV3")
            {
                Undo.DestroyObjectImmediate(go);
                _log.Add("B1  da xoa Canvas_KitchenV3 (suon V3 bo di).");
            }
        }

        // ── B1b. bat lai canvas bep ──
        BatLaiCanvas(false);

        // ── B2. vat chat hoa UI ──
        var ui = TimUIBep(out var kieu);
        if (ui == null)
        {
            _log.Add("LOI  Khong tim thay KitchenSceneV2UI trong scene nay.");
            _log.Add("     Mo dung scene BEP (SampleScene) roi bam lai.");
            Repaint(); return;
        }
        _log.Add("B2  tim thay component tren: " + LayDuongDan(ui.transform));

        // bo khoa tam de con dung duoc
        DatCo(ui, kieu, false);

        int truoc = ui.transform.childCount;
        var mBuild = kieu.GetMethod("BuildEditorPreview",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (mBuild == null)
        {
            _log.Add("LOI  Khong co ham BuildEditorPreview() trong KitchenSceneV2UI.");
            Repaint(); return;
        }

        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Dong bang UI bep");
        try
        {
            mBuild.Invoke(ui, null);
        }
        catch (System.Exception e)
        {
            _log.Add("CANH BAO  BuildEditorPreview nem loi: " + (e.InnerException?.Message ?? e.Message));
            _log.Add("          UI co the dung thieu mot phan. Doc Console xem buoc nao gay.");
        }
        int sau = ui.transform.childCount;
        int tong = ui.GetComponentsInChildren<Transform>(true).Length;
        _log.Add($"B2  da vat chat hoa: {truoc} -> {sau} cum goc, TONG {tong} object trong Hierarchy.");

        // ── B3. bat khoa ──
        DatCo(ui, kieu, true);
        _log.Add("B3  da BAT 'Khoa layout'. Tu gio code khong ghi vi tri/kich thuoc nua.");

        // kiem tra cong tac bind
        bool coBanner = ui.transform.Find("Order_Banner") != null;
        _log.Add(coBanner
            ? "OK  Co 'Order_Banner' -> luc Play code se chay duong BIND (chi noi tham chieu)."
            : "CANH BAO  THIEU 'Order_Banner' -> luc Play code se DUNG LAI tu dau. Phai co object nay!");

        EditorUtility.SetDirty(ui);
        EditorSceneManagerMarkDirty();
        _log.Add("");
        _log.Add("XONG. Bay gio bam Ctrl+S de luu scene.");
        _log.Add("Sau do keo tha chinh tay thoai mai, bam Play khong mat gi.");
        Repaint();
    }

    private void BatLaiCanvas(bool rieng)
    {
        if (rieng) _log.Clear();
        var scene = SceneManager.GetActiveScene();
        int bat = 0;

        // canvas bep co the da bi doi ten kieu "_OLD_...", "Canvas_KitchenV2 (an)"...
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid() || go.scene != scene) continue;
            string t = go.name.ToLowerInvariant();
            bool laBep = t.Contains("kitchen") && (t.Contains("canvas") || t.Contains("ui"));
            if (!laBep) continue;
            if (!go.activeSelf)
            {
                Undo.RecordObject(go, "Bat lai canvas bep");
                go.SetActive(true);
                EditorUtility.SetDirty(go);
                _log.Add("B1  bat lai: " + go.name);
                bat++;
            }
            // tra lai ten chuan neu bi them tien to
            if (go.name.StartsWith("_OLD_") || go.name.StartsWith("_BACKUP_"))
            {
                string moi = go.name.Replace("_OLD_", "").Replace("_BACKUP_", "");
                Undo.RecordObject(go, "Doi ten canvas bep");
                _log.Add($"B1  doi ten: {go.name} -> {moi}");
                go.name = moi;
                EditorUtility.SetDirty(go);
            }
        }
        if (bat == 0) _log.Add("B1  khong co canvas bep nao dang tat.");
        if (rieng) { EditorSceneManagerMarkDirty(); Repaint(); }
    }

    private void KiemTra()
    {
        _log.Clear();
        var ui = TimUIBep(out var kieu);
        if (ui == null) { _log.Add("Khong thay KitchenSceneV2UI trong scene."); Repaint(); return; }

        var f = kieu.GetField("khoaLayout", BindingFlags.NonPublic | BindingFlags.Instance);
        bool khoa = f != null && (bool)f.GetValue(ui);
        int tong = ui.GetComponentsInChildren<Transform>(true).Length;
        bool banner = ui.transform.Find("Order_Banner") != null;

        _log.Add("Object      : " + LayDuongDan(ui.transform));
        _log.Add("Dang bat    : " + ui.gameObject.activeInHierarchy);
        _log.Add("So object   : " + tong + (tong < 20 ? "   <- CHUA vat chat hoa, con dung bang code" : "   <- da co Hierarchy that"));
        _log.Add("Order_Banner: " + (banner ? "CO  -> Play se BIND, khong dung lai" : "THIEU -> Play se DUNG LAI, mat chinh tay"));
        _log.Add("Khoa layout : " + (khoa ? "BAT  -> code khong ghi vi tri" : "TAT  -> code VAN CO THE ghi de vi tri"));
        _log.Add("");
        _log.Add(khoa && banner && tong > 20
            ? "KET LUAN: da dong bang dung cach. Keo tha thoai mai."
            : "KET LUAN: CHUA an toan. Bam nut xanh 'DONG BANG UI BEP'.");
        Repaint();
    }

    private void DungLaiBangCode()
    {
        if (!EditorUtility.DisplayDialog("Dung lai bang code",
            "NGUY HIEM: XOA SACH Hierarchy hien tai roi dung lai bang code.\n\nTrong Edit Mode se dung THIEU (khong co khay, khong co danh sach mon, chu tieng Viet).\nChi dung khi muon lam lai tu dau va se chay Play de dung day du.\n\nChac chan?", "Dung lai", "Huy")) return;
        _log.Clear();
        var ui = TimUIBep(out var kieu);
        if (ui == null) { _log.Add("Khong thay KitchenSceneV2UI."); Repaint(); return; }
        DatCo(ui, kieu, false);
        var m = kieu.GetMethod("BuildEditorPreview", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Dung lai bang code");
        m?.Invoke(ui, null);
        _log.Add("Da dung lai bang code. 'Khoa layout' dang TAT.");
        EditorUtility.SetDirty(ui); EditorSceneManagerMarkDirty(); Repaint();
    }

    private static void DatCo(MonoBehaviour ui, System.Type kieu, bool bat)
    {
        var f = kieu.GetField("khoaLayout", BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) { Undo.RecordObject(ui, "Khoa layout"); f.SetValue(ui, bat); }
        var sf = kieu.GetField("KhoaLayout", BindingFlags.Public | BindingFlags.Static);
        sf?.SetValue(null, bat);
    }

    private static string LayDuongDan(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    private static void EditorSceneManagerMarkDirty() =>
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
}
