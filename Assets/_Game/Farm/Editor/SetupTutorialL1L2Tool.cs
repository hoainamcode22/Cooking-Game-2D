using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor Tool: Tools/Farm Game/Setup Tutorial L1-L2
///
/// Tạo/cập nhật toàn bộ hierarchy tutorial Level 1→2 trong scene.
/// Flow: Intro → Guide Board → 6 ô lúa → 2 chậu hoa hướng dương → Level Up.
/// Chạy nhiều lần an toàn (không tạo trùng).
///
/// EXP flow: 6 lúa×5 = 30 EXP + 2 hoa hướng dương×5 = 10 EXP = 40 EXP → Level 2
/// </summary>
public static class SetupTutorialL1L2Tool
{
    private const string MENU         = "Tools/Farm Game/Setup Tutorial L1-L2";
    private const string STEPS_FOLDER = "Assets/Resources/TutorialSteps/L1_L2";

    // =========================================================================
    // Lana VFX prefab paths
    // =========================================================================
    private const string LANA_CONFETTI_BLAST =
        "Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_blast_multicolor.prefab";
    private const string LANA_CONFETTI_DIR =
        "Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_directional_multicolor.prefab";
    private const string LANA_FLASH_MAGIC_BLUE =
        "Assets/Lana Studio/Hyper Casual FX/Prefabs/Flash/Flash_magic_blue_pink.prefab";

    // =========================================================================
    // Step Spec
    // =========================================================================
    private struct StepSpec
    {
        public string fileName;
        public string npcText;
        public string targetID;
        public TutorialWaitAction waitAction;
        public bool showHandPointer;
        public bool showGuideBoard;
        public Vector2 handOffset;
        public float typingSpeed;
        public string dragToTargetId; // target ID hand pointer kéo ĐẾN (để trống = no drag)
    }

