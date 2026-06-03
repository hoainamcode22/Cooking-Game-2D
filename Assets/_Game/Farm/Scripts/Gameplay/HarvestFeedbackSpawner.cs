using System.Collections;
using UnityEngine;

public class HarvestFeedbackSpawner : MonoBehaviour
{
    public static HarvestFeedbackSpawner Instance { get; private set; }

    [Header("Fly FX")]
    [SerializeField] private HarvestFlyItemFX harvestFlyPrefab;
    [SerializeField] private Transform warehouseTarget;
    [SerializeField] private WarehousePulseFX warehousePulseFX;

    [Header("EXP FX")]
    [SerializeField] private ExpFlyToAvatarFX expFlyPrefab;
    [SerializeField] private TopBarExpUI topBarExpUI;
    [SerializeField] private RectTransform expTarget;
    [SerializeField] private RectTransform expPulseTarget;

    [Header("Tuning (World)")]
    [SerializeField] private int minVisualIcons = 2;
    [SerializeField] private int maxVisualIcons = 4;
    [SerializeField] private float spawnGap = 0.06f;
    [SerializeField] private float spawnScatterRadius = 70f;

    [Header("Tuning (EXP)")]
    [SerializeField] private int minVisualExpOrbs = 1;
    [SerializeField] private int maxVisualExpOrbs = 3;
    [SerializeField] private float expSpawnGap = 0.05f;
    [SerializeField] private float expSpawnScatterRadius = 55f;
    [SerializeField] private float expPulseDuration = 0.28f;
    [SerializeField] private float expPulseScale = 1.14f;
    [SerializeField] private float expShakePixels = 4f;

