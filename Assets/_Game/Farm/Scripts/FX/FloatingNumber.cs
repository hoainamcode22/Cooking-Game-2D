using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// SỐ THƯỞNG BAY LÊN ("+10") — cú "nảy" quan trọng nhất của bộ hiệu ứng.
/// ══════════════════════════════════════════════════════════════════
///
/// Thông số ĐO TỪ VIDEO (PHAN_TICH_TOWNSHIP_ANIMATION.md §4.3):
///     position.y : +90 px trong 1.2 s, ease-out
///     scale      : 0 → 1.25 → 1.0  (ease-out-BACK)
///     alpha      : giữ 1 trong 60 %, rồi tắt
///
/// 🔴 CÁI OVERSHOOT 1.25 LÀ TOÀN BỘ VỊ "NẢY" — tài liệu ghi thẳng "bỏ nó đi là mất hết vị".
/// Ở đây nó KHÔNG phải hằng số quen tay 1.70158 (chỉ cho đỉnh ~1.10) mà là
/// <see cref="FxEase.BackC1Peak125"/> = 3, nghiệm CHÍNH XÁC cho đỉnh 1.25 — xem phần
/// giải tích trong FxEase.cs.
///
/// ⚠ MỘT SUY LUẬN CÓ CHỦ Ý (khác tài liệu một chút, cố ý ghi ra để người sau biết):
/// tài liệu liệt kê `scale` cùng khối với `y` (1.2 s) nhưng KHÔNG ghi riêng thời gian cho
/// cú pop. Nếu cho cú pop kéo dài đủ 1.2 s thì đỉnh 1.25 rơi vào giữa quãng bay — mắt đọc
/// ra "số đang phình lên khi bay", không phải "số nảy ra rồi bay". Vì vậy tách
/// `scaleDuration` = 0.45 s (pop xong ở 3/8 quãng đường). Muốn y hệt tài liệu thì đặt
/// scaleDuration = 1.2.
/// </summary>
[DisallowMultipleComponent]
public class FloatingNumber : MonoBehaviour
{
    [Header("◆ BAY LÊN (đo từ video)")]

    [Tooltip("Độ cao bay lên, 'px' đo từ video Township = 90. Nhân với 'Pixel To Unit'.")]
    [SerializeField] private float risePixels = 90f;

    [Tooltip("Thời gian bay, giây. Township = 1.2s.")]
    [SerializeField] private float duration = 1.2f;

    [Header("◆ CÚ NẢY (đừng bỏ)")]

    [Tooltip("Đỉnh scale. Township = 1.25. Đây là cú 'nảy' — hạ về 1.0 là mất hết cảm giác.")]
    [SerializeField] private float scalePeak = 1.25f;

    [Tooltip("Thời gian riêng cho cú pop, giây. Xem ghi chú ⚠ ở đầu file để biết vì sao " +
             "nó ngắn hơn thời gian bay. Đặt bằng 'Duration' nếu muốn đúng y tài liệu.")]
    [SerializeField] private float scaleDuration = 0.45f;

    [Header("◆ TAN DẦN")]

    [Tooltip("Alpha bắt đầu tắt ở mốc nào của quãng bay. Township = 0.60 (giữ rõ 60 % đầu).")]
    [SerializeField] private float fadeStart01 = 0.60f;

    [Header("◆ QUY ĐỔI ĐƠN VỊ")]

    [Tooltip("1 'px' thông số bằng bao nhiêu đơn vị LOCAL. Canvas UI: 1. " +
             "Sprite/TMP world của game này (1 ô = 100 unit): ~2.5.")]
    [SerializeField] private float pixelToUnit = 1f;

    [Header("◆ VẬN HÀNH")]

    [Tooltip("BẬT = tự chạy ngay khi được bật lên.")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("BẬT = huỷ GameObject sau khi xong. Số thưởng là vật dùng-một-lần.")]
    [SerializeField] private bool destroyOnFinish = true;

    [Tooltip("TMP hiển thị con số. Để trống = tự tìm trong con.")]
    [SerializeField] private TMP_Text label;

    private Vector3     _basePos;
    private Vector3     _baseScale;
    private Component[] _faders;
    private Coroutine   _routine;

    /// <summary>Đặt nội dung (vd "+10", "+250 🪙"). Gọi TRƯỚC Play().</summary>
    public void SetText(string content)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = content;
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

        // Bắt đầu từ scale 0: nếu để nguyên cỡ ở frame đầu thì người chơi thấy một frame
        // "số hiện đủ cỡ" trước khi cú pop bắt đầu → nháy.
        transform.localScale = Vector3.zero;

        _routine = StartCoroutine(Rise());
    }

    private IEnumerator Rise()
    {
        float dur   = Mathf.Max(0.05f, duration);
        float sDur  = Mathf.Max(0.05f, scaleDuration);

        // c1 giải MỘT LẦN ở đây, không giải mỗi frame (xem FxEase.BackConstantFor).
        // Đỉnh đúng 1.25 có nghiệm chính xác c1 = 3 nên dùng luôn hằng số cho khỏi sai số.
        float c1 = Mathf.Approximately(scalePeak, 1.25f)
                 ? FxEase.BackC1Peak125
                 : FxEase.BackConstantFor(Mathf.Max(0f, scalePeak - 1f));

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / dur);

            // Y: ease-out.
            transform.localPosition = _basePos
                + new Vector3(0f, FxEase.OutCubic(raw) * risePixels * pixelToUnit, 0f);

            // Scale: 0 → scalePeak → 1.0 bằng ease-out-back, xong sớm rồi giữ 1.0.
            float sT = Mathf.Clamp01(t / sDur);
            transform.localScale = _baseScale * FxEase.OutBackRaw(sT, c1);

            // Alpha: giữ 1 rồi tắt ở phần cuối.
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
        duration      = Mathf.Max(0.05f, duration);
        scaleDuration = Mathf.Max(0.05f, scaleDuration);
        pixelToUnit   = Mathf.Max(0.0001f, pixelToUnit);
        fadeStart01   = Mathf.Clamp01(fadeStart01);
    }
}
