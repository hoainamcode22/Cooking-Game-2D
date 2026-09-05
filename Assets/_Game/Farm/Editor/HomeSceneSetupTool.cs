using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool dựng màn hình Home & Loading hoàn toàn mới theo ảnh thiết kế:
/// - Background: Source_Home/ChatGPT Image... toàn màn hình
/// - Khung Tip & Progress Bar ở cạnh dưới màn hình
/// - Nhân vật hoạt hình nhún nhảy bên trái
/// - Món ăn thơm ngon decor bên phải
/// - Tự động tải ngầm và chuyển vào SCN_Farm mượt mà.
/// </summary>
// ⛔ [VÒNG 13 — 04/09/2026] ĐÃ TẮT TỰ CHẠY THEO LỆNH LEAD.
// Trước đây attribute [InitializeOnLoad] khiến static constructor chạy MỖI LẦN Unity biên dịch
// lại, kéo theo EditorApplication.delayCall → tool tự sửa scene rồi TỰ LƯU. Hậu quả: mọi thứ
// Sếp kéo tay trong scene (vị trí prefab tàu, nút HUD, reference nhân vật popup) đều bị ghi đè
// âm thầm sau mỗi lần compile — đây chính là nguyên nhân của chuỗi lỗi "tự nhiên hỏng".
// Menu trong Tools/... VẪN CÒN — muốn chạy thì bấm tay, chủ động và kiểm soát được.
// Muốn bật lại: bỏ dấu // ở dòng dưới.
// [InitializeOnLoad]
public static class HomeSceneSetupTool
{
    private const string MenuPath = "Tools/Farm Game/Dựng Màn Hình Home & Loading (SCN_Home)";

    static HomeSceneSetupTool()
    {
        // ⛔ [VÒNG 14] ĐÃ TẮT — dòng dưới từng khiến tool tự chạy + tự lưu scene mỗi lần compile.
        // Comment [InitializeOnLoad] ở vòng 13 là CHƯA ĐỦ: chỉ cần code khác chạm vào bất kỳ
        // member nào của class là static constructor vẫn chạy, và dòng này vẫn đăng ký.
        // Muốn chạy: bấm menu trong Tools/... (chủ động, kiểm soát được).
        // EditorApplication.delayCall += AutoCheckHomeScene;
    }