    private Coroutine expPulseRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[VFX] HarvestFeedbackSpawner: duplicate Instance — destroying extra copy.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[VFX] HarvestFeedbackSpawner Instance ready");
    }

    private void Start()
    {
        if (topBarExpUI == null)
            topBarExpUI = FindFirstObjectByType<TopBarExpUI>();

        ResolveExpTarget();
    }

    public void SpawnHarvestFly(Sprite icon, Vector3 worldPosition, int amount)
    {
        bool warehouseIsRectTransform = warehouseTarget != null && warehouseTarget is RectTransform;

        Debug.Log($"[HarvestFX] SpawnHarvestFly | amount={amount} | icon={(icon != null ? icon.name : "NULL")} | spawn={worldPosition} | warehouseTarget={(warehouseTarget != null ? warehouseTarget.name : "NULL")} | warehouseTarget.position={(warehouseTarget != null ? warehouseTarget.position.ToString() : "NULL")} | warehouseTargetIsRectTransform={warehouseIsRectTransform}");

        if (warehouseIsRectTransform)
            Debug.LogWarning("[HarvestFX] warehouseTarget is RectTransform (UI). Please assign a WORLD Transform.");

        if (harvestFlyPrefab == null)
        {
            Debug.LogWarning("[HarvestFX] harvestFlyPrefab NULL");
            return;
        }

        if (warehouseTarget == null)
        {
            Debug.LogWarning("[HarvestFX] warehouseTarget NULL");
            return;
        }

        if (icon == null)
        {
            Debug.LogWarning("[HarvestFX] icon NULL");
            return;
        }

        StartCoroutine(CoSpawnFly(icon, worldPosition, amount));
    }

    public void SpawnExpFly(Vector3 worldPosition, int expAmount)
    {
        if (expAmount <= 0)
            return;

        if (expFlyPrefab == null)
            return;

        if (topBarExpUI == null)
            topBarExpUI = FindFirstObjectByType<TopBarExpUI>();

        if (ResolveExpTarget() == null)
            return;

        StartCoroutine(CoSpawnExp(worldPosition, expAmount));
    }

    private IEnumerator CoSpawnFly(Sprite icon, Vector3 worldPosition, int amount)
    {
        int visualCount = Mathf.Clamp(amount, minVisualIcons, maxVisualIcons);
        int arrivedCount = 0;

        Debug.Log($"[HarvestFX] Begin spawn fly icons | visualCount={visualCount} | spawnCenter={worldPosition} | warehouseTarget={(warehouseTarget != null ? warehouseTarget.position.ToString() : "NULL")}");

        for (int i = 0; i < visualCount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * spawnScatterRadius;
            Vector3 spawnPos = worldPosition + new Vector3(scatter.x, scatter.y, 0f);

            HarvestFlyItemFX fx = Instantiate(harvestFlyPrefab, spawnPos, Quaternion.identity);

            if (fx == null)
            {
                Debug.LogError("[HarvestFX] Instantiate fx returned NULL");
                continue;
            }

            // Defensive: clear any prefab default sprite to prevent flashing/incorrect default icon
            fx.ClearIconImmediate();

            Debug.Log($"[HarvestFX] Spawned fly fx #{i + 1}/{visualCount} | pos={spawnPos} | target={(warehouseTarget != null ? warehouseTarget.position.ToString() : "NULL")} | icon={icon.name}");

            if (warehouseTarget == null) continue;
            fx.Play(icon, spawnPos, warehouseTarget.position, () =>
            {
                arrivedCount++;
                Debug.Log($"[HarvestFX] Fly arrived | arrivedCount={arrivedCount}/{visualCount} | target={(warehouseTarget != null ? warehouseTarget.position.ToString() : "NULL")}");

                if (arrivedCount >= visualCount)
                {
                    if (warehousePulseFX == null)
                    {
                        Debug.LogWarning("[HarvestFX] warehousePulseFX NULL (cannot pulse)");
                        return;
                    }

                    warehousePulseFX.PlayPulse();
                }
            });

            if (spawnGap > 0f)
                yield return new WaitForSeconds(spawnGap);
        }
    }

    private IEnumerator CoSpawnExp(Vector3 worldPosition, int expAmount)
    {
        Camera cam = Camera.main;
        if (cam == null)
            yield break;

        Vector3 expWorldTarget = GetExpTargetWorldPosition(worldPosition);

        int visualCount = Mathf.Clamp(expAmount, minVisualExpOrbs, maxVisualExpOrbs);
        int arrivedCount = 0;

        for (int i = 0; i < visualCount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * expSpawnScatterRadius;
            Vector3 spawnPos = worldPosition + new Vector3(scatter.x, scatter.y, 0f);

            ExpFlyToAvatarFX fx = Instantiate(expFlyPrefab, spawnPos, Quaternion.identity);
            if (fx == null)
                continue;

            fx.Play(spawnPos, expWorldTarget, () =>
            {
                arrivedCount++;
                if (arrivedCount >= visualCount)
                {
                    PlayExpTargetPulse();

                    if (PlayerProgressManager.Instance != null)
                        PlayerProgressManager.Instance.AddExp(expAmount);
                }
            });

            if (expSpawnGap > 0f)
                yield return new WaitForSeconds(expSpawnGap);
        }
    }

    private RectTransform ResolveExpTarget()
    {
        if (expTarget != null)
            return expTarget;

        GameObject judgeAvatar = GameObject.Find("JudgeAvatar");
        if (judgeAvatar != null)
        {
            Transform icon = FindDeepChild(judgeAvatar.transform, "icon_exp");
            if (icon != null)
                expTarget = icon as RectTransform;

            if (expPulseTarget == null)
                expPulseTarget = expTarget;
        }

        if (expTarget == null)
        {
            GameObject iconExp = GameObject.Find("icon_exp");
            if (iconExp != null)
                expTarget = iconExp.transform as RectTransform;
        }

        if (expTarget == null && topBarExpUI != null)
            expTarget = topBarExpUI.IconExp;

        if (expPulseTarget == null)
            expPulseTarget = expTarget;

        return expTarget;
    }

    private Vector3 GetExpTargetWorldPosition(Vector3 spawnWorldPosition)
    {
        RectTransform target = ResolveExpTarget();
        if (target == null)
            return spawnWorldPosition;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return target.position;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        float depth = Mathf.Abs(worldCamera.transform.position.z - spawnWorldPosition.z);
        Vector3 worldTarget = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        worldTarget.z = spawnWorldPosition.z;
        return worldTarget;
    }

    private void PlayExpTargetPulse()
    {
        RectTransform target = expPulseTarget != null ? expPulseTarget : ResolveExpTarget();
        if (target == null)
            return;

        if (expPulseRoutine != null)
            StopCoroutine(expPulseRoutine);

        expPulseRoutine = StartCoroutine(CoPulseExpTarget(target));
    }

    private IEnumerator CoPulseExpTarget(RectTransform target)
    {
        Vector3 baseScale = target.localScale;
        Vector2 basePos = target.anchoredPosition;
        float duration = Mathf.Max(0.05f, expPulseDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            float shake = expShakePixels * wave;

            target.localScale = Vector3.LerpUnclamped(baseScale, baseScale * expPulseScale, wave);
            target.anchoredPosition = basePos + new Vector2(
                Mathf.Sin(t * Mathf.PI * 10f) * shake,
                Mathf.Cos(t * Mathf.PI * 12f) * shake * 0.6f);

            yield return null;
        }

        target.localScale = baseScale;
        target.anchoredPosition = basePos;
        expPulseRoutine = null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }
}

