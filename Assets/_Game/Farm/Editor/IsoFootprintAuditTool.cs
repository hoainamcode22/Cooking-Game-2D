#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ============================================================================
/// Tools/Map45/13. Soi So O + Thu Nho Decor
/// ============================================================================
///
/// VI SAO CO TOOL NAY
/// ------------------
/// Vong 14 da chua loi "plot/decor khong dat sat nhau" trong PlacementManager
/// (SizeForSpawned roi vao nhanh do bang RectFromWorldBounds -> 1 o thanh 3x3 o).
/// Tool nay de:
///   1. NHIN THAY bang so: moi mon do rong bao nhieu world unit, suy ra may o,
///      asset dang ghi may o — lech cho nao do ngay.
///   2. GHI SO O DUNG vao asset, de luc chay khong phai doan nua.
///   3. THU NHO DECOR: art decor rong ~280 unit trong khi mot o chi rong 300,
///      nen mon nao cung chiem gan tron mot o, nhin rat chat. Keo thanh truot
///      roi bam ap dung — tool sua localScale cua ROOT prefab, co Undo.
///
/// AN TOAN
/// -------
///   • Chi ghi gridSize cho asset dang de 1x1 (chua dien). Asset da dien tay
///     (2x1, 2x2, 5x3...) KHONG bi dung toi.
///   • Chi thu nho prefab cua mon duoc coi la DECOR (suy ra <= 1 o va khong co
///     script cong trinh). Nha / chuong / may khong bi dung toi.
///   • Moi thay doi deu qua Undo.RecordObject -> Ctrl+Z tra lai duoc.
///   • BuildingFootprintKit tu bu scale (`_goc.localScale = 1/lossyScale`) nen
///     thu nho prefab KHONG lam sai tham nen / khung 4 goc.
/// </summary>
public class IsoFootprintAuditTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData data;
        public GameObject prefab;
        public Vector2 artWorld;        // be ngang / cao cua art (world unit)
        public Vector2Int suggested;    // so o suy ra tu art
        public bool isDecor;
        public float curScale;
    }

    private readonly List<Row> rows = new List<Row>();
    private Vector2 scroll;
    private string status = "";
    private float decorScale = 0.65f;
    private bool onlyMismatch = false;

    [MenuItem("Tools/Map45/13. Soi So O + Thu Nho Decor", false, 13)]
    public static void Open() => GetWindow<IsoFootprintAuditTool>("So O & Decor");

    private void OnEnable() => Rescan();

    // ─────────────────────────────────────────────────────────────────────
    private void Rescan()
    {
        rows.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (d == null) continue;

            var r = new Row { data = d, prefab = d.prefabToBuild };
            if (r.prefab != null)
            {
                r.artWorld = MeasureArt(r.prefab);
                r.suggested = EstimateCells(r.artWorld);
                r.curScale = r.prefab.transform.localScale.x;
                // DECOR = suy ra 1x1 VA asset cung dang de 1x1 (khong phai cong trinh)
                r.isDecor = r.suggested.x <= 1 && r.suggested.y <= 1
                            && d.gridSize.x <= 1 && d.gridSize.y <= 1;
            }
            rows.Add(r);
        }
        rows.Sort((a, b) => string.Compare(a.data.name, b.data.name, System.StringComparison.Ordinal));
        status = $"Quet duoc {rows.Count} mon. O luoi hien tai = {IsoGrid.CellWidth:0} x {IsoGrid.CellHeight:0} world unit.";
        Repaint();
    }

    /// <summary>Do be ngang / cao art cua prefab (world unit), giong TryMeasurePrefabVisualBounds.</summary>
    private static Vector2 MeasureArt(GameObject prefab)
    {
        bool found = false;
        Bounds b = default;
        foreach (var sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            string n = sr.gameObject.name;
            if (n.StartsWith("Kit_") || n.Contains("Footprint") || n.Contains("Tham_") ||
                n.StartsWith("Vien_") || n.StartsWith("Ngoac_") || n.Contains("Shadow")) continue;

            Vector2 local = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)sr.sprite.bounds.size : sr.size;
            Vector3 s = sr.transform.lossyScale;
            float w = Mathf.Abs(local.x * s.x), h = Mathf.Abs(local.y * s.y);
            if (w <= 0.0001f || h <= 0.0001f) continue;

            var one = new Bounds(sr.transform.TransformPoint(sr.sprite.bounds.center) - prefab.transform.position,
                                 new Vector3(w, h, 0f));
            if (!found) { b = one; found = true; } else b.Encapsulate(one);
        }
        return found ? new Vector2(b.size.x, b.size.y) : Vector2.zero;
    }

    private static Vector2Int EstimateCells(Vector2 art)
    {
        if (art.x <= 0.001f) return Vector2Int.one;
        Vector2Int e = IsoGrid.EstimateSizeFromWorldSize(art);
        return new Vector2Int(Mathf.Clamp(e.x, 1, 14), Mathf.Clamp(e.y, 1, 14));
    }

    // ─────────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Cot 'asset' = so o dang ghi trong PlaceableItemData.\n" +
            "Cot 'suy ra' = so o tinh tu be ngang art: (N+M) = rong / (rong_o / 2).\n" +
            "Do lech o day chinh la thu lam do dat khong khit. Nha/chuong da dien tay " +
            "thi cu de nguyen, tool khong dung toi.",
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Quet lai", GUILayout.Height(24))) Rescan();
            onlyMismatch = EditorGUILayout.ToggleLeft("Chi hien mon lech", onlyMismatch, GUILayout.Width(140));
        }

        // ── Bang ────────────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"{"MON",-24}{"asset",8}{"suy ra",9}{"art rong x cao",20}{"scale",8}   loai",
            EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(260));
        int lech = 0, soDecor = 0;
        foreach (var r in rows)
        {
            bool mismatch = r.prefab != null &&
                            (r.data.gridSize.x != r.suggested.x || r.data.gridSize.y != r.suggested.y);
            if (mismatch) lech++;
            if (r.isDecor) soDecor++;
            if (onlyMismatch && !mismatch) continue;

            string a  = $"{r.data.gridSize.x}x{r.data.gridSize.y}";
            string s  = r.prefab != null ? $"{r.suggested.x}x{r.suggested.y}" : "-";
            string ar = r.prefab != null ? $"{r.artWorld.x:0} x {r.artWorld.y:0}" : "(khong co prefab)";
            string sc = r.prefab != null ? $"{r.curScale:0.##}" : "-";
            string kind = r.prefab == null ? "?" : (r.isDecor ? "decor" : "cong trinh");

            var old = GUI.color;
            if (mismatch) GUI.color = new Color(1f, 0.82f, 0.4f);
            EditorGUILayout.LabelField($"{Trim(r.data.name, 24),-24}{a,8}{s,9}{ar,20}{sc,8}   {kind}");
            GUI.color = old;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.LabelField($"Lech: {lech} mon  ·  Decor: {soDecor} mon", EditorStyles.miniLabel);

        // ── 1. Ghi so o ─────────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("1. Chot so o vao asset", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Chi ghi cho mon dang de 1x1 (chua dien). Mon da dien tay giu nguyen.",
            EditorStyles.miniLabel);
        if (GUILayout.Button("Ghi so o suy ra vao cac asset con de 1x1", GUILayout.Height(26)))
            WriteGridSizes();

        // ── 2. Thu nho decor ────────────────────────────────────────────
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Thu nho decor", EditorStyles.boldLabel);
        decorScale = EditorGUILayout.Slider("Ti le", decorScale, 0.3f, 1.2f);

        float mau = 280f;
        EditorGUILayout.LabelField(
            $"Vi du: decor rong {mau:0} -> {mau * decorScale:0} unit " +
            $"(= {mau * decorScale / IsoGrid.CellWidth * 100f:0}% be ngang mot o).",
            EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(1f, 0.85f, 0.6f);
        if (GUILayout.Button($"Ap dung ti le {decorScale:0.##} cho {CountDecor()} prefab decor", GUILayout.Height(26)))
            ApplyDecorScale();
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Tra decor ve ti le 1 (huy thu nho)"))
        {
            decorScale = 1f;
            ApplyDecorScale();
        }

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(status, MessageType.None);
        }
    }

    private static string Trim(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

    private int CountDecor()
    {
        int n = 0;
        foreach (var r in rows) if (r.isDecor) n++;
        return n;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void WriteGridSizes()
    {
        int n = 0;
        foreach (var r in rows)
        {
            if (r.prefab == null) continue;
            if (r.data.gridSize.x > 1 || r.data.gridSize.y > 1) continue;   // da dien tay
            if (r.suggested == r.data.gridSize) continue;

            Undo.RecordObject(r.data, "Ghi so o");
            r.data.gridSize = r.suggested;
            EditorUtility.SetDirty(r.data);
            n++;
        }
        AssetDatabase.SaveAssets();
        status = n > 0 ? $"Da ghi so o cho {n} asset." : "Khong co asset nao can ghi — tat ca da khop.";
        Debug.Log($"[SoO] {status}");
        Rescan();
    }

    private void ApplyDecorScale()
    {
        var list = new List<Row>();
        foreach (var r in rows) if (r.isDecor && r.prefab != null) list.Add(r);
        if (list.Count == 0) { status = "Khong tim thay prefab decor nao."; return; }

        int n = 0;
        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                EditorUtility.DisplayProgressBar("Thu nho decor", r.prefab.name, i / (float)list.Count);

                string path = AssetDatabase.GetAssetPath(r.prefab);
                if (string.IsNullOrEmpty(path)) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;

                // Giu nguyen ti le goc cua prefab: chi NHAN them he so, khong ghi de.
                // Vi vay bam nhieu lan khong lam no be dan mai — moi lan tinh lai tu
                // ti le CHUAN (luu trong _tiLeGoc theo guid).
                float goc = GocScaleOf(path, root.transform.localScale.x);
                root.transform.localScale = new Vector3(goc * decorScale, goc * decorScale, 1f);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                n++;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        status = $"Da dat ti le {decorScale:0.##} cho {n} prefab decor. " +
                 "Cong trinh da dat tren map se doi theo o lan Play ke tiep.";
        Debug.Log($"[SoO] {status}");
        Rescan();
    }

    // Ti le CHUAN cua tung prefab, ghi vao EditorPrefs de bam nhieu lan khong dồn nhau.
    private static float GocScaleOf(string path, float hienTai)
    {
        string key = "ISO_DECOR_BASESCALE_" + path;
        if (!EditorPrefs.HasKey(key)) EditorPrefs.SetFloat(key, hienTai);
        float g = EditorPrefs.GetFloat(key, hienTai);
        return Mathf.Abs(g) < 0.0001f ? hienTai : g;
    }
}
#endif
