using System;
using UnityEngine;

public class FarmLevelManager : MonoBehaviour
{
    public static FarmLevelManager Instance { get; private set; }

    [SerializeField] private int startLevel = 1;
    public int CurrentLevel { get; private set; }

    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentLevel = Mathf.Max(1, startLevel);
    }

    private void Start()
    {
        OnLevelChanged?.Invoke(CurrentLevel);
    }

    public bool HasReached(int level)
    {
        return CurrentLevel >= level;
    }

    public void SetLevel(int level)
    {
        level = Mathf.Max(1, level);

        if (level == CurrentLevel)
            return;

        CurrentLevel = level;
        OnLevelChanged?.Invoke(CurrentLevel);
    }
}