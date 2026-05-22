using System.Collections.Generic;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByTypeSafe();

                if (_instance == null)
                {
                    GameObject go = new GameObject("FarmManager(Auto)");
                    _instance = go.AddComponent<FarmManager>();
                }
            }

            return _instance;
        }
        private set => _instance = value;
    }

    private static FarmManager _instance;

    [System.Serializable]
    public class SeedStockData
    {
        public string cropId;
        public int amount = 10;
    }

    [Header("Roots")]
    [SerializeField] private Transform normalPlotsRoot;
    [SerializeField] private Transform rarePlotsRoot;

    [Header("Crop Database")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();

    [Header("Flower Crop Database")]
    [SerializeField] private List<CropData> flowerCropDatabase = new List<CropData>();

    [Header("Default Crops")]
    [SerializeField] private CropData defaultNormalCrop;
    [SerializeField] private CropData defaultRareCrop;

    [Header("Seed Stocks")]
    [SerializeField] private List<SeedStockData> seedStocks = new List<SeedStockData>();

    [Header("Fast Time")]
    [Range(0.1f, 1f)]
    [SerializeField] private float realTimeMultiplier = 0.3f;

    [Header("Plot Debug / Layout")]
    [SerializeField] private bool unlockAllPlotsForLayout = true;
    [SerializeField] private int startUnlockedNormalCount = 20;

    public int CropDatabaseCount => cropMap.Count;

    // Fired every time a plot is actively planted by the player (not on scene load).
    // FarmerBehavior subscribes to this to know when to start a new job.
    public static event System.Action<PlotController> OnPlotPlantedEvent;

    private readonly Dictionary<string, CropData> cropMap = new Dictionary<string, CropData>();
    private readonly Dictionary<string, int> seedStockMap = new Dictionary<string, int>();

    private readonly List<PlotController> normalPlots = new List<PlotController>();
    private readonly List<PlotController> rarePlots = new List<PlotController>();

    private PlotController selectedPlot;

    private int lastHandledClickFrame = -1;
    private PlotController lastHandledClickPlot = null;

    public PlotController SelectedPlot => selectedPlot;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        RebuildCropMap();
        RebuildSeedStockMap();
        CachePlotsFromRoots();
    }

    private static FarmManager FindFirstObjectByTypeSafe()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<FarmManager>();
#else
        return Object.FindObjectOfType<FarmManager>();
