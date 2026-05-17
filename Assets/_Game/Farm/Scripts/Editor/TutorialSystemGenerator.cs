#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// FarmTools ▸ Generate Tutorial System (1-Click)
///
/// Tự động hóa 100%:
///   • Tạo Tutorial_Canvas → Cloud_Panel → NPC_Dialog_Popup → Hand_Pointer
///   • Cloud_Left / Cloud_Right dùng Anchor chia đôi màn hình
///   • Thêm Button vào NPC_Dialog_Popup + auto-wire NextStep() qua UnityEventTools
///   • Tạo 5 TutorialStepData (kịch bản Level 1 Hay Day) trong Resources/TutorialSteps
///   • Gán tất cả references vào TutorialManager — bấm Play là chạy ngay
/// </summary>
public static class TutorialSystemGenerator
{
    private const string MENU_PATH   = "FarmTools/Generate Tutorial System";
    private const int    CANVAS_SORT = 999;
    private const float  DIM_ALPHA   = 150f / 255f;
    private const string SHADER_PATH = "FarmGame/DimWithHole";
    private const string STEPS_RES   = "Assets/Resources/TutorialSteps";

    // =========================================================================
    // Entry Point
    // =========================================================================
    [MenuItem(MENU_PATH)]
    private static void Generate()
    {
        if (!EditorUtility.DisplayDialog(
            "Generate Tutorial System",
            "Tạo Tutorial_System trong Scene hiện hành?\n\n" +
            "• Tạo 5 TutorialStepData (kịch bản Level 1) trong Assets/Resources/TutorialSteps/\n" +
            "• Gán tự động vào TutorialManager\n" +
            "• Auto-wire Button → NextStep() trên NPC_Dialog_Popup\n" +
            "(Ctrl+Z để hoàn tác hierarchy, data file giữ nguyên)",
            "Tạo ngay", "Huỷ")) return;

        // --- BƯỚC 1: Step Data ---
        EnsureFolder("Assets/Resources", STEPS_RES);
        var steps = GenerateStepAssets();

        // --- BƯỚC 2: Hierarchy ---
        var root = CreateEmpty("Tutorial_System");
        Undo.RegisterCreatedObjectUndo(root, "Create Tutorial System");

        // Tutorial_Manager (non-UI host)
        var managerGo = CreateEmpty("Tutorial_Manager", root.transform);
        var manager   = managerGo.AddComponent<TutorialManager>();

        // Tutorial_Canvas
        var canvasGo = BuildCanvas(root.transform);

        // Draw order (sibling index = rendering order):
        //   0. Dim_Background
        //   1. NPC_Dialog_Popup
        //   2. Hand_Pointer
        //   3. Cloud_Panel  ← trên cùng, che hết khi Intro
        var (unmask, _)                    = BuildDimBackground(canvasGo.transform);
        var (popup, tmpText, portrait)     = BuildNPCDialog(canvasGo.transform);
        var (handRT, handAnim)             = BuildHandPointer(canvasGo.transform);
        var (cloudPanel, cloudL, cloudR)   = BuildCloudPanel(canvasGo.transform);

        // Tutorial_Camera (ngoài canvas)
        var camZoom = BuildTutorialCamera(root.transform);

        // --- BƯỚC 3: Auto-Wire ---
        WireReferences(manager, steps, unmask, popup, tmpText, portrait,
                       handRT, handAnim, cloudPanel, cloudL, cloudR, camZoom);

        // AUTO-WIRE BUTTON → NextStep()  ← điểm mới quan trọng
        WireNPCButton(popup, manager);

        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("Hoàn tất!",
            "Tutorial_System đã sẵn sàng.\n\n" +
            "Còn lại cần làm thủ công:\n" +
            "① Gắn TutorialTarget lên các UI Button/đất (đặt targetID khớp với step).\n" +
            "② Gán Sprite cloud vào Cloud_Left/Cloud_Right > Image.\n" +
            "③ Gán AnimatorController cho Hand_Pointer.\n" +
            "④ Gán Camera (hoặc để Camera.main) vào Tutorial_Camera.", "OK");

        Debug.Log("[TutorialSystemGenerator] Done. 5 steps tại: " + STEPS_RES);
    }

