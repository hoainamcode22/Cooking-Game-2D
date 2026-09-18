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
        // [VONG 12] KHONG dung new CultureInfo("vi-VN"): build IL2CPP bat Invariant Globalization
        // se nem CultureNotFoundException => chet popup Profile. Dung NumberFormatInfo tu khai bao,
        // giong TownshipHUDController.cs / BoatDockSlot.cs. Giu DUNG output cu: vi-VN nhom bang dau ".",
        private static readonly System.Globalization.NumberFormatInfo DinhDangSoProfile =
            // nen .Replace(",", " ") cu khong bao gio khop => bo di la dung.
            new System.Globalization.NumberFormatInfo
            {
                NumberGroupSeparator   = ".",
                NumberDecimalSeparator = ",",
                NumberGroupSizes       = new[] { 3 },
            };

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

        // [Localization] Popup nay dung chu bang code sau khi scene da tai xong, nen luot quet
        // luc sceneLoaded khong thay. Xin quet lai ngay de khong loe tieng Viet mot nhip.
        Loc.RequestRescan();
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

        if (txtLevel != null) txtLevel.text = Loc.TF("Cấp độ {0}", level);
        if (txtLevelRange != null) txtLevelRange.text = Loc.TF("Cấp 1 – {0}", PlayerProgressManager.CapToiDa);
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
            txtWarehouseLevel.text = Loc.TF("{0} ô", whLv);
            txtWarehouseLevel.color = new Color32(75, 40, 15, 255);
        }

        // 2. Điểm nấu ăn
        // Nguồn THẬT: MissionProgressTracker, khoá "CookDish:*" — CookingChallengeManager và
        // TouristVisitorManager đều ReportEvent(MissionEventType.CookDish, …) vào đó, dữ liệu nằm
        // trong PlayerPrefs "MISSION_PROGRESS_V1" nên reset đúng cùng "Chơi lại từ đầu".
        // Hai khoá cũ bên dưới KHÔNG có chỗ nào ghi trong dự án (đã grep toàn Assets/_Game) —
        // giữ lại làm fallback cho save cũ, không xoá để không phá dữ liệu người chơi.
        int cookCount = 0;
        if (MissionProgressTracker.Instance != null)
        {
            cookCount = MissionProgressTracker.Instance.GetProgress($"{MissionEventType.CookDish}:*");
        }
        if (cookCount <= 0) cookCount = PlayerPrefs.GetInt("COOKING_CHALLENGE_TOTAL_DISHES", 0);
        if (cookCount <= 0) cookCount = PlayerPrefs.GetInt("COOKING_TOTAL_DISHES_MADE", 0);
        if (txtCookingScore != null) 
        {
            txtCookingScore.text = Loc.TF("{0} món", cookCount);
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
            txtGoldEarned.text = gold.ToString("N0", DinhDangSoProfile);
            txtGoldEarned.color = new Color32(75, 40, 15, 255);
        }

        // 4. Thành tựu
        int ach = PlayerPrefs.GetInt(PrefAchievementCount, defaultAchievementCount);
        if (ach <= 0) ach = PlayerPrefs.GetInt("COMPLETED_MISSION_COUNT", 0);
        if (txtAchievementCount != null) 
        {
            txtAchievementCount.text = Loc.TF("{0} đã xong", ach);
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
            Instance.txtAchievementCount.text = Loc.TF("{0} đã xong", cur);
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
        // [FIX 2026-09-06] Hierarchy đã dựng trong SCN_Farm đặt tên node giá trị của CẢ 4 thẻ
        // thống kê là "Txt_Value" (mặc định của CreateStatCard), KHÔNG phải "Txt_WarehouseVal"…
        // Tìm theo tên riêng nên trả null → RefreshStats bỏ qua cả 4 → số mockup lúc dựng
        // ("120 ô", "35 món", "1 520", "18 đã xong") đứng yên vĩnh viễn, nhìn y như dữ liệu
        // cũ không chịu reset. Fallback: lấy "Txt_Value" BÊN TRONG đúng thẻ tương ứng.
        txtWarehouseLevel   = FindStatValueText(board, "Txt_WarehouseVal",   "Card_Warehouse");
        txtCookingScore     = FindStatValueText(board, "Txt_CookingVal",     "Card_Cooking");
        txtGoldEarned       = FindStatValueText(board, "Txt_GoldVal",        "Card_Gold");
        txtAchievementCount = FindStatValueText(board, "Txt_AchievementVal", "Card_Achievement");
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

        if (txtWarehouseLevel == null || txtCookingScore == null || txtGoldEarned == null || txtAchievementCount == null)
        {
            Debug.LogWarning("[AvatarProfile] Thiếu ô chữ giá trị của thẻ thống kê — số trên thẻ sẽ đứng yên. Kiểm Board_Wooden > Panel_Parchment > Col_Right > Grid_Cards > Card_* > Fill > Txt_Value.");
        }
    }

    /// <summary>
    /// Lấy ô CHỮ GIÁ TRỊ của một thẻ thống kê, chịu được cả hai kiểu đặt tên:
    ///  • mới — node tên riêng: "Txt_WarehouseVal" / "Txt_CookingVal" / …
    ///  • cũ  — node tên chung "Txt_Value" nằm TRONG thẻ (mặc định của CreateStatCard).
    /// Phải giới hạn tìm "Txt_Value" TRONG đúng thẻ, vì cả 4 thẻ đều có node trùng tên này.
    /// CHỈ ĐỌC hierarchy — không tạo, không xoá, không đổi tên node nào.
    /// </summary>
    private static TMP_Text FindStatValueText(Transform board, string valueNodeName, string cardName)
    {
        TMP_Text direct = FindChildComponent<TMP_Text>(board, valueNodeName);
        if (direct != null) return direct;

        Transform card = FindDeepChild(board, cardName);
        if (card == null) return null;

        Transform legacy = FindDeepChild(card, "Txt_Value");
        return legacy != null ? legacy.GetComponent<TMP_Text>() : null;
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
        // 1. Khung ván gỗ ngoài (1080 x 680) — [WP-D1] UIStandardSprites.FrameWood, fallback VanGo* nếu null
        RectTransform board = CreateRect(root, "Board_Wooden", new Vector2(1080f, 680f), Vector2.zero);
        RectTransform boardFill = CreateRect(board, "Fill", new Vector2(1066f, 666f), Vector2.zero);
        RectTransform boardGrad = CreateRect(board, "Gradient", new Vector2(1066f, 666f), Vector2.zero);
        SetFrameOrFallback(board, boardFill, boardGrad, UIStandardSprites.FrameWood,
            TaskPopupDesign.VanGoVien, TaskPopupDesign.VanGoDuoi, TaskPopupDesign.VanGoTren, 38f);

        // 2. Ruy băng tiêu đề "HỒ SƠ" (400 x 96) — [WP-D1] UIStandardSprites.Ribbon
        RectTransform ribbon = CreateRect(board, "Ribbon_Header", new Vector2(400f, 96f), new Vector2(0f, 340f));
        RectTransform ribbonFill = CreateRect(ribbon, "Fill", new Vector2(390f, 86f), Vector2.zero);
        RectTransform ribbonGrad = CreateRect(ribbon, "Gradient", new Vector2(390f, 86f), Vector2.zero);
        SetFrameOrFallback(ribbon, ribbonFill, ribbonGrad, UIStandardSprites.Ribbon,
            TaskPopupDesign.RibbonVien, TaskPopupDesign.RibbonDuoi, TaskPopupDesign.RibbonTren, 22f);

        TMP_Text titleTxt = CreateText(ribbon, "Txt_Title", "HỒ SƠ", 42, TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Center, Vector2.zero, new Vector2(380f, 70f), FontStyles.Bold);
        AddShadow(titleTxt.gameObject, TaskPopupDesign.VienChuTieuDe, new Vector2(2f, -3f));

        // 3. Nút đóng [X] — nằm ngay góc trên-phải khung gỗ
        Vector2 closeSize = UIStandardSprites.CloseSize;
        RectTransform closeRt = CreateRect(board, "Btn_Close", closeSize, new Vector2(510f, 310f));
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

        // 4. Tấm giấy kem bên trong (1008 x 574) — [WP-D1] UIStandardSprites.PanelPaper
        RectTransform parchment = CreateRect(board, "Panel_Parchment", new Vector2(1008f, 574f), new Vector2(0f, -20f));
        RectTransform paperFill = CreateRect(parchment, "Fill", new Vector2(1000f, 566f), Vector2.zero);
        RectTransform paperGrad = CreateRect(parchment, "Gradient", new Vector2(1000f, 566f), Vector2.zero);
        SetFrameOrFallback(parchment, paperFill, paperGrad, UIStandardSprites.PanelPaper,
            TaskPopupDesign.GiayVien, TaskPopupDesign.GiayDuoi, TaskPopupDesign.GiayTren, 22f);

        // ═════════════════════════════════════════════════════════════════════
        //  CỘT TRÁI: AVATAR & LƯỚI CHỌN (X: -320, Rộng: 310)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform leftCol = CreateRect(parchment, "Col_Left", new Vector2(310f, 520f), new Vector2(-320f, 0f));

        // Khung avatar chính tròn (210 x 210) — [WP-D1] UIStandardSprites.AvatarBase (hud_avatar_base)
        RectTransform avFrame = CreateRect(leftCol, "Avatar_Main_Frame", new Vector2(210f, 210f), new Vector2(0f, 130f));
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

        // Khung danh sách chọn avatar bên dưới (Rộng 310, Cao 185)
        RectTransform choiceBox = CreateRect(leftCol, "Box_AvatarChoices", new Vector2(310f, 185f), new Vector2(0f, -145f));
        AddImage(choiceBox.gameObject, new Color32(201, 154, 92, 120), BoGoc(16f), true);
        RectTransform choiceInner = CreateRect(choiceBox, "Inner", new Vector2(304f, 179f), Vector2.zero);
        AddImage(choiceInner.gameObject, new Color32(243, 226, 187, 140), BoGoc(14f), true);

        CreateText(choiceBox, "Txt_Title", "Chọn avatar", 16, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Center, new Vector2(0f, 72f), new Vector2(290f, 24f), FontStyles.Bold);

        RectTransform grid = CreateRect(choiceBox, "Grid_AvatarChoices", new Vector2(290f, 130f), new Vector2(0f, -12f));
        Button[] btns = new Button[8];
        Image[] btnImgs = new Image[8];
        GameObject[] hlObjs = new GameObject[8];
        Image[] slotBgImgs = new Image[8]; // [WP-D1] để RefreshAvatarSelection đổi SlotNormal/SlotSelected

        float[] posX = { -105f, -35f, 35f, 105f };
        float[] posY = { 30f, -32f };

        Sprite slotNormalSpr = UIStandardSprites.SlotNormal;

        for (int i = 0; i < 8; i++)
        {
            int col = i % 4;
            int row = i / 4;
            Vector2 slotPos = new Vector2(posX[col], posY[row]);

            RectTransform slot = CreateRect(grid, $"Slot_{i}", new Vector2(58f, 58f), slotPos);
            AddImage(slot.gameObject, TaskPopupDesign.HangVien, BoGoc(28f), true);

            // [WP-D1] Nền ô chọn: UIStandardSprites.SlotNormal lúc dựng, RefreshAvatarSelection sẽ
            // đổi sang SlotSelected khi ô này đang được chọn (xem avatarSlotBgImages).
            RectTransform slotBg = CreateRect(slot, "Bg", new Vector2(54f, 54f), Vector2.zero);
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

            RectTransform iconRt = CreateRect(slotBg, "Img_Icon", new Vector2(50f, 50f), Vector2.zero);
            Image ic = AddImage(iconRt.gameObject, Color.white, null, false);
            ic.preserveAspect = true;
            btnImgs[i] = ic;

            // Dấu tích chữ V màu xanh 3D khi được chọn (Selection Indicator) — Ring giữ nguyên làm lớp phụ
            RectTransform selectGroup = CreateRect(slot, "Selection_Indicator", new Vector2(58f, 58f), Vector2.zero);

            // 1. Viền sáng xanh lá quanh ô avatar (Outline) — giữ code-drawn làm lớp phụ nổi bật thêm
            RectTransform ring = CreateRect(selectGroup, "Ring", new Vector2(60f, 60f), Vector2.zero);
            Image ringImg = AddImage(ring.gameObject, new Color32(76, 185, 30, 255), BoGoc(29f), true);
            ringImg.type = Image.Type.Sliced;
            ringImg.fillCenter = false; // Rỗng ruột để không che mặt avatar

            // 2. Huy hiệu tròn dấu tích — [WP-D1] UIStandardSprites.CheckBadge, fallback 2 lớp tròn + chữ V
            RectTransform checkBadge = CreateRect(selectGroup, "Badge_Check", new Vector2(24f, 24f), new Vector2(18f, -18f));
            RectTransform checkInner = CreateRect(checkBadge, "Inner", new Vector2(20f, 20f), Vector2.zero);
            TMP_Text checkTxt = CreateText(checkBadge, "Txt_Check", "V", 14, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 1f), new Vector2(20f, 20f), FontStyles.Bold);

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
                AddImage(checkBadge.gameObject, new Color32(35, 105, 18, 255), BoGoc(12f), true);
                AddImage(checkInner.gameObject, new Color32(76, 175, 30, 255), BoGoc(10f), true);
                AddShadow(checkTxt.gameObject, new Color32(20, 70, 10, 220), new Vector2(1f, -1f));
            }

            selectGroup.gameObject.SetActive(i == 0);
            hlObjs[i] = selectGroup.gameObject;

            btns[i] = slot.gameObject.AddComponent<Button>();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  CỘT PHẢI: THÔNG TIN & THỐNG KÊ (X: 165, Rộng: 610)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform rightCol = CreateRect(parchment, "Col_Right", new Vector2(610f, 520f), new Vector2(165f, 0f));

        // 1. Hộp Tên Nông Trại
        CreateText(rightCol, "Lbl_FarmName", "Tên nông trại", 17, new Color32(138, 99, 55, 255), TextAlignmentOptions.Left, new Vector2(0f, 230f), new Vector2(610f, 24f), FontStyles.Bold);

        // [WP-D1] UIStandardSprites.RowDark (hàng lõm tối) — hàng nhập tên nông trại
        RectTransform nameBox = CreateRect(rightCol, "Box_FarmName", new Vector2(610f, 56f), new Vector2(0f, 185f));
        RectTransform nameFill = CreateRect(nameBox, "Fill", new Vector2(604f, 50f), Vector2.zero);
        Sprite rowDarkSpr = UIStandardSprites.RowDark;
        SetFrameOrFallback(nameBox, nameFill, null, rowDarkSpr,
            new Color32(217, 180, 120, 255), new Color32(243, 226, 187, 255), default, 16f);
        if (rowDarkSpr != null)
        {
            // Khi dùng RowDark (nền tối) → lót PanelPaper (giấy kem) vào "Fill" để chữ nâu của ô nhập vẫn đọc được.
            Sprite paperInsetSpr = UIStandardSprites.PanelPaper;
            nameFill.sizeDelta = new Vector2(596f, 44f); // chừa ~7px viền tối lộ ra quanh giấy
            if (paperInsetSpr != null) AddImage(nameFill.gameObject, Color.white, paperInsetSpr, true);
        }

        TMP_InputField input = CreateInput(nameFill, "Input_FarmName", new Vector2(530f, 44f), new Vector2(-25f, 0f));
        
        // Nút biểu tượng bút chì 3D thay cho chữ SỬA phẳng
        RectTransform pencilRt = CreateRect(nameBox, "Btn_Pencil", new Vector2(36f, 36f), new Vector2(270f, 0f));
        Sprite pencilSpr = UIStandardSprites.Load("Assets/Assetsgame/PopupArt_Custom/icon_pencil_edit.png");
        if (pencilSpr != null)
        {
            Image pImg = AddImage(pencilRt.gameObject, Color.white, pencilSpr, false);
            pImg.preserveAspect = true;
            pImg.type = Image.Type.Simple;
        }
        else
        {
            CreateText(pencilRt, "Txt_Pencil", "✎", 20, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Center, Vector2.zero, new Vector2(36f, 36f), FontStyles.Bold);
        }

        // 2. Cấp Độ & Thanh EXP
        TMP_Text txtLvlTitle = CreateText(rightCol, "Txt_LevelTitle", Loc.TF("Cấp độ {0}", 7), 22, TaskPopupDesign.TenBinhThuong, TextAlignmentOptions.Left, new Vector2(-180f, 128f), new Vector2(240f, 30f), FontStyles.Bold);
        TMP_Text txtLvlRange = CreateText(rightCol, "Txt_LevelRange", Loc.TF("Cấp 1 – {0}", PlayerProgressManager.CapToiDa), 16, new Color32(0x65, 0x41, 0x29, 255), TextAlignmentOptions.Right, new Vector2(180f, 128f), new Vector2(240f, 30f), FontStyles.Bold);

        // [WP-D1] Track = UIStandardSprites.BarTrack, Fill = UIStandardSprites.BarFill (giữ cơ chế Filled+fillAmount)
        RectTransform expBar = CreateRect(rightCol, "Bar_Exp", new Vector2(610f, 34f), new Vector2(0f, 96f));
        Sprite barTrackSpr = UIStandardSprites.BarTrack;
        if (barTrackSpr != null) AddImage(expBar.gameObject, Color.white, barTrackSpr, true);
        else { LogSpriteFallbackOnce(); AddImage(expBar.gameObject, TaskPopupDesign.TdMang, BoGoc(16f), true); }

        RectTransform expInner = CreateRect(expBar, "Fill_Track", new Vector2(604f, 28f), Vector2.zero);

        RectTransform fillRt = CreateRect(expInner, "Img_ExpFill", new Vector2(604f, 28f), Vector2.zero);
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Sprite barFillSpr = UIStandardSprites.BarFill;
        Image fillImg = barFillSpr != null
            ? AddImage(fillRt.gameObject, Color.white, barFillSpr, false)
            // Ruột thanh EXP fallback: Xanh dương biển (#1CA4FF) đồng bộ hoàn hảo với HUD ngoài
            : AddImage(fillRt.gameObject, new Color32(28, 164, 255, 255), BoGoc(14f), false);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.62f;

        // Gloss highlight nửa trên — chỉ vẽ khi dùng fallback (sprite thật thường đã có bóng sẵn)
        RectTransform gloss = CreateRect(expBar, "Gloss", new Vector2(598f, 14f), new Vector2(0f, 7f));
        if (barTrackSpr != null) gloss.gameObject.SetActive(false);
        else AddImage(gloss.gameObject, new Color32(150, 225, 255, 120), BoGoc(14f), false);

        TMP_Text txtExp = CreateText(expBar, "Txt_ExpValue", "248 / 400 EXP", 17, new Color32(0x44, 0x25, 0x10, 255), TextAlignmentOptions.Center, Vector2.zero, new Vector2(560f, 28f), FontStyles.Bold);
        AddShadow(txtExp.gameObject, new Color(1f, 0.97f, 0.85f, 0.5f), new Vector2(0f, -2f));

        // 3. Lưới 4 Thẻ Thống Kê (2x2)
        RectTransform cardsGrid = CreateRect(rightCol, "Grid_Cards", new Vector2(610f, 164f), new Vector2(0f, -12f));

        // [WP-D1] Icon thẻ thống kê: đi qua UIStandardSprites.Load
        Sprite warehouseSpr = UIStandardSprites.Load("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_warehouse_v2_1786984374562-removebg-preview.png");
        Sprite cookingSpr   = Resources.Load<Sprite>("Icons/icon_cooking_building");
        Sprite goldSpr      = UIStandardSprites.IconGold;
        Sprite achSpr       = UIStandardSprites.Load("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_market_board_v2_1786984419449-removebg-preview.png");

        if (cookingSpr == null)   cookingSpr   = UIStandardSprites.Load("Assets/Resources/Icons/icon_cooking_building.png");
        if (warehouseSpr == null) warehouseSpr = Resources.Load<Sprite>("Icons/icon_warehouse");
        if (goldSpr == null)      goldSpr      = Resources.Load<Sprite>("Icons/icon_gold");
        if (achSpr == null)       achSpr       = Resources.Load<Sprite>("Icons/icon_achievement");

        // Tên node giá trị đặt theo đúng khoá mà AutoWireNewHierarchy tìm (Txt_WarehouseVal…)
        TMP_Text txtWh = CreateStatCard(cardsGrid, "Card_Warehouse", "Sức chứa kho", "120 ô", warehouseSpr, new Vector2(-155f, 42f), "Txt_WarehouseVal");
        TMP_Text txtCook = CreateStatCard(cardsGrid, "Card_Cooking", "Điểm nấu ăn", "35 món", cookingSpr, new Vector2(155f, 42f), "Txt_CookingVal");
        TMP_Text txtGold = CreateStatCard(cardsGrid, "Card_Gold", "Tiền vàng", "1 520", goldSpr, new Vector2(-155f, -42f), "Txt_GoldVal");
        TMP_Text txtAch = CreateStatCard(cardsGrid, "Card_Achievement", "Nhiệm vụ", "18 đã xong", achSpr, new Vector2(155f, -42f), "Txt_AchievementVal");

        // 4. Nút Lưu Hồ Sơ 3D xanh lá (Rộng 340, Cao 62)
        RectTransform saveBtnRt = CreateRect(rightCol, "Btn_SaveProfile", new Vector2(340f, 62f), new Vector2(0f, -155f));
        RectTransform saveFill = CreateRect(saveBtnRt, "Fill", new Vector2(330f, 54f), Vector2.zero);
        RectTransform saveGrad = CreateRect(saveBtnRt, "Gradient", new Vector2(330f, 54f), Vector2.zero);
        SetFrameOrFallback(saveBtnRt, saveFill, saveGrad, UIStandardSprites.BtnGreen3D,
            TaskPopupDesign.NutNhan.vien, TaskPopupDesign.NutNhan.nenDuoi, TaskPopupDesign.NutNhan.nen, 26f);

        TMP_Text saveTxt = CreateText(saveBtnRt, "Txt_Save", "LƯU HỒ SƠ", 24, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(320f, 50f), FontStyles.Bold);
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
        ui.avatarSlotBgImages = slotBgImgs;
    }

    /// <summary>
    /// Thẻ thống kê 295x76. Viền = CardOuter, ruột "Fill" = CardInner,
    /// khung icon = SlotNormal; sprite nào null → fallback vẽ code SkinKit.BoGoc.
    /// </summary>
    private static TMP_Text CreateStatCard(Transform parent, string name, string label, string val, Sprite icon, Vector2 pos, string valueNodeName = "Txt_Value")
    {
        RectTransform card = CreateRect(parent, name, new Vector2(295f, 76f), pos);
        RectTransform fill = CreateRect(card, "Fill", new Vector2(289f, 70f), Vector2.zero);

        Sprite cardOuterSpr = UIStandardSprites.CardOuter;
        Sprite cardInnerSpr = UIStandardSprites.CardInner;
        if (cardOuterSpr != null)
        {
            AddImage(card.gameObject, Color.white, cardOuterSpr, true);
            if (cardInnerSpr != null) AddImage(fill.gameObject, Color.white, cardInnerSpr, true);
        }
        else
        {
            LogSpriteFallbackOnce();
            AddImage(card.gameObject, new Color32(217, 180, 120, 255), BoGoc(18f), true);
            AddImage(fill.gameObject, new Color32(245, 235, 205, 255), BoGoc(16f), true);
        }

        // Khung Icon nhỏ bên trái (52 x 52)
        RectTransform icFrame = CreateRect(fill, "Icon_Frame", new Vector2(52f, 52f), new Vector2(-110f, 0f));
        RectTransform icBg = CreateRect(icFrame, "Bg", new Vector2(48f, 48f), Vector2.zero);
        Sprite slotSpr = UIStandardSprites.SlotNormal;
        if (slotSpr != null)
        {
            Image icFrameImg = AddImage(icFrame.gameObject, Color.white, slotSpr, false);
            icFrameImg.raycastTarget = false;
        }
        else
        {
            LogSpriteFallbackOnce();
            AddImage(icFrame.gameObject, TaskPopupDesign.KhungIconVien, BoGoc(26f), false);
            AddImage(icBg.gameObject, new Color32(255, 250, 235, 255), BoGoc(24f), false);
        }

        RectTransform icImg = CreateRect(icBg, "Img_Icon", new Vector2(42f, 42f), Vector2.zero);
        Image img = AddImage(icImg.gameObject, Color.white, icon, false);
        img.preserveAspect = true;
        img.type = Image.Type.Simple;

        CreateText(fill, "Txt_Label", label, 15, new Color32(110, 75, 45, 255), TextAlignmentOptions.Left, new Vector2(30f, 13f), new Vector2(190f, 24f), FontStyles.Bold);
        TMP_Text txtVal = CreateText(fill, valueNodeName, val, 22, new Color32(75, 40, 15, 255), TextAlignmentOptions.Left, new Vector2(30f, -13f), new Vector2(190f, 28f), FontStyles.Bold);

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
