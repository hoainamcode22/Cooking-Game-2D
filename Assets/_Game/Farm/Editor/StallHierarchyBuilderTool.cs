#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TOOL DỰNG QUẦY HÀNG (T2) — Menu: <c>Tools ▸ Farm ▸ Quầy Hàng</c>
///
/// VÌ SAO PHẢI LÀ EDITOR TOOL chứ không dựng bằng code lúc chạy: dự án đã có
/// `UnifiedTaskPopupUI` 1433 dòng dựng UI bằng `new GameObject()` với ~200 toạ độ
/// hardcode — không ai sửa nổi, muốn nhích một cái nút cũng phải đọc code. Tool này
/// chạy MỘT LẦN rồi sinh ra hierarchy + prefab thật; từ đó về sau mọi chỉnh sửa đều
/// làm bằng chuột trong Editor, và code runtime không tạo GameObject nào.
///
/// Tool sinh ra:
///   • `StallSystem`         — PlayerStallManager + StallItemCatalog (scene)
///   • `Canvas_StallPopup`   — popup quầy hàng, nối với prefab để mở Prefab Mode sửa được
///   • `Stall_WorldObject`   — quầy ngoài map, có collider + chỗ bày hàng lên mặt quầy
///   • 2 prefab ô lặp lại    — `PF_StallSlot`, `PF_StallPickCell`
///
/// MỌI KHỐI CHỜ ART đều là `Image`/`SpriteRenderer` MÀU PHẲNG và có tên bắt đầu bằng
/// `IMG_Art...` — chủ dự án tìm theo tiền tố đó là thấy hết chỗ cần thay art.
/// </summary>
public class StallHierarchyBuilderTool : EditorWindow
{
    private const string PrefabFolder     = "Assets/_Game/Prefab/ui/Stall";
    private const string CanvasName       = "Canvas_StallPopup";
    private const string SystemName       = "StallSystem";
    private const string WorldObjectName  = "Stall_WorldObject";

    // Kích thước tham chiếu — khớp `Canvas_MarketPopup` đang có trong SCN_Farm.
    private const float REF_W = 1920f;
    private const float REF_H = 1080f;

    private const float POPUP_W = 1500f;
    private const float POPUP_H = 860f;

    private bool _regenSprites;

