using UnityEngine;
using UnityEngine.UI;

public class HomeMenuManager : MonoBehaviour
{
    [Header("Home Button")]
    [SerializeField] private Button     btn_Home;

    [Header("Panel chứa dải nút")]
    [SerializeField] private GameObject panel_Items;

    [Header("Nút Ewar (nằm trong panel_Items)")]
    [SerializeField] private Button     btn_Ewar;

    [Header("Popup Manager")]
    [SerializeField] private PopupEwarManager popupEwarManager;

    private void Awake()
    {
        btn_Home.onClick.AddListener(OnHomeClicked);
        btn_Ewar.onClick.AddListener(OnEwarClicked);

        panel_Items.SetActive(false);
    }

    private void OnHomeClicked()
    {
        panel_Items.SetActive(!panel_Items.activeSelf);
    }

    private void OnEwarClicked()
    {
        panel_Items.SetActive(false);
        popupEwarManager.OpenPopup();
    }
}
