using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// CHUYỂN TILE NỀN VUÔNG → Ô THOI ISOMETRIC (bản xem thử)
/// ══════════════════════════════════════════════════════
/// Menu: Tools ▸ Farm ▸ Thử Nền Isometric
///
/// MỤC ĐÍCH: cho bạn NHÌN TẬN MẮT nền isometric trông thế nào trước khi
/// quyết định có đổi cả game sang isometric hay không.
///
/// ⚠️ CHỈ CHUYỂN ĐƯỢC TILE NỀN PHẲNG (cỏ, đất, ruộng, lối đi, nước).
///    Nhà / hàng rào / vách đá / cây KHÔNG chuyển được — ảnh gốc vẽ nhìn
///    thẳng mặt, KHÔNG CÓ pixel mặt hông, không phép biến hình nào bịa ra được.
///
/// AN TOÀN: chỉ ĐỌC thư mục gốc, mọi thứ sinh ra nằm trong _Iso_Preview/.
///          Xoá thư mục đó là dự án về nguyên trạng.
///
/// ── TOÁN HỌC ────────────────────────────────────────────────────────
/// Phép chiếu isometric 2:1 chuẩn (xoay 45° rồi ép dẹt chiều dọc 50%):
///     u = x - cx ,  v = y - cy          (toạ độ so với tâm ô nguồn)
///     dU = u - v                        (ngang, dải rộng gấp đôi)
///     dV = (u + v) / 2                  (dọc, dẹt một nửa)
/// Nghịch đảo (dùng khi lấy mẫu ngược từ ảnh đích về ảnh nguồn):
///     u = dU/2 + dV
///     v = dV - dU/2
/// Ô nguồn 64×64  →  ô thoi đích 128×64.
/// </summary>
public class IsoGroundTileConverter : EditorWindow
{
    // ── Đường dẫn ───────────────────────────────────────────────────────
    private const string SRC_ROOT = "Assets/maptitle/Design_Map/HappyHarvest_NatureDecor/Tiles";
    private const string OUT_ROOT = "Assets/_Iso_Preview";

    /// <summary>
    /// Các thư mục NỀN PHẲNG — chuyển được.
    /// KHÔNG có "Water": đã kiểm tận nơi, thư mục Water chỉ chứa Sprite_WaterIcon.png
    /// (64×64, Sprite Mode = Single) — đó là ICON UI, không phải tile nền. Chuyển nó
    /// thành hình thoi sẽ ra kết quả vô nghĩa và báo "✔ Water: 1 ô thoi" gây hiểu nhầm.
    /// </summary>
    private static readonly string[] GroundFolders =
    { "Grass", "Dirt", "Soil", "SoilWatered", "Walkway" };

    /// <summary>Thư mục vật thể có chiều cao — KHÔNG chuyển, chỉ để cảnh báo.</summary>
    private static readonly string[] HeightFolders =
    { "House", "Fence", "Elevation", "Pinetrees", "Warehouse" };

    // ── Tuỳ chọn ────────────────────────────────────────────────────────
    // [SerializeField] để tuỳ chọn không bị reset mỗi lần Unity recompile
    [SerializeField, Tooltip("Nhân độ phân giải khi chuyển. 2 = nét hơn, file to gấp 4.")]
    private int   _supersample   = 2;
    [SerializeField] private bool _buildPreview = true;
    [SerializeField] private int  _previewSize  = 14;
    [SerializeField] private bool _cleanOutput  = true;

    private Vector2 _scroll;
    private readonly List<string> _log = new List<string>();

