using System;
using System.Collections.Generic;

/// <summary>Kết quả một lần mua. Có mã lý do để UI hiện đúng câu, không đoán mò.</summary>
public enum MarketBuyResult
{
    Success            = 0,
    ListingNotFound    = 1,
    ListingNotActive   = 2,
    NotEnoughGold      = 3,
    InventoryMissing   = 4,
    OwnListing         = 5,  // hàng của chính mình — mua lại chỉ tốn phí, chặn luôn

    // TESTER-F8 — kho hết SLOT (số LOẠI vật phẩm) và món này là loại MỚI.
    // Phải là mã riêng, không gộp vào InventoryMissing: "kho chưa sẵn sàng" và "kho đầy"
    // đòi hai hành động khác nhau của người chơi (chờ vs bán bớt/nâng cấp kho).
    InventoryFull      = 6
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  CỔNG LẤY HÀNG CHO BẢNG TIN CHỢ (A5)
/// ══════════════════════════════════════════════════════════════════════════
///
/// Giai đoạn 1: LocalMarketProvider — trộn hàng NPC sinh tại máy + hàng người chơi
///              đang bày ở Quầy Hàng (DEV-B).
/// Giai đoạn 2: ServerMarketProvider — cùng interface, UI không đổi một dòng.
///
/// Vì thế mọi hàm ở đây đều nhận/trả kiểu dữ liệu THUẦN (string, int, MarketListing),
/// không có MonoBehaviour, không có Sprite. Có Sprite là hết đường nối server.
/// </summary>
public interface IMarketProvider
{
    /// <summary>Bắn mỗi khi danh sách đổi (làm mới, mua xong, quầy hàng thay đổi).</summary>
    event Action OnListingsChanged;

    /// <summary>
    /// Danh sách hàng đang bán, đã lọc theo danh mục và đã sắp xếp.
    /// Truyền <see cref="MarketCategory.All"/> để lấy tất cả.
    /// </summary>
    IReadOnlyList<MarketListing> GetListings(MarketCategory category);

    /// <summary>Tra một listing theo id. Trả null nếu không còn.</summary>
    MarketListing GetListing(string listingId);

    /// <summary>Sinh lại toàn bộ hàng NPC. cycleSeed giữ cho mọi lần vào cùng chu kỳ ra cùng hàng.</summary>
    void RegenerateNpcListings(int slotCount, int playerLevel, int cycleSeed);

    /// <summary>Đánh dấu đã bán. Việc trừ vàng / cộng kho do MarketManager làm, không phải provider.</summary>
    bool MarkListingSold(string listingId);

    /// <summary>Số hàng đang Active (đã tính cả hàng người chơi).</summary>
    int ActiveListingCount { get; }
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  CẦU NỐI DEV-A ↔ DEV-B
/// ══════════════════════════════════════════════════════════════════════════
///
/// DEV-B (Quầy Hàng) chỉ cần gán một delegate vào đây, KHÔNG cần biết gì về
/// provider hay UI bảng tin. DEV-A cũng không cần tham chiếu tới class
/// PlayerListing của DEV-B → hai bên code song song không chặn nhau,
/// và nếu một bên chưa xong thì bên kia vẫn biên dịch được.
///
/// Cách dùng phía DEV-B (đặt trong Awake của manager quầy hàng):
/// <code>
/// MarketPlayerListingBridge.GetActiveListings = () =>
/// {
///     var result = new List&lt;MarketListing&gt;();
///     foreach (var l in myActiveListings)
///         result.Add(MarketListing.CreatePlayerListing(
///             l.listingId, l.itemId, l.quantity, l.pricePerUnit,
///             l.createdUtcTicks, l.expiresUtcTicks, l.hasLoa,
///             playerName, playerLevel));
///     return result;
/// };
/// MarketPlayerListingBridge.NotifyChanged();   // gọi mỗi khi quầy đổi
/// </code>
/// </summary>
public static class MarketPlayerListingBridge
{
    /// <summary>DEV-B gán hàm này. Để null nghĩa là chưa có quầy hàng — bảng tin chỉ có hàng NPC.</summary>
    public static Func<List<MarketListing>> GetActiveListings;

    /// <summary>DEV-B gán hàm này để bảng tin gỡ hàng khi có người mua. Trả false nếu không gỡ được.</summary>
    public static Func<string, bool> OnPlayerListingSold;

    /// <summary>Bảng tin lắng nghe sự kiện này để vẽ lại ngay khi quầy hàng đổi.</summary>
    public static event Action OnPlayerListingsChanged;

    /// <summary>DEV-B gọi sau mỗi lần đặt hàng lên quầy / huỷ / hết hạn.</summary>
    public static void NotifyChanged()
    {
        OnPlayerListingsChanged?.Invoke();
    }

    /// <summary>Gọi an toàn — không bao giờ trả null, không ném lỗi khi DEV-B chưa gán.</summary>
    public static List<MarketListing> FetchActiveListings()
    {
        if (GetActiveListings == null)
            return new List<MarketListing>();

        List<MarketListing> result = GetActiveListings.Invoke();
        return result ?? new List<MarketListing>();
    }

    /// <summary>
    /// Xoá toàn bộ delegate. BẮT BUỘC gọi khi thoát scene farm:
    /// delegate static giữ tham chiếu tới MonoBehaviour đã bị huỷ ⇒ rò bộ nhớ
    /// và ném MissingReferenceException ở lần vào scene sau.
    /// </summary>
    public static void Clear()
    {
        GetActiveListings   = null;
        OnPlayerListingSold = null;
        OnPlayerListingsChanged = null;
    }
}
