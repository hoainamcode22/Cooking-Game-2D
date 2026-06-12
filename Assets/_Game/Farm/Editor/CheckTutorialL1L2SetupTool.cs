using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Tool: Tools/Farm Game/Test/Check Tutorial L1-L2 Setup
///
/// Kiem tra tat ca cac thanh phan tutorial L1-L2 (18 buoc).
/// Chay sau khi da chay "Setup Tutorial L1-L2".
/// </summary>
public static class CheckTutorialL1L2SetupTool
{
    private const string MENU         = "Tools/Farm Game/Test/Check Tutorial L1-L2 Setup";
    private const string STEPS_FOLDER = "Assets/Resources/TutorialSteps/L1_L2";

    private static int _pass;
    private static int _fail;
    private static int _warn;

    // 18 buoc - phai khop voi SetupTutorialL1L2Tool.Steps
    private static readonly string[] ExpectedStepFiles =
    {
        "L1L2_01_Welcome",
        "L1L2_02_ReadyQuestion",
        "L1L2_03_GuideBoard",
        "L1L2_04_FocusPlots",
        "L1L2_05_DragFirstRice",
        "L1L2_06_PlantAllRice",
        "L1L2_07_OpenCropProgress",
        "L1L2_08_SpeedUpTip",
        "L1L2_09_HarvestFirstRice",
        "L1L2_10_HarvestAllRice",
        "L1L2_11_TransitionFlower",
        "L1L2_12_FocusFlowerPots",
        "L1L2_13_DragFirstFlower",
        "L1L2_14_PlantAllFlowers",
        "L1L2_15_FlowerSpeedUp",
        "L1L2_16_HarvestFirstFlower",
        "L1L2_17_HarvestAllFlowers",
        "L1L2_18_LevelUpCelebration",
    };

