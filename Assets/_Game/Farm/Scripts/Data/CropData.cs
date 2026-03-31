using UnityEngine;

[CreateAssetMenu(fileName = "Crop_", menuName = "Farm/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    public string cropId;
    public string displayName;
    public Sprite icon;

    [Header("World Visual")]
    public Sprite sproutSprite;
    public Sprite growingSprite;
    public Sprite readySprite;

    [Header("Visual Tuning")]
    public float sproutScale = 1f;
    public float growingScale = 1f;
    public float readyScale = 1f;

    public Vector2 sproutOffset = Vector2.zero;
    public Vector2 growingOffset = Vector2.zero;
    public Vector2 readyOffset = Vector2.zero;

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

    [Header("Progression")]
    public int expReward = 5;

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


    public Sprite GetStageSprite(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (progress01 >= 1f)
        {
            if (readySprite != null) return readySprite;
            if (growingSprite != null) return growingSprite;
            if (sproutSprite != null) return sproutSprite;
            return icon;
        }

        if (progress01 < 0.5f)
        {
            if (sproutSprite != null) return sproutSprite;
            if (growingSprite != null) return growingSprite;
            if (readySprite != null) return readySprite;
            return icon;
        }

        if (growingSprite != null) return growingSprite;
        if (sproutSprite != null) return sproutSprite;
        if (readySprite != null) return readySprite;
        return icon;
    }



    public float GetStageScale(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (progress01 >= 1f) return Mathf.Max(0.01f, readyScale);
        if (progress01 < 0.5f) return Mathf.Max(0.01f, sproutScale);
        return Mathf.Max(0.01f, growingScale);
    }

    public Vector2 GetStageOffset(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (progress01 >= 1f) return readyOffset;
        if (progress01 < 0.5f) return sproutOffset;
        return growingOffset;
    }
}