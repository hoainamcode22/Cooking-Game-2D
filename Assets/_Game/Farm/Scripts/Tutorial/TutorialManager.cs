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
    public static void UnregisterTarget(string id)
    {
        if (!string.IsNullOrEmpty(id)) _targetRegistry.Remove(id);
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

    /// <summary>Báº¯t Ä‘áº§u tutorial tá»« Step 0 (gá»i sau intro animation).</summary>
    public void StartTutorial()
    {
        _state        = TutorialState.Idle;
        _currentIndex = -1;
        SetTutorialUIVisible(true);

        // Focus camera vào 6 ô lúa ngay khi tutorial bắt đầu
        var bridge = GetComponent<TutorialStepTriggerBridge>();
        if (_cameraFocus != null && bridge != null)
            _cameraFocus.FocusOnRice(bridge);

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
        _interactionDialogDismissed = false;
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

        if (step.name == "L1L2_11_TransitionFlower" && _cameraFocus != null)
        {
            var bridge = GetComponent<TutorialStepTriggerBridge>();
            if (bridge != null) _cameraFocus.FocusOnFlower(bridge);
        }

        _state = TutorialState.Transitioning;
        StartCoroutine(PlayStep(step));
    }

    private IEnumerator PlayStep(TutorialStepData step)
    {
        if (_dimBackground != null)
            _dimBackground.gameObject.SetActive(true);

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

            if (step.name == "L1L2_07_OpenCropProgress"
                || step.name == "L1L2_08_SpeedUpTip")
                _actionHandGuide?.GuideSpeedUp("tutorial_plot_01");
            else if (step.name == "L1L2_09_HarvestFirstRice")
                _actionHandGuide?.GuideHarvest("tutorial_plot_01");
            else if (step.name == "L1L2_10_HarvestAllRice")
                _actionHandGuide?.GuideHarvest("tutorial_plot_01");
            else if (step.name == "L1L2_17_HarvestAllFlowers")
                _actionHandGuide?.GuideHarvest("tutorial_flower_01");

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

    private static bool IsInteractionStep(string stepName)
    {
        return stepName == "L1L2_12_FocusFlowerPots"
            || stepName == "L1L2_15_FlowerSpeedUp"
            || stepName == "L1L2_16_HarvestFirstFlower";
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
