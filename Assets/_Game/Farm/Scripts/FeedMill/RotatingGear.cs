using UnityEngine;

/// <summary>
/// Bánh răng tự xoay quanh trục Z. Gắn lên TỪNG bánh răng (to / nhỏ).
/// Controller (FeedMillController) sẽ bật/tắt và set tốc độ + chiều cho từng bánh.
///
/// ══════════════════════════════════════════════════════════════════════════
///  ⚠ SỬA 21/08 — "BÁNH RĂNG XOAY RA NGOÀI RÌA MÁY"
/// ══════════════════════════════════════════════════════════════════════════
/// `transform.Rotate` quay quanh GỐC TOẠ ĐỘ CỤC BỘ của node, và với RectTransform thì gốc
/// đó nằm ở **PIVOT**. `MillPopupBuilderTool` neo bánh răng bằng helper `TL` (top-left) nên
/// pivot rơi vào GÓC TRÊN-TRÁI.
///
/// Hậu quả: bánh răng không xoay tại chỗ mà **đi vòng quanh cái góc đó** — quét một đường
/// tròn bán kính tới `size × √2` (bánh 140px ⇒ ~198px), tức là lao hẳn ra ngoài thân máy.
/// Đúng hiện tượng chủ dự án báo: "cái bánh quay nó to quá nó xoay ra ngoài rìa luôn rồi".
///
/// Không có lỗi đỏ nào, vì mọi thứ đều hợp lệ — chỉ là quay quanh sai điểm.
///
/// `Awake` dưới đây đưa pivot về giữa mà KHÔNG làm bánh răng xê dịch (xem
/// <see cref="MillRectUtil.DoiPivotVeGiua"/>). Đây là lưới an toàn ở phía RUNTIME: kể cả
/// scene/prefab cũ chưa dựng lại, hoặc ai đó thêm một bánh răng mới bằng tay, nó vẫn xoay
/// đúng. Tool cũng đã được sửa để dựng sẵn pivot giữa.
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

    private void Awake()
    {
        // Bánh răng trong popup là UI (RectTransform); bánh răng world (FeedMillGears.prefab)
        // chỉ có Transform thường — `as` trả null, bỏ qua an toàn.
        RectTransform rt = transform as RectTransform;
        if (rt != null)
            MillRectUtil.DoiPivotVeGiua(rt);
    }

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
