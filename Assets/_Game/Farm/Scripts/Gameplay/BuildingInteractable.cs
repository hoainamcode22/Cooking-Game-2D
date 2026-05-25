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
                if (FarmUIManager.Instance != null)
                    FarmUIManager.Instance.OnClick_GoCooking();
                break;

            case BuildingType.SeedShop:
                FarmUIManager.Instance?.ShowHint("Mở shop hạt giống.");
                break;
        }
    }
}
