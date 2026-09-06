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
    /// <summary>
    /// [VÒNG 15] Tutorial CÓ ĐANG CHẠY không (đã bắt đầu, chưa kết thúc).
    /// Gameplay hỏi cái này để MIỄN PHÍ mọi thao tác trong lúc hướng dẫn — người chơi mới
    /// vào game chỉ có 5 kim cương, mà nút tăng tốc đòi tới 29 thì bước đó là ngõ cụt.
    /// </summary>
    public bool DangChayTutorial =>
        _currentIndex >= 0 && _currentIndex < _steps.Count && _state != TutorialState.Finished;

    public bool IsActive => DangChayTutorial;

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
    [Header("Steps (tự động gán bởi TutorialSystemGenerator)")]
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
    [Header("Intro — Đám Mây")]
    [SerializeField] private GameObject    _cloudPanel;
    [SerializeField] private RectTransform _cloudLeft;
    [SerializeField] private RectTransform _cloudRight;

    [Tooltip("Số đơn vị canvas mây bay ra ngoài màn hình (>= nửa chiều rộng canvas)")]
    [SerializeField] private float _cloudSlideDistance = 620f;

    [Tooltip("Thời gian animation mây bay (giây)")]
    [SerializeField] private float _introDuration = 1.5f;

    [SerializeField] private AnimationCurve _introEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Intro — Camera Zoom")]
    [SerializeField] private TutorialCameraZoom _cameraZoom;

    // ═══════════════════════════════════════════════════════════════════════
    // [V2 2026-09-04] TUTORIAL V2 — card bo góc + NPC 12 frame + VFX + camera easing
    // ═══════════════════════════════════════════════════════════════════════
    // AN TOÀN THEO THIẾT KẾ: để trống _v2Card thì DungCardV2 = false ⇒ tutorial chạy
    // Y HỆT bản cũ. Scene chưa chạy tool dựng V2 sẽ không đổi hành vi một chút nào.
    [Header("── TUTORIAL V2 (để trống = dùng bản cũ) ──")]
    [Tooltip("Card hội thoại V2. ĐỂ TRỐNG ⇒ toàn bộ V2 tắt, tutorial về nguyên bản cũ 100%.")]
    [SerializeField] private TutorialDialogueCard _v2Card;

    [Tooltip("Đạo diễn hiệu ứng V2. Để trống ⇒ bỏ qua VFX, mọi thứ khác vẫn chạy.")]
    [SerializeField] private TutorialVfxDirector _v2Vfx;

    [Tooltip("Đạo diễn camera V2 (zoom có easing). Để trống ⇒ dùng TutorialCameraFocus cũ.")]
    [SerializeField] private TutorialCameraDirector _v2Camera;

    [Tooltip("Công tắc tổng V2. Bỏ tick là về bản cũ ngay cả khi đã gán đủ ref.")]
    [SerializeField] private bool _useV2Dialogue = true;

    /// <summary>Có dùng card V2 cho bước này không. Thiếu ref ⇒ FALSE ⇒ chạy đường cũ.</summary>
    private bool DungCardV2 => _useV2Dialogue && _v2Card != null;

    // =========================================================================
    // Inspector â€” Settings
    // =========================================================================
    [Header("Settings")]
    [SerializeField] private bool _clickToSkipTyping = true;

    [Tooltip("Bỏ qua intro animation khi debug trong Editor")]
    [SerializeField] private bool _skipIntroInEditor = false;

    // =========================================================================
    //  B1 · B2 — CỜ "ĐÃ XONG TUTORIAL"
    // =========================================================================
    // VÌ SAO: `Start()` trước đây LUÔN chạy `PlayIntroAnimation()` → `StartTutorial()` →
    // step 0, MỖI LẦN bấm Play. Người chơi đã xong hướng dẫn, đã lên cấp 12, mở game lên
    // vẫn bị dắt lại từ "kéo hạt lúa vào ô đất" — và bị KHOÁ CỨNG ở đó, vì cổng qua bước là
    // "không còn ô nào trống" mà ruộng của họ đang trồng đầy. Watchdog hết hạt chỉ đỡ được
    // trường hợp hết hạt, không đỡ được trường hợp này.
    //
    // Cờ lưu ở PlayerPrefs `TUTORIAL_MAIN_DONE`, thuộc họ save "TUTORIAL" có saveVersion
    // — xem `SaveVersionGuard`.

    /// <summary>Khoá PlayerPrefs: đã chạy hết tutorial chính chưa.</summary>
    private const string PrefKeyDone = "TUTORIAL_MAIN_DONE";
    // [WP-A1] Bước tutorial đang đứng (lưu mỗi lần sang bước) — thoát app giữa chừng thì
    // lần sau mở lại tiếp đúng bước, không phải dắt lại từ đầu. Xoá khi xong / khi reset cờ.
    private const string PrefKeyStep = "TUTORIAL_STEP_INDEX";

    // ═════════════════════════════════════════════════════════════════════════
    //  TÊN CHUỒNG DÙNG TRONG TUTORIAL — MỘT CHỖ DUY NHẤT
    // ═════════════════════════════════════════════════════════════════════════
    //  Tên này trước đây được gõ cứng ở BỐN file khác nhau (TutorialManager,
    //  TutorialCameraFocus, TutorialRuntimeTargetResolver, AnimalGuideController).
    //  Đổi chuồng dạy tutorial thì phải sửa cả bốn, sót một chỗ là camera lia tới
    //  chuồng này còn bàn tay chỉ vào chuồng khác — mà không có lỗi nào báo.
    //  Giờ đổi ở đây là đổi hết.

    /// <summary>
    /// Chuồng mà tutorial L2 dạy cho ăn.
    ///
    /// Phải là CHUỒNG GÀ (`Pen_03`) vì nó mở khoá SỚM NHẤT — cấp 2, 100 vàng. Thứ tự
    /// mở: gà (L2) → heo (L4) → bò thịt (L8) → bò sữa (L13). Trỏ vào chuồng bò `Pen_01`
    /// là dạy người chơi cấp 2 dùng công trình họ chỉ mua được ở cấp 8.
    ///
    /// Kèm điều kiện: bản `Pen_03` trong scene phải đang BẬT, không thì `GameObject.Find`
    /// trả null và bước `L2_07_FocusPen` treo. Dùng
    /// Tools ▸ Farm ▸ Chuồng ▸ Bật chuồng tutorial để kiểm và sửa.
    /// </summary>
    public const string TenChuongTutorial = "Pen_03";

    /// <summary>
    /// Tên cần dò trong scene. Có bản `(Clone)` vì chuồng do người chơi mua sẽ được
    /// `PlacementManager` Instantiate ra và Unity tự thêm hậu tố đó.
    /// </summary>
    public static readonly string[] TenChuongCanDo =
        { TenChuongTutorial, TenChuongTutorial + "(Clone)" };

    /// <summary>Họ save + phiên bản của MỌI cờ hướng dẫn. Tăng khi đổi ý nghĩa các cờ.</summary>
    private const string TutorialSaveFamily  = "TUTORIAL";
    private const int    TutorialSaveVersion = 1;

    [Header("Dev — chạy lại tutorial để test (B2)")]
    [Tooltip("Tick để BỎ QUA cờ đã-xong và chạy lại tutorial từ bước 0 ở lần Play tới. " +
             "Cờ trong PlayerPrefs vẫn giữ nguyên, bỏ tick là trở lại bình thường.")]
    [SerializeField] private bool _devForceReplayTutorial = false;

    [Tooltip("Tick để XOÁ HẲN cờ đã-xong ngay khi vào scene → lần Play sau cũng chạy lại " +
             "dù đã bỏ tick ô trên. Dùng khi muốn thử đúng trải nghiệm người chơi MỚI.")]
    [SerializeField] private bool _devClearDoneFlagOnStart = false;

    /// <summary>Đã chạy hết tutorial chính chưa (đọc từ PlayerPrefs).</summary>
    public static bool IsTutorialDone => PlayerPrefs.GetInt(PrefKeyDone, 0) == 1;

    /// <summary>Đóng dấu đã xong. Gọi từ <see cref="FinishTutorial"/>.</summary>
    private static void MarkTutorialDone()
    {
        PlayerPrefs.SetInt(PrefKeyDone, 1);
        PlayerPrefs.DeleteKey(PrefKeyStep);   // [WP-A1] xong rồi thì không cần resume nữa
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    /// <summary>Xoá cờ để chơi lại như người mới. Dùng cho tool reset save.</summary>
    public static void ClearTutorialDoneFlag()
    {
        PlayerPrefs.DeleteKey(PrefKeyDone);
        PlayerPrefs.DeleteKey(PrefKeyStep);   // [WP-A1] chơi lại từ đầu ⇒ bỏ luôn bước đã lưu
        SaveVersionGuard.Clear(TutorialSaveFamily);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    // =========================================================================
    // Runtime
    // =========================================================================
    private int                _currentIndex = -1;
    private Coroutine          _typingCoroutine;
    private bool               _typingDone;
    private TutorialWaitAction _pendingWait;
    // [VÒNG 17] Trước đây chỉ giữ ĐÚNG 1 action: action thứ hai đến trước khi cái đầu
    // được tiêu thụ sẽ GHI ĐÈ cái cũ ⇒ mất tín hiệu, bước sau chờ mãi không tới.
    // Nay dùng hàng đợi thật, giữ tối đa 8 action theo thứ tự đến.
    private readonly Queue<TutorialWaitAction> _hangDoiAction = new Queue<TutorialWaitAction>();
    private const int SUC_CHUA_HANG_DOI = 8;
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
    /// <summary>
    /// [V2] Bỏ tick _useV2Dialogue thì phải tắt LUÔN đạo diễn camera, nếu không
    /// TutorialCameraFocus vẫn tự dò thấy nó và uỷ quyền ⇒ "về bản cũ" không trọn vẹn.
    /// </summary>
    private void DongBoCongTacV2()
    {
        if (_v2Camera != null && _v2Camera.enabled != _useV2Dialogue)
            _v2Camera.enabled = _useV2Dialogue;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (_v2Card == null)
            _v2Card = FindFirstObjectByType<TutorialDialogueCard>(FindObjectsInactive.Include);
        if (_v2Vfx == null)
            _v2Vfx = FindFirstObjectByType<TutorialVfxDirector>(FindObjectsInactive.Include);
        if (_v2Camera == null)
            _v2Camera = FindFirstObjectByType<TutorialCameraDirector>(FindObjectsInactive.Include);

        if (_v2Card != null && _v2Card.gameObject != null)
        {
            _v2Card.gameObject.SetActive(true);
        }

        if (FindFirstObjectByType<TutorialPhantomDemoManager>(FindObjectsInactive.Include) == null)
        {
            gameObject.AddComponent<TutorialPhantomDemoManager>();
        }

        // [V6] Hộp thoại NPC cũ đã bị khai tử — chỉ dùng card V2.
        if (_npcDialogPopup != null)
        {
            _npcDialogPopup.SetActive(false);
        }
        var oldPopup = GameObject.Find("NPC_Dialog_Popup");
        if (oldPopup != null)
        {
            oldPopup.SetActive(false);
        }

        // [V6] Vào scene là bàn tay chưa có chủ — static không tự reset khi tắt domain reload.
        TutorialHandBus.NhaTatCa();
    }

    void Start()
    {
        DongBoCongTacV2();   // [V2] đồng bộ công tắc trước khi tutorial chạy
        StartCoroutine(WatchdogChongKet());   // [VÒNG 14] lưới an toàn chống kẹt
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
            Debug.LogWarning("[TutorialManager] Không có step nào. Hãy gán TutorialStepData vào _steps.");
            return;
        }

        // ── B1/B2 · ĐÃ XONG RỒI THÌ KHÔNG DẮT LẠI ──
        // Đóng dấu phiên bản họ save "TUTORIAL" TRƯỚC khi đọc cờ. `hasExistingSave` chỉ
        // true khi máy này thật sự đã có cờ hướng dẫn nào — người chơi mới thì bỏ qua nhánh
        // migrate cho khỏi ghi log báo động giả.
        bool coCoTutorialCu = PlayerPrefs.HasKey(PrefKeyDone)
                              || PlayerPrefs.HasKey("TUTORIAL_PREPLANT_DONE")
                              || PlayerPrefs.HasKey("STARTER_ITEMS_GIVEN");
        SaveVersionGuard.Ensure(TutorialSaveFamily, TutorialSaveVersion,
                                hasExistingSave: coCoTutorialCu);

        if (_devClearDoneFlagOnStart)
        {
            ClearTutorialDoneFlag();
            Debug.Log("[Tutorial] DEV: đã xoá cờ TUTORIAL_MAIN_DONE → chạy lại như người mới.");
        }

        if (IsTutorialDone && !_devForceReplayTutorial)
        {
            // KHÔNG chỉ `return`: phải DỌN sạch UI hướng dẫn. Bỏ qua bước dọn thì
            // Tutorial_Canvas còn tàng hình mà `blocksRaycasts` vẫn bật ⇒ nuốt hết click
            // của người chơi, cả bản đồ thành không bấm được.
            Debug.Log("[Tutorial] Đã xong từ phiên trước → bỏ qua, không dắt lại.");
            SkipTutorialEntirely();
            return;
        }

        if (_devForceReplayTutorial && IsTutorialDone)
            Debug.LogWarning("[Tutorial] DEV: _devForceReplayTutorial đang bật → chạy lại dù đã xong. " +
                             "Nhớ bỏ tick trước khi build.");

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

        // [WP-A1] RESUME: phiên trước thoát giữa tutorial ⇒ tiếp đúng bước đã lưu.
        // Đặt _currentIndex = saved-1 để AdvanceToNextStep() ++ lên đúng saved.
        int buocDaLuu = PlayerPrefs.GetInt(PrefKeyStep, 0);
        if (!IsTutorialDone && !_devForceReplayTutorial && buocDaLuu > 0 && buocDaLuu < _steps.Count)
        {
            // [RESUME] Bước "kéo hạt đầu tiên" cần KHAY HẠT đang mở (target seed_* chỉ đăng ký khi
            // khay mở). Mở lại app thì khay đã đóng ⇒ tay không có gì để chỉ. Lùi 1 bước về
            // "chạm ô đất để mở khay" (04 / 12) cho người chơi tự mở khay lại.
            string tenBuocLuu = LayTenBuoc(buocDaLuu);
            if ((tenBuocLuu == "L1L2_05_DragFirstRice" || tenBuocLuu == "L1L2_13_DragFirstFlower") && buocDaLuu - 1 > 0)
            {
                Debug.Log($"[Tutorial] Resume: bước đã lưu '{tenBuocLuu}' cần khay hạt đang mở → lùi về bước " +
                          $"{buocDaLuu - 1} '{LayTenBuoc(buocDaLuu - 1)}'.");
                buocDaLuu -= 1;
            }
            _currentIndex = buocDaLuu - 1;
            Debug.Log($"[Tutorial] Resume bước {buocDaLuu} '{LayTenBuoc(buocDaLuu)}' (lưu từ phiên trước).");
        }

        // KHÔNG focus camera ở màn chào mừng — camera chỉ lia vào 6 ô đất
        // khi tới bước L1L2_04_FocusPlots (sau khi player bấm "ĐÃ RÕ").

        Debug.Log($"[Tutorial] StartTutorial — total steps: {_steps.Count}");
        AdvanceToNextStep();
    }

    /// <summary>
    /// Chuyá»ƒn bÆ°á»›c tiáº¿p theo.
    /// â€¢ Äang typewriter â†’ skip text.
    /// â€¢ Äang WaitForClick â†’ advance.
    /// Gá»i tá»« Button "Tiếp Theo" trÃªn NPC_Dialog_Popup (auto-wired bá»Ÿi Generator).
    /// </summary>
    public void NextStep()
    {
        if (TryDismissInteractionDialog()) return;

        if (_state == TutorialState.TypingText && _clickToSkipTyping)
        {
            SkipTyping();
            return;
        }
        if (_state == TutorialState.WaitingAction)
        {
            if (_pendingWait == TutorialWaitAction.WaitForClick)
            {
                AdvanceToNextStep();
            }
            else
            {
                // [Chạm là ẩn] Khi đang ở bước chờ thao tác, chạm vào card thoại sẽ ẩn card đi ngay để thao tác
                AnHopThoai();
            }
        }
    }

    public void ConfirmGuidePopup()
    {
        if (_state != TutorialState.WaitingAction
            || _pendingWait != TutorialWaitAction.WaitForClick
            || _currentIndex < 0
            || _currentIndex >= _steps.Count
            || _steps[_currentIndex] == null
            || !CoBangHuongDan(_steps[_currentIndex]))
            return;

        _guideBoardUI?.Hide();
        AdvanceToNextStep();
    }

    // =========================================================================
    // [BOARD 4 TRANG] Bảng hướng dẫn 4 trang (gieo hạt / kim cương / liềm / kết quả)
    // =========================================================================
    /// <summary>Vô hiệu hoá hoàn toàn bảng popup cứng màu be theo yêu cầu Sếp (dùng Live Phantom Demo thay thế).</summary>
    private static bool LaBuocBangHuongDan(string stepName)
    {
        return false;
    }

    /// <summary>Bước này có hiện bảng hướng dẫn không -> luôn false để không bật popup cứng.</summary>
    private static bool CoBangHuongDan(TutorialStepData step)
    {
        return false;
    }

    /// <summary>Bước hiện tại có bảng VÀ scene có gắn TutorialGuideBoardUI (bảng thật sự đang là lối đi).</summary>
    private bool BuocDangHienBang()
    {
        return false;
    }

    /// <summary>Game systems gá»i Ä‘á»ƒ bÃ¡o player hoÃ n thÃ nh hÃ nh Ä‘á»™ng.</summary>
    public void NotifyAction(TutorialWaitAction action)
    {
        if (_state == TutorialState.WaitingAction && _pendingWait == action)
        {
            if (TryConsumePenOpenSubAction(action))
                return;

            // [VÒNG 17 — CỔNG POPUP] Có popup hệ thống đang mở (lên cấp, kho, shop…)
            // thì KHÔNG được nhảy bước ngay: nhảy lúc này là card thoại tutorial bật lên
            // đè thẳng vào mặt popup. Cất action vào hàng đợi, cổng ở đầu PlayStep sẽ
            // tiêu thụ khi popup đóng.
            // [FIX KẸT] Ngoại lệ: mini-panel CropProcess CHÍNH LÀ thứ bước này dạy mở/bấm
            // (NotifyOpenCropProcess bắn SAU khi panel đã đăng ký AnyOpen, NotifySpeedUp bắn
            // TRƯỚC ClosePopup). Coi nó là popup để hoãn thì bước 07/08 không bao giờ qua.
            if (TutorialGate.CoPopupDangMo() && !LaPopupCuaChinhBuoc(action))
            {
                DayVaoHangDoi(action);
                Debug.Log($"[Tutorial] Hoãn '{action}' — popup '{TutorialGate.TenPopupDangMo()}' đang mở.");
                // [FIX KẸT] Trước đây KHÔNG AI tiêu thụ hàng đợi sau khi popup đóng (chỉ có
                // PlayStep gọi ConsumeQueuedAction, mà ta chưa sang bước) ⇒ thu hoạch xong lên
                // cấp là tay đứng im mãi. Nay canh popup đóng rồi tự tiêu thụ.
                BatChoPopupDongRoiTieuThu();
                return;
            }

            AdvanceToNextStep();
            return;
        }

        if (_state == TutorialState.TypingText || _state == TutorialState.Transitioning)
        {
            if (_currentIndex >= 0 && _currentIndex < _steps.Count
                && _steps[_currentIndex] != null
                && _steps[_currentIndex].waitAction == action)
            {
                DayVaoHangDoi(action);
                // Vô hại: khi bước vào WaitingAction thì ConsumeQueuedAction ở cuối PlayStep
                // đã lấy; coroutine này chỉ là lưới đỡ thêm nếu popup mở đúng lúc chuyển bước.
                if (TutorialGate.CoPopupDangMo()) BatChoPopupDongRoiTieuThu();
                return;
            }
        }

        // [WP-A1] Action "quét hết ô" tới SỚM: đang ở bước lẻ (WaitForPlant / WaitForHarvest…)
        // mà người chơi đã trồng/thu hoạch hết ⇒ gate bắn ngay, nhưng bước KẾ TIẾP mới chờ nó.
        // Trước đây tín hiệu này bị bỏ rơi (latch đã set, không bắn lại) ⇒ tay kẹt ở bước sau.
        // Nay: bước kế tiếp cần đúng action này ⇒ cất vào hàng đợi, ConsumeQueuedAction ở đầu
        // bước sau sẽ tiêu thụ (và ThuQuaGateNgay cũng kiểm tra lại theo trạng thái thật).
        if (LaBuocChoQuetO(action) && BuocKeTiepCho(action))
        {
            DayVaoHangDoi(action);
            Debug.Log($"[Tutorial][Gate] Xếp hàng '{action}' tới sớm (đang chờ '{_pendingWait}' ở bước " +
                      $"'{CurrentStepName}', bước kế tiếp mới cần nó).");
        }
    }

    // =========================================================================
    // [FIX KẸT] Canh popup đóng → tiêu thụ action đã hoãn
    // =========================================================================
    private Coroutine _choPopupDongCo;

    /// <summary>
    /// Action này có phải "popup của chính bước" không — mini-panel CropProcess đang mở
    /// đúng là điều bước 07 (mở panel) / 08 (bấm kim cương trong panel) chờ. Chỉ ngoại lệ khi
    /// KHÔNG có popup lên cấp chồng lên (LevelUp mới là thứ cần nhường sân khấu).
    /// </summary>
    private static bool LaPopupCuaChinhBuoc(TutorialWaitAction action)
    {
        // Popup LEN CAP luon la "khach la" — co no thi hoan het, khong ngoai le.
        if (LevelUpPopupUI.IsActive) return false;

        // (a) Mini-panel cay/hoa — bai hoc cua chinh buoc 07/08 va 15.
        if (action == TutorialWaitAction.WaitForOpenCropProcess
            || action == TutorialWaitAction.WaitForSpeedUp)
            return CropProcessPopupUI.AnyOpen;

        // (b) [FIX 2026-09-06] CUA HANG — day chinh la bai hoc cua buoc L2_01..L2_04.
        // Truoc day: nguoi choi mo shop ⇒ PopupManager bao "co popup he thong dang mo" ⇒
        // NotifyOpenShop bi hoan vao hang doi cho toi khi ĐONG shop ⇒ tutorial dung im o
        // L2_01, tay van chi vao nut CUA HANG du shop da mo tren man hinh. Mua xong cung bi
        // hoan not, roi NotifyCloseShop ban khi _pendingWait van con la WaitForBuyItem ⇒ mat luon.
        // Nay: shop dang mo thi 3 action cua chinh chuoi shop KHONG bi hoan.
        if (action == TutorialWaitAction.WaitForOpenShop
            || action == TutorialWaitAction.WaitForBuyItem
            || action == TutorialWaitAction.WaitForCloseShop)
            return true;

        // (c) [FIX 2026-09-06] CHUONG GIA SUC — cung mot lop loi, se dinh y het o buoc L2_07..L2_10.
        if (action == TutorialWaitAction.WaitForOpenPen
            || action == TutorialWaitAction.WaitForFeed
            || action == TutorialWaitAction.WaitForPenSpeedUp
            || action == TutorialWaitAction.WaitForPenHarvest)
            return true;

        return false;
    }

    /// <summary>Bật coroutine canh popup (chỉ 1 bản chạy tại một thời điểm).</summary>
    private void BatChoPopupDongRoiTieuThu()
    {
        if (_choPopupDongCo != null) return;
        _choPopupDongCo = StartCoroutine(ChoPopupDongRoiTieuThu());
    }

    /// <summary>
    /// Poll 0.1s (unscaled) tới khi hết popup, thở 0.25s cho anim đóng, rồi tiêu thụ hàng đợi
    /// nếu vẫn đang WaitingAction. Hết hạn 60s vẫn cố tiêu thụ để không kẹt vĩnh viễn.
    /// </summary>
    private IEnumerator ChoPopupDongRoiTieuThu()
    {
        const float HET_HAN_GIAY = 60f;
        float moc = Time.unscaledTime;
        bool hetHan = false;
        string tenPopup = TutorialGate.TenPopupDangMo();

        while (TutorialGate.CoPopupDangMo())
        {
            if (Time.unscaledTime - moc >= HET_HAN_GIAY) { hetHan = true; break; }
            string tenHienTai = TutorialGate.TenPopupDangMo();   // nhớ popup CUỐI CÙNG còn mở
            if (!string.IsNullOrEmpty(tenHienTai)) tenPopup = tenHienTai;
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (hetHan)
            Debug.LogWarning($"[Tutorial][Gate] Popup '{tenPopup}' mở quá {HET_HAN_GIAY:0}s — " +
                             "vẫn thử tiêu thụ hàng đợi để tutorial không kẹt.");
        else
            yield return new WaitForSecondsRealtime(0.25f);

        // Ghi lại popup vừa đóng — WaitForLevelUpClaim (bước 11) nhìn vào đây để KHÔNG chờ 12s
        // một popup lên cấp mà người chơi đã bấm "Nhận" xong rồi.
        _tenPopupVuaDong = tenPopup;
        _lucPopupVuaDong = Time.unscaledTime;

        _choPopupDongCo = null;
        Debug.Log("[Tutorial][Gate] Popup đóng → tiêu thụ hàng đợi");
        if (_state == TutorialState.WaitingAction) ConsumeQueuedAction();
    }

    /// <summary>Popup cuối cùng đóng mà coroutine trên đã canh, và lúc nào (unscaled). Rỗng = chưa có.</summary>
    private string _tenPopupVuaDong;
    private float  _lucPopupVuaDong = -999f;

    /// <summary>Popup lên cấp VỪA MỚI đóng (trong vòng vài giây) — tức người chơi đã bấm "Nhận" rồi.</summary>
    private bool LevelUpVuaDongXong() =>
        _tenPopupVuaDong == "LevelUp" && Time.unscaledTime - _lucPopupVuaDong < 3f;

    /// <summary>[VÒNG 17] Cất một action vào hàng đợi, bỏ qua nếu đã có cùng loại trong hàng.</summary>
    private void DayVaoHangDoi(TutorialWaitAction action)
    {
        if (_hangDoiAction.Contains(action)) return;
        if (_hangDoiAction.Count >= SUC_CHUA_HANG_DOI)
        {
            Debug.LogWarning($"[Tutorial] Hàng đợi action đầy ({SUC_CHUA_HANG_DOI}) — bỏ '{action}'.");
            return;
        }
        _hangDoiAction.Enqueue(action);
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
        // 🔴 [VÒNG 14] TRƯỚC ĐÂY ĐÒI `quantity >= 8` TRONG MỘT GIAO DỊCH — ĐÂY LÀ LỖI KẸT CỨNG.
        // UI shop mặc định số lượng = 1, người chơi bấm mua từng hạt ⇒ điều kiện KHÔNG BAO GIỜ
        // đúng ⇒ bước L2_03_BuyCorn treo vĩnh viễn, không có timeout, phải gỡ app.
        // Nay: chỉ cần ĐÚNG LOẠI HẠT là qua bước. Số lượng do bước sau (trồng đủ ô) tự lo.
        if (CurrentStepName == "L2_03_BuyCorn" && !IsCornSeed(itemId, cropId))
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

    // ═══════════════════════════════════════════════════════════════════════
    // [VÒNG 14] WATCHDOG CHỐNG KẸT — lưới an toàn cho TOÀN BỘ 31 bước
    // ═══════════════════════════════════════════════════════════════════════
    //
    // VÌ SAO CẦN: QA vòng 14 rà 23 điều kiện chờ và tìm ra ít nhất 4 bước có thể treo
    // VĨNH VIỄN, không có đường thoát nào (phải gỡ app):
    //   • L2_03_BuyCorn      — đòi mua ≥8 hạt trong MỘT giao dịch, shop mặc định 1 (đã vá)
    //   • L1L2_08_SpeedUpTip — hết kim cương thì NotifySpeedUp không bao giờ chạy
    //   • L1L2_15_FlowerSpeedUp — như trên, lại còn không có bước dạy mở popup cho hoa
    //   • L1L2_07_OpenCropProgress — lúa chín trước khi kịp chạm thì mở khay liềm, không
    //     phải CropProcess, nên Notify không bao giờ bắn
    //
    // Vá từng chỗ chỉ chữa được cái ĐÃ BIẾT. Watchdog chữa cả những chỗ CHƯA AI PHÁT HIỆN:
    // ngồi một bước quá lâu ⇒ hiện nút "Bỏ qua bước này".
    //
    // CỐ Ý KHÔNG TỰ NHẢY BƯỚC: người chơi có thể đang đọc kỹ, hoặc đi pha trà. Tự nhảy sẽ
    // cướp mất bước học. Đưa cho họ CÁI NÚT, để họ quyết định.
    private const float GIAY_NGHI_KET = 45f;

    /// <summary>[FIX 2026-09-06] So giay man hinh dang bi lop toi phu kin ma khong co loi thoat.</summary>
    private float _demManToi;

    private System.Collections.IEnumerator WatchdogChongKet()
    {
        int buocDangTheoDoi = -1;
        float dungYenBaoLau = 0f;
        bool  daHienNutThoat = false;

        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);   // realtime: tutorial có lúc timeScale = 0

            bool dangCho = _state == TutorialState.WaitingAction;

            if (!dangCho || _currentIndex != buocDangTheoDoi)
            {
                // Đổi bước (hoặc không còn chờ) ⇒ đặt lại đồng hồ.
                buocDangTheoDoi = _currentIndex;
                dungYenBaoLau   = 0f;

                if (daHienNutThoat)
                {
                    daHienNutThoat = false;
                    if (_v2Card != null) _v2Card.TraLaiNhanTiepTuc();
                }
                continue;
            }

            // ── [FIX 2026-09-06] LUOI AN TOAN: man hinh toi den ma khong co gi de bam ──
            // Lop toi bat + KHONG khoet lo = chan sach click toan man hinh. Neu dong thoi
            // khong co ban tay nao va khong co card thoai thi nguoi choi hoan toan be tac
            // (dung canh Sep gap o phan hoa: man toi thui, khong tay, khong chu).
            // Giu trang thai do qua 2.5s la chac chan hong ⇒ tu tat lop toi + go mask vung.
            {
                bool toiMaKhongLo = _dimBackground != null
                                    && _dimBackground.gameObject.activeInHierarchy
                                    && !_dimBackground.CoLoKhoet;
                bool coTay  = CoTayTinhDangBat || CoTayKeoDangBat || CoTayHanhDongDangBat;
                bool coChu  = DungCardV2 && _v2Card.DangMo;

                if (toiMaKhongLo && !coTay && !coChu)
                {
                    _demManToi += 1f;
                    if (_demManToi >= 2.5f)
                    {
                        Debug.LogWarning($"[Tutorial] ⚠ Bước '{CurrentStepName}': màn hình bị lớp tối "
                            + "phủ kín mà không có lỗ, không tay, không lời thoại — người chơi không "
                            + "bấm được gì. Tự gỡ lớp tối để tutorial đi tiếp được.");
                        _dimBackground.ClearHole();
                        _dimBackground.gameObject.SetActive(false);
                        _runtimeTargetResolver?.EnableAreaMask(TutorialAreaKind.None, null);
                        _demManToi = 0f;
                    }
                }
                else _demManToi = 0f;
            }

            // ── [VÒNG 15] LỐI THOÁT MỀM: chờ cây chín tự nhiên cũng qua bước ──
            // Sếp yêu cầu: "nếu user đợi chín và không cần bấm, chỉ cần lúa chín là qua step
            // luôn, tuỳ ý user". Trước đây bước WaitForSpeedUp CHỈ nhả khi bấm nút kim cương
            // (CropProcessPopupUI.OnGemClick), nên ai kiên nhẫn đợi cây chín sẽ đứng mãi ở đó.
            // Nay: đang chờ tăng tốc mà có ô nào chín rồi ⇒ coi như đạt, đi tiếp.
            // [FIX 2026-09-06] Mo rong cho ca buoc "cham o de xem bang tien trinh" (L1L2_07).
            // Nguoi choi tiet kiem kim cuong, doi cay chin tu nhien: luc do cham vao o la THU
            // HOACH chu khong mo bang tien trinh nua (CropProcessPopupUI doi IsGrowing) ⇒ buoc 07
            // treo vinh vien. Cay da chin thi bai hoc "tang toc" khong con y nghia — cho qua.
            if (_pendingWait == TutorialWaitAction.WaitForOpenCropProcess && CoOChinRoi())
            {
                Debug.Log($"[Tutorial] ✅ Bước '{CurrentStepName}' — cây đã chín tự nhiên nên không "
                          + "mở được bảng tiến trình nữa. Cho qua bước.");
                NotifyAction(TutorialWaitAction.WaitForOpenCropProcess);
                continue;
            }

            if (_pendingWait == TutorialWaitAction.WaitForSpeedUp && CoOChinRoi())
            {
                Debug.Log($"[Tutorial] ✅ Bước '{CurrentStepName}' — cây đã chín tự nhiên, " +
                          "người chơi không cần bấm kim cương. Cho qua bước.");
                NotifyAction(TutorialWaitAction.WaitForSpeedUp);
                continue;
            }

            // ── [VÒNG 15] CỨU SỚM: bước WaitForClick mà nút Tiếp tục không hiện ──
            // Bước chỉ cần "đọc rồi bấm" thì BẮT BUỘC phải có nút. Không hiện là hỏng ở đâu đó
            // (ref rơi, object bị tắt, GoXong không chạy). Chờ 45s ở một bước như thế là vô nghĩa
            // — ép hiện nút sau 3 giây, người chơi đi tiếp được ngay.
            // [BOARD 4 TRANG] Bước đang hiện BẢNG hướng dẫn thì nút "ĐÃ RÕ" của bảng mới là lối đi —
            // không ép nút Tiếp tục của card (card đang ẩn, ép cũng không ai thấy, chỉ rác log).
            if (dungYenBaoLau >= 3f && DungCardV2
                && _pendingWait == TutorialWaitAction.WaitForClick
                && !_v2Card.NutTiepTucDangHien
                && !BuocDangHienBang())
            {
                Debug.LogWarning($"[Tutorial] ⚠ Bước '{CurrentStepName}' là WaitForClick nhưng nút " +
                                 "Tiếp tục KHÔNG hiện — ép hiện để không kẹt.");
                _v2Card.EpHienNutTiepTuc(NextStep);
            }

            dungYenBaoLau += 1f;

            if (dungYenBaoLau < GIAY_NGHI_KET || daHienNutThoat) continue;

            daHienNutThoat = true;

            Debug.LogWarning($"[Tutorial] ⏳ NGHI KẸT: đứng ở bước [{_currentIndex}] " +
                             $"'{CurrentStepName}' chờ '{_pendingWait}' quá {GIAY_NGHI_KET:0}s. " +
                             "Đã hiện nút 'Bỏ qua bước này' cho người chơi. " +
                             "Nếu lỗi lặp lại, kiểm xem ai gọi Notify tương ứng với điều kiện chờ này.");

            if (DungCardV2)
            {
                _v2Card.HienNutBoQua("Bỏ qua bước này", () =>
                {
                    Debug.LogWarning($"[Tutorial] Người chơi BỎ QUA bước '{CurrentStepName}'.");
                    _v2Card.TraLaiNhanTiepTuc();
                    AdvanceToNextStep();
                });
                DamBaoNutBoQuaNhinThay();   // [WP-A1] card đang ẩn (AnHopThoai) thì nút cũng ẩn → bật lại
            }
        }
    }

    /// <summary>
    /// [VÒNG 15] Có ô đất nào đã CHÍN (Ready) chưa. Dùng cho lối thoát mềm của bước tăng tốc:
    /// người chơi kiên nhẫn đợi cây lớn thì cũng phải được đi tiếp, không bắt buộc tiêu kim cương.
    /// Quét cả object đang tắt để không bỏ sót ô bị ẩn tạm.
    /// </summary>
    private bool CoOChinRoi()
    {
        var oDat = FindObjectsByType<PlotController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < oDat.Length; i++)
            if (oDat[i] != null && oDat[i].IsReady) return true;

        return false;
    }

    private void AdvanceToNextStep()
    {
        TutorialPhantomDemoManager.Instance?.StopDemo();   // [PHANTOM] sang bước là tắt ảo ảnh + trả tay thật
        // [V6] Sang bước mới: nhả quyền + ẩn CẢ BA tay thật, để không sót tay của bước cũ.
        // Bước mới tự dựng lại đúng bàn tay nó cần (UpdateHandPointer / StartDragHint / GuideX).
        DonSachMoiBanTay();
        _runtimeTargetResolver?.EnableAreaMask(TutorialAreaKind.None, null); // tắt nền xám (nếu đang bật)
        _interactionDialogDismissed = false;
        _penOpenSubActionReceived = false;
        _currentIndex++;

        if (_currentIndex >= _steps.Count)
        {
            FinishTutorial();
            return;
        }

        // [WP-A1] Lưu bước hiện tại để lần mở sau resume đúng chỗ (gộp lưu qua LuuGopPrefs).
        PlayerPrefs.SetInt(PrefKeyStep, _currentIndex);
        LuuGopPrefs.Hen();

        var step = _steps[_currentIndex];
        Debug.Log($"[Tutorial] Step [{_currentIndex}/{_steps.Count - 1}] {step.name} — waitAction={step.waitAction} showGuideBoard={step.showGuideBoard}");

        // Focus camera vào các ô lúa ngay từ đầu Tutorial (L1L2_01 đến L1L2_10)
        if (_cameraFocus != null)
        {
            var bridge = GetComponent<TutorialStepTriggerBridge>();
            if (step.name.StartsWith("L1L2_01") || step.name.StartsWith("L1L2_02") 
                || step.name.StartsWith("L1L2_03") || step.name.StartsWith("L1L2_04")
                || step.name.StartsWith("L1L2_05") || step.name.StartsWith("L1L2_06")
                || step.name.StartsWith("L1L2_07") || step.name.StartsWith("L1L2_08")
                || step.name.StartsWith("L1L2_09") || step.name.StartsWith("L1L2_10"))
            {
                _cameraFocus.FocusOnRice(bridge);
            }
        }

        // L1L2_11_TransitionFlower: KHÔNG focus hoa ở đây nữa — phải chờ user bấm "Nhận"
        // ở popup lên cấp 2 trước (xử lý trong PlayStep → WaitForLevelUpClaim).

        _state = TutorialState.Transitioning;
        StartCoroutine(PlayStep(step));
    }

    private IEnumerator PlayStep(TutorialStepData step)
    {
        // ═══ [VÒNG 17] CỔNG POPUP — chạy TRƯỚC MỌI BƯỚC, không phân biệt bước nào ═══
        // Trước đây chỉ đúng bước 'L1L2_11_TransitionFlower' mới chờ popup lên cấp. Lên cấp
        // 3/4/5 hay bất kỳ popup nào khác bật giữa chừng đều bị tutorial vẽ đè lên.
        // Nay mọi bước đều đi qua cổng này: có popup thì ẩn UI tutorial, chờ đóng, rồi hiện lại.
        yield return TutorialGate.ChoPopupDongHet(AnToanBoUiTutorial, HienLaiUiTutorial);

        // Popup vừa đóng có thể đã kèm theo một action bị hoãn — tiêu thụ ngay khi vào bước.
        // (ConsumeQueuedAction ở cuối mỗi nhánh sẽ lo phần khớp _pendingWait.)

        // ─── Nhường "sân khấu" cho popup LÊN CẤP ───
        // Thu hoạch xong các ô lúa = đúng 40 EXP → lên cấp 2. Bước chuyển sang trồng hoa
        // phải ĐỢI popup lên cấp hiện ra + user bấm "Nhận" rồi mới bắt đầu (focus hoa + tay quét).
        if (step.name == "L1L2_11_TransitionFlower")
        {
            TutorialPhantomDemoManager.Instance?.StopDemo();
            _actionHandGuide?.StopGuide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);

            yield return WaitForLevelUpClaim();

            if (FarmUIManager.Instance != null)
            {
                FarmUIManager.Instance.HidePlantSelectPopup();
                FarmUIManager.Instance.HideAllPopups();
            }

            if (_cameraFocus != null)
                _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
        }

        if (step.name == "L1L2_12_FocusFlowerPots")
        {
            if (FarmUIManager.Instance != null)
                FarmUIManager.Instance.HidePlantSelectPopup();

            if (_cameraFocus != null)
                _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
        }

        if (_dimBackground != null)
            _dimBackground.gameObject.SetActive(true);

        // ─── GUIDE THÔNG MINH: nền xám bao vùng + tay CHỈ quét ô CÒN VIỆC (theo tiến độ user) ───
        // Ô đất — trồng: L1L2_06_PlantAllRice (chờ kéo đủ hạt vào 6 ô)
        if (step.name == "L1L2_06_PlantAllRice")
        {
            NhacNhanh(step != null && !string.IsNullOrWhiteSpace(step.npcText) ? step.npcText : "Gieo nốt hạt lúa cho kín cả 8 ô ruộng nhé — bàn tay sẽ chỉ ô còn trống cho bạn!");
            SetupSmartGuide(TutorialAreaKind.Rice, harvestMode: false);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            if (ThuQuaGateNgay(step)) { ConsumeQueuedAction(); yield break; }   // [WP-A1] đã đủ từ trước → qua luôn
            BatWatchdogHetHat(step);
            // [PHANTOM] Ảo ảnh kéo hạt vào ô TRỐNG kế tiếp (tay thật tạm ẩn trong lúc demo).
            TutorialPhantomDemoManager.Instance?.PlayPlantPhantom(LayIconHat("seed_rice"), "seed_rice",
                TimIdOConViec(TutorialAreaKind.Rice, false, 0) ?? "tutorial_plot_02");
            ConsumeQueuedAction(); yield break;
        }
        // Ô đất — thu hoạch: tay chỉ vào ô đã chín, chờ thu hoạch hết.
        if (step.name == "L1L2_10_HarvestAllRice")
        {
            NhacNhanh(step != null && !string.IsNullOrWhiteSpace(step.npcText) ? step.npcText : "Lúa chín vàng cả ruộng rồi! Quẹt liềm thu hoạch nốt những ô còn lại nào.");
            SetupSmartGuide(TutorialAreaKind.Rice, harvestMode: true);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            if (ThuQuaGateNgay(step)) { ConsumeQueuedAction(); yield break; }   // [WP-A1] đã đủ từ trước → qua luôn
            // [PHANTOM] Ảo ảnh liềm quét từ ô chín thứ nhất sang ô chín thứ hai.
            TutorialPhantomDemoManager.Instance?.PlayHarvestPhantom(
                TimIdOConViec(TutorialAreaKind.Rice, true, 0) ?? "tutorial_plot_01",
                TimIdOConViec(TutorialAreaKind.Rice, true, 1) ?? "tutorial_plot_02");
            ConsumeQueuedAction(); yield break;
        }
        // Chậu hoa — trồng toàn bộ.
        if (step.name == "L1L2_14_PlantAllFlowers")
        {
            NhacNhanh(step != null && !string.IsNullOrWhiteSpace(step.npcText) ? step.npcText : "Gieo nốt hạt hướng dương vào những chậu còn trống nhé!");
            if (_cameraFocus != null) _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
            SetupSmartGuide(TutorialAreaKind.Flower, harvestMode: false);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            if (ThuQuaGateNgay(step)) { ConsumeQueuedAction(); yield break; }   // [WP-A1] đã đủ từ trước → qua luôn
            BatWatchdogHetHat(step);
            // [PHANTOM] Ảo ảnh kéo hạt hướng dương vào chậu TRỐNG kế tiếp.
            TutorialPhantomDemoManager.Instance?.PlayPlantPhantom(LayIconHat("seed_huong_duong"), "seed_huong_duong",
                TimIdOConViec(TutorialAreaKind.Flower, false, 0) ?? "tutorial_flower_02");
            ConsumeQueuedAction(); yield break;
        }
        // Chậu hoa — thu hoạch.
        if (step.name == "L1L2_17_HarvestAllFlowers")
        {
            NhacNhanh(step != null && !string.IsNullOrWhiteSpace(step.npcText) ? step.npcText : "Hoa nở rực rỡ hết rồi! Thu hoạch nốt những chậu còn lại thôi nào.");
            if (_cameraFocus != null) _cameraFocus.FocusOnFlower(GetComponent<TutorialStepTriggerBridge>());
            SetupSmartGuide(TutorialAreaKind.Flower, harvestMode: true);
            _pendingWait = step.waitAction; _state = TutorialState.WaitingAction;
            if (ThuQuaGateNgay(step)) { ConsumeQueuedAction(); yield break; }   // [WP-A1] đã đủ từ trước → qua luôn
            // [PHANTOM] Ảo ảnh liềm quét chậu chín thứ nhất → thứ hai (null = chỉ 1 chậu, demo bỏ qua ô 2).
            TutorialPhantomDemoManager.Instance?.PlayHarvestPhantom(
                TimIdOConViec(TutorialAreaKind.Flower, true, 0) ?? "tutorial_flower_01",
                TimIdOConViec(TutorialAreaKind.Flower, true, 1));
            ConsumeQueuedAction(); yield break;
        }

        // ═══ TUTORIAL L2 — SHOP & TRỒNG NGÔ (B1–B7) ═══
        // L2_01: tay chỉ Home→Store (tự nhảy khi menu mở), chờ shop mở.
        if (step.name == "L2_01_GotoShop")
        {
            AnHopThoai();
            _guideBoardUI?.Hide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);

            // [FIX 2026-09-06] Camera dang zoom o vuon hoa (buoc 12-18). Khong lui ra thi
            // nguoi choi khong thay nut CUA HANG o goc duoi ⇒ tuong tutorial dung.
            _cameraFocus?.RestoreCamera();
            yield return new WaitForSecondsRealtime(0.45f);   // cho camera lui xong roi moi chi tay

            // Bao dam nut CUA HANG da duoc dang ky lam target (resolver quet moi 0,3s,
            // nhung goi thang o day de tay hien NGAY, khong doi 1 nhip).
            _runtimeTargetResolver?.RefreshShopTargets();
            var rectShop = GetTargetRect("hud_shop") ?? GetTargetRect("btn_store");
            if (_dimBackground != null)
            {
                if (rectShop != null)
                {
                    _dimBackground.gameObject.SetActive(true);
                    _dimBackground.SetTarget(rectShop, false, 18f);
                }
                else
                {
                    _dimBackground.ClearHole();
                    _dimBackground.gameObject.SetActive(false);
                    Debug.LogWarning("[Tutorial] L2_01_GotoShop: khong thay nut CUA HANG tren HUD → "
                                     + "bo lop toi de nguoi choi van bam duoc.");
                }
            }
            _actionHandGuide?.GuidePointFirstActive(new[] { "hud_shop", "btn_store", "btn_home" });
            _pendingWait = step.waitAction;   // WaitForOpenShop
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_03: bao xám quanh item Bắp Cải / Ngô + tay chỉ Bắp Cải/＋, chờ mua.
        if (step.name == "L2_03_BuyCorn")
        {
            AnHopThoai();
            _guideBoardUI?.Hide();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
            // TẮT lớp tối TRƯỚC khi chờ. Lớp tối không có lỗ thì chặn 100% click
            // (UnmaskRaycastFilter trả true khi không có target), nên để nó bật suốt
            // 0,4 giây này là khoá cứng shop đúng lúc người chơi vừa mở ra.
            if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);

            // Cuộn shop để item Bắp Cải / Ngô hiện TRỌN (kèm ＋/－/Mua) rồi mới set vùng sáng + tay.
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.ScrollItemIntoView("seed_bapcai");
                ShopManager.Instance.ScrollItemIntoView("seed_ngo");
            }
            yield return new WaitForSecondsRealtime(0.4f);

            _runtimeTargetResolver?.RefreshShopTargets();
            var cornRect = GetTargetRect("shop_bapcai_plus") ?? GetTargetRect("shop_corn_plus")
                        ?? GetTargetRect("shop_bapcai") ?? GetTargetRect("shop_corn");
            if (_dimBackground != null)
            {
                // Không tìm được ô Bắp Cải/Ngô thì KHÔNG bật lớp tối. Bật mà không khoét lỗ
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
                    Debug.LogWarning("[Tutorial] L2_03_BuyCorn: không thấy item Bắp Cải/Ngô trong shop → " +
                                     "bỏ lớp tối để người chơi vẫn bấm mua được.");
                }
            }
            // Tay chỉ nút ＋ tới khi chọn đủ 8 hạt → nhảy sang nút Mua.
            string plusTarget = GetTargetRect("shop_bapcai_plus") != null ? "shop_bapcai_plus" : "shop_corn_plus";
            string buyTarget  = GetTargetRect("shop_bapcai_buy")  != null ? "shop_bapcai_buy"  : "shop_corn_buy";
            string itemTarget = GetTargetRect("shop_bapcai")      != null ? "shop_bapcai"      : "shop_corn";
            _actionHandGuide?.GuideShopBuy(plusTarget, buyTarget, itemTarget, 8, _dimBackground);
            _pendingWait = step.waitAction;   // WaitForBuyItem
            _state = TutorialState.WaitingAction;
            ConsumeQueuedAction(); yield break;
        }
        // L2_04: tay chỉ Btn_Close, chờ đóng shop.
        if (step.name == "L2_04_CloseShop")
        {
            AnHopThoai();
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
            if (ThuQuaGateNgay(step)) { ConsumeQueuedAction(); yield break; }   // [WP-A1] ngô đã trồng đủ → qua luôn
            BatWatchdogHetHat(step);
            ConsumeQueuedAction(); yield break;
        }

        // ═══ TUTORIAL L2 — CHĂN NUÔI (B8–B13) ═══
        // L2_07: zoom chuồng tutorial + tay chỉ giữa chuồng, chờ mở chuồng.
        if (step.name == "L2_07_FocusPen")
        {
            if (_cameraFocus != null) _cameraFocus.FocusOnPen(TenChuongTutorial);
            AnHopThoai();
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
            AnHopThoai();
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
            AnHopThoai();
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
            AnHopThoai();
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
            {
                // VONG 16 — FIX: 'seed_rice'/'seed_huong_duong' chi duoc dang ky
                // KHI khay hat mo ra (TutorialRuntimeTargetResolver quet 0.25s/lan).
                // Truoc day resolve dung 1 lan roi bo qua => vong sang highlight
                // khong bao gio bam vao hat giong. Nay cho dan toi 12 giay.
                Debug.Log($"[Tutorial] Hand pointer target '{step.targetID}' chua dang ky — cho toi da 12s.");
                if (_choTargetCo != null) StopCoroutine(_choTargetCo);
                _choTargetCo = StartCoroutine(ChoTargetXuatHienMuon(step));
            }
        }

        // 2. Dim / highlight
        if (targetRect != null)
        {
            if (_dimBackground != null)
            {
                _dimBackground.gameObject.SetActive(true);
                _dimBackground.SetTarget(targetRect, step.useCircleHole, step.holePaddingPx > 0f ? step.holePaddingPx : 25f);
            }
        }
        else
        {
            _dimBackground?.ClearHole();
        }

        // 3. Hand Pointer
        UpdateHandPointer(step, targetRect);
        if (step.showHandPointer)
            Debug.Log($"[Tutorial] Hand pointer target: {(targetRect != null ? targetRect.name : "NONE")}");

        // Drag hint animation
        if (!string.IsNullOrEmpty(step.dragToTargetId))
            _dragHintAnimator?.StartDragHint(step.targetID, step.dragToTargetId);
        else
            _dragHintAnimator?.StopDragHint();

        // 3b. Phantom Live Demo (Ảo ảnh làm mẫu trực quan như video) — CHỈ ở bước chờ thao tác.
        // Các bước có BẢNG hướng dẫn (03 / 06b / 08b / 09b) KHÔNG chạy ảo ảnh: bảng có tay demo riêng.
        // Ảo ảnh chạy TRƯỚC, tay thật bị ẩn (alpha 0) trong lúc demo rồi hiện lại — xem AnTayThat.
        if (step.name == "L1L2_05_DragFirstRice")
        {
            TutorialPhantomDemoManager.Instance?.PlayPlantPhantom(LayIconHat(HatCanChoBuoc(step.name)), "seed_rice", "tutorial_plot_01");
        }
        else if (step.name == "L1L2_07_OpenCropProgress" || step.name == "L1L2_08_SpeedUpTip")
        {
            // SpeedUpRoutine tự xử lý 2 pha: panel chưa mở → chạm ô rồi chạm kim cương; đã mở → chạm kim cương.
            TutorialPhantomDemoManager.Instance?.PlaySpeedUpPhantom("tutorial_plot_01");
        }
        else if (step.name == "L1L2_09_HarvestFirstRice")
        {
            TutorialPhantomDemoManager.Instance?.PlayHarvestPhantom("tutorial_plot_01", TimIdOConViec(TutorialAreaKind.Rice, true, 1));
        }
        else if (step.name == "L1L2_13_DragFirstFlower")
        {
            TutorialPhantomDemoManager.Instance?.PlayPlantPhantom(LayIconHat(HatCanChoBuoc(step.name)), "seed_huong_duong", "tutorial_flower_01");
        }
        else
        {
            TutorialPhantomDemoManager.Instance?.StopDemo();
        }

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

            // [V6-tay] Bước "chỉ hành động" mà KHÔNG dựng tay quét (startsGuide) và cũng
            // KHÔNG có tay kéo (dragToTargetId rỗng) — ví dụ L1L2_04_FocusPlots — thì
            // HideBlockingTutorialUI() vừa tắt tay tĩnh sẽ để lại 0 BÀN TAY.
            // Bật lại tay tĩnh cho đúng bước đó (chỉ khi bước thật sự cần tay).
            if (!startsGuide && string.IsNullOrEmpty(step.dragToTargetId) && step.showHandPointer)
                UpdateHandPointer(step, TutorialManager.GetTargetRect(step.targetID));

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

        // 4b. Guide Board — [BOARD 4 TRANG] bật lại theo yêu cầu Sếp: 03 / 06b / 08b / 09b.
        // Cờ trên asset HOẶC tên bước (CoBangHuongDan) — không phụ thuộc tool đã chạy lại chưa.
        if (CoBangHuongDan(step) && _guideBoardUI != null)
        {
            Debug.Log($"[Tutorial] Showing guide board cho bước '{step.name}'.");
            TutorialPhantomDemoManager.Instance?.StopDemo();   // bảng có tay demo riêng — không chạy ảo ảnh chồng
            AnHopThoai();
            _guideBoardUI.ShowForStep(step.name);
            _state       = TutorialState.WaitingAction;
            _pendingWait = step.waitAction;
            yield break;
        }
        else
        {
            if (_guideBoardUI != null) _guideBoardUI.Hide();

            if (DungCardV2)
            {
                // Tắt RIÊNG popup cũ — KHÔNG gọi AnHopThoai() ở đây,
                // vì hàm đó ẩn luôn cả card V2 mà ta sắp mở.
                if (_npcDialogPopup != null)
                    _npcDialogPopup.SetActive(false);

                // Đặt state TRƯỚC khi Show: câu thoại rỗng sẽ hiện nút Tiếp tục ngay trong
                // lời gọi Show, lúc đó state phải đúng để NextStep không nhảy bừa một bước.
                _state = TutorialState.TypingText;

                // CHỈ bước WaitForClick mới có nút Tiếp tục. Bước chờ THAO TÁC mà có nút thì
                // người chơi bấm Tiếp tục để đi qua, và không học được thao tác đó.
                System.Action khiBamTiep = (step.waitAction == TutorialWaitAction.WaitForClick)
                    ? (System.Action)NextStep
                    : null;

                Debug.Log($"[Tutorial] ▶ Bước [{_currentIndex}] '{step.name}' · chờ '{step.waitAction}' · " +
                          $"nút Tiếp tục = {(khiBamTiep != null ? "CÓ" : "KHÔNG (chờ thao tác)")}");

                if (_v2Vfx != null) _v2Vfx.OnStepEnter();

                    // [FIX 2026-09-06] Buoc CAN THAO TAC (tang toc / thu hoach): truoc day ban tay
                // CHI hien khi nguoi choi CHAM vao card (TryDismissInteractionDialog). Ai khong
                // cham thi khong bao gio thay tay — dung canh Sep gap o phan hoa. Nay tu bung
                // tay sau 1,2 giay doc chu, khong bat phai cham gi ca.
                if (IsInteractionStep(step.name)) BatTuHienTayThaoTac();

            // Tham số 4 = "chạm bất kỳ đâu trên card" → LUÔN LUÔN là NextStep.
                // Khôi phục hành vi bản cũ (cả tấm NPC_Dialog_Popup là Button nối NextStep).
                // NextStep tự lọc: bước WaitForClick thì advance, bước chờ thao tác thì chỉ
                // TryDismissInteractionDialog() — nên chạm KHÔNG BAO GIỜ làm người chơi bỏ
                // qua thao tác cần học.
                // Thiếu tham số này, bước L1L2_15_FlowerSpeedUp KẸT CỨNG VĨNH VIỄN: không
                // nút, dim không lỗ nuốt hết click, NotifySpeedUp không bao giờ tới.
                // [FIX 2026-09-06] KHONG noi khi nguoi choi con dang thao tac.
                // Truoc day card bung ra ngay giua luc dang keo hat / khay hat con mo,
                // vua de len khay vua che nut "Tiep tuc". Nay: cho tha tay + dong khay + 1 nhip.
                yield return ChoThaoTacXongHan();

                _v2Card.Show(step.npcText, ChonClipNpc(step), khiBamTiep, NextStep);
            }
            // [V6] Hộp thoại NPC cũ đã bị khai tử — chỉ dùng card V2.
            // Trước đây nhánh else ở đây gọi _npcDialogPopup.SetActive(true), làm hộp thoại
            // ông già sống lại mỗi khi thiếu ref card V2. Nay KHÔNG bao giờ bật lại nó nữa;
            // thiếu card V2 thì bước vẫn chạy (chỉ không có khung thoại), không dựng lại đồ cũ.
            else if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
        }

        // 5. Typewriter
        _state = TutorialState.TypingText;

        if (DungCardV2)
        {
            // Card V2 tự gõ chữ bằng maxVisibleCharacters (0 rác GC, khác hẳn cách cũ nối
            // chuỗi từng ký tự ở TypeRoutine) — ở đây chỉ cần chờ nó gõ xong.
            yield return new WaitUntil(() => _v2Card == null || !_v2Card.DangGoChu);
            _typingDone = true;
        }
        else
        {
            yield return StartTyping(step.npcText, step.typingSpeed);
        }

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
            if (step.waitAction != TutorialWaitAction.WaitForClick)
            {
                // Bước chờ thao tác (kéo hạt, thu hoạch, cho ăn...): sau khi đọc xong tự động ẩn card sau 1.8s để lộ toàn bộ khay hạt
                StartCoroutine(AutoAnHopThoaiSauKhiHuongDan(1.8f));
            }
            ConsumeQueuedAction();
        }
    }

    private IEnumerator AutoAnHopThoaiSauKhiHuongDan(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_state == TutorialState.WaitingAction && _pendingWait != TutorialWaitAction.WaitForClick)
        {
            AnHopThoai();
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

    /// <summary>
    /// [V2] Ẩn hộp thoại — cả popup cũ LẪN card V2. Thay cho 11 chỗ gọi rời rạc trước đây,
    /// để không bao giờ xảy ra cảnh popup cũ tắt mà card V2 vẫn đứng che màn hình.
    /// </summary>
    private void AnHopThoai()
    {
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);
        if (_v2Card != null) _v2Card.Hide();
        if (_v2Vfx  != null) _v2Vfx.ClearHighlight();
    }

    /// <summary>
    /// [V2] Chọn clip NPC theo bản chất của bước:
    ///   • Bước đầu / ăn mừng      → Wave  (vẫy tay chào)
    ///   • Bước chỉ đọc rồi bấm    → Talk  (đang giảng giải)
    ///   • Bước chờ người chơi làm → Point (chỉ tay, giữ tư thế suốt lúc chờ)
    /// </summary>
    private TutorialNpcClip ChonClipNpc(TutorialStepData step)
    {
        if (step == null) return TutorialNpcClip.Talk;

        string ten = step.name ?? string.Empty;
        if (_currentIndex <= 0 || ten.Contains("Welcome") || ten.Contains("Celebration"))
            return TutorialNpcClip.Wave;

        return step.waitAction == TutorialWaitAction.WaitForClick
            ? TutorialNpcClip.Talk
            : TutorialNpcClip.Point;
    }

    private IEnumerator TypeRoutine(string fullText, float speed)
    {
        // [V6] Hộp thoại NPC cũ đã bị khai tử — chỉ dùng card V2.
        // Object bị xoá khỏi scene ⇒ _npcDialogText null. Không được ném lỗi ở đây,
        // nếu không bước đứng im vĩnh viễn (_typingDone không bao giờ true).
        if (_npcDialogText == null)
        {
            _typingDone = true;
            yield break;
        }

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

        // [V2] Card mới giữ chữ của riêng nó — phải bảo nó hiện hết, nếu không state
        // nhảy sang WaitingAction trong khi chữ vẫn đang gõ dở.
        if (DungCardV2) _v2Card.SkipTyping();

        // Null-check thêm: ở chế độ V2 scene có thể không còn gán _npcDialogText.
        if (_npcDialogText != null && _currentIndex >= 0 && _currentIndex < _steps.Count
            && _steps[_currentIndex] != null)
            _npcDialogText.text = _steps[_currentIndex].npcText;
        _typingDone          = true;
        _state               = TutorialState.WaitingAction;
        _pendingWait         = _steps[_currentIndex].waitAction;

        if (_pendingWait == TutorialWaitAction.Auto) AdvanceToNextStep();
    }

    // =========================================================================
    // Hand Pointer
    // =========================================================================
    // =========================================================================
    // VONG 16 — Cho target dang ky muon (khay hat giong mo sau khi buoc bat dau)
    // =========================================================================
    private Coroutine _choTargetCo;

    private IEnumerator ChoTargetXuatHienMuon(TutorialStepData step)
    {
        const float HAN_CHO = 12f;
        float hetHan = Time.unscaledTime + HAN_CHO;

        while (Time.unscaledTime < hetHan)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            // Da sang buoc khac -> bo cuoc, tranh ghi de len buoc moi.
            if (_currentIndex < 0 || _currentIndex >= _steps.Count || _steps[_currentIndex] != step)
                yield break;

            if (!_targetRegistry.TryGetValue(step.targetID, out var tt) || tt == null) continue;
            var rt = tt.RectTransform;
            if (rt == null) continue;

            // Chi ve lai lo dim NEU dim dang bat san — khong tu y bat lai lop dim
            // o nhung buoc da co chu y tat no (IsActionOnlyStep / HideBlockingTutorialUI).
            if (_dimBackground != null && _dimBackground.gameObject.activeInHierarchy)
                _dimBackground.SetTarget(rt, step.useCircleHole, step.holePaddingPx);

            UpdateHandPointer(step, rt);
            Debug.Log($"[Tutorial] Target '{step.targetID}' da dang ky muon — gan lai vao '{rt.name}'.");
            _choTargetCo = null;
            yield break;
        }

        Debug.Log($"[Tutorial] Target '{step.targetID}' khong xuat hien sau {HAN_CHO}s — bo qua highlight.");
        _choTargetCo = null;
    }

    private RectTransform _currentTargetRect;
    private Vector2       _currentHandOffset;

    private void LateUpdate()
    {
        if (_handPointer != null && _handPointer.gameObject.activeInHierarchy && _currentTargetRect != null)
        {
            PlaceHandPointerAtTarget(_currentTargetRect, _currentHandOffset);
        }
    }

    private void UpdateHandPointer(TutorialStepData step, RectTransform targetRect)
    {
        if (_handPointer == null || step == null) return;

        bool show = step.showHandPointer && targetRect != null;
        if (!show)
        {
            _currentTargetRect = null;
            if (!TayTinhDungChungVoiTayKeo())
                _handPointer.gameObject.SetActive(false);
            return;
        }

        // ── [V6] MỘT TAY MỘT LÚC ──────────────────────────────────────────────
        bool keoDangChay   = _dragHintAnimator != null && _dragHintAnimator.IsRunning;
        bool aoAnhDangChay = TutorialPhantomDemoManager.Instance != null
                             && TutorialPhantomDemoManager.Instance.IsDemoRunning;

        if (keoDangChay || aoAnhDangChay)
        {
            _currentTargetRect = null;
            if (!TayTinhDungChungVoiTayKeo())
                _handPointer.gameObject.SetActive(false);
            return;
        }

        _currentTargetRect = targetRect;
        _currentHandOffset = step.handOffset;

        _handPointer.gameObject.SetActive(true);
        TutorialHandBus.Nhan(LoaiTay.TayTinh);

        PlaceHandPointerAtTarget(targetRect, step.handOffset);

        if (_handAnimator != null) _handAnimator.SetTrigger("Bounce");
    }

    private void PlaceHandPointerAtTarget(RectTransform targetRect, Vector2 offset)
    {
        if (_handPointer == null || targetRect == null) return;

        Vector3[] corners = new Vector3[4];
        targetRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        Canvas targetCanvas = targetRect.GetComponentInParent<Canvas>();
        Camera targetCam = (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? targetCanvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCam, worldCenter);

        RectTransform handParent = _handPointer.parent as RectTransform;
        Canvas handCanvas = _handPointer.GetComponentInParent<Canvas>();
        Camera handCam = (handCanvas != null && handCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? handCanvas.worldCamera : null;

        if (handParent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(handParent, screenPoint, handCam, out Vector2 localPoint))
        {
            _handPointer.anchoredPosition = localPoint + offset;
        }
        else
        {
            _handPointer.position = targetRect.position;
            _handPointer.anchoredPosition += offset;
        }
    }

    /// <summary>
    /// [V6] Tay tĩnh và tay kéo có đang là CÙNG MỘT object không.
    /// Xảy ra khi TutorialDragHintAnimator._hand bỏ trống trong Inspector — nó tự lấy
    /// <see cref="HandPointerRT"/>. Lúc đó không được tắt "tay tĩnh" để nhường tay kéo,
    /// vì tắt là tắt luôn chính tay kéo.
    /// </summary>
    private bool TayTinhDungChungVoiTayKeo()
    {
        return _handPointer != null
               && _dragHintAnimator != null
               && _dragHintAnimator.TayKeoRT == _handPointer;
    }

    // =========================================================================
    // [V6] API CÔNG KHAI CHO TRỌNG TÀI BÀN TAY (TutorialHandBus)
    // =========================================================================
    // Cờ chống dội: đang CHỦ ĐỘNG dọn tay (sang bước mới / ẩn UI cho popup) thì
    // CapNhatLaiTayTinh phải câm, nếu không StopDragHint/StopGuide trong lúc dọn sẽ
    // lập tức bật lại tay tĩnh — đúng thứ ta vừa tắt.
    private bool _dangDonTay;

    /// <summary>[V6] Ẩn tay tĩnh. Tay kéo / tay hành động gọi trước khi bật tay của mình.</summary>
    public void AnTayTinh()
    {
        if (this == null) return;   // manager đã bị Destroy (thoát Play) → không đụng gì
        if (_handPointer == null) return;
        if (TayTinhDungChungVoiTayKeo()) return;   // dùng chung object → tắt là tắt nhầm
        if (_handPointer.gameObject.activeSelf)
            _handPointer.gameObject.SetActive(false);
    }

    /// <summary>[V6] Dừng tay kéo (tay hành động gọi để không hiện 2 tay).</summary>
    public void DungTayKeo()
    {
        if (this == null) return;
        _dragHintAnimator?.StopDragHint();
    }

    /// <summary>[V6] Dừng tay hành động (tay kéo gọi để không hiện 2 tay).</summary>
    public void DungTayHanhDong()
    {
        if (this == null) return;
        _actionHandGuide?.StopGuide();
    }

    /// <summary>
    /// [V6] Chủ vừa nhả quyền ⇒ xem bước hiện tại có còn CẦN tay tĩnh không, cần thì bật lại.
    /// ƯU TIÊN TUYỆT ĐỐI: không bao giờ để 0 bàn tay ở một bước cần tay.
    ///
    /// Gọi từ cuối StopDragHint / StopGuide. KHÔNG gây đệ quy: hàm này chỉ đi tới
    /// UpdateHandPointer, mà UpdateHandPointer không gọi ngược lại Stop… của ai cả.
    /// </summary>
    public void CapNhatLaiTayTinh()
    {
        if (this == null) return;   // manager đã bị Destroy (thoát Play) → không đụng gì
        if (_dangDonTay) return;                       // đang dọn tay có chủ đích → kệ
        if (_handPointer == null) return;
        if (_state == TutorialState.Finished || _state == TutorialState.Idle) return;
        if (_currentIndex < 0 || _currentIndex >= _steps.Count) return;

        var step = _steps[_currentIndex];
        if (step == null || !step.showHandPointer) return;

        // Các bước "chỉ hành động" CỐ Ý không dùng tay tĩnh (đã có tay quét / tay hành động).
        if (IsActionOnlyStep(step.name)) return;

        // Vẫn còn chủ khác giữ quyền ⇒ chưa tới lượt tay tĩnh.
        if (TutorialHandBus.ChuKhacDangGiu(LoaiTay.TayTinh)) return;
        if (TutorialPhantomDemoManager.Instance != null
            && TutorialPhantomDemoManager.Instance.IsDemoRunning) return;

        UpdateHandPointer(step, GetTargetRect(step.targetID));
    }

    /// <summary>
    /// [V6] Nhả quyền + ẩn CẢ BA bàn tay thật. Dùng khi sang bước mới hoặc dọn UI:
    /// sang bước mới mà sót tay của bước cũ là lỗi hay gặp nhất.
    /// </summary>
    private void DonSachMoiBanTay()
    {
        bool truoc = _dangDonTay;
        _dangDonTay = true;   // chặn CapNhatLaiTayTinh dội ngược trong lúc dọn
        try
        {
            TutorialHandBus.NhaTatCa();
            _actionHandGuide?.StopGuide();
            _dragHintAnimator?.StopDragHint();
            if (_handPointer != null) _handPointer.gameObject.SetActive(false);
        }
        finally
        {
            _dangDonTay = truoc;
        }
    }

    // =========================================================================
    // Finish
    // =========================================================================
    /// <summary>
    /// Tắt sạch mọi thứ của tutorial mà KHÔNG chạy qua bước nào. Dùng khi người chơi đã
    /// xong tutorial ở phiên trước (B1).
    ///
    /// Tách riêng khỏi <see cref="FinishTutorial"/> vì hàm kia còn đóng dấu cờ và lia
    /// camera về — ở đây cờ đã có sẵn, chỉ cần dọn màn hình cho sạch raycast.
    /// </summary>
    private void SkipTutorialEntirely()
    {
        _state        = TutorialState.Finished;
        _currentIndex = _steps.Count;

        SetTutorialUIVisible(false);
        SetCloudPanelVisible(false);
        AnHopThoai();   // [V6] đóng card thoại để nhả HUD (HudNavHider) khi bỏ qua tutorial
        TutorialPhantomDemoManager.Instance?.StopDemo();
        _guideBoardUI?.Hide();
        _dimBackground?.ClearHole();
        if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
        DonSachMoiBanTay();   // [V6] nhả quyền + ẩn cả ba tay thật
        _runtimeTargetResolver?.EnableAreaMask(TutorialAreaKind.None, null);

        // Bắt buộc: Canvas tàng hình mà còn blocksRaycasts thì nuốt sạch click game.
        if (_tutorialCanvasCG != null)
        {
            _tutorialCanvasCG.alpha          = 0f;
            _tutorialCanvasCG.interactable   = false;
            _tutorialCanvasCG.blocksRaycasts = false;
        }
    }

    private void FinishTutorial()
    {
        _state = TutorialState.Finished;

        // B1 — đóng dấu NGAY tại đây, TRƯỚC phần dọn UI: nếu một lời gọi dọn bên dưới ném
        // lỗi (ref rỗng chẳng hạn) thì cờ vẫn đã ghi xong, người chơi không bị dắt lại.
        MarkTutorialDone();
        Debug.Log("[Tutorial] Tutorial FINISHED — restoring camera and closing UI.");
        SetTutorialUIVisible(false);
        AnHopThoai();   // [V6] đóng card thoại để nhả HUD (HudNavHider) khi kết thúc tutorial
        TutorialPhantomDemoManager.Instance?.StopDemo();
        _guideBoardUI?.Hide();
        _dimBackground?.ClearHole();
        _cameraFocus?.RestoreCamera();
        DonSachMoiBanTay();   // [V6] nhả quyền + ẩn cả ba tay thật

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
            case "L2_05_PlantCorn":           return "seed_bapcai";
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
        AnHopThoai();
        _guideBoardUI?.Hide();
        if (_handPointer != null) _handPointer.gameObject.SetActive(false); // tránh 2 bàn tay

        if (_dimBackground != null) _dimBackground.gameObject.SetActive(true);
        _runtimeTargetResolver?.EnableAreaMask(kind, _dimBackground);

        // [WP-A1] Số id = số ô mà GATE đếm (LayODatLua / LayChauHoa) — trước đây cứng 8/6,
        // lệch với gate là tay quét thiếu/thừa ô. Resolver đặt tên tutorial_plot_01.. / tutorial_flower_01..
        string[] ids = TaoIdQuetO(kind);

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
        // [VÒNG 17 — SỬA LỖI ĐUA] Bản cũ xét 'Level < 2' rồi bỏ qua chờ nếu đã lên cấp.
        // Nhưng EXP KHÔNG cộng một lần: HarvestFeedbackSpawner sinh nhiều viên EXP bay,
        // MỖI VIÊN gọi AddExp() riêng khi chạm thanh. Nếu Level kịp chạm 2 TRƯỚC khi
        // coroutine chạy tới đây thì hàm thoát ngay ⇒ KHÔNG ẩn UI, KHÔNG chờ bấm "Nhận"
        // ⇒ card thoại (order cao) đè thẳng lên popup lên cấp. Đó là lỗi Sếp gặp.
        // Nay không đoán theo Level nữa — chỉ nhìn trạng thái popup THẬT.

        // Ẩn UI tutorial khi đang đợi để không che/đè popup lên cấp.
        AnToanBoUiTutorial();

        // [FIX KẸT] Popup lên cấp đã hiện TRƯỚC khi tín hiệu thu hoạch được tiêu thụ (hàng đợi
        // vừa nhả sau khi người chơi bấm "Nhận") ⇒ không còn gì để chờ, chờ 12s chỉ làm treo.
        if (!LevelUpPopupUI.IsActive && LevelUpVuaDongXong())
        {
            Debug.Log("[Tutorial] WaitForLevelUpClaim: popup lên cấp vừa đóng xong (đã bấm Nhận) → không chờ nữa.");
            yield break;
        }

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

    // =========================================================================
    // [VÒNG 17] Ẩn / hiện toàn bộ lớp UI tutorial — dùng cho CỔNG POPUP
    // =========================================================================

    /// <summary>Tắt sạch mọi thứ tutorial đang vẽ trên màn, để popup hệ thống có sân khấu riêng.</summary>
    private void AnToanBoUiTutorial()
    {
        AnHopThoai();
        _guideBoardUI?.Hide();
        DonSachMoiBanTay();   // [V6] nhả quyền + ẩn cả ba tay thật
        TutorialPhantomDemoManager.Instance?.StopDemo();
        _dimBackground?.ClearHole();
        if (_dimBackground != null) _dimBackground.gameObject.SetActive(false);
    }

    /// <summary>
    /// Bật lại lớp nền tối sau khi popup đóng. CHỈ bật nền — card thoại, tay chỉ và guide
    /// board do chính bước đang chạy dựng lại, bật mù ở đây sẽ hiện nhầm nội dung bước cũ.
    /// </summary>
    private void HienLaiUiTutorial()
    {
        if (_state == TutorialState.Finished) return;
        if (_dimBackground != null) _dimBackground.gameObject.SetActive(true);
    }

    // =========================================================================
    // [VÒNG 17] API cho công cụ test TutorialDebugJump (chỉ Editor / dev build)
    // =========================================================================

    public int TongSoBuoc => _steps != null ? _steps.Count : 0;

    public int ChiSoBuocHienTai => _currentIndex;

    public string TenBuocHienTai =>
        (_currentIndex >= 0 && _currentIndex < _steps.Count && _steps[_currentIndex] != null)
            ? _steps[_currentIndex].name
            : "(chưa vào bước nào)";

    public IReadOnlyList<TutorialStepData> DanhSachBuoc => _steps;

    public TutorialStepData LayStepData(int chiSo) =>
        (_steps != null && chiSo >= 0 && chiSo < _steps.Count) ? _steps[chiSo] : null;

    public string LayTenBuoc(int chiSo) =>
        (_steps != null && chiSo >= 0 && chiSo < _steps.Count && _steps[chiSo] != null)
            ? _steps[chiSo].name
            : "(trống)";

    // =========================================================================
    // [V6] CHỈ-ĐỌC CHO CHẨN ĐOÁN (F10 — PopupCaptureReporter)
    // =========================================================================
    // Toàn bộ là THÊM MỚI, không đổi thành viên cũ. Mục đích: Lead nhìn một báo cáo
    // là biết tutorial đang đứng ở đâu và vì sao đứng im, không phải đoán qua log.

    /// <summary>Tên trạng thái máy trạng thái (Idle/Intro/TypingText/WaitingAction/…).</summary>
    public string TenTrangThai => _state.ToString();

    /// <summary>Điều kiện chờ của bước đang chạy (WaitForPlant, WaitForHarvest…).</summary>
    public string TenActionDangCho => _pendingWait.ToString();

    /// <summary>Bản sao hàng đợi action đang tồn đọng (không cho sửa hàng đợi thật).</summary>
    public string[] HangDoiActionHienTai
    {
        get
        {
            if (_hangDoiAction == null || _hangDoiAction.Count == 0) return new string[0];
            var mang = _hangDoiAction.ToArray();
            var ten = new string[mang.Length];
            for (int i = 0; i < mang.Length; i++) ten[i] = mang[i].ToString();
            return ten;
        }
    }

    /// <summary>Tay TĨNH (_handPointer) có đang bật không.</summary>
    public bool CoTayTinhDangBat =>
        _handPointer != null && _handPointer.gameObject.activeInHierarchy;

    /// <summary>Tên object tay tĩnh trong scene (hoặc "(chưa gán)").</summary>
    public string TenTayTinh => _handPointer != null ? _handPointer.gameObject.name : "(chưa gán)";

    /// <summary>Tay KÉO (TutorialDragHintAnimator) có đang bật không.</summary>
    public bool CoTayKeoDangBat
    {
        get
        {
            if (_dragHintAnimator == null) return false;
            var rt = _dragHintAnimator.TayKeoRT;
            return _dragHintAnimator.IsRunning
                   || (rt != null && rt.gameObject.activeInHierarchy);
        }
    }

    /// <summary>Tên object tay kéo trong scene.</summary>
    public string TenTayKeo
    {
        get
        {
            var rt = _dragHintAnimator != null ? _dragHintAnimator.TayKeoRT : null;
            return rt != null ? rt.gameObject.name : "(chưa gán)";
        }
    }

    /// <summary>Tay HÀNH ĐỘNG (TutorialActionHandGuide) có đang bật không.</summary>
    public bool CoTayHanhDongDangBat
    {
        get
        {
            if (_actionHandGuide == null) return false;
            var rt = _actionHandGuide.TayHanhDongRT;
            return _actionHandGuide.DangChay
                   || (rt != null && rt.gameObject.activeInHierarchy);
        }
    }

    /// <summary>Tên object tay hành động trong scene.</summary>
    public string TenTayHanhDong
    {
        get
        {
            var rt = _actionHandGuide != null ? _actionHandGuide.TayHanhDongRT : null;
            return rt != null ? rt.gameObject.name : "(chưa gán)";
        }
    }

    /// <summary>Ảo ảnh làm mẫu (TutorialPhantomDemoManager) có đang chạy không.</summary>
    public bool CoAoAnhDangChay =>
        TutorialPhantomDemoManager.Instance != null
        && TutorialPhantomDemoManager.Instance.IsDemoRunning;

    /// <summary>Ai đang giữ quyền bàn tay theo trọng tài TutorialHandBus.</summary>
    public string TenChuTayHienTai => TutorialHandBus.ChuHienTai.ToString();

    /// <summary>Hộp thoại NPC cũ còn tồn tại trong scene không (mong đợi: đã bị xoá).</summary>
    public bool CoHopThoaiNpcCu => _npcDialogPopup != null;

    /// <summary>Khoá PlayerPrefs "đã xong tutorial" — cho công cụ chẩn đoán đọc.</summary>
    public static string KhoaPrefDaXong => PrefKeyDone;

    /// <summary>Khoá PlayerPrefs "bước đang đứng" — cho công cụ chẩn đoán đọc.</summary>
    public static string KhoaPrefBuoc => PrefKeyStep;

    /// <summary>
    /// Nhảy thẳng tới bước chỉ định để test. Dọn sạch UI bước cũ rồi chạy bước mới.
    /// KHÔNG chạy lại logic gameplay của các bước bị bỏ qua — chỉ dùng để xem giao diện.
    /// </summary>
    public void DebugNhayToiBuoc(int chiSo)
    {
        if (_steps == null || chiSo < 0 || chiSo >= _steps.Count)
        {
            Debug.LogWarning($"[Tutorial] DebugNhayToiBuoc({chiSo}) — ngoài khoảng 0..{(_steps?.Count ?? 0) - 1}.");
            return;
        }

        StopAllCoroutines();
        _choPopupDongCo = null;             // StopAllCoroutines đã giết nó — xoá cờ để lần sau bật lại được
        AnToanBoUiTutorial();
        _hangDoiAction.Clear();
        _penOpenSubActionReceived = false;

        _currentIndex = chiSo - 1;          // AdvanceToNextStep sẽ ++ lên đúng chiSo
        _state = TutorialState.Transitioning;
        StartCoroutine(WatchdogChongKet());  // StopAllCoroutines đã tắt watchdog, bật lại
        AdvanceToNextStep();

        Debug.Log($"[Tutorial] ⏭ NHẢY TỚI bước [{chiSo}] '{LayTenBuoc(chiSo)}' (chế độ test).");
    }

    private void SetTutorialUIVisible(bool visible)
    {
        if (_dimBackground  != null) _dimBackground.gameObject.SetActive(visible);

        // [V6] Hộp thoại NPC cũ đã bị khai tử — chỉ dùng card V2.
        // Trước đây là SetActive(visible) ⇒ mỗi lần hiện lại UI tutorial là hộp thoại
        // ông già sống dậy. Nay CHỈ cho phép tắt, không bao giờ bật.
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(false);

        if (_handPointer    != null) _handPointer.gameObject.SetActive(false);
    }

    /// <summary>
    /// [FIX 2026-09-06] Tu dong bung ban tay huong dan cho buoc CAN THAO TAC sau khi nguoi choi
    /// co ~1,2 giay doc loi thoai — khong con phu thuoc vao viec ho co cham vao card hay khong.
    /// Dung chung duong voi TryDismissInteractionDialog nen khong nhan doi logic.
    /// </summary>
    private void BatTuHienTayThaoTac()
    {
        if (_tuHienTayCo != null) StopCoroutine(_tuHienTayCo);
        _tuHienTayCo = StartCoroutine(TuHienTayThaoTacRoutine());
    }

    private Coroutine _tuHienTayCo;

    private IEnumerator TuHienTayThaoTacRoutine()
    {
        int buocLucGoi = _currentIndex;

        // Cho doc chu (card tu go xong) roi them mot nhip ngan.
        float t = 0f;
        while (t < 1.2f)
        {
            if (_currentIndex != buocLucGoi) { _tuHienTayCo = null; yield break; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_currentIndex == buocLucGoi && !_interactionDialogDismissed)
            TryDismissInteractionDialog();

        _tuHienTayCo = null;
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

    /// <summary>
    /// [FIX 2026-09-06] Nhac nhanh: hien card thoai voi 1 cau ngan roi TU AN sau vai giay.
    /// Dung cho cac buoc "quet" (gieo/thu hoach het o) — truoc day cac buoc nay an sach card
    /// nen nguoi choi khong biet phai lam gi tiep, cam giac tutorial dung.
    /// KHONG doi bam, khong chan thao tac: tay van quet binh thuong ben duoi.
    /// </summary>
    private void NhacNhanh(string noiDung, float giay = 3.2f)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return;
        if (!DungCardV2) return;
        if (_nhacNhanhCo != null) StopCoroutine(_nhacNhanhCo);
        _nhacNhanhCo = StartCoroutine(NhacNhanhRoutine(noiDung, giay));
    }

    private Coroutine _nhacNhanhCo;

    private IEnumerator NhacNhanhRoutine(string noiDung, float giay)
    {
        // Cho 1 khung hinh: nhanh buoc goi NhacNhanh() xong con chay tiep SetupSmartGuide(),
        // ma ham do mo dau bang AnHopThoai() — hien card truoc la bi an ngay lap tuc.
        yield return null;

        int buocLucGoi = _currentIndex;

        // [FIX 2026-09-06] Khong nhac khi nguoi choi dang keo. Neu khay hat dang mo (buoc gieo)
        // thi CHO khay dong roi moi nhac — khong bao gio de card len khay hat.
        float cho = 0f;
        while (cho < 8f && (FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle
                            || FarmInputLock.IsSeedPopupOpen))
        {
            if (_currentIndex != buocLucGoi) { _nhacNhanhCo = null; yield break; }
            cho += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_currentIndex != buocLucGoi) { _nhacNhanhCo = null; yield break; }
        if (_v2Card == null) { _nhacNhanhCo = null; yield break; }

        _v2Card.Show(Loc.T(noiDung), TutorialNpcClip.Talk);
        _v2Card.SkipTyping();                       // hien het chu ngay, khong bat cho go

        float t = 0f;
        while (t < giay)
        {
            if (_currentIndex != buocLucGoi) break;   // qua buoc khac roi thi thoi nhac
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_currentIndex == buocLucGoi) _v2Card.Hide();
        _nhacNhanhCo = null;
    }

    /// <summary>
    /// [FIX 2026-09-06] Cho den khi nguoi choi THAT SU roi tay ra:
    ///   1) khong con keo hat / keo liem,
    ///   2) khay hat (va khay hoa) da dong — con mo thi tu dong dong lai,
    ///   3) nghi mot nhip ngan cho hieu ung dong khay chay xong.
    /// Nho vay hoi thoai luon xuat hien SAU thao tac, khong bao gio de len khay.
    /// Co tran thoi gian de khong bao gio treo buoc.
    /// </summary>
    private IEnumerator ChoThaoTacXongHan(float tranGiay = 4f)
    {
        float t = 0f;
        while (t < tranGiay)
        {
            bool dangKeo = FarmInputLock.IsDraggingSeed || FarmInputLock.IsDraggingSickle;
            if (!dangKeo) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (FarmInputLock.IsSeedPopupOpen && FarmUIManager.Instance != null)
            FarmUIManager.Instance.HidePlantSelectPopup();

        yield return new WaitForSecondsRealtime(0.35f);   // nhip nghi cho khay dong xong
    }

    private void HideBlockingTutorialUI()
    {
        AnHopThoai();
    }

    // =========================================================================
    // [WP-A1] Chống "tay kẹt" ở các bước quét ô (trồng/thu hoạch hết lúa/hoa)
    // =========================================================================
    private TutorialStepTriggerBridge _bridgeCache;

    /// <summary>Bridge đếm ô — ưu tiên cùng GameObject, không có thì tìm trong scene; cache lại.</summary>
    private TutorialStepTriggerBridge LayBridge()
    {
        if (_bridgeCache == null) _bridgeCache = GetComponent<TutorialStepTriggerBridge>();
        if (_bridgeCache == null) _bridgeCache = FindFirstObjectByType<TutorialStepTriggerBridge>();
        return _bridgeCache;
    }

    /// <summary>Action thuộc nhóm 4 gate "quét hết ô" (lúa/hoa × trồng/thu hoạch)?</summary>
    private static bool LaBuocChoQuetO(TutorialWaitAction a) =>
        a == TutorialWaitAction.WaitForAllPlotsPlanted
        || a == TutorialWaitAction.WaitForAllPlotsHarvested
        || a == TutorialWaitAction.WaitForAllFlowerPlotsPlanted
        || a == TutorialWaitAction.WaitForAllFlowerPlotsHarvested;

    /// <summary>Bước KẾ TIẾP (currentIndex+1) có chờ đúng action này không?</summary>
    private bool BuocKeTiepCho(TutorialWaitAction a)
    {
        int ke = _currentIndex + 1;
        if (ke < 0 || ke >= _steps.Count || _steps[ke] == null) return false;
        return _steps[ke].waitAction == a;
    }

    /// <summary>Bỏ mọi bản của action ra khỏi hàng đợi (giữ thứ tự phần còn lại).</summary>
    private void XoaKhoiHangDoi(TutorialWaitAction a)
    {
        if (!_hangDoiAction.Contains(a)) return;
        var conLai = new Queue<TutorialWaitAction>();
        while (_hangDoiAction.Count > 0)
        {
            var x = _hangDoiAction.Dequeue();
            if (x != a) conLai.Enqueue(x);
        }
        while (conLai.Count > 0) _hangDoiAction.Enqueue(conLai.Dequeue());
    }

    /// <summary>
    /// Gọi NGAY SAU khi đặt _pendingWait/_state cho một bước quét ô:
    /// reset latch của bridge rồi kiểm tra lại gate theo TRẠNG THÁI THẬT của ruộng.
    /// Đã đủ (người chơi làm xong từ trước) ⇒ bridge gọi Notify ⇒ qua bước ngay; trả true.
    /// Tín hiệu cũ cùng loại trong hàng đợi bị bỏ trước — gate sống mới là nguồn sự thật,
    /// tránh tín hiệu "thừa" từ bước trước làm bước sau (vd L2_05 trồng Ngô) qua oan.
    /// </summary>
    private bool ThuQuaGateNgay(TutorialStepData step)
    {
        if (step == null || !LaBuocChoQuetO(step.waitAction)) return false;
        var bridge = LayBridge();
        if (bridge == null)
        {
            Debug.LogWarning("[Tutorial][Gate] Không tìm thấy TutorialStepTriggerBridge — không kiểm tra lại gate được.");
            return false;
        }
        XoaKhoiHangDoi(step.waitAction);
        bridge.ResetAllTracking();
        return bridge.KiemTraLaiGate(step.waitAction);
    }

    /// <summary>Id ô cho tay quét — đếm theo đúng tập ô của gate (tối thiểu 1 để không rỗng).</summary>
    private static string[] TaoIdQuetO(TutorialAreaKind kind)
    {
        bool hoa = kind == TutorialAreaKind.Flower;
        int n = hoa ? TutorialStepTriggerBridge.LayChauHoa().Count : TutorialStepTriggerBridge.LayODatLua().Count;
        if (n <= 0) n = hoa ? 6 : 8;   // chưa có ô nào (scene chưa sẵn) → giữ số cũ
        var ids = new string[n];
        string tienTo = hoa ? "tutorial_flower_" : "tutorial_plot_";
        for (int i = 0; i < n; i++) ids[i] = $"{tienTo}{(i + 1):00}";
        return ids;
    }

    // =========================================================================
    // [PHANTOM] Trợ giúp cho ảo ảnh demo: id ô còn việc + icon hạt
    // =========================================================================

    /// <summary>
    /// Id proxy (tutorial_plot_xx / tutorial_flower_xx) của ô CÒN VIỆC thứ <paramref name="thuTu"/>
    /// (0 = ô đầu tiên) — trồng ⇒ ô trống, thu hoạch ⇒ ô chín — và proxy đã đăng ký (có RectTransform).
    /// Dựa trên map của TutorialRuntimeTargetResolver (IsPlotPending). Không có ⇒ null.
    /// Public static để công cụ debug / script khác hỏi được ô mà tay đang nhắm.
    /// </summary>
    public static string TimIdOConViec(TutorialAreaKind kind, bool canChin, int thuTu = 0)
    {
        if (kind == TutorialAreaKind.None || thuTu < 0) return null;
        string[] ids = TaoIdQuetO(kind);
        int dem = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (!TutorialRuntimeTargetResolver.IsPlotPending(ids[i], canChin)) continue;
            if (GetTargetRect(ids[i]) == null) continue;
            if (dem == thuTu) return ids[i];
            dem++;
        }
        return null;
    }

    /// <summary>Icon hạt giống theo seedId ("seed_rice" → CropData.icon). null nếu không có / chưa có FarmManager.</summary>
    private static Sprite LayIconHat(string seedId)
    {
        if (string.IsNullOrEmpty(seedId) || FarmManager.Instance == null) return null;
        var crop = FarmManager.Instance.GetCropById(seedId)
                   ?? FarmManager.Instance.GetCropById(seedId.Replace("seed_", ""));
        return crop != null ? crop.icon : null;
    }

    /// <summary>
    /// [WP-A1] Nút "Bỏ qua bước này" là nút Tiếp tục TRONG card thoại. Ở bước quét ô card đã bị
    /// AnHopThoai() ẩn (root SetActive(false)) ⇒ nút được bật nhưng không ai thấy ⇒ lối thoát vô dụng.
    /// Bật lại chuỗi cha từ nút lên tới Canvas (và kéo CanvasGroup về 1). Vì nút là con của card,
    /// khung card sẽ hiện lại kèm — chấp nhận: thấy nút thoát quan trọng hơn giấu card.
    /// </summary>
    private void DamBaoNutBoQuaNhinThay()
    {
        if (_v2Card == null) return;

        Button nut = null;
        foreach (var b in _v2Card.GetComponentsInChildren<Button>(true))
        {
            var lbl = b.GetComponentInChildren<TMP_Text>(true);
            if (lbl != null && lbl.text == "Bỏ qua bước này") { nut = b; break; }
        }
        if (nut == null)
        {
            Debug.LogWarning("[Tutorial][Watchdog] Không tìm thấy nút 'Bỏ qua bước này' trong card — " +
                             "người chơi có thể không thấy lối thoát.");
            return;
        }
        if (nut.gameObject.activeInHierarchy) return;   // đang thấy rồi, không đụng

        Transform t = nut.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            if (t.GetComponent<Canvas>() != null) break;
            t = t.parent;
        }
        Debug.LogWarning("[Tutorial][Watchdog] Card thoại đang ẩn — đã bật lại chuỗi cha để nút " +
                         "'Bỏ qua bước này' hiện cho người chơi.");
    }

    private void ConsumeQueuedAction()
    {
        if (_hangDoiAction.Count == 0 || !_hangDoiAction.Contains(_pendingWait)) return;
        // Lọc bỏ đúng action khớp, giữ nguyên thứ tự các action còn lại.
        var conLai = new Queue<TutorialWaitAction>();
        bool daLay = false;
        while (_hangDoiAction.Count > 0)
        {
            var a = _hangDoiAction.Dequeue();
            if (!daLay && a == _pendingWait) { daLay = true; continue; }
            conLai.Enqueue(a);
        }
        while (conLai.Count > 0) _hangDoiAction.Enqueue(conLai.Dequeue());
        if (!daLay) return;
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
        return string.Equals(itemId, "seed_bapcai", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cropId, "bapcai", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cropId, "cabbage", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(itemId, "seed_ngo", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(cropId, "ngo", System.StringComparison.OrdinalIgnoreCase);
    }

    public bool LaBuocGameplayMoKhongKhoaRaycast()
    {
        if (_currentIndex < 0 || _currentIndex >= _steps.Count) return false;
        var step = _steps[_currentIndex];
        if (step == null) return false;
        return step.waitAction == TutorialWaitAction.WaitForPlant
            || step.waitAction == TutorialWaitAction.WaitForAllPlotsPlanted
            || step.waitAction == TutorialWaitAction.WaitForHarvest
            || step.waitAction == TutorialWaitAction.WaitForAllPlotsHarvested
            || step.waitAction == TutorialWaitAction.WaitForAllFlowerPlotsPlanted
            || step.waitAction == TutorialWaitAction.WaitForAllFlowerPlotsHarvested
            || step.waitAction == TutorialWaitAction.WaitForFeed
            || step.waitAction == TutorialWaitAction.WaitForBuyItem
            || step.waitAction == TutorialWaitAction.WaitForSickleShown;
    }

    private static bool IsActionOnlyStep(string stepName)
    {
        return stepName == "L1L2_04_FocusPlots"
            || stepName == "L1L2_04b_FirstHarvest"
            || stepName == "L1L2_05_DragFirstRice"
            || stepName == "L1L2_06_PlantAllRice"
            || stepName == "L1L2_07_OpenCropProgress"
            || stepName == "L1L2_08_SpeedUpTip"
            || stepName == "L1L2_09_HarvestFirstRice"
            || stepName == "L1L2_10_HarvestAllRice"
            || stepName == "L1L2_12_FocusFlowerPots"
            || stepName == "L1L2_13_DragFirstFlower"
            || stepName == "L1L2_14_PlantAllFlowers"
            || stepName == "L1L2_15_FlowerSpeedUp"
            || stepName == "L1L2_16_HarvestFirstFlower"
            || stepName == "L1L2_17_HarvestAllFlowers"
            || stepName == "L2_01_GotoShop"
            || stepName == "L2_03_BuyCorn"
            || stepName == "L2_04_CloseShop"
            || stepName == "L2_05_PlantCorn"
            || stepName == "L2_07_FocusPen"
            || stepName == "L2_08_FeedPen"
            || stepName == "L2_09_PenSpeedUp"
            || stepName == "L2_10_HarvestPen";
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
