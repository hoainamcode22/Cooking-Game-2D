using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MissionHudButtonSetupTool
{
    private const string MenuPath = "Tools/Farm Game/Setup Mission HUD Button";
    private const string MainMissionDbPath = "Assets/_Game/Farm/data/Data_Ewa/MissionDatabase_Main.asset";

    private static readonly Color Gold = new Color(1f, 0.72f, 0.06f, 1f);
    private static readonly Color GoldDark = new Color(0.73f, 0.34f, 0.04f, 1f);
    private static readonly Color Paper = new Color(1f, 0.93f, 0.73f, 1f);
    private static readonly Color PaperLine = new Color(0.64f, 0.49f, 0.28f, 1f);
    private static readonly Color Blue = new Color(0.25f, 0.49f, 0.86f, 1f);
    private static readonly Color BlueDark = new Color(0.08f, 0.22f, 0.48f, 1f);
    private static readonly Color Yellow = new Color(1f, 0.86f, 0.03f, 1f);
    private static readonly Color BarBack = new Color(0.58f, 0.55f, 0.32f, 0.9f);

    [MenuItem(MenuPath)]
    public static void ApplyToOpenScene()
    {
        Canvas hudCanvas = FindCanvas("Canvas_HUD");
        if (hudCanvas == null)
        {
            EditorUtility.DisplayDialog("Mission HUD Button", "Khong tim thay Canvas_HUD.", "OK");
            return;
        }

        MissionHudButtonUI hud = Apply(hudCanvas);
        Selection.activeGameObject = hud.gameObject;
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
        Debug.Log("[MissionHudButtonSetupTool] Applied Mission HUD Button.");
    }

    public static MissionHudButtonUI Apply(Canvas hudCanvas)
    {
        if (hudCanvas == null)
            return null;

        Transform old = hudCanvas.transform.Find("MissionHudButton");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        RectTransform root = CreateRect(hudCanvas.transform, "MissionHudButton", new Vector2(700f, 270f));
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = new Vector2(124f, -300f);
        root.SetAsLastSibling();

        MissionHudButtonUI hud = Undo.AddComponent<MissionHudButtonUI>(root.gameObject);

        ButtonParts button = CreateMissionCircleButton(root);
        BubbleParts bubble = CreateBubble(root);

        AssignReferences(hud, button, bubble);
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(hudCanvas);

        return hud;
    }

    private static ButtonParts CreateMissionCircleButton(RectTransform root)
    {
        RectTransform buttonRoot = CreateImage(root, "MissionCircleButton", new Vector2(126f, 126f), GoldDark, GetCircleSprite());
        buttonRoot.anchorMin = new Vector2(0f, 0.5f);
        buttonRoot.anchorMax = new Vector2(0f, 0.5f);
        buttonRoot.pivot = new Vector2(0.5f, 0.5f);
        buttonRoot.anchoredPosition = new Vector2(70f, 72f);

        Button button = GetOrAdd<Button>(buttonRoot.gameObject);
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonRoot.GetComponent<Image>();

        CreateImage(buttonRoot, "Outer_Ring", new Vector2(112f, 112f), Gold, GetCircleSprite());
        RectTransform iconMask = CreateImage(buttonRoot, "IconMask", new Vector2(92f, 92f), Color.white, GetCircleSprite());
        Mask mask = GetOrAdd<Mask>(iconMask.gameObject);
        mask.showMaskGraphic = false;
        Image icon = CreateImage(iconMask, "Img_MissionIcon", new Vector2(92f, 92f), new Color(1f, 0.83f, 0.25f, 1f), GetCircleSprite()).GetComponent<Image>();
        icon.preserveAspect = true;

        RectTransform sparkle = CreateImage(buttonRoot, "Small_Star_Badge", new Vector2(42f, 42f), Blue, GetCircleSprite());
        sparkle.anchoredPosition = new Vector2(35f, -43f);
        CreateText(sparkle, "Txt_Badge", "!", 28f, Color.white, new Vector2(32f, 32f), FontStyles.Bold);

        ProgressParts miniProgress = CreateProgressBar(buttonRoot, "MiniProgress", new Vector2(116f, 25f), new Vector2(0f, -74f), true);
        miniProgress.text.text = "1/2";

        return new ButtonParts
        {
            button = button,
            icon = icon,
            progressFill = miniProgress.fill,
            progressText = miniProgress.text
        };
    }

    private static BubbleParts CreateBubble(RectTransform root)
    {
        RectTransform bubbleRoot = CreateRect(root, "MissionBubble", new Vector2(520f, 250f));
        bubbleRoot.anchorMin = new Vector2(0f, 0.5f);
        bubbleRoot.anchorMax = new Vector2(0f, 0.5f);
        bubbleRoot.pivot = new Vector2(0f, 0.5f);
        bubbleRoot.anchoredPosition = new Vector2(150f, 34f);
        CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(bubbleRoot.gameObject);

        RectTransform tail = CreateImage(bubbleRoot, "BubbleTail", new Vector2(46f, 46f), Paper, GetRoundedSprite());
        tail.anchorMin = new Vector2(0f, 0.5f);
        tail.anchorMax = new Vector2(0f, 0.5f);
        tail.pivot = new Vector2(0.5f, 0.5f);
        tail.anchoredPosition = new Vector2(8f, 55f);
        tail.localEulerAngles = new Vector3(0f, 0f, 45f);

        RectTransform shadow = CreateImage(bubbleRoot, "BubbleShadow", new Vector2(506f, 218f), new Color(0f, 0f, 0f, 0.25f), GetRoundedSprite());
        shadow.anchorMin = new Vector2(0f, 0.5f);
        shadow.anchorMax = new Vector2(0f, 0.5f);
        shadow.pivot = new Vector2(0f, 0.5f);
        shadow.anchoredPosition = new Vector2(17f, -8f);

        RectTransform paper = CreateImage(bubbleRoot, "BubblePaper", new Vector2(506f, 218f), Paper, GetRoundedSprite());
        paper.anchorMin = new Vector2(0f, 0.5f);
        paper.anchorMax = new Vector2(0f, 0.5f);
        paper.pivot = new Vector2(0f, 0.5f);
        paper.anchoredPosition = new Vector2(12f, 0f);
        AddOutline(paper.gameObject, PaperLine, new Vector2(2f, -2f));

        RectTransform tab = CreateImage(bubbleRoot, "TitlePill_NewMission", new Vector2(220f, 56f), Blue, GetRoundedSprite());
        tab.anchorMin = new Vector2(0f, 1f);
        tab.anchorMax = new Vector2(0f, 1f);
        tab.pivot = new Vector2(0.5f, 0.5f);
        tab.anchoredPosition = new Vector2(183f, -9f);
        AddOutline(tab.gameObject, Color.white, new Vector2(3f, -3f));
        TMP_Text title = CreateText(tab, "Txt_Title", "Nhi\u1ec7m V\u1ee5 M\u1edbi", 28f, Color.white, new Vector2(205f, 42f), FontStyles.Bold);

        RectTransform card = CreateImage(paper, "MissionCard_Blue", new Vector2(466f, 92f), Blue, GetRoundedSprite());
        card.anchoredPosition = new Vector2(0f, 43f);
        AddOutline(card.gameObject, BlueDark, new Vector2(2f, -2f));

        RectTransform iconBack = CreateImage(card, "IconCircle_Back", new Vector2(84f, 84f), Gold, GetCircleSprite());
        iconBack.anchorMin = new Vector2(0f, 0.5f);
        iconBack.anchorMax = new Vector2(0f, 0.5f);
        iconBack.pivot = new Vector2(0.5f, 0.5f);
        iconBack.anchoredPosition = new Vector2(58f, 0f);
        Image icon = CreateImage(iconBack, "Img_MissionIcon", new Vector2(72f, 72f), new Color(1f, 0.83f, 0.25f, 1f), GetCircleSprite()).GetComponent<Image>();
        icon.preserveAspect = true;

        TMP_Text nameText = CreateText(card, "Txt_MissionName", "S\u1ea3n xu\u1ea5t 2 B\u00e1nh M\u00ec", 28f, Color.white, new Vector2(340f, 58f), FontStyles.Bold);
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(0f, 0.5f);
        nameRect.pivot = new Vector2(0f, 0.5f);
        nameRect.anchoredPosition = new Vector2(114f, 0f);
        nameText.alignment = TextAlignmentOptions.Left;

        ProgressParts progress = CreateProgressBar(paper, "MissionProgress", new Vector2(325f, 30f), new Vector2(-70f, -63f), false);
        progress.text.text = "1/2";

        RectTransform goRoot = CreateImage(paper, "Btn_GoMission", new Vector2(132f, 70f), Blue, GetRoundedSprite());
        goRoot.anchorMin = new Vector2(1f, 0f);
        goRoot.anchorMax = new Vector2(1f, 0f);
        goRoot.pivot = new Vector2(1f, 0.5f);
        goRoot.anchoredPosition = new Vector2(-28f, 58f);
        AddOutline(goRoot.gameObject, BlueDark, new Vector2(3f, -3f));
        Button goButton = GetOrAdd<Button>(goRoot.gameObject);
        goButton.transition = Selectable.Transition.ColorTint;
        goButton.targetGraphic = goRoot.GetComponent<Image>();
        TMP_Text goText = CreateText(goRoot, "Txt_Go", "\u0110\u1ebfn", 29f, Color.white, new Vector2(110f, 44f), FontStyles.Bold);

        return new BubbleParts
        {
            root = bubbleRoot,
            canvasGroup = canvasGroup,
            titleText = title,
            icon = icon,
            nameText = nameText,
            progressFill = progress.fill,
            progressText = progress.text,
            goButton = goButton,
            goButtonText = goText
        };
    }

    private static ProgressParts CreateProgressBar(RectTransform parent, string name, Vector2 size, Vector2 position, bool darkBlueFrame)
    {
        RectTransform root = CreateImage(parent, name, size, darkBlueFrame ? BlueDark : BarBack, GetRoundedSprite());
        root.anchoredPosition = position;
        AddOutline(root.gameObject, darkBlueFrame ? Color.white : new Color(0.34f, 0.31f, 0.18f, 0.7f), new Vector2(2f, -2f));

        RectTransform fillRect = CreateImage(root, "Fill", size - new Vector2(12f, 10f), Yellow, GetRoundedSprite());
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(6f, 0f);
        fillRect.offsetMin = new Vector2(6f, -(size.y - 10f) * 0.5f);
        fillRect.offsetMax = new Vector2(-6f, (size.y - 10f) * 0.5f);
        Image fill = fillRect.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0.5f;

        TMP_Text text = CreateText(root, "Txt_Progress", "1/2", darkBlueFrame ? 18f : 24f, Color.white, size, FontStyles.Bold);
        AddOutline(text.gameObject, new Color(0.12f, 0.08f, 0.05f, 0.9f), new Vector2(2f, -2f));

        return new ProgressParts { fill = fill, text = text };
    }

    private static void AssignReferences(MissionHudButtonUI hud, ButtonParts button, BubbleParts bubble)
    {
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("missionDatabase").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<MissionDatabase>(MainMissionDbPath);
        so.FindProperty("popupEwarManager").objectReferenceValue =
            Object.FindFirstObjectByType<PopupEwarManager>(FindObjectsInactive.Include);
        so.FindProperty("bubbleInitiallyVisible").boolValue = true;
        so.FindProperty("missionButton").objectReferenceValue = button.button;
        so.FindProperty("buttonIcon").objectReferenceValue = button.icon;
        so.FindProperty("buttonProgressFill").objectReferenceValue = button.progressFill;
        so.FindProperty("buttonProgressText").objectReferenceValue = button.progressText;
        so.FindProperty("bubbleRoot").objectReferenceValue = bubble.root;
        so.FindProperty("bubbleCanvasGroup").objectReferenceValue = bubble.canvasGroup;
        so.FindProperty("titleText").objectReferenceValue = bubble.titleText;
        so.FindProperty("missionIcon").objectReferenceValue = bubble.icon;
        so.FindProperty("missionNameText").objectReferenceValue = bubble.nameText;
        so.FindProperty("missionProgressFill").objectReferenceValue = bubble.progressFill;
        so.FindProperty("missionProgressText").objectReferenceValue = bubble.progressText;
        so.FindProperty("goButton").objectReferenceValue = bubble.goButton;
        so.FindProperty("goButtonText").objectReferenceValue = bubble.goButtonText;
        so.ApplyModifiedProperties();
    }

    private static Canvas FindCanvas(string canvasName)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
            if (canvas != null && canvas.name == canvasName)
                return canvas;
        return null;
    }

    private static RectTransform CreateImage(Transform parent, string name, Vector2 size, Color color, Sprite sprite)
    {
        RectTransform rect = CreateRect(parent, name, size);
        Image image = GetOrAdd<Image>(rect.gameObject);
        image.color = color;
        image.sprite = sprite;
        image.type = sprite == GetRoundedSprite() ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = sprite == GetCircleSprite();
        image.raycastTarget = true;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, Color color, Vector2 size, FontStyles style)
    {
        RectTransform rect = CreateRect(parent, name, size);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        rect.gameObject.layer = LayerMask.NameToLayer("UI");
        return rect;
    }

    private static void AddOutline(GameObject go, Color color, Vector2 distance)
    {
        Outline outline = GetOrAdd<Outline>(go);
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null)
            return component;
        return Undo.AddComponent<T>(go);
    }

    private static Sprite GetRoundedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static Sprite GetCircleSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    private struct ButtonParts
    {
        public Button button;
        public Image icon;
        public Image progressFill;
        public TMP_Text progressText;
    }

    private struct BubbleParts
    {
        public RectTransform root;
        public CanvasGroup canvasGroup;
        public TMP_Text titleText;
        public Image icon;
        public TMP_Text nameText;
        public Image progressFill;
        public TMP_Text progressText;
        public Button goButton;
        public TMP_Text goButtonText;
    }

    private struct ProgressParts
    {
        public Image fill;
        public TMP_Text text;
    }
}
