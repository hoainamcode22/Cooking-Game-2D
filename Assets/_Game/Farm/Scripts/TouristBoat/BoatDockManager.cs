using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Singleton trung tâm của hệ Bến Tàu Du Lịch — wrapper MỎNG quanh
/// <see cref="BoatScheduleCore"/> (mọi logic thời gian nằm bên đó, lớp này
/// chỉ lo phần dính Unity):
///
///   • Persist PlayerPrefs (key V1 giữ nguyên + key V2 mới, migrate nhẹ).
///   • Trừ tiền qua FarmEconomyManager.SpendGold/SpendGems (API tự từ chối nếu thiếu).
///   • Đọc level qua FarmLevelManager.Instance.CurrentLevel / HasReached.
///   • Quản lý 3 dock (berth, path, boat controller) + bắn event cho Dev B / Dev C.
///
/// ── V2 (BOAT-002, event-driven) ──────────────────────────────────────────
/// KHÔNG còn chu kỳ thời gian cố định. Mỗi bến là 1 máy trạng thái persist:
///   WaitingNext(arrivalUtc) → Arriving(travel) → Docked (VÔ HẠN, chờ lệnh)
///   → [Dev B gọi ReportVisitorsAllAboard] → Departing(travel) → WaitingNext(...)
///
/// Lịch chuyến kế (GDD V2 §3.2): gap = gapOneDockMinutes (5) nếu chỉ 1 bến mở,
/// gapMultiDockMinutes (10) nếu ≥2 bến; mọi cặp arrival bị ép cách nhau
/// ≥ minStaggerMinutes (3) bằng cách DỜI MUỘN.
///
/// Trạng thái tàu vẫn là hàm thuần của (state persist, mốc UTC, now) nên
/// reload scene / tắt mở game đều idempotent (GDD V2 §5 edge 1).
///
/// Hierarchy mong đợi (tool sinh — mọi chỗ tìm đều phòng thủ null):
///   BoatSystem (BoatDockManager)
///   ├─ BlindPoint
///   ├─ Dock_01..Dock_03 ── Berth · Path (WP_01..WP_n) · Boat (TouristBoatController)
/// </summary>
public class BoatDockManager : MonoBehaviour
{
    /// <summary>Số bến cố định của hệ thống (GDD §1: 3 bến).</summary>
    public const int DockCount = 3;

    public static BoatDockManager Instance { get; private set; }

    // ─── Inspector ──────────────────────────────────────────────────────

    [Header("Config (bắt buộc)")]
    [Tooltip("Asset TouristBoatConfig — mọi tuning knob của hệ boat")]
    [SerializeField] private TouristBoatConfig config;

    // ─── PlayerPrefs keys ───────────────────────────────────────────────
    // V1 (giữ nguyên pattern — KHÔNG đổi tên, save cũ vẫn đọc được):
    private const string KeyUnlockedFormat   = "TouristBoat_Unlocked_{0}";
    private const string KeyAnchorFormat     = "TouristBoat_AnchorUtc_{0}";   // anchor chu kỳ V1 (chỉ đọc để migrate)
    private const string KeyIntroDone        = "TouristBoat_IntroDone";
    // V2 (mới — version-safe, không đụng key V1):
    private const string KeyStateFormat      = "TouristBoat_V2_State_{0}";
    private const string KeyStateAnchorFormat= "TouristBoat_V2_Anchor_{0}";
    private const string KeyNextArrivalFormat= "TouristBoat_V2_NextArrival_{0}";
    private const string KeySchemaVersion    = "TouristBoat_ScheduleVersion";

    /// <summary>Phiên bản schema lịch tàu hiện tại (V1 = 1 / không có key, V2 = 2).</summary>
    private const int SchemaVersionV2 = 2;

    // ─── Runtime ────────────────────────────────────────────────────────

    private readonly bool[]              _unlocked      = new bool[DockCount];
    private readonly DockScheduleState[] _states        = new DockScheduleState[DockCount];
    private readonly float[]             _travelSeconds = new float[DockCount];

    // m-3 (quyết định lead giữ từ V1): travel dùng cho LỊCH = max travel của cả 3 bến.
    // Controller vẫn di chuyển theo path RIÊNG của bến, map theo progress 0-1 —
    // tàu có path ngắn trôi chậm hơn một chút (chấp nhận: tàu du lịch thong thả).
    private float _scheduleTravelSeconds;

    // Cache trạng thái frame trước — chỉ để phát hiện đổi state mà bắn
    // OnBoatStateChanged, KHÔNG phải nguồn sự thật (nguồn sự thật là _states).
    private readonly BoatState[] _lastStates = new BoatState[DockCount];

    // Arrival đã BÁO cho Dev C (OnNextTripScheduled) — chống bắn trùng cùng 1 chuyến.
    private readonly long[] _announcedArrival = new long[DockCount];

    // Docked resolve lúc load: hoãn bắn event tới khi Dev B kịp subscribe (xem FlushPendingDockedEvents).
    private readonly bool[] _pendingDockedEvent = new bool[DockCount];
    private int _readyFrame = -1;

    // Lưới an toàn chống kẹt tàu (QA B-1): đã báo OnDockTimeoutForced cho chuyến này chưa,
    // báo lúc mấy giờ (giây thực từ lúc chạy app), và chuyến này có bị ép rời không.
    private readonly bool[]  _timeoutNoticed        = new bool[DockCount];
    private readonly float[] _timeoutNoticeRealtime = new float[DockCount];
    private readonly bool[]  _departForcedByTimeout = new bool[DockCount];

    /// <summary>
    /// Cửa sổ ân hạn (GIÂY THỰC — cố ý KHÔNG chia debugTimeScale) giữa lúc bắn
    /// OnDockTimeoutForced và lúc manager tự ép tàu rời bến: đủ cho Dev B cho khách
    /// còn lại quay về tàu. Đây là hằng KỸ THUẬT, không phải tuning knob gameplay.
    /// </summary>
    private const float ForcedDepartGraceSeconds = 3f;

    private readonly Transform[]             _berths     = new Transform[DockCount];
    private readonly Transform[][]           _pathPoints = new Transform[DockCount][];
    private readonly TouristBoatController[] _boats      = new TouristBoatController[DockCount];

    // Buffer dựng sẵn cho phép so le — tránh alloc mỗi lần lên lịch.
    private readonly long[] _otherArrivalScratch = new long[DockCount];

    // Keys dựng sẵn 1 lần — tránh string.Format lặp lại mỗi lần save.
    private readonly string[] _keyUnlocked    = new string[DockCount];
    private readonly string[] _keyAnchorV1    = new string[DockCount];
    private readonly string[] _keyState       = new string[DockCount];
    private readonly string[] _keyStateAnchor = new string[DockCount];
    private readonly string[] _keyNextArrival = new string[DockCount];

    private Transform _blindPoint;
    private bool      _introDone;
    private bool      _allowDebugTime; // debugTimeScale chỉ ăn trong Editor/Dev build

    // ═════════════════════════════════════════════════════════════════════
    //  API CONTRACT (Dev B/C code song song dựa trên đúng chữ ký này)
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Config đang dùng — Dev B đọc visitors*/patience/queueSpacing..., UI đọc giá/level từ đây.</summary>
    public TouristBoatConfig Config => config;

