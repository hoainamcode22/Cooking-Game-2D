using UnityEngine;

public class HomeMenuController : MonoBehaviour
{
    [Header("Kéo dải băng rôn (Panel_Items) vào đây")]
    public GameObject panelItems;

    void Start()
    {
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
            panelItems.SetActive(!isCurrentlyOpen);
        }
    }
}