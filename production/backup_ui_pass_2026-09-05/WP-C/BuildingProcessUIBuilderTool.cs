#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildingProcessUIBuilderTool
{
    private const string SpritesFolder = "Assets/Assetsgame/popup/ui_building_svg/generated_sprites";
    private const string DesignAssetsFolder = "Assets/Assetsgame";

    [MenuItem("Tools/Farm/Process UI/Dựng & Áp Dụng Toàn Bộ Process UI Mới (ui_building_svg)")]
    public static void BuildAndApplyAllProcessUI()
    {
        // 1. Tự động sinh/cập nhật sprites 9-slice sắc nét
        BuildingProcessSpriteGenerator.GenerateAllSprites();

        // 2. Load Resources / Sprites / Fonts
        TMP_FontAsset fontVo = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");
        if (fontVo == null)
            fontVo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Resources/Fonts/Baloo2 SDF.asset");

        Sprite frameBgSpr = LoadSprite(SpritesFolder, "proc_frame_bg.png");
        Sprite trackBgSpr = LoadSprite(SpritesFolder, "proc_track_bg.png");
        Sprite fillGreenSpr = LoadSprite(SpritesFolder, "proc_fill_green.png");
        Sprite btnBlueSpr = LoadSprite(SpritesFolder, "proc_btn_blue.png");
        Sprite diamondIconSpr = LoadSprite(DesignAssetsFolder, "kimcuong.png") ?? LoadSprite(DesignAssetsFolder, "kimcuong-removebg-preview.png");

        // 3. Điêu khắc lại toàn bộ CropProcessPopupUI trong Active Scene
        var allCropPopups = Object.FindObjectsByType<CropProcessPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int cropCount = 0;
        foreach (var cropUI in allCropPopups)
        {
            ReskinCropProcessPopup(cropUI, frameBgSpr, trackBgSpr, fillGreenSpr, btnBlueSpr, diamondIconSpr, fontVo);
            cropCount++;
        }

        // 4. Điêu khắc lại toàn bộ TrainProcessPopupUI trong Active Scene
        var allTrainPopups = Object.FindObjectsByType<TrainProcessPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int trainCount = 0;
        foreach (var trainUI in allTrainPopups)
        {
            ReskinTrainProcessPopup(trainUI, frameBgSpr, trackBgSpr, fillGreenSpr, btnBlueSpr, diamondIconSpr, fontVo);
            trainCount++;
        }

        // 5. Cập nhật Prefab PF_PenMiniPanel và tất cả Chuồng Gia Súc (Pen_01, Pen_02, Pen_03, Pen_04)
        ReskinAllAnimalPens(frameBgSpr, trackBgSpr, fillGreenSpr, btnBlueSpr, diamondIconSpr, fontVo);

        // Đánh dấu Scene Dirty & Lưu lại Scene
        if (allCropPopups.Length > 0 || allTrainPopups.Length > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"[ProcessUI] Đã điêu khắc giao diện mới thành công cho {cropCount} Ô Đất Cây Trồng, {trainCount} Ga Tàu Hoả & Toàn Bộ Chuồng Trại!");
    }

    public static void ReskinCropProcessPopup(CropProcessPopupUI cropUI, Sprite frameBgSpr, Sprite trackBgSpr, Sprite fillGreenSpr, Sprite btnBlueSpr, Sprite diamondIconSpr, TMP_FontAsset fontVo)
    {
        if (cropUI == null) return;
        GameObject rootGO = cropUI.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(rootGO, "Reskin Crop Process UI");

        // 1. Xoá sạch các con cũ bị xấu/lỗi thời bên trong
        List<GameObject> toDelete = new List<GameObject>();
        for (int i = 0; i < rootGO.transform.childCount; i++)
        {
            toDelete.Add(rootGO.transform.GetChild(i).gameObject);
        }
        foreach (var go in toDelete)
        {
            Object.DestroyImmediate(go);
        }

        // 2. Căn chỉnh Root RectTransform & Scale
        RectTransform rootRect = rootGO.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = rootGO.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(360f, 84f);
        rootRect.localScale = Vector3.one; // Khắc phục tỉ lệ bị co nhỏ 0.5

        // Đảm bảo Canvas_Popup có Sorting Order cao (300) để luôn vẽ đè lên trên ô đất & cây trồng
        Canvas parentCanvas = rootGO.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 300;
            EditorUtility.SetDirty(parentCanvas);
        }

        // Khung Nền Kem Viền Gỗ Nâu (Frame Base)
        Image rootImg = rootGO.GetComponent<Image>();
        if (rootImg == null) rootImg = rootGO.AddComponent<Image>();
        rootImg.sprite = frameBgSpr;
        rootImg.type = Image.Type.Sliced;
        rootImg.raycastTarget = true;

        // 3. Tên Cây Trồng (Header Text) - Màu Trắng Nổi Bật
        RectTransform nameRect = CreateRect(rootGO.transform, "Txt_CropName", new Vector2(230f, 28f), new Vector2(-48f, 52f));
        TMP_Text txtCropName = CreateText(nameRect, "LÚA MÌ", 22f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var nameOutline = nameRect.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0.2f, 0.12f, 0.05f, 1f);
        nameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 4. Rãnh Tiến Độ Nâu (Track Bar)
        RectTransform trackRect = CreateRect(rootGO.transform, "Track_Bar", new Vector2(230f, 38f), new Vector2(-48f, 0f));
        Image trackImg = trackRect.gameObject.AddComponent<Image>();
        trackImg.sprite = trackBgSpr;
        trackImg.type = Image.Type.Sliced;
        trackImg.raycastTarget = false;

        // 5. Thanh Xanh Lá Gradient 3D Fill (Progress Fill)
        RectTransform fillRect = CreateRect(trackRect, "Progress_Fill", new Vector2(222f, 30f), Vector2.zero);
        Image fillImg = fillRect.gameObject.AddComponent<Image>();
        fillImg.sprite = fillGreenSpr;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0.65f;
        fillImg.raycastTarget = false;

        // 6. Text Thời Gian Còn Lại ("00:45") Căn Giữa Thanh Xanh
        RectTransform timeRect = CreateRect(trackRect, "Txt_TimeRemaining", new Vector2(210f, 30f), Vector2.zero);
        TMP_Text txtTimeRemaining = CreateText(timeRect, "00:45", 20f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var timeOutline = timeRect.gameObject.AddComponent<Outline>();
        timeOutline.effectColor = new Color(0.18f, 0.31f, 0.06f, 1f);
        timeOutline.effectDistance = new Vector2(1f, -1f);

        // 7. Nút Kim Cương Xanh Dương 3D (Btn_SpeedUp) - Nằm bên phải khung
        RectTransform btnRect = CreateRect(rootGO.transform, "Btn_SpeedUp", new Vector2(88f, 60f), new Vector2(124f, 0f));
        Image btnImg = btnRect.gameObject.AddComponent<Image>();
        btnImg.sprite = btnBlueSpr;
        btnImg.type = Image.Type.Sliced;
        Button btnSpeedUp = btnRect.gameObject.AddComponent<Button>();

        // 7a. Icon Kim Cương Đồng Bộ Trong Game
        RectTransform diaIconRect = CreateRect(btnRect, "Icon_Diamond", new Vector2(32f, 32f), new Vector2(-16f, 0f));
        Image diaIconImg = diaIconRect.gameObject.AddComponent<Image>();
        diaIconImg.sprite = diamondIconSpr;
        diaIconImg.preserveAspect = true;
        diaIconImg.raycastTarget = false;

        // 7b. Text Số Lượng Kim Cương Cần Dùng ("1" / "2")
        RectTransform costRect = CreateRect(btnRect, "Txt_GemCost", new Vector2(36f, 30f), new Vector2(18f, 0f));
        TMP_Text txtGemCost = CreateText(costRect, "1", 22f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var costOutline = costRect.gameObject.AddComponent<Outline>();
        costOutline.effectColor = new Color(0.11f, 0.36f, 0.53f, 1f);
        costOutline.effectDistance = new Vector2(1f, -1f);

        // 8. Gán Tham Chiếu Vào CropProcessPopupUI
        cropUI.txtCropName = txtCropName;
        cropUI.txtTimeRemaining = txtTimeRemaining;
        cropUI.progressFill = fillImg;
        cropUI.btnSpeedUp = btnSpeedUp;
        cropUI.txtGemCost = txtGemCost;
        cropUI.imgDiamondIcon = diaIconImg;
        cropUI.AutoBindComponents();

        EditorUtility.SetDirty(cropUI);
        EditorUtility.SetDirty(rootGO);
    }

    public static void ReskinTrainProcessPopup(TrainProcessPopupUI trainUI, Sprite frameBgSpr, Sprite trackBgSpr, Sprite fillGreenSpr, Sprite btnBlueSpr, Sprite diamondIconSpr, TMP_FontAsset fontVo)
    {
        if (trainUI == null) return;
        GameObject rootGO = trainUI.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(rootGO, "Reskin Train Process UI");

        // 1. Xoá sạch các con cũ bên trong
        List<GameObject> toDelete = new List<GameObject>();
        for (int i = 0; i < rootGO.transform.childCount; i++)
        {
            toDelete.Add(rootGO.transform.GetChild(i).gameObject);
        }
        foreach (var go in toDelete)
        {
            Object.DestroyImmediate(go);
        }

        // 2. Căn chỉnh Root RectTransform & Scale
        RectTransform rootRect = rootGO.GetComponent<RectTransform>();
        if (rootRect == null) rootRect = rootGO.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(360f, 84f);
        rootRect.localScale = Vector3.one;

        // Khung Nền Kem Viền Gỗ Nâu (Frame Base)
        Image rootImg = rootGO.GetComponent<Image>();
        if (rootImg == null) rootImg = rootGO.AddComponent<Image>();
        rootImg.sprite = frameBgSpr;
        rootImg.type = Image.Type.Sliced;
        rootImg.raycastTarget = true;

        // 3. Header Status - Màu Trắng Nổi Bật
        RectTransform nameRect = CreateRect(rootGO.transform, "Txt_Status", new Vector2(230f, 28f), new Vector2(-48f, 52f));
        TMP_Text txtStatus = CreateText(nameRect, "GA TÀU HOẢ", 22f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var nameOutline = nameRect.gameObject.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0.2f, 0.12f, 0.05f, 1f);
        nameOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 4. Rãnh Tiến Độ Nâu (Track Bar)
        RectTransform trackRect = CreateRect(rootGO.transform, "Track_Bar", new Vector2(230f, 38f), new Vector2(-48f, 0f));
        Image trackImg = trackRect.gameObject.AddComponent<Image>();
        trackImg.sprite = trackBgSpr;
        trackImg.type = Image.Type.Sliced;
        trackImg.raycastTarget = false;

        // 5. Thanh Xanh Lá Gradient 3D Fill (Progress Fill)
        RectTransform fillRect = CreateRect(trackRect, "Progress_Fill", new Vector2(222f, 30f), Vector2.zero);
        Image fillImg = fillRect.gameObject.AddComponent<Image>();
        fillImg.sprite = fillGreenSpr;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0.65f;
        fillImg.raycastTarget = false;

        // 6. Text Thời Gian Còn Lại ("00:45") Căn Giữa Thanh Xanh
        RectTransform timeRect = CreateRect(trackRect, "Txt_TimeRemaining", new Vector2(210f, 30f), Vector2.zero);
        TMP_Text txtTimeRemaining = CreateText(timeRect, "00:45", 20f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var timeOutline = timeRect.gameObject.AddComponent<Outline>();
        timeOutline.effectColor = new Color(0.18f, 0.31f, 0.06f, 1f);
        timeOutline.effectDistance = new Vector2(1f, -1f);

        // 7. Nút Kim Cương Xanh Dương 3D (Btn_SpeedUp) - Nằm bên phải khung
        RectTransform btnRect = CreateRect(rootGO.transform, "Btn_SpeedUp", new Vector2(88f, 60f), new Vector2(124f, 0f));
        Image btnImg = btnRect.gameObject.AddComponent<Image>();
        btnImg.sprite = btnBlueSpr;
        btnImg.type = Image.Type.Sliced;
        Button btnSpeedUp = btnRect.gameObject.AddComponent<Button>();

        // 7a. Icon Kim Cương Đồng Bộ Trong Game
        RectTransform diaIconRect = CreateRect(btnRect, "Icon_Diamond", new Vector2(32f, 32f), new Vector2(-16f, 0f));
        Image diaIconImg = diaIconRect.gameObject.AddComponent<Image>();
        diaIconImg.sprite = diamondIconSpr;
        diaIconImg.preserveAspect = true;
        diaIconImg.raycastTarget = false;

        // 7b. Text Số Lượng Kim Cương Cần Dùng
        RectTransform costRect = CreateRect(btnRect, "Txt_GemCost", new Vector2(36f, 30f), new Vector2(18f, 0f));
        TMP_Text txtGemCost = CreateText(costRect, "1", 22f, Color.white, fontVo, TextAlignmentOptions.Center, true);
        var costOutline = costRect.gameObject.AddComponent<Outline>();
        costOutline.effectColor = new Color(0.11f, 0.36f, 0.53f, 1f);
        costOutline.effectDistance = new Vector2(1f, -1f);

        trainUI.AutoBindComponents();

        EditorUtility.SetDirty(trainUI);
        EditorUtility.SetDirty(rootGO);
    }

    private static void ReskinAllAnimalPens(Sprite frameBgSpr, Sprite trackBgSpr, Sprite fillGreenSpr, Sprite btnBlueSpr, Sprite diamondIconSpr, TMP_FontAsset fontVo)
    {
        // 1. Prefab cơ sở
        string[] penPrefabPaths = new string[]
        {
            "Assets/_Game/Farm/Prefabs/PF_PenMiniPanel.prefab",
            "Assets/_Game/Farm/CÔNG TRÌNH/Pen_01.prefab",
            "Assets/_Game/Farm/CÔNG TRÌNH/Pen_02.prefab",
            "Assets/_Game/Farm/CÔNG TRÌNH/Pen_03.prefab",
            "Assets/_Game/Farm/CÔNG TRÌNH/Pen_04.prefab",
            "Assets/_Game/Farm/Frefab_home/May_01.prefab",
            "Assets/_Game/Farm/Frefab_home/May_02.prefab",
            "Assets/_Game/Farm/Frefab_home/May_03.prefab"
        };

        int soPrefabXong = 0, soPrefabLoi = 0;

        foreach (var path in penPrefabPaths)
        {
            // ─── SỬA LỖI "Setting the parent of a transform which resides in a Prefab Asset" ───
            // TRƯỚC ĐÂY: AssetDatabase.LoadAssetAtPath<GameObject>(path) rồi Reskin thẳng.
            // Unity CẤM Transform.SetParent() vào transform nằm trong prefab asset (chống hỏng dữ liệu),
            // nên CreateRect() thất bại: object mới KHÔNG vào được prefab mà bị RƠI RA GỐC SCENE
            // đang mở (đã tìm thấy 4 object "Txt_PenName" rác trong SCN_Farm.unity vì lỗi này).
            // Tệ hơn: hàm vẫn chạy tới cuối nên tool báo "thành công" trong khi 8 prefab không đổi gì.
            // ĐÚNG CÁCH: mở nội dung prefab ra một scene tạm bằng LoadPrefabContents, sửa trên bản tạm,
            // rồi SaveAsPrefabAsset + UnloadPrefabContents. GUID prefab giữ nguyên nên instance
            // đã đặt trong scene KHÔNG bị "Missing Prefab".
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"[ProcessUI] Không thấy prefab {path} — bỏ qua.");
                continue;
            }

            GameObject goc = null;
            try
            {
                goc = PrefabUtility.LoadPrefabContents(path);
                if (goc == null)
                {
                    Debug.LogError($"[ProcessUI] Không mở được nội dung prefab {path}.");
                    soPrefabLoi++;
                    continue;
                }

                PenMiniPanelUI penUI = goc.GetComponentInChildren<PenMiniPanelUI>(true);
                if (penUI == null) continue;   // prefab này không có panel — không phải lỗi

                ReskinPenMiniPanelObject(penUI, frameBgSpr, trackBgSpr, fillGreenSpr, btnBlueSpr, diamondIconSpr, fontVo);
                PrefabUtility.SaveAsPrefabAsset(goc, path);
                soPrefabXong++;
            }
            catch (System.Exception e)
            {
                // Báo LỖI THẬT thay vì im lặng rồi cuối cùng in "thành công".
                Debug.LogError($"[ProcessUI] Reskin prefab {path} THẤT BẠI: {e.Message}");
                soPrefabLoi++;
            }
            finally
            {
                // BẮT BUỘC unload kể cả khi lỗi, nếu không scene tạm bị treo trong bộ nhớ.
                if (goc != null) PrefabUtility.UnloadPrefabContents(goc);
            }
        }

        if (soPrefabLoi > 0)
            Debug.LogError($"[ProcessUI] {soPrefabLoi}/{penPrefabPaths.Length} prefab reskin THẤT BẠI — " +
                           "đọc các dòng đỏ phía trên. KHÔNG được coi là thành công.");
        else
            Debug.Log($"[ProcessUI] Reskin {soPrefabXong}/{penPrefabPaths.Length} prefab OK (mở bằng LoadPrefabContents, GUID giữ nguyên).");

        // 2. Toàn bộ Scene instances
        var scenePenUIs = Object.FindObjectsByType<PenMiniPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var penUI in scenePenUIs)
        {
            ReskinPenMiniPanelObject(penUI, frameBgSpr, trackBgSpr, fillGreenSpr, btnBlueSpr, diamondIconSpr, fontVo);
            EditorUtility.SetDirty(penUI.gameObject);
        }

        AssetDatabase.SaveAssets();
    }

    public static void ReskinPenMiniPanelObject(PenMiniPanelUI penUI, Sprite frameBgSpr, Sprite trackBgSpr, Sprite fillGreenSpr, Sprite btnBlueSpr, Sprite diamondIconSpr, TMP_FontAsset fontVo)
    {
        if (penUI == null) return;
        Transform rootTf = penUI.transform;

        // ── 1. Khay Chọn Thức Ăn (PanelContent) ──────────────────────────────────
        Transform panelContent = rootTf.Find("PanelContent") ?? rootTf.Find("panelContent");
        if (panelContent != null)
        {
            RectTransform pcRect = panelContent.GetComponent<RectTransform>();
            if (pcRect != null) pcRect.sizeDelta = new Vector2(270f, 136f);

            // Nền Khung Bo Góc Màu Kem Viền Gỗ
            Image bgImg = panelContent.Find("Background")?.GetComponent<Image>() ?? panelContent.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.sprite = frameBgSpr;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }

            // Slot Thức Ăn 1
            Transform slot1 = panelContent.Find("Slot_Food1") ?? panelContent.Find("slot1Root");
            if (slot1 != null)
            {
                RectTransform s1Rect = slot1.GetComponent<RectTransform>();
                s1Rect.anchoredPosition = new Vector2(-60f, 0f);
                s1Rect.sizeDelta = new Vector2(96f, 110f);

                // Đĩa lót
                Image plate1 = slot1.Find("Plate")?.GetComponent<Image>() ?? slot1.GetComponent<Image>();
                if (plate1 != null)
                {
                    plate1.sprite = trackBgSpr;
                    plate1.type = Image.Type.Sliced;
                }

                // Gắn Button hỗ trợ Chạm vào để cho ăn
                Button btn1 = slot1.GetComponent<Button>();
                if (btn1 == null) btn1 = slot1.gameObject.AddComponent<Button>();
                btn1.onClick.RemoveAllListeners();
                btn1.onClick.AddListener(penUI.OnSlot1Clicked);
            }

            // Slot Thức Ăn 2
            Transform slot2 = panelContent.Find("Slot_Food2") ?? panelContent.Find("slot2Root");
            if (slot2 != null)
            {
                RectTransform s2Rect = slot2.GetComponent<RectTransform>();
                s2Rect.anchoredPosition = new Vector2(60f, 0f);
                s2Rect.sizeDelta = new Vector2(96f, 110f);

                // Đĩa lót
                Image plate2 = slot2.Find("Plate")?.GetComponent<Image>() ?? slot2.GetComponent<Image>();
                if (plate2 != null)
                {
                    plate2.sprite = trackBgSpr;
                    plate2.type = Image.Type.Sliced;
                }

                // Gắn Button hỗ trợ Chạm vào để cho ăn
                Button btn2 = slot2.GetComponent<Button>();
                if (btn2 == null) btn2 = slot2.gameObject.AddComponent<Button>();
                btn2.onClick.RemoveAllListeners();
                btn2.onClick.AddListener(penUI.OnSlot2Clicked);
            }

            // Slot Rổ Thu Hoạch
            Transform basketSlot = panelContent.Find("Slot_Basket") ?? panelContent.Find("basketRoot");
            if (basketSlot != null)
            {
                RectTransform bRect = basketSlot.GetComponent<RectTransform>();
                bRect.anchoredPosition = Vector2.zero;
                bRect.sizeDelta = new Vector2(100f, 110f);

                // Đĩa lót
                Image bPlate = basketSlot.Find("Plate")?.GetComponent<Image>() ?? basketSlot.GetComponent<Image>();
                if (bPlate != null)
                {
                    bPlate.sprite = trackBgSpr;
                    bPlate.type = Image.Type.Sliced;
                }

                // Gắn Button hỗ trợ Chạm vào để thu hoạch ngay
                Button btnB = basketSlot.GetComponent<Button>();
                if (btnB == null) btnB = basketSlot.gameObject.AddComponent<Button>();
                btnB.onClick.RemoveAllListeners();
                btnB.onClick.AddListener(penUI.OnBasketClicked);
            }
        }

        // ── 2. Khung Tiến Độ Nuôi (ProgressOverlay) ──────────────────────────────
        Transform progressOverlay = rootTf.Find("Progress_Overlay") ?? rootTf.Find("progressOverlay") ?? rootTf.Find("PanelContent/ProgressOverlay");
        if (progressOverlay != null)
        {
            RectTransform poRect = progressOverlay.GetComponent<RectTransform>();
            poRect.sizeDelta = new Vector2(360f, 84f);
            poRect.anchoredPosition = Vector2.zero;

            // Khung Nền Kem Viền Gỗ Nâu
            Image poBg = progressOverlay.GetComponent<Image>();
            if (poBg == null) poBg = progressOverlay.gameObject.AddComponent<Image>();
            poBg.sprite = frameBgSpr;
            poBg.type = Image.Type.Sliced;
            poBg.color = Color.white;

            // Header Tên Chuồng Trại - Màu Trắng Nổi Bật
            Transform nameTr = progressOverlay.Find("Txt_PenName");
            TMP_Text txtPenName = null;
            if (nameTr == null)
            {
                RectTransform nameRect = CreateRect(progressOverlay, "Txt_PenName", new Vector2(230f, 28f), new Vector2(-48f, 52f));
                txtPenName = CreateText(nameRect, "CHUỒNG NUÔI", 22f, Color.white, fontVo, TextAlignmentOptions.Center, true);
                var nameOutline = nameRect.gameObject.AddComponent<Outline>();
                nameOutline.effectColor = new Color(0.2f, 0.12f, 0.05f, 1f);
                nameOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }
            else
            {
                txtPenName = nameTr.GetComponent<TMP_Text>();
                if (txtPenName != null)
                {
                    txtPenName.color = Color.white;
                    txtPenName.fontSize = 22f;
                    txtPenName.fontStyle = FontStyles.Bold;
                }
            }

            SetPrivateField(penUI, "txtPenTitle", txtPenName);

            // Rãnh Nền Nâu
            Transform trackTr = progressOverlay.Find("Track_Bar") ?? progressOverlay.Find("Background");
            if (trackTr != null)
            {
                RectTransform trackRect = trackTr.GetComponent<RectTransform>();
                trackRect.sizeDelta = new Vector2(230f, 38f);
                trackRect.anchoredPosition = new Vector2(-48f, 0f);

                Image trackImg = trackTr.GetComponent<Image>();
                if (trackImg != null)
                {
                    trackImg.sprite = trackBgSpr;
                    trackImg.type = Image.Type.Sliced;
                }
            }

            // Thanh Fill Xanh 3D
            Transform fillTr = progressOverlay.Find("ProgressFill") ?? progressOverlay.Find("Progress_Fill") ?? progressOverlay.Find("Track_Bar/Progress_Fill");
            if (fillTr != null)
            {
                RectTransform fillRect = fillTr.GetComponent<RectTransform>();
                fillRect.sizeDelta = new Vector2(222f, 30f);
                fillRect.anchoredPosition = Vector2.zero;

                Image fillImg = fillTr.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.sprite = fillGreenSpr;
                    fillImg.type = Image.Type.Filled;
                    fillImg.fillMethod = Image.FillMethod.Horizontal;
                    fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                }
            }

            // Text Thời Gian
            Transform timerTr = progressOverlay.Find("TimerText") ?? progressOverlay.Find("Txt_TimeRemaining") ?? progressOverlay.Find("Track_Bar/TimerText");
            if (timerTr != null)
            {
                RectTransform timerRect = timerTr.GetComponent<RectTransform>();
                timerRect.sizeDelta = new Vector2(210f, 30f);
                timerRect.anchoredPosition = Vector2.zero;

                TMP_Text tmpTimer = timerTr.GetComponent<TMP_Text>();
                if (tmpTimer != null)
                {
                    tmpTimer.color = Color.white;
                    tmpTimer.fontSize = 20f;
                    tmpTimer.fontStyle = FontStyles.Bold;
                    tmpTimer.alignment = TextAlignmentOptions.Center;
                }

                Outline timerOutline = timerTr.GetComponent<Outline>();
                if (timerOutline == null) timerOutline = timerTr.gameObject.AddComponent<Outline>();
                timerOutline.effectColor = new Color(0.18f, 0.31f, 0.06f, 1f);
                timerOutline.effectDistance = new Vector2(1f, -1f);
            }

            // Nút Kim Cương Xanh Dương (btn_PenGem)
            Transform gemBtnTr = rootTf.Find("btn_PenGem") ?? progressOverlay.Find("btn_PenGem");
            if (gemBtnTr != null)
            {
                RectTransform gemBtnRect = gemBtnTr.GetComponent<RectTransform>();
                gemBtnRect.sizeDelta = new Vector2(88f, 60f);
                gemBtnRect.anchoredPosition = new Vector2(124f, 0f);

                Image btnImg = gemBtnTr.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.sprite = btnBlueSpr;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = Color.white;
                }

                // Icon Kim Cương
                Transform iconGemTr = gemBtnTr.Find("Img_Gem") ?? gemBtnTr.Find("Icon_Diamond");
                if (iconGemTr != null)
                {
                    RectTransform iconRect = iconGemTr.GetComponent<RectTransform>();
                    iconRect.sizeDelta = new Vector2(32f, 32f);
                    iconRect.anchoredPosition = new Vector2(-16f, 0f);

                    Image iconImg = iconGemTr.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = diamondIconSpr;
                        iconImg.preserveAspect = true;
                        iconImg.color = Color.white;
                    }
                }

                // Text Giá Kim Cương
                Transform costTr = gemBtnTr.Find("Txt_Cost") ?? gemBtnTr.Find("Txt_GemCost");
                if (costTr != null)
                {
                    RectTransform costRect = costTr.GetComponent<RectTransform>();
                    costRect.sizeDelta = new Vector2(36f, 30f);
                    costRect.anchoredPosition = new Vector2(18f, 0f);

                    TMP_Text tmpCost = costTr.GetComponent<TMP_Text>();
                    if (tmpCost != null)
                    {
                        tmpCost.color = Color.white;
                        tmpCost.fontSize = 22f;
                        tmpCost.fontStyle = FontStyles.Bold;
                        tmpCost.alignment = TextAlignmentOptions.Center;
                    }

                    Outline costOutline = costTr.GetComponent<Outline>();
                    if (costOutline == null) costOutline = costTr.gameObject.AddComponent<Outline>();
                    costOutline.effectColor = new Color(0.11f, 0.36f, 0.53f, 1f);
                    costOutline.effectDistance = new Vector2(1f, -1f);
                }

                Button btnSpeedUp = gemBtnTr.GetComponent<Button>();
                if (btnSpeedUp != null)
                {
                    btnSpeedUp.onClick.RemoveAllListeners();
                    btnSpeedUp.onClick.AddListener(() => penUI.TrySpeedUpGem());
                }
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return rt;
    }

    private static TMP_Text CreateText(RectTransform parent, string text, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions alignment, bool bold)
    {
        TextMeshProUGUI tmp = parent.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        if (font != null) tmp.font = font;
        tmp.alignment = alignment;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Sprite LoadSprite(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName).Replace('\\', '/');
        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr == null)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        return spr;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f != null)
        {
            f.SetValue(target, value);
        }
    }
}
#endif
