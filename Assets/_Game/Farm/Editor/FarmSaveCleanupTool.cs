#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DỌN DẸP DỮ LIỆU ĐÃ LƯU — công cụ cho DEV.
/// Menu: Tools ▸ Farm ▸ Dọn Dẹp Dữ Liệu Đã Lưu
///
/// ══ VẤN ĐỀ NÓ GIẢI QUYẾT ══
/// Công trình mua trong Play Mode KHÔNG nằm trên Hierarchy của scene. Nó được
/// `PlacementManager.LoadBuildings()` sinh ra mỗi lần Play, đọc từ PlayerPrefs.
/// Vì vậy:
///   • Xoá GameObject trong Play Mode → Play lại NÓ VẪN CÒN (save chưa đổi)
///   • Xoá trong Scene lúc Edit Mode → không tìm thấy, vì nó không có trong scene
/// Cách duy nhất để xoá hẳn là **sửa save**. Tool này làm việc đó.
///
/// ══ KHÁC GÌ `PlacedObjectsManagerTool` CŨ ══
///   • Hiện TÊN THẬT (“Chậu Hoa1”) thay vì `itemId` dạng số (“103”)
///   • Bao cả `FARM_CONSTRUCTION_SITES` (công trường đang xây) — tool cũ không có
///   • Dò và xoá được dữ liệu Ô ĐẤT mồ côi (`PLOT_NORMAL_*` / `PLOT_RARE_*`)
///   • Có ô tìm kiếm, chọn nhiều, xoá theo loại
///   • Chạy được cả trong Play Mode (có cảnh báo rõ phải Play lại mới thấy)
/// </summary>
public class FarmSaveCleanupTool : EditorWindow
{
    // Các key PHẢI khớp hằng số trong code runtime.
    private const string KeyBuildings = "FARM_PLACED_BUILDINGS";   // PlacementManager.BuildingsSaveKey
    private const string KeySites     = "FARM_CONSTRUCTION_SITES"; // ConstructionManager.SaveKey

    // ── Cấu trúc save (phải khớp field, KHÔNG được thiếu) ────────────────────
    // JsonUtility bỏ qua field lạ lúc ĐỌC nhưng KHÔNG giữ lại lúc GHI.
    // Thiếu `rot` là xoá 1 vật sẽ làm MẤT HƯỚNG XOAY của tất cả vật còn lại.
    [Serializable]
    private class BEntry { public string itemId; public float x, y; public int plotId; public int rot; }
    [Serializable]
    private class BSave  { public int saveVersion; public List<BEntry> list = new List<BEntry>(); }

    [Serializable]
    private class SEntry
    {
        public string itemId; public float x, y; public int rot; public int plotId;
        public long startUnix; public float duration;
    }
    [Serializable]
    private class SSave { public int saveVersion; public List<SEntry> list = new List<SEntry>(); }

    // ── Dòng hiển thị ────────────────────────────────────────────────────────
    private class Row
    {
        public int      index;
        public string   itemId;
        public string   tenThat;      // tên đọc được, tra từ asset
        public Vector2  pos;
        public int      plotId;
        public int      rot;
        public bool     chon;
        public string   ghiChu;       // "đang xây — còn 42s" ...
        public bool     laCongTruong;
    }

    private BSave _b;
    private SSave _s;
    private readonly List<Row> _rows = new List<Row>();
    private Dictionary<string, string> _tenTheoId;   // itemId → itemName

    private string  _timKiem = "";
    private Vector2 _scroll;
    private bool    _hienODatMoCoi;
    private List<string> _plotKeysMoCoi = new List<string>();

    // ═════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Farm/Dọn Dẹp Dữ Liệu Đã Lưu", false, 40)]
    public static void Open()
    {
        var w = GetWindow<FarmSaveCleanupTool>("Dọn Dẹp Save");
        w.minSize = new Vector2(680f, 460f);
        w.TaiLai();
        w.Show();
    }

    private void OnEnable() => TaiLai();

    // ═════════════════════════════════════════════════════════════════════════
    // ĐỌC
    // ═════════════════════════════════════════════════════════════════════════

