#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ============================================================================
/// TU CHIA LO DAT THEO VUNG — Tools ▸ Farm Game ▸ Land: Tu chia lo theo vung
/// ============================================================================
///
/// MUC DICH
/// --------
/// Nguoi dung khoanh MOT VUNG (keo hop trong Scene view, hoac bam preset "ben tau"
/// / "duong ray"), tool tu:
///   1. Doi vung world -> tap o luoi iso (IsoGrid: o 300 x 150, goc Grid_Iso45).
///   2. Loai o "bi chiem": da thuoc LandRegionData nao, co tile tren tilemap
///      vat can (nuoc, cau tau, da, vach da, duong ray...), co SpriteRenderer /
///      Collider2D cong trinh, co HouseGrowthController / PlotController /
///      BoatDockSlot.
///   3. Xep cac lo hinh gan vuong (kich thuoc = median so o cua lo dang ban)
///      theo kieu quet hang, chua 1 hang o dem giua cac lo.
///   4. Tao asset LandRegionData tien to North_Tau_ / North_Ray_, gia theo dien
///      tich (so o x vang/o — cung cong thuc voi LandRegionAuthorTool), unlockLevel
///      = median lo hien co + 0..4 theo khoang cach toi tam ban do.
///   5. Gan vao LandExpansionManager.regions trong scene (Undo duoc, KHONG tu luu
///      scene). Bien bao do LandExpansionManager tu cam luc chay game.
///
/// KHONG dung toi lo cu, khong tao trung regionId, khong sua .unity bang tay.
/// Hinh hoc iso: xem LandRegionAuthorTool.cs / IsoGrid.cs
///     worldX = Ox + (cx - cy) * 150 ;  worldY = Oy + (cx + cy) * 75
///     cx = dx + dy ; cy = dy - dx   (dx = wx/300, dy = wy/150)
/// </summary>
public class LandRegionAutoFillTool : EditorWindow
{
    // ═════════════════════════════════════════════════════════════════════
    //  HANG SO
    // ═════════════════════════════════════════════════════════════════════

    private const string MenuPath    = "Tools/Farm Game/Land: Tu chia lo theo vung";
    private const string AssetFolder = "Assets/_Game/Farm/Land";

    private const int  MaxZoneCells   = 40000;  // chan tren de Unity khong treo
    private const int  MaxDrawCells   = 6000;

    /// <summary>Tilemap MAT DAT — KHONG coi la vat can (co tile o moi noi).</summary>
    private static readonly string[] TilemapMatDat =
    {
        "GroundBase_Dirt", "M\u00F3ng", "Mong", "Dat_Nen", "Dat_Nen (1)", "Co_Grass", "Tilemap_IsoGrass", "Tilemap_IsoDirt", "Tilemap_IsoDirtPatch",
        "Tilemap_IsoSand", "Tilemap_IsoStone", "Tilemap_LockedOverlay", "Tilemap_IsoFence"
    };

    /// <summary>Tilemap luon la VAT CAN (khop theo ten, khong phan biet hoa thuong).</summary>
    private static readonly string[] TilemapVatCanMacDinh =
    {
        "Water_Tilemap", "Underwater_Tilemap", "Tilemap_IsoDock", "Tilemap_IsoRock",
        "Tilemap_IsoCliff", "Tilemap_IsoDecor"
    };

    /// <summary>Tu khoa ten object / tilemap = duong ray, tau, ben tau (luon la vat can).</summary>
    private static readonly string[] TuKhoaVatCan =
    {
        "rail", "ray", "track", "duong", "train", "tau", "dock", "cliff", "vachda", "nuithac", "thacnuoc"
    };

    /// <summary>Sorting layer cua SpriteRenderer duoc coi la cong trinh / vat the.</summary>
    private static readonly string[] SortingLayerCongTrinh = { "CongTrinh", "Objects", "ObjectsFront" };

    // ═════════════════════════════════════════════════════════════════════
    //  VUNG
    // ═════════════════════════════════════════════════════════════════════

    private enum LoaiVung { BenTau = 0, DuongRay = 1, KeoHop = 2 }

    private class CauHinhVung
    {
        public string filePrefix;   // North_Tau_
        public string idPrefix;     // north_tau_
        public string displayPrefix;// Dock Lot
        public Vector2 tam;
        public Vector2 nuaKich;     // nua chieu rong / cao hop world
    }

    private LoaiVung loaiVung = LoaiVung.BenTau;

    // preset — nguoi dung sua duoc trong cua so
    private Vector2 tamBenTau   = new Vector2(-2000f, 1971f);
    private Vector2 nuaBenTau   = new Vector2(2200f, 2200f);
    private Vector2 tamDuongRay = new Vector2(-3750f, 2061f);
    private Vector2 nuaDuongRay = new Vector2(2000f, 2000f);

    // keo hop
    private bool    batKeoTrongScene;
    private bool    dangKeo;
    private Vector3 diemDau, diemCuoi;
    private bool    coHopWorld;
    private int     tienToKhiKeo = 0; // 0 = Tau, 1 = Ray

    // ═════════════════════════════════════════════════════════════════════
    //  THAM SO CHIA LO
    // ═════════════════════════════════════════════════════════════════════

    private int   loRong = 10, loCao = 10;     // kich thuoc lo (o)
    private int   hangDem = 1;                 // hang o dem giua lo
    private float tyLeKhungToiThieu = 0.90f;   // >= 90% o khung trong
    private float tyLeLoToiThieu    = 0.60f;   // lo con >= 60% o chuan
    private bool  demVoiLoCu = true;           // chua 1 o cach lo cu
    private bool  thuNhieuPhaXep = true;       // thu nhieu offset, lay ket qua nhieu lo nhat

    private float goldPerCell   = 800f;
    private int   unlockCoBan   = 30;
    private int   unlockTangMax = 4;
    private int   clearSeconds  = 300;
    private Vector2 tamBanDo    = Vector2.zero;
    private bool  tuTinhTamBanDo = true;

