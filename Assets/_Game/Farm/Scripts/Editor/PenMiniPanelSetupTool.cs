using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Tools/Farm/Pen Mini Panel Setup
/// Tự động tạo PF_PenMiniPanel.prefab và setup 4 pen prefabs.
/// </summary>
public class PenMiniPanelSetupTool : EditorWindow
{
    // ─── Paths ────────────────────────────────────────────────────────────────

    private const string PrefabOutputPath = "Assets/_Game/Farm/Prefabs/PF_PenMiniPanel.prefab";
    private const string PrefabsFolder    = "Assets/_Game/Farm/Prefabs";
    private const string TargetScene      = "Scene_Farm";

    // (prefabPath, configPath, penId, food1ItemId, food2ItemId)
    private static readonly (string prefab, string config, string penId,
                              string food1, string food2)[] PenData =
    {
        ("Assets/_Game/Farm/CÔNG TRÌNH/Pen_01.prefab",
         "Assets/_Game/Farm/Data/PenConfig/Config_Pen01_BoThit.asset",
         "pen_01", "rice", "ngo"),

        ("Assets/_Game/Farm/CÔNG TRÌNH/Pen_02.prefab",
         "Assets/_Game/Farm/Data/PenConfig/Config_Pen02_Heo.asset",
         "pen_02", "bapcai", "carot"),

        ("Assets/_Game/Farm/CÔNG TRÌNH/Pen_03.prefab",
         "Assets/_Game/Farm/Data/PenConfig/Config_Pen03_Ga.asset",
         "pen_03", "rice", "ngo"),

        ("Assets/_Game/Farm/CÔNG TRÌNH/Pen_04.prefab",
         "Assets/_Game/Farm/Data/PenConfig/Config_Pen04_BoSua.asset",
         "pen_04", "rice", "ngo"),
    };

    // (itemId, assetPath, isCropData)
    private static readonly (string id, string path, bool isCrop)[] IconSources =
    {
        ("rice",         "Assets/_Game/Farm/data/Item_Kho_Cook/Item_Rice.asset",        false),
        ("ngo",          "Assets/_Game/Farm/Data/Hat_giong/Ngo.asset",                  true),
        ("bapcai",       "Assets/_Game/Farm/Data/Hat_giong/BapCai.asset",               true),
        ("carot",        "Assets/_Game/Farm/data/Item_Kho_Cook/Item_Carot.asset",       false),
        ("beef",         "Assets/_Game/Farm/Data/Farm_dong_vat/Item_Beef.asset",        false),
        ("pork",         "Assets/_Game/Farm/data/Farm_dong_vat/Item_Pork.asset",        false),
        ("chicken_meat", "Assets/_Game/Farm/data/Farm_dong_vat/Item_ChickenMeat.asset", false),
        ("egg",          "Assets/_Game/Farm/data/Farm_dong_vat/Item_Egg.asset",         false),
        ("milk",         "Assets/_Game/Farm/Data/Farm_dong_vat/Item_Milk.asset",        false),
    };

    // ─── EditorWindow ─────────────────────────────────────────────────────────

    [MenuItem("Tools/Farm/Pen Mini Panel Setup")]
    public static void ShowWindow() =>
        GetWindow<PenMiniPanelSetupTool>("Pen Mini Panel Setup");

    private Vector2 _scroll;

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(4);
        GUILayout.Label("Pen Mini Panel Setup", EditorStyles.boldLabel);
        GUILayout.Label("Chạy tuần tự 1→4 hoặc nhấn Run All.", EditorStyles.miniLabel);
        EditorGUILayout.Space(8);

        DrawStep("1. Create PF_PenMiniPanel Prefab",
            "Tạo World-Space Canvas prefab 3 slot (food1, food2, basket) + progress overlay.",
            Step1_CreatePanelPrefab);

        DrawStep("2. Setup Pen Prefabs  (Pen_01..04)",
            "Thêm BoxCollider2D, PenClickDetector, PenDropTarget, nested panel vào mỗi pen.",
            Step2_SetupPenPrefabs);

