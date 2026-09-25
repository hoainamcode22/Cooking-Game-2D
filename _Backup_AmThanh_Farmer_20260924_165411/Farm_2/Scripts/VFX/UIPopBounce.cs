// ============================================================================
//  UIPopBounce — popup/bang mo ra co NAY nhe (2026-09-24)
//  Gan vao bang chinh cua popup (object duoc bat/tat khi mo/dong). Moi lan bat: 0.86 -> 1.04 -> 1
//  trong ~0.26s (gio thuc, chay ca khi game dung timeScale = 0).
//  An toan khi popup DA CO anim rieng: neu script khac dang doi scale cung luc -> tu dung ngay,
//  khong gianh giat. Chay xong chi con 1 phep so sanh bool moi khung (gan nhu 0).
//  Gan nhanh: chon bang trong Hierarchy > Tools > VFX > 2. Gan nay mo cho popup dang chon.
// ============================================================================
using UnityEngine;

[DisallowMultipleComponent]
public class UIPopBounce : MonoBehaviour
{
    [SerializeField] private float thoiGian = 0.26f;
    [SerializeField] private float batDau = 0.86f;
    [SerializeField] private float vuot = 1.04f;

    private Vector3 _goc = Vector3.one;
    private bool _coGoc;
    private Vector3 _vuaDat;
    private float _t;
    private bool _chay;

    private void OnEnable()
    {
        if (!_coGoc || transform.localScale != _vuaDat) { _goc = transform.localScale; _coGoc = true; }
        if (_goc == Vector3.zero) _goc = Vector3.one;
        _t = 0f;
        _chay = true;
        Dat(_goc * batDau);
    }

    private void OnDisable()
    {
        if (_chay) { transform.localScale = _goc; _chay = false; }
    }

    private void LateUpdate()
    {
        if (!_chay) return;
        if (transform.localScale != _vuaDat) { _chay = false; return; }   // script khac dang anim -> nhuong

        _t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(_t / Mathf.Max(0.05f, thoiGian));
        float s = k < 0.6f ? Mathf.Lerp(batDau, vuot, Mathf.Sin(k / 0.6f * Mathf.PI * 0.5f))
                           : Mathf.Lerp(vuot, 1f, (k - 0.6f) / 0.4f);
        Dat(_goc * s);
        if (k >= 1f) { Dat(_goc); _chay = false; }
    }

    private void Dat(Vector3 s)
    {
        transform.localScale = s;
        _vuaDat = s;
    }
}
