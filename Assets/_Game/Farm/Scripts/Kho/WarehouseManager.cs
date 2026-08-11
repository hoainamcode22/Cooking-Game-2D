using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WarehouseItemEntry
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public int amount;

    public WarehouseItemEntry(string itemId, string displayName, Sprite icon, int amount)
    {
        this.itemId = itemId;
        this.displayName = displayName;
        this.icon = icon;
        this.amount = amount;
    }
}

public class WarehouseManager : MonoBehaviour
{
    public static WarehouseManager Instance { get; private set; }

    [SerializeField] private List<WarehouseItemEntry> items = new List<WarehouseItemEntry>();

    public Action OnWarehouseChanged;

    // IReadOnlyList chứ không phải List: trả về List sửa được thì ai đó làm
    // `Items[0].amount--` là qua mặt Save() một cách im lặng, kho lệch với save.
    public IReadOnlyList<WarehouseItemEntry> Items => items;

    // =========================================================================
    //  LƯU KHO
    // =========================================================================
    //  TRƯỚC ĐÂY KHO KHÔNG ĐƯỢC LƯU GÌ CẢ. Mỗi lần vào scene là trống trơn, trong khi
    //  CẤP ĐỘ thì có lưu (PlayerProgressManager). Hậu quả nặng nhất: tutorial chạy lại
    //  từ bước 0 và đòi "trồng cho hết ô", nhưng StarterInventorySetup chặn cấp > 1 nên
    //  không cấp hạt ⇒ các bước WaitForAllPlots* (không có timeout) treo vĩnh viễn.
    //  Với người chơi thật thì còn tệ hơn: tắt app là mất sạch hạt giống đã mua.
    //
    //  KHÔNG LƯU `icon`: JsonUtility không serialize được `Sprite`. Và không cần —
    //  đã kiểm toàn bộ Assets\_Game: KHÔNG có chỗ nào đọc `Items` hay `.icon`; mọi nơi
    //  chỉ gọi GetAmount / AddItem / RemoveItem. Bảng kho (WarehousePopupUI) tự tra icon
    //  từ cropDatabase/extraItemDatabase của riêng nó, còn bảng chọn hạt lấy icon từ
    //  CropData. Vậy `icon` ở đây là dữ liệu ghi-rồi-không-ai-đọc, bỏ qua là an toàn.
    //  (Nếu sau này có UI đọc `.icon` thì nó sẽ null sau khi load — nhớ tra lại theo itemId.)

    private const string SaveKey = "FARM_WAREHOUSE";

    /// <summary>Tăng số này khi đổi cấu trúc save, rồi viết bước chuyển đổi trong Load().</summary>
    public const int CurrentSaveVersion = 1;

    /// <summary>
    /// Kho đã từng được lưu chưa? = "đây KHÔNG phải lần chơi đầu".
    ///
    /// `StarterInventorySetup` dùng cờ này để chỉ cấp hạt khởi đầu ĐÚNG MỘT LẦN.
    /// Không dùng cấp độ được: tutorial chạy lại từ bước 0 mỗi lần Play, nên nếu chặn
    /// theo cấp thì bản chơi lại ở cấp cao sẽ không có hạt và treo. Còn nếu bù mỗi
    /// lần Play thì kho đã lưu sẽ được rót thêm 10 hạt mỗi phiên — thành hạt vô hạn.
    /// Mốc "đã có save kho" tách được hai trường hợp đó.
    /// </summary>
    public static bool DaCoSaveKho => PlayerPrefs.HasKey(SaveKey);

    [Serializable]
    private class SaveEntry
    {
        public string itemId;
        public string displayName;
        public int    amount;
    }

    [Serializable]
    private class SaveData
    {
        public int saveVersion;
        public List<SaveEntry> list = new List<SaveEntry>();
    }

    private bool _canGhi;   // có thay đổi chưa được đẩy xuống đĩa

    // Save đọc không được (hỏng, hoặc của bản game MỚI HƠN) → CẤM ghi.
    // Nếu không có cờ này thì ta từ chối đọc nhưng lần AddItem kế tiếp vẫn ghi đè
    // thẳng lên save đó bằng dữ liệu v1 ⇒ người chơi hạ cấp bản game hoặc restore
    // từ cloud sẽ MẤT SẠCH KHO. Thà không lưu phiên này còn hơn phá save của họ.
    private bool _khongDuocGhi;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;   // save mới → giữ nguyên list gán ở scene

