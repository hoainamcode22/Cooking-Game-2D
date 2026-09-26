using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// POPUP "MÁY XAY THỨC ĂN" — bộ điều khiển duy nhất của popup.
///
/// ══════════════════════════════════════════════════════════════════════════
///  NGUỒN THIẾT KẾ
/// ══════════════════════════════════════════════════════════════════════════
/// `Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html` — file HTML/CSS mà video demo
/// được render ra. Mọi con số animation KHÔNG nằm trong file này; chúng nằm trong
/// `MillConfig` (xem tooltip từng field ở đó để biết dòng CSS/SVG tương ứng).
///
/// ══════════════════════════════════════════════════════════════════════════
///  ⚠ KHÔNG CÓ HỆ TAB
/// ══════════════════════════════════════════════════════════════════════════
/// Video có 3 tab (Thức ăn gia súc / Máy xay mía / Máy làm nước mắm). Chủ dự án xác nhận
/// đó là LỖI THIẾT KẾ — mỗi máy một popup riêng. File này CHỈ phục vụ máy thức ăn gia súc.
/// KHÔNG có field tab, KHÔNG có code tab chết. Đừng "khôi phục theo video".
///
/// ══════════════════════════════════════════════════════════════════════════
///  BẮT ĐẦU MỘT MẺ XAY = KÉO-THẢ, KHÔNG PHẢI BẤM NÚT  (chốt 21/08)
/// ══════════════════════════════════════════════════════════════════════════
/// Người chơi KÉO một card công thức từ danh sách bên trái và THẢ vào một slot trống.
/// Chuỗi gọi:
///     MillRecipeDragSource.OnBeginDrag  (card)      → BatDauKeo()      ← chọn + sáng viền
///     MillSlotUI.OnDrop                 (slot)      → ThaVaoSlot(idx)  ← chỗ chặn thật
///                                                   → BatDauXay(idx)   ← trừ nguyên liệu
///     MillRecipeDragSource.OnEndDrag    (card)      → KetThucKeo()     ← tắt viền
///
/// Nút "XAY NGAY" cũ ĐÃ BỎ. Node `btnMain` được giữ lại làm BẢNG GỢI Ý (cùng chỗ, cùng
/// sprite, component Button bị tắt) — xem `GanSuKienNut` để biết vì sao tắt component chứ
/// không đặt interactable = false.
///
/// ⚠ ĐỪNG "khôi phục nút cho tiện": bấm nút thì máy tự chọn slot, kéo-thả thì người chơi
/// chọn slot. Hai lối vào cùng tồn tại sẽ có hai thứ tự kiểm tra khác nhau và sớm muộn lệch
/// nhau ở chỗ trừ nguyên liệu.
///
/// ══════════════════════════════════════════════════════════════════════════
///  AI GIỮ TRẠNG THÁI
/// ══════════════════════════════════════════════════════════════════════════
/// File này là chỗ DUY NHẤT giữ trạng thái slot. `MillSlotUI` chỉ vẽ, `RotatingGear` /
/// `UIScrollingTexture` / `ConveyorItem` chỉ chạy hình. Nhờ vậy save-load và bù thời gian
/// offline chỉ phải viết một lần.
///
/// Thời điểm xong của mỗi slot lưu bằng **UTC ticks TUYỆT ĐỐI**, không lưu "còn bao nhiêu
/// giây". Vì thế:
///   • đóng game 10 phút rồi mở lại → slot ủ 2 phút đã XONG, không cần code bù riêng;
///   • người chơi đổi múi giờ → không ảnh hưởng (UTC);
///   • đổi giờ hệ thống về quá khứ → slot lâu hơn thật, KHÔNG bị hack ngược (xem ghi chú
///     ở `LuuTrangThai`).
///
/// ══════════════════════════════════════════════════════════════════════════
///  CHỐNG RÁC MỖI FRAME (cạm bẫy #3)
/// ══════════════════════════════════════════════════════════════════════════
/// `Update` chạy liên tục khi popup mở. Ba hàng rào bắt buộc, ĐỪNG BỎ:
///   • `_soDangXayDaHien` / `_soChoThuDaHien` / `_soDaMoDaHien` → chỉ dựng lại chuỗi
///     badge + dòng tổng kết khi CON SỐ đổi.
///   • `_gemDaHien` → chỉ ghi lại số dư kim cương khi nó đổi.
///   • `_trangThaiNutDaHien` → chỉ đổi chữ/màu nút lớn khi trạng thái nút đổi.
/// `MillSlotUI.BindRunning` cũng có hàng rào riêng cho đồng hồ. Không dùng LINQ, không
/// dùng foreach trên IEnumerable trong Update.
/// </summary>
[DisallowMultipleComponent]
public class MillPopupUI : MonoBehaviour
{
    // ═════════════════════════════ SINGLETON ═════════════════════════════

    /// <summary>Thể hiện duy nhất trong scene. Dùng bởi <see cref="MillBuildingClick"/>.</summary>
    public static MillPopupUI Instance { get; private set; }

    /// <summary>
    /// Cờ static "popup máy xay đang mở". Theo đúng quy ước sẵn có của dự án
    /// (`CropProcessPopupUI.AnyOpen`, `OrderBoardPopupUI.AnyOpen`) để `PopupManager` chặn
    /// click xuống world được mà KHÔNG cần thêm [SerializeField] và KHÔNG cần tôi sửa
    private static int _sceneDaTim = -1;   // [PERF 2026-09-26 v2] Find(Include) cap phat ca tram KB -> chi tim 1 LAN / scene (popup tu gan Instance trong Awake)
    public static bool AnyOpen
    {
        get
        {
            // [FIX 2026-09-21 P0] Ban cu FindFirstObjectByType(Include) MOI LAN GOI khi Instance null
            // (popup khong co trong scene => tim mai, moi frame, tu nhieu noi). Chi tim lai toi da 1 lan/frame.
            if (Instance == null && _sceneDaTim != UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetHashCode())
            {
                _sceneDaTim = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetHashCode();
                Instance = FindFirstObjectByType<MillPopupUI>(FindObjectsInactive.Include);
            }
            return Instance != null && Instance.IsOpen;
        }
    }

    // ═════════════════════════════ THAM CHIẾU (Dev B wire) ═════════════════════════════

    [Header("Cấu hình")]
    [Tooltip("Asset MillConfig. Thiếu cái này popup không mở được.")]
    [SerializeField] private MillConfig config;

    [Header("Gốc popup")]
    [Tooltip("GameObject bật/tắt khi Open()/Close(). Thường là node cha của cả cửa sổ gỗ.\n" +
             "ĐỂ TRỐNG ⇒ code dùng chính gameObject này (khi đó component phải nằm trên node gốc).")]
    [SerializeField] private GameObject popupRoot;

    [Header("Danh sách công thức")]
    [Tooltip("Prefab một card công thức (có MillRecipeCardUI).")]
    [SerializeField] private MillRecipeCardUI recipeCardPrefab;

    [Tooltip("Node cha chứa các card — thường là Content của ScrollRect.\n" +
             "⚠ PHẢI là Transform TRONG SCENE. Tuyệt đối không trỏ vào transform bên trong " +
             "một prefab asset: Unity cấm SetParent vào prefab asset (phải dùng " +
             "PrefabUtility.LoadPrefabContents), Instantiate sẽ ném exception.")]
    [SerializeField] private Transform recipeContainer;

    [Header("Slot xay")]
    [Tooltip("5 ô slot theo thứ tự #1..#5. Số phần tử NÊN bằng MillConfig.slotCount.")]
    [SerializeField] private MillSlotUI[] slots;

    [Header("Animation")]
    [Tooltip("Bánh răng LỚN của máy (RotatingGear).")]
    [SerializeField] private RotatingGear gearLarge;

    [Tooltip("Bánh răng NHỎ của máy (RotatingGear).")]
    [SerializeField] private RotatingGear gearSmall;

    [Tooltip("Băng tải sọc (UIScrollingTexture trên RawImage).")]
    [SerializeField] private UIScrollingTexture belt;

    [Tooltip("Các bó cỏ chạy trên băng. Video có 2 cái, lệch pha nhau 1.5s. " +
             "Code tự đặt độ lệch = chỉ số × MillConfig.itemStaggerSeconds.")]
    [SerializeField] private ConveyorItem[] beltItems;

    [Header("Chữ")]
    [Tooltip("Ruy băng tiêu đề. Code ghi bằng MillConfig.title.")]
    [SerializeField] private TMP_Text txtTitle;

    [Tooltip("Nhãn trạng thái góc trên khu animation: \"Máy đang rảnh\" / \"Đang xay · 2 slot\".")]
    [SerializeField] private TMP_Text txtStatusBadge;

    [Tooltip("Dòng tổng kết cạnh chữ SLOT XAY: \"3/5 slot đã mở · 0 đang xay · 2 chờ thu\".")]
    [SerializeField] private TMP_Text txtSlotSummary;

    [Tooltip("Số dư kim cương góc trên phải.")]
    [SerializeField] private TMP_Text txtGemBalance;

    [Tooltip("Chữ trên nút lớn: XAY NGAY / THIẾU NGUYÊN LIỆU / HẾT SLOT TRỐNG.")]
    [SerializeField] private TMP_Text txtMainButton;

    [Tooltip("Bong bóng nguyên liệu đầu vào bên trái máy: \"x8\".")]
    [SerializeField] private TMP_Text txtInputBubble;

    [Tooltip("Nhãn sản phẩm dưới bong bóng đầu ra: \"Cám gà\".")]
    [SerializeField] private TMP_Text txtOutputTag;

    [Header("Ảnh")]
    [Tooltip("Chấm tròn cạnh nhãn trạng thái. Xanh #62E15D khi đang xay, xám khi rảnh.")]
    [SerializeField] private Image imgStatusDot;

    [Tooltip("Icon sản phẩm trong bong bóng đầu ra.")]
    [SerializeField] private Image imgOutputIcon;

    [Tooltip("Icon nguyên liệu trong bong bóng đầu vào.")]
    [SerializeField] private Image imgInputIcon;

    [Header("Nút")]
    [SerializeField] private Button btnClose;

    [Tooltip("Nút lớn dưới danh sách công thức.")]
    [SerializeField] private Button btnMain;

    [Header("Toast")]
    [Tooltip("Node bật/tắt của toast.")]
    [SerializeField] private GameObject toastRoot;

    [Tooltip("Chữ trong toast.")]
    [SerializeField] private TMP_Text toastText;

    [Header("Vừa khung màn hình (TUỲ CHỌN)")]
    [Tooltip("TUỲ CHỌN. Tấm bảng gỗ THẬT (node \"Window\") — thứ được co lại cho vừa màn hình.\n" +
             "⚠ ĐỪNG trỏ vào popupRoot: ô đó là node phủ kín màn hình chứa cả tấm nền mờ " +
             "(\"Dim\"); co nó thì nền mờ thu lại thành ô vuông giữa màn hình và lòi cả map ra " +
             "hai bên.\n" +
             "ĐỂ TRỐNG ⇒ code tự dò (xem TimBangPopup), không cần thao tác tay nào trong Unity.")]
    [SerializeField] private RectTransform popupBoard;

    // ── Các field TUỲ CHỌN do Dev A thêm; để trống popup vẫn chạy đúng ──

