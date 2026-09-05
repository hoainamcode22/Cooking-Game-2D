using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialFourPopupSetupTool
{
    private const string MenuPath = "Tools/Farm Game/Rebuild Tutorial 4 Popups";
    private const string FarmScenePath = "Assets/_Game/Scenes/SCN_Farm.unity";
    private const string HandSpritePath = "Assets/_Game/Farm/Art/UI/tutorial_hand.png";
    private const string RiceDataPath = "Assets/_Game/Farm/data/Hat_giong/Crop_Rice.asset";

    // Asset paths for Package E - Guide Board
    private const string FrameSpritePath = "Assets/Art/UI/TutorialV2/guide_board/tut_board_frame.png";
    private const string RibbonSpritePath = "Assets/Art/UI/TutorialV2/guide_board/tut_board_ribbon.png";
    private const string SlotSpritePath = "Assets/Art/UI/TutorialV2/guide_board/tut_slot_illustration.png";
    private const string DotOnSpritePath = "Assets/Art/UI/TutorialV2/guide_board/tut_step_dot_on.png";
    private const string DotOffSpritePath = "Assets/Art/UI/TutorialV2/guide_board/tut_step_dot_off.png";

    /// <summary>[WP-C2] Khung bảng dùng chung cho 4 trang — gán trong RebuildLayout, đọc trong BuildPageRoot.</summary>
    private static Sprite s_frameSprite;

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

        var board = Object.FindFirstObjectByType<TutorialGuideBoardUI>(FindObjectsInactive.Include);
        if (board == null)
        {
            Debug.LogError("[Tutorial4Popup] TutorialGuideBoardUI not found.");
            return;
        }

        ConfigureSpriteImportSettings();

        Sprite hand = AssetDatabase.LoadAssetAtPath<Sprite>(HandSpritePath);
        CropData rice = AssetDatabase.LoadAssetAtPath<CropData>(RiceDataPath);
        // [WP-C2] Ưu tiên sprite chuẩn từ registry UIStandardSprites; không có mới quay về đường dẫn cũ của tool
        Sprite frameSprite = Pick(UIStandardSprites.TutFrame, AssetDatabase.LoadAssetAtPath<Sprite>(FrameSpritePath));
        Sprite ribbonSprite = Pick(UIStandardSprites.TutRibbon, AssetDatabase.LoadAssetAtPath<Sprite>(RibbonSpritePath));
        Sprite slotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SlotSpritePath);
        Sprite dotOnSprite = Pick(UIStandardSprites.TutDotOn, AssetDatabase.LoadAssetAtPath<Sprite>(DotOnSpritePath));
        Sprite dotOffSprite = Pick(UIStandardSprites.TutDotOff, AssetDatabase.LoadAssetAtPath<Sprite>(DotOffSpritePath));
        s_frameSprite = frameSprite; // khung bảng dùng chung cho 4 trang (BuildPageRoot)

        Sprite plot = FindPlotSprite();
        // [WP-C2] Kim cương: nền nút + icon lấy từ registry trong BuildGemButton; sprite tìm theo tên trong scene chỉ còn là fallback
        Sprite gem = FindNamedSprite("btn_RutNang_TGCay", "Btn_gem", "GemBox");
        // [WP-C2] Liềm: thử asset sprite trong project trước; không có mới tìm theo tên object trong scene như cũ
        Sprite sickle = Pick(FindSickleSpriteAsset(), FindNamedSprite("Sickle_Icon", "SickleTool", "Sickle_Bottom_Tray"));

        Transform oldContent = board.transform.Find("ContentPanel");
        if (oldContent != null) Undo.DestroyObjectImmediate(oldContent.gameObject);

        // 1. Root Content Panel (Floating Cards container - transparent dimming background)
        GameObject content = CreateUI("ContentPanel", board.transform);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        
        Image contentBg = AddOrGet<Image>(content);
        contentBg.color = new Color(0f, 0f, 0f, 0.45f); // Lớp dim tối 0.45 phía sau khung bảng (giữ nguyên)
        contentBg.raycastTarget = true;

        // 2. Container for 4 Pages
        GameObject pagesRoot = CreateUI("Popup_Pages", content.transform);
        RectTransform pagesRt = pagesRoot.GetComponent<RectTransform>();
        pagesRt.anchorMin = pagesRt.anchorMax = new Vector2(0.5f, 0.5f);
        pagesRt.sizeDelta = new Vector2(760f, 650f);
        pagesRt.anchoredPosition = Vector2.zero;

        var pages = new List<PageRefs>
        {
            BuildPlantPage(pagesRoot.transform, hand, ribbonSprite, slotSprite, plot, rice != null ? rice.icon : null),
            BuildSpeedPage(pagesRoot.transform, hand, ribbonSprite, slotSprite, rice != null ? rice.growingSprite : null, gem),
            BuildHarvestPage(pagesRoot.transform, hand, ribbonSprite, slotSprite, rice != null ? rice.readySprite : null, sickle),
            BuildResultPage(pagesRoot.transform, ribbonSprite, slotSprite, rice != null ? rice.harvestIcon : null, rice != null ? rice.icon : null),
        };

        // 3. Stepper 4 Dots at bottom
        var (stepperRoot, dotImages) = BuildStepperDots(pagesRoot.transform, dotOnSprite, dotOffSprite);

        // 4. Confirm Button
        Button confirm = BuildConfirmButton(pagesRoot.transform);

        // 5. Link References to TutorialGuideBoardUI
        var so = new SerializedObject(board);
        so.FindProperty("rootPanel").objectReferenceValue = board.gameObject;
        so.FindProperty("confirmButton").objectReferenceValue = confirm;
        
        // Stepper dots linking
        SerializedProperty dotsProp = so.FindProperty("stepDots");
        dotsProp.arraySize = dotImages.Count;
        for (int i = 0; i < dotImages.Count; i++)
        {
            dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dotImages[i];
        }
        so.FindProperty("dotOnSprite").objectReferenceValue = dotOnSprite;
        so.FindProperty("dotOffSprite").objectReferenceValue = dotOffSprite;

        // Pages linking
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
        Debug.Log("[Tutorial4Popup] Successfully built 4 high-end popup pages with GuideBoard art in SCN_Farm.");
    }

    private static void ConfigureSpriteImportSettings()
    {
        SetSpriteBorder(FrameSpritePath, new Vector4(72, 72, 72, 72));
        SetSpriteBorder(RibbonSpritePath, new Vector4(60, 0, 60, 0));
        // [WP-C2] Bản trong registry (thư mục board/) cũng cần border để Sliced không bị méo; importer null → bỏ qua
        SetSpriteBorder(UIStandardSprites.PathTutFrame, new Vector4(72, 72, 72, 72));
        SetSpriteBorder(UIStandardSprites.PathTutRibbon, new Vector4(60, 0, 60, 0));
        SetSpriteBorder(SlotSpritePath, new Vector4(40, 40, 40, 40));
        SetSpriteBorder(DotOnSpritePath, Vector4.zero);
        SetSpriteBorder(DotOffSpritePath, Vector4.zero);
    }

    private static void SetSpriteBorder(string path, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteBorder != border)
        {
            importer.spriteBorder = border;
            changed = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }

    private static PageRefs BuildPlantPage(
        Transform parent, Sprite hand, Sprite ribbon, Sprite slot, Sprite plot, Sprite seed)
    {
        GameObject page = BuildPageRoot(parent, "Popup_01_Plant_Rice", ribbon,
            "BƯỚC 1 — TRỒNG LÚA", "Chạm ô đất, rồi kéo hạt lúa vào ô.");
        RectTransform plotTarget = BuildTemplate(page.transform, "Template_01_Plot_Top", slot,
            new Vector2(0f, 95f), new Vector2(460f, 130f), plot, "Ô ĐẤT");
        RectTransform seedTarget = BuildTemplate(page.transform, "Template_02_SeedPanel_Bottom", slot,
            new Vector2(0f, -65f), new Vector2(460f, 130f), seed, "HẠT GIỐNG LÚA");
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_03_GuideBoard", page, animatedHand,
            seedTarget, plotTarget, 0.75f);
    }

    private static PageRefs BuildSpeedPage(
        Transform parent, Sprite hand, Sprite ribbon, Sprite slot, Sprite crop, Sprite gem)
    {
        GameObject page = BuildPageRoot(parent, "Popup_02_Diamond_Process", ribbon,
            "BƯỚC 2 — TĂNG TỐC", "Chạm ô lúa, rồi bấm kim cương để chín ngay.");
        RectTransform process = BuildTemplate(page.transform, "Template_Process_Diamond", slot,
            new Vector2(0f, 15f), new Vector2(500f, 220f), crop, "LÚA ĐANG LỚN");
        // [WP-C2] Nút kim cương thật (nền proc_btn_blue + icon kim cương) thay cho ô vuông xanh
        RectTransform gemTarget = BuildGemButton(process, "Diamond_Button", gem,
            new Vector2(150f, -50f), new Vector2(120f, 84f));
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_06b_GuideSpeedUp", page, animatedHand,
            gemTarget, gemTarget, 0.5f);
    }

    private static PageRefs BuildHarvestPage(
        Transform parent, Sprite hand, Sprite ribbon, Sprite slot, Sprite readyRice, Sprite sickle)
    {
        GameObject page = BuildPageRoot(parent, "Popup_03_Harvest_Sickle", ribbon,
            "BƯỚC 3 — THU HOẠCH", "Chạm lúa chín, kéo liềm vào ô lúa để gặt.");
        RectTransform riceTarget = BuildTemplate(page.transform, "Template_01_Ripe_Rice", slot,
            new Vector2(0f, 95f), new Vector2(460f, 130f), readyRice, "LÚA CHÍN");
        RectTransform sickleTarget = BuildTemplate(page.transform, "Template_02_Drag_Sickle", slot,
            new Vector2(0f, -65f), new Vector2(460f, 130f), sickle, "KÉO LIỀM GẶT");
        RectTransform animatedHand = BuildHand(page.transform, hand);
        return new PageRefs("L1L2_08b_GuideHarvest", page, animatedHand,
            sickleTarget, riceTarget, 0.8f);
    }

    private static PageRefs BuildResultPage(
        Transform parent, Sprite ribbon, Sprite slot, Sprite harvest, Sprite inventory)
    {
        GameObject page = BuildPageRoot(parent, "Popup_04_Harvest_Result", ribbon,
            "BƯỚC 4 — NHẬN KẾT QUẢ", "Thu hoạch thành công! Nhận ngay lúa vào kho.");
        // [WP-C2] Đổi tên Image_* → Card_* để AnimateIconsFloat (whitelist "Icon/Diamond_/Badge") KHÔNG xoay/nhấp nhô 2 thẻ này
        BuildTemplate(page.transform, "Card_Harvest_Drop", slot,
            new Vector2(-140f, 15f), new Vector2(230f, 240f), harvest, "BÓ LÚA VÀNG");
        BuildTemplate(page.transform, "Card_Rice_Collected", slot,
            new Vector2(140f, 15f), new Vector2(230f, 240f), inventory, "KHO LÚA (+4)");
        return new PageRefs("L1L2_09b_HarvestResult", page, null, null, null, 0f);
    }

    private static GameObject BuildPageRoot(
        Transform parent, string name, Sprite ribbonSprite, string title, string instruction)
    {
        GameObject page = CreateUI(name, parent);
        Stretch(page.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        // [WP-C2] Khung bảng thật (Board_Frame) — vẽ TRƯỚC ribbon/hướng dẫn/thẻ: sibling 0 trong trang,
        // tức ngay sau lớp dim 0.45 của ContentPanel. Kích thước 900×620, căn giữa.
        GameObject frameGo = CreateUI("Board_Frame", page.transform);
        frameGo.transform.SetSiblingIndex(0);
        RectTransform frameRt = frameGo.GetComponent<RectTransform>();
        frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
        frameRt.anchoredPosition = Vector2.zero;
        frameRt.sizeDelta = new Vector2(900f, 620f);
        Image frameImg = AddOrGet<Image>(frameGo);
        frameImg.raycastTarget = false;
        if (s_frameSprite != null)
        {
            frameImg.sprite = s_frameSprite;
            frameImg.type = Image.Type.Sliced;
            // Ám kem ấm #FFF6E5: sprite nền trắng → ra màu kem; sprite đã có màu giấy → gần như không đổi
            frameImg.color = new Color32(0xFF, 0xF6, 0xE5, 0xFF);
        }
        else
        {
            frameImg.sprite = null;
            frameImg.color = new Color(0.98f, 0.93f, 0.82f, 1f); // fallback: tấm giấy kem phẳng
        }

        // Header Ribbon Banner
        GameObject ribbonGo = CreateUI("Header_Ribbon", page.transform);
        RectTransform ribbonRt = ribbonGo.GetComponent<RectTransform>();
        ribbonRt.anchorMin = ribbonRt.anchorMax = new Vector2(0.5f, 0.5f);
        ribbonRt.anchoredPosition = new Vector2(0f, 240f);
        ribbonRt.sizeDelta = new Vector2(480f, 62f);

        Image ribbonImg = AddOrGet<Image>(ribbonGo);
        // [WP-C2] Ribbon chưa có sprite (vd. tái dùng object cũ) → lấy TutRibbon từ registry
        if (ribbonSprite == null && ribbonImg.sprite == null) ribbonSprite = UIStandardSprites.TutRibbon;
        if (ribbonSprite != null)
        {
            ribbonImg.sprite = ribbonSprite;
            ribbonImg.type = Image.Type.Sliced;
            ribbonImg.color = Color.white;
        }
        else
        {
            ribbonImg.color = new Color(0.6f, 0.15f, 0.12f, 1f);
        }

        // Title text inside Ribbon
        // [WP-C2] Tiêu đề tự co 20–26 để không tràn ribbon
        TMP_Text titleText = BuildText(ribbonGo.transform, "Title", title, 25f,
            new Vector2(0f, 3f), new Vector2(440f, 48f), 20f, 26f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.95f, 0.78f); // Warm cream/gold

        // Instruction Text below Ribbon
        if (!string.IsNullOrEmpty(instruction))
        {
            // [WP-C2] Khung 720×72, tự co 16–22, xuống dòng; hạ y=176 để không chồng lên ribbon (đáy ribbon ≈ 209)
            TMP_Text instructionText = BuildText(page.transform, "Instruction",
                instruction, 20f, new Vector2(0f, 176f), new Vector2(720f, 72f), 16f, 22f);
            instructionText.fontStyle = FontStyles.Normal;
            instructionText.color = new Color(0.38f, 0.22f, 0.08f); // Rich warm brown for parchment
        }
        return page;
    }

    private static RectTransform BuildTemplate(
        Transform parent, string name, Sprite slotSprite, Vector2 position, Vector2 size,
        Sprite sprite, string label)
    {
        GameObject card = CreateUI(name, parent);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        
        Image bg = AddOrGet<Image>(card);
        if (slotSprite != null)
        {
            bg.sprite = slotSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.94f, 0.82f, 0.55f, 1f);
        }

        GameObject imageGo = CreateUI("Image", card.transform);
        RectTransform imageRt = imageGo.GetComponent<RectTransform>();
        imageRt.anchorMin = imageRt.anchorMax = new Vector2(0.5f, 0.5f);
        imageRt.anchoredPosition = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(0f, 12f);
        imageRt.sizeDelta = new Vector2(size.y * 0.65f, size.y * 0.65f);

        Image image = AddOrGet<Image>(imageGo);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : new Color(0.55f, 0.38f, 0.18f);

        if (!string.IsNullOrEmpty(label))
        {
            // [WP-C2] Nhãn thẻ tự co 12–17 để không tràn ra ngoài thẻ / đè lên hình
            TMP_Text labelText = BuildText(card.transform, "Label", label, 17f,
                new Vector2(0f, -size.y * 0.34f), new Vector2(size.x - 20f, 30f), 12f, 17f);
            labelText.fontStyle = FontStyles.Bold;
            labelText.color = new Color(0.42f, 0.24f, 0.10f);
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
        Image image = AddOrGet<Image>(badge);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = sprite != null ? Color.white : new Color(0.2f, 0.75f, 1f);
        return rt;
    }

    /// <summary>
    /// [WP-C2] Nút kim cương "thật": nền proc_btn_blue (Sliced) + con Icon_Gem (kimcuong) cao 60% nút.
    /// Fallback theo thứ tự: registry → sprite tìm theo tên trong scene (cũ) → ô xanh phẳng (cũ).
    /// </summary>
    private static RectTransform BuildGemButton(
        RectTransform parent, string name, Sprite fallbackGem, Vector2 position, Vector2 size)
    {
        GameObject badge = CreateUI(name, parent);
        RectTransform rt = badge.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image bg = AddOrGet<Image>(badge);
        bg.raycastTarget = false;
        Sprite btnBg = UIStandardSprites.BtnGem;
        if (btnBg != null)
        {
            bg.sprite = btnBg;
            bg.type = Image.Type.Sliced;
            bg.preserveAspect = false;
            bg.color = Color.white;
        }
        else if (fallbackGem != null)
        {
            bg.sprite = fallbackGem;          // cách cũ: dùng thẳng sprite tìm theo tên
            bg.type = Image.Type.Simple;
            bg.preserveAspect = true;
            bg.color = Color.white;
        }
        else
        {
            bg.sprite = null;
            bg.color = new Color(0.2f, 0.75f, 1f); // fallback cuối: ô xanh phẳng như trước
        }

        // Icon kim cương con — tên bắt đầu bằng "Icon" nên vẫn được AnimateIconsFloat cho nhấp nhô
        Sprite gemIcon = Pick(UIStandardSprites.IconGem, fallbackGem);
        GameObject iconGo = CreateUI("Icon_Gem", badge.transform);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(size.y * 0.6f, size.y * 0.6f);
        Image icon = AddOrGet<Image>(iconGo);
        icon.sprite = gemIcon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.color = Color.white;
        icon.enabled = gemIcon != null && btnBg != null; // không có nền riêng thì nền đã là icon → ẩn con để khỏi vẽ 2 lần
        return rt;
    }

    private static RectTransform BuildHand(Transform parent, Sprite sprite)
    {
        GameObject hand = CreateUI("Hand_Animated", parent);
        RectTransform rt = hand.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(92f, 92f);
        Image image = AddOrGet<Image>(hand);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return rt;
    }

    private static (GameObject root, List<Image> dots) BuildStepperDots(
        Transform parent, Sprite dotOn, Sprite dotOff)
    {
        GameObject stepper = CreateUI("Stepper_Dots", parent);
        RectTransform rt = stepper.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 102f);
        rt.sizeDelta = new Vector2(160f, 32f);

        var dots = new List<Image>();
        float startX = -45f;
        float spacing = 30f;

        for (int i = 0; i < 4; i++)
        {
            GameObject dotGo = CreateUI($"Dot_{i + 1:D2}", stepper.transform);
            RectTransform dotRt = dotGo.GetComponent<RectTransform>();
            dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            dotRt.sizeDelta = new Vector2(24f, 24f);

            Image dotImg = AddOrGet<Image>(dotGo);
            dotImg.sprite = (i == 0) ? dotOn : dotOff; // [WP-C2] dotOn/dotOff đã ưu tiên TutDotOn/TutDotOff từ registry
            dotImg.preserveAspect = true;
            dotImg.raycastTarget = false;
            dots.Add(dotImg);
        }

        return (stepper, dots);
    }

    private static Button BuildConfirmButton(Transform parent)
    {
        GameObject buttonGo = CreateUI("ConfirmButton", parent);
        RectTransform rt = buttonGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        // [WP-C2] Nút 300×72, nâng y=56 để mép dưới nằm trong khung bảng 620 cao
        rt.anchoredPosition = new Vector2(0f, 56f);
        rt.sizeDelta = new Vector2(300f, 72f);

        Image bg = AddOrGet<Image>(buttonGo);
        Sprite btnSprite = UIStandardSprites.BtnGreen; // btn_big_green
        if (btnSprite != null)
        {
            bg.sprite = btnSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.sprite = null;
            bg.color = new Color(0.24f, 0.68f, 0.22f); // fallback: nút xanh phẳng như cũ
        }
        Button button = AddOrGet<Button>(buttonGo);
        button.targetGraphic = bg;

        // Nhãn 28 bold, chừa padding (24,12) để chữ không dính mép sprite nút
        TMP_Text text = BuildText(buttonGo.transform, "Text", "ĐÃ RÕ", 28f,
            Vector2.zero, rt.sizeDelta - new Vector2(24f, 12f));
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        return button;
    }

    /// <summary>
    /// Tạo TMP text căn giữa, xuống dòng. [WP-C2] Thêm <paramref name="minAuto"/>/<paramref name="maxAuto"/>:
    /// khi maxAuto &gt; 0 bật Auto Size trong khoảng [minAuto, maxAuto] để chữ tự co vừa khung, không đè nhau.
    /// </summary>
    private static TMP_Text BuildText(
        Transform parent, string name, string value, float size,
        Vector2 position, Vector2 dimensions, float minAuto = 0f, float maxAuto = 0f)
    {
        GameObject go = CreateUI(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = dimensions;
        TextMeshProUGUI text = AddOrGet<TextMeshProUGUI>(go);
        text.text = value;
        text.fontSize = size;
        if (maxAuto > 0f)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = minAuto > 0f ? minAuto : size * 0.7f;
            text.fontSizeMax = maxAuto;
        }
        else
        {
            text.enableAutoSizing = false;
        }
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

    /// <summary>
    /// [WP-C2] Tìm sprite liềm trong project: đường dẫn đã biết trước, sau đó quét AssetDatabase theo từ khoá
    /// ("liem", "sickle") lấy kết quả đầu tiên. Trả null nếu không có → caller quay về tìm theo tên trong scene.
    /// </summary>
    private static Sprite FindSickleSpriteAsset()
    {
        string[] knownPaths =
        {
            "Assets/Assetsgame/liemthuhoach-removebg-preview.png",
            "Assets/Anh/liemthuhoach-removebg-preview.png",
        };
        foreach (string path in knownPaths)
        {
            Sprite s = UIStandardSprites.Load(path);
            if (s != null) return s;
        }

        foreach (string filter in new[] { "liem t:Sprite", "sickle t:Sprite" })
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0) continue;
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (s != null) return s;
        }
        return null;
    }

    /// <summary>
    /// Tạo GameObject UI con. [WP-C2] Idempotent: nếu <paramref name="parent"/> đã có con cùng tên thì tái dùng,
    /// không tạo trùng — nhờ đó chạy tool nhiều lần không sinh object thừa.
    /// </summary>
    private static GameObject CreateUI(string name, Transform parent)
    {
        Transform existing = parent != null ? parent.Find(name) : null;
        if (existing != null)
        {
            Undo.RecordObject(existing.gameObject, "Reuse " + name);
            return existing.gameObject;
        }

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    /// <summary>[WP-C2] Lấy component có sẵn, chưa có mới thêm (tránh lỗi thêm trùng Graphic khi tái dùng object).</summary>
    private static T AddOrGet<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(go);
    }

    /// <summary>[WP-C2] Chọn sprite ưu tiên, null thì lấy fallback (so sánh kiểu Unity, không dùng ??).</summary>
    private static Sprite Pick(Sprite preferred, Sprite fallback)
    {
        return preferred != null ? preferred : fallback;
    }

    private static void Stretch(
        RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
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
