using UnityEngine;

/// <summary>
/// Marker RỖNG: gắn lên NPC cảnh KHÔNG có script di chuyển riêng
/// (bà lão hàng rông / quân nhân / nhân viên tàu — con của NPC_Villagers trong SCN_Farm)
/// để <see cref="AfterimageBootstrap"/> coi object này là mục tiêu afterimage
/// bất kể tên class. Gắn bằng tay trong Inspector hoặc menu
/// Tools/Farm Game/Afterimage/Gắn tag cho NPC cảnh (NPC_Villagers).
/// </summary>
[DisallowMultipleComponent]
public class AfterimageTag : MonoBehaviour
{
    [Tooltip("true: nhả ghost cho mọi SpriteRenderer con đang nhìn thấy (tối đa 6/nhịp); " +
             "false: chỉ SpriteRenderer chính.")]
    public bool includeChildren = true;

    [Tooltip("Ngưỡng tốc độ RIÊNG (world unit/giây) để bắt đầu nhả bóng mờ. " +
             "0 = dùng minSpeed chung của AfterimageConfig (60 — tinh cho người đi bộ 420 u/s). " +
             "NPC cảnh đi lững thững ~20-40 u/s thì đặt 15-25 mới thấy bóng.")]
    public float minSpeedOverride = 0f;
}
