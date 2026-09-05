using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BỘ ĐIỀU PHỐI hệ "cô gái giỏ hoa làm shipper" (Task 1). Đây là API Lead/DEV-D dùng.
///
/// Luồng đúng yêu cầu Sếp: cô gái đứng cạnh bảng đơn hàng → user giao hàng xong →
/// cô gái đi bộ tới MỘT ngôi nhà random trong 5 nhà village (1 đơn = 1 nhà) → có mũi
/// tên chỉ nhà đích → đứng trước nhà một lúc → đi bộ về lại bảng đơn hàng.
///
/// ── HOOK "GIAO HÀNG XONG" ───────────────────────────────────────────────────
/// <c>MissionProgressTracker.OnProgressChanged</c> (static event), lọc key bắt đầu
/// bằng <c>"DeliverOrder:"</c>. Đây là chỗ DUY NHẤT trong project bắn
/// <c>MissionEventType.DeliverOrder</c> (<c>OrderBoardManager.cs:355,359</c>).
/// KHÔNG dùng <c>OrderBoardManagerBase.OnBoardChanged</c> — nó bắn cả khi sinh đơn /
/// bỏ đơn / lên cấp.
///
/// ⚠ BẪY ĐẾM ĐÔI — ĐÃ ĐỌC MÃ NGUỒN THẬT ĐỂ CHỐNG:
/// <c>MissionProgressTracker.ReportEvent(type, itemId, amount, includeTypeWide = true)</c>
/// bơm HAI key cho cùng một lời gọi: <c>"DeliverOrder:{itemId}"</c> và key type-wide
/// <c>"DeliverOrder:*"</c> (<c>AnyToken = "*"</c>). Thêm nữa,
/// <c>OrderBoardManager.CompleteOrder</c> gọi ReportEvent cho TỪNG DÒNG của đơn
/// (dòng đầu typeWide = true, các dòng sau = false). Vậy một đơn 3 món bắn 4 event.
/// Nếu đếm thô thì 1 đơn thành 4 chuyến đi.
/// Lọc riêng <c>":*"</c> cũng KHÔNG cứu được: đơn có <c>itemId</c> rỗng chỉ bắn đúng
/// key type-wide, bỏ nó đi là mất luôn chuyến.
/// ⇒ Cách chắc chắn: GỘP THEO FRAME. Mọi event <c>DeliverOrder:</c> trong CÙNG một
/// <c>Time.frameCount</c> là CÙNG MỘT ĐƠN (ReportEvent chạy đồng bộ) ⇒ đúng 1 chuyến.
/// Sandbox đã kiểm 4 biến thể chuỗi key, kể cả key type-wide: luôn ra đúng số đơn.
///
/// ⚠ Static event ⇒ PHẢI <c>-=</c> trong <c>OnDisable</c> VÀ <c>OnDestroy</c>, không thì
/// leak qua reload scene và một đơn kích 2-3 chuyến (mỗi instance cũ một chuyến).
/// </summary>
public class ShipperManager : MonoBehaviour
{
    /// <summary>Tên asset cấu hình trong Resources.</summary>
    private const string ConfigResourceName = "ShipperConfig";

    /// <summary>Tên object Sếp kéo tay để ghim chỗ đứng chờ (ưu tiên số 1).</summary>
    private const string HomeAnchorObjectName = "Shipper_HomeAnchor";

    /// <summary>Tên object bảng đơn hàng trong scene.</summary>
    private const string OrderBoardObjectName = "OrderBoard_WorldObject";

    /// <summary>Toạ độ bảng đơn đã ĐO trong scene (CONTRACT §2) — lưới an toàn cuối cùng.</summary>
    private static readonly Vector3 OrderBoardFallbackPos = new Vector3(-579f, -672f, 0f);

    /// <summary>Tiền tố key tiến độ của sự kiện giao đơn.</summary>
    private const string DeliverKeyPrefix = "DeliverOrder:";