    [MenuItem("Tools/Farm/Thử Nền Isometric", false, 22)]
    public static void Open()
    {
        var w = GetWindow<IsoGroundTileConverter>(true, "Thử Nền Isometric");
        w.minSize = new Vector2(460, 520);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("CHUYỂN NỀN VUÔNG → Ô THOI ISOMETRIC", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Bản XEM THỬ — không đụng vào asset gốc.\n" +
            "• Chuyển 5 thư mục nền phẳng: Grass, Dirt, Soil, SoilWatered, Walkway\n" +
            "• Mọi thứ sinh ra nằm trong " + OUT_ROOT + " — xoá là về nguyên trạng.",
            MessageType.Info);

        EditorGUILayout.HelpBox(
            "GIỚI HẠN — đọc kỹ trước khi đánh giá kết quả:\n\n" +
            "1. KHÔNG chuyển được: House, Fence, Elevation, Pinetrees, Warehouse.\n" +
            "   Ảnh gốc vẽ nhìn thẳng mặt, KHÔNG CÓ pixel mặt hông — không phép biến\n" +
            "   hình nào bịa ra được khối 3 chiều chưa từng được vẽ.\n\n" +
            "2. MẤT AUTOTILING. Nền thật trong game đang dùng RuleTile (RuleTile_Grass,\n" +
            "   RuleTile_Dirt, RuleTile_Walkway…). Tool này sinh Tile TĨNH nên mất hết\n" +
            "   logic viền chuyển tiếp. Muốn dùng thật phải dựng lại RuleTile theo luật\n" +
            "   hàng xóm của lưới isometric — khác hoàn toàn lưới vuông.\n\n" +
            "3. Ảnh bị XIÊN 26.57°, không bị cắt. Cỏ/đất (vân hữu cơ) gần như không thấy;\n" +
            "   luống cày thấy rõ; ĐÁ LÁT (Walkway) hỏng rõ — cạnh thẳng thành hình\n" +
            "   bình hành. Đá lát bắt buộc phải vẽ lại.\n\n" +
            "4. Mất một nửa độ phân giải chiều dọc (ép 2:1). Kết quả mềm hơn bản gốc.",
            MessageType.Warning);

        EditorGUILayout.Space(6);
        _supersample  = EditorGUILayout.IntSlider(
            new GUIContent("Độ nét", "2 = lấy mẫu gấp đôi rồi thu nhỏ, viền mượt hơn"),
            _supersample, 1, 3);
        _cleanOutput  = EditorGUILayout.Toggle(
            new GUIContent("Xoá kết quả cũ", "Xoá sạch _Iso_Preview trước khi chạy"), _cleanOutput);
        _buildPreview = EditorGUILayout.Toggle(
            new GUIContent("Dựng lưới xem thử", "Tạo Grid Isometric + tô sẵn một mảng cỏ trong scene"),
            _buildPreview);
        if (_buildPreview)
            _previewSize = EditorGUILayout.IntSlider("Kích thước mảng", _previewSize, 4, 30);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.55f, 0.9f, 0.5f);
        if (GUILayout.Button("CHUYỂN & XEM THỬ", GUILayout.Height(40)))
            Run();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Xoá toàn bộ bản xem thử", GUILayout.Height(24)))
            CleanAll();

        if (_log.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Kết quả", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            foreach (var l in _log) EditorGUILayout.LabelField(l, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CHẠY
    // ════════════════════════════════════════════════════════════════════

    private void Run()
    {
        // Bọc toàn bộ: nếu exception thoát ra OnGUI thì _log mất sạch,
        // người dùng không biết vì sao hỏng.
        try { RunInternal(); }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            _log.Add("✘ LỖI: " + e.Message);
            Debug.LogException(e);
        }
    }

