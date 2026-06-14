using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TutorialFourPopupSetupTool
{
    private const string MenuPath = "Tools/Farm Game/Rebuild Tutorial 4 Popups";
    private const string FarmScenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
    private const string HandSpritePath = "Assets/_Game/Farm/Art/UI/tutorial_hand.png";
    private const string RiceDataPath = "Assets/_Game/Farm/data/Hat_giong/Crop_Rice.asset";
    private const string AutoRunKey = "Codex.TutorialFourPopups.v3";

    static TutorialFourPopupSetupTool()
    {
        EditorApplication.delayCall += TryAutoRunOnce;
    }

    [MenuItem(MenuPath)]
    public static void RebuildLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.GetActiveScene().path != FarmScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(FarmScenePath);
        }

        SetupTutorialL1L2Tool.RunSetupSilent();

        var board = Object.FindFirstObjectByType<TutorialGuideBoardUI>(
            FindObjectsInactive.Include);
        if (board == null)
        {
            Debug.LogError("[Tutorial4Popup] TutorialGuideBoardUI not found.");
            return;
        }

        Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>(HandSpritePath);
        CropData rice = AssetDatabase.LoadAssetAtPath<CropData>(RiceDataPath);
        Sprite plot = FindPlotSprite();
        Sprite gem = FindNamedSprite("btn_RutNang_TGCay", "Btn_gem", "GemBox");
        Sprite sickle = FindNamedSprite("Sickle_Icon", "SickleTool", "Sickle_Bottom_Tray");

        Transform oldContent = board.transform.Find("ContentPanel");
        if (oldContent != null) Undo.DestroyObjectImmediate(oldContent.gameObject);

        GameObject content = CreateUI("ContentPanel", board.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 0.5f);
        contentRt.sizeDelta = new Vector2(760f, 610f);
        contentRt.anchoredPosition = Vector2.zero;
        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.48f, 0.28f, 0.09f, 1f);

        GameObject pagesRoot = CreateUI("Popup_Pages", content.transform);
        Stretch(pagesRoot.GetComponent<RectTransform>(), 24f, 24f, 92f, 92f);

        var pages = new List<PageRefs>
        {
            BuildPlantPage(pagesRoot.transform, hand, plot, rice != null ? rice.icon : null),
            BuildSpeedPage(pagesRoot.transform, hand,
                rice != null ? rice.growingSprite : null, gem),
            BuildHarvestPage(pagesRoot.transform, hand,
                rice != null ? rice.readySprite : null, sickle),
            BuildResultPage(pagesRoot.transform,
                rice != null ? rice.harvestIcon : null,
                rice != null ? rice.icon : null),
        };

        Button confirm = BuildConfirmButton(content.transform);

        var so = new SerializedObject(board);
        so.FindProperty("rootPanel").objectReferenceValue = board.gameObject;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        SerializedProperty pageProp = so.FindProperty("popupPages");
        pageProp.arraySize = pages.Count;
        for (int i = 0; i < pages.Count; i++)
        {
            SerializedProperty page = pageProp.GetArrayElementAtIndex(i);
            page.FindPropertyRelative("stepName").stringValue = pages[i].stepName;
            page.FindPropertyRelative("root").objectReferenceValue = pages[i].root;
            page.FindPropertyRelative("animatedHand").objectReferenceValue = pages[i].hand;
            page.FindPropertyRelative("handFrom").objectReferenceValue = pages[i].from;
            page.FindPropertyRelative("handTo").objectReferenceValue = pages[i].to;
            page.FindPropertyRelative("travelDuration").floatValue = pages[i].travelDuration;
            page.FindPropertyRelative("pauseDuration").floatValue = 0.45f;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var page in pages) page.root.SetActive(false);
        board.gameObject.SetActive(false);
        EditorUtility.SetDirty(board);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = board.gameObject;
        Debug.Log("[Tutorial4Popup] Built 4 separate popup pages in SCN_Farm.");
    }

    private static PageRefs BuildPlantPage(
        Transform parent, Sprite hand, Sprite plot, Sprite seed)
    {
        GameObject page = BuildPageRoot(parent, "Popup_01_Plant_Rice",
            "BUOC 1 - TRONG LUA", "Cham o dat, sau do keo hat lua vao o.");
        RectTransform plotTarget = BuildTemplate(page.transform, "Template_01_Plot_Top",
            new Vector2(0f, 105f), new Vector2(460f, 150f), plot, "O DAT");
        RectTransform seedTarget = BuildTemplate(page.transform, "Template_02_SeedPanel_Bottom",
            new Vector2(0f, -90f), new Vector2(460f, 150f), seed, "PANEL HAT LUA");
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_03_GuideBoard", page, animatedHand,
            seedTarget, plotTarget, 0.75f);
    }

    private static PageRefs BuildSpeedPage(
        Transform parent, Sprite hand, Sprite crop, Sprite gem)
    {
        GameObject page = BuildPageRoot(parent, "Popup_02_Diamond_Process",
            "BUOC 2 - TANG TOC", "Cham o lua, sau do bam kim cuong de chin ngay.");
        RectTransform process = BuildTemplate(page.transform, "Template_Process_Diamond",
            new Vector2(0f, 15f), new Vector2(560f, 300f), crop, "PROCESS");
        RectTransform gemTarget = BuildBadge(process, "Diamond_Button", gem,
            new Vector2(155f, -70f), new Vector2(100f, 100f));
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_06b_GuideSpeedUp", page, animatedHand,
            gemTarget, gemTarget, 0.5f);
    }

    private static PageRefs BuildHarvestPage(
        Transform parent, Sprite hand, Sprite readyRice, Sprite sickle)
    {
        GameObject page = BuildPageRoot(parent, "Popup_03_Harvest_Sickle",
            "BUOC 3 - THU HOACH", "Cham lua chin, keo liem vao o lua de gat.");
        RectTransform riceTarget = BuildTemplate(page.transform, "Template_01_Ripe_Rice",
            new Vector2(0f, 105f), new Vector2(460f, 150f), readyRice, "LUA CHIN");
        RectTransform sickleTarget = BuildTemplate(page.transform, "Template_02_Drag_Sickle",
            new Vector2(0f, -90f), new Vector2(460f, 150f), sickle, "KEO LIEM");
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_08b_GuideHarvest", page, animatedHand,
            sickleTarget, riceTarget, 0.8f);
    }

    private static PageRefs BuildResultPage(
        Transform parent, Sprite harvest, Sprite inventory)
    {
        GameObject page = BuildPageRoot(parent, "Popup_04_Harvest_Result",
            "BUOC 4 - NHAN LUA", string.Empty);
        BuildTemplate(page.transform, "Image_Harvest_Drop",
            new Vector2(-145f, 10f), new Vector2(240f, 280f), harvest, string.Empty);
        BuildTemplate(page.transform, "Image_Rice_Collected",
            new Vector2(145f, 10f), new Vector2(240f, 280f), inventory, "x4");
        return new PageRefs("L1L2_09b_HarvestResult", page, null, null, null, 0f);
    }

    private static GameObject BuildPageRoot(
        Transform parent, string name, string title, string instruction)
    {
        GameObject page = CreateUI(name, parent);
        Stretch(page.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        TMP_Text titleText = BuildText(page.transform, "Title", title, 32f,
            new Vector2(0f, 205f), new Vector2(650f, 55f));
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.92f, 0.45f);

        if (!string.IsNullOrEmpty(instruction))
        {
            TMP_Text instructionText = BuildText(page.transform, "Instruction",
                instruction, 21f, new Vector2(0f, 160f), new Vector2(650f, 45f));
            instructionText.color = Color.white;
        }
        return page;
    }

    private static RectTransform BuildTemplate(
        Transform parent, string name, Vector2 position, Vector2 size,
        Sprite sprite, string label)
    {
        GameObject card = CreateUI(name, parent);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.94f, 0.82f, 0.55f, 1f);

        GameObject imageGo = CreateUI("Image", card.transform);
        RectTransform imageRt = imageGo.GetComponent<RectTransform>();
        imageRt.anchorMin = imageRt.anchorMax = new Vector2(0.5f, 0.5f);
        imageRt.anchoredPosition = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(0f, 16f);
        imageRt.sizeDelta = new Vector2(size.y * 0.72f, size.y * 0.72f);
        Image image = imageGo.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : new Color(0.55f, 0.38f, 0.18f);

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text labelText = BuildText(card.transform, "Label", label, 18f,
                new Vector2(0f, -size.y * 0.35f), new Vector2(size.x - 20f, 34f));
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color(0.28f, 0.14f, 0.04f);
        }
        return rt;
    }

    private static RectTransform BuildBadge(
        RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        GameObject badge = CreateUI(name, parent);
        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        Image image = badge.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : new Color(0.2f, 0.75f, 1f);
        return rt;
    }

    private static RectTransform BuildHand(Transform parent, Sprite sprite)
    {
        GameObject hand = CreateUI("Hand_Animated", parent);
        RectTransform rt = hand.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(92f, 92f);
        Image image = hand.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rt;
    }

    private static Button BuildConfirmButton(Transform parent)
    {
        GameObject buttonGo = CreateUI("ConfirmButton", parent);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 48f);
        rt.sizeDelta = new Vector2(270f, 64f);
        Image bg = buttonGo.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.68f, 0.2f);
        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = bg;

        TMP_Text text = BuildText(buttonGo.transform, "Text", "DA RO", 27f,
            Vector2.zero, rt.sizeDelta);
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        return button;
    }

    private static TMP_Text BuildText(
        Transform parent, string name, string value, float size,
        Vector2 position, Vector2 dimensions)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = dimensions;
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite FindPlotSprite()
    {
        var plots = Object.FindObjectsByType<PlotController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var plot in plots)
        {
            if (plot.Category != PlotCategory.Normal) continue;
            var so = new SerializedObject(plot);
            var prop = so.FindProperty("groundSprite");
            var renderer = prop?.objectReferenceValue as SpriteRenderer;
            if (renderer != null && renderer.sprite != null) return renderer.sprite;
        }
        return null;
    }

    private static Sprite FindNamedSprite(params string[] names)
    {
        var transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string name in names)
        {
            foreach (var transform in transforms)
            {
                if (transform.name != name) continue;
                Image image = transform.GetComponentInChildren<Image>(true);
                if (image != null && image.sprite != null) return image.sprite;
                SpriteRenderer renderer = transform.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null && renderer.sprite != null) return renderer.sprite;
            }
        }
        return null;
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    private static void Stretch(
        RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static void TryAutoRunOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorPrefs.GetBool(AutoRunKey, false)) return;
        if (EditorSceneManager.GetActiveScene().path != FarmScenePath) return;
        EditorPrefs.SetBool(AutoRunKey, true);
        RebuildLayout();
    }

    private readonly struct PageRefs
    {
        public readonly string stepName;
        public readonly GameObject root;
        public readonly RectTransform hand;
        public readonly RectTransform from;
        public readonly RectTransform to;
        public readonly float travelDuration;

        public PageRefs(string stepName, GameObject root, RectTransform hand,
            RectTransform from, RectTransform to, float travelDuration)
        {
            this.stepName = stepName;
            this.root = root;
            this.hand = hand;
            this.from = from;
            this.to = to;
            this.travelDuration = travelDuration;
        }
    }
}
