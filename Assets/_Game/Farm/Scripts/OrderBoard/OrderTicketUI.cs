using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT PHIẾU TRÊN LƯỚI 3x3 (B4 + B5).
///
/// BỐN trạng thái, dựng sẵn thành nhánh con trong prefab rồi bật/tắt — KHÔNG dựng bằng
/// code lúc chạy. Chủ dự án mở prefab `PF_OrderTicket` là kéo art vào được từng trạng
/// thái mà không phải đọc một dòng C# nào (bài học `UnifiedTaskPopupUI` 1433 dòng).
///
///   1. Chưa đủ hàng  → giấy TRẮNG NGÀ
///   2. Đủ hàng       → giấy XANH LÁ + dấu tích to góc trên phải
///   3. Đang chọn     → thêm KHUNG PHÁT SÁNG VÀNG bao ngoài (chồng lên 1 hoặc 2)
///   4. Ô trống       → khung VIỀN NÉT ĐỨT, không có giấy
///
/// ⚠ PHIẾU CHỈ HIỆN PHẦN THƯỞNG (sao EXP + đồng vàng), TUYỆT ĐỐI KHÔNG hiện yêu cầu.
/// Đây là ý đồ thiết kế cốt lõi của bản tham chiếu: người chơi quét mắt cả lưới để tìm
/// đơn ĐÁNG GIÁ NHẤT trước, rồi mới bấm vào xem cần những gì. Nhồi cả yêu cầu lên phiếu
/// thì lưới rối và mất hẳn nhịp "chọn" — mà nhịp chọn mới là thứ giữ người chơi ở lại
/// popup này. Ai định thêm dòng nguyên liệu lên phiếu, đọc lại đoạn này trước.
/// </summary>
public class OrderTicketUI : MonoBehaviour
{
    [Header("Hai nhánh trạng thái (bật/tắt, không dựng bằng code)")]
    [SerializeField] private GameObject stateFilledRoot;
    [SerializeField] private GameObject stateEmptyRoot;

    [Header("Chỗ chờ art")]
    [Tooltip("Tờ giấy — ĐỔI MÀU theo trạng thái đủ/thiếu hàng.")]
    [SerializeField] private Image imageArtPaper;
    [Tooltip("Đinh ghim ở mép trên tờ giấy.")]
    [SerializeField] private Image imageArtPin;

    [Header("Phần thưởng (thứ DUY NHẤT hiện trên phiếu)")]
    [SerializeField] private TMP_Text textExp;
    [SerializeField] private TMP_Text textGold;

    [Header("Dấu hiệu trạng thái")]
    [Tooltip("Dấu tích to góc trên phải — chỉ hiện khi giao được.")]
    [SerializeField] private GameObject checkBadge;
    [Tooltip("Khung phát sáng vàng — chỉ hiện khi phiếu đang được chọn.")]
    [SerializeField] private GameObject selectedGlow;

    [Header("Vùng bấm")]
    [SerializeField] private Button button;

    [Header("Bảng màu giấy")]
    [Tooltip("Chưa đủ hàng.")]
    [SerializeField] private Color colorPaperNormal = new Color(0.96f, 0.94f, 0.86f, 1f);
    [Tooltip("Đủ hàng, giao được.")]
    [SerializeField] private Color colorPaperReady = new Color(0.60f, 0.83f, 0.51f, 1f);
    [Tooltip("Màu chữ trên giấy trắng ngà.")]
    [SerializeField] private Color colorTextNormal = new Color(0.24f, 0.21f, 0.16f, 1f);
    [Tooltip("Màu chữ trên giấy xanh — đậm hơn để vẫn đọc được.")]
    [SerializeField] private Color colorTextReady = new Color(0.11f, 0.26f, 0.10f, 1f);

    [Tooltip("Đinh ghim trên giấy trắng ngà.")]
    [SerializeField] private Color colorPinNormal = new Color(0.91f, 0.71f, 0.30f, 1f);
    [Tooltip("Đinh ghim trên giấy xanh — đổi tông để không chìm vào nền.")]
    [SerializeField] private Color colorPinReady = new Color(0.99f, 0.86f, 0.45f, 1f);

