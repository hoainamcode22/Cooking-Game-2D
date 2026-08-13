using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AvatarProfilePopupUI : MonoBehaviour
{
    public static AvatarProfilePopupUI Instance { get; private set; }
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private const string PrefName = "PLAYER_PROFILE_NAME";
    private const string PrefAvatarIndex = "PLAYER_PROFILE_AVATAR_INDEX";
    private const string PrefWarehouseLevel = "PLAYER_PROFILE_WAREHOUSE_LEVEL";
    private const string PrefAchievementCount = "PLAYER_PROFILE_ACHIEVEMENT_COUNT";

    // B4 — họ save + phiên bản cho 4 khoá hồ sơ ở trên. Bốn khoá này ghi thẳng số/chuỗi
    // nên dấu phiên bản nằm ở khoá phụ `SAVE_VER_PLAYER_PROFILE`.
    //
    // v1 = tên + chỉ số avatar + cấp kho + "điểm nấu ăn" như hiện tại.
    // TĂNG SỐ NÀY nếu đổi số lượng avatar (chỉ số cũ vượt mảng mới → phải kẹp lại) hoặc
    // đổi ý nghĩa `PLAYER_PROFILE_WAREHOUSE_LEVEL` (nó đang là BẢN SAO của `WAREHOUSE_LEVEL`
    // dùng để hiện lên hồ sơ — hai khoá cùng nội dung, xem 6.A phần rủi ro).
    private const string SaveFamily  = "PLAYER_PROFILE";
    private const int    SaveVersion = 1;

    /// <summary>
    /// Đóng dấu phiên bản + kẹp lại dữ liệu vượt biên. Gọi từ mọi cửa ĐỌC static bên dưới:
    /// popup này có thể chưa được mở lần nào mà `AddAchievementCount()` đã bị gọi từ
    /// `UnifiedTaskPopupUI`, nên không thể chỉ đóng dấu trong `Awake`.
    /// </summary>
    private static void EnsureProfileSaveVersion()
    {
        if (_profileVersionChecked) return;
        _profileVersionChecked = true;

        bool coSaveCu = PlayerPrefs.HasKey(PrefName)
                        || PlayerPrefs.HasKey(PrefAvatarIndex)
                        || PlayerPrefs.HasKey(PrefWarehouseLevel)
                        || PlayerPrefs.HasKey(PrefAchievementCount);

        SaveVersionGuard.Ensure(SaveFamily, SaveVersion, null, coSaveCu);
    }

    private static bool _profileVersionChecked;

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Avatar")]
    [SerializeField] private Image mainAvatarImage;
    [SerializeField] private Button mainAvatarButton;
    [SerializeField] private Image outsideAvatarImage;
    [SerializeField] private TMP_Text txtLevelBadge;
    [SerializeField] private GameObject avatarChoicesRoot;
    [SerializeField] private Sprite[] avatarSprites = new Sprite[6];
    [SerializeField] private Button[] avatarButtons = new Button[6];
    [SerializeField] private Image[] avatarButtonImages = new Image[6];

    [Header("Profile")]
    [SerializeField] private TMP_InputField inputPlayerName;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtLevelRange;
    [SerializeField] private TMP_Text txtExpValue;
    [SerializeField] private Image expFill;

    [Header("Stats")]
    [SerializeField] private GameObject legacyStatsRoot;
    [SerializeField] private GameObject profileCardsRoot;
    [SerializeField] private TMP_Text txtProfileWarehouseLevel;
    [SerializeField] private TMP_Text txtProfileAchievementCount;
    [SerializeField] private TMP_Text txtFarmLevel;
    [SerializeField] private TMP_Text txtWarehouseLevel;
    [SerializeField] private TMP_Text txtHarvestCount;
    [SerializeField] private TMP_Text txtAchievementCount;

    [Header("Fallback Stats")]
    [SerializeField] private int defaultWarehouseLevel = 1;
    [SerializeField] private int defaultHarvestCount;
    [SerializeField] private int defaultAchievementCount;

    private bool popupInputLockHeld;
    private bool started;
    private bool avatarChoicesVisible;
    private bool isRefreshingName;
    private Sprite[] cachedAvatarChoiceSprites;

    public static event Action OnProfileStatsChanged;

    public static AvatarProfilePopupUI FindOrCreate(Image outsideAvatar)
    {
        AvatarProfilePopupUI existing = FindFirstObjectByType<AvatarProfilePopupUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.SetOutsideAvatar(outsideAvatar);
            return existing;
        }

        Transform canvasPopup = FindCanvasPopup();
        if (canvasPopup == null)
        {
            Debug.LogError("[AvatarProfilePopupUI] Cannot find Canvas_Popup.");
            return null;
        }

        AvatarProfilePopupUI created = CreateHierarchy(canvasPopup);
        created.SetOutsideAvatar(outsideAvatar);
        return created;
    }

    public void SetOutsideAvatar(Image image)
    {
        outsideAvatarImage = image;

        if (outsideAvatarImage != null && mainAvatarImage != null && mainAvatarImage.sprite == null)
            mainAvatarImage.sprite = outsideAvatarImage.sprite;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AutoWire();
        BindButtons();

        if (popupRoot == null)
            popupRoot = gameObject;

        popupRoot.SetActive(false);
    }

    private void Start()
    {
        started = true;
        SubscribeProgress();
        RefreshAll();
    }

    private void OnEnable()
    {
        BindButtons();
        SubscribeProgress();

        if (started)
            RefreshAll();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
        UnsubscribeProgress();
    }

    private void OnDestroy()
    {
        ReleasePopupInputBlock();
        UnsubscribeProgress();
    }

    public void OpenPopup()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        popupRoot.SetActive(true);
        AcquirePopupInputBlock();
        SetAvatarChoicesVisible(false);
        RefreshAll();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void RefreshAll()
    {
        RefreshName();
        RefreshProgress();
        RefreshStats();
        RefreshAvatarSelection();
        RefreshAvatarChoiceVisibility();
    }

    private void RefreshName()
    {
        if (inputPlayerName == null)
            return;

        EnsureProfileSaveVersion();
        string savedName = PlayerPrefs.GetString(PrefName, "Nong Dan Vui Ve");
        isRefreshingName = true;
        inputPlayerName.SetTextWithoutNotify(savedName);
        isRefreshingName = false;
    }

    private void RefreshProgress()
    {
        int level = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
        int currentExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.CurrentExp : 0;
        int requiredExp = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.RequiredExpCurrentLevel : 40;

        SetText(txtLevel, $"Cấp độ : {level}");
        SetText(txtLevelBadge, level.ToString());
        // Trần cấp thật của game là CapToiDa (30) — chuỗi "1 - 50" cũ ghi cứng là
        // thông tin sai cho người chơi từ khi chốt trần 30.
        SetText(txtLevelRange, $"Cấp độ 1 - {PlayerProgressManager.CapToiDa}");
        SetText(txtExpValue, requiredExp <= 0 ? "MAX" : $"{currentExp} / {requiredExp}");

        if (expFill != null)
        {
            expFill.type = Image.Type.Filled;
            expFill.fillMethod = Image.FillMethod.Horizontal;
            expFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            expFill.fillAmount = requiredExp <= 0 ? 1f : Mathf.Clamp01((float)currentExp / requiredExp);
        }
    }

    private void RefreshStats()
    {
        int warehouseLevel = GetSavedWarehouseLevel(defaultWarehouseLevel);
        int achievementCount = GetSavedAchievementCount(defaultAchievementCount);

        SetText(txtProfileWarehouseLevel, "");
        SetText(txtProfileAchievementCount, "");

        // Keep the user's hand-designed labels alive in Play Mode.
        SetText(txtFarmLevel, "");
        SetText(txtWarehouseLevel, $"Kho Cấp {warehouseLevel}");
        SetText(txtHarvestCount, "");
        SetText(txtAchievementCount, $"Điểm nấu ăn {achievementCount}");
    }

    public static int GetSavedWarehouseLevel(int fallback = 1)
    {
        EnsureProfileSaveVersion();
        return Mathf.Max(1, PlayerPrefs.GetInt(PrefWarehouseLevel, Mathf.Max(1, fallback)));
    }

    public static int GetSavedAchievementCount(int fallback = 0)
    {
        EnsureProfileSaveVersion();
        return Mathf.Max(0, PlayerPrefs.GetInt(PrefAchievementCount, Mathf.Max(0, fallback)));
    }

    public static void SetWarehouseLevel(int level)
    {
        PlayerPrefs.SetInt(PrefWarehouseLevel, Mathf.Max(1, level));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        OnProfileStatsChanged?.Invoke();
    }

    public static void SetAchievementCount(int count)
    {
        PlayerPrefs.SetInt(PrefAchievementCount, Mathf.Max(0, count));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        OnProfileStatsChanged?.Invoke();
    }

    public static void AddAchievementCount(int amount = 1)
    {
        if (amount <= 0)
            return;

        SetAchievementCount(GetSavedAchievementCount() + amount);
    }

    private void RefreshAvatarSelection()
    {
        CacheAvatarChoiceSpritesFromScene();

        Sprite currentSprite = GetCurrentAvatarSprite();

        if (mainAvatarImage != null)
            mainAvatarImage.sprite = currentSprite;

        if (outsideAvatarImage != null)
            outsideAvatarImage.sprite = currentSprite;

        for (int i = 0; i < avatarButtonImages.Length; i++)
        {
            if (avatarButtonImages[i] == null)
                continue;

            Sprite sprite = GetAvatarSprite(i);
            if (sprite != null)
                avatarButtonImages[i].sprite = sprite;

            avatarButtonImages[i].enabled = sprite != null;
        }
    }

    private Sprite GetCurrentAvatarSprite()
    {
        EnsureProfileSaveVersion();
        // Kẹp chỉ số: save cũ có thể giữ index của một bộ avatar NHIỀU HƠN bộ hiện tại.
        int index = Mathf.Clamp(PlayerPrefs.GetInt(PrefAvatarIndex, 0), 0, Mathf.Max(0, avatarSprites.Length - 1));
        Sprite selected = GetAvatarSprite(index);

        if (selected != null)
            return selected;

        return outsideAvatarImage != null ? outsideAvatarImage.sprite : null;
    }

    private Sprite GetAvatarSprite(int index)
    {
        if (avatarSprites != null && index >= 0 && index < avatarSprites.Length && avatarSprites[index] != null)
            return avatarSprites[index];

        if (cachedAvatarChoiceSprites != null && index >= 0 && index < cachedAvatarChoiceSprites.Length && cachedAvatarChoiceSprites[index] != null)
            return cachedAvatarChoiceSprites[index];

        if (avatarButtonImages != null && index >= 0 && index < avatarButtonImages.Length && avatarButtonImages[index] != null)
            return avatarButtonImages[index].sprite;

        return null;
    }

    private void SelectAvatar(int index)
    {
        PlayerPrefs.SetInt(PrefAvatarIndex, index);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        RefreshAvatarSelection();
        SetAvatarChoicesVisible(false);
    }

    private void SavePlayerName(string value)
    {
        string cleanName = string.IsNullOrWhiteSpace(value) ? "Nong Dan Vui Ve" : value.Trim();
        PlayerPrefs.SetString(PrefName, cleanName);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs

        if (inputPlayerName != null && inputPlayerName.text != cleanName)
            inputPlayerName.SetTextWithoutNotify(cleanName);
    }

    private void SavePlayerNameLive(string value)
    {
        if (isRefreshingName)
            return;

        PlayerPrefs.SetString(PrefName, string.IsNullOrEmpty(value) ? "Nong Dan Vui Ve" : value);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    private void BindButtons()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(ClosePopup);
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (inputPlayerName != null)
        {
            inputPlayerName.onEndEdit.RemoveListener(SavePlayerName);
            inputPlayerName.onValueChanged.RemoveListener(SavePlayerNameLive);
            inputPlayerName.onEndEdit.AddListener(SavePlayerName);
            inputPlayerName.onValueChanged.AddListener(SavePlayerNameLive);
        }

        if (mainAvatarButton != null)
        {
            mainAvatarButton.onClick.RemoveListener(ToggleAvatarChoices);
            mainAvatarButton.onClick.AddListener(ToggleAvatarChoices);
        }

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            if (avatarButtons[i] == null)
                continue;

            int index = i;
            avatarButtons[i].onClick.RemoveAllListeners();
            avatarButtons[i].onClick.AddListener(() => SelectAvatar(index));
        }
    }

    private void SubscribeProgress()
    {
        OnProfileStatsChanged -= HandleProfileStatsChanged;
        OnProfileStatsChanged += HandleProfileStatsChanged;

        if (PlayerProgressManager.Instance == null)
            return;

        PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged -= HandleExpChanged;
        PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged += HandleExpChanged;
    }

    private void UnsubscribeProgress()
    {
        OnProfileStatsChanged -= HandleProfileStatsChanged;

        if (PlayerProgressManager.Instance == null)
            return;

        PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
        PlayerProgressManager.Instance.OnExpChanged -= HandleExpChanged;
    }

    private void HandleLevelChanged(int level)
    {
        RefreshProgress();
        RefreshStats();
    }

    private void HandleExpChanged(int currentExp, int requiredExp)
    {
        RefreshProgress();
    }

    private void HandleProfileStatsChanged()
    {
        RefreshStats();
    }

    private void AcquirePopupInputBlock()
    {
        if (popupRoot == null)
            return;

        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        if (popupRoot != null)
            FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    private void AutoWire()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (btnClose == null)
            btnClose = FindChildComponent<Button>(transform, "btn_close");

        if (mainAvatarImage == null)
            mainAvatarImage = FindChildComponent<Image>(transform, "Img_MainAvatar");

        if (mainAvatarImage != null)
        {
            mainAvatarImage.raycastTarget = true;

            if (mainAvatarButton == null)
                mainAvatarButton = mainAvatarImage.GetComponent<Button>();

            if (mainAvatarButton == null)
                mainAvatarButton = mainAvatarImage.gameObject.AddComponent<Button>();

            mainAvatarButton.transition = Selectable.Transition.None;
            mainAvatarButton.targetGraphic = mainAvatarImage;
        }

        if (avatarChoicesRoot == null)
        {
            Transform choices = FindDeepChild(transform, "Panel_AvatarChoices");
            if (choices != null)
                avatarChoicesRoot = choices.gameObject;
        }

        if (inputPlayerName == null)
            inputPlayerName = FindChildComponent<TMP_InputField>(transform, "Input_PlayerName");

        if (txtLevel == null)
            txtLevel = FindChildComponent<TMP_Text>(transform, "Txt_Level");

        if (txtLevelBadge == null)
            txtLevelBadge = FindChildComponent<TMP_Text>(transform, "Txt_LevelBadge");

        if (txtLevelRange == null)
            txtLevelRange = FindChildComponent<TMP_Text>(transform, "Txt_LevelRange");

        if (txtExpValue == null)
            txtExpValue = FindChildComponent<TMP_Text>(transform, "Txt_ExpValue");

        if (expFill == null)
            expFill = FindChildComponent<Image>(transform, "Img_ExpFill");

        if (txtFarmLevel == null)
            txtFarmLevel = FindChildComponent<TMP_Text>(transform, "Txt_FarmLevel");

        if (txtWarehouseLevel == null)
            txtWarehouseLevel = FindChildComponent<TMP_Text>(transform, "Txt_WarehouseLevel");

        if (txtHarvestCount == null)
            txtHarvestCount = FindChildComponent<TMP_Text>(transform, "Txt_HarvestCount");

        if (txtAchievementCount == null)
            txtAchievementCount = FindChildComponent<TMP_Text>(transform, "Txt_AchievementCount");

        if (legacyStatsRoot == null)
        {
            Transform stats = FindDeepChild(transform, "Panel_Stats");
            if (stats != null)
                legacyStatsRoot = stats.gameObject;
        }

        EnsureProfileCards();
        ConfigureInputField();
        CacheAvatarChoiceSpritesFromScene();
    }

    private void ToggleAvatarChoices()
    {
        SetAvatarChoicesVisible(!avatarChoicesVisible);
    }

    private void SetAvatarChoicesVisible(bool visible)
    {
        avatarChoicesVisible = visible;
        RefreshAvatarChoiceVisibility();
    }

    private void CacheAvatarChoiceSpritesFromScene()
    {
        if (avatarButtonImages == null || avatarButtonImages.Length == 0)
            return;

        if (cachedAvatarChoiceSprites == null || cachedAvatarChoiceSprites.Length != avatarButtonImages.Length)
            cachedAvatarChoiceSprites = new Sprite[avatarButtonImages.Length];

        for (int i = 0; i < avatarButtonImages.Length; i++)
        {
            Sprite sprite = null;

            if (avatarSprites != null && i < avatarSprites.Length)
                sprite = avatarSprites[i];

            if (sprite == null && avatarButtonImages[i] != null)
                sprite = avatarButtonImages[i].sprite;

            if (sprite != null)
                cachedAvatarChoiceSprites[i] = sprite;
        }
    }

    private void ConfigureInputField()
    {
        if (inputPlayerName == null)
            return;

        inputPlayerName.interactable = true;
        inputPlayerName.readOnly = false;
        inputPlayerName.lineType = TMP_InputField.LineType.SingleLine;
        if (inputPlayerName.characterLimit <= 0)
            inputPlayerName.characterLimit = 18;

        if (inputPlayerName.targetGraphic == null)
            inputPlayerName.targetGraphic = inputPlayerName.GetComponent<Image>();
    }

    private void RefreshAvatarChoiceVisibility()
    {
        if (avatarChoicesRoot != null)
            avatarChoicesRoot.SetActive(avatarChoicesVisible);

        if (profileCardsRoot != null)
            profileCardsRoot.SetActive(!avatarChoicesVisible);

        if (legacyStatsRoot != null)
            legacyStatsRoot.SetActive(!avatarChoicesVisible);
    }

    private void EnsureProfileCards()
    {
        if (profileCardsRoot == null)
        {
            Transform existing = FindDeepChild(transform, "Panel_ProfileCards");
            if (existing != null)
                profileCardsRoot = existing.gameObject;
        }

        if (profileCardsRoot == null)
        {
            Transform rightInfo = FindDeepChild(transform, "Panel_RightInfo");
            if (rightInfo == null)
                return;

            profileCardsRoot = CreateProfileCards(rightInfo);
        }
        else
        {
            EnsureProfileCardsContent(profileCardsRoot.transform);
        }

        if (txtProfileWarehouseLevel == null)
            txtProfileWarehouseLevel = FindChildComponent<TMP_Text>(profileCardsRoot.transform, "Txt_ProfileWarehouseLevel");

        if (txtProfileAchievementCount == null)
            txtProfileAchievementCount = FindChildComponent<TMP_Text>(profileCardsRoot.transform, "Txt_ProfileAchievementCount");
    }

    private static void EnsureProfileCardsContent(Transform root)
    {
        if (root == null)
            return;

        Transform warehouseCard = root.Find("Card_WarehouseLevel");
        if (warehouseCard == null)
        {
            RectTransform card = CreateRect(root, "Card_WarehouseLevel", new Vector2(300f, 84f), new Vector2(-165f, 0f));
            AddImage(card.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.62f), true);
            warehouseCard = card;
        }

        if (FindDeepChild(warehouseCard, "Txt_ProfileWarehouseLevel") == null)
            CreateText(warehouseCard, "Txt_ProfileWarehouseLevel", "", 26f, new Color(0.28f, 0.13f, 0.04f, 1f), Vector2.zero, new Vector2(280f, 72f));

        Transform achievementCard = root.Find("Card_AchievementCount");
        if (achievementCard == null)
        {
            RectTransform card = CreateRect(root, "Card_AchievementCount", new Vector2(300f, 84f), new Vector2(165f, 0f));
            AddImage(card.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.62f), true);
            achievementCard = card;
        }

        if (FindDeepChild(achievementCard, "Txt_ProfileAchievementCount") == null)
            CreateText(achievementCard, "Txt_ProfileAchievementCount", "", 26f, new Color(0.28f, 0.13f, 0.04f, 1f), Vector2.zero, new Vector2(280f, 72f));
    }

    private static GameObject CreateProfileCards(Transform parent)
    {
        RectTransform root = CreateRect(parent, "Panel_ProfileCards", new Vector2(640f, 92f), new Vector2(-15f, -76f));
        root.SetAsLastSibling();

        RectTransform warehouseCard = CreateRect(root, "Card_WarehouseLevel", new Vector2(300f, 84f), new Vector2(-165f, 0f));
        AddImage(warehouseCard.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.62f), true);
        CreateText(warehouseCard, "Txt_ProfileWarehouseLevel", "", 26f, new Color(0.28f, 0.13f, 0.04f, 1f), Vector2.zero, new Vector2(280f, 72f));

        RectTransform achievementCard = CreateRect(root, "Card_AchievementCount", new Vector2(300f, 84f), new Vector2(165f, 0f));
        AddImage(achievementCard.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.62f), true);
        CreateText(achievementCard, "Txt_ProfileAchievementCount", "", 26f, new Color(0.28f, 0.13f, 0.04f, 1f), Vector2.zero, new Vector2(280f, 72f));

        return root.gameObject;
    }

    private static AvatarProfilePopupUI CreateHierarchy(Transform parent)
    {
        RectTransform root = CreateRect(parent, "Popup_AvatarProfile", new Vector2(1250f, 430f), Vector2.zero);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);

        Image rootImage = AddImage(root.gameObject, new Color(0.1f, 0.08f, 0.06f, 0.35f), true);
        rootImage.raycastTarget = true;
        root.gameObject.AddComponent<CanvasGroup>();

        RectTransform panel = CreateRect(root, "Panel_ProfileFrame", new Vector2(1180f, 380f), Vector2.zero);
        AddImage(panel.gameObject, new Color(0.93f, 0.78f, 0.52f, 0.95f), true);

        RectTransform close = CreateRect(panel, "btn_close", new Vector2(84f, 84f), new Vector2(565f, 170f));
        AddImage(close.gameObject, new Color(0.65f, 0.25f, 0.12f, 1f), true);
        Button closeButton = close.gameObject.AddComponent<Button>();
        CreateText(close, "Txt_Close", "X", 48f, Color.white, Vector2.zero, new Vector2(80f, 80f));

        RectTransform left = CreateRect(panel, "Panel_LeftAvatar", new Vector2(310f, 310f), new Vector2(-400f, 0f));
        AddImage(left.gameObject, new Color(0.48f, 0.27f, 0.12f, 0.35f), true);

        RectTransform avatarFrame = CreateRect(left, "Img_AvatarFrame", new Vector2(280f, 280f), Vector2.zero);
        AddImage(avatarFrame.gameObject, new Color(1f, 0.86f, 0.45f, 0.4f), false);

        RectTransform avatar = CreateRect(left, "Img_MainAvatar", new Vector2(230f, 230f), new Vector2(0f, 8f));
        Image mainAvatar = AddImage(avatar.gameObject, new Color(1f, 1f, 1f, 0.85f), false);
        mainAvatar.preserveAspect = true;

        RectTransform badge = CreateRect(left, "Img_LevelBadge", new Vector2(110f, 95f), new Vector2(-95f, -130f));
        AddImage(badge.gameObject, new Color(0.08f, 0.42f, 1f, 0.95f), true);
        TMP_Text badgeLevel = CreateText(badge, "Txt_LevelBadge", "1", 38f, Color.white, Vector2.zero, new Vector2(100f, 70f));

        RectTransform right = CreateRect(panel, "Panel_RightInfo", new Vector2(760f, 310f), new Vector2(180f, 0f));
        AddImage(right.gameObject, new Color(1f, 0.92f, 0.72f, 0.45f), true);

        TMP_InputField input = CreateInput(right, "Input_PlayerName", new Vector2(420f, 58f), new Vector2(-40f, 122f));
        CreateText(right, "Txt_Level", "Cấp độ : 1", 32f, new Color(0.28f, 0.13f, 0.04f, 1f), new Vector2(-250f, 62f), new Vector2(240f, 46f));
        CreateText(right, "Txt_LevelRange", $"Cấp độ 1 - {PlayerProgressManager.CapToiDa}", 24f, new Color(0.36f, 0.2f, 0.08f, 1f), new Vector2(185f, 62f), new Vector2(250f, 42f));

        RectTransform expBar = CreateRect(right, "Panel_ExpBar", new Vector2(650f, 46f), new Vector2(-10f, 10f));
        AddImage(expBar.gameObject, new Color(0.38f, 0.18f, 0.06f, 1f), true);
        RectTransform expFill = CreateRect(expBar, "Img_ExpFill", new Vector2(650f, 46f), Vector2.zero);
        expFill.anchorMin = new Vector2(0f, 0f);
        expFill.anchorMax = new Vector2(1f, 1f);
        expFill.offsetMin = new Vector2(6f, 6f);
        expFill.offsetMax = new Vector2(-6f, -6f);
        Image expFillImage = AddImage(expFill.gameObject, new Color(1f, 0.62f, 0.06f, 1f), false);
        expFillImage.type = Image.Type.Filled;
        expFillImage.fillMethod = Image.FillMethod.Horizontal;
        expFillImage.fillAmount = 0.25f;
        TMP_Text expValue = CreateText(expBar, "Txt_ExpValue", "0 / 40", 26f, Color.white, Vector2.zero, new Vector2(620f, 40f));

        RectTransform choicePanel = CreateRect(right, "Panel_AvatarChoices", new Vector2(640f, 86f), new Vector2(-15f, -76f));
        AddImage(choicePanel.gameObject, new Color(0.32f, 0.32f, 0.32f, 0.75f), true);

        Button[] buttons = new Button[6];
        Image[] buttonImages = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            RectTransform slot = CreateRect(choicePanel, $"Slot_Avatar_{i + 1:00}", new Vector2(78f, 78f), new Vector2(-255f + i * 102f, 0f));
            AddImage(slot.gameObject, new Color(1f, 0.88f, 0.58f, 1f), true);
            buttons[i] = slot.gameObject.AddComponent<Button>();

            RectTransform icon = CreateRect(slot, "Img_AvatarChoice", new Vector2(64f, 64f), Vector2.zero);
            buttonImages[i] = AddImage(icon.gameObject, new Color(1f, 1f, 1f, 0.95f), false);
            buttonImages[i].preserveAspect = true;
        }

        RectTransform stats = CreateRect(right, "Panel_Stats", new Vector2(650f, 72f), new Vector2(-10f, -145f));
        AddImage(stats.gameObject, new Color(0.96f, 0.78f, 0.48f, 0.55f), true);
        TMP_Text farmLevel = CreateText(stats, "Txt_FarmLevel", "", 21f, new Color(0.28f, 0.13f, 0.04f, 1f), new Vector2(-240f, 0f), new Vector2(145f, 66f));
        TMP_Text warehouseLevel = CreateText(stats, "Txt_WarehouseLevel", "Kho Cấp 1", 21f, new Color(0.28f, 0.13f, 0.04f, 1f), new Vector2(-80f, 0f), new Vector2(130f, 66f));
        TMP_Text harvestCount = CreateText(stats, "Txt_HarvestCount", "Thu Hoach\n0", 21f, new Color(0.28f, 0.13f, 0.04f, 1f), new Vector2(80f, 0f), new Vector2(140f, 66f));
        TMP_Text achievementCount = CreateText(stats, "Txt_AchievementCount", "Điểm nấu ăn 0", 21f, new Color(0.28f, 0.13f, 0.04f, 1f), new Vector2(240f, 0f), new Vector2(145f, 66f));

        AvatarProfilePopupUI ui = root.gameObject.AddComponent<AvatarProfilePopupUI>();
        ui.popupRoot = root.gameObject;
        ui.btnClose = closeButton;
        ui.mainAvatarImage = mainAvatar;
        ui.txtLevelBadge = badgeLevel;
        ui.avatarButtons = buttons;
        ui.avatarButtonImages = buttonImages;
        ui.inputPlayerName = input;
        ui.txtLevel = FindText(right, "Txt_Level");
        ui.txtLevelRange = FindText(right, "Txt_LevelRange");
        ui.txtExpValue = expValue;
        ui.expFill = expFillImage;
        ui.txtFarmLevel = farmLevel;
        ui.txtWarehouseLevel = warehouseLevel;
        ui.txtHarvestCount = harvestCount;
        ui.txtAchievementCount = achievementCount;

        if (badgeLevel != null)
            badgeLevel.name = "Txt_LevelBadge";

        root.gameObject.SetActive(false);
        return ui;
    }

    private static Transform FindCanvasPopup()
    {
        GameObject canvasPopup = GameObject.Find("Canvas_Popup");
        if (canvasPopup != null)
            return canvasPopup.transform;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        return canvas != null ? canvas.transform : null;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image AddImage(GameObject go, Color color, bool raycastTarget)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color, Vector2 position, Vector2 size)
    {
        RectTransform rt = CreateRect(parent, name, size, position);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, Vector2 size, Vector2 position)
    {
        RectTransform root = CreateRect(parent, name, size, position);
        AddImage(root.gameObject, new Color(1f, 0.86f, 0.58f, 1f), true);

        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();

        RectTransform textArea = CreateRect(root, "Text Area", size - new Vector2(24f, 10f), Vector2.zero);
        RectTransform placeholder = CreateRect(textArea, "Placeholder", size - new Vector2(36f, 12f), Vector2.zero);
        TextMeshProUGUI placeholderText = placeholder.gameObject.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Nong Dan Vui Ve";
        placeholderText.fontSize = 28f;
        placeholderText.fontStyle = FontStyles.Bold;
        placeholderText.alignment = TextAlignmentOptions.Center;
        placeholderText.color = new Color(0.35f, 0.2f, 0.08f, 0.45f);
        placeholderText.raycastTarget = false;

        RectTransform text = CreateRect(textArea, "Text", size - new Vector2(36f, 12f), Vector2.zero);
        TextMeshProUGUI inputText = text.gameObject.AddComponent<TextMeshProUGUI>();
        inputText.text = "";
        inputText.fontSize = 28f;
        inputText.fontStyle = FontStyles.Bold;
        inputText.alignment = TextAlignmentOptions.Center;
        inputText.color = new Color(0.28f, 0.13f, 0.04f, 1f);
        inputText.raycastTarget = false;

        input.textViewport = textArea;
        input.textComponent = inputText;
        input.placeholder = placeholderText;
        input.characterLimit = 18;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static TMP_Text FindText(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindDeepChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }
}
