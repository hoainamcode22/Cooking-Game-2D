using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DEV-1 · V2 — Suy kích thước Ô LƯỚI cho toàn bộ PlaceableItemData.
///
/// CÁCH LÀM: đo hộp bao các SpriteRenderer của prefabToBuild (bỏ qua bóng đổ /
/// thảm footprint / marker), rồi Ceil(size / PlacementManager.CELL).
/// Có BẢNG XEM TRƯỚC — không bao giờ ghi đè asset khi chưa bấm ÁP DỤNG.
///
/// VÌ SAO PHẢI CÓ TOOL: 33 asset chỉnh tay thì vừa lâu vừa dễ sai, mà sai gridSize
/// là sai luôn cả footprint, cả kiểm tra chồng lấn lẫn biên bản đồ.
///
/// Menu: Tools/Farm/Suy Kích Thước Ô Công Trình
/// </summary>
public class BuildingGridSizeTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData data;
        public string   assetPath;
        public string   typeName;
        public bool     hasPrefab;
        public Vector2  worldSize;      // hộp bao đo được (world unit)
        public Vector2  pivotOffset;    // lệch giữa tâm art và gốc prefab
        public Vector2Int current;
        public Vector2Int suggested;
        public bool     selected;
        public string   note = "";
        /// <summary>true = ghi chú này là VẤN ĐỀ CẦN SỬA (đỏ). false = chỉ là thông tin (xám).</summary>
        public bool     noteIsProblem;

        public bool Changed => current != suggested;
    }

    private readonly List<Row> rows = new();
    private Vector2 scroll;
    private bool onlyChanged = false;
    private bool includeDecor = true;
    private bool includeBuilding = true;

    [MenuItem("Tools/Farm/Suy Kích Thước Ô Công Trình")]
    public static void Open()
    {
        var w = GetWindow<BuildingGridSizeTool>(true, "Suy Kích Thước Ô Công Trình");
        w.minSize = new Vector2(900f, 460f);
        w.Scan();
        w.Show();
    }

    // ── QUÉT ─────────────────────────────────────────────────────────────────

    private void Scan()
    {
        rows.Clear();

        string[] guids = AssetDatabase.FindAssets("t:PlaceableItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (data == null) continue;

            var row = new Row
            {
                data      = data,
                assetPath = path,
                typeName  = data.GetType().Name,
                current   = data.gridSize,
                hasPrefab = data.prefabToBuild != null
            };

            if (!row.hasPrefab)
            {
                row.suggested = new Vector2Int(Mathf.Max(1, data.gridSize.x), Mathf.Max(1, data.gridSize.y));
                row.note = "THIẾU prefabToBuild — không đo được";
                row.noteIsProblem = true;
            }
            else
            {
                if (TryMeasure(data.prefabToBuild, out Bounds b))
                {
                    row.worldSize = new Vector2(b.size.x, b.size.y);
                    row.pivotOffset = new Vector2(
                        b.center.x - data.prefabToBuild.transform.position.x,
                        b.center.y - data.prefabToBuild.transform.position.y);

                    row.suggested = new Vector2Int(
                        Mathf.Max(1, Mathf.CeilToInt(b.size.x / PlacementManager.CELL - 0.02f)),
                        Mathf.Max(1, Mathf.CeilToInt(b.size.y / PlacementManager.CELL - 0.02f)));

                    // GHI CHÚ (KHÔNG PHẢI LỖI) — từ V7 PlacementManager tự bù độ lệch pivot:
                    // PivotOffsetOf() đo đúng con số này, AnchorToFootprintCenter() cộng vào
                    // trước mọi phép tính ô lưới, và thảm xanh cũng được kéo theo.
                    // Pivot ở ĐÁY sprite là ĐÚNG CHUẨN của dự án (chân nhà chạm điểm đặt),
                    // nên tuyệt đối đừng "sửa" pivot art vì thấy dòng này.
                    float half = PlacementManager.CELL * 0.5f;
                    if (Mathf.Abs(row.pivotOffset.x) > half || Mathf.Abs(row.pivotOffset.y) > half)
                    {
                        bool bottomPivot = row.pivotOffset.y > 0f && Mathf.Abs(row.pivotOffset.x) <= half;
                        row.note = bottomPivot
                            ? $"pivot ở đáy ({row.pivotOffset.x:F0},{row.pivotOffset.y:F0}) — đã tự bù"
                            : $"pivot lệch ({row.pivotOffset.x:F0},{row.pivotOffset.y:F0}) — đã tự bù";
                        row.noteIsProblem = false;
                    }
                }
                else
                {
                    row.suggested = new Vector2Int(Mathf.Max(1, data.gridSize.x), Mathf.Max(1, data.gridSize.y));
                    row.note = "prefab không có SpriteRenderer hợp lệ";
                    row.noteIsProblem = true;
                }
            }

            row.selected = row.Changed && row.hasPrefab;
            rows.Add(row);
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.assetPath, b.assetPath));
        Repaint();
    }

    /// <summary>
    /// Đo hộp bao visual của một prefab ASSET (không cần Instantiate vào scene).
    /// Renderer.bounds trả về rỗng với prefab asset nên phải tự tính từ
    /// sprite.bounds × lossyScale, đúng như PlacementManager làm lúc chạy.
    /// </summary>
    private static bool TryMeasure(GameObject prefabRoot, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (prefabRoot == null) return false;

        bool found = false;
        foreach (SpriteRenderer sr in prefabRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null || sr.sprite == null) continue;
            if (!IsVisualRenderer(sr.gameObject.name)) continue;

            // drawMode Sliced/Tiled dùng sr.size chứ không phải kích thước gốc của sprite.
            Vector2 localSize = sr.drawMode == SpriteDrawMode.Simple
                ? (Vector2)sr.sprite.bounds.size
                : sr.size;

            Vector3 scale = sr.transform.lossyScale;
            float w = Mathf.Abs(localSize.x * scale.x);
            float h = Mathf.Abs(localSize.y * scale.y);
            if (w <= 0.0001f || h <= 0.0001f) continue;

            // TransformPoint để tôn trọng cả pivot lệch tâm lẫn xoay của sprite con.
            Vector3 center = sr.transform.TransformPoint(sr.sprite.bounds.center);
            Bounds one = new Bounds(center, new Vector3(w, h, 0f));

            if (!found) { bounds = one; found = true; }
            else bounds.Encapsulate(one);
        }

        return found;
    }

    /// <summary>Bộ lọc tên GIỐNG HỆT PlacementManager.IsValidSourceVisualRenderer.</summary>
    private static bool IsVisualRenderer(string n)
    {
        if (string.IsNullOrEmpty(n)) return false;
        return !(n == "Selection_Ring" ||
                 n == "Grid_Footprint" ||
                 n.Contains("Footprint") ||
                 n.Contains("Shadow") ||
                 n.StartsWith("Marker_") ||
                 n.StartsWith("Arrow_") ||
                 n.StartsWith("Placement_") ||
                 n == "Designed_Placement_Frame" ||
                 n == "Lift_Arrow_Effect");
    }

    // ── GIAO DIỆN ────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            $"CELL = {PlacementManager.CELL} world unit (PlacementManager.CELL — nguồn sự thật duy nhất).\n" +
            "Công thức: gridSize = Ceil( kích thước hộp bao prefab / CELL ), tối thiểu 1×1.\n" +
            "Kiểm tra bảng bên dưới rồi mới bấm ÁP DỤNG. Có thể sửa tay cột 'Suy ra' trước khi áp dụng.\n" +
            "Ghi chú XÁM (vd \"pivot ở đáy … — đã tự bù\") là BÌNH THƯỜNG, không phải lỗi: " +
            "pivot ở đáy sprite đúng chuẩn dự án và PlacementManager đã tự cộng bù. " +
            "Chỉ ghi chú ĐỎ mới cần xử lý.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Quét lại", GUILayout.Width(90f))) Scan();

            GUILayout.Space(10f);
            includeBuilding = GUILayout.Toggle(includeBuilding, "BuildingData", EditorStyles.miniButtonLeft, GUILayout.Width(110f));
            includeDecor    = GUILayout.Toggle(includeDecor,    "DecorData",    EditorStyles.miniButtonRight, GUILayout.Width(110f));
            GUILayout.Space(10f);
            onlyChanged = GUILayout.Toggle(onlyChanged, "Chỉ hiện dòng thay đổi", GUILayout.Width(180f));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Chọn hết thay đổi", GUILayout.Width(150f)))
                foreach (var r in rows) r.selected = r.Changed && r.hasPrefab;
            if (GUILayout.Button("Bỏ chọn hết", GUILayout.Width(110f)))
                foreach (var r in rows) r.selected = false;
        }

        EditorGUILayout.Space(4f);
        DrawHeader();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        int shown = 0, changed = 0, selected = 0;
        foreach (Row r in rows)
        {
            if (!PassesFilter(r)) continue;
            if (r.Changed) changed++;
            if (r.selected) selected++;
            if (onlyChanged && !r.Changed) continue;
            shown++;
            DrawRow(r);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"Hiện {shown} dòng · {changed} dòng khác giá trị hiện tại · {selected} dòng được chọn");

        using (new EditorGUI.DisabledScope(selected == 0))
        {
            GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f);
            if (GUILayout.Button($"ÁP DỤNG cho {selected} asset đã chọn", GUILayout.Height(30f)))
                Apply();
            GUI.backgroundColor = Color.white;
        }
    }

    private bool PassesFilter(Row r)
    {
        bool isDecor = r.typeName == "DecorData";
        if (isDecor && !includeDecor) return false;
        if (!isDecor && !includeBuilding) return false;
        return true;
    }

    private static void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(22f));
            GUILayout.Label("Asset", EditorStyles.miniBoldLabel, GUILayout.Width(190f));
            GUILayout.Label("Loại", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            GUILayout.Label("Bounds (WU)", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            GUILayout.Label("Hiện tại", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
            GUILayout.Label("Suy ra", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            GUILayout.Label("Ghi chú", EditorStyles.miniBoldLabel);
        }
    }

    private void DrawRow(Row r)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!r.hasPrefab))
                r.selected = EditorGUILayout.Toggle(r.selected, GUILayout.Width(22f));

            if (GUILayout.Button(r.data.name, EditorStyles.miniButton, GUILayout.Width(190f)))
                EditorGUIUtility.PingObject(r.data);

            GUILayout.Label(r.typeName, GUILayout.Width(90f));
            GUILayout.Label(r.hasPrefab ? $"{r.worldSize.x:F0} × {r.worldSize.y:F0}" : "—", GUILayout.Width(120f));

            Color old = GUI.color;
            if (r.Changed) GUI.color = new Color(1f, 0.82f, 0.4f);
            GUILayout.Label($"{r.current.x}×{r.current.y}", GUILayout.Width(80f));
            GUI.color = old;

            // Cho phép sửa tay trước khi áp dụng — designer là người chốt cuối.
            r.suggested.x = Mathf.Max(1, EditorGUILayout.IntField(r.suggested.x, GUILayout.Width(45f)));
            GUILayout.Label("×", GUILayout.Width(12f));
            r.suggested.y = Mathf.Max(1, EditorGUILayout.IntField(r.suggested.y, GUILayout.Width(45f)));

            if (!string.IsNullOrEmpty(r.note))
            {
                // Đỏ = phải sửa. Xám = chỉ là thông tin (vd pivot ở đáy — hệ thống đã tự bù).
                GUI.color = r.noteIsProblem ? new Color(1f, 0.6f, 0.5f)
                                            : new Color(0.62f, 0.62f, 0.62f);
                GUILayout.Label(r.note);
                GUI.color = old;
            }
            else GUILayout.Label("");
        }
    }

    // ── ÁP DỤNG ──────────────────────────────────────────────────────────────

    private void Apply()
    {
        int n = 0;
        foreach (Row r in rows)
        {
            if (!r.selected || r.data == null) continue;
            if (r.data.gridSize == r.suggested) continue;

            Undo.RecordObject(r.data, "Suy kích thước ô công trình");
            r.data.gridSize = r.suggested;
            EditorUtility.SetDirty(r.data);
            r.current = r.suggested;
            r.selected = false;
            n++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BuildingGridSizeTool] Đã cập nhật gridSize cho {n} asset (CELL = {PlacementManager.CELL}).");
        Repaint();
    }
}