    // =========================================================================
    // UI Repair Tools
    // =========================================================================

    /// <summary>
    /// Tìm EventSystem trong scene, xoá StandaloneInputModule cũ, thêm InputSystemUIInputModule mới.
    /// Khắc phục lỗi double-click khi dùng New Input System.
    /// </summary>
    [MenuItem("FarmTools/① Fix EventSystem (New Input System)")]
    private static void FixEventSystem()
    {
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", "Không có EventSystem nào trong scene hiện tại.", "OK");
            return;
        }

        bool changed = false;

        // Xoá StandaloneInputModule cũ (nguyên nhân double-click với New Input System)
        var standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Undo.DestroyObjectImmediate(standalone);
            Debug.Log("[FixEventSystem] Đã xoá StandaloneInputModule.");
            changed = true;
        }

        // Thêm InputSystemUIInputModule nếu chưa có
        var newModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (newModule == null)
        {
            Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
            Debug.Log("[FixEventSystem] Đã thêm InputSystemUIInputModule.");
            changed = true;
        }

        EditorUtility.SetDirty(eventSystem.gameObject);

        string msg = changed
            ? "✓ EventSystem đã được nâng cấp:\n• Đã xoá StandaloneInputModule\n• Đã thêm InputSystemUIInputModule\n\nBây giờ click 1 phát là ăn ngay!"
            : "EventSystem đã có InputSystemUIInputModule — không cần sửa.";
        EditorUtility.DisplayDialog("Fix EventSystem", msg, "OK");
    }

    [MenuItem("FarmTools/① Fix EventSystem (New Input System)", true)]
    private static bool ValidateFixEventSystem() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid();

    /// <summary>
    /// Quét toàn bộ TextMeshProUGUI trong scene, tắt Raycast Target để chữ không nuốt click của Button.
    /// </summary>
    [MenuItem("FarmTools/② Fix TMP Raycast Targets (Disable on all Text)")]
    private static void FixTMPRaycastTargets()
    {
        var allTMPs = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;
        foreach (var tmp in allTMPs)
        {
            if (tmp.raycastTarget)
            {
                Undo.RecordObject(tmp, "Disable TMP RaycastTarget");
                tmp.raycastTarget = false;
                EditorUtility.SetDirty(tmp);
                count++;
            }
        }

        EditorUtility.DisplayDialog("Fix TMP Raycast Targets",
            $"✓ Đã tắt Raycast Target trên {count} TextMeshProUGUI.\n(Chữ không còn chặn click button phía sau.)",
            "OK");
        Debug.Log($"[FixTMPRaycastTargets] Đã xử lý {count} TMP text.");
    }

    [MenuItem("FarmTools/② Fix TMP Raycast Targets (Disable on all Text)", true)]
    private static bool ValidateFixTMPRaycast() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid();

    // =========================================================================
    // Step Data — Kịch bản Level 1 (Hay Day style)
    // =========================================================================
    private static List<TutorialStepData> GenerateStepAssets()
    {
        // (fileName, npcText, targetID, waitAction, useCircle, showHand)
        var defs = new[]
        {
            (
                "1_Welcome",
                "Cháu đến rồi à! Trang trại hoang tàn quá, bắt tay vào việc bằng cách vào Cửa Hàng mua một mảnh đất nhé!",
                "",
                TutorialWaitAction.WaitForClick,
                false,
                false
            ),
            (
                "2_OpenShop",
                "Bấm vào đây để mở Cửa Hàng. Sau đó chọn tab Đất để mua mảnh đất đầu tiên nào!",
                "btn_shop",
                TutorialWaitAction.WaitForClick,
                true,
                true
            ),
            (
                "3_BuyDirt",
                "Chọn mua Ô Đất ở đây — lần đầu hoàn toàn miễn phí! Nhấn Mua rồi đặt xuống map nhé.",
                "btn_buy_dirt",
                TutorialWaitAction.WaitForClick,
                false,
                true
            ),
            (
                "4_PlantWheat",
                "Tuyệt vời! Giờ hãy gieo hạt Lúa vào ô đất vừa đặt. Cây sẽ chín chỉ sau 5 giây thôi!",
                "plot_0",
                TutorialWaitAction.WaitForPlant,
                false,
                true
            ),
            (
                "5_HarvestWheat",
                "Lúa chín rồi! Dùng liềm chạm vào để thu hoạch. Thu xong nhận 10 Exp và lên ngay Level 2!",
                "plot_0",
                TutorialWaitAction.WaitForHarvest,
                true,
                true
            ),
        };

        // Đảm bảo thư mục tồn tại trên disk trước khi CreateAsset
        string folderPath = Application.dataPath + "/Resources/TutorialSteps";
        if (!System.IO.Directory.Exists(folderPath))
        {
            System.IO.Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }

        var list = new List<TutorialStepData>();
        foreach (var (file, text, id, action, circle, hand) in defs)
        {
            string path  = $"{STEPS_RES}/{file}.asset";
            var    asset = AssetDatabase.LoadAssetAtPath<TutorialStepData>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TutorialStepData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("npcText").stringValue          = text;
            so.FindProperty("targetID").stringValue         = id;
            so.FindProperty("waitAction").enumValueIndex    = (int)action;
            so.FindProperty("useCircleHole").boolValue      = circle;
            so.FindProperty("showHandPointer").boolValue    = hand;
            so.ApplyModifiedPropertiesWithoutUndo();

            list.Add(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return list;
    }

    // =========================================================================
    // Hierarchy Builders
    // =========================================================================

    private static GameObject BuildCanvas(Transform parent)
    {
        var go     = CreateUI("Tutorial_Canvas", parent);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CANVAS_SORT;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        // CanvasGroup: TutorialManager dùng để tắt hoàn toàn block raycast khi tutorial kết thúc
        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.interactable   = true;
        cg.blocksRaycasts = true;

        StretchFull(go.GetComponent<RectTransform>());
        return go;
    }

    private static (UnmaskRaycastFilter unmask, GameObject dimGo) BuildDimBackground(Transform parent)
    {
        var dimGo  = CreateUI("Dim_Background", parent);
        var dimImg = dimGo.AddComponent<Image>();
        StretchFull(dimGo.GetComponent<RectTransform>());

        var shader = Shader.Find(SHADER_PATH);
        if (shader != null)
        {
            var mat = new Material(shader) { color = new Color(0f, 0f, 0f, DIM_ALPHA) };
            dimImg.material = mat;
        }
        else
        {
            dimImg.color = new Color(0f, 0f, 0f, DIM_ALPHA);
            Debug.LogWarning($"[Generator] Shader \"{SHADER_PATH}\" chưa import — dùng màu tối thay thế.");
        }

        var unmask = dimGo.AddComponent<UnmaskRaycastFilter>();
        return (unmask, dimGo);
    }

    private static (GameObject popup, TextMeshProUGUI text, Image portrait) BuildNPCDialog(Transform parent)
    {
        // Panel popup phía dưới màn hình
        var popupGo = CreateUI("NPC_Dialog_Popup", parent);
        var popupRT = popupGo.GetComponent<RectTransform>();
        popupRT.anchorMin        = new Vector2(0f,   0f);
        popupRT.anchorMax        = new Vector2(1f,   0f);
        popupRT.pivot            = new Vector2(0.5f, 0f);
        popupRT.anchoredPosition = new Vector2(0f,  20f);
        popupRT.sizeDelta        = new Vector2(0f, 280f);

        // Background
        var bgGo  = CreateUI("NPC_Background", popupGo.transform);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.05f, 0.02f, 0.93f);
        StretchFull(bgGo.GetComponent<RectTransform>());

        // Portrait
        var portraitGo = CreateUI("NPC_Portrait", popupGo.transform);
        var portraitRT = portraitGo.GetComponent<RectTransform>();
        portraitRT.anchorMin        = new Vector2(0f, 1f);
        portraitRT.anchorMax        = new Vector2(0f, 1f);
        portraitRT.pivot            = new Vector2(0f, 0f);
        portraitRT.anchoredPosition = new Vector2(12f, 0f);
        portraitRT.sizeDelta        = new Vector2(190f, 190f);
        var portrait = portraitGo.AddComponent<Image>();
        portrait.preserveAspect = true;

        // NPC Text
        var textGo = CreateUI("NPC_Text", popupGo.transform);
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.offsetMin = new Vector2(210f,  20f);
        textRT.offsetMax = new Vector2(-20f, -20f);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text          = "Cháu đến rồi à! Bắt tay vào việc thôi!";
        tmp.fontSize      = 36f;
        tmp.color         = Color.white;
        tmp.alignment     = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false; // chữ không được nuốt click của Button phía sau

        return (popupGo, tmp, portrait);
    }

    private static (RectTransform handRT, Animator anim) BuildHandPointer(Transform parent)
    {
        var handGo = CreateUI("Hand_Pointer", parent);
        var handRT = handGo.GetComponent<RectTransform>();
        handRT.sizeDelta = new Vector2(110f, 110f);

        var imgGo = CreateUI("Hand_Image", handGo.transform);
        imgGo.AddComponent<Image>().preserveAspect = true;
        StretchFull(imgGo.GetComponent<RectTransform>());

        var anim = handGo.AddComponent<Animator>();
        return (handRT, anim);
    }

    /// <summary>
    /// Cloud_Panel che toàn màn hình trong Intro.
    /// Cloud_Left  = nửa trái  → anchor (0,   0) – (0.5, 1).
    /// Cloud_Right = nửa phải  → anchor (0.5, 0) – (1,   1).
    /// Hai nửa tự dính nhau và chia đôi màn hình ở mọi độ phân giải.
    /// </summary>
    private static (GameObject panel, RectTransform left, RectTransform right)
        BuildCloudPanel(Transform parent)
    {
        var panelGo = CreateUI("Cloud_Panel", parent);
        StretchFull(panelGo.GetComponent<RectTransform>());

        // CanvasGroup: TutorialManager set blocksRaycasts=false khi ẩn mây
        // tránh Panel vô hình nuốt click của game UI bên dưới
        var cg = panelGo.AddComponent<CanvasGroup>();
        cg.alpha          = 1f;
        cg.interactable   = true;
        cg.blocksRaycasts = true;

        // Cloud_Left — nửa trái, anchor chia đôi
        var leftGo = CreateUI("Cloud_Left", panelGo.transform);
        var leftRT = leftGo.GetComponent<RectTransform>();
        leftRT.anchorMin        = new Vector2(0f,   0f);
        leftRT.anchorMax        = new Vector2(0.5f, 1f);
        leftRT.offsetMin        = Vector2.zero;
        leftRT.offsetMax        = Vector2.zero;
        leftRT.anchoredPosition = Vector2.zero;
        leftGo.AddComponent<Image>().color = new Color(0.85f, 0.92f, 1f, 1f);

        // Cloud_Right — nửa phải, anchor chia đôi
        var rightGo = CreateUI("Cloud_Right", panelGo.transform);
        var rightRT = rightGo.GetComponent<RectTransform>();
        rightRT.anchorMin        = new Vector2(0.5f, 0f);
        rightRT.anchorMax        = new Vector2(1f,   1f);
        rightRT.offsetMin        = Vector2.zero;
        rightRT.offsetMax        = Vector2.zero;
        rightRT.anchoredPosition = Vector2.zero;
        rightGo.AddComponent<Image>().color = new Color(0.85f, 0.92f, 1f, 1f);

        return (panelGo, leftRT, rightRT);
    }

    private static TutorialCameraZoom BuildTutorialCamera(Transform parent)
    {
        var camGo = new GameObject("Tutorial_Camera");
        Undo.RegisterCreatedObjectUndo(camGo, "Create Tutorial_Camera");
        camGo.transform.SetParent(parent, false);
        return camGo.AddComponent<TutorialCameraZoom>();
    }

    // =========================================================================
    // Auto-Wire Button → NextStep()  (UnityEventTools — không cần kéo tay)
    // =========================================================================
    private static void WireNPCButton(GameObject popup, TutorialManager manager)
    {
        // Thêm Button vào NPC_Dialog_Popup nếu chưa có
        var btn = popup.GetComponent<Button>();
        if (btn == null) btn = popup.AddComponent<Button>();

        // Đặt target graphic thành Image nền cho visual feedback
        var bg = popup.transform.Find("NPC_Background")?.GetComponent<Image>();
        if (bg != null) btn.targetGraphic = bg;

        // Xoá listener cũ để tránh duplicate khi chạy lại Generate
        btn.onClick.RemoveAllListeners();

        // AddPersistentListener: gán hàm vĩnh viễn (lưu vào scene), không phải runtime-only
        UnityEventTools.AddPersistentListener(btn.onClick, manager.NextStep);

        EditorUtility.SetDirty(popup);
        Debug.Log("[TutorialSystemGenerator] Auto-wired NPC_Dialog_Popup.Button → TutorialManager.NextStep()");
    }

    // =========================================================================
    // Wire All SerializedObject References
    // =========================================================================
    private static void WireReferences(
        TutorialManager        manager,
        List<TutorialStepData> steps,
        UnmaskRaycastFilter    unmask,
        GameObject             popup,
        TextMeshProUGUI        tmpText,
        Image                  portrait,
        RectTransform          handRT,
        Animator               handAnim,
        GameObject             cloudPanel,
        RectTransform          cloudL,
        RectTransform          cloudR,
        TutorialCameraZoom     camZoom)
    {
        var so = new SerializedObject(manager);

        // Steps array
        var stepsProp = so.FindProperty("_steps");
        stepsProp.arraySize = steps.Count;
        for (int i = 0; i < steps.Count; i++)
            stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];

        // Core UI
        so.FindProperty("_dimBackground").objectReferenceValue  = unmask;
        so.FindProperty("_npcDialogPopup").objectReferenceValue = popup;
        so.FindProperty("_npcDialogText").objectReferenceValue  = tmpText;
        so.FindProperty("_npcPortrait").objectReferenceValue    = portrait;
        so.FindProperty("_handPointer").objectReferenceValue    = handRT;
        so.FindProperty("_handAnimator").objectReferenceValue   = handAnim;

        // Intro
        so.FindProperty("_cloudPanel").objectReferenceValue  = cloudPanel;
        so.FindProperty("_cloudLeft").objectReferenceValue   = cloudL;
        so.FindProperty("_cloudRight").objectReferenceValue  = cloudR;
        so.FindProperty("_cameraZoom").objectReferenceValue  = camZoom;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static GameObject CreateUI(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject CreateEmpty(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void EnsureFolder(string parentPath, string fullPath)
    {
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            string folderName = fullPath.Substring(parentPath.Length + 1);
            AssetDatabase.CreateFolder(parentPath, folderName);
            AssetDatabase.Refresh();
        }
    }

    [MenuItem(MENU_PATH, true)]
    private static bool ValidateGenerate() =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid();
}
#endif
