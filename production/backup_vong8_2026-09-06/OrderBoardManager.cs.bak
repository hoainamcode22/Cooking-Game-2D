using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  BẢNG ĐƠN HÀNG — 9 ô, luôn đầy, lưu qua phiên
/// ══════════════════════════════════════════════════════════════════════════
///
/// Thay hoàn toàn `VillageOrderManager` + bong bóng trên 5 nhà dân. Ba thứ hệ cũ làm sai
/// mà lớp này sinh ra để sửa:
///
///  1. **Đơn không lưu.** Hệ cũ giữ đơn thuần runtime. Người chơi gom nửa chừng cho một
///     đơn, tắt app, quay lại thì đơn biến mất — công gom coi như đổ sông. Lớp này lưu
///     xuống PlayerPrefs kèm `saveVersion`.
///  2. **Trừ kho không nguyên tử.** Hệ cũ trừ món 1 xong mới thử trừ món 2; món 2 hụt là
///     món 1 đã bay khỏi kho, chỉ để lại một dòng LogError. Lớp này kiểm đủ TOÀN BỘ trước,
///     và vẫn hoàn lại nếu bước trừ giữa chừng hỏng.
///  3. **Bảng kẹt cứng.** Hệ cũ không cho bỏ đơn, nên một đơn đòi thứ chưa mở khoá chiếm
///     chỗ nhà đó vĩnh viễn. Lớp này có nút bỏ đơn + luật "luôn ≥2 đơn giao được".
///
/// Kế thừa `OrderBoardManagerBase` — hợp đồng do DEV-B sở hữu (mục 8 file TEAM).
/// </summary>
public class OrderBoardManager : OrderBoardManagerBase
{
    // ══════════════════════════════════════════════════════════════════════
    //  CẤU HÌNH
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Luôn phải có ít nhất bằng này đơn giao được ngay với kho hiện tại — mục 5.4 file TEAM.
    /// Hai chứ không phải một: một đơn thì giao xong là bảng lại trống trơn, người chơi
    /// vừa có cảm giác "làm được" đã mất ngay.
    /// </summary>
    private const int MinDeliverableOrders = 2;

    private const string SaveKey           = "OrderBoard_Save";
    private const int    CurrentSaveVersion = 1;

    [Header("Vị trí bảng ngoài map (DEV-B gán hoặc gọi RegisterBoardAnchor)")]
    [Tooltip("Con vật hướng dẫn chỉ tay vào đây. Bỏ trống thì tay chỉ vào gốc toạ độ.")]
    [SerializeField] private Transform boardWorldAnchor;

    [Header("Gỡ lỗi")]
    [Tooltip("In ra mọi đơn được sinh. Bật khi cân bằng số, tắt khi build.")]
    [SerializeField] private bool verboseLog = false;

    // ══════════════════════════════════════════════════════════════════════
    //  TRẠNG THÁI
    // ══════════════════════════════════════════════════════════════════════

    private readonly List<OrderData>            _orders = new List<OrderData>(SlotCount);
    private readonly List<OrderBoardOrderView>  _views  = new List<OrderBoardOrderView>(SlotCount);

    /// <summary>
    /// Bộ ngẫu nhiên RIÊNG, không dùng `UnityEngine.Random`.
    ///
    /// `UnityEngine.Random` là trạng thái TOÀN CỤC: sinh 9 đơn lúc vào scene sẽ đẩy lệch
    /// mọi thứ khác đang bốc số cùng lúc (rơi vật phẩm, hoạt ảnh, bảng tin chợ). Bộ riêng
    /// thì bảng đơn không bao giờ là nguyên nhân của một lỗi ngẫu nhiên ở chỗ khác.
    /// </summary>
    private readonly System.Random _rng = new System.Random();

    private bool _inventoryDirty;
    private bool _ready;

    // CỐ Ý KHÔNG có sự kiện `OnOrderDelivered` riêng (mục 8.4 file TEAM — DEV-B từ chối).
    //
    // Lý do của DEV-B đúng và đáng ghi lại: sự kiện đó nằm trên LỚP CON này, nên giao diện
    // muốn nghe thì phải viết thẳng tên `OrderBoardManager` — phá đúng ranh giới mà
    // `OrderBoardContract.cs` sinh ra để giữ. Popup tự ghi lại chỉ số ô TRƯỚC khi gọi
    // `TryDeliverOrder` nên đã đủ dữ liệu cho ba hiệu ứng B9, không cần thêm kênh nào.
    //
    // Đổi lại, phía này phải bảo đảm một điều: khi giao/bỏ, ô bị XOÁ KHỎI DANH SÁCH và đơn
    // mới rơi xuống CUỐI lưới (mục 5.4) — nhờ vậy các phiếu phía sau tự trượt lên và hiệu
    // ứng "dồn lưới" của DEV-B có cái để chạy. Xem `TryDeliverOrder` bước 5.

