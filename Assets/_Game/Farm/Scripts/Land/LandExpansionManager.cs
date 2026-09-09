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

    [Header("Hang rao chia lo (do designer ve tay)")]
    [Tooltip("Tilemap hang rao designer da ve san de chia lo — Tilemap_IsoFence.")]
    public Tilemap designerFenceTilemap;

    [Tooltip("Lop tilemap ban VE DUONG KE CHIA LO — co the la lop BAT KY, khong nhat " +
             "thiet ten co chu 'Fence'. Trong du an nay luoi chia lo nam tren Tilemap_IsoDock.")]
    public Tilemap dividerTilemap;

    [Tooltip("Danh sach o duong ke chia lo. Vao game se AN DUNG NHUNG O NAY, cac tile " +
             "khac tren cung lop (vd cau tau that) van giu nguyen. Do tool 11 dien vao.")]
    public List<Vector2Int> dividerCells = new List<Vector2Int>();

    [Tooltip("Bat = khi VAO GAME thi AN duong ke chia lo, chi con bien chi duong. " +
             "Trong Editor van thay de designer ve tiep.")]
    public bool hideFenceInPlayMode = true;

    [Tooltip("Bat = an LUON CA LOP co chu 'Fence' trong ten (vd vien dao). " +
             "Mac dinh TAT: chi an duong ke chia lo, vien dao van hien de bien co bo.")]
    public bool hideWholeFenceLayer = false;

    [Header("Camera")]
    [Tooltip("Bat = moi lan mo them dat thi noi rong gioi han keo camera.")]
    public bool expandCameraOnUnlock = true;

    [Tooltip("Le them quanh vung dat da mo (world unit).")]
    public float cameraPadding = 600f;

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
    private readonly Dictionary<string, LandClearingSite> _clearingSites
        = new Dictionary<string, LandClearingSite>();
    private bool _dividersHidden;      // chi xoa o duong ke MOT LAN moi phien choi

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LoadSave();
    }

    private void Start()
    {
        // 🔴 VÒNG 17 — THỨ TỰ NÀY LÀ CÓ CHỦ Ý, ĐỪNG ĐỔI.
        // Bản cũ gọi RefreshAll() TRƯỚC. RefreshAll() có RespawnSigns() ở giữa; chỉ cần
        // MỘT tấm biển ném lỗi là cả Start() chết theo, nên ApplyFenceVisibility() xếp
        // sau nó không bao giờ chạy → hàng rào vẫn hiện nguyên. Đã dính đúng lỗi này:
        // 152 lô mà 0 tấm biển VÀ rào không ẩn — hai triệu chứng của cùng một ngoại lệ.
        // Việc ẩn rào không phụ thuộc gì vào biển báo, nên cho nó chạy TRƯỚC TIÊN.
        ApplyFenceVisibility();

        RefreshAll();
        ResumePendingClearings();
        UpdateCameraBounds();

        Debug.Log($"[Land] Khoi dong: {regions.Count} khu, {spawnedSigns.Count} bien, " +
                  $"{_soODaAn} o hang rao da an" +
                  (_soBienLoi > 0 ? $", {_soBienLoi} bien LOI (xem canh bao ben tren)" : "") + ".");
    }

    private int _soODaAn;      // để in ra log tổng kết, khỏi phải đoán
    private int _soBienLoi;

    /// <summary>
    /// An hang rao chia lo khi vao game (designer van thay trong Editor).
    ///
    /// VONG 15 — SUA LOI "VAO GAME VAN THAY HANG RAO".
    ///
    /// Do lai scene moi ro: TEN LOP BI NGUOC VOI NOI DUNG.
    ///   • Tilemap_IsoFence (812 tile) = VIEN DAO, mot hinh thoi lon bao quanh map.
    ///   • Tilemap_IsoDock (1820 tile) = LUOI CHIA LO ma designer ve tay.
    /// Nen loc theo chu "Fence" trong ten la sai huong. Cach lam moi:
    ///
    ///   1. AN THEO O (dividerCells): tool 11 ghi lai chinh xac nhung o nao la
    ///      duong ke chia lo. Vao game chi xoa dung nhung o do khoi lop tuong ung.
    ///      => Cau tau that (neu ve chung lop) VAN CON. Art goc khong bi dung toi:
    ///         thay doi tilemap trong Play mode tu mat khi thoat Play.
    ///   2. AN CA LOP (designerFenceTilemap): chi dung khi ca lop deu la hang rao.
    /// </summary>
    public void ApplyFenceVisibility()
    {
        if (!Application.isPlaying) return;

        // ── 0. AN THEO O — duong ke chia lo, giu nguyen phan con lai cua lop ──
        if (hideFenceInPlayMode && !_dividersHidden &&
            dividerTilemap != null && dividerCells != null && dividerCells.Count > 0)
        {
            foreach (var c in dividerCells)
                dividerTilemap.SetTile(new Vector3Int(c.x, c.y, 0), null);
            _dividersHidden = true;
            _soODaAn = dividerCells.Count;

            if (verboseLog)
                Debug.Log($"[Land] Da an {dividerCells.Count} o duong ke chia lo tren " +
                          $"'{dividerTilemap.name}' (thoat Play la hien lai).");
        }

        // ── 1. AN CA LOP — chi khi duoc bat han (mac dinh TAT) ──────────
        // Vien dao nam tren Tilemap_IsoFence; tat ca lop nay se lam bien mat bo.
        if (!hideWholeFenceLayer) return;

        var found = new List<Tilemap>();
        if (designerFenceTilemap != null) found.Add(designerFenceTilemap);

        var grid = GameObject.Find(IsoGrid.IsoGridObjectName);
        if (grid != null)
        {
            foreach (var tm in grid.GetComponentsInChildren<Tilemap>(true))
                if (IsFenceTilemap(tm) && !found.Contains(tm)) found.Add(tm);
        }

        if (found.Count == 0)
        {
            if (verboseLog) Debug.LogWarning("[Land] Khong tim thay tilemap hang rao nao de an.");
            return;
        }

        // ── 2. Bat / tat renderer (tile VAN CON de tinh vung lo) ────────
        foreach (var tm in found)
        {
            var r = tm.GetComponent<TilemapRenderer>();
            if (r != null) r.enabled = false;
        }

        if (verboseLog)
            Debug.Log($"[Land] Da an ca {found.Count} lop hang rao khi vao game.");
    }

    private static bool IsFenceTilemap(Tilemap t)
        => t != null && t.name.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>Khoi phuc cac cong truong don dat con dang do sau khi tat game.</summary>
    private void ResumePendingClearings()
    {
        foreach (var r in regions)
        {
            if (r == null || IsRegionUnlocked(r)) continue;
            if (!LandClearingSite.HasPending(r.regionId)) continue;
            var site = LandClearingSite.Create(r, this, transform);
            if (site != null) _clearingSites[r.regionId] = site;
        }
    }

    // ── San gioi han camera (world unit) — xem giai thich trong UpdateCameraBounds ──
    // Do truc tiep tu scene: cau tau y -4875..-3525 (tam -4200), trai rong toi x 2100.
    // Chua them le de con thay mep nuoc quanh cau tau.
    private const float SAN_MIN_Y = -5100f;
    private const float SAN_MIN_X = -2000f;
    private const float SAN_MAX_X = 2400f;

    /// <summary>
    /// Noi gioi han keo camera theo phan dat DA MO — mo dat toi dau vuot toi do.
    /// Tim field bounds cua CameraController bang reflection de khong phu thuoc ten.
    /// </summary>
    public void UpdateCameraBounds()
    {
        if (!expandCameraOnUnlock) return;
        if (unlockedCells.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var c in unlockedCells)
        {
            Vector3 w = IsoGrid.CellCenterToWorld(c);
            minX = Mathf.Min(minX, w.x); maxX = Mathf.Max(maxX, w.x);
            minY = Mathf.Min(minY, w.y); maxY = Mathf.Max(maxY, w.y);
        }
        minX -= cameraPadding; maxX += cameraPadding;
        minY -= cameraPadding; maxY += cameraPadding;

        // VONG 14 — SUA LOI "khong keo camera xuong ben cang duoc".
        // Truoc day ham nay THAY THE han gioi han camera bang hop bao cac o dat DA MO.
        // Chi khu Land_1_1 (o 0..7) mo san nen hop chi ra X[-1650..1650] Y[-600..1650].
        // Ma cau tau (Tilemap_IsoDock) nam o world y -4875..-3525, tam -4200 → camera
        // khong bao gio xuong toi, zoom het co (ortho 1500) cung chi thay toi y = -2100.
        // Cach chua: HOP (union) hop dat da mo voi mot san co dinh phu vung bien + ben cang,
        // thay vi thay the. Mo them dat van noi rong binh thuong, khong mat tinh nang.
        // KHONG dung [SerializeField] moi: field moi se serialize thanh 0 trong scene co san
        // (cai bay da dinh nhieu lan trong du an nay) — nen de thang hang so o day.
        minX = Mathf.Min(minX, SAN_MIN_X);
        maxX = Mathf.Max(maxX, SAN_MAX_X);
        minY = Mathf.Min(minY, SAN_MIN_Y);

        // Ghi ro UnityEngine.Object: file nay co "using System;" nen ten tran "Object"
        // bi mo ho giua UnityEngine.Object va System.Object (CS0104).
        var cam = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        if (cam == null) return;
        cam.SetBounds(minX, maxX, minY, maxY);      // API san co cua CameraController
        if (verboseLog)
            Debug.Log($"[Land] Noi camera theo dat da mo: X[{minX:0}..{maxX:0}] Y[{minY:0}..{maxY:0}].");
    }

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
        // VÒNG 16 — lý do có dấu, vì được hiện thẳng lên bảng và popup.
        // LandRegionSignBoard nhận diện "khoá theo cấp" bằng tiền tố "Mở ở cấp".
        if (r == null)                     { reason = "Khu không tồn tại.";        return false; }
        if (IsRegionUnlocked(r))           { reason = "Khu này đã mở rồi.";        return false; }

        foreach (var need in r.requiredRegionIds)
        {
            if (string.IsNullOrEmpty(need)) continue;
            if (!IsRegionUnlocked(need))
            {
                var nr = FindRegion(need);
                reason = $"Cần mở \"{(nr != null ? nr.displayName : need)}\" trước.";
                return false;
            }
        }

        int level = CurrentLevel();
        if (r.unlockLevel > 0 && level < r.unlockLevel)
        {
            reason = $"Mở ở cấp {r.unlockLevel}.";
            return false;
        }

        var eco = FarmEconomyManager.Instance;
        if (r.goldPrice > 0)
        {
            if (eco == null || eco.Gold < r.goldPrice) { reason = "Không đủ vàng."; return false; }
        }
        else if (r.gemPrice > 0)
        {
            if (eco == null || eco.Gems < r.gemPrice)  { reason = "Không đủ kim cương."; return false; }
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

    /// <summary>
    /// Mua o dat roi BAT DAU GIAI DOAN DON DEP (cong nhan + bui + dem nguoc)
    /// thay vi mo khoa ngay. Day la duong chinh ma popup mua dat goi.
    /// </summary>
    public bool TryBuyAndStartClearing(LandRegionData r)
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

        if (r.clearSeconds <= 0) { Unlock(r); return true; }

        var site = LandClearingSite.Create(r, this, transform);
        if (site != null) _clearingSites[r.regionId] = site;
        return true;
    }

    /// <summary>Cong truong dang don cua mot khu (null neu khong co).</summary>
    public LandClearingSite ClearingSiteOf(string regionId)
        => _clearingSites.TryGetValue(regionId, out var s) && s != null ? s : null;

    /// <summary>Khu nay dang trong giai doan don dep?</summary>
    public bool IsClearing(LandRegionData r)
        => r != null && (ClearingSiteOf(r.regionId) != null || LandClearingSite.HasPending(r.regionId));

    /// <summary>Mo khoa khong tinh tien (dung cho phan thuong / cheat / khu khoi dau).</summary>
    public void Unlock(LandRegionData r)
    {
        if (r == null || IsRegionUnlocked(r)) return;
        unlockedIds.Add(r.regionId);
        _clearingSites.Remove(r.regionId);
        SaveNow();
        RefreshAll();
        UpdateCameraBounds();                       // dat rong ra -> camera noi theo
        if (PlacementManager.Instance != null)
            PlacementManager.Instance.RefreshOccupancy();
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
        ApplyFenceVisibility();   // giu hang rao luon an sau moi lan mo dat
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

    /// <summary>Duong dan bang chi duong mac dinh (Sep chon Prefab_Sprite_Sign_right).</summary>
    public const string DefaultSignResource = "Prefab_Sprite_Sign_right";

    private void RespawnSigns()
    {
        foreach (var g in spawnedSigns) if (g != null) Destroy(g);
        spawnedSigns.Clear();

        // chua gan tay -> thu lay bang chi duong mac dinh trong Resources
        if (signPrefab == null)
            signPrefab = Resources.Load<GameObject>(DefaultSignResource);

        Transform parent = signParent != null ? signParent : transform;
        _soBienLoi = 0;

        foreach (var r in regions)
        {
            if (r == null || IsRegionUnlocked(r)) continue;

            // VÒNG 17 — BỌC TỪNG TẤM BIỂN.
            // Một tấm hỏng (thiếu font, prefab lạ, art rỗng…) trước đây kéo sập cả
            // vòng lặp lẫn mọi thứ chạy sau RefreshAll(). Giờ nó chỉ mất đúng tấm đó.
            try
            {

            // VONG 15 — bien duoc BOC trong mot ROOT scale 1.
            // Ly do: art goc chi 1.26 x 2.14 world unit trong khi mot o luoi la
            // 300 x 150, nen phai phong art len ~100 lan. Neu script + collider
            // nam chung GameObject voi art thi collider bi phong theo => tinh
            // vung bam sai bet. Tach root (scale 1) / art (duoc scale) la sach nhat.
            var root = new GameObject($"Sign_{r.regionId}");
            root.transform.SetParent(parent, false);
            root.transform.position = r.CenterWorld();

            if (signPrefab != null)
            {
                var art = Instantiate(signPrefab, root.transform);
                art.name = "Art";
                art.transform.localPosition = Vector3.zero;
                art.transform.localRotation = Quaternion.identity;

                // prefab cu da co san script bien -> dung luon script do
                var old = art.GetComponent<LandRegionSign>();
                if (old != null)
                {
                    old.Bind(r, this);
                    spawnedSigns.Add(root);
                    continue;
                }
            }

            // khong co prefab thi LandRegionSignBoard tu ve tam bien bang code
            var board = root.AddComponent<LandRegionSignBoard>();
            board.Bind(r, this);
            spawnedSigns.Add(root);
            }
            catch (Exception ex)
            {
                _soBienLoi++;
                Debug.LogWarning($"[Land] Khong dung duoc bien cho khu '{r.regionId}': {ex.Message}");
            }
        }

        if (verboseLog)
            Debug.Log($"[Land] Da cam {spawnedSigns.Count} bang chi duong " +
                      $"(prefab: {(signPrefab != null ? signPrefab.name : "KHONG CO — dung bien ve bang code")}).");
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
