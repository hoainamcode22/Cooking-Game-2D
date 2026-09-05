using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Module tạo hiệu ứng nảy đàn hồi "mẩy mẩy / nhấp nhấp" (Juicy Squash & Stretch Pulse)
/// cho các UI Container (EXP_Bar_Container, Gold_Container, Diamond_Container, WarehouseGainToast)
/// khi nhận được vật phẩm / tiền / EXP bay tới.
/// </summary>
public class JuicyPulseFX : MonoBehaviour
{
    private static readonly Dictionary<Transform, Coroutine> ActiveRoutines = new Dictionary<Transform, Coroutine>();
    private static JuicyPulseFX _runner;

    private static JuicyPulseFX Runner
    {
        get
        {
            if (_runner == null)
            {
                var go = new GameObject("[JuicyPulseFX_Runner]");
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<JuicyPulseFX>();
            }
            return _runner;
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng nảy đàn hồi "mẩy mẩy" trên bất kỳ Transform/RectTransform nào.
    /// </summary>
    /// <param name="target">Container cần nảy</param>
    /// <param name="punchScale">Độ phóng to cực đại (mặc định 1.22x)</param>
    /// <param name="duration">Thời gian nảy (mặc định 0.26s)</param>
    public static void Play(Transform target, float punchScale = 1.22f, float duration = 0.26f)
    {
        if (target == null) return;
        Runner.StartPulse(target, punchScale, duration);
    }

    private void StartPulse(Transform target, float punchScale, float duration)
    {
        if (ActiveRoutines.TryGetValue(target, out var oldRoutine) && oldRoutine != null)
        {
            StopCoroutine(oldRoutine);
        }

        ActiveRoutines[target] = StartCoroutine(RoutineJuicyPulse(target, punchScale, duration));
    }

    private IEnumerator RoutineJuicyPulse(Transform target, float punchScale, float duration)
    {
        if (target == null) yield break;

        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;
        duration = Mathf.Max(0.08f, duration);

        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Đường cong sóng đàn hồi nảy 2 nhịp (Overshoot -> Rebound -> Settle)
            // Nhịp 1: Nở to 1.22x
            // Nhịp 2: Co nhẹ 0.94x
            // Nhịp 3: Về 1.0x
            float scaleMultiplier;
            if (t < 0.35f)
            {
                float k = t / 0.35f;
                scaleMultiplier = Mathf.Lerp(1.0f, punchScale, Mathf.Sin(k * Mathf.PI * 0.5f));
            }
            else if (t < 0.7f)
            {
                float k = (t - 0.35f) / 0.35f;
                scaleMultiplier = Mathf.Lerp(punchScale, 0.94f, k);
            }
            else
            {
                float k = (t - 0.7f) / 0.3f;
                scaleMultiplier = Mathf.Lerp(0.94f, 1.0f, k);
            }

            target.localScale = baseScale * scaleMultiplier;
            yield return null;
        }

        if (target != null)
        {
            target.localScale = baseScale;
        }

        ActiveRoutines.Remove(target);
    }
}
