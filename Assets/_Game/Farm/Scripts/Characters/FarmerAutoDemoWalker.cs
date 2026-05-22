using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class FarmerAutoDemoWalker : MonoBehaviour
{
    [Header("Demo AI")]
    public bool enableDemoAI = true;
    public float moveSpeed = 1.2f;
    public float wanderRadius = 2.5f;
    public float jumpInterval = 4f;
    public float wateringInterval = 6f;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsJumping = Animator.StringToHash("IsJumping");
    private static readonly int IsWatering = Animator.StringToHash("IsWatering");

    private Animator animator;
    private SortingGroup sortingGroup;
    private Vector3 origin;
    private Vector3 target;
    private float nextTargetTime;
    private float nextJumpTime;
    private float nextWateringTime;
    private bool busy;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sortingGroup = GetComponent<SortingGroup>();
        origin = transform.position;
        PickTarget();
    }

    private void OnEnable()
    {
        nextJumpTime = Time.time + Random.Range(jumpInterval * 0.6f, jumpInterval * 1.4f);
        nextWateringTime = Time.time + Random.Range(wateringInterval * 0.6f, wateringInterval * 1.4f);
    }

    private void Update()
    {
        if (!enableDemoAI || animator == null)
        {
            SetWalking(false);
            return;
        }

        if (!busy)
        {
            UpdateWander();
            TryStartActions();
        }

        UpdateSorting();
    }

    private void UpdateWander()
    {
        Vector3 pos = transform.position;
        Vector3 toTarget = target - pos;
        toTarget.z = 0f;

        if (toTarget.sqrMagnitude < 0.01f || Time.time >= nextTargetTime)
        {
            PickTarget();
            toTarget = target - transform.position;
            toTarget.z = 0f;
        }

        bool walking = toTarget.sqrMagnitude > 0.01f;
        SetWalking(walking);
        if (!walking) return;

        Vector3 delta = toTarget.normalized * (moveSpeed * Time.deltaTime);
        if (delta.sqrMagnitude > toTarget.sqrMagnitude)
            delta = toTarget;

        transform.position += delta;

        if (Mathf.Abs(delta.x) > 0.001f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (delta.x < 0f ? -1f : 1f);
            transform.localScale = scale;
        }
    }

    private void TryStartActions()
    {
        if (Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpRoutine());
            nextJumpTime = Time.time + Random.Range(jumpInterval * 0.75f, jumpInterval * 1.5f);
            return;
        }

        if (Time.time >= nextWateringTime)
        {
            StartCoroutine(WateringRoutine());
            nextWateringTime = Time.time + Random.Range(wateringInterval * 0.75f, wateringInterval * 1.5f);
        }
    }

    private IEnumerator JumpRoutine()
    {
        busy = true;
        SetWalking(false);
        animator.SetBool(IsJumping, true);
        yield return new WaitForSeconds(0.55f);
        animator.SetBool(IsJumping, false);
        busy = false;
    }

    private IEnumerator WateringRoutine()
    {
        busy = true;
        SetWalking(false);
        animator.SetBool(IsWatering, true);
        yield return new WaitForSeconds(1.25f);
        animator.SetBool(IsWatering, false);
        busy = false;
    }

    private void PickTarget()
    {
        Vector2 random = Random.insideUnitCircle * Mathf.Max(0.1f, wanderRadius);
        target = origin + new Vector3(random.x, random.y, 0f);
        nextTargetTime = Time.time + Random.Range(2f, 4f);
    }

    private void SetWalking(bool value)
    {
        if (animator != null)
            animator.SetBool(IsWalking, value);
    }

    private void UpdateSorting()
    {
        if (sortingGroup != null)
            sortingGroup.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }
}
