using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MÁY TRẠNG THÁI của MỘT khách du lịch (GDD BOAT-002 §3.3).
///
///   Disembark   — đi từ tàu qua tấm gỗ (gangplank) xuống bờ
///   WalkPath    — đi theo waypoint TouristPath_Dock0X (đường đất Sếp vẽ)
///   QueueSlot   — đi tới slot của mình trong hàng chờ
///   WaitServe   — đứng chờ; bubble mở (theo lượt do manager điều phối) + đồng hồ kiên nhẫn UTC
///   Served/TimedOut — mặt cười 0.5s (rồi bay lên HUD) / mặt TỨC GIẬN 2s
///   WalkBack    — đi ngược path về đầu gangplank (lệch làn để không đi xuyên khách đang lên)
///   Board       — lên tàu → despawn, báo manager
///
/// ── SỬA THEO QA + SẾP CHỐT 2026-08-29 ──────────────────────────────────────
///   [Sếp 1] Bubble mở cho MỌI khách trong hàng, lần lượt cách nhau
///           <c>bubbleStaggerDelay</c> (manager cấp lượt qua TakeBubbleStaggerDelay).
///           ⇒ đồng hồ kiên nhẫn chạy SONG SONG cho mọi khách (QA M-2), và khách nào
///           cũng tap giao được — không bắt buộc theo thứ tự hàng.
///   [Sếp 2] Hết kiên nhẫn hiện mặt TỨC GIẬN (Angry), không phải mặt buồn.
///   [QA B-2] Mốc kiên nhẫn CHIA <c>debugTimeScale</c> lúc ĐẶT (không chia lúc so sánh)
///           để mốc UTC đã persist vẫn đúng sau khi tắt/mở game.
///   [QA B-1] Không còn phụ thuộc "đầu hàng" để chạy đồng hồ ⇒ thiếu TouristQueue
///           KHÔNG làm khách kẹt vĩnh viễn. Thêm <see cref="ForceLeaveAngry"/> cho
///           lưới an toàn OnDockTimeoutForced của Dev A.
///   [QA M-3] Dồn hàng KHÔNG cướp mục tiêu của khách đang đi bộ trên đường đất
///           (cấm quay đầu ngược waypoint) — chỉ nhận slot mới khi đã ở khu hàng chờ.
///   [QA m-9] Hướng đứng chờ suy từ vị trí hàng chờ, không cứng Vector2.up.
///
/// LUẬT KỸ THUẬT:
///   • Di chuyển bằng Vector3.MoveTowards trên danh sách điểm — KHÔNG physics
///     (giống LivestockAI: transform thuần, không Rigidbody2D).
///   • Animator param: DirX (float), DirY (float), IsMoving (bool) — khớp
///     AnimatorController do NPCAnimationSetupTool sinh. Hướng SNAP về 4 hướng chính.
///   • sortingOrder theo Y như LivestockAI, có KẸP biên (map toạ độ lớn dễ tràn ±32767).
///   • Đồng hồ kiên nhẫn dùng DateTime.UtcNow.Ticks TUYỆT ĐỐI (chạy cả khi offline).
///   • Tap khách: OnMouseUpAsButton (pattern Collider2D của BoatDockSlot/TrainWagonSlot).
/// </summary>
[RequireComponent(typeof(SortingGroup))]
public class TouristAgent : MonoBehaviour
{
    /// <summary>Các pha của một khách.</summary>
    public enum AgentState
    {
        Idle = 0,
        Disembarking = 1,
        WalkingPath  = 2,
        WalkingToSlot = 3,
        WaitingServe = 4,
        Happy        = 5,
        Angry        = 6,
        WalkingBack  = 7,
        Boarding     = 8,
        Done         = 9,
    }

    [Header("Sorting (khách phải nổi trên decor — yêu cầu Sếp)")]
    // [BUG Sếp gặp lúc Play test 2026-08-29] Bản đầu để "CongTrinh" (chép từ LivestockAI)
    // — layer đó KHÔNG có trong project, Unity im lặng đẩy renderer về Default ⇒ khách bị
    // cây/nhà che kín. Nay để TRỐNG = tự giải theo TouristSortingLayers.Visitor
    // (Objects → Default) và CẢNH BÁO nếu phải rơi dự phòng.
    // [SUA 2026-09-03 - DEV-3, Sep duyet] Comment/Tooltip ban cu ghi NHAM "ObjectsFront"
    // la lua chon khuyen nghi -- SAI so voi code that (TouristSortingLayers.Visitor thuc te
    // la {"Objects","Default"}) va la CAI BAY: "ObjectsFront" la layer CUA TAU
    // (TrainPathFollower, order 650). Go tay "ObjectsFront" vao field ben duoi se khien
    // khach (baseSortingOrder 5000) de LEN TREN tau ngay lap tuc.
    // TUYET DOI KHONG dat "ObjectsFront" vao sortingLayerName.
    [Tooltip("Sorting layer của khách. ĐỂ TRỐNG = tự chọn 'Objects' (khuyến nghị, đúng với code thật). " +
             "TUYỆT ĐỐI KHÔNG gõ 'ObjectsFront' — đó là layer của TÀU (TrainPathFollower, order 650); " +
             "đặt vào đây sẽ khiến khách đè lên tàu. Gõ tên khác chỉ khi layer đó CÓ THẬT trong " +
             "Project Settings > Tags and Layers và KHÔNG PHẢI 'ObjectsFront'.")]
    [SerializeField] private string sortingLayerName = "";

