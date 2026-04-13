using UnityEngine;

/// <summary>
/// Marker component — gắn lên bất kỳ building cố định nào của map
/// (ví dụ: gataulua, kho, chợ) để đảm bảo nó KHÔNG bao giờ bị ẩn
/// bởi FarmUIManager.HideAllPopups() hay bất kỳ popup-close nào.
///
/// Không cần code gì thêm ở đây — component này chỉ đóng vai trò cờ đánh dấu.
/// </summary>
public class PermanentBuilding : MonoBehaviour { }
