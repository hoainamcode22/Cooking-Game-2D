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

    // ── [PERF F4.6 2026-09-17] CHI GHI KHI SO THAT SU DOI ────────────────────────
    // Ghi `anchoredPosition` LAM BAN (dirty) RectTransform => Canvas cha phai dung lai
    // layout + rebuild batch. Cha o day la `MillPopup_Root`: 231 object, 138 Image — mot
    // lan rebuild la rat dat. Ban cu ghi MOI FRAME ke ca khi popup dang an.
    // Hai cong chan:
    //   ① Update thoat ngay khi popup khong hien (activeInHierarchy=false thi Unity da khong
    //      goi Update; con lai truong hop CanvasGroup.alpha=0 / Graphic tat => kiem tay).
    //   ② Chi gan anchoredPosition khi lech qua NGUONG_LECH_PX so voi lan ghi truoc.
    // KHONG doi quy dao: gia tri van tinh lai tu moc goc `_goc`, chi bot luot GHI trung.
    private const float NGUONG_LECH_PX = 0.01f;
    private Vector2 _viTriDaGhi;
    private bool    _daGhiLanNao;
    private float   _alphaDaGhi = float.NaN;

    private void Awake()
    {
        _rt          = GetComponent<RectTransform>();
        _graphic     = GetComponent<Graphic>();
        _canvasGroup = GetComponent<CanvasGroup>();

        _goc = _rt.anchoredPosition;
        _viTriDaGhi  = _goc;      // [PERF F4.6] moc "da ghi" khop voi thuc te ngay tu dau.
        _daGhiLanNao = true;

        if (_graphic != null)
            _mauGoc = _graphic.color;

        _running = autoStart;

        // Vào scene ở trạng thái tắt thì phải ẩn ngay, đừng để 1 frame lộ bó cỏ đứng im.
        if (!_running) DatHienThi(false);
    }

    private void Update()
    {
        // [PERF F4.6] ① `_running` CHINH LA cong "popup co dang hien khong": MillPopupUI goi
        // SetRunning(false) khi dong popup, va Unity von khong goi Update tren GameObject tat.
        //
        // ⚠ TUYET DOI KHONG them cong kieu `if (_canvasGroup.alpha <= 0) return;`: alpha do
        // CHINH component nay ghi (DatAlpha), nen 15% cuoi chu ky alpha = 0 se tu khoa minh
        // lai va bo co khong bao gio sang lai o dau chu ky sau.
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
        // [PERF F4.6] ② chi gan khi lech that su, de khong lam ban RectTransform mien phi.
        Vector2 viTriMoi = new Vector2(_goc.x + x, _goc.y + y);
        if (!_daGhiLanNao ||
            Mathf.Abs(viTriMoi.x - _viTriDaGhi.x) > NGUONG_LECH_PX ||
            Mathf.Abs(viTriMoi.y - _viTriDaGhi.y) > NGUONG_LECH_PX)
        {
            _rt.anchoredPosition = viTriMoi;
            _viTriDaGhi  = viTriMoi;
            _daGhiLanNao = true;
        }

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
        {
            _rt.anchoredPosition = _goc;
            // [PERF F4.6] dong bo lai moc "da ghi", neu khong lan chay sau co the bo qua
            // luot ghi dau tien va bo co dung sai cho mot frame.
            _viTriDaGhi  = _goc;
            _daGhiLanNao = true;
        }

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
        // [PERF F4.6] Graphic.color / CanvasGroup.alpha deu goi SetVerticesDirty tren ca cum
        // => bo qua luot gan khi gia tri khong doi. `float.NaN` o moc dau lam moi phep so
        // sanh false nen lan GAN DAU TIEN luon di qua.
        if (Mathf.Abs(a - _alphaDaGhi) <= 0.001f) return;
        _alphaDaGhi = a;

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