    [Tooltip("Order gốc — cộng thêm phần tính theo Y. Đặt CAO hơn decor để khách không bị che.")]
    [SerializeField] private int baseSortingOrder = 5000;

    [Tooltip("Hệ số đổi Y world → sortingOrder. Map dùng toạ độ RẤT lớn (bến cách nhau ~740 unit) " +
             "nên hệ số phải nhỏ, không thì tràn giới hạn sorting order của Unity (±32767).")]
    [SerializeField] private float ySortFactor = 0.5f;

    [Tooltip("Biên kẹp phần Y-sort — giữ tổng order trong khoảng an toàn của Unity.")]
    [SerializeField] private int ySortClamp = 8000;

    // [LUOI AN TOAN 2026-09-03 - DEV-3, Sep duyet] Tau (TrainPathFollower) co dinh layer
    // "ObjectsFront", order 650-660 (TrainPathFollower.cs -- CHI DOC, khong sua). Neu tuong
    // lai ai do lo go "ObjectsFront" vao sortingLayerName phia tren (bat chap canh bao
    // Tooltip), khach se roi vao layer cua tau -- kep tran order duoi day dam bao khach van
    // KHONG de len tau trong tinh huong do. Hien khach dang o layer "Objects" (khac layer
    // tau "ObjectsFront") nen dieu kien kep KHONG kich hoat -- hanh vi runtime hien tai
    // KHONG doi.
    private const int TrainSortingOrderRef = 650; // khop TrainPathFollower.TrainSortingOrder

    [Tooltip("Lưới an toàn: nếu sortingLayerName bị đặt trùng layer của TÀU ('ObjectsFront'), " +
             "kẹp trần sortingOrder của khách xuống dưới tàu để không đè lên tàu. " +
             "Bật mặc định (revert bằng cách bỏ tick).")]
    [SerializeField] private bool clampBelowTrain = true;

    [Header("Ngưỡng di chuyển")]
    [Tooltip("Khoảng cách coi như 'đã tới điểm' (unit world). Map lớn nên số này không thể là 0.05.")]
    [SerializeField] private float arriveThreshold = 6f;

    [Tooltip("Độ lệch làn khi ĐI VỀ tàu (unit world) — để luồng khách về không đi xuyên " +
             "luồng khách đang lên bờ (QA/Sếp: polish 2026-08-29). Đặt 0 để tắt.")]
    [SerializeField] private float walkBackLaneOffset = 26f;

    [Header("Fade (GIÂY THỰC — cố ý KHÔNG chia debugTimeScale, cho khớp nhau)")]
    [Tooltip("Giây mờ-dần-hiện khi khách vừa xuống tàu. Đối xứng với fade-out lúc lên tàu " +
             "(0.25s) để hai đầu vòng đời trông cùng một nhịp.")]
    [SerializeField] private float fadeInSeconds = 0.25f;

    [Tooltip("Giây mờ-dần-tắt khi khách lên tàu xong.")]
    [SerializeField] private float fadeOutSeconds = 0.25f;

    [Header("Thời lượng cảm xúc (GIÂY THỰC — cố ý KHÔNG chia debugTimeScale)")]
    [Tooltip("Giữ mặt cười trước khi bay lên HUD (GDD §3.3: 0.5s).")]
    [SerializeField] private float happyHoldSeconds = 0.5f;

    [Tooltip("Giữ mặt tức giận trước khi bỏ về (GDD §3.3: 2s).")]
    [SerializeField] private float angryHoldSeconds = 2f;

    [Tooltip("Giữ mặt tức giận khi bị LƯỚI AN TOÀN ép rời bến — ngắn, vì Dev A chỉ cho 3s ân hạn.")]
    [SerializeField] private float forcedAngryHoldSeconds = 0.4f;

    // ─── Runtime ────────────────────────────────────────────────────────

    private TouristVisitorManager _manager;
    private TouristBoatConfig     _config;
    private TouristQueue          _queue;
    private TouristRequestBubble  _bubble;
    private Animator              _animator;
    private SpriteRenderer[]      _renderers;   // MỌI renderer con — nhân vật có thể nhiều mảnh
    private SortingGroup          _sortingGroup;

    // Fade-in lúc vừa xuống tàu: chạy SONG SONG với bước đi đầu tiên (không chặn state machine).
    private float _fadeInConLai = -1f;

    private Vector3[] _pathPoints;      // [gangplankEnd, WP_01..WP_n] — hàng chờ nối sau
    private int       _pathIndex;
    private Vector3   _target;
    private bool      _hasTarget;

    private int   _slotIndex = -1;
    private bool  _isFront;
    private float _stateTimer;
    private float _speed = 150f;
    private float _bubbleOpenAt = -1f;   // Time.time được phép mở bubble (lượt do manager cấp)
    private float _angryHold;            // thời lượng giữ mặt giận của lần này

