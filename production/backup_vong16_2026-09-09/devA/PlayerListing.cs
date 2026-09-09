using System;
using UnityEngine;

/// <summary>
/// Kho NGUỒN của một mặt hàng đã đặt lên quầy.
///
/// VÌ SAO phải ghi kho nguồn vào từng listing thay vì tra lại lúc hoàn hàng:
/// dự án có HAI kho tách biệt (WarehouseManager = hạt giống, FarmInventoryManager =
/// nông sản/món/chăn nuôi). Nếu đợi đến lúc hoàn hàng mới đoán kho thì phải đoán
/// bằng dữ liệu bên ngoài (CropData) — mà lúc đó số lượng trong kho đã bằng 0 nên
/// mọi phép "kho nào đang có món này" đều sai. Đoán sai một lần là hàng của người
/// chơi rơi vào kho không đúng và coi như MẤT (hạt giống lạc vào kho nông sản thì
/// không trồng được). Ghi lại nguồn ngay lúc trừ kho là cách duy nhất chắc chắn.
/// </summary>
public enum StallSourceStore
{
    /// <summary>FarmInventoryManager — nông sản, món ăn, sản phẩm chuồng, gia vị, vật liệu.</summary>
    FarmInventory = 0,

    /// <summary>WarehouseManager — CHỈ hạt giống.</summary>
    SeedWarehouse = 1,
}

/// <summary>
/// Trạng thái vòng đời của một mặt hàng rao bán.
///
/// Giá trị được gán số CỐ ĐỊNH và listing lưu xuống save bằng số nguyên thô
/// (<see cref="PlayerListing.statusRaw"/>) — chèn thêm trạng thái mới ở giữa
/// sẽ làm mọi save cũ đọc sai. Trạng thái mới phải thêm vào CUỐI.
/// </summary>
public enum ListingStatus
{
    Active    = 0,
    Sold      = 1,
    Expired   = 2,
    Cancelled = 3,
}

/// <summary>
/// Một mặt hàng người chơi rao bán ở quầy.
///
/// Hình dạng trường được viết theo kiểu SERVER ngay từ bây giờ (sellerId/sellerName/
/// createdUtc/expiresUtc) dù giai đoạn này chỉ có một người chơi. Lý do ở
/// `PHAN_TICH_CHO_VA_PLAN.md` mục 2.1: khi thay nguồn dữ liệu bằng server thật,
/// UI và luồng không phải sửa dòng nào. Đổi hình dạng này về sau là phải viết lại
/// cả bảng tin chợ lẫn quầy hàng.
///
/// Mọi mốc thời gian lưu bằng **UTC ticks tuyệt đối**, không lưu "còn lại bao nhiêu
/// giây". Nhờ vậy thoát game rồi mở lại vẫn đúng mà không cần chạy nền — và người
/// chơi không lách được bằng cách tắt app.
/// </summary>
[Serializable]
public class PlayerListing
{
    // ── Danh tính ────────────────────────────────────────────────────────────
    public string listingId;
    public string sellerId;        // "local" khi là mình; id thật khi có server
    public string sellerName;
    public int    sellerAvatar;

    // ── Mặt hàng ─────────────────────────────────────────────────────────────
    public string itemId;
    public int    quantity;
    public int    pricePerUnit;

    // ── Thời gian (UTC ticks) ────────────────────────────────────────────────
    public long createdUtcTicks;
    public long expiresUtcTicks;

    /// <summary>
    /// Mốc NPC sẽ mua món này (B9). Rút ngẫu nhiên ngay lúc đăng bán chứ không
    /// quay số mỗi frame: có mốc tuyệt đối thì lúc người chơi tắt app rồi mở lại,
    /// ta so mốc với hiện tại là biết ngay đã bán hay chưa — không cần mô phỏng
    /// lại quãng thời gian offline.
    /// </summary>
    public long npcBuyAtUtcTicks;

    // ── Trạng thái ───────────────────────────────────────────────────────────
    /// <summary>Lưu thô dạng int: JsonUtility ghi enum theo số thứ tự, để int cho rõ ràng.</summary>
    public int  statusRaw;
    public int  sourceStoreRaw;
    public bool hasLoa;
    public int  slotIndex;

    /// <summary>
    /// Hàng đã kết thúc (hết hạn/huỷ) nhưng CHƯA hoàn được về kho vì manager kho
    /// chưa tồn tại lúc đó. Giữ cờ này để phiên sau thử hoàn lại — nếu bỏ qua thì
    /// hàng biến mất vĩnh viễn, phạm điều kiện "không mất hàng trong mọi đường" (B8).
    /// </summary>
    public bool refundPending;

    // ── Truy cập tiện lợi (property KHÔNG được JsonUtility lưu — cố ý) ────────

    public ListingStatus Status
    {
        get => (ListingStatus)statusRaw;
        set => statusRaw = (int)value;
    }

    public StallSourceStore SourceStore
    {
        get => (StallSourceStore)sourceStoreRaw;
        set => sourceStoreRaw = (int)value;
    }

    public bool IsActive => Status == ListingStatus.Active;

    public int TotalPrice => Mathf.Max(0, quantity) * Mathf.Max(0, pricePerUnit);

    public DateTime CreatedUtc => TicksToUtc(createdUtcTicks);
    public DateTime ExpiresUtc => TicksToUtc(expiresUtcTicks);
    public DateTime NpcBuyAtUtc => TicksToUtc(npcBuyAtUtcTicks);

    /// <summary>Số giây còn lại trước khi hết hạn. Âm nghĩa là đã quá hạn.</summary>
    public double RemainingSeconds(DateTime utcNow) => (ExpiresUtc - utcNow).TotalSeconds;

    public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresUtc;

    public bool IsNpcReadyToBuyAt(DateTime utcNow)
        => npcBuyAtUtcTicks > 0 && utcNow >= NpcBuyAtUtc;

    /// <summary>
    /// Ticks bẩn (0, âm, hoặc vượt biên DateTime) sẽ ném ArgumentOutOfRangeException
    /// và làm hỏng cả vòng cập nhật quầy. Save của người chơi có thể bị sửa tay hoặc
    /// hỏng nửa chừng, nên phải kẹp chứ không tin.
    /// </summary>
    private static DateTime TicksToUtc(long ticks)
    {
        if (ticks < DateTime.MinValue.Ticks) return DateTime.MinValue;
        if (ticks > DateTime.MaxValue.Ticks) return DateTime.MaxValue;
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>Chuỗi ngắn để log/soi trong Inspector, không dùng cho UI.</summary>
    public override string ToString()
        => $"[{listingId?.Substring(0, Mathf.Min(6, listingId?.Length ?? 0))}] " +
           $"{itemId} x{quantity} @{pricePerUnit} · {Status} · ô{slotIndex}";
}
