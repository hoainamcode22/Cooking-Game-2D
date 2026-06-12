using UnityEngine;
using UnityEngine.UI;

public class HomeMenuManager : MonoBehaviour
{
    public static HomeMenuManager Instance { get; private set; }
    public bool IsOpen => panel_Items != null && panel_Items.activeSelf;

    [Header("Home Button")]
    [SerializeField] private Button     btn_Home;

    [Header("Panel chứa dải nút")]
    [SerializeField] private GameObject panel_Items;

    [Header("Nút Ewar (nằm trong panel_Items)")]
    [SerializeField] private Button     btn_Ewar;

    [Header("Popup Manager")]
    [SerializeField] private PopupEwarManager popupEwarManager;
    private bool popupInputLockHeld;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        btn_Home.onClick.AddListener(OnHomeClicked);
        btn_Ewar.onClick.AddListener(OnEwarClicked);

        panel_Items.SetActive(false);
    }

    private void OnHomeClicked()
    {
        bool shouldOpen = !panel_Items.activeSelf;
        panel_Items.SetActive(shouldOpen);

        if (shouldOpen)
            AcquirePopupInputBlock();
        else
            ReleasePopupInputBlock();
    }

    private void OnEwarClicked()
    {
        ReleasePopupInputBlock();
        panel_Items.SetActive(false);
        popupEwarManager.OpenPopup();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(panel_Items, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(panel_Items, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }
}
