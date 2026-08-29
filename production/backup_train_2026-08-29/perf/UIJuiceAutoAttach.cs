using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự động gắn UIJuiceFeedback (nhún khi bấm/hover) cho MỌI Button đang hoạt động —
/// cả nút bake sẵn trong scene lẫn nút được Instantiate lúc chạy (slot kho, thẻ shop...).
///
/// Cách làm: một sweeper DontDestroyOnLoad quét định kỳ (2s) tất cả Button active,
/// nút nào chưa có UIJuiceFeedback thì AddComponent. Chi phí ~vài trăm GetComponent
/// mỗi 2 giây — không đáng kể. Chỉ quét Button (không quét Slider/Scrollbar/Toggle
/// để thanh kéo không bị nhún theo). Thẻ nguyên liệu bếp v2 được gắn riêng lúc build
/// (BuildTrayCards). [Sếp 2026-08-27 — mọi nút phải có cảm giác chạm]
/// </summary>
public class UIJuiceAutoAttach : MonoBehaviour
{
    private const float SweepInterval = 2f;
    private float _timer; // 0 → quét ngay frame đầu sau khi scene nạp

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("~UIJuiceAutoAttach");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<UIJuiceAutoAttach>();
    }

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;
        _timer = SweepInterval;

        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None); // mặc định: chỉ object active
        foreach (var b in buttons)
        {
            if (b == null) continue;
            if (b.GetComponent<UIJuiceFeedback>() == null)
                b.gameObject.AddComponent<UIJuiceFeedback>();
        }
    }
}
