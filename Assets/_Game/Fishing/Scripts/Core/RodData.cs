using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Cần câu. Kế thừa BaseItemData để có sẵn itemID/itemName/itemIcon/goldPrice/diamondPrice
    /// (diamondPrice > 0 = mua bằng gem, khi đó goldPrice bị bỏ qua, giống luật ShopItemUI).
    /// Mua ở Quầy Cá (FishCounterPopupUI), KHÔNG qua ShopManager.
    /// </summary>
    [CreateAssetMenu(fileName = "Rod_", menuName = "Farm Game/Fishing/Rod Data")]
    public class RodData : BaseItemData
    {
        [Header("Bậc cần")]
        [Range(1, 4)] public int tier = 1;
        [Tooltip("Cấp mở khoá mua (shop đọc field tên 'unlockLevel' qua reflection, giữ đúng tên).")]
        [Min(1)] public int unlockLevel = 1;

        [Header("Độ bền (ĐỀ XUẤT)")]
        [Tooltip("Số lần QUĂNG trước khi hỏng. Hỏng = phải mua cần mới.")]
        [Min(1)] public int durabilityCasts = 20;

        [Header("Hiệu suất (ĐỀ XUẤT)")]
        [Tooltip("Cộng thẳng vào xác suất bắt được khi THU đúng lúc (0..1).")]
        [Range(0f, 1f)] public float catchChanceBonus = 0f;
        [Tooltip("Nhân thời gian chờ cá cắn (0.5 = nhanh gấp đôi).")]
        [Range(0.2f, 2f)] public float biteWaitMultiplier = 1f;
        [Tooltip("Nhân trọng số cá Rare/Epic (1 = không đổi).")]
        [Range(1f, 5f)] public float rareWeightMultiplier = 1f;
        [Tooltip("Cửa sổ cắn dài thêm (giây) so với FishingConfig.biteWindowSeconds.")]
        [Range(0f, 2f)] public float biteWindowBonusSeconds = 0f;

        public bool IsGemRod => diamondPrice > 0;
    }
}
