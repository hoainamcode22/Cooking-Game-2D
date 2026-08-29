using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  BỘ SINH ĐƠN HÀNG — mục 5.1, 5.3, 5.4 file TEAM
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO là `static` chứ không phải MonoBehaviour hay ScriptableObject:
/// bộ sinh là một HÀM THUẦN — vào là (cấp người chơi, kho hiện có), ra là một đơn.
/// Không giữ trạng thái nào ngoài kho tên (đã tách sang <see cref="OrderNameBank"/>).
/// Để nó là component thì mọi nơi muốn sinh thử một đơn (Editor tool, test cân bằng)
/// đều phải dựng scene trước — đúng cái phiền của `VillageOrderManager` cũ.
///
/// VÌ SAO KHÔNG có 37 file `.asset` định nghĩa vật phẩm như hệ cũ:
/// <see cref="MarketPriceTable"/> đã khai đủ ~85 vật phẩm kèm giá, cấp mở khoá, danh mục
/// và cờ bật/tắt. 37 asset kia chỉ là bản sao KÉM HƠN của cùng dữ liệu đó — và đã lệch
/// thật: `Order_item_salad_nam_rau.asset` điền nhầm `itemId = salad_bap_cai_chanh`, món
/// salad nấm không bao giờ ra đơn mà không ai phát hiện suốt nhiều tháng.
/// Một nguồn sự thật thì không có chỗ cho loại lỗi đó.
/// </summary>
public static class OrderGenerator
{
    // ══════════════════════════════════════════════════════════════════════
    //  HẰNG SỐ CÂN BẰNG — mọi con số cân bằng nằm ở đây, không rải trong code
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Hệ số thưởng theo bậc — mục 5.3. Chỉ số mảng = (int)tier - 1.</summary>
    // Sàn lợi nhuận là `giá_gốc × 1.3 (giá bán ở quầy) × 1.1` = **1.43**. Bốn hệ số cũ
    // {1.00, 1.15, 1.30, 1.50} nằm DƯỚI hoặc sát sàn đó, nên mô phỏng 10.000 đơn/cấp cho
    // thấy: cấp 1–5 có **100%** đơn chạm sàn, cấp 6–12 là 68–72%. Nghĩa là hệ số bậc và
    // cả nhiễu ngẫu nhiên 0.90–1.15 đều là CODE CHẾT ở nửa đầu game — thưởng hoàn toàn
    // tất định, mọi đơn cùng giá trị hàng đều trả đúng một con số, không có gì bất ngờ.
    //
    // Nâng lên trên 1.43 để hệ số bậc thật sự có tác dụng. Bậc 4 cố ý chỉ 1.85 chứ không
    // cao hơn: NPC ở chợ bán ra ×1.5, để bậc 4 quá cao là người chơi mua lại rồi giao đơn
    // ăn chênh lệch, khỏi cần farm.
    private static readonly float[] TierGoldMultiplier = { 1.50f, 1.62f, 1.74f, 1.85f };

    /// <summary>Đơn có món nấu được thêm 40% — trả công nấu, không phải chỉ công gom.</summary>
    private const float DishGoldMultiplier = 1.40f;

    private const float RewardRandomMin = 0.90f;
    private const float RewardRandomMax = 1.15f;

    /// <summary>EXP = vàng thưởng / 8, tối thiểu 3 — mục 5.3.</summary>
    private const int   ExpPerGold      = 8;
    private const int   MinRewardExp    = 3;

