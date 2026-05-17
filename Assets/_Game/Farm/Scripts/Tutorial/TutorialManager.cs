using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// State Machine quản lý Tutorial Level 1-5 (Hay Day style).
///
/// Luồng khởi động:
///   Start() → PlayIntroAnimation() [mây bay + camera zoom] → StartTutorial() → Step 1…
///
/// API cho game systems:
///   TutorialManager.Instance.NextStep()
///   TutorialManager.Instance.NotifyPlant()
///   TutorialManager.Instance.NotifyHarvest()
///   TutorialManager.Instance.NotifyCook()
///   TutorialManager.Instance.NotifyDelivery()
///   TutorialManager.Instance.NotifyBuyItem()
///   TutorialManager.Instance.NotifyBuyAnimal()
///   TutorialManager.Instance.NotifyTrainLoad()
///   TutorialManager.Instance.NotifyAction(TutorialWaitAction)
///   TutorialManager.RegisterTarget / UnregisterTarget
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // =========================================================================
    // Singleton
    // =========================================================================
    public static TutorialManager Instance { get; private set; }

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
    public static void UnregisterTarget(string id)
    {
        if (!string.IsNullOrEmpty(id)) _targetRegistry.Remove(id);
    }

    // =========================================================================
    // Inspector — Steps
    // =========================================================================
    [Header("Steps (tự động gán bởi TutorialSystemGenerator)")]
    [SerializeField] private List<TutorialStepData> _steps = new();

    // =========================================================================
    // Inspector — Core UI
    // =========================================================================
    [Header("Core UI")]
    [SerializeField] private UnmaskRaycastFilter _dimBackground;
    [SerializeField] private GameObject          _npcDialogPopup;
    [SerializeField] private TextMeshProUGUI     _npcDialogText;
    [SerializeField] private Image               _npcPortrait;
    [SerializeField] private RectTransform       _handPointer;
    [SerializeField] private Animator            _handAnimator;

    // =========================================================================
    // Inspector — Intro Animation
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

    // =========================================================================
    // Inspector — Settings
    // =========================================================================
    [Header("Settings")]
    [SerializeField] private bool _clickToSkipTyping = true;

    [Tooltip("Bỏ qua intro animation khi debug trong Editor")]
    [SerializeField] private bool _skipIntroInEditor = false;

    // =========================================================================
    // Runtime
    // =========================================================================
    private int                _currentIndex = -1;
    private Coroutine          _typingCoroutine;
    private bool               _typingDone;
    private TutorialWaitAction _pendingWait;

    private Vector2 _cloudLeftOrigin;
    private Vector2 _cloudRightOrigin;

    // CanvasGroup — tắt blocksRaycasts khi ẩn để UI tàng hình không nuốt click game
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

        // Cache CanvasGroup để điều khiển blocksRaycasts khi ẩn/hiện
        if (_cloudPanel != null)
            _cloudPanelCG = _cloudPanel.GetComponent<CanvasGroup>();

        // Tutorial_Canvas là cha của Dim_Background — leo lên tìm CanvasGroup
        if (_dimBackground != null)
            _tutorialCanvasCG = _dimBackground.GetComponentInParent<Canvas>()
                                              ?.GetComponent<CanvasGroup>();

        SetTutorialUIVisible(false);
        SetCloudPanelVisible(true);

        if (_steps.Count == 0)
        {
            Debug.LogWarning("[TutorialManager] Không có step nào. Hãy gán TutorialStepData vào _steps.");
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
    /// (1) Lerp mây bay ra hai bên trong _introDuration.
    /// (2) Camera zoom in song song.
    /// (3) Sau khi xong → StartTutorial() → Step 1.
    /// </summary>
    private IEnumerator PlayIntroAnimation()
    {
        _state = TutorialState.Intro;

        _cameraZoom?.ResetZoom();
        if (_cameraZoom != null) StartCoroutine(_cameraZoom.ZoomIn());

        float elapsed   = 0f;
        var   leftEnd   = _cloudLeftOrigin  + new Vector2(-_cloudSlideDistance, 0f);
        var   rightEnd  = _cloudRightOrigin + new Vector2( _cloudSlideDistance, 0f);

        while (elapsed < _introDuration)
        {
            elapsed += Time.deltaTime;
            float t = _introEase.Evaluate(Mathf.Clamp01(elapsed / _introDuration));

            if (_cloudLeft  != null) _cloudLeft.anchoredPosition  = Vector2.Lerp(_cloudLeftOrigin,  leftEnd,  t);
            if (_cloudRight != null) _cloudRight.anchoredPosition = Vector2.Lerp(_cloudRightOrigin, rightEnd, t);

            yield return null;
        }

        SetCloudPanelVisible(false);
        yield return null;

        StartTutorial();
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Bắt đầu tutorial từ Step 0 (gọi sau intro animation).</summary>
    public void StartTutorial()
    {
        _state        = TutorialState.Idle;
        _currentIndex = -1;
        SetTutorialUIVisible(true);
        AdvanceToNextStep();
    }

    /// <summary>
    /// Chuyển bước tiếp theo.
    /// • Đang typewriter → skip text.
    /// • Đang WaitForClick → advance.
    /// Gọi từ Button "Tiếp Theo" trên NPC_Dialog_Popup (auto-wired bởi Generator).
    /// </summary>
    public void NextStep()
    {
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

    /// <summary>Game systems gọi để báo player hoàn thành hành động.</summary>
    public void NotifyAction(TutorialWaitAction action)
    {
        if (_state != TutorialState.WaitingAction) return;
        if (_pendingWait == action) AdvanceToNextStep();
    }

    // Convenience wrappers — từng game system gọi đúng loại
    public void NotifyPlant()       => NotifyAction(TutorialWaitAction.WaitForPlant);
    public void NotifyHarvest()     => NotifyAction(TutorialWaitAction.WaitForHarvest);
    public void NotifyCook()        => NotifyAction(TutorialWaitAction.WaitForCook);

    /// <summary>Gọi khi player giao hàng thành công cho Nhà Dân (Level 2).</summary>
    public void NotifyDelivery()    => NotifyAction(TutorialWaitAction.WaitForDelivery);

    /// <summary>Gọi khi player mua vật phẩm trong Shop (Level 2 — chuồng gà, gà).</summary>
    public void NotifyBuyItem()     => NotifyAction(TutorialWaitAction.WaitForBuyItem);

    /// <summary>Gọi khi player mua gia súc (gà, bò…).</summary>
    public void NotifyBuyAnimal()   => NotifyAction(TutorialWaitAction.WaitForBuyAnimal);

    /// <summary>Gọi khi player giao đủ hàng cho Tàu Hoả (Level 4).</summary>
    public void NotifyTrainLoad()   => NotifyAction(TutorialWaitAction.WaitForTrainLoad);

    // =========================================================================
    // State Machine Core
    // =========================================================================
    private void AdvanceToNextStep()
    {
        _currentIndex++;

        if (_currentIndex >= _steps.Count)
        {
            FinishTutorial();
            return;
        }

        _state = TutorialState.Transitioning;
        StartCoroutine(PlayStep(_steps[_currentIndex]));
    }

    private IEnumerator PlayStep(TutorialStepData step)
    {
        // 1. Resolve target
        RectTransform targetRect = null;
        if (!string.IsNullOrEmpty(step.targetID) &&
            _targetRegistry.TryGetValue(step.targetID, out var tutTarget))
        {
            targetRect = tutTarget.RectTransform;
        }

        // 2. Dim / highlight
        if (targetRect != null)
            _dimBackground.SetTarget(targetRect, step.useCircleHole, step.holePaddingPx);
        else
            _dimBackground.ClearHole();

        // 3. Hand Pointer
        UpdateHandPointer(step, targetRect);

        // 4. NPC Portrait
        if (_npcPortrait != null)
        {
            _npcPortrait.sprite = step.npcPortrait;
            _npcPortrait.gameObject.SetActive(step.npcPortrait != null);
        }

        // 5. Typewriter
        _state = TutorialState.TypingText;
        yield return StartTyping(step.npcText, step.typingSpeed);

        // 6. Chờ action
        _pendingWait = step.waitAction;

        if (step.waitAction == TutorialWaitAction.Auto)
        {
            yield return new WaitForSeconds(0.8f);
            AdvanceToNextStep();
        }
        else
        {
            _state = TutorialState.WaitingAction;
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
        SetTutorialUIVisible(false);
        _dimBackground.ClearHole();

        // Tắt hoàn toàn Tutorial_Canvas — không để Canvas tàng hình chặn raycast game UI
        if (_tutorialCanvasCG != null)
        {
            _tutorialCanvasCG.alpha          = 0f;
            _tutorialCanvasCG.interactable   = false;
            _tutorialCanvasCG.blocksRaycasts = false;
        }

        Debug.Log("[TutorialManager] Tutorial hoàn thành! Bảng Xếp Hạng Vàng bắt đầu mở.");
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private void SetTutorialUIVisible(bool visible)
    {
        if (_dimBackground  != null) _dimBackground.gameObject.SetActive(visible);
        if (_npcDialogPopup != null) _npcDialogPopup.SetActive(visible);
        if (_handPointer    != null) _handPointer.gameObject.SetActive(false);
    }

    private void SetCloudPanelVisible(bool visible)
    {
        if (_cloudPanel == null) return;

        if (_cloudPanelCG != null)
        {
            // CanvasGroup: ẩn hoàn toàn kể cả raycast — không cần SetActive(false)
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
