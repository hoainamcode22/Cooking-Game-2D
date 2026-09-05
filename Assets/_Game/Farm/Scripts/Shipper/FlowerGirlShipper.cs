using UnityEngine;

/// <summary>
/// MÁY TRẠNG THÁI của MỘT cô gái giỏ hoa làm shipper (Task 1).
///
///   Idle            — đứng cạnh bảng đơn hàng, mặt hướng Down (về phía người chơi)
///   WalkingToHouse  — đi theo waypoint do <see cref="VillageRoadRing.BuildPath"/> sinh
///   StandingAtHouse — đứng trước nhà <c>standAtHouseSeconds</c>, mặt hướng vào nhà
///   WalkingBack     — đi NGƯỢC mảng waypoint, LỆCH LÀN để đường đi ≠ đường về
///   Done            — DÀNH SẴN, hiện KHÔNG dùng: mọi đường đều kết thúc ở Idle.
///                     Giữ trong enum theo hợp đồng API để Lead/DEV-D không phải đổi
///                     chữ ký nếu sau này cần pha "nghỉ hẳn".
///
/// Yêu cầu Sếp: "nhân vật này chỉ có động tác đi bộ, chỉ cần đi bộ tới là được"
/// ⇒ KHÔNG có animation cầm/đưa hàng, chỉ walk + idle (frame giữa).
///
/// ── PATTERN COPY TỪ <see cref="TouristAgent"/> (CONTRACT §10) ────────────────
///   • <c>_logicalWorldPos</c> RIÊNG, tách khỏi <c>transform.position</c>.
///     ĐÂY LÀ CÁI BẪY: <c>ApplyLivingMotion()</c> ghi <c>transform.position</c> mỗi
///     LateUpdate để nhún nhảy. Nếu <c>MoveToTarget()</c> đọc lại
///     <c>transform.position</c> làm điểm xuất phát thì nhân vật cộng dồn cả phần
///     nhún vào đường đi → trôi dạt lệch khỏi waypoint. Mọi phép tính di chuyển
///     PHẢI dùng <c>_logicalWorldPos</c>; <c>transform.position</c> chỉ là hiển thị.
///   • <c>Vector3.MoveTowards</c> thuần transform — KHÔNG Rigidbody2D, KHÔNG physics.
///   • <c>LanePoint()</c> lệch làn theo pháp tuyến đoạn đường.
///   • Y-sort động có KẸP biên (toạ độ map lớn dễ tràn ±32767).
///
/// ⚠ Nhà đích có thể bị Destroy giữa đường (người chơi vào Edit Mode xoá/di chuyển)
///   ⇒ MỖI BƯỚC kiểm <see cref="TargetHouse"/> == null ⇒ chuyển <c>WalkingBack</c> ngay.
/// ⚠ Tôn trọng Edit Mode: <c>EditModeManager.IsEditMode</c> ⇒ ĐỨNG IM, thoát thì đi tiếp.
/// </summary>
[DisallowMultipleComponent]
public class FlowerGirlShipper : MonoBehaviour
{
    /// <summary>Các pha của cô gái.</summary>
    public enum ShipperState
    {
        Idle            = 0,
        WalkingToHouse  = 1,
        StandingAtHouse = 2,
        WalkingBack     = 3,
        Done            = 4,
    }

    /// <summary>Order gốc của Y-sort (CONTRACT §2).</summary>
    private const int BaseSortingOrder = 5000;

    /// <summary>Hệ số Y → sortingOrder (CONTRACT §2).</summary>
    private const float YSortFactor = 0.5f;

    /// <summary>Biên kẹp Y-sort (CONTRACT §2) — toạ độ lớn dễ tràn ±32767.</summary>
    private const int YSortClamp = 8000;

    /// <summary>Y đổi ít hơn mức này thì KHÔNG tính lại sorting (khỏi ghi renderer mỗi frame).</summary>
    private const float YSortDirtyThreshold = 1f;

    // ─── Runtime ────────────────────────────────────────────────────────

    private ShipperConfig       _cfg;
    private FourDirWalkAnimator _anim;
    private SpriteRenderer      _sr;

    private Vector3 _homeAnchor;
    private Vector3 _logicalWorldPos;

