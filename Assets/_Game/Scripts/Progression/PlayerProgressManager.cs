using System;
using UnityEngine;

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }

    private const string Pref_Level = "PLAYER_LEVEL";
    private const string Pref_Exp = "PLAYER_EXP";

    [Header("Config")]
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int startExp = 0;
    [SerializeField] private int maxLevel = 30;

    public int Level { get; private set; }
    public int CurrentExp { get; private set; }

    public event Action<int, int> OnExpChanged; // currentExp, requiredExp
    public event Action<int> OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // Tách ra root để DontDestroyOnLoad hoạt động (fix warning)
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void Start()
    {
        RaiseAll();
    }

    public int RequiredExpForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        int n = level - 1;

        // Rebalanced for max level 30.
        // A smooth quadratic-ish curve that ramps up noticeably toward level 30,
        // without exploding like the 100-level curve.
        // L1 ~ 40, L10 ~ 150, L20 ~ 410, L30 ~ 820
        return 40 + (n * 10) + (n * n);
    }

    public int RequiredExpCurrentLevel => RequiredExpForLevel(Level);

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        if (Level >= maxLevel)
        {
            CurrentExp = 0;
            Save();
            RaiseExpChanged();
            return;
        }

        CurrentExp += amount;

        bool leveledUp = false;
        while (Level < maxLevel)
        {
            int required = RequiredExpForLevel(Level);
            if (CurrentExp < required)
                break;

            CurrentExp -= required;
            Level++;
            leveledUp = true;

            if (Level >= maxLevel)
            {
                Level = maxLevel;
                CurrentExp = 0;
                break;
            }
        }

        Save();

        if (leveledUp)
            OnLevelChanged?.Invoke(Level);

        RaiseExpChanged();

        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.SetLevel(Level);
    }

    public void ForceSetLevelExp(int level, int exp)
    {
        Level = Mathf.Clamp(level, 1, maxLevel);
        CurrentExp = Mathf.Max(0, exp);

        if (Level >= maxLevel)
            CurrentExp = 0;

        Save();
        OnLevelChanged?.Invoke(Level);
        RaiseExpChanged();

        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.SetLevel(Level);
    }

    private void RaiseAll()
    {
        OnLevelChanged?.Invoke(Level);
        RaiseExpChanged();

        if (FarmLevelManager.Instance != null)
            FarmLevelManager.Instance.SetLevel(Level);
    }

    private void RaiseExpChanged()
    {
        OnExpChanged?.Invoke(CurrentExp, RequiredExpForLevel(Level));
    }

    private void Load()
    {
        Level = PlayerPrefs.GetInt(Pref_Level, Mathf.Max(1, startLevel));
        CurrentExp = PlayerPrefs.GetInt(Pref_Exp, Mathf.Max(0, startExp));

        Level = Mathf.Clamp(Level, 1, maxLevel);

        if (Level >= maxLevel)
            CurrentExp = 0;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(Pref_Level, Level);
        PlayerPrefs.SetInt(Pref_Exp, CurrentExp);
        PlayerPrefs.Save();
    }
}
