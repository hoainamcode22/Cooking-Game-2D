using System;
using UnityEngine;

public class LivestockPenController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string penId = "cow_pen_01";
    [SerializeField] private string penDisplayName = "Chuồng Bò";
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private Sprite animalSprite;

    [Header("Feed Config")]
    [SerializeField] private string feedItemId = "lua";
    [SerializeField] private int feedCost = 2;
    [SerializeField] private float feedDurationSeconds = 15f;

    [Header("Collect Config")]
    [SerializeField] private string outputItemId = "beef";
    [SerializeField] private int outputAmount = 4;

    [Header("Upgrade UI Only")]
    [SerializeField] private int previewNextLevel = 2;
    [SerializeField] private int previewGemCost = 5;
    [SerializeField] private int previewGoldCost = 300;

    private bool isFeeding;
    private bool readyToCollect;
    private long feedStartUnix;
    private long feedEndUnix;

    public string PenDisplayName => penDisplayName;
    public int CurrentLevel => currentLevel;
    public int PreviewNextLevel => previewNextLevel;
    public int PreviewGemCost => previewGemCost;
    public int PreviewGoldCost => previewGoldCost;
    public Sprite AnimalSprite => animalSprite;

    public bool IsFeeding => isFeeding;
    public bool ReadyToCollect => readyToCollect;

    private void Awake()
    {
        LoadState();
        RefreshStateByTime();
    }

    private void OnEnable()
    {
        RefreshStateByTime();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveState();
    }

    private void OnApplicationQuit()
    {
        SaveState();
    }

    private void OnDisable()
    {
        SaveState();
    }
    // mở chuồngg ra popup
    private void OnMouseDown()
    {
        Debug.Log("CLICKED COW PEN");
        OpenPopup();
    }
    // Hàm mở popup, gọi đến UI để hiển thị thông tin và tương tác
    public void OpenPopup()
    {
        Debug.Log("[LivestockPenController] OpenPopup called: " + gameObject.name);

        RefreshStateByTime();

        if (LivestockPenPopupUI.Instance == null)
        {
            Debug.LogError("[LivestockPenController] LivestockPenPopupUI.Instance is NULL");
            return;
        }

        LivestockPenPopupUI.Instance.Open(this);
    }

    public void RefreshStateByTime()
    {
        if (!isFeeding)
            return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (now >= feedEndUnix)
        {
            isFeeding = false;
            readyToCollect = true;
            SaveState();
        }
    }

    public bool CanFeed()
    {
        RefreshStateByTime();

        if (isFeeding) return false;
        if (readyToCollect) return false;

        return true;
    }

    public bool CanCollect()
    {
        RefreshStateByTime();
        return readyToCollect;
    }

    public bool TryFeed()
    {
        Debug.Log("[LivestockPenController] TryFeed called");

        RefreshStateByTime();

        if (!CanFeed())
        {
            Debug.Log("[LivestockPenController] Cannot feed now");
            return false;
        }

        if (WarehouseInventory.Instance == null)
        {
            Debug.LogError("[LivestockPenController] WarehouseInventory NULL");
            return false;
        }

        bool consumed = WarehouseInventory.Instance.TryConsume(feedItemId, feedCost);
        Debug.Log("[LivestockPenController] Consume result = " + consumed);

        if (!consumed)
        {
            Debug.Log("[LivestockPenController] Not enough feed item");
            return false;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        isFeeding = true;
        readyToCollect = false;
        feedStartUnix = now;
        feedEndUnix = now + Mathf.CeilToInt(feedDurationSeconds);

        SaveState();

        Debug.Log($"[LivestockPenController] Feeding started. End at {feedEndUnix}");
        return true;
    }

    public bool TryCollect()
    {
        RefreshStateByTime();

        if (!readyToCollect)
        {
            Debug.Log("[Pen] Chưa tới lúc thu thập.");
            return false;
        }

        if (WarehouseInventory.Instance == null)
        {
            Debug.LogError("[Pen] Chưa có WarehouseInventory trong scene.");
            return false;
        }

        WarehouseInventory.Instance.AddItem(outputItemId, outputAmount);

        readyToCollect = false;
        isFeeding = false;
        feedStartUnix = 0;
        feedEndUnix = 0;

        SaveState();

        Debug.Log($"[Pen] Thu thập thành công: {outputItemId} x{outputAmount}");
        return true;
    }

    public float GetProgress01()
    {
        RefreshStateByTime();

        if (readyToCollect)
            return 1f;

        if (!isFeeding)
            return 0f;

        long total = feedEndUnix - feedStartUnix;
        if (total <= 0)
            return 0f;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = now - feedStartUnix;

        return Mathf.Clamp01((float)elapsed / total);
    }

    public int GetProgressPercent()
    {
        return Mathf.RoundToInt(GetProgress01() * 100f);
    }

    public int GetRemainingSeconds()
    {
        RefreshStateByTime();

        if (!isFeeding)
            return 0;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long remain = feedEndUnix - now;
        return Mathf.Max(0, (int)remain);
    }

    public string GetTitleText()
    {
        return $"{penDisplayName}-cấp {currentLevel}";
    }

    public string GetNextLevelText()
    {
        return $"Cấp {previewNextLevel}";
    }

    private void SaveState()
    {
        string key = GetKeyPrefix();

        PlayerPrefs.SetInt(key + "_isFeeding", isFeeding ? 1 : 0);
        PlayerPrefs.SetInt(key + "_readyToCollect", readyToCollect ? 1 : 0);
        PlayerPrefs.SetString(key + "_feedStartUnix", feedStartUnix.ToString());
        PlayerPrefs.SetString(key + "_feedEndUnix", feedEndUnix.ToString());
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        string key = GetKeyPrefix();

        isFeeding = PlayerPrefs.GetInt(key + "_isFeeding", 0) == 1;
        readyToCollect = PlayerPrefs.GetInt(key + "_readyToCollect", 0) == 1;

        long.TryParse(PlayerPrefs.GetString(key + "_feedStartUnix", "0"), out feedStartUnix);
        long.TryParse(PlayerPrefs.GetString(key + "_feedEndUnix", "0"), out feedEndUnix);
    }

    private string GetKeyPrefix()
    {
        return $"LIVESTOCK_PEN_{penId}";
    }
}