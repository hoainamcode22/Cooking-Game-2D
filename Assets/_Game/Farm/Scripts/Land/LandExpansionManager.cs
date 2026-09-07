using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ============================================================================
/// HE MO RONG DAT (Land Expansion) — kieu Township / Hay Day
/// ============================================================================
///
/// Y TUONG
/// -------
/// Ban do duoc chia thanh cac KHU (LandRegionData), moi khu la mot tap o tren
/// luoi ISO. Khu chua mua:
///   • KHONG cho dat cong trinh (PlacementManager hoi qua IsRectUnlocked).
///   • Duoc phu tile "co dai / toi mau" tren lockedOverlayTilemap.
///   • Duoc vien bang hang rao tren fenceTilemap (RuleTile tu noi).
///   • Co mot bien bao o giua ("CAP X MO" / gia tien) de nguoi choi bam mua.
///
/// LUU TRU
/// -------
/// PlayerPrefs key FARM_UNLOCKED_REGIONS = danh sach regionId, ngan cach bang '|'.
///
/// CACH DUNG
/// ---------
/// 1. Tao cac asset LandRegionData (Tools/Farm/Khu Dat sinh nhanh dang luoi).
/// 2. Gan het vao field 'regions' cua component nay trong scene.
/// 3. Keo Tilemap_LockedOverlay va Tilemap_IsoFence vao 2 field tilemap.
/// 4. Goi TryBuy(regionId) tu UI popup mua dat.
/// </summary>
[DefaultExecutionOrder(-50)]
public class LandExpansionManager : MonoBehaviour
{
    public static LandExpansionManager Instance { get; private set; }

    private const string SaveKey = "FARM_UNLOCKED_REGIONS";

    // ─────────────────────────────────────────────────────────────────────
    [Header("Danh sach khu dat")]
    [Tooltip("Keo tat ca asset LandRegionData vao day.")]
    public List<LandRegionData> regions = new List<LandRegionData>();

    [Header("Tilemap hien thi (co the de trong)")]
    [Tooltip("Tilemap phu len khu CHUA MUA — co dai / lop toi.")]
    public Tilemap lockedOverlayTilemap;
    [Tooltip("Tile dung phu khu chua mua.")]
    public TileBase lockedOverlayTile;

    [Tooltip("Tilemap ve HANG RAO quanh khu chua mua.")]
    public Tilemap fenceTilemap;
    [Tooltip("RuleTile hang rao — RuleTile_IsoFence45.")]
    public TileBase fenceTile;

    [Header("Bien bao mua dat")]
    [Tooltip("Prefab bien bao (co component LandRegionSign). De trong = khong sinh bien.")]
    public GameObject signPrefab;
    [Tooltip("Cha chua cac bien bao sinh ra.")]
    public Transform signParent;

    [Header("Luat")]
    [Tooltip("Bat = cam dat cong trinh ra ngoai khu da mua.")]
    public bool enforceLandBounds = true;
    [Tooltip("Bat = ghi log chi tiet khi mua / kiem tra o.")]
    public bool verboseLog = false;

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Ban ra khi mot khu vua duoc mo khoa.</summary>
    public static event Action<LandRegionData> OnRegionUnlocked;

