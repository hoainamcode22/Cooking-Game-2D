using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mot KHU DAT co the mua de mo rong ban do (kieu Township / Hay Day).
///
/// Vung dat cua khu duoc mo ta bang cac RectInt TREN LUOI ISO (IsoGrid),
/// khong phai world — nho vay doi scale/goc luoi khong lam hong du lieu.
///
/// Tao asset: chuot phai trong Project > Create > Farm > Land Region
/// (hoac dung Tools/Farm/Khu Dat de sinh hang loat).
/// </summary>
[CreateAssetMenu(fileName = "Land_", menuName = "Farm/Land Region")]
public class LandRegionData : ScriptableObject
{
    [Header("Dinh danh")]
    [Tooltip("Ma khu — DUY NHAT, dung lam khoa luu. Vd: region_02")]
    public string regionId = "region_01";

    [Tooltip("Ten hien thi cho nguoi choi. Vd: Doi Bac")]
    public string displayName = "Khu Dat";

    [Header("Vung dat (toa do O LUOI ISO)")]
    [Tooltip("Cac hinh chu nhat o tao thanh khu. Thuong chi can 1.")]
    public List<RectInt> cellRects = new List<RectInt>();

    [Header("Dieu kien mo khoa")]
    [Tooltip("Level toi thieu de duoc mua. 0 = khong gioi han.")]
    public int unlockLevel = 1;

    [Tooltip("Gia vang. 0 = mien phi (khu khoi dau).")]
    public int goldPrice = 1000;

    [Tooltip("Gia kim cuong (neu > 0 se cho phep mua bang kim cuong).")]
    public int gemPrice = 0;

    [Header("Nguyen lieu can (lay tu tau lua)")]
    [Tooltip("Vd: 12 Go, 8 Da, 4 Kinh, 6 Dinh. De trong = khong can nguyen lieu.")]
    public List<BuildMaterialCost> materialCosts = new List<BuildMaterialCost>();

    [Header("Don dep o dat (sau khi mua)")]
    [Tooltip("Thoi gian cong nhan don dep o dat, tinh bang giay. 0 = mo ngay.")]
    public int clearSeconds = 300;

    [Tooltip("So kim cuong de hoan thanh ngay. 0 = tu tinh theo thoi gian con lai.")]
    public int rushGemCost = 0;

    [Tooltip("So cong nhan xuat hien trong lo dat khi dang don.")]
    [Range(1, 6)] public int workerCount = 4;

    [Tooltip("Cac khu phai mo truoc khu nay (regionId). De trong = khong can.")]
    public List<string> requiredRegionIds = new List<string>();

    [Header("Khoi dau")]
    [Tooltip("Bat = khu nay MO SAN tu dau game, khong can mua.")]
    public bool unlockedByDefault = false;

    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Tong so o cua khu.</summary>
    public int CellCount
    {
        get
        {
            int t = 0;
            foreach (var r in cellRects) t += Mathf.Max(0, r.width) * Mathf.Max(0, r.height);
            return t;
        }
    }

    /// <summary>O co nam trong khu nay khong.</summary>
    public bool ContainsCell(Vector2Int cell)
    {
        foreach (var r in cellRects)
            if (cell.x >= r.xMin && cell.x < r.xMax && cell.y >= r.yMin && cell.y < r.yMax)
                return true;
        return false;
    }

    /// <summary>Toan bo rect co chua trong khu khong.</summary>
    public bool ContainsRect(RectInt rect)
    {
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                if (!ContainsCell(new Vector2Int(x, y))) return false;
        return true;
    }

    /// <summary>Duyet moi o trong khu.</summary>
    public IEnumerable<Vector2Int> AllCells()
    {
        foreach (var r in cellRects)
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    yield return new Vector2Int(x, y);
    }

    /// <summary>Cac o nam tren VIEN khu (dung de dat hang rao).</summary>
    public IEnumerable<Vector2Int> BorderCells()
    {
        foreach (var c in AllCells())
        {
            bool border =
                !ContainsCell(c + Vector2Int.right) || !ContainsCell(c + Vector2Int.left) ||
                !ContainsCell(c + Vector2Int.up)    || !ContainsCell(c + Vector2Int.down);
            if (border) yield return c;
        }
    }

    /// <summary>Dien tich khu (so o) — dung de tinh gia theo dien tich.</summary>
    public int AreaCells => CellCount;

    /// <summary>
    /// Gia vang tinh theo DIEN TICH neu goldPrice = 0 va co pricePerCell &gt; 0.
    /// Giu goldPrice lam gia chot; ham nay chi de tool sinh du lieu dung.
    /// </summary>
    public static int PriceByArea(int cells, int pricePerCell) => Mathf.Max(0, cells * pricePerCell);

    /// <summary>Tam khu (world) — dat bien bao / nut mua.</summary>
    public Vector3 CenterWorld()
    {
        if (cellRects.Count == 0) return Vector3.zero;
        float sx = 0f, sy = 0f; int n = 0;
        foreach (var r in cellRects)
        {
            sx += r.xMin + r.width * 0.5f;
            sy += r.yMin + r.height * 0.5f;
            n++;
        }
        return IsoGrid.CellFloatToWorld(new Vector2(sx / n, sy / n));
    }
}
