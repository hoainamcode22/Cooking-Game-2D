using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  DEV-A → DEV-B : CẮM BẢNG GIÁ GỐC VÀO SỔ GIÁ CỦA QUẦY HÀNG
/// ══════════════════════════════════════════════════════════════════════════
///
/// Hợp đồng chốt ở mục 7 file `production/TEAM_CHO_BANG_TIN_QUAY_HANG.md`:
/// DEV-B sở hữu `BasePriceBook` + `IBasePriceProvider`, DEV-A cắm bảng giá vào.
///
/// Chiều ngược lại (hàng người chơi → bảng tin) do DEV-B tự cắm trong
/// `PlayerStallManager.RegisterMarketBridge()`. CỐ Ý KHÔNG làm lại ở đây:
/// hai bên cùng ghi vào `MarketPlayerListingBridge.GetActiveListings` thì
/// bên nào chạy sau thắng, và sau này ai đọc cũng không biết bản nào đang chạy.
///
/// ── VÌ SAO KHÔNG PHẢI MonoBehaviour ─────────────────────────────────────
/// `MarketManager` nằm trên popup TẮT SẴN nên `Awake()` của nó chỉ chạy đúng lúc
/// người chơi mở chợ lần đầu. Quầy hàng thì cần giá gợi ý ngay khi vào farm.
/// `[RuntimeInitializeOnLoadMethod]` chạy trước mọi scene, không cần object nào
/// trong hierarchy, không ai kéo nhầm được.
///
/// (Ghi chú: `BasePriceBook` hiện đã gọi thẳng `MarketPriceTable` ở bậc 2 nên
/// việc đăng ký này là lớp bọc ngoài — giữ vì đó là hợp đồng đã chốt, và vì
/// nó là chỗ móc sẵn cho bản cân bằng thử nghiệm sau này.)
/// </summary>
public static class MarketStallBridgeAdapter
{
    private sealed class PriceProvider : IBasePriceProvider
    {
        public bool TryGetBasePrice(string itemId, out int basePrice)
        {
            if (MarketPriceTable.TryGet(itemId, out MarketItemInfo info) && info.BasePrice > 0)
            {
                basePrice = info.BasePrice;
                return true;
            }

            // Trả false chứ KHÔNG trả true kèm 0: BasePriceBook còn chuỗi dự phòng
            // phía sau (StallItemCatalog → bảng cứng → 10). Trả 0 là chặn mất chuỗi đó
            // và người chơi bán hàng lấy 0 vàng.
            basePrice = 0;
            return false;
        }
    }

    private static readonly PriceProvider Provider = new PriceProvider();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoRegister()
    {
        BasePriceBook.Register(Provider);
    }
}
