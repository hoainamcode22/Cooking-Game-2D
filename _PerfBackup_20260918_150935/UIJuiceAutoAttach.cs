using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tự động gắn UIJuiceFeedback (nhún khi bấm/hover) cho MỌI Button đang hoạt động —
/// cả nút bake sẵn trong scene lẫn nút được Instantiate lúc chạy (slot kho, thẻ shop...).
///
/// Cách làm: một sweeper DontDestroyOnLoad quét định kỳ tất cả Button active, nút nào
/// chưa có UIJuiceFeedback thì AddComponent. Chỉ quét Button (không quét Slider/
/// Scrollbar/Toggle để thanh kéo không bị nhún theo). Thẻ nguyên liệu bếp v2 được gắn
/// riêng lúc build (BuildTrayCards). [Sếp 2026-08-27 — mọi nút phải có cảm giác chạm]
///
/// ── HIỆU NĂNG 2026-08-29 ────────────────────────────────────────────────────────
/// Bản trước quét cứng 2 giây/lần và gọi GetComponent&lt;UIJuiceFeedback&gt;() cho TỪNG
/// nút, mỗi lần quét, mãi mãi — kể cả những nút đã gắn từ lâu. Trong SCN_Farm (6.907
/// object) đó là vài trăm lượt GetComponent lặp lại đều đặn suốt phiên chơi.
///
/// Bản này giữ NGUYÊN cam kết "mọi nút đều có cảm giác chạm", chỉ bỏ phần làm lại:
///   · Nút nào đã xử lý thì nhớ instanceID vào <see cref="Known"/> ⇒ lần quét sau bỏ
///     qua ngay, không GetComponent nữa.
///   · Nhịp quét KHÔNG bao giờ chậm hơn 2 giây như cũ, nhưng được phép NHANH HƠN
///     (0,35 giây) ngay sau khi nạp scene hoặc vừa phát hiện nút mới — nên nút sinh
///     lúc chạy thực ra còn được gắn sớm hơn bản cũ.
/// </summary>
public class UIJuiceAutoAttach : MonoBehaviour
{
    private const float SweepInterval = 2f;      // nhịp nghỉ — bằng đúng bản cũ
    private const float FastInterval  = 0.35f;   // nhịp gấp — khi UI vừa đổi
    private const int   FastAfterScene = 6;      // số lượt gấp sau khi nạp scene
    private const int   FastAfterNew   = 4;      // số lượt gấp sau khi thấy nút mới
    private const int   ForgetAbove    = 4000;   // dọn bộ nhớ đệm khi phình quá

    private static readonly HashSet<int> Known = new HashSet<int>();

    private float _timer;          // 0 → quét ngay frame đầu sau khi scene nạp
    private int   _fastSweepsLeft = FastAfterScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("~UIJuiceAutoAttach");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<UIJuiceAutoAttach>();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene mới = instanceID cũ vô nghĩa. Xoá đệm và quét gấp vài lượt.
        Known.Clear();
        _fastSweepsLeft = FastAfterScene;
        _timer          = 0f;
    }

    private void Update()
    {
        _timer -= Time.unscaledDeltaTime;
        if (_timer > 0f) return;

        _timer = _fastSweepsLeft > 0 ? FastInterval : SweepInterval;
        if (_fastSweepsLeft > 0) _fastSweepsLeft--;

        if (Known.Count > ForgetAbove) Known.Clear();

        var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None); // mặc định: chỉ object active
        bool foundNew = false;

        foreach (var b in buttons)
        {
            if (b == null) continue;

            int id = b.GetInstanceID();
            if (!Known.Add(id)) continue;   // Add trả false = đã xử lý lần trước

            foundNew = true;
            if (b.GetComponent<UIJuiceFeedback>() == null)
                b.gameObject.AddComponent<UIJuiceFeedback>();
        }

        if (foundNew) _fastSweepsLeft = FastAfterNew;
    }
}
