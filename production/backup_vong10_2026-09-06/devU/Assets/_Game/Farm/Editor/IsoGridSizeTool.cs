#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Farm/Suy Kich Thuoc O theo LUOI ISO.
///
/// VI SAO CAN
/// ----------
/// Tool cu (BuildingGridSizeTool) do bounds roi Ceil theo o VUONG 100 =>
/// LUON LAM TRON LEN => cong trinh chiem nhieu o hon art that => dat canh nhau
/// bi ho gan mot o. Vi du House_01 art rong 312 world nhung an 4 o = 400 => ho 88.
///
/// Tool nay do lai theo O ISO (150 x 75 world) va LAM TRON GAN NHAT (Round) thay vi
/// Ceil, nen vung o bam sat art => hai cong trinh dat canh nhau se DINH VAO NHAU.
///
/// Luon xem bang de xuat truoc, chinh tay cot "Moi" neu can, roi moi bam Ap dung.
/// </summary>
public class IsoGridSizeTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData data;
        public Vector2Int oldSize;
        public Vector2Int newSize;
        public Vector2 worldSize;
        public bool apply = true;
    }

    private readonly List<Row> rows = new List<Row>();
    private Vector2 scroll;
    private float cellW = IsoGrid.FallbackCellWidth;
    private float cellH = IsoGrid.FallbackCellHeight;
    private bool  squareBias = true;

    [MenuItem("Tools/Map45/8. Suy Kich Thuoc O — het ho giua cong trinh", false, 8)]
    public static void Open() => GetWindow<IsoGridSizeTool>("Kich Thuoc O ISO");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Do lai footprint moi cong trinh theo O ISO va lam tron GAN NHAT " +
            "(khong lam tron len nhu tool cu) => cong trinh dat sat nhau khong con ho.",
            MessageType.Info);

        EditorGUILayout.Space();
        cellW = EditorGUILayout.FloatField("Chieu rong o (world)", cellW);
        cellH = EditorGUILayout.FloatField("Chieu cao o (world)", cellH);
        squareBias = EditorGUILayout.Toggle(
            new GUIContent("Uu tien o vuong", "Chia deu N va M thay vi doan theo chieu cao art"),
            squareBias);

        EditorGUILayout.Space();
        if (GUILayout.Button("Quet toan bo PlaceableItemData", GUILayout.Height(28))) Scan();

        if (rows.Count == 0) return;

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Ap", GUILayout.Width(24));
        GUILayout.Label("Cong trinh", GUILayout.Width(150));
        GUILayout.Label("Art (world)", GUILayout.Width(110));
        GUILayout.Label("Cu", GUILayout.Width(60));
        GUILayout.Label("Moi", GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        foreach (var r in rows)
        {
            EditorGUILayout.BeginHorizontal();
            r.apply = EditorGUILayout.Toggle(r.apply, GUILayout.Width(24));
            EditorGUILayout.ObjectField(r.data, typeof(PlaceableItemData), false, GUILayout.Width(150));
            GUILayout.Label($"{r.worldSize.x:0} x {r.worldSize.y:0}", GUILayout.Width(110));
            var old = GUI.color;
            GUI.color = (r.oldSize != r.newSize) ? new Color(1f, 0.8f, 0.4f) : old;
            GUILayout.Label($"{r.oldSize.x}x{r.oldSize.y}", GUILayout.Width(60));
            GUI.color = old;
            r.newSize = EditorGUILayout.Vector2IntField("", r.newSize, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Chon het"))  foreach (var r in rows) r.apply = true;
        if (GUILayout.Button("Bo het"))    foreach (var r in rows) r.apply = false;
        EditorGUILayout.EndHorizontal();

        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("AP DUNG vao asset", GUILayout.Height(32))) Apply();
        GUI.backgroundColor = Color.white;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Scan()
    {
        rows.Clear();
        var seen = new HashSet<string>();
        var guids = new List<string>();
        foreach (var filter in new[] { "t:PlaceableItemData", "t:BuildingData", "t:DecorData" })
            foreach (var g in AssetDatabase.FindAssets(filter))
                if (seen.Add(g)) guids.Add(g);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (data == null || data.prefabToBuild == null) continue;

            Vector2 world = MeasurePrefabWorldSize(data.prefabToBuild);
            if (world.sqrMagnitude <= 0.01f) continue;

            rows.Add(new Row
            {
                data = data,
                oldSize = data.gridSize,
                newSize = Estimate(world),
                worldSize = world
            });
        }
        rows.Sort((a, b) => string.Compare(a.data.name, b.data.name, System.StringComparison.Ordinal));
        Debug.Log($"[IsoGridSize] Quet duoc {rows.Count} cong trinh.");
    }

    /// <summary>
    /// Suy so o tu be rong art. Hop bao cua N x M o iso: rong = (N+M) * W/2.
    /// => tong = N + M = round(rong / (W/2)). Chia deu cho N va M (uu tien o vuong).
    /// </summary>
    private Vector2Int Estimate(Vector2 worldSize)
    {
        float half = Mathf.Max(1f, cellW * 0.5f);
        int total = Mathf.Max(2, Mathf.RoundToInt(worldSize.x / half));
        if (squareBias)
        {
            int n = Mathf.Max(1, total / 2);
            return new Vector2Int(n, Mathf.Max(1, total - n));
        }
        // doan theo ti le art: cao/rong
        float ratio = Mathf.Clamp(worldSize.y / Mathf.Max(1f, worldSize.x), 0.4f, 2.5f);
        int m = Mathf.Max(1, Mathf.RoundToInt(total * ratio / (1f + ratio)));
        return new Vector2Int(Mathf.Max(1, total - m), m);
    }

    private static Vector2 MeasurePrefabWorldSize(GameObject prefab)
    {
        var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return Vector2.zero;

        bool has = false;
        Bounds b = default;
        foreach (var r in renderers)
        {
            if (r.sprite == null) continue;
            // kich thuoc local * scale cua prefab root
            Vector3 size = r.sprite.bounds.size;
            Vector3 ls = r.transform.lossyScale;
            Bounds wb = new Bounds(r.transform.localPosition,
                                   new Vector3(size.x * ls.x, size.y * ls.y, 0f));
            if (!has) { b = wb; has = true; } else b.Encapsulate(wb);
        }
        if (!has) return Vector2.zero;
        Vector3 rootScale = prefab.transform.localScale;
        return new Vector2(Mathf.Abs(b.size.x * rootScale.x), Mathf.Abs(b.size.y * rootScale.y));
    }

    private void Apply()
    {
        int n = 0;
        foreach (var r in rows)
        {
            if (!r.apply || r.data == null) continue;
            if (r.data.gridSize == r.newSize) continue;
            Undo.RecordObject(r.data, "Doi gridSize theo luoi ISO");
            r.data.gridSize = r.newSize;
            EditorUtility.SetDirty(r.data);
            n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[IsoGridSize] Da cap nhat {n} asset.");
        EditorUtility.DisplayDialog("Kich Thuoc O ISO",
            $"Da cap nhat {n} cong trinh.\n\nNho chay lai Tools/Farm/Bo Kit Dat Cong Trinh " +
            "de collider + tham nen khop kich thuoc moi.", "OK");
    }
}
#endif
