using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CẦU NỐI THỢ BÚA ↔ NHÀ VILLAGE (<see cref="HouseGrowthController"/>).
/// ═══════════════════════════════════════════════════════════════════
///
/// VÌ SAO PHẢI POLL: <see cref="HouseGrowthController"/> là code CŨ và **KHÔNG có event
/// nào** (§3 CONTRACT). Luật §0.1 cấm sửa file của người khác để thêm event. Vậy nên
/// bridge này đọc <c>State</c>/<c>Progress</c> theo nhịp <b>0.2s</b> — không đọc mỗi
/// frame (bài học §7: <c>UpdateVisuals()</c> chạy 60 lần/giây là nguyên nhân tụt fps).
///
/// ⚠ BUG CÓ THẬT CỦA HỆ NHÀ mà bridge phải đỡ:
///   <c>HouseGrowthController</c> chuyển sang <c>Completed</c> ở giây <b>1.35</b>, trong
///   khi <c>ConstructionCelebrationFX.TotalLife = 3.5s</c> — pháo hoa còn nổ hơn 2 giây
///   nữa. Nếu dismiss thợ ngay lúc thấy <c>Completed</c> thì thợ biến mất giữa lúc pháo
///   hoa đang bay, trông như bug. Vì vậy khi thấy chuyển tiếp
///   <c>ReadyToReveal → Completed</c>, bridge cho thợ ăn mừng THÊM ĐỦ 3.5s rồi mới fade.
///   (Hệ decor mới của DEV-A không có vấn đề này — nó bắn <c>OnRevealFinished</c> đúng lúc.)
///
/// BOUNDS NHÀ: nhà ĐỔI SPRITE theo stage (khung sườn → nhà hoàn thiện) nên bounds phình
/// ra. Bridge so bounds mỗi nhịp poll, lệch &gt; 10% thì gọi
/// <see cref="BuilderWorkerCrew.RefreshLayout"/> — không refresh mỗi nhịp (tốn vô ích).
///
/// ══ HAI VAI CỦA CLASS NÀY ═══════════════════════════════════════════════════════
/// 1. BRIDGE (một thực thể trên MỖI ngôi nhà): poll State, điều khiển tổ thợ của nhà đó.
/// 2. RUNTIME HOST (đúng MỘT thực thể, trên GameObject <c>"HouseWorkerRuntime"</c> ẩn,
///    <c>DontDestroyOnLoad</c>): đi quét scene để gắn bridge, không theo dõi nhà nào.
/// Phân biệt bằng cờ <see cref="IsRuntimeHost"/>. Cố ý gộp vào một class thay vì tách
/// file thứ 6 vì §10 CONTRACT chốt DEV-B đúng 5 file, và §7 cấm 2 class KHÔNG LIÊN QUAN
/// nằm chung file — host này là bootstrap của chính bridge, cùng một chủ đề, cùng một chủ.
///
/// ⚠ VÌ SAO PHẢI CÓ RUNTIME HOST (bug R2 do QA tìm ra):
///   Thứ tự Unity thật là <b>Awake → AfterSceneLoad → Start</b>. Nhà người chơi mua được
///   <c>PlacementManager.Start() → LoadBuildings()</c> Instantiate lại, nên nếu quét scene
///   NGAY trong <c>AfterSceneLoad</c> thì nhà CHƯA TỒN TẠI ⇒ mua Home1 (60s), thoát ở giây
///   20, mở lại thì nhà đang xây mà KHÔNG có thợ nào. Ngoài ra
///   <c>RuntimeInitializeOnLoadMethod</c> chỉ chạy MỘT LẦN cả phiên ⇒ Menu → Farm cũng
///   không quét lại. Runtime host giải quyết cả ba:
///     • chờ 2 frame (<c>yield return null</c> ×2) ⇒ chắc chắn sau mọi <c>Start()</c>
///     • <c>SceneManager.sceneLoaded</c> ⇒ quét lại mỗi lần load scene
///     • quét lại định kỳ mỗi 2 giây ⇒ nhà MUA MỚI trong lúc chơi cũng có thợ (≤2s)
///   KHÔNG quét mỗi frame: <c>FindObjectsByType</c> trên map này rất đắt.
///
/// TỰ CHẠY: <c>[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]</c> chỉ dựng runtime host,
/// và CHỈ khi có <c>Resources/BuilderWorkerConfig.asset</c> với <c>enabled == true</c>.
/// Flag tắt ⇒ host không tồn tại ⇒ không có hook <c>sceneLoaded</c>, không coroutine,
/// không quét scene, không cấp phát gì — game chạy y như trước (§9 CONTRACT).
///
/// BONUS: bridge cũng nghe <see cref="DecorGrowthBootstrap.OnControllerSpawned"/> để gắn
/// crew cho decor MỚI ĐẶT. Đặt ở đây là đúng chỗ vì DEV-A không được biết gì về thợ búa.
///
/// [Worker]
/// </summary>
[DisallowMultipleComponent]
public class HouseWorkerBridge : MonoBehaviour
{
    /// <summary>Nhịp poll (giây). 0.2s = 5 lần/giây, đủ mượt cho việc đổi mode.</summary>
    private const float POLL_INTERVAL = 0.2f;