    private static void AutoCheckHomeScene()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name == "SCN_Home")
        {
            SetupHomeScene(false);
        }
    }

    [MenuItem(MenuPath, false, 20)]
    public static void SetupHomeSceneMenu()
    {
        SetupHomeScene(true);
    }

    public static void SetupHomeScene(bool showDialog = true)
    {
        string scenePath = "Assets/_Game/Scenes/SCN_Home.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Dựng Màn Hình Home Mới");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas_Home", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Undo.RegisterCreatedObjectUndo(canvasGo, "Dựng Màn Hình Home");
        }

        // Xóa sạch toàn bộ UI cũ để dựng lại mới 100%
        while (canvas.transform.childCount > 0)
        {
            Undo.DestroyObjectImmediate(canvas.transform.GetChild(0).gameObject);
        }

        // 1. Background Nền Thung Lũng Nông Trại Mới (Stretch Full Screen)
        RectTransform bgRoot = CreateRect(canvas.transform, "Bg_Root", new Vector2(1920f, 1080f), Vector2.zero);
        StretchFull(bgRoot);

        Sprite bgSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Source_Home/ChatGPT Image 16_08_16 3 thg 9, 2026.png");
        if (bgSpr == null)
        {
            // Thử load tất cả asset tại đường dẫn (nếu import dạng Multiple)
            var allAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Source_Home/ChatGPT Image 16_08_16 3 thg 9, 2026.png");
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Sprite s) { bgSpr = s; break; }
            }
        }
        Image bgImg = AddImage(bgRoot.gameObject, Color.white, bgSpr, false);
        bgImg.preserveAspect = false;

        // 2. KHUNG TIP & LOADING CHÍNH Ở ĐÁY MÀN HÌNH (Rộng: 1060, Cao: 175, Y: 110 từ đáy)
        RectTransform tipCardRoot = CreateRect(canvas.transform, "Card_LoadingTip", new Vector2(1060f, 175f), new Vector2(0f, 110f));
        tipCardRoot.anchorMin = new Vector2(0.5f, 0f);
        tipCardRoot.anchorMax = new Vector2(0.5f, 0f);
        tipCardRoot.pivot = new Vector2(0.5f, 0.5f);

        // Viền ngoài màu nâu vàng kem
        AddImage(tipCardRoot.gameObject, new Color32(230, 200, 155, 255), BoGoc(44f), true);
        RectTransform tipCardFill = CreateRect(tipCardRoot, "Card_Fill", new Vector2(1050f, 165f), Vector2.zero);
        AddImage(tipCardFill.gameObject, new Color32(255, 247, 230, 255), BoGoc(40f), true);

        // 3. NHÂN VẬT HOẠT HÌNH BÊN TRÁI (Kích thước: 165 x 165, Nhô nhẹ lên trên)
        RectTransform charRt = CreateRect(tipCardRoot, "Img_Character", new Vector2(165f, 165f), new Vector2(-455f, 25f));
        Image charImg = AddImage(charRt.gameObject, Color.white, null, false);
        charImg.preserveAspect = true;

        // Tải sequence 12 frame animation của nhân vật
        List<Sprite> charFrames = new List<Sprite>();
        for (int i = 1; i <= 12; i++)
        {
            string p = string.Format("Assets/Art/UI/LevelUpV2/characters/char_01/char_01_f{0:00}.png", i);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) charFrames.Add(s);
        }
        if (charFrames.Count == 0)
        {
            Sprite master = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/LevelUpV2/characters/char_01/char_01_master.png")
                         ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Anh/nhanvatcuocdat-removebg-preview.png");
            if (master != null) charFrames.Add(master);
        }
        if (charFrames.Count > 0) charImg.sprite = charFrames[0];

        // 4. MÓN ĂN DECOR BÊN PHẢI (Kích thước: 155 x 155, Nhô nhẹ ra mép phải)
        RectTransform foodRt = CreateRect(tipCardRoot, "Img_FoodDecor", new Vector2(155f, 155f), new Vector2(445f, 15f));
        Image foodImg = AddImage(foodRt.gameObject, Color.white, null, false);
        foodImg.preserveAspect = true;

        Sprite dishSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/Món ăn/Cơm chiên trứng.png")
                      ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Anh/todaydish.png");
        if (dishSpr != null) foodImg.sprite = dishSpr;

        // 5. CỤM NỘI DUNG TIP & THANH TIẾN ĐỘ Ở GIỮA
        RectTransform centerGroup = CreateRect(tipCardFill, "Center_Content", new Vector2(660f, 140f), new Vector2(5f, 0f));

        // Tiêu đề: 🌱 Tip:
        TMP_Text txtTipTitle = CreateText(centerGroup, "Txt_TipTitle", "🌱 Tip:", 22f, new Color32(47, 125, 24, 255), TextAlignmentOptions.Center, new Vector2(0f, 44f), new Vector2(600f, 30f), FontStyles.Bold);

        // Nội dung Tip: Grow crops, cook delicious dishes, and build your dream farm!
        GameObject tipBoxGo = new GameObject("Box_TipText", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform tipBoxRt = (RectTransform)tipBoxGo.transform;
        tipBoxRt.SetParent(centerGroup, false);
        tipBoxRt.anchoredPosition = new Vector2(0f, 12f);
        tipBoxRt.sizeDelta = new Vector2(620f, 40f);
        CanvasGroup tipCg = tipBoxGo.GetComponent<CanvasGroup>();

        TMP_Text txtFunTip = CreateText(tipBoxRt, "Txt_FunTip", "Grow crops, cook delicious dishes, and build your dream farm!", 18f, new Color32(98, 63, 30, 255), TextAlignmentOptions.Center, Vector2.zero, new Vector2(620f, 40f), FontStyles.Bold);

        // 6. THANH FILL BAR & TEXT PHẦN TRĂM (%)
        RectTransform barArea = CreateRect(centerGroup, "Bar_Area", new Vector2(620f, 32f), new Vector2(0f, -36f));

        // Rãnh trượt nền kem/nâu nhạt (Rộng 480)
        RectTransform barTrack = CreateRect(barArea, "Bar_Track", new Vector2(480f, 22f), new Vector2(-40f, 0f));
        AddImage(barTrack.gameObject, new Color32(234, 209, 168, 255), BoGoc(11f), true);
        RectTransform barInner = CreateRect(barTrack, "Inner", new Vector2(474f, 16f), Vector2.zero);
        AddImage(barInner.gameObject, new Color32(215, 185, 138, 255), BoGoc(8f), true);

        // Lớp Ruột Fill Màu Xanh Lá Tươi Sáng (#76D82C)
        RectTransform fillRt = CreateRect(barInner, "Img_ProgressFill", new Vector2(474f, 16f), Vector2.zero);
        StretchFull(fillRt);
        Image fillImg = AddImage(fillRt.gameObject, new Color32(118, 216, 44, 255), BoGoc(8f), true);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.35f;

        // Text % bên phải thanh trượt (ví dụ 72%)
        TMP_Text txtPercent = CreateText(barArea, "Txt_Percent", "72%", 20f, new Color32(98, 63, 30, 255), TextAlignmentOptions.Left, new Vector2(245f, 0f), new Vector2(80f, 28f), FontStyles.Bold);

        // 7. Gắn & Wire Component HomeScreenManager
        HomeScreenManager manager = canvas.GetComponent<HomeScreenManager>();
        if (manager == null) manager = canvas.gameObject.AddComponent<HomeScreenManager>();

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("imgProgressFill").objectReferenceValue = fillImg;
        so.FindProperty("txtProgressPercent").objectReferenceValue = txtPercent;
        so.FindProperty("characterRect").objectReferenceValue = charRt;
        so.FindProperty("characterImage").objectReferenceValue = charImg;
        
        var framesProp = so.FindProperty("characterFrames");
        if (framesProp != null)
        {
            framesProp.arraySize = charFrames.Count;
            for (int i = 0; i < charFrames.Count; i++)
            {
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = charFrames[i];
            }
        }

        so.FindProperty("foodDecorRect").objectReferenceValue = foodRt;
        so.FindProperty("txtTipTitle").objectReferenceValue = txtTipTitle;
        so.FindProperty("txtFunTip").objectReferenceValue = txtFunTip;
        so.FindProperty("tipCanvasGroup").objectReferenceValue = tipCg;
        so.FindProperty("targetSceneName").stringValue = "SCN_Farm";
        so.FindProperty("minLoadingSeconds").floatValue = 2.8f;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = canvas.gameObject;
        EditorGUIUtility.PingObject(canvas.gameObject);
        Debug.Log("[HomeSceneSetupTool] Đã dựng thành công Màn hình Home & Loading mới theo concept!");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công", "Đã dựng hoàn tất Màn Hình Home & Loading mới:\n1. Background thung lũng nông trại nghệ thuật toàn màn hình\n2. Xóa sạch UI cũ\n3. Khung Tip & Loading giấy kem bo góc mềm mại ở đáy\n4. Nhân vật hoạt hình nhún nhảy bên trái & Dĩa món ăn thơm ngon bên phải\n5. Thanh Fill Bar xanh tươi kèm % tiến độ\n6. Tự động nạp và chuyển cảnh thẳng vào SCN_Farm!", "Tuyệt vời!");
        }
    }

    private static Sprite BoGoc(float r) => SkinKit.BoGoc(r);

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite, bool isSliced)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = isSliced ? Image.Type.Sliced : Image.Type.Simple;
        }
        return img;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content, float size, Color color, TextAlignmentOptions align, Vector2 pos, Vector2 boxSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = boxSize;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;

        if (SkinKit.FontVo != null)
        {
            tmp.font = SkinKit.FontVo;
        }

        return tmp;
    }
}

