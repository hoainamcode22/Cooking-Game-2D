using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BÓ CỎ CHẠY TRÊN BĂNG TẢI — chạy lặp đúng 4 mốc keyframe của `moveItem` trong bản thiết kế.
///
/// ══ KEYFRAME GỐC (full_mill_ui.html) ══
///     .moving-item { animation: moveItem 3s linear infinite; }
///     .mi-1 { animation-delay: 0s;   }
///     .mi-2 { animation-delay: 1.5s; }
///
///     @keyframes moveItem {
///           0% { translateX(0)                      opacity: 1 }
///          80% { translateX(230px)                  opacity: 1 }
///          85% { translateX(250px) translateY(10px) opacity: 0 }
///         100% { translateX(250px)                  opacity: 0 }
///     }
///
/// Đọc ra hành vi: bó cỏ trôi đều 230px trong 80% chu kỳ (2.4s), rồi trong 5% chu kỳ (0.15s)
/// nó vọt thêm 20px, RƠI XUỐNG 10px và MỜ HẲN — đó là lúc nó rớt vào phễu máy xay.
/// 15% chu kỳ cuối (0.45s) nó vô hình, đứng chờ để vòng lặp tiếp theo bắt đầu.
/// Nhờ 0.45s "chết" này mà hai bó cỏ lệch pha 1.5s không bao giờ chồng lên nhau.
///
/// ⚠ TRỤC Y NGƯỢC NHAU: CSS `translateY(+10px)` là đi XUỐNG. Unity `anchoredPosition.y`
/// dương là đi LÊN. Nên code phải TRỪ: y = y0 - dropPx. Sai dấu chỗ này thì bó cỏ bay lên
/// trời thay vì rơi vào phễu — nhìn kỹ mới thấy nên rất dễ lọt.
///
/// ══ CHỐNG TRÔI (drift) ══
/// `anchoredPosition` được TÍNH LẠI từ mốc gốc `_goc` lưu lúc Awake, KHÔNG cộng dồn
/// (`pos += v * dt`). Cộng dồn thì sai số float tích lại và sau vài phút bó cỏ lệch khỏi
/// băng tải, tệ hơn là mỗi máy khác nhau lệch khác nhau.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class ConveyorItem : MonoBehaviour
{
    [Header("Nhịp — MillPopupUI ghi đè bằng số trong MillConfig")]
    [Tooltip("Độ dài MỘT chu kỳ, giây. HTML: animation: moveItem 3s")]
    public float cycleSeconds = 3f;

    [Tooltip("Lệch pha so với chu kỳ chung, giây. HTML: .mi-1 delay 0s / .mi-2 delay 1.5s.\n" +
             "MillPopupUI tự đặt = chỉ số item × MillConfig.itemStaggerSeconds.")]
    public float delaySeconds = 0f;

    [Header("Quỹ đạo — số lấy từ @keyframes moveItem")]
    [Tooltip("Khoảng chạy ngang tới mốc 80%. HTML: 80% { translateX(230px) }")]
    public float travelPx = 230f;

    [Tooltip("Chạy THÊM bao nhiêu px từ mốc 80% đến 85%. HTML: 250px − 230px = 20px.")]
    public float overshootPx = 20f;

    [Tooltip("Rơi XUỐNG bao nhiêu px ở mốc 85%. HTML: 85% { translateY(10px) }.\n" +
             "Code tự đảo dấu cho trục Y của Unity, cứ để số dương.")]
    public float dropPx = 10f;

    [Header("Mốc keyframe (tỉ lệ của chu kỳ) — chỉ sửa nếu bản thiết kế đổi")]
    [Tooltip("HTML: 80%")]
    [Range(0f, 1f)] public float mocChay = 0.80f;

    [Tooltip("HTML: 85%")]
    [Range(0f, 1f)] public float mocRoi = 0.85f;

    [Header("Khởi động")]
    [Tooltip("Có chạy ngay khi bật object. MillPopupUI điều khiển qua SetRunning() nên để TẮT.")]
    public bool autoStart = false;

    /// <summary>Đang chạy hay không.</summary>
    public bool IsRunning => _running;

    private RectTransform _rt;
    private Graphic       _graphic;      // Image HOẶC TMP_Text — cả hai đều kế thừa Graphic.
    private CanvasGroup   _canvasGroup;  // Tuỳ chọn; có thì dùng để mờ cả cụm con.

    private Vector2 _goc;                // Vị trí gốc lúc Awake — MỐC DUY NHẤT để tính lại.
    private Color   _mauGoc;             // Màu gốc, để phục hồi alpha đúng chứ không ép về 1.
    private bool    _running;
    private float   _dongHo;             // Thời gian đã chạy, đã bọc trong [0, cycleSeconds).

    private void Awake()
    {
        _rt          = GetComponent<RectTransform>();
        _graphic     = GetComponent<Graphic>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _goc = _rt.anchoredPosition;

        if (_graphic != null)
            _mauGoc = _graphic.color;

        _running = autoStart;

        // Vào scene ở trạng thái tắt thì phải ẩn ngay, đừng để 1 frame lộ bó cỏ đứng im.
        if (!_running) DatHienThi(false);
    }

    private void Update()
    {
        if (!_running) return;

        // Bọc trong [0, cycleSeconds) mỗi frame ⇒ không bao giờ tràn float dù popup mở cả ngày.
        _dongHo = Mathf.Repeat(_dongHo + Time.deltaTime, cycleSeconds);

        // delaySeconds LÙI pha: item thứ 2 (delay 1.5s) ở thời điểm t đang ở pha t + 1.5s
        // của chu kỳ — giống hệt cách CSS animation-delay hoạt động sau khi đã chạy ổn định.
        float t = Mathf.Repeat(_dongHo + delaySeconds, cycleSeconds);
        float p = cycleSeconds > 0f ? t / cycleSeconds : 0f;   // pha, 0..1

        float x, y, alpha;

        if (p <= mocChay)
        {
            // Đoạn 0% → 80%: trôi đều trên băng, hiện rõ.
            float k = mocChay > 0f ? p / mocChay : 1f;
            x     = travelPx * k;
            y     = 0f;
            alpha = 1f;
        }
        else if (p <= mocRoi)
        {
            // Đoạn 80% → 85%: vọt thêm, rơi xuống, mờ dần về 0.
            float khoang = mocRoi - mocChay;
            float k      = khoang > 0f ? (p - mocChay) / khoang : 1f;
            x     = travelPx + overshootPx * k;
            y     = -dropPx * k;                 // dấu trừ: CSS +Y xuống = Unity −Y
            alpha = 1f - k;
        }
        else
        {
            // Đoạn 85% → 100%: đã vô hình. CSS đưa translateY về 0 trong đoạn này (mốc 100%
            // không khai translateY nên nó = 0). Vô hình rồi nên không ai thấy, nhưng cứ
            // làm đúng để nếu ai bật opacity lên debug thì thấy khớp bản gốc.
            float khoang = 1f - mocRoi;
            float k      = khoang > 0f ? (p - mocRoi) / khoang : 1f;
            x     = travelPx + overshootPx;
            y     = -dropPx * (1f - k);
            alpha = 0f;
        }

        // TÍNH LẠI TỪ MỐC GỐC — không cộng dồn.
        _rt.anchoredPosition = new Vector2(_goc.x + x, _goc.y + y);

        DatAlpha(alpha);
    }

    /// <summary>
    /// Bật/tắt. Tắt thì ẩn item và ĐƯA VỀ mốc gốc, để lần bật lại bó cỏ xuất phát từ đầu
    /// băng tải chứ không "hiện ra giữa đường".
    /// </summary>
    public void SetRunning(bool on)
    {
        _running = on;

        if (on) return;

        _dongHo = 0f;

        if (_rt != null)
            _rt.anchoredPosition = _goc;

        DatHienThi(false);
    }

    /// <summary>Áp nhịp từ <see cref="MillConfig"/>. Gọi từ MillPopupUI lúc Open().</summary>
    public void Configure(float chuKyGiay, float lechPhaGiay, float khoangChayPx)
    {
        cycleSeconds = Mathf.Max(0.01f, chuKyGiay);
        delaySeconds = lechPhaGiay;
        travelPx     = khoangChayPx;
    }

    private void DatHienThi(bool hien)
    {
        DatAlpha(hien ? 1f : 0f);
    }

    private void DatAlpha(float a)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = a;
            return;
        }

        if (_graphic == null) return;

        // Chỉ đổi alpha, giữ nguyên RGB gốc (bó cỏ có thể được tint sẵn trong prefab).
        Color c = _mauGoc;
        c.a = a;
        _graphic.color = c;
    }
}
