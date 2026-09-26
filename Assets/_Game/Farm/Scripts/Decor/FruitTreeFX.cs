// ============================================================================
//  FruitTreeFX — cay an qua (tao, cam, chanh, dua) dung tu Blender (2026-09-26)
//   - Tan cay (child "Tan") lac nhe quanh diem ngon than (khong lech khoi than).
//   - Lau lau 1 trai roi tu tan xuong dat, nay nhe, nam 1 chut roi mo dan bien mat.
//   - Chat bang riu: ten prefab bat dau "Prefab_FruitTree" -> WorldClear tu nhan (tien to "prefab_fruittree").
//  TOI UU MOBILE: ngoai man hinh khong chay gi. Moi cay chi 1 trai dung chung (tao 1 lan, tai su dung).
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class FruitTreeFX : MonoBehaviour
{
    [Header("Tan cay lac")]
    public Transform tan;
    [Tooltip("Diem xoay (ngon than cay), toa do local cua cay.")]
    public Vector2 diemXoay = new Vector2(0f, 1.2f);
    public float bienDoXoay = 1.3f;
    public float tocDoXoay = 1.2f;

    [Header("Trai roi")]
    public Sprite quaSprite;
    public float coQua = 0.55f;
    [Tooltip("Giay giua 2 lan roi (ngau nhien trong khoang).")]
    public Vector2 nhipRoi = new Vector2(8f, 18f);
    [Tooltip("Vung trai bat dau roi (local): x min/max, y min/max.")]
    public Vector4 vungQua = new Vector4(-0.9f, 0.9f, 1.3f, 2.6f);
    [Tooltip("Trai cham dat quanh day cay (local): x lech toi da, y min/max.")]
    public Vector3 vungDat = new Vector3(0.8f, -0.12f, 0.12f);

    private SpriteRenderer _tanSr;
    private float _pha, _henRoi, _t;
    private int _giai;                     // 0 cho, 1 roi, 2 nay, 3 nam, 4 mo
    private Transform _qua; private SpriteRenderer _quaSr;
    private Vector3 _tu, _den;

    private void Awake()
    {
        if (tan != null) _tanSr = tan.GetComponent<SpriteRenderer>();
        _pha = Random.Range(0f, 10f);
        _henRoi = Time.time + Random.Range(nhipRoi.x, nhipRoi.y);
    }

    private void OnDisable() { if (_qua != null) _qua.gameObject.SetActive(false); _giai = 0; }

    private void Update()
    {
        if (_tanSr == null || !_tanSr.isVisible) return;

        // --- lac tan: 2 song sin lech pha, xoay quanh ngon than
        float tt = Time.time * tocDoXoay + _pha;
        float a = Mathf.Sin(tt) * bienDoXoay + Mathf.Sin(tt * 2.3f + 1.7f) * bienDoXoay * 0.3f;
        Quaternion q = Quaternion.Euler(0f, 0f, a);
        Vector3 p = diemXoay;
        tan.localRotation = q;
        tan.localPosition = p - q * p;

        if (quaSprite != null) CapNhatQua();
    }

    private void CapNhatQua()
    {
        if (_giai == 0)
        {
            if (Time.time < _henRoi) return;
            if (_qua == null) TaoQua();
            _tu = new Vector3(Random.Range(vungQua.x, vungQua.y), Random.Range(vungQua.z, vungQua.w), 0f);
            _den = new Vector3(Mathf.Clamp(_tu.x * 0.8f + Random.Range(-0.2f, 0.2f), -vungDat.x, vungDat.x), Random.Range(vungDat.y, vungDat.z), 0f);
            _qua.localPosition = _tu; _qua.localRotation = Quaternion.identity;
            _quaSr.color = Color.white;
            _qua.gameObject.SetActive(true);
            _giai = 1; _t = 0f;
            return;
        }
        _t += Time.deltaTime;
        switch (_giai)
        {
            case 1:   // roi nhanh dan (gia toc)
            {
                float d = Mathf.Max(0.3f, (_tu.y - _den.y) * 0.22f);
                float k = Mathf.Clamp01(_t / d);
                _qua.localPosition = new Vector3(Mathf.Lerp(_tu.x, _den.x, k), Mathf.Lerp(_tu.y, _den.y, k * k), 0f);
                _qua.localRotation = Quaternion.Euler(0f, 0f, -k * 120f);
                if (k >= 1f) { _giai = 2; _t = 0f; }
                break;
            }
            case 2:   // nay nhe
            {
                float k = Mathf.Clamp01(_t / 0.28f);
                _qua.localPosition = _den + new Vector3(k * 0.12f, Mathf.Sin(k * 3.14159265f) * 0.18f, 0f);
                if (k >= 1f) { _den += new Vector3(0.12f, 0f, 0f); _giai = 3; _t = 0f; }
                break;
            }
            case 3:   // nam yen
                if (_t >= 1.4f) { _giai = 4; _t = 0f; }
                break;
            case 4:   // mo dan
            {
                float k = Mathf.Clamp01(_t / 0.6f);
                _quaSr.color = new Color(1f, 1f, 1f, 1f - k);
                if (k >= 1f)
                {
                    _qua.gameObject.SetActive(false);
                    _giai = 0;
                    _henRoi = Time.time + Random.Range(nhipRoi.x, nhipRoi.y);
                }
                break;
            }
        }
    }

    private void TaoQua()
    {
        var go = new GameObject("TraiRoi");
        _qua = go.transform;
        _qua.SetParent(transform, false);
        _qua.localScale = Vector3.one * coQua;
        _quaSr = go.AddComponent<SpriteRenderer>();
        _quaSr.sprite = quaSprite;
        if (_tanSr != null)
        {
            _quaSr.sortingLayerID = _tanSr.sortingLayerID;
            _quaSr.sortingOrder = _tanSr.sortingOrder + 1;
            _quaSr.sharedMaterial = _tanSr.sharedMaterial;
        }
        go.SetActive(false);
    }
}
