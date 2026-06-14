using UnityEngine;

public enum RewardType { Coin, Diamond }

/// <summary>
/// Loại sự kiện gameplay mà 1 mission theo dõi.
/// Key tiến độ chuẩn: "{MissionEventType}:{targetItemId}" hoặc "{MissionEventType}:*" (mọi item).
/// </summary>
public enum MissionEventType
{
    HarvestItem,          // thu hoạch nông sản (PlotController.Harvest)
    DeliverOrder,         // giao đơn hàng làng (VillageOrderManager.DeliverOrder)
    CookDish,             // nấu món thành công (CookingChallengeManager)
    FeedAnimal,           // cho vật nuôi ăn (PenMiniPanelUI.TryFeed)
    CollectAnimalProduct, // thu sản phẩm chuồng (PenMiniPanelUI.TryHarvest)
    BuyShopItem,          // mua trong Shop (ShopItemUI.BuyItem)
    BuySeed,              // mua hạt giống (ShopItemUI / MarketManager)
    ReachLevel,           // đạt cấp X (PlayerProgressManager.OnLevelChanged)
    PlantCrop             // trồng cây (PlotController.TryPlant)
}

[CreateAssetMenu(fileName = "MissionData", menuName = "Game/Mission Data")]
public class MissionData : ScriptableObject
{
    public Sprite missionIcon;
    public string missionName;
    public int targetAmount;
    public Sprite rewardIcon;
    public int rewardAmount;
    public RewardType rewardType;

    [Header("Mission Logic (L1-L10)")]
    [Tooltip("ID duy nhất để lưu tiến độ/claimed. Rỗng = dùng tên asset.")]
    public string missionId = "";

    [Tooltip("Level người chơi tối thiểu để mission hiện trong popup.")]
    public int requiredLevel = 1;

    [Tooltip("Loại sự kiện gameplay được theo dõi.")]
    public MissionEventType eventType = MissionEventType.HarvestItem;

    [Tooltip("Lọc theo itemId cụ thể (vd: rice, egg, 108). Rỗng = mọi item của loại sự kiện.")]
    public string targetItemId = "";

    [Tooltip("true = nhiệm vụ ngày (tiến độ reset mỗi ngày, không hiện trong list chính).")]
    public bool isDaily = false;

    /// <summary>missionId nếu có, fallback tên asset — dùng làm key lưu claimed/tiến độ.</summary>
    public string MissionId => string.IsNullOrEmpty(missionId) ? name : missionId;
}