    private void TaiLai()
    {
        XayBangTen();

        _b = DocJson<BSave>(KeyBuildings) ?? new BSave();
        _s = DocJson<SSave>(KeySites)     ?? new SSave();
        if (_b.list == null) _b.list = new List<BEntry>();
        if (_s.list == null) _s.list = new List<SEntry>();

        _rows.Clear();

        for (int i = 0; i < _b.list.Count; i++)
        {
            var e = _b.list[i];
            _rows.Add(new Row
            {
                index   = i,
                itemId  = e.itemId,
                tenThat = TraTen(e.itemId),
                pos     = new Vector2(e.x, e.y),
                plotId  = e.plotId,
                rot     = e.rot,
                laCongTruong = false
            });
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        for (int i = 0; i < _s.list.Count; i++)
        {
            var e = _s.list[i];
            float conLai = e.duration - (now - e.startUnix);
            _rows.Add(new Row
            {
                index   = i,
                itemId  = e.itemId,
                tenThat = TraTen(e.itemId),
                pos     = new Vector2(e.x, e.y),
                plotId  = e.plotId,
                rot     = e.rot,
                laCongTruong = true,
                ghiChu  = conLai > 0f ? $"đang xây — còn {Mathf.CeilToInt(conLai)}s"
                                      : "đã xong, chờ nhận"
            });
        }

        DoOdatMoCoi();
    }

    /// <summary>
    /// Tra tên đọc được từ mọi asset PlaceableItemData trong project.
    ///
    /// 🔴 VÌ SAO PHẢI QUÉT CẢ 3 TÊN LỚP: trong dự án này KHÔNG có asset nào dùng trực tiếp
    /// `PlaceableItemData` — 18 asset là `BuildingData`, 15 asset là `DecorData`. Việc
    /// `AssetDatabase.FindAssets("t:...")` có match LỚP CON hay không tuỳ phiên bản Unity,
    /// nên nếu chỉ tra tên lớp cha thì có nguy cơ bảng tên RỖNG SẠCH → mọi dòng hiện
    /// "(id lạ: 12)" và tool mất hết công dụng. Gọi riêng từng tên lớp là cách chắc chắn.
    /// </summary>
    private void XayBangTen()
    {
        _tenTheoId = new Dictionary<string, string>();

        // Bao cả lớp cha (nếu sau này có asset tạo trực tiếp) và cả hai lớp con hiện có.
        string[] tenLop = { "PlaceableItemData", "BuildingData", "DecorData" };
        var daXet = new HashSet<string>();

        foreach (string lop in tenLop)
        foreach (string guid in AssetDatabase.FindAssets("t:" + lop))
        {
            if (!daXet.Add(guid)) continue;      // 3 lượt quét có thể trùng nhau

            var d = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(
                        AssetDatabase.GUIDToAssetPath(guid));
            if (d == null || string.IsNullOrEmpty(d.itemID)) continue;

            // Ghép CẢ HAI tên: itemName ("Chậu Đất") và tên file asset ("Chậu Hoa1").
            // Chúng khác nhau ở nhiều asset, mà người dùng thường nhớ tên file — gộp lại
            // thì gõ kiểu nào ô Tìm cũng ra.
            string ten = !string.IsNullOrEmpty(d.itemName) ? d.itemName : d.name;
            if (!string.IsNullOrEmpty(d.name) && d.name != ten) ten += $"  ({d.name})";

            // itemID trùng nhau (dự án đang có 2 asset cùng id 104) sẽ khiến bảng tra
            // GHI ĐÈ lẫn nhau → dòng hiện sai tên. Báo ra thay vì âm thầm nuốt.
            if (_tenTheoId.TryGetValue(d.itemID, out string cu) && cu != ten)
                Debug.LogWarning($"[DọnSave] itemID '{d.itemID}' bị DÙNG TRÙNG bởi \"{cu}\" " +
                                 $"và \"{ten}\". Dòng trong bảng có thể hiện sai tên — " +
                                 "hãy dựa vào cột Vị trí, hoặc nút Soi, để chắc chắn.");

            _tenTheoId[d.itemID] = ten;
        }
    }

    private string TraTen(string id)
    {
        if (string.IsNullOrEmpty(id)) return "(không có id)";
        if (_tenTheoId == null) return id;   // lưới an toàn nếu OnGUI chạy trước TaiLai
        return _tenTheoId.TryGetValue(id, out string ten) ? ten : $"(id lạ: {id})";
    }

    /// <summary>
    /// Dò key dữ liệu Ô ĐẤT không còn công trình nào dùng.
    ///
    /// VÌ SAO QUAN TRỌNG: `PlotController` lưu cây trồng theo `PLOT_NORMAL_{id}`.
    /// `GetNextPlotId()` cấp id = max+1, nên nếu xoá ô đất có id cao nhất thì id đó
    /// được CẤP LẠI cho ô mới → ô mới thừa hưởng cây trồng của ô cũ. Xoá key mồ côi
    /// là cách chặn chuyện đó.
    /// </summary>
    private void DoOdatMoCoi()
    {
        _plotKeysMoCoi.Clear();

        var idDangDung = new HashSet<int>();
        foreach (var e in _b.list) if (e.plotId > 0) idDangDung.Add(e.plotId);
        foreach (var e in _s.list) if (e.plotId > 0) idDangDung.Add(e.plotId);

        // PlayerPrefs không cho liệt kê key → phải quét theo dải id hợp lý.
        const int QUET_TOI = 400;
        foreach (string mau in new[] { "PLOT_NORMAL_", "PLOT_RARE_" })
        {
            for (int id = 1; id <= QUET_TOI; id++)
            {
                string k = mau + id;
                if (!PlayerPrefs.HasKey(k)) continue;
                if (idDangDung.Contains(id)) continue;
                _plotKeysMoCoi.Add(k);
            }
        }
    }

    private static T DocJson<T>(string key) where T : class
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return null;
        try   { return JsonUtility.FromJson<T>(json); }
        catch { return null; }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GHI
    // ═════════════════════════════════════════════════════════════════════════

    private void LuuBuildings()
    {
        // Cùng lý do như PlacedObjectsManagerTool: ghi lại saveVersion = 0 sẽ khiến
        // PlacementManager dịch toạ độ LẦN NỮA. Runtime luôn dịch + ghi v1 ở lần Play
        // đầu, nên tới lúc tool chạy thì giá trị đúng phải là CurrentSaveVersion.
        if (_b.saveVersion == 0 && _b.list.Count > 0)
            _b.saveVersion = PlacementManager.CurrentSaveVersion;

        PlayerPrefs.SetString(KeyBuildings, JsonUtility.ToJson(_b));
        PlayerPrefs.Save();
    }

    private void LuuSites()
    {
        PlayerPrefs.SetString(KeySites, JsonUtility.ToJson(_s));
        PlayerPrefs.Save();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // GUI
    // ═════════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("DỌN DẸP DỮ LIỆU ĐÃ LƯU", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Công trình mua trong Play Mode KHÔNG nằm trên Hierarchy — nó được sinh lại " +
            "từ save mỗi lần Play. Vì vậy xoá GameObject trong Play Mode là vô ích, " +
            "Play lại nó vẫn còn.\n\n" +
            "Tool này sửa thẳng vào save. Xoá ở đây là xoá HẲN.",
            MessageType.Info);

        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox(
                "⚠ ĐANG PLAY MODE. Xoá vẫn ghi được vào save, nhưng object trên màn hình " +
                "sẽ KHÔNG biến mất ngay — phải THOÁT Play rồi Play lại mới thấy.\n" +
                "Ngoài ra: nếu game tự lưu lúc thoát Play, nó có thể GHI ĐÈ thay đổi của bạn. " +
                "An toàn nhất là thoát Play trước khi dọn.",
                MessageType.Warning);

        // ── Thanh công cụ ────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("↻ Tải lại", GUILayout.Width(90), GUILayout.Height(24)))
                TaiLai();

            EditorGUILayout.LabelField("Tìm:", GUILayout.Width(30));
            _timKiem = EditorGUILayout.TextField(_timKiem, GUILayout.MinWidth(120));

            if (GUILayout.Button("Chọn hết", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                foreach (var r in _rows) if (LotLoc(r)) r.chon = true;
            if (GUILayout.Button("Bỏ chọn", EditorStyles.miniButtonRight, GUILayout.Width(70)))
                foreach (var r in _rows) r.chon = false;
        }

        // ── Thống kê ─────────────────────────────────────────────────────────
        int soCongTrinh  = _rows.Count(r => !r.laCongTruong);
        int soCongTruong = _rows.Count(r =>  r.laCongTruong);
        int soChon       = _rows.Count(r =>  r.chon);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(
            $"{soCongTrinh} công trình đã xây · {soCongTruong} công trường đang xây · " +
            $"{soChon} dòng được chọn", EditorStyles.miniBoldLabel);

        // ── Bảng ─────────────────────────────────────────────────────────────
        VeTieuDe();

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        var hienThi = _rows.Where(LotLoc).ToList();

        if (hienThi.Count == 0)
            EditorGUILayout.HelpBox(
                _rows.Count == 0 ? "Save trống — chưa có vật nào được đặt."
                                 : "Không có dòng nào khớp từ khoá tìm kiếm.",
                MessageType.None);

        foreach (var r in hienThi) VeDong(r);
        EditorGUILayout.EndScrollView();

        // ── Nút xoá ──────────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(soChon == 0))
        {
            GUI.backgroundColor = new Color(0.92f, 0.45f, 0.42f);
            if (GUILayout.Button($"XOÁ {soChon} DÒNG ĐÃ CHỌN", GUILayout.Height(30)))
                XoaDaChon();
            GUI.backgroundColor = Color.white;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Xoá HẾT công trình đã xây", EditorStyles.miniButtonLeft))
                XoaHet(KeyBuildings, "toàn bộ công trình đã xây");
            if (GUILayout.Button("Xoá HẾT công trường đang xây", EditorStyles.miniButtonRight))
                XoaHet(KeySites, "toàn bộ công trường đang xây");
        }

        VeOdatMoCoi();
    }

    private bool LotLoc(Row r)
    {
        if (string.IsNullOrWhiteSpace(_timKiem)) return true;
        string t = _timKiem.Trim().ToLowerInvariant();
        return r.tenThat.ToLowerInvariant().Contains(t)
            || (r.itemId != null && r.itemId.ToLowerInvariant().Contains(t));
    }

    private static void VeTieuDe()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("", GUILayout.Width(22));
            GUILayout.Label("Tên công trình", EditorStyles.miniBoldLabel, GUILayout.Width(250));
            GUILayout.Label("id",             EditorStyles.miniBoldLabel, GUILayout.Width(50));
            GUILayout.Label("Vị trí",         EditorStyles.miniBoldLabel, GUILayout.Width(130));
            GUILayout.Label("ô đất",          EditorStyles.miniBoldLabel, GUILayout.Width(50));
            GUILayout.Label("xoay",           EditorStyles.miniBoldLabel, GUILayout.Width(40));
            GUILayout.Label("Trạng thái",     EditorStyles.miniBoldLabel);
        }
    }

    private void VeDong(Row r)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            r.chon = EditorGUILayout.Toggle(r.chon, GUILayout.Width(22));

            Color cu = GUI.color;
            if (r.laCongTruong) GUI.color = new Color(1f, 0.85f, 0.55f);
            GUILayout.Label(r.tenThat, GUILayout.Width(250));
            GUI.color = cu;

            GUILayout.Label(r.itemId ?? "-", GUILayout.Width(50));
            GUILayout.Label($"({r.pos.x:0}, {r.pos.y:0})", GUILayout.Width(130));
            GUILayout.Label(r.plotId > 0 ? r.plotId.ToString() : "-", GUILayout.Width(50));
            GUILayout.Label((r.rot * 90) + "°", GUILayout.Width(40));

            GUILayout.Label(r.laCongTruong ? "🏗 " + r.ghiChu : "đã xây xong");

            // Trong Play Mode: cho phép chọn thẳng object trên màn hình để nhìn cho chắc.
            if (EditorApplication.isPlaying && GUILayout.Button("Soi", GUILayout.Width(44)))
                SoiObject(r);
        }
    }

    /// <summary>Tìm object gần vị trí đã lưu nhất rồi chọn nó — để chắc chắn xoá đúng cái.</summary>
    private static void SoiObject(Row r)
    {
        var all = UnityEngine.Object.FindObjectsByType<Transform>(
                      FindObjectsInactive.Include, FindObjectsSortMode.None);

        Transform gan = null;
        float minD = float.MaxValue;
        foreach (var t in all)
        {
            if (t.parent != null) continue;                 // chỉ xét object gốc
            float d = Vector2.Distance(t.position, r.pos);
            if (d < minD) { minD = d; gan = t; }
        }

        if (gan != null && minD < 250f)
        {
            Selection.activeGameObject = gan.gameObject;
            EditorGUIUtility.PingObject(gan.gameObject);
            Debug.Log($"[DọnSave] Gần nhất với ({r.pos.x:0},{r.pos.y:0}) là '{gan.name}' " +
                      $"— cách {minD:0} unit.");
        }
        else
        {
            Debug.LogWarning($"[DọnSave] Không thấy object nào gần ({r.pos.x:0},{r.pos.y:0}). " +
                             "Có thể nó chưa được spawn, hoặc toạ độ save đã lệch hệ.");
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // XOÁ
    // ═════════════════════════════════════════════════════════════════════════

    private void XoaDaChon()
    {
        var xoaCT  = _rows.Where(r => r.chon && !r.laCongTruong).Select(r => r.index).ToHashSet();
        var xoaCTr = _rows.Where(r => r.chon &&  r.laCongTruong).Select(r => r.index).ToHashSet();

        if (!EditorUtility.DisplayDialog("Xoá khỏi save?",
                $"Sẽ xoá {xoaCT.Count} công trình + {xoaCTr.Count} công trường khỏi save.\n\n" +
                "KHÔNG hoàn tác được (PlayerPrefs không có Undo).\n" +
                "Lần Play sau chúng sẽ không xuất hiện nữa.",
                "Xoá", "Huỷ")) return;

        if (xoaCT.Count > 0)
        {
            // Xoá theo index GIẢM DẦN, nếu không index sau sẽ trượt.
            var ds = xoaCT.OrderByDescending(i => i).ToList();
            foreach (int i in ds) if (i >= 0 && i < _b.list.Count) _b.list.RemoveAt(i);
            LuuBuildings();
        }

        if (xoaCTr.Count > 0)
        {
            var ds = xoaCTr.OrderByDescending(i => i).ToList();
            foreach (int i in ds) if (i >= 0 && i < _s.list.Count) _s.list.RemoveAt(i);
            LuuSites();
        }

        Debug.Log($"[DọnSave] Đã xoá {xoaCT.Count} công trình + {xoaCTr.Count} công trường. " +
                  $"Còn lại {_b.list.Count} + {_s.list.Count}. Play lại để thấy kết quả.");

        TaiLai();
        Repaint();
    }

    private void XoaHet(string key, string moTa)
    {
        if (!EditorUtility.DisplayDialog("Xoá hết?",
                $"Xoá {moTa} khỏi save?\n\nKhông hoàn tác được.", "Xoá hết", "Huỷ")) return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log($"[DọnSave] Đã xoá key '{key}' ({moTa}).");
        TaiLai();
        Repaint();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Ô ĐẤT MỒ CÔI
    // ═════════════════════════════════════════════════════════════════════════

    private void VeOdatMoCoi()
    {
        EditorGUILayout.Space(6);
        _hienODatMoCoi = EditorGUILayout.Foldout(_hienODatMoCoi,
            $"Dữ liệu ô đất mồ côi ({_plotKeysMoCoi.Count})", true);

        if (!_hienODatMoCoi) return;

        EditorGUILayout.HelpBox(
            "Ô đất lưu cây trồng riêng theo key PLOT_NORMAL_{id}. Khi bạn xoá một ô đất, " +
            "key này KHÔNG tự mất. Vì id mới được cấp bằng (id lớn nhất + 1), ô đất mới có thể " +
            "nhận lại đúng id đó và thừa hưởng cây trồng của ô cũ.\n" +
            "Xoá key mồ côi để tránh chuyện đó.",
            MessageType.None);

        if (_plotKeysMoCoi.Count == 0)
        {
            EditorGUILayout.LabelField("Không có key mồ côi nào.", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.LabelField(string.Join("  ·  ", _plotKeysMoCoi.Take(20)),
                                   EditorStyles.wordWrappedMiniLabel);
        if (_plotKeysMoCoi.Count > 20)
            EditorGUILayout.LabelField($"… và {_plotKeysMoCoi.Count - 20} key nữa",
                                       EditorStyles.miniLabel);

        GUI.backgroundColor = new Color(0.95f, 0.7f, 0.4f);
        if (GUILayout.Button($"Xoá {_plotKeysMoCoi.Count} key ô đất mồ côi", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("Xoá key mồ côi?",
                    $"Xoá {_plotKeysMoCoi.Count} key dữ liệu ô đất không còn ai dùng?\n\n" +
                    "Không hoàn tác được.", "Xoá", "Huỷ"))
            {
                foreach (string k in _plotKeysMoCoi) PlayerPrefs.DeleteKey(k);
                PlayerPrefs.Save();
                Debug.Log($"[DọnSave] Đã xoá {_plotKeysMoCoi.Count} key ô đất mồ côi.");
                TaiLai();
                Repaint();
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif
