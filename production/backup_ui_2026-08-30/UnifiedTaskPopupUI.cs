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
        public Image background;          // lớp gradient trên
        public Image backgroundBottom;    // lớp màu đáy
        public GameObject pointer;        // chấm đỏ "có thưởng chưa nhận"
        public TMP_Text label;
        public Image icon;
        public RectTransform root;
        public int chiSo;                 // 0..2 — tính lại toạ độ khi lún/nổi
    }

    private const string RootName = "UnifiedTaskPopupRoot";
    private const string DailyLastSeenKey = "UNIFIED_TASK_DAILY_LAST_SEEN";
    private const string DailyStreakKey = "UNIFIED_TASK_DAILY_STREAK";
    private const string DailyClaimedDateKey = "UNIFIED_TASK_DAILY_CLAIMED_DATE";

    // B4 — họ save + phiên bản cho MỌI khoá nhiệm vụ ghi trực tiếp:
    //   MISSION_CLAIMED_{id} · MISSION_CLAIMED_DAILY_{yyyyMMdd}_{id} · ACHIEVEMENT_CLAIMED_{id}
    //   UNIFIED_TASK_DAILY_LAST_SEEN / _STREAK / _CLAIMED_DATE
    // Chúng ghi thẳng số/chuỗi, và số lượng khoá thì SINH ĐỘNG theo từng `MissionData` —
    // không có chỗ nào nhét `saveVersion` vào. Dấu phiên bản nằm ở `SAVE_VER_MISSION`.
    //
    // v1 = khoá claimed đặt theo `MissionData.MissionId` (rỗng thì lấy tên asset).
    // TĂNG SỐ NÀY nếu đổi cách đặt `MissionId` hoặc đổi tên asset mission hàng loạt: khoá cũ
    // trở thành rác vĩnh viễn và người chơi được NHẬN LẠI mọi nhiệm vụ đã nhận — nhân đôi thưởng.
    private const string SaveFamily  = "MISSION";
    private const int    SaveVersion = 1;

    private static bool _missionVersionChecked;

    /// <summary>
    /// Đóng dấu phiên bản họ save MISSION. Gọi từ mọi cửa ĐỌC static bên dưới: popup này
    /// được dựng lười (`EnsureInstance`) nên không thể chỉ đóng dấu trong `Awake`.
    /// </summary>
    private static void EnsureMissionSaveVersion()
    {
        if (_missionVersionChecked) return;
        _missionVersionChecked = true;

        bool coSaveCu = PlayerPrefs.HasKey(DailyLastSeenKey)
                        || PlayerPrefs.HasKey(DailyStreakKey)
                        || PlayerPrefs.HasKey(DailyClaimedDateKey)
                        || PlayerPrefs.HasKey("MISSION_PROGRESS_V1");

        SaveVersionGuard.Ensure(SaveFamily, SaveVersion, null, coSaveCu);
    }
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

    // ═════════════════════════════════════════════════════════════════════════
    //  CHIA TRANG THEO MỐC CẤP
    // ═════════════════════════════════════════════════════════════════════════
    //  Database có 307 nhiệm vụ chính. Dựng hết vào một danh sách cuộn là 5.833
    //  GameObject — và toàn bộ bị huỷ rồi dựng lại mỗi lần bấm "Nhận". Đó là nguyên
    //  nhân chính của hiện tượng giật đo được trong video (12 lần khựng 0,13–0,37s).
    //
    //  Chia theo MỐC CẤP chứ không theo "N dòng mỗi trang": người chơi cấp 3 không
    //  quan tâm nhiệm vụ cấp 27, và đếm dòng thì ranh giới trang rơi vào giữa một mốc
    //  trông rất tuỳ tiện. Làm xong hết một mốc thì tự sang mốc sau.

    /// <summary>Cận dưới của 6 mốc. Mốc cuối gom cả nhiệm vụ cấp >30 nếu sau này có thêm.</summary>
    private static readonly int[] MocCap = { 1, 5, 10, 15, 20, 25 };

    private static int CapDauTrang(int trang) => MocCap[Mathf.Clamp(trang, 0, MocCap.Length - 1)];

    /// <summary>Cấp CAO NHẤT thuộc trang. Trang cuối lấy vô cực để không bỏ sót nhiệm vụ nào.</summary>
    private static int CapCuoiTrang(int trang)
    {
        trang = Mathf.Clamp(trang, 0, MocCap.Length - 1);
        return trang == MocCap.Length - 1 ? int.MaxValue : MocCap[trang + 1] - 1;
    }

    private static string TenTrang(int trang)
    {
        int a = CapDauTrang(trang);
        return trang == MocCap.Length - 1 ? $"Cấp {a}+" : $"Cấp {a}–{CapCuoiTrang(trang)}";
    }

    private int _trangNhiemVu = -1;      // -1 = chưa chọn, sẽ tự nhảy tới mốc đang chơi

    // ═════════════════════════════════════════════════════════════════════════
    //  TÁI DÙNG HÀNG (row pooling)
    // ═════════════════════════════════════════════════════════════════════════
    //  Chia trang giảm 307 hàng xuống 28–84. Vẫn còn ~1.600 GameObject nếu mỗi lần đổi
    //  trang lại huỷ sạch rồi dựng lại. Nên hàng được DỰNG MỘT LẦN rồi NẠP LẠI NỘI DUNG:
    //  đổi trang chỉ là gán text và sprite — 0 Instantiate, 0 Destroy, 0 rác cho GC.

    private sealed class HangThuong
    {
        public RectTransform goc;
        public Image         nenHang;
        public Image         iconVien;
        public Image         icon;
        public TMP_Text      ten;
        public Image         thanhTienDo;
        public TMP_Text      chuTienDo;
        public OThuong[]     oThuong;
        public Image         nutNen;          // nút, lớp gradient trên
        public Image         nutNenDuoi;      // nút, lớp màu đáy
        public Image         nutVien;
        public TMP_Text      nutChu;
        public Button        nut;
        public GameObject    chamDo;
        public MissionData   duLieu;
        public CanvasGroup   doMo;            // mờ cả hàng khi khoá/đã nhận (thiết kế)
    }

    private sealed class OThuong
    {
        public RectTransform goc;
        public Image         icon;
        public TMP_Text      so;
    }

    private sealed class ChanMoc
    {
        public RectTransform goc;
        public TMP_Text      mota;
        public Image         thanh;
        public TMP_Text      soTienDo;
    }

    private readonly List<HangThuong> _khoHangNhiemVu  = new List<HangThuong>();
    private readonly List<HangThuong> _khoHangThanhTuu = new List<HangThuong>();

    /// <summary>Tra ngược từ nhiệm vụ về hàng đang hiện nó — để bấm Nhận chỉ sửa đúng hàng đó.</summary>
    private readonly Dictionary<MissionData, HangThuong> _hangDangHien =
        new Dictionary<MissionData, HangThuong>();

    private RectTransform _vungCuonNhiemVu;
    private RectTransform _vungCuonThanhTuu;
    private TMP_Text      _nhanTrangNhiemVu;
    private TMP_Text      _nhanTrangThanhTuu;
    private ChanMoc       _chanMocNhiemVu;
    private ChanMoc       _chanMocThanhTuu;

    // ═════════════════════════════════════════════════════════════════════════
    //  TOẠ ĐỘ CỘT — nguồn duy nhất, tránh chữ đè chữ
    // ═════════════════════════════════════════════════════════════════════════
    //  Bố cục cũ rải số toạ độ khắp `BuildMissionRow`, chỉnh một chỗ là lệch chỗ khác —
    //  đó là lý do chữ chồng lên ô thưởng. Giờ mọi mốc x nằm ở đây và có bài kiểm tra
    //  tính lại các hình chữ nhật để bắt chồng lấn.
    //
    //   -402┃ ○ icon ┃ tên nhiệm vụ       ┃ ▣  ▣  ▣ ┃  [ nút ]  ┃402
    //       ┃  tròn  ┃ ▬▬▬ tiến độ ▬▬▬   ┃ thưởng  ┃           ┃

    private const float RongHang   = 805f;
    private const float CaoHangNV  = 96f;
    private const float CaoHangTT  = 84f;

    private const float X_IconTron = -336f;
    private const float D_IconTron = 72f;

    private const float X_CotChu   = -172f;
    private const float W_CotChu   = 250f;

    private const float X_OThuong0 = 22f;
    private const float B_OThuong  = 98f;
    private const float W_OThuong  = 92f;
    private const float H_OThuong  = 58f;

    private const float X_Nut      = 332f;
    private const float W_Nut      = 122f;
    private const float H_Nut      = 56f;

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
        SkinKit.ApFont(_root);
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

#if UNITY_EDITOR
        if (_missionDatabase == null)
            _missionDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MissionDatabase>("Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Main.asset");
        if (_dailyMissionDatabase == null)
            _dailyMissionDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MissionDatabase>("Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Daily.asset");
        if (_achievementDatabase == null)
            _achievementDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<MissionDatabase>("Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Achievement.asset");
