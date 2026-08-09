using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SỬA SORTING LAYER CHẾT + BAKE Y-SORT CHO CÔNG TRÌNH
/// ════════════════════════════════════════════════════
/// Menu: Tools ▸ Farm ▸ Sửa Sorting Layer Chết
///
/// VẤN ĐỀ ĐANG CÓ TRONG SCN_Farm:
///   • 218/257 SpriteRenderer trỏ `m_SortingLayerID: 1669604809` — layer NÀY ĐÃ BỊ XOÁ.
///     TagManager chỉ còn: Bottom · Default · Objects · ObjectsFront · Foreground.
///     ID chết → Unity dồn hết về layer sâu nhất, tức NẰM DƯỚI "Objects".
///   • 222 renderer cùng `m_SortingOrder: 500`, và 416/418 object cùng z = 0
///     → thứ tự vẽ giữa chúng là NGẪU NHIÊN theo instance ID.
///   • `TransparencySortMode` = Default (không bật Custom Axis Y).
///
/// Hậu quả: công trình dán đè lên nhau lộn xộn, và mọi nhân vật đặt ở layer "Objects"
/// (như đầu bếp NV_CHEF) sẽ LUÔN vẽ trên công trình vì so sánh LAYER thắng so sánh ORDER.
///
/// TOOL NÀY LÀM 2 VIỆC (bật/tắt riêng):
///   A. Trỏ mọi sorting layer chết về một layer thật.
///   B. Bake Y-sort: order mới = order cũ − round(Y của GỐC công trình).
///      Giữ nguyên thứ tự các mảnh BÊN TRONG một công trình (vì chỉ trừ đi cùng một số),
///      nhưng cả công trình được xếp đúng theo độ sâu.
///
/// AN TOÀN: có bảng xem trước, có Undo (Ctrl+Z), không tự lưu scene.
/// </summary>
public class SortingLayerRepairTool : EditorWindow
{
    private const int ORPHAN_HINT = 1669604809;   // ID chết đã biết trong dự án này

    private string _targetLayer   = "Objects";
    private bool   _fixOrphan     = true;
    private bool   _bakeYSort     = true;
    private bool   _setCustomAxis = true;
    private bool   _onlyOrphan    = false;   // B chỉ áp cho renderer từng bị orphan

    private readonly List<Row> _rows = new List<Row>();
    private Vector2 _scroll;
    private bool    _scanned;

    private class Row
    {
        public SpriteRenderer sr;
        public GameObject     root;      // gốc công trình (để lấy Y chung)
        public int            oldLayerId;
        public string         oldLayerName;
        public int            oldOrder;
        public int            newOrder;
        public bool           orphan;
        public bool           selected = true;
    }

    [MenuItem("Tools/Farm/Sửa Sorting Layer Chết", false, 30)]
    public static void Open()
    {
        var w = GetWindow<SortingLayerRepairTool>(true, "Sửa Sorting Layer");
        w.minSize = new Vector2(720, 560);
        w.Show();
    }

