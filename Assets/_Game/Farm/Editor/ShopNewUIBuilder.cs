#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ShopNewUIBuilder
{
    private const string ShopSpriteFolder = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";
    private const string WarehouseSpriteFolder = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";
    private const string DesignAssetsFolder = "Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/assets";

    [MenuItem("Tools/Farm/Shop/Build New Shop UI 100% Mockup")]
    public static void BuildShopUI()
    {
        // 1. Sinh các sprite 9-slice
        WarehouseSpriteGenerator.GenerateAllSprites();
        ShopSpriteGenerator.GenerateAllSprites();

        // 2. Tìm ShopManager trong scene
        ShopManager shopManager = Object.FindFirstObjectByType<ShopManager>(FindObjectsInactive.Include);
        if (shopManager == null)
        {
            Debug.LogError("[ShopBuilder] Không tìm thấy ShopManager trong Scene!");
            return;
        }

        Canvas popupCanvas = shopManager.GetComponentInParent<Canvas>();
        if (popupCanvas != null)
        {
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 150;
            EditorUtility.SetDirty(popupCanvas);
        }

        // Tắt ShopSkin cũ nếu có
        ShopSkin oldSkin = shopManager.GetComponent<ShopSkin>();
        if (oldSkin != null) Object.DestroyImmediate(oldSkin);

        // Lưu lại hoặc thu thập danh sách dữ liệu
        List<BaseItemData> savedSeeds = (shopManager.seedList != null && shopManager.seedList.Count > 0)
            ? new List<BaseItemData>(shopManager.seedList)
            : LoadAllAssetsOfType<CropData>();

        List<BaseItemData> savedBuildings = (shopManager.buildingList != null && shopManager.buildingList.Count > 0)
            ? new List<BaseItemData>(shopManager.buildingList)
            : LoadAllBuildings();

        List<BaseItemData> savedDecors = (shopManager.decorList != null && shopManager.decorList.Count > 0)
            ? new List<BaseItemData>(shopManager.decorList)
            : LoadAllDecors();

        GameObject shopPanelGO = shopManager.shopPanel != null ? shopManager.shopPanel : shopManager.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(shopPanelGO, "Build New Shop UI");

        // Xoá các con cũ trong shopPanel
        for (int i = shopPanelGO.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(shopPanelGO.transform.GetChild(i).gameObject);
        }

        // RectTransform của shopPanel (1500x880 chuẩn to ngang Quầy Hàng)
        RectTransform rootRect = shopPanelGO.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = shopPanelGO.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(1500f, 880f);
        rootRect.anchoredPosition = Vector2.zero;

        // Đảm bảo Popup nằm ĐÈ LÊN TRÊN HUD (sortingOrder 120 như Quầy Hàng)
        Canvas panelCanvas = shopPanelGO.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = shopPanelGO.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 120;
        if (shopPanelGO.GetComponent<GraphicRaycaster>() == null)
            shopPanelGO.AddComponent<GraphicRaycaster>();

        // Load TMP Font
        TMP_FontAsset fontVo = LoadFontVo();

        // Load Sprites
        Sprite bannerRibbonSpr = LoadSprite(ShopSpriteFolder, "shop_banner_ribbon.png");
        Sprite tabActiveSpr = LoadSprite(WarehouseSpriteFolder, "tab_active.png");
        Sprite tabInactiveSpr = LoadSprite(WarehouseSpriteFolder, "tab_inactive.png");
        Sprite innerPanelSpr = LoadSprite(WarehouseSpriteFolder, "inner_panel.png");
        Sprite btnCloseSpr = LoadSprite(DesignAssetsFolder, "btnX.png") ?? LoadSprite(WarehouseSpriteFolder, "btn_close.png");
        Sprite btnMinusSpr = LoadSprite(WarehouseSpriteFolder, "btn_minus.png");
        Sprite btnPlusSpr = LoadSprite(WarehouseSpriteFolder, "btn_plus.png");

        Sprite searchBoxSpr = LoadSprite(ShopSpriteFolder, "shop_search_box.png");
        Sprite chipSpr = LoadSprite(ShopSpriteFolder, "shop_currency_chip.png");
        Sprite cardOuterSpr = LoadSprite(ShopSpriteFolder, "shop_card_outer.png");
        Sprite cardInnerSpr = LoadSprite(ShopSpriteFolder, "shop_card_inner.png");
        Sprite circlePlateSpr = LoadSprite(ShopSpriteFolder, "shop_circle_plate.png");
        Sprite buyGoldSpr = LoadSprite(ShopSpriteFolder, "shop_btn_buy_gold.png");
        Sprite buyGemSpr = LoadSprite(ShopSpriteFolder, "shop_btn_buy_gem.png");
        Sprite buyLockedSpr = LoadSprite(ShopSpriteFolder, "shop_btn_buy_locked.png");
        Sprite toastSpr = LoadSprite(ShopSpriteFolder, "shop_toast.png");
        Sprite lockBadgeSpr = LoadSprite(ShopSpriteFolder, "shop_lock_badge.png");

        Sprite iconGoldSpr = LoadSprite(DesignAssetsFolder, "Icon_vang.png") ?? FindSprite("gold", "vang", "coin");
        Sprite iconDiamondSpr = LoadSprite(DesignAssetsFolder, "kimcuong.png") ?? FindSprite("diamond", "gem");

        Sprite iconTabSeedSpr = LoadSprite(DesignAssetsFolder, "iconlua.png");
        Sprite iconTabBuildSpr = LoadSprite(DesignAssetsFolder, "khungchuong.png") ?? LoadSprite(DesignAssetsFolder, "chuongheo.png") ?? iconTabSeedSpr;
        Sprite iconTabDecorSpr = LoadSprite(DesignAssetsFolder, "caythong.png") ?? iconTabSeedSpr;

        // 2b. LỚP MÀN MỜ TỐI CHE TOÀN MÀN HÌNH (Panel_Dim giống Quầy Hàng & Nhiệm Vụ)
        RectTransform dim = CreateRect(rootRect, "Panel_Dim", new Vector2(3840f, 2160f), Vector2.zero);
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.04f, 0.08f, 0.03f, 0.65f);
        dimImg.raycastTarget = true;

        // 3. KHUNG VÁN GỖ ĐỒNG BỘ 100% VỚI KHO VẬT PHẨM (1500x880)
        Image rootImg = shopPanelGO.GetComponent<Image>();
        if (rootImg != null) Object.DestroyImmediate(rootImg);

        // 3a. Viền gỗ ngoài #4A2508
        RectTransform boardBorder = CreateRect(rootRect, "Board_Border", new Vector2(1516f, 896f), Vector2.zero);
        Image borderImg = boardBorder.gameObject.AddComponent<Image>();
        borderImg.color = TaskPopupDesign.VanGoVien;
        borderImg.raycastTarget = true;

        // 3b. Thân ván gỗ đáy #7C4E22
        RectTransform boardFill = CreateRect(rootRect, "Board_Fill_Bottom", new Vector2(1500f, 880f), Vector2.zero);
        Image fillBaseImg = boardFill.gameObject.AddComponent<Image>();
        fillBaseImg.color = TaskPopupDesign.VanGoDuoi;
        fillBaseImg.raycastTarget = false;

        // 3c. Lớp phủ gradient #A9743C
        RectTransform boardTop = CreateRect(rootRect, "Board_Fill_Top", new Vector2(1500f, 880f), Vector2.zero);
        Image fillTopImg = boardTop.gameObject.AddComponent<Image>();
        fillTopImg.color = new Color(TaskPopupDesign.VanGoTren.r, TaskPopupDesign.VanGoTren.g, TaskPopupDesign.VanGoTren.b, 0.45f);
        fillTopImg.raycastTarget = false;

        // 3d. Thớ ván ngang
        for (int i = 1; i <= 6; i++)
        {
            float yPos = 400f - i * 125f;
            RectTransform grainRect = CreateRect(rootRect, $"Board_Grain_{i}", new Vector2(1460f, 5f), new Vector2(0f, yPos));
            Image grainImg = grainRect.gameObject.AddComponent<Image>();
            grainImg.color = TaskPopupDesign.VanGoTho;
            grainImg.raycastTarget = false;
        }

        // 3e. 4 Đinh sắt góc
        Vector2[] studPositions = {
            new Vector2(-700f, 385f), new Vector2(700f, 385f),
            new Vector2(-700f, -385f), new Vector2(700f, -385f)
        };
        for (int i = 0; i < studPositions.Length; i++)
        {
            Vector2 pos = studPositions[i];
            RectTransform sRim = CreateRect(rootRect, $"Stud_{i}_Rim", new Vector2(30f, 30f), pos);
            sRim.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatVien;

            RectTransform sBase = CreateRect(rootRect, $"Stud_{i}_Base", new Vector2(26f, 26f), pos);
            sBase.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatToi;

            RectTransform sShine = CreateRect(rootRect, $"Stud_{i}_Shine", new Vector2(13f, 13f), pos + new Vector2(-2f, 2f));
            sShine.gameObject.AddComponent<Image>().color = TaskPopupDesign.DinhSatSang;
        }

        // 4. RIBBON TIÊU ĐỀ ("CỬA HÀNG" CHUẨN MOCKUP ASSET 100%)
        RectTransform bannerRect = CreateRect(rootRect, "Header_Banner", new Vector2(620f, 126f), new Vector2(0f, 445f));
        Image bannerImg = bannerRect.gameObject.AddComponent<Image>();
        bannerImg.sprite = bannerRibbonSpr;
        bannerImg.type = Image.Type.Sliced;
        bannerImg.raycastTarget = false;

        TMP_Text txtTitle = CreateText(bannerRect, "Txt_Title", "CỬA HÀNG", 46f, TaskPopupDesign.ChuTieuDe, new Vector2(0f, 6f), new Vector2(540f, 70f), TextAlignmentOptions.Center, true, fontVo);
        txtTitle.characterSpacing = 4f;
        txtTitle.textWrappingMode = TextWrappingModes.NoWrap;

        // 5. NÚT ĐÓNG [X] (90x90 nhô góc trên-phải)
        RectTransform closeRect = CreateRect(rootRect, "Btn_Close", new Vector2(90f, 90f), new Vector2(705f, 445f));
        Image closeImg = closeRect.gameObject.AddComponent<Image>();
        closeImg.sprite = btnCloseSpr;
        closeImg.preserveAspect = true;
        Button btnClose = closeRect.gameObject.AddComponent<Button>();

        // 6. 3 TAB DANH MỤC
        RectTransform tabsRow = CreateRect(rootRect, "Tabs_Row", new Vector2(800f, 64f), new Vector2(-260f, 355f));

        // Tab 1: Seed
        RectTransform tab1Rect = CreateRect(tabsRow, "Tab_Seed", new Vector2(240f, 64f), new Vector2(-260f, 0f));
        Image tab1Img = tab1Rect.gameObject.AddComponent<Image>();
        tab1Img.sprite = tabActiveSpr;
        tab1Img.type = Image.Type.Sliced;
        Button btnTab1 = tab1Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab1Rect, "Tab1_Content", iconTabSeedSpr, "Hạt giống", out TMP_Text txtTab1, new Color(0.36f, 0.20f, 0.09f, 1f), fontVo);

        // Tab 2: Building
        RectTransform tab2Rect = CreateRect(tabsRow, "Tab_Building", new Vector2(240f, 64f), new Vector2(0f, -6f));
        Image tab2Img = tab2Rect.gameObject.AddComponent<Image>();
        tab2Img.sprite = tabInactiveSpr;
        tab2Img.type = Image.Type.Sliced;
        Button btnTab2 = tab2Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab2Rect, "Tab2_Content", iconTabBuildSpr, "Công trình", out TMP_Text txtTab2, new Color(0.43f, 0.25f, 0.08f, 1f), fontVo);

        // Tab 3: Decor
        RectTransform tab3Rect = CreateRect(tabsRow, "Tab_Decor", new Vector2(240f, 64f), new Vector2(260f, -6f));
        Image tab3Img = tab3Rect.gameObject.AddComponent<Image>();
        tab3Img.sprite = tabInactiveSpr;
        tab3Img.type = Image.Type.Sliced;
        Button btnTab3 = tab3Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab3Rect, "Tab3_Content", iconTabDecorSpr, "Trang trí", out TMP_Text txtTab3, new Color(0.43f, 0.25f, 0.08f, 1f), fontVo);

        // 7. KHUNG GIẤY KEM CHỨA NỘI DUNG (Inner Container 1340x740)
        RectTransform innerPaper = CreateRect(rootRect, "Inner_PaperContainer", new Vector2(1340f, 740f), new Vector2(0f, -45f));
        Image paperImg = innerPaper.gameObject.AddComponent<Image>();
        paperImg.sprite = innerPanelSpr;
        paperImg.type = Image.Type.Sliced;

        // 7a. Header Bar (Search Input + 2 Currency Chips)
        RectTransform headerBar = CreateRect(innerPaper, "Header_Bar", new Vector2(1280f, 56f), new Vector2(0f, 320f));

        // Search Bar Input
        RectTransform searchRect = CreateRect(headerBar, "SearchBar_Input", new Vector2(760f, 56f), new Vector2(-260f, 0f));
        Image searchBg = searchRect.gameObject.AddComponent<Image>();
        searchBg.sprite = searchBoxSpr;
        searchBg.type = Image.Type.Sliced;

        // Search Icon
        CreateText(searchRect, "Txt_SearchIcon", "", 24f, new Color(0.64f, 0.50f, 0.25f, 1f), new Vector2(-350f, 0f), new Vector2(40f, 40f), TextAlignmentOptions.Center, true, fontVo);

        // Search Text Area
        RectTransform textAreaRect = CreateRect(searchRect, "TextArea", new Vector2(680f, 46f), new Vector2(30f, 0f));
        textAreaRect.gameObject.AddComponent<RectMask2D>();

        TMP_Text placeholderText = CreateText(textAreaRect, "Placeholder", "Tìm vật phẩm...", 22f, new Color(0.69f, 0.55f, 0.36f, 1f), Vector2.zero, new Vector2(680f, 46f), TextAlignmentOptions.Left, false, fontVo);
        TMP_Text searchInputText = CreateText(textAreaRect, "Text", "", 22f, new Color(0.36f, 0.20f, 0.09f, 1f), Vector2.zero, new Vector2(680f, 46f), TextAlignmentOptions.Left, true, fontVo);

        TMP_InputField inputField = searchRect.gameObject.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = searchInputText;
        inputField.placeholder = placeholderText;
        inputField.fontAsset = fontVo;

        // Gold Balance Chip
        RectTransform goldChip = CreateRect(headerBar, "Gold_Chip", new Vector2(240f, 56f), new Vector2(250f, 0f));
        Image goldChipImg = goldChip.gameObject.AddComponent<Image>();
        goldChipImg.sprite = chipSpr;
        goldChipImg.type = Image.Type.Sliced;

        RectTransform gIconRect = CreateRect(goldChip, "Img_GoldIcon", new Vector2(50f, 50f), new Vector2(-75f, 0f));
        Image gIcon = gIconRect.gameObject.AddComponent<Image>();
        gIcon.sprite = iconGoldSpr;
        gIcon.preserveAspect = true;

        TMP_Text txtGold = CreateText(goldChip, "Txt_GoldAmount", "1.250", 24f, new Color(0.48f, 0.29f, 0.06f, 1f), new Vector2(25f, 0f), new Vector2(130f, 40f), TextAlignmentOptions.Left, true, fontVo);

        // Gem Balance Chip
        RectTransform gemChip = CreateRect(headerBar, "Gem_Chip", new Vector2(240f, 56f), new Vector2(510f, 0f));
        Image gemChipImg = gemChip.gameObject.AddComponent<Image>();
        gemChipImg.sprite = chipSpr;
        gemChipImg.type = Image.Type.Sliced;

        RectTransform gmIconRect = CreateRect(gemChip, "Img_GemIcon", new Vector2(40f, 40f), new Vector2(-75f, 0f));
        Image gmIcon = gmIconRect.gameObject.AddComponent<Image>();
        gmIcon.sprite = iconDiamondSpr;
        gmIcon.preserveAspect = true;

        TMP_Text txtGem = CreateText(gemChip, "Txt_GemAmount", "40", 24f, new Color(0.48f, 0.29f, 0.06f, 1f), new Vector2(25f, 0f), new Vector2(130f, 40f), TextAlignmentOptions.Left, true, fontVo);

        // 7b. Scroll View & Grid (1280x620, 4 Cột trải đều chiều ngang chuẩn thiết kế)
        RectTransform scrollViewRect = CreateRect(innerPaper, "Scroll_View", new Vector2(1280f, 620f), new Vector2(0f, -40f));
        ScrollRect scrollRect = scrollViewRect.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        RectTransform viewport = CreateRect(scrollViewRect, "Viewport", new Vector2(1280f, 620f), Vector2.zero);
        Image vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = Color.clear;
        vpImg.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        scrollRect.viewport = viewport;

        RectTransform content = CreateRect(viewport, "Content", new Vector2(1280f, 620f), Vector2.zero);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f); // 0f để Content co giãn đúng bằng 1280px của Viewport
        scrollRect.content = content;

        GridLayoutGroup gridLayout = content.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(296f, 335f);
        gridLayout.spacing = new Vector2(18f, 18f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.padding = new RectOffset(20, 20, 16, 16);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 8. SHOP ITEM TEMPLATE (Đặt dưới rootRect ngoài contentParent)
        GameObject itemTemplate = CreateShopItemTemplate(rootRect, cardOuterSpr, cardInnerSpr, circlePlateSpr, btnMinusSpr, btnPlusSpr, buyGoldSpr, buyGemSpr, buyLockedSpr, iconGoldSpr, iconDiamondSpr, lockBadgeSpr, fontVo);
        itemTemplate.name = "ShopItem_Template";
        itemTemplate.SetActive(false);

        // 9. TOAST NOTIFICATION
        RectTransform toastRect = CreateRect(rootRect, "Toast_Root", new Vector2(340f, 56f), new Vector2(0f, -370f));
        Image toastBg = toastRect.gameObject.AddComponent<Image>();
        toastBg.sprite = toastSpr;
        toastBg.type = Image.Type.Sliced;
        TMP_Text txtToast = CreateText(toastRect, "Txt_Toast", "Đã mua hạt giống!", 22f, Color.white, Vector2.zero, new Vector2(320f, 46f), TextAlignmentOptions.Center, true, fontVo);
        toastRect.gameObject.SetActive(false);

        // 10. Gán Serialized Properties cho ShopManager
        SerializedObject so = new SerializedObject(shopManager);
        so.Update();

        SetSerializedProperty(so, "shopPanel", shopPanelGO);
        SetSerializedProperty(so, "contentParent", content);
        SetSerializedProperty(so, "itemPrefab", itemTemplate);
        SetSerializedProperty(so, "searchBar", inputField);
        SetSerializedProperty(so, "btnClose", btnClose);

        SetSerializedProperty(so, "btnTabSeed", btnTab1);
        SetSerializedProperty(so, "btnTabBuilding", btnTab2);
        SetSerializedProperty(so, "btnTabDecor", btnTab3);
        SetSerializedProperty(so, "imgTabSeed", tab1Img);
        SetSerializedProperty(so, "imgTabBuilding", tab2Img);
        SetSerializedProperty(so, "imgTabDecor", tab3Img);
        SetSerializedProperty(so, "txtTabSeed", txtTab1);
        SetSerializedProperty(so, "txtTabBuilding", txtTab2);
        SetSerializedProperty(so, "txtTabDecor", txtTab3);
        SetSerializedProperty(so, "tabActiveSprite", tabActiveSpr);
        SetSerializedProperty(so, "tabInactiveSprite", tabInactiveSpr);

        SetSerializedProperty(so, "txtGoldBalance", txtGold);
        SetSerializedProperty(so, "txtGemBalance", txtGem);

        SetSerializedProperty(so, "toastRoot", toastRect.gameObject);
        SetSerializedProperty(so, "txtToast", txtToast);

        // Nạp và khôi phục danh sách dữ liệu
        SetListProperty(so, "seedList", savedSeeds);
        SetListProperty(so, "buildingList", savedBuildings);
        SetListProperty(so, "decorList", savedDecors);

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(shopManager);
        EditorSceneManager.MarkSceneDirty(shopManager.gameObject.scene);

        Debug.Log($"[ShopBuilder] Đã hoàn tất dựng lại Cửa Hàng 100% Mockup! (Seeds: {savedSeeds.Count}, Buildings: {savedBuildings.Count}, Decors: {savedDecors.Count})");
    }

    private static GameObject CreateShopItemTemplate(Transform parent, Sprite cardOuterSpr, Sprite cardInnerSpr, Sprite circlePlateSpr, Sprite minusSpr, Sprite plusSpr, Sprite buyGoldSpr, Sprite buyGemSpr, Sprite buyLockedSpr, Sprite iconGoldSpr, Sprite iconDiamondSpr, Sprite lockBadgeSpr, TMP_FontAsset font)
    {
        GameObject cardGO = new GameObject("ShopItem_Template", typeof(RectTransform));
        RectTransform cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.SetParent(parent, false);
        cardRect.sizeDelta = new Vector2(296f, 335f);

        Image cardOuterImg = cardGO.AddComponent<Image>();
        cardOuterImg.sprite = cardOuterSpr;
        cardOuterImg.type = Image.Type.Sliced;
        cardOuterImg.raycastTarget = true;

        // Inner Paper
        RectTransform innerRect = CreateRect(cardRect, "Card_Inner", new Vector2(276f, 248f), new Vector2(0f, 33f));
        Image innerImg = innerRect.gameObject.AddComponent<Image>();
        innerImg.sprite = cardInnerSpr;
        innerImg.type = Image.Type.Sliced;
        innerImg.raycastTarget = false;

        // Item Name (2 lines fixed 44px)
        TMP_Text txtName = CreateText(innerRect, "Txt_Name", "Hạt lúa", 20f, new Color(0.36f, 0.20f, 0.09f, 1f), new Vector2(0f, 88f), new Vector2(250f, 44f), TextAlignmentOptions.Center, true, font);

        // Circle Plate Avatar
        RectTransform circleRect = CreateRect(innerRect, "Circle_Plate", new Vector2(112f, 112f), new Vector2(0f, 10f));
        Image circleImg = circleRect.gameObject.AddComponent<Image>();
        circleImg.sprite = circlePlateSpr;
        circleImg.raycastTarget = false;

        // Item Icon (84x84)
        RectTransform iconRect = CreateRect(circleRect, "Img_Icon", new Vector2(84f, 84f), Vector2.zero);
        Image iconImg = iconRect.gameObject.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Stepper Row (Minus, Count, Plus)
        RectTransform stepperRow = CreateRect(innerRect, "Stepper_Row", new Vector2(200f, 38f), new Vector2(0f, -74f));

        RectTransform minusBtnRect = CreateRect(stepperRow, "Btn_Minus", new Vector2(36f, 36f), new Vector2(-60f, 0f));
        Image minusImg = minusBtnRect.gameObject.AddComponent<Image>();
        minusImg.sprite = minusSpr;
        Button btnMinus = minusBtnRect.gameObject.AddComponent<Button>();
        minusBtnRect.gameObject.AddComponent<UIDragScrollForwarder>();

        TMP_Text txtQuantity = CreateText(stepperRow, "Txt_Quantity", "1", 24f, new Color(0.36f, 0.20f, 0.09f, 1f), Vector2.zero, new Vector2(50f, 36f), TextAlignmentOptions.Center, true, font);

        RectTransform plusBtnRect = CreateRect(stepperRow, "Btn_Plus", new Vector2(36f, 36f), new Vector2(60f, 0f));
        Image plusImg = plusBtnRect.gameObject.AddComponent<Image>();
        plusImg.sprite = plusSpr;
        Button btnPlus = plusBtnRect.gameObject.AddComponent<Button>();
        plusBtnRect.gameObject.AddComponent<UIDragScrollForwarder>();

        // Placeable Note ("Mua 1 cái / lần")
        TMP_Text txtPlaceableNote = CreateText(innerRect, "Txt_PlaceableNote", "Mua 1 cái / lần", 16f, new Color(0.64f, 0.50f, 0.25f, 1f), new Vector2(0f, -74f), new Vector2(240f, 36f), TextAlignmentOptions.Center, true, font);
        txtPlaceableNote.gameObject.SetActive(false);

        // Buy Button = Price Button (52px high, 276px wide with HorizontalLayoutGroup spacing)
        RectTransform buyBtnRect = CreateRect(cardRect, "Btn_Buy", new Vector2(276f, 52f), new Vector2(0f, -132f));
        Image buyBgImg = buyBtnRect.gameObject.AddComponent<Image>();
        buyBgImg.sprite = buyGoldSpr;
        buyBgImg.type = Image.Type.Sliced;
        Button btnBuy = buyBtnRect.gameObject.AddComponent<Button>();
        buyBtnRect.gameObject.AddComponent<UIDragScrollForwarder>();

        HorizontalLayoutGroup buyLayout = buyBtnRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        buyLayout.childAlignment = TextAnchor.MiddleCenter;
        buyLayout.spacing = 14f;
        buyLayout.childControlWidth = false;
        buyLayout.childControlHeight = false;
        buyLayout.childForceExpandWidth = false;
        buyLayout.childForceExpandHeight = false;

        // Currency Icon inside Buy Button
        RectTransform currIconRect = CreateRect(buyBtnRect, "Img_CurrencyIcon", new Vector2(34f, 34f), Vector2.zero);
        Image currIconImg = currIconRect.gameObject.AddComponent<Image>();
        currIconImg.sprite = iconGoldSpr;
        currIconImg.preserveAspect = true;
        currIconImg.raycastTarget = false;

        // Price Text inside Buy Button
        TMP_Text txtPrice = CreateText(buyBtnRect, "Txt_Price", "10", 24f, Color.white, Vector2.zero, new Vector2(130f, 38f), TextAlignmentOptions.Left, true, font);

        // Lock Overlay Root
        RectTransform lockOverlay = CreateRect(cardRect, "Lock_Overlay", new Vector2(296f, 335f), Vector2.zero);
        Image lockBg = lockOverlay.gameObject.AddComponent<Image>();
        lockBg.color = new Color(0.24f, 0.16f, 0.06f, 0.65f); // rgba(62,40,16,0.65)
        lockBg.raycastTarget = true;
        lockOverlay.gameObject.AddComponent<UIDragScrollForwarder>();

        // Lock Badge Circle
        RectTransform lockBadgeRect = CreateRect(lockOverlay, "Lock_Badge", new Vector2(58f, 58f), new Vector2(0f, 25f));
        Image lockBadgeImg = lockBadgeRect.gameObject.AddComponent<Image>();
        lockBadgeImg.sprite = lockBadgeSpr;
        lockBadgeImg.raycastTarget = false;

        // Lock Text ("Mở ở cấp X")
        TMP_Text lockLvlText = CreateText(lockOverlay, "Txt_LockLevel", "Mở ở cấp 6", 22f, new Color(1f, 0.91f, 0.74f, 1f), new Vector2(0f, -25f), new Vector2(260f, 36f), TextAlignmentOptions.Center, true, font);

        lockOverlay.gameObject.SetActive(false);

        // Attach ShopItemUI
        ShopItemUI shopItemUI = cardGO.AddComponent<ShopItemUI>();
        shopItemUI.txtName = txtName;
        shopItemUI.imgIcon = iconImg;
        shopItemUI.imgCirclePlate = circleImg;
        shopItemUI.stepperRoot = stepperRow.gameObject;
        shopItemUI.txtQuantity = txtQuantity;
        shopItemUI.btnMinus = btnMinus;
        shopItemUI.btnPlus = btnPlus;
        shopItemUI.placeableNote = txtPlaceableNote.gameObject;

        shopItemUI.btnBuy = btnBuy;
        shopItemUI.imgBuyBackground = buyBgImg;
        shopItemUI.imgCurrencyIcon = currIconImg;
        shopItemUI.txtPrice = txtPrice;

        shopItemUI.lockOverlayRoot = lockOverlay.gameObject;
        shopItemUI.lockLevelText = lockLvlText;

        shopItemUI.iconGold = iconGoldSpr;
        shopItemUI.iconDiamond = iconDiamondSpr;
        shopItemUI.btnBuyGoldSprite = buyGoldSpr;
        shopItemUI.btnBuyGemSprite = buyGemSpr;
        shopItemUI.btnBuyLockedSprite = buyLockedSpr;

        return cardGO;
    }

    private static void CreateTabContent(RectTransform tabRect, string name, Sprite iconSpr, string label, out TMP_Text txtLabel, Color textColor, TMP_FontAsset font)
    {
        RectTransform contentRect = CreateRect(tabRect, name, new Vector2(220f, 54f), new Vector2(0f, 2f));
        contentRect.pivot = new Vector2(0.5f, 0.5f);

        if (iconSpr != null)
        {
            RectTransform iconRect = CreateRect(contentRect, "Img_Icon", new Vector2(38f, 38f), new Vector2(-65f, 0f));
            Image iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.sprite = iconSpr;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            txtLabel = CreateText(contentRect, "Txt_Label", label, 24f, textColor, new Vector2(24f, 0f), new Vector2(150f, 44f), TextAlignmentOptions.Left, true, font);
        }
        else
        {
            txtLabel = CreateText(contentRect, "Txt_Label", label, 24f, textColor, Vector2.zero, new Vector2(200f, 44f), TextAlignmentOptions.Center, true, font);
        }
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, Color color, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align, bool isBold, TMP_FontAsset font)
    {
        RectTransform rect = CreateRect(parent, name, size, anchoredPos);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = align;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        if (isBold) label.fontStyle = FontStyles.Bold;
        return label;
    }

    private static TMP_FontAsset LoadFontVo()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/FontVo");
        if (font == null)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Resources/Fonts/FontVo.asset");
        return font;
    }

    private static Sprite LoadSprite(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName).Replace("\\", "/");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite FindSprite(params string[] keywords)
    {
        foreach (string kw in keywords)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{kw} t:Sprite"))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sp != null) return sp;
            }
        }
        return null;
    }

    private static List<BaseItemData> LoadAllAssetsOfType<T>() where T : BaseItemData
    {
        List<BaseItemData> list = new List<BaseItemData>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && !list.Contains(asset))
                list.Add(asset);
        }
        return list;
    }

    private static List<BaseItemData> LoadAllBuildings()
    {
        List<BaseItemData> list = new List<BaseItemData>();
        string[] guids = AssetDatabase.FindAssets("t:PlaceableItemData");
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && !asset.name.ToLower().Contains("decor") && !list.Contains(asset))
                list.Add(asset);
        }
        return list;
    }

    private static List<BaseItemData> LoadAllDecors()
    {
        List<BaseItemData> list = new List<BaseItemData>();
        string[] guids = AssetDatabase.FindAssets("t:PlaceableItemData");
        foreach (string guid in guids)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlaceableItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && asset.name.ToLower().Contains("decor") && !list.Contains(asset))
                list.Add(asset);
        }
        return list;
    }

    private static void SetSerializedProperty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static void SetListProperty(SerializedObject so, string propName, List<BaseItemData> list)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null && prop.isArray)
        {
            prop.ClearArray();
            for (int i = 0; i < list.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
        }
    }
}
#endif
