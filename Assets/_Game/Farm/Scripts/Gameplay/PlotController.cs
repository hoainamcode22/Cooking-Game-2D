using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public enum PlotCategory { Normal, Flower }

public class PlotController : MonoBehaviour, IPointerClickHandler
{
    private enum PlotState
    {
        Locked = 0,
        Empty = 1,
        Growing = 2,
        Ready = 3
    }

    [Serializable]
    private class PlotSaveData
    {
        /// <summary>
        /// Phiên bản save của MỘT ô đất.
        ///
        /// VÌ SAO cần: save cũ (bản chưa cấp lại plotId) không có khoá này nên
        /// JsonUtility để 0. Nhờ đó phân biệt được "save v0 — có thể đang dùng chung
        /// khoá với ô khác" và "save v1 — khoá đã là duy nhất", để chỉ chuyển đổi MỘT LẦN.
        /// </summary>
        public int saveVersion;

        public bool isUnlocked;
        public string plantedCropId;
        public long startUnixTime;
        public long finishUnixTime;
        public int state;
    }

    /// <summary>v0 = trước khi cấp lại plotId (F1) · v1 = plotId đã duy nhất.</summary>
    private const int CurrentSaveVersion = 1;

    /// <summary>
    /// BẢNG CHUYỂN KHOÁ SAVE — plotId MỚI → plotId CŨ (F1).
    ///
    /// VÌ SAO PHẢI CÓ: trước bản này 8 ô đất trong `SCN_Farm` dùng TRÙNG plotId với 8 ô
    /// khác (thường 1..6, chậu hoa 26, 27). Vì `SaveKey = PLOT_NORMAL_{plotId}` không
    /// chứa category, mỗi cặp ghi/đọc CÙNG một khoá PlayerPrefs: trồng ô này, thoát vào
    /// lại thì ô kia hiện cây. Đó là lỗi mất dữ liệu, nên 8 ô bị đổi sang 101..108.
    ///
    /// Đổi plotId = ĐỔI KHOÁ LƯU. Nếu không chuyển đổi thì người chơi đang có save cũ
    /// mở game lên thấy 8 ô trắng trơn — mất sạch cây đang trồng. Nên lần nạp đầu tiên
    /// sau khi cập nhật, ô mang id mới sẽ COPY trạng thái từ khoá cũ của nó.
    ///
    /// Cặp trùng cũ đọc chung một khoá nên sau khi chuyển, cả hai ô cùng hiện một cây —
    /// ĐÚNG với những gì người chơi đang thấy trên màn hình trước khi cập nhật, và từ lần
    /// trồng sau hai ô tách hẳn ra. Không thể làm tốt hơn: dữ liệu cũ vốn không phân biệt
    /// được cây đó thuộc ô nào.
    ///
    /// VÌ SAO chọn dải 101..108 thay vì 31..38: `PlacementManager.GetNextPlotId()` trả
    /// max(plotId trong scene) + 1, mà max cũ = 30 → người chơi đang có save đã được cấp
    /// 31, 32, 33... cho các ô đất họ MUA. Đặt id mới vào 31..38 là đè lên chính save đó.
    /// </summary>
    private static readonly System.Collections.Generic.Dictionary<int, int> LegacyPlotIdMap =
        new System.Collections.Generic.Dictionary<int, int>
        {
            { 101, 2  },   // Plot_01 (1)
            { 102, 3  },   // Plot_01 (2)
            { 103, 4  },   // Plot_01 (3)
            { 104, 5  },   // Plot_01 (4)
            { 105, 6  },   // Plot_01 (5)
            { 106, 1  },   // Plot_01     (dùng plotId mặc định của prefab)
            { 107, 26 },   // Chauhoa_1 (4)
            { 108, 27 },   // Chauhoa_1 (5)
        };

    [Header("Category")]
    [SerializeField] private PlotCategory plotCategory = PlotCategory.Normal;

    [Header("Identity")]
    [SerializeField] private int plotId = 1;
    [SerializeField] private bool isRarePlot = false;

