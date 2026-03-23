using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Identity")]
    [SerializeField] private int plotId = 1;
    [SerializeField] private bool isRarePlot = false;

    [Header("Unlock")]
    [SerializeField] private bool unlockedAtStart = false;
    [SerializeField] private int requiredLevel = 1;
    [SerializeField] private int gemCost = 0;
    [SerializeField] private bool requireAd = false;

    [Header("Refs")]
    [SerializeField] private SpriteRenderer groundSprite;
    [SerializeField] private Transform cropGroup;
    [SerializeField] private SpriteRenderer[] cropSprites = new SpriteRenderer[4];
    [SerializeField] private SpriteRenderer lockSprite;
    [SerializeField] private SpriteRenderer readyIcon;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;
    [SerializeField] private TMP_Text timerText;

    [Header("Progress UI")]
    [SerializeField] private Transform progressFill;
    [SerializeField] private GameObject progressRoot;

    private PlotState state;
    private CropData plantedCrop;
    private string plantedCropId = "";
    private long startUnixTime;
    private long finishUnixTime;

    private readonly HashSet<SpriteRenderer> validCropSpriteSet = new HashSet<SpriteRenderer>();

    public int PlotId => plotId;
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

    // Bind lại ref child đúng của chính plot này khi reset.
    private void Reset()
    {
        ForceRebindChildren();
    }

    // Bind lại ref child đúng của chính plot này khi inspector đổi.
    private void OnValidate()
    {
        ForceRebindChildren();
    }

    // Cache ref runtime, đồng thời dọn sạch sprite cũ của plot.
    private void Awake()
    {
        ForceRebindChildren();
        ClearAllCropVisualsImmediate();
        HideUnexpectedCropRenderers();
    }

    // Load save và đồng bộ visual ban đầu.
    private void Start()
    {
        Load();
        RefreshVisual();
    }

    // Tick thời gian grow, hết giờ thì chuyển sang Ready.
    private void Update()
    {
        if (state != PlotState.Growing)
            return;

        if (IsTimeUp())
        {
            state = PlotState.Ready;
            Save();
        }

        RefreshVisual();
    }

    // Click vào plot để chọn plot / mở popup / harvest.
    public void OnPointerClick(PointerEventData eventData)
    {
        HandlePlotClick();
    }

    // Luồng xử lý click tập trung.
    public void HandlePlotClick()
    {
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
            string harvestedName = plantedCrop != null ? plantedCrop.displayName : "Nông sản";
            bool harvested = Harvest();

            if (harvested)
                FarmManager.Instance.OnPlotHarvested(this, harvestedName);

            return;
        }

        if (state == PlotState.Growing)
        {
            FarmManager.Instance.OnGrowingPlotClicked(this);
            return;
        }

        if (state == PlotState.Empty)
        {
            FarmManager.Instance.OnPlotClicked(this);
        }
    }

    // Bind ref đúng theo child nằm dưới chính plot này, không dùng ref cũ bị dính từ plot khác.
    [ContextMenu("Force Rebind Children")]
    public void ForceRebindChildren()
    {
        groundSprite = null;
        cropGroup = null;
        lockSprite = null;
        readyIcon = null;
        timerRoot = null;
        timerText = null;
        progressRoot = null;
        progressFill = null;
        cropSprites = new SpriteRenderer[4];

        Transform t;

        t = transform.Find("GroundSprite");
        if (t != null) groundSprite = t.GetComponent<SpriteRenderer>();

        t = transform.Find("CropGroup");
        if (t != null) cropGroup = t;

        if (cropGroup != null)
        {
            Transform p1 = cropGroup.Find("CropPoint_1");
            Transform p2 = cropGroup.Find("CropPoint_2");
            Transform p3 = cropGroup.Find("CropPoint_3");
            Transform p4 = cropGroup.Find("CropPoint_4");

            if (p1 != null) cropSprites[0] = p1.GetComponent<SpriteRenderer>();
            if (p2 != null) cropSprites[1] = p2.GetComponent<SpriteRenderer>();
            if (p3 != null) cropSprites[2] = p3.GetComponent<SpriteRenderer>();
            if (p4 != null) cropSprites[3] = p4.GetComponent<SpriteRenderer>();
        }

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

        RebuildValidCropSpriteSet();
    }

    // Cache đúng 4 SpriteRenderer hợp lệ của cây.
    private void RebuildValidCropSpriteSet()
    {
        validCropSpriteSet.Clear();

        if (cropSprites == null)
            return;

        for (int i = 0; i < cropSprites.Length; i++)
        {
            if (cropSprites[i] != null)
                validCropSpriteSet.Add(cropSprites[i]);
        }
    }

    // Tắt toàn bộ SpriteRenderer dư trong CropGroup để bỏ cây giữa cũ.
    private void HideUnexpectedCropRenderers()
    {
        if (cropGroup == null)
            return;

        RebuildValidCropSpriteSet();

        SpriteRenderer[] allRenderers = cropGroup.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            SpriteRenderer sr = allRenderers[i];
            if (sr == null)
                continue;

            if (!validCropSpriteSet.Contains(sr))
            {
                sr.sprite = null;
                sr.enabled = false;
            }
        }
    }

    // Xóa ngay toàn bộ sprite cây của plot này để tránh mang theo visual cũ khi Ctrl+D.
    private void ClearAllCropVisualsImmediate()
    {
        if (cropSprites == null)
            return;

        for (int i = 0; i < cropSprites.Length; i++)
        {
            if (cropSprites[i] == null)
                continue;

            cropSprites[i].sprite = null;
            cropSprites[i].enabled = false;
        }
    }

    // Xóa save plot này để test lại từ đầu.
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

        ClearAllCropVisualsImmediate();
        RefreshVisual();

        Debug.Log("Cleared save for: " + SaveKey);
    }

    // Đổi trạng thái khóa / mở khóa.
    public void SetUnlocked(bool value)
    {
        state = value ? PlotState.Empty : PlotState.Locked;
        Save();
        RefreshVisual();
    }

    // Check điều kiện unlock bằng level.
    public bool CanUnlockByLevel()
    {
        if (FarmLevelManager.Instance == null)
            return requiredLevel <= 1;

        return FarmLevelManager.Instance.HasReached(requiredLevel);
    }

    // Chỉ mở popup seed khi plot đang Empty.
    public bool CanOpenSeedPopup()
    {
        return state == PlotState.Empty;
    }

    // Bản test: chỉ cần crop khác null và plot đang Empty là trồng được.
    public bool CanPlantCrop(CropData crop)
    {
        return crop != null && state == PlotState.Empty;
    }

    // Trồng crop vào plot hiện tại.
    public bool TryPlant(CropData crop)
    {
        Debug.Log($"[TryPlant] Plot={name}, State={state}, Crop={(crop != null ? crop.displayName : "NULL")}");

        if (crop == null)
        {
            Debug.LogError("[TryPlant] FAIL: crop NULL");
            return false;
        }

        if (state != PlotState.Empty)
        {
            Debug.LogError($"[TryPlant] FAIL: state hiện tại không phải Empty, state={state}");
            return false;
        }

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

    // Plant helper từ UI.
    public bool TryPlantFromUI(CropData crop)
    {
        bool planted = TryPlant(crop);

        if (planted && FarmManager.Instance != null)
            FarmManager.Instance.OnPlotPlanted(this, crop);

        return planted;
    }

    // Check plot đã sẵn sàng harvest chưa.
    public bool IsReadyToHarvest()
    {
        return state == PlotState.Ready;
    }

    // Lấy thời gian còn lại.
    public long GetRemainingSeconds()
    {
        if (state != PlotState.Growing)
            return 0;

        long remain = finishUnixTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, remain);
    }

    // Format thời gian còn lại.
    public string GetRemainingTimeText()
    {
        long remain = GetRemainingSeconds();
        long minutes = remain / 60;
        long seconds = remain % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    // Tính tiến độ grow từ 0 đến 1.
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

    // Thu hoạch rồi reset plot về Empty.
    public bool Harvest()
    {
        if (state != PlotState.Ready || plantedCrop == null)
            return false;

        string harvestItemId = string.IsNullOrEmpty(plantedCrop.harvestItemId)
            ? plantedCrop.cropId
            : plantedCrop.harvestItemId;

        int amount = Mathf.Max(1, plantedCrop.harvestAmount);

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.AddItem(harvestItemId, amount);

        plantedCrop = null;
        plantedCropId = "";
        startUnixTime = 0;
        finishUnixTime = 0;
        state = PlotState.Empty;

        Save();
        RefreshVisual();
        return true;
    }

    // Giảm thời gian grow cho plot.
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

    // Đồng bộ visual của plot theo state hiện tại.
    public void RefreshVisual()
    {
        ForceRebindChildren();
        HideUnexpectedCropRenderers();

        if (groundSprite != null)
            groundSprite.enabled = true;

        if (lockSprite != null)
            lockSprite.enabled = state == PlotState.Locked;

        if (readyIcon != null)
            readyIcon.enabled = state == PlotState.Ready;

        if ((state == PlotState.Growing || state == PlotState.Ready) && plantedCrop != null)
        {
            float progress = GetGrowProgress01();
            Sprite stageSprite = plantedCrop.GetStageSprite(progress);

            if (cropSprites != null)
            {
                for (int i = 0; i < cropSprites.Length; i++)
                {
                    if (cropSprites[i] == null)
                        continue;

                    cropSprites[i].sprite = stageSprite;
                    cropSprites[i].enabled = stageSprite != null;
                }
            }

            if (timerRoot != null)
                timerRoot.SetActive(true);

            if (timerText != null)
                timerText.text = state == PlotState.Ready ? "Chín" : GetRemainingTimeText();

            if (progressRoot != null)
                progressRoot.SetActive(state == PlotState.Growing);

            if (progressFill != null)
            {
                Vector3 scale = progressFill.localScale;
                scale.x = Mathf.Clamp01(progress);
                progressFill.localScale = scale;
            }
        }
        else
        {
            ClearAllCropVisualsImmediate();

            if (timerRoot != null)
                timerRoot.SetActive(false);

            if (progressRoot != null)
                progressRoot.SetActive(false);
        }
    }

    // Check hết giờ grow.
    private bool IsTimeUp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= finishUnixTime;
    }

    // Lưu save plot.
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

    // Load save plot.
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

        if (!string.IsNullOrEmpty(plantedCropId) && FarmManager.Instance != null)
            plantedCrop = FarmManager.Instance.GetCropById(plantedCropId);

        if (state == PlotState.Growing && IsTimeUp())
            state = PlotState.Ready;

        RefreshVisual();
    }
}