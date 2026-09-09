using System.Collections.Generic;

/// <summary>
/// CỔNG CẮM KHO NGOÀI cho quầy hàng (Vòng 16 · Hồ Câu).
///
/// VÌ SAO cần interface này: quầy hàng (Assets/_Game/Farm) chỉ biết hai kho farm
/// (FarmInventoryManager, WarehouseManager). Module Hồ Câu có giỏ cá riêng
/// (FarmGame.Fishing.FishBasket) và muốn cá cũng lên quầy — nhưng farm KHÔNG được
/// tham chiếu namespace Fishing (module có thể tắt/gỡ mà farm vẫn biên dịch).
/// Đảo chiều phụ thuộc: module ngoài implement interface này rồi gọi
/// <see cref="StallExternalStores.Register"/> một lần lúc khởi động.
///
/// Hợp đồng B8 (KHÔNG MẤT HÀNG) áp lên mọi implement:
/// • <see cref="TryTake"/> trả false thì KHÔNG được trừ gì.
/// • <see cref="GiveBack"/> trả false khi không nhận được TRỌN số lượng (kho đầy, kho chưa nạp…) —
///   quầy sẽ giữ cờ refundPending và thử lại nhịp sau. Tuyệt đối không nhận một phần rồi trả true,
///   cũng không kẹp/cắt số lượng im lặng.
/// </summary>
public interface IStallExternalStore
{
    /// <summary>
    /// Kho có biết mặt hàng này không (kể cả khi đang giữ 0 cái). true kèm thông tin hiển thị + giá gốc.
    /// Đây cũng là cách quầy xác định kho nguồn của một itemId lạ, nên phải trả lời ổn định (không phụ thuộc số lượng).
    /// </summary>
    bool TryGetItemInfo(string itemId, out StallExternalItemInfo info);

    /// <summary>Số lượng đang có trong kho. 0 khi không biết mặt hàng.</summary>
    int GetAvailable(string itemId);

    /// <summary>Trừ kho để đưa lên quầy. false = không đủ / không hợp lệ, kho không đổi.</summary>
    bool TryTake(string itemId, int amount);

    /// <summary>Hoàn hàng về kho (huỷ tay / hết hạn). false = chưa nhận được, quầy sẽ thử lại sau.</summary>
    bool GiveBack(string itemId, int amount);

    /// <summary>Thêm mọi dòng số lượng &gt; 0 vào danh sách (điền <c>store</c> bằng giá trị đã đăng ký). Không Clear danh sách.</summary>
    void EnumerateSellable(List<StallSellableItem> into);
}
