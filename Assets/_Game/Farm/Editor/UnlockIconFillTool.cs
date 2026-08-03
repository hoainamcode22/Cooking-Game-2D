using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: <b>Tools/Farm/Điền Icon Unlock (Level Reward)</b>
///
/// Mục đích
/// --------
/// 31 asset <see cref="LevelRewardConfig"/> trong <c>data/Lever Game</c> chỉ có
/// <c>unlockDescriptions</c> (thuần chữ) nên popup lên cấp hiện các ô tròn TRẮNG.
/// Tool này khớp TỪ KHOÁ trong từng dòng mô tả với một Sprite THẬT của game,
/// rồi ghi vào <c>LevelRewardConfig.unlockEntries</c> (label + icon).
///
/// Quy trình: mở tool → xem BẢNG XEM TRƯỚC (khớp / KHÔNG khớp) → bấm ÁP DỤNG.
/// Tool KHÔNG bao giờ sửa <c>unlockDescriptions</c> và <c>giftItems</c>.
///
/// Nguồn icon (thứ tự ưu tiên trong bảng luật ở dưới)
/// --------------------------------------------------
///   • <see cref="CropData"/>            — nông sản, hạt giống, hoa   (field itemIcon)
///   • <see cref="PlaceableItemData"/>   — BuildingData + DecorData   (field itemIcon)
///   • InventoryItemData                 — thịt/trứng/sữa/món ăn      (field icon)
///   • Sprite art rời                    — tra theo TÊN FILE (không hardcode
///                                         đường dẫn, tránh lỗi Unicode thư mục)
///
/// LƯU Ý cho designer
/// ------------------
/// Nếu chạy lại <c>Tools/Farm Game/Setup Level Up Popup/Setup Reward Data</c>
/// thì <c>unlockDescriptions</c> bị ghi đè → chạy lại tool này để đồng bộ nhãn.
/// Bảng xem trước sẽ cảnh báo khi nhãn cũ trong unlockEntries đã lệch.
/// </summary>
public class UnlockIconFillTool : EditorWindow
{
    private const string MENU   = "Tools/Farm/Điền Icon Unlock (Level Reward)";
    private const string FOLDER = "Assets/_Game/Farm/data/Lever Game";

    // ─────────────────────────────────────────────────────────────────────────
    //  1. BẢNG LUẬT KHỚP TỪ KHOÁ → ICON
    //     Từ khoá viết dạng ĐÃ BỎ DẤU + chữ thường (ASCII) để khớp bền vững
    //     bất kể asset ghi "khóa" hay "khoá", "MỞ KHÓA" hay "Mở khóa".
    //     Khớp theo NGUYÊN TỪ (word boundary) nên "kho" không dính vào "mở khoá".
    //     Luật nào ĐỨNG TRƯỚC thì thắng → xếp từ CỤ THỂ đến CHUNG.
    // ─────────────────────────────────────────────────────────────────────────

    private enum Src
    {
        Crop,   // CropData.itemIcon                — key = itemName (bỏ dấu)
        Place,  // PlaceableItemData.itemIcon       — key = itemName (bỏ dấu)
        Inv,    // InventoryItemData.icon           — key = displayName (bỏ dấu)
        File,   // Sprite art rời                   — key = tên file (không phần mở rộng)
    }

    private class Rule
    {
        public readonly string keyword;   // đã bỏ dấu, chữ thường
        public readonly Src    src;
        public readonly string key;
        public readonly string prefer;    // gợi ý đường dẫn khi trùng tên file
        public readonly string note;      // ghi chú cho designer (icon tạm...)

        public Rule(string keyword, Src src, string key, string note = null, string prefer = null)
        {
            this.keyword = keyword; this.src = src; this.key = key;
            this.note = note; this.prefer = prefer;
        }
    }

