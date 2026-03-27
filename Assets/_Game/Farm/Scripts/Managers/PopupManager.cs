using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance;

    [Header("Block Click Popups")]
    [SerializeField] private GameObject warehousePopup;
    [SerializeField] private GameObject pigPenPopup;
    [SerializeField] private GameObject chickenPenPopup;
    [SerializeField] private GameObject cowPenPopup;
    [SerializeField] private GameObject marketPopup;

    private void Awake()
    {
        Instance = this;
        Debug.Log("[PopupManager] Awake");
    }

    public bool IsAnyPopupOpen()
    {
        return IsOpen(warehousePopup)
            || IsOpen(pigPenPopup)
            || IsOpen(chickenPenPopup)
            || IsOpen(cowPenPopup)
            || IsOpen(marketPopup);
    }

    private bool IsOpen(GameObject go)
    {
        return go != null && go.activeInHierarchy;
    }
}