    private void RunInternal()
    {
        _log.Clear();

        if (!AssetDatabase.IsValidFolder(SRC_ROOT))
        {
            _log.Add($"✘ Không tìm thấy thư mục nguồn:\n{SRC_ROOT}");
            return;
        }

        if (_cleanOutput) CleanAll(false);
        EnsureFolder(OUT_ROOT);
        EnsureFolder(OUT_ROOT + "/Sprites");
        EnsureFolder(OUT_ROOT + "/Tiles");

        int totalSprites = 0, totalTiles = 0;
        var firstTilePerFolder = new List<TileBase>();

        try
        {
            for (int fi = 0; fi < GroundFolders.Length; fi++)
            {
                string folder = GroundFolders[fi];
                EditorUtility.DisplayProgressBar("Chuyển sang isometric",
                    $"Đang xử lý {folder}…", (float)fi / GroundFolders.Length);

                string dir = $"{SRC_ROOT}/{folder}";
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    _log.Add($"–  {folder}: không có thư mục, bỏ qua");
                    continue;
                }

                // Tìm mọi PNG trong thư mục, bỏ normal map và mask
                var pngs = AssetDatabase.FindAssets("t:Texture2D", new[] { dir })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    .Where(p => !p.Contains("_normal") && !p.Contains("_mask"))
                    .ToList();

                if (pngs.Count == 0) { _log.Add($"–  {folder}: không có PNG"); continue; }

                int folderSprites = 0;
                foreach (var png in pngs)
                {
                    var made = ConvertSheet(png, folder, out int n);
                    folderSprites += n;
                    if (made != null && made.Count > 0)
                    {
                        totalTiles += made.Count;
                        // Lấy ô GIỮA sheet, không lấy made[0].
                        // Index 0 = ô trên-trái của sheet RuleTile = ô GÓC/VIỀN chuyển tiếp,
                        // tô đầy màn hình bằng nó sẽ ra hoa văn góc lặp lại → đánh giá sai.
                        if (firstTilePerFolder.Count <= fi)
                            firstTilePerFolder.Add(made[made.Count / 2]);
                    }
                }

                totalSprites += folderSprites;
                _log.Add($"✔  {folder}: {folderSprites} ô thoi");
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _log.Add("");
        _log.Add($"TỔNG: {totalSprites} sprite · {totalTiles} tile asset");
        _log.Add($"Nằm ở: {OUT_ROOT}");
        _log.Add("");
        _log.Add("⚠ KHÔNG chuyển (vật thể có chiều cao): " + string.Join(", ", HeightFolders));
        _log.Add("   → Muốn isometric hoàn chỉnh phải vẽ lại/mua pack cho những thứ này.");

        if (_buildPreview && totalTiles > 0)
        {
            var tile = firstTilePerFolder.FirstOrDefault(t => t != null);
            if (tile != null)
            {
                BuildPreviewGrid(tile);
                _log.Add("");
                _log.Add($"✔ Đã dựng 'ISO_Preview_Grid' trong scene ({_previewSize}×{_previewSize} ô).");
                _log.Add("   Nhấn F để zoom tới nó trong Scene view.");
            }
        }

        Debug.Log("[IsoConverter] " + string.Join("\n", _log));
    }

    // ════════════════════════════════════════════════════════════════════
    // CHUYỂN MỘT SHEET
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Chuyển mọi sprite trong 1 sheet thành ô thoi, mỗi ô 1 file PNG riêng.</summary>
    private List<TileBase> ConvertSheet(string srcPath, string folderName, out int count)
    {
        count = 0;
        var result = new List<TileBase>();

        var importer = AssetImporter.GetAtPath(srcPath) as TextureImporter;
        if (importer == null) return result;

        // Ghi lại trạng thái gốc để TRẢ NGUYÊN sau khi xong.
        bool oldReadable = importer.isReadable;
        var  oldComp     = importer.textureCompression;
        bool oldCrunch   = importer.crunchedCompression;

        // GetPixels() cần Read/Write. Ngoài ra nếu texture bị nén (BC/DXT) thì màu
        // đọc ra bị lệch, còn Crunch thì GetPixels NÉM EXCEPTION → ép Uncompressed.
        bool needFix = !oldReadable
                    || oldComp != TextureImporterCompression.Uncompressed
                    || oldCrunch;

        try
        {
            if (needFix)
            {
                importer.isReadable          = true;
                importer.textureCompression  = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;
                importer.SaveAndReimport();
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
            if (tex == null) return result;

            // Lấy danh sách sprite con. Sheet Multiple → nhiều sub-asset;
            // Single → chính nó là 1 sprite.
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(srcPath)
                                       .OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(srcPath);
                if (single != null) sprites.Add(single);
            }
            if (sprites.Count == 0) return result;

            foreach (var spr in sprites)
            {
                var rect = spr.rect;
                int sw = Mathf.RoundToInt(rect.width);
                int sh = Mathf.RoundToInt(rect.height);
                if (sw <= 0 || sh <= 0) continue;

                // Phép thoi 2:1 (dw = sw*2, dh = sh) chỉ đúng khi ô VUÔNG.
                // Với sw≠sh, max|dV| = (sw+sh)/4 sẽ vượt dh/2 → bị cắt cụt trên/dưới.
                if (sw != sh)
                {
                    _log.Add($"–  bỏ qua {spr.name} ({sw}×{sh}) — chỉ chuyển được ô vuông");
                    continue;
                }

                var srcPx = tex.GetPixels(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), sw, sh);
                var diamond = ToIsoDiamond(srcPx, sw, sh, _supersample);

                int dw = sw * 2, dh = sh;   // ô thoi rộng gấp đôi, cao giữ nguyên

                // Tên file có KÈM TÊN SHEET: FindAssets quét đệ quy nên 2 sheet khác
                // nhau trong cùng thư mục có thể có sprite trùng tên → ghi đè âm thầm.
                string sheet    = MakeSafe(Path.GetFileNameWithoutExtension(srcPath));
                string safeName = MakeSafe(spr.name);
                string outPng   = $"{OUT_ROOT}/Sprites/{folderName}_{sheet}_{safeName}.png";

                WritePng(diamond, dw, dh, outPng);
                ImportAsIsoSprite(outPng, dw);

                var newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(outPng);
                if (newSprite == null) continue;

                // Tạo Tile asset trỏ tới sprite mới
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite    = newSprite;
                tile.colliderType = Tile.ColliderType.None;
                string tilePath = $"{OUT_ROOT}/Tiles/T_{folderName}_{sheet}_{safeName}.asset";
                AssetDatabase.CreateAsset(tile, tilePath);

                result.Add(tile);
                count++;
            }
        }
        finally
        {
            // Trả asset GỐC về đúng nguyên trạng — tuyệt đối không để lại tác dụng phụ
            if (needFix)
            {
                importer.isReadable          = oldReadable;
                importer.textureCompression  = oldComp;
                importer.crunchedCompression = oldCrunch;
                importer.SaveAndReimport();
            }
        }

        return result;
    }

