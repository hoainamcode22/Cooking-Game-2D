using UnityEngine;

/// <summary>
/// ============================================================================
/// ISO GRID V1 — TOAN LUOI ISOMETRIC DUNG CHUNG CHO CA GAME
/// ============================================================================
///
/// VI SAO CO FILE NAY
/// ------------------
/// Truoc day PlacementManager snap theo LUOI VUONG rieng (CELL = 100 world, goc (0,0)),
/// trong khi nen ban do da chuyen sang Grid_Iso45 — o KIM CUONG 150 x 75 world.
/// Hai luoi khong the trung nhau => cong trinh khong bao gio khop o nen, va hai cong
/// trinh canh nhau luon ho mot khoang le.
///
/// IsoGrid la NGUON SU THAT DUY NHAT ve toa do o ke tu ban nay. Moi he thong
/// (PlacementManager, ObjectDragHandler, ConstructionManager, LandExpansionManager)
/// deu phai goi qua day, khong tu che cong thuc nua.
///
/// QUY UOC
/// -------
///   • O luoi la Vector2Int (cx, cy) theo he Isometric cua Unity:
///       +cx  = huong LEN-PHAI tren man hinh
///       +cy  = huong LEN-TRAI tren man hinh
///   • Footprint N x M = RectInt(ox, oy, N, M) — vung o hinh thoi.
///   • CENTER = tam hinh hoc cua vung o (dung cho tham nen, gian giao, VFX).
///   • ANCHOR = "chan" cong trinh = dinh NAM (thap nhat) cua vung o.
///     transform.position cua cong trinh = ANCHOR (giu nguyen quy uoc cu cua du an,
///     vi art co pivot o day sprite).
///
/// BAT BIEN BAT BUOC (da chung minh trong SnapCenter):
///     Snap(Snap(p)) == Snap(p)
/// Nho vay keo cong trinh ra roi tha lai KHONG lam no dich nua o.
/// </summary>
public static class IsoGrid
{
    // ─────────────────────────────────────────────────────────────────────
    // THAM SO LUOI — tu dong doc tu Grid_Iso45 trong scene, co fallback
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Ten GameObject chua component Grid cua ban do iso.</summary>
    public const string IsoGridObjectName = "Grid_Iso45";

    /// <summary>Chieu RONG mot o kim cuong (world). Fallback khi chua tim thay Grid.</summary>
    public const float FallbackCellWidth = 150f;

    /// <summary>Chieu CAO mot o kim cuong (world). Fallback khi chua tim thay Grid.</summary>
    public const float FallbackCellHeight = 75f;

    private static Grid _grid;
    private static bool _searched;

    /// <summary>Grid thuc trong scene (co the null neu scene chua co Grid_Iso45).</summary>
    public static Grid SceneGrid
    {
        get
        {
            if (_grid != null) return _grid;
            if (_searched && _grid == null) TryFind();   // thu lai khi scene doi
            else if (!_searched) TryFind();
            return _grid;
        }
    }

    private static void TryFind()
    {
        _searched = true;
        var go = GameObject.Find(IsoGridObjectName);
        if (go != null) _grid = go.GetComponent<Grid>();
        if (_grid == null)
        {
            // du phong: lay Grid isometric dau tien tim thay
            foreach (var g in Object.FindObjectsByType<Grid>(FindObjectsSortMode.None))
            {
                if (g.cellLayout == GridLayout.CellLayout.Isometric ||
                    g.cellLayout == GridLayout.CellLayout.IsometricZAsY)
                {
                    _grid = g;
                    break;
                }
            }
        }
    }

    /// <summary>Goi khi doi scene / doi Grid de xoa cache.</summary>
    public static void ResetCache()
    {
        _grid = null;
        _searched = false;
    }

    /// <summary>Chieu rong mot o (world).</summary>
    public static float CellWidth
    {
        get
        {
            var g = SceneGrid;
            if (g == null) return FallbackCellWidth;
            return g.cellSize.x * g.transform.lossyScale.x;
        }
    }

