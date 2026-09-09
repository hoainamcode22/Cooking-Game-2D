using UnityEngine;

/// <summary>
/// Dụng cụ bán ở tab thứ 4 "CÔNG CỤ" của Shop farm (vòng 14 — Hồ Câu).
/// Global namespace giống BaseItemData để ShopManager/ShopItemUI dùng thẳng, không cần using.
///
/// LƯU Ý TÊN FIELD: "unlockLevel" là tên BẮT BUỘC — ShopItemUI.GetUnlockLevel,
/// ShopManager.GetUnlockLevel và ShopLevelLockUI.GetUnlockLevel đều đọc bằng reflection
/// theo đúng chuỗi này. Đổi tên = mọi item ToolData tụt về "mở ở cấp 1".
///
/// RÌU (itemID "tool_axe") HIỆN CHƯA CÓ CHỨC NĂNG: mua xong chỉ vào kho như một món
/// thường, không chặt được gì cả. Sau này dùng cho việc khác (Sếp chốt 08/09).
/// durabilityUses là số ĐỀ XUẤT để dành sẵn, chưa hệ nào đọc.
/// </summary>
[CreateAssetMenu(fileName = "Tool_", menuName = "Farm Game/Tool Data")]
public class ToolData : BaseItemData
{
    [Header("Điều kiện mở")]
    [Tooltip("Cấp mở khoá mua. Tên field phải giữ nguyên 'unlockLevel' — shop đọc bằng reflection.")]
    [Min(1)] public int unlockLevel = 1;

    [Header("Loại dụng cụ")]
    [Tooltip("Khoá phân loại, ví dụ \"axe\" = rìu. Chưa hệ nào đọc — để dành cho chức năng sau.")]
    public string toolKind = "axe";

    [Header("Độ bền (ĐỀ XUẤT — chưa dùng)")]
    [Tooltip("Số lần dùng trước khi hỏng. Chưa có hệ tiêu hao, để dành cho chức năng sau.")]
    [Min(1)] public int durabilityUses = 50;

    [Header("Mô tả")]
    [TextArea] public string description;
}
