using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
//  LevelUpPopupUI — popup "LÊN CẤP!" (phong cách Township)
//  [V6 2026-09-05] Bố trí vùng đáy: BoTriVungDuoi() chạy cuối PopulateUI, ép
//  Dai_MoKhoa y=-215, Text_Hint (hintText) y=-385 (1100×66, autosize 20–28, 2 dòng),
//  Btn_TiepTuc y=-500; tắt lớp mờ trùng V3_DimBackground (chỉ giữ Bg_NenToi 0.65),
//  Nen_Dai màu kem (255,243,214,235). Text_MoKhoa (unlockDescText) giữ field, luôn ẩn.
//  Khi useUIFireworks = true không còn spawn/replay vfxSidePrefab (ParticleSystem
//  không bao giờ vẽ nổi lên Overlay canvas → chỉ tốn CPU). Tool Township/Rewire đã
//  đổi cùng số để chạy lại tool không phá bố cục này.
// =============================================================================

public class LevelUpPopupUI : MonoBehaviour
{
    // =========================================================================
    // Inspector
    // =========================================================================

    [Header("Level Reward Configs (1 asset mỗi level)")]
    [SerializeField] private List<LevelRewardConfig> levelRewardConfigs = new List<LevelRewardConfig>();

    [Header("Root & Fade")]
    [SerializeField] private GameObject   popupRoot;
    [SerializeField] private CanvasGroup  canvasGroup;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Gold / Gems Display")]
    [SerializeField] private GameObject      goldRewardRow;
    [SerializeField] private TextMeshProUGUI goldRewardText;
    [SerializeField] private GameObject      gemRewardRow;
    [SerializeField] private TextMeshProUGUI gemRewardText;

    [Header("Gift Items Container")]
    [SerializeField] private Transform  giftItemsContainer;
    [SerializeField] private GameObject giftItemSlotPrefab;

    [Header("Unlock Descriptions")]
    [SerializeField] private TextMeshProUGUI unlockDescText;

    // ─────────────────────────────────────────────────────────────────────────
    //  DẢI Ô "VỪA MỞ KHOÁ"
    //  VÌ SAO CÓ CẢ container LẪN mảng?
    //  ---------------------------------------------------------------------
    //  Ưu tiên 1 = unlockSlotsContainer (tự dò UnlockSlotUI bên trong).
    //     BỀN HƠN vì: tool LevelUpPopupTownshipTool dựng lại popup là DestroyImmediate
    //     toàn bộ cây cũ → mọi phần tử trong mảng thành null "mồ côi", còn 1 tham chiếu
    //     container thì chỉ cần gán lại 1 lần. Designer thêm/bớt ô trong Hierarchy
    //     cũng KHÔNG phải sửa mảng, chạy vẫn đúng.
    //  Ưu tiên 2 = unlockSlots[] (dự phòng): dùng khi các ô không có 1 cha chung,
    //     hoặc designer muốn tự quyết định thứ tự hiện.
    // ─────────────────────────────────────────────────────────────────────────
    [Header("Unlock Slots — dải ô tròn 'vừa mở khoá'")]
    [Tooltip("Cha của các ô mở khoá (thường là Dai_MoKhoa/ScrollView/Viewport/Content). " +
             "Script tự dò UnlockSlotUI bên trong — ƯU TIÊN cách này.")]
    [SerializeField] private Transform unlockSlotsContainer;

    [Tooltip("Danh sách ô mở khoá gán tay. CHỈ dùng khi unlockSlotsContainer để trống.")]
    [SerializeField] private UnlockSlotUI[] unlockSlots;

    [Tooltip("Nền cả dải mở khoá (Dai_MoKhoa). Tự ẩn khi level không mở khoá gì " +
             "→ không để lại thanh nền tối rỗng. Có thể để trống.")]
    [SerializeField] private GameObject unlockStripRoot;

    [Header("Buttons")]
    [SerializeField] private Button claimButton;

    // [V2 ADD] ────────────────────────────────────────────────────────────────
    //  BỘ "JUICE" V2 — nhân vật ăn mừng + sparkle thuần code + chạm-để-đóng.
    //  Tool dựng & nối dây: Tools ▸ Farm Game ▸ Level Up Popup ▸ ★ Nâng cấp V2 (1 nút)
    // ─────────────────────────────────────────────────────────────────────────
    [Header("V2 — Nhân vật ăn mừng / Sparkle / Chạm-để-đóng")]
    [Tooltip("[V2] 4 slot nhân vật nhảy múa quanh badge (2 trái 2 phải). " +
             "Slot nào chưa có frames sẽ TỰ ẨN — an toàn khi chờ art.")]
    [SerializeField] private CelebrationCharacterSlot[] celebrationSlots;   // [V2 ADD]

    [Tooltip("[V2] Bộ tia sáng quay + sparkle 4 cánh + glow pulse (vẽ runtime, không cần art).")]
    [SerializeField] private LevelUpSparkleFX sparkleFx;                    // [V2 ADD]

    [Tooltip("[V2] Overlay trong suốt bắt tap toàn màn hình để đóng popup.")]
    [SerializeField] private LevelUpTapToClose tapCatcher;                  // [V2 ADD]

    [Tooltip("[V2] Bật/tắt tính năng chạm bất kỳ đâu để nhận quà + đóng popup.")]
    [SerializeField] private bool tapAnywhereToClose = true;                // [V2 ADD]

    [Header("VFX")]
    [Tooltip("LanaDemo02 – confetti bắn từ trên (Confetti_blast_multicolor)")]
    [SerializeField] private GameObject vfxConfettiPrefab;
    [SerializeField] private Transform  vfxSpawnPoint;

    [Tooltip("LanaDemo03 – flash 2 bên (Flash_magic_blue_pink hoặc tương đương)")]
    [SerializeField] private GameObject vfxSidePrefab;
    [SerializeField] private Transform  vfxLeftPoint;
    [SerializeField] private Transform  vfxRightPoint;

    [Header("VFX Screen Composition")]
    [SerializeField] private float vfxTopPanelGap = 70f;
    [SerializeField] private float vfxSidePanelGap = 130f;
    [SerializeField] private float vfxSideVerticalOffset = 70f;
    [SerializeField] private float vfxTopDemoScale = 0.5f;
    [SerializeField] private float vfxSideDemoScale = 0.38f;
    // vfxLifetime đã bỏ: VFX sống theo vòng đời popup (destroy khi popup đóng),
    // không tự hết hạn theo giây nữa.

    [Header("VFX Intensity — bùm bùm rầm rộ tới khi nhận quà")]
    [Tooltip("Phóng to pháo hoa (confetti) phía trên")]
    [SerializeField] private float vfxScaleBoost        = 2.0f;
    [Tooltip("Phóng to Lana03 hai bên (to hơn confetti)")]
    [SerializeField] private float vfxSideScaleBoost    = 2.8f;
    [Tooltip("Nhân số lượng particle (nhiều hơn)")]
    [SerializeField] private float vfxEmissionMultiplier = 2.5f;
    [Tooltip("Khoảng cách giữa các lần bùm (giây). Lặp tới khi user bấm Nhận Quà.")]
    [SerializeField] private float vfxBurstInterval      = 0.6f;

    [Header("UI Fireworks (fix Overlay Canvas luôn vẽ sau cùng, đè lên ParticleSystem)")]
    [Tooltip("TRUE: pháo hoa dựng bằng UI (Image, con của popup) → luôn nổi trên popup. " +
             "FALSE: dùng lại ParticleSystem cũ (vfxConfettiPrefab) — công tắc revert an toàn.")]
    [SerializeField] private bool useUIFireworks = true;
    [Tooltip("Sprite hạt pháo hoa UI (VD: confetti_01..06, spark_star sau khi import vào Assets). " +
             "Để trống → tự sinh khối màu phẳng theo bảng màu lễ hội.")]
    [SerializeField] private Sprite[] fireworkSprites;

    [Tooltip("[V3 2026-09-04 - Sep yeu cau] TRUE: phao hoa ban o LOP RIENG phu TOAN khung popup, " +
             "nam TREN ca nen mo lan card => no chung mot khung hinh, khong bi card che. " +
             "FALSE: ve hanh vi cu (con cua contentPanel - no gon trong card, an theo scale card).")]
    [SerializeField] private bool fireworksOnTopLayer = true;

    [Tooltip("[V3] Nhan toc do bay cua hat. Lop phu toan man rong hon card nhieu nen hat can bay " +
             "xa hon thi moi lap day khung. 1.0 = nhu cu. Thay qua da thi ha ve 1.2.")]
    [Range(0.5f, 3f)]
    [SerializeField] private float fireworkSpreadBoost = 1.55f;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration  = 0.25f;
    [SerializeField] private float scaleInDuration = 0.3f;
    [SerializeField] private RectTransform contentPanel;

    // =========================================================================
    // Runtime
    // =========================================================================

    /// <summary>TRUE khi đang có popup lên cấp hiển thị (từ lúc bật tới khi user bấm "Nhận" đóng hẳn).
    /// Tutorial dùng cờ này để "nhường sân khấu" — chờ user nhận quà rồi mới chạy bước tiếp.</summary>
    public static bool IsActive { get; private set; }

    private readonly Queue<int> _levelUpQueue = new Queue<int>();
    private bool                _isShowing    = false;
    private int                 _lastKnownLevel;
    private LevelRewardConfig   _currentConfig;
    private bool                _inputLockHeld;
    private GameObject          _activeVfxRoot;
    private Coroutine           _vfxLoop;
    /// <summary>[V3] Ten lop phu toan khung chua phao hoa - dung de tim lai, tranh tao trung.</summary>
    private const string kFireworksLayerName = "FX_Fireworks_Layer";

    private GameObject          _activeUiFireworksRoot;   // container "FX_Fireworks_UI" (con popup)
    private Coroutine           _uiFireworksRoutine;

    // Danh sách ô mở khoá đã dò xong (tránh GetComponentsInChildren mỗi lần mở popup).
    private UnlockSlotUI[]      _unlockSlotsCache;
    // Số ô cần chạy hiệu ứng "bung ra". Phải HOÃN tới sau khi popupRoot bật —
    // xem PlayUnlockPops() để biết lý do.
    private int                 _pendingUnlockPopCount;
    private bool                _warnedNoUnlockSlots;

    // [V2 ADD] Chống nhận quà 2 lần khi tap màn hình và bấm nút gần như đồng thời
    // (tap-to-close + claimButton đều dẫn về ClaimAndClose). Reset mỗi lần mở popup.
    private bool                _v2Closing;

    // [R2 GỘP] ────────────────────────────────────────────────────────────────
    //  MỘT KHU PHẦN THƯỞNG DUY NHẤT (lệnh Sếp 02/09): các ô quà KHÔNG dàn hàng
    //  riêng giữa popup nữa mà nằm CHUNG khung trắng dưới (Dai_MoKhoa) với các ô
    //  "vừa mở khoá" NEW — một grid flow, tự xuống hàng, căn giữa.
    //  _mergedContainer = Dai_MoKhoa/ScrollView/Viewport/Content (suy từ
    //  unlockSlotsContainer đã nối dây, hoặc từ cha của ô mở khoá đầu tiên).
    // ─────────────────────────────────────────────────────────────────────────
    private RectTransform _mergedContainer;      // khu phần thưởng gộp; null = giữ đường cũ
    private RectTransform _preparedContainer;    // container ĐÃ tắt HLG/CSF/ScrollRect — so theo instance,
                                                 // vì tool Township có thể DestroyImmediate + dựng lại popup giữa phiên
    // [B4 GỘP Q1 — 2026-09-05] Ô vàng/gem/quà nay là UnlockSlotUI dựng runtime (IsRuntimeCell=true)
    // → cùng pop + bob + tag + tên với ô mở khoá. Không dùng LevelUpGiftSlotUI trong khu gộp nữa.
    private readonly List<UnlockSlotUI> _mergedCells = new List<UnlockSlotUI>();
    private Coroutine     _mergedLayoutRoutine;