    [MenuItem(MENU)]
    public static void RunCheck()
    {
        _pass = 0; _fail = 0; _warn = 0;

        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("  CHECK TUTORIAL L1-L2 SETUP (18 steps)");
        Debug.Log("═══════════════════════════════════════════════════");

        CheckStartupPopups();
        CheckSeedIds();
        CheckTutorialManager();
        CheckCameraFocus();
        CheckGuideBoardUI();
        CheckGuideBoardCanvas();
        CheckTutorialBridge();
        CheckRuntimeTargetsAndDragHint();
        CheckStepAssets();
        CheckDragHintStepData();
        CheckStepCountInManager();
        CheckStarterInventory();
        CheckLevelUpPopup();
        CheckLevelRewardL2();
        CheckFlowerPotsInScene();

        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log($"  KIEM TRA XONG: PASS={_pass}  WARN={_warn}  FAIL={_fail}");
        Debug.Log("═══════════════════════════════════════════════════");

        string canStart = (_fail == 0) ? "YES" : "NO (co FAIL)";
        Debug.Log($"  Tutorial can start: {canStart}");
        Debug.Log("═══════════════════════════════════════════════════");

        string summary = $"PASS: {_pass}  WARN: {_warn}  FAIL: {_fail}\n\n";
        summary += $"Tutorial co the chay: {canStart}\n\n";
        if (_fail > 0)
            summary += "Co LOI - xem Console de biet chi tiet.";
        else if (_warn > 0)
            summary += "Tat ca co nhung con canh bao - xem Console.";
        else
            summary += "Tat ca OK! Tutorial L1-L2 (18 buoc) san sang test.";

        EditorUtility.DisplayDialog("Check Tutorial L1-L2", summary, "OK");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    // =========================================================================
    // Checks
    // =========================================================================

    private static void CheckSeedIds()
    {
        const string RICE_ASSET     = "Assets/_Game/Farm/data/Hat_giong/Crop_Rice.asset";
        const string FLOWER_ASSET   = "Assets/_Game/Farm/data/Hạt Hoa/HuongDuong.asset";
        const string RICE_ALIAS     = "seed_rice";
        const string FLOWER_ALIAS   = "seed_huong_duong";

        // --- Rice ---
        var riceData = AssetDatabase.LoadAssetAtPath<CropData>(RICE_ASSET);
        if (riceData == null)
        {
            Fail($"[SeedIdScan] Crop_Rice.asset KHONG TIM THAY tai: {RICE_ASSET}");
        }
        else
        {
            Debug.Log($"  [SeedIdScan] Rice seed candidates:");
            Debug.Log($"    asset: {RICE_ASSET}");
            Debug.Log($"    cropId (dung trong SeedDragItem.CropId): {riceData.cropId}");
            Debug.Log($"    seedItemId (dung trong Warehouse): {riceData.seedItemId}");
            Debug.Log($"  [SeedIdScan] SELECTED rice seed id = {riceData.seedItemId}");

            // cropId phai nam trong RICE_ALIASES cua TutorialRuntimeTargetResolver
            string[] riceAliases = { "rice", "Rice", "lua", "Lua", "hat_lua", "seed_rice" };
            bool cropIdMatched = System.Array.Exists(riceAliases,
                a => string.Equals(a, riceData.cropId, System.StringComparison.OrdinalIgnoreCase));
            if (!cropIdMatched)
                Fail($"Rice cropId='{riceData.cropId}' KHONG nam trong RICE_ALIASES cua TutorialRuntimeTargetResolver — can cap nhat aliases!");
            else
                Pass($"Rice cropId='{riceData.cropId}' nam trong RICE_ALIASES OK");

            if (riceData.seedItemId != RICE_ALIAS)
                Fail($"Rice seedItemId='{riceData.seedItemId}' != '{RICE_ALIAS}' — StarterInventory va tutorial dang dung ID sai!");
            else
                Pass($"Rice seedItemId='{riceData.seedItemId}' (tutorial alias '{RICE_ALIAS}') OK");
        }

        // --- Sunflower ---
        var flowerData = AssetDatabase.LoadAssetAtPath<CropData>(FLOWER_ASSET);
        if (flowerData == null)
        {
            Fail($"[SeedIdScan] HuongDuong.asset KHONG TIM THAY tai: {FLOWER_ASSET}");
        }
        else
        {
            Debug.Log($"  [SeedIdScan] Sunflower seed candidates:");
            Debug.Log($"    asset: {FLOWER_ASSET}");
            Debug.Log($"    cropId (dung trong SeedDragItem.CropId): {flowerData.cropId}");
            Debug.Log($"    seedItemId (dung trong Warehouse): {flowerData.seedItemId}");
            Debug.Log($"  [SeedIdScan] SELECTED sunflower seed id = {flowerData.seedItemId}");

            string[] flowerAliases = { "huong_duong", "Huong_Duong", "hoa_huong_duong", "seed_huong_duong", "sunflower" };
            bool cropIdMatched = System.Array.Exists(flowerAliases,
                a => string.Equals(a, flowerData.cropId, System.StringComparison.OrdinalIgnoreCase));
            if (!cropIdMatched)
                Fail($"Sunflower cropId='{flowerData.cropId}' KHONG nam trong FLOWER_ALIASES cua TutorialRuntimeTargetResolver — can cap nhat aliases!");
            else
                Pass($"Sunflower cropId='{flowerData.cropId}' nam trong FLOWER_ALIASES OK");

            if (flowerData.seedItemId != FLOWER_ALIAS)
                Fail($"Sunflower seedItemId='{flowerData.seedItemId}' != '{FLOWER_ALIAS}' — StarterInventory va tutorial dang dung ID sai!");
            else
                Pass($"Sunflower seedItemId='{flowerData.seedItemId}' (tutorial alias '{FLOWER_ALIAS}') OK");
        }
    }

    private static void CheckStartupPopups()
    {
        bool marketActive    = DisableStartupPopupsTool.IsMarketPopupActiveAtStart();
        bool warehouseActive = DisableStartupPopupsTool.IsWarehousePopupActiveAtStart();

        if (marketActive)
            Fail("Startup popup: Canvas_MarketPopup popup root dang ACTIVE trong scene — se tu mo khi Play Mode! Chay 'Disable Startup Popups'.");
        else
            Pass("Startup popup: Market popup root INACTIVE OK");

        if (warehouseActive)
            Fail("Startup popup: Warehouse popup root dang ACTIVE trong scene — se tu mo khi Play Mode! Chay 'Disable Startup Popups'.");
        else
            Pass("Startup popup: Warehouse popup root INACTIVE OK");
    }

    private static void CheckCameraFocus()
    {
        var allFocus = Object.FindObjectsByType<TutorialCameraFocus>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allFocus.Length == 0)
        {
            Warn("TutorialCameraFocus: chua co trong scene — camera se khong tu dong di chuyen den o lua/chau hoa");
            return;
        }
        Pass("TutorialCameraFocus: " + allFocus[0].gameObject.name);

        // Check xem TutorialManager co reference hay khong
        var mgr = Object.FindFirstObjectByType<TutorialManager>();
        if (mgr == null) return;
        var so  = new SerializedObject(mgr);
        CheckRef(so, "_cameraFocus", "TutorialManager._cameraFocus", warn: true);
    }

