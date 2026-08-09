#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  DỰNG HIERARCHY BẢNG TIN CHỢ + 2 PREFAB (A8)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO dựng ở Editor chứ không dựng lúc chạy:
/// dự án đã có bài học UnifiedTaskPopupUI — 1433 dòng `new GameObject()` lúc runtime,
/// muốn dịch một cái nút sang phải 10px cũng phải đọc hết file rồi build lại.
/// Dựng bằng tool: chạy một lần ra hierarchy thật trong scene, sau đó chủ dự án
/// kéo thả trong Inspector như mọi UI bình thường. Cần dựng lại thì bấm menu.
///
/// ⚠️ Tool này XOÁ toàn bộ con của Canvas_MarketPopup (trừ object mang MarketManager)
/// rồi dựng lại từ đầu. Chỉnh tay xong mà chạy lại là mất chỉnh tay.
///
/// ── TRANG TRÍ KHÁC VIDEO THAM CHIẾU (tránh đạo ý tưởng) ─────────────────
/// Video: nền cam đất · mái hiên SỌC xanh-trắng · thẻ khung vé GÓC KHUYẾT ·
///        icon danh mục treo DÂY THỪNG.
/// Bản này: nền xanh mòng két · dải CHẤM BI tím-kem · thẻ BO GÓC TRÒN ĐỀU ·
///        tab danh mục dạng VIÊN THUỐC gắn trên thanh ray dọc.
/// Bố cục (lọc dọc bên trái · đếm ngược + làm mới trên phải · thẻ 2 tầng có người bán)
/// giữ theo video vì đó là bố cục tốt, còn diện mạo thì đổi hẳn.
/// </summary>
public static class MarketBoardUIBuilder
{
    // ── Kích thước ───────────────────────────────────────────────────────
    private const float PopupWidth   = 1180f;
    private const float PopupHeight  = 860f;
    private const float RibbonHeight = 64f;
    private const float RailWidth    = 120f;
    private const float CardWidth    = MarketBoardPalette.CardWidth;
    private const float CardHeight   = MarketBoardPalette.CardHeight;
    private const float CardSpacing  = 16f;
    private const float TabHeight    = 70f;
    private const float TabSpacing   = 8f;

    /// <summary>
    /// Khoảng chừa phía trên cho dải trang trí + tiêu đề + hàng chip đồng hồ.
    /// Đổi số này là cả ray danh mục lẫn vùng lưới cùng dịch — để hai chỗ rời rạc
    /// thì sửa một bên quên bên kia, ray sẽ đè lên nút làm mới.
    /// </summary>
    private const float ContentTopInset = 186f;

    private const string PrefabFolder    = "Assets/_Game/Prefab/ui/Market";
    private const string CardPrefabPath  = PrefabFolder + "/MarketListingCard_Prefab.prefab";
    private const string TabPrefabPath   = PrefabFolder + "/MarketCategoryTab_Prefab.prefab";
    private const string CanvasName      = "Canvas_MarketPopup";