    [Header("TUỲ CHỌN (Dev A thêm)")]
    [Tooltip("TUỲ CHỌN. Ảnh nền của nút lớn, để tô xanh/xám. Để trống ⇒ chỉ đổi chữ và " +
             "interactable, không đổi màu.")]
    [SerializeField] private Image imgMainButtonBg;

    [Tooltip("Màu nút khi bấm được. HTML :root --btn-green = #82C94F.")]
    [SerializeField] private Color mauNutBamDuoc = new Color(0.510f, 0.788f, 0.310f, 1f);

    [Tooltip("Màu nút khi KHÔNG bấm được. HTML :root --locked-bg = #D9CDB9.")]
    [SerializeField] private Color mauNutKhoa = new Color(0.851f, 0.804f, 0.725f, 1f);

    [Tooltip("Màu chấm trạng thái khi ĐANG XAY. HTML .status-dot = #62E15D.")]
    [SerializeField] private Color mauDotDangXay = new Color(0.384f, 0.882f, 0.365f, 1f);

    [Tooltip("Màu chấm trạng thái khi máy RẢNH (xám).")]
    [SerializeField] private Color mauDotRanh = new Color(0.729f, 0.694f, 0.643f, 1f);

    [Tooltip("Toast hiện bao lâu trước khi mờ dần, giây.")]
    [SerializeField] private float toastGiuGiay = 1.8f;

    [Tooltip("Thời gian mờ dần của toast, giây.")]
    [SerializeField] private float toastFadeGiay = 0.35f;

    // ── Hiệu ứng: cả ba đều TUỲ CHỌN, để trống popup vẫn chạy đúng logic ──

    [Header("Hiệu ứng (TUỲ CHỌN)")]
    [Tooltip("TUỲ CHỌN. Hạt nguyên liệu bay vào phễu máy khi một mẻ bắt đầu.\n" +
             "Để trống ⇒ mẻ xay vẫn bắt đầu, chỉ không có hạt bay.")]
    [SerializeField] private MillIntakeFX fxNguyenLieu;

    [Tooltip("TUỲ CHỌN. Bao thành phẩm nảy ra + vòng sáng chờ thu ở vòng tròn đầu ra.\n" +
             "Để trống ⇒ không có nhịp nảy và không có vòng sáng.")]
    [SerializeField] private MillOutputBagFX fxBaoRa;

    [Tooltip("TUỲ CHỌN. Icon sản phẩm bay từ slot về nút KHO ở HUD khi bấm THU.\n" +
             "Để trống ⇒ hàng vẫn vào kho, chỉ không có icon bay.")]
    [SerializeField] private MillCollectFlyFX fxBayVeKho;

    [Tooltip("TUỲ CHỌN. Pháo hoa \"bùm bùm\" nổ ở bao thành phẩm khi một mẻ xay xong.\n" +
             "Để trống ⇒ vẫn có nhịp nảy bao, chỉ không có pháo hoa.")]
    [SerializeField] private MillCelebrationFX fxPhaoHoa;

    [Tooltip("TUỲ CHỌN. Khói + bong bóng phun khỏi phễu KHI MÁY ĐANG XAY.\n" +
             "Để trống ⇒ không có khói; máy rảnh và máy đang chạy nhìn giống nhau hơn.")]
    [SerializeField] private MillSmokeFX fxKhoi;

    // ═════════════════════════════ TRẠNG THÁI ═════════════════════════════

    /// <summary>Một slot của máy — chỉ tồn tại trong bộ nhớ, được lưu/nạp qua PlayerPrefs.</summary>
    private class SlotState
    {
        public MillRecipeData recipe;      // null = slot trống
        public long           endTicksUtc; // thời điểm xay xong, DateTime.UtcNow.Ticks
        public float          totalSec;    // tổng thời gian lượt xay, để vẽ thanh tiến độ
    }

    private const string LOG = "[MILL] ";

    private readonly List<MillRecipeCardUI> _cards      = new List<MillRecipeCardUI>();
    private SlotState[]                     _slotStates;
    private int                             _soSlotDaMo;
    private MillRecipeData                  _congThucChon;

    // Hàng rào chống dựng chuỗi mỗi frame — xem khối ghi chú ở đầu file.
    private int  _soDangXayDaHien   = int.MinValue;
    private int  _soChoThuDaHien    = int.MinValue;
    private int  _soDaMoDaHien      = int.MinValue;
    private int  _gemDaHien         = int.MinValue;
    private int  _trangThaiNutDaHien = int.MinValue;

    /// <summary>
    /// Mode ĐÃ VẼ của từng slot, lưu dạng int với −1 = "chưa vẽ lần nào".
    ///
    /// ⚠ VÌ SAO KHÔNG DÙNG `MillSlotUI.Mode` LÀM HÀNG RÀO:
    /// `_mode` bên trong MillSlotUI khởi tạo bằng `MillSlotMode.Empty`. Nếu Update chỉ gọi
    /// `BindEmpty()` khi `ui.Mode != Empty` thì một slot trống LÚC MỚI MỞ POPUP sẽ không
    /// bao giờ được vẽ — 5 root giữ nguyên trạng thái ai đó lưu trong prefab, thường là
    /// bật hết cùng lúc. Mảng này bị đặt lại −1 mỗi lần `Open()` nên frame đầu luôn vẽ thật.
    /// </summary>
    private int[] _modeDaVe;

    private Coroutine  _toastCo;
    private CanvasGroup _toastGroup;
    private bool        _daKhoiTao;

    // ── VỪA KHUNG MÀN HÌNH ──
    /// <summary>Lề an toàn quanh bảng — giống ShopManager/StallPopupUI.LE_AN_TOAN_POPUP.</summary>
    private const float LE_AN_TOAN_POPUP = 24f;

    private Vector3 _scaleGocBang;
    private Vector2 _viTriGocBang;
    private bool    _daLuuBang;

    /// <summary>Bộ nhớ đệm 4 góc — tránh cấp phát mảng mới cho mỗi node lúc đo dấu chân.</summary>
    private static readonly Vector3[] _gocTam = new Vector3[4];

    // ── CHẶN INPUT / NỀN MỜ ──
    /// <summary>CHÍNH popup này có đang giữ một nhịp khoá của FarmInputLock hay không.</summary>
    private bool _popupInputLockHeld;

    private Graphic _dimGraphic;
    private bool    _daTimDim;

    /// <summary>Popup có đang mở.</summary>
    public bool IsOpen
    {
        get
        {
            GameObject root = popupRoot != null ? popupRoot : gameObject;
            return root != null && root.activeInHierarchy;
        }
    }

    // ═════════════════════════════ VÒNG ĐỜI ═════════════════════════════

    private void Awake()
    {
        Instance = this;

        // Toast dùng CanvasGroup để mờ dần cả cụm bằng một giá trị.
        // ⚠ KHÔNG viết `GetComponent<CanvasGroup>() ?? AddComponent<CanvasGroup>()`:
        //   component thiếu trả về "fake-null"; `??` so tham chiếu nên coi như ĐÃ CÓ và
        //   không thêm gì, dòng sau chạm `.alpha` là nổ. Phải so tường minh `== null`.
        if (toastRoot != null)
        {
            _toastGroup = toastRoot.GetComponent<CanvasGroup>();
            if (_toastGroup == null)
                _toastGroup = toastRoot.AddComponent<CanvasGroup>();
        }

        BaoDamCoRaycaster();
        KhoiTaoTrangThai();
        GanSuKienNut();

        // Đóng sẵn. Đặt ở Awake để không lộ popup một frame lúc vào scene.
        GameObject r = popupRoot != null ? popupRoot : gameObject;
        if (r.activeSelf && r != gameObject)
            r.SetActive(false);

        // ⚠ CHẶN KÉO MAP — bug thật, console báo:
        //     "[UiProbe] ⛔ KÉO MAP BỊ CHẶN — UI dưới con trỏ (dòng đầu = thủ phạm)"
        // Trong SCN_Farm, node `Canvas_Popup/MillPopup_Root/PopupRoot` được LƯU Ở TRẠNG THÁI
        // BẬT, và con của nó `Dim` là một Image phủ kín màn hình (anchor 0;0 → 1;1) với
        // raycastTarget = 1, nằm trên canvas lồng `MillPopup_Root` (sortingOrder 410 — cao
        // nhất scene). Từ khung hình đầu tiên nó ăn hết con trỏ: map không kéo được dù CHƯA
        // AI mở popup.
        //
        // Dòng trên đã tắt PopupRoot, nhưng đó chỉ là một lớp. Tắt luôn raycast của Dim là
        // lớp thứ hai, phòng trường hợp ai đó (PopupManager / tool bake scene / SetActive
        // thẳng) bật PopupRoot lại mà không đi qua Open().
        DatChanRaycastDim(false);

        AnToast(true);
    }

    /// <summary>
    /// Lưới an toàn thứ ba cho lỗi "kéo map bị chặn": Start chạy SAU toàn bộ Awake của
    /// scene, nên nếu một script khác bật `PopupRoot` lên trong Awake của nó thì ở đây ta
    /// sửa lại. Popup đang thật sự mở thì không đụng vào.
    /// </summary>
    private void Start()
    {
        if (IsOpen) return;

        GameObject r = popupRoot != null ? popupRoot : gameObject;
        if (r != null && r != gameObject && r.activeSelf)
            r.SetActive(false);

        DatChanRaycastDim(false);
        ReleasePopupInputBlock();
    }

    /// <summary>
    /// Popup bị tắt bằng đường khác (PopupManager, đổi scene, ai đó SetActive thẳng).
    /// GỌI VÔ ĐIỀU KIỆN: `ReleasePopupInputBlock` là bất biến nên gọi chồng lên `Close()`
    /// cũng chỉ trả đúng một nhịp. Không làm thế thì popupLockCount kẹt > 0 và
    /// `FarmInputLock.BlockMapPan` đúng mãi mãi — map chết hẳn cho tới khi restart.
    /// </summary>
    private void OnDisable()
    {
        DatChanRaycastDim(false);
        ReleasePopupInputBlock();
    }

