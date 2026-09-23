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

    /// <summary>
    /// [Vòng 16 · Hồ Câu] Cá từ giỏ cá (kho ngoài). Thêm ở CUỐI vì `CategoryOverride.category`
    /// serialize theo số. CHƯA có tab riêng trong StallPopupUI (cột tab 620 px đã đủ 5 tab) →
    /// cá hiện ở tab "Tất cả"; muốn tab "Cá" riêng phải dựng thêm ở StallHierarchyBuilderTool.
    /// </summary>
    Ca       = 5,

    /// <summary>[2026-09-22] Vật liệu xây dựng: gỗ, đá, kính, đinh, sơn, gạch.
    /// Thêm ở CUỐI — `CategoryOverride.category` serialize theo SỐ, đổi thứ tự là hỏng dữ liệu.</summary>
    VatLieu      = 6,

    /// <summary>[2026-09-22] Thức ăn gia súc: cám gà, cám heo, cỏ trộn bò, cám bò sữa.</summary>
    ThucAnGiaSuc = 7,
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

        // Tự động tìm nguồn dữ liệu dự phòng nếu danh sách trong scene bị trống
        if (cropDatabase == null || cropDatabase.Count == 0)
        {
            var mm = UnityEngine.Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);
            if (mm != null && mm.CropDatabase != null && mm.CropDatabase.Count > 0)
                cropDatabase = new List<CropData>(mm.CropDatabase);
        }
        if (itemDatabase == null || itemDatabase.Count == 0)
        {
            var mm = UnityEngine.Object.FindFirstObjectByType<MarketManager>(FindObjectsInactive.Include);
            if (mm != null && mm.ItemDatabase != null && mm.ItemDatabase.Count > 0)
                itemDatabase = new List<InventoryItemData>(mm.ItemDatabase);
        }

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

        // ── 2.5 · Định tuyến mặc định theo ID (2026-09-22) ───────────────────
        // VÌ SAO: mọi InventoryItemData đều rơi vào CheBien ở bước 2, nên gỗ/đá/kính và
        // cám gà/cám heo nằm chung tab "Cook". Danh sách `categoryOverrides` trong scene
        // đang RỖNG nên không ai sửa việc đó. Bảng cứng dưới đây bảo đảm đúng tab kể cả
        // khi Sếp chưa điền gì trong Inspector; `categoryOverrides` chạy SAU nên vẫn thắng.
        foreach (var kv in _entries)
        {
            StallItemCategory dm = PhanLoaiMacDinhTheoId(kv.Key);
            if (dm != StallItemCategory.TatCa) kv.Value.category = dm;
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
        if (_entries.TryGetValue(id, out Entry e)) { return e; }

        // [Vòng 16] Không có trong asset farm → hỏi KHO NGOÀI (giỏ cá…) qua cổng cắm. Tra lười
        // và cache vào _entries (Build() có xoá thì lần tra sau lại hỏi tiếp) — nhờ vậy thứ tự
        // đăng ký kho ngoài / Awake catalog không quan trọng. Không có kho ngoài → null như cũ.
        return FindExternal(id);
    }

    private Entry FindExternal(string id)
    {
        if (!StallExternalStores.TryFindOwner(id, out StallSourceStore store, out _, out StallExternalItemInfo info))
        {
            return null;
        }

        var e = new Entry
        {
            icon        = info.icon,
            displayName = !string.IsNullOrEmpty(info.displayName) ? info.displayName : id,
            category    = info.category,
            store       = store,
            sellGold    = Mathf.Max(0, info.baseSellGold),
        };
        Put(id, e);
        return e;
    }

    /// <summary>"Lúa" → "Hạt Lúa". Không thêm nếu tên đã tự nói nó là hạt.</summary>
    private static string TenHatGiong(string cropName)
    {
        if (string.IsNullOrEmpty(cropName)) return Loc.T("Hạt giống");
        // Ghép chuỗi thì interceptor không match được -> dùng format key "Hạt {0}".
        return cropName.StartsWith("Hạt", StringComparison.OrdinalIgnoreCase)
            ? Loc.T(cropName)
            : Loc.TF("Hạt {0}", Loc.T(cropName));
    }

    // ── API tra cứu ──────────────────────────────────────────────────────────

    private static Sprite ResolveMissingIcon(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        string key = Normalize(id);

        Sprite s = MarketManager.TryResolveFallbackIcon(key);
        if (s != null) return s;

        // Thử tìm qua WarehousePopupUI
        var wh = UnityEngine.Object.FindFirstObjectByType<WarehousePopupUI>(FindObjectsInactive.Include);
        if (wh != null)
        {
            Sprite whIcon = wh.GetDirectIconFromDatabases(key);
            if (whIcon != null) return whIcon;
        }

        // ── F4: PHẢI NẠP ĐƯỢC CẢ Ở BẢN RELEASE ──────────────────────────────
        // Trước đây cả khối tra cứu này nằm trong #if UNITY_EDITOR + AssetDatabase, nên
        // trên máy người chơi nó luôn trả null → icon cám gà / cám heo / cám bò sữa /
        // cỏ trộn bò TRẮNG TRƠN. Bốn PNG đó đã có sẵn trong
        // Assets/_Game/Resources/Mill/Icons/ nên nạp bằng Resources.Load là chạy ở mọi
        // nền tảng. Vẫn giữ nhánh AssetDatabase làm DỰ PHÒNG cho Editor (phòng khi ai đó
        // xoá/đổi chỗ thư mục Resources thì trong Editor vẫn thấy icon mà sửa).
        string resName = null;
        string editorPath = null;
        switch (key)
        {
            case "cam_ga":
                resName = "Mill/Icons/feed_cam_ga";
                editorPath = "Assets/_Game/GeneratedUI/Mill/Icons/feed_cam_ga.png";
                break;
            case "cam_heo":
                resName = "Mill/Icons/feed_cam_heo";
                editorPath = "Assets/_Game/GeneratedUI/Mill/Icons/feed_cam_heo.png";
                break;
            case "co_tron_bo":
                resName = "Mill/Icons/feed_co_tron_bo";
                editorPath = "Assets/_Game/GeneratedUI/Mill/Icons/feed_co_tron_bo.png";
                break;
            case "cam_bo_sua":
                resName = "Mill/Icons/feed_cam_bo_sua";
                editorPath = "Assets/_Game/GeneratedUI/Mill/Icons/feed_cam_bo_sua.png";
                break;
        }

        if (!string.IsNullOrEmpty(resName))
        {
            Sprite fromRes = Resources.Load<Sprite>(resName);
            if (fromRes != null) return fromRes;
        }

        // Thử load trực tiếp từ Resources với các folder icon
        Sprite rSpr = Resources.Load<Sprite>($"UI_Crop/{key}") ??
                      Resources.Load<Sprite>($"UI_Items/{key}") ??
                      Resources.Load<Sprite>($"Icons/{key}") ??
                      Resources.Load<Sprite>($"UI_MarketBoard/{key}");
        if (rSpr != null) return rSpr;

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(editorPath))
        {
            Sprite fromEditor = LoadSpriteAtPath(editorPath);
            if (fromEditor != null) return fromEditor;
        }
