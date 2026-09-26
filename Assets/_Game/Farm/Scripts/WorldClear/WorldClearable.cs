// ============================================================================
//  WorldClearable — 1 vat the tren map chat / cat / dap duoc (cay, bui, da) (2026-09-25)
//  WorldClearManager tu gan luc vao scene (quet theo ten), KHONG sua prefab / scene.
//  Trang thai luu PlayerPrefs:
//    WCLR_W_<id> = moc UNIX xong (dang lam, tat game van chay tiep)
//    WCLR_T_<id> = tong giay cua lan lam do (ve thanh tien trinh)
//    WCLR_D_<id> = 1 (da don xong -> lan sau vao game an luon)
//  Luc dang lam: dung cu vung nhip (code, khong art moi) + thanh tien trinh + dem giay tren dau vat.
//    Riu : lao vao tu BEN TRAI, vung bua bo vao chan cay, vun go + la bay, cay rung.
//    Keo : lia doc ngon bui, cat tach tach, la + co bay, bui thap dan.
//    Bua : giang tu tren BEN PHAI xuong, dap dinh da, manh da + tia lua + bui, da nhun.
//  Xong: cay do nghieng roi mo (quay quanh goc), bui xep lai, da vo -> bung bui lon -> an.
// ============================================================================
using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldClearable : MonoBehaviour
{
    public WorldClearKind kind;
    public string id;

    private enum TT { Nghi, Lam, Xong }
    private TT _tt = TT.Nghi;

    private SpriteRenderer[] _rs = new SpriteRenderer[0];
    private Bounds _b;
    private Vector3 _posGoc, _scaleGoc;
    private Quaternion _rotGoc;
    private long _finish;
    private int _tong = 1;

    // FX dang lam
    private GameObject _fx;
    private SpriteRenderer _barNen, _barFill, _tool;
    private Transform _pivot;
    private TextMeshPro _txt;
    private float _tAnim, _tVao, _rung, _pha;
    private bool _daDanh;
    private float _barW, _barH;
    private int _layer, _orderMax;
    private string _chuCu;

    public bool DangLam => _tt == TT.Lam;
    public bool DaXong => _tt == TT.Xong;
    public Bounds Bien => _b;
    public float K => Mathf.Clamp(Mathf.Max(_b.size.y, _b.size.x * 0.8f) / 100f, 0.5f, 4f);
    public int GiayConLai => _tt != TT.Lam ? 0 : Mathf.Max(0, (int)(_finish - WorldClearManager.NowUnix()));
    public float TienDo01 => _tt != TT.Lam ? (_tt == TT.Xong ? 1f : 0f) : Mathf.Clamp01(1f - GiayConLai / (float)Mathf.Max(1, _tong));
    public Vector3 DiemChan => new Vector3(_b.center.x, _b.min.y, _posGoc.z);

    // =====================================================================
    public void Init(WorldClearKind k, string maId, SpriteRenderer[] rs)
    {
        kind = k;
        id = maId;
        _rs = rs ?? new SpriteRenderer[0];
        _posGoc = transform.position;
        _rotGoc = transform.rotation;
        _scaleGoc = transform.localScale;
        TinhBien();
        enabled = false;
    }

    private void TinhBien()
    {
        bool co = false;
        _layer = 0; _orderMax = int.MinValue;
        int layerVal = int.MinValue;
        for (int i = 0; i < _rs.Length; i++)
        {
            var r = _rs[i];
            if (r == null || r.sprite == null) continue;
            if (!co) { _b = r.bounds; co = true; } else _b.Encapsulate(r.bounds);
            int lv = SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            if (lv > layerVal || (lv == layerVal && r.sortingOrder > _orderMax)) { layerVal = lv; _layer = r.sortingLayerID; _orderMax = r.sortingOrder; }
        }
        if (!co) _b = new Bounds(transform.position, Vector3.one * 50f);
        if (_orderMax == int.MinValue) _orderMax = 0;
    }

    /// <summary>Uu tien khi 2 vat chong nhau: lop ve cao hon thang.</summary>
    public long UuTien => SortingLayer.GetLayerValueFromID(_layer) * 100000L + _orderMax;

    /// <summary>Diem world nam TREN hinh that cua vat (theo luoi tam giac sprite, khong phai hop vuong).</summary>
    public bool ChuaDiem(Vector2 w)
    {
        if (_tt == TT.Xong || !gameObject.activeInHierarchy) return false;
        if (!_b.Contains(new Vector3(w.x, w.y, _b.center.z))) return false;
        for (int i = 0; i < _rs.Length; i++)
        {
            var r = _rs[i];
            if (r == null || !r.enabled || r.sprite == null) continue;
            if (!r.bounds.Contains(new Vector3(w.x, w.y, r.bounds.center.z))) continue;
            Vector3 l = r.transform.InverseTransformPoint(new Vector3(w.x, w.y, r.transform.position.z));
            if (r.flipX) l.x = -l.x;
            if (r.flipY) l.y = -l.y;
            var v = r.sprite.vertices;
            var tri = r.sprite.triangles;
            for (int t = 0; t + 2 < tri.Length; t += 3)
                if (TrongTamGiac(l, v[tri[t]], v[tri[t + 1]], v[tri[t + 2]])) return true;
        }
        return false;
    }

    private static bool TrongTamGiac(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool am = d1 < 0 || d2 < 0 || d3 < 0, duong = d1 > 0 || d2 > 0 || d3 > 0;
        return !(am && duong);
    }

    /// <summary>Nhay nhe khi duoc chon / keo dung cu qua (goi moi lan).</summary>
    public void NhunChon() { _rung = Mathf.Max(_rung, 0.6f); if (!enabled) StartCoroutine(RungNhanh()); }

    private IEnumerator RungNhanh()
    {
        float t = 0f;
        while (t < 0.3f && _tt == TT.Nghi)
        {
            t += Time.deltaTime;
            float a = Mathf.Sin(t * 50f) * (1f - t / 0.3f) * _b.size.x * 0.015f;
            transform.position = _posGoc + new Vector3(a, 0f, 0f);
            yield return null;
        }
        if (_tt == TT.Nghi) transform.position = _posGoc;
    }

    // =====================================================================
    //  Bat dau / khoi phuc / xong ngay
    // =====================================================================
    public void BatDau(int giay)
    {
        _tong = Mathf.Max(1, giay);
        _finish = WorldClearManager.NowUnix() + _tong;
        PlayerPrefs.SetString("WCLR_W_" + id, _finish.ToString());
        PlayerPrefs.SetInt("WCLR_T_" + id, _tong);
        WorldClearManager.GhiNhoId(id);
        PlayerPrefs.Save();
        VaoLam(true);
    }

    public void KhoiPhuc(long finish, int tong)
    {
        _finish = finish;
        _tong = Mathf.Max(1, tong);
        if (_finish <= WorldClearManager.NowUnix()) { _tt = TT.Lam; HoanThanh(false); return; }
        VaoLam(false);
    }

    public void XongNgay()
    {
        if (_tt != TT.Lam) return;
        _finish = WorldClearManager.NowUnix();
    }

    private void VaoLam(bool coIntro)
    {
        _tt = TT.Lam;
        _tAnim = 0f;
        _tVao = coIntro ? 0f : 1f;
        _daDanh = false;
        _pha = Random.value;
        DungFx();
        enabled = true;
    }

    // =====================================================================
    private void Update()
    {
        if (_tt != TT.Lam) { enabled = false; return; }
        float dt = Time.deltaTime;
        _tAnim += dt;
        _tVao = Mathf.Min(1f, _tVao + dt / 0.45f);

        CapNhatThanh();
        switch (kind)
        {
            case WorldClearKind.Riu: AnimRiu(); break;
            case WorldClearKind.Keo: AnimKeo(); break;
            case WorldClearKind.Bua: AnimBua(); break;
        }

        // rung vat khi trung don
        _rung = Mathf.MoveTowards(_rung, 0f, dt * 3.5f);
        float a = Mathf.Sin(Time.time * 55f) * _rung * _b.size.x * 0.02f;
        transform.position = _posGoc + new Vector3(a, 0f, 0f);
        if (kind == WorldClearKind.Keo)                                           // bui thap dan theo tien do
            transform.localScale = new Vector3(_scaleGoc.x, _scaleGoc.y * Mathf.Lerp(1f, 0.82f, TienDo01), _scaleGoc.z);
        else if (kind == WorldClearKind.Bua)                                      // da nhun khi bi dap
            transform.localScale = new Vector3(_scaleGoc.x * (1f + _rung * 0.05f), _scaleGoc.y * (1f - _rung * 0.07f), _scaleGoc.z);

        if (GiayConLai <= 0) HoanThanh(true);
    }

    // =====================================================================
    //  FX dang lam: dung cu + thanh tien trinh
    // =====================================================================
    private void DungFx()
    {
        HuyFx();
        var cfg = WorldClearConfig.Load();
        _fx = new GameObject("WorkFX_" + id);
        _fx.transform.SetParent(WorldClearManager.GocFx, false);

        float k = K;
        int oThanh = Mathf.Clamp(_orderMax + 300, -32000, 32000);

        // Thanh tien trinh tren dau vat
        _barW = Mathf.Clamp(_b.size.x * 0.75f, 120f, 200f);
        _barH = 24f;
        Vector3 goc = new Vector3(_b.center.x, _b.max.y + 22f, _posGoc.z);
        _barNen = TaoSr("ThanhNen", goc, WorldClearFX.BoGocTrang(64, 30, 30f / 14f), new Color(0.22f, 0.14f, 0.08f, 0.88f), oThanh);
        _barNen.drawMode = SpriteDrawMode.Sliced;
        _barNen.size = new Vector2(_barW + 8f, _barH + 8f);
        _barFill = TaoSr("ThanhDay", goc, WorldClearFX.BoGocTrang(64, 30, 30f / 12f), new Color(0.50f, 0.85f, 0.32f, 1f), oThanh + 1);
        _barFill.drawMode = SpriteDrawMode.Sliced;
        _barFill.size = new Vector2(_barW, _barH);

        var tgo = new GameObject("Gio");
        tgo.transform.SetParent(_fx.transform, false);
        tgo.transform.position = goc + new Vector3(0f, 0f, -0.01f);
        _txt = tgo.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null) _txt.font = TMP_Settings.defaultFontAsset;
        _txt.fontSize = 180f;                                  // TMP 3D: fontSize/10 = chieu cao chu (unit) -> ~18
        _txt.fontStyle = FontStyles.Bold;
        _txt.alignment = TextAlignmentOptions.Center;
        _txt.color = Color.white;
        _txt.textWrappingMode = TextWrappingModes.NoWrap;
        _txt.overflowMode = TextOverflowModes.Overflow;
        _txt.rectTransform.sizeDelta = new Vector2(_barW, _barH);
        var mr = _txt.GetComponent<MeshRenderer>();
        if (mr != null) { mr.sortingLayerID = _layer; mr.sortingOrder = oThanh + 2; }
        _chuCu = null;

        // Dung cu
        var pv = new GameObject("DungCu");
        pv.transform.SetParent(_fx.transform, false);
        _pivot = pv.transform;
        var tg = new GameObject("Icon");
        tg.transform.SetParent(_pivot, false);
        _tool = tg.AddComponent<SpriteRenderer>();
        _tool.sprite = cfg.IconCua(kind);
        _tool.sortingLayerID = _layer;
        _tool.sortingOrder = Mathf.Clamp(_orderMax + 150, -32000, 32000);
        // [2026-09-25] Icon riu/bua trong game: luoi o TREN-TRAI, can xuong duoi-phai. Lat ngang -> luoi quay ve phia vat
        // (riu lao tu trai sang, luoi bo vao than cay), dau can nam o goc duoi-trai = tay cam = diem xoay.
        _tool.flipX = kind != WorldClearKind.Keo;
        if (_tool.sprite != null)
        {
            float S = CoDungCu();
            float h = Mathf.Max(0.001f, _tool.sprite.bounds.size.y);
            float s = S / h;
            tg.transform.localScale = new Vector3(s, s, 1f);
            // Goc duoi-trai (hoac duoi-phai khi lat) cua icon nam o pivot = tay cam
            Vector2 nua = new Vector2(_tool.sprite.bounds.size.x, _tool.sprite.bounds.size.y) * 0.5f * s;
            float chieu = kind == WorldClearKind.Bua ? -1f : 1f;
            tg.transform.localPosition = new Vector3(chieu * nua.x * 0.8f, nua.y * 0.8f, 0f);
        }
        CapNhatThanh();
    }

    private float CoDungCu()
    {
        float H = _b.size.y, W = _b.size.x;
        switch (kind)
        {
            case WorldClearKind.Riu: return Mathf.Clamp(Mathf.Min(H * 0.42f, W * 0.8f), 70f, 150f);
            case WorldClearKind.Keo: return Mathf.Clamp(Mathf.Max(H, W) * 0.55f, 55f, 110f);
            default: return Mathf.Clamp(Mathf.Max(H, W) * 0.7f, 60f, 130f);
        }
    }

    private SpriteRenderer TaoSr(string ten, Vector3 p, Sprite sp, Color c, int order)
    {
        var go = new GameObject(ten);
        go.transform.SetParent(_fx.transform, false);
        go.transform.position = p;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = c;
        sr.sortingLayerID = _layer;
        sr.sortingOrder = order;
        return sr;
    }

    private void CapNhatThanh()
    {
        if (_barFill == null) return;
        float p = TienDo01;
        float w = Mathf.Max(_barH, _barW * p);
        _barFill.size = new Vector2(w, _barH);
        _barFill.transform.position = _barNen.transform.position + new Vector3(-(_barW - w) * 0.5f, 0f, -0.001f);
        int s = GiayConLai;
        string chu = s >= 60 ? (s / 60) + ":" + (s % 60).ToString("00") : s + "s";
        if (chu != _chuCu && _txt != null) { _txt.text = chu; _chuCu = chu; }
        // hien dan khi vua bat dau
        float a = Mathf.Clamp01(_tVao * 2f);
        DatAlpha(_barNen, 0.88f * a); DatAlpha(_barFill, a);
        if (_txt != null) { var c = _txt.color; c.a = a; _txt.color = c; }
    }

    private static void DatAlpha(SpriteRenderer r, float a)
    {
        if (r == null) return;
        var c = r.color; c.a = a; r.color = c;
    }

    // ── Riu: tu trai lao vao, vung nhip 0.85s, bo vao chan cay ──
    private void AnimRiu()
    {
        if (_tool == null || _tool.sprite == null) { NhipKhongIcon(0.85f); return; }
        float S = CoDungCu();
        Vector3 C = new Vector3(_b.center.x - _b.size.x * 0.06f, _b.min.y + _b.size.y * 0.16f, _posGoc.z);
        float gHit = -18f, gRaise = 38f;
        float T = 0.85f;
        float u = Mathf.Repeat(_tAnim / T + _pha, 1f);
        float goc = NhipVung(u, gHit, gRaise, 0.58f, 0.76f);
        DatDungCu(C, S, goc, gHit, 1f, new Vector3(-S * 2.4f, S * 0.15f, 0f));
        if (u >= 0.76f && !_daDanh) { _daDanh = true; Danh(C, new Vector2(-1f, 0.7f)); }
        if (u < 0.5f) _daDanh = false;
    }

    // ── Bua: tu tren ben phai giang xuong, nhip 0.7s ──
    private void AnimBua()
    {
        if (_tool == null || _tool.sprite == null) { NhipKhongIcon(0.7f); return; }
        float S = CoDungCu();
        Vector3 C = new Vector3(_b.center.x + _b.size.x * 0.08f, _b.max.y - _b.size.y * 0.22f, _posGoc.z);
        float T = 0.7f;
        float u = Mathf.Repeat(_tAnim / T + _pha, 1f);
        float goc = -NhipVung(u, -22f, 62f, 0.6f, 0.74f);                     // lat guong: xoay nguoc chieu
        DatDungCu(C, S, goc, 22f, -1f, new Vector3(S * 1.6f, S * 1.8f, 0f));
        if (u >= 0.74f && !_daDanh) { _daDanh = true; Danh(C, new Vector2(0.4f, 1f)); }
        if (u < 0.5f) _daDanh = false;
    }

    // ── Keo: lia doc ngon bui trai -> phai -> trai, cat tach tach 0.42s ──
    private void AnimKeo()
    {
        if (_tool == null || _tool.sprite == null) { NhipKhongIcon(0.42f); return; }
        float S = CoDungCu();
        float chay = Mathf.PingPong(_tAnim * 0.35f, 1f);
        float x = Mathf.Lerp(_b.min.x + _b.size.x * 0.2f, _b.max.x - _b.size.x * 0.2f, chay);
        float yNgon = Mathf.Lerp(_b.max.y, _b.min.y + _b.size.y * 0.55f, TienDo01 * 0.6f);
        Vector3 C = new Vector3(x, yNgon - _b.size.y * 0.12f, _posGoc.z);
        float T = 0.42f;
        float u = Mathf.Repeat(_tAnim / T + _pha, 1f);
        float cat = u < 0.35f ? u / 0.35f : 1f - (u - 0.35f) / 0.65f;          // khep nhanh, mo cham
        float goc = Mathf.Lerp(-8f, 14f, cat);
        Vector3 vao = new Vector3(-S * 1.8f, S * 1.2f, 0f);
        float kv = 1f - EaseOut(_tVao);
        _pivot.position = C + new Vector3(-S * 0.35f, -S * 0.25f, 0f) + vao * kv;
        _pivot.rotation = Quaternion.Euler(0f, 0f, goc);
        float sq = 1f - 0.18f * cat;
        _pivot.localScale = new Vector3(sq, 1f / sq, 1f);
        DatAlpha(_tool, Mathf.Clamp01(_tVao * 2f));
        if (u >= 0.35f && !_daDanh) { _daDanh = true; Danh(C, new Vector2(Random.Range(-1f, 1f), 1f)); }
        if (u < 0.2f) _daDanh = false;
    }

    /// <summary>Vi tri + goc dung cu sao cho DAU dung cu cham dung diem C luc goc = gHit (tinh nguoc tu dau -> tay cam).</summary>
    private void DatDungCu(Vector3 C, float S, float goc, float gHit, float chieu, Vector3 huongVao)
    {
        Vector2 dau0 = DauDungCu(S, chieu);
        Vector2 dauHit = Xoay(dau0, gHit);
        Vector3 P = C - new Vector3(dauHit.x, dauHit.y, 0f);
        float kv = 1f - EaseOut(_tVao);
        _pivot.position = P + huongVao * kv;
        _pivot.rotation = Quaternion.Euler(0f, 0f, goc);
        _pivot.localScale = Vector3.one;
        DatAlpha(_tool, Mathf.Clamp01(_tVao * 2f));
    }

    private Vector2 DauDungCu(float S, float chieu)
    {
        if (_tool == null || _tool.sprite == null) return new Vector2(chieu * S * 0.7f, S * 0.8f);
        float s = _tool.transform.localScale.y;
        Vector2 kt = new Vector2(_tool.sprite.bounds.size.x, _tool.sprite.bounds.size.y) * s;
        // tam icon lech (0.4 W, 0.4 H) so voi pivot; dau dung cu ~ goc tren doi dien = (0.75 W, 0.75 H)
        return new Vector2(chieu * kt.x * 0.75f, kt.y * 0.75f);
    }

    private static Vector2 Xoay(Vector2 v, float goc)
    {
        float r = goc * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    /// <summary>0..a: gio len (cham, eo ra) · a..b: bo xuong (nhanh, tang toc) · b..1: nay nhe.</summary>
    private static float NhipVung(float u, float gHit, float gRaise, float a, float b)
    {
        if (u < a) return Mathf.Lerp(gHit, gRaise, EaseOut(u / a));
        if (u < b) { float t = (u - a) / (b - a); return Mathf.Lerp(gRaise, gHit, t * t); }
        float n = (u - b) / (1f - b);
        return gHit + Mathf.Sin(n * Mathf.PI) * 6f;
    }

    private static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }

    private void NhipKhongIcon(float T)
    {
        // Khong co icon dung cu (thieu art) -> van co nhip rung + vun cho thay dang lam
        float u = Mathf.Repeat(_tAnim / T, 1f);
        if (u >= 0.75f && !_daDanh) { _daDanh = true; Danh(new Vector3(_b.center.x, _b.min.y + _b.size.y * 0.3f, _posGoc.z), Vector2.up); }
        if (u < 0.5f) _daDanh = false;
    }

    private void Danh(Vector3 C, Vector2 huong)
    {
        if (_tVao < 0.9f) return;                             // dang lao vao, chua danh
        _rung = 1f;
        WorldClearFX.TrungDon(kind, C, K, huong, _layer, Mathf.Clamp(_orderMax + 200, -32000, 32000));
        if (AudioManager.Instance != null && WorldClearFX.TrongCamera(C, 0f))
        {
            if (kind == WorldClearKind.Bua) AudioManager.Instance.PlayBuildingHammer();
            else AudioManager.Instance.PlayCookingChop();
        }
    }

    private void HuyFx()
    {
        if (_fx != null) Destroy(_fx);
        _fx = null; _barNen = _barFill = _tool = null; _pivot = null; _txt = null;
    }

    // =====================================================================
    //  Xong
    // =====================================================================
    private void HoanThanh(bool coAnim)
    {
        if (_tt == TT.Xong) return;
        _tt = TT.Xong;
        enabled = false;
        PlayerPrefs.DeleteKey("WCLR_W_" + id);
        PlayerPrefs.DeleteKey("WCLR_T_" + id);
        PlayerPrefs.SetInt("WCLR_D_" + id, 1);
        WorldClearManager.GhiNhoId(id);
        PlayerPrefs.Save();
        HuyFx();
        WorldClearManager.TraThuong(this);
        if (coAnim && gameObject.activeInHierarchy && WorldClearFX.TrongCamera(_b.center, _b.size.y)) StartCoroutine(BienMat());
        else gameObject.SetActive(false);
    }

    private IEnumerator BienMat()
    {
        // Tat script khac tren vat (lac lu, la roi...) de khong gianh transform
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null && mb != this) mb.enabled = false;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        transform.position = _posGoc;
        Vector3 chan = DiemChan;
        int o = Mathf.Clamp(_orderMax + 200, -32000, 32000);

        if (kind == WorldClearKind.Riu)
        {
            float huong = Random.value < 0.5f ? -1f : 1f;
            float T = 0.8f, t = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / T);
                float goc = huong * 84f * u * u;                                  // do nhanh dan
                Quaternion q = Quaternion.Euler(0f, 0f, goc);
                transform.position = chan + q * (_posGoc - chan);
                transform.rotation = q * _rotGoc;
                DatAlphaTatCa(u < 0.55f ? 1f : 1f - (u - 0.55f) / 0.45f);
                yield return null;
            }
            WorldClearFX.BungKetThuc(kind, chan + new Vector3(huong * _b.size.y * 0.45f, 0f, 0f), K, _layer, o);
            WorldClearFX.BungKetThuc(kind, chan, K * 0.7f, _layer, o);
        }
        else
        {
            WorldClearFX.BungKetThuc(kind, chan, K, _layer, o);
            float T = kind == WorldClearKind.Keo ? 0.4f : 0.32f, t = 0f;
            while (t < T)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / T);
                float sx = kind == WorldClearKind.Bua ? 1f + 0.25f * u : 1f - 0.6f * u;
                float sy = 1f - u;
                transform.localScale = new Vector3(_scaleGoc.x * sx, _scaleGoc.y * Mathf.Max(0.01f, sy), _scaleGoc.z);
                DatAlphaTatCa(1f - u * u);
                yield return null;
            }
        }
        gameObject.SetActive(false);
    }

    private void DatAlphaTatCa(float a)
    {
        for (int i = 0; i < _rs.Length; i++)
        {
            var r = _rs[i];
            if (r == null) continue;
            var c = r.color; c.a = a; r.color = c;
        }
    }

    private void OnDestroy() => HuyFx();
}
