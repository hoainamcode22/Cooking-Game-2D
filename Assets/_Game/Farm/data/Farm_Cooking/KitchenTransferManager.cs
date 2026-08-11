using System;
using System.Collections.Generic;
using UnityEngine;

public class KitchenTransferManager : MonoBehaviour
{
    [Serializable]
    private class TransferEntry
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    private class TransferSaveData
    {
        /// <summary>
        /// B4 — phiên bản save của hàng đã gửi vào bếp.
        ///
        /// KHÔNG đặt mặc định = <see cref="CurrentSaveVersion"/>: save đời trước không có
        /// khoá "saveVersion" nên `JsonUtility` để nguyên giá trị khởi tạo. Đặt mặc định
        /// bằng phiên bản hiện tại là save cũ tự nhận mình là mới ⇒ mất luôn cơ hội chuyển đổi.
        /// Để 0 thì phân biệt được ngay. (Cùng thủ thuật `ConstructionManager` đang dùng.)
        /// </summary>
        public int saveVersion;

        public List<TransferEntry> entries = new List<TransferEntry>();
    }

    public static KitchenTransferManager Instance { get; private set; }

    private const string SaveKey = "KITCHEN_TRANSFER_SAVE";

    /// <summary>
    /// v0 = save trước khi dọn `IngredientData` trùng (A7) và trước khi xoá 2 món cá (A4).
    /// v1 = itemId đã chuẩn hoá theo bảng asset hiện hành.
    ///
    /// VÌ SAO cần: save này giữ ĐÚNG `itemId` chuỗi. `ca` (cá) đã bị xoá khỏi dự án, nên
    /// người chơi đang có "ca" trong save sẽ giữ một khoá rác vĩnh viễn — `CookingBoot`
    /// bỏ qua im lặng, còn `WarehousePopupUI` thì vẫn trừ khỏi kho. Phải cắt tại lúc nạp.
    /// </summary>
    private const int CurrentSaveVersion = 1;

    /// <summary>
    /// itemId đã bị xoá khỏi dự án — phải gỡ khỏi save cũ khi migrate.
    /// Thêm id vào đây MỖI KHI xoá một `InventoryItemData`, đồng thời tăng
    /// <see cref="CurrentSaveVersion"/>.
    /// </summary>
    private static readonly string[] DeadItemIds = { "ca", "ca_nuong_tieu", "canh_chua_ca" };

    private readonly Dictionary<string, int> transferredItems = new Dictionary<string, int>();

    // C10 — đã xoá `public Action OnTransferredItemsChanged;`.
    // Sự kiện này được bắn ở 4 chỗ (AddTransferredItem, ClearTransferredItems,
    // RemoveTransferredItem, SetAfterCooking) mà KHÔNG MỘT AI đăng ký nghe.
    // Event mồ côi tệ hơn cả code chết: người đọc tin rằng UI bếp tự cập nhật theo
    // kho nên không đi tìm chỗ gọi refresh — mà thực tế màn hình chỉ được làm mới
    // khi `CookingChallengeManager.ResetCookingSelectionState()` gọi tay
    // `cookingBoot.RefreshTransferredItemCards()`. Cần cập nhật realtime thì nối
    // thẳng lời gọi đó, đừng dựng lại event rồi lại quên đăng ký.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // Tách ra root để DontDestroyOnLoad hoạt động (fix warning)
        DontDestroyOnLoad(gameObject);

