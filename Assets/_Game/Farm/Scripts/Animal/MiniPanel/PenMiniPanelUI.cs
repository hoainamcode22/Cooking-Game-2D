using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Mini panel hiá»‡n cáº¡nh chuá»“ng khi click vÃ o. Cháº¡y trÃªn World Space Canvas.
/// State machine: Idle â†’ Processing â†’ Ready â†’ Idle.
/// Save/load tiáº¿n Ä‘á»™ qua PlayerPrefs (timestamp thá»±c táº¿).
/// </summary>
public class PenMiniPanelUI : MonoBehaviour
{
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Enums & Constants
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public enum PenState { Idle, Processing, Ready }

    private const string PrefKeyState     = "PenState_";
    private const string PrefKeyFood      = "PenFood_";
    private const string PrefKeyStartTime = "PenStartTime_";

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Inspector
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Config â€” gÃ¡n ScriptableObject Ä‘Ãºng loáº¡i chuá»“ng")]
    [SerializeField] private PenMiniPanelConfig config;

    [Header("Panel Root (World Space Canvas / Container)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slot thá»©c Äƒn 1")]
    [SerializeField] private GameObject slot1Root;
    [SerializeField] private Image      slot1Icon;
    [SerializeField] private TMP_Text   slot1Amount;

    [Header("Slot thá»©c Äƒn 2")]
    [SerializeField] private GameObject slot2Root;
    [SerializeField] private Image      slot2Icon;
    [SerializeField] private TMP_Text   slot2Amount;

    [Header("Slot rá»• thu hoáº¡ch")]
    [SerializeField] private GameObject basketRoot;
    [SerializeField] private Image      basketIcon;
    [SerializeField] private GameObject basketActiveGlow; // báº­t/táº¯t tÃ¹y Ready state

    [Header("Overlay tiáº¿n trÃ¬nh (hiá»‡n khi Processing)")]
    [SerializeField] private GameObject progressOverlay;
    [SerializeField] private Image      progressFill;     // fillAmount 0â†’1
    [SerializeField] private TMP_Text   progressLabel;    // "1:23"

    // panelCollider Ä‘Ã£ bá» â€” dÃ¹ng RectTransform cá»§a root canvas Ä‘á»ƒ check click-outside

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Runtime State
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public PenState CurrentState { get; private set; } = PenState.Idle;

    private float   processStartUnix;
    private string  activeFoodId;
    private Coroutine timerCoroutine;
    private int     _openedAtFrame = -10; // frame-guard: trÃ¡nh Ä‘Ã³ng ngay frame vá»«a má»Ÿ

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Unity Lifecycle
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Start()
    {
        LoadState();
        if (CurrentState == PenState.Processing)
        {
            float remaining = GetRemainingSeconds();
            if (remaining <= 0f)
                SetState(PenState.Ready);
            else
                timerCoroutine = StartCoroutine(ProcessTimerCoroutine(remaining));
        }
    }