    // [B5 PHÁO HOA LẶP — 2026-09-05] Mỗi "bùm" là 1 root con của _activeUiFireworksRoot, tự huỷ
    // khi tàn; vòng lặp VongLapPhaoHoa bắn tiếp mỗi 0.8–1.2s, tối đa 3 bùm cùng lúc.
    private readonly List<GameObject> _fireworkBurstRoots    = new List<GameObject>();
    private readonly List<Coroutine>  _fireworkBurstRoutines = new List<Coroutine>();
    private const int   kMaxConcurrentBursts = 3;

    // [B6 NHỚ ĐÃ XEM — 2026-09-05] Cấp cao nhất user đã bấm "Nhận" — lưu PlayerPrefs để popup chưa
    // xem (tắt game giữa lúc đang hiện) tự hiện lại khi mở lại. Thiếu key = save cũ → coi như đã xem tới cấp hiện tại.
    private const string kSeenKey = "LEVELUP_SEEN_MAX";
    private int          _currentShownLevel;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);

        if (PlayerProgressManager.Instance != null)
        {
            int capHienTai = PlayerProgressManager.Instance.Level;
            _lastKnownLevel = capHienTai;
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;

            // [B6] Popup chưa xem bị mất khi thoát game (queue chỉ nằm RAM) → so với mốc đã lưu.
            int daXem = PlayerPrefs.GetInt(kSeenKey, 0);
            if (daXem <= 0)
            {
                // Save cũ chưa có key → KHÔNG replay hết L2..L{n}; coi như đã xem tới cấp hiện tại.
                daXem = capHienTai;
                PlayerPrefs.SetInt(kSeenKey, daXem);
            }
            if (capHienTai > daXem && capHienTai > 1)
            {
                for (int l = daXem + 1; l <= capHienTai; l++) _levelUpQueue.Enqueue(l);
                Debug.Log($"[LevelUpPopupUI] Có {capHienTai - daXem} popup lên cấp chưa xem (L{daXem + 1}→L{capHienTai}) → hiện lại.");
                StartCoroutine(CoHienLaiPopupChuaXem());
            }
        }
        else
        {
            Debug.LogWarning("[LevelUpPopupUI] PlayerProgressManager.Instance không tìm thấy tại Start(). " +
                             "Đặt PlayerProgressManager vào scene trước LevelUpPopupUI.");
        }

        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimAndClose);
    }

    /// <summary>[B6] Đợi scene ổn định 1 nhịp (HUD/tutorial Start xong) rồi mới hiện popup tồn đọng.</summary>
    private IEnumerator CoHienLaiPopupChuaXem()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        if (!_isShowing && _levelUpQueue.Count > 0) ShowNextPopup();
    }

    /// <summary>[B6] Ghi mốc "đã xem" khi user bấm Nhận. Không vượt cấp thật của người chơi
    /// (DebugShowLevel(30) không được đánh dấu đã xem cấp chưa tới).</summary>
    private void GhiNhoDaXem(int level)
    {
        if (level <= 0) return;
        int capThat = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : level;
        int ghi = Mathf.Min(level, capThat);
        if (ghi > PlayerPrefs.GetInt(kSeenKey, 0))
        {
            PlayerPrefs.SetInt(kSeenKey, ghi);
            PlayerPrefs.Save();
        }
    }

    private void OnDestroy()
    {
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;

        IsActive = false;   // tránh kẹt cờ nếu popup bị huỷ giữa chừng
        StopVFX();
        StopV2Fx();         // [V2 ADD] dừng sparkle/nhân vật/tap-catcher khi popup bị huỷ
        if (_mergedLayoutRoutine != null) { StopCoroutine(_mergedLayoutRoutine); _mergedLayoutRoutine = null; }   // [R2 GỘP]
        ReleaseInputLock();
    }

    private void OnDisable()
    {
        StopVFX();
    }

    // =========================================================================
    // Event Handler
    // =========================================================================

    private void HandleLevelChanged(int newLevel)
    {
        // Bỏ qua lần gọi đầu tiên khi Start() đồng bộ UI; và khi reset xuống (vd về L1)
        // → đồng bộ lại mốc để lần lên cấp sau vẫn hiện popup + pháo hoa.
        if (newLevel <= _lastKnownLevel) { _lastKnownLevel = newLevel; return; }

        _lastKnownLevel = newLevel;
        _levelUpQueue.Enqueue(newLevel);

        if (!_isShowing)
            ShowNextPopup();
    }

    // =========================================================================
    // Show Logic
    // =========================================================================

    /// <summary>
    /// Cho phép công cụ Timeline Studio / QA mở popup lên cấp của bất kỳ level nào để preview đồ họa và phần thưởng.
    /// </summary>
    public void DebugShowLevel(int level)
    {
        _levelUpQueue.Clear();
        _levelUpQueue.Enqueue(level);
        _lastKnownLevel = level;
        ShowNextPopup();
    }

    private void ShowNextPopup()
    {
        if (_levelUpQueue.Count == 0)
        {
            _isShowing = false;
            IsActive   = false;          // hết popup → tutorial được phép chạy tiếp
            return;
        }

        int level = _levelUpQueue.Dequeue();
        _currentShownLevel = level;   // [B6] để ClaimAndClose ghi mốc đã xem
        _currentConfig = levelRewardConfigs.Find(c => c != null && c.levelReached == level);

        // ── CHỐT 1: popupRoot chưa gán ───────────────────────────────────
        if (popupRoot == null)
        {
            Debug.LogError("[LevelUpPopupUI] popupRoot = NULL → popup không thể hiện. " +
                           "Dựng lại bằng Tools ▸ Farm ▸ Popup Lên Cấp (Township).");
            _isShowing = false; IsActive = false;
            return;
        }

        _isShowing    = true;
        IsActive      = true;            // có popup đang hiện → tutorial chờ
        _v2Closing    = false;           // [V2 ADD] popup mới → mở lại cửa ClaimAndClose

        PopulateUI(level, _currentConfig);
        popupRoot.SetActive(true);

        // ── CHỐT 2: có tổ tiên đang TẮT → SetActive(true) vô nghĩa ───────
        // Đây là lỗi đã từng xảy ra: popup bị dựng vào Canvas World Space nằm trong
        // prefab nhà, mà HouseOrderBubble.Awake() tắt object đó → activeInHierarchy
        // luôn false → coroutine không chạy, popup vô hình. Tuyệt đối KHÔNG được
        // để _isShowing/IsActive kẹt true, nếu không lần gọi sau sẽ im lặng hoàn toàn
        // và tutorial bị chặn vĩnh viễn.
        if (!popupRoot.activeInHierarchy)
        {
            Transform bad = transform;
            while (bad != null && bad.gameObject.activeSelf) bad = bad.parent;

            var cv = GetComponentInParent<Canvas>(true);

            Debug.LogError(
                $"[LevelUpPopupUI] '{popupRoot.name}' đã SetActive(true) nhưng activeInHierarchy = FALSE.\n" +
                $"   • Tổ tiên đang TẮT : '{(bad != null ? bad.name : "?")}'\n" +
                $"   • Canvas gần nhất  : '{(cv != null ? cv.name : "KHÔNG CÓ")}'" +
                $" (renderMode = {(cv != null ? cv.renderMode.ToString() : "?")})\n" +
                $"   • Đường dẫn        : {HierarchyPath(transform)}\n" +
                $"   • lossyScale       : {transform.lossyScale} (phải ≈ 1,1,1)\n" +
                "→ Popup đang nằm sai chỗ. Dựng lại vào Canvas_Popup (Screen Space Overlay).");

            popupRoot.SetActive(false);
            _isShowing = false; IsActive = false;
            return;   // KHÔNG AcquireInputLock — tránh kẹt luôn thao tác kéo map
        }

        AcquireInputLock();
        SpawnVFX();
        AudioManager.Instance?.PlayLevelUpFanfare();

        // Chỉ tới ĐÂY các ô mở khoá mới thật sự activeInHierarchy → mới chạy được
        // coroutine hiệu ứng. Gọi sớm hơn (trong PopulateUI) là vô tác dụng.
        PlayUnlockPops();

        // [R2 GỘP] Xếp khu phần thưởng gộp SAU 1 frame: Destroy() ô cũ chỉ thật sự
        // biến mất cuối frame, và rect của khung trắng cần canvas cập nhật xong.
        // 1 frame trống được AnimateIn (alpha 0 → 1 trong 0.25s) che kín, mắt không thấy.
        if (_mergedContainer != null)
        {
            if (_mergedLayoutRoutine != null) StopCoroutine(_mergedLayoutRoutine);
            _mergedLayoutRoutine = StartCoroutine(CoLayoutMergedRewards());
        }

        // [V2 ADD] Bật bộ juice V2 — cũng phải nằm SAU popupRoot.SetActive(true)
        // vì sparkleFx / celebrationSlots đều StartCoroutine trên object con của popup.
        StartV2Fx();

        StartCoroutine(AnimateIn());
    }

    /// <summary>Đường dẫn hierarchy đầy đủ, dùng cho log lỗi.</summary>
    private static string HierarchyPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + " / " + p; }
        return p;
    }

    private void PopulateUI(int level, LevelRewardConfig cfg)
    {
        // Title
        if (titleText != null)
            titleText.text = "LÊN CẤP!";
        if (levelNumberText != null)
            levelNumberText.text = Mathf.Clamp(level, 1, 30).ToString();

        // Clear gift slots
        if (giftItemsContainer != null)
            foreach (Transform child in giftItemsContainer)
                if (child.GetComponent<LevelUpGiftSlotUI>() != null)
                    Destroy(child.gameObject);

        // [R2 GỘP] Ô quà của popup TRƯỚC nằm trong khu gộp (khung trắng dưới) → dọn nốt.
        ClearMergedGiftCells();

        // Dải ô "vừa mở khoá" — gọi cho CẢ hai trường hợp cfg có / không có:
        // cfg == null vẫn phải vào để ẩn hết ô, nếu không sẽ còn lại ô trắng trơn.
        ApplyUnlockSlots(cfg);

        if (cfg != null)
        {
            // Gold row
            bool hasGold = cfg.giftGold > 0;
            if (goldRewardRow  != null) goldRewardRow.SetActive(hasGold);
            if (goldRewardText != null) goldRewardText.text = $"+{cfg.giftGold}";

            // Gem row
            bool hasGem = cfg.giftGems > 0;
            if (gemRewardRow  != null) gemRewardRow.SetActive(hasGem);
            if (gemRewardText != null) gemRewardText.text = $"+{cfg.giftGems}";

            // Gift item slots — dùng GetGiftItemsToShow(), KHÔNG dùng thẳng cfg.giftItems.
            // Món nào đã hiện ở dải ô tròn "vừa mở khoá" thì không vẽ lại ô quà nữa,
            // nếu không cùng một icon xuất hiện hai lần trong cùng một popup.
            // Vật phẩm vẫn được tặng đủ khi bấm "Nhận" (chỗ đó vẫn duyệt cfg.giftItems).
            var quaCanVe = cfg.GetGiftItemsToShow();

            // [V4 ADD] Vàng + Kim cương thành 2 Ô QUÀ TRÒN đầu dải (Sếp muốn thấy
            // NHIỀU quà). 2 entry này CHỈ ĐỂ VẼ — grant thật vẫn qua giftGold/giftGems
            // trong GrantRewards, itemId "__gold"/"__gem" không bao giờ vào kho.
            var quaHienThi = new List<LevelRewardConfig.ItemGift>(quaCanVe.Count + 2);
            if (cfg.giftGold > 0)
                quaHienThi.Add(new LevelRewardConfig.ItemGift
                {
                    itemId = LevelUpRewardIconResolver.GoldId, displayName = "Vàng", amount = cfg.giftGold,
                    // [R2 ICON] TimIconVangV4 (library → HUD) vẫn là nguồn chính; miss thì
                    // resolver lo tiếp + log [LevelUp] một lần (trước đây miss là ô trống câm).
                    icon = LevelUpRewardIconResolver.Resolve(LevelUpRewardIconResolver.GoldId, TimIconVangV4(), "Vàng"),
                });
            if (cfg.giftGems > 0)
                quaHienThi.Add(new LevelRewardConfig.ItemGift
                {
                    itemId = LevelUpRewardIconResolver.GemId, displayName = "Kim cương", amount = cfg.giftGems,
                    icon = LevelUpRewardIconResolver.Resolve(LevelUpRewardIconResolver.GemId, TimIconGemV4(), "Kim cương"),
                });

            // [R2 ICON] Icon đúng 100% cho mọi loại quà: gift.icon trong asset →
            // RewardIconLibrary (tiền tệ) → StallItemCatalog (icon của chính data item
            // theo id) → null (placeholder + warning [LevelUp] một lần trong resolver).
            // TẠO BẢN SAO ĐỂ VẼ, không gán ngược vào cfg.giftItems — ItemGift trong
            // quaCanVe là THAM CHIẾU thẳng vào asset, ghi icon vào đó là sửa asset
            // trong RAM (Editor sẽ giữ thay đổi đó, thành sửa .asset chui).
            for (int i = 0; i < quaCanVe.Count; i++)
            {
                var g = quaCanVe[i];
                if (g == null) continue;
                quaHienThi.Add(new LevelRewardConfig.ItemGift
                {
                    itemId      = g.itemId,
                    displayName = g.displayName,
                    amount      = g.amount,
                    icon        = LevelUpRewardIconResolver.Resolve(g.itemId, g.icon, g.displayName),
                });
            }

            // [V4 ADD] Vàng/gem đã có ô tròn riêng → tắt 2 dòng chữ cũ (khỏi lặp thông tin)
            if (goldRewardRow != null) goldRewardRow.SetActive(false);
            if (gemRewardRow  != null) gemRewardRow.SetActive(false);

            // [R2 GỘP] MỘT KHU PHẦN THƯỞNG DUY NHẤT: ô quà vào chung khung trắng dưới
            // (Dai_MoKhoa) với ô mở khoá NEW. Dải card giữa popup (Hang_Qua) tắt hẳn.
            // Không tìm được khu gộp (scene chưa dựng bằng tool Township) → giữ NGUYÊN
            // đường cũ bên dưới, popup không bao giờ trắng tay.
            var mergedRT = ResolveMergedContainer();
            if (mergedRT != null)
            {
                if (giftItemsContainer != null && giftItemsContainer != (Transform)mergedRT)
                    giftItemsContainer.gameObject.SetActive(false);   // dải card giữa — dẹp

                // Dòng "Phần thưởng: +" mồ côi (2 chip vàng/gem con đã tắt ở [V4]) —
                // chỉ ẩn khi đúng là hàng Hang_PhanThuong của tool Township, tránh
                // tắt nhầm cả Content nếu ai đó đổi hierarchy.
                if (goldRewardRow != null && goldRewardRow.transform.parent != null &&
                    goldRewardRow.transform.parent.name == "Hang_PhanThuong")
                    goldRewardRow.transform.parent.gameObject.SetActive(false);

                BuildMergedGiftCells(mergedRT, quaHienThi);

                // Khu gộp CÓ NỘI DUNG → khung trắng phải bật, kể cả khi level này
                // không mở khoá gì (ApplyUnlockSlots vừa tắt nó vì withIcon == 0).
                if (unlockStripRoot != null && quaHienThi.Count > 0 && !unlockStripRoot.activeSelf)
                    unlockStripRoot.SetActive(true);
            }
            else if (giftItemsContainer != null)
            {
                if (giftItemSlotPrefab != null)
                {
                    // Có prefab thật → dùng prefab
                    foreach (var gift in quaHienThi)   // [V4]
                    {
                        var go   = Instantiate(giftItemSlotPrefab, giftItemsContainer);
                        var slot = go.GetComponent<LevelUpGiftSlotUI>();
                        if (slot != null) slot.Setup(gift);
                        go.AddComponent<GiftSlotBounceTooltip>().Init(gift);      // [V3 ADD] chạm quà → nhún mẩy + tooltip
                    }

                    // [V2 ADD] 5–6 quà/level: co dải quà lại cho vừa khung,
                    // không để ô tràn ra ngoài ContentPanel.
                    FitGiftRowV2(quaHienThi.Count);   // [V4]
                }
                else
                {
                    // Chưa có prefab → DỰNG NỀN ô quà bằng code (placeholder, thay sprite sau)
                    BuildProceduralGiftSlots(quaHienThi);   // [V4]
                }
            }

            // [V5] Dải icon quà mở khóa đã hiển thị đầy đủ icon + nhãn MỚI → Text_MoKhoa
            // (unlockDescText) không cần nữa. [V6] hintText KHÔNG tắt ở đây nữa —
            // BoTriVungDuoi() bên dưới bật + đặt chữ + căn vị trí an toàn dưới khung trắng.
            if (unlockDescText != null) unlockDescText.gameObject.SetActive(false);
        }
        else
        {
            // Không có config → hiển thị minimal
            if (goldRewardRow != null) goldRewardRow.SetActive(false);
            if (gemRewardRow  != null) gemRewardRow.SetActive(false);
            if (unlockDescText != null) unlockDescText.gameObject.SetActive(false);

            Debug.Log($"[LevelUpPopupUI] Không tìm thấy LevelRewardConfig cho level {level}. " +
                      "Tạo asset và kéo vào levelRewardConfigs list.");
        }

        // [V6 2026-09-05] Bố trí vùng đáy (khung trắng · dòng hint · nút Tiếp tục · nền mờ).
        // Phải chạy CUỐI PopulateUI — sau khi khu gộp đã chuẩn bị container và bật/tắt
        // unlockStripRoot — để không bị bước nào phía trên ghi đè lại.
        BoTriVungDuoi(cfg, level);
    }

    // =========================================================================
    // [V6 2026-09-05] BỐ TRÍ VÙNG ĐÁY POPUP
    // =========================================================================
    //  VÌ SAO CẦN: toạ độ trong scene (do tool dựng qua nhiều đời) làm 3 thứ chồng
    //  nhau ở đáy Content (1920×1134, tâm = canvas y+63):
    //    • Dai_MoKhoa y=-272 cao 250 → -397..-147, Btn_TiepTuc y=-462 cao 118 → -521..-403
    //      ⇒ khung trắng đè lên đỉnh nút ~6px và chữ hint (neo đáy y=165 ≈ Content -402)
    //      nằm NGAY TRÊN nút → không thể hiện được (V5 phải tắt hẳn hintText).
    //    • 2 lớp mờ (V3_DimBackground 0.62 + Bg_NenToi 0.65) chồng → nền tối ~87%.
    //    • Nen_Dai trắng tinh (1,1,1,1) → dải trắng chói, không hợp bảng màu kem.
    //  BỐ CỤC MỚI (Content-local):
    //    Dai_MoKhoa  y=-215 (cao 250)  → -340..-90
    //    Text_Hint   y=-385 (1100×66)  → -418..-352   (khe 12px dưới khung)
    //    Btn_TiepTuc y=-500 (420×118)  → -559..-441   (khe 23px dưới hint)
    //    Ở 1080p đáy nút = canvas -559+63 = -496 > -540 ⇒ vẫn trong màn hình.
    //  Hàm idempotent, rẻ (vài phép gán), không ném exception khi thiếu tham chiếu:
    //  field null → tự tìm theo tên trong cây popupRoot; không thấy thì bỏ qua mục đó.
    // =========================================================================

    private const float V6_STRIP_Y   = -215f;
    private const float V6_HINT_Y    = -385f;
    private const float V6_BUTTON_Y  = -500f;
    private static readonly Vector2 V6_HINT_SIZE = new Vector2(1100f, 66f);

    /// <summary>Tìm con theo tên ở MỌI độ sâu (kể cả object đang tắt). Không có → null.</summary>
    private static Transform TimConDeQuy(Transform goc, string ten)
    {
        if (goc == null || string.IsNullOrEmpty(ten)) return null;
        for (int i = 0; i < goc.childCount; i++)
        {
            var c = goc.GetChild(i);
            if (c.name == ten) return c;
            var sau = TimConDeQuy(c, ten);
            if (sau != null) return sau;
        }
        return null;
    }

    /// <summary>
    /// [V6] Ép bố cục vùng đáy popup (xem khối chú thích ở trên). Gọi cuối
    /// <see cref="PopulateUI"/>; chạy lại mỗi lần mở popup nên tool Township dựng lại
    /// cây giữa phiên cũng không làm lệch.
    /// </summary>
    private void BoTriVungDuoi(LevelRewardConfig cfg, int level)
    {
        Transform goc = popupRoot != null ? popupRoot.transform : transform;

        // ── 1 · Khung trắng Dai_MoKhoa: chỉ đổi y, giữ x / anchor / size ─────────
        RectTransform stripRT = unlockStripRoot != null ? unlockStripRoot.transform as RectTransform : null;
        if (stripRT == null) stripRT = TimConDeQuy(goc, "Dai_MoKhoa") as RectTransform;
        if (stripRT != null && !Mathf.Approximately(stripRT.anchoredPosition.y, V6_STRIP_Y))
            stripRT.anchoredPosition = new Vector2(stripRT.anchoredPosition.x, V6_STRIP_Y);

        // ── 2 · Nen_Dai: kem ấm thay trắng chói ────────────────────────────────
        Transform nenDaiTf = stripRT != null ? stripRT.Find("Nen_Dai") : null;
        if (nenDaiTf == null) nenDaiTf = TimConDeQuy(goc, "Nen_Dai");
        var nenDaiImg = nenDaiTf != null ? nenDaiTf.GetComponent<Image>() : null;
        if (nenDaiImg != null)
        {
            Color kem = new Color32(255, 243, 214, 235);   // Color32 không có operator != → so ở dạng Color
            if (nenDaiImg.color != kem) nenDaiImg.color = kem;
        }

        // ── 3 · Nút Tiếp tục: hạ xuống -500 (giữ x) ──────────────────────────────
        RectTransform btnRT = claimButton != null ? claimButton.transform as RectTransform : null;
        if (btnRT == null) btnRT = TimConDeQuy(goc, "Btn_TiepTuc") as RectTransform;
        if (btnRT != null && !Mathf.Approximately(btnRT.anchoredPosition.y, V6_BUTTON_Y))
            btnRT.anchoredPosition = new Vector2(btnRT.anchoredPosition.x, V6_BUTTON_Y);

        // ── 4 · Text_MoKhoa (unlockDescText): giữ field để tương thích, luôn ẩn ──
        if (unlockDescText != null && unlockDescText.gameObject.activeSelf)
            unlockDescText.gameObject.SetActive(false);

        // ── 5 · Dòng hint: neo tâm Content, dưới khung trắng, trên nút ───────────
        TextMeshProUGUI hint = hintText;
        if (hint == null)
        {
            var hintTf = TimConDeQuy(goc, "Text_Hint");
            if (hintTf != null) hint = hintTf.GetComponent<TextMeshProUGUI>();
        }
        if (hint != null)
        {
            var hrt = hint.rectTransform;

            // Cha phải là Content (contentPanel). Lỡ ai kéo vào Dai_MoKhoa thì RectMask2D
            // của Viewport / SetActive(false) của khung sẽ nuốt mất chữ → đưa về Content.
            Transform chaMuon = contentPanel != null ? (Transform)contentPanel : null;
            if (chaMuon == null && btnRT != null) chaMuon = btnRT.parent;
            if (chaMuon != null && hrt.parent != chaMuon)
                hrt.SetParent(chaMuon, false);

            var anchor = new Vector2(0.5f, 0.5f);
            if (hrt.anchorMin != anchor) hrt.anchorMin = anchor;
            if (hrt.anchorMax != anchor) hrt.anchorMax = anchor;
            if (hrt.pivot     != anchor) hrt.pivot     = anchor;
            var pos = new Vector2(0f, V6_HINT_Y);
            if (hrt.anchoredPosition != pos)   hrt.anchoredPosition = pos;
            if (hrt.sizeDelta != V6_HINT_SIZE) hrt.sizeDelta = V6_HINT_SIZE;
            if (hrt.localScale != Vector3.one) hrt.localScale = Vector3.one;

            // Vẽ TRƯỚC nút (sibling nhỏ hơn) → không bao giờ đè lên nút dù có tràn.
            if (btnRT != null && hrt.parent == btnRT.parent)
            {
                int idxBtn = btnRT.GetSiblingIndex();
                if (hrt.GetSiblingIndex() > idxBtn) hrt.SetSiblingIndex(idxBtn);
            }

            hint.enableAutoSizing = true;
            hint.fontSizeMin      = 20f;
            hint.fontSizeMax      = 28f;
            hint.textWrappingMode = TextWrappingModes.Normal;
            hint.overflowMode     = TextOverflowModes.Ellipsis;
            hint.maxVisibleLines  = 2;
            hint.alignment        = TextAlignmentOptions.Center;
            hint.color            = new Color32(255, 245, 220, 255);   // #FFF5DC
            hint.raycastTarget    = false;

            string chu = cfg != null ? cfg.hintText : null;
            if (string.IsNullOrWhiteSpace(chu))
                chu = $"Lên cấp {level}! Quà mới đã vào kho của bạn.";
            if (hint.text != chu) hint.text = chu;

            if (!hint.gameObject.activeSelf) hint.gameObject.SetActive(true);
            if (!hint.enabled) hint.enabled = true;
        }

        // ── 6 · Nền mờ: chỉ 1 lớp Bg_NenToi 0.65; tắt lớp trùng V3_DimBackground ──
        var dimV3 = TimConDeQuy(goc, "V3_DimBackground");
        if (dimV3 != null && dimV3.gameObject.activeSelf)
            dimV3.gameObject.SetActive(false);

        var nenToiTf = TimConDeQuy(goc, "Bg_NenToi");
        var nenToiImg = nenToiTf != null ? nenToiTf.GetComponent<Image>() : null;
        if (nenToiImg != null)
        {
            var mau = new Color(0f, 0f, 0f, 0.65f);
            if (nenToiImg.color != mau) nenToiImg.color = mau;
            if (!nenToiImg.enabled) nenToiImg.enabled = true;
            if (!nenToiTf.gameObject.activeSelf) nenToiTf.gameObject.SetActive(true);
        }
    }

    /// <summary>Dựng ô quà bằng code khi chưa gán giftItemSlotPrefab.
    /// Ẩn các ô placeholder tĩnh (QUÀ 1-5) trong prefab, rồi tạo 1 ô/quà, dàn ngang giữa.</summary>
    private void BuildProceduralGiftSlots(List<LevelRewardConfig.ItemGift> gifts)
    {
        // Ẩn ô placeholder tĩnh có sẵn trong prefab (không có LevelUpGiftSlotUI)
        foreach (Transform child in giftItemsContainer)
            if (child.gameObject.activeSelf && child.GetComponent<LevelUpGiftSlotUI>() == null)
                child.gameObject.SetActive(false);

        int n = gifts.Count;

        // [V2 ADD] KHOẢNG CÁCH CO GIÃN: spacing cứng 118px chỉ vừa tới 4 quà
        // (khung ~460px). Yêu cầu mới là hiển thị được 5–6 quà/level → khi đông,
        // tự chia đều bề rộng khung và THU NHỎ ô theo cùng tỉ lệ, không tràn khung.
        float areaW = 460f;
        var contRt = giftItemsContainer as RectTransform;
        if (contRt != null && contRt.rect.width > 1f) areaW = contRt.rect.width;

        float spacing = 118f;                                        // giữ nguyên khi ≤ 4 quà
        if (n > 1) spacing = Mathf.Min(118f, areaW / n);             // [V2 ADD]
        float slotScale = Mathf.Clamp(spacing / 118f, 0.6f, 1f);     // [V2 ADD]

        float startX = -(n - 1) * 0.5f * spacing;

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject($"GiftSlot_{i}", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(giftItemsContainer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            rt.localScale = new Vector3(slotScale, slotScale, 1f);   // [V2 ADD]

            // LayoutElement để nếu container có LayoutGroup thì vẫn dàn đẹp
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = 108f * slotScale;                   // [V2 ADD] khớp scale mới
            le.preferredHeight = 120f * slotScale;                   // [V2 ADD]

            var slot = go.AddComponent<LevelUpGiftSlotUI>();
            slot.BuildProcedural(gifts[i]);
            go.AddComponent<GiftSlotBounceTooltip>().Init(gifts[i]);          // [V3 ADD] chạm quà → nhún mẩy + tooltip
        }
    }

    // =========================================================================
    // [R2 GỘP] KHU PHẦN THƯỞNG DUY NHẤT — ô quà nằm CHUNG khung trắng với ô mở khoá
    // =========================================================================
    //  Lệnh Sếp 02/09: "gộp 2 vùng quà làm 1 — làm riêng ở trên kia là sai".
    //  Cách làm: KHÔNG sửa tay scene. Lúc chạy, mượn luôn Content của khung trắng
    //  (Dai_MoKhoa/ScrollView/Viewport/Content — đã nối dây qua unlockSlotsContainer),
    //  tắt HorizontalLayoutGroup + ContentSizeFitter + ScrollRect của nó, rồi tự xếp
    //  TẤT CẢ cell (ô mở khoá NEW + ô quà) thành flow-layout: tự xuống hàng, căn giữa,
    //  đông quá thì co đều cả cụm cho vừa khung. Dải card cũ giữa popup (Hang_Qua)
    //  bị tắt hẳn trong PopulateUI.
    // =========================================================================

    // Thông số khu gộp (px canvas, TRƯỚC khi co giãn).
    private const float MERGED_PAD_X     = 24f;   // lề trái/phải trong khung trắng
    private const float MERGED_PAD_Y     = 10f;   // lề trên/dưới
    private const float MERGED_SPACING_X = 16f;   // khoảng cách ngang giữa 2 cell
    private const float MERGED_ROW_GAP   = 10f;   // khoảng cách dọc giữa 2 hàng

    // ═════════════════════════════════════════════════════════════════════════
    // [V7 — 2026-09-06] CHỪA CHỖ CHO BẢNG CHỮ DƯỚI Ô
    // ─────────────────────────────────────────────────────────────────────────
    //  Flow-layout trước nay tính chiều cao một hàng CHỈ bằng chiều cao ô (190), KHÔNG kể
    //  bảng chữ treo bên dưới. Hậu quả đo được:
    //
    //   • Dải trắng Dai_MoKhoa cao STRIP_H = 250 và Viewport có RectMask2D. Ô 190 nằm giữa
    //     → mép dưới ô ở y = −95, đáy mask ở y = −125, tức CHỈ CÒN 30px cho chữ. Bảng chữ
    //     cũ cao 26px vừa khít 30px đó (nên chữ hiện được), nhưng chỉ cần cao hơn một chút
    //     là bị mask XÉN NGANG.
    //   • Khi dải xuống 2 hàng, chữ hàng trên đâm thẳng vào ô hàng dưới (khe chỉ 10px).
    //
    //  Cách sửa: cộng chiều cao bảng chữ vào chiều cao HÀNG (chỉ để tính chỗ, không đụng
    //  sizeDelta của ô), rồi đẩy ô lên nửa bảng chữ. Thế là cụm được canh giữa theo ĐÚNG
    //  chiều cao thật (ô + chữ): chữ không bao giờ lọt ra ngoài mask, hai hàng không đụng
    //  nhau, và nếu chật thì k tự co — thay vì âm thầm xén mất chữ.
    //
    //  Phải KHỚP với UnlockSlotUI: CAPTION_GAP_Y(4) + CAPTION_H(52) = 56.
    // ═════════════════════════════════════════════════════════════════════════
    private const float MERGED_CAPTION_BAND = 56f;
    private const float MERGED_MIN_SCALE = 0.5f;  // co tối đa còn 50%; lỡ vẫn tràn thì RectMask2D của Viewport cắt gọn

    private bool _warnedNoMergeContainer;

    /// <summary>
    /// Tìm (và chuẩn bị) khu phần thưởng gộp. Ưu tiên <see cref="unlockSlotsContainer"/>
    /// đã nối dây; thiếu thì suy từ cha của ô mở khoá đầu tiên. Trả null = scene chưa
    /// có khung trắng (chưa dựng bằng tool Township) → PopulateUI giữ bố cục cũ.
    /// </summary>
    private RectTransform ResolveMergedContainer()
    {
        if (_mergedContainer != null) return _mergedContainer;

        if (unlockSlotsContainer is RectTransform rtWired)
            _mergedContainer = rtWired;
        else
        {
            var slots = ResolveUnlockSlots();
            if (slots.Length > 0 && slots[0] != null)
                _mergedContainer = slots[0].transform.parent as RectTransform;
        }

        if (_mergedContainer == null)
        {
            // Tự động tìm dải trắng Dai_MoKhoa trong Hierarchy popup
            Transform dai = transform.Find("PopupRoot/Dai_MoKhoa")
                         ?? transform.Find("PopupRoot/Panel_Dim/Dai_MoKhoa")
                         ?? transform.Find("Dai_MoKhoa");
            if (dai != null)
            {
                Transform content = dai.Find("ScrollView/Viewport/Content") ?? dai;
                _mergedContainer = content as RectTransform;
            }
        }

        if (_mergedContainer == null)
        {
            if (!_warnedNoMergeContainer)
            {
                _warnedNoMergeContainer = true;
                Debug.LogWarning("[LevelUp] Không tìm thấy khung trắng Dai_MoKhoa");
            }
            return null;
        }

        PrepareMergedContainer(_mergedContainer);
        return _mergedContainer;
    }

    /// <summary>
    /// Tắt bộ layout CŨ của khung trắng (1 lần / phiên chạy): HorizontalLayoutGroup dàn
    /// ngang + ContentSizeFitter nở vô hạn + ScrollRect cuộn ngang đều nhường chỗ cho
    /// flow-layout tự xếp ở <see cref="LayoutMergedRewardArea"/>. Content được kéo phủ
    /// kín Viewport để rect của nó = đúng lòng khung trắng.
    /// CHỈ đổi component lúc RUNTIME — không đụng file scene (luật: cấm sửa tay .unity).
    /// </summary>
    private void PrepareMergedContainer(RectTransform rt)
    {
        if (_preparedContainer == rt) return;   // container này đã chuẩn bị rồi
        _preparedContainer = rt;

        var hlg = rt.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;

        var csf = rt.GetComponent<ContentSizeFitter>();
        if (csf != null) csf.enabled = false;

        // Hết cuộn ngang: mọi thứ giờ tự xuống hàng trong khung, không còn gì để kéo.
        var sr = rt.GetComponentInParent<ScrollRect>(true);
        if (sr != null) sr.enabled = false;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Debug.Log("[LevelUp] Đã gộp 2 vùng quà làm 1: ô quà + ô mở khoá NEW cùng nằm trong " +
                  $"khung trắng '{(unlockStripRoot != null ? unlockStripRoot.name : rt.name)}' (flow-layout, tự xuống hàng, căn giữa).");
    }

    /// <summary>Dọn ô quà của popup trước ra khỏi khu gộp (ô mở khoá GIỮ NGUYÊN — ApplyUnlockSlots lo).</summary>
    private void ClearMergedGiftCells()
    {
        _mergedCells.Clear();
        if (_mergedContainer == null) return;
        foreach (Transform child in _mergedContainer)
        {
            var runtimeCell = child.GetComponent<UnlockSlotUI>();
            // [B4] ô runtime (vàng/gem/quà) → dọn; 9 ô mở khoá của scene (IsRuntimeCell=false) GIỮ.
            // Vẫn dọn LevelUpGiftSlotUI để sạch ô sót từ bản code cũ (nếu scene còn).
            if ((runtimeCell != null && runtimeCell.IsRuntimeCell) || child.GetComponent<LevelUpGiftSlotUI>() != null)
                Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// [B4 GỘP Q1 — 2026-09-05] Dựng ô vàng / kim cương / quà TRỰC TIẾP trong khu gộp bằng
    /// <see cref="UnlockSlotUI"/> — CÙNG một đường vẽ với ô mở khoá (pop + bob + tag + tên).
    /// Tag: quà vật phẩm "×N" cam; vàng/gem "+N" cam (đọc tự nhiên: +350 Vàng). Ô mở khoá giữ
    /// tag "MỚI" đỏ ở ApplyUnlockSlots. Mẫu vòng viền/tag sao chép từ ô mở khoá đầu tiên của scene.
    /// </summary>
    private void BuildMergedGiftCells(RectTransform parent, List<LevelRewardConfig.ItemGift> gifts)
    {
        UnlockSlotUI mau = null;
        var slotsMau = ResolveUnlockSlots();
        for (int i = 0; i < slotsMau.Length && mau == null; i++) mau = slotsMau[i];

        for (int i = 0; i < gifts.Count; i++)
        {
            var gift = gifts[i];
            if (gift == null) continue;

            var slot = UnlockSlotUI.CreateRuntimeCell(parent, $"GiftCell_{i:00}_{gift.itemId}", mau);

            bool laTienTe = gift.itemId == LevelUpRewardIconResolver.GoldId || gift.itemId == LevelUpRewardIconResolver.GemId;
            string tag = laTienTe ? $"+{gift.amount}" : $"×{Mathf.Max(1, gift.amount)}";
            string ten = string.IsNullOrWhiteSpace(gift.displayName) ? gift.itemId : gift.displayName;
            slot.Setup(gift.icon, tag, UnlockSlotUI.TagColorGift, ten);

            // [R2 ICON] resolver đã chịu thua (và đã log [LevelUp] một lần) → đĩa màu
            // theo id thay vì ô trống chỉ có chữ.
            if (gift.icon == null)
                slot.ShowPlaceholderTint(OrderBoardIconResolver.TintFromId(gift.itemId));

            slot.gameObject.AddComponent<GiftSlotBounceTooltip>().Init(gift);   // chạm quà → nhún mẩy + tooltip
            _mergedCells.Add(slot);
        }
    }

    /// <summary>
    /// Đợi đúng 1 frame rồi xếp khu gộp: Destroy() ô cũ hoàn tất cuối frame và rect
    /// khung trắng cần canvas tính xong. AnimateIn (fade từ alpha 0) che kín frame chờ.
    /// </summary>
    private IEnumerator CoLayoutMergedRewards()
    {
        yield return null;
        LayoutMergedRewardArea();
        _mergedLayoutRoutine = null;
    }

    /// <summary>
    /// FLOW-LAYOUT khu phần thưởng gộp: ô mở khoá NEW đứng đầu, rồi vàng / kim cương /
    /// vật phẩm; xếp trái→phải, đầy bề ngang thì tự xuống hàng; mỗi hàng căn giữa ngang,
    /// cả block căn giữa dọc; tổng cao quá khung thì co đều tất cả (bước 5%, sàn 50%).
    /// Ô mở khoá co qua UnlockSlotUI.SetBaseScale để KHÔNG đánh nhau với animation pop.
    /// </summary>
    private void LayoutMergedRewardArea()
    {
        var parent = _mergedContainer;
        if (parent == null) return;

        // ── 1 · Gom cell theo thứ tự hiển thị ───────────────────────────────
        var rts      = new List<RectTransform>();
        var sizes    = new List<Vector2>();
        var unlockOf = new List<UnlockSlotUI>();   // != null nếu cell là ô mở khoá

        var slots = ResolveUnlockSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null || !s.gameObject.activeSelf) continue;
            if (s.transform.parent != parent) continue;   // ô nằm chỗ khác thì không xếp hộ

            var rt = (RectTransform)s.transform;
            Vector2 sz = rt.sizeDelta;
            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // phòng hờ, chuẩn tool = 190
            sz.y += MERGED_CAPTION_BAND;   // [V7] chừa chỗ bảng chữ, xem chú thích ở hằng số
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(s);
        }
        for (int i = 0; i < _mergedCells.Count; i++)
        {
            var c = _mergedCells[i];
            if (c == null) continue;                       // đã bị Destroy
            var rt = (RectTransform)c.transform;
            Vector2 sz = rt.sizeDelta;
            if (sz.x < 1f || sz.y < 1f) sz = new Vector2(190f, 190f);   // chuẩn đồng bộ 190x190
            sz.y += MERGED_CAPTION_BAND;   // [V7] chừa chỗ bảng chữ, xem chú thích ở hằng số
            rts.Add(rt); sizes.Add(sz); unlockOf.Add(c);   // [B4] ô runtime cũng là UnlockSlotUI → co qua SetBaseScale
        }
        if (rts.Count == 0) return;

        float availW = parent.rect.width  - MERGED_PAD_X * 2f;
        float availH = parent.rect.height - MERGED_PAD_Y * 2f;
        if (availW < 100f) availW = 980f;   // rect chưa kịp tính (hiếm) → dùng bề rộng banner
        if (availH < 80f)  availH = 230f;

        // ── 2 · Tìm scale k vừa khung: thử 1.0 rồi giảm dần 5% ───────────────
        float k = 1f;
        var rows = new List<List<int>>();
        var rowW = new List<float>();
        var rowH = new List<float>();
        while (true)
        {
            rows.Clear(); rowW.Clear(); rowH.Clear();
            var cur = new List<int>();
            float curW = 0f, curH = 0f;
            for (int i = 0; i < rts.Count; i++)
            {
                float w    = sizes[i].x * k;
                float cand = cur.Count == 0 ? w : curW + MERGED_SPACING_X * k + w;
                if (cur.Count > 0 && cand > availW)
                {
                    rows.Add(cur); rowW.Add(curW); rowH.Add(curH);
                    cur = new List<int> { i }; curW = w; curH = sizes[i].y * k;
                }
                else
                {
                    cur.Add(i); curW = cand; curH = Mathf.Max(curH, sizes[i].y * k);
                }
            }
            rows.Add(cur); rowW.Add(curW); rowH.Add(curH);

            float totalH = (rows.Count - 1) * MERGED_ROW_GAP * k;
            for (int r = 0; r < rowH.Count; r++) totalH += rowH[r];

            if (totalH <= availH || k <= MERGED_MIN_SCALE + 0.001f) break;
            k = Mathf.Max(MERGED_MIN_SCALE, k - 0.05f);
        }

        // ── 3 · Đặt chỗ: block giữa khung, hàng giữa ngang, cell giữa dọc hàng ─
        float blockH = (rows.Count - 1) * MERGED_ROW_GAP * k;
        for (int r = 0; r < rowH.Count; r++) blockH += rowH[r];
        float y = blockH * 0.5f;                        // mép TRÊN block (gốc toạ độ = tâm khung)

        for (int r = 0; r < rows.Count; r++)
        {
            float x       = -rowW[r] * 0.5f;            // mép trái hàng
            float centerY = y - rowH[r] * 0.5f;
            var row = rows[r];
            for (int j = 0; j < row.Count; j++)
            {
                int i  = row[j];
                var rt = rts[i];
                float w = sizes[i].x * k;

                // HLG cũ từng ghim anchor (0,1) — trả về anchor tâm để toạ độ dễ hiểu.
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                // [V7] Hàng nay cao (ô + bảng chữ); bảng chữ nằm DƯỚI ô nên ô phải lệch
                // LÊN nửa bảng chữ, chừa đúng phần dưới cho chữ.
                rt.anchoredPosition = new Vector2(x + w * 0.5f, centerY + MERGED_CAPTION_BAND * 0.5f * k);

                if (unlockOf[i] != null) unlockOf[i].SetBaseScale(k);
                else rt.localScale = new Vector3(k, k, 1f);

                x += w + MERGED_SPACING_X * k;
            }
            y -= rowH[r] + MERGED_ROW_GAP * k;
        }
    }

    // =========================================================================
    // Dải ô "VỪA MỞ KHOÁ"
    // =========================================================================

    /// <summary>
    /// Nạp icon vào các ô mở khoá và ẨN mọi ô thừa.
    ///
    /// Prefab dựng CỨNG 9 ô, còn mỗi level chỉ mở 1–3 thứ → phần lớn ô sẽ bị ẩn,
    /// đó là ĐÚNG. Không ẩn thì người chơi thấy 6–8 khung tròn trắng trơn.
    ///
    /// [V2 ADD] SIẾT THÊM: mục unlock KHÔNG có icon cũng bị ẨN LUÔN (trước đây vẫn
    /// hiện khung tròn trắng + nhãn NEW — chính là cái ảnh chụp bị sếp chê).
    /// Nhãn chữ của mục đó vẫn hiện đầy đủ ở dòng "Mở khóa: ..." nên không mất thông tin.
    /// </summary>
    private void ApplyUnlockSlots(LevelRewardConfig cfg)
    {
        var slots = ResolveUnlockSlots();
        if (slots.Length == 0)
        {
            _pendingUnlockPopCount = 0;
            return;
        }

        // GetUnlockEntries() theo hợp đồng của DEV-A: LUÔN != null (có thể rỗng)
        // → không cần "?? new List<>()". cfg == null thì mới phải tự lo.
        // UnlockEntry là class LỒNG trong LevelRewardConfig → phải viết đủ tên.
        List<LevelRewardConfig.UnlockEntry> entries =
            cfg != null ? cfg.GetUnlockEntries() : null;

        int wanted = entries != null ? entries.Count : 0;   // số mục level này mở
        int used   = Mathf.Min(wanted, slots.Length);       // số ô thật sự hiện được

        int withIcon = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;      // phần tử mồ côi trong mảng gán tay

            bool inUse = i < used;

            // [V2 ADD] LUẬT MỚI: ô KHÔNG CÓ ICON thì coi như KHÔNG DÙNG → tắt hẳn.
            // Dẹp nạn "vòng tròn trắng rỗng đeo tag NEW". Đọc entry TRƯỚC khi quyết
            // định bật/tắt để một lần SetActive là xong, không bật lên rồi tắt lại.
            LevelRewardConfig.UnlockEntry entry = inUse ? entries[i] : null;
            Sprite icon = entry != null ? entry.icon : null;
            if (icon == null && entry != null)
            {
                string idOrLabel = !string.IsNullOrEmpty(entry.label) ? entry.label : "";
                icon = LevelUpRewardIconResolver.Resolve(idOrLabel, null, idOrLabel);
            }
            if (inUse && icon == null) inUse = false;        // [V2 ADD]

            // BẮT BUỘC: ô thừa phải TẮT hẳn. HorizontalLayoutGroup bỏ qua con đang tắt
            // nên dải icon tự co lại vừa số ô — không để khoảng trống.
            if (slot.gameObject.activeSelf != inUse)
                slot.gameObject.SetActive(inUse);

            if (!inUse) continue;

            withIcon++;

            // [R2 GỘP Q1 — 2026-09-05] Sếp chốt "mọi ô đều có tên": hiện thẳng trên ô
            slot.Setup(icon, true, entry.label ?? "");
        }

        // Hiệu ứng "bung ra" phải hoãn — xem PlayUnlockPops().
        _pendingUnlockPopCount = used;

        // Level không mở gì / không mục nào CÓ ICON → ẩn cả dải, tránh thanh nền tối rỗng.
        // [V2 ADD] đổi điều kiện used → withIcon: nếu mọi mục đều thiếu icon thì các ô
        // đã bị ẩn hết ở trên, giữ nền dải lại sẽ thành thanh tối rỗng.
        if (unlockStripRoot != null && unlockStripRoot.activeSelf != (withIcon > 0))
            unlockStripRoot.SetActive(withIcon > 0);

        if (wanted > slots.Length)
            Debug.LogWarning($"[LevelUpPopupUI] Level mở {wanted} thứ nhưng popup chỉ có {slots.Length} ô → " +
                             $"{wanted - slots.Length} mục KHÔNG hiện. Tăng 'Số ô mở khoá' rồi dựng lại " +
                             "bằng Tools ▸ Farm ▸ Popup Lên Cấp (Township).");

        if (used - withIcon > 0)
            Debug.LogWarning($"[LevelUpPopupUI] {used - withIcon}/{used} mục mở khoá có icon = NULL → " +
                             "đã ẨN các ô đó (luật V2: không hiện vòng tròn trắng rỗng). " +   // [V2 ADD]
                             "Chạy Tools ▸ Farm ▸ Điền Icon Unlock (Level Reward) để điền unlockEntries.");

        Debug.Log($"[LevelUpPopupUI] Ô mở khoá: hiện {withIcon}/{slots.Length} (mục có icon), " +
                  $"{used - withIcon} mục thiếu icon đã ẩn.");
    }

    /// <summary>
    /// Chạy hiệu ứng "bung ra" cho các ô đang bật.
    ///
    /// VÌ SAO TÁCH RIÊNG KHỎI ApplyUnlockSlots: PopulateUI() được gọi TRƯỚC
    /// popupRoot.SetActive(true), nên lúc đó ô mở khoá chưa activeInHierarchy.
    /// UnlockSlotUI.PlayPop() có chốt `if (!isActiveAndEnabled) return;`
    /// (StartCoroutine trên object đang tắt sẽ bị Unity từ chối) → gọi sớm là mất animation.
    /// </summary>
    private void PlayUnlockPops()
    {
        int n = 0;
        if (_pendingUnlockPopCount > 0)
        {
            var slots = ResolveUnlockSlots();
            n = Mathf.Min(_pendingUnlockPopCount, slots.Length);
            for (int i = 0; i < n; i++)
                if (slots[i] != null && slots[i].gameObject.activeInHierarchy)
                    slots[i].PlayPop(i);
        }
        _pendingUnlockPopCount = 0;

        // [B4 GỘP Q1] Ô vàng/gem/quà dựng runtime cũng "bung ra" nối tiếp sau ô mở khoá
        // (index tiếp nối → stagger liền mạch trái→phải).
        for (int i = 0; i < _mergedCells.Count; i++)
        {
            var c = _mergedCells[i];
            if (c != null && c.gameObject.activeInHierarchy) c.PlayPop(n + i);
        }
    }

    /// <summary>[B4] Lọc bỏ ô runtime (vàng/gem/quà) khỏi kết quả dò UnlockSlotUI — chỉ giữ ô mở khoá của scene.</summary>
    private static UnlockSlotUI[] LocBoOCellRuntime(UnlockSlotUI[] arr)
    {
        if (arr == null || arr.Length == 0) return arr;
        var kq = new List<UnlockSlotUI>(arr.Length);
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] != null && !arr[i].IsRuntimeCell) kq.Add(arr[i]);
        return kq.Count == arr.Length ? arr : kq.ToArray();
    }

    /// <summary>
    /// Trả về danh sách ô mở khoá theo thứ tự hiển thị. LUÔN != null.
    /// Ưu tiên tự dò trong <see cref="unlockSlotsContainer"/>; nếu container để trống
    /// thì mới dùng mảng <see cref="unlockSlots"/> gán tay.
    /// </summary>
    private UnlockSlotUI[] ResolveUnlockSlots()
    {
        // Cache còn dùng được? Phần tử null = ô đã bị Destroy (tool dựng lại popup) → dò lại.
        if (_unlockSlotsCache != null)
        {
            bool valid = true;
            for (int i = 0; i < _unlockSlotsCache.Length; i++)
                if (_unlockSlotsCache[i] == null) { valid = false; break; }
            if (valid) return _unlockSlotsCache;
            _unlockSlotsCache = null;
        }

        // 1) Tự dò trong container — luôn khớp Hierarchy thật.
        //    includeInactive = TRUE vì từ lần mở popup thứ 2, các ô thừa đang bị TẮT,
        //    nếu bỏ qua ô tắt thì mảng ngắn dần và không bao giờ bật lại được.
        if (unlockSlotsContainer != null)
        {
            _unlockSlotsCache = LocBoOCellRuntime(unlockSlotsContainer.GetComponentsInChildren<UnlockSlotUI>(true));   // [B4]
            if (_unlockSlotsCache.Length > 0) return _unlockSlotsCache;
        }

        // 2) Dự phòng: mảng gán tay (bỏ phần tử null).
        if (unlockSlots != null && unlockSlots.Length > 0)
        {
            var picked = new List<UnlockSlotUI>(unlockSlots.Length);
            foreach (var s in unlockSlots)
                if (s != null) picked.Add(s);

            if (picked.Count > 0)
            {
                _unlockSlotsCache = picked.ToArray();
                return _unlockSlotsCache;
            }
        }

        // 3) CỨU CÁNH CUỐI: quét cả cây popup.
        //    BẮT BUỘC PHẢI CÓ vì scene hiện tại được dựng bằng bản tool CŨ (chưa biết
        //    2 field ở trên) → nếu thiếu bước này thì phải mở Unity bấm "DỰNG POPUP"
        //    lại mới thấy icon, mà đúng lúc đó không ai bấm được.
        Transform searchRoot = popupRoot != null ? popupRoot.transform : transform;
        _unlockSlotsCache = LocBoOCellRuntime(searchRoot.GetComponentsInChildren<UnlockSlotUI>(true));   // [B4]
        if (_unlockSlotsCache.Length > 0)
        {
            if (!_warnedNoUnlockSlots)
            {
                _warnedNoUnlockSlots = true;
                Debug.Log($"[LevelUpPopupUI] Chưa nối dây ô mở khoá, đã TỰ TÌM được " +
                          $"{_unlockSlotsCache.Length} ô dưới '{searchRoot.name}'. " +
                          "Chạy lại Tools ▸ Farm ▸ Popup Lên Cấp (Township) để nối dây cho chắc.");
            }
            return _unlockSlotsCache;
        }

        _unlockSlotsCache = System.Array.Empty<UnlockSlotUI>();
        if (!_warnedNoUnlockSlots)
        {
            _warnedNoUnlockSlots = true;   // cảnh báo 1 lần, tránh spam Console mỗi lần lên cấp
            Debug.LogWarning("[LevelUpPopupUI] Không tìm thấy UnlockSlotUI nào (cả 'unlockSlotsContainer', " +
                             "'unlockSlots' và cây con của popupRoot đều trống) → không có ô mở khoá nào " +
                             "để nạp icon. Dựng lại popup bằng Tools ▸ Farm ▸ Popup Lên Cấp (Township).");
        }
        return _unlockSlotsCache;
    }

    // =========================================================================
    // Claim & Close
    // =========================================================================

    private void ClaimAndClose()
    {
        // [V2 ADD] Chống double-claim: tap màn hình + bấm nút (hoặc double-tap)
        // có thể gọi hàm này 2 lần trước khi AnimateOut kịp đóng → nhận quà 2 lần.
        if (_v2Closing) return;
        _v2Closing = true;

        StopV2Fx();   // [V2 ADD] tắt sparkle + nhân vật + tap-catcher NGAY khi bắt đầu đóng

        GrantRewards(_currentConfig);
        GhiNhoDaXem(_currentShownLevel);   // [B6] đã nhận → lần mở game sau không hiện lại cấp này
        StopVFX(); // bấm Nhận Quà → tắt pháo hoa NGAY rồi mới đóng popup (dừng cả vòng lặp [B5])
        StartCoroutine(AnimateOut(() =>
        {
            StopVFX();
            if (popupRoot != null) popupRoot.SetActive(false);
            ReleaseInputLock();
            ShowNextPopup();
        }));
    }

    /// <summary>[V4 ADD] Icon vàng cho ô quà: RewardIconLibrary → HUD Icon_Gold → null.</summary>
    private static Sprite TimIconVangV4()
    {
        var lib = RewardIconLibrary.Instance;
        if (lib != null && lib.goldSprite != null) return lib.goldSprite;
        var go = GameObject.Find("Icon_Gold");
        var img = go != null ? go.GetComponent<UnityEngine.UI.Image>() : null;
        return img != null ? img.sprite : null;
    }

    /// <summary>[V4 ADD] Icon kim cương cho ô quà: RewardIconLibrary → HUD Icon_Gem/Icon_Diamond → null.</summary>
    private static Sprite TimIconGemV4()
    {
        var lib = RewardIconLibrary.Instance;
        if (lib != null && lib.gemSprite != null) return lib.gemSprite;
        var go = GameObject.Find("Icon_Gem") ?? GameObject.Find("Icon_Diamond") ?? GameObject.Find("Diamond_Container");
        var img = go != null ? go.GetComponent<UnityEngine.UI.Image>() : null;
        if (img == null && go != null) img = go.GetComponentInChildren<UnityEngine.UI.Image>();
        return img != null ? img.sprite : null;
    }

    private void GrantRewards(LevelRewardConfig cfg)
    {
        if (cfg == null) return;

        if (cfg.giftGold > 0 && FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.AddGold(cfg.giftGold);
            Debug.Log($"[LevelUpPopup] +{cfg.giftGold} vàng");
        }

        if (cfg.giftGems > 0 && FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.AddGems(cfg.giftGems);
            Debug.Log($"[LevelUpPopup] +{cfg.giftGems} kim cương");
        }

        if (WarehouseManager.Instance != null)
        {
            foreach (var gift in cfg.giftItems)
            {
                WarehouseManager.Instance.AddItem(
                    gift.itemId, gift.displayName, gift.icon, gift.amount);
                Debug.Log($"[LevelUpPopup] +{gift.amount}x {gift.displayName}");
            }
        }
    }

    // =========================================================================
    // [V2 ADD] Bộ juice V2 — nhân vật ăn mừng / sparkle / tap-to-close
    // =========================================================================

    /// <summary>
    /// [V2 ADD] Bật toàn bộ hiệu ứng V2. Gọi từ ShowNextPopup() SAU khi popupRoot
    /// đã activeInHierarchy (các component con mới StartCoroutine được).
    /// Mọi tham chiếu đều nullable — chưa chạy tool Nâng cấp V2 thì popup vẫn
    /// hoạt động y như cũ, không lỗi.
    /// </summary>
    private void StartV2Fx()
    {
        if (sparkleFx != null)
            sparkleFx.Play();

        if (celebrationSlots != null)
        {
            for (int i = 0; i < celebrationSlots.Length; i++)
            {
                var slot = celebrationSlots[i];
                if (slot == null)
                {
                    // [B1 TỰ HỒI PHỤC — 2026-09-05] Mảng gán tay bị đứt (null) → tự dò lại theo
                    // đường dẫn chuẩn Township trước khi bỏ qua hẳn. Đây là CỨU CÁNH tạm cho LẦN
                    // HIỆN NÀY (không ghi ngược vào celebrationSlots[] — mảng serialize không lưu
                    // được lúc runtime); chạy lại "★ Nối lại dây popup (APPLY)" để nối dây bền hơn.
                    string path = $"Root_HienThi/Content/V2_Celebration/V2_CharSlot_{i + 1:00}";
                    Transform found = transform.Find(path);
                    slot = found != null ? found.GetComponent<CelebrationCharacterSlot>() : null;
                    Debug.LogWarning($"[LevelUp] celebrationSlots null → tự tìm '{path}'" +
                                      (slot != null ? " → tìm thấy, dùng tạm cho lần hiện này." : " → KHÔNG tìm thấy."));
                }
                if (slot != null)
                    slot.Play();   // slot thiếu frames sẽ tự SetActive(false)
            }
        }

        if (tapCatcher != null)
        {
            tapCatcher.gameObject.SetActive(tapAnywhereToClose);
            if (tapAnywhereToClose)
                tapCatcher.Arm(OnTapAnywhereV2);  // reset delay 0.8s cho MỖI popup trong hàng đợi
            else
                tapCatcher.Disarm();
        }
    }

    /// <summary>[V2 ADD] Tắt toàn bộ hiệu ứng V2. Gọi khi bắt đầu đóng popup / OnDestroy.</summary>
    private void StopV2Fx()
    {
        if (sparkleFx != null)
            sparkleFx.Stop();

        if (celebrationSlots != null)
            for (int i = 0; i < celebrationSlots.Length; i++)
                if (celebrationSlots[i] != null)
                    celebrationSlots[i].StopAndReset();

        if (tapCatcher != null)
            tapCatcher.Disarm();
    }

    /// <summary>
    /// [V2 ADD] Callback khi người chơi chạm vào vùng trống của màn hình
    /// (sau delay tối thiểu của LevelUpTapToClose). Hành xử Y HỆT bấm nút Nhận Quà:
    /// nhận đủ vàng / ngọc / vật phẩm rồi đóng popup.
    /// </summary>
    private void OnTapAnywhereV2()
    {
        ClaimAndClose();
    }

    /// <summary>
    /// [V2 ADD] Co dải quà (nhánh dùng prefab + HorizontalLayoutGroup) khi có 5–6 quà:
    /// giảm spacing và scale từng ô theo cùng tỉ lệ để cả dải nằm gọn trong khung.
    /// Bật childScaleWidth/Height để LayoutGroup tính đúng bề rộng ô đã thu nhỏ.
    /// </summary>
    private void FitGiftRowV2(int slotCount)
    {
        if (giftItemsContainer == null || slotCount < 5) return;

        var contRt = giftItemsContainer as RectTransform;
        float areaW = contRt != null && contRt.rect.width > 1f ? contRt.rect.width : 460f;

        var layout = giftItemsContainer.GetComponent<HorizontalLayoutGroup>();
        float spacing = 8f;
        if (layout != null)
        {
            layout.spacing = Mathf.Min(layout.spacing, spacing);
            spacing = layout.spacing;
            layout.childScaleWidth  = true;   // LayoutGroup nhân scale vào bề rộng con
            layout.childScaleHeight = true;
        }

        const float slotW = 108f;   // bề rộng ô quà chuẩn của prefab / procedural
        float need = slotCount * slotW + (slotCount - 1) * spacing;
        float k = Mathf.Clamp(areaW / need, 0.55f, 1f);
        if (k >= 0.999f) return;    // vẫn vừa khung → không đụng gì

        foreach (Transform child in giftItemsContainer)
            if (child.gameObject.activeSelf && child.GetComponent<LevelUpGiftSlotUI>() != null)
                child.localScale = new Vector3(k, k, 1f);
    }

    // =========================================================================
    // Animations
    // =========================================================================

    private IEnumerator AnimateIn()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeInDuration;
                canvasGroup.alpha = Mathf.Clamp01(t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        if (contentPanel != null)
        {
            contentPanel.localScale = Vector3.one * 0.6f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / scaleInDuration;
                float s = EaseOutBack(Mathf.Clamp01(t));
                contentPanel.localScale = Vector3.one * s;
                yield return null;
            }
            contentPanel.localScale = Vector3.one;
        }
    }

    private IEnumerator AnimateOut(System.Action onDone)
    {
        if (canvasGroup != null)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.18f;
                canvasGroup.alpha = 1f - Mathf.Clamp01(t);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        onDone?.Invoke();
    }

    // Easing: overshoot spring feel khi bật popup
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // =========================================================================
    // VFX
    // =========================================================================

    private void SpawnVFX()
    {
        StopVFX();

        Transform parentTarget = popupRoot != null ? popupRoot.transform : transform;
        _activeVfxRoot = new GameObject("LevelUpPopup_VFX_Runtime", typeof(RectTransform));
        _activeVfxRoot.transform.SetParent(parentTarget, false);
        _activeVfxRoot.transform.SetAsLastSibling(); // Nổi lên trên cùng của popup

        RectTransform rootRt = (RectTransform)_activeVfxRoot.transform;
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        Camera renderCamera = Camera.main;

        if (useUIFireworks)
        {
            // Overlay canvas luôn vẽ sau cùng → thay pháo hoa ParticleSystem bằng UI thuần
            // (Image, con trực tiếp của popup). Xem giải thích đầy đủ ở comment trên
            // hàm SpawnUIFireworks() bên dưới.
            // [V3 2026-09-04] Truoc day parent = contentPanel (chinh cai card) => phao hoa no
            // GON TRONG CARD va an theo animation phong to 0.6->1.0 cua card => nhin nhu bi
            // card che phu. Nay ban o lop rieng phu TOAN khung popup, nam tren ca nen mo.
            RectTransform fireworksParent = fireworksOnTopLayer
                ? EnsureFireworksLayer()
                : (contentPanel != null
                    ? contentPanel
                    : (popupRoot != null ? popupRoot.transform as RectTransform : transform as RectTransform));
            SpawnUIFireworks(fireworksParent);
        }
        else if (vfxConfettiPrefab != null)
        {
            // Pháo hoa: bắn 3 điểm phía trên (giữa + 2 góc) cho rầm rộ
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_Top",
                GetVfxScreenPoint(VfxPlacement.Top),      renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopLeft",
                GetVfxScreenPoint(VfxPlacement.TopLeft),  renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopRight",
                GetVfxScreenPoint(VfxPlacement.TopRight), renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
        }

        // [V6 2026-09-05] useUIFireworks = true ⇒ KHÔNG spawn Lana03 hai bên nữa:
        // ParticleSystem do Camera vẽ không bao giờ nổi lên Overlay canvas (xem khối
        // chú thích "UI Fireworks" bên dưới) → trước đây spawn + replay mỗi 0.6s chỉ tốn CPU
        // mà mắt không thấy gì. Tắt useUIFireworks (đường revert) thì vẫn chạy như cũ.
        if (!useUIFireworks && vfxSidePrefab != null)
        {
            // Lana03 hai bên — to hơn confetti
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Left",
                GetVfxScreenPoint(VfxPlacement.Left),  renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Right",
                GetVfxScreenPoint(VfxPlacement.Right), renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
        }

        // KHÔNG tự huỷ sau vài giây nữa — lặp "bùm bùm bùm" tới khi user bấm Nhận Quà (StopVFX dừng).
        // [V6] Chỉ cần vòng lặp replay khi thật sự có ParticleSystem trong _activeVfxRoot
        // (đường useUIFireworks = false). Pháo hoa UI có vòng lặp riêng (VongLapPhaoHoa).
        if (_vfxLoop != null) StopCoroutine(_vfxLoop);
        if (!useUIFireworks)
            _vfxLoop = StartCoroutine(VfxBurstLoop());
        else
            _vfxLoop = null;
    }

    private enum VfxPlacement
    {
        Top,
        TopLeft,
        TopRight,
        Left,
        Right
    }

    private Vector2 GetVfxScreenPoint(VfxPlacement placement)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;

        if (contentPanel != null)
        {
            Vector3[] corners = new Vector3[4];
            contentPanel.GetWorldCorners(corners);

            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(null, corners[3]);

            if (placement == VfxPlacement.Top)
                return Vector2.Lerp(topLeft, topRight, 0.5f)
                    + Vector2.up * (vfxTopPanelGap * scaleFactor);

            if (placement == VfxPlacement.TopLeft)
                return topLeft
                    + Vector2.up   * (vfxTopPanelGap * scaleFactor)
                    + Vector2.left * (vfxSidePanelGap * 0.5f * scaleFactor);

            if (placement == VfxPlacement.TopRight)
                return topRight
                    + Vector2.up    * (vfxTopPanelGap * scaleFactor)
                    + Vector2.right * (vfxSidePanelGap * 0.5f * scaleFactor);

            Vector2 sideCenter = placement == VfxPlacement.Left
                ? Vector2.Lerp(bottomLeft, topLeft, 0.5f)
                : Vector2.Lerp(bottomRight, topRight, 0.5f);
            Vector2 horizontalOffset =
                (placement == VfxPlacement.Left ? Vector2.left : Vector2.right)
                * (vfxSidePanelGap * scaleFactor);

            return sideCenter
                + horizontalOffset
                + Vector2.up * (vfxSideVerticalOffset * scaleFactor);
        }

        Transform fallback =
            (placement == VfxPlacement.Top || placement == VfxPlacement.TopLeft || placement == VfxPlacement.TopRight)
            ? vfxSpawnPoint
            : placement == VfxPlacement.Left ? vfxLeftPoint : vfxRightPoint;
        return fallback != null
            ? RectTransformUtility.WorldToScreenPoint(null, fallback.position)
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void SpawnWorldVfx(
        GameObject prefab,
        string instanceName,
        Vector2 screenPoint,
        Camera renderCamera,
        float demoOrthoSize,
        float demoScale)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        RectTransform canvasRt = rootCanvas != null ? rootCanvas.transform as RectTransform : (RectTransform)_activeVfxRoot.transform;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPoint, rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? renderCamera : null, out localPoint);

        GameObject instance = Instantiate(
            prefab,
            _activeVfxRoot.transform);
        instance.name = instanceName;
        instance.transform.localPosition = new Vector3(localPoint.x, localPoint.y, -10f); // Nhô ra phía trước UI

        float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        float finalScale = demoScale * scaleFactor * 100f;
        instance.transform.localScale = Vector3.one * finalScale;

        foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.useUnscaledTime = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Nhiều particle hơn: nhân rate + số lượng burst
            var emission = particleSystem.emission;
            emission.rateOverTimeMultiplier *= vfxEmissionMultiplier;
            int burstCount = emission.burstCount;
            if (burstCount > 0)
            {
                var bursts = new ParticleSystem.Burst[burstCount];
                emission.GetBursts(bursts);
                for (int b = 0; b < bursts.Length; b++)
                {
                    var c = bursts[b].count;
                    c.constantMin *= vfxEmissionMultiplier;
                    c.constantMax *= vfxEmissionMultiplier;
                    bursts[b].count = c;
                }
                emission.SetBursts(bursts);
            }

            particleSystem.Clear(true);
            particleSystem.Play(true);
        }

        int sortingOffset = 0;
        foreach (ParticleSystemRenderer particleRenderer in
                 instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            particleRenderer.sortingLayerName = "Foreground";
            particleRenderer.sortingOrder = 5000 + sortingOffset++;
        }
    }

    // Lặp bùm tới khi user bấm Nhận Quà.
    private IEnumerator VfxBurstLoop()
    {
        var wait = new WaitForSecondsRealtime(Mathf.Max(0.15f, vfxBurstInterval));
        while (_activeVfxRoot != null)
        {
            yield return wait;
            if (_activeVfxRoot == null) yield break;

            // Bùm lại tất cả emitter → cảm giác "bùm bùm bùm" liên tục
            foreach (ParticleSystem ps in _activeVfxRoot.GetComponentsInChildren<ParticleSystem>(true))
                ps.Play(false);
        }
    }

    private void StopVFX()
    {
        if (_vfxLoop != null) { StopCoroutine(_vfxLoop); _vfxLoop = null; }

        // Dọn pháo hoa UI nếu đang chạy — container này là con của contentPanel/popup,
        // KHÔNG nằm trong _activeVfxRoot (root đó chỉ chứa ParticleSystem cũ).
        if (_uiFireworksRoutine != null) { StopCoroutine(_uiFireworksRoutine); _uiFireworksRoutine = null; }
        // [B5] dừng từng "bùm" đang bay + huỷ root của chúng (đều là con của _activeUiFireworksRoot)
        for (int i = 0; i < _fireworkBurstRoutines.Count; i++)
            if (_fireworkBurstRoutines[i] != null) StopCoroutine(_fireworkBurstRoutines[i]);
        _fireworkBurstRoutines.Clear();
        for (int i = 0; i < _fireworkBurstRoots.Count; i++)
            if (_fireworkBurstRoots[i] != null) Destroy(_fireworkBurstRoots[i]);
        _fireworkBurstRoots.Clear();
        if (_activeUiFireworksRoot != null) { Destroy(_activeUiFireworksRoot); _activeUiFireworksRoot = null; }

        if (_activeVfxRoot == null) return;

        foreach (ParticleSystem particleSystem in
                 _activeVfxRoot.GetComponentsInChildren<ParticleSystem>(true))
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Destroy(_activeVfxRoot);
        _activeVfxRoot = null;
    }

    // =========================================================================
    // UI Fireworks — pháo hoa dựng bằng UI thuần (Image + RectTransform)
    //
    // LÝ DO: Canvas_Popup dùng Render Mode = Screen Space – Overlay
    // (Assets/_Game/Scenes/SCN_Farm.unity, m_RenderMode: 0). Unity LUÔN vẽ
    // Overlay Canvas SAU CÙNG, đè lên mọi thứ do Camera vẽ. Pháo hoa cũ là
    // ParticleSystem (do Camera vẽ) nên dù ép particleRenderer.sortingLayerName /
    // sortingOrder cao cỡ nào cũng KHÔNG BAO GIỜ nổi lên trên popup — sortingOrder
    // chỉ so được giữa các đối tượng CÙNG do Camera vẽ với nhau, không so được với
    // một Canvas Overlay riêng. Đây là giới hạn cứng của Unity, KHÔNG sửa bằng
    // sortingOrder nữa (đời trước đã thử và sai chỗ).
    //
    // GIẢI PHÁP: dựng hạt pháo hoa bằng Image (CanvasRenderer) làm CON của popup
    // (contentPanel) → cùng hệ vẽ Overlay với popup → chỉ cần SetAsLastSibling()
    // là chắc chắn vẽ trên mọi thành phần khác của popup, không cần đụng tới
    // renderMode của Canvas_Popup (canvas dùng chung nhiều popup khác).
    // =========================================================================

    private struct UIFireworkParticle
    {
        public RectTransform rt;
        public Image         image;
        public Vector2       velocity;
        public float         angularSpeed;
        public float         rotationStart;
    }

    // Bảng màu lễ hội dùng khi chưa kéo sprite (fireworkSprites rỗng).
    private static readonly Color[] kUIFireworksFallbackPalette =
    {
        new Color(1f,    0.35f, 0.35f), // đỏ
        new Color(1f,    0.78f, 0.20f), // vàng cam
        new Color(0.40f, 0.85f, 1f),    // xanh dương
        new Color(0.55f, 1f,    0.55f), // xanh lá
        new Color(1f,    0.50f, 0.90f), // hồng
        new Color(0.80f, 0.60f, 1f),    // tím
    };

    private static Sprite s_uiFireworksFallbackSprite;
    private static bool   s_uiFireworksFallbackSpriteTried;

    // Sprite tròn mặc định có sẵn trong mọi build Unity (dùng khi chưa có
    // fireworkSprites) để hạt trông "bo tròn" thay vì hình vuông cứng.
    // Chỉ thử lấy 1 lần rồi cache — null vẫn chấp nhận được (fallback ra khối
    // màu vuông phẳng, hiệu ứng vẫn chạy đúng như yêu cầu).
    private static Sprite GetUIFireworksFallbackSprite()
    {
        if (!s_uiFireworksFallbackSpriteTried)
        {
            s_uiFireworksFallbackSpriteTried = true;
            s_uiFireworksFallbackSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
        return s_uiFireworksFallbackSprite;
    }

    /// <summary>
    /// Bắn pháo hoa bằng UI thuần, làm con của <paramref name="parent"/> (contentPanel của
    /// popup) rồi SetAsLastSibling() để chắc chắn nổi trên mọi thành phần khác của popup —
    /// xem giải thích ở comment ngay trên đầu region này.
    /// </summary>
    /// <summary>
    /// [V3 2026-09-04] Lop phu TOAN KHUNG danh rieng cho phao hoa.
    ///
    /// Vi sao can: phao hoa cu la con cua contentPanel - tuc la con cua chinh cai card.
    /// He qua (Sep bao 04/09): (1) hat chi bay duoc trong pham vi card, phan con lai cua man
    /// hinh trong tron; (2) hat an theo animation phong to 0.6->1.0 cua card nen luc no bi co
    /// lai; (3) moi thu ve sau card o cap popupRoot deu de len => cam giac "bi che phu".
    ///
    /// Lop nay la con TRUC TIEP cua popupRoot, keo gian full man (anchor 0->1),
    /// SetAsLastSibling() nen ve SAU ca nen mo lan card, va KHONG chiu scale cua card.
    /// Khong co Image, khong raycast => tuyet doi khong chan nut Claim.
    /// Tai dung object cu neu da co, tranh rac moi lan len cap.
    /// </summary>
    private RectTransform EnsureFireworksLayer()
    {
        Transform host = popupRoot != null ? popupRoot.transform : transform;

        Transform found = host.Find(kFireworksLayerName);
        RectTransform layer = found as RectTransform;

        // Object cu ton tai nhung KHONG phai RectTransform (ai do tao tay bang Transform thuong)
        // => neu chi kiem 'layer == null' thi moi lan len cap lai de mot object trung ten,
        // rac tich luy dan trong scene. Xoa cai hong roi tao lai cho dut diem.
        if (found != null && layer == null)
        {
            Destroy(found.gameObject);
            found = null;
        }

        if (layer == null)
        {
            var go = new GameObject(kFireworksLayerName, typeof(RectTransform));
            layer = (RectTransform)go.transform;
            layer.SetParent(host, false);
        }

        layer.anchorMin  = Vector2.zero;
        layer.anchorMax  = Vector2.one;
        layer.offsetMin  = Vector2.zero;
        layer.offsetMax  = Vector2.zero;
        layer.pivot      = new Vector2(0.5f, 0.5f);
        layer.localScale = Vector3.one;   // KHONG an theo scale-pop cua card
        layer.SetAsLastSibling();         // ve sau nen mo + card => noi tren cung khung

        return layer;
    }

    private void SpawnUIFireworks(RectTransform parent)
    {
        if (parent == null) return;

        if (_uiFireworksRoutine != null) { StopCoroutine(_uiFireworksRoutine); _uiFireworksRoutine = null; }
        if (_activeUiFireworksRoot != null) { Destroy(_activeUiFireworksRoot); _activeUiFireworksRoot = null; }

        _activeUiFireworksRoot = new GameObject("FX_Fireworks_UI", typeof(RectTransform));
        var containerRt = (RectTransform)_activeUiFireworksRoot.transform;
        containerRt.SetParent(parent, false);
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.pivot     = new Vector2(0.5f, 0.5f);
        containerRt.anchoredPosition = Vector2.zero;
        containerRt.sizeDelta = Vector2.zero;
        containerRt.SetAsLastSibling(); // Nổi trên mọi thành phần khác của popup (nút, text...)

        if (fireworkSprites == null || fireworkSprites.Length == 0)
        {
            Debug.Log("[LevelUpPopupUI] fireworkSprites đang trống → pháo hoa UI dùng khối màu " +
                "phẳng tạm thời. Sau khi import confetti_01..06.png / spark_star.png (nguồn: " +
                "production/art-handoff/2026-08-31_JuiceFX/1_Celebrate_FX/) vào Assets, kéo chúng " +
                "vào field 'Firework Sprites' trên Inspector của LevelUpPopupUI.");
        }

        // [B5 PHÁO HOA LẶP — 2026-09-05] Trước đây bắn ĐÚNG 1 bùm 1.5s rồi Destroy → popup còn
        // hiện 10s mà pháo hoa đã tắt ("pháo hoa mất"). Nay chạy vòng lặp tới khi StopVFX.
        _uiFireworksRoutine = StartCoroutine(VongLapPhaoHoa(containerRt));
    }

    /// <summary>
    /// [B5] Vòng lặp pháo hoa: bùm đầu ngay tâm, sau đó mỗi 0.8–1.2s bắn thêm 1 bùm tại vị trí
    /// ngẫu nhiên (x 15–85%, y 55–90% khung cha — nửa trên, tránh che nút Nhận), tối đa
    /// <see cref="kMaxConcurrentBursts"/> bùm cùng lúc. Dừng bởi StopVFX (ClaimAndClose / OnDisable).
    /// </summary>
    private IEnumerator VongLapPhaoHoa(RectTransform container)
    {
        var khung = container != null ? container.parent as RectTransform : null;
        bool bumDau = true;

        while (container != null && _activeUiFireworksRoot != null)
        {
            _fireworkBurstRoots.RemoveAll(r => r == null);   // bùm đã tàn tự Destroy → dọn list
            if (_fireworkBurstRoots.Count < kMaxConcurrentBursts)
            {
                float w = khung != null && khung.rect.width  > 10f ? khung.rect.width  : 1080f;
                float h = khung != null && khung.rect.height > 10f ? khung.rect.height : 1920f;
                Vector2 viTri = bumDau
                    ? new Vector2(0f, h * 0.2f)
                    : new Vector2((Random.Range(0.15f, 0.85f) - 0.5f) * w, (Random.Range(0.55f, 0.90f) - 0.5f) * h);
                bumDau = false;

                var bum   = new GameObject($"Burst_{Time.frameCount}", typeof(RectTransform));
                var bumRt = (RectTransform)bum.transform;
                bumRt.SetParent(container, false);
                bumRt.anchorMin = bumRt.anchorMax = new Vector2(0.5f, 0.5f);
                bumRt.pivot     = new Vector2(0.5f, 0.5f);
                bumRt.anchoredPosition = viTri;
                bumRt.sizeDelta = Vector2.zero;

                _fireworkBurstRoots.Add(bum);
                _fireworkBurstRoutines.Add(StartCoroutine(RunUIFireworks(bumRt)));
            }

            float cho = Random.Range(0.8f, 1.2f), t = 0f;
            while (t < cho) { t += Time.unscaledDeltaTime; yield return null; }
        }
        _uiFireworksRoutine = null;
    }

    /// <summary>Một "bùm" pháo hoa: 24–40 hạt bay từ tâm <paramref name="container"/> rồi tàn trong 1.5s;
    /// xong tự Destroy container (root bùm). [B5] không đụng _activeUiFireworksRoot nữa — root đó sống cả vòng lặp.</summary>
    private IEnumerator RunUIFireworks(RectTransform container)
    {
        if (container == null) yield break;
        int particleCount = Random.Range(24, 41); // 24-40 hạt
        var particles = new List<UIFireworkParticle>(particleCount);

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject($"Spark_{i:00}", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(container, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            float size = Random.Range(14f, 30f);
            rt.sizeDelta = new Vector2(size, size);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false; // BẮT BUỘC — không được chặn click nút của popup

            if (fireworkSprites != null && fireworkSprites.Length > 0)
            {
                img.sprite = fireworkSprites[Random.Range(0, fireworkSprites.Length)];
                img.color  = Color.white;
            }
            else
            {
                img.sprite = GetUIFireworksFallbackSprite(); // null vẫn ra khối màu phẳng, chạy được
                img.color  = kUIFireworksFallbackPalette[Random.Range(0, kUIFireworksFallbackPalette.Length)];
            }

            float angle = Random.Range(0f, Mathf.PI * 2f);
            // [V3] Lop phu toan khung rong hon card nhieu => nhan toc do de hat lap day khung.
            float speed = Random.Range(220f, 520f) * Mathf.Max(0.5f, fireworkSpreadBoost);
            var particle = new UIFireworkParticle
            {
                rt            = rt,
                image         = img,
                velocity      = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                angularSpeed  = Random.Range(-540f, 540f),
                rotationStart = Random.Range(0f, 360f),
            };
            rt.localEulerAngles = new Vector3(0f, 0f, particle.rotationStart);
            particles.Add(particle);
        }

        const float kTotalDuration   = 1.5f;  // 1.2 - 1.8s theo yêu cầu
        const float kGravity         = -680f; // trọng lực nhẹ, kéo hạt rơi dần
        const float kFadeStartRatio  = 0.55f; // bắt đầu fade alpha từ 55% thời lượng

        float elapsed = 0f;
        while (elapsed < kTotalDuration)
        {
            // Time.unscaledDeltaTime: popup có thể mở lúc Time.timeScale = 0 (khớp với
            // fade cũ ở FadeCanvasGroup và VfxBurstLoop vốn đã dùng unscaled/realtime).
            if (container == null) yield break;   // [B5] StopVFX đã huỷ root giữa chừng
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;

            for (int i = 0; i < particles.Count; i++)
            {
                UIFireworkParticle p = particles[i];
                if (p.rt == null) continue;

                p.velocity += new Vector2(0f, kGravity * dt);
                p.rt.anchoredPosition += p.velocity * dt;
                p.rt.localEulerAngles = new Vector3(0f, 0f, p.rotationStart + p.angularSpeed * elapsed);

                float lifeRatio = elapsed / kTotalDuration;
                if (lifeRatio > kFadeStartRatio)
                {
                    float fadeT = (lifeRatio - kFadeStartRatio) / (1f - kFadeStartRatio);
                    Color c = p.image.color;
                    c.a = Mathf.Clamp01(1f - fadeT);
                    p.image.color = c;
                }
                particles[i] = p;
            }

            yield return null;
        }

        // [B5] Bùm tàn → chỉ huỷ root của CHÍNH bùm này; container cha + vòng lặp vẫn chạy.
        if (container != null)
        {
            _fireworkBurstRoots.Remove(container.gameObject);
            Destroy(container.gameObject);
        }
    }

    // =========================================================================
    // Input Lock
    // =========================================================================

    private void AcquireInputLock()
    {
        if (!_inputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            _inputLockHeld = true;
        }
    }

    private void ReleaseInputLock()
    {
        if (_inputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            _inputLockHeld = false;
        }
    }

    // =========================================================================
    // Debug
    // =========================================================================

#if UNITY_EDITOR
    [ContextMenu("Debug: Preview Level 2 Popup")]
    private void DebugPreviewL2()
    {
        _lastKnownLevel = 1;
        HandleLevelChanged(2);
    }

    [ContextMenu("Debug: Preview Level 5 Popup (Cooking Unlock)")]
    private void DebugPreviewL5()
    {
        _lastKnownLevel = 4;
        HandleLevelChanged(5);
    }
#endif
}
