using UnityEngine;
using Village;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    // Dùng typed component reference để gọi IsOpen thật sự của từng popup,
    // tránh lỗi khi parent container luôn activeInHierarchy.
    [Header("Block Click Popups")]
    [SerializeField] private WarehousePopupUI  warehousePopup;
    [SerializeField] private PigPenPopupUI     pigPenPopup;
    [SerializeField] private ChickenPenPopupUI chickenPenPopup;
    [SerializeField] private CowPenPopupUI     cowPenPopup;
    [SerializeField] private MarketPopupUI     marketPopup;
    [SerializeField] private TrainProcessPopupUI trainProcessPopup;
    [SerializeField] private TrainLoadPopupUI   trainLoadPopup;
    [SerializeField] private HouseOrderPopupUI houseOrderPopup;
    [SerializeField] private ShopManager       shopPopup;

    /// <summary>
    /// CanvasGroup full-screen trong suốt nằm dưới tất cả popup.
    /// Khi bật blocksRaycasts=true sẽ chặn click xuyên qua lớp popup xuống world.
    /// Gán trong Inspector: tạo Image trong Canvas, kéo dài full-screen, alpha=0,
    /// đặt sort order thấp hơn popup, gắn CanvasGroup vào đây.
    /// </summary>
    [Header("Blocking Overlay")]
    [SerializeField] private CanvasGroup blockingOverlay;

    private bool _prevAnyOpen;

    private void Awake()
    {
        Instance = this;
        Debug.Log("[PopupManager] Awake");

        // Khởi tạo overlay về trạng thái không chặn
        if (blockingOverlay != null)
        {
            blockingOverlay.alpha          = 0f;
            blockingOverlay.blocksRaycasts = false;
            blockingOverlay.interactable   = false;
        }
    }


    private void LateUpdate()
    {
        if (blockingOverlay == null)
            return;

        bool anyOpen = IsAnyPopupOpen();
        if (anyOpen == _prevAnyOpen)
            return;

        _prevAnyOpen = anyOpen;
        blockingOverlay.blocksRaycasts = anyOpen;
        // alpha giữ = 0 để overlay vô hình nhưng vẫn chặn raycast
    }
    

    public bool IsAnyPopupOpen()
    {
        return (warehousePopup    != null && warehousePopup.IsOpen)
            || (pigPenPopup       != null && pigPenPopup.IsOpen)
            || (chickenPenPopup   != null && chickenPenPopup.IsOpen)
            || (cowPenPopup       != null && cowPenPopup.IsOpen)
            || (marketPopup       != null && marketPopup.IsOpen)
            || (trainProcessPopup != null && trainProcessPopup.IsOpen)
            || (trainLoadPopup    != null && trainLoadPopup.IsOpen)
            || (houseOrderPopup   != null && HouseOrderPopupUI.IsOpen)
            || (shopPopup         != null && shopPopup.IsOpen);
    }
    
}