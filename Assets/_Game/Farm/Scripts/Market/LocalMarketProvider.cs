using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  NGUỒN HÀNG GIAI ĐOẠN 1 — TRỘN HÀNG NPC + HÀNG NGƯỜI CHƠI (A5)
/// ══════════════════════════════════════════════════════════════════════════
///
/// Chủ dự án yêu cầu: chợ LUÔN có hàng, kể cả khi chưa có multiplayer.
/// Vì vậy provider này tự sinh hàng NPC, rồi gộp thêm hàng người chơi đang bày ở
/// Quầy Hàng (lấy qua MarketPlayerListingBridge — không phụ thuộc code DEV-B).
///
/// Bốc hàng bằng RNG CÓ HẠT (System.Random với cycleSeed) chứ không phải
/// UnityEngine.Random: thoát game rồi vào lại trong cùng một chu kỳ làm mới phải
/// thấy ĐÚNG những món cũ. Dùng Random toàn cục là mỗi lần vào lại ra một bảng khác,
/// người chơi tưởng bị mất hàng vừa nhắm.
/// </summary>
public class LocalMarketProvider : IMarketProvider
{
    public event Action OnListingsChanged;

    /// <summary>Hàng NPC của chu kỳ hiện tại.</summary>
    private readonly List<MarketListing> npcListings = new List<MarketListing>();

    /// <summary>Bộ đệm kết quả sau khi trộn + lọc. Tái dùng để không rải rác cho GC mỗi lần lọc tab.</summary>
    private readonly List<MarketListing> filterBuffer = new List<MarketListing>();

    /// <summary>Rổ hàng hợp lệ, lấy từ MarketDatabase.asset.</summary>
    private readonly List<MarketItemDef> pool = new List<MarketItemDef>();

    private readonly MarketDatabase_SO database;

    /// <summary>Hàng NPC sống bao lâu. Dài hơn chu kỳ làm mới nên thực tế bị thay bởi refresh trước khi hết hạn.</summary>
    private static readonly TimeSpan NpcListingLifetime = TimeSpan.FromHours(6);

    public LocalMarketProvider(MarketDatabase_SO marketDatabase)
    {
        database = marketDatabase;
        RebuildPool();
    }

