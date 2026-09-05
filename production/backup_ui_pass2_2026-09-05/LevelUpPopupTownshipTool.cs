using System.Collections.Generic;
using System.Linq;              // cần cho OfType<Sprite>().FirstOrDefault() khi nạp sub-asset
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TOOL DỰNG POPUP LÊN CẤP — PHONG CÁCH TOWNSHIP
/// ═══════════════════════════════════════════════
/// Menu: Tools ▸ Farm ▸ Popup Lên Cấp (Township)
///
/// Dựng nguyên cây Hierarchy A-Z theo đúng bố cục video tham chiếu:
///
///   Nền tối toàn màn  →  quầng sáng ấm  →  [CHỖ TRỐNG CHO NHÂN VẬT]
///   →  băng rôn xanh + chữ vàng  →  ngôi sao số cấp  →  dòng phần thưởng
///   →  dải icon mở khoá cuộn ngang (khung tròn + nhãn NEW)  →  nút xanh
///
/// • Sprite sinh thủ tục, KHÔNG cần import art ngoài.
/// • Pháo hoa: tự tìm prefab VFX Lana Studio đã có sẵn trong project và gắn vào.
/// • Nhân vật: chỉ tạo Ô TRỐNG có đánh dấu — bạn tự thả art vào sau.
/// • Tự động nối dây (wire) vào script LevelUpPopupUI đang có, không viết lại logic.
/// </summary>
public class LevelUpPopupTownshipTool : EditorWindow
{
    // ── Tuỳ chọn ────────────────────────────────────────────────────────
    private Canvas _targetCanvas;
    private string _titleText      = "CẤP MỚI!";
    private string _rewardLabel    = "Phần thưởng:";
    private string _buttonText     = "Bắt đầu nào";
    private int    _previewLevel   = 5;
    private int    _slotCount      = 9;
    private bool   _regenSprites   = false;
    private bool   _wireToExisting = true;
    private bool   _addFireworks   = true;

    // Kích thước tham chiếu (khớp CanvasScaler phổ biến 1080x1920 dọc / 1920x1080 ngang)
    private const float BANNER_W   = 980f;
    private const float BANNER_H   = 150f;
    private const float STAR_SIZE  = 250f;
    private const float SLOT_SIZE  = 190f;
    private const float STRIP_H    = 250f;

    private const string ROOT_NAME = "Popup_LevelUp_Township";

    // ── ICON TIỀN TỆ THẬT ───────────────────────────────────────────────
    // Đúng 2 sprite mà HUD góc trên-trái đang dùng (DEV-A dò từ SCN_Farm.unity,
    // xem TEAM_LEVELUP_REWARDS.md §4.1). Trước đây dòng "Phần thưởng" dùng
    // spr_circle_fill tô màu → chỉ là đĩa tròn phẳng, không phải xu/kim cương.
    //
    // ⚠ Tên thư mục xu vàng có HAI DẤU CÁCH: "Fantasy Wooden GUI␣␣Free".
    //   ĐỪNG "sửa" thành một dấu cách, đường dẫn sẽ sai và icon quay về đĩa tròn.
    //
    // Hardcode đường dẫn ở đây CHẤP NHẬN ĐƯỢC vì đây là code EDITOR (dựng scene
    // một lần rồi tham chiếu nằm trong scene). Code RUNTIME vẫn không hardcode gì.
    private const string PATH_ICON_GOLD =
        "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/vang-removebg-preview.png";
    private const string PATH_ICON_GEM =
        "Assets/Assetsgame/kimcuong-removebg-preview.png";

    [MenuItem("Tools/Farm/Popup Lên Cấp (Township)", false, 20)]
    public static void Open()
    {
        var w = GetWindow<LevelUpPopupTownshipTool>(true, "Popup Lên Cấp — Township");
        w.minSize = new Vector2(420, 560);
        w.Show();
    }

    private void OnEnable()
    {
        if (_targetCanvas == null) _targetCanvas = PickBestCanvas();
    }

    /// <summary>
    /// Chọn Canvas đích an toàn. CHỈ nhận Canvas GỐC + Screen Space.
    ///
    /// ⚠️ BUG ĐÃ TỪNG XẢY RA: code cũ chỉ kiểm `name.Contains("popup")`.
    /// Prefab `House_02` có GameObject `OrderPopup2` — bong bóng đơn hàng trên đầu nhà,
    /// dùng Canvas WORLD SPACE, scale 0.005, và `HouseOrderBubble.Awake()` gọi
    /// SetActive(false) ngay frame đầu. Tên nó chứa "popup" nên tool bốc trúng.
    /// Scene có ~50 căn nhà như vậy và FindObjectsSortMode.None không bảo đảm thứ tự
    /// → popup bị dựng vào bụng một căn nhà, co nhỏ 67 lần, rồi bị tắt → VÔ HÌNH.
    /// </summary>
    private static Canvas PickBestCanvas()
    {
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                                                   FindObjectsSortMode.None);
        Canvas best      = null;
        int    bestScore = int.MinValue;

        foreach (var c in all)
        {
            if (c == null)                              continue;
            if (c.renderMode == RenderMode.WorldSpace)  continue;  // loại bong bóng world-space
            if (c.rootCanvas != c)                      continue;  // chỉ nhận canvas GỐC
            // Loại mọi canvas nằm trong prefab công trình
            if (IsInsideBuildingPrefab(c.transform))    continue;

            string n = c.name.ToLowerInvariant();
            int score = 0;
            if (n == "canvas_popup")                        score += 100;
            else if (n.Contains("popup"))                   score += 50;
            if (c.GetComponent<CanvasScaler>() != null)     score += 10;
            if (c.gameObject.activeInHierarchy)             score += 5;

            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best;
    }

