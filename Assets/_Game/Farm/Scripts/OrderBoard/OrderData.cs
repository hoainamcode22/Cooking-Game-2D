using System;
using System.Collections.Generic;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  MÔ HÌNH ĐƠN HÀNG — phía DEV-A (logic)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO có lớp này bên cạnh <see cref="OrderBoardOrderView"/> của DEV-B, nghe như thừa:
///
///  • `OrderBoardOrderView` là thứ GIAO DIỆN nhìn thấy — cố ý mỏng, chỉ đủ để vẽ.
///  • `OrderData` là thứ BỘ SINH và BỘ LƯU cần — có thêm bậc khó và chủ đề tên.
///
/// Hai trường `tier` / `theme` KHÔNG được nhét vào view: view đi thẳng ra UI, mà UI
/// không được phép quyết định gì dựa trên bậc khó (nếu được thì sớm muộn sẽ có đoạn
/// `if (tier == BacThay) tô đỏ` nằm lẫn trong code vẽ, rồi cân bằng game phải sửa UI).
///
/// Đổi lại phải giữ view đồng bộ — nên view được dựng MỘT LẦN trong
/// <see cref="BuildView"/> và không ai được sửa nó từ bên ngoài.
/// </summary>
public class OrderData
{
    // ── Nhận dạng ────────────────────────────────────────────────────────────

    /// <summary>Khoá duy nhất. Giao diện chỉ cầm chuỗi này khi gọi giao/bỏ đơn.</summary>
    public string orderId;

    /// <summary>Tên đơn lấy từ <see cref="OrderNameBank"/>.</summary>
    public string title;

    /// <summary>Mã khách hàng — 12 mã cố định, xem <see cref="OrderNameBank.CustomerIds"/>.</summary>
    public string customerAvatarId;

    // ── Phân loại (chỉ dùng nội bộ logic) ────────────────────────────────────

    public OrderTier  tier;
    public OrderTheme theme;

    // ── Nội dung ─────────────────────────────────────────────────────────────

    public readonly List<OrderLine> lines = new List<OrderLine>(4);

    public int rewardGold;

    /// <summary>
    /// EXP thưởng — ĐÃ LÀ CON SỐ CUỐI CÙNG.
    ///
    /// Hệ cũ (`VillageOrderManager.DeliverOrder`) hiện `rewardExp` lên popup rồi khi giao
    /// lại cộng `rewardExp * 2`. Người chơi thấy "+20 EXP" nhưng nhận 40 — một lỗi hiển thị
    /// kéo dài. Ở hệ mới KHÔNG có phép nhân nào ở bước giao: số hiện ra là số nhận được.
    /// </summary>
    public int rewardExp;

    // ── Cầu nối sang giao diện ───────────────────────────────────────────────

    private OrderBoardOrderView _view;

    /// <summary>
    /// Bản chiếu sang giao diện. Dựng một lần rồi dùng lại: lưới 3×3 vẽ lại mỗi khi kho
    /// đổi, tạo mới 9 view mỗi lần vẽ là rác GC vô ích trên máy yếu.
    /// </summary>
    public OrderBoardOrderView View
    {
        get
        {
            if (_view == null) _view = BuildView();
            return _view;
        }
    }

    private OrderBoardOrderView BuildView()
    {
        OrderBoardOrderView v = new OrderBoardOrderView
        {
            orderId          = orderId,
            title            = title,
            customerAvatarId = customerAvatarId,
            rewardGold       = rewardGold,
            rewardExp        = rewardExp,
            requirements     = new List<OrderBoardRequirementView>(lines.Count),
        };

        for (int i = 0; i < lines.Count; i++)
        {
            OrderLine line = lines[i];
            if (line == null) continue;

            v.requirements.Add(new OrderBoardRequirementView
            {
                itemId      = line.itemId,
                displayName = line.displayName,
                needAmount  = line.requiredAmount,
                ownedAmount = 0,   // giao diện tự nạp bằng GetOwnedAmount — xem OrderBoardContract
            });
        }

        return v;
    }

    /// <summary>Tổng giá trị gốc của hàng trong đơn — dùng để đối chiếu với giá bán ở quầy.</summary>
    public int GetBaseGoodsValue()
    {
        int sum = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            OrderLine line = lines[i];
            if (line == null) continue;
            sum += MarketPriceTable.GetBasePrice(line.itemId) * line.requiredAmount;
        }
        return sum;
    }

    public override string ToString()
    {
        string items = string.Empty;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] == null) continue;
            if (items.Length > 0) items += " + ";
            items += $"{lines[i].requiredAmount}x{lines[i].itemId}";
        }
        return $"[{tier}] \"{title}\" ({items}) → {rewardGold}v {rewardExp}exp";
    }
}

/// <summary>Một món trong đơn.</summary>
public class OrderLine
{
    /// <summary>
    /// Khoá kho ĐÃ CHUẨN HOÁ qua <see cref="MarketPriceTable.Canonical"/>.
    ///
    /// Bắt buộc chuẩn hoá tại nguồn chứ không phải lúc trừ kho: đơn được LƯU xuống
    /// PlayerPrefs, nếu ghi khoá thô ("Chicken", " rice ") thì phiên sau đọc lên là
    /// tra kho trượt, đơn vĩnh viễn không giao được mà không có lỗi nào báo.
    /// </summary>
    public string itemId;

    public string displayName;
    public int    requiredAmount;
}

/// <summary>
/// Bốn bậc độ khó theo cấp người chơi — mục 5.1 file TEAM.
/// Giá trị số được LƯU XUỐNG SAVE, không được đổi thứ tự.
/// </summary>
public enum OrderTier
{
    TapSu    = 1,   // cấp 1–5   · 1 món      · 2–5  mỗi món · nông sản cơ bản
    QuenTay  = 2,   // cấp 6–12  · 1–2 món    · 2–8  mỗi món · + chăn nuôi, hoa, món ăn dễ
    LanhNghe = 3,   // cấp 13–20 · 2–3 món    · 3–10 mỗi món · + món ăn thường, hoa hiếm
    BacThay  = 4,   // cấp 21+   · 3–4 món    · 4–12 mỗi món · mọi thứ
}

/// <summary>
/// Bảy chủ đề tên đơn — mục 5.2 file TEAM. Chủ đề được CHỌN THEO NỘI DUNG ĐƠN
/// (đơn toàn hoa thì không thể mang tên "Bữa cơm nhà bác Heo").
/// Giá trị số được LƯU XUỐNG SAVE, không được đổi thứ tự.
/// </summary>
public enum OrderTheme
{
    BuaComGiaDinh = 0,   // nông sản cơ bản
    TiecMung      = 1,   // nhiều món, giá trị cao
    QuanAn        = 2,   // có món nấu
    BoHoa         = 3,   // toàn hoa
    ChoPhien      = 4,   // hỗn hợp, số lượng lớn
    TrangTraiBan  = 5,   // sản phẩm chăn nuôi
    DonGap        = 6,   // thưởng cao, khó
}
