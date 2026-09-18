#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ============================================================================
/// SOAN KHU DAT BAN — Tools ▸ Farm Game ▸ Land Region Author
/// ============================================================================
///
/// VI SAO CO FILE NAY
/// ------------------
/// Cong cu cu "Tools/Map45/11. Chia Lo Dat" (IsoFenceLotScanner) chi sinh duoc
/// lo o nhung cho CO HANG RAO KHEP KIN do designer ve tay, va moi lan chay no
/// XOA + DANH SO LAI toan bo asset Lot_*.asset. Save cua nguoi choi la
/// PlayerPrefs["FARM_UNLOCKED_REGIONS"] = danh sach regionId ngan cach '|',
/// nen viec danh so lai bien lo_07 da mua thanh MOT MANH DAT KHAC.
///
/// File nay la duong DOC LAP:
///   • KHONG can hang rao — runtime chi doc LandRegionData.cellRects, hang rao
///     thuan tuy la do hoa (kiem chung: khong co doan code runtime nao doc
///     hinh hoc hang rao).
///   • Ghi asset voi tien to "North_" chu KHONG phai "Lot_", nen vong quet
///     cua IsoFenceLotScanner khong bao gio dung toi.
///   • Dinh gia THEO DIEN TICH (vang/o), khong theo chi so nhu PriceOf() cu —
///     chinh cho do ma Lo 21/25/27 (47/22/13 o) dat hon lo to gap 4 lan.
///
/// HINH HOC ISO (xem IsoGrid.cs)
/// -----------------------------
///     worldX = Ox + (cx - cy) * CellWidth  * 0.5      (CellWidth  = 300)
///     worldY = Oy + (cx + cy) * CellHeight * 0.5      (CellHeight = 150)
///   nguoc:
///     dx = (wx - Ox) / CellWidth ; dy = (wy - Oy) / CellHeight
///     cx = dx + dy ; cy = dy - dx
/// "HUONG BAC" tren man hinh = cx + cy TANG (ca hai truc cung tang), khong phai
/// mot truc duy nhat. Vi vay mot HOP VUONG trong world la mot HINH THOI trong
/// khong gian o => phai bam thanh nhieu RectInt theo TUNG COT cx (bac thang).
/// </summary>
public class LandRegionAuthorTool : EditorWindow
{
    // ═════════════════════════════════════════════════════════════════════
    //  HANG SO
    // ═════════════════════════════════════════════════════════════════════

    private const string MenuPath    = "Tools/Farm Game/Land Region Author";
    private const string AssetFolder = "Assets/_Game/Farm/Land";
    private const string IdPrefix    = "north_";     // regionId
    private const string FilePrefix  = "North_";     // ten file asset

    /// <summary>Bo dem cho o chieu cao dong khi ve bang tham khao.</summary>
    private const int MaxReferenceRows = 8;

    // ═════════════════════════════════════════════════════════════════════
    //  TRANG THAI CUA SO
    // ═════════════════════════════════════════════════════════════════════

    private enum CheDoNhap
    {
        GoRectTheoO = 0,     // (a) go tay x / y / width / height
        KeoHopTrongScene = 1 // (b) keo hop world trong Scene view
    }

    private CheDoNhap cheDo = CheDoNhap.KeoHopTrongScene;

    // — (a) rect go tay —
    private int rectX = 0, rectY = 30, rectW = 8, rectH = 8;

    // — (b) keo hop trong Scene —
    private bool batKeoTrongScene;
    private bool dangKeo;
    private Vector3 diemDau, diemCuoi;
    private bool coHopWorld;

    // — ket qua —
    private readonly List<RectInt> ketQuaRects = new List<RectInt>();
    private readonly HashSet<Vector2Int> ketQuaCells = new HashSet<Vector2Int>();

    // — thuoc tinh khu —
    private string tenHienThi   = "Đồng Bắc";
    private int    unlockLevel  = 30;
    private int    goldPrice    = 0;
    private int    gemPrice     = 0;
    private int    clearSeconds = 300;
    private bool   tuTinhGia    = true;
    private float  goldPerCell  = 800f;
    private readonly List<string> requiredRegionIds = new List<string>();

