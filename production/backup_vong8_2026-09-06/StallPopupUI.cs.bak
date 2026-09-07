using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POPUP QUẦY HÀNG — lưới ô 4 trạng thái (B3) + panel chọn vật phẩm trượt đè (B4)
/// + bộ chỉnh số lượng/giá (B5) + lọc hết hàng (B6) + nút gạt loa (B7).
///
/// Lớp này CHỈ đọc trạng thái từ <see cref="PlayerStallManager"/> rồi vẽ, và gọi ngược
/// lại các hàm Try* của manager khi người chơi bấm. Nó không tự trừ kho, không tự cộng
/// vàng, không tự quyết định ô nào mở được — mọi luật nằm ở manager. Giữ ranh giới này
/// là lý do quầy hàng, mặt quầy ngoài map và bảng tin chợ của DEV-A không bao giờ nói
/// ba con số khác nhau về cùng một mặt hàng.
///
/// Toàn bộ hierarchy do Editor tool `Tools ▸ Farm ▸ Quầy Hàng` sinh ra — file này KHÔNG
/// tạo GameObject nào lúc chạy, chỉ Instantiate prefab đã dựng sẵn.
/// </summary>
public class StallPopupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  THAM CHIẾU
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Khung popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button     buttonClose;
    [SerializeField] private Button     buttonDimBackground;
    [SerializeField] private TMP_Text   textTitle;
    [SerializeField] private TMP_Text   textGold;

    [Header("Lưới ô quầy")]
    [SerializeField] private Transform   slotGridContent;
    [SerializeField] private StallSlotUI slotPrefab;

    [Header("Hồ sơ người chơi (góc dưới trái)")]
    [SerializeField] private TMP_Text textPlayerName;
    [SerializeField] private TMP_Text textPlayerLevel;

    [Header("Panel chọn vật phẩm (trượt đè)")]
    [SerializeField] private GameObject     pickerRoot;
    [SerializeField] private RectTransform  pickerPanel;
    [SerializeField] private Button         buttonPickerBack;
    [Tooltip("Toạ độ X lúc panel đã trượt vào hẳn.")]
    [SerializeField] private float          pickerShownX = 0f;
    [Tooltip("Toạ độ X lúc panel còn nằm ngoài màn hình.")]
    [SerializeField] private float          pickerHiddenX = 2200f;
    [SerializeField] private float          pickerSlideSeconds = 0.22f;

    [Header("Panel chọn — cột trái: tab danh mục")]
    [SerializeField] private List<StallCategoryTabUI> categoryTabs = new List<StallCategoryTabUI>();

    [Header("Panel chọn — cột giữa: lưới vật phẩm")]
    [SerializeField] private Transform            pickGridContent;
    [SerializeField] private StallPickItemCellUI  pickCellPrefab;
    [SerializeField] private GameObject           pickEmptyHint;
    [SerializeField] private TMP_Text             textPickEmptyHint;

    [Header("Panel chọn — cột phải: khu thiết lập")]
    [SerializeField] private GameObject setupEmptyHint;
    [SerializeField] private GameObject setupContentRoot;
    [SerializeField] private Image      imageSelectedIcon;
    [SerializeField] private TMP_Text   textSelectedName;

    [SerializeField] private Button   buttonQuantityMinus;
    [SerializeField] private Button   buttonQuantityPlus;
    [SerializeField] private TMP_Text textQuantity;

    [SerializeField] private Button   buttonPriceMinus;
    [SerializeField] private Button   buttonPricePlus;
    [SerializeField] private TMP_Text textPrice;
    [SerializeField] private TMP_Text textPriceHint;

    [SerializeField] private Button         buttonLoaToggle;
    [SerializeField] private TMP_Text       textLoaLabel;
    [SerializeField] private TMP_Text       textLoaCost;
    [SerializeField] private RectTransform  loaKnob;
    [SerializeField] private Image          imageLoaTrack;

    [SerializeField] private Button   buttonConfirm;
    [SerializeField] private TMP_Text textConfirmLabel;

    [Header("Thông báo")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text   textMessage;
    [SerializeField] private float      messageSeconds = 2.2f;

    [Header("Màu nút bậc (B5 — `−` phải XÁM khi chạm giới hạn)")]
    [SerializeField] private Color colorStepEnabled  = new Color(0.18f, 0.75f, 0.40f, 1f);
    [SerializeField] private Color colorStepDisabled = new Color(0.42f, 0.42f, 0.46f, 1f);

    [Header("Màu nút gạt loa")]
    [SerializeField] private Color colorLoaOn  = new Color(0.18f, 0.75f, 0.66f, 1f);
    [SerializeField] private Color colorLoaOff = new Color(0.35f, 0.30f, 0.42f, 1f);
    [SerializeField] private float loaKnobOffX = -46f;
    [SerializeField] private float loaKnobOnX  = 46f;

    // ─────────────────────────────────────────────────────────────────────────
    //  TRẠNG THÁI TRONG PHIÊN
    // ─────────────────────────────────────────────────────────────────────────

    private readonly List<StallSlotUI>        _slots     = new List<StallSlotUI>();
    private readonly List<StallPickItemCellUI> _pickCells = new List<StallPickItemCellUI>();

    private int               _targetSlotIndex = -1;
    private string            _selectedItemId;
    private int               _quantity     = 1;
    private int               _pricePerUnit = 1;
    private bool              _hasLoa;
    private StallItemCategory _category = StallItemCategory.TatCa;

    private Coroutine _slideRoutine;
    private Coroutine _messageRoutine;
    private float     _nextSlotRefresh;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;
    public static bool AnyOpen { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  VÒNG ĐỜI
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveAllListeners();
            buttonClose.onClick.AddListener(ClosePopup);
        }

        if (buttonDimBackground != null)
        {
            buttonDimBackground.onClick.RemoveAllListeners();
            buttonDimBackground.onClick.AddListener(ClosePopup);
        }

        if (buttonPickerBack != null)
        {
            buttonPickerBack.onClick.RemoveAllListeners();
            buttonPickerBack.onClick.AddListener(HidePicker);
        }

        WireStepButton(buttonQuantityMinus, () => ChangeQuantity(-1));
        WireStepButton(buttonQuantityPlus,  () => ChangeQuantity(+1));
        WireStepButton(buttonPriceMinus,    () => ChangePrice(-1));
        WireStepButton(buttonPricePlus,     () => ChangePrice(+1));

        if (buttonLoaToggle != null)
        {
            buttonLoaToggle.onClick.RemoveAllListeners();
            buttonLoaToggle.onClick.AddListener(ToggleLoa);
        }

        if (buttonConfirm != null)
        {
            buttonConfirm.onClick.RemoveAllListeners();
            buttonConfirm.onClick.AddListener(ConfirmPost);
        }

        // Popup phải TẮT lúc khởi động.
        if (popupRoot != null) popupRoot.SetActive(false);
        AnyOpen = false;
    }

    private void OnEnable() => Resubscribe();

    private void Start()
    {
        Resubscribe();
        RefreshGold();
    }

    /// <summary>Gỡ trước rồi mới gắn: gọi bao nhiêu lần cũng chỉ có đúng một đăng ký.</summary>
    private void Resubscribe()
    {
        if (FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
            FarmEconomyManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
        }

        if (PlayerStallManager.Instance != null)
        {
            PlayerStallManager.Instance.OnStallChanged -= OnStallChanged;
            PlayerStallManager.Instance.OnStallChanged += OnStallChanged;
        }
    }

    private void OnDisable()
    {
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;

        if (PlayerStallManager.Instance != null)
            PlayerStallManager.Instance.OnStallChanged -= OnStallChanged;

        AnyOpen = false;
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePopup();
            return;
        }

        // Đồng hồ "còn lại" trên các ô đang bán nhích mỗi giây.
        if (Time.unscaledTime < _nextSlotRefresh) return;
        _nextSlotRefresh = Time.unscaledTime + 1f;
        RefreshSlots();
    }

    private void OnCurrencyChanged(int gold, int gems) => RefreshGold();

    private void OnStallChanged()
    {
        if (!IsOpen) return;
        RefreshSlots();
        if (pickerRoot != null && pickerRoot.activeSelf) RefreshPickGrid();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MỞ / ĐÓNG
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenPopup()
    {
        if (popupRoot == null) return;
        if (IsOpen) return;

        Transform p = popupRoot.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
                p.gameObject.SetActive(true);
            p = p.parent;
        }

        popupRoot.SetActive(true);
        AnyOpen = true;
        Resubscribe();

        FarmInputLock.RegisterPopupOpen();
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        EnsureSlots();
        HidePickerImmediate();
        RefreshAll();
    }

    public void ClosePopup()
    {
        if (popupRoot == null) return;
        if (!IsOpen) return;

        HidePickerImmediate();
        popupRoot.SetActive(false);
        AnyOpen = false;

        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);
        FarmInputLock.RegisterPopupClose();
    }

    /// <summary>Cho nút HUD/phím tắt: mở nếu đang đóng, đóng nếu đang mở.</summary>
    public void TogglePopup()
    {
        if (IsOpen) ClosePopup();
        else        OpenPopup();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LƯỚI Ô QUẦY
    // ─────────────────────────────────────────────────────────────────────────

    private void EnsureSlots()
    {
        if (slotGridContent == null || slotPrefab == null) return;

        PlayerStallManager stall = PlayerStallManager.Instance;
        int want = stall != null ? stall.TotalSlotCount : 10;

        while (_slots.Count < want)
        {
            StallSlotUI slot = Instantiate(slotPrefab, slotGridContent);
            slot.name = $"Slot_{_slots.Count:00}";
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] == null) continue;
            _slots[i].gameObject.SetActive(i < want);
            if (i < want) _slots[i].Bind(this, i);
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] != null && _slots[i].gameObject.activeSelf) _slots[i].Refresh();
        }
    }

    private void RefreshAll()
    {
        RefreshGold();
        RefreshProfile();
        RefreshSlots();

        if (textTitle != null) textTitle.text = "QUẦY HÀNG";
    }

    private void RefreshGold()
    {
        if (textGold == null) return;
        int gold = FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gold : 0;
        textGold.text = gold.ToString("N0");
    }

    private void RefreshProfile()
    {
        if (textPlayerName != null)
        {
            string ten = PlayerPrefs.GetString("PLAYER_PROFILE_NAME", "");
            textPlayerName.text = string.IsNullOrWhiteSpace(ten) ? "Người chơi" : ten;
        }

        if (textPlayerLevel != null)
        {
            int level = PlayerProgressManager.Instance != null
                ? PlayerProgressManager.Instance.Level
                : (FarmLevelManager.Instance != null ? FarmLevelManager.Instance.CurrentLevel : 1);
            textPlayerLevel.text = level.ToString();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Ô QUẦY GỌI NGƯỢC LÊN
    // ─────────────────────────────────────────────────────────────────────────

    public void OnSlotRequestSell(int slotIndex)
    {
        _targetSlotIndex = slotIndex;
        _selectedItemId  = null;
        _hasLoa          = false;
        _category        = StallItemCategory.TatCa;
        ShowPicker();
    }

    public void OnSlotRequestUnlock(int slotIndex)
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null) return;

        if (!stall.TryUnlockSlot(slotIndex, out string error))
        {
            ShowMessage(error);
            return;
        }

        ShowMessage("Đã mở thêm một ô quầy!");
        RefreshSlots();
        RefreshGold();
    }

    public void OnSlotRequestCancel(int slotIndex)
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null) return;

        PlayerListing listing = stall.GetListingAtSlot(slotIndex);
        if (listing == null) return;

        if (!stall.TryCancelListing(listing.listingId, out string error))
        {
            ShowMessage(error);
            return;
        }

        ShowMessage("Đã gỡ hàng, hoàn về kho.");
        RefreshSlots();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PANEL CHỌN VẬT PHẨM (B4)
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowPicker()
    {
        if (pickerRoot == null) return;

        pickerRoot.SetActive(true);
        BindCategoryTabs();
        RefreshPickGrid();
        RefreshSetupPanel();

        // TRƯỢT ĐÈ lên lưới, không mở popup mới: người chơi vẫn thấy các ô quầy phía sau
        // nên không mất phương hướng về việc "mình đang đặt hàng vào ô nào".
        if (pickerPanel != null)
        {
            if (_slideRoutine != null) StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(SlidePicker(pickerHiddenX, pickerShownX, false));
        }
    }

    private void HidePicker()
    {
        if (pickerRoot == null) return;

        if (pickerPanel != null && gameObject.activeInHierarchy)
        {
            if (_slideRoutine != null) StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(
                SlidePicker(pickerPanel.anchoredPosition.x, pickerHiddenX, true));
        }
        else
        {
            HidePickerImmediate();
        }
    }

    private void HidePickerImmediate()
    {
        if (_slideRoutine != null)
        {
            StopCoroutine(_slideRoutine);
            _slideRoutine = null;
        }

        if (pickerPanel != null)
        {
            Vector2 p = pickerPanel.anchoredPosition;
            pickerPanel.anchoredPosition = new Vector2(pickerHiddenX, p.y);
        }

        if (pickerRoot != null) pickerRoot.SetActive(false);

        _targetSlotIndex = -1;
        _selectedItemId  = null;
    }

    /// <summary>
    /// Trượt panel theo trục X.
    ///
    /// Cố tình chỉ có MỘT coroutine cho cả trượt vào lẫn trượt ra (`hideWhenDone`), không
    /// lồng coroutine này trong coroutine khác. Bản lồng nhau từng gây lỗi: cái bên trong
    /// chạy xong tự xoá `_slideRoutine`, nên nếu người chơi bấm mở lại panel ngay lúc đó
    /// thì `ShowPicker` không tìm thấy routine nào để dừng ⇒ hai coroutine cùng kéo panel
    /// và panel vừa hiện lên đã bị cái cũ tắt đi.
    ///
    /// Thứ tự hai dòng cuối cũng quan trọng: xoá `_slideRoutine` TRƯỚC rồi mới gọi
    /// `HidePickerImmediate`, nếu ngược lại thì hàm đó sẽ `StopCoroutine` lên chính
    /// coroutine đang chạy và phần dọn dẹp phía sau không bao giờ tới.
    /// </summary>
    private IEnumerator SlidePicker(float fromX, float toX, bool hideWhenDone)
    {
        Vector2 p = pickerPanel.anchoredPosition;
        pickerPanel.anchoredPosition = new Vector2(fromX, p.y);

        float t = 0f;
        float dur = Mathf.Max(0.01f, pickerSlideSeconds);

        while (t < dur)
        {
            // unscaledDeltaTime: popup vẫn phải trượt mượt kể cả khi có hệ thống khác
            // đặt Time.timeScale = 0 lúc mở giao diện.
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            k = 1f - (1f - k) * (1f - k);   // ease-out: nhanh lúc đầu, dừng êm
            pickerPanel.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k), p.y);
            yield return null;
        }

        pickerPanel.anchoredPosition = new Vector2(toX, p.y);
        _slideRoutine = null;

        if (hideWhenDone) HidePickerImmediate();
    }

    private void BindCategoryTabs()
    {
        for (int i = 0; i < categoryTabs.Count; i++)
        {
            if (categoryTabs[i] == null) continue;
            categoryTabs[i].Bind(this);
            categoryTabs[i].SetSelected(categoryTabs[i].Category == _category);
        }
    }

    public void OnSelectCategory(StallItemCategory category)
    {
        _category = category;
        BindCategoryTabs();
        RefreshPickGrid();
    }

    /// <summary>
    /// Dựng lại lưới chọn. Vật phẩm số lượng 0 KHÔNG bao giờ lọt vào đây (B6) — nguồn
    /// dữ liệu là <see cref="PlayerStallManager.GetSellableItems"/> vốn đã lọc sẵn, nên
    /// bán hết món nào là món đó tự biến mất ở lần dựng kế tiếp.
    /// </summary>
    private void RefreshPickGrid()
    {
        if (pickGridContent == null || pickCellPrefab == null) return;

        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null) return;

        List<StallSellableItem> all = stall.GetSellableItems();
        StallItemCatalog catalog = StallItemCatalog.Instance;

        var shown = new List<StallSellableItem>();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].amount <= 0) continue;

            if (_category != StallItemCategory.TatCa)
            {
                StallItemCategory cat = catalog != null
                    ? catalog.GetCategory(all[i].itemId)
                    : StallItemCategory.CheBien;
                if (cat != _category) continue;
            }

            shown.Add(all[i]);
        }

        // Dùng lại ô cũ thay vì Destroy/Instantiate mỗi lần đổi tab: đổi tab là thao tác
        // người chơi bấm liên tục, sinh rác mỗi lần sẽ gây khựng trên máy yếu.
        while (_pickCells.Count < shown.Count)
        {
            StallPickItemCellUI cell = Instantiate(pickCellPrefab, pickGridContent);
            cell.name = $"PickCell_{_pickCells.Count:00}";
            _pickCells.Add(cell);
        }

        for (int i = 0; i < _pickCells.Count; i++)
        {
            if (_pickCells[i] == null) continue;

            bool use = i < shown.Count;
            _pickCells[i].gameObject.SetActive(use);
            if (!use) continue;

            _pickCells[i].Bind(this, shown[i].itemId, shown[i].amount);
            _pickCells[i].SetSelected(shown[i].itemId == _selectedItemId);
        }

        // Vật phẩm đang chọn vừa bán hết (hoặc bị lọc khỏi tab) → bỏ chọn, nếu không
        // khu thiết lập sẽ vẫn mời "Đặt lên quầy" một món không còn tồn tại.
        if (!string.IsNullOrEmpty(_selectedItemId))
        {
            bool stillThere = false;
            for (int i = 0; i < shown.Count; i++)
            {
                if (shown[i].itemId == _selectedItemId) { stillThere = true; break; }
            }

            if (!stillThere)
            {
                _selectedItemId = null;
                RefreshSetupPanel();
            }
        }

        bool empty = shown.Count == 0;
        if (pickEmptyHint != null) pickEmptyHint.SetActive(empty);
        if (empty && textPickEmptyHint != null)
            textPickEmptyHint.text = "KHÔNG CÒN VẬT PHẨM NÀO ĐỂ BÁN";
    }

    public void OnPickItem(string itemId)
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null) return;

        _selectedItemId = itemId;

        int available = stall.GetAvailableAmount(itemId);
        _quantity     = Mathf.Clamp(available, 1, Mathf.Max(1, available));
        _pricePerUnit = stall.GetSuggestedPricePerUnit(itemId);

        for (int i = 0; i < _pickCells.Count; i++)
        {
            if (_pickCells[i] != null && _pickCells[i].gameObject.activeSelf)
                _pickCells[i].SetSelected(_pickCells[i].ItemId == itemId);
        }

        RefreshSetupPanel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  KHU THIẾT LẬP — SỐ LƯỢNG / GIÁ / LOA (B5, B7)
    // ─────────────────────────────────────────────────────────────────────────

    private void ChangeQuantity(int direction)
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null || string.IsNullOrEmpty(_selectedItemId)) return;

        int max = Mathf.Max(1, stall.GetAvailableAmount(_selectedItemId));
        _quantity = Mathf.Clamp(_quantity + direction, 1, max);
        RefreshSetupPanel();
    }

    private void ChangePrice(int direction)
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null || string.IsNullOrEmpty(_selectedItemId)) return;

        int step = stall.GetPriceStepPerUnit(_selectedItemId);
        int min  = stall.GetMinPricePerUnit(_selectedItemId);
        int max  = stall.GetMaxPricePerUnit(_selectedItemId);

        _pricePerUnit = Mathf.Clamp(_pricePerUnit + direction * step, min, max);
        RefreshSetupPanel();
    }

    private void ToggleLoa()
    {
        _hasLoa = !_hasLoa;
        RefreshSetupPanel();
    }

    private void RefreshSetupPanel()
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        bool has = stall != null && !string.IsNullOrEmpty(_selectedItemId);

        if (setupEmptyHint   != null) setupEmptyHint.SetActive(!has);
        if (setupContentRoot != null) setupContentRoot.SetActive(has);

        if (!has) return;

        StallItemCatalog catalog = StallItemCatalog.Instance;

        if (imageSelectedIcon != null)
        {
            Sprite icon = catalog != null ? catalog.GetIcon(_selectedItemId) : null;
            imageSelectedIcon.sprite  = icon;
            imageSelectedIcon.enabled = icon != null;
        }

        if (textSelectedName != null)
            textSelectedName.text = catalog != null ? catalog.GetDisplayName(_selectedItemId) : _selectedItemId;

        int maxQty = Mathf.Max(1, stall.GetAvailableAmount(_selectedItemId));
        int minP   = stall.GetMinPricePerUnit(_selectedItemId);
        int maxP   = stall.GetMaxPricePerUnit(_selectedItemId);
        int sugg   = stall.GetSuggestedPricePerUnit(_selectedItemId);

        _quantity     = Mathf.Clamp(_quantity, 1, maxQty);
        _pricePerUnit = Mathf.Clamp(_pricePerUnit, minP, maxP);

        if (textQuantity != null) textQuantity.text = _quantity.ToString();

        // Hiện TỔNG giá cho cả lô. Đây là con số người chơi thật sự quan tâm ("bán lô
        // này được bao nhiêu"), và nó tự đổi khi số lượng đổi — chính là chi tiết
        // "số lượng và giá liên động" trong video.
        if (textPrice != null) textPrice.text = (_pricePerUnit * _quantity).ToString("N0");

        if (textPriceHint != null)
        {
            int suggestedTotal = sugg * _quantity;
            textPriceHint.text = _pricePerUnit > sugg
                ? $"Cao hơn giá gợi ý ({suggestedTotal:N0}) — lâu bán hơn"
                : _pricePerUnit < sugg
                    ? $"Thấp hơn giá gợi ý ({suggestedTotal:N0}) — bán nhanh hơn"
                    : $"Giá gợi ý · {_pricePerUnit:N0}/cái";
        }

        // ── B5: `−` phải XÁM khi chạm giới hạn ───────────────────────────────
        SetStepButtonEnabled(buttonQuantityMinus, _quantity > 1);
        SetStepButtonEnabled(buttonQuantityPlus,  _quantity < maxQty);
        SetStepButtonEnabled(buttonPriceMinus,    _pricePerUnit > minP);
        SetStepButtonEnabled(buttonPricePlus,     _pricePerUnit < maxP);

        RefreshLoaSwitch(stall);

        if (textConfirmLabel != null) textConfirmLabel.text = "Đặt lên quầy";
    }

    private void RefreshLoaSwitch(PlayerStallManager stall)
    {
        // "TẮT LOA" nghĩa là loa ĐANG BẬT và bấm để tắt — đúng như video. Nhãn phải mô tả
        // HÀNH ĐỘNG sắp xảy ra chứ không phải trạng thái hiện tại, nếu không người chơi
        // sẽ bấm nhầm rồi mất vàng oan.
        if (textLoaLabel != null) textLoaLabel.text = _hasLoa ? "TẮT LOA" : "BẬT LOA";

        if (textLoaCost != null) textLoaCost.text = stall.LoaGoldCost.ToString("N0");

        if (imageLoaTrack != null) imageLoaTrack.color = _hasLoa ? colorLoaOn : colorLoaOff;

        if (loaKnob != null)
        {
            Vector2 p = loaKnob.anchoredPosition;
            loaKnob.anchoredPosition = new Vector2(_hasLoa ? loaKnobOnX : loaKnobOffX, p.y);
        }
    }

    /// <summary>
    /// Bật/tắt một nút bậc kèm TÍN HIỆU MÀU rõ ràng. Không dựa vào tint tự động của
    /// Button.interactable: màu disabled mặc định chỉ nhạt đi một chút, trên nền tối
    /// của popup thì gần như không phân biệt được với nút còn bấm được.
    /// </summary>
    private void SetStepButtonEnabled(Button button, bool enabled)
    {
        if (button == null) return;

        button.interactable = enabled;

        Graphic target = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
        if (target != null) target.color = enabled ? colorStepEnabled : colorStepDisabled;
    }

    private void WireStepButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ĐẶT LÊN QUẦY (B8)
    // ─────────────────────────────────────────────────────────────────────────

    private void ConfirmPost()
    {
        PlayerStallManager stall = PlayerStallManager.Instance;
        if (stall == null) return;

        if (_targetSlotIndex < 0 || string.IsNullOrEmpty(_selectedItemId))
        {
            ShowMessage("Hãy chọn một vật phẩm.");
            return;
        }

        if (!stall.TryPostListing(_targetSlotIndex, _selectedItemId, _quantity, _pricePerUnit,
                                  _hasLoa, out string error))
        {
            ShowMessage(error);

            // Bật loa thất bại vì thiếu vàng → tự gạt về TẮT, để người chơi bấm lại
            // là đăng bán được ngay thay vì bấm mãi vào cùng một lỗi.
            if (_hasLoa)
            {
                _hasLoa = false;
                RefreshSetupPanel();
            }
            return;
        }

        ShowMessage("Đã đặt lên quầy!");
        HidePicker();
        RefreshSlots();
        RefreshGold();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  THÔNG BÁO
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (messageRoot == null || textMessage == null)
        {
            Debug.Log($"[QuầyHàng] {message}");
            return;
        }

        textMessage.text = message;
        messageRoot.SetActive(true);

        if (_messageRoutine != null) StopCoroutine(_messageRoutine);
        if (gameObject.activeInHierarchy) _messageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, messageSeconds));
        if (messageRoot != null) messageRoot.SetActive(false);
        _messageRoutine = null;
    }
}
