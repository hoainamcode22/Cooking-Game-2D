// ============================================================================
//  Edric Tools > Mở bảng công cụ   (2026-09-25)
//  Sep: "Tools > Farm Game nhieu tool qua tim met". Bang nay gom tool theo NHOM + O TIM + SAO YEU THICH.
//  - Tu quet moi [MenuItem] trong project (khong can sua tool cu), bam nut = chay dung menu do.
//  - Nhom: Dung cu Riu/Keo/Bua, Quay hang, Collider om khit, Lam sang art, Cho, May xay, Bep, Nong dan...
//  - Bam ★ de ghim len dau (luu theo may).
//  - Keo tab bang nay dock canh Inspector la luc nao cung thay.
// ============================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EdricToolsHub : EditorWindow
{
    private const string KeyYeuThich = "EdricToolsHub_YeuThich";

    private static readonly (string ten, string khop, string moTa)[] NHOM =
    {
        ("Chuồng v3 (ngoài map + shop)", "Edric Tools/Chuong (Pen)/", "Lắp chuồng mới sân rộng, con vật đi trong sân / trả lại"),
        ("Bếp: lửa lò mới", "Edric Tools/Bep (Kitchen)/", "Mở SampleScene rồi bấm"),
        ("Nhà hàng v3 (bếp mở)", "Edric Tools/Nha hang (Restaurant)/", "Lắp nhà hàng mới + lửa bếp, lò pizza, bảng treo, khói"),
        ("Dụng cụ Rìu / Kéo / Búa", "Dung Cu (Riu Keo Bua)", "Chặt cây, cắt bụi, đập đá bằng dụng cụ mua ở Shop"),
        ("Quầy hàng", "Quay Hang (Stall)", "Lắp quầy mới + nông sản nảy / trả lại quầy cũ"),
        ("Collider ôm khít công trình", "Collider om khit", "Bấm trúng đúng hình công trình"),
        ("Làm sáng art (giống quầy hàng)", "Edric Tools/Art/", "Chọn ảnh PNG trong Project rồi bấm"),
        ("Chợ (Market)", "Cho (Market)", ""),
        ("Máy xay", "May Xay", ""),
        ("Bếp (Kitchen V3)", "Kitchen V3", ""),
        ("Nông dân", "Nong Dan", ""),
        ("Chế độ sửa (Edit Mode)", "Edit Mode", ""),
    };

    private string _tim = "";
    private Vector2 _cuon;
    private List<string> _tatCa;
    private HashSet<string> _yeuThich;
    private readonly Dictionary<string, bool> _mo = new Dictionary<string, bool>();

    [MenuItem("Edric Tools/Mở bảng công cụ", false, 0)]
    public static void Mo()
    {
        var w = GetWindow<EdricToolsHub>();
        w.titleContent = new GUIContent("Edric Tools");
        w.minSize = new Vector2(320, 300);
        w.Show();
    }

    private void OnEnable()
    {
        _tatCa = null;
        _yeuThich = new HashSet<string>(EditorPrefs.GetString(KeyYeuThich, "").Split('\n').Where(s => s.Length > 0));
    }

    private void Quet()
    {
        _tatCa = TypeCache.GetMethodsWithAttribute<MenuItem>()
            .SelectMany(m => m.GetCustomAttributes(typeof(MenuItem), false).Cast<MenuItem>())
            .Where(a => !a.validate && (a.menuItem.StartsWith("Tools/") || a.menuItem.StartsWith("Edric Tools/")))
            .Select(a => a.menuItem)
            .Where(p => p != "Edric Tools/Mở bảng công cụ")
            .Distinct().OrderBy(p => p).ToList();
    }

    private void OnGUI()
    {
        if (_tatCa == null) Quet();

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Tìm:", GUILayout.Width(32));
        _tim = EditorGUILayout.TextField(_tim);
        if (GUILayout.Button("Xóa", GUILayout.Width(40))) { _tim = ""; GUI.FocusControl(null); }
        if (GUILayout.Button("Quét lại", GUILayout.Width(64))) Quet();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Bấm nút = chạy tool. ★ = ghim lên đầu. Dock bảng này cạnh Inspector cho tiện.", MessageType.None);

        _cuon = EditorGUILayout.BeginScrollView(_cuon);

        if (!string.IsNullOrEmpty(_tim))
        {
            string t = BoDau(_tim.ToLowerInvariant());
            var kq = _tatCa.Where(p => BoDau(p.ToLowerInvariant()).Contains(t)).ToList();
            GUILayout.Label($"Kết quả ({kq.Count})", EditorStyles.boldLabel);
            foreach (var p in kq) Dong(p, true);
        }
        else
        {
            var yt = _tatCa.Where(_yeuThich.Contains).ToList();
            if (yt.Count > 0)
            {
                GUILayout.Label("★ Đã ghim", EditorStyles.boldLabel);
                foreach (var p in yt) Dong(p, true);
                EditorGUILayout.Space(6);
            }
            foreach (var n in NHOM)
            {
                var ds = _tatCa.Where(p => p.Contains(n.khop)).ToList();
                if (ds.Count == 0) continue;
                bool mo;
                if (!_mo.TryGetValue(n.ten, out mo)) mo = n.khop.Contains("Chuong (Pen)") || n.khop.Contains("Nha hang") || n.khop.Contains("Bep (Kitchen)") || n.khop.Contains("Riu") || n.khop.Contains("Stall") || n.khop.Contains("om khit") || n.khop.Contains("Art");
                mo = EditorGUILayout.Foldout(mo, $"{n.ten}  ({ds.Count})", true, EditorStyles.foldoutHeader);
                _mo[n.ten] = mo;
                if (!mo) continue;
                if (!string.IsNullOrEmpty(n.moTa)) EditorGUILayout.LabelField(n.moTa, EditorStyles.miniLabel);
                foreach (var p in ds) Dong(p, false);
                EditorGUILayout.Space(4);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Dong(string path, bool hienDuong)
    {
        EditorGUILayout.BeginHorizontal();
        bool ghim = _yeuThich.Contains(path);
        if (GUILayout.Button(ghim ? "★" : "☆", GUILayout.Width(24)))
        {
            if (ghim) _yeuThich.Remove(path); else _yeuThich.Add(path);
            EditorPrefs.SetString(KeyYeuThich, string.Join("\n", _yeuThich));
        }
        string ten = path.Substring(path.LastIndexOf('/') + 1);
        string nhan = hienDuong ? path.Replace("Tools/Farm Game/", "").Replace("Tools/", "") : ten;
        if (GUILayout.Button(new GUIContent(nhan, path), EditorStyles.miniButtonLeft))
        {
            if (!EditorApplication.ExecuteMenuItem(path)) Debug.LogWarning("[EdricTools] Khong chay duoc menu: " + path);
        }
        EditorGUILayout.EndHorizontal();
    }

    private static string BoDau(string s)
    {
        var n = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(n.Length);
        foreach (var ch in n)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().Replace('đ', 'd');
    }
}
