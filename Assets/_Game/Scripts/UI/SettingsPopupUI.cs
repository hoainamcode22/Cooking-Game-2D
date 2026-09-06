using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FarmGame.UI;

/// <summary>
/// POPUP CÀI ĐẶT (SettingsPopupUI)
/// Thiết kế cao cấp đồng bộ với nông trại:
/// - Khung gỗ sang trọng & Ruy băng tiêu đề 3D "CÀI ĐẶT"
/// - Nút Đóng [X] đỏ 3D bo tròn (không bị ô vuông trắng)
/// - 2 hàng Slider Âm thanh game & Âm thanh VFX với nút BẬT/TẮT 3D bo góc
/// - 2 nút Ngôn ngữ với Cờ Việt Nam 🇻🇳 và Cờ English 🇬🇧 vẽ vector sắc nét + Dấu tích chữ V (✓)
/// - Nút ĐÓNG 3D xanh lá bên dưới
/// </summary>
public class SettingsPopupUI : MonoBehaviour
{
    public static SettingsPopupUI Instance { get; private set; }
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private const string PrefLanguage = "GAME_LANGUAGE";

    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnBottomClose;

    [Header("BGM / Game Audio")]
    [SerializeField] private Slider sliderBgm;
    [SerializeField] private Button btnToggleBgm;
    [SerializeField] private Image imgToggleBgmBg;
    [SerializeField] private TMP_Text txtToggleBgm;

    [Header("SFX / VFX Audio")]
    [SerializeField] private Slider sliderSfx;
    [SerializeField] private Button btnToggleSfx;
    [SerializeField] private Image imgToggleSfxBg;
    [SerializeField] private TMP_Text txtToggleSfx;

    [Header("Language Buttons")]
    [SerializeField] private Button btnLangVi;
    [SerializeField] private Button btnLangEn;
    [SerializeField] private Image imgLangViBg;
    [SerializeField] private Image imgLangEnBg;
    [SerializeField] private GameObject goCheckVi;
    [SerializeField] private GameObject goCheckEn;
    [Header("Localized Labels")]
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtBgmLabel;
    [SerializeField] private TMP_Text txtSfxLabel;
    [SerializeField] private TMP_Text txtLangLabel;
    [SerializeField] private TMP_Text txtBottomClose;
    [SerializeField] private Button btnResetProgress;
    [SerializeField] private TMP_Text txtResetProgress;

    [Header("Cached Art Sprites")]
    [SerializeField] private Sprite sprBtnGreen;
    [SerializeField] private Sprite sprBtnYellow;
    [SerializeField] private Sprite sprBtnDisabled;
    [SerializeField] private Sprite sprBtnRed;

    private bool popupInputLockHeld;
    private string currentLanguage = "vi";
    private Coroutine animRoutine;

    public static event Action<string> OnLanguageChanged;

    public static SettingsPopupUI FindOrCreate()
    {
        SettingsPopupUI existing = FindFirstObjectByType<SettingsPopupUI>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        Transform canvasPopup = AvatarProfilePopupUI.FindCanvasPopup();
        if (canvasPopup == null)
        {
            Canvas anyCanvas = FindFirstObjectByType<Canvas>();
            if (anyCanvas != null) canvasPopup = anyCanvas.transform;
        }

        if (canvasPopup == null)
        {
            Debug.LogError("[SettingsPopupUI] Cannot find Canvas for popup.");
            return null;
        }

        return CreateHierarchy(canvasPopup);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentLanguage = PlayerPrefs.GetString(PrefLanguage, "vi");
        BindEvents();

        if (popupRoot == null) popupRoot = gameObject;
        popupRoot.SetActive(false);
    }

    private void Start()
    {
        BindEvents();
        RefreshUI();
    }

