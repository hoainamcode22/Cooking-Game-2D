using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HỆ THỐNG LIVE PHANTOM DEMO (ẢO ẢNH HƯỚNG DẪN TRỰC QUAN — "như video")
/// ══════════════════════════════════════════════════════════════════════════
/// Hiển thị ảo ảnh bàn tay mờ + icon hạt giống / liềm / kim cương thực hiện thao tác mẫu
/// trực tiếp trên scene để người chơi nhìn mẫu là hiểu ngay cách kéo thả hoặc bấm nút.
///
/// NGUYÊN TẮC (bản này):
///  • Ảo ảnh chạy TRƯỚC, tay thật chạy SAU: trong lúc demo, mọi tay hướng dẫn thật
///    (Tutorial_Hands) bị ẩn bằng CanvasGroup.alpha = 0 (KHÔNG SetActive — script khác đang
///    điều khiển activation), demo xong trả alpha = 1. Xem <see cref="AnTayThat"/>.
///  • Mỗi demo ≤ 3,2s, thời gian unscaled, ease in-out, alpha lớp ảo ảnh ≤ 0,75,
///    tay ảo nhỏ hơn tay thật (scale 0,9), icon ~110px.
///  • Demo xong mà người chơi chưa chạm gì và vẫn đứng cùng bước ⇒ tự lặp lại sau
///    <see cref="_lapLaiSauGiay"/> giây, tối đa <see cref="_soLanLapToiDa"/> lần.
///  • Người chơi chạm màn hình ⇒ demo mờ đi ngay, tay thật hiện lại, không lặp nữa.
///  • Lớp ảo ảnh nằm trong Tutorial_Canvas (fallback Canvas_Popup / canvas bất kỳ), Canvas lồng
///    overrideSorting=450, KHÔNG có GraphicRaycaster, mọi Image raycastTarget=false ⇒ không bao
///    giờ nuốt click của người chơi.
/// </summary>
public class TutorialPhantomDemoManager : MonoBehaviour
{
    private static TutorialPhantomDemoManager _instance;
    public static TutorialPhantomDemoManager Instance => _instance;

    [Header("Phantom UI Setup")]
    [SerializeField] private CanvasGroup _phantomGroup;
    [SerializeField] private RectTransform _phantomHand;
    [SerializeField] private Image _handImage;
    [SerializeField] private RectTransform _phantomItem;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Sprite _defaultHandSprite;
    [SerializeField] private Sprite _sickleSprite;
    [SerializeField] private Sprite _riceSeedSprite;

    [Header("Nhịp lặp demo")]
    [Tooltip("Demo xong mà người chơi chưa làm gì (cùng bước, chưa chạm) thì chạy lại sau bấy nhiêu giây.")]
    [SerializeField] private float _lapLaiSauGiay = 8f;
    [Tooltip("Số lần tự lặp lại tối đa cho một bước.")]
    [SerializeField] private int _soLanLapToiDa = 3;

    // ── Hằng số chuyển động ─────────────────────────────────────────────────
    private const float ALPHA_TOI_DA   = 0.75f;   // alpha lớp ảo ảnh khi hiện rõ nhất
    private const float TY_LE_TAY      = 0.9f;    // tay ảo nhỏ hơn tay thật
    private const float KICH_ICON_PX   = 110f;    // icon hạt / liềm
    private const float KICH_TAY_MAC_DINH = 96f;  // khi không đo được tay thật
    private const float CHO_TARGET_GIAY = 1.5f;   // chờ khay liềm / nút kim cương xuất hiện
    private const string TEN_LOP        = "Tutorial_Phantom_Demo_Layer";
    private const string TEN_TAY_THAT   = "Tutorial_Hands";

    // Đầu ngón tay trên ảnh tutorial_hand (chỉ XUỐNG) — cùng số với TutorialActionHandGuide.
    private static readonly Vector2 PIVOT_DAU_NGON = new Vector2(0.36f, 0.1f);
    private static readonly Vector3 TAY_GOC = Vector3.one * TY_LE_TAY;

    // ── Trạng thái demo ─────────────────────────────────────────────────────
    private enum LoaiDemo { None, Plant, SpeedUp, Harvest }

    private Coroutine _currentDemoCo;
    private Coroutine _lapLaiCo;
    private bool      _isDemoRunning = false;
    public  bool      IsDemoRunning => _isDemoRunning;

