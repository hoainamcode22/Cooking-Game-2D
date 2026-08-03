using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EDITOR TOOL — `Tools/Farm/Điền Thời Gian Xây` (N6).
///
/// VẤN ĐỀ: cả 33 asset `PlaceableItemData` đang có `buildTimeSeconds = 0`, mà theo hợp
/// đồng §3 thì 0 nghĩa là "hiện ngay, bỏ qua giai đoạn xây" → KHÔNG TEST ĐƯỢC giàn giáo,
/// đồng hồ, nút rush hay hiệu ứng hoàn thành.
///
/// CÁCH LÀM: gợi ý thời gian theo GIÁ (đắt = xây lâu, đúng quy ước Township), cho xem
/// trước cả bảng, sửa tay từng dòng được, rồi mới ÁP DỤNG. Không tự ghi đè âm thầm.
///
///     buildTime ≈ clamp( goldPrice / hệSố , min , max )  rồi làm tròn về bội số `bước`
///
/// Mặc định: hệ số 10, min 5 s, max 300 s, bước 5 s.
/// Item bán bằng kim cương (goldPrice = 0) thì quy đổi 1 💎 ≈ 20 🪙 để không ra 5 giây.
/// </summary>
public class ConstructionBuildTimeTool : EditorWindow
{
    private class Row
    {
        public PlaceableItemData Asset;
        public string  Path;
        public bool    Selected = true;
        public float   Current;
        public float   Suggested;
        public int     RushPreview;
    }

    private readonly List<Row> _rows = new List<Row>();

    // Tham số công thức
    private float _divisor = 10f;
    private float _minSec  = 5f;
    private float _maxSec  = 300f;
    private float _step    = 5f;
    private float _gemToGold = 20f;

    private Vector2 _scroll;
    private bool    _onlyZero = true;

