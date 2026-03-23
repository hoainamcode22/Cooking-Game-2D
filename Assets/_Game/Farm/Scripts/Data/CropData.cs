using UnityEngine;

[CreateAssetMenu(fileName = "Crop_", menuName = "Farm/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropId;
    public string displayName;
    public Sprite icon;

    [Header("World Visual")]
    public Sprite sproutSprite;        // Giai đoạn mới gieo / mầm
    public Sprite growingSprite;       // Giai đoạn trưởng thành
    public Sprite readySprite;         // Giai đoạn chín / sẵn sàng thu hoạch

    [Header("Inventory")]
    public string seedItemId;
    public string harvestItemId;

    [Header("Economy")]
    public IngredientTier tier;
    public int unlockLevel;
    public int growSeconds = 30;
    public int seedBuyGold;
    public int sellGold;
    public int harvestAmount = 1;

    [Header("Cooking Vector")]
    public int sweet;
    public int spicy;
    public int sour;
    public int umami;
    public int texture;

    [Header("Flags")]
    public bool canBuyInSeedShop = true;
    public bool canDropFromAds = false;
    public bool canAppearInRareMarket = false;

    // Trả sprite theo 3 mốc rõ ràng:
    // 0%  -> < 50%  : mầm
    // 50% -> < 100% : trưởng thành
    // 100%          : thu hoạch / chín
    public Sprite GetStageSprite(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        // Đã hoàn tất thời gian grow -> dùng sprite chín
        if (progress01 >= 1f)
        {
            if (readySprite != null) return readySprite;
            if (growingSprite != null) return growingSprite;
            if (sproutSprite != null) return sproutSprite;
            return icon;
        }

        // Nửa đầu thời gian -> dùng sprite mầm
        if (progress01 < 0.5f)
        {
            if (sproutSprite != null) return sproutSprite;
            if (growingSprite != null) return growingSprite;
            if (readySprite != null) return readySprite;
            return icon;
        }

        // Nửa sau thời gian -> dùng sprite trưởng thành
        if (growingSprite != null) return growingSprite;
        if (sproutSprite != null) return sproutSprite;
        if (readySprite != null) return readySprite;
        return icon;
    }
}