    /// <summary>
    /// SÀN LỢI NHUẬN: đơn phải trả cao hơn bán thẳng ở Quầy Hàng ít nhất 10%.
    ///
    /// VÌ SAO cần cái sàn này dù mục 5.3 đã cho công thức: bán ở quầy được
    /// `giá gốc × 1.3` (<see cref="MarketPriceTable.SuggestedSellMultiplier"/>), trong khi
    /// hệ số bậc 1 chỉ là ×1.0. Nghĩa là nếu chạy đúng công thức trần thì suốt cấp 1–5
    /// giao đơn LỖ so với mang ra quầy bán — người chơi tính ra là bỏ hẳn bảng đơn,
    /// và ta mất luôn một trong hai vòi vàng lặp lại (mục 3 file TEAM).
    /// Sàn này giữ đúng tinh thần câu chốt của mục 5.3: "phải có lãi thì người chơi
    /// mới giao đơn thay vì bán".
    /// </summary>
    private const float ProfitOverStallSelling = 1.10f;

    /// <summary>
    /// Danh mục được phép vào đơn. Ngoài bốn cái này đều bị loại:
    ///   • `HatGiong`  — hạt là ĐẦU VÀO của farm. Bắt giao hạt là bắt người chơi phá
    ///                   chính ruộng của mình để trả đơn.
    ///   • `GiaVi`     — muối/rau thơm/nước tương/nước mắm KHÔNG có nguồn sản xuất trong
    ///                   farm, chỉ mua được ở chợ (mục 4 file TEAM). Đơn đòi gia vị là
    ///                   đơn không làm nổi bằng lao động, chỉ bằng tiền.
    ///   • `CheBien`   — cả ba món (bot_gao, nuoc_mia_ep, pho_mai) đang `MarketEnabled=false`
    ///                   vì thiếu icon; lọc theo cờ đó cũng đã loại rồi, để đây cho rõ ý.
    ///   • `VatLieu`   — gỗ/đá/đinh/sơn/kính đến từ tàu hàng, không phải từ farm.
    /// </summary>
    private static readonly HashSet<MarketCategory> AllowedCategories = new HashSet<MarketCategory>
    {
        MarketCategory.NongSan,
        MarketCategory.Hoa,
        MarketCategory.ChanNuoi,
        MarketCategory.MonAn,
    };

    /// <summary>
    /// Mốc giá dùng để suy số lượng: hàng rẻ thì đặt nhiều, hàng đắt thì đặt ít.
    ///
    /// VÌ SAO cần: bậc 4 cho phép 4–12 đơn vị mỗi món. Bốc thẳng trong khoảng đó thì có
    /// ngày ra đơn "12 Phở Bò Tái" — nấu 12 bát phở là hàng giờ đồng hồ, người chơi bấm
    /// thùng rác ngay và cái đơn đó chỉ tổ tốn một ô bảng. Neo số lượng theo giá trị món
    /// giữ mọi đơn ở cùng một tầm "công sức bỏ ra", bất kể món gì.
    /// </summary>
    private const int ValueRefCheap    = 7;     // giá lúa — món rẻ nhất bảng
    private const int ValueRefPremium  = 300;   // mốc "đắt" — KHÔNG phải giá cao nhất
    // 28-08-2026: thêm 20 món mới, món đắt nhất giờ là 884 (Salad dưa hấu bò áp chảo),
    // không còn là phở bò tái 400. CỐ Ý GIỮ mốc ở 300: `Mathf.InverseLerp` tự kẹp về 1
    // nên mọi món ≥ 300 đều rơi về `minAmt` — đúng ý muốn (món đắt thì đặt ít nhất).
    // Nâng mốc lên 884 sẽ khiến các món 300–500 bị đặt số lượng LỚN hơn hiện tại,
    // tức đơn khó hơn — đó là quyết định cân bằng, phải Sếp duyệt mới đổi.

    // ══════════════════════════════════════════════════════════════════════
    //  BẬC ĐỘ KHÓ
    // ══════════════════════════════════════════════════════════════════════

    public static OrderTier GetTierForLevel(int level)
    {
        if (level <= 5)  return OrderTier.TapSu;
        if (level <= 12) return OrderTier.QuenTay;
        if (level <= 20) return OrderTier.LanhNghe;
        return OrderTier.BacThay;
    }

