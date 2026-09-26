// ============================================================================
//  FarmerNPC — 1 ONG NONG DAN (2026-09-24)
//  FarmerCrew giao cho ong toi da 4 o dat (hoac tat ca chau hoa). Vong viec:
//    - Hien ra (mo dan len) canh o dat vua gieo.
//    - O con NHO (stage 1-3): di thong tha toi 1 diem trong o -> TUOI NUOC hoac CUOC DAT -> nghi -> o ke.
//    - Het o can lam (cay da stage 4-5 / chin): chi DI QUA DI LAI giua cac o cua minh, dung nghi.
//    - Ong chau hoa: chi tuoi, dung CANH chau, di tu chau nay sang chau khac.
//    - O bi thu hoach het -> ong mo dan roi an (FarmerCrew thu lai de dung sau).
//  Animation tu choi frame (khong Animator), sorting xen dung giua cac cay trong o.
//  Vet mo khi di: bong mo thua (0.28s), song lau (0.6s), mo nhat -> diu nhu video.
//  Toi uu: ngoai man hinh thi khong doi sprite / khong nha bong; sort chi tinh lai khi di chuyen.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FarmerNPC : MonoBehaviour
{
    private enum TT { An, Hien, Di, Tuoi, Cuoc, Nghi, Bien }
    private enum SauKhiDen { Nghi, Tuoi, Cuoc }

    private FarmerConfig _cfg;
    private SpriteRenderer _sr;
    private readonly List<PlotController> _ds = new List<PlotController>(8);
    private bool _chiTuoi;
    private int _ke;
    private TT _tt = TT.An;
    private SauKhiDen _sau;
    private Vector3 _dich;
    private PlotController _oDangLam;
    private float _hen;
    private float _alpha;
    private float _scale = 1f;
    private float _hDon = 1f;                 // chieu cao nhin thay cua sprite walk khi scale = 1
    private readonly List<Vector3> _duong = new List<Vector3>(8);   // diem vong qua chau hoa
    private bool _dichChuyenSauKhiAn, _viecCho;
    private Vector3 _viTriHienLai;
    private bool _vuaTuoi;                    // dang / vua tuoi chau _oDangLam (ve SAU chau do)

    // animation
    private Sprite[] _frames;
    private int _dau, _so, _vongConLai, _frameCu = -1;
    private float _fps, _animT;
    private bool _pingPong;
    private int _huong;                 // 0 DOWN, 1 LEFT, 2 RIGHT, 3 UP (dung hang sheet walk)
    private float _henBong;
    private Vector3 _viTriSortCu = new Vector3(float.NaN, 0f, 0f);
    private float _henSort;

    public bool DangAn => _tt == TT.An;
    public bool ChiTuoi => _chiTuoi;

    // =====================================================================
    public void Init(FarmerConfig cfg)
    {
        _cfg = cfg;
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.color = new Color(1f, 1f, 1f, 0f);
        Sprite dai = cfg != null && cfg.walkFrames != null && cfg.walkFrames.Length > 1 ? cfg.walkFrames[1] : null;
        if (dai != null)
        {
            float h = dai.rect.height / Mathf.Max(1f, dai.pixelsPerUnit) * Mathf.Max(0.05f, cfg.tiLeNhinThay);
            _hDon = Mathf.Max(0.001f, h);
            _scale = cfg.chieuCaoNhinThay / _hDon;
        }
        transform.localScale = new Vector3(_scale, _scale, 1f);
        DungYen();
        _tt = TT.An;
        gameObject.SetActive(false);
    }

    /// <summary>Giao danh sach o (ruong: toi da 4 o; chau: tat ca chau dang trong).</summary>
    public void GiaoViec(List<PlotController> oDat, bool chiTuoi)
    {
        _ds.Clear();
        if (oDat != null) _ds.AddRange(oDat);
        _chiTuoi = chiTuoi;
        if (_ke >= _ds.Count) _ke = 0;
        if (_ds.Count == 0) { ThoiViec(); return; }
        if (_chiTuoi) DatCoTheoChau();

        if (_tt == TT.An)
        {
            _viecCho = false;
            HienTai(ViTriTrongO(_ds[0]));
        }
        else if (_tt == TT.Bien && !_dichChuyenSauKhiAn)
        {
            _tt = TT.Hien;                                   // dang mo di thi hien lai
        }
        else if (_tt == TT.Di && (_oDangLam == null || !_ds.Contains(_oDangLam)))
        {
            ChonViecTiep();                                  // o dang toi khong con la cua minh
        }
    }

    /// <summary>Het viec -> mo dan roi an.</summary>
    public void ThoiViec()
    {
        _ds.Clear();
        _dichChuyenSauKhiAn = false;
        if (_tt == TT.An) return;
        _tt = TT.Bien;
    }

    // =====================================================================
    private void Update()
    {
        if (_cfg == null || _sr == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float tHien = Mathf.Max(0.05f, _cfg.thoiGianHien);

        switch (_tt)
        {
            case TT.Hien:
                _alpha = Mathf.MoveTowards(_alpha, 1f, dt / tHien);
                ApAlpha();
                if (_alpha >= 1f)
                {
                    if (_viecCho) { _viecCho = false; DenNoi(); }
                    else ChonViecTiep();
                }
                break;

            case TT.Bien:
                _alpha = Mathf.MoveTowards(_alpha, 0f, dt / tHien);
                ApAlpha();
                if (_alpha <= 0f)
                {
                    if (_dichChuyenSauKhiAn)
                    {
                        _dichChuyenSauKhiAn = false;
                        _viecCho = true;
                        HienTai(_viTriHienLai);
                    }
                    else
                    {
                        _tt = TT.An;
                        gameObject.SetActive(false);
                    }
                }
                break;

            case TT.Di:
                DiBo(dt);
                break;

            case TT.Tuoi:
            case TT.Cuoc:
                if (ChayAnim(dt))
                {
                    _sr.flipX = false;
                    Nghi(Random.Range(_cfg.nghiGiuaViec.x, _cfg.nghiGiuaViec.y));
                }
                break;

            case TT.Nghi:
                if (Time.time >= _hen) ChonViecTiep();
                break;
        }
        CapNhatSort(false);
    }

    // =====================================================================
    //  Chon viec
    // =====================================================================
    private void ChonViecTiep()
    {
        if (_chiTuoi) DatCoTheoChau();                       // hoa lon dan -> ong cao theo
        for (int i = _ds.Count - 1; i >= 0; i--)
            if (_ds[i] == null || !_ds[i].IsPlanted) _ds.RemoveAt(i);
        if (_ds.Count == 0) { ThoiViec(); return; }
        if (_ke >= _ds.Count) _ke = 0;

        for (int i = 0; i < _ds.Count; i++)
        {
            int k = (_ke + i) % _ds.Count;
            var o = _ds[k];
            if (!CanLam(o)) continue;
            _ke = k;
            DiToi(o, ViTriTrongO(o), ChonHanhDong(o));
            if (Random.value < 0.6f) _ke = (_ke + 1) % _ds.Count;   // 40%: lam them 1 luot o cung o
            return;
        }

        // Cay da lon het: di qua di lai giua cac o cua minh
        var d = _ds[Random.Range(0, _ds.Count)];
        DiToi(d, _chiTuoi ? DiemNghiChau(d) : ViTriTrongO(d), SauKhiDen.Nghi);
    }

    /// <summary>
    /// [2026-09-25 v6] CHO DUNG TUOI CHINH XAC: dong nuoc trong sheet water roi xuong NGANG CHAN ong, cach chan
    /// diemNuocPx.x ve phia binh. Nen chan ong dat NGANG mat dat trong chau (sau chau, chau che chan) va lech ngang
    /// dung diemNuocPx.x -> nuoc roi dung vao dat trong chau. Chi nhan cho nao ca THAN ong (hop chu nhat) khong de
    /// len chau / hoa nao khac -> het "dam len chau". Thu 2 ben, ben ngau nhien truoc.
    /// </summary>
    private bool TimChoTuoi(PlotController o, out Vector3 p)
    {
        p = transform.position;
        Bounds b; Vector3 dat;
        if (_cfg == null || !FarmerCrew.ThongTinChau(o, out b, out dat)) return false;
        var wf = _cfg.waterFrames;
        float ppuW = wf != null && wf.Length > 0 && wf[0] != null ? wf[0].pixelsPerUnit : 111.1f;
        float nx = _cfg.diemNuocPx.x / Mathf.Max(1f, ppuW) * _scale;
        float ny = _cfg.diemNuocPx.y / Mathf.Max(1f, ppuW) * _scale;
        float H = _hDon * _scale, hw = H * 0.2f;
        float yF = dat.y - ny;
        float s0 = Random.value < 0.5f ? -1f : 1f;
        for (int ben = 0; ben < 2; ben++)
        {
            float sb = ben == 0 ? s0 : -s0;
            float x = dat.x + sb * nx;
            var than = new Rect(x - hw, yF + H * 0.04f, hw * 2f, H * 0.9f);
            if (FarmerCrew.ThanDeChau(than, o)) continue;
            p = new Vector3(x, yF, transform.position.z);
            return true;
        }
        return false;
    }

    /// <summary>Cho dung nghi: DUOI ca nhom chau (khong dung giua cac chau).</summary>
    private Vector3 DiemNghiChau(PlotController o)
    {
        Bounds b; Vector3 dat;
        float minY = float.MaxValue, cx = transform.position.x, cao = 60f, rong = 60f;
        for (int i = 0; i < _ds.Count; i++)
            if (FarmerCrew.ThongTinChau(_ds[i], out b, out dat)) { minY = Mathf.Min(minY, b.min.y); cao = b.size.y; rong = b.size.x; }
        if (o != null && FarmerCrew.ThongTinChau(o, out b, out dat)) cx = b.center.x;
        if (minY == float.MaxValue) minY = transform.position.y;
        return new Vector3(cx + Random.Range(-1f, 1f) * rong, minY - cao * Random.Range(0.35f, 0.6f), transform.position.z);
    }

    private bool CanLam(PlotController o)
    {
        if (o == null || !o.IsPlanted) return false;
        if (_chiTuoi) { Vector3 bo; return TimChoTuoi(o, out bo); }
        return o.IsGrowing && !CayDaLon(o);
    }

    /// <summary>Stage 4-5 (bo 5 stage) / stage cuoi (bo 3 stage) / da chin.</summary>
    public static bool CayDaLon(PlotController o)
    {
        if (o == null || o.IsReady) return true;
        var c = o.CurrentCrop;
        if (c == null) return false;
        int n = c.StageCount;
        int st = c.StageFromProgress(o.GetGrowProgress01());
        return st >= Mathf.Max(1, n - 2);
    }

    private SauKhiDen ChonHanhDong(PlotController o)
    {
        if (_chiTuoi) return SauKhiDen.Tuoi;
        int st = 0;
        var c = o.CurrentCrop;
        if (c != null) st = c.StageFromProgress(o.GetGrowProgress01());
        float tiLeCuoc = st == 0 ? 0.5f : 0.3f;
        return Random.value < tiLeCuoc ? SauKhiDen.Cuoc : SauKhiDen.Tuoi;
    }

    /// <summary>Ong tuoi chau: cao it nhat tiLeCaoSoVoiChau x chieu cao chau (khong thap hon ong ruong).</summary>
    private void DatCoTheoChau()
    {
        // [2026-09-25 v2] Cao hon CA chau + hoa: tong = (mat dat trong chau - day chau) + hoa no to nhat (0.9 x chau),
        // hoac dinh hoa that dang hien neu cao hon.
        // [2026-09-25 v3] Theo THAN CHAU: cao = tiLeCaoSoVoiChau x chau (mac dinh 3.4) -> voi binh o tren mieng chau.
        // Van luon cao hon chau + hoa (x1.3) va khong thap hon ong ruong.
        float cao = _cfg.chieuCaoNhinThay;
        float chauMax = 0f, tongMax = 0f;
        for (int i = 0; i < _ds.Count; i++)
        {
            Bounds b; Vector3 d;
            if (!FarmerCrew.ThongTinChau(_ds[i], out b, out d)) continue;
            chauMax = Mathf.Max(chauMax, b.size.y);
            float tong = b.size.y * 1.9f;
            float dinh = FarmerCrew.DinhCay(_ds[i]);
            if (dinh > float.MinValue) tong = Mathf.Max(tong, dinh - b.min.y);
            tongMax = Mathf.Max(tongMax, tong);
        }
        // [2026-09-25 v5] Sep: "giam size". Ong chau cao = tiLeCaoSoVoiChau x than chau (1.9 ~ ngang chau + hoa),
        // KHONG ep cao hon ong ruong / hoa nua. Nuoc roi dung chau nho NEO (diemNuocPx), khong nho do cao.
        if (chauMax > 0f) cao = chauMax * Mathf.Max(0.8f, _cfg.tiLeCaoSoVoiChau);
        _scale = cao / _hDon;
        if (_tt != TT.Hien && _tt != TT.Bien) transform.localScale = new Vector3(_scale, _scale, 1f);
    }

    /// <summary>Khoang cach ngang tu chan ong toi cho nuoc roi (world), theo scale hien tai.</summary>
    private float TamTuoi()
    {
        var w = _cfg.waterFrames;
        float ppu = w != null && w.Length > 0 && w[0] != null ? w[0].pixelsPerUnit : 111.1f;
        return _cfg.tamChauPxNuoc / Mathf.Max(1f, ppu) * _scale;
    }

    private static bool TrongVatCan(Vector3 p, List<Vector4> vc)
    {
        if (vc == null) return false;
        for (int i = 0; i < vc.Count; i++)
        {
            var e = vc[i];
            float dx = (p.x - e.x) / e.z, dy = (p.y - e.y) / e.w;
            if (dx * dx + dy * dy < 1f) return true;
        }
        return false;
    }

    /// <summary>Than ong (rong 2*nuaThan, chan o p) co de len chau nao KHAC chau dang tuoi (tamX) khong.</summary>
    private static bool DeChauKhac(Vector3 p, List<Vector4> vc, float nuaThan, float tamX)
    {
        if (vc == null) return false;
        for (int i = 0; i < vc.Count; i++)
        {
            var e = vc[i];
            if (Mathf.Abs(e.x - tamX) < 2f) continue;                         // chinh chau dang tuoi
            if (Mathf.Abs(p.x - e.x) < e.z + nuaThan && Mathf.Abs(p.y - e.y) < e.w * 3f) return true;
        }
        return false;
    }

    private Vector3 ViTriTrongO(PlotController o)
    {
        Vector3 c; float hw, hh; Bounds b;
        if (_chiTuoi)
        {
            // [v3] Chau hoa: dung CANH chau, chan hoi SAU day chau (ve sau chau) -> binh tren mieng chau,
            // chau che doan nuoc phia duoi mieng -> nhin nhu nuoc roi vao dat trong chau, khong chay xuong day chau.
            Vector3 dat;
            if (FarmerCrew.ThongTinChau(o, out b, out dat))
            {
                // [v6 NEO] Chi dung o cho THAN ong khong de len chau nao khac; khong co cho -> ra ngoai nghi.
                Vector3 pt;
                if (TimChoTuoi(o, out pt)) return pt;
                return DiemNghiChau(o);
            }
        }
        if (!FarmerCrew.HinhThoi(o, out c, out hw, out hh, out b))
            return o != null ? o.transform.position : transform.position;
        if (_chiTuoi)
        {
            float s = Random.value < 0.5f ? -1f : 1f;
            return new Vector3(b.center.x + s * (b.extents.x + 18f), b.min.y + b.size.y * 0.12f, transform.position.z);
        }
        float k = _cfg.vungDung;
        float u = Random.Range(-1f, 1f), v = Random.Range(-1f, 1f);
        return new Vector3(c.x + (u - v) * hw * 0.5f * k, c.y + (u + v) * hh * 0.5f * k, transform.position.z);
    }

    private void DiToi(PlotController o, Vector3 p, SauKhiDen sau)
    {
        _oDangLam = o;
        _vuaTuoi = false;
        _dich = p;
        _sau = sau;
        _duong.Clear();
        Vector3 d = p - transform.position; d.z = 0f;
        if (d.magnitude > _cfg.xaQuaThiHienLai)
        {
            _dichChuyenSauKhiAn = true;                     // qua xa: mo di, hien ra o cho moi
            _viTriHienLai = p;
            _tt = TT.Bien;
            return;
        }
        // Di vong qua chau hoa, khong dap len chau
        var vc = FarmerCrew.VatCan();
        if (vc != null && vc.Count > 0)
        {
            TimDuong(transform.position, p, vc, 0);
            if (_duong.Count > 0) { _dich = _duong[0]; _duong.RemoveAt(0); }
        }
        _tt = TT.Di;
    }

    /// <summary>Doan a->b cat elip chan chau nao thi chen 1 diem vong ra canh elip do (toi da 4 tang).</summary>
    private void TimDuong(Vector3 a, Vector3 b, List<Vector4> vc, int tang)
    {
        int cat = -1; float tCat = float.MaxValue;
        if (tang < 4)
            for (int i = 0; i < vc.Count; i++)
            {
                var e = vc[i];
                // doi sang khong gian tron: y nhan rx/ry
                float k = e.z / Mathf.Max(0.01f, e.w);
                Vector2 A = new Vector2(a.x - e.x, (a.y - e.y) * k), B = new Vector2(b.x - e.x, (b.y - e.y) * k);
                float r = e.z;
                if (A.sqrMagnitude <= r * r || B.sqrMagnitude <= r * r) continue;   // dau / cuoi da sat chau
                Vector2 AB = B - A;
                float L2 = AB.sqrMagnitude;
                if (L2 < 1f) continue;
                float t = Mathf.Clamp01(-Vector2.Dot(A, AB) / L2);
                if ((A + AB * t).sqrMagnitude < r * r && t < tCat) { tCat = t; cat = i; }
            }
        if (cat < 0) { _duong.Add(new Vector3(b.x, b.y, a.z)); return; }

        var ce = vc[cat];
        float kk = ce.z / Mathf.Max(0.01f, ce.w);
        Vector2 A2 = new Vector2(a.x - ce.x, (a.y - ce.y) * kk), B2 = new Vector2(b.x - ce.x, (b.y - ce.y) * kk);
        Vector2 dir = (B2 - A2).normalized;
        Vector2 n = new Vector2(-dir.y, dir.x);
        Vector2 giua = (A2 + B2) * 0.5f;
        if (Vector2.Dot(giua, n) < 0f) n = -n;                     // vong ve phia gan duong thang hon
        Vector2 w = n * ce.z * 1.35f;
        Vector3 vong = new Vector3(ce.x + w.x, ce.y + w.y / kk, a.z);
        TimDuong(a, vong, vc, tang + 1);
        TimDuong(vong, b, vc, tang + 1);
    }

    private void DenNoi()
    {
        switch (_sau)
        {
            case SauKhiDen.Tuoi:
                _vuaTuoi = true;
                if (_cfg.waterFrames == null || _cfg.waterFrames.Length == 0) goto default;
                _tt = TT.Tuoi;
                HuongVeO();
                DatAnim(_cfg.waterFrames, 0, _cfg.waterFrames.Length, _cfg.waterFps, false, Mathf.Max(1, _cfg.soVongTuoi));
                break;
            case SauKhiDen.Cuoc:
                if (_cfg.hoeFrames == null || _cfg.hoeFrames.Length == 0) goto default;
                _tt = TT.Cuoc;
                HuongVeO();
                DatAnim(_cfg.hoeFrames, 0, _cfg.hoeFrames.Length, _cfg.hoeFps, false, Mathf.Max(1, _cfg.soVongCuoc));
                break;
            default:
                Nghi(Random.Range(_cfg.nghiKhiDiDao.x, _cfg.nghiKhiDiDao.y));
                break;
        }
        CapNhatSort(true);                                   // [v3] toi noi: tinh lai lop ve ngay (ong chau -> sau chau)
    }

    /// <summary>Sheet cuoc/tuoi: dung cu o phia PHAI anh. O nam ben trai -> lat ngang.</summary>
    private void HuongVeO()
    {
        Vector3 c; float hw, hh; Bounds b; Vector3 dat;
        float dx = FarmerCrew.HinhThoi(_oDangLam, out c, out hw, out hh, out b) ? c.x - transform.position.x : 0f;
        if (_chiTuoi && FarmerCrew.ThongTinChau(_oDangLam, out b, out dat)) dx = b.center.x - transform.position.x;   // quay binh ve tam chau
        _sr.flipX = Mathf.Abs(dx) < 20f ? Random.value < 0.5f : dx < 0f;
    }

    private void Nghi(float giay)
    {
        _tt = TT.Nghi;
        _hen = Time.time + giay;
        _sr.flipX = false;
        if (!_chiTuoi && Random.value < 0.5f) _huong = 0;   // thinh thoang quay mat ra ngoai
        DungYen();
    }

    // =====================================================================
    //  Di chuyen
    // =====================================================================
    private void HienTai(Vector3 p)
    {
        gameObject.SetActive(true);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
        _alpha = 0f;
        _huong = 0;
        _sr.flipX = false;
        DungYen();
        ApAlpha();
        _tt = TT.Hien;
        CapNhatSort(true);
    }

    private void DiBo(float dt)
    {
        Vector3 pos = transform.position;
        Vector3 d = _dich - pos; d.z = 0f;
        float dist = d.magnitude;
        float buoc = Mathf.Max(1f, _cfg.tocDoDi) * dt;
        if (dist <= buoc)
        {
            transform.position = new Vector3(_dich.x, _dich.y, pos.z);
            if (_duong.Count > 0) { _dich = _duong[0]; _duong.RemoveAt(0); return; }   // diem vong ke tiep
            DenNoi();
            return;
        }
        Vector3 dir = d / dist;
        transform.position = pos + dir * buoc;

        int h = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) * 1.2f ? (dir.x < 0f ? 1 : 2) : (dir.y < 0f ? 0 : 3);
        bool doiHuong = h != _huong || _frames != _cfg.walkFrames || _vongConLai != 0;
        _huong = h;
        _sr.flipX = false;
        if (doiHuong) DatAnim(_cfg.walkFrames, _huong * 3, 3, _cfg.walkFps, true, 0, true);
        ChayAnim(dt);

        if (_cfg.vetMo && Time.time >= _henBong)
        {
            _henBong = Time.time + Mathf.Max(0.05f, _cfg.nhipBong);
            if (_sr.isVisible && _alpha > 0.9f) FarmerCrew.NhaBong(_sr);
        }
    }

    // =====================================================================
    //  Animation
    // =====================================================================
    private void DungYen()
    {
        _frames = null;
        var w = _cfg != null ? _cfg.walkFrames : null;
        if (w != null && w.Length >= 12) _sr.sprite = w[_huong * 3 + 1];
        else if (w != null && w.Length > 0) _sr.sprite = w[0];
        _frameCu = -1;
    }

    private void DatAnim(Sprite[] frames, int dau, int so, float fps, bool pingPong, int vong, bool giuThoiGian = false)
    {
        _frames = frames;
        _dau = Mathf.Max(0, dau);
        _so = frames == null ? 0 : Mathf.Clamp(so, 0, frames.Length - _dau);
        _fps = Mathf.Max(0.5f, fps);
        _pingPong = pingPong;
        _vongConLai = vong;
        if (!giuThoiGian) _animT = 0f;
        _frameCu = -1;
    }

    /// <summary>true = da chay xong so vong (anim khong lap).</summary>
    private bool ChayAnim(float dt)
    {
        if (_frames == null || _so <= 0) return true;
        _animT += dt;
        int dai = _pingPong && _so > 2 ? _so * 2 - 2 : _so;
        int buoc = Mathf.FloorToInt(_animT * _fps);
        if (_vongConLai > 0 && buoc >= dai * _vongConLai) return true;
        int i = buoc % dai;
        if (_pingPong && i >= _so) i = dai - i;
        if (i != _frameCu && _sr.isVisible)
        {
            _frameCu = i;
            var sp = _frames[_dau + i];
            if (sp != null) _sr.sprite = sp;
        }
        return false;
    }

    private void ApAlpha()
    {
        var c = _sr.color; c.a = _alpha; _sr.color = c;
        float k = _scale * (0.88f + 0.12f * _alpha);          // hien ra nay nhe
        transform.localScale = new Vector3(k, k, 1f);
    }

    // =====================================================================
    //  Sorting: xen DUNG giua cac cay trong o (cay truoc mat che ong, cay sau lung bi ong che)
    // =====================================================================
    private void CapNhatSort(bool ep)
    {
        Vector3 p = transform.position;
        if (!ep)
        {
            if (Time.time < _henSort) return;
            if (!float.IsNaN(_viTriSortCu.x) && (p - _viTriSortCu).sqrMagnitude < 4f) return;
        }
        _henSort = Time.time + 0.12f;
        _viTriSortCu = p;

        PlotController o = null;
        if (_oDangLam != null && FarmerCrew.TrongO(_oDangLam, p)) o = _oDangLam;
        else
            for (int i = 0; i < _ds.Count; i++)
                if (_ds[i] != null && FarmerCrew.TrongO(_ds[i], p)) { o = _ds[i]; break; }

        int layer, order; float z;
        // [v4] Ong chau: dung / tuoi canh chau -> ve TREN chau dang tuoi (Sep chot: sorting cao hon chau)
        if (_chiTuoi && _vuaTuoi && _tt != TT.Di && _oDangLam != null && FarmerCrew.TinhSortTruocChau(_oDangLam, out layer, out order, out z, !_cfg.veTruocChauKhiTuoi))
        {
            _sr.sortingLayerID = layer;
            _sr.sortingOrder = order;
            if (Mathf.Abs(p.z - z) > 0.001f) transform.position = new Vector3(p.x, p.y, z);
            return;
        }
        if (o != null && o.Category == PlotCategory.Flower) o = null;          // chau hoa: sort theo chan chau
        if (o != null || !FarmerCrew.TinhSortChau(p, out layer, out order, out z, false))
            FarmerCrew.TinhSort(o, p, out layer, out order, out z);
        _sr.sortingLayerID = layer;
        _sr.sortingOrder = order;
        if (Mathf.Abs(p.z - z) > 0.001f) transform.position = new Vector3(p.x, p.y, z);
    }
}
