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
            {
                controller = FindObjectOfType<DayNightCycleController>();
            }

            float time = controller != null ? controller.CurrentDayRatio : 0.5f;
            float angle = Mathf.Lerp(90f, 270f, time);
            float noonDistance = Mathf.Abs(time - 0.5f) * 2f;
            float length = Mathf.Lerp(0.45f, 1.6f, noonDistance) * BaseLength;

            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            transform.localScale = new Vector3(transform.localScale.x, length, transform.localScale.z);
        }
    }
}
