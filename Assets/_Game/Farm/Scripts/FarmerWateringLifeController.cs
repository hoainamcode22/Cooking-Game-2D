using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn vào root NPC cũ (Animation_cuocdat) sau khi thay visual.
/// Điều khiển chuỗi hành động sống động: di chuyển tới điểm đứng → tưới → ăn mừng → đi dạo → idle.
/// Được gọi bởi FarmerAnimationAdapter khi FarmerBehavior trigger Work.
/// </summary>
public class FarmerWateringLifeController : MonoBehaviour
{
    [SerializeField] private FarmerWateringAnimator wateringAnimator;
    [SerializeField] private Transform              visualRoot;

    [Header("Watering stand offset from plot center")]
    [SerializeField] private Vector3 wateringStandOffset = new Vector3(-0.6f, -0.5f, 0f);

    [Header("Wander")]
    [SerializeField] private float wanderRadius  = 1.2f;
    [SerializeField] private float wanderSpeed   = 1.0f;
    [SerializeField] private float arrivalThresh = 0.12f;

    private Coroutine _routine;
    private Vector3   _homePos;

    private void Start()
    {
        _homePos = transform.position;
        if (wateringAnimator == null)
            wateringAnimator = GetComponentInChildren<FarmerWateringAnimator>(true);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayWorkLifeSequence(PlotController plot)
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(WorkLifeRoutine(plot));
        Debug.Log("[FarmerWateringLifeController] WorkLifeSequence started on " + name);
    }

    // ── Coroutine chính ───────────────────────────────────────────────────────

    private IEnumerator WorkLifeRoutine(PlotController plot)
    {
        // 1. Fine-tune tới điểm đứng tưới
        if (plot != null)
        {
            Vector3 standPoint = plot.transform.position + wateringStandOffset;
            Debug.Log("[FarmerWateringBehavior] Plot center = " + plot.transform.position);
            Debug.Log("[FarmerWateringBehavior] Stand point = " + standPoint);

            float speed = wanderSpeed * 2f;
            wateringAnimator?.SetMoving(true);

            while (Vector3.Distance(transform.position, standPoint) > arrivalThresh)
            {
                float dx = standPoint.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f)
                {
                    Vector3 s = transform.localScale;
                    s.x = dx > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                    transform.localScale = s;
                }
                transform.position = Vector3.MoveTowards(transform.position, standPoint, speed * Time.deltaTime);
                yield return null;
            }

            Debug.Log("[FarmerWateringBehavior] NPC arrived at stand point.");
        }

        // 2. Đứng yên tưới nước (không lean người, không động chân)
        wateringAnimator?.SetMoving(false);
        wateringAnimator?.PlayWatering();
        yield return new WaitForSeconds(3.2f);   // clip dài 3s + buffer

        // 3. Ăn mừng
        wateringAnimator?.PlayCelebrate();
        yield return new WaitForSeconds(0.9f);   // clip dài 0.8s + buffer

        // 4. Đi dạo quanh
        int wanderCount = Random.Range(1, 3);
        yield return StartCoroutine(WanderRoutine(wanderCount));

        // 5. Về idle
        wateringAnimator?.SetMoving(false);
        _routine = null;
    }

    private IEnumerator WanderRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float   dir    = Random.value > 0.5f ? 1f : -1f;
            float   radius = Random.Range(wanderRadius * 0.6f, wanderRadius);
            Vector3 target = new Vector3(
                _homePos.x + dir * radius,
                transform.position.y,
                transform.position.z);

            float speed = Random.Range(wanderSpeed * 0.8f, wanderSpeed * 1.2f);
            wateringAnimator?.SetMoving(true);

            while (Vector3.Distance(transform.position, target) > arrivalThresh)
            {
                float dx = target.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f)
                {
                    Vector3 s = transform.localScale;
                    s.x = dx > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                    transform.localScale = s;
                }

                transform.position = Vector3.MoveTowards(
                    transform.position, target, speed * Time.deltaTime);
                yield return null;
            }

            wateringAnimator?.SetMoving(false);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
