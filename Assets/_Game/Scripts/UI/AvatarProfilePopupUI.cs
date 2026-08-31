using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POPUP HỒ SƠ & AVATAR NGƯỜI CHƠI (AvatarProfilePopupUI).
/// Thiết kế 2 cột chuẩn visual: Bảng gỗ · Giấy kem · Juicy (đồng bộ Nhiệm vụ, Kho & Cửa hàng).
/// </summary>
public class AvatarProfilePopupUI : MonoBehaviour
{
    public static AvatarProfilePopupUI Instance { get; private set; }
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private const string PrefName = "PLAYER_PROFILE_NAME";
    private const string PrefAvatarIndex = "PLAYER_PROFILE_AVATAR_INDEX";
    private const string PrefWarehouseLevel = "PLAYER_PROFILE_WAREHOUSE_LEVEL";
    private const string PrefAchievementCount = "PLAYER_PROFILE_ACHIEVEMENT_COUNT";

    private const string SaveFamily  = "PLAYER_PROFILE";
    private const int    SaveVersion = 1;

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
    [SerializeField] private Sprite[] avatarSprites = new Sprite[8];
    [SerializeField] private Button[] avatarButtons = new Button[8];
    [SerializeField] private Image[] avatarButtonImages = new Image[8];
    [SerializeField] private GameObject[] avatarSelectionHighlights = new GameObject[8];

    [Header("Profile")]
    [SerializeField] private TMP_InputField inputPlayerName;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtLevelRange;
    [SerializeField] private TMP_Text txtExpValue;
    [SerializeField] private Image expFill;

    [Header("Stats")]
    [SerializeField] private TMP_Text txtWarehouseLevel;
    [SerializeField] private TMP_Text txtCookingScore;
    [SerializeField] private TMP_Text txtGoldEarned;
    [SerializeField] private TMP_Text txtAchievementCount;

    [Header("Save Button")]
    [SerializeField] private Button btnSaveProfile;

    [Header("Fallback Stats")]
    [SerializeField] private int defaultWarehouseLevel = 1;
    [SerializeField] private int defaultHarvestCount;
    [SerializeField] private int defaultAchievementCount;

    private bool popupInputLockHeld;
    private bool started;
    private bool isRefreshingName;
    private int currentSelectedIndex = 0;

    public static event Action OnProfileStatsChanged;
    public static event Action<int> OnAvatarSelected;

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
        if (outsideAvatarImage != null)
        {
            Sprite sel = GetCurrentSelectedAvatar();
            if (sel != null)
                outsideAvatarImage.sprite = sel;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        LoadAllAvatars();
        BuildOrFormatUI();
        BindButtons();

        if (popupRoot == null) popupRoot = gameObject;
        popupRoot.SetActive(false);
    }

    private void Start()
    {
        started = true;
        if (outsideAvatarImage == null && FarmGame.UI.TownshipHUDController.Instance != null)
        {
            outsideAvatarImage = FarmGame.UI.TownshipHUDController.Instance.imgAvatar;
        }
        SubscribeProgress();
        RefreshAll();
    }

    private void OnEnable()
    {
        BindButtons();
        SubscribeProgress();
        if (started) RefreshAll();
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
        if (popupRoot == null) popupRoot = gameObject;
        popupRoot.SetActive(true);
        AcquirePopupInputBlock();
        AudioManager.Instance?.PlayUIClick();

        if (outsideAvatarImage == null && FarmGame.UI.TownshipHUDController.Instance != null)
        {
            outsideAvatarImage = FarmGame.UI.TownshipHUDController.Instance.imgAvatar;
        }

        RefreshAll();
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        AudioManager.Instance?.PlayUIClick();
        if (popupRoot != null) popupRoot.SetActive(false);
    }

    private void SaveAndClose()
    {
        if (inputPlayerName != null)
        {
            SavePlayerName(inputPlayerName.text);
        }
        AudioManager.Instance?.PlayUIClick();
        ClosePopup();
    }

    public void RefreshAll()
    {
        LoadAllAvatars();
        RefreshName();
        RefreshProgress();
        RefreshStats();
        RefreshAvatarSelection();
    }

    private void LoadAllAvatars()
    {
        if (avatarSprites == null || avatarSprites.Length < 8)
            avatarSprites = new Sprite[8];

        for (int i = 0; i < 8; i++)
        {
            if (avatarSprites[i] == null)
            {
                avatarSprites[i] = Resources.Load<Sprite>($"Avatars/avatar_npc_{i}");
            }
        }
    }

