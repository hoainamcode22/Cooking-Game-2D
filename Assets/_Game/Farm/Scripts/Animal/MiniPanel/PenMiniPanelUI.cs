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
    [SerializeField] private GameObject basketRoot;
    [SerializeField] private Image      basketIcon;
    [SerializeField] private GameObject basketActiveGlow;

    [Header("Progress Overlay")]
    [SerializeField] private GameObject progressOverlay;
    [SerializeField] private Image      progressFill;
    [SerializeField] private TMP_Text   progressLabel;

    public PenState CurrentState { get; private set; } = PenState.Idle;

    private float   processStartUnix;
    private string  activeFoodId;
    private Coroutine timerCoroutine;
    private int     _openedAtFrame = -10;
    private bool    popupInputLockHeld;

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
        UpdateReadyBubble();
    }

    private void Update()
    {
        if (!IsPanelOpen()) return;

        if (FarmInputLock.IsDraggingSeed) return;

        if (Time.frameCount <= _openedAtFrame) return;

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
    }

    [Header("Title")]
    [SerializeField] private TMP_Text txtPenTitle;

    [Tooltip("Offset spawn FX")]
    [SerializeField] private float harvestSpawnUpOffset = 120f;

    private int SpeedUpGemCost =>
        CurrentState == PenState.Processing
            ? ConstructionManager.RushCostFor(GetRemainingSeconds())
            : 0;

    [Header("Sorting")]
    [SerializeField] private Canvas processOverlayCanvas;
    [SerializeField] private int processSortingOrder = 300;

    [Header("Tutorial")]
    [SerializeField] private Sprite gemButtonBgSprite;
    [SerializeField] private Sprite gemIconSprite;
    [SerializeField] private Sprite readyBubbleBgSprite;
    [SerializeField] private Vector2 readyBubbleLocalPos = new Vector2(0f, 320f);
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
        if (config == null) return;
        _openedAtFrame = Time.frameCount;
        if (panelRoot != null) panelRoot.SetActive(true);
        AcquirePopupInputBlock();
        RefreshUI();
        TutorialManager.Instance?.NotifyOpenPen();
    }

    public void ClosePanel()
    {
        ReleasePopupInputBlock();
        if (panelRoot != null) panelRoot.SetActive(false);
        FarmInputLock.SuppressWorldClickForCurrentFrame();
    }

    public void OnSlot1Clicked()
    {
        if (config == null || CurrentState != PenState.Idle) return;
        TryFeed(config.food1ItemId, transform.position);
    }

    public void OnSlot2Clicked()
    {
        if (config == null || CurrentState != PenState.Idle) return;
        TryFeed(config.food2ItemId, transform.position);
    }

    public void OnBasketClicked()
    {
        if (CurrentState != PenState.Ready) return;
        TryHarvest(transform.position);
    }

    public bool TryFeed(string foodItemId, Vector3 vfxWorldPosition)
    {
        if (CurrentState != PenState.Idle) return false;
        if (foodItemId != config.food1ItemId && foodItemId != config.food2ItemId) return false;

        int need = FoodNeeded;
        if (!FarmInventoryManager.Instance.HasItem(foodItemId, need))
        {
            FarmUIManager.Instance?.ShowHint($"Cần {need} phần thức ăn cho một lượt nuôi.");
            return false;
        }

        FarmInventoryManager.Instance.RemoveItem(foodItemId, need);
        MissionProgressTracker.ReportEvent(MissionEventType.FeedAnimal, foodItemId, need);
        PlayFeedVFX(foodItemId, vfxWorldPosition);
        AudioManager.Instance?.PlayPlanting();
        activeFoodId = foodItemId;
        processStartUnix = (float)GetUnixNow();
        SetState(PenState.Processing);
        SaveState();

        StopTimerIfRunning();
        timerCoroutine = StartCoroutine(ProcessTimerCoroutine(EffectiveFeedSeconds));

        if (IsPenTutorialStep("L2_08_FeedPen"))
            ClosePanel();

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
                    $"Kho đầy ({inv.UsedSlots}/{inv.SlotCapacity} slot) — bán bớt hoặc nâng cấp kho rồi thu hoạch.");
                return false;
            }
        }

        Vector3 productSpawn = vfxWorldPosition + Vector3.up * harvestSpawnUpOffset;
        int productAmount = Mathf.Max(1, config.productAmount);
        SpawnHarvestFX(config.productItemId, config.productIcon, productAmount, productSpawn);

        if (!string.IsNullOrEmpty(config.secondProductItemId))
            SpawnHarvestFX(config.secondProductItemId, config.secondProductIcon,
                Mathf.Max(1, config.secondProductAmount), productSpawn);

        if (HarvestFeedbackSpawner.Instance != null)
            HarvestFeedbackSpawner.Instance.SpawnExpFly(transform.position + Vector3.up * harvestSpawnUpOffset, config.expReward);

        AudioManager.Instance?.PlayHarvest();

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

    private void SetState(PenState newState)
    {
        CurrentState = newState;
        RefreshUI();
        UpdateReadyBubble();
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

    private int FoodNeeded =>
        config != null ? Mathf.Max(1, config.foodAmountPerFeed) : 1;

    private float EffectiveFeedSeconds =>
        config != null ? FarmManager.ScaleSeconds(config.feedDurationSeconds) : 1f;

    private float GetRemainingSeconds()
    {
        double startUnix = processStartUnix;
        double nowUnix   = GetUnixNow();
        float elapsed    = (float)(nowUnix - startUnix);
        return Mathf.Max(0f, EffectiveFeedSeconds - elapsed);
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
        }

        if (slot1Root != null)
        {
            slot1Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot1Icon, slot1Amount, config.food1ItemId, config.food1Icon);
        }

        if (slot2Root != null)
        {
            slot2Root.SetActive(isIdle);
            if (isIdle) RefreshFoodSlot(slot2Icon, slot2Amount, config.food2ItemId, config.food2Icon);
        }

        if (basketRoot != null)
        {
            basketRoot.SetActive(isReady);
            if (basketActiveGlow != null)
                basketActiveGlow.SetActive(isReady);
        }

        if (progressOverlay != null)
        {
            progressOverlay.SetActive(isProcessing);
            if (isProcessing)
            {
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
            amtText.text = $"{amount}/{FoodNeeded}";
            amtText.color = amount >= FoodNeeded ? new Color(1f, 0.97f, 0.84f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
        }
    }

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
        if (icon == null) return;
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

        if (CurrentState == PenState.Processing)
            PlayerPrefs.SetString(PrefKeyStartTime + id, processStartUnix.ToString("R"));

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

        string startStr = PlayerPrefs.GetString(PrefKeyStartTime + id, "0");
        double.TryParse(startStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double startUnix);
        processStartUnix = (float)startUnix;

        if (verCu < PenSaveVersion && coSaveCu && CurrentState == PenState.Processing)
        {
            if (!string.IsNullOrEmpty(activeFoodId) && FarmInventoryManager.Instance != null)
                FarmInventoryManager.Instance.AddItem(activeFoodId, FoodNeeded);

            CurrentState      = PenState.Idle;
            activeFoodId      = "";
            processStartUnix  = 0f;
            SaveState();
        }
    }

    private string GetPenDisplayName()
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

    private static double GetUnixNow() =>
        (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc))
        .TotalSeconds;

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m}:{s:D2}";
    }

    private GameObject _gemButtonGO;
    private TMP_Text   _gemCostText;
    private GameObject _readyBubble;
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
        t.text = "x" + SpeedUpGemCost;
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
        if (_readyBubble != null) _readyBubble.SetActive(CurrentState == PenState.Ready);
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
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = readyBubbleLocalPos;

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder    = readyBubbleSortingOrder;
        go.AddComponent<GraphicRaycaster>();

        var img = go.GetComponent<Image>();
        img.sprite = readyBubbleBgSprite != null ? readyBubbleBgSprite : GetRoundSprite();
        img.type = Image.Type.Sliced;
        img.color = readyBubbleBgSprite != null ? Color.white : new Color32(255, 246, 214, 255);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => TryHarvest(transform.position));

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

        _readyBubble = go;
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
