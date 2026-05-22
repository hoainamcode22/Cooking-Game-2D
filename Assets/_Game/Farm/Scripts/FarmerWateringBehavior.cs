using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào root của prefab PFB_FarmerWatering (cùng node với FarmerWateringAnimator).
/// Điều khiển NPC tưới nước: đi tới ô đất, tưới, quay về.
///
/// Nhận job từ FarmerWateringJobManager qua AssignWateringTask().
///
/// Animator flow:
///   Idle  (SetMoving(false) — đứng tại homePosition)
///   Walk  (SetMoving(true)  — đi tới plot)
///   Idle  (SetMoving(false))
///   Watering (PlayWatering() — animation tưới)
///   Walk  (SetMoving(true)  — đi về home)
///   Idle  (SetMoving(false))
/// </summary>
[RequireComponent(typeof(FarmerWateringAnimator))]
public class FarmerWateringBehavior : MonoBehaviour
{
    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed       = 3f;
    [SerializeField] private float arrivalDistance = 0.5f;

    [Header("Tưới nước")]
    [Tooltip("Thời gian chờ clip Watering (giây) — điều chỉnh theo độ dài clip thực tế")]
    [SerializeField] private float wateringDuration = 1.5f;

    [Tooltip("Số giây rút khỏi thời gian phát triển khi tưới (ApplyWaterBonus)")]
    [SerializeField] private int waterBonusSeconds = 30;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>True khi NPC đang thực hiện nhiệm vụ.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Phát khi một task hoàn thành. Tham số: (behavior, plotId).</summary>
    public event System.Action<FarmerWateringBehavior, int> OnJobComplete;

    // ── Private ───────────────────────────────────────────────────────────────

    private FarmerWateringAnimator farmerAnimator;
    private Vector3 homePosition;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        farmerAnimator = GetComponent<FarmerWateringAnimator>();
    }

    private void Start()
    {
        homePosition = transform.position;
        if (farmerAnimator != null) farmerAnimator.SetMoving(false);
        Debug.Log($"[FarmerWateringBehavior:{name}] Home saved: {homePosition}");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Giao nhiệm vụ tưới nước cho ô đất này.
    /// Chỉ gọi khi IsBusy == false.
    /// </summary>
    public void AssignWateringTask(PlotController plot)
    {
        if (plot == null)
        {
            Debug.LogWarning($"[FarmerWateringBehavior:{name}] AssignWateringTask: plot null — bỏ qua");
            return;
        }

        if (IsBusy)
        {
            Debug.LogWarning($"[FarmerWateringBehavior:{name}] Đang bận — task bị từ chối");
            return;
        }

        Debug.Log($"[FarmerWateringBehavior] Watering task assigned → plot {plot.PlotId}");
        StartCoroutine(ExecuteTask(plot));
    }

    // ── Coroutine chính ───────────────────────────────────────────────────────

    private IEnumerator ExecuteTask(PlotController target)
    {
        IsBusy = true;
        int plotId = target.PlotId;

        // Kiểm tra trạng thái trước khi xuất phát
        if (target == null || !target.IsGrowing)
        {
            Debug.Log($"[FarmerWateringBehavior] Plot {plotId} không ở trạng thái Growing — bỏ task");
            IsBusy = false;
            OnJobComplete?.Invoke(this, plotId);
            yield break;
        }

        // ── Đi tới ô đất ─────────────────────────────────────────────────────
        Vector3 standPos = target.GetFarmerStandPosition();
        Debug.Log($"[FarmerWateringBehavior] Moving to planted seed → plot {plotId} tại {standPos}");

        if (farmerAnimator != null) farmerAnimator.SetMoving(true);
        yield return StartCoroutine(WalkTo(standPos));
        if (farmerAnimator != null) farmerAnimator.SetMoving(false);

        // Kiểm tra lại sau khi đi xong
        if (target == null || !target.IsGrowing)
        {
            Debug.Log($"[FarmerWateringBehavior] Plot {plotId} hết Growing sau khi đi — quay về");
            yield return StartCoroutine(ReturnHome());
            IsBusy = false;
            OnJobComplete?.Invoke(this, plotId);
            yield break;
        }

        // ── Tưới nước ─────────────────────────────────────────────────────────
        Debug.Log($"[FarmerWateringBehavior] Watering started → plot {plotId}");
        if (farmerAnimator != null) farmerAnimator.PlayWatering();
        yield return new WaitForSeconds(wateringDuration);

        // ── Áp dụng hiệu ứng tưới ────────────────────────────────────────────
        if (target != null && target.IsGrowing)
        {
            target.ApplyWaterBonus(waterBonusSeconds);
            Debug.Log($"[FarmerWateringBehavior] Water applied → plot {plotId} (-{waterBonusSeconds}s grow time)");
        }
        else
        {
            Debug.LogWarning($"[FarmerWateringBehavior] Plot {plotId} không còn Growing khi tưới xong — bỏ qua ApplyWaterBonus");
        }

        // ── Quay về home ──────────────────────────────────────────────────────
        yield return StartCoroutine(ReturnHome());

        IsBusy = false;
        OnJobComplete?.Invoke(this, plotId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator ReturnHome()
    {
        if (farmerAnimator != null) farmerAnimator.SetMoving(true);
        yield return StartCoroutine(WalkTo(homePosition));
        if (farmerAnimator != null) farmerAnimator.SetMoving(false);
        Debug.Log($"[FarmerWateringBehavior:{name}] Đã về home — Idle");
    }

    /// Di chuyển tới đích, lật hướng khi đổi chiều ngang.
    private IEnumerator WalkTo(Vector3 destination)
    {
        while (Vector3.Distance(transform.position, destination) > arrivalDistance)
        {
            float dx = destination.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                Vector3 s = transform.localScale;
                s.x = dx > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                transform.localScale = s;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
    }
}
