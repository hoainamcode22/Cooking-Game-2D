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

        if (topBarExpUI == null || topBarExpUI.IconExp == null)
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

        Vector3 uiWorldTarget = topBarExpUI.IconExp.position;

        int visualCount = Mathf.Clamp(expAmount, minVisualExpOrbs, maxVisualExpOrbs);
        int arrivedCount = 0;

        for (int i = 0; i < visualCount; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * expSpawnScatterRadius;
            Vector3 spawnPos = worldPosition + new Vector3(scatter.x, scatter.y, 0f);

            ExpFlyToAvatarFX fx = Instantiate(expFlyPrefab, spawnPos, Quaternion.identity);
            if (fx == null)
                continue;

            fx.Play(spawnPos, uiWorldTarget, () =>
            {
                arrivedCount++;
                if (arrivedCount >= visualCount)
                {
                    if (PlayerProgressManager.Instance != null)
                        PlayerProgressManager.Instance.AddExp(expAmount);
                }
            });

            if (expSpawnGap > 0f)
                yield return new WaitForSeconds(expSpawnGap);
        }
    }
}