    /// <summary>Khớp <c>ConstructionCelebrationFX.TotalLife</c> — thợ ăn mừng tới hết pháo hoa.</summary>
    public const float CELEBRATE_HOLD_SECONDS = 3.5f;

    /// <summary>
    /// Nhịp quét lại scene (giây) để bắt nhà MUA MỚI trong lúc chơi. 2s là đủ nhanh cho
    /// mắt vì nhà vừa đặt xong còn đang animation, mà vẫn không giết hiệu năng —
    /// <c>FindObjectsByType</c> mỗi frame trên map này là bug hiệu năng nặng.
    /// </summary>
    public const float RESCAN_INTERVAL_SECONDS = 2f;

    /// <summary>Tên GameObject của runtime host (QA tìm trong Hierarchy bằng tên này).</summary>
    public const string RUNTIME_HOST_NAME = "HouseWorkerRuntime";

    /// <summary>Tên asset trong Resources — Editor Tool của DEV-D phải lưu ĐÚNG tên này.</summary>
    public const string RESOURCE_NAME = "BuilderWorkerConfig";

    // ── Static: cấu hình + hook decor + runtime host ─────────────────────────
    private static BuilderWorkerConfig _cachedConfig;
    private static bool _configLookedUp;
    private static bool _decorHooked;
    private static HouseWorkerBridge _runtimeHost;

    /// <summary>
    /// Cấu hình dùng chung, tra một lần rồi cache (kể cả khi tra ra null — tránh
    /// Resources.Load mỗi lần gọi). Có thể null: bên gọi PHẢI null-check.
    /// </summary>
    public static BuilderWorkerConfig ResolvedConfig
    {
        get
        {
            if (!_configLookedUp)
            {
                _configLookedUp = true;
                _cachedConfig = Resources.Load<BuilderWorkerConfig>(RESOURCE_NAME);
            }
            return _cachedConfig;
        }
    }

    /// <summary>Bắt tra lại cấu hình (Editor Tool vừa tạo asset xong thì gọi cái này).</summary>
    public static void InvalidateConfigCache()
    {
        _configLookedUp = false;
        _cachedConfig = null;
    }

    // ── Instance ─────────────────────────────────────────────────────────────
    private HouseGrowthController _house;
    private BuilderWorkerConfig   _cfg;
    private BuilderWorkerCrew     _crew;

    private HouseGrowthController.GrowthState _prevState;
    private bool  _prevStateValid;

    private float _pollTimer;
    private Bounds _lastBounds;
    private bool   _hasBounds;

