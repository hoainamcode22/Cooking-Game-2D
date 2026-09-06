using System;
using System.Collections.Generic;
using UnityEngine;


public class StarterInventorySetup : MonoBehaviour
{
    private const string PREF_KEY = "STARTER_ITEMS_GIVEN";

    /// <summary>Số hạt lúa khởi đầu mặc định — bằng số ô đất trong scene (8).</summary>
    public const int SO_HAT_LUA_KHOI_DAU = 8;
    /// <summary>Số hạt hướng dương khởi đầu mặc định — bằng số chậu hoa trong scene (6).</summary>
    public const int SO_HAT_HUONG_DUONG_KHOI_DAU = 6;

    /// <summary>[FIX 2026-09-06] Cam ga tang san dau game — nguoi choi moi khong phai di xay
    /// o Mill giua chung tutorial moi cho ga an duoc.</summary>
    public const int SO_CAM_GA_KHOI_DAU = 5;

    [Serializable]
    public class StarterItemEntry
    {
        [Tooltip("ItemId trong WarehouseManager (ví dụ: seed_rice, seed_huong_duong)")]
        public string itemId;

        [Tooltip("Tên hiển thị trong kho")]
        public string displayName;

        [Tooltip("Icon hạt giống — kéo sprite từ CropData vào đây")]
        public Sprite icon;

        [Tooltip("Số lượng ban đầu")]
        public int amount = 10;
    }

    [Header("Starter Items (chỉ cấp 1 lần cho save mới)")]
    [SerializeField] private List<StarterItemEntry> starterItems = new List<StarterItemEntry>();

    [Header("Debug")]
    [SerializeField] private bool forceResetOnPlay = false;

    private void Start()
    {
#if UNITY_EDITOR
        if (forceResetOnPlay)
        {
            PlayerPrefs.DeleteKey(PREF_KEY);

            // Phải dọn LUÔN kho, không thì cờ DaCoSaveKho vẫn true và GiveStarterItems()
            // return sớm ⇒ bật forceResetOnPlay mà chẳng có tác dụng gì. Dùng hàm của
            // WarehouseManager vì kho đã Load() ở Awake — chỉ xoá key thì hàng vẫn còn
            // trong bộ nhớ và lần AddItem kế tiếp sẽ ghi lại y nguyên.
            if (WarehouseManager.Instance != null)
                WarehouseManager.Instance.XoaSaveVaLamTrongKho();

            Debug.Log("[StarterInventory] DEBUG: Đã reset cờ starter + dọn kho.");
        }
#endif

        GiveStarterItems();
    }

    private void GiveStarterItems()
    {
        if (WarehouseManager.Instance == null && FarmInventoryManager.Instance == null)
        {
            Debug.LogWarning("[StarterInventory] WarehouseManager / FarmInventoryManager chưa sẵn sàng. Sẽ thử lại sau.");
            Invoke(nameof(GiveStarterItems), 0.2f);
            return;
        }

        // Kiểm tra xem kho có lúa (hạt cốt lõi để chạy tutorial) chưa
        int riceAmount = 0;
        if (FarmInventoryManager.Instance != null) riceAmount = FarmInventoryManager.Instance.GetAmount("seed_rice");
        else if (WarehouseManager.Instance != null) riceAmount = WarehouseManager.Instance.GetAmount("seed_rice");

        bool daCapStarter = PlayerPrefs.GetInt(PREF_KEY, 0) == 1;
        bool dangTutorial = TutorialManager.Instance != null && TutorialManager.Instance.DangChayTutorial;
        // Nếu đã cấp rồi VÀ trong kho vẫn còn đủ lúa thì không cấp thêm
        if (daCapStarter && riceAmount >= SO_HAT_LUA_KHOI_DAU && !dangTutorial)
        {
            Debug.Log("[StarterInventory] Kho đã có đủ hạt giống → không cấp lại.");
            return;
        }

        // Danh sách quà khởi đầu tiêu chuẩn
        var defaultStarters = new List<StarterItemEntry>
        {
            // WP-A2 (2026-09-05): khớp số ô thật trong scene — 8 ô đất, 6 chậu hoa.
            // Cấp đúng số thì tutorial "gieo hết" / "thu hết" không dư hạt gây hiểu nhầm.
            new StarterItemEntry { itemId = "seed_rice", displayName = "Hạt Lúa", amount = SO_HAT_LUA_KHOI_DAU },
            new StarterItemEntry { itemId = "seed_huong_duong", displayName = "Hạt Hoa Hướng Dương", amount = SO_HAT_HUONG_DUONG_KHOI_DAU },
            new StarterItemEntry { itemId = "cam_ga", displayName = "Cám Gà", amount = SO_CAM_GA_KHOI_DAU },
            new StarterItemEntry { itemId = "seed_bapcai", displayName = "Hạt Bắp Cải", amount = 5 },
            new StarterItemEntry { itemId = "seed_cachua", displayName = "Hạt Cà Chua", amount = 5 },
            new StarterItemEntry { itemId = "seed_carot", displayName = "Hạt Cà Rốt", amount = 5 },
            new StarterItemEntry { itemId = "ca_rot", displayName = "Hạt Cà Rốt", amount = 5 },
            new StarterItemEntry { itemId = "seed_ngo", displayName = "Hạt Ngô", amount = 5 }
        };

        var itemsToGive = (starterItems != null && starterItems.Count > 0) ? starterItems : defaultStarters;

        int given = 0;
        foreach (var entry in itemsToGive)
        {
            if (string.IsNullOrEmpty(entry.itemId)) continue;
            
            // Đảm bảo số hạt lúa luôn đúng 8 hạt (khớp 8 ô đất)
            int targetAmount = entry.amount;
            if (entry.itemId == "seed_rice" && targetAmount > SO_HAT_LUA_KHOI_DAU)
                targetAmount = SO_HAT_LUA_KHOI_DAU;

            int current = 0;
            if (FarmInventoryManager.Instance != null) current = FarmInventoryManager.Instance.GetAmount(entry.itemId);
            else if (WarehouseManager.Instance != null) current = WarehouseManager.Instance.GetAmount(entry.itemId);

            int missing = Mathf.Max(0, targetAmount - current);
            if (missing <= 0) continue;

            if (FarmInventoryManager.Instance != null)
            {
                FarmInventoryManager.Instance.AddItem(entry.itemId, missing);
            }
            else if (WarehouseManager.Instance != null)
            {
                WarehouseManager.Instance.AddItem(entry.itemId, entry.displayName, entry.icon, missing);
            }
            Debug.Log($"[StarterInventory] +{missing}x {entry.displayName} ({entry.itemId})");
            given++;
        }

        // Cập nhật lại toàn bộ khay hạt giống dưới đáy màn hình
        var allSeedDrags = UnityEngine.Object.FindObjectsByType<SeedDragItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allSeedDrags.Length; i++)
        {
            allSeedDrags[i].RefreshStockDisplay();
        }

        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.GhiSaveNgay();
        }

        PlayerPrefs.SetInt(PREF_KEY, 1);
        LuuGopPrefs.Hen();
        Debug.Log($"[StarterInventory] Đã cấp {given} loại hạt giống khởi đầu. Flag đã set.");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Reset Starter Flag (cho test)")]
    private void DebugReset()
    {
        PlayerPrefs.DeleteKey(PREF_KEY);
        Debug.Log("[StarterInventory] Flag da reset — lan chay toi se cap lai starter items.");
    }
#endif
}
