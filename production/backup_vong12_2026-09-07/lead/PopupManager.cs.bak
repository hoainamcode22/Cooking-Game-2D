using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    // DÃ¹ng typed component reference Ä‘á»ƒ gá»i IsOpen tháº­t sá»± cá»§a tá»«ng popup,
    // trÃ¡nh lá»—i khi parent container luÃ´n activeInHierarchy.
    [Header("Block Click Popups")]
    [SerializeField] private WarehousePopupUI  warehousePopup;
    [SerializeField] private MarketPopupUI     marketPopup;
    [SerializeField] private TrainProcessPopupUI trainProcessPopup;
    [SerializeField] private TrainLoadPopupUI   trainLoadPopup;
    // (đã gỡ `houseOrderPopup`) — popup đơn hàng cũ của hệ nhà dân đã bị xoá cùng
    // `HouseOrderPopupUI`. Bảng đơn mới được hỏi qua `OrderBoardPopupUI.AnyOpen` ở cuối
    // `IsAnyPopupOpen()`, không cần ô kéo thả trong Inspector nữa.
    [SerializeField] private ShopManager       shopPopup;
    // Popup nhiá»‡m vá»¥ tÃ¢n thá»§ â€” Ä‘Äƒng kÃ½ Ä‘á»ƒ BlockMapPan vÃ  blockingOverlay hoáº¡t Ä‘á»™ng
    [SerializeField] private PopupEwarManager  ewarPopup;

    /// <summary>
    /// CanvasGroup full-screen trong suá»‘t náº±m dÆ°á»›i táº¥t cáº£ popup.
    /// Khi báº­t blocksRaycasts=true sáº½ cháº·n click xuyÃªn qua lá»›p popup xuá»‘ng world.
    /// GÃ¡n trong Inspector: táº¡o Image trong Canvas, kÃ©o dÃ i full-screen, alpha=0,
    /// Ä‘áº·t sort order tháº¥p hÆ¡n popup, gáº¯n CanvasGroup vÃ o Ä‘Ã¢y.
    /// </summary>
    [Header("Blocking Overlay")]
    [SerializeField] private CanvasGroup blockingOverlay;

    private bool _prevAnyOpen;

    private void Awake()
    {
        Instance = this;

        // Khá»Ÿi táº¡o overlay vá» tráº¡ng thÃ¡i khÃ´ng cháº·n
        if (blockingOverlay != null)
        {
            blockingOverlay.alpha          = 0f;
            blockingOverlay.blocksRaycasts = false;
            blockingOverlay.interactable   = false;
        }
    }


    private void LateUpdate()
    {
        bool anyOpen = IsAnyPopupOpen();

        // Self-healing: nếu popupLockCount hoặc input lock bị kẹt nhưng không có popup nào thực sự mở,
        // reset ngay lập tức để tránh khóa di chuyển camera / click map.
        if (!anyOpen
            && !FarmInputLock.IsDraggingSeed
            && !FarmInputLock.IsDraggingSickle)
        {
            FarmInputLock.ResetAll();
        }

        if (blockingOverlay != null)
        {
            if (anyOpen != _prevAnyOpen)
            {
                _prevAnyOpen = anyOpen;
                blockingOverlay.blocksRaycasts = anyOpen;
            }
        }
    }

    public bool IsAnyPopupOpen()
    {
        return (warehousePopup    != null && warehousePopup.gameObject.activeInHierarchy && warehousePopup.IsOpen)
            || (marketPopup       != null && marketPopup.gameObject.activeInHierarchy && marketPopup.IsOpen)
            || (trainProcessPopup != null && trainProcessPopup.gameObject.activeInHierarchy && trainProcessPopup.IsOpen)
            || (trainLoadPopup    != null && trainLoadPopup.gameObject.activeInHierarchy && trainLoadPopup.IsOpen)
            || (shopPopup         != null && shopPopup.gameObject.activeInHierarchy && shopPopup.IsOpen)
            || (ewarPopup         != null && ewarPopup.gameObject.activeInHierarchy && ewarPopup.IsOpen)
            || (WelfareEventManager.Instance  != null && WelfareEventManager.Instance.gameObject.activeInHierarchy && WelfareEventManager.Instance.IsOpen)
            || (AttendanceManager.Instance    != null && AttendanceManager.Instance.gameObject.activeInHierarchy && AttendanceManager.Instance.IsOpen)
            || (AvatarProfilePopupUI.Instance != null && AvatarProfilePopupUI.Instance.gameObject.activeInHierarchy && AvatarProfilePopupUI.Instance.IsOpen)
            || CropProcessPopupUI.AnyOpen
            || OrderBoardPopupUI.AnyOpen
            || StallPopupUI.AnyOpen
            || MillPopupUI.AnyOpen
            // POPUP CHO (21/08): field `marketPopup` trong scene dang NULL/chua gan —
            // F9 debug chup duoc canh popup cho MO nhung IsAnyPopupOpen van False,
            // blockingOverlay khong bat, click world xuyen qua popup cho. Doc thang
            // MarketManager.Instance de khong phu thuoc keo-tha Inspector.
            || (MarketManager.Instance != null && MarketManager.Instance.IsOpen)
            || (ExportTrainUIPackage.TrainStationMasterPopupUI.Instance != null && ExportTrainUIPackage.TrainStationMasterPopupUI.Instance.gameObject.activeInHierarchy)
            || (ExportTrainUIPackage.TrainLoadPopupUI.Instance != null && ExportTrainUIPackage.TrainLoadPopupUI.Instance.gameObject.activeInHierarchy)
            || (ExportTrainUIPackage.TrainProcessPopupUI.Instance != null && ExportTrainUIPackage.TrainProcessPopupUI.Instance.gameObject.activeInHierarchy)
            || UnifiedTaskPopupUI.IsOpenStatic
            // Hai popup dưới đây trước nay LỌT LƯỚI: chúng tự bật theo sự kiện /
            // đồng hồ, không có ô kéo-thả trong Inspector nên IsAnyPopupOpen() không
            // thấy → tutorial vẫn chạy đè lên chúng.
            || LevelUpPopupUI.IsActive
            || BoatAnnouncePopupUI.IsActive;
        // [ROLLBACK 2026-09-06] KHONG dua BuildingProcessPopupUI vao day.
        // IsAnyPopupOpen() duoc FarmInputLock.BlockMapPan dung => se chan TOAN BO
        // keo map va click world suot thoi gian popup tien do dang mo. Popup do neo
        // o world, khong che man hinh, khong can khoa. (Van liet ke o TenPopupDangMo.)
    }

    /// <summary>Trả tên popup đang mở (chuỗi rỗng nếu không có). Dùng để ghi log chẩn đoán.</summary>
    public static string TenPopupDangMo()
    {
        // Nhóm popup gắn qua Inspector — chỉ đọc được khi đã có Instance trong scene.
        PopupManager pm = Instance;
        if (pm != null)
        {
            if (pm.warehousePopup    != null && pm.warehousePopup.gameObject.activeInHierarchy    && pm.warehousePopup.IsOpen)    return "Kho";
            if (pm.marketPopup       != null && pm.marketPopup.gameObject.activeInHierarchy       && pm.marketPopup.IsOpen)       return "Cho";
            if (pm.trainProcessPopup != null && pm.trainProcessPopup.gameObject.activeInHierarchy && pm.trainProcessPopup.IsOpen) return "TrainProcess";
            if (pm.trainLoadPopup    != null && pm.trainLoadPopup.gameObject.activeInHierarchy    && pm.trainLoadPopup.IsOpen)    return "TrainLoad";
            if (pm.shopPopup         != null && pm.shopPopup.gameObject.activeInHierarchy         && pm.shopPopup.IsOpen)         return "Shop";
            if (pm.ewarPopup         != null && pm.ewarPopup.gameObject.activeInHierarchy         && pm.ewarPopup.IsOpen)         return "NhiemVuTanThu";
        }

        // Nhóm popup tự quản bằng singleton / cờ static — không cần Instance.
        if (WelfareEventManager.Instance  != null && WelfareEventManager.Instance.gameObject.activeInHierarchy  && WelfareEventManager.Instance.IsOpen)  return "Welfare";
        if (AttendanceManager.Instance    != null && AttendanceManager.Instance.gameObject.activeInHierarchy    && AttendanceManager.Instance.IsOpen)    return "DiemDanh";
        if (AvatarProfilePopupUI.Instance != null && AvatarProfilePopupUI.Instance.gameObject.activeInHierarchy && AvatarProfilePopupUI.Instance.IsOpen) return "AvatarProfile";
        if (CropProcessPopupUI.AnyOpen) return "CropProcess";
        if (OrderBoardPopupUI.AnyOpen)  return "OrderBoard";
        if (StallPopupUI.AnyOpen)       return "Stall";
        if (MillPopupUI.AnyOpen)        return "Mill";
        if (MarketManager.Instance != null && MarketManager.Instance.IsOpen) return "Cho";
        if (ExportTrainUIPackage.TrainStationMasterPopupUI.Instance != null && ExportTrainUIPackage.TrainStationMasterPopupUI.Instance.gameObject.activeInHierarchy) return "TrainStationMaster";
        if (ExportTrainUIPackage.TrainLoadPopupUI.Instance          != null && ExportTrainUIPackage.TrainLoadPopupUI.Instance.gameObject.activeInHierarchy)          return "ExportTrainLoad";
        if (ExportTrainUIPackage.TrainProcessPopupUI.Instance       != null && ExportTrainUIPackage.TrainProcessPopupUI.Instance.gameObject.activeInHierarchy)       return "ExportTrainProcess";
        if (UnifiedTaskPopupUI.IsOpenStatic) return "UnifiedTask";
        if (LevelUpPopupUI.IsActive)         return "LevelUp";
        if (BoatAnnouncePopupUI.IsActive)    return "BoatAnnounce";
        // [FIX 2026-09-06 B4]
        if (BuildingProcessPopupUI.Instance != null && BuildingProcessPopupUI.Instance.IsOpen) return "BuildingProcess";

        return string.Empty;
    }

    
}
