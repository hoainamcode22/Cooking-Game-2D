using UnityEngine;

public class BuildingInteractable : MonoBehaviour
{
    public enum BuildingType
    {
        Warehouse,
        Market,
        CookingGate,
        SeedShop
    }

    [SerializeField] private BuildingType buildingType;
    [SerializeField] private MarketManager marketManager;

    private void OnMouseDown()
    {
        // Không mở popup khi đang Edit Mode
        if (EditModeManager.IsEditMode) return;

        if (FarmInputLock.BlockMapPan) return;

        // Không xử lý khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        switch (buildingType)
        {
            case BuildingType.Market:
                if (marketManager == null)
                    marketManager = MarketManager.Instance;

                if (marketManager != null)
                    marketManager.OpenMarketPopup();
                break;

            case BuildingType.CookingGate:
                // A6 — khoá tới cấp 5. Món có unlockLevel thấp nhất là cấp 5, nên vào
                // sớm hơn là mở ra màn hình bếp KHÔNG CÓ MÓN NÀO chọn được.
                // Con số 5 nằm ở `CookingGateAccess`, không gõ lại ở đây.
                if (!CookingGateAccess.CanEnterOrWarn())
                    break;

                if (FarmUIManager.Instance != null)
                    FarmUIManager.Instance.OnClick_GoCooking();
                break;

            case BuildingType.SeedShop:
                FarmUIManager.Instance?.ShowHint("Mở shop hạt giống.");
                break;
        }
    }
}
