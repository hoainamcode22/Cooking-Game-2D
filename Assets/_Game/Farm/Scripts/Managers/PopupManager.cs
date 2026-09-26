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

    // [FIX 2026-09-18 P0 — PROFILER] IsAnyPopupOpen() co 82 call site, moi HouseGrowthController.Update()
    // goi 2 lan/frame qua FarmInputLock, CameraController goi them. Ben trong lai co IsFishingPopupOpen()
    // dung Type.GetType (reflection) => Profiler do: HouseGrowthController.Update 61ms, CameraController 25ms.
    // Cache ket qua theo frameCount: du bao nhieu nguoi goi, moi frame chi tinh dung mot lan.
    private int  _frameCacheAnyOpen = -1;
    private bool _giaTriCacheAnyOpen;

    public bool IsAnyPopupOpen()
    {
        int f = Time.frameCount;
        if (f == _frameCacheAnyOpen) return _giaTriCacheAnyOpen;
        _frameCacheAnyOpen = f;
        _giaTriCacheAnyOpen = TinhIsAnyPopupOpen();
        return _giaTriCacheAnyOpen;
    }

    // [DO 2026-09-21] Profiler: PopupManager.LateUpdate 7.14ms SELF ma moi property doc ra deu re.
    // Tach thanh 5 nhom co marker de Hierarchy chi dung nhom.
    private static readonly UnityEngine.Profiling.CustomSampler _spA = UnityEngine.Profiling.CustomSampler.Create("PM.A_Inspector");
    private static readonly UnityEngine.Profiling.CustomSampler _spB = UnityEngine.Profiling.CustomSampler.Create("PM.B_Singleton");
    private static readonly UnityEngine.Profiling.CustomSampler _spC = UnityEngine.Profiling.CustomSampler.Create("PM.C_AnyOpenStatic");
    private static readonly UnityEngine.Profiling.CustomSampler _spD = UnityEngine.Profiling.CustomSampler.Create("PM.D_Train");
    private static readonly UnityEngine.Profiling.CustomSampler _spE = UnityEngine.Profiling.CustomSampler.Create("PM.E_Fishing");

    private bool TinhIsAnyPopupOpen()
    {
        bool r;
        _spA.Begin();
        r = (warehousePopup    != null && warehousePopup.gameObject.activeInHierarchy && warehousePopup.IsOpen)
         || (marketPopup       != null && marketPopup.gameObject.activeInHierarchy && marketPopup.IsOpen)
         || (trainProcessPopup != null && trainProcessPopup.gameObject.activeInHierarchy && trainProcessPopup.IsOpen)
         || (trainLoadPopup    != null && trainLoadPopup.gameObject.activeInHierarchy && trainLoadPopup.IsOpen)
         || (shopPopup         != null && shopPopup.gameObject.activeInHierarchy && shopPopup.IsOpen)
         || (ewarPopup         != null && ewarPopup.gameObject.activeInHierarchy && ewarPopup.IsOpen);
        _spA.End();
        if (r) return true;

        _spB.Begin();
        r = (WelfareEventManager.Instance  != null && WelfareEventManager.Instance.gameObject.activeInHierarchy && WelfareEventManager.Instance.IsOpen)
         || (AttendanceManager.Instance    != null && AttendanceManager.Instance.gameObject.activeInHierarchy && AttendanceManager.Instance.IsOpen)
         || (AvatarProfilePopupUI.Instance != null && AvatarProfilePopupUI.Instance.gameObject.activeInHierarchy && AvatarProfilePopupUI.Instance.IsOpen)
         || (MarketManager.Instance != null && MarketManager.Instance.IsOpen)
         || UnifiedTaskPopupUI.IsOpenStatic
         || LevelUpPopupUI.IsActive
         || BoatAnnouncePopupUI.IsActive;
        _spB.End();
        if (r) return true;

        _spC.Begin();
        r = OrderBoardPopupUI.AnyOpen || StallPopupUI.AnyOpen || MillPopupUI.AnyOpen;
        _spC.End();
        if (r) return true;

        _spD.Begin();
        r = (ExportTrainUIPackage.TrainStationMasterPopupUI.Instance != null && ExportTrainUIPackage.TrainStationMasterPopupUI.Instance.gameObject.activeInHierarchy)
         || (ExportTrainUIPackage.TrainLoadPopupUI.Instance != null && ExportTrainUIPackage.TrainLoadPopupUI.Instance.gameObject.activeInHierarchy)
         || (ExportTrainUIPackage.TrainProcessPopupUI.Instance != null && ExportTrainUIPackage.TrainProcessPopupUI.Instance.gameObject.activeInHierarchy);
        _spD.End();
        if (r) return true;

        _spE.Begin();
        r = IsFishingPopupOpen();
        _spE.End();
        return r;
        // [ROLLBACK 2026-09-06] KHONG dua BuildingProcessPopupUI vao day (xem ghi chu cu).
    }

    private static bool _daTraCuuFishing;
    private static System.Reflection.PropertyInfo _propFishingEntry;
    private static System.Reflection.PropertyInfo _propFishCounter;
    // [PERF 2026-09-26] Profiler: PM.E_Fishing 6.3ms/frame. AnyOpen cua popup cau ca goi FindFirstObjectByType(Include)
    // MOI FRAME khi scene Farm khong co popup cau ca. Nay doc 'Instance' truoc: null = popup chua tung bat = chac chan dang dong.
    private static System.Reflection.PropertyInfo _propFishingEntryInst;
    private static System.Reflection.PropertyInfo _propFishCounterInst;

    private static void TraCuuFishingMotLan()
    {
        if (_daTraCuuFishing) return;
        _daTraCuuFishing = true;
        const System.Reflection.BindingFlags co = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
        try
        {
            var t1 = System.Type.GetType("FarmGame.Fishing.FishingEntryPopupUI, Assembly-CSharp");
            if (t1 != null) { _propFishingEntry = t1.GetProperty("AnyOpen", co); _propFishingEntryInst = t1.GetProperty("Instance", co); }
            var t2 = System.Type.GetType("FarmGame.Fishing.FishCounterPopupUI, Assembly-CSharp");
            if (t2 != null) { _propFishCounter = t2.GetProperty("AnyOpen", co); _propFishCounterInst = t2.GetProperty("Instance", co); }
        }
        catch { /* khong co fishing => khong co popup fishing */ }
    }

    private static bool IsFishingPopupOpen()
    {
        TraCuuFishingMotLan();
        if (_propFishingEntry == null && _propFishCounter == null) return false;
        try
        {
            if (_propFishingEntry != null && CoInstance(_propFishingEntryInst) && (bool)_propFishingEntry.GetValue(null)) return true;
            if (_propFishCounter  != null && CoInstance(_propFishCounterInst)  && (bool)_propFishCounter.GetValue(null))  return true;
        }
        catch { }
        return false;
    }

    private static bool CoInstance(System.Reflection.PropertyInfo p)
    {
        if (p == null) return true;                          // khong doc duoc Instance -> hanh vi cu
        var o = p.GetValue(null) as UnityEngine.Object;
        return o != null;
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
