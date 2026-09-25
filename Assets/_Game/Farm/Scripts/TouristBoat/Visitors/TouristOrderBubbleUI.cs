// ============================================================================
//  TouristOrderBubbleUI — BUBBLE DON HANG TO cua khach du lich (1 prefab dung chung moi khach)
// ----------------------------------------------------------------------------
//  Cham vao khach dang doi mon -> bubble to hien TREN DAU khach (bam theo khi camera keo/zoom):
//    Anh mon + ten mon + "Trong kho: n"
//    Thuong khach tra: vang + EXP (tinh dung TouristRewardCalculator nhu luc giao that)
//    Nguyen lieu can de nau (icon + ten)
//    1 nut:  chua co mon -> "NAU NGAY" (vang, sang nhap nhay) -> vao bep, chon san mon nay
//            da co mon   -> "GIAO MON" (xanh, sang nhap nhay) -> giao: bubble vui bay len,
//                           vang + EXP bay vao HUD bang RewardFlyFX co san.
//  Hierarchy (dung bang Tools > Khach du lich > Dung bubble don hang). Code tim con THEO TEN
//  (tim sau trong cay), nen Sep keo tha / doi vi tri / doi co tuy y - Play giu nguyen.
//    TouristOrderBubble (script nay)
//      Bubble_Root            <- code chi dat VI TRI cai nay (bam dau khach)
//        Tail_Dot_0, Tail_Dot_1, Img_Glow
//        Img_Frame
//          Header_Ribbon/Txt_Title, Btn_Close
//          Dish_Box/Img_Dish, Txt_DishName, Txt_Stock
//          Reward_Gold/{Img_Icon,Txt_Value}, Reward_Exp/{Img_Icon,Txt_Value}
//          Txt_NeedTitle, Row_Ingredients/Ing_Slot_0..5/{Img_Icon,Txt_Name}
//          Btn_Action/{Img_BtnGlow, Txt_Action}
//      Fx_Happy (mau, tat san)  <- chu "Ngon qua! Cam on!" bay len khi giao xong
// ============================================================================
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TouristOrderBubbleUI : MonoBehaviour
{
    public static TouristOrderBubbleUI Instance { get; private set; }

    [Header("Bam theo dau khach")]
    [Tooltip("Diem dau khach, tinh theo toa do CUC BO cua khach (nhan theo scale khach). Chan khach = (0,0).")]
    [SerializeField] private Vector3 dauKhachOffset = new Vector3(0f, 175f, 0f);
    [Tooltip("Lech them tren man hinh (don vi canvas) sau khi bam dau khach.")]
    [SerializeField] private Vector2 lechManHinh = new Vector2(0f, 0f);
    [Tooltip("Le toi thieu voi mep man hinh (don vi canvas) - bubble khong bi cat.")]
    [SerializeField] private float leManHinh = 18f;

    [Header("Nut hanh dong")]
    [SerializeField] private Sprite nutNauNgay;     // trong -> Resources/UI/Standard/btn_yellow_3d
    [SerializeField] private Sprite nutGiaoMon;     // trong -> Resources/UI/Standard/btn_green_3d
    [SerializeField] private string chuNauNgay = "Nấu ngay";
    [SerializeField] private string chuGiaoMon = "Giao món";
    [SerializeField] private Color mauSangNau = new Color(1f, 0.86f, 0.35f, 1f);
    [SerializeField] private Color mauSangGiao = new Color(0.55f, 1f, 0.55f, 1f);
    [SerializeField] private Color mauChuNau = new Color(0.36f, 0.18f, 0.05f, 1f);
    [SerializeField] private Color mauChuGiao = Color.white;
    [Tooltip("[2026-09-25] Nut GIAO MON luon dung nut XANH (btn_green_3d) du scene gan sprite khac -> nhin la biet giao duoc.")]
    [SerializeField] private bool nutGiaoLuonXanh = true;

    [Header("Hieu ung")]
    [SerializeField] private float thoiGianMo = 0.30f;
    [SerializeField] private float thoiGianDong = 0.16f;
    [SerializeField] private string chuVuiVe = "Ngon quá! Cảm ơn!";

    // ── Tham chieu (tu tim theo ten) ──
    private RectTransform _root, _frame, _btnActionRt, _btnGlow, _glow, _fxHappy, _rewardGold;
    private CanvasGroup _cg;
    private Image _imgDish, _imgBtn, _imgBtnGlow, _imgGlow;
    private TMP_Text _txtName, _txtStock, _txtGold, _txtExp, _txtAction, _txtTitle, _txtNeedTitle;
    private Button _btnAction, _btnClose;
    private readonly List<Transform> _ingSlots = new List<Transform>();
    private Vector3 _rootScale0 = Vector3.one;

    // ── Trang thai ──
    private TouristAgent _agent;
    private bool _mo, _coMon, _dangDong;
    private int _moFrame = -1;
    private float _hetKiemKho;
    private Coroutine _anim;
    private bool _dangNhan;
    private Vector2 _nhanTai;
    private Camera _cam;
    private Canvas _canvas;

    public bool DangMo => _mo;
    public TouristAgent Khach => _agent;

    // =====================================================================
    //  API tinh
    // =====================================================================

    /// <summary>Mo bubble cho khach. Tra false neu scene chua co bubble (goi fallback giao thang).</summary>
    public static bool Mo(TouristAgent agent)
    {
        var b = Instance;
        if (b == null || !b.isActiveAndEnabled || b._root == null) return false;
        b.Open(agent);
        return true;
    }

    /// <summary>Con tro dang nam tren bubble (dung de chan cham xuyen xuong khach/o dat phia sau).</summary>
    public static bool ConTroTrenBubble()
    {
        var b = Instance;
        if (b == null || !b._mo) return false;
        return b.TrongBubble(b.ViTriConTro());
    }

    // =====================================================================
    //  Vong doi
    // =====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TouristOrderBubble] Co 2 bubble don hang trong scene - chi dung cai dau tien, tat cai nay.", this);
            enabled = false;
            return;
        }
        Instance = this;
        Bind();
        if (_root != null) _root.gameObject.SetActive(false);
        if (_fxHappy != null) _fxHappy.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDisable()
    {
        if (_mo) DongNgay();
    }

    public void Bind()
    {
        _root = Tim<RectTransform>("Bubble_Root");
        if (_root == null) { Debug.LogWarning("[TouristOrderBubble] Thieu 'Bubble_Root' - hay chay Tools > Khach du lich > Dung bubble don hang.", this); return; }
        _rootScale0 = _root.localScale == Vector3.zero ? Vector3.one : _root.localScale;
        _cg = _root.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = _root.gameObject.AddComponent<CanvasGroup>();

        _frame       = Tim<RectTransform>("Img_Frame");
        _glow        = Tim<RectTransform>("Img_Glow");
        _imgGlow     = _glow != null ? _glow.GetComponent<Image>() : null;
        _imgDish     = TimC<Image>("Img_Dish");
        _txtTitle    = TimC<TMP_Text>("Txt_Title");
        _txtName     = TimC<TMP_Text>("Txt_DishName");
        _txtStock    = TimC<TMP_Text>("Txt_Stock");
        _txtNeedTitle= TimC<TMP_Text>("Txt_NeedTitle");
        _rewardGold  = Tim<RectTransform>("Reward_Gold");
        _txtGold     = TimTrong<TMP_Text>("Reward_Gold", "Txt_Value");
        _txtExp      = TimTrong<TMP_Text>("Reward_Exp", "Txt_Value");
        _btnActionRt = Tim<RectTransform>("Btn_Action");
        _btnAction   = _btnActionRt != null ? _btnActionRt.GetComponent<Button>() : null;
        _imgBtn      = _btnActionRt != null ? _btnActionRt.GetComponent<Image>() : null;
        _btnGlow     = Tim<RectTransform>("Img_BtnGlow");
        _imgBtnGlow  = _btnGlow != null ? _btnGlow.GetComponent<Image>() : null;
        _txtAction   = TimC<TMP_Text>("Txt_Action");
        var close    = Tim<RectTransform>("Btn_Close");
        _btnClose    = close != null ? close.GetComponent<Button>() : null;
        _fxHappy     = Tim<RectTransform>("Fx_Happy");

        _ingSlots.Clear();
        var row = Tim<RectTransform>("Row_Ingredients");
        if (row != null)
            for (int i = 0; i < row.childCount; i++)
                if (row.GetChild(i).name.StartsWith("Ing_Slot")) _ingSlots.Add(row.GetChild(i));

        // Sprite mem cho hao quang neu Sep chua gan
        if (_imgGlow != null && _imgGlow.sprite == null) _imgGlow.sprite = SoftFxSprites.GlowSprite;
        if (_imgBtnGlow != null && _imgBtnGlow.sprite == null) _imgBtnGlow.sprite = SoftFxSprites.GlowSprite;

        if (nutNauNgay == null) nutNauNgay = Resources.Load<Sprite>("UI/Standard/btn_yellow_3d");
        if (nutGiaoMon == null) nutGiaoMon = Resources.Load<Sprite>("UI/Standard/btn_green_3d");

        if (_btnAction != null) { _btnAction.onClick.RemoveListener(OnAction); _btnAction.onClick.AddListener(OnAction); }
        if (_btnActionRt != null) UIShineSweep.GanVao(_btnActionRt);   // [VFX 2026-09-24] anh sang luot qua nut
        if (_btnClose  != null) { _btnClose.onClick.RemoveListener(OnClose);   _btnClose.onClick.AddListener(OnClose); }

        // Cac o tren bubble khong chan raycast tru nut + khung (khung chan de bam khong xuyen)
        _canvas = GetComponentInParent<Canvas>();
    }

    // =====================================================================
    //  Mo / dong
    // =====================================================================

    public void Open(TouristAgent agent)
    {
        if (agent == null || _root == null) return;
        if (_mo && agent == _agent && !_dangDong) { OnClose(); return; }   // cham lai dung khach = dong

        _agent = agent;
        _mo = true;
        _dangDong = false;
        _moFrame = Time.frameCount;
        _coDauChan = false;                 // do lai dau chan (ruy-bang tieu de + nut X tho ra ngoai khung)
        FillData();
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        BamDauKhach();
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoMo());
        AudioManager.Instance?.PlayBubblePop();
    }

    private void OnClose()
    {
        if (!_mo || _dangDong) return;
        _dangDong = true;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoDong());
    }

    private void DongNgay()
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
        _mo = false;
        _dangDong = false;
        _agent = null;
        if (_root != null)
        {
            _root.gameObject.SetActive(false);
            _root.localScale = _rootScale0;
        }
        if (_cg != null) _cg.alpha = 1f;
    }

    private IEnumerator CoMo()
    {
        float t = 0f, d = Mathf.Max(0.05f, thoiGianMo);
        if (_cg != null) { _cg.alpha = 0f; _cg.blocksRaycasts = true; _cg.interactable = true; }
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            float s = Mathf.LerpUnclamped(0.55f, 1f, OutBack(k, 1.9f));
            _root.localScale = _rootScale0 * s;
            if (_cg != null) _cg.alpha = Mathf.Clamp01(k * 2.2f);
            yield return null;
        }
        _root.localScale = _rootScale0;
        if (_cg != null) _cg.alpha = 1f;
        _anim = null;
    }

    private IEnumerator CoDong()
    {
        float t = 0f, d = Mathf.Max(0.05f, thoiGianDong);
        if (_cg != null) _cg.blocksRaycasts = false;
        Vector3 s0 = _root.localScale;
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / d);
            _root.localScale = Vector3.LerpUnclamped(s0, _rootScale0 * 0.82f, k * k);
            if (_cg != null) _cg.alpha = 1f - k;
            yield return null;
        }
        _anim = null;
        DongNgay();
    }

    // =====================================================================
    //  Du lieu
    // =====================================================================

    private void FillData()
    {
        DishData dish = _agent != null ? _agent.Dish : null;
        if (dish == null) return;

        if (_txtTitle != null) _txtTitle.text = Loc.T("Khách đặt món");
        if (_imgDish != null) { _imgDish.sprite = dish.dishSprite; _imgDish.enabled = dish.dishSprite != null; _imgDish.preserveAspect = true; }
        if (_txtName != null) _txtName.text = Loc.T(!string.IsNullOrEmpty(dish.dishName) ? dish.dishName : dish.dishId);
        if (_txtNeedTitle != null) _txtNeedTitle.text = Loc.T("Nguyên liệu cần nấu:");

        // Thuong: cung cong thuc luc giao that
        int vang, exp;
        var mgr = TouristVisitorManager.Instance;
        var cfg = mgr != null ? mgr.Config : null;
        bool fb;
        if (cfg != null) { vang = TouristRewardCalculator.ComputeGold(dish, cfg, out fb); exp = TouristRewardCalculator.ComputeExp(dish, cfg); }
        else { vang = TouristRewardCalculator.ComputeGold(dish, 1f, out fb); exp = TouristRewardCalculator.ComputeExp(dish); }
        if (_txtGold != null) _txtGold.text = "+" + Mathf.Max(0, vang);
        if (_txtExp  != null) _txtExp.text  = "+" + Mathf.Max(0, exp) + " EXP";

        // Nguyen lieu
        var ds = dish.requiredIngredients;
        int n = ds != null ? ds.Count : 0;
        for (int i = 0; i < _ingSlots.Count; i++)
        {
            var o = _ingSlots[i];
            IngredientData ing = i < n ? ds[i] : null;
            o.gameObject.SetActive(ing != null);
            if (ing == null) continue;
            var ic = TimCon<Image>(o, "Img_Icon");
            if (ic != null) { ic.sprite = ing.icon; ic.enabled = ing.icon != null; ic.preserveAspect = true; }
            var tx = TimCon<TMP_Text>(o, "Txt_Name");
            if (tx != null) tx.text = Loc.T(!string.IsNullOrEmpty(ing.displayName) ? ing.displayName : ing.id);
        }

        KiemKho(true);
    }

    private Sprite _nutXanh;

    private void KiemKho(bool ep)
    {
        if (!ep && Time.unscaledTime < _hetKiemKho) return;
        _hetKiemKho = Time.unscaledTime + 0.25f;
        DishData dish = _agent != null ? _agent.Dish : null;
        var kho = FarmInventoryManager.Instance;
        int co = (dish != null && kho != null) ? kho.GetAmount(dish.dishId) : 0;
        bool coMon = co > 0;
        if (_txtStock != null) _txtStock.text = Loc.TF("Trong kho: {0}", co);
        if (!ep && coMon == _coMon) return;
        _coMon = coMon;

        if (_imgBtn != null)
        {
            var sp = _coMon ? nutGiaoMon : nutNauNgay;
            if (_coMon && nutGiaoLuonXanh) { if (_nutXanh == null) _nutXanh = Resources.Load<Sprite>("UI/Standard/btn_green_3d"); if (_nutXanh != null) sp = _nutXanh; }
            if (sp != null) _imgBtn.sprite = sp;
            _imgBtn.color = Color.white;
        }
        if (_txtAction != null) { _txtAction.text = Loc.T(_coMon ? chuGiaoMon : chuNauNgay); _txtAction.color = _coMon ? mauChuGiao : mauChuNau; }
    }

    // =====================================================================
    //  Nut
    // =====================================================================

    private void OnAction()
    {
        if (!_mo || _dangDong || _agent == null) return;
        AudioManager.Instance?.PlayButton();
        KiemKho(true);
        DishData dish = _agent.Dish;
        if (dish == null) { OnClose(); return; }

        if (_coMon)
        {
            // Diem bung vang/EXP = cum thuong tren bubble -> chum icon bay tu day len HUD
            Vector2 diem = DiemManHinh(_rewardGold != null ? _rewardGold : _btnActionRt);
            RewardFlyFX.GoiYDiemXuatPhat(diem, 0.8f);
            var agent = _agent;
            var mgr = TouristVisitorManager.Instance;
            if (mgr != null) mgr.DeliverTo(agent);
            if (agent != null && agent.WasServed)
            {
                BayChuVuiVe();
                OnClose();
            }
            else
            {
                RewardFlyFX.XoaGoiYDiemXuatPhat();
                KiemKho(true);
            }
        }
        else
        {
            KitchenOrderRequest.Dat(dish.dishId);
            DongNgay();
            var ui = FarmUIManager.Instance;
            if (ui != null) ui.OnClick_GoCooking();
        }
    }

    // =====================================================================
    //  Update: bam dau khach, tu dong, nhap nhay nut, cham ngoai de dong
    // =====================================================================

    private void LateUpdate()
    {
        if (!_mo) return;

        if (_agent == null || !_agent.gameObject.activeInHierarchy || _agent.WasServed || _agent.WasTimedOut
            || (!_agent.CanReceiveDish && !_agent.IsWaitingBubble) || FarmInputLock.IsCookingMode)
        {
            if (!_dangDong) { if (_agent == null || !_agent.gameObject.activeInHierarchy) DongNgay(); else OnClose(); }
            if (!_mo) return;
        }

        BamDauKhach();
        KiemKho(false);

        // Nut "sang den": hao quang tho nhe + nut phong nhe
        float w = Time.unscaledTime;
        if (_imgBtnGlow != null)
        {
            Color c = _coMon ? mauSangGiao : mauSangNau;
            c.a = 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(w * 4.2f));
            _imgBtnGlow.color = c;
            float gs = 1f + 0.06f * Mathf.Sin(w * 4.2f);
            _btnGlow.localScale = new Vector3(gs, gs, 1f);
        }
        if (_btnActionRt != null && _anim == null)
        {
            float bs = 1f + 0.025f * Mathf.Sin(w * 4.2f + 0.6f);
            _btnActionRt.localScale = new Vector3(bs, bs, 1f);
        }
        if (_imgGlow != null)
        {
            var c = _imgGlow.color;
            c.a = 0.28f + 0.1f * Mathf.Sin(w * 1.6f);
            _imgGlow.color = c;
        }

        XuLyChamNgoai();
    }

    private void XuLyChamNgoai()
    {
        bool nhan, tha;
        Vector2 pos = ViTriConTro(out nhan, out tha);
        if (nhan) { _dangNhan = true; _nhanTai = pos; }
        if (tha && _dangNhan)
        {
            _dangNhan = false;
            if (Time.frameCount == _moFrame) return;              // vua mo o khung nay (cham khach khac)
            if ((pos - _nhanTai).sqrMagnitude > 30f * 30f) return; // dang keo ban do -> khong dong
            if (!TrongBubble(pos)) OnClose();
        }
    }

    private void BamDauKhach()
    {
        if (_agent == null || _root == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;
        var cha = _root.parent as RectTransform;
        if (cha == null) return;

        Vector3 dau = _agent.transform.TransformPoint(dauKhachOffset);
        Vector3 sp = _cam.WorldToScreenPoint(dau);
        if (sp.z < 0f) return;

        Camera uiCam = UiCam();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(cha, sp, uiCam, out Vector2 local)) return;
        local += lechManHinh;

        // Kep trong man hinh
        Rect r = cha.rect;
        Vector2 kt = Vector2.Scale(_root.rect.size, new Vector2(_rootScale0.x, _rootScale0.y));
        Vector2 pv = _root.pivot;
        float minX = r.xMin + kt.x * pv.x + leManHinh, maxX = r.xMax - kt.x * (1f - pv.x) - leManHinh;
        float minY = r.yMin + kt.y * pv.y + leManHinh, maxY = r.yMax - kt.y * (1f - pv.y) - leManHinh;
        // [2026-09-25] Kep theo DAU CHAN THAT (ruy-bang "Tourist Order" + nut X tho ra ngoai khung) -> khong bi mep man hinh cat
        if (!_coDauChan) DoDauChan();
        if (_coDauChan)
        {
            float sx = _rootScale0.x, sy = _rootScale0.y;
            minX = r.xMin - _dcMin.x * sx + leManHinh; maxX = r.xMax - _dcMax.x * sx - leManHinh;
            minY = r.yMin - _dcMin.y * sy + leManHinh; maxY = r.yMax - _dcMax.y * sy - leManHinh;
        }
        if (minX <= maxX) local.x = Mathf.Clamp(local.x, minX, maxX);
        if (minY <= maxY) local.y = Mathf.Clamp(local.y, minY, maxY);

        _root.localPosition = new Vector3(local.x, local.y, 0f);
    }

    private bool _coDauChan;
    private Vector2 _dcMin, _dcMax;
    private static readonly Vector3[] _g4 = new Vector3[4];

    /// <summary>Dau chan cua bubble trong toa do CUC BO cua _root (khong tinh hao quang / hieu ung).</summary>
    private void DoDauChan()
    {
        _coDauChan = false;
        if (_root == null) return;
        Vector2 mn = new Vector2(float.MaxValue, float.MaxValue), mx = new Vector2(float.MinValue, float.MinValue);
        GomDauChan(_root, true, ref mn, ref mx);
        if (mn.x > mx.x || mn.y > mx.y) return;
        _dcMin = mn; _dcMax = mx; _coDauChan = true;
    }

    private void GomDauChan(RectTransform n, bool goc, ref Vector2 mn, ref Vector2 mx)
    {
        if (n == null || (!goc && !n.gameObject.activeSelf)) return;
        string t = n.name;
        if (!goc && (t.StartsWith("Fx") || t.StartsWith("FX") || t.Contains("Glow") || t.Contains("Shine"))) return;
        n.GetWorldCorners(_g4);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = _root.InverseTransformPoint(_g4[i]);
            mn = Vector2.Min(mn, p); mx = Vector2.Max(mx, p);
        }
        if (n.GetComponent<RectMask2D>() != null || n.GetComponent<Mask>() != null) return;
        for (int i = 0; i < n.childCount; i++) GomDauChan(n.GetChild(i) as RectTransform, false, ref mn, ref mx);
    }

    // =====================================================================
    //  Bubble vui ve bay len khi giao xong
    // =====================================================================

    private void BayChuVuiVe()
    {
        if (_root == null) return;
        var cha = _root.parent as RectTransform;
        if (cha == null) return;
        Vector3 goc = _frame != null ? _frame.position : _root.position;
        StartCoroutine(CoVuiVe(cha, goc));
    }

    private IEnumerator CoVuiVe(RectTransform cha, Vector3 viTriTheGioi)
    {
        // 1) Chu "Ngon qua! Cam on!" (clone Fx_Happy de Sep chinh font/mau; thieu thi tu tao)
        RectTransform chu;
        if (_fxHappy != null)
        {
            chu = Instantiate(_fxHappy, cha);
            chu.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("Fx_Happy_Run", typeof(RectTransform));
            chu = (RectTransform)go.transform;
            chu.SetParent(cha, false);
            chu.sizeDelta = new Vector2(520f, 90f);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = 46f; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(1f, 0.93f, 0.55f); t.raycastTarget = false;
            t.outlineWidth = 0f;
        }
        var txt = chu.GetComponentInChildren<TMP_Text>(true);
        if (txt != null) txt.text = Loc.T(chuVuiVe);
        chu.position = viTriTheGioi;
        chu.SetAsLastSibling();
        var cg = chu.GetComponent<CanvasGroup>();
        if (cg == null) cg = chu.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        Vector2 p0 = chu.anchoredPosition;

        // 2) Chum hat lap lanh + tim nho bung ra
        var hat = new List<RectTransform>();
        var huong = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            var g = new GameObject("Fx_HappySpark", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)g.transform;
            rt.SetParent(cha, false);
            rt.position = viTriTheGioi;
            float s = Random.Range(20f, 38f);
            rt.sizeDelta = new Vector2(s, s);
            var im = g.GetComponent<Image>();
            im.sprite = (i % 2 == 0) ? SoftFxSprites.SparkleSprite : SoftFxSprites.CircleSprite;
            im.color = (i % 3 == 0) ? new Color(1f, 0.62f, 0.72f, 1f) : new Color(1f, 0.92f, 0.5f, 1f);
            im.raycastTarget = false;
            float a = (i / 10f) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            huong.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(140f, 230f));
            hat.Add(rt);
        }
        var hat0 = new List<Vector2>();
        foreach (var h in hat) hat0.Add(h.anchoredPosition);

        float d = 1.25f, tt = 0f;
        while (tt < d)
        {
            tt += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(tt / d);
            if (chu != null)
            {
                float sc = k < 0.25f ? Mathf.LerpUnclamped(0.4f, 1f, OutBack(k / 0.25f, 2.2f)) : 1f;
                chu.localScale = new Vector3(sc, sc, 1f);
                chu.anchoredPosition = p0 + new Vector2(Mathf.Sin(k * 9f) * 6f, 190f * OutCubic(k));
                cg.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            }
            for (int i = 0; i < hat.Count; i++)
            {
                if (hat[i] == null) continue;
                float kk = OutCubic(Mathf.Clamp01(k / 0.7f));
                hat[i].anchoredPosition = hat0[i] + huong[i] * kk + new Vector2(0f, -60f * k * k);
                hat[i].localRotation = Quaternion.Euler(0f, 0f, 200f * k * (i % 2 == 0 ? 1f : -1f));
                var im = hat[i].GetComponent<Image>();
                var c = im.color; c.a = 1f - k; im.color = c;
            }
            yield return null;
        }
        if (chu != null) Destroy(chu.gameObject);
        foreach (var h in hat) if (h != null) Destroy(h.gameObject);
    }

    // =====================================================================
    //  Tien ich
    // =====================================================================

    private Camera UiCam()
    {
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return null;
        var root = _canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    private Vector2 DiemManHinh(RectTransform rt)
    {
        if (rt == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        return RectTransformUtility.WorldToScreenPoint(UiCam(), rt.position);
    }

    private bool TrongBubble(Vector2 sp)
    {
        Camera c = UiCam();
        if (_frame != null && RectTransformUtility.RectangleContainsScreenPoint(_frame, sp, c)) return true;
        if (_btnActionRt != null && RectTransformUtility.RectangleContainsScreenPoint(_btnActionRt, sp, c)) return true;
        var close = _btnClose != null ? _btnClose.transform as RectTransform : null;
        if (close != null && RectTransformUtility.RectangleContainsScreenPoint(close, sp, c)) return true;
        return false;
    }

    private Vector2 ViTriConTro() { bool a, b; return ViTriConTro(out a, out b); }

    private Vector2 ViTriConTro(out bool nhan, out bool tha)
    {
#if ENABLE_INPUT_SYSTEM
        var p = UnityEngine.InputSystem.Pointer.current;
        if (p != null)
        {
            nhan = p.press.wasPressedThisFrame;
            tha = p.press.wasReleasedThisFrame;
            return p.position.ReadValue();
        }
        nhan = tha = false;
        return Vector2.zero;
#else
        nhan = Input.GetMouseButtonDown(0);
        tha = Input.GetMouseButtonUp(0);
        return Input.mousePosition;
#endif
    }

    private T Tim<T>(string ten) where T : Component
    {
        var t = TimSau(transform, ten);
        return t != null ? t.GetComponent<T>() : null;
    }

    private T TimC<T>(string ten) where T : Component => Tim<T>(ten);

    private T TimTrong<T>(string cha, string con) where T : Component
    {
        var c = TimSau(transform, cha);
        if (c == null) return null;
        var t = TimSau(c, con);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static T TimCon<T>(Transform cha, string ten) where T : Component
    {
        var t = TimSau(cha, ten);
        return t != null ? t.GetComponent<T>() : null;
    }

    private static Transform TimSau(Transform cha, string ten)
    {
        if (cha == null) return null;
        for (int i = 0; i < cha.childCount; i++)
        {
            var c = cha.GetChild(i);
            if (c.name == ten) return c;
            var s = TimSau(c, ten);
            if (s != null) return s;
        }
        return null;
    }

    private static float OutBack(float t, float c1)
    {
        float c3 = c1 + 1f; float u = t - 1f;
        return 1f + c3 * u * u * u + c1 * u * u;
    }

    private static float OutCubic(float t) { float u = 1f - t; return 1f - u * u * u; }
}