    /// <summary>Chu kỳ kiểm SỐ NHÀ đổi (giây). KHÔNG kiểm mỗi frame.</summary>
    private const float HouseScanInterval = 2f;

    // ─── Static ─────────────────────────────────────────────────────────

    private static ShipperManager _instance;
    private static bool _sceneHookInstalled;
    private static bool _warnedNoAnchor;
    private static bool _warnedNoConfig;

    /// <summary>Instance hiện có, hoặc null khi feature flag tắt / chưa có cấu hình.</summary>
    public static ShipperManager Instance => _instance;

    /// <summary>
    /// Tạo manager nếu chưa có. <b>RETURN NGAY</b> nếu không nạp được
    /// <c>Resources/ShipperConfig</c> hoặc <c>cfg.enabled == false</c> (CONTRACT §9:
    /// chưa bật thì game chạy y như trước, không spawn gì, không nghe event nào).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureInstance()
    {
        if (_instance != null) return;

        ShipperConfig cfg = Resources.Load<ShipperConfig>(ConfigResourceName);
        if (cfg == null)
        {
            if (!_warnedNoConfig)
            {
                _warnedNoConfig = true;
                Debug.Log("[Shipper] Chưa có Resources/ShipperConfig.asset — hệ shipper TẮT " +
                          "(đúng default an toàn CONTRACT §9). Chạy Editor Tool của DEV-D để tạo.");
            }
            return;
        }

        if (!cfg.enabled) return;   // feature flag tắt: im lặng, đây là trạng thái BÌNH THƯỜNG

        // [QA] §9: chỉ cài hook scene SAU khi flag đã bật. Trước đây InstallSceneHook()
        // nằm ở dòng đầu ⇒ SceneManager.sceneLoaded bị đăng ký + Resources.Load chạy lại
        // mỗi lần load scene KỂ CẢ khi hệ shipper đang tắt (rò rỉ feature flag).
        InstallSceneHook();

        var go = new GameObject("ShipperManager");
        _instance = go.AddComponent<ShipperManager>();
        _instance._cfg = cfg;
    }

