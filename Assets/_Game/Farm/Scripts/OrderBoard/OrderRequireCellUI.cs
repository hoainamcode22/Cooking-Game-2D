using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MỘT Ô TRÊN LƯỚI YÊU CẦU 3x2 CỦA CỘT PHẢI (B7).
///
/// ⚠ CÁCH ĐỌC CON SỐ — chi tiết dễ làm sai nhất của cả màn hình:
/// ô hiện <c>có/cần</c>, ví dụ <c>6/2</c> nghĩa là "kho đang có 6, đơn cần 2".
/// KHÔNG phải <c>2/2</c> kiểu tiến độ.
///
/// Vì sao chọn kiểu này: nó rộng lượng hơn hẳn. Người chơi thấy luôn mình đang dư bao
/// nhiêu mà không phải thoát popup mở kho đếm. Số vượt mức (<c>8/1</c>) vẫn hiện đúng
/// chứ không cắt về <c>1/1</c> — cắt đi là giấu mất thông tin duy nhất có giá trị.
///
/// Dấu gạch dùng ký tự <c>/</c> ASCII. KHÔNG dùng dấu phân số U+2044 hay U+2215: font
/// mặc định của dự án là LiberationSans SDF kiểu Static 250 ký tự, thiếu là ra ô vuông rỗng.
/// </summary>
public class OrderRequireCellUI : MonoBehaviour
{
    [Header("Hai nhánh trạng thái")]
    [SerializeField] private GameObject stateFilledRoot;
    [Tooltip("Ô chưa dùng tới của lưới 3x2 — khung viền nét đứt.")]
    [SerializeField] private GameObject stateEmptyRoot;

    [Header("Nội dung")]
    [Tooltip("Icon vật phẩm. Chờ art thì lùi về khối màu phẳng.")]
    [SerializeField] private Image     imageArtItemIcon;
    [SerializeField] private TMP_Text  textAmount;
    [SerializeField] private TMP_Text  textName;

    [Tooltip("Dấu tích xanh — chỉ hiện khi đã đủ món này.")]
    [SerializeField] private GameObject checkBadge;

    [Tooltip("Nền ô — đổi màu nhẹ khi đã đủ, để liếc qua thấy ngay còn thiếu món nào.")]
    [SerializeField] private Image imageArtCellBackground;

    [Header("Bảng màu")]
    [SerializeField] private Color colorCellNormal = new Color(0.18f, 0.25f, 0.19f, 1f);
    [SerializeField] private Color colorCellEnough = new Color(0.20f, 0.36f, 0.22f, 1f);
    [SerializeField] private Color colorAmountEnough = new Color(0.62f, 0.90f, 0.55f, 1f);
    [SerializeField] private Color colorAmountLack   = new Color(0.95f, 0.62f, 0.50f, 1f);

    /// <summary>Ô chưa dùng tới trong lưới 6 ô.</summary>
    public void ShowEmpty()
    {
        SetActiveSafe(stateFilledRoot, false);
        SetActiveSafe(stateEmptyRoot,  true);
    }

    /// <summary>Vẽ một dòng yêu cầu.</summary>
    public void Show(OrderBoardRequirementView req)
    {
        if (req == null) { ShowEmpty(); return; }

        SetActiveSafe(stateFilledRoot, true);
        SetActiveSafe(stateEmptyRoot,  false);

        bool enough = req.IsEnough;

        if (imageArtItemIcon != null)
        {
            Sprite icon = OrderBoardIconResolver.GetIcon(req.itemId);
            imageArtItemIcon.sprite = icon;

            // Không có icon thì KHÔNG để hình trắng vô nghĩa: tắt sprite và tô một khối
            // màu suy ra từ itemId. Người chơi vẫn phân biệt được ba ô cạnh nhau, và
            // tên + con số bên dưới mới là thứ mang thông tin.
            if (icon != null)
            {
                imageArtItemIcon.color = Color.white;
            }
            else
            {
                imageArtItemIcon.color = OrderBoardIconResolver.TintFromId(req.itemId);
            }
        }

        // "có/cần" — vế trái là kho, vế phải là yêu cầu.
        if (textAmount != null)
        {
            textAmount.text  = req.ownedAmount + "/" + req.needAmount;
            textAmount.color = enough ? colorAmountEnough : colorAmountLack;
        }

        if (textName != null) textName.text = req.ResolveDisplayName();

        SetActiveSafe(checkBadge, enough);

        if (imageArtCellBackground != null)
            imageArtCellBackground.color = enough ? colorCellEnough : colorCellNormal;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
