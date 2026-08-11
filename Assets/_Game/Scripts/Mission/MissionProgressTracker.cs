using System;
using System.Collections.Generic;
using UnityEngine;



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

    /// <summary>
    /// B4 — phiên bản của blob tiến độ nhiệm vụ.
    ///
    /// ⚠️ Tên khoá `MISSION_PROGRESS_V1` có chữ "V1" nhưng đó CHỈ LÀ TÊN, không phải cơ chế
    /// version: khi đổi cấu trúc, đổi tên khoá thành `_V2` là **mất sạch** tiến độ cũ chứ
    /// không chuyển đổi được gì. Nên version thật nằm trong blob, ở field `saveVersion`.
    ///
    /// v1 = key tiến độ dạng "{MissionEventType}:{targetItemId}" hoặc "{MissionEventType}:*".
    /// TĂNG SỐ NÀY nếu đổi cách ghép key hoặc xoá một `MissionEventType`.
    /// </summary>
    private const int CurrentSaveVersion = 1;

    /// <summary>
    /// Key tiến độ đã chết vì vật phẩm bị xoá khỏi dự án (A4 xoá 2 món cá). Không gỡ thì
    /// nhiệm vụ "nấu canh chua cá" giữ tiến độ vĩnh viễn mà chẳng có món nào nấu ra nó nữa.
    /// </summary>
    private static readonly string[] DeadKeySubstrings = { ":ca_nuong_tieu", ":canh_chua_ca", ":ca" };

    [Serializable]
    private class SaveBlob
    {
        // KHÔNG mặc định = CurrentSaveVersion: save đời trước không có khoá này nên
        // JsonUtility để 0, nhờ đó phân biệt được save cũ. `SaveToPrefs` luôn gán tường minh.
        public int saveVersion;

        public List<string> keys        = new List<string>();
        public List<int>    values      = new List<int>();
        public List<string> dailyKeys   = new List<string>();
        public List<int>    dailyValues = new List<int>();
        public string       dailyDate   = "";
    }

    /// <summary>
    /// F3 — TỰ DỰNG instance nếu trong scene không có.
    ///
    /// VẤN ĐỀ CŨ: `SCN_Farm` KHÔNG có object nào mang script này (grep guid trong scene =
    /// 0 kết quả). Các hàm static (`ReportEvent`, `GetProgressFor`) vẫn chạy vì chúng tự
    /// `EnsureLoaded()`, nên bug rất khó thấy — nhưng `TryInstallLevelHook()` nằm trong
    /// Awake/Start, và không có instance thì hook `PlayerProgressManager.OnLevelChanged`
    /// KHÔNG BAO GIỜ được cài. Hậu quả: mọi mission loại `ReachLevel` đứng im ở 0 (trừ
    /// khi popup nhiệm vụ tình cờ đọc `PlayerProgressManager.Level` trực tiếp).
    ///
    /// VÌ SAO dựng bằng code chứ không thêm object vào scene: `SCN_Farm` là file 594.000
    /// dòng, thêm object vào đó là mổ YAML với rủi ro lan truyền. Và tracker còn phải sống
    /// ở scene bếp nữa — bootstrap bằng code đúng cho MỌI scene, không phải nhớ thêm tay
    /// vào từng scene mới.
    ///
    /// AfterSceneLoad chứ không phải BeforeSceneLoad: `TryInstallLevelHook` cần
    /// `PlayerProgressManager.Instance` đã tồn tại; nếu chưa, `Start()` sẽ thử lại.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (Instance != null) return;

        var existing = FindFirstObjectByType<MissionProgressTracker>(FindObjectsInactive.Include);
        if (existing != null) return;   // Awake của nó sẽ tự gán Instance

        var go = new GameObject("MissionProgressTracker(Auto)");
        go.AddComponent<MissionProgressTracker>();
        Debug.Log("[MissionTracker] Scene không có tracker — đã tự dựng instance (F3).");
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
        HandleLevelChanged(ppm.Level); 
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

            if (blob.saveVersion > CurrentSaveVersion)
            {
                // Save mới hơn code = hạ cấp bản game. Đọc tiếp chứ không xoá: dữ liệu chỉ là
                // (key, số), key lạ thì không nhiệm vụ nào khớp — vô hại. Xoá thì mất tiến độ.
                Debug.LogWarning($"[MissionTracker] Save v{blob.saveVersion} mới hơn code " +
                                 $"v{CurrentSaveVersion} — đọc tiếp, key lạ sẽ bị bỏ qua.");
            }

            bool canMigrate = blob.saveVersion < CurrentSaveVersion;
            int boDi = 0;

            for (int i = 0; i < blob.keys.Count && i < blob.values.Count; i++)
            {
                if (canMigrate && LaKeyChet(blob.keys[i])) { boDi++; continue; }
                _progress[blob.keys[i]] = blob.values[i];
            }

            for (int i = 0; i < blob.dailyKeys.Count && i < blob.dailyValues.Count; i++)
            {
                if (canMigrate && LaKeyChet(blob.dailyKeys[i])) { boDi++; continue; }
                _dailyProgress[blob.dailyKeys[i]] = blob.dailyValues[i];
            }

            _dailyDate = blob.dailyDate ?? "";

            if (canMigrate)
            {
                Debug.Log($"[MissionTracker] Chuyển save v{blob.saveVersion} → v{CurrentSaveVersion}" +
                          (boDi > 0 ? $", bỏ {boDi} key của vật phẩm đã xoá." : "."));
                SaveToPrefs();   // ghi lại kèm dấu phiên bản → chỉ chuyển MỘT LẦN
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MissionTracker] Không đọc được save blob — reset tiến độ. {e.Message}");
        }
    }

    /// <summary>Key trỏ vào một vật phẩm đã bị xoá khỏi dự án?</summary>
    private static bool LaKeyChet(string key)
    {
        if (string.IsNullOrEmpty(key)) return true;

        for (int i = 0; i < DeadKeySubstrings.Length; i++)
        {
            // So khớp HẬU TỐ chứ không phải Contains: `:ca` mà dùng Contains sẽ khớp luôn
            // `:cachua` và `:carot` — xoá oan tiến độ cà chua và cà rốt.
            if (key.EndsWith(DeadKeySubstrings[i], StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void SaveToPrefs()
    {
        if (_progress == null) return;

        // LUÔN gán tường minh (mặc định của field là 0 — cố ý, xem chú thích ở SaveBlob).
        var blob = new SaveBlob { saveVersion = CurrentSaveVersion, dailyDate = _dailyDate };
        foreach (var kv in _progress)      { blob.keys.Add(kv.Key);      blob.values.Add(kv.Value); }
        foreach (var kv in _dailyProgress) { blob.dailyKeys.Add(kv.Key); blob.dailyValues.Add(kv.Value); }

        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(blob));
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }
}
