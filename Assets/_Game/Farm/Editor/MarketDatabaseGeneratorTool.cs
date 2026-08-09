#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  SINH LẠI MarketDatabase.asset TỪ BẢNG GIÁ (A3)
/// ══════════════════════════════════════════════════════════════════════════
///
/// VÌ SAO phải có tool thay vì gõ tay 60 dòng YAML:
/// bản cũ có 48 dòng thì 38 dòng là TODO_* và 10 dòng còn lại trùng nhau
/// (fishsauce ×6). Đó là kết quả tất yếu của việc gõ tay. Sửa giá ở
/// MarketPriceTable.cs rồi bấm menu này là xong, không bao giờ lệch nữa.
///
/// Tool CÒN LỌC theo icon: vật phẩm chưa có icon sẽ KHÔNG được đưa vào chợ.
/// Mục 8 BÀN GIAO ghi rõ "không có icon trắng" — chặn từ khâu sinh dữ liệu
/// chắc chắn hơn là chặn ở UI.
/// </summary>
public static class MarketDatabaseGeneratorTool
{
    private const string DatabasePath = "Assets/_Game/Farm/data/Market/MarketDatabase.asset";

    [MenuItem("Tools/Farm/Chợ/2 · Sinh lại MarketDatabase từ bảng giá", false, 20)]
    public static void Generate()
    {
        MarketDatabase_SO database = AssetDatabase.LoadAssetAtPath<MarketDatabase_SO>(DatabasePath);
        if (database == null)
        {
            EditorUtility.DisplayDialog("Chợ",
                "Không tìm thấy asset:\n" + DatabasePath,
                "OK");
            return;
        }

        Dictionary<string, bool> iconMap = BuildIconAvailabilityMap();

        List<MarketItemDef> rows = new List<MarketItemDef>();
        List<string> skippedNoIcon  = new List<string>();
        List<string> skippedDisabled = new List<string>();

        IReadOnlyList<MarketItemInfo> all = MarketPriceTable.AllItems;
        for (int i = 0; i < all.Count; i++)
        {
            MarketItemInfo info = all[i];

            if (!info.MarketEnabled)
            {
                skippedDisabled.Add(info.ItemId);
                continue;
            }

            if (!iconMap.TryGetValue(info.ItemId, out bool hasIcon) || !hasIcon)
            {
                skippedNoIcon.Add(info.ItemId);
                continue;
            }

            GetQuantityRange(info.Category, out int minQ, out int maxQ);

            rows.Add(new MarketItemDef
            {
                ItemID      = info.ItemId,
                BuyPrice    = Mathf.Max(1, Mathf.RoundToInt(info.BasePrice * MarketPriceTable.MarketBuyMultiplier)),
                MinQuantity = minQ,
                MaxQuantity = maxQ,
                Category    = info.Category,
                UnlockLevel = info.UnlockLevel,
                Weight      = info.Weight
            });
        }

        // Sắp theo danh mục rồi theo cấp mở khoá — mở asset ra đọc là hiểu ngay,
        // không phải cuộn tìm giữa mớ dòng lộn xộn
        rows.Sort((a, b) =>
        {
            int byCategory = ((int)a.Category).CompareTo((int)b.Category);
            if (byCategory != 0) return byCategory;

            int byLevel = a.UnlockLevel.CompareTo(b.UnlockLevel);
            if (byLevel != 0) return byLevel;

            return a.BuyPrice.CompareTo(b.BuyPrice);
        });

        string notes =
            "SINH TỰ ĐỘNG — KHÔNG GÕ TAY.\n" +
            "Nguồn: MarketPriceTable.cs · Menu: Tools/Farm/Chợ/2 · Sinh lại MarketDatabase từ bảng giá\n" +
            "BuyPrice = BasePrice × " + MarketPriceTable.MarketBuyMultiplier.ToString("0.0") + "\n" +
            "Số dòng: " + rows.Count + " · bỏ vì thiếu icon: " + skippedNoIcon.Count +
            " · bỏ vì tắt thủ công: " + skippedDisabled.Count;

        Undo.RecordObject(database, "Sinh lại MarketDatabase");
        database.EditorReplaceItems(rows, notes);
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        StringBuilder report = new StringBuilder();
        report.AppendLine("Đã ghi " + rows.Count + " dòng vào MarketDatabase.asset.");
        if (skippedNoIcon.Count > 0)
            report.AppendLine("\nBỏ qua vì CHƯA CÓ ICON (gán icon vào asset rồi chạy lại):\n  " +
                              string.Join(", ", skippedNoIcon));
        if (skippedDisabled.Count > 0)
            report.AppendLine("\nBỏ qua vì MarketEnabled=false trong MarketPriceTable:\n  " +
                              string.Join(", ", skippedDisabled));

        Debug.Log("[Chợ] " + report);
        EditorUtility.DisplayDialog("Chợ", report.ToString(), "OK");
    }

