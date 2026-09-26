// ============================================================================
//  WorldClearManager — CHAT CAY / CAT BUI / DAP DA bang dung cu mua o Shop (2026-09-25)
//
//  LUONG CHOI:
//    1. Cham 1 cay / bui / tang da tren map -> hien KHAY DUNG CU (Rìu / Kéo / Búa) o duoi man hinh.
//    2. Keo dung cu tu khay tha vao vat -> tru 1 dung cu, bat dau dem gio (cay 25s, da 18s, bui 12s).
//       Trong luc do: dung cu vung nhip + VFX + thanh tien trinh tren dau vat. Tat game van dem tiep.
//    3. Het gio -> vat do / vo / xep lai roi bien mat, roi Go / Da vao kho + EXP. Lan sau vao game van mat.
//    4. Cham vat dang lam -> khay hien gio con lai + nut kim cuong "Xong ngay".
//  CHI trong khu dat da mo (rung vien map + dat chua mua: bao "Mo khoa vung dat nay truoc").
//
//  Tu khoi dong o scene co FarmManager. KHONG sua prefab / scene: quet object theo ten (WorldClearConfig.tienToTen).
//  Tat nhanh ca tinh nang: bo tick 'batTinhNang' tren [WorldClearManager] luc Play, hoac dat WorldClearManager.TatTinhNang = true.
// ============================================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class WorldClearManager : MonoBehaviour
{
    public static WorldClearManager Instance { get; private set; }
    public static bool TatTinhNang = false;

    [Tooltip("Tat = khong nhan cham vao cay / bui / da nua (VFX dang chay van xong).")]
    public bool batTinhNang = true;
    [Tooltip("In log so vat quet duoc.")]
    public bool logQuet = true;

    private readonly List<WorldClearable> _ds = new List<WorldClearable>(256);
    private WorldClearConfig _cfg;
    private WorldClearTrayUI _tray;

    // cham
    private bool _nhanHopLe;
    private Vector2 _p0;
    private float _t0, _diXa;

    private static Transform _gocFx;
    public static Transform GocFx
    {
        get
        {
            if (_gocFx == null) _gocFx = new GameObject("[WorldClear_FX]").transform;
            return _gocFx;
        }
    }

    public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // Danh sach id da luu (PlayerPrefs khong liet ke duoc key) -> de tool Editor xoa save khi test.
    private const string KeyDs = "WCLR_DS";
    private static HashSet<string> _idDaLuu;

    public static void GhiNhoId(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (_idDaLuu == null)
        {
            _idDaLuu = new HashSet<string>();
            foreach (var s in PlayerPrefs.GetString(KeyDs, "").Split('|')) if (!string.IsNullOrEmpty(s)) _idDaLuu.Add(s);
        }
        if (_idDaLuu.Add(id)) PlayerPrefs.SetString(KeyDs, string.Join("|", _idDaLuu));
    }

    /// <summary>Xoa het tien do chat / cat / dap (cay hien lai o lan vao game sau). Dung khi test.</summary>
    public static int XoaSaveTatCa()
    {
        int n = 0;
        foreach (var s in PlayerPrefs.GetString(KeyDs, "").Split('|'))
        {
            if (string.IsNullOrEmpty(s)) continue;
            PlayerPrefs.DeleteKey("WCLR_W_" + s);
            PlayerPrefs.DeleteKey("WCLR_T_" + s);
            PlayerPrefs.DeleteKey("WCLR_D_" + s);
            n++;
        }
        PlayerPrefs.DeleteKey(KeyDs);
        PlayerPrefs.Save();
        _idDaLuu = null;
        return n;
    }

    // =====================================================================
    //  Khoi dong
    // =====================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiDong()
    {
        SceneManager.sceneLoaded -= KhiNapScene;
        SceneManager.sceneLoaded += KhiNapScene;
        TaoNeuCan();
    }

    private static void KhiNapScene(Scene s, LoadSceneMode m) => TaoNeuCan();

    private static void TaoNeuCan()
    {
        if (Instance != null || TatTinhNang) return;
        if (UnityEngine.Object.FindFirstObjectByType<FarmManager>() == null) return;   // chi scene nong trai
        var go = new GameObject("[WorldClearManager]");
        go.AddComponent<WorldClearManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cfg = WorldClearConfig.Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        yield return null;          // cho kho / land / progress nap save xong
        yield return null;
        try { int n = WildDebrisSpawner.Rai(_cfg); if (logQuet && n > 0) Debug.Log("[WorldClear] Rai " + n + " da nho / bui ram tren dat da mo."); }
        catch (Exception e) { Debug.LogWarning("[WorldClear] Rai vat hoang loi: " + e.Message); }
        Quet();
    }

    // =====================================================================
    //  Quet scene tim cay / bui / da
    // =====================================================================
    private void Quet()
    {
        _ds.Clear();
        int an = 0, dem = 0;
        var sc = SceneManager.GetActiveScene();
        foreach (var goc in sc.GetRootGameObjects())
            Duyet(goc.transform, ref dem, ref an);
        if (logQuet)
            Debug.Log($"[WorldClear] Quet xong: {dem} vat chat/cat/dap duoc, {an} vat da don truoc do (an). " +
                      $"Cay/bui/da nam ngoai khu dat se bao 'Mo khoa vung dat nay truoc'.");
    }

    private void Duyet(Transform t, ref int dem, ref int an)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return;
        if (t.GetComponent<Canvas>() != null || t.GetComponent<RectTransform>() != null) return;   // bo UI
        string ten = t.name;
        if (_cfg.boQuaNeuTenChua != null)
            for (int i = 0; i < _cfg.boQuaNeuTenChua.Length; i++)
                if (!string.IsNullOrEmpty(_cfg.boQuaNeuTenChua[i]) && ten.IndexOf(_cfg.boQuaNeuTenChua[i], StringComparison.OrdinalIgnoreCase) >= 0) return;

        WorldClearKind k;
        if (KhopTen(ten.ToLowerInvariant(), out k))
        {
            var rs = t.GetComponentsInChildren<SpriteRenderer>(false);
            if (rs != null && rs.Length > 0)
            {
                if (Gan(t, k, rs)) dem++; else an++;
                return;
            }
        }
        for (int i = 0; i < t.childCount; i++) Duyet(t.GetChild(i), ref dem, ref an);
    }

    private bool KhopTen(string low, out WorldClearKind k)
    {
        k = WorldClearKind.Riu;
        var ds = _cfg.loai;
        if (ds == null) return false;
        for (int i = 0; i < ds.Length; i++)
        {
            var l = ds[i];
            if (l == null || l.tienToTen == null) continue;
            for (int j = 0; j < l.tienToTen.Length; j++)
            {
                string p = l.tienToTen[j];
                if (!string.IsNullOrEmpty(p) && low.StartsWith(p.ToLowerInvariant())) { k = l.kind; return true; }
            }
        }
        return false;
    }

    /// <summary>true = vat con tren map (them vao danh sach); false = da don truoc do (an luon).</summary>
    private bool Gan(Transform t, WorldClearKind k, SpriteRenderer[] rs)
    {
        Vector3 p = t.position;
        string id = (int)k + "_" + t.name + "_" + Mathf.RoundToInt(p.x / 4f) + "_" + Mathf.RoundToInt(p.y / 4f);
        if (PlayerPrefs.GetInt("WCLR_D_" + id, 0) == 1) { t.gameObject.SetActive(false); return false; }

        var c = t.GetComponent<WorldClearable>();
        if (c == null) c = t.gameObject.AddComponent<WorldClearable>();
        c.Init(k, id, rs);
        _ds.Add(c);

        string w = PlayerPrefs.GetString("WCLR_W_" + id, "");
        long fin;
        if (!string.IsNullOrEmpty(w) && long.TryParse(w, out fin))
            c.KhoiPhuc(fin, PlayerPrefs.GetInt("WCLR_T_" + id, 20));
        return true;
    }

    // =====================================================================
    //  Cham
    // =====================================================================
    private void Update()
    {
        bool nhan, tha, giu, nhieuNgon; Vector2 pos;
        if (!DocConTro(out nhan, out tha, out giu, out nhieuNgon, out pos)) return;

        if (nhan)
        {
            _nhanHopLe = batTinhNang && !TatTinhNang && !WorldClearTrayUI.DangKeo
                         && !FarmInputLock.BlockWorldInteraction && !WorldClickGuard.ConTroTrenPopup(pos);
            _p0 = pos; _t0 = Time.unscaledTime; _diXa = 0f;
        }
        if (giu || tha)
        {
            _diXa = Mathf.Max(_diXa, (pos - _p0).magnitude);
            if (nhieuNgon) _nhanHopLe = false;
        }
        if (tha && _nhanHopLe)
        {
            _nhanHopLe = false;
            float nguong = Mathf.Max(14f, Screen.dpi > 0 ? Screen.dpi * 0.09f : 18f);
            if (_diXa <= nguong && Time.unscaledTime - _t0 <= 0.6f) XuLyCham(pos);
        }
    }

    private static bool DocConTro(out bool nhan, out bool tha, out bool giu, out bool nhieuNgon, out Vector2 pos)
    {
        nhan = tha = giu = nhieuNgon = false; pos = Vector2.zero;
        var ts = Touchscreen.current;
        if (ts != null)
        {
            var t = ts.primaryTouch;
            if (t.press.isPressed || t.press.wasPressedThisFrame || t.press.wasReleasedThisFrame)
            {
                nhan = t.press.wasPressedThisFrame;
                tha = t.press.wasReleasedThisFrame;
                giu = t.press.isPressed;
                pos = t.position.ReadValue();
                var ds = ts.touches;
                for (int i = 1; i < ds.Count; i++) if (ds[i].press.isPressed) { nhieuNgon = true; break; }
                return true;
            }
        }
        var m = Mouse.current;
        if (m == null) return false;
        nhan = m.leftButton.wasPressedThisFrame;
        tha = m.leftButton.wasReleasedThisFrame;
        giu = m.leftButton.isPressed;
        pos = m.position.ReadValue();
        return true;
    }

    private void XuLyCham(Vector2 manHinh)
    {
        var c = TimTai(manHinh);
        if (c == null) { if (_tray != null) _tray.An(); return; }

        // Cong trinh / o dat / chuong co collider VE TRUOC vat nay -> nhuong cho no
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(manHinh.x, manHinh.y, -cam.transform.position.z));
            var hits = Physics2D.OverlapPointAll(w);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null || h.GetComponentInParent<WorldClearable>() != null) continue;
                if (h.bounds.size.x > 2500f) continue;                                  // collider nen lon: bo qua
                var sr = h.GetComponentInParent<SpriteRenderer>();
                if (sr == null) sr = h.GetComponentInChildren<SpriteRenderer>();
                if (sr == null) continue;
                long ut = SortingLayer.GetLayerValueFromID(sr.sortingLayerID) * 100000L + sr.sortingOrder;
                if (ut > c.UuTien) return;
            }
        }

        if (c.DangLam) { Tray().HienDangLam(c); c.NhunChon(); return; }
        if (!DatMoCho(c))
        {
            c.NhunChon();
            if (_tray != null) _tray.An();
            LockedHintFX.Show(Loc.T("Mở khóa vùng đất này trước"), manHinh);
            return;
        }
        c.NhunChon();
        Tray().HienChon(c);
    }

    /// <summary>Vat nam duoi diem man hinh (lop ve cao nhat thang).</summary>
    public WorldClearable TimTai(Vector2 manHinh)
    {
        var cam = Camera.main;
        if (cam == null) return null;
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(manHinh.x, manHinh.y, -cam.transform.position.z));
        WorldClearable best = null; long ut = long.MinValue;
        for (int i = _ds.Count - 1; i >= 0; i--)
        {
            var c = _ds[i];
            if (c == null) { _ds.RemoveAt(i); continue; }
            if (c.DaXong || !c.ChuaDiem(w)) continue;
            long u = c.UuTien;
            if (best == null || u > ut || (u == ut && c.DiemChan.y < best.DiemChan.y)) { best = c; ut = u; }
        }
        return best;
    }

    /// <summary>Chan HOAC tam vat nam tren dat da mo (vat nam sat mep 2 khu van chat duoc).</summary>
    public bool DatMoCho(WorldClearable c) => c != null && (TrongDatDaMo(c.DiemChan) || TrongDatDaMo(c.Bien.center));

    public bool TrongDatDaMo(Vector3 p)
    {
        var lm = LandExpansionManager.Instance;
        if (lm == null) return true;
        var cell = IsoGrid.WorldToCell(p);
        // [2026-09-25] Cac khu chong nhau (Land_x_y va Lot_xx cung phu 1 o). RegionAtCell tra khu DAU TIEN
        // (co the la khu chua mua) du o do da mo qua khu khac -> bao nham "mo khoa truoc". Hoi thang theo O.
        // [v2] Dung CHUNG luat voi viec dat cong trinh (PlacementManager.IsRectUnlocked): o nao dat nha duoc
        // thi chat duoc. O khong thuoc khu nao (dat khoi dau ve tay) = da mo. Chi o thuoc khu CHUA mua moi chan.
        if (lm.IsCellUnlocked(cell)) return true;
        return !lm.IsCellOwnedByAnyRegion(cell) && _cfg.choPhepNgoaiKhuDat;
    }

    private WorldClearTrayUI Tray()
    {
        if (_tray == null) _tray = WorldClearTrayUI.TimHoacDung();
        return _tray;
    }

    // =====================================================================
    //  Dung cu / bat dau / kim cuong / thuong
    // =====================================================================
    public WorldClearConfig Cfg => _cfg;

    public int SoDungCu(WorldClearKind k)
    {
        var inv = FarmInventoryManager.Instance;
        return inv != null ? inv.GetAmount(_cfg.Tim(k).toolItemId) : 0;
    }

    public bool ThuBatDau(WorldClearable c, out string loi)
    {
        loi = null;
        if (c == null || c.DaXong || c.DangLam) return false;
        if (!DatMoCho(c)) { loi = Loc.T("Mở khóa vùng đất này trước"); return false; }
        var l = _cfg.Tim(c.kind);
        var inv = FarmInventoryManager.Instance;
        if (inv == null || !inv.RemoveItem(l.toolItemId, 1)) { loi = Loc.T(WorldClearTrayUI.ChuHet(c.kind)); return false; }
        c.BatDau(l.giayLam);
        return true;
    }

    public int GiaKimCuong(WorldClearable c)
    {
        if (c == null) return 1;
        return Mathf.Max(1, Mathf.CeilToInt(c.GiayConLai / (float)Mathf.Max(1, _cfg.giayMoiKimCuong)));
    }

    public bool ThuXongNgay(WorldClearable c)
    {
        if (c == null || !c.DangLam) return false;
        var eco = FarmEconomyManager.Instance;
        if (eco == null || !eco.SpendGems(GiaKimCuong(c))) return false;
        c.XongNgay();
        return true;
    }

    /// <summary>Goi tu WorldClearable khi xong: roi nguyen lieu vao kho + EXP + chu bay.</summary>
    public static void TraThuong(WorldClearable c)
    {
        if (c == null) return;
        var cfg = WorldClearConfig.Load();
        var l = cfg.Tim(c.kind);
        string chu = "";
        // [2026-09-25] Nguyen lieu + EXP ROI XUONG DAT roi bay ve nut Kho / thanh EXP (dung prefab roi cua thu hoach).
        var hfs = HarvestFeedbackSpawner.Instance;
        Vector3 diemRoi = new Vector3(c.Bien.center.x, c.Bien.min.y + c.Bien.size.y * 0.35f, c.DiemChan.z);
        string id; Vector2Int sl; cfg.Thuong(c.kind, out id, out sl);
        if (!string.IsNullOrEmpty(id) && sl.y > 0)
        {
            int n = UnityEngine.Random.Range(Mathf.Max(0, sl.x), sl.y + 1);
            if (n > 0)
            {
                string ten = BuildMaterials.DisplayNameOf(id);
                if (ten == id) { var cat = StallItemCatalog.Instance; if (cat != null) ten = cat.GetDisplayName(id); }
                Sprite icon = cfg.IconThuong(c.kind);
                bool roi = hfs != null && hfs.CoItemFly && icon != null;
                if (roi)
                {
                    if (FarmInventoryManager.Instance != null) FarmInventoryManager.Instance.AddItem(id, n);   // khong ban FX kho lan 2
                    hfs.SpawnHarvestFly(icon, diemRoi, n);
                }
                else if (WarehouseManager.Instance != null) WarehouseManager.Instance.AddItem(id, ten, icon, n);
                else if (FarmInventoryManager.Instance != null) FarmInventoryManager.Instance.AddItem(id, n);
                chu = "+" + n + " " + Loc.T(ten);
            }
        }
        if (l.thuongExp > 0 && PlayerProgressManager.Instance != null)
        {
            if (hfs != null && hfs.CoExpFly) hfs.SpawnExpFly(diemRoi + new Vector3(30f, 10f, 0f), l.thuongExp);   // cong EXP khi orb bay toi
            else PlayerProgressManager.Instance.AddExp(l.thuongExp);
            chu += (chu.Length > 0 ? "   " : "") + "+" + l.thuongExp + " EXP";
        }
        var cam = Camera.main;
        if (chu.Length > 0 && cam != null && WorldClearFX.TrongCamera(c.Bien.center, 0f))
            LockedHintFX.Show(chu, (Vector2)cam.WorldToScreenPoint(c.Bien.center));

        if (Instance != null)
        {
            Instance._ds.Remove(c);
            if (Instance._tray != null) Instance._tray.KhiVatXong(c);
        }
    }
}
