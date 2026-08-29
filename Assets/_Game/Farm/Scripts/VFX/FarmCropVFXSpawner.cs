using UnityEngine;

/// <summary>
/// Singleton quáº£n lÃ½ 3 loáº¡i VFX bá»• sung cho trá»“ng/thu hoáº¡ch.
/// Äáº·t 1 object cÃ³ component nÃ y trong scene, kÃ©o 3 prefab vÃ o Inspector.
///
/// KhÃ´ng thay tháº¿ HarvestFeedbackSpawner hay logic cÅ© â€”
/// chá»‰ cháº¡y song song thÃªm hiá»‡u á»©ng visual.
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
    }

    // â”€â”€ Gieo háº¡t â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void PlaySeedPlantVFX(CropData crop, Vector3 plotPos, int seedCost = 1)
    {

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
            cost.Play(seedCost, costPos, count: 4);
        }
        else if (seedCostTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlaySeedPlantVFX: seedCostTextPrefab là NULL — kéo PF_SeedCostText_World vào slot seedCostTextPrefab của FarmCropVFXSpawner trong Inspector.");
        }
    }

    // â”€â”€ Thu hoáº¡ch â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void PlayItemDropVFX(Sprite itemIcon, Vector3 worldPos, int itemCost = 1)
    {
        if (itemIcon == null) return;

        if (seedRainPrefab != null)
        {
            SeedRainVFX rain = Instantiate(seedRainPrefab, worldPos, Quaternion.identity);
            rain.Play(itemIcon, worldPos, count: 8);
        }
        else
        {
            Debug.LogWarning("[VFX] PlayItemDropVFX: seedRainPrefab is NULL.");
        }

        if (seedCostTextPrefab != null && itemCost > 0)
        {
            Vector3 costPos = worldPos + new Vector3(0f, 0.1f, 0f);
            SeedCostTextVFX cost = Instantiate(seedCostTextPrefab, costPos, Quaternion.identity);
            cost.Play(itemCost, costPos, count: 4);
        }
        else if (seedCostTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlayItemDropVFX: seedCostTextPrefab is NULL.");
        }
    }

    public void PlayHarvestAmountVFX(int amount, Vector3 plotPos)
    {

        if (harvestAmountTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlayHarvestAmountVFX: harvestAmountTextPrefab là NULL — kéo PF_HarvestAmountText_World vào slot harvestAmountTextPrefab của FarmCropVFXSpawner trong Inspector.");
            return;
        }

        if (amount <= 0) return;

        Vector3 spawnPos = plotPos + new Vector3(0f, 0.45f, 0f);
        HarvestAmountTextVFX fx = Instantiate(harvestAmountTextPrefab, spawnPos, Quaternion.identity);
        fx.Play(amount, spawnPos, count: 4);
    }
}