    private void Update()
    {
        if (!IsPanelOpen()) return;

        // Frame-guard: bá» qua frame panel vá»«a má»Ÿ, trÃ¡nh Ä‘Ã³ng ngay láº­p tá»©c
        if (Time.frameCount <= _openedAtFrame) return;

        // Click outside â†’ Ä‘Ã³ng panel (dÃ¹ng New Input System, fallback sang legacy)
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

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Public API â€” gá»i tá»« PenClickDetector vÃ  PenDropTarget
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public bool IsPanelOpen() => panelRoot != null && panelRoot.activeSelf;

    public void OpenPanel()
    {
        if (config == null)
        {
            return;
        }
        _openedAtFrame = Time.frameCount; // Ä‘Ã¡nh dáº¥u frame má»Ÿ Ä‘á»ƒ Update bá» qua
        panelRoot.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        FarmInputLock.SuppressWorldClickForCurrentFrame();
    }

    /// <summary>
    /// PenDropTarget gá»i khi user tháº£ thá»©c Äƒn vÃ o collider chuá»“ng.
    /// Tráº£ vá» true náº¿u feed thÃ nh cÃ´ng.
    /// </summary>
    public bool TryFeed(string foodItemId, Vector3 vfxWorldPosition)
    {
        if (CurrentState != PenState.Idle)
        {
            return false;
        }

        if (foodItemId != config.food1ItemId && foodItemId != config.food2ItemId)
        {
            return false;
        }

        if (!FarmInventoryManager.Instance.HasItem(foodItemId, 1))
        {
            return false;
        }

        FarmInventoryManager.Instance.RemoveItem(foodItemId, 1);
        MissionProgressTracker.ReportEvent(MissionEventType.FeedAnimal, foodItemId, 1);
        PlayFeedVFX(foodItemId, vfxWorldPosition);
        activeFoodId = foodItemId;
        processStartUnix = (float)GetUnixNow(); // ghi timestamp trÆ°á»›c khi save
        SetState(PenState.Processing);
        SaveState();

        StopTimerIfRunning();
        timerCoroutine = StartCoroutine(ProcessTimerCoroutine(config.feedDurationSeconds));
        return true;
    }

    /// <summary>
    /// PenDropTarget gá»i khi user tháº£ rá»• vÃ o collider chuá»“ng.
    /// Tráº£ vá» true náº¿u thu hoáº¡ch thÃ nh cÃ´ng.
    /// </summary>
    public bool TryHarvest(Vector3 vfxWorldPosition)
    {
        if (CurrentState != PenState.Ready)
        {
            return false;
        }

        // Spawn sáº£n pháº©m chÃ­nh
        int productAmount = Mathf.Max(1, config.productAmount);
        SpawnHarvestFX(config.productItemId, config.productIcon, productAmount, vfxWorldPosition);

        // Sáº£n pháº©m phá»¥ (chá»‰ gÃ : egg)
        if (!string.IsNullOrEmpty(config.secondProductItemId))
            SpawnHarvestFX(config.secondProductItemId, config.secondProductIcon,
                Mathf.Max(1, config.secondProductAmount), vfxWorldPosition);

        // EXP
        if (HarvestFeedbackSpawner.Instance != null)
            HarvestFeedbackSpawner.Instance.SpawnExpFly(transform.position, config.expReward);

        // Cá»™ng vÃ o FarmInventoryManager â€” Kho popup Ä‘á»c tá»« Ä‘Ã¢y, rá»“i user chuyá»ƒn sang KitchenTransferManager
        FarmInventoryManager.Instance.AddItem(config.productItemId, productAmount);
        MissionProgressTracker.ReportEvent(MissionEventType.CollectAnimalProduct, config.productItemId, productAmount);
        if (!string.IsNullOrEmpty(config.secondProductItemId))
        {
            FarmInventoryManager.Instance.AddItem(config.secondProductItemId, Mathf.Max(1, config.secondProductAmount));
            MissionProgressTracker.ReportEvent(MissionEventType.CollectAnimalProduct, config.secondProductItemId, Mathf.Max(1, config.secondProductAmount));
        }

        activeFoodId = null;
        SetState(PenState.Idle);
        SaveState();
        RefreshUI();
        return true;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” State & Timer
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SetState(PenState newState)
    {
        CurrentState = newState;
        RefreshUI();
    }

    private IEnumerator ProcessTimerCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float remaining = Mathf.Max(0f, duration - elapsed);

            if (progressFill  != null) progressFill.fillAmount = t;
            if (progressLabel != null) progressLabel.text = FormatTime(remaining);

            yield return null;
        }

        timerCoroutine = null;
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

    private float GetRemainingSeconds()
    {
        double startUnix = processStartUnix;
        double nowUnix   = GetUnixNow();
        float elapsed    = (float)(nowUnix - startUnix);
        return Mathf.Max(0f, config.feedDurationSeconds - elapsed);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” UI Refresh
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void RefreshUI()
    {
        if (config == null) return;

        bool isIdle       = CurrentState == PenState.Idle;
        bool isProcessing = CurrentState == PenState.Processing;
        bool isReady      = CurrentState == PenState.Ready;

        // Thá»©c Äƒn slot 1
        if (slot1Root != null)
        {
            slot1Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot1Icon, slot1Amount, config.food1ItemId, config.food1Icon);
        }

        // Thá»©c Äƒn slot 2
        if (slot2Root != null)
        {
            slot2Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot2Icon, slot2Amount, config.food2ItemId, config.food2Icon);
        }

        // Rá»• thu hoáº¡ch â€” sprite giá»¯ nguyÃªn tá»« prefab (khÃ´ng ghi Ä‘Ã¨ báº±ng config)
        if (basketRoot != null)
        {
            basketRoot.SetActive(true);
            if (basketActiveGlow != null)
                basketActiveGlow.SetActive(isReady);
        }

        // Progress overlay
        if (progressOverlay != null)
        {
            progressOverlay.SetActive(isProcessing);
            if (isProcessing)
            {
                float remaining = GetRemainingSeconds();
                if (progressFill != null)
                    progressFill.fillAmount = 1f - remaining / config.feedDurationSeconds;
                if (progressLabel != null)
                    progressLabel.text = FormatTime(remaining);
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
            amtText.text = "x" + amount;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” Harvest FX
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void PlayFeedVFX(string foodItemId, Vector3 vfxWorldPosition)
    {
        if (FarmCropVFXSpawner.Instance == null) return;

        Sprite foodIcon = foodItemId == config.food1ItemId
            ? config.food1Icon
            : config.food2Icon;

        FarmCropVFXSpawner.Instance.PlayItemDropVFX(foodIcon, vfxWorldPosition, 1);
    }

    private void SpawnHarvestFX(string itemId, Sprite icon, int amount, Vector3 vfxWorldPosition)
    {
        if (icon == null)
        {
            return;
        }
        HarvestFeedbackSpawner.Instance?.SpawnHarvestFly(icon, vfxWorldPosition, amount);
        FarmCropVFXSpawner.Instance?.PlayHarvestAmountVFX(amount, vfxWorldPosition);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” Click-outside Detection
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private bool IsPointerOverPanel(Vector2 screenPos)
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return false;
        Camera cam = Camera.main;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” Save / Load
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SaveState()
    {
        if (config == null) return;
        string id = config.penId;

        PlayerPrefs.SetInt(PrefKeyState + id, (int)CurrentState);
        PlayerPrefs.SetString(PrefKeyFood + id, activeFoodId ?? "");

        if (CurrentState == PenState.Processing)
            PlayerPrefs.SetString(PrefKeyStartTime + id, processStartUnix.ToString("R"));

        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        if (config == null) return;
        string id = config.penId;

        int stateInt = PlayerPrefs.GetInt(PrefKeyState + id, (int)PenState.Idle);
        CurrentState = (PenState)stateInt;
        activeFoodId = PlayerPrefs.GetString(PrefKeyFood + id, "");

        string startStr = PlayerPrefs.GetString(PrefKeyStartTime + id, "0");
        double.TryParse(startStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double startUnix);
        processStartUnix = (float)startUnix;

    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Helpers
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static double GetUnixNow() =>
        (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))
        .TotalSeconds;

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Gá»i khi báº¯t Ä‘áº§u nuÃ´i Ä‘á»ƒ ghi timestamp
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private void OnFeedStarted()
    {
        processStartUnix = (float)GetUnixNow();
    }
}
