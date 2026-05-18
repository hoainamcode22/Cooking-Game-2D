using UnityEngine;

/// <summary>
/// Singleton quản lý 3 loại VFX bổ sung cho trồng/thu hoạch.
/// Đặt 1 object có component này trong scene, kéo 3 prefab vào Inspector.
///
/// Không thay thế HarvestFeedbackSpawner hay logic cũ —
/// chỉ chạy song song thêm hiệu ứng visual.
/// </summary>
public class FarmCropVFXSpawner : MonoBehaviour
{
    public static FarmCropVFXSpawner Instance { get; private set; }

    [Header("VFX Prefabs — kéo prefab vào sau khi tạo xong")]
    public SeedRainVFX           seedRainPrefab;
    public SeedCostTextVFX       seedCostTextPrefab;
    public HarvestAmountTextVFX  harvestAmountTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[VFX] FarmCropVFXSpawner: duplicate Instance destroyed — chỉ giữ 1 instance trong scene.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log($"[VFX] FarmCropVFXSpawner Instance ready | rainPrefab={seedRainPrefab != null} | costPrefab={seedCostTextPrefab != null} | harvestPrefab={harvestAmountTextPrefab != null}");
    }

    // ── Gieo hạt ─────────────────────────────────────────────────────────────

    public void PlaySeedPlantVFX(CropData crop, Vector3 plotPos, int seedCost = 1)
    {
        Debug.Log($"[VFX] PlaySeedPlantVFX called | crop={crop?.cropId} | icon={(crop?.icon != null ? crop.icon.name : "NULL")} | pos={plotPos} | seedCost={seedCost} | rainPrefab={seedRainPrefab != null} | costPrefab={seedCostTextPrefab != null}");

        if (crop == null) return;

        if (seedRainPrefab != null)
        {
            SeedRainVFX rain = Instantiate(seedRainPrefab, plotPos, Quaternion.identity);
            rain.Play(crop.icon, plotPos, count: 8);
        }
        else
        {
            Debug.LogWarning("[VFX] PlaySeedPlantVFX: seedRainPrefab là NULL — kéo PF_SeedRain_World vào slot seedRainPrefab của FarmCropVFXSpawner trong Inspector.");
        }

        if (seedCostTextPrefab != null && seedCost > 0)
        {
            Vector3 costPos = plotPos + new Vector3(0f, 0.1f, 0f);
            SeedCostTextVFX cost = Instantiate(seedCostTextPrefab, costPos, Quaternion.identity);
            cost.Play(seedCost, costPos, count: 5);
        }
        else if (seedCostTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlaySeedPlantVFX: seedCostTextPrefab là NULL — kéo PF_SeedCostText_World vào slot seedCostTextPrefab của FarmCropVFXSpawner trong Inspector.");
        }
    }

    // ── Thu hoạch ─────────────────────────────────────────────────────────────

    public void PlayHarvestAmountVFX(int amount, Vector3 plotPos)
    {
        Debug.Log($"[VFX] PlayHarvestAmountTextVFX called | amount={amount} | pos={plotPos} | prefab={harvestAmountTextPrefab != null}");

        if (harvestAmountTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlayHarvestAmountVFX: harvestAmountTextPrefab là NULL — kéo PF_HarvestAmountText_World vào slot harvestAmountTextPrefab của FarmCropVFXSpawner trong Inspector.");
            return;
        }

        if (amount <= 0) return;

        Vector3 spawnPos = plotPos + new Vector3(0f, 0.45f, 0f);
        HarvestAmountTextVFX fx = Instantiate(harvestAmountTextPrefab, spawnPos, Quaternion.identity);
        fx.Play(amount, spawnPos, count: 5);
    }
}