    [MenuItem("Tools/Farm/Quầy Hàng", false, 22)]
    public static void Open()
    {
        StallHierarchyBuilderTool w = GetWindow<StallHierarchyBuilderTool>(true, "Quầy Hàng — Dựng giao diện");
        w.minSize = new Vector2(430, 340);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("QUẦY HÀNG (T2)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Dựng nền CÓ MÀU cho quầy hàng: popup 4 trạng thái ô, panel chọn vật phẩm " +
            "trượt đè, bộ chỉnh số lượng/giá, nút gạt loa, và object quầy ngoài map.\n\n" +
            "Chạy trên scene ĐANG MỞ (thường là SCN_Farm). Mọi chỗ chờ art tên là IMG_Art...",
            MessageType.Info);

        EditorGUILayout.Space();
        _regenSprites = EditorGUILayout.ToggleLeft("Vẽ lại toàn bộ sprite (ghi đè file cũ)", _regenSprites);

        EditorGUILayout.Space();

        if (GUILayout.Button("1 · Sinh sprite quầy hàng", GUILayout.Height(28)))
            StallSpriteFactory.GenerateAll(_regenSprites);

        if (GUILayout.Button("2 · Dựng TẤT CẢ (sprite + hệ thống + popup + quầy ngoài map)", GUILayout.Height(40)))
            BuildEverything(_regenSprites);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Từng phần", EditorStyles.boldLabel);

        if (GUILayout.Button("Chỉ dựng hệ thống (StallSystem)"))     BuildSystem();
        if (GUILayout.Button("Chỉ dựng popup (Canvas_StallPopup)"))  BuildPopup();
        if (GUILayout.Button("Chỉ dựng quầy ngoài map"))             BuildWorldObject();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ĐIỀU PHỐI
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildEverything(bool regenSprites)
    {
        StallSpriteFactory.GenerateAll(regenSprites);
        EnsurePrefabFolder();

        BuildSystem();
        BuildPopup();
        BuildWorldObject();

        MarkSceneDirty();
        Debug.Log("[QuầyHàng] Dựng xong. Kiểm tra 3 object gốc trong scene: " +
                  $"{SystemName} · {CanvasName} · {WorldObjectName}");
    }

    private static void EnsurePrefabFolder()
    {
        string abs = Path.Combine(Directory.GetCurrentDirectory(), PrefabFolder);
        if (!Directory.Exists(abs))
        {
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }
    }

    // Gọi thẳng SceneManager của runtime chứ không qua EditorSceneManager: EditorSceneManager
    // kế thừa SceneManager nên `EditorSceneManager.GetActiveScene()` biên dịch được, nhưng
    // viết vậy khiến người đọc tưởng đó là API riêng của Editor. Ghi rõ nguồn cho khỏi nhầm.
    private static void MarkSceneDirty()
        => EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

    // ─────────────────────────────────────────────────────────────────────────
    //  1 · HỆ THỐNG
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildSystem()
    {
        GameObject go = GameObject.Find(SystemName);
        if (go == null)
        {
            go = new GameObject(SystemName);
            Undo.RegisterCreatedObjectUndo(go, "Tạo StallSystem");
        }

        PlayerStallManager manager = go.GetComponent<PlayerStallManager>();
        if (manager == null) manager = Undo.AddComponent<PlayerStallManager>(go);

        StallItemCatalog catalog = go.GetComponent<StallItemCatalog>();
        if (catalog == null) catalog = Undo.AddComponent<StallItemCatalog>(go);

        // Quét asset ngay trong Editor thay vì Resources.LoadAll lúc chạy: Resources nhồi
        // MỌI asset vào build kể cả thứ không dùng, còn danh sách gán sẵn thì Unity chỉ
        // đóng gói đúng những asset được tham chiếu.
        var crops = new List<CropData>();
        foreach (string guid in AssetDatabase.FindAssets("t:CropData"))
        {
            CropData c = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null) crops.Add(c);
        }

        var items = new List<InventoryItemData>();
        foreach (string guid in AssetDatabase.FindAssets("t:InventoryItemData"))
        {
            InventoryItemData it =
                AssetDatabase.LoadAssetAtPath<InventoryItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (it != null) items.Add(it);
        }

        catalog.EditorSetDatabases(crops, items);
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(manager);

        Debug.Log($"[QuầyHàng] StallSystem: nạp {crops.Count} CropData + {items.Count} InventoryItemData.");
        MarkSceneDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  2 · POPUP
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildPopup()
    {
        StallSpriteFactory.GenerateAll(false);   // bỏ qua file đã có, chỉ bù thứ còn thiếu
        EnsurePrefabFolder();

        // Xoá bản cũ để chạy lại tool nhiều lần không sinh ra hai popup chồng nhau.
        GameObject old = GameObject.Find(CanvasName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        StallSlotUI         slotPrefab = BuildSlotPrefab();
        StallPickItemCellUI cellPrefab = BuildPickCellPrefab();

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGo = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGo, "Tạo Canvas_StallPopup");

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;   // trên HUD, dưới popup hệ thống

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.matchWidthOrHeight  = 0.5f;

        StallPopupUI popup = canvasGo.AddComponent<StallPopupUI>();

        // ── Nền mờ (chính là popupRoot) ──────────────────────────────────────
        RectTransform dim = CreateUI("Panel_Dim", canvasGo.transform);
        Stretch(dim, 0, 0, 0, 0);
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.62f);
        Button dimBtn = dim.gameObject.AddComponent<Button>();
        dimBtn.targetGraphic = dimImg;
        dimBtn.transition    = Selectable.Transition.None;

        // ── Thân popup ───────────────────────────────────────────────────────
        RectTransform main = CreateUI("Popup_Main", dim);
        Center(main, Vector2.zero, new Vector2(POPUP_W, POPUP_H));

        RectTransform bg = CreateUI("IMG_ArtPanelBackground", main);
        Stretch(bg, 0, 0, 0, 0);
        Sliced(bg, "stall_panel", Color.white);

        // Mái hiên răng sò vắt ngang đỉnh — thay cho mái sọc xanh-trắng của video.
        RectTransform valance = CreateUI("IMG_ArtValance", main);
        TopStretch(valance, 64f, 10f);
        Image valImg = valance.gameObject.AddComponent<Image>();
        valImg.sprite = StallSpriteFactory.Load("stall_valance");
        valImg.type   = Image.Type.Tiled;
        valImg.color  = Color.white;
        valImg.raycastTarget = false;

        // Biển tên đè lên mái
        RectTransform pill = CreateUI("TitlePill", main);
        Anchor(pill, new Vector2(0.5f, 1f), new Vector2(0f, 18f), new Vector2(420f, 84f));
        Sliced(pill, "stall_pill", Color.white);
        TextMeshProUGUI title = AddText(pill, "Text_Title", "QUẦY HÀNG", 40, StallSpriteFactory.Gold,
                                        TextAlignmentOptions.Center);
        Stretch(title.rectTransform, 30, 6, 30, 6);
        title.fontStyle = FontStyles.Bold;

        // Nút X LỒI RA NGOÀI mép panel (theo video) — nằm ngoài nên không ăn mất chỗ bên trong.
        RectTransform close = CreateUI("BtnClose", main);
        Anchor(close, new Vector2(1f, 1f), new Vector2(26f, 26f), new Vector2(84f, 84f));
        Image closeImg = close.gameObject.AddComponent<Image>();
        closeImg.sprite = StallSpriteFactory.Load("stall_circle");
        closeImg.color  = StallSpriteFactory.Hex("#E4574C");
        Button closeBtn = close.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        TextMeshProUGUI closeTxt = AddText(close, "Text_X", "X", 40, Color.white, TextAlignmentOptions.Center);
        Stretch(closeTxt.rectTransform, 0, 0, 0, 4);
        closeTxt.fontStyle = FontStyles.Bold;

        // Ví vàng
        RectTransform goldBar = CreateUI("GoldBar", main);
        // y = -96: nằm DƯỚI dải mái hiên (mái cao 64, cách đỉnh 10). Đặt cao hơn là ví vàng
        // bị mái đè lên và người chơi không đọc được số dư.
        Anchor(goldBar, new Vector2(1f, 1f), new Vector2(-140f, -96f), new Vector2(220f, 62f));
        Sliced(goldBar, "stall_btn", StallSpriteFactory.Hex("#2A1A3C"));
        RectTransform goldIcon = CreateUI("IMG_ArtGoldIcon", goldBar);
        Anchor(goldIcon, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));
        Simple(goldIcon, "stall_icon_coin", Color.white);
        TextMeshProUGUI goldTxt = AddText(goldBar, "Text_Gold", "0", 32, StallSpriteFactory.Cream,
                                          TextAlignmentOptions.MidlineLeft);
        Stretch(goldTxt.rectTransform, 68, 4, 14, 4);

        // ── Lưới ô quầy 5×2 ──────────────────────────────────────────────────
        RectTransform slotGrid = CreateUI("SlotGrid", main);
        Center(slotGrid, new Vector2(0f, 24f), new Vector2(1330f, 490f));
        GridLayoutGroup grid = slotGrid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(250f, 230f);
        grid.spacing         = new Vector2(20f, 20f);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment  = TextAnchor.UpperCenter;

        // ── Hồ sơ người chơi (góc dưới trái, theo video) ─────────────────────
        RectTransform profile = CreateUI("ProfileBar", main);
        Anchor(profile, new Vector2(0f, 0f), new Vector2(210f, 60f), new Vector2(360f, 88f));
        Sliced(profile, "stall_btn", StallSpriteFactory.Hex("#2A1A3C"));

        RectTransform avatar = CreateUI("IMG_ArtPlayerAvatar", profile);
        Anchor(avatar, new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(68f, 68f));
        Simple(avatar, "stall_circle", StallSpriteFactory.Teal);

        TextMeshProUGUI pName = AddText(profile, "Text_PlayerName", "Người chơi", 28,
                                        StallSpriteFactory.Cream, TextAlignmentOptions.MidlineLeft);
        Stretch(pName.rectTransform, 96, 6, 80, 6);

        RectTransform lvBadge = CreateUI("Badge_Level", profile);
        Anchor(lvBadge, new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(58f, 58f));
        Simple(lvBadge, "stall_circle", StallSpriteFactory.Gold);
        TextMeshProUGUI pLevel = AddText(lvBadge, "Text_PlayerLevel", "1", 26,
                                         StallSpriteFactory.Hex("#2A1A3C"), TextAlignmentOptions.Center);
        Stretch(pLevel.rectTransform, 0, 0, 0, 0);
        pLevel.fontStyle = FontStyles.Bold;

