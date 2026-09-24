using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh kho kieu Township: khi nhan BAT KY vat pham nao (moi nguon di qua FarmInventoryManager.AddItem)
/// thanh [icon kho | fill bar "used/cap"] truot xuong tu mep tren, nay nhe + "+N" bay len,
/// roi TU AN sau idleBeforeHide giay khong co gi moi.
///
/// [2026-09-23] VIET LAI GON:
///   • Dung 3 thanh phan: 1 khung (Panel), 1 icon (Img_Icon), 1 fill bar (Bar_Track/Bar_Fill/Txt_Count).
///     Cac lop cu chong len nhau (Img_BarnHouse 145px, Badge_Header, Progress_Container, Img_CrateBadge)
///     bi tat — tool "Tools/Kho/Dung thanh kho (toast) gon" xoa han khoi Hierarchy.
///   • Hien/an chay bang Update (khong coroutine) => khong bao gio bi "ket" tren man hinh.
///   • Fill bar dung anchor (khong can sprite Filled) => luon chay dung.
///   • boCucGon = true (tool danh dau) => code KHONG dat lai vi tri/kich thuoc: Sep chinh tay trong
///     Hierarchy bao nhieu thi Play giu nguyen bay nhieu.
/// </summary>
public class WarehouseGainToastUI : MonoBehaviour
{
    public static WarehouseGainToastUI Instance { get; private set; }

    public RectTransform PanelRect
    {
        get
        {
            EnsureBuilt();
            return _panel;
        }
    }

    // [2026-09-24] DICH BAY CHUNG cho moi vat pham vao kho (thu hoach, mua cho, tau, may, thuong):
    // icon kho TREN thanh toast. Goi HienNgay() truoc khi bay de thanh da hien san cho icon bay vao.
    public RectTransform IconRect
    {
        get
        {
            if (!EnsureBuilt()) return null;
            return _imgIcon != null ? _imgIcon.rectTransform : _panel;
        }
    }

    public void HienNgay()
    {
        if (!EnsureBuilt()) return;
        Show();
    }

    [Header("[2026-09-24] Uu tien hien tren cung")]
    [Tooltip("Thanh kho ve TREN cac UI khac (sort order rieng). 0 = theo canvas cha nhu cu.")]
    [SerializeField] private int thuTuVeTren = 440;