    [Header("Refs")]
    [SerializeField] private CropProcessPopupUI processPopup;
    [SerializeField] private SpriteRenderer groundSprite;
    [SerializeField] private Transform cropGroup;
    [SerializeField] private PlotCropVisual cropVisual;
    [SerializeField] private SpriteRenderer readyIcon;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;

    [Header("Progress UI")]
    [SerializeField] private Transform progressFill;
    [SerializeField] private GameObject progressRoot;
    [SerializeField] private float progressFullWidth = 1f;
    [SerializeField] private bool progressLeftToRight = true;

    [Header("FX")]
    [SerializeField] private Transform harvestSpawnPoint;
    [SerializeField] private Transform expSpawnPoint;

    [Header("Plot VFX")]
    [SerializeField] private Transform cropVFXRoot;
    [SerializeField] private SeedRainVFX seedRainPrefab;
    [SerializeField] private SeedCostTextVFX seedCostTextPrefab;
    [SerializeField] private HarvestAmountTextVFX harvestAmountTextPrefab;

    private PlotState state;
    private CropData plantedCrop;
    private string plantedCropId = "";
    private long startUnixTime;
    private long finishUnixTime;

    // Đặt true trước Start() để Start() bỏ qua Load() và khởi tạo sạch
    private bool _skipLoad;

    // Guard: ngăn HandlePlotClick bị gọi 2 lần trong cùng 1 frame
    // (do cả IPointerClickHandler lẫn FarmPlotInput cùng fire)
    private int lastHandledFrame = -1;

    public int PlotId => plotId;
    public PlotCategory Category => plotCategory;

    /// <summary>
    /// Gán plotId mới và đổi tên GameObject. Gọi từ PlacementManager sau Instantiate.
    ///
    /// Tên theo ĐÚNG loại ô: chậu hoa mua ở Shop trước đây bị đặt tên "Plot_39" trong khi
    /// nó là ô hoa. `TutorialStepTriggerBridge.AllRiceFieldPlanted()` lọc chậu hoa bằng
    /// TÊN ("chau"/"pot"/"hoa") song song với lọc theo Category, nên tên sai làm lớp lọc
    /// thứ hai mất tác dụng — và người đọc Hierarchy cũng không phân biệt được ô nào là ô nào.
    /// </summary>
    public void SetPlotId(int newId)
    {
        plotId = newId;
        gameObject.name = plotCategory == PlotCategory.Flower
            ? $"Chauhoa_{plotId:00}"
            : $"Plot_{plotId:00}";
    }
    public bool IsRarePlot => isRarePlot;
    public bool IsUnlocked => state != PlotState.Locked;
    public bool IsPlanted => state == PlotState.Growing || state == PlotState.Ready;
    public bool IsEmpty => state == PlotState.Empty;
    public bool IsGrowing => state == PlotState.Growing;
    public bool IsReady => state == PlotState.Ready;
    public CropData CurrentCrop => plantedCrop;

    private string SaveKey => KeyFor(plotId);

    private string KeyFor(int id) => isRarePlot ? $"PLOT_RARE_{id}" : $"PLOT_NORMAL_{id}";

    /// <summary>Khoá save CŨ của ô này (trước F1). Trả về chuỗi rỗng nếu ô không bị đổi id.</summary>
    private string LegacySaveKey =>
        LegacyPlotIdMap.TryGetValue(plotId, out int oldId) ? KeyFor(oldId) : string.Empty;

    private void Reset()
    {
        ForceRebindChildren();
    }

    private void OnValidate()
    {
        ForceRebindChildren();
    }

    private void Awake()
    {
        ForceRebindChildren();
        EnsureCropVFXRoot();

        if (cropVisual != null)
            cropVisual.ClearAll();
    }

    private void Start()
    {
        if (processPopup == null)
            processPopup = FindFirstObjectByType<CropProcessPopupUI>(FindObjectsInactive.Include);

        if (_skipLoad)
        {
            // Ô đất mới mua — không được nạp dữ liệu cũ
            state         = PlotState.Empty;
            plantedCrop   = null;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
        }
        else
        {
            Load();
        }

        TryResolvePlantedCrop();
        RefreshVisual();
    }

