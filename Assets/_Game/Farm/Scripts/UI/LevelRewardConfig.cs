using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa thông tin phần thưởng khi người chơi lên một level cụ thể.
/// Tạo asset: Right-click → FarmGame → Level Reward Config
/// Một asset cho mỗi level (Level 2, Level 3, ...).
/// </summary>
[CreateAssetMenu(fileName = "LevelReward_L2", menuName = "FarmGame/Level Reward Config")]
public class LevelRewardConfig : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("Popup sẽ hiển thị khi người chơi đạt đúng level này")]
    public int levelReached = 2;

    [Header("Phần thưởng tặng ngay khi nhận")]
    public int giftGold;
    public int giftGems;

    [System.Serializable]
    public class ItemGift
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        [Min(1)] public int amount = 1;
    }

    [Tooltip("Vật phẩm tặng thêm vào kho khi nhấn Nhận Quà")]
    public List<ItemGift> giftItems = new List<ItemGift>();

    // ─────────────────────────────────────────────────────────────────────────
    //  VẬT PHẨM MỞ KHÓA
    // ─────────────────────────────────────────────────────────────────────────
    //  VÌ SAO PHẢI THÊM unlockEntries?
    //  ---------------------------------------------------------------------
    //  Popup lên cấp (Township-style) có một dải ô tròn "vừa mở khoá".
    //  Trước đây dữ liệu duy nhất mô tả các ô đó là `unlockDescriptions`
    //  (List<string>) — THUẦN CHỮ, KHÔNG CÓ Sprite. Vì vậy UnlockSlotUI.Setup()
    //  luôn nhận icon = null → mọi ô tròn hiện ra TRẮNG TRƠN, chỉ còn nhãn NEW.
    //
    //  `unlockEntries` ghép mỗi dòng mô tả với ĐÚNG 1 Sprite thật của game
    //  (lấy từ CropData / BuildingData / DecorData / InventoryItemData hoặc
    //  sprite art), nên UI chỉ cần đọc icon là hiển thị được ngay.
    //
    //  KHÔNG XOÁ `unlockDescriptions`:
    //    • LevelUpPopupUI.cs (dòng ~267) vẫn dùng để ghép chuỗi "Mở khóa: ..."
    //    • LevelUpRewardDataSetupTool.cs (dòng ~455) vẫn ghi vào field này
    //  → Giữ cả hai, và dùng GetUnlockEntries() làm CẦU NỐI tương thích ngược.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Một mục "vừa mở khoá" = 1 ô tròn trong popup: nhãn + icon thật.</summary>
    [System.Serializable]
    public class UnlockEntry
    {
        [Tooltip("Nhãn hiển thị, ví dụ: \"Mở khóa Khoai tây\"")]
        public string label;

        [Tooltip("Icon THẬT lấy từ thư viện asset của game. Null → ô sẽ trống.")]
        public Sprite icon;

        public UnlockEntry() { }

        public UnlockEntry(string label, Sprite icon)
        {
            this.label = label;
            this.icon  = icon;
        }
    }

    [Header("Hiển thị trong Popup — Vật phẩm mở khóa (CÓ ICON)")]
    [Tooltip("Danh sách ô mở khoá kèm icon thật. Điền tự động bằng " +
             "Tools/Farm/Điền Icon Unlock (Level Reward), hoặc gán tay ở đây.\n" +
             "Nếu để RỖNG, UI sẽ tự suy từ unlockDescriptions bên dưới (icon = null).")]
    public List<UnlockEntry> unlockEntries = new List<UnlockEntry>();

    [Header("Hiển thị trong Popup — Vật phẩm mở khóa (CHỈ CHỮ, LEGACY)")]
    [Tooltip("Mô tả ngắn cho mỗi thứ được mở khóa ở level này (chỉ hiển thị, không tự unlock). " +
             "Giữ lại để tương thích code cũ — nguồn hiển thị chính bây giờ là unlockEntries.")]
    public List<string> unlockDescriptions = new List<string>();

    [Header("Hint Text (tùy chọn)")]
    [TextArea(1, 4)]
    [Tooltip("Lời gợi ý hiển thị ở cuối popup. Để trống nếu không cần.")]
    public string hintText = "";

    // ─── API cho tầng UI (DEV-B gọi hàm này) ─────────────────────────────────

    /// <summary>
    /// Trả về danh sách unlock để UI hiển thị (mỗi phần tử = 1 ô tròn).
    /// Ưu tiên <see cref="unlockEntries"/>; nếu rỗng thì tự suy từ
    /// <see cref="unlockDescriptions"/> với icon = null để TƯƠNG THÍCH NGƯỢC
    /// (asset chưa được điền icon vẫn hiện đủ số ô, chỉ là ô trống).
    ///
    /// LUÔN trả về list khác null (có thể rỗng nếu level không mở gì).
    /// List trả về là bản sao — sửa nó không ảnh hưởng asset.
    /// </summary>
    public List<UnlockEntry> GetUnlockEntries()
    {
        var result = new List<UnlockEntry>();

        // 1) Nguồn chính: unlockEntries (có icon thật)
        if (unlockEntries != null && unlockEntries.Count > 0)
        {
            // KHÔNG lọc gì ở dải ô tròn — đây là dải chính, giữ nguyên đủ mục.
            // Việc chống hiện trùng icon được xử lý ở phía DẢI QUÀ, xem GetGiftItemsToShow().
            foreach (var e in unlockEntries)
            {
                if (e == null) continue;                               // bỏ phần tử null do designer xoá dở
                if (string.IsNullOrWhiteSpace(e.label) && e.icon == null) continue; // bỏ dòng rỗng hoàn toàn
                result.Add(e);
            }

            // Có unlockEntries thì LUÔN trả về kết quả của nó, KỂ CẢ KHI RỖNG.
            // Không rơi xuống nhánh fallback: fallback sẽ đọc lại unlockDescriptions và
            // dựng đủ số ô với icon = null → 9 ô tròn TRẮNG TRƠN.
            // Fallback chỉ dành cho asset cũ chưa từng điền unlockEntries.
            return result;
        }

        // 2) Fallback: suy từ unlockDescriptions (icon = null)
        if (unlockDescriptions != null)
        {
            foreach (string d in unlockDescriptions)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                result.Add(new UnlockEntry(d, null));
            }
        }

        return result;
    }

    /// <summary>
    /// Số ô mở khoá cần hiển thị ở level này. UI dùng để ẩn các ô thừa
    /// (prefab dựng cứng 9 ô nhưng L5 chỉ có 3 mục).
    /// </summary>
    public int UnlockCount => GetUnlockEntries().Count;

    // ─────────────────────────────────────────────────────────────────────────
    //  CHỐNG HIỆN TRÙNG ICON — lọc ở DẢI QUÀ, không lọc ở dải ô tròn
    // ─────────────────────────────────────────────────────────────────────────
    //  Popup có hai dải: dải Ô TRÒN "vừa mở khoá" (unlockEntries) và dải QUÀ
    //  (giftItems, kèm số lượng). Nhiều asset gán CÙNG một Sprite cho cả hai —
    //  LevelReward_L2 vừa tặng "Hạt Ngô x3" vừa có ô tròn "Mở khóa hạt Ngô" dùng
    //  đúng sprite đó → hiện HAI icon bắp, người chơi tưởng là hai thứ khác nhau.
    //
    //  Bỏ bên nào? Bỏ Ô QUÀ. Dải ô tròn là phần chính của popup kiểu Township và
    //  đã nói đủ ý "bạn vừa mở khoá thứ này"; còn dòng "Phần thưởng: 🪙+150 💎+2"
    //  vẫn nằm đó nên người chơi không mất thông tin gì.
    //
    //  ⚠ CHỈ ẢNH HƯỞNG HIỂN THỊ. Vật phẩm vẫn được TẶNG ĐẦY ĐỦ vào kho —
    //  LevelUpPopupUI khi bấm "Nhận" duyệt thẳng `giftItems`, không dùng hàm này.

    /// <summary>Danh sách quà CẦN VẼ Ô — bỏ những món đã hiện ở dải ô tròn mở khoá.</summary>
    public List<ItemGift> GetGiftItemsToShow()
    {
        var result = new List<ItemGift>();
        if (giftItems == null) return result;

        var iconOTron = new HashSet<Sprite>();
        foreach (var e in GetUnlockEntries())
            if (e != null && e.icon != null) iconOTron.Add(e.icon);

        foreach (var g in giftItems)
        {
            if (g == null) continue;
            if (g.icon != null && iconOTron.Contains(g.icon)) continue;   // đã hiện ở ô tròn
            result.Add(g);
        }
        return result;
    }

    /// <summary>
    /// Nhãn cho dòng chữ "Mở khóa: …" dưới popup.
    ///
    /// VÌ SAO KHÔNG ĐỌC THẲNG <see cref="unlockDescriptions"/>: danh sách chữ đó là
    /// bản sao TAY của <see cref="unlockEntries"/>, hai bên rất dễ lệch nhau khi designer
    /// sửa một bên mà quên bên kia — popup sẽ kể tên một thứ không có ô tròn nào.
    /// Lấy nhãn từ chính danh sách dựng ô để hai phần luôn khớp.
    /// </summary>
    public List<string> GetUnlockLabels()
    {
        var result = new List<string>();

        // Cùng nguyên tắc với GetUnlockEntries(): asset ĐÃ có unlockEntries thì trả về
        // đúng kết quả đã lọc, KỂ CẢ RỖNG. Nếu để rơi xuống fallback thì dòng chữ sẽ kể
        // đủ tên trong khi không còn ô tròn nào — đúng cái mâu thuẫn cần tránh.
        if (unlockEntries != null && unlockEntries.Count > 0)
        {
            foreach (var e in GetUnlockEntries())
                if (!string.IsNullOrWhiteSpace(e.label)) result.Add(e.label);
            return result;
        }

        // unlockEntries rỗng hoàn toàn → asset cũ chỉ có chữ, dùng nguyên bản.
        if (unlockDescriptions != null)
            foreach (string d in unlockDescriptions)
                if (!string.IsNullOrWhiteSpace(d)) result.Add(d);

        return result;
    }
}