    private Vector3[] _path;          // đường ĐI (từ home vào vòng rồi tới trước nhà)
    private int       _pathIndex;
    private Vector3   _target;
    private bool      _hasTarget;

    private Vector3 _standPoint;
    private float   _standRemaining;

    private float _lastSortedY = float.NaN;
    private int   _lastOrder   = int.MinValue;
    private string _layerResolved = "Default";

    private bool  _scaleApplied;
    private bool  _editModeHooked;
    private float _motionSeed;
    private bool  _warnedNoAnim;

    /// <summary>Pha hiện tại.</summary>
    public ShipperState State { get; private set; } = ShipperState.Idle;

    /// <summary>Nhà đang giao tới. NULL nếu đang rảnh, hoặc nhà đã bị Destroy.</summary>
    public Transform TargetHouse { get; private set; }

    /// <summary>Đang trong một chuyến giao (không nhận chuyến mới).</summary>
    public bool IsBusy => State == ShipperState.WalkingToHouse
                       || State == ShipperState.StandingAtHouse
                       || State == ShipperState.WalkingBack;

    /// <summary>Điểm đứng chờ cạnh bảng đơn hàng.</summary>
    public Vector3 HomeAnchor => _homeAnchor;

    /// <summary>Vị trí LOGIC (đã bỏ phần nhún nhảy hiển thị).</summary>
    public Vector3 LogicalPosition => _logicalWorldPos;

    /// <summary>Bắn ĐÚNG 1 LẦN mỗi chuyến, khi vừa tới điểm đứng trước nhà.</summary>
    public event System.Action<FlowerGirlShipper> OnArrivedAtHouse;

    /// <summary>Bắn ĐÚNG 1 LẦN mỗi chuyến, khi vừa về tới bảng đơn hàng.</summary>
    public event System.Action<FlowerGirlShipper> OnReturnedHome;

    // ─── Setup ──────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn cấu hình + điểm đứng chờ. Gọi được nhiều lần (đổi config lúc test).
    /// Tự tìm/tạo <see cref="SpriteRenderer"/> và <see cref="FourDirWalkAnimator"/>.
    /// </summary>
    public void Setup(ShipperConfig cfg, Vector3 homeAnchor)
    {
        _cfg        = cfg;
        _homeAnchor = homeAnchor;
        _motionSeed = Random.Range(0f, 10f);

        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null) _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

        _anim = GetComponent<FourDirWalkAnimator>();
        if (_anim == null) _anim = gameObject.AddComponent<FourDirWalkAnimator>();

        if (_cfg != null && _cfg.HasWalkFrames)
            _anim.SetupFromFlat(_cfg.walkFrames, _cfg.SafeWalkFps, _sr);
        else if (!_warnedNoAnim)
        {
            _warnedNoAnim = true;
            Debug.LogWarning("[Shipper] ShipperConfig.walkFrames chưa đủ 12 sprite — cô gái " +
                             "vẫn đi lại đúng logic nhưng KHÔNG có animation. Chạy tool slice " +
                             "flowergirl_walk_spritesheet.png rồi gán vào config. " +
                             "(Cảnh báo này chỉ in 1 lần.)", this);
        }