#endif
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

        // Đảm bảo Popup luôn nổi trên HUD (sortingOrder 120)
        Canvas popupCanvas = GetComponent<Canvas>();
        if (popupCanvas == null) popupCanvas = gameObject.AddComponent<Canvas>();
        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = 120;
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

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
        {
            // Cùng bẫy `??` + fake-null như ở NapHang — chỗ này chưa nổ chỉ vì root
            // thường đã có sẵn CanvasGroup, nhưng scene nào thiếu là chết y hệt.
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (_built && _board != null)
            return;

        for (int i = _root.childCount - 1; i >= 0; i--)
        {
            GameObject child = _root.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }

        // ⚠ BẮT BUỘC dọn kho hàng TÁI DÙNG cùng lúc với việc huỷ cây.
        // Kho giữ tham chiếu tới các RectTransform vừa bị huỷ ở vòng lặp trên. Không dọn
        // thì lần dựng lại `NapDanhSach` thấy `kho.Count == 28`, đi vào nhánh "dùng lại
        // hàng có sẵn", rồi chạm vào object ĐÃ BỊ HUỶ → MissingReferenceException ngay
        // hàng đầu → cả 28 hàng không lên, nhãn vẫn ghi "28 nhiệm vụ" mà màn hình trống.
        _khoHangNhiemVu.Clear();
        _khoHangThanhTuu.Clear();
        _hangDangHien.Clear();
        _vungCuonNhiemVu   = null;
        _vungCuonThanhTuu  = null;
        _nhanTrangNhiemVu  = null;
        _nhanTrangThanhTuu = null;
        _chanMocNhiemVu    = null;
        _chanMocThanhTuu   = null;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        // Bẫy `??` + fake-null thứ ba, cùng họ với hai chỗ CanvasGroup đã sửa.
        Image overlay = gameObject.GetComponent<Image>();
        if (overlay == null) overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0.04f, 0.08f, 0.03f, 0.68f);
        overlay.raycastTarget = true;

        if (gameObject.GetComponent<UIRaycastBlocker>() == null)
            gameObject.AddComponent<UIRaycastBlocker>();

        // ══ VÁN GỖ 1300×850 — README: bo 42, viền 8px #4a2508, gradient #a9743c→#7c4e22,
        // thớ ván mỗi 158px, 4 đinh sắt góc ═════════════════════════════════════
        _board = CreateRect(_root, "Board_WoodFrame", Vector2.zero,
            new Vector2(TaskPopupDesign.BangRong, TaskPopupDesign.BangCao));

        if (sprites.boardFrame != null)
        {
            // Hỏi FIELD THÔ chứ không phải property BoardFrameSprite: property tự lùi
            // về sprite thủ tục TRẮNG khi chưa có art, nên nhánh này luôn được chọn và
            // ván gỗ thành tấm trắng — đúng ảnh chụp lần chạy đầu.
            CreateImage(_board, "Img_WoodBoard", sprites.boardFrame, Color.white, Vector2.zero,
                new Vector2(TaskPopupDesign.BangRong, TaskPopupDesign.BangCao), true);
        }
        else
        {
            CreateImage(_board, "Board_Border", BoGoc(TaskPopupDesign.BangBoGoc),
                TaskPopupDesign.VanGoVien, Vector2.zero,
                new Vector2(TaskPopupDesign.BangRong + 16f, TaskPopupDesign.BangCao + 16f), true);
            CreateImage(_board, "Board_Fill_Bottom", BoGoc(TaskPopupDesign.BangBoGoc),
                TaskPopupDesign.VanGoDuoi, Vector2.zero,
                new Vector2(TaskPopupDesign.BangRong, TaskPopupDesign.BangCao), true);
            PhuGradient(_board, "Board_Fill_Top", TaskPopupDesign.VanGoTren, Vector2.zero,
                new Vector2(TaskPopupDesign.BangRong, TaskPopupDesign.BangCao),
                TaskPopupDesign.BangBoGoc);

            // Thớ ván ngang — repeating-linear-gradient mỗi 158px, dày 5px.
            int soVach = Mathf.FloorToInt(TaskPopupDesign.BangCao / TaskPopupDesign.ThoVanBuoc);
            for (int i = 1; i <= soVach; i++)
                CreateImage(_board, $"Board_Grain_{i}", null, TaskPopupDesign.VanGoTho,
                    new Vector2(0f, TaskPopupDesign.BangCao * 0.5f - i * TaskPopupDesign.ThoVanBuoc),
                    new Vector2(TaskPopupDesign.BangRong - 24f, TaskPopupDesign.ThoVanDay), true);
        }

        // 4 đinh sắt góc — 3 lớp: vành tối, thân, chấm sáng lệch trên-trái.
        Vector2[] choDinh = { TaskPopupDesign.DinhTrenTrai, TaskPopupDesign.DinhTrenPhai,
                              TaskPopupDesign.DinhDuoiTrai, TaskPopupDesign.DinhDuoiPhai };
        var ktDinh = new Vector2(TaskPopupDesign.DinhKichThuoc, TaskPopupDesign.DinhKichThuoc);
        for (int i = 0; i < choDinh.Length; i++)
        {
            CreateImage(_board, $"Stud_{i}_Rim", GetCircleSprite(), TaskPopupDesign.DinhSatVien,
                choDinh[i], ktDinh + new Vector2(4f, 4f), false);
            CreateImage(_board, $"Stud_{i}_Base", GetCircleSprite(), TaskPopupDesign.DinhSatToi,
                choDinh[i], ktDinh, false);
            CreateImage(_board, $"Stud_{i}_Shine", GetCircleSprite(), TaskPopupDesign.DinhSatSang,
                choDinh[i] + new Vector2(-2f, 2f), ktDinh * 0.5f, false);
        }

        BuildRibbon();
        BuildTabs();

        // ══ GIẤY KEM — bo góc CHỈ Ở ĐÁY vì mép trên nối liền tab đang chọn ═════
        var ktGiay = new Vector2(TaskPopupDesign.GiayRong, TaskPopupDesign.GiayCao);
        CreateImage(_board, "Paper_Border", BoGoc(TaskPopupDesign.GiayBoGoc),
            TaskPopupDesign.GiayVien, TaskPopupDesign.GiayTam, ktGiay + new Vector2(8f, 8f), true);
        CreateImage(_board, "Paper_Fill", BoGoc(TaskPopupDesign.GiayBoGoc),
            TaskPopupDesign.GiayDuoi, TaskPopupDesign.GiayTam, ktGiay, true);
        PhuGradient(_board, "Paper_Fill_Top", TaskPopupDesign.GiayTren,
            TaskPopupDesign.GiayTam, ktGiay, TaskPopupDesign.GiayBoGoc);
        CreateImage(_board, "Paper_InnerRing", BoGoc(TaskPopupDesign.GiayBoGoc - 3f),
            TaskPopupDesign.GiayVienTrong, TaskPopupDesign.GiayTam, ktGiay - new Vector2(6f, 6f), true);
        CreateImage(_board, "Paper_Fill_Inner", BoGoc(TaskPopupDesign.GiayBoGoc - 4f),
            TaskPopupDesign.GiayDuoi, TaskPopupDesign.GiayTam, ktGiay - new Vector2(12f, 12f), true);

        _contentRoot = CreateRect(_board, "ContentRoot", TaskPopupDesign.GiayTam, ktGiay);
        _missionPanel     = CreateRect(_contentRoot, "Panel_Mission", Vector2.zero, ktGiay);
        _dailyPanel       = CreateRect(_contentRoot, "Panel_Daily", Vector2.zero, ktGiay);
        _achievementPanel = CreateRect(_contentRoot, "Panel_Achievement", Vector2.zero, ktGiay);

        // ══ NÚT ĐÓNG 100×100 nhô góc trên-phải — art btnX gán qua tool ═════════
        Button close = CreateTextButton(_board, "Btn_Close", "", TaskPopupDesign.NutDongTam,
            new Vector2(TaskPopupDesign.NutDongKichThuoc, TaskPopupDesign.NutDongKichThuoc),
            new Color32(255, 255, 255, 0), 1);
        close.image.sprite = sprites.closeButton != null ? sprites.closeButton : GetCircleSprite();
        close.image.type = Image.Type.Simple;
        close.image.color = sprites.closeButton != null ? Color.white : new Color32(239, 75, 51, 255);
        close.image.preserveAspect = true;
        close.onClick.AddListener(Close);
        TMP_Text chuX = close.GetComponentInChildren<TMP_Text>();
        if (chuX != null)
        {
            // Nhãn "X" chỉ hiện khi CHƯA có art — có art rồi thì chữ đè lên hình.
            if (sprites.closeButton != null) chuX.gameObject.SetActive(false);
            else { chuX.text = "X"; chuX.fontSize = 44; }
        }

        _built = true;                       // chốt CUỐI: nếu dựng lỗi giữa chừng sẽ thử lại lần sau
        _root.gameObject.SetActive(false);
    }

    private void BuildRibbon()
    {
        Sprite ribbonSpr = sprites.ribbon;
        if (ribbonSpr == null)
        {
#if UNITY_EDITOR
            ribbonSpr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_shop_svg/generated_sprites/shop_banner_ribbon.png");
#endif
        }

        RectTransform bien;
        if (ribbonSpr != null)
        {
            bien = CreateImage(_board, "Ribbon_Title", ribbonSpr, Color.white,
                TaskPopupDesign.RibbonVungTam,
                new Vector2(620f, 126f), true);
            var img = bien.GetComponent<Image>();
            if (img != null) img.type = Image.Type.Sliced;
        }
        else
        {
            // Hai đuôi ribbon đỏ #d8641f→#a84812, vẽ TRƯỚC để nằm dưới tấm biển vàng.
            CreateImage(_board, "Ribbon_Tail_Left", BoGoc(6f), TaskPopupDesign.DuoiRibbonDuoi,
                TaskPopupDesign.DuoiRibbonTrai, TaskPopupDesign.DuoiRibbonKichThuoc, true);
            CreateImage(_board, "Ribbon_Tail_Right", BoGoc(6f), TaskPopupDesign.DuoiRibbonDuoi,
                TaskPopupDesign.DuoiRibbonPhai, TaskPopupDesign.DuoiRibbonKichThuoc, true);

            // Tấm biển vàng #ffd257→#f0a32f, viền 5px #a35c14, bo 24.
            bien = CreateRect(_board, "Ribbon_Title", TaskPopupDesign.RibbonTamTam,
                TaskPopupDesign.RibbonTamKichThuoc);
            CreateImage(bien, "Plate_Border", BoGoc(TaskPopupDesign.RibbonBoGoc),
                TaskPopupDesign.RibbonVien, Vector2.zero,
                TaskPopupDesign.RibbonTamKichThuoc + new Vector2(10f, 10f), true);
            CreateImage(bien, "Plate_Fill", BoGoc(TaskPopupDesign.RibbonBoGoc),
                TaskPopupDesign.RibbonDuoi, Vector2.zero, TaskPopupDesign.RibbonTamKichThuoc, true);
            PhuGradient(bien, "Plate_Fill_Top", TaskPopupDesign.RibbonTren, Vector2.zero,
                TaskPopupDesign.RibbonTamKichThuoc, TaskPopupDesign.RibbonBoGoc);
        }

        // Chữ 46px trắng kem, không ngắt dòng
        _titleText = CreateText(bien, "Txt_Title", "NHIỆM VỤ", 46,
            TaskPopupDesign.ChuTieuDe, TextAlignmentOptions.Center, new Vector2(0f, 6f),
            new Vector2(540f, 70f), FontStyles.Bold);
        _titleText.characterSpacing = 4f;
        _titleText.textWrappingMode = TextWrappingModes.NoWrap;
        AddOutline(_titleText.gameObject, TaskPopupDesign.VienChuTieuDe, new Vector2(2f, -2f));
        AddShadow(_titleText.gameObject, TaskPopupDesign.VienChuTieuDe, new Vector2(0f, -3f));
    }

    private void BuildDecorations()
    {
        CreateImage(_board, "Decor_Leaf_TopLeft", LeafSprite, new Color32(101, 158, 45, 255), new Vector2(-420f, 328f), new Vector2(125f, 45f), false);
        CreateImage(_board, "Decor_Flowers_TopLeft", FlowerSprite, new Color32(255, 245, 185, 255), new Vector2(-510f, 330f), new Vector2(80f, 45f), false);
        CreateImage(_board, "Decor_Leaf_TopRight", LeafSprite, new Color32(101, 158, 45, 255), new Vector2(360f, 328f), new Vector2(125f, 45f), false);
        CreateImage(_board, "Decor_Flowers_TopRight", FlowerSprite, new Color32(255, 245, 185, 255), new Vector2(480f, 330f), new Vector2(80f, 45f), false);
        CreateImage(_board, "Decor_Mascot_Placeholder", MascotSprite, new Color32(235, 154, 91, 255), new Vector2(-470f, -260f), new Vector2(190f, 160f), false);
        CreateText(_board, "Txt_Mascot_Placeholder", "NPC", 28, new Color32(108, 64, 34, 255), TextAlignmentOptions.Center, new Vector2(-470f, -260f), new Vector2(130f, 50f), FontStyles.Bold);
    }

    private void BuildTabs()
    {
        // Tab NGANG dán mép trên tờ giấy — bố cục thiết kế. Bản cũ xếp DỌC ở ray trái.
        // Tab đang chọn nổi lên nối liền giấy, tab thường lún xuống 14px — chuyển động
        // lún đó là thứ README gọi là "tab 3D".
        _missionTab     = CreateTabButton("Tab_Mission", "Nhiệm vụ", MissionTabSprite, 0, Tab.Mission);
        _dailyTab       = CreateTabButton("Tab_Daily", "Hằng ngày", DailyTabSprite, 1, Tab.Daily);
        _achievementTab = CreateTabButton("Tab_Achievement", "Thành tựu", AchievementTabSprite, 2, Tab.Achievement);
    }

    private TabButtonView CreateTabButton(string name, string label, Sprite iconSprite, int chiSo, Tab targetTab)
    {
        var kt = new Vector2(TaskPopupDesign.TabRong, TaskPopupDesign.TabCao);
        var viTri = new Vector2(TaskPopupDesign.TabTamX(chiSo), TaskPopupDesign.TabTamY(false));

        RectTransform root = CreateRect(_board, name, viTri, kt);

        // Viền 4px #6e4014, hở mép dưới (CSS border-bottom: none) — đẩy viền lên 4px.
        CreateImage(root, "Tab_Border", BoGoc(TaskPopupDesign.TabBoGoc), TaskPopupDesign.TabVien,
            new Vector2(0f, 4f), kt + new Vector2(8f, 8f), true);

        Image nenDuoi = CreateImage(root, "Tab_Fill_Bottom", BoGoc(TaskPopupDesign.TabBoGoc),
            TaskPopupDesign.TabThuongDuoi, Vector2.zero, kt, true).GetComponent<Image>();
        Image nenTren = CreateImage(root, "Tab_Fill_Top", DaiGradient(),
            TaskPopupDesign.TabThuongTren, Vector2.zero,
            new Vector2(kt.x - TaskPopupDesign.TabBoGoc * 2f, kt.y - 8f), false).GetComponent<Image>();

        // Vùng bấm riêng, alpha 0. KHÔNG bật raycast trên lớp gradient: sprite có vùng
        // alpha≈0, Unity coi pixel alpha thấp là "không trúng" → nửa dưới tab bấm hụt.
        Image vungBam = CreateImage(root, "Tab_Hit", BoGoc(TaskPopupDesign.TabBoGoc),
            new Color(1f, 1f, 1f, 0f), Vector2.zero, kt, true).GetComponent<Image>();
        vungBam.raycastTarget = true;

        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = vungBam;
        button.onClick.AddListener(() => ShowTab(targetTab));

        // Đĩa tròn trắng mờ 54px chứa icon 38px, đặt lệch trái để chừa chỗ cho nhãn.
        RectTransform dia = CreateImage(root, "Icon_Disc", GetCircleSprite(),
            TaskPopupDesign.TabDiaIcon, new Vector2(-84f, 0f),
            new Vector2(TaskPopupDesign.TabDiaKichThuoc, TaskPopupDesign.TabDiaKichThuoc), false);
        CreateImage(dia, "Disc_Rim", GetCircleSprite(), TaskPopupDesign.TabDiaVien, Vector2.zero,
            new Vector2(TaskPopupDesign.TabDiaKichThuoc + 4f, TaskPopupDesign.TabDiaKichThuoc + 4f),
            false).SetAsFirstSibling();

        Image icon = CreateImage(dia, "Img_Icon", iconSprite, Color.white, Vector2.zero,
            new Vector2(TaskPopupDesign.TabIconKichThuoc, TaskPopupDesign.TabIconKichThuoc), false)
            .GetComponent<Image>();
        icon.preserveAspect = true;

        TMP_Text text = CreateText(root, "Txt_Label", label, TaskPopupDesign.CoChuTab,
            TaskPopupDesign.TabChuThuong, TextAlignmentOptions.Center,
            new Vector2(TaskPopupDesign.TabDiaKichThuoc * 0.5f, 0f),
            new Vector2(TaskPopupDesign.TabRong - TaskPopupDesign.TabDiaKichThuoc - 40f, 40f),
            FontStyles.Bold);
        text.textWrappingMode = TextWrappingModes.NoWrap;

        // Chấm đỏ top 6 right 10 — nghĩa MỚI theo thiết kế: "tab này có thứ chưa nhận",
        // KHÔNG phải "đang chọn" như bản cũ.
        // CHA = vành trắng, CON = tâm đỏ. Mọi CON đều vẽ SAU cha (SetAsFirstSibling
        // chỉ xếp giữa anh em) — bản trước để vành trắng làm con nên nó phủ kín tâm
        // đỏ, chấm báo hiệu hoá thành chấm TRẮNG như ảnh chụp.
        RectTransform cham = CreateImage(root, "Dot_Notice", GetCircleSprite(), Color.white,
            new Vector2(TaskPopupDesign.TabRong * 0.5f - 22f, TaskPopupDesign.TabCao * 0.5f - 18f),
            new Vector2(TaskPopupDesign.ChamDoKichThuoc + 6f, TaskPopupDesign.ChamDoKichThuoc + 6f), false);
        CreateImage(cham, "Dot_Fill", GetCircleSprite(), TaskPopupDesign.ChamDoGiua, Vector2.zero,
            new Vector2(TaskPopupDesign.ChamDoKichThuoc, TaskPopupDesign.ChamDoKichThuoc), false);
        cham.gameObject.SetActive(false);

        return new TabButtonView
        {
            button = button,
            background = nenTren,
            backgroundBottom = nenDuoi,
            pointer = cham.gameObject,
            label = text,
            icon = icon,
            root = root,
            chiSo = chiSo,
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

        // Chữ HOA theo thiết kế — ribbon biển hiệu nông trại: NHIỆM VỤ / ĐIỂM DANH / THÀNH TỰU.
        _titleText.text = tab switch
        {
            Tab.Daily       => "ĐIỂM DANH",
            Tab.Achievement => "THÀNH TỰU",
            _               => "NHIỆM VỤ",
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

        // Có art tab thì thay sprite; chưa có thì đổi màu hai lớp gradient theo token.
        if (selected && sprites.selectedTabButton != null)
        {
            view.background.sprite = sprites.selectedTabButton;
            view.background.color = Color.white;
        }
        else if (!selected && sprites.tabButton != null)
        {
            view.background.sprite = sprites.tabButton;
            view.background.color = Color.white;
        }
        else
        {
            view.background.sprite = DaiGradient();
            view.background.color = selected ? TaskPopupDesign.TabChonTren : TaskPopupDesign.TabThuongTren;
            if (view.backgroundBottom != null)
                view.backgroundBottom.color = selected ? TaskPopupDesign.TabChonDuoi : TaskPopupDesign.TabThuongDuoi;
        }

        view.label.color = selected ? TaskPopupDesign.TabChuChon : TaskPopupDesign.TabChuThuong;

        // Nổi/lún: tab đang chọn nối liền tờ giấy, tab thường tụt xuống 14px.
        if (view.root != null)
            view.root.anchoredPosition = new Vector2(
                TaskPopupDesign.TabTamX(view.chiSo), TaskPopupDesign.TabTamY(selected));

        // Chấm đỏ = "có thưởng chưa nhận" và KHÔNG đang xem tab đó.
        bool co = false;
        if (!selected)
        {
            if (view == _missionTab)          co = CoThuongChoNhan(_missionDatabase, false);
            else if (view == _achievementTab) co = CoThuongChoNhan(_achievementDatabase, true);
            else if (view == _dailyTab)       co = !SyncDailyState().claimedToday;
        }
        if (view.pointer != null && view.pointer.activeSelf != co) view.pointer.SetActive(co);
    }

    /// <summary>Tab có ít nhất một mục đã xong mà chưa nhận thưởng?</summary>
    private bool CoThuongChoNhan(MissionDatabase db, bool laThanhTuu)
    {
        if (db == null || db.missions == null) return false;

        int cap = GetPlayerLevel();
        foreach (MissionData m in db.missions)
        {
            if (m == null || m.isDaily) continue;
            if (m.requiredLevel > cap) continue;
            if (IsAchievementOrMissionClaimed(m, laThanhTuu)) continue;
            if (MissionProgressTracker.GetProgressFor(m) >= Mathf.Max(1, m.targetAmount)) return true;
        }
        return false;
    }


    private void BuildMissionContent()
    {
        // Dựng khung MỘT LẦN. Các lần sau chỉ nạp lại nội dung — đây là điểm mấu chốt để
        // hết giật: `ClearChildren` + dựng lại là thứ đang tốn hàng trăm mili giây.
        if (_vungCuonNhiemVu == null)
        {
            ClearChildren(_missionPanel);
            // KHÔNG còn dòng tiêu đề trong giấy — ribbon đã ghi NHIỆM VỤ ngay trên đầu,
            // thêm một dòng nữa là lặp và ăn mất 38px chiều cao danh sách.
            _nhanTrangNhiemVu = DungThanhChuyenTrang(
                _missionPanel, new Vector2(0f, 283f),
                () => DoiTrangNhiemVu(-1), () => DoiTrangNhiemVu(+1));

            _vungCuonNhiemVu = BuildVerticalScroll(_missionPanel, "Mission_ScrollView",
                new Vector2(0f, 33f), new Vector2(TaskPopupDesign.VungTrongRong, 456f));

            _chanMocNhiemVu = DungChanMoc(_missionPanel, "Phần thưởng mốc — cả trang",
                "Hoàn thành tất cả nhiệm vụ để nhận thưởng đặc biệt!");
        }

        if (_trangNhiemVu < 0) _trangNhiemVu = TrangDangChoi(false);
        NapLaiTrangNhiemVu();
    }





    /// <summary>
    /// Trang ứng với chặng người chơi đang ở. Mở popup lần đầu phải rơi vào đúng mốc
    /// đang chơi, không phải mốc 1–4 mà họ đã làm xong từ lâu.
    /// </summary>
    private int TrangDangChoi(bool laThanhTuu)
    {
        int cap = GetPlayerLevel();
        int trang = 0;
        for (int i = MocCap.Length - 1; i >= 0; i--)
        {
            if (cap >= MocCap[i]) { trang = i; break; }
        }

        // Mốc hiện tại đã nhận hết thưởng thì nhảy sang mốc sau — đúng ý "làm xong một
        // lượt sẽ tới trang khác". Dừng ở mốc đầu tiên còn việc.
        while (trang < MocCap.Length - 1 && DaXongTrang(trang, laThanhTuu))
            trang++;

        return trang;
    }

    /// <summary>Mọi nhiệm vụ mở khoá trong trang này đều đã nhận thưởng?</summary>
    private bool DaXongTrang(int trang, bool laThanhTuu)
    {
        int a = CapDauTrang(trang), b = CapCuoiTrang(trang);
        int cap = GetPlayerLevel();
        bool coItemMoKhoa = false;

        MissionDatabase db = laThanhTuu ? _achievementDatabase : _missionDatabase;
        if (db == null || db.missions == null) return true;

        foreach (MissionData m in db.missions)
        {
            if (m == null || m.isDaily) continue;
            if (m.requiredLevel < a || m.requiredLevel > b) continue;
            if (m.requiredLevel > cap) continue;      // chưa tới cấp thì không tính là còn việc

            coItemMoKhoa = true;
            bool daNhan = laThanhTuu ? IsAchievementClaimed(m) : IsMissionClaimed(m);
            if (!daNhan) return false;
        }

        // Không có nhiệm vụ nào mở khoá trong mốc này ⇒ coi như chưa xong, đứng lại đây.
        return coItemMoKhoa;
    }

    private void DoiTrangNhiemVu(int buoc)
    {
        int moi = Mathf.Clamp(_trangNhiemVu + buoc, 0, MocCap.Length - 1);
        if (moi == _trangNhiemVu) return;
        _trangNhiemVu = moi;
        NapLaiTrangNhiemVu();
    }

    private void NapLaiTrangNhiemVu()
    {
        int cap = GetPlayerLevel();
        int a = CapDauTrang(_trangNhiemVu), b = CapCuoiTrang(_trangNhiemVu);

        List<MissionData> ds = LocTheoTrang(_missionDatabase, a, b, false);

        if (_nhanTrangNhiemVu != null)
            _nhanTrangNhiemVu.text = $"{TenTrang(_trangNhiemVu)}   ·   {ds.Count} nhiệm vụ";

        NapDanhSach(_khoHangNhiemVu, _vungCuonNhiemVu, ds, cap, false);
        CapNhatChanMoc(_chanMocNhiemVu, ds, false);
    }

    /// <summary>
    /// Lấy nhiệm vụ thuộc một mốc cấp, xếp: làm được ngay → đang làm → đã nhận → chưa mở.
    /// </summary>
    private List<MissionData> LocTheoTrang(MissionDatabase db, int capDau, int capCuoi, bool laThanhTuu)
    {
        var b0 = new List<MissionData>();   // đang làm
        var b1 = new List<MissionData>();   // xong, chờ nhận
        var b2 = new List<MissionData>();   // đã nhận
        var b3 = new List<MissionData>();   // chưa đủ cấp

        if (db != null && db.missions != null)
        {
            int cap = GetPlayerLevel();
            foreach (MissionData m in db.missions)
            {
                if (m == null || m.isDaily) continue;
                if (m.requiredLevel < capDau || m.requiredLevel > capCuoi) continue;

                if (m.requiredLevel > cap) { b3.Add(m); continue; }

                bool daNhan = laThanhTuu ? IsAchievementClaimed(m) : IsAchievementOrMissionClaimed(m, laThanhTuu);
                if (daNhan) { b2.Add(m); continue; }

                int cur = MissionProgressTracker.GetProgressFor(m);
                if (cur >= Mathf.Max(1, m.targetAmount)) b1.Add(m);
                else                                     b0.Add(m);
            }
            b3.Sort((x, y) => x.requiredLevel.CompareTo(y.requiredLevel));
        }

        var ket = new List<MissionData>(b0.Count + b1.Count + b2.Count + b3.Count);
        ket.AddRange(b1); ket.AddRange(b0); ket.AddRange(b2); ket.AddRange(b3);
        return ket;
    }

    private static bool IsAchievementOrMissionClaimed(MissionData m, bool laThanhTuu)
        => laThanhTuu ? IsAchievementClaimed(m) : IsMissionClaimed(m);

    /// <summary>
    /// Nạp danh sách vào kho hàng: dùng lại hàng đã có, chỉ dựng thêm khi thiếu, và
    /// TẮT phần thừa thay vì huỷ. Đổi trang qua lại nhiều lần vẫn không sinh rác.
    /// </summary>
    private void NapDanhSach(List<HangThuong> kho, RectTransform vungCuon,
                             List<MissionData> ds, int capNguoiChoi, bool laThanhTuu)
    {
        _hangDangHien.Clear();

        // Bọc từng hàng trong try/catch. Trước đây một lỗi ở hàng ĐẦU làm chết cả vòng
        // lặp: 27 hàng còn lại không bao giờ được dựng, nhãn vẫn ghi "28 nhiệm vụ" mà
        // màn hình chỉ có một hàng trống chữ. Không có gì chỉ ra nguyên nhân.
        // Giờ hàng lỗi bị bỏ qua, các hàng khác vẫn lên, và Console in đúng tên nhiệm vụ
        // gây lỗi.
        int soLoi = 0;
        for (int i = 0; i < ds.Count; i++)
        {
            HangThuong h = null;
            try
            {
                if (i < kho.Count)
                {
                    h = kho[i];
                }
                else
                {
                    h = DungHangTrong(vungCuon, laThanhTuu);
                    kho.Add(h);
                }

                if (!h.goc.gameObject.activeSelf) h.goc.gameObject.SetActive(true);
                NapHang(h, ds[i], capNguoiChoi, laThanhTuu);
                _hangDangHien[ds[i]] = h;
            }
            catch (System.Exception e)
            {
                soLoi++;
                string ten = ds[i] != null ? ds[i].name : "(null)";
                if (soLoi <= 3)
                    Debug.LogError($"[PopupNV] Dựng hàng #{i} ('{ten}') lỗi: {e.GetType().Name}: " +
                                   $"{e.Message}\n{e.StackTrace}", ds[i]);
                if (h != null && h.goc != null) h.goc.gameObject.SetActive(false);
            }
        }

        if (soLoi > 0)
            Debug.LogError($"[PopupNV] {soLoi}/{ds.Count} hàng dựng lỗi — xem 3 lỗi đầu ở trên.");

        for (int i = ds.Count; i < kho.Count; i++)
        {
            if (kho[i].goc.gameObject.activeSelf)
                kho[i].goc.gameObject.SetActive(false);
        }

        // Cuộn về đầu: đổi trang mà giữ nguyên vị trí cuộn thì người chơi tưởng chưa đổi.
        if (vungCuon != null) vungCuon.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Nạp danh sách vào kho hàng: dùng lại hàng đã có, chỉ dựng thêm khi thiếu, và
    /// TẮT phần thừa thay vì huỷ. Đổi trang qua lại nhiều lần vẫn không sinh rác.
    /// </summary>


    // ═════════════════════════════════════════════════════════════════════════
    //  THÀNH TỰU LÀ CHUỖI NHIỀU MỐC — CHỈ HIỆN MỐC ĐANG LÀM
    // ═════════════════════════════════════════════════════════════════════════
    //  Database thành tựu có 157 mục nhưng KHÔNG phải 157 việc khác nhau: đó là 7 chuỗi,
    //  mỗi chuỗi nhiều bậc tăng dần cùng theo dõi một sự kiện.
    //      "Vua Lúa Nước (Mốc 1) — Gặt 100 bó" → Mốc 2: 200 → Mốc 3: 300 → … 15 bậc
    //  Đổ cả 157 bậc ra danh sách vừa nặng vừa vô nghĩa: người chơi mới thấy 33 dòng
    //  "thu hoạch 100 / 150 / 300 / 450 …" xếp liền nhau, không biết nhìn dòng nào.
    //
    //  Cũng vì vậy chia trang theo cấp không dùng được ở tab này — CẢ 157 mục đều
    //  `requiredLevel: 1`. Cách chia đúng là theo chuỗi: mỗi chuỗi một dòng, hiện đúng
    //  mốc đang làm dở. 157 dòng → 7 dòng, khớp luôn với ảnh mẫu 3.
    private List<MissionData> LocThanhTuuTheoChuoi(out int tongBac, out int bacDaXong)
    {
        tongBac = 0; bacDaXong = 0;
        var ket = new List<MissionData>();
        if (_achievementDatabase == null || _achievementDatabase.missions == null) return ket;

        // Gom theo (loại sự kiện, item) — đúng định nghĩa "cùng một chuỗi".
        var nhom = new Dictionary<string, List<MissionData>>();
        foreach (MissionData m in _achievementDatabase.missions)
        {
            if (m == null) continue;
            string khoa = $"{(int)m.eventType}|{(m.targetItemId ?? string.Empty).Trim().ToLowerInvariant()}";
            if (!nhom.TryGetValue(khoa, out var ds)) nhom[khoa] = ds = new List<MissionData>();
            ds.Add(m);
            tongBac++;
        }

        foreach (var ds in nhom.Values)
        {
            ds.Sort((x, y) => x.targetAmount.CompareTo(y.targetAmount));

            MissionData dangLam = null;
            foreach (MissionData m in ds)
            {
                if (IsAchievementClaimed(m)) { bacDaXong++; continue; }
                if (dangLam == null) dangLam = m;
            }

            // Cả chuỗi đã nhận hết → vẫn hiện bậc cuối để người chơi thấy "đã hoàn tất",
            // chứ không để chuỗi biến mất khỏi danh sách như chưa từng có.
            ket.Add(dangLam ?? ds[ds.Count - 1]);
        }

        // Chuỗi nhận được ngay lên đầu, rồi tới chuỗi đang làm, cuối là chuỗi đã xong.
        ket.Sort((x, y) => HangThanhTuu(x).CompareTo(HangThanhTuu(y)));
        return ket;
    }

    private int HangThanhTuu(MissionData m)
    {
        if (m == null) return 3;
        if (IsAchievementClaimed(m)) return 2;
        return MissionProgressTracker.GetProgressFor(m) >= Mathf.Max(1, m.targetAmount) ? 0 : 1;
    }

    private void CapNhatChanMocChuoi(ChanMoc c, int tongBac, int bacDaXong)
    {
        if (c == null) return;
        int mau = Mathf.Max(1, tongBac);
        c.thanh.fillAmount = (float)bacDaXong / mau;
        c.soTienDo.text = $"{bacDaXong}/{tongBac}";
        c.mota.text = bacDaXong >= tongBac
            ? "Đã hoàn tất mọi thành tựu!"
            : "Hoàn thành các mốc để mở rương thưởng!";
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  DỰNG HÀNG TRỐNG (một lần)
    // ═════════════════════════════════════════════════════════════════════════
    private HangThuong DungHangTrong(RectTransform parent, bool laThanhTuu)
    {
        float cao = laThanhTuu ? TaskPopupDesign.HangCaoTT : TaskPopupDesign.HangCao;
        var kt = new Vector2(TaskPopupDesign.HangRong, cao);
        var h = new HangThuong();

        // Nền hàng: viền 3px #ecd09c + gradient #fffdf4→#fdf6e3 + cạnh dưới dày 5px.
        h.goc = CreateRect(parent, laThanhTuu ? "Achievement_Row" : "Mission_Row", Vector2.zero, kt);
        AddLayoutHeight(h.goc, cao + 5f);   // +5 cho cạnh dưới, không bị hàng sau đè

        CreateImage(h.goc, "Row_EdgeBottom", BoGoc(TaskPopupDesign.HangBoGoc),
            TaskPopupDesign.HangDoCanh, new Vector2(0f, -5f), kt, true);
        CreateImage(h.goc, "Row_Border", BoGoc(TaskPopupDesign.HangBoGoc),
            TaskPopupDesign.HangVien, Vector2.zero, kt + new Vector2(6f, 6f), true);
        h.nenHang = CreateImage(h.goc, "Row_Fill", BoGoc(TaskPopupDesign.HangBoGoc),
            TaskPopupDesign.HangDuoi, Vector2.zero, kt, true).GetComponent<Image>();
        PhuGradient(h.goc, "Row_Fill_Top", TaskPopupDesign.HangTren, Vector2.zero, kt,
            TaskPopupDesign.HangBoGoc);

        // ── cột 1 · khung icon 76×76 bo 20, NGHIÊNG −3° ──────────────────────
        // Xoay cả khung để viền nghiêng theo — chi tiết "juicy" của README.
        float dk = TaskPopupDesign.IconKhungKichThuoc;
        RectTransform khung = CreateRect(h.goc, "IconFrame",
            new Vector2(TaskPopupDesign.XKhungIcon, 0f), new Vector2(dk, dk));
        khung.localRotation = Quaternion.Euler(0f, 0f, TaskPopupDesign.IconNghieng);

        CreateImage(khung, "Frame_Border", BoGoc(TaskPopupDesign.IconBoGoc),
            TaskPopupDesign.KhungIconVien, Vector2.zero, new Vector2(dk + 6f, dk + 6f), true);
        h.iconVien = CreateImage(khung, "Frame_Fill", BoGoc(TaskPopupDesign.IconBoGoc),
            TaskPopupDesign.KhungIconDuoi, Vector2.zero, new Vector2(dk, dk), true).GetComponent<Image>();
        PhuGradient(khung, "Frame_Fill_Top", TaskPopupDesign.KhungIconTren, Vector2.zero,
            new Vector2(dk, dk), TaskPopupDesign.IconBoGoc);

        h.icon = CreateImage(khung, "Img_Icon", null, Color.white, Vector2.zero,
            new Vector2(TaskPopupDesign.IconKichThuoc, TaskPopupDesign.IconKichThuoc), false)
            .GetComponent<Image>();
        h.icon.preserveAspect = true;

        // ── cột 2 · tên 25px (trên) + thanh tiến độ 28px gloss (dưới) ────────
        float xCot = TaskPopupDesign.XCotChu;

        h.ten = CreateText(h.goc, "Txt_Title", "", TaskPopupDesign.CoChuTen,
            TaskPopupDesign.TenBinhThuong, TextAlignmentOptions.Left,
            new Vector2(xCot, 21f), new Vector2(TaskPopupDesign.CotChuRong, 34f), FontStyles.Bold);
        h.ten.textWrappingMode = TextWrappingModes.NoWrap;
        h.ten.overflowMode = TextOverflowModes.Ellipsis;

        var ktTd = new Vector2(TaskPopupDesign.CotChuRong, TaskPopupDesign.TdCao);
        RectTransform mangTd = CreateImage(h.goc, "Progress", BoGoc(TaskPopupDesign.TdBoGoc),
            TaskPopupDesign.TdMang, new Vector2(xCot, -18f), ktTd, true);

        var fill = CreateImage(mangTd, "Fill", BoGoc(TaskPopupDesign.TdBoGoc),
            TaskPopupDesign.TdRuotDuoi, Vector2.zero, ktTd, true);
        h.thanhTienDo = fill.GetComponent<Image>();
        h.thanhTienDo.type = Image.Type.Filled;
        h.thanhTienDo.fillMethod = Image.FillMethod.Horizontal;
        h.thanhTienDo.fillOrigin = 0;

        // Gloss trắng nửa trên — CSS rgba(255,255,255,.42), bo trên.
        CreateImage(mangTd, "Gloss", BoGoc(TaskPopupDesign.TdBoGoc), TaskPopupDesign.TdGloss,
            new Vector2(0f, TaskPopupDesign.TdCao * 0.25f),
            new Vector2(ktTd.x - 6f, TaskPopupDesign.TdCao * 0.5f), true);

        h.chuTienDo = CreateText(mangTd, "Txt_Progress", "", TaskPopupDesign.CoChuTd,
            TaskPopupDesign.TdChu, TextAlignmentOptions.Center, Vector2.zero,
            new Vector2(ktTd.x - 8f, 24f), FontStyles.Bold);
        AddShadow(h.chuTienDo.gameObject, TaskPopupDesign.TdChuVien, new Vector2(0f, -2f));

        // ── cột 3 · 3 chip thưởng (icon trái · số phải) ──────────────────────
        h.oThuong = new OThuong[3];
        for (int i = 0; i < 3; i++)
            h.oThuong[i] = DungOThuong(h.goc, i, laThanhTuu ? TaskPopupDesign.OThuongCao - 4f
                                                            : TaskPopupDesign.OThuongCao);

        // ── cột 4 · nút 156×60 bo 18, cạnh dưới dày 6px, bấm lún ─────────────
        var ktNut = new Vector2(TaskPopupDesign.NutRong, TaskPopupDesign.NutCao);
        RectTransform gocNut = CreateRect(h.goc, "Btn_Action",
            new Vector2(TaskPopupDesign.XNut, 0f), ktNut);

        CreateImage(gocNut, "Btn_EdgeBottom", BoGoc(TaskPopupDesign.NutBoGoc),
            TaskPopupDesign.NutDoCanh, new Vector2(0f, -6f), ktNut, true);
        h.nutVien = CreateImage(gocNut, "Btn_Border", BoGoc(TaskPopupDesign.NutBoGoc),
            TaskPopupDesign.NutNhan.vien, Vector2.zero, ktNut + new Vector2(6f, 6f), true)
            .GetComponent<Image>();
        h.nutNenDuoi = CreateImage(gocNut, "Btn_Fill_Bottom", BoGoc(TaskPopupDesign.NutBoGoc),
            TaskPopupDesign.NutNhan.nenDuoi, Vector2.zero, ktNut, true).GetComponent<Image>();
        h.nutNen = CreateImage(gocNut, "Btn_Fill_Top", DaiGradient(),
            TaskPopupDesign.NutNhan.nen, Vector2.zero,
            new Vector2(ktNut.x - TaskPopupDesign.NutBoGoc * 2f, ktNut.y - 8f), false)
            .GetComponent<Image>();

        Image vungBamNut = CreateImage(gocNut, "Btn_Hit", BoGoc(TaskPopupDesign.NutBoGoc),
            new Color(1f, 1f, 1f, 0f), Vector2.zero, ktNut, true).GetComponent<Image>();
        vungBamNut.raycastTarget = true;

        h.nut = gocNut.gameObject.AddComponent<Button>();
        h.nut.transition = Selectable.Transition.None;
        h.nut.targetGraphic = vungBamNut;
        gocNut.gameObject.AddComponent<UIDragScrollForwarder>();
        h.goc.gameObject.AddComponent<UIDragScrollForwarder>();

        h.nutChu = CreateText(gocNut, "Txt_Label", "", TaskPopupDesign.CoChuNut, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, ktNut - new Vector2(10f, 10f), FontStyles.Bold);
        h.nutChu.textWrappingMode = TextWrappingModes.NoWrap;
        AddShadow(h.nutChu.gameObject, new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, -2f));

        // Chấm đỏ nhô ra góc trên-phải nút (CSS top -10 right -10).
        float nc = TaskPopupDesign.ChamDoKichThuoc * 0.5f;
        // Cha = vành trắng, con = tâm đỏ — cùng lý do với chấm trên tab.
        RectTransform cham = CreateImage(gocNut, "Dot_Claimable", GetCircleSprite(), Color.white,
            new Vector2(ktNut.x * 0.5f + 10f - nc, ktNut.y * 0.5f + 10f - nc),
            new Vector2(TaskPopupDesign.ChamDoKichThuoc + 6f, TaskPopupDesign.ChamDoKichThuoc + 6f), false);
        CreateImage(cham, "Dot_Fill", GetCircleSprite(), TaskPopupDesign.ChamDoGiua, Vector2.zero,
            new Vector2(TaskPopupDesign.ChamDoKichThuoc, TaskPopupDesign.ChamDoKichThuoc), false);
        h.chamDo = cham.gameObject;
        h.chamDo.SetActive(false);

        return h;
    }

    private OThuong DungOThuong(RectTransform hang, int chiSo, float chieuCao)
    {
        var o = new OThuong();
        var kt = new Vector2(TaskPopupDesign.OThuongRong, chieuCao);

        // Chip thưởng: viền 3px #e0b26a + gradient #fff6de→#ffe9bd + cạnh dưới 3px.
        o.goc = CreateRect(hang, $"RewardSlot_{chiSo}",
            new Vector2(TaskPopupDesign.XOThuong(chiSo), 0f), kt);

        CreateImage(o.goc, "Chip_EdgeBottom", BoGoc(TaskPopupDesign.OThuongBoGoc),
            TaskPopupDesign.HangDoCanh, new Vector2(0f, -3f), kt, true);
        CreateImage(o.goc, "Chip_Border", BoGoc(TaskPopupDesign.OThuongBoGoc),
            TaskPopupDesign.OThuongVien, Vector2.zero, kt + new Vector2(6f, 6f), true);
        CreateImage(o.goc, "Chip_Fill", BoGoc(TaskPopupDesign.OThuongBoGoc),
            TaskPopupDesign.OThuongDuoi, Vector2.zero, kt, true);
        PhuGradient(o.goc, "Chip_Fill_Top", TaskPopupDesign.OThuongTren, Vector2.zero, kt,
            TaskPopupDesign.OThuongBoGoc);

        // Icon 36 TRÁI · số PHẢI — thiết kế xếp ngang (ô 134 đủ rộng, "x1000" không cắt).
        float xIcon = -kt.x * 0.5f + 12f + TaskPopupDesign.OThuongIcon * 0.5f;
        o.icon = CreateImage(o.goc, "Img_Icon", null, Color.white, new Vector2(xIcon, 0f),
            new Vector2(TaskPopupDesign.OThuongIcon, TaskPopupDesign.OThuongIcon), false)
            .GetComponent<Image>();
        o.icon.preserveAspect = true;

        float xSo = xIcon + TaskPopupDesign.OThuongIcon * 0.5f + 6f;
        float wSo = kt.x * 0.5f - 12f - xSo;
        o.so = CreateText(o.goc, "Txt_Amount", "", TaskPopupDesign.CoChuOThuong,
            TaskPopupDesign.OThuongChu, TextAlignmentOptions.Left,
            new Vector2(xSo + wSo * 0.5f, 0f), new Vector2(wSo, 26f), FontStyles.Bold);
        o.so.textWrappingMode = TextWrappingModes.NoWrap;

        return o;
    }

    /// <summary>Thanh chuyển trang: ‹  Cấp 5–9 · 39 nhiệm vụ  ›</summary>
    private TMP_Text DungThanhChuyenTrang(RectTransform cha, Vector2 viTri,
                                          UnityEngine.Events.UnityAction lui,
                                          UnityEngine.Events.UnityAction toi,
                                          bool coNut = true)
    {
        if (coNut)
        {
            Button bLui = CreateTextButton(cha, "Btn_PrevPage", "‹", new Vector2(-330f, viTri.y),
                new Vector2(52f, 40f), new Color32(196, 143, 74, 255), 28);
            bLui.onClick.AddListener(lui);

            Button bToi = CreateTextButton(cha, "Btn_NextPage", "›", new Vector2(330f, viTri.y),
                new Vector2(52f, 40f), new Color32(196, 143, 74, 255), 28);
            bToi.onClick.AddListener(toi);
        }

        return CreateText(cha, "Txt_PageLabel", "", 20, new Color32(124, 83, 43, 255),
            TextAlignmentOptions.Center, viTri, new Vector2(480f, 32f), FontStyles.Bold);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NẠP NỘI DUNG VÀO HÀNG CÓ SẴN
    // ═════════════════════════════════════════════════════════════════════════
    private void NapHang(HangThuong h, MissionData data, int capNguoiChoi, bool laThanhTuu)
    {
        h.duLieu = data;
        h.goc.localScale = Vector3.one;

        bool khoa   = data != null && data.requiredLevel > capNguoiChoi;
        bool daNhan = data != null && IsAchievementOrMissionClaimed(data, laThanhTuu);

        int muc  = data != null ? Mathf.Max(1, data.targetAmount) : 1;
        int nay  = data != null ? Mathf.Clamp(MissionProgressTracker.GetProgressFor(data), 0, muc) : 0;
        bool xong = !khoa && data != null && nay >= muc;
        bool nhanDuoc = xong && !daNhan;

        // Thiết kế làm MỜ CẢ HÀNG: khoá 0.55, đã nhận 0.68 — một giá trị CanvasGroup
        // điều khiển toàn bộ, không thể lệch giữa nền và chữ như đổi màu từng phần.
        if (h.doMo == null)
        {
            // KHÔNG dùng `??` với GetComponent: khi thiếu component Unity trả object
            // "giả null" — `== null` bắt được (toán tử nạp chồng) nhưng `??` thì không,
            // và chạm vào nó là MissingComponentException. Chính lỗi này làm 84/84
            // hàng chết trong lần chạy đầu.
            h.doMo = h.goc.GetComponent<CanvasGroup>();
            if (h.doMo == null) h.doMo = h.goc.gameObject.AddComponent<CanvasGroup>();
        }
        h.doMo.alpha = khoa ? TaskPopupDesign.MoKhoa : daNhan ? TaskPopupDesign.MoDaNhan : 1f;
        h.doMo.blocksRaycasts = true;

        // Nhiệm vụ chưa gán icon (Giao đơn, Nấu món, Đạt cấp…) thì ẨN ảnh — để
        // fallback vòng tròn trắng như ảnh chụp trông như lỗi. Khung vàng nghiêng
        // trống tự nó đã là một ô sạch sẽ, chờ chủ dự án gán icon là hiện.
        bool coIcon = data != null && data.missionIcon != null;
        h.icon.enabled = coIcon;
        if (coIcon) { h.icon.sprite = data.missionIcon; h.icon.color = Color.white; }

        h.ten.text  = data != null ? data.missionName : "";
        h.ten.color = (khoa || daNhan) ? TaskPopupDesign.TenMoNhat : TaskPopupDesign.TenBinhThuong;

        h.thanhTienDo.fillAmount = khoa ? 0f : daNhan ? 1f : (float)nay / muc;
        h.thanhTienDo.color = daNhan ? TaskPopupDesign.TdRuotXong : TaskPopupDesign.TdRuotDuoi;
        h.chuTienDo.text = khoa ? $"Mở ở cấp {(data != null ? data.requiredLevel : 0)}" : $"{nay}/{muc}";

        RewardBundle thuong = laThanhTuu ? GetAchievementRewards(data) : GetMissionRewards(data);
        NapOThuong(h.oThuong[0], CoinSprite,    thuong.coin,    new Color32(240, 174, 45, 255), khoa);
        NapOThuong(h.oThuong[1], DiamondSprite, thuong.diamond, new Color32(120, 205, 255, 255), khoa);
        NapOThuong(h.oThuong[2], ExpSprite,     thuong.exp,     new Color32(120, 220, 80, 255), khoa);

        CapNhatNut(h, khoa, daNhan, nhanDuoc, data, laThanhTuu);
    }

    private void NapOThuong(OThuong o, Sprite spr, int soLuong, Color mauDuPhong, bool khoa)
    {
        bool hien = soLuong > 0;
        if (o.goc.gameObject.activeSelf != hien) o.goc.gameObject.SetActive(hien);
        if (!hien) return;

        o.icon.sprite = spr != null ? spr : GetCircleSprite();
        // KHÔNG tự làm mờ icon — CanvasGroup của cả hàng đã lo. Mờ hai lần thì hàng
        // khoá tụt còn 0.55×0.5=0.27, gần như không đọc được.
        o.icon.color  = spr != null ? Color.white : mauDuPhong;
        o.so.text  = "x" + soLuong;
        o.so.color = TaskPopupDesign.OThuongChu;
    }

    /// <summary>
    /// Bốn trạng thái nút gom vào MỘT chỗ, để trạng thái sau khi bấm Nhận và trạng thái
    /// lúc dựng lại không thể lệch nhau — trước đây hai đoạn code riêng, sửa một bên là lệch.
    /// </summary>
    private void CapNhatNut(HangThuong h, bool khoa, bool daNhan, bool nhanDuoc,
                            MissionData data, bool laThanhTuu)
    {
        h.nut.onClick.RemoveAllListeners();

        // Bốn kiểu nút lấy nguyên mã màu thiết kế (TaskPopupDesign.Nut*):
        //   Nhận #a5e05e→#57a51f · Đi làm #ffd977→#f2a636 · Đã nhận #ded4bd · Khoá #cfc7b4
        TaskPopupDesign.KieuNut kieu;
        string chu;

        if (khoa)
        {
            kieu = TaskPopupDesign.NutKhoa;
            chu  = data != null ? $"Cấp {data.requiredLevel}" : kieu.nhan;
        }
        else if (daNhan)
        {
            kieu = TaskPopupDesign.NutDaNhan;
            chu  = kieu.nhan;
        }
        else if (nhanDuoc)
        {
            kieu = TaskPopupDesign.NutNhan;
            chu  = kieu.nhan;
            RectTransform nguon = h.nut.transform as RectTransform;
            if (laThanhTuu) h.nut.onClick.AddListener(() => ClaimAchievement(data, nguon));
            else            h.nut.onClick.AddListener(() => ClaimMission(data, nguon));
        }
        else
        {
            kieu = TaskPopupDesign.NutDiLam;
            chu  = laThanhTuu ? "Đang làm" : kieu.nhan;

            // "Đi làm" phải LÀM GÌ ĐÓ — đóng popup để ra ruộng làm nhiệm vụ.
            if (!laThanhTuu) h.nut.onClick.AddListener(ClosePopupForAction);
        }

        h.nutChu.text  = chu;
        h.nutChu.color = kieu.chu;
        // Nhãn dài ("Đang làm", "Đã nhận") co chữ — để 25 là Ellipsis cắt thành "Đang là…".
        h.nutChu.fontSize = chu.Length > 6 ? TaskPopupDesign.CoChuNut - 4 : TaskPopupDesign.CoChuNut;

        h.nutNen.color = kieu.nen;
        if (h.nutNenDuoi != null) h.nutNenDuoi.color = kieu.nenDuoi;
        if (h.nutVien != null)    h.nutVien.color    = kieu.vien;

        h.nut.interactable = nhanDuoc || (!khoa && !daNhan && !laThanhTuu);
        if (h.chamDo.activeSelf != nhanDuoc) h.chamDo.SetActive(nhanDuoc);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CHÂN TRANG "PHẦN THƯỞNG MỐC"
    // ═════════════════════════════════════════════════════════════════════════
    //  Bản cũ nhét thanh này vào TRONG danh sách cuộn như một dòng nữa, nên với 307 dòng
    //  thì gần như không ai cuộn tới. Giờ là chân trang cố định, dựng một lần, chỉ cập
    //  nhật con số khi đổi trang.
    private ChanMoc DungChanMoc(RectTransform cha, string tieuDe, string moTa)
    {
        var c = new ChanMoc();
        var kt = new Vector2(TaskPopupDesign.HangRong, TaskPopupDesign.MocCao);

        // Banner vàng #ffe2a0→#f5b94e, viền 4px #c07d24, cạnh dưới 5px, bo 22.
        c.goc = CreateRect(cha, "MilestoneFooter", new Vector2(0f, -255f), kt);
        CreateImage(c.goc, "Moc_EdgeBottom", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocDoCanh, new Vector2(0f, -5f), kt, true);
        CreateImage(c.goc, "Moc_Border", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocVien, Vector2.zero, kt + new Vector2(8f, 8f), true);
        CreateImage(c.goc, "Moc_Fill", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocDuoi, Vector2.zero, kt, true);
        PhuGradient(c.goc, "Moc_Fill_Top", TaskPopupDesign.MocTren, Vector2.zero, kt,
            TaskPopupDesign.MocBoGoc);

        // Chỉ may nét đứt inset 6px — vẽ bằng 4 vạch mảnh, uGUI không có viền dashed.
        float ix = kt.x * 0.5f - 9f, iy = kt.y * 0.5f - 9f;
        CreateImage(c.goc, "Stitch_Top",    null, TaskPopupDesign.MocChiMay, new Vector2(0f,  iy), new Vector2(ix * 2f, 3f), true);
        CreateImage(c.goc, "Stitch_Bottom", null, TaskPopupDesign.MocChiMay, new Vector2(0f, -iy), new Vector2(ix * 2f, 3f), true);
        CreateImage(c.goc, "Stitch_Left",   null, TaskPopupDesign.MocChiMay, new Vector2(-ix, 0f), new Vector2(3f, iy * 2f), true);
        CreateImage(c.goc, "Stitch_Right",  null, TaskPopupDesign.MocChiMay, new Vector2( ix, 0f), new Vector2(3f, iy * 2f), true);

        // Túi vàng NHÔ LÊN mép banner (CSS margin-top -26).
        float xRuong = -kt.x * 0.5f + 18f + TaskPopupDesign.MocRuongKichThuoc * 0.5f;
        RectTransform ruong = CreateImage(c.goc, "Img_Chest", ChestSprite, Color.white,
            new Vector2(xRuong, 14f),
            new Vector2(TaskPopupDesign.MocRuongKichThuoc, TaskPopupDesign.MocRuongKichThuoc), false);
        ruong.GetComponent<Image>().preserveAspect = true;

        float xChu = xRuong + TaskPopupDesign.MocRuongKichThuoc * 0.5f + 22f;
        float wChu = TaskPopupDesign.MocTdRong;

        CreateText(c.goc, "Txt_Title", tieuDe, TaskPopupDesign.CoChuMoc - 3, TaskPopupDesign.MocChu,
            TextAlignmentOptions.Left, new Vector2(xChu + (wChu + 30f) * 0.5f, 18f),
            new Vector2(wChu + 30f, 32f), FontStyles.Bold);

        var ktTd = new Vector2(wChu, TaskPopupDesign.MocTdCao);
        RectTransform mang = CreateImage(c.goc, "Milestone_Progress", BoGoc(12f),
            TaskPopupDesign.MocTdMang, new Vector2(xChu + wChu * 0.5f, -14f), ktTd, true);
        var fill = CreateImage(mang, "Fill", BoGoc(12f), TaskPopupDesign.MocTdDuoi,
            Vector2.zero, ktTd, true);
        c.thanh = fill.GetComponent<Image>();
        c.thanh.type = Image.Type.Filled;
        c.thanh.fillMethod = Image.FillMethod.Horizontal;
        c.thanh.fillOrigin = 0;

        c.soTienDo = CreateText(mang, "Txt_Value", "", 16, Color.white,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(wChu - 8f, 22f), FontStyles.Bold);
        AddShadow(c.soTienDo.gameObject, TaskPopupDesign.TdChuVien, new Vector2(0f, -2f));

        c.mota = CreateText(c.goc, "Txt_Desc", moTa, 14, TaskPopupDesign.MocChu,
            TextAlignmentOptions.Left, new Vector2(xChu + wChu * 0.5f, -35f),
            new Vector2(wChu, 20f));

        // Hai chip thưởng bên phải — thiết kế chỉ có vàng + kim cương ở footer.
        float xO = kt.x * 0.5f - 18f - TaskPopupDesign.OThuongRong * 0.5f;
        DungChipTinh(c.goc, "RewardSlot_Diamond", DiamondSprite, "x20", new Vector2(xO, 0f));
        DungChipTinh(c.goc, "RewardSlot_Coin", CoinSprite, "x200",
            new Vector2(xO - TaskPopupDesign.OThuongRong - TaskPopupDesign.OThuongKheHo, 0f));

        return c;
    }

    /// <summary>Chip thưởng CỐ ĐỊNH (không nạp lại) cho chân trang.</summary>
    private void DungChipTinh(RectTransform cha, string ten, Sprite spr, string so, Vector2 viTri)
    {
        var kt = new Vector2(TaskPopupDesign.OThuongRong, TaskPopupDesign.OThuongCao);
        RectTransform goc = CreateRect(cha, ten, viTri, kt);

        CreateImage(goc, "Chip_Border", BoGoc(TaskPopupDesign.OThuongBoGoc),
            TaskPopupDesign.OThuongVien, Vector2.zero, kt + new Vector2(6f, 6f), true);
        CreateImage(goc, "Chip_Fill", BoGoc(TaskPopupDesign.OThuongBoGoc),
            TaskPopupDesign.OThuongDuoi, Vector2.zero, kt, true);
        PhuGradient(goc, "Chip_Fill_Top", TaskPopupDesign.OThuongTren, Vector2.zero, kt,
            TaskPopupDesign.OThuongBoGoc);

        float xIcon = -kt.x * 0.5f + 12f + TaskPopupDesign.OThuongIcon * 0.5f;
        Image ic = CreateImage(goc, "Img_Icon", spr, Color.white, new Vector2(xIcon, 0f),
            new Vector2(TaskPopupDesign.OThuongIcon, TaskPopupDesign.OThuongIcon), false)
            .GetComponent<Image>();
        ic.preserveAspect = true;

        float xSo = xIcon + TaskPopupDesign.OThuongIcon * 0.5f + 6f;
        float wSo = kt.x * 0.5f - 12f - xSo;
        CreateText(goc, "Txt_Amount", so, TaskPopupDesign.CoChuOThuong, TaskPopupDesign.OThuongChu,
            TextAlignmentOptions.Left, new Vector2(xSo + wSo * 0.5f, 0f), new Vector2(wSo, 26f),
            FontStyles.Bold);
    }

    private void CapNhatChanMoc(ChanMoc c, List<MissionData> ds, bool laThanhTuu)
    {
        if (c == null) return;

        int xong = 0, moKhoa = 0;
        int cap = GetPlayerLevel();
        for (int i = 0; i < ds.Count; i++)
        {
            MissionData m = ds[i];
            if (m == null || m.requiredLevel > cap) continue;
            moKhoa++;
            if (IsAchievementOrMissionClaimed(m, laThanhTuu)) xong++;
        }

        int mau = Mathf.Max(1, moKhoa);
        c.thanh.fillAmount = (float)xong / mau;
        c.soTienDo.text = $"{xong}/{moKhoa}";
        c.mota.text = xong >= moKhoa && moKhoa > 0
            ? "Đã xong cả mốc này! Sang mốc sau để nhận thêm."
            : (laThanhTuu ? "Hoàn thành các mốc để mở rương thưởng!"
                          : "Hoàn thành tất cả nhiệm vụ để nhận thưởng đặc biệt!");
    }

    /// <summary>
    /// Đổi đúng một hàng sau khi nhận thưởng: nút thành "Đã nhận", nền nhạt đi, tắt
    /// chấm đỏ. Các hàng khác không bị đụng tới nên không nhấp nháy, không sinh rác.
    /// </summary>
    private void CapNhatMotHang(MissionData data, bool daNhan, bool laThanhTuu)
    {
        if (data == null) return;
        if (!_hangDangHien.TryGetValue(data, out HangThuong h) || h == null || h.goc == null) return;

        NapHang(h, data, GetPlayerLevel(), laThanhTuu);
        StartCoroutine(CoNhipHang(h.goc));
    }

    /// <summary>Nhịp phồng nhẹ để mắt bắt được hàng nào vừa đổi trạng thái.</summary>
    private IEnumerator CoNhipHang(RectTransform rt)
    {
        if (rt == null) yield break;

        const float thoiGian = 0.22f;
        float t = 0f;
        while (t < thoiGian && rt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / thoiGian);
            float s = 1f + 0.05f * Mathf.Sin(k * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    private void ClosePopupForAction() => Close();



    /// <summary>
    /// Ô trên HUD phồng lên rồi về — để mắt bắt được "vàng vừa tăng", kể cả khi con
    /// số đổi quá nhanh. Chờ một nhịp cho icon bay tới nơi rồi mới đập.
    /// </summary>
    private IEnumerator CoDapHud(RectTransform hud, float treGiay)
    {
        if (hud == null) yield break;
        yield return new WaitForSecondsRealtime(treGiay);
        if (hud == null) yield break;

        Vector3 goc = hud.localScale;
        const float thoiGian = 0.34f;
        float t = 0f;
        while (t < thoiGian && hud != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / thoiGian);
            // Nảy một nhịp rồi lún nhẹ trước khi về — cảm giác "va vào" chứ không phải
            // phóng to trơn tuột.
            float s = 1f + 0.32f * Mathf.Sin(k * Mathf.PI) - 0.06f * Mathf.Sin(k * Mathf.PI * 2f);
            hud.localScale = goc * s;
            yield return null;
        }
        if (hud != null) hud.localScale = goc;
    }


    private void BuildDailyContent()
    {
        ClearChildren(_dailyPanel);
        DailyState state = SyncDailyState();

        // Tiêu đề 29px giữa panel — thiết kế chỉ có MỘT dòng, không có subtitle.
        CreateText(_dailyPanel, "Txt_DailyTitle", "Điểm danh mỗi ngày để nhận quà!", 29,
            TaskPopupDesign.TenBinhThuong, TextAlignmentOptions.Center,
            new Vector2(0f, 283f), new Vector2(900f, 40f), FontStyles.Bold);

        // 7 thẻ: (1152 − 6·14) / 7 = 152 mỗi thẻ, cao 300, tâm y = 60.
        DailyReward[] rewards = GetDailyRewards();
        const float theRong = 152f, theCao = 300f, khe = 14f;
        float x0 = -TaskPopupDesign.VungTrongRong * 0.5f + theRong * 0.5f;
        for (int i = 0; i < 7; i++)
            BuildDailyCard(i + 1, rewards[i], state,
                new Vector2(x0 + i * (theRong + khe), 60f), new Vector2(theRong, theCao));

        BuildDailyWeeklyReward();
    }

    private void BuildDailyCard(int day, DailyReward reward, DailyState state,
                                Vector2 position, Vector2 kt)
    {
        // Trạng thái đúng theo vmDay của thiết kế.
        bool daNhan  = day < state.streakDay || (day == state.streakDay && state.claimedToday);
        bool homNay  = day == state.streakDay && !state.claimedToday;
        bool tuongLai = day > state.streakDay;

        // Bảng màu vmDay: past [#f4e6c4,#d8b174] · today [#fff4c2,#ffce3d] · future [#efe3c6,#d8bd8d]
        Color nen, vien;
        if (day == state.streakDay) { nen = TaskPopupDesign.Hex("#fff4c2"); vien = TaskPopupDesign.Hex("#ffce3d"); }
        else if (daNhan)            { nen = TaskPopupDesign.Hex("#f4e6c4"); vien = TaskPopupDesign.Hex("#d8b174"); }
        else                        { nen = TaskPopupDesign.Hex("#efe3c6"); vien = TaskPopupDesign.Hex("#d8bd8d"); }

        RectTransform the = CreateRect(_dailyPanel, $"Daily_Day_{day:00}", position, kt);

        // Hôm nay: vòng glow vàng 5px quanh thẻ (CSS 0 0 0 5px rgba(255,206,61,.35)).
        if (homNay)
            CreateImage(the, "Glow_Ring", BoGoc(24f), TaskPopupDesign.Hex("#ffce3d", 0.35f),
                Vector2.zero, kt + new Vector2(14f, 14f), true);

        CreateImage(the, "Card_EdgeBottom", BoGoc(20f), TaskPopupDesign.Hex("#965f1e", 0.3f),
            new Vector2(0f, -5f), kt, true);
        CreateImage(the, "Card_Border", BoGoc(20f), vien, Vector2.zero, kt + new Vector2(6f, 6f), true);
        CreateImage(the, "Card_Fill", BoGoc(20f), nen, Vector2.zero, kt, true);

        // Band tên ngày trên đỉnh — nâu #c98a3f, hôm nay cam #e6913c.
        var ktBand = new Vector2(kt.x, 44f);
        CreateImage(the, "Band", BoGoc(16f),
            TaskPopupDesign.Hex(homNay ? "#e6913c" : "#c98a3f"),
            new Vector2(0f, kt.y * 0.5f - 22f), ktBand, true);
        TMP_Text nhan = CreateText(the, "Txt_Day", $"Ngày {day}", 21, Color.white,
            TextAlignmentOptions.Center, new Vector2(0f, kt.y * 0.5f - 22f),
            new Vector2(kt.x - 8f, 30f), FontStyles.Bold);
        AddShadow(nhan.gameObject, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -2f));

        // Icon quà 82px + số lượng.
        Image icon = CreateImage(the, "Img_RewardIcon", GetDailyRewardSprite(day),
            new Color32(245, 182, 67, 255), new Vector2(0f, 28f), new Vector2(82f, 82f), false)
            .GetComponent<Image>();
        icon.preserveAspect = true;
        if (icon.sprite != null) icon.color = Color.white;

        CreateText(the, "Txt_Amount", reward.amount, 21, TaskPopupDesign.TenBinhThuong,
            TextAlignmentOptions.Center, new Vector2(0f, -38f), new Vector2(kt.x - 10f, 28f),
            FontStyles.Bold);

        if (homNay)
        {
            // Nút Nhận 112×50 xanh — pulse tĩnh (glow ring đã báo "hôm nay").
            Button claim = CreateTextButton(the, "Btn_ClaimToday", "Nhận",
                new Vector2(0f, -kt.y * 0.5f + 45f), new Vector2(112f, 50f),
                new Color32(104, 186, 45, 255), 22);
            RectTransform claimRect = claim.transform as RectTransform;
            claim.onClick.AddListener(() => ClaimDailyReward(day, reward, claimRect));
        }
        else
        {
            // Chip trạng thái: "Đã nhận" nền xanh chữ trắng · "Ngày mai"/"X ngày nữa" nền be.
            string chu = daNhan ? "Đã nhận"
                       : day == state.streakDay + 1 ? "Ngày mai"
                       : $"{day - state.streakDay} ngày nữa";
            Color nenChip = daNhan ? TaskPopupDesign.Hex("#61a832") : TaskPopupDesign.Hex("#e8d9b4");
            Color chuChip = daNhan ? Color.white : TaskPopupDesign.Hex("#8d7550");

            RectTransform chip = CreateImage(the, "Chip_Status", BoGoc(12f), nenChip,
                new Vector2(0f, -kt.y * 0.5f + 42f), new Vector2(kt.x - 32f, 34f), true);
            CreateText(chip, "Txt_Status", chu, 17, chuChip, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(kt.x - 36f, 30f), FontStyles.Bold);
        }

        // Tick xanh NHÔ góc trên-phải thẻ đã nhận (CSS top -16 right -10).
        if (daNhan)
        {
            // Thứ tự cha→con quyết định thứ tự vẽ: vành trắng NGOÀI cùng làm cha,
            // vòng xanh làm con, hai vạch tick làm cháu — mỗi lớp vẽ sau lớp trước.
            RectTransform rim = CreateImage(the, "Img_Check", GetCircleSprite(), Color.white,
                new Vector2(kt.x * 0.5f - 14f, kt.y * 0.5f + 2f), new Vector2(50f, 50f), false);
            RectTransform xanh = CreateImage(rim, "Check_Green", GetCircleSprite(),
                TaskPopupDesign.Hex("#61a832"), Vector2.zero, new Vector2(44f, 44f), false);
            RectTransform v1 = CreateImage(xanh, "Tick_Short", null, Color.white,
                new Vector2(-7f, -4f), new Vector2(12f, 5f), false);
            v1.localRotation = Quaternion.Euler(0f, 0f, 45f);
            RectTransform v2 = CreateImage(xanh, "Tick_Long", null, Color.white,
                new Vector2(4f, 0f), new Vector2(20f, 5f), false);
            v2.localRotation = Quaternion.Euler(0f, 0f, -50f);
        }

        // Ngày tương lai mờ 62% cả thẻ.
        if (tuongLai)
        {
            var cg = the.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0.62f;
            cg.blocksRaycasts = true;
        }
    }

    private void BuildDailyWeeklyReward()
    {
        // Footer quà tuần — cùng ngôn ngữ với chân mốc: banner vàng + chỉ may + túi nhô.
        var kt = new Vector2(TaskPopupDesign.HangRong, TaskPopupDesign.MocCao);
        RectTransform goc = CreateRect(_dailyPanel, "Daily_WeeklyReward", new Vector2(0f, -255f), kt);

        CreateImage(goc, "Wk_EdgeBottom", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocDoCanh, new Vector2(0f, -5f), kt, true);
        CreateImage(goc, "Wk_Border", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocVien, Vector2.zero, kt + new Vector2(8f, 8f), true);
        CreateImage(goc, "Wk_Fill", BoGoc(TaskPopupDesign.MocBoGoc),
            TaskPopupDesign.MocDuoi, Vector2.zero, kt, true);
        PhuGradient(goc, "Wk_Fill_Top", TaskPopupDesign.MocTren, Vector2.zero, kt,
            TaskPopupDesign.MocBoGoc);

        float ix = kt.x * 0.5f - 9f, iy = kt.y * 0.5f - 9f;
        CreateImage(goc, "Stitch_Top",    null, TaskPopupDesign.MocChiMay, new Vector2(0f,  iy), new Vector2(ix * 2f, 3f), true);
        CreateImage(goc, "Stitch_Bottom", null, TaskPopupDesign.MocChiMay, new Vector2(0f, -iy), new Vector2(ix * 2f, 3f), true);

        float xRuong = -kt.x * 0.5f + 18f + 50f;
        RectTransform ruong = CreateImage(goc, "Img_WeeklyChest", ChestSprite, Color.white,
            new Vector2(xRuong, 14f), new Vector2(100f, 100f), false);
        ruong.GetComponent<Image>().preserveAspect = true;

        float xChu = xRuong + 72f;
        CreateText(goc, "Txt_Title", "Phần thưởng tuần — điểm danh đủ 7 ngày", 21,
            TaskPopupDesign.MocChu, TextAlignmentOptions.Left,
            new Vector2(xChu + 230f, 12f), new Vector2(460f, 30f), FontStyles.Bold);
        CreateText(goc, "Txt_Desc", "Quà tuần đặc biệt đang chờ bạn!", 14,
            TaskPopupDesign.MocChu, TextAlignmentOptions.Left,
            new Vector2(xChu + 230f, -16f), new Vector2(460f, 22f));

        float xO = kt.x * 0.5f - 18f - TaskPopupDesign.OThuongRong * 0.5f;
        float buoc = TaskPopupDesign.OThuongRong + TaskPopupDesign.OThuongKheHo;
        DungChipTinh(goc, "RewardSlot_EXP",     ExpSprite,     "x100", new Vector2(xO, 0f));
        DungChipTinh(goc, "RewardSlot_Diamond", DiamondSprite, "x30",  new Vector2(xO - buoc, 0f));
        DungChipTinh(goc, "RewardSlot_Coin",    CoinSprite,    "x500", new Vector2(xO - buoc * 2f, 0f));
    }


    private void BuildAchievementContent()
    {
        if (_vungCuonThanhTuu == null)
        {
            ClearChildren(_achievementPanel);
            _nhanTrangThanhTuu = DungThanhChuyenTrang(
                _achievementPanel, new Vector2(0f, 283f), null, null, false);

            _vungCuonThanhTuu = BuildVerticalScroll(_achievementPanel, "Achievement_ScrollView",
                new Vector2(0f, 33f), new Vector2(TaskPopupDesign.VungTrongRong, 456f));

            _chanMocThanhTuu = DungChanMoc(_achievementPanel, "Mốc thành tựu",
                "Hoàn thành các mốc để mở rương thưởng!");
        }

        NapLaiTrangThanhTuu();
    }

    private void NapLaiTrangThanhTuu()
    {
        int cap = GetPlayerLevel();
        List<MissionData> ds = LocThanhTuuTheoChuoi(out int tongBac, out int bacDaXong);

        if (_nhanTrangThanhTuu != null)
            _nhanTrangThanhTuu.text = $"{ds.Count} chuỗi thành tựu   ·   {bacDaXong}/{tongBac} mốc";

        NapDanhSach(_khoHangThanhTuu, _vungCuonThanhTuu, ds, cap, true);
        CapNhatChanMocChuoi(_chanMocThanhTuu, tongBac, bacDaXong);
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

        // Đánh dấu đã nhận NGAY LẬP TỨC để chặn spam click
        GhiCoDaNhan(MissionClaimedPrefsKey(data));
        CapNhatMotHang(data, true, false);

        RewardBundle rewards = GetMissionRewards(data);
        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(rewards);
        PlayRewardFly(rewards, src);
        AvatarProfilePopupUI.AddAchievementCount();
    }

    private void ClaimAchievement(MissionData data, RectTransform source)
    {
        if (data == null || IsAchievementClaimed(data))
            return;

        int current = MissionProgressTracker.GetProgressFor(data);
        if (current < data.targetAmount)
            return;

        // Đánh dấu đã nhận NGAY LẬP TỨC để chặn spam click
        GhiCoDaNhan(AchievementClaimedPrefsKey(data));
        CapNhatMotHang(data, true, true);

        RewardBundle rewards = GetAchievementRewards(data);
        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(rewards);
        PlayRewardFly(rewards, src);
        AvatarProfilePopupUI.AddAchievementCount();
    }

    private void ClaimDailyReward(int day, DailyReward reward, RectTransform source)
    {
        DailyState state = SyncDailyState();
        if (state.claimedToday || day != state.streakDay)
            return;

        PlayerPrefs.SetString(DailyClaimedDateKey, TodayKey());
        LuuGopPrefs.Hen();

        Vector3 src = source != null ? source.position : _root.position;
        GrantRewards(reward.grant);
        PlayRewardFly(reward.grant, src);
        ShowTab(Tab.Daily);
    }

    private static void GrantRewards(RewardBundle rewards)
    {
        if (rewards.coin > 0)
            FarmEconomyManager.Instance?.AddGold(rewards.coin);
        if (rewards.diamond > 0)
            FarmEconomyManager.Instance?.AddGems(rewards.diamond);
        if (rewards.exp > 0)
            PlayerProgressManager.Instance?.AddExp(rewards.exp);
    }

    // =========================================================================
    // Reward Fly FX — Bay mượt về đúng Container HUD, tự hủy an toàn không đơ
    // =========================================================================

    private void PlayRewardFly(RewardBundle r, Vector3 sourceWorld)
    {
        RectTransform gemTarget = ResolveGemHud();
        RectTransform expTarget = ResolveExpHud();
        RectTransform coinTarget = ResolveCoinHud();

        if (r.diamond > 0)
            StartCoroutine(CoFlyReward(DiamondSprite, new Color32(120, 205, 255, 255),
                sourceWorld, gemTarget, Mathf.Clamp(r.diamond, 2, 4)));
        if (r.exp > 0)
            StartCoroutine(CoFlyReward(ExpSprite, new Color32(120, 220, 80, 255),
                sourceWorld, expTarget, 3));

        if (r.coin > 0 && coinTarget != null)
            StartCoroutine(CoDapHud(coinTarget, 0.45f));
    }

    private static RectTransform ResolveGemHud()
    {
        var go = GameObject.Find("Diamond_Container")
            ?? GameObject.Find("TopRight_Township_HUD/Diamond_Container")
            ?? GameObject.Find("GemBox");
        return go != null ? go.transform as RectTransform : null;
    }

    private static RectTransform ResolveCoinHud()
    {
        var go = GameObject.Find("Gold_Container")
            ?? GameObject.Find("TopRight_Township_HUD/Gold_Container")
            ?? GameObject.Find("CoinBox");
        return go != null ? go.transform as RectTransform : null;
    }

    private static RectTransform ResolveExpHud()
    {
        var go = GameObject.Find("EXP_Bar_Container")
            ?? GameObject.Find("TopLeft_Township_HUD/EXP_Bar_Container")
            ?? GameObject.Find("Avatar_Lv_Pill");
        if (go != null) return go.transform as RectTransform;

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
            : new Vector2(Screen.width * 0.15f, Screen.height * 0.92f); // Mặc định góc trên bên trái nếu là EXP

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        Sprite spr = icon != null ? icon : GetCircleSprite();
        Color color = icon != null ? Color.white : fallbackColor;

        for (int i = 0; i < count; i++)
        {
            StartCoroutine(CoFlyOne(canvasRect, spr, color, startLocal, endLocal, target));
            yield return new WaitForSecondsRealtime(0.04f);
        }

        StartCoroutine(CoDapHud(target, 0.45f));
    }

    private IEnumerator CoFlyOne(RectTransform canvasRect, Sprite spr, Color color, Vector2 startLocal, Vector2 endLocal, RectTransform targetHud)
    {
        if (canvasRect == null) yield break;

        GameObject go = new GameObject("RewardFly", typeof(RectTransform), typeof(Image));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(canvasRect, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(44f, 44f);
        rt.SetAsLastSibling();

        // Safety Auto-Destroy: Bảo hiểm 100% tự dọn dẹp, không bao giờ bị kẹt lại trên màn hình
        Destroy(go, 1.0f);

        Image img = go.GetComponent<Image>();
        img.sprite = spr;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;

        float startRot = UnityEngine.Random.Range(0f, 360f);
        float rotSpeed = UnityEngine.Random.Range(-300f, 300f);

        Vector2 burst = startLocal + UnityEngine.Random.insideUnitCircle * 55f;
        rt.anchoredPosition = startLocal;
        rt.localScale = Vector3.zero;

        float t = 0f;
        const float burstT = 0.14f;
        while (t < burstT)
        {
            if (go == null) yield break;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / burstT);
            rt.anchoredPosition = Vector2.Lerp(startLocal, burst, k);
            rt.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, k);
            rt.localRotation = Quaternion.Euler(0, 0, startRot + rotSpeed * t);
            yield return null;
        }

        const float dur = 0.38f;
        t = 0f;
        Vector2 curPos = rt != null ? rt.anchoredPosition : burst;
        while (t < dur)
        {
            if (go == null) yield break;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float easeIn = k * k;
            rt.anchoredPosition = Vector2.Lerp(curPos, endLocal, easeIn);
            float s = Mathf.Lerp(1.15f, 0.5f, easeIn);
            rt.localScale = new Vector3(s, s, 1f);
            rt.localRotation = Quaternion.Euler(0, 0, startRot + rotSpeed * (burstT + t));
            yield return null;
        }

        if (targetHud != null)
        {
            JuicyPulseFX.Play(targetHud);
        }

        if (go != null) Destroy(go);
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

        // Không còn trả null: ngày 3-6 trước đây ra khối màu trơn, không đọc được là thưởng gì.
        return day switch
        {
            1 => CoinSprite,
            2 => DiamondSprite,
            7 => ChestSprite,
            _ => ExpSprite
        };
    }

    private struct DailyState
    {
        public int streakDay;
        public bool claimedToday;
    }

    private DailyState SyncDailyState()
    {
        EnsureMissionSaveVersion();

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
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs

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

    // ═════════════════════════════════════════════════════════════════════════
    //  CACHE CỜ "ĐÃ NHẬN"
    // ═════════════════════════════════════════════════════════════════════════
    //  `IsMissionClaimed` được gọi HAI lần cho mỗi nhiệm vụ khi dựng danh sách: một lần
    //  trong `MissionBucket` (phân nhóm) và một lần trong `BuildMissionRow`. Với 307
    //  nhiệm vụ chính + 157 thành tựu là hơn 900 lần `PlayerPrefs.GetInt` mỗi lần dựng.
    //  `GetInt` là lệnh gọi native có marshal chuỗi — không đắt bằng `Save()` nhưng nhân
    //  900 lần thì thành một phần đáng kể của khung hình bị giật.
    //
    //  Chỉ CHÍNH LỚP NÀY ghi các khoá đó, nên cache không thể lệch với đĩa: mọi đường
    //  ghi đều đi qua `ClaimMission`/`ClaimAchievement` và cập nhật cache tại chỗ.
    private static readonly Dictionary<string, bool> _cacheDaNhan = new Dictionary<string, bool>();

    private static bool DocCoDaNhan(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (_cacheDaNhan.TryGetValue(key, out bool co)) return co;

        co = PlayerPrefs.GetInt(key, 0) == 1;
        _cacheDaNhan[key] = co;
        return co;
    }

    private static void GhiCoDaNhan(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        PlayerPrefs.SetInt(key, 1);
        _cacheDaNhan[key] = true;
        LuuGopPrefs.Hen();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Dọn cache khi vào Play Mode. Có hai lý do bắt buộc:
    ///   • Bật "Enter Play Mode Options" (không reload domain) thì `static` giữ nguyên
    ///     giá trị của lần chạy trước.
    ///   • Tool "CHƠI LẠI TỪ ĐẦU" gọi `PlayerPrefs.DeleteAll()` — không dọn cache thì
    ///     popup vẫn tưởng mọi nhiệm vụ đã nhận, và không có gì báo là sai.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DonCacheKhiVaoPlay()
    {
        _cacheDaNhan.Clear();
        _missionVersionChecked = false;
    }
#endif

    private static bool IsMissionClaimed(MissionData data)
    {
        if (data == null) return false;
        EnsureMissionSaveVersion();
        return DocCoDaNhan(MissionClaimedPrefsKey(data));
    }

    private static bool IsAchievementClaimed(MissionData data)
    {
        if (data == null) return false;
        EnsureMissionSaveVersion();
        return DocCoDaNhan(AchievementClaimedPrefsKey(data));
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
        var f = SkinKit.FontVo;
        if (f != null)
        {
            tmp.font = f;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
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

    // ══════════════════════════════════════════════════════════════════════
    //  F7 — SPRITE THỦ TỤC CHO 14 REF RỖNG
    // ══════════════════════════════════════════════════════════════════════
    //
    // VẤN ĐỀ: `UnifiedTaskPopupSprites` có 14 ô sprite và trong `SCN_Farm` TẤT CẢ đều
    // rỗng, nên popup nhiệm vụ dựng ra bằng khối màu trơn: vàng, kim cương, EXP, hòm,
    // ổ khoá đều là hình chữ nhật y như nhau — không đọc được cái nào là cái gì.
    //
    // CÁCH SỬA: sinh sprite ngay trong code như đã làm cho bảng tin chợ và quầy hàng.
    // Nếu chủ dự án gán art thật vào Inspector thì art LUÔN THẮNG (mỗi property kiểm
    // field trước), nên đây thuần là lớp dự phòng, không chặn đường gắn ảnh sau này.
    //
    // Texture đều `HideAndDontSave` + cache static → mỗi hình chỉ sinh một lần cho cả
    // vòng đời app, không rò rỉ asset vào scene.

    private Sprite BoardFrameSprite   => sprites.boardFrame   != null ? sprites.boardFrame   : GetPanelSprite();
    private Sprite PaperPanelSprite   => sprites.paperPanel   != null ? sprites.paperPanel   : GetPanelSprite();
    private Sprite RibbonSprite       => sprites.ribbon       != null ? sprites.ribbon       : GetRoundedSprite();
    private Sprite MascotSprite       => sprites.mascot       != null ? sprites.mascot       : GetRoundedSprite();
    private Sprite LeafSprite         => sprites.leafCluster  != null ? sprites.leafCluster  : GetLeafShapeSprite();
    private Sprite FlowerSprite       => sprites.flowerCluster!= null ? sprites.flowerCluster: GetFlowerShapeSprite();

    private Sprite MissionTabSprite     => sprites.missionTabIcon     != null ? sprites.missionTabIcon     : GetStarSprite();
    private Sprite DailyTabSprite       => sprites.dailyTabIcon       != null ? sprites.dailyTabIcon       : GetCircleSprite();
    private Sprite AchievementTabSprite => sprites.achievementTabIcon != null ? sprites.achievementTabIcon : GetDiamondShapeSprite();

    private Sprite CoinSprite    => sprites.coinIcon    != null ? sprites.coinIcon    : GetCoinShapeSprite();
    private Sprite DiamondSprite => sprites.diamondIcon != null ? sprites.diamondIcon : GetDiamondShapeSprite();
    private Sprite ExpSprite     => sprites.expIcon     != null ? sprites.expIcon     : GetStarSprite();
    private Sprite ChestSprite   => sprites.chestIcon   != null ? sprites.chestIcon   : GetChestShapeSprite();
    private Sprite LockSprite    => sprites.lockIcon    != null ? sprites.lockIcon    : GetLockShapeSprite();

    private static Sprite _panelSprite;
    private static Sprite _coinShape;
    private static Sprite _diamondShape;
    private static Sprite _starShape;
    private static Sprite _chestShape;
    private static Sprite _lockShape;
    private static Sprite _leafShape;
    private static Sprite _flowerShape;

    /// <summary>Nền bo góc lớn (khung gỗ / tờ giấy). Border 9-slice để kéo giãn không méo góc.</summary>
    private static Sprite GetPanelSprite()
    {
        if (_panelSprite != null) return _panelSprite;
        _panelSprite = CreateRoundedSprite("UnifiedTask_Panel", 96, 26);
        return _panelSprite;
    }

    /// <summary>Sinh sprite từ một hàm quyết định "điểm (x,y) có nằm trong hình không".</summary>
    private static Sprite MakeShape(string name, int size, System.Func<float, float, bool> inside)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Quy về hệ toạ độ [-1, 1] để công thức hình không phụ thuộc kích thước
                float u = (x / (float)(size - 1)) * 2f - 1f;
                float v = (y / (float)(size - 1)) * 2f - 1f;
                tex.SetPixel(x, y, inside(u, v) ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        Sprite sp = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sp.name = name;
        sp.hideFlags = HideFlags.HideAndDontSave;
        return sp;
    }

    /// <summary>Đồng vàng: đĩa tròn có vành trong để phân biệt với chấm đỏ thông báo.</summary>
    private static Sprite GetCoinShapeSprite()
    {
        if (_coinShape != null) return _coinShape;
        _coinShape = MakeShape("UnifiedTask_Coin", 96, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            return r <= 0.96f && !(r > 0.66f && r < 0.74f);   // đĩa + khe vành
        });
        return _coinShape;
    }

    private static Sprite GetDiamondShapeSprite()
    {
        if (_diamondShape != null) return _diamondShape;
        _diamondShape = MakeShape("UnifiedTask_Diamond", 96,
            (u, v) => Mathf.Abs(u) + Mathf.Abs(v) <= 0.95f);
        return _diamondShape;
    }

    /// <summary>Ngôi sao 5 cánh — dùng cho EXP và tab Nhiệm vụ.</summary>
    private static Sprite GetStarSprite()
    {
        if (_starShape != null) return _starShape;
        _starShape = MakeShape("UnifiedTask_Star", 96, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            if (r < 0.001f) return true;
            float ang = Mathf.Atan2(v, u);
            // Bán kính dao động 5 nhịp quanh vòng tròn → 5 cánh nhọn
            float wave = 0.62f + 0.34f * Mathf.Cos(5f * (ang - Mathf.PI * 0.5f));
            return r <= wave;
        });
        return _starShape;
    }

    /// <summary>Hòm gỗ: thân chữ nhật + nắp vòm, có khe hở giữa hai phần.</summary>
    private static Sprite GetChestShapeSprite()
    {
        if (_chestShape != null) return _chestShape;
        _chestShape = MakeShape("UnifiedTask_Chest", 96, (u, v) =>
        {
            if (Mathf.Abs(u) > 0.86f) return false;
            if (v >= -0.86f && v <= -0.02f) return true;                      // thân
            if (v > 0.06f && v <= 0.82f) return (u * u) / 0.74f + (v - 0.06f) * (v - 0.06f) / 0.58f <= 1f; // nắp vòm
            return false;
        });
        return _chestShape;
    }

    /// <summary>Ổ khoá: thân chữ nhật + quai hình chữ U ở trên.</summary>
    private static Sprite GetLockShapeSprite()
    {
        if (_lockShape != null) return _lockShape;
        _lockShape = MakeShape("UnifiedTask_Lock", 96, (u, v) =>
        {
            if (Mathf.Abs(u) <= 0.62f && v >= -0.88f && v <= 0.12f) return true;   // thân
            if (v > 0.12f)
            {
                float r = Mathf.Sqrt(u * u + (v - 0.12f) * (v - 0.12f));
                return r <= 0.52f && r >= 0.28f;                                    // quai
            }
            return false;
        });
        return _lockShape;
    }

    /// <summary>Chùm lá: hai hình bầu dục nghiêng ngược nhau.</summary>
    private static Sprite GetLeafShapeSprite()
    {
        if (_leafShape != null) return _leafShape;
        _leafShape = MakeShape("UnifiedTask_Leaf", 96, (u, v) =>
        {
            bool a = ((u + 0.32f) * (u + 0.32f)) / 0.30f + ((v - 0.10f) * (v - 0.10f)) / 0.09f <= 1f;
            bool b = ((u - 0.32f) * (u - 0.32f)) / 0.30f + ((v + 0.10f) * (v + 0.10f)) / 0.09f <= 1f;
            return a || b;
        });
        return _leafShape;
    }

    /// <summary>Chùm hoa: bốn cánh tròn quanh một tâm tròn.</summary>
    private static Sprite GetFlowerShapeSprite()
    {
        if (_flowerShape != null) return _flowerShape;
        _flowerShape = MakeShape("UnifiedTask_Flower", 96, (u, v) =>
        {
            if (u * u + v * v <= 0.11f) return true;                       // tâm
            Vector2[] petals =
            {
                new Vector2(0f, 0.52f), new Vector2(0f, -0.52f),
                new Vector2(0.52f, 0f), new Vector2(-0.52f, 0f)
            };
            for (int i = 0; i < petals.Length; i++)
            {
                float du = u - petals[i].x, dv = v - petals[i].y;
                if (du * du + dv * dv <= 0.14f) return true;
            }
            return false;
        });
        return _flowerShape;
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

    // ═════════════════════════════════════════════════════════════════════════
    //  SPRITE CHO BẢN THIẾT KẾ — BoGoc / DaiGradient / PhuGradient
    // ═════════════════════════════════════════════════════════════════════════
    //  uGUI không có gradient. Thiết kế dùng `linear-gradient(180deg,A,B)` ở gần như
    //  mọi thành phần, nên dựng bằng HAI lớp: dưới = màu B đặc, trên = màu A với dải
    //  alpha giảm dần.
    //
    //  ⚠ BÀI HỌC từ lần hỏng trước: KHÔNG làm sprite gradient có bo góc rồi vẽ Sliced.
    //  Sliced kéo giãn vùng GIỮA sprite — toàn bộ chuyển sắc nằm đúng đó, bị bóp thành
    //  một hàng pixel; với ván gỗ cao 850px hàng đó rơi vào alpha≈0 → tấm ván trắng
    //  toát. Gradient phải là ảnh 1×64 vẽ Simple.
    private static readonly Dictionary<string, Sprite> _khoSpriteTK = new Dictionary<string, Sprite>();

    /// <summary>Chữ nhật bo góc alpha đặc, 9-slice được. Bán kính theo pixel thiết kế.</summary>
    private static Sprite BoGoc(float banKinh)
    {
        string khoa = $"bogoc_{banKinh:0.#}";
        if (_khoSpriteTK.TryGetValue(khoa, out Sprite co) && co != null) return co;

        int r = Mathf.Max(2, Mathf.RoundToInt(banKinh));
        int n = r * 4 + 8;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        { name = khoa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
          hideFlags = HideFlags.HideAndDontSave };

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = x < r ? r - x : (x >= n - r ? x - (n - r - 1) : 0f);
            float dy = y < r ? r - y : (y >= n - r ? y - (n - r - 1) : 0f);
            float a = (dx <= 0f || dy <= 0f) ? 1f : Mathf.Clamp01(r - Mathf.Sqrt(dx*dx+dy*dy) + 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                                SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
        spr.name = khoa; spr.hideFlags = HideFlags.HideAndDontSave;
        _khoSpriteTK[khoa] = spr;
        return spr;
    }

    /// <summary>Dải gradient dọc 1×64: alpha 1 trên, 0 dưới. Vẽ Simple, KHÔNG Sliced.</summary>
    private static Sprite DaiGradient()
    {
        const string khoa = "dai_gradient";
        if (_khoSpriteTK.TryGetValue(khoa, out Sprite co) && co != null) return co;

        const int n = 64;
        var tex = new Texture2D(1, n, TextureFormat.RGBA32, false)
        { name = khoa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
          hideFlags = HideFlags.HideAndDontSave };

        for (int y = 0; y < n; y++)
        {
            float t = (float)y / (n - 1);          // y=0 là ĐÁY texture → alpha thấp
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, t)));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, 1, n), new Vector2(0.5f, 0.5f), 100f);
        spr.name = khoa; spr.hideFlags = HideFlags.HideAndDontSave;
        _khoSpriteTK[khoa] = spr;
        return spr;
    }

    /// <summary>
    /// Phủ lớp gradient dọc lên khối đã có nền bo góc. Thu vào theo bán kính để lớp
    /// phẳng không chìa ra khỏi bốn góc bo của nền.
    /// </summary>
    private static void PhuGradient(Transform cha, string ten, Color mau,
                                    Vector2 viTri, Vector2 kichThuoc, float banKinh)
    {
        // Thu 60% bán kính là đủ né góc bo — bản trước thu nguyên bán kính làm lớp
        // gradient trên hụt hẳn so với nền, plate ribbon lộ mép cam dày và cả khối
        // nhìn cam thay vì vàng như thiết kế.
        float thu = Mathf.Min(banKinh * 0.6f, Mathf.Min(kichThuoc.x, kichThuoc.y) * 0.2f);
        RectTransform rt = CreateRect(cha, ten, viTri, new Vector2(kichThuoc.x - thu * 2f, kichThuoc.y - thu * 0.5f));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = DaiGradient();
        img.type = Image.Type.Simple;
        img.color = mau;
        img.raycastTarget = false;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void DonKhoSpriteTK() => _khoSpriteTK.Clear();
#endif

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