    private LoaiDemo _loai = LoaiDemo.None;
    private Sprite   _seedSpriteDemo;
    private string   _idA;            // Plant: khay hạt · SpeedUp: ô · Harvest: ô chín 1
    private string   _idB;            // Plant: ô đích · Harvest: ô chín 2 (null = bỏ)
    private Action   _onDone;
    private bool     _daGoiOnDone;
    private string   _stepLucBatDau;
    private int      _soLanDaLap;
    private bool     _nguoiChoiDaCham;

    // ── Ẩn tay thật ─────────────────────────────────────────────────────────
    private CanvasGroup _tayThatCG;
    private bool        _dangAnTayThat;

    private static readonly Vector3[] _gocBuf = new Vector3[4];

    // =========================================================================
    // Lifecycle
    // =========================================================================
    void Awake()
    {
        _instance = this;
        EnsureUI();
    }

    void OnDisable()
    {
        // Bị tắt giữa demo thì KHÔNG được để tay thật kẹt alpha 0. Unity đã tự giết coroutine,
        // nhưng cờ _isDemoRunning / alpha lớp ảo ảnh (object riêng, không bị tắt theo) vẫn còn
        // ⇒ dùng StopDemo() để dọn trọn: cờ, lịch lặp, alpha lớp ảo ảnh, tay thật.
        StopDemo();
    }

    void OnDestroy()
    {
        AnTayThat(false);
        if (_instance == this) _instance = null;
    }

    void Update()
    {
        // Lưới an toàn: không chạy demo mà tay thật vẫn đang ẩn ⇒ trả lại ngay.
        if (!_isDemoRunning && _dangAnTayThat) AnTayThat(false);

        if (!_isDemoRunning && _lapLaiCo == null) return;

        // Người chơi chạm màn hình để làm thật ⇒ demo mờ đi, tay thật hiện, không lặp nữa.
        bool cham = Input.GetMouseButtonDown(0)
                    || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (cham) DungDoNguoiChoiCham();
    }

    // =========================================================================
    // Dựng UI
    // =========================================================================
    private void EnsureUI()
    {
        if (_phantomGroup != null) return;

        Canvas canvas = TimCanvasChua();
        if (canvas == null) return;

        Transform existing = canvas.transform.Find(TEN_LOP);
        GameObject rootGo;
        if (existing != null)
        {
            rootGo = existing.gameObject;
        }
        else
        {
            rootGo = new GameObject(TEN_LOP, typeof(RectTransform));
            rootGo.transform.SetParent(canvas.transform, false);
            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Canvas ownCanvas = rootGo.GetComponent<Canvas>();
        if (ownCanvas == null) ownCanvas = rootGo.AddComponent<Canvas>();
        ownCanvas.overrideSorting = true;
        // [FIX 2026-09-06 vong4] TRUOC DAY go cung 450. Lop ao anh nam duoi Tutorial_Canvas nen
        // 450 chi du vuot Canvas_Popup (300) — KHONG du vuot khay chuong PenSupplyTrayV2 (800)
        // hay bang tien trinh chuong (500). Ket qua: buoc L2_08 dien canh keo bao thoc ma tay ao
        // chim sau khay, nguoi choi khong thay demo. Nay lay so tu TutorialManager (doc THAT tu
        // order khay roi cong bien an toan), van duoi ghost keo 999 va man chuyen canh 9999.
        ownCanvas.sortingOrder    = TutorialManager.OrderLopAoAnhCanDung;

        // Lớp ảo ảnh KHÔNG được có GraphicRaycaster — nó chỉ để nhìn, không bao giờ nhận click.
        var raycaster = rootGo.GetComponent<GraphicRaycaster>();
        if (raycaster != null) Destroy(raycaster);

        _phantomGroup = rootGo.GetComponent<CanvasGroup>();
        if (_phantomGroup == null) _phantomGroup = rootGo.AddComponent<CanvasGroup>();
        _phantomGroup.alpha          = 0f;
        _phantomGroup.blocksRaycasts = false;
        _phantomGroup.interactable   = false;

        // Item ảo ảnh (hạt giống / liềm) — tạo TRƯỚC tay để tay vẽ đè lên item.
        Transform itemTf = rootGo.transform.Find("Phantom_Item");
        if (itemTf == null)
        {
            var iGo = new GameObject("Phantom_Item", typeof(RectTransform), typeof(Image));
            iGo.transform.SetParent(rootGo.transform, false);
            _phantomItem = iGo.GetComponent<RectTransform>();
            _itemImage   = iGo.GetComponent<Image>();
        }
        else
        {
            _phantomItem = (RectTransform)itemTf;
            _itemImage   = itemTf.GetComponent<Image>();
        }
        _phantomItem.sizeDelta = new Vector2(KICH_ICON_PX, KICH_ICON_PX);
        _phantomItem.pivot     = new Vector2(0.5f, 0.5f);
        if (_itemImage != null)
        {
            _itemImage.raycastTarget  = false;
            _itemImage.preserveAspect = true;
        }

        // Bàn tay ảo ảnh
        Transform handTf = rootGo.transform.Find("Phantom_Hand");
        if (handTf == null)
        {
            var hGo = new GameObject("Phantom_Hand", typeof(RectTransform), typeof(Image));
            hGo.transform.SetParent(rootGo.transform, false);
            _phantomHand = hGo.GetComponent<RectTransform>();
            _handImage   = hGo.GetComponent<Image>();
        }
        else
        {
            _phantomHand = (RectTransform)handTf;
            _handImage   = handTf.GetComponent<Image>();
        }
        _phantomHand.pivot      = PIVOT_DAU_NGON;   // .position = đầu ngón tay
        _phantomHand.localScale = TAY_GOC;
        _phantomHand.SetAsLastSibling();
        if (_handImage != null)
        {
            _handImage.raycastTarget  = false;
            _handImage.preserveAspect = true;
        }

        CapNhatSpriteTay();
    }

    /// <summary>Ưu tiên Tutorial_Canvas (cùng canvas với proxy ô đất) → Canvas_Popup → canvas cha → bất kỳ.</summary>
    private Canvas TimCanvasChua()
    {
        Canvas theoTen = TimCanvasTheoTen("Tutorial_Canvas") ?? TimCanvasTheoTen("Canvas_Popup");
        if (theoTen != null) return theoTen;

        Canvas cha = GetComponentInParent<Canvas>();
        if (cha != null) return cha;

        return FindFirstObjectByType<Canvas>();
    }

    private static Canvas TimCanvasTheoTen(string ten)
    {
        var go = GameObject.Find(ten);
        if (go != null)
        {
            var c = go.GetComponent<Canvas>();
            if (c != null) return c;
        }
        // GameObject.Find bỏ qua object đang tắt — quét thêm cả canvas inactive.
        var all = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == ten) return all[i];
        return null;
    }