    private void SubscribeProgress()
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged += OnLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged += OnExpChanged;
        }
    }

    private void UnsubscribeProgress()
    {
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged -= OnLevelChanged;
            PlayerProgressManager.Instance.OnExpChanged -= OnExpChanged;
        }
    }

    private void OnLevelChanged(int level) => RefreshProgress();
    private void OnExpChanged(int cur, int req) => RefreshProgress();

    private void RefreshName()
    {
        EnsureProfileSaveVersion();
        string name = PlayerPrefs.GetString(PrefName, "Nông Dân Vui Vẻ");
        if (inputPlayerName != null)
        {
            isRefreshingName = true;
            inputPlayerName.text = name;
            isRefreshingName = false;
        }
    }

    private void RefreshProgress()
    {
        int level = 1;
        int expCurrent = 0;
        int expRequired = 100;

        if (PlayerProgressManager.Instance != null)
        {
            level = PlayerProgressManager.Instance.Level;
            expCurrent = PlayerProgressManager.Instance.CurrentExp;
            expRequired = PlayerProgressManager.Instance.RequiredExpForLevel(level);
        }

        if (txtLevel != null) txtLevel.text = $"Cấp độ {level}";
        if (txtLevelRange != null) txtLevelRange.text = $"Cấp 1 – {PlayerProgressManager.CapToiDa}";
        if (txtLevelBadge != null) txtLevelBadge.text = level.ToString();

        if (expFill != null)
        {
            float fill = expRequired > 0 ? Mathf.Clamp01((float)expCurrent / expRequired) : 0f;
            expFill.fillAmount = fill;
        }

        if (txtExpValue != null)
        {
            txtExpValue.text = $"{expCurrent:N0} / {expRequired:N0} EXP".Replace(",", " ");
        }
    }

    private void RefreshStats()
    {
        EnsureProfileSaveVersion();

        // 1. Kho
        int whLv = defaultWarehouseLevel;
        if (FarmInventoryManager.Instance != null)
        {
            whLv = FarmInventoryManager.Instance.SlotCapacity;
        }
        else
        {
            whLv = PlayerPrefs.GetInt(PrefWarehouseLevel, defaultWarehouseLevel);
        }
        if (txtWarehouseLevel != null) txtWarehouseLevel.text = $"{whLv} ô";

        // 2. Điểm nấu ăn
        int cookCount = PlayerPrefs.GetInt("COOKING_CHALLENGE_TOTAL_DISHES", 0);
        if (cookCount <= 0) cookCount = PlayerPrefs.GetInt("COOKING_TOTAL_DISHES_MADE", 0);
        if (txtCookingScore != null) txtCookingScore.text = $"{cookCount} món";

        // 3. Tiền vàng kiếm được
        int gold = 0;
        if (FarmEconomyManager.Instance != null)
        {
            gold = FarmEconomyManager.Instance.Gold;
        }
        else
        {
            gold = PlayerPrefs.GetInt("FARM_ECONOMY_GOLD", 0);
        }
        if (txtGoldEarned != null) txtGoldEarned.text = gold.ToString("N0", new System.Globalization.CultureInfo("vi-VN")).Replace(",", " ");

        // 4. Thành tựu
        int ach = PlayerPrefs.GetInt(PrefAchievementCount, defaultAchievementCount);
        if (ach <= 0) ach = PlayerPrefs.GetInt("COMPLETED_MISSION_COUNT", 0);
        if (txtAchievementCount != null) txtAchievementCount.text = $"{ach} đã xong";

        OnProfileStatsChanged?.Invoke();
    }

    public static void AddAchievementCount(int amount = 1)
    {
        EnsureProfileSaveVersion();
        int cur = PlayerPrefs.GetInt(PrefAchievementCount, 0) + amount;
        PlayerPrefs.SetInt(PrefAchievementCount, cur);
        LuuGopPrefs.Hen();
        if (Instance != null && Instance.txtAchievementCount != null)
        {
            Instance.txtAchievementCount.text = $"{cur} đã xong";
        }
    }

    private void RefreshAvatarSelection()
    {
        EnsureProfileSaveVersion();
        currentSelectedIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefAvatarIndex, 0), 0, Mathf.Max(0, avatarSprites.Length - 1));

        Sprite selected = GetAvatarSprite(currentSelectedIndex);
        if (mainAvatarImage != null && selected != null)
        {
            mainAvatarImage.sprite = selected;
        }

        if (outsideAvatarImage != null && selected != null)
        {
            outsideAvatarImage.sprite = selected;
        }

        for (int i = 0; i < 8; i++)
        {
            if (avatarButtonImages != null && i < avatarButtonImages.Length && avatarButtonImages[i] != null)
            {
                Sprite s = GetAvatarSprite(i);
                if (s != null) avatarButtonImages[i].sprite = s;
            }

            if (avatarSelectionHighlights != null && i < avatarSelectionHighlights.Length && avatarSelectionHighlights[i] != null)
            {
                avatarSelectionHighlights[i].SetActive(i == currentSelectedIndex);
            }
        }
    }

    public Sprite GetCurrentSelectedAvatar()
    {
        EnsureProfileSaveVersion();
        int index = Mathf.Clamp(PlayerPrefs.GetInt(PrefAvatarIndex, 0), 0, Mathf.Max(0, avatarSprites.Length - 1));
        Sprite selected = GetAvatarSprite(index);
        if (selected != null) return selected;
        return outsideAvatarImage != null ? outsideAvatarImage.sprite : null;
    }

    private Sprite GetAvatarSprite(int index)
    {
        if (avatarSprites != null && index >= 0 && index < avatarSprites.Length && avatarSprites[index] != null)
            return avatarSprites[index];
        return Resources.Load<Sprite>($"Avatars/avatar_npc_{index}");
    }

    private void SelectAvatar(int index)
    {
        AudioManager.Instance?.PlayUIClick();
        PlayerPrefs.SetInt(PrefAvatarIndex, index);
        LuuGopPrefs.Hen();
        currentSelectedIndex = index;
        RefreshAvatarSelection();

        Sprite spr = GetAvatarSprite(index);
        if (mainAvatarImage != null) mainAvatarImage.sprite = spr;
        if (outsideAvatarImage != null) outsideAvatarImage.sprite = spr;

        OnAvatarSelected?.Invoke(index);
        if (FarmGame.UI.TownshipHUDController.Instance != null)
        {
            FarmGame.UI.TownshipHUDController.Instance.RefreshAvatar(index);
        }
    }

    private void SavePlayerName(string value)
    {
        string cleanName = string.IsNullOrWhiteSpace(value) ? "Nông Dân Vui Vẻ" : value.Trim();
        PlayerPrefs.SetString(PrefName, cleanName);
        LuuGopPrefs.Hen();

        if (inputPlayerName != null && inputPlayerName.text != cleanName)
            inputPlayerName.SetTextWithoutNotify(cleanName);
    }

    private void SavePlayerNameLive(string value)
    {
        if (isRefreshingName) return;
        PlayerPrefs.SetString(PrefName, string.IsNullOrEmpty(value) ? "Nông Dân Vui Vẻ" : value);
        LuuGopPrefs.Hen();
    }

    private void BindButtons()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(ClosePopup);
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (btnSaveProfile != null)
        {
            btnSaveProfile.onClick.RemoveListener(SaveAndClose);
            btnSaveProfile.onClick.AddListener(SaveAndClose);
        }

        if (inputPlayerName != null)
        {
            inputPlayerName.onEndEdit.RemoveListener(SavePlayerName);
            inputPlayerName.onValueChanged.RemoveListener(SavePlayerNameLive);
            inputPlayerName.onEndEdit.AddListener(SavePlayerName);
            inputPlayerName.onValueChanged.AddListener(SavePlayerNameLive);
        }

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            if (avatarButtons[i] == null) continue;
            int index = i;
            avatarButtons[i].onClick.RemoveAllListeners();
            avatarButtons[i].onClick.AddListener(() => SelectAvatar(index));
        }
    }

    private void AcquirePopupInputBlock()
    {
        if (popupRoot != null) FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        if (popupRoot != null) FarmInputLock.SetPopupRaycastBlock(popupRoot, false);
        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  UI BUILDER — Tạo và làm mới giao diện 2 cột theo đúng Mockup
    // ═════════════════════════════════════════════════════════════════════════

    private void BuildOrFormatUI()
    {
        // 1. Xoá mọi script HoSoSkin cũ để không bị ghi đè giao diện
        var oldSkins = GetComponents<HoSoSkin>();
        for (int i = 0; i < oldSkins.Length; i++)
        {
            DestroyImmediate(oldSkins[i]);
        }

        // 2. Thiết lập root thành Dim Overlay toàn màn hình đen mờ
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        Image rootImg = GetComponent<Image>();
        if (rootImg == null) rootImg = gameObject.AddComponent<Image>();
        rootImg.sprite = null;
        rootImg.color = new Color(0f, 0f, 0f, 0.65f);
        rootImg.raycastTarget = true;

        // 3. Kiểm tra xem đã có cấu trúc Board_Wooden chưa
        Transform mainBoard = transform.Find("Board_Wooden");
        if (mainBoard != null)
        {
            // Dọn các con rác cũ khác ngoài Board_Wooden
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name != "Board_Wooden") DestroyImmediate(c.gameObject);
            }
            AutoWireNewHierarchy(mainBoard);
            return;
        }

        // Dọn dẹp con cũ nếu có cấu trúc legacy
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        CreateFreshHierarchy(this, transform);
    }

    private void AutoWireNewHierarchy(Transform board)
    {
        popupRoot = gameObject;
        btnClose = FindChildComponent<Button>(board, "Btn_Close");
        mainAvatarImage = FindChildComponent<Image>(board, "Img_MainAvatar");
        txtLevelBadge = FindChildComponent<TMP_Text>(board, "Txt_BadgeLevel");
        inputPlayerName = FindChildComponent<TMP_InputField>(board, "Input_FarmName");
        txtLevel = FindChildComponent<TMP_Text>(board, "Txt_LevelTitle");
        txtLevelRange = FindChildComponent<TMP_Text>(board, "Txt_LevelRange");
        txtExpValue = FindChildComponent<TMP_Text>(board, "Txt_ExpValue");
        expFill = FindChildComponent<Image>(board, "Img_ExpFill");
        txtWarehouseLevel = FindChildComponent<TMP_Text>(board, "Txt_WarehouseVal");
        txtCookingScore = FindChildComponent<TMP_Text>(board, "Txt_CookingVal");
        txtGoldEarned = FindChildComponent<TMP_Text>(board, "Txt_GoldVal");
        txtAchievementCount = FindChildComponent<TMP_Text>(board, "Txt_AchievementVal");
        btnSaveProfile = FindChildComponent<Button>(board, "Btn_SaveProfile");

        Transform grid = FindDeepChild(board, "Grid_AvatarChoices");
        if (grid != null)
        {
            avatarButtons = new Button[8];
            avatarButtonImages = new Image[8];
            avatarSelectionHighlights = new GameObject[8];

            for (int i = 0; i < 8; i++)
            {
                Transform slot = grid.Find($"Slot_{i}");
                if (slot != null)
                {
                    avatarButtons[i] = slot.GetComponent<Button>();
                    Transform icon = slot.Find("Img_Icon");
                    if (icon == null) icon = FindDeepChild(slot, "Img_Icon");
                    if (icon != null) avatarButtonImages[i] = icon.GetComponent<Image>();
                    
                    Transform hl = slot.Find("Selection_Indicator");
                    if (hl == null) hl = slot.Find("Selection_Ring");
                    if (hl != null) avatarSelectionHighlights[i] = hl.gameObject;
                }
            }
        }
    }

    public static AvatarProfilePopupUI CreateHierarchy(Transform parent)
    {
        RectTransform root = CreateRect(parent, "Popup_AvatarProfile", new Vector2(1000f, 640f), Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);

        Image dim = AddImage(root.gameObject, new Color(0f, 0f, 0f, 0.65f), true);

        CanvasGroup cg = root.gameObject.AddComponent<CanvasGroup>();
        AvatarProfilePopupUI ui = root.gameObject.AddComponent<AvatarProfilePopupUI>();

        CreateFreshHierarchy(ui, root);
        root.gameObject.SetActive(false);
        return ui;
    }

    private static void CreateFreshHierarchy(AvatarProfilePopupUI ui, Transform root)
    {
        // 1. Khung ván gỗ ngoài (1000 x 640)
        RectTransform board = CreateRect(root, "Board_Wooden", new Vector2(1000f, 640f), Vector2.zero);
        AddImage(board.gameObject, TaskPopupDesign.VanGoVien, BoGoc(38f), true);
        RectTransform boardFill = CreateRect(board, "Fill", new Vector2(986f, 626f), Vector2.zero);
        AddImage(boardFill.gameObject, TaskPopupDesign.VanGoDuoi, BoGoc(34f), true);
        PhuGradient(board, "Gradient", TaskPopupDesign.VanGoTren, Vector2.zero, new Vector2(986f, 626f), 34f);

        // 2. Ruy băng tiêu đề "HỒ SƠ" (400 x 96)
        RectTransform ribbon = CreateRect(board, "Ribbon_Header", new Vector2(400f, 96f), new Vector2(0f, 320f));
        AddImage(ribbon.gameObject, TaskPopupDesign.RibbonVien, BoGoc(22f), true);
        RectTransform ribbonFill = CreateRect(ribbon, "Fill", new Vector2(390f, 86f), Vector2.zero);
        AddImage(ribbonFill.gameObject, TaskPopupDesign.RibbonDuoi, BoGoc(18f), true);
        PhuGradient(ribbon, "Gradient", TaskPopupDesign.RibbonTren, Vector2.zero, new Vector2(390f, 86f), 18f);

        TMP_Text titleTxt = CreateText(ribbon, "Txt_Title", "HỒ SƠ", 42, TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Center, Vector2.zero, new Vector2(380f, 70f), FontStyles.Bold);
        AddShadow(titleTxt.gameObject, TaskPopupDesign.VienChuTieuDe, new Vector2(2f, -3f));

        // 3. Nút đóng [X] đỏ (78 x 78) chuẩn asset btnX của Kho, Chợ, Cửa hàng
        Sprite btnCloseSpr = Resources.Load<Sprite>("Icons/btnX");
#if UNITY_EDITOR
        if (btnCloseSpr == null)
            btnCloseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/btnX.png");
#endif
        RectTransform closeRt = CreateRect(board, "Btn_Close", new Vector2(78f, 78f), new Vector2(476f, 296f));
        Image closeImg = AddImage(closeRt.gameObject, Color.white, btnCloseSpr, true);
        closeImg.preserveAspect = true;
        Button btnClose = closeRt.gameObject.AddComponent<Button>();

        // 4. Tấm giấy kem bên trong (928 x 534)
        RectTransform parchment = CreateRect(board, "Panel_Parchment", new Vector2(928f, 534f), new Vector2(0f, -24f));
        AddImage(parchment.gameObject, TaskPopupDesign.GiayVien, BoGoc(22f), true);
        RectTransform paperFill = CreateRect(parchment, "Fill", new Vector2(920f, 526f), Vector2.zero);
        AddImage(paperFill.gameObject, TaskPopupDesign.GiayDuoi, BoGoc(18f), true);
        PhuGradient(parchment, "Gradient", TaskPopupDesign.GiayTren, Vector2.zero, new Vector2(920f, 526f), 18f);

        // ═════════════════════════════════════════════════════════════════════
        //  CỘT TRÁI: AVATAR & LƯỚI CHỌN (X: -290, Rộng: 300)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform leftCol = CreateRect(parchment, "Col_Left", new Vector2(300f, 490f), new Vector2(-290f, 0f));

        // Khung avatar chính tròn (210 x 210)
        RectTransform avFrame = CreateRect(leftCol, "Avatar_Main_Frame", new Vector2(210f, 210f), new Vector2(0f, 120f));
        AddImage(avFrame.gameObject, TaskPopupDesign.KhungIconVien, BoGoc(105f), true);
        RectTransform avBg = CreateRect(avFrame, "Bg", new Vector2(198f, 198f), Vector2.zero);
        AddImage(avBg.gameObject, new Color32(245, 235, 205, 255), BoGoc(99f), true);

        RectTransform avImgRt = CreateRect(avBg, "Img_MainAvatar", new Vector2(180f, 180f), Vector2.zero);
        Image mainAv = AddImage(avImgRt.gameObject, Color.white, null, false);
        mainAv.preserveAspect = true;

        // Huy hiệu CẤP hình tròn góc dưới-trái (64 x 64)
        RectTransform badgeRt = CreateRect(avFrame, "Badge_Level", new Vector2(64f, 64f), new Vector2(-75f, -75f));
        AddImage(badgeRt.gameObject, TaskPopupDesign.RibbonVien, BoGoc(32f), true);
        RectTransform badgeFill = CreateRect(badgeRt, "Fill", new Vector2(56f, 56f), Vector2.zero);
        AddImage(badgeFill.gameObject, TaskPopupDesign.RibbonDuoi, BoGoc(28f), true);
        PhuGradient(badgeRt, "Gradient", TaskPopupDesign.RibbonTren, Vector2.zero, new Vector2(56f, 56f), 28f);

        CreateText(badgeRt, "Txt_Cap", "CẤP", 12, new Color32(122, 67, 16, 255), TextAlignmentOptions.Center, new Vector2(0f, 13f), new Vector2(50f, 20f), FontStyles.Bold);
        TMP_Text txtBadgeLevel = CreateText(badgeRt, "Txt_BadgeLevel", "7", 26, new Color32(122, 67, 16, 255), TextAlignmentOptions.Center, new Vector2(0f, -8f), new Vector2(50f, 32f), FontStyles.Bold);

        // Huy hiệu Bút chì góc dưới-phải (48 x 48)
        RectTransform editRt = CreateRect(avFrame, "Badge_Edit", new Vector2(48f, 48f), new Vector2(75f, -75f));
        AddImage(editRt.gameObject, new Color32(63, 138, 18, 255), BoGoc(24f), true);
        RectTransform editFill = CreateRect(editRt, "Fill", new Vector2(42f, 42f), Vector2.zero);
        AddImage(editFill.gameObject, new Color32(97, 181, 39, 255), BoGoc(21f), true);
        PhuGradient(editRt, "Gradient", new Color32(165, 224, 94, 255), Vector2.zero, new Vector2(42f, 42f), 21f);
        CreateText(editRt, "Txt_Icon", "✎", 22, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(40f, 40f), FontStyles.Bold);

        // Khung danh sách chọn avatar bên dưới (Rộng 300, Cao 175)
        RectTransform choiceBox = CreateRect(leftCol, "Box_AvatarChoices", new Vector2(300f, 175f), new Vector2(0f, -145f));
        AddImage(choiceBox.gameObject, new Color32(201, 154, 92, 120), BoGoc(16f), true);
        RectTransform choiceInner = CreateRect(choiceBox, "Inner", new Vector2(294f, 169f), Vector2.zero);
        AddImage(choiceInner.gameObject, new Color32(243, 226, 187, 140), BoGoc(14f), true);

        CreateText(choiceBox, "Txt_Title", "Chọn avatar", 16, new Color32(138, 99, 55, 255), TextAlignmentOptions.Center, new Vector2(0f, 68f), new Vector2(280f, 24f), FontStyles.Bold);

        RectTransform grid = CreateRect(choiceBox, "Grid_AvatarChoices", new Vector2(280f, 125f), new Vector2(0f, -12f));
        Button[] btns = new Button[8];
        Image[] btnImgs = new Image[8];
        GameObject[] hlObjs = new GameObject[8];

        float[] posX = { -99f, -33f, 33f, 99f };
        float[] posY = { 28f, -32f };

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Vector2 slotPos = new Vector2(posX[col], posY[row]);

            RectTransform slot = CreateRect(grid, $"Slot_{i}", new Vector2(56f, 56f), slotPos);
            AddImage(slot.gameObject, TaskPopupDesign.HangVien, BoGoc(28f), true);
            RectTransform slotBg = CreateRect(slot, "Bg", new Vector2(52f, 52f), Vector2.zero);
            AddImage(slotBg.gameObject, new Color32(255, 253, 244, 255), BoGoc(26f), true);

            RectTransform iconRt = CreateRect(slotBg, "Img_Icon", new Vector2(48f, 48f), Vector2.zero);
            Image ic = AddImage(iconRt.gameObject, Color.white, null, false);
            ic.preserveAspect = true;
            btnImgs[i] = ic;

            // Dấu tích chữ V màu xanh 3D khi được chọn (Selection Indicator)
            RectTransform selectGroup = CreateRect(slot, "Selection_Indicator", new Vector2(56f, 56f), Vector2.zero);

            // 1. Viền sáng xanh lá quanh ô avatar
            RectTransform ring = CreateRect(selectGroup, "Ring", new Vector2(60f, 60f), Vector2.zero);
            AddImage(ring.gameObject, new Color32(86, 175, 34, 255), BoGoc(30f), false);

            // 2. Huy hiệu tròn dấu tích chữ V ở góc dưới bên phải
            RectTransform checkBadge = CreateRect(selectGroup, "Badge_Check", new Vector2(24f, 24f), new Vector2(18f, -18f));
            AddImage(checkBadge.gameObject, new Color32(35, 105, 18, 255), BoGoc(12f), false);
            RectTransform checkInner = CreateRect(checkBadge, "Inner", new Vector2(20f, 20f), Vector2.zero);
            AddImage(checkInner.gameObject, new Color32(76, 175, 30, 255), BoGoc(10f), false);

            TMP_Text checkTxt = CreateText(checkBadge, "Txt_Check", "✔", 16, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(20f, 20f), FontStyles.Bold);
            AddShadow(checkTxt.gameObject, new Color32(20, 70, 10, 220), new Vector2(1f, -1f));

            selectGroup.gameObject.SetActive(i == 0);
            hlObjs[i] = selectGroup.gameObject;

            btns[i] = slot.gameObject.AddComponent<Button>();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  CỘT PHẢI: THÔNG TIN & THỐNG KÊ (X: 145, Rộng: 550)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform rightCol = CreateRect(parchment, "Col_Right", new Vector2(550f, 490f), new Vector2(145f, 0f));

        // 1. Hộp Tên Nông Trại
        CreateText(rightCol, "Lbl_FarmName", "Tên nông trại", 16, new Color32(138, 99, 55, 255), TextAlignmentOptions.Left, new Vector2(0f, 222f), new Vector2(550f, 24f), FontStyles.Bold);

        RectTransform nameBox = CreateRect(rightCol, "Box_FarmName", new Vector2(550f, 54f), new Vector2(0f, 180f));
        AddImage(nameBox.gameObject, new Color32(217, 180, 120, 255), BoGoc(16f), true);
        RectTransform nameFill = CreateRect(nameBox, "Fill", new Vector2(544f, 48f), Vector2.zero);
        AddImage(nameFill.gameObject, new Color32(243, 226, 187, 255), BoGoc(14f), true);

        TMP_InputField input = CreateInput(nameFill, "Input_FarmName", new Vector2(490f, 44f), new Vector2(-20f, 0f));
        CreateText(nameBox, "Txt_Pencil", "✎", 22, new Color32(163, 128, 63, 255), TextAlignmentOptions.Center, new Vector2(240f, 0f), new Vector2(40f, 40f), FontStyles.Bold);

        // 2. Cấp Độ & Thanh EXP
        TMP_Text txtLvlTitle = CreateText(rightCol, "Txt_LevelTitle", "Cấp độ 7", 21, TaskPopupDesign.TenBinhThuong, TextAlignmentOptions.Left, new Vector2(-160f, 122f), new Vector2(220f, 28f), FontStyles.Bold);
        TMP_Text txtLvlRange = CreateText(rightCol, "Txt_LevelRange", $"Cấp 1 – {PlayerProgressManager.CapToiDa}", 15, new Color32(163, 128, 63, 255), TextAlignmentOptions.Right, new Vector2(160f, 122f), new Vector2(220f, 28f), FontStyles.Bold);

        RectTransform expBar = CreateRect(rightCol, "Bar_Exp", new Vector2(550f, 32f), new Vector2(0f, 92f));
        AddImage(expBar.gameObject, TaskPopupDesign.TdMang, BoGoc(16f), true);
        RectTransform expInner = CreateRect(expBar, "Fill_Track", new Vector2(544f, 26f), Vector2.zero);

        RectTransform fillRt = CreateRect(expInner, "Img_ExpFill", new Vector2(544f, 26f), Vector2.zero);
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = AddImage(fillRt.gameObject, TaskPopupDesign.TdRuotDuoi, BoGoc(13f), false);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.62f;

        // Gloss highlight nửa trên
        RectTransform gloss = CreateRect(expBar, "Gloss", new Vector2(538f, 13f), new Vector2(0f, 7f));
        AddImage(gloss.gameObject, TaskPopupDesign.TdGloss, BoGoc(13f), false);

        TMP_Text txtExp = CreateText(expBar, "Txt_ExpValue", "248 / 400 EXP", 17, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(500f, 26f), FontStyles.Bold);
        AddShadow(txtExp.gameObject, TaskPopupDesign.TdChuVien, new Vector2(0f, -2f));

        // 3. Lưới 4 Thẻ Thống Kê (2x2)
        RectTransform cardsGrid = CreateRect(rightCol, "Grid_Cards", new Vector2(550f, 150f), new Vector2(0f, -12f));

        Sprite warehouseSpr = Resources.Load<Sprite>("Tiles/Warehouse/Sprites/Sprite_Tiles_Warehouse");
        if (warehouseSpr == null)
        {
#if UNITY_EDITOR
            warehouseSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/Warehouse/Sprites/Sprite_Tiles_Warehouse.png");
#endif
        }

        Sprite cookingSpr = Resources.Load<Sprite>("Icons/icon_cooking_building");
#if UNITY_EDITOR
        if (cookingSpr == null)
            cookingSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Icons/icon_cooking_building.png");
#endif

        Sprite goldSpr = Resources.Load<Sprite>("Icons/icon_gold") ?? Resources.Load<Sprite>("Icons/gold");
#if UNITY_EDITOR
        if (goldSpr == null)
            goldSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/icon/gold.png");
#endif

        Sprite achSpr = Resources.Load<Sprite>("Icons/icon_achievement") ?? Resources.Load<Sprite>("Icons/trophy");
#if UNITY_EDITOR
        if (achSpr == null)
            achSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/icon/trophy.png");
#endif

        TMP_Text txtWh = CreateStatCard(cardsGrid, "Card_Warehouse", "Sức chứa kho", "120 ô", warehouseSpr, new Vector2(-142f, 38f));
        TMP_Text txtCook = CreateStatCard(cardsGrid, "Card_Cooking", "Điểm nấu ăn", "35 món", cookingSpr, new Vector2(142f, 38f));
        TMP_Text txtGold = CreateStatCard(cardsGrid, "Card_Gold", "Tiền vàng", "1 520", goldSpr, new Vector2(-142f, -42f));
        TMP_Text txtAch = CreateStatCard(cardsGrid, "Card_Achievement", "Nhiệm vụ", "18 đã xong", achSpr, new Vector2(142f, -42f));

        // 4. Nút Lưu Hồ Sơ 3D xanh lá (Rộng 320, Cao 62)
        RectTransform saveBtnRt = CreateRect(rightCol, "Btn_SaveProfile", new Vector2(320f, 62f), new Vector2(0f, -145f));
        AddImage(saveBtnRt.gameObject, TaskPopupDesign.NutNhan.vien, BoGoc(26f), true);
        RectTransform saveFill = CreateRect(saveBtnRt, "Fill", new Vector2(310f, 54f), Vector2.zero);
        AddImage(saveFill.gameObject, TaskPopupDesign.NutNhan.nenDuoi, BoGoc(22f), true);
        PhuGradient(saveBtnRt, "Gradient", TaskPopupDesign.NutNhan.nen, Vector2.zero, new Vector2(310f, 54f), 22f);

        TMP_Text saveTxt = CreateText(saveBtnRt, "Txt_Save", "LƯU HỒ SƠ", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(300f, 50f), FontStyles.Bold);
        AddShadow(saveTxt.gameObject, new Color32(35, 80, 10, 220), new Vector2(1.5f, -2.5f));
        Button btnSave = saveBtnRt.gameObject.AddComponent<Button>();

        // Wire references
        ui.popupRoot = root.gameObject;
        ui.btnClose = btnClose;
        ui.mainAvatarImage = mainAv;
        ui.txtLevelBadge = txtBadgeLevel;
        ui.inputPlayerName = input;
        ui.txtLevel = txtLvlTitle;
        ui.txtLevelRange = txtLvlRange;
        ui.txtExpValue = txtExp;
        ui.expFill = fillImg;
        ui.txtWarehouseLevel = txtWh;
        ui.txtCookingScore = txtCook;
        ui.txtGoldEarned = txtGold;
        ui.txtAchievementCount = txtAch;
        ui.btnSaveProfile = btnSave;
        ui.avatarButtons = btns;
        ui.avatarButtonImages = btnImgs;
        ui.avatarSelectionHighlights = hlObjs;
    }

    private static TMP_Text CreateStatCard(Transform parent, string name, string label, string val, Sprite icon, Vector2 pos)
    {
        RectTransform card = CreateRect(parent, name, new Vector2(265f, 70f), pos);
        AddImage(card.gameObject, new Color32(217, 180, 120, 255), BoGoc(16f), true);
        RectTransform fill = CreateRect(card, "Fill", new Vector2(259f, 64f), Vector2.zero);
        AddImage(fill.gameObject, new Color32(245, 235, 205, 255), BoGoc(14f), true);

        // Khung Icon tròn nhỏ bên trái
        RectTransform icFrame = CreateRect(fill, "Icon_Frame", new Vector2(48f, 48f), new Vector2(-96f, 0f));
        AddImage(icFrame.gameObject, TaskPopupDesign.KhungIconVien, BoGoc(24f), false);
        RectTransform icBg = CreateRect(icFrame, "Bg", new Vector2(44f, 44f), Vector2.zero);
        AddImage(icBg.gameObject, new Color32(255, 250, 235, 255), BoGoc(22f), false);

        RectTransform icImg = CreateRect(icBg, "Img_Icon", new Vector2(38f, 38f), Vector2.zero);
        Image img = AddImage(icImg.gameObject, Color.white, icon, false);
        img.preserveAspect = true;

        CreateText(fill, "Txt_Label", label, 14, new Color32(145, 108, 65, 255), TextAlignmentOptions.Left, new Vector2(28f, 12f), new Vector2(170f, 22f), FontStyles.Bold);
        TMP_Text txtVal = CreateText(fill, "Txt_Value", val, 18, TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Left, new Vector2(28f, -12f), new Vector2(170f, 26f), FontStyles.Bold);

        return txtVal;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private static Sprite BoGoc(float r) => SkinKit.BoGoc(r);

    private static void PhuGradient(Transform parent, string name, Color32 color, Vector2 pos, Vector2 size, float radius)
    {
        RectTransform rt = CreateRect(parent, name, size, pos);
        Image img = AddImage(rt.gameObject, color, BoGoc(radius), false);
        img.raycastTarget = false;
    }

    private static void AddShadow(GameObject go, Color color, Vector2 dist)
    {
        Shadow s = go.GetComponent<Shadow>();
        if (s == null) s = go.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = dist;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        if (parent != null) rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static Image AddImage(GameObject go, Color color, bool raycastTarget)
    {
        return AddImage(go, color, null, raycastTarget);
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite, bool raycastTarget)
    {
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.color = color;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, int size, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 rectSize, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = CreateRect(parent, name, rectSize, position);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        var f = SkinKit.FontVo;
        if (f != null) tmp.font = f;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_InputField CreateInput(Transform parent, string name, Vector2 size, Vector2 position)
    {
        RectTransform root = CreateRect(parent, name, size, position);
        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();

        RectTransform textArea = CreateRect(root, "Text Area", size, Vector2.zero);
        RectTransform text = CreateRect(textArea, "Text", size, Vector2.zero);
        TextMeshProUGUI inputText = text.gameObject.AddComponent<TextMeshProUGUI>();
        var f = SkinKit.FontVo;
        if (f != null) inputText.font = f;
        inputText.fontSize = 22;
        inputText.fontStyle = FontStyles.Bold;
        inputText.alignment = TextAlignmentOptions.Left;
        inputText.color = new Color32(91, 52, 23, 255);
        inputText.raycastTarget = true;

        input.textViewport = textArea;
        input.textComponent = inputText;
        return input;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        Transform child = parent.Find(childName);
        if (child == null) child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindCanvasPopup()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name.Contains("Popup") || canvases[i].name.Contains("UI"))
                return canvases[i].transform;
        }
        return canvases.Length > 0 ? canvases[0].transform : null;
    }
}
