using UnityEngine;

[CreateAssetMenu(fileName = "PenConfig_", menuName = "Farm/Pen Mini Panel Config")]
public class PenMiniPanelConfig : ScriptableObject
{
    [Header("Identity")]
    public string penId; // "pen_01" .. "pen_04" — dùng làm key PlayerPrefs
    public string penName; // "Chuồng Bò", "Chuồng Heo", "Chuồng Gà", "Chuồng Bò Sữa"

    [Header("Thức ăn")]
    public string food1ItemId;   // "rice"
    public string food2ItemId;   // "ngo" / "bapcai" / "carot"
    public Sprite food1Icon;
    public Sprite food2Icon;

    [Header("Sản phẩm")]
    public string productItemId;          // "beef" / "pork" / "chicken_meat" / "milk"
    public string secondProductItemId;    // "egg" — chỉ gà, để trống nếu không dùng
    public Sprite productIcon;
    [Min(1)] public int productAmount = 1;
    [Min(1)] public int secondProductAmount = 1;
    public Sprite secondProductIcon;      // icon trứng

    [Header("Rổ thu hoạch")]
    public Sprite basketIcon;

    /// <summary>
    /// E1 — Số ĐƠN VỊ thức ăn cho một lượt nuôi.
    ///
    /// VÌ SAO phải có field này: trước đây `TryFeed` cứng `RemoveItem(foodItemId, 1)`.
    /// Một hạt lúa (7 vàng) thả vào chuồng gà trả về 4 thịt gà + 4 trứng = 320 vàng
    /// trong 30 giây, tức lãi/giây gấp ~70 lần ruộng tốt nhất. Từ cấp 2 trồng trọt
    /// thành vô nghĩa. Nay: gà 2 · heo 2 · bò 3 · bò sữa 3 · máy 1.
    /// </summary>
    [Header("Thời gian & EXP")]
    [Min(1)] public int foodAmountPerFeed = 1;

    /// <summary>
    /// Thời gian nuôi, GIÂY THẬT — cùng đơn vị với `CropData.growSeconds`
    /// sau khi `FarmManager.realTimeMultiplier` về 1.0 (quyết định #6).
    /// </summary>
    public float feedDurationSeconds = 120f;
    public int expReward = 10;
}
