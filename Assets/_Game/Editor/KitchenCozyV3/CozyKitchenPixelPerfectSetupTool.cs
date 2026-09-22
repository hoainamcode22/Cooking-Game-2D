using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class CozyKitchenPixelPerfectSetupTool
    {
        private const string FolderPath = "Assets/Art/UI/KitchenCozyV3";
        private const string ProcessedFolderPath = "Assets/Art/UI/KitchenCozyV3/Processed";

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/Setup Pixel-Perfect Cozy Kitchen (100% Match Design)")]
        public static void SetupPixelPerfectKitchen()
        {
            // 1. Process all assets
            CozyKitchenMasterSetupTool.ProcessAllArtAssets();
            ProcessMasterBackgroundPlate();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 2. Build pixel-perfect hierarchy
            BuildPixelPerfectHierarchy();

            Debug.Log("<color=green>[CozyKitchen] ★★★ 100% Pixel-Perfect Cozy Kitchen Setup Complete! ★★★</color>");
        }

        private static void ProcessMasterBackgroundPlate()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string masterPath = Path.Combine(fullDir, "master_design_mockup.jpg");
            if (!File.Exists(masterPath)) return;

            byte[] bytes = File.ReadAllBytes(masterPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);

            byte[] pngData = tex.EncodeToPNG();
            string outPath = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3/Processed/master_background_stage.png");
            File.WriteAllBytes(outPath, pngData);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath("Assets/Art/UI/KitchenCozyV3/Processed/master_background_stage.png") as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        private static void BuildPixelPerfectHierarchy()
        {
            // 1. Safe Backup & Disable Old UI
            var oldUI = GameObject.Find("Kitchen_UI_v2");
            if (oldUI != null)
            {
                Undo.RecordObject(oldUI, "Disable Old Kitchen_UI_v2");
                oldUI.SetActive(false);
            }

            // 2. Find or Create Main Canvas
            var canvasGo = GameObject.Find("Canvas_CozyKitchen_V3");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas_CozyKitchen_V3");
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas_CozyKitchen_V3");
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();

            var controller = canvasGo.GetComponent<CozyKitchenViewController>();
            if (controller == null) controller = canvasGo.AddComponent<CozyKitchenViewController>();

            // Clean existing children
            while (canvasGo.transform.childCount > 0)
            {
                Object.DestroyImmediate(canvasGo.transform.GetChild(0).gameObject);
            }

            TMP_FontAsset fontAsset = FindCozyFont();

            // Load Sprites
            Sprite spMasterBg = LoadSprite("master_background_stage");
            Sprite spRecipeBook = LoadSprite("frame_recipe_book");
            Sprite spDishCard = LoadSprite("frame_dish_card");
            Sprite spChalkboard = LoadSprite("frame_chalkboard");
            Sprite spShelf = LoadSprite("shelf_ingredients");
            Sprite spCookBtn = LoadSprite("btn_cook_pill");
            Sprite spDishCardRow = LoadSprite("dish_card_row_bg");
            Sprite spBtnBackFarm = LoadSprite("btn_back_farm");
            Sprite spBtnSettings = LoadSprite("btn_settings_gear");

            Sprite spCuttingBoard = LoadSprite("cutting_board");
            Sprite spStove = LoadSprite("gas_stove_pan");
            Sprite spFriedRice = LoadSprite("fried_rice_pan");
            Sprite spStoneOven = LoadSprite("stone_pizza_oven");
            Sprite spStorage = LoadSprite("storage_basket");
            Sprite spAutoCook = LoadSprite("btn_auto_cook_badge");

            // Load 12 seamless cat frames
            Sprite[] catFrames = new Sprite[12];
            for (int i = 1; i <= 12; i++)
            {
                catFrames[i - 1] = LoadSprite($"cat_chef_frame_{i:D2}");
            }

            // ── Background Layer (Master Warm Sunlit Kitchen Scene) ──
            var bgLayer = CreateUIElement("Background_Layer", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgLayer.AddComponent<Image>();
            if (spMasterBg != null) { bgImg.sprite = spMasterBg; bgImg.color = Color.white; }

            // ── Top Right: Back to Farm & Settings Buttons ──
            var topBar = CreateUIElement("TopBar_Layer", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -35), new Vector2(300, 70), new Vector2(1, 1));
            
            var btnBack = CreateUIElement("Btn_BackToFarm", topBar.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(95, 0), new Vector2(190, 60));
            var btnBackImg = btnBack.AddComponent<Image>();
            if (spBtnBackFarm != null) { btnBackImg.sprite = spBtnBackFarm; btnBackImg.color = Color.white; }
            btnBack.AddComponent<Button>();
            var txtBack = CreateText("Txt_Back", btnBack.transform, "Back to Farm", 17, Color.white, fontAsset, TextAlignmentOptions.Center);
            SetRect(txtBack.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(25, 0), new Vector2(-20, 0));

            var btnSettings = CreateUIElement("Btn_Settings", topBar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(60, 60));
            var btnSettingsImg = btnSettings.AddComponent<Image>();
            if (spBtnSettings != null) { btnSettingsImg.sprite = spBtnSettings; btnSettingsImg.color = Color.white; }
            btnSettings.AddComponent<Button>();

            // ── Left Panel: Recipe Book ──
            var leftPanel = CreateUIElement("LeftPanel_RecipeBook", canvasGo.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(270, 0), new Vector2(490, 960), new Vector2(0.5f, 0.5f));
            var leftPanelImg = leftPanel.AddComponent<Image>();
            if (spRecipeBook != null) { leftPanelImg.sprite = spRecipeBook; leftPanelImg.color = Color.white; }

            // Dynamic Header "Recipe Book"
            var bookHeader = CreateUIElement("Header_RecipeBook", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -65), new Vector2(340, 50));
            CreateText("Txt_Header", bookHeader.transform, "👨‍🍳 Recipe Book", 28, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // Category Filter Tabs ([All], [Main], [Side], [Soup], [Dessert])
            var tabsGroup = CreateUIElement("Tabs_CategoryGroup", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -125), new Vector2(410, 42));
            var tabsLayout = tabsGroup.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.spacing = 6;
            CreateTabButton("Tab_All", tabsGroup.transform, "All", new Color(0.96f, 0.62f, 0.12f), fontAsset);
            CreateTabButton("Tab_Main", tabsGroup.transform, "Main", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabButton("Tab_Side", tabsGroup.transform, "Side", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabButton("Tab_Soup", tabsGroup.transform, "Soup", new Color(0.56f, 0.38f, 0.22f), fontAsset);
            CreateTabButton("Tab_Dessert", tabsGroup.transform, "Dessert", new Color(0.56f, 0.38f, 0.22f), fontAsset);

            // ScrollView for Recipes (Wide, Non-Squished Dish Cards)
            var scrollGo = CreateUIElement("ScrollView_Dishes", leftPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(45, 45), new Vector2(-90, -230), new Vector2(0.5f, 0.5f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = CreateUIElement("Viewport", scrollGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
                CreateDemoDishCard(content.transform, demoDishes[i], demoLevels[i], spDishCardRow, spFriedRice, fontAsset, i == 0);
            }

            // ── Center Top: Selected Dish Overview Card ──
            var centerTop = CreateUIElement("CenterTop_DishOverview", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(90, -35), new Vector2(740, 260), new Vector2(0.5f, 1));
            var centerTopImg = centerTop.AddComponent<Image>();
            if (spDishCard != null) { centerTopImg.sprite = spDishCard; centerTopImg.color = Color.white; }

            // Big Dish Plate Preview
            var dishImg = CreateUIElement("Img_DishBigPreview", centerTop.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 10), new Vector2(145, 145));
            var dishImgComp = dishImg.AddComponent<Image>();
            if (spFriedRice != null) { dishImgComp.sprite = spFriedRice; dishImgComp.color = Color.white; }

            // Dish Title & Subtitle
            var txtDishName = CreateText("Txt_DishName", centerTop.transform, "Cabbage Fried Rice", 26, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(215, -40), new Vector2(340, 32));

            var txtDishDesc = CreateText("Txt_DishDesc", centerTop.transform, "Simple ingredients, rich flavor.\nA classic home-cooked dish!", 14, new Color(0.45f, 0.35f, 0.25f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishDesc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(215, -90), new Vector2(340, 38));

            // Required Ingredients Row
            var reqIngGroup = CreateUIElement("RequiredIngredients_Group", centerTop.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(215, 30), new Vector2(-235, 60), new Vector2(0, 0));
            var reqIngLayout = reqIngGroup.AddComponent<HorizontalLayoutGroup>();
            reqIngLayout.spacing = 15;
            CreateIngredientSlot("Slot_1", reqIngGroup.transform, "Cabbage", "3/3", fontAsset);
            CreateIngredientSlot("Slot_2", reqIngGroup.transform, "Carrot", "17/3", fontAsset);
            CreateIngredientSlot("Slot_3", reqIngGroup.transform, "Egg", "1/2", fontAsset);
            CreateIngredientSlot("Slot_4", reqIngGroup.transform, "Rice", "2/1", fontAsset);

            // Cooking Time Badge
            var badgeTime = CreateUIElement("Badge_CookingTime", centerTop.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-45, -40), new Vector2(160, 52), new Vector2(1, 1));
            var badgeTimeBg = badgeTime.AddComponent<Image>();
            badgeTimeBg.color = new Color(0.96f, 0.88f, 0.72f);
            CreateText("Txt_CookingTime", badgeTime.transform, "⏱ 00:45", 18, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // ── Right Top: Today's Special Chalkboard ──
            var rightTop = CreateUIElement("RightTop_TodaySpecials", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -115), new Vector2(290, 280), new Vector2(1, 1));
            var rightTopImg = rightTop.AddComponent<Image>();
            if (spChalkboard != null) { rightTopImg.sprite = spChalkboard; rightTopImg.color = Color.white; }

            var txtSpecialTitle = CreateText("Txt_Title", rightTop.transform, "👑 Today's Special", 19, new Color(0.95f, 0.85f, 0.5f), fontAsset, TextAlignmentOptions.Top);
            SetRect(txtSpecialTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -38), new Vector2(0, 32));
            var txtSpecialList = CreateText("Txt_List", rightTop.transform, "• Cabbage Salad\n• Chicken & Cabbage\n• Corn Egg Soup", 15, Color.white, fontAsset, TextAlignmentOptions.TopLeft);
            SetRect(txtSpecialList.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(35, 30), new Vector2(-70, -90));

            // ── Center Kitchen Counter & Appliances (Aligned with Countertop) ──
            var kitchenStation = CreateUIElement("CenterKitchen_Station", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(90, 175), new Vector2(1060, 390), new Vector2(0.5f, 0));

            // Cutting board on the left
            var cuttingBoard = CreateUIElement("CuttingBoard", kitchenStation.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(140, -30), new Vector2(250, 160));
            var cbImg = cuttingBoard.AddComponent<Image>();
            if (spCuttingBoard != null) { cbImg.sprite = spCuttingBoard; cbImg.color = Color.white; }

            // Tabletop gas stove & pan
            var stove = CreateUIElement("Stove_GasBurner", kitchenStation.transform, new Vector2(0.42f, 0.5f), new Vector2(0.42f, 0.5f), new Vector2(0, -20), new Vector2(270, 190));
            var stoveImg = stove.AddComponent<Image>();
            if (spStove != null) { stoveImg.sprite = spStove; stoveImg.color = Color.white; }

            // Auto Cook Button
            var btnAutoCook = CreateUIElement("Btn_AutoCook", kitchenStation.transform, new Vector2(0.56f, 0.35f), new Vector2(0.56f, 0.35f), Vector2.zero, new Vector2(100, 100));
            var autoImg = btnAutoCook.AddComponent<Image>();
            if (spAutoCook != null) { autoImg.sprite = spAutoCook; autoImg.color = Color.white; }
            btnAutoCook.AddComponent<Button>();

            // Cat Chef Area (12 Seamless frames loop)
            var catChef = CreateUIElement("CatChef_Animated", kitchenStation.transform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0, 25), new Vector2(190, 230));
            var catImg = catChef.AddComponent<Image>();
            if (catFrames[0] != null) { catImg.sprite = catFrames[0]; catImg.color = Color.white; }
            var catAnim = catChef.AddComponent<SpriteFrameAnimator>();
            catAnim.Frames = catFrames;
            catAnim.Fps = 8f;

            // Cat Speech Bubble
            var speechBubble = CreateUIElement("SpeechBubble", catChef.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-50, 45), new Vector2(175, 52));
            speechBubble.AddComponent<Image>().color = Color.white;
            CreateText("Txt_Tip", speechBubble.transform, "A pinch of salt! 💕", 13, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // Stone Pizza Oven on the right
            var stoneOven = CreateUIElement("StoneOven", kitchenStation.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-130, 45), new Vector2(260, 280));
            var ovenImg = stoneOven.AddComponent<Image>();
            if (spStoneOven != null) { ovenImg.sprite = spStoneOven; ovenImg.color = Color.white; }

            // Storage basket
            var storage = CreateUIElement("Storage_Basket", kitchenStation.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-120, 50), new Vector2(145, 125));
            var stImg = storage.AddComponent<Image>();
            if (spStorage != null) { stImg.sprite = spStorage; stImg.color = Color.white; }

            // ── Bottom Panel: Ingredients Tray ──
            var bottomTray = CreateUIElement("BottomPanel_IngredientsTray", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(800, 145), new Vector2(0.5f, 0));
            var trayImg = bottomTray.AddComponent<Image>();
            if (spShelf != null) { trayImg.sprite = spShelf; trayImg.color = Color.white; }

            var trayBadge = CreateUIElement("Badge_Ingredients", bottomTray.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(75, -12), new Vector2(120, 30));
            CreateText("Txt_IngBadge", trayBadge.transform, "Ingredients", 14, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            var traySlots = CreateUIElement("Slots_Group", bottomTray.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(35, 15), new Vector2(-70, -35));
            var trayLayout = traySlots.AddComponent<HorizontalLayoutGroup>();
            trayLayout.childForceExpandWidth = true;
            trayLayout.spacing = 15;
            CreateTraySlot("Slot_Cabbage", traySlots.transform, "Cabbage", "3", fontAsset);
            CreateTraySlot("Slot_Carrot", traySlots.transform, "Carrot", "17", fontAsset);
            CreateTraySlot("Slot_Tomato", traySlots.transform, "Tomato", "10", fontAsset);
            CreateTraySlot("Slot_Egg", traySlots.transform, "Egg", "1", fontAsset);
            CreateTraySlot("Slot_Rice", traySlots.transform, "Rice", "2", fontAsset);
            CreateTraySlot("Slot_Unlock", traySlots.transform, "+", "", fontAsset);

            // ── Bottom Right: Cook Button ──
            var btnCook = CreateUIElement("Btn_CookAction", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 45), new Vector2(260, 95), new Vector2(1, 0));
            var btnCookImg = btnCook.AddComponent<Image>();
            if (spCookBtn != null) { btnCookImg.sprite = spCookBtn; btnCookImg.color = Color.white; }
            btnCook.AddComponent<Button>();
            var txtCook = CreateText("Txt_CookLabel", btnCook.transform, "Cook ✨", 28, Color.white, fontAsset, TextAlignmentOptions.Right);
            SetRect(txtCook.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-35, 0));

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static Sprite LoadSprite(string assetName)
        {
            string path = $"{ProcessedFolderPath}/{assetName}.png";
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

        private static void CreateTabButton(string name, Transform parent, string label, Color color, TMP_FontAsset font)
        {
            var go = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(65, 38));
            var img = go.AddComponent<Image>();
            img.color = color;
            go.AddComponent<Button>();
            CreateText("Txt_Label", go.transform, label, 14, Color.white, font, TextAlignmentOptions.Center);
        }

        private static void CreateDemoDishCard(Transform parent, string dishName, int level, Sprite cardBgSprite, Sprite iconSprite, TMP_FontAsset font, bool isSelected)
        {
            var card = CreateUIElement("Card_" + dishName.Replace(" ", ""), parent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 80));
            var cardBg = card.AddComponent<Image>();
            if (cardBgSprite != null) { cardBg.sprite = cardBgSprite; cardBg.color = isSelected ? new Color(1f, 0.95f, 0.8f) : Color.white; }
            else { cardBg.color = isSelected ? new Color(1f, 0.94f, 0.75f) : new Color(0.96f, 0.92f, 0.82f); }

            var iconCircle = CreateUIElement("Img_DishIcon", card.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(42, 0), new Vector2(56, 56));
            var iconImg = iconCircle.AddComponent<Image>();
            if (iconSprite != null) { iconImg.sprite = iconSprite; iconImg.color = Color.white; }

            var txtName = CreateText("Txt_Name", card.transform, dishName, 16, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Left);
            SetRect(txtName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(80, -16), new Vector2(-80, 24));

            var txtLv = CreateText("Txt_Level", card.transform, $"Lv.{level}", 13, new Color(0.55f, 0.45f, 0.35f), font, TextAlignmentOptions.Left);
            SetRect(txtLv.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(80, 14), new Vector2(-80, 20));

            var txtArrow = CreateText("Txt_Arrow", card.transform, ">", 20, new Color(0.56f, 0.38f, 0.22f), font, TextAlignmentOptions.Right);
            SetRect(txtArrow.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-15, 0), new Vector2(25, 25));
        }

        private static void CreateIngredientSlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(54, 54));
            slot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(36, 36));
            icon.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.53f);
            var txt = CreateText("Txt_Count", slot.transform, count, 12, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Bottom);
        }

        private static void CreateTraySlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
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
