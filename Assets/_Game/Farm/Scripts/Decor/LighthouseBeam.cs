// ============================================================================
//  LighthouseBeam — den hai dang quay 360 do soi sang (2026-09-26)
//   - "BeamPivot" nen doc 0.5 (goc iso) -> tia sang quet thanh hinh elip tren mat dat.
//   - Tia huong ra SAU thap thi mo di (nhu bi thap che), huong ra truoc thi sang ro.
//   - Quang sang o dinh den nhap nhay nhe.
//  TOI UU MOBILE: chi 2 sprite, khong Light2D, ngoai man hinh khong chay.
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class LighthouseBeam : MonoBehaviour
{
    public Transform xoay;                   // tia sang (con cua BeamPivot)
    public SpriteRenderer tia, quang;
    [Tooltip("So vong / giay.")]
    public float vongMoiGiay = 0.1f;
    [Range(0f, 1f)] public float doSangTia = 0.55f;
    [Range(0f, 1f)] public float doSangQuang = 0.8f;

    private SpriteRenderer _nha;
    private float _goc;

    private void Awake()
    {
        _nha = GetComponent<SpriteRenderer>();
        _goc = Random.Range(0f, 360f);
    }

    private void Update()
    {
        if (_nha != null && !_nha.isVisible && (tia == null || !tia.isVisible)) return;
        _goc = (_goc + 360f * vongMoiGiay * Time.deltaTime) % 360f;
        if (xoay != null) xoay.localRotation = Quaternion.Euler(0f, 0f, _goc);

        // huong tia tren man hinh: sin > 0 = huong len (ra sau thap) -> mo
        float s = Mathf.Sin(_goc * 0.017453292f);
        float k = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(s * 1.4f + 0.2f));
        if (tia != null) { var c = tia.color; c.a = doSangTia * k; tia.color = c; }
        if (quang != null)
        {
            var c = quang.color;
            c.a = doSangQuang * (0.8f + 0.2f * Mathf.Sin(Time.time * 3.1f)) * Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(s));
            quang.color = c;
        }
    }
}
