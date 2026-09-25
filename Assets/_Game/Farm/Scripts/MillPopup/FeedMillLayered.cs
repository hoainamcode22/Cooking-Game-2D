// ============================================================================
//  MAY XAY THUC AN GIA SUC - BAN NHIEU LOP (2026-09-25)
//  Nha (base) DUNG YEN tuyet doi. Chi cac bo phan chay tren nen:
//    - FM_Gears : 8 frame banh rang (quay nhanh khi dang xay, cham khi ranh)
//    - FM_Chute : 6 frame hat chay tren mang (chi hien khi dang xay)
//    - FM_LampGlow : quang sang den nhap nhay nhe
//    - FM_Smoke : ParticleSystem khoi ong khoi (day khi xay, thua khi ranh)
//    - FM_Crates : thung go hien ra duoi mang, day hat, truot ra xep hang (khi dang xay)
//  "Dang xay" doc tu save cua popup may xay (MILL_S{i}_EndTicks) moi 1 giay, khong sua MillPopupUI.
//  Moi object con nam trong Hierarchy, Sep keo / doi so trong Inspector tuy y.
//  Edit mode hien dung nhu luc Play (frame dau, den sang).
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class FeedMillLayered : MonoBehaviour
{
    [Header("Lop bo phan (tool tu gan)")]
    public SpriteRenderer gears;
    public SpriteRenderer chute;
    public SpriteRenderer lampGlow;
    public ParticleSystem smoke;
    public Transform crateRoot;

    [Header("Frame")]
    public Sprite[] gearFrames;
    public Sprite[] chuteFrames;
    public Sprite crateEmpty;
    public Sprite crateFull;

    [Header("Toc do")]
    [Tooltip("Frame/giay cua banh rang khi dang xay")] public float gearFps = 12f;
    [Tooltip("He so toc do banh rang khi may ranh (0 = dung im)")] [Range(0f, 1f)] public float gearRanh = 0.3f;
    [Tooltip("Frame/giay cua dong hat")] public float chuteFps = 12f;

    [Header("Den")]
    [Range(0f, 1f)] public float denMin = 0.55f;
    [Range(0f, 1f)] public float denMax = 0.95f;
    public float denChuKy = 2.4f;

    [Header("Khoi (hat / giay)")]
    public float khoiKhiXay = 1.8f;
    public float khoiKhiRanh = 0.45f;

    [Header("Thung hang (don vi local cua may)")]
    [Tooltip("Cho thung dung hung hat, ngay duoi dau mang")] public Vector2 choHungHat = new Vector2(1.03f, 0.40f);
    [Tooltip("Moi lan truot ra, thung di them 1 buoc (huong dong-nam isometric)")] public Vector2 buocTruot = new Vector2(0.28f, -0.14f);
    [Min(1)] public int soThungToiDa = 3;
    [Tooltip("So thung day xep san luc vao game")] [Min(0)] public int soThungBanDau = 2;
    public float thoiGianDay = 2.6f;
    public float thoiGianTruot = 0.55f;
    [Tooltip("Bat de xem may chay lien tuc (khong can dang xay that)")] public bool xemThuLuonChay = false;

    [HideInInspector] public Sprite spriteCu;          // de muc "Tra lai may cu" dung
    [HideInInspector] public bool animatorCuBat = true;

    // ── runtime ──
    private bool _dangXay;
    private bool _daApDung;
    private float _hetGioKiemTra;
    private float _tGear, _tChute;
    private readonly List<SpriteRenderer> _thung = new List<SpriteRenderer>();
    private SpriteRenderer _thungDangDay;
    private float _tDay;
    private bool _dangTruot;
    private float _tTruot;
    private readonly List<Vector3> _tuViTri = new List<Vector3>();
    private int _orderGoc;

    private void OnEnable()
    {
        _hetGioKiemTra = 0f;
        _daApDung = false;
        HienTinh();
        if (!Application.isPlaying) return;
        var sr = GetComponent<SpriteRenderer>();
        _orderGoc = sr != null ? sr.sortingOrder : 0;
        TaoThungBanDau();
    }

    /// <summary>Trang thai tinh (Edit mode / luc moi bat): frame dau, den sang vua.</summary>
    private void HienTinh()
    {
        if (gears != null && gearFrames != null && gearFrames.Length > 0) gears.sprite = gearFrames[0];
        if (chute != null && chuteFrames != null && chuteFrames.Length > 0) chute.sprite = chuteFrames[0];
        if (chute != null) chute.enabled = xemThuLuonChay;
        if (lampGlow != null) DatAlpha(lampGlow, (denMin + denMax) * 0.5f);
    }

    private void Update()
    {
        if (!Application.isPlaying) { HienTinh(); return; }

        float dt = Time.deltaTime;
        if (Time.unscaledTime >= _hetGioKiemTra)
        {
            _hetGioKiemTra = Time.unscaledTime + 1f;
            bool moi = xemThuLuonChay || DocDangXay();
            if (moi != _dangXay || !_daApDung) { _dangXay = moi; _daApDung = true; DoiTrangThai(); }
        }

        // Banh rang
        if (gears != null && gearFrames != null && gearFrames.Length > 1)
        {
            _tGear += dt * gearFps * (_dangXay ? 1f : gearRanh);
            gears.sprite = gearFrames[(int)_tGear % gearFrames.Length];
        }
        // Dong hat
        if (chute != null && chute.enabled && chuteFrames != null && chuteFrames.Length > 0)
        {
            _tChute += dt * chuteFps;
            chute.sprite = chuteFrames[(int)_tChute % chuteFrames.Length];
        }
        // Den
        if (lampGlow != null)
        {
            float s = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.2f, denChuKy));
            float hi = _dangXay ? denMax : Mathf.Lerp(denMin, denMax, 0.5f);
            DatAlpha(lampGlow, Mathf.Lerp(denMin, hi, s));
        }
        CapNhatThung(dt);
    }

    private void DoiTrangThai()
    {
        if (chute != null) chute.enabled = _dangXay;
        if (smoke != null)
        {
            var em = smoke.emission;
            em.rateOverTime = _dangXay ? khoiKhiXay : khoiKhiRanh;
            if (!smoke.isPlaying) smoke.Play();
        }
    }

    // ─────────────────────────── DANG XAY? ───────────────────────────
    private static bool DocDangXay()
    {
        long now = System.DateTime.UtcNow.Ticks;
        for (int i = 0; i < 8; i++)
        {
            string k = "MILL_S" + i + "_EndTicks";
            if (!PlayerPrefs.HasKey(k)) continue;
            if (long.TryParse(PlayerPrefs.GetString(k, "0"), out long end) && end > now) return true;
        }
        return false;
    }

    // ─────────────────────────── THUNG HANG ───────────────────────────
    private Vector3 ViTriO(int buoc) => new Vector3(choHungHat.x + buocTruot.x * buoc, choHungHat.y + buocTruot.y * buoc, 0f);

    private SpriteRenderer TaoThung(Sprite sp)
    {
        if (crateRoot == null || sp == null) return null;
        var go = new GameObject("Thung");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(crateRoot, false);
        var sr = go.AddComponent<SpriteRenderer>();
        var goc = GetComponent<SpriteRenderer>();
        if (goc != null) { sr.sharedMaterial = goc.sharedMaterial; sr.sortingLayerID = goc.sortingLayerID; }
        sr.sprite = sp;
        return sr;
    }

    private void DatOrderThung()
    {
        // Thung cang xa may (truot ve phia nam) cang o phia truoc
        for (int i = 0; i < _thung.Count; i++) if (_thung[i] != null) _thung[i].sortingOrder = _orderGoc + 7 + i;
        if (_thungDangDay != null) _thungDangDay.sortingOrder = _orderGoc + 6;
    }

    private void TaoThungBanDau()
    {
        foreach (var t in _thung) if (t != null) Destroy(t.gameObject);
        _thung.Clear();
        if (_thungDangDay != null) Destroy(_thungDangDay.gameObject);
        _thungDangDay = null; _dangTruot = false;
        int n = Mathf.Min(soThungBanDau, soThungToiDa);
        for (int i = n; i >= 1; i--)          // _thung[0] = gan may nhat
        {
            var sr = TaoThung(crateFull);
            if (sr == null) return;
            sr.transform.localPosition = ViTriO(i);
            _thung.Insert(0, sr);
        }
        DatOrderThung();
    }

    private void CapNhatThung(float dt)
    {
        if (crateRoot == null) return;

        if (_dangTruot)
        {
            _tTruot += dt / Mathf.Max(0.05f, thoiGianTruot);
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_tTruot));
            for (int i = 0; i < _thung.Count; i++)
            {
                if (_thung[i] == null) continue;
                _thung[i].transform.localPosition = Vector3.Lerp(_tuViTri[i], ViTriO(i + 1), k);
                if (i >= soThungToiDa) DatAlpha(_thung[i], 1f - k);   // thung thua mo dan
            }
            if (_tTruot >= 1f)
            {
                _dangTruot = false;
                while (_thung.Count > soThungToiDa)
                {
                    var cu = _thung[_thung.Count - 1];
                    _thung.RemoveAt(_thung.Count - 1);
                    if (cu != null) Destroy(cu.gameObject);
                }
            }
            return;
        }

        if (_thungDangDay == null)
        {
            if (!_dangXay) return;
            _thungDangDay = TaoThung(crateEmpty);
            if (_thungDangDay == null) return;
            _thungDangDay.transform.localPosition = ViTriO(0);
            _thungDangDay.transform.localScale = Vector3.zero;
            _tDay = 0f;
            DatOrderThung();
        }

        _tDay += dt;
        float pop = Mathf.Clamp01(_tDay / 0.2f);
        float s = pop < 1f ? Mathf.SmoothStep(0f, 1.08f, pop) : 1f;
        if (_tDay > thoiGianDay && _thungDangDay.sprite != crateFull) _thungDangDay.sprite = crateFull;
        if (_tDay > thoiGianDay && _tDay < thoiGianDay + 0.16f)
            s = 1f + 0.1f * Mathf.Sin((_tDay - thoiGianDay) / 0.16f * Mathf.PI);     // nay nhe khi day
        _thungDangDay.transform.localScale = new Vector3(s, s, 1f);

        if (_tDay >= thoiGianDay + 0.35f)
        {
            // Bat dau truot: thung vua day vao dau hang, ca hang di them 1 buoc
            _thungDangDay.transform.localScale = Vector3.one;
            _thung.Insert(0, _thungDangDay);
            _thungDangDay = null;
            _tuViTri.Clear();
            for (int i = 0; i < _thung.Count; i++)
                _tuViTri.Add(_thung[i] != null ? _thung[i].transform.localPosition : ViTriO(i));
            _tuViTri[0] = ViTriO(0);
            _tTruot = 0f; _dangTruot = true;
            DatOrderThung();
        }
    }

    private static void DatAlpha(SpriteRenderer r, float a)
    {
        var c = r.color; c.a = a; r.color = c;
    }
}
