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

    /// <summary>
    /// [VÒNG 15] Scale GỐC của từng target, chụp ĐÚNG MỘT LẦN ở nhịp pulse đầu tiên.
    ///
    /// VÌ SAO PHẢI CÓ: bản vá vòng 13 cho coroutine tự chụp `target.localScale` mỗi lần chạy.
    /// Nhưng `StartPulse` DỪNG coroutine cũ GIỮA CHỪNG — đúng lúc scale đang là base×1.22.
    /// Lượt mới chụp lại con số đang phình đó làm "gốc" ⇒ 1.22 → 1.49 → 1.82… **phình vô hạn**.
    /// Đó là lý do icon vàng cứ to dần lên sau mỗi lần nhận thưởng (Sếp chụp được).
    ///
    /// Bản gốc hard-code Vector3.one thì sai kiểu khác (ép cụm scale 1.2 về 1.0) nhưng KHÔNG
    /// tích luỹ. Chỉ nhớ scale gốc một lần mới chữa được cả hai.
    /// </summary>
    private static readonly Dictionary<Transform, Vector3> BaseScales = new Dictionary<Transform, Vector3>();
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
        // Chụp scale gốc ĐÚNG MỘT LẦN, lúc target chắc chắn chưa bị pulse nào đụng vào.
        if (!BaseScales.ContainsKey(target))
            BaseScales[target] = target.localScale;

        if (ActiveRoutines.TryGetValue(target, out var oldRoutine) && oldRoutine != null)
        {
            StopCoroutine(oldRoutine);

            // BẮT BUỘC trả về gốc ngay: coroutine bị dừng giữa chừng để lại scale đang phình
            // (base×1.22). Không trả về thì mắt người chơi thấy một cú giật, và mọi phép tính
            // sau đó đều lệch. Đây là chỗ vòng 13 bỏ sót.
            target.localScale = BaseScales[target];
        }

        ActiveRoutines[target] = StartCoroutine(RoutineJuicyPulse(target, punchScale, duration));
    }

    private IEnumerator RoutineJuicyPulse(Transform target, float punchScale, float duration)
    {
        if (target == null) yield break;

        // [VÒNG 15] Đọc scale gốc từ BaseScales — TUYỆT ĐỐI KHÔNG chụp lại `target.localScale`
        // ở đây. Chụp lại chính là cái làm icon phình dần vô hạn (xem chú thích ở BaseScales).
        if (!BaseScales.TryGetValue(target, out Vector3 baseScale))
        {
            baseScale = target.localScale;
            BaseScales[target] = baseScale;
        }
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

        // Dọn rác: target đã bị huỷ (đổi scene, popup đóng) thì bỏ luôn khỏi bảng scale gốc,
        // nếu không Dictionary giữ tham chiếu chết và lần sau tạo lại object sẽ đọc nhầm.
        if (target == null)
        {
            var chet = new List<Transform>();
            foreach (var k in BaseScales.Keys) if (k == null) chet.Add(k);
            foreach (var k in chet) BaseScales.Remove(k);
        }
    }
}