        DrawStep("3. Auto-assign Icons to Configs",
            "Đọc icon từ InventoryItemData / CropData → gán food1Icon..basketIcon.",
            Step3_AssignIcons);

        DrawStep("4. Register Item_Milk → WarehousePopupUI",
            $"Mở Scene '{TargetScene}' trước. Thêm Item_Milk vào extraItemDatabase.",
            Step4_RegisterMilk);

        EditorGUILayout.Space(8);

        var prevColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.2f, 0.72f, 0.25f);
        if (GUILayout.Button("▶  Run All Steps", GUILayout.Height(42)))
            RunAllSteps();
        GUI.backgroundColor = prevColor;

        EditorGUILayout.Space(8);

        if (GUILayout.Button("✓  Validate Setup", GUILayout.Height(30)))
            ValidateSetup();

        EditorGUILayout.Space(4);
        EditorGUILayout.EndScrollView();
    }

    private static void DrawStep(string label, string desc, Action action)
    {
        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button(label, GUILayout.Height(28)))
            action();
        EditorGUILayout.LabelField(desc, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    // ─── Guards ───────────────────────────────────────────────────────────────

    private static bool IsPrefabModeActive()
    {
        if (PrefabStageUtility.GetCurrentPrefabStage() == null) return false;
        Debug.LogError("[PenSetup] ❌ Đang ở Prefab Mode — nhấn Esc thoát trước khi chạy tool.");
        return true;
    }

    private static bool IsTargetSceneLoaded()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.name == TargetScene && s.isLoaded) return true;
        }
        Debug.LogError($"[PenSetup] ❌ Scene '{TargetScene}' chưa mở. Mở scene đó trước.");
        return false;
    }

    // ─── Step 1: Create PF_PenMiniPanel Prefab ───────────────────────────────

    private static void Step1_CreatePanelPrefab()
    {
        if (IsPrefabModeActive()) return;

        if (File.Exists(Path.GetFullPath(PrefabOutputPath)))
        {
            bool ok = EditorUtility.DisplayDialog("Overwrite?",
                $"Prefab đã tồn tại:\n{PrefabOutputPath}\n\nGhi đè?", "Ghi đè", "Hủy");
            if (!ok) return;
        }

        EnsureFolder(PrefabsFolder);

        GameObject root = BuildPanelHierarchy();
        var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabOutputPath);
        DestroyImmediate(root);
        AssetDatabase.Refresh();

        if (saved != null)
            Debug.Log($"[PenSetup] ✅ Step 1 — Prefab lưu: {PrefabOutputPath}");
        else
            Debug.LogError("[PenSetup] ❌ Step 1 FAIL — SaveAsPrefabAsset trả null.");
    }

    private static GameObject BuildPanelHierarchy()
    {
        // ── Root ─────────────────────────────────────────────────────────────
        var root = new GameObject("PF_PenMiniPanel");

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        ApplyCanvasSorting(canvas, "CongTrinh", 600);
        root.AddComponent<GraphicRaycaster>();

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400f, 150f);
        root.transform.localScale = Vector3.one * 0.01f;

        // BoxCollider2D — world-space bounds = (400 * 0.01, 150 * 0.01) = (4, 1.5)
        var col = root.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(4f, 1.5f);
        col.offset = Vector2.zero;

        var panelUI = root.AddComponent<PenMiniPanelUI>();

        // ── PanelContent (= panelRoot) — wrapper ẩn/hiện toàn bộ panel ────────
        // Tất cả visual elements là con của PanelContent.
        // PenMiniPanelUI (trên root) luôn active để timer coroutine tiếp tục chạy.
        var panelContent = new GameObject("PanelContent");
        panelContent.transform.SetParent(root.transform, false);
        var pcRect = panelContent.AddComponent<RectTransform>();
        StretchFill(pcRect);
        panelContent.SetActive(false); // ẩn mặc định khi load scene

        // ── Background (child của PanelContent) ──────────────────────────────
        var bg = MakeImage("Background", panelContent.transform, new Color(0.15f, 0.12f, 0.08f, 0.92f));
        StretchFill(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().raycastTarget = true;

        // ── 3 Slots (thủ công — không dùng HLG để tránh runtime layout phụ thuộc) ─
        // Vị trí: food1 X=-130, food2 X=0, basket X=+130, Y=0
        var (slot1, icon1, amt1) = MakeFoodSlot("Slot_Food1", panelContent.transform, -130f);
        var drag1 = slot1.AddComponent<DraggableFeedItem>();
        SetSerializedRef(drag1, "imgFeedIcon",   icon1);
        SetSerializedRef(drag1, "txtFeedAmount", amt1);

        var (slot2, icon2, amt2) = MakeFoodSlot("Slot_Food2", panelContent.transform, 0f);
        var drag2 = slot2.AddComponent<DraggableFeedItem>();
        SetSerializedRef(drag2, "imgFeedIcon",   icon2);
        SetSerializedRef(drag2, "txtFeedAmount", amt2);

        var (slotBasket, iconBasket, glow) = MakeBasketSlot(panelContent.transform, 130f);
        var basketDrag = slotBasket.AddComponent<PenBasketDragItem>();
        SetSerializedRef(basketDrag, "basketImage", iconBasket);

        // ── Progress Overlay (ẩn mặc định, child của PanelContent) ──────────
        var overlay = MakeImage("ProgressOverlay", panelContent.transform, new Color(0f, 0f, 0f, 0.72f));
        StretchFill(overlay.GetComponent<RectTransform>());
        overlay.SetActive(false);

        var fillGO = MakeImage("ProgressFill", overlay.transform,
            new Color(0.25f, 0.82f, 0.35f, 0.9f));
        var fillImg = fillGO.GetComponent<Image>();
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.05f, 0.15f);
        fillRect.anchorMax = new Vector2(0.95f, 0.42f);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        var timerGO   = new GameObject("TimerText");
        timerGO.transform.SetParent(overlay.transform, false);
        var timerTxt  = timerGO.AddComponent<TextMeshProUGUI>();
        timerTxt.text      = "0:00";
        timerTxt.fontSize  = 30f;
        timerTxt.alignment = TextAlignmentOptions.Center;
        timerTxt.color     = Color.white;
        var timerRect = timerGO.GetComponent<RectTransform>();
        timerRect.anchorMin       = new Vector2(0.1f, 0.48f);
        timerRect.anchorMax       = new Vector2(0.9f, 0.92f);
        timerRect.offsetMin       = timerRect.offsetMax = Vector2.zero;

        // ── Bind PenMiniPanelUI references via SerializedObject ───────────────
        var so = new SerializedObject(panelUI);
        so.FindProperty("panelRoot").objectReferenceValue       = panelContent;
        so.FindProperty("slot1Root").objectReferenceValue       = slot1;
        so.FindProperty("slot1Icon").objectReferenceValue       = icon1;
        so.FindProperty("slot1Amount").objectReferenceValue     = amt1;
        so.FindProperty("slot2Root").objectReferenceValue       = slot2;
        so.FindProperty("slot2Icon").objectReferenceValue       = icon2;
        so.FindProperty("slot2Amount").objectReferenceValue     = amt2;
        so.FindProperty("basketRoot").objectReferenceValue      = slotBasket;
        so.FindProperty("basketIcon").objectReferenceValue      = iconBasket;
        so.FindProperty("basketActiveGlow").objectReferenceValue = glow;
        so.FindProperty("progressOverlay").objectReferenceValue = overlay;
        so.FindProperty("progressFill").objectReferenceValue    = fillImg;
        so.FindProperty("progressLabel").objectReferenceValue   = timerTxt;
        so.FindProperty("panelCollider").objectReferenceValue   = col;
        so.ApplyModifiedProperties();

        return root;
    }

    // ─── Step 2: Setup Pen Prefabs ────────────────────────────────────────────

    private static void Step2_SetupPenPrefabs()
    {
        if (IsPrefabModeActive()) return;

        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabOutputPath);
        if (panelPrefab == null)
        {
            Debug.LogError($"[PenSetup] ❌ PF_PenMiniPanel.prefab không tồn tại ({PrefabOutputPath}). Chạy Step 1 trước.");
            return;
        }

        foreach (var (path, cfgPath, penId, f1, f2) in PenData)
            SetupOnePen(path, cfgPath, penId, f1, f2, panelPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PenSetup] ✅ Step 2 — 4 pen đã được setup.");
    }

    private static void SetupOnePen(string penPath, string cfgPath, string penId,
        string food1Id, string food2Id, GameObject panelPrefab)
    {
        var config = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(cfgPath);
        if (config == null)
        {
            Debug.LogError($"[PenSetup] Config không tìm thấy: {cfgPath}");
            return;
        }

        // BS3 — Prefab Mode guard đã ở đầu Step2, nhưng check thêm per-pen để chắc
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogError("[PenSetup] ❌ Đang ở Prefab Mode — thoát Prefab Mode trước.");
            return;
        }

        var penRoot = PrefabUtility.LoadPrefabContents(penPath);
        if (penRoot == null)
        {
            Debug.LogError($"[PenSetup] Không load được prefab: {penPath}");
            return;
        }

        try
        {
            // ── Idempotency: kiểm tra child PF_PenMiniPanel đã có chưa ─────────
            Transform existingPanelTrans = penRoot.transform.Find("PF_PenMiniPanel");
            PenMiniPanelUI panelUI;

            if (existingPanelTrans == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, penRoot.transform);
                instance.transform.localPosition = new Vector3(1.5f, 1.0f, 0f);
                panelUI = instance.GetComponent<PenMiniPanelUI>();
                Debug.Log($"[PenSetup] {penId}: Instantiated PF_PenMiniPanel child.");
            }
            else
            {
                panelUI = existingPanelTrans.GetComponent<PenMiniPanelUI>();
                Debug.Log($"[PenSetup] {penId}: Child đã có — re-bind.");
            }

            if (panelUI == null)
            {
                Debug.LogError($"[PenSetup] {penId}: Không tìm thấy PenMiniPanelUI!");
                return;
            }

            // Config → PenMiniPanelUI.config
            var panelSO = new SerializedObject(panelUI);
            panelSO.FindProperty("config").objectReferenceValue = config;
            panelSO.ApplyModifiedProperties();

            // ── Collider2D trên pen root ──────────────────────────────────────
            Collider2D existingCol = penRoot.GetComponent<Collider2D>();
            BoxCollider2D penBox;

            if (existingCol == null)
            {
                penBox = penRoot.AddComponent<BoxCollider2D>();
                penBox.size   = new Vector2(3f, 3f);
                penBox.offset = Vector2.zero;
                Debug.Log($"[PenSetup] {penId}: BoxCollider2D added (size 3×3 — điều chỉnh trong prefab nếu cần).");
            }
            else
            {
                penBox = existingCol as BoxCollider2D;
                if (penBox == null)
                    Debug.LogWarning($"[PenSetup] {penId}: Collider đã là {existingCol.GetType().Name} — giữ nguyên.");
                else
                    Debug.Log($"[PenSetup] {penId}: BoxCollider2D đã tồn tại — giữ nguyên.");
            }

            Collider2D finalCol = (Collider2D)penBox ?? existingCol;

            // ── PenClickDetector (runtime type lookup — avoids compile-time coupling) ──
            var cdType    = FindRuntimeType("PenClickDetector");
            Component detector = null;
            if (cdType != null)
            {
                detector = penRoot.GetComponent(cdType);
                if (detector == null) detector = penRoot.AddComponent(cdType);
            }
            if (detector != null)
            {
                var cdSO = new SerializedObject(detector);
                cdSO.FindProperty("miniPanel").objectReferenceValue      = panelUI;
                cdSO.FindProperty("targetCollider").objectReferenceValue = finalCol;
                cdSO.ApplyModifiedProperties();
            }
            else
                Debug.LogError($"[PenSetup] {penId}: PenClickDetector type không tìm thấy — kiểm tra compile errors trong Console.");

            // ── PenDropTarget ─────────────────────────────────────────────────
            var dropTarget = penRoot.GetComponent<PenDropTarget>()
                          ?? penRoot.AddComponent<PenDropTarget>();
            var dtSO = new SerializedObject(dropTarget);
            dtSO.FindProperty("miniPanel").objectReferenceValue = panelUI;
            dtSO.ApplyModifiedProperties();

            // ── feedItemId trên DraggableFeedItem của panel này ───────────────
            // Slot_Food1/2 giờ nằm dưới PanelContent (con của panel root)
            Transform panelContentChild = panelUI.transform.Find("PanelContent");
            Transform slotSearchRoot    = panelContentChild != null ? panelContentChild : panelUI.transform;
            SetFeedItemId(slotSearchRoot, "Slot_Food1", food1Id, config.feedDurationSeconds);
            SetFeedItemId(slotSearchRoot, "Slot_Food2", food2Id, config.feedDurationSeconds);

            // ── Save ──────────────────────────────────────────────────────────
            PrefabUtility.SaveAsPrefabAsset(penRoot, penPath);
            Debug.Log($"[PenSetup] ✅ {penId} — saved: {penPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(penRoot);
        }
    }

    private static void SetFeedItemId(Transform panelRoot, string slotName,
        string itemId, float duration)
    {
        Transform slot = panelRoot.Find(slotName);
        if (slot == null)
        {
            Debug.LogWarning($"[PenSetup] Không tìm thấy '{slotName}' trong panel.");
            return;
        }

        var drag = slot.GetComponent<DraggableFeedItem>();
        if (drag == null)
        {
            Debug.LogWarning($"[PenSetup] DraggableFeedItem không có trên '{slotName}'.");
            return;
        }

        var so = new SerializedObject(drag);
        so.FindProperty("feedItemId").stringValue  = itemId;
        so.FindProperty("feedDuration").floatValue = duration;
        so.ApplyModifiedProperties();
        Debug.Log($"[PenSetup]   {slotName}.feedItemId = '{itemId}'");
    }

    // ─── Step 3: Auto-assign Icons ────────────────────────────────────────────

    private static void Step3_AssignIcons()
    {
        if (IsPrefabModeActive()) return;

        var iconMap = BuildIconMap();

        foreach (var (_, cfgPath, penId, _, _) in PenData)
        {
            var config = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(cfgPath);
            if (config == null)
            {
                Debug.LogWarning($"[PenSetup] Config không tìm thấy: {cfgPath}");
                continue;
            }

            var so = new SerializedObject(config);

            SetIconField(so, "food1Icon",         config.food1ItemId,         iconMap, penId);
            SetIconField(so, "food2Icon",         config.food2ItemId,         iconMap, penId);
            SetIconField(so, "productIcon",       config.productItemId,       iconMap, penId);
            SetIconField(so, "secondProductIcon", config.secondProductItemId, iconMap, penId);
            SetBasketIcon(so, iconMap, penId);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[PenSetup] ✅ Step 3 — Icons đã gán vào 4 Config.");
    }

    private static Dictionary<string, Sprite> BuildIconMap()
    {
        var map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        foreach (var (id, path, isCrop) in IconSources)
        {
            Sprite sprite;

            if (isCrop)
            {
                var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
                sprite = crop != null ? crop.itemIcon : null;
                if (crop == null)
                    Debug.LogWarning($"[PenSetup] CropData không tìm thấy: {path}");
            }
            else
            {
                var item = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
                sprite = item != null ? item.icon : null;
                if (item == null)
                    Debug.LogWarning($"[PenSetup] InventoryItemData không tìm thấy: {path}");
            }

            if (sprite != null)
            {
                map[id] = sprite;
                Debug.Log($"[PenSetup] Icon loaded: '{id}' → {sprite.name}");
            }
        }

        return map;
    }

    private static void SetIconField(SerializedObject so, string propName,
        string itemId, Dictionary<string, Sprite> map, string penId)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        if (map.TryGetValue(itemId, out Sprite sp))
        {
            so.FindProperty(propName).objectReferenceValue = sp;
            Debug.Log($"[PenSetup] {penId}: {propName} = {sp.name}");
        }
        else
            Debug.LogWarning($"[PenSetup] {penId}: icon cho '{itemId}' không có trong map.");
    }

    private static void SetBasketIcon(SerializedObject so,
        Dictionary<string, Sprite> map, string penId)
    {
        // Không có sprite rổ trong project → dùng beef icon tạm
        Sprite placeholder = null;
        string usedName    = "none";

        if (map.TryGetValue("beef", out var beefSprite))
        {
            placeholder = beefSprite;
            usedName    = beefSprite.name;
        }

        if (placeholder != null)
        {
            so.FindProperty("basketIcon").objectReferenceValue = placeholder;
            Debug.LogWarning(
                $"[PenSetup] {penId}: basketIcon ← sprite tạm '{usedName}' (beef icon). " +
                "Thay sprite rổ thật trong Config asset sau.");
        }
        else
            Debug.LogWarning($"[PenSetup] {penId}: basketIcon — không tìm được sprite tạm.");
    }

    // ─── Step 4: Register Item_Milk → WarehousePopupUI ───────────────────────

    private static void Step4_RegisterMilk()
    {
        if (IsPrefabModeActive()) return;
        if (!IsTargetSceneLoaded()) return;

#pragma warning disable CS0618
        var warehouseUI = FindObjectOfType<WarehousePopupUI>();
#pragma warning restore CS0618

        if (warehouseUI == null)
        {
            Debug.LogError("[PenSetup] ❌ WarehousePopupUI không tìm thấy trong scene. " +
                           "Kiểm tra GameObject có active không.");
            return;
        }

        var milkItem = AssetDatabase.LoadAssetAtPath<InventoryItemData>(
            "Assets/_Game/Farm/Data/Farm_dong_vat/Item_Milk.asset");
        if (milkItem == null)
        {
            Debug.LogError("[PenSetup] Item_Milk.asset không tìm thấy.");
            return;
        }

        var so       = new SerializedObject(warehouseUI);
        var listProp = so.FindProperty("extraItemDatabase");

        // Idempotency: kiểm tra đã có chưa
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == milkItem)
            {
                Debug.Log("[PenSetup] Item_Milk đã có trong extraItemDatabase — bỏ qua.");
                return;
            }
        }

        listProp.InsertArrayElementAtIndex(listProp.arraySize);
        listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = milkItem;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(warehouseUI);
        EditorSceneManager.MarkSceneDirty(warehouseUI.gameObject.scene);

        Debug.Log("[PenSetup] ✅ Step 4 — Item_Milk thêm vào WarehousePopupUI.extraItemDatabase.");
    }

    // ─── Run All ──────────────────────────────────────────────────────────────

    private static void RunAllSteps()
    {
        Debug.Log("[PenSetup] ══════════ Run All bắt đầu ══════════");
        Step1_CreatePanelPrefab();
        Step2_SetupPenPrefabs();
        Step3_AssignIcons();
        Step4_RegisterMilk();
        Debug.Log("[PenSetup] ══════════ Run All kết thúc ══════════");
    }

    // ─── Validate ─────────────────────────────────────────────────────────────

    private static void ValidateSetup()
    {
        Debug.Log("[PenSetup] ══════════ Validate bắt đầu ══════════");

        // 1. Prefab exists
        bool panelOK = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabOutputPath) != null;
        Log(panelOK, $"PF_PenMiniPanel.prefab tại {PrefabOutputPath}");

        // 2. Each pen prefab
        foreach (var (penPath, cfgPath, penId, _, _) in PenData)
        {
            var penGO = AssetDatabase.LoadAssetAtPath<GameObject>(penPath);
            if (penGO == null)
            {
                Debug.LogError($"[Validate] ❌ Pen prefab không tìm thấy: {penPath}");
                continue;
            }

            bool hasDetector  = penGO.GetComponent("PenClickDetector") != null;
            bool hasDropTarget = penGO.GetComponent<PenDropTarget>() != null;
            bool hasPanelChild = penGO.transform.Find("PF_PenMiniPanel") != null;

            Log(hasDetector,   $"{penId}: PenClickDetector trên root");
            Log(hasDropTarget, $"{penId}: PenDropTarget trên root");
            Log(hasPanelChild, $"{penId}: Child PF_PenMiniPanel");

            // Config binding check
            PenMiniPanelUI panelUI = penGO.GetComponentInChildren<PenMiniPanelUI>(true);
            if (panelUI != null)
            {
                var so = new SerializedObject(panelUI);
                var configRef = so.FindProperty("config").objectReferenceValue as PenMiniPanelConfig;
                bool configOK = configRef != null && configRef.penId == penId;
                Log(configOK, $"{penId}: config.penId == '{penId}'");
            }
            else
                Log(false, $"{penId}: PenMiniPanelUI trong child");

            // Config icons
            var cfg = AssetDatabase.LoadAssetAtPath<PenMiniPanelConfig>(cfgPath);
            if (cfg != null)
            {
                Log(cfg.food1Icon   != null, $"{penId}: config.food1Icon");
                Log(cfg.food2Icon   != null, $"{penId}: config.food2Icon");
                Log(cfg.productIcon != null, $"{penId}: config.productIcon");

                if (cfg.basketIcon != null)
                    Debug.LogWarning($"[Validate] ⚠️ {penId}: basketIcon = '{cfg.basketIcon.name}' " +
                                     "(placeholder) — thay sprite rổ thật.");
                else
                    Debug.LogWarning($"[Validate] ⚠️ {penId}: basketIcon = null");
            }
        }

        // 3. Item_Milk in WarehousePopupUI
