using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TRUNG TÂM HỆ KHÁCH DU LỊCH (GDD BOAT-002 §3.3, §3.4, §5).
///
/// Vai trò: nghe <c>BoatDockManager.OnBoatDocked</c> → dựng CHUYẾN (random số khách,
/// nhân vật, món) → spawn khách lần lượt xuống tàu → quản hàng chờ → nhận thao tác
/// giao món (thưởng vàng/EXP) → khi khách CUỐI lên tàu thì gọi
/// <c>BoatDockManager.ReportVisitorsAllAboard(dock)</c> cho Dev A cho tàu rời bến.
///
/// ── SỬA THEO QA + SẾP CHỐT 2026-08-29 ──────────────────────────────────────
///   [Sếp 1] Bubble mở LẦN LƯỢT cho MỌI khách (cách nhau <c>bubbleStaggerDelay</c>),
///           đồng hồ kiên nhẫn chạy SONG SONG ⇒ cận trên rời bến = 30p + đi bộ (QA M-2).
///   [QA B-1] 3 lớp chống kẹt tàu:
///           ① thiếu <c>TouristQueue</c> → TỰ TẠO runtime (LogError), không đi tiếp với null;
///           ② nghe <c>OnDockTimeoutForced</c> của Dev A → ép khách tức giận về tàu + dọn save;
///           ③ watchdog riêng mỗi 5s — chuyến sống quá (patience + 10 phút) thì tự kết thúc.
///   [QA B-2] Mọi mốc thời gian của Dev B (kiên nhẫn, giãn cách xuống tàu, nhịp bubble)
///           đều chia <see cref="EffectiveTimeScale"/> — cùng cách Dev A làm, và cũng
///           chỉ ăn trong Editor/Development Build.
///   [QA B-3] KHÔNG BAO GIỜ mất món: tính thưởng TRƯỚC, thiếu điều kiện cộng thưởng thì
///           HỦY giao dịch (không RemoveItem); chỉ trừ kho khi chắc chắn cộng được.
///   [QA m-8] Chỉ xoá save/RAM SAU khi Dev A thực sự nhận lệnh rời bến; bị từ chối thì
///           giữ chuyến lại cho watchdog thử lại (không mất đường phục hồi trong phiên).
///
/// PERSIST (GDD §5.1): PlayerPrefs JSON key <c>TouristTrip_{dock}</c>.
/// Resolve khi load: khách đang đi bộ → SNAP vào slot · patience hết hạn offline →
/// TimedOut ngay · mọi khách xong → báo rời bến ngay.
///
/// LUẬT: mọi con số đi qua <see cref="TouristBoatConfig"/>; mọi Instance đều kiểm null;
/// thời gian dùng DateTime.UtcNow (KHÔNG bao giờ DateTime.Now).
/// </summary>
public class TouristVisitorManager : MonoBehaviour
{
    public static TouristVisitorManager Instance { get; private set; }

    /// <summary>Bắn khi hàng chờ thay đổi (khách mới đến, nhận món, dồn hàng, hết giờ) để Cooking UI đồng bộ.</summary>
    public static event System.Action OnQueueOrderChanged;

    /// <summary>Lấy khách du lịch đang đứng đầu hàng chờ và đang đợi nhận món.</summary>
    public TouristAgent GetFrontWaitingTourist()
    {
        if (queue == null) return null;
        TouristAgent front = queue.Front;
        if (front != null && front.Dish != null && (front.State == TouristAgent.AgentState.WaitingServe || front.State == TouristAgent.AgentState.WalkingToSlot))
            return front;
        return front;
    }

    /// <summary>Báo cho toàn bộ UI cập nhật đơn khách.</summary>
    public static void TriggerQueueOrderChanged()
    {
        OnQueueOrderChanged?.Invoke();
    }

    // ─── Inspector (tool TouristVisitorSetupTool wire hộ) ───────────────

    [Header("Config")]
    [Tooltip("Asset TouristBoatConfig — CÙNG asset mà BoatDockManager dùng. " +
             "Bỏ trống sẽ tự lấy từ BoatDockManager.Instance.Config lúc boot.")]
    [SerializeField] private TouristBoatConfig config;
    public TouristBoatConfig Config => config;

    // [Sếp chốt 2026-08-29] Nhịp mở bubble giữa 2 khách liền nhau.
    // ĐỂ Ở ĐÂY chứ không nhét vào TouristBoatConfig vì file config thuộc gói Dev A —
    // luật của em là KHÔNG sửa file của người khác. Lead muốn gom về config thì thêm
    // 1 field `bubbleStaggerDelay` bên đó rồi đổi dòng đọc ở TakeBubbleStaggerDelay().
    [Tooltip("Giây giữa 2 bubble mở liên tiếp (Sếp chốt 0.4s). Bubble mở lần lượt từ " +
             "khách đứng đầu để người chơi thấy hết đơn của chuyến.")]
    [SerializeField] private float bubbleStaggerDelay = 0.4f;

    [Header("Roster nhân vật (11 prefab NV01..NV11)")]
    [Tooltip("Prefab khách sinh bởi Tools/Farm Game/Tourist Boat/Setup NPC Animations.")]
    [SerializeField] private List<GameObject> touristPrefabs = new List<GameObject>();

    [Header("Database món ăn (38 asset DishData)")]
    [Tooltip("Tool quét AssetDatabase gán sẵn. KHÔNG Resources.Load lúc runtime — " +
             "dự án không dùng pattern đó, và Resources làm phình build.")]
    [SerializeField] private List<DishData> dishDatabase = new List<DishData>();

    [Header("Scene refs")]
    [Tooltip("Hàng chờ trước nhà hàng cooking (object QueueAnchor).")]
    [SerializeField] private TouristQueue queue;

    [Tooltip("Gốc waypoint đường đi bộ của từng bến: TouristPath_Dock01..03 (index 0-2). " +
             "Con WP_01..WP_n sắp theo TÊN.")]
    [SerializeField] private Transform[] dockPathRoots = new Transform[3];

    [Tooltip("Object Gangplank của từng bến (index 0-2) — đầu tấm gỗ phía BỜ là điểm " +
             "khách đặt chân xuống đất.")]
    [SerializeField] private Transform[] gangplanks = new Transform[3];

    [Tooltip("Node cha chứa khách spawn ra. Bỏ trống sẽ tự tạo con 'Visitors'.")]
    [SerializeField] private Transform visitorsRoot;

    [Tooltip("Bỏ qua các waypoint ĐẦU đường mà khách đã đi qua rồi (nằm xa hàng chờ hơn cả " +
             "đầu tấm gỗ) — tránh cảnh khách bước xuống ván rồi vòng ngược lại. Tắt nếu " +
             "muốn khách bám ĐÚNG mọi waypoint bạn đặt.")]
    [SerializeField] private bool boQuaWaypointDaDiQua = true;

    [Header("Hiệu ứng mặt cười")]
    [Tooltip("Sorting layer của FX mặt cười. ĐỂ TRỐNG = tự chọn 'Foreground' (trên đầu khách).")]
    [SerializeField] private string fxSortingLayerName = "";