#endif
        return null;
    }

    private static Sprite LoadSpriteAtPath(string path)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(path)) return null;
        var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (all != null)
        {
            foreach (var o in all) if (o is Sprite s) return s;
        }
#endif
        return null;
    }

    public bool TryGetVisual(string itemId, out Sprite icon, out string displayName)
    {
        Entry e = Find(itemId);
        string id = Normalize(itemId);
        if (e == null)
        {
            icon = ResolveMissingIcon(id);
            displayName = GetDisplayName(itemId);
            return icon != null;
        }

        if (e.icon == null) e.icon = ResolveMissingIcon(id);
        icon = e.icon;
        displayName = GetDisplayName(itemId);
        return true;
    }

    public Sprite GetIcon(string itemId)
    {
        var e = Find(itemId);
        if (e != null && e.icon != null) return e.icon;
        string id = Normalize(itemId);
        Sprite fallback = ResolveMissingIcon(id);
        if (fallback != null && e != null) e.icon = fallback;
        return fallback ?? e?.icon;
    }

    public string GetDisplayName(string itemId)
    {
        Entry e = Find(itemId);
        // [2026-09-23] Tra ten qua Loc.T (truoc day tra thang "Gao", "Go", "Da"...).
        if (e != null && !string.IsNullOrEmpty(e.displayName) && e.displayName != itemId)
            return Loc.T(e.displayName);
        string id = Normalize(itemId);
        switch (id)
        {
            case "cam_ga": return Loc.T("Cám cho gà");
            case "cam_heo": return Loc.T("Cám cho heo");
            case "co_tron_bo": return Loc.T("Cỏ trộn cho bò");
            case "cam_bo_sua": return Loc.T("Cám cho bò sữa");
        }
        return e != null ? Loc.T(e.displayName) : itemId;
    }

    /// <summary>Bảng định tuyến ID → tab, dùng khi dữ liệu asset không nói rõ danh mục.
    /// Trả về TatCa nghĩa là "không biết, đừng đụng vào".</summary>
    public static StallItemCategory PhanLoaiMacDinhTheoId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return StallItemCategory.TatCa;
        string k = itemId.Trim().ToLowerInvariant();

        switch (k)
        {
            // Vật liệu xây dựng
            case "go": case "go_":  case "wood":
            case "da": case "stone":
            case "kinh": case "glass":
            case "dinh": case "nail": case "nails":
            case "son": case "paint":
            case "gach": case "brick":
                return StallItemCategory.VatLieu;

            // Thức ăn gia súc
            case "cam_ga": case "cam_heo": case "cam_bo_sua":
            case "co_tron_bo": case "co_tron":
                return StallItemCategory.ThucAnGiaSuc;
        }

        if (k.StartsWith("cam_") || k.StartsWith("co_tron") || k.StartsWith("feed_"))
            return StallItemCategory.ThucAnGiaSuc;

        return StallItemCategory.TatCa;
    }

    public StallItemCategory GetCategory(string itemId)
    {
        Entry e = Find(itemId);
        if (e != null) return e.category;
        // Khong co trong so tra (vd cam_ga chua duoc keo vao itemDatabase trong scene):
        // van phai ve dung tab thay vi roi het vao "Cook".
        StallItemCategory dm = PhanLoaiMacDinhTheoId(itemId);
        return dm != StallItemCategory.TatCa ? dm : StallItemCategory.CheBien;
    }

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
