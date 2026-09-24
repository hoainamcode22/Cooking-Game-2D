// ============================================================================
//  FarmAmbientFX — HIEU UNG MOI TRUONG + HAT NHO cho nong trai, TOI UU NHE (2026-09-24)
// ----------------------------------------------------------------------------
//  Mot component, MOT vong Update cho tat ca (khong Update rieng tung hat, khong Instantiate
//  luc choi): moi hat la SpriteRenderer tao san 1 lan (pool), chi bat/tat renderer.
//  Dung chung 1 bo sprite ve bang code (SoftFxSprites, < 0.5 MB) -> cung texture + material
//  -> Unity gop draw call duoc.
//    Moi truong:  bong may troi tren dong · lap lanh + gon song tren mat nuoc (chi o Water_Tilemap
//                 trong khung hinh) · buom bay quanh (ban ngay) · dan chim bay ngang (thinh thoang)
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
    [SerializeField] private bool bongMay = true;
    [SerializeField] private bool lapLanhNuoc = true;
    [SerializeField] private bool buom = true;
    [SerializeField] private bool chim = true;
    [SerializeField] private bool domDom = true;
    [Tooltip("Dom dom: 0 = tu dong theo gio may, 1 = luon bat (de xem thu), -1 = luon tat")]
    [SerializeField] private int domDomCheDo = 0;
    [SerializeField] private int gioToiBatDau = 18;
    [SerializeField] private int gioSangKetThuc = 6;

    [Header("So luong (may yeu tu giam 1/2)")]
    [SerializeField] private int soBongMay = 3;
    [SerializeField] private int soLapLanh = 14;
    [SerializeField] private int soGonSong = 4;
    [SerializeField] private int soBuom = 4;
    [SerializeField] private int soChimToiDa = 5;
    [SerializeField] private int soDomDom = 14;
    [SerializeField] private int poolBui = 20;
    [SerializeField] private int poolSao = 24;

    [Header("Bong may")]
    [SerializeField] private float bongMayAlpha = 0.08f;
    [SerializeField] private Vector2 bongMayKichThuoc = new Vector2(1600f, 2600f);
    [SerializeField] private float bongMayToc = 35f;

    [Header("Nuoc")]
    [SerializeField] private string tenTilemapNuoc = "Water_Tilemap";
    [SerializeField] private Vector2 lapLanhKichThuoc = new Vector2(22f, 42f);
    [SerializeField] private float gonSongKichThuoc = 170f;

    [Header("Buom / chim / dom dom")]
    [SerializeField] private float buomKichThuoc = 34f;
    [SerializeField] private float buomToc = 70f;
    [SerializeField] private Vector2 chimCachGiay = new Vector2(25f, 50f);
    [SerializeField] private float chimToc = 260f;
    [SerializeField] private float chimKichThuoc = 48f;
    [SerializeField] private float domDomKichThuoc = 40f;

    [Header("Sorting")]
    [SerializeField] private string lopTren = "ObjectsFront";
    [SerializeField] private int thuTuBongMay = 32000;
    [SerializeField] private int thuTuBuom = 32300;
    [SerializeField] private int thuTuDomDom = 32400;
    [SerializeField] private int thuTuSao = 32500;
    [SerializeField] private int thuTuChim = 32700;
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
        public Vector3 pos, van, goc;
        public Color mau;
    }

    private Hat[] _may, _lap, _song, _buom, _chim, _dom, _bui, _sao;
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
    private Transform _goc;
    private float _tAm;

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

        _may  = TaoNhom("Fx_BongMay",  VfxCauHinh.SoLuong(soBongMay), SoftFxSprites.GlowSprite,    lopTren, thuTuBongMay);
        _lap  = TaoNhom("Fx_LapLanhNuoc", VfxCauHinh.SoLuong(soLapLanh), SoftFxSprites.SparkleSprite, lopDat, thuTuNuoc);
        _song = TaoNhom("Fx_GonSong",  VfxCauHinh.SoLuong(soGonSong), SoftFxSprites.RingSprite,   lopDat, thuTuNuoc);
        _buom = TaoNhom("Fx_Buom",     VfxCauHinh.SoLuong(soBuom),    SoftFxSprites.ButterflySprite, lopTren, thuTuBuom);
        _chim = TaoNhom("Fx_Chim",     Mathf.Max(1, soChimToiDa),     SoftFxSprites.BirdSprite,   lopTren, thuTuChim);
        _dom  = TaoNhom("Fx_DomDom",   VfxCauHinh.SoLuong(soDomDom),  SoftFxSprites.GlowSprite,   lopTren, thuTuDomDom);
        _bui  = TaoNhom("Fx_Bui",      VfxCauHinh.SoLuong(poolBui),   SoftFxSprites.CircleSprite, lopDat, thuTuBui);
        _sao  = TaoNhom("Fx_Sao",      VfxCauHinh.SoLuong(poolSao),   SoftFxSprites.SparkleSprite, lopTren, thuTuSao);

        TimNuoc();
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
        if (chim) CapNhatChim(dt); else TatNhom(_chim);
        if (domDom && _laDem) CapNhatDom(dt); else TatNhom(_dom);
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

    // ── Bong may ──
    private Rect VungMay()
    {
        var mb = MapBoundary.Instance;
        if (mb != null)
        {
            Vector4 b = mb.GetCurrentBounds();
            if (b.y > b.x && b.w > b.z) return Rect.MinMaxRect(b.x, b.z, b.y, b.w);
        }
        return new Rect(_khung.x - _khung.width, _khung.y - _khung.height, _khung.width * 3f, _khung.height * 3f);
    }

    private void CapNhatMay(float dt)
    {
        Rect v = VungMay();
        for (int i = 0; i < _may.Length; i++)
        {
            var m = _may[i];
            if (!m.song)
            {
                m.kt = UnityEngine.Random.Range(bongMayKichThuoc.x, bongMayKichThuoc.y);
                m.pos = new Vector3(UnityEngine.Random.Range(v.xMin, v.xMax), UnityEngine.Random.Range(v.yMin, v.yMax), 0f);
                m.van = new Vector3(1f, -0.25f, 0f).normalized * bongMayToc * UnityEngine.Random.Range(0.8f, 1.25f);
                m.pha = UnityEngine.Random.Range(0f, 10f);
                m.mau = new Color(0.05f, 0.08f, 0.15f, bongMayAlpha);
                Bat(m);
                m.tf.localScale = new Vector3(m.kt, m.kt * 0.62f, 1f) / m.ks;
                m.sr.color = m.mau;
            }
            m.pos += m.van * dt;
            if (m.pos.x - m.kt > v.xMax || m.pos.y + m.kt < v.yMin)
            {
                m.pos = new Vector3(v.xMin - m.kt * 0.5f, UnityEngine.Random.Range(v.yMin, v.yMax + m.kt * 0.3f), 0f);
            }
            // chi day vi tri khi gan khung hinh (xa thi khoi ghi transform)
            if (Gan(m.pos, m.kt)) m.tf.position = m.pos;
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
                b.tuoi = 0f; b.doi = 0f; b.mo = 0f;
                Bat(b);
            }
            // Muc tieu moi moi 1.5-3.5s, quanh diem neo
            b.tuoi += dt;
            if (b.tuoi >= b.doi)
            {
                b.tuoi = 0f; b.doi = UnityEngine.Random.Range(1.5f, 3.5f);
                Vector2 r = UnityEngine.Random.insideUnitCircle * 260f;
                b.a0 = r.x; b.kt = r.y;   // dung a0/kt lam toa do muc tieu (khong cap phat)
            }
            Vector3 dich = b.goc + new Vector3(b.a0, b.kt, 0f);
            Vector3 muon = (dich - b.pos);
            float d = muon.magnitude;
            if (d > 1f) muon = muon / d * buomToc;
            b.van = Vector3.Lerp(b.van, muon, dt * 1.8f);
            b.pha += dt;
            b.pos += b.van * dt;
            float nhun = Mathf.Sin(b.pha * 3.1f) * 10f;
            b.tf.position = b.pos + new Vector3(0f, nhun, 0f);
            float vo = 0.2f + 0.8f * Mathf.Abs(Mathf.Sin(b.pha * 17f));
            float kt0 = buomKichThuoc / b.ks;
            b.tf.localScale = new Vector3(kt0 * vo, kt0, 1f);
            b.tf.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-b.van.x * 0.25f, -25f, 25f));
            if (b.mo < 1f) { b.mo = Mathf.Min(1f, b.mo + dt * 1.2f); b.sr.color = new Color(b.mau.r, b.mau.g, b.mau.b, b.mo); }
        }
    }

    // ── Chim ──
    private void CapNhatChim(float dt)
    {
        if (!_dangChimBay)
        {
            if (Time.time < _henChim) return;
            _henChim = Time.time + UnityEngine.Random.Range(chimCachGiay.x, chimCachGiay.y);
            _soChimDan = Mathf.Clamp(UnityEngine.Random.Range(3, _chim.Length + 1), 1, _chim.Length);
            if (VfxCauHinh.CheDoNhe) _soChimDan = Mathf.Max(1, _soChimDan / 2);
            bool tuTrai = UnityEngine.Random.value < 0.5f;
            _chimHuong = new Vector3(tuTrai ? 1f : -1f, UnityEngine.Random.Range(-0.12f, 0.18f), 0f).normalized;
            Vector3 dau = new Vector3(tuTrai ? _khung.xMin - 150f : _khung.xMax + 150f,
                                      Mathf.Lerp(_khung.yMin, _khung.yMax, UnityEngine.Random.Range(0.45f, 0.85f)), 0f);
            for (int i = 0; i < _soChimDan; i++)
            {
                var b = _chim[i];
                int hang = (i + 1) / 2; float ben = (i % 2 == 0) ? 1f : -1f;
                b.pos = dau - _chimHuong * (hang * 70f) + new Vector3(0f, ben * hang * 42f, 0f);
                b.pha = UnityEngine.Random.Range(0f, 6f);
                b.kt = chimKichThuoc * UnityEngine.Random.Range(0.85f, 1.1f);
                Bat(b);
                b.sr.color = new Color(0.18f, 0.18f, 0.24f, 0.85f);
                b.sr.flipX = _chimHuong.x < 0f;
            }
            _dangChimBay = true;
        }

        bool conTrong = false;
        float ngoai = _khung.width * 0.5f + 400f;
        for (int i = 0; i < _soChimDan; i++)
        {
            var b = _chim[i];
            if (!b.song) continue;
            b.pha += dt;
            b.pos += _chimHuong * chimToc * dt;
            // Vo canh 1.6s roi luot 0.8s (tu nhien hon vo lien tuc)
            float chuKy = b.pha % 2.4f;
            float vo = chuKy < 1.6f ? Mathf.Abs(Mathf.Sin(chuKy * 9f)) : 0.55f;
            float kt0 = b.kt / b.ks;
            b.tf.localScale = new Vector3(kt0, kt0 * (0.3f + 0.7f * vo), 1f);
            b.tf.position = b.pos + new Vector3(0f, Mathf.Sin(b.pha * 1.3f) * 6f, 0f);
            if (Mathf.Abs(b.pos.x - _khung.center.x) > ngoai && Mathf.Sign(b.pos.x - _khung.center.x) == Mathf.Sign(_chimHuong.x)) Tat(b);
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
            h.sr.color = new Color(0.62f, 0.5f, 0.36f, h.a0 * (1f - k));
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

    private bool Gan(Vector3 p, float r) =>
        p.x + r > _khung.xMin && p.x - r < _khung.xMax && p.y + r > _khung.yMin && p.y - r < _khung.yMax;

    private static float KichSprite(SpriteRenderer sr)
    {
        var sp = sr.sprite;
        return sp != null ? Mathf.Max(0.0001f, sp.bounds.size.x) : 1f;
    }
}