    /// <summary>
    /// Sprite + kích cỡ tay ảo lấy từ tay thật (Image nằm ở CON 'Hand_Image', không ở root),
    /// fallback _defaultHandSprite. Gọi lại mỗi lần Play vì lúc Awake TutorialManager có thể chưa có.
    /// </summary>
    private void CapNhatSpriteTay()
    {
        if (_handImage == null || _phantomHand == null) return;

        RectTransform tayThat = TutorialManager.Instance != null ? TutorialManager.Instance.HandPointerRT : null;
        Image anhTayThat = tayThat != null ? tayThat.GetComponentInChildren<Image>(true) : null;

        if (_handImage.sprite == null)
        {
            if (anhTayThat != null && anhTayThat.sprite != null) _handImage.sprite = anhTayThat.sprite;
            else if (_defaultHandSprite != null)                  _handImage.sprite = _defaultHandSprite;
            else
                Debug.LogWarning("[PhantomDemo] Không có sprite bàn tay (tay thật chưa sẵn, _defaultHandSprite trống) — " +
                                 "ảo ảnh sẽ là ô trắng. Gán _defaultHandSprite trên TutorialPhantomDemoManager.");
        }

        // Kích cỡ: theo ảnh tay thật (không có thì 96px). Scale 0,9 làm tay ảo nhỏ hơn.
        Vector2 kich = Vector2.one * KICH_TAY_MAC_DINH;
        if (anhTayThat != null && anhTayThat.rectTransform.rect.width > 1f && anhTayThat.rectTransform.rect.height > 1f)
            kich = anhTayThat.rectTransform.rect.size;
        else if (tayThat != null && tayThat.rect.width > 1f && tayThat.rect.height > 1f)
            kich = tayThat.rect.size;
        _phantomHand.sizeDelta = kich;
    }

    /// <summary>Sprite liềm: khay liềm thật đang có trong scene → _sickleSprite. null = không có.</summary>
    private Sprite LaySpriteLiem()
    {
        var khay = FarmUIManager.Instance != null ? FarmUIManager.Instance.SickleTrayRect : null;
        if (khay != null)
        {
            var img = khay.GetComponentInChildren<Image>(true);
            if (img != null && img.sprite != null) return img.sprite;
        }
        return _sickleSprite;
    }

