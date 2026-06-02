using UnityEngine;
using Village;

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
    [SerializeField] private HouseOrderPopupUI houseOrderPopup;
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
        if (blockingOverlay == null)
            return;

        bool anyOpen = IsAnyPopupOpen();
        if (anyOpen == _prevAnyOpen)
            return;

        _prevAnyOpen = anyOpen;
        blockingOverlay.blocksRaycasts = anyOpen;
        // alpha giá»¯ = 0 Ä‘á»ƒ overlay vÃ´ hÃ¬nh nhÆ°ng váº«n cháº·n raycast
    }
    

    public bool IsAnyPopupOpen()
    {
        return (warehousePopup    != null && warehousePopup.IsOpen)
            || (marketPopup       != null && marketPopup.IsOpen)
            || (trainProcessPopup != null && trainProcessPopup.IsOpen)
            || (trainLoadPopup    != null && trainLoadPopup.IsOpen)
            || (houseOrderPopup   != null && HouseOrderPopupUI.IsOpen)
            || (shopPopup         != null && shopPopup.IsOpen)
            || (ewarPopup         != null && ewarPopup.IsOpen);
    }
    
}