        _layerResolved   = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);
        _logicalWorldPos = homeAnchor;
        transform.position = homeAnchor;

        ApplyScaleIfPossible();
        HookEditMode();

        State = ShipperState.Idle;
        _hasTarget = false;
        _anim.SetWalking(false);
        _anim.FaceFacing(FourDirWalkAnimator.Facing.Down);
        UpdateDynamicSorting(true);
    }

    // ─── Điều phối ──────────────────────────────────────────────────────

    /// <summary>
    /// Nhận một chuyến giao. Trả FALSE nếu đang bận, chưa Setup, hoặc thiếu dữ liệu
    /// (bên gọi tự xếp hàng chờ — KHÔNG spawn thêm cô gái).
    /// </summary>
    public bool TryDispatch(Transform house, Vector3[] pathToHouse, Vector3 standPoint)
    {
        if (_cfg == null || IsBusy) return false;
        if (house == null) return false;
        if (pathToHouse == null || pathToHouse.Length == 0) return false;

        TargetHouse = house;
        _standPoint = standPoint;

        // Đường đi = waypoint do VillageRoadRing sinh, ĐIỂM CUỐI luôn là điểm đứng trước nhà.
        _path = new Vector3[pathToHouse.Length];
        for (int i = 0; i < pathToHouse.Length; i++)
            _path[i] = new Vector3(pathToHouse[i].x, pathToHouse[i].y, _logicalWorldPos.z);
        _path[_path.Length - 1] = new Vector3(_standPoint.x, _standPoint.y, _logicalWorldPos.z);

        _pathIndex = 0;
        State      = ShipperState.WalkingToHouse;
        SetTarget(_path[0]);
        return true;
    }

    /// <summary>
    /// Bỏ chuyến hiện tại, quay về bảng đơn ngay. Không có đường về (chưa từng đi)
    /// ⇒ dịch chuyển thẳng về <see cref="HomeAnchor"/> rồi về <c>Idle</c>.
    /// </summary>
    public void ForceReturnHome()
    {
        TargetHouse = null;

        if (_path == null || _path.Length == 0)
        {
            _logicalWorldPos   = _homeAnchor;
            transform.position = _homeAnchor;
            FinishAtHome();
            return;
        }

        BeginWalkBack();
    }

    // ─── Vòng đời ───────────────────────────────────────────────────────

    private void Update()
    {
        if (_cfg == null) return;

        // Edit Mode: ĐỨNG IM tại chỗ, không tiến bước nào. Thoát Edit Mode thì đi tiếp
        // (target và _pathIndex giữ nguyên nên không mất chuyến).
        if (EditModeManager.IsEditMode)
        {
            if (_anim != null) _anim.SetWalking(false);
            return;
        }

        if (!_scaleApplied) ApplyScaleIfPossible();

        switch (State)
        {
            case ShipperState.WalkingToHouse:  TickWalkToHouse();  break;
            case ShipperState.StandingAtHouse: TickStandAtHouse(); break;
            case ShipperState.WalkingBack:     TickWalkBack();     break;
        }

        UpdateDynamicSorting(false);
    }

    private void LateUpdate()
    {
        // [QA] chưa Setup() thì _cfg == null và _logicalWorldPos == (0,0,0):
        // ApplyLivingMotion() sẽ GHI transform.position = gốc toạ độ mỗi frame,
        // kéo prefab (hoặc object Sếp kéo tay vào scene) về (0,0,0) và rung liên tục.
        if (_cfg == null) return;

        ApplyLivingMotion();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        UnhookEditMode();

        // huỷ mũi tên của nhà đang giao (nếu chuyến bị cắt giữa đường)
        DeliveryArrowFX.HideAll();

        OnArrivedAtHouse = null;
        OnReturnedHome   = null;
    }

    // ─── Các pha ────────────────────────────────────────────────────────

    private void TickWalkToHouse()
    {
        // Nhà bị Destroy giữa đường ⇒ quay về NGAY, không NullReference.
        if (TargetHouse == null)
        {
            BeginWalkBack();
            return;
        }

        if (MoveToTarget()) return;      // còn đang đi

        _pathIndex++;
        if (_pathIndex < _path.Length)
        {
            SetTarget(_path[_pathIndex]);
            return;
        }

        // Tới điểm đứng trước nhà
        State           = ShipperState.StandingAtHouse;
        _standRemaining = _cfg.SafeStandSeconds;
        _hasTarget      = false;

        if (_anim != null)
        {
            _anim.SetWalking(false);
            FaceTowardHouse();
        }

        var cb = OnArrivedAtHouse;
        if (cb != null) cb(this);
    }

    private void TickStandAtHouse()
    {
        if (TargetHouse == null)
        {
            BeginWalkBack();
            return;
        }

        _standRemaining -= Time.deltaTime;
        if (_standRemaining > 0f) return;

        BeginWalkBack();
    }

    private void TickWalkBack()
    {
        if (MoveToTarget()) return;

        _pathIndex--;
        if (_pathIndex >= 0)
        {
            SetTarget(LanePoint(_pathIndex));
            return;
        }

        // hết waypoint → chặng cuối về đúng điểm đứng chờ
        if ((_logicalWorldPos - _homeAnchor).sqrMagnitude > ArriveSqr())
        {
            SetTarget(_homeAnchor);
            _pathIndex = -1;
            State      = ShipperState.WalkingBack;
            if (MoveToTarget()) return;
        }

        FinishAtHome();
    }

    private void BeginWalkBack()
    {
        ShipperState truoc = State;

        TargetHouse = null;
        State       = ShipperState.WalkingBack;

        if (_path == null || _path.Length == 0)
        {
            SetTarget(_homeAnchor);
            _pathIndex = -1;
            return;
        }

        // Đi NGƯỢC mảng waypoint — nhưng phải bắt đầu từ ĐÚNG CHỖ ĐANG ĐỨNG.
        //
        // ⚠ BUG ĐÃ SỬA: bản đầu luôn lấy `_path.Length - 2`. Đúng cho trường hợp giao
        // xong bình thường, nhưng SAI khi nhà bị Destroy giữa đường: cô gái đang ở
        // waypoint 3/10 lại nhận mục tiêu waypoint 8 ⇒ đi CHÉO XUYÊN QUA CẢ KHU NHÀ
        // thay vì lần ngược đường cũ. Nay suy từ `_pathIndex` hiện tại.
        int batDau;
        if (truoc == ShipperState.StandingAtHouse || _pathIndex >= _path.Length)
            batDau = _path.Length - 2;      // đã đứng ở điểm cuối
        else
            batDau = _pathIndex - 1;        // đang đi giữa path[_pathIndex-1] → path[_pathIndex]

        if (batDau < 0)
        {
            // mới rời bảng đơn, chưa qua waypoint nào ⇒ về thẳng
            SetTarget(_homeAnchor);
            _pathIndex = -1;
            return;
        }

        _pathIndex = Mathf.Min(batDau, _path.Length - 1);
        SetTarget(LanePoint(_pathIndex));
    }

    private void FinishAtHome()
    {
        _logicalWorldPos = new Vector3(_homeAnchor.x, _homeAnchor.y, _logicalWorldPos.z);
        transform.position = _logicalWorldPos;

        State       = ShipperState.Idle;
        _hasTarget  = false;
        _path       = null;
        _pathIndex  = 0;
        TargetHouse = null;

        if (_anim != null)
        {
            _anim.SetWalking(false);
            _anim.FaceFacing(FourDirWalkAnimator.Facing.Down);
        }

        var cb = OnReturnedHome;
        if (cb != null) cb(this);
    }

    // ─── Di chuyển (pattern TouristAgent.MoveToTarget) ──────────────────

    /// <summary>
    /// Tiến về <c>_target</c>. Trả TRUE nếu CHƯA tới (còn đang đi), FALSE khi đã tới nơi.
    /// MoveTowards thuần transform — không physics, không Rigidbody.
    /// </summary>
    private bool MoveToTarget()
    {
        if (!_hasTarget) return false;

        Vector3 pos = _logicalWorldPos;
        Vector3 to  = _target - pos;
        to.z = 0f;

        float thr = _cfg.SafeArriveThreshold;
        if (to.sqrMagnitude <= thr * thr)
        {
            _logicalWorldPos   = new Vector3(_target.x, _target.y, pos.z);
            transform.position = _logicalWorldPos;
            _hasTarget = false;
            if (_anim != null) _anim.SetWalking(false);
            return false;
        }

        _logicalWorldPos = Vector3.MoveTowards(pos, new Vector3(_target.x, _target.y, pos.z),
                                               _cfg.SafeWalkSpeed * Time.deltaTime);
        if (_anim != null)
        {
            _anim.SetWalking(true);
            _anim.FaceDirection(new Vector2(to.x, to.y));
        }
        return true;
    }

    private void SetTarget(Vector3 worldPos)
    {
        _target    = new Vector3(worldPos.x, worldPos.y, _logicalWorldPos.z);
        _hasTarget = true;

        if (_anim != null)
        {
            Vector3 d = _target - _logicalWorldPos;
            _anim.FaceDirection(new Vector2(d.x, d.y));
        }
    }

    private float ArriveSqr()
    {
        float thr = _cfg != null ? _cfg.SafeArriveThreshold : 12f;
        return thr * thr;
    }

    /// <summary>
    /// Điểm waypoint đã LỆCH LÀN cho luồng ĐI VỀ — pattern <c>TouristAgent.LanePoint()</c>.
    ///
    /// Khác bản gốc ở HAI chỗ, cả hai đều do sandbox phát hiện:
    ///   • CHỌN PHÍA nhất quán: luôn lệch RA NGOÀI so với tâm vòng đường. Bản gốc lấy
    ///     pháp tuyến thô nên ở đoạn đổi hướng, làn về có thể nhảy sang phía bên kia
    ///     rồi cắt ngang qua chính làn đi.
    ///   • BÙ ĐỘ DÀI 1/sin: ở đỉnh gấp khúc, lệch đúng <c>offset</c> theo đường phân giác
    ///     chỉ cho khoảng cách vuông góc <c>offset·sin(nửa góc)</c> — với góc 90° chỉ còn
    ///     0.707·offset, tụt dưới ngưỡng an toàn. Chia cho sin đưa khoảng cách tới CẢ HAI
    ///     đoạn kề về đúng <c>offset</c>. Sandbox đo lại: min 41 unit với offset 40. ✔
    /// </summary>
    private Vector3 LanePoint(int index)
    {
        if (_path == null || index < 0 || index >= _path.Length) return _logicalWorldPos;

        Vector3 p = _path[index];
        float offset = _cfg != null ? _cfg.SafeLaneOffset : 0f;
        if (offset <= 0.01f || _path.Length < 2) return p;

        int a = Mathf.Max(0, index - 1);
        int b = Mathf.Min(_path.Length - 1, index + 1);

        Vector3 huong = _path[b] - _path[a];
        huong.z = 0f;
        if (huong.sqrMagnitude < 0.0001f) return p;

        Vector3 vuongGoc = new Vector3(-huong.y, huong.x, 0f).normalized;

        // lệch RA NGOÀI so với tâm vòng đường (nhất quán, không nhảy làn)
        Vector3 tam = VillageRoadRing.Instance != null
                    ? VillageRoadRing.Instance.RingCenter
                    : _homeAnchor;
        Vector3 raNgoai = p - tam;
        raNgoai.z = 0f;
        if (Vector3.Dot(vuongGoc, raNgoai) < 0f) vuongGoc = -vuongGoc;

        // bù 1/sin để khoảng cách vuông góc tới CẢ HAI đoạn kề đạt đúng offset
        float s1 = index > a ? Mathf.Abs(Cross2(vuongGoc, (_path[index] - _path[a]).normalized)) : 1f;
        float s2 = b > index ? Mathf.Abs(Cross2(vuongGoc, (_path[b] - _path[index]).normalized)) : 1f;
        if (s1 < 1e-4f) s1 = 1f;
        if (s2 < 1e-4f) s2 = 1f;

        float len = offset / Mathf.Max(0.2f, Mathf.Min(s1, s2));
        len = Mathf.Min(len, offset * 5f);

        return p + vuongGoc * len;
    }

    private static float Cross2(Vector3 u, Vector3 v) => u.x * v.y - u.y * v.x;

    private void FaceTowardHouse()
    {
        if (_anim == null) return;

        Vector3 d = TargetHouse != null
                  ? TargetHouse.position - _logicalWorldPos
                  : Vector3.up;
        d.z = 0f;

        if (d.sqrMagnitude < 1f) _anim.FaceFacing(FourDirWalkAnimator.Facing.Up);
        else                     _anim.FaceDirection(new Vector2(d.x, d.y));
    }

    // ─── Scale theo chiều cao world ─────────────────────────────────────

    /// <summary>
    /// Scale = <c>worldHeight / sprite.bounds.size.y</c> tính từ frame đại diện.
    /// Sprite chưa có (art chưa slice) ⇒ thử lại ở frame sau, không log, không crash.
    /// </summary>
    private void ApplyScaleIfPossible()
    {
        if (_cfg == null || _scaleApplied) return;

        Sprite s = _anim != null ? _anim.RepresentativeSprite : null;
        if (s == null && _sr != null) s = _sr.sprite;
        if (s == null) return;

        float h = s.bounds.size.y;
        if (h <= 0.0001f) return;

        float k = _cfg.SafeWorldHeight / h;
        transform.localScale = new Vector3(k, k, 1f);
        _baseLocalScale = transform.localScale;
        _hasBaseScale   = true;
        _scaleApplied   = true;
    }

    // ─── Sorting theo Y chuẩn Isometric / Farm ──────────────────────────

    /// <summary>
    /// Sorting Order = -Round(Y * 10).
    /// Khi nhân vật đi SAU ngôi nhà (Y nhân vật > Y nhà), order nhân vật < order nhà
    /// → Ngôi nhà sẽ CHE nhân vật một cách tự nhiên!
    /// </summary>
    private void UpdateDynamicSorting(bool force)
    {
        if (_sr == null) return;

        float y = _logicalWorldPos.y;
        if (!force && !float.IsNaN(_lastSortedY) && Mathf.Abs(y - _lastSortedY) <= YSortDirtyThreshold)
            return;

        _lastSortedY = y;

        int order = Mathf.Clamp(-Mathf.RoundToInt(y * 10f), -YSortClamp, YSortClamp);
        if (order == _lastOrder && !force) return;
        _lastOrder = order;

        _sr.sortingLayerName = "Objects";
        _sr.sortingOrder     = order;

        var sg = GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null)
        {
            sg.sortingLayerName = "Objects";
            sg.sortingOrder     = order;
        }
    }

    // ─── Nhún nhảy (pattern TouristAgent.ApplyLivingMotion) ─────────────

    private Vector3 _baseLocalScale = Vector3.one;
    private bool    _hasBaseScale;

    /// <summary>
    /// Nhịp bước chân / thở khi đứng — vẽ bằng code, KHÔNG DOTween (CONTRACT §0.6).
    /// GHI <c>transform.position</c>, KHÔNG ghi <c>_logicalWorldPos</c>.
    /// </summary>
    private void ApplyLivingMotion()
    {
        if (!_hasBaseScale)
        {
            _baseLocalScale = transform.localScale;
            _hasBaseScale   = true;
        }

        float t = Time.time;
        float squashX = 1f, stretchY = 1f, offsetY = 0f, rotZ = 0f;

        if (_hasTarget && !EditModeManager.IsEditMode)
        {
            float step = Mathf.Abs(Mathf.Sin(t * 11f + _motionSeed)) * 3.5f;
            offsetY  = step;
            stretchY = 1f + 0.03f * (step / 3.5f);
            squashX  = 1f - 0.02f * (step / 3.5f);
            rotZ     = Mathf.Sin(t * 11f + _motionSeed) * 2.2f;
        }
        else
        {
            float breath = Mathf.Sin(t * 3.0f + _motionSeed);
            stretchY = 1f + 0.018f * breath;
            squashX  = 1f - 0.012f * breath;
            rotZ     = Mathf.Sin(t * 1.4f + _motionSeed) * 0.9f;
        }

        transform.position      = _logicalWorldPos + new Vector3(0f, offsetY, 0f);
        transform.localScale    = new Vector3(_baseLocalScale.x * squashX,
                                              _baseLocalScale.y * stretchY,
                                              _baseLocalScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    // ─── Edit Mode ──────────────────────────────────────────────────────

    private void HookEditMode()
    {
        if (_editModeHooked) return;
        EditModeManager.OnEditModeChanged += HandleEditModeChanged;
        _editModeHooked = true;
    }

    private void UnhookEditMode()
    {
        if (!_editModeHooked) return;
        EditModeManager.OnEditModeChanged -= HandleEditModeChanged;
        _editModeHooked = false;
    }

    /// <summary>
    /// Vào Edit Mode ⇒ đứng im (Update tự bỏ qua). Ra Edit Mode ⇒ đi tiếp; nhưng nếu
    /// nhà đích đã bị người chơi xoá/di chuyển thì quay về ngay.
    /// </summary>
    private void HandleEditModeChanged(bool editMode)
    {
        if (_anim != null) _anim.SetWalking(false);

        if (editMode) return;

        if ((State == ShipperState.WalkingToHouse || State == ShipperState.StandingAtHouse)
            && TargetHouse == null)
            BeginWalkBack();
    }
}
