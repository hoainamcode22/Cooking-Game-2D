using System.Collections.Generic;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }

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

    [Header("Default Crops")]
    [SerializeField] private CropData defaultNormalCrop;
    [SerializeField] private CropData defaultRareCrop;

    [Header("Seed Stocks")]
    [SerializeField] private List<SeedStockData> seedStocks = new List<SeedStockData>();

    [Header("Fast Time")]
    [Range(0.1f, 1f)]
    [SerializeField] private float realTimeMultiplier = 0.3f;

    [Header("Farmer")]
    [SerializeField] private FarmerNPCController farmerNPC;

    private readonly Dictionary<string, CropData> cropMap = new Dictionary<string, CropData>();
    private readonly Dictionary<string, int> seedStockMap = new Dictionary<string, int>();

    private readonly List<PlotController> normalPlots = new List<PlotController>();
    private readonly List<PlotController> rarePlots = new List<PlotController>();

    private PlotController selectedPlot;

    // Chống double click khi cùng 1 frame bị gọi từ nhiều input path.
    private int lastHandledClickFrame = -1;
    private PlotController lastHandledClickPlot = null;

    public PlotController SelectedPlot => selectedPlot;

    // Khởi tạo singleton, cache crop / seed / plot từ scene.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildCropMap();
        RebuildSeedStockMap();
        CachePlotsFromRoots();
    }

    // Mở sẵn 4 ô đầu để test và gameplay đầu game.
    private void Start()
    {
        ForceUnlockFirst4NormalPlots();
    }

    // Rebuild dữ liệu khi Inspector thay đổi.
    private void OnValidate()
    {
        RebuildCropMap();
        RebuildSeedStockMap();
    }

    // Cache toàn bộ plot dưới các root.
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

    // Build map cropId -> CropData để lookup nhanh.
    private void RebuildCropMap()
    {
        cropMap.Clear();

        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null || string.IsNullOrEmpty(crop.cropId))
                continue;

            cropMap[crop.cropId] = crop;
        }
    }

    // Build map cropId -> số lượng hạt giống đang có.
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

    // Mở sẵn 4 plot đầu ở khu thường.
    private void ForceUnlockFirst4NormalPlots()
    {
        for (int i = 0; i < normalPlots.Count; i++)
        {
            PlotController plot = normalPlots[i];
            if (plot == null)
                continue;

            if (plot.PlotId <= 4)
                plot.SetUnlocked(true);
        }
    }

    // Lấy CropData theo cropId.
    public CropData GetCropById(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return null;

        return cropMap.TryGetValue(cropId, out CropData crop) ? crop : null;
    }

    // Convert grow time thiết kế sang grow time runtime.
    public int GetRealGrowSeconds(CropData crop)
    {
        if (crop == null)
            return 60;

        return Mathf.Max(5, Mathf.RoundToInt(crop.growSeconds * realTimeMultiplier));
    }

    // Lấy số lượng hạt đang có theo cropId.
    public int GetSeedStock(string cropId)
    {
        if (string.IsNullOrEmpty(cropId))
            return 0;

        return seedStockMap.TryGetValue(cropId, out int amount) ? amount : 0;
    }

    // Kiểm tra có đủ hạt cho một lần trồng hay không.
    public bool HasSeed(string cropId, int amount = 1)
    {
        return GetSeedStock(cropId) >= amount;
    }

    // Trừ hạt giống sau khi plant thành công.
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

    // Set plot đang được target để trồng / thu hoạch.
    public void SetSelectedPlot(PlotController plot)
    {
        selectedPlot = plot;
    }

    // Trả về plot đang được chọn hiện tại.
    public PlotController GetSelectedPlot()
    {
        return selectedPlot;
    }

    // Xử lý khi player click vào 1 plot.
    public void OnPlotClicked(PlotController plot)
    {
        if (plot == null)
            return;

        // Chặn cùng 1 click bị gọi 2 lần trong cùng frame.
        if (Time.frameCount == lastHandledClickFrame && lastHandledClickPlot == plot)
            return;

        lastHandledClickFrame = Time.frameCount;
        lastHandledClickPlot = plot;

        selectedPlot = plot;

        Debug.Log($"[FarmManager] OnPlotClicked -> {plot.name}");

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
            FarmUIManager.Instance?.ShowPlantSelectForPlot(plot);
        }
    }

    // Xử lý click vào plot đang khóa.
    public void OnLockedPlotClicked(PlotController plot)
    {
        selectedPlot = plot;

        if (plot == null)
            return;

        FarmUIManager.Instance?.ShowHint($"Ô đất {plot.PlotId} chưa mở khóa.");
    }

    // Xử lý click vào plot đang grow.
    public void OnGrowingPlotClicked(PlotController plot)
    {
        selectedPlot = plot;

        if (plot == null)
            return;

        if (plot.CurrentCrop != null)
            FarmUIManager.Instance?.ShowHint($"{plot.CurrentCrop.displayName} đang lớn. Còn {plot.GetRemainingTimeText()}");
        else
            FarmUIManager.Instance?.ShowHint("Ô đất đang trồng.");
    }

    // Xử lý click vào plot đã ready.
    public void OnReadyPlotClicked(PlotController plot)
    {
        selectedPlot = plot;

        if (plot == null)
            return;

        string cropName = plot.CurrentCrop != null ? plot.CurrentCrop.displayName : "Nông sản";
        FarmUIManager.Instance?.ShowHint($"{cropName} đã chín ở ô {plot.PlotId}.");
    }

    // Callback sau khi plant thành công để update UI và farmer.
    public void OnPlotPlanted(PlotController plot, CropData crop)
    {
        selectedPlot = plot;

        if (crop != null && plot != null)
            FarmUIManager.Instance?.ShowHint($"Đã trồng {crop.displayName} ở ô {plot.PlotId}");

        // Trồng xong thì ẩn popup hạt giống.
        FarmUIManager.Instance?.HidePlantSelectPopup();

        if (farmerNPC != null)
            farmerNPC.NotifyNewGrowingPlot();
    }

    // Callback sau khi harvest thành công.
    public void OnPlotHarvested(PlotController plot, string cropName = "")
    {
        selectedPlot = plot;

        string finalName = string.IsNullOrEmpty(cropName) ? "Nông sản" : cropName;
        FarmUIManager.Instance?.ShowHint($"Đã thu hoạch {finalName} ở ô {plot.PlotId}");
        FarmUIManager.Instance?.HideAllPopups();
    }

    // Trồng bằng cropId vào plot đang chọn.
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

    // Trồng bằng cropId vào đúng plot chỉ định.
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

    // Trồng crop vào plot đang được chọn hiện tại.
    public bool TryPlantToSelectedPlot(CropData crop)
    {
        Debug.Log("[FarmManager] TryPlantToSelectedPlot");

        if (selectedPlot == null)
        {
            Debug.LogError("[FarmManager] selectedPlot NULL");
            FarmUIManager.Instance?.ShowHint("Chưa chọn ô đất.");
            return false;
        }

        if (crop == null)
        {
            Debug.LogError("[FarmManager] crop NULL");
            FarmUIManager.Instance?.ShowHint("Hạt giống rỗng.");
            return false;
        }

        Debug.Log($"[FarmManager] selectedPlot = {selectedPlot.name}, crop = {crop.displayName}");

        return TryPlantToSpecificPlot(selectedPlot, crop);
    }

    // Trồng trực tiếp crop vào đúng plot chỉ định.
    public bool TryPlantToSpecificPlot(PlotController plot, CropData crop)
    {
        if (plot == null)
        {
            Debug.LogError("[FarmManager] plot NULL");
            FarmUIManager.Instance?.ShowHint("Không tìm thấy ô đất.");
            return false;
        }

        if (crop == null)
        {
            Debug.LogError("[FarmManager] crop NULL");
            FarmUIManager.Instance?.ShowHint("Crop rỗng.");
            return false;
        }

        selectedPlot = plot;

        Debug.Log($"[FarmManager] TryPlantToSpecificPlot -> plot={plot.name}, crop={crop.displayName}, cropId={crop.cropId}, stateEmpty={plot.IsEmpty}");

        bool canPlant = plot.CanPlantCrop(crop);
        Debug.Log($"[FarmManager] CanPlantCrop = {canPlant}");

        if (!canPlant)
        {
            FarmUIManager.Instance?.ShowHint($"Không thể trồng {crop.displayName} ở ô {plot.PlotId}");
            return false;
        }

        // TẠM THỜI BỎ CHECK HẠT GIỐNG ĐỂ TEST TRỒNG
        bool planted = plot.TryPlant(crop);
        Debug.Log($"[FarmManager] plot.TryPlant = {planted}");

        if (planted)
        {
            Debug.Log("[FarmManager] TEST MODE: skip ConsumeSeed");
            OnPlotPlanted(plot, crop);
        }
        else
        {
            FarmUIManager.Instance?.ShowHint($"Không thể trồng {crop.displayName} ở ô {plot.PlotId}");
        }

        return planted;
    }

    // Trồng crop mặc định tùy loại plot.
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

    // Thu hoạch plot đang chọn nếu đã ready.
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

    // Mở khóa plot đang chọn bằng gem.
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

    // Xóa selectedPlot khi cần reset flow UI.
    public void ClearSelectedPlot()
    {
        selectedPlot = null;
    }

    // Tìm plot đang grow có thời gian còn lại ít nhất để farmer ưu tiên xử lý.
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