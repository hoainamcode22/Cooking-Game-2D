using System.Collections;
using UnityEngine;

/// <summary>
/// NPC nông dân tự di chuyển giữa các điểm neo và tự thực hiện hành động.
/// Script này không đọc input người chơi.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NPCFarmController : MonoBehaviour
{
    private enum NPCState
    {
        Moving,
        Acting
    }

    [Header("Điểm neo")]
    [Tooltip("4 điểm NPC sẽ đi tới lần lượt. Có thể kéo từng Anchor trong Scene để đổi đường đi.")]
    public Transform[] waypoints = new Transform[4];

    [Header("Thiết lập di chuyển")]
    [Tooltip("Tốc độ di chuyển của NPC. Tăng giá trị này nếu muốn nông dân đi nhanh hơn.")]
    public float moveSpeed = 2f;

    [Tooltip("Thời gian NPC đứng chờ và làm hành động ở mỗi điểm neo.")]
    public float actionDelay = 1.5f;

    [Tooltip("Khoảng cách đủ gần để xem như NPC đã tới điểm neo.")]
    [SerializeField] private float arrivalDistance = 0.03f;

    [Header("Animator Parameters")]
    [Tooltip("Tên Float parameter điều khiển hướng ngang trong Animator.")]
    [SerializeField] private string directionXParameter = "Direction X";

    [Tooltip("Tên Float parameter điều khiển hướng dọc trong Animator.")]
    [SerializeField] private string directionYParameter = "Direction Y";

    [Tooltip("Tên Float parameter điều khiển trạng thái đi/đứng.")]
    [SerializeField] private string speedParameter = "Speed";

    [Tooltip("Tên Trigger phát animation tưới cây.")]
    [SerializeField] private string waterTriggerParameter = "Water";

    [Tooltip("Tên Trigger phát animation nhảy ăn mừng.")]
    [SerializeField] private string celebrateTriggerParameter = "Celebrate";

    private Animator animator;
    private NPCState currentState = NPCState.Moving;
    private Vector3[] cachedWaypointPositions;
    private Vector2 lastDirection = Vector2.down;
    private int currentWaypointIndex;
    private Coroutine actionRoutine;

    private int directionXHash;
    private int directionYHash;
    private int speedHash;
    private int waterTriggerHash;
    private int celebrateTriggerHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        CacheAnimatorHashes();
        CacheWaypointPositions();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        actionDelay = Mathf.Max(0f, actionDelay);
        arrivalDistance = Mathf.Max(0.001f, arrivalDistance);
        CacheAnimatorHashes();
    }

    private void Update()
    {
        if (currentState != NPCState.Moving)
        {
            UpdateAnimator(lastDirection, 0f);
            return;
        }

        if (!HasValidWaypoints())
        {
            UpdateAnimator(lastDirection, 0f);
            return;
        }

        MoveToCurrentWaypoint();
    }

    /// <summary>
    /// Gọi hàm này nếu bạn đổi vị trí Anchor bằng code trước khi cho NPC bắt đầu chạy.
    /// </summary>
    public void RefreshWaypointPositions()
    {
        CacheWaypointPositions();
        currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 0, Mathf.Max(0, cachedWaypointPositions.Length - 1));
    }

    private void MoveToCurrentWaypoint()
    {
        Vector3 targetPosition = cachedWaypointPositions[currentWaypointIndex];
        Vector2 toTarget = targetPosition - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= arrivalDistance)
        {
            transform.position = targetPosition;
            StartAnchorAction();
            return;
        }

        Vector2 direction = toTarget.normalized;
        lastDirection = direction;

        Vector2 nextPosition = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        UpdateAnimator(direction, moveSpeed);
    }

    private void StartAnchorAction()
    {
        currentState = NPCState.Acting;
        UpdateAnimator(lastDirection, 0f);

        if (Random.value < 0.5f)
        {
            animator.ResetTrigger(celebrateTriggerHash);
            animator.SetTrigger(waterTriggerHash);
        }
        else
        {
            animator.ResetTrigger(waterTriggerHash);
            animator.SetTrigger(celebrateTriggerHash);
        }

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
        }

        actionRoutine = StartCoroutine(WaitThenGoNextWaypoint());
    }

    private IEnumerator WaitThenGoNextWaypoint()
    {
        if (actionDelay > 0f)
        {
            yield return new WaitForSeconds(actionDelay);
        }

        currentWaypointIndex = (currentWaypointIndex + 1) % cachedWaypointPositions.Length;
        currentState = NPCState.Moving;
        actionRoutine = null;
    }

    private void CacheWaypointPositions()
    {
        if (waypoints == null)
        {
            cachedWaypointPositions = new Vector3[0];
            return;
        }

        int validCount = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                validCount++;
            }
        }

        cachedWaypointPositions = new Vector3[validCount];
        int writeIndex = 0;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            // Anchor là con của NPC theo yêu cầu setup, nên cần lưu vị trí world lúc bắt đầu.
            // Nhờ vậy NPC không bị đuổi theo waypoint đang di chuyển cùng chính nó.
            cachedWaypointPositions[writeIndex] = waypoints[i].position;
            writeIndex++;
        }
    }

    private bool HasValidWaypoints()
    {
        return cachedWaypointPositions != null && cachedWaypointPositions.Length > 0;
    }

    private void UpdateAnimator(Vector2 direction, float speed)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(directionXHash, direction.x);
        animator.SetFloat(directionYHash, direction.y);
        animator.SetFloat(speedHash, speed);
    }

    private void CacheAnimatorHashes()
    {
        directionXHash = Animator.StringToHash(directionXParameter);
        directionYHash = Animator.StringToHash(directionYParameter);
        speedHash = Animator.StringToHash(speedParameter);
        waterTriggerHash = Animator.StringToHash(waterTriggerParameter);
        celebrateTriggerHash = Animator.StringToHash(celebrateTriggerParameter);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Vector3 previousPoint = Vector3.zero;
        bool hasPreviousPoint = false;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
            {
                continue;
            }

            Vector3 point = waypoints[i].position;
            Gizmos.DrawWireSphere(point, 0.15f);

            if (hasPreviousPoint)
            {
                Gizmos.DrawLine(previousPoint, point);
            }

            previousPoint = point;
            hasPreviousPoint = true;
        }

        if (hasPreviousPoint && waypoints[0] != null)
        {
            Gizmos.DrawLine(previousPoint, waypoints[0].position);
        }
    }
}