    private bool coiMoiTilemapKhacMatDatLaVatCan = true;
    private bool dungSpriteRenderer = true;
    private bool dungCollider2D     = true;
    private bool dungComponentDacBiet = true;

    // ═════════════════════════════════════════════════════════════════════
    //  DU LIEU DOC TU DIA / SCENE
    // ═════════════════════════════════════════════════════════════════════

    private class ThongTinKhu
    {
        public LandRegionData data;
        public string path;
        public HashSet<Vector2Int> cellSet = new HashSet<Vector2Int>();
    }

    private readonly List<ThongTinKhu> khuHienCo = new List<ThongTinKhu>();
    private readonly HashSet<Vector2Int> oCuaKhuCu = new HashSet<Vector2Int>();
    private int medianCells = 104, medianUnlock = 30;
    private float medianGoldPerCell = 800f;
    private string thongKe = "";

    // ═════════════════════════════════════════════════════════════════════
    //  KET QUA QUET
    // ═════════════════════════════════════════════════════════════════════

    private class LoDeXuat
    {
        public RectInt khung;
        public List<Vector2Int> cells = new List<Vector2Int>();
        public List<RectInt> rects = new List<RectInt>();
        public float khoangCachTam;
        public int unlockLevel;
        public int goldPrice;
    }

    private readonly HashSet<Vector2Int> oVung      = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> oBiChiem   = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, string> lyDoChiem = new Dictionary<Vector2Int, string>();
    private readonly List<LoDeXuat> loDeXuat        = new List<LoDeXuat>();
    private readonly Dictionary<string, int> demLyDo = new Dictionary<string, int>();
    private bool daQuet;
    private bool hienXemTruoc = true;
    private bool hienOBiChiem = true;
    private string logQuet = "";

    private Vector2 scroll;

    // ═════════════════════════════════════════════════════════════════════
    //  CUA VAO
    // ═════════════════════════════════════════════════════════════════════