    /// <summary>Bắn khi 1 bến vừa được mở khóa thành công (tham số: dockIndex 0-2).</summary>
    public event System.Action<int> OnDockUnlocked;

    /// <summary>Bắn khi tàu của 1 bến đổi trạng thái (dockIndex, state mới).</summary>
    public event System.Action<int, BoatState> OnBoatStateChanged;

    /// <summary>
    /// V2 — bắn ĐÚNG 1 LẦN khi tàu CHẠM BẾN (dockIndex), kể cả khi cú chạm bến đó
    /// được resolve lúc load (tắt game trong lúc tàu đang chạy vào / đang chờ).
    /// Dev B nghe event này để spawn chuyến khách mới.
    ///
    /// KHÔNG bắn lại cho chuyến đã chạm bến ở phiên chơi TRƯỚC (state save = Docked):
    /// chuyến đó Dev B tự khôi phục từ persistence riêng (TouristTrip_{dock}) — nếu
    /// bắn lại sẽ nhân đôi khách (GDD V2 §8.6). Boot xong cứ hỏi <see cref="IsDocked"/>.
    /// </summary>
    public event System.Action<int> OnBoatDocked;

    /// <summary>V2 — bắn khi tàu BẮT ĐẦU rời bến (dockIndex): gangplank rút, khách đã lên hết.</summary>
    public event System.Action<int> OnBoatDeparting;

    /// <summary>
    /// V2 — LƯỚI AN TOÀN (QA B-1): bến bị ép rời do đậu quá <c>maxDockMinutes</c>
    /// (mặc định 35 phút — LỚN HƠN mốc kiên nhẫn khách 30 phút để nhánh "khách giận
    /// tự về tàu" của Dev B vẫn chạy được, xem [QA M-7]). Bắn TRƯỚC khi tàu chuyển
    /// Departing, kèm CỬA SỔ ÂN HẠN vài giây để Dev B đuổi nốt khách còn lại về tàu.
    ///
    /// Dev B nên: nghe event này → cho mọi khách chưa xong đi thẳng về tàu (icon buồn,
    /// không thưởng) → gọi ReportVisitorsAllAboard như bình thường nếu kịp. Không gọi
    /// kịp cũng KHÔNG sao: hết ân hạn manager tự chuyển pha, hệ không bao giờ kẹt.
    /// </summary>
    public event System.Action<int> OnDockTimeoutForced;

    /// <summary>
    /// V2 — bắn khi chuyến KẾ của 1 bến được lên lịch:
    /// (dockIndex, arrivalUtc — DateTimeKind.Utc, số phút chờ đã làm tròn).
    /// Thời điểm bắn: tàu vừa rời bến · vừa mở bến · vào game thấy chuyến kế ở tương lai.
    /// Mỗi chuyến (mỗi mốc arrival) chỉ bắn 1 lần; Dev C tự persist key theo arrivalUtc
    /// để popup không hiện lại sau khi reload, và tự bỏ qua khi số phút &lt; 1.
    /// </summary>
    public event System.Action<int, DateTime, int> OnNextTripScheduled;

    /// <summary>V2 — tàu của bến này ĐANG ĐẬU (pha Docked) không? Index sai/bến chưa mở → false.</summary>
    public bool IsDocked(int dockIndex)
    {
        return IsValidDock(dockIndex) && _unlocked[dockIndex] && GetBoatState(dockIndex) == BoatState.Docked;
    }

