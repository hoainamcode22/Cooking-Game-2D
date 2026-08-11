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

    // B4 — họ save + phiên bản cho ba khoá chuồng ở trên (mỗi chuồng một hậu tố `penId`).
    // Ba khoá ghi thẳng số/chuỗi nên dấu phiên bản nằm ở khoá phụ `SAVE_VER_PEN_STATE`.
    //
    // v1 = thời gian chuồng tính bằng GIÂY THẬT, đi qua `FarmManager.ScaleSeconds`.
    // VÌ SAO PHẢI CÓ: `PenStartTime_*` là MỐC THỜI GIAN unix, còn thời lượng thì suy ra từ
    // `config.feedDurationSeconds` LÚC ĐỌC. Bảng D2 vừa đổi 30s → 90..480s. Người chơi bấm
    // cho ăn ở bản cũ rồi cập nhật game sẽ thấy lượt đang chạy bỗng dài thêm gấp 3-16 lần,
    // không thu được. Nhánh migrate cắt lượt đang chạy về Idle và ghi rõ trong log.
    private const string PenSaveFamily  = "PEN_STATE";
    private const int    PenSaveVersion = 1;

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
        UpdateReadyBubble();   // hiện bubble nếu nạp lại lúc đang Ready
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

    [Tooltip("Kéo thịt/trứng/EXP spawn LÊN CAO (world units) — tránh nằm sát panel thức ăn (sorting C)")]
    [SerializeField] private float harvestSpawnUpOffset = 120f;

    /// <summary>
    /// F9 — số kim cương để hoàn tất NGAY, tính theo THỜI GIAN CÒN LẠI chứ không phải
    /// số cứng 1 như trước.
    ///
    /// VÌ SAO: sau E1 chuồng chạy 90–480 giây. Cứng 1 gem thì cách chơi tối ưu là cho ăn
    /// rồi bấm gem ngay, và toàn bộ bảng thời gian D2 vô nghĩa. Dùng ĐÚNG công thức của
    /// `ConstructionManager` để cả game chỉ có một thang giá rush: ceil(15 + 0.82·√giây).
    /// </summary>
    private int SpeedUpGemCost =>
        CurrentState == PenState.Processing
            ? ConstructionManager.RushCostFor(GetRemainingSeconds())
            : 0;

    [Header("Sorting (C) — process thấp hơn vật phẩm")]
    [Tooltip("(C) Canvas của process overlay — GÁN để ép sortingOrder THẤP hơn vật phẩm chuồng. Trống = chỉ dùng sibling order.")]
    [SerializeField] private Canvas processOverlayCanvas;
    [SerializeField] private int processSortingOrder = -10;

    [Header("Tutorial — nút Gem (process) + bubble 'sẵn sàng' (tự dựng nếu trống, gán ảnh sau)")]
    [Tooltip("Nền nút kim cương — để trống dùng bo góc tự vẽ.")]
    [SerializeField] private Sprite gemButtonBgSprite;
    [Tooltip("Icon kim cương — để trống dùng hình thoi tự vẽ.")]
    [SerializeField] private Sprite gemIconSprite;
    [Tooltip("Nền bubble sẵn sàng — để trống dùng bo góc tự vẽ.")]
    [SerializeField] private Sprite readyBubbleBgSprite;
    [Tooltip("Vị trí bubble 'sẵn sàng' so với chuồng (local) — đặt CAO trên đầu con vật.")]
    [SerializeField] private Vector2 readyBubbleLocalPos = new Vector2(0f, 420f);
    [Tooltip("Sorting order của bubble — cao hơn chuồng & con vật để luôn nổi trên cùng.")]
    [SerializeField] private int readyBubbleSortingOrder = 1300;

    public bool IsPanelOpen() => panelRoot != null && panelRoot.activeSelf;
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
        if (config == null)
        {
            return;
        }
        _openedAtFrame = Time.frameCount; // Ä‘Ã¡nh dáº¥u frame má»Ÿ Ä‘á»ƒ Update bá» qua
        panelRoot.SetActive(true);
        RefreshUI();
        TutorialManager.Instance?.NotifyOpenPen();   // tutorial L2: bước "mở chuồng"
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

        // E1 — một lượt nuôi tốn `foodAmountPerFeed` đơn vị, không còn cứng 1.
        int need = FoodNeeded;

        if (!FarmInventoryManager.Instance.HasItem(foodItemId, need))
        {
            // Nói rõ vì sao không cho ăn, thay vì im lặng như bản cũ — người chơi kéo
            // thức ăn vào mà không có phản hồi gì sẽ tưởng chuồng bị lỗi.
            FarmUIManager.Instance?.ShowHint($"Cần {need} phần thức ăn cho một lượt nuôi.");
            return false;
        }

        FarmInventoryManager.Instance.RemoveItem(foodItemId, need);
        MissionProgressTracker.ReportEvent(MissionEventType.FeedAnimal, foodItemId, need);
        PlayFeedVFX(foodItemId, vfxWorldPosition);
        activeFoodId = foodItemId;
        processStartUnix = (float)GetUnixNow(); // ghi timestamp trÆ°á»›c khi save
        SetState(PenState.Processing);
        SaveState();

        StopTimerIfRunning();
        timerCoroutine = StartCoroutine(ProcessTimerCoroutine(EffectiveFeedSeconds));

        if (IsPenTutorialStep("L2_08_FeedPen"))
            ClosePanel();

        TutorialManager.Instance?.NotifyFeed();   // tutorial L2: đã cho ăn
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

        // F8 — kho có sức chứa THẬT. Kiểm CẢ HAI sản phẩm trước khi đổi state: nếu thu
        // hoạch xong rồi kho từ chối thì thịt/trứng bốc hơi mà chuồng đã về Idle, người
        // chơi mất trắng một lượt nuôi kèm thức ăn đã tốn. Thà giữ chuồng ở Ready.
        var inv = FarmInventoryManager.Instance;
        if (inv != null)
        {
            bool fit = inv.CanAddItem(config.productItemId)
                    && (string.IsNullOrEmpty(config.secondProductItemId) || inv.CanAddItem(config.secondProductItemId));

            if (!fit)
            {
                FarmUIManager.Instance?.ShowHint(
                    $"Kho đầy ({inv.UsedSlots}/{inv.SlotCapacity} slot) — bán bớt hoặc nâng cấp kho rồi thu hoạch.");
                return false;
            }
        }

        // Spawn sáº£n pháº©m chÃ­nh
        Vector3 productSpawn = vfxWorldPosition + Vector3.up * harvestSpawnUpOffset; // kéo lên cao, tránh panel thức ăn (C)
        int productAmount = Mathf.Max(1, config.productAmount);
        SpawnHarvestFX(config.productItemId, config.productIcon, productAmount, productSpawn);

        // Sáº£n pháº©m phá»¥ (chá»‰ gÃ : egg)
        if (!string.IsNullOrEmpty(config.secondProductItemId))
            SpawnHarvestFX(config.secondProductItemId, config.secondProductIcon,
                Mathf.Max(1, config.secondProductAmount), productSpawn);

        // EXP
        if (HarvestFeedbackSpawner.Instance != null)
            HarvestFeedbackSpawner.Instance.SpawnExpFly(transform.position + Vector3.up * harvestSpawnUpOffset, config.expReward);

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
        TutorialManager.Instance?.NotifyPenHarvest();   // tutorial L2: đã thu hoạch chuồng
        return true;
    }

    /// <summary>Dùng kim cương hoàn tất NGAY quá trình nuôi.
    /// Gắn vào nút Gem trên pen panel: OnClick → PenMiniPanelUI.TrySpeedUpGem.
    /// • Trong game thường: bấm gem = hoàn tất + THU HOẠCH gia súc luôn (về Idle).
    /// • Trong tutorial chuồng (L2_09/L2_10): chỉ chuyển Ready để bước "kéo rổ" còn dạy được.</summary>
    public bool TrySpeedUpGem()
    {
        if (CurrentState != PenState.Processing) return false;
        if (FarmEconomyManager.Instance == null) return false;
        int gemCost = SpeedUpGemCost;
        if (FarmEconomyManager.Instance.Gems < gemCost)
        {
            FarmUIManager.Instance?.ShowHint($"Cần {gemCost} kim cương để hoàn tất ngay.");
            return false;
        }
        if (!FarmEconomyManager.Instance.SpendGems(gemCost)) return false;

        StopTimerIfRunning();
        SetState(PenState.Ready);
        SaveState();

        bool penTutorialActive = IsPenTutorialActive();
        if (penTutorialActive)
            ClosePanel();

        TutorialManager.Instance?.NotifyPenSpeedUp();   // tutorial L2: đã dùng gem hoàn tất

        // Ngoài tutorial chuồng → thu hoạch ngay (không cần kéo rổ).
        if (!penTutorialActive)
            TryHarvest(transform.position);

        return true;
    }

    /// <summary>Đang ở bước tutorial chuồng (gem-speedup / kéo rổ)? Khi đó GIỮ luồng dạy:
    /// gem chỉ chuyển Ready, để bước L2_10 dạy kéo rổ thu hoạch.</summary>
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

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    //  Internal â€” State & Timer
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SetState(PenState newState)
    {
        CurrentState = newState;
        RefreshUI();
        UpdateReadyBubble();   // bubble 'sẵn sàng' bật/tắt theo state (kể cả khi panel đóng)
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
            // Giá gem tụt dần cùng đồng hồ (F9) — không cập nhật thì nhãn đứng ở số cũ
            if (_gemCostText  != null) _gemCostText.text = "x" + ConstructionManager.RushCostFor(remaining);

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

    /// <summary>
    /// Số phần thức ăn cho MỘT lượt nuôi (E1). Config cũ chưa có field này thì
    /// `foodAmountPerFeed` = 0 khi đọc từ YAML → Max(1, …) để không bao giờ ra 0 phần.
    /// </summary>
    private int FoodNeeded => config != null ? Mathf.Max(1, config.foodAmountPerFeed) : 1;

    /// <summary>
    /// Thời gian nuôi sau khi quy đổi qua `FarmManager.realTimeMultiplier`.
    ///
    /// VÌ SAO: trước đây chuồng dùng thẳng `feedDurationSeconds` còn cây trồng thì nhân
    /// hệ số → hạ multiplier để test thì ruộng nhanh gấp 3 mà chuồng vẫn 30 giây, hai hệ
    /// đo thời gian bằng hai đơn vị. Với multiplier = 1.0 (mặc định) con số không đổi.
    /// </summary>
    private float EffectiveFeedSeconds =>
        config != null ? FarmManager.ScaleSeconds(config.feedDurationSeconds) : 1f;

    private float GetRemainingSeconds()
    {
        double startUnix = processStartUnix;
        double nowUnix   = GetUnixNow();
        float elapsed    = (float)(nowUnix - startUnix);
        return Mathf.Max(0f, EffectiveFeedSeconds - elapsed);
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

        // Rá»• thu hoạch chỉ hiện khi đã sẵn sàng, nhường chỗ cho nút gem lúc đang Processing.
        if (basketRoot != null)
        {
            basketRoot.SetActive(isReady);
            if (basketActiveGlow != null)
                basketActiveGlow.SetActive(isReady);
        }

        // Progress overlay
        if (progressOverlay != null)
        {
            progressOverlay.SetActive(isProcessing);
            if (isProcessing)
            {
                progressOverlay.transform.SetAsFirstSibling();   // (C) render dưới vật phẩm cùng panel
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
            }
        }

        // Nút Gem: dựng 1 lần (kể cả khi chưa gán progressOverlay), chỉ hiện khi đang Processing.
        EnsureGemButton();
        PlaceGemButton();
        if (_gemButtonGO != null)
        {
            _gemButtonGO.SetActive(isProcessing);
            if (isProcessing)
            {
                _gemButtonGO.transform.SetAsLastSibling();
                // F9: giá phụ thuộc thời gian còn lại → vẽ lại mỗi lần mở panel,
                // không chỉ lúc coroutine đang chạy.
                if (_gemCostText != null) _gemCostText.text = "x" + SpeedUpGemCost;
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

        // Hiện "đang có / cần" thay vì chỉ "xN": từ E1 một lượt nuôi tốn 2-3 phần, người
        // chơi phải thấy được vì sao kéo thức ăn vào mà chuồng không nhận.
        if (amtText != null)
            amtText.text = $"{amount}/{FoodNeeded}";
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

        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    private void LoadState()
    {
        if (config == null) return;
        string id = config.penId;

        // B4 — đóng dấu phiên bản trước khi đọc. Dùng `id` của CHÍNH chuồng này để dò
        // "đã có save cũ chưa", nhưng dấu phiên bản là CHUNG cho cả họ PEN_STATE: chuồng
        // nào nạp trước thì đóng dấu, các chuồng sau thấy version đã đúng nên không migrate lại.
        bool coSaveCu = PlayerPrefs.HasKey(PrefKeyState + id);
        int verCu = SaveVersionGuard.Ensure(PenSaveFamily, PenSaveVersion, null, coSaveCu);

        int stateInt = PlayerPrefs.GetInt(PrefKeyState + id, (int)PenState.Idle);
        CurrentState = (PenState)stateInt;
        activeFoodId = PlayerPrefs.GetString(PrefKeyFood + id, "");

        string startStr = PlayerPrefs.GetString(PrefKeyStartTime + id, "0");
        double.TryParse(startStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double startUnix);
        processStartUnix = (float)startUnix;

        // v0 → v1: cắt lượt đang chạy. Không cắt thì người chơi đang có lượt 30 giây của
        // bản cũ bị đo lại bằng thời lượng mới (90..480 giây) ⇒ đồng hồ nhảy vọt lên, và
        // với chuồng bò sữa là chờ thêm 4 phút rưỡi cho một lượt họ tưởng sắp xong.
        // Trả về Idle là mất công cho ăn của họ, nên HOÀN thức ăn lại vào kho.
        if (verCu < PenSaveVersion && coSaveCu && CurrentState == PenState.Processing)
        {
            Debug.LogWarning($"[Chuồng {id}] Save v{verCu}: bảng thời gian đã đổi (D2) nên lượt " +
                             $"đang chạy không còn đo được đúng — trả về Idle và hoàn thức ăn.");

            if (!string.IsNullOrEmpty(activeFoodId) && FarmInventoryManager.Instance != null)
                FarmInventoryManager.Instance.AddItem(activeFoodId, FoodNeeded);

            CurrentState      = PenState.Idle;
            activeFoodId      = "";
            processStartUnix  = 0f;
            SaveState();
        }

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

    // =========================================================================
    //  Tutorial L2 — Nút Gem (trên ô process) + Bubble "sẵn sàng thu hoạch"
    // =========================================================================

    private GameObject _gemButtonGO;
    private TMP_Text   _gemCostText;
    private GameObject _readyBubble;
    private static Sprite _roundSprite;
    private static Sprite _diamondSprite;

    /// <summary>Dựng nút kim cương trên ô process (nếu chưa có). Đặt tên 'btn_PenGem' để tutorial
    /// (tutorial_pen_gem) chỉ tay vào được. OnClick → TrySpeedUpGem (hoàn tất ngay).</summary>
    private void EnsureGemButton()
    {
        // Gắn trên panelRoot để không bị progressOverlay clipping/sorting che mất.
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

            // Nút đã có sẵn trong prefab/scene → vẫn phải bắt lấy nhãn giá, nếu không
            // thì F9 hiện số cũ mãi (nhãn chỉ được cập nhật qua _gemCostText).
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

        // icon kim cương (placeholder hình thoi — gán ảnh thật sau)
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
        t.text = "x" + SpeedUpGemCost;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = 80;
        t.raycastTarget = false;
        _gemCostText = t;   // giữ lại để RefreshUI cập nhật giá theo thời gian còn lại

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

    /// <summary>Bật/tắt bubble 'sẵn sàng thu hoạch' theo state (Ready = hiện).</summary>
    private void UpdateReadyBubble()
    {
        if (config == null) return;
        EnsureReadyBubble();
        if (_readyBubble != null) _readyBubble.SetActive(CurrentState == PenState.Ready);
    }

    /// <summary>Bubble nổi trên chuồng (kiểu đơn hàng dân làng) hiện icon sản phẩm khi process xong.
    /// Gà: thịt + trứng; Heo/Bò: thịt; Bò sữa: sữa — tất cả lấy theo config từng chuồng. Tự dựng 1 lần.</summary>
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
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = readyBubbleLocalPos;

        // Canvas riêng overrideSorting → bubble luôn NỔI TRÊN cùng (trên chuồng + con vật).
        var bubbleCanvas = go.AddComponent<Canvas>();
        bubbleCanvas.overrideSorting = true;
        bubbleCanvas.sortingLayerName = "Foreground";
        bubbleCanvas.sortingOrder = readyBubbleSortingOrder;

        var bg = go.GetComponent<Image>();
        bg.sprite = readyBubbleBgSprite != null ? readyBubbleBgSprite : GetRoundSprite();
        bg.type = Image.Type.Sliced;
        bg.color = readyBubbleBgSprite != null ? Color.white : new Color32(255, 252, 240, 245);
        bg.raycastTarget = false;

        float iconSize = refSize.y * 0.8f;
        AddBubbleIcon(rt, config.productIcon, two ? new Vector2(-w * 0.24f, 6f) : new Vector2(0f, 6f), iconSize);
        if (two) AddBubbleIcon(rt, config.secondProductIcon, new Vector2(w * 0.24f, 6f), iconSize);

        // Đuôi bubble chỉ xuống chuồng (placeholder hình vuông xoay 45°).
        var tail = new GameObject("Tail", typeof(RectTransform), typeof(Image));
        tail.transform.SetParent(rt, false);
        var tailRt = (RectTransform)tail.transform;
        tailRt.sizeDelta = new Vector2(refSize.x * 0.32f, refSize.x * 0.32f);
        tailRt.anchoredPosition = new Vector2(0f, -h * 0.5f);
        tailRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var tailImg = tail.GetComponent<Image>();
        tailImg.sprite = GetRoundSprite();
        tailImg.color = bg.color;
        tailImg.raycastTarget = false;

        _readyBubble = go;
        _readyBubble.SetActive(false);
    }

    private static void AddBubbleIcon(RectTransform parent, Sprite icon, Vector2 pos, float size)
    {
        var go = new GameObject("Img_Product", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;
        var img = go.GetComponent<Image>();
        img.sprite = icon;
        img.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0f);   // chưa có icon → ẩn
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    private Vector2 ReferenceSlotSize()
    {
        GameObject r = slot1Root != null ? slot1Root : (basketRoot != null ? basketRoot : panelRoot);
        if (r != null)
        {
            var rt = r.GetComponent<RectTransform>();
            if (rt != null && rt.rect.width > 1f) return rt.rect.size;
        }
        return new Vector2(120f, 120f);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform c in parent)
        {
            if (c.name == childName) return c;
            Transform found = FindDeepChild(c, childName);
            if (found != null) return found;
        }
        return null;
    }

    private static Sprite GetRoundSprite()
    {
        if (_roundSprite != null) return _roundSprite;
        const int s = 48; const int rad = 14;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x < rad ? rad - x : x >= s - rad ? x - (s - rad - 1) : 0f;
                float dy = y < rad ? rad - y : y >= s - rad ? y - (s - rad - 1) : 0f;
                bool inside = (dx <= 0f && dy <= 0f) || dx * dx + dy * dy <= (float)rad * rad;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        tex.Apply();
        _roundSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(rad, rad, rad, rad));
        _roundSprite.hideFlags = HideFlags.HideAndDontSave;
        return _roundSprite;
    }

    private static Sprite GetDiamondSprite()
    {
        if (_diamondSprite != null) return _diamondSprite;
        const int s = 48;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        float c = (s - 1) * 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = Mathf.Abs(x - c) / c, dy = Mathf.Abs(y - c) / c;
                tex.SetPixel(x, y, (dx + dy) <= 1f ? Color.white : Color.clear);   // hình thoi
            }
        tex.Apply();
        _diamondSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        _diamondSprite.hideFlags = HideFlags.HideAndDontSave;
        return _diamondSprite;
    }
}
