using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class TutorialHandFlowRebuildTool
{
    private const string MenuPath = "Tools/Farm Game/Rebuild Tutorial Hand Flow (One Click)";
    private const string FarmScenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
    private const string HandSpritePath = "Assets/_Game/Farm/Art/UI/tutorial_hand.png";
    private const string AutoRunKey = "Codex.TutorialHandFlowRebuild.v2";

    static TutorialHandFlowRebuildTool()
    {
        EditorApplication.delayCall += TryAutoRunOnce;
    }

    [MenuItem(MenuPath)]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        if (EditorSceneManager.GetActiveScene().path != FarmScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(FarmScenePath);
        }

        SetupTutorialL1L2Tool.RunSetupSilent();
        Sprite handSprite = ImportHandSprite();

        var manager = Object.FindFirstObjectByType<TutorialManager>(
            FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError("[TutorialHandTool] TutorialManager not found.");
            return;
        }

        var managerSo = new SerializedObject(manager);
        var handProp = managerSo.FindProperty("_handPointer");
        var clickHand = handProp?.objectReferenceValue as RectTransform;
        if (clickHand == null)
        {
            Debug.LogError("[TutorialHandTool] TutorialManager._handPointer is not assigned.");
            return;
        }

        Transform canvas = clickHand.parent;
        Transform root = canvas.Find("Tutorial_Hands");
        if (root == null)
        {
            var rootGo = new GameObject("Tutorial_Hands", typeof(RectTransform));
            rootGo.transform.SetParent(canvas, false);
            root = rootGo.transform;
            Undo.RegisterCreatedObjectUndo(rootGo, "Create Tutorial Hands");
        }

        clickHand.name = "Hand_Click_Plot";
        if (clickHand.parent != root) clickHand.SetParent(root, true);
        ConfigureHand(clickHand, handSprite);

        RectTransform dragHand = GetOrCloneHand(root, clickHand, "Hand_Drag_Seed");
        RectTransform actionHand = GetOrCloneHand(
            root, clickHand, "Hand_Action_Plot_Diamond_Sickle");
        ConfigureHand(dragHand, handSprite);
        ConfigureHand(actionHand, handSprite);

        var dragAnimator = manager.GetComponent<TutorialDragHintAnimator>();
        if (dragAnimator == null)
            dragAnimator = Undo.AddComponent<TutorialDragHintAnimator>(manager.gameObject);
        var dragSo = new SerializedObject(dragAnimator);
        dragSo.FindProperty("_hand").objectReferenceValue = dragHand;
        dragSo.ApplyModifiedPropertiesWithoutUndo();

        var actionGuide = manager.GetComponent<TutorialActionHandGuide>();
        if (actionGuide == null)
            actionGuide = Undo.AddComponent<TutorialActionHandGuide>(manager.gameObject);
        var actionSo = new SerializedObject(actionGuide);
        actionSo.FindProperty("_hand").objectReferenceValue = actionHand;
        actionSo.ApplyModifiedPropertiesWithoutUndo();

        var actionProp = managerSo.FindProperty("_actionHandGuide");
        if (actionProp != null) actionProp.objectReferenceValue = actionGuide;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        ForceStarterInventory(manager);
        NormalizeGuideBoard();
        TutorialFourPopupSetupTool.RebuildLayout();

        clickHand.gameObject.SetActive(false);
        dragHand.gameObject.SetActive(false);
        actionHand.gameObject.SetActive(false);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(dragAnimator);
        EditorUtility.SetDirty(actionGuide);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = root.gameObject;
        Debug.Log("[TutorialHandTool] Rebuilt 3 hands, 6 rice plots, 6 flower pots, "
            + "10 rice seeds and 10 sunflower seeds. SCN_Farm saved.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateRebuild() => !EditorApplication.isPlaying;

    private static void TryAutoRunOnce()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorPrefs.GetBool(AutoRunKey, false)) return;
        if (EditorSceneManager.GetActiveScene().path != FarmScenePath) return;

        EditorPrefs.SetBool(AutoRunKey, true);
        Rebuild();
    }

    private static Sprite ImportHandSprite()
    {
        AssetDatabase.ImportAsset(HandSpritePath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(HandSpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(HandSpritePath);
    }

    private static RectTransform GetOrCloneHand(
        Transform root, RectTransform source, string name)
    {
        Transform existing = root.Find(name);
        if (existing != null) return existing as RectTransform;

        var clone = Object.Instantiate(source.gameObject, root);
        clone.name = name;
        Undo.RegisterCreatedObjectUndo(clone, "Create " + name);
        foreach (var animator in clone.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;
        return clone.GetComponent<RectTransform>();
    }

    private static void ConfigureHand(RectTransform hand, Sprite sprite)
    {
        if (hand == null) return;
        foreach (var graphic in hand.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
            if (graphic is Image image && sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }
        EditorUtility.SetDirty(hand.gameObject);
    }

    private static void ForceStarterInventory(TutorialManager manager)
    {
        var starter = manager.GetComponent<StarterInventorySetup>();
        if (starter == null) starter = Undo.AddComponent<StarterInventorySetup>(manager.gameObject);

        var so = new SerializedObject(starter);
        var items = so.FindProperty("starterItems");
        if (items == null) return;
        while (items.arraySize < 2) items.InsertArrayElementAtIndex(items.arraySize);

        SetStarterItem(items.GetArrayElementAtIndex(0), "seed_rice", "Hat Lua", 10);
        SetStarterItem(items.GetArrayElementAtIndex(1), "seed_huong_duong",
            "Hat Hoa Huong Duong", 10);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(starter);
    }

    private static void SetStarterItem(
        SerializedProperty item, string id, string displayName, int amount)
    {
        item.FindPropertyRelative("itemId").stringValue = id;
        item.FindPropertyRelative("displayName").stringValue = displayName;
        item.FindPropertyRelative("amount").intValue = amount;
    }

    private static void NormalizeGuideBoard()
    {
        var guideBoard = Object.FindFirstObjectByType<TutorialGuideBoardUI>(
            FindObjectsInactive.Include);
        if (guideBoard == null) return;

        foreach (var text in guideBoard.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (text.transform.parent != null && text.transform.parent.name == "ConfirmButton")
                text.text = "Da hieu";
        guideBoard.gameObject.SetActive(false);
        EditorUtility.SetDirty(guideBoard.gameObject);
    }
}