    // ════════════════════════════════════════════════════════════════════
    // PHÉP BIẾN HÌNH
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Biến ô vuông thành ô thoi isometric 2:1.
    /// Lấy mẫu NGƯỢC: duyệt từng pixel ĐÍCH, tính ngược về toạ độ NGUỒN.
    /// Cách này không để lại lỗ hổng như lấy mẫu xuôi.
    /// </summary>
    private static Color[] ToIsoDiamond(Color[] src, int sw, int sh, int ss)
    {
        int dw = sw * 2, dh = sh;
        var dst = new Color[dw * dh];

        float cx = sw * 0.5f, cy = sh * 0.5f;
        int   n  = Mathf.Max(1, ss);
        float inv = 1f / (n * n);

        for (int dy = 0; dy < dh; dy++)
        for (int dx = 0; dx < dw; dx++)
        {
            float r = 0f, g = 0f, b = 0f, a = 0f;

            // Siêu lấy mẫu: chia mỗi pixel đích thành n×n mẫu con
            for (int sy = 0; sy < n; sy++)
            for (int sx = 0; sx < n; sx++)
            {
                float px = dx + (sx + 0.5f) / n;
                float py = dy + (sy + 0.5f) / n;

                // Toạ độ so với tâm ảnh đích
                float dU = px - dw * 0.5f;
                float dV = py - dh * 0.5f;

                // NGHỊCH ĐẢO phép chiếu iso (xem ghi chú đầu file)
                float u = dU * 0.5f + dV;
                float v = dV - dU * 0.5f;

                float fx = cx + u;
                float fy = cy + v;

                var c = SampleBilinear(src, sw, sh, fx, fy);
                r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
            }

            a *= inv;
            if (a > 0.0001f)
            {
                // Bỏ nhân alpha (unpremultiply) để không bị viền tối
                float k = 1f / (a * n * n);
                dst[dy * dw + dx] = new Color(r * k, g * k, b * k, a);
            }
            else dst[dy * dw + dx] = Color.clear;
        }

        return dst;
    }