    // ══════════════════════════════════════════════════════════════════════
    //  MENU
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Một nút làm hết. Thứ tự BẮT BUỘC: sprite → dữ liệu → hierarchy → nguồn icon.
    /// Dựng hierarchy trước khi có sprite thì mọi Image ra ô vuông trắng; gán nguồn icon
    /// trước khi có MarketManager mới thì gán vào object sắp bị xoá.
    /// </summary>
    [MenuItem("Tools/Farm/Chợ/0 · CHẠY TẤT CẢ (sprite → dữ liệu → UI → icon)", false, 0)]
    public static void RunEverything()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Chợ", "Thoát Play Mode trước đã.", "OK");
            return;
        }

        MarketBoardSpriteFactory.GenerateAll(false);
        MarketDatabaseGeneratorTool.Generate();
        BuildAll();
        RefillVisualSources();
    }

    [MenuItem("Tools/Farm/Chợ/3 · Dựng lại UI Bảng Tin Chợ", false, 30)]
    public static void BuildAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Chợ", "Thoát Play Mode trước khi dựng UI.", "OK");
            return;
        }

        MarketBoardSpriteFactory.GenerateAll(false);

        GameObject cardPrefab = BuildCardPrefab();
        GameObject tabPrefab  = BuildTabPrefab();

        if (cardPrefab == null || tabPrefab == null)
        {
            EditorUtility.DisplayDialog("Chợ", "Không tạo được prefab thẻ/tab. Xem Console.", "OK");
            return;
        }

        bool ok = BuildSceneHierarchy(cardPrefab, tabPrefab);

        EditorUtility.DisplayDialog("Chợ",
            ok
                ? "Đã dựng lại Bảng Tin Chợ.\n\nPrefab:\n" + CardPrefabPath + "\n" + TabPrefabPath +
                  "\n\nNhớ Ctrl+S để lưu scene."
                : "Không tìm thấy " + CanvasName + " trong scene đang mở.",
            "OK");
    }

    [MenuItem("Tools/Farm/Chợ/5 · Nạp lại nguồn icon cho MarketManager", false, 50)]
    public static void RefillVisualSources()
    {
        MarketManager manager = Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Chợ", "Không tìm thấy MarketManager trong scene.", "OK");
            return;
        }

        List<CropData> crops = new List<CropData>();
        string[] cropGuids = AssetDatabase.FindAssets("t:CropData");
        for (int i = 0; i < cropGuids.Length; i++)
        {
            CropData crop = AssetDatabase.LoadAssetAtPath<CropData>(AssetDatabase.GUIDToAssetPath(cropGuids[i]));
            if (crop != null) crops.Add(crop);
        }

        List<InventoryItemData> items = new List<InventoryItemData>();
        string[] itemGuids = AssetDatabase.FindAssets("t:InventoryItemData");
        for (int i = 0; i < itemGuids.Length; i++)
        {
            InventoryItemData item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(
                AssetDatabase.GUIDToAssetPath(itemGuids[i]));
            if (item != null) items.Add(item);
        }

        Undo.RecordObject(manager, "Nạp nguồn icon cho chợ");
        manager.EditorSetVisualSources(crops, items);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        EditorUtility.DisplayDialog("Chợ",
            "Đã nạp " + crops.Count + " CropData và " + items.Count + " InventoryItemData.",
            "OK");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HIERARCHY TRONG SCENE
    // ══════════════════════════════════════════════════════════════════════

    private static bool BuildSceneHierarchy(GameObject cardPrefab, GameObject tabPrefab)
    {
        GameObject canvasGO = FindInScene(CanvasName);
        if (canvasGO == null)
            return false;

        Undo.RegisterFullObjectHierarchyUndo(canvasGO, "Dựng lại Bảng Tin Chợ");

        // MarketManager phải được GIỮ NGUYÊN component: PopupManager, MarketClickOpen,
        // BuildingInteractable đang trỏ tới nó bằng tham chiếu scene. Tạo mới là ba
        // chỗ đó thành null mà không có lỗi biên dịch nào cảnh báo.
        MarketManager manager = Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);
        GameObject boardGO;

        if (manager != null)
        {
            boardGO = manager.gameObject;
            boardGO.transform.SetParent(canvasGO.transform, false);   // gỡ khỏi cây cũ trước khi xoá cây cũ
        }
        else
        {
            boardGO = new GameObject("Popup_Board", typeof(RectTransform));
            boardGO.transform.SetParent(canvasGO.transform, false);
            manager = boardGO.AddComponent<MarketManager>();
        }

        // Xoá cây cũ (Panel_Background và mọi thứ bên trong)
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasGO.transform.GetChild(i);
            if (child.gameObject == boardGO)
                continue;
            Object.DestroyImmediate(child.gameObject);
        }

        // ── Panel_Dim: gốc popup, tắt sẵn ────────────────────────────────
        RectTransform dim = NewRect("Panel_Dim", canvasGO.transform);
        StretchFull(dim);
        AddImage(dim, SpritePanel(), MarketBoardPalette.Dim, Image.Type.Sliced);

        boardGO.name = "Popup_Board";
        boardGO.transform.SetParent(dim, false);

        RectTransform board = boardGO.GetComponent<RectTransform>();
        if (board == null)
            board = boardGO.AddComponent<RectTransform>();

        Center(board, new Vector2(PopupWidth, PopupHeight));
        Image boardImage = Ensure<Image>(boardGO);
        boardImage.sprite = SpritePanel();
        boardImage.type   = Image.Type.Sliced;
        boardImage.color  = MarketBoardPalette.PanelBase;

        // Dọn sạch con cũ của board rồi dựng lại
        for (int i = board.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(board.GetChild(i).gameObject);

        MarketBoardUI boardUI = Ensure<MarketBoardUI>(boardGO);

        // ── Trang trí: dải chấm bi vắt ngang đỉnh ────────────────────────
        RectTransform ribbon = NewRect("Deco_RibbonTop", board);
        AnchorTopStretch(ribbon, RibbonHeight, 18f, 18f, -10f);
        AddImage(ribbon, SpritePanel(), MarketBoardPalette.RibbonBase, Image.Type.Sliced);

        RectTransform ribbonDots = NewRect("Deco_RibbonDots", ribbon);
        StretchFull(ribbonDots);
        Image dotsImage = AddImage(ribbonDots, SpriteDots(), MarketBoardPalette.RibbonDot, Image.Type.Tiled);
        dotsImage.raycastTarget = false;

        // ── Tiêu đề dạng viên thuốc đè lên dải ───────────────────────────
        RectTransform titlePill = NewRect("Header_TitlePill", board);
        AnchorTop(titlePill, new Vector2(460f, 84f), new Vector2(0f, -12f));
        AddImage(titlePill, SpritePill(), MarketBoardPalette.RibbonBase, Image.Type.Sliced);

        RectTransform titleText = NewRect("Text_Title", titlePill);
        StretchFull(titleText);
        AddText(titleText, "BẢNG TIN CHỢ", 40f, MarketBoardPalette.TextOnPanel,
                TextAlignmentOptions.Center, FontStyles.Bold);

        // ── Nút X LỒI RA NGOÀI mép phải (offset âm = đẩy ra khỏi panel) ──
        RectTransform closeRT = NewRect("Btn_Close", board);
        AnchorTopRight(closeRT, new Vector2(78f, 78f), new Vector2(-14f, -6f));
        Image closeImage = AddImage(closeRT, SpriteCircle(), MarketBoardPalette.ButtonClose, Image.Type.Simple);
        Button closeButton = closeRT.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;

        RectTransform closeText = NewRect("Text_Close", closeRT);
        StretchFull(closeText);
        // Dùng chữ "X" thường chứ KHÔNG dùng ✕ (U+2715): font mặc định của dự án là
        // LiberationSans, ký tự đó không có trong bộ nên sẽ ra ô vuông rỗng.
        AddText(closeText, "X", 40f, MarketBoardPalette.TextOnPanel,
                TextAlignmentOptions.Center, FontStyles.Bold);

        // ── Ví vàng, góc trên trái ───────────────────────────────────────
        RectTransform goldChip = NewRect("Chip_Gold", board);
        AnchorTopLeft(goldChip, new Vector2(230f, 60f), new Vector2(26f, 110f));
        AddImage(goldChip, SpritePill(), MarketBoardPalette.PanelInset, Image.Type.Sliced);

        RectTransform goldIcon = NewRect("Icon_Gold_ChoArt", goldChip);
        AnchorLeft(goldIcon, new Vector2(40f, 40f), 14f);
        AddImage(goldIcon, SpriteCircle(), MarketBoardPalette.TextGold, Image.Type.Simple);

        RectTransform goldText = NewRect("Text_Gold", goldChip);
        StretchFull(goldText, 62f, 0f, 16f, 0f);
        TMP_Text textGold = AddText(goldText, "0", 30f, MarketBoardPalette.TextGold,
                                    TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        // ── Đếm ngược + nút làm mới, góc trên phải ───────────────────────
        RectTransform timerChip = NewRect("Chip_Timer", board);
        AnchorTopRight(timerChip, new Vector2(300f, 60f), new Vector2(304f, 110f));
        AddImage(timerChip, SpritePill(), MarketBoardPalette.PanelInset, Image.Type.Sliced);

        RectTransform timerFill = NewRect("Fill_Timer", timerChip);
        StretchFull(timerFill, 4f, 4f, 4f, 4f);
        Image fillTimer = AddImage(timerFill, SpritePill(), MarketBoardPalette.TabIdle, Image.Type.Filled);
        fillTimer.fillMethod  = Image.FillMethod.Horizontal;
        fillTimer.fillOrigin  = 0;
        fillTimer.fillAmount  = 1f;
        fillTimer.raycastTarget = false;

        RectTransform timerLabel = NewRect("Text_TimerLabel", timerChip);
        StretchFull(timerLabel, 18f, 0f, 118f, 0f);
        AddText(timerLabel, "Làm mới sau", 22f, MarketBoardPalette.TextMuted,
                TextAlignmentOptions.MidlineLeft);

        RectTransform timerValue = NewRect("Text_Timer", timerChip);
        StretchFull(timerValue, 180f, 0f, 18f, 0f);
        TMP_Text textTimer = AddText(timerValue, "05:00", 28f, MarketBoardPalette.TextOnPanel,
                                     TextAlignmentOptions.MidlineRight, FontStyles.Bold);

        RectTransform refreshRT = NewRect("Btn_Refresh", board);
        AnchorTopRight(refreshRT, new Vector2(262f, 64f), new Vector2(26f, 108f));
        Image refreshImage = AddImage(refreshRT, SpritePill(), MarketBoardPalette.ButtonGold, Image.Type.Sliced);
        Button refreshButton = refreshRT.gameObject.AddComponent<Button>();
        refreshButton.targetGraphic = refreshImage;

        RectTransform refreshIcon = NewRect("Icon_Gold_ChoArt", refreshRT);
        AnchorLeft(refreshIcon, new Vector2(34f, 34f), 14f);
        AddImage(refreshIcon, SpriteCircle(), MarketBoardPalette.TextGold, Image.Type.Simple);

        RectTransform refreshCost = NewRect("Text_RefreshCost", refreshRT);
        AnchorLeft(refreshCost, new Vector2(84f, 40f), 52f);
        TMP_Text textRefreshCost = AddText(refreshCost, "150", 26f, MarketBoardPalette.TextOnCard,
                                           TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        RectTransform refreshLabel = NewRect("Text_RefreshLabel", refreshRT);
        StretchFull(refreshLabel, 138f, 0f, 14f, 0f);
        TMP_Text textRefreshLabel = AddText(refreshLabel, "LÀM MỚI NGAY", 20f, MarketBoardPalette.TextOnCard,
                                            TextAlignmentOptions.Midline, FontStyles.Bold);

        // ── Thanh ray + dải lọc danh mục dọc bên trái ────────────────────
        RectTransform rail = NewRect("Rail_Categories", board);
        AnchorLeftStretch(rail, RailWidth, 24f, 24f, ContentTopInset);
        AddImage(rail, SpritePanel(), MarketBoardPalette.TabRail, Image.Type.Sliced);

        RectTransform railContent = NewRect("Content_Categories", rail);
        AnchorTopStretch(railContent, 10f, 8f, 8f, 12f);
        railContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vlg = railContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing              = TabSpacing;
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter railFitter = railContent.gameObject.AddComponent<ContentSizeFitter>();
        railFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Vùng lưới thẻ ────────────────────────────────────────────────
        RectTransform listingArea = NewRect("Panel_ListingArea", board);
        StretchFull(listingArea, 24f + RailWidth + 16f, 24f, 24f, ContentTopInset);
        AddImage(listingArea, SpritePanel(), MarketBoardPalette.PanelInset, Image.Type.Sliced);

        RectTransform scrollRT = NewRect("Scroll_Listings", listingArea);
        StretchFull(scrollRT, 10f, 10f, 10f, 10f);
        ScrollRect scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal          = false;
        scroll.vertical            = true;
        scroll.movementType        = ScrollRect.MovementType.Elastic;
        scroll.elasticity          = 0.1f;
        scroll.scrollSensitivity   = 32f;

        RectTransform viewport = NewRect("Viewport", scrollRT);
        StretchFull(viewport);
        viewport.pivot = new Vector2(0.5f, 1f);
        viewport.gameObject.AddComponent<RectMask2D>();   // rẻ hơn Mask, không cần Image nền

        RectTransform content = NewRect("Content_Listings", viewport);
        AnchorTopStretch(content, 100f, 0f, 0f, 0f);
        content.pivot = new Vector2(0.5f, 1f);

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize    = new Vector2(CardWidth, CardHeight);
        grid.spacing     = new Vector2(CardSpacing, CardSpacing);
        grid.constraint  = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = MarketBoardPalette.GridColumns;
        grid.childAlignment  = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(16, 16, 16, 16);

        // ContentSizeFitter — bản cũ THIẾU cái này nên cuộn không tới hàng cuối
        ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content  = content;

        // ── Trạng thái rỗng ──────────────────────────────────────────────
        RectTransform empty = NewRect("Panel_Empty", listingArea);
        StretchFull(empty, 40f, 40f, 40f, 40f);
        RectTransform emptyText = NewRect("Text_Empty", empty);
        StretchFull(emptyText);
        TMP_Text textEmpty = AddText(emptyText, "CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN",
                                     30f, MarketBoardPalette.TextMuted,
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        empty.gameObject.SetActive(false);

        // ── Thông báo ngắn ───────────────────────────────────────────────
        RectTransform toast = NewRect("Panel_Toast", board);
        AnchorBottom(toast, new Vector2(420f, 62f), new Vector2(0f, 34f));
        AddImage(toast, SpritePill(), MarketBoardPalette.PanelEdge, Image.Type.Sliced);
        RectTransform toastText = NewRect("Text_Toast", toast);
        StretchFull(toastText);
        TMP_Text textToast = AddText(toastText, "", 26f, MarketBoardPalette.TextOnPanel,
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        toast.gameObject.SetActive(false);

        // ══════════════════════════════════════════════════════════════════
        //  NỐI DÂY
        // ══════════════════════════════════════════════════════════════════

        MarketListingCardUI cardComponent = cardPrefab.GetComponent<MarketListingCardUI>();
        MarketCategoryTabUI tabComponent  = tabPrefab.GetComponent<MarketCategoryTabUI>();

        SerializedObject soBoard = new SerializedObject(boardUI);
        SetRef(soBoard, "textTimer",              textTimer);
        SetRef(soBoard, "fillTimer",              fillTimer);
        SetRef(soBoard, "buttonRefresh",          refreshButton);
        SetRef(soBoard, "textRefreshCost",        textRefreshCost);
        SetRef(soBoard, "textRefreshLabel",       textRefreshLabel);
        SetRef(soBoard, "imageRefreshBackground", refreshImage);
        SetRef(soBoard, "textGold",               textGold);
        SetRef(soBoard, "categoryContent",        railContent);
        SetRef(soBoard, "categoryTabPrefab",      tabComponent);
        SetRef(soBoard, "listingContent",         content);
        SetRef(soBoard, "listingCardPrefab",      cardComponent);
        SetRef(soBoard, "listingScroll",          scroll);
        SetRef(soBoard, "panelEmpty",             empty.gameObject);
        SetRef(soBoard, "textEmpty",              textEmpty);
        SetRef(soBoard, "panelToast",             toast.gameObject);
        SetRef(soBoard, "textToast",              textToast);
        SetRef(soBoard, "buttonClose",            closeButton);
        soBoard.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soManager = new SerializedObject(manager);
        SetRef(soManager, "popupRoot",   dim.gameObject);
        SetRef(soManager, "buttonClose", closeButton);

        SerializedProperty dbProp = soManager.FindProperty("marketDatabase");
        if (dbProp != null && dbProp.objectReferenceValue == null)
        {
            dbProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<MarketDatabase_SO>(
                "Assets/_Game/Farm/data/Market/MarketDatabase.asset");
        }
        soManager.ApplyModifiedPropertiesWithoutUndo();

        // MarketPopupUI vẫn còn trên object này để PopupManager / DisableStartupPopupsTool
        // không bị đứt tham chiếu — trỏ popupRoot của nó về đúng Panel_Dim mới
        MarketPopupUI legacyPopup = boardGO.GetComponent<MarketPopupUI>();
        if (legacyPopup != null)
        {
            SerializedObject soLegacy = new SerializedObject(legacyPopup);
            SetRef(soLegacy, "popupRoot", dim.gameObject);
            // btnClose để TRỐNG có chủ đích: MarketPopupUI.Start() gọi RemoveAllListeners()
            // nên nếu cũng trỏ vào nút X thì nó sẽ xoá listener mà MarketBoardUI vừa gắn.
            // Ba script cùng tranh một nút là kiểu lỗi rất khó lần ra.
            SetRef(soLegacy, "btnClose", null);
            soLegacy.ApplyModifiedPropertiesWithoutUndo();
        }

        // Popup phải TẮT khi vào scene, nếu không mở game là chợ đè lên màn hình
        dim.gameObject.SetActive(false);

        EditorUtility.SetDirty(boardGO);
        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PREFAB THẺ HÀNG (2 tầng)
    // ══════════════════════════════════════════════════════════════════════

    private static GameObject BuildCardPrefab()
    {
        EnsureFolder(PrefabFolder);

        RectTransform root = NewRect("MarketListingCard", null);
        root.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image frame = AddImage(root, SpriteCard(), MarketBoardPalette.CardBase, Image.Type.Sliced);
        root.gameObject.AddComponent<CanvasGroup>();
        Button buyButton = root.gameObject.AddComponent<Button>();
        buyButton.targetGraphic = frame;
        MarketListingCardUI cardUI = root.gameObject.AddComponent<MarketListingCardUI>();

        // ── Tầng trên: ô lõm chứa icon + tên + số lượng + giá ─────────────
        // Chiều cao thẻ 250 = 10 (mép) + 158 (ô hàng) + 6 + 66 (tầng người bán) + 10
        RectTransform slot = NewRect("Panel_ItemSlot", root);
        StretchFull(slot, 10f, 82f, 10f, 10f);
        AddImage(slot, SpriteInset(), MarketBoardPalette.CardInset, Image.Type.Sliced);

        RectTransform icon = NewRect("Image_Icon", slot);
        AnchorTop(icon, new Vector2(78f, 78f), new Vector2(0f, 6f));
        Image imageIcon = AddImage(icon, null, Color.white, Image.Type.Simple);
        imageIcon.preserveAspect = true;

        RectTransform nameRT = NewRect("Text_ItemName", slot);
        AnchorTopStretch(nameRT, 34f, 6f, 6f, 86f);
        TMP_Text textItemName = AddText(nameRT, "Tên vật phẩm", 21f, MarketBoardPalette.TextOnCard,
                                        TextAlignmentOptions.Center, FontStyles.Bold);
        textItemName.textWrappingMode = TextWrappingModes.Normal;
        textItemName.enableAutoSizing = true;
        textItemName.fontSizeMin      = 13f;
        textItemName.fontSizeMax      = 21f;

        RectTransform qtyBadge = NewRect("Badge_Quantity", slot);
        AnchorBottomLeft(qtyBadge, new Vector2(54f, 32f), new Vector2(6f, 6f));
        AddImage(qtyBadge, SpritePill(), MarketBoardPalette.PanelBase, Image.Type.Sliced);
        RectTransform qtyText = NewRect("Text_Quantity", qtyBadge);
        StretchFull(qtyText);
        TMP_Text textQuantity = AddText(qtyText, "0", 22f, MarketBoardPalette.TextOnPanel,
                                        TextAlignmentOptions.Center, FontStyles.Bold);

        RectTransform priceRow = NewRect("Row_Price", slot);
        AnchorBottomRight(priceRow, new Vector2(104f, 32f), new Vector2(6f, 6f));
        RectTransform priceIcon = NewRect("Icon_Gold_ChoArt", priceRow);
        AnchorLeft(priceIcon, new Vector2(24f, 24f), 0f);
        AddImage(priceIcon, SpriteCircle(), MarketBoardPalette.ButtonGold, Image.Type.Simple);
        RectTransform priceText = NewRect("Text_Price", priceRow);
        StretchFull(priceText, 28f, 0f, 0f, 0f);
        TMP_Text textPrice = AddText(priceText, "0", 23f, MarketBoardPalette.TextOnCard,
                                     TextAlignmentOptions.MidlineRight, FontStyles.Bold);

        // ── Tầng dưới: người bán ─────────────────────────────────────────
        RectTransform seller = NewRect("Panel_Seller", root);
        AnchorBottomStretch(seller, 66f, 10f, 10f, 10f);
        AddImage(seller, SpriteInset(), MarketBoardPalette.CardSellerBar, Image.Type.Sliced);

        RectTransform avatar = NewRect("Image_SellerAvatar_ChoArt", seller);
        AnchorLeft(avatar, new Vector2(44f, 44f), 6f);
        Image imageAvatar = AddImage(avatar, SpriteCircle(), MarketSellerDirectory.GetAvatarColor(0), Image.Type.Simple);

        RectTransform initial = NewRect("Text_SellerInitial", avatar);
        StretchFull(initial);
        TMP_Text textInitial = AddText(initial, "?", 24f, MarketBoardPalette.TextOnCard,
                                       TextAlignmentOptions.Center, FontStyles.Bold);

        RectTransform sellerName = NewRect("Text_SellerName", seller);
        StretchFull(sellerName, 56f, 4f, 44f, 4f);
        TMP_Text textSellerName = AddText(sellerName, "Người bán", 18f, MarketBoardPalette.TextOnCard,
                                          TextAlignmentOptions.MidlineLeft);
        textSellerName.textWrappingMode = TextWrappingModes.NoWrap;
        textSellerName.overflowMode     = TextOverflowModes.Ellipsis;

        RectTransform levelBadge = NewRect("Badge_SellerLevel", seller);
        AnchorRight(levelBadge, new Vector2(36f, 30f), 4f);
        AddImage(levelBadge, SpritePill(), MarketBoardPalette.BadgePlayer, Image.Type.Sliced);
        RectTransform levelText = NewRect("Text_SellerLevel", levelBadge);
        StretchFull(levelText);
        TMP_Text textSellerLevel = AddText(levelText, "1", 19f, MarketBoardPalette.TextOnPanel,
                                           TextAlignmentOptions.Center, FontStyles.Bold);

        // ── Nhãn ─────────────────────────────────────────────────────────
        RectTransform dealBadge = NewRect("Badge_Deal", root);
        AnchorTopLeft(dealBadge, new Vector2(76f, 36f), new Vector2(-6f, -6f));
        AddImage(dealBadge, SpritePill(), MarketBoardPalette.BadgeDeal, Image.Type.Sliced);
        RectTransform dealText = NewRect("Text_Deal", dealBadge);
        StretchFull(dealText);
        TMP_Text textDeal = AddText(dealText, "-20%", 20f, MarketBoardPalette.TextOnPanel,
                                    TextAlignmentOptions.Center, FontStyles.Bold);
        dealBadge.gameObject.SetActive(false);

        RectTransform playerBadge = NewRect("Badge_Player", root);
        AnchorTopRight(playerBadge, new Vector2(92f, 36f), new Vector2(-6f, -6f));
        AddImage(playerBadge, SpritePill(), MarketBoardPalette.BadgePlayer, Image.Type.Sliced);
        RectTransform playerText = NewRect("Text_Player", playerBadge);
        StretchFull(playerText);
        AddText(playerText, "CỦA BẠN", 17f, MarketBoardPalette.TextOnPanel,
                TextAlignmentOptions.Center, FontStyles.Bold);
        playerBadge.gameObject.SetActive(false);

        RectTransform soldOut = NewRect("Overlay_SoldOut", root);
        StretchFull(soldOut);
        AddImage(soldOut, SpriteCard(), MarketBoardPalette.SoldOutVeil, Image.Type.Sliced);
        RectTransform soldText = NewRect("Text_SoldOut", soldOut);
        StretchFull(soldText);
        AddText(soldText, "ĐÃ BÁN", 30f, MarketBoardPalette.TextOnPanel,
                TextAlignmentOptions.Center, FontStyles.Bold);
        soldOut.gameObject.SetActive(false);

        // ── Nối dây trong prefab ─────────────────────────────────────────
        SerializedObject so = new SerializedObject(cardUI);
        SetRef(so, "imageIcon",         imageIcon);
        SetRef(so, "textItemName",      textItemName);
        SetRef(so, "textQuantity",      textQuantity);
        SetRef(so, "textPrice",         textPrice);
        SetRef(so, "imageSellerAvatar", imageAvatar);
        SetRef(so, "textSellerInitial", textInitial);
        SetRef(so, "textSellerName",    textSellerName);
        SetRef(so, "textSellerLevel",   textSellerLevel);
        SetRef(so, "badgeDeal",         dealBadge.gameObject);
        SetRef(so, "textDeal",          textDeal);
        SetRef(so, "badgePlayer",       playerBadge.gameObject);
        SetRef(so, "overlaySoldOut",    soldOut.gameObject);
        SetRef(so, "imageCardFrame",    frame);
        SetRef(so, "buttonBuy",         buyButton);
        SetRef(so, "canvasGroup",       root.GetComponent<CanvasGroup>());
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, CardPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PREFAB TAB DANH MỤC
    // ══════════════════════════════════════════════════════════════════════

    private static GameObject BuildTabPrefab()
    {
        EnsureFolder(PrefabFolder);

        RectTransform root = NewRect("MarketCategoryTab", null);
        root.sizeDelta = new Vector2(104f, TabHeight);

        Image background = AddImage(root, SpritePill(), MarketBoardPalette.TabIdle, Image.Type.Sliced);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        // Không có LayoutElement thì VerticalLayoutGroup (childControlHeight = true)
        // sẽ bóp mọi tab về chiều cao tối thiểu và cả dải lọc dẹp lép
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = TabHeight;
        layout.minHeight       = TabHeight;

        MarketCategoryTabUI tabUI = root.gameObject.AddComponent<MarketCategoryTabUI>();

        // Ô màu đại diện — CHỖ CHỜ ART: thay sprite là thành icon danh mục thật
        RectTransform accent = NewRect("Image_Accent_ChoArt", root);
        AnchorTop(accent, new Vector2(40f, 40f), new Vector2(0f, 4f));
        Image imageAccent = AddImage(accent, SpriteCircle(), Color.white, Image.Type.Simple);

        RectTransform shortRT = NewRect("Text_Short", accent);
        StretchFull(shortRT);
        TMP_Text textShort = AddText(shortRT, "TC", 19f, MarketBoardPalette.TextOnCard,
                                     TextAlignmentOptions.Center, FontStyles.Bold);

        RectTransform labelRT = NewRect("Text_Label", root);
        AnchorBottomStretch(labelRT, 22f, 4f, 4f, 3f);
        TMP_Text textLabel = AddText(labelRT, "Tất cả", 16f, MarketBoardPalette.TextOnCard,
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        textLabel.enableAutoSizing = true;
        textLabel.fontSizeMin      = 10f;
        textLabel.fontSizeMax      = 16f;

        SerializedObject so = new SerializedObject(tabUI);
        SetRef(so, "imageBackground", background);
        SetRef(so, "imageAccent",     imageAccent);
        SetRef(so, "textShort",       textShort);
        SetRef(so, "textLabel",       textLabel);
        SetRef(so, "button",          button);
        SetRef(so, "scaleTarget",     root);
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, TabPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH
    // ══════════════════════════════════════════════════════════════════════

    private static Sprite SpritePanel()  => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.PanelName);
    private static Sprite SpriteCard()   => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.CardName);
    private static Sprite SpriteInset()  => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.InsetName);
    private static Sprite SpritePill()   => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.PillName);
    private static Sprite SpriteCircle() => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.CircleName);
    private static Sprite SpriteDots()   => MarketBoardSpriteFactory.LoadOrGenerate(MarketBoardSpriteFactory.DotsName);

    private static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolder);
        Directory.CreateDirectory(abs);
        AssetDatabase.Refresh();
    }

    private static GameObject FindInScene(string name)
    {
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];

            Transform found = FindChildRecursive(roots[i].transform, name);
            if (found != null)
                return found.gameObject;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;

            Transform deeper = FindChildRecursive(child, name);
            if (deeper != null)
                return deeper;
        }
        return null;
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void SetRef(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning("[Chợ] Không có field '" + propertyName + "' trên " + so.targetObject.GetType().Name);
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        if (parent != null)
            rt.SetParent(parent, false);

        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image AddImage(RectTransform rt, Sprite sprite, Color color, Image.Type type)
    {
        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color  = color;

        // Sliced/Tiled trên sprite không có border sẽ ra cảnh báo đỏ mỗi frame
        image.type = sprite != null ? type : Image.Type.Simple;
        return image;
    }

    private static TMP_Text AddText(RectTransform rt, string content, float size, Color color,
                                    TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text          = content;
        text.fontSize      = size;
        text.color         = color;
        text.alignment     = alignment;
        text.fontStyle     = style;
        text.raycastTarget = false;   // chữ nuốt mất cú bấm vào nút là lỗi rất khó tìm
        return text;
    }

    // ── Neo ──────────────────────────────────────────────────────────────

    private static void StretchFull(RectTransform rt, float left = 0f, float bottom = 0f,
                                    float right = 0f, float top = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void Center(RectTransform rt, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;
    }

    private static void AnchorTop(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(offset.x, -offset.y);
    }

    private static void AnchorBottom(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(offset.x, offset.y);
    }

    private static void AnchorTopLeft(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(offset.x, -offset.y);
    }

    private static void AnchorTopRight(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(-offset.x, -offset.y);
    }

    private static void AnchorBottomLeft(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = offset;
    }

    private static void AnchorBottomRight(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(-offset.x, offset.y);
    }

    private static void AnchorLeft(RectTransform rt, Vector2 size, float leftOffset)
    {
        rt.anchorMin        = new Vector2(0f, 0.5f);
        rt.anchorMax        = new Vector2(0f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(leftOffset, 0f);
    }

    private static void AnchorRight(RectTransform rt, Vector2 size, float rightOffset)
    {
        rt.anchorMin        = new Vector2(1f, 0.5f);
        rt.anchorMax        = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(1f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(-rightOffset, 0f);
    }

    private static void AnchorTopStretch(RectTransform rt, float height, float left, float right, float top)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.offsetMin        = new Vector2(left, 0f);
        rt.offsetMax        = new Vector2(-right, 0f);
        rt.sizeDelta        = new Vector2(rt.sizeDelta.x, height);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -top);
    }

    private static void AnchorBottomStretch(RectTransform rt, float height, float left, float right, float bottom)
    {
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.offsetMin        = new Vector2(left, 0f);
        rt.offsetMax        = new Vector2(-right, 0f);
        rt.sizeDelta        = new Vector2(rt.sizeDelta.x, height);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, bottom);
    }

    /// <summary>Dải dọc bám mép trái: rộng cố định, cao co giãn theo cha.</summary>
    private static void AnchorLeftStretch(RectTransform rt, float width, float left, float bottom, float topInset)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(left + width, -topInset);
    }
}
#endif
