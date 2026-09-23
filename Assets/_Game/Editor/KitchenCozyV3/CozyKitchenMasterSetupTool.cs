using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class CozyKitchenMasterSetupTool
    {
        private const string FolderPath = "Assets/Art/UI/KitchenCozyV3";
        private const string ProcessedFolderPath = "Assets/Art/UI/KitchenCozyV3/Processed";

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/Setup Cozy Kitchen (Full Art & Hierarchy 1-Click)")]
        public static void SetupFullArtAndHierarchy()
        {
            // 1. Process & Chroma-Key all sprites
            ProcessAllArtAssets();

            // 2. Build and skin the UI hierarchy
            BuildSkinnedHierarchy();

            Debug.Log("<color=green>[CozyKitchen] Full 100% Clean Art & Hierarchy Setup Complete!</color>");
        }

        public static void ProcessAllArtAssets()
        {
            if (!AssetDatabase.IsValidFolder(ProcessedFolderPath))
            {
                AssetDatabase.CreateFolder(FolderPath, "Processed");
            }

            ProcessBackground();
            ProcessSeamlessCatChef();
            ProcessAppliances();
            ProcessCleanFrames();
            ProcessTopButtons();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureSpritesInFolder();
        }

        private static void ProcessBackground()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "cozy_kitchen_bg_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            SaveTextureAsPNG(src, $"{ProcessedFolderPath}/kitchen_background_main.png");
        }

        private static void ProcessSeamlessCatChef()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "seamless_cat_chef_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            int cols = 4;
            int rows = 3;
            int cellW = src.width / cols;
            int cellH = src.height / rows;

            int frameIndex = 1;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int x = c * cellW;
                    int y = (rows - 1 - r) * cellH;

                    Texture2D cell = ExtractAndChromaKey(src, x, y, cellW, cellH);
                    SaveTextureAsPNG(cell, $"{ProcessedFolderPath}/cat_chef_frame_{frameIndex:D2}.png");
                    frameIndex++;
                }
            }
        }

        private static void ProcessAppliances()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "kitchen_appliances_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            int cols = 3;
            int rows = 2;
            int cellW = src.width / cols;
            int cellH = src.height / rows;

            string[] names = {
                "cutting_board", "gas_stove_pan", "fried_rice_pan",
                "stone_pizza_oven", "storage_basket", "btn_auto_cook_badge"
            };

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (idx >= names.Length) break;
                    int x = c * cellW;
                    int y = (rows - 1 - r) * cellH;

                    Texture2D cell = ExtractAndChromaKey(src, x, y, cellW, cellH);
                    SaveTextureAsPNG(cell, $"{ProcessedFolderPath}/{names[idx]}.png");
                    idx++;
                }
            }
        }

        private static void ProcessCleanFrames()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            
            // 1. Blank recipe frame
            ProcessSingleImage(fullDir, "blank_recipe_frame_*.jpg", "frame_recipe_book.png");
            // 2. Blank parchment card
            ProcessSingleImage(fullDir, "blank_parchment_card_*.jpg", "frame_dish_card.png");
            // 3. Blank chalkboard
            ProcessSingleImage(fullDir, "blank_chalkboard_*.jpg", "frame_chalkboard.png");
            // 4. Blank wooden tray
            ProcessSingleImage(fullDir, "blank_wooden_tray_*.jpg", "shelf_ingredients.png");
            // 5. Blank cook button
            ProcessSingleImage(fullDir, "blank_cook_btn_*.jpg", "btn_cook_pill.png");
            // 6. Blank dish card row
            ProcessSingleImage(fullDir, "blank_dish_card_*.jpg", "dish_card_row_bg.png");
        }

        private static void ProcessSingleImage(string fullDir, string searchPattern, string outPngName)
        {
            string[] files = Directory.GetFiles(fullDir, searchPattern);
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            Texture2D transparent = ExtractAndChromaKey(src, 0, 0, src.width, src.height);
            SaveTextureAsPNG(transparent, $"{ProcessedFolderPath}/{outPngName}");
        }

        private static void ProcessTopButtons()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3");
            string[] files = Directory.GetFiles(fullDir, "cozy_top_buttons_*.jpg");
            if (files.Length == 0) return;

            Texture2D src = LoadReadableTexture(files[0]);
            if (src == null) return;

            // Slice Left (Back to farm pill) and Right (Gear button)
            Texture2D btnBack = ExtractAndChromaKey(src, 0, 0, (int)(src.width * 0.6f), src.height);
            Texture2D btnGear = ExtractAndChromaKey(src, (int)(src.width * 0.6f), 0, (int)(src.width * 0.4f), src.height);

            SaveTextureAsPNG(btnBack, $"{ProcessedFolderPath}/btn_back_farm.png");
            SaveTextureAsPNG(btnGear, $"{ProcessedFolderPath}/btn_settings_gear.png");
        }

        private static Texture2D LoadReadableTexture(string fullPath)
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            return tex;
        }

        private static Texture2D ExtractAndChromaKey(Texture2D src, int startX, int startY, int width, int height)
        {
            Texture2D dst = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = src.GetPixels(startX, startY, width, height);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float r = c.r;
                float g = c.g;
                float b = c.b;

                // Robust Chroma-key for pure Magenta #FF00FF
                if (r > 0.65f && b > 0.65f && g < 0.35f && Mathf.Abs(r - b) < 0.25f)
                {
                    pixels[i] = Color.clear;
                }
                else if (r > 0.50f && b > 0.50f && g < 0.45f && (r + b) > (g * 2.2f))
                {
                    float alpha = Mathf.Clamp01((g - 0.20f) / 0.25f);
                    pixels[i] = new Color(r, g, b, alpha);
                }
            }

            dst.SetPixels(pixels);
            dst.Apply();
            return dst;
        }

        private static void SaveTextureAsPNG(Texture2D tex, string assetPath)
        {
            byte[] pngData = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllBytes(fullPath, pngData);
        }

        private static void ConfigureSpritesInFolder()
        {
            string fullDir = Path.Combine(Application.dataPath, "Art/UI/KitchenCozyV3/Processed");
            if (!Directory.Exists(fullDir)) return;

            string[] files = Directory.GetFiles(fullDir, "*.png");
            foreach (var f in files)
            {
                string relPath = "Assets" + f.Substring(Application.dataPath.Length).Replace("\\", "/");
                var importer = AssetImporter.GetAtPath(relPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }
        }

        public static void BuildSkinnedHierarchy()
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

            // Clean children
            while (canvasGo.transform.childCount > 0)
            {
                Object.DestroyImmediate(canvasGo.transform.GetChild(0).gameObject);
            }

            TMP_FontAsset fontAsset = FindCozyFont();

            // Load Processed Sprites
            Sprite spBg = LoadSprite("kitchen_background_main");
            Sprite spRecipeBook = LoadSprite("frame_recipe_book");
            Sprite spRecipeHeader = LoadSprite("banner_recipe_scroll_spoon") ?? LoadSprite("banner_recipe_book_pill");
            Sprite spTabActive = LoadSprite("tab_filter_active");
            Sprite spTabInactive = LoadSprite("tab_filter_inactive");
            Sprite spRowSelected = LoadSprite("recipe_row_selected");
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

            // Load 12-frame cat chef sprites
            Sprite[] catFrames = new Sprite[12];
            for (int i = 1; i <= 12; i++)
            {
                catFrames[i - 1] = LoadSprite($"cat_chef_frame_{i:D2}");
            }

            // ── Background Layer ──
            var bgLayer = CreateUIElement("Background_Layer", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgLayer.AddComponent<Image>();
            if (spBg != null) { bgImg.sprite = spBg; bgImg.color = Color.white; }

            // ── Top Right Bar ──
            var topBar = CreateUIElement("TopBar_Layer", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(300, 65), new Vector2(1, 1));
            
            var btnBack = CreateUIElement("Btn_BackToFarm", topBar.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(200, 60));
            var btnBackImg = btnBack.AddComponent<Image>();
            if (spBtnBackFarm != null) { btnBackImg.sprite = spBtnBackFarm; btnBackImg.color = Color.white; }
            btnBack.AddComponent<Button>();
            var txtBack = CreateText("Txt_Back", btnBack.transform, "Back to Farm", 16, Color.white, fontAsset, TextAlignmentOptions.Center);
            SetRect(txtBack.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, 0));

            var btnSettings = CreateUIElement("Btn_Settings", topBar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(60, 60));
            var btnSettingsImg = btnSettings.AddComponent<Image>();
            if (spBtnSettings != null) { btnSettingsImg.sprite = spBtnSettings; btnSettingsImg.color = Color.white; }
            btnSettings.AddComponent<Button>();

            // ── Left Panel: Recipe Book ──
            var leftPanel = CreateUIElement("LeftPanel_RecipeBook", canvasGo.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(30, 20), new Vector2(440, -40), new Vector2(0, 0.5f));
            var leftPanelImg = leftPanel.AddComponent<Image>();
            if (spRecipeBook != null) { leftPanelImg.sprite = spRecipeBook; leftPanelImg.color = Color.white; }

            // Recipe Book Dynamic Header
            var bookHeader = CreateUIElement("Header_RecipeBook", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(340, 65));
            var headerImg = bookHeader.AddComponent<Image>();
            if (spRecipeHeader != null) { headerImg.sprite = spRecipeHeader; headerImg.color = Color.white; }
            CreateText("Txt_Header", bookHeader.transform, "RECIPE BOOK", 22, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // Category Filter Tabs
            var tabsGroup = CreateUIElement("Tabs_CategoryGroup", leftPanel.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -125), new Vector2(380, 42));
            var tabsLayout = tabsGroup.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.spacing = 6;
            CreateSpriteTabButton("Tab_All", tabsGroup.transform, "All", spTabActive, Color.white, fontAsset);
            CreateSpriteTabButton("Tab_Easy", tabsGroup.transform, "Easy", spTabInactive, new Color(0.35f, 0.25f, 0.15f), fontAsset);
            CreateSpriteTabButton("Tab_Medium", tabsGroup.transform, "Medium", spTabInactive, new Color(0.35f, 0.25f, 0.15f), fontAsset);
            CreateSpriteTabButton("Tab_Hard", tabsGroup.transform, "Hard", spTabInactive, new Color(0.35f, 0.25f, 0.15f), fontAsset);

            // ScrollView for Recipes
            var scrollGo = CreateUIElement("ScrollView_Dishes", leftPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(35, 30), new Vector2(-70, -205), new Vector2(0.5f, 0.5f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = CreateUIElement("Viewport", scrollGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;
            var content = CreateUIElement("Content", viewport.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 600), new Vector2(0.5f, 1));
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 8;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            string[] demoDishes = { "Fried Rice", "Vegetable Soup", "Grilled Fish", "Tomato Pasta", "Roasted Chicken", "Beef Stew", "Fruit Salad" };
            int[] demoStars = { 3, 3, 3, 3, 3, 3, 3 };
            for (int i = 0; i < demoDishes.Length; i++)
            {
                CreateDemoDishCard(content.transform, demoDishes[i], demoStars[i], spRowSelected, spFriedRice, fontAsset, i == 3);
            }

            // ── Center Top: Selected Dish Overview Card ──
            var centerTop = CreateUIElement("CenterTop_DishOverview", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(70, -25), new Vector2(700, 240), new Vector2(0.5f, 1));
            var centerTopImg = centerTop.AddComponent<Image>();
            if (spDishCard != null) { centerTopImg.sprite = spDishCard; centerTopImg.color = Color.white; }

            var dishImg = CreateUIElement("Img_DishBigPreview", centerTop.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 10), new Vector2(130, 130));
            var dishImgComp = dishImg.AddComponent<Image>();
            if (spFriedRice != null) { dishImgComp.sprite = spFriedRice; dishImgComp.color = Color.white; }

            var txtDishName = CreateText("Txt_DishName", centerTop.transform, "Cabbage Fried Rice", 26, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(200, -35), new Vector2(320, 32));

            var txtDishDesc = CreateText("Txt_DishDesc", centerTop.transform, "Simple ingredients, rich flavor.\nA classic home-cooked dish!", 14, new Color(0.45f, 0.35f, 0.25f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishDesc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(200, -85), new Vector2(320, 38));

            // Required Ingredients Row
            var reqIngGroup = CreateUIElement("RequiredIngredients_Group", centerTop.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(200, 25), new Vector2(-220, 55), new Vector2(0, 0));
            var reqIngLayout = reqIngGroup.AddComponent<HorizontalLayoutGroup>();
            reqIngLayout.spacing = 15;
            CreateIngredientSlot("Slot_1", reqIngGroup.transform, "Cabbage", "3/3", fontAsset);
            CreateIngredientSlot("Slot_2", reqIngGroup.transform, "Carrot", "17/3", fontAsset);
            CreateIngredientSlot("Slot_3", reqIngGroup.transform, "Egg", "1/2", fontAsset);
            CreateIngredientSlot("Slot_4", reqIngGroup.transform, "Rice", "2/1", fontAsset);

            // Cooking Time Badge
            var badgeTime = CreateUIElement("Badge_CookingTime", centerTop.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -35), new Vector2(150, 48), new Vector2(1, 1));
            var badgeTimeBg = badgeTime.AddComponent<Image>();
            badgeTimeBg.color = new Color(0.96f, 0.88f, 0.72f);
            CreateText("Txt_CookingTime", badgeTime.transform, "⏱ 00:45", 18, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // ── Right Top: Today's Special Chalkboard ──
            var rightTop = CreateUIElement("RightTop_TodaySpecials", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -110), new Vector2(270, 260), new Vector2(1, 1));
            var rightTopImg = rightTop.AddComponent<Image>();
            if (spChalkboard != null) { rightTopImg.sprite = spChalkboard; rightTopImg.color = Color.white; }

            var txtSpecialTitle = CreateText("Txt_Title", rightTop.transform, "👑 Today's Special", 18, new Color(0.95f, 0.85f, 0.5f), fontAsset, TextAlignmentOptions.Top);
            SetRect(txtSpecialTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -35), new Vector2(0, 30));
            var txtSpecialList = CreateText("Txt_List", rightTop.transform, "• Cabbage Salad\n• Chicken & Cabbage\n• Corn Egg Soup", 15, Color.white, fontAsset, TextAlignmentOptions.TopLeft);
            SetRect(txtSpecialList.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 25), new Vector2(-60, -80));

            // ── Center Kitchen: Station & Appliances ──
            var kitchenStation = CreateUIElement("CenterKitchen_Station", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(70, 160), new Vector2(1000, 380), new Vector2(0.5f, 0));

            // Cutting board
            var cuttingBoard = CreateUIElement("CuttingBoard", kitchenStation.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(130, -30), new Vector2(240, 160));
            var cbImg = cuttingBoard.AddComponent<Image>();
            if (spCuttingBoard != null) { cbImg.sprite = spCuttingBoard; cbImg.color = Color.white; }

            // Gas stove + Pan
            var stove = CreateUIElement("Stove_GasBurner", kitchenStation.transform, new Vector2(0.42f, 0.5f), new Vector2(0.42f, 0.5f), new Vector2(0, -20), new Vector2(260, 180));
            var stoveImg = stove.AddComponent<Image>();
            if (spStove != null) { stoveImg.sprite = spStove; stoveImg.color = Color.white; }

            // Auto Cook Button
            var btnAutoCook = CreateUIElement("Btn_AutoCook", kitchenStation.transform, new Vector2(0.56f, 0.35f), new Vector2(0.56f, 0.35f), Vector2.zero, new Vector2(100, 100));
            var autoImg = btnAutoCook.AddComponent<Image>();
            if (spAutoCook != null) { autoImg.sprite = spAutoCook; autoImg.color = Color.white; }
            btnAutoCook.AddComponent<Button>();

            // Cat Chef Area (12 frames loop - Seamless, NO white border!)
            var catChef = CreateUIElement("CatChef_Animated", kitchenStation.transform, new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0, 20), new Vector2(180, 220));
            var catImg = catChef.AddComponent<Image>();
            if (catFrames[0] != null) { catImg.sprite = catFrames[0]; catImg.color = Color.white; }
            var catAnim = catChef.AddComponent<SpriteFrameAnimator>();
            catAnim.Frames = catFrames;
            catAnim.Fps = 8f;

            // Cat Speech Bubble
            var speechBubble = CreateUIElement("SpeechBubble", catChef.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-50, 40), new Vector2(170, 50));
            speechBubble.AddComponent<Image>().color = Color.white;
            CreateText("Txt_Tip", speechBubble.transform, "A pinch of salt! 💕", 13, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            // Stone Pizza Oven
            var stoneOven = CreateUIElement("StoneOven", kitchenStation.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-120, 40), new Vector2(240, 260));
            var ovenImg = stoneOven.AddComponent<Image>();
            if (spStoneOven != null) { ovenImg.sprite = spStoneOven; ovenImg.color = Color.white; }

            // Storage basket
            var storage = CreateUIElement("Storage_Basket", kitchenStation.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-110, 50), new Vector2(140, 120));
            var stImg = storage.AddComponent<Image>();
            if (spStorage != null) { stImg.sprite = spStorage; stImg.color = Color.white; }

            // ── Bottom Panel: Ingredients Tray ──
            var bottomTray = CreateUIElement("BottomPanel_IngredientsTray", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(760, 140), new Vector2(0.5f, 0));
            var trayImg = bottomTray.AddComponent<Image>();
            if (spShelf != null) { trayImg.sprite = spShelf; trayImg.color = Color.white; }

            var trayBadge = CreateUIElement("Badge_Ingredients", bottomTray.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -10), new Vector2(110, 28));
            CreateText("Txt_IngBadge", trayBadge.transform, "Ingredients", 13, new Color(0.24f, 0.14f, 0.06f), fontAsset, TextAlignmentOptions.Center);

            var traySlots = CreateUIElement("Slots_Group", bottomTray.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 15), new Vector2(-60, -35));
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
            var btnCook = CreateUIElement("Btn_CookAction", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-70, 40), new Vector2(250, 90), new Vector2(1, 0));
            var btnCookImg = btnCook.AddComponent<Image>();
            if (spCookBtn != null) { btnCookImg.sprite = spCookBtn; btnCookImg.color = Color.white; }
            btnCook.AddComponent<Button>();
            var txtCook = CreateText("Txt_CookLabel", btnCook.transform, "Cook ✨", 26, Color.white, fontAsset, TextAlignmentOptions.Right);
            SetRect(txtCook.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-30, 0));

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

        private static void CreateSpriteTabButton(string name, Transform parent, string label, Sprite tabSprite, Color textColor, TMP_FontAsset font)
        {
            var go = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(80, 36));
            var img = go.AddComponent<Image>();
            if (tabSprite != null) { img.sprite = tabSprite; img.color = Color.white; }
            else { img.color = new Color(0.9f, 0.85f, 0.75f); }
            go.AddComponent<Button>();
            CreateText("Txt_Label", go.transform, label, 14, textColor, font, TextAlignmentOptions.Center);
        }

        private static void CreateDemoDishCard(Transform parent, string dishName, int stars, Sprite selectedRowSprite, Sprite iconSprite, TMP_FontAsset font, bool isSelected)
        {
            var card = CreateUIElement("Card_" + dishName.Replace(" ", ""), parent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 68));
            var cardBg = card.AddComponent<Image>();
            if (isSelected && selectedRowSprite != null)
            {
                cardBg.sprite = selectedRowSprite;
                cardBg.color = Color.white;
            }
            else
            {
                cardBg.color = Color.clear;
            }

            var iconCircle = CreateUIElement("Img_DishIcon", card.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(48, 48));
            var iconImg = iconCircle.AddComponent<Image>();
            if (iconSprite != null) { iconImg.sprite = iconSprite; iconImg.color = Color.white; }

            var txtName = CreateText("Txt_Name", card.transform, dishName, 16, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Left);
            SetRect(txtName.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(75, 0), new Vector2(-120, 30));

            var txtStars = CreateText("Txt_Stars", card.transform, "★★★", 12, new Color(0.85f, 0.65f, 0.35f), font, TextAlignmentOptions.Right);
            SetRect(txtStars.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-45, 0), new Vector2(50, 25));

            var txtArrow = CreateText("Txt_Arrow", card.transform, ">", 18, new Color(0.7f, 0.55f, 0.4f), font, TextAlignmentOptions.Right);
            SetRect(txtArrow.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-15, 0), new Vector2(20, 25));
        }

        private static void CreateIngredientSlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(52, 52));
            slot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(34, 34));
            icon.AddComponent<Image>().color = new Color(0.85f, 0.72f, 0.53f);
            var txt = CreateText("Txt_Count", slot.transform, count, 12, new Color(0.24f, 0.14f, 0.06f), font, TextAlignmentOptions.Bottom);
        }

        private static void CreateTraySlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(72, 72));
            slot.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(42, 42));
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
