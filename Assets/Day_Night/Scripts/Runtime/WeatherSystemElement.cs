using UnityEngine;

// Giữ namespace HappyHarvest để tương thích với prefabs cũ (GUID giữ nguyên)
namespace HappyHarvest
{
    /// <summary>
    /// Legacy component - chỉ giữ để tương thích GUID với prefabs cũ (VFX_Cliffs).
    /// Không còn phụ thuộc HappyHarvest.WeatherSystem nữa.
    /// </summary>
    [DefaultExecutionOrder(999)]
    public class WeatherSystemElement : MonoBehaviour
    {
        [Tooltip("0=Sun, 1=Rain, 2=Thunder (legacy, không dùng nữa)")]
        public int WeatherType;
    }
}