    private void OnEnable()
    {
        BindEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void OnDestroy()
    {
        ReleasePopupInputBlock();
    }

    public void OpenPopup()
    {
        if (popupRoot == null) popupRoot = gameObject;
        popupRoot.SetActive(true);
        AcquirePopupInputBlock();
        AudioManager.Instance?.PlayUIClick();

        BindEvents();
        RefreshUI();

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(OpenAnimRoutine());
    }

    public void ClosePopup()
    {
        AudioManager.Instance?.PlayUIClick();
        ReleasePopupInputBlock();
        if (animRoutine != null) StopCoroutine(animRoutine);

        // ⚠️ [VÒNG 13] popupRoot CHÍNH LÀ gameObject này (gán trong Awake), và Awake đã tắt nó.
        // CloseAnimRoutine() kết thúc bằng popupRoot.SetActive(false) — tức coroutine tự tắt
        // object đang chạy chính nó. Gọi ClosePopup() lúc object đã tắt (bấm X hai nhịp, bấm X
        // trùng frame với click nền mờ, hoặc FarmUIManager.ForceCloseAllPopups quét qua) sẽ ném
        // lỗi đỏ "Coroutine couldn't be started because the game object 'Popup_Settings' is
        // inactive!". Đang tắt thì coi như đã đóng — về sạch trạng thái rồi thoát.
        if (popupRoot == null) popupRoot = gameObject;
        if (!popupRoot.activeInHierarchy)
        {
            popupRoot.SetActive(false);
            animRoutine = null;
            return;
        }

        animRoutine = StartCoroutine(CloseAnimRoutine());
    }

    private IEnumerator OpenAnimRoutine()
    {
        if (contentGroup != null) contentGroup.alpha = 0f;
        if (cardRect != null) cardRect.localScale = Vector3.one * 0.85f;

        float t = 0f;
        float dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float k = EaseOutBack(p);

            if (cardRect != null) cardRect.localScale = Vector3.one * k;
            if (contentGroup != null) contentGroup.alpha = Mathf.Clamp01(p * 2f);
            yield return null;
        }

        if (cardRect != null) cardRect.localScale = Vector3.one;
        if (contentGroup != null) contentGroup.alpha = 1f;
        animRoutine = null;
    }

    private IEnumerator CloseAnimRoutine()
    {
        float t = 0f;
        float dur = 0.16f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float k = Mathf.Lerp(1f, 0.82f, p);

            if (cardRect != null) cardRect.localScale = Vector3.one * k;
            if (contentGroup != null) contentGroup.alpha = 1f - p;
            yield return null;
        }

        if (popupRoot != null) popupRoot.SetActive(false);
        animRoutine = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private void Update()
    {
        // Phím tắt tiện ích cho Tester / Developer reset save ngay lập tức trong phiên bản EXE / Editor
        if (Input.GetKeyDown(KeyCode.F8))
        {
            OnResetProgressClicked();
        }
    }

    public void RefreshUI()
    {
        float bgmVal = 1f;
        bool bgmEn = true;
        if (AudioManager.Instance != null)
        {
            bgmVal = AudioManager.Instance.BGMVolume;
            bgmEn = AudioManager.Instance.IsBGMEnabled;
        }
        else
        {
            bgmVal = PlayerPrefs.GetFloat("SETTING_BGM_VOLUME", 1f);
            bgmEn = PlayerPrefs.GetInt("SETTING_BGM_ENABLED", 1) == 1;
        }

        if (sliderBgm != null) sliderBgm.SetValueWithoutNotify(bgmVal);
        UpdateBgmToggleVisual(bgmEn);

        float sfxVal = 1f;
        bool sfxEn = true;
        if (AudioManager.Instance != null)
        {
            sfxVal = AudioManager.Instance.SFXVolume;
            sfxEn = AudioManager.Instance.IsSFXEnabled;
        }
        else
        {
            sfxVal = PlayerPrefs.GetFloat("SETTING_SFX_VOLUME", 1f);
            sfxEn = PlayerPrefs.GetInt("SETTING_SFX_ENABLED", 1) == 1;
        }

        if (sliderSfx != null) sliderSfx.SetValueWithoutNotify(sfxVal);
        UpdateSfxToggleVisual(sfxEn);

        currentLanguage = PlayerPrefs.GetString(PrefLanguage, "vi");
        UpdateLanguageVisual(currentLanguage);
        RefreshLocalizedTexts();
    }

    public void BindEvents()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (btnBottomClose != null)
        {
            btnBottomClose.onClick.RemoveAllListeners();
            btnBottomClose.onClick.AddListener(ClosePopup);
        }

        if (btnResetProgress != null)
        {
            btnResetProgress.onClick.RemoveAllListeners();
            btnResetProgress.onClick.AddListener(OnResetProgressClicked);
        }