    public Transform BoardWorldAnchor => boardWorldAnchor;

    /// <summary>Object bảng ngoài map tự khai vị trí của mình — gọi ở Awake của nó.</summary>
    public void RegisterBoardAnchor(Transform anchor)
    {
        if (anchor != null) boardWorldAnchor = anchor;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VÒNG ĐỜI
    // ══════════════════════════════════════════════════════════════════════

    // Không override Awake: `OrderBoardManagerBase.Awake` đã gán `Instance` và tự huỷ
    // bản sao thừa. Mọi khởi tạo của lớp này nằm ở Start (xem lý do ngay dưới).

    private void Start()
    {
        // Bản sao thừa (base.Awake đã gọi Destroy(this)) TUYỆT ĐỐI không được chạy tiếp.
        // Unity vẫn gọi Start/OnApplicationQuit trên component đã đánh dấu huỷ trong cùng
        // khung hình — nếu để nó chạy thì `SaveBoard()` của bản sao sẽ GHI ĐÈ save bằng
        // một bảng rỗng, và người chơi mất sạch 9 đơn chỉ vì scene lỡ có hai manager.
        if (Instance != this) return;

        // Khởi tạo ở Start chứ không phải Awake: cần `FarmInventoryManager` và
        // `PlayerProgressManager` đã nạp xong save của chúng, nếu không thì cấp người chơi
        // đọc ra 1 và cả bảng sinh toàn đơn bậc tập sự cho một người đang ở cấp 12.
        if (!LoadBoard())
            BuildFreshBoard();

        RefillAndBalance();
        _ready = true;

        ThuDangKyKho();

        RebuildViews();

        // Ghi ngay bảng vừa dựng. Trên điện thoại người chơi hay bị hệ điều hành giết app
        // mà không có `OnApplicationQuit` — không lưu ở đây thì 9 đơn vừa sinh sẽ khác
        // hoàn toàn ở lần mở sau, và mọi công gom dở dang thành vô nghĩa.
        SaveBoard();

        RaiseBoardChanged();
    }

    protected override void OnDestroy()
    {
        if (_daNgheKho && FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= MarkInventoryDirty;

        if (_daNgheCap && PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.OnLevelChanged -= XuLyLenCap;

        base.OnDestroy();
    }

    private bool  _daNgheKho;
    private bool  _daNgheCap;
    private int   _capDaBiet = -1;

    // Thời gian chờ giữa hai lần tự cân bằng bảng. Xem lý do ở LateUpdate.
    private const float GianCachCanBang = 20f;
    private float _lanCanBangCuoi = -999f;

    /// <summary>
    /// Nối tai nghe kho + cấp độ. Gọi lại được nhiều lần, chỉ đăng ký đúng một lần.
    ///
    /// VÌ SAO KHÔNG CHỈ THỬ MỘT LẦN Ở Start: bản cũ thử đúng một lần, `Instance == null`
    /// là thôi hẳn. Mà popup KHÔNG tự nghe kho — nó chỉ nghe `OnBoardChanged` của manager.
    /// Trượt lần đó là phiếu **không bao giờ** đổi sang xanh khi người chơi thu hoạch,
    /// và không có lỗi nào báo. Đây là điểm gãy một chiều nên phải thử lại.
    /// </summary>
    private void ThuDangKyKho()
    {
        if (!_daNgheKho && FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.OnInventoryChanged += MarkInventoryDirty;
            _daNgheKho = true;
        }

        if (!_daNgheCap && PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnLevelChanged += XuLyLenCap;
            _daNgheCap  = true;
            _capDaBiet  = PlayerProgressManager.Instance.Level;
        }
    }

    /// <summary>
    /// Lên cấp → đơn phải khó dần theo.
    ///
    /// Bản cũ chỉ đọc cấp lúc SINH đơn, mà đơn chỉ sinh khi giao/bỏ. Người chơi lên cấp 13
    /// vẫn ngồi nhìn mấy đơn "3 Lúa" của bậc Tập Sự cho tới khi dọn hết thủ công.
    /// Ở đây thay các đơn XA HOÀN THÀNH NHẤT (`FindLeastProgressedSlot`) chứ không xoá sạch —
    /// xoá hết là cướp mất công gom dở dang của người chơi.
    /// </summary>
    private void XuLyLenCap(int capMoi)
    {
        if (!_ready || capMoi <= _capDaBiet) return;
        _capDaBiet = capMoi;

        RefillAndBalance();
        RebuildViews();
        SaveBoard();
        RaiseBoardChanged();
    }

    private void MarkInventoryDirty() => _inventoryDirty = true;

    private void LateUpdate()
    {
        // Chưa nối được tai nghe thì thử lại mỗi khung hình cho tới khi manager kia sẵn sàng.
        if (!_daNgheKho || !_daNgheCap) ThuDangKyKho();

        // Kho đổi → phiếu nào đủ hàng phải chuyển sang xanh. Gom về một lần mỗi khung hình
        // thay vì bắn thẳng trong callback: thu hoạch một luống là hàng chục lần
        // `OnInventoryChanged` liên tiếp, vẽ lại lưới từng lần là giật hình thấy rõ.
        if (!_inventoryDirty) return;

        _inventoryDirty = false;
        if (!_ready) return;

        // 🔴 Cân lại bảng khi kho đổi, KHÔNG chỉ vẽ lại màu.
        //
        // Luật "luôn có ≥2 đơn giao được" trước đây chỉ chạy lúc giao/bỏ/Start. Người chơi
        // mới, kho rỗng ⇒ 9 đơn không đơn nào giao được, và bảng NẰM NGUYÊN như vậy kể cả
        // sau khi họ thu hoạch đầy kho — vì không có gì gọi lại RefillAndBalance.
        //
        // ⏱ NHƯNG PHẢI CÓ THỜI GIAN CHỜ. Mô phỏng một giờ chơi cho thấy nếu cân lại mỗi
        // lần kho đổi thì bảng tự thay **2,7–5,8 đơn mỗi phút** — người chơi ngồi nhìn lưới
        // nhấp nháy liên tục, và đơn họ đang nhắm tới biến mất trước mắt. Thu hoạch một
        // luống là hàng chục lần `OnInventoryChanged`, mỗi lần lại đủ điều kiện cân lại.
        //
        // 20 giây là đủ lâu để người chơi thu hoạch xong cả vòng trước khi bảng động đậy,
        // mà vẫn đủ nhanh để không ai kịp thấy bảng "chết".
        if (Time.unscaledTime - _lanCanBangCuoi >= GianCachCanBang
            && DemSoDonGiaoDuoc() < MinDeliverableOrders)
        {
            _lanCanBangCuoi = Time.unscaledTime;
            RefillAndBalance();
            RebuildViews();
            SaveBoard();
        }

        RaiseBoardChanged();
    }

    private int DemSoDonGiaoDuoc()
    {
        int n = 0;
        for (int i = 0; i < _orders.Count; i++)
            if (_orders[i] != null && IsDeliverable(_orders[i])) n++;
        return n;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveBoard();
    }

    private void OnApplicationQuit() => SaveBoard();

    // ══════════════════════════════════════════════════════════════════════
    //  BỐN HÀM HỢP ĐỒNG VỚI DEV-B
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>(1) 9 đơn đang treo, thứ tự = thứ tự ô trên lưới 3×3.</summary>
    public override IReadOnlyList<OrderBoardOrderView> GetOrders() => _views;

    /// <summary>
    /// (2) Số lượng một món ĐANG CÓ trong kho — vế trái của `có/cần`.
    ///
    /// Trả số THẬT, không cắt về `needAmount`: video hiện `8/1` chứ không phải `1/1`, và
    /// đó là chủ đích — người chơi thấy luôn mình đang dư bao nhiêu, không phải thoát ra
    /// mở kho đếm.
    /// </summary>
    public override int GetOwnedAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        if (FarmInventoryManager.Instance == null) return 0;

        return FarmInventoryManager.Instance.GetAmount(MarketPriceTable.Canonical(itemId));
    }

    /// <summary>
    /// (3) GIAO ĐƠN — trừ kho nguyên tử, cộng vàng/EXP, bắn hook nhiệm vụ + tutorial.
    /// </summary>
    public override bool TryDeliverOrder(string orderId, out string failReason)
    {
        failReason = null;

        int slot = IndexOf(orderId);
        if (slot < 0)
        {
            failReason = "Đơn hàng này không còn nữa.";
            return false;
        }

        OrderData order = _orders[slot];

        if (FarmInventoryManager.Instance == null)
        {
            failReason = "Chưa mở được kho, thử lại sau.";
            Debug.LogError("[BảngĐơn] FarmInventoryManager.Instance = null — không giao đơn được.");
            return false;
        }

        // ── BƯỚC 1 · Kiểm đủ TOÀN BỘ trước khi động vào kho ───────────────────
        // Đây là nửa đầu của "nguyên tử". Hệ cũ bỏ qua bước này nên trừ được món 1
        // rồi mới phát hiện món 2 thiếu — kho lệch vĩnh viễn, chỉ để lại một dòng log.
        for (int i = 0; i < order.lines.Count; i++)
        {
            OrderLine line = order.lines[i];
            if (line == null) continue;

            int owned = FarmInventoryManager.Instance.GetAmount(line.itemId);
            if (owned < line.requiredAmount)
            {
                failReason = $"Còn thiếu {MarketPriceTable.GetDisplayName(line.itemId)} " +
                             $"({owned}/{line.requiredAmount}).";
                return false;
            }
        }

        // ── BƯỚC 2 · Trừ kho, có đường lui ────────────────────────────────────
        // Nửa sau của "nguyên tử". Về lý thuyết bước 1 đã bảo đảm đủ hàng, nhưng
        // `RemoveItem` vẫn có thể trả false nếu một hệ khác vừa lấy hàng đi giữa hai bước
        // (nấu ăn tự động, quầy hàng bán xong). Đã lỡ trừ vài món thì PHẢI trả lại —
        // để mất hàng của người chơi mà không cho gì là lỗi không bao giờ được phép có.
        List<OrderLine> removed = new List<OrderLine>(order.lines.Count);
        bool ok = true;

        for (int i = 0; i < order.lines.Count; i++)
        {
            OrderLine line = order.lines[i];
            if (line == null) continue;

            if (FarmInventoryManager.Instance.RemoveItem(line.itemId, line.requiredAmount))
            {
                removed.Add(line);
                continue;
            }

            ok = false;
            Debug.LogError($"[BảngĐơn] Trừ kho hỏng giữa chừng: '{line.itemId}' x{line.requiredAmount}. Đang hoàn lại.");
            break;
        }

        if (!ok)
        {
            for (int i = 0; i < removed.Count; i++)
                FarmInventoryManager.Instance.AddItem(removed[i].itemId, removed[i].requiredAmount);

            failReason = "Kho vừa thay đổi, thử lại nhé.";
            return false;
        }

        // ── BƯỚC 3 · Trả thưởng ───────────────────────────────────────────────
        // KHÔNG nhân đôi EXP ở đây. `rewardExp` đã là con số cuối cùng và cũng chính là
        // con số đang hiện trên phiếu — hệ cũ hiện một đằng cộng một nẻo (×2 lúc giao).
        if (FarmEconomyManager.Instance != null)
            FarmEconomyManager.Instance.AddGold(order.rewardGold);
        else
            Debug.LogError("[BảngĐơn] FarmEconomyManager.Instance = null — vàng thưởng KHÔNG vào túi người chơi!");

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(order.rewardExp);
        else
            Debug.LogError("[BảngĐơn] PlayerProgressManager.Instance = null — EXP thưởng bị mất!");

        // ── BƯỚC 4 · Hook nhiệm vụ ────────────────────────────────────────────
        // GIỮ NGUYÊN NGỮ NGHĨA HAI DÒNG của `VillageOrderManager.cs:286,288`.
        //   • Dòng đầu `includeTypeWide = true`  → cộng khoá "DeliverOrder:*", tức ĐẾM SỐ ĐƠN.
        //   • Các dòng sau `includeTypeWide = false` → chỉ cộng khoá riêng của từng món.
        // Nếu mọi dòng đều để true thì một đơn 3 món sẽ tính thành 3 đơn, và 26 nhiệm vụ
        // kiểu "giao 50 đơn" xong sớm gấp ba. Thiếu hẳn lời gọi thì 26 nhiệm vụ treo vĩnh
        // viễn — trong đó có `main_l2_deliver_1` ngay ở cấp 2, người chơi kẹt tiến trình.
        if (order.lines.Count > 0 && order.lines[0] != null)
            MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, order.lines[0].itemId, 1);

        for (int i = 1; i < order.lines.Count; i++)
            if (order.lines[i] != null)
                MissionProgressTracker.ReportEvent(MissionEventType.DeliverOrder, order.lines[i].itemId, 1,
                                                   includeTypeWide: false);

        // C8 — chỗ này trước đây gọi `QuestManager.Instance?.OnOrderDelivered()`.
        // Đã xoá cùng cả hệ `QuestManager`: không có instance trong bất kỳ scene nào, không
        // có một asset `QuestData` nào, và `CheckQuestCompletion` chỉ ghi `// TODO: Give
        // rewards`. Hai lời gọi `MissionProgressTracker.ReportEvent` ngay bên trên là hệ
        // nhiệm vụ CÒN SỐNG và đã lo đủ phần báo tiến độ giao đơn.

        // Bước tutorial `6_DeliverOrder` chờ đúng lời gọi này. Hệ cũ không ai gọi nên
        // bước đó treo — người chơi mới bị chặn ngay ở phần hướng dẫn.
        TutorialManager.Instance?.NotifyDelivery();

        Debug.Log($"[BảngĐơn] ✓ Giao đơn {order} (ô {slot})");

        // ── BƯỚC 5 · Dọn ô, lấp đơn mới ───────────────────────────────────────
        // RemoveAt chứ không phải gán null — mục 5.4: "đơn mới lấp vào CUỐI lưới".
        // Bỏ ô tại chỗ thì các phiếu phía sau tự trượt lên một bậc và đơn mới xuất hiện ở
        // ô cuối, đúng hiệu ứng ③ "lưới dồn lại" của video. Nếu gán null rồi lấp lại đúng
        // chỗ cũ thì nhìn như phiếu bị "thay ruột" — người chơi mất dấu đơn mình vừa giao.
        _orders.RemoveAt(slot);

        RefillAndBalance();
        RebuildViews();
        SaveBoard();
        RaiseBoardChanged();

        return true;
    }

    /// <summary>
    /// (4) BỎ ĐƠN — van xả áp. Không có nút này thì một đơn đòi thứ chưa mở khoá sẽ chiếm
    /// ô đó vĩnh viễn (đúng bệnh của hệ cũ). Đơn mới sinh NGAY, rơi xuống cuối lưới.
    /// </summary>
    public override bool DiscardOrder(string orderId)
    {
        int slot = IndexOf(orderId);
        if (slot < 0) return false;

        if (verboseLog) Debug.Log($"[BảngĐơn] Bỏ đơn {_orders[slot]} (ô {slot})");

        // Cùng luật với giao đơn: bỏ ô tại chỗ, đơn mới rơi xuống cuối lưới.
        _orders.RemoveAt(slot);

        RefillAndBalance();
        RebuildViews();
        SaveBoard();
        RaiseBoardChanged();

        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  TIỆN ÍCH CHO DEV-B
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Ngoài map có phiếu nào xanh không (B2) — không cần mở popup mới biết.</summary>
    public bool HasAnyDeliverableOrder() => CountDeliverableOrders() > 0;

    public int CountDeliverableOrders()
    {
        int count = 0;
        for (int i = 0; i < _orders.Count; i++)
            if (IsDeliverable(_orders[i])) count++;
        return count;
    }

    public static Sprite GetItemIcon(string itemId)        => OrderBoardIconResolver.GetIcon(itemId);
    public static string GetItemDisplayName(string itemId) => MarketPriceTable.GetDisplayName(itemId);

    // ══════════════════════════════════════════════════════════════════════
    //  NỘI BỘ — DUY TRÌ BẢNG
    // ══════════════════════════════════════════════════════════════════════

    private int GetPlayerLevel() =>
        PlayerProgressManager.Instance != null ? Mathf.Max(1, PlayerProgressManager.Instance.Level) : 1;

    private int IndexOf(string orderId)
    {
        if (string.IsNullOrEmpty(orderId)) return -1;

        for (int i = 0; i < _orders.Count; i++)
            if (_orders[i] != null && _orders[i].orderId == orderId) return i;

        return -1;
    }

    private bool IsDeliverable(OrderData order)
    {
        if (order == null || order.lines.Count == 0) return false;

        for (int i = 0; i < order.lines.Count; i++)
        {
            OrderLine line = order.lines[i];
            if (line == null) continue;
            if (GetOwnedAmount(line.itemId) < line.requiredAmount) return false;
        }

        return true;
    }

    private void BuildFreshBoard()
    {
        _orders.Clear();
        for (int i = 0; i < SlotCount; i++) _orders.Add(null);
        OrderNameBank.ClearRecent();
    }

    /// <summary>
    /// Lấp đầy 9 ô rồi bảo đảm luật "≥2 đơn giao được".
    ///
    /// Hai giai đoạn tách bạch có chủ đích:
    ///   A. lấp ô trống — ô nào trống thì sinh, ưu tiên sinh đơn giao được nếu đang thiếu chỉ tiêu;
    ///   B. nếu lấp xong vẫn chưa đủ 2 đơn giao được thì THAY một vài đơn đang treo.
    ///
    /// Ở giai đoạn B chỉ thay những đơn người chơi CÒN XA NHẤT (tỉ lệ hoàn thành thấp nhất).
    /// Nếu thay bừa thì có ngày xoá mất đúng cái đơn người chơi đã gom được 9/10 món —
    /// mất công vô cớ là cách nhanh nhất để người ta bỏ hệ thống này.
    /// </summary>
    private void RefillAndBalance()
    {
        while (_orders.Count < SlotCount) _orders.Add(null);
        if (_orders.Count > SlotCount) _orders.RemoveRange(SlotCount, _orders.Count - SlotCount);

        int level = GetPlayerLevel();

        // ── A · lấp ô trống ───────────────────────────────────────────────────
        for (int i = 0; i < _orders.Count; i++)
        {
            if (_orders[i] != null) continue;

            bool needEasy = CountDeliverableOrders() < MinDeliverableOrders;
            _orders[i] = CreateOrder(level, needEasy);
        }

        // ── B · ép đủ chỉ tiêu đơn giao được ─────────────────────────────────
        // Trần 3 vòng: nếu kho nghèo tới mức không sinh nổi đơn dễ nào thì lặp thêm cũng
        // vô ích, thà để bảng như vậy còn hơn treo game trong một vòng while.
        for (int guard = 0; guard < 3 && CountDeliverableOrders() < MinDeliverableOrders; guard++)
        {
            int victim = FindLeastProgressedSlot();
            if (victim < 0) break;

            OrderData easy = OrderGenerator.GenerateDeliverable(level, GetOwnedAmount, _rng);
            if (easy == null) break;   // kho không nuôi nổi đơn nào — chịu, vòng sau tính tiếp

            _orders[victim] = easy;
            if (verboseLog) Debug.Log($"[BảngĐơn] Ép đơn dễ vào ô {victim}: {easy}");
        }
    }

    private OrderData CreateOrder(int level, bool preferDeliverable)
    {
        OrderData order = null;

        if (preferDeliverable)
            order = OrderGenerator.GenerateDeliverable(level, GetOwnedAmount, _rng);

        if (order == null)
            order = OrderGenerator.Generate(level, _rng);

        if (order == null)
        {
            // Rổ rỗng hoàn toàn — chỉ xảy ra nếu MarketPriceTable bị lọc sạch. Không được
            // trả null lên trên: hợp đồng với DEV-B là 9 phần tử KHÔNG BAO GIỜ null.
            Debug.LogError("[BảngĐơn] Không sinh nổi đơn nào — kiểm tra bộ lọc trong OrderGenerator.");
            order = new OrderData
            {
                orderId          = Guid.NewGuid().ToString("N").Substring(0, 12),
                title            = "Đơn hàng",
                customerAvatarId = "heo",
                tier             = OrderTier.TapSu,
                theme            = OrderTheme.BuaComGiaDinh,
                rewardGold       = 10,
                rewardExp        = 3,
            };
            order.lines.Add(new OrderLine { itemId = "rice", displayName = "Lúa", requiredAmount = 1 });
        }

        if (verboseLog) Debug.Log($"[BảngĐơn] Sinh đơn: {order}");
        return order;
    }

    /// <summary>Ô có đơn mà người chơi còn xa hoàn thành nhất — ứng viên bị thay.</summary>
    private int FindLeastProgressedSlot()
    {
        int   worstSlot  = -1;
        float worstRatio = float.MaxValue;

        for (int i = 0; i < _orders.Count; i++)
        {
            OrderData order = _orders[i];
            if (order == null || order.lines.Count == 0) continue;
            if (IsDeliverable(order)) continue;   // đơn giao được thì không đụng tới

            float sum = 0f;
            int   n   = 0;

            for (int j = 0; j < order.lines.Count; j++)
            {
                OrderLine line = order.lines[j];
                if (line == null || line.requiredAmount <= 0) continue;

                sum += Mathf.Clamp01(GetOwnedAmount(line.itemId) / (float)line.requiredAmount);
                n++;
            }

            float ratio = n > 0 ? sum / n : 0f;
            if (ratio < worstRatio)
            {
                worstRatio = ratio;
                worstSlot  = i;
            }
        }

        return worstSlot;
    }

    private void RebuildViews()
    {
        _views.Clear();
        for (int i = 0; i < _orders.Count; i++)
            if (_orders[i] != null) _views.Add(_orders[i].View);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LƯU / NẠP  (A9)
    // ══════════════════════════════════════════════════════════════════════

    [Serializable]
    private class SaveLine
    {
        public string itemId;
        public string displayName;
        public int    need;
    }

    [Serializable]
    private class SaveEntry
    {
        public string orderId;
        public string title;
        public string customerAvatarId;
        public int    rewardGold;
        public int    rewardExp;
        public int    tier;
        public int    theme;
        public List<SaveLine> lines = new List<SaveLine>();
    }

    [Serializable]
    private class SaveRoot
    {
        /// <summary>
        /// Phiên bản định dạng. BẮT BUỘC là trường đầu tiên và không bao giờ đổi tên:
        /// đây là thứ duy nhất đọc được từ một save của phiên bản tương lai chưa biết.
        /// </summary>
        public int saveVersion = CurrentSaveVersion;

        public List<SaveEntry> orders = new List<SaveEntry>();

        /// <summary>Hàng cấm tên đơn — xem `OrderNameBank.RestoreRecent`.</summary>
        public List<string> recentTitles = new List<string>();
    }

    private void SaveBoard()
    {
        // Chưa dựng xong bảng thì KHÔNG được ghi. Nếu không, một lần tắt game ngay lúc
        // đang load (hoặc một bản sao manager thừa) sẽ ghi đè save bằng danh sách rỗng.
        if (!_ready) return;

        SaveRoot root = new SaveRoot
        {
            saveVersion  = CurrentSaveVersion,
            recentTitles = OrderNameBank.SnapshotRecent(),
        };

        for (int i = 0; i < _orders.Count; i++)
        {
            OrderData order = _orders[i];
            if (order == null) continue;

            SaveEntry entry = new SaveEntry
            {
                orderId          = order.orderId,
                title            = order.title,
                customerAvatarId = order.customerAvatarId,
                rewardGold       = order.rewardGold,
                rewardExp        = order.rewardExp,
                tier             = (int)order.tier,
                theme            = (int)order.theme,
            };

            for (int j = 0; j < order.lines.Count; j++)
            {
                OrderLine line = order.lines[j];
                if (line == null) continue;

                entry.lines.Add(new SaveLine
                {
                    itemId      = line.itemId,
                    displayName = line.displayName,
                    need        = line.requiredAmount,
                });
            }

            root.orders.Add(entry);
        }

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(root));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    /// <summary>Trả true khi đã dựng lại được bảng từ save.</summary>
    private bool LoadBoard()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return false;

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return false;

        SaveRoot root;
        try
        {
            root = JsonUtility.FromJson<SaveRoot>(json);
        }
        catch (Exception e)
        {
            // Save hỏng (người dùng nghịch PlayerPrefs, ghi dở dang khi tắt máy đột ngột).
            // Nuốt lỗi và dựng bảng mới: mất 9 đơn còn hơn không vào được game.
            Debug.LogWarning($"[BảngĐơn] Save hỏng, dựng bảng mới. Chi tiết: {e.Message}");
            return false;
        }

        if (root == null) return false;

        // ── NHÁNH MIGRATE ─────────────────────────────────────────────────────
        if (root.saveVersion != CurrentSaveVersion)
        {
            root = Migrate(root);
            if (root == null) return false;
        }

        BuildFreshBoard();
        OrderNameBank.RestoreRecent(root.recentTitles);

        int slot = 0;
        for (int i = 0; i < root.orders.Count && slot < SlotCount; i++)
        {
            OrderData order = FromSave(root.orders[i]);
            if (order == null) continue;
            _orders[slot++] = order;
        }

        Debug.Log($"[BảngĐơn] Nạp lại {slot} đơn từ save (v{root.saveVersion}).");
        return true;
    }

    /// <summary>
    /// Chuyển save phiên bản cũ sang định dạng hiện tại.
    ///
    /// Hôm nay chưa có phiên bản cũ nào để chuyển — `VillageOrderManager` KHÔNG lưu gì cả,
    /// nên không tồn tại dữ liệu v0. Nhánh này vẫn phải có mặt từ ngày đầu: thêm nó vào
    /// sau, khi ngoài kia đã có hàng nghìn máy giữ save v1, là lúc không còn chỗ để thử.
    ///
    /// Quy ước: trả `null` = bỏ save, dựng bảng mới. Với một save chỉ chứa 9 đơn hàng sinh
    /// tự động thì vứt đi là mất mát chấp nhận được — kho, vàng, EXP đều nằm ở nơi khác.
    /// </summary>
    private SaveRoot Migrate(SaveRoot old)
    {
        if (old == null) return null;

        if (old.saveVersion > CurrentSaveVersion)
        {
            // Save của bản mới hơn (người chơi hạ cấp app). Đọc bừa là ra dữ liệu rác.
            Debug.LogWarning($"[BảngĐơn] Save v{old.saveVersion} mới hơn bản đang chạy " +
                             $"(v{CurrentSaveVersion}) — bỏ qua, dựng bảng mới.");
            return null;
        }

        Debug.Log($"[BảngĐơn] Save v{old.saveVersion} → v{CurrentSaveVersion}: không có dữ liệu cũ để chuyển.");
        return null;
    }

    private OrderData FromSave(SaveEntry entry)
    {
        if (entry == null || entry.lines == null || entry.lines.Count == 0) return null;

        OrderData order = new OrderData
        {
            orderId          = string.IsNullOrEmpty(entry.orderId)
                                 ? Guid.NewGuid().ToString("N").Substring(0, 12)
                                 : entry.orderId,
            title            = entry.title,
            customerAvatarId = entry.customerAvatarId,
            rewardGold       = entry.rewardGold,
            rewardExp        = entry.rewardExp,
            tier             = (OrderTier)Mathf.Clamp(entry.tier, (int)OrderTier.TapSu, (int)OrderTier.BacThay),
            theme            = (OrderTheme)Mathf.Clamp(entry.theme, 0, (int)OrderTheme.DonGap),
        };

        // Chặn cứng số dòng theo số ô thật của cột phải.
        //
        // Cột phải chỉ vẽ được 6 ô, nhưng `TryDeliverOrder` trừ kho theo TOÀN BỘ `lines`.
        // Save bị sửa tay hoặc save của bản cũ có 10 dòng ⇒ người chơi thấy 6 yêu cầu,
        // bấm giao lại bị trừ đủ 10 món — **yêu cầu ẩn**, không cách nào biết được.
        int soDong = Mathf.Min(entry.lines.Count, MaxRequirementSlots);
        for (int i = 0; i < soDong; i++)
        {
            SaveLine line = entry.lines[i];
            if (line == null || string.IsNullOrEmpty(line.itemId) || line.need <= 0) continue;

            // Chuẩn hoá lại khi nạp, không tin save: save có thể do bản cũ ghi ra trước khi
            // bảng bí danh `chicken → chicken_meat` tồn tại.
            order.lines.Add(new OrderLine
            {
                itemId         = MarketPriceTable.Canonical(line.itemId),
                displayName    = line.displayName,
                requiredAmount = line.need,
            });
        }

        return order.lines.Count > 0 ? order : null;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GỠ LỖI
    // ══════════════════════════════════════════════════════════════════════

    [ContextMenu("Gỡ lỗi: In bảng đơn hiện tại")]
    private void DebugPrintBoard()
    {
        for (int i = 0; i < _orders.Count; i++)
            Debug.Log($"  ô {i}: {(_orders[i] == null ? "(trống)" : _orders[i].ToString())}" +
                      $"{(IsDeliverable(_orders[i]) ? "  ✓ giao được" : string.Empty)}");
    }

    [ContextMenu("Gỡ lỗi: Xoá save bảng đơn")]
    private void DebugClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        Debug.Log("[BảngĐơn] Đã xoá save. Vào Play lại để sinh bảng mới.");
    }
}
