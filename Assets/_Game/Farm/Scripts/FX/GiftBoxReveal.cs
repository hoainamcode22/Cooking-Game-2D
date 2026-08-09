using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// HỘP QUÀ KHÁNH THÀNH — "nở ra rồi tan".
/// ═════════════════════════════════════
///
/// Thông số ĐO TỪ VIDEO (PHAN_TICH_TOWNSHIP_ANIMATION.md §4.4):
///     pha 1 (0.4 s) : scale 0 → 1.15 → 1.0   ease-out-back
///     pha 2 (1.2 s) : GIỮ nguyên
///     pha 3 (0.5 s) : scale 1.0 → 1.3  đồng thời  alpha 1 → 0
///
/// VÌ SAO PHA 3 PHẢI PHÌNH RA CHỨ KHÔNG CO LẠI: hộp co lại + mờ đi đọc ra "biến mất";
/// hộp phình ra + mờ đi đọc ra "vỡ ra, để lộ công trình bên trong". Township chọn cách
/// thứ hai vì đúng lúc đó công trình thật hiện lên — hộp phải trông như đang MỞ.
///
/// VÌ SAO CÓ PHA GIỮ 1.2 s: người chơi cần một nhịp để nhìn thấy cái hộp trước khi nó mở.
/// Bỏ pha giữ thì cả chuỗi trôi qua trong 0.9 s và người chơi không kịp nhận ra có quà.
///
/// <see cref="OnFinished"/> gọi khi tan xong — chỗ để ConstructionCompleteFX bật công
/// trình thật lên nếu sau này muốn ghép vào.
/// </summary>
[DisallowMultipleComponent]
public class GiftBoxReveal : MonoBehaviour
{
    [Header("◆ PHA 1 — NỞ RA (đo từ video)")]

    [Tooltip("Đỉnh scale của cú nở. Township = 1.15.")]
    [SerializeField] private float popPeak = 1.15f;

    [Tooltip("Thời gian nở, giây. Township = 0.4s.")]
    [SerializeField] private float popDuration = 0.4f;

    [Header("◆ PHA 2 — GIỮ")]

    [Tooltip("Thời gian đứng yên cho người chơi kịp thấy cái hộp, giây. Township = 1.2s.")]
    [SerializeField] private float holdDuration = 1.2f;

    [Header("◆ PHA 3 — TAN")]

    [Tooltip("Scale lúc tan hết. Township = 1.3 — PHÌNH RA, không co lại. Xem ghi chú đầu file.")]
    [SerializeField] private float burstScale = 1.3f;

    [Tooltip("Thời gian tan, giây. Township = 0.5s.")]
    [SerializeField] private float burstDuration = 0.5f;

    [Header("◆ VẬN HÀNH")]

    [Tooltip("BẬT = tự chạy ngay khi được bật lên.")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("BẬT = huỷ GameObject sau khi tan.")]
    [SerializeField] private bool destroyOnFinish = true;

    /// <summary>Gọi ngay sau khi hộp tan hết (trước khi huỷ GameObject).</summary>
    public Action OnFinished;

    private Vector3     _baseScale;
    private Component[] _faders;
    private Coroutine   _routine;

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

        _baseScale = transform.localScale;
        _faders    = FxEase.CollectFaders(transform);

        transform.localScale = Vector3.zero;   // tránh nháy một frame đủ cỡ
        FxEase.SetAlpha(_faders, 1f);

        _routine = StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // ── PHA 1: nở ra 0 → popPeak → 1.0 ──────────────────────────────────
        // c1 giải MỘT LẦN. Đỉnh 1.15 không có nghiệm "đẹp" như 1.25 (c1 = 3) nên phải giải:
        // o(c1) = 4c1³/(27(c1+1)²) = 0.15  →  c1 ≈ 2.164.
        float c1  = FxEase.BackConstantFor(Mathf.Max(0f, popPeak - 1f));
        float dur = Mathf.Max(0.05f, popDuration);
        float t   = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = _baseScale * FxEase.OutBackRaw(t / dur, c1);
            yield return null;
        }
        transform.localScale = _baseScale;

        // ── PHA 2: giữ ──────────────────────────────────────────────────────
        // Đếm bằng deltaTime chứ không WaitForSeconds: WaitForSeconds không tôn trọng
        // Time.timeScale = 0 theo cách ta muốn ở đây, và cách này dừng cùng cả game.
        t = 0f;
        while (t < holdDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // ── PHA 3: phình ra + mờ đi ─────────────────────────────────────────
        dur = Mathf.Max(0.05f, burstDuration);
        t   = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / dur);
            float k   = FxEase.OutQuad(raw);        // phình đều tay, không nảy nữa

            transform.localScale = _baseScale * Mathf.LerpUnclamped(1f, burstScale, k);
            FxEase.SetAlpha(_faders, 1f - k);
            yield return null;
        }

        FxEase.SetAlpha(_faders, 0f);
        _routine = null;

        OnFinished?.Invoke();
        if (destroyOnFinish) Destroy(gameObject);
    }

    private void OnValidate()
    {
        popDuration   = Mathf.Max(0.05f, popDuration);
        burstDuration = Mathf.Max(0.05f, burstDuration);
        holdDuration  = Mathf.Max(0f, holdDuration);
    }
}
