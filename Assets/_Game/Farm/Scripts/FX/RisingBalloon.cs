using System.Collections;
using UnityEngine;

/// <summary>
/// BÓNG BAY KHÁNH THÀNH — bay lên, trôi ngang, tan dần.
/// ═══════════════════════════════════════════════════
///
/// Thông số ĐO TỪ VIDEO (PHAN_TICH_TOWNSHIP_ANIMATION.md §4.2):
///     position.y : +250 px trong 2.5 s, ease-out
///     position.x : dao động SIN biên độ 15 px
///     alpha      : 1 → 0 ở 30 % CUỐI
///     scale      : nhỏ dần theo độ cao (giả phối cảnh)
///
/// VÌ SAO DAO ĐỘNG X LÀ BẮT BUỘC: tài liệu ghi thẳng "không có thì trông như thang máy".
/// Bóng bay thật không đi thẳng đứng — một đường sin biên độ nhỏ là toàn bộ khác biệt
/// giữa "hạt bay lên" và "bóng bay".
///
/// VÌ SAO SCALE NHỎ DẦN: game 2D không có phối cảnh thật, nên bóng bay càng cao mà càng
/// giữ nguyên cỡ thì nó "dán lên màn hình". Thu nhỏ dần là cách giả độ sâu rẻ nhất.
///
/// CÁCH DÙNG: gắn lên GameObject bóng bay rồi bật (autoPlay), hoặc gọi <see cref="Play"/>.
/// Component tự huỷ GameObject khi xong nếu <see cref="destroyOnFinish"/> bật.
/// </summary>
[DisallowMultipleComponent]
public class RisingBalloon : MonoBehaviour
{
    [Header("◆ BAY LÊN (đo từ video)")]

    [Tooltip("Độ cao bay lên, tính bằng 'px' đo từ video Township = 250.\n" +
             "Nhân với 'Pixel To Unit' bên dưới để ra đơn vị local thật.")]
    [SerializeField] private float risePixels = 250f;

    [Tooltip("Thời gian bay hết đoạn trên, giây. Township = 2.5s.")]
    [SerializeField] private float duration = 2.5f;

    [Header("◆ TRÔI NGANG")]

    [Tooltip("Biên độ dao động ngang. Township = 15 'px'. ĐỪNG ĐẶT 0 — xem ghi chú đầu file.")]
    [SerializeField] private float swayPixels = 15f;

    [Tooltip("Số nhịp trôi qua-lại trong cả quãng bay. 1.5 = một vòng rưỡi, đủ để mắt " +
             "thấy nó lượn mà không thành zig-zag.")]
    [SerializeField] private float swayCycles = 1.5f;

    [Header("◆ TAN DẦN & THU NHỎ")]

    [Tooltip("Alpha bắt đầu tắt ở mốc nào của quãng bay. Township = 0.70 (tức 30 % cuối).")]
    [SerializeField] private float fadeStart01 = 0.70f;

    [Tooltip("Scale lúc lên tới đỉnh, so với scale ban đầu. 0.72 = nhỏ đi 28 % (giả phối cảnh).")]
    [SerializeField] private float endScale = 0.72f;

    [Header("◆ QUY ĐỔI ĐƠN VỊ")]

    [Tooltip("1 'px' thông số bằng bao nhiêu đơn vị LOCAL. Canvas UI: 1. " +
             "Sprite world của game này (1 ô = 100 unit): ~2.5.")]
    [SerializeField] private float pixelToUnit = 1f;

    [Header("◆ VẬN HÀNH")]

    [Tooltip("BẬT = tự chạy ngay khi được bật lên. TẮT = chờ ai gọi Play().")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("BẬT = huỷ GameObject sau khi bay xong. Bóng bay là vật dùng-một-lần, " +
             "không huỷ thì mỗi lần khánh thành lại để lại rác trong scene.")]
    [SerializeField] private bool destroyOnFinish = true;

    private Vector3   _basePos;
    private Vector3   _baseScale;
    private Component[] _faders;
    private Coroutine _routine;
    private float     _swayPhase01;

    /// <summary>Đổi biên độ/thời gian trước khi Play (ConstructionCompleteFX có thể dùng).</summary>
    public void Configure(float risePixelsValue, float durationValue, float pixelToUnitValue)
    {
        risePixels  = risePixelsValue;
        duration    = durationValue;
        pixelToUnit = pixelToUnitValue;
    }

    private void OnEnable()
    {
        if (autoPlay) Play();
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);

        _basePos   = transform.localPosition;
        _baseScale = transform.localScale;
        _faders    = FxEase.CollectFaders(transform);

        // Lệch pha ngang riêng từng quả: spawn 4–6 quả cùng lúc mà cùng pha thì cả chùm
        // lượn y như nhau, mắt đọc ra "một tấm ảnh đang bay" chứ không phải nhiều quả.
        _swayPhase01 = FxEase.StablePhase01(transform);

        _routine = StartCoroutine(Fly());
    }

    private IEnumerator Fly()
    {
        float dur = Mathf.Max(0.05f, duration);
        float t   = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / dur);

            // Y: ease-out — bung nhanh lúc thoát khỏi hộp quà rồi hãm lại khi lên cao.
            float rise = FxEase.OutCubic(raw) * risePixels * pixelToUnit;

            // X: sin theo TIẾN ĐỘ (không theo Time.time) để quãng lượn luôn khép kín
            // dù bóng bay lâu hay nhanh.
            float sway = Mathf.Sin((raw * swayCycles + _swayPhase01) * Mathf.PI * 2f)
                       * swayPixels * pixelToUnit;

            transform.localPosition = _basePos + new Vector3(sway, rise, 0f);

            // Scale: nội suy theo ĐỘ CAO đã đi được (dùng `rise` đã ease, không dùng raw)
            // → càng gần đỉnh càng nhỏ đúng nhịp với chuyển động, không bị lệch.
            float heightK = risePixels > 0.001f ? rise / (risePixels * pixelToUnit) : raw;
            transform.localScale = _baseScale * Mathf.LerpUnclamped(1f, endScale, heightK);

            // Alpha: giữ nguyên tới mốc fadeStart01 rồi tắt hẳn ở phần còn lại.
            float fs = Mathf.Clamp01(fadeStart01);
            if (raw > fs)
            {
                float k = (raw - fs) / Mathf.Max(0.0001f, 1f - fs);
                FxEase.SetAlpha(_faders, 1f - FxEase.InCubic(k));
            }

            yield return null;
        }

        FxEase.SetAlpha(_faders, 0f);
        _routine = null;

        if (destroyOnFinish) Destroy(gameObject);
    }

    private void OnValidate()
    {
        duration    = Mathf.Max(0.05f, duration);
        pixelToUnit = Mathf.Max(0.0001f, pixelToUnit);
        fadeStart01 = Mathf.Clamp01(fadeStart01);
    }
}