    // ════════════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("SỬA SORTING LAYER CHẾT + BAKE Y-SORT", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "⛔ TOOL CHƯA HOÀN CHỈNH — ĐANG KHOÁ, ĐỪNG CHẠY LÊN SCENE THẬT.\n\n" +
            "QA đã tìm ra 4 lỗ hổng khiến nó có thể làm HỎNG sorting nặng hơn hiện tại:\n\n" +
            "1. Bỏ sót 10 SortingGroup cũng dùng layer chết (Tàu thủy, Taulua…). SortingGroup " +
            "GHI ĐÈ sorting của mọi con → sửa con là vô nghĩa, phải sửa chính group.\n\n" +
            "2. Layer chết còn nằm TRONG PREFAB ASSET (House_01..05, Pen_01..04, Chauhoa_1..4…). " +
            "Sửa scene sẽ tạo prefab override hàng loạt, và kéo prefab mới vào map là lại chết layer.\n\n" +
            "3. Bake mốc 500 sẽ CHÔN Player NV_01 sau mọi công trình — Player dùng YSortIso " +
            "mốc 0, lệch đúng 500 bậc.\n\n" +
            "4. Bấm ÁP DỤNG hai lần sẽ trừ toạ độ Y hai lần → sort vỡ.\n\n" +
            "Nút quét vẫn dùng được để XEM hiện trạng (chỉ đọc, không sửa gì).",
            MessageType.Error);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Việc cần làm", EditorStyles.boldLabel);

        _fixOrphan = EditorGUILayout.Toggle(
            new GUIContent("A. Sửa layer chết",
                "Trỏ mọi SpriteRenderer đang dùng sorting layer không tồn tại về layer thật."), _fixOrphan);

        using (new EditorGUI.DisabledScope(!_fixOrphan))
        {
            EditorGUI.indentLevel++;
            _targetLayer = DrawLayerPopup("Trỏ về layer", _targetLayer);
            EditorGUI.indentLevel--;
        }

        _bakeYSort = EditorGUILayout.Toggle(
            new GUIContent("B. Bake Y-sort theo độ sâu",
                "order mới = order cũ − round(Y của gốc công trình). Giữ nguyên thứ tự mảnh bên trong."),
            _bakeYSort);

        using (new EditorGUI.DisabledScope(!_bakeYSort))
        {
            EditorGUI.indentLevel++;
            _onlyOrphan = EditorGUILayout.Toggle(
                new GUIContent("Chỉ áp cho renderer bị orphan",
                    "An toàn hơn: không đụng những renderer vốn đã có layer đúng."), _onlyOrphan);
            EditorGUI.indentLevel--;
        }

        _setCustomAxis = EditorGUILayout.Toggle(
            new GUIContent("C. Bật Transparency Sort = Custom Axis (0,1,0)",
                "Cho Unity tự sắp sprite theo trục Y. Ghi vào Graphics Settings."), _setCustomAxis);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("QUÉT SCENE", GUILayout.Height(28))) Scan();

        if (!_scanned)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Bấm QUÉT SCENE để xem hiện trạng.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        // ── Thống kê ──
        int orphanCount = _rows.Count(r => r.orphan);
        int selCount    = _rows.Count(r => r.selected);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(
            $"Tổng {_rows.Count} SpriteRenderer · {orphanCount} dùng layer CHẾT · {selCount} dòng được chọn",
            EditorStyles.boldLabel);

        if (orphanCount > 0)
            EditorGUILayout.HelpBox(
                $"Tìm thấy {orphanCount} renderer trỏ vào sorting layer không tồn tại. " +
                "Đây là lý do thứ tự vẽ bị lộn xộn.", MessageType.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Chọn hết orphan", EditorStyles.miniButtonLeft))
                foreach (var r in _rows) r.selected = r.orphan;
            if (GUILayout.Button("Chọn tất cả", EditorStyles.miniButtonMid))
                foreach (var r in _rows) r.selected = true;
            if (GUILayout.Button("Bỏ chọn hết", EditorStyles.miniButtonRight))
                foreach (var r in _rows) r.selected = false;
        }

        DrawHeader();
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var r in _rows) DrawRow(r);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // KHOÁ CỨNG nút áp dụng cho tới khi 4 lỗ hổng ở HelpBox đầu cửa sổ được sửa.
        // Thà không sửa gì còn hơn làm sorting vỡ nặng hơn hiện tại.
        using (new EditorGUI.DisabledScope(true))
        {
            GUILayout.Button($"ÁP DỤNG cho {selCount} renderer  —  ĐANG KHOÁ", GUILayout.Height(32));
        }

        EditorGUILayout.LabelField("Nút bị khoá có chủ đích. Xem HelpBox đỏ ở đầu cửa sổ.",
                                   EditorStyles.miniLabel);
    }

    // ════════════════════════════════════════════════════════════════════
    private void Scan()
    {
        _rows.Clear();
        _scanned = true;

        var valid = new HashSet<int>(SortingLayer.layers.Select(l => l.id));

        var all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,
                                                          FindObjectsSortMode.None);
        foreach (var sr in all)
        {
            if (sr == null) continue;

            bool orphan = !valid.Contains(sr.sortingLayerID);
            GameObject root = FindBuildingRoot(sr.gameObject);

            var row = new Row
            {
                sr           = sr,
                root         = root,
                oldLayerId   = sr.sortingLayerID,
                oldLayerName = orphan ? $"CHẾT ({sr.sortingLayerID})" : sr.sortingLayerName,
                oldOrder     = sr.sortingOrder,
                orphan       = orphan
            };

            // order mới = order cũ − round(Y gốc). Trừ CÙNG một số cho mọi mảnh
            // của một công trình → thứ tự bên trong công trình giữ nguyên.
            int shift = Mathf.RoundToInt(root != null ? root.transform.position.y
                                                     : sr.transform.position.y);
            row.newOrder = ClampOrder(row.oldOrder - shift);
            row.selected = orphan;   // mặc định chỉ chọn dòng có vấn đề

            _rows.Add(row);
        }

        // Orphan lên đầu cho dễ nhìn
        _rows.Sort((a, b) => b.orphan.CompareTo(a.orphan));
    }

    /// <summary>
    /// Gốc của một công trình. Ưu tiên object có EditableBuilding (công trình đặt được),
    /// nếu không thì leo tới prefab root, cuối cùng là chính nó.
    /// VÌ SAO cần gốc: nếu lấy Y của TỪNG mảnh thì mái nhà và móng nhà sẽ sort
    /// đá nhau, công trình bị xé làm nhiều lớp.
    /// </summary>
    private static GameObject FindBuildingRoot(GameObject go)
    {
        var eb = go.GetComponentInParent<EditableBuilding>();
        if (eb != null) return eb.gameObject;

        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        if (prefabRoot != null) return prefabRoot;

        return go;
    }

    private static int ClampOrder(int v) => Mathf.Clamp(v, short.MinValue, short.MaxValue);

    // ════════════════════════════════════════════════════════════════════
    private void Apply()
    {
        var targets = _rows.Where(r => r.selected && r.sr != null).ToList();
        if (targets.Count == 0) return;

        Undo.RecordObjects(targets.Select(r => (Object)r.sr).ToArray(), "Sửa sorting layer");

        int fixedLayer = 0, bakedOrder = 0;
        int targetId = SortingLayer.NameToID(_targetLayer);
        bool targetExists = SortingLayer.layers.Any(l => l.name == _targetLayer);

        foreach (var r in targets)
        {
            if (_fixOrphan && r.orphan && targetExists)
            {
                r.sr.sortingLayerName = _targetLayer;
                fixedLayer++;
            }

            if (_bakeYSort && (!_onlyOrphan || r.orphan))
            {
                r.sr.sortingOrder = r.newOrder;
                bakedOrder++;
            }

            EditorUtility.SetDirty(r.sr);
        }

        if (_setCustomAxis) SetCustomAxis();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[SortingRepair] ✔ Sửa layer: {fixedLayer} · Bake order: {bakedOrder}" +
                  (_setCustomAxis ? " · Đã bật Custom Axis (0,1,0)" : "") +
                  $"\n   Layer đích: '{_targetLayer}'" +
                  (targetExists ? "" : "  ✘ LAYER NÀY KHÔNG TỒN TẠI — phần sửa layer bị bỏ qua!") +
                  "\n   Xem Scene, đúng thì Ctrl+S, sai thì Ctrl+Z.");

        Scan();   // quét lại để bảng phản ánh trạng thái mới
    }

    /// <summary>
    /// Bật Transparency Sort Mode = Custom Axis (0,1,0) trong Graphics Settings.
    /// Không có nó thì Unity sắp sprite theo khoảng cách tới camera — với game 2D
    /// mọi thứ z=0 nên thứ tự thành ngẫu nhiên.
    /// </summary>
    private static void SetCustomAxis()
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")
                                 .FirstOrDefault();
        if (asset == null)
        {
            Debug.LogWarning("[SortingRepair] Không mở được GraphicsSettings.asset — " +
                             "hãy tự đặt Edit ▸ Project Settings ▸ Graphics ▸ " +
                             "Transparency Sort Mode = Custom Axis, Axis = (0, 1, 0).");
            return;
        }

        var so   = new SerializedObject(asset);
        var mode = so.FindProperty("m_TransparencySortMode");
        var axis = so.FindProperty("m_TransparencySortAxis");

        if (mode == null || axis == null)
        {
            Debug.LogWarning("[SortingRepair] Không tìm thấy property TransparencySort — đặt tay giúp.");
            return;
        }

        mode.intValue        = 3;                        // 3 = CustomAxis
        axis.vector3Value    = new Vector3(0f, 1f, 0f);
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    // ════════════════════════════════════════════════════════════════════
    private static string DrawLayerPopup(string label, string current)
    {
        string[] names = SortingLayer.layers.Select(l => l.name).ToArray();
        int idx = Mathf.Max(0, System.Array.IndexOf(names, current));
        idx = EditorGUILayout.Popup(label, idx, names);
        return names.Length > 0 ? names[idx] : current;
    }

    private static void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(22));
            GUILayout.Label("Object",        EditorStyles.miniBoldLabel, GUILayout.Width(200));
            GUILayout.Label("Gốc công trình", EditorStyles.miniBoldLabel, GUILayout.Width(160));
            GUILayout.Label("Layer hiện tại", EditorStyles.miniBoldLabel, GUILayout.Width(130));
            GUILayout.Label("Order",         EditorStyles.miniBoldLabel, GUILayout.Width(60));
            GUILayout.Label("→ Order mới",   EditorStyles.miniBoldLabel, GUILayout.Width(80));
        }
    }

    private void DrawRow(Row r)
    {
        if (r.sr == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            r.selected = EditorGUILayout.Toggle(r.selected, GUILayout.Width(22));

            if (GUILayout.Button(r.sr.gameObject.name, EditorStyles.label, GUILayout.Width(200)))
            {
                Selection.activeGameObject = r.sr.gameObject;
                EditorGUIUtility.PingObject(r.sr.gameObject);
            }

            GUILayout.Label(r.root != null ? r.root.name : "—", GUILayout.Width(160));

            Color old = GUI.color;
            if (r.orphan) GUI.color = new Color(1f, 0.45f, 0.4f);
            GUILayout.Label(r.oldLayerName, GUILayout.Width(130));
            GUI.color = old;

            GUILayout.Label(r.oldOrder.ToString(), GUILayout.Width(60));

            if (_bakeYSort && r.newOrder != r.oldOrder) GUI.color = new Color(0.6f, 0.95f, 0.6f);
            GUILayout.Label(_bakeYSort ? r.newOrder.ToString() : "—", GUILayout.Width(80));
            GUI.color = old;
        }
    }
}
