using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào từng object farmer NPC (Animation_cuocdat, (1), (2), (3), ...).
/// KHÔNG tự subscribe FarmManager.OnPlotPlantedEvent nữa.
/// Nhận job từ FarmerJobManager qua AssignPlotJob().
///
/// Animator flow:
///   LiftHoe  (idle / chờ tại homePosition)
///   WalkHoe  (đi tới plot — Play trực tiếp)
///   → SetTrigger("StartWork") → WalkToWork → HoeLoop (auto transition)
///   → SetTrigger("Rest") khi plot sang stage 1 (progress >= 0.5) → RestWipe
///   WalkHoe  (đi về home)
///   LiftHoe  (idle)
/// </summary>
[RequireComponent(typeof(Animator))]
public class FarmerBehavior : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Tốc độ di chuyển (world units/giây)")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Khoảng cách coi là đã tới đích (world units)")]
    [SerializeField] private float arrivalDistance = 0.5f;

    [Header("Timing")]
    [Tooltip("Thời gian WalkToWork animation trước khi HoeLoop (giây)")]
    [SerializeField] private float walkToWorkDuration = 0.6f;

    [Tooltip("Thời gian RestWipe animation (giây)")]
    [SerializeField] private float restDuration = 1.2f;

    [Tooltip("Chu kỳ poll kiểm tra stage transition (giây)")]
    [SerializeField] private float stagePollInterval = 0.3f;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>True khi farmer đang thực hiện job (đi tới, cuốc, hoặc về nhà).</summary>
    public bool IsBusy { get; private set; }

    /// <summary>
    /// Gọi bởi FarmerJobManager khi job hoàn thành.
    /// Tham số: (farmer, plotId vừa xong).
    /// </summary>
    public event System.Action<FarmerBehavior, int> OnJobComplete;

    // ── Private state ─────────────────────────────────────────────────────────

    private Animator animator;
    private Vector3 homePosition;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("[Farmer] Animator not found on " + name);
    }

    private void Start()
    {
        homePosition = transform.position;
        Debug.Log($"[Farmer:{name}] Home position saved: {homePosition}");

        if (animator != null)
            animator.Play("LiftHoe");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Giao một plot job cho farmer này.
    /// Chỉ gọi khi IsBusy == false.
    /// </summary>
    public void AssignPlotJob(PlotController plot)
    {
        if (plot == null)
        {
            Debug.LogWarning($"[Farmer:{name}] AssignPlotJob called with null plot");
            return;
        }

        if (IsBusy)
        {
            Debug.LogWarning($"[Farmer:{name}] AssignPlotJob called while already busy — ignored");
            return;
        }

        Debug.Log($"[Farmer:{name}] Job started for plot {plot.PlotId}");
        StartCoroutine(ExecuteJob(plot));
    }

    // ── Job execution ─────────────────────────────────────────────────────────

    private IEnumerator ExecuteJob(PlotController target)
    {
        IsBusy = true;
        int plotId = target.PlotId;

        // Plot không còn Growing khi job bắt đầu (edge case)
        if (!target.IsGrowing)
        {
            Debug.Log($"[Farmer:{name}] Plot {plotId} not Growing at job start — abort");
            IsBusy = false;
            OnJobComplete?.Invoke(this, plotId);
            yield break;
        }

        // ── Đi tới plot ───────────────────────────────────────────────────────
        Vector3 standPos = target.GetFarmerStandPosition();
        Debug.Log($"[Farmer:{name}] Walking to plot {plotId} at {standPos}");
        if (animator != null) animator.Play("WalkHoe");
        yield return StartCoroutine(WalkTo(standPos));

        // Kiểm tra lại sau khi đi xong
        if (!target.IsGrowing)
        {
            Debug.Log($"[Farmer:{name}] Plot {plotId} no longer Growing after walk — return home");
            yield return StartCoroutine(ReturnHome());
            IsBusy = false;
            OnJobComplete?.Invoke(this, plotId);
            yield break;
        }

        // ── Tới nơi: trigger WalkToWork ───────────────────────────────────────
        Debug.Log($"[Farmer:{name}] Arrived at plot {plotId} — trigger StartWork");
        if (animator != null) animator.SetTrigger("StartWork");
        yield return new WaitForSeconds(walkToWorkDuration);

        // ── HoeLoop: chờ đến khi plot sang stage 1 (progress >= 0.5) ──────────
        Debug.Log($"[Farmer:{name}] Hoe start (stage 0) for plot {plotId} | progress={target.GetGrowProgress01():F2}");
        yield return StartCoroutine(WaitForStageOne(target));

        // ── Trigger Rest ──────────────────────────────────────────────────────
        Debug.Log($"[Farmer:{name}] Plot {plotId} reached stage 1 — trigger Rest | progress={target.GetGrowProgress01():F2}");
        if (animator != null) animator.SetTrigger("Rest");
        yield return new WaitForSeconds(restDuration);

        // ── Về home ───────────────────────────────────────────────────────────
        Debug.Log($"[Farmer:{name}] Job done for plot {plotId} — walking home");
        yield return StartCoroutine(ReturnHome());

        IsBusy = false;
        OnJobComplete?.Invoke(this, plotId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// Chờ cho đến khi plot thoát stage 0 (progress >= 0.5) hoặc không còn Growing.
    private IEnumerator WaitForStageOne(PlotController target)
    {
        while (target != null && target.IsGrowing && target.GetGrowProgress01() < 0.5f)
        {
            yield return new WaitForSeconds(stagePollInterval);
        }
    }

    /// Đi bộ về homePosition rồi phát LiftHoe.
    private IEnumerator ReturnHome()
    {
        if (animator != null) animator.Play("WalkHoe");
        yield return StartCoroutine(WalkTo(homePosition));
        if (animator != null) animator.Play("LiftHoe");
        Debug.Log($"[Farmer:{name}] Arrived home — LiftHoe idle");
    }

    /// Di chuyển trong world space tới đích cố định.
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
