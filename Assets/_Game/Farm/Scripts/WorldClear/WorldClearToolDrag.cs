// ============================================================================
//  WorldClearToolDrag — KEO icon dung cu tu khay tha vao cay / bui / da (2026-09-25)
//  Keo: icon ma bay theo ngon tay, lac lu nhe. Tro trung vat cung loai -> vat nhun + icon phong to.
//  Tha: trung vat -> WorldClearManager.ThuBatDau (tru 1 dung cu, bat dau dem gio). Truot -> icon bay ve.
// ============================================================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldClearToolDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public WorldClearTrayUI tray;

    private RectTransform _ma;
    private Image _maImg;
    private Canvas _cv;
    private WorldClearable _dangTro;
    private float _t;
    private bool _dangKeo;

    public void OnPointerClick(PointerEventData e)
    {
        if (_dangKeo || tray == null) return;
        tray.RungLoi();                                              // cham icon: nhac "keo di"
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (tray == null || !tray.CoTheKeo) { if (tray != null) tray.RungLoi(); return; }
        var src = GetComponent<Image>();
        _cv = GetComponentInParent<Canvas>();
        if (_cv != null) _cv = _cv.rootCanvas;
        if (src == null || _cv == null) return;

        var go = new GameObject("DungCu_DangKeo", typeof(RectTransform));
        _ma = (RectTransform)go.transform;
        _ma.SetParent(_cv.transform, false);
        _ma.SetAsLastSibling();
        _ma.sizeDelta = ((RectTransform)transform).rect.size * 1.1f;
        _maImg = go.AddComponent<Image>();
        _maImg.sprite = src.sprite;
        _maImg.preserveAspect = true;
        _maImg.raycastTarget = false;
        var c = src.color; c.a = 0.3f; src.color = c;
        _dangKeo = true;
        WorldClearTrayUI.DangKeo = true;
        _t = 0f;
        DatViTri(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dangKeo || _ma == null) return;
        _t += Time.unscaledDeltaTime;
        DatViTri(e.position);
        var mgr = WorldClearManager.Instance;
        var c = mgr != null ? mgr.TimTai(e.position) : null;
        if (c != null && (c.kind != tray.Kind || c.DangLam)) c = null;
        if (c != _dangTro) { _dangTro = c; if (c != null) c.NhunChon(); }
        float s = _dangTro != null ? 1.18f : 1f;
        _ma.localScale = Vector3.Lerp(_ma.localScale, new Vector3(s, s, 1f), 0.35f);
        _ma.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 9f) * 8f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_dangKeo) return;
        _dangKeo = false;
        WorldClearTrayUI.DangKeo = false;
        if (_ma != null) Destroy(_ma.gameObject);
        _ma = null;
        var src = GetComponent<Image>();
        if (src != null) { var c = src.color; c.a = 1f; src.color = c; }

        var mgr = WorldClearManager.Instance;
        var vat = mgr != null ? mgr.TimTai(e.position) : null;
        _dangTro = null;
        if (vat == null || tray == null || vat.kind != tray.Kind) return;
        string loi;
        if (mgr.ThuBatDau(vat, out loi))
        {
            AudioManager.Instance?.PlayUIClick();
            tray.HienDangLam(vat);                                   // khay chuyen sang dem gio + nut kim cuong
        }
        else if (!string.IsNullOrEmpty(loi)) LockedHintFX.Show(loi, e.position);
    }

    private void DatViTri(Vector2 manHinh)
    {
        if (_ma == null || _cv == null) return;
        var cam = _cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cv.worldCamera;
        Vector2 lp;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_cv.transform, manHinh, cam, out lp))
            _ma.anchoredPosition = lp + new Vector2(0f, 40f);        // lech len tren ngon tay cho de nhin
    }

    private void OnDisable()
    {
        if (_ma != null) Destroy(_ma.gameObject);
        _ma = null;
        if (_dangKeo) { _dangKeo = false; WorldClearTrayUI.DangKeo = false; }
    }
}
