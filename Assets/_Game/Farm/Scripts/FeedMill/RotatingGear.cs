using UnityEngine;

/// <summary>
/// Bánh răng tự xoay quanh trục Z. Gắn lên TỪNG bánh răng (to / nhỏ).
/// Controller (FeedMillController) sẽ bật/tắt và set tốc độ + chiều cho từng bánh.
/// </summary>
public class RotatingGear : MonoBehaviour
{
    [Tooltip("Tốc độ xoay — độ/giây.")]
    [SerializeField] private float degreesPerSecond = 60f;

    [Tooltip("Xoay theo chiều kim đồng hồ? (Unity quay DƯƠNG = ngược kim đồng hồ).")]
    [SerializeField] private bool clockwise = true;

    [Tooltip("Tự xoay ngay khi vào game (không cần controller gọi).")]
    [SerializeField] private bool playOnStart = false;

    /// <summary>Đang xoay hay không.</summary>
    public bool IsRunning { get; private set; }

    private void Start()
    {
        // playOnStart = true thì chạy ngay; không thì chờ controller gọi StartRotating().
        IsRunning = playOnStart;
    }

    private void Update()
    {
        if (!IsRunning) return;

        // clockwise → chiều âm (kim đồng hồ); ngược lại → chiều dương.
        float dir = clockwise ? -1f : 1f;
        transform.Rotate(0f, 0f, dir * degreesPerSecond * Time.deltaTime);
    }

    // ── API công khai ─────────────────────────────────────────────────────────

    /// <summary>Bắt đầu xoay.</summary>
    public void StartRotating() => IsRunning = true;

    /// <summary>Dừng xoay.</summary>
    public void StopRotating() => IsRunning = false;

    /// <summary>Bật/tắt xoay theo cờ.</summary>
    public void SetRunning(bool on) => IsRunning = on;

    /// <summary>Đặt lại tốc độ + chiều lúc chạy (controller dùng để set tỉ số truyền & chiều ăn khớp).</summary>
    public void Configure(float newDegreesPerSecond, bool newClockwise)
    {
        degreesPerSecond = newDegreesPerSecond;
        clockwise = newClockwise;
    }
}
