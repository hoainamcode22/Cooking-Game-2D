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

    // ── BỘ 5 STAGE (đội vẽ giao 2026-08-27) ───────────────────────────────────
    // hạt → mầm → lá → nụ/ra hoa → chín. Sprite pivot Bottom-Center, PPU 100.
    //
    // 3 field cũ ở TRÊN GIỮ NGUYÊN, không xoá: cây nào chưa gán bộ mới thì
    // StageCount tự trả về 3 và toàn bộ hành vi y như trước (không vỡ save, không
    // vỡ cây/hoa chưa chuyển). Có đủ mảng mới → tự chạy 5 stage.
    [Header("World Visual — bộ 5 stage (2026-08-27)")]
    public Sprite[]  stageSprites = new Sprite[0];
    public Vector3[] stageScales  = new Vector3[0];

    [Header("Visual")]
    public int displayCount = 4;

    // ── Inventory ─────────────────────────────────────────────────────────────
    [Header("Inventory")]
    public string seedItemId;       // ID hạt giống trong kho (ví dụ: "seed_lua")
    public string harvestItemId;    // ID nông sản thu hoạch trong kho (ví dụ: "lua")

    // ── Economy ───────────────────────────────────────────────────────────────
    // C5 — đã xoá `public IngredientTier tier;`: không một dòng code nào đọc nó, và giá
    // trị trên 21 asset thì lộn xộn (cây cấp 1 mang tier Epic, cây cấp 10 mang tier Basic).
    // Field vô nghĩa mà hiện trong Inspector là bẫy: người cân bằng game tưởng nó có tác dụng.
    [Header("Economy")]
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
    // C3 — đã xoá `canDropFromAds`: dự án KHÔNG có hệ quảng cáo nào (0 SDK, 0 nơi gọi),
    //      nhưng cờ này đang bật cho 11 asset ⇒ ai đọc data cũng tưởng có rơi hạt từ ads.
    // C4 — đã xoá `canAppearInRareMarket`: 21/21 asset đều = 0 và không nơi nào đọc.
    //      Chợ hiếm đã được thay bằng `MarketPriceTable.MarketEnabled` + `MarketRefreshTimer`.
    [Header("Flags")]
    public bool canBuyInSeedShop       = true;

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

    /// <summary>Đã gán đủ bộ stage mới chưa (mọi ô trong stageSprites đều có sprite).</summary>
    public bool HasStageSet
    {
        get
        {
            if (stageSprites == null || stageSprites.Length < 2) return false;
            for (int i = 0; i < stageSprites.Length; i++)
                if (stageSprites[i] == null) return false;
            return true;
        }
    }

    /// <summary>Số stage thực tế: có bộ mới → theo độ dài mảng (5); chưa có → 3 như cũ.</summary>
    public int StageCount => HasStageSet ? stageSprites.Length : 3;

    /// <summary>progress 0..1 → index stage. Chia đều; progress = 1 luôn ra stage cuối.</summary>
    public int StageFromProgress(float progress01)
    {
        int n = StageCount;
        return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress01) * n), 0, n - 1);
    }

    /// <summary>Scale của stage: bộ mới đọc stageScales, chưa có thì về 3 field cũ.</summary>
    public Vector3 GetScale(int stage)
    {
        if (HasStageSet && stageScales != null && stageScales.Length > 0)
        {
            Vector3 s = stageScales[Mathf.Clamp(stage, 0, stageScales.Length - 1)];
            return s == Vector3.zero ? Vector3.one : s;
        }
        if (stage <= 0) return sproutScale;
        if (stage == 1) return growingScale;
        return readyScale;
    }

    public Sprite GetStageSprite(float progress01)
    {
        if (HasStageSet) return GetSprite(StageFromProgress(progress01));

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
        if (HasStageSet)
            return stageSprites[Mathf.Clamp(stage, 0, stageSprites.Length - 1)];

        if (stage <= 0) return sproutSprite;
        if (stage == 1) return growingSprite;
        return readySprite;
    }
}
