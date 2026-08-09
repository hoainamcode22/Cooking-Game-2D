using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Nhóm vật phẩm dùng cho dải tab danh mục ở panel chọn vật phẩm (B4).</summary>
public enum StallItemCategory
{
    TatCa    = 0,
    NongSan  = 1,
    Hoa      = 2,
    HatGiong = 3,
    CheBien  = 4,   // sản phẩm chuồng, sản phẩm máy, gia vị, món ăn, vật liệu
}

/// <summary>
/// SỔ TRA VẬT PHẨM cho quầy hàng: itemId → icon, tên hiển thị, danh mục, kho nguồn, giá gốc.
///
/// VÌ SAO phải có lớp này: dự án KHÔNG có registry vật phẩm toàn cục (không có
/// `ItemDatabase.GetItemById`). Mỗi màn hình đang tự khai một `List&lt;CropData&gt;` +
/// `List&lt;InventoryItemData&gt;` rồi tự dựng Dictionary — xem `MarketManager.BuildVisualLookup`
/// và `WarehousePopupUI`. Quầy hàng cần tra ở BA nơi (popup, lưới chọn, mặt quầy ngoài map);
/// nhân bản ba lần cùng một danh sách là ba cơ hội để chúng lệch nhau. Gom về một chỗ,
/// mọi nơi đọc qua `Instance`.
///
/// Danh sách asset do Editor tool `Tools ▸ Farm ▸ Quầy Hàng` quét và gán — không quét
/// bằng Resources lúc runtime, vì như vậy mọi asset sẽ bị nhồi vào build kể cả thứ không dùng.
/// </summary>
public class StallItemCatalog : MonoBehaviour
{
    public static StallItemCatalog Instance { get; private set; }

