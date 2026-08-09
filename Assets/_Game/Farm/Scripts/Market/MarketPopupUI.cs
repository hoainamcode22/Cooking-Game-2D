using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  LỚP VỎ TƯƠNG THÍCH — KHÔNG CÒN LOGIC RIÊNG
/// ══════════════════════════════════════════════════════════════════════════
///
/// LỖI 1 ĐÃ SỬA: bản cũ có `popupRoot.SetActive(false)` trong Start().
/// Cả script này và MarketManager cùng trỏ popupRoot vào Panel_Background, mà
/// Panel_Background để tắt sẵn trong scene ⇒ Start() CHỈ chạy đúng lúc popup vừa
/// được bật lên ⇒ vừa mở chợ đã tự đóng ngay. Dòng đó đã bị bỏ hẳn.
///
/// VÌ SAO không xoá luôn file: PopupManager, DisableStartupPopupsTool,
/// DemoL1L10Tool và MarketClickOpen đều tham chiếu kiểu này. Xoá file là scene
/// mất component (Missing Script) và ba tool Editor không biên dịch được.
/// Giữ lại nhưng uỷ quyền toàn bộ sang MarketManager để chỉ còn MỘT nguồn sự thật.
/// </summary>
// KHÔNG gắn [Obsolete] lên class: PopupManager / DisableStartupPopupsTool /
// DemoL1L10Tool đều khai báo kiểu này, gắn vào là ra một đống cảnh báo CS0618
// che mất cảnh báo thật.
public class MarketPopupUI : MonoBehaviour
{
    // Tên field phải giữ nguyên: DisableStartupPopupsTool đọc bằng
    // SerializedProperty("popupRoot"), đổi tên là tool im lặng bỏ qua popup này.
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    private void Start()
    {
        // KHÔNG SetActive(false) ở đây. Xem phần mô tả LỖI 1 phía trên.
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }
    }

    /// <summary>true khi popup đang thực sự hiển thị.</summary>
    public bool IsOpen
    {
        get
        {
            if (MarketManager.Instance != null)
                return MarketManager.Instance.IsOpen;

            return popupRoot != null && popupRoot.activeSelf;
        }
    }

    public void OpenPopup()
    {
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.OpenMarketPopup();
            return;
        }

        // Chỉ chạy khi scene chưa kịp Awake MarketManager — bật cả chuỗi cha đang tắt
        if (popupRoot == null)
            return;

        Transform parent = popupRoot.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
                parent.gameObject.SetActive(true);
            parent = parent.parent;
        }

        popupRoot.SetActive(true);
    }

    public void ClosePopup()
    {
        if (MarketManager.Instance != null)
        {
            MarketManager.Instance.CloseMarketPopup();
            return;
        }

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }
}
