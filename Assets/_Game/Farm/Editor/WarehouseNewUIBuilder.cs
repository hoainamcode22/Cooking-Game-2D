#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class WarehouseNewUIBuilder
{
    private const string SpriteFolder = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";
    private const string DesignAssetsFolder = "Assets/thietke/Redesign popup nhiệm vụ game1/Export_Popups_Chon/assets";

    [MenuItem("Tools/Farm/Warehouse/Build New Warehouse UI 100% Mockup")]
    public static void BuildWarehouseUI()
    {
        // 1. Generate crisp UI sprites
        WarehouseSpriteGenerator.GenerateAllSprites();

        // 2. Find WarehousePopupUI in active scene
        WarehousePopupUI warehousePopup = Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (warehousePopup == null)
        {
            Debug.LogError("[WarehouseBuilder] Không tìm thấy WarehousePopupUI trong Scene!");
            return;
        }

        Canvas popupCanvas = warehousePopup.GetComponentInParent<Canvas>();
        if (popupCanvas != null)
        {
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 150;
            EditorUtility.SetDirty(popupCanvas);
        }

        GameObject popupRootGO = warehousePopup.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(popupRootGO, "Build New Warehouse UI");

        // Remove old KhoSkin component if present to avoid runtime override bugs
        KhoSkin oldSkin = warehousePopup.GetComponent<KhoSkin>();
        if (oldSkin != null) Object.DestroyImmediate(oldSkin);

        // Clear old children of popupRoot
        for (int i = popupRootGO.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(popupRootGO.transform.GetChild(i).gameObject);
        }

        // Set RectTransform of popupRoot (1500x880 - Cực to, cực đẹp và thoáng mắt chuẩn Quầy Hàng)
        RectTransform rootRect = popupRootGO.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = popupRootGO.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(1500f, 880f);
        rootRect.anchoredPosition = Vector2.zero;

        // Đảm bảo Popup nằm ĐÈ LÊN TRÊN HUD (sortingOrder 120 như Quầy Hàng)
        Canvas panelCanvas = popupRootGO.GetComponent<Canvas>();
        if (panelCanvas == null) panelCanvas = popupRootGO.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 120;
        if (popupRootGO.GetComponent<GraphicRaycaster>() == null)
            popupRootGO.AddComponent<GraphicRaycaster>();

        // Load TMP Font (FontVo)
        TMP_FontAsset fontVo = LoadFontVo();

        // Load Sprites
        Sprite tabActiveSpr = LoadSprite(SpriteFolder, "tab_active.png");
        Sprite tabInactiveSpr = LoadSprite(SpriteFolder, "tab_inactive.png");
        Sprite innerPanelSpr = LoadSprite(SpriteFolder, "inner_panel.png");
        Sprite slotNormalSpr = LoadSprite(SpriteFolder, "slot_normal.png");
        Sprite slotSelectedSpr = LoadSprite(SpriteFolder, "slot_selected.png");
        Sprite slotEmptySpr = LoadSprite(SpriteFolder, "slot_empty.png");
        Sprite badgeCountSpr = LoadSprite(SpriteFolder, "badge_count.png");
        Sprite stepperBoxSpr = LoadSprite(SpriteFolder, "stepper_box.png");
        Sprite btnGreenSpr = LoadSprite(SpriteFolder, "btn_green.png");
        Sprite btnUpgradeSpr = LoadSprite(SpriteFolder, "btn_upgrade.png");
        Sprite btnMinusSpr = LoadSprite(SpriteFolder, "btn_minus.png");
        Sprite btnPlusSpr = LoadSprite(SpriteFolder, "btn_plus.png");
        Sprite btnMaxSpr = LoadSprite(SpriteFolder, "btn_max.png");
        Sprite circlePreviewSpr = LoadSprite(SpriteFolder, "circle_preview.png");
        Sprite progressTrackSpr = LoadSprite(SpriteFolder, "progress_track.png");
        Sprite progressFillSpr = LoadSprite(SpriteFolder, "progress_fill.png");
        Sprite upgradeBoxSpr = LoadSprite(SpriteFolder, "upgrade_box.png");

        // Tab & Button icons
        Sprite btnCloseSpr = LoadSprite(DesignAssetsFolder, "btnX.png") ?? LoadSprite(SpriteFolder, "btn_close.png");
        Sprite iconLuaSpr = LoadSprite(DesignAssetsFolder, "iconlua.png");
        Sprite iconTrungSpr = LoadSprite(DesignAssetsFolder, "trung.png");
        Sprite iconMonAnSpr = LoadSprite(DesignAssetsFolder, "monan1.png");

        // 2b. LỚP MÀN MỜ TỐI CHE TOÀN MÀN HÌNH (Panel_Dim giống Quầy Hàng & Nhiệm Vụ)
        RectTransform dim = CreateRect(rootRect, "Panel_Dim", new Vector2(3840f, 2160f), Vector2.zero);
        Image dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0.04f, 0.08f, 0.03f, 0.65f);
        dimImg.raycastTarget = true;

        // 3. KHUNG VÁN GỖ ĐỒNG BỘ 100% VỚI POPUP NHIỆM VỤ (1500x880)
        Image rootImg = popupRootGO.GetComponent<Image>();
        if (rootImg != null) Object.DestroyImmediate(rootImg);

        // 3a. Viền gỗ ngoài (Board Border #4A2508)
        RectTransform boardBorder = CreateRect(rootRect, "Board_Border", new Vector2(1516f, 896f), Vector2.zero);
        Image borderImg = boardBorder.gameObject.AddComponent<Image>();
        borderImg.color = TaskPopupDesign.VanGoVien; // #4a2508
        borderImg.raycastTarget = true;

        // 3b. Thân ván gỗ đáy (Board Fill Bottom #7C4E22)
        RectTransform boardFill = CreateRect(rootRect, "Board_Fill_Bottom", new Vector2(1500f, 880f), Vector2.zero);
        Image fillBaseImg = boardFill.gameObject.AddComponent<Image>();
        fillBaseImg.color = TaskPopupDesign.VanGoDuoi; // #7c4e22
        fillBaseImg.raycastTarget = false;

        // 3c. Lớp phủ gradient trên (Board Fill Top #A9743C)
        RectTransform boardTop = CreateRect(rootRect, "Board_Fill_Top", new Vector2(1500f, 880f), Vector2.zero);
        Image fillTopImg = boardTop.gameObject.AddComponent<Image>();
        fillTopImg.color = new Color(TaskPopupDesign.VanGoTren.r, TaskPopupDesign.VanGoTren.g, TaskPopupDesign.VanGoTren.b, 0.45f);
        fillTopImg.raycastTarget = false;

        // 3d. Thớ ván ngang (Repeating wood grain lines)
        for (int i = 1; i <= 6; i++)
        {
            float yPos = 400f - i * 125f;
            RectTransform grainRect = CreateRect(rootRect, $"Board_Grain_{i}", new Vector2(1460f, 5f), new Vector2(0f, yPos));
            Image grainImg = grainRect.gameObject.AddComponent<Image>();
            grainImg.color = TaskPopupDesign.VanGoTho; // #3a1c04 (alpha 0.32)
            grainImg.raycastTarget = false;
        }

        // 3e. 4 Đinh sắt ở 4 góc (Corner Studs: Rim, Base, Shine)
        Vector2[] studPositions = {
            new Vector2(-700f, 385f), new Vector2(700f, 385f),
            new Vector2(-700f, -385f), new Vector2(700f, -385f)
        };
        for (int i = 0; i < studPositions.Length; i++)
        {
            Vector2 pos = studPositions[i];
            // Rim
            RectTransform sRim = CreateRect(rootRect, $"Stud_{i}_Rim", new Vector2(30f, 30f), pos);
            Image rImg = sRim.gameObject.AddComponent<Image>();
            rImg.color = TaskPopupDesign.DinhSatVien; // #5a3210
            rImg.raycastTarget = false;

            // Base
            RectTransform sBase = CreateRect(rootRect, $"Stud_{i}_Base", new Vector2(26f, 26f), pos);
            Image bImg = sBase.gameObject.AddComponent<Image>();
            bImg.color = TaskPopupDesign.DinhSatToi; // #7a4a1a
            bImg.raycastTarget = false;

            // Shine
            RectTransform sShine = CreateRect(rootRect, $"Stud_{i}_Shine", new Vector2(13f, 13f), pos + new Vector2(-2f, 2f));
            Image shImg = sShine.gameObject.AddComponent<Image>();
            shImg.color = TaskPopupDesign.DinhSatSang; // #ffe9b8
            shImg.raycastTarget = false;
        }

        // 4. RIBBON TIÊU ĐỀ ("KHO VẬT PHẨM" DÃN RỘNG NẰM TRÊN 1 HÀNG CHUẨN ĐẸP)
        Sprite bannerRibbonSpr = LoadSprite("Assets/Assetsgame/popup/ui_shop_svg/generated_sprites", "shop_banner_ribbon.png");
        RectTransform bannerRect = CreateRect(rootRect, "Header_Banner", new Vector2(620f, 126f), new Vector2(0f, 445f));
        Image bannerImg = bannerRect.gameObject.AddComponent<Image>();
        bannerImg.sprite = bannerRibbonSpr;
        bannerImg.type = Image.Type.Sliced;
        bannerImg.raycastTarget = false;

        TMP_Text txtTitle = CreateText(bannerRect, "Txt_Title", "KHO VẬT PHẨM", 46f, TaskPopupDesign.ChuTieuDe, new Vector2(0f, 6f), new Vector2(540f, 70f), TextAlignmentOptions.Center, true, fontVo);
        txtTitle.characterSpacing = 4f;
        txtTitle.textWrappingMode = TextWrappingModes.NoWrap;

        // 5. NÚT ĐÓNG [X] (90x90 nhô góc trên-phải)
        RectTransform closeRect = CreateRect(rootRect, "Btn_Close", new Vector2(90f, 90f), new Vector2(705f, 445f));
        Image closeImg = closeRect.gameObject.AddComponent<Image>();
        closeImg.sprite = btnCloseSpr;
        closeImg.type = Image.Type.Simple;
        closeImg.preserveAspect = true;
        Button btnClose = closeRect.gameObject.AddComponent<Button>();

        // 6. LEFT CONTAINER (Khung Chứa Vật Phẩm & 3 Tab)
        RectTransform leftContainer = CreateRect(rootRect, "Left_Container", new Vector2(800f, 740f), new Vector2(-270f, -45f));

        // 6a. Category Tabs Row
        RectTransform tabsRow = CreateRect(leftContainer, "Tabs_Row", new Vector2(800f, 64f), new Vector2(0f, 310f));

        // Tab 1: Nong San (Active)
        RectTransform tab1Rect = CreateRect(tabsRow, "Tab_NongSan", new Vector2(220f, 64f), new Vector2(-245f, 0f));
        Image tab1Img = tab1Rect.gameObject.AddComponent<Image>();
        tab1Img.sprite = tabActiveSpr;
        tab1Img.type = Image.Type.Sliced;
        Button btnTab1 = tab1Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab1Rect, "Tab1_Content", iconLuaSpr, "Nông sản", out TMP_Text txtTab1, new Color(0.36f, 0.20f, 0.09f, 1f), fontVo);

        // Tab 2: Chan Nuoi
        RectTransform tab2Rect = CreateRect(tabsRow, "Tab_ChanNuoi", new Vector2(220f, 64f), new Vector2(0f, -6f));
        Image tab2Img = tab2Rect.gameObject.AddComponent<Image>();
        tab2Img.sprite = tabInactiveSpr;
        tab2Img.type = Image.Type.Sliced;
        Button btnTab2 = tab2Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab2Rect, "Tab2_Content", iconTrungSpr, "Chăn nuôi", out TMP_Text txtTab2, new Color(0.43f, 0.25f, 0.08f, 1f), fontVo);

        // Tab 3: Mon An
        RectTransform tab3Rect = CreateRect(tabsRow, "Tab_MonAn", new Vector2(220f, 64f), new Vector2(245f, -6f));
        Image tab3Img = tab3Rect.gameObject.AddComponent<Image>();
        tab3Img.sprite = tabInactiveSpr;
        tab3Img.type = Image.Type.Sliced;
        Button btnTab3 = tab3Rect.gameObject.AddComponent<Button>();
        CreateTabContent(tab3Rect, "Tab3_Content", iconMonAnSpr, "Món ăn", out TMP_Text txtTab3, new Color(0.43f, 0.25f, 0.08f, 1f), fontVo);

        // 6b. Inner Grid Box (Khung giấy kem)
        RectTransform innerGridBox = CreateRect(leftContainer, "Inner_GridBox", new Vector2(800f, 610f), new Vector2(0f, -20f));
        Image innerGridImg = innerGridBox.gameObject.AddComponent<Image>();
        innerGridImg.sprite = innerPanelSpr;
        innerGridImg.type = Image.Type.Sliced;

        // Item Grid Container (GridLayoutGroup)
        RectTransform gridContainer = CreateRect(innerGridBox, "ItemGrid", new Vector2(760f, 530f), new Vector2(0f, 15f));
        GridLayoutGroup gridLayout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(170f, 155f);
        gridLayout.spacing = new Vector2(18f, 18f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 4;
        gridLayout.padding = new RectOffset(20, 20, 20, 20);
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        // Create Slot Template
        GameObject slotTemplate = CreateSlotTemplate(gridContainer, slotNormalSpr, slotSelectedSpr, slotEmptySpr, badgeCountSpr, fontVo);
        slotTemplate.name = "WarehouseSlot_Template";
        slotTemplate.SetActive(false);

        // 6c. Capacity Bar (Thanh tiến độ sức chứa)
        RectTransform capBarRoot = CreateRect(leftContainer, "CapacityBar_Root", new Vector2(800f, 36f), new Vector2(0f, -350f));

        RectTransform trackRect = CreateRect(capBarRoot, "Progress_Track", new Vector2(600f, 24f), new Vector2(-80f, 0f));
        Image trackImg = trackRect.gameObject.AddComponent<Image>();
        trackImg.sprite = progressTrackSpr;
        trackImg.type = Image.Type.Sliced;

        RectTransform fillRect = CreateRect(trackRect, "Progress_Fill", new Vector2(594f, 20f), Vector2.zero);
        Image fillImg = fillRect.gameObject.AddComponent<Image>();
        fillImg.sprite = progressFillSpr;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.5f;

        TMP_Text txtCapacity = CreateText(capBarRoot, "Txt_Capacity", "12/25 Slot", 22f, new Color(0.48f, 0.29f, 0.06f, 1f), new Vector2(320f, 0f), new Vector2(140f, 36f), TextAlignmentOptions.Right, true, fontVo);

        // 7. RIGHT CONTAINER (Detail Panel: Width 510, Height 740)
        RectTransform rightContainer = CreateRect(rootRect, "Right_DetailPanel", new Vector2(510f, 740f), new Vector2(420f, -45f));
        Image rightBgImg = rightContainer.gameObject.AddComponent<Image>();
        rightBgImg.sprite = innerPanelSpr;
        rightBgImg.type = Image.Type.Sliced;

        // Detail Title
        CreateText(rightContainer, "Txt_DetailHeader", "Chi tiết vật phẩm", 26f, new Color(0.36f, 0.20f, 0.09f, 1f), new Vector2(0f, 320f), new Vector2(470f, 40f), TextAlignmentOptions.Center, true, fontVo);

        // Circle Avatar Preview
        RectTransform circleRect = CreateRect(rightContainer, "Circle_Preview", new Vector2(165f, 165f), new Vector2(0f, 210f));
        Image circleImg = circleRect.gameObject.AddComponent<Image>();
        circleImg.sprite = circlePreviewSpr;
        circleImg.type = Image.Type.Simple;

        RectTransform detailIconRect = CreateRect(circleRect, "Img_DetailIcon", new Vector2(115f, 115f), Vector2.zero);
        Image detailIconImg = detailIconRect.gameObject.AddComponent<Image>();
        detailIconImg.raycastTarget = false;

        // Item Name & Quantity
        TMP_Text txtDetailTitle = CreateText(rightContainer, "Txt_DetailTitle", "Ớt · x28", 30f, new Color(0.36f, 0.20f, 0.09f, 1f), new Vector2(0f, 95f), new Vector2(470f, 48f), TextAlignmentOptions.Center, true, fontVo);

        // Item Description
        TMP_Text txtDetailDesc = CreateText(rightContainer, "Txt_DetailDesc", "Nguyên liệu nông sản tươi ngon. Dùng để nấu ăn tại bếp hoặc bán tại quầy. Giá tham khảo 68 vàng/cái.", 17f, new Color(0.64f, 0.50f, 0.25f, 1f), new Vector2(0f, 25f), new Vector2(460f, 66f), TextAlignmentOptions.Center, false, fontVo);

        // Stepper Group with Stepper Box
        RectTransform stepperBoxRect = CreateRect(rightContainer, "Stepper_Box", new Vector2(460f, 125f), new Vector2(0f, -85f));
        Image stepperBoxImg = stepperBoxRect.gameObject.AddComponent<Image>();
        stepperBoxImg.sprite = stepperBoxSpr;
        stepperBoxImg.type = Image.Type.Sliced;

        CreateText(stepperBoxRect, "Txt_StepperLabel", "Chuyển sang bếp", 20f, new Color(0.36f, 0.20f, 0.09f, 1f), new Vector2(0f, 32f), new Vector2(420f, 34f), TextAlignmentOptions.Center, true, fontVo);

        RectTransform stepperRow = CreateRect(stepperBoxRect, "Stepper_Row", new Vector2(420f, 60f), new Vector2(0f, -20f));

        // Stepper Minus
        RectTransform minusRect = CreateRect(stepperRow, "Btn_Minus", new Vector2(52f, 52f), new Vector2(-140f, 0f));
        Image minusImg = minusRect.gameObject.AddComponent<Image>();
        minusImg.sprite = btnMinusSpr;
        Button btnMinus = minusRect.gameObject.AddComponent<Button>();

        // Stepper Count Text
        TMP_Text txtTransferCount = CreateText(stepperRow, "Txt_TransferCount", "1", 34f, new Color(0.36f, 0.20f, 0.09f, 1f), new Vector2(-45f, 0f), new Vector2(76f, 52f), TextAlignmentOptions.Center, true, fontVo);

        // Stepper Plus
        RectTransform plusRect = CreateRect(stepperRow, "Btn_Plus", new Vector2(52f, 52f), new Vector2(45f, 0f));
        Image plusImg = plusRect.gameObject.AddComponent<Image>();
        plusImg.sprite = btnPlusSpr;
        Button btnPlus = plusRect.gameObject.AddComponent<Button>();

        // Stepper MAX
        RectTransform maxRect = CreateRect(stepperRow, "Btn_Max", new Vector2(90f, 48f), new Vector2(145f, 0f));
        Image maxImg = maxRect.gameObject.AddComponent<Image>();
        maxImg.sprite = btnMaxSpr;
        maxImg.type = Image.Type.Sliced;
        Button btnMax = maxRect.gameObject.AddComponent<Button>();
        CreateText(maxRect, "Txt_MaxLabel", "MAX", 20f, new Color(0.43f, 0.25f, 0.08f, 1f), Vector2.zero, new Vector2(84f, 40f), TextAlignmentOptions.Center, true, fontVo);

        // Main Transfer Button ("CHUYỂN BẾP")
        RectTransform transferBtnRect = CreateRect(rightContainer, "Btn_TransferKitchen", new Vector2(460f, 72f), new Vector2(0f, -200f));
        Image transferBtnImg = transferBtnRect.gameObject.AddComponent<Image>();
        transferBtnImg.sprite = btnGreenSpr;
        transferBtnImg.type = Image.Type.Sliced;
        Button btnTransferKitchen = transferBtnRect.gameObject.AddComponent<Button>();
        CreateText(transferBtnRect, "Txt_TransferLabel", "CHUYỂN BẾP", 27f, Color.white, new Vector2(0f, 2f), new Vector2(440f, 56f), TextAlignmentOptions.Center, true, fontVo);

        // Upgrade Footer Box
        RectTransform upgradeBoxRect = CreateRect(rightContainer, "Upgrade_Box", new Vector2(460f, 78f), new Vector2(0f, -295f));
        Image upgradeBoxImg = upgradeBoxRect.gameObject.AddComponent<Image>();
        upgradeBoxImg.sprite = upgradeBoxSpr;
        upgradeBoxImg.type = Image.Type.Sliced;

        TMP_Text txtUpgradeInfo = CreateText(upgradeBoxRect, "Txt_UpgradeInfo", "Cấp 7 · 175 Slot\n(Đạt cấp tối đa)", 18f, new Color(0.43f, 0.24f, 0.07f, 1f), new Vector2(-60f, 0f), new Vector2(280f, 62f), TextAlignmentOptions.Left, true, fontVo);

        RectTransform upgradeBtnRect = CreateRect(upgradeBoxRect, "Btn_Upgrade", new Vector2(150f, 54f), new Vector2(145f, 0f));
        Image upgradeBtnImg = upgradeBtnRect.gameObject.AddComponent<Image>();
        upgradeBtnImg.sprite = btnUpgradeSpr;
        upgradeBtnImg.type = Image.Type.Sliced;
        Button btnUpgrade = upgradeBtnRect.gameObject.AddComponent<Button>();
        CreateText(upgradeBtnRect, "Txt_UpgradeBtnLabel", "NÂNG CẤP", 19f, new Color(0.48f, 0.29f, 0.06f, 1f), new Vector2(0f, 2f), new Vector2(140f, 44f), TextAlignmentOptions.Center, true, fontVo);

        // 8. Wire Serialized Fields to WarehousePopupUI
        SerializedObject so = new SerializedObject(warehousePopup);
        so.Update();

        SetSerializedProperty(so, "popupRoot", popupRootGO);
        SetSerializedProperty(so, "btnClose", btnClose);

        SetSerializedProperty(so, "btnTabNongSan", btnTab1);
        SetSerializedProperty(so, "btnTabChanNuoi", btnTab2);
        SetSerializedProperty(so, "btnTabMonAn", btnTab3);
        SetSerializedProperty(so, "imgTabNongSan", tab1Img);
        SetSerializedProperty(so, "imgTabChanNuoi", tab2Img);
        SetSerializedProperty(so, "imgTabMonAn", tab3Img);
        SetSerializedProperty(so, "txtTabNongSan", txtTab1);
        SetSerializedProperty(so, "txtTabChanNuoi", txtTab2);
        SetSerializedProperty(so, "txtTabMonAn", txtTab3);
        SetSerializedProperty(so, "rectTabNongSan", tab1Rect);
        SetSerializedProperty(so, "rectTabChanNuoi", tab2Rect);
        SetSerializedProperty(so, "rectTabMonAn", tab3Rect);
        SetSerializedProperty(so, "tabActiveSprite", tabActiveSpr);
        SetSerializedProperty(so, "tabInactiveSprite", tabInactiveSpr);

        SetSerializedProperty(so, "slotPrefab", slotTemplate);
        SetSerializedProperty(so, "itemGridContainer", gridContainer);
        SetSerializedProperty(so, "slotNormalSprite", slotNormalSpr);
        SetSerializedProperty(so, "slotSelectedSprite", slotSelectedSpr);
        SetSerializedProperty(so, "slotEmptySprite", slotEmptySpr);

        SetSerializedProperty(so, "imgCapacityFill", fillImg);
        SetSerializedProperty(so, "txtCapacity", txtCapacity);

        SetSerializedProperty(so, "detailPanelRoot", rightContainer.gameObject);
        SetSerializedProperty(so, "imgDetailIcon", detailIconImg);
        SetSerializedProperty(so, "txtDetailTitle", txtDetailTitle);
        SetSerializedProperty(so, "txtDetailDesc", txtDetailDesc);
        SetSerializedProperty(so, "txtTransferCount", txtTransferCount);
        SetSerializedProperty(so, "btnMinus", btnMinus);
        SetSerializedProperty(so, "btnPlus", btnPlus);
        SetSerializedProperty(so, "btnMax", btnMax);
        SetSerializedProperty(so, "btnTransferKitchen", btnTransferKitchen);

        SetSerializedProperty(so, "txtUpgradeInfo", txtUpgradeInfo);
        SetSerializedProperty(so, "btnUpgrade", btnUpgrade);

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(warehousePopup);
        EditorSceneManager.MarkSceneDirty(warehousePopup.gameObject.scene);

        Debug.Log("[WarehouseBuilder] Đã hoàn tất phóng to UI Kho Vật Phẩm (1440x920), khắc phục text số lượng 100%!");
    }

    private static void CreateTabContent(RectTransform tabRect, string name, Sprite iconSpr, string label, out TMP_Text txtLabel, Color textColor, TMP_FontAsset font)
    {
        RectTransform contentRect = CreateRect(tabRect, name, new Vector2(204f, 54f), new Vector2(0f, 2f));
        contentRect.pivot = new Vector2(0.5f, 0.5f);

        if (iconSpr != null)
        {
            RectTransform iconRect = CreateRect(contentRect, "Img_Icon", new Vector2(38f, 38f), new Vector2(-60f, 0f));
            Image iconImg = iconRect.gameObject.AddComponent<Image>();
            iconImg.sprite = iconSpr;
            iconImg.type = Image.Type.Simple;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            txtLabel = CreateText(contentRect, "Txt_Label", label, 24f, textColor, new Vector2(24f, 0f), new Vector2(136f, 44f), TextAlignmentOptions.Left, true, font);
        }
        else
        {
            txtLabel = CreateText(contentRect, "Txt_Label", label, 24f, textColor, Vector2.zero, new Vector2(190f, 44f), TextAlignmentOptions.Center, true, font);
        }
    }

    private static GameObject CreateSlotTemplate(Transform parent, Sprite normalSpr, Sprite selectedSpr, Sprite emptySpr, Sprite badgeSpr, TMP_FontAsset font)
    {
        GameObject slotGO = new GameObject("WarehouseSlot_Template", typeof(RectTransform));
        RectTransform slotRect = slotGO.GetComponent<RectTransform>();
        slotRect.SetParent(parent, false);
        slotRect.sizeDelta = new Vector2(170f, 155f);

        Image bgCard = slotGO.AddComponent<Image>();
        bgCard.sprite = normalSpr;
        bgCard.type = Image.Type.Sliced;
        Button btn = slotGO.AddComponent<Button>();

        // Item Icon (106x106)
        RectTransform iconRect = CreateRect(slotRect, "Img_Icon", new Vector2(106f, 106f), Vector2.zero);
        Image iconImg = iconRect.gameObject.AddComponent<Image>();
        iconImg.raycastTarget = false;

        // Badge Count (Pill at bottom-right)
        RectTransform badgeRect = CreateRect(slotRect, "Badge_Count", new Vector2(58f, 34f), new Vector2(52f, -56f));
        Image badgeImg = badgeRect.gameObject.AddComponent<Image>();
        badgeImg.sprite = badgeSpr;
        badgeImg.type = Image.Type.Sliced;
        badgeImg.raycastTarget = false;

        TMP_Text txtCount = CreateText(badgeRect, "Txt_Count", "1", 22f, new Color(1f, 0.93f, 0.77f, 1f), Vector2.zero, new Vector2(54f, 30f), TextAlignmentOptions.Center, true, font);

        // WarehouseSlotUI Component
        WarehouseSlotUI slotUI = slotGO.AddComponent<WarehouseSlotUI>();
        SerializedObject soSlot = new SerializedObject(slotUI);
        soSlot.Update();
        SetSerializedProperty(soSlot, "bgCard", bgCard);
        SetSerializedProperty(soSlot, "icon", iconImg);
        SetSerializedProperty(soSlot, "badgeRoot", badgeRect.gameObject);
        SetSerializedProperty(soSlot, "txtSoLuong", txtCount);
        SetSerializedProperty(soSlot, "button", btn);
        SetSerializedProperty(soSlot, "normalSprite", normalSpr);
        SetSerializedProperty(soSlot, "selectedSprite", selectedSpr);
        SetSerializedProperty(soSlot, "emptySprite", emptySpr);
        soSlot.ApplyModifiedProperties();

        return slotGO;
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

    private static void SetSerializedProperty(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }
}
#endif
