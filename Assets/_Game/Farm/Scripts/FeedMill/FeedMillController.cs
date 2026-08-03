using UnityEngine;

/// <summary>
/// Điều khiển cụm bánh răng của MÁY LÀM THỨC ĂN GIA SÚC.
/// Gắn lên root "FeedMillGears". Từ gameplay gọi StartWorking() / StopWorking().
/// Bánh nhỏ luôn quay NGƯỢC chiều bánh lớn (mô phỏng ăn khớp răng) và nhanh hơn theo gearRatio.
/// </summary>
public class FeedMillController : MonoBehaviour
{
    public enum State { Idle, Working }

    [Header("Bánh răng")]
    [SerializeField] private RotatingGear bigGear;
    [SerializeField] private RotatingGear smallGear;

    [Header("Hiệu ứng (đều cho phép null)")]
    [Tooltip("GameObject lửa/khói — bật khi chạy, tắt khi dừng.")]
    [SerializeField] private GameObject fireVFX;
    [Tooltip("ParticleSystem rơi thức ăn — Play khi chạy, Stop khi dừng.")]
    [SerializeField] private ParticleSystem feedParticle;

    [Header("Thông số quay")]
    [Tooltip("Tốc độ bánh LỚN (độ/giây).")]
    [SerializeField] private float bigGearSpeed = 60f;

    [Tooltip("Bánh NHỎ quay nhanh hơn bánh lớn theo tỉ số này (1.4 = nhanh hơn 40%).")]
    [SerializeField] private float gearRatio = 1.4f;

    [Tooltip("Bánh lớn quay theo chiều kim đồng hồ? Bánh nhỏ sẽ tự quay NGƯỢC lại.")]
    [SerializeField] private bool bigGearClockwise = true;

    /// <summary>Trạng thái hiện tại.</summary>
    public State CurrentState { get; private set; } = State.Idle;

    /// <summary>Máy đang chạy?</summary>
    public bool IsWorking => CurrentState == State.Working;

    // ── API công khai ─────────────────────────────────────────────────────────

    /// <summary>Bật máy: 2 bánh quay NGƯỢC chiều nhau + bật VFX/particle.</summary>
    public void StartWorking()
    {
        CurrentState = State.Working;

        if (bigGear != null)
        {
            bigGear.Configure(bigGearSpeed, bigGearClockwise);
            bigGear.StartRotating();
        }

        if (smallGear != null)
        {
            // Bánh nhỏ: nhanh hơn (×gearRatio) và NGƯỢC chiều bánh lớn → trông như ăn khớp.
            smallGear.Configure(bigGearSpeed * gearRatio, !bigGearClockwise);
            smallGear.StartRotating();
        }

        if (fireVFX != null) fireVFX.SetActive(true);
        if (feedParticle != null) feedParticle.Play();
    }

    /// <summary>Tắt máy: dừng 2 bánh + tắt VFX/particle.</summary>
    public void StopWorking()
    {
        CurrentState = State.Idle;

        if (bigGear != null) bigGear.StopRotating();
        if (smallGear != null) smallGear.StopRotating();

        if (fireVFX != null) fireVFX.SetActive(false);
        if (feedParticle != null) feedParticle.Stop();
    }

    /// <summary>Bật/tắt theo cờ (tiện cho code gameplay).</summary>
    public void SetWorking(bool on)
    {
        if (on) StartWorking();
        else StopWorking();
    }

    // ── Test nhanh trong Inspector (chuột phải vào component) ───────────────────
    // Lưu ý: bánh chỉ XOAY khi đang ở Play Mode (Update chạy). Vào Play rồi bấm Test Start.

    [ContextMenu("Test Start")]
    private void TestStart() => StartWorking();

    [ContextMenu("Test Stop")]
    private void TestStop() => StopWorking();
}
