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
///  DỰNG HIERARCHY BẢNG TIN CHỢ + PREFABS VỚI ASSETS SVG CAO CẤP
/// ══════════════════════════════════════════════════════════════════════════
/// </summary>
public static class MarketBoardUIBuilder
{
    private const string ShopSvgDir     = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";
    private const string PerfectSvgDir  = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";
    private const string BuildingSvgDir = "Assets/Assetsgame/popup/ui_building_svg/generated_sprites";
    private const string RedesignArtDir = "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets";
    private const string MarketArtDir   = "Assets/_Game/Farm/Art/UI_MarketBoard";

    // ── Kích thước Full-Screen ───────────────────────────────────────────
    private const float PopupWidth      = 1420f;
    private const float PopupHeight     = 840f;
    private const float RailWidth       = 192f;
    private const float CardWidth       = 265f;
    private const float CardHeight      = 268f;
    private const float CardSpacing     = 16f;
    private const float TabHeight       = 66f;
    private const float TabSpacing      = 8f;
    private const float ContentTopInset = 176f;

    private const string PrefabFolder   = "Assets/_Game/Prefab/ui/Market";
    private const string CardPrefabPath = PrefabFolder + "/MarketListingCard_Prefab.prefab";
    private const string TabPrefabPath  = PrefabFolder + "/MarketCategoryTab_Prefab.prefab";
    private const string CanvasName     = "Canvas_MarketPopup";