    private static readonly Rule[] RULES =
    {
        // ── A. Công trình / hệ thống (cụ thể nhất) ───────────────────────────
        new Rule("nha bep",         Src.File,  "cooking"),
        new Rule("nha hang",        Src.File,  "cooking",  "tạm dùng icon nhà bếp — chưa có art nhà hàng ven biển"),
        new Rule("chuong bo sua",   Src.Place, "chuong bo sua"),
        new Rule("chuong bo",       Src.Place, "chuong bo"),
        new Rule("chuong ga",       Src.Place, "chuong ga"),
        new Rule("chuong heo",      Src.Place, "chuong heo"),
        new Rule("nha dan",         Src.Place, "nha dan 1"),
        new Rule("may xay bot",     Src.File,  "maylamthucangiasuc", "BuildingData 'Máy Xay Bột' chưa có itemIcon → dùng sprite máy chung"),
        new Rule("may ep mia",      Src.File,  "maylamthucangiasuc", "BuildingData 'Máy Ép Mía' chưa có itemIcon → dùng sprite máy chung"),
        new Rule("may pho mai",     Src.File,  "maylamthucangiasuc", "BuildingData 'Máy Phô Mai' chưa có itemIcon → dùng sprite máy chung"),
        new Rule("slot san xuat",   Src.File,  "maylamthucangiasuc"),
        new Rule("ho ca",           Src.Place, "ho da"),
        new Rule("ben tau",         Src.File,  "gataulua", "tạm dùng icon nhà ga tàu — chưa có art bến tàu du lịch"),
        new Rule("tau du lich",     Src.File,  "gataulua"),
        new Rule("don tau",         Src.File,  "taulua"),
        new Rule("mo rong dat",     Src.Place, "dat trong"),
        new Rule("nang cap kho",    Src.File,  "Sprite_Tiles_Warehouse", null, "AssetsTitl"),
        new Rule("kho",             Src.File,  "Sprite_Tiles_Warehouse", null, "AssetsTitl"),

        // ── B. Sản phẩm chế biến (phải đứng TRƯỚC nông sản: "nước mía" ≠ "mía") ──
        new Rule("bot gao",         Src.File,  "gaoicon",  "Item_BotGao chưa có icon → dùng icon gạo"),
        new Rule("nuoc mia",        Src.Inv,   "nuoc mia chanh"),
        new Rule("pho mai",         Src.File,  "suamilk",  "Item_PhoMai chưa có icon → TẠM dùng icon sữa, cần art phô mai"),

        // ── C. Nông sản & hoa (CropData) ─────────────────────────────────────
        new Rule("khoai tay",       Src.Crop,  "khoai tay"),
        new Rule("ca chua",         Src.Crop,  "ca chua"),
        new Rule("ca rot",          Src.Crop,  "ca rot"),
        new Rule("bap cai",         Src.Crop,  "bap cai"),
        new Rule("tulip",           Src.Crop,  "tulip"),
        new Rule("huong duong",     Src.Crop,  "huong duong"),
        new Rule("hoa hong",        Src.Crop,  "hoa hong"),
        new Rule("oai huong",       Src.Crop,  "hoa oai huong"),
        new Rule("hoa lan",         Src.Crop,  "hoa lan"),
        new Rule("cuc trang",       Src.Crop,  "hoa cuc trang"),
        new Rule("cuc van tho",     Src.Crop,  "hoa cuc van tho"),
        new Rule("mau don",         Src.Crop,  "hoa mau don"),
        new Rule("cam tu cau",      Src.Crop,  "hoa cam tu cau"),
        new Rule("anh thao",        Src.Crop,  "hoa anh thao"),
        new Rule("ngo",             Src.Crop,  "ngo"),
        new Rule("mia",             Src.Crop,  "mia"),
        new Rule("nam",             Src.Crop,  "nam"),
        new Rule("chanh",           Src.Crop,  "chanh"),
        new Rule("ot",              Src.Crop,  "ot"),
        new Rule("tieu",            Src.Crop,  "tieu"),
        new Rule("lua",             Src.Crop,  "lua"),
        new Rule("hoa",             Src.Crop,  "hoa hong", "'hoa' chung → dùng icon Hoa Hồng làm đại diện"),

        // ── D. Thịt / trứng / sữa / cá ───────────────────────────────────────
        new Rule("thit bo",         Src.Inv,   "thit bo"),
        new Rule("thit heo",        Src.Inv,   "thit heo"),
        new Rule("thit ga",         Src.Inv,   "thit ga"),
        new Rule("mon ca",          Src.Inv,   "ca nuong tieu"),
        new Rule("don ca",          Src.Inv,   "ca nuong tieu"),
        new Rule("loai ca",         Src.File,  "ca"),
        new Rule("cau ca",          Src.File,  "ca"),
        new Rule("trung",           Src.Inv,   "trung"),
        new Rule("sua",             Src.Inv,   "sua"),

        // ── E. Meta / hệ thống chung ─────────────────────────────────────────
        new Rule("nhiem vu",        Src.File,  "Lich"),
        new Rule("su kien",         Src.File,  "Lich"),
        new Rule("cong thuc",       Src.File,  "SachNauAn"),
        new Rule("trang tri",       Src.Place, "vong hoa"),
        new Rule("decor",           Src.Place, "vong hoa"),
        new Rule("pet",             Src.Place, "vit vui ve"),
        new Rule("cay trong",       Src.Crop,  "bap cai", "'cây trồng' chung → dùng icon Bắp Cải làm đại diện"),
        new Rule("cay",             Src.Crop,  "bap cai", "'cây' chung → dùng icon Bắp Cải làm đại diện"),
        new Rule("danh hieu",       Src.File,  "iconsao-removebg-preview"),
        new Rule("hoan thanh hanh trinh", Src.File, "iconsao-removebg-preview"),
        new Rule("toan bo pool",    Src.File,  "iconsao-removebg-preview"),
        new Rule("noi dung toi da", Src.File,  "iconsao-removebg-preview"),
        new Rule("don 2 mon",       Src.File,  "delivery"),
        new Rule("don combo",       Src.File,  "delivery"),
        new Rule("don hang",        Src.File,  "delivery"),
        new Rule("don",             Src.File,  "delivery"),
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  2. THƯ VIỆN ICON — quét từ AssetDatabase, KHÔNG hardcode đường dẫn
    // ─────────────────────────────────────────────────────────────────────────

    private class Found
    {
        public Sprite sprite;
        public string assetPath;   // nơi lấy sprite (để log)
    }

    private Dictionary<string, Found> _crop;
    private Dictionary<string, Found> _place;
    private Dictionary<string, Found> _inv;
    private Dictionary<string, Found> _file;   // cache theo tên file

    // ─────────────────────────────────────────────────────────────────────────
    //  3. KẾT QUẢ XEM TRƯỚC
    // ─────────────────────────────────────────────────────────────────────────

    private class Row
    {
        public string label;
        public Sprite icon;
        public string matchedKeyword;
        public string sourceInfo;
        public string note;
        public bool   labelChanged;   // unlockEntries cũ có nhãn khác
    }

    private class Block
    {
        public LevelRewardConfig cfg;
        public string            assetName;
        public List<Row>         rows = new List<Row>();
    }

    private List<Block> _blocks = new List<Block>();
    private Vector2     _scroll;
    private int         _totalRows, _missRows;
    private bool        _scanned;

    // ─────────────────────────────────────────────────────────────────────────
    //  4. MENU + GUI
    // ─────────────────────────────────────────────────────────────────────────

    [MenuItem(MENU)]
    public static void Open()
    {
        var w = GetWindow<UnlockIconFillTool>(true, "Điền Icon Unlock", true);
        w.minSize = new Vector2(760f, 520f);
        w.Scan();
        w.Show();
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Khớp từ khoá trong 'unlockDescriptions' với sprite thật của game rồi ghi vào " +
            "'unlockEntries'. KHÔNG sửa unlockDescriptions / giftItems.\n" +
            "Xem bảng dưới trước — chỉ bấm ÁP DỤNG khi thấy hợp lý.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("① Quét lại", GUILayout.Height(26f))) Scan();

            GUI.enabled = _scanned && _blocks.Count > 0;
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button($"② ÁP DỤNG vào {_blocks.Count} asset", GUILayout.Height(26f)))
                Apply();
            GUI.backgroundColor = old;
            GUI.enabled = true;

            if (GUILayout.Button("In log ra Console", GUILayout.Height(26f))) Debug.Log(BuildLog());
            if (GUILayout.Button("Đường dẫn XU/KIM CƯƠNG", GUILayout.Height(26f))) LogCurrencyIcons();
        }