    /// <summary>
    /// Gọi từ PlacementManager ngay sau Instantiate để tránh Load() nạp dữ liệu cũ
    /// của ô đất trùng plotId.
    /// </summary>
    public void InitializeAsNew()
    {
        _skipLoad = true;
        PlayerPrefs.DeleteKey(SaveKey);   // Xóa luôn để không còn "vết tích" cũ
    }

    private float wiggleTimer = 0f;

    private void Update()
    {
        if (state == PlotState.Ready)
        {
            wiggleTimer -= Time.deltaTime;
            if (wiggleTimer <= 0f)
            {
                wiggleTimer = UnityEngine.Random.Range(3f, 6f);
                if (cropVisual != null) cropVisual.PlayWiggleAnimation();
            }
        }

        if (state != PlotState.Growing)
            return;

        TryResolvePlantedCrop();

        if (IsTimeUp())
        {
            state = PlotState.Ready;
            Save();
        }

        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandlePlotClick();
    }

    public void HandlePlotClick()
    {
        // Chống double-fire trong cùng 1 frame (IPointerClickHandler + FarmPlotInput)
        if (Time.frameCount == lastHandledFrame)
            return;
        lastHandledFrame = Time.frameCount;

        // Không xử lý click khi đang kéo hạt giống — tránh liềm hiện nhầm
        if (FarmInputLock.IsDraggingSeed)
            return;

        if (state == PlotState.Growing || state == PlotState.Ready)
        {
            if (cropVisual != null) cropVisual.PlayWiggleAnimation();
        }

        if (FarmManager.Instance == null)
        {
            return;
        }

        FarmManager.Instance.SetSelectedPlot(this);

        if (state == PlotState.Locked)
        {
            FarmManager.Instance.OnLockedPlotClicked(this);
            return;
        }

        if (state == PlotState.Ready)
        {
            TryResolvePlantedCrop();
            // Nếu không có crop nào (save bị lỗi) → reset về Empty thay vì hiện liềm
            if (plantedCrop == null)
            {
                state = PlotState.Empty;
                plantedCropId = "";
                startUnixTime = 0;
                finishUnixTime = 0;
                Save();
                RefreshVisual();
                FarmManager.Instance.OnPlotClicked(this);
                return;
            }
            FarmManager.Instance.OnReadyPlotClicked(this);
            return;
        }

        if (state == PlotState.Growing)
        {
            if (processPopup != null)
                processPopup.OpenForPlot(this);
            return;
        }

        if (state == PlotState.Empty)
        {
            FarmManager.Instance.OnPlotClicked(this);
        }
    }

