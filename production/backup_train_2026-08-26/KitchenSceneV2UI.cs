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
    public class KitchenSceneV2UI : MonoBehaviour
    {
        public static KitchenSceneV2UI Instance { get; private set; }

        [Header("Data — tool Setup gán")]
        [SerializeField] private IngredientData[] allIngredients;
        [SerializeField] private ListDishData dishBook;

        [Header("Managers — trống thì tự tìm")]
        [SerializeField] private CookingChallengeManager challenge;
        [SerializeField] private CookingSelectionManager selection;

        [Header("Layout")]
        [SerializeField] private int canvasSortingOrder = 5;
        [SerializeField] private float pollInterval = 0.15f;

        // ── Runtime refs ────────────────────────────────────────────
        private Canvas _canvas;
        private RectTransform _root;

        private TMP_Text _txtChef, _txtGold, _txtOrderName, _txtOrderChips;
        private Image _imgChefExpFill, _imgOrderIcon;

        private GameObject _boardDetail, _boardList;
        private TMP_Text _txtDishName, _txtDishMeta, _txtNeedTitle, _txtRewards, _txtProjection;
        private Image _imgDishIcon;
        private Transform _needChipsRoot;
        private readonly FlavorRow[] _flavorRows = new FlavorRow[5];
        private Transform _dishListContent;
        private int _listFilter = -1; // -1 = tất cả, else (int)DishDifficulty

        private TMP_Text _txtOvenState, _txtSentCount, _txtChalk, _txtPrepToast;
        private Image _imgOvenFill;
        private Button _btnPlating;
        private TMP_Text _txtPlating;

        private Transform _gridIngredients, _gridSeasonings;
        private GameObject _tabIngredients, _tabSeasonings;
        private TMP_Text _txtTabIng, _txtTabSea;
        private Button _btnClearAll;

        private Button _btnAction;
        private Image _imgAction;
        private TMP_Text _txtAction, _txtActionSub;

        private readonly Dictionary<string, SelectableIngredientCard> _cards = new Dictionary<string, SelectableIngredientCard>();
        private float _pollT;
        private bool _built;
        private bool _ovenBusy;

        private const string SentCountKey = "kitchen_sent_dishes_v2";

        private struct FlavorRow
        {
            public TMP_Text label;
            public Image fill;
            public RectTransform marker;
            public TMP_Text value;
        }

        // ── Vòng đời ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (challenge == null) challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            if (selection == null) selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

            EnsureBuilt();
            RaiseLegacyOverlays();
            BuildTrayCards();
            PickDefaultDish();
            RefreshAll();
        }

        private void OnEnable()
        {
            CookingChallengeManager.OnCookStarted  += HandleCookStarted;
            CookingChallengeManager.OnDishCooked   += HandleDishCooked;
            CookingChallengeManager.OnDishFailed   += HandleDishFailed;
            CookingChallengeManager.OnDishCollected += HandleDishCollected;
        }

        private void OnDisable()
        {
            CookingChallengeManager.OnCookStarted  -= HandleCookStarted;
            CookingChallengeManager.OnDishCooked   -= HandleDishCooked;
            CookingChallengeManager.OnDishFailed   -= HandleDishFailed;
            CookingChallengeManager.OnDishCollected -= HandleDishCollected;
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
            SetText(_txtOvenState, "LÒ ĐANG CHÁY...");
            SetText(_txtPrepToast, $"Sơ chế: {(d != null ? d.dishName : "")}");
        }

        private void HandleDishCooked(DishData d, int score)
        {
            _ovenBusy = false;
            SetText(_txtOvenState, $"XONG! {score}đ");
            SetText(_txtPrepToast, "Chạm bàn trình bày để cất vào kho →");
        }

        private void HandleDishFailed(DishData d, int score)
        {
            _ovenBusy = false;
            SetText(_txtOvenState, $"HỎNG... {score}đ");
            SetText(_txtPrepToast, "Chọn lại nguyên liệu rồi nấu tiếp nhé!");
        }

        private void HandleDishCollected(DishData d)
        {
            int n = PlayerPrefs.GetInt(SentCountKey, 0) + 1;
            PlayerPrefs.SetInt(SentCountKey, n);
            SetText(_txtSentCount, $"Đã gửi {n} món");
            SetText(_txtOvenState, "Lò đã nghỉ");
            SetText(_txtPrepToast, "");
        }

        // ── Actions ─────────────────────────────────────────────────

        private void OnActionClicked()
        {
            if (challenge == null) return;
            if (challenge.CookedDishOnPlate != null) return; // còn món trên dĩa — cất trước
            challenge.OnClickCookSubmit();
        }

        private void OnPlatingClicked()
        {
            challenge?.CollectCookedDishToWarehouse();
            RefreshDynamic();
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
            if (!detail) RebuildDishList();
        }

        private void PickDefaultDish()
        {
            if (challenge == null) return;
            if (challenge.CurrentDish != null) return;
            if (dishBook == null || dishBook.allDishes == null) return;

            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
            foreach (var d in dishBook.allDishes)
                if (d != null && d.unlockLevel <= lv) { challenge.SetCurrentDish(d); return; }
        }

        // ── Refresh ────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshStatic();
            RefreshDynamic();
        }

        private void RefreshStatic()
        {
            var dish = challenge != null ? challenge.CurrentDish : null;

            SetText(_txtOrderName, dish != null ? dish.dishName : "Chọn món trong sổ công thức");
            if (_imgOrderIcon != null)
            {
                _imgOrderIcon.sprite  = dish != null ? dish.dishSprite : null;
                _imgOrderIcon.enabled = _imgOrderIcon.sprite != null;
            }
            if (dish != null)
            {
                var f = dish.targetFlavor;
                SetText(_txtOrderChips, $"Ngọt {f.sweet} · Cay {f.spicy} · Chua {f.sour} · Đậm {f.umami} · Kết cấu {f.texture}");
            }
            else SetText(_txtOrderChips, "");

            SetText(_txtDishName, dish != null ? dish.dishName : "—");
            if (_imgDishIcon != null)
            {
                _imgDishIcon.sprite  = dish != null ? dish.dishSprite : null;
                _imgDishIcon.enabled = _imgDishIcon.sprite != null;
            }
            SetText(_txtDishMeta, dish != null ? $"{DiffName(dish.difficulty)} · Cấp {dish.unlockLevel}" : "");
            SetText(_txtRewards, dish != null ? $"+{dish.rewardGold} vàng   +{dish.rewardExp} EXP   Bán {dish.sellPrice}" : "");

            // Chip nguyên liệu cần
            if (_needChipsRoot != null && dish != null)
            {
                for (int i = _needChipsRoot.childCount - 1; i >= 0; i--)
                    Destroy(_needChipsRoot.GetChild(i).gameObject);
                if (dish.requiredIngredients != null)
                    foreach (var ing in dish.requiredIngredients)
                        if (ing != null) MakeNeedChip(_needChipsRoot, ing);
            }

            // Bảng đen món hôm nay
            var daily = DailySpecialManager.Instance;
            if (daily != null && _txtChalk != null)
            {
                var sb = new System.Text.StringBuilder("MÓN HÔM NAY (+vàng)\n");
                foreach (var d in daily.TodayDishes)
                    if (d != null) sb.Append("· ").Append(d.dishName).Append('\n');
                _txtChalk.text = sb.ToString();
            }
        }

        private void RefreshDynamic()
        {
            // TopBar
            var prog = PlayerProgressManager.Instance;
            if (prog != null)
            {
                SetText(_txtChef, $"Bếp trưởng · Cấp {prog.Level}   {prog.CurrentExp}/{prog.RequiredExpCurrentLevel}");
                if (_imgChefExpFill != null && prog.RequiredExpCurrentLevel > 0)
                    _imgChefExpFill.fillAmount = Mathf.Clamp01((float)prog.CurrentExp / prog.RequiredExpCurrentLevel);
            }
            if (FarmEconomyManager.Instance != null)
                SetText(_txtGold, FarmEconomyManager.Instance.Gold.ToString("N0"));

            // Số lượng kho bếp trên thẻ
            RefreshCardQuantities();

            var dish   = challenge != null ? challenge.CurrentDish : null;
            var selIng = selection != null ? selection.GetSelectedIngredientCards() : null;
            var selSea = selection != null ? selection.GetSelectedSeasoningCards() : null;
            int nIng = CountNonNull(selIng);
            int nSea = CountNonNull(selSea);

            SetText(_txtTabIng, $"Nguyên liệu  {nIng}/4");
            SetText(_txtTabSea, $"Gia vị  {nSea}/3");

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
                    SetText(_txtProjection, $"Điểm dự kiến:  {result.finalScore}đ");
                }
                else SetText(_txtProjection, "Điểm dự kiến:  — đ");
            }

            // Nút hành động 3 trạng thái + bàn trình bày
            bool plateReady = challenge != null && challenge.CookedDishOnPlate != null;
            bool cooking    = (challenge != null && challenge.IsCooking) || _ovenBusy;

            if (_btnPlating != null) _btnPlating.interactable = plateReady;
            SetText(_txtPlating, plateReady ? "CHẠM ĐỂ CẤT VÀO KHO!" : "Trình bày");

            if (_btnAction != null && _imgAction != null)
            {
                if (cooking)
                {
                    _btnAction.interactable = false;
                    _imgAction.color = new Color(0.55f, 0.52f, 0.48f);
                    SetText(_txtAction, "ĐANG NẤU...");
                    SetText(_txtActionSub, "");
                }
                else if (plateReady)
                {
                    _btnAction.interactable = false;
                    _imgAction.color = new Color(0.55f, 0.52f, 0.48f);
                    SetText(_txtAction, "MÓN TRÊN DĨA");
                    SetText(_txtActionSub, "cất vào kho trước đã");
                }
                else if (nIng + nSea > 0)
                {
                    _btnAction.interactable = true;
                    _imgAction.color = new Color(0.36f, 0.72f, 0.26f);
                    SetText(_txtAction, "NẤU!");
                    SetText(_txtActionSub, $"{nIng} nguyên liệu · {nSea} gia vị");
                }
                else
                {
                    _btnAction.interactable = false;
                    _imgAction.color = new Color(0.62f, 0.58f, 0.53f);
                    SetText(_txtAction, "CHỌN NGUYÊN LIỆU");
                    SetText(_txtActionSub, "chạm khay bên dưới");
                }
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
                    if (!string.IsNullOrEmpty(kv.Key)) map[kv.Key.Trim().ToLower()] = kv.Value;

            foreach (var kv in _cards)
            {
                int qty = map.TryGetValue(kv.Key, out int v) ? v : 0;
                kv.Value.SetQuantityFromKitchen(qty);
            }
        }

        private void SetFlavorRow(int i, string label, int cur, int target)
        {
            var row = _flavorRows[i];
            if (row.label == null) return;

            row.label.text = label;
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
            if (row.marker != null)
            {
                var parent = row.marker.parent as RectTransform;
                float w = parent != null ? parent.rect.width : 140f;
                row.marker.anchoredPosition = new Vector2(w * Mathf.Clamp01(target / barMax), 0f);
                row.marker.gameObject.SetActive(target > 0);
            }
            if (row.value != null) row.value.text = $"{cur}/{target}";
        }

        // ── Helpers ────────────────────────────────────────────────

        private static string DiffName(DishDifficulty d) =>
            d == DishDifficulty.Easy ? "Dễ" : d == DishDifficulty.Hard ? "Khó" : "Vừa";

        private static int CountNonNull(List<SelectableIngredientCard> list)
        {
            if (list == null) return 0;
            int n = 0;
            foreach (var c in list) if (c != null) n++;
            return n;
        }

        private static void SetText(TMP_Text t, string s) { if (t != null) t.text = s; }

        /// <summary>Nâng sorting canvas của minigame + popup CŨ lên trên UI v2 (runtime, không sửa scene).</summary>
        private void RaiseLegacyOverlays()
        {
            RaiseOne(FindFirstObjectByType<CookingTimingMiniGameUI>(FindObjectsInactive.Include));
            RaiseOne(FindFirstObjectByType<LetterMiniGame>(FindObjectsInactive.Include));
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
            var floor = MakePanel(_root, "BG_Floor", new Color(0.82f, 0.66f, 0.45f));
            Stretch(floor, 0f, 0f, 1f, 0.32f);

            BuildTopBar();
            BuildOrderBanner();
            BuildRecipeBoard();
            BuildStage();
            BuildTray();
            BuildActionButton();

            _built = true;
        }

        private void BuildTopBar()
        {
            var chef = MakePanel(_root, "Pill_Chef", new Color(0.98f, 0.94f, 0.84f));
            Anchor(chef, 0f, 1f, new Vector2(16f, -14f), new Vector2(300f, 46f), new Vector2(0f, 1f));
            _txtChef = MakeText(chef.transform, "Txt_Chef", "Bếp trưởng", 19, new Color(0.36f, 0.20f, 0.09f));
            StretchText(_txtChef, 12f, 2f);
            var expTrack = MakePanel((RectTransform)chef.transform, "Exp_Track", new Color(0.55f, 0.42f, 0.28f));
            Anchor(expTrack, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(260f, 8f), new Vector2(0.5f, 0f));
            _imgChefExpFill = MakeFill((RectTransform)expTrack.transform, new Color(0.42f, 0.78f, 0.30f));

            var gold = MakePanel(_root, "Pill_Gold", new Color(0.98f, 0.94f, 0.84f));
            Anchor(gold, 0f, 1f, new Vector2(16f, -66f), new Vector2(180f, 40f), new Vector2(0f, 1f));
            _txtGold = MakeText(gold.transform, "Txt_Gold", "0", 20, new Color(0.72f, 0.52f, 0.08f));
            StretchText(_txtGold, 12f, 0f);
        }

        private void BuildOrderBanner()
        {
            var banner = MakePanel(_root, "Order_Banner", new Color(0.52f, 0.33f, 0.16f));
            Anchor(banner, 0.5f, 1f, new Vector2(60f, -10f), new Vector2(430f, 96f), new Vector2(0.5f, 1f));

            var title = MakeText(banner.transform, "Txt_Title", "ĐƠN CỦA KHÁCH", 17, new Color(1f, 0.85f, 0.4f));
            Anchor((RectTransform)title.transform.parent == null ? null : title.rectTransform, 0.5f, 1f, new Vector2(0f, -4f), new Vector2(400f, 24f), new Vector2(0.5f, 1f));
            title.alignment = TextAlignmentOptions.Center;

            var card = MakePanel((RectTransform)banner.transform, "Order_Card", new Color(0.98f, 0.94f, 0.84f));
            Anchor(card, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(406f, 58f), new Vector2(0.5f, 0f));

            var iconGo = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(card.transform, false);
            _imgOrderIcon = iconGo.GetComponent<Image>();
            _imgOrderIcon.preserveAspect = true;
            _imgOrderIcon.raycastTarget = false;
            Anchor((RectTransform)iconGo.transform, 0f, 0.5f, new Vector2(30f, 0f), new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));

            _txtOrderName = MakeText(card.transform, "Txt_Name", "—", 19, new Color(0.36f, 0.20f, 0.09f));
            Anchor(_txtOrderName.rectTransform, 0f, 1f, new Vector2(62f, -4f), new Vector2(330f, 26f), new Vector2(0f, 1f));

            _txtOrderChips = MakeText(card.transform, "Txt_Chips", "", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(_txtOrderChips.rectTransform, 0f, 0f, new Vector2(62f, 4f), new Vector2(330f, 20f), new Vector2(0f, 0f));
        }

        private void BuildRecipeBoard()
        {
            var board = MakePanel(_root, "Recipe_Board", new Color(0.52f, 0.33f, 0.16f));
            Anchor(board, 0f, 1f, new Vector2(14f, -118f), new Vector2(318f, 520f), new Vector2(0f, 1f));

            var hdr = MakeText(board.transform, "Txt_Header", "BẢNG CÔNG THỨC", 18, new Color(1f, 0.85f, 0.4f));
            Anchor(hdr.rectTransform, 0.5f, 1f, new Vector2(0f, -6f), new Vector2(300f, 26f), new Vector2(0.5f, 1f));
            hdr.alignment = TextAlignmentOptions.Center;

            // ── DETAIL ──
            var det = MakePanel((RectTransform)board.transform, "Board_Detail", new Color(0.98f, 0.94f, 0.84f));
            Stretch((RectTransform)det.transform, 0.02f, 0.02f, 0.98f, 0.93f);
            _boardDetail = det;
            var dt = (RectTransform)det.transform;

            var btnOther = MakeButton(dt, "Btn_OtherDish", "‹ Xem món khác", new Color(0.93f, 0.80f, 0.55f), () => ShowBoardDetail(false));
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

            var chips = new GameObject("Need_Chips", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            chips.transform.SetParent(dt, false);
            var hl = chips.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8f; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = false; hl.childControlHeight = false;
            Anchor((RectTransform)chips.transform, 0f, 1f, new Vector2(12f, -134f), new Vector2(290f, 64f), new Vector2(0f, 1f));
            _needChipsRoot = chips.transform;

            var tasteTitle = MakeText(dt, "Txt_TasteTitle", "VỊ KHÁCH MUỐN · vạch đỏ là mốc", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(tasteTitle.rectTransform, 0f, 1f, new Vector2(12f, -206f), new Vector2(280f, 18f), new Vector2(0f, 1f));

            for (int i = 0; i < 5; i++)
                BuildFlavorRow(dt, i, -230f - i * 34f);

            _txtRewards = MakeText(dt, "Txt_Rewards", "", 15, new Color(0.72f, 0.52f, 0.08f));
            Anchor(_txtRewards.rectTransform, 0f, 0f, new Vector2(12f, 40f), new Vector2(290f, 22f), new Vector2(0f, 0f));

            _txtProjection = MakeText(dt, "Txt_Projection", "Điểm dự kiến:  — đ", 17, new Color(0.75f, 0.25f, 0.15f));
            Anchor(_txtProjection.rectTransform, 0f, 0f, new Vector2(12f, 10f), new Vector2(290f, 26f), new Vector2(0f, 0f));

            // ── LIST (Sổ công thức) ──
            var lst = MakePanel((RectTransform)board.transform, "Board_List", new Color(0.98f, 0.94f, 0.84f));
            Stretch((RectTransform)lst.transform, 0.02f, 0.02f, 0.98f, 0.93f);
            _boardList = lst;
            var lt = (RectTransform)lst.transform;

            string[] tabNames = { "Tất cả", "Dễ", "Vừa", "Khó" };
            for (int i = 0; i < 4; i++)
            {
                int filter = i - 1;
                var b = MakeButton(lt, "Tab_" + i, tabNames[i], new Color(0.93f, 0.80f, 0.55f),
                    () => { _listFilter = filter; RebuildDishList(); });
                Anchor((RectTransform)b.transform, 0f, 1f, new Vector2(8f + i * 74f, -8f), new Vector2(68f, 30f), new Vector2(0f, 1f));
            }

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
            scroll.content = crt; scroll.horizontal = false; scroll.vertical = true;
            _dishListContent = content.transform;

            lst.SetActive(false);
        }

        private void BuildFlavorRow(RectTransform parent, int index, float y)
        {
            var row = new FlavorRow();

            row.label = MakeText(parent, $"Flavor_Label_{index}", "", 14, new Color(0.36f, 0.20f, 0.09f));
            Anchor(row.label.rectTransform, 0f, 1f, new Vector2(12f, y), new Vector2(62f, 22f), new Vector2(0f, 1f));

            var track = MakePanel(parent, $"Flavor_Track_{index}", new Color(0.85f, 0.76f, 0.60f));
            Anchor(track, 0f, 1f, new Vector2(80f, y - 3f), new Vector2(160f, 14f), new Vector2(0f, 1f));
            row.fill = MakeFill((RectTransform)track.transform, new Color(0.55f, 0.75f, 0.35f));

            var marker = MakePanel((RectTransform)track.transform, "Marker", new Color(0.82f, 0.16f, 0.12f));
            var mrt = (RectTransform)marker.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(3f, 20f);
            row.marker = mrt;

            row.value = MakeText(parent, $"Flavor_Value_{index}", "0/0", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(row.value.rectTransform, 0f, 1f, new Vector2(248f, y), new Vector2(52f, 22f), new Vector2(0f, 1f));

            _flavorRows[index] = row;
        }

        private void RebuildDishList()
        {
            if (_dishListContent == null || dishBook == null || dishBook.allDishes == null) return;

            for (int i = _dishListContent.childCount - 1; i >= 0; i--)
                Destroy(_dishListContent.GetChild(i).gameObject);

            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;

            foreach (var d in dishBook.allDishes)
            {
                if (d == null) continue;
                if (_listFilter >= 0 && (int)d.difficulty != _listFilter) continue;

                bool unlocked = d.unlockLevel <= lv;
                var dish = d;

                var row = MakeButton((RectTransform)_dishListContent, "Row_" + d.dishId,
                    "", unlocked ? new Color(1f, 0.99f, 0.94f) : new Color(0.88f, 0.84f, 0.76f),
                    unlocked ? () => SelectDish(dish) : (UnityEngine.Events.UnityAction)null);
                var rrt = (RectTransform)row.transform;
                rrt.sizeDelta = new Vector2(0f, 52f);

                var ico = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image));
                ico.transform.SetParent(row.transform, false);
                var im = ico.GetComponent<Image>();
                im.sprite = d.dishSprite; im.enabled = d.dishSprite != null;
                im.preserveAspect = true; im.raycastTarget = false;
                Anchor((RectTransform)ico.transform, 0f, 0.5f, new Vector2(26f, 0f), new Vector2(38f, 38f), new Vector2(0.5f, 0.5f));

                var name = MakeText(row.transform, "Txt_Name", d.dishName, 16, new Color(0.36f, 0.20f, 0.09f));
                Anchor(name.rectTransform, 0f, 1f, new Vector2(52f, -4f), new Vector2(210f, 22f), new Vector2(0f, 1f));

                string meta = unlocked
                    ? $"{DiffName(d.difficulty)} · Cấp {d.unlockLevel} · {d.rewardGold} vàng"
                    : $"🔒 Mở ở cấp {d.unlockLevel}";
                var sub = MakeText(row.transform, "Txt_Meta", meta, 12, new Color(0.6f, 0.45f, 0.28f));
                Anchor(sub.rectTransform, 0f, 0f, new Vector2(52f, 4f), new Vector2(220f, 18f), new Vector2(0f, 0f));
            }
        }

        private void BuildStage()
        {
            // Bảng đen MÓN HÔM NAY
            var chalk = MakePanel(_root, "Chalkboard", new Color(0.16f, 0.14f, 0.12f));
            Anchor(chalk, 0.5f, 1f, new Vector2(215f, -125f), new Vector2(210f, 110f), new Vector2(0.5f, 1f));
            _txtChalk = MakeText(chalk.transform, "Txt_Chalk", "MÓN HÔM NAY", 13, new Color(0.95f, 0.92f, 0.85f));
            StretchText(_txtChalk, 10f, 6f);
            _txtChalk.alignment = TextAlignmentOptions.TopLeft;

            // Mèo Thần Tài (placeholder K1 — K2 gắn sprite maneki 4 frame)
            var cat = MakePanel(_root, "ManekiCat", new Color(0.97f, 0.96f, 0.93f));
            Anchor(cat, 0.5f, 1f, new Vector2(-170f, -130f), new Vector2(64f, 72f), new Vector2(0.5f, 1f));
            var catTxt = MakeText(cat.transform, "Txt_Cat", "🐱\nMèo Thần Tài", 11, new Color(0.55f, 0.38f, 0.22f));
            StretchText(catTxt, 2f, 2f);
            catTxt.alignment = TextAlignmentOptions.Center;

            // Lò nướng
            var oven = MakePanel(_root, "Oven", new Color(0.72f, 0.45f, 0.30f));
            Anchor(oven, 1f, 1f, new Vector2(-20f, -110f), new Vector2(230f, 190f), new Vector2(1f, 1f));
            var mouth = MakePanel((RectTransform)oven.transform, "Oven_Mouth", new Color(0.25f, 0.13f, 0.08f));
            Anchor(mouth, 0.5f, 0.5f, new Vector2(0f, 14f), new Vector2(120f, 84f), new Vector2(0.5f, 0.5f));
            var stateBar = MakePanel((RectTransform)oven.transform, "Oven_StateBar", new Color(0.98f, 0.94f, 0.84f));
            Anchor(stateBar, 0.5f, 0f, new Vector2(0f, 10f), new Vector2(200f, 30f), new Vector2(0.5f, 0f));
            _imgOvenFill = MakeFill((RectTransform)stateBar.transform, new Color(0.95f, 0.65f, 0.2f));
            _imgOvenFill.fillAmount = 0f;
            _txtOvenState = MakeText(stateBar.transform, "Txt_State", "Lò chưa nhóm", 14, new Color(0.36f, 0.20f, 0.09f));
            StretchText(_txtOvenState, 6f, 0f);
            _txtOvenState.alignment = TextAlignmentOptions.Center;

            // Bàn sơ chế (toast tiến trình)
            var prep = MakePanel(_root, "Prep_Table", new Color(0.85f, 0.70f, 0.50f));
            Anchor(prep, 0.5f, 0.5f, new Vector2(-120f, 40f), new Vector2(190f, 64f), new Vector2(0.5f, 0.5f));
            var prepLbl = MakeText(prep.transform, "Txt_Label", "Bàn sơ chế", 14, new Color(0.36f, 0.20f, 0.09f));
            Anchor(prepLbl.rectTransform, 0.5f, 0f, new Vector2(0f, 4f), new Vector2(170f, 20f), new Vector2(0.5f, 0f));
            prepLbl.alignment = TextAlignmentOptions.Center;
            _txtPrepToast = MakeText(_root, "Txt_PrepToast", "", 15, new Color(0.30f, 0.55f, 0.15f));
            Anchor(_txtPrepToast.rectTransform, 0.5f, 0.5f, new Vector2(0f, 110f), new Vector2(420f, 24f), new Vector2(0.5f, 0.5f));
            _txtPrepToast.alignment = TextAlignmentOptions.Center;

            // Bàn trình bày (nút cất kho)
            var plating = MakeButton(_root, "Plating_Table", "Trình bày", new Color(0.90f, 0.78f, 0.58f), OnPlatingClicked);
            Anchor((RectTransform)plating.transform, 0.5f, 0.5f, new Vector2(110f, 40f), new Vector2(190f, 64f), new Vector2(0.5f, 0.5f));
            _btnPlating = plating;
            _txtPlating = plating.GetComponentInChildren<TMP_Text>();

            // Hộp VÀO KHO
            var wh = MakePanel(_root, "Warehouse_Box", new Color(0.45f, 0.26f, 0.12f));
            Anchor(wh, 1f, 0.5f, new Vector2(-250f, 90f), new Vector2(150f, 74f), new Vector2(1f, 0.5f));
            var whLbl = MakeText(wh.transform, "Txt_Wh", "VÀO KHO", 15, new Color(1f, 0.85f, 0.4f));
            Anchor(whLbl.rectTransform, 0.5f, 1f, new Vector2(0f, -6f), new Vector2(140f, 22f), new Vector2(0.5f, 1f));
            whLbl.alignment = TextAlignmentOptions.Center;
            _txtSentCount = MakeText(wh.transform, "Txt_Sent", $"Đã gửi {PlayerPrefs.GetInt(SentCountKey, 0)} món", 13, new Color(0.98f, 0.94f, 0.84f));
            Anchor(_txtSentCount.rectTransform, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(140f, 20f), new Vector2(0.5f, 0f));
            _txtSentCount.alignment = TextAlignmentOptions.Center;
        }

        private void BuildTray()
        {
            var tray = MakePanel(_root, "Tray", new Color(0.42f, 0.26f, 0.13f));
            Anchor(tray, 0.5f, 0f, new Vector2(30f, 12f), new Vector2(880f, 250f), new Vector2(0.5f, 0f));
            var trt = (RectTransform)tray.transform;

            var tabIng = MakeButton(trt, "Tab_Ingredients", "Nguyên liệu 0/4", new Color(0.55f, 0.78f, 0.35f), () => ShowTrayTab(true));
            Anchor((RectTransform)tabIng.transform, 0f, 1f, new Vector2(12f, -8f), new Vector2(160f, 34f), new Vector2(0f, 1f));
            _tabIngredients = tabIng.gameObject;
            _txtTabIng = tabIng.GetComponentInChildren<TMP_Text>();

            var tabSea = MakeButton(trt, "Tab_Seasonings", "Gia vị 0/3", new Color(0.45f, 0.65f, 0.90f), () => ShowTrayTab(false));
            Anchor((RectTransform)tabSea.transform, 0f, 1f, new Vector2(182f, -8f), new Vector2(120f, 34f), new Vector2(0f, 1f));
            _tabSeasonings = tabSea.gameObject;
            _txtTabSea = tabSea.GetComponentInChildren<TMP_Text>();

            var clear = MakeButton(trt, "Btn_ClearAll", "Bỏ hết", new Color(0.82f, 0.30f, 0.22f), OnClearAllClicked);
            Anchor((RectTransform)clear.transform, 1f, 1f, new Vector2(-12f, -8f), new Vector2(90f, 34f), new Vector2(1f, 1f));
            _btnClearAll = clear;

            _gridIngredients = MakeGrid(trt, "Grid_Ingredients");
            _gridSeasonings  = MakeGrid(trt, "Grid_Seasonings");
            _gridSeasonings.gameObject.SetActive(false);
        }

        private Transform MakeGrid(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(12f, 10f); rt.offsetMax = new Vector2(-12f, -48f);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 88f);
            grid.spacing = new Vector2(9f, 9f);
            grid.childAlignment = TextAnchor.UpperLeft;
            return go.transform;
        }

        private void ShowTrayTab(bool ingredients)
        {
            if (_gridIngredients != null) _gridIngredients.gameObject.SetActive(ingredients);
            if (_gridSeasonings != null)  _gridSeasonings.gameObject.SetActive(!ingredients);
        }

        /// <summary>Dựng thẻ nguyên liệu/gia vị bằng chính SelectableIngredientCard cũ — SelectionManager giữ nguyên.</summary>
        private void BuildTrayCards()
        {
            if (allIngredients == null || _gridIngredients == null || selection == null) return;

            foreach (var data in allIngredients)
            {
                if (data == null) continue;
                bool isSea = data.kind == IngredientKind.Seasoning;
                var parent = isSea ? _gridSeasonings : _gridIngredients;

                var card = new GameObject("Card_" + data.id, typeof(RectTransform), typeof(Image));
                card.transform.SetParent(parent, false);
                card.GetComponent<Image>().color = new Color(1f, 0.99f, 0.94f);

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
                st.raycastTarget = false;
                Stretch((RectTransform)status.transform, 0f, 0f, 1f, 1f);
                status.SetActive(false);

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
        }

        private void BuildActionButton()
        {
            var btn = MakeButton(_root, "Btn_Action", "CHỌN NGUYÊN LIỆU", new Color(0.62f, 0.58f, 0.53f), OnActionClicked);
            Anchor((RectTransform)btn.transform, 1f, 0f, new Vector2(-18f, 26f), new Vector2(250f, 74f), new Vector2(1f, 0f));
            _btnAction = btn;
            _imgAction = btn.GetComponent<Image>();
            _txtAction = btn.GetComponentInChildren<TMP_Text>();
            _txtAction.fontSize = 22;

            var sub = MakeText(btn.transform, "Txt_Sub", "chạm khay bên dưới", 12, new Color(1f, 1f, 1f, 0.85f));
            Anchor(sub.rectTransform, 0.5f, 0f, new Vector2(0f, 6f), new Vector2(230f, 16f), new Vector2(0.5f, 0f));
            sub.alignment = TextAlignmentOptions.Center;
            _txtActionSub = sub;
        }

        private void MakeNeedChip(Transform parent, IngredientData ing)
        {
            var chip = new GameObject("Chip_" + ing.id, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            ((RectTransform)chip.transform).sizeDelta = new Vector2(56f, 62f);
            chip.GetComponent<Image>().color = new Color(1f, 0.99f, 0.94f);

            var ico = new GameObject("Img", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(chip.transform, false);
            var im = ico.GetComponent<Image>();
            im.sprite = ing.icon; im.enabled = ing.icon != null;
            im.preserveAspect = true; im.raycastTarget = false;
            Anchor((RectTransform)ico.transform, 0.5f, 1f, new Vector2(0f, -4f), new Vector2(34f, 34f), new Vector2(0.5f, 1f));

            var t = MakeText(chip.transform, "Txt", ing.displayName, 10, new Color(0.36f, 0.20f, 0.09f));
            Anchor(t.rectTransform, 0.5f, 0f, new Vector2(0f, 2f), new Vector2(54f, 16f), new Vector2(0.5f, 0f));
            t.alignment = TextAlignmentOptions.Center;
        }

        // ── UI factory helpers (skin tạm K1) ───────────────────────

        private static TMP_FontAsset _viFont;
        private void ApplyFont(TMP_Text t)
        {
            if (_viFont == null)
            {
                foreach (var txt in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (txt != null && txt.font != null && txt.transform.root != transform.root)
                    { _viFont = txt.font; break; }
            }
            if (_viFont != null) t.font = _viFont;
        }

        private GameObject MakePanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
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

        private Button MakeButton(RectTransform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
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
            img.sprite = null;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f); rt.offsetMax = new Vector2(-2f, -2f);
            return img;
        }

        private static void Stretch(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(GameObject go, float xMin, float yMin, float xMax, float yMax)
            => Stretch((RectTransform)go.transform, xMin, yMin, xMax, yMax);

        private static void StretchText(TMP_Text t, float padX, float padY)
        {
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void Anchor(GameObject go, float ax, float ay, Vector2 pos, Vector2 size, Vector2 pivot)
            => Anchor((RectTransform)go.transform, ax, ay, pos, size, pivot);

        private static void Anchor(RectTransform rt, float ax, float ay, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
