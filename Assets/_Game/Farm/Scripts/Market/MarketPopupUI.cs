using UnityEngine;
using UnityEngine.UI;

public class MarketPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    private void Start()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }
    }

    public void OpenPopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }
}