    /// <summary>Lấy mẫu song tuyến. Ngoài biên → trong suốt (đó là phần ngoài hình thoi).</summary>
    private static Color SampleBilinear(Color[] src, int w, int h, float x, float y)
    {
        x -= 0.5f; y -= 0.5f;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float tx = x - x0, ty = y - y0;

        Color c00 = Px(src, w, h, x0,     y0);
        Color c10 = Px(src, w, h, x0 + 1, y0);
        Color c01 = Px(src, w, h, x0,     y0 + 1);
        Color c11 = Px(src, w, h, x0 + 1, y0 + 1);

        return Color.Lerp(Color.Lerp(c00, c10, tx), Color.Lerp(c01, c11, tx), ty);
    }

    /// <summary>
    /// Lấy pixel, có KẸP BIÊN 1 texel.
    ///
    /// ⚠️ ĐÂY LÀ CHỖ TỪNG GÂY LỖI NẶNG NHẤT: nếu trả Color.clear ngay khi ra ngoài
    /// [0,w) thì pixel nằm ĐÚNG trên cạnh hình thoi chỉ còn alpha 0.5 (bilinear trộn
    /// nửa trong nửa "clear"). Hai ô cạnh nhau ghép lại chỉ ra alpha 0.75 →
    /// **25% nền lọt qua** → cả mảng cỏ bị kẻ một mạng lưới đường mờ dọc mọi cạnh.
    ///
    /// Cách sửa: trong phạm vi 1 texel thì KẸP vào trong (giống extrude/padding của
    /// atlas). Quá 1 texel mới trả trong suốt — đó là 4 tam giác góc nằm ngoài hình thoi,
    /// vẫn bị cắt đúng như mong muốn.
    /// </summary>
    private static Color Px(Color[] src, int w, int h, int x, int y)
    {
        if (x < -1 || y < -1 || x > w || y > h) return Color.clear;
        return src[Mathf.Clamp(y, 0, h - 1) * w + Mathf.Clamp(x, 0, w - 1)];
    }

    // ════════════════════════════════════════════════════════════════════
    // GHI FILE
    // ════════════════════════════════════════════════════════════════════