    private Coroutine _holdCo;
    private bool _finished;

    // ── Runtime host (vai 2) ─────────────────────────────────────────────────
    private bool      _isRuntimeHost;
    private bool      _sceneHookAttached;
    private Coroutine _scanCo;

    /// <summary>Nhà mà bridge này đang theo dõi.</summary>
    public HouseGrowthController House => _house;

    /// <summary>Tổ thợ đang gắn (có thể null nếu nhà đã Completed từ đầu).</summary>
    public BuilderWorkerCrew Crew => _crew;

    /// <summary>true = thực thể này là RUNTIME HOST (đi quét scene), không theo dõi nhà nào.</summary>
    public bool IsRuntimeHost => _isRuntimeHost;

    // ── Boot tự động ─────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBoot()
    {
        BuilderWorkerConfig cfg = ResolvedConfig;
        if (cfg == null || !cfg.enabled) return;   // FEATURE FLAG — không dựng gì, không hook gì

        // CỐ Ý KHÔNG quét scene ở đây: AfterSceneLoad chạy TRƯỚC Start(), nhà do người
        // chơi mua chưa được PlacementManager.LoadBuildings() Instantiate (bug R2).
        EnsureRuntimeHost(cfg);
        HookDecorBootstrap();
    }

    /// <summary>
    /// Dựng (hoặc trả lại) RUNTIME HOST — một GameObject <c>DontDestroyOnLoad</c> ẩn chịu
    /// trách nhiệm quét scene. Trả null nếu feature flag tắt: khi đó KHÔNG có object nào,
    /// KHÔNG có hook <c>sceneLoaded</c>, không rò rỉ gì (§9 CONTRACT).
    /// </summary>
    public static HouseWorkerBridge EnsureRuntimeHost(BuilderWorkerConfig cfg)
    {
        if (cfg == null || !cfg.enabled) return null;
        if (_runtimeHost != null) return _runtimeHost;

        GameObject go = new GameObject(RUNTIME_HOST_NAME);
        go.hideFlags = HideFlags.HideInHierarchy;   // ẩn — không làm rác Hierarchy của Sếp
        Object.DontDestroyOnLoad(go);

        HouseWorkerBridge host = go.AddComponent<HouseWorkerBridge>();

        // ⚠ AddComponent gọi Awake + OnEnable NGAY, TRƯỚC khi ta kịp gán cờ bên dưới ⇒
        // OnEnable không thể tự gắn hook. Vì thế gán cờ rồi gọi tay 2 việc khởi động.
        host._isRuntimeHost = true;
        host._cfg           = cfg;
        _runtimeHost        = host;

        host.AttachSceneHook();
        host.BatDauQuetDinhKy();

        return host;
    }

    /// <summary>
    /// Quét scene, gắn bridge cho MỌI <see cref="HouseGrowthController"/> chưa có.
    /// An toàn khi gọi nhiều lần (idempotent). cfg null/!enabled ⇒ return ngay.
    /// </summary>
    public static void EnsureOnAllHouses(BuilderWorkerConfig cfg)
    {
        if (cfg == null || !cfg.enabled) return;

        // Unity 6 API — KHÔNG dùng FindObjectsOfType (deprecated), §3 CONTRACT
        HouseGrowthController[] all =
            Object.FindObjectsByType<HouseGrowthController>(FindObjectsSortMode.None);

        if (all == null) return;

        int gan = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (all[i].GetComponent<HouseWorkerBridge>() != null) continue;
            if (AttachTo(all[i], cfg) != null) gan++;
        }

