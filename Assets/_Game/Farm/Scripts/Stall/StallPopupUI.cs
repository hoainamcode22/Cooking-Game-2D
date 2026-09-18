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

    [Header("Vừa khung màn hình (fit-to-screen)")]
    [Tooltip("Tấm bảng gỗ thật sự phải co lại (Popup_Main). Để trống thì tự dò lúc chạy — " +
             "KHÔNG phải popupRoot, vì popupRoot là Panel_Dim phủ kín màn hình.")]
    [SerializeField] private RectTransform popupBoard;

    [Header("Màu chữ gợi ý (nền kem — trắng gần như không đọc được)")]
    [Tooltip("Màu cho dòng 'KHÔNG CÒN VẬT PHẨM NÀO ĐỂ BÁN'. Prefab để trắng alpha 0.45 " +
             "trên nền kem ⇒ mờ tịt. Nâu trầm này khớp với Text_PriceHint/Text_SetupHint.")]
    [SerializeField] private Color colorEmptyHint = new Color(0.48f, 0.29f, 0.06f, 0.85f);

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

    /// <summary>Lề an toàn quanh bảng — giống ShopManager.LE_AN_TOAN_POPUP.</summary>
    private const float LE_AN_TOAN_POPUP = 24f;

    private Vector3 _scaleGocBang;
    private Vector2 _viTriGocBang;
    private bool    _daLuuBang;

    /// <summary>
    /// Đúng MỘT lần tăng popupLockCount cho mỗi lần popup mở. Không có cờ này thì mỗi
    /// đường đóng lại tự quyết định có giảm hay không, và chỉ cần một lần lệch là
    /// popupLockCount kẹt &gt; 0 vĩnh viễn ⇒ FarmInputLock.BlockMapPan luôn true ⇒ map chết.
    /// </summary>
    private bool _popupInputLockHeld;

    public static StallPopupUI Instance { get; private set; }

    public bool IsOpen => popupRoot != null && popupRoot.activeInHierarchy;

    public static bool AnyOpen
    {
        get
        {
            if (Instance == null)
                Instance = Object.FindFirstObjectByType<StallPopupUI>(FindObjectsInactive.Include);
            return Instance != null && Instance.IsOpen;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VÒNG ĐỜI
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;

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
    }

    private void OnDestroy()
    {
        // Đóng nửa chừng (đổi scene, popup bị Destroy thẳng) vẫn phải trả khoá input,
        // nếu không map đứng im mà không còn object nào để mà đóng lại cho đúng.
        ReleasePopupInputBlock();

        if (Instance == this) Instance = null;
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

        if (popupRoot != null && popupRoot.activeSelf)
            popupRoot.SetActive(false);

        // GỌI VÔ ĐIỀU KIỆN. Bản cũ chỉ trả khoá khi popupRoot CÒN ĐANG BẬT — nghĩa là
        // nếu ai đó tắt Panel_Dim bằng đường khác (PopupManager, tool, SetActive thẳng)
        // rồi mới tắt canvas, nhánh này không chạy và popupLockCount kẹt lại mãi mãi.
        // ReleasePopupInputBlock() tự biết mình có đang giữ khoá hay không nên gọi thừa
        // là vô hại.
        ReleasePopupInputBlock();
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
        Resubscribe();

        AcquirePopupInputBlock();

        EnsureSlots();
        HidePickerImmediate();
        RefreshAll();

        // Khung vàng: prefab neo nó ở góc PHẢI-TRÊN, ngay cạnh nút X ⇒ hai thứ đè nhau.
        ChuyenKhungVangSangTrai();

        // Kích thước canvas đổi theo cửa sổ/xoay máy ⇒ căn lại tỉ lệ bảng MỖI LẦN MỞ.
        // Phải gọi SAU HidePickerImmediate(): panel chọn vật phẩm đỗ ở x=1700, còn bật
        // là dấu chân bảng phình ra gấp đôi và hệ số co sẽ bé đến vô lý.
        VuaKhungManHinh();

        // [FIX QA] Chu vua dung xong => xin dich sang tieng Anh ngay (re, da gop chung 1 khung hinh).
        Loc.RequestRescan();
    }

    public void ClosePopup()
    {
        HidePickerImmediate();
        if (popupRoot != null)
            popupRoot.SetActive(false);

        // Bản cũ quyết định có giảm popupLockCount hay không bằng
        // `wasOpen || FarmInputLock.IsPopupOpen`. Vế thứ hai là khoá của NGƯỜI KHÁC:
        // đóng quầy trong lúc một popup khác đang mở sẽ ăn trộm đúng một nhịp giảm của
        // popup đó, và popup đó đóng xong thì popupLockCount tụt xuống dưới 0 (bị kẹp về 0)
        // ⇒ hai popup cùng mở lần sau chỉ còn một khoá. Cờ _popupInputLockHeld cho biết
        // CHÍNH quầy hàng có đang giữ khoá hay không, nên mọi đường đóng đều cân bằng.
        ReleasePopupInputBlock();
    }

    /// <summary>Giữ khoá input + bật lớp chặn raycast. Gọi nhiều lần vẫn chỉ tăng một nhịp.</summary>
    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _popupInputLockHeld = true;
    }

    /// <summary>
    /// Trả khoá input + tắt lớp chặn raycast. BẤT BIẾN (idempotent): gọi mười lần cũng
    /// chỉ giảm đúng một nhịp, và gọi khi chưa từng giữ khoá thì không giảm nhịp nào.
    /// Nhờ vậy ClosePopup / OnDisable / OnDestroy được phép gọi chồng lên nhau.
    /// </summary>
    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (!_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _popupInputLockHeld = false;
    }

    /// <summary>Cho nút HUD/phím tắt: mở nếu đang đóng, đóng nếu đang mở.</summary>
    public void TogglePopup()
    {
        if (IsOpen) ClosePopup();
        else        OpenPopup();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VỪA KHUNG MÀN HÌNH (fit-to-screen)
    //  Cùng một cách làm với ShopManager.VuaKhungManHinh() và
    //  UnifiedTaskPopupUI.VuaKhungManHinh(): nhớ scale gốc lúc mở lần đầu, đo dấu chân
    //  thật, so với khung canvas trừ lề, rồi nhân một hệ số ≤ 1 — KHÔNG BAO GIỜ phóng to.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm tấm bảng gỗ (Popup_Main) — thứ THẬT SỰ phải co lại.
    ///
    /// KHÔNG được co <see cref="popupRoot"/>: ô đó trỏ vào Panel_Dim, một tấm phủ kín màn
    /// hình (anchor 0;0 → 1;1). Co Panel_Dim là nền mờ tự thu lại thành một ô vuông giữa
    /// màn hình, lòi cả map ra hai bên.
    ///
    /// Thứ tự dò: ô kéo thả → con tên "Popup_Main" → cha của lưới ô quầy → con đầu của Dim.
    /// Ba đường dự phòng sau là để KHÔNG cần thao tác tay nào trong Unity.
    /// </summary>
    private RectTransform TimBangPopup()
    {
        if (popupBoard != null) return popupBoard;

        if (popupRoot != null)
        {
            Transform t = popupRoot.transform.Find("Popup_Main");
            if (t != null) popupBoard = t as RectTransform;
        }

        if (popupBoard == null && slotGridContent != null)
            popupBoard = slotGridContent.parent as RectTransform;

        if (popupBoard == null && popupRoot != null && popupRoot.transform.childCount > 0)
            popupBoard = popupRoot.transform.GetChild(0) as RectTransform;

        return popupBoard;
    }

    /// <summary>
    /// Co bảng cho vừa canvas và dời xuống cho ruy-băng tiêu đề thôi bị cắt.
    ///
    /// DẤU CHÂN THẬT ≠ rect của bảng. Đo bằng <c>rect</c> của Popup_Main (1500×860) thì
    /// thiếu mất hai thứ vẽ NGOÀI mép bảng: ruy-băng tiêu đề (Header_Banner 620×126 ở
    /// y = +415 ⇒ thò lên trên 48) và nút X (64×64 scale 1.5 ở x = +723 ⇒ thò sang phải
    /// ~21). Co theo rect là ruy-băng vẫn lòi ra ngoài mép màn hình.
    /// <see cref="RectTransformUtility.CalculateRelativeRectTransformBounds(Transform)"/>
    /// gom đúng mọi RectTransform ĐANG BẬT trong bảng nên nó cho ra dấu chân thật, và tự
    /// bỏ qua panel chọn vật phẩm đang tắt (Picker_Panel đỗ ở x = 1700).
    ///
    /// Gọi MỖI LẦN MỞ: kích thước canvas đổi theo cửa sổ và theo hướng máy, hệ số của lần
    /// trước không còn đúng cho lần này.
    /// </summary>
    private void VuaKhungManHinh()
    {
        RectTransform rtBang = TimBangPopup();
        if (rtBang == null) return;

        RectTransform rtKhung = rtBang.parent as RectTransform;
        if (rtKhung == null) return;

        if (!_daLuuBang)
        {
            _daLuuBang    = true;
            _scaleGocBang = rtBang.localScale;
            _viTriGocBang = rtBang.anchoredPosition;
        }

        // Trả về NGUYÊN BẢN trước khi đo. Bounds tính theo không gian cục bộ của bảng nên
        // không dính scale của chính nó, nhưng độ lệch vị trí thì cộng dồn — không trả về
        // gốc là mỗi lần mở bảng lại trôi xuống thêm một đoạn.
        rtBang.localScale       = _scaleGocBang;
        rtBang.anchoredPosition = _viTriGocBang;

        // EnsureSlots() vừa Instantiate thêm ô vào lưới ngay trước đó. Chưa ép layout
        // chạy thì các ô mới còn nằm ở rect của prefab, dấu chân đo ra sẽ to hơn thật và
        // bảng bị co quá tay ngay lần mở đầu tiên.
        Canvas.ForceUpdateCanvases();

        Bounds bound = RectTransformUtility.CalculateRelativeRectTransformBounds(rtBang);

        float rongBang = bound.size.x * Mathf.Abs(_scaleGocBang.x);
        float caoBang  = bound.size.y * Mathf.Abs(_scaleGocBang.y);
        if (rongBang < 1f || caoBang < 1f) return;

        float rongKhung = rtKhung.rect.width  - LE_AN_TOAN_POPUP * 2f;
        float caoKhung  = rtKhung.rect.height - LE_AN_TOAN_POPUP * 2f;
        if (rongKhung < 1f || caoKhung < 1f) return;

        float heSo = Mathf.Min(1f, Mathf.Min(rongKhung / rongBang, caoKhung / caoBang));
        rtBang.localScale = _scaleGocBang * heSo;

        // Bảng căn giữa theo RECT, nhưng phần NHÌN THẤY lệch lên trên vì ruy-băng thò ra
        // khỏi mép trên (48) mà mép dưới thì không thò gì. Tâm dấu chân vì thế nằm cao hơn
        // tâm rect đúng 24 — và đó chính là lý do ruy-băng luôn là thứ bị cắt ĐẦU TIÊN dù
        // bảng đã co vừa. Dời bảng xuống đúng khoảng lệch đó thì tâm HÌNH trùng tâm màn hình.
        Vector2 lech = new Vector2(bound.center.x * _scaleGocBang.x,
                                   bound.center.y * _scaleGocBang.y) * heSo;
        rtBang.anchoredPosition = _viTriGocBang - lech;
    }

    /// <summary>
    /// Kéo khung vàng sang MÉP TRÁI của bảng (yêu cầu của Sếp: "khung vàng kéo sang bên trái").
    ///
    /// Trong prefab <c>GoldBar</c> neo góc PHẢI-TRÊN (anchor 1;1 · anchoredPosition −140;−96)
    /// nên nó nằm đúng cạnh nút X (x ≈ +723) và hai thứ đè lên nhau. Ở đây chỉ LẬT NEO sang
    /// trái rồi đổi dấu X: khoảng cách tới mép giữ nguyên 140, không phải sờ vào prefab.
    ///
    /// Đã ở bên trái rồi thì thoát ngay ⇒ gọi lại mỗi lần mở popup là vô hại.
    /// </summary>
    private void ChuyenKhungVangSangTrai()
    {
        if (textGold == null) return;

        // Ô kéo thả trỏ vào Text_Gold; thứ cần dời là cái pill bọc ngoài (GoldBar) chứ
        // không phải riêng con số, nếu không icon vàng vẫn nằm lại bên phải.
        RectTransform rtKhungVang = textGold.transform.parent as RectTransform;
        if (rtKhungVang == null || rtKhungVang.name.IndexOf("Gold", System.StringComparison.OrdinalIgnoreCase) < 0)
            rtKhungVang = textGold.transform as RectTransform;
        if (rtKhungVang == null) return;

        if (rtKhungVang.anchorMin.x <= 0.5f && rtKhungVang.anchorMax.x <= 0.5f) return;

        Vector2 aMin = rtKhungVang.anchorMin;
        Vector2 aMax = rtKhungVang.anchorMax;
        rtKhungVang.anchorMin = new Vector2(0f, aMin.y);
        rtKhungVang.anchorMax = new Vector2(0f, aMax.y);

        Vector2 p = rtKhungVang.anchoredPosition;
        rtKhungVang.anchoredPosition = new Vector2(-p.x, p.y);
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

        SkinKit.ApFont(transform);
        HidePlayerProfileTab();

        if (textTitle != null)
        {
            textTitle.text = Loc.T("QUẦY HÀNG");
            textTitle.ForceMeshUpdate(true);
        }
    }

    private void HidePlayerProfileTab()
    {
        if (textPlayerName != null && textPlayerName.gameObject != null)
        {
            textPlayerName.gameObject.SetActive(false);
            Transform p = textPlayerName.transform.parent;
            if (p != null && p != transform && p.gameObject != popupRoot && (p.name.Contains("Profile") || p.name.Contains("Player") || p.name.Contains("Tab")))
            {
                p.gameObject.SetActive(false);
            }
        }

        if (textPlayerLevel != null && textPlayerLevel.gameObject != null)
        {
            textPlayerLevel.gameObject.SetActive(false);
        }
    }

    private void RefreshGold()
    {
        if (textGold == null) return;
        int gold = FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gold : 0;
        textGold.text = gold.ToString("N0");
    }

    private void RefreshProfile()
    {
        HidePlayerProfileTab();
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

        ShowMessage(Loc.T("Đã mở thêm một ô quầy!"));
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

        ShowMessage(Loc.T("Đã gỡ hàng, hoàn về kho."));
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
        {
            textPickEmptyHint.text = Loc.T("KHÔNG CÒN VẬT PHẨM NÀO ĐỂ BÁN");

            // Prefab để trắng alpha 0.45 trên nền kem ⇒ đọc không ra. Đổi sang nâu trầm
            // đúng tông với Text_PriceHint / Text_SetupHint trong cùng panel.
            textPickEmptyHint.color = colorEmptyHint;
        }
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
                ? Loc.TF("Cao hơn giá gợi ý ({0:N0}) — lâu bán hơn", suggestedTotal)
                : _pricePerUnit < sugg
                    ? Loc.TF("Thấp hơn giá gợi ý ({0:N0}) — bán nhanh hơn", suggestedTotal)
                    : Loc.TF("Giá gợi ý · {0:N0}/cái", _pricePerUnit);
        }

        // ── B5: `−` phải XÁM khi chạm giới hạn ───────────────────────────────
        SetStepButtonEnabled(buttonQuantityMinus, _quantity > 1);
        SetStepButtonEnabled(buttonQuantityPlus,  _quantity < maxQty);
        SetStepButtonEnabled(buttonPriceMinus,    _pricePerUnit > minP);
        SetStepButtonEnabled(buttonPricePlus,     _pricePerUnit < maxP);

        RefreshLoaSwitch(stall);

        if (textConfirmLabel != null) textConfirmLabel.text = Loc.T("Đặt lên quầy");
    }

    private void RefreshLoaSwitch(PlayerStallManager stall)
    {
        // "TẮT LOA" nghĩa là loa ĐANG BẬT và bấm để tắt — đúng như video. Nhãn phải mô tả
        // HÀNH ĐỘNG sắp xảy ra chứ không phải trạng thái hiện tại, nếu không người chơi
        // sẽ bấm nhầm rồi mất vàng oan.
        if (textLoaLabel != null) textLoaLabel.text = _hasLoa ? Loc.T("TẮT LOA") : Loc.T("BẬT LOA");

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
            ShowMessage(Loc.T("Hãy chọn một vật phẩm."));
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

        ShowMessage(Loc.T("Đã đặt lên quầy!"));
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
