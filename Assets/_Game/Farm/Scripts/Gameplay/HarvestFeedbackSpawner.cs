using System.Collections;
using UnityEngine;

public class HarvestFeedbackSpawner : MonoBehaviour
{
    public static HarvestFeedbackSpawner Instance { get; private set; }

    [Header("Text FX")]
    [SerializeField] private FloatingHarvestText prefab;

    [Header("Fly FX")]
    [SerializeField] private HarvestFlyItemFX harvestFlyPrefab;
    [SerializeField] private Transform warehouseTarget;
    [SerializeField] private WarehousePulseFX warehousePulseFX;

    [Header("Tuning (World)")]
    [SerializeField] private int minVisualIcons = 2;
    [SerializeField] private int maxVisualIcons = 4;
    [SerializeField] private float spawnGap = 0.06f;
    [SerializeField] private float spawnScatterRadius = 0.25f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Spawn(Vector3 worldPosition, string content)
    {
        if (prefab == null)
            return;

        FloatingHarvestText item = Instantiate(prefab, worldPosition, Quaternion.identity);
        item.Setup(content);
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

    private IEnumerator CoSpawnFly(Sprite icon, Vector3 worldPosition, int amount)
    {
        int visualCount = Mathf.Clamp(amount, minVisualIcons, maxVisualIcons);
        int arrivedCount = 0;

        Debug.Log($"[HarvestFX] Begin spawn fly icons | visualCount={visualCount} | spawnCenter={worldPosition} | warehouseTarget={warehouseTarget.position}");

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

            Debug.Log($"[HarvestFX] Spawned fly fx #{i + 1}/{visualCount} | pos={spawnPos} | target={warehouseTarget.position} | icon={icon.name}");

            fx.Play(icon, spawnPos, warehouseTarget.position, () =>
            {
                arrivedCount++;
                Debug.Log($"[HarvestFX] Fly arrived | arrivedCount={arrivedCount}/{visualCount} | target={warehouseTarget.position}");

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
}