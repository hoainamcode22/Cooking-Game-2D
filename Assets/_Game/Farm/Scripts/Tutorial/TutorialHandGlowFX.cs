// ============================================================================
//  TutorialHandGlowFX — hao quang mem + bui lap lanh quanh BAN TAY chi dan tutorial (2026-09-24)
//  Gan vao chinh ban tay (TutorialManager tu goi GanVao). Hao quang nam o anh em NGAY TRUOC
//  ban tay trong cung cha -> ve PHIA SAU tay, bam theo vi tri/bat-tat cua tay moi khung.
// ============================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialHandGlowFX : MonoBehaviour
{
    [SerializeField] private float kichThuocHaoQuang = 190f;
    [SerializeField] private Color mauHaoQuang = new Color(1f, 0.93f, 0.6f, 0.55f);
    [SerializeField] private Vector2 lechDauNgon = new Vector2(-18f, 34f);   // tam hao quang ~ dau ngon tro
    [SerializeField] private float nhipTho = 2.6f;
    [SerializeField] private float moiHatGiay = 0.22f;

    private RectTransform _tay, _glow;
    private Image _glowImg;
    private CanvasGroup _cgTay;
    private float _hen;
    private readonly List<RectTransform> _hat = new List<RectTransform>();
    private readonly List<float> _tuoi = new List<float>();
    private readonly List<Vector2> _vanToc = new List<Vector2>();

    public static TutorialHandGlowFX GanVao(RectTransform tay)
    {
        if (tay == null) return null;
        var fx = tay.GetComponent<TutorialHandGlowFX>();
        if (fx == null) fx = tay.gameObject.AddComponent<TutorialHandGlowFX>();
        return fx;
    }

    private void Awake()
    {
        _tay = transform as RectTransform;
        _cgTay = GetComponentInParent<CanvasGroup>();
    }

    private void DamBaoGlow()
    {
        if (_glow != null || _tay == null || _tay.parent == null) return;
        var go = new GameObject("Tutorial_HandGlow", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        _glow = (RectTransform)go.transform;
        _glow.SetParent(_tay.parent, false);
        _glow.anchorMin = _glow.anchorMax = new Vector2(0.5f, 0.5f);
        _glow.sizeDelta = new Vector2(kichThuocHaoQuang, kichThuocHaoQuang);
        _glowImg = go.GetComponent<Image>();
        _glowImg.sprite = SoftFxSprites.GlowSprite;
        _glowImg.raycastTarget = false;
        _glowImg.color = mauHaoQuang;
    }

    private void LateUpdate()
    {
        DamBaoGlow();
        if (_glow == null) return;

        bool hien = _tay.gameObject.activeInHierarchy;
        if (_glow.gameObject.activeSelf != hien) _glow.gameObject.SetActive(hien);

        // Giu ngay sau lung ban tay
        if (_glow.parent != _tay.parent) _glow.SetParent(_tay.parent, false);
        int iTay = _tay.GetSiblingIndex(), iGlow = _glow.GetSiblingIndex();
        if (iGlow > iTay) _glow.SetSiblingIndex(iTay);              // dang ve tren tay -> dua ra sau
        else if (iGlow < iTay - 1) _glow.SetSiblingIndex(iTay - 1); // sat ngay sau lung tay

        float w = Time.unscaledTime;
        float tho = 0.5f + 0.5f * Mathf.Sin(w * Mathf.PI * 2f / Mathf.Max(0.5f, nhipTho));
        Vector3 lech = _tay.TransformVector(new Vector3(lechDauNgon.x, lechDauNgon.y, 0f));
        _glow.position = _tay.position + lech;
        float s = 0.9f + 0.22f * tho;
        _glow.localScale = new Vector3(s, s, 1f);
        float a = mauHaoQuang.a * (0.55f + 0.45f * tho) * (_cgTay != null ? _cgTay.alpha : 1f);
        _glowImg.color = new Color(mauHaoQuang.r, mauHaoQuang.g, mauHaoQuang.b, a);

        // Bui lap lanh nho bay ra quanh dau ngon
        if (hien && w >= _hen)
        {
            _hen = w + moiHatGiay * Random.Range(0.7f, 1.3f);
            TaoHat();
        }
        CapNhatHat(Time.unscaledDeltaTime);
    }

    private void TaoHat()
    {
        var go = new GameObject("Tutorial_HandSpark", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(_glow.parent, false);
        rt.SetSiblingIndex(_glow.GetSiblingIndex());
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float kt = Random.Range(14f, 26f);
        rt.sizeDelta = new Vector2(kt, kt);
        rt.position = _glow.position;
        var img = go.GetComponent<Image>();
        img.sprite = Random.value < 0.5f ? SoftFxSprites.SparkleSprite : SoftFxSprites.CircleSprite;
        img.color = Random.value < 0.5f ? new Color(1f, 0.95f, 0.7f, 1f) : Color.white;
        img.raycastTarget = false;
        float g = Random.Range(0f, Mathf.PI * 2f);
        _hat.Add(rt);
        _tuoi.Add(0f);
        _vanToc.Add(new Vector2(Mathf.Cos(g), Mathf.Sin(g) * 0.6f + 0.5f) * Random.Range(40f, 80f));
    }

    private void CapNhatHat(float dt)
    {
        const float SONG = 0.9f;
        for (int i = _hat.Count - 1; i >= 0; i--)
        {
            var rt = _hat[i];
            _tuoi[i] += dt;
            if (rt == null || _tuoi[i] >= SONG)
            {
                if (rt != null) Destroy(rt.gameObject);
                _hat.RemoveAt(i); _tuoi.RemoveAt(i); _vanToc.RemoveAt(i);
                continue;
            }
            float k = _tuoi[i] / SONG;
            rt.anchoredPosition += _vanToc[i] * dt;
            float s = Mathf.Sin(k * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0f, 0f, k * 120f);
            var img = rt.GetComponent<Image>();
            var c = img.color; c.a = s; img.color = c;
        }
    }

    private void OnDisable()
    {
        if (_glow != null) _glow.gameObject.SetActive(false);
        for (int i = 0; i < _hat.Count; i++) if (_hat[i] != null) Destroy(_hat[i].gameObject);
        _hat.Clear(); _tuoi.Clear(); _vanToc.Clear();
    }

    private void OnDestroy()
    {
        if (_glow != null) Destroy(_glow.gameObject);
    }
}
