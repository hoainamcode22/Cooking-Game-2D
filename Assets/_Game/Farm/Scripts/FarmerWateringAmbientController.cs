using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FarmerWateringAmbientController : MonoBehaviour
{
    private const string LogPrefix = "[FarmerWateringAmbient]";

    [Header("References")]
    [SerializeField] private FarmerWateringAnimator wateringAnimator;
    [SerializeField] private Transform visualRoot;

    [Header("Wander")]
    [SerializeField] private Vector2 wanderRadiusRange = new Vector2(120f, 260f);
    [SerializeField] private Vector2 walkSpeedRange = new Vector2(70f, 110f);
    [SerializeField] private Vector2 walkDurationRange = new Vector2(1f, 3f);
    [SerializeField] private Vector2 pauseDurationRange = new Vector2(0.5f, 1f);
    [SerializeField] private float arrivalDistance = 4f;

    [Header("Watering")]
    [Range(0f, 1f)]
    [SerializeField] private float wateringChance = 0.5f;
    [SerializeField] private Vector2 wateringDurationRange = new Vector2(2.5f, 4f);

    [Header("Celebrate")]
    [SerializeField] private Vector2Int celebrateJumpRange = new Vector2Int(2, 4);
    [SerializeField] private Vector2 celebrateDurationRange = new Vector2(1.2f, 2f);
    [SerializeField] private float celebrateJumpHeight = 0.35f;
    [SerializeField] private float celebrateScaleBonus = 0.04f;

    private Coroutine ambientRoutine;
    private Vector3 homePosition;
    private Vector3 baseVisualLocalPosition;
    private Vector3 baseVisualLocalScale;
    private float facingScaleSign = 1f;

    private void Awake()
    {
        ResolveReferences();
        DisableLegacyJobComponents();
        ConfigureVisualForNoClipping();
        ValidateNoVisualClipping(gameObject);
    }

    private void OnEnable()
    {
        homePosition = transform.position;
        CaptureVisualBasePose();

        if (ambientRoutine == null)
            ambientRoutine = StartCoroutine(AmbientLoop());
    }

    private void OnDisable()
    {
        if (ambientRoutine != null)
        {
            StopCoroutine(ambientRoutine);
            ambientRoutine = null;
        }

        if (wateringAnimator != null)
            wateringAnimator.SetMoving(false);

        RestoreVisualBasePose();
    }

    private void ResolveReferences()
    {
        if (wateringAnimator == null)
            wateringAnimator = GetComponentInChildren<FarmerWateringAnimator>(true);

        if (visualRoot == null)
        {
            Transform namedVisual = transform.Find("Visual_FarmerWatering");
            visualRoot = namedVisual != null
                ? namedVisual
                : wateringAnimator != null ? wateringAnimator.transform : transform;
        }

        if (wateringAnimator != null && wateringAnimator.animator == null)
            wateringAnimator.animator = wateringAnimator.GetComponent<Animator>();
    }

    private void DisableLegacyJobComponents()
    {
        FarmerBehavior farmerBehavior = GetComponent<FarmerBehavior>();
        if (farmerBehavior != null)
            farmerBehavior.enabled = false;

        FarmerAnimationAdapter adapter = GetComponent<FarmerAnimationAdapter>();
        if (adapter != null)
            adapter.enabled = false;

        FarmerWateringLifeController lifeController = GetComponent<FarmerWateringLifeController>();
        if (lifeController != null)
            lifeController.enabled = false;

        foreach (FarmerWateringBehavior behavior in GetComponentsInChildren<FarmerWateringBehavior>(true))
            behavior.enabled = false;
    }

    private void ConfigureVisualForNoClipping()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.maskInteraction = SpriteMaskInteraction.None;
    }

    private void CaptureVisualBasePose()
    {
        if (visualRoot == null)
            return;

        baseVisualLocalPosition = visualRoot.localPosition;
        baseVisualLocalScale = visualRoot.localScale;
    }

    private void RestoreVisualBasePose()
    {
        if (visualRoot == null)
            return;

        visualRoot.localPosition = baseVisualLocalPosition;
        visualRoot.localScale = baseVisualLocalScale;
    }

    private IEnumerator AmbientLoop()
    {
        Debug.Log($"{LogPrefix} Ambient loop started");

        while (true)
        {
            Vector3 target = PickWanderTarget(out float speed, out float maxWalkTime);
            Debug.Log($"{LogPrefix} Walking to target {target}");

            wateringAnimator?.SetMoving(true);
            yield return WalkTo(target, speed, maxWalkTime);
            wateringAnimator?.SetMoving(false);

            yield return new WaitForSeconds(Random.Range(pauseDurationRange.x, pauseDurationRange.y));

            if (Random.value <= wateringChance)
            {
                Debug.Log($"{LogPrefix} Watering started");
                wateringAnimator?.SetMoving(false);
                wateringAnimator?.PlayWatering();
                yield return new WaitForSeconds(Random.Range(wateringDurationRange.x, wateringDurationRange.y));

                Debug.Log($"{LogPrefix} Celebrate started");
                wateringAnimator?.PlayCelebrate();
                yield return CelebrateRoutine();
            }

            Debug.Log($"{LogPrefix} Continue wandering");
            yield return null;
        }
    }

    private Vector3 PickWanderTarget(out float speed, out float maxWalkTime)
    {
        speed = Random.Range(walkSpeedRange.x, walkSpeedRange.y);
        maxWalkTime = Random.Range(walkDurationRange.x, walkDurationRange.y);

        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.001f)
            dir = Vector2.right;

        dir.Normalize();
        float desiredDistance = Mathf.Clamp(
            speed * maxWalkTime,
            wanderRadiusRange.x,
            wanderRadiusRange.y);

        Vector3 target = homePosition + new Vector3(dir.x, dir.y, 0f) * desiredDistance;
        target.z = transform.position.z;
        return target;
    }

    private IEnumerator WalkTo(Vector3 target, float speed, float maxWalkTime)
    {
        float elapsed = 0f;

        while (elapsed < maxWalkTime &&
               Vector3.Distance(transform.position, target) > arrivalDistance)
        {
            FaceTarget(target);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void FaceTarget(Vector3 target)
    {
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) <= 0.01f)
            return;

        Vector3 scale = transform.localScale;
        if (!Mathf.Approximately(scale.x, 0f))
            facingScaleSign = Mathf.Sign(scale.x);

        float absX = Mathf.Abs(scale.x);
        scale.x = dx >= 0f ? absX : -absX;
        if (Mathf.Sign(scale.x) != facingScaleSign)
            facingScaleSign = Mathf.Sign(scale.x);

        transform.localScale = scale;
    }

    private IEnumerator CelebrateRoutine()
    {
        if (visualRoot == null)
            yield break;

        int jumps = Random.Range(celebrateJumpRange.x, celebrateJumpRange.y + 1);
        float duration = Random.Range(celebrateDurationRange.x, celebrateDurationRange.y);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalized = Mathf.Clamp01(elapsed / duration);
            float jumpPhase = Mathf.Repeat(normalized * jumps, 1f);
            float hop = Mathf.Sin(jumpPhase * Mathf.PI);

            visualRoot.localPosition = baseVisualLocalPosition + Vector3.up * (hop * celebrateJumpHeight);
            visualRoot.localScale = baseVisualLocalScale * (1f + hop * celebrateScaleBonus);

            elapsed += Time.deltaTime;
            yield return null;
        }

        RestoreVisualBasePose();
    }

    public static void ValidateNoVisualClipping(GameObject root)
    {
        Debug.Log($"{LogPrefix} Checking clipping sources...");

        if (root == null)
        {
            Debug.LogWarning($"{LogPrefix} WARNING: Mask source found on <null root>");
            return;
        }

        bool foundClippingSource = false;

        for (Transform current = root.transform; current != null; current = current.parent)
        {
            if (current.GetComponent<Mask>() != null)
                foundClippingSource |= LogMaskWarning(current.name);

            if (current.GetComponent<RectMask2D>() != null)
                foundClippingSource |= LogMaskWarning(current.name);

            if (current.GetComponent<SpriteMask>() != null)
                foundClippingSource |= LogMaskWarning(current.name);

            SpriteRenderer parentRenderer = current.GetComponent<SpriteRenderer>();
            if (parentRenderer != null && parentRenderer.maskInteraction != SpriteMaskInteraction.None)
                foundClippingSource |= LogMaskWarning(current.name);

            if (current.GetComponent<Canvas>() != null ||
                current.GetComponent<RectTransform>() != null ||
                current.name.IndexOf("Viewport", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundClippingSource |= LogMaskWarning(current.name);
            }
        }

        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.maskInteraction == SpriteMaskInteraction.None)
                continue;

            foundClippingSource |= LogMaskWarning(sr.name);
            sr.maskInteraction = SpriteMaskInteraction.None;
        }

        if (!foundClippingSource)
            Debug.Log($"{LogPrefix} No mask clipping found");
    }

    private static bool LogMaskWarning(string objectName)
    {
        Debug.LogWarning($"{LogPrefix} WARNING: Mask source found on {objectName}");
        return true;
    }
}
