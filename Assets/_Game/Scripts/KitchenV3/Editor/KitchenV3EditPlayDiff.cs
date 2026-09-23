// ============================================================================
//  KITCHEN V3 — SO SANH EDIT MODE vs PLAY MODE (chi CHAN DOAN, khong sua gi)
// ----------------------------------------------------------------------------
//  Muc dich: Sep chinh UI bep bang tay o Edit mode, nhung luc Play mot so thu bi code
//  doi lai. Thay vi doan, tool nay:
//    1. Luc bam Play (truoc khi scene chay): chup lai TOAN BO Kitchen_UI_v3 — vi tri, kich
//       thuoc, bat/tat, sprite, mau, chu, co chu — ghi ra Library/KitchenEditSnapshot.json.
//    2. 3 giay sau khi vao Play: chup lai lan nua, so sanh tung object, ghi bao cao
//       Logs/KitchenEditVsPlay.txt (object nao bi doi, doi cai gi, object nao code sinh them).
//  Tat/bat: Tools > Kitchen V3 > 8. So sanh Edit vs Play (bat/tat)
// ============================================================================
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KitchenUIv2;

[InitializeOnLoad]
public static class KitchenV3EditPlayDiff
{
    private const string ROOT_V3   = "Kitchen_UI_v3";
    private const string FILE_SNAP = "Library/KitchenEditSnapshot.json";
    private const string FILE_REPORT = "Logs/KitchenEditVsPlay.txt";
    private const string MENU = "Tools/Kitchen V3/8. So sanh Edit vs Play (bat-tat)";
    private static string KhoaBat => "KitchenV3EditPlayDiff_on|" + Application.dataPath;

    [System.Serializable]
    private class Muc
    {
        public string path;
        public bool active;
        public Vector2 aMin, aMax, pivot, pos, size;
        public Vector3 scale;
        public int sibling;
        public bool hasImg; public string sprite; public Color imgColor; public bool imgEnabled; public int imgType; public bool preserve;
        public bool hasTxt; public string text; public float fontSize; public bool autoSize; public Color txtColor; public int wrap;
    }

    [System.Serializable]
    private class Goi { public List<Muc> ds = new List<Muc>(); }

    private static double _hen = -1;

    static KitchenV3EditPlayDiff()
    {
        EditorApplication.playModeStateChanged += KhiDoiMode;
    }

    private static bool DangBat => EditorPrefs.GetBool(KhoaBat, true);

    [MenuItem(MENU, false, 40)]
    private static void BatTat()
    {
        EditorPrefs.SetBool(KhoaBat, !DangBat);
        Debug.Log("[Kitchen V3] So sanh Edit vs Play: " + (DangBat ? "BAT" : "TAT"));
    }

    [MenuItem(MENU, true)]
    private static bool BatTatCheck()
    {
        Menu.SetChecked(MENU, DangBat);
        return true;
    }

    private static void KhiDoiMode(PlayModeStateChange s)
    {
        if (!DangBat) return;
        try
        {
            if (s == PlayModeStateChange.ExitingEditMode)
            {
                var goc = TimGoc();
                if (goc == null) { if (File.Exists(FILE_SNAP)) File.Delete(FILE_SNAP); return; }
                File.WriteAllText(FILE_SNAP, JsonUtility.ToJson(Chup(goc)));
            }
            else if (s == PlayModeStateChange.EnteredPlayMode)
            {
                if (!File.Exists(FILE_SNAP)) return;
                _hen = EditorApplication.timeSinceStartup + 3.0;
                EditorApplication.update -= Doi;
                EditorApplication.update += Doi;
            }
            else if (s == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= Doi;
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[Kitchen V3] So sanh Edit/Play loi: " + e.Message); }
    }

    private static void Doi()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Doi; return; }
        if (EditorApplication.timeSinceStartup < _hen) return;
        EditorApplication.update -= Doi;
        try { SoSanh(); }
        catch (System.Exception e) { Debug.LogWarning("[Kitchen V3] So sanh Edit/Play loi: " + e.Message); }
    }

    private static Transform TimGoc()
    {
        foreach (var ui in Object.FindObjectsByType<KitchenSceneV2UI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ui != null && ui.gameObject.name == ROOT_V3 && ui.gameObject.activeInHierarchy) return ui.transform;
        return null;
    }

    private static Goi Chup(Transform goc)
    {
        var g = new Goi();
        DiQua(goc, goc, g.ds);
        return g;
    }

    private static void DiQua(Transform goc, Transform t, List<Muc> ds)
    {
        if (t != goc) ds.Add(Doc(goc, t));
        for (int i = 0; i < t.childCount; i++) DiQua(goc, t.GetChild(i), ds);
    }

    // Duong dan bo qua lop boc "~SafeArea" (SafeAreaBootstrap chen luc Play) de khop voi Edit.
    private static string DuongDan(Transform goc, Transform t)
    {
        var sb = new List<string>();
        for (var x = t; x != null && x != goc; x = x.parent)
            if (x.name != "~SafeArea") sb.Add(x.name);
        sb.Reverse();
        return string.Join("/", sb);
    }

    private static Muc Doc(Transform goc, Transform t)
    {
        var m = new Muc { path = DuongDan(goc, t), active = t.gameObject.activeSelf, scale = t.localScale, sibling = t.GetSiblingIndex() };
        var rt = t as RectTransform;
        if (rt != null) { m.aMin = rt.anchorMin; m.aMax = rt.anchorMax; m.pivot = rt.pivot; m.pos = rt.anchoredPosition; m.size = rt.sizeDelta; }
        var img = t.GetComponent<Image>();
        if (img != null) { m.hasImg = true; m.sprite = img.sprite != null ? img.sprite.name : ""; m.imgColor = img.color; m.imgEnabled = img.enabled; m.imgType = (int)img.type; m.preserve = img.preserveAspect; }
        var tx = t.GetComponent<TMP_Text>();
        if (tx != null) { m.hasTxt = true; m.text = tx.text; m.fontSize = tx.fontSize; m.autoSize = tx.enableAutoSizing; m.txtColor = tx.color; m.wrap = (int)tx.textWrappingMode; }
        return m;
    }