    /// <summary>
    /// 18 bước tutorial L1→L2.
    /// Phase 1: Intro (01-04)
    /// Phase 2: Lúa trồng (05-08)
    /// Phase 3: Lúa thu hoạch (09-10)
    /// Phase 4: Hoa hướng dương trồng (11-14)
    /// Phase 5: Hoa thu hoạch + Level Up (15-18)
    /// EXP: 6 lúa×5=30 + 2 hoa×5=10 = 40 EXP = Level 2 exactly
    /// </summary>
    private static readonly StepSpec[] Steps = new StepSpec[]
    {
        // ── Phase 1: Intro ────────────────────────────────────────────────────
        new StepSpec
        {
            fileName       = "L1L2_01_Welcome",
            npcText        = "Chao mung ban da den voi nong trai!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_02_ReadyQuestion",
            npcText        = "Ban da san sang xay dung mot nong trai that dep cho rieng minh chua?",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_03_GuideBoard",
            npcText        = "",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = true,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_04_FocusPlots",
            npcText        = "Bat dau nhe! Truoc tien, chung ta se gieo hat lua tren nhung o dat nay.",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },

        // ── Phase 2: Lúa trồng ───────────────────────────────────────────────
        new StepSpec
        {
            fileName       = "L1L2_05_DragFirstRice",
            npcText        = "O day co 6 o dat. Hay keo hat lua tu bang hat giong vao o dat!",
            targetID       = "seed_rice",
            waitAction     = TutorialWaitAction.WaitForPlant,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(40f, -30f),
            typingSpeed    = 0.04f,
            dragToTargetId = "tutorial_plot_01",
        },
        new StepSpec
        {
            fileName       = "L1L2_06_PlantAllRice",
            npcText        = "Tot lam! Hay gieo hat cho ca 6 o dat nhe.",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForAllPlotsPlanted,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_07_OpenCropProgress",
            npcText        = "Bam vao o dat de xem cay dang lon.",
            targetID       = "tutorial_plot_01",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(30f, -20f),
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_08_SpeedUpTip",
            npcText        = "Ban co the dung kim cuong de hoan tat nhanh qua trinh nay.",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },

        // ── Phase 3: Lúa thu hoạch ───────────────────────────────────────────
        new StepSpec
        {
            fileName       = "L1L2_09_HarvestFirstRice",
            npcText        = "Lua da chin roi! Hay dung liem de thu hoach.",
            targetID       = "tutorial_plot_01",
            waitAction     = TutorialWaitAction.WaitForHarvest,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(40f, -30f),
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_10_HarvestAllRice",
            npcText        = "Tuyet voi! Ban da thu hoach xong vu lua dau tien.",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForAllPlotsHarvested,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },

        // ── Phase 4: Hoa hướng dương trồng ──────────────────────────────────
        new StepSpec
        {
            fileName       = "L1L2_11_TransitionFlower",
            npcText        = "Minh trong them hoa huong duong de nong trai dep hon nhe!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_12_FocusFlowerPots",
            npcText        = "Day la khu trong hoa. Hay keo hat hoa huong duong vao chau hoa!",
            targetID       = "seed_huong_duong",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(40f, -30f),
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_13_DragFirstFlower",
            npcText        = "Hay keo hat hoa huong duong vao chau hoa nao!",
            targetID       = "seed_huong_duong",
            waitAction     = TutorialWaitAction.WaitForPlant,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(40f, -30f),
            typingSpeed    = 0.04f,
            dragToTargetId = "tutorial_flower_01",
        },
        new StepSpec
        {
            fileName       = "L1L2_14_PlantAllFlowers",
            npcText        = "Trong them vao cac chau hoa con lai nua nhe!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForAllFlowerPlotsPlanted,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_15_FlowerSpeedUp",
            npcText        = "Dung kim cuong de hoan tat nhanh neu ban muon.",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },

        // ── Phase 5: Hoa thu hoạch + Level Up ────────────────────────────────
        new StepSpec
        {
            fileName       = "L1L2_16_HarvestFirstFlower",
            npcText        = "Hoa huong duong da no roi! Hay thu hoach nao.",
            targetID       = "tutorial_flower_01",
            waitAction     = TutorialWaitAction.WaitForHarvest,
            showHandPointer= true,
            showGuideBoard = false,
            handOffset     = new Vector2(40f, -30f),
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_17_HarvestAllFlowers",
            npcText        = "Thu hoach het cac chau hoa con lai de nhan EXP nhe!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForAllFlowerPlotsHarvested,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
        new StepSpec
        {
            fileName       = "L1L2_18_LevelUpCelebration",
            npcText        = "Gioi qua! Ban da du kinh nghiem de len cap 2. Nong trai cua ban dang phat trien roi!",
            targetID       = "",
            waitAction     = TutorialWaitAction.WaitForClick,
            showHandPointer= false,
            showGuideBoard = false,
            handOffset     = Vector2.zero,
            typingSpeed    = 0.04f,
        },
    };

    // =========================================================================
    // Menu Entry
    // =========================================================================
    [MenuItem(MENU)]
    public static void RunSetup()
    {
        int stepsMade = CreateStepAssets();   // 1. Tao file .asset
        AssignStepsToManager();               // 2. Gan 18 steps vao TutorialManager._steps
        SetupTutorialHierarchy();             // 3. GuideBoard, Bridge, plots
        SetupStarterInventory();              // 4. Hat giong starter
        LevelUpPopupSetupTool.EnsureExists(); // 5. LevelUp popup
        PrintExpReport();
        PrintFinalReport(stepsMade);

        EditorUtility.DisplayDialog("Setup Tutorial L1-L2",
            $"Hoan thanh!\n\n" +
            $"Step assets: {stepsMade}/18\n" +
            $"18 steps da gan vao TutorialManager._steps\n" +
            $"EXP: 6 lua x5=30 + 2 hoa x5=10 = 40 -> Level 2\n\n" +
            "Van can gan tay:\n" +
            "1. NPC portrait sprite\n" +
            "2. 4 anh minh hoa trong GuideBoard\n" +
            "3. Icon hat giong (seed_rice, seed_huong_duong)\n" +
            "4. Lana VFX prefabs vao LevelUpPopup\n" +
            "Xem Console de biet chi tiet.",
            "OK");
    }

    [MenuItem(MENU, true)]
    private static bool Validate() => !EditorApplication.isPlaying;

    // =========================================================================
    // Step Asset Creation
    // =========================================================================
    private static int CreateStepAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/TutorialSteps"))
            AssetDatabase.CreateFolder("Assets/Resources", "TutorialSteps");
        if (!AssetDatabase.IsValidFolder(STEPS_FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources/TutorialSteps", "L1_L2");

        int count = 0;
        foreach (var spec in Steps)
        {
            string path  = $"{STEPS_FOLDER}/{spec.fileName}.asset";
            var    step  = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            bool   isNew = (step == null);
            if (isNew) step = ScriptableObject.CreateInstance<TutorialStepData>();

            // Always set ALL fields so re-runs update existing assets (e.g. dragToTargetId)
            step.npcText         = spec.npcText;
            step.targetID        = spec.targetID;
            step.waitAction      = spec.waitAction;
            step.showHandPointer = spec.showHandPointer;
            step.showGuideBoard  = spec.showGuideBoard;
            step.handOffset      = spec.handOffset;
            step.typingSpeed     = spec.typingSpeed;
            step.dragToTargetId  = spec.dragToTargetId;

            if (isNew)
            {
                AssetDatabase.CreateAsset(step, path);
                Debug.Log($"[TutorialSetup] TAO: {path}");
            }
            else
            {
                EditorUtility.SetDirty(step);
                Debug.Log($"[TutorialSetup] UPDATE: {spec.fileName} (dragToTargetId='{spec.dragToTargetId}')");
            }
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("═══ STEP ORDER (keo theo thu tu nay vao TutorialManager._steps) ═══");
        for (int i = 0; i < Steps.Length; i++)
            Debug.Log($"  [{i:00}] {Steps[i].fileName,-35} waitAction={Steps[i].waitAction}");
        Debug.Log("═══════════════════════════════════════════════════════════════════");

        return count;
    }

    // =========================================================================
    // Assign 18 steps to TutorialManager._steps (auto, no drag needed)
    // =========================================================================
    private static void AssignStepsToManager()
    {
        var tutMgr = Object.FindFirstObjectByType<TutorialManager>();
        if (tutMgr == null)
        {
            Debug.LogWarning("[TutorialSetup] AssignSteps: TutorialManager khong tim thay trong scene!");
            return;
        }

        var so = new SerializedObject(tutMgr);
        var stepsProp = so.FindProperty("_steps");
        if (stepsProp == null)
        {
            Debug.LogError("[TutorialSetup] AssignSteps: khong tim thay field '_steps' tren TutorialManager. Kiem tra ten field?");
            return;
        }

        stepsProp.ClearArray();
        int assigned = 0;
        foreach (var spec in Steps)
        {
            string path  = $"{STEPS_FOLDER}/{spec.fileName}.asset";
            var    asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);
            if (asset == null)
            {
                Debug.LogError($"[TutorialSetup] AssignSteps: KHONG TIM THAY asset: {path}");
                continue;
            }
            stepsProp.InsertArrayElementAtIndex(assigned);
            stepsProp.GetArrayElementAtIndex(assigned).objectReferenceValue = asset;
            assigned++;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tutMgr);
        Debug.Log($"[TutorialSetup] Assigned {assigned} steps to TutorialManager._steps");
    }

    // =========================================================================
    // Hierarchy Setup
    // =========================================================================
    private static void SetupTutorialHierarchy()
    {
        var tutMgr = Object.FindFirstObjectByType<TutorialManager>();
        if (tutMgr == null)
        {
            Debug.LogWarning("[TutorialSetup] Khong tim thay TutorialManager trong scene!");
            return;
        }

        // TutorialStepTriggerBridge
        var bridge = tutMgr.GetComponent<TutorialStepTriggerBridge>();
        if (bridge == null)
        {
            bridge = tutMgr.gameObject.AddComponent<TutorialStepTriggerBridge>();
            Undo.RegisterCreatedObjectUndo(bridge, "Add TutorialStepTriggerBridge");
            Debug.Log("[TutorialSetup] THEM TutorialStepTriggerBridge -> " + tutMgr.gameObject.name);
        }

        // StarterInventorySetup
        var starter = tutMgr.GetComponent<StarterInventorySetup>();
        if (starter == null)
        {
            starter = tutMgr.gameObject.AddComponent<StarterInventorySetup>();
            Undo.RegisterCreatedObjectUndo(starter, "Add StarterInventorySetup");
            Debug.Log("[TutorialSetup] THEM StarterInventorySetup -> " + tutMgr.gameObject.name);
        }

        // TutorialCameraFocus
        var cameraFocus = tutMgr.GetComponent<TutorialCameraFocus>();
        if (cameraFocus == null)
        {
            cameraFocus = tutMgr.gameObject.AddComponent<TutorialCameraFocus>();
            Undo.RegisterCreatedObjectUndo(cameraFocus, "Add TutorialCameraFocus");
            Debug.Log("[TutorialSetup] THEM TutorialCameraFocus -> " + tutMgr.gameObject.name);
        }

        // Wire _cameraFocus vào TutorialManager
        var mgrSO  = new SerializedObject(tutMgr);
        var cfProp = mgrSO.FindProperty("_cameraFocus");
        if (cfProp != null && cfProp.objectReferenceValue == null)
        {
            cfProp.objectReferenceValue = cameraFocus;
            mgrSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(tutMgr);
            Debug.Log("[TutorialSetup] Wire TutorialManager._cameraFocus OK");
        }
        else if (cfProp == null)
        {
            Debug.LogWarning("[TutorialSetup] TutorialManager._cameraFocus field khong tim thay — kiem tra ten field?");
        }

        // TutorialRuntimeTargetResolver
        var resolver = tutMgr.GetComponent<TutorialRuntimeTargetResolver>();
        if (resolver == null)
        {
            resolver = tutMgr.gameObject.AddComponent<TutorialRuntimeTargetResolver>();
            Undo.RegisterCreatedObjectUndo(resolver, "Add TutorialRuntimeTargetResolver");
            Debug.Log("[TutorialSetup] THEM TutorialRuntimeTargetResolver -> " + tutMgr.gameObject.name);
        }

        // TutorialDragHintAnimator
        var dragHint = tutMgr.GetComponent<TutorialDragHintAnimator>();
        if (dragHint == null)
        {
            dragHint = tutMgr.gameObject.AddComponent<TutorialDragHintAnimator>();
            Undo.RegisterCreatedObjectUndo(dragHint, "Add TutorialDragHintAnimator");
            Debug.Log("[TutorialSetup] THEM TutorialDragHintAnimator -> " + tutMgr.gameObject.name);
        }

        // Wire resolver + dragHint vào TutorialManager
        var mgrSO2 = new SerializedObject(tutMgr);
        var resolverProp = mgrSO2.FindProperty("_runtimeTargetResolver");
        if (resolverProp != null && resolverProp.objectReferenceValue == null)
            resolverProp.objectReferenceValue = resolver;
        var dragHintProp = mgrSO2.FindProperty("_dragHintAnimator");
        if (dragHintProp != null && dragHintProp.objectReferenceValue == null)
            dragHintProp.objectReferenceValue = dragHint;
        mgrSO2.ApplyModifiedProperties();
        EditorUtility.SetDirty(tutMgr);
        Debug.Log("[TutorialSetup] Wire _runtimeTargetResolver + _dragHintAnimator OK");

        // Assign plots to bridge
        AssignFirst6PlotsTobridge(bridge);
        AssignFirst2FlowerPotsTobridge(bridge);

        // Setup GuideBoard UI
        Canvas tutCanvas = FindOrCreateGuideBoardCanvas();
        if (tutCanvas != null)
        {
            // Đảm bảo GraphicRaycaster tồn tại để button click được
            if (tutCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                tutCanvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log($"[TutorialSetup] THEM GraphicRaycaster vao canvas '{tutCanvas.name}'");
            }

            SetupGuideBoardUI(tutMgr, tutCanvas.transform);
            Debug.Log($"[TutorialSetup] GuideBoard canvas: '{tutCanvas.name}' sortingOrder={tutCanvas.sortingOrder}");

            // Wire resolver._tutorialCanvas for world-proxy creation
            var resolverSO   = new SerializedObject(resolver);
            var canvasProp   = resolverSO.FindProperty("_tutorialCanvas");
            if (canvasProp != null && canvasProp.objectReferenceValue == null)
            {
                canvasProp.objectReferenceValue = tutCanvas;
                resolverSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(resolver);
                Debug.Log($"[TutorialSetup] Wire resolver._tutorialCanvas -> '{tutCanvas.name}'");
            }
        }
        else
        {
            Debug.LogWarning("[TutorialSetup] Khong tim thay Tutorial Canvas. Can tao thu cong.");
        }

        // TutorialTarget on first flower pot
        AssignTutorialTargetToFirstFlowerPot();
        AssignTutorialTargetToFirstRicePlot();
    }

    // =========================================================================
    // StarterInventory default setup
    // =========================================================================
    private static void SetupStarterInventory()
    {
        var tutMgr = Object.FindFirstObjectByType<TutorialManager>();
        if (tutMgr == null) return;

        var starter = tutMgr.GetComponent<StarterInventorySetup>();
        if (starter == null) return;

        var so = new SerializedObject(starter);
        var itemsProp = so.FindProperty("starterItems");
        if (itemsProp == null) return;

        if (itemsProp.arraySize > 0)
        {
            Debug.Log("[TutorialSetup] StarterInventory da co items — khong ghi de.");
            return;
        }

        // item 0: seed_rice
        itemsProp.InsertArrayElementAtIndex(0);
        var rice = itemsProp.GetArrayElementAtIndex(0);
        rice.FindPropertyRelative("itemId").stringValue      = "seed_rice";
        rice.FindPropertyRelative("displayName").stringValue = "Hat Lua";
        rice.FindPropertyRelative("amount").intValue         = 10;

        // item 1: seed_huong_duong
        itemsProp.InsertArrayElementAtIndex(1);
        var flower = itemsProp.GetArrayElementAtIndex(1);
        flower.FindPropertyRelative("itemId").stringValue      = "seed_huong_duong";
        flower.FindPropertyRelative("displayName").stringValue = "Hat Hoa Huong Duong";
        flower.FindPropertyRelative("amount").intValue         = 10;

        so.ApplyModifiedProperties();
        Debug.Log("[TutorialSetup] Da cau hinh StarterInventory: 10 seed_rice + 10 seed_huong_duong");
        Debug.Log("[TutorialSetup] [CAN GAN ANH] Keo sprite icon vao StarterInventorySetup.starterItems[0].icon va [1].icon");
    }

    // =========================================================================
    // Guide Board UI
    // =========================================================================
    /// <summary>
    /// Tìm canvas phù hợp cho GuideBoard (ưu tiên sorting order cao).
    /// Tạo Canvas_TutorialOverlay (sortingOrder=9999) nếu không tìm thấy.
    /// </summary>
    private static Canvas FindOrCreateGuideBoardCanvas()
    {
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Priority 1: Canvas đã tạo từ lần chạy trước (Canvas_TutorialOverlay)
        foreach (var c in all)
            if (c.name == "Canvas_TutorialOverlay") return c;

        // Priority 2: Canvas có tên chứa "Tutorial"
        foreach (var c in all)
            if (c.name.Contains("Tutorial")) return c;

        // Priority 3: Canvas có sortingOrder cao nhất (>= 100 — đủ trên HUD)
        Canvas best = null; int bestOrder = 99;
        foreach (var c in all)
            if (c.sortingOrder > bestOrder) { bestOrder = c.sortingOrder; best = c; }
        if (best != null) return best;

        // Fallback: Tạo Canvas_TutorialOverlay riêng với sortingOrder 9999
        var canvasGo = new GameObject("Canvas_TutorialOverlay");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas_TutorialOverlay");
        Debug.Log("[TutorialSetup] TAO Canvas_TutorialOverlay (sortingOrder=9999) — se dung cho GuideBoard");
        return canvas;
    }

    private static void SetupGuideBoardUI(TutorialManager tutMgr, Transform canvasRoot)
    {
        const string GB_NAME = "Tutorial_GuideBoard";

        // Tìm existing GuideBoard trên MỌI canvas (không chỉ canvasRoot)
        var existingAll = Object.FindObjectsByType<TutorialGuideBoardUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existingAll.Length > 0)
        {
            var existingGb = existingAll[0];
            Debug.Log($"[TutorialSetup] Tutorial_GuideBoard da co tren canvas '{existingGb.transform.parent?.name}' — wire reference.");
            TryWireGuideBoardReference(tutMgr, existingGb);
            return;
        }

        // Cũng check theo tên trực tiếp trong canvasRoot
        var existingByName = canvasRoot.Find(GB_NAME);
        if (existingByName != null)
        {
            Debug.Log("[TutorialSetup] Tutorial_GuideBoard da co (by name) — wire reference.");
            TryWireGuideBoardReference(tutMgr, existingByName.GetComponent<TutorialGuideBoardUI>());
            return;
        }

        // Root overlay
        var rootGo = CreateUIObject(GB_NAME, canvasRoot, Vector2.zero, Vector2.one);
        rootGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        rootGo.AddComponent<CanvasGroup>();
        var gbComp = rootGo.AddComponent<TutorialGuideBoardUI>();

        // Content panel
        var contentGo = CreateUIObject("ContentPanel", rootGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        contentGo.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 580);
        contentGo.AddComponent<Image>().color = new Color(0.50f, 0.29f, 0.09f, 1f);

        // Title
        var titleGo = CreateUIObject("Title", contentGo.transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0, -46);
        titleRT.sizeDelta        = new Vector2(640, 64);
        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "TONG HOP HUONG DAN NONG TRAI";
        titleTxt.fontSize  = 32;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color     = new Color(1f, 0.95f, 0.65f);
        titleTxt.textWrappingMode = TextWrappingModes.Normal;

        // 4 step cards
        var cardsGo = CreateUIObject("StepCards", contentGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var cardsRT = cardsGo.GetComponent<RectTransform>();
        cardsRT.anchoredPosition = new Vector2(0, 18);
        cardsRT.sizeDelta        = new Vector2(660, 310);
        var hLayout = cardsGo.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment         = TextAnchor.MiddleCenter;
        hLayout.spacing                = 14;
        hLayout.childForceExpandWidth  = true;
        hLayout.childForceExpandHeight = false;
        hLayout.padding = new RectOffset(8, 8, 0, 0);

        string[] labels = { "1. GIEO HAT", "2. TANG TOC", "3. THU HOACH", "4. KET QUA" };
        var iconImages = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            var cardGo = CreateUIObject($"StepCard_{i + 1}", cardsGo.transform,
                Vector2.zero, Vector2.zero);
            cardGo.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 270);
            cardGo.AddComponent<Image>().color = new Color(0.42f, 0.24f, 0.07f, 1f);

            var vl = cardGo.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment         = TextAnchor.UpperCenter;
            vl.spacing                = 8;
            vl.padding                = new RectOffset(6, 6, 10, 6);
            vl.childForceExpandWidth  = true;
            vl.childForceExpandHeight = false;

            // Image placeholder
            var iconGo  = new GameObject("IllustrationImage", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(cardGo.transform, false);
            iconGo.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.color = new Color(1f, 1f, 1f, 0.12f);
            iconImg.preserveAspect = true;
            iconImages[i] = iconImg;
            var le = iconGo.AddComponent<LayoutElement>();
            le.preferredHeight = 130; le.flexibleWidth = 1;

            // Label
            var lblGo = new GameObject("StepLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(cardGo.transform, false);
            lblGo.GetComponent<RectTransform>().sizeDelta = new Vector2(138, 52);
            var lblTxt = lblGo.GetComponent<TextMeshProUGUI>();
            lblTxt.text             = labels[i];
            lblTxt.fontSize         = 18;
            lblTxt.fontStyle        = FontStyles.Bold;
            lblTxt.alignment        = TextAlignmentOptions.Center;
            lblTxt.color            = new Color(1f, 0.95f, 0.7f);
            lblTxt.textWrappingMode = TextWrappingModes.Normal;
            var lble = lblGo.AddComponent<LayoutElement>();
            lble.preferredHeight = 55; lble.flexibleWidth = 1;
        }

        // Confirm button
        var btnGo = CreateUIObject("ConfirmButton", contentGo.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        var btnRT = btnGo.GetComponent<RectTransform>();
        btnRT.anchoredPosition = new Vector2(0, 52);
        btnRT.sizeDelta        = new Vector2(260, 62);
        btnGo.AddComponent<Image>().color = new Color(0.13f, 0.62f, 0.24f);
        var btn = btnGo.AddComponent<Button>();

        var btnTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        var btnTxtRT = btnTxtGo.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero; btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.offsetMin = Vector2.zero; btnTxtRT.offsetMax = Vector2.zero;
        var btnTxt = btnTxtGo.GetComponent<TextMeshProUGUI>();
        btnTxt.text             = "Bat dau trong!";
        btnTxt.fontSize         = 26;
        btnTxt.fontStyle        = FontStyles.Bold;
        btnTxt.alignment        = TextAlignmentOptions.Center;
        btnTxt.color            = Color.white;
        btnTxt.textWrappingMode = TextWrappingModes.NoWrap;

        // Wire TutorialGuideBoardUI references
        var gbSO = new SerializedObject(gbComp);
        gbSO.FindProperty("rootPanel").objectReferenceValue     = rootGo;
        gbSO.FindProperty("step1Icon").objectReferenceValue     = iconImages[0];
        gbSO.FindProperty("step2Icon").objectReferenceValue     = iconImages[1];
        gbSO.FindProperty("step3Icon").objectReferenceValue     = iconImages[2];
        gbSO.FindProperty("step4Icon").objectReferenceValue     = iconImages[3];
        gbSO.FindProperty("confirmButton").objectReferenceValue = btn;
        gbSO.ApplyModifiedProperties();

        TryWireGuideBoardReference(tutMgr, gbComp);

        rootGo.SetActive(false);
        Undo.RegisterCreatedObjectUndo(rootGo, "Setup Tutorial_GuideBoard");
        Selection.activeGameObject = rootGo;

        Debug.Log("[TutorialSetup] TAO Tutorial_GuideBoard OK");
        Debug.Log("[TutorialSetup] [CAN GAN ANH] contentPanel/StepCards/StepCard_1..4/IllustrationImage.sprite");
    }

    private static void TryWireGuideBoardReference(TutorialManager tutMgr, TutorialGuideBoardUI gbComp)
    {
        if (gbComp == null) return;
        var tmSO   = new SerializedObject(tutMgr);
        var gbProp = tmSO.FindProperty("_guideBoardUI");
        if (gbProp != null && gbProp.objectReferenceValue == null)
        {
            gbProp.objectReferenceValue = gbComp;
            tmSO.ApplyModifiedProperties();
            Debug.Log("[TutorialSetup] Wire _guideBoardUI OK");
        }
    }

    // =========================================================================
    // Plot / Flower Pot Assignment
    // =========================================================================
    private static void AssignFirst6PlotsTobridge(TutorialStepTriggerBridge bridge)
    {
        var allPlots = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        if (allPlots.Length == 0) { Debug.LogWarning("[TutorialSetup] Khong tim thay PlotController nao."); return; }

        System.Array.Sort(allPlots, (a, b) => a.PlotId.CompareTo(b.PlotId));
        var first6 = new List<PlotController>();
        foreach (var p in allPlots)
        {
            if (p.Category == PlotCategory.Normal) first6.Add(p);
            if (first6.Count >= 6) break;
        }

        var so = new SerializedObject(bridge);
        var listProp = so.FindProperty("tutorialPlots");
        if (listProp == null) return;
        listProp.ClearArray();
        for (int i = 0; i < first6.Count; i++)
        {
            listProp.InsertArrayElementAtIndex(i);
            listProp.GetArrayElementAtIndex(i).objectReferenceValue = first6[i];
        }
        so.ApplyModifiedProperties();
        Debug.Log($"[TutorialSetup] {first6.Count} o lua gan vao bridge:");
        foreach (var p in first6) Debug.Log($"  - {p.gameObject.name} (id={p.PlotId})");
    }

    private static void AssignFirst2FlowerPotsTobridge(TutorialStepTriggerBridge bridge)
    {
        var allPlots = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        if (allPlots.Length == 0) return;

        System.Array.Sort(allPlots, (a, b) => a.PlotId.CompareTo(b.PlotId));
        var flowerPots = new List<PlotController>();
        foreach (var p in allPlots)
        {
            if (p.Category == PlotCategory.Flower) flowerPots.Add(p);
            if (flowerPots.Count >= 2) break;
        }

        if (flowerPots.Count == 0)
        {
            Debug.LogWarning("[TutorialSetup] Khong tim thay Flower plot (PlotCategory.Flower) trong scene!");
            Debug.LogWarning("[TutorialSetup] Kiem tra Chauhoa_1..4.prefab da dat vao scene chua?");
            return;
        }

        var so = new SerializedObject(bridge);
        var listProp = so.FindProperty("tutorialFlowerPots");
        if (listProp == null) return;
        listProp.ClearArray();
        for (int i = 0; i < flowerPots.Count; i++)
        {
            listProp.InsertArrayElementAtIndex(i);
            listProp.GetArrayElementAtIndex(i).objectReferenceValue = flowerPots[i];
        }
        so.ApplyModifiedProperties();
        Debug.Log($"[TutorialSetup] {flowerPots.Count} chau hoa gan vao bridge:");
        foreach (var p in flowerPots) Debug.Log($"  - {p.gameObject.name} (id={p.PlotId})");
    }

    private static void AssignTutorialTargetToFirstRicePlot()
    {
        var allPlots = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        if (allPlots.Length == 0) return;
        System.Array.Sort(allPlots, (a, b) => a.PlotId.CompareTo(b.PlotId));

        PlotController first = null;
        foreach (var p in allPlots) { if (p.Category == PlotCategory.Normal) { first = p; break; } }
        if (first == null) return;

        if (first.GetComponent<RectTransform>() == null)
        {
            Debug.Log($"[TutorialSetup] {first.gameObject.name} la World Space — TutorialTarget chi hoat dong tren UI Canvas. TargetID 'tutorial_plot_01' se bo qua.");
            return;
        }

        var t = first.GetComponent<TutorialTarget>() ?? first.gameObject.AddComponent<TutorialTarget>();
        var tso = new SerializedObject(t);
        tso.FindProperty("targetID").stringValue = "tutorial_plot_01";
        tso.ApplyModifiedProperties();
        Debug.Log($"[TutorialSetup] TutorialTarget 'tutorial_plot_01' -> {first.gameObject.name}");
    }

    private static void AssignTutorialTargetToFirstFlowerPot()
    {
        var allPlots = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        if (allPlots.Length == 0) return;
        System.Array.Sort(allPlots, (a, b) => a.PlotId.CompareTo(b.PlotId));

        PlotController first = null;
        foreach (var p in allPlots) { if (p.Category == PlotCategory.Flower) { first = p; break; } }
        if (first == null) { Debug.LogWarning("[TutorialSetup] Khong tim thay Flower plot de gan TutorialTarget."); return; }

        if (first.GetComponent<RectTransform>() == null)
        {
            Debug.Log($"[TutorialSetup] {first.gameObject.name} la World Space — TutorialTarget 'tutorial_flower_01' se bo qua.");
            return;
        }

        var t = first.GetComponent<TutorialTarget>() ?? first.gameObject.AddComponent<TutorialTarget>();
        var tso = new SerializedObject(t);
        tso.FindProperty("targetID").stringValue = "tutorial_flower_01";
        tso.ApplyModifiedProperties();
        Debug.Log($"[TutorialSetup] TutorialTarget 'tutorial_flower_01' -> {first.gameObject.name}");
    }

    // =========================================================================
    // EXP Report
    // =========================================================================
    private static void PrintExpReport()
    {
        const int EXP_NEEDED       = 40;
        const int EXP_PER_RICE     = 5;
        const int EXP_PER_FLOWER   = 5;
        const int RICE_PLOTS       = 6;
        const int FLOWER_POTS      = 2;
        int total = RICE_PLOTS * EXP_PER_RICE + FLOWER_POTS * EXP_PER_FLOWER;

        Debug.Log("═══ EXP REPORT L1→L2 ═══");
        Debug.Log($"  EXP can len Level 2        : {EXP_NEEDED}");
        Debug.Log($"  EXP 6 o lua harvest        : {RICE_PLOTS}×{EXP_PER_RICE} = {RICE_PLOTS * EXP_PER_RICE}");
        Debug.Log($"  EXP 2 chau hoa harvest     : {FLOWER_POTS}×{EXP_PER_FLOWER} = {FLOWER_POTS * EXP_PER_FLOWER}");
        Debug.Log($"  Tong EXP                   : {total}");
        Debug.Log(total >= EXP_NEEDED
            ? "  KET LUAN: DU EXP len Level 2! EXP du = " + (total - EXP_NEEDED)
            : $"  KET LUAN: CHUA DU — thieu {EXP_NEEDED - total} EXP");
        Debug.Log($"  ItemId hat lua             : seed_rice");
        Debug.Log($"  ItemId hat hoa huong duong : seed_huong_duong");
        Debug.Log($"  Chau hoa prefab            : Chauhoa_1..4.prefab (PlotId 21-24)");
        Debug.Log("═════════════════════════════");
    }

    // =========================================================================
    // Final Report
    // =========================================================================
    private static void PrintFinalReport(int stepCount)
    {
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log("[TutorialSetup] FINAL REPORT");
        Debug.Log("═════════════════════════════════════════════════════");
        Debug.Log($"Step assets: {STEPS_FOLDER}/ (18 steps: L1L2_01 → L1L2_18)");
        Debug.Log("");
        Debug.Log("CAN LAM THU CONG:");
        Debug.Log("  1. Keo 18 step asset vao TutorialManager._steps (thu tu 01→18)");
        Debug.Log("  2. Gan NPC portrait sprite -> TutorialManager._npcPortrait");
        Debug.Log("  3. Gan 4 anh minh hoa vao:");
        Debug.Log("       Tutorial_GuideBoard/ContentPanel/StepCards/StepCard_1/IllustrationImage");
        Debug.Log("       Tutorial_GuideBoard/ContentPanel/StepCards/StepCard_2/IllustrationImage");
        Debug.Log("       Tutorial_GuideBoard/ContentPanel/StepCards/StepCard_3/IllustrationImage");
        Debug.Log("       Tutorial_GuideBoard/ContentPanel/StepCards/StepCard_4/IllustrationImage");
        Debug.Log("  4. Gan icon cho StarterInventorySetup.starterItems[0].icon (hat lua)");
        Debug.Log("     va starterItems[1].icon (hat hoa huong duong)");
        Debug.Log("  5. Kiem tra Chauhoa_1.prefab da dat vao scene");
        Debug.Log("  6. Chay: Tools/Farm Game/Test/Check Tutorial L1-L2 Setup");
        Debug.Log("═════════════════════════════════════════════════════");
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    private static GameObject CreateUIObject(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }
}
