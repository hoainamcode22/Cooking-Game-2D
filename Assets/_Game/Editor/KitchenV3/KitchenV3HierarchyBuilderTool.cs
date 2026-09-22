using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class KitchenV3HierarchyBuilderTool
    {
        private const string V3SpritesPath = "Assets/Export_Kitchen_UI_V3/Sprites";
        private const string BordersFile = "Assets/Export_Kitchen_UI_V3/BORDERS.txt";

        [MenuItem("Tools/Farm Game/Kitchen V3: Dựng khung tĩnh (100% Hierarchy Contract)")]
        public static void BuildStaticHierarchyContract()
        {
            // 1. Rename and disable old root
            var oldCanvas = GameObject.Find("Canvas_KitchenV2");
            if (oldCanvas != null && oldCanvas.name != "_OLD_Canvas_KitchenV2")
            {
                Undo.RecordObject(oldCanvas, "Disable old Canvas_KitchenV2");
                oldCanvas.name = "_OLD_Canvas_KitchenV2";
                oldCanvas.SetActive(false);
            }

            // Also disable any other leftover Canvas
            var oldV3Canvas = GameObject.Find("Canvas_CozyKitchen_V3");
            if (oldV3Canvas != null)
            {
                oldV3Canvas.SetActive(false);
            }

            // 2. Create target Canvas_KitchenV2 (Matches name contract for BindExistingHierarchy)
            var canvasGo = new GameObject("Canvas_KitchenV2");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas_KitchenV2 (V3 Static Hierarchy)");

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            TMP_FontAsset fontAsset = FindCozyFont();

            // Load V3 or fallback sprites
            Sprite spStageBg = LoadV3Sprite("stage_kitchen_bg") ?? LoadV3Sprite("master_background_stage") ?? LoadV3Sprite("kitchen_background_main");
            Sprite spWoodFrame = LoadV3Sprite("panel_wood_frame") ?? LoadV3Sprite("frame_recipe_book");
            Sprite spPaperCream = LoadV3Sprite("panel_paper_cream") ?? LoadV3Sprite("frame_dish_card");
            Sprite spChalkboard = LoadV3Sprite("chalkboard_special") ?? LoadV3Sprite("frame_chalkboard");
            Sprite spTray = LoadV3Sprite("panel_tray") ?? LoadV3Sprite("shelf_ingredients");
            Sprite spBtnCook = LoadV3Sprite("btn_cook_orange") ?? LoadV3Sprite("btn_cook_pill");
            Sprite spCardNormal = LoadV3Sprite("card_dish_normal") ?? LoadV3Sprite("dish_card_row_bg");
            Sprite spCardSelected = LoadV3Sprite("card_dish_selected");
            Sprite spBtnBack = LoadV3Sprite("btn_back_to_farm") ?? LoadV3Sprite("btn_back_farm");
            Sprite spBtnSettings = LoadV3Sprite("btn_settings_gear");

            Sprite spStove = LoadV3Sprite("stove_gas") ?? LoadV3Sprite("gas_stove_pan");
            Sprite spPan = LoadV3Sprite("pan_empty");
            Sprite spPanFood = LoadV3Sprite("pan_food_cabbage_fried_rice") ?? LoadV3Sprite("fried_rice_pan");
            Sprite spOven = LoadV3Sprite("oven_brick") ?? LoadV3Sprite("stone_pizza_oven");
            Sprite spAutoCook = LoadV3Sprite("toggle_autocook_on") ?? LoadV3Sprite("btn_auto_cook_badge");
            Sprite spStorage = LoadV3Sprite("box_storage") ?? LoadV3Sprite("storage_basket");

            // Load 12-frame Cat Chef
            Sprite[] catFrames = new Sprite[12];
            for (int i = 1; i <= 12; i++)
            {
                catFrames[i - 1] = LoadV3Sprite($"cat_chef_jump_{i:D2}") ?? LoadV3Sprite($"cat_chef_frame_{i:D2}");
            }

            // ═══════════════════════════════════════════════════════════════════
            // 1. Stage_BG (Image, Tranh nền liền mạch)
            // ═══════════════════════════════════════════════════════════════════
            var stageBg = CreateUIElement("Stage_BG", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var stageBgImg = stageBg.AddComponent<Image>();
            if (spStageBg != null) { stageBgImg.sprite = spStageBg; stageBgImg.color = Color.white; }

            // ═══════════════════════════════════════════════════════════════════
            // 2. Order_Banner (BẮT BUỘC CÓ — Công tắc bind, ẩn an toàn)
            // ═══════════════════════════════════════════════════════════════════
            var orderBanner = CreateUIElement("Order_Banner", canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-1000, 1000), new Vector2(400, 100));
            var obCg = orderBanner.AddComponent<CanvasGroup>();
            obCg.alpha = 0f;
            obCg.blocksRaycasts = false;
            var orderCard = CreateUIElement("Order_Card", orderBanner.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateUIElement("Img_Avatar", orderCard.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(50, 50)).AddComponent<Image>();
            CreateUIElement("Img_Dish", orderCard.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(50, 50)).AddComponent<Image>();
            CreateText("Txt_Name", orderCard.transform, "Order", 16, Color.white, fontAsset, TextAlignmentOptions.Left);
            CreateText("Txt_Rewards", orderCard.transform, "+10", 14, Color.white, fontAsset, TextAlignmentOptions.Left);

            // ═══════════════════════════════════════════════════════════════════
            // 3. Recipe_Board (Vùng A: Sổ công thức & Thẻ chi tiết)
            // ═══════════════════════════════════════════════════════════════════
            var recipeBoard = CreateUIElement("Recipe_Board", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Board_List (Bên trái: Recipe Book) ──
            var boardList = CreateUIElement("Board_List", recipeBoard.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(270, 0), new Vector2(480, 960));
            var blImg = boardList.AddComponent<Image>();
            if (spWoodFrame != null) { blImg.sprite = spWoodFrame; blImg.color = Color.white; }

            // Header Recipe Book
            var bookHeader = CreateUIElement("Header", boardList.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -65), new Vector2(340, 50));
            CreateText("Txt_RecipeBook", bookHeader.transform, "👨‍🍳 Recipe Book", 28, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // 5 Tabs (All / Main / Side / Soup / Dessert)
            var tabsGroup = CreateUIElement("Tabs", boardList.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -125), new Vector2(410, 42));
            var tabsLayout = tabsGroup.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.spacing = 6;
            CreateTabPill("Tab_All", tabsGroup.transform, "All", new Color(0.96f, 0.62f, 0.12f), fontAsset);
            CreateTabPill("Tab_Main", tabsGroup.transform, "Main", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabPill("Tab_Side", tabsGroup.transform, "Side", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabPill("Tab_Soup", tabsGroup.transform, "Soup", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabPill("Tab_Dessert", tabsGroup.transform, "Dessert", new Color(0.56f, 0.38f, 0.22f), fontAsset);

            // ScrollRect: Dish_Scroll/Viewport/Content
            var dishScroll = CreateUIElement("Dish_Scroll", boardList.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(45, 45), new Vector2(-90, -230));
            var scrollRect = dishScroll.AddComponent<ScrollRect>();
            var viewport = CreateUIElement("Viewport", dishScroll.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;
            var content = CreateUIElement("Content", viewport.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 680), new Vector2(0.5f, 1));
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 10;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            string[] demoDishes = { "Cabbage Fried Rice", "Beef Carrot Stew", "Pepper Beef", "Potato Pork Soup", "Egg Fried Rice", "Roast Chicken", "Chili Chicken" };
            int[] demoLevels = { 6, 8, 10, 6, 5, 7, 9 };
            for (int i = 0; i < demoDishes.Length; i++)
            {
                CreateDemoDishCard(content.transform, demoDishes[i], demoLevels[i], spCardNormal, spPanFood, fontAsset, i == 0);
            }

            // ── Board_Detail (Vùng B: Thẻ chi tiết món - Giữa trên) ──
            var boardDetail = CreateUIElement("Board_Detail", recipeBoard.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(90, -35), new Vector2(740, 260), new Vector2(0.5f, 1));
            var bdImg = boardDetail.AddComponent<Image>();
            if (spPaperCream != null) { bdImg.sprite = spPaperCream; bdImg.color = Color.white; }

            var imgDish = CreateUIElement("Img_Dish", boardDetail.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 10), new Vector2(145, 145));
            var imgDishComp = imgDish.AddComponent<Image>();
            if (spPanFood != null) { imgDishComp.sprite = spPanFood; imgDishComp.color = Color.white; }

            var txtDishName = CreateText("Txt_DishName", boardDetail.transform, "Cabbage Fried Rice", 26, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(215, -40), new Vector2(340, 32));

            var txtDishMeta = CreateText("Txt_DishMeta", boardDetail.transform, "Simple ingredients, rich flavor.\nA classic home-cooked dish!", 14, new Color(0.45f, 0.35f, 0.25f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishMeta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(215, -90), new Vector2(340, 38));

            // Need_Chips container
            var needChips = CreateUIElement("Need_Chips", boardDetail.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(215, 30), new Vector2(-235, 60), new Vector2(0, 0));
            var needChipsLayout = needChips.AddComponent<HorizontalLayoutGroup>();
            needChipsLayout.spacing = 15;
            CreateChipSlot("Chip_1", needChips.transform, "3/3", fontAsset);
            CreateChipSlot("Chip_2", needChips.transform, "17/3", fontAsset);
            CreateChipSlot("Chip_3", needChips.transform, "1/2", fontAsset);
            CreateChipSlot("Chip_4", needChips.transform, "2/1", fontAsset);

            // Hidden required labels for bind compatibility
            var hiddenTitles = CreateUIElement("HiddenTitles", boardDetail.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            hiddenTitles.SetActive(false);
            CreateText("Txt_NeedTitle", hiddenTitles.transform, "Need", 12, Color.clear, fontAsset, TextAlignmentOptions.Left);
            CreateText("Txt_TasteTitle", hiddenTitles.transform, "Taste", 12, Color.clear, fontAsset, TextAlignmentOptions.Left);
            CreateText("Txt_Rewards", hiddenTitles.transform, "Reward", 12, Color.clear, fontAsset, TextAlignmentOptions.Left);
            CreateText("Txt_Projection", hiddenTitles.transform, "Proj", 12, Color.clear, fontAsset, TextAlignmentOptions.Left);
            CreateUIElement("Btn_OtherDish", hiddenTitles.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero).AddComponent<Button>();

            // ═══════════════════════════════════════════════════════════════════
            // 4. Oven & Cooking Time (Vùng C: Lò nướng & Plaque đếm ngược)
            // ═══════════════════════════════════════════════════════════════════
            var oven = CreateUIElement("Oven", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(430, 200), new Vector2(260, 280), new Vector2(0.5f, 0));
            var ovenImg = oven.AddComponent<Image>();
            if (spOven != null) { ovenImg.sprite = spOven; ovenImg.color = Color.white; }

            CreateUIElement("Oven_Fire", oven.transform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(100, 100));
            CreateUIElement("Oven_Glow", oven.transform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(120, 120));
            CreateUIElement("Oven_Mouth", oven.transform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(100, 80));

            // Oven_StateBar (Plaque Cooking Time - Đặt góc trên thẻ chi tiết)
            var stateBar = CreateUIElement("Oven_StateBar", boardDetail.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-45, -40), new Vector2(160, 52), new Vector2(1, 1));
            var stateBarBg = stateBar.AddComponent<Image>();
            stateBarBg.color = new Color(0.96f, 0.88f, 0.72f);
            CreateText("Txt_State", stateBar.transform, "⏱ 00:45", 18, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);
            CreateUIElement("Fill", stateBar.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).AddComponent<Image>();

            // ═══════════════════════════════════════════════════════════════════
            // 5. Chalkboard (Vùng D: Today's Special - Phải trên)
            // ═══════════════════════════════════════════════════════════════════
            var chalkboard = CreateUIElement("Chalkboard", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -115), new Vector2(290, 280), new Vector2(1, 1));
            var cbImg = chalkboard.AddComponent<Image>();
            if (spChalkboard != null) { cbImg.sprite = spChalkboard; cbImg.color = Color.white; }

            CreateText("Chalk_Gold", chalkboard.transform, "👑 Today's Special", 19, new Color(0.95f, 0.85f, 0.5f), fontAsset, TextAlignmentOptions.Top);
            var txtChalk = CreateText("Txt_Chalk", chalkboard.transform, "• Cabbage Salad\n• Chicken & Cabbage\n• Corn Egg Soup", 15, Color.white, fontAsset, TextAlignmentOptions.TopLeft);
            SetRect(txtChalk.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(35, 30), new Vector2(-70, -90));

            // ═══════════════════════════════════════════════════════════════════
            // 6. Sân Khấu Bếp & Thiết Bị (Vùng E: Bếp Gas, Chảo, Mèo Đầu Bếp)
            // ═══════════════════════════════════════════════════════════════════
            // Stove_01
            var stove01 = CreateUIElement("Stove_01", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 160), new Vector2(270, 190), new Vector2(0.5f, 0));
            var s01Img = stove01.AddComponent<Image>();
            if (spStove != null) { s01Img.sprite = spStove; s01Img.color = Color.white; }

            CreateUIElement("Flame", stove01.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 80));
            var pan01 = CreateUIElement("Pan", stove01.transform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(240, 150));
            var pan01Food = CreateUIElement("Pan_Food", pan01.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var panFoodImg = pan01Food.AddComponent<Image>();
            if (spPanFood != null) { panFoodImg.sprite = spPanFood; panFoodImg.color = Color.white; }

            // Stove_02 (Bản sao thứ 2)
            var stove02 = CreateUIElement("Stove_02", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-280, 160), new Vector2(250, 160), new Vector2(0.5f, 0));
            var s02Img = stove02.AddComponent<Image>();
            if (spStove != null) { s02Img.sprite = spStove; s02Img.color = Color.white; }
            CreateUIElement("Flame", stove02.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var pan02 = CreateUIElement("Pan", stove02.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CreateUIElement("Pan_Food", pan02.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Cat_Chef (12-Frame Animation)
            var catChef = CreateUIElement("Cat_Chef", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(230, 185), new Vector2(190, 230), new Vector2(0.5f, 0));
            var catImg = catChef.AddComponent<Image>();
            if (catFrames[0] != null) { catImg.sprite = catFrames[0]; catImg.color = Color.white; }
            var catAnim = catChef.AddComponent<SpriteFrameAnimator>();
            catAnim.Frames = catFrames;
            catAnim.Fps = 8f;

            // Speech_Bubble/Txt_PrepToast
            var speechBubble = CreateUIElement("Speech_Bubble", catChef.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-50, 45), new Vector2(175, 52));
            speechBubble.AddComponent<Image>().color = Color.white;
            CreateText("Txt_PrepToast", speechBubble.transform, "A pinch of salt! 💕", 13, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // Plating_Table (Cần cho code bind, ẩn an toàn)
            var plating = CreateUIElement("Plating_Table", canvasGo.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(-1000, -1000), new Vector2(100, 100));
            plating.AddComponent<Button>();
            CreateText("Txt_Plating", plating.transform, "Plating", 14, Color.clear, fontAsset, TextAlignmentOptions.Center);
            CreateUIElement("Dish_Visual", plating.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ═══════════════════════════════════════════════════════════════════
            // 7. Tray (Vùng F: Khay nguyên liệu - Dưới giữa)
            // ═══════════════════════════════════════════════════════════════════
            var tray = CreateUIElement("Tray", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(800, 145), new Vector2(0.5f, 0));
            var trayImg = tray.AddComponent<Image>();
            if (spTray != null) { trayImg.sprite = spTray; trayImg.color = Color.white; }

            CreateUIElement("Tab_Ingredients", tray.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(75, -12), new Vector2(120, 30));
            CreateUIElement("Tab_Seasonings", tray.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(200, -12), new Vector2(120, 30)).SetActive(false);
            CreateUIElement("Btn_ClearAll", tray.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -12), new Vector2(80, 30)).AddComponent<Button>();

            // Scroll_Grid_Ingredients/Viewport/Grid_Ingredients
            var scrollIng = CreateUIElement("Scroll_Grid_Ingredients", tray.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(35, 15), new Vector2(-70, -35));
            var ingVp = CreateUIElement("Viewport", scrollIng.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ingVp.AddComponent<Mask>().showMaskGraphic = false;
            ingVp.AddComponent<Image>().color = Color.white;
            var gridIng = CreateUIElement("Grid_Ingredients", ingVp.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var gridLayout = gridIng.AddComponent<HorizontalLayoutGroup>();
            gridLayout.childForceExpandWidth = true;
            gridLayout.spacing = 15;

            CreateTraySlotItem("Slot_1", gridIng.transform, "3", fontAsset);
            CreateTraySlotItem("Slot_2", gridIng.transform, "17", fontAsset);
            CreateTraySlotItem("Slot_3", gridIng.transform, "10", fontAsset);
            CreateTraySlotItem("Slot_4", gridIng.transform, "1", fontAsset);
            CreateTraySlotItem("Slot_5", gridIng.transform, "2", fontAsset);
            CreateTraySlotItem("Slot_Add", gridIng.transform, "+", fontAsset);

            // Hidden Seasonings grid for Bind compatibility
            var scrollSea = CreateUIElement("Scroll_Grid_Seasonings", tray.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            scrollSea.SetActive(false);
            var seaVp = CreateUIElement("Viewport", scrollSea.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            CreateUIElement("Grid_Seasonings", seaVp.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);

            // ═══════════════════════════════════════════════════════════════════
            // 8. Điều Khiển Phải & Nút Cook (Vùng G & H)
            // ═══════════════════════════════════════════════════════════════════
            // Warehouse_Box
            var warehouseBox = CreateUIElement("Warehouse_Box", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(430, 50), new Vector2(145, 125), new Vector2(0.5f, 0));
            var whImg = warehouseBox.AddComponent<Image>();
            if (spStorage != null) { whImg.sprite = spStorage; whImg.color = Color.white; }
            CreateText("Txt_Label", warehouseBox.transform, "Storage", 12, Color.white, fontAsset, TextAlignmentOptions.Top);
            CreateText("Txt_Sent", warehouseBox.transform, "12/50", 14, Color.white, fontAsset, TextAlignmentOptions.Bottom);

            // Toggle_AutoCook
            var toggleAuto = CreateUIElement("Toggle_AutoCook", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(150, 140), new Vector2(100, 100), new Vector2(0.5f, 0));
            var autoImg = toggleAuto.AddComponent<Image>();
            if (spAutoCook != null) { autoImg.sprite = spAutoCook; autoImg.color = Color.white; }
            toggleAuto.AddComponent<Button>();

            // Btn_Action (Nút Cook cam lớn)
            var btnAction = CreateUIElement("Btn_Action", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 45), new Vector2(260, 95), new Vector2(1, 0));
            var btnActionImg = btnAction.AddComponent<Image>();
            if (spBtnCook != null) { btnActionImg.sprite = spBtnCook; btnActionImg.color = Color.white; }
            btnAction.AddComponent<Button>();
            CreateText("Txt_Action", btnAction.transform, "Cook ✨", 28, Color.white, fontAsset, TextAlignmentOptions.Right);
            var txtSub = CreateText("Txt_Sub", btnAction.transform, "", 12, Color.white, fontAsset, TextAlignmentOptions.Center);
            txtSub.gameObject.SetActive(false);

            // Top Buttons: Btn_BackFarm & Btn_Settings
            var btnBackFarm = CreateUIElement("Btn_BackFarm", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-140, -35), new Vector2(190, 60), new Vector2(1, 1));
            var bfImg = btnBackFarm.AddComponent<Image>();
            if (spBtnBack != null) { bfImg.sprite = spBtnBack; bfImg.color = Color.white; }
            btnBackFarm.AddComponent<Button>();
            CreateText("Txt_BackLabel", btnBackFarm.transform, "Back to Farm", 16, Color.white, fontAsset, TextAlignmentOptions.Center);

            var btnSettings = CreateUIElement("Btn_Settings", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -35), new Vector2(60, 60), new Vector2(1, 1));
            var stgImg = btnSettings.AddComponent<Image>();
            if (spBtnSettings != null) { stgImg.sprite = spBtnSettings; stgImg.color = Color.white; }
            btnSettings.AddComponent<Button>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static Sprite LoadV3Sprite(string spriteName)
        {
            // First check V3
            string path = $"{V3SpritesPath}/{spriteName}.png";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            // Fallback to Processed
            path = $"Assets/Art/UI/KitchenCozyV3/Processed/{spriteName}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject CreateUIElement(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            SetRect(rt, anchorMin, anchorMax, anchoredPos, sizeDelta, pivot);
            return go;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2? pivot = null)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            if (pivot.HasValue) rt.pivot = pivot.Value;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<TextMeshProUGUI>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            if (font != null) txt.font = font;
            txt.alignment = align;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return txt;
        }

        private static void CreateTabPill(string name, Transform parent, string label, Color color, TMP_FontAsset font)
        {
            var go = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(65, 38));
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<Button>();
            CreateText("Txt_Label", go.transform, label, 14, Color.white, font, TextAlignmentOptions.Center);
        }

        private static void CreateDemoDishCard(Transform parent, string dishName, int level, Sprite cardBgSprite, Sprite iconSprite, TMP_FontAsset font, bool isSelected)
        {
            var card = CreateUIElement("Card_Dish", parent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 80));
            var cardBg = card.AddComponent<Image>();
            if (cardBgSprite != null) { cardBg.sprite = cardBgSprite; cardBg.color = isSelected ? new Color(1f, 0.95f, 0.8f) : Color.white; }
            else { cardBg.color = isSelected ? new Color(1f, 0.94f, 0.75f) : new Color(0.96f, 0.92f, 0.82f); }

            var iconCircle = CreateUIElement("Img_Dish", card.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(42, 0), new Vector2(56, 56));
            var iconImg = iconCircle.AddComponent<Image>();
            if (iconSprite != null) { iconImg.sprite = iconSprite; iconImg.color = Color.white; }

            var txtName = CreateText("Txt_DishName", card.transform, dishName, 16, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Left);
            SetRect(txtName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(80, -16), new Vector2(-80, 24));

            var txtLv = CreateText("Txt_Level", card.transform, $"Lv.{level}", 13, new Color(0.55f, 0.45f, 0.35f), font, TextAlignmentOptions.Left);
            SetRect(txtLv.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(80, 14), new Vector2(-80, 20));

            var txtArrow = CreateText("Icon_Chevron", card.transform, ">", 20, new Color(0.56f, 0.38f, 0.22f), font, TextAlignmentOptions.Right);
            SetRect(txtArrow.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-15, 0), new Vector2(25, 25));
        }

        private static void CreateChipSlot(string name, Transform parent, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(54, 54));
            slot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(36, 36));
            icon.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.53f);
            var txt = CreateText("Txt_Chips", slot.transform, count, 12, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Bottom);
        }

        private static void CreateTraySlotItem(string name, Transform parent, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(74, 74));
            slot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(44, 44));
            icon.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.53f);
            if (!string.IsNullOrEmpty(count))
            {
                var txt = CreateText("Txt_Count", slot.transform, count, 14, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.BottomRight);
                SetRect(txt.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(-6, 4), new Vector2(0, 20));
            }
        }

        private static TMP_FontAsset FindCozyFont()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null && (font.name.Contains("Cozy") || font.name.Contains("Game") || font.name.Contains("Noto") || font.name.Contains("Liberation")))
                    return font;
            }
            return null;
        }
    }
}