#endif
    }

    private void Start()
    {
        ApplyStartupUnlockState();
    }

    private void OnValidate()
    {
        RebuildCropMap();
        RebuildSeedStockMap();
    }

    [ContextMenu("Cache Plots From Roots")]
    public void CachePlotsFromRoots()
    {
        normalPlots.Clear();
        rarePlots.Clear();

        if (normalPlotsRoot != null)
            normalPlots.AddRange(normalPlotsRoot.GetComponentsInChildren<PlotController>(true));

        if (rarePlotsRoot != null)
            rarePlots.AddRange(rarePlotsRoot.GetComponentsInChildren<PlotController>(true));
    }

    [ContextMenu("Unlock All Plots Now")]
    public void UnlockAllPlotsNow()
    {
        CachePlotsFromRoots();

        for (int i = 0; i < normalPlots.Count; i++)
        {
            if (normalPlots[i] != null && !normalPlots[i].HasSavedState())
                normalPlots[i].SetUnlocked(true);
        }

        for (int i = 0; i < rarePlots.Count; i++)
        {
            if (rarePlots[i] != null && !rarePlots[i].HasSavedState())
                rarePlots[i].SetUnlocked(true);
        }
    }

    [ContextMenu("Apply Startup Unlock State")]
    public void ApplyStartupUnlockState()
    {
        CachePlotsFromRoots();

        if (unlockAllPlotsForLayout)
        {
            UnlockAllPlotsNow();
            return;
        }

        for (int i = 0; i < normalPlots.Count; i++)
        {
            PlotController plot = normalPlots[i];
            if (plot == null)
                continue;

            if (plot.HasSavedState())
                continue;

            bool unlocked = plot.PlotId <= startUnlockedNormalCount;
            plot.SetUnlocked(unlocked);
        }
    }

    private void RebuildCropMap()
    {
        cropMap.Clear();

        // Normal crops
        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null || string.IsNullOrEmpty(crop.cropId)) continue;
            cropMap[crop.cropId] = crop;
        }

        // Flower crops — cần có trong map để TryResolvePlantedCrop tìm được sau khi load save
        for (int i = 0; i < flowerCropDatabase.Count; i++)
        {
            CropData crop = flowerCropDatabase[i];
            if (crop == null || string.IsNullOrEmpty(crop.cropId)) continue;
            cropMap[crop.cropId] = crop;
        }

        Debug.Log($"[FarmManager] RebuildCropMap: {cropMap.Count} crops (normal={cropDatabase.Count}, flower={flowerCropDatabase.Count})");
    }

    private void RebuildSeedStockMap()
    {
        seedStockMap.Clear();

        for (int i = 0; i < seedStocks.Count; i++)
        {
            SeedStockData data = seedStocks[i];
            if (data == null || string.IsNullOrEmpty(data.cropId))
                continue;

            seedStockMap[data.cropId] = Mathf.Max(0, data.amount);
        }
    }

    public CropData GetCropById(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return null;

        return cropMap.TryGetValue(cropId, out CropData crop) ? crop : null;
    }

    public int GetRealGrowSeconds(CropData crop)
    {
        if (crop == null)
            return 60;

        return Mathf.Max(5, Mathf.RoundToInt(crop.growSeconds * realTimeMultiplier));
    }

    public int GetSeedStock(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return 0;

        return seedStockMap.TryGetValue(cropId, out int amount) ? amount : 0;
    }

    public bool HasSeed(string cropId, int amount = 1)
    {
        return GetSeedStock(cropId) >= amount;
    }

    public bool ConsumeSeed(string cropId, int amount = 1)
    {
        if (string.IsNullOrEmpty(cropId))
            return false;

        if (!seedStockMap.TryGetValue(cropId, out int current))
            return false;

        if (current < amount)
            return false;

        current -= amount;
        seedStockMap[cropId] = current;

        for (int i = 0; i < seedStocks.Count; i++)
        {
            if (seedStocks[i] == null)
                continue;

            if (seedStocks[i].cropId == cropId)
            {
                seedStocks[i].amount = current;
                break;
            }
        }

        return true;
    }

    public void SetSelectedPlot(PlotController plot)
    {
        selectedPlot = plot;
    }

    public PlotController GetSelectedPlot()
    {
        return selectedPlot;
    }

    public void OnPlotClicked(PlotController plot)
    {
        if (plot == null)
            return;

        if (Time.frameCount == lastHandledClickFrame && lastHandledClickPlot == plot)
            return;

        lastHandledClickFrame = Time.frameCount;
        lastHandledClickPlot = plot;

        selectedPlot = plot;

        if (!plot.IsUnlocked)
        {
            OnLockedPlotClicked(plot);
            return;
        }

        if (plot.IsReadyToHarvest())
        {
            OnReadyPlotClicked(plot);
            return;
        }

        if (plot.IsPlanted)
        {
            OnGrowingPlotClicked(plot);
            return;
        }

        if (plot.CanOpenSeedPopup())
        {
            if (plot.Category == PlotCategory.Flower)
                FarmUIManager.Instance?.ShowFlowerSelectForPlot(plot);
            else
                FarmUIManager.Instance?.ShowPlantSelectForPlot(plot);
        }
    }

    public void OnLockedPlotClicked(PlotController plot)
    {
        selectedPlot = plot;

        if (plot == null)
            return;

        FarmUIManager.Instance?.ShowHint($"Ô đất {plot.PlotId} chưa mở khóa.");
    }

    public void OnGrowingPlotClicked(PlotController plot)
    {
        selectedPlot = plot;

        if (plot == null)
            return;

        // Popup được xử lý trực tiếp bởi PlotController (mỗi ô đất tự quản lý popup con của nó)
    }

    public void OnReadyPlotClicked(PlotController plot)
    {
        if (plot == null)
            return;

        selectedPlot = plot;
        FarmUIManager.Instance?.ShowHint("Nhấn và kéo lưỡi liềm qua cây để thu hoạch.");
        FarmUIManager.Instance?.ShowSickleTray();
    }

    public void OnPlotPlanted(PlotController plot, CropData crop)
    {
        selectedPlot = plot;

        if (crop != null && plot != null)
            FarmUIManager.Instance?.ShowHint($"Đã trồng {crop.displayName} ở ô {plot.PlotId}");

        OnPlotPlantedEvent?.Invoke(plot);
    }

    public void OnPlotHarvested(PlotController plot, string cropName = "")
    {
        selectedPlot = plot;

        string finalName = string.IsNullOrEmpty(cropName) ? "Nông sản" : cropName;
        FarmUIManager.Instance?.ShowHint($"Đã thu hoạch {finalName} ở ô {plot.PlotId}");
        FarmUIManager.Instance?.HideAllPopups();
    }

    public bool TryPlantSelectedCropById(string cropId)
    {
        if (selectedPlot == null)
        {
            FarmUIManager.Instance?.ShowHint("Chưa chọn ô đất.");
            return false;
        }

        CropData crop = GetCropById(cropId);
        if (crop == null)
        {
            FarmUIManager.Instance?.ShowHint("Không tìm thấy hạt giống.");
            return false;
        }

        return TryPlantToSpecificPlot(selectedPlot, crop);
    }

    public bool TryPlantCropByIdOnPlot(PlotController plot, string cropId)
    {
        if (plot == null)
        {
            FarmUIManager.Instance?.ShowHint("Không tìm thấy ô đất.");
            return false;
        }

        CropData crop = GetCropById(cropId);
        if (crop == null)
        {
            FarmUIManager.Instance?.ShowHint("Không tìm thấy hạt giống.");
            return false;
        }

        return TryPlantToSpecificPlot(plot, crop);
    }

    public bool TryPlantToSelectedPlot(CropData crop)
    {
        if (selectedPlot == null)
        {
            FarmUIManager.Instance?.ShowHint("Chưa chọn ô đất.");
            return false;
        }

        if (crop == null)
        {
            FarmUIManager.Instance?.ShowHint("Hạt giống rỗng.");
            return false;
        }

        return TryPlantToSpecificPlot(selectedPlot, crop);
    }

    public bool TryPlantToSpecificPlot(PlotController plot, CropData crop)
    {
        if (plot == null)
        {
            FarmUIManager.Instance?.ShowHint("Không tìm thấy ô đất.");
            return false;
        }

        if (crop == null)
        {
            FarmUIManager.Instance?.ShowHint("Crop rỗng.");
            return false;
        }

        selectedPlot = plot;

        if (!plot.CanPlantCrop(crop))
        {
            FarmUIManager.Instance?.ShowHint($"Không thể trồng {crop.displayName} ở ô {plot.PlotId}");
            return false;
        }

        bool planted = plot.TryPlant(crop);

        if (planted)
        {
            OnPlotPlanted(plot, crop);
        }
        else
        {
            FarmUIManager.Instance?.ShowHint($"Không thể trồng {crop.displayName} ở ô {plot.PlotId}");
        }

        return planted;
    }

    public bool TryPlantSelectedDefaultCrop()
    {
        if (selectedPlot == null)
            return false;

        CropData cropToPlant = selectedPlot.IsRarePlot ? defaultRareCrop : defaultNormalCrop;
        if (cropToPlant == null)
        {
            FarmUIManager.Instance?.ShowHint("Chưa gán crop mặc định.");
            return false;
        }

        return TryPlantSelectedCropById(cropToPlant.cropId);
    }

    public bool TryHarvestSelected()
    {
        if (selectedPlot == null)
            return false;

        if (!selectedPlot.IsReadyToHarvest())
            return false;

        string cropName = selectedPlot.CurrentCrop != null ? selectedPlot.CurrentCrop.displayName : "Nông sản";

        bool harvested = selectedPlot.Harvest();
        if (harvested)
        {
            OnPlotHarvested(selectedPlot, cropName);
        }
        else
        {
            FarmUIManager.Instance?.ShowHint("Thu hoạch thất bại.");
        }

        return harvested;
    }

    public bool TryUnlockSelectedPlotByGem()
    {
        if (selectedPlot == null)
            return false;

        if (selectedPlot.IsUnlocked)
            return false;

        int gemCost = Mathf.Max(0, selectedPlot.GemCost);

        if (FarmEconomyManager.Instance != null && gemCost > 0)
        {
            if (!FarmEconomyManager.Instance.SpendGems(gemCost))
            {
                FarmUIManager.Instance?.ShowHint("Không đủ kim cương.");
                return false;
            }
        }

        selectedPlot.SetUnlocked(true);
        FarmUIManager.Instance?.ShowHint($"Đã mở ô đất {selectedPlot.PlotId}");
        FarmUIManager.Instance?.HideAllPopups();

        return true;
    }

    public void ClearSelectedPlot()
    {
        selectedPlot = null;
    }

    public PlotController GetNextGrowingPlot()
    {
        PlotController bestPlot = null;
        long bestRemain = long.MaxValue;

        for (int i = 0; i < normalPlots.Count; i++)
        {
            PlotController plot = normalPlots[i];
            if (plot == null || !plot.IsUnlocked || !plot.IsPlanted || plot.IsReadyToHarvest())
                continue;

            long remain = plot.GetRemainingSeconds();
            if (remain < bestRemain)
            {
                bestRemain = remain;
                bestPlot = plot;
            }
        }

        for (int i = 0; i < rarePlots.Count; i++)
        {
            PlotController plot = rarePlots[i];
            if (plot == null || !plot.IsUnlocked || !plot.IsPlanted || plot.IsReadyToHarvest())
                continue;

            long remain = plot.GetRemainingSeconds();
            if (remain < bestRemain)
            {
                bestRemain = remain;
                bestPlot = plot;
            }
        }

        return bestPlot;
    }
}