#pragma warning disable CS0618
        var warehouseUI = FindObjectOfType<WarehousePopupUI>();
#pragma warning restore CS0618

        if (warehouseUI != null)
        {
            var so       = new SerializedObject(warehouseUI);
            var listProp = so.FindProperty("extraItemDatabase");
            bool milkOK  = false;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue is InventoryItemData d
                    && d.itemId == "milk")
                {
                    milkOK = true;
                    break;
                }
            }
            Log(milkOK, "Item_Milk trong WarehousePopupUI.extraItemDatabase");
        }
        else
            Debug.LogWarning("[Validate] WarehousePopupUI không tìm thấy — mở Scene_Farm để validate bước này.");

        Debug.Log("[PenSetup] ══════════ Validate xong ══════════");
    }

    private static void Log(bool ok, string label)
    {
        if (ok) Debug.Log($"  [✅] {label}");
        else    Debug.LogError($"  [❌] {label}  — THIẾU");
    }

    // ─── Prefab Build Helpers ─────────────────────────────────────────────────

    private static void ApplyCanvasSorting(Canvas canvas, string layerName, int order)
    {
        // BS1: World Space canvas sort qua sortingLayerName / sortingOrder, không phải "Sort Order" field
        bool exists = SortingLayer.layers.Any(l => l.name == layerName);
        if (exists)
        {
            canvas.sortingLayerName = layerName;
        }
        else
        {
            canvas.sortingLayerName = "Default";
            Debug.LogWarning($"[PenSetup] ⚠️ Sorting layer '{layerName}' không tồn tại — Canvas dùng 'Default'. " +
                             $"Tạo layer '{layerName}' trong Project Settings → Tags & Layers rồi chạy lại Step 1.");
        }
        canvas.sortingOrder = order;
    }

    private static (GameObject slot, Image icon, TextMeshProUGUI amt)
        MakeFoodSlot(string name, Transform parent, float xPos)
    {
        var slot    = new GameObject(name);
        slot.transform.SetParent(parent, false);
        var bgImg   = slot.AddComponent<Image>();
        bgImg.color = new Color(0.28f, 0.24f, 0.18f, 0.88f);
        var rt      = slot.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, 0f);
        rt.sizeDelta        = new Vector2(110f, 110f);

        // Icon (top 70% of slot)
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(slot.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.raycastTarget = false;
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.28f);
        iconRect.anchorMax = new Vector2(0.9f, 0.95f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

        // Amount text (bottom 25%)
        var amtGO   = new GameObject("AmountText");
        amtGO.transform.SetParent(slot.transform, false);
        var amtTxt  = amtGO.AddComponent<TextMeshProUGUI>();
        amtTxt.text      = "x0";
        amtTxt.fontSize  = 22f;
        amtTxt.alignment = TextAlignmentOptions.BottomRight;
        amtTxt.color     = Color.white;
        var amtRect = amtGO.GetComponent<RectTransform>();
        amtRect.anchorMin = new Vector2(0f, 0f);
        amtRect.anchorMax = new Vector2(1f, 0.30f);
        amtRect.offsetMin = new Vector2(4f,  3f);
        amtRect.offsetMax = new Vector2(-4f, 0f);

        return (slot, iconImg, amtTxt);
    }

    private static (GameObject slot, Image icon, GameObject glow)
        MakeBasketSlot(Transform parent, float xPos)
    {
        var slot    = new GameObject("Slot_Basket");
        slot.transform.SetParent(parent, false);
        var bgImg   = slot.AddComponent<Image>();
        bgImg.color = new Color(0.28f, 0.24f, 0.18f, 0.88f);
        var rt      = slot.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xPos, 0f);
        rt.sizeDelta        = new Vector2(110f, 110f);

        // Icon
        var iconGO  = new GameObject("Icon");
        iconGO.transform.SetParent(slot.transform, false);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.raycastTarget = false;
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

        // Glow (vàng, ẩn mặc định, bật khi Ready)
        var glowGO  = new GameObject("BasketGlow");
        glowGO.transform.SetParent(slot.transform, false);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.color         = new Color(1f, 0.9f, 0.2f, 0.55f);
        glowImg.raycastTarget = false;
        var glowRect = glowGO.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = glowRect.offsetMax = Vector2.zero;
        glowGO.SetActive(false);

        return (slot, iconImg, glowGO);
    }

    private static GameObject MakeImage(string name, Transform parent, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Gán giá trị field (public hoặc [SerializeField]) qua SerializedObject.</summary>
    private static void SetSerializedRef(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
            Debug.LogWarning($"[PenSetup] SerializedProperty '{fieldName}' không tìm thấy trên {target.GetType().Name}.");
    }

    // ─── Runtime Type Resolver ────────────────────────────────────────────────

    /// <summary>
    /// Tìm type trong tất cả assembly đang load — tránh compile-time coupling
    /// với runtime scripts (giải quyết CS0246 khi script chưa hoàn chỉnh compile).
    /// </summary>
    private static Type FindRuntimeType(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(typeName);
            if (t != null) return t;
        }
        Debug.LogError($"[PenSetup] ❌ Type '{typeName}' không tìm thấy trong assembly. " +
                       "Kiểm tra compile errors trong Console (có thể cần Assets → Refresh).");
        return null;
    }

    // ─── Folder Helper ────────────────────────────────────────────────────────

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
        string leaf   = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
        AssetDatabase.Refresh();
        Debug.Log($"[PenSetup] Tạo folder: {folderPath}");
    }
}
