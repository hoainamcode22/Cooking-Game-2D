using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Level Up Popup
///
/// Tạo UI hierarchy cho LevelUpPopupUI trong scene.
/// Gọi EnsureExists() từ các tool khác (ví dụ SetupTutorialL1L2Tool) để tự tạo nếu chưa có.
/// </summary>
public static class LevelUpPopupSetupTool
{
    private const string MENU = "Tools/Farm Game/Setup Level Up Popup";
    private const string MENU_LANA_VFX = MENU + "/Integrate Lana VFX";
    private const string LANA_CONFETTI_SOURCE =
        "Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_blast_multicolor.prefab";
    private const string LANA_FLASH_SOURCE =
        "Assets/Lana Studio/Hyper Casual FX/Prefabs/Flash/Flash_magic_blue_pink.prefab";
    private const string LEVEL_UP_VFX_FOLDER =
        "Assets/_Game/Farm/Prefabs/VFX/LevelUp";
    private const string LEVEL_UP_CONFETTI =
        LEVEL_UP_VFX_FOLDER + "/LevelUp_Confetti_Lana02.prefab";
    private const string LEVEL_UP_FLASH =
        LEVEL_UP_VFX_FOLDER + "/LevelUp_Flash_Lana03.prefab";

    // =========================================================================
    // Menu entry (có dialog xác nhận)
    // =========================================================================
    [MenuItem(MENU)]
    public static void SetupLevelUpPopup()
    {
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Level Up Popup Setup",
                "Khong tim thay Canvas trong scene!\n\n" +
                "Hay chac chan scene dang co Canvas (thuong la Canvas_Popup) roi chay lai tool nay.",
                "OK");
            return;
        }

        // Kiểm tra đã tồn tại chưa
        var existingAll = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existingAll.Length > 0)
        {
            IntegrateLanaVfx(existingAll[0]);
            Selection.activeGameObject = existingAll[0].gameObject;
            EditorUtility.DisplayDialog("Level Up Popup Setup",
                "LevelUpPopup da ton tai.\n\n" +
                "Tool da giu nguyen popup va chi cap nhat hierarchy Lana VFX.",
                "OK");
            return;
        }

        var rootGo = CreatePopupHierarchy(targetCanvas);
        Undo.RegisterCreatedObjectUndo(rootGo, "Setup Level Up Popup");
        Selection.activeGameObject = rootGo;

        Debug.Log("[LevelUpPopupSetupTool] Tao LevelUpPopup thanh cong trong Canvas: " + targetCanvas.name);
        EditorUtility.DisplayDialog("Level Up Popup Setup",
            "Tao LevelUpPopup thanh cong!\n\n" +
            "Tiep theo:\n" +
            "1. Keo cac LevelRewardConfig asset (L2-L6) vao 'Level Reward Configs'\n" +
            "2. Keo Confetti_blast_multicolor (Lana Demo02) vao 'Vfx Confetti Prefab'\n" +
            "3. Keo Flash_magic_blue_pink (Lana Demo03) vao 'Vfx Side Prefab'\n" +
            "4. (Tuy chon) Tao prefab 'GiftItemSlot' va keo vao 'Gift Item Slot Prefab'",
            "OK");
    }

    [MenuItem(MENU, true)]
    private static bool ValidateSetup() => !EditorApplication.isPlaying;

    [MenuItem(MENU_LANA_VFX)]
    public static void IntegrateLanaVfxMenu()
    {
        var popups = Object.FindObjectsByType<LevelUpPopupUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        if (popups.Length != 1)
        {
            EditorUtility.DisplayDialog(
                "Integrate Lana VFX",
                $"Can dung 1 LevelUpPopupUI trong scene, hien tim thay: {popups.Length}.",
                "OK");
            return;
        }

        IntegrateLanaVfx(popups[0]);
        Selection.activeGameObject = popups[0].gameObject;
        EditorUtility.DisplayDialog(
            "Integrate Lana VFX",
            "Da cap nhat LanaDemo02 tren dau va LanaDemo03 hai ben popup.\n" +
            "Khong thay the LevelUpPopup va khong sua reward data.",
            "OK");
    }

    [MenuItem(MENU_LANA_VFX, true)]
    private static bool ValidateIntegrateLanaVfx() => !EditorApplication.isPlaying;

    // =========================================================================
    // EnsureExists: gọi từ SetupTutorialL1L2Tool — không dialog, không force replace
    // =========================================================================
    public static void EnsureExists()
    {
        var existingAll = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existingAll.Length > 0)
        {
            Debug.Log("[LevelUpPopupSetupTool] LevelUpPopupUI da co: " + existingAll[0].gameObject.name);
            IntegrateLanaVfx(existingAll[0]);
            TryWireLevelRewardL2(existingAll[0]);
            return;
        }

        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
        {
            Debug.LogWarning("[LevelUpPopupSetupTool] EnsureExists: Khong tim thay Canvas trong scene — LevelUpPopup chua duoc tao.");
            return;
        }

        var rootGo = CreatePopupHierarchy(targetCanvas);
        Undo.RegisterCreatedObjectUndo(rootGo, "Auto-Create Level Up Popup");
        Debug.Log("[LevelUpPopupSetupTool] EnsureExists: Da tao LevelUpPopup trong Canvas: " + targetCanvas.name);
    }

    // =========================================================================
    // Core: Tạo LevelUpPopup hierarchy + wire tất cả references
    // =========================================================================
    private static GameObject CreatePopupHierarchy(Canvas targetCanvas)
    {
        // Root + LevelUpPopupUI
        var rootGo  = CreateUIObject("LevelUpPopup", targetCanvas.transform, Vector2.zero, Vector2.one);
        var rootImg = rootGo.AddComponent<Image>();
        rootImg.color         = new Color(0f, 0f, 0f, 0.6f);
        rootImg.raycastTarget = true;

        var canvasGroup = rootGo.AddComponent<CanvasGroup>();
        var popupUI     = rootGo.AddComponent<LevelUpPopupUI>();

        // VFX background hierarchy. Runtime converts these UI anchors to camera space.
        var vfxBackground = CreateUIObject(
            "VFX_Background",
            rootGo.transform,
            Vector2.zero,
            Vector2.one);
        var vfxPoint = CreateUIObject(
            "VFX_Top_Lana02",
            vfxBackground.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        vfxPoint.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 370f);
        vfxPoint.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
        var vfxLeft = CreateUIObject(
            "VFX_Left_Lana03",
            vfxBackground.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        vfxLeft.GetComponent<RectTransform>().anchoredPosition = new Vector2(-390f, 70f);
        vfxLeft.GetComponent<RectTransform>().sizeDelta  = new Vector2(20, 20);
        var vfxRight = CreateUIObject(
            "VFX_Right_Lana03",
            vfxBackground.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        vfxRight.GetComponent<RectTransform>().anchoredPosition = new Vector2(390f, 70f);
        vfxRight.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);

        // Content panel
        var contentGo = CreateUIObject("ContentPanel", rootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        contentGo.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 600);
        contentGo.AddComponent<Image>().color = new Color(0.22f, 0.50f, 0.25f, 1f);

        // Title
        var titleGo = CreateUIObject("TitleText", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        titleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        titleGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(460, 70);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text             = "Len cap 2!";
        titleTxt.fontSize         = 42;
        titleTxt.fontStyle        = FontStyles.Bold;
        titleTxt.alignment        = TextAlignmentOptions.Center;
        titleTxt.color            = new Color(1f, 0.95f, 0.6f);
        titleTxt.textWrappingMode = TextWrappingModes.Normal;

        // Unlock desc
        var unlockGo = CreateUIObject("UnlockDescText", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        unlockGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -135);
        unlockGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(460, 45);
        var unlockTxt = unlockGo.AddComponent<TextMeshProUGUI>();
        unlockTxt.text             = "Mo khoa: Ngo, Ca chua";
        unlockTxt.fontSize         = 22;
        unlockTxt.alignment        = TextAlignmentOptions.Center;
        unlockTxt.color            = new Color(1f, 1f, 0.8f);
        unlockTxt.textWrappingMode = TextWrappingModes.Normal;

        // Gold row
        var goldRow = CreateUIObject("GoldRewardRow", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        goldRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
        goldRow.GetComponent<RectTransform>().sizeDelta        = new Vector2(350, 50);
        var goldHL = goldRow.AddComponent<HorizontalLayoutGroup>();
        goldHL.childAlignment = TextAnchor.MiddleCenter; goldHL.spacing = 12;

        var goldLblGo = new GameObject("GoldLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        goldLblGo.transform.SetParent(goldRow.transform, false);
        var goldLblTxt = goldLblGo.GetComponent<TextMeshProUGUI>();
        goldLblTxt.text = "Vang:"; goldLblTxt.fontSize = 24; goldLblTxt.color = Color.white;
        goldLblTxt.alignment = TextAlignmentOptions.Center;

        var goldValGo = new GameObject("GoldValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        goldValGo.transform.SetParent(goldRow.transform, false);
        var goldValTxt = goldValGo.GetComponent<TextMeshProUGUI>();
        goldValTxt.text = "+50"; goldValTxt.fontSize = 24; goldValTxt.fontStyle = FontStyles.Bold;
        goldValTxt.color = new Color(1f, 0.9f, 0.3f); goldValTxt.alignment = TextAlignmentOptions.Center;

        // Gem row
        var gemRow = CreateUIObject("GemRewardRow", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        gemRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -255);
        gemRow.GetComponent<RectTransform>().sizeDelta        = new Vector2(350, 50);
        var gemHL = gemRow.AddComponent<HorizontalLayoutGroup>();
        gemHL.childAlignment = TextAnchor.MiddleCenter; gemHL.spacing = 12;

        var gemLblGo = new GameObject("GemLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        gemLblGo.transform.SetParent(gemRow.transform, false);
        var gemLblTxt = gemLblGo.GetComponent<TextMeshProUGUI>();
        gemLblTxt.text = "Kim cuong:"; gemLblTxt.fontSize = 24; gemLblTxt.color = Color.white;
        gemLblTxt.alignment = TextAlignmentOptions.Center;

        var gemValGo = new GameObject("GemValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        gemValGo.transform.SetParent(gemRow.transform, false);
        var gemValTxt = gemValGo.GetComponent<TextMeshProUGUI>();
        gemValTxt.text = "+10"; gemValTxt.fontSize = 24; gemValTxt.fontStyle = FontStyles.Bold;
        gemValTxt.color = new Color(0.6f, 0.85f, 1f); gemValTxt.alignment = TextAlignmentOptions.Center;

        // Gift items container
        var giftContGo = CreateUIObject("GiftItemsContainer", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        var giftContRT = giftContGo.GetComponent<RectTransform>();
        giftContRT.anchoredPosition = new Vector2(0, -340);
        giftContRT.sizeDelta        = new Vector2(460, 90);
        var giftLayout = giftContGo.AddComponent<HorizontalLayoutGroup>();
        giftLayout.childAlignment        = TextAnchor.MiddleCenter;
        giftLayout.spacing               = 16;
        giftLayout.childForceExpandWidth  = false;
        giftLayout.childForceExpandHeight = false;

        // Hint text
        var hintGo = CreateUIObject("HintText", contentGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        hintGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -440);
        hintGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(460, 60);
        var hintTxt = hintGo.AddComponent<TextMeshProUGUI>();
        hintTxt.text             = "Tiep tuc trong trot va giao don hang!";
        hintTxt.fontSize         = 20;
        hintTxt.fontStyle        = FontStyles.Italic;
        hintTxt.alignment        = TextAlignmentOptions.Center;
        hintTxt.color            = new Color(0.9f, 1f, 0.85f);
        hintTxt.textWrappingMode = TextWrappingModes.Normal;

        // Claim button
        var btnGo = CreateUIObject("ClaimButton", contentGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        btnGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 60);
        btnGo.GetComponent<RectTransform>().sizeDelta        = new Vector2(280, 65);
        btnGo.AddComponent<Image>().color = new Color(0.9f, 0.65f, 0.1f);
        var btn = btnGo.AddComponent<Button>();

        var btnTxtGo = new GameObject("ButtonText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        btnTxtGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        btnTxtGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
        btnTxtGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        btnTxtGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        var btnTxt = btnTxtGo.GetComponent<TextMeshProUGUI>();
        btnTxt.text             = "Nhan Qua!";
        btnTxt.fontSize         = 28;
        btnTxt.fontStyle        = FontStyles.Bold;
        btnTxt.alignment        = TextAlignmentOptions.Center;
        btnTxt.color            = Color.white;
        btnTxt.textWrappingMode = TextWrappingModes.NoWrap;

        // Wire references
        var so = new SerializedObject(popupUI);
        so.FindProperty("popupRoot").objectReferenceValue      = rootGo;
        so.FindProperty("canvasGroup").objectReferenceValue    = canvasGroup;
        so.FindProperty("contentPanel").objectReferenceValue   = contentGo.GetComponent<RectTransform>();
        so.FindProperty("titleText").objectReferenceValue      = titleTxt;
        so.FindProperty("hintText").objectReferenceValue       = hintTxt;
        so.FindProperty("goldRewardRow").objectReferenceValue  = goldRow;
        so.FindProperty("goldRewardText").objectReferenceValue = goldValTxt;
        so.FindProperty("gemRewardRow").objectReferenceValue   = gemRow;
        so.FindProperty("gemRewardText").objectReferenceValue  = gemValTxt;
        so.FindProperty("giftItemsContainer").objectReferenceValue = giftContGo.transform;
        so.FindProperty("unlockDescText").objectReferenceValue = unlockTxt;
        so.FindProperty("claimButton").objectReferenceValue    = btn;
        so.FindProperty("vfxSpawnPoint").objectReferenceValue  = vfxPoint.transform;
        so.FindProperty("vfxLeftPoint").objectReferenceValue   = vfxLeft.transform;
        so.FindProperty("vfxRightPoint").objectReferenceValue  = vfxRight.transform;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(popupUI);

        // Wire Lana VFX prefabs nếu tìm được
        TryWireLanaVfx(popupUI);

        // Gán LevelReward_L2 config
        TryWireLevelRewardL2(popupUI);

        rootGo.SetActive(true);
        return rootGo;
    }

    // =========================================================================
    // Tự wire Lana VFX nếu tìm được trong project
    // =========================================================================
    private static void TryWireLanaVfx(LevelUpPopupUI popupUI)
    {
        EnsureGameOwnedVfxCopies();

        var so = new SerializedObject(popupUI);
        bool dirty = false;

        var confettiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEVEL_UP_CONFETTI);
        if (confettiPrefab != null)
        {
            var p = so.FindProperty("vfxConfettiPrefab");
            if (p != null && p.objectReferenceValue != confettiPrefab)
            {
                p.objectReferenceValue = confettiPrefab;
                dirty = true;
            }
            Debug.Log("[LevelUpPopupSetupTool] Wire game-owned LanaDemo02 confetti OK");
        }
        else Debug.LogWarning("[LevelUpPopupSetupTool] [WARN] Khong tim thay Lana Confetti: " + LEVEL_UP_CONFETTI);

        var flashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEVEL_UP_FLASH);
        if (flashPrefab != null)
        {
            var p = so.FindProperty("vfxSidePrefab");
            if (p != null && p.objectReferenceValue != flashPrefab)
            {
                p.objectReferenceValue = flashPrefab;
                dirty = true;
            }
            Debug.Log("[LevelUpPopupSetupTool] Wire game-owned LanaDemo03 flash OK");
        }
        else Debug.LogWarning("[LevelUpPopupSetupTool] [WARN] Khong tim thay Lana Flash: " + LEVEL_UP_FLASH);

        if (dirty) so.ApplyModifiedProperties();
    }

    private static void IntegrateLanaVfx(LevelUpPopupUI popupUI)
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Integrate Level Up Lana VFX");

        RectTransform root = popupUI.GetComponent<RectTransform>();
        RectTransform background = FindDirectChild(root, "VFX_Background");
        if (background == null)
        {
            GameObject go = CreateUIObject("VFX_Background", root, Vector2.zero, Vector2.one);
            Undo.RegisterCreatedObjectUndo(go, "Create VFX Background");
            background = go.GetComponent<RectTransform>();
        }

        RectTransform top = EnsureVfxAnchor(
            root,
            background,
            "VFX_Top_Lana02",
            "VFX_SpawnPoint",
            new Vector2(0f, 370f));
        RectTransform left = EnsureVfxAnchor(
            root,
            background,
            "VFX_Left_Lana03",
            null,
            new Vector2(-390f, 70f));
        RectTransform right = EnsureVfxAnchor(
            root,
            background,
            "VFX_Right_Lana03",
            null,
            new Vector2(390f, 70f));

        background.SetSiblingIndex(0);
        RectTransform content = FindDirectChild(root, "ContentPanel");
        if (content != null)
            content.SetSiblingIndex(1);

        var so = new SerializedObject(popupUI);
        so.FindProperty("vfxSpawnPoint").objectReferenceValue = top;
        so.FindProperty("vfxLeftPoint").objectReferenceValue = left;
        so.FindProperty("vfxRightPoint").objectReferenceValue = right;
        so.ApplyModifiedProperties();

        TryWireLanaVfx(popupUI);
        EditorUtility.SetDirty(popupUI);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static RectTransform EnsureVfxAnchor(
        RectTransform popupRoot,
        RectTransform background,
        string desiredName,
        string legacyName,
        Vector2 anchoredPosition)
    {
        RectTransform anchor = FindDirectChild(background, desiredName);
        if (anchor == null)
            anchor = FindDirectChild(popupRoot, desiredName);
        if (anchor == null && !string.IsNullOrEmpty(legacyName))
            anchor = FindDirectChild(popupRoot, legacyName);

        if (anchor == null)
        {
            GameObject go = CreateUIObject(
                desiredName,
                background,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
            Undo.RegisterCreatedObjectUndo(go, "Create " + desiredName);
            anchor = go.GetComponent<RectTransform>();
        }
        else
        {
            if (anchor.parent != background)
                Undo.SetTransformParent(anchor, background, "Move " + desiredName);
            if (anchor.name != desiredName)
            {
                Undo.RecordObject(anchor.gameObject, "Rename " + desiredName);
                anchor.name = desiredName;
            }
        }

        Undo.RecordObject(anchor, "Position " + desiredName);
        anchor.anchorMin = new Vector2(0.5f, 0.5f);
        anchor.anchorMax = new Vector2(0.5f, 0.5f);
        anchor.anchoredPosition = anchoredPosition;
        anchor.sizeDelta = new Vector2(20f, 20f);
        return anchor;
    }

    private static RectTransform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child as RectTransform;
        }
        return null;
    }

    private static void EnsureGameOwnedVfxCopies()
    {
        EnsureAssetFolder("Assets/_Game/Farm/Prefabs/VFX");
        EnsureAssetFolder(LEVEL_UP_VFX_FOLDER);

        CopyAssetIfMissing(LANA_CONFETTI_SOURCE, LEVEL_UP_CONFETTI);
        CopyAssetIfMissing(LANA_FLASH_SOURCE, LEVEL_UP_FLASH);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string folderName = path.Substring(separator + 1);
        EnsureAssetFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void CopyAssetIfMissing(string source, string destination)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(destination) != null) return;
        if (AssetDatabase.LoadAssetAtPath<Object>(source) == null)
        {
            Debug.LogWarning("[LevelUpPopupSetupTool] Source VFX not found: " + source);
            return;
        }

        if (!AssetDatabase.CopyAsset(source, destination))
            Debug.LogError("[LevelUpPopupSetupTool] Failed to copy VFX: " + destination);
    }

    // =========================================================================
    // Tự gán LevelReward_L2 vào levelRewardConfigs (idempotent)
    // =========================================================================
    private static void TryWireLevelRewardL2(LevelUpPopupUI popupUI)
    {
        const string L2_PATH = "Assets/_Game/Farm/data/Lever Game/LevelReward_L2.asset";

        var l2Config = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(L2_PATH);
        if (l2Config == null)
        {
            Debug.LogWarning("[LevelUpSetup] Khong tim thay LevelReward_L2.asset tai: " + L2_PATH);
            return;
        }

        var so         = new SerializedObject(popupUI);
        var configList = so.FindProperty("levelRewardConfigs");
        if (configList == null)
        {
            Debug.LogWarning("[LevelUpSetup] Khong tim thay field 'levelRewardConfigs' tren LevelUpPopupUI.");
            return;
        }

        // Kiểm tra đã có Level 2 chưa — không thêm trùng
        for (int i = 0; i < configList.arraySize; i++)
        {
            var existing = configList.GetArrayElementAtIndex(i).objectReferenceValue as LevelRewardConfig;
            if (existing != null && existing.levelReached == 2)
            {
                Debug.Log("[LevelUpSetup] LevelReward_L2 da co trong levelRewardConfigs — bo qua");
                return;
            }
        }

        int idx = configList.arraySize;
        configList.InsertArrayElementAtIndex(idx);
        configList.GetArrayElementAtIndex(idx).objectReferenceValue = l2Config;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(popupUI);
        Debug.Log("[LevelUpSetup] Assigned LevelReward_L2 to LevelUpPopupUI.levelRewardConfigs");
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private static Canvas FindTargetCanvas()
    {
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all) if (c.name == "Canvas_Popup") return c;
        foreach (var c in all) if (c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        return all.Length > 0 ? all[0] : null;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }
}
