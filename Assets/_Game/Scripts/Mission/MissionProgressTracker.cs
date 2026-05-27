using System.Collections.Generic;
using UnityEngine;

public class MissionProgressTracker : MonoBehaviour
{
    public static MissionProgressTracker Instance { get; private set; }

    private const string PrefKeys   = "MISSION_KEYS";
    private const string PrefPrefix = "MISSION_";

    private readonly Dictionary<string, int> _progressMap = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public int GetProgress(string missionName)
    {
        return _progressMap.TryGetValue(missionName, out int val) ? val : 0;
    }

    public void SetProgress(string missionName, int value)
    {
        _progressMap[missionName] = value;
        Save(missionName);
    }

    public void AddProgress(string missionName, int amount = 1)
    {
        _progressMap[missionName] = GetProgress(missionName) + amount;
        Save(missionName);
    }

    private void Save(string missionName)
    {
        PlayerPrefs.SetInt(PrefPrefix + missionName, _progressMap[missionName]);
        var keyList = new KeyList { keys = new List<string>(_progressMap.Keys) };
        PlayerPrefs.SetString(PrefKeys, JsonUtility.ToJson(keyList));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(PrefKeys, "");
        if (string.IsNullOrEmpty(json)) return;

        var keyList = JsonUtility.FromJson<KeyList>(json);
        if (keyList?.keys == null) return;

        foreach (string key in keyList.keys)
            _progressMap[key] = PlayerPrefs.GetInt(PrefPrefix + key, 0);
    }

    [System.Serializable]
    private class KeyList { public List<string> keys = new List<string>(); }
}