    /// <summary>V2 — số bến ĐÃ MỞ (0-3). Quyết định gap 5 phút (1 bến) hay 10 phút (≥2 bến).</summary>
    public int UnlockedDockCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < DockCount; i++)
                if (_unlocked[i]) n++;
            return n;
        }
    }

    /// <summary>
    /// V2 — TouristVisitorManager (Dev B) gọi khi khách CUỐI đã lên tàu:
    /// Docked → Departing + lên lịch chuyến kế (gap theo số bến mở, ép so le ≥3 phút),
    /// persist, bắn OnBoatDeparting và OnNextTripScheduled.
    ///
    /// An toàn khi gọi sai/gọi trùng: bến chưa mở, index sai, hoặc tàu KHÔNG ở pha
    /// Docked → bỏ qua êm (log Debug), không đổi state, không bắn event lần hai.
    /// Gọi được ngay frame đầu sau load (khi Dev B resolve xong khách offline).
    /// </summary>
    public void ReportVisitorsAllAboard(int dockIndex)
    {
        if (!IsReady || config == null || !IsValidDock(dockIndex) || !_unlocked[dockIndex])
        {
            Debug.Log($"[TouristBoat] ReportVisitorsAllAboard({dockIndex}) bị bỏ qua: hệ chưa sẵn sàng hoặc bến chưa mở.");
            return;
        }

        long now = DateTime.UtcNow.Ticks;

        // Tua tới hiện tại trước đã (có thể vừa mới chạm bến trong frame này).
        ResolveDock(dockIndex, now, allowImmediateDockedEvent: true);

        if (_states[dockIndex].State != BoatState.Docked)
        {
            // Đã bị lưới an toàn ép rời bến trước đó → Dev B gọi sau cũng ĐÚNG luồng,
            // bỏ qua HOÀN TOÀN ÊM (không log, không phải lỗi của ai).
            if (_departForcedByTimeout[dockIndex]) return;

            // Guard chống double-fire: Dev B lỡ gọi 2 lần cho cùng 1 chuyến.
            Debug.Log($"[TouristBoat] ReportVisitorsAllAboard(bến {BoatNumber(dockIndex)}) bỏ qua — " +
                      $"tàu đang ở pha {_states[dockIndex].State}, không phải Docked.");
            return;
        }

        BeginDeparture(dockIndex, now, forcedByTimeout: false);
    }

    /// <summary>
    /// Lõi chuyển Docked → Departing + lên lịch chuyến kế + persist + bắn event.
    /// Dùng chung cho 2 đường vào: Dev B báo khách lên tàu hết (forcedByTimeout = false)
    /// và lưới an toàn ép rời do quá giờ (forcedByTimeout = true).
    /// Trả false nếu lõi từ chối (không ở pha Docked).
    /// </summary>
    private bool BeginDeparture(int dockIndex, long nowUtcTicks, bool forcedByTimeout)
    {
        DockScheduleState next;
        bool ok = BoatScheduleCore.TryBeginDeparture(
            _states[dockIndex], nowUtcTicks,
            EffectiveGapSeconds(), EffectiveTravelSeconds(), EffectiveStaggerSeconds(),
            BuildOtherArrivals(dockIndex, out int otherCount), otherCount,
            out next);

        if (!ok) return false;

        _states[dockIndex]              = next;
        _departForcedByTimeout[dockIndex] = forcedByTimeout;
        SaveDock(dockIndex);
        LuuGopPrefs.LuuNgay(); // mốc quan trọng (kết thúc 1 chuyến) — flush đĩa ngay

        Debug.Log($"[TouristBoat] Tàu số {BoatNumber(dockIndex):00} rời bến" +
                  (forcedByTimeout ? " (LƯỚI AN TOÀN ép rời do quá giờ đậu)" : "") + " — " +
                  $"chuyến kế cập bến lúc {new DateTime(next.NextArrivalUtcTicks, DateTimeKind.Utc):HH:mm:ss} UTC " +
                  $"(gap {EffectiveGapSeconds() / 60.0:0.#} phút thực).");

        RaiseStateChanged(dockIndex, BoatState.Departing);
        OnBoatDeparting?.Invoke(dockIndex);
        AnnounceNextTrip(dockIndex, nowUtcTicks);
        return true;
    }

    /// <summary>
    /// LƯỚI AN TOÀN CHỐNG KẸT TÀU (QA B-1 — Sếp duyệt 2026-08-29).
    /// Tàu đậu quá <c>maxDockMinutes</c> (UTC tuyệt đối, có chia debugTimeScale như
    /// mọi duration khác) thì:
    ///   • Lần đầu phát hiện: LogWarning + bắn <see cref="OnDockTimeoutForced"/> rồi
    ///     CHỜ cửa sổ ân hạn <see cref="ForcedDepartGraceSeconds"/> giây THỰC cho Dev B
    ///     đuổi khách còn lại về tàu (khách đi bộ cần thời gian thật, nên cửa sổ này
    ///     KHÔNG chia timeScale).
    ///   • Hết ân hạn mà vẫn còn Docked: manager TỰ chuyển Departing — chắc chắn không kẹt,
    ///     không phụ thuộc Dev B có gọi lại hay không.
    /// Dev B kịp gọi ReportVisitorsAllAboard trong lúc ân hạn → đi đường bình thường,
    /// cờ tự dọn vì state đã rời khỏi Docked.
    /// </summary>
    private void UpdateDockTimeout(int dockIndex, long nowUtcTicks)
    {
        // Chờ Dev B/C subscribe xong (giống FlushPendingDockedEvents) — bắn sớm hơn là rơi vào hư không.
        if (_readyFrame < 0 || Time.frameCount <= _readyFrame + 1) return;

        if (_states[dockIndex].State != BoatState.Docked)
        {
            _timeoutNoticed[dockIndex] = false; // sang pha khác → dọn cờ cho chuyến sau
            return;
        }

        double maxDock = EffectiveMaxDockSeconds();
        if (maxDock <= 0.0) return; // config đặt 0 = TẮT lưới an toàn (chỉ dùng khi debug)

        if (!BoatScheduleCore.IsDockTimedOut(_states[dockIndex], nowUtcTicks, maxDock))
            return;

        if (!_timeoutNoticed[dockIndex])
        {
            _timeoutNoticed[dockIndex]        = true;
            _timeoutNoticeRealtime[dockIndex] = Time.realtimeSinceStartup;
            Debug.LogWarning($"[TouristBoat] Tàu số {BoatNumber(dockIndex):00} đậu quá " +
                             $"{maxDock / 60.0:0.#} phút mà chưa có báo khách lên tàu — " +
                             $"báo Dev B dọn khách, {ForcedDepartGraceSeconds:0}s nữa sẽ ép rời bến.");
            OnDockTimeoutForced?.Invoke(dockIndex);
            return; // ân hạn
        }

        if (Time.realtimeSinceStartup - _timeoutNoticeRealtime[dockIndex] < ForcedDepartGraceSeconds)
            return; // vẫn trong cửa sổ ân hạn

        BeginDeparture(dockIndex, nowUtcTicks, forcedByTimeout: true);
    }

    /// <summary>Số hiệu tàu hiển thị cho người chơi: bến 0 → "Tàu số 01" (GDD V2 §3.1).</summary>
    public int BoatNumber(int dockIndex) => dockIndex + 1;

    /// <summary>Hội thoại intro (4 câu) đã chạy xong chưa — persist, chỉ chạy 1 lần.</summary>
    public bool IsIntroDone => _introDone;

    /// <summary>
    /// M-1: true SAU khi LoadFromPrefs xong trong Start — Dev B/C đợi cờ này trong
    /// BootRoutine trước khi đọc IsIntroDone/IsDockUnlocked/IsDocked và subscribe event
    /// (thứ tự Start giữa các MonoBehaviour không bảo đảm).
    /// </summary>
    public bool IsReady { get; private set; }

    // ─── Unity lifecycle ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Debug.isDebugBuild = true trong Editor và Development Build —
        // đúng phạm vi cho phép của debugTimeScale (GDD §7).
        _allowDebugTime = Application.isEditor || Debug.isDebugBuild;

        for (int i = 0; i < DockCount; i++)
        {
            _keyUnlocked[i]    = string.Format(KeyUnlockedFormat, i);
            _keyAnchorV1[i]    = string.Format(KeyAnchorFormat, i);
            _keyState[i]       = string.Format(KeyStateFormat, i);
            _keyStateAnchor[i] = string.Format(KeyStateAnchorFormat, i);
            _keyNextArrival[i] = string.Format(KeyNextArrivalFormat, i);

            _lastStates[i]     = BoatState.Locked;
            _states[i].State   = BoatState.Locked;
        }
    }

    private void Start()
    {
        if (config == null)
        {
            Debug.LogError("[TouristBoat] BoatDockManager: chưa gán TouristBoatConfig! " +
                           "Kéo asset config vào Inspector (hoặc chạy lại tool sinh BoatSystem). Hệ boat tắt.");
            return;
        }

        FindSceneReferences();
        LoadFromPrefs();

        IsReady     = true; // M-1: chỉ bật SAU khi LoadFromPrefs xong
        _readyFrame = Time.frameCount;

        Debug.Log($"[TouristBoat] Khởi tạo xong (V2 event-driven): introDone={_introDone}, " +
                  $"bến mở=[{(_unlocked[0] ? 1 : 0)},{(_unlocked[1] ? 1 : 0)},{(_unlocked[2] ? 1 : 0)}], " +
                  $"gap hiệu lực={EffectiveGapSeconds() / 60.0:0.#} phút, timeScale={EffectiveTimeScale():0.##}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsReady || config == null) return;

        long now = DateTime.UtcNow.Ticks;

        for (int i = 0; i < DockCount; i++)
        {
            if (!_unlocked[i]) continue;

            // Đồng hồ máy bị chỉnh lùi giữa phiên chơi (GDD V2 §5 edge 2): mọi mốc UTC
            // của bến vọt lên tương lai quá horizon (≈ 1 gap + dự phòng so le) →
            // reset về WaitingNext(now + 30s) thay vì kẹt tàu hàng giờ.
            if (BoatScheduleCore.IsScheduleImplausiblyFuture(_states[i], now, RollbackHorizonSeconds()))
            {
                ResetDockSchedule(i, now, "đồng hồ máy chỉnh lùi");
                continue;
            }

            ResolveDock(i, now, allowImmediateDockedEvent: true);
            UpdateDockTimeout(i, now); // lưới an toàn chống kẹt tàu (QA B-1)
            AnnounceNextTripIfPending(i, now);
        }

        FlushPendingDockedEvents();
    }

    // ─── API contract — mở khóa (giữ nguyên V1) ─────────────────────────

    /// <summary>Bến dockIndex (0-2) đã mở khóa chưa. Index sai → false.</summary>
    public bool IsDockUnlocked(int dockIndex)
    {
        return IsValidDock(dockIndex) && _unlocked[dockIndex];
    }

    /// <summary>
    /// Đánh dấu hội thoại intro đã chạy xong (persist) — UI gọi sau câu thứ 4.
    /// Đảm bảo hội thoại chỉ chạy đúng 1 lần kể cả nhảy cóc nhiều level.
    /// </summary>
    public void MarkIntroDone()
    {
        if (_introDone) return;
        _introDone = true;
        PlayerPrefs.SetInt(KeyIntroDone, 1);
        LuuGopPrefs.Hen();
        Debug.Log("[TouristBoat] Intro hoàn tất (persist).");
    }

    /// <summary>
    /// Kiểm tra đủ điều kiện mở bến chưa — KHÔNG trừ tiền, chỉ trả lý do để UI
    /// disable nút + hiện tooltip. reason rỗng khi trả true.
    /// </summary>
    public bool CanUnlockDock(int dockIndex, out string reason)
    {
        if (!IsValidDock(dockIndex) || config == null)
        {
            reason = "Bến không hợp lệ";
            return false;
        }

        // Chưa load xong PlayerPrefs (IsReady) mà cho mở thì có thể trừ tiền
        // trùng cho bến đã mở từ phiên trước — chặn lại cho chắc.
        if (!IsReady)
        {
            reason = "Hệ thống bến tàu chưa sẵn sàng";
            return false;
        }

        var req = config.GetDockRequirement(dockIndex);
        int  level = FarmLevelManager.Instance   != null ? FarmLevelManager.Instance.CurrentLevel : 0;
        long gold  = FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gold       : 0;
        long gems  = FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gems       : 0;

        UnlockDenyReason deny = BoatScheduleCore.EvaluateUnlock(req, _unlocked[dockIndex], level, gold, gems);
        switch (deny)
        {
            case UnlockDenyReason.None:            reason = string.Empty;                     return true;
            case UnlockDenyReason.AlreadyUnlocked: reason = "Bến đã mở khóa";                 return false;
            case UnlockDenyReason.LevelTooLow:     reason = $"Cần đạt Lv{req.RequiredLevel}"; return false;
            case UnlockDenyReason.NotEnoughGold:   reason = "Không đủ vàng";                  return false;
            case UnlockDenyReason.NotEnoughGems:   reason = "Không đủ gem";                   return false;
            default:                               reason = "Bến không hợp lệ";               return false;
        }
    }

    /// <summary>
    /// Mở bến trả phí (bến 2 vàng / bến 3 gem): kiểm điều kiện → trừ tiền →
    /// persist → tàu xuất phát ngay (tôn trọng luật so le §3.2).
    /// Trả false + log lý do nếu bị từ chối; KHÔNG trừ tiền khi thất bại.
    /// </summary>
    public bool TryUnlockDock(int dockIndex)
    {
        if (!CanUnlockDock(dockIndex, out string reason))
        {
            Debug.Log($"[TouristBoat] Từ chối mở bến {dockIndex + 1}: {reason}");
            return false;
        }

        var req = config.GetDockRequirement(dockIndex);

        // SpendGold/SpendGems tự từ chối nếu không đủ — vẫn re-check kết quả vì
        // số dư có thể đổi giữa CanUnlock và Spend (race với reward khác).
        if (req.GoldCost > 0)
        {
            if (FarmEconomyManager.Instance == null || !FarmEconomyManager.Instance.SpendGold(req.GoldCost))
            {
                Debug.Log($"[TouristBoat] Mở bến {dockIndex + 1} thất bại: không trừ được {req.GoldCost} vàng.");
                return false;
            }
        }
        if (req.GemCost > 0)
        {
            if (FarmEconomyManager.Instance == null || !FarmEconomyManager.Instance.SpendGems(req.GemCost))
            {
                // Hoàn vàng nếu lỡ trừ ở bước trên (hiện không có bến nào tốn cả 2 loại,
                // nhưng phòng thủ cho config tương lai — tiền người chơi không được bốc hơi).
                if (req.GoldCost > 0) FarmEconomyManager.Instance?.AddGold(req.GoldCost);
                Debug.Log($"[TouristBoat] Mở bến {dockIndex + 1} thất bại: không trừ được {req.GemCost} gem.");
                return false;
            }
        }

        UnlockInternal(dockIndex);
        return true;
    }

    /// <summary>
    /// Mở bến MIỄN PHÍ — dành cho bến 1 qua hội thoại intro.
    /// Không kiểm level/giá (flow intro đã tự kiểm HasReached trước khi chạy);
    /// idempotent: gọi trên bến đã mở thì bỏ qua êm.
    /// </summary>
    public void UnlockDockFree(int dockIndex)
    {
        if (!IsValidDock(dockIndex) || !IsReady) return;
        if (_unlocked[dockIndex]) return; // đã mở — bỏ qua, không reset lịch

        UnlockInternal(dockIndex);
    }

    // ─── API contract — trạng thái tàu ──────────────────────────────────

    /// <summary>
    /// Trạng thái tàu của bến dockIndex tại thời điểm gọi — suy trực tiếp từ
    /// state persist + UTC now, không cache. Bến chưa mở / index sai → Locked.
    /// </summary>
    public BoatState GetBoatState(int dockIndex)
    {
        BoatPhaseInfo info;
        return TryGetPhaseInfo(dockIndex, out info) ? info.State : BoatState.Locked;
    }

    /// <summary>
    /// [V1 API — GIỮ CHỮ KÝ] Giây còn lại của pha Docked.
    /// V2: pha Docked VÔ HẠN (tàu chờ khách xong) nên hàm này LUÔN trả -1;
    /// UI không được hiện countdown khi đậu nữa — hiện "Đang đón khách..." (GDD V2 §3.1).
    /// </summary>
    public float GetDockedRemainingSeconds(int dockIndex)
    {
        BoatPhaseInfo info;
        if (!TryGetPhaseInfo(dockIndex, out info)) return -1f;
        return info.State == BoatState.Docked ? (float)info.DockedRemainingSeconds : -1f;
    }

    /// <summary>Transform điểm đậu (Berth) của bến — cho camera zoom intro / Dev B đặt gangplank. Null nếu thiếu.</summary>
    public UnityEngine.Transform GetDockBerth(int dockIndex)
    {
        return IsValidDock(dockIndex) ? _berths[dockIndex] : null;
    }

    // ─── API nội bộ cho Controller / Dev B / Dev C (chỉ THÊM, không sửa V1) ───

    /// <summary>
    /// Pha đầy đủ của tàu (state + tiến độ 0-1) — controller gọi mỗi frame để đặt
    /// vị trí. Trả false (state Locked) nếu bến chưa mở / chưa sẵn sàng.
    /// Struct thuần, không alloc — an toàn trong Update.
    /// </summary>
    public bool TryGetPhaseInfo(int dockIndex, out BoatPhaseInfo info)
    {
        if (!IsReady || config == null || !IsValidDock(dockIndex) || !_unlocked[dockIndex])
        {
            info = default(BoatPhaseInfo);
            info.State = BoatState.Locked;
            return false;
        }

        // QueryPhase tự tua nội bộ nên hiển thị đúng ngay cả khi Update của manager
        // chưa chạy trong frame này (thứ tự script không bảo đảm).
        info = BoatScheduleCore.QueryPhase(_states[dockIndex], DateTime.UtcNow.Ticks, EffectiveTravelSeconds());
        return true;
    }

    /// <summary>
    /// V2 — giờ cập bến của chuyến SẮP TỚI của 1 bến (UTC). Trả false khi bến chưa
    /// mở hoặc tàu đang đậu (không có chuyến kế nào được lên lịch — đang chờ khách).
    /// Dev C dùng để dựng lại popup "Tàu số 0X sẽ cập bến sau X phút" sau reload.
    /// </summary>
    public bool TryGetNextArrivalUtc(int dockIndex, out DateTime arrivalUtc)
    {
        arrivalUtc = default(DateTime);
        if (!IsReady || !IsValidDock(dockIndex) || !_unlocked[dockIndex]) return false;

        long ticks = BoatScheduleCore.UpcomingArrivalUtcTicks(_states[dockIndex]);
        if (ticks <= 0L) return false;

        arrivalUtc = new DateTime(ticks, DateTimeKind.Utc);
        return true;
    }

    /// <summary>V2 — số phút chờ (đã làm tròn, theo thang thời gian game) tới chuyến kế; -1 nếu không có.</summary>
    public int GetMinutesToNextArrival(int dockIndex)
    {
        DateTime arrival;
        if (!TryGetNextArrivalUtc(dockIndex, out arrival)) return -1;
        return ScaledWaitMinutes(DateTime.UtcNow.Ticks, arrival.Ticks);
    }

    /// <summary>Điểm mù chung ngoài khơi (nơi tàu núp). Null nếu hierarchy thiếu.</summary>
    public Transform GetBlindPoint()
    {
        return _blindPoint;
    }

    /// <summary>
    /// Chuỗi điểm path đầy đủ của bến: [BlindPoint, WP_01..WP_n, Berth].
    /// Mảng cache dựng 1 lần lúc Start — CHỈ ĐỌC, không sửa phần tử.
    /// Null nếu bến/hierarchy thiếu.
    /// </summary>
    public Transform[] GetDockPathPoints(int dockIndex)
    {
        return IsValidDock(dockIndex) ? _pathPoints[dockIndex] : null;
    }

    /// <summary>
    /// Giây chạy 1 chiều tính từ độ dài path THỰC của bến (tham khảo/debug).
    /// LƯU Ý m-3: LỊCH của cả 3 bến chạy theo giá trị đồng nhất
    /// <see cref="GetScheduleTravelSeconds"/> (= max 3 bến), không phải giá trị này.
    /// </summary>
    public float GetTravelSeconds(int dockIndex)
    {
        return IsValidDock(dockIndex) ? _travelSeconds[dockIndex] : 0f;
    }

    /// <summary>Giây chạy 1 chiều ĐỒNG NHẤT dùng cho lịch của cả 3 bến (m-3).</summary>
    public float GetScheduleTravelSeconds()
    {
        return _scheduleTravelSeconds;
    }

    // ─── Nội bộ: mở khóa & lên lịch ─────────────────────────────────────

    /// <summary>
    /// Lõi mở bến (đã qua kiểm điều kiện/trừ tiền): đặt lịch sao cho tàu chạy vào
    /// NGAY (dopamine §3.6) — arrival = now + travel — rồi ép luật so le với arrival
    /// của các bến đã mở, persist, bắn event OnDockUnlocked + OnNextTripScheduled.
    /// </summary>
    private void UnlockInternal(int dockIndex)
    {
        long   now    = DateTime.UtcNow.Ticks;
        double travel = EffectiveTravelSeconds();

        long desiredArrival = now + BoatScheduleCore.SecondsToTicks(travel);

        _unlocked[dockIndex] = true; // bật TRƯỚC khi tính gap: bến này tính vào UnlockedDockCount

        long arrival = BoatScheduleCore.ResolveStaggeredArrival(
            desiredArrival, EffectiveStaggerSeconds(),
            BuildOtherArrivals(dockIndex, out int otherCount), otherCount);

        _states[dockIndex].State               = BoatState.WaitingNext;
        _states[dockIndex].AnchorUtcTicks      = arrival;
        _states[dockIndex].NextArrivalUtcTicks = 0L;
        _announcedArrival[dockIndex]           = 0L;

        SaveDock(dockIndex);
        PlayerPrefs.SetInt(KeySchemaVersion, SchemaVersionV2);
        LuuGopPrefs.LuuNgay(); // giao dịch quan trọng (có thể vừa trừ tiền) — flush đĩa ngay

        double delaySeconds = BoatScheduleCore.TicksToSeconds(arrival - desiredArrival);
        Debug.Log($"[TouristBoat] Mở bến {BoatNumber(dockIndex):00} thành công" +
                  (delaySeconds > 0.5 ? $" (so le: tàu vào bến trễ {delaySeconds:0}s)." : " — tàu xuất phát ngay."));

        OnDockUnlocked?.Invoke(dockIndex);

        // Tua ngay trong frame này để state/visual khớp (thường vào pha Arriving luôn).
        ResolveDock(dockIndex, DateTime.UtcNow.Ticks, allowImmediateDockedEvent: true);
        AnnounceNextTrip(dockIndex, now);
    }

    /// <summary>
    /// Tua máy trạng thái của 1 bến tới nowUtc, persist khi đổi, bắn
    /// OnBoatStateChanged, và xử lý cờ JustDocked (bắn ngay hay hoãn tới khi
    /// Dev B kịp subscribe — xem FlushPendingDockedEvents).
    /// </summary>
    private void ResolveDock(int dockIndex, long nowUtcTicks, bool allowImmediateDockedEvent)
    {
        DockResolveResult r = BoatScheduleCore.ResolveDock(
            _states[dockIndex], nowUtcTicks, EffectiveTravelSeconds());

        if (r.Changed)
        {
            _states[dockIndex] = r.State;
            SaveDock(dockIndex); // persist TRƯỚC khi bắn event → reload không bắn lại
        }

        if (r.State.State != _lastStates[dockIndex])
            RaiseStateChanged(dockIndex, r.State.State);

        if (!r.JustDocked) return;

        // Chuyến MỚI bắt đầu → dọn cờ lưới an toàn của chuyến trước.
        _timeoutNoticed[dockIndex]        = false;
        _departForcedByTimeout[dockIndex] = false;

        Debug.Log($"[TouristBoat] Tàu số {BoatNumber(dockIndex):00} đã cập bến — chờ khách được phục vụ xong.");

        // Chống double-fire: Docked là trạng thái hấp thụ và đã persist, nên
        // ResolveDock lần sau không thể ra JustDocked lần hai cho cùng chuyến.
        if (allowImmediateDockedEvent && Time.frameCount > _readyFrame + 1)
            OnBoatDocked?.Invoke(dockIndex);
        else
            _pendingDockedEvent[dockIndex] = true; // hoãn: Dev B chưa kịp subscribe
    }

    /// <summary>
    /// Bắn các OnBoatDocked bị hoãn trong 1-2 frame đầu sau IsReady.
    /// Lý do hoãn: Dev B/C subscribe trong coroutine "đợi IsReady", coroutine chạy
    /// SAU Update của frame đó — bắn ngay lúc load sẽ rơi vào hư không.
    /// </summary>
    private void FlushPendingDockedEvents()
    {
        if (_readyFrame < 0 || Time.frameCount <= _readyFrame + 1) return;

        for (int i = 0; i < DockCount; i++)
        {
            if (!_pendingDockedEvent[i]) continue;
            _pendingDockedEvent[i] = false;
            OnBoatDocked?.Invoke(i);
        }
    }

    /// <summary>Bắn OnBoatStateChanged + cập nhật cache state frame trước.</summary>
    private void RaiseStateChanged(int dockIndex, BoatState newState)
    {
        _lastStates[dockIndex] = newState;
        OnBoatStateChanged?.Invoke(dockIndex, newState);
    }

    /// <summary>
    /// Bắn OnNextTripScheduled cho arrival sắp tới của bến (nếu chưa báo chuyến này).
    /// Mỗi mốc arrival chỉ báo 1 lần trong 1 phiên chơi; Dev C persist thêm theo
    /// arrivalUtc để không báo lại sau reload.
    /// </summary>
    private void AnnounceNextTrip(int dockIndex, long nowUtcTicks)
    {
        long arrival = BoatScheduleCore.UpcomingArrivalUtcTicks(_states[dockIndex]);
        if (arrival <= 0L) return;
        if (_announcedArrival[dockIndex] == arrival) return; // đã báo chuyến này

        _announcedArrival[dockIndex] = arrival;
        OnNextTripScheduled?.Invoke(
            dockIndex,
            new DateTime(arrival, DateTimeKind.Utc),
            ScaledWaitMinutes(nowUtcTicks, arrival));
    }

    /// <summary>
    /// Trong Update: bến nào có arrival sắp tới mà chưa báo (vd vừa load game, hoặc
    /// vừa chuyển Departing → WaitingNext) thì báo — nhưng chỉ khi tàu CHƯA chạy vào
    /// (Arriving thì popup vô nghĩa, Dev C cũng lọc &lt;1 phút).
    /// </summary>
    private void AnnounceNextTripIfPending(int dockIndex, long nowUtcTicks)
    {
        if (_states[dockIndex].State == BoatState.Docked) return;
        AnnounceNextTrip(dockIndex, nowUtcTicks);
    }

    /// <summary>
    /// Gom arrival sắp tới của các bến KHÁC vào buffer dựng sẵn (không alloc)
    /// để lõi ép luật so le. Phần tử 0 = bến đó không có arrival sắp tới.
    /// </summary>
    private long[] BuildOtherArrivals(int dockIndex, out int count)
    {
        count = 0;
        for (int j = 0; j < DockCount; j++)
        {
            if (j == dockIndex || !_unlocked[j]) continue;
            _otherArrivalScratch[count++] = BoatScheduleCore.UpcomingArrivalUtcTicks(_states[j]);
        }
        return _otherArrivalScratch;
    }

    /// <summary>
    /// Reset lịch 1 bến về WaitingNext(now + 30s) — dùng khi phát hiện đồng hồ lùi
    /// hoặc dữ liệu prefs hỏng. Persist + báo chuyến mới cho Dev C.
    /// </summary>
    private void ResetDockSchedule(int dockIndex, long nowUtcTicks, string lyDo)
    {
        double scale = EffectiveTimeScale();
        _states[dockIndex] = BoatScheduleCore.MakeFreshWaiting(
            nowUtcTicks, BoatScheduleCore.FreshArrivalDelaySeconds / scale);
        _announcedArrival[dockIndex] = 0L;

        SaveDock(dockIndex);
        Debug.LogWarning($"[TouristBoat] Reset lịch bến {BoatNumber(dockIndex):00} ({lyDo}) — " +
                         $"tàu sẽ cập bến sau {BoatScheduleCore.FreshArrivalDelaySeconds / scale:0}s.");

        RaiseStateChanged(dockIndex, _states[dockIndex].State);
        AnnounceNextTrip(dockIndex, nowUtcTicks);
    }

    // ─── Nội bộ: các giá trị thời gian hiệu lực ─────────────────────────

    /// <summary>Hệ số tua thời gian hiệu lực: debugTimeScale chỉ ăn trong Editor/Dev build.</summary>
    private float EffectiveTimeScale()
    {
        if (!_allowDebugTime || config == null) return 1f;
        return Mathf.Max(0.01f, config.debugTimeScale);
    }

    /// <summary>
    /// Giây chạy 1 chiều dùng cho LỊCH, đã chia debugTimeScale (scale 60 →
    /// travel 20s thực rút còn 0.33s). Lõi V2 chỉ biết giây "đồng hồ thật".
    /// </summary>
    private double EffectiveTravelSeconds()
    {
        return Mathf.Max(0.01f, _scheduleTravelSeconds) / EffectiveTimeScale();
    }

    /// <summary>Gap hiệu lực (giây thực): 5 phút nếu 1 bến mở, 10 phút nếu ≥2 — đã chia timeScale.</summary>
    private double EffectiveGapSeconds()
    {
        if (config == null) return 300.0;
        double gap = BoatScheduleCore.SelectGapSeconds(
            UnlockedDockCount, config.GapOneDockSeconds, config.GapMultiDockSeconds);
        return gap / EffectiveTimeScale();
    }

    /// <summary>Khoảng so le tối thiểu hiệu lực (giây thực) — đã chia timeScale.</summary>
    private double EffectiveStaggerSeconds()
    {
        if (config == null) return 180.0;
        return config.MinStaggerSeconds / EffectiveTimeScale();
    }

    /// <summary>
    /// Giới hạn đậu bến hiệu lực (giây thực) của lưới an toàn — đã chia timeScale.
    /// 0 = tắt lưới (config maxDockMinutes = 0).
    /// </summary>
    private double EffectiveMaxDockSeconds()
    {
        if (config == null) return 1800.0;
        double max = config.MaxDockSeconds;
        return max <= 0.0 ? 0.0 : max / EffectiveTimeScale();
    }

    /// <summary>
    /// Horizon chống đồng hồ lùi: mốc UTC hợp lệ không bao giờ xa hơn
    /// 1 gap + (số bến × so le) + 2 travel + 60s dự phòng. Vượt qua = đồng hồ
    /// bị chỉnh lùi hoặc save hỏng (giữ tinh thần luật V1: "lùi quá 1 gap thì reset").
    /// </summary>
    private double RollbackHorizonSeconds()
    {
        return EffectiveGapSeconds()
             + EffectiveStaggerSeconds() * DockCount
             + EffectiveTravelSeconds() * 2.0
             + 60.0;
    }

    /// <summary>
    /// Số phút chờ hiển thị cho người chơi — quy đổi theo thang thời gian GAME
    /// (debugTimeScale 60: chờ 5 giây thực vẫn hiện "5 phút", đúng ngôn ngữ GDD).
    /// </summary>
    private int ScaledWaitMinutes(long nowUtcTicks, long arrivalUtcTicks)
    {
        double gameSeconds = BoatScheduleCore.TicksToSeconds(arrivalUtcTicks - nowUtcTicks) * EffectiveTimeScale();
        long scaledArrival = nowUtcTicks + BoatScheduleCore.SecondsToTicks(gameSeconds);
        return BoatScheduleCore.RoundedWaitMinutes(nowUtcTicks, scaledArrival);
    }

    private static bool IsValidDock(int dockIndex)
    {
        return dockIndex >= 0 && dockIndex < DockCount;
    }

#if UNITY_EDITOR
    // ─── API test (chỉ Editor) ──────────────────────────────────────────
    // V1 để tool chẩn đoán thọc reflection vào field private _anchorTicks — V2 KHÔNG
    // còn field đó (state là struct máy trạng thái), nên mở 2 cửa CHÍNH THỨC dưới đây
    // cho QA/tool. Không tồn tại trong bản build player.

    /// <summary>
    /// (Editor/QA) Ép tàu của bến CẬP BẾN NGAY lập tức — bỏ qua thời gian chờ.
    /// Bắn OnBoatDocked đúng 1 lần như cú cập bến thật (Dev B spawn khách luôn).
    /// </summary>
    public void EditorForceDockNow(int dockIndex)
    {
        if (!IsReady || !IsValidDock(dockIndex) || !_unlocked[dockIndex]) return;

        long now = DateTime.UtcNow.Ticks;
        _states[dockIndex] = BoatScheduleCore.MakeFreshWaiting(now, 0.0); // arrival = ngay bây giờ
        SaveDock(dockIndex);
        ResolveDock(dockIndex, now, allowImmediateDockedEvent: true);
        Debug.Log($"[TouristBoat] (Editor) Ép tàu số {BoatNumber(dockIndex):00} cập bến ngay.");
    }

    /// <summary>
    /// (Editor/QA) Ép tàu của bến RỜI BẾN ngay — tương đương Dev B báo khách lên tàu hết.
    /// Chỉ có tác dụng khi tàu đang đậu.
    /// </summary>
    public void EditorForceDepartNow(int dockIndex)
    {
        ReportVisitorsAllAboard(dockIndex);
    }

    /// <summary>
    /// (Editor/QA) Chuỗi mô tả ĐẦY ĐỦ state V2 của 1 bến cho tool chẩn đoán:
    /// pha hiện tại · mốc UTC · giờ đậu đã trôi so với lưới an toàn · chuyến kế lúc nào ·
    /// chuyến vừa rồi có bị ép rời do quá giờ không.
    /// </summary>
    public string EditorDescribeState(int dockIndex)
    {
        if (!IsValidDock(dockIndex)) return "(bến không hợp lệ)";
        if (!_unlocked[dockIndex])   return "Locked (bến chưa mở)";

        DockScheduleState s   = _states[dockIndex];
        long              now = DateTime.UtcNow.Ticks;
        var sb = new System.Text.StringBuilder();

        sb.Append(s.State == BoatState.WaitingNext ? "WaitingNext" : s.State.ToString());
        sb.Append(" · mốc=").Append(new DateTime(Math.Max(s.AnchorUtcTicks, 0L), DateTimeKind.Utc).ToString("HH:mm:ss")).Append(" UTC");

        if (s.State == BoatState.Docked)
        {
            double elapsed = BoatScheduleCore.DockedElapsedSeconds(s, now);
            double maxDock = EffectiveMaxDockSeconds();
            sb.Append(" · đã đậu ").Append(elapsed.ToString("0")).Append('s');
            sb.Append(maxDock > 0.0
                ? "/" + maxDock.ToString("0") + "s (lưới an toàn)"
                : " (lưới an toàn TẮT — maxDockMinutes = 0)");
            if (_timeoutNoticed[dockIndex])
                sb.Append(" · ĐÃ BÁO TIMEOUT, sắp bị ép rời");
        }

        long upcoming = BoatScheduleCore.UpcomingArrivalUtcTicks(s);
        sb.Append(" · chuyến kế=");
        if (upcoming > 0L)
        {
            double conLai = BoatScheduleCore.TicksToSeconds(upcoming - now);
            sb.Append(new DateTime(upcoming, DateTimeKind.Utc).ToString("HH:mm:ss")).Append(" UTC (còn ")
              .Append(Math.Max(0.0, conLai).ToString("0")).Append("s)");
        }
        else
        {
            sb.Append("chưa lên lịch (đang đón khách)");
        }

        if (_departForcedByTimeout[dockIndex])
            sb.Append(" · chuyến vừa rồi BỊ ÉP RỜI do quá giờ đậu");

        return sb.ToString();
    }

    /// <summary>(Editor/QA) Chuyến đang chạy của bến này có bị lưới an toàn ép rời không.</summary>
    public bool EditorIsDepartForcedByTimeout(int dockIndex)
        => IsValidDock(dockIndex) && _departForcedByTimeout[dockIndex];

    /// <summary>(Editor/QA) Số giây tàu đã đậu ở bến (0 nếu không ở pha Docked).</summary>
    public double EditorDockedElapsedSeconds(int dockIndex)
        => IsValidDock(dockIndex)
            ? BoatScheduleCore.DockedElapsedSeconds(_states[dockIndex], DateTime.UtcNow.Ticks)
            : 0.0;

    /// <summary>(Editor/QA) Giới hạn đậu bến hiệu lực (giây thực, đã chia debugTimeScale); 0 = tắt lưới.</summary>
    public double EditorMaxDockSeconds() => EffectiveMaxDockSeconds();
#endif

    // ─── Nội bộ: persist ────────────────────────────────────────────────

    /// <summary>
    /// Load cờ unlock + máy trạng thái V2 từ PlayerPrefs.
    /// Migrate nhẹ từ save V1: chưa có key V2 nào mà bến đã mở (hoặc còn anchor V1)
    /// → coi như WaitingNext với arrival = now + 30s (tàu vào ngay lần đầu, GDD V2).
    /// Load vào giữa pha Docked thì GIỮ Docked — Dev B tự resolve khách rồi gọi
    /// ReportVisitorsAllAboard (có thể ngay frame đầu).
    /// </summary>
    private void LoadFromPrefs()
    {
        long   now   = DateTime.UtcNow.Ticks;
        double scale = EffectiveTimeScale();

        _introDone = PlayerPrefs.GetInt(KeyIntroDone, 0) == 1;

        int schema = PlayerPrefs.GetInt(KeySchemaVersion, 1);
        int migrated = 0;

        for (int i = 0; i < DockCount; i++)
        {
            _unlocked[i] = PlayerPrefs.GetInt(_keyUnlocked[i], 0) == 1;

            if (!_unlocked[i])
            {
                _states[i].State               = BoatState.Locked;
                _states[i].AnchorUtcTicks      = 0L;
                _states[i].NextArrivalUtcTicks = 0L;
                _lastStates[i]                 = BoatState.Locked;
                continue;
            }

            bool hasV2 = PlayerPrefs.HasKey(_keyState[i]);
            if (!hasV2)
            {
                // ── Migrate V1 → V2 ──────────────────────────────────────
                // Save V1 chỉ có anchor chu kỳ; pha cũ không map 1-1 sang máy trạng
                // thái mới nên chốt luật đơn giản & vui: tàu vào bến ngay lần đầu.
                _states[i] = BoatScheduleCore.MakeFreshWaiting(
                    now, BoatScheduleCore.FreshArrivalDelaySeconds / scale);
                SaveDock(i);
                migrated++;
            }
            else
            {
                int  rawState = PlayerPrefs.GetInt(_keyState[i], (int)BoatState.WaitingNext);
                long anchor   = ReadLong(_keyStateAnchor[i], 0L);
                long next     = ReadLong(_keyNextArrival[i], 0L);

                _states[i].State               = ToKnownState(rawState);
                _states[i].AnchorUtcTicks      = anchor;
                _states[i].NextArrivalUtcTicks = next;

                // Dữ liệu hỏng (anchor 0/âm) → coi như chuyến mới.
                if (anchor <= 0L)
                {
                    _states[i] = BoatScheduleCore.MakeFreshWaiting(
                        now, BoatScheduleCore.FreshArrivalDelaySeconds / scale);
                    SaveDock(i);
                    Debug.LogWarning($"[TouristBoat] Bến {BoatNumber(i):00}: dữ liệu lịch hỏng — đặt lại chuyến mới.");
                }
                // Đồng hồ lùi khi đang tắt game (GDD V2 §5 edge 2).
                else if (BoatScheduleCore.IsScheduleImplausiblyFuture(_states[i], now, RollbackHorizonSeconds()))
                {
                    ResetDockSchedule(i, now, "đồng hồ máy chỉnh lùi (phát hiện lúc load)");
                }
            }

            // Tua offline: Departing/Arriving đã xong từ đời nào → về đúng pha hiện tại.
            // JustDocked ở đây sẽ được HOÃN (frame đầu) rồi bắn 1 lần khi Dev B đã subscribe.
            ResolveDock(i, now, allowImmediateDockedEvent: false);
            _lastStates[i] = _states[i].State;
        }

        PlayerPrefs.SetInt(KeySchemaVersion, SchemaVersionV2);
        if (migrated > 0)
        {
            Debug.Log($"[TouristBoat] Migrate lịch V1 → V2 cho {migrated} bến (schema cũ = {schema}) — " +
                      "tàu sẽ cập bến sau ~30 giây.");
        }
        LuuGopPrefs.Hen();
    }

    /// <summary>Ép giá trị int đọc từ prefs về BoatState hợp lệ (dữ liệu lạ → WaitingNext).</summary>
    private static BoatState ToKnownState(int raw)
    {
        switch (raw)
        {
            case (int)BoatState.Locked:      return BoatState.Locked;
            case (int)BoatState.WaitingNext: return BoatState.WaitingNext; // == Hidden (V1)
            case (int)BoatState.Arriving:    return BoatState.Arriving;
            case (int)BoatState.Docked:      return BoatState.Docked;
            case (int)BoatState.Departing:   return BoatState.Departing;
            default:                         return BoatState.WaitingNext;
        }
    }

    /// <summary>PlayerPrefs không có GetLong — ticks lưu dạng string invariant (pattern V1).</summary>
    private static long ReadLong(string key, long fallback)
    {
        string raw = PlayerPrefs.GetString(key, string.Empty);
        long value;
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    /// <summary>Ghi cờ unlock + toàn bộ máy trạng thái V2 của 1 bến (lưu gộp có trễ).</summary>
    private void SaveDock(int dockIndex)
    {
        PlayerPrefs.SetInt(_keyUnlocked[dockIndex], _unlocked[dockIndex] ? 1 : 0);
        PlayerPrefs.SetInt(_keyState[dockIndex], (int)_states[dockIndex].State);
        PlayerPrefs.SetString(_keyStateAnchor[dockIndex],
            _states[dockIndex].AnchorUtcTicks.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(_keyNextArrival[dockIndex],
            _states[dockIndex].NextArrivalUtcTicks.ToString(CultureInfo.InvariantCulture));
        LuuGopPrefs.Hen(); // lưu gộp có trễ — xem LuuGopPrefs
    }

    // ─── Nội bộ: tìm reference trong scene ──────────────────────────────

    /// <summary>
    /// Tìm BlindPoint / Dock_01..03 / Berth / Path / Boat theo cấu trúc hierarchy
    /// tool sinh ra. Mọi mảnh thiếu chỉ LogWarning + fallback, không NRE
    /// (game vẫn chạy khi chưa gắn đủ art/waypoint).
    /// </summary>
    private void FindSceneReferences()
    {
        _blindPoint = transform.Find("BlindPoint");
        if (_blindPoint == null)
            Debug.LogWarning("[TouristBoat] Thiếu con 'BlindPoint' dưới BoatSystem — tàu sẽ dùng waypoint đầu tiên làm điểm mù.");

        for (int i = 0; i < DockCount; i++)
        {
            Transform dock = transform.Find($"Dock_{i + 1:00}");
            if (dock == null)
            {
                Debug.LogWarning($"[TouristBoat] Thiếu 'Dock_{i + 1:00}' — bến {i + 1} không có visual (logic thời gian vẫn chạy).");
                _travelSeconds[i] = config.fallbackTravelSeconds;
                continue;
            }

            _berths[i] = dock.Find("Berth");
            if (_berths[i] == null)
                Debug.LogWarning($"[TouristBoat] Dock_{i + 1:00} thiếu con 'Berth'.");

            // Boat controller: ưu tiên con tên "Boat", fallback quét trong dock.
            Transform boatT = dock.Find("Boat");
            _boats[i] = boatT != null
                ? boatT.GetComponent<TouristBoatController>()
                : dock.GetComponentInChildren<TouristBoatController>(true);
            if (_boats[i] == null)
                Debug.LogWarning($"[TouristBoat] Dock_{i + 1:00} thiếu Boat/TouristBoatController.");

            _pathPoints[i]    = BuildPathPoints(dock, i);
            _travelSeconds[i] = ComputeTravelSeconds(_pathPoints[i]);
        }

        // m-3: travel cho LỊCH = max của 3 bến → 3 bến chung một travel, luật so le
        // tính trên arrival tuyệt đối nên vẫn đúng vĩnh viễn.
        _scheduleTravelSeconds = Mathf.Max(_travelSeconds[0],
                                 Mathf.Max(_travelSeconds[1], _travelSeconds[2]));
        if (_scheduleTravelSeconds <= 0.01f)
            _scheduleTravelSeconds = config.fallbackTravelSeconds;
    }

    /// <summary>
    /// Dựng chuỗi điểm path đầy đủ [BlindPoint, WP_01..WP_n, Berth] cho 1 bến.
    /// Waypoint là con của "Path", sắp theo tên (WP_01, WP_02, ... — tên có số 0 đệm
    /// nên so sánh chuỗi thường là đủ đúng thứ tự).
    /// </summary>
    private Transform[] BuildPathPoints(Transform dock, int dockIndex)
    {
        Transform pathRoot = dock.Find("Path");
        int wpCount = pathRoot != null ? pathRoot.childCount : 0;

        Transform[] wps = new Transform[wpCount];
        for (int c = 0; c < wpCount; c++)
            wps[c] = pathRoot.GetChild(c);
        System.Array.Sort(wps, (a, b) => string.CompareOrdinal(a.name, b.name));

        int total = (_blindPoint != null ? 1 : 0) + wpCount + (_berths[dockIndex] != null ? 1 : 0);
        if (total < 2)
        {
            Debug.LogWarning($"[TouristBoat] Dock_{dockIndex + 1:00}: path < 2 điểm — dùng fallbackTravelSeconds.");
            return null;
        }

        Transform[] points = new Transform[total];
        int n = 0;
        if (_blindPoint != null) points[n++] = _blindPoint;
        for (int c = 0; c < wpCount; c++) points[n++] = wps[c];
        if (_berths[dockIndex] != null) points[n++] = _berths[dockIndex];
        return points;
    }

    /// <summary>travelTime = độ dài polyline thực / boatSpeed. Path hỏng → fallback.</summary>
    private float ComputeTravelSeconds(Transform[] points)
    {
        if (points == null || points.Length < 2)
            return config.fallbackTravelSeconds;

        float length = 0f;
        for (int c = 1; c < points.Length; c++)
        {
            if (points[c] == null || points[c - 1] == null) continue;
            length += Vector3.Distance(points[c - 1].position, points[c].position);
        }

        if (length <= 0.01f)
            return config.fallbackTravelSeconds;

        return length / Mathf.Max(1f, config.boatSpeed);
    }
}
