using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExportTrainUIPackage
{
    public class TrainStationMasterPopupUI : MonoBehaviour
    {
        public static TrainStationMasterPopupUI Instance { get; private set; }

        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";
        private const string ShopSvgDir = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";
        private const string MillSvgDir = "Assets/Assetsgame/popup/ui_mill_assets/generated_sprites";
        private const string PerfectSvgDir = "Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites";

        [Header("Canvas & Dimming")]
        public Canvas canvasComponent;
        public Image imgDimOverlay;

        [Header("Main Wood Frame")]
        public RectTransform mainWoodFrame;
        public Image imgWoodFrame;

        [Header("Background")]
        public Image imgBackground;

        [Header("UI Header & Footer")]
        public Image ribbonBannerImage;
        public TextMeshProUGUI txtTitle;
        public Button btnClose;
        public GameObject hintPill;
        public Image imgHintPill;
        public TextMeshProUGUI txtHint;

        [Header("Train & Wagons")]
        public RectTransform trainContainer;
        public Image imgLocomotive;
        public RectTransform[] wagonContainers = new RectTransform[4];
        public StationWagonSlotUI[] wagonSlots = new StationWagonSlotUI[4];

        [Header("Smoke Animation")]
        public RectTransform smokePuffRoot;
        public Image[] smokePuffs = new Image[4];
        private Coroutine smokeRoutine;

        [Header("State Info")]
        public TrainState currentState = TrainState.WaitingForLoad;

        private void Awake()
        {
            Instance = this;
            BuildOrFixHierarchy();
        }

        private void OnEnable()
        {
            BuildOrFixHierarchy();
            ApplyThemeSprites();
            RefreshUI();
            StartSmokeAnimation();
        }

        private void OnDisable()
        {
            StopSmokeAnimation();
        }

        public void OpenPopup(TrainState state = TrainState.WaitingForLoad)
        {
            currentState = state;
            gameObject.SetActive(true);
            BuildOrFixHierarchy();
            ApplyThemeSprites();
            RefreshUI();
            StartSmokeAnimation();
        }

        public void ClosePopup()
        {
            StopSmokeAnimation();
            gameObject.SetActive(false);
        }

        public void AutoBindIfNull() => BuildOrFixHierarchy();

        public void BuildOrFixHierarchy()
        {
            // 1. Canvas Sorting Order 160 để đè lên toàn bộ HUD
            if (canvasComponent == null) canvasComponent = GetComponent<Canvas>();
            if (canvasComponent == null) canvasComponent = gameObject.AddComponent<Canvas>();
            canvasComponent.overrideSorting = true;
            canvasComponent.sortingOrder = 160;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Xóa/tắt Image trên chính Root nếu có để không bị tấm trắng đè
            var rootImg = GetComponent<Image>();
            if (rootImg != null)
            {
                rootImg.color = new Color(0, 0, 0, 0);
                rootImg.raycastTarget = false;
            }

            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.anchorMin = new Vector2(0.5f, 0.5f);
                rootRt.anchorMax = new Vector2(0.5f, 0.5f);
                rootRt.pivot = new Vector2(0.5f, 0.5f);
                rootRt.anchoredPosition = Vector2.zero;
                rootRt.sizeDelta = new Vector2(1400f, 820f);
            }

            // 2. Dim Overlay Full-screen (3840x2160)
            Transform dimTr = transform.Find("Panel_Dim") ?? transform.Find("Dim_Overlay");
            if (dimTr == null)
            {
                GameObject dGo = new GameObject("Panel_Dim", typeof(RectTransform));
                dGo.transform.SetParent(transform, false);
                dimTr = dGo.transform;
            }
            dimTr.SetAsFirstSibling();
            imgDimOverlay = dimTr.GetComponent<Image>() ?? dimTr.gameObject.AddComponent<Image>();
            RectTransform dimRt = dimTr.GetComponent<RectTransform>();
            dimRt.anchorMin = new Vector2(0.5f, 0.5f);
            dimRt.anchorMax = new Vector2(0.5f, 0.5f);
            dimRt.anchoredPosition = Vector2.zero;
            dimRt.sizeDelta = new Vector2(3840f, 2160f);
            imgDimOverlay.color = new Color(0.04f, 0.08f, 0.03f, 0.75f);
            imgDimOverlay.raycastTarget = true;

            // 3. Main Wood Frame (1400x820)
            Transform frameTr = transform.Find("Main_Frame");
            if (frameTr == null)
            {
                GameObject fGo = new GameObject("Main_Frame", typeof(RectTransform));
                fGo.transform.SetParent(transform, false);
                frameTr = fGo.transform;
            }
            mainWoodFrame = frameTr.GetComponent<RectTransform>();
            mainWoodFrame.anchorMin = new Vector2(0.5f, 0.5f);
            mainWoodFrame.anchorMax = new Vector2(0.5f, 0.5f);
            mainWoodFrame.pivot = new Vector2(0.5f, 0.5f);
            mainWoodFrame.anchoredPosition = Vector2.zero;
            mainWoodFrame.sizeDelta = new Vector2(1400f, 820f);

            imgWoodFrame = frameTr.GetComponent<Image>() ?? frameTr.gameObject.AddComponent<Image>();
            imgWoodFrame.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_panel.png")
                               ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/popup_frame_wood.png");
            imgWoodFrame.type = Image.Type.Sliced;
            imgWoodFrame.color = Color.white;

            // 4. Inner Scene Container (Lọt lòng 22px)
            Transform innerTr = frameTr.Find("Inner_Scene");
            if (innerTr == null)
            {
                GameObject inGo = new GameObject("Inner_Scene", typeof(RectTransform));
                inGo.transform.SetParent(frameTr, false);
                innerTr = inGo.transform;
            }
            innerTr.SetAsFirstSibling();
            RectTransform inRt = innerTr.GetComponent<RectTransform>();
            inRt.anchorMin = Vector2.zero;
            inRt.anchorMax = Vector2.one;
            inRt.offsetMin = new Vector2(22f, 22f);
            inRt.offsetMax = new Vector2(-22f, -22f);

            // 5. Background Scene (station_full_scene_bg.png)
            Transform bgTr = innerTr.Find("Img_Background");
            if (bgTr == null)
            {
                GameObject bgGo = new GameObject("Img_Background", typeof(RectTransform));
                bgGo.transform.SetParent(innerTr, false);
                bgTr = bgGo.transform;
            }
            bgTr.SetAsFirstSibling();
            imgBackground = bgTr.GetComponent<Image>() ?? bgTr.gameObject.AddComponent<Image>();
            RectTransform bgRt = bgTr.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            imgBackground.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/station_full_scene_bg.png");
            imgBackground.color = Color.white;

            // Ẩn building trùng nếu có
            Transform dupStn = innerTr.Find("Building_GaHang") ?? frameTr.Find("Building_GaHang");
            if (dupStn != null) dupStn.gameObject.SetActive(false);

            // 6. Train Container (Nằm trên nền cảnh, khớp đúng đường ray)
            Transform tcTr = innerTr.Find("Train_Container");
            if (tcTr == null)
            {
                GameObject tcGo = new GameObject("Train_Container", typeof(RectTransform));
                tcGo.transform.SetParent(innerTr, false);
                tcTr = tcGo.transform;
            }
            trainContainer = tcTr.GetComponent<RectTransform>();
            trainContainer.anchorMin = new Vector2(0.5f, 0.5f);
            trainContainer.anchorMax = new Vector2(0.5f, 0.5f);
            trainContainer.pivot = new Vector2(0.5f, 0.5f);
            trainContainer.anchoredPosition = new Vector2(-60f, -145f); // Tọa độ bánh xe đặt trực tiếp trên mặt ray
            trainContainer.sizeDelta = new Vector2(1050f, 300f);

            // 4 Wagons
            for (int i = 0; i < 4; i++)
            {
                Transform wTr = trainContainer.Find($"Wagon_{i + 1}");
                if (wTr == null)
                {
                    GameObject wGo = new GameObject($"Wagon_{i + 1}", typeof(RectTransform));
                    wGo.transform.SetParent(trainContainer, false);
                    wTr = wGo.transform;
                }
                wagonContainers[i] = wTr.GetComponent<RectTransform>();
                wagonContainers[i].anchorMin = new Vector2(0f, 0.5f);
                wagonContainers[i].anchorMax = new Vector2(0f, 0.5f);
                wagonContainers[i].pivot = new Vector2(0.5f, 0.5f);
                wagonContainers[i].anchoredPosition = new Vector2(i * 185f + 95f, 0f);
                wagonContainers[i].sizeDelta = new Vector2(180f, 240f);

                wagonSlots[i] = wTr.GetComponent<StationWagonSlotUI>() ?? wTr.gameObject.AddComponent<StationWagonSlotUI>();
                wagonSlots[i].BuildWagonHierarchy();
            }

            // Locomotive
            Transform locoTr = trainContainer.Find("Locomotive_Flat");
            if (locoTr == null)
            {
                GameObject lGo = new GameObject("Locomotive_Flat", typeof(RectTransform));
                lGo.transform.SetParent(trainContainer, false);
                locoTr = lGo.transform;
            }
            imgLocomotive = locoTr.GetComponent<Image>() ?? locoTr.gameObject.AddComponent<Image>();
            RectTransform lRt = locoTr.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0.5f, 0.5f);
            lRt.anchoredPosition = new Vector2(4 * 185f + 115f, 30f);
            lRt.sizeDelta = new Vector2(240f, 240f);
            imgLocomotive.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/flat_locomotive_horizontal.png");
            imgLocomotive.preserveAspect = true;
            imgLocomotive.color = Color.white;

            // Smoke Puff Root (Nằm trên Train_Container, SetAsLastSibling để vẽ đè lên miệng ống khói)
            Transform smkTr = trainContainer.Find("Smoke_Puff_Root");
            if (smkTr == null)
            {
                GameObject smkGo = new GameObject("Smoke_Puff_Root", typeof(RectTransform));
                smkGo.transform.SetParent(trainContainer, false);
                smkTr = smkGo.transform;
            }
            smkTr.SetAsLastSibling();

            smokePuffRoot = smkTr.GetComponent<RectTransform>();
            smokePuffRoot.anchorMin = new Vector2(0f, 0.5f);
            smokePuffRoot.anchorMax = new Vector2(0f, 0.5f);
            smokePuffRoot.pivot = new Vector2(0.5f, 0.5f);
            smokePuffRoot.anchoredPosition = new Vector2(4 * 185f + 115f + 40f, 135f); // Ngay miệng ống khói
            smokePuffRoot.sizeDelta = new Vector2(50f, 50f);

            Sprite puffSp = TrainSpriteLoader.GetSprite($"{MillSvgDir}/mill_smoke_puff.png")
                         ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/train_smoke_puff.png");

            for (int i = 0; i < 4; i++)
            {
                Transform pTr = smokePuffRoot.Find($"Puff_{i + 1}");
                if (pTr == null)
                {
                    GameObject pGo = new GameObject($"Puff_{i + 1}", typeof(RectTransform));
                    pGo.transform.SetParent(smokePuffRoot, false);
                    pTr = pGo.transform;
                }
                var pImg = pTr.GetComponent<Image>() ?? pTr.gameObject.AddComponent<Image>();
                pImg.sprite = puffSp;
                pImg.color = Color.white;
                pImg.preserveAspect = true;
                pImg.raycastTarget = false;
                smokePuffs[i] = pImg;
                pTr.gameObject.SetActive(false);
            }

            // 7. Hint Pill (Đáy Inner_Scene)
            Transform hintTr = innerTr.Find("Hint_Pill");
            if (hintTr == null)
            {
                GameObject hintGo = new GameObject("Hint_Pill", typeof(RectTransform));
                hintGo.transform.SetParent(innerTr, false);
                hintTr = hintGo.transform;
            }
            hintTr.SetAsLastSibling();
            hintPill = hintTr.gameObject;
            imgHintPill = hintTr.GetComponent<Image>() ?? hintTr.gameObject.AddComponent<Image>();
            imgHintPill.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                              ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
            imgHintPill.type = Image.Type.Sliced;
            imgHintPill.color = Color.white;

            RectTransform hRt = hintTr.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 0f);
            hRt.anchorMax = new Vector2(0.5f, 0f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.anchoredPosition = new Vector2(0f, 35f);
            hRt.sizeDelta = new Vector2(800f, 52f);

            Transform txtHTr = hintTr.Find("Txt_Hint");
            if (txtHTr == null)
            {
                GameObject thGo = new GameObject("Txt_Hint", typeof(RectTransform));
                thGo.transform.SetParent(hintTr, false);
                txtHTr = thGo.transform;
            }
            txtHint = txtHTr.GetComponent<TextMeshProUGUI>() ?? txtHTr.gameObject.AddComponent<TextMeshProUGUI>();
            RectTransform thRt = txtHTr.GetComponent<RectTransform>();
            thRt.anchorMin = Vector2.zero;
            thRt.anchorMax = Vector2.one;
            thRt.offsetMin = Vector2.zero;
            thRt.offsetMax = Vector2.zero;
            txtHint.text = "Nạp đủ hàng cho các toa để tàu khởi hành vận chuyển!";
            txtHint.alignment = TextAlignmentOptions.Center;
            txtHint.fontSize = 20;
            txtHint.fontStyle = FontStyles.Bold;
            txtHint.color = new Color(0.36f, 0.20f, 0.09f);

            // 8. Header Banner Ribbon (Con của Main_Frame, SetAsLastSibling để nằm ở mặt tiền)
            Transform ribTr = frameTr.Find("Header_Banner") ?? frameTr.Find("Ribbon_Banner");
            if (ribTr == null)
            {
                GameObject rGo = new GameObject("Header_Banner", typeof(RectTransform));
                rGo.transform.SetParent(frameTr, false);
                ribTr = rGo.transform;
            }
            ribTr.SetAsLastSibling();

            ribbonBannerImage = ribTr.GetComponent<Image>() ?? ribTr.gameObject.AddComponent<Image>();
            ribbonBannerImage.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_banner_ribbon.png")
                                    ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/ribbon_banner_gold.png");
            ribbonBannerImage.type = Image.Type.Sliced;
            ribbonBannerImage.color = Color.white;

            RectTransform ribRt = ribTr.GetComponent<RectTransform>();
            ribRt.anchorMin = new Vector2(0.5f, 1f);
            ribRt.anchorMax = new Vector2(0.5f, 1f);
            ribRt.pivot = new Vector2(0.5f, 0.5f);
            ribRt.anchoredPosition = new Vector2(0f, -8f);
            ribRt.sizeDelta = new Vector2(620f, 126f);

            Transform txtRTr = ribTr.Find("Txt_Title");
            if (txtRTr == null)
            {
                GameObject trGo = new GameObject("Txt_Title", typeof(RectTransform));
                trGo.transform.SetParent(ribTr, false);
                txtRTr = trGo.transform;
            }
            txtTitle = txtRTr.GetComponent<TextMeshProUGUI>() ?? txtRTr.gameObject.AddComponent<TextMeshProUGUI>();
            RectTransform trRt = txtRTr.GetComponent<RectTransform>();
            trRt.anchorMin = Vector2.zero;
            trRt.anchorMax = Vector2.one;
            trRt.offsetMin = Vector2.zero;
            trRt.offsetMax = Vector2.zero;
            txtTitle.text = "TÀU LỬA";
            txtTitle.alignment = TextAlignmentOptions.Center;
            txtTitle.fontSize = 42;
            txtTitle.fontStyle = FontStyles.Bold;
            txtTitle.color = new Color(0.36f, 0.20f, 0.09f);

            // 9. Close Button X (Con của Main_Frame, SetAsLastSibling ở tầng cao nhất)
            Transform closeTr = frameTr.Find("Btn_Close") ?? frameTr.Find("Btn_close");
            if (closeTr == null)
            {
                GameObject cGo = new GameObject("Btn_Close", typeof(RectTransform));
                cGo.transform.SetParent(frameTr, false);
                closeTr = cGo.transform;
            }
            closeTr.SetAsLastSibling();

            btnClose = closeTr.GetComponent<Button>() ?? closeTr.gameObject.AddComponent<Button>();
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);

            var cImg = closeTr.GetComponent<Image>() ?? closeTr.gameObject.AddComponent<Image>();
            cImg.sprite = TrainSpriteLoader.GetSprite($"{PerfectSvgDir}/btn_close.png")
                       ?? TrainSpriteLoader.GetSprite("Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png");
            cImg.preserveAspect = true;
            cImg.color = Color.white;

            RectTransform cRt = closeTr.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(-20f, -20f);
            cRt.sizeDelta = new Vector2(86f, 86f);
        }

        public void ApplyThemeSprites()
        {
            BuildOrFixHierarchy();
        }

        public void RefreshUI()
        {
            ApplyThemeSprites();

            if (currentState == TrainState.WaitingForLoad)
            {
                if (txtTitle != null) txtTitle.text = "TÀU LỬA";
                if (txtHint != null) txtHint.text = "Nạp đủ hàng cho các toa để tàu khởi hành vận chuyển!";

                // Setup all 4 cargo wagons
                for (int i = 0; i < wagonSlots.Length; i++)
                {
                    if (wagonSlots[i] == null) continue;
                    int wagonIdx = i;
                    var sample = TrainItemDatabase.SampleCrops[Mathf.Clamp(i, 0, TrainItemDatabase.SampleCrops.Count - 1)];
                    wagonSlots[i].SetupCargoMode(sample.itemName, sample.iconPath, sample.currentAmount, sample.targetAmount, () =>
                    {
                        OnWagonClicked(wagonIdx);
                    });
                }
            }
            else if (currentState == TrainState.RewardReadyToCollect)
            {
                if (txtTitle != null) txtTitle.text = "NHẬN THƯỞNG";
                if (txtHint != null) txtHint.text = "Chạm từng toa để thu — thu hết 4 toa tàu sẽ rời ga";

                // Setup 4 reward wagons
                for (int i = 0; i < wagonSlots.Length; i++)
                {
                    if (wagonSlots[i] == null) continue;
                    int wagonIdx = i;
                    var rew = TrainItemDatabase.SampleRewards[Mathf.Clamp(i, 0, TrainItemDatabase.SampleRewards.Count - 1)];
                    wagonSlots[i].SetupRewardMode(rew.rewardName, rew.iconPath, rew.amount, rew.isCollected, () =>
                    {
                        OnRewardWagonClicked(wagonIdx);
                    });
                }
            }
        }

        private void OnWagonClicked(int wagonIndex)
        {
            if (currentState != TrainState.WaitingForLoad) return;

            // Open Load Popup (Popup 2/3)
            var loadPopup = TrainLoadPopupUI.Instance
                ?? FindFirstObjectByType<TrainLoadPopupUI>(FindObjectsInactive.Include);

            if (loadPopup != null)
            {
                loadPopup.OpenForWagon(wagonIndex);
            }
        }

        public void CheckAndTriggerDepartureIfAllComplete()
        {
            bool allDone = true;
            for (int i = 0; i < 4; i++)
            {
                if (i < TrainItemDatabase.SampleCrops.Count && !TrainItemDatabase.SampleCrops[i].isComplete)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                StartCoroutine(RoutineDepartAndOpenTransit());
            }
        }

        private IEnumerator RoutineDepartAndOpenTransit()
        {
            if (txtHint != null) txtHint.text = "Tất cả các toa đã đủ hàng! Tàu đang khởi hành...";
            
            // KÍCH HOẠT TÀU NGOÀI MAP CHẠY
            if (TrainManager.Instance != null)
            {
                TrainManager.Instance.SendMessage("CheckAllLoaded", SendMessageOptions.DontRequireReceiver);
            }

            yield return new WaitForSeconds(0.6f);

            // Train slides right smoothly
            if (trainContainer != null)
            {
                Vector2 startPos = trainContainer.anchoredPosition;
                Vector2 targetPos = startPos + new Vector2(950f, 0f);
                float dur = 1.3f;
                float el = 0f;
                while (el < dur)
                {
                    el += Time.deltaTime;
                    trainContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, el / dur);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.3f);
            ClosePopup();

            // Open Transit Countdown Popup (State 4)
            var procPopup = TrainProcessPopupUI.Instance
                ?? FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);

            if (procPopup != null)
            {
                procPopup.OpenPopup(134f);
            }
        }

        private void OnRewardWagonClicked(int wagonIndex)
        {
            if (currentState != TrainState.RewardReadyToCollect) return;

            var rew = TrainItemDatabase.SampleRewards[Mathf.Clamp(wagonIndex, 0, TrainItemDatabase.SampleRewards.Count - 1)];
            if (rew.isCollected) return;

            rew.isCollected = true;
            if (wagonSlots[wagonIndex] != null)
            {
                wagonSlots[wagonIndex].PlayClaimRewardEffect();
            }

            // GIAO PHẦN THƯỞNG VÀO KHO & VÍ TIỀN THẬT
            if (rew.rewardId == "gold")
            {
                if (FarmEconomyManager.Instance != null)
                    FarmEconomyManager.Instance.AddGold(rew.amount);
            }
            else if (rew.rewardId == "gem")
            {
                if (FarmEconomyManager.Instance != null)
                    FarmEconomyManager.Instance.AddGems(rew.amount);
            }
            else
            {
                if (FarmInventoryManager.Instance != null)
                {
                    FarmInventoryManager.Instance.AddItem(rew.rewardId, rew.amount);
                }
            }

            // Check if all 4 rewards collected
            bool allCollected = true;
            for (int i = 0; i < 4; i++)
            {
                var r = TrainItemDatabase.SampleRewards[Mathf.Clamp(i, 0, TrainItemDatabase.SampleRewards.Count - 1)];
                if (!r.isCollected) { allCollected = false; break; }
            }

            if (allCollected)
            {
                StartCoroutine(RoutineDepartRewardTrainAndReset());
            }
        }

        private IEnumerator RoutineDepartRewardTrainAndReset()
        {
            yield return new WaitForSeconds(1.0f);
            if (txtHint != null) txtHint.text = "Tàu đang rời ga... chuyến tàu mới sắp tới!";

            // Smooth slide train out
            if (trainContainer != null)
            {
                Vector2 startPos = trainContainer.anchoredPosition;
                Vector2 targetPos = startPos + new Vector2(950f, 0f);
                float dur = 1.3f;
                float el = 0f;
                while (el < dur)
                {
                    el += Time.deltaTime;
                    trainContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, el / dur);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Reset trip to State 1 with fresh cargo requirements
            for (int i = 0; i < TrainItemDatabase.SampleRewards.Count; i++)
                TrainItemDatabase.SampleRewards[i].isCollected = false;

            for (int i = 0; i < TrainItemDatabase.SampleCrops.Count; i++)
                TrainItemDatabase.SampleCrops[i].currentAmount = 0;

            currentState = TrainState.WaitingForLoad;
            if (trainContainer != null) trainContainer.anchoredPosition = new Vector2(-60f, -145f);
            RefreshUI();
        }

        // =========================================================================
        // Hiệu ứng Khói bốc lên bụp bụp liên tục từ miệng ống khói
        // =========================================================================
        private void StartSmokeAnimation()
        {
            StopSmokeAnimation();
            smokeRoutine = StartCoroutine(RoutineSmokePuff());
        }

        private void StopSmokeAnimation()
        {
            if (smokeRoutine != null)
            {
                StopCoroutine(smokeRoutine);
                smokeRoutine = null;
            }
        }

        private IEnumerator RoutineSmokePuff()
        {
            while (true)
            {
                for (int i = 0; i < smokePuffs.Length; i++)
                {
                    if (smokePuffs[i] != null)
                    {
                        StartCoroutine(AnimateSinglePuff(smokePuffs[i]));
                        yield return new WaitForSeconds(0.35f);
                    }
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator AnimateSinglePuff(Image puff)
        {
            if (puff == null) yield break;
            RectTransform rt = puff.GetComponent<RectTransform>();
            Vector2 origin = new Vector2(0f, 0f);
            float duration = 1.4f;
            float elapsed = 0f;
            puff.gameObject.SetActive(true);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = Mathf.Lerp(0.35f, 1.4f, t);
                float alpha = Mathf.Lerp(0.95f, 0f, t);
                float dy = Mathf.Lerp(0f, 120f, t); // Bốc cao lên 120px
                float dx = Mathf.Sin(t * Mathf.PI * 2f) * 16f - (t * 22f); // Lượn nhẹ sang trái theo gió

                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = origin + new Vector2(dx, dy);
                puff.color = new Color(1f, 1f, 1f, alpha);

                yield return null;
            }

            puff.gameObject.SetActive(false);
        }
    }

    public class StationWagonSlotUI : MonoBehaviour
    {
        private const string SpritesDir = "Assets/Export_Train_UI_Package/Sprites";
        private const string ShopSvgDir = "Assets/Assetsgame/popup/ui_shop_svg/generated_sprites";

        public Image imgWagon;
        public GameObject bubbleReq;
        public Image imgBubble;
        public Image imgDisc;
        public Image imgIcon;
        public TextMeshProUGUI txtAmount;
        public GameObject checkBadge;
        public Image imgCheckBadge;
        public Button btnSlot;

        private System.Action onClickCallback;
        private Coroutine bobbingRoutine;

        public void AutoBindComponents() => BuildWagonHierarchy();

        public void BuildWagonHierarchy()
        {
            RectTransform rootRt = GetComponent<RectTransform>();
            if (rootRt == null) rootRt = gameObject.AddComponent<RectTransform>();

            // 1. Wagon Image
            Transform wTr = transform.Find("Img_Wagon");
            if (wTr == null)
            {
                GameObject wGo = new GameObject("Img_Wagon", typeof(RectTransform));
                wGo.transform.SetParent(transform, false);
                wTr = wGo.transform;
            }
            imgWagon = wTr.GetComponent<Image>() ?? wTr.gameObject.AddComponent<Image>();
            RectTransform wiRt = wTr.GetComponent<RectTransform>();
            wiRt.anchorMin = new Vector2(0.5f, 0f);
            wiRt.anchorMax = new Vector2(0.5f, 0f);
            wiRt.pivot = new Vector2(0.5f, 0.5f);
            wiRt.anchoredPosition = new Vector2(0f, 55f);
            wiRt.sizeDelta = new Vector2(170f, 110f);
            imgWagon.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/flat_wagon_horizontal.png");
            imgWagon.preserveAspect = true;
            imgWagon.color = Color.white;

            // 2. Bubble Req
            Transform bTr = transform.Find("Bubble_Req");
            if (bTr == null)
            {
                GameObject bGo = new GameObject("Bubble_Req", typeof(RectTransform));
                bGo.transform.SetParent(transform, false);
                bTr = bGo.transform;
            }
            bubbleReq = bTr.gameObject;
            imgBubble = bTr.GetComponent<Image>() ?? bTr.gameObject.AddComponent<Image>();
            imgBubble.sprite = TrainSpriteLoader.GetSprite($"{ShopSvgDir}/shop_card_outer.png")
                            ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/bubble_cargo_req.png");
            imgBubble.type = Image.Type.Sliced;
            imgBubble.color = Color.white;

            RectTransform bRt = bTr.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, 175f);
            bRt.sizeDelta = new Vector2(130f, 62f);

            // Icon Disc
            Transform dTr = bTr.Find("Icon_Disc");
            if (dTr == null)
            {
                GameObject dGo = new GameObject("Icon_Disc", typeof(RectTransform));
                dGo.transform.SetParent(bTr, false);
                dTr = dGo.transform;
            }
            imgDisc = dTr.GetComponent<Image>() ?? dTr.gameObject.AddComponent<Image>();
            imgDisc.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/icon_disc_large.png");
            imgDisc.preserveAspect = true;
            imgDisc.color = Color.white;

            RectTransform dRt = dTr.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = new Vector2(32f, 0f);
            dRt.sizeDelta = new Vector2(40f, 40f);

            // Icon
            Transform icTr = dTr.Find("Img_Icon");
            if (icTr == null)
            {
                GameObject icGo = new GameObject("Img_Icon", typeof(RectTransform));
                icGo.transform.SetParent(dTr, false);
                icTr = icGo.transform;
            }
            imgIcon = icTr.GetComponent<Image>() ?? icTr.gameObject.AddComponent<Image>();
            imgIcon.preserveAspect = true;
            RectTransform icRt = icTr.GetComponent<RectTransform>();
            icRt.anchorMin = Vector2.zero;
            icRt.anchorMax = Vector2.one;
            icRt.offsetMin = new Vector2(4f, 4f);
            icRt.offsetMax = new Vector2(-4f, -4f);

            // Amount Text
            Transform amTr = bTr.Find("Txt_Amount");
            if (amTr == null)
            {
                GameObject amGo = new GameObject("Txt_Amount", typeof(RectTransform));
                amGo.transform.SetParent(bTr, false);
                amTr = amGo.transform;
            }
            txtAmount = amTr.GetComponent<TextMeshProUGUI>() ?? amTr.gameObject.AddComponent<TextMeshProUGUI>();
            RectTransform amRt = amTr.GetComponent<RectTransform>();
            amRt.anchorMin = new Vector2(0.45f, 0f);
            amRt.anchorMax = new Vector2(1f, 1f);
            amRt.offsetMin = Vector2.zero;
            amRt.offsetMax = new Vector2(-4f, 0f);
            txtAmount.alignment = TextAlignmentOptions.Center;
            txtAmount.fontSize = 22;
            txtAmount.fontStyle = FontStyles.Bold;
            txtAmount.color = new Color(0.36f, 0.20f, 0.09f);

            // 3. Check Badge
            Transform chkTr = transform.Find("Check_Badge");
            if (chkTr == null)
            {
                GameObject chkGo = new GameObject("Check_Badge", typeof(RectTransform));
                chkGo.transform.SetParent(transform, false);
                chkTr = chkGo.transform;
            }
            checkBadge = chkTr.gameObject;
            imgCheckBadge = chkTr.GetComponent<Image>() ?? chkTr.gameObject.AddComponent<Image>();
            imgCheckBadge.sprite = TrainSpriteLoader.GetSprite($"{SpritesDir}/check_badge_green.png");
            imgCheckBadge.preserveAspect = true;
            imgCheckBadge.color = Color.white;

            RectTransform chkRt = chkTr.GetComponent<RectTransform>();
            chkRt.anchorMin = new Vector2(1f, 0f);
            chkRt.anchorMax = new Vector2(1f, 0f);
            chkRt.pivot = new Vector2(0.5f, 0.5f);
            chkRt.anchoredPosition = new Vector2(-25f, 55f);
            chkRt.sizeDelta = new Vector2(38f, 38f);
            checkBadge.SetActive(false);

            // Button
            btnSlot = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            btnSlot.onClick.RemoveAllListeners();
            btnSlot.onClick.AddListener(() => onClickCallback?.Invoke());
        }

        public void SetupCargoMode(string name, string iconPath, int cur, int target, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (bubbleReq != null) bubbleReq.SetActive(true);

            if (txtAmount != null)
            {
                txtAmount.text = $"{cur}/{target}";
                txtAmount.color = (cur >= target) ? new Color(0.30f, 0.56f, 0.11f) : new Color(0.36f, 0.20f, 0.09f);
            }

            if (checkBadge != null)
                checkBadge.SetActive(cur >= target);

            if (imgIcon != null)
            {
                imgIcon.sprite = TrainSpriteLoader.GetSprite(iconPath);
                imgIcon.color = Color.white;
                imgIcon.enabled = true;
            }

            StartBobbingAnimation();
        }

        public void SetupRewardMode(string name, string iconPath, int count, bool isCollected, System.Action onClick)
        {
            BuildWagonHierarchy();
            onClickCallback = onClick;

            if (checkBadge != null) checkBadge.SetActive(false);

            if (bubbleReq != null)
            {
                bubbleReq.SetActive(!isCollected);
            }

            if (txtAmount != null)
            {
                txtAmount.text = $"x{count}";
                txtAmount.color = new Color(0.48f, 0.29f, 0.06f);
            }

            if (imgIcon != null)
            {
                imgIcon.sprite = TrainSpriteLoader.GetSprite(iconPath);
                imgIcon.color = Color.white;
                imgIcon.enabled = true;
            }

            if (!isCollected) StartBobbingAnimation();
            else StopBobbingAnimation();
        }

        public void PlayClaimRewardEffect()
        {
            if (bubbleReq != null)
                StartCoroutine(RoutineClaimBounce());
        }

        private IEnumerator RoutineClaimBounce()
        {
            if (bubbleReq == null) yield break;
            RectTransform rt = bubbleReq.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            float el = 0f;
            while (el < 0.4f)
            {
                el += Time.deltaTime;
                float scale = 1f + Mathf.Sin(el / 0.4f * Mathf.PI) * 0.4f;
                rt.localScale = Vector3.one * scale;
                rt.anchoredPosition = startPos + new Vector2(0f, Mathf.Sin(el / 0.4f * Mathf.PI) * 25f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            bubbleReq.SetActive(false);
        }

        private void StartBobbingAnimation()
        {
            StopBobbingAnimation();
            bobbingRoutine = StartCoroutine(RoutineBobbing());
        }

        private void StopBobbingAnimation()
        {
            if (bobbingRoutine != null)
            {
                StopCoroutine(bobbingRoutine);
                bobbingRoutine = null;
            }
        }

        private IEnumerator RoutineBobbing()
        {
            if (bubbleReq == null) yield break;
            RectTransform rt = bubbleReq.GetComponent<RectTransform>();
            Vector2 basePos = rt.anchoredPosition;
            float seed = Random.Range(0f, 10f);
            while (true)
            {
                float dy = Mathf.Sin((Time.time + seed) * 3.5f) * 6f;
                rt.anchoredPosition = basePos + new Vector2(0f, dy);
                yield return null;
            }
        }
    }
}
