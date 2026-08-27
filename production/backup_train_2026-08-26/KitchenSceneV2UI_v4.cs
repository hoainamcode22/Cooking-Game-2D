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
        public Sprite[] catChefWalk;
    }

    public class KitchenSceneV2UI : MonoBehaviour
    {
        public static KitchenSceneV2UI Instance { get; private set; }

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

        [System.Serializable]
        public class LayoutOverride
        {
            public string path;   // tên khối con cấp 1 dưới Kitchen_UI_v2
            public Vector2 pos;
            public Vector2 size;
            public Vector3 scale = Vector3.one;
        }

        [Header("Vị trí Sếp chỉnh tay — lưu bằng menu Tools/Farm Game/Kitchen/Lưu vị trí chỉnh tay")]
        [SerializeField] private List<LayoutOverride> layoutOverrides = new List<LayoutOverride>();

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

        /// <summary>
        /// Xoá sạch hierarchy con (kể cả bản PREVIEW tool bake trong Edit mode) rồi dựng lại.
        /// Editor tool gọi để Sếp NHÌN THẤY UI ngay ngoài Edit mode; Start gọi để runtime
        /// luôn dựng bản tươi — nhờ đó preview không bao giờ bị nhân đôi khi Play.
        /// ⚠ Preview chỉ để QUAN SÁT: chỉnh tay lên preview sẽ bị dựng lại đè khi Play/chạy tool.
        /// </summary>
        public void RebuildNow()
        {
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

        /// <summary>Editor tool gọi: dựng cả thẻ nguyên liệu cho preview đầy đủ.</summary>
        public void BuildEditorPreview()
        {
            RebuildNow();
            if (selection == null) selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);
            if (selection != null) BuildTrayCards();
            ShowBoardDetail(true);
        }


        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (challenge == null) challenge = FindFirstObjectByType<CookingChallengeManager>(FindObjectsInactive.Include);
            if (selection == null) selection = FindFirstObjectByType<CookingSelectionManager>(FindObjectsInactive.Include);

            RebuildNow();
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
            _ovenFakeProgress = 0f;
            if (_imgOvenFill != null) _imgOvenFill.fillAmount = 0f;
            SetText(_txtOvenState, $"HỎNG... {score}đ");
            SetText(_txtPrepToast, "Chọn lại nguyên liệu rồi nấu tiếp nhé!");
        }

        private void HandleDishCollected(DishData d)
        {
            _ovenFakeProgress = 0f;
            if (_imgOvenFill != null) _imgOvenFill.fillAmount = 0f;
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
            else
            {
                SetText(_txtProjection, "Điểm dự kiến:  — đ");
            }

            // Nút hành động 3 trạng thái + bàn trình bày
            bool plateReady = challenge != null && challenge.CookedDishOnPlate != null;
            bool cooking    = (challenge != null && challenge.IsCooking) || _ovenBusy;

            if (_btnPlating != null) _btnPlating.interactable = plateReady;
            SetText(_txtPlating, plateReady ? "CHẠM ĐỂ CẤT VÀO KHO!" : "Trình bày");

            if (_btnAction != null && _imgAction != null)
            {
                bool useSkin = skin.btnGreen != null && skin.btnGray != null;
                if (cooking)
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, "ĐANG NẤU...");
                    SetText(_txtActionSub, "");
                }
                else if (plateReady)
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, "MÓN TRÊN DĨA");
                    SetText(_txtActionSub, "cất vào kho trước đã");
                }
                else if (nIng + nSea > 0)
                {
                    _btnAction.interactable = true;
                    ApplyActionSkin(useSkin, true);
                    SetText(_txtAction, "NẤU!");
                    SetText(_txtActionSub, $"{nIng} nguyên liệu · {nSea} gia vị");
                }
                else
                {
                    _btnAction.interactable = false;
                    ApplyActionSkin(useSkin, false);
                    SetText(_txtAction, "CHỌN NGUYÊN LIỆU");
                    SetText(_txtActionSub, "chạm khay bên dưới");
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
                _imgAction.type = Image.Type.Sliced;
                _imgAction.color = Color.white;
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

            ApplyLayoutOverrides(); // vị trí Sếp chỉnh tay LUÔN thắng code — cả edit lẫn Play
            _built = true;
        }

        /// <summary>Áp các vị trí Sếp đã chỉnh tay (khối cấp 1) đè lên layout code dựng.</summary>
        private void ApplyLayoutOverrides()
        {
            if (layoutOverrides == null) return;
            foreach (var o in layoutOverrides)
            {
                if (o == null || string.IsNullOrEmpty(o.path)) continue;
                var t = transform.Find(o.path) as RectTransform;
                if (t == null) continue;
                t.anchoredPosition = o.pos;
                t.sizeDelta = o.size;
                t.localScale = o.scale;
            }
        }

        /// <summary>EDITOR gọi: so hiện trạng với bản code dựng để CHỈ lưu những khối Sếp đã kéo/đổi size.
        /// Trả về số chỗ đã lưu. Sau đó vị trí chỉnh tay giữ nguyên qua Play/rebuild.</summary>
        public int CaptureLayoutOverrides()
        {
            var current = new Dictionary<string, LayoutOverride>();
            foreach (Transform c in transform)
            {
                var rt = c as RectTransform;
                if (rt == null) continue;
                current[c.name] = new LayoutOverride
                { path = c.name, pos = rt.anchoredPosition, size = rt.sizeDelta, scale = c.localScale };
            }

            layoutOverrides.Clear();
            RebuildNow(); // dựng chuẩn theo code để so sánh

            foreach (Transform c in transform)
            {
                if (!current.TryGetValue(c.name, out var was)) continue;
                var rt = c as RectTransform;
                if (rt == null) continue;
                bool moved = (rt.anchoredPosition - was.pos).sqrMagnitude > 0.25f
                          || (rt.sizeDelta - was.size).sqrMagnitude > 0.25f
                          || (c.localScale - was.scale).sqrMagnitude > 0.0001f;
                if (!moved) continue;
                layoutOverrides.Add(was);
                rt.anchoredPosition = was.pos;
                rt.sizeDelta = was.size;
                c.localScale = was.scale;
            }
            return layoutOverrides.Count;
        }

        public void ClearLayoutOverrides()
        {
            if (layoutOverrides != null) layoutOverrides.Clear();
            RebuildNow();
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
            Anchor(banner, 0.5f, 1f, new Vector2(60f, -70f), new Vector2(460f, 104f), new Vector2(0.5f, 1f));
            Skin9(banner, skin.panelBoard);

            // Ribbon to + chữ TRẮNG đậm nằm TRONG ribbon → không bao giờ tràn/chìm
            var ribbonB = MakePanel((RectTransform)banner.transform, "Ribbon", new Color(0.90f, 0.55f, 0.12f));
            Anchor(ribbonB, 0.5f, 1f, new Vector2(0f, 12f), new Vector2(330f, 46f), new Vector2(0.5f, 0.5f));
            Skin9(ribbonB, skin.ribbon);
            var title = MakeText(ribbonB.transform, "Txt_Title", "ĐƠN CỦA KHÁCH", 19, Color.white);
            StretchText(title, 8f, 2f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            var card = MakePanel((RectTransform)banner.transform, "Order_Card", new Color(0.98f, 0.94f, 0.84f));
            Anchor(card, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(436f, 64f), new Vector2(0.5f, 0f));
            Skin9(card, skin.panelPaper);

            var iconGo = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(card.transform, false);
            _imgOrderIcon = iconGo.GetComponent<Image>();
            _imgOrderIcon.preserveAspect = true;
            _imgOrderIcon.raycastTarget = false;
            Anchor((RectTransform)iconGo.transform, 0f, 0.5f, new Vector2(32f, 0f), new Vector2(48f, 48f), new Vector2(0.5f, 0.5f));

            _txtOrderName = MakeText(card.transform, "Txt_Name", "—", 20, new Color(0.30f, 0.16f, 0.07f));
            Anchor(_txtOrderName.rectTransform, 0f, 1f, new Vector2(66f, -5f), new Vector2(356f, 28f), new Vector2(0f, 1f));

            _txtOrderChips = MakeText(card.transform, "Txt_Chips", "", 14, new Color(0.52f, 0.35f, 0.18f));
            Anchor(_txtOrderChips.rectTransform, 0f, 0f, new Vector2(66f, 5f), new Vector2(356f, 22f), new Vector2(0f, 0f));
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
            Skin9(lst, skin.panelPaper);
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

            // Chấm màu tròn nhỏ trước tên vị (theo mockup: Ngọt hồng, Cay đỏ, Chua xanh, Đậm dương, Kết cấu nâu)
            var dotCols = new[] {
                new Color(0.93f, 0.45f, 0.65f), new Color(0.88f, 0.28f, 0.22f), new Color(0.45f, 0.75f, 0.30f),
                new Color(0.35f, 0.60f, 0.88f), new Color(0.65f, 0.45f, 0.28f) };
            var dot = new GameObject($"Flavor_Dot_{index}", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(parent, false);
            var dimg = dot.GetComponent<Image>();
            dimg.sprite = GetDotSprite();
            dimg.color = dotCols[index % dotCols.Length];
            dimg.raycastTarget = false;
            Anchor((RectTransform)dot.transform, 0f, 1f, new Vector2(14f, y - 5f), new Vector2(12f, 12f), new Vector2(0f, 1f));

            row.label = MakeText(parent, $"Flavor_Label_{index}", "", 14, new Color(0.36f, 0.20f, 0.09f));
            Anchor(row.label.rectTransform, 0f, 1f, new Vector2(30f, y), new Vector2(58f, 22f), new Vector2(0f, 1f));

            var track = MakePanel(parent, $"Flavor_Track_{index}", new Color(0.85f, 0.76f, 0.60f));
            Anchor(track, 0f, 1f, new Vector2(80f, y - 3f), new Vector2(160f, 14f), new Vector2(0f, 1f));
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

            row.value = MakeText(parent, $"Flavor_Value_{index}", "0/0", 13, new Color(0.55f, 0.38f, 0.22f));
            Anchor(row.value.rectTransform, 0f, 1f, new Vector2(248f, y), new Vector2(52f, 22f), new Vector2(0f, 1f));

            _flavorRows[index] = row;
        }

        private void RebuildDishList()
        {
            if (_dishListContent == null || dishBook == null || dishBook.allDishes == null) return;

            for (int i = _dishListContent.childCount - 1; i >= 0; i--)
                Destroy(_dishListContent.GetChild(i).gameObject);

            // Không có PlayerProgressManager = đang chạy riêng scene bếp để dev/test → mở hết
            int lv = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 999;

            foreach (var d in dishBook.allDishes)
            {
                if (d == null) continue;
                if (_listFilter >= 0 && (int)d.difficulty != _listFilter) continue;

                bool unlocked = d.unlockLevel <= lv;
                var dish = d;

                var row = MakeButton((RectTransform)_dishListContent, "Row_" + d.dishId,
                    "", unlocked ? new Color(1f, 0.99f, 0.94f) : new Color(0.88f, 0.84f, 0.76f),
                    unlocked ? () => SelectDish(dish) : (UnityEngine.Events.UnityAction)null);
                Skin9(row.gameObject, unlocked ? skin.cardIngredient : skin.cardLocked);
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
            Skin9(stateBar, skin.panelPaper);
            Anchor(stateBar, 0.5f, 0f, new Vector2(0f, 8f), new Vector2(214f, 34f), new Vector2(0.5f, 0f));
            _imgOvenFill = MakeFill((RectTransform)stateBar.transform, new Color(0.95f, 0.65f, 0.2f));
            _imgOvenFill.fillAmount = 0f;
            _txtOvenState = MakeText(stateBar.transform, "Txt_State", "Lò chưa nhóm", 16, new Color(0.32f, 0.17f, 0.07f));
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
            _txtSentCount = MakeText(wh.transform, "Txt_Sent", $"Đã gửi {PlayerPrefs.GetInt(SentCountKey, 0)} món", 14, new Color(0.99f, 0.96f, 0.88f));
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

            var tabIng = MakeButton(trt, "Tab_Ingredients", "Nguyên liệu 0/4", new Color(0.55f, 0.78f, 0.35f), () => ShowTrayTab(true));
            Anchor((RectTransform)tabIng.transform, 0f, 1f, new Vector2(12f, -8f), new Vector2(160f, 34f), new Vector2(0f, 1f));
            _tabIngredients = tabIng.gameObject;
            _txtTabIng = tabIng.GetComponentInChildren<TMP_Text>();

            var tabSea = MakeButton(trt, "Tab_Seasonings", "Gia vị 0/3", new Color(0.45f, 0.65f, 0.90f), () => ShowTrayTab(false));
            Anchor((RectTransform)tabSea.transform, 0f, 1f, new Vector2(182f, -8f), new Vector2(120f, 34f), new Vector2(0f, 1f));
            _tabSeasonings = tabSea.gameObject;
            _txtTabSea = tabSea.GetComponentInChildren<TMP_Text>();

            var clear = MakeButton(trt, "Btn_ClearAll", "Bỏ hết", new Color(0.82f, 0.30f, 0.22f), OnClearAllClicked);
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

            if (skin.tabOn != null && skin.tabOff != null)
            {
                Skin9(_tabIngredients, ingredients ? skin.tabOn : skin.tabOff);
                Skin9(_tabSeasonings, ingredients ? skin.tabOff : skin.tabOn);
            }

            // Phối màu chữ tab cho tương phản: tab đang mở nâu sậm, tab kia kem sáng
            var cTabOn  = new Color(0.30f, 0.16f, 0.07f);
            var cTabOff = new Color(0.99f, 0.96f, 0.88f);
            if (_txtTabIng != null) { _txtTabIng.color = ingredients ? cTabOn : cTabOff; _txtTabIng.fontStyle = FontStyles.Bold; }
            if (_txtTabSea != null) { _txtTabSea.color = ingredients ? cTabOff : cTabOn; _txtTabSea.fontStyle = FontStyles.Bold; }
        }

        /// <summary>Dựng thẻ nguyên liệu/gia vị bằng chính SelectableIngredientCard cũ — SelectionManager giữ nguyên.</summary>
        private void BuildTrayCards()
        {
            if (allIngredients == null || _gridIngredients == null || selection == null) return;

            int playerLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 999; // chạy riêng scene = dev mode mở hết

            foreach (var data in allIngredients)
            {
                if (data == null) continue;
                bool isSea = data.kind == IngredientKind.Seasoning;
                var parent = isSea ? _gridSeasonings : _gridIngredients;

                // Nguyên liệu chưa mở khoá (vd Sữa cấp 14) → thẻ khoá xám, không chọn được
                if (data.unlockLevel > playerLevel)
                {
                    BuildLockedCard(parent, data);
                    continue;
                }

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
                $"+ Mở {slotPackSize} ô\n{cost:N0} vàng", new Color(0.30f, 0.55f, 0.90f), () => TryBuySlots(tab));
            var bl = buy.GetComponentInChildren<TMP_Text>();
            if (bl != null) { bl.fontSize = 13; bl.fontStyle = FontStyles.Bold; }
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
                { _txtPrepToast.text = $"Không đủ {cost:N0} vàng để mở ô!"; _txtPrepToast.color = new Color(0.85f, 0.25f, 0.18f); }
                return;
            }
            PlayerPrefs.SetInt(SlotKeyPrefix + tab, GetExtraSlots(tab) + slotPackSize);
            PlayerPrefs.SetInt(SlotKeyPrefix + "bought_" + tab, bought + 1);
            PlayerPrefs.Save();
            if (_txtPrepToast != null)
            { _txtPrepToast.text = $"Đã mở thêm {slotPackSize} ô khay!"; _txtPrepToast.color = new Color(0.30f, 0.55f, 0.15f); }
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

            var t = MakeText(card.transform, "Txt_Lv", $"Cấp {data.unlockLevel}\n{data.displayName}", 11, new Color(0.45f, 0.40f, 0.34f));
            Anchor(t.rectTransform, 0.5f, 0f, new Vector2(0f, 4f), new Vector2(106f, 32f), new Vector2(0.5f, 0f));
            t.alignment = TextAlignmentOptions.Center;
        }

        private void BuildActionButton()
        {
            var btn = MakeButton(_root, "Btn_Action", "CHỌN NGUYÊN LIỆU", new Color(0.62f, 0.58f, 0.53f), OnActionClicked);
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
            var b = MakeButton(_root, "Btn_BackFarm", "VỀ NÔNG TRẠI", new Color(0.62f, 0.42f, 0.20f), OnBackFarmClicked);
            Anchor((RectTransform)b.transform, 0f, 1f, new Vector2(14f, -2f), new Vector2(170f, 92f), new Vector2(0f, 1f));
            if (skin.btnBackFarm != null)
                SkinFlat(b.gameObject, skin.btnBackFarm);
            var lb = b.GetComponentInChildren<TMP_Text>();
            if (lb != null)
            {
                lb.text = "VỀ NÔNG TRẠI";
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

        private void MakeNeedChip(Transform parent, IngredientData ing)
        {
            var chip = new GameObject("Chip_" + ing.id, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            ((RectTransform)chip.transform).sizeDelta = new Vector2(72f, 68f);
            chip.GetComponent<Image>().color = new Color(1f, 0.99f, 0.94f);
            Skin9(chip, skin.cardIngredient);

            var ico = new GameObject("Img", typeof(RectTransform), typeof(Image));
            ico.transform.SetParent(chip.transform, false);
            var im = ico.GetComponent<Image>();
            im.sprite = ing.icon; im.enabled = ing.icon != null;
            im.preserveAspect = true; im.raycastTarget = false;
            Anchor((RectTransform)ico.transform, 0.5f, 1f, new Vector2(0f, -4f), new Vector2(38f, 38f), new Vector2(0.5f, 1f));

            var t = MakeText(chip.transform, "Txt", ing.displayName, 11, new Color(0.36f, 0.20f, 0.09f));
            t.enableAutoSizing = true; t.fontSizeMin = 8f; t.fontSizeMax = 11f;
            Anchor(t.rectTransform, 0.5f, 0f, new Vector2(0f, 2f), new Vector2(68f, 16f), new Vector2(0.5f, 0f));
            t.alignment = TextAlignmentOptions.Center;
        }

        // ── Skin helpers (K2) ──────────────────────────────────────

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
            if (go == null || sp == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white;
        }

        private static void SkinFlat(GameObject go, Sprite sp, bool preserveAspect = true)
        {
            if (go == null || sp == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sp; img.type = Image.Type.Simple;
            img.preserveAspect = preserveAspect; img.color = Color.white;
        }

        private static void SkinTiled(GameObject go, Sprite sp)
        {
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
                    _ovenFakeProgress = Mathf.MoveTowards(_ovenFakeProgress, 0.92f, Time.unscaledDeltaTime * 0.25f);
                    _imgOvenFill.fillAmount = _ovenFakeProgress;
                }
                else if (_ovenFakeProgress > 0f)
                {
                    _imgOvenFill.fillAmount = 1f;
                    _ovenFakeProgress = 0f;
                }
            }

            // Khói bốc từ ống khói lò khi đang nấu
            if (_ovenBusy && skin.smokePuff != null && _ovenRect != null)
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

    /// <summary>Chữ zZz bay lên lơ lửng trên mèo đang ngủ — thuần code, không cần art.</summary>
    public class KitchenZzzFloat : MonoBehaviour
    {
        public TMP_Text txt;
        public float cycle = 2.4f;
        private float _t;

        private void Update()
        {
            if (txt == null) return;
            _t += Time.unscaledDeltaTime;
            float p = (_t % cycle) / cycle;
            var rt = txt.rectTransform;
            rt.anchoredPosition = new Vector2(12f + 7f * Mathf.Sin(p * 6.283f), 6f + 36f * p);
            var c = txt.color;
            c.a = p < 0.15f ? p / 0.15f : 1f - (p - 0.15f) / 0.85f;
            txt.color = c;
            float sc = 0.7f + 0.45f * p;
            rt.localScale = new Vector3(sc, sc, 1f);
        }
    }

    /// <summary>Mèo đầu bếp đi qua đi lại trên sàn bếp: đi → dừng nghỉ → quay đầu đi tiếp. Frame do agent-sprite-forge vẽ.</summary>
    public class KitchenCatWalker : MonoBehaviour
    {
        public Sprite[] frames;
        public float speed = 85f;
        public float minX = 500f, maxX = 900f;
        public float frameTime = 0.14f;
        public Vector2 pauseRange = new Vector2(1.2f, 3.2f);

        private Image _img;
        private RectTransform _rt;
        private float _dir = 1f;
        private float _pauseT;
        private float _frameT;
        private int _frame;

        private void Awake()
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
        }

        private void Update()
        {
            if (_img == null || _rt == null || frames == null || frames.Length == 0) return;

            if (_pauseT > 0f)
            {
                _pauseT -= Time.unscaledDeltaTime;
                if (frames[0] != null) _img.sprite = frames[0]; // đứng yên = frame đầu
                return;
            }

            var pos = _rt.anchoredPosition;
            pos.x += _dir * speed * Time.unscaledDeltaTime;
            if (pos.x >= maxX) { pos.x = maxX; _dir = -1f; _pauseT = Random.Range(pauseRange.x, pauseRange.y); }
            else if (pos.x <= minX) { pos.x = minX; _dir = 1f; _pauseT = Random.Range(pauseRange.x, pauseRange.y); }
            _rt.anchoredPosition = pos;

            // Lật mặt theo hướng đi (frame gốc vẽ hướng PHẢI)
            var sc = _rt.localScale;
            sc.x = Mathf.Abs(sc.x) * (_dir >= 0f ? 1f : -1f);
            _rt.localScale = sc;

            _frameT += Time.unscaledDeltaTime;
            if (_frameT >= frameTime)
            {
                _frameT = 0f;
                _frame = (_frame + 1) % frames.Length;
                if (frames[_frame] != null) _img.sprite = frames[_frame];
            }
        }
    }
}
