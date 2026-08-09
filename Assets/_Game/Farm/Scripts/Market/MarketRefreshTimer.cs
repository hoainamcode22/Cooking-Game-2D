using System;
using UnityEngine;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  ĐỒNG HỒ LÀM MỚI BẢNG TIN — CHẠY NỀN + LƯU (A6 + A7)
/// ══════════════════════════════════════════════════════════════════════════
///
/// A7 — VÌ SAO không đếm bằng Time.deltaTime như bản cũ:
/// coroutine chỉ chạy khi popup đang bật. Người chơi đóng chợ đi trồng cây 10 phút
/// rồi mở lại thì đồng hồ vẫn y nguyên chỗ cũ — sai hoàn toàn. Thoát hẳn game
/// còn tệ hơn: đếm lại từ đầu. Ở đây mốc hết hạn được ghi bằng
/// DateTimeOffset.UtcNow.UtcTicks vào PlayerPrefs nên đúng trong mọi trường hợp.
///
/// Dùng UTC chứ không dùng giờ máy: người chơi đổi múi giờ hoặc chỉnh đồng hồ hệ thống
/// sẽ không nhảy chu kỳ. (Vặn ngược đồng hồ vẫn ăn gian được — chỉ server mới chặn nổi,
/// nhưng đây là game đơn nên chấp nhận.)
///
/// A6 — Làm mới trả bằng VÀNG, KHÔNG có gem, KHÔNG có đồng tiền thứ ba.
/// Giá luỹ tiến trong ngày để chặn bấm làm mới liên tục: 150 → 300 → 450 → …
/// và reset lúc sang ngày mới.
/// </summary>
public class MarketRefreshTimer
{
    // ── Khoá lưu ─────────────────────────────────────────────────────────
    private const string KeyVersion    = "MARKET_TIMER_SAVE_VERSION";
    private const string KeyNextTicks  = "MARKET_TIMER_NEXT_UTC_TICKS";
    private const string KeyCycleIndex = "MARKET_TIMER_CYCLE_INDEX";
    private const string KeyPaidCount  = "MARKET_REFRESH_PAID_COUNT";
    private const string KeyPaidDate   = "MARKET_REFRESH_PAID_DATE";

    /// <summary>
    /// Tăng số này khi đổi ý nghĩa dữ liệu lưu. Bản cũ hơn sẽ bị xoá và dựng lại
    /// thay vì đọc nhầm sang kiểu mới rồi ra đồng hồ âm.
    /// </summary>
    public const int CurrentSaveVersion = 1;

    // ── Cấu hình ─────────────────────────────────────────────────────────
    private readonly int cycleSeconds;
    private readonly int baseGoldCost;
    private readonly int maxGoldCost;

    // ── Trạng thái ───────────────────────────────────────────────────────
    private long nextRefreshUtcTicks;
    private int  cycleIndex;

    /// <summary>Bắn khi chu kỳ trôi qua — bảng tin phải sinh lại hàng.</summary>
    public event Action OnCycleElapsed;

    public MarketRefreshTimer(int cycleSeconds, int baseGoldCost, int maxGoldCost)
    {
        this.cycleSeconds = Mathf.Max(30, cycleSeconds);
        this.baseGoldCost = Mathf.Max(1, baseGoldCost);
        this.maxGoldCost  = Mathf.Max(this.baseGoldCost, maxGoldCost);

        Load();
    }

    /// <summary>Hạt ngẫu nhiên của chu kỳ hiện tại — cùng chu kỳ luôn ra cùng bảng hàng.</summary>
    public int CurrentCycleSeed => cycleIndex;

    public int CycleSeconds => cycleSeconds;

    /// <summary>Số giây còn lại. Không bao giờ âm.</summary>
    public float SecondsRemaining
    {
        get
        {
            long deltaTicks = nextRefreshUtcTicks - DateTimeOffset.UtcNow.UtcTicks;
            if (deltaTicks <= 0) return 0f;
            return (float)TimeSpan.FromTicks(deltaTicks).TotalSeconds;
        }
    }