    public bool HasSavedState()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }

    [ContextMenu("Force Rebind Children")]
    public void ForceRebindChildren()
    {
        Transform t;

        groundSprite = null;
        cropGroup = null;
        cropVisual = null;
        readyIcon = null;
        timerRoot = null;
        timerText = null;
        progressRoot = null;
        progressFill = null;

        t = transform.Find("GroundSprite");
        if (t != null) groundSprite = t.GetComponent<SpriteRenderer>();

        t = transform.Find("CropGroup");
        if (t != null) cropGroup = t;

        if (cropGroup != null)
            cropVisual = cropGroup.GetComponent<PlotCropVisual>();

        t = transform.Find("ReadyIcon");
        if (t != null) readyIcon = t.GetComponent<SpriteRenderer>();

        t = transform.Find("TimerRoot");
        if (t != null) timerRoot = t.gameObject;

        t = transform.Find("TimerRoot/TimerText");
        if (t != null) timerText = t.GetComponent<TMP_Text>();

        t = transform.Find("ProgressRoot");
        if (t != null) progressRoot = t.gameObject;

        t = transform.Find("ProgressRoot/Fill");
        if (t != null) progressFill = t;

        if (processPopup == null)
            processPopup = GetComponentInChildren<CropProcessPopupUI>(true);

        AutoFindHarvestSpawnPoint();
        AutoFindExpSpawnPoint();

        Transform vfxRoot = transform.Find("CropVFXRoot");
        if (vfxRoot != null) cropVFXRoot = vfxRoot;
    }

    private Transform AutoFindHarvestSpawnPoint()
    {
        // Always prefer the local child named "HarvestSpawnPoint" on THIS plot.
        // This prevents a wrong serialized reference (e.g. from another plot) from causing bad spawn positions.
        Transform local = transform.Find("HarvestSpawnPoint");
        if (local != null)
        {
            harvestSpawnPoint = local;
            return harvestSpawnPoint;
        }

        // Fallback: keep any manually assigned reference.
        return harvestSpawnPoint;
    }

    private Transform AutoFindExpSpawnPoint()
    {
        Transform local = transform.Find("ExpSpawnPoint");
        if (local != null)
        {
            expSpawnPoint = local;
            return expSpawnPoint;
        }

        return expSpawnPoint;
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null) return "NULL";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    public Vector3 GetHarvestSpawnPosition()
    {
        AutoFindHarvestSpawnPoint();
        return harvestSpawnPoint != null ? harvestSpawnPoint.position : transform.position + Vector3.up * 0.6f;
    }

    public Vector3 GetExpSpawnPosition()
    {
        AutoFindExpSpawnPoint();
        return expSpawnPoint != null ? expSpawnPoint.position : GetHarvestSpawnPosition();
    }

    // ── Plot VFX ──────────────────────────────────────────────────────────────

    private void EnsureCropVFXRoot()
    {
        if (cropVFXRoot != null) return;

        Transform existing = transform.Find("CropVFXRoot");
        if (existing != null)
        {
            cropVFXRoot = existing;
            return;
        }

        GameObject go = new GameObject("CropVFXRoot");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        cropVFXRoot = go.transform;
    }

    private void PlaySeedPlantVFX(CropData crop, int seedCost)
    {
        EnsureCropVFXRoot();
        Vector3 pos = cropVFXRoot.position;
        SeedRainVFX rainPrefab = seedRainPrefab != null
            ? seedRainPrefab
            : FarmCropVFXSpawner.Instance?.seedRainPrefab;
        SeedCostTextVFX costTextPrefab = seedCostTextPrefab != null
            ? seedCostTextPrefab
            : FarmCropVFXSpawner.Instance?.seedCostTextPrefab;

        if (rainPrefab != null)
        {
            var rain = Instantiate(rainPrefab, pos, Quaternion.identity, cropVFXRoot);
            rain.Play(crop != null ? crop.icon : null, pos, 8);
        }

        if (costTextPrefab != null)
        {
            Vector3 textPos = pos + new Vector3(0.15f, 0.35f, 0f);
            var cost = Instantiate(costTextPrefab, textPos, Quaternion.identity, cropVFXRoot);
            cost.Play(seedCost, textPos, 4);
        }
    }

    private void PlayHarvestAmountTextVFX(int amount)
    {
        EnsureCropVFXRoot();
        Vector3 pos = cropVFXRoot.position + new Vector3(0f, 0.45f, 0f);

        HarvestAmountTextVFX amountTextPrefab = harvestAmountTextPrefab != null
            ? harvestAmountTextPrefab
            : FarmCropVFXSpawner.Instance?.harvestAmountTextPrefab;

        if (amountTextPrefab != null)
        {
            var text = Instantiate(amountTextPrefab, pos, Quaternion.identity, cropVFXRoot);
            text.Play(amount, pos, 4);
        }
    }

    [ContextMenu("TEST Play Seed VFX")]
    private void TestPlaySeedVFX()
    {
        EnsureCropVFXRoot();
        if (seedRainPrefab != null)
        {
            var rain = Instantiate(seedRainPrefab, cropVFXRoot.position, Quaternion.identity, cropVFXRoot);
            rain.Play(plantedCrop != null ? plantedCrop.icon : null, cropVFXRoot.position, 8);
        }
        else
        {
        }

        if (seedCostTextPrefab != null)
        {
            var cost = Instantiate(seedCostTextPrefab, cropVFXRoot.position, Quaternion.identity, cropVFXRoot);
            cost.Play(1, cropVFXRoot.position, 4);
        }
        else
        {
        }
    }

    [ContextMenu("Clear This Plot Save")]
    public void ClearThisPlotSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs

        state = PlotState.Empty;   // F10: không còn hệ khoá ô đất — ô mới luôn trống, sẵn sàng trồng
        plantedCrop = null;
        plantedCropId = "";
        startUnixTime = 0;
        finishUnixTime = 0;

        if (cropVisual != null)
            cropVisual.ClearAll();

        RefreshVisual();
    }

    public void SetUnlocked(bool value)
    {
        state = value ? PlotState.Empty : PlotState.Locked;
        plantedCrop = null;

        plantedCropId = "";
        startUnixTime = 0;
        finishUnixTime = 0;

        Save();
        RefreshVisual();
    }

    public bool CanOpenSeedPopup()
    {
        return state == PlotState.Empty;
    }

    public bool CanPlantCrop(CropData crop)
    {
        if (crop == null)
        {
            return false;
        }

        if (state != PlotState.Empty)
        {
            return false;
        }

        // Chặn trồng sai loại: plot hoa chỉ nhận Flower, plot thường chỉ nhận Normal.
        if ((int)crop.cropCategory != (int)plotCategory)
        {
            return false;
        }

        return true;
    }

    public bool TryPlant(CropData crop)
    {

        if (crop == null)
            return false;

        if (state != PlotState.Empty)
            return false;

        plantedCrop = crop;
        plantedCropId = crop.cropId;

        int realGrowSeconds = crop.growSeconds;
        if (FarmManager.Instance != null)
            realGrowSeconds = FarmManager.Instance.GetRealGrowSeconds(crop);

        realGrowSeconds = Mathf.Max(5, realGrowSeconds);

        startUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        finishUnixTime = startUnixTime + realGrowSeconds;
        state = PlotState.Growing;

        Save();
        RefreshVisual();
        PlaySeedPlantVFX(crop, 1);
        AudioManager.Instance?.PlayPlanting();

        // Tiến độ nhiệm vụ trồng cây (đếm theo ô đất, 1 lần trồng = 1)
        MissionProgressTracker.ReportEvent(MissionEventType.PlantCrop, crop.cropId, 1);

        // C8 — ĐÃ GỠ `QuestManager.Instance?.OnCropPlanted(crop.cropId, 1);`.
        // Hai lý do, mỗi lý do đủ để vỡ biên dịch:
        //   1. `QuestManager` là hệ nhiệm vụ thứ hai, đã xoá sạch (0 instance trong mọi
        //      scene, 0 asset QuestData, `CheckQuestCompletion` chỉ ghi `// TODO: Give rewards`).
        //   2. `OnCropPlanted` CHƯA TỪNG tồn tại trên `QuestManager` — class đó chỉ có
        //      `OnItemHarvested` / `OnItemCooked` / `OnOrderDelivered`.
        // `MissionProgressTracker.ReportEvent` ngay trên là hệ CÒN SỐNG và đã báo đủ.
        return true;
    }

    public bool TryPlantFromUI(CropData crop)
    {
        bool planted = TryPlant(crop);

        if (planted && FarmManager.Instance != null)
            FarmManager.Instance.OnPlotPlanted(this, crop);

        return planted;
    }

    public bool IsReadyToHarvest()
    {
        return state == PlotState.Ready;
    }

    public long GetRemainingSeconds()
    {
        if (state != PlotState.Growing)
            return 0;

        long remain = finishUnixTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, remain);
    }

    public string GetRemainingTimeText()
    {
        long remain = GetRemainingSeconds();
        long minutes = remain / 60;
        long seconds = remain % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    public float GetGrowProgress01()
    {
        if (state == PlotState.Empty || state == PlotState.Locked || plantedCrop == null)
            return 0f;

        if (state == PlotState.Ready)
            return 1f;

        long total = finishUnixTime - startUnixTime;
        if (total <= 0)
            return 1f;

        long passed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - startUnixTime;
        return Mathf.Clamp01((float)passed / total);
    }

    public bool Harvest()
    {
        if (state != PlotState.Ready || plantedCrop == null)
            return false;

        CropData harvestedCrop = plantedCrop;

        string harvestItemId = string.IsNullOrEmpty(harvestedCrop.harvestItemId)
            ? harvestedCrop.cropId
            : harvestedCrop.harvestItemId;

        int amount = Mathf.Max(1, harvestedCrop.harvestAmount);

        // F8 — kho có sức chứa THẬT. Kiểm TRƯỚC khi xoá cây: nếu cứ thu hoạch rồi AddItem
        // từ chối thì nông sản bốc hơi và ô đất đã trống — người chơi mất công cả một vòng
        // trồng mà không hiểu vì sao. Thà để cây đứng nguyên chờ dọn kho.
        if (FarmInventoryManager.Instance != null &&
            !FarmInventoryManager.Instance.CanAddItem(harvestItemId))
        {
            FarmUIManager.Instance?.ShowHint(
                $"Kho đầy ({FarmInventoryManager.Instance.UsedSlots}/{FarmInventoryManager.Instance.SlotCapacity} slot) — " +
                "bán bớt hoặc nâng cấp kho rồi thu hoạch.");
            return false;
        }

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.AddItem(harvestItemId, amount);

        // Cập nhật tiến độ nhiệm vụ theo loại nông sản vừa thu hoạch
        MissionProgressTracker.ReportEvent(MissionEventType.HarvestItem, harvestItemId, amount);

        AutoFindHarvestSpawnPoint();
        AutoFindExpSpawnPoint();

        bool plotIsRectTransform = transform is RectTransform;
        bool spawnIsRectTransform = harvestSpawnPoint != null && harvestSpawnPoint is RectTransform;

        Vector3 plotWorldPos = transform.position;
        Vector3 spawnPointWorldPos = harvestSpawnPoint != null ? harvestSpawnPoint.position : Vector3.zero;

        Vector3 fxSpawn = GetHarvestSpawnPosition();


        // Ưu tiên harvestIcon (gán riêng trong CropData), fallback về icon rồi readySprite như cũ
        Sprite fxIcon = harvestedCrop.harvestIcon != null
            ? harvestedCrop.harvestIcon
            : (harvestedCrop.icon != null ? harvestedCrop.icon : harvestedCrop.readySprite);


        HarvestFeedbackSpawner.Instance?.SpawnHarvestFly(
            fxIcon,
            fxSpawn,
            amount
        );
        
        HarvestSlashFX.Spawn(fxSpawn);

        PlayHarvestAmountTextVFX(amount);
        AudioManager.Instance?.PlayHarvest();

        int expReward = harvestedCrop != null ? Mathf.Max(0, harvestedCrop.expReward) : 5;
        if (expReward <= 0)
            expReward = 5;

        HarvestFeedbackSpawner.Instance?.SpawnExpFly(GetExpSpawnPosition(), expReward);

        plantedCrop = null;
        plantedCropId = "";
        startUnixTime = 0;
        finishUnixTime = 0;
        state = PlotState.Empty;

        Save();
        RefreshVisual();
        return true;
    }

    /// <summary>Bỏ qua toàn bộ thời gian còn lại — dùng cho nút Speed Up (kim cương).</summary>
    public void CompleteInstantly()
    {
        if (state != PlotState.Growing)
            return;

        finishUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state = PlotState.Ready;
        Save();
        RefreshVisual();
    }

    /// <summary>
    /// F9 — Số kim cương để bỏ qua thời gian còn lại của ô đất này.
    ///
    /// VÌ SAO KHÔNG CÒN CỨNG 1 GEM: cây 50 giây và cây 700 giây trước đây cùng giá 1 gem,
    /// nên cách chơi tối ưu là chỉ trồng cây cấp 10 rồi bấm gem — thời gian trong bảng D1
    /// mất hết ý nghĩa. Dùng lại đúng công thức của `ConstructionManager` để cả game chỉ
    /// có MỘT thang giá rush: ceil(15 + 0.82·√giây_còn_lại).
    ///
    /// Hằng số 15 là "phí bấm nút": rush lúc còn 2 giây vẫn mất 17 gem → không ai chờ
    /// gần chín rồi rush cho rẻ. Dạng √ giữ giá cây 700 giây ở ~37 gem thay vì 700×hệ số.
    /// </summary>
    public int GetSpeedUpGemCost()
    {
        if (state != PlotState.Growing)
            return 0;

        return ConstructionManager.RushCostFor(GetRemainingSeconds());
    }

    /// <summary>Trừ gem theo thời gian còn lại rồi chín ngay — gắn vào nút Gem trên CropProcessPopupUI.</summary>
    public void InstantGrow()
    {
        if (state != PlotState.Growing)
        {
            return;
        }

        if (FarmEconomyManager.Instance == null)
        {
            return;
        }

        int cost = GetSpeedUpGemCost();

        // Kiểm tra ĐỦ TIỀN trước khi trừ: SpendGems trả false là không mất gì,
        // nhưng vẫn phải chặn ở đây để hiện được thông báo cho người chơi.
        if (FarmEconomyManager.Instance.Gems < cost)
        {
            FarmUIManager.Instance?.ShowHint($"Cần {cost} kim cương để tăng tốc.");
            return;
        }

        if (!FarmEconomyManager.Instance.SpendGems(cost))
            return;

        StopAllCoroutines();                             // dừng mọi timer coroutine nếu có

        // Ép trạng thái Ready ngay lập tức, không phụ thuộc finishUnixTime
        finishUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state = PlotState.Ready;
        Save();
        RefreshVisual();

    }

    /// <summary>Xóa sạch toàn bộ save của các ô đất (PLOT_NORMAL_* và PLOT_RARE_*) khỏi PlayerPrefs.
    /// Dùng khi cần reset trang trại hoặc dọn "ô đất ma" còn sót từ Play Mode cũ.</summary>
    [ContextMenu("Debug: Clear All Plot Data")]
    public void DebugClearData()
    {
        int removed = 0;
        for (int i = 0; i <= 200; i++)
        {
            string nk = $"PLOT_NORMAL_{i}";
            string rk = $"PLOT_RARE_{i}";
            if (PlayerPrefs.HasKey(nk)) { PlayerPrefs.DeleteKey(nk); removed++; }
            if (PlayerPrefs.HasKey(rk)) { PlayerPrefs.DeleteKey(rk); removed++; }
        }
        // Xóa luôn danh sách nhà/công trình đã đặt
        if (PlayerPrefs.HasKey(PlacementManager.BuildingsSaveKey))
        {
            PlayerPrefs.DeleteKey(PlacementManager.BuildingsSaveKey);
            removed++;
        }
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    public void RefreshVisual()
    {
        TryResolvePlantedCrop();


        if (groundSprite != null)
            groundSprite.enabled = true;

        if (readyIcon != null)
            readyIcon.enabled = state == PlotState.Ready;

        if ((state == PlotState.Growing || state == PlotState.Ready) && plantedCrop != null)
        {
            float progress = GetGrowProgress01();

            if (cropVisual != null)
                cropVisual.ShowCrop(plantedCrop, progress);

            if (timerRoot != null)
                timerRoot.SetActive(true);

            if (timerText != null)
                timerText.text = state == PlotState.Ready ? "Chín" : GetRemainingTimeText();

            if (progressRoot != null)
                progressRoot.SetActive(state == PlotState.Growing);

            if (progressFill != null)
            {
                float p = Mathf.Clamp01(progress);

                Vector3 scale = progressFill.localScale;
                scale.x = p;
                progressFill.localScale = scale;

                if (progressLeftToRight)
                {
                    Vector3 pos = progressFill.localPosition;
                    pos.x = -(progressFullWidth * (1f - p)) * 0.5f;
                    progressFill.localPosition = pos;
                }
            }
        }
        else
        {
            if (cropVisual != null)
                cropVisual.ClearAll();

            if (timerRoot != null)
                timerRoot.SetActive(false);

            if (progressRoot != null)
                progressRoot.SetActive(false);
        }
    }

    public void ShowProgressBar(bool show)
    {
        if (progressRoot != null)
            progressRoot.SetActive(show);
    }

    private bool IsTimeUp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= finishUnixTime;
    }

    private void TryResolvePlantedCrop()
    {
        if (plantedCrop != null)
            return;

        if (string.IsNullOrEmpty(plantedCropId))
            return;

        if (FarmManager.Instance == null)
        {
            return;
        }

        CropData resolved = FarmManager.Instance.GetCropById(plantedCropId);
        plantedCrop = resolved;
    }

    private void Save()
    {
        PlotSaveData data = new PlotSaveData
        {
            saveVersion = CurrentSaveVersion,
            isUnlocked = state != PlotState.Locked,
            plantedCropId = plantedCropId,
            startUnixTime = startUnixTime,
            finishUnixTime = finishUnixTime,
            state = (int)state
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }

    /// <summary>
    /// Chuyển save v0 → v1 cho 8 ô đất bị cấp lại plotId ở F1.
    ///
    /// Chỉ chạy khi: (a) ô này nằm trong <see cref="LegacyPlotIdMap"/>, (b) khoá MỚI
    /// chưa tồn tại, (c) khoá CŨ có dữ liệu. Sau khi copy thì khoá mới tồn tại nên
    /// lần mở game sau không chạy lại — không có nguy cơ ghi đè tiến trình mới.
    ///
    /// KHÔNG xoá khoá cũ: nó vẫn là khoá thật của ô đất "song sinh" đã giữ nguyên id.
    /// </summary>
    private void MigrateLegacySaveIfNeeded()
    {
        string legacyKey = LegacySaveKey;
        if (string.IsNullOrEmpty(legacyKey))
            return;

        if (PlayerPrefs.HasKey(SaveKey))
            return;

        if (!PlayerPrefs.HasKey(legacyKey))
            return;

        string legacyJson = PlayerPrefs.GetString(legacyKey, "");
        if (string.IsNullOrEmpty(legacyJson))
            return;

        PlotSaveData legacy = JsonUtility.FromJson<PlotSaveData>(legacyJson);
        if (legacy == null)
            return;

        legacy.saveVersion = CurrentSaveVersion;
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(legacy));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs

        Debug.Log($"[Plot] Chuyển save ô đất {legacyKey} → {SaveKey} (F1: plotId trùng đã được cấp lại).");
    }

    private void Load()
    {
        // Phải chạy TRƯỚC mọi lần đọc SaveKey, nếu không ô mang id mới sẽ thấy khoá rỗng
        // rồi tự Save() một trạng thái trắng đè lên — đúng lúc đó là mất cây thật.
        MigrateLegacySaveIfNeeded();

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            state = PlotState.Empty;   // F10: không còn hệ khoá ô đất — ô mới luôn trống, sẵn sàng trồng
            plantedCrop = null;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            state = PlotState.Empty;   // F10: không còn hệ khoá ô đất — ô mới luôn trống, sẵn sàng trồng
            plantedCrop = null;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
            return;
        }

        PlotSaveData data = JsonUtility.FromJson<PlotSaveData>(json);
        if (data == null)
        {
            state = PlotState.Empty;   // F10: không còn hệ khoá ô đất — ô mới luôn trống, sẵn sàng trồng
            plantedCrop = null;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
            return;
        }

        state = data.isUnlocked ? (PlotState)data.state : PlotState.Locked;
        plantedCropId = data.plantedCropId;
        startUnixTime = data.startUnixTime;
        finishUnixTime = data.finishUnixTime;
        plantedCrop = null;

        // ── CHUYỂN ĐỔI save v0 → v1: hệ khoá ô đất đã bị xoá (F10 / quyết định #5) ──
        // Save cũ có thể ghi state = Locked. Từ bản này KHÔNG còn cách nào mở khoá nữa,
        // nên ô đó sẽ chết vĩnh viễn (`UnlockAllPlotsNow` bỏ qua ô đã có save). Nâng lên
        // Empty ngay tại đây, và chỉ một lần vì Save() bên dưới đóng dấu saveVersion = 1.
        if (state == PlotState.Locked)
        {
            state = PlotState.Empty;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
            RefreshVisual();
            return;
        }

        // Nếu state là Growing/Ready nhưng không có cropId → dữ liệu bị hỏng, reset về Empty
        if ((state == PlotState.Growing || state == PlotState.Ready) && string.IsNullOrEmpty(plantedCropId))
        {
            state = PlotState.Empty;
            plantedCropId = "";
            startUnixTime = 0;
            finishUnixTime = 0;
            Save();
            return;
        }

        TryResolvePlantedCrop();

        if (state == PlotState.Growing && IsTimeUp())
            state = PlotState.Ready;

        RefreshVisual();
    }
}

