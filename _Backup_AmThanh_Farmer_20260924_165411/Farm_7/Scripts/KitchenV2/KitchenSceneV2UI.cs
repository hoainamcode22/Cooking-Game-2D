using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KitchenUIv2
{
    /// <summary>
    /// KITCHEN UI v2 (Sprint K1 — skin tạm, layout theo mockup "Kitchen Cook Flow" 2026-08-26).
    /// VIEW THUẦN tự dựng runtime — não là các manager CŨ, không sửa logic:
    ///   chọn món → SetCurrentDish · chọn thẻ → SelectableIngredientCard/CookingSelectionManager (tái dùng)
    ///   NẤU → OnClickCookSubmit (minigame giữ nguyên) · cất kho → CollectCookedDishToWarehouse
    ///   điểm dự kiến → CookingScoreCalculator.Evaluate (static, gọi tự do).
    /// UI cũ KHÔNG bị xoá — canvas này che phủ; minigame/popup cũ được nâng sorting để nổi lên trên.
    /// </summary>
    /// <summary>Bộ da K2 — tool Setup gán từ Export_Kitchen_UI_Package. Field nào trống → giữ màu phẳng K1.</summary>
    [System.Serializable]
    public class KitchenSkin
    {
        [Header("Nền & decor")]
        public Sprite wallTile, floorTile, shelfProps, plantPot, sackFlour, catSleeping;
        [Header("Lò")]
        public Sprite ovenBody, ovenGlow;
        public Sprite[] ovenFire;
        public Sprite smokePuff;
        [Header("Trạm")]
        public Sprite prepTable, platingTable, warehouseHatch, chalkboard;
        [Header("Mèo thần tài")]
        public Sprite[] manekiIdle;
        [Header("Khung UI 9-slice")]
        public Sprite panelBoard, panelPaper, cardIngredient, cardSelectedGlow, cardLocked, iconLock;
        public Sprite tasteTrack, tasteFill, tasteMarker;
        public Sprite btnGreen, btnGray, btnRedSmall, tabOn, tabOff, chipTaste, ribbon;

        [Header("Polish R3 — nút & decor bếp ấm")]
        public Sprite btnBackFarm, btnPaperSmall, cookPot;
        public Sprite decorGarlic, decorOnion, decorHerbs, decorLights;
        /// <summary>[V3] Nen dong mon trong sach cong thuc. Null = dung cardIngredient nhu cu.
        /// Tach rieng vi the khay va dong mon truoc day dung chung 1 sprite, khong the khac kieu.</summary>
        public Sprite cardDishRow;
        public Sprite[] catChefWalk;

        [Header("Icon nhỏ & Polish R7 (2026-08-27)")]
        public Sprite iconGold;
        public Sprite plaqueOvenState, decoCrateStack, decoFirewood;
    }

    public class KitchenSceneV2UI : MonoBehaviour
    {
        public static KitchenSceneV2UI Instance { get; private set; }

        /// <summary>Màu chấm vị dùng chung: Ngọt hồng, Cay đỏ, Chua xanh, Đậm dương, Kết cấu nâu.</summary>
        private static readonly Color[] FlavorDotColors = {
            new Color(0.93f, 0.45f, 0.65f), new Color(0.88f, 0.28f, 0.22f), new Color(0.45f, 0.75f, 0.30f),
            new Color(0.35f, 0.60f, 0.88f), new Color(0.65f, 0.45f, 0.28f)
        };

        [Header("Data — tool Setup gán")]
        [SerializeField] private IngredientData[] allIngredients;
        [SerializeField] private ListDishData dishBook;

        [Header("Skin K2 — tool Setup gán")]
        [SerializeField] private KitchenSkin skin = new KitchenSkin();

        [Header("Managers — trống thì tự tìm")]
        [SerializeField] private CookingChallengeManager challenge;
        [SerializeField] private CookingSelectionManager selection;

        [Header("Layout")]
        [SerializeField] private int canvasSortingOrder = 5;
        [SerializeField] private float pollInterval = 0.15f;

        [Header("Mở rộng ô khay — Sếp chỉnh giá tại đây")]
        [SerializeField] private int slotPackSize = 7;
        [SerializeField] private int slotPackBaseCostGold = 500; // giá gói 1; gói sau = giá × (số gói đã mua + 1)

        [Header("Lửa lò — prefab hạt (bỏ trống = dùng lửa frame như cũ)")]
        [SerializeField] private GameObject ovenFirePrefab;
        [SerializeField] private float fireScale = 1f;
        [Tooltip("CHỈ BẬT SAU KHI XÓA UI CŨ! Đổi canvas sang ScreenSpaceCamera để particle nổi lên UI — nhưng canvas UI CŨ (Overlay) sẽ đè lên canvas camera → UI cũ nổi lên lại.")]
        [SerializeField] private bool useCameraCanvasForFire = false;

        // ── Runtime refs ────────────────────────────────────────────
        private Canvas _canvas;
        private RectTransform _root;

        private TMP_Text _txtChef, _txtGold, _txtOrderName, _txtOrderRewards;
        private Image _imgChefExpFill, _imgOrderIcon, _imgCustomerAvatar;

        private GameObject _boardDetail, _boardList;
        private TMP_Text _txtDishName, _txtDishMeta, _txtNeedTitle, _txtTasteTitle, _txtRewards, _txtProjection;
        private Image _imgDishIcon;
        private Transform _needChipsRoot;
        private readonly FlavorRow[] _flavorRows = new FlavorRow[5];
        private readonly TMP_Text[] _orderChipValues = new TMP_Text[5];
        private Transform _dishListContent;
        private int _listFilter = -1; // -1 = tất cả, else (int)DishDifficulty

        private TMP_Text _txtOvenState, _txtSentCount, _txtChalk, _txtPrepToast;
        private Image _imgOvenFill;
        private Button _btnPlating;
        private TMP_Text _txtPlating;
        private Image _imgPlateDish;
        private bool _dishPopAnimated;
        // [2026-09-24] Vi tri/scale Dish_Visual Sep dat trong Edit mode -> pop/bay kho quay ve dung cho nay
        private Vector3 _plateDishPos0 = new Vector3(0f, 22f, 0f);
        private Vector3 _plateDishScale0 = Vector3.one;

        private Transform _gridIngredients, _gridSeasonings;
        private GameObject _tabIngredients, _tabSeasonings;
        private TMP_Text _txtTabIng, _txtTabSea;
        private Button _btnClearAll;

        private Button _btnAction;
        private Image _imgAction;
        private TMP_Text _txtAction, _txtActionSub;

        private readonly Dictionary<string, SelectableIngredientCard> _cards = new Dictionary<string, SelectableIngredientCard>();
        private TMP_Text _hintEmptyIng, _hintEmptySea; // gợi ý khi khay trống (Sếp 2026-08-27: chỉ hiện món đã gửi)
        private float _pollT;
        private bool _built;
        private bool _ovenBusy;
        [Tooltip("[2026-09-24] Dom khoi bay tu ong khoi lo khi nau (Sep: bay lech khoi ong khoi -> tat).")]
        [SerializeField] private bool khoiLo = false;

        private const string SentCountKey = "kitchen_sent_dishes_v2";

        private struct FlavorRow
        {
            public RectTransform rowRoot;
            public RectTransform dot;
            public TMP_Text label;
            public RectTransform track;
            public Image fill;
            public RectTransform marker;
            public TMP_Text value;

            public void SetY(float y)
            {
                if (rowRoot != null)
                {
                    rowRoot.anchoredPosition = new Vector2(10f, y);
                }
                else
                {
                    if (dot != null) dot.anchoredPosition = new Vector2(14f, y - 5f);
                    if (label != null) label.rectTransform.anchoredPosition = new Vector2(30f, y);
                    if (track != null) track.anchoredPosition = new Vector2(80f, y - 3f);
                    if (value != null) value.rectTransform.anchoredPosition = new Vector2(248f, y);
                }
            }
        }

        // ── Vòng đời ────────────────────────────────────────────────

        /// <summary>
        /// Xoá sạch hierarchy con (kể cả bản PREVIEW tool bake trong Edit mode) rồi dựng lại.
        /// Editor tool gọi để Sếp NHÌN THẤY UI ngay ngoài Edit mode; Start gọi để runtime
        /// luôn dựng bản tươi — nhờ đó preview không bao giờ bị nhân đôi khi Play.
        /// ⚠ Preview chỉ để QUAN SÁT: chỉnh tay lên preview sẽ bị dựng lại đè khi Play/chạy tool.
        /// </summary>
        [Header("Khoa layout (2026-09-21)")]
        [Tooltip("Bat = code KHONG duoc ghi vi tri / kich thuoc nua. Chinh tay bang keo tha se giu nguyen sau khi Play. " +
                 "Bat bang tool 'Kitchen: Dong bang UI thanh Hierarchy' hoac tick tay o day.")]
        [SerializeField] private bool khoaLayout = false;

        /// <summary>[V3 2026-09-22] Bo qua PopupSkinUnifier luc Start. Bat cho Kitchen_UI_v3:
        /// bo art rieng cua Sep khong duoc bi doi ve bo nut/khung cua Shop.</summary>
        [SerializeField] private bool boQuaDongBoSkinShop = false;

        /// <summary>Dung lai UI bang CODE. Da khoa layout thi tu choi, tranh xoa sach chinh tay cua Sep.</summary>
        public void RebuildNow()
        {
            if (khoaLayout || KhoaLayout)
            {
                Debug.LogWarning("[KitchenV2] RebuildNow() bi TU CHOI vi dang KHOA LAYOUT. " +
                                 "Muon dung lai bang code thi bo tick 'Khoa layout' trong Inspector truoc.");
                return;
            }
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) DestroyImmediate(child);
                else DestroyImmediate(child);
            }
            _built = false;
            _cards.Clear();
            _lastSelected.Clear();
            _imgManeki = null; _imgOvenFire = null; _imgOvenGlow = null; _ovenRect = null;
            EnsureBuilt();
        }

        /// <summary>
        /// [2026-09-22] TOOL AN TOAN cho Edit Mode — CHI THEM phan con thieu, KHONG XOA GI.
        ///
        /// Khac han <see cref="BuildEditorPreview"/>: ham do goi RebuildNow() -> xoa sach con
        /// roi dung lai. Do la dung cai da lam hong hierarchy cua Sep hom 21/09.
        /// Ham nay chi chay cac ham Build cua DUNG NHUNG NHANH KHONG TON TAI, phan da co
        /// khong bi dung vao mot li nao. Chay xong Sep Ctrl+S la luu vinh vien.
        ///
        /// Tra ve: mo ta nhung gi vua duoc them (de tool in ra cho Sep doc).
        /// </summary>
        public string VaPhanThieuChoEditor()
        {
            bool khoaCu = KhoaLayout;
            DangVaTrongEditor = true;
            // KHOA LAYOUT trong suot ca luot va. Ly do: KhoaLayout la bien static chi duoc gan
            // trong Start() — o Edit Mode no dang la false, nen Anchor()/Stretch() se GHI DE
            // vi tri Sep vua keo tay. Bat len thi 87 cho goi Anchor deu tro thanh khong lam gi.
            // Rieng luc dung object MOI thi VaPhanThieuTrongHierarchy() tu mo khoa tam thoi,
            // vi object moi bat buoc phai co anchor.
            KhoaLayout = true;
            try
            {
            _canvas = GetComponent<Canvas>();
            _root   = KhungGoc();
            if (!boQuaDongBoSkinShop) EnsureSkinLoaded();

            var truoc = new System.Text.StringBuilder();
            if (_root.Find("Tray")          == null) truoc.Append("Tray (khay nguyen lieu + gia vi), ");
            if (_root.Find("Btn_Action")    == null) truoc.Append("Btn_Action, ");
            if (_root.Find("Btn_BackFarm")  == null) truoc.Append("Btn_BackFarm, ");
            if (_root.Find("Cat_Chef")      == null) truoc.Append("Cat_Chef, ");
            if (_root.Find("Deco_Garlic_R") == null) truoc.Append("bo trang tri treo tuong (7 mon), ");
            var whKiemTra = _root.Find("Warehouse_Box");
            if (whKiemTra != null && whKiemTra.Find("Txt_Sent") == null) truoc.Append("Txt_Sent, ");

            VaPhanThieuTrongHierarchy();

            // Noi tham chieu lai (khong xoa gi) de cac buoc duoi co _gridIngredients, _dishListContent...
            BindExistingHierarchy();

            if (selection == null)
                selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

            int theTruoc = _gridIngredients != null ? _gridIngredients.childCount : 0;
            int monTruoc = _dishListContent != null ? _dishListContent.childCount : 0;

            // Khung RONG -> dung the/dong MOI. Object moi bat buoc phai duoc Anchor(), ma
            // Anchor() dang bi KhoaLayout chan. Mo khoa DUNG trong 2 loi goi nay: chung chi
            // tao con moi trong khung rong, khong dong vao object nao da co.
            KhoaLayout = false;
            try
            {
                if (selection != null && theTruoc == 0) BuildTrayCards();
                // CO Y KHONG goi PickDefaultDish() o day: no dat "mon dang nau" -> keo theo viec
                // ve lai Need_Chips (co vong Destroy con). Danh sach mon khong can no.
                if (monTruoc == 0) RebuildDishList();
            }
            finally { KhoaLayout = true; }

            if (truoc.Length == 0 && theTruoc > 0 && monTruoc > 0)
                return "Khong thieu gi — hierarchy da day du, tool khong dong vao gi ca.";

            return "Da them: " + (truoc.Length > 0 ? truoc.ToString() : "")
                 + (theTruoc == 0 ? ((_gridIngredients != null ? _gridIngredients.childCount : 0) + " the nguyen lieu, "
                                   + (_gridSeasonings  != null ? _gridSeasonings.childCount  : 0) + " the gia vi, ") : "")
                 + (monTruoc == 0 ? ((_dishListContent != null ? _dishListContent.childCount : 0) + " dong mon an") : "");
            }
            finally
            {
                DangVaTrongEditor = false;
                KhoaLayout        = khoaCu;
            }
        }

        /// <summary>Editor tool gọi: dựng cả thẻ nguyên liệu cho preview đầy đủ.</summary>
        public void BuildEditorPreview()
        {
            RebuildNow();
            if (selection == null) selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);
            if (selection != null) BuildTrayCards();
            // [FIX 2026-09-21] Truoc day THIEU 2 dong nay: Dish_Scroll/Content sinh ra RONG,
            // 7 the mon chi hien luc Play -> Sep nhin Scene view thay danh sach trong.
            PickDefaultDish();
            RebuildDishList();
            ShowBoardDetail(true);
        }


        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // [2026-09-23] Scene bep nap ADDITIVE: khung hinh dau tien Unity ve UI TRUOC khi Start() do
            // du lieu that => thay UI mau/Edit mode roi moi doi. An canvas toi khi do xong du lieu.
            var cv = GetComponent<Canvas>();
            if (cv != null && Application.isPlaying) cv.enabled = false;
        }

        private System.Collections.IEnumerator HienKhiSan()
        {
            yield return null;                 // Start() da chay xong
            Loc.RequestRescan();
            yield return null;                 // them 1 khung: dich chu + tinh layout xong
            var cv = GetComponent<Canvas>();
            if (cv != null) cv.enabled = true;
        }

        private void Start()
        {
            // Hen hien canvas o khung SAU (Start chay het ben duoi truoc) — dat dau ham de du Start
            // co loi giua chung thi canvas van hien, khong bao gio bi an vinh vien.
            StartCoroutine(HienKhiSan());
            if (challenge == null) challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            if (selection == null) selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

            // UI SỐNG TRONG HIERARCHY (từ 2026-08-26): scene đã có sẵn khung → chỉ NỐI logic,
            // KHÔNG dựng lại — mọi chỉnh tay của Sếp ngoài editor giữ nguyên khi Play.
            // [FIX 2026-09-02] Rào TỪNG BƯỚC init: trước đây 1 NullReference trong
            // BindExistingHierarchy (Need_Chips còn HorizontalLayoutGroup bake cũ →
            // AddComponent<GridLayoutGroup> trả null) giết cả chuỗi Start → bảng công thức
            // trống + MỌI nút (kể cả VỀ NÔNG TRẠI, khay nguyên liệu) mất listener cùng lúc.
            // Một tài nguyên/hierarchy gãy chỉ được phép làm hỏng đúng phần của nó.
            KhoaLayout = khoaLayout;   // [2026-09-21] ap co truoc moi buoc init
            // [V3] boQuaDongBoSkinShop = "skin nay la cua Sep, dung tu dien": EnsureSkinLoaded chi
            // chay trong Editor va se dien lai tabOn/tabOff ma V3 co y de null -> Editor va build
            // nhin khac nhau. V3 da duoc tool copy day du skin nen khong can loader nay.
            if (!boQuaDongBoSkinShop) EnsureSkinLoaded();
            BuocInit("Bind/Build khung", () =>
            {
                if (KhungGoc().Find("Order_Banner") != null) BindExistingHierarchy();
                else RebuildNow(); // scene trống → dựng khung lần đầu (sau đó Ctrl+S là thành hierarchy cố định)
            });
            BuocInit("RaiseLegacyOverlays", RaiseLegacyOverlays);
            BuocInit("BuildTrayCards", BuildTrayCards);
            BuocInit("PickDefaultDish", PickDefaultDish);
            BuocInit("RefreshAll", RefreshAll);
            BuocInit("ShowRecipeListMenu", () => ShowBoardDetail(false));
            BuocInit("ApplyFont", () => SkinKit.ApFont(transform));
            // [SkinUnifier 2026-09-21] Dong bo nut/vien/ruy bang theo bo cua Shop (chi doi sprite/mau/font).
            if (!boQuaDongBoSkinShop)
                BuocInit("PopupSkinUnifier", () => PopupSkinUnifier.ApDung(transform));
        }

        /// <summary>[FIX 2026-09-02] Chạy 1 bước init trong rào try/catch — lỗi thì log rõ
        /// bước nào gãy và CHO CÁC BƯỚC SAU CHẠY TIẾP (nút vẫn có listener, board vẫn fill).</summary>
        private static void BuocInit(string ten, System.Action buoc)
        {
            try { buoc(); }
            catch (System.Exception e)
            {
                Debug.LogError($"[KitchenV2] Bước init '{ten}' ném exception nhưng KHÔNG chặn các bước sau: {e}");
            }
        }

        private void OnEnable()
        {
            CookingChallengeManager.OnCookStarted  += HandleCookStarted;
            CookingChallengeManager.OnDishCooked   += HandleDishCooked;
            CookingChallengeManager.OnDishFailed   += HandleDishFailed;
            CookingChallengeManager.OnDishCollected += HandleDishCollected;
            TouristVisitorManager.OnQueueOrderChanged += RefreshAll;
            LocalizationManager.OnChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            CookingChallengeManager.OnCookStarted  -= HandleCookStarted;
            CookingChallengeManager.OnDishCooked   -= HandleDishCooked;
            CookingChallengeManager.OnDishFailed   -= HandleDishFailed;
            CookingChallengeManager.OnDishCollected -= HandleDishCollected;
            TouristVisitorManager.OnQueueOrderChanged -= RefreshAll;
            LocalizationManager.OnChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(string lang)
        {
            RefreshAll();
            // Danh sach mon dung ten bang Loc.T() (khong qua interceptor) ⇒ dung lai de doi ngon ngu.
            if (_boardList != null && _boardList.activeSelf) RebuildDishList();
            // Goi y khay trong dung mot lan bang Loc.T() ⇒ doi lai chu theo ngon ngu moi.
            if (_hintEmptyIng != null) SetText(_hintEmptyIng, Loc.T(HintEmptyIngVi));
            if (_hintEmptySea != null) SetText(_hintEmptySea, Loc.T(HintEmptySeaVi));
        }

        private void Update()
        {
            if (!_built) return;
            _pollT -= Time.unscaledDeltaTime;
            if (_pollT <= 0f)
            {
                _pollT = pollInterval;
                RefreshDynamic();
            }
        }

        // ── Event handlers (lò + toast) ─────────────────────────────

        private void HandleCookStarted(DishData d)
        {
            _ovenBusy = true;
            SetText(_txtOvenState, Loc.T("LÒ ĐANG CHÁY..."));
            SetText(_txtPrepToast, "");   // [2026-09-25] bo dong "Preparing: ..." (trung ten mon tren banner, de len chip nguyen lieu)
        }

        private void HandleDishCooked(DishData d, int score)
        {
            _ovenBusy = false;
            SetText(_txtOvenState, Loc.TF("XONG! {0}đ", score));
            SetText(_txtPrepToast, Loc.T("Chạm bàn trình bày để cất vào kho →"));
            // [2026-09-25] Mon vua nau nam tren dia = coi nhu da co -> banner LUOT sang mon khach ke tiep
            TouristVisitorManager.MonDangTrenDia = d != null ? d.dishId : null;
            if (isActiveAndEnabled) { if (_coLuotDon != null) StopCoroutine(_coLuotDon); _coLuotDon = StartCoroutine(CoLuotSangKhachSau(0.9f)); }
        }

        // ── [2026-09-25] Tu chuyen sang don khach ke tiep ─────────────────────
        private Coroutine _coLuotDon;
        private RectTransform _rtBanner;
        private Vector2 _bannerPos0;
        private bool _coBannerPos0;

        private System.Collections.IEnumerator CoLuotSangKhachSau(float cho)
        {
            yield return new WaitForSecondsRealtime(cho);       // de mon bay len dia xong da
            _coLuotDon = null;
            var tm = TouristVisitorManager.Instance;
            var khach = tm != null ? tm.GetFrontWaitingTourist() : null;
            if (khach == null || khach.Dish == null || challenge == null || challenge.IsCooking) { RefreshAll(); yield break; }
            if (challenge.CurrentDish == khach.Dish) { RefreshAll(); yield break; }
            SelectDish(khach.Dish);
            yield return CoLuotBanner();
        }

        private System.Collections.IEnumerator CoLuotBanner()
        {
            if (_rtBanner == null) _rtBanner = TimSauTen(transform, "Order_Banner") as RectTransform;
            if (_rtBanner == null) yield break;
            if (!_coBannerPos0) { _coBannerPos0 = true; _bannerPos0 = _rtBanner.anchoredPosition; }
            var cg = _rtBanner.GetComponent<CanvasGroup>();
            if (cg == null) cg = _rtBanner.gameObject.AddComponent<CanvasGroup>();
            float t = 0f, T = 0.32f;
            while (t < T)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / T), e = 1f - (1f - k) * (1f - k) * (1f - k);
                _rtBanner.anchoredPosition = _bannerPos0 + new Vector2(120f * (1f - e), 0f);
                cg.alpha = Mathf.Lerp(0.2f, 1f, e);
                yield return null;
            }
            _rtBanner.anchoredPosition = _bannerPos0;
            cg.alpha = 1f;
        }

        private static Transform TimSauTen(Transform goc, string ten)
        {
            if (goc == null) return null;
            if (goc.name == ten) return goc;
            for (int i = 0; i < goc.childCount; i++) { var r = TimSauTen(goc.GetChild(i), ten); if (r != null) return r; }
            return null;
        }

        private void HandleDishFailed(DishData d, int score)
        {
            _ovenBusy = false;
            _ovenFakeProgress = 0f;
            TouristVisitorManager.MonDangTrenDia = null;
            if (_imgOvenFill != null) _imgOvenFill.fillAmount = 0f;
            SetText(_txtOvenState, Loc.TF("HỎNG... {0}đ", score));
            SetText(_txtPrepToast, Loc.T("Chọn lại nguyên liệu rồi nấu tiếp nhé!"));
        }

        private void HandleDishCollected(DishData d)
        {
            TouristVisitorManager.MonDangTrenDia = null;   // mon da vao kho -> kho tu dem
            _ovenFakeProgress = 0f;
            if (_imgOvenFill != null) _imgOvenFill.fillAmount = 0f;
            int n = PlayerPrefs.GetInt(SentCountKey, 0) + 1;
            PlayerPrefs.SetInt(SentCountKey, n);
            SetText(_txtSentCount, Loc.TF("Đã gửi {0} món", n));
            SetText(_txtOvenState, Loc.T("Lò đã nghỉ"));
            SetText(_txtPrepToast, "");
        }

        // ── Actions ─────────────────────────────────────────────────

        private void OnActionClicked()
        {
            if (challenge == null) return;
            if (challenge.CookedDishOnPlate != null) return; // còn món trên dĩa — cất trước
            // [VFX 2026-09-24] Bam lien tuc nhom lua: KitchenJuiceFX tu goi OnClickCookSubmit khi lua bung
            if (KitchenJuiceFX.XuLyNutNau(challenge)) return;
            challenge.OnClickCookSubmit();
        }

        private void EnsurePlatingDishVisual()
        {
            if (_btnPlating == null) return;
            if (_imgPlateDish == null)
            {
                var tr = _btnPlating.transform.Find("Dish_Visual");
                if (tr != null)
                {
                    _imgPlateDish = tr.GetComponent<Image>();
                    if (_imgPlateDish != null)
                    {
                        _plateDishPos0 = tr.localPosition;
                        _plateDishScale0 = tr.localScale == Vector3.zero ? Vector3.one : tr.localScale;
                        _imgPlateDish.raycastTarget = false;
                    }
                }
                else
                {
                    var go = new GameObject("Dish_Visual", typeof(RectTransform));
                    go.transform.SetParent(_btnPlating.transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, 22f); // Nổi lên trên mặt đĩa trình bày
                    rt.sizeDelta = new Vector2(72f, 72f);

                    _imgPlateDish = go.AddComponent<Image>();
                    _imgPlateDish.preserveAspect = true;
                    _imgPlateDish.raycastTarget = false;
                }
            }
        }

        private System.Collections.IEnumerator AnimateDishPopToPlate()
        {
            if (_imgPlateDish == null) yield break;
            Transform tr = _imgPlateDish.transform;
            Vector3 baseScale = _plateDishScale0;
            Vector3 endPos = _plateDishPos0;
            Vector3 startPos = endPos + new Vector3(0f, -32f, 0f);

            float duration = 0.45f;
            float elapsed = 0f;
            tr.localScale = Vector3.zero;

            // SFX tiếng hoàn thành món ăn
            AudioManager.Instance?.PlaySuccess();

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Hiệu ứng nảy bounce đàn hồi
                float scaleT = Mathf.Sin(t * Mathf.PI * 0.7f) * 1.25f;
                if (t > 0.7f)
                {
                    float subT = (t - 0.7f) / 0.3f;
                    scaleT = Mathf.Lerp(1.25f, 1f, subT);
                }

                tr.localScale = baseScale * scaleT;
                tr.localPosition = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            tr.localScale = baseScale;
            tr.localPosition = endPos;
        }

        private System.Collections.IEnumerator AnimateDishFlyToWarehouse(System.Action onDone)
        {
            if (_imgPlateDish == null)
            {
                onDone?.Invoke();
                yield break;
            }

            // [FIX 2026-09-11] Âm thanh ting ting vui tai khi món bay vào kho theo yêu cầu của Sếp!
            AudioManager.Instance?.PlayCoinTing();
            AudioManager.Instance?.PlayGemSparkle();

            Transform tr = _imgPlateDish.transform;
            Vector3 startPos = tr.position;
            Vector3 targetPos = startPos + new Vector3(250f, 120f, 0f);

            Transform wh = (_txtSentCount != null ? _txtSentCount.transform.parent : null)
                ?? (_root != null ? _root.Find("Warehouse_Box") : null)
                ?? transform.Find("Warehouse_Box")
                ?? transform.Find("Top_Bar/Btn_Warehouse");
            if (wh != null) targetPos = wh.position;

            float duration = 0.45f;
            float elapsed = 0f;
            Vector3 startScale = tr.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Đường bay cánh cung parabol mềm mại
                float height = Mathf.Sin(t * Mathf.PI) * 70f;
                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += height;

                tr.position = current;
                tr.localScale = Vector3.Lerp(startScale, startScale * 0.35f, t);

                yield return null;
            }

            _imgPlateDish.gameObject.SetActive(false);
            tr.localPosition = _plateDishPos0;
            tr.localScale = _plateDishScale0;

            onDone?.Invoke();
        }

        private void OnPlatingClicked()
        {
            if (_btnPlating != null) _btnPlating.interactable = false;
            StartCoroutine(AnimateDishFlyToWarehouse(() =>
            {
                challenge?.CollectCookedDishToWarehouse();
                RefreshDynamic();
            }));
        }

        private void OnClearAllClicked()
        {
            if (selection == null) return;
            selection.ResetSelection();
            selection.ResetFlavor();
            selection.EnableIngredientSelection();
            RefreshDynamic();
        }

        private void SelectDish(DishData dish)
        {
            if (dish == null || challenge == null) return;
            challenge.SetCurrentDish(dish);
            OnClearAllClicked();
            ShowBoardDetail(true);
            RefreshAll();
        }

        private void ShowBoardDetail(bool detail)
        {
            if (_boardDetail != null) _boardDetail.SetActive(detail);
            if (_boardList != null)  _boardList.SetActive(!detail);
            if (!detail)
            {
                RebuildDishList();
                // [FIX 2026-09-24] Luot dau vao bep chu ten mon chua hien (phai bam mon roi quay lai moi hien).
                // Lam moi lai danh sach sau 1 khung hinh + sau 0.25s (luc layout / font / bo dich da xong).
                if (Application.isPlaying && isActiveAndEnabled)
                {
                    if (_coLamMoiDs != null) StopCoroutine(_coLamMoiDs);
                    _coLamMoiDs = StartCoroutine(CoLamMoiDanhSach());
                }
            }
        }

        private Coroutine _coLamMoiDs;
        private System.Collections.IEnumerator CoLamMoiDanhSach()
        {
            yield return null;
            if (_boardList != null && _boardList.activeInHierarchy) { Canvas.ForceUpdateCanvases(); RebuildDishList(); }
            yield return new WaitForSecondsRealtime(0.25f);
            if (_boardList != null && _boardList.activeInHierarchy) { Canvas.ForceUpdateCanvases(); RebuildDishList(); }
            _coLamMoiDs = null;
        }

        /// <summary>[2026-09-23] Danh sach mon xep theo cap mo khoa tang dan (giu thu tu goc khi cung cap).</summary>
        private List<DishData> MonTheoCap()
        {
            var ds = new List<DishData>();
            if (dishBook == null || dishBook.allDishes == null) return ds;
            for (int i = 0; i < dishBook.allDishes.Count; i++) if (dishBook.allDishes[i] != null) ds.Add(dishBook.allDishes[i]);
            var goc = new Dictionary<DishData, int>();
            for (int i = 0; i < ds.Count; i++) goc[ds[i]] = i;
            ds.Sort((a, b) => a.unlockLevel != b.unlockLevel ? a.unlockLevel.CompareTo(b.unlockLevel) : goc[a].CompareTo(goc[b]));
            return ds;
        }

        private void PickDefaultDish()
        {
            if (challenge == null) return;

            // [BUBBLE DON HANG 2026-09-24] Vao bep tu nut "Nau ngay" cua bubble khach du lich
            // -> chon san dung mon khach doi (phieu tu het han sau 120s).
            string monDat = KitchenOrderRequest.LayVaXoa();
            if (!string.IsNullOrEmpty(monDat) && !challenge.IsCooking && dishBook != null && dishBook.allDishes != null)
                foreach (var d in dishBook.allDishes)
                    if (d != null && d.dishId == monDat) { challenge.SetCurrentDish(d); return; }

            if (challenge.CurrentDish != null) return;
            if (dishBook == null || dishBook.allDishes == null) return;

            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            foreach (var d in dishBook.allDishes)
                if (d != null && d.unlockLevel <= lv) { challenge.SetCurrentDish(d); return; }

            // Scene bếp chạy riêng không có PlayerProgressManager (level=1) → không món nào "mở"
            // → fallback lấy món đầu tiên, tránh màn hình trống "—" 0/0 (bug thấy từ screenshot Sếp).
            foreach (var d in dishBook.allDishes)
                if (d != null) { challenge.SetCurrentDish(d); return; }
        }

        // ── Refresh ────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshStatic();
            RefreshDynamic();
            Loc.RequestRescan(); // chip/nhan vua dung lai ⇒ interceptor dich ngay khung sau
        }

        private void RefreshStatic()
        {
            var frontTourist = TouristVisitorManager.Instance != null ? TouristVisitorManager.Instance.GetFrontWaitingTourist() : null;
            DishData orderDish = null;
            Sprite customerAvatar = null;
            int orderGold = 0;
            int orderExp = 0;

            if (frontTourist != null && frontTourist.Dish != null)
            {
                orderDish = frontTourist.Dish;
                customerAvatar = frontTourist.GetAvatarSprite();
                bool fb;
                var boatCfg = (BoatDockManager.Instance != null ? BoatDockManager.Instance.Config : (TouristVisitorManager.Instance != null ? TouristVisitorManager.Instance.Config : null));
                orderGold = TouristRewardCalculator.ComputeGold(orderDish, boatCfg, out fb);
                orderExp = TouristRewardCalculator.ComputeExp(orderDish, boatCfg);
            }
            else
            {
                orderDish = challenge != null ? challenge.CurrentDish : null;
                if (orderDish != null)
                {
                    orderGold = orderDish.rewardGold;
                    orderExp = orderDish.rewardExp;
                }
            }

            if (_imgCustomerAvatar != null)
            {
                _imgCustomerAvatar.sprite = customerAvatar;
                _imgCustomerAvatar.enabled = customerAvatar != null;
            }

            SetText(_txtOrderName, orderDish != null ? Loc.T(orderDish.dishName) : Loc.T(frontTourist != null ? "Đang chọn món..." : "Chưa có khách chờ"));
            if (_imgOrderIcon != null)
            {
                _imgOrderIcon.sprite  = orderDish != null ? orderDish.dishSprite : null;
                _imgOrderIcon.enabled = _imgOrderIcon.sprite != null;
            }
            if (_txtOrderRewards != null)
            {
                _txtOrderRewards.text = orderDish != null
                    ? $"<color=#FFD34D>+{orderGold} 🪙</color>  <color=#5DD6FF>+{orderExp} ⭐</color>"
                    : "";
            }

            if (orderDish != null)
            {
                var f = orderDish.targetFlavor;
                var chipVals = new[] { f.sweet, f.spicy, f.sour, f.umami, f.texture };
                for (int i = 0; i < 5; i++) SetText(_orderChipValues[i], chipVals[i].ToString());
            }
            else
            {
                for (int i = 0; i < 5; i++) SetText(_orderChipValues[i], "");
            }

            var dish = challenge != null ? challenge.CurrentDish : null;
            if (dish != null || orderDish != null)
            {
                SyncOrderBannerCozy(dish ?? orderDish);
            }

            SetText(_txtDishName, dish != null ? Loc.T(dish.dishName) : "—");
            if (_imgDishIcon != null)
            {
                _imgDishIcon.sprite  = dish != null ? dish.dishSprite : null;
                _imgDishIcon.enabled = _imgDishIcon.sprite != null;
            }
            SetText(_txtDishMeta, dish != null ? Loc.TF("{0} · Cấp {1}", DiffName(dish.difficulty), dish.unlockLevel) : "");
            SetText(_txtRewards, dish != null ? Loc.TF("+{0} vàng   +{1} EXP   Bán {2}", dish.rewardGold, dish.rewardExp, dish.sellPrice) : "");

            // Chip nguyên liệu cần (Xếp vào Grid: tối đa 4 thẻ/hàng, từ 5 thẻ tự động xuống hàng 2)
            if (_needChipsRoot != null && dish != null)
            {
                EnsureNeedChipsGrid(_needChipsRoot); // [FIX 2026-09-02] null-safe, không throw

                // [2026-09-23] O nguyen lieu la object THAT trong Hierarchy ("Chip_Slot_0..n", tool Kitchen V3/11
                // dung san) — Sep chinh tay duoc, Play chi thay icon + ten va bat/tat theo so nguyen lieu.
                // Chi xoa o cu kieu "Chip_<id>" do ban truoc tao luc chay.
                for (int i = _needChipsRoot.childCount - 1; i >= 0; i--)
                {
                    var con = _needChipsRoot.GetChild(i);
                    if (con.name.StartsWith("Chip_") && !con.name.StartsWith("Chip_Slot_")) Destroy(con.gameObject);
                }
                int soCan = 0;
                if (dish.requiredIngredients != null)
                    foreach (var ing in dish.requiredIngredients) if (ing != null) soCan++;
                for (int i = 0; ; i++)
                {
                    var o = _needChipsRoot.Find("Chip_Slot_" + i);
                    if (o == null) break;
                    if (i >= soCan && o.gameObject.activeSelf) o.gameObject.SetActive(false);
                }

                if (dish.requiredIngredients != null)
                {
                    int count = dish.requiredIngredients.Count;
                    bool twoRows = count >= 5;

                    var rtChips = (RectTransform)_needChipsRoot;
                    if (!KhoaLayout) rtChips.sizeDelta = new Vector2(285f, twoRows ? 136f : 66f);

                    if (_txtTasteTitle != null)
                        if (!KhoaLayout) _txtTasteTitle.rectTransform.anchoredPosition = new Vector2(12f, twoRows ? -276f : -206f);

                    if (!KhoaLayout)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float y = twoRows ? (-300f - i * 27f) : (-230f - i * 34f);
                            _flavorRows[i].SetY(y);
                        }
                    }

                    int k = 0;
                    foreach (var ing in dish.requiredIngredients)
                    {
                        if (ing == null) continue;
                        var o = _needChipsRoot.Find("Chip_Slot_" + k);
                        if (o == null) o = MakeNeedChip(_needChipsRoot, ing, 66f, 64f, 38f, 11, "Chip_Slot_" + k);
                        GanOChip(o, ing);
                        k++;
                    }
                }
            }

            // Bảng đen món hôm nay
            var daily = DailySpecialManager.Instance;
            if (daily != null && _txtChalk != null)
            {
                // [2026-09-23] Tieu de "TODAY'S SPECIAL" da nam tren bang => bo dong tieu de lap lai,
                // chi liet ke mon (moi mon 1 dong, to, can giua). Gan truc tiep, khong qua LocFit.
                var sb = new System.Text.StringBuilder();
                foreach (var d in daily.TodayDishes)
                    if (d != null) { if (sb.Length > 0) sb.Append('\n'); sb.Append("• ").Append(Loc.T(d.dishName)); }
                string chuBang = sb.ToString();
                if (_txtChalk.text != chuBang) _txtChalk.text = chuBang;
            }
        }

        /// <summary>
        /// Đồng bộ dữ liệu món ăn vào thanh Order_Banner theo phong cách Cozy V3 (ảnh mẫu Tomato Pasta).
        /// </summary>
        private void SyncOrderBannerCozy(DishData activeDish)
        {
            if (activeDish == null) return;

            Transform bannerT = _root != null ? _root.Find("Order_Banner") : null;
            if (bannerT == null) return;
            Transform cardT = bannerT.Find("Order_Card") ?? bannerT;

            // 1. Đĩa món ăn
            var imgDish = cardT.Find("Img_Dish")?.GetComponent<Image>() ?? _imgOrderIcon;
            if (imgDish != null && activeDish.dishSprite != null)
            {
                imgDish.sprite = activeDish.dishSprite;
                imgDish.enabled = true;
                imgDish.preserveAspect = true;
                // Img_Dish trong scene de alpha 0.001 (placeholder) => dia mon vo hinh khi Play.
                if (imgDish.color.a < 0.99f) imgDish.color = Color.white;
            }

            // 2. Tên món ăn
            var txtTitle = cardT.Find("Txt_Name")?.GetComponent<TMP_Text>() ?? _txtOrderName;
            if (txtTitle != null)
            {
                txtTitle.text = Loc.T(activeDish.dishName);
            }

            // 3. Dãy nguyên liệu yêu cầu (icon + 0/1)
            Transform reqGrp = cardT.Find("Group_RequiredIngredients");
            if (reqGrp != null)
            {
                var reqList = activeDish.requiredIngredients;
                int count = reqList != null ? reqList.Count : 0;
                var selIng = selection != null ? selection.GetSelectedIngredientCards() : null;

                for (int i = 0; i < 4; i++)
                {
                    Transform slot = reqGrp.Find($"Slot_{i}");
                    if (slot == null) continue;

                    if (i < count && reqList[i] != null)
                    {
                        var ing = reqList[i];
                        slot.gameObject.SetActive(true);

                        var iconImg = slot.Find("Img_Icon")?.GetComponent<Image>();
                        if (iconImg != null)
                        {
                            iconImg.sprite = ing.icon;
                            iconImg.enabled = ing.icon != null;
                        }

                        var qtyTxt = slot.Find("Txt_Qty")?.GetComponent<TMP_Text>();
                        if (qtyTxt != null)
                        {
                            bool inPot = false;
                            if (selIng != null)
                            {
                                foreach (var c in selIng)
                                {
                                    if (c != null && c.GetIngredientData() == ing)
                                    {
                                        inPot = true;
                                        break;
                                    }
                                }
                            }
                            qtyTxt.text = inPot ? "1/1" : "0/1";
                        }
                    }
                    else
                    {
                        slot.gameObject.SetActive(false);
                    }
                }
            }

            // 4. Thời gian nấu
            Transform timeGrp = cardT.Find("Group_CookingTime");
            if (timeGrp != null)
            {
                var timeTxt = timeGrp.Find("Txt_Time")?.GetComponent<TMP_Text>();
                if (timeTxt != null)
                {
                    timeTxt.text = "2m";
                }
            }
        }

        private void RefreshDynamic()
        {
            // Số lượng kho bếp trên thẻ
            RefreshCardQuantities();

            var dish   = challenge != null ? challenge.CurrentDish : null;
            if (dish != null)
            {
                SyncOrderBannerCozy(dish);
            }
            var selIng = selection != null ? selection.GetSelectedIngredientCards() : null;
            var selSea = selection != null ? selection.GetSelectedSeasoningCards() : null;
            int nIng = CountNonNull(selIng);
            int nSea = CountNonNull(selSea);

            SetText(_txtTabIng, Loc.TF("Nguyên liệu  {0}/4", nIng));
            SetText(_txtTabSea, Loc.TF("Gia vị  {0}/3", nSea));

            // 5 thanh vị + điểm dự kiến
            if (dish != null)
            {
                FlavorVector cur = FlavorVector.Zero;
                if (selIng != null) cur += CookingScoreCalculator.SumVectorsFromCards(selIng);
                if (selSea != null) cur += CookingScoreCalculator.SumVectorsFromCards(selSea);
                var tgt = dish.targetFlavor;

                SetFlavorRow(0, "Ngọt",    cur.sweet,   tgt.sweet);
                SetFlavorRow(1, "Cay",     cur.spicy,   tgt.spicy);
                SetFlavorRow(2, "Chua",    cur.sour,    tgt.sour);
                SetFlavorRow(3, "Đậm",     cur.umami,   tgt.umami);
                SetFlavorRow(4, "Kết cấu", cur.texture, tgt.texture);

                if (nIng + nSea > 0 && selIng != null && selSea != null)
                {
                    var result = CookingScoreCalculator.Evaluate(dish, selIng, selSea);
                    SetText(_txtProjection, Loc.TF("Điểm dự kiến:  {0}đ", result.finalScore));
                }
                else SetText(_txtProjection, Loc.T("Điểm dự kiến:  — đ"));
            }
            else
            {
                SetText(_txtProjection, Loc.T("Điểm dự kiến:  — đ"));
            }

            // Nút hành động 3 trạng thái + bàn trình bày
            bool plateReady = challenge != null && challenge.CookedDishOnPlate != null;
            bool cooking    = (challenge != null && challenge.IsCooking) || _ovenBusy;

            EnsurePlatingDishVisual();
            if (_btnPlating != null) _btnPlating.interactable = plateReady;
            SetText(_txtPlating, Loc.T(plateReady ? "CHẠM ĐỂ CẤT VÀO KHO!" : "Trình bày"));

            if (_imgPlateDish != null)
            {
                if (plateReady)
                {
                    _imgPlateDish.sprite = challenge.CookedDishOnPlate.dishSprite;
                    _imgPlateDish.gameObject.SetActive(true);

                    if (!_dishPopAnimated)
                    {
                        _dishPopAnimated = true;
                        StartCoroutine(AnimateDishPopToPlate());
                    }
                }
                else
                {
                    _dishPopAnimated = false;
                    _imgPlateDish.gameObject.SetActive(false);
                }
            }

            if (_btnAction != null && _imgAction != null)
            {
                bool useSkin = skin.btnGreen != null && skin.btnGray != null;
                if (cooking)
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, Loc.T("ĐANG NẤU..."));
                    SetText(_txtActionSub, "");
                }
                else if (plateReady)
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, Loc.T("MÓN TRÊN DĨA"));
                    SetText(_txtActionSub, Loc.T("cất vào kho trước đã"));
                }
                else if (nIng + nSea > 0)
                {
                    _btnAction.interactable = true;
                    ApplyActionSkin(useSkin, true);
                    SetText(_txtAction, Loc.T("NẤU!"));
                    SetText(_txtActionSub, Loc.TF("{0} nguyên liệu · {1} gia vị", nIng, nSea));
                }
                else
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, Loc.T("CHỌN NGUYÊN LIỆU"));
                    SetText(_txtActionSub, Loc.T("chạm khay bên dưới"));
                }
                PunchChangedCards();
            }
        }

        private void ApplyActionSkin(bool useSkin, bool green)
        {
            if (_imgAction == null) return;
            if (useSkin)
            {
                _imgAction.sprite = green ? skin.btnGreen : skin.btnGray;
                // [V3] Nut cua Sep la anh nguyen khoi (Simple + preserveAspect). Ep Sliced la meo.
                // V3 giu nguyen Image.Type da dat trong Hierarchy; trang thai xam dung tint mau.
                if (!boQuaDongBoSkinShop) _imgAction.type = Image.Type.Sliced;
                _imgAction.color = (boQuaDongBoSkinShop && !green) ? new Color(0.72f, 0.72f, 0.72f) : Color.white;
            }
            else
            {
                _imgAction.sprite = null;
                _imgAction.color = green ? new Color(0.36f, 0.72f, 0.26f)
                                         : new Color(0.58f, 0.55f, 0.50f);
            }
        }

        private void RefreshCardQuantities()
        {
            var ktm = KitchenTransferManager.Instance;
            if (ktm == null) return;

            var items = ktm.GetTransferredItems();
            var map = new Dictionary<string, int>();
            if (items != null)
                foreach (var kv in items)
                    if (!string.IsNullOrEmpty(kv.Key))
                    {
                        // Khoá kho là itemId NÔNG TRẠI (vd chicken_meat/nam) → dịch sang id BẾP
                        // (chicken/mushroom) bằng đúng bảng CookingBoot đang dùng. Id trùng thì giữ nguyên.
                        string kitchenKey = KitchenIdMap.ToKitchen(kv.Key);
                        map[kitchenKey] = map.TryGetValue(kitchenKey, out int cur) ? cur + kv.Value : kv.Value;
                    }

            // Thẻ ĐANG CHỌN đã trừ tạm 1 trên hiển thị (TrySelect) — poll không được cộng ngược lại.
            var selIngQ = selection != null ? selection.GetSelectedIngredientCards() : null;
            var selSeaQ = selection != null ? selection.GetSelectedSeasoningCards() : null;

            // Sếp 2026-08-27 (chốt qua hỏi đáp): khay CHỈ hiện món đã gửi vào bếp — thẻ x0 ẩn đi
            // (trừ thẻ đang chọn: còn nằm trong nồi thì phải thấy). GridLayout tự dồn ô khi ẩn.
            int visibleIng = 0, visibleSea = 0;
            foreach (var kv in _cards)
            {
                int raw = map.TryGetValue(kv.Key, out int v) ? v : 0;
                bool isSel = (selIngQ != null && selIngQ.Contains(kv.Value))
                          || (selSeaQ != null && selSeaQ.Contains(kv.Value));
                kv.Value.SetQuantityFromKitchen(Mathf.Max(raw - (isSel ? 1 : 0), 0));

                bool show = raw > 0 || isSel;
                var go = kv.Value.gameObject;
                if (go.activeSelf != show) go.SetActive(show);
                if (show)
                {
                    if (kv.Value.isSeasoning) visibleSea++;
                    else visibleIng++;
                }
            }
            UpdateTrayEmptyHints(visibleIng, visibleSea);
        }

        /// <summary>Khay trống → chỉ đường cho người chơi cách gửi nguyên liệu từ Kho nông trại.</summary>
        private const string HintEmptyIngVi = "Khay trống — về nông trại mở KHO,\nchọn nguyên liệu rồi bấm GỬI BẾP nhé!";
        private const string HintEmptySeaVi = "Chưa có gia vị — về nông trại mở KHO,\nchọn gia vị rồi bấm GỬI BẾP nhé!";

        private void UpdateTrayEmptyHints(int nIng, int nSea)
        {
            if (_hintEmptyIng == null && _gridIngredients != null)
                _hintEmptyIng = MakeTrayEmptyHint(_gridIngredients, HintEmptyIngVi);
            if (_hintEmptySea == null && _gridSeasonings != null)
                _hintEmptySea = MakeTrayEmptyHint(_gridSeasonings, HintEmptySeaVi);

            if (_hintEmptyIng != null && _hintEmptyIng.gameObject.activeSelf != (nIng == 0))
                _hintEmptyIng.gameObject.SetActive(nIng == 0);
            if (_hintEmptySea != null && _hintEmptySea.gameObject.activeSelf != (nSea == 0))
                _hintEmptySea.gameObject.SetActive(nSea == 0);

            // [2026-09-23] Khay trong: chi hien dong chi duong. O trong + nut "Mo 7 o" nam chung
            // cho voi dong chu => de len nhau. Co hang roi thi hien lai o trong + nut mua.
            AnHienOTrong(_gridIngredients, nIng > 0);
            AnHienOTrong(_gridSeasonings, nSea > 0);
        }

        private static void AnHienOTrong(Transform grid, bool hien)
        {
            if (grid == null) return;
            for (int i = 0; i < grid.childCount; i++)
            {
                var c = grid.GetChild(i);
                if (!c.name.StartsWith("Slot_Empty_") && c.name != "Btn_BuySlots") continue;
                if (c.gameObject.activeSelf != hien) c.gameObject.SetActive(hien);
            }
        }

        /// <summary>
        /// So luong phai nam TRONG o nau Qty_Badge (scene cu de Txt_Quantity ngang hang, 200x50 giua the
        /// => chu "x0" roi ra ngoai). Chuyen vao badge + keo gian vua o. Da dung cho roi thi khong lam gi.
        /// </summary>
        private static void ChuanHoaSoLuongThe(Transform card)
        {
            if (card == null) return;
            var badge = card.Find("Qty_Badge");
            if (badge == null) return;
            var qty = badge.Find("Txt_Quantity") ?? card.Find("Txt_Quantity");
            if (qty == null) return;
            if (qty.parent != badge) qty.SetParent(badge, false);
            var rt = (RectTransform)qty;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = qty.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.enableAutoSizing = true; t.fontSizeMin = 9f; t.fontSizeMax = 15f;
                t.raycastTarget = false;
            }
        }

        private TMP_Text MakeTrayEmptyHint(Transform grid, string message)
        {
            // Treo lên gốc Scroll (ông của grid) để không bị GridLayout xếp như một ô thẻ.
            Transform host = grid.parent != null && grid.parent.parent != null ? grid.parent.parent : grid;

            var existed = host.Find("Txt_EmptyHint");
            if (existed != null) return existed.GetComponent<TMP_Text>();

            var go = new GameObject("Txt_EmptyHint", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            ApplyFont(t);
            t.text = Loc.T(message);
            t.fontSize = 14;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.55f, 0.42f, 0.28f);
            t.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(20f, 8f);
            rt.offsetMax = new Vector2(-20f, -8f);
            go.SetActive(false);
            return t;
        }

        private void SetFlavorRow(int i, string label, int cur, int target)
        {
            var row = _flavorRows[i];
            if (row.label == null) return;

            SetText(row.label, Loc.T(label));
            float barMax = Mathf.Max(target * 1.5f, target + 2f, 1f);
            if (row.fill != null)
            {
                row.fill.fillAmount = Mathf.Clamp01(cur / barMax);
                bool over = target > 0 && cur > target;
                bool hit  = target > 0 && cur == target;
                row.fill.color = over ? new Color(0.85f, 0.45f, 0.15f)
                       : hit  ? new Color(0.30f, 0.70f, 0.20f)
                              : new Color(0.55f, 0.75f, 0.35f);
            }
            // Sếp 2026-08-27: bỏ vạch đỏ (đè lên thanh) — fill bar + số cur/target là đủ.
            if (row.marker != null)
                row.marker.gameObject.SetActive(false);
            if (row.value != null) SetText(row.value, $"{cur}/{target}");
        }

        // ── Helpers ────────────────────────────────────────────────

        private static string DiffName(DishDifficulty d) =>
            Loc.T(d == DishDifficulty.Easy ? "Dễ" : d == DishDifficulty.Hard ? "Khó" : "Vừa");

        private static int CountNonNull(List<SelectableIngredientCard> list)
        {
            if (list == null) return 0;
            int n = 0;
            foreach (var c in list) if (c != null) n++;
            return n;
        }

        /// <summary>
        /// [2026-09-21] Gan chu + tu vua khung. Chi ghi khi chu THAT SU doi (RefreshDynamic chay moi
        /// pollInterval; ghi lai chu cu moi lan se danh nhau voi LocRuntimeInterceptor va lam TMP
        /// do lai layout vo ich). Chu tieng Anh do Loc.T() tra ve dai hon ⇒ LocFit.Fit thu nho cho vua.
        /// </summary>
        // Co chu THIET KE cua tung nhan (chup lan dau) — de DatChuGiuCo khong bao gio lam nho dan.
        private readonly Dictionary<TMP_Text, float> _coChuGoc = new Dictionary<TMP_Text, float>();

        /// <summary>
        /// Gan chu nhung GIU co chu Edit mode. Chi thu nho khi chu rong hon khung (toi da con 70%),
        /// khong xuong dong, khong cat mat chu. Dung cho the da dong bang (KhoaLayout).
        /// </summary>
        private void DatChuGiuCo(TMP_Text t, string s)
        {
            if (t == null || s == null) return;
            LocRuntimeInterceptor.HoanVuaKhung(t);
            if (!_coChuGoc.TryGetValue(t, out float co)) { co = t.fontSize > 0f ? t.fontSize : 16f; _coChuGoc[t] = co; }
            t.enableAutoSizing = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode     = TextOverflowModes.Overflow;
            if (t.text != s) t.text = s;
            t.fontSize = co;
            float rong = t.rectTransform.rect.width;
            if (rong > 1f)
            {
                float can = t.GetPreferredValues(s).x;
                if (can > rong) t.fontSize = Mathf.Max(co * 0.7f, co * rong / can);
            }
        }

        private static void SetText(TMP_Text t, string s)
        {
            if (t == null || s == null) return;
            if (t.text == s) return;
            t.text = s;
            // [2026-09-23] Da dong bang UI (KhoaLayout) => co chu/khung la cua Sep chinh o Edit mode,
            // KHONG de LocFit bop nho (ly do chu Play nho xiu/mat chu so voi Edit).
            if (KhoaLayout) return;
            LocFit.Fit(t);
        }

        /// <summary>Nâng sorting canvas của popup CŨ lên trên UI v2 (runtime, không sửa scene).</summary>
        // [DỌN 2026-08-31] Đã xoá hẳn 2 minigame (LetterMiniGame + CookingTimingMiniGameUI)
        // theo lệnh Sếp 27/08 — luồng nấu đi thẳng, không còn minigame chen giữa.
        // Backup: production/backup_xoa_minigame_2026-08-31/
        private void RaiseLegacyOverlays()
        {
            RaiseOne(FindFirstObjectByType<CookingPopupController>(FindObjectsInactive.Include));
        }

        private void RaiseOne(Component c)
        {
            if (c == null) return;
            var cv = c.GetComponent<Canvas>();
            if (cv == null) cv = c.gameObject.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = canvasSortingOrder + 60;
            if (c.GetComponent<GraphicRaycaster>() == null)
                c.gameObject.AddComponent<GraphicRaycaster>();
        }

        // ══════════════════════════════════════════════════════════
        //  BUILD UI (runtime, idempotent) — skin tạm K1, K2 thay asset sprite-forge
        // ══════════════════════════════════════════════════════════

        private void EnsureBuilt()
        {
            if (_built) return;

            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = canvasSortingOrder;
            if (GetComponent<CanvasScaler>() == null)
            {
                var sc = gameObject.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1600f, 900f);
                sc.matchWidthOrHeight = 0.5f;
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            _root = (RectTransform)transform;

            // Nền bếp (tường kem + sàn caro tạm bằng 2 mảng màu)
            var wall = MakePanel(_root, "BG_Wall", new Color(0.93f, 0.87f, 0.74f));
            Stretch(wall, 0f, 0.32f, 1f, 1f);
            SkinTiled(wall, skin.wallTile);
            var floor = MakePanel(_root, "BG_Floor", new Color(0.82f, 0.66f, 0.45f));
            Stretch(floor, 0f, 0f, 1f, 0.32f);
            SkinTiled(floor, skin.floorTile);

            // BuildTopBar() ĐÃ BỎ theo lệnh Sếp 2026-08-26 — avatar + ví vàng dư thừa
            // (vàng/EXP đã hiển thị trên HUD chính của game khi quay ra farm).
            BuildOrderBanner();
            BuildRecipeBoard();
            BuildStage();
            BuildTray();
            BuildActionButton();
            BuildBackFarmButton();

            _built = true;
        }

        /// <summary>Dung lai CHI nhung nhanh bi thieu trong Hierarchy (Tray, Btn_Action,
        /// Btn_BackFarm, Cat_Chef). Khong dung den cac nhanh da co -> chinh sua tay duoc giu.
        /// Trong luc va phai MO khoa layout, neu khong object moi sinh ra se khong co anchor.</summary>
        /// <summary>
        /// [FIX 2026-09-22 — LOI NANG NHAT] SafeAreaBootstrap boc TOAN BO con cua canvas vao
        /// mot lop "~SafeArea" luc chay, va no chay o RuntimeInitializeLoadType.AfterSceneLoad
        /// tuc la XONG TRUOC Start() cua lop nay. Sau khi boc, "Order_Banner" khong con la con
        /// TRUC TIEP cua canvas nua. Ma Start() lai kiem tra dung
        ///     transform.Find("Order_Banner") != null
        /// -> luon tra ve null -> chay nhanh RebuildNow() -> XOA SACH con (ke ca "~SafeArea")
        /// roi dung lai bang code. Do la ly do that su vi sao moi lan Sep bam Play la toan bo
        /// chinh sua tay bay mat, du da bat "Khoa layout".
        /// Ham nay tra ve dung khung goc de moi phep Find/ dung moi deu di qua lop boc neu co.
        /// </summary>
        private RectTransform KhungGoc()
        {
            var boc = transform.Find("~SafeArea") as RectTransform;
            return boc != null ? boc : (RectTransform)transform;
        }

        private void VaPhanThieuTrongHierarchy()
        {
            // [2026-09-22] "Thieu" khong chi la KHONG CO. Lan chay truoc nem exception giua
            // chung BuildTray (Loc.T goi DontDestroyOnLoad ngoai Play mode) nen co the con lai
            // mot 'Tray' DUNG DO: co vo nhung thieu luoi ben trong. Neu de nguyen, lan chay sau
            // thay Tray != null se bo qua va Sep giu mai cai khay hong. Nen: phat hien khay hong
            // thi XOA DUNG NO roi dung lai. Day la truong hop DUY NHAT tool nay duoc phep xoa.
            _root = KhungGoc();   // dung trong "~SafeArea" neu lop boc da ton tai

            // Lan chay hong truoc co the de lai 'Tray' NGOAI lop boc -> xoa cho sach.
            if (_root != transform)
            {
                var lac = transform.Find("Tray");
                if (lac != null) CatSangMotBen(lac, "lac ngoai ~SafeArea");
            }

            var trayCu = _root.Find("Tray");
            if (trayCu != null &&
                (trayCu.Find("Scroll_Grid_Ingredients/Viewport/Grid_Ingredients") == null ||
                 trayCu.Find("Scroll_Grid_Seasonings/Viewport/Grid_Seasonings")  == null))
            {
                CatSangMotBen(trayCu, "dung do — thieu luoi ben trong");
                _gridIngredients = null; _gridSeasonings = null;
                _tabIngredients = null;  _tabSeasonings = null;
                _txtTabIng = null;       _txtTabSea = null;
                _btnClearAll = null;
            }

            bool thieuTray   = _root.Find("Tray")         == null;
            bool thieuAction = _root.Find("Btn_Action")   == null;
            bool thieuBack   = _root.Find("Btn_BackFarm") == null;
            // V3 dung meo dung yen "Cat_Chef_Idle" (Sep dat tay) => KHONG sinh them meo di bo thu 2 luc Play.
            bool thieuCat    = _root.Find("Cat_Chef") == null && _root.Find("Cat_Chef_Idle") == null;
            bool thieuDeco   = _root.Find("Deco_Garlic_R") == null || _root.Find("Deco_Onion_R")  == null
                            || _root.Find("Deco_Herbs_R")  == null || _root.Find("Deco_Herbs_L")  == null
                            || _root.Find("Deco_Garlic_L") == null || _root.Find("Deco_Lights_L") == null
                            || _root.Find("Deco_Lights_R") == null;
            var whBox = _root.Find("Warehouse_Box");
            bool thieuSent = whBox != null && whBox.Find("Txt_Sent") == null;

            if (!thieuTray && !thieuAction && !thieuBack && !thieuCat && !thieuDeco && !thieuSent) return;

            bool khoaCu = KhoaLayout;
            KhoaLayout = false;                 // object moi bat buoc phai duoc anchor
            try
            {
                if (thieuSent && whBox != null) VaTxtSent(whBox);
                if (thieuDeco)   { VaTrangTri();           Debug.LogWarning("[KitchenV2] Va lai bo trang tri treo tuong (toi, hanh, thao moc, den day)."); }
                if (thieuCat)    { VaCatChef(); }
                if (thieuTray)   { BuildTray();            Debug.LogWarning("[KitchenV2] Va lai 'Tray' (khay nguyen lieu + gia vi) bi thieu trong Hierarchy."); }
                if (thieuAction) { BuildActionButton();    Debug.LogWarning("[KitchenV2] Va lai 'Btn_Action' bi thieu trong Hierarchy."); }
                if (thieuBack)   { BuildBackFarmButton();  Debug.LogWarning("[KitchenV2] Va lai 'Btn_BackFarm' bi thieu trong Hierarchy."); }
            }
            finally { KhoaLayout = khoaCu; }
        }

        /// <summary>
        /// Dong "Da gui N mon" duoi hop kho. Day la dong DAU TIEN trong chuoi dung goi Loc.TF,
        /// va cung la dong ma exception DontDestroyOnLoad tung giet chuoi dung o Edit Mode —
        /// moi thu sinh ra SAU no (trang tri, meo dau bep, khay, 2 nut) deu bien mat theo.
        /// </summary>
        private void VaTxtSent(Transform wh)
        {
            _txtSentCount = MakeText(wh, "Txt_Sent", Loc.TF("Đã gửi {0} món", PlayerPrefs.GetInt(SentCountKey, 0)), 14, new Color(0.99f, 0.96f, 0.88f));
            Anchor(_txtSentCount.rectTransform, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(176f, 22f), new Vector2(0.5f, 0f));
            _txtSentCount.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// KHONG XOA, chi doi ten + tat di roi day xuong cuoi. Lenh cua Sep: co gi sai thi phai
        /// con duong lui. Object bi cat sang mot ben van nam trong Hierarchy de Sep xem lai
        /// (hoac lay lai thu da keo tay trong do); thay khong can thi tu xoa bang tay.
        /// </summary>
        private void CatSangMotBen(Transform t, string lyDo)
        {
            if (t == null) return;
            t.name = t.name + "_CU_" + System.DateTime.Now.ToString("HHmmss");
            t.gameObject.SetActive(false);
            t.SetAsLastSibling();
            Debug.LogWarning($"[KitchenV2] '{t.name}' ({lyDo}) — da TAT va doi ten, KHONG xoa. Xem lai roi tu xoa neu khong can.", t);
        }

        /// <summary>Bo trang tri treo tuong — copy nguyen ven tu BuildStage(), chi dung cai nao con thieu.</summary>
        private void VaTrangTri()
        {
            void D(string ten, Sprite sp, Vector2 pos, Vector2 size)
            {
                if (_root.Find(ten) != null) return;
                MakeDecor(_root, ten, sp, 0.5f, 1f, pos, size, new Vector2(0.5f, 1f));
            }
            D("Deco_Garlic_R", skin.decorGarlic, new Vector2(330f, -4f),  new Vector2(48f, 82f));
            D("Deco_Onion_R",  skin.decorOnion,  new Vector2(392f, -6f),  new Vector2(50f, 86f));
            D("Deco_Herbs_R",  skin.decorHerbs,  new Vector2(456f, -4f),  new Vector2(52f, 78f));
            D("Deco_Herbs_L",  skin.decorHerbs,  new Vector2(-540f, -4f), new Vector2(52f, 78f));
            D("Deco_Garlic_L", skin.decorGarlic, new Vector2(-478f, -6f), new Vector2(48f, 82f));
            D("Deco_Lights_L", skin.decorLights, new Vector2(-360f, 0f),  new Vector2(280f, 40f));
            D("Deco_Lights_R", skin.decorLights, new Vector2(520f, 0f),   new Vector2(300f, 40f));
        }

        private void VaCatChef()
        {
            if (skin == null || skin.catChefWalk == null || skin.catChefWalk.Length == 0 || skin.catChefWalk[0] == null) return;
            var catGo = new GameObject("Cat_Chef", typeof(RectTransform), typeof(Image), typeof(KitchenCatWalker));
            catGo.transform.SetParent(_root, false);
            var ci = catGo.GetComponent<Image>();
            ci.sprite = skin.catChefWalk[0]; ci.preserveAspect = true; ci.raycastTarget = false;
            Anchor((RectTransform)catGo.transform, 0.5f, 0.5f, new Vector2(-100f, -84f), new Vector2(96f, 88f), new Vector2(0.5f, 0.5f));
            var walker = catGo.GetComponent<KitchenCatWalker>();
            walker.frames = skin.catChefWalk;
            walker.minX = -280f; walker.maxX = 260f;
            Debug.LogWarning("[KitchenV2] Va lai 'Cat_Chef' bi thieu trong Hierarchy.");
        }

        /// <summary>UI đã nằm sẵn trong Hierarchy (Sếp chỉnh tay tự do, Ctrl+S là vĩnh viễn).
        /// Hàm này chỉ NỐI tham chiếu + gắn listener + dọn nội dung động để runtime sinh lại.
        /// Đổi tên/xóa object trong hierarchy sẽ được log warning rõ ràng, không crash.</summary>
        private void BindExistingHierarchy()
        {
            _canvas = GetComponent<Canvas>();
            _root = KhungGoc();   // [2026-09-22] di qua lop boc "~SafeArea" neu co

            // ── TU VA PHAN THIEU (2026-09-22) ─────────────────────────────────────
            // Su co: Hierarchy da luu de mat Tray / Btn_Action / Btn_BackFarm / Cat_Chef.
            // Vi Order_Banner van con nen Start() chon nhanh Bind -> khay nguyen lieu,
            // gia vi va nut ve nong trai khong bao gio duoc dung lai => man hinh trong.
            // Cach xu ly: chi dung LAI DUNG PHAN THIEU, khong dung ham RebuildNow()
            // (RebuildNow xoa sach con -> mat toan bo chinh sua tay cua Sep).
            VaPhanThieuTrongHierarchy();

            RectTransform F(string path)
            {
                var t = _root.Find(path);
                if (t == null) Debug.LogWarning($"[KitchenV2] Bind: thiếu '{path}' trong hierarchy (bị đổi tên/xóa?)");
                return t as RectTransform;
            }
            TMP_Text FT(string path) { var t = F(path); return t != null ? t.GetComponent<TMP_Text>() : null; }
            Image FI(string path) { var t = F(path); return t != null ? t.GetComponent<Image>() : null; }
            Button FB(string path, UnityEngine.Events.UnityAction fn)
            {
                var t = F(path); if (t == null) return null;
                var b = t.GetComponent<Button>(); if (b == null) return null;
                b.onClick.RemoveAllListeners();
                if (fn != null) b.onClick.AddListener(fn);
                return b;
            }

            // ── Banner đơn khách ──
            var orderCardT = F("Order_Banner/Order_Card");
            if (orderCardT != null)
            {
                var cardRt = orderCardT as RectTransform;
                if (!KhoaLayout && cardRt != null && cardRt.sizeDelta.x < 550f)
                {
                    cardRt.sizeDelta = new Vector2(596f, 72f);
                    var bannerRt = orderCardT.parent as RectTransform;
                    if (bannerRt != null) bannerRt.sizeDelta = new Vector2(620f, 112f);
                }

                var avGo = orderCardT.Find("Img_Avatar");
                if (avGo == null)
                {
                    var aGo = new GameObject("Img_Avatar", typeof(RectTransform), typeof(Image));
                    aGo.transform.SetParent(orderCardT, false);
                    _imgCustomerAvatar = aGo.GetComponent<Image>();
                    _imgCustomerAvatar.preserveAspect = true;
                    _imgCustomerAvatar.raycastTarget = false;
                    Anchor((RectTransform)aGo.transform, 0f, 0.5f, new Vector2(34f, 0f), new Vector2(52f, 52f), new Vector2(0.5f, 0.5f));
                }
                else
                {
                    _imgCustomerAvatar = avGo.GetComponent<Image>();
                }

                _imgOrderIcon  = FI("Order_Banner/Order_Card/Img_Dish");
                if (_imgOrderIcon != null)
                {
                    Anchor(_imgOrderIcon.rectTransform, 0f, 0.5f, new Vector2(92f, 0f), new Vector2(48f, 48f), new Vector2(0.5f, 0.5f));
                }

                _txtOrderName  = FT("Order_Banner/Order_Card/Txt_Name");
                if (_txtOrderName != null)
                {
                    Anchor(_txtOrderName.rectTransform, 0f, 1f, new Vector2(124f, -4f), new Vector2(240f, 26f), new Vector2(0f, 1f));
                }

                var rewGo = orderCardT.Find("Txt_Rewards");
                if (rewGo == null)
                {
                    _txtOrderRewards = MakeText(orderCardT, "Txt_Rewards", "", 15, new Color(0.72f, 0.52f, 0.08f));
                    Anchor(_txtOrderRewards.rectTransform, 0f, 1f, new Vector2(124f, -36f), new Vector2(240f, 22f), new Vector2(0f, 1f));
                }
                else
                {
                    _txtOrderRewards = rewGo.GetComponent<TMP_Text>();
                }

                var oldChips = orderCardT.Find("Txt_Chips"); // bản cũ trước 2026-08-27 — thay bằng chip icon màu
                if (oldChips != null && !DangVaTrongEditor) DestroyImmediate(oldChips.gameObject);
                for (int i = 0; i < 5; i++)
                {
                    var chip = orderCardT.Find($"Chip_{i}");
                    if (chip == null)
                    {
                        BuildOrderChip(orderCardT, i, 368f + i * 44f);
                    }
                    else
                    {
                        Anchor((RectTransform)chip.transform, 0f, 0.5f, new Vector2(368f + i * 44f, 0f), new Vector2(42f, 24f), new Vector2(0f, 0.5f));
                        var vt = chip.Find("Txt_Val");
                        _orderChipValues[i] = vt != null ? vt.GetComponent<TMP_Text>() : null;
                        var dotT = chip.Find("Dot");
                        if (dotT != null)
                        {
                            var dimg2 = dotT.GetComponent<Image>();
                            if (dimg2 != null && dimg2.sprite == null) dimg2.sprite = GetDotSprite();
                        }
                    }
                }

                var cardBtn = orderCardT.GetComponent<Button>() ?? orderCardT.gameObject.AddComponent<Button>();
                cardBtn.onClick.RemoveAllListeners();
                cardBtn.onClick.AddListener(() => {
                    var front = TouristVisitorManager.Instance != null ? TouristVisitorManager.Instance.GetFrontWaitingTourist() : null;
                    if (front != null && front.Dish != null)
                    {
                        SelectDish(front.Dish);
                    }
                });
            }

            // ── Bảng công thức: DETAIL ──
            var det = F("Recipe_Board/Board_Detail");
            _boardDetail = det != null ? det.gameObject : null;
            FB("Recipe_Board/Board_Detail/Btn_OtherDish", () => ShowBoardDetail(false));
            _imgDishIcon   = FI("Recipe_Board/Board_Detail/Img_Dish");
            _txtDishName   = FT("Recipe_Board/Board_Detail/Txt_DishName");
            _txtDishMeta   = FT("Recipe_Board/Board_Detail/Txt_DishMeta");
            _txtNeedTitle  = FT("Recipe_Board/Board_Detail/Txt_NeedTitle");
            _txtTasteTitle = FT("Recipe_Board/Board_Detail/Txt_TasteTitle");
            _needChipsRoot = F("Recipe_Board/Board_Detail/Need_Chips");

            // [FIX 2026-09-02 — BUG SẾP BÁO "vào bếp trống trơn"] Need_Chips trong scene bake
            // (SampleScene lưu 2026-08-29) vẫn còn HorizontalLayoutGroup cũ. Destroy() là DEFERRED
            // (cuối frame) nên AddComponent<GridLayoutGroup> ngay sau đó bị Unity TỪ CHỐI
            // (mỗi GameObject chỉ 1 LayoutGroup) và trả về null → gl.cellSize nổ NullReference
            // → chết cả BindExistingHierarchy. Dùng helper DestroyImmediate + null-check.
            if (_needChipsRoot != null && !DangVaTrongEditor)
                EnsureNeedChipsGrid(_needChipsRoot);   // doi component layout -> khong chay o luot va Editor

            _txtRewards    = FT("Recipe_Board/Board_Detail/Txt_Rewards");
            _txtProjection = FT("Recipe_Board/Board_Detail/Txt_Projection");
            for (int i = 0; i < 5; i++)
            {
                var row = new FlavorRow();
                var rGo = F($"Recipe_Board/Board_Detail/Flavor_Row_{i}");
                if (rGo != null)
                {
                    row.rowRoot = rGo as RectTransform;
                    row.label = FT($"Recipe_Board/Board_Detail/Flavor_Row_{i}/Label");
                    var track = F($"Recipe_Board/Board_Detail/Flavor_Row_{i}/Track");
                    if (track != null)
                    {
                        row.track = track as RectTransform;
                        var fill = track.Find("Fill");
                        if (fill != null) row.fill = fill.GetComponent<Image>();
                        row.marker = track.Find("Marker") as RectTransform;
                    }
                    row.value = FT($"Recipe_Board/Board_Detail/Flavor_Row_{i}/Value");
                }
                else
                {
                    var dot = F($"Recipe_Board/Board_Detail/Flavor_Dot_{i}");
                    if (dot != null) row.dot = dot as RectTransform;
                    row.label = FT($"Recipe_Board/Board_Detail/Flavor_Label_{i}");
                    var track = F($"Recipe_Board/Board_Detail/Flavor_Track_{i}");
                    if (track != null)
                    {
                        row.track = track as RectTransform;
                        var fill = track.Find("Fill");
                        if (fill != null) row.fill = fill.GetComponent<Image>();
                        row.marker = track.Find("Marker") as RectTransform;
                    }
                    row.value = FT($"Recipe_Board/Board_Detail/Flavor_Value_{i}");
                }
                _flavorRows[i] = row;

                // Icon vị (5 gia vị: Ngọt, Cay, Chua, Đậm, Giòn)
                var dotT = transform.Find($"Recipe_Board/Board_Detail/Flavor_Dot_{i}") ?? transform.Find($"Recipe_Board/Board_Detail/Flavor_Row_{i}/Dot");
                if (dotT != null)
                {
                    var im = dotT.GetComponent<Image>();
                    if (im != null)
                    {
                        var flavorSp = GetFlavorSprite(i);
                        if (flavorSp != null)
                        {
                            im.sprite = flavorSp;
                            im.color = Color.white;
                            im.preserveAspect = true;
                        }
                        else if (im.sprite == null)
                        {
                            im.sprite = GetDotSprite();
                        }
                    }
                }
            }

            // ── Bảng công thức: LIST ──
            var lst = F("Recipe_Board/Board_List");
            _boardList = lst != null ? lst.gameObject : null;
            for (int i = 0; i < 4; i++)
            {
                int filter = i - 1;
                FB($"Recipe_Board/Board_List/Tab_{i}", () => { _listFilter = filter; RebuildDishList(); });
            }
            _dishListContent = F("Recipe_Board/Board_List/Dish_Scroll/Content");

            // ── Sân khấu ──
            _txtChalk = FT("Chalkboard/Txt_Chalk");
            if (_txtChalk != null) { _txtChalk.enableAutoSizing = true; _txtChalk.fontSizeMin = 9f; _txtChalk.fontSizeMax = 15f; } // chữ tràn viền (Sếp 2026-08-27) — tự co vừa bảng
            EnsureChalkGoldIcon(F("Chalkboard"));
            _ovenRect = F("Oven");
            if (_ovenRect != null)
            {
                var g = _ovenRect.Find("Oven_Glow");
                if (g != null) _imgOvenGlow = g.GetComponent<Image>();
                var fr = _ovenRect.Find("Oven_Fire");
                if (fr != null) _imgOvenFire = fr.GetComponent<Image>();
                var sbar = _ovenRect.Find("Oven_StateBar");
                if (sbar != null)
                {
                    Skin9(sbar.gameObject, skin.plaqueOvenState != null ? skin.plaqueOvenState : skin.panelPaper);
                    var f2 = sbar.Find("Fill");
                    if (f2 != null)
                    {
                        _imgOvenFill = f2.GetComponent<Image>();
                        if (_imgOvenFill != null)
                        {
                            _imgOvenFill.sprite = GetDotSprite();
                            _imgOvenFill.type = Image.Type.Filled;
                            _imgOvenFill.fillAmount = 0f;
                        }
                    }
                    var st = sbar.Find("Txt_State");
                    if (st != null)
                    {
                        _txtOvenState = st.GetComponent<TMP_Text>();
                        if (_txtOvenState != null)
                        {
                            _txtOvenState.color = new Color(0.99f, 0.96f, 0.88f);
                            _txtOvenState.fontStyle = FontStyles.Bold;
                        }
                    }
                }
            }

            // Xóa triệt để các decor thừa nếu từng sinh
            var oldCrates = _root.Find("Deco_Crates");
            if (oldCrates != null && !DangVaTrongEditor) DestroyImmediate(oldCrates.gameObject);
            var oldWood = _root.Find("Deco_Firewood");
            if (oldWood != null && !DangVaTrongEditor) DestroyImmediate(oldWood.gameObject);
            var maneki = F("ManekiCat");
            if (maneki != null) _imgManeki = maneki.GetComponent<Image>();
            _txtPrepToast = FT("Txt_PrepToast");
            var plat = F("Plating_Table");
            if (plat != null)
            {
                _btnPlating = plat.GetComponent<Button>();
                if (_btnPlating != null)
                {
                    _btnPlating.onClick.RemoveAllListeners();
                    _btnPlating.onClick.AddListener(OnPlatingClicked);
                }
                _txtPlating = plat.GetComponentInChildren<TMP_Text>(true);
                // [2026-09-24] Dish_Visual trong Hierarchy (icon mau Edit mode) -> an ngay khi vao Play, co mon moi hien
                EnsurePlatingDishVisual();
                if (_imgPlateDish != null) _imgPlateDish.gameObject.SetActive(false);
            }
            var wh = F("Warehouse_Box");
            if (wh != null)
            {
                var sc = wh.Find("Txt_Sent");
                if (sc != null) _txtSentCount = sc.GetComponent<TMP_Text>();
            }

            // ── Khay ──
            var tabI = F("Tray/Tab_Ingredients");
            if (tabI != null)
            {
                _tabIngredients = tabI.gameObject;
                _txtTabIng = tabI.GetComponentInChildren<TMP_Text>(true);
                var b = tabI.GetComponent<Button>();
                if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ShowTrayTab(true)); }
            }
            var tabS = F("Tray/Tab_Seasonings");
            if (tabS != null)
            {
                _tabSeasonings = tabS.gameObject;
                _txtTabSea = tabS.GetComponentInChildren<TMP_Text>(true);
                var b = tabS.GetComponent<Button>();
                if (b != null) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ShowTrayTab(false)); }
            }
            _btnClearAll = FB("Tray/Btn_ClearAll", OnClearAllClicked);
            _gridIngredients = F("Tray/Scroll_Grid_Ingredients/Viewport/Grid_Ingredients");
            _gridSeasonings  = F("Tray/Scroll_Grid_Seasonings/Viewport/Grid_Seasonings");

            // ── Nút hành động + Về nông trại ──
            var act = F("Btn_Action");
            if (act != null)
            {
                _btnAction = act.GetComponent<Button>();
                if (_btnAction != null)
                {
                    _btnAction.onClick.RemoveAllListeners();
                    _btnAction.onClick.AddListener(OnActionClicked);
                }
                _imgAction = act.GetComponent<Image>();
                var tl = act.Find("Txt_Label");
                if (tl != null) _txtAction = tl.GetComponent<TMP_Text>();
                var ts = act.Find("Txt_Sub");
                if (ts != null) _txtActionSub = ts.GetComponent<TMP_Text>();
            }
            FB("Btn_BackFarm", OnBackFarmClicked);

            // ── Polish R3/R4: Mèo đầu bếp & mèo ngủ ──
            var catChef = F("Cat_Chef");
            if (catChef != null)
            {
                var walker = catChef.GetComponent<KitchenCatWalker>();
                if (walker == null) walker = catChef.gameObject.AddComponent<KitchenCatWalker>();
                if (skin.catChefWalk != null && skin.catChefWalk.Length > 0) walker.frames = skin.catChefWalk;
                walker.minX = -280f;
                walker.maxX = 260f;
                var ci = catChef.GetComponent<Image>();
                if (ci != null && skin.catChefWalk != null && skin.catChefWalk.Length > 0 && skin.catChefWalk[0] != null)
                    ci.sprite = skin.catChefWalk[0];
            }

            var sleepCat = F("Cat_Sleeping");
            if (sleepCat != null)
            {
                var fl = sleepCat.GetComponent<KitchenZzzFloat>();
                if (fl == null) fl = sleepCat.gameObject.AddComponent<KitchenZzzFloat>();
                var zzz = sleepCat.Find("Txt_Zzz");
                if (zzz != null) fl.txt = zzz.GetComponent<TMP_Text>();
                var si = sleepCat.GetComponent<Image>();
                if (si != null && skin.catSleeping != null) si.sprite = skin.catSleeping;
            }

            // ── Dọn nội dung ĐỘNG đã bake (thẻ, ô trống, danh sách món, chip) → runtime sinh lại ──
            // [2026-09-22] Tool Editor thi KHONG don: Sep co the vua keo tay tung the trong
            // 4 khung nay, xoa di la mat sach cong suc. Luc CHAY thi van don nhu cu vi runtime
            // se sinh lai ngay sau do.
            // [2026-09-22] Them dieu kien !KhoaLayout. Truoc day khoa layout van khong cuu duoc
            // noi dung khay: dong nay xoa sach TRUOC, nen BuildTrayCards/RebuildDishList thay
            // childCount == 0 va dung lai tu dau — dung thu ma Sep vua keo tay. Khi da khoa thi
            // giu nguyen the cu, hai ham kia co duong cap nhat tai cho.
            if (!DangVaTrongEditor && !KhoaLayout)
            {
                ClearDynamicChildren(_gridIngredients);
                ClearDynamicChildren(_gridSeasonings);
                ClearDynamicChildren(_dishListContent);
                ClearDynamicChildren(_needChipsRoot);
                _cards.Clear();
                _lastSelected.Clear();
            }

            ShowTrayTab(true);
            TrySpawnFirePrefab();
            _built = true;
        }

        /// <summary>Xóa ngay con của container động (thẻ/chip bake trong scene) trước khi runtime sinh lại.</summary>
        private static void ClearDynamicChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
                DestroyImmediate(t.GetChild(i).gameObject);
        }

        private void BuildTopBar()
        {
            var chef = MakePanel(_root, "Pill_Chef", new Color(0.98f, 0.94f, 0.84f));
            Skin9(chef, skin.panelPaper);
            Anchor(chef, 0f, 1f, new Vector2(16f, -14f), new Vector2(300f, 46f), new Vector2(0f, 1f));
            _txtChef = MakeText(chef.transform, "Txt_Chef", "Bếp trưởng", 19, new Color(0.36f, 0.20f, 0.09f));
            StretchText(_txtChef, 12f, 2f);
            var expTrack = MakePanel((RectTransform)chef.transform, "Exp_Track", new Color(0.55f, 0.42f, 0.28f));
            Anchor(expTrack, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(260f, 8f), new Vector2(0.5f, 0f));
            _imgChefExpFill = MakeFill((RectTransform)expTrack.transform, new Color(0.42f, 0.78f, 0.30f));

            var gold = MakePanel(_root, "Pill_Gold", new Color(0.98f, 0.94f, 0.84f));
            Skin9(gold, skin.panelPaper);
            Anchor(gold, 0f, 1f, new Vector2(16f, -66f), new Vector2(180f, 40f), new Vector2(0f, 1f));
            _txtGold = MakeText(gold.transform, "Txt_Gold", "0", 20, new Color(0.72f, 0.52f, 0.08f));
            StretchText(_txtGold, 12f, 0f);
        }

        private void BuildOrderBanner()
        {
            var banner = MakePanel(_root, "Order_Banner", new Color(0.52f, 0.33f, 0.16f));
            Anchor(banner, 0.5f, 1f, new Vector2(70f, -70f), new Vector2(620f, 112f), new Vector2(0.5f, 1f));
            Skin9(banner, skin.panelBoard);

            // Ribbon to + chữ TRẮNG đậm nằm TRONG ribbon → không bao giờ tràn/chìm
            var ribbonB = MakePanel((RectTransform)banner.transform, "Ribbon", new Color(0.90f, 0.55f, 0.12f));
            Anchor(ribbonB, 0.5f, 1f, new Vector2(0f, 12f), new Vector2(360f, 46f), new Vector2(0.5f, 0.5f));
            Skin9(ribbonB, skin.ribbon);
            var title = MakeText(ribbonB.transform, "Txt_Title", "ĐƠN CỦA KHÁCH", 19, Color.white);
            StretchText(title, 8f, 2f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            var card = MakePanel((RectTransform)banner.transform, "Order_Card", new Color(0.98f, 0.94f, 0.84f));
            Anchor(card, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(596f, 72f), new Vector2(0.5f, 0f));
            Skin9(card, skin.panelPaper);

            var avGo = new GameObject("Img_Avatar", typeof(RectTransform), typeof(Image));
            avGo.transform.SetParent(card.transform, false);
            _imgCustomerAvatar = avGo.GetComponent<Image>();
            _imgCustomerAvatar.preserveAspect = true;
            _imgCustomerAvatar.raycastTarget = false;
            Anchor((RectTransform)avGo.transform, 0f, 0.5f, new Vector2(34f, 0f), new Vector2(52f, 52f), new Vector2(0.5f, 0.5f));

            var iconGo = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(card.transform, false);
            _imgOrderIcon = iconGo.GetComponent<Image>();
            _imgOrderIcon.preserveAspect = true;
            _imgOrderIcon.raycastTarget = false;
            Anchor((RectTransform)iconGo.transform, 0f, 0.5f, new Vector2(92f, 0f), new Vector2(48f, 48f), new Vector2(0.5f, 0.5f));

            _txtOrderName = MakeText(card.transform, "Txt_Name", "—", 19, new Color(0.30f, 0.16f, 0.07f));
            Anchor(_txtOrderName.rectTransform, 0f, 1f, new Vector2(124f, -4f), new Vector2(240f, 26f), new Vector2(0f, 1f));
            _txtOrderName.fontStyle = FontStyles.Bold;

            _txtOrderRewards = MakeText(card.transform, "Txt_Rewards", "", 15, new Color(0.72f, 0.52f, 0.08f));
            Anchor(_txtOrderRewards.rectTransform, 0f, 1f, new Vector2(124f, -36f), new Vector2(240f, 22f), new Vector2(0f, 1f));
            _txtOrderRewards.fontStyle = FontStyles.Bold;

            for (int i = 0; i < 5; i++) BuildOrderChip(card.transform, i, 368f + i * 44f);

            var cardBtn = card.GetComponent<Button>() ?? card.gameObject.AddComponent<Button>();
            cardBtn.onClick.RemoveAllListeners();
            cardBtn.onClick.AddListener(() => {
                var front = TouristVisitorManager.Instance != null ? TouristVisitorManager.Instance.GetFrontWaitingTourist() : null;
                if (front != null && front.Dish != null)
                {
                    SelectDish(front.Dish);
                }
            });
        }

        private void BuildRecipeBoard()
        {
            var board = MakePanel(_root, "Recipe_Board", new Color(0.52f, 0.33f, 0.16f));
            Anchor(board, 0f, 1f, new Vector2(14f, -152f), new Vector2(318f, 520f), new Vector2(0f, 1f));
            Skin9(board, skin.panelBoard);

            var ribbonR = MakePanel((RectTransform)board.transform, "Ribbon", new Color(0.90f, 0.55f, 0.12f));
            Anchor(ribbonR, 0.5f, 1f, new Vector2(0f, 12f), new Vector2(310f, 46f), new Vector2(0.5f, 0.5f));
            Skin9(ribbonR, skin.ribbon);

            var hdr = MakeText(ribbonR.transform, "Txt_Header", "BẢNG CÔNG THỨC", 18, Color.white);
            StretchText(hdr, 8f, 2f);
            hdr.alignment = TextAlignmentOptions.Center;
            hdr.fontStyle = FontStyles.Bold;

            // ── DETAIL ──
            var det = MakePanel((RectTransform)board.transform, "Board_Detail", new Color(0.98f, 0.94f, 0.84f));
            Skin9(det, skin.panelPaper);
            Stretch((RectTransform)det.transform, 0.02f, 0.02f, 0.98f, 0.93f);
            _boardDetail = det;
            var dt = (RectTransform)det.transform;

            var btnOther = MakeButton(dt, "Btn_OtherDish", "‹ Xem món khác", new Color(0.93f, 0.80f, 0.55f), () => ShowBoardDetail(false));
            Skin9(btnOther.gameObject, skin.btnPaperSmall);
            Anchor((RectTransform)btnOther.transform, 0f, 1f, new Vector2(10f, -8f), new Vector2(150f, 32f), new Vector2(0f, 1f));

            var iconGo = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(dt, false);
            _imgDishIcon = iconGo.GetComponent<Image>();
            _imgDishIcon.preserveAspect = true; _imgDishIcon.raycastTarget = false;
            Anchor((RectTransform)iconGo.transform, 0f, 1f, new Vector2(34f, -52f), new Vector2(52f, 52f), new Vector2(0.5f, 1f));

            _txtDishName = MakeText(dt, "Txt_DishName", "—", 20, new Color(0.36f, 0.20f, 0.09f));
            Anchor(_txtDishName.rectTransform, 0f, 1f, new Vector2(72f, -48f), new Vector2(230f, 26f), new Vector2(0f, 1f));
            _txtDishMeta = MakeText(dt, "Txt_DishMeta", "", 14, new Color(0.72f, 0.42f, 0.15f));
            Anchor(_txtDishMeta.rectTransform, 0f, 1f, new Vector2(72f, -76f), new Vector2(230f, 20f), new Vector2(0f, 1f));

            _txtNeedTitle = MakeText(dt, "Txt_NeedTitle", "CẦN NHỮNG THỨ NÀY", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(_txtNeedTitle.rectTransform, 0f, 1f, new Vector2(12f, -112f), new Vector2(280f, 18f), new Vector2(0f, 1f));

            var chips = new GameObject("Need_Chips", typeof(RectTransform), typeof(GridLayoutGroup));
            chips.transform.SetParent(dt, false);
            var gl = chips.GetComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(66f, 64f);
            gl.spacing = new Vector2(6f, 6f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperLeft;
            Anchor((RectTransform)chips.transform, 0f, 1f, new Vector2(12f, -134f), new Vector2(285f, 66f), new Vector2(0f, 1f));
            _needChipsRoot = chips.transform;

            _txtTasteTitle = MakeText(dt, "Txt_TasteTitle", "VỊ KHÁCH MUỐN", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(_txtTasteTitle.rectTransform, 0f, 1f, new Vector2(12f, -206f), new Vector2(280f, 18f), new Vector2(0f, 1f));

            for (int i = 0; i < 5; i++)
                BuildFlavorRow(dt, i, -230f - i * 34f);

            _txtRewards = MakeText(dt, "Txt_Rewards", "", 15, new Color(0.72f, 0.52f, 0.08f));
            Anchor(_txtRewards.rectTransform, 0f, 0f, new Vector2(12f, 40f), new Vector2(290f, 22f), new Vector2(0f, 0f));

            _txtProjection = MakeText(dt, "Txt_Projection", "Điểm dự kiến:  — đ", 17, new Color(0.75f, 0.25f, 0.15f));
            Anchor(_txtProjection.rectTransform, 0f, 0f, new Vector2(12f, 10f), new Vector2(290f, 26f), new Vector2(0f, 0f));

            // ── LIST (Sổ công thức) ──
            var lst = MakePanel((RectTransform)board.transform, "Board_List", new Color(0.98f, 0.94f, 0.84f));
            Skin9(lst, skin.panelPaper);
            Stretch((RectTransform)lst.transform, 0.02f, 0.02f, 0.98f, 0.93f);
            _boardList = lst;
            var lt = (RectTransform)lst.transform;

            // [VÒNG 13] 4 nút lọc trước đây là MÀU PHẲNG be nhạt, cả 4 giống hệt nhau nên người
            // chơi không biết đang ở tab nào. Nay dùng tab_pill_on/off (9-slice border 24) — art
            // ĐÃ CÓ SẴN trong skin (nạp ở KitchenV2SetupTool), không phải vẽ mới.
            string[] tabNames = { "Tất cả", "Dễ", "Vừa", "Khó" };
            _listTabs = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                int filter = i - 1;
                var b = MakeButton(lt, "Tab_" + i, tabNames[i], new Color(0.93f, 0.80f, 0.55f),
                    () => { _listFilter = filter; CapNhatTabLoc(); RebuildDishList(); });
                Anchor((RectTransform)b.transform, 0f, 1f, new Vector2(8f + i * 74f, -8f), new Vector2(68f, 30f), new Vector2(0f, 1f));
                _listTabs[i] = b.gameObject;
            }
            CapNhatTabLoc();

            var scrollGo = new GameObject("Dish_Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(lt, false);
            var srt = (RectTransform)scrollGo.transform;
            Stretch(srt, 0.02f, 0.02f, 0.98f, 1f);
            srt.offsetMax = new Vector2(srt.offsetMax.x, -44f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.03f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollGo.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f); crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var vl = content.GetComponent<VerticalLayoutGroup>();
            vl.spacing = 6f; vl.padding = new RectOffset(6, 6, 6, 6);
            vl.childControlHeight = false; vl.childControlWidth = true;
            vl.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = srt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            _dishListContent = content.transform;

            lst.SetActive(true);
            det.SetActive(false);
        }

        private void BuildFlavorRow(RectTransform parent, int index, float y)
        {
            var rowGo = new GameObject($"Flavor_Row_{index}", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRt = (RectTransform)rowGo.transform;
            Anchor(rowRt, 0f, 1f, new Vector2(10f, y), new Vector2(285f, 22f), new Vector2(0f, 1f));

            var row = new FlavorRow();
            row.rowRoot = rowRt;

            // Icon gia vị (5 gia vị: Ngọt, Cay, Chua, Đậm, Giòn)
            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(rowRt, false);
            var dimg = dot.GetComponent<Image>();
            var flavorSp = GetFlavorSprite(index);
            if (flavorSp != null)
            {
                dimg.sprite = flavorSp;
                dimg.color = Color.white;
                dimg.preserveAspect = true;
                Anchor((RectTransform)dot.transform, 0f, 0.5f, new Vector2(4f, 0f), new Vector2(22f, 22f), new Vector2(0f, 0.5f));
            }
            else
            {
                dimg.sprite = GetDotSprite();
                dimg.color = FlavorDotColors[index % FlavorDotColors.Length];
                Anchor((RectTransform)dot.transform, 0f, 0.5f, new Vector2(4f, 0f), new Vector2(12f, 12f), new Vector2(0f, 0.5f));
            }
            dimg.raycastTarget = false;

            row.label = MakeText(rowRt, "Label", "", 14, new Color(0.36f, 0.20f, 0.09f));
            Anchor(row.label.rectTransform, 0f, 0.5f, new Vector2(28f, 0f), new Vector2(52f, 22f), new Vector2(0f, 0.5f));

            var track = MakePanel(rowRt, "Track", new Color(0.85f, 0.76f, 0.60f));
            Anchor(track, 0f, 0.5f, new Vector2(70f, 0f), new Vector2(160f, 14f), new Vector2(0f, 0.5f));
            Skin9(track, skin.tasteTrack);
            row.fill = MakeFill((RectTransform)track.transform, new Color(0.55f, 0.75f, 0.35f));
            if (skin.tasteFill != null) row.fill.sprite = skin.tasteFill;

            var marker = MakePanel((RectTransform)track.transform, "Marker", new Color(0.82f, 0.16f, 0.12f));
            SkinFlat(marker, skin.tasteMarker, false);
            var mrt = (RectTransform)marker.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(3f, 20f);
            row.marker = mrt;

            row.value = MakeText(rowRt, "Value", "0/0", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(row.value.rectTransform, 0f, 0.5f, new Vector2(238f, 0f), new Vector2(50f, 22f), new Vector2(0f, 0.5f));

            _flavorRows[index] = row;
        }

        private static readonly string[] FlavorIconAssetPaths = new string[] {
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_sweet.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_spicy.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_sour.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_umami.png",
            "Assets/Art/UI/KitchenCozyV3/Flavors/icon_flavor_texture.png"
        };
        private Sprite[] _cachedFlavorSprites;

        private Sprite GetFlavorSprite(int index)
        {
            if (index < 0 || index >= FlavorIconAssetPaths.Length) return null;
            if (_cachedFlavorSprites == null) _cachedFlavorSprites = new Sprite[5];
            if (_cachedFlavorSprites[index] == null)
            {
#if UNITY_EDITOR
                _cachedFlavorSprites[index] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(FlavorIconAssetPaths[index]);
#endif
            }
            return _cachedFlavorSprites[index];
        }

        /// <summary>Chip nhỏ dot-màu + số trên banner đơn khách (Sếp yêu cầu icon vị 2026-08-27).</summary>
        private void BuildOrderChip(Transform cardT, int index, float x)
        {
            var chip = new GameObject($"Chip_{index}", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(cardT, false);
            var cimg = chip.GetComponent<Image>();
            cimg.raycastTarget = false;
            Skin9(chip, skin.chipTaste); // nền chip bo góc (chờ art "chip_taste"); trống thì trong suốt
            if (skin.chipTaste == null) cimg.color = Color.clear;
            Anchor((RectTransform)chip.transform, 0f, 0.5f, new Vector2(x, 0f), new Vector2(42f, 24f), new Vector2(0f, 0.5f));

            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(chip.transform, false);
            var dimg = dot.GetComponent<Image>();
            dimg.sprite = GetDotSprite();
            dimg.color = FlavorDotColors[index % FlavorDotColors.Length];
            dimg.raycastTarget = false;
            Anchor((RectTransform)dot.transform, 0f, 0.5f, new Vector2(3f, 0f), new Vector2(11f, 11f), new Vector2(0f, 0.5f));

            var val = MakeText(chip.transform, "Txt_Val", "0", 14, new Color(0.36f, 0.20f, 0.09f));
            Anchor(val.rectTransform, 0f, 0.5f, new Vector2(16f, 0f), new Vector2(24f, 20f), new Vector2(0f, 0.5f));
            val.fontStyle = FontStyles.Bold;
            _orderChipValues[index] = val;
        }

        /// <summary>
        /// [2026-09-21] Cap nhat danh sach mon MA KHONG huy object — dung khi da khoa layout.
        /// Doi chieu theo TEN object "Row_&lt;dishId&gt;": the nao con trong bo loc thi bat + doi
        /// chu/anh/mau/interactable; the ngoai bo loc thi TAT, khong xoa. Nho vay moi chinh tay
        /// cua Sep (vi tri, co chu, sprite thay the) deu giu nguyen qua moi lan Play.
        /// </summary>
        private void CapNhatDanhSachMonTaiCho()
        {
            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 999;

            var theo = new Dictionary<string, Transform>();
            for (int i = 0; i < _dishListContent.childCount; i++)
            {
                var c = _dishListContent.GetChild(i);
                if (c.name.StartsWith("Row_")) theo[c.name.Substring(4)] = c;
                c.gameObject.SetActive(false);
            }

            int thuTu = 0;
            foreach (var d in MonTheoCap())
            {
                if (d == null) continue;
                // [2026-09-23] Mon mo o cap thap dung truoc: xep lai thu tu dong theo cap mo khoa.
                if (theo.TryGetValue(d.dishId, out var dongXep) && dongXep != null) dongXep.SetSiblingIndex(thuTu++);
                if (_listFilter >= 0 && (int)d.difficulty != _listFilter) continue;
                if (!theo.TryGetValue(d.dishId, out var row) || row == null) continue;

                row.gameObject.SetActive(true);
                bool unlocked = d.unlockLevel <= lv;

                var bg = row.GetComponent<Image>();
                if (bg != null && !KhoaLayout)
                {
                    Skin9(row.gameObject, skin.cardDishRow != null ? skin.cardDishRow : (unlocked ? skin.cardIngredient : skin.cardLocked));
                    bg.color = unlocked ? new Color(1f, 0.99f, 0.94f) : new Color(0.88f, 0.84f, 0.76f);
                }

                var btn = row.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.interactable = unlocked;
                    if (unlocked) { var dd = d; btn.onClick.AddListener(() => SelectDish(dd)); }
                }

                var ico = row.Find("Img_Icon");
                if (ico != null)
                {
                    var im = ico.GetComponent<Image>();
                    if (im != null)
                    {
                        im.sprite  = d.dishSprite;
                        im.enabled = d.dishSprite != null;
                        im.color   = unlocked ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.65f);
                    }
                }

                var kh = row.Find("Img_Lock");
                if (kh != null) kh.gameObject.SetActive(!unlocked);

                // [2026-09-23] Tim chu THEO TEN (Txt_Name / Txt_Meta) thay vi theo thu tu, va KHONG
                // di qua LocFit: LocFit bop co chu (25 -> 18, hop cao 24 => ten bi cat mat) nen Play
                // nhin nho xiu/mat ten so voi Edit. Giu dung co chu Sep chinh trong Edit mode.
                var chu = row.GetComponentsInChildren<TMP_Text>(true);
                var tTen  = row.Find("Txt_Name") != null ? row.Find("Txt_Name").GetComponent<TMP_Text>() : (chu.Length > 0 ? chu[0] : null);
                var tMeta = row.Find("Txt_Meta") != null ? row.Find("Txt_Meta").GetComponent<TMP_Text>() : (chu.Length > 1 ? chu[1] : null);
                DatChuGiuCo(tTen, Loc.T(d.dishName));
                DatChuGiuCo(tMeta, unlocked
                    ? Loc.TF("{0} · Cấp {1} · {2} vàng", DiffName(d.difficulty), d.unlockLevel, d.rewardGold)
                    : Loc.TF("Mở ở cấp {0}", d.unlockLevel));
            }
        }

        private void RebuildDishList()
        {
            if (_dishListContent == null || dishBook == null || dishBook.allDishes == null) return;

            // [FIX 2026-09-21 — KHOA LAYOUT] Ban cu XOA SACH roi tao lai moi lan goi.
            // Khi Sep da dong bang UI thanh Hierarchy va chinh tay tung the, xoa sach = mat het.
            // Khoa layout thi CHI cap nhat chu/anh/mau tren the DA CO SAN, khong huy object nao.
            if (KhoaLayout && _dishListContent.childCount > 0)
            {
                CapNhatDanhSachMonTaiCho();
                return;
            }

            for (int i = _dishListContent.childCount - 1; i >= 0; i--)
                Destroy(_dishListContent.GetChild(i).gameObject);

            // Không có PlayerProgressManager = đang chạy riêng scene bếp để dev/test → mở hết
            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 999;

            foreach (var d in MonTheoCap())
            {
                if (d == null) continue;
                if (_listFilter >= 0 && (int)d.difficulty != _listFilter) continue;

                bool unlocked = d.unlockLevel <= lv;
                var dish = d;

                var row = MakeButton((RectTransform)_dishListContent, "Row_" + d.dishId,
                    "", unlocked ? new Color(1f, 0.99f, 0.94f) : new Color(0.88f, 0.84f, 0.76f),
                    unlocked ? () => SelectDish(dish) : (UnityEngine.Events.UnityAction)null);
                Skin9(row.gameObject, skin.cardDishRow != null ? skin.cardDishRow : (unlocked ? skin.cardIngredient : skin.cardLocked));
                var rrt = (RectTransform)row.transform;
                rrt.sizeDelta = new Vector2(0f, 52f);

                var ico = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image));
                ico.transform.SetParent(row.transform, false);
                var im = ico.GetComponent<Image>();
                im.sprite = d.dishSprite; im.enabled = d.dishSprite != null;
                im.preserveAspect = true; im.raycastTarget = false;
                if (!unlocked) im.color = new Color(0.45f, 0.45f, 0.45f, 0.65f);
                Anchor((RectTransform)ico.transform, 0f, 0.5f, new Vector2(26f, 0f), new Vector2(38f, 38f), new Vector2(0.5f, 0.5f));

                if (!unlocked)
                {
                    var lockGo = new GameObject("Img_Lock", typeof(RectTransform), typeof(Image));
                    lockGo.transform.SetParent(row.transform, false);
                    var lockImg = lockGo.GetComponent<Image>();
                    var lockSp = skin.iconLock != null ? skin.iconLock : Resources.Load<Sprite>("icon_lock");
                    lockImg.sprite = lockSp;
                    lockImg.preserveAspect = true;
                    lockImg.raycastTarget = false;
                    Anchor((RectTransform)lockGo.transform, 0f, 0.5f, new Vector2(26f, 0f), new Vector2(24f, 24f), new Vector2(0.5f, 0.5f));
                }

                var name = MakeText(row.transform, "Txt_Name", Loc.T(d.dishName), 16, unlocked ? new Color(0.36f, 0.20f, 0.09f) : new Color(0.52f, 0.46f, 0.40f));
                Anchor(name.rectTransform, 0f, 1f, new Vector2(52f, -4f), new Vector2(210f, 22f), new Vector2(0f, 1f));

                string meta = unlocked
                    ? Loc.TF("{0} · Cấp {1} · {2} vàng", DiffName(d.difficulty), d.unlockLevel, d.rewardGold)
                    : Loc.TF("Mở ở cấp {0}", d.unlockLevel);
                var sub = MakeText(row.transform, "Txt_Meta", meta, 12, new Color(0.6f, 0.45f, 0.28f));
                Anchor(sub.rectTransform, 0f, 0f, new Vector2(52f, 4f), new Vector2(220f, 18f), new Vector2(0f, 0f));
            }
            Loc.RequestRescan(); // danh sach vua dung sau luot quet ⇒ xin quet lai
        }

        private void BuildStage()
        {
            // Bảng đen MÓN HÔM NAY
            var chalk = MakePanel(_root, "Chalkboard", new Color(0.16f, 0.14f, 0.12f));
            Anchor(chalk, 0.5f, 1f, new Vector2(240f, -180f), new Vector2(280f, 175f), new Vector2(0.5f, 1f));
            SkinFlat(chalk, skin.chalkboard, false);

            // Decor bếp (chỉ hiện khi có skin)
            MakeDecor(_root, "Shelf_Props", skin.shelfProps, 0.5f, 1f, new Vector2(-40f, -185f), new Vector2(180f, 90f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Plant_L", skin.plantPot, 0.5f, 0.5f, new Vector2(-292f, -172f), new Vector2(58f, 70f), new Vector2(0.5f, 0.5f));
            MakeDecor(_root, "Plant_L2", skin.plantPot, 0.5f, 0.5f, new Vector2(-232f, -172f), new Vector2(58f, 70f), new Vector2(0.5f, 0.5f));
            MakeDecor(_root, "Sack_Flour", skin.sackFlour, 1f, 0.5f, new Vector2(-150f, -176f), new Vector2(62f, 66f), new Vector2(1f, 0.5f));
            var sleepCat = MakeDecor(_root, "Cat_Sleeping", skin.catSleeping, 1f, 0.5f, new Vector2(-60f, -184f), new Vector2(74f, 48f), new Vector2(1f, 0.5f));
            if (sleepCat != null)
            {
                var zzz = MakeText(sleepCat.transform, "Txt_Zzz", "z Z z", 15, new Color(0.42f, 0.52f, 0.72f));
                zzz.fontStyle = FontStyles.Bold | FontStyles.Italic;
                zzz.alignment = TextAlignmentOptions.Center;
                zzz.raycastTarget = false;
                Anchor(zzz.rectTransform, 0.5f, 1f, new Vector2(12f, 6f), new Vector2(64f, 24f), new Vector2(0.5f, 0f));
                var fl = sleepCat.gameObject.AddComponent<KitchenZzzFloat>();
                fl.txt = zzz;
            }
            _txtChalk = MakeText(chalk.transform, "Txt_Chalk", "MÓN HÔM NAY", 15, new Color(0.97f, 0.94f, 0.88f));
            StretchText(_txtChalk, 22f, 16f);
            _txtChalk.alignment = TextAlignmentOptions.Left; // giữa dọc + trái ngang — khối chữ nằm chính giữa bảng
            _txtChalk.enableAutoSizing = true; _txtChalk.fontSizeMin = 9f; _txtChalk.fontSizeMax = 15f; // Sếp báo chữ tràn viền (2026-08-27) — tự co vừa bảng
            EnsureChalkGoldIcon(chalk.transform);

            // Mèo Thần Tài — có skin thì animation 4 frame, chưa có thì placeholder chữ
            var cat = MakePanel(_root, "ManekiCat", new Color(0.97f, 0.96f, 0.93f));
            Anchor(cat, 0.5f, 1f, new Vector2(-230f, -130f), new Vector2(86f, 92f), new Vector2(0.5f, 1f));
            if (skin.manekiIdle != null && skin.manekiIdle.Length > 0)
            {
                var catImg = cat.GetComponent<Image>();
                catImg.sprite = skin.manekiIdle[0];
                catImg.preserveAspect = true;
                catImg.color = Color.white;
                _imgManeki = catImg;
            }
            else
            {
                var catTxt = MakeText(cat.transform, "Txt_Cat", "Mèo Thần Tài", 11, new Color(0.55f, 0.38f, 0.22f));
                StretchText(catTxt, 2f, 2f);
                catTxt.alignment = TextAlignmentOptions.Center;
            }

            // Kệ gỗ đỡ mèo + bảng tên "Mèo Thần Tài" (theo mockup Kitchen Cook Flow)
            var catShelf = MakePanel(_root, "Maneki_Shelf", new Color(0.55f, 0.36f, 0.18f));
            Skin9(catShelf, skin.panelBoard);
            Anchor(catShelf, 0.5f, 1f, new Vector2(-230f, -220f), new Vector2(126f, 20f), new Vector2(0.5f, 1f));
            var catPlate = MakePanel(_root, "Maneki_Label", new Color(0.98f, 0.94f, 0.84f));
            Skin9(catPlate, skin.panelPaper);
            Anchor(catPlate, 0.5f, 1f, new Vector2(-230f, -244f), new Vector2(114f, 28f), new Vector2(0.5f, 1f));
            var catName = MakeText(catPlate.transform, "Txt", "Mèo Thần Tài", 12, new Color(0.36f, 0.20f, 0.09f));
            StretchText(catName, 4f, 2f);
            catName.alignment = TextAlignmentOptions.Center;
            catName.fontStyle = FontStyles.Bold;

            // Lò nướng
            var oven = MakePanel(_root, "Oven", new Color(0.72f, 0.45f, 0.30f));
            Anchor(oven, 1f, 1f, new Vector2(-20f, -110f), new Vector2(280f, 240f), new Vector2(1f, 1f));
            _ovenRect = (RectTransform)oven.transform;

            var mouth = MakePanel((RectTransform)oven.transform, "Oven_Mouth", new Color(0.25f, 0.13f, 0.08f));
            Anchor(mouth, 0.5f, 0.5f, new Vector2(0f, 16f), new Vector2(140f, 100f), new Vector2(0.5f, 0.5f));

            if (skin.ovenBody != null)
            {
                // Có art thật: thân lò vẽ đè, tắt 2 khối màu phẳng
                oven.GetComponent<Image>().color = Color.clear;
                mouth.GetComponent<Image>().color = Color.clear;
                var body = MakeDecor(_ovenRect, "Oven_Body", skin.ovenBody, 0.5f, 0.5f, new Vector2(0f, 12f), new Vector2(260f, 225f), new Vector2(0.5f, 0.5f));
                if (body != null) body.transform.SetAsFirstSibling();

                var glow = MakeDecor(_ovenRect, "Oven_Glow", skin.ovenGlow, 0.5f, 0.5f, new Vector2(0f, 8f), new Vector2(150f, 112f), new Vector2(0.5f, 0.5f));
                if (glow != null) { _imgOvenGlow = glow; glow.enabled = false; }

                if (skin.ovenFire != null && skin.ovenFire.Length > 0)
                {
                    var fire = MakeDecor(_ovenRect, "Oven_Fire", skin.ovenFire[0], 0.5f, 0.5f, new Vector2(0f, 4f), new Vector2(100f, 88f), new Vector2(0.5f, 0.5f));
                    if (fire != null) { _imgOvenFire = fire; fire.enabled = false; }
                }
            }
            var stateBar = MakePanel((RectTransform)oven.transform, "Oven_StateBar", new Color(0.98f, 0.94f, 0.84f));
            Skin9(stateBar, skin.plaqueOvenState != null ? skin.plaqueOvenState : skin.panelPaper);
            Anchor(stateBar, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(214f, 34f), new Vector2(0.5f, 0f));
            _imgOvenFill = MakeFill((RectTransform)stateBar.transform, new Color(0.95f, 0.65f, 0.2f));
            _imgOvenFill.fillAmount = 0f;
            _txtOvenState = MakeText(stateBar.transform, "Txt_State", "Lò chưa nhóm", 15, new Color(0.99f, 0.96f, 0.88f));
            _txtOvenState.fontStyle = FontStyles.Bold;
            StretchText(_txtOvenState, 6f, 0f);
            _txtOvenState.alignment = TextAlignmentOptions.Center;

            // Bàn sơ chế (toast tiến trình)
            var prep = MakePanel(_root, "Prep_Table", new Color(0.85f, 0.70f, 0.50f));
            SkinFlat(prep, skin.prepTable);
            Anchor(prep, 0.5f, 0.5f, new Vector2(-120f, 40f), new Vector2(190f, 64f), new Vector2(0.5f, 0.5f));
            var prepPill = MakePanel((RectTransform)prep.transform, "Label_Pill", new Color(0.98f, 0.94f, 0.84f));
            Skin9(prepPill, skin.panelPaper);
            Anchor(prepPill, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(128f, 30f), new Vector2(0.5f, 1f));
            prepPill.GetComponent<Image>().raycastTarget = false;
            var prepLbl = MakeText(prepPill.transform, "Txt_Label", "Bàn sơ chế", 15, new Color(0.36f, 0.20f, 0.09f));
            StretchText(prepLbl, 4f, 0f);
            prepLbl.alignment = TextAlignmentOptions.Center;
            prepLbl.fontStyle = FontStyles.Bold;
            _txtPrepToast = MakeText(_root, "Txt_PrepToast", "", 15, new Color(0.30f, 0.55f, 0.15f));
            Anchor(_txtPrepToast.rectTransform, 0.5f, 0.5f, new Vector2(0f, 110f), new Vector2(420f, 24f), new Vector2(0.5f, 0.5f));
            _txtPrepToast.alignment = TextAlignmentOptions.Center;

            // Bàn trình bày (nút cất kho)
            var plating = MakeButton(_root, "Plating_Table", "Trình bày", new Color(0.90f, 0.78f, 0.58f), OnPlatingClicked);
            SkinFlat(plating.gameObject, skin.platingTable);
            Anchor((RectTransform)plating.transform, 0.5f, 0.5f, new Vector2(110f, 40f), new Vector2(190f, 64f), new Vector2(0.5f, 0.5f));
            _btnPlating = plating;
            _txtPlating = plating.GetComponentInChildren<TMP_Text>();
            var platPill = MakePanel((RectTransform)plating.transform, "Label_Pill", new Color(0.98f, 0.94f, 0.84f));
            Skin9(platPill, skin.panelPaper);
            Anchor(platPill, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(150f, 30f), new Vector2(0.5f, 1f));
            platPill.GetComponent<Image>().raycastTarget = false;
            _txtPlating.transform.SetParent(platPill.transform, false);
            StretchText(_txtPlating, 4f, 0f);
            _txtPlating.enableAutoSizing = true;
            _txtPlating.fontSizeMin = 9f; _txtPlating.fontSizeMax = 15f;
            _txtPlating.fontStyle = FontStyles.Bold;
            _txtPlating.color = new Color(0.36f, 0.20f, 0.09f);
            _txtPlating.alignment = TextAlignmentOptions.Center;

            // Hộp VÀO KHO
            var wh = MakePanel(_root, "Warehouse_Box", new Color(0.45f, 0.26f, 0.12f));
            SkinFlat(wh, skin.warehouseHatch, false); // false: sprite phủ kín khung → chữ luôn nằm TRÊN nền sprite
            Anchor(wh, 1f, 0.5f, new Vector2(-250f, 108f), new Vector2(160f, 152f), new Vector2(1f, 0.5f));
            var whLbl = MakeText(wh.transform, "Txt_Wh", "VÀO KHO", 17, new Color(1f, 0.93f, 0.55f));
            Anchor(whLbl.rectTransform, 0.5f, 1f, new Vector2(0f, -10f), new Vector2(176f, 24f), new Vector2(0.5f, 1f));
            whLbl.alignment = TextAlignmentOptions.Center;
            whLbl.fontStyle = FontStyles.Bold;
            _txtSentCount = MakeText(wh.transform, "Txt_Sent", Loc.TF("Đã gửi {0} món", PlayerPrefs.GetInt(SentCountKey, 0)), 14, new Color(0.99f, 0.96f, 0.88f));
            Anchor(_txtSentCount.rectTransform, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(176f, 22f), new Vector2(0.5f, 0f));
            _txtSentCount.alignment = TextAlignmentOptions.Center;

            // ── Polish R3: decor treo tường + nồi nấu + mèo đầu bếp đi dạo + lửa prefab ──
            MakeDecor(_root, "Deco_Garlic_R", skin.decorGarlic, 0.5f, 1f, new Vector2(330f, -4f), new Vector2(48f, 82f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Onion_R",  skin.decorOnion,  0.5f, 1f, new Vector2(392f, -6f), new Vector2(50f, 86f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Herbs_R",  skin.decorHerbs,  0.5f, 1f, new Vector2(456f, -4f), new Vector2(52f, 78f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Herbs_L",  skin.decorHerbs,  0.5f, 1f, new Vector2(-540f, -4f), new Vector2(52f, 78f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Garlic_L", skin.decorGarlic, 0.5f, 1f, new Vector2(-478f, -6f), new Vector2(48f, 82f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Lights_L", skin.decorLights, 0.5f, 1f, new Vector2(-360f, 0f), new Vector2(280f, 40f), new Vector2(0.5f, 1f));
            MakeDecor(_root, "Deco_Lights_R", skin.decorLights, 0.5f, 1f, new Vector2(520f, 0f), new Vector2(300f, 40f), new Vector2(0.5f, 1f));

            if (skin.catChefWalk != null && skin.catChefWalk.Length > 0 && skin.catChefWalk[0] != null)
            {
                var catGo = new GameObject("Cat_Chef", typeof(RectTransform), typeof(Image), typeof(KitchenCatWalker));
                catGo.transform.SetParent(_root, false);
                var ci = catGo.GetComponent<Image>();
                ci.sprite = skin.catChefWalk[0]; ci.preserveAspect = true; ci.raycastTarget = false;
                Anchor((RectTransform)catGo.transform, 0.5f, 0.5f, new Vector2(-100f, -84f), new Vector2(96f, 88f), new Vector2(0.5f, 0.5f)); // nâng lên để thấy hết thân (Sếp 2026-08-26)
                var walker = catGo.GetComponent<KitchenCatWalker>();
                walker.frames = skin.catChefWalk;
                walker.minX = -280f; walker.maxX = 260f;
            }

            TrySpawnFirePrefab();
        }

        private void BuildTray()
        {
            var tray = MakePanel(_root, "Tray", new Color(0.42f, 0.26f, 0.13f));
            Skin9(tray, skin.panelBoard);
            Anchor(tray, 0.5f, 0f, new Vector2(30f, 12f), new Vector2(880f, 250f), new Vector2(0.5f, 0f));
            var trt = (RectTransform)tray.transform;

            var tabIng = MakeButton(trt, "Tab_Ingredients", Loc.TF("Nguyên liệu {0}/{1}", 0, 4), new Color(0.55f, 0.78f, 0.35f), () => ShowTrayTab(true));
            Anchor((RectTransform)tabIng.transform, 0f, 1f, new Vector2(12f, -8f), new Vector2(160f, 34f), new Vector2(0f, 1f));
            _tabIngredients = tabIng.gameObject;
            _txtTabIng = tabIng.GetComponentInChildren<TMP_Text>();

            var tabSea = MakeButton(trt, "Tab_Seasonings", Loc.TF("Gia vị {0}/{1}", 0, 3), new Color(0.45f, 0.65f, 0.90f), () => ShowTrayTab(false));
            Anchor((RectTransform)tabSea.transform, 0f, 1f, new Vector2(182f, -8f), new Vector2(120f, 34f), new Vector2(0f, 1f));
            _tabSeasonings = tabSea.gameObject;
            _txtTabSea = tabSea.GetComponentInChildren<TMP_Text>();

            var clear = MakeButton(trt, "Btn_ClearAll", Loc.T("Bỏ hết"), new Color(0.82f, 0.30f, 0.22f), OnClearAllClicked);
            Skin9(clear.gameObject, skin.btnRedSmall);
            Anchor((RectTransform)clear.transform, 1f, 1f, new Vector2(-12f, -8f), new Vector2(90f, 34f), new Vector2(1f, 1f));
            _btnClearAll = clear;

            _gridIngredients = MakeGrid(trt, "Grid_Ingredients");
            _gridSeasonings  = MakeGrid(trt, "Grid_Seasonings");
            _gridSeasonings.gameObject.SetActive(false);
            ShowTrayTab(true);
        }

        private Transform MakeGrid(RectTransform parent, string name)
        {
            // ScrollRect bọc grid — kéo bằng tay/chuột + lăn chuột đều được (Sếp yêu cầu 2026-08-26)
            var scroll = new GameObject("Scroll_" + name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scroll.transform.SetParent(parent, false);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0f, 0f); srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(12f, 10f); srt.offsetMax = new Vector2(-12f, -48f);
            scroll.GetComponent<Image>().color = Color.clear; // cần Graphic trong suốt để bắt drag

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scroll.transform, false);
            var vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;

            var go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(viewport.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 88f);
            grid.spacing = new Vector2(9f, 9f);
            grid.childAlignment = TextAnchor.UpperLeft;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scroll.GetComponent<ScrollRect>();
            sr.viewport = vrt; sr.content = rt;
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 24f;
            return go.transform;
        }

        private void ShowTrayTab(bool ingredients)
        {
            if (_gridIngredients != null) _gridIngredients.parent.parent.gameObject.SetActive(ingredients);
            if (_gridSeasonings != null)  _gridSeasonings.parent.parent.gameObject.SetActive(!ingredients);

            // Khoa layout: giu sprite tab Sep chon o Edit mode, chi doi mau chu de biet tab nao dang mo.
            if (!KhoaLayout && skin.tabOn != null && skin.tabOff != null)
            {
                Skin9(_tabIngredients, ingredients ? skin.tabOn : skin.tabOff);
                Skin9(_tabSeasonings, ingredients ? skin.tabOff : skin.tabOn);
            }

            // Phối màu chữ tab cho tương phản: tab đang mở nâu sậm, tab kia kem sáng
            var cTabOn  = new Color(0.30f, 0.16f, 0.07f);
            var cTabOff = new Color(0.55f, 0.38f, 0.20f);   // [2026-09-23] kem tren nen kem = mat chu
            if (_txtTabIng != null) { _txtTabIng.color = ingredients ? cTabOn : cTabOff; _txtTabIng.fontStyle = FontStyles.Bold; }
            if (_txtTabSea != null) { _txtTabSea.color = ingredients ? cTabOff : cTabOn; _txtTabSea.fontStyle = FontStyles.Bold; }
        }

        /// <summary>Dựng thẻ nguyên liệu/gia vị bằng chính SelectableIngredientCard cũ — SelectionManager giữ nguyên.</summary>
        private void BuildTrayCards()
        {
            if (allIngredients == null || _gridIngredients == null || selection == null) return;

            // [2026-09-22] KHOA LAYOUT + khay DA CO the san ⇒ DUNG LAI the cu, khong dung moi.
            // Neu khong, moi lan bam Play la the bi xoa roi sinh lai, va moi tinh chinh tay
            // cua Sep trong khay bay sach. Day la doi xung voi CapNhatDanhSachMonTaiCho()
            // ma RebuildDishList() da lam cho danh sach mon.
            if (KhoaLayout && _gridIngredients.childCount > 0)
            {
                DungLaiTheKhayCoSan();
                return;
            }

            int playerLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 999; // chạy riêng scene = dev mode mở hết

            foreach (var data in allIngredients)
            {
                if (data == null) continue;
                bool isSea = data.kind == IngredientKind.Seasoning;
                var parent = isSea ? _gridSeasonings : _gridIngredients;

                // Sếp 2026-08-27: khay chỉ hiện món ĐÃ GỬI — nguyên liệu chưa mở khoá thì chưa thể
                // gửi nên bỏ luôn thẻ khoá (BuildLockedCard giữ lại phòng khi đổi ý về chế độ danh mục).
                if (data.unlockLevel > playerLevel)
                    continue;

                var card = new GameObject("Card_" + data.id, typeof(RectTransform), typeof(Image));
                card.transform.SetParent(parent, false);
                card.GetComponent<Image>().color = new Color(1f, 0.99f, 0.94f);
                Skin9(card, skin.cardIngredient);

                // Tên child ĐÚNG quy ước ResolveRefsIfMissing của SelectableIngredientCard cũ
                var icon = new GameObject("Img_MainIcon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(card.transform, false);
                var im = icon.GetComponent<Image>();
                im.sprite = data.icon; im.enabled = data.icon != null;
                im.preserveAspect = true; im.raycastTarget = false;
                Anchor((RectTransform)icon.transform, 0.5f, 1f, new Vector2(0f, -6f), new Vector2(44f, 44f), new Vector2(0.5f, 1f));

                var name = new GameObject("Txt_Name", typeof(RectTransform));
                name.transform.SetParent(card.transform, false);
                var nt = name.AddComponent<TextMeshProUGUI>();
                ApplyFont(nt); nt.text = data.displayName; nt.fontSize = 13;
                nt.color = new Color(0.36f, 0.20f, 0.09f); nt.alignment = TextAlignmentOptions.Center;
                nt.raycastTarget = false;
                Anchor((RectTransform)name.transform, 0.5f, 0f, new Vector2(0f, 16f), new Vector2(106f, 18f), new Vector2(0.5f, 0f));

                var qty = new GameObject("Txt_Quantity", typeof(RectTransform));
                qty.transform.SetParent(card.transform, false);
                var qt = qty.AddComponent<TextMeshProUGUI>();
                ApplyFont(qt); qt.text = "x0"; qt.fontSize = 12; qt.fontStyle = FontStyles.Bold;
                qt.color = new Color(0.30f, 0.55f, 0.15f); qt.alignment = TextAlignmentOptions.Center;
                qt.raycastTarget = false;
                Anchor((RectTransform)qty.transform, 1f, 1f, new Vector2(-16f, -10f), new Vector2(40f, 18f), new Vector2(0.5f, 0.5f));

                var status = new GameObject("Img_Status", typeof(RectTransform), typeof(Image));
                status.transform.SetParent(card.transform, false);
                var st = status.GetComponent<Image>();
                st.color = new Color(0.42f, 0.78f, 0.30f, 0.35f);
                if (skin.cardSelectedGlow != null)
                {
                    st.sprite = skin.cardSelectedGlow;
                    st.type = Image.Type.Sliced;
                    st.color = Color.white;
                }
                st.raycastTarget = false;
                Stretch((RectTransform)status.transform, 0f, 0f, 1f, 1f);
                status.SetActive(false);

                // Sếp 2026-08-27: thẻ phải "nhún" khi chạm + có bóng đổ mềm cho khối bo góc.
                var juice = card.AddComponent<UIJuiceFeedback>();
                juice.SetSound(UIJuiceFeedback.SoundType.Ingredient);
                var cardShadow = card.AddComponent<UnityEngine.UI.Shadow>();
                cardShadow.effectColor = new Color(0.25f, 0.15f, 0.05f, 0.35f);
                cardShadow.effectDistance = new Vector2(0f, -4f);

                var sel = card.AddComponent<SelectableIngredientCard>();
                sel.SetIngredientData(data);
                sel.setIdItem(data.id);
                var txtRef = qty.GetComponent<TMP_Text>();
                // txtQuantity là [SerializeField] private — set qua SetQuantityFromKitchen sau khi gán field bằng reflection
                var fld = typeof(SelectableIngredientCard).GetField("txtQuantity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fld?.SetValue(sel, txtRef);

                _cards[data.id != null ? data.id.Trim().ToLower() : ""] = sel;
            }

            // Đăng ký với SelectionManager CŨ — Init từng card + bật chọn
            selection.RegisterAllLeftCards(_gridIngredients, _gridSeasonings);
            selection.EnableIngredientSelection();
            RefreshCardQuantities();

            // Ô trống + nút mua thêm slot (mockup: "Ô trống" + "Mở 7 ô") — cả nguyên liệu lẫn gia vị
            BuildSlotShop(_gridIngredients, "ing");
            BuildSlotShop(_gridSeasonings, "sea");
            Loc.RequestRescan(); // the khay + o trong vua dung ⇒ xin quet lai
        }

        /// <summary>
        /// Nhan lai cac the nguyen lieu/gia vi DA CO trong Hierarchy thay vi dung moi:
        /// dung lai so tra _cards roi dang ky voi SelectionManager nhu binh thuong.
        /// Id lay tu ten object ("Card_&lt;id&gt;") nen khong phu thuoc vao field private.
        /// </summary>
        private void DungLaiTheKhayCoSan()
        {
            _cards.Clear();

            // Xay bang tra nhanh IngredientData theo id
            System.Collections.Generic.Dictionary<string, IngredientData> dataMap = null;
            if (allIngredients != null && allIngredients.Length > 0)
            {
                dataMap = new System.Collections.Generic.Dictionary<string, IngredientData>();
                foreach (var d in allIngredients)
                {
                    if (d != null && !string.IsNullOrEmpty(d.id))
                        dataMap[d.id.Trim().ToLower()] = d;
                }
            }

            var fldTxtQty = typeof(SelectableIngredientCard).GetField("txtQuantity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            void Nhan(Transform khung)
            {
                if (khung == null) return;
                for (int i = 0; i < khung.childCount; i++)
                {
                    var child = khung.GetChild(i);
                    if (!child.name.StartsWith("Card_")) continue;

                    // Dam bao co SelectableIngredientCard
                    var sel = child.GetComponent<SelectableIngredientCard>();
                    if (sel == null)
                        sel = child.gameObject.AddComponent<SelectableIngredientCard>();

                    string id = child.name.Substring(5).Trim().ToLower();

                    // Gan IngredientData neu chua co
                    var dat = sel.GetIngredientData();
                    if (dat == null && dataMap != null && dataMap.ContainsKey(id))
                    {
                        dat = dataMap[id];
                        sel.SetIngredientData(dat);
                    }
                    if (dat != null && !string.IsNullOrEmpty(dat.id))
                        id = dat.id.Trim().ToLower();

                    // Gan idItem
                    sel.setIdItem(id);

                    // Gan isSeasoning
                    if (dat != null)
                        sel.isSeasoning = (dat.kind == IngredientKind.Seasoning);

                    ChuanHoaSoLuongThe(child);

                    // Noi txtQuantity neu chua co
                    if (fldTxtQty != null && fldTxtQty.GetValue(sel) == null)
                    {
                        var txtQ = child.Find("Qty_Badge/Txt_Quantity") ?? child.Find("Txt_Quantity");
                        if (txtQ != null)
                        {
                            var tmp = txtQ.GetComponent<TMP_Text>();
                            if (tmp != null)
                                fldTxtQty.SetValue(sel, tmp);
                        }
                    }

                    // Gan icon tu IngredientData neu Image rong
                    if (dat != null && dat.icon != null)
                    {
                        var iconT = child.Find("Img_MainIcon");
                        if (iconT != null)
                        {
                            var im = iconT.GetComponent<Image>();
                            if (im != null && im.sprite == null)
                            {
                                im.sprite = dat.icon;
                                im.enabled = true;
                            }
                        }
                    }

                    _cards[id] = sel;
                }
            }

            Nhan(_gridIngredients);
            Nhan(_gridSeasonings);
            ChuyenTheDungKhay();   // [2026-09-24] the nam sai khay (vd ot, chanh doi sang nguyen lieu) -> dua ve dung khay

            selection.RegisterAllLeftCards(_gridIngredients, _gridSeasonings);
            selection.EnableIngredientSelection();
            RefreshCardQuantities();
            Loc.RequestRescan();

            Debug.Log($"[KitchenV2] Khoa layout — dung lai {_cards.Count} the khay co san, tu dong gan data cho card thieu.");
        }

        /// <summary>[2026-09-24] Theo IngredientData.kind: nguyen lieu ve khay Nguyen lieu, gia vi ve khay Gia vi.
        /// The chuyen sang duoc dat TRUOC o trong / nut mo o cua khay moi.</summary>
        private void ChuyenTheDungKhay()
        {
            if (_gridIngredients == null || _gridSeasonings == null) return;
            var canChuyen = new List<(Transform the, Transform dich)>();
            void Xet(Transform khung, bool laKhayGiaVi)
            {
                for (int i = 0; i < khung.childCount; i++)
                {
                    var c = khung.GetChild(i);
                    var sel = c.GetComponent<SelectableIngredientCard>();
                    var d = sel != null ? sel.GetIngredientData() : null;
                    if (d == null) continue;
                    bool laGiaVi = d.kind == IngredientKind.Seasoning;
                    if (laGiaVi != laKhayGiaVi) canChuyen.Add((c, laGiaVi ? _gridSeasonings : _gridIngredients));
                    sel.isSeasoning = laGiaVi;
                }
            }
            Xet(_gridIngredients, false);
            Xet(_gridSeasonings, true);
            foreach (var (the, dich) in canChuyen)
            {
                int viTri = dich.childCount;
                for (int i = 0; i < dich.childCount; i++)
                {
                    var n = dich.GetChild(i).name;
                    if (n.StartsWith("Slot_Empty_") || n == "Btn_BuySlots") { viTri = i; break; }
                }
                the.SetParent(dich, false);
                the.SetSiblingIndex(Mathf.Min(viTri, dich.childCount - 1));
            }
        }

        private const string SlotKeyPrefix = "kitchen_extra_slots_v2_";

        private int GetExtraSlots(string tab) => PlayerPrefs.GetInt(SlotKeyPrefix + tab, slotPackSize); // mặc định có sẵn 1 hàng ô trống như mockup

        private void BuildSlotShop(Transform parent, string tab)
        {
            if (parent == null) return;

            int empty = GetExtraSlots(tab);
            for (int i = 0; i < empty; i++)
            {
                var cell = new GameObject("Slot_Empty_" + i, typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(parent, false);
                var img = cell.GetComponent<Image>();
                img.color = new Color(0.72f, 0.62f, 0.50f, 0.4f);
                Skin9(cell, skin.cardLocked);
                if (skin.cardLocked != null) img.color = new Color(1f, 1f, 1f, 0.45f);
                var t = MakeText(cell.transform, "Txt", "+\nÔ trống", 11, new Color(0.55f, 0.45f, 0.35f));
                StretchText(t, 4f, 8f);
                t.alignment = TextAlignmentOptions.Center;
            }

            int bought = PlayerPrefs.GetInt(SlotKeyPrefix + "bought_" + tab, 0);
            int cost = slotPackBaseCostGold * (bought + 1);
            var buy = MakeButton((RectTransform)parent, "Btn_BuySlots",
                Loc.TF("+ Mở {0} ô\n{1:N0} vàng", slotPackSize, cost), new Color(0.30f, 0.55f, 0.90f), () => TryBuySlots(tab));
            Skin9(buy.gameObject, skin.btnGreen); // Sếp báo khung trống chưa có card bo góc (2026-08-27) — dùng lại art nút xanh đã có
            var bl = buy.GetComponentInChildren<TMP_Text>();
            if (bl != null) { bl.fontSize = 13; bl.fontStyle = FontStyles.Bold; }
            if (skin.iconGold != null)
            {
                var goldGo = new GameObject("Img_Gold", typeof(RectTransform), typeof(Image));
                goldGo.transform.SetParent(buy.transform, false);
                var gi = goldGo.GetComponent<Image>();
                gi.sprite = skin.iconGold; gi.preserveAspect = true; gi.raycastTarget = false;
                Anchor((RectTransform)goldGo.transform, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(16f, 16f), new Vector2(0.5f, 0f));
            }
        }

        /// <summary>Mua thêm gói ô khay bằng VÀNG (FarmEconomyManager cũ — không sửa logic tiền).</summary>
        private void TryBuySlots(string tab)
        {
            int bought = PlayerPrefs.GetInt(SlotKeyPrefix + "bought_" + tab, 0);
            int cost = slotPackBaseCostGold * (bought + 1);
            var eco = FarmEconomyManager.Instance;
            if (eco == null || !eco.SpendGold(cost))
            {
                if (_txtPrepToast != null)
                { _txtPrepToast.text = Loc.TF("Không đủ {0:N0} vàng để mở ô!", cost); _txtPrepToast.color = new Color(0.85f, 0.25f, 0.18f); }
                return;
            }
            PlayerPrefs.SetInt(SlotKeyPrefix + tab, GetExtraSlots(tab) + slotPackSize);
            PlayerPrefs.SetInt(SlotKeyPrefix + "bought_" + tab, bought + 1);
            PlayerPrefs.Save();
            if (_txtPrepToast != null)
            { _txtPrepToast.text = Loc.TF("Đã mở thêm {0} ô khay!", slotPackSize); _txtPrepToast.color = new Color(0.30f, 0.55f, 0.15f); }
            RebuildSlotShop(tab);
        }

        private void RebuildSlotShop(string tab)
        {
            var parent = tab == "ing" ? _gridIngredients : _gridSeasonings;
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i);
                if (c.name.StartsWith("Slot_Empty_") || c.name == "Btn_BuySlots")
                    Destroy(c.gameObject);
            }
            BuildSlotShop(parent, tab);
            Loc.RequestRescan();
        }

        private void BuildLockedCard(Transform parent, IngredientData data)
        {
            var card = new GameObject("Locked_" + data.id, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            var bg = card.GetComponent<Image>();
            bg.color = new Color(0.80f, 0.77f, 0.71f);
            Skin9(card, skin.cardLocked);
            bg.raycastTarget = false;

            var ico = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(card.transform, false);
            var im = ico.GetComponent<Image>();
            im.sprite = data.icon; im.enabled = data.icon != null;
            im.preserveAspect = true; im.raycastTarget = false;
            im.color = new Color(0.4f, 0.4f, 0.4f, 0.55f);
            Anchor((RectTransform)ico.transform, 0.5f, 1f, new Vector2(0f, -6f), new Vector2(40f, 40f), new Vector2(0.5f, 1f));

            if (skin.iconLock != null)
            {
                var lk = new GameObject("Img_Lock", typeof(RectTransform), typeof(Image));
                lk.transform.SetParent(card.transform, false);
                var li = lk.GetComponent<Image>();
                li.sprite = skin.iconLock; li.preserveAspect = true; li.raycastTarget = false;
                Anchor((RectTransform)lk.transform, 0.5f, 0.5f, new Vector2(0f, 2f), new Vector2(26f, 26f), new Vector2(0.5f, 0.5f));
            }

            var t = MakeText(card.transform, "Txt_Lv", Loc.TF("Cấp {0}\n{1}", data.unlockLevel, Loc.T(data.displayName)), 11, new Color(0.45f, 0.40f, 0.34f));
            Anchor(t.rectTransform, 0.5f, 0f, new Vector2(0f, 4f), new Vector2(106f, 32f), new Vector2(0.5f, 0f));
            t.alignment = TextAlignmentOptions.Center;
        }

        private void BuildActionButton()
        {
            var btn = MakeButton(_root, "Btn_Action", Loc.T("CHỌN NGUYÊN LIỆU"), new Color(0.62f, 0.58f, 0.53f), OnActionClicked);
            Anchor((RectTransform)btn.transform, 1f, 0f, new Vector2(-16f, 24f), new Vector2(264f, 80f), new Vector2(1f, 0f));
            _btnAction = btn;
            _imgAction = btn.GetComponent<Image>();
            _txtAction = btn.GetComponentInChildren<TMP_Text>();
            _txtAction.fontSize = 22;

            var sub = MakeText(btn.transform, "Txt_Sub", "chạm khay bên dưới", 13, new Color(0.28f, 0.15f, 0.07f));
            Anchor(sub.rectTransform, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(244f, 18f), new Vector2(0.5f, 0f));
            sub.alignment = TextAlignmentOptions.Center;
            _txtActionSub = sub;
        }

        /// <summary>Nút VỀ NÔNG TRẠI treo góc trái trên — gọi CookingSceneUI.BackToFarm() cũ (logic giữ nguyên).</summary>
        private void BuildBackFarmButton()
        {
            var b = MakeButton(_root, "Btn_BackFarm", Loc.T("VỀ NÔNG TRẠI"), new Color(0.62f, 0.42f, 0.20f), OnBackFarmClicked);
            Anchor((RectTransform)b.transform, 0f, 1f, new Vector2(14f, -2f), new Vector2(170f, 92f), new Vector2(0f, 1f));
            if (skin.btnBackFarm != null)
                SkinFlat(b.gameObject, skin.btnBackFarm);
            var lb = b.GetComponentInChildren<TMP_Text>();
            if (lb != null)
            {
                lb.text = Loc.T("VỀ NÔNG TRẠI");
                lb.fontStyle = FontStyles.Bold;
                lb.enableAutoSizing = true; lb.fontSizeMin = 10f; lb.fontSizeMax = 16f;
                lb.color = new Color(0.30f, 0.16f, 0.07f);
                Anchor(lb.rectTransform, 0.5f, 0f, new Vector2(0f, 16f), new Vector2(152f, 28f), new Vector2(0.5f, 0f));
                lb.alignment = TextAlignmentOptions.Center;
            }
        }

        private void OnBackFarmClicked()
        {
            var legacy = FindFirstObjectByType<CookingSceneUI>(FindObjectsInactive.Include);
            if (legacy != null) { legacy.BackToFarm(); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("SCN_Farm");
        }

        /// <summary>Lửa lò bằng prefab hạt (Area_fire_red). Cần canvas ScreenSpaceCamera mới đè lên UI được.
        /// Bỏ trống ovenFirePrefab = giữ lửa frame cũ. Chỉ chạy trong Play.</summary>
        private void TrySpawnFirePrefab()
        {
            if (ovenFirePrefab == null || !Application.isPlaying) return;
            // AN TOÀN LAYERING: UI cũ vẫn còn (Overlay). Đổi canvas mới sang ScreenSpaceCamera
            // sẽ khiến UI cũ đè lên trên (bug 2026-08-26). Chỉ chạy khi Sếp bật cờ SAU KHI xóa UI cũ.
            if (!useCameraCanvasForFire) return;
            var cam = Camera.main;
            if (cam == null || _canvas == null || _ovenRect == null) return;

            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = cam;
            _canvas.planeDistance = Mathf.Max(1f, cam.nearClipPlane + 2f);

            var mouth = _ovenRect.Find("Oven_Mouth") as RectTransform;
            var fx = Instantiate(ovenFirePrefab, mouth != null ? mouth : _ovenRect);
            fx.name = "FX_OvenFire_Prefab";
            fx.transform.localPosition = new Vector3(0f, -18f, -0.5f);
            fx.transform.localScale = Vector3.one * fireScale;
            foreach (var r in fx.GetComponentsInChildren<ParticleSystemRenderer>(true))
                r.sortingOrder = canvasSortingOrder + 1;

            // Tắt lửa frame để không trùng 2 lớp lửa
            if (_imgOvenFire != null) { _imgOvenFire.enabled = false; _imgOvenFire = null; }
        }

        /// <summary>Gan icon + ten nguyen lieu vao 1 o co san (khong doi vi tri/kich thuoc Sep chinh).</summary>
        public static void GanOChip(Transform o, IngredientData ing)
        {
            if (o == null) return;
            if (!o.gameObject.activeSelf) o.gameObject.SetActive(true);
            var img = o.Find("Img") != null ? o.Find("Img").GetComponent<Image>() : null;
            if (img != null) { img.sprite = ing != null ? ing.icon : null; img.enabled = img.sprite != null; img.preserveAspect = true; }
            var txt = o.Find("Txt") != null ? o.Find("Txt").GetComponent<TMP_Text>() : null;
            if (txt != null && ing != null)
            {
                string ten = Loc.T(ing.displayName);
                if (txt.text != ten) txt.text = ten;
            }
        }

        public Transform MakeNeedChip(Transform parent, IngredientData ing, float width = 66f, float height = 64f, float iconSize = 38f, int fontSize = 11, string tenO = null)
        {
            var chip = new GameObject(tenO ?? ("Chip_" + ing.id), typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            ((RectTransform)chip.transform).sizeDelta = new Vector2(width, height);
            chip.GetComponent<Image>().color = new Color(1f, 0.99f, 0.94f);
            Skin9(chip, skin.cardIngredient);
            // [2026-09-23] Bang chi tiet V3 co the bo goc "Card_Need" (tool Kitchen V3/10) => o nguyen lieu
            // dung CUNG sprite bo goc do (truoc la o vuong trang tron).
            var theNeed = parent != null && parent.parent != null ? parent.parent.Find("Card_Need") : null;
            var spBoGoc = theNeed != null ? theNeed.GetComponent<Image>() : null;
            if (spBoGoc != null && spBoGoc.sprite != null)
            {
                var ci = chip.GetComponent<Image>();
                ci.sprite = spBoGoc.sprite; ci.type = Image.Type.Sliced; ci.color = Color.white;
            }

            // [2026-09-23] Object MOI tao => dat rect TRUC TIEP. Anchor() bi KhoaLayout chan nen truoc day
            // icon nam giua o voi kich thuoc mac dinh 100x100 => to dung, de len chu "Ingredients needed".
            var gl = parent != null ? parent.GetComponent<GridLayoutGroup>() : null;
            if (gl != null) { width = gl.cellSize.x; height = gl.cellSize.y; }
            iconSize = Mathf.Min(width - 12f, height - 26f);

            var ico = new GameObject("Img", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(chip.transform, false);
            var im = ico.GetComponent<Image>();
            im.sprite = ing.icon; im.enabled = ing.icon != null;
            im.preserveAspect = true; im.raycastTarget = false;
            var irt = (RectTransform)ico.transform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f); irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0f, -4f); irt.sizeDelta = new Vector2(iconSize, iconSize);

            var t = MakeText(chip.transform, "Txt", Loc.T(ing.displayName), fontSize, new Color(0.36f, 0.20f, 0.09f));
            t.enableAutoSizing = true; t.fontSizeMin = 8f; t.fontSizeMax = Mathf.Max(fontSize, 13);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            var trt = t.rectTransform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0f); trt.pivot = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0f, 3f); trt.sizeDelta = new Vector2(width - 6f, 18f);
            t.alignment = TextAlignmentOptions.Center;
            return chip.transform;
        }

        // ── Skin helpers (K2) ──────────────────────────────────────

        /// <summary>Icon vàng nhỏ góc bảng đen, thay chữ "(+vàng)" — tự lên khi đội vẽ giao icon_gold (chờ art R7).</summary>
        private void EnsureChalkGoldIcon(Transform chalkT)
        {
            if (chalkT == null || skin.iconGold == null) return;
            if (chalkT.Find("Chalk_Gold") != null) return;
            var goldGo = new GameObject("Chalk_Gold", typeof(RectTransform), typeof(Image));
            goldGo.transform.SetParent(chalkT, false);
            var gi = goldGo.GetComponent<Image>();
            gi.sprite = skin.iconGold; gi.preserveAspect = true; gi.raycastTarget = false;
            Anchor((RectTransform)goldGo.transform, 1f, 1f, new Vector2(-10f, -8f), new Vector2(22f, 22f), new Vector2(1f, 1f));
        }

        private static Sprite _dotSprite;
        /// <summary>Sprite hình tròn 24px vẽ bằng code — dùng cho chấm màu, không cần art.</summary>
        private static Sprite GetDotSprite()
        {
            if (_dotSprite != null) return _dotSprite;
            const int S = 24;
            var tex = new Texture2D(S, S, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.DontSave;
            float c = (S - 1) * 0.5f, r = S * 0.5f - 1f;
            var px = new Color[S * S];
            for (int yy = 0; yy < S; yy++)
                for (int xx = 0; xx < S; xx++)
                {
                    float d = Mathf.Sqrt((xx - c) * (xx - c) + (yy - c) * (yy - c));
                    float a = Mathf.Clamp01(r - d + 0.5f); // mép mềm 1px
                    px[yy * S + xx] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px); tex.Apply();
            _dotSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            _dotSprite.hideFlags = HideFlags.DontSave;
            return _dotSprite;
        }

        private static void Skin9(GameObject go, Sprite sp)
        {
            if (KhoaLayout) return;
            if (go == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            if (sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.sprite = SkinKit.BoGoc(12f);
                img.type = Image.Type.Sliced;
            }
        }

        private static void SkinFlat(GameObject go, Sprite sp, bool preserveAspect = true)
        {
            if (KhoaLayout) return;
            if (go == null || sp == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sp; img.type = Image.Type.Simple;
            img.preserveAspect = preserveAspect; img.color = Color.white;
        }

        private static void SkinTiled(GameObject go, Sprite sp)
        {
            if (KhoaLayout) return;
            if (go == null || sp == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sp; img.type = Image.Type.Tiled; img.color = Color.white;
        }

        private Image MakeDecor(RectTransform parent, string name, Sprite sp, float ax, float ay, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            if (sp == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sp; img.preserveAspect = true; img.raycastTarget = false;
            Anchor((RectTransform)go.transform, ax, ay, pos, size, pivot);
            return img;
        }

        // ── Animation runtime (K2): mèo vẫy, lửa lò, khói, % nướng, card punch ──

        private Image _imgManeki, _imgOvenFire, _imgOvenGlow;
        private RectTransform _ovenRect;
        private float _animT;
        private int _animFrame;
        private float _smokeT2;
        private float _ovenFakeProgress;
        private readonly Dictionary<SelectableIngredientCard, bool> _lastSelected = new Dictionary<SelectableIngredientCard, bool>();

        private void LateUpdate()
        {
            if (!_built) return;
            _animT += Time.unscaledDeltaTime;
            if (_animT >= 0.22f)
            {
                _animT = 0f;
                _animFrame++;

                if (_imgManeki != null && skin.manekiIdle != null && skin.manekiIdle.Length > 0)
                    _imgManeki.sprite = skin.manekiIdle[_animFrame % skin.manekiIdle.Length];

                if (_imgOvenFire != null && skin.ovenFire != null && skin.ovenFire.Length > 0)
                {
                    _imgOvenFire.enabled = _ovenBusy;
                    if (_ovenBusy) _imgOvenFire.sprite = skin.ovenFire[_animFrame % skin.ovenFire.Length];
                }
                if (_imgOvenGlow != null)
                {
                    _imgOvenGlow.enabled = _ovenBusy;
                    if (_ovenBusy)
                        _imgOvenGlow.color = new Color(1f, 1f, 1f, 0.5f + 0.3f * Mathf.PingPong(_animFrame * 0.25f, 1f));
                }
            }

            // % nướng giả lập khi lò bận (flow thật kết thúc bằng event OnDishCooked)
            if (_imgOvenFill != null)
            {
                if (_ovenBusy)
                {
                    // [2026-09-23] Tien do THAT theo thoi gian nau cua mon (thay cho % gia lap).
                    float that = challenge != null && challenge.IsCooking ? challenge.CookProgress01 : -1f;
                    _ovenFakeProgress = that >= 0f ? that : Mathf.MoveTowards(_ovenFakeProgress, 0.92f, Time.unscaledDeltaTime * 0.25f);
                    _imgOvenFill.fillAmount = _ovenFakeProgress;
                }
                else if (_ovenFakeProgress > 0f)
                {
                    _imgOvenFill.fillAmount = 1f;
                    _ovenFakeProgress = 0f;
                }
            }

            // Khói bốc từ ống khói lò khi đang nấu
            if (khoiLo && _ovenBusy && skin.smokePuff != null && _ovenRect != null)
            {
                _smokeT2 -= Time.unscaledDeltaTime;
                if (_smokeT2 <= 0f)
                {
                    _smokeT2 = 0.45f;
                    StartCoroutine(RoutineOvenSmoke());
                }
            }
        }

        private System.Collections.IEnumerator RoutineOvenSmoke()
        {
            var go = new GameObject("OvenSmoke", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root, false);
            var img = go.GetComponent<Image>();
            img.sprite = skin.smokePuff; img.preserveAspect = true; img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            Vector2 start = _ovenRect.anchoredPosition + new Vector2(Random.Range(-8f, 8f), 30f);
            rt.anchorMin = rt.anchorMax = _ovenRect.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = start;
            rt.sizeDelta = new Vector2(34f, 34f);

            float t = 0f, dur = 1.3f;
            while (t < dur && img != null)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                rt.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 8f - 12f * k, 80f * k);
                rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.4f, k);
                img.color = new Color(1f, 1f, 1f, 0.85f * (1f - k * k));
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        /// <summary>Punch scale thẻ khi trạng thái chọn đổi (gọi từ RefreshDynamic).</summary>
        private void PunchChangedCards()
        {
            foreach (var kv in _cards)
            {
                var card = kv.Value;
                if (card == null) continue;
                bool was = _lastSelected.TryGetValue(card, out bool b) && b;
                if (card.IsSelected != was)
                {
                    _lastSelected[card] = card.IsSelected;
                    StartCoroutine(RoutinePunch(card.transform));
                }
            }
        }

        private System.Collections.IEnumerator RoutinePunch(Transform tr)
        {
            float t = 0f, dur = 0.18f;
            while (t < dur && tr != null)
            {
                t += Time.unscaledDeltaTime;
                tr.localScale = Vector3.one * (1f + 0.14f * Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI));
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        // ── UI factory helpers (skin tạm K1) ───────────────────────

        /// <summary>[FIX 2026-09-02] Bảo đảm Need_Chips dùng GridLayoutGroup mà KHÔNG bao giờ throw.
        /// HorizontalLayoutGroup bake cũ phải gỡ bằng DestroyImmediate (Destroy() deferred làm
        /// AddComponent thất bại — chính là NullReference giết cả init scene bếp 2026-09-01).
        /// Trả null nếu vẫn không gắn được — caller bỏ qua, chip vẫn hiện (không xếp lưới).</summary>
        private static GridLayoutGroup EnsureNeedChipsGrid(Transform root)
        {
            if (root == null) return null;

            var gl = root.GetComponent<GridLayoutGroup>();
            // [2026-09-23] Da co Grid (Sep/tool chinh o Edit mode) => GIU NGUYEN o/khoang cach, khong ghi de.
            if (gl != null) return gl;
            if (gl == null)
            {
                var oldHl = root.GetComponent<HorizontalLayoutGroup>();
                if (oldHl != null) DestroyImmediate(oldHl); // immediate — cùng frame AddComponent mới ăn

                gl = root.gameObject.AddComponent<GridLayoutGroup>();
            }
            if (gl == null)
            {
                Debug.LogWarning("[KitchenV2] Không gắn được GridLayoutGroup cho Need_Chips (còn LayoutGroup khác?) — bỏ qua, không chặn init.");
                return null;
            }

            gl.cellSize = new Vector2(66f, 64f);
            gl.spacing = new Vector2(6f, 6f);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 4;
            gl.childAlignment = TextAnchor.UpperLeft;
            return gl;
        }

        private static Sprite LoadKitchenSprite(string path)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(path))
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
                var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                if (all != null)
                {
                    foreach (var obj in all)
                        if (obj is Sprite s) return s;
                }
            }
#endif
            return null;
        }

        private void EnsureSkinLoaded()
        {
            if (skin == null) skin = new KitchenSkin();
            const string P = "Assets/Export_Kitchen_UI_Package/Sprites";
            if (skin.wallTile == null) skin.wallTile = LoadKitchenSprite($"{P}/bg_wall_tile.png");
            if (skin.floorTile == null) skin.floorTile = LoadKitchenSprite($"{P}/bg_floor_tile.png");
            if (skin.panelBoard == null) skin.panelBoard = LoadKitchenSprite($"{P}/panel_board_9slice.png");
            if (skin.panelPaper == null) skin.panelPaper = LoadKitchenSprite($"{P}/panel_paper_9slice.png");
            if (skin.cardIngredient == null) skin.cardIngredient = LoadKitchenSprite($"{P}/card_ingredient.png");
            if (skin.cardSelectedGlow == null) skin.cardSelectedGlow = LoadKitchenSprite($"{P}/card_selected_glow.png");
            if (skin.cardLocked == null) skin.cardLocked = LoadKitchenSprite($"{P}/card_locked.png");
            if (skin.btnGreen == null) skin.btnGreen = LoadKitchenSprite($"{P}/btn_big_green.png");
            if (skin.btnGray == null) skin.btnGray = LoadKitchenSprite($"{P}/btn_big_gray.png");
            if (skin.btnRedSmall == null) skin.btnRedSmall = LoadKitchenSprite($"{P}/btn_red_small.png");
            if (skin.btnPaperSmall == null) skin.btnPaperSmall = LoadKitchenSprite($"{P}/btn_paper_small.png");
            if (skin.tabOn == null) skin.tabOn = LoadKitchenSprite($"{P}/tab_pill_on.png");
            if (skin.tabOff == null) skin.tabOff = LoadKitchenSprite($"{P}/tab_pill_off.png");
            if (skin.btnBackFarm == null) skin.btnBackFarm = LoadKitchenSprite($"{P}/btn_back_farm_sign.png");
            if (skin.ribbon == null) skin.ribbon = LoadKitchenSprite($"{P}/ribbon_header.png");
            if (skin.chalkboard == null) skin.chalkboard = LoadKitchenSprite($"{P}/chalkboard_menu.png");
            if (skin.tasteTrack == null) skin.tasteTrack = LoadKitchenSprite($"{P}/track_taste_bg.png");
            if (skin.tasteFill == null) skin.tasteFill = LoadKitchenSprite($"{P}/fill_taste_green.png");
            if (skin.chipTaste == null) skin.chipTaste = LoadKitchenSprite($"{P}/chip_taste.png");
            if (skin.iconLock == null) skin.iconLock = LoadKitchenSprite($"{P}/icon_lock_small.png");
            if (skin.iconGold == null) skin.iconGold = LoadKitchenSprite("Assets/Assetsgame/Icon_vang.png") ?? LoadKitchenSprite($"{P}/icon_gold_coin.png");
        }

        private void ApplyFont(TMP_Text t)
        {
            if (t == null) return;
            var f = SkinKit.FontVo;
            if (f != null)
            {
                t.font = f;
                if (f.material != null) t.fontSharedMaterial = f.material;
            }
        }

        private GameObject MakePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = SkinKit.BoGoc(14f);
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string text, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            ApplyFont(t);
            t.text = text; t.fontSize = size; t.color = color;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>
        /// [VÒNG 13] 4 nút lọc Tất cả / Dễ / Vừa / Khó — nhớ lại để tô trạng thái đang chọn.
        /// </summary>
        private GameObject[] _listTabs;

        /// <summary>
        /// [VÒNG 13] Tô nút đang chọn bằng tab_pill_on, 3 nút còn lại tab_pill_off.
        /// </summary>
        private void CapNhatTabLoc()
        {
            if (_listTabs == null) return;

            for (int i = 0; i < _listTabs.Length; i++)
            {
                var go = _listTabs[i];
                if (go == null) continue;

                bool dangChon = (i - 1) == _listFilter;
                var img = go.GetComponent<Image>();

                if (skin != null && skin.tabOn != null && skin.tabOff != null)
                {
                    Skin9(go, dangChon ? skin.tabOn : skin.tabOff);
                }
                else
                {
                    if (img != null)
                    {
                        img.sprite = SkinKit.BoGoc(10f);
                        img.type = Image.Type.Sliced;
                        img.color = dangChon ? new Color(0.98f, 0.86f, 0.58f)
                                             : new Color(0.86f, 0.76f, 0.58f);
                    }
                }

                var txt = go.GetComponentInChildren<TMP_Text>(true);
                if (txt != null)
                {
                    ApplyFont(txt);
                    txt.color = dangChon ? new Color(0.28f, 0.15f, 0.05f)
                                         : new Color(0.48f, 0.38f, 0.28f);
                }
            }
        }

        private Button MakeButton(RectTransform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = SkinKit.BoGoc(12f);
            img.type = Image.Type.Sliced;
            img.color = color;
            var btn = go.GetComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(onClick);
            else btn.interactable = false;

            if (!string.IsNullOrEmpty(label))
            {
                var t = MakeText(go.transform, "Txt_Label", label, 15, Color.white);
                StretchText(t, 4f, 0f);
                t.alignment = TextAlignmentOptions.Center;
                t.color = new Color(0.25f, 0.15f, 0.08f);
            }
            return btn;
        }

        private Image MakeFill(RectTransform track, Color color)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(track, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.sprite = GetDotSprite();
            img.fillAmount = 0f;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f); rt.offsetMax = new Vector2(-2f, -2f);
            return img;
        }

        // ════════════════════════════════════════════════════════════════════
        //  KHOA LAYOUT  [2026-09-21]
        //  Sep muon UI song trong Hierarchy va chinh tay bang keo tha, chinh xong
        //  Ctrl+S la vinh vien. Van de: mot so cho trong duong Bind/Refresh van ghi de
        //  sizeDelta / anchoredPosition, lam mat chinh tay ngay khi bam Play.
        //  Khi KhoaLayout = true: MOI phep ghi vi tri / kich thuoc do CODE thuc hien
        //  deu bi chan. Code chi con doi CHU, SO, SPRITE, mau va bat/tat object.
        //  Bat bang tool: Tools > Farm Game > Kitchen: Dong bang UI thanh Hierarchy
        //  hoac tick o "Khoa layout" tren component trong Inspector.
        // ════════════════════════════════════════════════════════════════════
        public static bool KhoaLayout;

        /// <summary>
        /// [2026-09-22] TRUE khi tool Editor "Va phan thieu" dang chay. Luc do TUYET DOI
        /// khong duoc dong vao nhung gi Sep da chinh tay: khong ghi de anchor (KhoaLayout = true
        /// lo viec do) va khong xoa con cua 4 khung dong (co nay lo viec do).
        /// </summary>
        public static bool DangVaTrongEditor;

        private static void Stretch(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            if (KhoaLayout) return;
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(GameObject go, float xMin, float yMin, float xMax, float yMax)
            => Stretch((RectTransform)go.transform, xMin, yMin, xMax, yMax);

        private static void StretchText(TMP_Text t, float padX, float padY)
        {
            if (KhoaLayout) return;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void Anchor(GameObject go, float ax, float ay, Vector2 pos, Vector2 size, Vector2 pivot)
            => Anchor((RectTransform)go.transform, ax, ay, pos, size, pivot);

        private static void Anchor(RectTransform rt, float ax, float ay, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            if (KhoaLayout) return;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }


}
