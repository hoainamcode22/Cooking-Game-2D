using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LevelUpPopupVisualDesignTool
{
    private const string MenuPath =
        "Tools/Farm Game/Setup Level Up Popup/Apply Reference Visual Design";

    private static readonly Color FrameShadow = new Color(0.02f, 0.08f, 0.11f, 0.78f);
    private static readonly Color FrameOuter = new Color(0.04f, 0.27f, 0.32f, 0.98f);
    private static readonly Color FrameInner = new Color(0.08f, 0.45f, 0.48f, 0.94f);
    private static readonly Color Cream = new Color(1f, 0.96f, 0.78f, 1f);
    private static readonly Color Gold = new Color(1f, 0.76f, 0.08f, 1f);
    private static readonly Color GoldDark = new Color(0.86f, 0.38f, 0.03f, 1f);
    private static readonly Color Coral = new Color(0.93f, 0.16f, 0.32f, 1f);
    private static readonly Color CoralDark = new Color(0.54f, 0.04f, 0.14f, 1f);
    private static readonly Color RewardPanel = new Color(0.03f, 0.12f, 0.15f, 0.82f);
    private static readonly Color RewardSlotColor = new Color(0.16f, 0.43f, 0.44f, 0.95f);
    private static readonly Color ClaimGreen = new Color(0.31f, 0.82f, 0.03f, 1f);
    private static readonly Color ClaimGreenDark = new Color(0.06f, 0.38f, 0.02f, 1f);

    [MenuItem(MenuPath)]
    public static void ApplyToOpenScene()
    {
        LevelUpPopupUI[] popups = Object.FindObjectsByType<LevelUpPopupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (popups.Length != 1)
        {
            EditorUtility.DisplayDialog(
                "Level-Up Visual Design",
                $"Can dung 1 LevelUpPopupUI trong scene, hien tim thay {popups.Length}.",
                "OK");
            return;
        }

        Apply(popups[0]);
        Selection.activeGameObject = popups[0].gameObject;
        EditorUtility.DisplayDialog(
            "Level-Up Visual Design",
            "Da dung xong popup mau.\n\n" +
            "Ban co the thay sprite tai cac object co ten Img_*_Placeholder.",
            "OK");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateApply() => !EditorApplication.isPlayingOrWillChangePlaymode;

    public static void Apply(LevelUpPopupUI popup)
    {
        if (popup == null) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Design Level-Up Popup");

        SerializedObject serializedPopup = new SerializedObject(popup);
        RectTransform content = serializedPopup.FindProperty("contentPanel").objectReferenceValue
            as RectTransform;
        if (content == null)
            content = popup.transform.Find("ContentPanel") as RectTransform;
        if (content == null)
        {
            Debug.LogError("[LevelUpPopupVisualDesignTool] ContentPanel not found.");
            return;
        }

        ClearChildren(content);
        ConfigureRect(content, new Vector2(760f, 860f), new Vector2(0f, -5f));
        Image contentImage = GetOrAdd<Image>(content.gameObject);
        contentImage.sprite = GetRoundedSprite();
        contentImage.type = Image.Type.Sliced;
        contentImage.color = Color.clear;
        contentImage.raycastTarget = false;

        CreateFrame(content);

        RectTransform badge = CreateImage(
            content,
            "Badge_EXP_Star_Placeholder",
            new Vector2(164f, 148f),
            new Vector2(0f, 365f),
            GoldDark,
            GetCircleSprite());
        CreateImage(
            badge,
            "Img_EXP_Icon_Placeholder",
            new Vector2(144f, 128f),
            Vector2.zero,
            Gold,
            GetCircleSprite());
        CreateText(
            badge,
            "Txt_EXP",
            "EXP",
            22f,
            new Vector2(0f, 30f),
            new Vector2(110f, 32f),
            Cream);
        TextMeshProUGUI levelNumber = CreateText(
            badge,
            "Txt_LevelNumber",
            "2",
            62f,
            new Vector2(0f, -14f),
            new Vector2(120f, 82f),
            Color.white);

        RectTransform avatarGlow = CreateImage(
            content,
            "Avatar_Glow",
            new Vector2(320f, 320f),
            new Vector2(0f, 185f),
            new Color(0.22f, 0.92f, 1f, 0.24f),
            GetCircleSprite());
        RectTransform avatarOuter = CreateImage(
            avatarGlow,
            "Avatar_Frame_Outer",
            new Vector2(286f, 286f),
            Vector2.zero,
            GoldDark,
            GetCircleSprite());
        RectTransform avatarInner = CreateImage(
            avatarOuter,
            "Avatar_Frame_Inner",
            new Vector2(264f, 264f),
            Vector2.zero,
            new Color(0.35f, 0.93f, 0.97f, 1f),
            GetCircleSprite());
        RectTransform avatarMask = CreateImage(
            avatarInner,
            "Avatar_Circle_Mask",
            new Vector2(238f, 238f),
            Vector2.zero,
            Color.white,
            GetCircleSprite());
        Mask mask = GetOrAdd<Mask>(avatarMask.gameObject);
        mask.showMaskGraphic = true;
        Image avatarPlaceholder = CreateImage(
            avatarMask,
            "Img_Avatar_Placeholder",
            new Vector2(238f, 238f),
            Vector2.zero,
            new Color(0.96f, 0.91f, 0.74f, 1f),
            null).GetComponent<Image>();
        avatarPlaceholder.preserveAspect = true;
        CreateText(
            avatarMask,
            "Txt_Avatar_Placeholder",
            "AVATAR",
            25f,
            Vector2.zero,
            new Vector2(180f, 50f),
            new Color(0.16f, 0.38f, 0.39f, 0.75f));

        RectTransform ribbonShadow = CreateImage(
            content,
            "Ribbon_LevelUp_Shadow",
            new Vector2(390f, 94f),
            new Vector2(0f, 39f),
            CoralDark,
            GetRoundedSprite());
        RectTransform ribbon = CreateImage(
            ribbonShadow,
            "Ribbon_LevelUp",
            new Vector2(374f, 80f),
            new Vector2(0f, 7f),
            Coral,
            GetRoundedSprite());
        TextMeshProUGUI title = CreateText(
            ribbon,
            "TitleText",
            "LÊN CẤP!",
            43f,
            Vector2.zero,
            new Vector2(340f, 62f),
            Cream);

        TextMeshProUGUI unlockText = CreateText(
            content,
            "UnlockDescText",
            "MỞ KHÓA PHẦN THƯỞNG MỚI",
            22f,
            new Vector2(0f, -32f),
            new Vector2(620f, 46f),
            Cream);

        RewardUi rewardUi = CreateRewardScroll(content);

        TextMeshProUGUI hint = CreateText(
            content,
            "HintText",
            "Vuốt ngang để xem tất cả phần thưởng",
            18f,
            new Vector2(0f, -286f),
            new Vector2(620f, 36f),
            new Color(0.88f, 1f, 0.94f, 0.9f));
        hint.fontStyle = FontStyles.Italic;

        Button claimButton = CreateClaimButton(content);

        serializedPopup.Update();
        serializedPopup.FindProperty("titleText").objectReferenceValue = title;
        serializedPopup.FindProperty("levelNumberText").objectReferenceValue = levelNumber;
        serializedPopup.FindProperty("hintText").objectReferenceValue = hint;
        serializedPopup.FindProperty("goldRewardRow").objectReferenceValue = rewardUi.goldRow;
        serializedPopup.FindProperty("goldRewardText").objectReferenceValue = rewardUi.goldText;
        serializedPopup.FindProperty("gemRewardRow").objectReferenceValue = rewardUi.gemRow;
        serializedPopup.FindProperty("gemRewardText").objectReferenceValue = rewardUi.gemText;
        serializedPopup.FindProperty("giftItemsContainer").objectReferenceValue = rewardUi.content;
        serializedPopup.FindProperty("unlockDescText").objectReferenceValue = unlockText;
        serializedPopup.FindProperty("claimButton").objectReferenceValue = claimButton;
        serializedPopup.FindProperty("contentPanel").objectReferenceValue = content;
        serializedPopup.ApplyModifiedProperties();

        EditorUtility.SetDirty(popup);
        EditorUtility.SetDirty(content);
        EditorSceneManager.MarkSceneDirty(popup.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[LevelUpPopupVisualDesignTool] Applied reference-inspired popup layout.");
    }

    private static void CreateFrame(RectTransform content)
    {
        CreateImage(
            content,
            "Frame_Shadow",
            new Vector2(734f, 794f),
            new Vector2(0f, -12f),
            FrameShadow,
            GetRoundedSprite());
        RectTransform outer = CreateImage(
            content,
            "Frame_Outer",
            new Vector2(718f, 786f),
            Vector2.zero,
            FrameOuter,
            GetRoundedSprite());
        CreateImage(
            outer,
            "Frame_Inner",
            new Vector2(684f, 750f),
            Vector2.zero,
            FrameInner,
            GetRoundedSprite());
        CreateImage(
            outer,
            "Frame_Highlight",
            new Vector2(650f, 716f),
            new Vector2(0f, 8f),
            new Color(0.36f, 0.83f, 0.76f, 0.14f),
            GetRoundedSprite());
    }

    private static RewardUi CreateRewardScroll(RectTransform content)
    {
        RectTransform panelShadow = CreateImage(
            content,
            "RewardPanel_Shadow",
            new Vector2(690f, 190f),
            new Vector2(0f, -174f),
            new Color(0f, 0f, 0f, 0.38f),
            GetRoundedSprite());
        RectTransform panel = CreateImage(
            panelShadow,
            "RewardPanel",
            new Vector2(678f, 178f),
            new Vector2(0f, 6f),
            RewardPanel,
            GetRoundedSprite());
        panel.GetComponent<Image>().raycastTarget = true;

        RectTransform viewport = CreateImage(
            panel,
            "Viewport",
            new Vector2(630f, 142f),
            Vector2.zero,
            new Color(1f, 1f, 1f, 0.03f),
            GetRoundedSprite());
        viewport.GetComponent<Image>().raycastTarget = true;
        GetOrAdd<RectMask2D>(viewport.gameObject);

        RectTransform scrollContent = CreateRect(
            viewport,
            "Content_HorizontalRewards",
            new Vector2(0f, 126f),
            new Vector2(0f, 0f));
        scrollContent.anchorMin = new Vector2(0f, 0.5f);
        scrollContent.anchorMax = new Vector2(0f, 0.5f);
        scrollContent.pivot = new Vector2(0f, 0.5f);

        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(scrollContent.gameObject);
        layout.padding = new RectOffset(12, 12, 6, 6);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(scrollContent.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scrollRect = GetOrAdd<ScrollRect>(panel.gameObject);
        scrollRect.content = scrollContent;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.12f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 32f;

        RewardSlot gold = CreateValueRewardSlot(
            scrollContent,
            "GoldRewardRow",
            "Img_GoldReward_Placeholder",
            "VÀNG",
            "+50",
            new Color(1f, 0.72f, 0.06f, 1f));
        RewardSlot gem = CreateValueRewardSlot(
            scrollContent,
            "GemRewardRow",
            "Img_GemReward_Placeholder",
            "KIM CƯƠNG",
            "+5",
            new Color(0.32f, 0.87f, 1f, 1f));

        for (int i = 1; i <= 6; i++)
            CreatePlaceholderRewardSlot(scrollContent, i);

        CreateText(
            panel,
            "Txt_ScrollHint_Left",
            "‹",
            42f,
            new Vector2(-326f, 0f),
            new Vector2(34f, 80f),
            Gold);
        CreateText(
            panel,
            "Txt_ScrollHint_Right",
            "›",
            42f,
            new Vector2(326f, 0f),
            new Vector2(34f, 80f),
            Gold);

        return new RewardUi
        {
            content = scrollContent,
            goldRow = gold.root.gameObject,
            goldText = gold.value,
            gemRow = gem.root.gameObject,
            gemText = gem.value
        };
    }

    private static RewardSlot CreateValueRewardSlot(
        RectTransform parent,
        string name,
        string iconName,
        string label,
        string value,
        Color accent)
    {
        RectTransform root = CreateImage(
            parent,
            name,
            new Vector2(116f, 116f),
            Vector2.zero,
            RewardSlotColor,
            GetRoundedSprite());
        AddLayoutElement(root, 116f, 116f);
        CreateImage(
            root,
            iconName,
            new Vector2(64f, 64f),
            new Vector2(0f, 17f),
            accent,
            GetCircleSprite());
        CreateText(
            root,
            "Txt_Label",
            label,
            13f,
            new Vector2(0f, -25f),
            new Vector2(106f, 22f),
            Cream);
        TextMeshProUGUI amount = CreateText(
            root,
            "Txt_Value",
            value,
            22f,
            new Vector2(0f, -45f),
            new Vector2(106f, 30f),
            Color.white);
        return new RewardSlot { root = root, value = amount };
    }

    private static void CreatePlaceholderRewardSlot(RectTransform parent, int index)
    {
        RectTransform root = CreateImage(
            parent,
            $"RewardSlot_{index:00}",
            new Vector2(116f, 116f),
            Vector2.zero,
            RewardSlotColor,
            GetRoundedSprite());
        AddLayoutElement(root, 116f, 116f);
        CreateImage(
            root,
            $"Img_Reward_{index:00}_Placeholder",
            new Vector2(76f, 76f),
            new Vector2(0f, 12f),
            new Color(1f, 1f, 1f, 0.18f),
            GetCircleSprite());
        CreateText(
            root,
            $"Txt_Reward_{index:00}_Placeholder",
            $"QUÀ {index}",
            15f,
            new Vector2(0f, -42f),
            new Vector2(102f, 28f),
            Cream);
    }

    private static Button CreateClaimButton(RectTransform content)
    {
        RectTransform shadow = CreateImage(
            content,
            "ClaimButton_Shadow",
            new Vector2(372f, 98f),
            new Vector2(0f, -363f),
            ClaimGreenDark,
            GetRoundedSprite());
        RectTransform buttonRoot = CreateImage(
            shadow,
            "ClaimButton",
            new Vector2(356f, 82f),
            new Vector2(0f, 8f),
            ClaimGreen,
            GetRoundedSprite());
        Image buttonImage = buttonRoot.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        Button button = GetOrAdd<Button>(buttonRoot.gameObject);
        button.targetGraphic = buttonImage;
        button.transition = Selectable.Transition.ColorTint;
        CreateText(
            buttonRoot,
            "ButtonText",
            "NHẬN QUÀ",
            36f,
            Vector2.zero,
            new Vector2(320f, 66f),
            Color.white);
        return button;
    }

    private static RectTransform CreateImage(
        Transform parent,
        string name,
        Vector2 size,
        Vector2 position,
        Color color,
        Sprite sprite)
    {
        RectTransform rect = CreateRect(parent, name, size, position);
        Image image = GetOrAdd<Image>(rect.gameObject);
        image.color = color;
        image.sprite = sprite;
        image.type = sprite == GetRoundedSprite() ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = sprite == GetCircleSprite();
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        RectTransform rect = CreateRect(parent, name, size, position);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        Outline outline = GetOrAdd<Outline>(rect.gameObject);
        outline.effectColor = new Color(0.02f, 0.08f, 0.1f, 0.78f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        return text;
    }

    private static RectTransform CreateRect(
        Transform parent,
        string name,
        Vector2 size,
        Vector2 position)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        Undo.RecordObject(rect, "Configure " + rect.name);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void AddLayoutElement(RectTransform rect, float width, float height)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(rect.gameObject);
        element.minWidth = width;
        element.preferredWidth = width;
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleWidth = 0f;
        element.flexibleHeight = 0f;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }

    private static Sprite GetRoundedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static Sprite GetCircleSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
    }

    private struct RewardUi
    {
        public RectTransform content;
        public GameObject goldRow;
        public TextMeshProUGUI goldText;
        public GameObject gemRow;
        public TextMeshProUGUI gemText;
    }

    private struct RewardSlot
    {
        public RectTransform root;
        public TextMeshProUGUI value;
    }
}