    /// <summary>
    /// ⚠ TỰ VÁ LỖI "POPUP NHƯ MỘT TẤM ẢNH" — bug thật, phát hiện 21/08.
    ///
    /// `MillPopupBuilderTool` dựng node gốc popup thành một **Canvas LỒNG**
    /// (`overrideSorting = true`, `sortingOrder = 400`) để nó luôn nằm trên mọi popup khác.
    /// Nhưng canvas lồng đăng ký toàn bộ Graphic con vào CHÍNH NÓ trong `GraphicRegistry`,
    /// và một `GraphicRaycaster` chỉ raycast những Graphic đăng ký vào canvas mà nó nằm
    /// trên. Nên `GraphicRaycaster` của `Canvas_Popup` KHÔNG thấy gì bên trong popup này.
    ///
    /// Thiếu raycaster ⇒ popup vẽ ra hoàn hảo, bánh răng vẫn quay, băng tải vẫn chạy,
    /// NHƯNG không một pixel nào hit-test được: nút X chết, card chết, nút THU chết,
    /// kéo-thả không bao giờ nổ `OnBeginDrag`. **Console hoàn toàn sạch** — không lỗi đỏ,
    /// không warning. Đó là kiểu bug tốn nhiều giờ nhất nếu không biết chỗ mà nhìn.
    ///
    /// Tool đã được sửa để luôn gắn raycaster. Hàm này là lưới an toàn thứ hai, cho
    /// trường hợp scene/prefab cũ chưa dựng lại, hoặc ai đó xoá component sau này.
    /// Nó CỘNG THÊM, không sửa gì đang chạy, nên an toàn tuyệt đối.
    /// </summary>
    private void BaoDamCoRaycaster()
    {
        // Quét cả nhánh, kể cả node đang tắt (popupRoot mặc định tắt): canvas lồng có thể
        // nằm ở node gốc HOẶC ở một node con, tuỳ phiên bản tool đã dựng.
        Canvas[] ds = GetComponentsInChildren<Canvas>(true);
        if (ds == null) return;

        for (int i = 0; i < ds.Length; i++)
        {
            Canvas cv = ds[i];
            if (cv == null) continue;

            // So tường minh `== null`: component thiếu trả về "fake-null", `?.`/`??` coi
            // như ĐÃ CÓ và bỏ qua — đúng cái bẫy đang phải vá ở đây.
            GraphicRaycaster gr = cv.GetComponent<GraphicRaycaster>();
            if (gr != null) continue;

            cv.gameObject.AddComponent<GraphicRaycaster>();

            Debug.LogWarning(LOG + "Canvas '" + cv.gameObject.name + "' thiếu GraphicRaycaster " +
                             "⇒ đã tự thêm lúc chạy để popup bấm được. Chạy " +
                             "Tools/Farm/Popup May Xay/1. Dung Popup để sửa hẳn trong scene + prefab.",
                             cv.gameObject);
        }
    }

    private void OnDestroy()
    {
        // Popup bị Destroy giữa chừng (đổi scene) vẫn phải trả khoá input, nếu không map
        // đứng im mà không còn object nào để đóng lại cho đúng.
        ReleasePopupInputBlock();

        if (Instance == this) Instance = null;
    }

    // ═════════════════════════════ NỀN MỜ + KHOÁ INPUT ═════════════════════════════

    /// <summary>
    /// Tìm tấm nền mờ phủ kín màn hình (node <c>Dim</c>) — thứ ĐANG ăn hết con trỏ khi popup
    /// đóng. Dò một lần rồi nhớ luôn, kể cả khi không tìm thấy (khỏi quét lại mỗi lần đóng).
    ///
    /// Dò theo TÊN chứ không bằng ô kéo thả trong Inspector: cấu trúc do
    /// <c>MillPopupBuilderTool</c> dựng luôn là <c>PopupRoot/Dim</c> + <c>PopupRoot/Window</c>,
    /// và yêu cầu là KHÔNG phải sờ tay vào scene.
    /// </summary>
    private Graphic TimDim()
    {
        if (_daTimDim) return _dimGraphic;
        _daTimDim = true;

        Transform goc = (popupRoot != null) ? popupRoot.transform : transform;
        if (goc == null) return null;

        // 1) Đúng tên "Dim" ngay dưới popupRoot — đường chính.
        Transform t = goc.Find("Dim");

        // 2) Dự phòng: con ĐẦU TIÊN phủ kín (anchor 0;0 → 1;1) mà KHÔNG phải tấm bảng.
        if (t == null)
        {
            RectTransform bang = TimBangPopup();
            for (int i = 0; i < goc.childCount; i++)
            {
                RectTransform con = goc.GetChild(i) as RectTransform;
                if (con == null || con == bang) continue;
                if (con.anchorMin != Vector2.zero || con.anchorMax != Vector2.one) continue;

                t = con;
                break;
            }
        }

        if (t != null) _dimGraphic = t.GetComponent<Graphic>();
        return _dimGraphic;
    }

    /// <summary>
    /// Bật/tắt khả năng ăn raycast của tấm nền mờ. Popup ĐÓNG ⇒ luôn tắt, nếu không nó chặn
    /// kéo map. Popup MỞ ⇒ bật lại để click ra ngoài bảng không rơi xuống đồng ruộng.
    /// </summary>
    private void DatChanRaycastDim(bool chan)
    {
        Graphic g = TimDim();
        if (g == null) return;

        if (g.raycastTarget != chan)
            g.raycastTarget = chan;
    }