    /// <summary>Bật lớp ảo ảnh lên và đưa lên trên cùng — chống HideAllPopups tắt anh em / vẽ chìm.</summary>
    private bool ChuanBiLop()
    {
        EnsureUI();
        if (_phantomGroup == null || _phantomHand == null) return false;
        _phantomGroup.gameObject.SetActive(true);
        _phantomGroup.transform.SetAsLastSibling();
        _phantomGroup.alpha = 0f;
        CapNhatSpriteTay();
        return true;
    }

    // =========================================================================
    // Public Demos — chữ ký CŨ giữ nguyên, thêm overload có onDone
    // =========================================================================

    /// <summary>ẢO ẢNH GIEO HẠT: tay + hạt giống lướt từ khay hạt vào ô đất.</summary>
    public void PlayPlantPhantom(Sprite seedSprite, string fromTargetId = "seed_rice", string toPlotId = "tutorial_plot_01")
        => PlayPlantPhantom(seedSprite, fromTargetId, toPlotId, null);

    /// <summary>ẢO ẢNH GIEO HẠT + callback khi demo kết thúc bình thường (hoặc bị người chơi chạm cắt).</summary>
    public void PlayPlantPhantom(Sprite seedSprite, string fromTargetId, string toPlotId, Action onDone)
    {
        BatDauDemo(LoaiDemo.Plant, seedSprite, fromTargetId, toPlotId, onDone);
    }

    /// <summary>ẢO ẢNH TĂNG TỐC: (panel chưa mở) chạm ô → chờ nút kim cương → chạm nút. (đã mở) chạm nút luôn.</summary>
    public void PlaySpeedUpPhantom(string plotId = "tutorial_plot_01")
        => PlaySpeedUpPhantom(plotId, null);

    public void PlaySpeedUpPhantom(string plotId, Action onDone)
    {
        BatDauDemo(LoaiDemo.SpeedUp, null, plotId, null, onDone);
    }

    /// <summary>ẢO ẢNH THU HOẠCH: chạm ô chín → liềm từ khay quét qua ô 1 rồi ô 2 (nextPlotId null = bỏ).</summary>
    public void PlayHarvestPhantom(string startPlotId = "tutorial_plot_01", string nextPlotId = "tutorial_plot_02")
        => PlayHarvestPhantom(startPlotId, nextPlotId, null);

    public void PlayHarvestPhantom(string startPlotId, string nextPlotId, Action onDone)
    {
        BatDauDemo(LoaiDemo.Harvest, null, startPlotId, nextPlotId, onDone);
    }

    /// <summary>
    /// Dừng hẳn demo (đổi bước / ẩn UI tutorial): tắt coroutine, huỷ lặp, trả tay thật.
    /// KHÔNG gọi onDone — bước đã đổi thì callback của bước cũ không còn ý nghĩa.
    /// </summary>
    public void StopDemo()
    {
        HuyLapLai();
        _isDemoRunning = false;
        _loai = LoaiDemo.None;
        _onDone = null;
        _daGoiOnDone = true;
        if (_currentDemoCo != null)
        {
            StopCoroutine(_currentDemoCo);
            _currentDemoCo = null;
        }
        if (_phantomGroup != null) _phantomGroup.alpha = 0f;
        AnTayThat(false);
    }

    // =========================================================================
    // Điều phối: bắt đầu / kết thúc / lặp / cắt do người chơi chạm
    // =========================================================================
    private void BatDauDemo(LoaiDemo loai, Sprite seed, string idA, string idB, Action onDone)
    {
        StopDemo();

        if (!ChuanBiLop())
        {
            // Không có canvas / UI để vẽ ⇒ coi như demo xong ngay, tay thật cứ chạy.
            onDone?.Invoke();
            return;
        }

        _loai            = loai;
        _seedSpriteDemo  = seed;
        _idA             = idA;
        _idB             = idB;
        _onDone          = onDone;
        _daGoiOnDone     = false;
        _soLanDaLap      = 0;
        _nguoiChoiDaCham = false;
        _stepLucBatDau   = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStepName : null;

        ChayMotLuot();
    }

    private void ChayMotLuot()
    {
        if (_phantomGroup == null) return;
        _phantomGroup.gameObject.SetActive(true);
        _phantomGroup.transform.SetAsLastSibling();
        _isDemoRunning = true;
        AnTayThat(true);
        _currentDemoCo = StartCoroutine(ChayDemo());
    }

    private IEnumerator ChayDemo()
    {
        switch (_loai)
        {
            case LoaiDemo.Plant:   yield return PlantRoutine(_seedSpriteDemo, _idA, _idB); break;
            case LoaiDemo.SpeedUp: yield return SpeedUpRoutine(_idA);                     break;
            case LoaiDemo.Harvest: yield return HarvestRoutine(_idA, _idB);                break;
            default: break;
        }
        KetThucBinhThuong();
    }