    /// <summary>
    /// Xác suất số món trong đơn — mục 5.1. Mảng con là xác suất tích luỹ cho
    /// 1, 2, 3, 4 món. Bậc 1 luôn 1 món (đang tập sự, đừng làm người ta rối).
    /// </summary>
    private static int RollLineCount(OrderTier tier, System.Random rng)
    {
        double r = rng.NextDouble();

        switch (tier)
        {
            case OrderTier.TapSu:
                return 1;

            case OrderTier.QuenTay:                       // 60 / 40
                return r < 0.60 ? 1 : 2;

            case OrderTier.LanhNghe:                      // 20 / 50 / 30
                if (r < 0.20) return 1;
                return r < 0.70 ? 2 : 3;

            case OrderTier.BacThay:                       // 10 / 30 / 40 / 20
                if (r < 0.10) return 1;
                if (r < 0.40) return 2;
                return r < 0.80 ? 3 : 4;

            default:
                return 1;
        }
    }

    private static void GetAmountRange(OrderTier tier, out int min, out int max)
    {
        switch (tier)
        {
            case OrderTier.TapSu:    min = 2; max = 5;  return;
            case OrderTier.QuenTay:  min = 2; max = 8;  return;
            case OrderTier.LanhNghe: min = 3; max = 10; return;
            case OrderTier.BacThay:  min = 4; max = 12; return;
            default:                 min = 2; max = 5;  return;
        }
    }

    /// <summary>
    /// Khoảng số lượng RIÊNG cho món nấu. Hẹp hơn hẳn khoảng chung ở trên.
    ///
    /// VÌ SAO tách riêng thay vì để công thức neo-theo-giá tự lo: khoảng "2–8 mỗi món"
    /// ở mục 5.1 rõ ràng viết cho NÔNG SẢN — thu hoạch 8 bắp cải là một lượt quét liềm,
    /// còn nấu 8 đĩa cơm chiên là tám lượt mở bếp và tám lần gom đủ nguyên liệu.
    /// Thử số thật với khoảng chung: 6 × Cơm Chiên Trứng ⇒ ~1060 vàng cho MỘT đơn ở cấp 6,
    /// trong khi mục 3 file TEAM ghi đơn món ăn cũ chỉ 110–340 vàng. Vừa quá sức người
    /// chơi vừa thổi bay cân bằng kinh tế.
    ///
    /// Với khoảng này: 2 × Cơm Chiên Trứng ⇒ 220 × 1.15 × 1.4 ≈ 354 vàng / 44 EXP —
    /// nhỉnh hơn mức cũ đúng như yêu cầu "tương đương hoặc hơn", mà vẫn làm nổi.
    /// </summary>
    private static void GetDishAmountRange(OrderTier tier, out int min, out int max)
    {
        switch (tier)
        {
            case OrderTier.QuenTay:  min = 1; max = 2; return;
            case OrderTier.LanhNghe: min = 1; max = 3; return;
            case OrderTier.BacThay:  min = 2; max = 4; return;
            default:                 min = 1; max = 1; return;   // bậc 1 không có món nấu
        }
    }

    /// <summary>
    /// Tối đa bao nhiêu MÓN NẤU trong một đơn.
    ///
    /// Bậc 4 mới cho 2 món nấu. Lý do: mỗi món nấu là một lượt mở bếp + gom nguyên liệu.
    /// Hai món nấu trong cùng một đơn ở cấp thấp là đơn "để đó", chiếm ô mà không ai làm.
    /// </summary>
    private static int MaxDishesFor(OrderTier tier) => tier == OrderTier.BacThay ? 2 : 1;