        // ── Thông báo ────────────────────────────────────────────────────────
        RectTransform toast = CreateUI("Message_Toast", main);
        Anchor(toast, new Vector2(0.5f, 0f), new Vector2(0f, 168f), new Vector2(760f, 68f));
        Sliced(toast, "stall_btn", StallSpriteFactory.Hex("#20122F"));
        TextMeshProUGUI toastTxt = AddText(toast, "Text_Message", "", 28, StallSpriteFactory.Cream,
                                           TextAlignmentOptions.Center);
        Stretch(toastTxt.rectTransform, 16, 4, 16, 4);
        toast.gameObject.SetActive(false);

        // ── Panel chọn vật phẩm (trượt đè) ───────────────────────────────────
        PickerRefs picker = BuildPicker(main, cellPrefab);

        // ── Nối dây ──────────────────────────────────────────────────────────
        new Wiring(popup)
            .Obj("popupRoot",           dim.gameObject)
            .Obj("buttonClose",         closeBtn)
            .Obj("buttonDimBackground", dimBtn)
            .Obj("textTitle",           title)
            .Obj("textGold",            goldTxt)
            .Obj("slotGridContent",     slotGrid)
            .Obj("slotPrefab",          slotPrefab)
            .Obj("textPlayerName",      pName)
            .Obj("textPlayerLevel",     pLevel)
            .Obj("pickerRoot",          picker.Root.gameObject)
            .Obj("pickerPanel",         picker.Panel)
            .Obj("buttonPickerBack",    picker.BackButton)
            .Num("pickerShownX",        0f)
            .Num("pickerHiddenX",       POPUP_W + 200f)
            .Num("pickerSlideSeconds",  0.22f)
            .ObjList("categoryTabs",    picker.Tabs)
            .Obj("pickGridContent",     picker.GridContent)
            .Obj("pickCellPrefab",      cellPrefab)
            .Obj("pickEmptyHint",       picker.EmptyHint)
            .Obj("textPickEmptyHint",   picker.EmptyHintText)
            .Obj("setupEmptyHint",      picker.SetupEmptyHint)
            .Obj("setupContentRoot",    picker.SetupContent)
            .Obj("imageSelectedIcon",   picker.SelectedIcon)
            .Obj("textSelectedName",    picker.SelectedName)
            .Obj("buttonQuantityMinus", picker.QtyMinus)
            .Obj("buttonQuantityPlus",  picker.QtyPlus)
            .Obj("textQuantity",        picker.QtyText)
            .Obj("buttonPriceMinus",    picker.PriceMinus)
            .Obj("buttonPricePlus",     picker.PricePlus)
            .Obj("textPrice",           picker.PriceText)
            .Obj("textPriceHint",       picker.PriceHint)
            .Obj("buttonLoaToggle",     picker.LoaButton)
            .Obj("textLoaLabel",        picker.LoaLabel)
            .Obj("textLoaCost",         picker.LoaCost)
            .Obj("loaKnob",             picker.LoaKnob)
            .Obj("imageLoaTrack",       picker.LoaTrack)
            .Obj("buttonConfirm",       picker.ConfirmButton)
            .Obj("textConfirmLabel",    picker.ConfirmLabel)
            .Obj("messageRoot",         toast.gameObject)
            .Obj("textMessage",         toastTxt)
            .Apply();

        dim.gameObject.SetActive(false);

        // Nối với prefab để chủ dự án mở Prefab Mode sửa được — yêu cầu ở mục 8 file TEAM
        // ("cả hai popup dựng bằng prefab, sửa được trong Editor").
        string canvasPrefabPath = $"{PrefabFolder}/{CanvasName}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(canvasGo, canvasPrefabPath, InteractionMode.AutomatedAction);

        Debug.Log($"[QuầyHàng] Đã dựng popup + lưu prefab: {canvasPrefabPath}");
        MarkSceneDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PANEL CHỌN VẬT PHẨM
    // ─────────────────────────────────────────────────────────────────────────

    private class PickerRefs
    {
        public RectTransform Root, Panel, GridContent, LoaKnob;
        public Button   BackButton, QtyMinus, QtyPlus, PriceMinus, PricePlus, LoaButton, ConfirmButton;
        public GameObject EmptyHint, SetupEmptyHint, SetupContent;
        public TextMeshProUGUI EmptyHintText, SelectedName, QtyText, PriceText, PriceHint,
                               LoaLabel, LoaCost, ConfirmLabel;
        public Image SelectedIcon, LoaTrack;
        public List<Object> Tabs = new List<Object>();
    }