    // — va cham —
    private readonly List<ThongTinKhu> khuHienCo = new List<ThongTinKhu>();
    private readonly List<string> khuBiChongLan = new List<string>();
    private int soODungChongLan;
    private bool daNapKhuHienCo;

    // — asset vua tao trong phien nay (de bam "Gan vao scene") —
    private readonly List<LandRegionData> assetVuaTao = new List<LandRegionData>();

    private Vector2 scroll;
    private bool moBangThamKhao = true;

    // ═════════════════════════════════════════════════════════════════════
    //  MO TA MOT KHU DA CO TREN DIA
    // ═════════════════════════════════════════════════════════════════════

    private class ThongTinKhu
    {
        public LandRegionData data;
        public string path;
        public string regionId;
        public string displayName;
        public int cells;
        public int goldPrice;
        public RectInt bounds;
        public Vector2 centerCell;
        public HashSet<Vector2Int> cellSet;

        public float GoldPerCell => cells > 0 ? goldPrice / (float)cells : 0f;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CUA VAO
    // ═════════════════════════════════════════════════════════════════════

    [MenuItem(MenuPath, false, 30)]
    private static void Mo()
    {
        var w = GetWindow<LandRegionAuthorTool>(false, "Land Region Author", true);
        w.minSize = new Vector2(430f, 560f);
        w.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += VeTrongScene;
        NapKhuHienCo();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= VeTrongScene;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  NAP CAC KHU DA CO
    // ═════════════════════════════════════════════════════════════════════

    private void NapKhuHienCo()
    {
        khuHienCo.Clear();
        string[] guids = AssetDatabase.FindAssets("t:LandRegionData");
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var data = AssetDatabase.LoadAssetAtPath<LandRegionData>(path);
            if (data == null) continue;

            var set = new HashSet<Vector2Int>();
            foreach (var c in data.AllCells()) set.Add(c);
            if (set.Count == 0) continue;

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            long sx = 0, sy = 0;
            foreach (var c in set)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
                sx += c.x; sy += c.y;
            }

            khuHienCo.Add(new ThongTinKhu
            {
                data        = data,
                path        = path,
                regionId    = data.regionId,
                displayName = data.displayName,
                cells       = set.Count,
                goldPrice   = data.goldPrice,
                bounds      = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1),
                centerCell  = new Vector2(sx / (float)set.Count, sy / (float)set.Count),
                cellSet     = set
            });
        }
        khuHienCo.Sort((a, b) => string.CompareOrdinal(a.regionId, b.regionId));
        daNapKhuHienCo = true;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HINH HOC — WORLD  →  O  →  BAC THANG RectInt
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bon goc world cua hop → tap o (tam o nam trong hop) → cac RectInt theo
    /// tung COT cx (moi cot mot hoac vai doan cy lien tuc = bac thang).
    /// </summary>
    private static void HopWorldSangBacThang(Vector3 a, Vector3 b,
                                             HashSet<Vector2Int> cellsOut,
                                             List<RectInt> rectsOut)
    {
        cellsOut.Clear();
        rectsOut.Clear();

        float xMin = Mathf.Min(a.x, b.x), xMax = Mathf.Max(a.x, b.x);
        float yMin = Mathf.Min(a.y, b.y), yMax = Mathf.Max(a.y, b.y);
        if (xMax - xMin < 1f || yMax - yMin < 1f) return;

        // 4 goc → khoang o bao ngoai (hop world la HINH THOI trong khong gian o)
        Vector2 c0 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMin, 0f));
        Vector2 c1 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMin, 0f));
        Vector2 c2 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMax, 0f));
        Vector2 c3 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMax, 0f));

        int cxMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x)));
        int cxMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x)));
        int cyMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y)));
        int cyMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y)));

        // chan tren cho khoi treo Unity neu Sep keo ca ban do
        const int GioiHanO = 400;
        if (cxMax - cxMin > GioiHanO) cxMax = cxMin + GioiHanO;
        if (cyMax - cyMin > GioiHanO) cyMax = cyMin + GioiHanO;

        for (int cx = cxMin; cx <= cxMax; cx++)
        {
            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                Vector3 w = IsoGrid.CellCenterToWorld(new Vector2Int(cx, cy));
                if (w.x < xMin || w.x > xMax || w.y < yMin || w.y > yMax) continue;
                cellsOut.Add(new Vector2Int(cx, cy));
            }
        }

        GomThanhRect(cellsOut, rectsOut);
    }

    /// <summary>
    /// Gom tap o thanh cac RectInt rong 1 cot (cx), cao bang doan cy lien tuc.
    /// Day chinh la "bac thang" ma hinh thoi iso can.
    /// </summary>
    private static void GomThanhRect(HashSet<Vector2Int> cells, List<RectInt> rectsOut)
    {
        rectsOut.Clear();
        if (cells.Count == 0) return;

        var theoCot = new Dictionary<int, List<int>>();
        foreach (var c in cells)
        {
            if (!theoCot.TryGetValue(c.x, out var list))
            {
                list = new List<int>();
                theoCot[c.x] = list;
            }
            list.Add(c.y);
        }

        var cot = theoCot.Keys.ToList();
        cot.Sort();
        foreach (int cx in cot)
        {
            var ys = theoCot[cx];
            ys.Sort();
            int dau = ys[0], truoc = ys[0];
            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == truoc + 1) { truoc = ys[i]; continue; }
                rectsOut.Add(new RectInt(cx, dau, 1, truoc - dau + 1));
                dau = ys[i]; truoc = ys[i];
            }
            rectsOut.Add(new RectInt(cx, dau, 1, truoc - dau + 1));
        }
    }

    private void DungRectGoTay()
    {
        ketQuaCells.Clear();
        ketQuaRects.Clear();
        int w = Mathf.Max(0, rectW), h = Mathf.Max(0, rectH);
        if (w == 0 || h == 0) { TinhLaiVaCham(); return; }

        ketQuaRects.Add(new RectInt(rectX, rectY, w, h));
        for (int x = rectX; x < rectX + w; x++)
            for (int y = rectY; y < rectY + h; y++)
                ketQuaCells.Add(new Vector2Int(x, y));

        TinhLaiVaCham();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  VA CHAM VOI KHU DA CO
    // ═════════════════════════════════════════════════════════════════════

    private void TinhLaiVaCham()
    {
        khuBiChongLan.Clear();
        soODungChongLan = 0;
        if (!daNapKhuHienCo) NapKhuHienCo();

        foreach (var k in khuHienCo)
        {
            int dung = 0;
            foreach (var c in ketQuaCells)
                if (k.cellSet.Contains(c)) dung++;
            if (dung <= 0) continue;
            soODungChongLan += dung;
            khuBiChongLan.Add($"{k.regionId} ({k.displayName}) — {dung} ô");
        }

        if (tuTinhGia) goldPrice = GiaTheoDienTich();
    }

    private int GiaTheoDienTich()
        => Mathf.Max(0, Mathf.RoundToInt(ketQuaCells.Count * goldPerCell));

    // ═════════════════════════════════════════════════════════════════════
    //  SCENE VIEW — KEO HOP + XEM TRUOC
    // ═════════════════════════════════════════════════════════════════════

    private void VeTrongScene(SceneView sv)
    {
        if (cheDo == CheDoNhap.KeoHopTrongScene && batKeoTrongScene)
            XuLyKeoHop(sv);

        VeXemTruoc();
    }

    private void XuLyKeoHop(SceneView sv)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(id);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (e.button != 0 || e.alt) break;
                GUIUtility.hotControl = id;
                diemDau = ChuotRaWorld(e.mousePosition);
                diemCuoi = diemDau;
                dangKeo = true;
                coHopWorld = false;
                e.Use();
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != id) break;
                diemCuoi = ChuotRaWorld(e.mousePosition);
                HopWorldSangBacThang(diemDau, diemCuoi, ketQuaCells, ketQuaRects);
                coHopWorld = ketQuaCells.Count > 0;
                e.Use();
                sv.Repaint();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != id) break;
                GUIUtility.hotControl = 0;
                dangKeo = false;
                diemCuoi = ChuotRaWorld(e.mousePosition);
                HopWorldSangBacThang(diemDau, diemCuoi, ketQuaCells, ketQuaRects);
                coHopWorld = ketQuaCells.Count > 0;
                TinhLaiVaCham();
                e.Use();
                Repaint();
                sv.Repaint();
                break;
        }
    }

    /// <summary>Diem chuot Scene view → world tren mat phang z = 0.</summary>
    private static Vector3 ChuotRaWorld(Vector2 mousePos)
    {
        Ray r = HandleUtility.GUIPointToWorldRay(mousePos);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(r, out float d)) return r.GetPoint(d);
        return new Vector3(r.origin.x, r.origin.y, 0f);
    }

    private void VeXemTruoc()
    {
        // ── hop world dang keo ────────────────────────────────────────────
        if (cheDo == CheDoNhap.KeoHopTrongScene && (dangKeo || coHopWorld))
        {
            float xMin = Mathf.Min(diemDau.x, diemCuoi.x), xMax = Mathf.Max(diemDau.x, diemCuoi.x);
            float yMin = Mathf.Min(diemDau.y, diemCuoi.y), yMax = Mathf.Max(diemDau.y, diemCuoi.y);
            var goc = new[]
            {
                new Vector3(xMin, yMin, 0f), new Vector3(xMax, yMin, 0f),
                new Vector3(xMax, yMax, 0f), new Vector3(xMin, yMax, 0f)
            };
            Handles.DrawSolidRectangleWithOutline(goc,
                new Color(0.2f, 0.7f, 1f, 0.06f), new Color(0.2f, 0.8f, 1f, 0.9f));
        }

        if (ketQuaCells.Count == 0) return;

        // ── tung o ket qua ───────────────────────────────────────────────
        Color mauNen  = soODungChongLan > 0 ? new Color(1f, 0.25f, 0.2f, 0.20f)
                                            : new Color(0.3f, 1f, 0.4f, 0.20f);
        Color mauVien = soODungChongLan > 0 ? new Color(1f, 0.35f, 0.3f, 0.65f)
                                            : new Color(0.35f, 1f, 0.5f, 0.55f);

        float hw = IsoGrid.CellWidth * 0.5f;
        float hh = IsoGrid.CellHeight * 0.5f;
        var quad = new Vector3[4];

        // ve toi da 4000 o de Scene view khong chet may
        int veDuoc = 0;
        foreach (var c in ketQuaCells)
        {
            if (++veDuoc > 4000) break;
            Vector3 t = IsoGrid.CellCenterToWorld(c);
            quad[0] = new Vector3(t.x,      t.y + hh, 0f);
            quad[1] = new Vector3(t.x + hw, t.y,      0f);
            quad[2] = new Vector3(t.x,      t.y - hh, 0f);
            quad[3] = new Vector3(t.x - hw, t.y,      0f);
            Handles.DrawSolidRectangleWithOutline(quad, mauNen, mauVien);
        }

        // ── nhan so lieu o tam ───────────────────────────────────────────
        Vector2 tam = Vector2.zero;
        foreach (var c in ketQuaCells) tam += new Vector2(c.x, c.y);
        tam /= ketQuaCells.Count;

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = soODungChongLan > 0 ? Color.red : Color.white },
            fontSize = 13
        };
        string nhan = $"{ketQuaCells.Count} ô  ·  {ketQuaRects.Count} rect";
        if (soODungChongLan > 0) nhan += $"\n⚠ CHỒNG LẤN {soODungChongLan} ô";
        Handles.Label(IsoGrid.CellFloatToWorld(tam), nhan, style);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GIAO DIEN CUA SO
    // ═════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        VeCanhBaoTienTo();
        EditorGUILayout.Space(4f);
        VeChonCheDo();
        EditorGUILayout.Space(4f);
        VeKetQua();
        EditorGUILayout.Space(4f);
        VeThuocTinh();
        EditorGUILayout.Space(4f);
        VeBangThamKhao();
        EditorGUILayout.Space(6f);
        VeNutHanhDong();

        EditorGUILayout.EndScrollView();
    }

    private void VeCanhBaoTienTo()
    {
        EditorGUILayout.HelpBox(
            "TIỀN TỐ BẮT BUỘC LÀ \"North_\" — KHÔNG DÙNG \"Lot_\".\n" +
            "Công cụ Tools/Map45/11 (Chia Lô Đất) XOÁ và ĐÁNH SỐ LẠI mọi asset " +
            "Lot_*.asset mỗi lần chạy. Save của người chơi là danh sách regionId " +
            "trong PlayerPrefs[FARM_UNLOCKED_REGIONS], nên lô đã mua sẽ biến thành " +
            "mảnh đất khác. Asset North_* nằm ngoài vòng xoá đó nên an toàn.",
            MessageType.Warning);
    }

    private void VeChonCheDo()
    {
        EditorGUILayout.LabelField("1 · Khoanh vùng", EditorStyles.boldLabel);
        cheDo = (CheDoNhap)EditorGUILayout.EnumPopup("Cách nhập", cheDo);

        using (new EditorGUI.IndentLevelScope())
        {
            if (cheDo == CheDoNhap.GoRectTheoO)
            {
                EditorGUI.BeginChangeCheck();
                rectX = EditorGUILayout.IntField("x (ô)", rectX);
                rectY = EditorGUILayout.IntField("y (ô)", rectY);
                rectW = Mathf.Max(0, EditorGUILayout.IntField("width (ô)", rectW));
                rectH = Mathf.Max(0, EditorGUILayout.IntField("height (ô)", rectH));
                if (EditorGUI.EndChangeCheck()) DungRectGoTay();

                if (GUILayout.Button("Dựng lại ô từ rect")) DungRectGoTay();
            }
            else
            {
                batKeoTrongScene = EditorGUILayout.ToggleLeft(
                    "BẬT kéo hộp trong Scene view (giữ chuột trái và kéo)",
                    batKeoTrongScene);

                EditorGUILayout.HelpBox(
                    "Khi bật, chuột trái trong Scene view dùng để KÉO HỘP, không chọn " +
                    "được object nữa — tắt lại khi xong.\n" +
                    "Hộp world sẽ được đổi sang không gian ô bằng công thức nghịch của " +
                    "IsoGrid rồi bẻ thành bậc thang RectInt theo từng cột cx " +
                    "(hộp vuông world = hình thoi trong lưới iso).",
                    MessageType.Info);

                if (coHopWorld)
                    EditorGUILayout.LabelField(
                        $"Hộp world: ({Mathf.Min(diemDau.x, diemCuoi.x):0}, {Mathf.Min(diemDau.y, diemCuoi.y):0})" +
                        $" → ({Mathf.Max(diemDau.x, diemCuoi.x):0}, {Mathf.Max(diemDau.y, diemCuoi.y):0})");

                if (GUILayout.Button("Xoá vùng đang khoanh"))
                {
                    ketQuaCells.Clear();
                    ketQuaRects.Clear();
                    coHopWorld = false;
                    TinhLaiVaCham();
                    SceneView.RepaintAll();
                }
            }
        }
    }

    private void VeKetQua()
    {
        EditorGUILayout.LabelField("2 · Kết quả", EditorStyles.boldLabel);

        RectInt bb = BoundsCuaKetQua();
        EditorGUILayout.LabelField($"Số ô: {ketQuaCells.Count}     Số RectInt: {ketQuaRects.Count}");
        if (ketQuaCells.Count > 0)
            EditorGUILayout.LabelField(
                $"Hộp bao (ô): x {bb.xMin}…{bb.xMax - 1}   y {bb.yMin}…{bb.yMax - 1}   " +
                $"(cx+cy: {bb.xMin + bb.yMin} … {bb.xMax + bb.yMax - 2})");

        if (soODungChongLan > 0)
        {
            EditorGUILayout.HelpBox(
                $"⚠ CHỒNG LẤN {soODungChongLan} ô với khu đã có:\n  • " +
                string.Join("\n  • ", khuBiChongLan) +
                "\nHai khu cùng chứa một ô sẽ làm người chơi mua hai lần cùng mảnh đất. " +
                "Hãy kéo lại hộp, hoặc bấm Tạo asset và tự chịu trách nhiệm.",
                MessageType.Error);
        }
        else if (ketQuaCells.Count > 0)
        {
            EditorGUILayout.HelpBox("Không chồng lấn khu nào.", MessageType.Info);
        }
    }

    private RectInt BoundsCuaKetQua()
    {
        if (ketQuaCells.Count == 0) return new RectInt(0, 0, 0, 0);
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in ketQuaCells)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private void VeThuocTinh()
    {
        EditorGUILayout.LabelField("3 · Thuộc tính khu", EditorStyles.boldLabel);

        tenHienThi   = EditorGUILayout.TextField("displayName", tenHienThi);
        unlockLevel  = Mathf.Max(0, EditorGUILayout.IntField("unlockLevel", unlockLevel));
        clearSeconds = Mathf.Max(0, EditorGUILayout.IntField("clearSeconds", clearSeconds));
        gemPrice     = Mathf.Max(0, EditorGUILayout.IntField("gemPrice", gemPrice));

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Giá vàng — TÍNH THEO DIỆN TÍCH", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(
            "PriceOf() của tool Chia Lô cũ tính giá theo CHỈ SỐ lô, nên Lô 21/25/27 " +
            "(47 / 22 / 13 ô) bị gán 260k–390k vàng — đắt hơn lô rộng gấp 4 lần. " +
            "Ở đây giá mặc định = số ô × vàng/ô.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        tuTinhGia   = EditorGUILayout.ToggleLeft("Tự tính giá = số ô × vàng/ô", tuTinhGia);
        goldPerCell = EditorGUILayout.FloatField("   vàng / ô", goldPerCell);
        if (EditorGUI.EndChangeCheck() && tuTinhGia) goldPrice = GiaTheoDienTich();

        using (new EditorGUI.DisabledScope(tuTinhGia))
            goldPrice = Mathf.Max(0, EditorGUILayout.IntField("goldPrice", goldPrice));

        if (tuTinhGia) goldPrice = GiaTheoDienTich();
        EditorGUILayout.LabelField($"   → goldPrice = {goldPrice:n0} vàng " +
                                   $"({(ketQuaCells.Count > 0 ? goldPrice / (float)ketQuaCells.Count : 0f):0.#}/ô)");

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("requiredRegionIds (khu phải mở trước)",
                                   EditorStyles.miniBoldLabel);
        int xoaDong = -1;
        for (int i = 0; i < requiredRegionIds.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                requiredRegionIds[i] = EditorGUILayout.TextField(requiredRegionIds[i]);
                if (GUILayout.Button("−", GUILayout.Width(24f))) xoaDong = i;
            }
        }
        // xoa SAU vong lap de khong pha cau truc layout cua IMGUI
        if (xoaDong >= 0) requiredRegionIds.RemoveAt(xoaDong);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Thêm dòng")) requiredRegionIds.Add("lot_00");
            if (GUILayout.Button("Chọn khu gần nhất")) ThemKhuGanNhatLamDieuKien();
        }
    }

    private void ThemKhuGanNhatLamDieuKien()
    {
        var gan = KhuGanNhat(1);
        if (gan.Count == 0)
        {
            EditorUtility.DisplayDialog("Land Region Author",
                "Chưa khoanh vùng, hoặc chưa có khu nào trên đĩa.", "OK");
            return;
        }
        string id = gan[0].regionId;
        if (!requiredRegionIds.Contains(id)) requiredRegionIds.Add(id);
    }

    /// <summary>Cac khu da co, sap theo khoang cach tam (khong gian o).</summary>
    private List<ThongTinKhu> KhuGanNhat(int soLuong)
    {
        if (ketQuaCells.Count == 0 || khuHienCo.Count == 0) return new List<ThongTinKhu>();

        Vector2 tam = Vector2.zero;
        foreach (var c in ketQuaCells) tam += new Vector2(c.x, c.y);
        tam /= ketQuaCells.Count;

        return khuHienCo
            .OrderBy(k => (k.centerCell - tam).sqrMagnitude)
            .Take(Mathf.Max(1, soLuong))
            .ToList();
    }

    private void VeBangThamKhao()
    {
        moBangThamKhao = EditorGUILayout.Foldout(moBangThamKhao,
            "4 · Vàng/ô của các khu lân cận (để chọn con số nhất quán)", true);
        if (!moBangThamKhao) return;

        if (!daNapKhuHienCo || khuHienCo.Count == 0)
        {
            EditorGUILayout.LabelField("   Chưa nạp được khu nào.");
            if (GUILayout.Button("Nạp lại danh sách khu")) NapKhuHienCo();
            return;
        }

        var gan = KhuGanNhat(MaxReferenceRows);
        if (gan.Count == 0)
        {
            EditorGUILayout.LabelField("   Khoanh vùng trước để thấy khu lân cận.");
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (var k in gan)
                EditorGUILayout.LabelField(
                    $"{k.regionId}  ·  {k.cells} ô  ·  {k.goldPrice:n0} vàng  " +
                    $"→  {k.GoldPerCell:0.#} vàng/ô");

            float tb = gan.Where(k => k.cells > 0 && k.goldPrice > 0)
                          .Select(k => k.GoldPerCell)
                          .DefaultIfEmpty(0f).Average();
            EditorGUILayout.LabelField($"Trung bình lân cận: {tb:0.#} vàng/ô",
                                       EditorStyles.miniBoldLabel);
            if (tb > 0f && GUILayout.Button($"Lấy {tb:0.#} vàng/ô làm mặc định"))
            {
                goldPerCell = tb;
                if (tuTinhGia) goldPrice = GiaTheoDienTich();
            }
        }

        if (GUILayout.Button("Nạp lại danh sách khu từ đĩa")) NapKhuHienCo();
    }

    private void VeNutHanhDong()
    {
        EditorGUILayout.LabelField("5 · Ghi ra asset", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(ketQuaCells.Count == 0))
        {
            if (GUILayout.Button($"Tạo asset {FilePrefix}xx.asset", GUILayout.Height(28f)))
                TaoAsset();
        }

        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField(
            assetVuaTao.Count == 0
                ? "Chưa tạo asset nào trong phiên này."
                : $"Đã tạo trong phiên này: {assetVuaTao.Count} asset.");

        using (new EditorGUI.DisabledScope(assetVuaTao.Count == 0))
        {
            if (GUILayout.Button("Gắn vào scene (LandExpansionManager.regions)",
                                 GUILayout.Height(24f)))
                GanVaoScene();
        }

        EditorGUILayout.HelpBox(
            "Nút gắn vào scene KHÔNG tự lưu scene. Sếp tự bấm Ctrl+S sau khi kiểm tra.",
            MessageType.None);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAO ASSET
    // ═════════════════════════════════════════════════════════════════════

    private void TaoAsset()
    {
        if (ketQuaCells.Count == 0) return;

        if (soODungChongLan > 0)
        {
            bool tiep = EditorUtility.DisplayDialog(
                "Chồng lấn khu đã có",
                $"Vùng đang khoanh chồng lấn {soODungChongLan} ô với:\n\n  • " +
                string.Join("\n  • ", khuBiChongLan) +
                "\n\nHai khu cùng chứa một ô sẽ khiến người chơi mua trùng mảnh đất. " +
                "Vẫn tạo asset?",
                "Vẫn tạo", "Huỷ, để tôi khoanh lại");
            if (!tiep) return;
        }

        if (!Directory.Exists(AssetFolder))
        {
            Directory.CreateDirectory(AssetFolder);
            AssetDatabase.Refresh();
        }

        int stt = SoThuTuTrong();
        string ten  = $"{FilePrefix}{stt:00}";
        string path = AssetDatabase.GenerateUniqueAssetPath($"{AssetFolder}/{ten}.asset");

        var data = ScriptableObject.CreateInstance<LandRegionData>();
        data.regionId          = $"{IdPrefix}{stt:00}";
        data.displayName       = string.IsNullOrEmpty(tenHienThi) ? $"Khu Bắc {stt}" : tenHienThi;
        data.cellRects         = new List<RectInt>(ketQuaRects);
        data.unlockLevel       = unlockLevel;
        data.goldPrice         = tuTinhGia ? GiaTheoDienTich() : goldPrice;
        data.gemPrice          = gemPrice;
        data.clearSeconds      = clearSeconds;
        data.rushGemCost       = 0;
        data.workerCount       = 4;
        data.unlockedByDefault = false;
        data.materialCosts     = new List<BuildMaterialCost>();
        data.requiredRegionIds = new List<string>(requiredRegionIds);

        AssetDatabase.CreateAsset(data, path);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        assetVuaTao.Add(data);
        NapKhuHienCo();
        EditorGUIUtility.PingObject(data);

        Debug.Log($"[LandRegionAuthor] Đã tạo {path} — regionId={data.regionId}, " +
                  $"{ketQuaCells.Count} ô, {ketQuaRects.Count} rect, {data.goldPrice:n0} vàng.");
    }

    /// <summary>So thu tu North_ con trong tren dia.</summary>
    private int SoThuTuTrong()
    {
        for (int i = 0; i < 1000; i++)
        {
            string p = $"{AssetFolder}/{FilePrefix}{i:00}.asset";
            if (AssetDatabase.LoadAssetAtPath<LandRegionData>(p) == null) return i;
        }
        return 999;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GAN VAO SCENE  (KHONG LUU SCENE)
    // ═════════════════════════════════════════════════════════════════════

    private void GanVaoScene()
    {
        var mgr = Object.FindFirstObjectByType<LandExpansionManager>(FindObjectsInactive.Include);
        if (mgr == null)
        {
            EditorUtility.DisplayDialog(
                "Không tìm thấy LandExpansionManager",
                "Scene đang mở không có component LandExpansionManager nào " +
                "(kể cả object đang tắt).\n\n" +
                "Hãy mở scene bản đồ chính rồi bấm lại. Công cụ KHÔNG tự tạo manager " +
                "để tránh đẻ ra object thừa trong scene sai.",
                "OK");
            return;
        }

        Undo.RecordObject(mgr, "Thêm khu đất North_ vào LandExpansionManager");

        var so   = new SerializedObject(mgr);
        var list = so.FindProperty("regions");
        if (list == null || !list.isArray)
        {
            EditorUtility.DisplayDialog("Land Region Author",
                "Không tìm thấy mảng 'regions' trên LandExpansionManager.", "OK");
            return;
        }

        int them = 0;
        foreach (var data in assetVuaTao)
        {
            if (data == null) continue;

            bool daCo = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) { daCo = true; break; }
            if (daCo) continue;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
            them++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(mgr);

        EditorUtility.DisplayDialog(
            "Land Region Author",
            them == 0
                ? "Tất cả asset đã có sẵn trong danh sách regions — không thêm gì."
                : $"Đã thêm {them} khu vào LandExpansionManager.regions.\n\n" +
                  "SCENE CHƯA ĐƯỢC LƯU — sếp tự bấm Ctrl+S sau khi kiểm tra.",
            "OK");
    }
}
#endif
