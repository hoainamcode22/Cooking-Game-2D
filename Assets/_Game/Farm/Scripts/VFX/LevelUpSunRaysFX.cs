// ============================================================================
//  LevelUpSunRaysFX — TIA NANG XOAY cham sau ngoi sao popup LEN CAP (2026-09-24)
//  LevelUpPopupUI goi LevelUpSunRaysFX.GanVao(content) moi lan hien. Tu tao 2 lop tia (xoay
//  nguoc chieu nhau) ngay sau FX_QuangSang -> nam sau bang ten + ngoi sao.
//  Nam trong Canvas con rieng: xoay moi khung chi dung lai lop nay, khong dung lai ca popup.
//  Popup dong (inactive) -> khong chay gi. Them anh sang luot cho nut "Tiep tuc".
// ============================================================================
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LevelUpSunRaysFX : MonoBehaviour
{
    [SerializeField] private float tocDoXoay = 14f;
    [SerializeField] private Color mau = new Color(1f, 0.9f, 0.5f, 0.42f);

    private RectTransform _lop1, _lop2;
    private Image _img1, _img2;
    private float _t;

    public static void GanVao(Transform content)
    {
        if (content == null) return;
        var cu = content.Find("FX_TiaNang");
        LevelUpSunRaysFX fx;
        if (cu != null) fx = cu.GetComponent<LevelUpSunRaysFX>();
        else
        {
            var go = new GameObject("FX_TiaNang", typeof(RectTransform));
            go.layer = content.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(content, false);
            var glow = content.Find("FX_QuangSang") as RectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = glow != null ? glow.anchoredPosition : new Vector2(0f, 210f);
            rt.sizeDelta = new Vector2(1000f, 1000f);
            rt.SetSiblingIndex(glow != null ? glow.GetSiblingIndex() + 1 : 0);
            var cv = go.AddComponent<Canvas>();           // canvas con: xoay khong lam dung lai ca popup
            cv.overrideSorting = false;
            fx = go.AddComponent<LevelUpSunRaysFX>();
        }
        if (fx != null) fx.BatDau();

        var btn = content.Find("Btn_TiepTuc") as RectTransform;
        if (btn != null) UIShineSweep.GanVao(btn);
    }

    private void Awake()
    {
        _lop1 = TaoLop("Tia_1", 1f);
        _lop2 = TaoLop("Tia_2", 0.8f);
        _img1 = _lop1.GetComponent<Image>();
        _img2 = _lop2.GetComponent<Image>();
    }

    private RectTransform TaoLop(string ten, float tiLe)
    {
        var go = new GameObject(ten, typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = new Vector3(tiLe, tiLe, 1f);
        var img = go.GetComponent<Image>();
        img.sprite = SoftFxSprites.RaysSprite;
        img.raycastTarget = false;
        img.color = new Color(mau.r, mau.g, mau.b, 0f);
        return rt;
    }

    public void BatDau()
    {
        _t = 0f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _t += dt;
        float vao = Mathf.Clamp01(_t / 0.6f);
        float tho = 0.85f + 0.15f * Mathf.Sin(_t * 2f);
        _lop1.localRotation = Quaternion.Euler(0f, 0f, -_t * tocDoXoay);
        _lop2.localRotation = Quaternion.Euler(0f, 0f, 15f + _t * tocDoXoay * 0.6f);
        _img1.color = new Color(mau.r, mau.g, mau.b, mau.a * vao * tho);
        _img2.color = new Color(1f, 1f, 1f, mau.a * 0.6f * vao * (1.7f - tho));
    }
}