    [MenuItem("Tools/Farm/Điền Thời Gian Xây")]
    public static void Open()
    {
        var win = GetWindow<ConstructionBuildTimeTool>(true, "Điền Thời Gian Xây", true);
        win.minSize = new Vector2(820f, 460f);
        win.Rescan();
        win.Show();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Rescan()
    {
        _rows.Clear();

        string[] guids = AssetDatabase.FindAssets("t:PlaceableItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (asset == null) continue;

            var row = new Row
            {
                Asset   = asset,
                Path    = path,
                Current = asset.buildTimeSeconds
            };
            row.Suggested = Suggest(asset);
            row.RushPreview = ConstructionManager.RushCostFor(row.Suggested);
            _rows.Add(row);
        }

        _rows.Sort((a, b) => string.CompareOrdinal(a.Asset.name, b.Asset.name));
        ApplySelectionFilter();
    }

    private float Suggest(PlaceableItemData data)
    {
        // Giá quy đổi: item chỉ bán bằng kim cương vẫn phải ra thời gian hợp lý.
        float price = data.goldPrice > 0
            ? data.goldPrice
            : data.diamondPrice * _gemToGold;

        float raw = price / Mathf.Max(0.01f, _divisor);
        float clamped = Mathf.Clamp(raw, _minSec, _maxSec);

        if (_step > 0.01f)
            clamped = Mathf.Round(clamped / _step) * _step;

        return Mathf.Clamp(clamped, _minSec, _maxSec);
    }

    private void RecomputeSuggestions()
    {
        foreach (Row r in _rows)
        {
            r.Suggested   = Suggest(r.Asset);
            r.RushPreview = ConstructionManager.RushCostFor(r.Suggested);
        }
    }

    private void ApplySelectionFilter()
    {
        foreach (Row r in _rows)
            r.Selected = !_onlyZero || Mathf.Approximately(r.Current, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Gợi ý thời gian xây theo giá:  buildTime = clamp(giá / hệ số, min, max), " +
            "làm tròn về bội số bước.\n" +
            "Xem kỹ cột GỢI Ý (sửa tay được) rồi mới bấm ÁP DỤNG. Chỉ dòng có tick mới bị ghi.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            _divisor   = EditorGUILayout.FloatField(new GUIContent("Hệ số chia giá", "giây = giá / hệ số"), _divisor, GUILayout.Width(220f));
            _minSec    = EditorGUILayout.FloatField("Tối thiểu (s)", _minSec, GUILayout.Width(180f));
            _maxSec    = EditorGUILayout.FloatField("Tối đa (s)", _maxSec, GUILayout.Width(180f));
            _step      = EditorGUILayout.FloatField("Bước làm tròn", _step, GUILayout.Width(180f));
            _gemToGold = EditorGUILayout.FloatField(new GUIContent("1 💎 = ? 🪙"), _gemToGold, GUILayout.Width(160f));
        }
        if (EditorGUI.EndChangeCheck())
            RecomputeSuggestions();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Quét lại", GUILayout.Width(120f))) Rescan();
            if (GUILayout.Button("Chọn tất cả", GUILayout.Width(120f)))
                foreach (Row r in _rows) r.Selected = true;
            if (GUILayout.Button("Bỏ chọn hết", GUILayout.Width(120f)))
                foreach (Row r in _rows) r.Selected = false;

            bool newOnlyZero = GUILayout.Toggle(_onlyZero, " Chỉ chọn dòng đang = 0", GUILayout.Width(210f));
            if (newOnlyZero != _onlyZero)
            {
                _onlyZero = newOnlyZero;
                ApplySelectionFilter();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{_rows.Count} asset", GUILayout.Width(80f));
        }

        EditorGUILayout.Space(4f);

        // ── Tiêu đề bảng ────────────────────────────────────────────────────
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(24f));
            GUILayout.Label("ASSET", EditorStyles.miniBoldLabel, GUILayout.Width(230f));
            GUILayout.Label("LOẠI", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            GUILayout.Label("GIÁ 🪙", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            GUILayout.Label("GIÁ 💎", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
            GUILayout.Label("HIỆN TẠI", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
            GUILayout.Label("GỢI Ý (s)", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            GUILayout.Label("HIỂN THỊ", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            GUILayout.Label("RUSH ~", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (Row r in _rows)
        {
            if (r.Asset == null) continue;

            using (new EditorGUILayout.HorizontalScope())
            {
                r.Selected = EditorGUILayout.Toggle(r.Selected, GUILayout.Width(24f));

                if (GUILayout.Button(r.Asset.name, EditorStyles.linkLabel, GUILayout.Width(230f)))
                {
                    Selection.activeObject = r.Asset;
                    EditorGUIUtility.PingObject(r.Asset);
                }

                GUILayout.Label(r.Asset.GetType().Name.Replace("Data", ""), GUILayout.Width(90f));
                GUILayout.Label(r.Asset.goldPrice.ToString(), GUILayout.Width(70f));
                GUILayout.Label(r.Asset.diamondPrice.ToString(), GUILayout.Width(70f));

                Color old = GUI.color;
                if (Mathf.Approximately(r.Current, 0f)) GUI.color = new Color(1f, 0.6f, 0.4f);
                GUILayout.Label(r.Current.ToString("0"), GUILayout.Width(80f));
                GUI.color = old;

                EditorGUI.BeginChangeCheck();
                r.Suggested = EditorGUILayout.FloatField(r.Suggested, GUILayout.Width(90f));
                if (EditorGUI.EndChangeCheck())
                    r.RushPreview = ConstructionManager.RushCostFor(r.Suggested);

                GUILayout.Label(ConstructionSiteUI.FormatTime(r.Suggested), GUILayout.Width(90f));
                GUILayout.Label(r.RushPreview.ToString(), GUILayout.Width(70f));
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6f);

        int selected = 0;
        foreach (Row r in _rows) if (r.Selected) selected++;

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            GUI.enabled = selected > 0;
            if (GUILayout.Button($"ÁP DỤNG cho {selected} asset", GUILayout.Width(240f), GUILayout.Height(30f)))
                Apply();
            GUI.enabled = true;

            if (GUILayout.Button("Đặt tất cả về 0 (tắt giai đoạn xây)", GUILayout.Width(260f), GUILayout.Height(30f)))
                ResetToZero();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void Apply()
    {
        int changed = 0;

        foreach (Row r in _rows)
        {
            if (!r.Selected || r.Asset == null) continue;
            if (Mathf.Approximately(r.Asset.buildTimeSeconds, r.Suggested)) continue;

            Undo.RecordObject(r.Asset, "Điền thời gian xây");
            r.Asset.buildTimeSeconds = Mathf.Max(0f, r.Suggested);
            EditorUtility.SetDirty(r.Asset);

            r.Current = r.Asset.buildTimeSeconds;
            changed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Điền Thời Gian Xây] Đã ghi {changed} asset. " +
                  "Bấm ✓ trong game giờ sẽ ra giàn giáo + đồng hồ đếm ngược.");
    }

    private void ResetToZero()
    {
        if (!EditorUtility.DisplayDialog(
                "Đặt tất cả về 0?",
                "Mọi công trình sẽ HIỆN NGAY khi bấm ✓, bỏ hẳn giai đoạn đang xây.\nTiếp tục?",
                "Đặt về 0", "Huỷ"))
            return;

        foreach (Row r in _rows)
        {
            if (r.Asset == null) continue;

            Undo.RecordObject(r.Asset, "Xoá thời gian xây");
            r.Asset.buildTimeSeconds = 0f;
            EditorUtility.SetDirty(r.Asset);
            r.Current = 0f;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
