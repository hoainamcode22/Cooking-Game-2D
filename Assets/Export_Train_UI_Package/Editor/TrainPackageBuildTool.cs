#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ExportTrainUIPackage
{
    public static class TrainPackageBuildTool
    {
        private const string PackageDir = "Assets/Export_Train_UI_Package";
        private const string SpritesDir = PackageDir + "/Sprites";
        private const string PrefabsDir = PackageDir + "/Prefabs";
        private const string ShopSvgDir = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";

        [MenuItem("Tools/Farm/DUNG PREFAB POPUP TAU HOA 6 STATE (EXPORT PACKAGE)", false, 1)]
        public static void BuildAllPopupsAndPrefabs()
        {
            Debug.Log("[TrainPackageBuildTool] BẮT ĐẦU DỰNG TOÀN BỘ POPUP TÀU HỎA 6 STATE...");

            if (!Directory.Exists(PrefabsDir))
            {
                Directory.CreateDirectory(PrefabsDir);
            }

            Canvas canvasPopup = FindPopupCanvas();
            if (canvasPopup == null)
            {
                Debug.LogError("[TrainPackageBuildTool] Không tìm thấy Canvas trong Scene!");
                return;
            }

            // 1. Dựng Popup 1 & 6: Ga Tàu Toàn Cảnh (Master Station View)
            GameObject masterGo = BuildMasterStationPopup(canvasPopup.transform);

            // 2. Dựng Popup 2 & 3: Nạp Hàng Chi Tiết (TrainLoadPopupUI)
            GameObject loadGo = BuildLoadPopup(canvasPopup.transform);

            // 3. Dựng Popup 4 & 5: Đang Vận Chuyển & Đếm Ngược (TrainProcessPopupUI)
            GameObject processGo = BuildProcessPopup(canvasPopup.transform);

            // 4. Lưu thành Prefab
            SaveAsPrefab(masterGo, $"{PrefabsDir}/Popup_Train_MasterStation.prefab");
            SaveAsPrefab(loadGo, $"{PrefabsDir}/Popup_item_Train.prefab");
            SaveAsPrefab(processGo, $"{PrefabsDir}/Popup_train.prefab");

            // 5. Cấu hình Collider và tương tác cho gataulua ngoài map
            SetupStationBuildingClick();

            // 6. Lưu Scene
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TrainPackageBuildTool] HOÀN TẤT 100% DỰNG POPUP VÀ XUẤT PREFAB THÀNH CÔNG!");
        }

        private static Canvas FindPopupCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name.Contains("Popup") || c.name.Contains("UI"))
                    return c;
            }
            return canvases.Length > 0 ? canvases[0] : null;
        }

        // =========================================================================
        // 1. POPUP 1 & 6: MASTER STATION POPUP (Full Screen Board 1400x820)
        // =========================================================================
        private static GameObject BuildMasterStationPopup(Transform canvasTr)
        {
            Sprite sceneBg    = TrainSpriteLoader.GetSprite($"{SpritesDir}/station_full_scene_bg.png");
            Sprite ribbon     = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_banner_ribbon.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
            Sprite bubbleBg   = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
            Sprite iconDisc   = TrainSpriteLoader.GetSprite($"{SpritesDir}/icon_disc_large.png");
            Sprite checkBadge = TrainSpriteLoader.GetSprite($"{SpritesDir}/check_badge_green.png");
            Sprite flatWagon  = TrainSpriteLoader.GetSprite($"{SpritesDir}/flat_wagon_horizontal.png");
            Sprite flatLoco   = TrainSpriteLoader.GetSprite($"{SpritesDir}/flat_locomotive_horizontal.png");
            Sprite stationBldg = TrainSpriteLoader.GetSprite($"{SpritesDir}/station_building_flat.png");
            Sprite smokePuff  = TrainSpriteLoader.GetSprite($"{SpritesDir}/train_smoke_puff.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/steam_smoke_cloud.png");
            Sprite woodPanel  = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_panel.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
            Sprite closeSp    = TrainSpriteLoader.GetSprite("Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png")
                              ?? TrainSpriteLoader.GetSprite("Assets/Assetsgame/btnX.png");

            GameObject rootGo = GetOrCreateChild(canvasTr, "Popup_Train_MasterStation");
            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = new Vector2(1400f, 820f);

            // Canvas override sorting order 160 để đè lên toàn bộ HUD
            var canvas = GetOrAddComponent<Canvas>(rootGo);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 160;
            if (rootGo.GetComponent<GraphicRaycaster>() == null)
                rootGo.AddComponent<GraphicRaycaster>();

            var masterUI = GetOrAddComponent<TrainStationMasterPopupUI>(rootGo);

            // Full-screen Dim Overlay (3840x2160)
            GameObject dimGo = GetOrCreateChild(rootGo.transform, "Panel_Dim");
            RectTransform dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = new Vector2(0.5f, 0.5f);
            dimRt.anchorMax = new Vector2(0.5f, 0.5f);
            dimRt.anchoredPosition = Vector2.zero;
            dimRt.sizeDelta = new Vector2(3840f, 2160f);
            var dimImg = GetOrAddComponent<Image>(dimGo);
            dimImg.color = new Color(0.04f, 0.08f, 0.03f, 0.75f);
            dimImg.raycastTarget = true;

            // Main Wood Frame (1400x820)
            GameObject frameGo = GetOrCreateChild(rootGo.transform, "Main_Frame");
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = Vector2.zero;
            frameRt.offsetMax = Vector2.zero;

            var frameImg = GetOrAddComponent<Image>(frameGo);
            frameImg.sprite = woodPanel;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;

            // 4 Brass Corner Studs
            Vector2[] studPos = { new Vector2(-660f, 370f), new Vector2(660f, 370f), new Vector2(-660f, -370f), new Vector2(660f, -370f) };
            for (int i = 0; i < studPos.Length; i++)
            {
                GameObject sGo = GetOrCreateChild(frameGo.transform, $"Stud_{i}");
                RectTransform sRt = sGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.5f, 0.5f);
                sRt.anchorMax = new Vector2(0.5f, 0.5f);
                sRt.anchoredPosition = studPos[i];
                sRt.sizeDelta = new Vector2(28f, 28f);
                var sImg = GetOrAddComponent<Image>(sGo);
                sImg.sprite = iconDisc;
                sImg.preserveAspect = true;
            }

            // Inner Scene Container (Lọt lòng trong khung gỗ)
            GameObject innerGo = GetOrCreateChild(frameGo.transform, "Inner_Scene");
            RectTransform innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(22f, 22f);
            innerRt.offsetMax = new Vector2(-22f, -36f);

            // Background Scene
            GameObject bgGo = GetOrCreateChild(innerGo.transform, "Img_Background");
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = GetOrAddComponent<Image>(bgGo);
            bgImg.sprite = sceneBg;
            bgImg.color = Color.white;

            // Station Building (GA HÀNG)
            GameObject stnGo = GetOrCreateChild(innerGo.transform, "Building_GaHang");
            RectTransform stnRt = stnGo.GetComponent<RectTransform>();
            stnRt.anchorMin = new Vector2(1f, 0.5f);
            stnRt.anchorMax = new Vector2(1f, 0.5f);
            stnRt.anchoredPosition = new Vector2(-150f, 40f);
            stnRt.sizeDelta = new Vector2(300f, 300f);
            var stnImg = GetOrAddComponent<Image>(stnGo);
            stnImg.sprite = stationBldg;
            stnImg.color = Color.white;
            stnImg.preserveAspect = true;

            // Ribbon Banner Gold (Header_Banner 620x120)
            GameObject ribGo = GetOrCreateChild(frameGo.transform, "Header_Banner");
            RectTransform ribRt = ribGo.GetComponent<RectTransform>();
            ribRt.anchorMin = new Vector2(0.5f, 1f);
            ribRt.anchorMax = new Vector2(0.5f, 1f);
            ribRt.anchoredPosition = new Vector2(0f, 16f);
            ribRt.sizeDelta = new Vector2(620f, 126f);
            var ribImg = GetOrAddComponent<Image>(ribGo);
            ribImg.sprite = ribbon;
            ribImg.type = Image.Type.Sliced;
            ribImg.color = Color.white;

            GameObject txtRibGo = GetOrCreateChild(ribGo.transform, "Txt_Title");
            RectTransform txtRibRt = txtRibGo.GetComponent<RectTransform>();
            txtRibRt.anchorMin = Vector2.zero;
            txtRibRt.anchorMax = Vector2.one;
            txtRibRt.offsetMin = Vector2.zero;
            txtRibRt.offsetMax = Vector2.zero;
            var txtRib = GetOrAddComponent<TextMeshProUGUI>(txtRibGo);
            txtRib.text = "TÀU LỬA";
            txtRib.alignment = TextAlignmentOptions.Center;
            txtRib.fontSize = 42;
            txtRib.fontStyle = FontStyles.Bold;
            txtRib.color = new Color(0.36f, 0.20f, 0.09f);

            // Close Button X
            GameObject closeGo = GetOrCreateChild(frameGo.transform, "Btn_Close");
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-10f, 10f);
            closeRt.sizeDelta = new Vector2(86f, 86f);
            var closeImg = GetOrAddComponent<Image>(closeGo);
            if (closeSp != null) closeImg.sprite = closeSp;
            closeImg.preserveAspect = true;
            GetOrAddComponent<Button>(closeGo);

            // Bottom Hint Pill
            GameObject hintGo = GetOrCreateChild(innerGo.transform, "Hint_Pill");
            RectTransform hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 30f);
            hintRt.sizeDelta = new Vector2(800f, 52f);
            var hintImg = GetOrAddComponent<Image>(hintGo);
            hintImg.sprite = bubbleBg;
            hintImg.type = Image.Type.Sliced;
            hintImg.color = Color.white;

            GameObject txtHintGo = GetOrCreateChild(hintGo.transform, "Txt_Hint");
            RectTransform thRt = txtHintGo.GetComponent<RectTransform>();
            thRt.anchorMin = Vector2.zero;
            thRt.anchorMax = Vector2.one;
            thRt.offsetMin = Vector2.zero;
            thRt.offsetMax = Vector2.zero;
            var txtHint = GetOrAddComponent<TextMeshProUGUI>(txtHintGo);
            txtHint.text = "Nạp đủ hàng cho các toa để tàu khởi hành vận chuyển!";
            txtHint.alignment = TextAlignmentOptions.Center;
            txtHint.fontSize = 22;
            txtHint.fontStyle = FontStyles.Bold;
            txtHint.color = new Color(0.36f, 0.20f, 0.09f);

            // Train Container (4 Wagons + 1 Locomotive)
            GameObject trainCont = GetOrCreateChild(innerGo.transform, "Train_Container");
            RectTransform tcRt = trainCont.GetComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0.5f, 0.5f);
            tcRt.anchorMax = new Vector2(0.5f, 0.5f);
            tcRt.anchoredPosition = new Vector2(-60f, -40f);
            tcRt.sizeDelta = new Vector2(1050f, 260f);

            // 4 Wagons
            for (int i = 0; i < 4; i++)
            {
                GameObject wGo = GetOrCreateChild(trainCont.transform, $"Wagon_{i + 1}");
                RectTransform wRt = wGo.GetComponent<RectTransform>();
                wRt.anchorMin = new Vector2(0f, 0.5f);
                wRt.anchorMax = new Vector2(0f, 0.5f);
                wRt.anchoredPosition = new Vector2(i * 190f + 95f, 0f);
                wRt.sizeDelta = new Vector2(185f, 220f);

                var slotUI = GetOrAddComponent<StationWagonSlotUI>(wGo);
                GetOrAddComponent<Button>(wGo);

                // Wagon Image
                GameObject wImgGo = GetOrCreateChild(wGo.transform, "Img_Wagon");
                RectTransform wiRt = wImgGo.GetComponent<RectTransform>();
                wiRt.anchorMin = new Vector2(0.5f, 0f);
                wiRt.anchorMax = new Vector2(0.5f, 0f);
                wiRt.anchoredPosition = new Vector2(0f, 60f);
                wiRt.sizeDelta = new Vector2(180f, 130f);
                var wiImg = GetOrAddComponent<Image>(wImgGo);
                wiImg.sprite = flatWagon;
                wiImg.color = Color.white;
                wiImg.preserveAspect = true;

                // Bubble Req
                GameObject bGo = GetOrCreateChild(wGo.transform, "Bubble_Req");
                RectTransform bRt = bGo.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 1f);
                bRt.anchorMax = new Vector2(0.5f, 1f);
                bRt.anchoredPosition = new Vector2(0f, -18f);
                bRt.sizeDelta = new Vector2(140f, 70f);
                var bImg = GetOrAddComponent<Image>(bGo);
                bImg.sprite = bubbleBg;
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;

                // Icon Disc + Icon
                GameObject discGo = GetOrCreateChild(bGo.transform, "Icon_Disc");
                RectTransform dRt = discGo.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0f, 0.5f);
                dRt.anchorMax = new Vector2(0f, 0.5f);
                dRt.anchoredPosition = new Vector2(30f, 4f);
                dRt.sizeDelta = new Vector2(46f, 46f);
                var dImg = GetOrAddComponent<Image>(discGo);
                dImg.sprite = iconDisc;
                dImg.color = Color.white;
                dImg.preserveAspect = true;

                GameObject iconGo = GetOrCreateChild(discGo.transform, "Img_Icon");
                RectTransform icRt = iconGo.GetComponent<RectTransform>();
                icRt.anchorMin = Vector2.zero;
                icRt.anchorMax = Vector2.one;
                icRt.offsetMin = Vector2.zero;
                icRt.offsetMax = Vector2.zero;
                var icImg = GetOrAddComponent<Image>(iconGo);
                icImg.preserveAspect = true;

                // Amount Text
                GameObject txtAmGo = GetOrCreateChild(bGo.transform, "Txt_Amount");
                RectTransform taRt = txtAmGo.GetComponent<RectTransform>();
                taRt.anchorMin = new Vector2(0.45f, 0f);
                taRt.anchorMax = new Vector2(1f, 1f);
                taRt.offsetMin = Vector2.zero;
                taRt.offsetMax = new Vector2(-4f, 0f);
                var txtAm = GetOrAddComponent<TextMeshProUGUI>(txtAmGo);
                txtAm.text = "3/6";
                txtAm.alignment = TextAlignmentOptions.Center;
                txtAm.fontSize = 24;
                txtAm.fontStyle = FontStyles.Bold;
                txtAm.color = new Color(0.36f, 0.20f, 0.09f);

                // Check Badge
                GameObject chkGo = GetOrCreateChild(wGo.transform, "Check_Badge");
                RectTransform chkRt = chkGo.GetComponent<RectTransform>();
                chkRt.anchorMin = new Vector2(1f, 0f);
                chkRt.anchorMax = new Vector2(1f, 0f);
                chkRt.anchoredPosition = new Vector2(-15f, 60f);
                chkRt.sizeDelta = new Vector2(44f, 44f);
                var chkImg = GetOrAddComponent<Image>(chkGo);
                chkImg.sprite = checkBadge;
                chkImg.color = Color.white;
                chkImg.preserveAspect = true;
                chkGo.SetActive(false);

                // Empty Pill
                GameObject empGo = GetOrCreateChild(wGo.transform, "Empty_Pill");
                RectTransform empRt = empGo.GetComponent<RectTransform>();
                empRt.anchorMin = new Vector2(0.5f, 1f);
                empRt.anchorMax = new Vector2(0.5f, 1f);
                empRt.anchoredPosition = new Vector2(0f, -20f);
                empRt.sizeDelta = new Vector2(130f, 44f);
                var empImg = GetOrAddComponent<Image>(empGo);
                empImg.sprite = bubbleBg;
                empImg.type = Image.Type.Sliced;
                empImg.color = Color.white;

                GameObject txtEmpGo = GetOrCreateChild(empGo.transform, "Txt_Empty");
                RectTransform teRt = txtEmpGo.GetComponent<RectTransform>();
                teRt.anchorMin = Vector2.zero;
                teRt.anchorMax = Vector2.one;
                teRt.offsetMin = Vector2.zero;
                teRt.offsetMax = Vector2.zero;
                var txtEmp = GetOrAddComponent<TextMeshProUGUI>(txtEmpGo);
                txtEmp.text = "Toa trống";
                txtEmp.alignment = TextAlignmentOptions.Center;
                txtEmp.fontSize = 20;
                txtEmp.fontStyle = FontStyles.Bold;
                txtEmp.color = new Color(0.55f, 0.40f, 0.25f);
                empGo.SetActive(false);

                slotUI.AutoBindComponents();
            }

            // Locomotive Horizontal
            GameObject locoGo = GetOrCreateChild(trainCont.transform, "Locomotive_Flat");
            RectTransform lRt = locoGo.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = new Vector2(4 * 190f + 115f, 0f);
            lRt.sizeDelta = new Vector2(230f, 230f);
            var lImg = GetOrAddComponent<Image>(locoGo);
            lImg.sprite = flatLoco;
            lImg.color = Color.white;
            lImg.preserveAspect = true;

            // Smoke Puff Root
            GameObject smokeRoot = GetOrCreateChild(locoGo.transform, "Smoke_Puff_Root");
            RectTransform smkRt = smokeRoot.GetComponent<RectTransform>();
            smkRt.anchorMin = new Vector2(0.68f, 0.88f);
            smkRt.anchorMax = new Vector2(0.68f, 0.88f);
            smkRt.anchoredPosition = Vector2.zero;
            smkRt.sizeDelta = new Vector2(50f, 50f);

            for (int i = 0; i < 3; i++)
            {
                GameObject puff = GetOrCreateChild(smokeRoot.transform, $"Puff_{i + 1}");
                RectTransform pRt = puff.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(0.5f, 0.5f);
                pRt.anchorMax = new Vector2(0.5f, 0.5f);
                pRt.anchoredPosition = Vector2.zero;
                pRt.sizeDelta = new Vector2(60f, 60f);
                var pImg = GetOrAddComponent<Image>(puff);
                pImg.sprite = smokePuff;
                pImg.color = Color.white;
                pImg.preserveAspect = true;
                puff.SetActive(false);
            }

            masterUI.ApplyThemeSprites();
            masterUI.AutoBindIfNull();
            rootGo.SetActive(false);
            return rootGo;
        }

        // =========================================================================
        // 2. POPUP 2 & 3: LOAD POPUP
        // =========================================================================
        private static GameObject BuildLoadPopup(Transform canvasTr)
        {
            Sprite woodFrame  = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_panel.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
            Sprite paperPanel = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_inner.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_panel_paper.png");
            Sprite ribbon     = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_banner_ribbon.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
            Sprite iconDisc   = TrainSpriteLoader.GetSprite($"{SpritesDir}/icon_disc_large.png");
            Sprite trackBar   = TrainSpriteLoader.GetSprite($"{SpritesDir}/progress_track_bar.png");
            Sprite fillGreen  = TrainSpriteLoader.GetSprite($"{SpritesDir}/progress_fill_green.png");
            Sprite btnGreen   = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_green_3d.png");
            Sprite btnYellow  = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_yellow_3d.png");
            Sprite btnGray    = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_disabled_3d.png");
            Sprite bubbleBg   = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
            Sprite closeSp    = TrainSpriteLoader.GetSprite("Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png")
                              ?? TrainSpriteLoader.GetSprite("Assets/Assetsgame/btnX.png");

            GameObject root = GetOrCreateChild(canvasTr, "Popup_item_Train");
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(552f, 440f);

            var canvas = GetOrAddComponent<Canvas>(root);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 165;
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();

            var bgImg = GetOrAddComponent<Image>(root);
            bgImg.sprite = woodFrame;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Paper Panel
            GameObject paperGo = GetOrCreateChild(root.transform, "Paper_Panel");
            RectTransform paperRt = paperGo.GetComponent<RectTransform>();
            paperRt.anchorMin = Vector2.zero;
            paperRt.anchorMax = Vector2.one;
            paperRt.offsetMin = new Vector2(16f, 16f);
            paperRt.offsetMax = new Vector2(-16f, -36f);
            var paperImg = GetOrAddComponent<Image>(paperGo);
            paperImg.sprite = paperPanel;
            paperImg.type = Image.Type.Sliced;
            paperImg.color = Color.white;

            // Ribbon Banner
            GameObject ribGo = GetOrCreateChild(root.transform, "Ribbon_Banner");
            RectTransform ribRt = ribGo.GetComponent<RectTransform>();
            ribRt.anchorMin = new Vector2(0.5f, 1f);
            ribRt.anchorMax = new Vector2(0.5f, 1f);
            ribRt.anchoredPosition = new Vector2(0f, 14f);
            ribRt.sizeDelta = new Vector2(360f, 76f);
            var ribImg = GetOrAddComponent<Image>(ribGo);
            ribImg.sprite = ribbon;
            ribImg.type = Image.Type.Sliced;
            ribImg.color = Color.white;

            GameObject txtRibGo = GetOrCreateChild(ribGo.transform, "Txt_Title");
            RectTransform txtRibRt = txtRibGo.GetComponent<RectTransform>();
            txtRibRt.anchorMin = Vector2.zero;
            txtRibRt.anchorMax = Vector2.one;
            txtRibRt.offsetMin = Vector2.zero;
            txtRibRt.offsetMax = Vector2.zero;
            var txtRib = GetOrAddComponent<TextMeshProUGUI>(txtRibGo);
            txtRib.text = "NẠP HÀNG";
            txtRib.alignment = TextAlignmentOptions.Center;
            txtRib.fontSize = 28;
            txtRib.fontStyle = FontStyles.Bold;
            txtRib.color = new Color(0.36f, 0.20f, 0.09f);

            // Wagon Tag
            GameObject tagGo = GetOrCreateChild(paperGo.transform, "Txt_WagonTag");
            RectTransform tagRt = tagGo.GetComponent<RectTransform>();
            tagRt.anchorMin = new Vector2(0.5f, 1f);
            tagRt.anchorMax = new Vector2(0.5f, 1f);
            tagRt.anchoredPosition = new Vector2(0f, -48f);
            tagRt.sizeDelta = new Vector2(380f, 28f);
            var txtTag = GetOrAddComponent<TextMeshProUGUI>(tagGo);
            txtTag.text = "Toa số 2 / 4";
            txtTag.alignment = TextAlignmentOptions.Center;
            txtTag.fontSize = 18;
            txtTag.fontStyle = FontStyles.Bold;
            txtTag.color = new Color(0.64f, 0.50f, 0.25f);

            // Icon Disc
            GameObject discGo = GetOrCreateChild(paperGo.transform, "Icon_Disc");
            RectTransform discRt = discGo.GetComponent<RectTransform>();
            discRt.anchorMin = new Vector2(0.5f, 0.5f);
            discRt.anchorMax = new Vector2(0.5f, 0.5f);
            discRt.anchoredPosition = new Vector2(-120f, 15f);
            discRt.sizeDelta = new Vector2(110f, 110f);
            var discImg = GetOrAddComponent<Image>(discGo);
            discImg.sprite = iconDisc;
            discImg.color = Color.white;
            discImg.preserveAspect = true;

            GameObject iconGo = GetOrCreateChild(discGo.transform, "Img_Icon");
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(76f, 76f);
            var imgIcon = GetOrAddComponent<Image>(iconGo);
            imgIcon.preserveAspect = true;

            // Info Right
            GameObject txtNameGo = GetOrCreateChild(paperGo.transform, "Txt_ItemName");
            RectTransform nameRt = txtNameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 0.5f);
            nameRt.anchorMax = new Vector2(0.5f, 0.5f);
            nameRt.anchoredPosition = new Vector2(60f, 50f);
            nameRt.sizeDelta = new Vector2(220f, 32f);
            var txtName = GetOrAddComponent<TextMeshProUGUI>(txtNameGo);
            txtName.text = "Cà chua";
            txtName.alignment = TextAlignmentOptions.Left;
            txtName.fontSize = 24;
            txtName.fontStyle = FontStyles.Bold;
            txtName.color = new Color(0.36f, 0.20f, 0.09f);

            // Progress Bar
            GameObject trackBarGo = GetOrCreateChild(paperGo.transform, "Progress_Track");
            RectTransform tbRt = trackBarGo.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 0.5f);
            tbRt.anchorMax = new Vector2(0.5f, 0.5f);
            tbRt.anchoredPosition = new Vector2(60f, 15f);
            tbRt.sizeDelta = new Vector2(220f, 32f);
            var tbImg = GetOrAddComponent<Image>(trackBarGo);
            tbImg.sprite = trackBar;
            tbImg.type = Image.Type.Sliced;
            tbImg.color = Color.white;

            GameObject fillGo = GetOrCreateChild(trackBarGo.transform, "Progress_Fill");
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            var fillImg = GetOrAddComponent<Image>(fillGo);
            fillImg.sprite = fillGreen;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.5f;
            fillImg.color = Color.white;

            GameObject txtSoluongGo = GetOrCreateChild(trackBarGo.transform, "Txt_Soluong");
            RectTransform slRt = txtSoluongGo.GetComponent<RectTransform>();
            slRt.anchorMin = Vector2.zero;
            slRt.anchorMax = Vector2.one;
            slRt.offsetMin = Vector2.zero;
            slRt.offsetMax = Vector2.zero;
            var txtSoluong = GetOrAddComponent<TextMeshProUGUI>(txtSoluongGo);
            txtSoluong.text = "3 / 6";
            txtSoluong.alignment = TextAlignmentOptions.Center;
            txtSoluong.fontSize = 18;
            txtSoluong.fontStyle = FontStyles.Bold;
            txtSoluong.color = Color.white;

            // Stock Text
            GameObject txtStockGo = GetOrCreateChild(paperGo.transform, "Txt_Stock");
            RectTransform stockRt = txtStockGo.GetComponent<RectTransform>();
            stockRt.anchorMin = new Vector2(0.5f, 0.5f);
            stockRt.anchorMax = new Vector2(0.5f, 0.5f);
            stockRt.anchoredPosition = new Vector2(60f, -20f);
            stockRt.sizeDelta = new Vector2(220f, 26f);
            var txtStock = GetOrAddComponent<TextMeshProUGUI>(txtStockGo);
            txtStock.text = "Trong kho: x9";
            txtStock.alignment = TextAlignmentOptions.Left;
            txtStock.fontSize = 16;
            txtStock.fontStyle = FontStyles.Bold;
            txtStock.color = new Color(0.54f, 0.39f, 0.22f);

            // Buttons
            GameObject btnThemGo = GetOrCreateChild(paperGo.transform, "Btn_themhang");
            RectTransform btRt = btnThemGo.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0.5f, 0f);
            btRt.anchorMax = new Vector2(0.5f, 0f);
            btRt.anchoredPosition = new Vector2(-80f, 40f);
            btRt.sizeDelta = new Vector2(190f, 56f);
            var btImg = GetOrAddComponent<Image>(btnThemGo);
            btImg.sprite = btnGreen;
            btImg.type = Image.Type.Sliced;
            btImg.color = Color.white;
            GetOrAddComponent<Button>(btnThemGo);

            GameObject txtBtGo = GetOrCreateChild(btnThemGo.transform, "Txt_Them");
            RectTransform tbtRt = txtBtGo.GetComponent<RectTransform>();
            tbtRt.anchorMin = Vector2.zero;
            tbtRt.anchorMax = Vector2.one;
            tbtRt.offsetMin = Vector2.zero;
            tbtRt.offsetMax = Vector2.zero;
            var txtBt = GetOrAddComponent<TextMeshProUGUI>(txtBtGo);
            txtBt.text = "THÊM HÀNG";
            txtBt.alignment = TextAlignmentOptions.Center;
            txtBt.fontSize = 20;
            txtBt.fontStyle = FontStyles.Bold;
            txtBt.color = Color.white;

            GameObject btnAllGo = GetOrCreateChild(paperGo.transform, "Btn_napTatCa");
            RectTransform baRt = btnAllGo.GetComponent<RectTransform>();
            baRt.anchorMin = new Vector2(0.5f, 0f);
            baRt.anchorMax = new Vector2(0.5f, 0f);
            baRt.anchoredPosition = new Vector2(120f, 40f);
            baRt.sizeDelta = new Vector2(170f, 56f);
            var baImg = GetOrAddComponent<Image>(btnAllGo);
            baImg.sprite = btnYellow;
            baImg.type = Image.Type.Sliced;
            baImg.color = Color.white;
            GetOrAddComponent<Button>(btnAllGo);

            GameObject txtAllGo = GetOrCreateChild(btnAllGo.transform, "Txt_All");
            RectTransform tallRt = txtAllGo.GetComponent<RectTransform>();
            tallRt.anchorMin = Vector2.zero;
            tallRt.anchorMax = Vector2.one;
            tallRt.offsetMin = Vector2.zero;
            tallRt.offsetMax = Vector2.zero;
            var txtAll = GetOrAddComponent<TextMeshProUGUI>(txtAllGo);
            txtAll.text = "NẠP TẤT CẢ";
            txtAll.alignment = TextAlignmentOptions.Center;
            txtAll.fontSize = 19;
            txtAll.fontStyle = FontStyles.Bold;
            txtAll.color = new Color(0.48f, 0.29f, 0.06f);

            // Button Đã đủ hàng (Disabled Gray)
            GameObject btnDaDuGo = GetOrCreateChild(paperGo.transform, "Btn_DaDuHang");
            RectTransform bddRt = btnDaDuGo.GetComponent<RectTransform>();
            bddRt.anchorMin = new Vector2(0.5f, 0f);
            bddRt.anchorMax = new Vector2(0.5f, 0f);
            bddRt.anchoredPosition = new Vector2(0f, 40f);
            bddRt.sizeDelta = new Vector2(360f, 56f);
            var bddImg = GetOrAddComponent<Image>(btnDaDuGo);
            bddImg.sprite = btnGray;
            bddImg.type = Image.Type.Sliced;
            bddImg.color = Color.white;

            GameObject txtDddGo = GetOrCreateChild(btnDaDuGo.transform, "Txt_Label");
            RectTransform tdddRt = txtDddGo.GetComponent<RectTransform>();
            tdddRt.anchorMin = Vector2.zero;
            tdddRt.anchorMax = Vector2.one;
            tdddRt.offsetMin = Vector2.zero;
            tdddRt.offsetMax = Vector2.zero;
            var txtDdd = GetOrAddComponent<TextMeshProUGUI>(txtDddGo);
            txtDdd.text = "ĐÃ ĐỦ HÀNG";
            txtDdd.alignment = TextAlignmentOptions.Center;
            txtDdd.fontSize = 20;
            txtDdd.fontStyle = FontStyles.Bold;
            txtDdd.color = Color.white;
            btnDaDuGo.SetActive(false);

            // Note Box
            GameObject noteGo = GetOrCreateChild(paperGo.transform, "Note_Box");
            RectTransform noteRt = noteGo.GetComponent<RectTransform>();
            noteRt.anchorMin = new Vector2(0.5f, 0f);
            noteRt.anchorMax = new Vector2(0.5f, 0f);
            noteRt.anchoredPosition = new Vector2(0f, -25f);
            noteRt.sizeDelta = new Vector2(360f, 48f);
            var noteBgImg = GetOrAddComponent<Image>(noteGo);
            noteBgImg.sprite = bubbleBg;
            noteBgImg.type = Image.Type.Sliced;
            noteBgImg.color = Color.white;

            GameObject noteIconGo = GetOrCreateChild(noteGo.transform, "Img_Icon");
            RectTransform niRt = noteIconGo.GetComponent<RectTransform>();
            niRt.anchorMin = new Vector2(0f, 0.5f);
            niRt.anchorMax = new Vector2(0f, 0.5f);
            niRt.anchoredPosition = new Vector2(24f, 0f);
            niRt.sizeDelta = new Vector2(32f, 32f);
            var niImg = GetOrAddComponent<Image>(noteIconGo);
            niImg.preserveAspect = true;

            GameObject txtNoteGo = GetOrCreateChild(noteGo.transform, "Txt_Note");
            RectTransform tnRt = txtNoteGo.GetComponent<RectTransform>();
            tnRt.anchorMin = new Vector2(0.18f, 0f);
            tnRt.anchorMax = new Vector2(1f, 1f);
            tnRt.offsetMin = Vector2.zero;
            tnRt.offsetMax = Vector2.zero;
            var txtNote = GetOrAddComponent<TextMeshProUGUI>(txtNoteGo);
            txtNote.text = "Còn 2 toa chưa đủ — nạp xong 3 toa yêu cầu, tàu sẽ khởi hành.";
            txtNote.alignment = TextAlignmentOptions.Left;
            txtNote.fontSize = 14;
            txtNote.fontStyle = FontStyles.Bold;
            txtNote.color = new Color(0.48f, 0.29f, 0.06f);
            noteGo.SetActive(false);

            // Close Button X
            GameObject closeGo = GetOrCreateChild(root.transform, "Btn_close");
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(12f, 12f);
            closeRt.sizeDelta = new Vector2(56f, 56f);
            var closeImg = GetOrAddComponent<Image>(closeGo);
            if (closeSp != null) closeImg.sprite = closeSp;
            closeImg.preserveAspect = true;
            GetOrAddComponent<Button>(closeGo);

            var loadUI = GetOrAddComponent<TrainLoadPopupUI>(root);
            loadUI.ApplyThemeSprites();
            loadUI.AutoBindComponents();
            root.SetActive(false);
            return root;
        }

        // =========================================================================
        // 3. POPUP 4 & 5: PROCESS POPUP
        // =========================================================================
        private static GameObject BuildProcessPopup(Transform canvasTr)
        {
            Sprite woodFrame  = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_panel.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
            Sprite paperPanel = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_inner.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_panel_paper.png");
            Sprite ribbon     = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_banner_ribbon.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
            Sprite trackBg    = TrainSpriteLoader.GetSprite($"{SpritesDir}/mini_train_track_bg.png");
            Sprite miniTrain  = TrainSpriteLoader.GetSprite($"{SpritesDir}/train_popup_mini_horizontal.png");
            Sprite timerBox   = TrainSpriteLoader.GetSprite($"{SpritesDir}/timer_box_dark.png");
            Sprite btnBlue    = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_blue_gem_3d.png");
            Sprite btnGreen   = TrainSpriteLoader.GetSprite($"{SpritesDir}/btn_green_3d.png");
            Sprite bubbleBg   = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
            Sprite closeSp    = TrainSpriteLoader.GetSprite("Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png")
                              ?? TrainSpriteLoader.GetSprite("Assets/Assetsgame/btnX.png");

            GameObject root = GetOrCreateChild(canvasTr, "Popup_train");
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(512f, 500f);

            var canvas = GetOrAddComponent<Canvas>(root);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 165;
            if (root.GetComponent<GraphicRaycaster>() == null)
                root.AddComponent<GraphicRaycaster>();

            var bgImg = GetOrAddComponent<Image>(root);
            bgImg.sprite = woodFrame;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            // Paper Panel
            GameObject paperGo = GetOrCreateChild(root.transform, "Paper_Panel");
            RectTransform paperRt = paperGo.GetComponent<RectTransform>();
            paperRt.anchorMin = Vector2.zero;
            paperRt.anchorMax = Vector2.one;
            paperRt.offsetMin = new Vector2(16f, 16f);
            paperRt.offsetMax = new Vector2(-16f, -36f);
            var paperImg = GetOrAddComponent<Image>(paperGo);
            paperImg.sprite = paperPanel;
            paperImg.type = Image.Type.Sliced;
            paperImg.color = Color.white;

            // Ribbon Banner
            GameObject ribGo = GetOrCreateChild(root.transform, "Ribbon_Banner");
            RectTransform ribRt = ribGo.GetComponent<RectTransform>();
            ribRt.anchorMin = new Vector2(0.5f, 1f);
            ribRt.anchorMax = new Vector2(0.5f, 1f);
            ribRt.anchoredPosition = new Vector2(0f, 14f);
            ribRt.sizeDelta = new Vector2(380f, 76f);
            var ribImg = GetOrAddComponent<Image>(ribGo);
            ribImg.sprite = ribbon;
            ribImg.type = Image.Type.Sliced;
            ribImg.color = Color.white;

            GameObject txtRibGo = GetOrCreateChild(ribGo.transform, "Txt_Title");
            RectTransform txtRibRt = txtRibGo.GetComponent<RectTransform>();
            txtRibRt.anchorMin = Vector2.zero;
            txtRibRt.anchorMax = Vector2.one;
            txtRibRt.offsetMin = Vector2.zero;
            txtRibRt.offsetMax = Vector2.zero;
            var txtRib = GetOrAddComponent<TextMeshProUGUI>(txtRibGo);
            txtRib.text = "ĐANG VẬN CHUYỂN";
            txtRib.alignment = TextAlignmentOptions.Center;
            txtRib.fontSize = 28;
            txtRib.fontStyle = FontStyles.Bold;
            txtRib.color = new Color(0.36f, 0.20f, 0.09f);

            // Mini Track Box
            GameObject trackBox = GetOrCreateChild(paperGo.transform, "Mini_Track_Box");
            RectTransform trackRt = trackBox.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0.5f, 1f);
            trackRt.anchorMax = new Vector2(0.5f, 1f);
            trackRt.anchoredPosition = new Vector2(0f, -80f);
            trackRt.sizeDelta = new Vector2(440f, 110f);
            var trackImg = GetOrAddComponent<Image>(trackBox);
            trackImg.sprite = trackBg;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = Color.white;

            // Mini Train
            GameObject miniTrainGo = GetOrCreateChild(trackBox.transform, "Mini_Train");
            RectTransform miniTrRt = miniTrainGo.GetComponent<RectTransform>();
            miniTrRt.anchorMin = new Vector2(0.5f, 0.5f);
            miniTrRt.anchorMax = new Vector2(0.5f, 0.5f);
            miniTrRt.anchoredPosition = new Vector2(-120f, -10f);
            miniTrRt.sizeDelta = new Vector2(240f, 90f);
            var miniImg = GetOrAddComponent<Image>(miniTrainGo);
            miniImg.sprite = miniTrain;
            miniImg.preserveAspect = true;
            miniImg.color = Color.white;

            // Status Text
            GameObject statusGo = GetOrCreateChild(paperGo.transform, "Txt_Status");
            RectTransform statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -150f);
            statusRt.sizeDelta = new Vector2(440f, 32f);
            var txtStatus = GetOrAddComponent<TextMeshProUGUI>(statusGo);
            txtStatus.text = "Đang vận chuyển...";
            txtStatus.alignment = TextAlignmentOptions.Center;
            txtStatus.fontSize = 22;
            txtStatus.fontStyle = FontStyles.Bold;
            txtStatus.color = new Color(0.36f, 0.20f, 0.09f);

            // Timer Box
            GameObject timerGo = GetOrCreateChild(paperGo.transform, "Timer_Box");
            RectTransform timerRt = timerGo.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.5f, 0.5f);
            timerRt.anchorMax = new Vector2(0.5f, 0.5f);
            timerRt.anchoredPosition = new Vector2(0f, -25f);
            timerRt.sizeDelta = new Vector2(320f, 85f);
            var timerImg = GetOrAddComponent<Image>(timerGo);
            timerImg.sprite = timerBox;
            timerImg.type = Image.Type.Sliced;
            timerImg.color = Color.white;

            GameObject txtLabelGo = GetOrCreateChild(timerGo.transform, "Txt_Label");
            RectTransform lblRt = txtLabelGo.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0.5f, 1f);
            lblRt.anchorMax = new Vector2(0.5f, 1f);
            lblRt.anchoredPosition = new Vector2(0f, -14f);
            lblRt.sizeDelta = new Vector2(200f, 22f);
            var txtLbl = GetOrAddComponent<TextMeshProUGUI>(txtLabelGo);
            txtLbl.text = "CÒN LẠI";
            txtLbl.alignment = TextAlignmentOptions.Center;
            txtLbl.fontSize = 16;
            txtLbl.fontStyle = FontStyles.Bold;
            txtLbl.color = new Color(0.79f, 0.67f, 0.49f);

            GameObject txtTimeGo = GetOrCreateChild(timerGo.transform, "Txt_Time");
            RectTransform timeRt = txtTimeGo.GetComponent<RectTransform>();
            timeRt.anchorMin = new Vector2(0.5f, 0f);
            timeRt.anchorMax = new Vector2(0.5f, 0f);
            timeRt.anchoredPosition = new Vector2(0f, 26f);
            timeRt.sizeDelta = new Vector2(240f, 44f);
            var txtTime = GetOrAddComponent<TextMeshProUGUI>(txtTimeGo);
            txtTime.text = "02:14";
            txtTime.alignment = TextAlignmentOptions.Center;
            txtTime.fontSize = 38;
            txtTime.fontStyle = FontStyles.Bold;
            txtTime.color = new Color(1f, 0.85f, 0.47f);

            // Sent Cargo Chips Container
            GameObject chipsGo = GetOrCreateChild(paperGo.transform, "Cargo_Chips");
            RectTransform chipsRt = chipsGo.GetComponent<RectTransform>();
            chipsRt.anchorMin = new Vector2(0.5f, 0f);
            chipsRt.anchorMax = new Vector2(0.5f, 0f);
            chipsRt.anchoredPosition = new Vector2(0f, 110f);
            chipsRt.sizeDelta = new Vector2(440f, 48f);

            for (int i = 0; i < 3; i++)
            {
                GameObject chip = GetOrCreateChild(chipsGo.transform, $"Chip_{i + 1}");
                RectTransform chipRt = chip.GetComponent<RectTransform>();
                chipRt.anchorMin = new Vector2(i * 0.33f, 0f);
                chipRt.anchorMax = new Vector2((i + 1) * 0.33f, 1f);
                chipRt.offsetMin = new Vector2(4f, 0f);
                chipRt.offsetMax = new Vector2(-4f, 0f);
                var chipImg = GetOrAddComponent<Image>(chip);
                chipImg.sprite = bubbleBg;
                chipImg.type = Image.Type.Sliced;
                chipImg.color = Color.white;

                GameObject cIconGo = GetOrCreateChild(chip.transform, "Img_Icon");
                RectTransform ciRt = cIconGo.GetComponent<RectTransform>();
                ciRt.anchorMin = new Vector2(0f, 0.5f);
                ciRt.anchorMax = new Vector2(0f, 0.5f);
                ciRt.anchoredPosition = new Vector2(22f, 0f);
                ciRt.sizeDelta = new Vector2(32f, 32f);
                var ciImg = GetOrAddComponent<Image>(cIconGo);
                ciImg.preserveAspect = true;

                GameObject cTxtGo = GetOrCreateChild(chip.transform, "Txt_Amount");
                RectTransform ctRt = cTxtGo.GetComponent<RectTransform>();
                ctRt.anchorMin = new Vector2(0.45f, 0f);
                ctRt.anchorMax = new Vector2(1f, 1f);
                ctRt.offsetMin = Vector2.zero;
                ctRt.offsetMax = Vector2.zero;
                var ctTxt = GetOrAddComponent<TextMeshProUGUI>(cTxtGo);
                ctTxt.text = "x8";
                ctTxt.alignment = TextAlignmentOptions.Center;
                ctTxt.fontSize = 18;
                ctTxt.fontStyle = FontStyles.Bold;
                ctTxt.color = new Color(0.48f, 0.29f, 0.06f);
            }

            // Speed Up Button
            GameObject btnSpeedGo = GetOrCreateChild(paperGo.transform, "Btn_SpeedUp");
            RectTransform btnSpeedRt = btnSpeedGo.GetComponent<RectTransform>();
            btnSpeedRt.anchorMin = new Vector2(0.5f, 0f);
            btnSpeedRt.anchorMax = new Vector2(0.5f, 0f);
            btnSpeedRt.anchoredPosition = new Vector2(0f, 40f);
            btnSpeedRt.sizeDelta = new Vector2(280f, 58f);
            var btnSpeedImg = GetOrAddComponent<Image>(btnSpeedGo);
            btnSpeedImg.sprite = btnBlue;
            btnSpeedImg.type = Image.Type.Sliced;
            btnSpeedImg.color = Color.white;
            GetOrAddComponent<Button>(btnSpeedGo);

            GameObject txtSpeedGo = GetOrCreateChild(btnSpeedGo.transform, "Txt_SpeedUp");
            RectTransform txtSpeedRt = txtSpeedGo.GetComponent<RectTransform>();
            txtSpeedRt.anchorMin = Vector2.zero;
            txtSpeedRt.anchorMax = Vector2.one;
            txtSpeedRt.offsetMin = Vector2.zero;
            txtSpeedRt.offsetMax = Vector2.zero;
            var txtSpeed = GetOrAddComponent<TextMeshProUGUI>(txtSpeedGo);
            txtSpeed.text = "💎 TĂNG TỐC · 12";
            txtSpeed.alignment = TextAlignmentOptions.Center;
            txtSpeed.fontSize = 22;
            txtSpeed.fontStyle = FontStyles.Bold;
            txtSpeed.color = Color.white;

            // Ra Ga Button
            GameObject btnRaGaGo = GetOrCreateChild(paperGo.transform, "Btn_RaGa");
            RectTransform btnRaGaRt = btnRaGaGo.GetComponent<RectTransform>();
            btnRaGaRt.anchorMin = new Vector2(0.5f, 0f);
            btnRaGaRt.anchorMax = new Vector2(0.5f, 0f);
            btnRaGaRt.anchoredPosition = new Vector2(0f, 40f);
            btnRaGaRt.sizeDelta = new Vector2(280f, 58f);
            var btnRaGaImg = GetOrAddComponent<Image>(btnRaGaGo);
            btnRaGaImg.sprite = btnGreen;
            btnRaGaImg.type = Image.Type.Sliced;
            btnRaGaImg.color = Color.white;
            GetOrAddComponent<Button>(btnRaGaGo);

            GameObject txtRaGaGo = GetOrCreateChild(btnRaGaGo.transform, "Txt_RaGa");
            RectTransform txtRaGaRt = txtRaGaGo.GetComponent<RectTransform>();
            txtRaGaRt.anchorMin = Vector2.zero;
            txtRaGaRt.anchorMax = Vector2.one;
            txtRaGaRt.offsetMin = Vector2.zero;
            txtRaGaRt.offsetMax = Vector2.zero;
            var txtRaGa = GetOrAddComponent<TextMeshProUGUI>(txtRaGaGo);
            txtRaGa.text = "RA GA NHẬN HÀNG";
            txtRaGa.alignment = TextAlignmentOptions.Center;
            txtRaGa.fontSize = 22;
            txtRaGa.fontStyle = FontStyles.Bold;
            txtRaGa.color = Color.white;
            btnRaGaGo.SetActive(false);

            // Close Button X
            GameObject closeGo = GetOrCreateChild(root.transform, "Btn_close");
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(12f, 12f);
            closeRt.sizeDelta = new Vector2(56f, 56f);
            var closeImg = GetOrAddComponent<Image>(closeGo);
            if (closeSp != null) closeImg.sprite = closeSp;
            closeImg.preserveAspect = true;
            GetOrAddComponent<Button>(closeGo);

            var procUI = GetOrAddComponent<TrainProcessPopupUI>(root);
            procUI.ApplyThemeSprites();
            procUI.AutoBindComponents();
            root.SetActive(false);
            return root;
        }

        private static void SetupStationBuildingClick()
        {
            GameObject ga = GameObject.Find("gataulua");
            if (ga != null)
            {
                var col = GetOrAddComponent<BoxCollider2D>(ga);
                col.isTrigger = false;
                col.size = new Vector2(3.5f, 3.0f);
                col.offset = new Vector2(0f, 0.5f);
            }
        }

        private static void SaveAsPrefab(GameObject go, string prefabPath)
        {
            if (go == null) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"[TrainPackageBuildTool] Đã lưu Prefab: {prefabPath}");
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                return go;
            }
            return child.gameObject;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
    }
}
#endif