    /// <summary>Demo chạy hết một lượt: trả tay thật, gọi onDone (1 lần), hẹn lặp lại.</summary>
    private void KetThucBinhThuong()
    {
        _currentDemoCo = null;
        _isDemoRunning = false;
        if (_phantomGroup != null) _phantomGroup.alpha = 0f;
        AnTayThat(false);
        GoiOnDoneMotLan();

        if (_loai != LoaiDemo.None && !_nguoiChoiDaCham && _soLanDaLap < _soLanLapToiDa && _lapLaiSauGiay > 0f)
        {
            HuyLapLai();
            _lapLaiCo = StartCoroutine(LapLaiSau());
        }
    }

    private IEnumerator LapLaiSau()
    {
        yield return new WaitForSecondsRealtime(_lapLaiSauGiay);
        _lapLaiCo = null;

        if (_isDemoRunning || _loai == LoaiDemo.None || _nguoiChoiDaCham) yield break;

        string buocHienTai = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStepName : null;
        if (buocHienTai != _stepLucBatDau)
        {
            // Đã sang bước khác mà chưa ai gọi StopDemo — dọn cho sạch.
            _loai = LoaiDemo.None;
            yield break;
        }

        _soLanDaLap++;
        Debug.Log($"[PhantomDemo] Lặp lại demo '{_loai}' lần {_soLanDaLap}/{_soLanLapToiDa} (bước '{buocHienTai}').");
        ChayMotLuot();
    }

    private void HuyLapLai()
    {
        if (_lapLaiCo != null)
        {
            StopCoroutine(_lapLaiCo);
            _lapLaiCo = null;
        }
    }

    /// <summary>Người chơi chạm để làm thật: mờ demo, dừng coroutine, trả tay thật NGAY, không lặp nữa, onDone 1 lần.</summary>
    private void DungDoNguoiChoiCham()
    {
        _nguoiChoiDaCham = true;
        HuyLapLai();

        if (!_isDemoRunning) return;

        _isDemoRunning = false;
        if (_currentDemoCo != null)
        {
            StopCoroutine(_currentDemoCo);
            _currentDemoCo = null;
        }
        AnTayThat(false);
        GoiOnDoneMotLan();
        StartCoroutine(FadeOutQuick());
    }