    private static void WritePng(Color[] px, int w, int h, string assetPath)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels(px);
        t.Apply();
        File.WriteAllBytes(Abs(assetPath), t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(assetPath,
            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    /// <summary>
    /// PPU = chiều rộng ô thoi ⇒ mỗi ô rộng đúng 1 unit, cao 0.5 unit.
    /// Khớp chính xác Grid isometric cellSize (1, 0.5, 1).
    /// </summary>
    private static void ImportAsIsoSprite(string assetPath, int diamondWidth)
    {
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return;

        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = diamondWidth;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = false;
        imp.filterMode          = FilterMode.Bilinear;
        imp.wrapMode            = TextureWrapMode.Clamp;

        // KHÔNG nén: nén BC/DXT làm vỡ khối màu ở viền thoi (viền chéo rất nhạy).
        imp.textureCompression  = TextureImporterCompression.Uncompressed;
        imp.crunchedCompression = false;
        imp.maxTextureSize      = Mathf.Max(2048, Mathf.NextPowerOfTwo(diamondWidth));

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Center;
        // FullRect chứ KHÔNG Tight: mesh Tight bám sát viền thoi sẽ làm sứt 4 đỉnh nhọn.
        s.spriteMeshType  = SpriteMeshType.FullRect;
        s.spriteGenerateFallbackPhysicsShape = false;
        imp.SetTextureSettings(s);

        imp.SaveAndReimport();
    }

    // ════════════════════════════════════════════════════════════════════
    // LƯỚI XEM THỬ
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Tạo Grid Isometric + Tilemap và tô sẵn một mảng để nhìn ngay.</summary>
    private void BuildPreviewGrid(TileBase tile)
    {
        var old = GameObject.Find("ISO_Preview_Grid");
        if (old != null) DestroyImmediate(old);

        var go   = new GameObject("ISO_Preview_Grid");
        var grid = go.AddComponent<Grid>();

        // ĐÂY LÀ 2 DÒNG QUAN TRỌNG NHẤT:
        // Grid hiện tại của bạn là Rectangle → phải đổi sang Isometric,
        // và cellSize phải là (1, 0.5) mới khớp tỉ lệ ô thoi 2:1.
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize   = new Vector3(1f, 0.5f, 1f);

        var tmGo = new GameObject("Tilemap_Ground_Iso");
        tmGo.transform.SetParent(go.transform, false);

        var tm  = tmGo.AddComponent<Tilemap>();
        var tmr = tmGo.AddComponent<TilemapRenderer>();

        // tileAnchor PHẢI là 0 trên lưới Isometric.
        // Mặc định AddComponent cho (0.5, 0.5) — với cellSize (1, 0.5) thì
        // CellToLocalInterpolated(0.5,0.5) = (0, +0.25) → cả tilemap lệch LÊN
        // nửa chiều cao ô so với gizmo lưới. CellToLocal của iso đã trả về TÂM ô rồi.
        tm.tileAnchor = Vector3.zero;

        // Chunk = gộp batch. Đừng để Individual — scene hiện tại của bạn đang
        // để Individual cho 26.408 tile, đó là quả bom hiệu năng.
        tmr.mode      = TilemapRenderer.Mode.Chunk;
        tmr.sortOrder = TilemapRenderer.SortOrder.TopRight;

        // Dự án dùng URP 2D. Các Light2D trong SCN_Farm KHÔNG áp lên sorting layer
        // "Default" → renderer mới để Default có thể render ĐEN THUI.
        // Đặt vào "Bottom" cho trùng layer với nền hiện có.
        // Chỉ dùng HasSortingLayer: NameToID trả hash của tên, với tên KHÔNG tồn tại
        // nó vẫn có thể trả khác 0 → guard mất tác dụng.
        if (HasSortingLayer("Bottom")) tmr.sortingLayerName = "Bottom";
        tmr.sortingOrder = -100;

        int half = _previewSize / 2;
        for (int x = -half; x < half; x++)
        for (int y = -half; y < half; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);

        Undo.RegisterCreatedObjectUndo(go, "Dựng lưới xem thử Isometric");
        Selection.activeGameObject = go;

        var sv = SceneView.lastActiveSceneView;
        if (sv != null) sv.FrameSelected();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // ════════════════════════════════════════════════════════════════════
    // TIỆN ÍCH
    // ════════════════════════════════════════════════════════════════════

    private void CleanAll(bool log = true)
    {
        var g = GameObject.Find("ISO_Preview_Grid");
        if (g != null) DestroyImmediate(g);

        if (AssetDatabase.IsValidFolder(OUT_ROOT))
        {
            AssetDatabase.DeleteAsset(OUT_ROOT);
            AssetDatabase.Refresh();
            if (log)
            {
                _log.Clear();
                _log.Add("✔ Đã xoá sạch bản xem thử. Dự án về nguyên trạng.");
            }
        }
        else if (log)
        {
            _log.Clear();
            _log.Add("– Không có gì để xoá.");
        }
    }

    private static bool HasSortingLayer(string name)
    {
        foreach (var l in SortingLayer.layers) if (l.name == name) return true;
        return false;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;
        string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        string leaf   = Path.GetFileName(assetPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string Abs(string assetPath)
        => Path.Combine(Directory.GetCurrentDirectory(), assetPath);

    private static string MakeSafe(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace(' ', '_');
    }
}
