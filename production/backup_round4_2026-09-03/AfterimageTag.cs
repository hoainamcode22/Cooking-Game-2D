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
}
