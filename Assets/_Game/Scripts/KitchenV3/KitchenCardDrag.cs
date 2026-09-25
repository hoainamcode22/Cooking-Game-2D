// ============================================================================
//  KitchenCardDrag — KEO THA the nguyen lieu / gia vi vao NOI (2026-09-24)
//  KitchenJuiceFX tu gan vao moi the trong khay. Cach dung:
//    - Cham nhanh  : nhu cu (chon / bo chon the).
//    - Vuot nhanh  : cuon khay nhu cu (chuyen tiep cho ScrollRect).
//    - GIU 0.16s roi keo : the nhac len, icon di theo tay; tha tren noi -> vao noi
//      (icon roi lach tach vao noi + noi rung rinh); tha ngoai -> icon bay ve cho cu.
// ============================================================================
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class KitchenCardDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
    IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private const float GIU_GIAY = 0.08f;   // [2026-09-24] giu rat ngan la nhac the

    private SelectableIngredientCard _the;
    private ScrollRect _scroll;
    private Canvas _canvas;
    private bool _nhan, _sanSang, _keoThe, _chuyenScroll;
    private float _nhanLuc;
    private Vector2 _contentLucDau;
    private RectTransform _bong;
    private Image _bongImg;
    private Vector3 _scale0 = Vector3.one;
    private bool _dangNhac;
    private static CookingSelectionManager _sel;

    private void Awake()
    {
        _the = GetComponent<SelectableIngredientCard>();
        _scroll = GetComponentInParent<ScrollRect>();
        _canvas = GetComponentInParent<Canvas>();
        _scale0 = transform.localScale;
    }

    private bool CoTheKeo() =>
        _the != null && !_the.IsSelected && _the.GetQuantity() > 0 && KitchenJuiceFX.Instance != null;

    public void OnPointerDown(PointerEventData e)
    {
        _nhan = true; _sanSang = false; _nhanLuc = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData e)
    {
        _nhan = false;
        if (!_keoThe) HaThe();
    }

    private void Update()
    {
        if (!_nhan || _sanSang || _keoThe || _chuyenScroll) return;
        if (Time.unscaledTime - _nhanLuc >= GIU_GIAY && CoTheKeo())
        {
            _sanSang = true;
            NhacThe();
        }
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        if (_scroll != null) _scroll.OnInitializePotentialDrag(e);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // [2026-09-24] Keo nhanh: vuot LEN (ve phia noi) thi keo the ngay; khay khong can cuon cung keo ngay.
        bool vuotLen = e.delta.y > 0f && Mathf.Abs(e.delta.y) >= Mathf.Abs(e.delta.x) * 0.6f;
        if (CoTheKeo() && (_sanSang || vuotLen || !KhayCuonDuoc()))
        {
            _keoThe = true;
            NhacThe();
            TaoBong(e);
        }
        else
        {
            _chuyenScroll = true;
            _nhan = false;
            HaThe();
            if (_scroll != null) { _contentLucDau = _scroll.content != null ? _scroll.content.anchoredPosition : Vector2.zero; _scroll.OnBeginDrag(e); }
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (_keoThe)
        {
            if (_bong != null) DatBongTheoTay(e);
            var fx = KitchenJuiceFX.Instance;
            if (fx != null) fx.BaoHieuNoi(fx.TrenNoi(e.position, e.pressEventCamera));
        }
        else if (_chuyenScroll && _scroll != null)
        {
            // Dang cuon ma ngon tay keo ra KHOI khay (len phia noi) -> doi sang keo the
            var vp = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
            if (CoTheKeo() && !RectTransformUtility.RectangleContainsScreenPoint(vp, e.position, e.pressEventCamera))
            {
                _scroll.OnEndDrag(e);
                if (_scroll.content != null) _scroll.content.anchoredPosition = _contentLucDau;
                _scroll.velocity = Vector2.zero;
                _chuyenScroll = false;
                _keoThe = true;
                NhacThe();
                TaoBong(e);
                return;
            }
            _scroll.OnDrag(e);
        }
    }

    private bool KhayCuonDuoc()
    {
        if (_scroll == null || _scroll.content == null) return false;
        var vp = _scroll.viewport != null ? _scroll.viewport : (RectTransform)_scroll.transform;
        return (_scroll.vertical && _scroll.content.rect.height > vp.rect.height + 4f)
            || (_scroll.horizontal && _scroll.content.rect.width > vp.rect.width + 4f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_keoThe)
        {
            _keoThe = false;
            var fx = KitchenJuiceFX.Instance;
            bool vao = fx != null && fx.TrenNoi(e.position, e.pressEventCamera) && CoTheKeo();
            if (vao)
            {
                KitchenJuiceFX.DatDiemTha(_bong != null ? _bong.position : transform.position);
                if (_sel == null) _sel = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);
                if (_sel != null) _sel.TrySelect(_the);   // TrySelect -> AnimateThrow -> KitchenJuiceFX.ThaVaoNoi
                XoaBong();
            }
            else if (_bong != null) StartCoroutine(CoBongVe());
            HaThe();
        }
        else if (_chuyenScroll)
        {
            _chuyenScroll = false;
            if (_scroll != null) _scroll.OnEndDrag(e);
        }
        _nhan = false; _sanSang = false;
    }

    public void OnScroll(PointerEventData e)
    {
        if (_scroll != null) _scroll.OnScroll(e);
    }

    // ── hinh anh ──
    private void NhacThe()
    {
        if (_dangNhac) return;
        _dangNhac = true;
        _scale0 = transform.localScale;
        transform.localScale = _scale0 * 1.08f;
    }

    private void HaThe()
    {
        if (!_dangNhac) return;
        _dangNhac = false;
        transform.localScale = _scale0;
    }

    private void TaoBong(PointerEventData e)
    {
        AudioManager.Instance?.PlayCardPick();       // [AM THANH 2026-09-24] nhac the
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        var goc = _canvas != null ? _canvas.rootCanvas.transform : transform.root;
        var go = new GameObject("Fx_KeoThe", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        go.layer = gameObject.layer;
        _bong = (RectTransform)go.transform;
        _bong.SetParent(goc, false);
        _bong.SetAsLastSibling();
        _bong.sizeDelta = new Vector2(110f, 110f);
        _bongImg = go.GetComponent<Image>();
        _bongImg.sprite = _the.GetMainSprite();
        _bongImg.preserveAspect = true;
        _bongImg.raycastTarget = false;
        go.GetComponent<CanvasGroup>().blocksRaycasts = false;
        DatBongTheoTay(e);
    }

    private void DatBongTheoTay(PointerEventData e)
    {
        var cha = _bong.parent as RectTransform;
        if (cha != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(cha, e.position, e.pressEventCamera, out Vector3 w))
            _bong.position = w + (Vector3)(Vector2.up * 30f * cha.lossyScale.y);
        float lac = Mathf.Sin(Time.unscaledTime * 18f) * 6f;
        _bong.localRotation = Quaternion.Euler(0f, 0f, lac);
    }

    private System.Collections.IEnumerator CoBongVe()
    {
        Vector3 a = _bong.position, b = transform.position;
        AudioManager.Instance?.PlayCardReturn();     // [AM THANH] the bay ve (chung file card_pick)
        float t = 0f;
        while (t < 0.22f && _bong != null)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.22f;
            _bong.position = Vector3.Lerp(a, b, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        XoaBong();
    }

    private void XoaBong()
    {
        if (_bong != null) Destroy(_bong.gameObject);
        _bong = null;
    }

    private void OnDisable()
    {
        XoaBong();
        HaThe();
        _nhan = _sanSang = _keoThe = _chuyenScroll = false;
    }
}