    [Header("Nguồn dữ liệu (Editor tool tự quét và gán)")]
    [Tooltip("Toàn bộ CropData: cho ra nông sản, hoa và hạt giống.")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();

    [Tooltip("Toàn bộ InventoryItemData: nguyên liệu, gia vị, sản phẩm chuồng, món ăn, vật liệu.")]
    [SerializeField] private List<InventoryItemData> itemDatabase = new List<InventoryItemData>();

    /// <summary>
    /// Sửa tay phân loại cho vài trường hợp cá biệt. Phân loại tự động chỉ đúng được
    /// với thứ suy ra từ CropData; mọi InventoryItemData còn lại đều rơi vào "Chế biến".
    /// Có bảng đè này thì chủ dự án chỉnh trong Inspector, không phải sửa code.
    /// </summary>
    [Serializable]
    public class CategoryOverride
    {
        public string            itemId;
        public StallItemCategory category = StallItemCategory.CheBien;
    }

    [Header("Ghi đè phân loại (tuỳ chọn)")]
    [SerializeField] private List<CategoryOverride> categoryOverrides = new List<CategoryOverride>();

    private class Entry
    {
        public Sprite            icon;
        public string            displayName;
        public StallItemCategory category;
        public StallSourceStore  store;
        public int               sellGold;   // 0 = không biết
    }

    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
    private bool _built;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);   // Destroy(this) chứ không phải gameObject: catalog thường gắn
            return;          // chung GameObject với popup, xoá cả object sẽ giết luôn popup.
        }

        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static string Normalize(string id)
        => string.IsNullOrEmpty(id) ? string.Empty : id.Trim().ToLowerInvariant();

    /// <summary>Dựng lại bảng tra. Public để Editor tool gọi được sau khi gán danh sách.</summary>
    public void Build()
    {
        _entries.Clear();

        // ── 1 · Từ CropData: ra cả HẠT GIỐNG lẫn NÔNG SẢN/HOA ────────────────
        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null) continue;

            bool isFlower = crop.cropCategory == CropCategory.Flower;
            string cropName = !string.IsNullOrEmpty(crop.itemName) ? crop.itemName : crop.cropId;

            // Hạt giống → kho WarehouseManager
            string seedId = Normalize(crop.seedItemId);
            if (!string.IsNullOrEmpty(seedId))
            {
                Put(seedId, new Entry
                {
                    icon        = crop.itemIcon,
                    displayName = TenHatGiong(cropName),
                    category    = StallItemCategory.HatGiong,
                    store       = StallSourceStore.SeedWarehouse,
                    // Hạt bán lại nửa giá mua: mua-bán vòng tròn phải LỖ, không thì
                    // người chơi đứng ở chợ bấm mua-bán là ra tiền vô hạn.
                    sellGold    = Mathf.Max(1, crop.goldPrice / 2),
                });
            }

            // Nông sản thu hoạch → kho FarmInventoryManager
            string harvestId = Normalize(!string.IsNullOrEmpty(crop.harvestItemId) ? crop.harvestItemId : crop.cropId);
            if (!string.IsNullOrEmpty(harvestId))
            {
                Put(harvestId, new Entry
                {
                    icon        = crop.harvestIcon != null ? crop.harvestIcon : crop.itemIcon,
                    displayName = cropName,
                    category    = isFlower ? StallItemCategory.Hoa : StallItemCategory.NongSan,
                    store       = StallSourceStore.FarmInventory,
                    sellGold    = crop.sellGold,
                });
            }
        }

        // ── 2 · Từ InventoryItemData ─────────────────────────────────────────
        //  Đi SAU CropData để icon/tên "chuẩn kho" được ưu tiên: `rice` trong CropData
        //  tên là "Lúa" (cây), còn trong InventoryItemData là "Gạo" (vật phẩm trong kho) —
        //  người chơi nhìn kho thấy "Gạo" thì ở quầy cũng phải thấy "Gạo".
        for (int i = 0; i < itemDatabase.Count; i++)
        {
            InventoryItemData item = itemDatabase[i];
            if (item == null) continue;

            string id = Normalize(item.itemId);
            if (string.IsNullOrEmpty(id)) continue;

            if (_entries.TryGetValue(id, out Entry existed))
            {
                // Đã có từ CropData → chỉ làm đẹp phần hiển thị, GIỮ NGUYÊN danh mục,
                // kho nguồn và giá (những thứ CropData biết chính xác hơn).
                if (item.icon != null) existed.icon = item.icon;
                if (!string.IsNullOrEmpty(item.displayName)) existed.displayName = item.displayName;
                continue;
            }

            Put(id, new Entry
            {
                icon        = item.icon,
                displayName = !string.IsNullOrEmpty(item.displayName) ? item.displayName : id,
                category    = StallItemCategory.CheBien,
                store       = StallSourceStore.FarmInventory,
                sellGold    = 0,   // để BasePriceBook rơi xuống bảng dự phòng / bảng của DEV-A
            });
        }

        // ── 3 · Ghi đè tay ───────────────────────────────────────────────────
        for (int i = 0; i < categoryOverrides.Count; i++)
        {
            CategoryOverride ov = categoryOverrides[i];
            if (ov == null) continue;
            string id = Normalize(ov.itemId);
            if (string.IsNullOrEmpty(id)) continue;
            if (_entries.TryGetValue(id, out Entry e)) e.category = ov.category;
        }

        _built = true;
    }

    private void Put(string id, Entry entry)
    {
        if (string.IsNullOrEmpty(id) || entry == null) return;
        _entries[id] = entry;
    }

    private Entry Find(string itemId)
    {
        if (!_built) Build();
        string id = Normalize(itemId);
        if (string.IsNullOrEmpty(id)) return null;
        return _entries.TryGetValue(id, out Entry e) ? e : null;
    }

    /// <summary>"Lúa" → "Hạt Lúa". Không thêm nếu tên đã tự nói nó là hạt.</summary>
    private static string TenHatGiong(string cropName)
    {
        if (string.IsNullOrEmpty(cropName)) return "Hạt giống";
        return cropName.StartsWith("Hạt", StringComparison.OrdinalIgnoreCase)
            ? cropName
            : "Hạt " + cropName;
    }

    // ── API tra cứu ──────────────────────────────────────────────────────────

    public bool TryGetVisual(string itemId, out Sprite icon, out string displayName)
    {
        Entry e = Find(itemId);
        if (e == null)
        {
            icon = null;
            displayName = itemId;
            return false;
        }

        icon = e.icon;
        displayName = e.displayName;
        return true;
    }

    public Sprite GetIcon(string itemId) => Find(itemId)?.icon;

    public string GetDisplayName(string itemId)
    {
        Entry e = Find(itemId);
        return e != null && !string.IsNullOrEmpty(e.displayName) ? e.displayName : itemId;
    }

    public StallItemCategory GetCategory(string itemId)
        => Find(itemId)?.category ?? StallItemCategory.CheBien;

    /// <summary>
    /// Kho nào đang giữ vật phẩm này. Mặc định là kho nông sản — an toàn hơn, vì đoán
    /// nhầm thành kho hạt giống sẽ nhét nông sản vào kho chỉ dành cho hạt và người chơi
    /// không lấy ra được.
    /// </summary>
    public StallSourceStore GetSourceStore(string itemId)
        => Find(itemId)?.store ?? StallSourceStore.FarmInventory;

    /// <summary>Giá bán lấy từ ASSET THẬT. false nghĩa là asset không khai giá.</summary>
    public bool TryGetSellGold(string itemId, out int gold)
    {
        Entry e = Find(itemId);
        gold = e?.sellGold ?? 0;
        return gold > 0;
    }

    public bool Contains(string itemId) => Find(itemId) != null;

#if UNITY_EDITOR
    /// <summary>Editor tool gọi để nhồi danh sách asset đã quét. Chỉ tồn tại trong Editor.</summary>
    public void EditorSetDatabases(List<CropData> crops, List<InventoryItemData> items)
    {
        cropDatabase = crops ?? new List<CropData>();
        itemDatabase = items ?? new List<InventoryItemData>();
        Build();
    }
#endif
}
