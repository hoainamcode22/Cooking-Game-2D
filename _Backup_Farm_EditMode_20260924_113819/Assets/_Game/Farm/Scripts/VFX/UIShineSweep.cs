// ============================================================================
//  UIShineSweep — vet sang luot cheo qua nut quan trong (2026-09-24)
//  Gan vao nut (hoac UIShineSweep.GanVao(rect)). Tu tao con "Fx_ShineMask" (RectMask2D, re hon
//  Mask vi khong dung stencil) + "Fx_Shine" (dai sang mem). Chi di chuyen trong ~0.55s moi
//  vai giay; luc cho: khong ghi gi vao UI -> canvas khong phai dung lai.
//  Nut bi an (inactive) -> khong chay gi.
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIShineSweep : MonoBehaviour
{
    [SerializeField] private float thoiGianLuot = 0.55f;
    [SerializeField] private Vector2 cachGiay = new Vector2(2.8f, 4.5f);
    [SerializeField] private float doRongTiLe = 0.28f;       // be rong vet sang / be rong nut
    [SerializeField] private float gocNghieng = 20f;
    [SerializeField] private Color mau = new Color(1f, 1f, 1f, 0.45f);

    private RectTransform _nut, _mask, _shine;
    private float _hen, _t = -1f;

    public static UIShineSweep GanVao(RectTransform nut)
    {
        if (nut == null) return null;
        var s = nut.GetComponent<UIShineSweep>();
        if (s == null) s = nut.gameObject.AddComponent<UIShineSweep>();
        return s;
    }

    private void OnEnable()
    {
        _nut = transform as RectTransform;
        DamBao();
        _t = -1f;
        _hen = Time.unscaledTime + Random.Range(0.6f, cachGiay.y);
        if (_shine != null) _shine.gameObject.SetActive(false);
    }

    private void DamBao()
    {
        if (_nut == null) return;
        if (_mask == null)
        {
            var m = _nut.Find("Fx_ShineMask") as RectTransform;
            if (m == null)
            {
                var go = new GameObject("Fx_ShineMask", typeof(RectTransform), typeof(RectMask2D));
                go.layer = gameObject.layer;
                m = (RectTransform)go.transform;
                m.SetParent(_nut, false);
                m.anchorMin = Vector2.zero; m.anchorMax = Vector2.one;
                m.offsetMin = new Vector2(6f, 6f); m.offsetMax = new Vector2(-6f, -6f);   // thut vao tranh goc bo tron
                go.AddComponent<LayoutElement>().ignoreLayout = true;   // nut co LayoutGroup cung khong bi xo lech
            }
            _mask = m;
        }
        _mask.SetAsLastSibling();
        if (_shine == null)
        {
            var s = _mask.Find("Fx_Shine") as RectTransform;
            if (s == null)
            {
                var go = new GameObject("Fx_Shine", typeof(RectTransform), typeof(Image));
                go.layer = gameObject.layer;
                s = (RectTransform)go.transform;
                s.SetParent(_mask, false);
                var img = go.GetComponent<Image>();
                img.sprite = SoftFxSprites.BandSprite;
                img.color = mau;
                img.raycastTarget = false;
            }
            _shine = s;
        }
    }

    private void Update()
    {
        if (_shine == null) return;
        float now = Time.unscaledTime;
        if (_t < 0f)
        {
            if (now < _hen) return;                  // dang cho: khong dung UI
            _t = 0f;
            Rect r = _mask.rect;
            _shine.anchorMin = _shine.anchorMax = new Vector2(0.5f, 0.5f);
            _shine.sizeDelta = new Vector2(Mathf.Max(24f, r.width * doRongTiLe), r.height * 1.8f);
            _shine.localRotation = Quaternion.Euler(0f, 0f, -gocNghieng);
            _shine.gameObject.SetActive(true);
        }
        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / Mathf.Max(0.1f, thoiGianLuot));
        float e = k * k * (3f - 2f * k);
        float w = _mask.rect.width + _shine.sizeDelta.x * 1.5f;
        _shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.5f, w * 0.5f, e), 0f);
        if (k >= 1f)
        {
            _shine.gameObject.SetActive(false);
            _t = -1f;
            _hen = now + Random.Range(cachGiay.x, cachGiay.y);
        }
    }
}