    // ─── Procedural Living Animation ─────────────────────────────────────
    private Vector3 _baseLocalScale = Vector3.one;
    private Vector3 _logicalWorldPos;
    private float   _motionSeed;
    private bool    _hasBaseScale;

    // Tên layer đã GIẢI XONG (có thật trong project) — tính 1 lần trong Awake, không
    // gọi lại mỗi frame trong UpdateDynamicSorting.
    private string _layerDaGiai;

    // [BUG Sếp gặp 2026-08-29] Controller hỏng (thiếu statemachine) làm mọi lời gọi
    // Animator.SetBool/SetFloat ném "Animator has not been initialized" — 60 lần/giây ×
    // số khách ⇒ Console ngập hàng nghìn dòng, che hết log thật. Cờ này để cảnh báo
    // ĐÚNG MỘT LẦN cho mỗi khách rồi im lặng bỏ qua.
    private bool _daCanhBaoAnimator;

    // [LUOI AN TOAN 2026-09-03 - DEV-3] Canh bao DUNG MOT LAN cho khach nay neu phat hien
    // sortingLayerName trung layer cua TAU ("ObjectsFront") -- khong spam moi frame.
    private bool _daCanhBaoObjectsFront;

    private static readonly int HashDirX     = Animator.StringToHash("DirX");
    private static readonly int HashDirY     = Animator.StringToHash("DirY");
    private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");

    // ─── Thông tin chuyến (manager quản, agent chỉ đọc/báo lại) ─────────

    /// <summary>Index của khách trong chuyến (0..n-1) — khớp index mảng trong save.</summary>
    public int VisitorIndex { get; private set; }

    /// <summary>Bến của chuyến này (0-2).</summary>
    public int DockIndex { get; private set; }

    /// <summary>Món khách yêu cầu (dishId của DishData).</summary>
    public string DishId { get; private set; }

    /// <summary>Asset món khách yêu cầu — manager cần để tính thưởng.</summary>
    public DishData Dish { get; private set; }

    /// <summary>Trạng thái hiện tại.</summary>
    public AgentState State { get; private set; } = AgentState.Idle;

    /// <summary>Hạn kiên nhẫn (UTC ticks). 0 = bubble chưa mở nên chưa tính giờ.</summary>
    public long PatienceEndUtcTicks { get; private set; }

    /// <summary>Đã được phục vụ chưa (để manager ghi save).</summary>
    public bool WasServed { get; private set; }

    /// <summary>Đã hết kiên nhẫn chưa (để manager ghi save).</summary>
    public bool WasTimedOut { get; private set; }

    /// <summary>
    /// Đang ở pha có thể nhận món. [Sếp chốt] KHÔNG còn đòi đứng đầu hàng — chỉ cần
    /// khách đang đứng chờ và bubble ĐÃ MỞ (người chơi nhìn thấy món mới tap được).
    /// </summary>
    public bool CanReceiveDish =>
        State == AgentState.WaitingServe && !WasServed && !WasTimedOut &&
        _bubble != null && _bubble.IsRequesting;

    /// <summary>
    /// Khách này có đang ĐỨNG ĐẦU hàng không. [Sếp chốt 2026-08-29] Giá trị này KHÔNG
    /// còn quyết định việc mở bubble hay nhận món nữa (mọi khách đều mở bubble và đều
    /// tap giao được) — giữ lại vì hàng chờ vẫn dồn theo slot, và để soi trạng thái
    /// lúc debug/QA.
    /// </summary>
    public bool IsFrontOfQueue => _isFront;

    /// <summary>Slot hiện tại trong hàng chờ (-1 = chưa xếp hàng).</summary>
    public int QueueSlot => _slotIndex;

    /// <summary>Tên sorting layer đang THỰC SỰ dùng (đã giải fallback) — tool chẩn đoán đọc.</summary>
    public string SortingLayerResolved => _layerDaGiai;

    /// <summary>Đang đứng chờ nhưng bubble chưa kịp mở (còn trong nhịp stagger 0.4s).</summary>
    public bool IsWaitingBubble =>
        State == AgentState.WaitingServe && !WasServed && !WasTimedOut &&
        (_bubble == null || _bubble.State == TouristRequestBubble.BubbleState.Hidden);

    // ─── Unity lifecycle ────────────────────────────────────────────────

    private void Awake()
    {
        _animator     = GetComponentInChildren<Animator>(true);
        _bubble       = GetComponent<TouristRequestBubble>();
        _sortingGroup = GetComponent<SortingGroup>();
        if (_sortingGroup == null)
            _sortingGroup = gameObject.AddComponent<SortingGroup>();

        // Lưu scale và vị trí ban đầu cho procedural animation
        _baseLocalScale  = transform.localScale;
        _hasBaseScale    = true;
        _motionSeed      = Random.Range(0f, 100f);
        _logicalWorldPos = transform.position;

        // Giải layer NGAY lúc Awake: prefab đời cũ có thể còn lưu tên layer không tồn tại,
        // và runtime phải sửa được để khách không bị chìm dưới decor.
        _layerDaGiai = TouristSortingLayers.ResolveOrOverride(sortingLayerName, TouristSortingLayers.Visitor);

        // Ép luôn cho mọi SpriteRenderer con (prefab cũ ghi sẵn layer Default trong .prefab).
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].sortingLayerName = _layerDaGiai;

