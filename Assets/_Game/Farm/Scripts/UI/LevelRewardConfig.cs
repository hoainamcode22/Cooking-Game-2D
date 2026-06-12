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

    [Header("Hiển thị trong Popup — Vật phẩm mở khóa")]
    [Tooltip("Mô tả ngắn cho mỗi thứ được mở khóa ở level này (chỉ hiển thị, không tự unlock)")]
    public List<string> unlockDescriptions = new List<string>();

    [Header("Hint Text (tùy chọn)")]
    [TextArea(1, 4)]
    [Tooltip("Lời gợi ý hiển thị ở cuối popup. Để trống nếu không cần.")]
    public string hintText = "";
}
