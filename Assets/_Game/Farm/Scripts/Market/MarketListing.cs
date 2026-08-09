using System;
using UnityEngine;

/// <summary>Trạng thái một mặt hàng rao bán. Đặt theo hình dạng server ngay từ đầu.</summary>
public enum MarketListingStatus
{
    Active    = 0,
    Sold      = 1,
    Expired   = 2,
    Cancelled = 3
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  MỘT MẶT HÀNG RAO BÁN — kiểu dữ liệu dùng chung DEV-A ↔ DEV-B
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO viết theo hình dạng SERVER dù bây giờ chưa có multiplayer:
/// giai đoạn 2 chỉ việc thay LocalMarketProvider bằng ServerMarketProvider,
/// UI và luồng mua bán KHÔNG phải sửa dòng nào. Nếu bây giờ nhét thẳng
/// MarketItemDef vào UI thì sau này phải viết lại toàn bộ thẻ hàng.
///
/// Thời gian lưu bằng TICKS UTC (long) chứ không phải float:
/// float đếm từ lúc mở game nên đóng game là mất; UtcTicks sống qua mọi phiên
/// và không lệch khi người chơi đổi múi giờ.
/// </summary>
[Serializable]
public class MarketListing
{
    public string              ListingId;
    public string              SellerId;
    public string              SellerName;
    public int                 SellerAvatarIndex;
    public int                 SellerLevel;

    public string              ItemId;
    public int                 Quantity;
    public int                 PricePerUnit;

    public long                CreatedUtcTicks;
    public long                ExpiresUtcTicks;
    public MarketListingStatus Status = MarketListingStatus.Active;

    /// <summary>Loa quảng cáo (B7). Bảng tin đẩy hàng có loa lên đầu danh sách.</summary>
    public bool                HasLoa;

    /// <summary>true = hàng của chính người chơi đăng ở Quầy Hàng, không phải NPC.</summary>
    public bool                IsPlayerListing;

    public int TotalPrice => Mathf.Max(0, PricePerUnit) * Mathf.Max(0, Quantity);

    public bool IsExpired(DateTimeOffset nowUtc)
    {
        return ExpiresUtcTicks > 0 && nowUtc.UtcTicks >= ExpiresUtcTicks;
    }

    public MarketCategory Category => MarketPriceTable.GetCategory(ItemId);

    /// <summary>
    /// Chênh lệch % so với giá NPC bán. Âm = hàng hời.
    /// Dùng để gắn nhãn "GIẢM x%" — thứ khiến người chơi quay lại bảng tin liên tục.
    /// </summary>
    public int DiscountPercentVsNpc()
    {
        int npcPrice = MarketPriceTable.GetMarketBuyPrice(ItemId);
        if (npcPrice <= 0) return 0;
        return Mathf.RoundToInt((PricePerUnit - npcPrice) * 100f / npcPrice);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HÀM DỰNG — DEV-B gọi cái này để đẩy hàng của người chơi lên bảng tin
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dựng listing cho hàng NGƯỜI CHƠI đang bán ở Quầy Hàng.
    /// DEV-B giữ kiểu PlayerListing riêng của mình, chỉ cần đổ sang đây khi
    /// MarketPlayerListingBridge được gọi — hai bên không phải phụ thuộc class của nhau.
    /// </summary>
    public static MarketListing CreatePlayerListing(
        string listingId, string itemId, int quantity, int pricePerUnit,
        long createdUtcTicks, long expiresUtcTicks, bool hasLoa,
        string playerName, int playerLevel)
    {
        MarketSeller me = MarketSellerDirectory.GetLocalPlayerSeller(playerName, playerLevel);

        return new MarketListing
        {
            ListingId         = string.IsNullOrEmpty(listingId) ? Guid.NewGuid().ToString("N") : listingId,
            SellerId          = me.SellerId,
            SellerName        = me.DisplayName,
            SellerAvatarIndex = me.AvatarIndex,
            SellerLevel       = me.Level,
            ItemId            = itemId,
            Quantity          = Mathf.Max(1, quantity),
            PricePerUnit      = Mathf.Max(1, pricePerUnit),
            CreatedUtcTicks   = createdUtcTicks,
            ExpiresUtcTicks   = expiresUtcTicks,
            Status            = MarketListingStatus.Active,
            HasLoa            = hasLoa,
            IsPlayerListing   = true
        };
    }

    /// <summary>Dựng listing cho hàng NPC. Chỉ LocalMarketProvider gọi.</summary>
    public static MarketListing CreateNpcListing(
        string listingId, MarketSeller seller, string itemId, int quantity, int pricePerUnit,
        DateTimeOffset nowUtc, TimeSpan lifetime)
    {
        return new MarketListing
        {
            ListingId         = listingId,
            SellerId          = seller.SellerId,
            SellerName        = seller.DisplayName,
            SellerAvatarIndex = seller.AvatarIndex,
            SellerLevel       = seller.Level,
            ItemId            = itemId,
            Quantity          = Mathf.Max(1, quantity),
            PricePerUnit      = Mathf.Max(1, pricePerUnit),
            CreatedUtcTicks   = nowUtc.UtcTicks,
            ExpiresUtcTicks   = nowUtc.Add(lifetime).UtcTicks,
            Status            = MarketListingStatus.Active,
            HasLoa            = false,
            IsPlayerListing   = false
        };
    }
}
