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
    /// `PopupManager.cs` (tôi không sửa file có sẵn).
    /// ➜ Lead chỉ cần thêm một dòng vào cuối `PopupManager.IsAnyPopupOpen()`:
    ///       || MillPopupUI.AnyOpen
    /// </summary>
    public static bool AnyOpen { get; private set; }

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

    /// <summary>Popup có đang mở.</summary>
    public bool IsOpen
    {
        get
        {
            GameObject root = popupRoot != null ? popupRoot : gameObject;
            return root.activeSelf;
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

        KhoiTaoTrangThai();
        GanSuKienNut();

        // Đóng sẵn. Đặt ở Awake để không lộ popup một frame lúc vào scene.
        GameObject r = popupRoot != null ? popupRoot : gameObject;
        if (r.activeSelf && r != gameObject)
            r.SetActive(false);

        AnToast(true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // BẮT BUỘC hạ cờ: nếu scene bị unload lúc popup đang mở mà cờ còn true thì
        // PopupManager sẽ chặn click xuống world MÃI MÃI ở scene sau.
        AnyOpen = false;
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

        CapNhatBadgeVaTongKet(soDangXay, soChoThu);
        CapNhatSoDuGem();
        CapNhatNutLon(soTrong);
        // KHÔNG truyền soDangXay: bánh răng/băng tải chạy LIÊN TỤC, xem chú thích trong
        // DatChayAnimation().
        DatChayAnimation();
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
        AnyOpen = true;

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

        // Chạy animation NGAY trong frame mở, không đợi Update: nếu đợi thì frame đầu
        // người chơi thấy máy đứng im.
        DatChayAnimation();

        AnToast(true);
    }

    /// <summary>Đóng popup, dừng toàn bộ animation và lưu trạng thái ngay.</summary>
    public void Close()
    {
        DungAnimation();
        LuuTrangThai();

        AnyOpen = false;

        GameObject root = popupRoot != null ? popupRoot : gameObject;
        if (root.activeSelf) root.SetActive(false);
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
            DatAnh(imgOutputIcon, r.icon);

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

    // ═════════════════════════════ NÚT LỚN ═════════════════════════════

    // Mã trạng thái nút, dùng cho hàng rào chống dựng chuỗi.
    private const int NUT_XAY_NGAY   = 0;
    private const int NUT_THIEU_NL   = 1;
    private const int NUT_HET_SLOT   = 2;
    private const int NUT_CHUA_CHON  = 3;

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
        bool   bamDuoc;

        switch (trangThai)
        {
            case NUT_HET_SLOT:  chu = "HẾT SLOT TRỐNG";     bamDuoc = false; break;
            case NUT_THIEU_NL:  chu = "THIẾU NGUYÊN LIỆU";  bamDuoc = false; break;
            case NUT_CHUA_CHON: chu = "XAY NGAY";           bamDuoc = false; break;
            default:            chu = "XAY NGAY";           bamDuoc = true;  break;
        }

        if (txtMainButton != null)   txtMainButton.text     = chu;
        if (btnMain != null)         btnMain.interactable   = bamDuoc;
        if (imgMainButtonBg != null) imgMainButtonBg.color  = bamDuoc ? mauNutBamDuoc : mauNutKhoa;
    }

    private void BamXayNgay()
    {
        if (_congThucChon == null)
        {
            HienToast("Hãy chọn một công thức");
            return;
        }

        int slotTrong = TimSlotTrongDauTien();
        if (slotTrong < 0)
        {
            HienToast("Hết slot trống");
            return;
        }

        if (!MillInventoryBridge.TruNguyenLieu(_congThucChon))
        {
            HienToast("Thiếu nguyên liệu");
            return;
        }

        float tong = _congThucChon.BrewSeconds;

        SlotState st   = _slotStates[slotTrong];
        st.recipe      = _congThucChon;
        st.totalSec    = tong;
        st.endTicksUtc = DateTime.UtcNow.Ticks + (long)(tong * TimeSpan.TicksPerSecond);

        LuuTrangThai();

        // slotTrong là chỉ số 0-based, nhãn cho người chơi là 1-based (#1..#5).
        HienToast("Đã cho " + _congThucChon.displayName + " vào slot " + (slotTrong + 1));
        _trangThaiNutDaHien = int.MinValue;
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

        // KHÔNG gọi slots[idx].BindEmpty() ở đây: Update là nơi DUY NHẤT vẽ slot, và nó
        // dùng hàng rào _modeDaVe. Vẽ ở hai chỗ thì hai chỗ sẽ lệch nhau lúc nào không biết.
        HienToast("Đã thu " + r.displayName + " vào kho!");
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

        HienToast("Đã xay xong " + st.recipe.displayName);
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
            HienToast("Cần đạt cấp " + config.levelRequiredLastSlot);
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

        // Vẽ lại khi ĐỔI MODE, hoặc khi ví người chơi đổi (nút mua có thể vừa bấm được /
        // vừa hết bấm được). `_gemDaHien` là số dư của FRAME TRƯỚC — CapNhatSoDuGem() chạy
        // SAU vòng lặp slot nên so ở đây là so với giá trị cũ, đúng ý.
        bool doiVi = (_gemDaHien != MillInventoryBridge.SoKimCuong());

        if (CanVeLai(idx, MillSlotMode.UnlockGem) || doiVi)
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
                    ? ("Đang xay · " + soDangXay + " slot")
                    : "Máy đang rảnh";
            }

            if (imgStatusDot != null)
                imgStatusDot.color = (soDangXay > 0) ? mauDotDangXay : mauDotRanh;
        }

        if (doiTong && txtSlotSummary != null && config != null)
        {
            // "3/5 slot đã mở · 0 đang xay · 2 chờ thu"
            txtSlotSummary.text = _soSlotDaMo + "/" + config.slotCount + " slot đã mở · "
                                + soDangXay + " đang xay · "
                                + soChoThu  + " chờ thu";
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
    /// Bật animation của khối máy: bánh răng lớn/nhỏ, băng tải, các bó cỏ trên băng tải.
    ///
    /// ⚠ SỬA NGÀY 20/08 — TRƯỚC ĐÂY GẮN VỚI `soDangXay > 0`, ĐÓ LÀ SAI SO VỚI THIẾT KẾ.
    /// Bản HTML gốc (Assets/Assetsgame/popup/ui_mill_assets/full_mill_ui.html) khai báo
    ///     &lt;animateTransform ... repeatCount="indefinite"&gt;   (2 bánh răng, 4s và 2.5s)
    ///     @keyframes beltScroll / moveItem ... infinite          (băng tải + 2 bó cỏ)
    /// nghĩa là máy QUAY MÃI, không phụ thuộc có mẻ hàng hay không — nó là hoạt cảnh trang
    /// trí của popup. Bản v1 chỉ chạy khi đang xay, nên lần đầu mở popup (5 slot trống,
    /// badge "Máy đang rảnh") mọi thứ đứng cứng ⇒ người chơi đọc là popup bị lỗi/ảnh tĩnh.
    ///
    /// Vẫn giữ hàng rào `IsRunning != true` để không gọi SetRunning mỗi frame (hàm này chạy
    /// trong Update).
    /// </summary>
    private void DatChayAnimation()
    {
        if (gearLarge != null && !gearLarge.IsRunning) gearLarge.SetRunning(true);
        if (gearSmall != null && !gearSmall.IsRunning) gearSmall.SetRunning(true);
        if (belt != null && !belt.IsRunning)           belt.SetRunning(true);

        if (beltItems == null) return;

        for (int i = 0; i < beltItems.Length; i++)
        {
            ConveyorItem it = beltItems[i];
            if (it == null) continue;
            if (!it.IsRunning) it.SetRunning(true);
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
            btnMain.onClick.RemoveAllListeners();
            btnMain.onClick.AddListener(BamXayNgay);
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
        }
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
