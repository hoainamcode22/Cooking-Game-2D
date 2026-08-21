using UnityEngine;

/// <summary>
/// MỘT NGUYÊN LIỆU của công thức xay.
/// `itemId` phải TRÙNG id trong kho (WarehouseManager) — nếu lệch thì kiểm tra nguyên liệu
/// luôn báo thiếu dù trong kho có hàng.
/// </summary>
[System.Serializable]
public class MillIngredient
{
    [Tooltip("Id vật phẩm trong kho. PHẢI trùng itemId mà WarehouseManager đang lưu.")]
    public string itemId;

    [Tooltip("Số lượng cần cho MỘT lượt xay.")]
    public int amount = 1;

    [Tooltip("Icon hiện trên chip nguyên liệu của card công thức (chip 'x3' trong video).")]
    public Sprite icon;
}

/// <summary>
/// CÔNG THỨC MÁY XAY THỨC ĂN — một dòng trong danh sách "CÔNG THỨC" bên trái popup.
///
/// Tạo asset: chuột phải trong Project → Create → Farm → Mill → Recipe.
///
/// ══ ĐỐI CHIẾU VỚI VIDEO / full_mill_ui.html ══
///   displayName      → .recipe-name    "Cám cho gà"
///   BrewTimeLabel    → .recipe-time    "Ủ 2p00"   (prefix "Ủ " do UI ghép, xem MillRecipeCardUI)
///   animalTag        → .animal-tag     "Gà" ở góc trên phải card
///   ingredients      → .cost-row       tối đa 2 chip được wire sẵn (imgIng1/imgIng2)
///
/// LƯU Ý VỀ unlockLevel: card khoá trong video ghi "Mở ở cấp 14" — con số đó chính là
/// `unlockLevel`, KHÔNG phải hằng số trong code.
/// </summary>
[CreateAssetMenu(fileName = "MillRecipe_", menuName = "Farm/Mill/Recipe")]
public class MillRecipeData : ScriptableObject
{
    [Header("Nhận dạng")]
    [Tooltip("Id nội bộ, dùng để LƯU trạng thái slot xuống PlayerPrefs. " +
             "ĐỔI id sau khi game đã phát hành = slot đang xay của người chơi bị mất. " +
             "Đặt kiểu snake_case: cam_ga, cam_heo, co_tron_bo.")]
    public string recipeId;

    [Tooltip("Tên hiện cho người chơi: \"Cám cho gà\".")]
    public string displayName;

    [Tooltip("Nhãn con vật ở góc card: \"Gà\", \"Heo\", \"Bò\". KHÔNG kèm emoji.")]
    public string animalTag;

    [Header("Hình ảnh")]
    [Tooltip("Icon sản phẩm — đĩa tròn giữa card và trong slot đang xay.")]
    public Sprite icon;

    [Tooltip("Icon nhỏ cạnh nhãn con vật.")]
    public Sprite animalBadgeIcon;

    [Header("Thời gian ủ")]
    [Tooltip("Số PHÚT để xay xong một lượt. Cho phép số thập phân: 2.5 = 2 phút 30 giây.")]
    public float brewMinutes = 2f;

    [Header("Đầu ra")]
    [Tooltip("Id vật phẩm cộng vào kho khi bấm THU.")]
    public string outputItemId;

    [Tooltip("Số lượng cộng vào kho mỗi lượt.")]
    public int outputAmount = 1;

    [Header("Đầu vào")]
    [Tooltip("Nguyên liệu bị trừ khi bấm XAY NGAY. UI wire sẵn 2 chip; " +
             "khai nhiều hơn 2 thì chip thứ 3+ không có chỗ hiện (video cũng chỉ vẽ 2–3).")]
    public MillIngredient[] ingredients;

    [Header("Điều kiện")]
    [Tooltip("Cấp nông trại tối thiểu để dùng công thức. 1 = mở từ đầu.")]
    public int unlockLevel = 1;

    /// <summary>
    /// Thời gian ủ ĐÃ ĐỊNH DẠNG theo đúng kiểu trong video: <c>"2p00"</c>, <c>"10p00"</c>.
    /// Dùng chữ <c>p</c> (phút) chứ KHÔNG dùng dấu hai chấm — đây là quy ước hiển thị của
    /// bản thiết kế, đừng "sửa lại cho đúng chuẩn" thành "2:00".
    /// Giây luôn 2 chữ số; phút KHÔNG đệm 0 (video ghi "2p00" chứ không "02p00").
    /// </summary>
    public string BrewTimeLabel
    {
        get
        {
            int tongGiay = Mathf.Max(0, Mathf.RoundToInt(brewMinutes * 60f));
            return MillTimeFormat.PhutGiay(tongGiay);
        }
    }

    /// <summary>Tổng số giây ủ — dùng cho đồng hồ đếm ngược của slot.</summary>
    public float BrewSeconds => Mathf.Max(1f, brewMinutes * 60f);
}

/// <summary>
/// ĐỊNH DẠNG THỜI GIAN CHUNG cho toàn bộ popup máy xay: <c>"1p58"</c>, <c>"3p53"</c>, <c>"10p00"</c>.
///
/// ══ VÌ SAO TÁCH RA STATIC RIÊNG ══
/// Cả `MillRecipeData.BrewTimeLabel` (chạy 1 lần) và `MillSlotUI` (chạy mỗi giây, 5 slot)
/// đều cần đúng một định dạng. Nhét vào một chỗ để không bao giờ lệch nhau, và để chỗ
/// nóng có thể tối ưu một lần cho tất cả.
///
/// ══ CHỐNG RÁC MỖI FRAME (cạm bẫy #3) ══
/// Hàm này CÓ cấp phát string. Nó được gọi từ Update nên `MillSlotUI` chỉ gọi lại khi
/// SỐ GIÂY NGUYÊN đổi (xem `_giayDangHien` trong MillSlotUI) ⇒ tối đa 1 string nhỏ /giây /slot,
/// không phải 60. Đừng gọi hàm này vô điều kiện trong Update.
/// </summary>
public static class MillTimeFormat
{
    /// <summary>Đổi tổng số giây thành nhãn kiểu video: 118 → "1p58", 0 → "0p00".</summary>
    public static string PhutGiay(int tongGiay)
    {
        if (tongGiay < 0) tongGiay = 0;

        int phut = tongGiay / 60;
        int giay = tongGiay % 60;

        // Giây đệm 2 chữ số, phút để nguyên — khớp "2p00" / "10p00" trong full_mill_ui.html.
        return phut + "p" + (giay < 10 ? "0" : "") + giay;
    }

    /// <summary>Bản nhận float (làm tròn LÊN) — dùng cho đếm ngược để không hiện "0p00" khi còn 0.4s.</summary>
    public static string PhutGiayTuGiay(float giayConLai)
    {
        return PhutGiay(Mathf.CeilToInt(Mathf.Max(0f, giayConLai)));
    }
}