    private static bool Khac(Vector2 a, Vector2 b) => (a - b).sqrMagnitude > 0.25f;
    private static bool Khac(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) > 0.02f;

    private static void SoSanh()
    {
        var goc = TimGoc();
        if (goc == null) return;
        var cu = JsonUtility.FromJson<Goi>(File.ReadAllText(FILE_SNAP));
        var moi = Chup(goc);

        var mapCu = new Dictionary<string, Muc>();
        foreach (var m in cu.ds) if (!mapCu.ContainsKey(m.path)) mapCu[m.path] = m;
        var mapMoi = new Dictionary<string, Muc>();
        foreach (var m in moi.ds) if (!mapMoi.ContainsKey(m.path)) mapMoi[m.path] = m;

        var doi = new StringBuilder(); var them = new StringBuilder(); var mat = new StringBuilder();
        int nDoi = 0, nThem = 0, nMat = 0;

        foreach (var kv in mapMoi)
        {
            if (!mapCu.TryGetValue(kv.Key, out var a)) { nThem++; if (nThem <= 150) them.AppendLine("  + " + kv.Key + (kv.Value.active ? "" : " (tat)")); continue; }
            var b = kv.Value;
            var ly = new List<string>();
            if (a.active != b.active) ly.Add("bat/tat " + a.active + " -> " + b.active);
            if (Khac(a.aMin, b.aMin) || Khac(a.aMax, b.aMax)) ly.Add("anchor " + a.aMin + "-" + a.aMax + " -> " + b.aMin + "-" + b.aMax);
            if (Khac(a.pivot, b.pivot)) ly.Add("pivot " + a.pivot + " -> " + b.pivot);
            if (Khac(a.pos, b.pos)) ly.Add("vi tri " + a.pos + " -> " + b.pos);
            if (Khac(a.size, b.size)) ly.Add("kich thuoc " + a.size + " -> " + b.size);
            if ((a.scale - b.scale).sqrMagnitude > 0.0004f) ly.Add("scale " + a.scale + " -> " + b.scale);
            if (a.sibling != b.sibling) ly.Add("thu tu " + a.sibling + " -> " + b.sibling);
            if (a.hasImg && b.hasImg)
            {
                if (a.sprite != b.sprite) ly.Add("sprite '" + a.sprite + "' -> '" + b.sprite + "'");
                if (Khac(a.imgColor, b.imgColor)) ly.Add("mau anh " + a.imgColor + " -> " + b.imgColor);
                if (a.imgEnabled != b.imgEnabled) ly.Add("anh bat " + a.imgEnabled + " -> " + b.imgEnabled);
                if (a.imgType != b.imgType) ly.Add("kieu anh " + a.imgType + " -> " + b.imgType);
                if (a.preserve != b.preserve) ly.Add("giu ti le " + a.preserve + " -> " + b.preserve);
            }
            if (a.hasTxt && b.hasTxt)
            {
                if (a.text != b.text) ly.Add("chu '" + Cat(a.text) + "' -> '" + Cat(b.text) + "'");
                if (Mathf.Abs(a.fontSize - b.fontSize) > 0.3f) ly.Add("co chu " + a.fontSize + " -> " + b.fontSize);
                if (a.autoSize != b.autoSize) ly.Add("autosize " + a.autoSize + " -> " + b.autoSize);
                if (Khac(a.txtColor, b.txtColor)) ly.Add("mau chu " + a.txtColor + " -> " + b.txtColor);
                if (a.wrap != b.wrap) ly.Add("xuong dong " + a.wrap + " -> " + b.wrap);
            }
            if (ly.Count > 0) { nDoi++; if (nDoi <= 400) doi.AppendLine("  * " + kv.Key + "\n      " + string.Join("\n      ", ly)); }
        }
        foreach (var kv in mapCu)
            if (!mapMoi.ContainsKey(kv.Key)) { nMat++; if (nMat <= 150) mat.AppendLine("  - " + kv.Key); }

        var bc = new StringBuilder();
        bc.AppendLine("SO SANH EDIT vs PLAY — " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  (3 giay sau khi vao Play)");
        bc.AppendLine("Object bi DOI: " + nDoi + "   |   code SINH THEM: " + nThem + "   |   bi XOA/doi ten: " + nMat);
        bc.AppendLine();
        bc.AppendLine("=== BI DOI ==="); bc.Append(doi);
        bc.AppendLine(); bc.AppendLine("=== SINH THEM LUC PLAY ==="); bc.Append(them);
        bc.AppendLine(); bc.AppendLine("=== MAT LUC PLAY ==="); bc.Append(mat);
        Directory.CreateDirectory("Logs");
        File.WriteAllText(FILE_REPORT, bc.ToString());
        Debug.Log("[Kitchen V3] So sanh Edit vs Play: " + nDoi + " object bi doi, " + nThem + " sinh them, " + nMat + " mat. Bao cao: " + FILE_REPORT);
    }

    private static string Cat(string s)
    {
        if (s == null) return "";
        s = s.Replace("\n", "\\n");
        return s.Length > 60 ? s.Substring(0, 60) + "..." : s;
    }
}
