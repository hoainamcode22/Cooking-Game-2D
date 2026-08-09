using System;
using System.Collections.Generic;
using UnityEngine;


public class StarterInventorySetup : MonoBehaviour
{
    private const string PREF_KEY = "STARTER_ITEMS_GIVEN";

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
        if (WarehouseManager.Instance == null)
        {
            Debug.LogWarning("[StarterInventory] WarehouseManager.Instance chua san sang. Se thu lai sau.");
            Invoke(nameof(GiveStarterItems), 0.5f);
            return;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  VÌ SAO KHÔNG CÒN CHẶN THEO CẤP ĐỘ
        // ══════════════════════════════════════════════════════════════════════
        //  Điều kiện cũ ở đây là `if (CurrentLevel > 1) return;`. Nó gây treo game:
        //  hồi đó `WarehouseManager` KHÔNG lưu gì cả (kho trống trơn mỗi lần vào scene)
        //  trong khi CẤP ĐỘ thì có lưu, và TutorialManager chạy lại từ bước 0 mỗi lần Play.
        //
        //  Ba điều đó cộng lại thành bẫy chết: thu hoạch 8 ô lúa → lên cấp 2 → thoát
        //  → vào lại thì cấp = 2, kho = 0 hạt, tutorial lại đòi "trồng hết các ô", mà
        //  điều kiện trên return sớm ⇒ KHÔNG cấp hạt ⇒ không có gì để trồng ⇒ các bước
        //  WaitForAllPlots* treo vĩnh viễn (chúng không có timeout). Đây đúng là hiện
        //  tượng "thu hoạch xong nó đứng im".
        //
        //  Kho GIỜ ĐÃ ĐƯỢC LƯU (WarehouseManager.Save/Load), nên mốc đúng để chặn là
        //  "đã có save kho hay chưa", KHÔNG phải cấp độ:
        //
        //    • Chưa có save kho  → lần chơi ĐẦU thật sự → cấp hạt khởi đầu.
        //    • Đã có save kho    → hạt đã được lưu qua các phiên → KHÔNG cấp thêm.
        //      Nếu vẫn bù mỗi lần Play thì mỗi phiên lại rót thêm cho đủ 10 hạt ⇒
        //      hạt vô hạn, phá cân bằng.
        //
        //  Trường hợp chơi lại tutorial ở cấp cao mà đã dùng hết hạt thì KHÔNG bù ở đây;
        //  lưới an toàn là watchdog trong TutorialManager (WatchdogHetHat) — nó nhả bước
        //  sau 6 giây nếu kho hết sạch hạt dùng được, nên không treo nữa.
        if (WarehouseManager.DaCoSaveKho)
        {
            Debug.Log("[StarterInventory] Kho đã có save → không cấp lại hạt khởi đầu.");
            return;
        }

        int given = 0;
        foreach (var entry in starterItems)
        {
            if (string.IsNullOrEmpty(entry.itemId)) continue;
            int current = WarehouseManager.Instance.GetAmount(entry.itemId);
            int missing = Mathf.Max(0, entry.amount - current);
            if (missing <= 0) continue;

            WarehouseManager.Instance.AddItem(entry.itemId, entry.displayName, entry.icon, missing);
            Debug.Log($"[StarterInventory] +{missing}x {entry.displayName} ({entry.itemId})");
            given++;
        }

        // Cắm mốc KỂ CẢ khi given == 0. Key save kho hiện chỉ sinh ra như tác dụng phụ
        // của AddItem; không cấp món nào thì key không tồn tại và DaCoSaveKho mãi false
        // ⇒ vòng này lặp lại mỗi phiên.
        WarehouseManager.Instance.GhiSaveNgay();

        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log($"[StarterInventory] Da cap {given} loai vat pham starter. Flag da set.");
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
