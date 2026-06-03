using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class JudgeAvatarHierarchyBuilder
{
    private const string FarmScenePath = "Assets/_Game/Scenes/SCN_Farm.unity";

    [MenuItem("Tools/Farm UI/Avatar/Build Task 1 In Current Scene")]
    public static void BuildInCurrentSceneMenu()
    {
        BuildInCurrentScene();
    }

    [MenuItem("Tools/Farm UI/Avatar/Build Task 1 In SCN_Farm")]
    public static void BuildInFarmScene()
    {
        var scene = EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Single);
        BuildInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[JudgeAvatarHierarchyBuilder] Built Task 1 avatar hierarchy in SCN_Farm.");
    }

    [MenuItem("Tools/Farm UI/Avatar/Build Task 2 Popup In Current Scene")]
    public static void BuildPopupInCurrentSceneMenu()
    {
        BuildPopupInCurrentScene();
    }

    [MenuItem("Tools/Farm UI/Avatar/Build Task 2 Popup In SCN_Farm")]
    public static void BuildPopupInFarmScene()
    {
        var scene = EditorSceneManager.OpenScene(FarmScenePath, OpenSceneMode.Single);
        BuildInCurrentScene();
        BuildPopupInCurrentScene();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[JudgeAvatarHierarchyBuilder] Built Task 2 avatar popup in SCN_Farm.");
    }

    public static void BuildInCurrentScene()
    {
        GameObject judgePanel = GameObject.Find("JudgePanel");
        if (judgePanel == null)
        {
            Debug.LogError("[JudgeAvatarHierarchyBuilder] Không tìm thấy JudgePanel trong scene.");
            return;
        }

        RectTransform judgeAvatar = FindDeepChild(judgePanel.transform, "JudgeAvatar") as RectTransform;
        if (judgeAvatar == null)
        {
            Debug.LogError("[JudgeAvatarHierarchyBuilder] Không tìm thấy JudgeAvatar trong JudgePanel.");
            return;
        }

        Image avatarFrame = judgeAvatar.GetComponent<Image>();
        RectTransform avatarImageRt = FindDeepChild(judgeAvatar, "Avata") as RectTransform;
        Image avatarImage = avatarImageRt != null ? avatarImageRt.GetComponent<Image>() : null;

        RectTransform circleFill = GetOrCreateUIChild(judgeAvatar, "Img_CircleExpFill");
        SetupStretchCenter(circleFill, new Vector2(185f, 185f), Vector2.zero);
        circleFill.SetSiblingIndex(0);

        Image circleFillImage = GetOrAdd<Image>(circleFill.gameObject);
        circleFillImage.color = new Color(1f, 0.72f, 0.08f, 0.75f);
        circleFillImage.raycastTarget = false;
        circleFillImage.type = Image.Type.Filled;
        circleFillImage.fillMethod = Image.FillMethod.Radial360;
        circleFillImage.fillOrigin = (int)Image.Origin360.Top;
        circleFillImage.fillClockwise = true;
        circleFillImage.fillAmount = 0.65f;
        circleFillImage.preserveAspect = true;

        RectTransform expLabel = GetOrCreateUIChild(judgeAvatar, "Txt_EXP");
        SetupStretchCenter(expLabel, new Vector2(80f, 24f), new Vector2(0f, 74f));

        TextMeshProUGUI expText = GetOrAdd<TextMeshProUGUI>(expLabel.gameObject);
        expText.text = "EXP";
        expText.fontSize = 20f;
        expText.fontStyle = FontStyles.Bold;
        expText.alignment = TextAlignmentOptions.Center;
        expText.color = Color.white;
        expText.raycastTarget = false;

        RectTransform hitArea = GetOrCreateUIChild(judgeAvatar, "Button_OpenAvatarProfile");
        hitArea.anchorMin = Vector2.zero;
        hitArea.anchorMax = Vector2.one;
        hitArea.offsetMin = Vector2.zero;
        hitArea.offsetMax = Vector2.zero;
        hitArea.SetAsLastSibling();

        Image hitImage = GetOrAdd<Image>(hitArea.gameObject);
        hitImage.color = new Color(1f, 1f, 1f, 0f);
        hitImage.raycastTarget = true;

        Button hitButton = GetOrAdd<Button>(hitArea.gameObject);
        hitButton.targetGraphic = hitImage;
        hitButton.transition = Selectable.Transition.None;

        JudgeAvatarProfileButton controller = GetOrAdd<JudgeAvatarProfileButton>(judgeAvatar.gameObject);
        TMP_Text levelText = FindLevelText(judgePanel.transform);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("openProfileButton").objectReferenceValue = hitButton;
        serialized.FindProperty("popupRoot").objectReferenceValue = null;
        serialized.FindProperty("avatarFrame").objectReferenceValue = avatarFrame;
        serialized.FindProperty("avatarImage").objectReferenceValue = avatarImage;
        serialized.FindProperty("circleExpFill").objectReferenceValue = circleFillImage;
        serialized.FindProperty("txtLevel").objectReferenceValue = levelText;
        serialized.FindProperty("txtExpLabel").objectReferenceValue = expText;
        serialized.FindProperty("expLabel").stringValue = "EXP";
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(judgePanel);
        EditorUtility.SetDirty(judgeAvatar.gameObject);
        Debug.Log("[JudgeAvatarHierarchyBuilder] JudgePanel avatar Task 1 hierarchy is ready.");
    }

    public static void BuildPopupInCurrentScene()
    {
        GameObject judgePanel = GameObject.Find("JudgePanel");
        if (judgePanel == null)
        {
            Debug.LogError("[JudgeAvatarHierarchyBuilder] Cannot find JudgePanel in scene.");
            return;
        }

        RectTransform judgeAvatar = FindDeepChild(judgePanel.transform, "JudgeAvatar") as RectTransform;
        if (judgeAvatar == null)
        {
            Debug.LogError("[JudgeAvatarHierarchyBuilder] Cannot find JudgeAvatar in JudgePanel.");
            return;
        }

        Image avatarImage = null;
        RectTransform avatarImageRt = FindDeepChild(judgeAvatar, "Avata") as RectTransform;
        if (avatarImageRt != null)
            avatarImage = avatarImageRt.GetComponent<Image>();

        AvatarProfilePopupUI popup = AvatarProfilePopupUI.FindOrCreate(avatarImage);
        if (popup == null)
            return;

        JudgeAvatarProfileButton button = judgeAvatar.GetComponent<JudgeAvatarProfileButton>();
        if (button != null)
        {
            SerializedObject serialized = new SerializedObject(button);
            serialized.FindProperty("popupRoot").objectReferenceValue = popup.gameObject;
            serialized.FindProperty("popupUI").objectReferenceValue = popup;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(button);
        }

        EditorUtility.SetDirty(popup.gameObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[JudgeAvatarHierarchyBuilder] Avatar profile popup hierarchy is ready.");
    }

    private static RectTransform GetOrCreateUIChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing as RectTransform;

        GameObject go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer));
        go.layer = parent.gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private static void SetupStretchCenter(RectTransform rt, Vector2 size, Vector2 anchoredPosition)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPosition;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static TMP_Text FindLevelText(Transform judgePanel)
    {
        Transform iconExp = FindDeepChild(judgePanel, "icon_exp");
        return iconExp != null ? iconExp.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
