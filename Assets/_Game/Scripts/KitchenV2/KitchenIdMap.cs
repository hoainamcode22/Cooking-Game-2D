using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bảng dịch 2 chiều giữa itemId NÔNG TRẠI (khoá trong KitchenTransferManager, vd
/// "chicken_meat", "nam") và id NGUYÊN LIỆU BẾP (IngredientData.id, vd "chicken",
/// "mushroom"). KHÔNG đổi bất kỳ id nào (luật dự án) — chỉ dịch khi đối chiếu.
///
/// Nguồn sự thật DUY NHẤT: CookingBoot.cookingInventoryItems (InventoryItemData.itemId
/// ↔ InventoryItemData.cookingData.id) — đúng bảng mà luồng bếp CŨ vẫn dùng
/// (CookingBoot.BuildInventoryLookup), nên hai luồng cũ/mới không bao giờ lệch nhau.
///
/// VÌ SAO cần: UI bếp v2 đối chiếu THẲNG khoá farm với id bếp — trùng nhau với đa số
/// nguyên liệu nhưng lệch với gà/nấm → hàng đã gửi vào bếp không hiện, và khi nấu xong
/// không trừ được kho. [Sếp 2026-08-27 — nối logic bếp mới]
/// </summary>
public static class KitchenIdMap
{
    /// <summary>
    /// Alias id NÔNG TRẠI → itemId chuẩn của InventoryItemData. KHÔNG đổi id gốc nào
    /// (luật dự án) — chỉ dịch khi đối chiếu. Trường hợp đã xác minh bằng asset thật:
    /// crop `nam.asset` thu hoạch ra id 'nam' nhưng InventoryItemData là Item_Mushroom
    /// (itemId 'mushroom') — không có bảng này thì nấm KHÔNG BAO GIỜ gửi bếp được.
    /// Thêm dòng mới MỖI KHI phát hiện id kho ↔ id item lệch nhau. [Sếp 2026-08-27]
    /// </summary>
    private static readonly Dictionary<string, string> FarmAliases = new Dictionary<string, string>
    {
        { "nam", "mushroom" },
    };

    /// <summary>Chuẩn hoá id kho nông trại về itemId InventoryItemData (alias đã xác minh; không có alias → trả nguyên).</summary>
    public static string NormalizeFarmId(string farmId)
    {
        if (string.IsNullOrEmpty(farmId)) return farmId;
        string key = farmId.Trim().ToLower();
        return FarmAliases.TryGetValue(key, out string canon) ? canon : farmId.Trim();
    }

    // key: farmId lower  → value: kitchenId lower  (để khớp khoá Dictionary _cards của UI v2)
    private static Dictionary<string, string> farmToKitchen;
    // key: kitchenId lower → value: farmId NGUYÊN GỐC (để khớp đúng khoá trong transferredItems)
    private static Dictionary<string, string> kitchenToFarm;

    private static void EnsureBuilt()
    {
        // Cho phép build lại khi map rỗng (vd gọi quá sớm, CookingBoot chưa có trong scene)
        if (farmToKitchen != null && farmToKitchen.Count > 0)
            return;

        farmToKitchen = new Dictionary<string, string>();
        kitchenToFarm = new Dictionary<string, string>();

        var boot = Object.FindFirstObjectByType<CookingBoot>(FindObjectsInactive.Include);
        if (boot == null || boot.cookingInventoryItems == null)
            return;

        foreach (var item in boot.cookingInventoryItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId) || item.cookingData == null)
                continue;
            string kitchenId = item.cookingData.id;
            if (string.IsNullOrEmpty(kitchenId))
                continue;

            string farmLower = item.itemId.Trim().ToLower();
            string kitchenLower = kitchenId.Trim().ToLower();

            if (!farmToKitchen.ContainsKey(farmLower))
                farmToKitchen[farmLower] = kitchenLower;
            if (!kitchenToFarm.ContainsKey(kitchenLower))
                kitchenToFarm[kitchenLower] = item.itemId.Trim();
        }
    }

    /// <summary>farmId (bất kỳ hoa/thường) → kitchenId lower. Không có trong bảng → trả nguyên input lower (đa số id vốn trùng).</summary>
    public static string ToKitchen(string farmId)
    {
        if (string.IsNullOrEmpty(farmId)) return farmId;
        EnsureBuilt();
        string key = NormalizeFarmId(farmId).ToLower();
        return farmToKitchen != null && farmToKitchen.TryGetValue(key, out string k) ? k : key;
    }

    /// <summary>kitchenId → farmId NGUYÊN GỐC. Không có trong bảng → trả nguyên input (giữ hành vi cũ với id trùng).</summary>
    public static string ToFarm(string kitchenId)
    {
        if (string.IsNullOrEmpty(kitchenId)) return kitchenId;
        EnsureBuilt();
        string key = kitchenId.Trim().ToLower();
        return kitchenToFarm != null && kitchenToFarm.TryGetValue(key, out string f) ? f : kitchenId;
    }
}