        if (gan > 0) Debug.Log($"[Worker] Đã gắn thợ búa cho {gan} ngôi nhà village.");
    }

    /// <summary>
    /// Gắn bridge cho MỘT ngôi nhà. Trả bridge có sẵn nếu đã gắn; null nếu
    /// nhà null hoặc feature flag tắt.
    /// </summary>
    public static HouseWorkerBridge AttachTo(HouseGrowthController house, BuilderWorkerConfig cfg)
    {
        if (house == null) return null;
        if (cfg == null || !cfg.enabled) return null;   // §9 CONTRACT

        HouseWorkerBridge b = house.GetComponent<HouseWorkerBridge>();
        if (b != null) return b;

        b = house.gameObject.AddComponent<HouseWorkerBridge>();
        b._house = house;
        b._cfg   = cfg;
        return b;
    }

    /// <summary>
    /// Nghe decor mới đặt xuống của DEV-A. Idempotent — gọi nhiều lần không nhân bản
    /// đăng ký (quan trọng vì domain reload trong Editor chạy lại AutoBoot).
    /// </summary>
    public static void HookDecorBootstrap()
    {
        if (_decorHooked) return;
        _decorHooked = true;

        DecorGrowthBootstrap.OnControllerSpawned -= HandleDecorSpawned;
        DecorGrowthBootstrap.OnControllerSpawned += HandleDecorSpawned;
    }

    private static void HandleDecorSpawned(DecorGrowthController ctrl)
    {
        if (ctrl == null) return;

        BuilderWorkerConfig cfg = ResolvedConfig;
        if (cfg == null || !cfg.enabled) return;

        // Crew tự subscribe 4 event của ctrl bên trong AttachTo — ở đây không làm gì thêm
        BuilderWorkerCrew.AttachTo(ctrl.gameObject, ctrl.VisualBounds, cfg);
    }

    // ── Vòng đời instance ────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Hook chỉ tồn tại trên runtime host ⇒ flag tắt thì host không được dựng ⇒
        // KHÔNG có đăng ký sceneLoaded nào tồn tại. Đây là chốt chống rò rỉ của §9.
        if (_isRuntimeHost) AttachSceneHook();
    }

    private void OnDisable()
    {
        DetachSceneHook();
    }

    private void AttachSceneHook()
    {
        if (_sceneHookAttached) return;
        _sceneHookAttached = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void DetachSceneHook()
    {
        if (!_sceneHookAttached) return;
        _sceneHookAttached = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    /// <summary>
    /// Mỗi lần load scene (Menu → Farm) phải quét lại: RuntimeInitializeOnLoadMethod chỉ
    /// chạy một lần cả phiên nên không thể trông vào nó.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_isRuntimeHost) return;

        if (_cfg == null) _cfg = ResolvedConfig;
        if (_cfg == null || !_cfg.enabled) return;

        BatDauQuetDinhKy();   // khởi động lại: chờ 2 frame rồi quét, sau đó lặp mỗi 2s
    }

    private void BatDauQuetDinhKy()
    {
        if (!_isRuntimeHost) return;
        if (_scanCo != null) StopCoroutine(_scanCo);
        _scanCo = StartCoroutine(QuetDinhKyRoutine());
    }

    /// <summary>
    /// Chờ 2 frame cho mọi <c>Start()</c> chạy xong (kể cả
    /// <c>PlacementManager.LoadBuildings()</c>), quét lần đầu, rồi quét lại mỗi
    /// <see cref="RESCAN_INTERVAL_SECONDS"/> giây để bắt nhà mua mới.
    /// </summary>
    private IEnumerator QuetDinhKyRoutine()
    {
        // 2 frame, không phải 1: frame 1 mới hết Start(), frame 2 cho chắc với các
        // manager tự Instantiate trong Start của nhau.
        yield return null;
        yield return null;

        while (true)
        {
            if (_cfg == null) _cfg = ResolvedConfig;
            if (_cfg != null && _cfg.enabled) EnsureOnAllHouses(_cfg);

            // Đếm bằng unscaledDeltaTime: popup mở (timeScale 0) vẫn phải bắt được nhà mới.
            float t = 0f;
            while (t < RESCAN_INTERVAL_SECONDS)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private void Start()
    {
        // Runtime host không theo dõi nhà nào — coroutine quét đã chạy từ EnsureRuntimeHost
        if (_isRuntimeHost)
        {
            // KHÔNG restart nếu coroutine đã chạy từ EnsureRuntimeHost — restart sẽ reset
            // lại đồng hồ chờ 2 frame một cách vô ích. Chỉ vá nếu vì lý do nào đó chưa có.
            if (_scanCo == null) BatDauQuetDinhKy();
            return;
        }

        if (_house == null) _house = GetComponent<HouseGrowthController>();
        if (_cfg == null)   _cfg   = ResolvedConfig;

        if (_house == null || _cfg == null || !_cfg.enabled)
        {
            enabled = false;
            return;
        }

        _prevState      = _house.State;
        _prevStateValid = true;

        // Đồng bộ ngay trạng thái ban đầu (nhà load save có thể đang ở giữa ván)
        ApDungState(_house.State, _house.State, true);
    }

    private void Update()
    {
        if (_isRuntimeHost) return;   // host chỉ chạy coroutine quét, không poll nhà nào
        if (_finished) return;

        _pollTimer += Time.deltaTime;
        if (_pollTimer < POLL_INTERVAL) return;   // THROTTLE — không đọc mỗi frame

        // TRỪ chứ không GÁN 0: gán 0 làm nhịp poll trôi thành ~0.217s ở 60fps (dt=1/60
        // không chia hết cho 0.2) — cộng dồn thành trễ nửa giây sau vài giây. Trừ đi thì
        // tần số poll trung bình đúng 5 Hz.
        _pollTimer -= POLL_INTERVAL;
        if (_pollTimer > POLL_INTERVAL) _pollTimer = 0f;   // tụt frame nặng → không dồn nợ

        if (_house == null)
        {
            enabled = false;
            return;
        }

        HouseGrowthController.GrowthState cur = _house.State;

        if (!_prevStateValid)
        {
            _prevState = cur;
            _prevStateValid = true;
            ApDungState(cur, cur, true);
        }
        else if (cur != _prevState)
        {
            HouseGrowthController.GrowthState prev = _prevState;
            _prevState = cur;
            ApDungState(prev, cur, false);
        }

        // Progress được đọc để bounds/stage đồng bộ — nhà đổi sprite theo Progress
        CapNhatBoundsNeuDoi();
    }

    private void OnDestroy()
    {
        DetachSceneHook();
        StopAllCoroutines();
        _holdCo = null;
        _scanCo = null;

        if (_runtimeHost == this) _runtimeHost = null;
    }

    // ── Lõi state machine ────────────────────────────────────────────────────

    private void ApDungState(HouseGrowthController.GrowthState prev,
                             HouseGrowthController.GrowthState cur,
                             bool khoiTao)
    {
        switch (cur)
        {
            case HouseGrowthController.GrowthState.Building:
                DamBaoCoCrew();
                if (_crew != null) _crew.SetHammering();
                break;

            case HouseGrowthController.GrowthState.ReadyToReveal:
                DamBaoCoCrew();
                if (_crew != null) _crew.SetIdleAtGift();
                break;

            case HouseGrowthController.GrowthState.Completed:
                if (!khoiTao && prev == HouseGrowthController.GrowthState.ReadyToReveal)
                {
                    // Người chơi VỪA mở hộp quà. Nhà nhảy sang Completed ở 1.35s nhưng
                    // pháo hoa còn tới 3.5s → thợ phải ăn mừng ĐỦ 3.5s mới được đi.
                    DamBaoCoCrew();
                    if (_crew != null) _crew.SetCelebrating();

                    if (_holdCo != null) StopCoroutine(_holdCo);

                    // BÙ ĐỘ TRỄ POLL: vì đọc State mỗi 0.2s, lúc phát hiện được thì việc
                    // đã xảy ra đâu đó trong 0.2s vừa rồi. Lấy điểm giữa (0.1s) làm ước
                    // lượng để tổng thời gian ăn mừng tính từ LÚC THẬT vẫn ≈ 3.5s, không
                    // bị cộng thêm cả cửa sổ poll (sandbox đo được lệch tới 0.2s).
                    float hold = Mathf.Max(0f, CELEBRATE_HOLD_SECONDS - POLL_INTERVAL * 0.5f);
                    _holdCo = StartCoroutine(AnMungRoiRutQuan(hold));
                }
                else
                {
                    // Nhà đã xong từ trước (load save) hoặc rush thẳng → không cần thợ
                    KetThuc();
                }
                break;
        }
    }

    /// <summary>Ăn mừng thêm <paramref name="giay"/> giây rồi cho cả tổ fade-out.</summary>
    private IEnumerator AnMungRoiRutQuan(float giay)
    {
        // unscaledDeltaTime: pháo hoa là FX, không được đứng im nếu ai đó mở popup (§0.6)
        float t = 0f;
        while (t < giay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _holdCo = null;
        KetThuc();
    }

    private void KetThuc()
    {
        _finished = true;

        if (_crew != null)
        {
            _crew.DismissWithFade();
            _crew = null;
        }

        enabled = false;   // hết việc → thôi poll
    }

    // ── Crew & bounds ────────────────────────────────────────────────────────

    private void DamBaoCoCrew()
    {
        if (_crew != null) return;

        Bounds b;
        if (!TryLayBoundsNha(out b)) return;   // chưa có sprite → nhịp poll sau thử lại

        _crew = BuilderWorkerCrew.AttachTo(_house.gameObject, b, _cfg);
        if (_crew == null) return;             // feature flag tắt

        _lastBounds = b;
        _hasBounds  = true;
    }

    /// <summary>
    /// Nhà đổi sprite theo stage nên bounds phình ra — xếp lại thợ khi lệch đáng kể.
    /// Ngưỡng lấy từ <see cref="BuilderWorkerCrew.BoundsChangedSignificantly"/> (NGUỒN DUY
    /// NHẤT) để đường nhà village và đường decor không bao giờ hành xử khác nhau.
    /// </summary>
    private void CapNhatBoundsNeuDoi()
    {
        if (_crew == null) return;

        Bounds b;
        if (!TryLayBoundsNha(out b)) return;

        if (!_hasBounds)
        {
            _lastBounds = b;
            _hasBounds  = true;
            _crew.RefreshLayout(b);
            return;
        }

        if (!_crew.RefreshLayoutIfChanged(b)) return;
        _lastBounds = b;
    }

    /// <summary>
    /// Bounds nhà từ SpriteRenderer. Lấy renderer trên CHÍNH nhà trước; nếu không có
    /// thì quét con nhưng BỎ QUA mọi renderer nằm trong "BuilderCrew" — nếu không sẽ
    /// đo chính con thợ và thợ tự đẩy nhau ra xa mỗi nhịp poll.
    /// </summary>
    private bool TryLayBoundsNha(out Bounds b)
    {
        b = new Bounds(transform.position, Vector3.zero);
        if (_house == null) return false;

        SpriteRenderer sr = _house.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            b = sr.bounds;
            return b.size.x > 0.0001f && b.size.y > 0.0001f;
        }

        SpriteRenderer[] ds = _house.GetComponentsInChildren<SpriteRenderer>(true);
        if (ds == null) return false;

        for (int i = 0; i < ds.Length; i++)
        {
            SpriteRenderer r = ds[i];
            if (r == null || r.sprite == null) continue;
            if (r.GetComponentInParent<BuilderWorkerCrew>() != null) continue;   // bỏ qua thợ

            b = r.bounds;
            return b.size.x > 0.0001f && b.size.y > 0.0001f;
        }

        return false;
    }
}