    /// <summary>
    /// Trần giá món ăn theo bậc — thay cho việc quét `DishData.difficulty` lúc chạy.
    ///
    /// VÌ SAO không đọc `DishData` thật: DishData nằm rải ở hai thư mục asset và KHÔNG
    /// có registry toàn cục nào tải sẵn chúng (xem chú thích của `StallItemCatalog`).
    /// Muốn đọc thì phải `Resources.LoadAll` — kéo mọi asset món ăn vào build kể cả món
    /// chưa mở khoá. Mà giá trong `MarketPriceTable` vốn đã được đặt THEO độ khó:
    /// Easy 95–165, Normal 175–240, Hard 270–320. Dùng giá làm thước là đọc đúng thứ
    /// mình cần mà không tải gì thêm.
    /// </summary>
    private static int MaxDishPriceFor(OrderTier tier)
    {
        switch (tier)
        {
            case OrderTier.TapSu:    return 0;     // bậc 1 không có món nấu
            case OrderTier.QuenTay:  return 175;   // món Dễ
            case OrderTier.LanhNghe: return 250;   // + món Thường
            default:                 return int.MaxValue;   // bậc 4: mọi món
        }
    }

    private static bool IsCategoryAllowedForTier(MarketCategory category, OrderTier tier)
    {
        if (!AllowedCategories.Contains(category)) return false;

        switch (tier)
        {
            // Bậc 1 — tập sự: CHỈ nông sản cơ bản. Người chơi cấp 1–5 chưa chắc đã có
            // chuồng trại hay bếp; đơn đòi trứng lúc chưa có gà là đơn chết.
            case OrderTier.TapSu:
                return category == MarketCategory.NongSan;

            // Bậc 2 trở lên mở hết bốn danh mục; món ăn còn bị chặn thêm bằng trần giá.
            default:
                return true;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  RỔ VẬT PHẨM
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dựng rổ vật phẩm hợp lệ cho một bậc + cấp người chơi.
    ///
    /// Bốn tầng lọc, theo thứ tự rẻ-trước-đắt-sau để bớt việc:
    ///   1. `MarketEnabled` — cờ sẵn có, tự loại 3 sản phẩm máy chế biến còn thiếu icon
    ///      (`bot_gao`, `nuoc_mia_ep`, `pho_mai`). Hai món cá và `nuoc_mia_chanh` trước đây
    ///      cũng bị cờ này loại; nay hai món cá đã XOÁ hẳn khỏi bảng giá (A4) và
    ///      `nuoc_mia_chanh` đã nấu được nên được bật lại (A2).
    ///   2. danh mục được phép theo bậc
    ///   3. cấp mở khoá
    ///   4. trần giá món ăn theo bậc
    /// </summary>
    private static List<MarketItemInfo> BuildPool(OrderTier tier, int playerLevel)
    {
        IReadOnlyList<MarketItemInfo> all = MarketPriceTable.AllItems;
        List<MarketItemInfo> pool = new List<MarketItemInfo>(all.Count);

        int maxDishPrice = MaxDishPriceFor(tier);

        for (int i = 0; i < all.Count; i++)
        {
            MarketItemInfo info = all[i];

            if (!info.MarketEnabled) continue;
            if (!IsCategoryAllowedForTier(info.Category, tier)) continue;
            if (info.UnlockLevel > playerLevel) continue;
            if (info.Category == MarketCategory.MonAn && info.BasePrice > maxDishPrice) continue;
            if (info.BasePrice <= 0) continue;   // giá 0 làm mọi phép nhân thưởng sập về 0

            pool.Add(info);
        }

        return pool;
    }

    private static MarketItemInfo PickWeighted(List<MarketItemInfo> pool, System.Random rng)
    {
        int total = 0;
        for (int i = 0; i < pool.Count; i++) total += Mathf.Max(1, pool[i].Weight);

        int roll = rng.Next(total);
        int acc  = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            acc += Mathf.Max(1, pool[i].Weight);
            if (roll < acc) return pool[i];
        }

        return pool[pool.Count - 1];
    }

    // ══════════════════════════════════════════════════════════════════════
    //  SINH ĐƠN
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sinh một đơn thường theo cấp người chơi.
    /// Trả null khi rổ rỗng (không có vật phẩm nào hợp lệ) — người gọi phải chặn.
    /// </summary>
    public static OrderData Generate(int playerLevel, System.Random rng)
    {
        OrderTier tier = GetTierForLevel(playerLevel);
        List<MarketItemInfo> pool = BuildPool(tier, playerLevel);
        if (pool.Count == 0) return null;

        int wantLines = Mathf.Min(RollLineCount(tier, rng), pool.Count);
        GetAmountRange(tier, out int minAmt, out int maxAmt);

        OrderData order = new OrderData { tier = tier };

        int dishesUsed = 0;
        int maxDishes  = MaxDishesFor(tier);
        HashSet<string> used = new HashSet<string>();

        // Vòng thử: mỗi lượt bốc một món, bỏ qua nếu trùng hoặc vượt hạn ngạch món nấu.
        // Trần 24 lượt để không bao giờ lặp vô hạn khi rổ nhỏ hơn số món muốn có.
        for (int attempt = 0; attempt < 24 && order.lines.Count < wantLines; attempt++)
        {
            MarketItemInfo pick = PickWeighted(pool, rng);

            if (used.Contains(pick.ItemId)) continue;

            bool isDish = pick.Category == MarketCategory.MonAn;
            if (isDish && dishesUsed >= maxDishes) continue;

            int lo = minAmt, hi = maxAmt;
            if (isDish) GetDishAmountRange(tier, out lo, out hi);

            int amount = RollAmount(pick.BasePrice, lo, hi, rng);

            order.lines.Add(new OrderLine
            {
                itemId         = MarketPriceTable.Canonical(pick.ItemId),
                displayName    = pick.DisplayName,
                requiredAmount = amount,
            });

            used.Add(pick.ItemId);
            if (isDish) dishesUsed++;
        }

        if (order.lines.Count == 0) return null;

        Finalize(order, rng);
        return order;
    }

    /// <summary>
    /// Sinh một đơn mà NGƯỜI CHƠI GIAO ĐƯỢC NGAY với kho hiện tại — mục 5.4 file TEAM.
    ///
    /// VÌ SAO phải có hàm riêng: bảng 9 ô toàn đơn không làm nổi là bảng chết. Người chơi
    /// mở ra, không bấm được gì, đóng lại, và không bao giờ mở nữa. Hệ cũ không có luật
    /// này nên một đơn đòi thứ chưa mở khoá sẽ chiếm chỗ nhà đó VĨNH VIỄN.
    ///
    /// Đơn sinh ra vẫn đúng bậc và đúng công thức thưởng — chỉ khác ở chỗ rổ bị siết
    /// xuống những món người chơi đang có, và số lượng không vượt quá số đang có.
    /// Trả null khi kho không đủ nuôi nổi một đơn nào; người gọi rơi về <see cref="Generate"/>.
    /// </summary>
    public static OrderData GenerateDeliverable(int playerLevel, Func<string, int> ownedLookup, System.Random rng)
    {
        if (ownedLookup == null) return null;

        OrderTier tier = GetTierForLevel(playerLevel);
        List<MarketItemInfo> pool = BuildPool(tier, playerLevel);
        if (pool.Count == 0) return null;

        GetAmountRange(tier, out int minAmt, out int maxAmt);

        // Chỉ giữ món đang có đủ mức TỐI THIỂU của chính loại đó trong kho — dưới mức ấy
        // thì dù đặt số lượng nhỏ nhất người chơi cũng không giao nổi.
        // Món nấu dùng ngưỡng riêng (thấp hơn), nếu không thì một đĩa phở đang nằm trong
        // kho vẫn bị loại chỉ vì ngưỡng nông sản là 3.
        List<MarketItemInfo> affordable = new List<MarketItemInfo>(pool.Count);
        for (int i = 0; i < pool.Count; i++)
        {
            string id = MarketPriceTable.Canonical(pool[i].ItemId);

            int need = minAmt;
            if (pool[i].Category == MarketCategory.MonAn)
            {
                GetDishAmountRange(tier, out int dishMin, out _);
                need = dishMin;
            }

            if (ownedLookup(id) >= need) affordable.Add(pool[i]);
        }

        if (affordable.Count == 0) return null;

        // Đơn "dễ" cố ý ÍT MÓN: một món là chắc chắn giao được, càng nhiều món càng dễ
        // vướng một món thiếu. Bậc cao vẫn cho 2 món để không nhìn ra là đơn được ưu ái.
        int wantLines = Mathf.Min(tier >= OrderTier.LanhNghe ? 2 : 1, affordable.Count);

        OrderData order = new OrderData { tier = tier };
        HashSet<string> used = new HashSet<string>();
        int dishesUsed = 0;
        int maxDishes  = MaxDishesFor(tier);

        for (int attempt = 0; attempt < 24 && order.lines.Count < wantLines; attempt++)
        {
            MarketItemInfo pick = PickWeighted(affordable, rng);
            string id = MarketPriceTable.Canonical(pick.ItemId);

            if (used.Contains(id)) continue;

            bool isDish = pick.Category == MarketCategory.MonAn;
            if (isDish && dishesUsed >= maxDishes) continue;

            int lo = minAmt, hi = maxAmt;
            if (isDish) GetDishAmountRange(tier, out lo, out hi);

            int owned  = ownedLookup(id);
            int amount = RollAmount(pick.BasePrice, lo, Mathf.Min(hi, owned), rng);
            if (amount > owned) amount = owned;      // chốt chặn cuối, không được vượt kho
            if (amount < 1) continue;

            order.lines.Add(new OrderLine
            {
                itemId         = id,
                displayName    = pick.DisplayName,
                requiredAmount = amount,
            });

            used.Add(id);
            if (isDish) dishesUsed++;
        }

        if (order.lines.Count == 0) return null;

        Finalize(order, rng);
        return order;
    }

    /// <summary>
    /// Số lượng đặt cho một món: neo theo giá trị món rồi nhiễu nhẹ ±1.
    /// Món rẻ đẩy về cận trên, món đắt kéo về cận dưới — xem <see cref="ValueRefCheap"/>.
    /// </summary>
    private static int RollAmount(int basePrice, int minAmt, int maxAmt, System.Random rng)
    {
        if (maxAmt < minAmt) maxAmt = minAmt;

        float t      = Mathf.InverseLerp(ValueRefCheap, ValueRefPremium, basePrice);
        int   anchor = Mathf.RoundToInt(Mathf.Lerp(maxAmt, minAmt, t));

        int jitter = rng.Next(-1, 2);   // -1, 0, +1
        return Mathf.Clamp(anchor + jitter, minAmt, maxAmt);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CHỦ ĐỀ + THƯỞNG
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Gắn chủ đề, tên, khách hàng và phần thưởng cho một đơn đã có nội dung.</summary>
    private static void Finalize(OrderData order, System.Random rng)
    {
        order.theme            = ChooseTheme(order, rng);
        order.title            = OrderNameBank.PickTitle(order.theme, rng);
        order.customerAvatarId = OrderNameBank.PickCustomerId(rng);
        order.orderId          = Guid.NewGuid().ToString("N").Substring(0, 12);

        ComputeReward(order, rng);
    }

    /// <summary>
    /// Chọn chủ đề THEO NỘI DUNG đơn. Thứ tự kiểm là thứ tự ưu tiên: đặc thù trước,
    /// chung chung sau — nếu đảo lại thì "Bữa cơm gia đình" sẽ nuốt hết mọi đơn.
    /// </summary>
    private static OrderTheme ChooseTheme(OrderData order, System.Random rng)
    {
        bool allFlowers  = true;
        bool allLivestock = true;
        bool hasDish     = false;
        int  totalQty    = 0;

        for (int i = 0; i < order.lines.Count; i++)
        {
            OrderLine line = order.lines[i];
            if (line == null) continue;

            MarketCategory cat = MarketPriceTable.GetCategory(line.itemId);

            if (cat != MarketCategory.Hoa)      allFlowers   = false;
            if (cat != MarketCategory.ChanNuoi) allLivestock = false;
            if (cat == MarketCategory.MonAn)    hasDish      = true;

            totalQty += line.requiredAmount;
        }

        // 1 · Toàn hoa thì chỉ có thể là bó hoa. Không có ngoại lệ nào nghe lọt tai.
        if (allFlowers) return OrderTheme.BoHoa;

        // 2 · Bậc thầy: 40% khoác áo "đơn gấp" — bậc này vốn thưởng cao và khó,
        //     đúng nghĩa "khách quý trả hậu". Không cho 100% vì như vậy mọi đơn
        //     cấp cao đều gấp, chữ "gấp" mất trọng lượng.
        if (order.tier == OrderTier.BacThay && rng.NextDouble() < 0.40) return OrderTheme.DonGap;

        // 3 · Có món nấu → đơn của một cái bếp nào đó.
        if (hasDish) return OrderTheme.QuanAn;

        // 4 · Toàn sản phẩm chuồng → trang trại bạn cần tiếp tế.
        if (allLivestock) return OrderTheme.TrangTraiBan;

        // 5 · Từ 3 món trở lên → mâm cỗ, không còn là bữa cơm thường.
        if (order.lines.Count >= 3) return OrderTheme.TiecMung;

        // 6 · Ít món nhưng số lượng lớn → hàng đi chợ, không phải để ăn.
        if (totalQty >= 5 && rng.NextDouble() < 0.35) return OrderTheme.ChoPhien;

        return OrderTheme.BuaComGiaDinh;
    }

    /// <summary>
    /// Công thức thưởng — mục 5.3 file TEAM, kèm sàn lợi nhuận so với Quầy Hàng.
    ///
    ///   vàng gốc     = Σ( số lượng × giá gốc )
    ///   vàng thưởng  = vàng gốc × hệ số bậc × hệ số món ăn × ngẫu nhiên(0.90–1.15)
    ///   SÀN          = vàng gốc × 1.3 × 1.10      (bán ở quầy được 1.3, đơn phải hơn 10%)
    ///   exp thưởng   = vàng thưởng / 8, tối thiểu 3
    ///
    /// EXP tính từ vàng SAU khi áp sàn, không phải trước: nếu tính trước thì đơn bậc 1
    /// được nâng vàng nhưng EXP đứng yên, người chơi thấy hai con số không ăn khớp nhau.
    /// </summary>
    public static void ComputeReward(OrderData order, System.Random rng)
    {
        int baseGold = 0;
        bool hasDish = false;

        for (int i = 0; i < order.lines.Count; i++)
        {
            OrderLine line = order.lines[i];
            if (line == null) continue;

            baseGold += MarketPriceTable.GetBasePrice(line.itemId) * line.requiredAmount;
            if (MarketPriceTable.GetCategory(line.itemId) == MarketCategory.MonAn) hasDish = true;
        }

        int   tierIndex = Mathf.Clamp((int)order.tier - 1, 0, TierGoldMultiplier.Length - 1);
        float multiplier = TierGoldMultiplier[tierIndex];
        if (hasDish) multiplier *= DishGoldMultiplier;

        float noise = RewardRandomMin + (float)rng.NextDouble() * (RewardRandomMax - RewardRandomMin);
        int   gold  = Mathf.RoundToInt(baseGold * multiplier * noise);

        int floorGold = Mathf.CeilToInt(baseGold * MarketPriceTable.SuggestedSellMultiplier
                                                 * ProfitOverStallSelling);
        if (gold < floorGold) gold = floorGold;

        order.rewardGold = Mathf.Max(1, gold);
        order.rewardExp  = Mathf.Max(MinRewardExp, Mathf.RoundToInt(order.rewardGold / (float)ExpPerGold));
    }
}
