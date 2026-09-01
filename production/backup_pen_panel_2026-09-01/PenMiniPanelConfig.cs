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

    // ═══════════════════════════════════════════════════════════════════════════
    //  THỨC ĂN CAO CẤP — TÚI CÁM TỪ MÁY XAY THỨC ĂN  (thêm 20/08)
    //
    //  Ý TƯỞNG: ruộng → máy xay → túi cám vào kho → cho gia súc ăn.
    //  Chuồng vẫn ăn được nông sản THÔ (food1/food2) như cũ, nên save cũ không hỏng và
    //  người chơi mới không bị chặn ở cửa ải "phải mở máy xay trước".
    //  Túi cám là đường CAO CẤP: tốn công xay nhưng nuôi nhanh hơn + ra nhiều sản phẩm hơn.
    //
    //  BỎ TRỐNG premiumFoodItemId ⇒ chuồng này chưa có túi cám, ô thức ăn thứ 3 TỰ ẨN.
    //  Nhờ vậy bật/tắt từng chuồng chỉ bằng data, không phải sửa code.
    // ═══════════════════════════════════════════════════════════════════════════
    [Header("Thức ăn cao cấp (túi cám từ máy xay)")]
    [Tooltip("outputItemId của công thức trong MillConfig. VD: cam_ga / cam_heo / " +
             "co_tron_bo / cam_bo_sua. Để TRỐNG nếu chuồng này chưa dùng túi cám.")]
    public string premiumFoodItemId;

    [Tooltip("Icon túi cám hiện trên ô thức ăn thứ 3.")]
    public Sprite premiumFoodIcon;

    [Tooltip("Số túi cám cho một lượt nuôi. Cám đã cô đặc nhiều nông sản nên thường = 1, " +
             "ít hơn foodAmountPerFeed của nông sản thô.")]
    [Min(1)] public int premiumFoodAmountPerFeed = 1;

    [Tooltip("Nuôi NHANH gấp mấy lần khi cho ăn cám. 2 = chỉ mất một nửa thời gian. " +
             "Để 1 nếu không muốn thưởng tốc độ.")]
    [Min(1f)] public float premiumSpeedMultiplier = 2f;

    [Tooltip("CỘNG THÊM bao nhiêu sản phẩm chính khi cho ăn cám (áp cả sản phẩm thứ 2). " +
             "0 = không thưởng sản lượng.")]
    [Min(0)] public int premiumProductBonus = 1;

    [Tooltip("CỘNG THÊM bao nhiêu EXP khi cho ăn cám.")]
    [Min(0)] public int premiumExpBonus = 10;

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
