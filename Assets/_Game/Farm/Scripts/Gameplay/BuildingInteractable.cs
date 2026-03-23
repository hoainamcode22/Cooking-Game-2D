using UnityEngine;
using UnityEngine.SceneManagement;

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
        switch (buildingType)
        {
/*            case BuildingType.Warehouse:
                FarmUIManager.Instance?.ShowWarehouse();
                break;

            case BuildingType.Market:
                FarmUIManager.Instance?.ShowMarket();
                break;*/

            case BuildingType.CookingGate:
                SceneManager.LoadScene("SCN_Cooking");
                break;

            case BuildingType.SeedShop:
                FarmUIManager.Instance?.ShowHint("Mở shop hạt giống.");
                break;
        }
    }
}