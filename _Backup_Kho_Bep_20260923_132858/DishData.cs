using System;
using System.Collections.Generic;
using UnityEngine;

public enum DishDifficulty
{
    Easy,
    Normal,
    Hard
}

[Serializable]
public class HintIngredientSlotData
{
    public string displayName;
    public Sprite icon;
}

[Serializable]
public class SeasoningTipData
{
    public string displayName;
    public Sprite icon;
    public string effectText;
}

[CreateAssetMenu(fileName = "Dish_", menuName = "Cooking/Dish Data")]
public class DishData : ScriptableObject
{
    [Header("Basic Info")]
    public string dishId;
    public string dishName;
    public string dishSubTitle;
    public Sprite dishSprite;
    public DishDifficulty difficulty = DishDifficulty.Normal;

    [Tooltip("Player level required to cook this dish (Demo L1-L10)")]
    public int unlockLevel = 5;

    [Header("Target Flavor")]
    public FlavorVector targetFlavor;

    [Header("Scoring - Required Ingredients")]
    public List<IngredientData> requiredIngredients;

    // ─────────────────────────────────────────────────────────────────────────
    //  THƯỞNG & GIÁ BÁN
    // ─────────────────────────────────────────────────────────────────────────
    // VÌ SAO phải có ba số này trên từng món thay vì tính bằng công thức trong code:
    // trước đây `CookingChallengeManager` cộng CỨNG `AddExp(20)` cho mọi món và 0 vàng.
    // Nấu "Phở bò tái" (5 nguyên liệu, cấp 9, cần thịt bò từ chuồng cấp 7) ăn đúng bằng
    // "Khoai tây chiên" (1 nguyên liệu, cấp 5) ⇒ không ai có lý do nấu món khó.
    // Để số ngay trên asset thì người cân bằng game sửa được mà không phải mở code.

    [Header("Rewards")]
    [Tooltip("EXP gốc khi nấu ĐẠT (70đ). Điểm cao hơn được nhân thêm — xem CookingChallengeManager.")]
    public int rewardExp = 20;

    [Tooltip("Vàng gốc khi nấu ĐẠT (70đ). Điểm cao hơn được nhân thêm.")]
    public int rewardGold = 0;

    // Bằng đúng `MarketPriceTable.GetBasePrice(dishId)`. KHÔNG được lệch: lệch là bán ở
    // chợ và bán ở kho ra hai số khác nhau, người chơi phát hiện ngay.
    [Tooltip("Giá bán 1 đĩa. Phải khớp MarketPriceTable.GetBasePrice(dishId).")]
    public int sellPrice = 0;
}