    [Tooltip("Sorting order FX (đặt CAO hơn bubble để không bị bubble che).")]
    [SerializeField] private int fxSortingOrder = 25000;

    [Tooltip("Cỡ mặt cười lúc scale 1.0 (unit world). Map dùng toạ độ lớn nên số này lớn.")]
    [SerializeField] private float fxWorldSize = 90f;

    [Tooltip("Đích bay của mặt cười = ô VÀNG trên HUD. Tool ★ tự wire. " +
             "Bỏ trống thì FX dò theo tên; dò không ra thì bay THẲNG LÊN TRỜI " +
             "(không bao giờ bay về tâm màn hình).")]
    [SerializeField] private Transform hudGoldTarget;

    [Header("Nhiệm vụ (mission)")]
    // [Lead chốt 2026-08-29] GIỮ TẮT. MissionEventType không có loại "phục vụ khách";
    // loại gần nhất là DeliverOrder nhưng chú thích trong MissionData.cs ghi rõ nó dành
    // riêng cho BẢNG ĐƠN HÀNG, và dự án đã từng phải TÁCH LoadTrainCargo khỏi DeliverOrder
    // vì "giao 5 đơn ở bảng đơn hàng" bị hệ khác hoàn thành hộ = gian lận tiến độ.
    // Ô tick vẫn để đây cho Sếp bật sau nếu muốn. GDD §3.4 cấm thêm enum mới.
    [Tooltip("Bật = bắn mission event khi giao món cho khách. MẶC ĐỊNH TẮT (Lead chốt).")]
    [SerializeField] private bool banMissionEvent = false;

    [Tooltip("Loại event bắn khi bật cờ trên. KHÔNG thêm enum mới (GDD §3.4).")]
    [SerializeField] private MissionEventType missionEventType = MissionEventType.DeliverOrder;

    [Header("Lưới an toàn (QA B-1)")]
    [Tooltip("Giây THỰC chờ khách bị ép rời đi về tàu trước khi despawn cứng. " +
             "Dev A chỉ cho 3s ân hạn nên số này phải nhỏ hơn 3.")]
    [SerializeField] private float forcedCleanupSeconds = 2.2f;

    [Tooltip("Số phút GAME cộng thêm vào patience để tính hạn tối đa của 1 chuyến " +
             "trước khi watchdog của Dev B tự kết thúc chuyến.")]
    [SerializeField] private float watchdogExtraMinutes = 10f;

    // ─── Persist ────────────────────────────────────────────────────────

    private const string KeyTripFormat = "TouristTrip_{0}";

    /// <summary>Pha của một chuyến (ghi vào save).</summary>
    private enum TripPhase { None = 0, Unloading = 1, Serving = 2, Boarding = 3, Done = 4 }

    /// <summary>Blob JSON của 1 chuyến — JsonUtility hỗ trợ mảng primitive/string.</summary>
    [Serializable]
    private class TripSave
    {
        public int    dock;
        public long   arrivalUtcTicks;      // seed random — offline tái lập đúng chuyến
        public int    phase;                // TripPhase
        public int[]  charIdx;              // index prefab trong roster
        public string[] dishId;             // dishId khách yêu cầu
        public bool[] served;
        public bool[] timedOut;
        public long[] patienceEndUtcTicks;  // 0 = bubble chưa mở
    }

    /// <summary>Trạng thái runtime của 1 chuyến.</summary>
    private class Trip
    {
        public TripSave           Save;
        public List<TouristAgent> Agents = new List<TouristAgent>();   // null tại slot đã xong
        public int                DoneCount;                            // đã aboard/resolve xong
        public bool               Reported;                             // Dev A đã nhận lệnh rời bến
        public bool               PendingReport;                        // Dev A từ chối — watchdog thử lại (QA m-8)
        public bool               ForcedEnding;                         // đang chạy dọn ép (lưới an toàn)
        public Coroutine          DisembarkRoutine;
        public float              StartRealtime;                        // để watchdog đo tuổi chuyến
    }

    // ─── Runtime ────────────────────────────────────────────────────────

    private readonly Trip[] _trips            = new Trip[BoatDockManager.DockCount];
    private readonly long[] _scheduledArrival = new long[BoatDockManager.DockCount];

    private BoatDockManager _mgr;          // instance đã subscribe (m-10: gỡ đúng cái đã gắn)
    private bool  _subscribed;
    private bool  _allowDebugTime;
    private bool  _warnedNoRoster;
    private bool  _warnedNoDish;
    private bool  _warnedNoQueue;
    private float _nextBubbleOpenTime;     // nhịp stagger dùng chung toàn hệ

    /// <summary>
    /// [QA B-2] Hệ số tua thời gian hiệu lực — CÙNG luật với Dev A: chỉ ăn trong
    /// Editor/Development Build, release luôn chạy 1. Dev A để hàm này private nên
    /// Dev B tự tính lại y hệt thay vì sửa file của họ.
    /// </summary>
    public float EffectiveTimeScale
    {
        get
        {
            if (!_allowDebugTime || config == null) return 1f;
            return Mathf.Max(0.01f, config.debugTimeScale);
        }
    }