        LoadTransferData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveTransferData();
    }

    private void OnApplicationQuit()
    {
        SaveTransferData();
    }

    public void AddTransferredItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        if (!transferredItems.ContainsKey(itemId))
            transferredItems[itemId] = 0;

        transferredItems[itemId] += amount;
        SaveTransferData();
    }

    public int GetTransferredAmount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return 0;

        return transferredItems.TryGetValue(itemId, out int value) ? value : 0;
    }

    public List<KeyValuePair<string, int>> GetTransferredItems()
    {
        List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();

        foreach (var kv in transferredItems)
        {
            if (kv.Value > 0)
                result.Add(kv);
        }

        return result;
    }

    public void ClearTransferredItems()
    {
        transferredItems.Clear();
        SaveTransferData();
    }

    public bool HasTransferredItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        return transferredItems.TryGetValue(itemId, out int value) && value >= amount;
    }

    public bool RemoveTransferredItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return false;

        if (!transferredItems.TryGetValue(itemId, out int current))
            return false;

        if (current < amount)
            return false;

        current -= amount;

        if (current <= 0)
            transferredItems.Remove(itemId);
        else
            transferredItems[itemId] = current;

        SaveTransferData();
        return true;
    }

    private void SaveTransferData()
    {
        // LUÔN gán tường minh: `saveVersion` mặc định là 0 (cố ý — xem chú thích ở class),
        // nên không gán ở đây là ghi ra save v0 và lần nạp sau lại chạy migrate lần nữa.
        TransferSaveData data = new TransferSaveData { saveVersion = CurrentSaveVersion };

        foreach (var kv in transferredItems)
        {
            if (kv.Value <= 0)
                continue;

            data.entries.Add(new TransferEntry
            {
                itemId = kv.Key,
                amount = kv.Value
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    private void LoadTransferData()
    {
        transferredItems.Clear();

        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
            return;

        TransferSaveData data = JsonUtility.FromJson<TransferSaveData>(json);
        if (data == null || data.entries == null)
            return;

        if (data.saveVersion > CurrentSaveVersion)
        {
            // Save mới hơn code = người chơi vừa hạ cấp bản game. Đọc tiếp thì có thể gặp
            // itemId mà bản này không biết; nhưng XOÁ save thì mất hàng của họ. Chọn đọc
            // tiếp và cảnh báo — dữ liệu chỉ là (itemId, số lượng), rủi ro thấp nhất.
            Debug.LogWarning($"[KitchenTransfer] Save v{data.saveVersion} mới hơn code " +
                             $"v{CurrentSaveVersion} — đọc tiếp, có thể gặp itemId chưa biết.");
        }

        bool canMigrate = data.saveVersion < CurrentSaveVersion;
        int soMucBoDi = 0;

        for (int i = 0; i < data.entries.Count; i++)
        {
            TransferEntry entry = data.entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.itemId) || entry.amount <= 0)
                continue;

            // v0 → v1: gỡ những itemId đã bị xoá khỏi dự án. Không gỡ thì khoá rác nằm
            // trong save mãi mãi: `CookingBoot` bỏ qua im lặng (không có `InventoryItemData`
            // tương ứng) nên người chơi thấy "đã gửi vào bếp" mà bếp không có gì.
            if (canMigrate && System.Array.IndexOf(DeadItemIds, entry.itemId) >= 0)
            {
                soMucBoDi++;
                continue;
            }

            transferredItems[entry.itemId] = entry.amount;
        }

        if (canMigrate)
        {
            Debug.Log($"[KitchenTransfer] Chuyển save v{data.saveVersion} → v{CurrentSaveVersion}" +
                      (soMucBoDi > 0 ? $", bỏ {soMucBoDi} vật phẩm đã xoá khỏi dự án." : "."));
            SaveTransferData();   // ghi lại kèm dấu phiên bản → chỉ chuyển MỘT LẦN
        }
    }
    //File nÃ y cá»§a NguyÃªn thÃªm vÃ o
    public void SetAfterCooking(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (!transferredItems.ContainsKey(itemId))
        {
            return;
        }

        transferredItems[itemId] -= 1;


        if (transferredItems[itemId] <= 0)
        {
            transferredItems.Remove(itemId);
        }

        SaveTransferData();
    }

    public void SetAfterCooking(List<string> selectedItemIds)
    {
        if (selectedItemIds == null || selectedItemIds.Count == 0)
            return;

        for (int i = 0; i < selectedItemIds.Count; i++)
        {
            SetAfterCooking(selectedItemIds[i]);
        }
    }
}
