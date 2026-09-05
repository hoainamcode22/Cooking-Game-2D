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
            || UnifiedTaskPopupUI.IsOpenStatic;
    }
    
}
