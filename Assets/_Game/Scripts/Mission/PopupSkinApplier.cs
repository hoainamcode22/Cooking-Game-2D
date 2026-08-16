using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MẶC ÁO MỚI CHO MỘT POPUP CÓ SẴN — gắn lên root của Kho / Hồ sơ / Shop.
///
/// Tool Editor quét hierarchy một lần và điền 4 danh sách bên dưới; bạn mở Inspector
/// XEM và GẠCH BỎ phần tử không muốn thay trước khi Play. Lúc chạy, component chỉ đổi
/// bề mặt (sprite/màu/lớp trang trí) đúng theo danh sách — không đổi cấu trúc, không
/// đổi kích thước, không đụng bất kỳ onClick hay [SerializeField] nào của logic.
///
/// Nút được PHÂN LOẠI THEO MÀU HIỆN TẠI để giữ đúng ý nghĩa cũ:
///   xanh lá → nút Chính (Mua/Nhận/Giao)   · vàng cam → nút phụ (Làm mới, Nâng cấp)
///   xanh dương → Kim cương                 · đỏ → Huỷ/Gỡ   · xám → khoá/vô hiệu
/// Màu đích lấy từ bảng "Ngôn ngữ thị giác dùng chung" trong README của nhà thiết kế.
/// </summary>
[DisallowMultipleComponent]
public class PopupSkinApplier : MonoBehaviour
{
    [Header("Điền bởi Tools ▸ Farm ▸ Thay Áo Popup — xem & gạch bỏ được")]
    [Tooltip("Nền ngoài cùng → ván gỗ nâu.")]
    public List<Image> vanGo = new List<Image>();

    [Tooltip("Panel lớn bên trong → giấy kem.")]
    public List<Image> giay = new List<Image>();

    [Tooltip("Thẻ/ô/hàng nội dung → nền kem sáng viền vàng nhạt.")]
    public List<Image> the = new List<Image>();

    [Tooltip("Mọi nút — tự phân loại theo màu hiện tại lúc áp.")]
    public List<Button> nut = new List<Button>();

    [Header("Tuỳ chỉnh")]
    [Tooltip("Tắt để popup này giữ nguyên áo cũ mà không phải gỡ component.")]
    public bool batAo = false;

    private bool _daAp;

    private void OnEnable()
    {
        // Tắt hoàn toàn can thiệp runtime để giữ 100% UI gốc do designer thiết kế
        return;
    }

    /// <summary>Áp toàn bộ. Chạy lại không nhân đôi lớp trang trí (SkinKit tự kiểm tên Skin_).</summary>
    public void ApDung()
    {
        foreach (var i in vanGo) if (i != null) SkinKit.MacAoVanGo(i);
        foreach (var i in giay)  if (i != null) SkinKit.MacAoGiay(i);
        foreach (var i in the)   if (i != null) SkinKit.MacAoThe(i);

        foreach (var b in nut)
        {
            if (b == null) continue;
            SkinKit.MacAoNut(b, PhanLoaiNut(b));
        }
    }

    /// <summary>
    /// Đoán vai trò nút từ MÀU đang có — vì màu cũ chính là cách dev cũ đánh dấu ý
    /// nghĩa (xanh = xác nhận, đỏ = huỷ…). Đổi sang bảng mới nhưng giữ nguyên vai trò.
    /// </summary>
    private static TaskPopupDesign.KieuNut PhanLoaiNut(Button b)
    {
        Color c = b.image != null ? b.image.color : Color.gray;

        // Xám/không màu: nút vô hiệu hoặc nền trung tính.
        float max = Mathf.Max(c.r, c.g, c.b), min = Mathf.Min(c.r, c.g, c.b);
        if (max - min < 0.12f) return SkinKit.NutXam;

        if (c.g > c.r && c.g > c.b) return TaskPopupDesign.NutNhan;      // xanh lá
        if (c.b > c.r && c.b > c.g) return SkinKit.NutKimCuong;          // xanh dương
        if (c.r > 0.6f && c.g > 0.45f && c.b < 0.45f) return TaskPopupDesign.NutDiLam; // vàng cam
        if (c.r > c.g && c.r > c.b) return SkinKit.NutDo;                // đỏ

        return TaskPopupDesign.NutDiLam;
    }
}