    /// <summary>Giữ khoá input + bật lớp chặn raycast. Gọi nhiều lần vẫn chỉ tăng MỘT nhịp.</summary>
    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _popupInputLockHeld = true;
    }

    /// <summary>
    /// Trả khoá input + tắt lớp chặn raycast. BẤT BIẾN (idempotent): gọi mười lần cũng chỉ
    /// giảm đúng một nhịp, và gọi khi chưa từng giữ khoá thì không giảm nhịp nào. Nhờ vậy
    /// Close / OnDisable / OnDestroy được phép gọi chồng lên nhau mà popupLockCount không
    /// bao giờ tụt xuống dưới 0 (kẹp về 0 ⇒ ăn trộm mất một nhịp của popup KHÁC).
    /// </summary>
    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (!_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _popupInputLockHeld = false;
    }

    private void Update()
    {
        // Popup đóng thì không tính gì cả. Thời điểm xong lưu tuyệt đối nên không cần
        // "chạy nền" — mở lại là tự đúng.
        if (!IsOpen || config == null) return;

        long nowTicks = DateTime.UtcNow.Ticks;

        int soDangXay = 0;
        int soChoThu  = 0;
        int soTrong   = 0;

        int n = _slotStates != null ? _slotStates.Length : 0;
        for (int i = 0; i < n; i++)
        {
            SlotState st = _slotStates[i];
            MillSlotUI ui = (slots != null && i < slots.Length) ? slots[i] : null;

            // ── Slot chưa mở ──
            if (i >= _soSlotDaMo)
            {
                if (ui != null) VeSlotChuaMo(ui, i);
                continue;
            }

            // ── Slot trống ──
            if (st.recipe == null)
            {
                soTrong++;
                // Chỉ Bind khi mode ĐỔI: BindEmpty gọi SetMode ⇒ bật/tắt 5 GameObject và
                // kéo layout theo. Gọi mỗi frame là 60 lần dựng lại layout mỗi giây.
                if (ui != null && CanVeLai(i, MillSlotMode.Empty)) ui.BindEmpty();
                continue;
            }

            float conLai = (float)((st.endTicksUtc - nowTicks) / (double)TimeSpan.TicksPerSecond);

            if (conLai <= 0f)
            {
                soChoThu++;
                if (ui != null && CanVeLai(i, MillSlotMode.ReadyToCollect)) ui.BindReady(st.recipe);
            }
            else
            {
                soDangXay++;
                // BindRunning PHẢI gọi mỗi frame (đồng hồ + thanh tiến độ). Nó tự có hàng
                // rào bên trong nên không dựng chuỗi và không SetMode lặp lại.
                GhiModeDaVe(i, MillSlotMode.Running);
                if (ui != null)
                    ui.BindRunning(st.recipe, conLai, st.totalSec, config.TinhGiaTangToc(conLai));
            }
        }

        // Đọc TRƯỚC khi CapNhatBadgeVaTongKet ghi đè — cần giá trị của frame trước để
        // phát hiện "vừa có thêm một mẻ xong" (xem khối dưới).
        int choThuTruoc = _soChoThuDaHien;

        CapNhatBadgeVaTongKet(soDangXay, soChoThu);
        CapNhatSoDuGem();
        CapNhatNutLon(soTrong);
        // Chỉ chạy animation bánh răng/băng tải khi máy CÓ THỨC ĂN đang xay (soDangXay > 0)
        DatChayAnimation(soDangXay > 0);

        // Khói phun khi VÀ CHỈ KHI có slot đang xay — đây là thứ duy nhất phân biệt "máy
        // đang làm việc" với "máy rảnh" mà không cần đọc chữ. Có hàng rào bên trong nên gọi
        // mỗi frame là an toàn.
        if (fxKhoi != null) fxKhoi.DatChay(soDangXay > 0);

        // Sự kiện "vừa có thêm một mẻ xong": số chờ thu TĂNG so với frame trước.
        // Bỏ qua frame đầu sau Open() (_soChoThuDaHien = int.MinValue) — lúc đó "tăng" chỉ
        // là hàng còn tồn từ phiên trước, ăn mừng ở đây là báo động sai.
        bool vuaXongMotMe = (choThuTruoc != int.MinValue) && (soChoThu > choThuTruoc);

        if (fxBaoRa != null)
        {
            // Trạng thái: còn hàng chưa thu ⇒ vòng sáng thở.
            fxBaoRa.DatSanSang(soChoThu > 0);

            if (vuaXongMotMe) fxBaoRa.PhatRoi();
        }

    }

    // ═════════════════════════════ MỞ / ĐÓNG ═════════════════════════════

    /// <summary>
    /// Mở popup: dựng lại danh sách card, chọn công thức đầu tiên chưa khoá, vẽ lại slot,
    /// bật animation, cập nhật số dư kim cương.
    /// </summary>
    public void Open()
    {
        if (config == null)
        {
            Debug.LogError(LOG + "Chưa gán MillConfig vào MillPopupUI ⇒ không mở được popup.", this);
            return;
        }

        string loi;
        if (!config.KiemTraHopLe(out loi))
        {
            Debug.LogError(LOG + "MillConfig không hợp lệ: " + loi, config);
            return;
        }

        if (!_daKhoiTao) KhoiTaoTrangThai();

        GameObject root = popupRoot != null ? popupRoot : gameObject;
        Transform p = root.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
                p.gameObject.SetActive(true);
            p = p.parent;
        }
        if (!root.activeSelf) root.SetActive(true);

        if (txtTitle != null) txtTitle.text = config.title;

        DungDanhSachCard();
        ChonCongThucDauTienMoDuoc();

        // Đặt nhãn số thứ tự + đăng ký sự kiện cho từng slot (một lần cho mỗi lần mở là đủ,
        // các hàm gán đều idempotent).
        GanSuKienSlot();

        // Áp số animation TỪ CONFIG. Làm ở đây (không phải Awake) để designer sửa config
        // rồi mở lại popup là thấy ngay, không cần chạy lại scene.
        if (gearLarge != null) gearLarge.Configure(config.gearLargeDegPerSec, true);
        if (gearSmall != null) gearSmall.Configure(config.gearSmallDegPerSec, false);
        if (belt != null)      belt.Configure(config.beltScrollPxPerSec, config.beltStripePeriodPx);

        if (beltItems != null)
        {
            for (int i = 0; i < beltItems.Length; i++)
            {
                if (beltItems[i] == null) continue;
                // Lệch pha = chỉ số × 1.5s, đúng như .mi-1 delay 0s / .mi-2 delay 1.5s.
                beltItems[i].Configure(config.itemCycleSeconds,
                                       i * config.itemStaggerSeconds,
                                       config.itemTravelPx);
            }
        }

        // Xoá hàng rào để lần Update đầu tiên chắc chắn vẽ lại toàn bộ chữ VÀ toàn bộ slot.
        // BẮT BUỘC: Dev B có thể sửa prefab giữa hai lần mở, và người chơi có thể lên cấp
        // trong lúc popup đóng ⇒ không được tin trạng thái vẽ của lần mở trước.
        if (_modeDaVe != null)
            for (int i = 0; i < _modeDaVe.Length; i++) _modeDaVe[i] = -1;

        _soDangXayDaHien    = int.MinValue;
        _soChoThuDaHien     = int.MinValue;
        _soDaMoDaHien       = int.MinValue;
        _gemDaHien          = int.MinValue;
        _trangThaiNutDaHien = int.MinValue;

        // Đặt trạng thái ban đầu của animation
        DatChayAnimation(false);

        AnToast(true);
        // [SkinUnifier 2026-09-21] Dong bo nut/vien/ruy bang theo bo cua Shop (chi doi sprite/mau/font).
        PopupSkinUnifier.ApDung(popupRoot != null ? popupRoot.transform : transform);
        // [FIX QA] Chu vua dung xong => xin dich sang tieng Anh ngay (re, da gop chung 1 khung hinh).
        Loc.RequestRescan();

        // Popup ĐANG MỞ ⇒ nền mờ phải ăn raycast trở lại (click ra ngoài bảng không được
        // rơi xuống đồng ruộng), và map phải ngừng kéo.
        DatChanRaycastDim(true);
        AcquirePopupInputBlock();

        // Chống tràn chữ cho các nhãn khung cứng — làm mỗi lần mở vì ngôn ngữ đổi được
        // giữa hai lần mở (bản tiếng Anh dài hơn bản tiếng Việt ở gần như mọi câu).
        ApChongTranChuToanPopup();

        // CUỐI CÙNG: đo dấu chân thật rồi co bảng cho vừa màn hình. Phải chạy SAU
        // DungDanhSachCard() (card mới vừa Instantiate) và sau khi chữ đã đổi cỡ.
        VuaKhungManHinh();
    }

    /// <summary>Đóng popup, dừng toàn bộ animation và lưu trạng thái ngay.</summary>
    public void Close()
    {
        // Nút X nằm NGOÀI ScrollRect nên vẫn bấm được giữa lúc người chơi đang kéo bao.
        // Khi đó card bị disable trước khi Unity kịp gửi OnEndDrag ⇒ bóng kéo sẽ nằm lại
        // trên màn hình vĩnh viễn. Dọn ở đây là đường thoát cuối cùng.
        MillDragSession.HuyNgay();
        SangVienSlot(false);

        // Icon bay-về-kho được gắn thẳng vào CANVAS (để nằm trên popup), không vào popupRoot
        // ⇒ tắt popupRoot KHÔNG tắt nó. Component MillCollectFlyFX cũng nằm trên node gốc
        // vẫn active nên OnDisable của nó không chạy. Phải dọn tường minh ở đây, nếu không
        // icon lơ lửng trên đồng ruộng sau khi đóng popup.
        if (fxBayVeKho != null) fxBayVeKho.DonSach();

        // Hai FX này nằm dưới popupRoot nên OnDisable của chúng cũng tự dọn. Gọi tường minh
        // ở đây để thứ tự dọn không phụ thuộc vào thứ tự Unity gửi OnDisable cho cây con.
        if (fxPhaoHoa != null) fxPhaoHoa.DonSach();
        if (fxKhoi != null)
        {
            fxKhoi.DatChay(false);
            fxKhoi.DonSach();
        }

        DungAnimation();
        LuuTrangThai();

        // Trả khoá TRƯỚC khi tắt node: ReleasePopupInputBlock đọc popupRoot, và tắt rồi
        // mới trả thì thứ tự vẫn đúng nhưng dễ bị ai đó "tối ưu" thành return sớm.
        DatChanRaycastDim(false);
        ReleasePopupInputBlock();

        GameObject root = popupRoot != null ? popupRoot : gameObject;
        if (root != null && root.activeSelf) root.SetActive(false);
    }

    // ═════════════════════════════ CARD CÔNG THỨC ═════════════════════════════

    private void DungDanhSachCard()
    {
        if (recipeCardPrefab == null || recipeContainer == null)
        {
            Debug.LogWarning(LOG + "Chưa gán recipeCardPrefab hoặc recipeContainer ⇒ " +
                             "danh sách công thức trống.", this);
            return;
        }

        int can = config.recipes.Length;

        // TÁI DÙNG card cũ thay vì Destroy + Instantiate lại mỗi lần mở: tránh rác GC và
        // tránh dựng lại layout của cả ScrollRect (giật một frame khi mở popup).
        while (_cards.Count < can)
        {
            // recipeContainer là Transform TRONG SCENE ⇒ Instantiate có cha là hợp lệ.
            // (Nếu nó là transform bên trong prefab asset thì Unity ném exception —
            //  trường hợp đó phải dùng PrefabUtility.LoadPrefabContents, không làm ở runtime.)
            MillRecipeCardUI card = Instantiate(recipeCardPrefab, recipeContainer);
            card.name = "RecipeCard_" + (_cards.Count + 1);
            card.OnClicked = ChonCongThuc;
            _cards.Add(card);
        }

        int cap = MillInventoryBridge.CapHienTai();

        for (int i = 0; i < _cards.Count; i++)
        {
            MillRecipeCardUI card = _cards[i];
            if (card == null) continue;

            if (i >= can)
            {
                // Card dư (config bị bớt công thức so với lần mở trước) — Bind(null) tự tắt.
                card.Bind(null, false);
                continue;
            }

            MillRecipeData r = config.recipes[i];
            bool moDuoc = (r != null) && (cap >= r.unlockLevel);
            card.Bind(r, moDuoc);
        }
    }

    private void ChonCongThucDauTienMoDuoc()
    {
        // Công thức đang chọn vẫn còn hợp lệ thì giữ nguyên — người chơi đóng/mở lại popup
        // không bị nhảy về công thức đầu.
        if (_congThucChon != null && MillInventoryBridge.DatCap(_congThucChon.unlockLevel))
        {
            ChonCongThuc(_congThucChon);
            return;
        }

        for (int i = 0; i < config.recipes.Length; i++)
        {
            MillRecipeData r = config.recipes[i];
            if (r == null) continue;

            if (MillInventoryBridge.CapHienTai() >= r.unlockLevel)
            {
                ChonCongThuc(r);
                return;
            }
        }

        ChonCongThuc(null);
    }

    private void ChonCongThuc(MillRecipeData r)
    {
        _congThucChon = r;

        for (int i = 0; i < _cards.Count; i++)
        {
            if (_cards[i] == null) continue;
            _cards[i].SetSelected(_cards[i].Recipe == r);
        }

        // Bong bóng đầu vào / đầu ra quanh máy đi theo công thức đang chọn.
        if (r != null)
        {
            if (txtOutputTag != null) txtOutputTag.text = r.displayName;
            DatAnh(imgOutputIcon, r.GetIcon());

            MillIngredient ing0 = (r.ingredients != null && r.ingredients.Length > 0) ? r.ingredients[0] : null;
            if (txtInputBubble != null) txtInputBubble.text = (ing0 != null) ? ("x" + ing0.amount) : string.Empty;
            DatAnh(imgInputIcon, ing0 != null ? ing0.icon : null);

            // BÓ NGUYÊN LIỆU TRÔI TRÊN BĂNG TẢI đi theo công thức đang chọn (video: chọn
            // "Cám cho gà" thì các bó LÚA chạy vào máy). Tool dựng 2 item này bằng sprite
            // placeholder tự vẽ — không đổi ở đây thì mãi mãi là hai chấm tròn vàng.
            DatAnhBeltItems(ing0 != null ? ing0.icon : null);
        }
        else
        {
            if (txtOutputTag != null)   txtOutputTag.text   = string.Empty;
            if (txtInputBubble != null) txtInputBubble.text = string.Empty;
            DatAnh(imgOutputIcon, null);
            DatAnh(imgInputIcon, null);
            DatAnhBeltItems(null);
        }

        _trangThaiNutDaHien = int.MinValue;   // buộc vẽ lại nút lớn
    }

    // ═════════════════════════════ BẢNG GỢI Ý (chỗ nút XAY NGAY cũ) ═════════════════════════════

    // Mã trạng thái bảng, dùng cho hàng rào chống dựng chuỗi mỗi frame.
    private const int NUT_XAY_NGAY   = 0;   // sẵn sàng: kéo là xay được
    private const int NUT_THIEU_NL   = 1;
    private const int NUT_HET_SLOT   = 2;
    private const int NUT_CHUA_CHON  = 3;

    /// <summary>
    /// Cập nhật BẢNG GỢI Ý dưới danh sách công thức. Đây KHÔNG còn là nút bấm — nó chỉ nói
    /// cho người chơi biết vì sao chưa xay được (hoặc nhắc thao tác kéo-thả).
    /// Nút bị tắt trong <see cref="GanSuKienNut"/>.
    /// </summary>
    private void CapNhatNutLon(int soSlotTrong)
    {
        int trangThai;

        if (_congThucChon == null)                            trangThai = NUT_CHUA_CHON;
        else if (soSlotTrong <= 0)                            trangThai = NUT_HET_SLOT;
        else if (!MillInventoryBridge.DuNguyenLieu(_congThucChon)) trangThai = NUT_THIEU_NL;
        else                                                  trangThai = NUT_XAY_NGAY;

        if (trangThai == _trangThaiNutDaHien) return;
        _trangThaiNutDaHien = trangThai;

        string chu;
        bool   sanSang;

        switch (trangThai)
        {
            // Ca bon cau deu da co san cap VI→EN trong LocStringTable (dong 272/274/312/313),
            // truoc day chi thieu buoc goi Loc.T nen ban tieng Anh van hien chu tieng Viet.
            case NUT_HET_SLOT:  chu = Loc.T("HẾT SLOT TRỐNG");      sanSang = false; break;
            case NUT_THIEU_NL:  chu = Loc.T("THIẾU NGUYÊN LIỆU");   sanSang = false; break;
            case NUT_CHUA_CHON: chu = Loc.T("CHỌN MỘT CÔNG THỨC");  sanSang = false; break;
            default:            chu = Loc.T("KÉO VÀO SLOT ĐỂ XAY"); sanSang = true;  break;
        }

        if (txtMainButton != null)   txtMainButton.text    = chu;
        if (imgMainButtonBg != null) imgMainButtonBg.color = sanSang ? mauNutBamDuoc : mauNutKhoa;
    }

    // ═════════════════════════════ KÉO-THẢ ═════════════════════════════

    /// <summary>
    /// Người chơi vừa nhấc một công thức lên. Gọi từ
    /// <see cref="MillRecipeDragSource.OnBeginDrag"/>.
    ///
    /// Chọn luôn công thức đó: bong bóng đầu ra và các bó cỏ trên băng tải đi theo công thức
    /// ĐANG CHỌN (xem <see cref="ChonCongThuc"/>), không đồng bộ thì người chơi kéo "Cám heo"
    /// mà máy vẫn hiện "Cám gà".
    /// </summary>
    public void BatDauKeo(MillRecipeData r)
    {
        if (r != null && r != _congThucChon) ChonCongThuc(r);
        SangVienSlot(true);
    }

    /// <summary>
    /// Người chơi vừa nhả tay. Gọi từ <see cref="MillRecipeDragSource.OnEndDrag"/>.
    /// </summary>
    /// <param name="aiNhan">Có slot nào nhận cú thả này không.</param>
    public void KetThucKeo(bool aiNhan)
    {
        SangVienSlot(false);

        if (aiNhan) return;

        // Thả ra chỗ trống mà im lặng thì người chơi tưởng game lỗi. Nói rõ vì sao.
        if (TimSlotTrongDauTien() < 0)
            HienToast("Hết slot trống — thu hàng đã xong rồi thả tiếp nhé!");
        else
            HienToast("Thả bao vào một SLOT TRỐNG để máy xay");
    }

    /// <summary>
    /// Slot <paramref name="idx"/> có nhận được một cú thả không: đã mở VÀ đang trống.
    /// Dùng cho viền sáng gợi ý; việc chặn thật nằm trong <see cref="ThaVaoSlot"/>.
    /// </summary>
    public bool SlotNhanDuoc(int idx)
    {
        if (!ChiSoHopLe(idx)) return false;
        if (idx >= _soSlotDaMo) return false;

        return _slotStates[idx].recipe == null;
    }

    /// <summary>
    /// NGƯỜI CHƠI THẢ công thức <paramref name="r"/> vào slot <paramref name="idx"/>.
    /// Đây là CHỖ CHẶN THẬT — mọi thông báo "vì sao không được" nằm ở đây, không ở UI.
    /// Gọi từ <see cref="MillSlotUI.OnDrop"/> qua closure trong <see cref="GanSuKienSlot"/>.
    /// </summary>
    public void ThaVaoSlot(int idx, MillRecipeData r)
    {
        if (r == null) return;
        if (!ChiSoHopLe(idx)) return;

        if (idx >= _soSlotDaMo)
        {
            HienToast(Loc.TF("Slot #{0} chưa mở", idx + 1));
            return;
        }

        if (_slotStates[idx].recipe != null)
        {
            HienToast(Loc.TF("Slot #{0} đang có hàng", idx + 1));
            return;
        }

        BatDauXay(idx, r);
    }

    /// <summary>
    /// Bật/tắt viền sáng gợi ý trên MỌI slot nhận được.
    ///
    /// Sáng cả hàng ngay khi nhấc bao lên (không đợi con trỏ đi tới) là điều kiện để
    /// kéo-thả dùng được trên mobile: ngón tay che mất slot, người chơi phải THẤY TRƯỚC
    /// chỗ nào thả được.
    /// </summary>
    private void SangVienSlot(bool on)
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            MillSlotUI ui = slots[i];
            if (ui == null) continue;

            ui.SetDropHighlight(on && SlotNhanDuoc(i), false);
        }
    }

    /// <summary>
    /// Bắt đầu một mẻ xay ở slot <paramref name="idx"/>. Trừ nguyên liệu, ghi trạng thái,
    /// lưu, phát hiệu ứng.
    ///
    /// KHÔNG vẽ slot ở đây: `Update` là nơi duy nhất vẽ slot (hàng rào `_modeDaVe`), frame
    /// sau nó tự thấy slot có recipe và chuyển sang Running.
    /// </summary>
    /// <returns>true nếu mẻ xay đã bắt đầu.</returns>
    private bool BatDauXay(int idx, MillRecipeData r)
    {
        if (r == null || !ChiSoHopLe(idx)) return false;
        if (idx >= _soSlotDaMo) return false;
        if (_slotStates[idx].recipe != null) return false;

        // Trừ nguyên liệu là bước CUỐI CÙNG có thể thất bại ⇒ đặt sát trước lúc ghi trạng
        // thái. TruNguyenLieu kiểm-hết-rồi-mới-trừ nên không bao giờ trừ một phần.
        if (!MillInventoryBridge.TruNguyenLieu(r))
        {
            HienToast("Thiếu nguyên liệu");
            return false;
        }

        float tong = r.BrewSeconds;

        SlotState st   = _slotStates[idx];
        st.recipe      = r;
        st.totalSec    = tong;
        st.endTicksUtc = DateTime.UtcNow.Ticks + (long)(tong * TimeSpan.TicksPerSecond);

        LuuTrangThai();

        // Hạt nguyên liệu chảy vào phễu máy — dùng icon nguyên liệu ĐẦU TIÊN, giống hệt các
        // bó cỏ trên băng tải (xem DatAnhBeltItems). Không có thì lấy icon sản phẩm.
        if (fxNguyenLieu != null)
        {
            MillIngredient ing0 = (r.ingredients != null && r.ingredients.Length > 0) ? r.ingredients[0] : null;
            fxNguyenLieu.Chay((ing0 != null && ing0.icon != null) ? ing0.icon : r.icon);
        }

        // Một cụm khói ngay lúc nhận nguyên liệu. Dòng khói liên tục do Update bật (theo số
        // slot đang xay) chỉ khởi động ở frame sau — chờ tới đó là mất liên hệ nhân-quả.
        if (fxKhoi != null) fxKhoi.PhunMotNhip();

        // idx là chỉ số 0-based, nhãn cho người chơi là 1-based (#1..#5).
        HienToast(Loc.TF("Đã cho {0} vào slot {1}", Loc.T(r.displayName), idx + 1));
        _trangThaiNutDaHien = int.MinValue;
        return true;
    }

    // ═════════════════════════════ HÀNH ĐỘNG TRÊN SLOT ═════════════════════════════

    private void BamThu(int idx)
    {
        if (!ChiSoHopLe(idx)) return;

        SlotState st = _slotStates[idx];
        if (st.recipe == null) return;

        // Chống bấm sớm: nút THU chỉ hiện khi đã xong, nhưng ai gọi bằng code thì vẫn phải chặn.
        if (DateTime.UtcNow.Ticks < st.endTicksUtc) return;

        MillRecipeData r = st.recipe;

        // Túi nông sản có sức chứa (SlotCapacity). Nếu ĐẦY và đây là loại mới thì
        // AddItem trả false — KHÔNG được xoá slot, nếu không người chơi mất trắng mẻ
        // hàng đã chờ xong. Giữ nguyên slot ở trạng thái "chờ thu" và báo cho người chơi.
        if (!MillInventoryBridge.CongSanPham(r))
        {
            HienToast("Túi nông sản đã đầy — bán bớt hoặc nâng cấp kho rồi thu lại!");
            return;
        }

        st.recipe      = null;
        st.endTicksUtc = 0L;
        st.totalSec    = 0f;

        LuuTrangThai();

        RectTransform oSlot = (slots != null && idx < slots.Length && slots[idx] != null)
                            ? slots[idx].transform as RectTransform
                            : null;

        // Icon bay từ slot về nút KHO ở HUD. Chỉ phát SAU khi CongSanPham thành công —
        // thấy hàng bay vào kho mà kho không tăng là lỗi tệ nhất có thể có ở đây.
        if (fxBayVeKho != null) fxBayVeKho.Bay(r != null ? r.GetIcon() : null, oSlot);

        // PHÁO HOA nổ NGAY TẠI Ô SLOT vừa bấm.
        //
        // ⚠ SỬA 21/08 — trước đây nổ lúc MẺ XAY XONG, và chủ dự án không bao giờ thấy:
        // mẻ thường xong khi popup đang ĐÓNG (ủ 2–10 phút), mà `Update` cố ý bỏ qua khung
        // hình đầu sau `Open()` để không ăn mừng oan cho hàng tồn từ phiên trước. Kết quả
        // là mở popup ra thấy 5 slot "chờ thu" mà chẳng có pháo hoa nào.
        //
        // Nổ lúc BẤM THU thì luôn đúng khoảnh khắc người chơi đang nhìn, và đó cũng là lúc
        // phần thưởng thực sự vào tay. Nhịp nảy + vòng sáng của bao (fxBaoRa) vẫn giữ ở
        // thời điểm xay xong để làm tín hiệu "có hàng mới".
        if (fxPhaoHoa != null && oSlot != null) fxPhaoHoa.BumTai(oSlot);

        // KHÔNG gọi slots[idx].BindEmpty() ở đây: Update là nơi DUY NHẤT vẽ slot, và nó
        // dùng hàng rào _modeDaVe. Vẽ ở hai chỗ thì hai chỗ sẽ lệch nhau lúc nào không biết.
        HienToast(Loc.TF("Đã thu {0} vào kho!", Loc.T(r.displayName)));
        _trangThaiNutDaHien = int.MinValue;
    }

    private void BamTangToc(int idx)
    {
        if (!ChiSoHopLe(idx)) return;

        SlotState st = _slotStates[idx];
        if (st.recipe == null) return;

        long nowTicks = DateTime.UtcNow.Ticks;
        if (nowTicks >= st.endTicksUtc) return;   // đã xong, không thu tiền

        float conLai = (float)((st.endTicksUtc - nowTicks) / (double)TimeSpan.TicksPerSecond);
        int   gia    = config.TinhGiaTangToc(conLai);

        if (!MillInventoryBridge.TruKimCuong(gia))
        {
            HienToast("Không đủ kim cương");
            return;
        }

        // Hoàn thành NGAY: đặt thời điểm xong về hiện tại. Update frame sau sẽ tự chuyển
        // slot sang ReadyToCollect — không nhân bản logic chuyển trạng thái ở đây.
        st.endTicksUtc = nowTicks;
        LuuTrangThai();

        HienToast(Loc.TF("Đã xay xong {0}", Loc.T(st.recipe.displayName)));
    }

    private void BamMoSlot(int idx)
    {
        if (config == null) return;

        // Chỉ mở được ĐÚNG slot kế tiếp — không cho nhảy cóc (slot #5 trước #4).
        if (idx != _soSlotDaMo)
        {
            HienToast("Hãy mở slot theo thứ tự");
            return;
        }

        if (idx >= config.slotCount) return;

        // Slot cuối cùng khoá theo CẤP, không bán bằng kim cương (video: "Chưa đủ cấp / Cấp 18").
        if (LaSlotCuoi(idx) && !MillInventoryBridge.DatCap(config.levelRequiredLastSlot))
        {
            HienToast(Loc.TF("Cần đạt cấp {0}", config.levelRequiredLastSlot));
            return;
        }

        if (!MillInventoryBridge.TruKimCuong(config.gemCostUnlockSlot))
        {
            HienToast("Không đủ kim cương");
            return;
        }

        _soSlotDaMo++;
        LuuTrangThai();

        HienToast("Đã mở thêm 1 slot xay!");
        _soDaMoDaHien       = int.MinValue;
        _trangThaiNutDaHien = int.MinValue;
    }

    private int TimSlotTrongDauTien()
    {
        if (_slotStates == null) return -1;

        int gioiHan = Mathf.Min(_soSlotDaMo, _slotStates.Length);
        for (int i = 0; i < gioiHan; i++)
        {
            if (_slotStates[i].recipe == null) return i;
        }

        return -1;
    }

    private bool LaSlotCuoi(int idx) => config != null && idx == config.slotCount - 1;

    private bool ChiSoHopLe(int idx) => _slotStates != null && idx >= 0 && idx < _slotStates.Length;

    // ═════════════════════════════ VẼ ═════════════════════════════

    /// <summary>
    /// Slot <paramref name="idx"/> có cần vẽ lại sang <paramref name="mode"/> không.
    /// Trả true ĐÚNG MỘT LẦN cho mỗi lần đổi mode (và luôn true ở frame đầu sau Open()).
    /// </summary>
    private bool CanVeLai(int idx, MillSlotMode mode)
    {
        if (_modeDaVe == null || idx < 0 || idx >= _modeDaVe.Length) return true;

        if (_modeDaVe[idx] == (int)mode) return false;

        _modeDaVe[idx] = (int)mode;
        return true;
    }

    /// <summary>Ghi nhận mode đã vẽ mà không hỏi — dùng cho mode phải Bind mỗi frame (Running).</summary>
    private void GhiModeDaVe(int idx, MillSlotMode mode)
    {
        if (_modeDaVe == null || idx < 0 || idx >= _modeDaVe.Length) return;
        _modeDaVe[idx] = (int)mode;
    }

    private void VeSlotChuaMo(MillSlotUI ui, int idx)
    {
        bool laCuoi  = LaSlotCuoi(idx);
        bool duCap   = !laCuoi || MillInventoryBridge.DatCap(config.levelRequiredLastSlot);
        bool ketTiep = (idx == _soSlotDaMo);

        if (!duCap)
        {
            if (CanVeLai(idx, MillSlotMode.LockedLevel))
                ui.BindLockedLevel(config.levelRequiredLastSlot);
            return;
        }

        // Mua được: chỉ slot KẾ TIẾP và chỉ khi đủ kim cương thì nút mới bấm được.
        bool duGem = ketTiep && MillInventoryBridge.SoKimCuong() >= config.gemCostUnlockSlot;

        // ⚠ SỬA 17/09 — "SLOT #5 HIỆN THANH GIÁ KIM CƯƠNG RỖNG, #4 HIỆN 💎15".
        //
        // Bản trước chỉ gọi BindUnlockGem khi ĐỔI MODE hoặc khi ví đổi:
        //     if (CanVeLai(idx, MillSlotMode.UnlockGem) || doiVi) ...
        // `CanVeLai` GHI LẠI mode ngay lần hỏi đầu rồi trả false mãi mãi, nên mọi đường vào
        // khác (mở slot #4 xong ⇒ #5 thành slot kế tiếp, đổi ngôn ngữ, layout chạy lại sau
        // khi popup co lại) đều KHÔNG được vẽ lại — ô giá giữ nguyên nội dung cũ, và nếu nó
        // chưa từng được ghi thì đứng nguyên chuỗi "0"/rỗng của prefab. Slot #4 may mắn rơi
        // đúng nhánh vẽ nên có số, slot #5 thì không: đúng cảnh trong ảnh chụp.
        //
        // Nay gọi MỖI FRAME. An toàn: `MillSlotUI.BindUnlockGem` đã có hàng rào
        // `_giaGemDangHien` bên trong nên chỉ dựng chuỗi khi CON SỐ đổi — không thêm một
        // byte rác nào mỗi frame (xem khối ghi chú "CHỐNG RÁC MỖI FRAME" ở đầu file).
        GhiModeDaVe(idx, MillSlotMode.UnlockGem);
        ui.BindUnlockGem(config.gemCostUnlockSlot, duGem);
    }

    private void CapNhatBadgeVaTongKet(int soDangXay, int soChoThu)
    {
        bool doiBadge = (soDangXay != _soDangXayDaHien);
        bool doiTong  = doiBadge || (soChoThu != _soChoThuDaHien) || (_soSlotDaMo != _soDaMoDaHien);

        if (doiBadge)
        {
            if (txtStatusBadge != null)
            {
                // Định dạng CHỐT: dấu · (middle dot U+00B7), không phải • và không phải "-".
                txtStatusBadge.text = (soDangXay > 0)
                    ? Loc.TF("Đang xay · {0} slot", soDangXay)
                    : "Máy đang rảnh";
            }

            if (imgStatusDot != null)
                imgStatusDot.color = (soDangXay > 0) ? mauDotDangXay : mauDotRanh;
        }

        if (doiTong && txtSlotSummary != null && config != null)
        {
            // "3/5 slot đã mở · 0 đang xay · 2 chờ thu"
            txtSlotSummary.text = Loc.TF("{0}/{1} slot đã mở · {2} đang xay · {3} chờ thu",
                                         _soSlotDaMo, config.slotCount, soDangXay, soChoThu);
        }

        _soDangXayDaHien = soDangXay;
        _soChoThuDaHien  = soChoThu;
        _soDaMoDaHien    = _soSlotDaMo;
    }

    private void CapNhatSoDuGem()
    {
        int gem = MillInventoryBridge.SoKimCuong();
        if (gem == _gemDaHien) return;

        _gemDaHien = gem;
        if (txtGemBalance != null)
            txtGemBalance.text = gem.ToString();
    }

    /// <summary>
    /// Dừng toàn bộ animation khi ĐÓNG popup — popup đã ẩn thì không có lý do gì để
    /// RotatingGear/UIScrollingTexture/ConveyorItem còn chạy Update và đốt CPU.
    /// </summary>
    private void DungAnimation()
    {
        if (gearLarge != null && gearLarge.IsRunning) gearLarge.SetRunning(false);
        if (gearSmall != null && gearSmall.IsRunning) gearSmall.SetRunning(false);
        if (belt != null && belt.IsRunning)           belt.SetRunning(false);

        if (beltItems == null) return;

        for (int i = 0; i < beltItems.Length; i++)
        {
            ConveyorItem it = beltItems[i];
            if (it == null) continue;
            if (it.IsRunning) it.SetRunning(false);
        }
    }

    /// <summary>
    /// Bật/tắt animation của khối máy: bánh răng lớn/nhỏ, băng tải, các bó cỏ trên băng tải.
    /// Chỉ chạy khi máy CÓ THỨC ĂN đang xay (soDangXay > 0).
    /// </summary>
    private void DatChayAnimation(bool dangChay)
    {
        if (gearLarge != null && gearLarge.IsRunning != dangChay) gearLarge.SetRunning(dangChay);
        if (gearSmall != null && gearSmall.IsRunning != dangChay) gearSmall.SetRunning(dangChay);
        if (belt != null && belt.IsRunning != dangChay)           belt.SetRunning(dangChay);

        if (beltItems == null) return;

        for (int i = 0; i < beltItems.Length; i++)
        {
            ConveyorItem it = beltItems[i];
            if (it == null) continue;
            if (it.IsRunning != dangChay) it.SetRunning(dangChay);
        }
    }

    /// <summary>
    /// Đổi sprite của các bó nguyên liệu trên băng tải theo icon nguyên liệu ĐẦU TIÊN của
    /// công thức đang chọn. null ⇒ giữ nguyên placeholder (còn hơn tắt hẳn, băng tải trống
    /// trơn nhìn như hỏng).
    /// </summary>
    private void DatAnhBeltItems(Sprite icon)
    {
        if (beltItems == null || icon == null) return;

        for (int i = 0; i < beltItems.Length; i++)
        {
            if (beltItems[i] == null) continue;

            Image img = beltItems[i].GetComponent<Image>();
            if (img != null && img.sprite != icon)
            {
                img.sprite = icon;
                // Sprite icon nông sản có tỉ lệ khác placeholder vuông — giữ tỉ lệ gốc
                // để lúa không bị bóp méo thành hình vuông.
                img.preserveAspect = true;
            }
        }
    }

    private static void DatAnh(Image img, Sprite s)
    {
        if (img == null) return;
        img.sprite  = s;
        img.enabled = (s != null);
    }

    // ═════════════════════════════ TOAST ═════════════════════════════

    /// <summary>Hiện thông báo ngắn giữa popup, tự ẩn sau <c>toastGiuGiay</c> + fade.</summary>
    public void HienToast(string noiDung)
    {
        if (toastRoot == null || toastText == null) return;

        toastText.text = noiDung;
        if (!toastRoot.activeSelf) toastRoot.SetActive(true);
        if (_toastGroup != null) _toastGroup.alpha = 1f;

        // Toast mới ĐÈ toast cũ: dừng coroutine trước, nếu không hai cái cùng fade và
        // toast mới bị ẩn theo đồng hồ của toast cũ.
        if (_toastCo != null) StopCoroutine(_toastCo);
        _toastCo = StartCoroutine(ChayToast());
    }

    private IEnumerator ChayToast()
    {
        yield return new WaitForSecondsRealtime(toastGiuGiay);

        // Realtime: popup thường mở lúc Time.timeScale = 0, dùng WaitForSeconds thì toast
        // treo mãi không tắt.
        float t = 0f;
        while (t < toastFadeGiay)
        {
            t += Time.unscaledDeltaTime;
            if (_toastGroup != null)
                _toastGroup.alpha = Mathf.Clamp01(1f - (t / toastFadeGiay));
            yield return null;
        }

        AnToast(false);
        _toastCo = null;
    }

    private void AnToast(bool ngayLapTuc)
    {
        if (_toastGroup != null) _toastGroup.alpha = 0f;

        if (toastRoot != null && toastRoot.activeSelf)
            toastRoot.SetActive(false);

        if (ngayLapTuc && _toastCo != null)
        {
            StopCoroutine(_toastCo);
            _toastCo = null;
        }
    }

    // ═════════════════════════════ GẮN SỰ KIỆN ═════════════════════════════

    private void GanSuKienNut()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(Close);
        }

        if (btnMain != null)
        {
            // KHÔNG còn nút "XAY NGAY" — luồng bắt đầu xay đã chuyển hẳn sang KÉO-THẢ
            // (chốt 21/08, xem khối ghi chú đầu file). Node giữ lại làm BẢNG GỢI Ý.
            //
            // ⚠ TẮT COMPONENT, KHÔNG dùng `interactable = false`:
            // interactable = false kích hoạt Disabled tint của Button (MillPopupBuilderTool
            // đặt disabled alpha = 0.55) ⇒ bảng gợi ý bị mờ đi trông như đang lỗi, và
            // `CapNhatNutLon` tô màu vào imgMainButtonBg sẽ bị Button tô lại đè lên.
            // Tắt component thì màu do CapNhatNutLon giữ nguyên và click cũng không vào.
            btnMain.onClick.RemoveAllListeners();
            btnMain.enabled = false;
        }
    }

    private void GanSuKienSlot()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            MillSlotUI ui = slots[i];
            if (ui == null) continue;

            ui.SetIndexLabel(i + 1);

            // `idx` phải là biến CỤC BỘ của mỗi vòng lặp, không dùng trực tiếp `i`:
            // closure bắt BIẾN chứ không bắt GIÁ TRỊ, dùng `i` thì cả 5 slot đều gọi với
            // i = slots.Length. Đây là bug kinh điển, C# 5+ chỉ tự xử lý cho foreach.
            int idx = i;

            // Gán thẳng (không +=) để mở popup nhiều lần không cộng dồn nhiều handler
            // ⇒ tránh cảnh bấm THU một lần mà cộng kho ba lần.
            ui.OnCollect = () => BamThu(idx);
            ui.OnSpeedUp = () => BamTangToc(idx);
            ui.OnUnlock  = () => BamMoSlot(idx);

            // Kéo-thả: slot không biết nó là slot thứ mấy, closure `idx` là cầu nối.
            ui.OnDropRecipe = r => ThaVaoSlot(idx, r);
            ui.CoTheNhanTha = () => SlotNhanDuoc(idx);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  VỪA KHUNG MÀN HÌNH (fit-to-screen)
    //  Cùng một cách làm với ShopManager.VuaKhungManHinh() và
    //  StallPopupUI.VuaKhungManHinh(): nhớ scale gốc lúc mở lần đầu, đo DẤU CHÂN THẬT,
    //  so với khung canvas trừ lề, rồi nhân một hệ số ≤ 1 — KHÔNG BAO GIỜ phóng to.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tìm tấm bảng gỗ (node <c>Window</c>) — thứ THẬT SỰ phải co lại.
    ///
    /// ⚠ KHÔNG được co <see cref="popupRoot"/>: ô đó trỏ vào <c>PopupRoot</c>, một node phủ
    /// kín màn hình (anchor 0;0 → 1;1) chứa CẢ tấm nền mờ <c>Dim</c>. Co nó là nền mờ tự thu
    /// lại thành một ô vuông giữa màn hình và lòi cả map ra hai bên — đúng cái bẫy
    /// StallPopupUI đã dính (popupRoot của nó là Panel_Dim, bảng thật là Popup_Main).
    ///
    /// Thứ tự dò: ô kéo thả → con tên "Window" → con tên "Popup_Main" → tổ tiên của slot #1
    /// nằm ngay dưới popupRoot → con ĐẦU TIÊN không-phủ-kín. Năm đường này để KHÔNG cần
    /// thao tác tay nào trong Unity.
    /// </summary>
    private RectTransform TimBangPopup()
    {
        if (popupBoard != null) return popupBoard;

        Transform goc = (popupRoot != null) ? popupRoot.transform : transform;
        if (goc == null) return null;

        Transform t = goc.Find("Window");
        if (t == null) t = goc.Find("Popup_Main");
        if (t != null) popupBoard = t as RectTransform;

        // Dò từ dưới lên: slot #1 chắc chắn nằm TRONG bảng, leo cha tới khi chạm con trực
        // tiếp của popupRoot.
        if (popupBoard == null && slots != null && slots.Length > 0 && slots[0] != null)
        {
            Transform cur = slots[0].transform;
            while (cur != null && cur.parent != null && cur.parent != goc)
                cur = cur.parent;

            if (cur != null && cur.parent == goc)
                popupBoard = cur as RectTransform;
        }

        // Cuối cùng: con đầu tiên KHÔNG phủ kín màn hình (nền mờ thì phủ kín ⇒ bị loại).
        if (popupBoard == null)
        {
            for (int i = 0; i < goc.childCount; i++)
            {
                RectTransform con = goc.GetChild(i) as RectTransform;
                if (con == null) continue;
                if (con.anchorMin == Vector2.zero && con.anchorMax == Vector2.one) continue;

                popupBoard = con;
                break;
            }
        }

        return popupBoard;
    }

    /// <summary>
    /// Đo DẤU CHÂN THẬT của <paramref name="bang"/> trong KHÔNG GIAN CỤC BỘ của chính nó.
    ///
    /// ⚠ VÌ SAO KHÔNG DÙNG <c>RectTransformUtility.CalculateRelativeRectTransformBounds</c>:
    /// hàm đó gom MỌI RectTransform con đang bật, KỂ CẢ phần bị xén. Danh sách công thức của
    /// popup này là một ScrollRect: <c>RecipeList/Viewport</c> có RectMask2D còn
    /// <c>Content</c> có VerticalLayoutGroup + ContentSizeFitter, nên Content cao theo SỐ
    /// CARD (4 card ≈ 650px trong khung chỉ 498px). Đo kiểu đó thì dấu chân phình ra theo số
    /// công thức và bảng bị co quá tay — càng thêm công thức, popup càng bé.
    ///
    /// Ở đây ta tự duyệt và DỪNG tại node có Mask / RectMask2D (vẫn tính rect của chính nó,
    /// bỏ qua con của nó). Đổi lại ta được đúng thứ MẮT NHÌN THẤY, và vẫn tự động gom
    /// ruy-băng tiêu đề + nút X thò ra ngoài mép bảng, vẫn tự động bỏ qua toast đang tắt.
    /// </summary>
    /// <returns>false nếu không đo được (bảng rỗng).</returns>
    private bool DoDauChan(RectTransform bang, out Vector2 tamCucBo, out Vector2 cheoCucBo)
    {
        tamCucBo  = Vector2.zero;
        cheoCucBo = Vector2.zero;
        if (bang == null) return false;

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, 0f);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, 0f);

        GomDauChan(bang, bang, ref min, ref max);

        if (min.x > max.x || min.y > max.y) return false;

        tamCucBo  = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
        cheoCucBo = new Vector2(max.x - min.x, max.y - min.y);
        return true;
    }

    private void GomDauChan(RectTransform goc, RectTransform node, ref Vector3 min, ref Vector3 max)
    {
        // So tường minh `== null`: component/GameObject đã Destroy trả về "fake-null".
        if (node == null || !node.gameObject.activeSelf) return;

        node.GetWorldCorners(_gocTam);
        for (int i = 0; i < 4; i++)
        {
            Vector3 cb = goc.InverseTransformPoint(_gocTam[i]);
            if (cb.x < min.x) min.x = cb.x;
            if (cb.y < min.y) min.y = cb.y;
            if (cb.x > max.x) max.x = cb.x;
            if (cb.y > max.y) max.y = cb.y;
        }

        // Node CẮT ⇒ con của nó bị xén, không tính vào dấu chân. Xem khối ghi chú ở
        // DoDauChan để biết vì sao đây là điểm khác biệt quan trọng nhất so với bản Stall.
        if (node.GetComponent<RectMask2D>() != null) return;
        if (node.GetComponent<Mask>() != null) return;

        for (int i = 0; i < node.childCount; i++)
            GomDauChan(goc, node.GetChild(i) as RectTransform, ref min, ref max);
    }

    /// <summary>
    /// Co bảng cho vừa canvas và dời cho ruy-băng tiêu đề thôi bị cắt ở mép trên.
    ///
    /// ĐO ĐƯỢC (SCN_Farm, 2026-09-17):
    ///   • rect bảng (Window)           = 1560 × 900
    ///   • DẤU CHÂN THẬT                = 1566.4 × 992.0, tâm lệch (+3.2 ; +46.0)
    ///       – ruy-băng Ribbon thò LÊN  +92 trên mép bảng  (y đỉnh = +542 so với tâm rect)
    ///       – nút X (64×64 · scale 1.5) thò SANG PHẢI +6.4 (x phải = +786.4)
    ///     ⇒ tâm HÌNH cao hơn tâm RECT đúng 46 — đó chính là lý do ruy-băng luôn là thứ bị
    ///       cắt ĐẦU TIÊN dù bảng đã co vừa. Dời xuống đúng 46 thì hết cắt.
    ///   • chuỗi scale tới canvas       = MillPopup_Root 1.2 × PopupRoot 0.9 = 1.08
    ///     ⇒ dấu chân TRÊN CANVAS       = 1691.7 × 1071.4
    ///   • Canvas_Popup: Scale With Screen Size, 1920×1080, Match Width-Or-Height = 0.5
    ///
    /// ⚠ VÌ SAO KHÔNG SO VỚI `rtBang.parent.rect` NHƯ BẢN STALL:
    /// ở đây chuỗi cha KHÔNG phải scale 1 — `MillPopup_Root` để localScale 1.2 và `PopupRoot`
    /// để 0.9. Rect của PopupRoot vẫn là 1920×1080 nhưng nó VẼ RA to gấp 1.08, nên so với
    /// rect cha là tưởng còn thừa chỗ trong khi thực tế đã tràn 8%. Phải quy dấu chân về
    /// ĐƠN VỊ CANVAS bằng tỉ số lossyScale rồi mới so.
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

        Canvas cv = rtBang.GetComponentInParent<Canvas>();
        if (cv == null) return;

        RectTransform rtCanvas = (cv.rootCanvas != null ? cv.rootCanvas : cv).transform as RectTransform;
        if (rtCanvas == null) return;

        if (!_daLuuBang)
        {
            _daLuuBang    = true;
            _scaleGocBang = rtBang.localScale;
            _viTriGocBang = rtBang.anchoredPosition;
        }

        // Trả về NGUYÊN BẢN trước khi đo. Bounds tính theo không gian cục bộ của bảng nên
        // không dính scale của chính nó, nhưng độ lệch vị trí thì CỘNG DỒN — không trả về
        // gốc là mỗi lần mở bảng lại trôi thêm một đoạn.
        rtBang.localScale       = _scaleGocBang;
        rtBang.anchoredPosition = _viTriGocBang;

        // DungDanhSachCard() vừa Instantiate thêm card vào ScrollRect ngay trước đó. Chưa ép
        // layout chạy thì card mới còn nằm ở rect của prefab và GetWorldCorners trả số cũ.
        Canvas.ForceUpdateCanvases();

        Vector2 tam, cheo;
        if (!DoDauChan(rtBang, out tam, out cheo)) return;
        if (cheo.x < 1f || cheo.y < 1f) return;

        // Quy ĐƠN VỊ CỤC BỘ CỦA BẢNG → ĐƠN VỊ CANVAS.
        Vector3 lsBang   = rtBang.lossyScale;
        Vector3 lsKhung  = rtKhung.lossyScale;
        Vector3 lsCanvas = rtCanvas.lossyScale;
        if (Mathf.Abs(lsCanvas.x) < 1e-5f || Mathf.Abs(lsCanvas.y) < 1e-5f) return;

        float rx = lsBang.x / lsCanvas.x;      // 1 đơn vị cục bộ bảng = rx đơn vị canvas
        float ry = lsBang.y / lsCanvas.y;
        float kx = lsKhung.x / lsCanvas.x;     // 1 đơn vị anchoredPosition = kx đơn vị canvas
        float ky = lsKhung.y / lsCanvas.y;
        if (Mathf.Abs(kx) < 1e-5f || Mathf.Abs(ky) < 1e-5f) return;

        float rongDauChan = Mathf.Abs(cheo.x * rx);
        float caoDauChan  = Mathf.Abs(cheo.y * ry);
        if (rongDauChan < 1f || caoDauChan < 1f) return;

        float rongKhung = rtCanvas.rect.width  - LE_AN_TOAN_POPUP * 2f;
        float caoKhung  = rtCanvas.rect.height - LE_AN_TOAN_POPUP * 2f;
        if (rongKhung < 1f || caoKhung < 1f) return;

        // Mathf.Min(1f, …) ⇒ CHỈ ĐƯỢC CO, không bao giờ phóng to. Bảng vẽ ở 1560×900 cho
        // màn 1920×1080 là cố ý; kéo nó to ra trên màn rộng là làm vỡ art.
        float heSo = Mathf.Min(1f, Mathf.Min(rongKhung / rongDauChan, caoKhung / caoDauChan));
        rtBang.localScale = _scaleGocBang * heSo;

        // ── CĂN GIỮA THEO HÌNH, KHÔNG THEO RECT ───────────────────────────────────
        // Bảng căn giữa theo RECT, nhưng phần NHÌN THẤY lệch LÊN TRÊN 46 vì ruy-băng thò ra
        // khỏi mép trên mà mép dưới không thò gì. Đó là lý do ruy-băng "FEED MILL" bị mép
        // trên màn hình cắt cụt dù bảng đã co vừa.
        //
        // Tính thẳng trong ĐƠN VỊ CANVAS nên nó gánh luôn cả offset -33 của PopupRoot: tâm
        // HÌNH của bảng rơi đúng tâm màn hình bất kể cha bị dời/scale bao nhiêu.
        Vector2 tamCanvas   = rtCanvas.rect.center;
        Vector3 pivotHienTai = rtCanvas.InverseTransformPoint(rtBang.position);

        float dichX = (tamCanvas.x - tam.x * rx * heSo) - pivotHienTai.x;
        float dichY = (tamCanvas.y - tam.y * ry * heSo) - pivotHienTai.y;

        rtBang.anchoredPosition = _viTriGocBang + new Vector2(dichX / kx, dichY / ky);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CHỐNG TRÀN CHỮ (chỉ CO LẠI, không bao giờ phóng to)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bản tiếng Anh dài hơn bản tiếng Việt ở gần như mọi câu ("THIẾU NGUYÊN LIỆU" →
    /// "NEED INGREDIENTS", "Cám gà" → "Cattle Feed Mix"), mà mọi nhãn trong popup này đều
    /// để <c>overflowMode = Overflow</c> và TẮT auto-size ⇒ chữ tràn ra ngoài khung gỗ.
    ///
    /// Ở đây bật auto-size CHỈ-CO: <c>fontSizeMax</c> = đúng cỡ designer đã đặt (nên bản
    /// tiếng Việt ngắn KHÔNG đổi một pixel nào), <c>fontSizeMin</c> = 0.75× cỡ đó, và cắt
    /// đuôi bằng "…" nếu vẫn không vừa.
    /// </summary>
    private void ApChongTranChuToanPopup()
    {
        // Ô hẹp nhất: Txt_OutputTag 220×54 @27pt — "Cattle Feed Mix" là 15 ký tự, sát mép.
        ApChongTranChu(txtOutputTag);
        ApChongTranChu(txtMainButton);    // 396×80 @32pt — "NEED INGREDIENTS"
        ApChongTranChu(txtStatusBadge);   // @28pt — "Mill is idle"
        ApChongTranChu(txtSlotSummary);   // 806×32 @24pt — "3/5 slots open · 0 milling · 0 ready"
        ApChongTranChu(txtTitle);         // 720×142 @48pt — "FEED MILL"
        ApChongTranChu(toastText);
    }

    /// <summary>
    /// Auto-size CHỈ-CO cho một nhãn: trần = cỡ chữ ĐANG CÓ, sàn = 0.75× cỡ đó.
    /// Idempotent — gọi lại lần thứ hai không hạ trần thêm lần nữa (đọc
    /// <c>fontSizeMax</c> đã đặt thay vì <c>fontSize</c> đã bị auto-size ghi đè).
    /// </summary>
    internal static void ApChongTranChu(TMP_Text txt)
    {
        if (txt == null) return;

        // Cắt đuôi thay vì để chữ tràn ra ngoài khung gỗ — đúng trong mọi trường hợp.
        if (txt.overflowMode != TextOverflowModes.Ellipsis)
            txt.overflowMode = TextOverflowModes.Ellipsis;

        // ⚠ Đã bật rồi thì THOÁT NGAY. Bật auto-size xong, TMP ghi đè `fontSize` bằng cỡ
        // thực tế đang vẽ; lấy cỡ đó làm trần cho lần gọi sau là trần tụt dần mỗi lần mở
        // popup cho tới khi chạm sàn.
        if (txt.enableAutoSizing) return;

        float tran = txt.fontSize;
        if (tran <= 0f) return;

        txt.fontSizeMax     = tran;
        txt.fontSizeMin     = tran * 0.75f;
        txt.enableAutoSizing = true;
    }

    // ═════════════════════════════ LƯU / NẠP ═════════════════════════════
    //
    // PlayerPrefs — theo đúng hệ dự án đang dùng. Ghi bằng PlayerPrefs.Set* rồi gọi
    // LuuGopPrefs.Hen() thay cho PlayerPrefs.Save():
    // Assets/_Game/Farm/Scripts/Managers/LuuGopPrefs.cs giải thích vì sao Save() trực tiếp
    // gây đứng hình ~10–100ms mỗi lần (ghi đĩa đồng bộ). Hen() gộp tối đa 1 lần ghi / 2 giây
    // và vẫn flush ở mọi đường thoát (pause / mất focus / quit / rời Play Mode).
    //
    // PlayerPrefs KHÔNG có SetLong ⇒ ticks lưu dạng string. Đừng đổi sang float:
    // DateTime.Ticks cỡ 6.4e17, float chỉ có ~7 chữ số ý nghĩa ⇒ sai số hàng NGÀY.

    private const string K_VER      = "MILL_Ver";
    private const string K_UNLOCKED = "MILL_SlotsUnlocked";
    private const int    SAVE_VER   = 1;

    // F8: may xay van co K_VER rieng nhung KHONG nam trong SaveVersionGuard.AllFamilies,
    // nen cac tool reset khong xoa duoc dau cua no va he version chung khong thay no.
    private const string SaveFamilyMill = "MILL";

    private void KhoiTaoTrangThai()
    {
        int n = (config != null) ? Mathf.Max(1, config.slotCount) : 5;

        _slotStates = new SlotState[n];
        for (int i = 0; i < n; i++) _slotStates[i] = new SlotState();

        // −1 = "chưa vẽ lần nào" ⇒ frame đầu tiên chắc chắn vẽ thật cho mọi slot.
        _modeDaVe = new int[n];
        for (int i = 0; i < n; i++) _modeDaVe[i] = -1;

        _soSlotDaMo = (config != null) ? config.slotsUnlockedAtStart : 3;

        NapTrangThai();
        _daKhoiTao = true;

        if (config != null && slots != null && slots.Length != config.slotCount)
        {
            Debug.LogWarning(LOG + "Số ô slot wire trong Inspector (" + slots.Length + ") khác " +
                             "MillConfig.slotCount (" + config.slotCount + "). Slot vượt quá sẽ " +
                             "chạy logic nhưng không có UI để hiện.", this);
        }
    }

    private void NapTrangThai()
    {
        // F8: dong dau vao he version chung (ngoai K_VER rieng o duoi).
        SaveVersionGuard.Ensure(SaveFamilyMill, SAVE_VER, null,
                                PlayerPrefs.HasKey(K_VER) || PlayerPrefs.HasKey(K_UNLOCKED));

        if (!PlayerPrefs.HasKey(K_VER))
            return;   // chưa từng lưu ⇒ giữ giá trị mặc định từ config

        int ver = PlayerPrefs.GetInt(K_VER, 0);
        if (ver != SAVE_VER)
        {
            Debug.LogWarning(LOG + "Bản lưu máy xay phiên bản " + ver + " ≠ " + SAVE_VER +
                             " ⇒ bỏ qua, dùng mặc định. (Nếu sau này đổi format thì viết " +
                             "chuyển đổi ở đây.)");
            return;
        }

        int daMo = PlayerPrefs.GetInt(K_UNLOCKED, _soSlotDaMo);
        int tran = (config != null) ? config.slotCount : _slotStates.Length;
        _soSlotDaMo = Mathf.Clamp(daMo, 0, tran);

        for (int i = 0; i < _slotStates.Length; i++)
        {
            string idRecipe = PlayerPrefs.GetString(KeyRecipe(i), string.Empty);
            if (string.IsNullOrEmpty(idRecipe)) continue;

            MillRecipeData r = TimCongThuc(idRecipe);
            if (r == null)
            {
                // Công thức bị xoá khỏi config sau khi người chơi đã lưu — bỏ slot đó,
                // KHÔNG treo slot vĩnh viễn ở trạng thái không vẽ được.
                Debug.LogWarning(LOG + "Slot #" + (i + 1) + " lưu recipeId '" + idRecipe +
                                 "' nhưng MillConfig không còn công thức này ⇒ trả slot về trống.");
                XoaSlotDaLuu(i);
                continue;
            }

            long ticks;
            if (!long.TryParse(PlayerPrefs.GetString(KeyEnd(i), "0"), out ticks) || ticks <= 0L)
            {
                XoaSlotDaLuu(i);
                continue;
            }

            _slotStates[i].recipe      = r;
            _slotStates[i].endTicksUtc = ticks;
            _slotStates[i].totalSec    = PlayerPrefs.GetFloat(KeyTotal(i), r.BrewSeconds);

            // BÙ THỜI GIAN OFFLINE: không cần code gì thêm. endTicksUtc là mốc tuyệt đối,
            // Update so với DateTime.UtcNow ⇒ slot ủ 2 phút mà người chơi tắt game 10 phút
            // sẽ hiện SẴN CHỜ THU ngay khi mở popup.
        }
    }

    private void LuuTrangThai()
    {
        // ⚠ GUARD 21/08 — sửa NullReferenceException lúc thoát Play.
        // OnApplicationQuit/OnApplicationPause gọi hàm này VÔ ĐIỀU KIỆN. Nếu popup chưa
        // từng khởi tạo (scene không có config, hoặc Awake chưa kịp chạy KhoiTaoTrangThai —
        // ví dụ mở prefab/scene thiếu manager rồi thoát ngay) thì _slotStates còn null ⇒
        // vòng for bên dưới nổ NRE ngay khung hình cuối cùng của phiên chơi.
        // Chưa khởi tạo nghĩa là KHÔNG có gì mới để lưu — thoát êm là đúng.
        if (!_daKhoiTao || _slotStates == null) return;

        PlayerPrefs.SetInt(K_VER, SAVE_VER);
        PlayerPrefs.SetInt(K_UNLOCKED, _soSlotDaMo);

        for (int i = 0; i < _slotStates.Length; i++)
        {
            SlotState st = _slotStates[i];

            if (st.recipe == null)
            {
                XoaSlotDaLuu(i);
                continue;
            }

            PlayerPrefs.SetString(KeyRecipe(i), st.recipe.recipeId);
            // Ticks là long ⇒ lưu string. Xem ghi chú ở đầu khối.
            PlayerPrefs.SetString(KeyEnd(i), st.endTicksUtc.ToString());
            PlayerPrefs.SetFloat(KeyTotal(i), st.totalSec);
        }

        // Hen() thay cho PlayerPrefs.Save() — xem LuuGopPrefs.cs.
        LuuGopPrefs.Hen();
    }

    private void XoaSlotDaLuu(int i)
    {
        PlayerPrefs.DeleteKey(KeyRecipe(i));
        PlayerPrefs.DeleteKey(KeyEnd(i));
        PlayerPrefs.DeleteKey(KeyTotal(i));
    }

    private MillRecipeData TimCongThuc(string id)
    {
        if (config == null || config.recipes == null || string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < config.recipes.Length; i++)
        {
            MillRecipeData r = config.recipes[i];
            if (r != null && r.recipeId == id) return r;
        }

        return null;
    }

    // Key ghép bằng string — chỉ gọi lúc lưu/nạp (vài lần mỗi phiên), KHÔNG trong Update.
    private static string KeyRecipe(int i) => "MILL_S" + i + "_Recipe";
    private static string KeyEnd(int i)    => "MILL_S" + i + "_EndTicks";
    private static string KeyTotal(int i)  => "MILL_S" + i + "_TotalSec";

    // ═════════════════════════════ AN TOÀN KHI THOÁT ═════════════════════════════

    private void OnApplicationPause(bool tam)
    {
        // Thu app trên điện thoại có thể bị hệ điều hành kill sau đó ⇒ lưu ngay.
        if (tam) LuuTrangThai();
    }

    private void OnApplicationQuit()
    {
        LuuTrangThai();
    }
}