    // ══════════════════════════════════════════════════════════════════════
    //  MENU
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Farm/Chợ/0 · CHẠY TẤT CẢ (sprite → dữ liệu → UI → icon)", false, 0)]
    public static void RunEverything()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Chợ", "Thoát Play Mode trước đã.", "OK");
            return;
        }

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
                ? "Đã dựng lại Bảng Tin Chợ với bộ Frame Gỗ & Tab cao cấp thành công!\n\nPrefab:\n" + CardPrefabPath + "\n" + TabPrefabPath +
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

        MarketManager manager = Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);
        GameObject boardGO;

        if (manager != null)
        {
            boardGO = manager.gameObject;
            boardGO.transform.SetParent(canvasGO.transform, false);
        }
        else
        {
            boardGO = new GameObject("Popup_Board", typeof(RectTransform));
            boardGO.transform.SetParent(canvasGO.transform, false);
            manager = boardGO.AddComponent<MarketManager>();
        }

        // Xoá cây cũ
        for (int i = canvasGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasGO.transform.GetChild(i);
            if (child.gameObject == boardGO)
                continue;
            Object.DestroyImmediate(child.gameObject);
        }

        // ── 1. Panel_Dim: Overlay phủ tối toàn màn hình ───────────────────
        RectTransform dim = NewRect("Panel_Dim", canvasGO.transform);
        StretchFull(dim);
        dim.sizeDelta = new Vector2(3840f, 2160f);
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.04f, 0.08f, 0.03f, 0.75f);
        dimImg.raycastTarget = true;

        boardGO.name = "Popup_Board";
        boardGO.transform.SetParent(dim, false);

        RectTransform board = boardGO.GetComponent<RectTransform>() ?? boardGO.AddComponent<RectTransform>();
        Center(board, new Vector2(PopupWidth, PopupHeight));

        // ── 2. Khung gỗ chính (shop_panel.png) ────────────────────────────
        Image boardImage = Ensure<Image>(boardGO);
        boardImage.sprite = LoadSprite($"{ShopSvgDir}/shop_panel.png");
        boardImage.type   = Image.Type.Sliced;
        boardImage.color  = Color.white;

        // Dọn con cũ
        for (int i = board.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(board.GetChild(i).gameObject);

        MarketBoardUI boardUI = Ensure<MarketBoardUI>(boardGO);

        // ── 3. Tiêu đề Ruy băng 3D ("BẢNG TIN CHỢ") ──────────────────────
        RectTransform bannerRect = NewRect("Header_Banner", board);
        AnchorTop(bannerRect, new Vector2(620f, 126f), new Vector2(0f, 16f));
        Image bannerImg = bannerRect.gameObject.AddComponent<Image>();
        bannerImg.sprite = LoadSprite($"{ShopSvgDir}/shop_banner_ribbon.png");
        bannerImg.type = Image.Type.Sliced;
        bannerImg.color = Color.white;
        bannerImg.raycastTarget = false;

        RectTransform titleText = NewRect("Text_Title", bannerRect);
        StretchFull(titleText);
        TMP_Text txtTitle = AddText(titleText, "BẢNG TIN CHỢ", 44f, new Color(0.36f, 0.20f, 0.09f),
                TextAlignmentOptions.Center, FontStyles.Bold);
        txtTitle.characterSpacing = 4f;
        txtTitle.textWrappingMode = TextWrappingModes.NoWrap;

        // ── 4. Nút Đóng [X] (btn_close.png / btnX.png) ───────────────────
        RectTransform closeRT = NewRect("Btn_Close", board);
        AnchorTopRight(closeRT, new Vector2(86f, 86f), new Vector2(-15f, -15f));
        Image closeImage = closeRT.gameObject.AddComponent<Image>();
        closeImage.sprite = UIStandardSprites.Close                      // WP-D2b: nút đóng chuẩn
                         ?? LoadSprite($"{PerfectSvgDir}/btn_close.png")
                         ?? LoadSprite("Assets/Assetsgame/btnX.png");
        closeImage.type = Image.Type.Sliced;
        closeImage.preserveAspect = true;
        closeImage.color = Color.white;
        Button closeButton = closeRT.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;

        // ── 5. Ví tiền vàng, góc trên trái ───────────────────────────────
        RectTransform goldChip = NewRect("Chip_Gold", board);
        AnchorTopLeft(goldChip, new Vector2(240f, 54f), new Vector2(32f, 96f));
        Image goldChipImg = AddImage(goldChip, LoadSprite($"{ShopSvgDir}/shop_currency_chip.png") ?? LoadSprite($"{PerfectSvgDir}/stepper_box.png"), Color.white, Image.Type.Sliced);

        RectTransform goldIcon = NewRect("Icon_Gold", goldChip);
        AnchorLeft(goldIcon, new Vector2(38f, 38f), 10f);
        Image gIconImg = goldIcon.gameObject.AddComponent<Image>();
        gIconImg.sprite = LoadSprite($"{RedesignArtDir}/Icon_vang.png");
        gIconImg.preserveAspect = true;

        RectTransform goldText = NewRect("Text_Gold", goldChip);
        StretchFull(goldText, 56f, 0f, 14f, 0f);
        TMP_Text textGold = AddText(goldText, "0", 28f, new Color(0.48f, 0.29f, 0.06f),
                                    TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        // ── 6. Đếm ngược làm mới chợ (Progress Bar) ─────────────────────
        RectTransform timerChip = NewRect("Chip_Timer", board);
        AnchorTopRight(timerChip, new Vector2(280f, 52f), new Vector2(310f, 96f));
        Image timerTrackImg = AddImage(timerChip, LoadSprite($"{BuildingSvgDir}/proc_track_bg.png") ?? LoadSprite($"{PerfectSvgDir}/progress_track.png"), Color.white, Image.Type.Sliced);

        RectTransform timerFill = NewRect("Fill_Timer", timerChip);
        StretchFull(timerFill, 4f, 4f, 4f, 4f);
        Image fillTimer = AddImage(timerFill, LoadSprite($"{BuildingSvgDir}/proc_fill_green.png") ?? LoadSprite($"{PerfectSvgDir}/progress_fill.png"), Color.white, Image.Type.Filled);
        fillTimer.fillMethod    = Image.FillMethod.Horizontal;
        fillTimer.fillOrigin    = 0;
        fillTimer.fillAmount    = 1f;
        fillTimer.raycastTarget = false;

        RectTransform timerLabel = NewRect("Text_TimerLabel", timerChip);
        StretchFull(timerLabel, 14f, 0f, 100f, 0f);
        AddText(timerLabel, "Làm mới sau:", 18f, new Color(0.36f, 0.20f, 0.09f),
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        RectTransform timerValue = NewRect("Text_Timer", timerChip);
        StretchFull(timerValue, 160f, 0f, 14f, 0f);
        TMP_Text textTimer = AddText(timerValue, "05:00", 22f, new Color(0.20f, 0.45f, 0.05f),
                                     TextAlignmentOptions.MidlineRight, FontStyles.Bold);

        // ── 7. Nút Làm mới ngay (btn_green.png) ──────────────────────────
        RectTransform refreshRT = NewRect("Btn_Refresh", board);
        AnchorTopRight(refreshRT, new Vector2(260f, 54f), new Vector2(32f, 96f));
        Image refreshImage = AddImage(refreshRT, LoadSprite($"{PerfectSvgDir}/btn_green.png"), Color.white, Image.Type.Sliced);
        Button refreshButton = refreshRT.gameObject.AddComponent<Button>();
        refreshButton.targetGraphic = refreshImage;

        RectTransform refreshIcon = NewRect("Icon_Gold", refreshRT);
        AnchorLeft(refreshIcon, new Vector2(32f, 32f), 10f);
        Image rIconImg = refreshIcon.gameObject.AddComponent<Image>();
        rIconImg.sprite = LoadSprite($"{RedesignArtDir}/Icon_vang.png");
        rIconImg.preserveAspect = true;

        RectTransform refreshCost = NewRect("Text_RefreshCost", refreshRT);
        AnchorLeft(refreshCost, new Vector2(70f, 36f), 46f);
        TMP_Text textRefreshCost = AddText(refreshCost, "150", 24f, Color.white,
                                           TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

        RectTransform refreshLabel = NewRect("Text_RefreshLabel", refreshRT);
        StretchFull(refreshLabel, 116f, 0f, 10f, 0f);
        TMP_Text textRefreshLabel = AddText(refreshLabel, "LÀM MỚI", 20f, Color.white,
                                            TextAlignmentOptions.Midline, FontStyles.Bold);

        // ── 8. Thanh ray dải lọc danh mục (bên trái) ─────────────────────
        RectTransform rail = NewRect("Rail_Categories", board);
        AnchorLeftStretch(rail, RailWidth, 24f, 24f, ContentTopInset);
        AddImage(rail, LoadSprite($"{ShopSvgDir}/shop_card_outer.png") ?? LoadSprite($"{PerfectSvgDir}/inner_panel.png"), Color.white, Image.Type.Sliced);

        RectTransform railContent = NewRect("Content_Categories", rail);
        AnchorTopStretch(railContent, 10f, 6f, 6f, 10f);
        railContent.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup vlg = railContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = TabSpacing;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter railFitter = railContent.gameObject.AddComponent<ContentSizeFitter>();
        railFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── 9. Vùng lưới thẻ hàng (Panel_ListingArea) ────────────────────
        RectTransform listingArea = NewRect("Panel_ListingArea", board);
        StretchFull(listingArea, 24f + RailWidth + 14f, 24f, 24f, ContentTopInset);
        AddImage(listingArea, LoadSprite($"{ShopSvgDir}/shop_card_inner.png") ?? LoadSprite($"{PerfectSvgDir}/inner_panel.png"), Color.white, Image.Type.Sliced);

        RectTransform scrollRT = NewRect("Scroll_Listings", listingArea);
        StretchFull(scrollRT, 8f, 8f, 8f, 8f);
        ScrollRect scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.movementType      = ScrollRect.MovementType.Elastic;
        scroll.elasticity        = 0.1f;
        scroll.scrollSensitivity = 32f;

        RectTransform viewport = NewRect("Viewport", scrollRT);
        StretchFull(viewport);
        viewport.pivot = new Vector2(0.5f, 1f);
        Image vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = NewRect("Content_Listings", viewport);
        AnchorTopStretch(content, 100f, 0f, 0f, 0f);
        content.pivot = new Vector2(0.5f, 1f);

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(CardWidth, CardHeight);
        grid.spacing         = new Vector2(CardSpacing, CardSpacing);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment  = TextAnchor.UpperCenter;
        grid.padding         = new RectOffset(16, 16, 16, 16);

        ContentSizeFitter contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content  = content;

        // ── 10. Trạng thái rỗng & Thông báo ngắn Toast ───────────────────
        RectTransform empty = NewRect("Panel_Empty", listingArea);
        StretchFull(empty, 40f, 40f, 40f, 40f);
        RectTransform emptyText = NewRect("Text_Empty", empty);
        StretchFull(emptyText);
        TMP_Text textEmpty = AddText(emptyText, "CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN",
                                     28f, new Color(0.54f, 0.39f, 0.22f),
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        empty.gameObject.SetActive(false);

        RectTransform toast = NewRect("Panel_Toast", board);
        AnchorBottom(toast, new Vector2(460f, 62f), new Vector2(0f, 36f));
        AddImage(toast, LoadSprite($"{ShopSvgDir}/shop_toast.png") ?? LoadSprite($"{ShopSvgDir}/shop_card_outer.png"), Color.white, Image.Type.Sliced);
        RectTransform toastText = NewRect("Text_Toast", toast);
        StretchFull(toastText);
        TMP_Text textToast = AddText(toastText, "", 24f, Color.white,
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        toast.gameObject.SetActive(false);

        // ── 11. Nối dây Component ────────────────────────────────────────
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

        MarketPopupUI legacyPopup = boardGO.GetComponent<MarketPopupUI>();
        if (legacyPopup != null)
        {
            SerializedObject soLegacy = new SerializedObject(legacyPopup);
            SetRef(soLegacy, "popupRoot", dim.gameObject);
            SetRef(soLegacy, "btnClose",  null);
            soLegacy.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(boardGO.scene);
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PREFAB THẺ HÀNG CHỢ (MarketListingCard_Prefab)
    // ══════════════════════════════════════════════════════════════════════

    private static GameObject BuildCardPrefab()
    {
        EnsureFolder(PrefabFolder);

        RectTransform root = NewRect("MarketListingCard", null);
        root.sizeDelta = new Vector2(CardWidth, CardHeight);

        // Khung ngoài thẻ hàng (shop_card_outer.png)
        Image cardBg = AddImage(root, LoadSprite($"{ShopSvgDir}/shop_card_outer.png"), Color.white, Image.Type.Sliced);

        MarketListingCardUI cardUI = root.gameObject.AddComponent<MarketListingCardUI>();
        root.gameObject.AddComponent<CanvasGroup>();

        // ── 1. Đĩa tròn đựng nông sản/vật phẩm (shop_circle_plate.png) ───
        RectTransform disc = NewRect("Item_Plate", root);
        AnchorTop(disc, new Vector2(104f, 104f), new Vector2(0f, 18f));
        AddImage(disc, LoadSprite($"{ShopSvgDir}/shop_circle_plate.png") ?? LoadSprite($"{PerfectSvgDir}/circle_preview.png"), Color.white, Image.Type.Simple);

        // Icon nông sản
        RectTransform iconRT = NewRect("Image_Icon", disc);
        Center(iconRT, new Vector2(80f, 80f));
        Image imageIcon = iconRT.gameObject.AddComponent<Image>();
        imageIcon.preserveAspect = true;
        imageIcon.raycastTarget  = false;

        // Huy hiệu số lượng (badge_count.png)
        RectTransform qtyBadge = NewRect("Badge_Quantity", disc);
        AnchorBottomRight(qtyBadge, new Vector2(36f, 36f), new Vector2(-4f, -4f));
        AddImage(qtyBadge, LoadSprite($"{PerfectSvgDir}/badge_count.png") ?? LoadSprite($"{BuildingSvgDir}/proc_btn_blue.png"), Color.white, Image.Type.Simple);
        RectTransform qtyText = NewRect("Text_Quantity", qtyBadge);
        StretchFull(qtyText);
        TMP_Text textQuantity = AddText(qtyText, "1", 20f, Color.white,
                                        TextAlignmentOptions.Center, FontStyles.Bold);

        // Tên mặt hàng
        RectTransform nameRT = NewRect("Text_ItemName", root);
        AnchorTop(nameRT, new Vector2(220f, 26f), new Vector2(0f, 126f));
        TMP_Text textItemName = AddText(nameRT, "Lúa mì", 20f, new Color(0.36f, 0.20f, 0.09f),
                                        TextAlignmentOptions.Center, FontStyles.Bold);
        textItemName.textWrappingMode = TextWrappingModes.NoWrap;
        textItemName.overflowMode     = TextOverflowModes.Ellipsis;

        // ── 2. Nút Mua / Giá vàng (shop_btn_buy_gold.png) ────────────────
        RectTransform buyRT = NewRect("Btn_Buy", root);
        AnchorTop(buyRT, new Vector2(210f, 46f), new Vector2(0f, 156f));
        Image buyImg = AddImage(buyRT, LoadSprite($"{ShopSvgDir}/shop_btn_buy_gold.png") ?? LoadSprite($"{PerfectSvgDir}/btn_green.png"), Color.white, Image.Type.Sliced);
        Button buyButton = buyRT.gameObject.AddComponent<Button>();
        buyButton.targetGraphic = buyImg;

        RectTransform priceIcon = NewRect("Icon_Gold", buyRT);
        AnchorLeft(priceIcon, new Vector2(28f, 28f), 14f);
        Image pIconImg = priceIcon.gameObject.AddComponent<Image>();
        pIconImg.sprite = LoadSprite($"{RedesignArtDir}/Icon_vang.png");
        pIconImg.preserveAspect = true;

        RectTransform priceText = NewRect("Text_Price", buyRT);
        StretchFull(priceText, 46f, 0f, 12f, 0f);
        TMP_Text textPrice = AddText(priceText, "120", 23f, Color.white,
                                     TextAlignmentOptions.Midline, FontStyles.Bold);

        // ── 3. Tầng dưới: Người bán (shop_card_inner.png) ────────────────
        RectTransform seller = NewRect("Panel_Seller", root);
        AnchorBottomStretch(seller, 52f, 12f, 12f, 8f);
        AddImage(seller, LoadSprite($"{ShopSvgDir}/shop_card_inner.png") ?? LoadSprite($"{PerfectSvgDir}/inner_panel.png"), Color.white, Image.Type.Sliced);

        RectTransform avatar = NewRect("Image_SellerAvatar", seller);
        AnchorLeft(avatar, new Vector2(40f, 40f), 6f);
        Image imageAvatar = avatar.gameObject.AddComponent<Image>();
        imageAvatar.sprite = LoadSprite($"{MarketArtDir}/avatar_npc_0.png");
        imageAvatar.color  = Color.white;
        imageAvatar.preserveAspect = true;

        RectTransform initial = NewRect("Text_SellerInitial", avatar);
        StretchFull(initial);
        TMP_Text textInitial = AddText(initial, "A", 20f, Color.white,
                                       TextAlignmentOptions.Center, FontStyles.Bold);
        initial.gameObject.SetActive(false);

        RectTransform sellerName = NewRect("Text_SellerName", seller);
        StretchFull(sellerName, 52f, 2f, 40f, 2f);
        TMP_Text textSellerName = AddText(sellerName, "Bác Năm", 17f, new Color(0.36f, 0.20f, 0.09f),
                                          TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        textSellerName.textWrappingMode = TextWrappingModes.NoWrap;
        textSellerName.overflowMode     = TextOverflowModes.Ellipsis;

        RectTransform levelBadge = NewRect("Badge_SellerLevel", seller);
        AnchorRight(levelBadge, new Vector2(34f, 28f), 6f);
        AddImage(levelBadge, LoadSprite($"{ShopSvgDir}/shop_currency_chip.png") ?? LoadSprite($"{PerfectSvgDir}/stepper_box.png"), Color.white, Image.Type.Sliced);
        RectTransform levelText = NewRect("Text_SellerLevel", levelBadge);
        StretchFull(levelText);
        TMP_Text textSellerLevel = AddText(levelText, "3", 16f, new Color(0.48f, 0.29f, 0.06f),
                                           TextAlignmentOptions.Center, FontStyles.Bold);

        // ── 4. Nhãn & Trạng thái (HỜI / CỦA BẠN / ĐÃ BÁN) ────────────────
        RectTransform dealBadge = NewRect("Badge_Deal", root);
        AnchorTopLeft(dealBadge, new Vector2(74f, 34f), new Vector2(-4f, -4f));
        AddImage(dealBadge, LoadSprite($"{BuildingSvgDir}/proc_btn_blue.png") ?? LoadSprite($"{PerfectSvgDir}/btn_green.png"), Color.white, Image.Type.Sliced);
        RectTransform dealText = NewRect("Text_Deal", dealBadge);
        StretchFull(dealText);
        TMP_Text textDeal = AddText(dealText, "-20%", 18f, Color.white,
                                    TextAlignmentOptions.Center, FontStyles.Bold);
        dealBadge.gameObject.SetActive(false);

        RectTransform playerBadge = NewRect("Badge_Player", root);
        AnchorTopRight(playerBadge, new Vector2(90f, 34f), new Vector2(-4f, -4f));
        AddImage(playerBadge, LoadSprite($"{PerfectSvgDir}/btn_green.png"), Color.white, Image.Type.Sliced);
        RectTransform playerText = NewRect("Text_Player", playerBadge);
        StretchFull(playerText);
        AddText(playerText, "CỦA BẠN", 15f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
        playerBadge.gameObject.SetActive(false);

        RectTransform soldOut = NewRect("Overlay_SoldOut", root);
        StretchFull(soldOut);
        AddImage(soldOut, LoadSprite($"{ShopSvgDir}/shop_card_outer.png"), new Color(0.08f, 0.10f, 0.10f, 0.75f), Image.Type.Sliced);
        RectTransform soldText = NewRect("Text_SoldOut", soldOut);
        StretchFull(soldText);
        AddText(soldText, "ĐÃ BÁN", 28f, Color.white,
                TextAlignmentOptions.Center, FontStyles.Bold);
        soldOut.gameObject.SetActive(false);

        // ── Nối dây Component trong Prefab ───────────────────────────────
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
        SetRef(so, "imageCardFrame",    cardBg);
        SetRef(so, "buttonBuy",         buyButton);
        SetRef(so, "canvasGroup",       root.GetComponent<CanvasGroup>());
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, CardPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PREFAB TAB DANH MỤC (Rộng, hiện rõ cả Icon và Tên)
    // ══════════════════════════════════════════════════════════════════════

    private static GameObject BuildTabPrefab()
    {
        EnsureFolder(PrefabFolder);

        RectTransform root = NewRect("MarketCategoryTab", null);
        root.sizeDelta = new Vector2(178f, TabHeight);

        Image background = AddImage(root, LoadSprite($"{PerfectSvgDir}/tab_inactive.png"), Color.white, Image.Type.Sliced);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = TabHeight;
        layout.minHeight       = TabHeight;

        MarketCategoryTabUI tabUI = root.gameObject.AddComponent<MarketCategoryTabUI>();

        // Icon danh mục tròn bên trái
        RectTransform accent = NewRect("Image_Accent", root);
        AnchorLeft(accent, new Vector2(44f, 44f), 10f);
        Image imageAccent = AddImage(accent, LoadSprite($"{MarketArtDir}/tab_icon_0.png"), Color.white, Image.Type.Simple);
        imageAccent.preserveAspect = true;

        RectTransform shortRT = NewRect("Text_Short", accent);
        StretchFull(shortRT);
        TMP_Text textShort = AddText(shortRT, "", 18f, Color.white,
                                     TextAlignmentOptions.Center, FontStyles.Bold);
        shortRT.gameObject.SetActive(false);

        // Tên danh mục to rõ bên phải
        RectTransform labelRT = NewRect("Text_Label", root);
        StretchFull(labelRT, 58f, 0f, 8f, 0f);
        TMP_Text textLabel = AddText(labelRT, "Tất cả", 18f, new Color(0.98f, 0.94f, 0.86f),
                                     TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        textLabel.textWrappingMode = TextWrappingModes.NoWrap;
        textLabel.characterSpacing = 1f;

        SerializedObject so = new SerializedObject(tabUI);
        SetRef(so, "imageBackground",   background);
        SetRef(so, "imageAccent",       imageAccent);
        SetRef(so, "textShort",         textShort);
        SetRef(so, "textLabel",         textLabel);
        SetRef(so, "button",            button);
        SetRef(so, "scaleTarget",       root);
        SetRef(so, "tabActiveSprite",   LoadSprite($"{PerfectSvgDir}/tab_active.png"));
        SetRef(so, "tabInactiveSprite", LoadSprite($"{PerfectSvgDir}/tab_inactive.png"));
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root.gameObject, TabPrefabPath);
        Object.DestroyImmediate(root.gameObject);
        return saved;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH LOAD SPRITE & TẠO RECT
    // ══════════════════════════════════════════════════════════════════════

    private static Sprite LoadSprite(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

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
        image.type   = sprite != null ? type : Image.Type.Simple;
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
        text.raycastTarget = false;
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

    private static void AnchorBottomRight(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(1f, 0f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = new Vector2(-offset.x, offset.y);
    }

    private static void AnchorBottomLeft(RectTransform rt, Vector2 size, Vector2 offset)
    {
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = offset;
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