    /// <summary>Scene reload thì object manager mất — hook này dựng lại.</summary>
    private static void InstallSceneHook()
    {
        if (_sceneHookInstalled) return;
        _sceneHookInstalled = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_instance == null) EnsureInstance();
    }

    // ─── Runtime ────────────────────────────────────────────────────────

    private ShipperConfig      _cfg;
    private FlowerGirlShipper  _shipper;
    private VillageRoadRing    _ring;

    private Vector3 _homeAnchor;
    private bool    _homeAnchorResolved;

    // Hàng chờ: chỉ cần ĐẾM số đơn dồn (nhà đích chọn lúc XUẤT PHÁT, không chọn trước —
    // nhà có thể bị người chơi xoá trong lúc đơn còn nằm chờ).
    private readonly Queue<int> _queue = new Queue<int>(4);
    private int _orderSeq;

    private int    _lastHouseCount = -1;
    private string _lastHouseId    = "";
    private int    _lastDeliverFrame = -1;
    private bool   _subscribed;

    private readonly List<HouseGrowthController> _candidates = new List<HouseGrowthController>(8);

    /// <summary>Cấu hình đang dùng.</summary>
    public ShipperConfig Config => _cfg;

    /// <summary>Điểm đứng chờ cạnh bảng đơn hàng.</summary>
    public Vector3 HomeAnchor => _homeAnchor;

    /// <summary>Số đơn đang xếp hàng chờ (chưa tính chuyến đang chạy).</summary>
    public int QueuedCount => _queue.Count;

    /// <summary>Đã spawn được cô gái hay chưa.</summary>
    public bool HasShipper => _shipper != null;

    /// <summary>Cô gái đang trong một chuyến giao.</summary>
    public bool IsShipperBusy => _shipper != null && _shipper.IsBusy;

    /// <summary>Vòng đường quanh khu nhà (có thể null).</summary>
    public VillageRoadRing Road => _ring;

    // ─── Vòng đời ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (_cfg == null) _cfg = Resources.Load<ShipperConfig>(ConfigResourceName);
    }

    private void Start()
    {
        if (_cfg == null || !_cfg.enabled)
        {
            Destroy(gameObject);
            return;
        }

        ResolveHomeAnchor();

        _ring = VillageRoadRing.EnsureInstance(_cfg);
        if (_ring != null)
        {
            _ring.Rebuild();
            _lastHouseCount = _ring.HouseCount;
        }

        SpawnShipper();
        StartCoroutine(WatchHouseCountRoutine());

        Debug.Log($"[Shipper] Bật hệ shipper. HomeAnchor = {_homeAnchor}, " +
                  $"số nhà = {(_ring != null ? _ring.HouseCount : 0)}, " +
                  $"có vòng đường = {(_ring != null && _ring.HasRing)}.");
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();   // static event: KHÔNG bỏ dòng này, xem ghi chú đầu file
    }

    private void OnDestroy()
    {
        Unsubscribe();   // static event: gỡ CẢ Ở ĐÂY (OnDisable có thể không chạy khi teardown)
        StopAllCoroutines();
        DeliveryArrowFX.HideAll();

        if (_instance == this) _instance = null;
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        MissionProgressTracker.OnProgressChanged += HandleProgressChanged;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;
        MissionProgressTracker.OnProgressChanged -= HandleProgressChanged;
        _subscribed = false;
    }

    // ─── Hook giao hàng ─────────────────────────────────────────────────

    /// <summary>
    /// Lọc key tiến độ. Xem ghi chú "BẪY ĐẾM ĐÔI" ở đầu file: gộp theo
    /// <c>Time.frameCount</c> để MỘT đơn (dù nhiều dòng, dù có key type-wide)
    /// chỉ kích ĐÚNG MỘT chuyến đi.
    /// </summary>
    private void HandleProgressChanged(string key, int newValue)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!key.StartsWith(DeliverKeyPrefix, System.StringComparison.Ordinal)) return;

        if (Time.frameCount == _lastDeliverFrame) return;   // cùng frame = cùng một đơn
        _lastDeliverFrame = Time.frameCount;

        TriggerDelivery();
    }

    // ─── API công khai ──────────────────────────────────────────────────

    /// <summary>
    /// Kích một chuyến giao. Gọi tay được (để test trong Editor).
    /// Cô gái đang bận ⇒ XẾP HÀNG (tối đa <c>maxQueuedDeliveries</c>, quá thì bỏ đơn
    /// CŨ nhất) — tuyệt đối KHÔNG spawn thêm cô gái.
    /// </summary>
    public void TriggerDelivery()
    {
        if (_cfg == null || !_cfg.enabled) return;

        if (_shipper == null)
        {
            SpawnShipper();
            if (_shipper == null) return;
        }

        if (_shipper.IsBusy)
        {
            int max = _cfg.SafeMaxQueued;
            if (max <= 0) return;

            while (_queue.Count >= max) _queue.Dequeue();   // bỏ đơn CŨ nhất
            _queue.Enqueue(++_orderSeq);
            return;
        }

        Transform house = PickRandomHouse();
        if (house == null)
        {
            // Người chơi chưa mua ngôi nhà nào ⇒ KHÔNG có đích ⇒ cô gái đứng im.
            // Không spawn gì thêm, không log mỗi lần (giao đơn là việc rất thường xuyên).
            _queue.Clear();
            return;
        }

        if (_ring == null) _ring = VillageRoadRing.EnsureInstance(_cfg);
        if (_ring == null) return;

        Vector3   front = _ring.FrontOfHouse(house);
        Vector3[] path  = _ring.BuildPath(_homeAnchor, front);

        if (!_shipper.TryDispatch(house, path, front)) return;

        DeliveryArrowFX.ShowAbove(house, _cfg);
    }

    /// <summary>Quét lại nhà và vẽ lại vòng đường. KHÔNG gọi mỗi frame — xem <see cref="VillageRoadRing.Rebuild"/>.</summary>
    public void RefreshRoad()
    {
        if (_cfg == null) return;
        if (_ring == null) _ring = VillageRoadRing.EnsureInstance(_cfg);
        if (_ring == null) return;

        _ring.Rebuild();
        _lastHouseCount = _ring.HouseCount;
    }

    /// <summary>
    /// Chọn RANDOM một ngôi nhà đủ điều kiện. Trả <c>null</c> nếu chưa có nhà nào
    /// (người chơi chưa mua) — bên gọi phải chịu được null.
    ///
    /// ⚠ Khớp nhà bằng <c>houseId</c>, KHÔNG bằng tên GameObject: nhà là công trình
    /// người chơi tự đặt nên clone có hậu tố <c>"(Clone)"</c>.
    /// ⚠ KHÔNG chọn trùng nhà vừa giao nếu có ≥ 2 nhà đủ điều kiện.
    /// </summary>
    public Transform PickRandomHouse()
    {
        _candidates.Clear();

        HouseGrowthController[] all =
            Object.FindObjectsByType<HouseGrowthController>(FindObjectsSortMode.None);

        bool chiNhaXong = _cfg == null || _cfg.onlyDeliverToCompletedHouses;

        for (int i = 0; i < all.Length; i++)
        {
            HouseGrowthController h = all[i];
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            if (chiNhaXong && h.State != HouseGrowthController.GrowthState.Completed) continue;
            _candidates.Add(h);
        }

        if (_candidates.Count == 0) return null;
        if (_candidates.Count == 1)
        {
            _lastHouseId = _candidates[0].houseId;
            return _candidates[0].transform;
        }

        // bỏ nhà vừa giao ra khỏi rổ; nếu bỏ hết thì lấy lại cả rổ
        var pool = new List<HouseGrowthController>(_candidates.Count);
        for (int i = 0; i < _candidates.Count; i++)
            if (_candidates[i].houseId != _lastHouseId) pool.Add(_candidates[i]);
        if (pool.Count == 0) pool = _candidates;

        HouseGrowthController pick = pool[Random.Range(0, pool.Count)];
        _lastHouseId = pick.houseId;
        return pick.transform;
    }

    // ─── Nội bộ ─────────────────────────────────────────────────────────

    /// <summary>
    /// Ưu tiên: object <c>Shipper_HomeAnchor</c> Sếp kéo tay → bảng đơn
    /// <c>OrderBoard_WorldObject</c> + offset → hằng số đã đo (-579, -672) + offset.
    /// </summary>
    private void ResolveHomeAnchor()
    {
        if (_homeAnchorResolved) return;
        _homeAnchorResolved = true;

        Vector2 offset = _cfg != null ? _cfg.homeAnchorOffset : new Vector2(-300f, -88f);

        GameObject anchor = GameObject.Find(HomeAnchorObjectName);
        if (anchor != null)
        {
            _homeAnchor = anchor.transform.position;   // Sếp ghim tay ⇒ TÔN TRỌNG, không cộng offset
            return;
        }

        GameObject board = GameObject.Find(OrderBoardObjectName);
        if (board != null)
        {
            Vector3 p = board.transform.position;
            _homeAnchor = new Vector3(p.x + offset.x, p.y + offset.y, p.z);
            return;
        }

        _homeAnchor = new Vector3(OrderBoardFallbackPos.x + offset.x,
                                  OrderBoardFallbackPos.y + offset.y, 0f);

        if (!_warnedNoAnchor)
        {
            _warnedNoAnchor = true;
            Debug.LogWarning($"[Shipper] Không tìm thấy '{HomeAnchorObjectName}' cũng không thấy " +
                             $"'{OrderBoardObjectName}' trong scene — dùng toạ độ bảng đơn đã đo " +
                             $"{OrderBoardFallbackPos} + offset {offset} = {_homeAnchor}. " +
                             "Kéo một GameObject rỗng tên 'Shipper_HomeAnchor' vào scene để ghim " +
                             "chỗ đứng chờ chính xác. (Cảnh báo này chỉ in 1 lần.)");
        }
    }

    /// <summary>
    /// Spawn ĐÚNG MỘT cô gái. <c>shipperPrefab</c> null ⇒ dựng GameObject bằng code
    /// (SpriteRenderer + SortingGroup + FourDirWalkAnimator + FlowerGirlShipper),
    /// KHÔNG crash.
    /// </summary>
    private void SpawnShipper()
    {
        if (_shipper != null) return;
        if (_cfg == null) return;

        GameObject go;
        if (_cfg.shipperPrefab != null)
        {
            go = Instantiate(_cfg.shipperPrefab, _homeAnchor, Quaternion.identity);
            go.name = "FlowerGirlShipper";
            StripTouristComponents(go);
        }
        else
        {
            go = new GameObject("FlowerGirlShipper");
            go.transform.position = _homeAnchor;
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<UnityEngine.Rendering.SortingGroup>();
        }

        go.transform.SetParent(transform, true);

        var shipper = go.GetComponent<FlowerGirlShipper>();
        if (shipper == null) shipper = go.AddComponent<FlowerGirlShipper>();

        shipper.Setup(_cfg, _homeAnchor);
        shipper.OnArrivedAtHouse += HandleArrivedAtHouse;
        shipper.OnReturnedHome   += HandleReturnedHome;

        _shipper = shipper;
    }

    /// <summary>
    /// Prefab mẫu để clone là <c>Tourist_NV01.prefab</c> ⇒ nó mang theo
    /// <see cref="TouristAgent"/> và có thể mang <see cref="Animator"/>. Cả hai đều
    /// ghi <c>transform.position</c> / <c>SpriteRenderer.sprite</c> nên phải vô hiệu,
    /// không thì đá nhau với <see cref="FlowerGirlShipper"/>.
    /// CONTRACT §10 cấm gọi <c>TouristAgent.Setup()</c> (coupling nặng) — ở đây chỉ HUỶ.
    /// </summary>
    private static void StripTouristComponents(GameObject go)
    {
        var agents = go.GetComponentsInChildren<TouristAgent>(true);
        for (int i = 0; i < agents.Length; i++)
            if (agents[i] != null) Destroy(agents[i]);

        var animators = go.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
            if (animators[i] != null) animators[i].enabled = false;
    }

    private void HandleArrivedAtHouse(FlowerGirlShipper s)
    {
        // tới nhà rồi thì không cần mũi tên chỉ đường nữa
        if (s != null && s.TargetHouse != null) DeliveryArrowFX.HideFor(s.TargetHouse);
        else                                    DeliveryArrowFX.HideAll();
    }

    private void HandleReturnedHome(FlowerGirlShipper s)
    {
        if (_queue.Count == 0) return;
        _queue.Dequeue();
        TriggerDelivery();
    }

    /// <summary>
    /// Kiểm SỐ NHÀ mỗi <see cref="HouseScanInterval"/> giây (KHÔNG mỗi frame) và chỉ
    /// vẽ lại vòng đường khi số nhà THẬT SỰ đổi — người chơi mua/xoá/di chuyển nhà.
    /// </summary>
    private IEnumerator WatchHouseCountRoutine()
    {
        var wait = new WaitForSeconds(HouseScanInterval);

        while (true)
        {
            yield return wait;

            if (_cfg == null || !_cfg.enabled) yield break;
            if (EditModeManager.IsEditMode) continue;   // đang sắp xếp thì đừng vẽ lại liên tục

            int n = CountHouses();
            if (n == _lastHouseCount) continue;

            _lastHouseCount = n;
            RefreshRoad();
            Debug.Log($"[Shipper] Số nhà village đổi thành {n} — đã vẽ lại đường line bao quanh.");
        }
    }

    private static int CountHouses()
    {
        HouseGrowthController[] all =
            Object.FindObjectsByType<HouseGrowthController>(FindObjectsSortMode.None);
        int n = 0;
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].gameObject.activeInHierarchy) n++;
        return n;
    }
}
