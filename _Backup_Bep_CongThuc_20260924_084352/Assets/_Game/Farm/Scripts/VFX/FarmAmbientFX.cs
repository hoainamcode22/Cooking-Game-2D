// ============================================================================
//  FarmAmbientFX — HIEU UNG MOI TRUONG + HAT NHO cho nong trai, TOI UU NHE (2026-09-24)
// ----------------------------------------------------------------------------
//  Mot component, MOT vong Update cho tat ca (khong Update rieng tung hat, khong Instantiate
//  luc choi): moi hat la SpriteRenderer tao san 1 lan (pool), chi bat/tat renderer.
//  Dung chung 1 bo sprite ve bang code (SoftFxSprites, < 0.5 MB) -> cung texture + material
//  -> Unity gop draw call duoc.
//    Moi truong:  bong may cum nho tren mat dat · lap lanh + gon song tren mat nuoc (chi o Water_Tilemap
//                 trong khung hinh) · buom bay + DAU tren hoa / o vua gieo hat (ban ngay)
//                 · dan chim bay tren cao (phoi canh + bong duoi dat, thinh thoang)
//                 · dom dom (ban dem theo gio may 18h-6h)
//    Hat dung chung (goi tu code khac):  FarmAmbientFX.BuiDat(pos) — bui dat khi trong/thu hoach
//                                         FarmAmbientFX.LapLanh(pos) — sao lap lanh (cay chin, len stage)
//  Tu tat khi: dang o Bep (IsCookingMode), timeScale = 0 (popup dung game), khong co camera.
//  May yeu (VfxCauHinh.CheDoNhe) tu giam 1/2 so hat.
//  Tu tao luc chay neu scene co PlotController; muon chinh thi Tools > VFX > 1 dung vao Hierarchy.
// ============================================================================
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class FarmAmbientFX : MonoBehaviour
{
    public static FarmAmbientFX Instance { get; private set; }

    [Header("Bat / tat")]
    [Tooltip("Bong may: tung cum may nho troi cham, CHI thay bong mo tren mat dat.")]
    [SerializeField] private bool bongMay = true;
    [SerializeField] private bool lapLanhNuoc = true;
    [SerializeField] private bool buom = true;
    [SerializeField] private bool chim = true;
    [SerializeField] private bool domDom = true;
    [Tooltip("Cham vao bui co / bui ram (tilemap Co_Grass) -> bui rung rinh + vai la bay.")]
    [SerializeField] private bool rungBuiCo = true;
    [SerializeField] private string tenTilemapCo = "Co_Grass";
    [Tooltip("Dom dom: 0 = tu dong theo gio may, 1 = luon bat (de xem thu), -1 = luon tat")]
    [SerializeField] private int domDomCheDo = 0;
    [SerializeField] private int gioToiBatDau = 18;
    [SerializeField] private int gioSangKetThuc = 6;

    [Header("So luong (may yeu tu giam 1/2)")]
    [SerializeField] private int soLapLanh = 14;
    [SerializeField] private int soGonSong = 4;
    [SerializeField] private int soBuom = 4;
    [SerializeField] private int soChimToiDa = 5;
    [SerializeField] private int soDomDom = 14;
    [SerializeField] private int poolBui = 20;
    [SerializeField] private int poolSao = 24;

    [Header("Bong may (cum nho)")]
    [SerializeField] private int soCumMay = 4;
    [SerializeField] private int soBongMoiCum = 4;
    [SerializeField] private Vector2 bongMayKichThuoc = new Vector2(170f, 300f);
    [SerializeField] private float bongMayToc = 22f;
    [SerializeField] private float bongMayAlpha = 0.075f;
    [SerializeField] private int thuTuBongMay = 26;

    [Header("Nuoc")]
    [SerializeField] private string tenTilemapNuoc = "Water_Tilemap";
    [SerializeField] private Vector2 lapLanhKichThuoc = new Vector2(22f, 42f);
    [SerializeField] private float gonSongKichThuoc = 170f;

    [Header("Buom / chim / dom dom")]
    [SerializeField] private float buomKichThuoc = 34f;
    [SerializeField] private float buomToc = 70f;
    [Tooltip("Ti le buom chon bay toi DAU tren hoa / o vua gieo hat (con lai bay lang thang).")]
    [Range(0f, 1f)] [SerializeField] private float buomTiLeDau = 0.8f;
    [SerializeField] private Vector2 buomDauGiay = new Vector2(3f, 7f);
    [SerializeField] private Vector2 chimCachGiay = new Vector2(25f, 50f);
    [SerializeField] private float chimToc = 340f;
    [SerializeField] private float chimKichThuoc = 120f;
    [Tooltip("Do cao chim (phoi canh): chim bi day ra xa tam man hinh theo ti le nay + to hon + troi nhanh hon mat dat khi keo camera -> cam giac bay tren cao, gan mat nguoi choi.")]
    [SerializeField] private float chimDoCao = 0.35f;
    [Tooltip("Bong chim duoi mat dat (lech theo huong nang).")]
    [SerializeField] private Vector2 chimLechBong = new Vector2(70f, -60f);
    [SerializeField] private float chimBongAlpha = 0.16f;
    [SerializeField] private float domDomKichThuoc = 40f;

    [Header("Sorting")]
    [SerializeField] private string lopTren = "ObjectsFront";
    [SerializeField] private int thuTuBuom = 32300;
    [SerializeField] private int thuTuDomDom = 32400;
    [SerializeField] private int thuTuSao = 32500;
    [SerializeField] private string lopChim = "Foreground";
    [SerializeField] private int thuTuChim = 32000;
    [SerializeField] private int thuTuBongChim = 25;
    [SerializeField] private string lopDat = "Default";
    [SerializeField] private int thuTuNuoc = -7;
    [SerializeField] private int thuTuBui = 20;

    // ─── Hat ───
    private class Hat
    {
        public SpriteRenderer sr;
        public Transform tf;
        public bool song;
        public float tuoi, doi, kt, pha, a0, mo, ks;
        public int trangThai;          // buom: 0 bay lang thang, 1 bay toi cho dau, 2 dang dau
        public Vector3 dich;
        public Vector3 pos, van, goc;
        public Color mau;
    }

    private Hat[] _may, _lap, _song, _buom, _chim, _bongChim, _dom, _bui, _sao;
    private int _buiKe, _saoKe;
    private Camera _cam;
    private Tilemap _nuoc;
    private readonly List<Tilemap> _datPhu = new List<Tilemap>();
    private Rect _khung;
    private float _henLap, _henSong, _henChim, _henKiemDem;
    private bool _laDem, _dangChimBay;
    private int _soChimDan;
    private Vector3 _chimHuong;
    private bool _daDung;
    private Vector3[] _cumMay;       // tam tung cum
    private float[] _cumMo;          // do hien (fade vao/ra)
    private Vector3[] _lechBong;     // lech tung bong trong cum
    private Transform _goc;
    private float _tAm;

    // ── Rung bui co (tap) ──
    private struct Rung { public Vector3Int o; public float t, day; public Matrix4x4 goc; }
    private readonly List<Rung> _rung = new List<Rung>(8);
    private Tilemap _co;
    private bool _dangNhanBui;
    private Vector2 _nhanBuiTai;

    // =====================================================================
    //  Tu tao
    // =====================================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiDong()
    {
        SceneManager.sceneLoaded -= KhiLoadScene;
        SceneManager.sceneLoaded += KhiLoadScene;
        DamBaoCo();
    }

    private static void KhiLoadScene(Scene s, LoadSceneMode m) => DamBaoCo();

    private static void DamBaoCo()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<FarmAmbientFX>(FindObjectsInactive.Include) != null) return;
        if (FindFirstObjectByType<PlotController>() == null) return;   // chi o nong trai
        new GameObject("[FarmAmbientFX]").AddComponent<FarmAmbientFX>();
    }

    // =====================================================================
    //  API dung chung
    // =====================================================================

    /// <summary>Bui dat mem tung len quanh diem (trong cay / thu hoach). Re: dung pool.</summary>
    public static void BuiDat(Vector3 pos, int soHat = 5)
    {
        var fx = Instance;
        if (fx == null || fx._bui == null) return;
        soHat = VfxCauHinh.CheDoNhe ? Mathf.Max(2, soHat / 2) : soHat;
        for (int i = 0; i < soHat; i++)
        {
            var h = fx._bui[fx._buiKe]; fx._buiKe = (fx._buiKe + 1) % fx._bui.Length;
            float g = (i / (float)soHat) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.3f, 0.3f);
            h.pos = pos + new Vector3(Mathf.Cos(g) * 18f, Mathf.Sin(g) * 8f, 0f);
            h.van = new Vector3(Mathf.Cos(g) * UnityEngine.Random.Range(40f, 80f), Mathf.Sin(g) * 18f + UnityEngine.Random.Range(20f, 45f), 0f);
            h.tuoi = 0f; h.doi = UnityEngine.Random.Range(0.45f, 0.7f);
            h.kt = UnityEngine.Random.Range(32f, 52f);
            h.a0 = UnityEngine.Random.Range(0.35f, 0.55f);
            h.mau = new Color(0.62f, 0.5f, 0.36f);
            fx.Bat(h);
        }
    }

    /// <summary>La xanh nho bay tung ra (cham bui co). Dung chung pool bui.</summary>
    public static void LaBay(Vector3 pos, int soLa = 4)
    {
        var fx = Instance;
        if (fx == null || fx._bui == null) return;
        soLa = VfxCauHinh.CheDoNhe ? Mathf.Max(2, soLa / 2) : soLa;
        for (int i = 0; i < soLa; i++)
        {
            var h = fx._bui[fx._buiKe]; fx._buiKe = (fx._buiKe + 1) % fx._bui.Length;
            h.pos = pos + new Vector3(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-10f, 10f), 0f);
            h.van = new Vector3(UnityEngine.Random.Range(-90f, 90f), UnityEngine.Random.Range(60f, 130f), 0f);
            h.tuoi = 0f; h.doi = UnityEngine.Random.Range(0.5f, 0.8f);
            h.kt = UnityEngine.Random.Range(14f, 22f);
            h.a0 = 0.9f;
            h.mau = UnityEngine.Random.value < 0.5f ? new Color(0.45f, 0.75f, 0.25f) : new Color(0.62f, 0.85f, 0.3f);
            fx.Bat(h);
        }
    }

    /// <summary>Sao lap lanh nho (cay chin, cay len stage). kichThuoc 1 = ~46 don vi world.</summary>
    public static void LapLanh(Vector3 pos, float kichThuoc = 1f)
    {
        var fx = Instance;
        if (fx == null || fx._sao == null || !fx.isActiveAndEnabled) return;
        var h = fx._sao[fx._saoKe]; fx._saoKe = (fx._saoKe + 1) % fx._sao.Length;
        h.pos = pos;
        h.tuoi = 0f; h.doi = UnityEngine.Random.Range(0.6f, 0.85f);
        h.kt = 46f * kichThuoc * UnityEngine.Random.Range(0.8f, 1.2f);
        h.pha = UnityEngine.Random.Range(0f, 90f);
        fx.Bat(h);
    }

    /// <summary>0 = theo gio may, 1 = luon bat dom dom (xem thu), -1 = tat.</summary>
    public void DatCheDoDomDom(int cheDo)
    {
        domDomCheDo = cheDo;
        KiemDem();
    }

    public int CheDoDomDom => domDomCheDo;

    // =====================================================================
    //  Dung
    // =====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Dung();
    }

    private void Dung()
    {
        if (_daDung) return;
        _daDung = true;
        _goc = new GameObject("Fx_Pool").transform;
        _goc.SetParent(transform, false);

        _lap  = TaoNhom("Fx_LapLanhNuoc", VfxCauHinh.SoLuong(soLapLanh), SoftFxSprites.SparkleSprite, lopDat, thuTuNuoc);
        _song = TaoNhom("Fx_GonSong",  VfxCauHinh.SoLuong(soGonSong), SoftFxSprites.RingSprite,   lopDat, thuTuNuoc);
        _buom = TaoNhom("Fx_Buom",     VfxCauHinh.SoLuong(soBuom),    SoftFxSprites.ButterflySprite, lopTren, thuTuBuom);
        _chim = TaoNhom("Fx_Chim",     Mathf.Max(1, soChimToiDa),     SoftFxSprites.BirdSprite,   lopChim, thuTuChim);
        _bongChim = TaoNhom("Fx_BongChim", _chim.Length,              SoftFxSprites.BirdSprite,   lopDat, thuTuBongChim);
        int cum = VfxCauHinh.SoLuong(soCumMay), moi = Mathf.Max(1, soBongMoiCum);
        _may = TaoNhom("Fx_BongMay", cum * moi, SoftFxSprites.GlowSprite, lopDat, thuTuBongMay);
        _cumMay = new Vector3[cum]; _cumMo = new float[cum]; _lechBong = new Vector3[cum * moi];
        for (int i = 0; i < cum; i++) _cumMo[i] = -1f;   // chua dat
        _dom  = TaoNhom("Fx_DomDom",   VfxCauHinh.SoLuong(soDomDom),  SoftFxSprites.GlowSprite,   lopTren, thuTuDomDom);
        _bui  = TaoNhom("Fx_Bui",      VfxCauHinh.SoLuong(poolBui),   SoftFxSprites.CircleSprite, lopDat, thuTuBui);
        _sao  = TaoNhom("Fx_Sao",      VfxCauHinh.SoLuong(poolSao),   SoftFxSprites.SparkleSprite, lopTren, thuTuSao);

        TimNuoc();
        TimCo();
        _henChim = Time.time + UnityEngine.Random.Range(6f, 14f);
        KiemDem();
    }

    private Hat[] TaoNhom(string ten, int n, Sprite sp, string lop, int thuTu)
    {
        var ds = new Hat[Mathf.Max(1, n)];
        for (int i = 0; i < ds.Length; i++)
        {
            var go = new GameObject(ten + "_" + i);
            go.transform.SetParent(_goc, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingLayerName = lop;
            sr.sortingOrder = thuTu;
            sr.enabled = false;
            ds[i] = new Hat { sr = sr, tf = go.transform, ks = KichSprite(sr) };
        }
        return ds;
    }

    private void TimCo()
    {
        _co = null;
        foreach (var tm in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (tm.name == tenTilemapCo) { _co = tm; return; }
    }

    private void XuLyChamBui()
    {
        if (!rungBuiCo || _co == null) return;
        bool nhan = false, tha = false; Vector2 p = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var ptr = UnityEngine.InputSystem.Pointer.current;
        if (ptr == null) return;
        nhan = ptr.press.wasPressedThisFrame; tha = ptr.press.wasReleasedThisFrame; p = ptr.position.ReadValue();
#else
        nhan = Input.GetMouseButtonDown(0); tha = Input.GetMouseButtonUp(0); p = Input.mousePosition;
#endif
        if (nhan) { _dangNhanBui = true; _nhanBuiTai = p; }
        if (!tha || !_dangNhanBui) return;
        _dangNhanBui = false;
        if ((p - _nhanBuiTai).sqrMagnitude > 25f * 25f) return;                 // dang keo ban do
        if (FarmInputLock.BlockWorldClickBySceneOrPopup || FarmInputLock.ConTroTrenUiThat()) return;

        Vector3 w = _cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -_cam.transform.position.z));
        Vector3 l = _co.transform.InverseTransformPoint(w);
        Vector3Int c = _co.WorldToCell(w);
        Vector3Int tot = c; float totY = float.MaxValue; Sprite totSp = null;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                var o = new Vector3Int(c.x + dx, c.y + dy, c.z);
                var sp = _co.GetSprite(o);
                if (sp == null) continue;
                Vector3 neo = _co.CellToLocalInterpolated(o + _co.tileAnchor);
                Bounds b = sp.bounds;
                if (l.x < neo.x + b.min.x || l.x > neo.x + b.max.x || l.y < neo.y + b.min.y || l.y > neo.y + b.max.y) continue;
                if (neo.y < totY) { totY = neo.y; tot = o; totSp = sp; }   // bui dung truoc (thap hon) uu tien
            }
        if (totSp == null) return;
        for (int i = 0; i < _rung.Count; i++) if (_rung[i].o == tot) return;      // dang rung
        if (_rung.Count >= 8) return;

        _co.SetTileFlags(tot, _co.GetTileFlags(tot) & ~TileFlags.LockTransform);
        _rung.Add(new Rung { o = tot, t = 0f, day = totSp.bounds.min.y, goc = _co.GetTransformMatrix(tot) });
        Vector3 dinh = _co.transform.TransformPoint(_co.CellToLocalInterpolated(tot + _co.tileAnchor) + new Vector3(0f, totSp.bounds.max.y * 0.7f, 0f));
        LaBay(dinh, 4);
    }

    private void CapNhatRungBui(float dt)
    {
        if (_co == null) { _rung.Clear(); return; }
        for (int i = _rung.Count - 1; i >= 0; i--)
        {
            var r = _rung[i];
            r.t += dt;
            float k = Mathf.Clamp01(r.t / 0.6f);
            float tat = (1f - k) * (1f - k);
            float nghieng = 0.2f * Mathf.Sin(k * Mathf.PI * 5f) * tat;          // lac trai phai quanh goc bui
            float nen = 1f - 0.07f * Mathf.Sin(k * Mathf.PI * 6f) * tat;
            var m = Matrix4x4.identity;
            m.m01 = nghieng; m.m11 = nen;
            m.m03 = -nghieng * r.day;                  // giu goc bui (day) dung yen
            m.m13 = r.day * (1f - nen);
            _co.SetTransformMatrix(r.o, r.goc * m);
            if (k >= 1f) { _co.SetTransformMatrix(r.o, r.goc); _rung.RemoveAt(i); }
            else _rung[i] = r;
        }
    }

    private void TimNuoc()
    {
        _nuoc = null; _datPhu.Clear();
        var tms = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        TilemapRenderer rNuoc = null;
        foreach (var tm in tms)
            if (tm.name == tenTilemapNuoc) { _nuoc = tm; rNuoc = tm.GetComponent<TilemapRenderer>(); break; }
        if (_nuoc == null) return;
        // Cac lop dat ve TREN nuoc, cung luoi -> o nao co dat thi khong dat lap lanh (do phi slot)
        foreach (var tm in tms)
        {
            if (tm == _nuoc || tm.layoutGrid != _nuoc.layoutGrid) continue;
            var r = tm.GetComponent<TilemapRenderer>();
            if (r == null || !r.enabled || rNuoc == null) continue;
            bool tren = SortingLayer.GetLayerValueFromID(r.sortingLayerID) > SortingLayer.GetLayerValueFromID(rNuoc.sortingLayerID)
                     || (r.sortingLayerID == rNuoc.sortingLayerID && r.sortingOrder > rNuoc.sortingOrder);
            if (tren && _datPhu.Count < 6) _datPhu.Add(tm);
        }
    }

    // =====================================================================
    //  Update
    // =====================================================================

    private void Update()
    {
        if (!_daDung) return;
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        bool an = FarmInputLock.IsCookingMode;
        if (_goc.gameObject.activeSelf == an) _goc.gameObject.SetActive(!an);
        if (an) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;                 // popup dung game -> khong ton CPU
        _tAm += dt;

        float h = _cam.orthographicSize, w = h * _cam.aspect;
        Vector3 c = _cam.transform.position;
        _khung = new Rect(c.x - w, c.y - h, w * 2f, h * 2f);

        if (Time.time >= _henKiemDem) KiemDem();

        if (bongMay) CapNhatMay(dt); else TatNhom(_may);
        if (lapLanhNuoc && _nuoc != null) { CapNhatLapLanh(dt); CapNhatSong(dt); } else { TatNhom(_lap); TatNhom(_song); }
        if (buom && !_laDem) CapNhatBuom(dt); else TatNhom(_buom);
        if (chim) CapNhatChim(dt); else { TatNhom(_chim); TatNhom(_bongChim); _dangChimBay = false; }
        if (domDom && _laDem) CapNhatDom(dt); else TatNhom(_dom);
        XuLyChamBui();
        if (_rung.Count > 0) CapNhatRungBui(dt);
        CapNhatBui(dt);
        CapNhatSao(dt);
    }

    private void KiemDem()
    {
        _henKiemDem = Time.time + 10f;
        if (domDomCheDo > 0) { _laDem = true; return; }
        if (domDomCheDo < 0) { _laDem = false; return; }
        int gio = DateTime.Now.Hour;
        _laDem = gioToiBatDau > gioSangKetThuc ? (gio >= gioToiBatDau || gio < gioSangKetThuc)
                                               : (gio >= gioToiBatDau && gio < gioSangKetThuc);
    }

    // ── Bong may: moi cum = vai bong tron mo chong len nhau (hinh dam may nho), troi cham theo gio ──
    private void DatCum(int c, bool trongKhung)
    {
        int moi = _may.Length / _cumMay.Length;
        float x = trongKhung ? UnityEngine.Random.Range(_khung.xMin, _khung.xMax) : _khung.xMin - 500f;
        float y = UnityEngine.Random.Range(_khung.yMin - 200f, _khung.yMax + 200f);
        _cumMay[c] = new Vector3(x, y, 0f);
        _cumMo[c] = 0f;
        for (int j = 0; j < moi; j++)
        {
            var h = _may[c * moi + j];
            Vector2 r = UnityEngine.Random.insideUnitCircle * 130f;
            _lechBong[c * moi + j] = new Vector3(r.x * 1.4f, r.y * 0.6f, 0f);
            h.kt = UnityEngine.Random.Range(bongMayKichThuoc.x, bongMayKichThuoc.y);
            h.pos = _cumMay[c] + _lechBong[c * moi + j];
            Bat(h);
            float s = h.kt / h.ks;
            h.tf.localScale = new Vector3(s * 1.3f, s * 0.75f, 1f);
            h.sr.color = new Color(0.05f, 0.08f, 0.15f, 0f);
        }
    }

    private void CapNhatMay(float dt)
    {
        int moi = _may.Length / _cumMay.Length;
        Vector3 gio = new Vector3(1f, -0.18f, 0f) * bongMayToc;
        for (int c = 0; c < _cumMay.Length; c++)
        {
            if (_cumMo[c] < 0f) DatCum(c, true);
            _cumMay[c] += gio * dt;
            // Ra khoi khung (troi di / keo camera xa) -> dat lai cho khac trong khung, hien RAT cham (khong loe)
            bool ra = _cumMay[c].x - 450f > _khung.xMax || _cumMay[c].y + 450f < _khung.yMin
                   || _cumMay[c].x + 1400f < _khung.xMin || _cumMay[c].y - 900f > _khung.yMax;
            if (ra) DatCum(c, true);
            _cumMo[c] = Mathf.Min(1f, _cumMo[c] + dt * 0.25f);
            float a = bongMayAlpha * _cumMo[c];
            for (int j = 0; j < moi; j++)
            {
                var h = _may[c * moi + j];
                h.tf.position = _cumMay[c] + _lechBong[c * moi + j];
                if (_cumMo[c] < 1f) h.sr.color = new Color(0.05f, 0.08f, 0.15f, a);
            }
        }
    }

    // ── Nuoc ──
    private bool LaNuoc(Vector3 p)
    {
        Vector3Int o = _nuoc.WorldToCell(p);
        if (!_nuoc.HasTile(o)) return false;
        for (int i = 0; i < _datPhu.Count; i++)
            if (_datPhu[i] != null && _datPhu[i].HasTile(o)) return false;
        return true;
    }

    private bool TimDiemNuoc(out Vector3 p)
    {
        for (int k = 0; k < 3; k++)
        {
            p = new Vector3(UnityEngine.Random.Range(_khung.xMin, _khung.xMax), UnityEngine.Random.Range(_khung.yMin, _khung.yMax), 0f);
            if (LaNuoc(p)) return true;
        }
        p = Vector3.zero;
        return false;
    }

    private void CapNhatLapLanh(float dt)
    {
        if (_tAm >= _henLap)
        {
            _henLap = _tAm + 0.09f;
            Hat r = null;
            for (int i = 0; i < _lap.Length; i++) if (!_lap[i].song) { r = _lap[i]; break; }
            if (r != null && TimDiemNuoc(out Vector3 p))
            {
                r.pos = p; r.tuoi = 0f; r.doi = UnityEngine.Random.Range(0.8f, 1.5f);
                r.kt = UnityEngine.Random.Range(lapLanhKichThuoc.x, lapLanhKichThuoc.y);
                r.a0 = UnityEngine.Random.Range(0.55f, 0.95f);
                r.pha = UnityEngine.Random.Range(0f, 45f);
                Bat(r);
                r.tf.position = p;
                r.sr.color = new Color(1f, 1f, 1f, 0f);
            }
        }
        for (int i = 0; i < _lap.Length; i++)
        {
            var r = _lap[i];
            if (!r.song) continue;
            r.tuoi += dt;
            float k = r.tuoi / r.doi;
            if (k >= 1f) { Tat(r); continue; }
            float s = Mathf.Sin(k * Mathf.PI);
            float kt = r.kt * (0.35f + 0.65f * s) / r.ks;
            r.tf.localScale = new Vector3(kt, kt, 1f);
            r.tf.localRotation = Quaternion.Euler(0f, 0f, r.pha + k * 40f);
            r.sr.color = new Color(1f, 1f, 1f, r.a0 * s);
        }
    }

    private void CapNhatSong(float dt)
    {
        if (_tAm >= _henSong)
        {
            _henSong = _tAm + UnityEngine.Random.Range(1.1f, 2.4f);
            Hat r = null;
            for (int i = 0; i < _song.Length; i++) if (!_song[i].song) { r = _song[i]; break; }
            if (r != null && TimDiemNuoc(out Vector3 p))
            {
                r.pos = p; r.tuoi = 0f; r.doi = UnityEngine.Random.Range(1.6f, 2.2f);
                r.kt = gonSongKichThuoc * UnityEngine.Random.Range(0.7f, 1.2f);
                Bat(r);
                r.tf.position = p;
            }
        }
        for (int i = 0; i < _song.Length; i++)
        {
            var r = _song[i];
            if (!r.song) continue;
            r.tuoi += dt;
            float k = r.tuoi / r.doi;
            if (k >= 1f) { Tat(r); continue; }
            float e = 1f - (1f - k) * (1f - k);
            float kt = r.kt * (0.25f + 0.75f * e) / r.ks;
            r.tf.localScale = new Vector3(kt, kt * 0.48f, 1f);
            r.sr.color = new Color(1f, 1f, 1f, 0.38f * (1f - k) * Mathf.Clamp01(k * 6f));
        }
    }

    // ── Buom ──
    private static readonly Color[] MauBuom =
    {
        new Color(1f, 0.92f, 0.45f), new Color(1f, 0.72f, 0.82f), new Color(0.98f, 0.98f, 0.95f),
        new Color(0.7f, 0.85f, 1f), new Color(1f, 0.7f, 0.4f)
    };

    private void CapNhatBuom(float dt)
    {
        for (int i = 0; i < _buom.Length; i++)
        {
            var b = _buom[i];
            if (!b.song || !_khung.Overlaps(new Rect(b.goc.x - 600f, b.goc.y - 600f, 1200f, 1200f)))
            {
                // Neo moi trong khung hinh (tren dat, khong tren nuoc)
                Vector3 p = new Vector3(UnityEngine.Random.Range(_khung.xMin, _khung.xMax), UnityEngine.Random.Range(_khung.yMin, _khung.yMax), 0f);
                if (_nuoc != null && LaNuoc(p)) continue;
                b.goc = p; b.pos = p; b.van = Vector3.zero;
                b.pha = UnityEngine.Random.Range(0f, 10f);
                b.mau = MauBuom[UnityEngine.Random.Range(0, MauBuom.Length)];
                b.tuoi = 0f; b.doi = 0f; b.mo = 0f; b.trangThai = 0;
                Bat(b);
            }
            b.tuoi += dt;
            b.pha += dt;
            float kt0 = buomKichThuoc / b.ks;

            if (b.trangThai == 2)
            {
                // DANG DAU: dung yen, canh xoe/khep cham, thinh thoang vo 1 nhip
                b.tf.position = b.dich;
                float chuKy = b.pha % 3f;
                float vo = chuKy < 0.5f ? 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(chuKy * 14f))
                                        : 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(b.pha * 1.6f));
                b.tf.localScale = new Vector3(kt0 * vo, kt0, 1f);
                b.tf.localRotation = Quaternion.identity;
                if (b.tuoi >= b.doi)
                {
                    b.trangThai = 0; b.tuoi = 0f; b.doi = UnityEngine.Random.Range(0.8f, 1.8f);
                    b.goc = b.dich;
                    Vector2 r = UnityEngine.Random.insideUnitCircle * 200f; b.a0 = r.x; b.kt = r.y;
                    b.van = new Vector3(UnityEngine.Random.Range(-30f, 30f), 50f, 0f);   // cat canh bay len
                }
            }
            else
            {
                if (b.tuoi >= b.doi)
                {
                    b.tuoi = 0f;
                    if (UnityEngine.Random.value < buomTiLeDau && TimChoDau(b, out Vector3 cho))
                    {
                        b.trangThai = 1; b.dich = cho; b.doi = 9f;      // toi da 9s de bay toi, qua thi bo
                    }
                    else
                    {
                        b.trangThai = 0; b.doi = UnityEngine.Random.Range(1.5f, 3.5f);
                        Vector2 r = UnityEngine.Random.insideUnitCircle * 260f; b.a0 = r.x; b.kt = r.y;
                    }
                }
                Vector3 dich = b.trangThai == 1 ? b.dich : b.goc + new Vector3(b.a0, b.kt, 0f);
                Vector3 muon = dich - b.pos;
                float d = muon.magnitude;
                if (b.trangThai == 1 && d < 10f)
                {
                    b.trangThai = 2; b.tuoi = 0f; b.doi = UnityEngine.Random.Range(buomDauGiay.x, buomDauGiay.y);
                    b.pos = b.dich; b.van = Vector3.zero; b.pha = 0f;
                    continue;
                }
                float toc = buomToc * (b.trangThai == 1 ? Mathf.Clamp01(d / 120f) * 0.6f + 0.4f : 1f);   // cham dan khi sap dau
                if (d > 1f) muon = muon / d * toc;
                b.van = Vector3.Lerp(b.van, muon, dt * 1.8f);
                b.pos += b.van * dt;
                float nhun = Mathf.Sin(b.pha * 3.1f) * 10f * (b.trangThai == 1 ? Mathf.Clamp01(d / 80f) : 1f);
                b.tf.position = b.pos + new Vector3(0f, nhun, 0f);
                float vo = 0.2f + 0.8f * Mathf.Abs(Mathf.Sin(b.pha * 17f));
                b.tf.localScale = new Vector3(kt0 * vo, kt0, 1f);
                b.tf.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-b.van.x * 0.25f, -25f, 25f));
            }
            if (b.mo < 1f) { b.mo = Mathf.Min(1f, b.mo + dt * 1.2f); b.sr.color = new Color(b.mau.r, b.mau.g, b.mau.b, b.mo); }
        }
    }

    /// <summary>Cho dau: ngon hoa / o vua gieo hat dang trong khung hinh, chua co buom khac dau.</summary>
    private bool TimChoDau(Hat buomNay, out Vector3 cho)
    {
        cho = Vector3.zero;
        var ds = PlotCropVisual.DiemDauBuom;
        int n = ds.Count;
        if (n == 0) return false;
        int batDau = UnityEngine.Random.Range(0, n);
        for (int k = 0; k < n; k++)
        {
            var v = ds[(batDau + k) % n];
            if (v == null || !v.LayDiemDau(out Vector3 p)) continue;
            if (!_khung.Contains(p)) continue;
            bool daCo = false;
            for (int j = 0; j < _buom.Length; j++)
            {
                var o = _buom[j];
                if (o != buomNay && o.song && o.trangThai != 0 && (o.dich - p).sqrMagnitude < 60f * 60f) { daCo = true; break; }
            }
            if (daCo) continue;
            cho = p;
            return true;
        }
        return false;
    }

    // ── Chim (bay tren cao: phoi canh + bong duoi dat) ──
    // G = diem duoi dat ngay duoi con chim (troi theo the gioi). Chim ve o G + (G - tam camera) x doCao:
    // dung cach camera nhin tu tren xuong thay vat o tren cao -> chim troi NHANH hon mat dat khi keo
    // ban do, to hon, va co bong mo lech duoi dat.
    private void CapNhatChim(float dt)
    {
        Vector3 tam = new Vector3(_khung.center.x, _khung.center.y, 0f);
        if (!_dangChimBay)
        {
            if (Time.time < _henChim) return;
            _henChim = Time.time + UnityEngine.Random.Range(chimCachGiay.x, chimCachGiay.y);
            _soChimDan = Mathf.Clamp(UnityEngine.Random.Range(3, _chim.Length + 1), 1, _chim.Length);
            if (VfxCauHinh.CheDoNhe) _soChimDan = Mathf.Max(1, _soChimDan / 2);
            bool tuTrai = UnityEngine.Random.value < 0.5f;
            _chimHuong = new Vector3(tuTrai ? 1f : -1f, UnityEngine.Random.Range(-0.35f, 0.35f), 0f).normalized;
            float heSo = 1f + chimDoCao;
            float ra = _khung.width * 0.5f / heSo + chimKichThuoc * 2f;   // G o ngoai mep de chim (da day ra) cung ngoai
            Vector3 dau = new Vector3(tam.x + (tuTrai ? -ra : ra),
                                      tam.y + UnityEngine.Random.Range(-0.3f, 0.3f) * _khung.height / heSo, 0f);
            for (int i = 0; i < _soChimDan; i++)
            {
                var b = _chim[i];
                int hang = (i + 1) / 2; float ben = (i % 2 == 0) ? 1f : -1f;
                b.pos = dau - _chimHuong * (hang * 110f) + new Vector3(0f, ben * hang * 70f, 0f);
                b.pha = UnityEngine.Random.Range(0f, 6f);
                b.kt = chimKichThuoc * UnityEngine.Random.Range(0.85f, 1.1f);
                Bat(b);
                b.sr.color = new Color(0.22f, 0.22f, 0.28f, 0.92f);
                b.sr.flipX = _chimHuong.x < 0f;
                var bg = _bongChim[i];
                bg.pos = b.pos; Bat(bg);
                bg.sr.color = new Color(0f, 0f, 0f, chimBongAlpha);
                bg.sr.flipX = b.sr.flipX;
            }
            _dangChimBay = true;
        }

        bool conTrong = false;
        float ngoai = _khung.width * 0.5f + chimKichThuoc * 3f;
        for (int i = 0; i < _soChimDan; i++)
        {
            var b = _chim[i];
            var bg = _bongChim[i];
            if (!b.song) continue;
            b.pha += dt;
            b.pos += _chimHuong * chimToc * dt;                       // G: diem duoi dat
            float chuKy = b.pha % 2.6f;                                 // vo canh 1.6s roi luot 1s
            float vo = chuKy < 1.6f ? Mathf.Abs(Mathf.Sin(chuKy * 7f)) : 0.5f;
            Vector3 veChim = b.pos + (b.pos - tam) * chimDoCao + new Vector3(0f, Mathf.Sin(b.pha * 1.1f) * 10f, 0f);
            float kt0 = b.kt * (1f + chimDoCao) / b.ks;
            b.tf.position = veChim;
            b.tf.localScale = new Vector3(kt0, kt0 * (0.3f + 0.7f * vo), 1f);
            float nghieng = Mathf.Atan2(_chimHuong.y, Mathf.Abs(_chimHuong.x)) * Mathf.Rad2Deg * (b.sr.flipX ? -1f : 1f);
            b.tf.localRotation = Quaternion.Euler(0f, 0f, nghieng);
            // Bong duoi dat: nho hon, mo, khong phoi canh
            float ktb = b.kt * 0.7f / bg.ks;
            bg.tf.position = b.pos + new Vector3(chimLechBong.x, chimLechBong.y, 0f);
            bg.tf.localScale = new Vector3(ktb, ktb * (0.3f + 0.7f * vo) * 0.6f, 1f);
            bg.tf.localRotation = b.tf.localRotation;

            float xChim = veChim.x - tam.x;
            bool quaMep = Mathf.Abs(xChim) > ngoai && Mathf.Sign(xChim) == Mathf.Sign(_chimHuong.x)
                          && Mathf.Abs(b.pos.x - tam.x) > _khung.width * 0.5f + 200f;
            if (quaMep) { Tat(b); Tat(bg); }
            else conTrong = true;
        }
        if (!conTrong) _dangChimBay = false;
    }

    // ── Dom dom ──
    private void CapNhatDom(float dt)
    {
        for (int i = 0; i < _dom.Length; i++)
        {
            var f = _dom[i];
            if (!f.song || !_khung.Contains(f.goc))
            {
                f.goc = new Vector3(UnityEngine.Random.Range(_khung.xMin, _khung.xMax), UnityEngine.Random.Range(_khung.yMin, _khung.yMax), 0f);
                f.pha = UnityEngine.Random.Range(0f, 20f);
                f.kt = domDomKichThuoc * UnityEngine.Random.Range(0.7f, 1.2f);
                Bat(f);
                float kt0 = f.kt / f.ks;
                f.tf.localScale = new Vector3(kt0, kt0, 1f);
            }
            f.pha += dt;
            f.goc += new Vector3(Mathf.Sin(f.pha * 0.21f), Mathf.Cos(f.pha * 0.17f), 0f) * 8f * dt;
            f.tf.position = f.goc + new Vector3(Mathf.Sin(f.pha * 0.9f) * 50f, Mathf.Cos(f.pha * 0.7f) * 32f, 0f);
            float nhay = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(f.pha * 2.3f + i));
            f.sr.color = new Color(0.85f, 1f, 0.45f, 0.15f + 0.8f * nhay * nhay);
        }
    }

    // ── Bui + sao (pool su kien) ──
    private void CapNhatBui(float dt)
    {
        for (int i = 0; i < _bui.Length; i++)
        {
            var h = _bui[i];
            if (!h.song) continue;
            h.tuoi += dt;
            float k = h.tuoi / h.doi;
            if (k >= 1f) { Tat(h); continue; }
            h.pos += h.van * dt;
            h.van *= 1f - Mathf.Min(1f, dt * 3.5f);
            h.tf.position = h.pos;
            float kt = h.kt * (0.5f + 0.8f * k) / h.ks;
            h.tf.localScale = new Vector3(kt, kt * 0.8f, 1f);
            h.sr.color = new Color(h.mau.r, h.mau.g, h.mau.b, h.a0 * (1f - k));
        }
    }

    private void CapNhatSao(float dt)
    {
        for (int i = 0; i < _sao.Length; i++)
        {
            var h = _sao[i];
            if (!h.song) continue;
            h.tuoi += dt;
            float k = h.tuoi / h.doi;
            if (k >= 1f) { Tat(h); continue; }
            float s = Mathf.Sin(k * Mathf.PI);
            float kt = h.kt * s / h.ks;
            h.tf.position = h.pos + new Vector3(0f, 18f * k, 0f);
            h.tf.localScale = new Vector3(kt, kt, 1f);
            h.tf.localRotation = Quaternion.Euler(0f, 0f, h.pha + 90f * k);
            h.sr.color = new Color(1f, 0.96f, 0.7f, s);
        }
    }

    // =====================================================================
    //  Tien ich
    // =====================================================================

    private void Bat(Hat h)
    {
        h.song = true;
        if (h.tf.gameObject.activeSelf == false) h.tf.gameObject.SetActive(true);
        h.sr.enabled = true;
        h.tf.position = h.pos;
    }

    private static void Tat(Hat h)
    {
        h.song = false;
        h.sr.enabled = false;
    }

    private static void TatNhom(Hat[] ds)
    {
        if (ds == null) return;
        for (int i = 0; i < ds.Length; i++) if (ds[i].song) Tat(ds[i]);
    }

    private static float KichSprite(SpriteRenderer sr)
    {
        var sp = sr.sprite;
        return sp != null ? Mathf.Max(0.0001f, sp.bounds.size.x) : 1f;
    }
}
