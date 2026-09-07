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
            // [VONG 6 - 06/09] CHONG NHAN BAN POPUP (Sep bao "3 popup de nhau").
            // Prefab Popup_Train_MasterStation dang mang THEM 4 component TrainStationMasterPopupUI
            // di lac tren Wagon_1..Wagon_4: m_Script cua chung tro ve fileID 11500000 (= class chinh
            // cua file, tuc chinh class nay) trong khi y dinh la StationWagonSlotUI. Class nay TU DUNG
            // toan bo popup trong BuildOrFixHierarchy(), nen moi component di lac lai dung THEM mot
            // khung go + dong chu hint day du => nhieu popup de nhau.
            // Quy tac: component nao co TO TIEN cung mang script nay thi KHONG phai popup that.
            if (LaBanDiLac())
            {
                _banDiLac = true;
                enabled = false; // chan OnEnable => khong bao gio dung popup tren toa tau
                {
                    Debug.LogWarning($"[Train] Bo component MasterPopupUI di lac tren '{name}' de tranh nhan ban popup.");
                }
                Destroy(this);
                return;
            }

            // Singleton THAT: trong scene chi duoc ton tai duy nhat MOT ban popup ga tau.
            if (Instance != null && Instance != this)
            {
                {
                    Debug.LogWarning($"[Train] Da co popup ga tau '{Instance.name}' - huy ban trung '{name}'.");
                }
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildOrFixHierarchy();
            // Bug 06/09: popup duoc dat SAN o trang thai TAT trong scene, nen Awake() khong
            // chay luc boot - no chi chay LAN DAU TIEN dung vao luc OpenPopup() goi
            // SetActive(true) de MO popup. Neu van tu SetActive(false) o day, popup vua mo
            // se bi tat ngay lap tuc => nguoi choi click ga tau LAN DAU khong thay gi.
            // Chi tu tat khi day KHONG phai la lan Awake do OpenPopup() kich hoat.
            if (!_openRequested)
            {
                gameObject.SetActive(false);
            }
        }

        private bool _openRequested;
        private bool _popupInputLockHeld;
        private bool _banDiLac;

        /// <summary>true = component nay nam TRONG LONG mot popup khac (di lac tren toa tau), khong phai popup that.</summary>
        private bool LaBanDiLac()
        {
            return transform.parent != null
                && transform.parent.GetComponentInParent<TrainStationMasterPopupUI>(true) != null;
        }

        /// <summary>Di nguoc len tim component MasterPopupUI NGOAI CUNG - do moi la popup that.</summary>
        private TrainStationMasterPopupUI TimPopupGoc()
        {
            var goc = this;
            Transform cha = transform.parent;
            while (cha != null)
            {
                var tren = cha.GetComponent<TrainStationMasterPopupUI>();
                if (tren != null) goc = tren;
                cha = cha.parent;
            }
            return goc;
        }

        /// <summary>Lay DUY NHAT popup ga tau that trong scene, bo qua moi component di lac tren toa tau.</summary>
        public static TrainStationMasterPopupUI LayPopupThat()
        {
            if (Instance != null) return Instance;

            var tatCa = FindObjectsByType<TrainStationMasterPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < tatCa.Length; i++)
            {
                if (tatCa[i] == null) continue;
                var goc = tatCa[i].TimPopupGoc();
                if (goc != null) return goc;
            }
            return null;
        }

        private void OnEnable()
        {
            if (_banDiLac) return; // ban di lac dang cho Destroy - khong dung gi ca
            BuildOrFixHierarchy();
            ApplyThemeSprites();
            RefreshUI();
            StartSmokeAnimation();
            if (TrainManager.Instance != null)
                TrainManager.Instance.OnStateChanged += HandleTrainStateChanged;
        }

        private void OnDisable()
        {
            StopSmokeAnimation();
            if (TrainManager.Instance != null)
                TrainManager.Instance.OnStateChanged -= HandleTrainStateChanged;
            ReleasePopupInputBlock();
        }

        /// <summary>Đồng bộ view khi TrainManager đổi state trong lúc popup đang mở.</summary>
        private void HandleTrainStateChanged(global::TrainState s)
        {
            if (!gameObject.activeSelf) return;

            if (s == global::TrainState.ShipDeparting && currentState == TrainState.WaitingForLoad)
                StartCoroutine(RoutineDepartAndOpenTransit());
            else if (s == global::TrainState.RewardDeparting && currentState == TrainState.RewardReadyToCollect)
                StartCoroutine(RoutineDepartRewardTrainAndReset());
        }

        /// <summary>Map state THẬT của TrainManager sang state hiển thị của popup.</summary>
        private TrainState SyncStateFromManager(TrainState fallback)
        {
            var mgr = TrainManager.Instance;
            if (mgr == null) return fallback;
            switch (mgr.State)
            {
                case global::TrainState.RewardArriving:
                case global::TrainState.RewardReadyToCollect:
                    return TrainState.RewardReadyToCollect;
                case global::TrainState.ShipDeparting:
                case global::TrainState.Processing:
                    return TrainState.Processing;
                default:
                    return TrainState.WaitingForLoad;
            }
        }

        public void OpenPopup(TrainState state = TrainState.WaitingForLoad)
        {
            // [VONG 6 - 06/09] Neu bi goi tren mot component di lac (nam trong long popup that),
            // chuyen huong sang popup that thay vi dung THEM mot popup moi.
            var popupThat = TimPopupGoc();
            if (popupThat != this)
            {
                {
                    Debug.LogWarning($"[Train] OpenPopup goi tren ban di lac '{name}' - chuyen huong sang popup that '{popupThat.name}'.");
                }
                popupThat.OpenPopup(state);
                return;
            }

            currentState = SyncStateFromManager(state);
            _openRequested = true; // Bat co TRUOC khi SetActive(true): neu day la lan dau
                                    // popup duoc bat, Awake() se biet day la MO popup, khong
                                    // phai boot, va se KHONG tu SetActive(false) lai.

            // [VONG 3 - 06/09] Bat cac TO TIEN dang tat, dung lai o Canvas gan nhat.
            // Trong SCN_Farm popup nay KHONG nam truc tiep duoi 'Canvas_Popup' ma nam duoi
            // 'Popup_LevelUp_Township'. Neu mot to tien bi tat thi SetActive(true) o day chi
            // doi activeSelf: activeInHierarchy VAN false => khong ve gi len man hinh va ca
            // Awake()/OnEnable() deu khong chay. Dung dung co che cua TrainLoadPopupUI.OpenForWagon().
            Transform anc = transform.parent;
            while (anc != null)
            {
                // [VONG 6 - 06/09] Khong duoc bat / vuot qua mot POPUP KHAC: neu khong vong lap nay
                // se keo ca popup la (vi du Popup_LevelUp_Township) hien theo popup ga tau.
                if (anc.GetComponent<TrainStationMasterPopupUI>() != null) break;

                if (!anc.gameObject.activeSelf)
                {
                    Debug.LogWarning($"[Train] To tien '{anc.name}' dang TAT - da bat lai de popup ga tau hien duoc.");
                    anc.gameObject.SetActive(true);
                }
                if (anc.GetComponent<Canvas>() != null) break;
                anc = anc.parent;
            }

            gameObject.SetActive(true);
            AcquirePopupInputBlock();
            BuildOrFixHierarchy();
            ApplyThemeSprites();
            RefreshUI();
            StartSmokeAnimation();

            {
                Debug.Log($"[Train] OpenPopup xong | state={currentState} | activeInHierarchy={gameObject.activeInHierarchy} | cha='{(transform.parent != null ? transform.parent.name : "(khong co cha)")}' | soBanMasterPopupUI={FindObjectsByType<TrainStationMasterPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}");
            }
        }

        public void ClosePopup()
        {
            // [VONG 6 - 06/09] Neu bi goi tren ban di lac, chuyen huong sang popup that - neu khong
            // cai "toggle" trong TrainStationBuilding se chi tat mot TOA TAU, popup that khong bao gio mo.
            var popupThat = TimPopupGoc();
            if (popupThat != this)
            {
                popupThat.ClosePopup();
                return;
            }

            StopSmokeAnimation();
            ReleasePopupInputBlock();

            // Dọn FX bay dở — coroutine chết khi popup tắt, không dọn sẽ sót icon đứng hình
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.name == "FX_CollectFly")
                    Destroy(child.gameObject);
            }

            gameObject.SetActive(false);
        }

        private void AcquirePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, true);
            if (!_popupInputLockHeld)
            {
                FarmInputLock.RegisterPopupOpen();
                _popupInputLockHeld = true;
            }
        }

        private void ReleasePopupInputBlock()
        {
            FarmInputLock.SetPopupRaycastBlock(gameObject, false);
            if (_popupInputLockHeld)
            {
                FarmInputLock.RegisterPopupClose();
                _popupInputLockHeld = false;
            }
        }

        public void AutoBindIfNull() => BuildOrFixHierarchy();

        public void BuildOrFixHierarchy()
        {
            // 1. Canvas Sorting Order 420 (PopupCaoCap + 20) để luôn nổi trên Tutorial (250) và HUD
            if (canvasComponent == null) canvasComponent = GetComponent<Canvas>();
            if (canvasComponent == null) canvasComponent = gameObject.AddComponent<Canvas>();
            canvasComponent.overrideSorting = true;
            canvasComponent.sortingOrder = 420;

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
            TrainSpriteLoader.Assign(imgWoodFrame, $"{ShopSvgDir}/shop_panel.png", $"{SpritesDir}/popup_frame_wood.png");
            imgWoodFrame.type = Image.Type.Sliced;
            imgWoodFrame.color = Color.white;

            // 4. Inner Scene Container (Lọt lòng 22px) có RectMask2D để clip tàu không tràn ra ngoài viền khung
            Transform innerTr = frameTr.Find("Inner_Scene");
            if (innerTr == null)
            {
                GameObject inGo = new GameObject("Inner_Scene", typeof(RectTransform));
                inGo.transform.SetParent(frameTr, false);
                innerTr = inGo.transform;
            }
            innerTr.SetAsFirstSibling();
            var mask = innerTr.GetComponent<RectMask2D>() ?? innerTr.gameObject.AddComponent<RectMask2D>();
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
            TrainSpriteLoader.Assign(imgBackground, $"{SpritesDir}/station_full_scene_bg.png");
            imgBackground.color = Color.white;

            // Ẩn building trùng nếu có
            Transform dupStn = innerTr.Find("Building_GaHang") ?? frameTr.Find("Building_GaHang");
            if (dupStn != null) dupStn.gameObject.SetActive(false);

            // 6. Train Container (Nằm trên nền cảnh, khớp đúng đường ray)
            Transform tcTr = innerTr.Find("Train_Container");
            bool isNewTc = tcTr == null;
            if (isNewTc)
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
                wTr.gameObject.SetActive(true);
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
            locoTr.gameObject.SetActive(true);
            imgLocomotive = locoTr.GetComponent<Image>() ?? locoTr.gameObject.AddComponent<Image>();
            RectTransform lRt = locoTr.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(0f, 0.5f);
            lRt.pivot = new Vector2(0.5f, 0.5f);
            lRt.anchoredPosition = new Vector2(4 * 185f + 115f, 30f);
            lRt.sizeDelta = new Vector2(240f, 240f);
            TrainSpriteLoader.Assign(imgLocomotive, $"{SpritesDir}/flat_locomotive_horizontal.png");
            imgLocomotive.preserveAspect = true;
            imgLocomotive.color = Color.white;

            // Smoke Puff Root (Nằm trên Train_Container, SetAsLastSibling để vẽ đè lên miệng ống khói)
            Transform smkTr = trainContainer.Find("Smoke_Puff_Root");
            bool isNewSmk = smkTr == null;
            if (isNewSmk)
            {
                GameObject smkGo = new GameObject("Smoke_Puff_Root", typeof(RectTransform));
                smkGo.transform.SetParent(trainContainer, false);
                smkTr = smkGo.transform;
            }
            smkTr.SetAsLastSibling();

            smokePuffRoot = smkTr.GetComponent<RectTransform>();
            if (isNewSmk)
            {
                smokePuffRoot.anchorMin = new Vector2(0f, 0.5f);
                smokePuffRoot.anchorMax = new Vector2(0f, 0.5f);
                smokePuffRoot.pivot = new Vector2(0.5f, 0.5f);
                smokePuffRoot.anchoredPosition = new Vector2(4 * 185f + 115f + 55f, 145f); // Ngay đỉnh miệng ống khói đầu tàu
                smokePuffRoot.sizeDelta = new Vector2(60f, 60f);
            }

            Sprite puffSp = TrainSpriteLoader.GetSprite($"{MillSvgDir}/mill_smoke_puff.png")
                         ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/train_smoke_puff.png")
                         ?? TrainSpriteLoader.GetSprite($"{SpritesDir}/steam_smoke_cloud.png");

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
                if (puffSp != null) pImg.sprite = puffSp;
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
            TrainSpriteLoader.Assign(imgHintPill, $"{ShopSvgDir}/shop_card_outer.png", $"{SpritesDir}/bubble_cargo_req.png");
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
            TrainSpriteLoader.Assign(ribbonBannerImage, $"{ShopSvgDir}/shop_banner_ribbon.png", $"{SpritesDir}/ribbon_banner_gold.png");
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
            Sprite sprClose = UIStandardSprites.Close;                 // WP-D2b: nút đóng chuẩn
            if (sprClose != null) { cImg.sprite = sprClose; cImg.type = Image.Type.Sliced; }
            else TrainSpriteLoader.Assign(cImg, $"{PerfectSvgDir}/btn_close.png", "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/btnX.png");
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

            var mgr   = TrainManager.Instance;
            var slots = mgr != null ? mgr.SlotData : null;

            if (currentState == TrainState.WaitingForLoad)
            {
                if (txtTitle != null) txtTitle.text = "TÀU CHỞ HÀNG";
                if (txtHint != null)
                {
                    int cargoCount = 0;
                    if (slots != null)
                        foreach (var s in slots)
                            if (s != null && s.mode == global::TrainWagonSlotMode.CargoRequest) cargoCount++;

                    int total = slots != null ? slots.Length : wagonSlots.Length;
                    txtHint.text = cargoCount > 0 && cargoCount < total
                        ? $"Tàu {total} toa · chuyến này yêu cầu {cargoCount} loại hàng — chạm toa để nạp!"
                        : "Chạm vào toa để nạp hàng — nạp đủ các toa yêu cầu, tàu sẽ khởi hành!";
                }

                for (int i = 0; i < wagonSlots.Length; i++)
                {
                    if (wagonSlots[i] == null) continue;
                    int wagonIdx = i;

                    var data = (slots != null && i < slots.Length) ? slots[i] : null;
                    if (data == null || data.mode != global::TrainWagonSlotMode.CargoRequest)
                    {
                        Sprite defaultIcon = GetFallbackCargoIcon(wagonIdx);
                        wagonSlots[i].SetupCargoMode("Nông sản", defaultIcon, 0, 10, () => OnWagonClicked(wagonIdx));
                        continue;
                    }

                    wagonSlots[i].SetupCargoMode(data.displayName, data.icon,
                        data.currentAmount, data.requiredAmount,
                        () => OnWagonClicked(wagonIdx));
                }
            }
            else if (currentState == TrainState.RewardReadyToCollect)
            {
                if (txtTitle != null) txtTitle.text = "NHẬN THƯỞNG";
                if (txtHint != null) txtHint.text = "Chạm từng toa để thu — thu hết các toa, tàu sẽ rời ga!";

                for (int i = 0; i < wagonSlots.Length; i++)
                {
                    if (wagonSlots[i] == null) continue;
                    int wagonIdx = i;

                    var data = (slots != null && i < slots.Length) ? slots[i] : null;
                    if (data == null || data.mode != global::TrainWagonSlotMode.Reward)
                    {
                        wagonSlots[i].SetupEmptyMode();
                        continue;
                    }

                    wagonSlots[i].SetupRewardMode(data.displayName, data.icon,
                        data.rewardAmount, data.isCollected,
                        () => OnRewardWagonClicked(wagonIdx));
                }
            }
        }

        private Sprite GetFallbackCargoIcon(int idx)
        {
#if UNITY_EDITOR
            string[] paths = {
                "Assets/_Game/Farm/data/Hat_giong/Item_LuaMi.asset",
                "Assets/_Game/Farm/data/Hat_giong/Item_Ngo.asset",
                "Assets/_Game/Farm/data/Hat_giong/Item_CaRot.asset",
                "Assets/_Game/Farm/data/Farm_dong_vat/Item_Egg.asset"
            };
            if (idx >= 0 && idx < paths.Length)
            {
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<InventoryItemData>(paths[idx]);
                if (item != null && item.icon != null) return item.icon;
            }
#endif
            return Resources.Load<Sprite>("Icons/icon_wheat") ?? Resources.Load<Sprite>("Icons/icon_corn");
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
            // TrainManager là nguồn sự thật — tự kiểm tra đủ hàng và khởi hành
            TrainManager.Instance?.CheckAllLoaded();
        }

        private IEnumerator RoutineDepartAndOpenTransit()
        {
            if (txtHint != null) txtHint.text = "Tất cả các toa đã đủ hàng! Tàu đang khởi hành...";

            yield return new WaitForSeconds(0.6f);

            // Tàu trượt sang phải mượt mà
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

            // Reset vị trí tàu cho lần mở sau
            if (trainContainer != null) trainContainer.anchoredPosition = new Vector2(-60f, -145f);

            ClosePopup();

            // Mở popup 'Đang vận chuyển' — timer thật từ TrainManager
            var procPopup = TrainProcessPopupUI.Instance
                ?? FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);
            if (procPopup != null) procPopup.OpenPopup();
        }

        private void OnRewardWagonClicked(int wagonIndex)
        {
            if (currentState != TrainState.RewardReadyToCollect) return;

            var mgr = TrainManager.Instance;
            if (mgr == null || mgr.SlotData == null || wagonIndex < 0 || wagonIndex >= mgr.SlotData.Length) return;

            var data = mgr.SlotData[wagonIndex];
            if (data == null || data.mode != global::TrainWagonSlotMode.Reward || data.isCollected) return;

            if (mgr.State == global::TrainState.RewardArriving)
            {
                FarmUIManager.Instance?.ShowHint("Tàu đang vào ga — chờ một chút nhé!");
                return;
            }

            // TrainManager tự lo: kho đầy → hint và KHÔNG thu; đủ chỗ → cộng kho + EXP + save.
            // FX world tắt (false) — popup tự vẽ FX 'bùm + bay vào kho' ngay trên canvas này.
            mgr.CollectReward(wagonIndex, false);

            if (data.isCollected)
            {
                if (wagonIndex < wagonSlots.Length && wagonSlots[wagonIndex] != null)
                    wagonSlots[wagonIndex].PlayClaimRewardEffect();

                SpawnCollectFlyFX(wagonIndex, data.icon, data.rewardAmount);

                // Đợi hiệu ứng nảy xong rồi mới vẽ lại (nếu tàu chưa rời ga)
                StartCoroutine(RoutineRefreshAfter(0.5f));
            }
        }

        // ─── FX nhận thưởng: icon "bùm" nảy lên rồi bay theo đường cong vào icon KHO trên HUD ───

        private void SpawnCollectFlyFX(int wagonIndex, Sprite icon, int amount)
        {
            if (icon == null) return;
            if (wagonIndex < 0 || wagonIndex >= wagonSlots.Length || wagonSlots[wagonIndex] == null) return;

            var from = wagonSlots[wagonIndex].transform as RectTransform;
            if (from == null) return;

            // Thưởng nhiều bay nhiều icon hơn cho đã mắt (2-4 cái, so le nhau)
            int count = Mathf.Clamp(1 + amount / 4, 2, 4);
            for (int i = 0; i < count; i++)
                StartCoroutine(RoutineCollectFly(from, icon, i * 0.09f));
        }

        private IEnumerator RoutineCollectFly(RectTransform from, Sprite icon, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (!gameObject.activeInHierarchy) yield break;

            // Đích: icon kho trên HUD (qua HarvestFeedbackSpawner) — thiếu thì bay lên mép trên màn hình
            // Camera UI: overlay → null; ScreenSpace-Camera → worldCamera (kẻo FX lệch đích)
            Camera uiCam = (canvasComponent != null && canvasComponent.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvasComponent.worldCamera : null;

            Vector2 targetScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.94f);
            var spawner = HarvestFeedbackSpawner.Instance;
            if (spawner != null && spawner.WarehouseTarget != null)
            {
                var wt = spawner.WarehouseTarget;
                if (wt is RectTransform wrt)
                    targetScreen = RectTransformUtility.WorldToScreenPoint(uiCam, wrt.position);
                else if (Camera.main != null)
                    targetScreen = Camera.main.WorldToScreenPoint(wt.position);
            }

            var canvasRt = transform as RectTransform;
            if (canvasRt == null) yield break;

            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(uiCam, from.position)
                                + new Vector2(Random.Range(-22f, 22f), 40f);

            Vector2 startLocal, endLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, startScreen, uiCam, out startLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, targetScreen, uiCam, out endLocal);

            var go = new GameObject("FX_CollectFly", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(64f, 64f);
            rt.anchoredPosition = startLocal;

            // Pha 1 — BÙM: icon rớt ra, nảy vọt lên + phóng to
            float popDur = 0.28f;
            Vector2 popUp = startLocal + new Vector2(Random.Range(-30f, 30f), 95f);
            float t1 = 0f;
            while (t1 < popDur)
            {
                t1 += Time.deltaTime;
                float k = Mathf.Clamp01(t1 / popDur);
                rt.localScale = Vector3.one * (0.3f + 1.15f * Mathf.Sin(k * Mathf.PI * 0.85f));
                float ease = 1f - (1f - k) * (1f - k); // ease-out
                rt.anchoredPosition = Vector2.Lerp(startLocal, popUp, ease);
                yield return null;
            }

            // Pha 2 — bay theo đường cong Bezier vào kho, thu nhỏ dần rồi tan
            float flyDur = 0.55f;
            Vector2 ctrl = (popUp + endLocal) * 0.5f + new Vector2(0f, 150f);
            float t2 = 0f;
            while (t2 < flyDur)
            {
                t2 += Time.deltaTime;
                float k = Mathf.Clamp01(t2 / flyDur);
                float ik = 1f - k;
                rt.anchoredPosition = ik * ik * popUp + 2f * ik * k * ctrl + k * k * endLocal;
                rt.localScale = Vector3.one * Mathf.Lerp(1.25f, 0.35f, k);
                img.color = new Color(1f, 1f, 1f, k > 0.82f ? (1f - k) / 0.18f : 1f);
                yield return null;
            }

            Destroy(go);
        }

        private IEnumerator RoutineRefreshAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            var mgr = TrainManager.Instance;
            if (gameObject.activeSelf && mgr != null && mgr.State == global::TrainState.RewardReadyToCollect)
                RefreshUI();
        }

        private IEnumerator RoutineDepartRewardTrainAndReset()
        {
            if (txtHint != null) txtHint.text = "Tàu đang rời ga... chuyến tàu mới sắp tới!";

            yield return new WaitForSeconds(1.0f);

            // Tàu trượt ra khỏi popup mượt mà
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

            // Chuyến mới do TrainManager tự tạo (OnRewardReachedHidden) — popup chỉ reset view
            currentState = TrainState.WaitingForLoad;
            if (trainContainer != null) trainContainer.anchoredPosition = new Vector2(-60f, -145f);
            ClosePopup();
        }

        // =========================================================================
        // Hiệu ứng Khói bốc lên bụp bụp liên tục từ miệng ống khói
        // =========================================================================
        private void StartSmokeAnimation()
        {
            StopSmokeAnimation();
            if (gameObject.activeInHierarchy)
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
                    if (smokePuffs[i] != null && gameObject.activeInHierarchy)
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
}
