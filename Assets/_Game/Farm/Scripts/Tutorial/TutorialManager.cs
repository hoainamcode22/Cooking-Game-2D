using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    // =========================================================================
    // Singleton
    // =========================================================================
    public static TutorialManager Instance { get; private set; }

    /// <summary>Tên step hiện tại (asset name), null nếu tutorial chưa chạy.
    /// Dùng cho failsafe của TutorialPrePlant (bỏ qua step 04b khi không có ô chín sẵn).</summary>
    public string CurrentStepName =>
        (_currentIndex >= 0 && _currentIndex < _steps.Count && _steps[_currentIndex] != null)
            ? _steps[_currentIndex].name
            : null;

    // =========================================================================
    // State
    // =========================================================================
    private enum TutorialState
    {
        Idle,
        Intro,
        TypingText,
        WaitingAction,
        Transitioning,
        Finished,
    }
    private TutorialState _state = TutorialState.Idle;

    // =========================================================================
    // Target Registry
    // =========================================================================
    private static readonly Dictionary<string, TutorialTarget> _targetRegistry =
        new Dictionary<string, TutorialTarget>();

    public static void RegisterTarget(string id, TutorialTarget t)
    {
        if (!string.IsNullOrEmpty(id)) _targetRegistry[id] = t;
    }
    public static void UnregisterTarget(string id, TutorialTarget target = null)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (target == null)
        {
            _targetRegistry.Remove(id);
            return;
        }

        if (_targetRegistry.TryGetValue(id, out var current) && current == target)
            _targetRegistry.Remove(id);
    }

    /// <summary>Returns the RectTransform for a registered tutorial target, or null.</summary>
    public static RectTransform GetTargetRect(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return _targetRegistry.TryGetValue(id, out var t) ? t?.RectTransform : null;
    }

    /// <summary>Exposes hand pointer for TutorialDragHintAnimator to control.</summary>
    public RectTransform HandPointerRT => _handPointer;

    // =========================================================================
    // Inspector â€” Steps
    // =========================================================================
    [Header("Steps (tá»± Ä‘á»™ng gÃ¡n bá»Ÿi TutorialSystemGenerator)")]
    [SerializeField] private List<TutorialStepData> _steps = new();

    // =========================================================================
    // Inspector â€” Core UI
    // =========================================================================
    [Header("Core UI")]
    [SerializeField] private UnmaskRaycastFilter _dimBackground;
    [SerializeField] private GameObject          _npcDialogPopup;
    [SerializeField] private TextMeshProUGUI     _npcDialogText;
    [SerializeField] private Image               _npcPortrait;
    [SerializeField] private RectTransform       _handPointer;
    [SerializeField] private Animator            _handAnimator;

    [Header("Guide Board (4-step popup)")]
    [SerializeField] private TutorialGuideBoardUI _guideBoardUI;

    [Header("Camera Focus")]
    [SerializeField] private TutorialCameraFocus _cameraFocus;

    [Header("Runtime Target & Drag Hint")]
    [SerializeField] private TutorialRuntimeTargetResolver _runtimeTargetResolver;
    [SerializeField] private TutorialDragHintAnimator      _dragHintAnimator;
    [SerializeField] private TutorialActionHandGuide       _actionHandGuide;

    // =========================================================================
    // Inspectorâ€” Intro Animation
    // =========================================================================
    [Header("Intro â€” ÄÃ¡m MÃ¢y")]
    [SerializeField] private GameObject    _cloudPanel;
    [SerializeField] private RectTransform _cloudLeft;
    [SerializeField] private RectTransform _cloudRight;

    [Tooltip("Sá»‘ Ä‘Æ¡n vá»‹ canvas mÃ¢y bay ra ngoÃ i mÃ n hÃ¬nh (>= ná»­a chiá»u rá»™ng canvas)")]
    [SerializeField] private float _cloudSlideDistance = 620f;

    [Tooltip("Thá»i gian animation mÃ¢y bay (giÃ¢y)")]
    [SerializeField] private float _introDuration = 1.5f;

    [SerializeField] private AnimationCurve _introEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Intro â€” Camera Zoom")]
    [SerializeField] private TutorialCameraZoom _cameraZoom;

    // =========================================================================
    // Inspector â€” Settings
    // =========================================================================
    [Header("Settings")]
    [SerializeField] private bool _clickToSkipTyping = true;

    [Tooltip("Bá» qua intro animation khi debug trong Editor")]
    [SerializeField] private bool _skipIntroInEditor = false;

    // =========================================================================
    // Runtime
    // =========================================================================
    private int                _currentIndex = -1;
    private Coroutine          _typingCoroutine;
    private bool               _typingDone;
    private TutorialWaitAction _pendingWait;
    private bool               _hasQueuedAction;
    private TutorialWaitAction _queuedAction;
    private bool               _interactionDialogDismissed;
    private bool               _penOpenSubActionReceived;

    private Vector2 _cloudLeftOrigin;
    private Vector2 _cloudRightOrigin;

    // CanvasGroup â€” táº¯t blocksRaycasts khi áº©n Ä‘á»ƒ UI tÃ ng hÃ¬nh khÃ´ng nuá»‘t click game
    private CanvasGroup _cloudPanelCG;
    private CanvasGroup _tutorialCanvasCG;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (_cloudLeft  != null) _cloudLeftOrigin  = _cloudLeft.anchoredPosition;
        if (_cloudRight != null) _cloudRightOrigin = _cloudRight.anchoredPosition;

        // Cache CanvasGroup Ä‘á»ƒ Ä‘iá»u khiá»ƒn blocksRaycasts khi áº©n/hiá»‡n
        if (_cloudPanel != null)
            _cloudPanelCG = _cloudPanel.GetComponent<CanvasGroup>();

        // Tutorial_Canvas lÃ  cha cá»§a Dim_Background â€” leo lÃªn tÃ¬m CanvasGroup
        if (_dimBackground != null)
            _tutorialCanvasCG = _dimBackground.GetComponentInParent<Canvas>()
                                              ?.GetComponent<CanvasGroup>();

        SetTutorialUIVisible(false);
        SetCloudPanelVisible(true);
        _guideBoardUI?.Hide();

        if (_steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] KhÃ´ng cÃ³ step nÃ o. HÃ£y gÃ¡n TutorialStepData vÃ o _steps.");
            return;
        }