        if (sliderBgm != null)
        {
            sliderBgm.onValueChanged.RemoveAllListeners();
            sliderBgm.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (btnToggleBgm != null)
        {
            btnToggleBgm.onClick.RemoveAllListeners();
            btnToggleBgm.onClick.AddListener(OnToggleBgmClicked);
        }

        if (sliderSfx != null)
        {
            sliderSfx.onValueChanged.RemoveAllListeners();
            sliderSfx.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (btnToggleSfx != null)
        {
            btnToggleSfx.onClick.RemoveAllListeners();
            btnToggleSfx.onClick.AddListener(OnToggleSfxClicked);
        }

        if (btnLangVi != null)
        {
            btnLangVi.onClick.RemoveAllListeners();
            btnLangVi.onClick.AddListener(() => SetLanguage("vi"));
        }

        if (btnLangEn != null)
        {
            btnLangEn.onClick.RemoveAllListeners();
            btnLangEn.onClick.AddListener(() => SetLanguage("en"));
        }
    }

    public void OnResetProgressClicked()   // [fix] GameProgressionStudioOverlay goi tu ngoai
    {
        AudioManager.Instance?.PlayUIClick();
        Debug.Log("[Settings] Người chơi yêu cầu Xoá dữ liệu / Chơi lại từ đầu (Cấp 1 & Tân thủ).");

        // 1. Xoá file save.json vật lý trên ổ đĩa
        SaveSystem.DeleteSave();

        // 2. Xoá sạch toàn bộ PlayerPrefs và cờ Tutorial
        PlayerPrefs.DeleteAll();
        TutorialManager.ClearTutorialDoneFlag();
        SaveVersionGuard.ClearAll();
        PlayerPrefs.Save();

        // 3. Xoá dữ liệu trong bộ nhớ RAM
        if (FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.ResetCurrency();
            Destroy(FarmEconomyManager.Instance.gameObject);
        }

        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.ForceSetLevelExp(1, 0);
            Destroy(PlayerProgressManager.Instance.gameObject);
        }

        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.XoaSaveVaLamTrongKho();
            Destroy(WarehouseManager.Instance.gameObject);
        }

        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.ClearAll();
            Destroy(FarmInventoryManager.Instance.gameObject);
        }

        if (KitchenTransferManager.Instance != null)
        {
            Destroy(KitchenTransferManager.Instance.gameObject);
        }

        if (MissionProgressTracker.Instance != null)
        {
            Destroy(MissionProgressTracker.Instance.gameObject);
        }

        if (AnimalGuideController.Instance != null)
        {
            Destroy(AnimalGuideController.Instance.gameObject);
        }

        if (TutorialManager.Instance != null)
        {
            Destroy(TutorialManager.Instance.gameObject);
        }

        if (TownshipHUDController.Instance != null)
        {
            Destroy(TownshipHUDController.Instance.gameObject);
        }

        // Đảm bảo xoá triệt để các cờ kho/starter phát sinh trong quá trình ClearAll
        PlayerPrefs.DeleteKey("FARM_INVENTORY_SAVE");
        PlayerPrefs.DeleteKey("STARTER_ITEMS_GIVEN");
        PlayerPrefs.DeleteKey("TUTORIAL_MAIN_DONE");
        PlayerPrefs.Save();

        // 4. Tải lại scene nông trại sạch sẽ từ đầu
        int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
        UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
    }

    public void RefreshLocalizedTexts()
    {
        if (txtTitle != null) txtTitle.text = Loc.T("CÀI ĐẶT");
        if (txtBgmLabel != null) txtBgmLabel.text = Loc.T("Âm thanh game");
        if (txtSfxLabel != null) txtSfxLabel.text = Loc.T("Âm thanh VFX");
        if (txtLangLabel != null) txtLangLabel.text = Loc.T("Ngôn ngữ");
        if (txtBottomClose != null) txtBottomClose.text = Loc.T("ĐÓNG");
        if (txtResetProgress != null) txtResetProgress.text = Loc.T("CHƠI LẠI TỪ ĐẦU");

        bool bgmEn = AudioManager.Instance != null ? AudioManager.Instance.IsBGMEnabled : (PlayerPrefs.GetInt("SETTING_BGM_ENABLED", 1) == 1);
        UpdateBgmToggleVisual(bgmEn);

        bool sfxEn = AudioManager.Instance != null ? AudioManager.Instance.IsSFXEnabled : (PlayerPrefs.GetInt("SETTING_SFX_ENABLED", 1) == 1);
        UpdateSfxToggleVisual(sfxEn);
    }

    private void OnBgmSliderChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.BGMVolume = val;
            if (val > 0.01f && !AudioManager.Instance.IsBGMEnabled)
            {
                AudioManager.Instance.IsBGMEnabled = true;
                UpdateBgmToggleVisual(true);
            }
        }
        else
        {
            PlayerPrefs.SetFloat("SETTING_BGM_VOLUME", val);
        }
    }

    private void OnToggleBgmClicked()
    {
        AudioManager.Instance?.PlayUIClick();
        if (AudioManager.Instance != null)
        {
            bool nextState = !AudioManager.Instance.IsBGMEnabled;
            AudioManager.Instance.IsBGMEnabled = nextState;
            UpdateBgmToggleVisual(nextState);
        }
        else
        {
            bool cur = PlayerPrefs.GetInt("SETTING_BGM_ENABLED", 1) == 1;
            PlayerPrefs.SetInt("SETTING_BGM_ENABLED", !cur ? 1 : 0);
            UpdateBgmToggleVisual(!cur);
        }
    }

    private void UpdateBgmToggleVisual(bool enabled)
    {
        if (imgToggleBgmBg != null)
        {
            imgToggleBgmBg.sprite = enabled ? GetSpriteBtnGreen() : GetSpriteBtnDisabled();
            imgToggleBgmBg.color = Color.white;
            imgToggleBgmBg.type = Image.Type.Sliced;
        }
        if (txtToggleBgm != null)
        {
            txtToggleBgm.text = enabled ? Loc.T("BẬT") : Loc.T("TẮT");
            txtToggleBgm.color = enabled ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.9f);
        }
    }

    private void OnSfxSliderChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SFXVolume = val;
            if (val > 0.01f && !AudioManager.Instance.IsSFXEnabled)
            {
                AudioManager.Instance.IsSFXEnabled = true;
                UpdateSfxToggleVisual(true);
            }
        }
        else
        {
            PlayerPrefs.SetFloat("SETTING_SFX_VOLUME", val);
        }
    }

    private void OnToggleSfxClicked()
    {
        AudioManager.Instance?.PlayUIClick();
        if (AudioManager.Instance != null)
        {
            bool nextState = !AudioManager.Instance.IsSFXEnabled;
            AudioManager.Instance.IsSFXEnabled = nextState;
            UpdateSfxToggleVisual(nextState);
        }
        else
        {
            bool cur = PlayerPrefs.GetInt("SETTING_SFX_ENABLED", 1) == 1;
            PlayerPrefs.SetInt("SETTING_SFX_ENABLED", !cur ? 1 : 0);
            UpdateSfxToggleVisual(!cur);
        }
    }

    private void UpdateSfxToggleVisual(bool enabled)
    {
        if (imgToggleSfxBg != null)
        {
            imgToggleSfxBg.sprite = enabled ? GetSpriteBtnGreen() : GetSpriteBtnDisabled();
            imgToggleSfxBg.color = Color.white;
            imgToggleSfxBg.type = Image.Type.Sliced;
        }
        if (txtToggleSfx != null)
        {
            txtToggleSfx.text = enabled ? Loc.T("BẬT") : Loc.T("TẮT");
            txtToggleSfx.color = enabled ? Color.white : new Color(0.85f, 0.85f, 0.85f, 0.9f);
        }
    }

    public void SetLanguage(string lang)
    {
        AudioManager.Instance?.PlayUIClick();
        currentLanguage = lang;
        PlayerPrefs.SetString(PrefLanguage, lang);
        PlayerPrefs.Save();
        UpdateLanguageVisual(lang);

        // Đẩy sang LocalizationManager: nó lưu, rồi báo cho mọi LocalizedText
        // trong scene và mọi UI dựng bằng code tự vẽ lại.
        LocalizationManager.SetLanguage(lang);

        RefreshLocalizedTexts();

        OnLanguageChanged?.Invoke(lang);   // giữ lại cho ai đang nghe sự kiện cũ
    }

    private void UpdateLanguageVisual(string lang)
    {
        bool isVi = lang == "vi";

        if (imgLangViBg != null)
        {
            imgLangViBg.sprite = isVi ? GetSpriteBtnYellow() : GetSpriteBtnDisabled();
            imgLangViBg.color = Color.white;
            imgLangViBg.type = Image.Type.Sliced;
        }
        if (imgLangEnBg != null)
        {
            imgLangEnBg.sprite = !isVi ? GetSpriteBtnYellow() : GetSpriteBtnDisabled();
            imgLangEnBg.color = Color.white;
            imgLangEnBg.type = Image.Type.Sliced;
        }

        if (goCheckVi != null) goCheckVi.SetActive(isVi);
        if (goCheckEn != null) goCheckEn.SetActive(!isVi);
    }

    private Sprite GetSpriteBtnGreen() => sprBtnGreen != null ? sprBtnGreen : LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_green_3d.png");
    private Sprite GetSpriteBtnYellow() => sprBtnYellow != null ? sprBtnYellow : LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_yellow_3d.png");
    private Sprite GetSpriteBtnDisabled() => sprBtnDisabled != null ? sprBtnDisabled : LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_disabled_3d.png");

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
    //  UI HIERARCHY BUILDER — 100% ART SPRITES TỪ SOURCE GAME
    // ═════════════════════════════════════════════════════════════════════════

    public static SettingsPopupUI CreateHierarchy(Transform canvas)
    {
        // Tải các Sprite Art chuẩn từ dự án
        Sprite sprWoodFrame     = LoadSprite("Assets/Export_Train_UI_Package/Sprites/popup_frame_wood.png");
        Sprite sprPaperPanel    = LoadSprite("Assets/Export_Train_UI_Package/Sprites/popup_panel_paper.png");
        Sprite sprGoldRibbon    = LoadSprite("Assets/Export_Train_UI_Package/Sprites/ribbon_banner_gold.png");
        Sprite sprBtnCloseRed   = LoadSprite("Assets/Export_Kitchen_UI_Package/Sprites/btn_red_small.png");
        Sprite sprCardDark      = LoadSprite("Assets/Export_Train_UI_Package/Sprites/timer_box_dark.png");
        Sprite sprTrackBar      = LoadSprite("Assets/Export_Train_UI_Package/Sprites/progress_track_bar.png");
        Sprite sprProgressFill  = LoadSprite("Assets/Export_Train_UI_Package/Sprites/progress_fill_green.png");
        Sprite sprHandleDisc    = LoadSprite("Assets/Export_Train_UI_Package/Sprites/icon_disc_large.png");
        Sprite sprBtnGreen3D    = LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_green_3d.png");
        Sprite sprBtnYellow3D   = LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_yellow_3d.png");
        Sprite sprBtnDisabled3D = LoadSprite("Assets/Export_Train_UI_Package/Sprites/btn_disabled_3d.png");
        Sprite sprCheckBadge    = LoadSprite("Assets/Export_Train_UI_Package/Sprites/check_badge_green.png");

        // 1. Root Dim Overlay
        RectTransform root = CreateRect(canvas, "Popup_Settings", new Vector2(1920f, 1080f), Vector2.zero);
        root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;

        Image dim = AddImage(root.gameObject, new Color(0f, 0f, 0f, 0.68f), null, false);
        dim.raycastTarget = true;

        CanvasGroup cg = root.gameObject.AddComponent<CanvasGroup>();
        SettingsPopupUI ui = root.gameObject.AddComponent<SettingsPopupUI>();
        ui.popupRoot = root.gameObject;
        ui.contentGroup = cg;
        ui.sprBtnGreen = sprBtnGreen3D;
        ui.sprBtnYellow = sprBtnYellow3D;
        ui.sprBtnDisabled = sprBtnDisabled3D;

        // 2. Khung Bảng Gỗ Thật 9-Slice (Board 880 x 600)
        RectTransform board = CreateRect(root, "Board", new Vector2(880f, 600f), Vector2.zero);
        ui.cardRect = board;
        Image imgBoard = AddImage(board.gameObject, Color.white, sprWoodFrame, true);
        imgBoard.raycastTarget = true;

        // 3. Tấm giấy kem Parchment 9-Slice bên trong (800 x 440)
        RectTransform parchment = CreateRect(board, "Panel_Parchment", new Vector2(800f, 440f), new Vector2(0f, -22f));
        Image imgParchment = AddImage(parchment.gameObject, Color.white, sprPaperPanel, true);
        imgParchment.raycastTarget = false;

        // 4. Ruy băng tiêu đề Vàng 3D "CÀI ĐẶT" (440 x 96)
        RectTransform ribbon = CreateRect(board, "Ribbon_Header", new Vector2(440f, 96f), new Vector2(0f, 280f));
        AddImage(ribbon.gameObject, Color.white, sprGoldRibbon, true);

        TMP_Text titleTxt = CreateText(ribbon, "Txt_Title", "CÀI ĐẶT", 38, new Color32(255, 255, 255, 255), TextAlignmentOptions.Center, new Vector2(0f, 4f), new Vector2(400f, 60f), FontStyles.Bold);
        AddShadow(titleTxt.gameObject, new Color32(140, 75, 10, 240), new Vector2(1.5f, -2.5f));
        ui.txtTitle = titleTxt;

        // 5. NÚT ĐÓNG [X] ĐỎ 3D (64 x 64)
        RectTransform closeRt = CreateRect(board, "Btn_Close", new Vector2(64f, 64f), new Vector2(410f, 270f));
        AddImage(closeRt.gameObject, Color.white, sprBtnCloseRed, true);
        TMP_Text xTxt = CreateText(closeRt, "Txt_X", "X", 26, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(50f, 50f), FontStyles.Bold);
        AddShadow(xTxt.gameObject, new Color32(90, 15, 20, 220), new Vector2(1f, -2f));
        Button btnClose = closeRt.gameObject.AddComponent<Button>();
        ui.btnClose = btnClose;

        // ── Hàng 1: Âm thanh Game / BGM (Y: 120) ───────────────────────────
        BuildAudioRow(parchment, "Row_BGM", "Âm thanh game", 120f, sprCardDark, sprTrackBar, sprProgressFill, sprHandleDisc, sprBtnGreen3D,
            out Slider bgmSlider, out Button bgmToggleBtn, out Image bgmToggleBg, out TMP_Text bgmToggleTxt, out TMP_Text bgmLabel);
        ui.sliderBgm = bgmSlider;
        ui.btnToggleBgm = bgmToggleBtn;
        ui.imgToggleBgmBg = bgmToggleBg;
        ui.txtToggleBgm = bgmToggleTxt;
        ui.txtBgmLabel = bgmLabel;

        // ── Hàng 2: Âm thanh VFX / SFX (Y: 35) ──────────────────────────────
        BuildAudioRow(parchment, "Row_SFX", "Âm thanh VFX", 35f, sprCardDark, sprTrackBar, sprProgressFill, sprHandleDisc, sprBtnGreen3D,
            out Slider sfxSlider, out Button sfxToggleBtn, out Image sfxToggleBg, out TMP_Text sfxToggleTxt, out TMP_Text sfxLabel);
        ui.sliderSfx = sfxSlider;
        ui.btnToggleSfx = sfxToggleBtn;
        ui.imgToggleSfxBg = sfxToggleBg;
        ui.txtToggleSfx = sfxToggleTxt;
        ui.txtSfxLabel = sfxLabel;

        // ── Hàng 3: Ngôn ngữ / Language (Y: -55) ────────────────────────────
        BuildLanguageRow(parchment, -55f, sprCardDark, sprBtnYellow3D, sprBtnDisabled3D, sprCheckBadge,
            out Button btnVi, out Button btnEn, out Image bgVi, out Image bgEn, out GameObject chkVi, out GameObject chkEn, out TMP_Text langLabel);
        ui.btnLangVi = btnVi;
        ui.btnLangEn = btnEn;
        ui.imgLangViBg = bgVi;
        ui.imgLangEnBg = bgEn;
        ui.goCheckVi = chkVi;
        ui.goCheckEn = chkEn;
        ui.txtLangLabel = langLabel;

        // ── Nút Bấm phía dưới: CHƠI LẠI TỪ ĐẦU (Đỏ) & ĐÓNG (Xanh lá) (Y: -155) ─
        RectTransform resetBtnRt = CreateRect(parchment, "Btn_ResetProgress", new Vector2(250f, 60f), new Vector2(-140f, -152f));
        AddImage(resetBtnRt.gameObject, Color.white, sprBtnCloseRed, true);
        TMP_Text resetTxt = CreateText(resetBtnRt, "Txt_Reset", "CHƠI LẠI TỪ ĐẦU", 18, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(230f, 48f), FontStyles.Bold);
        AddShadow(resetTxt.gameObject, new Color32(90, 15, 20, 240), new Vector2(1.5f, -2f));
        Button resetBtn = resetBtnRt.gameObject.AddComponent<Button>();
        ui.btnResetProgress = resetBtn;
        ui.txtResetProgress = resetTxt;

        RectTransform saveBtnRt = CreateRect(parchment, "Btn_BottomClose", new Vector2(220f, 60f), new Vector2(140f, -152f));
        AddImage(saveBtnRt.gameObject, Color.white, sprBtnGreen3D, true);
        TMP_Text saveTxt = CreateText(saveBtnRt, "Txt_Save", "ĐÓNG", 22, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(200f, 48f), FontStyles.Bold);
        AddShadow(saveTxt.gameObject, new Color32(30, 80, 10, 240), new Vector2(1.5f, -2f));
        Button bottomCloseBtn = saveBtnRt.gameObject.AddComponent<Button>();
        ui.btnBottomClose = bottomCloseBtn;
        ui.txtBottomClose = saveTxt;

        ui.BindEvents();
        ui.RefreshUI();

        return ui;
    }

    private static void BuildAudioRow(RectTransform parent, string name, string label, float posY,
        Sprite sprCard, Sprite sprTrack, Sprite sprFill, Sprite sprHandle, Sprite sprToggleBtn,
        out Slider slider, out Button toggleBtn, out Image toggleBg, out TMP_Text toggleTxt, out TMP_Text lblOut)
    {
        // Khung Recessed Card gỗ tối
        RectTransform row = CreateRect(parent, name, new Vector2(740f, 72f), new Vector2(0f, posY));
        AddImage(row.gameObject, Color.white, sprCard, true);

        // Label bên trái
        TMP_Text lbl = CreateText(row, "Txt_Label", label, 20, new Color32(255, 242, 215, 255), TextAlignmentOptions.Left, new Vector2(-225f, 0f), new Vector2(230f, 40f), FontStyles.Bold);
        AddShadow(lbl.gameObject, new Color32(30, 20, 10, 200), new Vector2(1f, -1.5f));
        lblOut = lbl;

        // Slider ở giữa (Rộng 260)
        RectTransform sliderRt = CreateRect(row, "Slider", new Vector2(260f, 24f), new Vector2(55f, 0f));
        slider = sliderRt.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // Background rãnh trượt
        RectTransform sliderBg = CreateRect(sliderRt, "Background", new Vector2(260f, 18f), Vector2.zero);
        AddImage(sliderBg.gameObject, Color.white, sprTrack, true);

        // Fill Area
        RectTransform fillArea = CreateRect(sliderRt, "Fill Area", new Vector2(260f, 18f), Vector2.zero);
        fillArea.anchorMin = new Vector2(0f, 0.15f); fillArea.anchorMax = new Vector2(1f, 0.85f);
        fillArea.sizeDelta = Vector2.zero;

        RectTransform fill = CreateRect(fillArea, "Fill", new Vector2(0f, 0f), Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.sizeDelta = Vector2.zero;
        AddImage(fill.gameObject, Color.white, sprFill, true);
        slider.fillRect = fill;

        // Handle Slide tròn 3D
        RectTransform handleArea = CreateRect(sliderRt, "Handle Slide Area", new Vector2(260f, 24f), Vector2.zero);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one; handleArea.sizeDelta = Vector2.zero;

        RectTransform handle = CreateRect(handleArea, "Handle", new Vector2(34f, 34f), Vector2.zero);
        Image handleImg = AddImage(handle.gameObject, Color.white, sprHandle, false);
        AddShadow(handle.gameObject, new Color32(40, 25, 10, 200), new Vector2(0f, -2f));
        slider.handleRect = handle;
        slider.targetGraphic = handleImg;

        // Nút Toggle 3D bên phải (BẬT / TẮT)
        RectTransform toggleRt = CreateRect(row, "Btn_Toggle", new Vector2(110f, 48f), new Vector2(285f, 0f));
        toggleBg = AddImage(toggleRt.gameObject, Color.white, sprToggleBtn, true);
        
        toggleTxt = CreateText(toggleRt, "Txt_Toggle", "BẬT", 16, Color.white, TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(100f, 36f), FontStyles.Bold);
        AddShadow(toggleTxt.gameObject, new Color32(25, 65, 10, 220), new Vector2(1f, -1.5f));
        toggleBtn = toggleRt.gameObject.AddComponent<Button>();
    }

    private static void BuildLanguageRow(RectTransform parent, float posY,
        Sprite sprCard, Sprite sprBtnYellow, Sprite sprBtnDisabled, Sprite sprCheckBadge,
        out Button btnVi, out Button btnEn, out Image bgVi, out Image bgEn, out GameObject chkVi, out GameObject chkEn, out TMP_Text lblOut)
    {
        // Khung Recessed Card gỗ tối
        RectTransform row = CreateRect(parent, "Row_Language", new Vector2(740f, 72f), new Vector2(0f, posY));
        AddImage(row.gameObject, Color.white, sprCard, true);

        // Label bên trái
        TMP_Text lbl = CreateText(row, "Txt_Label", "Ngôn ngữ", 20, new Color32(255, 242, 215, 255), TextAlignmentOptions.Left, new Vector2(-225f, 0f), new Vector2(230f, 40f), FontStyles.Bold);
        AddShadow(lbl.gameObject, new Color32(30, 20, 10, 200), new Vector2(1f, -1.5f));
        lblOut = lbl;

        // ═════════════════════════════════════════════════════════════════════
        // Nút Tiếng Việt (Có Cờ Đỏ Sao Vàng 🇻🇳 & Dấu Tích Xanh 3D)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform viRt = CreateRect(row, "Btn_Lang_VI", new Vector2(195f, 52f), new Vector2(50f, 0f));
        bgVi = AddImage(viRt.gameObject, Color.white, sprBtnYellow, true);

        // Cờ Việt Nam 🇻🇳 (Nền đỏ bo góc + Ngôi sao vàng)
        RectTransform flagVi = CreateRect(viRt, "Flag_VN", new Vector2(30f, 22f), new Vector2(-62f, 2f));
        AddImage(flagVi.gameObject, new Color32(218, 37, 29, 255), SkinKit.BoGoc(4f), true);
        RectTransform starImg = CreateRect(flagVi, "Star", new Vector2(10f, 10f), Vector2.zero);
        starImg.localEulerAngles = new Vector3(0f, 0f, 45f);
        AddImage(starImg.gameObject, new Color32(255, 235, 50, 255), SkinKit.BoGoc(2f), true);

        TMP_Text viTxt = CreateText(viRt, "Txt", "Tiếng Việt", 15, new Color32(65, 38, 12, 255), TextAlignmentOptions.Left, new Vector2(-5f, 2f), new Vector2(85f, 36f), FontStyles.Bold);
        AddShadow(viTxt.gameObject, new Color32(255, 245, 200, 200), new Vector2(1f, -1f));

        // Dấu tích xanh 3D góc phải
        RectTransform viCheck = CreateRect(viRt, "Check", new Vector2(28f, 28f), new Vector2(70f, 2f));
        AddImage(viCheck.gameObject, Color.white, sprCheckBadge, false);
        chkVi = viCheck.gameObject;
        btnVi = viRt.gameObject.AddComponent<Button>();

        // ═════════════════════════════════════════════════════════════════════
        // Nút English (Có Cờ English 🇬🇧 & Dấu Tích Xanh 3D)
        // ═════════════════════════════════════════════════════════════════════
        RectTransform enRt = CreateRect(row, "Btn_Lang_EN", new Vector2(195f, 52f), new Vector2(260f, 0f));
        bgEn = AddImage(enRt.gameObject, Color.white, sprBtnDisabled, true);

        // Cờ English 🇬🇧 (Nền xanh đậm + chữ EN sắc nét)
        RectTransform flagEn = CreateRect(enRt, "Flag_EN", new Vector2(30f, 22f), new Vector2(-62f, 2f));
        AddImage(flagEn.gameObject, new Color32(1, 33, 105, 255), SkinKit.BoGoc(4f), true);
        RectTransform flagEnCross = CreateRect(flagEn, "Cross", new Vector2(30f, 6f), Vector2.zero);
        AddImage(flagEnCross.gameObject, new Color32(200, 16, 46, 255), SkinKit.BoGoc(2f), true);
        TMP_Text enBadge = CreateText(flagEn, "EN_Txt", "EN", 10, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(28f, 18f), FontStyles.Bold);
        enBadge.raycastTarget = false;

        TMP_Text enTxt = CreateText(enRt, "Txt", "English", 15, new Color32(65, 38, 12, 255), TextAlignmentOptions.Left, new Vector2(-5f, 2f), new Vector2(85f, 36f), FontStyles.Bold);
        AddShadow(enTxt.gameObject, new Color32(255, 245, 200, 200), new Vector2(1f, -1f));

        // Dấu tích xanh 3D góc phải
        RectTransform enCheck = CreateRect(enRt, "Check", new Vector2(28f, 28f), new Vector2(70f, 2f));
        AddImage(enCheck.gameObject, Color.white, sprCheckBadge, false);
        chkEn = enCheck.gameObject;
        btnEn = enRt.gameObject.AddComponent<Button>();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SPRITE LOADER & HELPERS
    // ═════════════════════════════════════════════════════════════════════════

    public static Sprite LoadSprite(string assetPath)
    {
#if UNITY_EDITOR
        var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (spr != null) return spr;
#endif
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        var resSpr = Resources.Load<Sprite>(fileName);
        if (resSpr != null) return resSpr;
        return Resources.Load<Sprite>($"Icons/{fileName}");
    }

    private static Sprite BoGoc(float r) => SkinKit.BoGoc(r);

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
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite, bool isSliced)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = isSliced ? Image.Type.Sliced : Image.Type.Simple;
        }
        return img;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 boxSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = boxSize;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;

        if (SkinKit.FontVo != null)
        {
            tmp.font = SkinKit.FontVo;
        }

        return tmp;
    }
}