    /// <summary>Canvas có tổ tiên là nhà/chuồng/công trình → không được dùng làm popup UI.</summary>
    private static bool IsInsideBuildingPrefab(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            string n = p.name.ToLowerInvariant();
            if (n.StartsWith("house") || n.StartsWith("pen_") ||
                n.Contains("orderanchor") || n.Contains("orderpopup") ||
                n.Contains("bubble"))
                return true;
        }
        return false;
    }

    /// <summary>Canvas có dùng được không? Trả về lý do nếu không.</summary>
    private static bool IsCanvasValid(Canvas c, out string reason)
    {
        if (c == null)
        { reason = "Chưa chọn Canvas."; return false; }

        if (c.renderMode == RenderMode.WorldSpace)
        { reason = $"'{c.name}' là Canvas WORLD SPACE — popup sẽ nằm trong thế giới game, " +
                   "không bao giờ hiện trên màn hình."; return false; }

        if (c.rootCanvas != c)
        { reason = $"'{c.name}' là Canvas LỒNG (canvas gốc là '{c.rootCanvas.name}'). " +
                   "Hãy chọn canvas gốc."; return false; }

        if (IsInsideBuildingPrefab(c.transform))
        { reason = $"'{c.name}' nằm bên trong prefab công trình (nhà/chuồng/bong bóng đơn hàng). " +
                   "Script của công trình sẽ tắt nó lúc chạy."; return false; }

        reason = null;
        return true;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("POPUP LÊN CẤP — PHONG CÁCH TOWNSHIP", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Dựng đầy đủ Hierarchy theo video tham chiếu.\n" +
            "• Sprite sinh thủ tục — không cần art ngoài.\n" +
            "• Nhân vật chỉ tạo Ô TRỐNG, bạn tự thả art vào.\n" +
            "• Pháo hoa tự gắn nếu tìm thấy prefab VFX trong project.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        _targetCanvas = (Canvas)EditorGUILayout.ObjectField("Canvas đích", _targetCanvas, typeof(Canvas), true);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Nội dung", EditorStyles.boldLabel);
        _titleText    = EditorGUILayout.TextField("Chữ tiêu đề",   _titleText);
        _rewardLabel  = EditorGUILayout.TextField("Nhãn thưởng",   _rewardLabel);
        _buttonText   = EditorGUILayout.TextField("Chữ trên nút",  _buttonText);
        _previewLevel = EditorGUILayout.IntSlider("Cấp xem thử",   _previewLevel, 1, 30);
        _slotCount    = EditorGUILayout.IntSlider("Số ô mở khoá",  _slotCount, 1, 14);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Tuỳ chọn", EditorStyles.boldLabel);
        _addFireworks   = EditorGUILayout.Toggle(new GUIContent("Gắn pháo hoa",
                            "Tự tìm prefab VFX confetti trong project"), _addFireworks);
        _wireToExisting = EditorGUILayout.Toggle(new GUIContent("Nối vào LevelUpPopupUI",
                            "Gán tham chiếu vào script logic đã có, không viết lại"), _wireToExisting);
        _regenSprites   = EditorGUILayout.Toggle(new GUIContent("Sinh lại sprite",
                            "Ghi đè sprite cũ. Bật khi bạn đổi màu trong PopupSpriteFactory."), _regenSprites);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.55f, 0.9f, 0.5f);
        if (GUILayout.Button("DỰNG POPUP", GUILayout.Height(42)))
            Build();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Chỉ sinh lại Sprite", GUILayout.Height(24)))
        {
            PopupSpriteFactory.GenerateAll(true);
            Debug.Log("[LevelUpTool] Đã sinh lại sprite vào " + PopupSpriteFactory.ArtFolder);
        }

        if (GUILayout.Button("Xoá popup cũ trong scene", GUILayout.Height(24)))
            DeleteExisting();

        // ── KIỂM TRA & TEST ─────────────────────────────────────────────
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Kiểm tra", EditorStyles.boldLabel);

        if (GUILayout.Button("① Chẩn đoán (popup có trong scene không?)", GUILayout.Height(26)))
            Diagnose();

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        if (GUILayout.Button(Application.isPlaying
                ? $"② Bật thử popup cấp {_previewLevel} NGAY"
                : "② Bật thử popup — cần bấm Play trước", GUILayout.Height(26)))
            TestShowPopup();

        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button(Application.isPlaying
                ? "③ CHỤP ẢNH + XUẤT BÁO CÁO  (hoặc bấm F10)"
                : "③ Chụp ảnh — cần bấm Play trước", GUILayout.Height(30)))
            PopupCaptureReporter.CaptureNow();
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField("   → xuất ra Assets/_Debug_Capture/", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "SAU KHI DỰNG:\n" +
            "1. Mở nhóm 'Layer_NhanVat' → thả art nhân vật vào các ô trống.\n" +
            "2. Ô 'Sau_BangRon' nằm SAU băng rôn (bị che chân), 'Truoc_BangRon' nằm TRƯỚC.\n" +
            "3. Icon mở khoá TỰ ĐỘNG: tool nối 9 ô vào LevelUpPopupUI, lúc chạy script đọc\n" +
            "   LevelRewardConfig.GetUnlockEntries() → nạp icon và ẨN ô thừa.\n" +
            "   Thiếu icon? Chạy Tools ▸ Farm ▸ Điền Icon Unlock (Level Reward).",
            MessageType.None);
    }

    // ════════════════════════════════════════════════════════════════════
    // DỰNG
    // ════════════════════════════════════════════════════════════════════

    private void Build()
    {
        // ── CHỐT CỨNG: canvas sai là nguyên nhân popup vô hình lần trước ──
        if (!IsCanvasValid(_targetCanvas, out string badReason))
        {
            var suggest = PickBestCanvas();
            EditorUtility.DisplayDialog("Canvas không dùng được",
                badReason + "\n\n" +
                (suggest != null
                    ? $"Đề xuất: dùng '{suggest.name}'. Bấm OK để tự đổi sang canvas này."
                    : "Không tìm thấy Canvas Screen Space gốc nào trong scene. " +
                      "Hãy tạo một Canvas (Screen Space - Overlay) rồi thử lại."),
                "OK");

            if (suggest != null) _targetCanvas = suggest;
            Repaint();
            return;
        }

        PopupSpriteFactory.GenerateAll(_regenSprites);

        DeleteExisting();

        // ── HOLDER — LUÔN BẬT, chứa script ───────────────────────────────
        // CỰC QUAN TRỌNG: LevelUpPopupUI.Start() gọi popupRoot.SetActive(false).
        // Nếu đặt script NGAY TRÊN popupRoot thì nó tự tắt chính mình → PopulateUI
        // chạy trên object đã tắt, coroutine không khởi động được. Vì vậy phải tách:
        //   Holder (luôn bật, giữ script)  →  Root_HienThi (bị bật/tắt)
        var root = NewUI(ROOT_NAME, _targetCanvas.transform);
        Stretch(root);

        // Canvas riêng để popup luôn nằm trên HUD (HUD hiện đang là 100)
        var ownCanvas = root.gameObject.AddComponent<Canvas>();
        ownCanvas.overrideSorting = true;
        ownCanvas.sortingOrder    = 300;
        root.gameObject.AddComponent<GraphicRaycaster>();

        // ── ROOT HIỂN THỊ — đây mới là popupRoot bị bật/tắt ───────────────
        var visibleRoot = NewUI("Root_HienThi", root);
        Stretch(visibleRoot);
        var cg = visibleRoot.gameObject.AddComponent<CanvasGroup>();

        // ── 1. NỀN TỐI ───────────────────────────────────────────────────
        var dim = NewImage("Bg_NenToi", visibleRoot, null);
        Stretch(dim.rectTransform);
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        dim.raycastTarget = true;   // chặn click xuyên xuống map

        // ── 2. KHUNG NỘI DUNG ────────────────────────────────────────────
        var content = NewUI("Content", visibleRoot);
        Stretch(content);

        // ── 3. QUẦNG SÁNG ẤM (sau ngôi sao) ──────────────────────────────
        var glow = NewImage("FX_QuangSang", content, PopupSpriteFactory.Load("spr_glow_radial"));
        Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(900f, 900f));
        glow.color = new Color(1f, 0.85f, 0.45f, 0.75f);
        glow.raycastTarget = false;

        // ── 4. NHÂN VẬT NẰM SAU BĂNG RÔN (bị băng rôn che chân) ──────────
        // QUAN TRỌNG: phải là con TRỰC TIẾP của content thì mới xen được
        // vào giữa quầng sáng và băng rôn theo thứ tự sibling.
        var behind = NewUI("Layer_NhanVat_Sau  ◄ THẢ ART VÀO ĐÂY", content);
        Stretch(behind);
        // 4 ô: 2 trái, 2 phải — bố cục giống video (gà, bò | cừu, lợn)
        MakeCharSlot(behind, "Slot_Trai_2", new Vector2(-330f, 235f), 210f);
        MakeCharSlot(behind, "Slot_Trai_1", new Vector2(-195f, 250f), 250f);
        MakeCharSlot(behind, "Slot_Phai_1", new Vector2( 195f, 250f), 250f);
        MakeCharSlot(behind, "Slot_Phai_2", new Vector2( 330f, 235f), 210f);

        // ── 5. BĂNG RÔN XANH ─────────────────────────────────────────────
        var bannerGroup = NewUI("BangRon", content);
        Anchor(bannerGroup, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(BANNER_W, BANNER_H));

        var tailL = NewImage("Duoi_Trai", bannerGroup, PopupSpriteFactory.Load("spr_banner_tail"));
        Anchor(tailL.rectTransform, new Vector2(0f, 0.5f), new Vector2(-46f, -14f), new Vector2(96f, 132f));
        tailL.rectTransform.localScale = new Vector3(-1f, 1f, 1f);   // lật gương
        tailL.raycastTarget = false;

        var tailR = NewImage("Duoi_Phai", bannerGroup, PopupSpriteFactory.Load("spr_banner_tail"));
        Anchor(tailR.rectTransform, new Vector2(1f, 0.5f), new Vector2(46f, -14f), new Vector2(96f, 132f));
        tailR.raycastTarget = false;

        var bannerBody = NewImage("Than_BangRon", bannerGroup, PopupSpriteFactory.Load("spr_banner_body"));
        Stretch(bannerBody.rectTransform);
        bannerBody.type = Image.Type.Sliced;
        bannerBody.raycastTarget = false;

        var title = NewText("Text_TieuDe", bannerGroup, _titleText, 82, FontStyles.Bold);
        Stretch(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(120f, -22f);
        title.rectTransform.offsetMax = new Vector2(-120f, -22f);
        title.color = PopupSpriteFactory.Hex("#FFF4B8");
        title.characterSpacing = 4f;
        AddTextOutline(title, PopupSpriteFactory.Hex("#8A3D00"), 0.35f);
        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color32(10, 45, 80, 240);
        titleShadow.effectDistance = new Vector2(0f, -6f);

        // ── 5b. NHÂN VẬT NẰM TRƯỚC BĂNG RÔN (đè lên băng rôn, như con lợn) ─
        var front = NewUI("Layer_NhanVat_Truoc  ◄ THẢ ART VÀO ĐÂY", content);
        Stretch(front);
        MakeCharSlot(front, "Slot_Truoc_Phai", new Vector2(340f, 175f), 235f);
        MakeCharSlot(front, "Slot_Truoc_Trai", new Vector2(-340f, 175f), 235f);

        // ── 6. NGÔI SAO + SỐ CẤP (trước băng rôn, nằm cao không che chữ) ─
        var starGroup = NewUI("NgoiSao", content);
        Anchor(starGroup, new Vector2(0.5f, 0.5f), new Vector2(0f, 275f), new Vector2(STAR_SIZE, STAR_SIZE));

        var star = NewImage("Hinh_Sao", starGroup, PopupSpriteFactory.Load("spr_star"));
        Stretch(star.rectTransform);
        star.raycastTarget = false;

        var lvNum = NewText("Text_SoCap", starGroup, _previewLevel.ToString(), 96, FontStyles.Bold);
        Anchor(lvNum.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(180f, 130f));
        lvNum.color = Color.white;
        AddTextOutline(lvNum, PopupSpriteFactory.Hex("#0E6FA8"), 0.30f);

        // ── 7. DÒNG PHẦN THƯỞNG ──────────────────────────────────────────
        var rewardRow = NewUI("Hang_PhanThuong", content);
        Anchor(rewardRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(BANNER_W, 74f));
        var hl = rewardRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment      = TextAnchor.MiddleCenter;
        hl.spacing             = 14f;
        hl.childForceExpandWidth  = false;
        hl.childForceExpandHeight = false;
        hl.childControlWidth   = true;
        hl.childControlHeight  = true;

        // KHÔNG gắn ContentSizeFitter cho con của LayoutGroup — Unity cảnh báo
        // và hai bên tranh nhau ghi sizeDelta. childControlWidth=true đã tự lấy
        // preferredWidth từ TMP rồi.
        var lblReward = NewText("Text_Nhan", rewardRow, _rewardLabel, 46, FontStyles.Bold);
        lblReward.color = PopupSpriteFactory.Hex("#FFF6E0");
        AddTextOutline(lblReward, new Color(0.15f, 0.08f, 0f, 1f), 0.25f);

        // Sprite THẬT cho xu vàng / kim cương (giống HUD). Null → tự lùi về đĩa tròn tô màu.
        Sprite goldIcon = LoadRealSprite(PATH_ICON_GOLD, "XU VÀNG");
        Sprite gemIcon  = LoadRealSprite(PATH_ICON_GEM,  "KIM CƯƠNG");

        var goldRow = MakeCurrencyChip(rewardRow, "Hang_Vang", "250",
                                       goldIcon, PopupSpriteFactory.Hex("#FFC531"), out var goldText);
        var plus = NewText("Text_Cong", rewardRow, "+", 46, FontStyles.Bold);
        plus.color = PopupSpriteFactory.Hex("#FFF6E0");
        AddTextOutline(plus, new Color(0.15f, 0.08f, 0f, 1f), 0.25f);

        var gemRow = MakeCurrencyChip(rewardRow, "Hang_Ngoc", "25",
                                      gemIcon, PopupSpriteFactory.Hex("#7ED957"), out var gemText);

        // ── 8. DẢI ICON MỞ KHOÁ (cuộn ngang) ─────────────────────────────
        // Giãn hết chiều rộng màn hình (giống video: dải icon chạy sát 2 mép),
        // cố định chiều cao.
        var strip = NewUI("Dai_MoKhoa", content);
        Anchor2(strip, 0f, 1f, new Vector2(0f, -272f), STRIP_H);

        var band = NewImage("Nen_Dai", strip, PopupSpriteFactory.Load("spr_band_dark"));
        Stretch(band.rectTransform);
        band.type = Image.Type.Sliced;
        band.color = new Color(1f, 1f, 1f, 0.85f);
        band.raycastTarget = false;

        var scroll = NewUI("ScrollView", strip);
        Stretch(scroll);
        var sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = true; sr.vertical = false;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.elasticity = 0.1f;
        sr.scrollSensitivity = 30f;

        var viewport = NewUI("Viewport", scroll);
        Stretch(viewport);
        // RectMask2D rẻ hơn Mask (không tốn draw-call ghi stencil, TMP không phải
        // sinh material masking riêng). Vẫn cần 1 Image alpha 0 để bắt thao tác kéo.
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color         = new Color(1f, 1f, 1f, 0f);
        vpImg.raycastTarget = true;

        var scrollContent = NewUI("Content", viewport);
        scrollContent.anchorMin = new Vector2(0f, 0f);
        scrollContent.anchorMax = new Vector2(0f, 1f);
        scrollContent.pivot     = new Vector2(0f, 0.5f);
        scrollContent.sizeDelta = new Vector2(0f, 0f);

        var chl = scrollContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        chl.childAlignment = TextAnchor.MiddleLeft;
        chl.spacing        = 18f;
        chl.padding        = new RectOffset(24, 24, 0, 0);
        chl.childForceExpandWidth  = false;
        chl.childForceExpandHeight = false;
        chl.childControlWidth  = false;
        chl.childControlHeight = false;

        var csf = scrollContent.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

        sr.viewport = viewport;
        sr.content  = scrollContent;

        // Giữ lại tham chiếu để nối dây vào LevelUpPopupUI ở bước 12.
        // Nếu KHÔNG nối, script không biết 9 ô nằm ở đâu → mọi ô trắng trơn khi chạy.
        var unlockSlotList = new List<UnlockSlotUI>(_slotCount);
        for (int i = 0; i < _slotCount; i++)
            unlockSlotList.Add(BuildUnlockSlot(scrollContent, i));

        // ── 8b. HÀNG QUÀ RIÊNG (do LevelUpPopupUI sinh lúc chạy) ─────────
        // BẮT BUỘC tách khỏi dải mở khoá: BuildProceduralGiftSlots() sẽ
        // SetActive(false) mọi con của giftItemsContainer không phải gift slot
        // → nếu dùng chung scrollContent thì 9 ô mở khoá bị tắt sạch khi popup mở.
        var giftRow = NewUI("Hang_Qua", content);
        Anchor(giftRow, new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(BANNER_W, 96f));
        var ghl = giftRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        ghl.childAlignment         = TextAnchor.MiddleCenter;
        ghl.spacing                = 16f;
        ghl.childForceExpandWidth  = false;
        ghl.childForceExpandHeight = false;

        // ── 9. NÚT XANH ──────────────────────────────────────────────────
        var btnRT = NewUI("Btn_TiepTuc", content);
        Anchor(btnRT, new Vector2(0.5f, 0.5f), new Vector2(0f, -462f), new Vector2(420f, 118f));

        var btnImg = btnRT.gameObject.AddComponent<Image>();
        btnImg.sprite = PopupSpriteFactory.Load("spr_btn_green");
        btnImg.type   = Image.Type.Sliced;

        var btn = btnRT.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.pressedColor   = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
        colors.fadeDuration   = 0.06f;
        btn.colors = colors;

        var btnLabel = NewText("Text_Nut", btnRT, _buttonText, 52, FontStyles.Bold);
        Stretch(btnLabel.rectTransform);
        btnLabel.rectTransform.offsetMin = new Vector2(20f, 8f);
        btnLabel.rectTransform.offsetMax = new Vector2(-20f, -12f);
        btnLabel.color = Color.white;
        AddTextOutline(btnLabel, PopupSpriteFactory.Hex("#1F5A08"), 0.26f);

        // ── 10. PHÁO HOA ─────────────────────────────────────────────────
        var fxAnchor = NewUI("FX_PhaoHoa_Anchor", content);
        Anchor(fxAnchor, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(10f, 10f));
        var fxLeft  = NewUI("FX_Trai_Anchor", content);
        Anchor(fxLeft, new Vector2(0.5f, 0.5f), new Vector2(-430f, 150f), new Vector2(10f, 10f));
        var fxRight = NewUI("FX_Phai_Anchor", content);
        Anchor(fxRight, new Vector2(0.5f, 0.5f), new Vector2(430f, 150f), new Vector2(10f, 10f));

        GameObject confettiPrefab = _addFireworks ? FindVfxPrefab(
            "Confetti_blast_multicolor", "Confetti", "Firework") : null;
        GameObject sidePrefab = _addFireworks ? FindVfxPrefab(
            "Flash_magic", "Flash", "Sparkle", "Star") : null;

        // ── 11. CHỐT THỨ TỰ VẼ (UI vẽ theo thứ tự sibling: dưới = trên cùng) ─
        // Các object đã được tạo đúng thứ tự sẵn, đây chỉ là chốt lại
        // để chắc chắn không bị lệch nếu sau này chèn thêm bước.
        glow.transform.SetSiblingIndex(0);   // quầng sáng — dưới cùng
        behind.SetSiblingIndex(1);           // nhân vật sau băng rôn
        bannerGroup.SetSiblingIndex(2);      // băng rôn
        front.SetSiblingIndex(3);            // nhân vật đè lên băng rôn
        starGroup.SetSiblingIndex(4);        // ngôi sao
        rewardRow.SetSiblingIndex(5);
        strip.SetSiblingIndex(6);
        giftRow.SetSiblingIndex(7);   // hàng quà vẽ TRÊN dải icon, không bị che
        btnRT.SetSiblingIndex(8);
        fxAnchor.SetAsLastSibling();         // pháo hoa trên cùng
        fxLeft.SetAsLastSibling();
        fxRight.SetAsLastSibling();

        // ── 12. NỐI DÂY VÀO SCRIPT LOGIC CÓ SẴN ──────────────────────────
        if (_wireToExisting)
            WireToLevelUpPopupUI(root.gameObject, visibleRoot.gameObject, cg, content, title, lvNum,
                                 goldRow, goldText, gemRow, gemText,
                                 giftRow, btn, confettiPrefab, sidePrefab,
                                 fxAnchor, fxLeft, fxRight,
                                 strip, scrollContent, unlockSlotList);

        // Xong
        Selection.activeGameObject = root.gameObject;
        EditorUtility.SetDirty(root.gameObject);

        // ── TỰ LƯU SCENE ─────────────────────────────────────────────────
        // BẮT BUỘC: chỉ MarkSceneDirty thì popup nằm trong RAM, Unity tải lại
        // scene là mất sạch — người dùng tưởng tool không chạy.
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);

        bool saved = false;
        if (!Application.isPlaying)
        {
            saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"[LevelUpTool] ✔ Đã dựng '{ROOT_NAME}' trong Canvas '{_targetCanvas.name}'.\n" +
                  $"   • {_slotCount} ô mở khoá\n" +
                  $"   • Pháo hoa: {(confettiPrefab != null ? confettiPrefab.name : "KHÔNG tìm thấy prefab — gắn tay sau")}\n" +
                  $"   • Thả art nhân vật vào: Layer_NhanVat_Sau / Layer_NhanVat_Truoc\n" +
                  $"   • Lưu scene: {(saved ? "ĐÃ LƯU ✔" : Application.isPlaying ? "BỎ QUA (đang Play — hãy THOÁT PLAY rồi dựng lại)" : "THẤT BẠI — hãy Ctrl+S ngay")}");

        if (Application.isPlaying)
            EditorUtility.DisplayDialog("Đang ở Play Mode",
                "Bạn đang chạy game nên scene KHÔNG lưu được — popup sẽ mất khi thoát Play.\n\n" +
                "Hãy THOÁT Play Mode rồi bấm DỰNG POPUP lại.", "Đã hiểu");
    }

    // ════════════════════════════════════════════════════════════════════
    // THÀNH PHẦN CON
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Ô trống đánh dấu chỗ đặt nhân vật — có khung gợi ý mờ.</summary>
    private void MakeCharSlot(RectTransform parent, string name, Vector2 pos, float size)
    {
        var slot = NewUI(name, parent);
        Anchor(slot, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));

        // Khung gợi ý — TẮT sẵn để không lộ khi chạy game.
        var hint = NewImage("_KhungGoiY (tắt khi build)", slot, PopupSpriteFactory.Load("spr_white_round"));
        Stretch(hint.rectTransform);
        hint.type  = Image.Type.Sliced;
        hint.color = new Color(1f, 1f, 1f, 0.12f);
        hint.raycastTarget = false;

        var lbl = NewText("_ChuGoiY", hint.rectTransform, "NHÂN VẬT", 26, FontStyles.Bold);
        Stretch(lbl.rectTransform);
        lbl.color = new Color(1f, 1f, 1f, 0.45f);
        lbl.alignment = TextAlignmentOptions.Center;

        // ĐỂ BẬT trong Editor để bạn nhìn thấy chỗ thả art.
        // Component dưới đây tự tắt nó lúc chạy game → không lộ ra với người chơi.
        hint.gameObject.AddComponent<EditorOnlyHint>();
    }

    /// <summary>Ô icon mở khoá: khung tròn + icon + nhãn NEW.
    /// Trả về component UnlockSlotUI để Build() nối dây vào LevelUpPopupUI.</summary>
    private UnlockSlotUI BuildUnlockSlot(RectTransform parent, int index)
    {
        var slot = NewUI($"Slot_MoKhoa_{index + 1:00}", parent);
        slot.sizeDelta = new Vector2(SLOT_SIZE, SLOT_SIZE);
        var le = slot.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth  = SLOT_SIZE;
        le.preferredHeight = SLOT_SIZE;

        // Nền trắng bên trong khung
        var fill = NewImage("Nen_Tron", slot, PopupSpriteFactory.Load("spr_circle_fill"));
        Anchor(fill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
               new Vector2(SLOT_SIZE - 22f, SLOT_SIZE - 22f));
        fill.color = new Color(0.86f, 0.92f, 0.98f, 1f);
        fill.raycastTarget = false;

        // Icon (để trống — bạn gán sau)
        var icon = NewImage("Icon", slot, null);
        Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
               new Vector2(SLOT_SIZE - 46f, SLOT_SIZE - 46f));
        icon.preserveAspect = true;
        icon.enabled = false;               // bật khi có sprite
        icon.raycastTarget = false;

        // Vòng viền kem (vẽ đè lên trên)
        var ring = NewImage("Vong_Vien", slot, PopupSpriteFactory.Load("spr_ring_circle"));
        Stretch(ring.rectTransform);
        ring.raycastTarget = false;

        // Nhãn NEW đỏ, nghiêng, góc dưới-trái
        var tag = NewUI("Nhan_NEW", slot);
        Anchor(tag, new Vector2(0.5f, 0f), new Vector2(-32f, 22f), new Vector2(104f, 46f));
        tag.localRotation = Quaternion.Euler(0f, 0f, 8f);

        var tagBg = NewImage("Nen", tag, PopupSpriteFactory.Load("spr_new_tag"));
        Stretch(tagBg.rectTransform);
        tagBg.type = Image.Type.Sliced;
        tagBg.raycastTarget = false;

        var tagTxt = NewText("Text", tag, "MỚI", 30, FontStyles.Bold);
        Stretch(tagTxt.rectTransform);
        tagTxt.color = Color.white;
        tagTxt.alignment = TextAlignmentOptions.Center;
        AddTextOutline(tagTxt, PopupSpriteFactory.Hex("#8A1008"), 0.22f);

        var ui = slot.gameObject.AddComponent<UnlockSlotUI>();
        ui.EditorBind(icon, ring, tag.gameObject, null);
        return ui;
    }

    /// <summary>
    /// Nạp Sprite THẬT từ 1 file art trong project.
    ///
    /// VÌ SAO KHÔNG GỌI LoadAssetAtPath&lt;Sprite&gt; RỒI THÔI:
    /// hai file icon xu vàng / kim cương đều có <c>spriteMode: 2</c> (Sprite Mode = Multiple),
    /// nên Sprite KHÔNG phải asset chính mà là SUB-ASSET của texture →
    /// <c>LoadAssetAtPath&lt;Sprite&gt;()</c> trả về NULL. Phải quét
    /// <c>LoadAllAssetRepresentationsAtPath()</c>. (Cùng cách DEV-A làm trong
    /// UnlockIconFillTool.LoadSprite().)
    ///
    /// Trả về null kèm LogWarning nêu rõ đường dẫn nếu không tìm được →
    /// bên gọi tự fallback về đĩa tròn tô màu, KHÔNG bao giờ để sprite null.
    /// </summary>
    private static Sprite LoadRealSprite(string assetPath, string what)
    {
        // Trường hợp texture ở Sprite Mode = Single thì cách này đủ.
        var direct = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (direct != null) return direct;

        // Sprite Mode = Multiple → Sprite nằm trong sub-asset.
        var sub = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
                               .OfType<Sprite>()
                               .FirstOrDefault();
        if (sub != null) return sub;

        Debug.LogWarning(
            $"[LevelUpTool] KHÔNG nạp được sprite {what} → dùng tạm đĩa tròn tô màu.\n" +
            $"   Đường dẫn thử: \"{assetPath}\"\n" +
            "   Kiểm tra: (a) file còn tồn tại? (b) tên thư mục xu vàng có ĐÚNG HAI dấu cách " +
            "('Fantasy Wooden GUI  Free')? (c) texture có Sprite nào trong Sprite Editor?");
        return null;
    }

    /// <summary>
    /// Cụm "icon tiền + số" cho dòng phần thưởng.
    /// <paramref name="iconSprite"/> null → tự lùi về đĩa tròn tô <paramref name="fallbackTint"/>.
    /// </summary>
    private RectTransform MakeCurrencyChip(RectTransform parent, string name, string value,
                                           Sprite iconSprite, Color fallbackTint,
                                           out TextMeshProUGUI valueText)
    {
        var row = NewUI(name, parent);
        row.sizeDelta = new Vector2(150f, 64f);

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = 8f;
        h.childForceExpandWidth  = false;
        h.childForceExpandHeight = false;
        h.childControlWidth  = true;
        h.childControlHeight = true;

        // Chiều rộng do LayoutElement của các con quyết định; không dùng
        // ContentSizeFitter vì row này là con của một LayoutGroup khác.
        var rowLE = row.gameObject.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 64f;

        // Có sprite thật → dùng nguyên bản, KHÔNG tint (tint sẽ nhuộm bẩn xu vàng).
        // Không có → lùi về đĩa tròn tô màu như trước, để icon không bao giờ null.
        bool hasReal = iconSprite != null;
        var icon = NewImage("Icon", row,
                            hasReal ? iconSprite : PopupSpriteFactory.Load("spr_circle_fill"));
        icon.color          = hasReal ? Color.white : fallbackTint;
        icon.preserveAspect = true;   // xu/kim cương không vuông tuyệt đối → tránh bóp méo
        icon.raycastTarget  = false;
        var iconLE = icon.gameObject.AddComponent<LayoutElement>();
        iconLE.preferredWidth = 52f; iconLE.preferredHeight = 52f;

        valueText = NewText("Text_SoLuong", row, value, 46, FontStyles.Bold);
        valueText.color = Color.white;
        AddTextOutline(valueText, new Color(0.15f, 0.08f, 0f, 1f), 0.25f);

        return row;
    }

    // ════════════════════════════════════════════════════════════════════
    // NỐI DÂY VÀO LevelUpPopupUI CÓ SẴN
    // ════════════════════════════════════════════════════════════════════

    private void WireToLevelUpPopupUI(
        GameObject holder, GameObject visibleRoot, CanvasGroup cg, RectTransform content,
        TextMeshProUGUI title, TextMeshProUGUI levelNum,
        RectTransform goldRow, TextMeshProUGUI goldText,
        RectTransform gemRow,  TextMeshProUGUI gemText,
        RectTransform giftContainer, Button claimBtn,
        GameObject confettiPrefab, GameObject sidePrefab,
        RectTransform fxTop, RectTransform fxLeft, RectTransform fxRight,
        RectTransform unlockStrip, RectTransform unlockSlotsContainer,
        List<UnlockSlotUI> unlockSlots)
    {
        // Script PHẢI nằm trên root MỚI.
        // Nếu để nguyên script trên popup CŨ rồi chỉ trỏ popupRoot sang root mới thì
        // Start() sẽ tắt root mới, còn popup CŨ không ai tắt → hiện đè lên màn hình
        // ngay từ lúc vào game.
        // Script gắn trên HOLDER (luôn bật), popupRoot trỏ vào Root_HienThi.
        var ui = holder.GetComponent<LevelUpPopupUI>();
        if (ui == null)
        {
            var old = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
            ui = holder.AddComponent<LevelUpPopupUI>();

            if (old != null && old != ui)
            {
                var oldGo = old.gameObject;

                // Tắt trước — đây mới là việc bắt buộc (chống 2 popup hiện song song).
                oldGo.SetActive(false);

                // Xoá component cũ. Nếu nó nằm trên prefab instance thì Unity CẤM
                // xoá rời component → bắt exception, giữ nguyên, chỉ cảnh báo.
                bool removed = true;
                try   { Object.DestroyImmediate(old); }
                catch (System.Exception e)
                {
                    removed = false;
                    Debug.LogWarning($"[LevelUpTool] Không xoá được LevelUpPopupUI cũ " +
                                     $"(có thể nằm trên prefab instance): {e.Message}");
                }

                Debug.LogWarning($"[LevelUpTool] Đã chuyển LevelUpPopupUI sang '{holder.name}'. " +
                                 $"Popup cũ '{oldGo.name}' đã TẮT" +
                                 (removed ? " và gỡ component." : " (component cũ vẫn còn — hãy xoá tay).") +
                                 " Kiểm tra lại popup cũ, xoá hẳn nếu không dùng nữa.");
            }
        }

        var so = new SerializedObject(ui);

        // ── NẠP LevelRewardConfig ────────────────────────────────────────
        // CopyFromSerializedProperty từng thất bại âm thầm với mảng object reference
        // (scene ghi ra `levelRewardConfigs: []`) → popup hiện mà không có vàng/ngọc.
        // Giờ quét thẳng project và gán từng phần tử, sắp theo levelReached.
        var cfgProp = so.FindProperty("levelRewardConfigs");
        if (cfgProp != null && cfgProp.arraySize == 0)
        {
            var guids = AssetDatabase.FindAssets("t:LevelRewardConfig");
            var list  = new System.Collections.Generic.List<LevelRewardConfig>();
            foreach (var g in guids)
            {
                var a = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(
                            AssetDatabase.GUIDToAssetPath(g));
                if (a != null) list.Add(a);
            }
            list.Sort((x, y) => x.levelReached.CompareTo(y.levelReached));

            cfgProp.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                cfgProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];

            Debug.Log($"[LevelUpTool] Đã nạp {list.Count} LevelRewardConfig từ project.");
        }

        void Set(string prop, Object val)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = val;
            else Debug.LogWarning($"[LevelUpTool] Không tìm thấy field '{prop}' trong LevelUpPopupUI — bỏ qua.");
        }

        Set("popupRoot",          visibleRoot);   // KHÔNG phải holder!
        Set("canvasGroup",        cg);
        Set("contentPanel",       content);
        Set("titleText",          title);
        Set("levelNumberText",    levelNum);
        Set("goldRewardRow",      goldRow.gameObject);
        Set("goldRewardText",     goldText);
        Set("gemRewardRow",       gemRow.gameObject);
        Set("gemRewardText",      gemText);
        Set("giftItemsContainer", giftContainer);
        Set("claimButton",        claimBtn);
        Set("vfxSpawnPoint",      fxTop);
        Set("vfxLeftPoint",       fxLeft);
        Set("vfxRightPoint",      fxRight);

        if (confettiPrefab != null) Set("vfxConfettiPrefab", confettiPrefab);
        if (sidePrefab     != null) Set("vfxSidePrefab",     sidePrefab);

        // ── Ô MỞ KHOÁ ────────────────────────────────────────────────────
        // Đây là mắt xích trước đây BỊ THIẾU: tool sinh ra 9 UnlockSlotUI nhưng
        // không gán vào script, nên PopulateUI() không biết ô nào để nạp icon
        // → cả 9 ô hiện ra khung tròn TRẮNG TRƠN chỉ còn nhãn NEW.
        //
        // Gán CẢ HAI đường:
        //   • unlockSlotsContainer → script dò UnlockSlotUI bên trong (ưu tiên 1,
        //     bền với việc thêm/bớt ô trong Hierarchy)
        //   • unlockSlots[]        → dự phòng nếu container bị xoá/đổi cha
        Set("unlockSlotsContainer", unlockSlotsContainer);
        Set("unlockStripRoot",      unlockStrip != null ? unlockStrip.gameObject : null);

        int wiredSlots = 0;
        var slotsProp = so.FindProperty("unlockSlots");
        if (slotsProp != null && unlockSlots != null)
        {
            slotsProp.arraySize = unlockSlots.Count;
            for (int i = 0; i < unlockSlots.Count; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = unlockSlots[i];
            wiredSlots = unlockSlots.Count;
        }
        else if (slotsProp == null)
        {
            Debug.LogWarning("[LevelUpTool] Không tìm thấy field 'unlockSlots' trong LevelUpPopupUI " +
                             "— icon mở khoá sẽ phụ thuộc hoàn toàn vào 'unlockSlotsContainer'.");
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        Debug.Log($"[LevelUpTool] Đã nối dây vào LevelUpPopupUI trên '{ui.gameObject.name}'. " +
                  $"Ô mở khoá đã nối: {wiredSlots} " +
                  $"(container = '{(unlockSlotsContainer != null ? unlockSlotsContainer.name : "NULL")}').");
    }

    /// <summary>Tìm prefab VFX theo danh sách từ khoá, ưu tiên từ khoá đứng trước.</summary>
    private static GameObject FindVfxPrefab(params string[] keywords)
    {
        foreach (var kw in keywords)
        {
            var guids = AssetDatabase.FindAssets($"{kw} t:Prefab");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                if (go.GetComponentInChildren<ParticleSystem>(true) != null)
                    return go;
            }
        }
        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    // CHẨN ĐOÁN & TEST
    // ════════════════════════════════════════════════════════════════════

    /// <summary>In ra Console tình trạng thật của popup — tìm nhanh lý do không hiện.</summary>
    private void Diagnose()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("═══ CHẨN ĐOÁN POPUP LÊN CẤP ═══");

        // 1. Popup có trong scene không?
        GameObject found = null;
        foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
            if (t != null && t.name == ROOT_NAME) { found = t.gameObject; break; }

        sb.AppendLine(found != null
            ? $"✔ Tìm thấy '{ROOT_NAME}' trong scene."
            : $"✘ KHÔNG có '{ROOT_NAME}' trong scene → bấm DỰNG POPUP.");

        // 2. Scene đã lưu chưa?
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        sb.AppendLine(scene.isDirty
            ? "✘ SCENE CHƯA LƯU — nhấn Ctrl+S ngay, không thì popup sẽ mất!"
            : "✔ Scene đã lưu.");

        // 3. Script ở đâu, popupRoot trỏ vào đâu?
        var uis = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include,
                                                           FindObjectsSortMode.None);
        sb.AppendLine($"• Số LevelUpPopupUI trong scene: {uis.Length}" +
                      (uis.Length > 1 ? "  ✘ NHIỀU HƠN 1 → sẽ xung đột!" : ""));

        foreach (var ui in uis)
        {
            var so   = new SerializedObject(ui);
            var pRoot = so.FindProperty("popupRoot")?.objectReferenceValue as GameObject;
            var cfgs  = so.FindProperty("levelRewardConfigs");

            sb.AppendLine($"  ├ Script nằm trên : '{ui.gameObject.name}' " +
                          $"(đang {(ui.gameObject.activeInHierarchy ? "BẬT ✔" : "TẮT ✘ → Start() sẽ KHÔNG chạy!")})");
            sb.AppendLine($"  ├ popupRoot       : {(pRoot != null ? $"'{pRoot.name}'" : "NULL ✘")}");

            if (pRoot != null && pRoot == ui.gameObject)
                sb.AppendLine("  │   ✘ popupRoot TRỎ VÀO CHÍNH MÌNH → script tự tắt chính nó. Dựng lại bằng tool.");

            // ── Canvas cha: đây chính là chỗ lỗi lần trước bị bỏ sót ──
            var cv = ui.GetComponentInParent<Canvas>(true);
            sb.AppendLine($"  ├ Canvas cha      : " +
                (cv != null ? $"'{cv.name}' (renderMode={cv.renderMode}, order={cv.sortingOrder})"
                            : "KHÔNG CÓ ✘"));

            if (cv != null && cv.rootCanvas.renderMode == RenderMode.WorldSpace)
                sb.AppendLine("  │   ✘✘ CANVAS GỐC LÀ WORLD SPACE → popup nằm trong world game, " +
                              "KHÔNG BAO GIỜ hiện trên màn hình! Dựng lại vào Canvas_Popup.");

            if (cv != null && IsInsideBuildingPrefab(cv.transform))
                sb.AppendLine("  │   ✘✘ Canvas nằm TRONG PREFAB CÔNG TRÌNH (nhà/bong bóng đơn) → " +
                              "sẽ bị script công trình tắt lúc chạy!");

            sb.AppendLine($"  ├ Đường dẫn       : {GetPath(ui.transform)}");

            var ls = ui.transform.lossyScale;
            sb.AppendLine($"  ├ lossyScale      : ({ls.x:F3}, {ls.y:F3}, {ls.z:F3})" +
                (Mathf.Abs(ls.x - 1f) > 0.05f ? "  ✘ PHẢI ≈ 1 — popup đang bị co/giãn!" : "  ✔"));

            if (!Application.isPlaying)
                sb.AppendLine("  │   ⚠ Đang Edit Mode — trạng thái BẬT/TẮT chưa phản ánh Awake() lúc chạy.");

            sb.AppendLine($"  ├ Số LevelRewardConfig: {(cfgs != null ? cfgs.arraySize : 0)}" +
                          (cfgs == null || cfgs.arraySize == 0 ? "  ✘ RỖNG → không có phần thưởng nào" : ""));
            sb.AppendLine($"  └ claimButton     : " +
                          $"{(so.FindProperty("claimButton")?.objectReferenceValue != null ? "✔" : "NULL ✘ → không đóng được popup")}");
        }

        // 4. PlayerProgressManager
        var ppm = Object.FindFirstObjectByType<PlayerProgressManager>(FindObjectsInactive.Include);
        sb.AppendLine(ppm != null
            ? $"✔ PlayerProgressManager có trong scene (đang {(ppm.gameObject.activeInHierarchy ? "BẬT" : "TẮT ✘")})."
            : "✘ KHÔNG có PlayerProgressManager → sự kiện lên cấp không bao giờ phát.");

        // 5. Sprite
        int missing = 0;
        foreach (var n in new[] { "spr_star", "spr_banner_body", "spr_btn_green",
                                  "spr_ring_circle", "spr_new_tag", "spr_glow_radial" })
            if (PopupSpriteFactory.Load(n) == null) missing++;
        sb.AppendLine(missing == 0
            ? "✔ Sprite đầy đủ."
            : $"✘ Thiếu {missing} sprite → bấm 'Chỉ sinh lại Sprite'.");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("Chẩn đoán xong", "Kết quả đã in ra Console (Window ▸ General ▸ Console).", "OK");
    }

    /// <summary>Đường dẫn hierarchy đầy đủ — để biết object đang nằm ở đâu.</summary>
    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + " / " + p; }
        return p;
    }

    /// <summary>Bật popup ngay trong Play Mode để xem, không cần lên cấp thật.</summary>
    private void TestShowPopup()
    {
        var ui = Object.FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            Debug.LogError("[LevelUpTool] Không tìm thấy LevelUpPopupUI trong scene.");
            return;
        }

        // Gọi qua reflection vì HandleLevelChanged là private
        var m = typeof(LevelUpPopupUI).GetMethod("HandleLevelChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var f = typeof(LevelUpPopupUI).GetField("_lastKnownLevel",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (m == null || f == null)
        {
            Debug.LogError("[LevelUpTool] Không gọi được HandleLevelChanged (script đã đổi?).");
            return;
        }

        // Reset cờ kẹt từ lần test trước. Nếu không: HandleLevelChanged thấy
        // _isShowing == true sẽ chỉ Enqueue rồi IM RE — đúng triệu chứng đã gặp.
        typeof(LevelUpPopupUI)
            .GetField("_isShowing", System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance)
            ?.SetValue(ui, false);

        if (!ui.gameObject.activeInHierarchy)
        {
            Debug.LogError($"[LevelUpTool] ✘ '{ui.gameObject.name}' đang TẮT trong Hierarchy " +
                           $"(cha: '{(ui.transform.parent != null ? ui.transform.parent.name : "—")}').\n" +
                           $"   Đường dẫn: {GetPath(ui.transform)}\n" +
                           "   → Popup KHÔNG thể hiện, coroutine không chạy được. " +
                           "Dựng lại vào Canvas_Popup bằng nút DỰNG POPUP.");
            return;
        }

        f.SetValue(ui, _previewLevel - 1);
        m.Invoke(ui, new object[] { _previewLevel });
        Debug.Log($"[LevelUpTool] Đã kích hoạt popup cấp {_previewLevel}.");
    }

    private void DeleteExisting()
    {
        int n = 0;
        foreach (var t in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                                                                  FindObjectsSortMode.None))
        {
            if (t != null && t.name == ROOT_NAME) { DestroyImmediate(t.gameObject); n++; }
        }
        if (n > 0) Debug.Log($"[LevelUpTool] Đã xoá {n} popup cũ.");
    }

    // ════════════════════════════════════════════════════════════════════
    // TIỆN ÍCH UI
    // ════════════════════════════════════════════════════════════════════

    private static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var rt  = NewUI(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        if (sprite == null) img.color = Color.white;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text,
                                           float size, FontStyles style)
    {
        var rt  = NewUI(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.fontStyle     = style;
        tmp.alignment     = TextAlignmentOptions.Center;
        // enableWordWrapping đã Obsolete trong TMP mới
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        tmp.overflowMode  = TextOverflowModes.Overflow;
        return tmp;
    }

    /// <summary>
    /// Viền chữ. KHÔNG tự `new Material(...)` — getter `fontMaterial` của TMP
    /// đã tự tạo instance riêng rồi; tự tạo thêm sẽ sinh 2 material mồ côi mỗi text.
    /// Lưu ý: setter outlineColor bỏ qua nếu truyền đúng Color.black (giá trị mặc định).
    /// </summary>
    private static void AddTextOutline(TextMeshProUGUI tmp, Color color, float width)
    {
        var mat = tmp.fontMaterial;   // TMP tự tạo instance
        mat.EnableKeyword(TMPro.ShaderUtilities.Keyword_Outline);
        tmp.outlineColor = color;
        tmp.outlineWidth = width;
        tmp.UpdateMeshPadding();      // nới padding kẻo viền bị cắt cụt
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    /// <summary>Neo giãn ngang toàn chiều rộng, cố định chiều cao.</summary>
    private static void Anchor2(RectTransform rt, float xMin, float xMax, Vector2 pos, float height)
    {
        rt.anchorMin        = new Vector2(xMin, 0.5f);
        rt.anchorMax        = new Vector2(xMax, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(0f, height);
        rt.anchoredPosition = pos;
    }

}