    private static void CheckTutorialManager()
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>();
        if (mgr == null) { Fail("TutorialManager: KHONG TIM THAY trong scene!"); return; }
        Pass("TutorialManager: " + mgr.gameObject.name);

        var so = new SerializedObject(mgr);

        CheckRef(so, "_npcDialogPopup", "TutorialManager._npcDialogPopup", warn: true);
        CheckRef(so, "_npcDialogText",  "TutorialManager._npcDialogText",  warn: true);
        CheckRef(so, "_handPointer",    "TutorialManager._handPointer",    warn: true);
        CheckRef(so, "_npcPortrait",    "TutorialManager._npcPortrait",    warn: true);
        CheckRef(so, "_guideBoardUI",   "TutorialManager._guideBoardUI",   warn: false);
    }

    private static void CheckGuideBoardUI()
    {
        // Tìm cả inactive objects — GuideBoard thường SetActive(false) khi không hiện
        var all = Object.FindObjectsByType<TutorialGuideBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TutorialGuideBoardUI gb = all.Length > 0 ? all[0] : null;

        // Fallback: lấy từ TutorialManager reference
        if (gb == null)
        {
            var mgr = Object.FindFirstObjectByType<TutorialManager>();
            if (mgr != null)
            {
                var tmso = new SerializedObject(mgr);
                var gbRef = tmso.FindProperty("_guideBoardUI");
                if (gbRef != null) gb = gbRef.objectReferenceValue as TutorialGuideBoardUI;
            }
        }

        if (gb == null) { Fail("TutorialGuideBoardUI: KHONG TIM THAY — chay Setup Tutorial L1-L2 truoc!"); return; }
        Pass("TutorialGuideBoardUI: " + gb.gameObject.name);

        var so = new SerializedObject(gb);
        string[] iconFields = { "step1Icon", "step2Icon", "step3Icon", "step4Icon" };
        string[] iconNames  = { "Gieo Hat",  "Tang Toc",  "Thu Hoach", "Ket Qua"  };
        for (int i = 0; i < iconFields.Length; i++)
        {
            var prop = so.FindProperty(iconFields[i]);
            if (prop == null || prop.objectReferenceValue == null)
            {
                Warn($"GuideBoardUI.{iconFields[i]} ({iconNames[i]}): chua gan anh minh hoa — can gan thu cong");
                continue;
            }
            var img = prop.objectReferenceValue as UnityEngine.UI.Image;
            if (img != null && img.sprite == null)
                Warn($"GuideBoardUI.{iconFields[i]} ({iconNames[i]}): Image da gan nhung Sprite = null");
            else
                Pass($"GuideBoardUI.{iconFields[i]}: OK");
        }

        CheckRef(so, "confirmButton", "GuideBoardUI.confirmButton", warn: false);
    }

    private static void CheckGuideBoardCanvas()
    {
        var all = Object.FindObjectsByType<TutorialGuideBoardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length == 0) return; // Already failed in CheckGuideBoardUI

        var gb     = all[0];
        var canvas = gb.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Fail("GuideBoard canvas: KHONG CO Canvas cha — GuideBoard se khong hien!");
            return;
        }

        Debug.Log($"  [INFO] GuideBoard canvas: '{canvas.name}' | sortingOrder={canvas.sortingOrder} | renderMode={canvas.renderMode}");

        if (canvas.sortingOrder < 100)
            Warn($"GuideBoard canvas '{canvas.name}': sortingOrder={canvas.sortingOrder} — co the bi UI khac che (khuyen nghi >= 100). Chay Setup Tutorial L1-L2 de tao Canvas_TutorialOverlay.");
        else
            Pass($"GuideBoard canvas '{canvas.name}': sortingOrder={canvas.sortingOrder} OK");

        if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            Fail($"GuideBoard canvas '{canvas.name}': THIEU GraphicRaycaster — nut 'Bat dau trong' se khong click duoc!");
        else
            Pass($"GuideBoard canvas '{canvas.name}': co GraphicRaycaster OK");

        // RectTransform check
        var rt = gb.GetComponent<RectTransform>();
        if (rt != null)
        {
            bool stretchFull = Mathf.Approximately(rt.anchorMin.x, 0f) &&
                               Mathf.Approximately(rt.anchorMin.y, 0f) &&
                               Mathf.Approximately(rt.anchorMax.x, 1f) &&
                               Mathf.Approximately(rt.anchorMax.y, 1f);
            if (stretchFull)
                Pass("GuideBoard RectTransform: stretch full-screen (anchor 0,0 → 1,1) OK");
            else
                Warn($"GuideBoard RectTransform: anchor={rt.anchorMin}→{rt.anchorMax} — co the khong fill man hinh");
        }

        // CanvasGroup check
        var cg = gb.GetComponent<CanvasGroup>();
        if (cg == null)
            Warn("GuideBoard: khong co CanvasGroup — nen co de de dieu khien raycast");
        else
            Pass("GuideBoard: co CanvasGroup OK");

        // Button wiring check
        var so      = new SerializedObject(gb);
        var btnProp = so.FindProperty("confirmButton");
        if (btnProp == null || btnProp.objectReferenceValue == null)
            Fail("GuideBoard confirmButton: CHUA GAN — nut 'Bat dau trong' khong hoat dong!");
        else
            Pass("GuideBoard confirmButton: da gan OK (listener dang ky qua Awake runtime)");

        // rootPanel check
        var rootProp = so.FindProperty("rootPanel");
        if (rootProp == null || rootProp.objectReferenceValue == null)
            Warn("GuideBoard rootPanel: CHUA GAN — Show() se dung gameObject fallback");
        else
            Pass($"GuideBoard rootPanel: '{(rootProp.objectReferenceValue as GameObject)?.name}' OK");
    }

    private static void CheckTutorialBridge()
    {
        var bridge = Object.FindFirstObjectByType<TutorialStepTriggerBridge>();
        if (bridge == null) { Fail("TutorialStepTriggerBridge: KHONG TIM THAY — chay Setup Tutorial L1-L2!"); return; }
        Pass("TutorialStepTriggerBridge: " + bridge.gameObject.name);

        var so = new SerializedObject(bridge);

        var riceProp = so.FindProperty("tutorialPlots");
        if (riceProp == null || riceProp.arraySize == 0)
            Warn("bridge.tutorialPlots: TRONG — se dem moi Normal plot (co the sai so)");
        else if (riceProp.arraySize < 6)
            Warn($"bridge.tutorialPlots: chi co {riceProp.arraySize}/6 o lua");
        else
            Pass($"bridge.tutorialPlots: {riceProp.arraySize} o lua OK");

        var flowerProp = so.FindProperty("tutorialFlowerPots");
        if (flowerProp == null || flowerProp.arraySize == 0)
            Fail("bridge.tutorialFlowerPots: TRONG — PHAI GAN 2 chau hoa! Chay Setup Tutorial L1-L2.");
        else if (flowerProp.arraySize < 2)
            Fail($"bridge.tutorialFlowerPots: chi co {flowerProp.arraySize}/2 chau hoa!");
        else
            Pass($"bridge.tutorialFlowerPots: {flowerProp.arraySize} chau hoa OK");
    }

    private static void CheckStepAssets()
    {
        if (!AssetDatabase.IsValidFolder(STEPS_FOLDER))
        {
            Fail($"Thu muc step assets KHONG TON TAI: {STEPS_FOLDER}");
            return;
        }

        int found = 0;
        foreach (var name in ExpectedStepFiles)
        {
            string path  = $"{STEPS_FOLDER}/{name}.asset";
            var    asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            if (asset == null)
                Fail($"Step asset KHONG CO: {name}.asset");
            else
                found++;
        }

        if (found == ExpectedStepFiles.Length)
            Pass($"Tat ca {found}/{ExpectedStepFiles.Length} step assets ton tai");
        else
            Warn($"Chi tim thay {found}/{ExpectedStepFiles.Length} step assets");
    }

    private static void CheckStepCountInManager()
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>();
        if (mgr == null) return;

        var so = new SerializedObject(mgr);
        var stepsArr = so.FindProperty("_steps");
        if (stepsArr == null || stepsArr.arraySize == 0)
        {
            Fail("TutorialManager._steps: TRONG — chua keo step assets vao Inspector!");
            return;
        }
        if (stepsArr.arraySize < ExpectedStepFiles.Length)
        {
            Warn($"TutorialManager._steps: co {stepsArr.arraySize} (can {ExpectedStepFiles.Length}) — keo tat ca 18 step assets vao");
            return;
        }
        Pass($"TutorialManager._steps: {stepsArr.arraySize} steps da gan");
    }

    private static void CheckStarterInventory()
    {
        var starter = Object.FindFirstObjectByType<StarterInventorySetup>();
        if (starter == null)
        {
            Fail("StarterInventorySetup: KHONG TIM THAY — chay Setup Tutorial L1-L2 truoc!");
            return;
        }
        Pass("StarterInventorySetup: " + starter.gameObject.name);

        var so        = new SerializedObject(starter);
        var itemsProp = so.FindProperty("starterItems");
        if (itemsProp == null || itemsProp.arraySize == 0)
        {
            Fail("StarterInventorySetup.starterItems: TRONG — chua co hat giong starter!");
            return;
        }

        bool hasSeedRice    = false;
        bool hasSeedFlower  = false;
        bool missingRiceIcon    = false;
        bool missingFlowerIcon  = false;

        for (int i = 0; i < itemsProp.arraySize; i++)
        {
            var elem  = itemsProp.GetArrayElementAtIndex(i);
            string id = elem.FindPropertyRelative("itemId").stringValue;
            var icon  = elem.FindPropertyRelative("icon").objectReferenceValue;
            if (id == "seed_rice")         { hasSeedRice   = true; if (icon == null) missingRiceIcon   = true; }
            if (id == "seed_huong_duong")  { hasSeedFlower = true; if (icon == null) missingFlowerIcon = true; }
        }

        if (!hasSeedRice)   Fail("StarterInventorySetup: KHONG CO 'seed_rice' trong danh sach!");
        else                Pass("StarterInventorySetup: seed_rice OK");

        if (!hasSeedFlower) Fail("StarterInventorySetup: KHONG CO 'seed_huong_duong' trong danh sach!");
        else                Pass("StarterInventorySetup: seed_huong_duong OK");

        if (missingRiceIcon)
            Warn("StarterInventorySetup: seed_rice chua co icon sprite — can gan thu cong");
        if (missingFlowerIcon)
            Warn("StarterInventorySetup: seed_huong_duong chua co icon sprite — can gan thu cong");
    }

    private static void CheckLevelUpPopup()
    {
        // Tìm cả inactive — LevelUpPopup thường SetActive(false) khi chờ
        var all = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all.Length == 0)
        {
            Fail("LevelUpPopupUI: KHONG TIM THAY — chay Setup Tutorial L1-L2 se tu tao!");
            return;
        }
        Pass("LevelUpPopupUI: " + all[0].gameObject.name);
    }

    private static void CheckLevelRewardL2()
    {
        const string L2_PATH = "Assets/_Game/Farm/data/Lever Game/LevelReward_L2.asset";

        // Check 1: file asset có tồn tại không
        var l2Asset = AssetDatabase.LoadAssetAtPath<LevelRewardConfig>(L2_PATH);
        if (l2Asset == null)
        {
            Fail($"LevelReward_L2 asset: KHONG TIM THAY tai {L2_PATH}");
            return;
        }
        Pass("LevelReward_L2 asset: OK");

        // Check 2: đã gán vào LevelUpPopupUI chưa
        var all = Object.FindObjectsByType<LevelUpPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var popup = all.Length > 0 ? all[0] : null;
        if (popup == null) return;

        var so         = new SerializedObject(popup);
        var configList = so.FindProperty("levelRewardConfigs");
        if (configList == null) { Warn("LevelUpPopupUI.levelRewardConfigs: khong tim thay field"); return; }

        bool foundL2 = false;
        for (int i = 0; i < configList.arraySize; i++)
        {
            var cfg = configList.GetArrayElementAtIndex(i).objectReferenceValue as LevelRewardConfig;
            if (cfg != null && cfg.levelReached == 2) { foundL2 = true; break; }
        }

        if (!foundL2)
            Fail("LevelUpPopupUI.levelRewardConfigs: CHUA CO LevelReward_L2 — chay Setup Level Up Popup hoac Setup Tutorial L1-L2");
        else
            Pass("LevelUpPopupUI.levelRewardConfigs contains LevelReward_L2");
    }

    private static void CheckRuntimeTargetsAndDragHint()
    {
        var mgr = Object.FindFirstObjectByType<TutorialManager>();
        if (mgr == null) return;

        // TutorialRuntimeTargetResolver
        var resolver = mgr.GetComponent<TutorialRuntimeTargetResolver>();
        if (resolver == null)
            Fail("TutorialRuntimeTargetResolver: KHONG CO tren TutorialManager GO — chay Setup Tutorial L1-L2!");
        else
        {
            Pass("TutorialRuntimeTargetResolver: OK tren " + mgr.gameObject.name);

            var so = new SerializedObject(resolver);
            var cvProp = so.FindProperty("_tutorialCanvas");
            if (cvProp == null || cvProp.objectReferenceValue == null)
                Warn("TutorialRuntimeTargetResolver._tutorialCanvas: chua gan — chay Setup Tutorial L1-L2 lai");
            else
                Pass($"TutorialRuntimeTargetResolver._tutorialCanvas: '{(cvProp.objectReferenceValue as Canvas)?.name}' OK");
        }

        // Check wiring in TutorialManager
        var mgrSO = new SerializedObject(mgr);
        CheckRef(mgrSO, "_runtimeTargetResolver", "TutorialManager._runtimeTargetResolver", warn: false);

        // TutorialDragHintAnimator
        var dragHint = mgr.GetComponent<TutorialDragHintAnimator>();
        if (dragHint == null)
            Fail("TutorialDragHintAnimator: KHONG CO tren TutorialManager GO — chay Setup Tutorial L1-L2!");
        else
            Pass("TutorialDragHintAnimator: OK tren " + mgr.gameObject.name);

        CheckRef(mgrSO, "_dragHintAnimator", "TutorialManager._dragHintAnimator", warn: false);
    }

    private static void CheckDragHintStepData()
    {
        const string STEPS_PATH = "Assets/Resources/TutorialSteps/L1_L2";
        if (!AssetDatabase.IsValidFolder(STEPS_PATH)) return;

        // Check step 05 (DragFirstRice) has dragToTargetId = "tutorial_plot_01"
        var step05 = AssetDatabase.LoadAssetAtPath<TutorialStepData>($"{STEPS_PATH}/L1L2_05_DragFirstRice.asset");
        if (step05 != null)
        {
            if (string.IsNullOrEmpty(step05.dragToTargetId))
                Fail("L1L2_05_DragFirstRice.dragToTargetId: TRONG — chay Setup Tutorial L1-L2 de cap nhat!");
            else
                Pass($"L1L2_05_DragFirstRice.dragToTargetId: '{step05.dragToTargetId}' OK");
        }

        // Check step 13 (DragFirstFlower) has dragToTargetId = "tutorial_flower_01"
        var step13 = AssetDatabase.LoadAssetAtPath<TutorialStepData>($"{STEPS_PATH}/L1L2_13_DragFirstFlower.asset");
        if (step13 != null)
        {
            if (string.IsNullOrEmpty(step13.dragToTargetId))
                Fail("L1L2_13_DragFirstFlower.dragToTargetId: TRONG — chay Setup Tutorial L1-L2 de cap nhat!");
            else
                Pass($"L1L2_13_DragFirstFlower.dragToTargetId: '{step13.dragToTargetId}' OK");
        }
    }

    private static void CheckFlowerPotsInScene()
    {
        var allPlots = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        int flowerCount = 0;
        foreach (var p in allPlots)
            if (p.Category == PlotCategory.Flower) flowerCount++;

        if (flowerCount == 0)
            Fail("Flower plot (PlotCategory.Flower): KHONG CO trong scene — can dat Chauhoa_1.prefab + Chauhoa_2.prefab");
        else if (flowerCount < 2)
            Warn($"Flower plot: chi co {flowerCount}/2 chau hoa trong scene");
        else
            Pass($"Flower plot: {flowerCount} chau hoa trong scene OK");

        // Cach B: Neu bridge da co 6 rice + 2 flower thi khong bat buoc TutorialTarget
        // TutorialTarget chi can thiet neu hand pointer dung targetID string de tim UI element
        // Voi World Space object, bridge reference la du, khong can TutorialTarget component
        var bridge = Object.FindFirstObjectByType<TutorialStepTriggerBridge>();
        bool bridgeHasRice   = false;
        bool bridgeHasFlower = false;
        if (bridge != null)
        {
            var bso = new SerializedObject(bridge);
            var riceProp   = bso.FindProperty("tutorialPlots");
            var flowerProp = bso.FindProperty("tutorialFlowerPots");
            bridgeHasRice   = riceProp   != null && riceProp.arraySize   >= 6;
            bridgeHasFlower = flowerProp != null && flowerProp.arraySize >= 2;
        }

        if (bridgeHasRice && bridgeHasFlower)
        {
            Pass("TutorialTarget: bridge da co 6 lua + 2 hoa — hand pointer se dung bridge reference (OK)");
        }
        else
        {
            var targets = Object.FindObjectsByType<TutorialTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool hasPlot01   = false;
            bool hasFlower01 = false;
            foreach (var t in targets)
            {
                var tso  = new SerializedObject(t);
                string id = tso.FindProperty("targetID").stringValue;
                if (id == "tutorial_plot_01")   hasPlot01   = true;
                if (id == "tutorial_flower_01") hasFlower01 = true;
            }
            if (!hasPlot01)   Warn("TutorialTarget 'tutorial_plot_01'   : chua tim thay (OK neu bridge da assign)");
            if (!hasFlower01) Warn("TutorialTarget 'tutorial_flower_01' : chua tim thay (OK neu bridge da assign)");
        }
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static void CheckRef(SerializedObject so, string propName, string label, bool warn)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue == null)
        {
            if (warn) Warn($"{label}: CHUA GAN");
            else      Fail($"{label}: CHUA GAN");
        }
        else
        {
            Pass($"{label}: OK");
        }
    }

    private static void Pass(string msg) { _pass++; Debug.Log($"  [PASS] {msg}"); }
    private static void Warn(string msg) { _warn++; Debug.LogWarning($"  [WARN] {msg}"); }
    private static void Fail(string msg) { _fail++; Debug.LogError($"  [FAIL] {msg}"); }
}