    public float Progress01
    {
        get
        {
            if (cycleSeconds <= 0) return 0f;
            return Mathf.Clamp01(SecondsRemaining / cycleSeconds);
        }
    }

    public bool IsCycleFinished => SecondsRemaining <= 0f;

    /// <summary>Chuỗi "mm:ss" cho đồng hồ. Quá 1 giờ thì hiện "h:mm:ss".</summary>
    public string FormatRemaining()
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, SecondsRemaining));
        if (span.TotalHours >= 1d)
            return string.Format("{0}:{1:00}:{2:00}", (int)span.TotalHours, span.Minutes, span.Seconds);

        return string.Format("{0:00}:{1:00}", span.Minutes, span.Seconds);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CHU KỲ
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gọi mỗi frame khi popup mở. Trả true nếu VỪA có ít nhất một chu kỳ trôi qua.
    ///
    /// Bù nhiều chu kỳ một lúc chứ không cộng đúng một lần: người chơi tắt game 3 tiếng
    /// thì đã trôi 36 chu kỳ; cộng một lần sẽ khiến mốc hết hạn vẫn nằm trong quá khứ
    /// và hàm này bắn liên tục mỗi frame.
    /// </summary>
    public bool Tick()
    {
        long nowTicks = DateTimeOffset.UtcNow.UtcTicks;
        if (nowTicks < nextRefreshUtcTicks)
            return false;

        long cycleTicks = TimeSpan.FromSeconds(cycleSeconds).Ticks;
        if (cycleTicks <= 0)
            cycleTicks = TimeSpan.FromSeconds(300).Ticks;

        long overdue     = nowTicks - nextRefreshUtcTicks;
        long cyclesSkipped = overdue / cycleTicks + 1;

        // Chặn trần: máy chỉnh sai giờ có thể ra hàng tỉ chu kỳ, cycleIndex tràn int
        if (cyclesSkipped > 10000L) cyclesSkipped = 10000L;

        cycleIndex          = unchecked(cycleIndex + (int)cyclesSkipped);
        nextRefreshUtcTicks = nowTicks + cycleTicks - (overdue % cycleTicks);

        Save();
        OnCycleElapsed?.Invoke();
        return true;
    }

    /// <summary>Ép sang chu kỳ mới ngay (dùng cho nút làm mới trả vàng).</summary>
    public void ForceNewCycle()
    {
        cycleIndex          = unchecked(cycleIndex + 1);
        nextRefreshUtcTicks = DateTimeOffset.UtcNow.UtcTicks + TimeSpan.FromSeconds(cycleSeconds).Ticks;
        Save();
        OnCycleElapsed?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  GIÁ LÀM MỚI BẰNG VÀNG (A6)
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Số lần đã trả vàng làm mới trong ngày hôm nay.</summary>
    public int PaidRefreshCountToday
    {
        get
        {
            if (PlayerPrefs.GetString(KeyPaidDate, string.Empty) != TodayKey())
                return 0;
            return PlayerPrefs.GetInt(KeyPaidCount, 0);
        }
    }

    /// <summary>
    /// Giá làm mới NGAY bằng vàng cho lần bấm tiếp theo.
    /// Luỹ tiến tuyến tính rồi chạm trần — người chơi vẫn dùng được cả ngày,
    /// nhưng không thể spam để farm hàng hời với chi phí không đổi.
    /// </summary>
    public int GetGoldRefreshCost()
    {
        int cost = baseGoldCost * (PaidRefreshCountToday + 1);
        return Mathf.Min(cost, maxGoldCost);
    }

    /// <summary>
    /// true khi được làm mới MIỄN PHÍ — chỉ khi đồng hồ đã chạy hết.
    /// Bản cũ để RefreshNowFree bấm lúc nào cũng được, tức là nút trả gem hoàn toàn vô nghĩa.
    /// </summary>
    public bool CanRefreshFree() => IsCycleFinished;

    /// <summary>Ghi nhận một lần làm mới có trả vàng. Gọi SAU KHI đã trừ vàng thành công.</summary>
    public void RegisterPaidRefresh()
    {
        string today = TodayKey();
        int count = PlayerPrefs.GetString(KeyPaidDate, string.Empty) == today
            ? PlayerPrefs.GetInt(KeyPaidCount, 0)
            : 0;

        PlayerPrefs.SetString(KeyPaidDate, today);
        PlayerPrefs.SetInt(KeyPaidCount, count + 1);
        PlayerPrefs.Save();
    }

    private static string TodayKey() => DateTimeOffset.UtcNow.ToString("yyyyMMdd");

    // ══════════════════════════════════════════════════════════════════════
    //  LƯU / NẠP
    // ══════════════════════════════════════════════════════════════════════

    private void Load()
    {
        int savedVersion = PlayerPrefs.GetInt(KeyVersion, 0);

        if (savedVersion != CurrentSaveVersion)
        {
            // Đường chuyển đổi: chưa có bản cũ nào cần giữ (bản trước đếm bằng float
            // trong bộ nhớ, không lưu gì cả) nên chỉ cần dựng mới rồi đóng dấu version.
            MigrateFromLegacy(savedVersion);
            StartFreshCycle();
            PlayerPrefs.SetInt(KeyVersion, CurrentSaveVersion);
            PlayerPrefs.Save();
            return;
        }

        string ticksText = PlayerPrefs.GetString(KeyNextTicks, string.Empty);
        if (!long.TryParse(ticksText, out nextRefreshUtcTicks) || nextRefreshUtcTicks <= 0)
        {
            StartFreshCycle();
            return;
        }

        cycleIndex = PlayerPrefs.GetInt(KeyCycleIndex, 0);

        // Mốc nằm quá xa tương lai = đồng hồ máy từng bị vặn tới. Dựng lại cho lành.
        long maxAheadTicks = DateTimeOffset.UtcNow.UtcTicks + TimeSpan.FromSeconds(cycleSeconds * 4L).Ticks;
        if (nextRefreshUtcTicks > maxAheadTicks)
            StartFreshCycle();
    }

    /// <summary>
    /// Chỗ để xử lý dữ liệu của các phiên bản save cũ.
    /// Hiện chưa có gì để chuyển, nhưng phải để sẵn nhánh — lần sau tăng version
    /// mà không có chỗ móc vào thì lại đi xoá save của người chơi.
    /// </summary>
    private void MigrateFromLegacy(int fromVersion)
    {
        if (fromVersion <= 0)
        {
            // v0 = chưa từng lưu gì. Dọn khoá rác nếu có.
            PlayerPrefs.DeleteKey(KeyNextTicks);
            PlayerPrefs.DeleteKey(KeyCycleIndex);
        }
    }

    private void StartFreshCycle()
    {
        cycleIndex          = Mathf.Abs(Environment.TickCount) % 100000;
        nextRefreshUtcTicks = DateTimeOffset.UtcNow.UtcTicks + TimeSpan.FromSeconds(cycleSeconds).Ticks;
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KeyVersion, CurrentSaveVersion);
        PlayerPrefs.SetString(KeyNextTicks, nextRefreshUtcTicks.ToString());
        PlayerPrefs.SetInt(KeyCycleIndex, cycleIndex);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    /// <summary>Xoá sạch dữ liệu đồng hồ — dùng cho tool test, không gọi lúc chơi.</summary>
    public static void EditorClearSave()
    {
        PlayerPrefs.DeleteKey(KeyVersion);
        PlayerPrefs.DeleteKey(KeyNextTicks);
        PlayerPrefs.DeleteKey(KeyCycleIndex);
        PlayerPrefs.DeleteKey(KeyPaidCount);
        PlayerPrefs.DeleteKey(KeyPaidDate);
        PlayerPrefs.Save();
    }
#endif
}
