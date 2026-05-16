using System.Collections.Generic;
using UnityEngine;

public class MissionProgressTracker : MonoBehaviour
{
    public static MissionProgressTracker Instance { get; private set; }

    private readonly Dictionary<string, int> _progressMap = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetProgress(string missionName)
    {
        return _progressMap.TryGetValue(missionName, out int val) ? val : 0;
    }

    public void SetProgress(string missionName, int value)
    {
        _progressMap[missionName] = value;
    }

    public void AddProgress(string missionName, int amount = 1)
    {
        _progressMap[missionName] = GetProgress(missionName) + amount;
    }
}