    private static PickerRefs BuildPicker(RectTransform main, StallPickItemCellUI cellPrefab)
    {
        var r = new PickerRefs();

        r.Root = CreateUI("Picker_Root", main);
        Stretch(r.Root, 0, 0, 0, 0);

        r.Panel = CreateUI("Picker_Panel", r.Root);
        Center(r.Panel, Vector2.zero, new Vector2(POPUP_W, POPUP_H));

        RectTransform pbg = CreateUI("IMG_ArtPickerBackground", r.Panel);
        Stretch(pbg, 0, 0, 0, 0);
        Sliced(pbg, "stall_panel", StallSpriteFactory.Hex("#4A3268"));

        // Nút quay lại
        RectTransform back = CreateUI("Btn_Back", r.Panel);
        Anchor(back, new Vector2(0f, 1f), new Vector2(96f, -46f), new Vector2(150f, 60f));
        Sliced(back, "stall_btn", StallSpriteFactory.Hex("#7A5C9C"));
        r.BackButton = back.gameObject.AddComponent<Button>();
        r.BackButton.targetGraphic = back.GetComponent<Image>();
        TextMeshProUGUI backTxt = AddText(back, "Text_Back", "Quay lại", 26, StallSpriteFactory.Cream,
                                          TextAlignmentOptions.Center);
        Stretch(backTxt.rectTransform, 6, 4, 6, 4);

        // ── Cột trái: tab danh mục ───────────────────────────────────────────
        RectTransform colTabs = CreateUI("Col_Categories", r.Panel);
        Anchor(colTabs, new Vector2(0f, 0.5f), new Vector2(120f, -40f), new Vector2(190f, 620f));
        VerticalLayoutGroup vlg = colTabs.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        AddCategoryTab(colTabs, StallItemCategory.TatCa,   "Tất cả",  r.Tabs);
        AddCategoryTab(colTabs, StallItemCategory.NongSan, "Nông sản", r.Tabs);
        AddCategoryTab(colTabs, StallItemCategory.Hoa,     "Hoa",      r.Tabs);
        AddCategoryTab(colTabs, StallItemCategory.HatGiong,"Hạt giống",r.Tabs);
        AddCategoryTab(colTabs, StallItemCategory.CheBien, "Chế biến", r.Tabs);

        // ── Cột giữa: lưới vật phẩm ──────────────────────────────────────────
        RectTransform colItems = CreateUI("Col_Items", r.Panel);
        Anchor(colItems, new Vector2(0f, 0.5f), new Vector2(620f, -40f), new Vector2(770f, 640f));
        Sliced(colItems, "stall_slot", StallSpriteFactory.Hex("#2E1D42"));

        RectTransform scroll = CreateUI("Scroll_View", colItems);
        Stretch(scroll, 14, 14, 14, 14);
        ScrollRect sr = scroll.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        RectTransform viewport = CreateUI("Viewport", scroll);
        Stretch(viewport, 0, 0, 0, 0);
        Image vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.02f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        r.GridContent = CreateUI("Content", viewport);
        TopStretchContent(r.GridContent);
        GridLayoutGroup g = r.GridContent.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize        = new Vector2(170f, 190f);
        g.spacing         = new Vector2(16f, 16f);
        g.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 4;
        g.childAlignment  = TextAnchor.UpperLeft;
        g.padding         = new RectOffset(8, 8, 8, 8);

        // ContentSizeFitter: thiếu nó thì Content không bao giờ cao lên và ScrollRect
        // tưởng nội dung vừa khít khung ⇒ không cuộn được. Chợ hiện tại đang thiếu
        // đúng thứ này (mục 2 file TEAM).
        ContentSizeFitter fitter = r.GridContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = viewport;
        sr.content  = r.GridContent;

        RectTransform emptyHint = CreateUI("Empty_Hint", colItems);
        Stretch(emptyHint, 20, 20, 20, 20);
        r.EmptyHintText = AddText(emptyHint, "Text_EmptyHint", "KHÔNG CÒN VẬT PHẨM NÀO ĐỂ BÁN", 28,
                                  new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center);
        Stretch(r.EmptyHintText.rectTransform, 0, 0, 0, 0);
        r.EmptyHint = emptyHint.gameObject;
        r.EmptyHint.SetActive(false);

        // ── Cột phải: khu thiết lập ──────────────────────────────────────────
        RectTransform colSetup = CreateUI("Col_Setup", r.Panel);
        Anchor(colSetup, new Vector2(0f, 0.5f), new Vector2(1250f, -40f), new Vector2(460f, 640f));
        Sliced(colSetup, "stall_slot", StallSpriteFactory.Hex("#2E1D42"));

        RectTransform setupEmpty = CreateUI("Setup_EmptyHint", colSetup);
        Stretch(setupEmpty, 20, 20, 20, 20);
        TextMeshProUGUI setupEmptyTxt = AddText(setupEmpty, "Text_SetupHint",
            "Chọn một vật phẩm để đặt lên quầy", 26, new Color(1f, 1f, 1f, 0.45f),
            TextAlignmentOptions.Center);
        Stretch(setupEmptyTxt.rectTransform, 0, 0, 0, 0);
        r.SetupEmptyHint = setupEmpty.gameObject;

        RectTransform setup = CreateUI("Setup_Content", colSetup);
        Stretch(setup, 0, 0, 0, 0);
        r.SetupContent = setup.gameObject;

        RectTransform selIcon = CreateUI("IMG_SelectedIcon", setup);
        Anchor(selIcon, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(150f, 150f));
        r.SelectedIcon = selIcon.gameObject.AddComponent<Image>();
        r.SelectedIcon.preserveAspect = true;

        r.SelectedName = AddText(setup, "Text_SelectedName", "", 30, StallSpriteFactory.Cream,
                                 TextAlignmentOptions.Center);
        Anchor(r.SelectedName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -206f),
               new Vector2(420f, 44f));

        // Hàng SỐ LƯỢNG
        BuildStepRow(setup, "Row_Quantity", -276f, "SỐ LƯỢNG", false,
                     out r.QtyMinus, out r.QtyPlus, out r.QtyText);

        // Hàng GIÁ BÁN (có icon xu)
        BuildStepRow(setup, "Row_Price", -392f, "GIÁ BÁN", true,
                     out r.PriceMinus, out r.PricePlus, out r.PriceText);

