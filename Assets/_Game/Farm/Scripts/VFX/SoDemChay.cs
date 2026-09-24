// ============================================================================
//  SoDemChay — so vang / EXP tren HUD DEM CHAY toi gia tri moi (2026-09-24)
//  Toi uu: component chi bat (enabled) trong ~0.6s dang dem, dem xong tu tat -> luc dung yen
//  khong ton 1 dong Update nao. Lan dat dau tien hien ngay (khong dem tu 0 luc vao game).
//    SoDemChay.Dat(txtGold, 1250, "N0", culture)              -> vang
//    SoDemChay.Dat(txtExp, 40, "N0", culture, " / 120", fill, 0.33f, false) -> EXP + thanh
// ============================================================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SoDemChay : MonoBehaviour
{
    [Tooltip("Thoi gian dem (giay, gio thuc).")]
    [SerializeField] private float thoiGian = 0.6f;
    [Tooltip("Cho 1 nhip truoc khi dem TANG (cho icon vang/EXP bay toi HUD roi moi nhay so).")]
    [SerializeField] private float treKhiTang = 0.5f;
    [Tooltip("Phong nhe chu khi dang dem tang.")]
    [SerializeField] private float nayChu = 0.12f;

    private TMP_Text _txt;
    private Image _fill;
    private string _dinhDang = "N0", _hauTo = "";
    private IFormatProvider _fp;
    private bool _coGiaTri;
    private long _hien, _tu, _den;
    private float _fillTu, _fillDen;
    private float _t, _tre;
    private Vector3 _scale0 = Vector3.one;
    private bool _dangNay;

    /// <summary>Dat gia tri moi cho o chu (tu gan component neu chua co).</summary>
    public static void Dat(TMP_Text txt, long giaTri, string dinhDang, IFormatProvider fp,
                           string hauTo = null, Image fill = null, float fillDich = -1f, bool demKhiGiam = true)
    {
        if (txt == null) return;
        var d = txt.GetComponent<SoDemChay>();
        if (d == null) d = txt.gameObject.AddComponent<SoDemChay>();
        d.DatGiaTri(txt, giaTri, dinhDang, fp, hauTo, fill, fillDich, demKhiGiam);
    }

    private void DatGiaTri(TMP_Text txt, long v, string dd, IFormatProvider fp, string hauTo, Image fill, float fillDich, bool demKhiGiam)
    {
        _txt = txt; _dinhDang = string.IsNullOrEmpty(dd) ? "N0" : dd; _fp = fp; _hauTo = hauTo ?? "";
        _fill = fill;

        bool conHoatDong = gameObject.activeInHierarchy;   // (enabled tu tat khi dung yen nen KHONG xet enabled)
        if (!_coGiaTri || !conHoatDong || (v < _hien && !demKhiGiam))
        {
            // Lan dau / dang an / giam ma khong dem -> hien ngay
            _coGiaTri = true;
            _hien = _den = _tu = v;
            if (_fill != null && fillDich >= 0f) { _fill.fillAmount = fillDich; _fillTu = _fillDen = fillDich; }
            Ghi(v);
            DungNay();
            enabled = false;
            return;
        }

        _tu = _hien;
        _den = v;
        _fillTu = _fill != null ? _fill.fillAmount : 0f;
        _fillDen = fillDich >= 0f ? fillDich : _fillTu;
        _t = 0f;
        _tre = v > _hien ? treKhiTang : 0f;
        if (_den == _tu && Mathf.Approximately(_fillTu, _fillDen)) { Ghi(v); enabled = false; return; }
        enabled = true;
    }

    private void Update()
    {
        if (_txt == null) { enabled = false; return; }
        float dt = Time.unscaledDeltaTime;
        if (_tre > 0f) { _tre -= dt; return; }

        _t += dt;
        float k = Mathf.Clamp01(_t / Mathf.Max(0.05f, thoiGian));
        float e = 1f - (1f - k) * (1f - k) * (1f - k);
        _hien = _tu + (long)Math.Round((_den - _tu) * (double)e);
        Ghi(_hien);
        if (_fill != null) _fill.fillAmount = Mathf.Lerp(_fillTu, _fillDen, e);

        if (_den > _tu && nayChu > 0f)
        {
            if (!_dangNay) { _scale0 = _txt.rectTransform.localScale; _dangNay = true; }
            float s = 1f + nayChu * Mathf.Sin(k * Mathf.PI);
            _txt.rectTransform.localScale = _scale0 * s;
        }

        if (k >= 1f)
        {
            _hien = _den;
            Ghi(_den);
            DungNay();
            enabled = false;
        }
    }

    private void DungNay()
    {
        if (_dangNay && _txt != null) _txt.rectTransform.localScale = _scale0;
        _dangNay = false;
    }

    private void OnDisable() => DungNay();

    private void Ghi(long v)
    {
        if (_txt == null) return;
        _txt.text = (_fp != null ? v.ToString(_dinhDang, _fp) : v.ToString(_dinhDang)) + _hauTo;
    }
}
