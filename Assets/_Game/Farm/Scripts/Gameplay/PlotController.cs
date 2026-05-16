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
        public bool isUnlocked;
        public string plantedCropId;
        public long startUnixTime;
        public long finishUnixTime;
        public int state;
    }

    [Header("Category")]
    [SerializeField] private PlotCategory plotCategory = PlotCategory.Normal;

    [Header("Identity")]
    [SerializeField] private int plotId = 1;
    [SerializeField] private bool isRarePlot = false;

    [Header("Unlock")]
    [SerializeField] private bool unlockedAtStart = false;
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private int gemCost = 0;
    [SerializeField] private bool requireAd = false;

    [Header("Refs")]
    [SerializeField] private CropProcessPopupUI processPopup;
    [SerializeField] private SpriteRenderer groundSprite;
    [SerializeField] private Transform cropGroup;
    [SerializeField] private PlotCropVisual cropVisual;
    [SerializeField] private SpriteRenderer lockSprite;
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
    public bool IsRarePlot => isRarePlot;
    public bool IsUnlocked => state != PlotState.Locked;
    public bool IsPlanted => state == PlotState.Growing || state == PlotState.Ready;
    public bool IsEmpty => state == PlotState.Empty;
    public bool IsGrowing => state == PlotState.Growing;
    public bool IsReady => state == PlotState.Ready;
    public int RequiredLevel => requiredLevel;
    public int GemCost => gemCost;
    public bool RequireAd => requireAd;
    public CropData CurrentCrop => plantedCrop;

    private string SaveKey => isRarePlot ? $"PLOT_RARE_{plotId}" : $"PLOT_NORMAL_{plotId}";

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

        if (cropVisual != null)
            cropVisual.ClearAll();
    }

    private void Start()
    {
        if (processPopup == null)
            processPopup = FindObjectOfType<CropProcessPopupUI>(true);

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

    private void Update()
    {
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

        if (FarmManager.Instance == null)
        {
            Debug.LogError("FarmManager.Instance NULL");
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
        lockSprite = null;
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

        t = transform.Find("LockSprite");
        if (t != null) lockSprite = t.GetComponent<SpriteRenderer>();

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

    /// <summary>
    /// Trả về vị trí để farmer đứng cuốc.
    /// Ưu tiên child "FarmerStandPoint" nếu có, fallback về plot.transform.position.
    /// </summary>
    public Vector3 GetFarmerStandPosition()
    {
        Transform standPoint = transform.Find("FarmerStandPoint");
        return standPoint != null ? standPoint.position : transform.position;
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

    [ContextMenu("Clear This Plot Save")]
    public void ClearThisPlotSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        state = unlockedAtStart ? PlotState.Empty : PlotState.Locked;
        plantedCrop = null;
        plantedCropId = "";
        startUnixTime = 0;
        finishUnixTime = 0;

        if (cropVisual != null)
            cropVisual.ClearAll();

        RefreshVisual();
        Debug.Log("Cleared save for: " + SaveKey);
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

    public bool CanUnlockByLevel()
    {
        if (FarmLevelManager.Instance == null)
            return requiredLevel <= 1;

        return FarmLevelManager.Instance.HasReached(requiredLevel);
    }

    public bool CanOpenSeedPopup()
    {
        return state == PlotState.Empty;
    }

    public bool CanPlantCrop(CropData crop)
    {
        if (crop == null)
        {
            Debug.LogWarning($"[CanPlantCrop] FAIL: crop null | plot={name}");
            return false;
        }

        if (state != PlotState.Empty)
        {
            Debug.LogWarning($"[CanPlantCrop] FAIL: plot not empty | plot={name} state={state}");
            return false;
        }

        // Chặn trồng sai loại: plot hoa chỉ nhận Flower, plot thường chỉ nhận Normal.
        if ((int)crop.cropCategory != (int)plotCategory)
        {
            Debug.LogWarning($"[CanPlantCrop] FAIL category mismatch | plot={name} plotCategory={plotCategory} | crop={crop.cropId} cropCategory={crop.cropCategory}" +
                             $"\n=> Nếu crop.cropCategory=Normal nhưng crop là hoa: mở inspector CropData, đặt cropCategory=Flower");
            return false;
        }

        return true;
    }

    public bool TryPlant(CropData crop)
    {
        Debug.Log($"[TryPlant] Plot={name}, State={state}, Crop={(crop != null ? crop.displayName : "NULL")}");

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

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.AddItem(harvestItemId, amount);

        // Cập nhật tiến độ nhiệm vụ tân thủ theo loại nông sản vừa thu hoạch
        MissionProgressTracker.Instance?.AddProgress(harvestItemId, amount);

        AutoFindHarvestSpawnPoint();
        AutoFindExpSpawnPoint();

        bool plotIsRectTransform = transform is RectTransform;
        bool spawnIsRectTransform = harvestSpawnPoint != null && harvestSpawnPoint is RectTransform;

        Vector3 plotWorldPos = transform.position;
        Vector3 spawnPointWorldPos = harvestSpawnPoint != null ? harvestSpawnPoint.position : Vector3.zero;

        Vector3 fxSpawn = GetHarvestSpawnPosition();

        Debug.Log(
            $"[Harvest] WorldPos Debug | plot={name} | plotPath={GetTransformPath(transform)} | plot.transform.position={plotWorldPos} | " +
            $"harvestSpawnPoint={(harvestSpawnPoint != null ? harvestSpawnPoint.name : "NULL")} | harvestSpawnPointPath={GetTransformPath(harvestSpawnPoint)} | " +
            $"harvestSpawnPoint.position={(harvestSpawnPoint != null ? spawnPointWorldPos.ToString() : "NULL")} | final fxSpawn={fxSpawn} | " +
            $"plotIsRectTransform={plotIsRectTransform} | harvestSpawnPointIsRectTransform={spawnIsRectTransform}"
        );

        HarvestFeedbackSpawner.Instance?.Spawn(
            transform.position + new Vector3(0f, 1.2f, 0f),
            $"+{amount} {harvestedCrop.displayName}"
        );

        // Ưu tiên harvestIcon (gán riêng trong CropData), fallback về icon rồi readySprite như cũ
        Sprite fxIcon = harvestedCrop.harvestIcon != null
            ? harvestedCrop.harvestIcon
            : (harvestedCrop.icon != null ? harvestedCrop.icon : harvestedCrop.readySprite);

        Debug.Log($"[Harvest] SpawnHarvestFly | plot={name} | crop={harvestedCrop.displayName} | cropId={harvestedCrop.cropId} | amount={amount} | icon={(fxIcon != null ? fxIcon.name : "NULL")} | fxSpawn={fxSpawn}");

        HarvestFeedbackSpawner.Instance?.SpawnHarvestFly(
            fxIcon,
            fxSpawn,
            amount
        );

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

    /// <summary>Trừ 1 Gem và chín lúa ngay lập tức — gắn vào nút Gem trên CropProcessPopupUI.</summary>
    public void InstantGrow()
    {
        if (state != PlotState.Growing)
        {
            Debug.LogWarning($"[PlotController] InstantGrow bị gọi nhưng state={state} (không phải Growing). Plot={name}");
            return;
        }

        if (FarmEconomyManager.Instance == null)
        {
            Debug.LogError("[PlotController] InstantGrow: FarmEconomyManager.Instance NULL.");
            return;
        }

        if (FarmEconomyManager.Instance.Gems < 1)
        {
            Debug.Log("[PlotController] InstantGrow: không đủ Gem.");
            return;
        }

        StopAllCoroutines();                             // dừng mọi timer coroutine nếu có
        FarmEconomyManager.Instance.SpendGems(1);

        // Ép trạng thái Ready ngay lập tức, không phụ thuộc finishUnixTime
        finishUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state = PlotState.Ready;
        Save();
        RefreshVisual();

        Debug.Log($"[PlotController] InstantGrow OK — Plot={name}, state={state}");
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
        PlayerPrefs.Save();
        Debug.Log($"[PlotController] DebugClearData: đã xóa {removed} key(s). Reload scene để áp dụng.");
    }

    public void ApplyWaterBonus(int reduceSeconds)
    {
        if (state != PlotState.Growing)
            return;

        if (reduceSeconds <= 0)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        finishUnixTime -= reduceSeconds;

        if (finishUnixTime <= now)
        {
            finishUnixTime = now;
            state = PlotState.Ready;
        }

        Save();
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        TryResolvePlantedCrop();

        Debug.Log($"[RefreshVisual] {name} | state={state} | plantedCrop={plantedCrop?.cropId ?? "NULL"} | plantedCropId={plantedCropId}");

        if (groundSprite != null)
            groundSprite.enabled = true;

        if (lockSprite != null)
            lockSprite.enabled = state == PlotState.Locked;

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
            Debug.LogWarning($"[ResolveCrop] {name} | FarmManager.Instance == NULL | cropId={plantedCropId}");
            return;
        }

        CropData resolved = FarmManager.Instance.GetCropById(plantedCropId);
        Debug.Log($"[ResolveCrop] {name} | Looking for cropId={plantedCropId} | DB count={FarmManager.Instance.CropDatabaseCount} | resolved={(resolved != null ? resolved.cropId : "NULL")}");
        plantedCrop = resolved;
    }

    private void Save()
    {
        PlotSaveData data = new PlotSaveData
        {
            isUnlocked = state != PlotState.Locked,
            plantedCropId = plantedCropId,
            startUnixTime = startUnixTime,
            finishUnixTime = finishUnixTime,
            state = (int)state
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            state = unlockedAtStart ? PlotState.Empty : PlotState.Locked;
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
            state = unlockedAtStart ? PlotState.Empty : PlotState.Locked;
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
            state = unlockedAtStart ? PlotState.Empty : PlotState.Locked;
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

