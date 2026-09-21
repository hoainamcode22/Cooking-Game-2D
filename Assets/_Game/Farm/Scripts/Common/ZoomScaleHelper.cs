using UnityEngine;

/// <summary>
/// [2026-09-21] Hệ số co/giãn theo ZOOM camera cho vật world-space muốn giữ KÍCH THƯỚC
/// TRÊN MÀN HÌNH ổn định (liềm gặt, icon EXP/coin bay lên khi thu hoạch...).
///
///     heSo = cam.orthographicSize / ORTHO_THAM_CHIEU
///
/// Ở ortho tham chiếu heSo = 1 ⇒ vật đúng cỡ thiết kế trong Inspector. Zoom in (ortho nhỏ)
/// ⇒ heSo &lt; 1 ⇒ world-scale nhỏ lại để trên màn hình không phình to; zoom out ngược lại.
/// ORTHO_THAM_CHIEU = CameraController.defaultSize (750) — ortho lúc vào game.
/// Không alloc, không FindObject: người gọi tự cache Camera.
/// </summary>
public static class ZoomScaleHelper
{
    /// <summary>Ortho mặc định của CameraController (defaultSize = 750, dải 400–1500).</summary>
    public const float ORTHO_THAM_CHIEU = 750f;

    /// <summary>Kẹp mặc định: không nhỏ hơn 0.5x, không lớn hơn 2.0x cỡ thiết kế.</summary>
    public const float HE_SO_MIN = 0.5f;
    public const float HE_SO_MAX = 2.0f;

    /// <summary>Hệ số thô theo ortho hiện tại (đã kẹp [0.5, 2]). cam null ⇒ 1.</summary>
    public static float HeSo(Camera cam)
    {
        return HeSo(cam, ORTHO_THAM_CHIEU);
    }

    /// <summary>Hệ số theo ortho tham chiếu tuỳ chọn (đã kẹp [0.5, 2]). cam null ⇒ 1.</summary>
    public static float HeSo(Camera cam, float orthoThamChieu)
    {
        if (cam == null || !cam.orthographic) return 1f;
        if (orthoThamChieu < 1f) orthoThamChieu = ORTHO_THAM_CHIEU;
        float k = cam.orthographicSize / orthoThamChieu;
        return Mathf.Clamp(k, HE_SO_MIN, HE_SO_MAX);
    }
}