    public int ActiveListingCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < npcListings.Count; i++)
                if (npcListings[i].Status == MarketListingStatus.Active) count++;

            List<MarketListing> playerListings = MarketPlayerListingBridge.FetchActiveListings();
            for (int i = 0; i < playerListings.Count; i++)
                if (playerListings[i] != null && playerListings[i].Status == MarketListingStatus.Active) count++;

            return count;
        }
    }

    /// <summary>Đọc lại rổ hàng từ asset. Gọi khi asset được sinh lại trong Editor.</summary>
    public void RebuildPool()
    {
        pool.Clear();
        if (database == null)
            return;

        IReadOnlyList<MarketItemDef> items = database.Items;
        for (int i = 0; i < items.Count; i++)
        {
            MarketItemDef def = items[i];
            if (def == null || string.IsNullOrWhiteSpace(def.ItemID))
                continue;

            // Weight <= 0 là cách tắt một dòng mà không phải xoá khỏi asset
            if (def.BuyPrice <= 0 || def.MaxQuantity <= 0 || def.Weight <= 0)
                continue;

            // Còn sót TODO_ nghĩa là ai đó gõ tay vào asset — bỏ, đừng để lên màn hình
            if (def.ItemID.StartsWith("TODO_", StringComparison.OrdinalIgnoreCase))
                continue;

            pool.Add(def);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SINH HÀNG NPC
    // ══════════════════════════════════════════════════════════════════════

    public void RegenerateNpcListings(int slotCount, int playerLevel, int cycleSeed)
    {
        npcListings.Clear();

        if (pool.Count == 0)
        {
            OnListingsChanged?.Invoke();
            return;
        }

        slotCount   = Mathf.Clamp(slotCount, 1, 60);
        playerLevel = Mathf.Max(1, playerLevel);

        // Nới trần cấp lên +2: bảng tin có vài món "trên tầm" mới đáng để ngó,
        // toàn hàng đã mở khoá thì chẳng có gì để mong.
        int levelCeiling = playerLevel + 2;

        List<MarketItemDef> eligible = new List<MarketItemDef>();
        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].UnlockLevel > levelCeiling)
                continue;

            eligible.Add(pool[i]);
            totalWeight += Mathf.Max(1, pool[i].Weight);
        }

        // Cấp 1 mà rổ rỗng thì thà hạ trần còn hơn hiện bảng trắng
        if (eligible.Count == 0)
        {
            eligible.AddRange(pool);
            totalWeight = 0;
            for (int i = 0; i < eligible.Count; i++)
                totalWeight += Mathf.Max(1, eligible[i].Weight);
        }

        System.Random rng = new System.Random(cycleSeed);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Không cho một món xuất hiện quá 2 lần trong cùng bảng — bản cũ có 6 dòng
        // nước mắm giống hệt nhau, nhìn như lỗi dữ liệu chứ không phải chợ
        Dictionary<string, int> appearCount = new Dictionary<string, int>();
        HashSet<int> usedSellerIndices = new HashSet<int>();

        int guard = slotCount * 12;   // chặn vòng lặp vô hạn khi rổ quá nhỏ
        while (npcListings.Count < slotCount && guard-- > 0)
        {
            MarketItemDef def = PickWeighted(eligible, totalWeight, rng);
            if (def == null)
                break;

            appearCount.TryGetValue(def.ItemID, out int seen);
            if (seen >= 2)
                continue;
            appearCount[def.ItemID] = seen + 1;

            // Người bán: rải đều, tránh một người ôm nửa bảng tin
            int sellerIndex = rng.Next(0, Mathf.Max(1, MarketSellerDirectory.Count));
            int sellerGuard = 8;
            while (usedSellerIndices.Contains(sellerIndex) && sellerGuard-- > 0)
                sellerIndex = rng.Next(0, Mathf.Max(1, MarketSellerDirectory.Count));
            usedSellerIndices.Add(sellerIndex);

            MarketSeller seller = MarketSellerDirectory.GetByIndex(sellerIndex);

            int minQ = Mathf.Max(1, def.MinQuantity);
            int maxQ = Mathf.Max(minQ, def.MaxQuantity);
            int quantity = rng.Next(minQ, maxQ + 1);

            // Dao động ±25% quanh giá niêm yết → có hàng hời để săn, có hàng chặt chém để bỏ qua
            float variance  = 0.75f + (float)rng.NextDouble() * 0.5f;
            int   unitPrice = Mathf.Max(1, Mathf.RoundToInt(def.BuyPrice * variance));

            string listingId = "npc_" + cycleSeed.ToString("X") + "_" + npcListings.Count.ToString("00");

            npcListings.Add(MarketListing.CreateNpcListing(
                listingId, seller, def.ItemID, quantity, unitPrice, now, NpcListingLifetime));
        }

        OnListingsChanged?.Invoke();
    }

    private static MarketItemDef PickWeighted(List<MarketItemDef> items, int totalWeight, System.Random rng)
    {
        if (items.Count == 0 || totalWeight <= 0)
            return null;

        int roll = rng.Next(0, totalWeight);
        for (int i = 0; i < items.Count; i++)
        {
            roll -= Mathf.Max(1, items[i].Weight);
            if (roll < 0)
                return items[i];
        }

        return items[items.Count - 1];
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ĐỌC DANH SÁCH
    // ══════════════════════════════════════════════════════════════════════

    public IReadOnlyList<MarketListing> GetListings(MarketCategory category)
    {
        filterBuffer.Clear();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Hàng người chơi đứng TRƯỚC hàng NPC: người chơi bỏ công đăng bán thì
        // phải thấy hàng mình ở chỗ dễ nhìn, không thì quầy hàng mất động lực.
        List<MarketListing> playerListings = MarketPlayerListingBridge.FetchActiveListings();
        for (int i = 0; i < playerListings.Count; i++)
            TryAdd(playerListings[i], category, now);

        for (int i = 0; i < npcListings.Count; i++)
            TryAdd(npcListings[i], category, now);

        // Ưu tiên: có loa → hàng người chơi → giảm giá sâu nhất
        filterBuffer.Sort(CompareListing);
        return filterBuffer;
    }

    private void TryAdd(MarketListing listing, MarketCategory category, DateTimeOffset now)
    {
        if (listing == null || listing.Status != MarketListingStatus.Active)
            return;

        if (listing.IsExpired(now))
            return;

        if (category != MarketCategory.All && listing.Category != category)
            return;

        filterBuffer.Add(listing);
    }

    private static int CompareListing(MarketListing a, MarketListing b)
    {
        if (a.HasLoa != b.HasLoa)
            return a.HasLoa ? -1 : 1;

        if (a.IsPlayerListing != b.IsPlayerListing)
            return a.IsPlayerListing ? -1 : 1;

        return a.DiscountPercentVsNpc().CompareTo(b.DiscountPercentVsNpc());
    }

    public MarketListing GetListing(string listingId)
    {
        if (string.IsNullOrEmpty(listingId))
            return null;

        for (int i = 0; i < npcListings.Count; i++)
            if (npcListings[i].ListingId == listingId)
                return npcListings[i];

        List<MarketListing> playerListings = MarketPlayerListingBridge.FetchActiveListings();
        for (int i = 0; i < playerListings.Count; i++)
            if (playerListings[i] != null && playerListings[i].ListingId == listingId)
                return playerListings[i];

        return null;
    }

    public bool MarkListingSold(string listingId)
    {
        MarketListing listing = GetListing(listingId);
        if (listing == null || listing.Status != MarketListingStatus.Active)
            return false;

        listing.Status = MarketListingStatus.Sold;

        // Hàng của người chơi nằm trong dữ liệu của DEV-B — phải báo về đó,
        // nếu chỉ đổi Status trên bản sao thì quầy hàng vẫn tưởng còn hàng
        if (listing.IsPlayerListing)
            MarketPlayerListingBridge.OnPlayerListingSold?.Invoke(listingId);

        OnListingsChanged?.Invoke();
        return true;
    }
}
