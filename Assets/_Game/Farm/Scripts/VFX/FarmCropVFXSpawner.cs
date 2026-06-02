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

    [Header("VFX Prefabs â€” kÃ©o prefab vÃ o sau khi táº¡o xong")]
    public SeedRainVFX           seedRainPrefab;
    public SeedCostTextVFX       seedCostTextPrefab;
    public HarvestAmountTextVFX  harvestAmountTextPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[VFX] FarmCropVFXSpawner: duplicate Instance destroyed â€” chá»‰ giá»¯ 1 instance trong scene.");
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
            Debug.LogWarning("[VFX] PlaySeedPlantVFX: seedRainPrefab lÃ  NULL â€” kÃ©o PF_SeedRain_World vÃ o slot seedRainPrefab cá»§a FarmCropVFXSpawner trong Inspector.");
        }

        if (seedCostTextPrefab != null && seedCost > 0)
        {
            Vector3 costPos = plotPos + new Vector3(0f, 0.1f, 0f);
            SeedCostTextVFX cost = Instantiate(seedCostTextPrefab, costPos, Quaternion.identity);
            cost.Play(seedCost, costPos, count: 5);
        }
        else if (seedCostTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlaySeedPlantVFX: seedCostTextPrefab lÃ  NULL â€” kÃ©o PF_SeedCostText_World vÃ o slot seedCostTextPrefab cá»§a FarmCropVFXSpawner trong Inspector.");
        }
    }

    // â”€â”€ Thu hoáº¡ch â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void PlayHarvestAmountVFX(int amount, Vector3 plotPos)
    {

        if (harvestAmountTextPrefab == null)
        {
            Debug.LogWarning("[VFX] PlayHarvestAmountVFX: harvestAmountTextPrefab lÃ  NULL â€” kÃ©o PF_HarvestAmountText_World vÃ o slot harvestAmountTextPrefab cá»§a FarmCropVFXSpawner trong Inspector.");
            return;
        }

        if (amount <= 0) return;

        Vector3 spawnPos = plotPos + new Vector3(0f, 0.45f, 0f);
        HarvestAmountTextVFX fx = Instantiate(harvestAmountTextPrefab, spawnPos, Quaternion.identity);
        fx.Play(amount, spawnPos, count: 5);
    }
}