        _angryHold = angryHoldSeconds;
    }

    private void Update()
    {
        UpdateDynamicSorting();
        TickFadeIn();

        switch (State)
        {
            case AgentState.Disembarking:  TickDisembark();  break;
            case AgentState.WalkingPath:   TickWalkPath();   break;
            case AgentState.WalkingToSlot: TickWalkToSlot(); break;
            case AgentState.WaitingServe:  TickWaitServe();  break;
            case AgentState.Happy:         TickHappy();      break;
            case AgentState.Angry:         TickAngry();      break;
            case AgentState.WalkingBack:   TickWalkBack();   break;
            case AgentState.Boarding:      TickBoarding();   break;
        }
    }

    private void LateUpdate()
    {
        ApplyLivingMotion();
    }

    /// <summary>
    /// Hiệu ứng nhún nhảy, thở, nghiêng người và bước chân sinh động bằng code (procedural animation).
    /// </summary>
    private void ApplyLivingMotion()
    {
        if (!_hasBaseScale)
        {
            _baseLocalScale = transform.localScale;
            _hasBaseScale = true;
        }

        float timeVal = Time.time;
        float squashX = 1f;
        float stretchY = 1f;
        float offsetY = 0f;
        float rotZ = 0f;

        bool isMoving = _hasTarget;

        if (State == AgentState.Happy)
        {
            // Nhảy cẫng lên vui mừng (Happy bounce)
            float hop = Mathf.Abs(Mathf.Sin(timeVal * 15f)) * 8f;
            offsetY = hop;
            stretchY = 1f + 0.15f * (hop / 8f);
            squashX  = 1f - 0.08f * (hop / 8f);
            rotZ = Mathf.Sin(timeVal * 15f) * 5f;
        }
        else if (State == AgentState.Angry)
        {
            // Giậm chân tức giận rung người
            float shakeX = Mathf.Sin(timeVal * 38f) * 3f;
            transform.position = _logicalWorldPos + new Vector3(shakeX, 0f, 0f);
            transform.localScale = new Vector3(_baseLocalScale.x * 1.05f, _baseLocalScale.y * 0.95f, _baseLocalScale.z);
            transform.localRotation = Quaternion.identity;
            return;
        }
        else if (isMoving)
        {
            // Nhịp bước chân khi đi bộ (Walk step bounce)
            float step = Mathf.Abs(Mathf.Sin(timeVal * 11f + _motionSeed)) * 3.5f;
            offsetY = step;
            stretchY = 1f + 0.03f * (step / 3.5f);
            squashX  = 1f - 0.02f * (step / 3.5f);
            rotZ = Mathf.Sin(timeVal * 11f + _motionSeed) * 2.2f;
        }
        else
        {
            // Đứng chờ (Idle Breathing & Soft Body Swaying): nhún thở tự nhiên + nghiêng nhẹ
            float breathPhase = Mathf.Sin(timeVal * 3.0f + _motionSeed);
            stretchY = 1f + 0.04f * breathPhase;
            squashX  = 1f - 0.025f * breathPhase;
            offsetY  = Mathf.Abs(breathPhase) * 1.8f;
            rotZ     = Mathf.Sin(timeVal * 1.3f + _motionSeed * 1.5f) * 1.8f;
        }

        transform.position = _logicalWorldPos + new Vector3(0f, offsetY, 0f);
        transform.localScale = new Vector3(_baseLocalScale.x * squashX, _baseLocalScale.y * stretchY, _baseLocalScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    // ─── Khởi tạo (manager gọi) ─────────────────────────────────────────

    /// <summary>
    /// Nạp dữ liệu khách. <paramref name="pathPoints"/> = [đầu gangplank phía bờ,
    /// WP_01..WP_n]; hàng chờ là điểm cuối tự tính theo slot.
    /// </summary>
    public void Setup(TouristVisitorManager manager, TouristBoatConfig config, TouristQueue queue,
                      int dockIndex, int visitorIndex, DishData dish,
                      Vector3[] pathPoints, long patienceEndUtcTicks)
    {
        _manager      = manager;
        _config       = config;
        _queue        = queue;
        DockIndex     = dockIndex;
        VisitorIndex  = visitorIndex;
        Dish          = dish;
        DishId        = dish != null ? dish.dishId : string.Empty;
        _pathPoints   = pathPoints;
        PatienceEndUtcTicks = patienceEndUtcTicks;

        if (config != null)
        {
            _speed = Mathf.Max(1f, config.visitorWalkSpeed);
            if (_bubble != null) _bubble.Configure(config.bubbleScaleInTime);
        }
    }

    /// <summary>Bắt đầu xuống tàu từ vị trí <paramref name="boatPos"/> (manager giãn cách disembarkInterval).</summary>
    public void BeginDisembark(Vector3 boatPos)
    {
        transform.position = boatPos;
        _logicalWorldPos   = boatPos;
        _pathIndex = 0;
        SetState(AgentState.Disembarking);
        SetTarget(FirstPathPoint());

        // Khách hiện ra MỜ RỒI RÕ thay vì pop đột ngột ở mạn tàu — đối xứng với
        // fade-out lúc lên tàu. Chạy trong Update nên KHÔNG chặn bước đi đầu tiên.
        BatDauFadeIn();
    }

    /// <summary>
    /// Khôi phục sau khi tắt/mở game (GDD §5.1): khách đang đi bộ → SNAP THẲNG tới
    /// đích của pha đó (slot hàng chờ), không phát lại animation đi bộ.
    /// </summary>
    public void ResumeInQueue(int slotIndex, Vector3 slotPos, bool isFront, long patienceEndUtcTicks)
    {
        _slotIndex = slotIndex;
        _isFront   = isFront;
        PatienceEndUtcTicks = patienceEndUtcTicks;

        transform.position = slotPos;
        _logicalWorldPos   = slotPos;
        _hasTarget = false;
        SetMovingAnim(false);
        FaceTowardQueue();

        // Khôi phục từ save: khách vốn đã đứng sẵn trong hàng từ phiên trước ⇒ hiện RÕ
        // ngay, fade-in ở đây sẽ trông như vừa mới xuống tàu (sai ngữ nghĩa).
        _fadeInConLai = -1f;
        DatAlpha(1f);

        EnterWaitServe();
    }

    // ─── Hàng chờ gọi ───────────────────────────────────────────────────

    /// <summary>
    /// Hàng chờ báo slot mới (có người rời đi → dồn hàng).
    ///
    /// [QA M-3] KHÔNG đổi target khi khách đang <c>Disembarking</c>/<c>WalkingPath</c>:
    /// bản cũ cướp mục tiêu giữa đường làm khách bỏ đường đất đi thẳng tới hàng rồi
    /// QUAY NGƯỢC ra waypoint. Chỉ ghi nhận slot; tới cuối path
    /// <see cref="AdvanceAlongPathOrQueue"/> tự dùng slot mới nhất.
    /// </summary>
    public void OnQueueSlotChanged(int slotIndex, Vector3 slotPos, bool isFront)
    {
        _slotIndex = slotIndex;
        _isFront   = isFront;

        // Chỉ khách ĐÃ Ở KHU HÀNG CHỜ mới dồn lên theo slot mới.
        if (State != AgentState.WaitingServe && State != AgentState.WalkingToSlot) return;

        SetTarget(slotPos);
        SetState(AgentState.WalkingToSlot);
    }

    /// <summary>Slot ban đầu (manager gán ngay lúc spawn để giữ thứ tự xuống tàu).</summary>
    public void AssignInitialSlot(int slotIndex, bool isFront)
    {
        _slotIndex = slotIndex;
        _isFront   = isFront;
    }

    // ─── Manager gọi khi giao món / hết giờ ─────────────────────────────

    /// <summary>Giao món thành công: mặt cười 0.5s rồi khách quay về tàu.</summary>
    public void MarkServed()
    {
        if (WasServed || WasTimedOut) return;
        WasServed  = true;
        _stateTimer = 0f;
        if (_bubble != null) _bubble.ShowHappy();
        SetMovingAnim(false);
        SetState(AgentState.Happy);
    }

    /// <summary>Hết kiên nhẫn: mặt TỨC GIẬN 2s rồi khách bỏ về, KHÔNG thưởng.</summary>
    public void MarkTimedOut()
    {
        if (WasServed || WasTimedOut) return;
        WasTimedOut = true;
        _stateTimer = 0f;
        _angryHold  = angryHoldSeconds;
        if (_bubble != null) _bubble.ShowAngry();
        SetMovingAnim(false);
        SetState(AgentState.Angry);
    }

    /// <summary>
    /// [QA B-1] LƯỚI AN TOÀN: Dev A báo tàu đậu quá <c>maxDockMinutes</c>
    /// (event OnDockTimeoutForced, chỉ có 3 giây ân hạn) — khách chưa được phục vụ
    /// chuyển TỨC GIẬN NGAY rồi về tàu, giữ mặt rất ngắn cho kịp.
    /// Khách đã Served vẫn đi tiếp luồng bình thường (đã có thưởng rồi).
    /// </summary>
    public void ForceLeaveAngry()
    {
        if (State == AgentState.Done || State == AgentState.Boarding ||
            State == AgentState.WalkingBack) return;

        if (!WasServed)
        {
            WasTimedOut = true;
            if (_bubble != null) _bubble.ShowAngry();
        }

        _stateTimer = 0f;
        _angryHold  = forcedAngryHoldSeconds;
        SetMovingAnim(false);
        SetState(AgentState.Angry);
    }

    // ─── Input: tap khách để giao món ───────────────────────────────────

    /// <summary>
    /// Tap khách (Collider2D trên prefab, pattern OnMouseDown của BoatDockSlot).
    /// Dùng OnMouseUpAsButton để KHÔNG ăn nhầm thao tác kéo bản đồ.
    /// </summary>
    private void OnMouseUpAsButton()
    {
        // Popup đang mở / đang kéo hạt giống-liềm → không nhận tap world (luật FarmInputLock).
        if (FarmInputLock.IsPopupOpen || FarmInputLock.BlockMapPan) return;
        if (_manager == null) return;
        _manager.DeliverTo(this);
    }

    // ─── Các pha ────────────────────────────────────────────────────────

    private void TickDisembark()
    {
        if (MoveToTarget()) return;
        _pathIndex = 1;
        AdvanceAlongPathOrQueue();
    }

    private void TickWalkPath()
    {
        if (MoveToTarget()) return;
        _pathIndex++;
        AdvanceAlongPathOrQueue();
    }

    /// <summary>Hết waypoint thì đi tới slot hàng chờ; còn waypoint thì đi tiếp.</summary>
    private void AdvanceAlongPathOrQueue()
    {
        if (_pathPoints != null && _pathIndex < _pathPoints.Length)
        {
            SetState(AgentState.WalkingPath);
            SetTarget(_pathPoints[_pathIndex]);
            return;
        }

        SetState(AgentState.WalkingToSlot);
        SetTarget(QueueSlotPosition());
    }

    private void TickWalkToSlot()
    {
        if (MoveToTarget()) return;

        SetMovingAnim(false);
        FaceTowardQueue();
        EnterWaitServe();
    }

    /// <summary>
    /// Vào pha đứng chờ + XIN LƯỢT MỞ BUBBLE từ manager.
    /// [Sếp chốt] mọi khách đều có bubble, mở lần lượt cách nhau bubbleStaggerDelay
    /// bắt đầu từ người tới hàng trước (= người đứng đầu).
    /// </summary>
    private void EnterWaitServe()
    {
        SetState(AgentState.WaitingServe);

        if (_bubble != null && _bubble.State != TouristRequestBubble.BubbleState.Hidden) return;

        float delay = _manager != null ? _manager.TakeBubbleStaggerDelay() : 0f;
        _bubbleOpenAt = Time.time + Mathf.Max(0f, delay);
    }

    private void TickWaitServe()
    {
        // Mở bubble khi tới lượt (nhịp stagger do manager cấp).
        if (_bubbleOpenAt >= 0f && Time.time >= _bubbleOpenAt)
        {
            _bubbleOpenAt = -1f;
            OpenBubbleIfNeeded();
        }

        if (PatienceEndUtcTicks <= 0) return;
        if (System.DateTime.UtcNow.Ticks < PatienceEndUtcTicks) return;

        // Hết kiên nhẫn (đo bằng UTC tuyệt đối — offline vẫn trôi).
        if (_manager != null) _manager.NotifyTimedOut(this);
        else MarkTimedOut();
    }

    private void TickHappy()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < happyHoldSeconds) return;

        // Mặt cười bay lên HUD rồi khách quay về tàu.
        if (_manager != null) _manager.SpawnSmileyFor(this);
        LeaveQueueAndGoBack();
    }

    private void TickAngry()
    {
        _stateTimer += Time.deltaTime;
        if (_stateTimer < _angryHold) return;
        LeaveQueueAndGoBack();
    }

    /// <summary>Rời hàng (hàng tự dồn) rồi đi ngược path về tàu, lệch làn để không đi xuyên khách đang lên.</summary>
    private void LeaveQueueAndGoBack()
    {
        if (_bubble != null) _bubble.Hide();
        if (_queue != null) _queue.Remove(this);
        _isFront = false;

        _pathIndex = _pathPoints != null ? _pathPoints.Length - 1 : -1;
        SetState(AgentState.WalkingBack);

        if (_pathIndex >= 0) SetTarget(LanePoint(_pathIndex));
        else                 BeginBoarding();
    }

    private void TickWalkBack()
    {
        if (MoveToTarget()) return;

        _pathIndex--;
        if (_pathPoints != null && _pathIndex >= 0)
        {
            SetTarget(LanePoint(_pathIndex));
            return;
        }
        BeginBoarding();
    }

    private void BeginBoarding()
    {
        _stateTimer = 0f;
        SetState(AgentState.Boarding);
        SetTarget(_manager != null ? _manager.GetBoardPosition(DockIndex) : transform.position);
    }

    private void TickBoarding()
    {
        if (MoveToTarget()) return;

        // Tới mạn tàu → mờ dần rồi despawn (không cần animation riêng).
        float raGiay = Mathf.Max(0.01f, fadeOutSeconds);
        _stateTimer += Time.deltaTime;
        DatAlpha(Mathf.Clamp01(1f - _stateTimer / raGiay));
        if (_stateTimer < raGiay) return;

        SetState(AgentState.Done);
        SetMovingAnim(false);
        if (_manager != null) _manager.NotifyAboard(this);
        else Destroy(gameObject);
    }

    // ─── Fade in / out ──────────────────────────────────────────────────

    /// <summary>Bật fade-in (alpha 0 → 1). Dùng GIÂY THỰC để khớp nhịp với fade-out.</summary>
    private void BatDauFadeIn()
    {
        if (fadeInSeconds <= 0.01f) { DatAlpha(1f); return; }
        _fadeInConLai = fadeInSeconds;
        DatAlpha(0f);
    }

    /// <summary>
    /// Chạy fade-in mỗi frame — SONG SONG với state machine, không chặn khách đi bộ.
    /// Cố ý KHÔNG chia debugTimeScale: đây là nhịp hình, giống fade-out và giống
    /// cửa sổ ân hạn của Dev A.
    /// </summary>
    private void TickFadeIn()
    {
        if (_fadeInConLai < 0f) return;

        _fadeInConLai -= Time.deltaTime;
        if (_fadeInConLai <= 0f)
        {
            _fadeInConLai = -1f;
            DatAlpha(1f);
            return;
        }

        float giay = Mathf.Max(0.01f, fadeInSeconds);
        DatAlpha(Mathf.Clamp01(1f - _fadeInConLai / giay));
    }

    /// <summary>
    /// Đặt alpha cho MỌI SpriteRenderer con (prefab nhân vật có thể gồm nhiều mảnh:
    /// thân, tóc, bóng…). Bản trước chỉ đụng renderer đầu tiên nên mảnh khác không mờ theo.
    /// </summary>
    private void DatAlpha(float alpha)
    {
        if (_renderers == null) return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            SpriteRenderer sr = _renderers[i];
            if (sr == null) continue;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    // ─── Bubble ─────────────────────────────────────────────────────────

    /// <summary>
    /// Mở bubble món + đặt mốc kiên nhẫn UTC nếu chưa có. Idempotent.
    ///
    /// [QA B-2] Số phút kiên nhẫn CHIA cho debugTimeScale ngay lúc ĐẶT MỐC — nhờ vậy
    /// mốc UTC đã persist vẫn đúng sau khi tắt/mở game (không chia lúc so sánh).
    /// debugTimeScale chỉ có tác dụng trong Editor/Development Build (manager kiểm hộ).
    /// </summary>
    private void OpenBubbleIfNeeded()
    {
        if (_bubble == null || _bubble.State != TouristRequestBubble.BubbleState.Hidden) return;
        if (WasServed || WasTimedOut) return;

        _bubble.ShowRequest(Dish != null ? Dish.dishSprite : null);

        if (PatienceEndUtcTicks <= 0)
        {
            float giay  = _config != null ? Mathf.Max(1f, _config.PatienceSeconds) : 1800f;
            float scale = _manager != null ? _manager.EffectiveTimeScale : 1f;
            PatienceEndUtcTicks = System.DateTime.UtcNow.Ticks +
                                  (long)(giay / Mathf.Max(0.01f, scale) * System.TimeSpan.TicksPerSecond);
        }

        if (_manager != null) _manager.NotifyBubbleOpened(this);
    }

    // ─── Di chuyển ──────────────────────────────────────────────────────

    /// <summary>
    /// Tiến về _target. Trả TRUE nếu CHƯA tới (còn đang đi), FALSE khi đã tới nơi.
    /// MoveTowards thuần transform như LivestockAI — không physics, không Rigidbody.
    /// </summary>
    private bool MoveToTarget()
    {
        if (!_hasTarget) return false;

        Vector3 pos = _logicalWorldPos;
        Vector3 to  = _target - pos;
        to.z = 0f;

        if (to.sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            _logicalWorldPos = new Vector3(_target.x, _target.y, pos.z);
            transform.position = _logicalWorldPos;
            _hasTarget = false;
            SetMovingAnim(false);
            return false;
        }

        _logicalWorldPos = Vector3.MoveTowards(pos, new Vector3(_target.x, _target.y, pos.z),
                                                 _speed * Time.deltaTime);
        SetMovingAnim(true);
        FaceCardinal(to);
        return true;
    }

    private void SetTarget(Vector3 worldPos)
    {
        _target    = worldPos;
        _hasTarget = true;
    }

    /// <summary>
    /// Điểm waypoint đã LỆCH LÀN cho luồng đi về (Sếp duyệt polish 2026-08-29):
    /// dịch vuông góc với hướng đoạn đường một khoảng nhỏ để 2 luồng khách
    /// (lên bờ / về tàu) không đi xuyên qua nhau.
    /// </summary>
    private Vector3 LanePoint(int index)
    {
        if (_pathPoints == null || index < 0 || index >= _pathPoints.Length) return transform.position;
        Vector3 p = _pathPoints[index];
        if (walkBackLaneOffset <= 0.01f || _pathPoints.Length < 2) return p;

        int a = Mathf.Max(0, index - 1);
        int b = Mathf.Min(_pathPoints.Length - 1, index + 1);
        Vector3 huong = _pathPoints[b] - _pathPoints[a];
        huong.z = 0f;
        if (huong.sqrMagnitude < 0.0001f) return p;

        Vector3 vuongGoc = new Vector3(-huong.y, huong.x, 0f).normalized;
        return p + vuongGoc * walkBackLaneOffset;
    }

    private Vector3 FirstPathPoint()
    {
        return _pathPoints != null && _pathPoints.Length > 0 ? _pathPoints[0] : transform.position;
    }

    private Vector3 QueueSlotPosition()
    {
        if (_queue == null) return transform.position;
        return _queue.GetSlotPosition(Mathf.Max(0, _slotIndex));
    }

    // ─── Animator ───────────────────────────────────────────────────────

    /// <summary>
    /// SNAP hướng về 4 hướng chính rồi ghi DirX/DirY — nhờ vậy điều kiện transition
    /// của AnimatorController (do tool sinh) chỉ cần so sánh 1 trục, không nhập nhằng.
    /// </summary>
    private void FaceCardinal(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f || !AnimatorSanSang()) return;

        float x = 0f, y = 0f;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) x = Mathf.Sign(dir.x);
        else                                      y = Mathf.Sign(dir.y);

        _animator.SetFloat(HashDirX, x);
        _animator.SetFloat(HashDirY, y);
    }

    /// <summary>
    /// [QA m-9] Đứng chờ thì quay mặt về phía ĐẦU HÀNG (nhà hàng), suy từ vị trí
    /// anchor hàng chờ thay vì cứng Vector2.up — Sếp kéo QueueAnchor kiểu gì cũng đúng.
    /// Khách đang đứng ngay tại anchor (slot 0) thì giữ hướng "lên" mặc định.
    /// </summary>
    private void FaceTowardQueue()
    {
        Vector2 huong = Vector2.up;
        if (_queue != null)
        {
            Vector3 d = _queue.transform.position - transform.position;
            if (d.sqrMagnitude > 1f) huong = new Vector2(d.x, d.y);
        }
        FaceCardinal(huong);
    }

    private void SetMovingAnim(bool moving)
    {
        if (!AnimatorSanSang()) return;
        _animator.SetBool(HashIsMoving, moving);
    }

    /// <summary>
    /// Animator có DÙNG ĐƯỢC không (đã gán controller + đã khởi tạo).
    /// Không đạt → trả false, cảnh báo ĐÚNG 1 LẦN cho khách này rồi im lặng bỏ qua:
    /// khách vẫn đi lại đúng logic (di chuyển là transform thuần), chỉ là đứng im hình.
    /// Bản trước gọi thẳng SetBool/SetFloat nên một controller hỏng đủ làm ngập Console.
    /// </summary>
    private bool AnimatorSanSang()
    {
        if (_animator != null && _animator.runtimeAnimatorController != null && _animator.isInitialized)
            return true;

        if (!_daCanhBaoAnimator)
        {
            _daCanhBaoAnimator = true;
            string lyDo = _animator == null ? "prefab thiếu component Animator"
                        : _animator.runtimeAnimatorController == null ? "Animator chưa gán AnimatorController"
                        : "AnimatorController hỏng (thiếu statemachine) nên Animator không khởi tạo được";
            Debug.LogWarning($"[TouristVisitor] '{name}': {lyDo} — khách vẫn đi lại bình thường " +
                             "nhưng KHÔNG có animation. Khắc phục: chạy " +
                             "Tools/Farm Game/Tourist Boat/Setup NPC Animations (tool tự xoá và tạo lại controller hỏng). " +
                             "(Cảnh báo này chỉ in 1 lần cho khách này.)", this);
        }
        return false;
    }

    // ─── Sorting theo Y (pattern LivestockAI) ───────────────────────────

    /// <summary>
    /// Đứng thấp hơn (Y nhỏ) thì nổi lên trên — hệt LivestockAI, nhưng KẸP lại vì map
    /// dùng toạ độ rất lớn: Y ~ vài nghìn × 50 sẽ vượt giới hạn sorting order (±32767)
    /// và Unity cắt cụt làm thứ tự nhảy loạn.
    /// </summary>
    private void UpdateDynamicSorting()
    {
        if (_sortingGroup == null) return;

        int dynamic = Mathf.Clamp(Mathf.RoundToInt(-transform.position.y * ySortFactor),
                                  -ySortClamp, ySortClamp);
        int order = baseSortingOrder + dynamic;

        // [LUOI AN TOAN 2026-09-03 - DEV-3, Sep duyet] CHI ap kep khi layer hien hanh dang
        // la "ObjectsFront" (tuc co nguy co dung tau). Hien khach o layer "Objects" nen
        // dieu kien nay KHONG thoa => KHONG kep gi => hanh vi runtime hien tai KHONG doi.
        if (_layerDaGiai == "ObjectsFront")
        {
            if (!_daCanhBaoObjectsFront)
            {
                _daCanhBaoObjectsFront = true;
                Debug.LogWarning("[Tourist] sortingLayer = ObjectsFront trùng layer TÀU — khách sẽ đè lên tàu. " +
                                 "Nên để trống field để dùng 'Objects'.", this);
            }

            if (clampBelowTrain)
                order = Mathf.Min(order, TrainSortingOrderRef - 50);
        }

        _sortingGroup.sortingLayerName = _layerDaGiai;   // tên ĐÃ giải, chắc chắn có thật
        _sortingGroup.sortingOrder     = order;
    }

    private void SetState(AgentState next)
    {
        State = next;
    }
}
