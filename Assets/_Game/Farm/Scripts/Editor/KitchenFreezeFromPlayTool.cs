// ============================================================================
//  KITCHEN: CHUP UI LUC PLAY  →  HIERARCHY CO DINH
//  Tools ▸ Farm Game ▸ Kitchen: Chup UI luc Play thanh Hierarchy
// ----------------------------------------------------------------------------
//  VI SAO CAN: BuildEditorPreview() chi dung KHUNG TINH. Con NOI DUNG DONG
//  (the mon, o nguyen lieu, o gia vi, meo dau bep, meo than tai, the khay...)
//  duoc sinh luc CHAY tu du lieu. Vi vay Scene view trong tho, Play moi day du.
//
//  CACH LAM CHUAN CUA UNITY: dang Play thi luu ca cay object thanh PREFAB,
//  thoat Play xong dan nguoc lai vao scene. Unity giu nguyen moi object,
//  moi RectTransform, moi sprite da gan. Tool nay tu dong hoa 2 buoc do.
//
//  QUY TRINH:
//    1. Bam Play, doi UI hien day du.
//    2. Bam nut "CHUP" (van dang Play).  -> luu prefab tam
//    3. Thoat Play.
//    4. Bam nut "DAN".                   -> thay canvas cu bang ban chup
//    5. Ctrl+S.
//  Tu do moi thu la object that, keo tha thoai mai.
//
//  LUU Y THAT:
//    • Listener nut (onClick) KHONG duoc serialize — nhung khong sao: luc Play
//      BindExistingHierarchy() gan lai het theo ten object.
//    • Texture sinh bang code luc chay (neu co) se mat -> tool bao ro cho nao.
//    • Sau khi dan, co "Khoa layout" van BAT nen code khong dat lai vi tri.
// ============================================================================
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenFreezeFromPlayTool : EditorWindow
{
    private const string MENU     = "Tools/Farm Game/Kitchen: Chup UI luc Play thanh Hierarchy";
    private const string THU_MUC  = "Assets/_Game/Prefab/_FrozenUI";
    private const string DUONG_DAN= THU_MUC + "/Kitchen_UI_v2_Frozen.prefab";
    private const string KEY_TEN  = "KitchenFreeze_TenObject";
    private const string KEY_ANH  = "KitchenFreeze_ChiSoAnhEm";

    private Vector2 _sc;
    private readonly List<string> _log = new List<string>();

    [MenuItem(MENU, false, 41)]
    public static void Open() => GetWindow<KitchenFreezeFromPlayTool>("Chup UI luc Play");

    private void OnGUI()
    {
        bool dangPlay = EditorApplication.isPlaying;

        EditorGUILayout.HelpBox(
            "B1  Bam Play, doi UI bep hien DAY DU (the mon, o nguyen lieu, meo...).\n" +
            "B2  Van dang Play -> bam nut CHUP.\n" +
            "B3  Thoat Play.\n" +
            "B4  Bam nut DAN.\n" +
            "B5  Ctrl+S.\n\n" +
            "Sau do moi thanh phan la GameObject that, keo tha thoai mai, Play khong mat.",
            MessageType.Info);

        EditorGUILayout.LabelField("Trang thai", dangPlay ? "DANG PLAY" : "DANG EDIT", EditorStyles.boldLabel);
        GUILayout.Space(6);

        GUI.enabled = dangPlay;
        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button("B2) CHUP UI dang chay  →  prefab tam", GUILayout.Height(34))) Chup();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        bool coBanChup = File.Exists(DUONG_DAN);
        GUILayout.Space(4);
        GUI.enabled = !dangPlay && coBanChup;
        GUI.backgroundColor = new Color(0.65f, 0.95f, 0.65f);
        if (GUILayout.Button("B4) DAN ban chup vao scene  →  Hierarchy co dinh", GUILayout.Height(34))) Dan();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (!coBanChup) EditorGUILayout.LabelField("   (chua co ban chup)");
        else EditorGUILayout.LabelField("   ban chup: " + DUONG_DAN);

        GUILayout.Space(10);
        if (GUILayout.Button("Kiem tra: so object trong canvas bep hien tai")) Dem();
        GUI.backgroundColor = new Color(1f, 0.85f, 0.8f);
        if (GUILayout.Button("Xoa ban chup tam")) XoaBanChup();
        GUI.backgroundColor = Color.white;

        _sc = EditorGUILayout.BeginScrollView(_sc);
        foreach (var d in _log) EditorGUILayout.LabelField(d);
        EditorGUILayout.EndScrollView();
    }

    private static GameObject TimCanvasBep()
    {
        var kieu = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new System.Type[0]; } })
            .FirstOrDefault(t => t.Name == "KitchenSceneV2UI");
        if (kieu != null)
        {
            var mb = Object.FindObjectsByType(kieu, FindObjectsInactive.Include, FindObjectsSortMode.None)
                           .OfType<MonoBehaviour>().FirstOrDefault();
            if (mb != null) return mb.gameObject;
        }
        var scene = SceneManager.GetActiveScene();
        return scene.GetRootGameObjects().FirstOrDefault(g =>
            g.name.ToLowerInvariant().Contains("kitchen") && g.GetComponent<Canvas>() != null);
    }

    private void Chup()
    {
        _log.Clear();
        var go = TimCanvasBep();
        if (go == null) { _log.Add("LOI  khong tim thay canvas bep dang chay."); Repaint(); return; }

        int tong = go.GetComponentsInChildren<Transform>(true).Length;
        _log.Add($"Tim thay: {go.name}  —  {tong} object dang song.");
        if (tong < 40)
            _log.Add("CANH BAO  chi " + tong + " object. UI co the chua dung xong, doi them roi chup lai.");

        if (!Directory.Exists(THU_MUC))
        {
            Directory.CreateDirectory(THU_MUC);
            AssetDatabase.Refresh();
        }

        // nho cho dung de dan lai dung thu tu
        EditorPrefs.SetString(KEY_TEN, go.name);
        EditorPrefs.SetInt(KEY_ANH, go.transform.GetSiblingIndex());

        var ban = PrefabUtility.SaveAsPrefabAsset(go, DUONG_DAN, out bool ok);
        if (!ok || ban == null)
        {
            _log.Add("LOI  khong luu duoc prefab. Xem Console.");
            Repaint(); return;
        }

        int tongPrefab = ban.GetComponentsInChildren<Transform>(true).Length;
        _log.Add($"CHUP XONG -> {DUONG_DAN}");
        _log.Add($"Prefab co {tongPrefab} object" + (tongPrefab == tong ? " (khop)" : $" (goc {tong}, lech {tong - tongPrefab})"));
        _log.Add("");
        _log.Add("=> Bay gio THOAT PLAY roi bam nut DAN.");
        Debug.Log($"[KitchenFreeze] Da chup {tongPrefab} object -> {DUONG_DAN}");
        Repaint();
    }

    private void Dan()
    {
        _log.Clear();
        var ban = AssetDatabase.LoadAssetAtPath<GameObject>(DUONG_DAN);
        if (ban == null) { _log.Add("LOI  khong thay ban chup. Chup truoc da."); Repaint(); return; }

        var scene = SceneManager.GetActiveScene();
        var cu = TimCanvasBep();
        int viTri = EditorPrefs.GetInt(KEY_ANH, -1);
        string ten = EditorPrefs.GetString(KEY_TEN, "Kitchen_UI_v2");

        if (cu != null)
        {
            if (viTri < 0) viTri = cu.transform.GetSiblingIndex();
            _log.Add("Xoa canvas cu: " + cu.name + $" ({cu.GetComponentsInChildren<Transform>(true).Length} object)");
            Undo.DestroyObjectImmediate(cu);
        }

        // dan thanh object THUONG, khong giu lien ket prefab -> Sep sua tu do
        var moi = (GameObject)PrefabUtility.InstantiatePrefab(ban, scene);
        PrefabUtility.UnpackPrefabInstance(moi, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        moi.name = ten;
        if (viTri >= 0) moi.transform.SetSiblingIndex(viTri);
        Undo.RegisterCreatedObjectUndo(moi, "Dan UI bep da chup");

        int tong = moi.GetComponentsInChildren<Transform>(true).Length;
        _log.Add($"DAN XONG: {moi.name} — {tong} object trong Hierarchy.");

        bool banner = moi.transform.Find("Order_Banner") != null;
        _log.Add(banner
            ? "OK  co 'Order_Banner' -> Play se BIND, khong dung lai."
            : "CANH BAO  THIEU 'Order_Banner' -> Play se DUNG LAI va xoa het!");

        // bao dam co khoa layout van bat
        var comp = moi.GetComponents<MonoBehaviour>()
                      .FirstOrDefault(c => c != null && c.GetType().Name == "KitchenSceneV2UI");
        if (comp != null)
        {
            var f = comp.GetType().GetField("khoaLayout",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null)
            {
                bool dangKhoa = (bool)f.GetValue(comp);
                if (!dangKhoa) { f.SetValue(comp, true); EditorUtility.SetDirty(comp); }
                _log.Add("OK  'Khoa layout' = BAT -> code khong dat lai vi tri.");
            }
        }
        else _log.Add("CANH BAO  ban chup khong con component KitchenSceneV2UI.");

        Selection.activeGameObject = moi;
        EditorSceneManager_MarkDirty();
        _log.Add("");
        _log.Add("=> Bam Ctrl+S de luu scene. Xong, keo tha thoai mai.");
        Debug.Log($"[KitchenFreeze] Da dan {tong} object vao scene.");
        Repaint();
    }

    private void Dem()
    {
        _log.Clear();
        var go = TimCanvasBep();
        if (go == null) { _log.Add("Khong thay canvas bep."); Repaint(); return; }
        int tong = go.GetComponentsInChildren<Transform>(true).Length;
        _log.Add($"{go.name}: {tong} object" + (EditorApplication.isPlaying ? "  (dang Play)" : "  (dang Edit)"));

        // liet ke cum goc de Sep doi chieu
        foreach (Transform c in go.transform)
            _log.Add($"   {c.name,-28} {c.GetComponentsInChildren<Transform>(true).Length} object con");
        Repaint();
    }

    private void XoaBanChup()
    {
        if (!File.Exists(DUONG_DAN)) { _log.Add("Khong co ban chup."); Repaint(); return; }
        AssetDatabase.DeleteAsset(DUONG_DAN);
        _log.Clear(); _log.Add("Da xoa ban chup tam.");
        Repaint();
    }

    private static void EditorSceneManager_MarkDirty() =>
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
}
