using UnityEngine;

namespace Day_Night
{
    [ExecuteAlways]
    [DefaultExecutionOrder(999)]
    public class DayNightShadowInstance : MonoBehaviour
    {
        [Range(0f, 10f)] public float BaseLength = 1f;

        private DayNightCycleController controller;

        private void LateUpdate()
        {
            if (controller == null)
                controller = FindFirstObjectByType<DayNightCycleController>();

            float time = controller != null ? controller.CurrentDayRatio : 0.5f;

            // Góc khớp với CCW LightsRoot rotation (DayLight bắt đầu ở dưới, y=-25)
            // t=0.25 (bình minh): DayLight bên PHẢI -> bóng chỉ TRÁI = 90°
            // t=0.50 (trưa):      DayLight bên TRÊN -> bóng chỉ XUỐNG = 180°
            // t=0.75 (hoàng hôn): DayLight bên TRÁI -> bóng chỉ PHẢI  = 270°
            float angle = 360f * time;

            // Độ dài: dài ở bình minh/hoàng hôn (|sin(2πt)| cao), ngắn ở trưa, mờ ban đêm
            float shadowShape = Mathf.Abs(Mathf.Sin(time * Mathf.PI * 2f));
            float nightFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.22f, time)) *
                              Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 0.78f, time));
            float length = (0.35f + 0.65f * shadowShape) * nightFade * BaseLength;

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(transform.localScale.x, length, transform.localScale.z);
        }
    }
}