        SaveData data;
        try { data = JsonUtility.FromJson<SaveData>(json); }
        catch (Exception e)
        {
            Debug.LogError($"[Warehouse] Save hỏng, bỏ qua để không mất phiên chơi: {e.Message}");
            _khongDuocGhi = true;
            return;
        }
        if (data == null || data.list == null) return;

        if (data.saveVersion > CurrentSaveVersion)
        {
            // Save của bản game MỚI HƠN. Đọc bừa dễ hiểu sai dữ liệu → thà giữ kho scene.
            Debug.LogWarning($"[Warehouse] Save version {data.saveVersion} > " +
                             $"{CurrentSaveVersion} (bản game cũ hơn save) → bỏ qua, không đọc " +
                             "và KHÔNG ghi đè.");
            _khongDuocGhi = true;
            return;
        }

        items.Clear();
        foreach (var e in data.list)
        {
            if (e == null || string.IsNullOrEmpty(e.itemId) || e.amount <= 0) continue;

            // Chuẩn hoá khi đọc: save cũ (trước khi có ChuanHoaId) có thể chứa id lẫn
            // chữ hoa. Gộp luôn nếu hai dòng chuẩn hoá về cùng một khoá.
            string id  = ChuanHoaId(e.itemId);
            var    cu  = items.Find(x => x.itemId == id);
            if (cu != null) cu.amount += e.amount;
            else items.Add(new WarehouseItemEntry(id, e.displayName, null, e.amount));
        }

