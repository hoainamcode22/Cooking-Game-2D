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

    private void OnMouseDown()
    {
        // Không xử lý khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        switch (buildingType)
        {
/*            case BuildingType.Warehouse:
                FarmUIManager.Instance?.ShowWarehouse();
                break;

            case BuildingType.Market:
                FarmUIManager.Instance?.ShowMarket();
                break;*/

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