    private readonly HashSet<string>    unlockedIds     = new HashSet<string>();
    private readonly HashSet<Vector2Int> unlockedCells  = new HashSet<Vector2Int>();
    private readonly List<GameObject>   spawnedSigns    = new List<GameObject>();

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadSave();
    }

    private void Start() => RefreshAll();

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // ─────────────────────────────────────────────────────────────────────
    // TRUY VAN
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Khu da duoc mo khoa chua.</summary>
    public bool IsRegionUnlocked(string regionId) => unlockedIds.Contains(regionId);

    public bool IsRegionUnlocked(LandRegionData r) => r != null && IsRegionUnlocked(r.regionId);

    /// <summary>O nay da thuoc dat cua nguoi choi chua.</summary>
    public bool IsCellUnlocked(Vector2Int cell)
    {
        if (!enforceLandBounds) return true;
        // Khong khu nao khai bao o nay => coi nhu dat tu do (tuong thich map cu).
        if (!IsCellOwnedByAnyRegion(cell)) return true;
        return unlockedCells.Contains(cell);
    }

    /// <summary>Toan bo vung o co nam trong dat da mua khong.</summary>
    public bool IsRectUnlocked(RectInt rect)
    {
        if (!enforceLandBounds) return true;
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
                if (!IsCellUnlocked(new Vector2Int(x, y))) return false;
        return true;
    }

    /// <summary>O co thuoc khu nao khong (du da mua hay chua).</summary>
    public bool IsCellOwnedByAnyRegion(Vector2Int cell)
    {
        foreach (var r in regions) if (r != null && r.ContainsCell(cell)) return true;
        return false;
    }

    /// <summary>Khu chua cell (null neu khong co).</summary>
    public LandRegionData RegionAtCell(Vector2Int cell)
    {
        foreach (var r in regions) if (r != null && r.ContainsCell(cell)) return r;
        return null;
    }

    public LandRegionData FindRegion(string regionId)
    {
        foreach (var r in regions) if (r != null && r.regionId == regionId) return r;
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    // DIEU KIEN MUA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Kiem tra co mua duoc khong. reason = ly do khong mua duoc (de hien UI).</summary>
    public bool CanBuy(LandRegionData r, out string reason)
    {
        reason = null;
        if (r == null)                     { reason = "Khu khong ton tai.";        return false; }
        if (IsRegionUnlocked(r))           { reason = "Khu nay da mo roi.";        return false; }

        foreach (var need in r.requiredRegionIds)
        {
            if (string.IsNullOrEmpty(need)) continue;
            if (!IsRegionUnlocked(need))
            {
                var nr = FindRegion(need);
                reason = $"Can mo khu \"{(nr != null ? nr.displayName : need)}\" truoc.";
                return false;
            }
        }

        int level = CurrentLevel();
        if (r.unlockLevel > 0 && level < r.unlockLevel)
        {
            reason = $"Mo o cap {r.unlockLevel}.";
            return false;
        }

        var eco = FarmEconomyManager.Instance;
        if (r.goldPrice > 0)
        {
            if (eco == null || eco.Gold < r.goldPrice) { reason = "Khong du vang."; return false; }
        }
        else if (r.gemPrice > 0)
        {
            if (eco == null || eco.Gems < r.gemPrice)  { reason = "Khong du kim cuong."; return false; }
        }
        return true;
    }

    /// <summary>Mua khu dat. Tra ve true neu thanh cong (da tru tien + mo khoa + ve lai).</summary>
    public bool TryBuy(LandRegionData r)
    {
        if (!CanBuy(r, out string reason))
        {
            if (verboseLog) Debug.Log($"[Land] Khong mua duoc {r?.regionId}: {reason}");
            return false;
        }

        var eco = FarmEconomyManager.Instance;
        if (r.goldPrice > 0)
        {
            if (eco == null || !eco.SpendGold(r.goldPrice)) return false;
        }
        else if (r.gemPrice > 0)
        {
            if (eco == null || !eco.SpendGems(r.gemPrice)) return false;
        }

        Unlock(r);
        return true;
    }

    public bool TryBuy(string regionId) => TryBuy(FindRegion(regionId));

    /// <summary>Mo khoa khong tinh tien (dung cho phan thuong / cheat / khu khoi dau).</summary>
    public void Unlock(LandRegionData r)
    {
        if (r == null || IsRegionUnlocked(r)) return;
        unlockedIds.Add(r.regionId);
        SaveNow();
        RefreshAll();
        OnRegionUnlocked?.Invoke(r);
        if (verboseLog) Debug.Log($"[Land] Da mo khu {r.regionId} ({r.CellCount} o).");
    }

    // ─────────────────────────────────────────────────────────────────────
    // VE LAI HIEN THI
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Dung lai tap o da mo + ve lai overlay, hang rao, bien bao.</summary>
    public void RefreshAll()
    {
        RebuildUnlockedCells();
        RedrawTilemaps();
        RespawnSigns();
    }

    private void RebuildUnlockedCells()
    {
        unlockedCells.Clear();
        foreach (var r in regions)
        {
            if (r == null) continue;
            if (r.unlockedByDefault && !unlockedIds.Contains(r.regionId))
                unlockedIds.Add(r.regionId);
            if (!unlockedIds.Contains(r.regionId)) continue;
            foreach (var c in r.AllCells()) unlockedCells.Add(c);
        }
    }

    private void RedrawTilemaps()
    {
        if (lockedOverlayTilemap != null) lockedOverlayTilemap.ClearAllTiles();
        if (fenceTilemap != null) fenceTilemap.ClearAllTiles();

        foreach (var r in regions)
        {
            if (r == null || IsRegionUnlocked(r)) continue;

            if (lockedOverlayTilemap != null && lockedOverlayTile != null)
                foreach (var c in r.AllCells())
                    lockedOverlayTilemap.SetTile(new Vector3Int(c.x, c.y, 0), lockedOverlayTile);

            if (fenceTilemap != null && fenceTile != null)
                foreach (var c in r.BorderCells())
                    fenceTilemap.SetTile(new Vector3Int(c.x, c.y, 0), fenceTile);
        }
    }

    private void RespawnSigns()
    {
        foreach (var g in spawnedSigns) if (g != null) Destroy(g);
        spawnedSigns.Clear();
        if (signPrefab == null) return;

        foreach (var r in regions)
        {
            if (r == null || IsRegionUnlocked(r)) continue;
            var go = Instantiate(signPrefab, r.CenterWorld(),
                                 Quaternion.identity, signParent != null ? signParent : transform);
            go.name = $"Sign_{r.regionId}";
            var sign = go.GetComponent<LandRegionSign>();
            if (sign != null) sign.Bind(r, this);
            spawnedSigns.Add(go);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // LUU / TAI
    // ─────────────────────────────────────────────────────────────────────

    private void LoadSave()
    {
        unlockedIds.Clear();
        string raw = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(raw)) return;
        foreach (var id in raw.Split('|'))
            if (!string.IsNullOrWhiteSpace(id)) unlockedIds.Add(id.Trim());
    }

    private void SaveNow()
    {
        PlayerPrefs.SetString(SaveKey, string.Join("|", unlockedIds));
        PlayerPrefs.Save();
    }

    /// <summary>Xoa toan bo tien do mua dat (dung cho nut "choi lai tu dau").</summary>
    public void ResetAllProgress()
    {
        unlockedIds.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        RefreshAll();
    }

    private static int CurrentLevel()
        => FarmLevelManager.Instance != null ? FarmLevelManager.Instance.CurrentLevel : 1;

    // ─────────────────────────────────────────────────────────────────────
    // GIZMO — nhin thay khu dat ngay trong Scene view
    // ─────────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        foreach (var r in regions)
        {
            if (r == null) continue;
            bool open = Application.isPlaying ? IsRegionUnlocked(r) : r.unlockedByDefault;
            Gizmos.color = open ? new Color(0.3f, 1f, 0.4f, 0.85f)
                                : new Color(1f, 0.75f, 0.2f, 0.85f);
            foreach (var c in r.BorderCells())
            {
                IsoGrid.CellCorners(c, out var n, out var e, out var s, out var w);
                Gizmos.DrawLine(n, e); Gizmos.DrawLine(e, s);
                Gizmos.DrawLine(s, w); Gizmos.DrawLine(w, n);
            }
        }
    }
}