        if (GUILayout.Button("⚠ Xoá sạch unlockEntries (rollback)")) ClearAll();

        if (!_scanned) { EditorGUILayout.LabelField("Chưa quét."); return; }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            $"Tổng {_totalRows} mục unlock — khớp {_totalRows - _missRows} ✔  ·  KHÔNG khớp {_missRows} ✘",
            EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var b in _blocks)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"■ {b.assetName}   (L{b.cfg.levelReached}) — {b.rows.Count} mục",
                                       EditorStyles.boldLabel);
            foreach (var r in b.rows)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    Rect box = GUILayoutUtility.GetRect(34f, 34f, GUILayout.Width(34f), GUILayout.Height(34f));
                    if (r.icon != null) DrawSprite(box, r.icon);
                    else EditorGUI.DrawRect(box, new Color(0.75f, 0.25f, 0.25f, 0.35f));

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(r.label, EditorStyles.wordWrappedLabel);
                        string line = r.icon != null
                            ? $"✔ '{r.matchedKeyword}' → {r.sourceInfo}"
                            : "✘ KHÔNG TÌM ĐƯỢC ICON — designer gán tay trong Inspector";
                        if (!string.IsNullOrEmpty(r.note)) line += $"   ⓘ {r.note}";
                        if (r.labelChanged) line += "   ⚠ nhãn cũ trong unlockEntries đã lệch, sẽ được cập nhật";
                        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static void DrawSprite(Rect r, Sprite s)
    {
        var tex = s.texture;
        if (tex == null) return;
        var tr = s.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height,
                          tr.width / tex.width, tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(r, tex, uv, true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  5. QUÉT + KHỚP
    // ─────────────────────────────────────────────────────────────────────────

    private void Scan()
    {
        BuildLibrary();

        _blocks = new List<Block>();
        _totalRows = _missRows = 0;

        foreach (var cfg in LoadConfigs())
        {
            var b = new Block { cfg = cfg, assetName = cfg.name };

            var descs = cfg.unlockDescriptions ?? new List<string>();
            for (int i = 0; i < descs.Count; i++)
            {
                string label = descs[i];
                if (string.IsNullOrWhiteSpace(label)) continue;

                var (icon, kw, src, note) = Match(label);

                bool changed = cfg.unlockEntries != null
                            && i < cfg.unlockEntries.Count
                            && cfg.unlockEntries[i] != null
                            && cfg.unlockEntries[i].label != label;

                b.rows.Add(new Row
                {
                    label = label, icon = icon, matchedKeyword = kw,
                    sourceInfo = src, note = note, labelChanged = changed
                });

                _totalRows++;
                if (icon == null) _missRows++;
            }

            _blocks.Add(b);
        }

        _scanned = true;
        Repaint();
    }

    private (Sprite icon, string kw, string src, string note) Match(string label)
    {
        string norm = Norm(label);

        foreach (var rule in RULES)
        {
            if (!ContainsWord(norm, rule.keyword)) continue;

            Found f = Resolve(rule);
            if (f == null || f.sprite == null)
                continue;   // luật khớp chữ nhưng thiếu sprite → thử luật sau

            return (f.sprite, rule.keyword, $"{rule.src}:{rule.key}  [{f.assetPath}]", rule.note);
        }
        return (null, null, null, null);
    }

    private Found Resolve(Rule rule)
    {
        switch (rule.src)
        {
            case Src.Crop:  return _crop.TryGetValue(rule.key,  out var a) ? a : null;
            case Src.Place: return _place.TryGetValue(rule.key, out var b) ? b : null;
            case Src.Inv:   return _inv.TryGetValue(rule.key,   out var c) ? c : null;
            case Src.File:  return ResolveFile(rule.key, rule.prefer);
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  6. DỰNG THƯ VIỆN
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildLibrary()
    {
        _crop  = new Dictionary<string, Found>();
        _place = new Dictionary<string, Found>();
        _inv   = new Dictionary<string, Found>();
        _file  = new Dictionary<string, Found>();

        // CropData — nông sản, hạt giống, hoa
        foreach (string guid in AssetDatabase.FindAssets("t:CropData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<CropData>(path);
            if (so == null || so.itemIcon == null) continue;
            Put(_crop, Norm(so.itemName), so.itemIcon, path);
        }

        // BuildingData + DecorData (đều kế thừa PlaceableItemData)
        foreach (string guid in AssetDatabase.FindAssets("t:PlaceableItemData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(path);
            if (so == null || so.itemIcon == null) continue;
            Put(_place, Norm(so.itemName), so.itemIcon, path);
        }

        // InventoryItemData — thịt/trứng/sữa/nguyên liệu/món ăn.
        // Dùng reflection để không phụ thuộc vào assembly định nghĩa class.
        const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject",
                     new[] { "Assets/_Game/Farm/data" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) continue;

            var t = so.GetType();
            if (t.Name != "InventoryItemData") continue;

            var fName = t.GetField("displayName", BF);
            var fIcon = t.GetField("icon", BF);
            if (fName == null || fIcon == null) continue;

            string name = fName.GetValue(so) as string;
            var    icon = fIcon.GetValue(so) as Sprite;
            if (string.IsNullOrEmpty(name) || icon == null) continue;

            Put(_inv, Norm(name), icon, path);
        }
    }

    private static void Put(Dictionary<string, Found> dict, string key, Sprite s, string path)
    {
        if (string.IsNullOrEmpty(key) || dict.ContainsKey(key)) return;
        dict[key] = new Found { sprite = s, assetPath = path };
    }

    /// <summary>
    /// Tìm sprite art rời theo TÊN FILE (không phần mở rộng).
    /// Dùng tên file thay vì đường dẫn cứng vì một số thư mục có ký tự Unicode
    /// ("Nhà", "Fantasy Wooden GUI  Free") dễ sai khi hardcode chuỗi.
    /// </summary>
    private Found ResolveFile(string fileName, string prefer)
    {
        if (_file == null) BuildLibrary();

        string cacheKey = fileName + "|" + prefer;
        if (_file.TryGetValue(cacheKey, out var hit)) return hit;

        var candidates = AssetDatabase.FindAssets($"{fileName} t:Texture2D")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => string.Equals(Path.GetFileNameWithoutExtension(p), fileName,
                                      System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToList();

        if (!string.IsNullOrEmpty(prefer))
        {
            var narrowed = candidates.Where(p => p.Contains(prefer)).ToList();
            if (narrowed.Count > 0) candidates = narrowed;
        }

        Found result = null;
        foreach (string p in candidates)
        {
            var s = LoadSprite(p);
            if (s != null) { result = new Found { sprite = s, assetPath = p }; break; }
        }

        _file[cacheKey] = result;
        return result;
    }

    /// <summary>
    /// Nạp Sprite từ 1 texture. Nhiều texture trong dự án ở chế độ
    /// Sprite Mode = Multiple → LoadAssetAtPath&lt;Sprite&gt; trả null,
    /// nên phải quét sub-asset.
    /// </summary>
    private static Sprite LoadSprite(string path)
    {
        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (direct != null) return direct;

        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
                            .OfType<Sprite>()
                            .FirstOrDefault();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  7. ÁP DỤNG / ROLLBACK  (BẮT BUỘC SetDirty + SaveAssets)
    // ─────────────────────────────────────────────────────────────────────────

    private void Apply()
    {
        if (_blocks.Count == 0) { Scan(); if (_blocks.Count == 0) return; }

        if (!EditorUtility.DisplayDialog(
                "Điền Icon Unlock",
                $"Sẽ ghi 'unlockEntries' cho {_blocks.Count} asset " +
                $"({_totalRows} mục, {_missRows} mục không có icon).\n\n" +
                "unlockDescriptions và giftItems KHÔNG bị sửa.\n\nTiếp tục?",
                "ÁP DỤNG", "Huỷ"))
            return;

        var log = new StringBuilder();
        log.AppendLine("[UnlockIcon] ═══ ÁP DỤNG ═══");

        int written = 0, misses = 0;
        foreach (var b in _blocks)
        {
            var list = new List<LevelRewardConfig.UnlockEntry>(b.rows.Count);
            foreach (var r in b.rows)
            {
                list.Add(new LevelRewardConfig.UnlockEntry(r.label, r.icon));
                if (r.icon == null)
                {
                    misses++;
                    log.AppendLine($"  ✘ {b.assetName}: \"{r.label}\" — KHÔNG có icon, gán tay!");
                }
            }

            b.cfg.unlockEntries = list;
            EditorUtility.SetDirty(b.cfg);          // ← bắt buộc, không có thì mất dữ liệu
            written++;
            log.AppendLine($"  {b.assetName}: {list.Count} mục ghi xong.");
        }

        AssetDatabase.SaveAssets();                  // ← bắt buộc, ghi xuống đĩa
        AssetDatabase.Refresh();

        log.AppendLine($"[UnlockIcon] Xong: {written} asset · {_totalRows - misses} icon ✔ · {misses} thiếu ✘");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("Điền Icon Unlock",
            $"Đã ghi {written} asset.\n" +
            $"Có icon: {_totalRows - misses}/{_totalRows}\n" +
            $"Thiếu icon: {misses} (xem Console để biết mục nào)",
            "OK");

        Scan();
    }

    private void ClearAll()
    {
        if (!EditorUtility.DisplayDialog("Rollback",
                "Xoá sạch unlockEntries của mọi LevelRewardConfig?\n" +
                "unlockDescriptions vẫn còn nguyên nên UI sẽ tự fallback về chữ (ô trống).",
                "XOÁ", "Huỷ"))
            return;

        int n = 0;
        foreach (var cfg in LoadConfigs())
        {
            cfg.unlockEntries = new List<LevelRewardConfig.UnlockEntry>();
            EditorUtility.SetDirty(cfg);
            n++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnlockIcon] Đã xoá unlockEntries của {n} asset.");
        Scan();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  8. TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    private static List<LevelRewardConfig> LoadConfigs()
    {
        string[] guids = AssetDatabase.IsValidFolder(FOLDER)
            ? AssetDatabase.FindAssets("t:LevelRewardConfig", new[] { FOLDER })
            : AssetDatabase.FindAssets("t:LevelRewardConfig");

        return guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(p))
            .Where(c => c != null)
            .OrderBy(c => c.levelReached)
            .ToList();
    }

    private string BuildLog()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[UnlockIcon] ═══ XEM TRƯỚC ═══");
        foreach (var b in _blocks)
        {
            sb.AppendLine($"── {b.assetName} (L{b.cfg.levelReached})");
            foreach (var r in b.rows)
                sb.AppendLine(r.icon != null
                    ? $"   ✔ \"{r.label}\"  ← '{r.matchedKeyword}'  {r.sourceInfo}" +
                      (string.IsNullOrEmpty(r.note) ? "" : $"  ⓘ {r.note}")
                    : $"   ✘ \"{r.label}\"  ← KHÔNG KHỚP");
        }
        sb.AppendLine($"[UnlockIcon] {_totalRows} mục · thiếu icon {_missRows}");
        return sb.ToString();
    }

    /// <summary>In đường dẫn sprite xu vàng / kim cương đúng như HUD đang dùng (cho DEV-B).</summary>
    private void LogCurrencyIcons()
    {
        var gold = ResolveFile("vang-removebg-preview", "Fantasy Wooden GUI");
        var gem  = ResolveFile("kimcuong-removebg-preview", "Assetsgame");
        Debug.Log(
            "[UnlockIcon] ICON TIỀN TỆ — đúng sprite HUD đang dùng:\n" +
            $"  XU VÀNG    : {(gold != null ? gold.assetPath + "  (sprite: " + gold.sprite.name + ")" : "KHÔNG TÌM THẤY")}\n" +
            "               HUD: Canvas_HUD/SafeArea/TOPBAR/LeftTopBar/GoldBox/Vangicon\n" +
            $"  KIM CƯƠNG  : {(gem  != null ? gem.assetPath  + "  (sprite: " + gem.sprite.name  + ")" : "KHÔNG TÌM THẤY")}\n" +
            "               HUD: Canvas_HUD/SafeArea/TOPBAR/LeftTopBar/GemBox/kimcuongIcon");
    }

    // ─── Chuẩn hoá chuỗi tiếng Việt ──────────────────────────────────────────

    /// <summary>Bỏ dấu + chữ thường + gộp khoảng trắng. "MỞ KHÓA NHÀ BẾP" → "mo khoa nha bep".</summary>
    private static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        // đ/Đ không phân rã được bằng FormD → thay thủ công trước
        s = s.Replace('Đ', 'D').Replace('đ', 'd');

        string d = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (char c in d)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.ToLowerInvariant(c));
        }

        // gộp mọi ký tự không phải chữ/số thành 1 khoảng trắng → khớp nguyên từ dễ hơn
        var outSb = new StringBuilder(sb.Length);
        bool lastSpace = true;
        foreach (char c in sb.ToString())
        {
            if (char.IsLetterOrDigit(c)) { outSb.Append(c); lastSpace = false; }
            else if (!lastSpace)         { outSb.Append(' '); lastSpace = true; }
        }
        return outSb.ToString().Trim();
    }

    /// <summary>
    /// Khớp NGUYÊN TỪ: "kho" KHÔNG khớp "mo khoa", nhưng khớp "nang cap kho lan 1".
    /// needle phải đã Norm sẵn (ASCII, chữ thường, cách nhau bằng 1 space).
    /// </summary>
    private static bool ContainsWord(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return false;

        int from = 0;
        while (true)
        {
            int i = haystack.IndexOf(needle, from, System.StringComparison.Ordinal);
            if (i < 0) return false;

            bool leftOk  = i == 0 || haystack[i - 1] == ' ';
            int  end     = i + needle.Length;
            bool rightOk = end == haystack.Length || haystack[end] == ' ';
            if (leftOk && rightOk) return true;

            from = i + 1;
        }
    }
}
