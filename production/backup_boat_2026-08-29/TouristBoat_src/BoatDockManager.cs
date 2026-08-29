using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Singleton trung tâm của hệ Bến Tàu Du Lịch — wrapper MỎNG quanh
/// <see cref="BoatScheduleCore"/> (mọi logic thời gian nằm bên đó, lớp này
/// chỉ lo phần dính Unity):
///
///   • Persist PlayerPrefs (keys theo GDD §3.4) + chống đồng hồ lùi.
///   • Trừ tiền qua FarmEconomyManager.SpendGold/SpendGems (API tự từ chối nếu thiếu).
///   • Đọc level qua FarmLevelManager.Instance.CurrentLevel / HasReached.
///   • Quản lý 3 dock (berth, path, boat controller) + bắn event cho UI (Dev B).
///
/// Trạng thái tàu KHÔNG lưu biến riêng — mỗi lần hỏi đều suy từ anchor + UTC now
/// (xem BoatScheduleCore.ComputePhase) nên reload scene / tắt mở game đều
/// idempotent, tàu luôn đúng pha (GDD §5 edge 2, 7).
///
/// Hierarchy mong đợi (tool của Dev B sinh — mọi chỗ tìm đều phòng thủ null):
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

    // ─── PlayerPrefs keys (GDD §3.4) ────────────────────────────────────

    private const string KeyUnlockedFormat = "TouristBoat_Unlocked_{0}";
    private const string KeyAnchorFormat   = "TouristBoat_AnchorUtc_{0}";
    private const string KeyIntroDone      = "TouristBoat_IntroDone";

    // ─── Runtime ────────────────────────────────────────────────────────

    private readonly bool[]  _unlocked      = new bool[DockCount];
    private readonly long[]  _anchorTicks   = new long[DockCount];
    private readonly float[] _travelSeconds = new float[DockCount];

    // m-3 (quyết định lead): travel dùng cho LỊCH = max travel của cả 3 bến.
    // Cả 3 bến chung một cycleDuration → hai chu kỳ bất kỳ không bao giờ lệch pha
    // dần theo thời gian → luật so le đúng VĨNH VIỄN (AC §8.4), không chỉ tại lúc
    // mở bến. Controller vẫn di chuyển theo path RIÊNG của bến, map theo progress
    // 0-1 — tàu có path ngắn trôi chậm hơn một chút (chấp nhận: tàu du lịch thong thả).
    private float _scheduleTravelSeconds;

    // Cache trạng thái frame trước — chỉ để phát hiện đổi state mà bắn event,
    // KHÔNG phải nguồn sự thật (nguồn sự thật là anchor + now).
    private readonly BoatState[] _lastStates = new BoatState[DockCount];

    private readonly Transform[]             _berths     = new Transform[DockCount];
    private readonly Transform[][]           _pathPoints = new Transform[DockCount][];
    private readonly TouristBoatController[] _boats      = new TouristBoatController[DockCount];

    // Buffer dựng sẵn cho phép giải so le — tránh alloc lúc mở bến.
    private readonly BoatCycleSpec[] _staggerScratch = new BoatCycleSpec[DockCount];

    // Keys dựng sẵn 1 lần — tránh string.Format lặp lại mỗi lần save.
    private readonly string[] _keyUnlocked = new string[DockCount];
    private readonly string[] _keyAnchor   = new string[DockCount];

    private Transform _blindPoint;
    private bool      _introDone;
    private bool      _allowDebugTime; // debugTimeScale chỉ ăn trong Editor/Dev build

    // ─── API contract (Dev B code song song dựa trên đúng chữ ký này) ───

    /// <summary>Config đang dùng — UI đọc giá/level/hội thoại từ đây.</summary>
    public TouristBoatConfig Config => config;

    /// <summary>Bắn khi 1 bến vừa được mở khóa thành công (tham số: dockIndex 0-2).</summary>
    public event System.Action<int> OnDockUnlocked;

    /// <summary>Bắn khi tàu của 1 bến đổi trạng thái (dockIndex, state mới).</summary>
    public event System.Action<int, BoatState> OnBoatStateChanged;

    /// <summary>Hội thoại intro (4 câu, GDD §3.1) đã chạy xong chưa — persist, chỉ chạy 1 lần.</summary>
    public bool IsIntroDone => _introDone;

    /// <summary>
    /// M-1: true SAU khi LoadFromPrefs xong trong Start — Dev B đợi cờ này trong
    /// BootRoutine trước khi đọc IsIntroDone/IsDockUnlocked (thứ tự Start giữa các
    /// MonoBehaviour không bảo đảm; đọc sớm sẽ thấy toàn giá trị mặc định → replay intro).
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
            _keyUnlocked[i] = string.Format(KeyUnlockedFormat, i);
            _keyAnchor[i]   = string.Format(KeyAnchorFormat, i);
            _lastStates[i]  = BoatState.Locked;
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

        IsReady = true; // M-1: chỉ bật SAU khi LoadFromPrefs xong — Dev B đợi cờ này
        Debug.Log($"[TouristBoat] Khởi tạo xong: introDone={_introDone}, " +
                  $"bến mở=[{(_unlocked[0] ? 1 : 0)},{(_unlocked[1] ? 1 : 0)},{(_unlocked[2] ? 1 : 0)}], " +
                  $"timeScale={EffectiveTimeScale():0.##}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsReady) return;

        long   now   = DateTime.UtcNow.Ticks;
        double cycle = BoatScheduleCore.ComputeCycleSeconds(
            config.DockSeconds, config.HideSeconds, _scheduleTravelSeconds);

        for (int i = 0; i < DockCount; i++)
        {
            if (!_unlocked[i]) continue;

            // [QA B-1] Đồng hồ máy bị chỉnh lùi giữa phiên chơi: reset anchor = now
            // (GDD §5 edge 4). Guard có DUNG SAI 1 chu kỳ: anchor nằm ở tương lai
            // TRONG phạm vi 1 cycle là HỢP LỆ — luật so le vừa đẩy anchor vượt
            // hideMinutes khi mở bến lúc bến khác đang hoạt động; reset ở đây sẽ
            // phá so le (QA đo gap 8' < stagger 12'). ComputePhase tự xử lý anchor
            // tương lai đúng nghĩa (kẹp về Hidden chờ tới lượt).
            if (BoatScheduleCore.IsClockRolledBack(now, _anchorTicks[i], cycle))
            {
                _anchorTicks[i] = now;
                SaveDock(i);
                Debug.LogWarning($"[TouristBoat] Phát hiện đồng hồ máy chỉnh lùi — reset anchor bến {i + 1}.");
            }

            RefreshDockState(i, now);
        }
    }

    // ─── API contract — mở khóa ─────────────────────────────────────────

    /// <summary>Bến dockIndex (0-2) đã mở khóa chưa. Index sai → false.</summary>
    public bool IsDockUnlocked(int dockIndex)
    {
        return IsValidDock(dockIndex) && _unlocked[dockIndex];
    }

    /// <summary>
    /// Đánh dấu hội thoại intro đã chạy xong (persist) — UI gọi sau câu thứ 4.
    /// Đảm bảo hội thoại chỉ chạy đúng 1 lần kể cả nhảy cóc nhiều level (GDD §5 edge 1).
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
    /// disable nút + hiện tooltip (GDD §5 edge 5). reason rỗng khi trả true.
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
            case UnlockDenyReason.None:            reason = string.Empty;                 return true;
            case UnlockDenyReason.AlreadyUnlocked: reason = "Bến đã mở khóa";             return false;
            case UnlockDenyReason.LevelTooLow:     reason = $"Cần đạt Lv{req.RequiredLevel}"; return false;
            case UnlockDenyReason.NotEnoughGold:   reason = "Không đủ vàng";              return false;
            case UnlockDenyReason.NotEnoughGems:   reason = "Không đủ gem";               return false;
            default:                               reason = "Bến không hợp lệ";           return false;
        }
    }

    /// <summary>
    /// Mở bến trả phí (bến 2 vàng / bến 3 gem): kiểm điều kiện → trừ tiền →
    /// persist → dispatch tàu ngay (tôn trọng luật so le §3.3).
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

        // SpendGold/SpendGems tự từ chối nếu không đủ (GDD §3.1) — vẫn re-check
        // kết quả vì số dư có thể đổi giữa CanUnlock và Spend (race với reward khác).
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
    /// Mở bến MIỄN PHÍ — dành cho bến 1 qua hội thoại intro (GDD §3.1 bước 3).
    /// Không kiểm level/giá (flow intro đã tự kiểm HasReached trước khi chạy);
    /// idempotent: gọi trên bến đã mở thì bỏ qua êm.
    /// </summary>
    public void UnlockDockFree(int dockIndex)
    {
        if (!IsValidDock(dockIndex) || !IsReady) return;
        if (_unlocked[dockIndex]) return; // đã mở — bỏ qua, không reset anchor

        UnlockInternal(dockIndex);
    }

    // ─── API contract — trạng thái tàu ──────────────────────────────────

    /// <summary>
    /// Trạng thái tàu của bến dockIndex tại thời điểm gọi — suy trực tiếp từ
    /// anchor + UTC now, không cache. Bến chưa mở / index sai → Locked.
    /// </summary>
    public BoatState GetBoatState(int dockIndex)
    {
        BoatPhaseInfo info;
        return TryGetPhaseInfo(dockIndex, out info) ? info.State : BoatState.Locked;
    }

    /// <summary>
    /// Giây còn lại của pha Docked (cho countdown UI). Trả -1 nếu tàu không
    /// đang Docked (thoả contract "&lt;=0 nếu không Docked").
    /// Lưu ý: khi debugTimeScale &gt; 1 thì đây là "giây game" (đếm nhanh hơn thực).
    /// </summary>
    public float GetDockedRemainingSeconds(int dockIndex)
    {
        BoatPhaseInfo info;
        if (!TryGetPhaseInfo(dockIndex, out info)) return -1f;
        return info.State == BoatState.Docked ? (float)info.DockedRemainingSeconds : -1f;
    }

    /// <summary>Transform điểm đậu (Berth) của bến — cho camera zoom intro. Null nếu thiếu.</summary>
    public UnityEngine.Transform GetDockBerth(int dockIndex)
    {
        return IsValidDock(dockIndex) ? _berths[dockIndex] : null;
    }

    // ─── API nội bộ cho TouristBoatController (ngoài contract, chỉ thêm không sửa) ───

    /// <summary>
    /// Pha đầy đủ của tàu (state + tiến độ 0-1 + countdown) — controller gọi mỗi
    /// frame để đặt vị trí. Trả false (state Locked) nếu bến chưa mở / chưa sẵn sàng.
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

        // [QA B-1] Truyền anchor NGUYÊN VẸN, không SanitizeAnchor ở đây: anchor
        // tương lai (≤ 1 cycle) là hợp lệ do so le đẩy — ComputePhase tự kẹp
        // elapsed âm về 0 (Hidden chờ tới lượt). Đồng hồ lùi thật do Update() xử lý
        // qua IsClockRolledBack (dung sai 1 cycle).
        // m-3: dùng _scheduleTravelSeconds đồng nhất — cả 3 bến chung chu kỳ.
        info = BoatScheduleCore.ComputePhase(
            DateTime.UtcNow.Ticks,
            _anchorTicks[dockIndex],
            config.DockSeconds, config.HideSeconds, _scheduleTravelSeconds,
            EffectiveTimeScale());
        return true;
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

    /// <summary>Giây chạy 1 chiều ĐỒNG NHẤT dùng cho lịch/chu kỳ của cả 3 bến (m-3).</summary>
    public float GetScheduleTravelSeconds()
    {
        return _scheduleTravelSeconds;
    }

    // ─── Nội bộ: mở khóa & dispatch ─────────────────────────────────────

    /// <summary>
    /// Lõi mở bến (đã qua kiểm điều kiện/trừ tiền): đặt anchor sao cho tàu
    /// Arriving NGAY (dopamine §3.1) — anchor = now - hide, tức phase vừa chạm
    /// mốc Arriving — rồi giải luật so le với các bến đã mở (§3.3), persist,
    /// bắn event. Nếu bị so le đẩy lùi, tàu sẽ Hidden thêm đúng phần thiếu.
    /// </summary>
    private void UnlockInternal(int dockIndex)
    {
        long   now   = DateTime.UtcNow.Ticks;
        double scale = EffectiveTimeScale();

        // Chia scale để "tàu vào ngay" vẫn đúng khi đang tua nhanh thời gian debug
        // (phase = elapsed * scale — muốn phase = hide thì elapsed = hide/scale).
        long desiredAnchor = now - BoatScheduleCore.SecondsToTicks(config.HideSeconds / scale);

        int otherCount = 0;
        for (int j = 0; j < DockCount; j++)
        {
            if (j == dockIndex || !_unlocked[j]) continue;
            _staggerScratch[otherCount].AnchorUtcTicks = _anchorTicks[j];
            _staggerScratch[otherCount].HideSeconds    = config.HideSeconds;
            _staggerScratch[otherCount].DockSeconds    = config.DockSeconds;
            _staggerScratch[otherCount].TravelSeconds  = _scheduleTravelSeconds; // m-3: chu kỳ đồng nhất
            otherCount++;
        }

        // Lưu ý debug: phép so le tính trong thang thời gian THỰC (scale = 1).
        // Khi debugTimeScale > 1 khoảng so le quan sát được sẽ ngắn lại tương ứng —
        // chấp nhận được vì knob này chỉ để tua nhanh lúc test, release luôn scale 1.
        // m-3: mọi bến dùng chung _scheduleTravelSeconds → mọi chu kỳ bằng nhau →
        // khoảng so le giải ở đây giữ nguyên VĨNH VIỄN, không trôi dần theo thời gian.
        long anchor = BoatScheduleCore.ResolveStaggeredAnchor(
            desiredAnchor,
            config.DockSeconds, config.HideSeconds, _scheduleTravelSeconds,
            config.StaggerSeconds,
            _staggerScratch, otherCount);

        _unlocked[dockIndex]    = true;
        _anchorTicks[dockIndex] = anchor;

        SaveDock(dockIndex);
        LuuGopPrefs.LuuNgay(); // giao dịch quan trọng (có thể vừa trừ tiền) — flush đĩa ngay

        double delaySeconds = BoatScheduleCore.TicksToSeconds(anchor - desiredAnchor);
        Debug.Log($"[TouristBoat] Mở bến {dockIndex + 1} thành công" +
                  (delaySeconds > 0.5 ? $" (so le: tàu xuất phát trễ {delaySeconds:0}s)." : " — tàu xuất phát ngay."));

        OnDockUnlocked?.Invoke(dockIndex);
        RefreshDockState(dockIndex, DateTime.UtcNow.Ticks); // bắn state mới ngay trong frame này
    }

    /// <summary>So sánh state hiện tại với frame trước, đổi thì bắn OnBoatStateChanged.</summary>
    private void RefreshDockState(int dockIndex, long nowUtcTicks)
    {
        BoatPhaseInfo info = BoatScheduleCore.ComputePhase(
            nowUtcTicks, _anchorTicks[dockIndex],
            config.DockSeconds, config.HideSeconds, _scheduleTravelSeconds, // m-3: chu kỳ đồng nhất
            EffectiveTimeScale());

        if (info.State == _lastStates[dockIndex]) return;

        _lastStates[dockIndex] = info.State;
        OnBoatStateChanged?.Invoke(dockIndex, info.State);
    }

    /// <summary>Hệ số tua thời gian hiệu lực: debugTimeScale chỉ ăn trong Editor/Dev build.</summary>
    private float EffectiveTimeScale()
    {
        if (!_allowDebugTime || config == null) return 1f;
        return Mathf.Max(0.01f, config.debugTimeScale);
    }

    private static bool IsValidDock(int dockIndex)
    {
        return dockIndex >= 0 && dockIndex < DockCount;
    }

    // ─── Nội bộ: persist (GDD §3.4) ─────────────────────────────────────

    private void LoadFromPrefs()
    {
        long now = DateTime.UtcNow.Ticks;
        _introDone = PlayerPrefs.GetInt(KeyIntroDone, 0) == 1;

        for (int i = 0; i < DockCount; i++)
        {
            _unlocked[i] = PlayerPrefs.GetInt(_keyUnlocked[i], 0) == 1;

            // PlayerPrefs không có SetLong — anchor ticks lưu dạng string invariant.
            string raw = PlayerPrefs.GetString(_keyAnchor[i], string.Empty);
            long anchor;
            if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out anchor))
            {
                // Thiếu/hỏng dữ liệu anchor: nếu bến đã mở thì coi như chu kỳ bắt đầu
                // lại từ bây giờ (tàu Hidden rồi vào bến bình thường) — vô hại.
                anchor = now;
                if (_unlocked[i]) SaveDockDeferred(i, anchor);
            }

            // [QA B-1] Chống đồng hồ lùi lúc load (GDD §3.4) — với DUNG SAI 1 chu kỳ:
            // anchor tương lai trong phạm vi 1 cycle là hợp lệ (so le vừa đẩy trước
            // khi người chơi thoát game); chỉ reset khi vượt quá 1 cycle.
            if (_unlocked[i] && BoatScheduleCore.IsClockRolledBack(now, anchor,
                    BoatScheduleCore.ComputeCycleSeconds(config.DockSeconds, config.HideSeconds, _scheduleTravelSeconds)))
            {
                anchor = now;
                SaveDockDeferred(i, anchor);
                Debug.LogWarning($"[TouristBoat] Anchor bến {i + 1} vượt tương lai quá 1 chu kỳ (đồng hồ máy chỉnh lùi?) — reset.");
            }

            _anchorTicks[i] = anchor;
        }
    }

    private void SaveDock(int dockIndex)
    {
        PlayerPrefs.SetInt(_keyUnlocked[dockIndex], _unlocked[dockIndex] ? 1 : 0);
        PlayerPrefs.SetString(_keyAnchor[dockIndex],
            _anchorTicks[dockIndex].ToString(CultureInfo.InvariantCulture));
        LuuGopPrefs.Hen(); // lưu gộp có trễ — xem LuuGopPrefs
    }

    /// <summary>Ghi anchor trong lúc load (trước khi _anchorTicks được gán) — dùng giá trị truyền vào.</summary>
    private void SaveDockDeferred(int dockIndex, long anchor)
    {
        PlayerPrefs.SetInt(_keyUnlocked[dockIndex], _unlocked[dockIndex] ? 1 : 0);
        PlayerPrefs.SetString(_keyAnchor[dockIndex], anchor.ToString(CultureInfo.InvariantCulture));
        LuuGopPrefs.Hen();
    }

    // ─── Nội bộ: tìm reference trong scene ──────────────────────────────

    /// <summary>
    /// Tìm BlindPoint / Dock_01..03 / Berth / Path / Boat theo cấu trúc hierarchy
    /// tool của Dev B sinh ra. Mọi mảnh thiếu chỉ LogWarning + fallback, không NRE
    /// (GDD §5 edge 8 — game vẫn chạy khi chưa gắn đủ art/waypoint).
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

        // m-3 (quyết định lead): travel cho LỊCH = max của 3 bến → 3 chu kỳ đồng
        // nhất, luật so le đúng vĩnh viễn (AC §8.4). Tàu path ngắn hơn max sẽ trôi
        // chậm hơn boatSpeed danh nghĩa (controller map progress 0-1 lên path riêng).
        _scheduleTravelSeconds = Mathf.Max(_travelSeconds[0],
                                 Mathf.Max(_travelSeconds[1], _travelSeconds[2]));
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

    /// <summary>travelTime = độ dài polyline thực / boatSpeed (GDD §4). Path hỏng → fallback.</summary>
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
