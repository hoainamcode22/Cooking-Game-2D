using UnityEngine;
using UnityEngine.UI;

public class MarketPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    private bool popupInputLockHeld;

    private void Start()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }
    }

    // true khi popup đang thực sự hiển thị
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            Transform parent = popupRoot.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);
                parent = parent.parent;
            }

            popupRoot.SetActive(true);
            AcquirePopupInputBlock();
        }
    }

    public void ClosePopup()
    {
        // Nếu MarketManager đang giữ lock (mở qua BuildingInteractable), delegate sang nó
        if (!popupInputLockHeld && MarketManager.Instance != null && MarketManager.Instance.IsOpen)
        {
            MarketManager.Instance.CloseMarketPopup();
            return;
        }

        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        GameObject root = popupRoot != null ? popupRoot : gameObject;
        Canvas parentCanvas = root.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, true);

        FarmInputLock.SetPopupRaycastBlock(root, true);
        FarmInputLock.IsMarketPopupOpen = true;

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        GameObject root = popupRoot != null ? popupRoot : gameObject;
        FarmInputLock.SetPopupRaycastBlock(root, false);

        Canvas parentCanvas = root.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, false);

        FarmInputLock.IsMarketPopupOpen = false;

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }
}
