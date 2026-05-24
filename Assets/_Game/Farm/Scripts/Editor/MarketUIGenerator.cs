using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MarketUIGenerator
{
    private const string MarketDataFolder = "Assets/_Game/ScriptableObjects/Market";
    private const string MarketDataPath = MarketDataFolder + "/MarketDatabase.asset";
    private const string MarketPrefabFolder = "Assets/_Game/Prefab/ui/Market";
    private const string ShopItemPrefabPath = MarketPrefabFolder + "/ShopItem_Prefab.prefab";

    [MenuItem("FarmTools/Generate Market UI")]
    public static void GenerateMarketUI()
    {
        EnsureFolder(MarketDataFolder);
        EnsureFolder(MarketPrefabFolder);

        MarketDatabase_SO database = LoadOrCreateDatabase();
        MarketShopItemUI shopItemPrefab = LoadOrCreateShopItemPrefab();

        GameObject canvas = CreateMarketUI(database, shopItemPrefab);
        Undo.RegisterCreatedObjectUndo(canvas, "Generate Market UI");
        EnsureEventSystem();
        ValidateGeneratedHierarchy(canvas);

        Selection.activeGameObject = canvas;
        EditorGUIUtility.PingObject(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.scene);

        EditorApplication.delayCall += () =>
        {
            if (canvas == null)
                return;

            Selection.activeGameObject = canvas;
            EditorGUIUtility.PingObject(canvas);
        };

        Debug.Log("[FarmTools] Generate Market UI completed. Canvas_MarketPopup child count = " + canvas.transform.childCount);
        EditorUtility.DisplayDialog(
            "Generate Market UI",
            "Done. Created Canvas_MarketPopup with Panel_Background, Popup_Main, Header_Bar, Scroll_View, Viewport, and Content.",
            "OK");
    }

    [MenuItem("Tools/Farm/Generate Market UI")]
    public static void GenerateMarketUIFromToolsMenu()
    {
        GenerateMarketUI();
    }

    [MenuItem("FarmTools/Create Default Market Database")]
    public static void CreateDefaultMarketDatabase()
    {
        EnsureFolder(MarketDataFolder);
        MarketDatabase_SO database = LoadOrCreateDatabase();
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }

    private static GameObject CreateMarketUI(MarketDatabase_SO database, MarketShopItemUI shopItemPrefab)
    {
        GameObject oldCanvas = GameObject.Find("Canvas_MarketPopup");
        if (oldCanvas != null)
        {
            ArchiveExistingCanvas(oldCanvas);
        }

        GameObject canvasObject = new GameObject("Canvas_MarketPopup");
        RectTransform canvasRect = canvasObject.AddComponent<RectTransform>();
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasObject.AddComponent<UIRaycastBlocker>();
        Image canvasRaycastShield = canvasObject.AddComponent<Image>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasRaycastShield.color = new Color(0f, 0f, 0f, 0.001f);
        canvasRaycastShield.raycastTarget = true;
        StretchFull(canvasRect);

        GameObject panelBackground = CreateChild("Panel_Background", canvasObject.transform);
        Image panelBackgroundImage = panelBackground.AddComponent<Image>();
        panelBackgroundImage.color = new Color(0f, 0f, 0f, 0.58f);
        panelBackgroundImage.raycastTarget = true;
        panelBackground.AddComponent<UIRaycastBlocker>();
        StretchFull(panelBackground.GetComponent<RectTransform>());

        GameObject popupMain = CreateChild("Popup_Main", panelBackground.transform);
        Image popupImage = popupMain.AddComponent<Image>();
        popupImage.color = new Color(0.56f, 0.36f, 0.18f, 1f);
        RectTransform popupRect = popupMain.GetComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = new Vector2(1120f, 640f);

        Button buttonClose = CreateButton("Button_Close", popupMain.transform, "X", new Color(0.92f, 0.38f, 0.30f, 1f));
        SetTopRight(buttonClose.GetComponent<RectTransform>(), new Vector2(-20f, -20f), new Vector2(58f, 58f));

        GameObject headerBar = CreateChild("Header_Bar", popupMain.transform);
        Image headerImage = headerBar.AddComponent<Image>();
        headerImage.color = new Color(0.24f, 0.55f, 0.42f, 1f);
        SetTopStretch(headerBar.GetComponent<RectTransform>(), 24f, -26f, -96f, -92f);

        Text textTimer = CreateText("Text_Timer", headerBar.transform, "05:00", 30, TextAnchor.MiddleCenter, Color.white);
        SetTopLeft(textTimer.rectTransform, new Vector2(20f, -12f), new Vector2(142f, 40f));

        GameObject timerBackground = CreateChild("Timer_Background", headerBar.transform);
        Image timerBackgroundImage = timerBackground.AddComponent<Image>();
        timerBackgroundImage.color = new Color(0.12f, 0.30f, 0.25f, 1f);
        SetTopLeft(timerBackground.GetComponent<RectTransform>(), new Vector2(182f, -18f), new Vector2(315f, 34f));

        GameObject fillBarObject = CreateChild("FillBar_Timer", timerBackground.transform);
        Image fillBarTimer = fillBarObject.AddComponent<Image>();
        fillBarTimer.color = new Color(1f, 0.78f, 0.22f, 1f);
        fillBarTimer.type = Image.Type.Filled;
        fillBarTimer.fillMethod = Image.FillMethod.Horizontal;
        fillBarTimer.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillBarTimer.fillAmount = 1f;
        StretchFull(fillBarObject.GetComponent<RectTransform>());

        Button buttonRefreshFree = CreateButton("Button_RefreshFree", headerBar.transform, "R", new Color(0.90f, 0.78f, 0.28f, 1f));
        SetTopLeft(buttonRefreshFree.GetComponent<RectTransform>(), new Vector2(532f, -10f), new Vector2(64f, 48f));

        Button buttonRefreshGem = CreateButton("Button_RefreshGem", headerBar.transform, "G", new Color(0.42f, 0.75f, 0.95f, 1f));
        SetTopLeft(buttonRefreshGem.GetComponent<RectTransform>(), new Vector2(610f, -10f), new Vector2(64f, 48f));

        GameObject scrollView = CreateChild("Scroll_View", popupMain.transform);
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.20f);
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        SetStretch(scrollView.GetComponent<RectTransform>(), 40f, 128f, -40f, -40f);

        GameObject viewport = CreateChild("Viewport", scrollView.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.05f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        StretchFull(viewport.GetComponent<RectTransform>());

        GameObject content = CreateChild("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = new Vector2(24f, 0f);
        contentRect.sizeDelta = new Vector2(1220f, 450f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 210f);
        grid.spacing = new Vector2(18f, 18f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 2;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;

        MarketManager manager = popupMain.AddComponent<MarketManager>();
        AssignMarketManager(manager, database, shopItemPrefab, content.transform, textTimer, fillBarTimer, buttonRefreshFree, buttonRefreshGem, buttonClose);

        MarketPopupUI marketPopupUI = popupMain.AddComponent<MarketPopupUI>();
        AssignMarketPopupUI(marketPopupUI, panelBackground, buttonClose);

        return canvasObject;
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.AddComponent<RectTransform>();
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void ArchiveExistingCanvas(GameObject oldCanvas)
    {
        if (oldCanvas == null)
            return;

        if (SelectionContains(oldCanvas))
        {
            Selection.objects = new Object[0];
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        Undo.RecordObject(oldCanvas, "Archive Old Market UI");
        oldCanvas.name = "Canvas_MarketPopup_Old_" + System.DateTime.Now.ToString("HHmmss");
        oldCanvas.SetActive(false);
    }

    private static bool SelectionContains(GameObject root)
    {
        if (root == null)
            return false;

        Object[] selectedObjects = Selection.objects;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Object selected = selectedObjects[i];
            GameObject selectedGameObject = null;

            if (selected is GameObject go)
                selectedGameObject = go;
            else if (selected is Component component)
                selectedGameObject = component.gameObject;

            if (selectedGameObject == null)
                continue;

            if (selectedGameObject == root || selectedGameObject.transform.IsChildOf(root.transform))
                return true;
        }

        return false;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        GameObject buttonObject = CreateChild(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text buttonText = CreateText("Text", buttonObject.transform, label, 22, TextAnchor.MiddleCenter, Color.black);
        StretchFull(buttonText.rectTransform);

        return button;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = CreateChild(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static MarketShopItemUI LoadOrCreateShopItemPrefab()
    {
        GameObject root = new GameObject("ShopItem_Prefab");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(170f, 210f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        MarketShopItemUI itemUI = root.AddComponent<MarketShopItemUI>();

        GameObject iconObject = CreateChild("Image_Icon", root.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.color = new Color(0.82f, 0.86f, 0.88f, 1f);
        SetTopStretch(icon.rectTransform, 18f, -14f, -18f, -78f);

        Text name = CreateText("Text_Name", root.transform, "Item Name", 16, TextAnchor.MiddleCenter, Color.black);
        SetBottomStretch(name.rectTransform, 12f, 78f, -12f, 110f);

        Text quantity = CreateText("Text_Quantity", root.transform, "x1", 22, TextAnchor.MiddleLeft, Color.black);
        SetBottomLeft(quantity.rectTransform, new Vector2(18f, 52f), new Vector2(70f, 28f));

        Text price = CreateText("Text_Price", root.transform, "100", 22, TextAnchor.MiddleRight, Color.black);
        SetBottomRight(price.rectTransform, new Vector2(-18f, 52f), new Vector2(78f, 28f));

        Button buy = CreateButton("Button_Buy", root.transform, "Buy", new Color(0.96f, 0.85f, 0.42f, 1f));
        SetBottomStretch(buy.GetComponent<RectTransform>(), 18f, 10f, -18f, 48f);

        GameObject soldOut = CreateChild("Panel_SoldOut", root.transform);
        Image soldOutImage = soldOut.AddComponent<Image>();
        soldOutImage.color = new Color(0f, 0f, 0f, 0.66f);
        StretchFull(soldOut.GetComponent<RectTransform>());

        Text soldOutText = CreateText("Text_SoldOut", soldOut.transform, "Hết hàng", 26, TextAnchor.MiddleCenter, Color.white);
        StretchFull(soldOutText.rectTransform);
        soldOut.SetActive(false);

        AssignShopItemUI(itemUI, icon, name, quantity, price, buy, soldOut);

        PrefabUtility.SaveAsPrefabAsset(root, ShopItemPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return AssetDatabase.LoadAssetAtPath<MarketShopItemUI>(ShopItemPrefabPath);
    }

    private static MarketDatabase_SO LoadOrCreateDatabase()
    {
        MarketDatabase_SO database = AssetDatabase.LoadAssetAtPath<MarketDatabase_SO>(MarketDataPath);
        if (database != null)
            return database;

        database = ScriptableObject.CreateInstance<MarketDatabase_SO>();
        database.ResetToDefaultPlaceholderRows();
        AssetDatabase.CreateAsset(database, MarketDataPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void AssignMarketManager(
        MarketManager manager,
        MarketDatabase_SO database,
        MarketShopItemUI prefab,
        Transform content,
        Text timer,
        Image fill,
        Button refreshFree,
        Button refreshGem,
        Button close)
    {
        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("marketDatabase").objectReferenceValue = database;
        serialized.FindProperty("shopItemPrefab").objectReferenceValue = prefab;
        serialized.FindProperty("content").objectReferenceValue = content;
        serialized.FindProperty("textTimer").objectReferenceValue = timer;
        serialized.FindProperty("fillBarTimer").objectReferenceValue = fill;
        serialized.FindProperty("buttonRefreshFree").objectReferenceValue = refreshFree;
        serialized.FindProperty("buttonRefreshGem").objectReferenceValue = refreshGem;
        serialized.FindProperty("buttonClose").objectReferenceValue = close;
        serialized.FindProperty("popupRoot").objectReferenceValue = manager.transform.parent != null
            ? manager.transform.parent.gameObject
            : manager.gameObject;

        AssignObjectList(serialized.FindProperty("cropDatabase"), FindAssetsOfType<CropData>());
        AssignObjectList(serialized.FindProperty("itemDatabase"), FindAssetsOfType<InventoryItemData>());
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T[] FindAssetsOfType<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        T[] assets = new T[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            assets[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return assets;
    }

    private static void AssignObjectList<T>(SerializedProperty listProperty, T[] assets) where T : Object
    {
        if (listProperty == null)
            return;

        listProperty.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++)
            listProperty.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
    }

    private static void AssignMarketPopupUI(MarketPopupUI popupUI, GameObject popupRoot, Button close)
    {
        SerializedObject serialized = new SerializedObject(popupUI);
        serialized.FindProperty("popupRoot").objectReferenceValue = popupRoot;
        serialized.FindProperty("btnClose").objectReferenceValue = close;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignShopItemUI(MarketShopItemUI itemUI, Image icon, Text name, Text quantity, Text price, Button buy, GameObject soldOut)
    {
        SerializedObject serialized = new SerializedObject(itemUI);
        serialized.FindProperty("imageIcon").objectReferenceValue = icon;
        serialized.FindProperty("textName").objectReferenceValue = name;
        serialized.FindProperty("textQuantity").objectReferenceValue = quantity;
        serialized.FindProperty("textPrice").objectReferenceValue = price;
        serialized.FindProperty("buttonBuy").objectReferenceValue = buy;
        serialized.FindProperty("panelSoldOut").objectReferenceValue = soldOut;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateGeneratedHierarchy(GameObject canvas)
    {
        string[] requiredPaths =
        {
            "Panel_Background",
            "Panel_Background/Popup_Main",
            "Panel_Background/Popup_Main/Button_Close",
            "Panel_Background/Popup_Main/Header_Bar",
            "Panel_Background/Popup_Main/Header_Bar/Text_Timer",
            "Panel_Background/Popup_Main/Header_Bar/Button_RefreshFree",
            "Panel_Background/Popup_Main/Header_Bar/Button_RefreshGem",
            "Panel_Background/Popup_Main/Scroll_View",
            "Panel_Background/Popup_Main/Scroll_View/Viewport",
            "Panel_Background/Popup_Main/Scroll_View/Viewport/Content"
        };

        for (int i = 0; i < requiredPaths.Length; i++)
        {
            if (canvas.transform.Find(requiredPaths[i]) == null)
                Debug.LogError("[FarmTools] Missing generated UI node: " + requiredPaths[i]);
        }
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void EnsureFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetStretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
        rect.localScale = Vector3.one;
    }

    private static void SetTopStretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
        rect.localScale = Vector3.one;
    }

    private static void SetBottomStretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
        rect.localScale = Vector3.one;
    }

    private static void SetTopLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetTopRight(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetBottomLeft(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void SetBottomRight(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}

[InitializeOnLoad]
public static class MarketEditorSelectionGuard
{
    static MarketEditorSelectionGuard()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
            return;

        if (!IsMarketPopupSelected())
            return;

        Selection.objects = new Object[0];
        ActiveEditorTracker.sharedTracker.ForceRebuild();
        EditorApplication.RepaintHierarchyWindow();
    }

    private static bool IsMarketPopupSelected()
    {
        Object[] selectedObjects = Selection.objects;
        for (int i = 0; i < selectedObjects.Length; i++)
        {
            GameObject selectedGameObject = ResolveGameObject(selectedObjects[i]);
            if (selectedGameObject == null)
                continue;

            Transform cursor = selectedGameObject.transform;
            while (cursor != null)
            {
                if (cursor.name.StartsWith("Canvas_MarketPopup", System.StringComparison.Ordinal))
                    return true;

                cursor = cursor.parent;
            }
        }

        return false;
    }

    private static GameObject ResolveGameObject(Object selectedObject)
    {
        if (selectedObject is GameObject go)
            return go;

        if (selectedObject is Component component)
            return component.gameObject;

        return null;
    }
}