    /// <summary>Chieu cao mot o (world).</summary>
    public static float CellHeight
    {
        get
        {
            var g = SceneGrid;
            if (g == null) return FallbackCellHeight;
            return g.cellSize.y * g.transform.lossyScale.y;
        }
    }

    /// <summary>Goc luoi (world) — vi tri cua Grid trong scene.</summary>
    public static Vector2 Origin
    {
        get
        {
            var g = SceneGrid;
            return g == null ? Vector2.zero : (Vector2)g.transform.position;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // CHUYEN DOI WORLD <-> O (dang thuc, chua lam tron)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// World -> toa do o dang THUC (chua lam tron).
    ///   cx = dx/W + dy/H
    ///   cy = dy/H - dx/W
    /// (suy nguoc tu: worldX = (cx-cy)*W/2 ; worldY = (cx+cy)*H/2)
    /// </summary>
    public static Vector2 WorldToCellFloat(Vector3 world)
    {
        float w = CellWidth, h = CellHeight;
        Vector2 o = Origin;
        float dx = (world.x - o.x) / w;
        float dy = (world.y - o.y) / h;
        return new Vector2(dx + dy, dy - dx);
    }

    /// <summary>Toa do o dang thuc -> world.</summary>
    public static Vector3 CellFloatToWorld(Vector2 cell)
    {
        float w = CellWidth, h = CellHeight;
        Vector2 o = Origin;
        return new Vector3(
            o.x + (cell.x - cell.y) * w * 0.5f,
            o.y + (cell.x + cell.y) * h * 0.5f,
            0f);
    }

    /// <summary>O luoi chua mot diem world.</summary>
    public static Vector2Int WorldToCell(Vector3 world)
    {
        Vector2 f = WorldToCellFloat(world);
        return new Vector2Int(Mathf.FloorToInt(f.x + 0.5f), Mathf.FloorToInt(f.y + 0.5f));
    }

    /// <summary>Tam world cua mot o luoi.</summary>
    public static Vector3 CellCenterToWorld(Vector2Int cell)
        => CellFloatToWorld(new Vector2(cell.x, cell.y));

    // ─────────────────────────────────────────────────────────────────────
    // FOOTPRINT N x M
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Nua chieu sau (world, truc Y) cua vung o N x M — tu tam xuong dinh Nam.</summary>
    public static float HalfDepth(Vector2Int size)
    {
        int n = Mathf.Max(1, size.x);
        int m = Mathf.Max(1, size.y);
        return (n + m) * CellHeight * 0.25f;
    }

    /// <summary>Kich thuoc hop bao (world) cua vung o N x M — dung cho tham nen / ghost.</summary>
    public static Vector2 FootprintWorldSize(Vector2Int size)
    {
        int n = Mathf.Max(1, size.x);
        int m = Mathf.Max(1, size.y);
        return new Vector2((n + m) * CellWidth * 0.5f, (n + m) * CellHeight * 0.5f);
    }

    /// <summary>
    /// SNAP TAM — dua mot diem world ve TAM vung o N x M gan nhat.
    /// Cong thuc (khong gian o lien tuc):
    ///     ox = Floor(fx - (N-1)/2 + 0.5)
    ///     oy = Floor(fy - (M-1)/2 + 0.5)
    ///     tam = CellFloatToWorld( ox + (N-1)/2 , oy + (M-1)/2 )
    /// IDEMPOTENT: WorldToCellFloat(tam) tra ve dung (ox+(N-1)/2, oy+(M-1)/2)
    ///             => Floor(ox + 0.5) = ox (ox nguyen).
    /// </summary>
    public static Vector3 SnapCenter(Vector3 world, Vector2Int size)
    {
        RectInt r = RectFromWorld(world, size);
        return RectCenterWorld(r);
    }

    /// <summary>
    /// SNAP DIEM NEO (chan cong trinh = dinh NAM cua vung o).
    /// Day la ham thay the PlacementManager.SnapAnchor.
    /// </summary>
    public static Vector3 SnapAnchor(Vector3 world, Vector2Int size)
    {
        // world dang la CHAN -> quy ve tam de snap, roi ha lai xuong chan
        float half = HalfDepth(size);
        Vector3 center = SnapCenter(new Vector3(world.x, world.y + half, 0f), size);
        return new Vector3(center.x, center.y - half, 0f);
    }

    /// <summary>Vung o N x M gan nhat quanh mot diem world (diem la TAM).</summary>
    public static RectInt RectFromWorld(Vector3 worldCenter, Vector2Int size)
    {
        int n = Mathf.Max(1, size.x);
        int m = Mathf.Max(1, size.y);
        Vector2 f = WorldToCellFloat(worldCenter);
        int ox = Mathf.FloorToInt(f.x - (n - 1) * 0.5f + 0.5f);
        int oy = Mathf.FloorToInt(f.y - (m - 1) * 0.5f + 0.5f);
        return new RectInt(ox, oy, n, m);
    }

    /// <summary>Vung o khi DIEM NEO (chan) nam o anchorWorld.</summary>
    public static RectInt RectFromAnchor(Vector3 anchorWorld, Vector2Int size)
    {
        float half = HalfDepth(size);
        return RectFromWorld(new Vector3(anchorWorld.x, anchorWorld.y + half, 0f), size);
    }

    /// <summary>Tam world cua mot vung o.</summary>
    public static Vector3 RectCenterWorld(RectInt rect)
        => CellFloatToWorld(new Vector2(
            rect.xMin + (rect.width - 1) * 0.5f,
            rect.yMin + (rect.height - 1) * 0.5f));

    /// <summary>Diem neo (chan) world cua mot vung o.</summary>
    public static Vector3 RectAnchorWorld(RectInt rect)
    {
        Vector3 c = RectCenterWorld(rect);
        return new Vector3(c.x, c.y - HalfDepth(new Vector2Int(rect.width, rect.height)), 0f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // TIEN ICH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>4 dinh (world) cua o kim cuong — dung ve gizmo / overlay luoi.</summary>
    public static void CellCorners(Vector2Int cell, out Vector3 n, out Vector3 e,
                                   out Vector3 s, out Vector3 w)
    {
        Vector3 c = CellCenterToWorld(cell);
        float hw = CellWidth * 0.5f, hh = CellHeight * 0.5f;
        n = c + new Vector3(0f,  hh, 0f);
        e = c + new Vector3(hw,  0f, 0f);
        s = c + new Vector3(0f, -hh, 0f);
        w = c + new Vector3(-hw, 0f, 0f);
    }

    /// <summary>Doi so o khi xoay 90 do (buoc le hoan X &lt;-&gt; Y).</summary>
    public static Vector2Int RotateSize(Vector2Int size, int rotationSteps)
        => (Mathf.Abs(rotationSteps) % 2 == 1) ? new Vector2Int(size.y, size.x) : size;

    /// <summary>Uoc luong so o tu kich thuoc hop bao world (dung cho tool do prefab).</summary>
    public static Vector2Int EstimateSizeFromWorldSize(Vector2 worldSize)
    {
        // hop bao cua N x M o: rong = (N+M)*W/2 ; cao = (N+M)*H/2
        // khong tach duoc N va M rieng tu hop bao => tra ve o vuong gan nhat.
        float k = worldSize.x / (CellWidth * 0.5f);
        int total = Mathf.Max(2, Mathf.RoundToInt(k));
        int n = Mathf.Max(1, Mathf.RoundToInt(total * 0.5f));
        return new Vector2Int(n, Mathf.Max(1, total - n));
    }

    /// <summary>Khoang cach o (Chebyshev) giua hai o — tien cho AI / kiem tra ke ben.</summary>
    public static int CellDistance(Vector2Int a, Vector2Int b)
        => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
}
