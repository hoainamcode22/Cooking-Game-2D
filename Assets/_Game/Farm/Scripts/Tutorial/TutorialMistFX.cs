// ============================================================================
//  TutorialMistFX — lam may intro tutorial MO AO, DIU NHE (2026-09-24)
//  Tu gan vao Cloud_Panel luc chay (TutorialManager goi), khong can keo tha gi.
//    - Lop suong phu (Mist_Haze): phu mo nhe len 2 manh may, tan dau tien
//    - 16 cum suong mem (Mist_Puff_i): troi lo lung, tach ra 2 ben cung may voi toc do khac
//      nhau (chieu sau), tan dan -> mep may nhoe mem, khong con mep cat cung
//    - Bui sang lap lanh (Mist_Spark_i): hat nho bay len, nhap nhay, tan cuoi cung
//  Chi dung sprite ve bang code (SoftFxSprites) -> khong them file anh.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialMistFX : MonoBehaviour
{
    private class Lop
    {
        public RectTransform rt;
        public Image img;
        public Vector2 goc;
        public float huong;      // -1 trai, +1 phai
        public float sau;        // he so chieu sau (parallax)
        public float alpha0;
        public float pha;
        public float tanTu, tanDen;
        public float scale0;
    }

    private RectTransform _panel;
    private Image _haze;
    private readonly List<Lop> _puffs = new List<Lop>();
    private readonly List<Lop> _sparks = new List<Lop>();
    private float _dist = 620f;
    private float _thoiGian;
    private bool _daDung;

    public static TutorialMistFX GanVao(GameObject panel, float khoangTach)
    {
        if (panel == null) return null;
        var fx = panel.GetComponent<TutorialMistFX>();
        if (fx == null) fx = panel.AddComponent<TutorialMistFX>();
        fx._dist = Mathf.Max(200f, khoangTach);
        fx.Dung();
        return fx;
    }

    private void Dung()
    {
        if (_daDung) return;
        _panel = transform as RectTransform;
        if (_panel == null) return;
        _daDung = true;

        Rect r = _panel.rect;
        float W = r.width > 10f ? r.width : 1920f, H = r.height > 10f ? r.height : 1080f;

        // Suong phu toan man
        var hz = TaoAnh("Mist_Haze", null, new Color(0.94f, 0.97f, 1f, 0.3f));
        hz.rt.anchorMin = Vector2.zero; hz.rt.anchorMax = Vector2.one;
        hz.rt.offsetMin = Vector2.zero; hz.rt.offsetMax = Vector2.zero;
        _haze = hz.img;

        // Cum suong: dam hon o duong noi giua 2 manh may (x ~ 0)
        for (int i = 0; i < 16; i++)
        {
            bool giua = i < 5;
            float x = giua ? Random.Range(-0.12f, 0.12f) * W : Random.Range(-0.5f, 0.5f) * W;
            float y = Random.Range(-0.45f, 0.45f) * H;
            float kt = giua ? Random.Range(620f, 900f) : Random.Range(420f, 760f);
            Color c = Random.value < 0.6f ? new Color(1f, 1f, 1f, 1f) : new Color(0.88f, 0.94f, 1f, 1f);
            float a = giua ? Random.Range(0.55f, 0.75f) : Random.Range(0.35f, 0.6f);
            c.a = a;
            var l = TaoAnh("Mist_Puff_" + i, SoftFxSprites.GlowSprite, c);
            l.rt.sizeDelta = new Vector2(kt * Random.Range(1.1f, 1.5f), kt);
            l.rt.anchoredPosition = new Vector2(x, y);
            l.goc = l.rt.anchoredPosition;
            l.huong = x < 0f ? -1f : (x > 0f ? 1f : (Random.value < 0.5f ? -1f : 1f));
            l.sau = Random.Range(0.75f, 1.35f);
            l.alpha0 = a;
            l.pha = Random.value * 10f;
            l.tanTu = Random.Range(0.15f, 0.35f);
            l.tanDen = Random.Range(0.6f, 0.9f);
            l.scale0 = 1f;
            _puffs.Add(l);
        }

        // Bui sang lap lanh
        for (int i = 0; i < 20; i++)
        {
            Color c = Random.value < 0.5f ? new Color(1f, 0.97f, 0.8f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
            var l = TaoAnh("Mist_Spark_" + i, (i % 3 == 0) ? SoftFxSprites.SparkleSprite : SoftFxSprites.CircleSprite, c);
            float kt = (i % 3 == 0) ? Random.Range(22f, 36f) : Random.Range(8f, 16f);
            l.rt.sizeDelta = new Vector2(kt, kt);
            l.rt.anchoredPosition = new Vector2(Random.Range(-0.48f, 0.48f) * W, Random.Range(-0.5f, 0.4f) * H);
            l.goc = l.rt.anchoredPosition;
            l.huong = l.goc.x < 0f ? -1f : 1f;
            l.sau = Random.Range(0.3f, 0.7f);
            l.alpha0 = Random.Range(0.55f, 0.95f);
            l.pha = Random.value * 10f;
            l.tanTu = Random.Range(0.55f, 0.75f);
            l.tanDen = Random.Range(0.85f, 1f);
            l.scale0 = 1f;
            _sparks.Add(l);
        }
    }

    private Lop TaoAnh(string ten, Sprite sp, Color c)
    {
        var go = new GameObject(ten, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(_panel, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.SetAsLastSibling();
        var img = go.GetComponent<Image>();
        img.sprite = sp;
        img.color = c;
        img.raycastTarget = false;
        return new Lop { rt = rt, img = img };
    }

    /// <summary>Hien lai trang thai dau (goi truoc moi lan intro).</summary>
    public void BatDau()
    {
        _thoiGian = 0f;
        if (_haze != null) _haze.color = new Color(0.94f, 0.97f, 1f, 0.3f);
        foreach (var l in _puffs) { l.rt.anchoredPosition = l.goc; l.rt.localScale = Vector3.one; DatAlpha(l, l.alpha0); }
        foreach (var l in _sparks) { l.rt.anchoredPosition = l.goc; DatAlpha(l, 0f); }
    }

    /// <summary>t &lt; 0: dang cho truoc khi tach (chi troi nhe). 0..1: tien do tach may (da ease).</summary>
    public void Tick(float t, float dt)
    {
        _thoiGian += dt;
        float tt = Mathf.Clamp01(t);

        if (_haze != null)
        {
            var c = _haze.color;
            c.a = 0.3f * (1f - Mathf.Clamp01(tt / 0.5f));
            _haze.color = c;
        }

        for (int i = 0; i < _puffs.Count; i++)
        {
            var l = _puffs[i];
            Vector2 troi = new Vector2(Mathf.Sin(_thoiGian * 0.55f + l.pha) * 22f, Mathf.Cos(_thoiGian * 0.4f + l.pha) * 12f);
            Vector2 tach = new Vector2(l.huong * _dist * 1.1f * l.sau * tt, 30f * tt * (l.sau - 1f));
            l.rt.anchoredPosition = l.goc + troi + tach;
            float s = 1f + 0.05f * Mathf.Sin(_thoiGian * 0.8f + l.pha) + 0.35f * tt;
            l.rt.localScale = new Vector3(s, s, 1f);
            float tan = t < 0f ? 0f : Mathf.InverseLerp(l.tanTu, l.tanDen, tt);
            DatAlpha(l, l.alpha0 * (1f - tan * tan * (3f - 2f * tan)));
        }

        for (int i = 0; i < _sparks.Count; i++)
        {
            var l = _sparks[i];
            float len = (_thoiGian * 28f * (0.6f + l.sau)) % 260f;
            Vector2 p = l.goc + new Vector2(Mathf.Sin(_thoiGian * 1.3f + l.pha) * 14f + l.huong * _dist * 0.5f * l.sau * tt, len);
            l.rt.anchoredPosition = p;
            float nhay = 0.5f + 0.5f * Mathf.Sin(_thoiGian * 5f + l.pha * 3f);
            float vao = Mathf.Clamp01(_thoiGian / 0.5f);
            float ra = t < 0f ? 0f : Mathf.InverseLerp(l.tanTu, l.tanDen, tt);
            DatAlpha(l, l.alpha0 * nhay * vao * (1f - ra) * (1f - Mathf.Clamp01((len - 200f) / 60f)));
            l.rt.localRotation = Quaternion.Euler(0f, 0f, _thoiGian * 40f * (i % 2 == 0 ? 1f : -1f));
        }
    }

    /// <summary>An het (sau khi intro xong).</summary>
    public void KetThuc()
    {
        if (_haze != null) { var c = _haze.color; c.a = 0f; _haze.color = c; }
        foreach (var l in _puffs) DatAlpha(l, 0f);
        foreach (var l in _sparks) DatAlpha(l, 0f);
    }

    private static void DatAlpha(Lop l, float a)
    {
        if (l.img == null) return;
        var c = l.img.color; c.a = a; l.img.color = c;
    }
}