    // ─── Unity lifecycle ────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Debug.isDebugBuild = true trong Editor và Development Build — đúng phạm vi
        // cho phép của debugTimeScale (GDD §7), copy nguyên cách BoatDockManager làm.
        _allowDebugTime = Application.isEditor || Debug.isDebugBuild;
    }

    private void Start()
    {
        StartCoroutine(BootRoutine());
    }

    private void OnDestroy()
    {
        if (_subscribed && _mgr != null)
        {
            _mgr.OnBoatDocked        -= HandleBoatDocked;
            _mgr.OnBoatDeparting     -= HandleBoatDeparting;
            _mgr.OnNextTripScheduled -= HandleNextTripScheduled;
            _mgr.OnDockTimeoutForced -= HandleDockTimeoutForced;
        }
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Đợi BoatDockManager sẵn sàng (IsReady = đã load PlayerPrefs xong — cùng cách
    /// TouristBoatUnlockFlow chờ), subscribe, rồi QUÉT IsDocked cả 3 bến để khôi phục
    /// chuyến dở dang (contract Dev A: OnBoatDocked KHÔNG bắn lại lúc load).
    /// </summary>
    private IEnumerator BootRoutine()
    {
        float waited = 0f;
        while ((BoatDockManager.Instance == null || !BoatDockManager.Instance.IsReady) && waited < 8f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        _mgr = BoatDockManager.Instance;
        if (_mgr == null || !_mgr.IsReady)
        {
            Debug.LogWarning("[TouristVisitor] BoatDockManager chưa sẵn sàng sau 8s — hệ khách không chạy. " +
                             "Kiểm tra BoatSystem có trong scene và đã gán TouristBoatConfig chưa.");
            yield break;
        }

        if (config == null) config = _mgr.Config;
        if (config == null)
        {
            Debug.LogError("[TouristVisitor] Không có TouristBoatConfig (cả trên manager lẫn BoatDockManager) — hệ khách tắt.");
            yield break;
        }

        EnsureSceneRefs();

        _mgr.OnBoatDocked        += HandleBoatDocked;
        _mgr.OnBoatDeparting     += HandleBoatDeparting;
        _mgr.OnNextTripScheduled += HandleNextTripScheduled;
        _mgr.OnDockTimeoutForced += HandleDockTimeoutForced;   // QA B-1 — lưới an toàn Dev A
        _subscribed = true;

        // ── QUÉT KHÔI PHỤC (bắt buộc theo contract Dev A) ──
        for (int dock = 0; dock < BoatDockManager.DockCount; dock++)
        {
            if (_mgr.IsDocked(dock))
            {
                ResumeOrStartTrip(dock);
            }
            else
            {
                // Tàu KHÔNG đậu mà còn save → chuyến đó đã kết thúc lúc offline: dọn sạch
                // để lần cập bến sau không hồi sinh khách cũ (GDD §5.1 "không nhân đôi khách").
                if (PlayerPrefs.HasKey(TripKey(dock))) ClearTripSave(dock);
            }
        }

        Debug.Log($"[TouristVisitor] Sẵn sàng — roster {touristPrefabs.Count} nhân vật, " +
                  $"{dishDatabase.Count} món, bến mở {_mgr.UnlockedDockCount}, timeScale={EffectiveTimeScale:0.##}.");

        StartCoroutine(WatchdogRoutine());
    }

    /// <summary>
    /// [QA B-1 lớp ③] Watchdog độc lập: mỗi 5 giây thực
    ///   • thử lại lệnh rời bến bị Dev A từ chối (QA m-8);
    ///   • chuyến sống quá (patience + watchdogExtraMinutes) mà chưa xong → tự kết thúc.
    /// Không phụ thuộc event nào — đây là lưới cuối cùng để hệ boat không bao giờ chết.
    /// </summary>
    private IEnumerator WatchdogRoutine()
    {
        var doi = new WaitForSeconds(5f);
        while (true)
        {
            yield return doi;

            for (int dock = 0; dock < BoatDockManager.DockCount; dock++)
            {
                Trip trip = _trips[dock];
                if (trip == null) continue;

                if (trip.PendingReport) { TryFinishTrip(dock, trip); continue; }
                if (trip.ForcedEnding) continue;

                float buGiay  = Mathf.Max(0f, watchdogExtraMinutes) * 60f / Mathf.Max(0.01f, EffectiveTimeScale);
                float hanGiay = PatienceSecondsScaled() + buGiay;
                if (Time.realtimeSinceStartup - trip.StartRealtime < hanGiay) continue;

                Debug.LogWarning($"[TouristVisitor] Watchdog: chuyến bến {dock + 1} sống quá " +
                                 $"{hanGiay:0}s thực mà chưa xong ({trip.DoneCount}/{trip.Save.charIdx.Length}) — " +
                                 "tự kết thúc để tàu không kẹt.");
                ForceEndTrip(dock, "watchdog Dev B");
            }
        }
    }

    // ─── Event từ Dev A ─────────────────────────────────────────────────

    private void HandleBoatDocked(int dock)
    {
        if (!IsValidDock(dock)) return;
        ResumeOrStartTrip(dock);
    }

    private void HandleBoatDeparting(int dock)
    {
        if (!IsValidDock(dock)) return;

        // Tàu rời bến: dọn sạch mọi thứ còn sót (bình thường đã rỗng vì tàu chỉ rời
        // sau khi ta báo ReportVisitorsAllAboard).
        DestroyTrip(dock, "tàu rời bến");
        ClearTripSave(dock);
    }

    private void HandleNextTripScheduled(int dock, DateTime arrivalUtc, int gapMinutes)
    {
        if (!IsValidDock(dock)) return;
        // Ghi lại mốc cập bến kế — dùng làm SEED random cho chuyến đó, nhờ vậy chuyến
        // được dựng lại y hệt nếu save bị mất (GDD §4: "seed = arrivalUtc").
        _scheduledArrival[dock] = arrivalUtc.Ticks;
    }

    /// <summary>
    /// [QA B-1 lớp ②] Dev A báo tàu đậu quá <c>maxDockMinutes</c> và sẽ tự ép rời bến
    /// sau 3 giây ân hạn: mọi khách CHƯA được phục vụ chuyển TỨC GIẬN ngay → về tàu →
    /// despawn, rồi dọn save chuyến (Sếp chốt 2026-08-29).
    /// </summary>
    private void HandleDockTimeoutForced(int dock)
    {
        if (!IsValidDock(dock)) return;
        Debug.LogWarning($"[TouristVisitor] Nhận OnDockTimeoutForced bến {dock + 1} — " +
                         "đuổi khách còn lại về tàu (khách chưa phục vụ: TỨC GIẬN, không thưởng).");
        ForceEndTrip(dock, "lưới an toàn Dev A");
    }

    /// <summary>
    /// Kết thúc CƯỠNG BỨC một chuyến: khách chưa phục vụ → Angry + về tàu; sau
    /// <c>forcedCleanupSeconds</c> giây thực thì despawn hết, báo Dev A, xoá save.
    /// </summary>
    private void ForceEndTrip(int dock, string lyDo)
    {
        Trip trip = _trips[dock];
        if (trip == null)
        {
            ClearTripSave(dock);
            BoatDockManager.Instance?.ReportVisitorsAllAboard(dock);
            return;
        }
        if (trip.ForcedEnding) return;
        trip.ForcedEnding = true;

        if (trip.DisembarkRoutine != null)
        {
            StopCoroutine(trip.DisembarkRoutine);
            trip.DisembarkRoutine = null;
        }

        for (int i = 0; i < trip.Agents.Count; i++)
        {
            TouristAgent a = trip.Agents[i];
            if (a == null) continue;
            if (!a.WasServed) trip.Save.timedOut[i] = true;
            a.ForceLeaveAngry();
        }
        SaveTrip(dock);

        StartCoroutine(ForcedCleanupRoutine(dock, trip, lyDo));
    }

    private IEnumerator ForcedCleanupRoutine(int dock, Trip trip, string lyDo)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, forcedCleanupSeconds));

        if (_trips[dock] != trip) yield break; // chuyến đã kết thúc êm trong lúc chờ

        for (int i = 0; i < trip.Agents.Count; i++)
        {
            TouristAgent a = trip.Agents[i];
            if (a == null) continue;
            if (queue != null) queue.Remove(a);
            Destroy(a.gameObject);
            trip.Agents[i] = null;
        }

        _trips[dock] = null;
        ClearTripSave(dock);
        BoatDockManager.Instance?.ReportVisitorsAllAboard(dock);
        Debug.Log($"[TouristVisitor] Đã dọn cưỡng bức chuyến bến {dock + 1} ({lyDo}).");
    }

    // ─── Dựng / khôi phục chuyến ────────────────────────────────────────

    /// <summary>Có save hợp lệ → khôi phục; không → dựng chuyến mới. Idempotent.</summary>
    private void ResumeOrStartTrip(int dock)
    {
        if (_trips[dock] != null) return; // chuyến đang chạy — không dựng chồng

        TripSave save = LoadTripSave(dock);
        if (save != null && save.phase != (int)TripPhase.None && save.phase != (int)TripPhase.Done)
            ResumeTrip(dock, save);
        else
            StartNewTrip(dock);
    }

    /// <summary>
    /// Dựng chuyến MỚI: random số khách [visitorsMin..visitorsMax], random nhân vật
    /// KHÔNG lặp trong chuyến, random món KHÔNG trùng (nếu đủ món hợp lệ).
    /// Seed lấy từ arrivalUtc + dock ⇒ cùng một chuyến luôn ra cùng kết quả.
    /// </summary>
    private void StartNewTrip(int dock)
    {
        if (config == null) return;

        if (touristPrefabs == null || touristPrefabs.Count == 0)
        {
            if (!_warnedNoRoster)
            {
                _warnedNoRoster = true;
                Debug.LogError("[TouristVisitor] Roster prefab khách TRỐNG — chạy " +
                               "Tools/Farm Game/Tourist Boat/Setup NPC Animations rồi " +
                               "Setup Tourist Visitors (Scene). Tạm báo tàu đi ngay để không kẹt bến.");
            }
            BoatDockManager.Instance?.ReportVisitorsAllAboard(dock);
            return;
        }

        long arrival = _scheduledArrival[dock] > 0 ? _scheduledArrival[dock] : DateTime.UtcNow.Ticks;
        var rng = new System.Random(MakeSeed(arrival, dock));

        int min = Mathf.Max(1, config.visitorsMin);
        int max = Mathf.Max(min, config.visitorsMax);
        int count = rng.Next(min, max + 1);
        count = Mathf.Min(count, touristPrefabs.Count); // roster 11 ≥ 6 nên thực tế không cắt

        var save = new TripSave
        {
            dock                = dock,
            arrivalUtcTicks     = arrival,
            phase               = (int)TripPhase.Unloading,
            charIdx             = PickCharacters(rng, count),
            dishId              = new string[count],
            served              = new bool[count],
            timedOut            = new bool[count],
            patienceEndUtcTicks = new long[count],
        };

        List<DishData> dishes = PickDishes(rng, count);
        for (int i = 0; i < count; i++)
            save.dishId[i] = dishes[i] != null ? dishes[i].dishId : string.Empty;

        var trip = new Trip { Save = save, StartRealtime = Time.realtimeSinceStartup };
        for (int i = 0; i < count; i++) trip.Agents.Add(null);
        _trips[dock] = trip;

        SaveTrip(dock);

        int boatNo = BoatDockManager.Instance != null ? BoatDockManager.Instance.BoatNumber(dock) : dock + 1;
        Debug.Log($"[TouristVisitor] Tàu số {boatNo:00} cập bến {dock + 1} — {count} khách xuống bờ.");
        AudioManager.Instance?.PlayTouristChatter();

        trip.DisembarkRoutine = StartCoroutine(DisembarkRoutine(dock));
    }

    /// <summary>
    /// Cho khách xuống tàu lần lượt, cách nhau <c>disembarkInterval</c> giây (GDD §3.1).
    /// [QA B-2] Giãn cách CHIA debugTimeScale để tua nhanh thì khách cũng xuống nhanh.
    /// </summary>
    private IEnumerator DisembarkRoutine(int dock)
    {
        Trip trip = _trips[dock];
        if (trip == null) yield break;

        float interval = config != null ? Mathf.Max(0f, config.disembarkInterval) : 0.8f;
        interval /= Mathf.Max(0.01f, EffectiveTimeScale);

        Vector3 boatPos = GetBoardPosition(dock);

        for (int i = 0; i < trip.Save.charIdx.Length; i++)
        {
            if (_trips[dock] != trip) yield break; // chuyến bị huỷ giữa chừng (đổi scene…)

            TouristAgent agent = SpawnAgent(dock, i, trip);
            if (agent != null)
            {
                int slot = queue != null ? queue.Enqueue(agent) : i;
                agent.AssignInitialSlot(slot, slot == 0);
                agent.BeginDisembark(boatPos);
            }
            else
            {
                // Không spawn được (prefab lỗi) → coi như khách đó đã xong, không kẹt tàu.
                trip.Save.timedOut[i] = true;
                trip.DoneCount++;
            }

            if (interval > 0f) yield return new WaitForSeconds(interval);
        }

        trip.Save.phase = (int)TripPhase.Serving;
        SaveTrip(dock);
        trip.DisembarkRoutine = null;

        CheckAllAboard(dock);
    }

    /// <summary>
    /// Khôi phục chuyến từ save (GDD §5.1). Khách đang đi bộ → SNAP vào slot; khách
    /// hết kiên nhẫn lúc offline → resolve TimedOut ngay (không spawn); tất cả xong →
    /// báo ReportVisitorsAllAboard lập tức.
    /// </summary>
    private void ResumeTrip(int dock, TripSave save)
    {
        var trip = new Trip { Save = save, StartRealtime = Time.realtimeSinceStartup };
        int count = save.charIdx != null ? save.charIdx.Length : 0;
        for (int i = 0; i < count; i++) trip.Agents.Add(null);
        _trips[dock] = trip;

        long now = DateTime.UtcNow.Ticks;
        int hetGio = 0, daXong = 0, hienLai = 0;

        for (int i = 0; i < count; i++)
        {
            if (save.served[i] || save.timedOut[i]) { trip.DoneCount++; daXong++; continue; }

            // Kiên nhẫn hết hạn trong lúc tắt game → TimedOut, không cần diễn lại.
            if (save.patienceEndUtcTicks[i] > 0 && now >= save.patienceEndUtcTicks[i])
            {
                save.timedOut[i] = true;
                trip.DoneCount++;
                hetGio++;
                continue;
            }

            TouristAgent agent = SpawnAgent(dock, i, trip);
            if (agent == null) { save.timedOut[i] = true; trip.DoneCount++; continue; }

            int slot = queue != null ? queue.Enqueue(agent) : hienLai;
            Vector3 slotPos = queue != null ? queue.GetSlotPosition(slot) : agent.transform.position;
            agent.ResumeInQueue(slot, slotPos, slot == 0, save.patienceEndUtcTicks[i]);
            hienLai++;
        }

        save.phase = (int)TripPhase.Serving;
        SaveTrip(dock);

        Debug.Log($"[TouristVisitor] Khôi phục chuyến bến {dock + 1}: {hienLai} khách còn chờ, " +
                  $"{daXong} đã xong trước đó, {hetGio} hết kiên nhẫn lúc offline.");

        CheckAllAboard(dock);
    }

    // ─── Random helper ──────────────────────────────────────────────────

    /// <summary>
    /// Seed ổn định từ mốc cập bến + bến (GDD §4: offline tái lập đúng chuyến).
    ///
    /// [QA đo được 2026-08-29] Công thức cũ <c>(giây ^ giây>>32) * 397 + dock * 7919</c>
    /// là phép NHÂN TUYẾN TÍNH: gap giữa 2 chuyến cố định ⇒ seed tăng đều ⇒ số khách ra
    /// dãy RĂNG CƯA nhìn thấy bằng mắt. Đo thật với gap 7 phút, 200 chuyến:
    /// <c>3 3 3 4 4 4 4 4 5 5 5 5 6 6 6 6 3 3 3 3…</c> — bậc thang leo rồi tụt.
    ///
    /// Nay dùng **SplitMix64 finalizer** — hàm băm trộn bit thật (avalanche): đổi 1 bit
    /// đầu vào làm đổi ~nửa số bit đầu ra, nên seed liền kề cho ra kết quả không tương quan.
    /// Vẫn TÁI LẬP tuyệt đối: cùng arrivalUtc + dock luôn cho cùng một seed, nên tắt/mở
    /// game không đổi số khách của chuyến đang dở.
    ///
    /// Kiểm chứng bằng test console (mono), 200 chuyến/cấu hình, so với RNG i.i.d. lý tưởng:
    /// <code>
    ///   chuỗi không-giảm dài nhất:  cũ 11-20  ·  MỚI 6-9  ·  i.i.d. lý tưởng 6-8
    ///   chuỗi giảm-hẳn dài nhất:    cũ 2-4    ·  MỚI 3-4  ·  i.i.d. lý tưởng 3-4
    ///   phân phối 3/4/5/6 (chi²/df=3): MỚI 2.0-10.3 · i.i.d. lý tưởng 0.7-6.2
    /// </code>
    /// ⇒ hết răng cưa, phân phối tương đương RNG thật.
    /// </summary>
    private static int MakeSeed(long arrivalUtcTicks, int dock)
    {
        unchecked
        {
            // Ticks 100ns quá mịn — quy về GIÂY để lệch vài tick không đổi chuyến.
            ulong z = (ulong)(arrivalUtcTicks / TimeSpan.TicksPerSecond)
                      + 0x9E3779B97F4A7C15UL * (ulong)(dock + 1); // tỉ lệ vàng 64-bit, tách bến
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;

            // Kẹp về non-negative: System.Random nhận int, và int.MinValue làm Abs() nổ.
            return (int)(z & 0x7FFFFFFF);
        }
    }

    /// <summary>Chọn <paramref name="count"/> nhân vật KHÔNG LẶP từ roster (Fisher-Yates).</summary>
    private int[] PickCharacters(System.Random rng, int count)
    {
        int n = touristPrefabs.Count;
        var pool = new int[n];
        for (int i = 0; i < n; i++) pool[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        var result = new int[count];
        for (int i = 0; i < count; i++) result[i] = pool[i % n];
        return result;
    }

    /// <summary>
    /// Chọn món cho từng khách: chỉ lấy DishData có <c>unlockLevel ≤ level hiện tại</c>,
    /// KHÔNG trùng nhau nếu đủ món. Không có món hợp lệ nào (GDD §5 edge 4) → lấy món
    /// có unlockLevel THẤP NHẤT để khách vẫn có gì đó để gọi.
    /// </summary>
    private List<DishData> PickDishes(System.Random rng, int count)
    {
        var result = new List<DishData>(count);

        if (dishDatabase == null || dishDatabase.Count == 0)
        {
            if (!_warnedNoDish)
            {
                _warnedNoDish = true;
                Debug.LogError("[TouristVisitor] Database món TRỐNG — khách sẽ không có món để gọi. " +
                               "Chạy Tools/Farm Game/Tourist Boat/Setup Tourist Visitors (Scene) để tool quét DishData.");
            }
            for (int i = 0; i < count; i++) result.Add(null);
            return result;
        }

        int level = FarmLevelManager.Instance != null ? FarmLevelManager.Instance.CurrentLevel : 1;

        var eligible = new List<DishData>();
        for (int i = 0; i < dishDatabase.Count; i++)
        {
            DishData d = dishDatabase[i];
            if (d == null || string.IsNullOrEmpty(d.dishId)) continue;
            if (d.unlockLevel <= level) eligible.Add(d);
        }

        if (eligible.Count == 0)
        {
            DishData thap = null;
            for (int i = 0; i < dishDatabase.Count; i++)
            {
                DishData d = dishDatabase[i];
                if (d == null || string.IsNullOrEmpty(d.dishId)) continue;
                if (thap == null || d.unlockLevel < thap.unlockLevel) thap = d;
            }
            Debug.LogWarning($"[TouristVisitor] Không món nào mở ở cấp {level} — " +
                             $"khách tạm gọi món thấp nhất '{(thap != null ? thap.dishId : "?")}'.");
            for (int i = 0; i < count; i++) result.Add(thap);
            return result;
        }

        // Xáo trộn rồi lấy lần lượt — không trùng khi đủ món, quay vòng khi thiếu.
        for (int i = eligible.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            DishData tmp = eligible[i]; eligible[i] = eligible[j]; eligible[j] = tmp;
        }
        for (int i = 0; i < count; i++)
            result.Add(eligible[i % eligible.Count]);

        return result;
    }

    // ─── Spawn / despawn khách ──────────────────────────────────────────

    private TouristAgent SpawnAgent(int dock, int visitorIndex, Trip trip)
    {
        int charIdx = trip.Save.charIdx[visitorIndex];
        if (touristPrefabs == null || charIdx < 0 || charIdx >= touristPrefabs.Count) return null;

        GameObject prefab = touristPrefabs[charIdx];
        if (prefab == null)
        {
            Debug.LogWarning($"[TouristVisitor] Roster slot {charIdx} bị trống (prefab null) — bỏ qua khách này.");
            return null;
        }

        GameObject go = Instantiate(prefab, GetBoardPosition(dock), Quaternion.identity, VisitorsRoot());
        go.name = $"Tourist_D{dock + 1}_{visitorIndex}";

        var agent = go.GetComponent<TouristAgent>();
        if (agent == null)
        {
            Debug.LogWarning($"[TouristVisitor] Prefab '{prefab.name}' thiếu component TouristAgent — bỏ qua.");
            Destroy(go);
            return null;
        }

        agent.Setup(this, config, queue, dock, visitorIndex,
                    FindDish(trip.Save.dishId[visitorIndex]),
                    GetPathPoints(dock),
                    trip.Save.patienceEndUtcTicks[visitorIndex]);

        trip.Agents[visitorIndex] = agent;
        return agent;
    }

    private DishData FindDish(string dishId)
    {
        if (string.IsNullOrEmpty(dishId) || dishDatabase == null) return null;
        for (int i = 0; i < dishDatabase.Count; i++)
        {
            DishData d = dishDatabase[i];
            if (d != null && string.Equals(d.dishId, dishId, StringComparison.OrdinalIgnoreCase))
                return d;
        }
        return null;
    }

    // ─── API cho TouristAgent ───────────────────────────────────────────

    /// <summary>
    /// [Sếp chốt] Cấp LƯỢT mở bubble: trả về số giây khách phải chờ trước khi mở, để
    /// các bubble nở lần lượt cách nhau <c>bubbleStaggerDelay</c> (đã chia debugTimeScale).
    /// Khách tới hàng trước (đứng đầu) xin lượt trước ⇒ mở trước, đúng yêu cầu.
    /// </summary>
    public float TakeBubbleStaggerDelay()
    {
        float stagger = Mathf.Max(0f, bubbleStaggerDelay) / Mathf.Max(0.01f, EffectiveTimeScale);
        float now     = Time.time;
        float openAt  = Mathf.Max(now, _nextBubbleOpenTime);
        _nextBubbleOpenTime = openAt + stagger;
        return openAt - now;
    }

    /// <summary>
    /// GIAO MÓN (tap khách). [QA B-3] KHÔNG BAO GIỜ ĐƯỢC MẤT MÓN — thứ tự bắt buộc:
    ///   ① tính thưởng TRƯỚC;
    ///   ② thiếu điều kiện cộng thưởng (vàng ≤ 0 / thiếu FarmEconomyManager /
    ///      thiếu PlayerProgressManager) ⇒ HỦY giao dịch, KHÔNG RemoveItem;
    ///   ③ chỉ khi chắc chắn cộng được mới RemoveItem;
    ///   ④ RemoveItem trả false ⇒ không cộng gì.
    /// Chỉ REMOVE, không ADD vào kho nên không đụng edge "kho đầy" (§5.3).
    /// </summary>
    public void DeliverTo(TouristAgent agent)
    {
        if (agent == null) return;

        if (agent.IsWaitingBubble)
        {
            FarmUIManager.Instance?.ShowHint("Khách đang xem thực đơn — đợi một chút nhé!");
            return;
        }

        if (!agent.CanReceiveDish)
        {
            FarmUIManager.Instance?.ShowHint("Khách này không nhận món lúc này.");
            return;
        }

        DishData dish = agent.Dish;
        if (dish == null || string.IsNullOrEmpty(dish.dishId))
        {
            Debug.LogWarning("[TouristVisitor] Khách không gắn được món (DishData null) — bỏ qua thao tác giao.");
            return;
        }

        string tenMon = !string.IsNullOrEmpty(dish.dishName) ? dish.dishName : dish.dishId;

        var kho = FarmInventoryManager.Instance;
        if (kho == null)
        {
            Debug.LogError("[TouristVisitor] Không có FarmInventoryManager — không giao món được.");
            return;
        }

        if (!kho.HasItem(dish.dishId))
        {
            FarmUIManager.Instance?.ShowHint($"Chưa có {tenMon} trong kho — vào bếp nấu nhé!");
            return;
        }

        // ── ① TÍNH THƯỞNG TRƯỚC KHI ĐỘNG VÀO KHO (QA B-3) ──
        // [QA B-6] TouristRewardCalculator là file RIÊNG của Dev A
        // (Visitors/TouristRewardCalculator.cs) — không còn nằm trong TouristSmileyFlyFX.cs.
        // Gọi chữ ký NHẬN CONFIG thay vì chữ ký cũ: ta đã có sẵn config trong tay, khỏi
        // phụ thuộc ẩn vào BoatDockManager.Instance (nó có thể null khi test scene riêng),
        // và chắc chắn ăn đúng các knob mới của Dev A (touristGoldMultiplier /
        // touristExpMultiplier = 0.4 — núm hãm lạm phát EXP, QA M-9).
        bool fallback;
        int vang = TouristRewardCalculator.ComputeGold(dish, config, out fallback);
        int exp  = TouristRewardCalculator.ComputeExp(dish, config);

        var eco  = FarmEconomyManager.Instance;
        var tien = PlayerProgressManager.Instance;

        // ── ② THIẾU ĐIỀU KIỆN ⇒ HỦY, KHÔNG TRỪ KHO ──
        if (vang <= 0 || eco == null || tien == null)
        {
            FarmUIManager.Instance?.ShowHint("Chưa nhận được thưởng — món vẫn còn nguyên trong kho.");
            Debug.LogError($"[TouristVisitor] HỦY giao '{dish.dishId}': vang={vang}, " +
                           $"FarmEconomyManager={(eco != null ? "OK" : "NULL")}, " +
                           $"PlayerProgressManager={(tien != null ? "OK" : "NULL")}. " +
                           "Món KHÔNG bị trừ khỏi kho (chống mất đồ người chơi).");
            return;
        }

        // ── ③ TRỪ KHO ──
        if (!kho.RemoveItem(dish.dishId, 1))
        {
            // Số lượng đổi giữa HasItem và RemoveItem (bán ở quầy cùng lúc…) — KHÔNG thưởng.
            FarmUIManager.Instance?.ShowHint($"Chưa có {tenMon} trong kho — vào bếp nấu nhé!");
            Debug.LogWarning($"[TouristVisitor] RemoveItem('{dish.dishId}') trả false dù HasItem true — không cộng thưởng.");
            return;
        }

        // ── ④ CỘNG THƯỞNG (chắc chắn thành công vì đã kiểm ở ②) ──
        eco.AddGold(vang);              // tự bắn OnGoldAddedFx → CoinFlyFX có sẵn
        if (exp > 0) tien.AddExp(exp);

        if (banMissionEvent)
            MissionProgressTracker.ReportEvent(missionEventType, dish.dishId, 1);

        Debug.Log($"[TouristVisitor] Giao '{dish.dishId}' cho khách bến {agent.DockIndex + 1}: " +
                  $"+{vang} vàng{(fallback ? " (fallback sellPrice)" : "")}, +{exp} EXP.");

        agent.MarkServed();

        Trip trip = TripOf(agent);
        if (trip != null)
        {
            trip.Save.served[agent.VisitorIndex] = true;
            SaveTrip(agent.DockIndex);
        }

        TriggerQueueOrderChanged();
    }

    /// <summary>Agent báo hết kiên nhẫn — ghi save rồi cho agent diễn mặt tức giận.</summary>
    public void NotifyTimedOut(TouristAgent agent)
    {
        if (agent == null) return;

        Trip trip = TripOf(agent);
        if (trip != null)
        {
            trip.Save.timedOut[agent.VisitorIndex] = true;
            SaveTrip(agent.DockIndex);
        }
        agent.MarkTimedOut();
        TriggerQueueOrderChanged();
    }

    /// <summary>Agent báo vừa mở bubble → lưu mốc kiên nhẫn UTC để offline vẫn đếm.</summary>
    public void NotifyBubbleOpened(TouristAgent agent)
    {
        if (agent == null) return;

        Trip trip = TripOf(agent);
        if (trip == null) return;

        trip.Save.patienceEndUtcTicks[agent.VisitorIndex] = agent.PatienceEndUtcTicks;
        SaveTrip(agent.DockIndex);
        TriggerQueueOrderChanged();
    }

    /// <summary>Agent đã lên tàu xong → despawn, đếm; khách cuối thì báo Dev A cho tàu đi.</summary>
    public void NotifyAboard(TouristAgent agent)
    {
        if (agent == null) return;

        int dock = agent.DockIndex;
        Trip trip = TripOf(agent);
        if (trip != null)
        {
            trip.Agents[agent.VisitorIndex] = null;
            trip.DoneCount++;
            trip.Save.phase = (int)TripPhase.Boarding;
            SaveTrip(dock);
        }

        Destroy(agent.gameObject);
        CheckAllAboard(dock);
    }

    /// <summary>Bắn mặt cười bay lên HUD tại đầu khách (agent gọi sau 0.5s giữ mặt cười).</summary>
    public void SpawnSmileyFor(TouristAgent agent)
    {
        if (agent == null) return;

        var bubble = agent.GetComponent<TouristRequestBubble>();
        Sprite smiley = bubble != null ? bubble.SmileySpriteResolved : null;
        float flyTime = config != null ? config.smileyFlyTime : 1.2f;

        // Đầu khách ≈ trên chân 1 chút — dùng bounds của renderer cho đúng mọi cỡ prefab.
        Vector3 start = agent.transform.position;
        var sr = agent.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) start = new Vector3(sr.bounds.center.x, sr.bounds.max.y, start.z);

        string layer = TouristSortingLayers.ResolveOrOverride(fxSortingLayerName, TouristSortingLayers.Overlay);
        TouristSmileyFlyFX.Spawn(start, smiley, flyTime, layer, fxSortingOrder, fxWorldSize, hudGoldTarget);
    }

    /// <summary>Vị trí lên/xuống tàu của bến (mạn tàu) — Berth của Dev A, fallback gangplank.</summary>
    public Vector3 GetBoardPosition(int dock)
    {
        var mgr = BoatDockManager.Instance;
        Transform berth = mgr != null ? mgr.GetDockBerth(dock) : null;
        if (berth != null) return berth.position;

        if (IsValidDock(dock) && gangplanks != null && dock < gangplanks.Length && gangplanks[dock] != null)
            return gangplanks[dock].position;

        return transform.position;
    }

    // ─── Điều phối chuyến ───────────────────────────────────────────────

    /// <summary>
    /// Mọi khách đã Served/TimedOut VÀ đã lên tàu xong → báo Dev A cho tàu rời bến.
    /// Gọi được nhiều lần, chỉ báo đúng 1 lần cho mỗi chuyến.
    /// </summary>
    private void CheckAllAboard(int dock)
    {
        Trip trip = _trips[dock];
        if (trip == null || trip.Reported || trip.ForcedEnding) return;
        if (trip.DisembarkRoutine != null) return; // còn khách chưa xuống tàu

        int total = trip.Save.charIdx != null ? trip.Save.charIdx.Length : 0;
        if (trip.DoneCount < total) return;

        // Còn agent nào sống (đang đi bộ về) thì chưa xong.
        for (int i = 0; i < trip.Agents.Count; i++)
            if (trip.Agents[i] != null) return;

        TryFinishTrip(dock, trip);
    }

    /// <summary>
    /// [QA m-8] Báo Dev A TRƯỚC, chỉ xoá save + RAM khi Dev A THỰC SỰ nhận lệnh
    /// (tàu rời khỏi pha Docked). Bị từ chối (lịch vừa reset vì đồng hồ lùi…) thì GIỮ
    /// chuyến lại và để watchdog thử lại mỗi 5s — không mất đường phục hồi trong phiên.
    /// </summary>
    private void TryFinishTrip(int dock, Trip trip)
    {
        var mgr = BoatDockManager.Instance;
        if (mgr == null)
        {
            _trips[dock] = null;
            ClearTripSave(dock);
            return;
        }

        mgr.ReportVisitorsAllAboard(dock);

        if (mgr.IsDocked(dock))
        {
            if (!trip.PendingReport)
            {
                trip.PendingReport = true;
                Debug.LogWarning($"[TouristVisitor] Bến {dock + 1}: Dev A chưa nhận lệnh rời bến " +
                                 "(tàu vẫn Docked) — GIỮ chuyến lại, watchdog sẽ thử lại mỗi 5s.");
            }
            return;
        }

        trip.Reported      = true;
        trip.PendingReport = false;
        trip.Save.phase    = (int)TripPhase.Done;
        _trips[dock]       = null;
        ClearTripSave(dock);

        int boatNo = mgr.BoatNumber(dock);
        Debug.Log($"[TouristVisitor] Khách cuối đã lên tàu số {boatNo:00} (bến {dock + 1}) — tàu rời bến.");
    }

    /// <summary>Huỷ toàn bộ khách của 1 bến (tàu đi / dọn scene).</summary>
    private void DestroyTrip(int dock, string lyDo)
    {
        Trip trip = _trips[dock];
        if (trip == null) return;

        if (trip.DisembarkRoutine != null) StopCoroutine(trip.DisembarkRoutine);

        for (int i = 0; i < trip.Agents.Count; i++)
        {
            TouristAgent a = trip.Agents[i];
            if (a == null) continue;
            if (queue != null) queue.Remove(a);
            Destroy(a.gameObject);
        }
        _trips[dock] = null;

        Debug.Log($"[TouristVisitor] Dọn chuyến bến {dock + 1} ({lyDo}).");
    }

    private Trip TripOf(TouristAgent agent)
    {
        if (agent == null || !IsValidDock(agent.DockIndex)) return null;
        Trip trip = _trips[agent.DockIndex];
        if (trip == null || trip.Save == null || trip.Save.charIdx == null) return null;
        if (agent.VisitorIndex < 0 || agent.VisitorIndex >= trip.Save.charIdx.Length) return null;
        return trip;
    }

    /// <summary>Giây kiên nhẫn THỰC TẾ (đã chia debugTimeScale) — dùng cho watchdog.</summary>
    private float PatienceSecondsScaled()
    {
        float giay = config != null ? Mathf.Max(1f, config.PatienceSeconds) : 1800f;
        return giay / Mathf.Max(0.01f, EffectiveTimeScale);
    }

    // ─── Persist ────────────────────────────────────────────────────────

    private static string TripKey(int dock) => string.Format(KeyTripFormat, dock);

    private void SaveTrip(int dock)
    {
        Trip trip = _trips[dock];
        if (trip == null || trip.Save == null) return;

        PlayerPrefs.SetString(TripKey(dock), JsonUtility.ToJson(trip.Save));
        LuuGopPrefs.Hen(); // lưu gộp có trễ — cùng cách các manager khác của dự án
    }

    private TripSave LoadTripSave(int dock)
    {
        string json = PlayerPrefs.GetString(TripKey(dock), string.Empty);
        if (string.IsNullOrEmpty(json)) return null;

        TripSave save = null;
        try { save = JsonUtility.FromJson<TripSave>(json); }
        catch (Exception e)
        {
            Debug.LogWarning($"[TouristVisitor] Save chuyến bến {dock + 1} hỏng ({e.Message}) — bỏ qua, dựng chuyến mới.");
            return null;
        }

        // Mảng lệch độ dài = save hỏng/đời cũ → vứt, an toàn hơn là đọc lệch index.
        if (save == null || save.charIdx == null || save.dishId == null ||
            save.served == null || save.timedOut == null || save.patienceEndUtcTicks == null ||
            save.dishId.Length != save.charIdx.Length ||
            save.served.Length != save.charIdx.Length ||
            save.timedOut.Length != save.charIdx.Length ||
            save.patienceEndUtcTicks.Length != save.charIdx.Length)
        {
            Debug.LogWarning($"[TouristVisitor] Save chuyến bến {dock + 1} không hợp lệ — dựng chuyến mới.");
            return null;
        }

        return save;
    }

    private void ClearTripSave(int dock)
    {
        PlayerPrefs.DeleteKey(TripKey(dock));
        LuuGopPrefs.Hen();
    }

    // ─── Scene refs ─────────────────────────────────────────────────────

    /// <summary>
    /// Bổ khuyết reference còn trống bằng cách dò theo TÊN (tool wire là chính, đây là
    /// lưới an toàn khi Sếp đổi hierarchy).
    ///
    /// [QA B-1 lớp ①] Thiếu <see cref="TouristQueue"/> thì KHÔNG đi tiếp với null —
    /// tự dựng một hàng chờ runtime tại chỗ + LogError. Khách sẽ đứng chồng nhau
    /// (xấu) nhưng hệ vẫn chạy đủ vòng, tàu không bao giờ kẹt.
    /// </summary>
    private void EnsureSceneRefs()
    {
        if (queue == null)
            queue = UnityEngine.Object.FindFirstObjectByType<TouristQueue>(FindObjectsInactive.Include);

        if (queue == null)
        {
            if (!_warnedNoQueue)
            {
                _warnedNoQueue = true;
                Debug.LogError("[TouristVisitor] KHÔNG THẤY TouristQueue (QueueAnchor) trong scene! " +
                               "Đã tự dựng một hàng chờ tạm ngay tại TouristSystem để hệ không kẹt tàu — " +
                               "khách sẽ đứng chồng nhau và SAI CHỖ. " +
                               "SỬA: chạy Tools/Farm Game/Tourist Boat/Setup Tourist Visitors (Scene) " +
                               "rồi kéo QueueAnchor ra trước cửa nhà hàng cooking.");
            }
            var go = new GameObject("QueueAnchor(Auto-Fallback)");
            go.transform.SetParent(transform, false);
            queue = go.AddComponent<TouristQueue>();
        }

        if (config != null) queue.Configure(config.queueSpacing);

        if (dockPathRoots == null || dockPathRoots.Length < BoatDockManager.DockCount)
            dockPathRoots = new Transform[BoatDockManager.DockCount];
        if (gangplanks == null || gangplanks.Length < BoatDockManager.DockCount)
            gangplanks = new Transform[BoatDockManager.DockCount];

        for (int i = 0; i < BoatDockManager.DockCount; i++)
        {
            if (dockPathRoots[i] == null)
            {
                GameObject go = GameObject.Find($"TouristPath_Dock{i + 1:00}");
                if (go != null) dockPathRoots[i] = go.transform;
            }
            if (gangplanks[i] == null)
            {
                var mgr = BoatDockManager.Instance;
                Transform berth = mgr != null ? mgr.GetDockBerth(i) : null;
                Transform dock  = berth != null ? berth.parent : null;
                if (dock != null) gangplanks[i] = dock.Find("Gangplank");
            }
        }
    }

    private Transform VisitorsRoot()
    {
        if (visitorsRoot != null) return visitorsRoot;

        Transform t = transform.Find("Visitors");
        if (t == null)
        {
            var go = new GameObject("Visitors");
            go.transform.SetParent(transform, false);
            t = go.transform;
        }
        visitorsRoot = t;
        return visitorsRoot;
    }

    /// <summary>
    /// Đường đi bộ của bến: [đầu gangplank phía bờ, WP_01..WP_n] — sắp theo TÊN
    /// (tên có số 0 đệm nên so chuỗi là đủ đúng thứ tự, giống BoatDockManager).
    /// </summary>
    private Vector3[] GetPathPoints(int dock)
    {
        var points = new List<Vector3>(6);

        // ── Điểm đầu = ĐẦU BỜ của tấm gỗ, không phải tâm ván ──
        // Ván dài 420 unit (sau khi sửa bug "ván bé xíu"), lấy tâm làm điểm đầu thì khách
        // mới đi được nửa ván đã rẽ đi. Đo từ bounds nên ván to nhỏ thế nào cũng đúng.
        bool coDiemDau = false;
        Vector3 diemDau = Vector3.zero;
        if (IsValidDock(dock) && gangplanks[dock] != null)
        {
            Transform gp = gangplanks[dock];
            diemDau = gp.position;

            var gsr = gp.GetComponent<SpriteRenderer>();
            if (gsr != null && gsr.sprite != null)
                diemDau += Vector3.up * (gsr.bounds.size.y * 0.5f); // +Y = hướng vào bờ

            points.Add(diemDau);
            coDiemDau = true;
        }

        Transform root = IsValidDock(dock) ? dockPathRoots[dock] : null;
        if (root != null && root.childCount > 0)
        {
            var wps = new Transform[root.childCount];
            for (int i = 0; i < root.childCount; i++) wps[i] = root.GetChild(i);
            Array.Sort(wps, (a, b) => string.CompareOrdinal(a.name, b.name));

            // Waypoint ĐẦU đường nào còn nằm XA hàng chờ hơn cả đầu ván thì khách đã đi
            // qua rồi — giữ lại sẽ thành đi giật lùi. Chỉ bỏ ở ĐẦU danh sách, gặp waypoint
            // hợp lệ đầu tiên là ngừng bỏ (không đụng tới waypoint giữa/cuối đường).
            int batDau = 0;
            if (boQuaWaypointDaDiQua && coDiemDau && queue != null)
            {
                Vector3 dich = queue.transform.position;
                float xaNhat = KhoangCachPhang(diemDau, dich);

                while (batDau < wps.Length && wps[batDau] != null &&
                       KhoangCachPhang(wps[batDau].position, dich) > xaNhat)
                    batDau++;

                if (batDau > 0)
                    Debug.Log($"[TouristVisitor] Bến {dock + 1}: bỏ qua {batDau} waypoint đầu " +
                              "(nằm sau đầu tấm gỗ — khách đã đi qua rồi). " +
                              "Tắt bằng cờ 'boQuaWaypointDaDiQua' nếu muốn bám đủ waypoint.");
            }

            for (int i = batDau; i < wps.Length; i++)
                if (wps[i] != null) points.Add(wps[i].position);
        }

        if (points.Count == 0)
        {
            Debug.LogWarning($"[TouristVisitor] Bến {dock + 1} chưa có đường đi bộ (Gangplank/TouristPath) — " +
                             "khách sẽ đi thẳng tới hàng chờ. Chạy tool Setup Tourist Visitors (Scene) rồi kéo WP theo đường đất.");
        }
        return points.ToArray();
    }

    /// <summary>Khoảng cách phẳng (bỏ Z) — dùng so sánh "ai gần hàng chờ hơn".</summary>
    private static float KhoangCachPhang(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dy = a.y - b.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsValidDock(int dock)
    {
        return dock >= 0 && dock < BoatDockManager.DockCount;
    }
}
