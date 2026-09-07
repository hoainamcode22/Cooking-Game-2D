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

    // C6 — đã xoá `class SeedStockData` + `List<SeedStockData> seedStocks` +
    // `Dictionary seedStockMap` + `RebuildSeedStockMap()` + `GetSeedStock()` +
    // `HasSeed()` + `ConsumeSeed()`, và cả dữ liệu của chúng trong `SCN_Farm`.
    //
    // VÌ SAO: đây là hệ hạt giống THỨ HAI. Kho hạt giống thật là `WarehouseManager`
    // (có save `FARM_WAREHOUSE`, Shop trừ hạt qua nó, `PlotController.TryPlant` đọc nó).
    // `seedStocks` chỉ là một danh sách trong Inspector, KHÔNG được lưu, KHÔNG ai ghi vào,
    // và `ConsumeSeed()` chưa từng được gọi từ bất kỳ đâu. Nhưng nó CÓ dữ liệu trong scene
    // ⇒ ai mở Inspector cũng tưởng đó là kho hạt thật rồi sửa số ở đó, và không hiểu vì
    // sao trong game không đổi gì. Code chết mà trông như đang chạy là loại tệ nhất.

    // ── HAI ROOT Ô ĐẤT: ĐÃ XOÁ (CS-3) ────────────────────────────────────────
    // Trước đây có `[SerializeField] Transform normalPlotsRoot` + `rarePlotsRoot`.
    //
    // VÌ SAO XOÁ — ĐO TRÊN `SCN_Farm` THẬT:
    //   • `rarePlotsRoot: {fileID: 0}` — RỖNG. Danh sách `rarePlots` do đó LUÔN rỗng.
    //   • `normalPlotsRoot` có trỏ vào một Transform, nhưng cây con của nó chỉ chứa
    //     **19** `PlotController` trong khi cả scene có **38**. Nghĩa là gần một nửa số ô
    //     đất đứng NGOÀI tầm nhìn của FarmManager: `UnlockAllPlotsNow()` không đụng tới,
    //     và `GetNextGrowingPlot()` bỏ qua khi dò "ô sắp chín nhất".
    //
    // Gán tay hai root trong Inspector thì chữa được HÔM NAY, nhưng lỗi quay lại ngay lúc
    // ai đó kéo một ô ra ngoài root, hoặc khi người chơi MUA thêm ô đất — `PlacementManager`
    // đẻ ô mới ở gốc scene, ngoài mọi root. Một ref Inspector là điểm hỏng câm: không log,
    // không lỗi, chỉ lặng lẽ bỏ sót.
    //
    // Nay quét thẳng scene rồi phân loại bằng `PlotController.IsRarePlot` — chính cờ mà
    // `PlotController.KeyFor()` dùng để chọn khoá save (`PLOT_RARE_x` / `PLOT_NORMAL_x`),
    // nên hai bên không thể lệch nhau. Một nguồn sự thật duy nhất, không quên kéo được.
    //
    // ⚠️ TRẠNG THÁI HIỆN TẠI, GHI RÕ ĐỂ NGƯỜI SAU KHỎI TƯỞNG NHẦM: cả **38/38** ô trong
    // scene lẫn cả 5 prefab ô/chậu đều đang để `isRarePlot: 0`. Vậy nên bây giờ 38 ô rơi
    // hết vào `normalPlots` và `rarePlots` vẫn rỗng — nhưng KHÔNG còn ô nào bị bỏ sót,
    // đó mới là thứ hai vòng lặp trên cần. Ngày nào chậu hoa được tick `isRarePlot` thì
    // chúng tự động tách nhóm, không phải sửa thêm dòng nào ở đây.

    [Header("Crop Database")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();

    [Header("Flower Crop Database")]
    [SerializeField] private List<CropData> flowerCropDatabase = new List<CropData>();

    [Header("Default Crops")]
    [SerializeField] private CropData defaultNormalCrop;
    [SerializeField] private CropData defaultRareCrop;

    /// <summary>
    /// Hệ số rút ngắn thời gian trồng — CHỈ để test, mặc định 1.0 = ĐÚNG GIÂY THẬT.
    ///
    /// VÌ SAO đặt về 1.0 (quyết định #6): trước đây để 0.3 nên con số trong `CropData
    /// .growSeconds` KHÔNG phải thời gian thật (asset ghi 180 → cây chín sau 54 giây).
    /// Hai người đọc cùng một asset ra hai con số khác nhau, và `feedDurationSeconds`
    /// của chuồng thì KHÔNG nhân hệ số này → ruộng và chuồng đo thời gian bằng hai đơn vị.
    /// Từ bản này `growSeconds` là GIÂY THẬT, cùng đơn vị với `feedDurationSeconds`.
    ///
    /// Ai muốn test nhanh thì hạ số này trong Inspector, KHÔNG sửa asset.
    /// </summary>
    [Header("Fast Time (chỉ để test — 1.0 = giây thật)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float realTimeMultiplier = 1.0f;

    public int CropDatabaseCount => cropMap.Count;

    // Fired every time a plot is actively planted by the player (not on scene load).
    public static event System.Action<PlotController> OnPlotPlantedEvent;

    // Fired every time a plot is successfully harvested (via sickle or direct call).
    public static event System.Action<PlotController> OnPlotHarvestedEvent;

    private readonly Dictionary<string, CropData> cropMap = new Dictionary<string, CropData>();

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
        // F10 — hệ khoá ô đất theo cấp/kim cương đã bị XOÁ (quyết định #5). Mọi ô đất
        // có mặt trong scene đều dùng được ngay; muốn thêm ô thì MUA công trình "Đất".
        // VÌ SAO không giữ hệ cũ: `unlockAllPlotsForLayout: 1` trong scene đã tắt nó
        // hoàn toàn từ lâu — code mở khoá theo cấp/gem chưa từng chạy một lần nào.
        UnlockAllPlotsNow();
    }

    private void OnValidate()
    {
        RebuildCropMap();
    }

    /// <summary>
    /// Nạp lại hai danh sách ô đất bằng cách QUÉT SCENE, phân loại theo
    /// <see cref="PlotController.IsRarePlot"/> (xem lý do ở khối "HAI ROOT Ô ĐẤT" trên đầu file).
    ///
    /// GIỮ NGUYÊN TÊN HÀM dù không còn "root" nào: đây là mục ContextMenu quen tay và có
    /// hai nơi trong chính file này gọi — đổi tên chỉ để cho đẹp là rước rủi ro không cần.
    ///
    /// `FindObjectsInactive.Include` là BẮT BUỘC: ô đất có thể đang tắt lúc scene khởi động
    /// (popup/nhóm bị ẩn), bỏ sót chúng thì đúng lại vào lỗi cũ.
    /// </summary>
    [ContextMenu("Cache Plots From Roots")]
    public void CachePlotsFromRoots()
    {
        normalPlots.Clear();
        rarePlots.Clear();

        PlotController[] tatCaO = FindObjectsByType<PlotController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < tatCaO.Length; i++)
        {
            PlotController o = tatCaO[i];
            if (o == null) continue;

            if (o.IsRarePlot) rarePlots.Add(o);
            else              normalPlots.Add(o);
        }
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

    /// <summary>
    /// Quy MỘT khoảng thời gian bất kỳ (giây thật) về thời gian trong game.
    ///
    /// VÌ SAO là hàm public: chuồng (`PenMiniPanelUI.feedDurationSeconds`) trước đây
    /// KHÔNG nhân hệ số này trong khi cây trồng thì có → hạ multiplier để test thì ruộng
    /// nhanh gấp 3 mà chuồng vẫn nguyên, hai hệ đo bằng hai đơn vị khác nhau. Giờ cả hai
    /// đi qua một cửa duy nhất nên luôn cùng nhịp.
    /// </summary>
    public float GetRealSeconds(float realSeconds)
    {
        return Mathf.Max(1f, realSeconds * realTimeMultiplier);
    }

    /// <summary>Bản static an toàn khi chưa có FarmManager trong scene (trả nguyên số).</summary>
    public static float ScaleSeconds(float realSeconds)
    {
        return _instance != null ? _instance.GetRealSeconds(realSeconds) : Mathf.Max(1f, realSeconds);
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
                FarmUIManager.Instance?.ShowPlantSelectForFlower(plot);
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
        FarmUIManager.Instance?.ShowHint("Kéo lưỡi liềm qua cây để thu hoạch.");
        // Chỉ hiện khay liềm — harvest bắt đầu khi player nhấn giữ icon liềm trong khay
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

        OnPlotHarvestedEvent?.Invoke(plot);
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
