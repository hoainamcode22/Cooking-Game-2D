using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PenMiniPanelUI : MonoBehaviour
{
    public enum PenState { Idle, Processing, Ready }

    private const string PrefKeyState     = "PenState_";
    private const string PrefKeyFood      = "PenFood_";
    private const string PrefKeyStartTime = "PenStartTime_";

    private const string PenSaveFamily  = "PEN_STATE";
    private const int    PenSaveVersion = 1;

    [Header("Config")]
    [SerializeField] private PenMiniPanelConfig config;
    public PenMiniPanelConfig Config => config;

    // [V2 ADD] ═══════════════════════════════════════════════════════════════
    // KHAY VẬT PHẨM V2 (PenSupplyTrayV2) — một khay duy nhất kiểu Hay Day gom
    // [rỗ thu hoạch] + [bao thức ăn] nổi cạnh chuồng, thay cho 2 UI rời rạc
    // (PenBasketTrayController + LivestockFeedPopupController).
    //
    // BẬT (mặc định): OpenPanel chuyển hướng sang PenSupplyTrayV2.TryShow(this).
    // TẮT: quay về NGUYÊN TRẠNG 100% — hai UI cũ, không một dòng logic nào bị xoá.
    // TryShow trả false (thiếu Camera, chuồng đang Processing…) cũng tự rơi về UI cũ.
    // Toàn bộ callback cho ăn / thu hoạch (TryFeed / TryHarvest) không đổi — khay V2
    // chỉ là lớp vỏ nhúng lại LivestockFeedDragItem + PenBasketDragItem.
    // ═════════════════════════════════════════════════════════════════════════
    [Header("Khay Vật Phẩm V2")] // [V2 ADD]
    [Tooltip("Bật: mở khay hợp nhất V2 (rỗ + thức ăn cạnh chuồng). Tắt: dùng 2 UI cũ như nguyên trạng.")] // [V2 ADD]
    [SerializeField] private bool useSupplyTrayV2 = true; // [V2 ADD]

    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slot Food 1")]
    [SerializeField] private GameObject slot1Root;
    [SerializeField] private Image      slot1Icon;
    [SerializeField] private TMP_Text   slot1Amount;

    [Header("Slot Food 2")]
    [SerializeField] private GameObject slot2Root;
    [SerializeField] private Image      slot2Icon;
    [SerializeField] private TMP_Text   slot2Amount;

    [Header("Slot Basket")]
    // Ô THỨC ĂN THỨ 3 — túi cám từ máy xay (thêm 20/08).
    // TỰ ẨN khi config.premiumFoodItemId trống, nên prefab chuồng chưa có node này vẫn
    // chạy bình thường (cả 3 field null → mọi nhánh dưới đều no-op).
    [SerializeField] private GameObject slot3Root;
    [SerializeField] private Image      slot3Icon;
    [SerializeField] private TMP_Text   slot3Amount;

    [SerializeField] private GameObject basketRoot;
    [SerializeField] private Image      basketIcon;
    [SerializeField] private GameObject basketActiveGlow;

    [Header("Progress Overlay")]
    [SerializeField] private GameObject progressOverlay;
    [SerializeField] private Image      progressFill;
    [SerializeField] private TMP_Text   progressLabel;

    public PenState CurrentState { get; private set; } = PenState.Idle;

    // [FIX 2026-09-06 vong6] TRUOC DAY LA float. Giay Unix bay gio ~1.79e9, vuot xa do
    // chinh xac cua float (o vung so nay float chi con buoc nhay 128 GIAY) nen moc bat dau
    // bi lam tron ve boi so cua 128 => dong ho 45 giay chay that su 0..109 giay, va co 20
    // giay trong moi 128 giay khien remaining = 0 NGAY khi vua cho an ("00:00" + thanh day
    // + gem 15). Nay dung long GIAY UNIX y het PlotController (cay trong) va
    // ConstructionManager (cong trinh) — mot cua duy nhat cho ca du an.
    private long    processStartUnix;
    private long    processEndUnix;
    private string  activeFoodId;
    private Coroutine timerCoroutine;
    private bool    popupInputLockHeld;

    // Giữ panel mở đủ lâu để user kéo thức ăn vào (không bị đóng ngay sau khi mở)
    private const float PanelKeepOpenSeconds = 1.5f;
    private float _openedAtTime = -99f;

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        EnsurePanelLayout();
    }

    private void EnsurePanelLayout()
    {
        // 1. Đảm bảo Canvas luôn Override Sorting ở lớp cao (1500) để nổi lên trên chuồng và cây cối
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1500;
        }

        // 2. Định vị Panel chính giữa chuồng, nổi cao ráo trên đỉnh hàng rào
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = new Vector2(0f, 3.2f);
            rt.sizeDelta = new Vector2(130f, 130f);
        }

        // 3. Chuẩn hóa kích thước khung chứa gọn gàng, bo góc đẹp
        if (panelRoot != null)
        {
            RectTransform prRt = panelRoot.GetComponent<RectTransform>();
            if (prRt != null)
            {
                prRt.anchoredPosition = Vector2.zero;
                prRt.sizeDelta = new Vector2(120f, 120f);
            }

            Transform bg = panelRoot.transform.Find("Background");
            if (bg != null)
            {
                RectTransform bgRt = bg.GetComponent<RectTransform>();
                if (bgRt != null)
                {
                    bgRt.anchoredPosition = Vector2.zero;
                    bgRt.sizeDelta = new Vector2(120f, 120f);
                }
                Image bgImg = bg.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = new Color(1f, 0.98f, 0.94f, 0.96f);
                }
            }
        }
    }

    private void Start()
    {
        LoadState();
        if (CurrentState == PenState.Processing)
        {
            // LoadState() da tu chua nhanh "het gio roi ma van con Processing" nen toi day
            // remaining chac chan > 0. Van giu chot nay cho chac.
            if (GetRemainingSeconds() <= 0f)
            {
                ClearProcessStamps();
                SetState(PenState.Ready);
                SaveState();
            }
            else
            {
                timerCoroutine = StartCoroutine(ProcessTimerCoroutine());
            }
        }
        UpdateReadyBubble();

        { Debug.Log($"[Pen] {(config != null ? config.penId : "?")} NAP_SAVE state={CurrentState} conLai={GetRemainingSeconds():F0}s start={processStartUnix} end={processEndUnix}"); }
    }

    /// <summary>
    /// RAO TU CHUA LUC DANG CHOI: coroutine dem gio BI UNITY GIET neu GameObject chuong bi
    /// SetActive(false) (culling / doi khu vuc), ma Start() thi KHONG chay lai khi bat len.
    /// Khi do chuong ket cung o Processing va khong bao gio sang Ready duoc. Chot nay chi ra
    /// tay dung truong hop do (timerCoroutine == null) nen khong dinh gi den luot dem binh thuong.
    /// </summary>
    private void TickProcessTimeout()
    {
        if (CurrentState != PenState.Processing) return;
        // CHI hoi DONG HO, khong hoi timerCoroutine: Unity giet coroutine khi GameObject bi
        // SetActive(false) nhung KHONG xoa bien timerCoroutine, nen bien do con la tham chieu
        // CHET (khac null) — tin no thi rao se khong bao gio ra tay dung luc can nhat.
        // Coroutine cung thoat vong lap tai dung moc nay nen hai ben khong da nhau.
        if (processEndUnix > 0L && GetUnixNow() < processEndUnix) return;

        StopTimerIfRunning();
        ClearProcessStamps();
        SetState(PenState.Ready);
        SaveState();
    }

    private void Update()
    {
        TickProcessTimeout();
        TickReadyBubbleBob();

        if (!IsPanelOpen()) return;

        // [FIX 2026-09-06 vong3] Toan bo doan duoi la logic "cham ra ngoai thi dong" CUA PANEL
        // CU (panelRoot, world-space). Khay V2 TU LO viec nay trong PenSupplyTrayV2.OnDimPressed()
        // va con co chot 0.08s de nuot lai chinh cai click vua mo. Neu de doan nay chay cho khay
        // V2 thi IsPointerOverPanel() se do vao panelRoot DANG TAT => luon tra false => dong khay
        // ngay trong frame vua mo. Vi vay: chi chay khi panel CU thuc su dang bat.
        if (panelRoot == null || !panelRoot.activeSelf) return;

        if (FarmInputLock.IsDraggingSeed) return;

        // Giữ panel mở trong PanelKeepOpenSeconds đầu sau khi mở
        // → user có đủ thời gian nhìn vào khung và bắt đầu kéo thức ăn
        if (Time.unscaledTime < _openedAtTime + PanelKeepOpenSeconds) return;

        bool clicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    || Input.GetMouseButtonDown(0);

        if (!clicked && Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
        }

        if (!clicked) return;

        Vector2 screenPos;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();
        else
            screenPos = Input.mousePosition;

        if (!IsPointerOverPanel(screenPos))
            ClosePanel();
    }

    private void AcquirePopupInputBlock()
    {
        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
        // Unity da GIET coroutine dem gio khi GameObject tat. Bo tham chieu chet di, neu
        // khong OnEnable ben duoi se tuong dong ho van dang chay va khong noi lai.
        timerCoroutine = null;
    }

    private void OnEnable()
    {
        // Bat lai chuong (culling / doi khu vuc): Start() KHONG chay lai, nen phai tu noi lai
        // dong ho. Moc thoi gian van con nguyen trong processEndUnix nen luot nuoi khong mat giay nao.
        if (CurrentState == PenState.Processing && timerCoroutine == null && GetRemainingSeconds() > 0f)
            timerCoroutine = StartCoroutine(ProcessTimerCoroutine());
    }

    [Header("Title")]
    [SerializeField] private TMP_Text txtPenTitle;

    [Tooltip("Offset spawn FX")]
    [SerializeField] private float harvestSpawnUpOffset = 280f;

    /// <summary>
    /// [FIX 2026-09-06] Nguoi choi moi (cap 1-2) duoc TANG TOC CHUONG MIEN PHI.
    /// Ly do: tang toc chuong ga ~21 kim cuong trong khi nguoi moi chi co ~15-19 ⇒ buoc
    /// "bam kim cuong" cua tutorial la ngo cut, khong the hoan thanh. Tu CAP 3 tro di tinh
    /// tien binh thuong. (O dat vốn đã miễn phí trong tutorial — nay chuong dong bo theo.)
    /// </summary>
    public const int CAP_BAT_DAU_TINH_GEM_CHUONG = 3;

    /// <summary>Dang trong giai doan mien phi tang toc chuong (cap &lt; 3)?</summary>
    public static bool TangTocChuongDangMienPhi
    {
        get
        {
            var pp = PlayerProgressManager.Instance;
            return pp == null || pp.Level < CAP_BAT_DAU_TINH_GEM_CHUONG;
        }
    }

    public int SpeedUpGemCost =>
        CurrentState == PenState.Processing
            ? (TangTocChuongDangMienPhi ? 0 : ConstructionManager.RushCostFor(GetRemainingSeconds()))
            : 0;

    [Header("Sorting")]
    [SerializeField] private Canvas processOverlayCanvas;
    [SerializeField] private int processSortingOrder = 1500;

    [Header("Tutorial")]
    [SerializeField] private Sprite gemButtonBgSprite;
    [SerializeField] private Sprite gemIconSprite;
    [SerializeField] private Sprite readyBubbleBgSprite;
    [SerializeField] private Vector2 readyBubbleLocalPos = new Vector2(0f, 320f);
    [SerializeField] private int readyBubbleSortingOrder = 1500;

    // ===================================================================================
    //  [FIX 2026-09-06 vong8 - Sep yeu cau] BONG BONG SAN PHAM: nang cao, noi tren bui co,
    //  CLICK 1 CAI LA NHAN.
    //
    //  VI SAO PHAI DO CHU KHONG GO SO: hai so cu deu la so ma va LECH DON VI.
    //    * EnsurePanelLayout() dat anchoredPosition = (0, 3.2) - don vi LOCAL cua chuong
    //      (Pen_0x scale = 100) nen thuc te la 320 world unit.
    //    * readyBubbleLocalPos.y = 320 - don vi canvas (canvas scale 0.01 x 100 = 1) nen
    //      cung la 320 world unit.
    //      Hai so "3.2" va "320" dien ta CUNG mot y do ma lech nhau 100 lan => bang chung
    //      day la so doan. Nay DO THAT tu bounds cua BarnSprite + cac renderer trang tri
    //      quanh chuong roi moi cong bien an toan.
    //
    //  VI SAO PHAI DOI SORTING LAYER: canvas bong bong duoc tao bang AddComponent<Canvas>()
    //  va TRUOC GIO KHONG HE duoc gan sortingLayer => nam im tren layer mac dinh
    //  (id 0 = "Default", chi la layer thu 2 tu duoi trong 5 layer). Trong khi do:
    //    * Con vat (LivestockAI) nam tren "Objects" - layer CAO HON - order >= 512.
    //    * Trang tri trong SCN_Farm.unity deu deo sorting layer id 1669604809, id nay
    //      KHONG co trong ProjectSettings/TagManager.asset - dung cai bay ma
    //      TouristSortingLayers.cs da ghi lai (layer sai => Unity im lang ha ve Default).
    //    * Gangplank nam tren "Objects" THAT (id 1471039481) order 900: de len bong bong
    //      du bong bong co order 1500, vi khac LAYER thi order khong con y nghia.
    //  Nay gan thang len "Foreground" (layer CAO NHAT co that) qua
    //  TouristSortingLayers.Overlay nen thang moi vat trang tri, bat ke dangling id kia
    //  duoc Unity giai ra layer nao. Foreground van NAM DUOI toan bo UI ScreenSpaceOverlay
    //  (HUD, PenSupplyTrayV2) nen khong the de len UI.
    // ===================================================================================

    [Header("Bong bong san pham (vong8)")]
    [Tooltip("DE TRONG = tu giai sang layer cao nhat CO THAT (Foreground) qua " +
             "TouristSortingLayers.Overlay. Chi go ten khac neu layer do co thuc.")]
    [SerializeField] private string readyBubbleSortingLayer = "";

    [Tooltip("Khoang ho (WORLD unit) tu dinh cao nhat do duoc (chuong / bui co) len day bong bong.")]
    [SerializeField] private float readyBubbleWorldClearance = 120f;

    [Tooltip("Noi rong (WORLD unit) vung quet sang hai ben be ngang cua bong bong. Chi vat NAM DE " +
             "LEN be ngang bong bong moi co the che no, nen quet theo be ngang thay vi ban kinh tron.")]
    [SerializeField] private float readyBubbleScanPad = 60f;

    [Tooltip("TRAN: nang toi da bao nhieu WORLD unit tinh tu dinh CHUONG. Chan truong hop mot sprite " +
             "cao bat thuong day bong bong bay ra ngoai khung hinh.")]
    [SerializeField] private float readyBubbleMaxRaiseWorld = 900f;

    [Tooltip("Bien an toan cong vao order LON NHAT do duoc quanh chuong.")]
    [SerializeField] private int readyBubbleOrderMargin = 200;

    [Tooltip("Bien do nhap nho len xuong (WORLD unit). Dat 0 = tat animation.")]
    [SerializeField] private float readyBubbleBobAmplitude = 16f;

    [Tooltip("Chu ky nhap nho (giay). Cang lon cang diu.")]
    [SerializeField] private float readyBubbleBobPeriod = 2.4f;

    // ╔══════════════════════════════════════════════════════════════════════╗
    // ║ V13e — "cho no nam CAO TREN MAT CHUONG, dang bubble / popup" (Sep)   ║
    // ╚══════════════════════════════════════════════════════════════════════╝
    // TRIEU CHUNG: bong bong san pham nam THAP, doc ra nhu dang nam duoi dat.
    //
    // NGUYEN NHAN: `readyBubbleLocalPos.y` DA BI SERIALIZE = 320 trong ca 4 prefab
    // chuong (khoi PrefabInstance, propertyPath readyBubbleLocalPos.y) nen gia tri
    // mac dinh trong code khong bao gio duoc dung. Ma than chuong (BarnSprite,
    // 4.13 x 2.98 unit, scale 150, pivot DAY) phu y thuoc [0, 447] world
    // => bong bong o 320 nam LOT GIUA than chuong, khong phai tren dinh.
    //
    // Bản cu co doan "tu do dinh chuong roi nang len", nhung no quy ket qua ve
    // `anchoredPosition` bang phep chia thu cong `(tamWorld - transform.position.y) / donVi`
    // — phep nay chi dung khi RectTransform cha nam DUNG tai goc chuong. Chi can panel
    // duoc dat lech mot chut la sai, va no lai bi `Mathf.Max` voi so 320 serialize nen
    // khong bao gio tut xuong duoi 320 nhung cung khong chac len dung cho.
    //
    // V13e: dat THANG bang WORLD POSITION. RectTransform nhan `.position` binh thuong,
    // Unity tu quy ve anchoredPosition — khong con phep chia tay nao de sai.
    // Ba field duoi deu la FIELD MOI (scene/prefab chua serialize) nen gia tri trong
    // code CHINH LA gia tri that luc chay.

    [Tooltip("V13e BAT = dat bong bong bang WORLD position (chuan, khong phu thuoc cay cha). " +
             "Tat = tra ve phep tinh anchoredPosition cu.")]
    [SerializeField] private bool datBongBongTheoWorld = true;

    [Tooltip("Nang THEM bao nhieu WORLD unit tren dinh do duoc, ngoai readyBubbleWorldClearance. " +
             "1 o luoi sau 150 world; 80 = hon nua o, du de doc ra 'dang bay tren mai'.")]
    [SerializeField] private float bongBongCaoThemWorld = 80f;

    [Tooltip("Du phong khi KHONG do duoc dinh chuong: dat bong bong cao bay nhieu WORLD unit " +
             "tinh tu GOC chuong. Than chuong cao 447 world nen 640 la trên mai, khong dinh art.")]
    [SerializeField] private float bongBongCaoDuPhongWorld = 640f;

    /// <summary>Tran order, khong bao gio vuot gioi han sorting cua Unity.</summary>
    private const int ReadyBubbleOrderMax = 30000;

    /// <summary>Chong bam 2 lan cong doi san pham (giay, dong ho khong phu thuoc timeScale).</summary>
    private const float ReadyBubbleClickCooldown = 0.35f;

    /// <summary>
    /// Renderer rong hon chuong QUA NHIEU lan thi la nen/dat, khong phai bui co - bo qua khi do
    /// chieu cao, neu khong bong bong se bi day len tan dinh anh nen.
    /// </summary>
    private const float ReadyBubbleNenRongGap = 4f;

    /// <summary>
    /// [FIX 2026-09-06] Khay V2 (PenSupplyTrayV2) moi la khay dang dung thuc te; panelRoot doi
    /// luon TAT nen ham nay tra false du khay DANG mo ⇒ tutorial tuong khay chua mo va cho mai.
    ///
    /// [FIX 2026-09-06 vong3] NHUNG phai hoi theo TUNG CHUONG. Ban truoc dung co toan cuc
    /// PenSupplyTrayV2.DangMoKhay, ma ham nay lai duoc 4 noi goi voi y nghia "cua RIENG chuong
    /// nay" (Update() ngay duoi, PenClickDetector:112, LivestockAI:412,
    /// TutorialRuntimeTargetResolver:139). Hau qua: mo khay cho chuong A thi chuong B/C/D cung
    /// bao "panel cua toi dang mo" va tu dong dong khay => bam chuong khong ra gi.
    /// </summary>
    public bool IsPanelOpen() => PenSupplyTrayV2.DangMoKhayCho(this)
                                 || (panelRoot != null && panelRoot.activeSelf);
    public RectTransform FirstFeedSlotRect => slot1Root != null ? slot1Root.GetComponent<RectTransform>() : null;
    public RectTransform BasketSlotRect => basketRoot != null ? basketRoot.GetComponent<RectTransform>() : null;
    public RectTransform SpeedUpButtonRect
    {
        get
        {
            EnsureGemButton();
            PlaceGemButton();
            return _gemButtonGO != null ? _gemButtonGO.GetComponent<RectTransform>() : null;
        }
    }

    public void OpenPanel()
    {
        if (config == null) return;

        // [FIX 2026-09-06 vong3] _openedAtTime truoc gio KHONG BAO GIO duoc gan (chi khai bao
        // = -99f roi thoi), nen chot PanelKeepOpenSeconds trong Update() la CODE CHET. Gan lai
        // o day de dung y do ban dau: giu panel mo it nhat 1.5s sau khi mo.
        _openedAtTime = Time.unscaledTime;

        NotifyAnimalsVoice();

        // 1. Nếu đang nuôi (Processing) -> Mở Process Popup (thanh đếm ngược + Speed-up Gem - GIỮ NGUYÊN)
        if (CurrentState == PenState.Processing)
        {
            var popup = PenProcessPopupUI.Instance ?? FindFirstObjectByType<PenProcessPopupUI>(FindObjectsInactive.Include);
            if (popup == null)
            {
                var go = new GameObject("PenProcessPopupUI_Host", typeof(PenProcessPopupUI));
                popup = go.GetComponent<PenProcessPopupUI>();
            }
            if (popup != null)
            {
                popup.Open(this);
                // [FIX 2026-09-06] Bao cho tutorial: buoc L2_09 (tang toc chuong) cho nguoi choi MO
                // bang tien trinh. Truoc day chi nhanh Idle bao, nen mo bang luc dang ap thi
                // tutorial khong biet ⇒ ket cung o buoc 28.
                TutorialManager.Instance?.NotifyOpenPen();
                return;
            }
        }

        // 2. Nếu đã sẵn sàng thu hoạch (Ready) -> Mở Khay Cái Rổ (Basket Tray) giống gặt lúa
        if (CurrentState == PenState.Ready)
        {
            // [V2 ADD] Khay hợp nhất V2 — TryShow false thì rơi về khay rỗ cũ y nguyên.
            // [FIX 2026-09-06] Nhanh Ready cung phai bao — buoc L2_10 (thu trung) cho MO khay ro.
            if (useSupplyTrayV2 && PenSupplyTrayV2.TryShow(this))
            {
                TutorialManager.Instance?.NotifyOpenPen();
                return; // [V2 ADD]
            }
            FarmUIManager.Instance?.ShowPenBasketTray(this);
            TutorialManager.Instance?.NotifyOpenPen();
            return;
        }

        // 3. Nếu đang đói (Idle) -> Mở Screen-Space Feed Popup (giống Seed Popup)
        if (CurrentState == PenState.Idle)
        {
            // [V2 ADD] Khay hợp nhất V2 — giữ đúng thứ tự cũ: mở UI xong mới Notify tutorial.
            if (useSupplyTrayV2 && PenSupplyTrayV2.TryShow(this)) // [V2 ADD]
            {                                                     // [V2 ADD]
                TutorialManager.Instance?.NotifyOpenPen();        // [V2 ADD]
                return;                                           // [V2 ADD]
            }                                                     // [V2 ADD]
            FarmUIManager.Instance?.ShowLivestockFeedPopup(this);
            TutorialManager.Instance?.NotifyOpenPen();
            return;
        }
    }

    private void NotifyAnimalsVoice()
    {
        var ais = GetComponentsInChildren<Assetsgame.Animals.LivestockAI>(true);
        if (ais != null && ais.Length > 0)
        {
            ais[UnityEngine.Random.Range(0, ais.Length)].PlayAnimalSound(true);
        }
    }

    public void ClosePanel()
    {
        ReleasePopupInputBlock();
        if (panelRoot != null) panelRoot.SetActive(false);
        FarmUIManager.Instance?.HideLivestockFeedPopup();
        FarmUIManager.Instance?.HidePenBasketTray();
        PenSupplyTrayV2.HideIfShowing(); // [V2 ADD] đóng khay V2 cùng nhịp 2 UI cũ (no-op khi không dùng)
        FarmInputLock.SuppressWorldClickForCurrentFrame();
    }

    public void OnSlot1Clicked()
    {
        if (config == null || CurrentState != PenState.Idle) return;
        string feedItemId = !string.IsNullOrEmpty(config.food1ItemId) ? config.food1ItemId : config.premiumFoodItemId;
        TryFeed(feedItemId, transform.position);
    }

    public void OnSlot2Clicked()
    {
    }

    public void OnSlot3Clicked()
    {
        OnSlot1Clicked();
    }

    public void OnBasketClicked()
    {
        if (CurrentState != PenState.Ready) return;
        TryHarvest(transform.position);
    }

    public bool TryFeed(string foodItemId, Vector3 vfxWorldPosition)
    {
        if (CurrentState != PenState.Idle || config == null) return false;

        string validFeedId = !string.IsNullOrEmpty(config.food1ItemId) ? config.food1ItemId : config.premiumFoodItemId;
        if (foodItemId != validFeedId && foodItemId != config.food1ItemId && foodItemId != config.premiumFoodItemId)
            return false;

        int need = FoodNeededFor(foodItemId);
        if (!FarmInventoryManager.Instance.HasItem(foodItemId, need))
        {
            FarmUIManager.Instance?.ShowHint(Loc.TF("Cần {0} bao thức ăn cho một lượt nuôi. Hãy xay tại Máy Xay Thức Ăn!", need));
            return false;
        }

        FarmInventoryManager.Instance.RemoveItem(foodItemId, need);
        MissionProgressTracker.ReportEvent(MissionEventType.FeedAnimal, foodItemId, need);
        PlayFeedVFX(foodItemId, vfxWorldPosition);
        AudioManager.Instance?.PlayPlanting();
        NotifyAnimalsVoice();
        activeFoodId = foodItemId;
        BeginProcessing();   // DAT LAI ca moc bat dau LAN moc ket thuc — moi luot, khong tru luot nao
        SaveState();

        StopTimerIfRunning();
        timerCoroutine = StartCoroutine(ProcessTimerCoroutine());

        { Debug.Log($"[Pen] {config.penId} CHO_AN state={CurrentState} conLai={GetRemainingSeconds():F0}s start={processStartUnix} end={processEndUnix}"); }

        // Đóng mini panel (Idle) và mở CropProcessPopupUI (thanh process mới giống ruộng)
        // Xoá flow cũ: progressOverlay hiện ngay → thay bằng popup riêng
        ClosePanel();
        var popup = PenProcessPopupUI.Instance ?? FindFirstObjectByType<PenProcessPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
        {
            var go = new GameObject("PenProcessPopupUI_Host", typeof(PenProcessPopupUI));
            popup = go.GetComponent<PenProcessPopupUI>();
        }
        if (popup != null)
            popup.Open(this);

        if (IsPenTutorialStep("L2_08_FeedPen"))
        {
            // Tutorial đã ClosePanel ở trên rồi, không cần gọi lại
        }

        TutorialManager.Instance?.NotifyFeed();
        return true;
    }


    public bool TryHarvest(Vector3 vfxWorldPosition)
    {
        if (CurrentState != PenState.Ready) return false;

        var inv = FarmInventoryManager.Instance;
        if (inv != null)
        {
            bool fit = inv.CanAddItem(config.productItemId)
                    && (string.IsNullOrEmpty(config.secondProductItemId) || inv.CanAddItem(config.secondProductItemId));

            if (!fit)
            {
                FarmUIManager.Instance?.ShowHint(
                    Loc.TF("Kho đầy ({0}/{1} slot) — bán bớt hoặc nâng cấp kho rồi thu hoạch.", inv.UsedSlots, inv.SlotCapacity));
                return false;
            }
        }

        // Chốt cờ cám NGAY ĐÂY: cuối hàm activeFoodId bị xoá về null, đọc sau là mất thưởng.
        bool anCam = DangNuoiBangCam;
        int bonus  = anCam ? Mathf.Max(0, config.premiumProductBonus) : 0;

        Vector3 productSpawn = vfxWorldPosition + Vector3.up * harvestSpawnUpOffset;
        int productAmount = Mathf.Max(1, config.productAmount) + bonus;
        SpawnHarvestFX(config.productItemId, config.productIcon, productAmount, productSpawn);

        int secondAmount = Mathf.Max(1, config.secondProductAmount) + bonus;

        if (!string.IsNullOrEmpty(config.secondProductItemId))
            SpawnHarvestFX(config.secondProductItemId, config.secondProductIcon,
                secondAmount, productSpawn);

        int expThuong = config.expReward + (anCam ? Mathf.Max(0, config.premiumExpBonus) : 0);

        if (HarvestFeedbackSpawner.Instance != null)
            HarvestFeedbackSpawner.Instance.SpawnExpFly(transform.position + Vector3.up * harvestSpawnUpOffset, expThuong);

        AudioManager.Instance?.PlayHarvest();
        NotifyAnimalsVoice();

        FarmInventoryManager.Instance.AddItem(config.productItemId, productAmount);
        MissionProgressTracker.ReportEvent(MissionEventType.CollectAnimalProduct, config.productItemId, productAmount);
        if (!string.IsNullOrEmpty(config.secondProductItemId))
        {
            FarmInventoryManager.Instance.AddItem(config.secondProductItemId, secondAmount);
            MissionProgressTracker.ReportEvent(MissionEventType.CollectAnimalProduct, config.secondProductItemId, secondAmount);
        }

        activeFoodId = null;
        StopTimerIfRunning();
        ClearProcessStamps();
        SetState(PenState.Idle);
        SaveState();
        RefreshUI();
        TutorialManager.Instance?.NotifyPenHarvest();
        return true;
    }

    public bool TrySpeedUpGem()
    {
        if (CurrentState != PenState.Processing) return false;
        if (FarmEconomyManager.Instance == null) return false;
        int gemCost = SpeedUpGemCost;
        if (FarmEconomyManager.Instance.Gems < gemCost)
        {
            FarmUIManager.Instance?.ShowHint(Loc.TF("Cần {0} kim cương để hoàn tất ngay.", gemCost));
            return false;
        }
        if (!FarmEconomyManager.Instance.SpendGems(gemCost)) return false;

        StopTimerIfRunning();
        ClearProcessStamps();
        SetState(PenState.Ready);
        SaveState();

        bool penTutorialActive = IsPenTutorialActive();
        if (penTutorialActive)
            ClosePanel();

        TutorialManager.Instance?.NotifyPenSpeedUp();

        if (!penTutorialActive)
            TryHarvest(transform.position);

        return true;
    }

    private static bool IsPenTutorialActive()
    {
        string step = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStepName : null;
        return step == "L2_09_PenSpeedUp" || step == "L2_10_HarvestPen";
    }

    private static bool IsPenTutorialStep(string stepName)
    {
        return TutorialManager.Instance != null
            && TutorialManager.Instance.CurrentStepName == stepName;
    }

    /// <summary>
    /// LOI VAO DUY NHAT cua trang thai Processing — DAT LAI moc bat dau VA moc ket thuc.
    /// Day chinh la cho bug vong 6 nam: tu luot cho an thu 2 tro di, moc cu (da bi float lam
    /// tron ve boi so cua 128 giay) khien GetRemainingSeconds() tra 0 ngay tu frame dau,
    /// nen popup dung im o "00:00" suot ca luot.
    /// </summary>
    private void BeginProcessing()
    {
        long duration    = (long)Mathf.Max(1f, Mathf.Ceil(EffectiveFeedSeconds));
        processStartUnix = GetUnixNow();
        processEndUnix   = processStartUnix + duration;
        SetState(PenState.Processing);
    }

    /// <summary>Roi khoi Processing — xoa moc de so cu khong con lam nhieu luot sau.</summary>
    private void ClearProcessStamps()
    {
        processStartUnix = 0L;
        processEndUnix   = 0L;
    }

    private void SetState(PenState newState)
    {
        CurrentState = newState;
        RefreshUI();
        UpdateReadyBubble();
    }

    /// <summary>
    /// Dem gio theo DONG HO THAT (processEndUnix), khong con cong don Time.deltaTime.
    /// Ly do: deltaTime dung lai khi timeScale = 0 / app bi treo nen dong ho lech dan so voi
    /// moc da luu; va sau khi tai lai scene, thanh tien do bi ve lai tu 0 du luot da chay noi.
    /// </summary>
    private IEnumerator ProcessTimerCoroutine()
    {
        float total = Mathf.Max(1f, EffectiveFeedSeconds);
        while (true)
        {
            float remaining = GetRemainingSeconds();
            if (remaining <= 0f) break;
            float t = Mathf.Clamp01(1f - remaining / total);

            if (progressFill  != null) progressFill.fillAmount = t;
            if (progressLabel != null) progressLabel.text = FormatTime(remaining);
            // [FIX 2026-09-06] Hien dung gia: mien phi thi bao MIEN PHI, khong bao "x21".
            if (_gemCostText  != null) _gemCostText.text = TangTocChuongDangMienPhi ? "MIỄN PHÍ" : ("x" + ConstructionManager.RushCostFor(remaining));

            yield return null;
        }

        timerCoroutine = null;
        ClearProcessStamps();
        SetState(PenState.Ready);
        SaveState();
    }

    private void StopTimerIfRunning()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    /// <summary>TRUE nếu itemId chính là túi cám của chuồng này (và config có khai báo).</summary>
    private bool LaThucAnCam(string itemId)
    {
        if (config == null || string.IsNullOrEmpty(config.premiumFoodItemId)) return false;
        return itemId == config.premiumFoodItemId;
    }

    /// <summary>Có đang nuôi bằng túi cám hay không (đọc activeFoodId nên sống qua save).</summary>
    private bool DangNuoiBangCam => LaThucAnCam(activeFoodId);

    /// <summary>
    /// Số đơn vị thức ăn cho một lượt, THEO TỪNG LOẠI.
    /// Túi cám đã cô đặc nhiều nông sản nên dùng số riêng (premiumFoodAmountPerFeed),
    /// thường = 1 thay vì 2-3 như nông sản thô.
    /// ⚠ Trước đây chỉ có một `FoodNeeded` dùng chung cho mọi ô ⇒ ô cám sẽ hiện "1/3" SAI,
    ///   và nhánh hoàn thức ăn lúc load save sẽ hoàn SAI số lượng.
    /// </summary>
    private int FoodNeededFor(string itemId)
    {
        if (config == null) return 1;
        return LaThucAnCam(itemId)
            ? Mathf.Max(1, config.premiumFoodAmountPerFeed)
            : Mathf.Max(1, config.foodAmountPerFeed);
    }

    private int FoodNeeded => FoodNeededFor(activeFoodId);

    /// <summary>
    /// Thời gian nuôi thực tế. Cho ăn túi cám thì CHIA cho premiumSpeedMultiplier
    /// (2 = nhanh gấp đôi). Đọc activeFoodId nên đóng game mở lại vẫn đúng mốc thời gian.
    /// </summary>
    public float EffectiveFeedSeconds
    {
        get
        {
            if (config == null) return 1f;
            float giay = config.feedDurationSeconds;
            return FarmManager.ScaleSeconds(giay);
        }
    }

    /// <summary>
    /// Giay con lai, do bang DONG HO THAT (UTC) nen song qua tat/mo game va qua tai lai scene.
    /// Dung processEndUnix (moc KET THUC) thay vi cong lai start + duration moi lan goi: neu
    /// designer doi feedDurationSeconds giua chung thi luot DANG chay khong bi nhay so.
    /// </summary>
    public float GetRemainingSeconds()
    {
        if (CurrentState != PenState.Processing || processEndUnix <= 0L) return 0f;
        long remain = processEndUnix - GetUnixNow();
        return remain > 0L ? (float)remain : 0f;
    }

    private void RefreshUI()
    {
        if (config == null) return;

        bool isIdle       = CurrentState == PenState.Idle;
        bool isProcessing = CurrentState == PenState.Processing;
        bool isReady      = CurrentState == PenState.Ready;

        Transform panelContent = panelRoot != null ? (panelRoot.transform.Find("PanelContent") ?? panelRoot.transform.Find("panelContent")) : null;
        if (panelContent != null)
        {
            panelContent.gameObject.SetActive(isIdle);
            RectTransform pcRect = panelContent.GetComponent<RectTransform>();
            if (pcRect != null)
            {
                // Thu nhỏ khung chứa thức ăn – gọn hơn, không che khuất chuồng
                pcRect.sizeDelta = new Vector2(110f, 110f);
            }
        }

        string feedItemId = !string.IsNullOrEmpty(config.food1ItemId) ? config.food1ItemId : config.premiumFoodItemId;
        Sprite feedIcon = config.food1Icon != null ? config.food1Icon : config.premiumFoodIcon;

        if (slot1Root != null)
        {
            slot1Root.SetActive(isIdle);
            if (isIdle)
            {
                RectTransform r = slot1Root.GetComponent<RectTransform>();
                if (r != null)
                {
                    r.anchoredPosition = Vector2.zero;
                    // Phóng to túi thức ăn để dễ kéo thả
                    r.sizeDelta = new Vector2(90f, 90f);
                }

                // Phóng to icon hình ảnh thức ăn bên trong slot
                if (slot1Icon != null)
                {
                    RectTransform iconRt = slot1Icon.GetComponent<RectTransform>();
                    if (iconRt != null) iconRt.sizeDelta = new Vector2(72f, 72f);
                }

                RefreshFoodSlot(slot1Icon, slot1Amount, feedItemId, feedIcon);

                var drag = slot1Root.GetComponent<DraggableFeedItem>();
                if (drag != null)
                {
                    drag.feedItemId = feedItemId;
                    drag.imgFeedIcon = slot1Icon;
                }
            }
        }

        if (slot2Root != null) slot2Root.SetActive(false);
        if (slot3Root != null) slot3Root.SetActive(false);

        if (basketRoot != null)
        {
            basketRoot.SetActive(isReady);
            RectTransform br = basketRoot.GetComponent<RectTransform>();
            if (br != null) br.anchoredPosition = Vector2.zero;

            if (basketActiveGlow != null)
                basketActiveGlow.SetActive(isReady);
        }

        if (progressOverlay != null)
        {
            progressOverlay.SetActive(isProcessing);

            // FIX LỖI BIẾN SẮC: Reset màu của toàn bộ Image trong overlay về trắng
            // Lỗi xảy ra do một số Image bị tinted (đổi màu) từ lần trước mà không reset lại
            if (isProcessing)
            {
                foreach (var img in progressOverlay.GetComponentsInChildren<Image>(true))
                {
                    // Chỉ reset nếu màu bị lệch khỏi trắng và không phải fillBar
                    if (img != progressFill && img.color.a > 0.05f)
                    {
                        Color c = img.color;
                        // Chỉ kéo RGB về trắng, giữ nguyên alpha để không làm mất hiệu ứng
                        img.color = new Color(1f, 1f, 1f, c.a);
                    }
                }

                progressOverlay.transform.SetAsLastSibling();
                if (processOverlayCanvas != null)
                {
                    processOverlayCanvas.overrideSorting = true;
                    processOverlayCanvas.sortingOrder    = processSortingOrder;
                }
                float remaining = GetRemainingSeconds();
                if (progressFill != null)
                    progressFill.fillAmount = 1f - remaining / Mathf.Max(1f, EffectiveFeedSeconds);
                if (progressLabel != null)
                    progressLabel.text = FormatTime(remaining);

                if (txtPenTitle != null && config != null)
                {
                    txtPenTitle.text = GetPenDisplayName();
                    txtPenTitle.color = Color.white;
                }
            }
            else
            {
                // Khi tắt overlay đi — reset sortingOrder để không bị sót
                if (processOverlayCanvas != null)
                    processOverlayCanvas.overrideSorting = false;
            }
        }

        EnsureGemButton();
        PlaceGemButton();
        if (_gemButtonGO != null)
        {
            _gemButtonGO.SetActive(isProcessing);
            if (isProcessing)
            {
                _gemButtonGO.transform.SetAsLastSibling();
                if (_gemCostText != null) _gemCostText.text = SpeedUpGemCost.ToString();
            }
        }
    }

    private void RefreshFoodSlot(Image iconImg, TMP_Text amtText, string itemId, Sprite fallbackIcon)
    {
        int amount = FarmInventoryManager.Instance != null
            ? FarmInventoryManager.Instance.GetAmount(itemId)
            : 0;

        if (iconImg != null && fallbackIcon != null)
            iconImg.sprite = fallbackIcon;

        if (amtText != null)
        {
            int need = FoodNeededFor(itemId);
            amtText.text = $"{amount}/{need}";
            amtText.color = amount >= need ? new Color(1f, 0.97f, 0.84f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
        }
    }

    private void PlayFeedVFX(string foodItemId, Vector3 vfxWorldPosition)
    {
        if (FarmCropVFXSpawner.Instance == null || config == null) return;

        Sprite feedIcon = config.food1Icon != null ? config.food1Icon : config.premiumFoodIcon;
        if (feedIcon != null)
            FarmCropVFXSpawner.Instance.PlayItemDropVFX(feedIcon, vfxWorldPosition, 1);
    }

    private void SpawnHarvestFX(string itemId, Sprite icon, int amount, Vector3 vfxWorldPosition)
    {
        if (icon == null) return;
        HarvestSlashFX.Spawn(vfxWorldPosition);
        HarvestFeedbackSpawner.Instance?.SpawnHarvestFly(icon, vfxWorldPosition, amount);
        FarmCropVFXSpawner.Instance?.PlayHarvestAmountVFX(amount, vfxWorldPosition);
    }

    private bool IsPointerOverPanel(Vector2 screenPos)
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return false;
        Camera cam = Camera.main;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    private void SaveState()
    {
        if (config == null) return;
        string id = config.penId;

        PlayerPrefs.SetInt(PrefKeyState + id, (int)CurrentState);
        PlayerPrefs.SetString(PrefKeyFood + id, activeFoodId ?? "");

        // [FIX 2026-09-06 vong6] TRUOC: processStartUnix.ToString("R") — KHONG truyen culture,
        // nen may cai tieng Viet ghi ra "1,7886528E+09"; con LoadState lai doc bang
        // InvariantCulture (NumberStyles.Float, KHONG co AllowThousands) => parse THAT BAI =>
        // moc = 0 => "00:00" vinh vien sau moi lan tai lai scene / mo lai game.
        // GIU NGUYEN ten khoa + kieu string de SaveAdapters / SaveData khong phai sua gi.
        if (CurrentState == PenState.Processing)
            PlayerPrefs.SetString(PrefKeyStartTime + id,
                processStartUnix.ToString(System.Globalization.CultureInfo.InvariantCulture));

        LuuGopPrefs.Hen();
    }

    private void LoadState()
    {
        if (config == null) return;
        string id = config.penId;

        bool coSaveCu = PlayerPrefs.HasKey(PrefKeyState + id);
        int verCu = SaveVersionGuard.Ensure(PenSaveFamily, PenSaveVersion, null, coSaveCu);

        int stateInt = PlayerPrefs.GetInt(PrefKeyState + id, (int)PenState.Idle);
        CurrentState = (PenState)stateInt;
        activeFoodId = PlayerPrefs.GetString(PrefKeyFood + id, "");

        processStartUnix = ParseUnixSeconds(PlayerPrefs.GetString(PrefKeyStartTime + id, "0"));
        long napDuration = (long)Mathf.Max(1f, Mathf.Ceil(EffectiveFeedSeconds));
        processEndUnix   = processStartUnix > 0L ? processStartUnix + napDuration : 0L;

        if (verCu < PenSaveVersion && coSaveCu && CurrentState == PenState.Processing)
        {
            if (!string.IsNullOrEmpty(activeFoodId) && FarmInventoryManager.Instance != null)
                FarmInventoryManager.Instance.AddItem(activeFoodId, FoodNeededFor(activeFoodId));

            CurrentState      = PenState.Idle;
            activeFoodId      = "";
            ClearProcessStamps();
            SaveState();
        }

        // ── RAO TU CHUA CHO SAVE CU ─────────────────────────────────────────────────
        // Save ghi "dang Processing" nhung moc ket thuc DA TROI QUA (dong game qua dem, hoac
        // save cu bi float lam tron / bi loi parse dau phay nen moc = 0). Truoc day chuong
        // dung im o "00:00" mai mai vi CHI coroutine moi day duoc sang Ready. Nay chuyen
        // thang sang Ready VA GHI LAI SAVE — ban cu quen SaveState nen mo game lan sau van
        // gap lai dung tinh trang do.
        if (CurrentState == PenState.Processing
            && (processStartUnix <= 0L || processEndUnix <= GetUnixNow()))
        {
            ClearProcessStamps();
            SetState(PenState.Ready);
            SaveState();
        }
    }

    public string GetPenDisplayName()
    {
        if (config != null && !string.IsNullOrEmpty(config.penName))
            return config.penName.ToUpper();

        if (config != null)
        {
            if (config.penId == "pen_01" || config.productItemId == "beef") return "CHUỒNG BÒ";
            if (config.penId == "pen_02" || config.productItemId == "pork") return "CHUỒNG HEO";
            if (config.penId == "pen_03" || config.productItemId == "chicken_meat" || config.secondProductItemId == "egg") return "CHUỒNG GÀ";
            if (config.penId == "pen_04" || config.productItemId == "milk") return "CHUỒNG BÒ SỮA";
        }

        return "CHUỒNG NUÔI";
    }

    /// <summary>Giay Unix (UTC) — CUNG mot cua voi PlotController / ConstructionManager.</summary>
    private static long GetUnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Doc moc thoi gian tu save. Chap nhan CA BA dang tung duoc ghi ra:
    ///   "1788652800"    — dang MOI (long, InvariantCulture)
    ///   "1.7886528E+09" — dang cu (float.ToString("R") tren may locale EN)
    ///   "1,7886528E+09" — dang cu tren may locale VI (dau phay). Ban cu parse bang
    ///                      InvariantCulture nen THAT BAI => moc = 0 => ket "00:00" vinh vien.
    /// </summary>
    private static long ParseUnixSeconds(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0L;
        var inv = System.Globalization.CultureInfo.InvariantCulture;

        if (long.TryParse(raw, System.Globalization.NumberStyles.Integer, inv, out long asLong))
            return asLong;

        if (double.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Float, inv,
                            out double asDouble) && asDouble > 0d)
            return (long)asDouble;

        return 0L;
    }

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }

    private GameObject _gemButtonGO;
    private TMP_Text   _gemCostText;
    private GameObject    _readyBubble;
    private RectTransform _readyBubbleRt;
    private Vector2       _readyBubbleBasePos;
    private float         _readyBubbleBobPhase;
    private float         _readyBubbleLastClickTime = -99f;

    /// <summary>
    /// Frame ma nguoi choi VUA nhan san pham bang bong bong. PenClickDetector doc co nay de
    /// KHONG mo tiep khay trong cung frame do: thu hoach xong state ve Idle, neu khong chan thi
    /// chinh cu click vua roi se bi hieu la "bam vao chuong dang doi" va mo khay cho an.
    /// </summary>
    private static int _bubbleHarvestFrame = -1;

    /// <summary>Vua nhan san pham bang bong bong trong frame nay?</summary>
    public static bool VuaThuBangBongBong => Time.frameCount <= _bubbleHarvestFrame;
    private static Sprite _roundSprite;
    private static Sprite _diamondSprite;

    private void EnsureGemButton()
    {
        Transform host = panelRoot != null ? panelRoot.transform : transform;
        if (host == null) return;
        if (_gemButtonGO != null)
        {
            if (_gemButtonGO.transform.parent != host)
                _gemButtonGO.transform.SetParent(host, false);
            return;
        }

        Transform existing = FindDeepChild(host, "btn_PenGem");
        if (existing != null)
        {
            _gemButtonGO = existing.gameObject;
            if (_gemButtonGO.transform.parent != host)
                _gemButtonGO.transform.SetParent(host, false);

            if (_gemCostText == null)
            {
                Transform costTf = FindDeepChild(_gemButtonGO.transform, "Txt_Cost");
                if (costTf != null) _gemCostText = costTf.GetComponent<TMP_Text>();
            }
            return;
        }

        Vector2 refSize = ReferenceSlotSize();
        var go = new GameObject("btn_PenGem", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(host, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(refSize.x * 1.25f, refSize.y * 0.72f);

        var img = go.GetComponent<Image>();
        img.sprite = gemButtonBgSprite != null ? gemButtonBgSprite : GetRoundSprite();
        img.type = Image.Type.Sliced;
        img.color = gemButtonBgSprite != null ? Color.white : new Color32(74, 154, 236, 255);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TrySpeedUpGem());

        var gemGO = new GameObject("Img_Gem", typeof(RectTransform), typeof(Image));
        gemGO.transform.SetParent(rt, false);
        var gemRt = (RectTransform)gemGO.transform;
        gemRt.sizeDelta = new Vector2(rt.sizeDelta.y * 0.7f, rt.sizeDelta.y * 0.7f);
        gemRt.anchoredPosition = new Vector2(-rt.sizeDelta.x * 0.24f, 0f);
        var gemImg = gemGO.GetComponent<Image>();
        gemImg.sprite = gemIconSprite != null ? gemIconSprite : GetDiamondSprite();
        gemImg.color = gemIconSprite != null ? Color.white : new Color32(150, 228, 255, 255);
        gemImg.preserveAspect = true;
        gemImg.raycastTarget = false;

        var txtGO = new GameObject("Txt_Cost", typeof(RectTransform));
        txtGO.transform.SetParent(rt, false);
        var txtRt = (RectTransform)txtGO.transform;
        txtRt.sizeDelta = new Vector2(rt.sizeDelta.x * 0.5f, rt.sizeDelta.y * 0.82f);
        txtRt.anchoredPosition = new Vector2(rt.sizeDelta.x * 0.18f, 0f);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = TangTocChuongDangMienPhi ? "MIỄN PHÍ" : ("x" + SpeedUpGemCost);
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 80;
        t.raycastTarget = false;
        _gemCostText = t;

        _gemButtonGO = go;
        PlaceGemButton();
    }

    private void PlaceGemButton()
    {
        if (_gemButtonGO == null) return;

        RectTransform rt = _gemButtonGO.GetComponent<RectTransform>();
        if (rt == null) return;

        RectTransform basketRt = BasketSlotRect;
        RectTransform progressRt = progressOverlay != null ? progressOverlay.GetComponent<RectTransform>() : null;
        Vector2 refSize = ReferenceSlotSize();

        if (basketRt != null)
            rt.anchoredPosition = basketRt.anchoredPosition;
        else if (progressRt != null)
            rt.anchoredPosition = progressRt.anchoredPosition + new Vector2(refSize.x * 0.95f, 0f);
        else
            rt.anchoredPosition = new Vector2(refSize.x * 0.95f, 0f);

        rt.sizeDelta = new Vector2(refSize.x * 1.25f, refSize.y * 0.72f);
    }

    private void UpdateReadyBubble()
    {
        if (config == null) return;
        EnsureReadyBubble();
        if (_readyBubble == null) return;

        // Chi hien o Ready. Idle / Processing => TAT (yeu cau cua Sep).
        bool hien = CurrentState == PenState.Ready;
        _readyBubble.SetActive(hien);
        if (hien) ApplyReadyBubblePlacement();
    }

    /// <summary>
    /// Bao nhieu WORLD unit cho MOT don vi local cua canvas chuong. Canvas nay la World Space
    /// (RenderMode 2, scale 0.01) nam duoi chuong scale 100 nen thuc te = 1.0. DO bang lossyScale
    /// chu khong go 1.0, de con dung neu Sep sua scale chuong.
    /// </summary>
    private float CanvasWorldUnitPerLocal()
    {
        Transform t = transform;
        float s = t != null ? Mathf.Abs(t.lossyScale.y) : 1f;
        return s > 0.00001f ? s : 1f;
    }

    /// <summary>
    /// Nhap nho nhe cho de thay. Dung unscaledTime vi popup co the dat timeScale = 0; bien do
    /// doi tu WORLD unit sang don vi canvas nen doi scale chuong khong lam giat.
    /// </summary>
    private void TickReadyBubbleBob()
    {
        if (_readyBubble == null || !_readyBubble.activeSelf) return;
        if (_readyBubbleRt == null) return;
        if (readyBubbleBobAmplitude <= 0.01f || readyBubbleBobPeriod <= 0.01f) return;

        float bienDo = readyBubbleBobAmplitude / CanvasWorldUnitPerLocal();
        float goc    = Time.unscaledTime * (Mathf.PI * 2f / readyBubbleBobPeriod) + _readyBubbleBobPhase;
        _readyBubbleRt.anchoredPosition = _readyBubbleBasePos + new Vector2(0f, Mathf.Sin(goc) * bienDo);
    }

    /// <summary>
    /// DO THAT roi moi dat: nang bong bong len tren dinh cao nhat quanh chuong, va gan sorting
    /// layer/order cao hon moi vat trang tri do duoc. KHONG BAO GIO ha thap hon
    /// readyBubbleLocalPos.y (so Sep da chinh tay trong prefab) - chi NANG len.
    /// </summary>
    /// <summary>
    /// GOC cua chuong (object mang BuildingFootprintKit / PenClickDetector). Panel co the
    /// nam sau vai tang Canvas nen `transform.parent` khong chac la goc — di nguoc len tim.
    /// </summary>
    private Transform LayGocChuong()
    {
        Transform t = transform;
        while (t != null)
        {
            if (t.GetComponent<BuildingFootprintKit>() != null || t.GetComponent<PenClickDetector>() != null)
                return t;
            t = t.parent;
        }
        return transform.root != null ? transform.root : transform;
    }

    private void ApplyReadyBubblePlacement()
    {
        if (_readyBubble == null || _readyBubbleRt == null) return;

        float donVi = CanvasWorldUnitPerLocal();
        Canvas canvas = _readyBubble.GetComponent<Canvas>();

        // ---- 1. Layer: cao nhat CO THAT (Foreground), khong bao gio hardcode ten ----
        if (canvas != null)
        {
            canvas.overrideSorting  = true;
            canvas.sortingLayerName = TouristSortingLayers.ResolveOrOverride(
                readyBubbleSortingLayer, TouristSortingLayers.Overlay);
        }

        // ---- 2. Do chieu cao + order THAT cua chuong ----
        float dinhWorldY;
        int   orderCaoNhat;
        bool  doDuoc = DoDinhVaOrderQuanhChuong(out dinhWorldY, out orderCaoNhat);

        // ---- 3. Order = max(so trong Inspector, order do duoc + bien an toan) ----
        if (canvas != null)
        {
            int order = readyBubbleSortingOrder;
            if (doDuoc) order = Mathf.Max(order, orderCaoNhat + Mathf.Max(0, readyBubbleOrderMargin));
            canvas.sortingOrder = Mathf.Clamp(order, 0, ReadyBubbleOrderMax);
        }

        // ---- 4. Vi tri: dat ngay tren dinh chuong, cach vua du de khong che ga va khong bay len kho ----
        float nuaCaoWorld = _readyBubbleRt.sizeDelta.y * 0.5f * donVi;
        Transform goc = LayGocChuong();
        float clearance = 65f; // Khoang cach phia tren dinh chuong/ga
        float tamWorldY = doDuoc
            ? (dinhWorldY + clearance + nuaCaoWorld)
            : (goc.position.y + 350f + nuaCaoWorld);

        Vector3 wp = _readyBubbleRt.position;
        _readyBubbleRt.position = new Vector3(goc.position.x, tamWorldY, wp.z);
        _readyBubbleBasePos = _readyBubbleRt.anchoredPosition; // bob nhun tiep tu day

        { Debug.Log($"[Pen] {(config != null ? config.penId : "?")} BONGBONG_DAT layer={(canvas != null ? canvas.sortingLayerName : "?")} order={(canvas != null ? canvas.sortingOrder : 0)} yWorld={_readyBubbleRt.position.y:F0} donVi={donVi:F3} dinhDo={(doDuoc ? dinhWorldY.ToString("F0") : "khongDo")} orderDo={(doDuoc ? orderCaoNhat.ToString() : "khongDo")}"); }
    }

    /// <summary>
    /// Do THAT chieu cao va sorting order cua CHINH chuong (BarnSprite + con vat):
    ///   * <paramref name="dinhWorldY"/> = canh TREN cao nhat cua chuong/con vat (world unit)
    ///   * <paramref name="orderCaoNhat"/> = sortingOrder lon nhat trong so do
    /// Tra false khi khong do duoc gi.
    /// </summary>
    private bool DoDinhVaOrderQuanhChuong(out float dinhWorldY, out int orderCaoNhat)
    {
        dinhWorldY   = 0f;
        orderCaoNhat = 0;
        bool coSoLieu = false;

        Transform penRoot = transform.parent != null ? transform.parent : transform;

        SpriteRenderer[] cuaChuong = penRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < cuaChuong.Length; i++)
        {
            SpriteRenderer sr = cuaChuong[i];
            if (sr == null || sr.sprite == null) continue;
            if (!sr.enabled || !sr.gameObject.activeInHierarchy) continue;

            Bounds bd = sr.bounds;
            if (!coSoLieu || bd.max.y > dinhWorldY) dinhWorldY = bd.max.y;
            if (!coSoLieu || sr.sortingOrder > orderCaoNhat) orderCaoNhat = sr.sortingOrder;
            coSoLieu = true;
        }

        return coSoLieu;
    }

    /// <summary>
    /// CLICK 1 CAI LA NHAN (yeu cau cua Sep). KHONG viet lai logic thu hoach - goi thang
    /// <see cref="TryHarvest"/> da co.
    ///
    /// VI SAO dung BlockWorldClickBySceneOrPopup CHU KHONG dung BlockWorldInteraction:
    /// BlockWorldInteraction co goi FarmInputLock.ConTroTrenUiThat(), ham nay tra TRUE khi con
    /// tro nam tren BAT KY graphic co GraphicRaycaster - ma chinh bong bong nay LA mot graphic
    /// nhu vay. Dung no o day thi bong bong TU CHAN chinh no, bam mai khong an. Cong
    /// BlockWorldClickBySceneOrPopup duoc viet dung cho tinh huong "con tro dang tren collider
    /// cua chinh vat do": van chan khi dang o Bep / mo popup / keo hat / keo liem / Edit Mode.
    /// </summary>
    private void OnReadyBubbleClicked()
    {
        if (CurrentState != PenState.Ready) return;
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;

        // Chong bam kep: 2 su kien click trong cung nhip khong the cong doi san pham.
        if (Time.unscaledTime < _readyBubbleLastClickTime + ReadyBubbleClickCooldown) return;
        _readyBubbleLastClickTime = Time.unscaledTime;

        // An bong bong NGAY, truoc khi thu hoach: khong con gi de bam lan hai.
        if (_readyBubble != null) _readyBubble.SetActive(false);

        _bubbleHarvestFrame = Time.frameCount;
        FarmInputLock.SuppressWorldClickForCurrentFrame();

        bool nhanDuoc = TryHarvest(transform.position);

        // Kho day / thu that bai => tra bong bong lai cho nguoi choi bam lan sau.
        if (!nhanDuoc) UpdateReadyBubble();

        { Debug.Log($"[Pen] {(config != null ? config.penId : "?")} BONGBONG_CLICK nhan={nhanDuoc} state={CurrentState} frame={Time.frameCount}"); }
    }

    private void EnsureReadyBubble()
    {
        if (_readyBubble != null) return;
        RectTransform host = GetComponent<RectTransform>();
        if (host == null) return;

        Vector2 refSize = ReferenceSlotSize();
        bool two = !string.IsNullOrEmpty(config.secondProductItemId) && config.secondProductIcon != null;
        float w = refSize.x * (two ? 2.1f : 1.35f);
        float h = refSize.y * 1.3f;

        var go = new GameObject("PenReadyBubble", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(host, false);
        var rt = (RectTransform)go.transform;

        // Neo GIUA ro rang: mac dinh cua RectTransform tao bang code la goc duoi-trai, khi do
        // anchoredPosition KHONG con bang localPosition va phep doi world <-> local ben
        // ApplyReadyBubblePlacement() se lech nua khung. Chot anchor/pivot = 0.5 cho chac.
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = readyBubbleLocalPos;

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting  = true;
        canvas.sortingLayerName = TouristSortingLayers.ResolveOrOverride(
            readyBubbleSortingLayer, TouristSortingLayers.Overlay);
        canvas.sortingOrder     = Mathf.Clamp(readyBubbleSortingOrder, 0, ReadyBubbleOrderMax);
        go.AddComponent<GraphicRaycaster>();

        var img = go.GetComponent<Image>();
        img.sprite = readyBubbleBgSprite != null ? readyBubbleBgSprite : GetRoundSprite();
        img.type = Image.Type.Sliced;
        img.color = readyBubbleBgSprite != null ? Color.white : new Color32(255, 246, 214, 255);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnReadyBubbleClicked);

        if (config.productIcon != null)
        {
            var p1 = new GameObject("Icon_Product1", typeof(RectTransform), typeof(Image));
            p1.transform.SetParent(rt, false);
            var p1Rt = (RectTransform)p1.transform;
            p1Rt.sizeDelta = new Vector2(h * 0.72f, h * 0.72f);
            p1Rt.anchoredPosition = two ? new Vector2(-w * 0.23f, 0f) : Vector2.zero;
            var p1Img = p1.GetComponent<Image>();
            p1Img.sprite = config.productIcon;
            p1Img.preserveAspect = true;
            p1Img.raycastTarget = false;
        }

        if (two)
        {
            var p2 = new GameObject("Icon_Product2", typeof(RectTransform), typeof(Image));
            p2.transform.SetParent(rt, false);
            var p2Rt = (RectTransform)p2.transform;
            p2Rt.sizeDelta = new Vector2(h * 0.72f, h * 0.72f);
            p2Rt.anchoredPosition = new Vector2(w * 0.23f, 0f);
            var p2Img = p2.GetComponent<Image>();
            p2Img.sprite = config.secondProductIcon;
            p2Img.preserveAspect = true;
            p2Img.raycastTarget = false;
        }

        _readyBubble        = go;
        _readyBubbleRt      = rt;
        _readyBubbleBasePos = readyBubbleLocalPos;

        // Lech pha theo tung chuong de 4 chuong khong nhap nho dong loat nhu may.
        _readyBubbleBobPhase = (Mathf.Abs(GetInstanceID()) % 100) * 0.0628f;
    }

    private Vector2 ReferenceSlotSize()
    {
        if (slot1Root != null)
        {
            var rt = slot1Root.GetComponent<RectTransform>();
            if (rt != null && rt.sizeDelta.sqrMagnitude > 1f) return rt.sizeDelta;
        }
        return new Vector2(100f, 100f);
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite GetRoundSprite()
    {
        if (_roundSprite != null) return _roundSprite;
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                tex.SetPixel(x, y, d <= r ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        _roundSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0,
            SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
        return _roundSprite;
    }

    private static Sprite GetDiamondSprite()
    {
        if (_diamondSprite != null) return _diamondSprite;
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float manhattan = Mathf.Abs(x + 0.5f - half) + Mathf.Abs(y + 0.5f - half);
                tex.SetPixel(x, y, manhattan <= half ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        _diamondSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _diamondSprite;
    }
}
