using System;
using UnityEngine;

public class PlayerProgressManager : MonoBehaviour
{
    public static PlayerProgressManager Instance { get; private set; }

    private const string Pref_Level = "PLAYER_LEVEL";
    private const string Pref_Exp = "PLAYER_EXP";

    // B4 — họ save + phiên bản. Hai khoá này ghi thẳng số nguyên nên không có chỗ nhét
    // `saveVersion` vào; dấu phiên bản nằm ở khoá phụ `SAVE_VER_PLAYER_PROGRESS`.
    //
    // v1 = đường cong EXP hiện tại (`RequiredExpForLevel` = 40 + 10n + 3n²/20, maxLevel 100).
    // TĂNG SỐ NÀY nếu đổi công thức EXP: `CurrentExp` là EXP DƯ của cấp hiện tại, nên đổi
    // công thức mà không migrate thì người chơi có thể đang giữ số dư LỚN HƠN mốc cấp mới
    // ⇒ vào game là lên vài cấp một lúc, hoặc kẹt vì mốc mới cao hơn nhiều.
    private const string SaveFamily  = "PLAYER_PROGRESS";
    // v2 (vong 15): duong cong 30L + 7L^2, tran 50. Moc moi >= moc cu o MOI cap nen EXP du cu
    // luon < moc moi => khong ai len/tut cap; van kep du <= Req-1 mot lan cho chac.
    private const int    SaveVersion = 2;

    [Header("Config")]
    [SerializeField] private int startLevel = 1;
    [SerializeField] private int startExp = 0;
    /// <summary>
    /// TRẦN CỨNG của game. [2026-09-21] 30 -> 50: bo du lieu LevelReward_L2..L50 da day du
    /// (vang 100 -> 5.010, moc gem L35/40/45/50, unlock o L31/40/45/50), truoc day code chan o 30
    /// nen 20 cap cuoi khong bao gio dung toi.
    /// Trước đây field dưới để 100 nên người chơi farm lên 31, 32… vào vùng không có
    /// nội dung nào. Trần khai bằng const và kẹp đè lên giá trị Inspector: field
    /// serialize trong scene vẫn đang lưu 100, đổi mỗi default là không đủ.
    /// </summary>
    public const int CapToiDa = 50;

    [SerializeField] private int maxLevel = CapToiDa;

    public int Level { get; private set; }
    public int CurrentExp { get; private set; }

    public event Action<int, int> OnExpChanged; // currentExp, requiredExp
    public event Action<int> OnLevelChanged;

    // [V2 ADD] FX: bắn khi CỘNG EXP để RewardFlyFX bay sao xanh về HUD
    // (mirror FarmEconomyManager.OnGoldAddedFx / OnGemAddedFx — event tĩnh, chỉ phục vụ hiệu ứng,
    // KHÔNG mang logic gameplay; ai nghe cũng không được đổi state từ đây).
    public static event Action<int> OnExpAddedFx;

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

        // Kẹp trần TRƯỚC khi Load: giá trị Inspector trong scene đang lưu 100 (đè lên
        // default), không kẹp thì const ở trên vô nghĩa.
        maxLevel = Mathf.Min(maxLevel, CapToiDa);

        Load();

        // Vong 15: save v1 co the giu EXP du lon hon moc moi (khong the xay ra vi moc moi >= moc cu,
        // nhung kep mot lan cho chac). Khong bao gio tu len cap tai day.
        if (Level < maxLevel && CurrentExp >= RequiredExpForLevel(Level))
        {
            CurrentExp = Mathf.Max(0, RequiredExpForLevel(Level) - 1);
            Save();
        }

        // Save cũ đã leo quá trần → đưa về đúng trần một lần và lưu lại.
        if (Level > maxLevel)
        {
            Debug.LogWarning($"[Progress] Save đang ở cấp {Level} > trần {maxLevel} — kẹp về trần.");
            Level = maxLevel;
            CurrentExp = 0;
            Save();
        }
    }

    private void Start()
    {
        RaiseAll();
    }

    public int RequiredExpForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        int n = level - 1;

        // VONG 15 — DUONG CONG MOI: 30·L + 7·L². Ly do: ban cu tong L1->L30 chi 6.822 EXP, ma
        // moi o dat cho ~6 EXP/phut => 9 o = het 30 cap sau ~2 gio (do that). Ban moi: tong toi
        // L30 = 80.138, toi L50 = 338.728. Mo phong 45 phut/ngay: L10≈2 ngay, L20≈9, L30≈23, L50≈77.
        // GIU Required(L1) = 40 (tutorial: 8 lua x 5 EXP).
        // Moc: L2=88, L5=325, L10=1000, L20=3400, L30=7200, L40=12400, L50=19000.
        if (level <= 1) return 40;
        return 30 * level + 7 * level * level;
    }

    public int RequiredExpCurrentLevel => RequiredExpForLevel(Level);

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        AudioManager.Instance?.PlayExp();

        // [V2 ADD] Bắn FX TRƯỚC xử lý level-up: hiệu ứng "sao xanh bay về HUD" minh hoạ
        // việc NHẬN exp, không phụ thuộc kết quả cộng dồn/lên cấp phía dưới.
        OnExpAddedFx?.Invoke(amount);

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
        bool coSaveCu = PlayerPrefs.HasKey(Pref_Level) || PlayerPrefs.HasKey(Pref_Exp);

        SaveVersionGuard.Ensure(SaveFamily, SaveVersion, MigrateProgress, coSaveCu);

        Level = PlayerPrefs.GetInt(Pref_Level, Mathf.Max(1, startLevel));
        CurrentExp = PlayerPrefs.GetInt(Pref_Exp, Mathf.Max(0, startExp));

        Level = Mathf.Clamp(Level, 1, maxLevel);

        if (Level >= maxLevel)
            CurrentExp = 0;
    }

    /// <summary>
    /// Nhánh chuyển đổi save cấp/EXP. Hiện chỉ có v0 → v1 và định dạng KHÔNG đổi
    /// (vẫn là hai số nguyên cùng ý nghĩa), nên chỉ cần kẹp lại EXP dư cho an toàn:
    /// save đời cũ có thể mang `CurrentExp` lớn hơn mốc cấp hiện tại nếu công thức từng khác.
    /// Kẹp ở đây thay vì để `AddExp` tự xử lý, vì `AddExp` chỉ chạy khi người chơi nhận EXP —
    /// còn màn hình thanh EXP thì vẽ ngay lúc vào game.
    /// </summary>
    private void MigrateProgress(int cu, int moi)
    {
        int lv  = Mathf.Clamp(PlayerPrefs.GetInt(Pref_Level, 1), 1, maxLevel);
        int exp = Mathf.Max(0, PlayerPrefs.GetInt(Pref_Exp, 0));
        int can = RequiredExpForLevel(lv);

        if (exp >= can && lv < maxLevel)
        {
            Debug.LogWarning($"[PlayerProgress] Save v{cu}: EXP dư {exp} ≥ mốc cấp {lv} ({can}) — " +
                             $"kẹp lại còn {can - 1} để không nhảy cấp ngay khi vào game.");
            PlayerPrefs.SetInt(Pref_Exp, can - 1);
            LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(Pref_Level, Level);
        PlayerPrefs.SetInt(Pref_Exp, CurrentExp);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
    }
}
