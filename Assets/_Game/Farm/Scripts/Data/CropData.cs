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
    public Vector3 sproutScale  = new Vector3(1f, 1f,   1f);
    public Vector3 growingScale = new Vector3(1f, 1.5f, 1f);
    public Vector3 readyScale   = new Vector3(1f, 2f,   1f);

    [Header("Visual")]
    public int displayCount = 4;

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



    public Sprite GetSprite(int stage)
    {
        if (stage == 0) return sproutSprite;
        if (stage == 1) return growingSprite;
        return readySprite;
    }
}