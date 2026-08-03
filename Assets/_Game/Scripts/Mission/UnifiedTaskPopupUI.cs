using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class UnifiedTaskPopupSprites
{
    [Header("Frame")]
    public Sprite boardFrame;
    public Sprite paperPanel;
    public Sprite ribbon;
    public Sprite closeButton;
    public Sprite tabButton;
    public Sprite selectedTabButton;

    [Header("Tab Icons")]
    public Sprite missionTabIcon;
    public Sprite dailyTabIcon;
    public Sprite achievementTabIcon;

    [Header("Reward Icons")]
    public Sprite coinIcon;
    public Sprite diamondIcon;
    public Sprite expIcon;
    public Sprite chestIcon;
    public Sprite lockIcon;

    [Header("Daily Reward Icons")]
    public Sprite[] dailyRewardIcons = new Sprite[7];

    [Header("Decoration")]
    public Sprite mascot;
    public Sprite leafCluster;
    public Sprite flowerCluster;
}

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class UnifiedTaskPopupUI : MonoBehaviour
{
    public enum Tab
    {
        Mission,
        Daily,
        Achievement
    }

    private struct RewardBundle
    {
        public int coin;
        public int diamond;
        public int exp;

        public RewardBundle(int coin, int diamond, int exp)
        {
            this.coin = coin;
            this.diamond = diamond;
            this.exp = exp;
        }
    }

    private struct DailyReward
    {
        public string title;
        public string amount;
        public RewardBundle grant;

        public DailyReward(string title, string amount, RewardBundle grant)
        {
            this.title = title;
            this.amount = amount;
            this.grant = grant;
        }
    }

    private sealed class TabButtonView
    {
        public Button button;
        public Image background;
        public GameObject pointer;
        public TMP_Text label;
        public Image icon;
    }

    private const string RootName = "UnifiedTaskPopupRoot";
    private const string DailyLastSeenKey = "UNIFIED_TASK_DAILY_LAST_SEEN";
    private const string DailyStreakKey = "UNIFIED_TASK_DAILY_STREAK";
    private const string DailyClaimedDateKey = "UNIFIED_TASK_DAILY_CLAIMED_DATE";
    private const int DefaultMissionCoinReward = 50;
    private const int DefaultMissionDiamondReward = 5;
    private const int DefaultMissionExpReward = 10;
    private const int DefaultAchievementExpReward = 20;

    private static UnifiedTaskPopupUI _instance;
    private static Sprite _roundedSprite;
    private static Sprite _circleSprite;
    private static Sprite _rightTriangleSprite;
    private static Sprite _leftTriangleSprite;

    [SerializeField] private UnifiedTaskPopupSprites sprites = new UnifiedTaskPopupSprites();

    [Header("Databases (để trống = tự lấy từ PopupEwarManager trong scene)")]
    [SerializeField] private MissionDatabase missionDatabase;
    [SerializeField] private MissionDatabase dailyMissionDatabase;
    [SerializeField] private MissionDatabase achievementDatabase;

    private RectTransform _root;
    private RectTransform _board;
    private RectTransform _contentRoot;
    private RectTransform _missionPanel;
    private RectTransform _dailyPanel;
    private RectTransform _achievementPanel;
    private CanvasGroup _canvasGroup;
    private TMP_Text _titleText;
    private TMP_Text _subtitleText;
    private TabButtonView _missionTab;
    private TabButtonView _dailyTab;
    private TabButtonView _achievementTab;
    private MissionDatabase _missionDatabase;
    private MissionDatabase _dailyMissionDatabase;
    private MissionDatabase _achievementDatabase;
    private Tab _currentTab;
    private bool _built;
    private bool _inputLockHeld;

    public static bool IsOpenStatic =>
        _instance != null && _instance._root != null && _instance._root.gameObject.activeSelf;

   
    public static void OpenMission()     => EnsureInstance().OpenInternal(Tab.Mission);
    public static void OpenDaily()       => EnsureInstance().OpenInternal(Tab.Daily);
    public static void OpenAchievement() => EnsureInstance().OpenInternal(Tab.Achievement);
    public static void Open(Tab tab)     => EnsureInstance().OpenInternal(tab);

    public static void CloseIfOpen()
    {
        if (_instance != null)
            _instance.Close();
    }

    public static void RefreshIfOpen()
    {
        if (IsOpenStatic)
            _instance.ShowTab(_instance._currentTab);
    }

    public void SetSprites(UnifiedTaskPopupSprites spriteSet)
    {
        if (spriteSet != null)
            sprites = spriteSet;
    }

    private static UnifiedTaskPopupUI EnsureInstance()
    {
        if (_instance == null)
            _instance = FindFirstObjectByType<UnifiedTaskPopupUI>(FindObjectsInactive.Include);

        if (_instance == null)
        {
            GameObject root = new GameObject(RootName, typeof(RectTransform));
            _instance = root.AddComponent<UnifiedTaskPopupUI>();
        }

        _instance.EnsureParentedToPopupCanvas();
        _instance.BuildIfNeeded();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        EnsureParentedToPopupCanvas();
        BuildIfNeeded();
        if (_root != null)
            _root.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        ReleaseInputBlock();
    }

    private void OpenInternal(Tab tab)
    {
        ResolveDatabases();

        BuildIfNeeded();
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        AcquireInputBlock();
        ShowTab(tab);
    }

    private void Close()
    {
        ReleaseInputBlock();

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        if (_root != null)
            _root.gameObject.SetActive(false);
    }
    private void ResolveDatabases()
    {
        if (missionDatabase != null) _missionDatabase = missionDatabase;
        if (dailyMissionDatabase != null) _dailyMissionDatabase = dailyMissionDatabase;
        if (achievementDatabase != null) _achievementDatabase = achievementDatabase;

        if (_missionDatabase == null || _dailyMissionDatabase == null || _achievementDatabase == null)
        {
            PopupEwarManager ewar = FindFirstObjectByType<PopupEwarManager>(FindObjectsInactive.Include);
            if (ewar != null)
            {
                if (_missionDatabase == null) _missionDatabase = ewar.MissionDatabaseRef;
                if (_dailyMissionDatabase == null) _dailyMissionDatabase = ewar.DailyMissionDatabaseRef;
                if (_achievementDatabase == null) _achievementDatabase = ewar.AchievementMissionDatabaseRef;
            }
        }

        if (_missionDatabase == null)
        {
            MissionHudButtonUI hud = FindFirstObjectByType<MissionHudButtonUI>(FindObjectsInactive.Include);
            if (hud != null) _missionDatabase = hud.MissionDatabaseRef;
        }
    }

    private void EnsureParentedToPopupCanvas()
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null)
            rect = gameObject.AddComponent<RectTransform>();

        GameObject canvasObject = GameObject.Find("Canvas_Popup");
        if (canvasObject == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            canvasObject = canvas != null ? canvas.gameObject : CreateFallbackCanvas();
        }

        if (transform.parent != canvasObject.transform)
            transform.SetParent(canvasObject.transform, false);

        gameObject.name = RootName;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        _root = rect;

        EnsureEventSystem();
    }

    private static GameObject CreateFallbackCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas_Popup", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasObject;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetAsLastSibling();
    }

    private void BuildIfNeeded()
    {
       
        if (_root == null)
            _root = GetComponent<RectTransform>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (_built && _board != null)
            return;

        for (int i = _root.childCount - 1; i >= 0; i--)
        {
            GameObject child = _root.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        Image overlay = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.08f, 0.03f, 0.68f);
        overlay.raycastTarget = true;

        if (gameObject.GetComponent<UIRaycastBlocker>() == null)
            gameObject.AddComponent<UIRaycastBlocker>();

        _board = CreateImage(_root, "Board_WoodFrame", sprites.boardFrame, new Color32(136, 73, 29, 255), new Vector2(0f, -10f), new Vector2(1180f, 720f), true);
        AddOutline(_board.gameObject, new Color32(83, 43, 18, 255), new Vector2(5f, -5f));
        CreateImage(_board, "Board_InnerShadow", null, new Color32(82, 43, 18, 120), new Vector2(0f, -4f), new Vector2(1130f, 660f), true);

        RectTransform paper = CreateImage(_board, "PaperPanel_Main", sprites.paperPanel, new Color32(255, 236, 195, 255), new Vector2(95f, -20f), new Vector2(890f, 595f), true);
        AddOutline(paper.gameObject, new Color32(205, 142, 65, 255), new Vector2(2f, -2f));

        CreateImage(_board, "WoodRail_Left", null, new Color32(118, 64, 29, 255), new Vector2(-460f, -20f), new Vector2(220f, 610f), true);
        BuildDecorations();
        BuildRibbon();
        BuildTabs();

        _contentRoot = CreateRect(_board, "ContentRoot", new Vector2(95f, -30f), new Vector2(860f, 540f));
        _missionPanel = CreateRect(_contentRoot, "Panel_Mission", Vector2.zero, new Vector2(860f, 540f));
        _dailyPanel = CreateRect(_contentRoot, "Panel_Daily", Vector2.zero, new Vector2(860f, 540f));
        _achievementPanel = CreateRect(_contentRoot, "Panel_Achievement", Vector2.zero, new Vector2(860f, 540f));

        Button close = CreateTextButton(_board, "Btn_Close", "X", new Vector2(565f, 300f), new Vector2(78f, 78f), new Color32(230, 92, 53, 255), 42);
        close.image.sprite = sprites.closeButton != null ? sprites.closeButton : GetCircleSprite();
        close.image.type = Image.Type.Simple;
        close.onClick.AddListener(Close);

        _built = true;                       // chốt CUỐI: nếu dựng lỗi giữa chừng sẽ thử lại lần sau
        _root.gameObject.SetActive(false);
    }

    private void BuildRibbon()
    {
        CreateImage(_board, "Ribbon_Tail_Left", null, new Color32(178, 49, 35, 255), new Vector2(-165f, 312f), new Vector2(185f, 72f), true);
        CreateImage(_board, "Ribbon_Tail_Right", null, new Color32(178, 49, 35, 255), new Vector2(165f, 312f), new Vector2(185f, 72f), true);
        RectTransform ribbon = CreateImage(_board, "Ribbon_Title", sprites.ribbon, new Color32(221, 70, 48, 255), new Vector2(0f, 322f), new Vector2(520f, 105f), true);
        AddOutline(ribbon.gameObject, new Color32(140, 34, 24, 255), new Vector2(3f, -3f));
        _titleText = CreateText(ribbon, "Txt_Title", "Nhiệm vụ", 52, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(500f, 90f), FontStyles.Bold);
        AddShadow(_titleText.gameObject, new Color32(120, 32, 22, 255), new Vector2(2f, -3f));
    }

    private void BuildDecorations()
    {
        CreateImage(_board, "Decor_Leaf_TopLeft", sprites.leafCluster, new Color32(101, 158, 45, 255), new Vector2(-420f, 328f), new Vector2(125f, 45f), false);
        CreateImage(_board, "Decor_Flowers_TopLeft", sprites.flowerCluster, new Color32(255, 245, 185, 255), new Vector2(-510f, 330f), new Vector2(80f, 45f), false);
        CreateImage(_board, "Decor_Leaf_TopRight", sprites.leafCluster, new Color32(101, 158, 45, 255), new Vector2(360f, 328f), new Vector2(125f, 45f), false);
        CreateImage(_board, "Decor_Flowers_TopRight", sprites.flowerCluster, new Color32(255, 245, 185, 255), new Vector2(480f, 330f), new Vector2(80f, 45f), false);
        CreateImage(_board, "Decor_Mascot_Placeholder", sprites.mascot, new Color32(235, 154, 91, 255), new Vector2(-470f, -260f), new Vector2(190f, 160f), false);
        CreateText(_board, "Txt_Mascot_Placeholder", "NPC", 28, new Color32(108, 64, 34, 255), TextAlignmentOptions.Center, new Vector2(-470f, -260f), new Vector2(130f, 50f), FontStyles.Bold);
    }

    private void BuildTabs()
    {
        _missionTab = CreateTabButton("Tab_Mission", "Nhiệm vụ", sprites.missionTabIcon, new Vector2(-460f, 190f), Tab.Mission);
        _dailyTab = CreateTabButton("Tab_Daily", "Hằng ngày", sprites.dailyTabIcon, new Vector2(-460f, 20f), Tab.Daily);
        _achievementTab = CreateTabButton("Tab_Achievement", "Thành tựu", sprites.achievementTabIcon, new Vector2(-460f, -150f), Tab.Achievement);
    }

    private TabButtonView CreateTabButton(string name, string label, Sprite iconSprite, Vector2 position, Tab targetTab)
    {
        RectTransform root = CreateImage(_board, name, sprites.tabButton, new Color32(251, 205, 127, 255), position, new Vector2(155f, 142f), true);
        AddOutline(root.gameObject, new Color32(132, 78, 32, 255), new Vector2(2f, -2f));

        Image tabBg = root.GetComponent<Image>();
        tabBg.raycastTarget = true;                   

        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = tabBg;
        button.onClick.AddListener(() => ShowTab(targetTab));

        Image icon = CreateImage(root, "Img_Icon", iconSprite, new Color32(255, 238, 175, 255), new Vector2(0f, 24f), new Vector2(74f, 70f), true).GetComponent<Image>();
        icon.preserveAspect = true;
        TMP_Text text = CreateText(root, "Txt_Label", label, 24, new Color32(92, 50, 25, 255), TextAlignmentOptions.Center, new Vector2(0f, -47f), new Vector2(145f, 36f), FontStyles.Bold);

        RectTransform pointer = CreateImage(root, "Img_SelectedPointer", GetRightTriangleSprite(), new Color32(255, 190, 45, 255), new Vector2(92f, 0f), new Vector2(34f, 54f), false);
        pointer.gameObject.SetActive(false);

        RectTransform notice = CreateImage(root, "Img_RedDot_Placeholder", GetCircleSprite(), new Color32(243, 83, 49, 255), new Vector2(58f, 52f), new Vector2(24f, 24f), false);
        CreateText(notice, "Txt_RedDot", "!", 18, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(20f, 20f), FontStyles.Bold);

        return new TabButtonView
        {
            button = button,
            background = root.GetComponent<Image>(),
            pointer = pointer.gameObject,
            label = text,
            icon = icon
        };
    }

    private void ShowTab(Tab tab)
    {
        _currentTab = tab;
        _missionPanel.gameObject.SetActive(tab == Tab.Mission);
        _dailyPanel.gameObject.SetActive(tab == Tab.Daily);
        _achievementPanel.gameObject.SetActive(tab == Tab.Achievement);

        ApplyTabState(_missionTab, tab == Tab.Mission);
        ApplyTabState(_dailyTab, tab == Tab.Daily);
        ApplyTabState(_achievementTab, tab == Tab.Achievement);

        _titleText.text = tab switch
        {
            Tab.Daily       => "Hằng Ngày",
            Tab.Achievement => "Thành tựu",
            _               => "Nhiệm vụ",
        };

        switch (tab)
        {
            case Tab.Mission:
                BuildMissionContent();
                break;
            case Tab.Daily:
                BuildDailyContent();
                break;
            case Tab.Achievement:
                BuildAchievementContent();
                break;
        }
    }

    private void ApplyTabState(TabButtonView view, bool selected)
    {
        if (view == null)
            return;

        view.background.sprite = selected && sprites.selectedTabButton != null ? sprites.selectedTabButton : sprites.tabButton ?? GetRoundedSprite();
        view.background.color = selected ? new Color32(255, 223, 151, 255) : new Color32(239, 177, 97, 255);
        view.pointer.SetActive(selected);
        view.label.color = selected ? new Color32(87, 43, 18, 255) : new Color32(116, 70, 35, 255);
    }

    private void BuildMissionContent()
    {
        ClearChildren(_missionPanel);
        CreateText(_missionPanel, "Txt_SectionTitle", "Danh sách nhiệm vụ", 30, new Color32(98, 56, 26, 255), TextAlignmentOptions.Center, new Vector2(0f, 245f), new Vector2(700f, 42f), FontStyles.Bold);
        CreateText(_missionPanel, "Txt_SectionSubtitle", "Hoàn thành mục tiêu để mở thưởng trong ngày.", 19, new Color32(124, 83, 43, 255), TextAlignmentOptions.Center, new Vector2(0f, 213f), new Vector2(720f, 34f), FontStyles.Bold);

        RectTransform content = BuildVerticalScroll(_missionPanel, "Mission_ScrollView", new Vector2(0f, -20f), new Vector2(840f, 405f));

        int level = GetPlayerLevel();
        List<MissionData> missions = GetOrderedMissions();
        foreach (MissionData m in missions)
            BuildMissionRow(content, m, m != null && m.requiredLevel > level);

        BuildMissionMilestone(content, missions);
    }

    private void BuildMissionRow(RectTransform parent, MissionData data, bool locked)
    {
        Color32 rowColor = locked ? new Color32(228, 222, 210, 255) : new Color32(255, 245, 223, 255);
        RectTransform row = CreateImage(parent, "Mission_Row", null, rowColor, Vector2.zero, new Vector2(805f, 92f), true);
        AddOutline(row.gameObject, new Color32(224, 174, 95, 255), new Vector2(1.5f, -1.5f));
        AddLayoutHeight(row, 92f);

        RectTransform iconFrame = CreateImage(row, "IconFrame_Task", GetCircleSprite(), new Color32(181, 101, 41, 255), new Vector2(-350f, 0f), new Vector2(76f, 76f), false);
        Image icon = CreateImage(iconFrame, "Img_TaskIcon", data != null ? data.missionIcon : null, new Color32(255, 230, 148, 255), Vector2.zero, new Vector2(62f, 62f), false).GetComponent<Image>();
        icon.sprite = data != null && data.missionIcon != null ? data.missionIcon : GetCircleSprite();
        icon.preserveAspect = true;
        if (locked) icon.color = new Color(1f, 1f, 1f, 0.5f);

        string title = data != null ? data.missionName : "Thêm nhiệm vụ mới";
        CreateText(row, "Txt_TaskTitle", title, 22, locked ? new Color32(132, 112, 92, 255) : new Color32(88, 48, 23, 255), TextAlignmentOptions.Left, new Vector2(-235f, 20f), new Vector2(260f, 32f), FontStyles.Bold);

        int current = data != null ? Mathf.Clamp(MissionProgressTracker.GetProgressFor(data), 0, Mathf.Max(1, data.targetAmount)) : 0;
        int target = data != null ? Mathf.Max(1, data.targetAmount) : 1;
        float progress = (!locked && data != null) ? Mathf.Clamp01((float)current / target) : 0f;
        bool claimed = data != null && IsMissionClaimed(data);
        bool canClaim = !locked && data != null && !claimed && current >= target;

        BuildProgressBar(row, "Progress", new Vector2(-235f, -20f), new Vector2(235f, 26f), progress, locked ? "Khoá" : $"{current}/{target}");

        RewardBundle rewards = GetMissionRewards(data);
        BuildRewardSlot(row, "RewardSlot_Coin", sprites.coinIcon ?? data?.rewardIcon, "x" + rewards.coin, new Vector2(48f, 0f), new Vector2(90f, 58f), new Color32(255, 228, 166, 255));
        BuildRewardSlot(row, "RewardSlot_Diamond", sprites.diamondIcon, "x" + rewards.diamond, new Vector2(155f, 0f), new Vector2(90f, 58f), new Color32(255, 228, 166, 255));
        BuildRewardSlot(row, "RewardSlot_EXP", sprites.expIcon, "x" + rewards.exp, new Vector2(262f, 0f), new Vector2(90f, 58f), new Color32(255, 228, 166, 255));

        if (locked)
        {
            int lv = data != null ? data.requiredLevel : 0;
            Button lockBtn = CreateTextButton(row, "Btn_Locked", $"Mở cấp {lv}", new Vector2(372f, 0f), new Vector2(102f, 58f), new Color32(150, 150, 150, 255), 17);
            lockBtn.interactable = false;
        }
        else
        {
            string buttonText = claimed ? "Xong" : canClaim ? "Nhận" : "Đi";
            Color32 buttonColor = canClaim ? new Color32(93, 181, 35, 255) : claimed ? new Color32(142, 164, 126, 255) : new Color32(167, 167, 167, 255);
            Button action = CreateTextButton(row, "Btn_Go", buttonText, new Vector2(372f, 0f), new Vector2(102f, 58f), buttonColor, 26);
            action.interactable = canClaim;
            RectTransform actionRect = action.transform as RectTransform;
            action.onClick.AddListener(() => ClaimMission(data, actionRect));
        }
    }

    private void BuildMissionMilestone(RectTransform parent, List<MissionData> visibleMissions)
    {
        RectTransform milestone = CreateImage(parent, "Mission_MilestoneReward", null, new Color32(255, 202, 96, 255), Vector2.zero, new Vector2(805f, 120f), true);
        AddLayoutHeight(milestone, 120f);
        AddOutline(milestone.gameObject, new Color32(199, 128, 43, 255), new Vector2(2f, -2f));
        CreateText(milestone, "Txt_Title", "Phần thưởng mốc", 26, new Color32(99, 54, 22, 255), TextAlignmentOptions.Left, new Vector2(-265f, 27f), new Vector2(270f, 36f), FontStyles.Bold);
        CreateText(milestone, "Txt_Desc", "Hoàn thành tất cả nhiệm vụ để nhận phần thưởng đặc biệt!", 18, new Color32(99, 54, 22, 255), TextAlignmentOptions.Left, new Vector2(-245f, -20f), new Vector2(315f, 56f), FontStyles.Bold);

        int completed = 0;
        for (int i = 0; i < visibleMissions.Count; i++)
        {
            MissionData mission = visibleMissions[i];
            if (mission != null && MissionProgressTracker.GetProgressFor(mission) >= mission.targetAmount)
                completed++;
        }

        RectTransform chest = CreateImage(milestone, "Img_ChestReward_Placeholder", sprites.chestIcon, new Color32(188, 89, 46, 255), new Vector2(92f, 2f), new Vector2(128f, 92f), false);
        CreateText(chest, "Txt_Chest", "CHEST", 20, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 40f), FontStyles.Bold);
        BuildRewardSlot(milestone, "RewardSlot_Coin", sprites.coinIcon, "x200", new Vector2(240f, -2f), new Vector2(92f, 78f), new Color32(255, 217, 132, 255));
        BuildRewardSlot(milestone, "RewardSlot_Diamond", sprites.diamondIcon, "x20", new Vector2(350f, -2f), new Vector2(92f, 78f), new Color32(255, 217, 132, 255));
        BuildRewardSlot(milestone, "RewardSlot_EXP", sprites.expIcon, "x50", new Vector2(460f, -2f), new Vector2(92f, 78f), new Color32(255, 217, 132, 255));
        CreateText(milestone, "Txt_MilestoneProgress", $"{completed}/{Mathf.Max(1, visibleMissions.Count)}", 20, new Color32(119, 67, 24, 255), TextAlignmentOptions.Center, new Vector2(-26f, -43f), new Vector2(120f, 28f), FontStyles.Bold);
    }

    private void BuildDailyContent()
    {
        ClearChildren(_dailyPanel);
        DailyState state = SyncDailyState();

        CreateText(_dailyPanel, "Txt_DailyTitle", "Điểm danh 7 ngày", 34, new Color32(93, 52, 24, 255), TextAlignmentOptions.Center, new Vector2(0f, 232f), new Vector2(620f, 44f), FontStyles.Bold);
        CreateText(_dailyPanel, "Txt_DailySubtitle", "Điểm danh mỗi ngày để nhận phần thưởng hấp dẫn!", 20, new Color32(113, 72, 36, 255), TextAlignmentOptions.Center, new Vector2(0f, 194f), new Vector2(680f, 34f), FontStyles.Bold);
        CreateImage(_dailyPanel, "Decor_Line_Left", null, new Color32(207, 147, 73, 255), new Vector2(-220f, 229f), new Vector2(110f, 4f), true);
        CreateImage(_dailyPanel, "Decor_Line_Right", null, new Color32(207, 147, 73, 255), new Vector2(220f, 229f), new Vector2(110f, 4f), true);

        DailyReward[] rewards = GetDailyRewards();
        const float spacing = 116f;
        for (int i = 0; i < 7; i++)
        {
            float x = -348f + i * spacing;
            BuildDailyCard(i + 1, rewards[i], state, new Vector2(x, 50f));
        }

        BuildDailyWeeklyReward();
    }

    private void BuildDailyCard(int day, DailyReward reward, DailyState state, Vector2 position)
    {
        bool isPast = day < state.streakDay || (day == state.streakDay && state.claimedToday);
        bool isToday = day == state.streakDay && !state.claimedToday;
        bool isFuture = day > state.streakDay;
        bool finalDay = day == 7;

        Color32 cardColor = finalDay ? new Color32(244, 215, 255, 255) : new Color32(255, 238, 206, 255);
        if (isToday)
            cardColor = new Color32(255, 245, 200, 255);

        RectTransform card = CreateImage(_dailyPanel, $"Daily_Day_{day:00}", null, cardColor, position, new Vector2(finalDay ? 122f : 108f, 240f), true);
        AddOutline(card.gameObject, isToday ? new Color32(255, 212, 47, 255) : new Color32(216, 157, 82, 255), new Vector2(2f, -2f));

        if (isToday)
            CreateImage(card, "Glow_SelectedDay", null, new Color32(255, 234, 74, 65), Vector2.zero, new Vector2(finalDay ? 134f : 120f, 252f), true);

        CreateText(card, "Txt_Day", $"Ngày {day}", 20, finalDay ? Color.white : new Color32(91, 54, 28, 255), TextAlignmentOptions.Center, new Vector2(0f, 92f), new Vector2(100f, 30f), FontStyles.Bold);
        if (finalDay)
            CreateImage(card, "Ribbon_Day7", null, new Color32(150, 79, 202, 255), new Vector2(0f, 94f), new Vector2(116f, 38f), true).SetAsFirstSibling();

        Image icon = CreateImage(card, "Img_RewardIcon", GetDailyRewardSprite(day), new Color32(245, 182, 67, 255), new Vector2(0f, 22f), new Vector2(finalDay ? 86f : 72f, finalDay ? 78f : 70f), false).GetComponent<Image>();
        icon.preserveAspect = true;
        CreateText(card, "Txt_Amount", reward.amount, 22, new Color32(85, 49, 25, 255), TextAlignmentOptions.Center, new Vector2(0f, -54f), new Vector2(90f, 30f), FontStyles.Bold);

        if (isPast)
        {
            RectTransform check = CreateImage(card, "Img_ClaimedCheck", GetCircleSprite(), new Color32(86, 173, 48, 255), new Vector2(40f, 93f), new Vector2(26f, 26f), false);
            CreateText(check, "Txt_Check", "✓", 20, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(24f, 24f), FontStyles.Bold);
            CreateStatusRibbon(card, "Đã nhận", new Color32(91, 160, 45, 255));
        }
        else if (isToday)
        {
            Button claim = CreateTextButton(card, "Btn_ClaimToday", "Nhận", new Vector2(0f, -91f), new Vector2(86f, 42f), new Color32(104, 186, 45, 255), 22);
            RectTransform claimRect = claim.transform as RectTransform;
            claim.onClick.AddListener(() => ClaimDailyReward(day, reward, claimRect));
        }
        else if (isFuture)
        {
            string lockText = day == state.streakDay + 1 ? "Ngày mai" : $"{day - state.streakDay} ngày nữa";
            CreateStatusRibbon(card, lockText, new Color32(198, 164, 120, 255));
            CreateImage(card, "Img_Lock", sprites.lockIcon, new Color32(132, 92, 56, 255), new Vector2(-34f, -91f), new Vector2(24f, 24f), false);
        }
    }

    private void BuildDailyWeeklyReward()
    {
        RectTransform weekly = CreateImage(_dailyPanel, "Daily_WeeklyReward", null, new Color32(255, 205, 103, 255), new Vector2(0f, -260f), new Vector2(805f, 120f), true);
        AddOutline(weekly.gameObject, new Color32(203, 130, 44, 255), new Vector2(2f, -2f));
        CreateText(weekly, "Txt_Title", "Phần thưởng tuần", 27, new Color32(91, 52, 24, 255), TextAlignmentOptions.Left, new Vector2(-260f, 28f), new Vector2(270f, 34f), FontStyles.Bold);
        CreateText(weekly, "Txt_Desc", "Điểm danh đủ 7 ngày để nhận quà tuần đặc biệt!", 19, new Color32(91, 52, 24, 255), TextAlignmentOptions.Left, new Vector2(-244f, -20f), new Vector2(318f, 54f), FontStyles.Bold);
        RectTransform chest = CreateImage(weekly, "Img_WeeklyChest_Placeholder", sprites.chestIcon, new Color32(173, 73, 157, 255), new Vector2(92f, 0f), new Vector2(130f, 92f), false);
        CreateText(chest, "Txt_Chest", "CHEST", 20, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 40f), FontStyles.Bold);
        BuildRewardSlot(weekly, "RewardSlot_Coin", sprites.coinIcon, "x500", new Vector2(245f, -2f), new Vector2(92f, 78f), new Color32(255, 218, 133, 255));
        BuildRewardSlot(weekly, "RewardSlot_Diamond", sprites.diamondIcon, "x30", new Vector2(360f, -2f), new Vector2(92f, 78f), new Color32(255, 218, 133, 255));
        BuildRewardSlot(weekly, "RewardSlot_EXP", sprites.expIcon, "x100", new Vector2(475f, -2f), new Vector2(92f, 78f), new Color32(255, 218, 133, 255));
    }

    private void BuildAchievementContent()
    {
        ClearChildren(_achievementPanel);
        CreateText(_achievementPanel, "Txt_AchievementTitle", "Thành tựu trang trại", 34, new Color32(93, 52, 24, 255), TextAlignmentOptions.Center, new Vector2(0f, 238f), new Vector2(620f, 44f), FontStyles.Bold);
        CreateText(_achievementPanel, "Txt_AchievementSubtitle", "Hoàn thành các cột mốc để nhận thưởng lâu dài!", 20, new Color32(113, 72, 36, 255), TextAlignmentOptions.Center, new Vector2(0f, 201f), new Vector2(680f, 34f), FontStyles.Bold);

        RectTransform content = BuildVerticalScroll(_achievementPanel, "Achievement_ScrollView", new Vector2(0f, -20f), new Vector2(840f, 405f));

        List<MissionData> achievements = GetOrderedAchievements(200);
        foreach (MissionData a in achievements)
            BuildAchievementRow(content, a);

        BuildAchievementMilestone(content, achievements);
    }

    private void BuildAchievementRow(RectTransform parent, MissionData data)
    {
        RectTransform row = CreateImage(parent, "Achievement_Row", null, new Color32(255, 245, 223, 255), Vector2.zero, new Vector2(805f, 74f), true);
        AddOutline(row.gameObject, new Color32(224, 174, 95, 255), new Vector2(1.3f, -1.3f));
        AddLayoutHeight(row, 74f);

        RectTransform iconFrame = CreateImage(row, "IconFrame_Achievement", GetCircleSprite(), new Color32(181, 101, 41, 255), new Vector2(-350f, 0f), new Vector2(62f, 62f), false);
        Image icon = CreateImage(iconFrame, "Img_AchievementIcon", data != null ? data.missionIcon : null, new Color32(255, 230, 148, 255), Vector2.zero, new Vector2(50f, 50f), false).GetComponent<Image>();
        icon.sprite = data != null && data.missionIcon != null ? data.missionIcon : GetCircleSprite();
        icon.preserveAspect = true;

        string title = data != null ? data.missionName : "Thêm thành tựu mới";
        CreateText(row, "Txt_Title", title, 21, new Color32(88, 48, 23, 255), TextAlignmentOptions.Left, new Vector2(-245f, 16f), new Vector2(255f, 28f), FontStyles.Bold);

        int current = data != null ? Mathf.Clamp(MissionProgressTracker.GetProgressFor(data), 0, Mathf.Max(1, data.targetAmount)) : 0;
        int target = data != null ? Mathf.Max(1, data.targetAmount) : 1;
        float progress = data != null ? Mathf.Clamp01((float)current / target) : 0f;
        BuildProgressBar(row, "Progress", new Vector2(-244f, -18f), new Vector2(225f, 23f), progress, $"{current}/{target}");

        RewardBundle rewards = GetAchievementRewards(data);
        Sprite mainRewardIcon = data != null && data.rewardType == RewardType.Diamond ? sprites.diamondIcon ?? data.rewardIcon : sprites.coinIcon ?? data?.rewardIcon;
        string mainRewardText = data != null && data.rewardType == RewardType.Diamond ? "x" + rewards.diamond : "x" + rewards.coin;
        BuildRewardSlot(row, "RewardSlot_Main", mainRewardIcon, mainRewardText, new Vector2(76f, 0f), new Vector2(116f, 54f), new Color32(255, 228, 166, 255));
        BuildRewardSlot(row, "RewardSlot_EXP", sprites.expIcon, "x" + rewards.exp, new Vector2(204f, 0f), new Vector2(116f, 54f), new Color32(255, 228, 166, 255));

        bool claimed = data != null && IsAchievementClaimed(data);
        bool complete = data != null && current >= target;
        bool started = data != null && current > 0;

        string statusText;
        Color32 statusColor;
        bool interactable;
        if (claimed)
        {
            statusText = "Đã nhận";
            statusColor = new Color32(134, 161, 122, 255);
            interactable = false;
        }
        else if (complete)
        {
            statusText = "Nhận";
            statusColor = new Color32(92, 177, 40, 255);
            interactable = true;
        }
        else if (started)
        {
            statusText = "Đang làm";
            statusColor = new Color32(219, 178, 128, 255);
            interactable = false;
        }
        else
        {
            statusText = "Khóa";
            statusColor = new Color32(155, 155, 155, 255);
            interactable = false;
        }

        Button status = CreateTextButton(row, "Btn_Status", statusText, new Vector2(366f, 0f), new Vector2(116f, 52f), statusColor, 22);
        status.interactable = interactable;
        RectTransform statusRect = status.transform as RectTransform;
        status.onClick.AddListener(() => ClaimAchievement(data, statusRect));
    }

    private void BuildAchievementMilestone(RectTransform parent, List<MissionData> achievements)
    {
        RectTransform milestone = CreateImage(parent, "Achievement_MilestoneReward", null, new Color32(255, 203, 101, 255), Vector2.zero, new Vector2(805f, 112f), true);
        AddOutline(milestone.gameObject, new Color32(203, 130, 44, 255), new Vector2(2f, -2f));
        AddLayoutHeight(milestone, 112f);
        CreateText(milestone, "Txt_Title", "Mốc thành tựu", 26, new Color32(91, 52, 24, 255), TextAlignmentOptions.Left, new Vector2(-270f, 26f), new Vector2(270f, 34f), FontStyles.Bold);
        CreateText(milestone, "Txt_Desc", "Tích lũy điểm thành tựu để mở rương thưởng!", 18, new Color32(91, 52, 24, 255), TextAlignmentOptions.Left, new Vector2(-250f, -14f), new Vector2(320f, 44f), FontStyles.Bold);

        int completed = 0;
        for (int i = 0; i < achievements.Count; i++)
        {
            MissionData data = achievements[i];
            if (data != null && MissionProgressTracker.GetProgressFor(data) >= data.targetAmount)
                completed++;
        }

        int points = Mathf.Clamp(completed * 100, 0, 500);
        BuildProgressBar(milestone, "AchievementPointProgress", new Vector2(-245f, -42f), new Vector2(245f, 24f), points / 500f, $"{points}/500");
        RectTransform chest = CreateImage(milestone, "Img_AchievementChest_Placeholder", sprites.chestIcon, new Color32(173, 73, 157, 255), new Vector2(98f, 0f), new Vector2(126f, 86f), false);
        CreateText(chest, "Txt_Chest", "CHEST", 20, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 40f), FontStyles.Bold);
        BuildRewardSlot(milestone, "RewardSlot_Coin", sprites.coinIcon, "x500", new Vector2(238f, -2f), new Vector2(86f, 74f), new Color32(255, 218, 133, 255));
        BuildRewardSlot(milestone, "RewardSlot_Diamond", sprites.diamondIcon, "x30", new Vector2(342f, -2f), new Vector2(86f, 74f), new Color32(255, 218, 133, 255));
        BuildRewardSlot(milestone, "RewardSlot_EXP", sprites.expIcon, "x100", new Vector2(446f, -2f), new Vector2(86f, 74f), new Color32(255, 218, 133, 255));
        BuildRewardSlot(milestone, "RewardSlot_Trophy", null, "x1", new Vector2(548f, -2f), new Vector2(86f, 74f), new Color32(255, 218, 133, 255));
    }

    private List<MissionData> GetVisibleMissions(int maxCount)
        => GetVisibleFrom(_missionDatabase, maxCount);

    private List<MissionData> GetVisibleAchievements(int maxCount)
        => GetVisibleFrom(_achievementDatabase != null ? _achievementDatabase : _missionDatabase, maxCount);


    private List<MissionData> GetOrderedAchievements(int maxCount)
    {
        var db = _achievementDatabase != null ? _achievementDatabase : _missionDatabase;
        var b0 = new List<MissionData>(); // hoàn thành, chưa nhận
        var b1 = new List<MissionData>(); // đang làm
        var b2 = new List<MissionData>(); // đã nhận

        if (db != null && db.missions != null)
        {
            foreach (var m in db.missions)
            {
                if (m == null || m.isDaily) continue;
                int cur = MissionProgressTracker.GetProgressFor(m);
                bool complete = cur >= Mathf.Max(1, m.targetAmount);
                if (IsAchievementClaimed(m)) b2.Add(m);
                else if (complete)          b0.Add(m);
                else                        b1.Add(m);
            }
            b0.Sort((a, b) => a.targetAmount.CompareTo(b.targetAmount));
            b1.Sort((a, b) => a.targetAmount.CompareTo(b.targetAmount));
            b2.Sort((a, b) => a.targetAmount.CompareTo(b.targetAmount));
        }

        var result = new List<MissionData>(b0.Count + b1.Count + b2.Count);
        result.AddRange(b0); result.AddRange(b1); result.AddRange(b2);
        if (result.Count > maxCount) result = result.GetRange(0, maxCount);
        return result;
    }

    private List<MissionData> GetVisibleFrom(MissionDatabase database, int maxCount)
    {
        List<MissionData> result = new List<MissionData>();
        if (database == null || database.missions == null)
            return result;

        int level = GetPlayerLevel();
        for (int i = 0; i < database.missions.Count; i++)
        {
            MissionData data = database.missions[i];
            if (data == null || data.isDaily || data.requiredLevel > level)
                continue;

            result.Add(data);
            if (result.Count >= maxCount)
                break;
        }

        return result;
    }

    // =========================================================================
    // Danh sách CUỘN ĐƯỢC (ScrollRect dọc) — dùng cho tab Nhiệm vụ & Thành tựu
    // =========================================================================

    /// <summary>Dựng ScrollRect dọc: View(ScrollRect) → Viewport(RectMask2D + raycast) →
    /// Content(VerticalLayoutGroup + ContentSizeFitter). Trả về Content để spawn item vào.</summary>
    private RectTransform BuildVerticalScroll(RectTransform parent, string name, Vector2 center, Vector2 size)
    {
        RectTransform view = CreateRect(parent, name, center, size);
        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 35f;

        // Viewport: clip nội dung + nhận kéo cuộn ở vùng trống.
        RectTransform viewport = CreateRect(view, "Viewport", Vector2.zero, size);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero; viewport.offsetMax = Vector2.zero;
        var vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0f);
        vpImg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        // Content: top-anchored, width theo viewport, cao tự giãn theo số item.
        RectTransform content = CreateRect(viewport, "Content", Vector2.zero, new Vector2(size.x, size.y));
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, size.y);

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(0, 0, 8, 8);
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    /// <summary>Gắn LayoutElement cố định chiều cao 1 dòng (để VerticalLayoutGroup xếp đúng).</summary>
    private static void AddLayoutHeight(RectTransform rt, float height)
    {
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleHeight = 0f;
    }

   
    private List<MissionData> GetOrderedMissions()
    {
        var b0 = new List<MissionData>(); var b1 = new List<MissionData>();
        var b2 = new List<MissionData>(); var b3 = new List<MissionData>();

        if (_missionDatabase != null && _missionDatabase.missions != null)
        {
            int level = GetPlayerLevel();
            foreach (var m in _missionDatabase.missions)
            {
                if (m == null || m.isDaily) continue;
                switch (MissionBucket(m, level))
                {
                    case 0: b0.Add(m); break;
                    case 1: b1.Add(m); break;
                    case 2: b2.Add(m); break;
                    default: b3.Add(m); break;
                }
            }
            b3.Sort((a, b) => a.requiredLevel.CompareTo(b.requiredLevel));
        }


        var result = new List<MissionData>(b0.Count + b1.Count + b2.Count + b3.Count);
        result.AddRange(b1); result.AddRange(b0); result.AddRange(b2); result.AddRange(b3);
        return result;
    }

    private int MissionBucket(MissionData m, int level)
    {
        if (m.requiredLevel > level) return 3;           
        int cur = MissionProgressTracker.GetProgressFor(m);
        bool complete = cur >= Mathf.Max(1, m.targetAmount);
        if (!complete) return 0;                              
        return IsMissionClaimed(m) ? 2 : 1;               
    }

    private void BuildProgressBar(RectTransform parent, string name, Vector2 position, Vector2 size, float fillAmount, string label)
    {
        RectTransform root = CreateImage(parent, name, null, new Color32(231, 195, 145, 255), position, size, true);
        RectTransform mask = CreateRect(root, "Mask", Vector2.zero, size - new Vector2(4f, 4f));
        Image maskImage = mask.gameObject.AddComponent<Image>();
        maskImage.sprite = GetRoundedSprite();
        maskImage.type = Image.Type.Sliced;
        maskImage.color = Color.white;
        maskImage.raycastTarget = false;
        Mask maskComponent = mask.gameObject.AddComponent<Mask>();
        maskComponent.showMaskGraphic = false;

        RectTransform fill = CreateImage(mask, "Fill", null, new Color32(105, 186, 48, 255), Vector2.zero, size - new Vector2(6f, 6f), true);
        fill.anchorMin = new Vector2(0f, 0.5f);
        fill.anchorMax = new Vector2(0f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.anchoredPosition = new Vector2(-size.x * 0.5f + 3f, 0f);
        fill.sizeDelta = new Vector2(Mathf.Max(4f, (size.x - 6f) * Mathf.Clamp01(fillAmount)), size.y - 6f);

        TMP_Text text = CreateText(root, "Txt_Progress", label, Mathf.RoundToInt(size.y * 0.75f), Color.white, TextAlignmentOptions.Center, Vector2.zero, size, FontStyles.Bold);
        AddShadow(text.gameObject, new Color32(90, 48, 24, 255), new Vector2(1.2f, -1.2f));
    }

    private void BuildRewardSlot(RectTransform parent, string name, Sprite iconSprite, string amount, Vector2 position, Vector2 size, Color32 color)
    {
        RectTransform slot = CreateImage(parent, name, null, color, position, size, true);
        AddOutline(slot.gameObject, new Color32(220, 171, 92, 255), new Vector2(1f, -1f));
        Image icon = CreateImage(slot, "Img_Icon", iconSprite, new Color32(240, 174, 45, 255), new Vector2(-size.x * 0.22f, 2f), new Vector2(size.y * 0.66f, size.y * 0.66f), false).GetComponent<Image>();
        icon.preserveAspect = true;
        if (iconSprite == null)
            icon.color = new Color(1f, 1f, 1f, 0f);   // chưa gán sprite → ẩn icon (tránh ★ thiếu glyph trong font)

        CreateText(slot, "Txt_Amount", amount, Mathf.RoundToInt(size.y * 0.34f), new Color32(87, 48, 24, 255), TextAlignmentOptions.Center, new Vector2(size.x * 0.22f, -1f), new Vector2(size.x * 0.48f, size.y * 0.72f), FontStyles.Bold);
    }

    private void CreateStatusRibbon(RectTransform parent, string text, Color32 color)
    {
        RectTransform ribbon = CreateImage(parent, "StatusRibbon", null, color, new Vector2(0f, -91f), new Vector2(90f, 36f), true);
        CreateText(ribbon, "Txt_Status", text, 17, Color.white, TextAlignmentOptions.Center, Vector2.zero, new Vector2(86f, 32f), FontStyles.Bold);
    }

    private void ClaimMission(MissionData data, RectTransform source)
    {
        if (data == null || IsMissionClaimed(data))
            return;

        int current = MissionProgressTracker.GetProgressFor(data);
        if (current < data.targetAmount)
            return;

        RewardBundle rewards = GetMissionRewards(data);
        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(rewards);
        PlayRewardFly(rewards, src);
        PlayerPrefs.SetInt(MissionClaimedPrefsKey(data), 1);
        PlayerPrefs.Save();
        AvatarProfilePopupUI.AddAchievementCount();
        ShowTab(Tab.Mission);
    }

    private void ClaimAchievement(MissionData data, RectTransform source)
    {
        if (data == null || IsAchievementClaimed(data))
            return;

        int current = MissionProgressTracker.GetProgressFor(data);
        if (current < data.targetAmount)
            return;

        RewardBundle rewards = GetAchievementRewards(data);
        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(rewards);
        PlayRewardFly(rewards, src);
        PlayerPrefs.SetInt(AchievementClaimedPrefsKey(data), 1);
        PlayerPrefs.Save();
        AvatarProfilePopupUI.AddAchievementCount();
        ShowTab(Tab.Achievement);
    }

    private void ClaimDailyReward(int day, DailyReward reward, RectTransform source)
    {
        DailyState state = SyncDailyState();
        if (state.claimedToday || day != state.streakDay)
            return;

        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(reward.grant);
        PlayRewardFly(reward.grant, src);
        PlayerPrefs.SetString(DailyClaimedDateKey, TodayKey());
        PlayerPrefs.Save();
        ShowTab(Tab.Daily);
    }

    private static void GrantRewards(RewardBundle rewards)
    {
        // Cộng vào ví CHÍNH (FarmEconomyManager = top-bar 740/677), KHÔNG dùng PlayerWallet (ví mồ côi).
        if (rewards.coin > 0)
            FarmEconomyManager.Instance?.AddGold(rewards.coin);     // tự kích CoinFlyFX → vàng bay về ví
        if (rewards.diamond > 0)
            FarmEconomyManager.Instance?.AddGems(rewards.diamond);
        if (rewards.exp > 0)
            PlayerProgressManager.Instance?.AddExp(rewards.exp);
    }

    // =========================================================================
    // Reward Fly FX — "spam" icon bay từ nút Nhận về đúng ô HUD
    // =========================================================================

    /// <summary>Bay phần thưởng về HUD. Vàng đã tự bay nhờ CoinFlyFX (OnGoldAddedFx) →
    /// ở đây chỉ bay kim cương + EXP về ô tương ứng.</summary>
    private void PlayRewardFly(RewardBundle r, Vector3 sourceWorld)
    {
        if (r.diamond > 0)
            StartCoroutine(CoFlyReward(sprites.diamondIcon, new Color32(120, 205, 255, 255),
                sourceWorld, FindHudRect("GemBox"), Mathf.Clamp(r.diamond, 3, 8)));
        if (r.exp > 0)
            StartCoroutine(CoFlyReward(sprites.expIcon, new Color32(120, 220, 80, 255),
                sourceWorld, ResolveExpHud(), Mathf.Clamp(r.exp / 4 + 3, 3, 8)));
    }

    private static RectTransform FindHudRect(string objName)
    {
        GameObject go = GameObject.Find(objName);
        return go != null ? go.transform as RectTransform : null;
    }

    private static RectTransform ResolveExpHud()
    {
        TopBarExpUI bar = FindFirstObjectByType<TopBarExpUI>(FindObjectsInactive.Include);
        return bar != null ? bar.IconExp : null;
    }

    private IEnumerator CoFlyReward(Sprite icon, Color fallbackColor, Vector3 sourceWorld, RectTransform target, int count)
    {
        Canvas canvas = _root != null ? _root.GetComponentInParent<Canvas>() : null;
        if (canvas == null) yield break;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(uiCam, sourceWorld);
        Vector2 endScreen = target != null
            ? RectTransformUtility.WorldToScreenPoint(uiCam, target.position)
            : new Vector2(Screen.width * 0.85f, Screen.height * 0.92f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        Sprite spr = icon != null ? icon : GetCircleSprite();
        Color color = icon != null ? Color.white : fallbackColor;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(CoFlyOne(canvasRect, spr, color, startLocal, endLocal));
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    private IEnumerator CoFlyOne(RectTransform canvasRect, Sprite spr, Color color, Vector2 startLocal, Vector2 endLocal)
    {
        GameObject go = new GameObject("RewardFly", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvasRect, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.SetAsLastSibling();

        Image img = go.GetComponent<Image>();
        img.sprite = spr;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;

        // Pha 1: bung nhẹ ra khỏi nút
        Vector2 burst = startLocal + UnityEngine.Random.insideUnitCircle * 70f;
        rt.anchoredPosition = startLocal;
        float t = 0f;
        const float burstT = 0.12f;
        while (t < burstT)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / burstT);
            k = k * (2f - k);
            rt.anchoredPosition = Vector2.Lerp(startLocal, burst, k);
            yield return null;
        }

        // Pha 2: bay về HUD + thu nhỏ
        const float dur = 0.7f;
        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.LerpUnclamped(burst, endLocal, k);
            float s = Mathf.Lerp(1f, 0.5f, k);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        Destroy(go);
    }

    private RewardBundle GetMissionRewards(MissionData data)
    {
        if (data == null)
            return new RewardBundle(DefaultMissionCoinReward, DefaultMissionDiamondReward, DefaultMissionExpReward);

        int coin = data.rewardType == RewardType.Coin ? Mathf.Max(0, data.rewardAmount) : DefaultMissionCoinReward;
        int diamond = data.rewardType == RewardType.Diamond ? Mathf.Max(0, data.rewardAmount) : DefaultMissionDiamondReward;
        return new RewardBundle(coin, diamond, DefaultMissionExpReward);
    }

    private RewardBundle GetAchievementRewards(MissionData data)
    {
        if (data == null)
            return new RewardBundle(100, 0, DefaultAchievementExpReward);

        // Thành tựu LÊN CẤP (ReachLevel): thưởng TĂNG DẦN theo cấp N — vàng + kim cương + EXP.
        // cấp 2 = 200 vàng, cấp 3 = 300 ... cấp 30 = 3000; mỗi 5 cấp +1 kim cương; EXP = cấp×15.
        if (data.eventType == MissionEventType.ReachLevel)
        {
            int lvl  = Mathf.Max(1, data.targetAmount);
            int coin = lvl * 100;
            int gems = Mathf.Max(1, lvl / 5);
            int exp  = lvl * 15;
            return new RewardBundle(coin, gems, exp);
        }

        int c = data.rewardType == RewardType.Coin ? Mathf.Max(0, data.rewardAmount) : 0;
        int d = data.rewardType == RewardType.Diamond ? Mathf.Max(0, data.rewardAmount) : 0;
        return new RewardBundle(c, d, DefaultAchievementExpReward);
    }

    private DailyReward[] GetDailyRewards()
    {
        return new[]
        {
            new DailyReward("Vàng", "x100", new RewardBundle(100, 0, 0)),
            new DailyReward("Kim cương", "x5", new RewardBundle(0, 5, 0)),
            new DailyReward("Hạt giống", "x2", new RewardBundle(0, 0, 5)),
            new DailyReward("Gỗ", "x5", new RewardBundle(0, 0, 5)),
            new DailyReward("Bình tưới", "x1", new RewardBundle(0, 0, 10)),
            new DailyReward("Hoa", "x1", new RewardBundle(0, 0, 10)),
            new DailyReward("Rương", "x1", new RewardBundle(500, 30, 100)),
        };
    }

    private Sprite GetDailyRewardSprite(int day)
    {
        int index = Mathf.Clamp(day - 1, 0, 6);
        if (sprites.dailyRewardIcons != null && index < sprites.dailyRewardIcons.Length && sprites.dailyRewardIcons[index] != null)
            return sprites.dailyRewardIcons[index];

        return day switch
        {
            1 => sprites.coinIcon,
            2 => sprites.diamondIcon,
            7 => sprites.chestIcon,
            _ => null
        };
    }

    private struct DailyState
    {
        public int streakDay;
        public bool claimedToday;
    }

    private DailyState SyncDailyState()
    {
        string today = TodayKey();
        string lastSeen = PlayerPrefs.GetString(DailyLastSeenKey, "");
        int streak = Mathf.Clamp(PlayerPrefs.GetInt(DailyStreakKey, 0), 0, 7);

        if (string.IsNullOrEmpty(lastSeen))
        {
            streak = 1;
        }
        else if (lastSeen == today)
        {
            if (streak <= 0)
                streak = 1;
        }
        else if (IsYesterday(lastSeen, today))
        {
            streak = streak >= 7 ? 1 : Mathf.Max(1, streak + 1);
        }
        else
        {
            streak = 1;
        }

        PlayerPrefs.SetString(DailyLastSeenKey, today);
        PlayerPrefs.SetInt(DailyStreakKey, streak);
        PlayerPrefs.Save();

        return new DailyState
        {
            streakDay = streak,
            claimedToday = PlayerPrefs.GetString(DailyClaimedDateKey, "") == today
        };
    }

    private static string TodayKey()
    {
        return DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    }

    private static bool IsYesterday(string previousKey, string todayKey)
    {
        if (!DateTime.TryParseExact(previousKey, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime previous))
            return false;
        if (!DateTime.TryParseExact(todayKey, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime today))
            return false;

        return previous.Date == today.Date.AddDays(-1);
    }

    private static bool IsMissionClaimed(MissionData data)
    {
        return data != null && PlayerPrefs.GetInt(MissionClaimedPrefsKey(data), 0) == 1;
    }

    private static bool IsAchievementClaimed(MissionData data)
    {
        return data != null && PlayerPrefs.GetInt(AchievementClaimedPrefsKey(data), 0) == 1;
    }

    private static string MissionClaimedPrefsKey(MissionData data)
    {
        string id = data.MissionId;
        return data.isDaily
            ? $"MISSION_CLAIMED_DAILY_{DateTime.Now:yyyyMMdd}_{id}"
            : $"MISSION_CLAIMED_{id}";
    }

    private static string AchievementClaimedPrefsKey(MissionData data)
    {
        return $"ACHIEVEMENT_CLAIMED_{data.MissionId}";
    }

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null)
            return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)
            return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }

    private void AcquireInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, true);
        if (_inputLockHeld)
            return;

        FarmInputLock.RegisterPopupOpen();
        _inputLockHeld = true;
    }

    private void ReleaseInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(gameObject, false);
        if (!_inputLockHeld)
            return;

        FarmInputLock.RegisterPopupClose();
        _inputLockHeld = false;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            child.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, bool sliced)
    {
        RectTransform rect = CreateRect(parent, name, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite != null ? sprite : GetRoundedSprite();
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, int size, Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 rectSize, FontStyles style = FontStyles.Normal)
    {
        RectTransform rect = CreateRect(parent, name, position, rectSize);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateTextButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color32 color, int fontSize)
    {
        RectTransform root = CreateImage(parent, name, null, color, position, size, true);
        AddOutline(root.gameObject, new Color32(77, 54, 24, 160), new Vector2(2f, -2f));
        Image btnBg = root.GetComponent<Image>();
        btnBg.raycastTarget = true;                    // BẮT BUỘC: để nút nhận click
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = btnBg;
        button.transition = Selectable.Transition.ColorTint;

        TMP_Text text = CreateText(root, "Txt_Label", label, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(6f, 6f), FontStyles.Bold);
        AddShadow(text.gameObject, new Color32(70, 42, 20, 220), new Vector2(1.5f, -1.5f));
        return button;
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null)
            return _roundedSprite;

        _roundedSprite = CreateRoundedSprite("UnifiedTask_Rounded", 64, 14);
        return _roundedSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null)
            return _circleSprite;

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 2) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        _circleSprite.name = "UnifiedTask_Circle";
        _circleSprite.hideFlags = HideFlags.HideAndDontSave;
        return _circleSprite;
    }

    private static Sprite GetRightTriangleSprite()
    {
        if (_rightTriangleSprite != null)
            return _rightTriangleSprite;

        _rightTriangleSprite = CreateTriangleSprite("UnifiedTask_TriangleRight", true);
        return _rightTriangleSprite;
    }

    private static Sprite GetLeftTriangleSprite()
    {
        if (_leftTriangleSprite != null)
            return _leftTriangleSprite;

        _leftTriangleSprite = CreateTriangleSprite("UnifiedTask_TriangleLeft", false);
        return _leftTriangleSprite;
    }

    private static Sprite CreateRoundedSprite(string name, int size, int radius)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        float r = radius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x < radius ? radius - x : x >= size - radius ? x - (size - radius - 1) : 0f;
                float dy = y < radius ? radius - y : y >= size - radius ? y - (size - radius - 1) : 0f;
                bool inside = dx <= 0f && dy <= 0f || dx * dx + dy * dy <= r * r;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static Sprite CreateTriangleSprite(string name, bool right)
    {
        const int width = 64;
        const int height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;

        for (int y = 0; y < height; y++)
        {
            float t = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
            int edge = Mathf.RoundToInt(Mathf.Lerp(width - 2, 2, t));
            for (int x = 0; x < width; x++)
            {
                bool inside = right ? x <= edge : x >= width - edge;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
