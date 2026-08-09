using System.Collections;
using UnityEngine;

/// <summary>
/// ICON NỔI NHẤP NHÔ — nhịp cơ bản của cả màn hình Township.
/// ═════════════════════════════════════════════════════════
///
/// Thông số ĐO TỪ VIDEO (PHAN_TICH_TOWNSHIP_ANIMATION.md §4.1):
///     position.y : ±6 px, chu kỳ 1.2 s, ease sin
///     scale      : 1.0 ↔ 1.06, LỆCH PHA với nhịp y
///
/// VÌ SAO PHẢI LỆCH PHA: nếu y và scale cùng pha thì icon lên cao đúng lúc phình to,
/// mắt đọc ra "một khối đang thở" — cứng. Lệch 1/4 vòng thì lúc icon lên cao nhất là lúc
/// scale đang về 1.0, cho cảm giác bồng bềnh trong không khí. Đây là toàn bộ khác biệt
/// giữa "có animation" và "trông như Township".
///
/// VÌ SAO CORoutine chứ không Update(): dự án không có DOTween (§9.3 doc đội) và mọi
/// hiệu ứng hiện có (PulseFrame, PulseArrow…) đều là coroutine — giữ một lối viết.
/// Coroutine tự chết khi component/GameObject bị tắt, khỏi phải nhớ dọn.
///
/// GẮN Ở ĐÂU: lên chính transform CỦA ICON (không phải lên công trình) — component ghi đè
/// localPosition/localScale nên nó phải sở hữu riêng transform đó.
/// </summary>
[DisallowMultipleComponent]
public class FloatingIconBob : MonoBehaviour
{
    [Header("◆ NHỊP LÊN XUỐNG (đo từ video)")]

    [Tooltip("Biên độ nhấp nhô theo trục Y. Thông số gốc Township = 6 'px' màn hình video.\n" +
             "Con số này còn được nhân với 'Pixel To Unit' bên dưới.")]
    [SerializeField] private float bobPixels = 6f;

    [Tooltip("Thời gian một nhịp lên-xuống đầy đủ, tính bằng giây. Township = 1.2s.\n" +
             "Nhanh hơn 0.8s thì thành 'rung', chậm hơn 2s thì trông như treo.")]
    [SerializeField] private float period = 1.2f;

    [Header("◆ NHỊP PHÌNH TO")]

    [Tooltip("Đỉnh scale. Township = 1.06 — chỉ 6 %, cố ý rất nhẹ. Đẩy lên 1.2 là thành " +
             "quảng cáo nhấp nháy, mất vẻ 'đắt tiền'.")]
    [SerializeField] private float scalePeak = 1.06f;

    [Tooltip("LỆCH PHA giữa nhịp scale và nhịp Y, tính theo VÒNG (0.25 = 90°).\n" +
             "Đặt 0 là hai nhịp trùng nhau → icon trông cứng. Xem ghi chú đầu file.")]
    [SerializeField] private float scalePhaseOffset = 0.25f;

    [Header("◆ QUY ĐỔI ĐƠN VỊ")]

    [Tooltip("1 'px' của thông số trên bằng bao nhiêu đơn vị LOCAL của transform này.\n" +
             "• Icon trong Canvas UI (1 unit = 1 px): để 1.\n" +
             "• Icon sprite trong world của game này (1 ô lưới = 100 unit): để ~2.5 " +
             "(bob ≈ 15 unit trên khung icon 110 unit — cùng TỈ LỆ với video).\n" +
             "VÌ SAO KHÔNG HARDCODE: giữ nguyên con số đo được (6 / 1.2s) để người sau còn " +
             "đối chiếu được với tài liệu, thay vì thấy một số 15 không rõ từ đâu ra.")]
    [SerializeField] private float pixelToUnit = 1f;

    [Header("◆ LỆCH PHA RIÊNG TỪNG ICON")]

    [Tooltip("BẬT = tự sinh lệch pha từ vị trí + InstanceID.\n" +
             "VÌ SAO CẦN: Township lúc nào cũng có 5–8 icon nổi; nếu tất cả bob ĐỒNG LOẠT " +
             "thì màn hình đập như một khối, phá hết cảm giác nhiều thứ đang sống riêng.\n" +
             "Dùng vị trí (không dùng Random) để mỗi lần Play ra kết quả GIỐNG NHAU — " +
             "bug tái hiện được.")]
    [SerializeField] private bool autoPhase = true;

    [Tooltip("Lệch pha tay theo VÒNG (0..1). Chỉ dùng khi tắt 'Auto Phase'.")]
    [SerializeField] private float manualPhase = 0f;

    // Mốc gốc: mọi phép tính đều CỘNG VÀO mốc này, không bao giờ cộng dồn lên giá trị
    // hiện tại — cộng dồn là nguyên nhân kinh điển của "icon trôi dần lên trời".
    private Vector3 _basePos;
    private Vector3 _baseScale;
    private float   _phase01;
    private Coroutine _loop;

    /// <summary>Đổi biên độ/chu kỳ lúc chạy (BuildingStatusIcon dùng khi tự dựng icon).</summary>
    public void Configure(float bobPixelsValue, float periodValue, float pixelToUnitValue)
    {
        bobPixels   = bobPixelsValue;
        period      = periodValue;
        pixelToUnit = pixelToUnitValue;
    }

    private void OnEnable()
    {
        _basePos   = transform.localPosition;
        _baseScale = transform.localScale;
        _phase01   = autoPhase ? FxEase.StablePhase01(transform) : Mathf.Repeat(manualPhase, 1f);

        _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }

        // Trả transform về đúng mốc gốc: nếu tắt giữa nhịp mà không trả thì lần bật lại
        // OnEnable sẽ chụp một mốc ĐÃ LỆCH và icon dịch dần mỗi lần bật/tắt.
        transform.localPosition = _basePos;
        transform.localScale    = _baseScale;
    }

    private IEnumerator Loop()
    {
        float t = 0f;

        while (true)
        {
            // Time.deltaTime (KHÔNG unscaled): mở popup làm timeScale = 0 thì icon đứng yên
            // cùng cả game — nhất quán với công nhân trong ConstructionSite.Tick().
            t += Time.deltaTime;

            float p = Mathf.Max(0.05f, period);
            float cycle = Mathf.Repeat(t / p + _phase01, 1f);

            // Nhịp Y: sin nguyên bản, biên độ ±bobPixels (Sin01 cho 0..1 nên đổi về −1..1).
            float wave = FxEase.Sin01(cycle) * 2f - 1f;
            transform.localPosition = _basePos + new Vector3(0f, wave * bobPixels * pixelToUnit, 0f);

            // Nhịp scale: cùng chu kỳ nhưng LỆCH PHA (xem ghi chú đầu file).
            float sWave = FxEase.Sin01(cycle + scalePhaseOffset);
            float k = Mathf.LerpUnclamped(1f, scalePeak, sWave);
            transform.localScale = _baseScale * k;

            yield return null;
        }
    }

    private void OnValidate()
    {
        period      = Mathf.Max(0.05f, period);
        pixelToUnit = Mathf.Max(0.0001f, pixelToUnit);
        scalePeak   = Mathf.Max(0.01f, scalePeak);
    }
}
