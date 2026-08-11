using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// POPUP BẢNG ĐƠN HÀNG — B3 · B4 · B5 · B6 · B7 · B8 · B9 · B10.
///
/// Bố cục bám đúng bản tham chiếu (`production\PHAN_TICH_BANG_DON_HANG_CU.md`):
///   • title pill + icon, nút X đỏ LỒI RA NGOÀI mép panel                     (B3)
///   • cột trái: lưới phiếu 3x3, bốn trạng thái                               (B4)
///   • phiếu CHỈ hiện phần thưởng, không hiện yêu cầu                          (B5)
///   • cột phải: avatar khách · ô thưởng · gạch nét đứt · lưới yêu cầu 3x2     (B6)
///   • ô yêu cầu hiện `có/cần`                                                 (B7)
///   • nút thùng rác đỏ + nút xanh dương "GIAO HÀNG"                           (B8)
///   • ba hiệu ứng khi giao chạy cùng lúc                                      (B9)
///
/// Lớp này CHỈ đọc trạng thái từ <see cref="OrderBoardManagerBase"/> rồi vẽ, và gọi
/// ngược lại 4 hàm của nó khi người chơi bấm. Nó KHÔNG tự trừ kho, KHÔNG tự cộng vàng,
/// KHÔNG tự sinh đơn. Giữ ranh giới này là lý do popup, phiếu ghim ngoài map và các
/// nhiệm vụ không bao giờ nói ba con số khác nhau về cùng một đơn.
///
/// Toàn bộ hierarchy do Editor tool `Tools ▸ Farm ▸ Bảng Đơn Hàng` sinh ra. File này
/// KHÔNG tạo GameObject nào lúc chạy ngoài việc Instantiate đúng prefab phiếu đã dựng
/// sẵn — bài học từ `UnifiedTaskPopupUI` 1433 dòng hardcode ~200 toạ độ.
/// </summary>
public class OrderBoardPopupUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  THAM CHIẾU
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Khung popup (B3)")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button     buttonClose;
    [SerializeField] private Button     buttonDimBackground;
    [SerializeField] private TMP_Text   textTitle;

    [Header("Cột trái — lưới phiếu 3x3 (B4)")]
    [SerializeField] private RectTransform   ticketGridContent;
    [SerializeField] private GridLayoutGroup ticketGridLayout;
    [SerializeField] private OrderTicketUI   ticketPrefab;

    [Header("Cột phải — chi tiết đơn (B6)")]
    [Tooltip("Hiện khi CHƯA chọn đơn nào.")]
    [SerializeField] private GameObject detailEmptyRoot;
    [Tooltip("Hiện khi ĐÃ chọn một đơn.")]
    [SerializeField] private GameObject detailContentRoot;
    [SerializeField] private Image      imageArtCustomerAvatar;

    [Tooltip("Ảnh 12 khách hàng. Danh sách tự điền sẵn đủ 12 mã — chỉ việc kéo ảnh vào ô " +
             "Sprite bên phải. Ô nào bỏ trống thì khách đó vẫn hiện khối màu như cũ.")]
    [SerializeField] private AnhKhachHang[] anhKhachHang;

    [SerializeField] private TMP_Text   textOrderTitle;
    [SerializeField] private TMP_Text   textRewardExp;
    [SerializeField] private TMP_Text   textRewardGold;

    [Header("Cột phải — lưới yêu cầu 3x2 (B7)")]
    [SerializeField] private OrderRequireCellUI[] requireCells;

    [Header("Nút hành động (B8)")]
    [SerializeField] private Button   buttonDiscard;
    [SerializeField] private Button   buttonDeliver;
    [SerializeField] private TMP_Text textDeliverLabel;
    [SerializeField] private Image    imageDeliverBackground;

    [Header("Màu nút GIAO HÀNG")]
    [Tooltip("Xanh dương — đủ hàng, bấm được.")]
    [SerializeField] private Color colorDeliverReady = new Color(0.23f, 0.51f, 0.85f, 1f);
    [Tooltip("Xám — chưa đủ hàng.")]
    [SerializeField] private Color colorDeliverBlocked = new Color(0.42f, 0.44f, 0.48f, 1f);

    [Header("Hiệu ứng giao hàng (B9)")]
    [SerializeField] private OrderDeliverFxUI deliverFx;
    [Tooltip("Thời gian lưới dồn lại. Phải khớp nhịp với OrderDeliverFxUI để ba hiệu ứng kết thúc cùng nhau.")]
    // Phải KHỚP với `OrderDeliverFxUI.duration`. Ba hiệu ứng khởi động cùng khung hình,
    // lệch thời lượng là lưới dồn xong từ lâu mà khói còn bay lơ lửng.
    [SerializeField] private float reflowSeconds = 0.42f;

    [Header("Thông báo")]
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text   textMessage;
    [SerializeField] private float      messageSeconds = 2.2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  TRẠNG THÁI TRONG PHIÊN
    // ─────────────────────────────────────────────────────────────────────────

    private readonly List<OrderTicketUI> _tickets = new List<OrderTicketUI>();

    private string _selectedOrderId;
    private bool   _popupInputLockHeld;

    /// <summary>
    /// Manager mà popup ĐANG nghe. Giữ đúng tham chiếu này thay vì một cờ bool: nếu chỉ
    /// nhớ "đã đăng ký rồi" thì khi scene nạp lại và manager là object MỚI, popup vẫn
    /// nghe cái cũ đã chết — lưới đứng im mà không có lỗi nào báo.
    /// </summary>
    private OrderBoardManagerBase _subscribedTo;

    /// <summary>Đang chạy hiệu ứng giao/bỏ đơn — chặn mọi lần vẽ lại đè lên animation.</summary>
    private bool _animating;

    private Coroutine _reflowRoutine;
    private Coroutine _messageRoutine;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    /// <summary>
    /// Cho <c>PopupManager.IsAnyPopupOpen()</c> hỏi mà không cần kéo tham chiếu vào
    /// Inspector — đúng lối đang dùng của <c>CropProcessPopupUI.AnyOpen</c>.
    /// Cố ý làm vậy để DEV-A và DEV-B không phải sửa cùng một dòng trong PopupManager.
    /// </summary>
    public static bool AnyOpen { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  VÒNG ĐỜI
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Một dòng "mã khách ↔ ảnh" trong Inspector.</summary>
    [System.Serializable]
    public class AnhKhachHang
    {
        [Tooltip("Mã khách — trùng với OrderNameBank.CustomerIds. Không sửa.")]
        public string maKhach;
        public Sprite anh;
    }

    /// <summary>
    /// Điền sẵn đủ 12 dòng theo <see cref="OrderNameBank.CustomerIds"/> để người gắn art
    /// mở Inspector là thấy ngay 12 ô có nhãn, không phải tự gõ mã — gõ sai một ký tự thì
    /// avatar im lặng không hiện, và không có gì báo cho biết là do gõ sai.
    /// Giữ nguyên ảnh đã gắn khi danh sách bị lệch (ai đó xoá bớt dòng).
    /// </summary>
    private void NapBangAvatar()
    {
        if (anhKhachHang == null || anhKhachHang.Length == 0)
        {
            OrderBoardIconResolver.DangKyAvatar(null);
            return;
        }

        var bang = new List<KeyValuePair<string, Sprite>>(anhKhachHang.Length);
        foreach (var d in anhKhachHang)
            if (d != null) bang.Add(new KeyValuePair<string, Sprite>(d.maKhach, d.anh));

        OrderBoardIconResolver.DangKyAvatar(bang);
    }

    private void OnValidate()
    {
        string[] ma = OrderNameBank.CustomerIds;
        if (anhKhachHang != null && anhKhachHang.Length == ma.Length)
        {
            bool khop = true;
            for (int i = 0; i < ma.Length; i++)
                if (anhKhachHang[i] == null || anhKhachHang[i].maKhach != ma[i]) { khop = false; break; }
            if (khop) return;
        }

        var cu = new Dictionary<string, Sprite>();
        if (anhKhachHang != null)
            foreach (var d in anhKhachHang)
                if (d != null && !string.IsNullOrEmpty(d.maKhach) && d.anh != null) cu[d.maKhach] = d.anh;

        var moi = new AnhKhachHang[ma.Length];
        for (int i = 0; i < ma.Length; i++)
            moi[i] = new AnhKhachHang { maKhach = ma[i], anh = cu.TryGetValue(ma[i], out Sprite s) ? s : null };

        anhKhachHang = moi;
    }

    private void Awake()
    {
        NapBangAvatar();
        WireButton(buttonClose,         ClosePopup);
        WireButton(buttonDimBackground, ClosePopup);
        WireButton(buttonDiscard,       OnClickDiscard);
        WireButton(buttonDeliver,       OnClickDeliver);

        if (textTitle != null && string.IsNullOrEmpty(textTitle.text))
            textTitle.text = "BẢNG ĐƠN HÀNG";

        // Tắt popup trong Awake chứ KHÔNG trong Start. Đây đúng chỗ `MarketPopupUI` đang
        // hỏng: Start() gọi SetActive(false) lên chính cái root vừa được bật, nên popup
        // tự đóng ngay khi mở. Awake chỉ chạy một lần lúc object sinh ra.
        if (popupRoot != null) popupRoot.SetActive(false);
        if (messageRoot != null) messageRoot.SetActive(false);

        AnyOpen = false;
    }

    private void OnEnable() => Subscribe();

    private void Start()
    {
        // Đăng ký LẠI ở Start: thứ tự Awake giữa các object không được bảo đảm, manager
        // của DEV-A hoàn toàn có thể Awake SAU popup. Không có bước này thì ở lần vào
        // scene đầu tiên popup nghe hụt sự kiện và lưới đứng im mãi.
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ReleasePopupInputBlock();
        AnyOpen = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        AnyOpen = false;
    }

    private void Subscribe()
    {
        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        if (board == null || _subscribedTo == board) return;

        Unsubscribe();
        board.OnBoardChanged += OnBoardChanged;
        _subscribedTo = board;
    }

    private void Unsubscribe()
    {
        if (_subscribedTo != null) _subscribedTo.OnBoardChanged -= OnBoardChanged;
        _subscribedTo = null;
    }

    private void OnBoardChanged()
    {
        // Đang chạy hiệu ứng thì bỏ qua: coroutine dồn lưới sẽ tự vẽ lại ở cuối.
        // Không có chốt này thì DEV-A bắn sự kiện ngay trong TryDeliverOrder và lưới
        // nhảy về vị trí mới TRƯỚC khi khói kịp bung — mất sạch hiệu ứng.
        if (_animating) return;
        if (!IsOpen) return;

        RefreshAll();
    }

    private static void WireButton(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(action);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  MỞ / ĐÓNG  (B10)
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenPopup()
    {
        if (popupRoot == null || IsOpen) return;

        popupRoot.SetActive(true);
        AnyOpen = true;

        Subscribe();               // manager có thể mới sinh ra sau lần Start đầu tiên
        AcquirePopupInputBlock();

        EnsureTickets();
        _selectedOrderId = null;
        _animating = false;

        RefreshAll();
    }

    public void ClosePopup()
    {
        if (popupRoot == null || !IsOpen) return;

        if (_reflowRoutine != null) { StopCoroutine(_reflowRoutine); _reflowRoutine = null; }
        _animating = false;

        popupRoot.SetActive(false);
        AnyOpen = false;

        ReleasePopupInputBlock();
    }

    /// <summary>Cho nút HUD hoặc bước tutorial: mở nếu đang đóng, đóng nếu đang mở.</summary>
    public void TogglePopup()
    {
        if (IsOpen) ClosePopup();
        else        OpenPopup();
    }

    /// <summary>
    /// Mẫu khoá input chép từ `HouseOrderPopupUI` cũ (dòng 353–369) — hệ cũ đã chạy ổn
    /// với đúng cặp này. Cờ <c>_popupInputLockHeld</c> là thứ quan trọng nhất: thiếu nó,
    /// gọi mở hai lần sẽ tăng bộ đếm hai lần mà đóng chỉ giảm một, và bản đồ kẹt input
    /// vĩnh viễn cho tới khi PopupManager tự chữa.
    /// </summary>
    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);

        if (_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        _popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (!_popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        _popupInputLockHeld = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LƯỚI PHIẾU 3x3  (B4)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sinh đủ 9 phiếu MỘT LẦN rồi ghi nhớ toạ độ từng ô.
    ///
    /// Sau khi ghi nhớ xong thì TẮT GridLayoutGroup. Vì sao: hiệu ứng "lưới dồn lại"
    /// (B9) cần trượt phiếu giữa hai ô, mà layout group thì mỗi khung hình lại kéo phiếu
    /// về đúng ô của nó — hiệu ứng sẽ không bao giờ nhìn thấy được. Toạ độ vẫn do layout
    /// group tính ra chứ không hardcode, nên chỉnh cellSize/spacing trong Inspector là
    /// lưới tự đổi theo.
    /// </summary>
    private void EnsureTickets()
    {
        if (_tickets.Count > 0) return;
        if (ticketGridContent == null || ticketPrefab == null) return;

        if (ticketGridLayout != null) ticketGridLayout.enabled = true;

        for (int i = 0; i < OrderBoardManagerBase.SlotCount; i++)
        {
            OrderTicketUI t = Instantiate(ticketPrefab, ticketGridContent);
            t.name = $"Ticket_{i}";
            t.Bind(this, i);
            _tickets.Add(t);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(ticketGridContent);

        for (int i = 0; i < _tickets.Count; i++)
            _tickets[i].HomePosition = _tickets[i].Rect.anchoredPosition;

        if (ticketGridLayout != null) ticketGridLayout.enabled = false;
    }

    /// <summary>Vẽ lại toàn bộ: lưới phiếu + cột chi tiết + nút.</summary>
    public void RefreshAll()
    {
        IReadOnlyList<OrderBoardOrderView> orders = GetOrders();

        // Đơn đang chọn có thể vừa bị giao/bỏ ở nơi khác → bỏ chọn cho khỏi trỏ vào hư không.
        if (!string.IsNullOrEmpty(_selectedOrderId) && FindOrderIndex(orders, _selectedOrderId) < 0)
            _selectedOrderId = null;

        for (int i = 0; i < _tickets.Count; i++)
        {
            OrderTicketUI t = _tickets[i];
            if (t == null) continue;

            t.SetGridPosition(t.HomePosition);
            t.Rect.localScale = Vector3.one;

            OrderBoardOrderView order = (orders != null && i < orders.Count) ? orders[i] : null;
            if (order == null) { t.ShowEmpty(); continue; }

            t.ShowOrder(order, order.orderId == _selectedOrderId);
        }

        RefreshDetail();
    }

    /// <summary>Người chơi bấm vào một phiếu — <see cref="OrderTicketUI"/> gọi vào đây.</summary>
    public void OnTicketClicked(int slotIndex)
    {
        if (_animating) return;

        IReadOnlyList<OrderBoardOrderView> orders = GetOrders();
        if (orders == null || slotIndex < 0 || slotIndex >= orders.Count) return;

        OrderBoardOrderView order = orders[slotIndex];
        if (order == null) return;

        _selectedOrderId = order.orderId;

        // Chỉ bật/tắt khung sáng thay vì vẽ lại cả lưới: bấm chọn là thao tác lặp nhiều
        // nhất trong popup, vẽ lại 9 phiếu mỗi lần là phí.
        for (int i = 0; i < _tickets.Count; i++)
            if (_tickets[i] != null) _tickets[i].SetSelected(i == slotIndex);

        RefreshDetail();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  CỘT PHẢI — CHI TIẾT ĐƠN  (B6 + B7)
    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshDetail()
    {
        OrderBoardOrderView order = FindSelectedOrder();

        SetActiveSafe(detailEmptyRoot,   order == null);
        SetActiveSafe(detailContentRoot, order != null);

        SetInteractable(buttonDiscard, order != null);

        if (order == null)
        {
            SetDeliverState(false);
            return;
        }

        // Nạp lại số lượng đang có NGAY TRƯỚC KHI VẼ. Kho đổi liên tục (thu hoạch,
        // nấu ăn) nên con số cũ trong view gần như luôn lỗi thời.
        order.RefreshOwnedAmounts();

        if (textOrderTitle != null) textOrderTitle.text = order.title;

        if (imageArtCustomerAvatar != null)
        {
            Sprite anh = OrderBoardIconResolver.GetAvatar(order.customerAvatarId);
            if (anh != null)
            {
                // Có art thật: đặt màu TRẮNG, nếu không ảnh sẽ bị nhuộm màu ngẫu nhiên
                // của khách và trông như hỏng.
                imageArtCustomerAvatar.sprite = anh;
                imageArtCustomerAvatar.color  = Color.white;
            }
            else
            {
                // Chưa có art khách hàng: tô màu suy ra từ mã khách để mỗi khách một sắc,
                // người chơi vẫn thấy "đơn này của người khác đơn kia".
                imageArtCustomerAvatar.color =
                    OrderBoardIconResolver.TintFromId(order.customerAvatarId, 0.40f, 0.88f);
            }
        }

        if (textRewardExp  != null) textRewardExp.text  = order.rewardExp.ToString();
        if (textRewardGold != null) textRewardGold.text = order.rewardGold.ToString();

        FillRequireGrid(order);
        SetDeliverState(order.CanDeliverNow());
    }

    private void FillRequireGrid(OrderBoardOrderView order)
    {
        if (requireCells == null) return;

        List<OrderBoardRequirementView> reqs = order.requirements;

        for (int i = 0; i < requireCells.Length; i++)
        {
            OrderRequireCellUI cell = requireCells[i];
            if (cell == null) continue;

            if (reqs != null && i < reqs.Count && reqs[i] != null) cell.Show(reqs[i]);
            else                                                   cell.ShowEmpty();
        }
    }

    /// <summary>
    /// Nút GIAO HÀNG: XANH DƯƠNG khi đủ hàng, XÁM khi chưa.
    ///
    /// Cố ý VẪN CHO BẤM lúc chưa đủ. Nút chết hẳn thì người chơi bấm mà không có gì xảy
    /// ra và tự đoán là game lỗi; cho bấm rồi hiện thông báo "còn thiếu ..." mới là câu
    /// trả lời. Màu xám đã đủ báo trước là chưa được.
    /// </summary>
    private void SetDeliverState(bool ready)
    {
        if (imageDeliverBackground != null)
            imageDeliverBackground.color = ready ? colorDeliverReady : colorDeliverBlocked;

        if (textDeliverLabel != null)
            textDeliverLabel.text = "GIAO HÀNG";

        SetInteractable(buttonDeliver, FindSelectedOrder() != null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GIAO ĐƠN  (B8 + B9)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnClickDeliver()
    {
        if (_animating) return;

        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        OrderBoardOrderView   order = FindSelectedOrder();
        if (board == null || order == null) return;

        int slot = FindSelectedSlot();
        if (slot < 0) return;

        int rewardExp  = order.rewardExp;
        int rewardGold = order.rewardGold;
        Vector2 fxPos  = _tickets[slot].HomePosition;

        // Bật cờ TRƯỚC khi gọi: DEV-A nhiều khả năng bắn OnBoardChanged ngay bên trong
        // TryDeliverOrder. Không chặn thì lưới bị vẽ lại tức thì và khói bung vào chỗ
        // đã đổi nội dung — nhìn như lỗi.
        _animating = true;

        if (!board.TryDeliverOrder(order.orderId, out string failReason))
        {
            _animating = false;
            ShowMessage(string.IsNullOrEmpty(failReason) ? "Chưa giao được đơn này." : failReason);
            RefreshAll();
            return;
        }

        _selectedOrderId = null;

        // ── BA HIỆU ỨNG BẮT ĐẦU TRONG CÙNG MỘT KHUNG HÌNH ────────────────────
        _tickets[slot].HideForDeliverFx();                       // phiếu biến mất
        if (deliverFx != null) deliverFx.Play(fxPos, rewardExp, rewardGold);  // khói + sao + vàng
        StartReflow(slot);                                       // lưới dồn lại
    }

    private void OnClickDiscard()
    {
        if (_animating) return;

        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        OrderBoardOrderView   order = FindSelectedOrder();
        if (board == null || order == null) return;

        int slot = FindSelectedSlot();
        if (slot < 0) return;

        _animating = true;

        if (!board.DiscardOrder(order.orderId))
        {
            _animating = false;
            ShowMessage("Không bỏ được đơn này.");
            RefreshAll();
            return;
        }

        _selectedOrderId = null;

        // Bỏ đơn KHÔNG có khói và không có phần thưởng bay lên — cố ý. Đây là hành động
        // vứt đi, thưởng cho nó một màn pháo hoa là gửi sai tín hiệu.
        _tickets[slot].HideForDeliverFx();
        StartReflow(slot);
    }

    private void StartReflow(int removedSlot)
    {
        if (_reflowRoutine != null) StopCoroutine(_reflowRoutine);
        _reflowRoutine = StartCoroutine(ReflowRoutine(removedSlot));
    }

    /// <summary>
    /// LƯỚI DỒN LẠI (hiệu ứng thứ 3 của B9).
    ///
    /// Cách làm: gán ngay dữ liệu MỚI cho từng phiếu, nhưng đặt phiếu ở toạ độ CŨ của
    /// đơn đó rồi trượt về ô mới. Mắt người đọc ra "đơn phía sau trượt lên lấp chỗ" —
    /// đúng thứ cần — mà không phải nhân bản thêm phiếu nào.
    /// </summary>
    private IEnumerator ReflowRoutine(int removedSlot)
    {
        IReadOnlyList<OrderBoardOrderView> orders = GetOrders();
        int count = _tickets.Count;

        var startPos = new Vector2[count];
        var endPos   = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            OrderTicketUI t = _tickets[i];
            if (t == null) continue;

            OrderBoardOrderView order = (orders != null && i < orders.Count) ? orders[i] : null;
            if (order == null) t.ShowEmpty();
            else               t.ShowOrder(order, false);

            endPos[i] = t.HomePosition;

            // Phiếu trước chỗ vừa trống thì đứng yên; phiếu sau đó xuất phát từ ô kế tiếp.
            if (i < removedSlot)      startPos[i] = t.HomePosition;
            else if (i + 1 < count)   startPos[i] = _tickets[i + 1].HomePosition;
            else                      startPos[i] = t.HomePosition;   // ô cuối: đơn mới, chỉ bung ra

            t.SetGridPosition(startPos[i]);

            // Ô cuối là đơn vừa sinh thêm — cho nó nở ra thay vì hiện đột ngột.
            t.Rect.localScale = (i == count - 1) ? Vector3.one * 0.4f : Vector3.one;
        }

        float total   = Mathf.Max(0.05f, reflowSeconds);
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / total);
            float ease = 1f - (1f - k) * (1f - k);   // ease-out, dừng lại êm

            for (int i = 0; i < count; i++)
            {
                OrderTicketUI t = _tickets[i];
                if (t == null) continue;

                t.SetGridPosition(Vector2.Lerp(startPos[i], endPos[i], ease));

                if (i == count - 1)
                    t.Rect.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, ease);
            }

            yield return null;
        }

        _reflowRoutine = null;
        _animating     = false;

        RefreshAll();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  THÔNG BÁO
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowMessage(string message)
    {
        if (messageRoot == null || textMessage == null) return;

        textMessage.text = message;
        messageRoot.SetActive(true);

        if (_messageRoutine != null) StopCoroutine(_messageRoutine);
        _messageRoutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        float t = 0f;
        while (t < messageSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (messageRoot != null) messageRoot.SetActive(false);
        _messageRoutine = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TIỆN ÍCH
    // ─────────────────────────────────────────────────────────────────────────

    private static IReadOnlyList<OrderBoardOrderView> GetOrders()
    {
        OrderBoardManagerBase board = OrderBoardManagerBase.Instance;
        return board != null ? board.GetOrders() : null;
    }

    private static int FindOrderIndex(IReadOnlyList<OrderBoardOrderView> orders, string orderId)
    {
        if (orders == null || string.IsNullOrEmpty(orderId)) return -1;

        for (int i = 0; i < orders.Count; i++)
            if (orders[i] != null && orders[i].orderId == orderId) return i;

        return -1;
    }

    private OrderBoardOrderView FindSelectedOrder()
    {
        IReadOnlyList<OrderBoardOrderView> orders = GetOrders();
        int index = FindOrderIndex(orders, _selectedOrderId);
        return index >= 0 ? orders[index] : null;
    }

    private int FindSelectedSlot()
    {
        int index = FindOrderIndex(GetOrders(), _selectedOrderId);
        return (index >= 0 && index < _tickets.Count) ? index : -1;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    private static void SetInteractable(Button b, bool value)
    {
        if (b != null) b.interactable = value;
    }
}
