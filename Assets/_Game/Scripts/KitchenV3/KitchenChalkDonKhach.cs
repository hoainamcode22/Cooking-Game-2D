// ============================================================================
//  KitchenChalkDonKhach — BANG PHAN (Today's Special) = DON CUA KHACH DU LICH (2026-09-25)
//  - Giua bang: icon mon khach dat, ten mon ben duoi, "1/3" + chu Ready neu mon da co san (kho / tren dia).
//  - KEO NGANG (trai <-> phai) de xem don khach thu 2, thu 3... (vong tron). Co mui ten mo 2 ben.
//  - CHAM vao mon -> bang giua (Order_Banner) hien dung mon do + nguyen lieu (chon san cong thuc).
//  - Nau xong mon 1 -> bang tu luot sang khach can nau tiep (tru khi Sep vua keo tay < 4s).
//  Tu gan vao Kitchen_UI_v3/Chalkboard luc chay (KitchenSceneV2UI). An 3 dong chu mon hom nay cu.
// ============================================================================
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class KitchenChalkDonKhach : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Bo cuc (don vi canvas)")]
    public float coIcon = 130f;
    public Vector2 lechIcon = new Vector2(0f, 6f);
    public float coChuTen = 24f;
    public float coChuPhu = 15f;
    public Color mauChu = new Color(0.97f, 0.94f, 0.86f, 1f);
    public Color mauDaCo = new Color(0.55f, 1f, 0.55f, 1f);
    [Tooltip("Keo ngang it nhat bao nhieu (don vi canvas) thi doi don.")]
    public float nguongKeo = 50f;

    private KitchenUIv2.KitchenSceneV2UI _ui;
    private RectTransform _noiDung;
    private Image _img;
    private TMP_Text _ten, _phu, _trai, _phai;
    private readonly List<TouristAgent> _ds = new List<TouristAgent>(8);
    private int _chiSo;
    private TouristAgent _dangXem, _canNauCu;
    private float _henQuet, _keoTayLuc = -99f;
    private bool _keo;
    private float _keoX;
    private Coroutine _anim;
    private Canvas _canvas;
    private readonly HashSet<Object> _moiTao = new HashSet<Object>();

    /// <summary>Tool Edit mode: object noi dung (de dang ky Undo).</summary>
    public RectTransform NoiDung => _noiDung;

    /// <summary>Tool Edit mode: dien noi dung mau de canh bo cuc.</summary>
    public void DienMau(Sprite icon, string ten, string phu)
    {
        if (_img != null) { _img.sprite = icon; _img.enabled = icon != null; }
        if (_ten != null) _ten.text = ten;
        if (_phu != null) _phu.text = phu;
    }

    public void Init(KitchenUIv2.KitchenSceneV2UI ui) { _ui = ui; DungKhung(); }

    /// <summary>Dung (hoac nhan lai) cac object con. Goi duoc ca trong Edit mode (tool Kitchen V3/17) -> Sep chinh tay trong Hierarchy.
    /// Object DA CO thi giu nguyen vi tri / co chu / mau Sep chinh, chi dien noi dung luc Play.</summary>
    public void DungKhung()
    {
        if (_noiDung != null) return;
        var chuCu = transform.Find("Txt_Chalk");
        TMP_FontAsset font = null;
        if (chuCu != null) { var t = chuCu.GetComponent<TMP_Text>(); if (t != null) font = t.font; chuCu.gameObject.SetActive(false); }
        var nen = GetComponent<Image>();
        if (nen != null) nen.raycastTarget = true;
        else { nen = gameObject.AddComponent<Image>(); nen.color = new Color(0f, 0f, 0f, 0f); }
        _canvas = GetComponentInParent<Canvas>();

        var ex = transform.Find("DonKhach_NoiDung") as RectTransform;
        if (ex != null) _noiDung = ex;
        else
        {
            var go = new GameObject("DonKhach_NoiDung", typeof(RectTransform));
            _noiDung = (RectTransform)go.transform;
            _noiDung.SetParent(transform, false);
            _noiDung.anchorMin = Vector2.zero; _noiDung.anchorMax = Vector2.one;
            _noiDung.offsetMin = new Vector2(24f, 22f); _noiDung.offsetMax = new Vector2(-24f, -60f);   // chua cho tieu de TODAY'S SPECIAL
        }

        _img = TimHoacTao<Image>("Img_MonKhach", r =>
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.62f);
            r.sizeDelta = new Vector2(coIcon, coIcon); r.anchoredPosition = lechIcon;
        });
        _img.preserveAspect = true; _img.raycastTarget = false;
        _ten = TimHoacTaoChu("Txt_TenMonKhach", font, coChuTen, FontStyles.Bold, r =>
        {
            r.anchorMin = new Vector2(0f, 0.12f); r.anchorMax = new Vector2(1f, 0.30f);
            r.offsetMin = new Vector2(30f, 0f); r.offsetMax = new Vector2(-30f, 0f);
        });
        _phu = TimHoacTaoChu("Txt_ThuTuKhach", font, coChuPhu, FontStyles.Normal, r =>
        {
            r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 0.12f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        });
        _trai = TimHoacTaoChu("Txt_MuiTrai", font, 40f, FontStyles.Bold, r =>
        {
            r.anchorMin = new Vector2(0f, 0.35f); r.anchorMax = new Vector2(0.12f, 0.85f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        });
        _phai = TimHoacTaoChu("Txt_MuiPhai", font, 40f, FontStyles.Bold, r =>
        {
            r.anchorMin = new Vector2(0.88f, 0.35f); r.anchorMax = new Vector2(1f, 0.85f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        });
        // chi dat mac dinh cho object VUA TAO; object Sep da chinh trong Hierarchy thi giu nguyen
        if (_moiTao.Contains(_trai)) { _trai.text = "<"; _trai.color = new Color(mauChu.r, mauChu.g, mauChu.b, 0.45f); }
        if (_moiTao.Contains(_phai)) { _phai.text = ">"; _phai.color = new Color(mauChu.r, mauChu.g, mauChu.b, 0.45f); }
        if (_moiTao.Contains(_ten)) { _ten.enableAutoSizing = true; _ten.fontSizeMin = 14f; _ten.fontSizeMax = coChuTen; }
        _moiTao.Clear();
        _henQuet = 0f;
    }

    private T TimHoacTao<T>(string ten, System.Action<RectTransform> datCho) where T : Component
    {
        var t = _noiDung.Find(ten) as RectTransform;
        if (t == null)
        {
            var go = new GameObject(ten, typeof(RectTransform), typeof(T));
            t = (RectTransform)go.transform;
            t.SetParent(_noiDung, false);
            datCho(t);
        }
        var c = t.GetComponent<T>();
        return c != null ? c : t.gameObject.AddComponent<T>();
    }

    private TMP_Text TimHoacTaoChu(string ten, TMP_FontAsset font, float co, FontStyles kieu, System.Action<RectTransform> datCho)
    {
        bool moi = _noiDung.Find(ten) == null;
        var t = TimHoacTao<TextMeshProUGUI>(ten, datCho);
        if (moi)
        {
            _moiTao.Add(t);
            if (font != null) t.font = font;
            t.fontSize = co; t.fontStyle = kieu;
            t.alignment = TextAlignmentOptions.Center;
            t.color = mauChu;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }
        t.raycastTarget = false;
        return t;
    }

    // =====================================================================
    private void Update()
    {
        if (_noiDung == null) return;
        if (Time.unscaledTime < _henQuet) return;
        _henQuet = Time.unscaledTime + 0.5f;
        Quet();
    }

    private void Quet()
    {
        _ds.Clear();
        var tm = TouristVisitorManager.Instance;
        if (tm != null) tm.LayKhachDangCho(_ds);

        // Khach can nau tiep doi (vua nau xong mon truoc) -> tu luot toi, tru khi vua keo tay
        var canNau = tm != null ? tm.GetFrontWaitingTourist() : null;
        if (canNau != _canNauCu)
        {
            _canNauCu = canNau;
            if (canNau != null && Time.unscaledTime - _keoTayLuc > 4f && canNau != _dangXem)
            {
                int i = _ds.IndexOf(canNau);
                if (i >= 0) { int huong = i > _chiSo ? 1 : -1; _chiSo = i; _dangXem = canNau; Ve(); ChayLuot(huong); return; }
            }
        }

        int k = _dangXem != null ? _ds.IndexOf(_dangXem) : -1;
        if (k >= 0) _chiSo = k;
        else { _chiSo = Mathf.Clamp(_chiSo, 0, Mathf.Max(0, _ds.Count - 1)); _dangXem = _ds.Count > 0 ? _ds[_chiSo] : null; }
        Ve();
    }

    private void Ve()
    {
        int n = _ds.Count;
        bool co = n > 0 && _dangXem != null && _dangXem.Dish != null;
        _img.enabled = co && _dangXem.Dish.dishSprite != null;
        if (co) _img.sprite = _dangXem.Dish.dishSprite;
        _ten.text = co ? Loc.T(_dangXem.Dish.dishName) : Loc.T("Chưa có khách chờ");
        bool daCo = co && DaCoSan(_chiSo);
        _phu.text = co ? (n > 1 ? $"{_chiSo + 1}/{n}" : "") + (daCo ? "  Ready" : "") : "";
        _phu.color = daCo ? mauDaCo : mauChu;
        bool nhieu = n > 1;
        if (_trai.gameObject.activeSelf != nhieu) { _trai.gameObject.SetActive(nhieu); _phai.gameObject.SetActive(nhieu); }
    }

    /// <summary>Mon cua khach thu i da co san (kho + tren dia), tru dan cho cac khach dung truoc cung mon.</summary>
    private bool DaCoSan(int idx)
    {
        var kho = FarmInventoryManager.Instance;
        var con = new Dictionary<string, int>();
        for (int i = 0; i <= idx && i < _ds.Count; i++)
        {
            var a = _ds[i];
            if (a == null || a.Dish == null || string.IsNullOrEmpty(a.Dish.dishId)) continue;
            string id = a.Dish.dishId;
            if (!con.TryGetValue(id, out int n))
            {
                n = kho != null ? kho.GetAmount(id) : 0;
                if (TouristVisitorManager.MonDangTrenDia == id) n++;
            }
            bool du = n > 0;
            con[id] = du ? n - 1 : 0;
            if (i == idx) return du;
        }
        return false;
    }

    // =====================================================================
    //  Keo / cham
    // =====================================================================
    public void OnBeginDrag(PointerEventData e) { _keo = true; _keoX = 0f; }

    public void OnDrag(PointerEventData e)
    {
        if (!_keo) return;
        float k = _canvas != null && _canvas.scaleFactor > 0.01f ? _canvas.scaleFactor : 1f;
        _keoX += e.delta.x / k;
        _noiDung.anchoredPosition = new Vector2(Mathf.Clamp(_keoX * 0.6f, -80f, 80f), 0f);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!_keo) return;
        _keo = false;
        _keoTayLuc = Time.unscaledTime;
        if (_ds.Count > 1 && Mathf.Abs(_keoX) >= nguongKeo)
        {
            int huong = _keoX > 0f ? 1 : -1;                       // keo trai -> phai = khach ke tiep
            _chiSo = ((_chiSo + huong) % _ds.Count + _ds.Count) % _ds.Count;
            _dangXem = _ds[_chiSo];
            Ve();
            ChayLuot(huong);
            AudioManager.Instance?.PlayCardPick();
        }
        else ChayLuot(0);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.dragging || Mathf.Abs(_keoX) > 10f) { _keoX = 0f; return; }
        if (_dangXem == null || _dangXem.Dish == null || _ui == null) return;
        _ui.ChonMonKhach(_dangXem.Dish);
        AudioManager.Instance?.PlayButton();
        if (_img != null) JuicyPulseFX.Play(_img.rectTransform, 1.15f, 0.18f);
    }

    private void ChayLuot(int huong)
    {
        if (_anim != null) StopCoroutine(_anim);
        if (isActiveAndEnabled) _anim = StartCoroutine(CoLuot(huong));
    }

    private System.Collections.IEnumerator CoLuot(int huong)
    {
        Vector2 tu = huong == 0 ? _noiDung.anchoredPosition : new Vector2(-90f * huong, 0f);
        float t = 0f, T = 0.22f;
        while (t < T)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / T), e = 1f - (1f - k) * (1f - k);
            _noiDung.anchoredPosition = Vector2.Lerp(tu, Vector2.zero, e);
            yield return null;
        }
        _noiDung.anchoredPosition = Vector2.zero;
        _anim = null;
    }
}