    [Header("Wiring (thieu thi tu tim)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Sprite panelSprite;       // khung (mac dinh Resources/UI/Standard/WoodBoard_Frame)
    [SerializeField] private Sprite iconSprite;        // icon kho
    [SerializeField] private Sprite headerBadgeSprite; // (cu — khong dung nua, giu de khong mat tham chieu)
    [SerializeField] private Sprite crateBadgeSprite;  // (cu — khong dung nua)
    [SerializeField] private Sprite barTrackSprite;    // ranh bar (mac dinh Resources/UI/Standard/inner_panel)
    [SerializeField] private Sprite barFillSprite;     // fill bar (mac dinh = ranh, to mau xanh)

    [Header("Layout")]
    [Tooltip("Vi tri thanh so voi mep TREN-GIUA canvas (chi dung khi boCucGon = false)")]
    [SerializeField] private Vector2 anchoredPos = new Vector2(0f, -36f);
    [SerializeField] private Vector2 panelSize   = new Vector2(330f, 84f);
    [Tooltip("Da dung bo cuc gon (tool danh dau). TRUE => code khong dat lai vi tri/kich thuoc, giu chinh tay.")]
    [SerializeField] private bool boCucGon = false;

    [Header("Timing")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float idleBeforeHide = 2.2f;

    // ─── Runtime refs ─────────────────────────────────────────────
    private RectTransform _panel, _fillRt;
    private Image _imgPanel, _imgIcon, _imgTrack, _imgFill;
    private TMP_Text _txtCount;
    private CanvasGroup _cg;
    private float _hideAt, _alpha, _shownFill, _targetFill;
    private int _currentDisplayUsed = -1;
    private bool _visible;
    private Vector2 _posShown;
    private float _flashT = -1f;
    private Color _panelColor = Color.white;

    /// <summary>Luoi an toan: scene chua co toast thi tu sinh.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include) != null) return;
        var cv = FindFirstObjectByType<Canvas>();
        if (cv == null) return;
        var go = new GameObject("WarehouseGainToast", typeof(RectTransform));
        go.transform.SetParent(cv.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<WarehouseGainToastUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Dung + AN ngay khi vao game (neu Sep de Panel dang bat trong Hierarchy de chinh, Play khong bi ket hien).
    private void Start() { EnsureBuilt(); }

    private void OnEnable()
    {
        FarmInventoryManager.OnItemAddedFx           += HandleItemAdded;
        FarmInventoryManager.OnAddRejectedByCapacity += HandleRejected;
    }

    private void OnDisable()
    {
        FarmInventoryManager.OnItemAddedFx           -= HandleItemAdded;
        FarmInventoryManager.OnAddRejectedByCapacity -= HandleRejected;
    }

    // ─── Hien / an + fill: tat ca trong Update, khong the ket ─────
    private void Update()
    {
        if (_panel == null) return;
        float dt = Time.unscaledDeltaTime;

        if (_visible && Time.unscaledTime >= _hideAt) _visible = false;

        if (!_panel.gameObject.activeSelf)
        {
            if (!_visible) return;
            _panel.gameObject.SetActive(true);
        }

        _alpha = Mathf.MoveTowards(_alpha, _visible ? 1f : 0f, dt / Mathf.Max(0.05f, showDuration));
        float k = _visible ? 1f - (1f - _alpha) * (1f - _alpha) : _alpha * _alpha;
        if (_cg != null) _cg.alpha = _alpha;
        _panel.anchoredPosition = Vector2.Lerp(_posShown + new Vector2(0f, 70f), _posShown, k);

        // fill chay muot toi dich
        if (!Mathf.Approximately(_shownFill, _targetFill))
        {
            _shownFill = Mathf.MoveTowards(_shownFill, _targetFill, dt * 2.5f);
            ApFill(_shownFill);
        }

        // nhay do khi kho day
        if (_flashT >= 0f && _imgPanel != null)
        {
            _flashT += dt;
            float t = Mathf.Clamp01(_flashT / 0.5f);
            _imgPanel.color = Color.Lerp(new Color(1f, 0.72f, 0.68f, _panelColor.a), _panelColor, t);
            if (t >= 1f) _flashT = -1f;
        }

        if (!_visible && _alpha <= 0f)
        {
            _panel.anchoredPosition = _posShown;
            _panel.gameObject.SetActive(false);
        }
    }

    // ─── Su kien ─────────────────────────────────────────────────

    public void OnHarvestItemArrived(Sprite icon = null)
    {
        if (!EnsureBuilt()) return;
        Show();
        var inv = FarmInventoryManager.Instance;
        int cap = inv != null ? Mathf.Max(1, inv.SlotCapacity) : 50;
        int actual = inv != null ? inv.UsedSlots : 0;
        if (_currentDisplayUsed < 0) _currentDisplayUsed = Mathf.Max(0, actual - 1);
        _currentDisplayUsed = Mathf.Min(_currentDisplayUsed + 1, actual);
        UpdateDisplayValues(_currentDisplayUsed, cap, animate: true);
        SpawnPlusText("+1", new Color(0.30f, 0.62f, 0.12f));
        JuicyPulseFX.Play(_panel, 1.12f, 0.2f);
        if (_imgIcon != null) JuicyPulseFX.Play(_imgIcon.rectTransform, 1.35f, 0.18f);   // icon kho nhun khi vat pham bay vao
    }

    private void HandleItemAdded(string itemId, int amount)
    {
        if (!EnsureBuilt()) return;
        Show();
        RefreshNumbers(true);
        SpawnPlusText("+" + amount, new Color(0.30f, 0.62f, 0.12f));
        JuicyPulseFX.Play(_panel, 1.12f, 0.2f);
    }

    private void HandleRejected(string itemId)
    {
        if (!EnsureBuilt()) return;
        Show();
        RefreshNumbers(false);
        SpawnPlusText("STORAGE FULL!", new Color(0.86f, 0.22f, 0.16f));
        _flashT = 0f;
    }

    private void Show()
    {
        _hideAt = Time.unscaledTime + idleBeforeHide;
        _visible = true;
        if (!_panel.gameObject.activeSelf) _panel.gameObject.SetActive(true);
    }

    // ─── So / fill ───────────────────────────────────────────────

    public void UpdateDisplayValues(int used, int cap, bool animate)
    {
        _currentDisplayUsed = used;
        cap = Mathf.Max(1, cap);
        if (_txtCount != null)
        {
            _txtCount.text = used + "/" + cap;
            _txtCount.color = Color.white;
        }
        _targetFill = Mathf.Clamp01((float)used / cap);
        if (_imgFill != null)
            _imgFill.color = _targetFill >= 1f ? new Color(0.90f, 0.20f, 0.18f)
                           : _targetFill >= 0.8f ? new Color(0.98f, 0.58f, 0.12f)
                           : new Color(0.38f, 0.74f, 0.18f);
        if (!animate) { _shownFill = _targetFill; ApFill(_shownFill); }
    }

    private void ApFill(float v)
    {
        if (_fillRt == null) return;
        _fillRt.anchorMin = new Vector2(0f, 0f);
        _fillRt.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
        _fillRt.gameObject.SetActive(v > 0.001f);
    }

    private void RefreshNumbers(bool animate)
    {
        var inv = FarmInventoryManager.Instance;
        if (inv == null) return;
        UpdateDisplayValues(inv.UsedSlots, Mathf.Max(1, inv.SlotCapacity), animate);
    }

    private void SpawnPlusText(string text, Color color)
    {
        if (_panel == null || !_panel.gameObject.activeInHierarchy) return;
        var go = new GameObject("Txt_Plus", typeof(RectTransform));
        go.transform.SetParent(_panel, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        var f = GetViFont(); if (f != null) txt.font = f;
        txt.text = text; txt.fontSize = 24; txt.fontStyle = FontStyles.Bold;
        txt.color = color; txt.alignment = TextAlignmentOptions.Center; txt.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.75f, 1f);
        rt.anchoredPosition = new Vector2(0f, 4f);
        rt.sizeDelta = new Vector2(180f, 32f);
        go.AddComponent<ToastPlusFloat>();
    }

    /// <summary>Chu "+N" bay len roi mo dan (Update rieng, tu huy).</summary>
    private class ToastPlusFloat : MonoBehaviour
    {
        private float _t; private Vector2 _from; private TMP_Text _txt; private RectTransform _rt;
        private void Start() { _rt = (RectTransform)transform; _from = _rt.anchoredPosition; _txt = GetComponent<TMP_Text>(); }
        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / 0.85f);
            if (_rt != null) _rt.anchoredPosition = _from + new Vector2(0f, 40f * (1f - (1f - k) * (1f - k)));
            if (_txt != null) _txt.alpha = k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f;
            if (k >= 1f) Destroy(gameObject);
        }
    }

    // ─── Dung / noi hierarchy (idempotent; tool Editor goi duoc) ──

    /// <summary>Dung thanh gon. apDungBoCuc = true thi dat lai vi tri/kich thuoc chuan (tool dung).</summary>
    public bool EnsureBuilt() => EnsureBuilt(false);

    public bool EnsureBuilt(bool apDungBoCuc)
    {
        if (_panel != null && !apDungBoCuc) return true;

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return false;
        }
        if (panelSprite == null) panelSprite = Resources.Load<Sprite>("UI/Standard/WoodBoard_Frame");
        if (barTrackSprite == null) barTrackSprite = Resources.Load<Sprite>("UI/Standard/inner_panel");
        if (barFillSprite == null) barFillSprite = barTrackSprite;

        bool datBoCuc = apDungBoCuc || !boCucGon;

        // 1. KHUNG
        var panelTr = transform.Find("Panel_WarehouseToast") as RectTransform;
        bool moi = panelTr == null;
        if (moi)
        {
            var go = new GameObject("Panel_WarehouseToast", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            panelTr = (RectTransform)go.transform;
        }
        _panel = panelTr;
        if (datBoCuc || moi)
        {
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f);
            _panel.pivot = new Vector2(0.5f, 1f);
            _panel.anchoredPosition = anchoredPos;
            _panel.sizeDelta = panelSize;
            _panel.localScale = Vector3.one;
        }
        _posShown = _panel.anchoredPosition;

        _imgPanel = _panel.GetComponent<Image>(); if (_imgPanel == null) _imgPanel = _panel.gameObject.AddComponent<Image>();
        if (datBoCuc || moi)
        {
            _imgPanel.sprite = panelSprite;
            _imgPanel.type = panelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _imgPanel.pixelsPerUnitMultiplier = 2.2f;   // vien go mong vua thanh cao 84
            _imgPanel.preserveAspect = false;
            _imgPanel.color = panelSprite != null ? Color.white : new Color(0.96f, 0.90f, 0.78f, 0.96f);
        }
        _imgPanel.raycastTarget = false;
        _panelColor = _imgPanel.color;
        // [2026-09-24] Vong sang CAM quanh thanh: UIGlowPulseFX tao 'FX_GlowHalo' ngay trong Awake
        // (ke ca khi component bi tat) -> go han component + vong sang.
        var glow = _panel.GetComponent<UIGlowPulseFX>();
        if (glow != null)
        {
            glow.Stop();
            if (Application.isPlaying) Destroy(glow); else DestroyImmediate(glow);
        }
        var halo = _panel.Find("FX_GlowHalo");
        if (halo != null) { if (Application.isPlaying) Destroy(halo.gameObject); else DestroyImmediate(halo.gameObject); }

        _cg = _panel.GetComponent<CanvasGroup>(); if (_cg == null) _cg = _panel.gameObject.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = false; _cg.interactable = false;
        if (thuTuVeTren > 0 && Application.isPlaying)
        {
            var cvTren = _panel.GetComponent<Canvas>();
            if (cvTren == null) cvTren = _panel.gameObject.AddComponent<Canvas>();
            cvTren.overrideSorting = true;
            cvTren.sortingOrder = thuTuVeTren;
        }

        // Tat cac lop cu chong len nhau (tool Editor xoa han)
        foreach (var ten in new[] { "Img_BarnHouse", "Badge_Header", "Progress_Container", "Img_CrateBadge" })
        {
            var x = _panel.Find(ten);
            if (x != null && x.gameObject.activeSelf) x.gameObject.SetActive(false);
        }

        // 2. ICON
        var iconTr = _panel.Find("Img_Icon") as RectTransform;
        bool moiIcon = iconTr == null;
        if (moiIcon) iconTr = (RectTransform)TaoCon(_panel, "Img_Icon");
        _imgIcon = iconTr.GetComponent<Image>(); if (_imgIcon == null) _imgIcon = iconTr.gameObject.AddComponent<Image>();
        if (_imgIcon.sprite == null && iconSprite != null) _imgIcon.sprite = iconSprite;
        _imgIcon.preserveAspect = true; _imgIcon.raycastTarget = false; _imgIcon.color = Color.white;
        if (datBoCuc || moiIcon)
        {
            iconTr.anchorMin = iconTr.anchorMax = new Vector2(0f, 0.5f);
            iconTr.pivot = new Vector2(0.5f, 0.5f);
            iconTr.anchoredPosition = new Vector2(48f, 2f);
            iconTr.sizeDelta = new Vector2(66f, 66f);
            iconTr.localScale = Vector3.one;
        }

        // 3. FILL BAR (Bar_Track > Bar_Fill + Txt_Count)
        var trackTr = _panel.Find("Bar_Track") as RectTransform;
        bool moiTrack = trackTr == null;
        if (moiTrack) trackTr = (RectTransform)TaoCon(_panel, "Bar_Track");
        if (trackTr.parent != _panel) trackTr.SetParent(_panel, false);
        _imgTrack = trackTr.GetComponent<Image>(); if (_imgTrack == null) _imgTrack = trackTr.gameObject.AddComponent<Image>();
        if (datBoCuc || moiTrack)
        {
            _imgTrack.sprite = barTrackSprite;
            _imgTrack.type = barTrackSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            _imgTrack.color = new Color(0.30f, 0.18f, 0.08f, 1f);
            trackTr.anchorMin = trackTr.anchorMax = new Vector2(0f, 0.5f);
            trackTr.pivot = new Vector2(0f, 0.5f);
            trackTr.anchoredPosition = new Vector2(90f, 0f);
            trackTr.sizeDelta = new Vector2(panelSize.x - 90f - 22f, 32f);
            trackTr.localScale = Vector3.one;
        }
        _imgTrack.raycastTarget = false;

        var fillTr = trackTr.Find("Bar_Fill") as RectTransform;
        bool moiFill = fillTr == null;
        if (moiFill) fillTr = (RectTransform)TaoCon(trackTr, "Bar_Fill");
        _fillRt = fillTr;
        _imgFill = fillTr.GetComponent<Image>(); if (_imgFill == null) _imgFill = fillTr.gameObject.AddComponent<Image>();
        _imgFill.sprite = barFillSprite;
        _imgFill.type = barFillSprite != null ? Image.Type.Sliced : Image.Type.Simple;   // fill bang anchor, khong can Filled
        _imgFill.raycastTarget = false;
        fillTr.pivot = new Vector2(0f, 0.5f);
        fillTr.offsetMin = new Vector2(3f, 3f);
        fillTr.offsetMax = new Vector2(-3f, -3f);
        fillTr.SetAsFirstSibling();

        var txtTr = trackTr.Find("Txt_Count") as RectTransform;
        if (txtTr == null) txtTr = (RectTransform)TaoCon(trackTr, "Txt_Count");
        _txtCount = txtTr.GetComponent<TMP_Text>();
        if (_txtCount == null) _txtCount = txtTr.gameObject.AddComponent<TextMeshProUGUI>();
        var vf = GetViFont(); if (vf != null && _txtCount.font != vf) _txtCount.font = vf;
        if (datBoCuc)
        {
            _txtCount.fontSize = 20; _txtCount.enableAutoSizing = false;
            _txtCount.fontStyle = FontStyles.Bold;
            _txtCount.alignment = TextAlignmentOptions.Center;
            txtTr.anchorMin = Vector2.zero; txtTr.anchorMax = Vector2.one;
            txtTr.offsetMin = Vector2.zero; txtTr.offsetMax = Vector2.zero;
        }
        _txtCount.raycastTarget = false;
        txtTr.SetAsLastSibling();

        if (apDungBoCuc) boCucGon = true;

        RefreshNumbers(false);
        if (Application.isPlaying)
        {
            _alpha = 0f; _cg.alpha = 0f; _visible = false;
            _panel.gameObject.SetActive(false);
        }
        return true;
    }

    private static TMP_FontAsset _viFont;
    private TMP_FontAsset GetViFont()
    {
        if (_viFont != null) return _viFont;
        try { var f = SkinKit.FontVo; if (f != null) return _viFont = f; } catch { }
        if (canvas == null) return null;
        foreach (var t in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t != null && t != _txtCount && t.font != null) return _viFont = t.font;
        return null;
    }

    private static Transform TaoCon(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.transform;
    }
}
