using UnityEngine;

/// <summary>
/// Ambient animation controller cho animal NPC (bò, heo, gà).
/// - Tự đổi state ngẫu nhiên sau vài giây.
/// - Khi ở walkState: đi bộ quanh chuồng trong bán kính walkRadius.
/// - Khi ở eatState / restState: đứng yên tại chỗ.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimalAmbientController : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Tự tìm nếu để trống")]
    [SerializeField] private Animator animator;

    [Tooltip("Tên int parameter trong Animator Controller (kiểm tra tab Parameters)")]
    [SerializeField] private string stateParamName = "State";

    [Tooltip("Tốc độ phát animation (1 = bình thường, 0.4 = chậm)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float animationSpeed = 0.4f;

    [Header("State Values")]
    [SerializeField] private int walkState = 0;
    [SerializeField] private int eatState  = 1;
    [SerializeField] private int restState = 2;

    [Header("Timing")]
    [Tooltip("Thời gian tối thiểu giữ một state (giây)")]
    [SerializeField] private float minStateDuration = 4f;

    [Tooltip("Thời gian tối đa giữ một state (giây)")]
    [SerializeField] private float maxStateDuration = 8f;

    [Header("Walk Movement")]
    [Tooltip("Tốc độ di chuyển khi đi bộ (world units/giây)")]
    [SerializeField] private float moveSpeed = 0.6f;

    [Tooltip("Bán kính vùng đi quanh tính từ vị trí ban đầu (world units). Chỉnh cho vừa chuồng.")]
    [SerializeField] private float walkRadius = 1.5f;

    [Tooltip("Khoảng cách coi là đã tới đích (world units)")]
    [SerializeField] private float arrivalThreshold = 0.15f;

    [Header("Behaviour")]
    [Tooltip("Chọn state ngẫu nhiên ngay lúc Start")]
    [SerializeField] private bool randomOnStart = true;

    [Tooltip("Walk có xác suất cao hơn (50% Walk, 25% Eat, 25% Rest)")]
    [SerializeField] private bool preferWalk = true;

    // ─────────────────────────────────────────────────────────────────────────

    private int   currentState  = -1;
    private float stateTimer    = 0f;
    private float nextStateTime = 0f;

    // Vị trí trung tâm chuồng — lưu lại lúc Start
    private Vector3 penCenter;

    // Điểm đang đi tới khi Walk
    private Vector3 walkTarget;
    private bool    hasWalkTarget = false;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator != null)
            animator.speed = animationSpeed;
    }

    private void Start()
    {
        penCenter = transform.position;

        int initial = randomOnStart ? PickRandomState(-1) : walkState;
        ApplyState(initial);

        nextStateTime = Random.Range(minStateDuration, maxStateDuration);
        stateTimer    = 0f;
    }

    private void Update()
    {
        // ── Đổi state theo timer ──────────────────────────────────────────────
        stateTimer += Time.deltaTime;
        if (stateTimer >= nextStateTime)
        {
            ApplyState(PickRandomState(currentState));
            stateTimer    = 0f;
            nextStateTime = Random.Range(minStateDuration, maxStateDuration);
        }

        // ── Di chuyển khi Walk ────────────────────────────────────────────────
        if (currentState == walkState)
            HandleWalkMovement();
    }

    // ── Walk movement ─────────────────────────────────────────────────────────

    private void HandleWalkMovement()
    {
        // Chọn điểm mới nếu chưa có hoặc đã tới đích
        if (!hasWalkTarget || Vector3.Distance(transform.position, walkTarget) <= arrivalThreshold)
        {
            walkTarget    = PickPointInPen();
            hasWalkTarget = true;
        }

        // Flip sprite theo hướng di chuyển (trục X)
        float dx = walkTarget.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f)
        {
            Vector3 s = transform.localScale;
            s.x = dx > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
            transform.localScale = s;
        }

        // Di chuyển tới đích
        transform.position = Vector3.MoveTowards(
            transform.position,
            walkTarget,
            moveSpeed * Time.deltaTime
        );
    }

    /// Chọn một điểm ngẫu nhiên trong vòng tròn bán kính walkRadius quanh penCenter.
    private Vector3 PickPointInPen()
    {
        Vector2 offset = Random.insideUnitCircle * walkRadius;
        return new Vector3(penCenter.x + offset.x, penCenter.y + offset.y, penCenter.z);
    }

    // ── State helpers ─────────────────────────────────────────────────────────

    private void ApplyState(int state)
    {
        currentState  = state;
        hasWalkTarget = false; // reset để chọn điểm đi mới khi vào Walk

        if (animator != null)
            animator.SetInteger(stateParamName, state);
    }

    private int PickRandomState(int excludeState)
    {
        int[] pool = preferWalk
            ? new[] { walkState, walkState, eatState, restState }
            : new[] { walkState, eatState, restState };

        for (int i = 0; i < 20; i++)
        {
            int candidate = pool[Random.Range(0, pool.Length)];
            if (candidate != excludeState)
                return candidate;
        }

        foreach (int s in pool)
            if (s != excludeState) return s;

        return walkState;
    }

    // Hiện vùng đi trong Scene view để dễ chỉnh walkRadius
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? penCenter : transform.position;
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.3f);
        Gizmos.DrawSphere(center, walkRadius);
        Gizmos.color = new Color(0.2f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(center, walkRadius);
    }
}