        Debug.Log($"[Warehouse] Đã đọc {items.Count} loại vật phẩm từ save (v{data.saveVersion}).");
        OnWarehouseChanged?.Invoke();
    }

    private void Save()
    {
        if (_khongDuocGhi) return;   // save hỏng / của bản mới hơn → không ghi đè

        var data = new SaveData { saveVersion = CurrentSaveVersion };
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.itemId) || it.amount <= 0) continue;
            data.list.Add(new SaveEntry
            {
                itemId      = it.itemId,
                displayName = it.displayName,
                amount      = it.amount,
            });
        }

        // Chỉ SetString ở đây (ghi vào bộ nhớ, rất nhẹ) — kho thay đổi liên tục mỗi lần
        // trồng 1 ô. PlayerPrefs.Save() (đẩy xuống đĩa, tốn) dồn lại làm ở Flush().
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        _canGhi = true;
    }

    private void Flush()
    {
        if (!_canGhi) return;
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        _canGhi = false;
    }

    /// <summary>
    /// Cắm mốc "đã có save kho" NGAY, kể cả khi kho không thay đổi gì.
    ///
    /// Cần vì key save hiện chỉ được tạo như TÁC DỤNG PHỤ của AddItem/RemoveItem.
    /// Nếu lần chơi đầu không cấp được món nào (danh sách starter rỗng, hoặc kho scene
    /// đã đủ số lượng) thì không có Save() nào chạy ⇒ key không tồn tại ⇒ cờ DaCoSaveKho
    /// mãi false ⇒ mỗi phiên lại chạy lại vòng cấp hạt khởi đầu.
    /// </summary>
    public void GhiSaveNgay()
    {
        Save();
        _canGhi = true;   // Save() có thể bị _khongDuocGhi chặn; Flush chỉ tốn 1 lời gọi
        Flush();
    }

    // Đẩy xuống đĩa ở mọi lối ra: người chơi mobile hầu như không bao giờ "thoát" hẳn,
    // họ chỉ chuyển app (OnApplicationPause) → thiếu nhánh này là mất dữ liệu.
    private void OnApplicationPause(bool paused) { if (paused) Flush(); }
    private void OnApplicationFocus(bool focus)  { if (!focus) Flush(); }
    private void OnApplicationQuit()             { Flush(); }
    private void OnDisable()                     { Flush(); }

    // Nhả Instance khi bị huỷ. Hiện chưa gây lỗi nhờ fake-null của Unity, nhưng nếu sau
    // này tắt domain reload thì static sẽ sống sót qua các lần Play và trỏ vào object đã chết.
    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>
    /// Thêm vật phẩm trực tiếp bằng id/tên/icon
    /// </summary>
    public void AddItem(string itemId, string displayName, Sprite icon, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        itemId = ChuanHoaId(itemId);
        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);

        if (found != null)
        {
            found.amount += amount;
        }
        else
        {
            items.Add(new WarehouseItemEntry(itemId, displayName, icon, amount));
        }

        Save();
        OnWarehouseChanged?.Invoke();
    }

    /// <summary>
    /// KHÔNG DÙNG NỮA — không có chỗ nào gọi. Nông sản thu hoạch đi vào
    /// <c>FarmInventoryManager</c> (xem <c>PlotController.cs:600</c>), còn kho này chỉ
    /// giữ HẠT GIỐNG. Giữ lại để không phá code cũ nếu có ai từng gọi qua reflection.
    ///
    /// Cẩn thận nếu định dùng lại: nó nạp theo <c>harvestItemId</c> (vd "rice") trong khi
    /// mọi chỗ khác của kho này nạp theo <c>seedItemId</c> (vd "seed_rice") — trộn hai họ
    /// khoá vào cùng một kho sẽ làm số lượng đếm sai.
    /// </summary>
    [Obsolete("Nông sản dùng FarmInventoryManager. Kho này chỉ giữ hạt giống.")]
    public void AddHarvest(CropData cropData, int amount)
    {
        if (cropData == null || amount <= 0)
            return;

        string itemId = cropData.harvestItemId;
        string displayName = string.IsNullOrEmpty(cropData.displayName) ? cropData.cropId : cropData.displayName;

        // ưu tiên icon trong crop
        Sprite icon = cropData.icon;

        AddItem(itemId, displayName, icon, amount);
    }

    /// <summary>
    /// Chuẩn hoá khoá vật phẩm.
    ///
    /// `FarmInventoryManager.NormalizeKey` đã làm việc này từ trước, còn kho hạt thì không.
    /// Giờ kho ĐƯỢC LƯU nên sai lệch chữ hoa/thường không còn là lỗi thoáng qua: "Seed_Rice"
    /// và "seed_rice" sẽ thành HAI dòng tồn tại vĩnh viễn trong save, người chơi thấy hạt
    /// trong kho nhưng trồng lại báo thiếu.
    /// </summary>
    private static string ChuanHoaId(string itemId)
        => string.IsNullOrEmpty(itemId) ? itemId : itemId.Trim().ToLowerInvariant();

    public int GetAmount(string itemId)
    {
        itemId = ChuanHoaId(itemId);
        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);
        return found != null ? found.amount : 0;
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetAmount(itemId) >= amount;
    }

    /// <summary>
    /// Trừ vật phẩm khỏi kho. Trả về false nếu không đủ.
    /// </summary>
    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

        itemId = ChuanHoaId(itemId);
        WarehouseItemEntry found = items.Find(x => x.itemId == itemId);
        if (found == null || found.amount < amount)
        {
            return false;
        }

        found.amount -= amount;
        if (found.amount <= 0) items.Remove(found);

        Save();
        OnWarehouseChanged?.Invoke();
        return true;
    }

    public void ClearAll()
    {
        items.Clear();
        Save();
        Flush();   // xoá sạch là hành động dứt khoát → ghi đĩa ngay, đừng để hồi lại
        OnWarehouseChanged?.Invoke();
    }

    /// <summary>
    /// Về trạng thái "chưa từng chơi": làm trống kho TRONG BỘ NHỚ rồi xoá save.
    ///
    /// Phải làm cả hai và theo đúng thứ tự này. Chỉ `PlayerPrefs.DeleteKey` là không đủ:
    /// `Load()` đã chạy ở Awake nên hàng vẫn nằm trong `items`, và lần `AddItem` kế tiếp
    /// sẽ `Save()` ghi lại y nguyên. Ngược lại, gọi `ClearAll()` cũng không đủ vì nó
    /// `Save()` → tạo lại key → cờ `DaCoSaveKho` vẫn true ⇒ không được cấp hạt khởi đầu.
    /// </summary>
    public void XoaSaveVaLamTrongKho()
    {
        items.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        _canGhi = false;
        OnWarehouseChanged?.Invoke();
        Debug.Log("[Warehouse] Đã làm trống kho và xoá save — coi như lần chơi đầu.");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Xoá save kho")]
    private void DebugXoaSave() => XoaSaveVaLamTrongKho();
#endif
}