    /// <summary>
    /// Số lượng mỗi thẻ theo danh mục.
    /// Hàng rẻ bán theo lô lớn, hàng đắt bán lẻ — không thì một thẻ phở bò
    /// ×12 sẽ ngốn sạch ví người chơi cấp 9.
    /// </summary>
    private static void GetQuantityRange(MarketCategory category, out int min, out int max)
    {
        switch (category)
        {
            case MarketCategory.NongSan:  min = 3; max = 12; break;
            case MarketCategory.Hoa:      min = 2; max = 8;  break;
            case MarketCategory.HatGiong: min = 1; max = 5;  break;
            case MarketCategory.ChanNuoi: min = 2; max = 6;  break;
            case MarketCategory.CheBien:  min = 1; max = 4;  break;
            case MarketCategory.GiaVi:    min = 2; max = 8;  break;
            case MarketCategory.MonAn:    min = 1; max = 3;  break;
            case MarketCategory.VatLieu:  min = 1; max = 4;  break;
            default:                      min = 1; max = 4;  break;
        }
    }

    /// <summary>
    /// Quét toàn dự án xem itemId nào đã có icon.
    /// Quét thật chứ không dựa vào danh sách trong Inspector: danh sách đó
    /// hay bị quên cập nhật, mà thiếu một dòng là ra thẻ icon trắng trên màn hình.
    /// </summary>
    private static Dictionary<string, bool> BuildIconAvailabilityMap()
    {
        Dictionary<string, bool> map = new Dictionary<string, bool>();

        string[] itemGuids = AssetDatabase.FindAssets("t:InventoryItemData");
        for (int i = 0; i < itemGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(itemGuids[i]);
            InventoryItemData data = AssetDatabase.LoadAssetAtPath<InventoryItemData>(path);
            if (data == null || string.IsNullOrEmpty(data.itemId))
                continue;

            SetHasIcon(map, data.itemId, data.icon != null);
        }

        string[] cropGuids = AssetDatabase.FindAssets("t:CropData");
        for (int i = 0; i < cropGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(cropGuids[i]);
            CropData crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
            if (crop == null)
                continue;

            bool hasIcon = crop.icon != null || crop.harvestIcon != null;

            SetHasIcon(map, crop.seedItemId,    hasIcon);
            SetHasIcon(map, crop.harvestItemId, hasIcon);
            SetHasIcon(map, crop.itemID,        hasIcon);
            SetHasIcon(map, crop.cropId,        hasIcon);
        }

        return map;
    }

    private static void SetHasIcon(Dictionary<string, bool> map, string itemId, bool hasIcon)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        string key = itemId.Trim().ToLowerInvariant();

        // OR dồn: một id có thể đến từ nhiều asset, chỉ cần MỘT nguồn có icon là đủ vẽ
        if (map.TryGetValue(key, out bool existing))
            map[key] = existing || hasIcon;
        else
            map[key] = hasIcon;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  KIỂM TRA DỮ LIỆU
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Farm/Chợ/4 · Kiểm tra dữ liệu chợ", false, 40)]
    public static void Audit()
    {
        MarketDatabase_SO database = AssetDatabase.LoadAssetAtPath<MarketDatabase_SO>(DatabasePath);
        StringBuilder sb = new StringBuilder();

        if (database == null)
        {
            EditorUtility.DisplayDialog("Chợ", "Không tìm thấy MarketDatabase.asset", "OK");
            return;
        }

        Dictionary<string, bool> iconMap = BuildIconAvailabilityMap();
        HashSet<string> seen = new HashSet<string>();

        int todoCount = 0, dupCount = 0, noPriceCount = 0, noIconCount = 0;

        IReadOnlyList<MarketItemDef> items = database.Items;
        for (int i = 0; i < items.Count; i++)
        {
            MarketItemDef def = items[i];
            if (def == null || string.IsNullOrWhiteSpace(def.ItemID))
                continue;

            string id = def.ItemID.Trim().ToLowerInvariant();

            if (id.StartsWith("todo_")) { todoCount++;  sb.AppendLine("TODO còn sót: " + def.ItemID); }
            if (!seen.Add(id))          { dupCount++;   sb.AppendLine("Trùng lặp: " + def.ItemID); }
            if (!MarketPriceTable.Has(id)) { noPriceCount++; sb.AppendLine("Không có trong bảng giá: " + def.ItemID); }
            if (!iconMap.TryGetValue(id, out bool hasIcon) || !hasIcon)
            {
                noIconCount++;
                sb.AppendLine("Thiếu icon: " + def.ItemID);
            }
        }

        string header =
            "Tổng dòng: " + items.Count + "\n" +
            "TODO còn sót: " + todoCount + "\n" +
            "Trùng lặp: " + dupCount + "\n" +
            "Thiếu giá gốc: " + noPriceCount + "\n" +
            "Thiếu icon: " + noIconCount + "\n";

        Debug.Log("[Chợ] Kiểm tra dữ liệu\n" + header + sb);
        EditorUtility.DisplayDialog("Chợ — kiểm tra dữ liệu",
            header + (sb.Length > 0 ? "\nXem Console để biết chi tiết." : "\nSạch."),
            "OK");
    }
}
#endif
