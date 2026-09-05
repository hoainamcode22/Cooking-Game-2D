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
    // [WP-D1] Ảnh nền "Bg" của từng ô chọn avatar — đổi UIStandardSprites.SlotNormal/SlotSelected khi chọn.
    // KHÔNG serialize: chỉ để tiện swap sprite lúc runtime, không phải reference scene cố định.
    private Image[] avatarSlotBgImages = new Image[8];

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

        // [FIX 2026-09-03] Nếu popupRoot có CanvasGroup (do CreateHierarchy gắn), đảm bảo không bị khoá
        // tương tác/raycast từ lần đóng trước (vd do tween/fade nào đó chỉnh) khiến nút X không bấm được.
        CanvasGroup popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.interactable = true;
            popupCanvasGroup.blocksRaycasts = true;
        }

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
        if (txtWarehouseLevel != null) 
        {
            txtWarehouseLevel.text = $"{whLv} ô";
            txtWarehouseLevel.color = new Color32(75, 40, 15, 255);
        }

        // 2. Điểm nấu ăn
        int cookCount = PlayerPrefs.GetInt("COOKING_CHALLENGE_TOTAL_DISHES", 0);
        if (cookCount <= 0) cookCount = PlayerPrefs.GetInt("COOKING_TOTAL_DISHES_MADE", 0);
        if (txtCookingScore != null) 
        {
            txtCookingScore.text = $"{cookCount} món";
            txtCookingScore.color = new Color32(75, 40, 15, 255);
        }

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
        if (txtGoldEarned != null) 
        {
            txtGoldEarned.text = gold.ToString("N0", new System.Globalization.CultureInfo("vi-VN")).Replace(",", " ");
            txtGoldEarned.color = new Color32(75, 40, 15, 255);
        }

        // 4. Thành tựu
        int ach = PlayerPrefs.GetInt(PrefAchievementCount, defaultAchievementCount);
        if (ach <= 0) ach = PlayerPrefs.GetInt("COMPLETED_MISSION_COUNT", 0);
        if (txtAchievementCount != null) 
        {
            txtAchievementCount.text = $"{ach} đã xong";
            txtAchievementCount.color = new Color32(75, 40, 15, 255);
        }

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

            // [WP-D1] Đổi khung nền ô chọn: SlotSelected khi đang chọn, SlotNormal khi không.
            // Ring + Badge_Check (Selection_Indicator) giữ nguyên làm lớp phụ nổi bật thêm.
            if (avatarSlotBgImages != null && i < avatarSlotBgImages.Length && avatarSlotBgImages[i] != null)
            {
                bool isSelected = i == currentSelectedIndex;
                Sprite slotSpr = isSelected ? UIStandardSprites.SlotSelected : UIStandardSprites.SlotNormal;
                if (slotSpr != null)
                {
                    avatarSlotBgImages[i].sprite = slotSpr;
                    avatarSlotBgImages[i].type = Image.Type.Sliced;
                    avatarSlotBgImages[i].color = Color.white;
                }
                // else: sprite null (chưa sync vào Resources/UI/Standard) -> giữ nguyên màu phẳng fallback đã gán lúc tạo.
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
        else Debug.LogWarning("[AvatarProfile] btnClose = null — nút X sẽ không bấm được. Kiểm Popup_AvatarProfile > Board_Wooden > Btn_Close");

        if (btnSaveProfile != null)
        {
            btnSaveProfile.onClick.RemoveListener(SaveAndClose);
            btnSaveProfile.onClick.AddListener(SaveAndClose);
        }
        else Debug.LogWarning("[AvatarProfile] btnSaveProfile = null — nút Lưu Hồ Sơ sẽ không bấm được. Kiểm Popup_AvatarProfile > Board_Wooden > Btn_SaveProfile");

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

        // 3. Kiểm tra xem đã có cấu trúc Board_Wooden với Badge Checkmark mới chưa
        Transform mainBoard = transform.Find("Board_Wooden");
        if (mainBoard != null)
        {
            Transform checkBadge = FindDeepChild(mainBoard, "Badge_Check");
            if (checkBadge != null)
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
        }

        // Dọn dẹp con cũ để tạo mới hoàn toàn với Badge Checkmark V & Icon mới
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

        // [FIX 2026-09-03] Hierarchy cũ trong prefab có thể bị chỉnh sai (tắt raycast / interactable / bị object khác đè).
        // Ép lại đúng trạng thái như CreateFreshHierarchy để nút X luôn bấm được.
        if (btnClose != null)
        {
            btnClose.interactable = true;
            if (btnClose.image != null) btnClose.image.raycastTarget = true;
            foreach (var g in btnClose.GetComponentsInChildren<Graphic>(true))
                if (g.gameObject != btnClose.gameObject) g.raycastTarget = false;
            btnClose.transform.SetAsLastSibling();
        }

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
            avatarSlotBgImages = new Image[8];

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

                    // [WP-D1] Wire lại Bg cho hierarchy đã dựng từ trước để RefreshAvatarSelection
                    // vẫn đổi được SlotNormal/SlotSelected dù không đi qua CreateFreshHierarchy.
                    Transform bg = slot.Find("Bg");
                    if (bg != null) avatarSlotBgImages[i] = bg.GetComponent<Image>();
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
        // 1. Khung ván gỗ ngoài (1000 x 640) — [WP-D1] UIStandardSprites.FrameWood, fallback VanGo* nếu null
        RectTransform board = CreateRect(root, "Board_Wooden", new Vector2(1000f, 640f), Vector2.zero);
        RectTransform boardFill = CreateRect(board, "Fill", new Vector2(986f, 626f), Vector2.zero);
        RectTransform boardGrad = CreateRect(board, "Gradient", new Vector2(986f, 626f), Vector2.zero);
        SetFrameOrFallback(board, boardFill, boardGrad, UIStandardSprites.FrameWood,
            TaskPopupDesign.VanGoVien, TaskPopupDesign.VanGoDuoi, TaskPopupDesign.VanGoTren, 38f);

        // 2. Ruy băng tiêu đề "HỒ SƠ" (400 x 96) — [WP-D1] UIStandardSprites.Ribbon
        RectTransform ribbon = CreateRect(board, "Ribbon_Header", new Vector2(400f, 96f), new Vector2(0f, 320f));
        RectTransform ribbonFill = CreateRect(ribbon, "Fill", new Vector2(390f, 86f), Vector2.zero);
        RectTransform ribbonGrad = CreateRect(ribbon, "Gradient", new Vector2(390f, 86f), Vector2.zero);
        SetFrameOrFallback(ribbon, ribbonFill, ribbonGrad, UIStandardSprites.Ribbon,
            TaskPopupDesign.RibbonVien, TaskPopupDesign.RibbonDuoi, TaskPopupDesign.RibbonTren, 22f);

        TMP_Text titleTxt = CreateText(ribbon, "Txt_Title", "HỒ SƠ", 42, TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Center, Vector2.zero, new Vector2(380f, 70f), FontStyles.Bold);
        AddShadow(titleTxt.gameObject, TaskPopupDesign.VienChuTieuDe, new Vector2(2f, -3f));

        // 3. Nút đóng [X] — [WP-D1] UIStandardSprites.Close (Sliced 64x64, chuẩn đồng bộ toàn game),
        //    fallback vẽ tròn code (SkinKit.HinhTron) nếu sprite null. Giữ TMP "X" trên cùng.
        Vector2 closeSize = UIStandardSprites.CloseSize;
        RectTransform closeRt = CreateRect(board, "Btn_Close", closeSize, new Vector2(470f, 290f));
        RectTransform closeInner = CreateRect(closeRt, "Inner", closeSize - new Vector2(8f, 8f), Vector2.zero);
        RectTransform closeGloss = CreateRect(closeInner, "Gloss", new Vector2(52f, 26f), new Vector2(0f, 13f));

        Sprite closeSpr = UIStandardSprites.Close;
        if (closeSpr != null)
        {
            AddImage(closeRt.gameObject, Color.white, closeSpr, true);
            closeInner.gameObject.SetActive(false);
            closeGloss.gameObject.SetActive(false);
        }
        else
        {
            LogSpriteFallbackOnce();
            AddImage(closeRt.gameObject, new Color32(140, 20, 25, 255), SkinKit.HinhTron(), true);
            Image innerImg = AddImage(closeInner.gameObject, new Color32(235, 60, 65, 255), SkinKit.HinhTron(), false);
            innerImg.raycastTarget = false;
            Image glossImg = AddImage(closeGloss.gameObject, new Color(1f, 1f, 1f, 0.25f), BoGoc(13f), false);
            glossImg.raycastTarget = false;
        }

        TMP_Text xTxt = CreateText(closeRt, "Txt_X", "X", (int)UIStandardSprites.CloseGlyphSize, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(54f, 54f), FontStyles.Bold);
        xTxt.raycastTarget = false;
        AddShadow(xTxt.gameObject, new Color32(80, 10, 15, 220), new Vector2(1f, -2f));
        Button btnClose = closeRt.gameObject.AddComponent<Button>();

        // 4. Tấm giấy kem bên trong (928 x 534) — [WP-D1] UIStandardSprites.PanelPaper
        RectTransform parchment = CreateRect(board, "Panel_Parchment", new Vector2(928f, 534f), new Vector2(0f, -24f));
        RectTransform paperFill = CreateRect(parchment, "Fill", new Vector2(920f, 526f), Vector2.zero);
        RectTransform paperGrad = CreateRect(parchment, "Gradient", new Vector2(920f, 526f), Vector2.zero);
        SetFrameOrFallback(parchment, paperFill, paperGrad, UIStandardSprites.PanelPaper,
            TaskPopupDesign.GiayVien, TaskPopupDesign.GiayDuoi, TaskPopupDesign.GiayTren, 22f);

        // ═════════════════════════════════════════════════════════════════════
        //  CỘT TRÁI: AVATAR & LƯỚI CHỌN (X: -290, Rộng: 300)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform leftCol = CreateRect(parchment, "Col_Left", new Vector2(300f, 490f), new Vector2(-290f, 0f));

        // Khung avatar chính tròn (210 x 210) — [WP-D1] UIStandardSprites.AvatarBase (hud_avatar_base)
        RectTransform avFrame = CreateRect(leftCol, "Avatar_Main_Frame", new Vector2(210f, 210f), new Vector2(0f, 120f));
        Sprite avatarBaseSpr = UIStandardSprites.AvatarBase;
        if (avatarBaseSpr != null) AddImage(avFrame.gameObject, Color.white, avatarBaseSpr, true);
        else { LogSpriteFallbackOnce(); AddImage(avFrame.gameObject, TaskPopupDesign.KhungIconVien, BoGoc(105f), true); }
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
        CreateText(editRt, "Txt_Icon", "SỬA", 12, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(40f, 40f), FontStyles.Bold);

        // Khung danh sách chọn avatar bên dưới (Rộng 300, Cao 175)
        RectTransform choiceBox = CreateRect(leftCol, "Box_AvatarChoices", new Vector2(300f, 175f), new Vector2(0f, -145f));
        AddImage(choiceBox.gameObject, new Color32(201, 154, 92, 120), BoGoc(16f), true);
        RectTransform choiceInner = CreateRect(choiceBox, "Inner", new Vector2(294f, 169f), Vector2.zero);
        AddImage(choiceInner.gameObject, new Color32(243, 226, 187, 140), BoGoc(14f), true);

        CreateText(choiceBox, "Txt_Title", "Chọn avatar", 16, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Center, new Vector2(0f, 68f), new Vector2(280f, 24f), FontStyles.Bold);

        RectTransform grid = CreateRect(choiceBox, "Grid_AvatarChoices", new Vector2(280f, 125f), new Vector2(0f, -12f));
        Button[] btns = new Button[8];
        Image[] btnImgs = new Image[8];
        GameObject[] hlObjs = new GameObject[8];
        Image[] slotBgImgs = new Image[8]; // [WP-D1] để RefreshAvatarSelection đổi SlotNormal/SlotSelected

        float[] posX = { -99f, -33f, 33f, 99f };
        float[] posY = { 28f, -32f };

        Sprite slotNormalSpr = UIStandardSprites.SlotNormal;

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Vector2 slotPos = new Vector2(posX[col], posY[row]);

            RectTransform slot = CreateRect(grid, $"Slot_{i}", new Vector2(56f, 56f), slotPos);
            AddImage(slot.gameObject, TaskPopupDesign.HangVien, BoGoc(28f), true);

            // [WP-D1] Nền ô chọn: UIStandardSprites.SlotNormal lúc dựng, RefreshAvatarSelection sẽ
            // đổi sang SlotSelected khi ô này đang được chọn (xem avatarSlotBgImages).
            RectTransform slotBg = CreateRect(slot, "Bg", new Vector2(52f, 52f), Vector2.zero);
            Image slotBgImg;
            if (slotNormalSpr != null)
            {
                slotBgImg = AddImage(slotBg.gameObject, Color.white, slotNormalSpr, true);
            }
            else
            {
                LogSpriteFallbackOnce();
                slotBgImg = AddImage(slotBg.gameObject, new Color32(255, 253, 244, 255), BoGoc(26f), true);
            }
            slotBgImgs[i] = slotBgImg;

            RectTransform iconRt = CreateRect(slotBg, "Img_Icon", new Vector2(48f, 48f), Vector2.zero);
            Image ic = AddImage(iconRt.gameObject, Color.white, null, false);
            ic.preserveAspect = true;
            btnImgs[i] = ic;

            // Dấu tích chữ V màu xanh 3D khi được chọn (Selection Indicator) — Ring giữ nguyên làm lớp phụ
            RectTransform selectGroup = CreateRect(slot, "Selection_Indicator", new Vector2(56f, 56f), Vector2.zero);

            // 1. Viền sáng xanh lá quanh ô avatar (Outline) — giữ code-drawn làm lớp phụ nổi bật thêm
            RectTransform ring = CreateRect(selectGroup, "Ring", new Vector2(58f, 58f), Vector2.zero);
            Image ringImg = AddImage(ring.gameObject, new Color32(76, 185, 30, 255), BoGoc(29f), true);
            ringImg.type = Image.Type.Sliced;
            ringImg.fillCenter = false; // Rỗng ruột để không che mặt avatar

            // 2. Huy hiệu tròn dấu tích — [WP-D1] UIStandardSprites.CheckBadge, fallback 2 lớp tròn + chữ V
            RectTransform checkBadge = CreateRect(selectGroup, "Badge_Check", new Vector2(22f, 22f), new Vector2(17f, -17f));
            RectTransform checkInner = CreateRect(checkBadge, "Inner", new Vector2(18f, 18f), Vector2.zero);
            TMP_Text checkTxt = CreateText(checkBadge, "Txt_Check", "V", 13, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(18f, 18f), FontStyles.Bold);

            Sprite checkBadgeSpr = UIStandardSprites.CheckBadge;
            if (checkBadgeSpr != null)
            {
                AddImage(checkBadge.gameObject, Color.white, checkBadgeSpr, true);
                checkInner.gameObject.SetActive(false);
                checkTxt.gameObject.SetActive(false);
            }
            else
            {
                LogSpriteFallbackOnce();
                AddImage(checkBadge.gameObject, new Color32(35, 105, 18, 255), BoGoc(11f), true);
                AddImage(checkInner.gameObject, new Color32(76, 175, 30, 255), BoGoc(9f), true);
                AddShadow(checkTxt.gameObject, new Color32(20, 70, 10, 220), new Vector2(1f, -1f));
            }

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

        // [WP-D1] UIStandardSprites.RowDark (hàng lõm tối) — hàng nhập tên nông trại
        RectTransform nameBox = CreateRect(rightCol, "Box_FarmName", new Vector2(550f, 54f), new Vector2(0f, 180f));
        RectTransform nameFill = CreateRect(nameBox, "Fill", new Vector2(544f, 48f), Vector2.zero);
        Sprite rowDarkSpr = UIStandardSprites.RowDark;
        SetFrameOrFallback(nameBox, nameFill, null, rowDarkSpr,
            new Color32(217, 180, 120, 255), new Color32(243, 226, 187, 255), default, 16f);
        if (rowDarkSpr != null)
        {
            // Khi dùng RowDark (nền tối) → lót PanelPaper (giấy kem) vào "Fill" để chữ nâu của ô nhập vẫn đọc được.
            // Fill vẫn là cha của Input_FarmName nên KHÔNG được tắt/xoá node này.
            Sprite paperInsetSpr = UIStandardSprites.PanelPaper;
            nameFill.sizeDelta = new Vector2(536f, 42f); // chừa ~7px viền tối lộ ra quanh giấy
            if (paperInsetSpr != null) AddImage(nameFill.gameObject, Color.white, paperInsetSpr, true);
        }

        TMP_InputField input = CreateInput(nameFill, "Input_FarmName", new Vector2(490f, 44f), new Vector2(-20f, 0f));
        CreateText(nameBox, "Txt_Pencil", "SỬA", 14, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Center, new Vector2(240f, 0f), new Vector2(40f, 40f), FontStyles.Bold);

        // 2. Cấp Độ & Thanh EXP
        TMP_Text txtLvlTitle = CreateText(rightCol, "Txt_LevelTitle", "Cấp độ 7", 21, TaskPopupDesign.TenBinhThuong, TextAlignmentOptions.Left, new Vector2(-160f, 122f), new Vector2(220f, 28f), FontStyles.Bold);
        TMP_Text txtLvlRange = CreateText(rightCol, "Txt_LevelRange", $"Cấp 1 – {PlayerProgressManager.CapToiDa}", 15, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Right, new Vector2(160f, 122f), new Vector2(220f, 28f), FontStyles.Bold);

        // [WP-D1] Track = UIStandardSprites.BarTrack, Fill = UIStandardSprites.BarFill (giữ cơ chế Filled+fillAmount)
        RectTransform expBar = CreateRect(rightCol, "Bar_Exp", new Vector2(550f, 32f), new Vector2(0f, 92f));
        Sprite barTrackSpr = UIStandardSprites.BarTrack;
        if (barTrackSpr != null) AddImage(expBar.gameObject, Color.white, barTrackSpr, true);
        else { LogSpriteFallbackOnce(); AddImage(expBar.gameObject, TaskPopupDesign.TdMang, BoGoc(16f), true); }

        RectTransform expInner = CreateRect(expBar, "Fill_Track", new Vector2(544f, 26f), Vector2.zero);

        RectTransform fillRt = CreateRect(expInner, "Img_ExpFill", new Vector2(544f, 26f), Vector2.zero);
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Sprite barFillSpr = UIStandardSprites.BarFill;
        Image fillImg = barFillSpr != null
            ? AddImage(fillRt.gameObject, Color.white, barFillSpr, false)
            // Ruột thanh EXP fallback: Xanh dương biển (#1CA4FF) đồng bộ hoàn hảo với HUD ngoài
            : AddImage(fillRt.gameObject, new Color32(28, 164, 255, 255), BoGoc(13f), false);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.62f;

        // Gloss highlight nửa trên — chỉ vẽ khi dùng fallback (sprite thật thường đã có bóng sẵn)
        RectTransform gloss = CreateRect(expBar, "Gloss", new Vector2(538f, 13f), new Vector2(0f, 7f));
        if (barTrackSpr != null) gloss.gameObject.SetActive(false);
        else AddImage(gloss.gameObject, new Color32(150, 225, 255, 120), BoGoc(13f), false);

        TMP_Text txtExp = CreateText(expBar, "Txt_ExpValue", "248 / 400 EXP", 17, new Color32(0x44, 0x25, 0x10, 255), TextAlignmentOptions.Center, Vector2.zero, new Vector2(500f, 26f), FontStyles.Bold);
        // [FIX 2026-09-03] Chu trắng chìm trên nền be khi EXP thấp -> đổi sang nâu đậm #442510.
        // Shadow tối (TdChuVien) trên chữ tối là thừa -> đổi sang highlight sáng đục thấp cho hiệu ứng nổi nhẹ.
        AddShadow(txtExp.gameObject, new Color(1f, 0.97f, 0.85f, 0.5f), new Vector2(0f, -2f));

        // 3. Lưới 4 Thẻ Thống Kê (2x2)
        RectTransform cardsGrid = CreateRect(rightCol, "Grid_Cards", new Vector2(550f, 150f), new Vector2(0f, -12f));

        // [WP-D1] Icon thẻ thống kê: đi qua UIStandardSprites.Load (Resources/UI/Standard → AssetDatabase → Resources theo tên),
        // KHÔNG gọi AssetDatabase trực tiếp nữa. Vẫn giữ các đường Resources/Icons/* làm fallback cuối cho build thật.
        Sprite warehouseSpr = UIStandardSprites.Load("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_warehouse_v2_1786984374562-removebg-preview.png");
        Sprite cookingSpr   = Resources.Load<Sprite>("Icons/icon_cooking_building");
        Sprite goldSpr      = UIStandardSprites.IconGold;
        Sprite achSpr       = UIStandardSprites.Load("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_market_board_v2_1786984419449-removebg-preview.png");

        if (cookingSpr == null)   cookingSpr   = UIStandardSprites.Load("Assets/Resources/Icons/icon_cooking_building.png");
        if (warehouseSpr == null) warehouseSpr = Resources.Load<Sprite>("Icons/icon_warehouse");
        if (goldSpr == null)      goldSpr      = Resources.Load<Sprite>("Icons/icon_gold");
        if (achSpr == null)       achSpr       = Resources.Load<Sprite>("Icons/icon_achievement");

        // Tên node giá trị đặt theo đúng khoá mà AutoWireNewHierarchy tìm (Txt_WarehouseVal…) để re-wire prefab cũ không bị null.
        TMP_Text txtWh = CreateStatCard(cardsGrid, "Card_Warehouse", "Sức chứa kho", "120 ô", warehouseSpr, new Vector2(-142f, 38f), "Txt_WarehouseVal");
        TMP_Text txtCook = CreateStatCard(cardsGrid, "Card_Cooking", "Điểm nấu ăn", "35 món", cookingSpr, new Vector2(142f, 38f), "Txt_CookingVal");
        TMP_Text txtGold = CreateStatCard(cardsGrid, "Card_Gold", "Tiền vàng", "1 520", goldSpr, new Vector2(-142f, -42f), "Txt_GoldVal");
        TMP_Text txtAch = CreateStatCard(cardsGrid, "Card_Achievement", "Nhiệm vụ", "18 đã xong", achSpr, new Vector2(142f, -42f), "Txt_AchievementVal");

        // 4. Nút Lưu Hồ Sơ 3D xanh lá (Rộng 320, Cao 62) — [WP-D1] UIStandardSprites.BtnGreen3D (Sliced),
        //    Fill/Gradient chỉ vẽ ở nhánh fallback (sprite thật đã có bóng/độ nổi sẵn).
        RectTransform saveBtnRt = CreateRect(rightCol, "Btn_SaveProfile", new Vector2(320f, 62f), new Vector2(0f, -145f));
        RectTransform saveFill = CreateRect(saveBtnRt, "Fill", new Vector2(310f, 54f), Vector2.zero);
        RectTransform saveGrad = CreateRect(saveBtnRt, "Gradient", new Vector2(310f, 54f), Vector2.zero);
        SetFrameOrFallback(saveBtnRt, saveFill, saveGrad, UIStandardSprites.BtnGreen3D,
            TaskPopupDesign.NutNhan.vien, TaskPopupDesign.NutNhan.nenDuoi, TaskPopupDesign.NutNhan.nen, 26f);

        TMP_Text saveTxt = CreateText(saveBtnRt, "Txt_Save", "LƯU HỒ SƠ", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(300f, 50f), FontStyles.Bold);
        AddShadow(saveTxt.gameObject, new Color32(35, 80, 10, 220), new Vector2(1.5f, -2.5f));
        Button btnSave = saveBtnRt.gameObject.AddComponent<Button>();

        // Ensure Close button sits at the very top of the board hierarchy
        closeRt.SetAsLastSibling();

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
        ui.avatarSlotBgImages = slotBgImgs; // [WP-D1] để RefreshAvatarSelection đổi SlotNormal/SlotSelected
    }

    /// <summary>
    /// Thẻ thống kê 265x70. [WP-D1] Viền = UIStandardSprites.CardOuter, ruột "Fill" = CardInner (đều Sliced),
    /// khung icon = SlotNormal; sprite nào null → fallback vẽ code SkinKit.BoGoc như bản cũ.
    /// <paramref name="valueNodeName"/>: tên node chữ giá trị (Txt_WarehouseVal…) khớp với AutoWireNewHierarchy.
    /// </summary>
    private static TMP_Text CreateStatCard(Transform parent, string name, string label, string val, Sprite icon, Vector2 pos, string valueNodeName = "Txt_Value")
    {
        RectTransform card = CreateRect(parent, name, new Vector2(265f, 70f), pos);
        RectTransform fill = CreateRect(card, "Fill", new Vector2(259f, 64f), Vector2.zero);

        Sprite cardOuterSpr = UIStandardSprites.CardOuter;
        Sprite cardInnerSpr = UIStandardSprites.CardInner;
        if (cardOuterSpr != null)
        {
            AddImage(card.gameObject, Color.white, cardOuterSpr, true);
            // Ruột: CardInner nếu có, không thì để Fill trong suốt (CardOuter tự có nền).
            if (cardInnerSpr != null) AddImage(fill.gameObject, Color.white, cardInnerSpr, true);
        }
        else
        {
            LogSpriteFallbackOnce();
            AddImage(card.gameObject, new Color32(217, 180, 120, 255), BoGoc(16f), true);
            AddImage(fill.gameObject, new Color32(245, 235, 205, 255), BoGoc(14f), true);
        }

        // Khung Icon nhỏ bên trái — [WP-D1] SlotNormal; fallback vòng tròn KhungIconVien + Bg kem
        RectTransform icFrame = CreateRect(fill, "Icon_Frame", new Vector2(48f, 48f), new Vector2(-96f, 0f));
        RectTransform icBg = CreateRect(icFrame, "Bg", new Vector2(44f, 44f), Vector2.zero);
        Sprite slotSpr = UIStandardSprites.SlotNormal;
        if (slotSpr != null)
        {
            Image icFrameImg = AddImage(icFrame.gameObject, Color.white, slotSpr, false);
            icFrameImg.raycastTarget = false;
            // "Bg" giữ làm node cha của Img_Icon (không Image) để tên hierarchy không đổi.
        }
        else
        {
            LogSpriteFallbackOnce();
            AddImage(icFrame.gameObject, TaskPopupDesign.KhungIconVien, BoGoc(24f), false);
            AddImage(icBg.gameObject, new Color32(255, 250, 235, 255), BoGoc(22f), false);
        }

        RectTransform icImg = CreateRect(icBg, "Img_Icon", new Vector2(38f, 38f), Vector2.zero);
        Image img = AddImage(icImg.gameObject, Color.white, icon, false);
        img.preserveAspect = true;
        img.type = Image.Type.Simple; // icon thật không 9-slice — AddImage mặc định Sliced nên ép lại Simple

        CreateText(fill, "Txt_Label", label, 14, new Color32(110, 75, 45, 255), TextAlignmentOptions.Left, new Vector2(28f, 12f), new Vector2(170f, 22f), FontStyles.Bold);
        TMP_Text txtVal = CreateText(fill, valueNodeName, val, 18, new Color32(75, 40, 15, 255), TextAlignmentOptions.Left, new Vector2(28f, -12f), new Vector2(170f, 26f), FontStyles.Bold);

        return txtVal;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    private static Sprite BoGoc(float r) => SkinKit.BoGoc(r);

    // [WP-D1] Chỉ log 1 lần/phiên khi có sprite chuẩn bị thiếu (chưa sync Resources/UI/Standard) → dùng fallback vẽ code.
    private static bool _daLogSpriteFallback;
    private static void LogSpriteFallbackOnce()
    {
        if (_daLogSpriteFallback) return;
        _daLogSpriteFallback = true;
        Debug.LogWarning("[AvatarProfilePopupUI] Một số sprite chuẩn (UIStandardSprites) chưa load được → dùng fallback vẽ code SkinKit.BoGoc. " +
                         "Chạy tool đồng bộ để copy vào Resources/UI/Standard nếu muốn build thật có sprite thật.");
    }

    /// <summary>
    /// [WP-D1] Gán khung 3 lớp (outer / Fill / Gradient) theo sprite chuẩn hoặc fallback vẽ code:
    /// - Sprite != null: outer nhận sprite (Sliced, màu trắng); "Fill" KHÔNG nhận Image (giữ làm node cha trong suốt
    ///   vì có chỗ con của nó là Input…); "Gradient" bị tắt (SetActive false) — sprite thật đã có bóng sẵn.
    /// - Sprite == null: vẽ đúng 3 lớp màu phẳng bo góc như bản cũ (vien / duoi / tren), radius trong = radius - 4.
    /// Tên node không đổi trong cả 2 nhánh.
    /// </summary>
    private static void SetFrameOrFallback(RectTransform outer, RectTransform fill, RectTransform gradient, Sprite sprite,
        Color32 mauVien, Color32 mauDuoi, Color32 mauTren, float radius)
    {
        if (sprite != null)
        {
            AddImage(outer.gameObject, Color.white, sprite, true);
            if (gradient != null) gradient.gameObject.SetActive(false);
            return;
        }

        LogSpriteFallbackOnce();
        float innerRadius = Mathf.Max(4f, radius - 4f);
        AddImage(outer.gameObject, mauVien, BoGoc(radius), true);
        if (fill != null) AddImage(fill.gameObject, mauDuoi, BoGoc(innerRadius), true);
        if (gradient != null)
        {
            Image gradImg = AddImage(gradient.gameObject, mauTren, BoGoc(innerRadius), false);
            gradImg.raycastTarget = false;
        }
    }

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

    public static Transform FindCanvasPopup()
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