#if UNITY_EDITOR
        if (_skipIntroInEditor) { SetCloudPanelVisible(false); StartTutorial(); return; }
#endif

        StartCoroutine(PlayIntroAnimation());
    }

    // =========================================================================
    // Intro Animation
    // =========================================================================

    /// <summary>
    /// (1) Lerp mÃ¢y bay ra hai bÃªn trong _introDuration.
    /// (2) Camera zoom in song song.
    /// (3) Sau khi xong â†’ StartTutorial() â†’ Step 1.
    /// </summary>
    private IEnumerator PlayIntroAnimation()
    {
        _state = TutorialState.Intro;

        // KHÔNG zoom camera trong intro (theo yêu cầu) — camera giữ khung gameplay.

        var leftCG  = EnsureCanvasGroup(_cloudLeft);
        var rightCG = EnsureCanvasGroup(_cloudRight);

        Vector3 leftScale0  = _cloudLeft  != null ? _cloudLeft.localScale  : Vector3.one;
        Vector3 rightScale0 = _cloudRight != null ? _cloudRight.localScale : Vector3.one;

        var leftEnd  = _cloudLeftOrigin  + new Vector2(-_cloudSlideDistance, 0f);
        var rightEnd = _cloudRightOrigin + new Vector2( _cloudSlideDistance, 0f);

        // Giữ mây 1 nhịp ngắn cho cảm giác "bình minh ló dạng" trước khi tách
        yield return new WaitForSeconds(0.25f);

        float elapsed = 0f;
        while (elapsed < _introDuration)
        {
            elapsed += Time.deltaTime;
            float t = _introEase.Evaluate(Mathf.Clamp01(elapsed / _introDuration));

            // Trượt ra hai bên + phóng to nhẹ + mờ dần nửa sau → mượt, bắt mắt
            if (_cloudLeft != null)
            {
                _cloudLeft.anchoredPosition = Vector2.Lerp(_cloudLeftOrigin, leftEnd, t);
                _cloudLeft.localScale       = Vector3.Lerp(leftScale0, leftScale0 * 1.15f, t);
            }
            if (_cloudRight != null)
            {
                _cloudRight.anchoredPosition = Vector2.Lerp(_cloudRightOrigin, rightEnd, t);
                _cloudRight.localScale       = Vector3.Lerp(rightScale0, rightScale0 * 1.15f, t);
            }

            float fade = Mathf.InverseLerp(0.45f, 1f, t); // bắt đầu mờ từ 45% thời lượng
            if (leftCG  != null) leftCG.alpha  = 1f - fade;
            if (rightCG != null) rightCG.alpha = 1f - fade;

            yield return null;
        }

        SetCloudPanelVisible(false);

        // Reset trạng thái mây để lần replay sau vẫn đẹp
        if (leftCG  != null) leftCG.alpha  = 1f;
        if (rightCG != null) rightCG.alpha = 1f;
        if (_cloudLeft  != null) _cloudLeft.localScale  = leftScale0;
        if (_cloudRight != null) _cloudRight.localScale = rightScale0;

        yield return null;
        StartTutorial();
    }

    private CanvasGroup EnsureCanvasGroup(RectTransform rt)
    {
        if (rt == null) return null;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Báº¯t Ä‘áº§u tutorial tá»« Step 0 (gá»i sau intro animation).</summary>
    public void StartTutorial()
    {
        _state        = TutorialState.Idle;
        _currentIndex = -1;
        SetTutorialUIVisible(true);

        // KHÔNG focus camera ở màn chào mừng — camera chỉ lia vào 6 ô đất
        // khi tới bước L1L2_04_FocusPlots (sau khi player bấm "ĐÃ RÕ").

        Debug.Log($"[Tutorial] StartTutorial — total steps: {_steps.Count}");
        AdvanceToNextStep();
    }

    /// <summary>
    /// Chuyá»ƒn bÆ°á»›c tiáº¿p theo.
    /// â€¢ Äang typewriter â†’ skip text.
    /// â€¢ Äang WaitForClick â†’ advance.
    /// Gá»i tá»« Button "Tiáº¿p Theo" trÃªn NPC_Dialog_Popup (auto-wired bá»Ÿi Generator).
    /// </summary>
    public void NextStep()
    {
        if (TryDismissInteractionDialog()) return;

        if (_state == TutorialState.TypingText && _clickToSkipTyping)
        {
            SkipTyping();
            return;
        }
        if (_state == TutorialState.WaitingAction &&
            _pendingWait == TutorialWaitAction.WaitForClick)
        {
            AdvanceToNextStep();
        }
    }

    public void ConfirmGuidePopup()
    {
        if (_state != TutorialState.WaitingAction
            || _pendingWait != TutorialWaitAction.WaitForClick
            || _currentIndex < 0
            || _currentIndex >= _steps.Count
            || _steps[_currentIndex] == null
            || !_steps[_currentIndex].showGuideBoard)
            return;

        _guideBoardUI?.Hide();
        AdvanceToNextStep();
    }

    /// <summary>Game systems gá»i Ä‘á»ƒ bÃ¡o player hoÃ n thÃ nh hÃ nh Ä‘á»™ng.</summary>
    public void NotifyAction(TutorialWaitAction action)
    {
        if (_state == TutorialState.WaitingAction && _pendingWait == action)
        {
            if (TryConsumePenOpenSubAction(action))
                return;

            AdvanceToNextStep();
            return;
        }

        if (_state == TutorialState.TypingText || _state == TutorialState.Transitioning)
        {
            if (_currentIndex < 0 || _currentIndex >= _steps.Count
                || _steps[_currentIndex] == null
                || _steps[_currentIndex].waitAction != action)
                return;
            _hasQueuedAction = true;
            _queuedAction = action;
        }
    }

    // Convenience wrappers â€” tá»«ng game system gá»i Ä‘Ãºng loáº¡i
    public void NotifyPlant()       => NotifyAction(TutorialWaitAction.WaitForPlant);
    public void NotifyHarvest()     => NotifyAction(TutorialWaitAction.WaitForHarvest);
    public void NotifyCook()        => NotifyAction(TutorialWaitAction.WaitForCook);

    /// <summary>Gá»i khi player giao hÃ ng thÃ nh cÃ´ng cho NhÃ  DÃ¢n (Level 2).</summary>
    public void NotifyDelivery()    => NotifyAction(TutorialWaitAction.WaitForDelivery);

    /// <summary>Gá»i khi player mua váº­t pháº©m trong Shop (Level 2 â€” chuá»“ng gÃ , gÃ ).</summary>
    public void NotifyBuyItem()     => NotifyAction(TutorialWaitAction.WaitForBuyItem);

    /// <summary>Gọi khi player mua hạt giống. Riêng bước mua Ngô L2 yêu cầu đúng Ngô và đủ 8 hạt.</summary>
    public void NotifyBuySeed(string itemId, string cropId, int quantity)
    {
        if (CurrentStepName == "L2_03_BuyCorn"
            && (!IsCornSeed(itemId, cropId) || quantity < 8))
            return;

        NotifyAction(TutorialWaitAction.WaitForBuyItem);
    }

    /// <summary>Gá»i khi player mua gia sÃºc (gÃ , bÃ²â€¦).</summary>
    public void NotifyBuyAnimal()   => NotifyAction(TutorialWaitAction.WaitForBuyAnimal);

    /// <summary>Gá»i khi player giao Ä‘á»§ hÃ ng cho TÃ u Hoáº£ (Level 4).</summary>
    public void NotifyTrainLoad()   => NotifyAction(TutorialWaitAction.WaitForTrainLoad);

    public void NotifyAllPlotsPlanted()         => NotifyAction(TutorialWaitAction.WaitForAllPlotsPlanted);
    public void NotifyAllPlotsHarvested()       => NotifyAction(TutorialWaitAction.WaitForAllPlotsHarvested);
    public void NotifyAllFlowerPlotsPlanted()   => NotifyAction(TutorialWaitAction.WaitForAllFlowerPlotsPlanted);
    public void NotifyAllFlowerPlotsHarvested() => NotifyAction(TutorialWaitAction.WaitForAllFlowerPlotsHarvested);
    public void NotifyOpenCropProcess()         => NotifyAction(TutorialWaitAction.WaitForOpenCropProcess);
    public void NotifySpeedUp()                 => NotifyAction(TutorialWaitAction.WaitForSpeedUp);
    public void NotifySickleShown()             => NotifyAction(TutorialWaitAction.WaitForSickleShown);
    public void NotifySeedPanelOpened()          => NotifyAction(TutorialWaitAction.WaitForSeedPanel);
    public void NotifyOpenShop()                 => NotifyAction(TutorialWaitAction.WaitForOpenShop);
    public void NotifyCloseShop()                => NotifyAction(TutorialWaitAction.WaitForCloseShop);
    public void NotifyOpenPen()                  => NotifyAction(TutorialWaitAction.WaitForOpenPen);
    public void NotifyFeed()                     => NotifyAction(TutorialWaitAction.WaitForFeed);
    public void NotifyPenSpeedUp()               => NotifyAction(TutorialWaitAction.WaitForPenSpeedUp);
    public void NotifyPenHarvest()               => NotifyAction(TutorialWaitAction.WaitForPenHarvest);

    // =========================================================================
    // State Machine Core
    // =========================================================================
    // Index bắt đầu phase hoa (L1L2_11_TransitionFlower = index 11, zero-based —
    // đã +1 sau khi chèn L1L2_04b_FirstHarvest ở index 4, Hay Day opening)
    // Camera transitions are keyed by step name so inserting guide popups is safe.

    private void AdvanceToNextStep()
    {
        _actionHandGuide?.StopGuide();
        _dragHintAnimator?.StopDragHint();
        _runtimeTargetResolver?.EnableAreaMask(TutorialAreaKind.None, null); // tắt nền xám (nếu đang bật)
        _interactionDialogDismissed = false;
        _penOpenSubActionReceived = false;
        _currentIndex++;

        if (_currentIndex >= _steps.Count)
        {
            FinishTutorial();
            return;
        }

        var step = _steps[_currentIndex];
        Debug.Log($"[Tutorial] Step [{_currentIndex}/{_steps.Count - 1}] {step.name} — waitAction={step.waitAction} showGuideBoard={step.showGuideBoard}");

        // Khi bắt đầu phase hoa: focus camera vào chậu hoa
        // Re-focus camera when reaching rice planting phase (L1L2_04_FocusPlots = index 3)
        if (step.name == "L1L2_04_FocusPlots" && _cameraFocus != null)
        {
            var bridge = GetComponent<TutorialStepTriggerBridge>();
            _cameraFocus.FocusOnRice(bridge);
        }

        // L1L2_11_TransitionFlower: KHÔNG focus hoa ở đây nữa — phải chờ user bấm "Nhận"
        // ở popup lên cấp 2 trước (xử lý trong PlayStep → WaitForLevelUpClaim).

        _state = TutorialState.Transitioning;
        StartCoroutine(PlayStep(step));
    }

    private IEnumerator PlayStep(TutorialStepData step)
    {
        // ─── Nhường "sân khấu" cho popup LÊN CẤP ───
        // Thu hoạch xong 8 ô lúa = đúng 40 EXP → lên cấp 2. Bước chuyển sang trồng hoa
        // phải ĐỢI popup lên cấp hiện ra + user bấm "Nhận" rồi mới bắt đầu (focus hoa + tay quét).
        if (step.name == "L1L2_11_TransitionFlower")
        {
            yield return WaitForLevelUpClaim();
            if (_cameraFocus != null)
                _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
        }

        if (_dimBackground != null)
            _dimBackground.gameObject.SetActive(true);

        // ─── GUIDE THÔNG MINH: nền xám bao vùng + tay CHỈ quét ô CÒN VIỆC (theo tiến độ user) ───
        // Ô đất — trồng: chờ user kéo hạt đủ tất cả ô; tay chỉ vào ô còn trống.
        if (step.name == "L1L2_04_FocusPlots" || step.name == "L1L2_06_PlantAllRice")
        {
            SetupSmartGuide(TutorialAreaKind.Rice, harvestMode: false);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            BatWatchdogHetHat(step);
            ConsumeQueuedAction(); yield break;
        }
        // Ô đất — thu hoạch: tay chỉ vào ô đã chín, chờ thu hoạch hết.
        if (step.name == "L1L2_10_HarvestAllRice")
        {
            SetupSmartGuide(TutorialAreaKind.Rice, harvestMode: true);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // Chậu hoa — trồng.
        if (step.name == "L1L2_12_FocusFlowerPots" || step.name == "L1L2_14_PlantAllFlowers")
        {
            if (_cameraFocus != null) _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
            SetupSmartGuide(TutorialAreaKind.Flower, harvestMode: false);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            BatWatchdogHetHat(step);
            ConsumeQueuedAction(); yield break;
        }
        // Chậu hoa — thu hoạch.
        if (step.name == "L1L2_17_HarvestAllFlowers")
        {
            if (_cameraFocus != null) _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
            SetupSmartGuide(TutorialAreaKind.Flower, harvestMode: true);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }

        // ═══ TUTORIAL L2 — SHOP & TRỒNG NGÔ (B1–B7) ═══
        // L2_01: tay chỉ Home→Store (tự nhảy khi menu mở), chờ shop mở.
        if (step.name == "L2_01_GotoShop")
        {
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            _dimBackground?.ClearHole();
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
            _actionHandGuide?.GuidePointFirstActive(new[] { "btn_store", "btn_home" });
            _pendingWait = step.waitAction;   // WaitForOpenShop
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_03: bao xám quanh item Ngô + tay chỉ Ngô/＋, chờ mua.
        if (step.name == "L2_03_BuyCorn")
        {
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            // TẮT lớp tối TRƯỚC khi chờ. Lớp tối không có lỗ thì chặn 100% click
            // (UnmaskRaycastFilter trả true khi không có target), nên để nó bật suốt
            // 0,4 giây này là khoá cứng shop đúng lúc người chơi vừa mở ra.
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);

            // Cuộn shop để item Ngô hiện TRỌN (kèm ＋/－/Mua) rồi mới set vùng sáng + tay.
            ShopManager.Instance?.ScrollItemIntoView("seed_ngo");
            yield return new WaitForSecondsRealtime(0.4f);

            _runtimeTargetResolver?.RefreshShopTargets();
            var cornRect = GetTargetRect("shop_corn_plus") ?? GetTargetRect("shop_corn");
            if (_dimBackground != null)
            {
                // Không tìm được ô Ngô thì KHÔNG bật lớp tối. Bật mà không khoét lỗ
                // sẽ chặn sạch click ⇒ người chơi không bấm mua được, kẹt luôn ở bước này.
                if (cornRect != null)
                {
                    _dimBackground.gameObject.SetActive(true);
                    _dimBackground.SetTarget(cornRect, false, 18f);
                }
                else
                {
                    _dimBackground.ClearHole();
                    _dimBackground.gameObject.SetActive(false);
                    Debug.LogWarning("[Tutorial] L2_03_BuyCorn: không thấy item Ngô trong shop → " +
                                     "bỏ lớp tối để người chơi vẫn bấm mua được.");
                }
            }
            // Tay chỉ nút ＋ tới khi chọn đủ 8 ngô → nhảy sang nút Mua.
            _actionHandGuide?.GuideShopBuy("shop_corn_plus", "shop_corn_buy", "shop_corn", 8, _dimBackground);
            _pendingWait = step.waitAction;   // WaitForBuyItem
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_04: tay chỉ Btn_Close, chờ đóng shop.
        if (step.name == "L2_04_CloseShop")
        {
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            _runtimeTargetResolver?.RefreshShopTargets();
            var closeRect = GetTargetRect("shop_close") ?? GetTargetRect("btn_close");
            if (_dimBackground != null)
            {
                // Cùng lý do như L2_03: lớp tối KHÔNG có lỗ thì chặn sạch click.
                // Không tìm được nút đóng mà vẫn bật lớp tối ⇒ không đóng shop được ⇒
                // bước WaitForCloseShop treo vĩnh viễn.
                if (closeRect != null)
                {
                    _dimBackground.gameObject.SetActive(true);
                    _dimBackground.SetTarget(closeRect, false, 18f);
                }
                else
                {
                    _dimBackground.ClearHole();
                    _dimBackground.gameObject.SetActive(false);
                    Debug.LogWarning("[Tutorial] L2_04_CloseShop: không thấy nút đóng shop → " +
                                     "bỏ lớp tối để người chơi vẫn đóng được.");
                }
            }
            // Ưu tiên nút đóng CỦA SHOP (shop_close, đăng ký scoped trong shopPanel) — tránh trùng "Btn_Close" của popup khác.
            _actionHandGuide?.GuidePointFirstActive(new[] { "shop_close", "btn_close" });
            _pendingWait = step.waitAction;   // WaitForCloseShop
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_05: trồng ngô 8 ô (tái dùng 8 ô — reset đếm), tay quét ô trống.
        if (step.name == "L2_05_PlantCorn")
        {
            GetComponent<TutorialStepTriggerBridge>()?.ResetRiceTracking();
            if (_cameraFocus != null) _cameraFocus.FocusOnRice(GetComponent<TutorialStepTriggerBridge>());
            SetupSmartGuide(TutorialAreaKind.Rice, harvestMode: false);
            _pendingWait = step.waitAction;   // WaitForAllPlotsPlanted
            _state = TutorialState.WaitingAction;
            BatWatchdogHetHat(step);
            ConsumeQueuedAction(); yield break;
        }

        // ═══ TUTORIAL L2 — CHĂN NUÔI (B8–B13) ═══
        // L2_07: zoom Pen_03 + tay chỉ giữa chuồng, chờ mở chuồng.
        if (step.name == "L2_07_FocusPen")
        {
            if (_cameraFocus != null) _cameraFocus.FocusOnPen("Pen_03");
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            _dragHintAnimator?.StopDragHint();
            _dimBackground?.ClearHole();
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            _actionHandGuide?.GuidePoint("tutorial_pen");
            _pendingWait = step.waitAction;   // WaitForOpenPen
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_08: kéo thức ăn (lúa) vào chuồng — tay drag-guide feed→pen, chờ cho ăn.
        if (step.name == "L2_08_FeedPen")
        {
            _actionHandGuide?.StopGuide();
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            _dimBackground?.ClearHole();
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
            _dragHintAnimator?.StartDragHint("tutorial_feed", "tutorial_pen");
            _pendingWait = step.waitAction;   // WaitForFeed
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_09: sau khi feed xong, chỉ chuồng -> user click mở process -> chỉ nút Gem.
        if (step.name == "L2_09_PenSpeedUp")
        {
            _dragHintAnimator?.StopDragHint();
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            _dimBackground?.ClearHole();
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);

            _penOpenSubActionReceived = IsTargetActive("tutorial_pen_gem");
            _actionHandGuide?.GuidePoint("tutorial_pen");
            _pendingWait = TutorialWaitAction.WaitForOpenPen;
            _state = TutorialState.WaitingAction;
            yield return new WaitUntil(() =>
                _penOpenSubActionReceived || IsTargetActive("tutorial_pen_gem"));

            _actionHandGuide?.GuidePoint("tutorial_pen_gem");
            _pendingWait = step.waitAction;   // WaitForPenSpeedUp
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_10: bubble Ready hiện -> chỉ giữa chuồng -> user click mở rổ -> drag-guide basket→pen.
        if (step.name == "L2_10_HarvestPen")
        {
            _actionHandGuide?.StopGuide();
            _dragHintAnimator?.StopDragHint();
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI?.Hide();
            _dimBackground?.ClearHole();
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);

            _penOpenSubActionReceived = IsTargetActive("tutorial_basket");
            _actionHandGuide?.GuidePoint("tutorial_pen");
            _pendingWait = TutorialWaitAction.WaitForOpenPen;
            _state = TutorialState.WaitingAction;
            yield return new WaitUntil(() =>
                _penOpenSubActionReceived || IsTargetActive("tutorial_basket"));

            _actionHandGuide?.StopGuide();
            _dragHintAnimator?.StartDragHint("tutorial_basket", "tutorial_pen");
            _pendingWait = step.waitAction;   // WaitForPenHarvest
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }

        // 1. Resolve target
        RectTransform targetRect = null;
        if (!string.IsNullOrEmpty(step.targetID))
        {
            if (_targetRegistry.TryGetValue(step.targetID, out var tutTarget))
                targetRect = tutTarget.RectTransform;
            else
                Debug.Log($"[Tutorial] Hand pointer target '{step.targetID}' chua dang ky — hand pointer se an.");
        }

        // 2. Dim / highlight
        if (targetRect != null)
            _dimBackground.SetTarget(targetRect, step.useCircleHole, step.holePaddingPx);
        else
            _dimBackground.ClearHole();

        // 3. Hand Pointer
        UpdateHandPointer(step, targetRect);
        if (step.showHandPointer)
            Debug.Log($"[Tutorial] Hand pointer target: {(targetRect != null ? targetRect.name : "NONE")}");

        // Drag hint animation
        if (!string.IsNullOrEmpty(step.dragToTargetId))
            _dragHintAnimator?.StartDragHint(step.targetID, step.dragToTargetId);
        else
            _dragHintAnimator?.StopDragHint();

        if (IsActionOnlyStep(step.name))
        {
            HideBlockingTutorialUI();
            _pendingWait = step.waitAction;
            _state = TutorialState.WaitingAction;

            // Các bước có action-guide (speedup/harvest) phải TẮT tay tĩnh để không hiện 2 bàn tay.
            bool startsGuide =
                step.name == "L1L2_07_OpenCropProgress" || step.name == "L1L2_08_SpeedUpTip"  ||
                step.name == "L1L2_09_HarvestFirstRice" || step.name == "L1L2_10_HarvestAllRice" ||
                step.name == "L1L2_17_HarvestAllFlowers";
            if (startsGuide && _handPointer != null)
                _handPointer.gameObject.SetActive(false);

            if (step.name == "L1L2_07_OpenCropProgress"
                || step.name == "L1L2_08_SpeedUpTip")
                _actionHandGuide?.GuideSpeedUp("tutorial_plot_01");
            else if (step.name == "L1L2_09_HarvestFirstRice")
                _actionHandGuide?.GuideHarvest("tutorial_plot_01");
            else if (step.name == "L1L2_10_HarvestAllRice")
                _actionHandGuide?.GuideHarvest("tutorial_plot_01");
            else if (step.name == "L1L2_17_HarvestAllFlowers")
                _actionHandGuide?.GuideHarvest("tutorial_flower_01");

            // Bao gồm L1L2_05_DragFirstRice và L1L2_13_DragFirstFlower — hai bước này
            // cũng chờ WaitForPlant nên cũng cần hạt, và cũng không có timeout.
            // HatCanChoBuoc() trả null cho các bước không cần hạt → không làm gì.
            BatWatchdogHetHat(step);

            ConsumeQueuedAction();
            yield break;
        }

        // 4. NPC Portrait
        if (_npcPortrait != null)
        {
            _npcPortrait.sprite = step.npcPortrait;
            _npcPortrait.gameObject.SetActive(step.npcPortrait != null);
        }

        // 4b. Guide Board
        if (step.showGuideBoard && _guideBoardUI != null)
        {
            Debug.Log("[Tutorial] Showing guide board.");
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
            _guideBoardUI.ShowForStep(step.name);
            _state       = TutorialState.WaitingAction;
            _pendingWait = step.waitAction;
            yield break;
        }
        else
        {
            if (_guideBoardUI != null) _guideBoardUI.Hide();
            if (_npcDialogPopup != null) _npcDialogPopup.SetActive(true);
        }

        // 5. Typewriter
        _state = TutorialState.TypingText;
        yield return StartTyping(step.npcText, step.typingSpeed);

        // 6. Chá» action
        _pendingWait = step.waitAction;
        Debug.Log($"[Tutorial] Waiting for: {step.waitAction}");

        if (step.waitAction == TutorialWaitAction.Auto)
        {
            yield return new WaitForSeconds(0.8f);
            AdvanceToNextStep();
        }
        else
        {
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction();
        }
    }

    // =========================================================================
    // Typewriter Effect
    // =========================================================================
    private IEnumerator StartTyping(string fullText, float speed)
    {
        _typingDone = false;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(TypeRoutine(fullText, speed));
        yield return new WaitUntil(() => _typingDone);
    }

    private IEnumerator TypeRoutine(string fullText, float speed)
    {
        _npcDialogText.text = "";
        foreach (char c in fullText)
        {
            _npcDialogText.text += c;
            yield return new WaitForSeconds(speed);
        }
        _typingDone = true;
    }

    private void SkipTyping()
    {
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _npcDialogText.text  = _steps[_currentIndex].npcText;
        _typingDone          = true;
        _state               = TutorialState.WaitingAction;
        _pendingWait         = _steps[_currentIndex].waitAction;

        if (_pendingWait == TutorialWaitAction.Auto) AdvanceToNextStep();
    }

    // =========================================================================
    // Hand Pointer
    // =========================================================================
    private void UpdateHandPointer(TutorialStepData step, RectTransform targetRect)
    {
        if (_handPointer == null) return;
        // Drag hint animator owns hand pointer position when running
        if (_dragHintAnimator != null && _dragHintAnimator.IsRunning) return;

        bool show = step.showHandPointer && targetRect != null;
        _handPointer.gameObject.SetActive(show);
        if (!show) return;

        _handPointer.position         = targetRect.position;
        _handPointer.anchoredPosition += step.handOffset;

        if (_handAnimator != null) _handAnimator.SetTrigger("Bounce");
    }

    // =========================================================================
    // Finish
    // =========================================================================
    private void FinishTutorial()
    {
        _state = TutorialState.Finished;
        Debug.Log("[Tutorial] Tutorial FINISHED — restoring camera and closing UI.");
        SetTutorialUIVisible(false);
        _dimBackground?.ClearHole();
        _cameraFocus?.RestoreCamera();
        _dragHintAnimator?.StopDragHint();
        _actionHandGuide?.StopGuide();

        // Táº¯t hoÃ n toÃ n Tutorial_Canvas â€” khÃ´ng Ä‘á»ƒ Canvas tÃ ng hÃ¬nh cháº·n raycast game UI
        if (_tutorialCanvasCG != null)
        {
            _tutorialCanvasCG.alpha          = 0f;
            _tutorialCanvasCG.interactable   = false;
            _tutorialCanvasCG.blocksRaycasts = false;
        }

    }

    // =========================================================================
    // Helpers
    // =========================================================================

    // =========================================================================
    //  CHỐNG TREO: các bước "trồng cho hết ô" khi người chơi KHÔNG CÒN HẠT
    // =========================================================================
    //  Cổng qua bước của những bước này là "không còn ô nào trống"
    //  (TutorialStepTriggerBridge.AllRiceFieldPlanted / AllUnlockedNonEmpty).
    //  Cổng đó KHÔNG có timeout. Nếu người chơi hết hạt giữa đường thì không còn
    //  cách nào làm cho hết ô trống ⇒ tutorial đứng im vĩnh viễn, không thông báo gì.
    //
    //  Kho hạt giống ĐÃ được lưu (WarehouseManager.Save/Load) nên tình huống này hiếm hơn
    //  nhiều so với trước. Nhưng vẫn xảy ra được: tutorial chạy lại từ bước 0 mỗi lần Play,
    //  nên người chơi ở cấp cao đã dùng hết hạt sẽ gặp lại đúng bước "trồng cho hết ô".
    //  Watchdog là lưới an toàn cho trường hợp đó.
    //
    //  Watchdog này chỉ nhả bước khi CHẮC CHẮN bế tắc: hết hạt cần dùng VÀ hết luôn
    //  mọi loại hạt khác (lấp ô bằng hạt nào cũng được), liên tục trong 6 giây.
    //  Còn dù chỉ 1 hạt → không can thiệp, để người chơi tự làm.

    /// <summary>Hạt mà bước "trồng cho hết ô" này cần. null = bước không cần hạt.</summary>
    private static string HatCanChoBuoc(string stepName)
    {
        switch (stepName)
        {
            case "L1L2_04_FocusPlots":
            case "L1L2_05_DragFirstRice":
            case "L1L2_06_PlantAllRice":      return "seed_rice";
            case "L1L2_12_FocusFlowerPots":
            case "L1L2_13_DragFirstFlower":
            case "L1L2_14_PlantAllFlowers":   return "seed_huong_duong";
            case "L2_05_PlantCorn":           return "seed_ngo";
            default:                          return null;
        }
    }

    private void BatWatchdogHetHat(TutorialStepData step)
    {
        string hat = HatCanChoBuoc(step != null ? step.name : null);
        if (hat != null) StartCoroutine(WatchdogHetHat(step, hat));
    }

    // Hạt trồng ở Ô ĐẤT (Normal) và hạt trồng ở CHẬU HOA — hai bảng chọn hạt RIÊNG,
    // KHÔNG dùng lẫn nhau được. Vì vậy phải đếm theo đúng loại ô đang bị kẹt: còn đầy
    // hạt lúa mà hết hạt hoa thì bước trồng hoa VẪN bế tắc.
    //
    // Liệt kê tường minh chứ KHÔNG dò tiền tố "seed_": `Khoai_Tay.asset` và
    // `Ca_Rot.asset` có seedItemId là `khoai_tay` / `ca_rot` — KHÔNG có tiền tố đó.
    // Dò tiền tố sẽ đếm chúng thành 0 và nhả bước oan trong khi người chơi vẫn trồng được.
    private static readonly string[] HAT_O_DAT = {
        "seed_rice", "seed_ngo", "seed_bapcai", "seed_cachua", "ca_rot", "khoai_tay",
        "seed_nam", "seed_sugarcane", "seed_lemon", "seed_chili", "seed_pepper",
    };
    private static readonly string[] HAT_CHAU_HOA = {
        "seed_huong_duong", "seed_hoa_hong", "seed_hoa_oai_huong", "seed_hoa_cuc_trang",
        "seed_hoa_lan", "seed_tulip", "seed_hoa_cuc_van_tho", "seed_hoa_anh_thao",
        "seed_hoa_cam_tu_cau", "seed_hoa_mau_don",
    };

    /// <summary>Tổng số hạt CÒN DÙNG ĐƯỢC cho loại ô mà hạt này thuộc về.</summary>
    private static int TongSoHatDungDuoc(string seedIdCanDung)
    {
        var kho = WarehouseManager.Instance;
        if (kho == null) return 0;

        bool laHoa = System.Array.IndexOf(HAT_CHAU_HOA, seedIdCanDung) >= 0;
        var bang = laHoa ? HAT_CHAU_HOA : HAT_O_DAT;

        int tong = 0;
        foreach (string id in bang) tong += kho.GetAmount(id);
        return tong;
    }

    private IEnumerator WatchdogHetHat(TutorialStepData step, string seedId)
    {
        const float NGUONG_GIAY = 6f;
        float canKho = 0f;

        // Chỉ canh đúng bước này, đúng lần chạy này. Bước đổi → thoát ngay.
        while (_state == TutorialState.WaitingAction
               && _currentIndex >= 0 && _currentIndex < _steps.Count
               && _steps[_currentIndex] == step)
        {
            // Đang mở Shop = người chơi đang tự đi mua hạt → KHÔNG được nhả bước,
            // nếu không tutorial nhảy ngay giữa lúc họ đang bấm mua.
            bool dangMuaHang = ShopManager.Instance != null && ShopManager.Instance.IsOpen;

            var kho = WarehouseManager.Instance;
            bool hetHat = kho != null
                          && !dangMuaHang
                          && kho.GetAmount(seedId) <= 0
                          && TongSoHatDungDuoc(seedId) <= 0;

            canKho = hetHat ? canKho + Time.unscaledDeltaTime : 0f;

            if (canKho >= NGUONG_GIAY)
            {
                Debug.LogWarning(
                    $"[Tutorial] '{step.name}' bị BẾ TẮC: kho hết sạch hạt (cần '{seedId}') " +
                    $"nên không thể trồng cho hết ô — cổng qua bước không bao giờ đạt. " +
                    "Tự nhả bước để tutorial không đứng im. " +
                    "Cách xử lý: mua thêm hạt trong Shop, hoặc dùng " +
                    "Tools ▸ SCN Farm ▸ Hard Reset Everything để chơi lại từ đầu.");
                AdvanceToNextStep();
                yield break;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Bật guide thông minh cho 1 vùng: ẩn dialog/tay tĩnh, bật nền xám bao vùng,
    /// và cho bàn tay quét CHỈ những ô còn việc (trồng = ô trống, harvest = ô chín).
    /// </summary>
    private void SetupSmartGuide(TutorialAreaKind kind, bool harvestMode)
    {
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
        _guideBoardUI?.Hide();
        if (_handPointer != null) _handPointer.gameObject.SetActive(false); // tránh 2 bàn tay

        if (_dimBackground != null) _dimBackground.gameObject.SetActive(true);
        _runtimeTargetResolver?.EnableAreaMask(kind, _dimBackground);

        string[] ids = kind == TutorialAreaKind.Flower
            ? new[] {
                "tutorial_flower_01", "tutorial_flower_02", "tutorial_flower_03",
                "tutorial_flower_04", "tutorial_flower_05", "tutorial_flower_06" }
            : new[] {
                "tutorial_plot_01", "tutorial_plot_02", "tutorial_plot_03", "tutorial_plot_04",
                "tutorial_plot_05", "tutorial_plot_06", "tutorial_plot_07", "tutorial_plot_08" };

        _actionHandGuide?.GuideSweepPlots(ids, harvestMode);
    }

    /// <summary>
    /// Chờ popup LÊN CẤP xuất hiện rồi chờ user bấm "Nhận" đóng hẳn — để bước trồng hoa
    /// "nhường sân khấu" cho popup. EXP bay về avatar mất ~1 nhịp nên popup hiện trễ →
    /// cho cửa sổ chờ xuất hiện tối đa 4 giây. Nếu không có popup (vd chơi lại ở cấp cao)
    /// thì sau 4 giây tự bỏ qua, tutorial chạy tiếp bình thường.
    /// </summary>
    private IEnumerator WaitForLevelUpClaim()
    {
        // Chỉ "nhường sân khấu" nếu user đang ở CẤP 1 — tức sắp lên cấp 2 nhờ thu hoạch lúa
        // (8 ô lúa = đúng 40 EXP = mốc cấp 2). Nếu đã ≥ cấp 2 (vd chơi lại) → bỏ qua, vào hoa ngay.
        bool expectLevelUp = PlayerProgressManager.Instance == null
                             || PlayerProgressManager.Instance.Level < 2;
        if (!expectLevelUp)
        {
            Debug.Log("[Tutorial] WaitForLevelUpClaim: đã ≥ cấp 2 → bỏ qua chờ, vào trồng hoa ngay.");
            yield break;
        }

        // Ẩn UI tutorial khi đang đợi để không che/đè popup lên cấp.
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
        _guideBoardUI?.Hide();
        if (_handPointer != null) _handPointer.gameObject.SetActive(false);
        _dimBackground?.ClearHole();
        if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);

        // 1) Chờ popup hiện. EXP "bay" về avatar mất ~2.8s (orb nằm đất 2s rồi mới bay) →
        //    mới cộng EXP → lên cấp → bật popup. Cho cửa sổ chờ tối đa 12s cho an toàn.
        float t = 0f;
        while (!LevelUpPopupUI.IsActive && t < 12f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        bool popupShown = LevelUpPopupUI.IsActive;

        // 2) Popup đã hiện → chờ user bấm "Nhận" cho tới khi đóng hoàn toàn (KHÔNG giới hạn).
        while (LevelUpPopupUI.IsActive)
            yield return null;

        // 3) Nhịp thở nhỏ cho mượt trước khi vào hướng dẫn trồng hoa.
        if (popupShown)
            yield return new WaitForSecondsRealtime(0.25f);

        Debug.Log($"[Tutorial] WaitForLevelUpClaim xong (popupShown={popupShown}) → bắt đầu hướng dẫn trồng hoa.");
    }

    private void SetTutorialUIVisible(bool visible)
    {
        if (_dimBackground  != null) _dimBackground.gameObject.SetActive(visible);
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(visible);
        if (_handPointer    != null) _handPointer.gameObject.SetActive(false);
    }

    private bool TryDismissInteractionDialog()
    {
        if (_currentIndex < 0 || _currentIndex >= _steps.Count) return false;
        var step = _steps[_currentIndex];
        if (step == null || _interactionDialogDismissed || !IsInteractionStep(step.name))
            return false;
        if (_state != TutorialState.TypingText && _state != TutorialState.WaitingAction)
            return false;

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_npcDialogText != null) _npcDialogText.text = step.npcText;
        _typingDone = true;
        _interactionDialogDismissed = true;
        _pendingWait = step.waitAction;
        _state = TutorialState.WaitingAction;
        HideBlockingTutorialUI();

        switch (step.name)
        {
            case "L1L2_07_OpenCropProgress":
            case "L1L2_15_FlowerSpeedUp":
                _actionHandGuide?.GuideSpeedUp(
                    step.name == "L1L2_15_FlowerSpeedUp"
                        ? "tutorial_flower_01"
                        : "tutorial_plot_01");
                break;
            case "L1L2_09_HarvestFirstRice":
                _actionHandGuide?.GuideHarvest("tutorial_plot_01");
                break;
            case "L1L2_16_HarvestFirstFlower":
                _actionHandGuide?.GuideHarvest("tutorial_flower_01");
                break;
            case "L1L2_12_FocusFlowerPots":
                UpdateHandPointer(step, TutorialManager.GetTargetRect(step.targetID));
                break;
        }

        ConsumeQueuedAction();
        return true;
    }

    private void HideBlockingTutorialUI()
    {
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
        if (_dimBackground != null)
        {
            _dimBackground.ClearHole();
            _dimBackground.gameObject.SetActive(false);
        }
    }

    private void ConsumeQueuedAction()
    {
        if (!_hasQueuedAction || _queuedAction != _pendingWait) return;
        _hasQueuedAction = false;
        AdvanceToNextStep();
    }

    private bool TryConsumePenOpenSubAction(TutorialWaitAction action)
    {
        if (action != TutorialWaitAction.WaitForOpenPen)
            return false;

        string step = CurrentStepName;
        if (step != "L2_09_PenSpeedUp" && step != "L2_10_HarvestPen")
            return false;

        _penOpenSubActionReceived = true;
        return true;
    }

    private static bool IsTargetActive(string targetId)
    {
        RectTransform rt = GetTargetRect(targetId);
        return rt != null && rt.gameObject.activeInHierarchy;
    }

    private static bool IsInteractionStep(string stepName)
    {
        // L1L2_12_FocusFlowerPots giờ xử lý như bước sweep (giống L1L2_04), không còn là interaction.
        return stepName == "L1L2_15_FlowerSpeedUp"
            || stepName == "L1L2_16_HarvestFirstFlower";
    }

    private static bool IsCornSeed(string itemId, string cropId)
    {
        return string.Equals(itemId, "seed_ngo", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cropId, "ngo", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActionOnlyStep(string stepName)
    {
        return stepName == "L1L2_04_FocusPlots"
            || stepName == "L1L2_05_DragFirstRice"
            || stepName == "L1L2_06_PlantAllRice"
            || stepName == "L1L2_07_OpenCropProgress"
            || stepName == "L1L2_08_SpeedUpTip"
            || stepName == "L1L2_09_HarvestFirstRice"
            || stepName == "L1L2_10_HarvestAllRice"
            || stepName == "L1L2_13_DragFirstFlower"
            || stepName == "L1L2_14_PlantAllFlowers"
            || stepName == "L1L2_17_HarvestAllFlowers";
    }

    private void SetCloudPanelVisible(bool visible)
    {
        if (_cloudPanel == null) return;

        if (_cloudPanelCG != null)
        {
            // CanvasGroup: áº©n hoÃ n toÃ n ká»ƒ cáº£ raycast â€” khÃ´ng cáº§n SetActive(false)
            _cloudPanelCG.alpha          = visible ? 1f : 0f;
            _cloudPanelCG.interactable   = visible;
            _cloudPanelCG.blocksRaycasts = visible;
        }
        else
        {
            _cloudPanel.SetActive(visible);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Next Step")]
    private void DebugNextStep() => NextStep();

    [ContextMenu("Debug: Skip to Finish")]
    private void DebugSkipAll() { _currentIndex = _steps.Count - 1; FinishTutorial(); }

    [ContextMenu("Debug: Replay Intro")]
    private void DebugReplayIntro()
    {
        if (_cloudLeft  != null) _cloudLeft.anchoredPosition  = _cloudLeftOrigin;
        if (_cloudRight != null) _cloudRight.anchoredPosition = _cloudRightOrigin;
        SetCloudPanelVisible(true);
        SetTutorialUIVisible(false);
        StartCoroutine(PlayIntroAnimation());
    }
#endif
}
