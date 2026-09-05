using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


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

    // Danh sách ô mở khoá đã dò xong (tránh GetComponentsInChildren mỗi lần mở popup).
    private UnlockSlotUI[]      _unlockSlotsCache;
    // Số ô cần chạy hiệu ứng "bung ra". Phải HOÃN tới sau khi popupRoot bật —
    // xem PlayUnlockPops() để biết lý do.
    private int                 _pendingUnlockPopCount;
    private bool                _warnedNoUnlockSlots;

    // [V2 ADD] Chống nhận quà 2 lần khi tap màn hình và bấm nút gần như đồng thời
    // (tap-to-close + claimButton đều dẫn về ClaimAndClose). Reset mỗi lần mở popup.
    private bool                _v2Closing;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================

    private void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);

        if (PlayerProgressManager.Instance != null)
        {
            _lastKnownLevel = PlayerProgressManager.Instance.Level;
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        }
        else
        {
            Debug.LogWarning("[LevelUpPopupUI] PlayerProgressManager.Instance không tìm thấy tại Start(). " +
                             "Đặt PlayerProgressManager vào scene trước LevelUpPopupUI.");
        }

        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimAndClose);
    }

    private void OnDestroy()
    {
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;

        IsActive = false;   // tránh kẹt cờ nếu popup bị huỷ giữa chừng
        StopVFX();
        StopV2Fx();         // [V2 ADD] dừng sparkle/nhân vật/tap-catcher khi popup bị huỷ
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

    private void ShowNextPopup()
    {
        if (_levelUpQueue.Count == 0)
        {
            _isShowing = false;
            IsActive   = false;          // hết popup → tutorial được phép chạy tiếp
            return;
        }

        int level = _levelUpQueue.Dequeue();
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

        // Chỉ tới ĐÂY các ô mở khoá mới thật sự activeInHierarchy → mới chạy được
        // coroutine hiệu ứng. Gọi sớm hơn (trong PopulateUI) là vô tác dụng.
        PlayUnlockPops();

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
                quaHienThi.Add(new LevelRewardConfig.ItemGift { itemId = "__gold", displayName = "Vàng", amount = cfg.giftGold, icon = TimIconVangV4() });
            if (cfg.giftGems > 0)
                quaHienThi.Add(new LevelRewardConfig.ItemGift { itemId = "__gem", displayName = "Kim cương", amount = cfg.giftGems, icon = TimIconGemV4() });
            quaHienThi.AddRange(quaCanVe);

            // [V4 ADD] Vàng/gem đã có ô tròn riêng → tắt 2 dòng chữ cũ (khỏi lặp thông tin)
            if (goldRewardRow != null) goldRewardRow.SetActive(false);
            if (gemRewardRow  != null) gemRewardRow.SetActive(false);
            if (giftItemsContainer != null)
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

            // Unlock descriptions — LẤY TỪ GetUnlockLabels(), KHÔNG đọc thẳng
            // cfg.unlockDescriptions: danh sách chữ đó là bản sao tay của unlockEntries,
            // rất dễ lệch khi sửa một bên mà quên bên kia.
            if (unlockDescText != null)
            {
                var nhan = cfg.GetUnlockLabels();
                if (nhan.Count > 0)
                {
                    unlockDescText.text = "Mở khóa: " + string.Join(", ", nhan);
                    unlockDescText.gameObject.SetActive(true);
                }
                else
                {
                    unlockDescText.gameObject.SetActive(false);
                }
            }

            // Hint text
            if (hintText != null)
            {
                bool hasHint = !string.IsNullOrEmpty(cfg.hintText);
                hintText.text = hasHint ? cfg.hintText : "";
                hintText.gameObject.SetActive(hasHint);
            }
        }
        else
        {
            // Không có config → hiển thị minimal
            if (goldRewardRow != null) goldRewardRow.SetActive(false);
            if (gemRewardRow  != null) gemRewardRow.SetActive(false);
            if (unlockDescText != null) unlockDescText.gameObject.SetActive(false);
            if (hintText      != null) hintText.gameObject.SetActive(false);

            Debug.Log($"[LevelUpPopupUI] Không tìm thấy LevelRewardConfig cho level {level}. " +
                      "Tạo asset và kéo vào levelRewardConfigs list.");
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
            if (inUse && icon == null) inUse = false;        // [V2 ADD]

            // BẮT BUỘC: ô thừa phải TẮT hẳn. HorizontalLayoutGroup bỏ qua con đang tắt
            // nên dải icon tự co lại vừa số ô — không để khoảng trống.
            if (slot.gameObject.activeSelf != inUse)
                slot.gameObject.SetActive(inUse);

            if (!inUse) continue;

            withIcon++;

            // caption để rỗng: nhãn chữ đã hiện gộp ở dòng "Mở khóa: ..." (unlockDescText),
            // nhồi thêm chữ vào ô 190px sẽ tràn khung.
            slot.Setup(icon, true, "");
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
        if (_pendingUnlockPopCount <= 0) return;

        var slots = ResolveUnlockSlots();
        int n = Mathf.Min(_pendingUnlockPopCount, slots.Length);
        for (int i = 0; i < n; i++)
            if (slots[i] != null && slots[i].gameObject.activeInHierarchy)
                slots[i].PlayPop(i);

        _pendingUnlockPopCount = 0;
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
            _unlockSlotsCache = unlockSlotsContainer.GetComponentsInChildren<UnlockSlotUI>(true);
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
        _unlockSlotsCache = searchRoot.GetComponentsInChildren<UnlockSlotUI>(true);
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
        StopVFX(); // bấm Nhận Quà → tắt pháo hoa NGAY rồi mới đóng popup
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
            for (int i = 0; i < celebrationSlots.Length; i++)
                if (celebrationSlots[i] != null)
                    celebrationSlots[i].Play();   // slot thiếu frames sẽ tự SetActive(false)

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

        Camera renderCamera = Camera.main;
        if (renderCamera == null)
        {
            Debug.LogWarning("[LevelUpPopupUI] Main Camera not found. Lana VFX cannot be placed from UI screen space.");
            return;
        }

        _activeVfxRoot = new GameObject("LevelUpPopup_VFX_Runtime");

        if (vfxConfettiPrefab != null)
        {
            // Pháo hoa: bắn 3 điểm phía trên (giữa + 2 góc) cho rầm rộ
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_Top",
                GetVfxScreenPoint(VfxPlacement.Top),      renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopLeft",
                GetVfxScreenPoint(VfxPlacement.TopLeft),  renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
            SpawnWorldVfx(vfxConfettiPrefab, "LevelUp_Confetti_TopRight",
                GetVfxScreenPoint(VfxPlacement.TopRight), renderCamera, 15.09f, vfxTopDemoScale * vfxScaleBoost);
        }

        if (vfxSidePrefab != null)
        {
            // Lana03 hai bên — to hơn confetti
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Left",
                GetVfxScreenPoint(VfxPlacement.Left),  renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
            SpawnWorldVfx(vfxSidePrefab, "LevelUp_Flash_Lana03_Right",
                GetVfxScreenPoint(VfxPlacement.Right), renderCamera, 20f, vfxSideDemoScale * vfxSideScaleBoost);
        }

        // KHÔNG tự huỷ sau vài giây nữa — lặp "bùm bùm bùm" tới khi user bấm Nhận Quà (StopVFX dừng).
        if (_vfxLoop != null) StopCoroutine(_vfxLoop);
        _vfxLoop = StartCoroutine(VfxBurstLoop());
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
        float cameraDistance = renderCamera.nearClipPlane + 1f;
        Vector3 worldPosition = renderCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, cameraDistance));

        GameObject instance = Instantiate(
            prefab,
            worldPosition,
            Quaternion.identity,
            _activeVfxRoot.transform);
        instance.name = instanceName;

        float worldScale = renderCamera.orthographic
            ? (renderCamera.orthographicSize / demoOrthoSize) * demoScale
            : demoScale;
        instance.transform.localScale = Vector3.one * worldScale;

        foreach (ParticleSystem particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.useUnscaledTime = true;

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
            particleRenderer.sortingOrder = 1000 + sortingOffset++;
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
