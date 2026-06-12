using UnityEngine;

public class HomeMenuController : MonoBehaviour
{
    public static HomeMenuController Instance { get; private set; }
    public bool IsOpen => panelItems != null && panelItems.activeSelf;

    [Header("Kéo dải băng rôn (Panel_Items) vào đây")]
    public GameObject panelItems;
    private bool popupInputLockHeld;

    void Start()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        // Đảm bảo khi mới chạy game, menu băng rôn luôn ở trạng thái đóng
        if (panelItems != null)
        {
            panelItems.SetActive(false);
        }
    }

    // Hàm này dùng để gán vào sự kiện OnClick của Btn_Home
    public void ToggleMenu()
    {
        if (panelItems != null)
        {
            // Kiểm tra trạng thái hiện tại (đang ẩn hay hiện)
            bool isCurrentlyOpen = panelItems.activeSelf;
            
            // Đảo ngược trạng thái: đang mở thì đóng, đang đóng thì mở
            bool shouldOpen = !isCurrentlyOpen;
            panelItems.SetActive(shouldOpen);

            if (shouldOpen)
                AcquirePopupInputBlock();
            else
                ReleasePopupInputBlock();
        }
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(panelItems, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(panelItems, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }
}