    [MenuItem(MenuPath, false, 31)]
    private static void Mo()
    {
        var w = GetWindow<LandRegionAutoFillTool>(false, "Tu chia lo theo vung", true);
        w.minSize = new Vector2(460f, 640f);
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
    //  NAP KHU HIEN CO + THONG KE MEDIAN
    // ═════════════════════════════════════════════════════════════════════

    private static LandExpansionManager TimManager()
        => UnityEngine.Object.FindFirstObjectByType<LandExpansionManager>(FindObjectsInactive.Include);

    private void NapKhuHienCo()
    {
        khuHienCo.Clear();
        oCuaKhuCu.Clear();

        string[] guids = AssetDatabase.FindAssets("t:LandRegionData");
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var data = AssetDatabase.LoadAssetAtPath<LandRegionData>(path);
            if (data == null) continue;
            var k = new ThongTinKhu { data = data, path = path };
            foreach (var c in data.AllCells()) { k.cellSet.Add(c); oCuaKhuCu.Add(c); }
            khuHienCo.Add(k);
        }

        // Thong ke uu tien theo regions trong scene (lo DANG BAN), fallback = moi asset
        var mgr = TimManager();
        List<LandRegionData> mau = new List<LandRegionData>();
        if (mgr != null) foreach (var r in mgr.regions) if (r != null) mau.Add(r);
        string nguon = "LandExpansionManager.regions";
        if (mau.Count == 0) { foreach (var k in khuHienCo) mau.Add(k.data); nguon = "moi asset LandRegionData"; }

        var cells  = mau.Where(r => r.CellCount > 0 && !r.unlockedByDefault).Select(r => r.CellCount).ToList();
        var unlock = mau.Where(r => r.unlockLevel > 0 && !r.unlockedByDefault).Select(r => r.unlockLevel).ToList();
        var gpc    = mau.Where(r => r.CellCount > 0 && r.goldPrice > 0)
                        .Select(r => r.goldPrice / (float)r.CellCount).ToList();

        if (cells.Count > 0)  medianCells  = Mathf.Max(1, Mathf.RoundToInt(Median(cells.Select(v => (float)v))));
        if (unlock.Count > 0) medianUnlock = Mathf.RoundToInt(Median(unlock.Select(v => (float)v)));
        if (gpc.Count > 0)    medianGoldPerCell = Median(gpc);

        // kich thuoc lo gan vuong tu median
        int canh = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(medianCells)));
        loRong = canh;
        loCao  = Mathf.Max(2, Mathf.RoundToInt(medianCells / (float)canh));
        unlockCoBan = medianUnlock;
        goldPerCell = Mathf.Round(medianGoldPerCell);

        // tam ban do = trong tam moi o cua lo hien co
        if (oCuaKhuCu.Count > 0)
        {
            Vector2 s = Vector2.zero;
            foreach (var c in oCuaKhuCu) s += new Vector2(c.x, c.y);
            s /= oCuaKhuCu.Count;
            tamBanDo = IsoGrid.CellFloatToWorld(s);
        }

        thongKe = $"Nguon: {nguon} ({mau.Count} lo)\n" +
                  $"Median so o = {medianCells}  →  lo goi y {loRong} x {loCao} = {loRong * loCao} o\n" +
                  $"Median unlockLevel = {medianUnlock}   ·   median vang/o = {medianGoldPerCell:0.#}\n" +
                  $"Tam ban do (world) = ({tamBanDo.x:0}, {tamBanDo.y:0})";
    }

    private static float Median(IEnumerable<float> src)
    {
        var l = src.ToList();
        if (l.Count == 0) return 0f;
        l.Sort();
        int n = l.Count;
        return n % 2 == 1 ? l[n / 2] : (l[n / 2 - 1] + l[n / 2]) * 0.5f;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  VUNG HIEN TAI
    // ═════════════════════════════════════════════════════════════════════

    private CauHinhVung VungHienTai()
    {
        switch (loaiVung)
        {
            case LoaiVung.BenTau:
                return new CauHinhVung
                {
                    filePrefix = "North_Tau_", idPrefix = "north_tau_", displayPrefix = "Dock Lot",
                    tam = tamBenTau, nuaKich = nuaBenTau
                };
            case LoaiVung.DuongRay:
                return new CauHinhVung
                {
                    filePrefix = "North_Ray_", idPrefix = "north_ray_", displayPrefix = "Rail Lot",
                    tam = tamDuongRay, nuaKich = nuaDuongRay
                };
            default:
            {
                bool ray = tienToKhiKeo == 1;
                Vector2 mn = new Vector2(Mathf.Min(diemDau.x, diemCuoi.x), Mathf.Min(diemDau.y, diemCuoi.y));
                Vector2 mx = new Vector2(Mathf.Max(diemDau.x, diemCuoi.x), Mathf.Max(diemDau.y, diemCuoi.y));
                return new CauHinhVung
                {
                    filePrefix = ray ? "North_Ray_" : "North_Tau_",
                    idPrefix   = ray ? "north_ray_" : "north_tau_",
                    displayPrefix = ray ? "Rail Lot" : "Dock Lot",
                    tam = (mn + mx) * 0.5f,
                    nuaKich = (mx - mn) * 0.5f
                };
            }
        }
    }

    private static void HopVung(CauHinhVung v, out float xMin, out float xMax, out float yMin, out float yMax)
    {
        xMin = v.tam.x - v.nuaKich.x; xMax = v.tam.x + v.nuaKich.x;
        yMin = v.tam.y - v.nuaKich.y; yMax = v.tam.y + v.nuaKich.y;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HINH HOC — hop world -> tap o (tai dung tu LandRegionAuthorTool)
    // ═════════════════════════════════════════════════════════════════════

    private static void HopWorldSangO(float xMin, float xMax, float yMin, float yMax, HashSet<Vector2Int> cellsOut)
    {
        cellsOut.Clear();
        if (xMax - xMin < 1f || yMax - yMin < 1f) return;

        Vector2 c0 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMin, 0f));
        Vector2 c1 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMin, 0f));
        Vector2 c2 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMax, 0f));
        Vector2 c3 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMax, 0f));

        int cxMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x)));
        int cxMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x)));
        int cyMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y)));
        int cyMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y)));

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
                if (cellsOut.Count >= MaxZoneCells) return;
            }
        }
    }

    /// <summary>Gom tap o thanh RectInt rong 1 cot (bac thang) — giong LandRegionAuthorTool.</summary>
    private static void GomThanhRect(IEnumerable<Vector2Int> cells, List<RectInt> rectsOut)
    {
        rectsOut.Clear();
        var theoCot = new Dictionary<int, List<int>>();
        foreach (var c in cells)
        {
            if (!theoCot.TryGetValue(c.x, out var list)) { list = new List<int>(); theoCot[c.x] = list; }
            list.Add(c.y);
        }
        if (theoCot.Count == 0) return;

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

    // ═════════════════════════════════════════════════════════════════════
    //  QUET O BI CHIEM
    // ═════════════════════════════════════════════════════════════════════

    private static bool TenCoTuKhoa(string ten)
    {
        if (string.IsNullOrEmpty(ten)) return false;
        string t = ten.ToLowerInvariant();
        foreach (var k in TuKhoaVatCan)
        {
            if (k == "ray" && t.Contains("tray")) continue; // "Tray" khong phai duong ray
            if (t.Contains(k)) return true;
        }
        return false;
    }

    private static bool LaTilemapMatDat(string ten)
    {
        foreach (var m in TilemapMatDat)
            if (string.Equals(m, ten, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool LaTilemapVatCan(string ten)
    {
        string t = ten.ToLowerInvariant();
        foreach (var m in TilemapVatCanMacDinh)
            if (t.StartsWith(m.ToLowerInvariant())) return true;
        return TenCoTuKhoa(ten);
    }

    private static bool DuoiCanvas(Transform t)
    {
        while (t != null)
        {
            if (t.GetComponent<Canvas>() != null) return true;
            t = t.parent;
        }
        return false;
    }

    private void GhiChiem(Vector2Int c, string lyDo)
    {
        if (oBiChiem.Add(c))
        {
            lyDoChiem[c] = lyDo;
            demLyDo.TryGetValue(lyDo, out int n);
            demLyDo[lyDo] = n + 1;
        }
    }

    /// <summary>5 diem mau trong o kim cuong (tam + 4 diem giua tam va dinh).</summary>
    private static void DiemMauTrongO(Vector2Int c, Vector3[] outPts)
    {
        Vector3 t = IsoGrid.CellCenterToWorld(c);
        float hw = IsoGrid.CellWidth * 0.25f, hh = IsoGrid.CellHeight * 0.25f;
        outPts[0] = t;
        outPts[1] = new Vector3(t.x, t.y + hh, 0f);
        outPts[2] = new Vector3(t.x + hw, t.y, 0f);
        outPts[3] = new Vector3(t.x, t.y - hh, 0f);
        outPts[4] = new Vector3(t.x - hw, t.y, 0f);
    }

    /// <summary>Danh dau moi o cua vung nam trong hop bao world (chi xet phan giao).</summary>
    private void ChiemTheoBounds(Bounds b, string lyDo)
    {
        // hop bao world -> khoang o bao ngoai
        float xMin = b.min.x, xMax = b.max.x, yMin = b.min.y, yMax = b.max.y;
        if (b.size.x <= 0.01f && b.size.y <= 0.01f)
        {
            GhiChiemNeuTrongVung(IsoGrid.WorldToCell(b.center), lyDo);
            return;
        }
        Vector2 c0 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMin, 0f));
        Vector2 c1 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMin, 0f));
        Vector2 c2 = IsoGrid.WorldToCellFloat(new Vector3(xMin, yMax, 0f));
        Vector2 c3 = IsoGrid.WorldToCellFloat(new Vector3(xMax, yMax, 0f));
        int cxMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x)));
        int cxMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x)));
        int cyMin = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y)));
        int cyMax = Mathf.CeilToInt (Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y)));

        // vat qua to (vd nen troi) thi bo qua de khong xoa sach vung
        if ((long)(cxMax - cxMin) * (cyMax - cyMin) > 4L * MaxZoneCells) return;

        for (int cx = cxMin; cx <= cxMax; cx++)
            for (int cy = cyMin; cy <= cyMax; cy++)
            {
                var c = new Vector2Int(cx, cy);
                if (!oVung.Contains(c) || oBiChiem.Contains(c)) continue;
                Vector3 w = IsoGrid.CellCenterToWorld(c);
                if (w.x < xMin || w.x > xMax || w.y < yMin || w.y > yMax) continue;
                GhiChiem(c, lyDo);
            }
    }

    private void GhiChiemNeuTrongVung(Vector2Int c, string lyDo)
    {
        if (oVung.Contains(c)) GhiChiem(c, lyDo);
    }

    private void QuetOBiChiem()
    {
        oBiChiem.Clear();
        lyDoChiem.Clear();
        demLyDo.Clear();

        // (i) da thuoc lo nao (bat ky asset LandRegionData trong project)
        foreach (var c in oVung)
            if (oCuaKhuCu.Contains(c)) GhiChiem(c, "Lo dat da co");

        if (demVoiLoCu)
        {
            var them = new List<Vector2Int>();
            foreach (var c in oVung)
            {
                if (oBiChiem.Contains(c)) continue;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if ((dx != 0 || dy != 0) && oCuaKhuCu.Contains(new Vector2Int(c.x + dx, c.y + dy)))
                        { them.Add(c); dx = 2; break; }
            }
            foreach (var c in them) GhiChiem(c, "Dem canh lo cu");
        }

        // (ii) tilemap vat can
        var pts = new Vector3[5];
        var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var tilemapDung = new List<Tilemap>();
        foreach (var tm in tilemaps)
        {
            if (tm == null) continue;
            string ten = tm.gameObject.name;
            if (LaTilemapMatDat(ten)) continue;
            if (LaTilemapVatCan(ten) || coiMoiTilemapKhacMatDatLaVatCan) tilemapDung.Add(tm);
        }
        foreach (var tm in tilemapDung)
        {
            string lyDo = "Tilemap " + tm.gameObject.name;
            foreach (var c in oVung)
            {
                if (oBiChiem.Contains(c)) continue;
                DiemMauTrongO(c, pts);
                for (int i = 0; i < pts.Length; i++)
                {
                    if (tm.HasTile(tm.WorldToCell(pts[i]))) { GhiChiem(c, lyDo); break; }
                }
            }
        }

        // (iii) SpriteRenderer cong trinh / vat the (theo sorting layer hoac tu khoa ten)
        if (dungSpriteRenderer)
        {
            var srs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var sr in srs)
            {
                if (sr == null || sr.sprite == null) continue;
                if (DuoiCanvas(sr.transform)) continue;
                bool theoLayer = Array.IndexOf(SortingLayerCongTrinh, sr.sortingLayerName) >= 0;
                bool theoTen   = TenCoTuKhoa(sr.gameObject.name) || (sr.transform.parent != null && TenCoTuKhoa(sr.transform.parent.name));
                if (!theoLayer && !theoTen) continue;
                if (sr.GetComponent<LandRegionSign>() != null || sr.GetComponentInParent<LandRegionSignBoard>() != null) continue;
                ChiemTheoBounds(sr.bounds, theoTen ? "Sprite ray/tau/ben (" + sr.gameObject.name + ")" : "Sprite cong trinh");
            }
        }

        // (iv) Collider2D
        if (dungCollider2D)
        {
            var cols = UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var col in cols)
            {
                if (col == null) continue;
                if (DuoiCanvas(col.transform)) continue;
                if (col.gameObject.layer == LayerMask.NameToLayer("UI")) continue;
                if (col.GetComponentInParent<LandRegionSign>() != null || col.GetComponentInParent<LandRegionSignBoard>() != null) continue;
                ChiemTheoBounds(col.bounds, "Collider2D");
            }
        }

        // (v) component dac biet
        if (dungComponentDacBiet)
        {
            foreach (var h in UnityEngine.Object.FindObjectsByType<HouseGrowthController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ChiemTheoComponent(h, "HouseGrowthController");
            foreach (var p in UnityEngine.Object.FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ChiemTheoComponent(p, "PlotController");
            foreach (var d in UnityEngine.Object.FindObjectsByType<BoatDockSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ChiemTheoComponent(d, "BoatDockSlot");
        }
    }

    private void ChiemTheoComponent(Component comp, string lyDo)
    {
        if (comp == null) return;
        var rs = comp.GetComponentsInChildren<Renderer>(true);
        bool co = false;
        foreach (var r in rs)
        {
            if (r == null) continue;
            co = true;
            ChiemTheoBounds(r.bounds, lyDo);
        }
        if (!co) GhiChiemNeuTrongVung(IsoGrid.WorldToCell(comp.transform.position), lyDo);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  XEP LO
    // ═════════════════════════════════════════════════════════════════════

    private void QuetVaXepLo()
    {
        NapKhuHienCoGiuThamSo();

        var v = VungHienTai();
        HopVung(v, out float xMin, out float xMax, out float yMin, out float yMax);
        HopWorldSangO(xMin, xMax, yMin, yMax, oVung);
        loDeXuat.Clear();
        daQuet = true;

        if (oVung.Count == 0)
        {
            logQuet = "Vung khong chua o nao — kiem tra tam / ban kinh hoac keo lai hop.";
            SceneView.RepaintAll();
            return;
        }

        QuetOBiChiem();

        int W = Mathf.Max(1, loRong), H = Mathf.Max(1, loCao), dem = Mathf.Max(0, hangDem);
        int bx = W + dem, by = H + dem;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in oVung)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }

        List<LoDeXuat> tot = null;
        int soPhaX = thuNhieuPhaXep ? bx : 1, soPhaY = thuNhieuPhaXep ? by : 1;
        for (int px = 0; px < soPhaX; px++)
        {
            for (int py = 0; py < soPhaY; py++)
            {
                var kq = XepMotPha(minX - bx + px, minY - by + py, maxX, maxY, W, H, bx, by);
                if (tot == null || kq.Count > tot.Count ||
                    (kq.Count == tot.Count && TongO(kq) > TongO(tot)))
                    tot = kq;
            }
        }
        if (tot != null) loDeXuat.AddRange(tot);

        // sap theo khoang cach tam ban do (gan -> xa) de danh so va tinh unlock
        Vector2 tam = tamBanDo;
        foreach (var lo in loDeXuat)
        {
            Vector2 s = Vector2.zero;
            foreach (var c in lo.cells) s += new Vector2(c.x, c.y);
            s /= lo.cells.Count;
            Vector3 w = IsoGrid.CellFloatToWorld(s);
            lo.khoangCachTam = Vector2.Distance(new Vector2(w.x, w.y), tam);
            lo.goldPrice = GiaTheoDienTich(lo.cells.Count);
        }
        loDeXuat.Sort((a, b) => a.khoangCachTam.CompareTo(b.khoangCachTam));

        if (loDeXuat.Count > 0)
        {
            float dMin = loDeXuat[0].khoangCachTam, dMax = loDeXuat[loDeXuat.Count - 1].khoangCachTam;
            foreach (var lo in loDeXuat)
            {
                float t = dMax - dMin > 1f ? (lo.khoangCachTam - dMin) / (dMax - dMin) : 0f;
                lo.unlockLevel = Mathf.Max(0, unlockCoBan + Mathf.RoundToInt(t * Mathf.Max(0, unlockTangMax)));
                GomThanhRect(lo.cells, lo.rects);
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.Append($"Vung: {oVung.Count} o · bi chiem: {oBiChiem.Count} o · trong: {oVung.Count - oBiChiem.Count} o\n");
        sb.Append($"Lo de xuat: {loDeXuat.Count} (khung {W}x{H}, dem {dem})\n");
        foreach (var kv in demLyDo.OrderByDescending(k => k.Value).Take(8))
            sb.Append($"   · {kv.Key}: {kv.Value} o\n");
        logQuet = sb.ToString().TrimEnd();

        SceneView.RepaintAll();
    }

    private static int TongO(List<LoDeXuat> l)
    {
        int t = 0;
        foreach (var lo in l) t += lo.cells.Count;
        return t;
    }

    private List<LoDeXuat> XepMotPha(int startX, int startY, int maxX, int maxY, int W, int H, int bx, int by)
    {
        var kq = new List<LoDeXuat>();
        int chuan = W * H;
        int nguongKhung = Mathf.CeilToInt(chuan * Mathf.Clamp01(tyLeKhungToiThieu));
        int nguongLo    = Mathf.CeilToInt(chuan * Mathf.Clamp01(tyLeLoToiThieu));

        for (int oy = startY; oy <= maxY; oy += by)
        {
            for (int ox = startX; ox <= maxX; ox += bx)
            {
                var lo = new LoDeXuat { khung = new RectInt(ox, oy, W, H) };
                for (int x = ox; x < ox + W; x++)
                    for (int y = oy; y < oy + H; y++)
                    {
                        var c = new Vector2Int(x, y);
                        if (oVung.Contains(c) && !oBiChiem.Contains(c)) lo.cells.Add(c);
                    }
                if (lo.cells.Count < nguongKhung) continue;
                if (lo.cells.Count < nguongLo) continue;
                kq.Add(lo);
            }
        }
        return kq;
    }

    private int GiaTheoDienTich(int soO) => Mathf.Max(0, Mathf.RoundToInt(soO * goldPerCell));

    /// <summary>Nap lai khu tu dia nhung GIU tham so nguoi dung da sua trong cua so.</summary>
    private void NapKhuHienCoGiuThamSo()
    {
        int r = loRong, h = loCao, u = unlockCoBan; float g = goldPerCell; Vector2 t = tamBanDo;
        NapKhuHienCo();
        loRong = r; loCao = h; unlockCoBan = u; goldPerCell = g;
        if (!tuTinhTamBanDo) tamBanDo = t;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SCENE VIEW
    // ═════════════════════════════════════════════════════════════════════

    private void VeTrongScene(SceneView sv)
    {
        if (loaiVung == LoaiVung.KeoHop && batKeoTrongScene) XuLyKeoHop(sv);
        if (hienXemTruoc) VeXemTruoc();
    }

    private void XuLyKeoHop(SceneView sv)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);
        Event e = Event.current;
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);

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
                coHopWorld = true;
                e.Use();
                sv.Repaint();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != id) break;
                GUIUtility.hotControl = 0;
                dangKeo = false;
                diemCuoi = ChuotRaWorld(e.mousePosition);
                coHopWorld = (diemCuoi - diemDau).sqrMagnitude > 1f;
                daQuet = false;
                loDeXuat.Clear();
                e.Use();
                Repaint();
                sv.Repaint();
                break;
        }
    }

    private static Vector3 ChuotRaWorld(Vector2 mousePos)
    {
        Ray r = HandleUtility.GUIPointToWorldRay(mousePos);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(r, out float d)) return r.GetPoint(d);
        return new Vector3(r.origin.x, r.origin.y, 0f);
    }

    private void VeODiamond(Vector2Int c, Vector3[] quad, Color nen, Color vien, float hw, float hh)
    {
        Vector3 t = IsoGrid.CellCenterToWorld(c);
        quad[0] = new Vector3(t.x,      t.y + hh, 0f);
        quad[1] = new Vector3(t.x + hw, t.y,      0f);
        quad[2] = new Vector3(t.x,      t.y - hh, 0f);
        quad[3] = new Vector3(t.x - hw, t.y,      0f);
        Handles.DrawSolidRectangleWithOutline(quad, nen, vien);
    }

    private void VeXemTruoc()
    {
        // hop vung
        var v = VungHienTai();
        if (loaiVung != LoaiVung.KeoHop || dangKeo || coHopWorld)
        {
            HopVung(v, out float xMin, out float xMax, out float yMin, out float yMax);
            var goc = new[]
            {
                new Vector3(xMin, yMin, 0f), new Vector3(xMax, yMin, 0f),
                new Vector3(xMax, yMax, 0f), new Vector3(xMin, yMax, 0f)
            };
            Handles.DrawSolidRectangleWithOutline(goc, new Color(0.2f, 0.7f, 1f, 0.04f), new Color(0.2f, 0.8f, 1f, 0.9f));
            Handles.Label(new Vector3(xMin, yMax, 0f), $"  {v.displayPrefix} zone", EditorStyles.whiteBoldLabel);
        }

        if (!daQuet) return;

        float hw = IsoGrid.CellWidth * 0.5f, hh = IsoGrid.CellHeight * 0.5f;
        var quad = new Vector3[4];
        int ve = 0;

        if (hienOBiChiem)
        {
            var nenDo = new Color(1f, 0.25f, 0.2f, 0.18f);
            var vienDo = new Color(1f, 0.3f, 0.25f, 0.45f);
            foreach (var c in oBiChiem)
            {
                if (++ve > MaxDrawCells) break;
                VeODiamond(c, quad, nenDo, vienDo, hw, hh);
            }
        }

        var nenXanh = new Color(0.3f, 1f, 0.4f, 0.28f);
        var vienXanh = new Color(0.35f, 1f, 0.5f, 0.8f);
        var style = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white }, fontSize = 12 };

        for (int i = 0; i < loDeXuat.Count; i++)
        {
            var lo = loDeXuat[i];
            foreach (var c in lo.cells)
            {
                if (++ve > MaxDrawCells * 2) break;
                VeODiamond(c, quad, nenXanh, vienXanh, hw, hh);
            }
            Vector2 s = Vector2.zero;
            foreach (var c in lo.cells) s += new Vector2(c.x, c.y);
            s /= Mathf.Max(1, lo.cells.Count);
            Handles.Label(IsoGrid.CellFloatToWorld(s),
                $"{v.displayPrefix} {i + 1:00}\n{lo.cells.Count} o · Lv{lo.unlockLevel} · {lo.goldPrice:n0}", style);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GIAO DIEN
    // ═════════════════════════════════════════════════════════════════════

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Tool tao them lo dat ban trong vung con trong. Asset ghi ra Assets/_Game/Farm/Land voi tien to " +
            "North_Tau_ (ben tau) / North_Ray_ (duong ray) — khong dung toi lo cu, khong tao trung regionId.",
            MessageType.Info);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("0 · Thong ke lo hien co", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(thongKe, MessageType.None);
        if (GUILayout.Button("Nap lai thong ke tu dia / scene")) { NapKhuHienCo(); daQuet = false; loDeXuat.Clear(); }

        EditorGUILayout.Space(4f);
        VeChonVung();
        EditorGUILayout.Space(4f);
        VeThamSo();
        EditorGUILayout.Space(4f);
        VeNutHanhDong();

        EditorGUILayout.EndScrollView();
    }

    private void VeChonVung()
    {
        EditorGUILayout.LabelField("1 · Chon vung", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Toggle(loaiVung == LoaiVung.BenTau, "Vung ben tau", EditorStyles.miniButtonLeft)) loaiVung = LoaiVung.BenTau;
            if (GUILayout.Toggle(loaiVung == LoaiVung.DuongRay, "Vung duong ray", EditorStyles.miniButtonMid)) loaiVung = LoaiVung.DuongRay;
            if (GUILayout.Toggle(loaiVung == LoaiVung.KeoHop, "Keo hop trong Scene", EditorStyles.miniButtonRight)) loaiVung = LoaiVung.KeoHop;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            switch (loaiVung)
            {
                case LoaiVung.BenTau:
                    tamBenTau = EditorGUILayout.Vector2Field("Tam (world)", tamBenTau);
                    nuaBenTau = EditorGUILayout.Vector2Field("Nua kich thuoc (ban kinh X/Y)", nuaBenTau);
                    break;
                case LoaiVung.DuongRay:
                    tamDuongRay = EditorGUILayout.Vector2Field("Tam (world)", tamDuongRay);
                    nuaDuongRay = EditorGUILayout.Vector2Field("Nua kich thuoc (ban kinh X/Y)", nuaDuongRay);
                    break;
                default:
                    tienToKhiKeo = EditorGUILayout.Popup("Loai lo khi keo", tienToKhiKeo, new[] { "Tau (North_Tau_)", "Ray (North_Ray_)" });
                    batKeoTrongScene = EditorGUILayout.ToggleLeft("BAT keo hop trong Scene view (giu chuot trai va keo)", batKeoTrongScene);
                    EditorGUILayout.HelpBox("Khi bat, chuot trai trong Scene view dung de KEO HOP, khong chon object — tat lai khi xong.", MessageType.None);
                    if (coHopWorld)
                        EditorGUILayout.LabelField(
                            $"Hop world: ({Mathf.Min(diemDau.x, diemCuoi.x):0}, {Mathf.Min(diemDau.y, diemCuoi.y):0}) → " +
                            $"({Mathf.Max(diemDau.x, diemCuoi.x):0}, {Mathf.Max(diemDau.y, diemCuoi.y):0})");
                    break;
            }
            if (GUILayout.Button("Dua Scene view toi vung"))
            {
                var v = VungHienTai();
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                {
                    sv.in2DMode = true;
                    sv.LookAt(new Vector3(v.tam.x, v.tam.y, 0f), Quaternion.identity, Mathf.Max(v.nuaKich.x, v.nuaKich.y) * 1.3f);
                }
            }
        }
        if (EditorGUI.EndChangeCheck()) { daQuet = false; loDeXuat.Clear(); SceneView.RepaintAll(); }
    }

    private void VeThamSo()
    {
        EditorGUILayout.LabelField("2 · Tham so chia lo", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUILayout.HorizontalScope())
        {
            loRong = Mathf.Max(1, EditorGUILayout.IntField("Lo rong (o cx)", loRong));
            loCao  = Mathf.Max(1, EditorGUILayout.IntField("Lo cao (o cy)", loCao));
        }
        EditorGUILayout.LabelField($"   = {loRong * loCao} o / lo (median lo hien co: {medianCells} o)");
        hangDem = Mathf.Clamp(EditorGUILayout.IntField("Hang o dem giua lo", hangDem), 0, 5);
        tyLeKhungToiThieu = EditorGUILayout.Slider("Khung phai trong >= (%)", tyLeKhungToiThieu * 100f, 30f, 100f) / 100f;
        tyLeLoToiThieu    = EditorGUILayout.Slider("Lo con lai >= (% chuan)", tyLeLoToiThieu * 100f, 10f, 100f) / 100f;
        demVoiLoCu     = EditorGUILayout.ToggleLeft("Chua 1 o dem voi lo cu", demVoiLoCu);
        thuNhieuPhaXep = EditorGUILayout.ToggleLeft("Thu nhieu vi tri bat dau, lay ket qua nhieu lo nhat", thuNhieuPhaXep);

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Gia & cap mo khoa", EditorStyles.miniBoldLabel);
        goldPerCell   = Mathf.Max(0f, EditorGUILayout.FloatField("Vang / o (gia = so o x vang/o)", goldPerCell));
        unlockCoBan   = Mathf.Max(0, EditorGUILayout.IntField("unlockLevel co ban (median)", unlockCoBan));
        unlockTangMax = Mathf.Clamp(EditorGUILayout.IntField("Tang toi da theo khoang cach", unlockTangMax), 0, 20);
        clearSeconds  = Mathf.Max(0, EditorGUILayout.IntField("clearSeconds", clearSeconds));
        tuTinhTamBanDo = EditorGUILayout.ToggleLeft("Tam ban do = trong tam lo hien co", tuTinhTamBanDo);
        using (new EditorGUI.DisabledScope(tuTinhTamBanDo))
            tamBanDo = EditorGUILayout.Vector2Field("Tam ban do (world)", tamBanDo);

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Vat can", EditorStyles.miniBoldLabel);
        coiMoiTilemapKhacMatDatLaVatCan = EditorGUILayout.ToggleLeft(
            "Moi tilemap KHONG phai mat dat la vat can (tat = chi Water/Underwater/Dock/Rock/Cliff/Decor + ten co Rail/Ray/Track/Duong)",
            coiMoiTilemapKhacMatDatLaVatCan);
        dungSpriteRenderer   = EditorGUILayout.ToggleLeft("SpriteRenderer sorting layer CongTrinh/Objects/ObjectsFront + ten co Rail/Ray/Train/Tau/Dock", dungSpriteRenderer);
        dungCollider2D       = EditorGUILayout.ToggleLeft("Collider2D (moi collider trong scene, tru UI va bien bao)", dungCollider2D);
        dungComponentDacBiet = EditorGUILayout.ToggleLeft("HouseGrowthController / PlotController / BoatDockSlot", dungComponentDacBiet);
        if (EditorGUI.EndChangeCheck()) { daQuet = false; loDeXuat.Clear(); SceneView.RepaintAll(); }
    }

    private void VeNutHanhDong()
    {
        EditorGUILayout.LabelField("3 · Hanh dong", EditorStyles.boldLabel);

        bool coVung = loaiVung != LoaiVung.KeoHop || coHopWorld;
        using (new EditorGUI.DisabledScope(!coVung))
        {
            if (GUILayout.Button("XEM TRUOC — quet vung va ve lo de xuat trong Scene view", GUILayout.Height(30f)))
                QuetVaXepLo();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            hienXemTruoc = EditorGUILayout.ToggleLeft("Hien xem truoc", hienXemTruoc);
            hienOBiChiem = EditorGUILayout.ToggleLeft("Hien o bi chiem (do)", hienOBiChiem);
        }
        if (GUI.changed) SceneView.RepaintAll();

        if (!string.IsNullOrEmpty(logQuet))
            EditorGUILayout.HelpBox(logQuet, daQuet && loDeXuat.Count > 0 ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(4f);
        using (new EditorGUI.DisabledScope(!daQuet || loDeXuat.Count == 0))
        {
            if (GUILayout.Button($"TAO {loDeXuat.Count} lo → asset + gan vao LandExpansionManager.regions", GUILayout.Height(30f)))
                TaoLo();
        }

        EditorGUILayout.Space(6f);
        var mauCu = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("XOA lo do tool nay tao (North_Tau_* / North_Ray_*)", GUILayout.Height(24f)))
            XoaLoCuaTool();
        GUI.backgroundColor = mauCu;

        EditorGUILayout.HelpBox(
            "Tao lo: asset duoc ghi ra dia, manager duoc Undo.RecordObject — Ctrl+Z go duoc phan gan vao scene, " +
            "asset thi dung nut XOA. Scene KHONG tu luu — tu bam Ctrl+S sau khi kiem tra.",
            MessageType.None);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAO LO
    // ═════════════════════════════════════════════════════════════════════

    private void TaoLo()
    {
        if (loDeXuat.Count == 0) return;
        var v = VungHienTai();

        var mgr = TimManager();
        if (mgr == null)
        {
            EditorUtility.DisplayDialog("Khong tim thay LandExpansionManager",
                "Scene dang mo khong co LandExpansionManager. Mo SCN_Farm roi bam lai.", "OK");
            return;
        }

        int tongO = TongO(loDeXuat);
        if (!EditorUtility.DisplayDialog("Tao lo dat",
                $"Tao {loDeXuat.Count} lo ({tongO} o) tien to {v.filePrefix}XX trong {AssetFolder}\n" +
                $"va gan vao LandExpansionManager.regions cua scene '{mgr.gameObject.scene.name}'?\n\n" +
                $"Gia = so o x {goldPerCell:0.#} vang · unlockLevel {unlockCoBan}..{unlockCoBan + unlockTangMax}",
                "Tao", "Huy"))
            return;

        if (!Directory.Exists(AssetFolder))
        {
            Directory.CreateDirectory(AssetFolder);
            AssetDatabase.Refresh();
        }

        // regionId da co trong project -> khong tao trung
        var idDaCo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in khuHienCo) if (k.data != null && !string.IsNullOrEmpty(k.data.regionId)) idDaCo.Add(k.data.regionId);
        foreach (var r in mgr.regions) if (r != null && !string.IsNullOrEmpty(r.regionId)) idDaCo.Add(r.regionId);

        var taoMoi = new List<LandRegionData>();
        int stt = 1;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var lo in loDeXuat)
            {
                // tim so thu tu trong (ca ten file lan regionId)
                string path; string id;
                while (true)
                {
                    id = $"{v.idPrefix}{stt:00}";
                    path = $"{AssetFolder}/{v.filePrefix}{stt:00}.asset";
                    if (!idDaCo.Contains(id) && AssetDatabase.LoadAssetAtPath<LandRegionData>(path) == null && !File.Exists(path)) break;
                    stt++;
                    if (stt > 999) break;
                }
                if (stt > 999) break;
                idDaCo.Add(id);

                var data = ScriptableObject.CreateInstance<LandRegionData>();
                data.regionId          = id;
                data.displayName       = $"{v.displayPrefix} {stt:00}";
                data.cellRects         = new List<RectInt>(lo.rects);
                data.unlockLevel       = lo.unlockLevel;
                data.goldPrice         = lo.goldPrice;
                data.gemPrice          = 0;
                data.clearSeconds      = clearSeconds;
                data.rushGemCost       = 0;
                data.workerCount       = 4;
                data.unlockedByDefault = false;
                data.materialCosts     = new List<BuildMaterialCost>();
                data.requiredRegionIds = new List<string>();

                AssetDatabase.CreateAsset(data, path);
                EditorUtility.SetDirty(data);
                taoMoi.Add(data);
                stt++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // gan vao scene
        Undo.RecordObject(mgr, "Tu chia lo: them lo vao LandExpansionManager");
        var so = new SerializedObject(mgr);
        var list = so.FindProperty("regions");
        int them = 0;
        if (list != null && list.isArray)
        {
            foreach (var data in taoMoi)
            {
                bool daCo = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) { daCo = true; break; }
                if (daCo) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
                them++;
            }
            so.ApplyModifiedProperties();
        }
        EditorUtility.SetDirty(mgr);
        EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);

        Debug.Log($"[LandRegionAutoFill] Da tao {taoMoi.Count} lo ({v.filePrefix}), gan {them} vao regions. " +
                  $"Tong {tongO} o, gia {goldPerCell:0.#} vang/o.");
        foreach (var d in taoMoi)
            Debug.Log($"[LandRegionAutoFill]   {d.name}: regionId={d.regionId}, {d.CellCount} o, {d.cellRects.Count} rect, Lv{d.unlockLevel}, {d.goldPrice:n0} vang");

        NapKhuHienCoGiuThamSo();
        daQuet = false;
        loDeXuat.Clear();
        logQuet = $"Da tao {taoMoi.Count} lo, gan {them} vao regions. Scene chua luu (Ctrl+S).";
        if (taoMoi.Count > 0) EditorGUIUtility.PingObject(taoMoi[0]);
        SceneView.RepaintAll();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  XOA LO DO TOOL TAO
    // ═════════════════════════════════════════════════════════════════════

    private void XoaLoCuaTool()
    {
        NapKhuHienCoGiuThamSo();
        var xoa = new List<ThongTinKhu>();
        foreach (var k in khuHienCo)
        {
            if (k.data == null) continue;
            string f = Path.GetFileNameWithoutExtension(k.path);
            bool theoTen = f.StartsWith("North_Tau_", StringComparison.OrdinalIgnoreCase) ||
                           f.StartsWith("North_Ray_", StringComparison.OrdinalIgnoreCase);
            bool theoId  = !string.IsNullOrEmpty(k.data.regionId) &&
                           (k.data.regionId.StartsWith("north_tau_", StringComparison.OrdinalIgnoreCase) ||
                            k.data.regionId.StartsWith("north_ray_", StringComparison.OrdinalIgnoreCase));
            if (theoTen && theoId) xoa.Add(k);
        }

        if (xoa.Count == 0)
        {
            EditorUtility.DisplayDialog("Xoa lo", "Khong co asset North_Tau_* / North_Ray_* nao.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Xoa lo do tool tao",
                $"Xoa {xoa.Count} asset (chi North_Tau_* / North_Ray_*) va go khoi LandExpansionManager.regions?\n\n" +
                "XOA ASSET KHONG UNDO DUOC. Lo cu (Lot_*, North_XX, Land_*) khong bi dung.\n\n  • " +
                string.Join("\n  • ", xoa.Select(k => Path.GetFileName(k.path))),
                "Xoa", "Huy"))
            return;

        var mgr = TimManager();
        if (mgr != null)
        {
            Undo.RecordObject(mgr, "Tu chia lo: go lo khoi LandExpansionManager");
            var so = new SerializedObject(mgr);
            var list = so.FindProperty("regions");
            if (list != null && list.isArray)
            {
                var set = new HashSet<UnityEngine.Object>(xoa.Select(k => (UnityEngine.Object)k.data));
                for (int i = list.arraySize - 1; i >= 0; i--)
                {
                    var el = list.GetArrayElementAtIndex(i);
                    if (el.objectReferenceValue != null && set.Contains(el.objectReferenceValue))
                    {
                        el.objectReferenceValue = null;
                        list.DeleteArrayElementAtIndex(i);
                    }
                }
                so.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(mgr);
            EditorSceneManager.MarkSceneDirty(mgr.gameObject.scene);
        }

        int daXoa = 0;
        foreach (var k in xoa)
            if (AssetDatabase.DeleteAsset(k.path)) daXoa++;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[LandRegionAutoFill] Da xoa {daXoa} asset lo do tool tao va go khoi regions.");
        NapKhuHienCoGiuThamSo();
        daQuet = false;
        loDeXuat.Clear();
        logQuet = $"Da xoa {daXoa} lo. Scene chua luu (Ctrl+S).";
        SceneView.RepaintAll();
    }
}
#endif
