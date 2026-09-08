using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Một loài cá. Tạo/sửa qua FishingDataSetupTool (đề xuất) hoặc tay trong Inspector.
    /// KHÔNG kế thừa BaseItemData vì cá không đi qua ShopManager; bán ở Quầy Cá riêng.
    /// </summary>
    [CreateAssetMenu(fileName = "Fish_", menuName = "Farm Game/Fishing/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Header("Định danh")]
        [Tooltip("Bắt buộc tiền tố fish_ (id 'ca' nằm trong danh sách xoá của kitchen/mission).")]
        public string fishId = "fish_";
        public string displayName = "Cá";
        public Sprite icon;

        [Header("Kinh tế (ĐỀ XUẤT, Sếp duyệt)")]
        [Min(1)] public int sellPrice = 20;
        public FishRarity rarity = FishRarity.Common;

        [Header("Điều kiện câu")]
        [Tooltip("Cần tối thiểu (1..4) mới có thể câu ra loài này.")]
        [Range(1, 4)] public int minRodTier = 1;
        [Tooltip("Trọng số xuất hiện trong bảng rút (càng lớn càng dễ ra). 0 = không bao giờ.")]
        [Min(0f)] public float spawnWeight = 10f;
        [Tooltip("Cấp người chơi tối thiểu để loài này xuất hiện (0/1 = luôn).")]
        [Min(0)] public int unlockLevel = 1;

        [Header("Hiển thị")]
        [Tooltip("Cân nặng ngẫu nhiên hiển thị trên toast, chỉ trang trí.")]
        public Vector2 weightKgRange = new Vector2(0.2f, 1.5f);
    }
}
