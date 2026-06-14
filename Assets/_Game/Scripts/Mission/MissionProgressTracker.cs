using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Theo dõi tiến độ nhiệm vụ toàn cục.
/// - Key chuẩn: "{MissionEventType}:{itemId-thường}" + key gộp "{MissionEventType}:*".
/// - Persist qua PlayerPrefs (1 blob JSON — JsonUtility wrapper, key MISSION_PROGRESS_V1).
/// - Daily: tiến độ ghi song song vào map riêng, tự reset khi sang ngày mới (yyyyMMdd).
/// - ReachLevel: tự nối PlayerProgressManager.OnLevelChanged; tiến độ = level hiện tại.
/// API cũ (Instance.GetProgress/SetProgress/AddProgress) giữ nguyên làm shim.
/// </summary>
public class MissionProgressTracker : MonoBehaviour
{
    public static MissionProgressTracker Instance { get; private set; }

    /// <summary>Bắn mỗi khi 1 key tiến độ đổi giá trị: (canonicalKey, newValue).</summary>
    public static event Action<string, int> OnProgressChanged;

    private const string PrefsKey = "MISSION_PROGRESS_V1";
    private const string AnyToken = "*";

    private static Dictionary<string, int> _progress;      // cộng dồn (mission chính)
    private static Dictionary<string, int> _dailyProgress; // trong ngày (mission daily)
    private static string _dailyDate = "";
    private static bool _levelHookInstalled;

    [Serializable]
    private class SaveBlob
    {
        public List<string> keys        = new List<string>();
        public List<int>    values      = new List<int>();
        public List<string> dailyKeys   = new List<string>();
        public List<int>    dailyValues = new List<int>();
        public string       dailyDate   = "";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureLoaded();
        TryInstallLevelHook();
    }

    private void Start()
    {
        // PlayerProgressManager.Awake có thể chạy sau Awake của tracker — thử lại lần nữa
        TryInstallLevelHook();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API mới
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ghi nhận 1 sự kiện gameplay.
    /// itemId rỗng → chỉ cộng key gộp "{type}:*".
    /// includeTypeWide=false → chỉ cộng key item, KHÔNG cộng key gộp
    /// (dùng cho item thứ 2 trong cùng 1 đơn hàng để "DeliverOrder:*" đếm SỐ ĐƠN).
    /// </summary>
    public static void ReportEvent(MissionEventType type, string itemId, int amount, bool includeTypeWide = true)
    {
        if (amount <= 0) return;
        EnsureLoaded();
        EnsureDailyFresh();

        string id = NormalizeId(itemId);
        if (id.Length > 0)
            Bump(KeyFor(type, id), amount);
        if (includeTypeWide)
            Bump(KeyFor(type, AnyToken), amount);

        SaveToPrefs();
        Debug.Log($"[MissionTracker] {type}:{(id.Length > 0 ? id : AnyToken)} +{amount}");
    }

    /// <summary>Tiến độ hiện tại của 1 mission (tự chọn key theo eventType + targetItemId + isDaily).</summary>
    public static int GetProgressFor(MissionData mission)
    {
        if (mission == null) return 0;
        EnsureLoaded();

        // ReachLevel: tiến độ = level hiện tại (đọc trực tiếp, không cộng dồn)
        if (mission.eventType == MissionEventType.ReachLevel)
        {
            int live = 0;
            if (PlayerProgressManager.Instance != null)
                live = PlayerProgressManager.Instance.Level;
            else if (FarmLevelManager.Instance != null)
                live = FarmLevelManager.Instance.CurrentLevel;

            int stored = Raw(_progress, KeyFor(MissionEventType.ReachLevel, AnyToken));
            return Mathf.Max(live, stored);
        }

        string id  = NormalizeId(mission.targetItemId);
        string key = KeyFor(mission.eventType, id.Length > 0 ? id : AnyToken);

        if (mission.isDaily)
        {
            EnsureDailyFresh();
            return Raw(_dailyProgress, key);
        }

        return Raw(_progress, key);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API cũ — shim giữ cho code hiện hữu hoạt động nguyên trạng
    // ─────────────────────────────────────────────────────────────────────

    public int GetProgress(string key)
    {
        EnsureLoaded();
        return Raw(_progress, key);
    }

    public void SetProgress(string key, int value)
    {
        EnsureLoaded();
        _progress[key] = value;
        SaveToPrefs();
        OnProgressChanged?.Invoke(key, value);
    }

    /// <summary>Shim cũ: PlotController từng gọi với harvestItemId → chuyển thành sự kiện HarvestItem.</summary>
    public void AddProgress(string harvestItemId, int amount = 1)
    {
        ReportEvent(MissionEventType.HarvestItem, harvestItemId, amount);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ReachLevel hook
    // ─────────────────────────────────────────────────────────────────────

    private static void TryInstallLevelHook()
    {
        if (_levelHookInstalled) return;

        var ppm = PlayerProgressManager.Instance;
        if (ppm == null) return;

        ppm.OnLevelChanged += HandleLevelChanged;
        _levelHookInstalled = true;
        HandleLevelChanged(ppm.Level); // sync ngay level hiện tại
    }

    private static void HandleLevelChanged(int level)
    {
        EnsureLoaded();
        string key = KeyFor(MissionEventType.ReachLevel, AnyToken);
        if (Raw(_progress, key) >= level) return;

        _progress[key] = level;
        SaveToPrefs();
        OnProgressChanged?.Invoke(key, level);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Nội bộ
    // ─────────────────────────────────────────────────────────────────────

    private static string NormalizeId(string itemId)
        => string.IsNullOrWhiteSpace(itemId) ? "" : itemId.Trim().ToLowerInvariant();

    private static string KeyFor(MissionEventType type, string id) => $"{type}:{id}";

    private static int Raw(Dictionary<string, int> map, string key)
        => map != null && map.TryGetValue(key, out int v) ? v : 0;

    private static void Bump(string key, int amount)
    {
        int newMain = Raw(_progress, key) + amount;
        _progress[key] = newMain;

        _dailyProgress[key] = Raw(_dailyProgress, key) + amount;

        OnProgressChanged?.Invoke(key, newMain);
    }

    private static void EnsureDailyFresh()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");
        if (_dailyDate == today) return;

        _dailyDate = today;
        _dailyProgress.Clear();
        SaveToPrefs();
    }

    private static void EnsureLoaded()
    {
        if (_progress != null) return;

        _progress      = new Dictionary<string, int>();
        _dailyProgress = new Dictionary<string, int>();

        string json = PlayerPrefs.GetString(PrefsKey, "");
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var blob = JsonUtility.FromJson<SaveBlob>(json);
            if (blob == null) return;

            for (int i = 0; i < blob.keys.Count && i < blob.values.Count; i++)
                _progress[blob.keys[i]] = blob.values[i];

            for (int i = 0; i < blob.dailyKeys.Count && i < blob.dailyValues.Count; i++)
                _dailyProgress[blob.dailyKeys[i]] = blob.dailyValues[i];

            _dailyDate = blob.dailyDate ?? "";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MissionTracker] Không đọc được save blob — reset tiến độ. {e.Message}");
        }
    }

    private static void SaveToPrefs()
    {
        if (_progress == null) return;

        var blob = new SaveBlob { dailyDate = _dailyDate };
        foreach (var kv in _progress)      { blob.keys.Add(kv.Key);      blob.values.Add(kv.Value); }
        foreach (var kv in _dailyProgress) { blob.dailyKeys.Add(kv.Key); blob.dailyValues.Add(kv.Value); }

        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(blob));
        PlayerPrefs.Save();
    }
}
