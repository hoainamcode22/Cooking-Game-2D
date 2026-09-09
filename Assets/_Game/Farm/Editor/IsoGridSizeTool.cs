#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools/Map45/8. Suy Kich Thuoc O — het ho / het chong de giua cong trinh.
///
/// ┌── VI SAO PHAI SUA LAI (V12) ────────────────────────────────────────────┐
/// │ Ban truoc do bounds bang cach lay sprite.bounds * transform.lossyScale, │
/// │ nhung lossyScale cua PREFAB ASSET (chua instantiate) KHONG tinh duoc     │
/// │ day du chuoi cha-con => nha Home_01 do ra sai, tool de xuat 1x1 trong    │
/// │ khi art rong 512 world (o iso chi 300) => hai nha dat canh nhau CHONG DE.│
/// │                                                                          │
/// │ Ban nay:                                                                 │
/// │   • Tu dung cay Transform tu file prefab, nhan scale don tu ROOT xuong.  │
/// │   • Lay be rong o THAT tu IsoGrid.CellWidth (300) chu khong hardcode 150.│
/// │   • Hop bao cua vung o N x M tren luoi iso: rong = (N+M) * CellWidth/2   │
/// │     => N + M = round(artWidth / (CellWidth/2)).                          │
/// │   • Chia N,M theo TI LE art thay vi luon chia doi, nen nha cao thi an     │
/// │     nhieu o theo chieu sau, chuong dai thi an nhieu o theo chieu ngang.  │
/// └──────────────────────────────────────────────────────────────────────────┘
///
/// Luon xem bang de xuat truoc, sua tay cot "Moi" neu can, roi moi Ap dung.
/// </summary>
public class IsoGridSizeTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData data;
        public Vector2Int oldSize;
        public Vector2Int newSize;
        public Vector2 artSize;      // world unit
        public bool apply = true;
    }

    private readonly List<Row> rows = new List<Row>();
    private Vector2 scroll;
    private float cellW = 300f;
    private bool squareBias = false;   // mac dinh: chia theo ti le art
    private bool onlyChanged = false;   // Sep muon thay HET, ke ca dong khong doi

    [MenuItem("Tools/Map45/8. Suy Kich Thuoc O — het ho giua cong trinh", false, 8)]
    public static void Open()
    {
        var w = GetWindow<IsoGridSizeTool>("Kich Thuoc O ISO");
        w.cellW = Mathf.Max(1f, IsoGrid.CellWidth);
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Do lai footprint theo O ISO THAT (mac dinh 300 x 150 world).\n" +
            "Muc tieu: vung o OM SAT art — dat canh nhau khong ho, khong chong de.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        cellW = EditorGUILayout.FloatField("Be rong o (world)", cellW);
        if (GUILayout.Button("Lay tu scene", GUILayout.Width(110)))
            cellW = Mathf.Max(1f, IsoGrid.CellWidth);
        EditorGUILayout.EndHorizontal();

        squareBias  = EditorGUILayout.Toggle(
            new GUIContent("Ep o vuong", "Bat = luon chia deu N=M. Tat = chia theo ti le art (khuyen dung)."),
            squareBias);
        onlyChanged = EditorGUILayout.Toggle(
            new GUIContent("Chi hien dong doi", "An bot cac cong trinh von da dung kich thuoc"),
            onlyChanged);

        EditorGUILayout.Space();
        if (GUILayout.Button("Quet toan bo cong trinh", GUILayout.Height(28))) Scan();
        if (rows.Count == 0) return;

        EditorGUILayout.Space();

        // bo dem: bao nhieu dong dang hien / tong so quet duoc
        int changed = 0, tiny = 0;
        foreach (var r in rows)
        {
            if (r.oldSize != r.newSize) changed++;
            if (r.artSize.x < cellW * 0.25f) tiny++;
        }
        EditorGUILayout.LabelField(
            $"Quet duoc {rows.Count} cong trinh + decor  ·  {changed} dong se doi" +
            (onlyChanged ? "  ·  (dang AN cac dong khong doi)" : ""),
            EditorStyles.boldLabel);
        if (tiny > 0)
            EditorGUILayout.HelpBox(
                $"{tiny} prefab do ra RAT NHO (< 1/4 o). Thuong do prefab de scale o " +
                "GameObject con — hay kiem tra lai truoc khi Ap dung.", MessageType.Warning);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Ap", GUILayout.Width(24));
        GUILayout.Label("Cong trinh", GUILayout.Width(160));
        GUILayout.Label("Art (world)", GUILayout.Width(120));
        GUILayout.Label("Cu", GUILayout.Width(50));
        GUILayout.Label("Moi", GUILayout.Width(110));
        GUILayout.Label("Sai lech");
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var r in rows)
        {
            if (onlyChanged && r.oldSize == r.newSize) continue;

            EditorGUILayout.BeginHorizontal();
            r.apply = EditorGUILayout.Toggle(r.apply, GUILayout.Width(24));
            EditorGUILayout.ObjectField(r.data, typeof(PlaceableItemData), false, GUILayout.Width(160));
            var cArt = GUI.color;
            if (r.artSize.x < cellW * 0.25f) GUI.color = new Color(1f, 0.55f, 0.45f);
            GUILayout.Label($"{r.artSize.x:0} x {r.artSize.y:0}", GUILayout.Width(120));
            GUI.color = cArt;

            var prev = GUI.color;
            GUI.color = (r.oldSize != r.newSize) ? new Color(1f, 0.75f, 0.35f) : prev;
            GUILayout.Label($"{r.oldSize.x}x{r.oldSize.y}", GUILayout.Width(50));
            GUI.color = prev;

            r.newSize = EditorGUILayout.Vector2IntField("", r.newSize, GUILayout.Width(110));

            float box = (r.newSize.x + r.newSize.y) * cellW * 0.5f;
            float diff = r.artSize.x - box;
            GUI.color = Mathf.Abs(diff) <= cellW * 0.25f ? new Color(0.55f, 1f, 0.55f)
                                                         : new Color(1f, 0.6f, 0.5f);
            GUILayout.Label($"{diff:+0;-0;0} world");
            GUI.color = prev;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Chon het")) foreach (var r in rows) r.apply = true;
        if (GUILayout.Button("Bo het"))   foreach (var r in rows) r.apply = false;
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

            Vector2 art = MeasurePrefab(data.prefabToBuild);
            // KHONG loai prefab nho nua — van liet ke de Sep thay va tu quyet dinh.
            // (ban truoc loai o day nen decor / plot bien mat khoi bang.)
            if (art.x <= 0.001f) continue;

            rows.Add(new Row
            {
                data = data,
                oldSize = data.gridSize,
                newSize = Estimate(art),
                artSize = art
            });
        }
        rows.Sort((a, b) => b.artSize.x.CompareTo(a.artSize.x));
        Debug.Log($"[IsoGridSize] Quet {rows.Count} cong trinh (o = {cellW:0} world).");
    }

    /// <summary>
    /// Hop bao cua N x M o iso: rong = (N+M) * W/2.
    /// => tong = N + M. Chia theo TI LE art (cao/rong) de giu dang cong trinh.
    /// </summary>
    private Vector2Int Estimate(Vector2 art)
    {
        float half = Mathf.Max(1f, cellW * 0.5f);
        int total = Mathf.Clamp(Mathf.RoundToInt(art.x / half), 2, 14);

        if (squareBias)
        {
            int a = Mathf.Max(1, total / 2);
            return new Vector2Int(a, Mathf.Max(1, total - a));
        }

        // art cao (nha) -> nhieu o theo chieu SAU (M); art be ngang (chuong dai) -> nhieu N
        float ratio = Mathf.Clamp(art.y / Mathf.Max(1f, art.x), 0.35f, 2.5f);
        int m = Mathf.Clamp(Mathf.RoundToInt(total * ratio / (1f + ratio)), 1, total - 1);
        int n = Mathf.Max(1, total - m);
        return new Vector2Int(n, m);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Do kich thuoc art THAT cua prefab (world unit), co nhan scale don tu root.
    /// Duyet cay bang Transform.parent nen dung ca khi sprite nam sau nhieu cap.
    /// </summary>
    private static Vector2 MeasurePrefab(GameObject prefab)
    {
        if (prefab == null) return Vector2.zero;

        var renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return Vector2.zero;

        float bestArea = 0f;
        Vector2 best = Vector2.zero;

        foreach (var r in renderers)
        {
            if (r == null || r.sprite == null) continue;
            if (IsHelperVisual(r.transform)) continue;   // bo tham nen / khung kit

            // scale don tu chinh no len den root cua prefab
            Vector2 scale = Vector2.one;
            var t = r.transform;
            int guard = 0;
            while (t != null && guard++ < 32)
            {
                scale.x *= Mathf.Abs(t.localScale.x);
                scale.y *= Mathf.Abs(t.localScale.y);
                if (t == prefab.transform) break;
                t = t.parent;
            }

            Vector3 s = r.sprite.bounds.size;   // da chia PPU san
            float w = s.x * scale.x;
            float h = s.y * scale.y;
            float area = w * h;
            if (area > bestArea) { bestArea = area; best = new Vector2(w, h); }
        }
        return best;
    }

    /// <summary>Bo qua cac sprite phu tro do bo kit sinh ra (tham nen, ngoac goc, chip keo).</summary>
    private static bool IsHelperVisual(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            string n = p.name;
            if (n.StartsWith("Kit_") || n.Contains("Footprint") || n.Contains("Grid_") ||
                n.Contains("Marker") || n.Contains("Shadow")) return true;
        }
        return false;
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
            $"Da cap nhat {n} cong trinh.\n\n" +
            "Buoc tiep: chay Tools/Farm/Bo Kit Dat Cong Trinh > muc 2 " +
            "de collider + tham nen khop kich thuoc moi.\n" +
            "⛔ DUNG bam muc 3 cua bo kit (no do theo cong thuc khac).", "OK");
    }
}
#endif