    private OrderBoardPopupUI _owner;
    private int               _slotIndex = -1;
    private string            _orderId;

    /// <summary>Vị trí gốc trên lưới — popup ghi vào lúc mở, dùng lại khi dồn lưới (B9).</summary>
    public Vector2 HomePosition { get; set; }

    public RectTransform Rect => (RectTransform)transform;
    public string OrderId => _orderId;
    public int    SlotIndex => _slotIndex;
    public bool   HasOrder => !string.IsNullOrEmpty(_orderId);

    /// <summary>Gắn phiếu vào popup. Gọi MỘT LẦN ngay sau khi Instantiate prefab.</summary>
    public void Bind(OrderBoardPopupUI owner, int slotIndex)
    {
        _owner     = owner;
        _slotIndex = slotIndex;

        // Dọn listener trước khi gắn: prefab có thể đã lưu sẵn listener từ Inspector, và
        // phiếu được dùng lại giữa các lần mở popup. Không dọn thì mỗi lần mở lại chồng
        // thêm một đăng ký, một cú bấm bắn ra nhiều lần.
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (_owner == null) return;
        if (!HasOrder) return;   // ô trống không bấm được — bấm vào chỉ gây bối rối
        _owner.OnTicketClicked(_slotIndex);
    }

    /// <summary>Ô trống — trạng thái 4.</summary>
    public void ShowEmpty()
    {
        _orderId = null;

        SetActiveSafe(stateFilledRoot, false);
        SetActiveSafe(stateEmptyRoot,  true);
        SetActiveSafe(selectedGlow,    false);

        if (button != null) button.interactable = false;
    }

    /// <summary>Vẽ một đơn lên phiếu. Trạng thái 1/2/3 quyết định ở đây.</summary>
    public void ShowOrder(OrderBoardOrderView order, bool isSelected)
    {
        if (order == null) { ShowEmpty(); return; }

        _orderId = order.orderId;

        SetActiveSafe(stateFilledRoot, true);
        SetActiveSafe(stateEmptyRoot,  false);

        if (button != null) button.interactable = true;

        bool ready = order.CanDeliverNow();

        if (imageArtPaper != null)
            imageArtPaper.color = ready ? colorPaperReady : colorPaperNormal;

        // Đinh ghim đổi tông theo màu giấy. Giữ nguyên một màu thì trên tờ xanh nó gần
        // như biến mất, và cả lưới mất đi chi tiết duy nhất khiến phiếu trông như giấy
        // ghim thật chứ không phải ô vuông màu.
        if (imageArtPin != null)
            imageArtPin.color = ready ? colorPinReady : colorPinNormal;

        Color textColor = ready ? colorTextReady : colorTextNormal;
        if (textExp  != null) { textExp.text  = order.rewardExp.ToString();  textExp.color  = textColor; }
        if (textGold != null) { textGold.text = order.rewardGold.ToString(); textGold.color = textColor; }

        SetActiveSafe(checkBadge,   ready);
        SetActiveSafe(selectedGlow, isSelected);
    }

    /// <summary>Bật/tắt riêng khung sáng — dùng khi chỉ đổi phiếu đang chọn, khỏi vẽ lại cả lưới.</summary>
    public void SetSelected(bool selected)
    {
        SetActiveSafe(selectedGlow, selected && HasOrder);
    }

    /// <summary>Ẩn tức thì (phiếu vừa được giao) — khói sẽ bung ra đúng chỗ này.</summary>
    public void HideForDeliverFx()
    {
        SetActiveSafe(stateFilledRoot, false);
        SetActiveSafe(stateEmptyRoot,  false);
        SetActiveSafe(selectedGlow,    false);
    }

    /// <summary>
    /// Đặt thẳng toạ độ trên lưới. Popup gọi hàm này chứ không để GridLayoutGroup tự đặt,
    /// vì hiệu ứng "lưới dồn lại" (B9) cần trượt phiếu giữa hai ô — mà layout group thì
    /// mỗi khung hình lại kéo phiếu về đúng ô của nó, hiệu ứng không bao giờ thấy được.
    /// </summary>
    public void SetGridPosition(Vector2 anchoredPos)
    {
        Rect.anchoredPosition = anchoredPos;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
