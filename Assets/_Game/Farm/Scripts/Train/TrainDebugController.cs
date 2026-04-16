using UnityEngine;

// TrainDebugController đã bị vô hiệu hoá.
// Các method DepartToProcess / ReturnToWait / ResetMove dùng path cũ (point00→01→02)
// gây xung đột với flow mới. Không được kích hoạt lại.
public class TrainDebugController : MonoBehaviour
{
    // Tất cả logic debug đã bị xoá để tránh gọi nhầm method cũ.
}
