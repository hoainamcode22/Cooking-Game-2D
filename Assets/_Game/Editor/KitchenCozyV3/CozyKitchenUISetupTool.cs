using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenCozyV3.Editor
{
    public static class CozyKitchenUISetupTool
    {
        // Palette Colors for Cozy Kitchen
        private static readonly Color ColorWoodDark = new Color(0.28f, 0.16f, 0.08f, 1f);      // #472914
        private static readonly Color ColorWoodMedium = new Color(0.56f, 0.38f, 0.22f, 1f);    // #8F6138
        private static readonly Color ColorWoodLight = new Color(0.85f, 0.72f, 0.53f, 1f);     // #D9B887
        private static readonly Color ColorParchment = new Color(0.98f, 0.94f, 0.85f, 1f);     // #FAF0D9
        private static readonly Color ColorChalkboard = new Color(0.18f, 0.24f, 0.22f, 1f);    // #2E3D38
        private static readonly Color ColorOrangeCook = new Color(0.96f, 0.62f, 0.12f, 1f);    // #F59E1E
        private static readonly Color ColorGreenTab = new Color(0.48f, 0.68f, 0.28f, 1f);      // #7BAE47
        private static readonly Color ColorCardBg = new Color(0.96f, 0.92f, 0.82f, 1f);        // #F5EBD2
        private static readonly Color ColorTextDark = new Color(0.24f, 0.14f, 0.06f, 1f);      // #3D240F

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/Setup Cozy Kitchen Hierarchy (1-Click)")]
        public static void SetupCozyKitchenHierarchy()
        {
            // 1. Safe Backup & Disable Old UI
            var oldUI = GameObject.Find("Kitchen_UI_v2");
            if (oldUI != null)
            {
                Undo.RecordObject(oldUI, "Disable Old Kitchen_UI_v2");
                oldUI.SetActive(false);
                Debug.Log("[CozyKitchen] Old UI 'Kitchen_UI_v2' has been safely disabled.");
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

            // Clean existing children if needed
            while (canvasGo.transform.childCount > 0)
            {
                Object.DestroyImmediate(canvasGo.transform.GetChild(0).gameObject);
            }

            // Find Font
            TMP_FontAsset fontAsset = FindCozyFont();

            // 3. Build Layers
            // Background Layer
            var bgLayer = CreateUIElement("Background_Layer", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgLayer.AddComponent<Image>();
            bgImg.color = new Color(0.92f, 0.85f, 0.72f, 1f); // Warm cozy kitchen tone

            // Top Right Bar (Back to farm + settings)
            var topBar = CreateUIElement("TopBar_Layer", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(300, 60), new Vector2(1, 1));
            var btnBack = CreateButton("Btn_BackToFarm", topBar.transform, new Vector2(0, 0), new Vector2(200, 56), ColorWoodMedium, "Back to Farm", 20, fontAsset);
            var btnSettings = CreateButton("Btn_Settings", topBar.transform, new Vector2(210, 0), new Vector2(60, 56), ColorWoodMedium, "⚙", 24, fontAsset);

            // Left Panel: Recipe Book
            var leftPanel = CreateUIElement("LeftPanel_RecipeBook", canvasGo.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(20, 20), new Vector2(400, -40), new Vector2(0, 0.5f));
            var leftPanelBg = leftPanel.AddComponent<Image>();
            leftPanelBg.color = ColorWoodLight;

            // Recipe Book Header
            var bookHeader = CreateUIElement("Header_RecipeBook", leftPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(-20, 60), new Vector2(0.5f, 1));
            var bookHeaderBg = bookHeader.AddComponent<Image>();
            bookHeaderBg.color = ColorWoodDark;
            CreateText("Txt_Header", bookHeader.transform, "👨‍🍳 Recipe Book", 26, Color.white, fontAsset, TextAlignmentOptions.Center);

            // Category Filter Tabs
            var tabsGroup = CreateUIElement("Tabs_CategoryGroup", leftPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(-20, 40), new Vector2(0.5f, 1));
            var tabsLayout = tabsGroup.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.spacing = 6;
            var tabAll = CreateButton("Tab_All", tabsGroup.transform, Vector2.zero, Vector2.zero, ColorOrangeCook, "All", 16, fontAsset);
            var tabMain = CreateButton("Tab_Main", tabsGroup.transform, Vector2.zero, Vector2.zero, ColorWoodMedium, "Main", 16, fontAsset);
            var tabSide = CreateButton("Tab_Side", tabsGroup.transform, Vector2.zero, Vector2.zero, ColorWoodMedium, "Side", 16, fontAsset);
            var tabSoup = CreateButton("Tab_Soup", tabsGroup.transform, Vector2.zero, Vector2.zero, ColorWoodMedium, "Soup", 16, fontAsset);
            var tabDessert = CreateButton("Tab_Dessert", tabsGroup.transform, Vector2.zero, Vector2.zero, ColorWoodMedium, "Dessert", 16, fontAsset);

            // ScrollView for Recipes
            var scrollGo = CreateUIElement("ScrollView_Dishes", leftPanel.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 10), new Vector2(-20, -140), new Vector2(0.5f, 0.5f));
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var viewport = CreateUIElement("Viewport", scrollGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;
            var content = CreateUIElement("Content", viewport.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 800), new Vector2(0.5f, 1));
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 8;
            contentLayout.padding = new RectOffset(6, 6, 6, 6);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Add demo dish cards
            string[] demoDishes = { "Cabbage Fried Rice", "Beef Carrot Stew", "Pepper Beef", "Potato Pork Soup", "Egg Fried Rice", "Roast Chicken", "Chili Chicken" };
            int[] demoLevels = { 6, 8, 10, 6, 5, 7, 9 };
            for (int i = 0; i < demoDishes.Length; i++)
            {
                CreateDemoDishCard(content.transform, demoDishes[i], demoLevels[i], fontAsset, i == 0);
            }

            // Center Top: Selected Dish Overview Card
            var centerTop = CreateUIElement("CenterTop_DishOverview", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(80, -30), new Vector2(650, 200), new Vector2(0.5f, 1));
            var centerTopBg = centerTop.AddComponent<Image>();
            centerTopBg.color = ColorParchment;

            var dishImg = CreateUIElement("Img_DishBigPreview", centerTop.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(110, 110));
            var dishImgComp = dishImg.AddComponent<Image>();
            dishImgComp.color = new Color(0.95f, 0.88f, 0.6f, 1f);

            var txtDishName = CreateText("Txt_DishName", centerTop.transform, "Cabbage Fried Rice", 24, ColorTextDark, fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -30), new Vector2(300, 30));

            var txtDishDesc = CreateText("Txt_DishDesc", centerTop.transform, "Simple ingredients, rich flavor.\nA classic home-cooked dish!", 14, new Color(0.45f, 0.35f, 0.25f), fontAsset, TextAlignmentOptions.Left);
            SetRect(txtDishDesc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(150, -75), new Vector2(300, 40));

            // Required Ingredients Row
            var reqIngGroup = CreateUIElement("RequiredIngredients_Group", centerTop.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(150, 20), new Vector2(-160, 50), new Vector2(0, 0));
            var reqIngLayout = reqIngGroup.AddComponent<HorizontalLayoutGroup>();
            reqIngLayout.spacing = 15;
            CreateIngredientSlot("Slot_1", reqIngGroup.transform, "Cabbage", "3/3", fontAsset);
            CreateIngredientSlot("Slot_2", reqIngGroup.transform, "Carrot", "17/3", fontAsset);
            CreateIngredientSlot("Slot_3", reqIngGroup.transform, "Egg", "1/2", fontAsset);
            CreateIngredientSlot("Slot_4", reqIngGroup.transform, "Rice", "2/1", fontAsset);

            // Cooking Time Badge
            var badgeTime = CreateUIElement("Badge_CookingTime", centerTop.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(140, 50), new Vector2(1, 1));
            var badgeTimeBg = badgeTime.AddComponent<Image>();
            badgeTimeBg.color = ColorWoodLight;
            var txtTime = CreateText("Txt_CookingTime", badgeTime.transform, "⏱ 00:45", 18, ColorTextDark, fontAsset, TextAlignmentOptions.Center);

            // Right Top: Today's Special Chalkboard
            var rightTop = CreateUIElement("RightTop_TodaySpecials", canvasGo.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -100), new Vector2(240, 200), new Vector2(1, 1));
            var rightTopBg = rightTop.AddComponent<Image>();
            rightTopBg.color = ColorChalkboard;
            var txtSpecialTitle = CreateText("Txt_Title", rightTop.transform, "👑 Today's Special", 18, new Color(0.95f, 0.85f, 0.5f), fontAsset, TextAlignmentOptions.Top);
            SetRect(txtSpecialTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -10), new Vector2(0, 30));
            var txtSpecialList = CreateText("Txt_List", rightTop.transform, "• Cabbage Salad\n• Chicken & Cabbage\n• Corn Egg Soup", 14, Color.white, fontAsset, TextAlignmentOptions.TopLeft);
            SetRect(txtSpecialList.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(15, 10), new Vector2(-30, -50));

            // Center Kitchen: Counter & Appliances
            var kitchenStation = CreateUIElement("CenterKitchen_Station", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(80, 180), new Vector2(960, 340), new Vector2(0.5f, 0));
            var stationBg = kitchenStation.AddComponent<Image>();
            stationBg.color = new Color(0.82f, 0.68f, 0.5f, 0.9f);

            // Cutting board
            var cuttingBoard = CreateUIElement("CuttingBoard", kitchenStation.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -20), new Vector2(180, 120));
            cuttingBoard.AddComponent<Image>().color = new Color(0.72f, 0.55f, 0.38f);
            CreateText("Txt_Board", cuttingBoard.transform, "Cutting Board", 14, ColorWoodDark, fontAsset, TextAlignmentOptions.Center);

            // Gas stove + Pan
            var stove = CreateUIElement("Stove_GasBurner", kitchenStation.transform, new Vector2(0.4f, 0.5f), new Vector2(0.4f, 0.5f), new Vector2(0, -10), new Vector2(220, 150));
            stove.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f);
            var stoveFireAnim = stove.AddComponent<SpriteFrameAnimator>();
            CreateText("Txt_Stove", stove.transform, "🍳 Gas Stove & Pan\n(Animated Flame)", 14, Color.white, fontAsset, TextAlignmentOptions.Center);

            // Auto Cook Button
            var btnAutoCook = CreateButton("Btn_AutoCook", kitchenStation.transform, new Vector2(490, 40), new Vector2(110, 45), ColorWoodLight, "Auto Cook", 14, fontAsset);

            // Cat Chef Area
            var catChef = CreateUIElement("CatChef_Animated", kitchenStation.transform, new Vector2(0.7f, 0.5f), new Vector2(0.7f, 0.5f), new Vector2(0, 20), new Vector2(140, 180));
            catChef.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.8f);
            var catAnim = catChef.AddComponent<SpriteFrameAnimator>();
            CreateText("Txt_Cat", catChef.transform, "🐱 Cat Chef\n(12-Frames Loop)", 13, ColorWoodDark, fontAsset, TextAlignmentOptions.Center);

            // Cat Speech Bubble
            var speechBubble = CreateUIElement("SpeechBubble", catChef.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-40, 30), new Vector2(150, 45));
            speechBubble.AddComponent<Image>().color = Color.white;
            CreateText("Txt_Tip", speechBubble.transform, "A pinch of salt! 💕", 12, ColorWoodDark, fontAsset, TextAlignmentOptions.Center);

            // Stone Pizza Oven
            var stoneOven = CreateUIElement("StoneOven", kitchenStation.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-100, 30), new Vector2(180, 220));
            stoneOven.AddComponent<Image>().color = new Color(0.6f, 0.4f, 0.35f);
            var ovenAnim = stoneOven.AddComponent<SpriteFrameAnimator>();
            CreateText("Txt_Oven", stoneOven.transform, "🔥 Stone Oven\n(Wood Fire Glow)", 14, Color.white, fontAsset, TextAlignmentOptions.Center);

            // Storage basket
            var storage = CreateUIElement("Storage_Basket", kitchenStation.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 40), new Vector2(140, 45));
            storage.AddComponent<Image>().color = ColorWoodMedium;
            CreateText("Txt_Storage", storage.transform, "📦 Storage 12/50", 13, Color.white, fontAsset, TextAlignmentOptions.Center);

            // Bottom Panel: Ingredients Tray
            var bottomTray = CreateUIElement("BottomPanel_IngredientsTray", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(680, 130), new Vector2(0.5f, 0));
            var trayBg = bottomTray.AddComponent<Image>();
            trayBg.color = ColorWoodLight;

            var badgeTray = CreateUIElement("Badge_TrayTitle", bottomTray.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, 5), new Vector2(120, 30));
            badgeTray.AddComponent<Image>().color = ColorWoodDark;
            CreateText("Txt_Ingredients", badgeTray.transform, "Ingredients", 14, Color.white, fontAsset, TextAlignmentOptions.Center);

            var traySlots = CreateUIElement("Slots_Group", bottomTray.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(15, 10), new Vector2(-30, -35));
            var trayLayout = traySlots.AddComponent<HorizontalLayoutGroup>();
            trayLayout.childForceExpandWidth = true;
            trayLayout.spacing = 10;
            CreateTraySlot("Slot_Cabbage", traySlots.transform, "Cabbage", "3", fontAsset);
            CreateTraySlot("Slot_Carrot", traySlots.transform, "Carrot", "17", fontAsset);
            CreateTraySlot("Slot_Tomato", traySlots.transform, "Tomato", "10", fontAsset);
            CreateTraySlot("Slot_Egg", traySlots.transform, "Egg", "1", fontAsset);
            CreateTraySlot("Slot_Rice", traySlots.transform, "Rice", "2", fontAsset);
            CreateTraySlot("Slot_Unlock", traySlots.transform, "+", "", fontAsset);

            // Bottom Right: Cook Button
            var btnCook = CreateButton("Btn_CookAction", canvasGo.transform, new Vector2(800, -450), new Vector2(220, 80), ColorOrangeCook, "👨‍🍳 Cook ✨", 28, fontAsset);

            // Wire references to Controller
            WireReferences(controller, canvasGo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>[CozyKitchen] Successfully created Canvas_CozyKitchen_V3 hierarchy!</color>");
        }

        [MenuItem("Tools/Farm Game/Kitchen Cozy V3/Toggle UI (Switch between V2 and V3)")]
        public static void ToggleUI()
        {
            var oldUI = GameObject.Find("Kitchen_UI_v2");
            var newUI = GameObject.Find("Canvas_CozyKitchen_V3");

            if (newUI != null && oldUI != null)
            {
                bool isNewActive = newUI.activeSelf;
                newUI.SetActive(!isNewActive);
                oldUI.SetActive(isNewActive);
                Debug.Log($"[CozyKitchen] Switched UI -> V3 Active: {!isNewActive}, V2 Active: {isNewActive}");
            }
            else
            {
                Debug.LogWarning("[CozyKitchen] Cannot find both UI roots to toggle.");
            }
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

        private static Button CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, Color color, string label, float fontSize, TMP_FontAsset font, Vector2? pivot = null)
        {
            var go = CreateUIElement(name, parent, pivot ?? new Vector2(0.5f, 0.5f), pivot ?? new Vector2(0.5f, 0.5f), pos, size, pivot);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            CreateText("Txt_Label", go.transform, label, fontSize, Color.white, font, TextAlignmentOptions.Center);
            return btn;
        }

        private static void CreateDemoDishCard(Transform parent, string dishName, int level, TMP_FontAsset font, bool isSelected)
        {
            var card = CreateUIElement("Card_" + dishName.Replace(" ", ""), parent, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 70));
            var cardBg = card.AddComponent<Image>();
            cardBg.color = isSelected ? new Color(1f, 0.94f, 0.75f) : ColorCardBg;

            var iconCircle = CreateUIElement("Img_DishIcon", card.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(35, 0), new Vector2(50, 50));
            iconCircle.AddComponent<Image>().color = isSelected ? ColorOrangeCook : ColorWoodMedium;

            var txtName = CreateText("Txt_Name", card.transform, dishName, 16, ColorTextDark, font, TextAlignmentOptions.Left);
            SetRect(txtName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(70, -15), new Vector2(-100, 24));

            var txtLv = CreateText("Txt_Level", card.transform, $"Lv.{level}", 13, new Color(0.55f, 0.45f, 0.35f), font, TextAlignmentOptions.Left);
            SetRect(txtLv.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(70, 12), new Vector2(-100, 20));

            var txtArrow = CreateText("Txt_Arrow", card.transform, ">", 20, ColorWoodMedium, font, TextAlignmentOptions.Right);
            SetRect(txtArrow.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(30, 30));
        }

        private static void CreateIngredientSlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(50, 50));
            slot.AddComponent<Image>().color = Color.white;
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 5), new Vector2(32, 32));
            icon.AddComponent<Image>().color = ColorWoodLight;
            var txt = CreateText("Txt_Count", slot.transform, count, 12, ColorTextDark, font, TextAlignmentOptions.Bottom);
        }

        private static void CreateTraySlot(string name, Transform parent, string ingName, string count, TMP_FontAsset font)
        {
            var slot = CreateUIElement(name, parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(70, 70));
            slot.AddComponent<Image>().color = Color.white;
            var icon = CreateUIElement("Img_Icon", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(40, 40));
            icon.AddComponent<Image>().color = ColorWoodLight;
            if (!string.IsNullOrEmpty(count))
            {
                var txt = CreateText("Txt_Count", slot.transform, count, 13, ColorTextDark, font, TextAlignmentOptions.BottomRight);
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

        private static void WireReferences(CozyKitchenViewController controller, GameObject canvasGo)
        {
            var so = new SerializedObject(controller);
            
            // Find DishData and Ingredients
            foreach (var guid in AssetDatabase.FindAssets("t:ListDishData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var dishBook = AssetDatabase.LoadAssetAtPath<ListDishData>(path);
                if (dishBook != null)
                {
                    so.FindProperty("dishBookData").objectReferenceValue = dishBook;
                    break;
                }
            }

            so.ApplyModifiedProperties();
        }
    }
}
