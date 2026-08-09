using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Bốn trạng thái của một ô quầy (B3). Thứ tự khớp thứ tự đọc từ trái sang trong video.</summary>
public enum StallSlotState
{
    /// <summary>Đã mở khoá, chưa có hàng → dấu `+` và chữ "Bán vật phẩm".</summary>
    Empty = 0,

    /// <summary>Đang có hàng bán → icon + số lượng + giá.</summary>
    Selling = 1,

    /// <summary>Chưa mở nhưng ĐỦ ĐIỀU KIỆN mở ngay → ổ khoá + "Thêm ô" + giá VÀNG.</summary>
    Unlockable = 2,

    /// <summary>Chưa tới lượt (ô trước chưa mở, hoặc chưa đủ cấp) → ô trơn.</summary>
    Locked = 3,
}

/// <summary>Một dòng trong lưới chọn vật phẩm: id + số lượng đang có + kho nguồn.</summary>
public struct StallSellableItem
{
    public string           itemId;
    public int              amount;
    public StallSourceStore store;
}

/// <summary>
/// BỘ NÃO CỦA QUẦY HÀNG — sở hữu toàn bộ trạng thái, KHÔNG đụng gì tới UI.
///
/// Tách hẳn khỏi UI là có chủ đích: dự án đã có bài học `UnifiedTaskPopupUI` 1433 dòng
/// vừa giữ dữ liệu vừa dựng giao diện bằng code, kết quả là không ai sửa nổi. Ở đây
/// popup chỉ đọc và gọi hàm; mọi quyết định "được/không được" nằm trong file này, nên
/// bảng tin chợ của DEV-A, mặt quầy ngoài map và popup đều thấy CÙNG một sự thật.
///
/// ⚠ Nguyên tắc số một (B8): KHÔNG ĐƯỢC MẤT HÀNG. Mọi đường ra khỏi quầy — huỷ tay,
/// hết hạn, NPC mua — đều phải kết thúc bằng "hàng về kho" hoặc "vàng về ví". Nếu không
/// làm được ngay (manager kho/ví chưa tồn tại) thì HOÃN lại chứ tuyệt đối không bỏ qua.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStallManager : MonoBehaviour
{
    public static PlayerStallManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  CẤU HÌNH
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ô quầy")]
    [Tooltip("Tổng số ô hiện trên lưới (kể cả ô còn khoá). Lưới dựng 5 cột × 2 hàng.")]
    [SerializeField] private int slotCountMax = 10;

    [Tooltip("Số ô dùng được ngay lần chơi đầu.")]
    [SerializeField] private int slotCountAtStart = 3;

    [Tooltip("Giá VÀNG để mở từng ô, xếp theo CHỈ SỐ Ô. Ô mở sẵn để 0.")]
    [SerializeField]
    private int[] slotUnlockGoldCosts = { 0, 0, 0, 500, 1200, 2500, 5000, 9000, 15000, 24000 };

    [Tooltip("Cấp tối thiểu để ô hiện thành 'mở được'. Chưa đủ cấp thì ô nằm im (chưa tới lượt).")]
    [SerializeField]
    private int[] slotUnlockLevels = { 0, 0, 0, 3, 5, 8, 12, 16, 21, 27 };

    [Header("Rao bán")]
    [Tooltip("Hàng nằm trên quầy được bao lâu trước khi tự hoàn về kho. Mặc định 4 giờ.")]
    [SerializeField] private int listingDurationSeconds = 4 * 3600;

    [Tooltip("Giá VÀNG cho một lần bật loa quảng cáo. Trừ lúc bấm 'Đặt lên quầy'.")]
    [SerializeField] private int loaGoldCost = 25;

    // Ba hệ số dưới đây CHỈ dùng khi `MarketPriceTable` của DEV-A không biết vật phẩm.
    // Giá trị mặc định cố tình đặt trùng hằng số của DEV-A
    // (SuggestedSellMultiplier 1.3 · PlayerPriceMinFactor 0.5 · PlayerPriceMaxFactor 2.0)
    // để đường dự phòng và đường chính không cho ra hai khoảng giá khác nhau.
    [Tooltip("DỰ PHÒNG · Giá gợi ý = giá gốc × hệ số này.")]
    [SerializeField] private float suggestedPriceMultiplier = 1.3f;

    [Tooltip("DỰ PHÒNG · Giá thấp nhất cho phép = giá gợi ý × hệ số này.")]
    [SerializeField, Range(0.1f, 1f)] private float minPriceFactor = 0.5f;

    [Tooltip("DỰ PHÒNG · Giá cao nhất cho phép = giá gợi ý × hệ số này.")]
    [SerializeField, Range(1f, 6f)] private float maxPriceFactor = 2f;

    [Header("NPC tự mua (B9)")]
    [Tooltip("Tắt đi thì hàng nằm trên quầy tới khi hết hạn — dùng khi đã có multiplayer thật.")]
    [SerializeField] private bool npcAutoBuyEnabled = true;

    [SerializeField] private int npcBuyMinSeconds = 120;
    [SerializeField] private int npcBuyMaxSeconds = 900;

    [Tooltip("Bán đắt thì lâu có người mua. Số càng lớn, giá càng ảnh hưởng mạnh tới tốc độ bán.")]
    [SerializeField, Range(0.5f, 4f)] private float npcPriceSensitivity = 1.8f;

    [Tooltip("Bật loa thì thời gian chờ nhân với số này — dưới 1 nghĩa là bán nhanh hơn.")]
    [SerializeField, Range(0.1f, 1f)] private float loaSpeedMultiplier = 0.45f;

    [Header("Gỡ lỗi")]
    [SerializeField] private bool verboseLog = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  SỰ KIỆN
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Bắn khi bất cứ thứ gì trên quầy đổi. DEV-A nghe để làm mới bảng tin chợ.</summary>
    public event Action OnStallChanged;

    /// <summary>(listing, tổng vàng nhận được). Dùng cho thông báo "đã bán được hàng".</summary>
    public event Action<PlayerListing, int> OnListingSold;

    /// <summary>(listing) — hàng hết hạn và đã hoàn về kho.</summary>
    public event Action<PlayerListing> OnListingExpired;

    // ─────────────────────────────────────────────────────────────────────────
    //  TRẠNG THÁI
    // ─────────────────────────────────────────────────────────────────────────

    private const string SaveKey = "FARM_PLAYER_STALL";

    /// <summary>Tăng khi đổi cấu trúc save, rồi viết bước chuyển đổi trong Load().</summary>
    public const int CurrentSaveVersion = 1;

    /// <summary>Giữ lại tối đa bấy nhiêu listing đã kết thúc, để hiện lịch sử và để thử hoàn hàng lại.</summary>
    private const int MaxFinishedKept = 30;

    private readonly List<PlayerListing> _listings = new List<PlayerListing>();
    private readonly List<PlayerListing> _activeCache = new List<PlayerListing>();
    private readonly List<PlayerListing> _loaCache = new List<PlayerListing>();

    private int  _unlockedSlots;
    private bool _canGhi;

    // Save đọc không được (hỏng, hoặc của bản game MỚI HƠN) → CẤM ghi đè. Cùng lý do
    // với WarehouseManager: thà mất một phiên còn hơn xoá sạch quầy hàng của người chơi
    // khi họ hạ cấp bản game hoặc khôi phục từ cloud.
    private bool _khongDuocGhi;

    private float _nextTickUnscaled;
    private bool  _activeCacheDirty = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  VÒNG ĐỜI
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
        RegisterMarketBridge();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  B10 · CẦU NỐI SANG BẢNG TIN CHỢ CỦA DEV-A
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cắm quầy hàng vào `MarketPlayerListingBridge` để hàng người chơi hiện ở bảng tin.
    ///
    /// Đăng ký ở Awake chứ không phải OnEnable/Start: bảng tin chợ có thể được mở trước
    /// khi quầy hàng kịp Start, và lúc đó `FetchActiveListings()` trả danh sách rỗng —
    /// người chơi mở chợ ngay khi vào game sẽ không thấy hàng của chính mình.
    ///
    /// Đổi dữ liệu qua delegate + kiểu `MarketListing` của DEV-A, KHÔNG cho hai bên
    /// tham chiếu class của nhau. Nhờ vậy quầy hàng và bảng tin biên dịch độc lập.
    /// </summary>
    private void RegisterMarketBridge()
    {
        MarketPlayerListingBridge.GetActiveListings   = BuildMarketListings;
        MarketPlayerListingBridge.OnPlayerListingSold = HandleSoldFromMarketBoard;
    }

    /// <summary>Đổi `PlayerListing` (kiểu của DEV-B) sang `MarketListing` (kiểu của DEV-A).</summary>
    private List<MarketListing> BuildMarketListings()
    {
        var result = new List<MarketListing>();

        IReadOnlyList<PlayerListing> active = GetActiveListings();
        string playerName  = GetPlayerName();
        int    playerLevel = GetPlayerLevel();

        for (int i = 0; i < active.Count; i++)
        {
            PlayerListing l = active[i];
            if (l == null) continue;

            result.Add(MarketListing.CreatePlayerListing(
                l.listingId, l.itemId, l.quantity, l.pricePerUnit,
                l.createdUtcTicks, l.expiresUtcTicks, l.hasLoa,
                playerName, playerLevel));
        }

        return result;
    }

    /// <summary>Bảng tin chợ báo có người mua hàng của mình → chốt giao dịch bên này.</summary>
    private bool HandleSoldFromMarketBoard(string listingId)
        => TryBuyListing(listingId, out _);

    private void Start()
    {
        // Bắt kịp quãng thời gian OFFLINE ngay khi vào game: mọi mốc đều là UTC tuyệt đối
        // nên chỉ cần so với hiện tại là biết hàng nào đã bán, hàng nào đã hết hạn.
        // Đặt ở Start (không phải Awake) để chắc chắn FarmEconomyManager / các kho đã Awake xong,
        // nếu không thì tiền bán hàng và hàng hoàn kho sẽ bị hoãn vô ích.
        TickStall(force: true);
    }

    private void Update()
    {
        // Quầy hàng đo bằng phút, không cần chạy mỗi frame. unscaledTime để lúc game
        // tạm dừng (Time.timeScale = 0 khi mở popup khác) đồng hồ vẫn đúng.
        if (Time.unscaledTime < _nextTickUnscaled) return;
        _nextTickUnscaled = Time.unscaledTime + 1f;
        TickStall(force: false);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Flush();
        else TickStall(force: true);   // quay lại app → bắt kịp thời gian đã trôi
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) Flush();
        else TickStall(force: true);
    }

    private void OnApplicationQuit() => Flush();
    private void OnDisable()         => Flush();

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;

        // Gỡ hai delegate CỦA MÌNH khỏi cầu nối. Delegate static giữ tham chiếu tới
        // MonoBehaviour đã bị huỷ ⇒ rò bộ nhớ và ném MissingReferenceException ở lần
        // vào scene sau.
        //
        // Cố tình KHÔNG gọi `MarketPlayerListingBridge.Clear()`: hàm đó xoá luôn event
        // `OnPlayerListingsChanged` mà UI của DEV-A đang nghe. Nếu quầy hàng bị huỷ
        // trước bảng tin (hai object khác nhau, thứ tự huỷ không bảo đảm) thì bảng tin
        // sẽ mất kết nối im lặng. Ai đăng ký thì người đó gỡ.
        MarketPlayerListingBridge.GetActiveListings   = null;
        MarketPlayerListingBridge.OnPlayerListingSold = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Ô QUẦY
    // ─────────────────────────────────────────────────────────────────────────

    public int TotalSlotCount    => Mathf.Max(1, slotCountMax);
    public int UnlockedSlotCount => Mathf.Clamp(_unlockedSlots, 0, TotalSlotCount);
    public int LoaGoldCost       => Mathf.Max(0, loaGoldCost);
    public int ListingDurationSeconds => Mathf.Max(60, listingDurationSeconds);

    public StallSlotState GetSlotState(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= TotalSlotCount) return StallSlotState.Locked;

        if (slotIndex < UnlockedSlotCount)
            return GetListingAtSlot(slotIndex) != null ? StallSlotState.Selling : StallSlotState.Empty;

        // Chỉ ĐÚNG MỘT ô kế tiếp được mời mở. Cho mở nhảy cóc thì cảm giác tiến trình
        // biến mất — người chơi không còn "ô kế tiếp đang chờ" để hướng tới.
        if (slotIndex != UnlockedSlotCount) return StallSlotState.Locked;

        return GetPlayerLevel() >= GetSlotRequiredLevel(slotIndex)
            ? StallSlotState.Unlockable
            : StallSlotState.Locked;
    }

    public int GetSlotUnlockGoldCost(int slotIndex) => ReadArray(slotUnlockGoldCosts, slotIndex, 0);
    public int GetSlotRequiredLevel(int slotIndex)  => ReadArray(slotUnlockLevels, slotIndex, 0);

    /// <summary>
    /// Đọc mảng cấu hình an toàn. Mảng ngắn hơn slotCountMax là chuyện thường xảy ra khi
    /// ai đó tăng số ô trong Inspector mà quên thêm dòng giá — lấy phần tử cuối thay vì
    /// ném IndexOutOfRange để quầy hàng không chết cả popup vì một ô cấu hình thiếu.
    /// </summary>
    private static int ReadArray(int[] arr, int index, int fallback)
    {
        if (arr == null || arr.Length == 0) return fallback;
        if (index < 0) return fallback;
        return index < arr.Length ? arr[index] : arr[arr.Length - 1];
    }

    public bool TryUnlockSlot(int slotIndex, out string error)
    {
        error = null;

        if (GetSlotState(slotIndex) != StallSlotState.Unlockable)
        {
            error = "Ô này chưa tới lượt mở.";
            return false;
        }

        int cost = GetSlotUnlockGoldCost(slotIndex);

        if (FarmEconomyManager.Instance == null)
        {
            // KHÔNG mở chùa khi thiếu manager — đây đúng là LỖI 2 của chợ (mua chùa) mà
            // mục 1 file TEAM bắt sửa; đừng lặp lại nó ở quầy hàng.
            error = "Chưa sẵn sàng, thử lại sau.";
            return false;
        }

        if (FarmEconomyManager.Instance.Gold < cost)
        {
            error = $"Không đủ vàng (cần {cost}).";
            return false;
        }

        if (!FarmEconomyManager.Instance.SpendGold(cost))
        {
            error = "Trừ vàng thất bại.";
            return false;
        }

        _unlockedSlots = Mathf.Clamp(slotIndex + 1, 0, TotalSlotCount);
        Save();
        RaiseChanged();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TRUY VẤN LISTING
    // ─────────────────────────────────────────────────────────────────────────

    public PlayerListing GetListingAtSlot(int slotIndex)
    {
        for (int i = 0; i < _listings.Count; i++)
        {
            PlayerListing l = _listings[i];
            if (l != null && l.IsActive && l.slotIndex == slotIndex) return l;
        }
        return null;
    }

    public PlayerListing GetListingById(string listingId)
    {
        if (string.IsNullOrEmpty(listingId)) return null;
        for (int i = 0; i < _listings.Count; i++)
        {
            if (_listings[i] != null && _listings[i].listingId == listingId) return _listings[i];
        }
        return null;
    }

    /// <summary>
    /// [GIAO DIỆN CHUNG VỚI DEV-A] Hàng đang bán của người chơi, để gộp vào bảng tin chợ (A5/B10).
    /// Không bao giờ null. Danh sách trả về là bộ đệm dùng lại — bên gọi CHỈ ĐỌC, đừng giữ lâu.
    /// </summary>
    public IReadOnlyList<PlayerListing> GetActiveListings()
    {
        RebuildActiveCacheIfNeeded();
        return _activeCache;
    }

    /// <summary>Hàng đang bán CÓ BẬT LOA — DEV-A nên đẩy lên đầu bảng tin (đó là thứ người chơi trả vàng để mua).</summary>
    public IReadOnlyList<PlayerListing> GetActiveListingsWithLoa()
    {
        RebuildActiveCacheIfNeeded();
        return _loaCache;
    }

    /// <summary>Toàn bộ listing kể cả đã bán/hết hạn — dùng cho màn lịch sử, KHÔNG dùng cho bảng tin.</summary>
    public List<PlayerListing> GetAllListings() => new List<PlayerListing>(_listings);

    private void RebuildActiveCacheIfNeeded()
    {
        if (!_activeCacheDirty) return;

        _activeCache.Clear();
        _loaCache.Clear();

        for (int i = 0; i < _listings.Count; i++)
        {
            PlayerListing l = _listings[i];
            if (l == null || !l.IsActive) continue;
            _activeCache.Add(l);
            if (l.hasLoa) _loaCache.Add(l);
        }

        _activeCacheDirty = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  GIÁ
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Giá gợi ý cho MỘT đơn vị. Luôn ≥ 1.
    ///
    /// Hỏi `MarketPriceTable` của DEV-A trước. Nếu quầy hàng tự tính theo công thức riêng
    /// thì cùng một món sẽ hiện một giá ở quầy và một giá khác ở bảng tin chợ — người chơi
    /// nhìn thấy ngay và mất lòng tin vào cả hai màn hình. Ba hàm min/gợi ý/max dưới đây
    /// phải cùng lấy từ MỘT nguồn, không được trộn nửa nọ nửa kia.
    /// </summary>
    public int GetSuggestedPricePerUnit(string itemId)
    {
        int fromDevA = MarketPriceTable.GetSuggestedUnitPrice(itemId);
        if (fromDevA > 0) return fromDevA;

        int basePrice = BasePriceBook.GetBasePrice(itemId);
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * Mathf.Max(0.1f, suggestedPriceMultiplier)));
    }

    public int GetMinPricePerUnit(string itemId)
    {
        int fromDevA = MarketPriceTable.GetMinPlayerUnitPrice(itemId);
        if (fromDevA > 0) return fromDevA;

        return Mathf.Max(1, Mathf.RoundToInt(GetSuggestedPricePerUnit(itemId) * minPriceFactor));
    }

    public int GetMaxPricePerUnit(string itemId)
    {
        int min = GetMinPricePerUnit(itemId);

        int fromDevA = MarketPriceTable.GetMaxPlayerUnitPrice(itemId);
        if (fromDevA > 0) return Mathf.Max(min + 1, fromDevA);

        return Mathf.Max(min + 1, Mathf.RoundToInt(GetSuggestedPricePerUnit(itemId) * maxPriceFactor));
    }

    /// <summary>
    /// Mỗi lần bấm `+`/`−` giá nhảy bao nhiêu. Tỉ lệ theo giá gợi ý chứ không cố định 1 vàng:
    /// món 160 vàng mà mỗi nhịp 1 vàng thì người chơi phải bấm 80 lần mới tới trần.
    /// </summary>
    public int GetPriceStepPerUnit(string itemId)
        => Mathf.Max(1, Mathf.RoundToInt(GetSuggestedPricePerUnit(itemId) * 0.1f));

    // ─────────────────────────────────────────────────────────────────────────
    //  KHO — CẦU NỐI HAI KHO
    // ─────────────────────────────────────────────────────────────────────────

    public StallSourceStore GetSourceStore(string itemId)
    {
        // `MarketPriceTable.IsSeed` là chỗ DUY NHẤT phân loại hạt giống đúng cho cả
        // `ca_rot` và `khoai_tay` — hai hạt KHÔNG có tiền tố `seed_`. Chính chỗ này là
        // LỖI 3 của chợ cũ (hạt mua về rơi vào kho nông sản nên trồng không được);
        // quầy hàng mà đoán sai thì hàng hoàn về sẽ lạc kho y hệt.
        if (MarketPriceTable.Has(itemId))
        {
            return MarketPriceTable.IsSeed(itemId)
                ? StallSourceStore.SeedWarehouse
                : StallSourceStore.FarmInventory;
        }

        StallItemCatalog catalog = StallItemCatalog.Instance;
        if (catalog != null && catalog.Contains(itemId))
            return catalog.GetSourceStore(itemId);

        // Không có sổ tra → suy từ kho nào đang thực sự giữ món này. Chỉ đúng khi trong
        // kho còn hàng, nên đây là đường CUỐI CÙNG, không phải đường chính.
        if (WarehouseManager.Instance != null && WarehouseManager.Instance.GetAmount(itemId) > 0)
            return StallSourceStore.SeedWarehouse;

        return StallSourceStore.FarmInventory;
    }

    public int GetAvailableAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;

        return GetSourceStore(itemId) == StallSourceStore.SeedWarehouse
            ? (WarehouseManager.Instance != null ? WarehouseManager.Instance.GetAmount(itemId) : 0)
            : (FarmInventoryManager.Instance != null ? FarmInventoryManager.Instance.GetAmount(itemId) : 0);
    }

    /// <summary>
    /// Mọi thứ người chơi đang có và bán được, gộp cả hai kho. B6 dựa vào đây: chỉ
    /// những dòng số lượng &gt; 0 mới lọt ra, nên vật phẩm bán hết tự biến khỏi lưới chọn.
    /// </summary>
    public List<StallSellableItem> GetSellableItems()
    {
        var result = new List<StallSellableItem>();

        if (FarmInventoryManager.Instance != null)
        {
            List<KeyValuePair<string, int>> inv = FarmInventoryManager.Instance.GetOrderedItems();
            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i].Value <= 0) continue;
                result.Add(new StallSellableItem
                {
                    itemId = inv[i].Key,
                    amount = inv[i].Value,
                    store  = StallSourceStore.FarmInventory,
                });
            }
        }

        if (WarehouseManager.Instance != null)
        {
            IReadOnlyList<WarehouseItemEntry> kho = WarehouseManager.Instance.Items;
            for (int i = 0; i < kho.Count; i++)
            {
                WarehouseItemEntry e = kho[i];
                if (e == null || e.amount <= 0 || string.IsNullOrEmpty(e.itemId)) continue;
                result.Add(new StallSellableItem
                {
                    itemId = e.itemId,
                    amount = e.amount,
                    store  = StallSourceStore.SeedWarehouse,
                });
            }
        }

        return result;
    }

    private bool TryTakeFromStore(string itemId, int amount, StallSourceStore store)
    {
        if (store == StallSourceStore.SeedWarehouse)
            return WarehouseManager.Instance != null && WarehouseManager.Instance.RemoveItem(itemId, amount);

        return FarmInventoryManager.Instance != null && FarmInventoryManager.Instance.RemoveItem(itemId, amount);
    }

    /// <summary>
    /// Trả hàng về ĐÚNG kho đã lấy ra. Trả false khi kho đích chưa tồn tại — bên gọi
    /// PHẢI hoãn lại (đặt refundPending) chứ không được coi như đã xong.
    /// </summary>
    private bool TryGiveBackToStore(string itemId, int amount, StallSourceStore store)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return true;   // không có gì để trả

        if (store == StallSourceStore.SeedWarehouse)
        {
            if (WarehouseManager.Instance == null) return false;

            // Kho hạt cần tên + icon để hiện trong bảng kho; tra lại từ sổ vì bản thân
            // listing chỉ giữ id (Sprite không lưu xuống save được).
            StallItemCatalog catalog = StallItemCatalog.Instance;
            string ten  = catalog != null ? catalog.GetDisplayName(itemId) : itemId;
            Sprite icon = catalog != null ? catalog.GetIcon(itemId) : null;

            WarehouseManager.Instance.AddItem(itemId, ten, icon, amount);
            return true;
        }

        if (FarmInventoryManager.Instance == null) return false;
        FarmInventoryManager.Instance.AddItem(itemId, amount);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ĐĂNG BÁN / HUỶ / MUA
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt hàng lên quầy. Trừ kho NGAY (B8) — trừ trước rồi mới tạo listing, để nếu
    /// trừ thất bại thì không có listing ma nào được sinh ra.
    /// </summary>
    public bool TryPostListing(int slotIndex, string itemId, int quantity, int pricePerUnit,
                               bool hasLoa, out string error)
    {
        error = null;

        if (GetSlotState(slotIndex) != StallSlotState.Empty)
        {
            error = "Ô này không dùng được.";
            return false;
        }

        itemId = string.IsNullOrEmpty(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(itemId))
        {
            error = "Chưa chọn vật phẩm.";
            return false;
        }

        if (quantity <= 0)
        {
            error = "Số lượng phải lớn hơn 0.";
            return false;
        }

        StallSourceStore store = GetSourceStore(itemId);

        int available = GetAvailableAmount(itemId);
        if (available < quantity)
        {
            error = $"Trong kho chỉ còn {available}.";
            return false;
        }

        pricePerUnit = Mathf.Clamp(pricePerUnit, GetMinPricePerUnit(itemId), GetMaxPricePerUnit(itemId));

        // Trừ tiền loa TRƯỚC khi trừ kho: nếu làm ngược lại mà ví thiếu vàng thì hàng đã
        // rời kho rồi, phải viết thêm đường hoàn — càng nhiều đường hoàn càng dễ mất hàng.
        if (hasLoa && loaGoldCost > 0)
        {
            if (FarmEconomyManager.Instance == null)
            {
                error = "Chưa sẵn sàng, thử lại sau.";
                return false;
            }

            if (!FarmEconomyManager.Instance.SpendGold(loaGoldCost))
            {
                error = $"Không đủ vàng để bật loa (cần {loaGoldCost}).";
                return false;
            }
        }

        if (!TryTakeFromStore(itemId, quantity, store))
        {
            // Kho từ chối sau khi đã trừ tiền loa → trả tiền loa lại ngay, không giữ của người chơi.
            if (hasLoa && loaGoldCost > 0 && FarmEconomyManager.Instance != null)
                FarmEconomyManager.Instance.AddGold(loaGoldCost);

            error = "Không lấy được hàng từ kho.";
            return false;
        }

        DateTime nowUtc = DateTime.UtcNow;

        var listing = new PlayerListing
        {
            listingId       = PlayerListing.NewId(),
            sellerId        = "local",
            sellerName      = GetPlayerName(),
            sellerAvatar    = GetPlayerAvatarIndex(),
            itemId          = itemId,
            quantity        = quantity,
            pricePerUnit    = pricePerUnit,
            createdUtcTicks = nowUtc.Ticks,
            expiresUtcTicks = nowUtc.AddSeconds(ListingDurationSeconds).Ticks,
            hasLoa          = hasLoa,
            slotIndex       = slotIndex,
            refundPending   = false,
        };

        listing.Status      = ListingStatus.Active;
        listing.SourceStore = store;
        listing.npcBuyAtUtcTicks = npcAutoBuyEnabled
            ? nowUtc.AddSeconds(RollNpcWaitSeconds(itemId, pricePerUnit, hasLoa)).Ticks
            : 0L;

        _listings.Add(listing);
        MarkListingsDirty();
        Save();
        RaiseChanged();

        if (verboseLog)
            Debug.Log($"[QuầyHàng] Đăng bán {listing} · kho={store} · loa={hasLoa}");

        return true;
    }

    /// <summary>Huỷ rao bán → hoàn hàng về đúng kho (B8). Tiền loa KHÔNG hoàn (đã tiêu).</summary>
    public bool TryCancelListing(string listingId, out string error)
    {
        error = null;

        PlayerListing l = GetListingById(listingId);
        if (l == null || !l.IsActive)
        {
            error = "Mặt hàng này không còn được bán.";
            return false;
        }

        l.Status = ListingStatus.Cancelled;

        if (!TryGiveBackToStore(l.itemId, l.quantity, l.SourceStore))
        {
            // Không hoàn được ngay → ĐÁNH DẤU HOÃN. Ô quầy vẫn trống ra cho người chơi
            // dùng tiếp, còn hàng thì phiên sau trả. Bỏ qua ở đây là mất hàng vĩnh viễn.
            l.refundPending = true;
            Debug.LogWarning($"[QuầyHàng] Chưa hoàn được {l.itemId} x{l.quantity} về kho — sẽ thử lại.");
        }

        MarkListingsDirty();
        TrimFinished();
        Save();
        RaiseChanged();
        return true;
    }

    /// <summary>
    /// [GIAO DIỆN CHUNG VỚI DEV-A] Bảng tin chợ gọi khi có người mua mặt hàng này.
    /// Cũng là đường NPC dùng ở B9.
    /// </summary>
    public bool TryBuyListing(string listingId, out string error)
    {
        error = null;

        PlayerListing l = GetListingById(listingId);
        if (l == null || !l.IsActive)
        {
            error = "Mặt hàng này không còn được bán.";
            return false;
        }

        if (!SellListing(l))
        {
            error = "Chưa sẵn sàng, thử lại sau.";
            return false;
        }

        TrimFinished();
        Save();
        RaiseChanged();
        return true;
    }

    /// <summary>
    /// Chốt một giao dịch: hàng đi, vàng về. Trả false khi CHƯA cộng được vàng —
    /// lúc đó listing phải GIỮ NGUYÊN trạng thái Active để lần sau bán lại, vì hàng
    /// đã rời kho từ lúc đăng bán rồi, đánh dấu Sold mà không trả tiền là ăn cướp.
    /// </summary>
    private bool SellListing(PlayerListing l)
    {
        if (l == null || !l.IsActive) return false;

        int total = l.TotalPrice;

        if (total > 0)
        {
            if (FarmEconomyManager.Instance == null) return false;
            FarmEconomyManager.Instance.AddGold(total);
        }

        l.Status = ListingStatus.Sold;
        l.refundPending = false;   // đã bán thì không còn gì phải hoàn

        MarkListingsDirty();

        if (verboseLog)
            Debug.Log($"[QuầyHàng] Bán được {l} → +{total} vàng");

        OnListingSold?.Invoke(l, total);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  NHỊP CẬP NHẬT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Một nhịp: NPC mua → hết hạn hoàn kho → thử lại các khoản hoàn còn treo.
    ///
    /// Thứ tự này quan trọng. Nếu xét hết hạn trước, mặt hàng mà NPC đã "mua" ở phút
    /// thứ 10 nhưng người chơi mở game lại ở giờ thứ 5 sẽ bị tính là hết hạn — người
    /// chơi mất khoản tiền lẽ ra đã kiếm được chỉ vì họ tắt app.
    /// </summary>
    private void TickStall(bool force)
    {
        DateTime nowUtc = DateTime.UtcNow;
        bool changed = false;

        for (int i = 0; i < _listings.Count; i++)
        {
            PlayerListing l = _listings[i];
            if (l == null) continue;

            if (l.IsActive)
            {
                bool npcMua = npcAutoBuyEnabled
                              && l.IsNpcReadyToBuyAt(nowUtc)
                              && l.NpcBuyAtUtc <= l.ExpiresUtc;   // NPC không mua được hàng đã quá hạn

                if (npcMua)
                {
                    if (SellListing(l)) changed = true;
                    continue;
                }

                if (l.IsExpiredAt(nowUtc))
                {
                    l.Status = ListingStatus.Expired;

                    if (!TryGiveBackToStore(l.itemId, l.quantity, l.SourceStore))
                        l.refundPending = true;

                    changed = true;
                    OnListingExpired?.Invoke(l);
                }

                continue;
            }

            // Khoản hoàn còn treo từ phiên trước / từ lúc kho chưa sẵn sàng.
            if (l.refundPending && TryGiveBackToStore(l.itemId, l.quantity, l.SourceStore))
            {
                l.refundPending = false;
                changed = true;
                Debug.Log($"[QuầyHàng] Đã hoàn bù {l.itemId} x{l.quantity} về kho.");
            }
        }

        if (changed)
        {
            MarkListingsDirty();
            TrimFinished();
            Save();
            RaiseChanged();
        }
        else if (force)
        {
            // Không có gì đổi nhưng vẫn báo một tiếng: đây là nhịp chạy lúc vào game /
            // quay lại app, UI cần được vẽ lại dù dữ liệu y nguyên (đồng hồ đếm ngược).
            RaiseChanged();
        }
    }

    /// <summary>
    /// Quay số thời gian chờ NPC. Bán rẻ thì nhanh có người mua, bán đắt thì phải chờ —
    /// đó là toàn bộ lý do bộ chỉnh giá tồn tại; nếu giá không ảnh hưởng gì thì người chơi
    /// luôn kéo giá kịch trần và bộ chỉnh trở thành trang trí.
    /// </summary>
    private float RollNpcWaitSeconds(string itemId, int pricePerUnit, bool hasLoa)
    {
        int suggested = GetSuggestedPricePerUnit(itemId);
        float ratio = suggested > 0 ? pricePerUnit / (float)suggested : 1f;
        ratio = Mathf.Clamp(ratio, 0.4f, 4f);

        float minWait = Mathf.Max(10, Mathf.Min(npcBuyMinSeconds, npcBuyMaxSeconds));
        float maxWait = Mathf.Max(minWait + 1f, Mathf.Max(npcBuyMinSeconds, npcBuyMaxSeconds));

        float wait = UnityEngine.Random.Range(minWait, maxWait)
                   * Mathf.Pow(ratio, npcPriceSensitivity);

        if (hasLoa) wait *= loaSpeedMultiplier;

        // Trần = 3× thời hạn: giá kịch trần thì gần như chắc chắn hết hạn phải hoàn kho,
        // nhưng vẫn chừa một cửa may mắn để người chơi thỉnh thoảng trúng quả đậm.
        return Mathf.Clamp(wait, 10f, ListingDurationSeconds * 3f);
    }

    private void MarkListingsDirty() => _activeCacheDirty = true;

    private void RaiseChanged()
    {
        MarkListingsDirty();
        OnStallChanged?.Invoke();

        // Báo sang bảng tin chợ để nó vẽ lại. Không có dòng này thì người chơi đăng bán
        // xong mở chợ vẫn thấy bảng cũ, tưởng hàng chưa lên.
        MarketPlayerListingBridge.NotifyChanged();
    }

    /// <summary>Giữ save gọn: chỉ giữ lại một ít listing đã kết thúc. KHÔNG bỏ dòng còn nợ hoàn hàng.</summary>
    private void TrimFinished()
    {
        int finished = 0;
        for (int i = _listings.Count - 1; i >= 0; i--)
        {
            PlayerListing l = _listings[i];
            if (l == null) { _listings.RemoveAt(i); continue; }
            if (l.IsActive) continue;
            if (l.refundPending) continue;   // còn nợ hàng thì không được xoá

            finished++;
            if (finished > MaxFinishedKept) _listings.RemoveAt(i);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HỒ SƠ NGƯỜI CHƠI
    // ─────────────────────────────────────────────────────────────────────────

    private static int GetPlayerLevel()
    {
        if (PlayerProgressManager.Instance != null) return PlayerProgressManager.Instance.Level;
        if (FarmLevelManager.Instance != null)      return FarmLevelManager.Instance.CurrentLevel;
        return 1;
    }

    // Đọc thẳng PlayerPrefs thay vì gọi AvatarProfilePopupUI: popup hồ sơ chỉ tồn tại khi
    // được mở, còn quầy hàng cần tên người bán kể cả khi popup đó chưa từng xuất hiện.
    private static string GetPlayerName()
    {
        string ten = PlayerPrefs.GetString("PLAYER_PROFILE_NAME", "");
        return string.IsNullOrWhiteSpace(ten) ? "Người chơi" : ten;
    }

    private static int GetPlayerAvatarIndex() => PlayerPrefs.GetInt("PLAYER_PROFILE_AVATAR_INDEX", 0);

    // ─────────────────────────────────────────────────────────────────────────
    //  LƯU / ĐỌC
    // ─────────────────────────────────────────────────────────────────────────

    [Serializable]
    private class StallSaveData
    {
        public int                 saveVersion;
        public int                 unlockedSlots;
        public List<PlayerListing> listings = new List<PlayerListing>();
    }

    private void Load()
    {
        _unlockedSlots = Mathf.Clamp(slotCountAtStart, 0, TotalSlotCount);

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;   // lần chơi đầu

        StallSaveData data;
        try
        {
            data = JsonUtility.FromJson<StallSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuầyHàng] Save hỏng, bỏ qua để không mất phiên chơi: {e.Message}");
            _khongDuocGhi = true;
            return;
        }

        if (data == null) return;

        if (data.saveVersion > CurrentSaveVersion)
        {
            Debug.LogWarning($"[QuầyHàng] Save version {data.saveVersion} > {CurrentSaveVersion} " +
                             "(bản game cũ hơn save) → không đọc và KHÔNG ghi đè.");
            _khongDuocGhi = true;
            return;
        }

        // ── Đường chuyển đổi ─────────────────────────────────────────────────
        // v0 (save đời trước khi có trường saveVersion) → v1: không có gì phải đổi,
        // các trường mới đều nhận giá trị mặc định hợp lệ. Khi lên v2 thì thêm nhánh
        // `if (data.saveVersion < 2) { ... }` ngay tại đây, đừng sửa lại code đọc bên dưới.

        _unlockedSlots = Mathf.Clamp(
            Mathf.Max(data.unlockedSlots, slotCountAtStart), 0, TotalSlotCount);

        _listings.Clear();
        if (data.listings != null)
        {
            for (int i = 0; i < data.listings.Count; i++)
            {
                PlayerListing l = data.listings[i];
                if (l == null || string.IsNullOrEmpty(l.itemId) || l.quantity <= 0) continue;

                if (string.IsNullOrEmpty(l.listingId)) l.listingId = PlayerListing.NewId();

                // Save cũ/hỏng có thể thiếu mốc hết hạn ⇒ ExpiresUtc = năm 0001 ⇒ hàng
                // "hết hạn" ngay lập tức. Vá bằng cách gia hạn từ bây giờ, thà bán muộn
                // còn hơn quẳng hàng của người chơi ra khỏi quầy ngay khi vào game.
                if (l.expiresUtcTicks <= 0)
                    l.expiresUtcTicks = DateTime.UtcNow.AddSeconds(ListingDurationSeconds).Ticks;

                if (l.createdUtcTicks <= 0)
                    l.createdUtcTicks = DateTime.UtcNow.Ticks;

                _listings.Add(l);
            }
        }

        MarkListingsDirty();

        if (verboseLog)
            Debug.Log($"[QuầyHàng] Đọc save v{data.saveVersion}: {_unlockedSlots} ô mở, {_listings.Count} listing.");
    }

    private void Save()
    {
        if (_khongDuocGhi) return;

        var data = new StallSaveData
        {
            saveVersion   = CurrentSaveVersion,
            unlockedSlots = _unlockedSlots,
        };

        for (int i = 0; i < _listings.Count; i++)
        {
            if (_listings[i] != null) data.listings.Add(_listings[i]);
        }

        // Chỉ SetString (ghi bộ nhớ, rất nhẹ). PlayerPrefs.Save() dồn về Flush() ở các lối ra.
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        _canGhi = true;
    }

    private void Flush()
    {
        if (!_canGhi) return;
        PlayerPrefs.Save();
        _canGhi = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Xoá save quầy hàng")]
    private void DebugXoaSave()
    {
        _listings.Clear();
        _unlockedSlots = Mathf.Clamp(slotCountAtStart, 0, TotalSlotCount);
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        _canGhi = false;
        RaiseChanged();
        Debug.Log("[QuầyHàng] Đã xoá save quầy hàng.");
    }
#endif
}
