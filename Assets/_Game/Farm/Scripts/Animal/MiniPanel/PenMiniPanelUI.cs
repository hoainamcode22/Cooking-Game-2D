using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini panel hiện cạnh chuồng khi click vào. Chạy trên World Space Canvas.
/// State machine: Idle → Processing → Ready → Idle.
/// Save/load tiến độ qua PlayerPrefs (timestamp thực tế).
/// </summary>
public class PenMiniPanelUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Enums & Constants
    // ─────────────────────────────────────────────────────────────

    public enum PenState { Idle, Processing, Ready }

    private const string PrefKeyState     = "PenState_";
    private const string PrefKeyFood      = "PenFood_";
    private const string PrefKeyStartTime = "PenStartTime_";

    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────

    [Header("Config — gán ScriptableObject đúng loại chuồng")]
    [SerializeField] private PenMiniPanelConfig config;

    [Header("Panel Root (World Space Canvas / Container)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Slot thức ăn 1")]
    [SerializeField] private GameObject slot1Root;
    [SerializeField] private Image      slot1Icon;
    [SerializeField] private TMP_Text   slot1Amount;

    [Header("Slot thức ăn 2")]
    [SerializeField] private GameObject slot2Root;
    [SerializeField] private Image      slot2Icon;
    [SerializeField] private TMP_Text   slot2Amount;

    [Header("Slot rổ thu hoạch")]
    [SerializeField] private GameObject basketRoot;
    [SerializeField] private Image      basketIcon;
    [SerializeField] private GameObject basketActiveGlow; // bật/tắt tùy Ready state

    [Header("Overlay tiến trình (hiện khi Processing)")]
    [SerializeField] private GameObject progressOverlay;
    [SerializeField] private Image      progressFill;     // fillAmount 0→1
    [SerializeField] private TMP_Text   progressLabel;    // "1:23"

    [Header("Collider để detect click-outside (BoxCollider2D trên panel)")]
    [SerializeField] private Collider2D panelCollider;

    // ─────────────────────────────────────────────────────────────
    //  Runtime State
    // ─────────────────────────────────────────────────────────────

    public PenState CurrentState { get; private set; } = PenState.Idle;

    private float   processStartUnix;   // Unix timestamp (giây) lúc bắt đầu nuôi
    private string  activeFoodId;       // feedItemId đang được nuôi
    private Coroutine timerCoroutine;

    // ─────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

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

        // Click outside → đóng panel
        bool clicked = Input.GetMouseButtonDown(0);
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            clicked = true;
#endif
        if (!clicked) return;

        Vector2 screenPos = Input.mousePosition;
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0)
            screenPos = Input.GetTouch(0).position;