        r.PriceHint = AddText(setup, "Text_PriceHint", "", 20, new Color(1f, 1f, 1f, 0.55f),
                              TextAlignmentOptions.Center);
        Anchor(r.PriceHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -452f),
               new Vector2(420f, 34f));

        // ── Nút gạt loa (B7) ─────────────────────────────────────────────────
        RectTransform loaRow = CreateUI("Switch_Loa", setup);
        Anchor(loaRow, new Vector2(0.5f, 1f), new Vector2(0f, -512f), new Vector2(410f, 76f));
        Sliced(loaRow, "stall_btn", StallSpriteFactory.Hex("#3B2653"));
        r.LoaButton = loaRow.gameObject.AddComponent<Button>();
        r.LoaButton.targetGraphic = loaRow.GetComponent<Image>();
        r.LoaButton.transition    = Selectable.Transition.None;

        RectTransform loaIcon = CreateUI("IMG_ArtSpeakerIcon", loaRow);
        Anchor(loaIcon, new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(42f, 42f));
        Simple(loaIcon, "stall_icon_speaker", StallSpriteFactory.Cream);

        r.LoaLabel = AddText(loaRow, "Text_LoaLabel", "BẬT LOA", 24, StallSpriteFactory.Cream,
                             TextAlignmentOptions.MidlineLeft);
        Anchor(r.LoaLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 0f),
               new Vector2(150f, 40f));

        RectTransform track = CreateUI("Loa_Track", loaRow);
        Anchor(track, new Vector2(1f, 0.5f), new Vector2(-90f, 0f), new Vector2(160f, 56f));
        r.LoaTrack = track.gameObject.AddComponent<Image>();
        r.LoaTrack.sprite = StallSpriteFactory.Load("stall_btn");
        r.LoaTrack.type   = Image.Type.Sliced;
        r.LoaTrack.color  = StallSpriteFactory.Hex("#5A4D6B");
        r.LoaTrack.raycastTarget = false;

        r.LoaKnob = CreateUI("Loa_Knob", track);
        Anchor(r.LoaKnob, new Vector2(0.5f, 0.5f), new Vector2(-46f, 0f), new Vector2(48f, 48f));
        Simple(r.LoaKnob, "stall_circle", StallSpriteFactory.Cream);

        RectTransform loaCoin = CreateUI("IMG_ArtLoaCoin", loaRow);
        Anchor(loaCoin, new Vector2(0.5f, 0f), new Vector2(-6f, 14f), new Vector2(26f, 26f));
        Simple(loaCoin, "stall_icon_coin", Color.white);

        r.LoaCost = AddText(loaRow, "Text_LoaCost", "0", 20, StallSpriteFactory.Gold,
                            TextAlignmentOptions.MidlineLeft);
        Anchor(r.LoaCost.rectTransform, new Vector2(0.5f, 0f), new Vector2(36f, 14f),
               new Vector2(80f, 28f));

        // ── Nút xác nhận ─────────────────────────────────────────────────────
        RectTransform confirm = CreateUI("Btn_Confirm", setup);
        Anchor(confirm, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(380f, 86f));
        Sliced(confirm, "stall_btn", StallSpriteFactory.Hex("#2FBF6A"));
        r.ConfirmButton = confirm.gameObject.AddComponent<Button>();
        r.ConfirmButton.targetGraphic = confirm.GetComponent<Image>();
        r.ConfirmLabel = AddText(confirm, "Text_Confirm", "Đặt lên quầy", 32,
                                 StallSpriteFactory.Hex("#0E3A20"), TextAlignmentOptions.Center);
        Stretch(r.ConfirmLabel.rectTransform, 10, 6, 10, 6);
        r.ConfirmLabel.fontStyle = FontStyles.Bold;

        r.SetupContent.SetActive(false);
        r.Root.gameObject.SetActive(false);
        r.Panel.anchoredPosition = new Vector2(POPUP_W + 200f, 0f);

        return r;
    }

    /// <summary>Một hàng `[−]  giá trị  [+]` — dùng cho cả SỐ LƯỢNG lẫn GIÁ BÁN (B5).</summary>
    private static void BuildStepRow(RectTransform parent, string name, float y, string label,
                                     bool withCoin,
                                     out Button minus, out Button plus, out TextMeshProUGUI valueText)
    {
        RectTransform row = CreateUI(name, parent);
        Anchor(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(420f, 96f));

        TextMeshProUGUI cap = AddText(row, "Text_Caption", label, 19,
                                      new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);
        Anchor(cap.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(300f, 24f));

        // Dùng dấu trừ ASCII "-" chứ KHÔNG dùng U+2212 "−".
        // Font mặc định của dự án là LiberationSans SDF kiểu Static, chỉ 250 ký tự và
        // KHÔNG có U+2212 → nút sẽ hiện ra ô vuông rỗng. Đây đúng cái bẫy mà nút X của
        // bảng tin chợ đã né. Khi nào đổi sang font Dynamic có đủ dấu thì mới dùng lại được.
        minus = MakeStepButton(row, "Btn_Minus", "-", new Vector2(0f, 0.5f), new Vector2(48f, -8f));
        plus  = MakeStepButton(row, "Btn_Plus",  "+", new Vector2(1f, 0.5f), new Vector2(-48f, -8f));

        RectTransform box = CreateUI("Value_Box", row);
        Anchor(box, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(220f, 62f));
        Sliced(box, "stall_slot", StallSpriteFactory.Hex("#20122F"));

        if (withCoin)
        {
            RectTransform coin = CreateUI("IMG_ArtCoin", box);
            Anchor(coin, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(34f, 34f));
            Simple(coin, "stall_icon_coin", Color.white);
        }

        valueText = AddText(box, "Text_Value", "0", 30, StallSpriteFactory.Cream,
                            withCoin ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center);
        Stretch(valueText.rectTransform, withCoin ? 60 : 8, 4, 12, 4);
    }

    private static Button MakeStepButton(RectTransform parent, string name, string glyph,
                                         Vector2 anchor, Vector2 pos)
    {
        RectTransform rt = CreateUI(name, parent);
        Anchor(rt, anchor, pos, new Vector2(72f, 62f));
        Sliced(rt, "stall_btn", new Color(0.18f, 0.75f, 0.40f, 1f));

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();

        // Transition.None vì màu của nút này do StallPopupUI.SetStepButtonEnabled điều khiển
        // tay (yêu cầu B5: `−` chuyển XÁM khi chạm giới hạn). Để ColorTint thì Unity sẽ
        // ghi đè màu đó mỗi lần con trỏ đi qua và tín hiệu "hết đường giảm" biến mất.
        b.transition = Selectable.Transition.None;

        TextMeshProUGUI t = AddText(rt, "Text_Glyph", glyph, 38, Color.white, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 0, 0, 0, 4);
        t.fontStyle = FontStyles.Bold;

        return b;
    }

    private static void AddCategoryTab(RectTransform parent, StallItemCategory category,
                                       string label, List<Object> collector)
    {
        RectTransform rt = CreateUI($"Tab_{category}", parent);
        rt.sizeDelta = new Vector2(190f, 96f);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 96f;

        Image bgImg = rt.gameObject.AddComponent<Image>();
        bgImg.sprite = StallSpriteFactory.Load("stall_btn");
        bgImg.type   = Image.Type.Sliced;
        bgImg.color  = new Color(0.28f, 0.18f, 0.40f, 1f);

        Button b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = bgImg;
        b.transition    = Selectable.Transition.None;

        RectTransform icon = CreateUI("IMG_ArtCategoryIcon", rt);
        Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(44f, 44f));
        Image iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.sprite = StallSpriteFactory.Load("stall_circle");
        iconImg.color  = new Color(1f, 1f, 1f, 0.55f);
        iconImg.raycastTarget = false;

        TextMeshProUGUI txt = AddText(rt, "Text_Label", label, 20, StallSpriteFactory.Cream,
                                      TextAlignmentOptions.Center);
        Anchor(txt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(180f, 30f));

        StallCategoryTabUI tab = rt.gameObject.AddComponent<StallCategoryTabUI>();
        tab.EditorSetCategory(category);

        new Wiring(tab)
            .Obj("button",                b)
            .Obj("imageArtTabBackground", bgImg)
            .Obj("imageArtCategoryIcon",  iconImg)
            .Obj("label",                 txt)
            .Obj("scaleTarget",           rt)
            .Apply();

        collector.Add(tab);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREFAB Ô QUẦY (4 TRẠNG THÁI)
    // ─────────────────────────────────────────────────────────────────────────

    private static StallSlotUI BuildSlotPrefab()
    {
        EnsurePrefabFolder();

        var root = new GameObject("PF_StallSlot", typeof(RectTransform));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250f, 230f);

        RectTransform bg = CreateUI("IMG_ArtSlotBackground", rt);
        Stretch(bg, 0, 0, 0, 0);
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = StallSpriteFactory.Load("stall_slot");
        bgImg.type   = Image.Type.Sliced;
        bgImg.color  = new Color(0.24f, 0.15f, 0.35f, 1f);

        // ── Trạng thái 1: TRỐNG, DÙNG ĐƯỢC ───────────────────────────────────
        RectTransform empty = CreateUI("State_Empty", rt);
        Stretch(empty, 0, 0, 0, 0);
        Image emptyHit = empty.gameObject.AddComponent<Image>();
        emptyHit.color = new Color(1f, 1f, 1f, 0.001f);   // vùng bấm phủ cả ô
        Button sellBtn = empty.gameObject.AddComponent<Button>();
        sellBtn.targetGraphic = emptyHit;

        RectTransform plus = CreateUI("IMG_ArtPlusIcon", empty);
        Anchor(plus, new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(76f, 76f));
        Simple(plus, "stall_icon_plus", StallSpriteFactory.Cream);

        TextMeshProUGUI emptyLabel = AddText(empty, "Text_EmptyLabel", "Bán vật phẩm", 24,
                                             StallSpriteFactory.Cream, TextAlignmentOptions.Center);
        Anchor(emptyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -52f),
               new Vector2(220f, 36f));

        // ── Trạng thái 2: ĐANG BÁN ───────────────────────────────────────────
        RectTransform selling = CreateUI("State_Selling", rt);
        Stretch(selling, 0, 0, 0, 0);

        RectTransform icon = CreateUI("IMG_ItemIcon", selling);
        Anchor(icon, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(96f, 96f));
        Image iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        TextMeshProUGUI qty = AddText(selling, "Text_Quantity", "x0", 24, StallSpriteFactory.Cream,
                                      TextAlignmentOptions.MidlineRight);
        Anchor(qty.rectTransform, new Vector2(1f, 1f), new Vector2(-52f, -34f), new Vector2(90f, 34f));

        RectTransform priceRow = CreateUI("Row_Price", selling);
        Anchor(priceRow, new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(190f, 40f));
        RectTransform priceCoin = CreateUI("IMG_ArtCoin", priceRow);
        Anchor(priceCoin, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(30f, 30f));
        Simple(priceCoin, "stall_icon_coin", Color.white);
        TextMeshProUGUI price = AddText(priceRow, "Text_Price", "0", 26, StallSpriteFactory.Gold,
                                        TextAlignmentOptions.MidlineLeft);
        Stretch(price.rectTransform, 52, 2, 6, 2);

        TextMeshProUGUI remain = AddText(selling, "Text_RemainTime", "", 19,
                                         new Color(1f, 1f, 1f, 0.5f), TextAlignmentOptions.Center);
        Anchor(remain.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(200f, 28f));

        RectTransform loaBadge = CreateUI("Badge_Loa", selling);
        Anchor(loaBadge, new Vector2(0f, 1f), new Vector2(34f, -32f), new Vector2(44f, 44f));
        Simple(loaBadge, "stall_circle", StallSpriteFactory.Teal);
        RectTransform loaBadgeIcon = CreateUI("IMG_ArtSpeakerIcon", loaBadge);
        Anchor(loaBadgeIcon, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
        Simple(loaBadgeIcon, "stall_icon_speaker", Color.white);
        loaBadge.gameObject.SetActive(false);

        RectTransform cancel = CreateUI("Btn_Cancel", selling);
        Anchor(cancel, new Vector2(1f, 0f), new Vector2(-34f, 34f), new Vector2(48f, 48f));
        Image cancelImg = cancel.gameObject.AddComponent<Image>();
        cancelImg.sprite = StallSpriteFactory.Load("stall_circle");
        cancelImg.color  = StallSpriteFactory.Hex("#E4574C");
        Button cancelBtn = cancel.gameObject.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImg;
        TextMeshProUGUI cancelTxt = AddText(cancel, "Text_X", "X", 24, Color.white,
                                            TextAlignmentOptions.Center);
        Stretch(cancelTxt.rectTransform, 0, 0, 0, 2);

        // ── Trạng thái 3: KHOÁ, MỞ ĐƯỢC ──────────────────────────────────────
        RectTransform unlockable = CreateUI("State_Unlockable", rt);
        Stretch(unlockable, 0, 0, 0, 0);

        RectTransform lockIcon = CreateUI("IMG_ArtLockIcon", unlockable);
        Anchor(lockIcon, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(62f, 62f));
        Simple(lockIcon, "stall_icon_lock", StallSpriteFactory.Cream);

        TextMeshProUGUI unlockLabel = AddText(unlockable, "Text_UnlockLabel", "Thêm ô", 24,
                                              StallSpriteFactory.Cream, TextAlignmentOptions.Center);
        Anchor(unlockLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f),
               new Vector2(200f, 32f));

        RectTransform unlockBtn = CreateUI("Btn_Unlock", unlockable);
        Anchor(unlockBtn, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(178f, 60f));
        Sliced(unlockBtn, "stall_btn", StallSpriteFactory.Gold);
        Button unlockButton = unlockBtn.gameObject.AddComponent<Button>();
        unlockButton.targetGraphic = unlockBtn.GetComponent<Image>();

        RectTransform unlockCoin = CreateUI("IMG_ArtCoin", unlockBtn);
        Anchor(unlockCoin, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(30f, 30f));
        Simple(unlockCoin, "stall_icon_coin", Color.white);

        TextMeshProUGUI unlockCost = AddText(unlockBtn, "Text_UnlockCost", "0", 26,
                                             StallSpriteFactory.Hex("#4A3208"),
                                             TextAlignmentOptions.MidlineLeft);
        Stretch(unlockCost.rectTransform, 54, 4, 10, 4);
        unlockCost.fontStyle = FontStyles.Bold;

        // ── Trạng thái 4: CHƯA TỚI LƯỢT (ô trơn) ─────────────────────────────
        RectTransform locked = CreateUI("State_Locked", rt);
        Stretch(locked, 10, 10, 10, 10);
        Image lockedImg = locked.gameObject.AddComponent<Image>();
        lockedImg.sprite = StallSpriteFactory.Load("stall_slot");
        lockedImg.type   = Image.Type.Sliced;
        lockedImg.color  = new Color(0.13f, 0.08f, 0.20f, 1f);
        lockedImg.raycastTarget = false;

        StallSlotUI slotUI = root.AddComponent<StallSlotUI>();
        new Wiring(slotUI)
            .Obj("stateEmptyRoot",         empty.gameObject)
            .Obj("stateSellingRoot",       selling.gameObject)
            .Obj("stateUnlockableRoot",    unlockable.gameObject)
            .Obj("stateLockedRoot",        locked.gameObject)
            .Obj("buttonSell",             sellBtn)
            .Obj("textEmptyLabel",         emptyLabel)
            .Obj("imageItemIcon",          iconImg)
            .Obj("textQuantity",           qty)
            .Obj("textPrice",              price)
            .Obj("textRemainTime",         remain)
            .Obj("loaBadge",               loaBadge.gameObject)
            .Obj("buttonCancel",           cancelBtn)
            .Obj("buttonUnlock",           unlockButton)
            .Obj("textUnlockCost",         unlockCost)
            .Obj("textUnlockLabel",        unlockLabel)
            .Obj("imageArtSlotBackground", bgImg)
            .Apply();

        string path = $"{PrefabFolder}/PF_StallSlot.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return saved != null ? saved.GetComponent<StallSlotUI>() : null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PREFAB Ô CHỌN VẬT PHẨM
    // ─────────────────────────────────────────────────────────────────────────

    private static StallPickItemCellUI BuildPickCellPrefab()
    {
        EnsurePrefabFolder();

        var root = new GameObject("PF_StallPickCell", typeof(RectTransform));
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(170f, 190f);

        Image bgImg = root.AddComponent<Image>();
        bgImg.sprite = StallSpriteFactory.Load("stall_slot");
        bgImg.type   = Image.Type.Sliced;
        bgImg.color  = new Color(0.24f, 0.15f, 0.35f, 1f);

        Button b = root.AddComponent<Button>();
        b.targetGraphic = bgImg;
        b.transition    = Selectable.Transition.None;   // màu do SetSelected điều khiển

        TextMeshProUGUI nameTxt = AddText(rt, "Text_Name", "", 19, StallSpriteFactory.Cream,
                                          TextAlignmentOptions.Center);
        Anchor(nameTxt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f),
               new Vector2(158f, 36f));
        // Tên vật phẩm tiếng Việt hay dài ("Hoa Cẩm Tú Cầu") — ô rộng 170px, phải cho xuống dòng.
        nameTxt.textWrappingMode = TextWrappingModes.Normal;
        nameTxt.overflowMode     = TextOverflowModes.Ellipsis;

        RectTransform icon = CreateUI("IMG_Icon", rt);
        Anchor(icon, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(92f, 92f));
        Image iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        // Badge số lượng ở GÓC DƯỚI PHẢI — đúng vị trí trong video, và là thứ duy nhất
        // cho biết còn bao nhiêu để bán.
        RectTransform badge = CreateUI("Badge_Amount", rt);
        Anchor(badge, new Vector2(1f, 0f), new Vector2(-28f, 26f), new Vector2(48f, 48f));
        Simple(badge, "stall_circle", StallSpriteFactory.Teal);
        TextMeshProUGUI amountTxt = AddText(badge, "Text_Amount", "0", 22, Color.white,
                                            TextAlignmentOptions.Center);
        Stretch(amountTxt.rectTransform, 0, 0, 0, 0);
        amountTxt.fontStyle = FontStyles.Bold;

        RectTransform frame = CreateUI("Frame_Selected", rt);
        Stretch(frame, -4, -4, -4, -4);
        Image frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = StallSpriteFactory.Load("stall_slot");
        frameImg.type   = Image.Type.Sliced;
        frameImg.color  = new Color(0.18f, 0.75f, 0.66f, 0.45f);
        frameImg.raycastTarget = false;
        frame.gameObject.SetActive(false);

        StallPickItemCellUI cell = root.AddComponent<StallPickItemCellUI>();
        new Wiring(cell)
            .Obj("imageIcon",              iconImg)
            .Obj("textName",               nameTxt)
            .Obj("textAmount",             amountTxt)
            .Obj("button",                 b)
            .Obj("imageArtCellBackground", bgImg)
            .Obj("selectedFrame",          frame.gameObject)
            .Apply();

        string path = $"{PrefabFolder}/PF_StallPickCell.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return saved != null ? saved.GetComponent<StallPickItemCellUI>() : null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  3 · QUẦY NGOÀI MAP
    // ─────────────────────────────────────────────────────────────────────────

    private static void BuildWorldObject()
    {
        // Sinh sprite trước (bỏ qua file đã có). Nếu người dùng bấm thẳng nút "chỉ dựng
        // quầy ngoài map" mà chưa chạy bước 1 thì mọi Load() trả null và cái quầy ra
        // một cục vô hình giữa bản đồ — rất khó đoán nguyên nhân.
        StallSpriteFactory.GenerateAll(false);

        // GameObject.Find KHÔNG thấy object đang TẮT → chạy tool lúc quầy đang tắt sẽ
        // sinh ra cái thứ hai, bản đồ có hai quầy chồng nhau. Phải quét cả object tắt.
        GameObject old = null;
        foreach (var t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.parent == null && t.name == WorldObjectName) { old = t.gameObject; break; }
        }

        // GIỮ LẠI VỊ TRÍ người dùng đã kéo. Trước đây destroy rồi tạo mới ở (0,0) nên
        // mỗi lần chạy lại tool là quầy nhảy về giữa bản đồ, phải kéo lại từ đầu.
        Vector3 viTriCu = old != null ? old.transform.position : new Vector3(0f, 0f, 0f);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject(WorldObjectName);
        Undo.RegisterCreatedObjectUndo(root, "Tạo quầy hàng ngoài map");
        root.transform.position = viTriCu;

        // Thân quầy — nền màu phẳng, chờ art
        var body = new GameObject("SPR_ArtStallBody");
        body.transform.SetParent(root.transform, false);
        SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = StallSpriteFactory.Load("stall_panel");
        bodySr.color  = StallSpriteFactory.Hex("#553873");
        bodySr.drawMode = SpriteDrawMode.Sliced;
        bodySr.size     = new Vector2(3.2f, 2.0f);
        bodySr.sortingOrder = 0;

        // Mái hiên răng sò
        var roof = new GameObject("SPR_ArtStallValance");
        roof.transform.SetParent(root.transform, false);
        roof.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        SpriteRenderer roofSr = roof.AddComponent<SpriteRenderer>();
        roofSr.sprite   = StallSpriteFactory.Load("stall_valance");
        roofSr.color    = Color.white;
        roofSr.drawMode = SpriteDrawMode.Tiled;
        roofSr.size     = new Vector2(3.4f, 0.55f);
        roofSr.sortingOrder = 2;

        // Mặt quầy: 5 chỗ bày hàng — nhìn từ ngoài là biết đang bán gì (B2)
        var display = new GameObject("Counter_Display");
        display.transform.SetParent(root.transform, false);
        display.transform.localPosition = new Vector3(0f, -0.35f, 0f);

        var slots = new List<Object>();
        for (int i = 0; i < 5; i++)
        {
            var s = new GameObject($"DisplaySlot_{i}");
            s.transform.SetParent(display.transform, false);
            s.transform.localPosition = new Vector3(-1.2f + i * 0.6f, 0f, 0f);
            s.transform.localScale    = new Vector3(0.5f, 0.5f, 1f);

            SpriteRenderer sr = s.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            sr.enabled = false;
            slots.Add(sr);
        }

        var emptySign = new GameObject("SPR_ArtEmptySign");
        emptySign.transform.SetParent(display.transform, false);
        SpriteRenderer signSr = emptySign.AddComponent<SpriteRenderer>();
        signSr.sprite = StallSpriteFactory.Load("stall_slot");
        signSr.color  = new Color(1f, 1f, 1f, 0.20f);
        signSr.drawMode = SpriteDrawMode.Sliced;
        signSr.size     = new Vector2(2.6f, 0.5f);
        signSr.sortingOrder = 3;

        StallCounterDisplay counter = display.AddComponent<StallCounterDisplay>();
        new Wiring(counter)
            .ObjList("displaySlots", slots)
            .Obj("emptySign", emptySign)
            .Apply();

        // Vùng bấm
        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(3.4f, 2.6f);
        col.offset = new Vector2(0f, 0.2f);
        col.isTrigger = true;

        StallWorldObject world = root.AddComponent<StallWorldObject>();

        StallPopupUI popup = Object.FindFirstObjectByType<StallPopupUI>(FindObjectsInactive.Include);
        if (popup == null)
            Debug.LogWarning("[QuầyHàng] Chưa có Canvas_StallPopup trong scene → quầy ngoài map " +
                             "chưa nối được popup. Chạy 'Dựng TẤT CẢ' hoặc dựng popup trước.");

        new Wiring(world)
            .Obj("popupUI",        popup)
            .Obj("mainCamera",     Camera.main)
            .Obj("targetCollider", col)
            .Apply();

        Debug.Log("[QuầyHàng] Đã tạo quầy ngoài map tại (0,0) — kéo tới vị trí mong muốn trên bản đồ.");
        MarkSceneDirty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH DỰNG UI
    // ─────────────────────────────────────────────────────────────────────────

    private static RectTransform CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        return rt;
    }

    /// <summary>Giãn kín cha với lề trái/dưới/phải/trên.</summary>
    private static void Stretch(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    private static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    /// <summary>Dải bám mép trên, cao <paramref name="height"/>, cách đỉnh <paramref name="top"/>.</summary>
    private static void TopStretch(RectTransform rt, float height, float top)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, 0f);
        rt.offsetMax = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(0f, -top);
        rt.sizeDelta        = new Vector2(0f, height);
    }

    /// <summary>Content của ScrollRect dọc: bám mép trên, cao tự co theo ContentSizeFitter.</summary>
    private static void TopStretchContent(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, 100f);
    }

    private static Image Sliced(RectTransform rt, string spriteName, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = StallSpriteFactory.Load(spriteName);
        img.type   = Image.Type.Sliced;
        img.color  = color;
        return img;
    }

    private static Image Simple(RectTransform rt, string spriteName, Color color)
    {
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = StallSpriteFactory.Load(spriteName);
        img.type   = Image.Type.Simple;
        img.color  = color;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }

    private static TextMeshProUGUI AddText(RectTransform parent, string name, string content,
                                           float size, Color color, TextAlignmentOptions align)
    {
        RectTransform rt = CreateUI(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text          = content;
        t.fontSize      = size;
        t.color         = color;
        t.alignment     = align;
        t.raycastTarget = false;
        // API mới của TMP trong Unity 6 (`enableWordWrapping` đã bị đánh dấu lỗi thời).
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode     = TextOverflowModes.Overflow;
        return t;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  NỐI DÂY VÀO FIELD PRIVATE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gán vào `[SerializeField] private` qua SerializedObject.
    ///
    /// Cố tình KHÔNG mở các field đó thành public chỉ để tool gán được: field public là
    /// lời mời cho mọi script khác sửa thẳng tham chiếu UI từ bên ngoài, và đó là cách
    /// một popup dần biến thành thứ không ai dám đụng. Sai tên field ở đây sẽ báo lỗi
    /// đỏ ngay lúc chạy tool chứ không âm thầm bỏ qua.
    /// </summary>
    private class Wiring
    {
        private readonly SerializedObject _so;
        private readonly string           _name;

        public Wiring(Object target)
        {
            _so   = new SerializedObject(target);
            _name = target != null ? target.GetType().Name : "(null)";
        }

        public Wiring Obj(string propertyName, Object value)
        {
            SerializedProperty p = _so.FindProperty(propertyName);
            if (p == null) { Debug.LogError($"[QuầyHàng] {_name}: không có field '{propertyName}'."); return this; }
            p.objectReferenceValue = value;
            return this;
        }

        public Wiring Num(string propertyName, float value)
        {
            SerializedProperty p = _so.FindProperty(propertyName);
            if (p == null) { Debug.LogError($"[QuầyHàng] {_name}: không có field '{propertyName}'."); return this; }
            p.floatValue = value;
            return this;
        }

        public Wiring ObjList(string propertyName, List<Object> values)
        {
            SerializedProperty p = _so.FindProperty(propertyName);
            if (p == null) { Debug.LogError($"[QuầyHàng] {_name}: không có field '{propertyName}'."); return this; }

            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            return this;
        }

        public void Apply() => _so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
