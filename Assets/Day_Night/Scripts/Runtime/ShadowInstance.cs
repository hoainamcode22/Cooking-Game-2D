using UnityEngine;

// Giữ namespace HappyHarvest để tương thích GUID với shadow.prefab
namespace HappyHarvest
{
    [DefaultExecutionOrder(999)]
    [ExecuteAlways]
    public class ShadowInstance : MonoBehaviour
    {
        [Range(0, 10f)] public float BaseLength = 1f;

        private Day_Night.DayNightCycleController controller;

        private void LateUpdate()
        {
            if (controller == null)
                controller = UnityEngine.Object.FindFirstObjectByType<Day_Night.DayNightCycleController>();

            if (controller == null) return;

            float time = controller.CurrentDayRatio;

            // Góc bóng khớp với CCW LightsRoot: bình minh=90°, trưa=180°, hoàng hôn=270°
            float angle = 360f * time;

            // Độ dài: dài ở bình minh/hoàng hôn, ngắn ở trưa, mờ ban đêm
            float shadowShape = Mathf.Abs(Mathf.Sin(time * Mathf.PI * 2f));
            float nightFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.22f, time)) *
                              Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.88f, 0.78f, time));
            float length = (0.35f + 0.65f * shadowShape) * nightFade * BaseLength;

            transform.eulerAngles = new Vector3(0f, 0f, angle);
            transform.localScale  = new Vector3(1f, length, 1f);
        }

        // Stub methods để code cũ không bị lỗi nếu vẫn còn gọi
        public static void RegisterShadow(ShadowInstance s) { }
        public static void UnregisterShadow(ShadowInstance s) { }
    }
}
