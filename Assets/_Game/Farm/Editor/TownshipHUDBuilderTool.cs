#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using FarmGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TownshipHUDBuilderTool
{
    private const string SpritesFolder = "Assets/Assetsgame/popup/ui_township_exact_bases/generated_sprites";
    private const string DesignAssetsFolder = "Assets/Assetsgame";

    [MenuItem("Tools/Farm/HUD/1. Cập Nhật Logic & Nối Dây HUD (Giữ Nguyên Vị Trí Kéo Tay)")]
    public static void WireAndFixExistingHUD()
    {
        // 1. Tự động sinh/cập nhật sprites 9-slice nếu thiếu
        TownshipHUDSpriteGenerator.GenerateAllSprites();

        // 2. Tìm Canvas_HUD trong Scene
        GameObject canvasGO = null;
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i].name == "Canvas_HUD" || allCanvases[i].name == "Canvas")
            {
                canvasGO = allCanvases[i].gameObject;
                break;
            }
        }

        if (canvasGO == null)
        {
            Debug.LogError("[TownshipHUD] Không tìm thấy Canvas_HUD trong Scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(canvasGO, "Wire Township HUD");

        // 3. Xoá Image/Blocker trên chính Canvas_HUD nếu bị dính (nguyên nhân gây khoá/đơ màn hình)
        Image rootImg = canvasGO.GetComponent<Image>();
        if (rootImg != null)
        {
            Object.DestroyImmediate(rootImg);
            Debug.Log("[TownshipHUD] Đã gỡ bỏ Image cản Raycast trên Canvas_HUD.");
        }
        var rootBlocker = canvasGO.GetComponent<UIRaycastBlocker>();
        if (rootBlocker != null)
        {
            Object.DestroyImmediate(rootBlocker);
        }

        // 4. Tìm các thành phần bên trong Canvas_HUD theo tên
        Transform[] allTransforms = canvasGO.GetComponentsInChildren<Transform>(true);
        Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (!map.ContainsKey(allTransforms[i].name))
                map.Add(allTransforms[i].name, allTransforms[i].gameObject);
        }

        TownshipHUDController controller = canvasGO.GetComponent<TownshipHUDController>();
        if (controller == null) controller = canvasGO.AddComponent<TownshipHUDController>();

        // Avatar & EXP
        if (map.TryGetValue("Avatar_Button", out GameObject goAvBtn)) controller.btnAvatar = goAvBtn.GetComponent<Button>();
        if (map.TryGetValue("Img_PlayerAvatar", out GameObject goAvImg)) controller.imgAvatar = goAvImg.GetComponent<Image>();
        if (map.TryGetValue("EXP_Fill", out GameObject goExpFill)) controller.imgExpFill = goExpFill.GetComponent<Image>();
        if (map.TryGetValue("Txt_EXP_Value", out GameObject goExpTxt)) controller.txtExp = goExpTxt.GetComponent<TMP_Text>();
        if (map.TryGetValue("Txt_Level_Number", out GameObject goLvlTxt)) controller.txtLevel = goLvlTxt.GetComponent<TMP_Text>();

        // Currency & Settings
        if (map.TryGetValue("Txt_Gold_Value", out GameObject goGoldTxt)) controller.txtGold = goGoldTxt.GetComponent<TMP_Text>();
        if (map.TryGetValue("Txt_Diamond_Value", out GameObject goDiaTxt)) controller.txtDiamond = goDiaTxt.GetComponent<TMP_Text>();
        if (map.TryGetValue("Btn_Add_Gold", out GameObject goAddGold)) controller.btnAddGold = goAddGold.GetComponent<Button>();
        if (map.TryGetValue("Btn_Add_Diamond", out GameObject goAddDia)) controller.btnAddDiamond = goAddDia.GetComponent<Button>();
        if (map.TryGetValue("Btn_Settings", out GameObject goSettings)) controller.btnSettings = goSettings.GetComponent<Button>();

        // 4 Navigation Tabs
        if (map.TryGetValue("Tab_Shop", out GameObject goTabShop)) controller.btnTabShop = goTabShop.GetComponent<Button>();
        if (map.TryGetValue("Tab_Warehouse", out GameObject goTabWh)) controller.btnTabWarehouse = goTabWh.GetComponent<Button>();
        if (map.TryGetValue("Tab_Market", out GameObject goTabMarket)) controller.btnTabMarket = goTabMarket.GetComponent<Button>();
        if (map.TryGetValue("Tab_Cooking", out GameObject goTabCooking)) controller.btnTabCooking = goTabCooking.GetComponent<Button>();

        // Tutorial Target for Shop
        if (controller.btnTabShop != null)
        {
            var tt = controller.btnTabShop.GetComponent<TutorialTarget>() ?? controller.btnTabShop.gameObject.AddComponent<TutorialTarget>();
            tt.targetID = "btn_store";
        }

        // Mission Button & Quick Widget
        if (map.TryGetValue("Btn_Mission_Toggle", out GameObject goMisBtn))
            controller.btnMission = goMisBtn.GetComponent<Button>();
        else if (map.TryGetValue("Btn_Mission_Card", out GameObject goMisCard))
            controller.btnMission = goMisCard.GetComponent<Button>();

        if (map.TryGetValue("Badge_Alert", out GameObject goBadge))
        {
            controller.goMissionBadge = goBadge;
            goBadge.SetActive(true);
        }

        if (map.TryGetValue("Quick_Mission_Widget", out GameObject goWidget))
        {
            controller.goMissionWidget = goWidget;
            // Ẩn mặc định khung nhiệm vụ khi mới vào game (chỉ hiện nút kẹp giấy)
            goWidget.SetActive(false);
        }

        if (map.TryGetValue("Item_Icon", out GameObject goItemIcon)) controller.imgMissionItem = goItemIcon.GetComponent<Image>();
        if (map.TryGetValue("Txt_Widget_Title", out GameObject goMisTitle)) controller.txtMissionTitle = goMisTitle.GetComponent<TMP_Text>();
        if (map.TryGetValue("Txt_Widget_Desc", out GameObject goMisDesc)) controller.txtMissionDesc = goMisDesc.GetComponent<TMP_Text>();
        if (map.TryGetValue("Progress_Fill", out GameObject goProgFill)) controller.imgMissionProgressFill = goProgFill.GetComponent<Image>();
        if (map.TryGetValue("Txt_Progress", out GameObject goProgTxt)) controller.txtMissionProgress = goProgTxt.GetComponent<TMP_Text>();
        if (map.TryGetValue("Btn_Go", out GameObject goBtnGo)) controller.btnMissionGo = goBtnGo.GetComponent<Button>();

        controller.btnTabMission = controller.btnMission;
        controller.btnTabMap = controller.btnTabCooking;

        // Reset toàn bộ FarmInputLock để giải phóng màn hình bị đơ
        FarmInputLock.ResetAll();

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvasGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(canvasGO.scene);

        Debug.Log("[TownshipHUD] Đã cập nhật logic, nối dây thành công và GIỮ NGUYÊN 100% vị trí bạn đã kéo tay!");
    }

    [MenuItem("Tools/Farm/HUD/9. [Tuỳ Chọn] Dựng Lại HUD Toàn Bộ Từ Đầu (Sẽ Reset Vị Trí)")]
    public static void BuildHUD()
    {
        // 1. Tự động sinh/cập nhật sprites 9-slice sắc nét
        TownshipHUDSpriteGenerator.GenerateAllSprites();

        // 2. Tìm Canvas_HUD trong active scene
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO = null;

        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allCanvases.Length; i++)
        {
            if (allCanvases[i].name == "Canvas_HUD" || allCanvases[i].name == "Canvas")
            {
                canvas = allCanvases[i];
                canvasGO = canvas.gameObject;
                break;
            }
        }

        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas_HUD");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        canvasGO.name = "Canvas_HUD";
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = false;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            canvasGO.AddComponent<GraphicRaycaster>();

        Undo.RegisterFullObjectHierarchyUndo(canvasGO, "Build Township HUD");

        // 3. Xoá triệt để tất cả các cụm HUD cũ bị vẽ đỏ / lỗi thời trong Scene
        CleanupLegacyHUDObjects(canvasGO);

        // 4. Load Resources & Fonts
        TMP_FontAsset fontVo = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");
        if (fontVo == null)
            fontVo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Resources/Fonts/Baloo2 SDF.asset");

        Sprite avatarBaseSpr = LoadSprite(SpritesFolder, "hud_avatar_base.png");
        Sprite currencyBaseSpr = LoadSprite(SpritesFolder, "hud_currency_base.png");
        Sprite expFillSpr = LoadSprite(SpritesFolder, "hud_exp_fill.png");
        Sprite bottomTabBaseSpr = LoadSprite(SpritesFolder, "hud_bottom_tab_base.png");
        Sprite levelStarSpr = LoadSprite(SpritesFolder, "hud_level_star.png");
        Sprite btnPlusSpr = LoadSprite(SpritesFolder, "hud_btn_plus.png");

        Sprite playerAvatarSpr = LoadSprite(DesignAssetsFolder, "avata_player.png") ?? FindSprite("avatar", "player");
        Sprite goldIconSpr = LoadSprite(DesignAssetsFolder, "Icon_vang.png") ?? FindSprite("gold", "vang", "coin");
        Sprite diamondIconSpr = LoadSprite(DesignAssetsFolder, "kimcuong.png") ?? LoadSprite(DesignAssetsFolder, "kimcuong-removebg-preview.png");
        Sprite settingsIconSpr = LoadSprite("Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG", "setting.png") ?? FindSprite("setting", "gear");

        Sprite shopIconSpr = LoadSprite(DesignAssetsFolder, "iconmarrket.png") ?? LoadSprite(DesignAssetsFolder, "khung_HatGiong.png");
        Sprite warehouseIconSpr = LoadSprite(DesignAssetsFolder, "khungkho-removebg-preview.png") ?? LoadSprite(DesignAssetsFolder, "khungchuong.png");
        Sprite marketIconSpr = LoadSprite("Assets/Assetsgame/Nhà", "BangdonHang.png") ?? LoadSprite(DesignAssetsFolder, "anh4nguoidan.png") ?? LoadSprite(DesignAssetsFolder, "khung_HatGiong.png") ?? FindSprite("bangdon", "market", "order");
        Sprite cookingIconSpr = LoadSprite(DesignAssetsFolder, "ngoinhacoooking.png") ?? LoadSprite("Assets/Anh", "ngoinhacoooking.png") ?? LoadSprite("Assets/Assetsgame/AssestCoooking", "nhahang.png") ?? FindSprite("cooking", "cook");
        Sprite missionIconSpr = LoadSprite(DesignAssetsFolder, "KhungLich.png") ?? LoadSprite(DesignAssetsFolder, "icon_lich.png") ?? LoadSprite(DesignAssetsFolder, "lichicon.png") ?? FindSprite("lich", "mission");

        // 5. CỤM 1: TOP-LEFT (Avatar độc lập bo góc + EXP Bar với Sao Cấp Độ - Scale x1.5-x2)
        RectTransform topLeftRoot = CreateRect(canvasGO.transform, "TopLeft_Township_HUD", new Vector2(650f, 155f), new Vector2(25f, -25f));
        SetAnchor(topLeftRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        // 5a. Avatar Frame (Click mở Profile) - Size 140x140
        RectTransform avatarBtnRect = CreateRect(topLeftRoot, "Avatar_Button", new Vector2(140f, 140f), new Vector2(70f, -72f));
        Image avatarFrameImg = avatarBtnRect.gameObject.AddComponent<Image>();
        avatarFrameImg.sprite = avatarBaseSpr;
        avatarFrameImg.type = Image.Type.Sliced;
        Button btnAvatar = avatarBtnRect.gameObject.AddComponent<Button>();

        // Avatar Image mask inside
        RectTransform avatarMaskRect = CreateRect(avatarBtnRect, "Avatar_Mask", new Vector2(118f, 118f), new Vector2(0f, 3f));
        Image maskImg = avatarMaskRect.gameObject.AddComponent<Image>();
        maskImg.sprite = avatarBaseSpr;
        maskImg.type = Image.Type.Sliced;
        Mask mask = avatarMaskRect.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform avatarPicRect = CreateRect(avatarMaskRect, "Img_PlayerAvatar", new Vector2(125f, 125f), Vector2.zero);
        Image avatarPicImg = avatarPicRect.gameObject.AddComponent<Image>();
        avatarPicImg.sprite = playerAvatarSpr;
        avatarPicImg.preserveAspect = true;
        avatarPicImg.raycastTarget = false;

        // 5b. EXP Bar Capsule (Nằm ngang cạnh Avatar) - Size 390x56
        RectTransform expBarContainer = CreateRect(topLeftRoot, "EXP_Bar_Container", new Vector2(390f, 56f), new Vector2(350f, -60f));
        Image expBgImg = expBarContainer.gameObject.AddComponent<Image>();
        expBgImg.sprite = currencyBaseSpr;
        expBgImg.type = Image.Type.Sliced;

        // EXP Fill Bar
        RectTransform expFillRect = CreateRect(expBarContainer, "EXP_Fill", new Vector2(376f, 44f), new Vector2(0f, 0f));
        Image expFillImg = expFillRect.gameObject.AddComponent<Image>();
        expFillImg.sprite = expFillSpr;
        expFillImg.type = Image.Type.Filled;
        expFillImg.fillMethod = Image.FillMethod.Horizontal;
        expFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        expFillImg.fillAmount = 0.65f;
        expFillImg.raycastTarget = false;

        // EXP Text
        TMP_Text txtExp = CreateText(expBarContainer, "Txt_EXP_Value", "4 680 / 6 200", 21f, Color.white, new Vector2(12f, 0f), new Vector2(260f, 36f), TextAlignmentOptions.Center, true, fontVo);
        txtExp.enableVertexGradient = true;
        txtExp.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.9f, 0.95f, 1f), new Color(0.9f, 0.95f, 1f));

        // Level Star Badge (Nằm đè mép trái của EXP bar) - Size 90x90
        RectTransform starRect = CreateRect(expBarContainer, "Level_Star_Badge", new Vector2(90f, 90f), new Vector2(-185f, 2f));
        Image starImg = starRect.gameObject.AddComponent<Image>();
        starImg.sprite = levelStarSpr;
        starImg.preserveAspect = true;
        starImg.raycastTarget = false;

        // Level Number Text
        TMP_Text txtLevel = CreateText(starRect, "Txt_Level_Number", "30", 34f, Color.white, new Vector2(0f, 0f), new Vector2(65f, 65f), TextAlignmentOptions.Center, true, fontVo);
        txtLevel.fontStyle = FontStyles.Bold;

        // 6. CỤM 2: TOP-RIGHT (2 Capsule Vàng & Kim Cương + Nút Cài Đặt - Scale x1.5-x2)
        RectTransform topRightRoot = CreateRect(canvasGO.transform, "TopRight_Township_HUD", new Vector2(760f, 100f), new Vector2(-25f, -25f));
        SetAnchor(topRightRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));

        // 6a. Settings Button - Size 78x78
        RectTransform settingsRect = CreateRect(topRightRoot, "Btn_Settings", new Vector2(78f, 78f), new Vector2(-39f, -44f));
        Image settingsImg = settingsRect.gameObject.AddComponent<Image>();
        settingsImg.sprite = settingsIconSpr;
        settingsImg.preserveAspect = true;
        Button btnSettings = settingsRect.gameObject.AddComponent<Button>();

        // 6b. Diamond Capsule - Size 235x58
        RectTransform diamondContainer = CreateRect(topRightRoot, "Diamond_Container", new Vector2(235f, 58f), new Vector2(-220f, -44f));
        Image diamondBgImg = diamondContainer.gameObject.AddComponent<Image>();
        diamondBgImg.sprite = currencyBaseSpr;
        diamondBgImg.type = Image.Type.Sliced;

        // Diamond Icon
        RectTransform diamondIconRect = CreateRect(diamondContainer, "Icon_Diamond", new Vector2(62f, 62f), new Vector2(-94f, 0f));
        Image diamondIconImg = diamondIconRect.gameObject.AddComponent<Image>();
        diamondIconImg.sprite = diamondIconSpr;
        diamondIconImg.preserveAspect = true;
        diamondIconImg.raycastTarget = false;

        // Diamond Text
        TMP_Text txtDiamond = CreateText(diamondContainer, "Txt_Diamond_Value", "320", 26f, Color.white, new Vector2(2f, 0f), new Vector2(115f, 42f), TextAlignmentOptions.Center, true, fontVo);

        // Plus Diamond Button
        RectTransform addDiamondRect = CreateRect(diamondContainer, "Btn_Add_Diamond", new Vector2(48f, 48f), new Vector2(94f, 0f));
        Image addDiamondImg = addDiamondRect.gameObject.AddComponent<Image>();
        addDiamondImg.sprite = btnPlusSpr;
        addDiamondImg.preserveAspect = true;
        Button btnAddDiamond = addDiamondRect.gameObject.AddComponent<Button>();

        // 6c. Gold Capsule - Size 270x58
        RectTransform goldContainer = CreateRect(topRightRoot, "Gold_Container", new Vector2(270f, 58f), new Vector2(-500f, -44f));
        Image goldBgImg = goldContainer.gameObject.AddComponent<Image>();
        goldBgImg.sprite = currencyBaseSpr;
        goldBgImg.type = Image.Type.Sliced;

        // Gold Icon
        RectTransform goldIconRect = CreateRect(goldContainer, "Icon_Gold", new Vector2(65f, 65f), new Vector2(-110f, 0f));
        Image goldIconImg = goldIconRect.gameObject.AddComponent<Image>();
        goldIconImg.sprite = goldIconSpr;
        goldIconImg.preserveAspect = true;
        goldIconImg.raycastTarget = false;

        // Gold Text
        TMP_Text txtGold = CreateText(goldContainer, "Txt_Gold_Value", "12 450", 26f, Color.white, new Vector2(-2f, 0f), new Vector2(135f, 42f), TextAlignmentOptions.Center, true, fontVo);

        // Plus Gold Button
        RectTransform addGoldRect = CreateRect(goldContainer, "Btn_Add_Gold", new Vector2(48f, 48f), new Vector2(110f, 0f));
        Image addGoldImg = addGoldRect.gameObject.AddComponent<Image>();
        addGoldImg.sprite = btnPlusSpr;
        addGoldImg.preserveAspect = true;
        Button btnAddGold = addGoldRect.gameObject.AddComponent<Button>();

        // 7. CỤM 3: NÚT NHIỆM VỤ & BẢNG QUICK MISSION WIDGET BÊN TRÁI (Khớp 100% Mockup 2 ảnh)
        Sprite clipboardIconSpr = LoadSprite("Assets/_Game/Farm/Art/UI_OrderBoard", "ob_clipboard.png") ?? LoadSprite(DesignAssetsFolder, "icon_lich.png") ?? LoadSprite(DesignAssetsFolder, "KhungLich.png") ?? FindSprite("clipboard", "lich");
        Sprite badgeAlertSpr = LoadSprite(SpritesFolder, "hud_badge_alert.png");
        Sprite arrowLeftSpr = LoadSprite(SpritesFolder, "hud_arrow_left.png");
        Sprite calloutPanelSpr = LoadSprite(SpritesFolder, "hud_callout_panel.png") ?? LoadSprite("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites", "panel_outer.png");
        Sprite slotBgSpr = LoadSprite("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites", "slot_normal.png") ?? LoadSprite(SpritesFolder, "hud_avatar_base.png");
        Sprite eggIconSpr = LoadSprite("Assets/thietke/Redesign popup nhi?m v? game/UnifiedTaskPopup_Export", "trung.png") ?? LoadSprite(DesignAssetsFolder, "Khung_NhanThuong.png") ?? FindSprite("trung", "egg");
        Sprite progressTrackSpr = LoadSprite("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites", "progress_track.png") ?? LoadSprite(SpritesFolder, "hud_currency_base.png");
        Sprite progressFillSpr = LoadSprite("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites", "progress_fill.png") ?? LoadSprite(SpritesFolder, "hud_exp_fill.png");
        Sprite btnGreenSpr = LoadSprite("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites", "btn_green.png") ?? LoadSprite("Assets/Assetsgame/popup/ui_shop_svg/generated_sprites", "shop_btn_buy_gold.png");

        RectTransform missionRoot = CreateRect(canvasGO.transform, "Left_Mission_Root", new Vector2(520f, 200f), new Vector2(25f, -280f));
        SetAnchor(missionRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));

        // 7a. Nút Bấm Nhiệm Vụ (Icon Clipboard + Badge Đỏ !) - Size 105x105
        RectTransform missionBtnRect = CreateRect(missionRoot, "Btn_Mission_Toggle", new Vector2(105f, 105f), new Vector2(52.5f, -52.5f));
        Image missionBtnBg = missionBtnRect.gameObject.AddComponent<Image>();
        missionBtnBg.sprite = bottomTabBaseSpr;
        missionBtnBg.type = Image.Type.Sliced;
        Button btnMission = missionBtnRect.gameObject.AddComponent<Button>();

        // Icon Clipboard
        RectTransform clipIconRect = CreateRect(missionBtnRect, "Icon_Clipboard", new Vector2(62f, 62f), new Vector2(0f, 0f));
        Image clipIconImg = clipIconRect.gameObject.AddComponent<Image>();
        clipIconImg.sprite = clipboardIconSpr;
        clipIconImg.preserveAspect = true;
        clipIconImg.raycastTarget = false;

        // Red Badge (!) ở góc trên phải nút
        RectTransform alertBadgeRect = CreateRect(missionBtnRect, "Badge_Alert", new Vector2(38f, 38f), new Vector2(40f, 40f));
        Image alertBadgeImg = alertBadgeRect.gameObject.AddComponent<Image>();
        alertBadgeImg.sprite = badgeAlertSpr;
        alertBadgeImg.preserveAspect = true;
        alertBadgeImg.raycastTarget = false;

        // 7b. Quick Mission Widget (Bảng Callout bên phải) - Size 360x170
        RectTransform widgetRect = CreateRect(missionRoot, "Quick_Mission_Widget", new Vector2(360f, 170f), new Vector2(300f, -52.5f));
        Image widgetBg = widgetRect.gameObject.AddComponent<Image>();
        widgetBg.sprite = calloutPanelSpr;
        widgetBg.type = Image.Type.Sliced;

        // Mũi tên chỉ sang trái (Connecting Arrow)
        RectTransform arrowRect = CreateRect(widgetRect, "Arrow_Pointer", new Vector2(20f, 24f), new Vector2(-188f, 0f));
        Image arrowImg = arrowRect.gameObject.AddComponent<Image>();
        arrowImg.sprite = arrowLeftSpr;
        arrowImg.preserveAspect = true;
        arrowImg.raycastTarget = false;

        // Tiêu đề "NHIỆM VỤ MỚI"
        TMP_Text txtWidgetTitle = CreateText(widgetRect, "Txt_Widget_Title", "NHIỆM VỤ MỚI", 20f, new Color(0.35f, 0.20f, 0.08f, 1f), new Vector2(0f, 52f), new Vector2(320f, 30f), TextAlignmentOptions.Center, true, fontVo);

        // Divider Line gạch ngang
        RectTransform divRect = CreateRect(widgetRect, "Divider_Line", new Vector2(310f, 2f), new Vector2(0f, 36f));
        Image divImg = divRect.gameObject.AddComponent<Image>();
        divImg.color = new Color(0.89f, 0.80f, 0.66f, 0.8f);
        divImg.raycastTarget = false;

        // Ô hiển thị item (Left Slot) - Size 62x62
        RectTransform slotRect = CreateRect(widgetRect, "Item_Slot", new Vector2(62f, 62f), new Vector2(-116f, -10f));
        Image slotImg = slotRect.gameObject.AddComponent<Image>();
        slotImg.sprite = slotBgSpr;
        slotImg.type = Image.Type.Sliced;

        // Item Icon (Egg) inside Slot
        RectTransform itemIconRect = CreateRect(slotRect, "Item_Icon", new Vector2(48f, 48f), Vector2.zero);
        Image itemIconImg = itemIconRect.gameObject.AddComponent<Image>();
        itemIconImg.sprite = eggIconSpr;
        itemIconImg.preserveAspect = true;
        itemIconImg.raycastTarget = false;

        // Text mô tả nhiệm vụ (Right Description)
        TMP_Text txtWidgetDesc = CreateText(widgetRect, "Txt_Widget_Desc", "Thu thập 4 quả trứng gà tươi", 16f, new Color(0.29f, 0.18f, 0.10f, 1f), new Vector2(24f, -8f), new Vector2(200f, 44f), TextAlignmentOptions.Left, true, fontVo);
        txtWidgetDesc.textWrappingMode = TextWrappingModes.Normal;

        // Thanh tiến độ Capsule (Bottom Left Progress Bar) - Size 160x34
        RectTransform progBarRect = CreateRect(widgetRect, "Progress_Bar_Track", new Vector2(160f, 34f), new Vector2(-68f, -54f));
        Image progTrackImg = progBarRect.gameObject.AddComponent<Image>();
        progTrackImg.sprite = progressTrackSpr;
        progTrackImg.type = Image.Type.Sliced;

        // Progress Fill
        RectTransform progFillRect = CreateRect(progBarRect, "Progress_Fill", new Vector2(152f, 26f), Vector2.zero);
        Image progFillImg = progFillRect.gameObject.AddComponent<Image>();
        progFillImg.sprite = progressFillSpr;
        progFillImg.type = Image.Type.Filled;
        progFillImg.fillMethod = Image.FillMethod.Horizontal;
        progFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        progFillImg.fillAmount = 0.75f;
        progFillImg.raycastTarget = false;

        // Progress Text "3 / 4"
        TMP_Text txtWidgetProgress = CreateText(progBarRect, "Txt_Progress", "3 / 4", 17f, Color.white, Vector2.zero, new Vector2(140f, 28f), TextAlignmentOptions.Center, true, fontVo);

        // Nút "ĐẾN" (Bottom Right Button) - Size 96x38
        RectTransform btnGoRect = CreateRect(widgetRect, "Btn_Go", new Vector2(96f, 38f), new Vector2(108f, -54f));
        Image btnGoImg = btnGoRect.gameObject.AddComponent<Image>();
        btnGoImg.sprite = btnGreenSpr;
        btnGoImg.type = Image.Type.Sliced;
        Button btnMissionGo = btnGoRect.gameObject.AddComponent<Button>();

        TMP_Text txtBtnGo = CreateText(btnGoRect, "Txt_Btn_Go", "ĐẾN", 18f, Color.white, new Vector2(0f, 1f), new Vector2(80f, 30f), TextAlignmentOptions.Center, true, fontVo);

        // 8. CỤM 4: BOTTOM-LEFT (4 Tab Điều Hướng Dàn Ngang: CỬA HÀNG, KHO, BẢNG TIN CHỢ, NẤU ĂN)
        RectTransform navGroupRoot = CreateRect(canvasGO.transform, "BottomLeft_Nav_Group", new Vector2(630f, 160f), new Vector2(25f, 25f));
        SetAnchor(navGroupRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        float cardWidth = 132f;
        float cardHeight = 132f;
        float spacing = 16f;
        float startX = cardWidth / 2f;

        // Tab 1: CỬA HÀNG
        Button btnTabShop = CreateNavTab(navGroupRoot, "Tab_Shop", new Vector2(startX + 0 * (cardWidth + spacing), cardHeight / 2f),
            cardWidth, cardHeight, bottomTabBaseSpr, shopIconSpr, "CỬA HÀNG", fontVo);
        
        // Gắn TutorialTarget cho Tab_Shop để tương thích các bước Tutorial chỉ vào Cửa Hàng
        var ttShop = btnTabShop.GetComponent<TutorialTarget>() ?? btnTabShop.gameObject.AddComponent<TutorialTarget>();
        ttShop.targetID = "btn_store";

        // Tab 2: KHO
        Button btnTabWarehouse = CreateNavTab(navGroupRoot, "Tab_Warehouse", new Vector2(startX + 1 * (cardWidth + spacing), cardHeight / 2f),
            cardWidth, cardHeight, bottomTabBaseSpr, warehouseIconSpr, "KHO", fontVo);

        // Tab 3: BẢNG TIN CHỢ
        Button btnTabMarket = CreateNavTab(navGroupRoot, "Tab_Market", new Vector2(startX + 2 * (cardWidth + spacing), cardHeight / 2f),
            cardWidth, cardHeight, bottomTabBaseSpr, marketIconSpr, "BẢNG TIN CHỢ", fontVo);

        // Tab 4: NẤU ĂN
        Button btnTabCooking = CreateNavTab(navGroupRoot, "Tab_Cooking", new Vector2(startX + 3 * (cardWidth + spacing), cardHeight / 2f),
            cardWidth, cardHeight, bottomTabBaseSpr, cookingIconSpr, "NẤU ĂN", fontVo);

        // 9. GẮN VÀ KẾT NỐI TOWNSHIP HUD CONTROLLER
        TownshipHUDController controller = canvasGO.GetComponent<TownshipHUDController>();
        if (controller == null) controller = canvasGO.AddComponent<TownshipHUDController>();

        controller.btnAvatar = btnAvatar;
        controller.imgAvatar = avatarPicImg;
        controller.imgExpFill = expFillImg;
        controller.txtExp = txtExp;
        controller.txtLevel = txtLevel;

        controller.txtGold = txtGold;
        controller.txtDiamond = txtDiamond;
        controller.btnAddGold = btnAddGold;
        controller.btnAddDiamond = btnAddDiamond;
        controller.btnSettings = btnSettings;

        controller.btnMission = btnMission;
        controller.goMissionBadge = alertBadgeRect.gameObject;
        controller.goMissionWidget = widgetRect.gameObject;
        controller.imgMissionItem = itemIconImg;
        controller.txtMissionTitle = txtWidgetTitle;
        controller.txtMissionDesc = txtWidgetDesc;
        controller.imgMissionProgressFill = progFillImg;
        controller.txtMissionProgress = txtWidgetProgress;
        controller.btnMissionGo = btnMissionGo;

        controller.btnTabShop = btnTabShop;
        controller.btnTabWarehouse = btnTabWarehouse;
        controller.btnTabMarket = btnTabMarket;
        controller.btnTabCooking = btnTabCooking;
        controller.btnTabMission = btnMission;
        controller.btnTabMap = btnTabCooking;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(canvasGO);

        Debug.Log("[TownshipHUD] Dựng hoàn tất HUD Township 100% Chuẩn Mockup 2 ảnh: Nút Nhiệm Vụ + Quick Mission Widget + 4 Tab Điều Hướng!");
    }

    // ── Helper Xoá Sạch UI Cũ ──────────────────────────────────────────────────

    private static void CleanupLegacyHUDObjects(GameObject canvasGO)
    {
        // 1. Danh sách tên các cụm GameObject UI cũ cần xoá hoặc ẩn triệt để
        string[] oldNames = new string[]
        {
            "SafeArea", "TOPBAR", "LeftTopBar", "RightTopBar", "GoldBox", "GemBox",
            "TopLeft_Anchor", "TopRight_Anchor", "HomeMenu", "Btn_Home", "Panel_Items",
            "Panel_Vang", "Panel_KimCuong", "Gold_Wood", "Gold_Background", "Diamond_Background",
            "Avatar_Container", "Img_AvatarFrame", "Avatar_Frame", "AvatarProfile", "AvatarProfileUI", "JudgeAvatar",
            "EXP_Background", "Canvas_HUD_Moi", "MissionHudButton", "Btn_EditMode",
            "BottomLeft_Nav_Group", "TopLeft_Township_HUD", "TopRight_Township_HUD",
            "Left_Mission_Root", "Left_Mission_Button", "Quick_Mission_Widget"
        };

        // Quét toàn bộ children trong Canvas_HUD (kể cả nested Canvas)
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < canvasGO.transform.childCount; i++)
        {
            Transform child = canvasGO.transform.GetChild(i);
            
            // Xoá nested Canvas cũ bên trong Canvas_HUD (nơi chứa HomeMenu cũ)
            if (child.name == "Canvas" && child.gameObject != canvasGO)
            {
                toDestroy.Add(child.gameObject);
                continue;
            }

            foreach (string legacyName in oldNames)
            {
                if (child.name.Equals(legacyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    toDestroy.Add(child.gameObject);
                    break;
                }
            }
        }

        foreach (var obj in toDestroy)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }

        // Quét đệ quy toàn Scene để dọn sạch nếu có object cũ nằm ngoài
        foreach (string legacyName in oldNames)
        {
            var oldObjs = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in oldObjs)
            {
                if (go != null && go.name.Equals(legacyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    if (go.transform.IsChildOf(canvasGO.transform) || go.name == "Canvas_HUD_Moi")
                    {
                        Object.DestroyImmediate(go);
                    }
                }
            }
        }

        // Gỡ bỏ HUDController và HomeMenuController cũ nếu còn gắn để tránh xung đột
        var oldHud = canvasGO.GetComponent<HUDController>();
        if (oldHud != null) Object.DestroyImmediate(oldHud);

        var oldHome = canvasGO.GetComponent<HomeMenuController>();
        if (oldHome != null) Object.DestroyImmediate(oldHome);
    }

    // ── Helper Dựng Tab Điều Hướng ───────────────────────────────────────────

    private static Button CreateNavTab(Transform parent, string name, Vector2 pos, float width, float height, Sprite baseSpr, Sprite iconSpr, string labelText, TMP_FontAsset font)
    {
        RectTransform tabRect = CreateRect(parent, name, new Vector2(width, height), pos);
        Image bgImg = tabRect.gameObject.AddComponent<Image>();
        bgImg.sprite = baseSpr;
        bgImg.type = Image.Type.Sliced;
        Button btn = tabRect.gameObject.AddComponent<Button>();

        // Icon inside card
        RectTransform iconRect = CreateRect(tabRect, "Icon", new Vector2(74f, 74f), new Vector2(0f, 14f));
        Image iconImg = iconRect.gameObject.AddComponent<Image>();
        iconImg.sprite = iconSpr;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Label Text
        CreateText(tabRect, "Txt_Label", labelText, 19f, new Color(0.35f, 0.20f, 0.08f, 1f), new Vector2(0f, -44f), new Vector2(122f, 30f), TextAlignmentOptions.Center, true, font);

        return btn;
    }

    // ── UI Builder Helpers ───────────────────────────────────────────────────

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
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
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (isBold) label.fontStyle = FontStyles.Bold;
        return label;
    }

    private static Sprite LoadSprite(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName).Replace("\\", "/");
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite FindSprite(params string[] keywords)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (var guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid).ToLower();
            foreach (var kw in keywords)
            {
                if (p.Contains(kw.ToLower()))
                    return AssetDatabase.LoadAssetAtPath<Sprite>(p);
            }
        }
        return null;
    }
}
#endif

