using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  F10 — GIÁ Ô ĐẤT TĂNG LUỸ TIẾN
/// ══════════════════════════════════════════════════════════════════════════
///
/// VẤN ĐỀ CŨ: "Đất Trồng" (itemID 100) cố định 50 vàng, mua vô hạn. Sau vài phút
/// người chơi có 50 ô đất, và mọi thứ khác trong game (chuồng 100–2000 vàng, nâng
/// cấp kho, đơn hàng) mất hết ý nghĩa vì cách kiếm tiền tối ưu chỉ là spam ô đất.
/// "Chậu Đá Quý" (112) còn tệ hơn: `goldPrice = 0` — ô hoa MIỄN PHÍ, vô hạn.
///
/// CÁCH SỬA: giá = giá gốc trong asset × 1.35^(số ô CÙNG LOẠI đã mua), làm tròn chục.
/// Ô đầu 50 · ô thứ 5 khoảng 170 · ô thứ 10 khoảng 740 · ô thứ 15 khoảng 3 300.
///
/// VÌ SAO 1.35 chứ không phải cộng thêm một số cố định: cộng tuyến tính thì ô thứ 30
/// vẫn rẻ so với thu nhập lúc đó (thu nhập tăng theo SỐ ô, tức là tuyến tính, nên
/// giá phải tăng nhanh hơn tuyến tính mới thành phanh). 1.35 cho khoảng 15 ô là dải
/// hợp lý trước khi giá chạm mức phải chơi nghiêm túc mới mua nổi.
///
/// VÌ SAO đếm "số ô đã MUA" chứ không phải "tổng số ô trong scene": scene có sẵn 26 ô
/// thường + 12 chậu hoa do designer đặt tay. Nếu đếm tổng thì ngay ô đầu tiên đã là
/// 50 × 1.35^26 ≈ 190 000 vàng — không ai mua nổi ô nào. Đếm theo save
/// `FARM_PLACED_BUILDINGS` nên "số đã mua" đúng bằng số ô người chơi tự bỏ tiền ra.
/// </summary>
public static class PlotPurchasePricing
{
    /// <summary>Hệ số nhân mỗi lần mua thêm một ô cùng loại.</summary>
    public const float GrowthPerPurchase = 1.35f;

    /// <summary>
    /// Sàn giá gốc. Cần vì `Chậu Đá Quý` có `goldPrice = 0`: nhân 0 với bao nhiêu
    /// cũng bằng 0, luỹ tiến sẽ không có tác dụng gì.
    /// </summary>
    public const int MinBasePrice = 50;

    /// <summary>
    /// itemID của những món trong Shop mà khi đặt xuống sẽ sinh ra một ô đất
    /// (prefab có <see cref="PlotController"/>).
    ///
    /// VÌ SAO khớp theo itemID chứ không dò `prefabToBuild.GetComponent&lt;PlotController&gt;()`:
    /// bảng giá bị tra lại mỗi frame khi Shop đang mở (UpdateUI) và khi Ghost đang di
    /// chuyển; `GetComponentInChildren` trên prefab mỗi frame là tốn vô ích. Danh sách
    /// này chỉ 5 dòng và thay đổi thì phải sửa cả asset lẫn shop nên khó lệch.
    /// </summary>
    private static readonly string[] PlotItemIds = { "100", "109", "110", "111", "112" };

    public static bool IsPlotItem(BaseItemData data)
    {
        if (data == null || string.IsNullOrEmpty(data.itemID)) return false;

        for (int i = 0; i < PlotItemIds.Length; i++)
            if (PlotItemIds[i] == data.itemID) return true;

        return false;
    }

    /// <summary>Giá của ô thứ (alreadyOwned + 1), làm tròn xuống mức chục gần nhất.</summary>
    public static int PriceFor(int baseGold, int alreadyOwned)
    {
        int b = Mathf.Max(MinBasePrice, baseGold);
        if (alreadyOwned <= 0) return b;

        float raw = b * Mathf.Pow(GrowthPerPurchase, alreadyOwned);

        // Làm tròn CHỤC để con số trên nút Mua luôn đọc được (170 chứ không phải 166).
        // Clamp trần: 100 triệu vàng là quá xa mọi ván chơi, chặn để không tràn int.
        return Mathf.Clamp(Mathf.RoundToInt(raw / 10f) * 10, b, 100_000_000);
    }

    /// <summary>
    /// Giá VÀNG thật phải trả cho <paramref name="data"/> ngay lúc này.
    /// Món không phải ô đất thì trả thẳng `goldPrice` trong asset — mọi nơi đang gọi
    /// `data.goldPrice` đều đổi sang gọi hàm này được mà không đổi hành vi.
    /// </summary>
    public static int EffectiveGoldPrice(BaseItemData data)
    {
        if (data == null) return 0;
        if (!IsPlotItem(data)) return data.goldPrice;

        int owned = PlacementManager.Instance != null
            ? PlacementManager.Instance.CountPlacedByItemId(data.itemID)
            : 0;

        return PriceFor(data.goldPrice, owned);
    }
}
