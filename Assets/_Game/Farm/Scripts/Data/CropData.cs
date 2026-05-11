using UnityEngine;

public enum CropCategory { Normal, Flower }

[CreateAssetMenu(fileName = "Crop_", menuName = "Farm/Crop Data")]
public class CropData : BaseItemData   // ← Đổi từ ScriptableObject sang BaseItemData
{
    // ── Thuộc tính tương thích ngược ─────────────────────────────────────────
    // Toàn bộ code cũ dùng cropData.displayName / cropData.icon / cropData.seedBuyGold
    // VẪN HOẠT ĐỘNG BÌNH THƯỜNG qua các property này — không cần sửa bất kỳ chỗ nào khác
    public string displayName => itemName;
    public Sprite icon        => itemIcon;
    public int    seedBuyGold => goldPrice;

    // ── Category ─────────────────────────────────────────────────────────────
    [Header("Category")]
    public CropCategory cropCategory = CropCategory.Normal;

    // ── Identity ─────────────────────────────────────────────────────────────
    [Header("Identity")]
    public string cropId;
    // itemName  → kế thừa từ BaseItemData (thay thế displayName cũ)
    // itemIcon  → kế thừa từ BaseItemData (thay thế icon cũ)
    // itemID    → kế thừa từ BaseItemData — điền bằng seedItemId (xem gợi ý bên dưới)

    // ── Plant FX ─────────────────────────────────────────────────────────────
    [Header("Plant FX")]
    public Sprite plantSeedFxIcon;  // Icon hạt nhỏ cho hiệu ứng mưa hạt giống

    [Header("Harvest FX")]
    public Sprite harvestIcon;      // Icon bay về kho khi thu hoạch

    // ── World Visual ──────────────────────────────────────────────────────────
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

    // ── Inventory ─────────────────────────────────────────────────────────────
    [Header("Inventory")]
    public string seedItemId;       // ID hạt giống trong kho (ví dụ: "seed_lua")
    public string harvestItemId;    // ID nông sản thu hoạch trong kho (ví dụ: "lua")

    // ── Economy ───────────────────────────────────────────────────────────────
    [Header("Economy")]
    public IngredientTier tier;
    public int unlockLevel;
    public int growSeconds  = 30;
    // goldPrice → kế thừa từ BaseItemData (thay thế seedBuyGold cũ, data cũ tự migrate)
    public int sellGold;
    public int harvestAmount = 1;
    public int plantCost     = 1;

    [Header("Progression")]
    public int expReward = 5;

    // ── Cooking Vector ────────────────────────────────────────────────────────
    [Header("Cooking Vector")]
    public int sweet;
    public int spicy;
    public int sour;
    public int umami;
    public int texture;

    // ── Flags ────────────────────────────────────────────────────────────────
    [Header("Flags")]
    public bool canBuyInSeedShop       = true;
    public bool canDropFromAds         = false;
    public bool canAppearInRareMarket  = false;

    // ── Editor Helper ────────────────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tự động điền itemID từ seedItemId nếu itemID còn trống
        // → Giúp không phải điền tay trên từng asset
        if (string.IsNullOrEmpty(itemID) && !string.IsNullOrEmpty(seedItemId))
            itemID = seedItemId;
    }
#endif

    // ── Methods ───────────────────────────────────────────────────────────────
    public Sprite GetStageSprite(float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);

        if (progress01 >= 1f)
        {
            if (readySprite   != null) return readySprite;
            if (growingSprite != null) return growingSprite;
            if (sproutSprite  != null) return sproutSprite;
            return itemIcon;
        }

        if (progress01 < 0.5f)
        {
            if (sproutSprite  != null) return sproutSprite;
            if (growingSprite != null) return growingSprite;
            if (readySprite   != null) return readySprite;
            return itemIcon;
        }

        if (growingSprite != null) return growingSprite;
        if (sproutSprite  != null) return sproutSprite;
        if (readySprite   != null) return readySprite;
        return itemIcon;
    }

    public Sprite GetSprite(int stage)
    {
        if (stage == 0) return sproutSprite;
        if (stage == 1) return growingSprite;
        return readySprite;
    }
}
