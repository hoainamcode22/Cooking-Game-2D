// ============================================================================
//  FarmerCrew — DOI NONG DAN cua nong trai (2026-09-24)
//  - Tu tao "Farmer_Crew" khi vao scene co o dat (neu Hierarchy chua co). Muon chinh tay: Tools >
//    Farm Game > Nong Dan > 2 tao san trong Hierarchy, keo FarmerConfig vao, sua tham so tuy y.
//  - Moi 1.5s xem o nao DANG TRONG: ruong thuong chia nhom 4 o gan nhau -> 1 ong / nhom
//    (4 o = 1 ong, 8 o = 2 ong, 9 o = 3 ong). Chau hoa dang trong -> 1 ong chi tuoi nuoc.
//  - Chi chia lai khi tap o dang trong THAY DOI (gieo / thu hoach), khong lam gi thua.
//  - Giu 1 pool bong mo (vet di) dung chung cho moi ong: toi da 24 bong, khong cap phat moi.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class FarmerCrew : MonoBehaviour
{
    public static FarmerCrew Instance { get; private set; }

    [SerializeField] private FarmerConfig config;
    [Tooltip("Tat = khong co ong nong dan nao.")]
    [SerializeField] private bool batNongDan = true;
    [Tooltip("Ong chau hoa (chi tuoi nuoc, di tu chau nay sang chau khac).")]
    [SerializeField] private bool batNguoiTuoiChau = true;

    private readonly List<PlotController> _tatCaO = new List<PlotController>(64);
    private readonly List<PlotController> _ruong = new List<PlotController>(64);
    private readonly List<PlotController> _chau = new List<PlotController>(32);
    private readonly List<FarmerNPC> _nongDan = new List<FarmerNPC>(8);
    private FarmerNPC _nguoiChau;
    private float _henQuetO, _henPhanCong;
    private long _dauRuong = long.MinValue, _dauChau = long.MinValue;

    private static int _layerCongTrinh = int.MinValue;
    private static bool _daBaoThieuConfig;

    // ── Thong tin o (cache) ──
    private class ThongTinO
    {
        public SpriteRenderer dat;
        public SpriteRenderer[] cay = new SpriteRenderer[0];
        public float henLamMoiCay;
    }
    private readonly Dictionary<PlotController, ThongTinO> _o = new Dictionary<PlotController, ThongTinO>(64);

    // =====================================================================
    //  Tu khoi dong
    // =====================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void TuKhoiDong()
    {
        SceneManager.sceneLoaded -= KhiLoad;
        SceneManager.sceneLoaded += KhiLoad;
        ThuTao();
    }

    private static void KhiLoad(Scene s, LoadSceneMode m) => ThuTao();

    private static void ThuTao()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<FarmerCrew>(FindObjectsInactive.Include) != null) return;   // Hierarchy co san
        if (FindFirstObjectByType<PlotController>() == null) return;                          // khong phai scene farm
        var cfg = Resources.Load<FarmerConfig>("FarmerConfig");
        if (cfg == null)
        {
            if (!_daBaoThieuConfig)
            {
                _daBaoThieuConfig = true;
                Debug.Log("[NongDan] Chua co Resources/FarmerConfig -> chay Tools > Farm Game > Nong Dan > 1 de cat sprite + tao config.");
            }
            return;
        }
        var go = new GameObject("Farmer_Crew");
        var crew = go.AddComponent<FarmerCrew>();
        crew.config = cfg;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // =====================================================================
    private void Update()
    {
        if (config == null) config = Resources.Load<FarmerConfig>("FarmerConfig");
        if (config == null) return;
        float now = Time.time;
        if (now >= _henQuetO)
        {
            _henQuetO = now + 10f;                           // o dat moi dat trong Edit mode
            _tatCaO.Clear();
            _tatCaO.AddRange(FindObjectsByType<PlotController>(FindObjectsSortMode.None));
        }
        if (now >= _henPhanCong)
        {
            _henPhanCong = now + Mathf.Max(0.5f, config.chuKyQuet);
            PhanCong();
        }
        if (_bong.Count > 0) CapNhatBong(Time.deltaTime);
    }

    private void PhanCong()
    {
        _ruong.Clear(); _chau.Clear();
        long h1 = 0, h2 = 0, c1 = 0, c2 = 0;
        if (batNongDan)
            for (int i = 0; i < _tatCaO.Count; i++)
            {
                var p = _tatCaO[i];
                if (p == null || !p.isActiveAndEnabled || !p.IsPlanted) continue;
                int id = p.GetInstanceID();
                if (p.Category == PlotCategory.Flower)
                {
                    if (!batNguoiTuoiChau) continue;
                    _chau.Add(p); c1 += id; c2 ^= (long)id * 16777619L;
                }
                else { _ruong.Add(p); h1 += id; h2 ^= (long)id * 16777619L; }
            }

        long dauR = h1 * 31 + h2 + _ruong.Count;
        if (dauR != _dauRuong) { _dauRuong = dauR; ChiaNhomRuong(); }

        long dauC = c1 * 31 + c2 + _chau.Count;
        if (dauC != _dauChau)
        {
            _dauChau = dauC;
            if (_chau.Count > 0)
            {
                if (_nguoiChau == null) _nguoiChau = TaoNongDan("Farmer_Chau");
                _nguoiChau.GiaoViec(_chau, true);
            }
            else if (_nguoiChau != null) _nguoiChau.ThoiViec();
        }
    }

    /// <summary>Nhom 4 o gan nhau: lay o trai nhat lam hat giong, gom 3 o gan no nhat.</summary>
    private void ChiaNhomRuong()
    {
        int moiNguoi = Mathf.Max(1, config.soODatMoiNguoi);
        var con = new List<PlotController>(_ruong);
        var nhom = new List<List<PlotController>>();
        while (con.Count > 0)
        {
            int iHat = 0;
            for (int i = 1; i < con.Count; i++)
            {
                Vector3 a = con[i].transform.position, b = con[iHat].transform.position;
                if (a.x < b.x - 0.5f || (Mathf.Abs(a.x - b.x) <= 0.5f && a.y > b.y)) iHat = i;
            }
            var hat = con[iHat];
            con.RemoveAt(iHat);
            var g = new List<PlotController>(moiNguoi) { hat };
            Vector3 ph = hat.transform.position;
            while (g.Count < moiNguoi && con.Count > 0)
            {
                int gan = 0; float dGan = float.MaxValue;
                for (int i = 0; i < con.Count; i++)
                {
                    float d = (con[i].transform.position - ph).sqrMagnitude;
                    if (d < dGan) { dGan = d; gan = i; }
                }
                g.Add(con[gan]);
                con.RemoveAt(gan);
            }
            nhom.Add(g);
        }

        // Ghep nhom voi ong dang dung gan nhat (ong dang lam uu tien truoc ong dang an)
        // (Ban cu: mang daDung co dinh do dai -> tao ong thu 2 la vang IndexOutOfRange -> chi co 1 ong.)
        var daDung = new List<bool>(_nongDan.Count + nhom.Count);
        for (int j = 0; j < _nongDan.Count; j++) daDung.Add(false);
        for (int n = 0; n < nhom.Count; n++)
        {
            Vector3 p0 = nhom[n][0].transform.position;
            int chon = -1; float dChon = float.MaxValue;
            for (int j = 0; j < _nongDan.Count; j++)
            {
                if (j >= daDung.Count || daDung[j] || _nongDan[j] == null) continue;
                float d = (_nongDan[j].transform.position - p0).sqrMagnitude + (_nongDan[j].DangAn ? 1e12f : 0f);
                if (d < dChon) { dChon = d; chon = j; }
            }
            FarmerNPC f;
            if (chon >= 0) { daDung[chon] = true; f = _nongDan[chon]; }
            else { f = TaoNongDan("Farmer_" + (_nongDan.Count + 1)); _nongDan.Add(f); daDung.Add(true); }
            f.GiaoViec(nhom[n], false);
        }
        for (int j = 0; j < daDung.Count && j < _nongDan.Count; j++)
            if (!daDung[j] && _nongDan[j] != null) _nongDan[j].ThoiViec();
        Debug.Log($"[NongDan] {_ruong.Count} o ruong dang trong -> {nhom.Count} ong ({moiNguoi} o / ong).");
    }

    // =====================================================================
    //  Chau hoa: vi tri dat trong chau, vat can khi di, sorting canh chau
    // =====================================================================
    private readonly List<Vector4> _vatCan = new List<Vector4>(16);
    private float _henVatCan;

    /// <summary>Chau hoa: b = bounds hinh chau, dat = tam mat dat trong mieng chau.</summary>
    public static bool ThongTinChau(PlotController o, out Bounds b, out Vector3 dat)
    {
        b = new Bounds(); dat = Vector3.zero;
        var t = Instance != null ? Instance.LayO(o) : null;
        if (t == null || t.dat == null) return false;
        b = t.dat.bounds;
        dat = new Vector3(b.center.x, b.max.y - b.size.x * 0.24f, b.center.z);
        return true;
    }

    /// <summary>Vat can khi di bo: moi chau hoa = 1 elip chan chau (x, y, rx, ry).</summary>
    public static List<Vector4> VatCan()
    {
        if (Instance == null) return null;
        var me = Instance;
        if (Time.time >= me._henVatCan)
        {
            me._henVatCan = Time.time + 2f;
            me._vatCan.Clear();
            for (int i = 0; i < me._tatCaO.Count; i++)
            {
                var o = me._tatCaO[i];
                if (o == null || !o.isActiveAndEnabled || o.Category != PlotCategory.Flower) continue;
                var t = me.LayO(o);
                if (t == null || t.dat == null) continue;
                var b = t.dat.bounds;
                float rx = b.extents.x * 0.95f + 12f, ry = rx * 0.5f;
                me._vatCan.Add(new Vector4(b.center.x, b.min.y + ry * 0.9f, rx, ry));
            }
        }
        return me._vatCan;
    }

    /// <summary>
    /// Ong dung canh 1 chau: chan cao hon day chau -> dung SAU chau (bi chau + cay che),
    /// thap hon -> dung TRUOC. Tra false neu khong co chau nao sat ben.
    /// </summary>
    public static bool TinhSortChau(Vector3 p, out int layer, out int order, out float z)
    {
        layer = 0; order = 0; z = 0f;
        if (Instance == null) return false;
        var me = Instance;
        ThongTinO chon = null; float dChon = float.MaxValue;
        for (int i = 0; i < me._tatCaO.Count; i++)
        {
            var o = me._tatCaO[i];
            if (o == null || !o.isActiveAndEnabled || o.Category != PlotCategory.Flower) continue;
            var t = me.LayO(o);
            if (t == null || t.dat == null) continue;
            var b = t.dat.bounds;
            float dx = Mathf.Abs(p.x - b.center.x);
            if (dx > b.extents.x + 70f) continue;
            if (p.y < b.min.y - 60f || p.y > b.max.y + 20f) continue;
            float d = dx + Mathf.Abs(p.y - b.center.y);
            if (d < dChon) { dChon = d; chon = t; }
        }
        if (chon == null) return false;
        var bb = chon.dat.bounds;
        int lo = chon.dat.sortingOrder, hi = chon.dat.sortingOrder;
        for (int i = 0; i < chon.cay.Length; i++)
        {
            var cr = chon.cay[i];
            if (cr == null) continue;
            if (cr.sortingOrder < lo) lo = cr.sortingOrder;
            if (cr.sortingOrder > hi) hi = cr.sortingOrder;
        }
        layer = chon.dat.sortingLayerID;
        order = p.y > bb.min.y + bb.size.y * 0.2f ? lo - 1 : hi + 1;
        z = chon.dat.transform.position.z;
        return true;
    }

    private FarmerNPC TaoNongDan(string ten)
    {
        var go = new GameObject(ten);
        go.transform.SetParent(transform, false);
        var f = go.AddComponent<FarmerNPC>();
        f.Init(config);
        return f;
    }

    // =====================================================================
    //  Hinh hoc o dat + sorting (dung chung cho moi ong)
    // =====================================================================
    private ThongTinO LayO(PlotController o)
    {
        if (o == null) return null;
        ThongTinO t;
        if (!_o.TryGetValue(o, out t))
        {
            t = new ThongTinO();
            var tf = o.transform.Find("GroundSprite");
            if (tf != null) t.dat = tf.GetComponent<SpriteRenderer>();
            if (t.dat == null)
            {
                float lon = 0f;
                foreach (var sr in o.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sprite == null) continue;
                    float s = sr.bounds.size.x;
                    if (s > lon) { lon = s; t.dat = sr; }
                }
            }
            _o[o] = t;
        }
        if (Time.time >= t.henLamMoiCay)
        {
            t.henLamMoiCay = Time.time + 2f;                 // cay sinh / doi stage luc chay
            var cv = o.GetComponentInChildren<PlotCropVisual>(true);
            var ds = new List<SpriteRenderer>(12);
            if (cv != null && t.dat != null)
                foreach (var sr in cv.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr != t.dat && sr.sortingLayerID == t.dat.sortingLayerID && sr.sortingOrder > t.dat.sortingOrder) ds.Add(sr);
            t.cay = ds.ToArray();
        }
        return t;
    }

    /// <summary>Hinh thoi mat dat cua o: tam c, nua rong hw, nua cao hh (iso 2:1, can theo dinh sprite).</summary>
    public static bool HinhThoi(PlotController o, out Vector3 c, out float hw, out float hh, out Bounds b)
    {
        c = Vector3.zero; hw = hh = 0f; b = new Bounds();
        var t = Instance != null ? Instance.LayO(o) : null;
        if (t == null || t.dat == null) return false;
        b = t.dat.bounds;
        hw = b.extents.x;
        hh = hw * 0.5f;
        c = new Vector3(b.center.x, b.max.y - hh, b.center.z);
        return true;
    }

    public static bool TrongO(PlotController o, Vector3 p)
    {
        Vector3 c; float hw, hh; Bounds b;
        if (!HinhThoi(o, out c, out hw, out hh, out b) || hw < 1f) return false;
        return Mathf.Abs(p.x - c.x) / hw + Mathf.Abs(p.y - c.y) / hh <= 1f;
    }

    /// <summary>
    /// Trong o: order = cay GAN NHAT dung truoc mat ong (cung order, ong lui z 0.5 -> ve truoc, bi cay che);
    /// khong co cay truoc mat -> tren moi cay. Ngoai o: cong thuc do sau giong PlotCropVisual + 12.
    /// </summary>
    public static void TinhSort(PlotController o, Vector3 p, out int layer, out int order, out float z)
    {
        if (_layerCongTrinh == int.MinValue) _layerCongTrinh = SortingLayer.NameToID("CongTrinh");
        var t = Instance != null && o != null ? Instance.LayO(o) : null;
        if (t != null && t.dat != null)
        {
            layer = t.dat.sortingLayerID;
            int truoc = int.MaxValue, sau = t.dat.sortingOrder;
            for (int i = 0; i < t.cay.Length; i++)
            {
                var cr = t.cay[i];
                if (cr == null || !cr.enabled || cr.sprite == null || !cr.gameObject.activeInHierarchy) continue;
                if (cr.transform.position.y < p.y) { if (cr.sortingOrder < truoc) truoc = cr.sortingOrder; }
                else if (cr.sortingOrder > sau) sau = cr.sortingOrder;
            }
            order = truoc != int.MaxValue ? Mathf.Max(truoc, sau + 1) : sau + 1;
            z = t.dat.transform.position.z + 0.5f;
            return;
        }
        layer = _layerCongTrinh;
        int doSau = Mathf.Clamp(Mathf.RoundToInt(-p.y / 37.5f) + 150, 0, 300);
        order = 501 + doSau * 13 + 12;
        z = 0.5f;
    }

    // =====================================================================
    //  Bong mo (vet di cham, diu)
    // =====================================================================
    private struct Bong { public SpriteRenderer sr; public float t, doi, a0; public Vector3 s0; }
    private readonly List<Bong> _bong = new List<Bong>(24);
    private readonly Stack<SpriteRenderer> _khoBong = new Stack<SpriteRenderer>(24);
    private Transform _gocBong;

    public static void NhaBong(SpriteRenderer src)
    {
        if (Instance != null) Instance.NhaBongNoi(src);
    }

    private void NhaBongNoi(SpriteRenderer src)
    {
        if (src == null || src.sprite == null || config == null || _bong.Count >= 24) return;
        if (_gocBong == null)
        {
            var g = new GameObject("Farmer_Ghosts");
            g.transform.SetParent(transform, false);
            _gocBong = g.transform;
        }
        SpriteRenderer sr = _khoBong.Count > 0 ? _khoBong.Pop() : null;
        if (sr == null)
        {
            var go = new GameObject("Ghost");
            go.transform.SetParent(_gocBong, false);
            sr = go.AddComponent<SpriteRenderer>();
        }
        sr.gameObject.SetActive(true);
        sr.sprite = src.sprite;
        sr.flipX = src.flipX;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder - 1;
        var tr = sr.transform;
        tr.position = src.transform.position + new Vector3(0f, 0f, 0.01f);
        tr.rotation = src.transform.rotation;
        Vector3 s0 = src.transform.lossyScale;
        tr.localScale = s0;
        Color m = config.mauBong; m.a = config.alphaBong;
        sr.color = m;
        _bong.Add(new Bong { sr = sr, t = 0f, doi = Mathf.Max(0.05f, config.doiBong), a0 = config.alphaBong, s0 = s0 });
    }

    private void CapNhatBong(float dt)
    {
        for (int i = _bong.Count - 1; i >= 0; i--)
        {
            var b = _bong[i];
            b.t += dt;
            if (b.sr == null) { _bong.RemoveAt(i); continue; }
            float k = b.t / b.doi;
            if (k >= 1f)
            {
                b.sr.gameObject.SetActive(false);
                _khoBong.Push(b.sr);
                _bong.RemoveAt(i);
                continue;
            }
            var c = b.sr.color; c.a = b.a0 * (1f - k) * (1f - k); b.sr.color = c;
            b.sr.transform.localScale = b.s0 * (1f - 0.05f * k);
            _bong[i] = b;
        }
    }
}