#endif

        if (!IsPointerOverPanel(screenPos))
            ClosePanel();
    }

    // ─────────────────────────────────────────────────────────────
    //  Public API — gọi từ PenClickDetector và PenDropTarget
    // ─────────────────────────────────────────────────────────────

    public bool IsPanelOpen() => panelRoot != null && panelRoot.activeSelf;

    public void OpenPanel()
    {
        if (config == null)
        {
            Debug.LogError("[PenMiniPanelUI] config chưa gán!");
            return;
        }
        panelRoot.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        FarmInputLock.SuppressWorldClickForCurrentFrame();
    }

    /// <summary>
    /// PenDropTarget gọi khi user thả thức ăn vào collider chuồng.
    /// Trả về true nếu feed thành công.
    /// </summary>
    public bool TryFeed(string foodItemId)
    {
        if (CurrentState != PenState.Idle)
        {
            Debug.Log($"[PenMiniPanelUI] TryFeed bị từ chối — state={CurrentState}");
            return false;
        }

        if (foodItemId != config.food1ItemId && foodItemId != config.food2ItemId)
        {
            Debug.Log($"[PenMiniPanelUI] foodItemId '{foodItemId}' không khớp config");
            return false;
        }

        if (!FarmInventoryManager.Instance.HasItem(foodItemId, 1))
        {
            Debug.Log($"[PenMiniPanelUI] Không đủ {foodItemId} trong kho");
            return false;
        }

        FarmInventoryManager.Instance.RemoveItem(foodItemId, 1);
        activeFoodId = foodItemId;
        processStartUnix = (float)GetUnixNow(); // ghi timestamp trước khi save
        SetState(PenState.Processing);
        SaveState();

        StopTimerIfRunning();
        timerCoroutine = StartCoroutine(ProcessTimerCoroutine(config.feedDurationSeconds));
        return true;
    }

    /// <summary>
    /// PenDropTarget gọi khi user thả rổ vào collider chuồng.
    /// Trả về true nếu thu hoạch thành công.
    /// </summary>
    public bool TryHarvest()
    {
        if (CurrentState != PenState.Ready)
        {
            Debug.Log($"[PenMiniPanelUI] TryHarvest bị từ chối — state={CurrentState}");
            return false;
        }

        // Spawn sản phẩm chính
        SpawnHarvestFX(config.productItemId, config.productIcon);

        // Sản phẩm phụ (chỉ gà: egg)
        if (!string.IsNullOrEmpty(config.secondProductItemId))
            SpawnHarvestFX(config.secondProductItemId, config.secondProductIcon);

        // EXP
        if (HarvestFeedbackSpawner.Instance != null)
            HarvestFeedbackSpawner.Instance.SpawnExpFly(transform.position, config.expReward);

        // Cộng vào kho
        FarmInventoryManager.Instance.AddItem(config.productItemId, 1);
        if (!string.IsNullOrEmpty(config.secondProductItemId))
            FarmInventoryManager.Instance.AddItem(config.secondProductItemId, 1);

        activeFoodId = null;
        SetState(PenState.Idle);
        SaveState();
        RefreshUI();
        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Internal — State & Timer
    // ─────────────────────────────────────────────────────────────

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
        Debug.Log($"[PenMiniPanelUI] {config.penId} — Process xong, chuyển Ready");
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

    // ─────────────────────────────────────────────────────────────
    //  Internal — UI Refresh
    // ─────────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (config == null) return;

        bool isIdle       = CurrentState == PenState.Idle;
        bool isProcessing = CurrentState == PenState.Processing;
        bool isReady      = CurrentState == PenState.Ready;

        // Thức ăn slot 1
        if (slot1Root != null)
        {
            slot1Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot1Icon, slot1Amount, config.food1ItemId, config.food1Icon);
        }

        // Thức ăn slot 2
        if (slot2Root != null)
        {
            slot2Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot2Icon, slot2Amount, config.food2ItemId, config.food2Icon);
        }

        // Rổ thu hoạch
        if (basketRoot != null)
        {
            basketRoot.SetActive(true); // luôn hiển thị
            if (basketIcon != null && config.basketIcon != null)
                basketIcon.sprite = config.basketIcon;
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

    // ─────────────────────────────────────────────────────────────
    //  Internal — Harvest FX
    // ─────────────────────────────────────────────────────────────

    private void SpawnHarvestFX(string itemId, Sprite icon)
    {
        if (HarvestFeedbackSpawner.Instance == null) return;
        if (icon == null)
        {
            Debug.LogWarning($"[PenMiniPanelUI] SpawnHarvestFX: icon null cho itemId='{itemId}'");
            return;
        }
        HarvestFeedbackSpawner.Instance.SpawnHarvestFly(icon, transform.position, 1);
    }

    // ─────────────────────────────────────────────────────────────
    //  Internal — Click-outside Detection
    // ─────────────────────────────────────────────────────────────

    private bool IsPointerOverPanel(Vector2 screenPos)
    {
        if (panelCollider == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;
        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        return panelCollider.OverlapPoint(new Vector2(world.x, world.y));
    }

    // ─────────────────────────────────────────────────────────────
    //  Internal — Save / Load
    // ─────────────────────────────────────────────────────────────

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

        Debug.Log($"[PenMiniPanelUI] Loaded {id}: state={CurrentState} food='{activeFoodId}'");
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────

    private static double GetUnixNow() =>
        (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))
        .TotalSeconds;

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }

    // ─────────────────────────────────────────────────────────────
    //  Gọi khi bắt đầu nuôi để ghi timestamp
    // ─────────────────────────────────────────────────────────────
    private void OnFeedStarted()
    {
        processStartUnix = (float)GetUnixNow();
    }
}