    private void GoiOnDoneMotLan()
    {
        if (_daGoiOnDone) return;
        _daGoiOnDone = true;
        var cb = _onDone;
        _onDone = null;
        try { cb?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    private IEnumerator FadeOutQuick()
    {
        if (_phantomGroup == null) yield break;
        float a = _phantomGroup.alpha;
        while (a > 0f && !_isDemoRunning)
        {
            a -= Time.unscaledDeltaTime * 4f;
            _phantomGroup.alpha = Mathf.Clamp01(a);
            yield return null;
        }
    }

    // =========================================================================
    // Ẩn / hiện TAY THẬT (Tutorial_Hands) bằng CanvasGroup alpha — không SetActive
    // =========================================================================
    /// <summary>
    /// an=true: alpha 0 cho gốc Tutorial_Hands (chứa Hand_Drag_Seed + Hand_Action_Plot_Diamond_Sickle).
    /// an=false: trả alpha 1. Không đụng activeSelf — TutorialManager / ActionHandGuide / DragHint vẫn
    /// tự bật tắt từng tay như cũ.
    /// </summary>
    private void AnTayThat(bool an)
    {
        if (an)
        {
            var cg = LayCanvasGroupTayThat();
            if (cg == null) return;
            cg.alpha = 0f;
            _dangAnTayThat = true;
        }
        else
        {
            if (!_dangAnTayThat) return;
            _dangAnTayThat = false;
            if (_tayThatCG != null) _tayThatCG.alpha = 1f;
        }
    }

    /// <summary>Tìm (cache) CanvasGroup của gốc tay thật. Leo từ HandPointerRT lên tới 'Tutorial_Hands' NGOÀI CÙNG;
    /// không có thì quét scene theo tên; vẫn không có thì dùng chính HandPointerRT.</summary>
    private CanvasGroup LayCanvasGroupTayThat()
    {
        if (_tayThatCG != null) return _tayThatCG;

        RectTransform tay = TutorialManager.Instance != null ? TutorialManager.Instance.HandPointerRT : null;
        Transform goc = null;

        if (tay != null)
        {
            Transform t = tay.parent;
            while (t != null)
            {
                if (t.name == TEN_TAY_THAT) goc = t;          // lấy bản NGOÀI CÙNG (scene có Tutorial_Hands lồng nhau)
                if (t.GetComponent<Canvas>() != null) break;
                t = t.parent;
            }
        }

        if (goc == null)
        {
            var all = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var rt = all[i];
                if (rt == null || rt.name != TEN_TAY_THAT) continue;
                if (rt.parent != null && rt.parent.name == TEN_TAY_THAT) continue;   // bỏ bản lồng trong
                goc = rt;
                break;
            }
        }

        if (goc == null && tay != null) goc = tay;
        if (goc == null) return null;

        _tayThatCG = goc.GetComponent<CanvasGroup>();
        if (_tayThatCG == null) _tayThatCG = goc.gameObject.AddComponent<CanvasGroup>();
        return _tayThatCG;
    }

    // =========================================================================
    // Routines (mỗi routine ≤ 3,2s, unscaled)
    // =========================================================================

    /// <summary>Gieo hạt: fade-in ở khay → nhấn → kéo (cung nhỏ) sang ô → thả (nhú) → fade-out. ≈1,7s.</summary>
    private IEnumerator PlantRoutine(Sprite seedSprite, string fromId, string toId)
    {
        RectTransform toRt = null;
        float han = Time.unscaledTime + CHO_TARGET_GIAY;
        while ((toRt = TutorialManager.GetTargetRect(toId)) == null && Time.unscaledTime < han)
            yield return new WaitForSecondsRealtime(0.1f);
        if (toRt == null)
        {
            Debug.Log($"[PhantomDemo] Không thấy ô đích '{toId}' — bỏ demo gieo hạt.");
            yield break;
        }

        // Khay hạt có thể đang ĐÓNG (bước 06/14) ⇒ xuất phát từ phía dưới ô đích.
        RectTransform fromRt = TutorialManager.GetTargetRect(fromId);
        Vector3 endPos   = TamRect(toRt);
        Vector3 startPos = (fromRt != null && fromRt.gameObject.activeInHierarchy)
            ? TamRect(fromRt)
            : endPos + LechTheoLop(0f, -0.28f);

        // Icon hạt: tham số → CropData → _riceSeedSprite.
        if (seedSprite == null && FarmManager.Instance != null && !string.IsNullOrEmpty(fromId))
        {
            var c = FarmManager.Instance.GetCropById(fromId) ?? FarmManager.Instance.GetCropById(fromId.Replace("seed_", ""));
            if (c != null) seedSprite = c.icon;
        }
        if (seedSprite == null) seedSprite = _riceSeedSprite;
        DatItem(seedSprite);

        // 1. Fade-in tại khay
        DatTay(startPos);
        DatViTriItem(startPos);
        _phantomHand.localScale = TAY_GOC;
        _phantomItem.localScale = Vector3.one;
        yield return FadeTo(ALPHA_TOI_DA, 0.2f);

        // 2. Nhấn giữ
        yield return NhanTay(0.15f);

        // 3. Kéo lướt sang ô (cung nhỏ), item theo tay
        yield return DiChuyen(startPos, endPos, 0.7f, 0.06f, true);

        // 4. Thả hạt: icon nhú lên rồi về
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / 0.2f) * Mathf.PI);
            _phantomItem.localScale = Vector3.one * (1f + 0.25f * k);
            yield return null;
        }
        _phantomItem.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(0.15f);

        // 5. Fade-out
        yield return FadeTo(0f, 0.25f);
    }

    /// <summary>
    /// Tăng tốc, 2 pha: (1) panel chưa mở ⇒ chạm ô rồi chờ ≤1,5s nút kim cương hiện; (2) chạm nút.
    /// Panel đã mở sẵn ⇒ bỏ pha 1. ≤3,1s.
    /// </summary>
    private IEnumerator SpeedUpRoutine(string plotId)
    {
        RectTransform plotRt = null;
        float han = Time.unscaledTime + CHO_TARGET_GIAY;
        while ((plotRt = TutorialManager.GetTargetRect(plotId)) == null && Time.unscaledTime < han)
            yield return new WaitForSecondsRealtime(0.1f);
        if (plotRt == null)
        {
            Debug.Log($"[PhantomDemo] Không thấy ô '{plotId}' — bỏ demo tăng tốc.");
            yield break;
        }

        AnItem();
        Vector3 plotPos = TamRect(plotRt);
        RectTransform nut = TimNutKimCuong();
        Vector3 viTriTay;

        if (nut == null)
        {
            // Pha 1: chạm ô để mở mini-panel
            DatTay(plotPos);
            _phantomHand.localScale = TAY_GOC;
            yield return FadeTo(ALPHA_TOI_DA, 0.2f);
            yield return NhanTay(0.24f);

            float hanNut = Time.unscaledTime + CHO_TARGET_GIAY;
            while ((nut = TimNutKimCuong()) == null && Time.unscaledTime < hanNut)
                yield return new WaitForSecondsRealtime(0.1f);
            viTriTay = plotPos;
        }
        else
        {
            viTriTay = TamRect(nut) + LechTheoLop(0f, -0.12f);
            DatTay(viTriTay);
            _phantomHand.localScale = TAY_GOC;
            yield return FadeTo(ALPHA_TOI_DA, 0.2f);
        }

        // Pha 2: lướt tới nút kim cương (không có nút ⇒ vị trí ước lượng phía trên ô) rồi chạm
        Vector3 dich = nut != null ? TamRect(nut) : plotPos + LechTheoLop(0f, 0.22f);
        yield return DiChuyen(viTriTay, dich, 0.45f, 0.03f, false);
        yield return NhanTay(0.24f);
        yield return new WaitForSecondsRealtime(0.2f);

        yield return FadeTo(0f, 0.25f);
    }

    /// <summary>
    /// Thu hoạch, 2 pha: (1) chạm ô chín (pulse) → chờ ≤1,5s khay liềm hiện; (2) liềm mờ + tay đi từ khay
    /// tới ô 1 rồi ô 2 (bỏ nếu null), cung quét nhỏ, fade-out. ≤3,2s.
    /// </summary>
    private IEnumerator HarvestRoutine(string startPlotId, string nextPlotId)
    {
        RectTransform p1 = null;
        float han = Time.unscaledTime + CHO_TARGET_GIAY;
        while ((p1 = TutorialManager.GetTargetRect(startPlotId)) == null && Time.unscaledTime < han)
            yield return new WaitForSecondsRealtime(0.1f);
        if (p1 == null)
        {
            Debug.Log($"[PhantomDemo] Không thấy ô chín '{startPlotId}' — bỏ demo thu hoạch.");
            yield break;
        }

        RectTransform p2 = string.IsNullOrEmpty(nextPlotId) ? null : TutorialManager.GetTargetRect(nextPlotId);
        Vector3 pos1 = TamRect(p1);

        // Pha 1: chạm ô chín
        AnItem();
        DatTay(pos1);
        _phantomHand.localScale = TAY_GOC;
        yield return FadeTo(ALPHA_TOI_DA, 0.2f);
        yield return NhanTay(0.24f);

        // Chờ khay liềm hiện (tối đa 1,5s); không hiện ⇒ liềm xuất phát ngay tại ô.
        RectTransform khay = null;
        float hanKhay = Time.unscaledTime + CHO_TARGET_GIAY;
        while (Time.unscaledTime < hanKhay)
        {
            khay = FarmUIManager.Instance != null ? FarmUIManager.Instance.SickleTrayRect : null;
            if (khay != null && khay.gameObject.activeInHierarchy) break;
            khay = null;
            yield return new WaitForSecondsRealtime(0.1f);
        }
        Vector3 gocLiem = khay != null ? TamRect(khay) : pos1;

        // Pha 2: liềm mờ theo tay từ khay → ô 1 → ô 2
        Sprite liem = LaySpriteLiem();
        if (liem != null)
        {
            DatItem(liem);
            DatViTriItem(gocLiem);
        }
        else
        {
            AnItem();
        }

        if (khay != null)
        {
            DatTay(gocLiem);
            yield return DiChuyen(gocLiem, pos1, 0.45f, 0.04f, liem != null);
        }

        if (p2 != null)
        {
            Vector3 pos2 = TamRect(p2);
            yield return DiChuyen(pos1, pos2, 0.4f, 0.08f, liem != null);
        }
        else
        {
            // Chỉ một ô ⇒ quẹt ngắn qua ô cho ra dáng "gặt"
            Vector3 trai = pos1 + LechTheoLop(-0.05f, 0f);
            Vector3 phai = pos1 + LechTheoLop( 0.05f, 0f);
            yield return DiChuyen(trai, phai, 0.35f, 0.05f, liem != null);
        }

        yield return new WaitForSecondsRealtime(0.15f);
        yield return FadeTo(0f, 0.25f);
    }

    // =========================================================================
    // Helpers chuyển động
    // =========================================================================
    private static float EaseInOut(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

    /// <summary>Tâm hình học world của RectTransform (không lệch do pivot).</summary>
    private static Vector3 TamRect(RectTransform rt)
    {
        rt.GetWorldCorners(_gocBuf);
        return (_gocBuf[0] + _gocBuf[2]) * 0.5f;
    }

    /// <summary>Độ lệch world tính theo % kích cỡ lớp ảo ảnh — không phụ thuộc scale canvas.</summary>
    private Vector3 LechTheoLop(float phanTramNgang, float phanTramDoc)
    {
        var lop = _phantomGroup != null ? _phantomGroup.transform as RectTransform : null;
        if (lop == null) return new Vector3(phanTramNgang * 1000f, phanTramDoc * 1000f, 0f);
        Vector3 local = new Vector3(lop.rect.width * phanTramNgang, lop.rect.height * phanTramDoc, 0f);
        return lop.TransformVector(local);
    }

    /// <summary>Đặt đầu ngón tay ảo vào điểm world (pivot đã là đầu ngón).</summary>
    private void DatTay(Vector3 world)
    {
        Vector3 p = world; p.z = _phantomHand.position.z;
        _phantomHand.position = p;
    }

    private void DatViTriItem(Vector3 world)
    {
        if (_phantomItem == null) return;
        Vector3 p = world; p.z = _phantomItem.position.z;
        _phantomItem.position = p;
    }

    private void DatItem(Sprite sprite)
    {
        if (_itemImage == null) return;
        if (sprite == null) { AnItem(); return; }
        _itemImage.sprite = sprite;
        _itemImage.gameObject.SetActive(true);
        _phantomItem.sizeDelta  = new Vector2(KICH_ICON_PX, KICH_ICON_PX);
        _phantomItem.localScale = Vector3.one;
    }

    private void AnItem()
    {
        if (_itemImage != null) _itemImage.gameObject.SetActive(false);
    }

    /// <summary>Nhấn: scale 1 → 0,85 → 1 (tương đối so với TAY_GOC).</summary>
    private IEnumerator NhanTay(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);   // 0→1→0
            _phantomHand.localScale = TAY_GOC * Mathf.Lerp(1f, 0.85f, k);
            yield return null;
        }
        _phantomHand.localScale = TAY_GOC;
    }

    /// <summary>Di chuyển tay (và item nếu keoItem) từ a → b, ease in-out, cung cao = cung% chiều cao lớp.</summary>
    private IEnumerator DiChuyen(Vector3 a, Vector3 b, float duration, float cung, bool keoItem)
    {
        Vector3 dinhCung = LechTheoLop(0f, cung);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = EaseInOut(t / duration);
            Vector3 cur = Vector3.Lerp(a, b, k) + dinhCung * Mathf.Sin(k * Mathf.PI);
            DatTay(cur);
            if (keoItem) DatViTriItem(cur);
            yield return null;
        }
        DatTay(b);
        if (keoItem) DatViTriItem(b);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (_phantomGroup == null) yield break;
        targetAlpha = Mathf.Min(targetAlpha, ALPHA_TOI_DA);
        float startAlpha = _phantomGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _phantomGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseInOut(t / duration));
            yield return null;
        }
        _phantomGroup.alpha = targetAlpha;
    }

    // =========================================================================
    // Tìm nút kim cương (nút tăng tốc) đang hiện
    // =========================================================================
    /// <summary>Ưu tiên CropProcessPopupUI đang mở (SpeedUpButtonRect) → Instance → quét tên nút (fallback cuối).</summary>
    private static RectTransform TimNutKimCuong()
    {
        var popups = FindObjectsByType<CropProcessPopupUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < popups.Length; i++)
        {
            var p = popups[i];
            if (p != null && p.IsOpen && p.SpeedUpButtonRect != null && p.SpeedUpButtonRect.gameObject.activeInHierarchy)
                return p.SpeedUpButtonRect;
        }

        var inst = CropProcessPopupUI.Instance;
        if (inst != null && inst.IsOpen && inst.SpeedUpButtonRect != null && inst.SpeedUpButtonRect.gameObject.activeInHierarchy)
            return inst.SpeedUpButtonRect;

        if (!CropProcessPopupUI.AnyOpen) return null;   // panel chưa mở thì quét tên cũng vô ích
        return FindSpeedButton();
    }

    /// <summary>Fallback cuối: quét mọi Button đang bật theo tên. Không tin cậy — chỉ dùng khi API trên trả null.</summary>
    private static RectTransform FindSpeedButton()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            string n = b.name.ToLowerInvariant();
            if (n.Contains("speedup") || n.Contains("rutnang") || n.Contains("gem"))
                return b.GetComponent<RectTransform>();
        }
        return null;
    }
}
