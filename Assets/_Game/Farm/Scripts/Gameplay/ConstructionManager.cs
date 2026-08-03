using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Loại tiền dùng để tăng tốc xây dựng.</summary>
public enum ConstructionRushCurrency
{
    Gold = 0,   // 🪙 vàng — mặc định (video f_045 vẽ tờ tiền xanh = "tiền mặt")
    Gems = 1    // 💎 kim cương
}

/// <summary>
/// QUẢN LÝ MỌI CÔNG TRƯỜNG ĐANG XÂY (N1).
///
/// ╔══════════════════════════════════════════════════════════════════════════╗
/// ║ ⚠ HỢP ĐỒNG VỚI DEV-1 — KHÔNG ĐƯỢC ĐỔI 3 THỨ SAU                          ║
/// ║   • class `ConstructionManager` — KHÔNG namespace, KHÔNG đổi tên          ║
/// ║   • property tĩnh public `Instance`                                      ║
/// ║   • `public bool TryStartConstruction(PlaceableItemData, Vector3, int, int)` ║
/// ║ PlacementManager tra 3 thứ này bằng REFLECTION (xem                      ║
/// ║ PlacementManager.TryStartConstructionDev2). Sai một chữ là game im lặng   ║
/// ║ rơi về đường cũ "bấm ✓ hiện công trình ngay" và không ai báo lỗi.         ║
/// ╚══════════════════════════════════════════════════════════════════════════╝
///
/// TỰ MỌC RA, KHÔNG CẦN KÉO VÀO SCENE: <see cref="Bootstrap"/> chạy sau khi scene load
/// và tự tạo object nếu scene đó có PlacementManager. Lý do: DEV-2 không được sửa
/// SCN_Farm.unity (dễ xung đột merge với DEV-1), mà `Instance` bắt buộc phải khác null
/// ngay lần đầu người chơi bấm ✓.
///
/// LƯU RIÊNG: key `FARM_CONSTRUCTION_SITES` — KHÔNG đụng `FARM_PLACED_BUILDINGS`
/// của DEV-1. Công trình chỉ được ghi vào save của DEV-1 khi đã xây XONG, thông qua
/// `PlacementManager.RegisterCompletedBuilding`.
/// </summary>
public class ConstructionManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    // HỢP ĐỒNG API
    // ══════════════════════════════════════════════════════════════════════

    public static ConstructionManager Instance { get; private set; }

    /// <summary>Bắn ra khi một công trình xây xong: (data, ĐIỂM NEO world, bước xoay, plotId).
    /// Neo = transform.position của prefab vừa Instantiate, KHÔNG phải tâm khối ô.</summary>
    public event Action<PlaceableItemData, Vector3, int, int> OnConstructionComplete;

    // ══════════════════════════════════════════════════════════════════════
    // CẤU HÌNH (Edric chỉnh trong Inspector nếu muốn)
    // ══════════════════════════════════════════════════════════════════════

    [Header("◆ Bộ ô art (kéo asset ConstructionArtKit vào đây)")]
    [Tooltip("Để trống vẫn chạy — mọi mảnh sẽ là hình vẽ code TÔ MÀU NHẬN DẠNG.")]
    [SerializeField] private ConstructionArtKit artKit;

    /// <summary>Bộ ô art đang dùng. Có thể null — mọi nơi gọi đều đi qua ResolveSafe.</summary>
    public ConstructionArtKit ArtKit => artKit;

    [Header("Art thay thế (để trống = dùng hình vẽ bằng code)")]
    [Tooltip("Ô CŨ — giữ lại để scene đang gán sẵn không bị vỡ. Nếu ArtKit có 'Worker' " +
             "thì ô của kit được ưu tiên và ô này bị bỏ qua.")]
    [SerializeField] private Sprite workerSprite;

    [Tooltip("Prefab VFX ăn mừng dùng lại (vd Assets/_Game/Farm/Prefabs/VFX/LevelUp/LevelUp_Confetti_Lana02).\n" +
             "Để trống: tự mượn prefab confetti mà LevelUpPopupUI trong scene đang dùng.")]
    [SerializeField] private GameObject completeVfxPrefab;

    [Tooltip("VFX của Lana Studio dựng cho world unit nhỏ; map này 1 ô = 100 unit nên phải phóng to.")]
    [SerializeField] private float completeVfxScale = 40f;

    [Header("Giá tăng tốc (rush)")]
    [SerializeField] private ConstructionRushCurrency rushCurrency = ConstructionRushCurrency.Gold;

    [Tooltip("Hằng số a trong  giá = ceil(a + b·√(giây còn lại)).")]
    [SerializeField] private float rushBaseCost = DefaultRushBase;

    [Tooltip("Hằng số b trong  giá = ceil(a + b·√(giây còn lại)).")]
    [SerializeField] private float rushSqrtFactor = DefaultRushSqrtFactor;

    [Header("Giới hạn")]
    [Tooltip("Số công trường được xây cùng lúc. 0 = không giới hạn.")]
    [SerializeField] private int maxConcurrentSites = 0;

    [SerializeField] private bool verboseLog = false;

    // ══════════════════════════════════════════════════════════════════════
    // CÔNG THỨC GIÁ RUSH
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// GIÁ RUSH = ceil( a + b · √(giây còn lại) ),  a = 15,  b = 0.82.
    ///
    /// SUY TỪ 2 MỐC ĐO TRONG VIDEO THAM CHIẾU (§0 doc đội):
    ///     52 giây      → 21    ·  15 + 0.82·√52  = 20.91 → ceil 21 ✔
    ///     1m59 (119 s) → 24    ·  15 + 0.82·√119 = 23.95 → ceil 24 ✔
    ///
    /// VÌ SAO CHỌN DẠNG √ CHỨ KHÔNG PHẢI TUYẾN TÍNH:
    ///   • Hai mốc chỉ cách nhau 67 giây mà giá chỉ tăng 3 → độ dốc rất thoải.
    ///     Nếu ép tuyến tính (a + b·t) thì b = 0.0448/giây, và một công trình 8 giờ
    ///     sẽ ra 1 305 — vô lý, không ai bấm.
    ///   • Với dạng √: 5 phút → 30 · 1 giờ → 64 · 8 giờ → 154. Đúng dải Township.
    ///   • Hằng số a = 15 chính là "phí bấm nút" tối thiểu: rush lúc còn 2 giây vẫn
    ///     mất 17 — chống việc đợi gần xong rồi rush cho rẻ. Township cũng làm vậy.
    ///
    /// `PlaceableItemData.rushGemCost > 0` thì DÙNG THẲNG số cứng đó, bỏ qua công thức.
    /// </summary>
    public const float DefaultRushBase       = 15f;
    public const float DefaultRushSqrtFactor = 0.82f;

    public static int RushCostFor(float remainingSeconds, float baseCost, float sqrtFactor)
    {
        float t = Mathf.Max(0f, remainingSeconds);
        return Mathf.Max(1, Mathf.CeilToInt(baseCost + sqrtFactor * Mathf.Sqrt(t)));
    }

    /// <summary>Bản dùng hằng số mặc định — Editor tool gọi để xem trước.</summary>
    public static int RushCostFor(float remainingSeconds)
        => RushCostFor(remainingSeconds, DefaultRushBase, DefaultRushSqrtFactor);

    public bool RushUsesGems => rushCurrency == ConstructionRushCurrency.Gems;

    // ══════════════════════════════════════════════════════════════════════
    // SORTING LAYER
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Layer cho giàn giáo — cùng nhóm với công trình thật.</summary>
    public const int SiteBaseOrder = 510;

    private static string _siteLayer;
    private static string _topLayer;

    public static string SiteSortingLayerName
    {
        get
        {
            if (string.IsNullOrEmpty(_siteLayer))
                _siteLayer = ResolveLayer("CongTrinh", "ObjectsFront", "Objects");
            return _siteLayer;
        }
    }

    /// <summary>Layer trên cùng — UI công trường dùng để không bị công trình khác che.</summary>
    public static string TopSortingLayerName
    {
        get
        {
            if (string.IsNullOrEmpty(_topLayer))
                _topLayer = ResolveLayer("Foreground", "ObjectsFront", "Objects");
            return _topLayer;
        }
    }

    private static string ResolveLayer(params string[] preferred)
    {
        SortingLayer[] layers = SortingLayer.layers;
        for (int i = 0; i < preferred.Length; i++)
        {
            for (int j = 0; j < layers.Length; j++)
            {
                if (layers[j].name == preferred[i]) return preferred[i];
            }
        }
        return "Default";
    }

    // ══════════════════════════════════════════════════════════════════════
    // SAVE
    // ══════════════════════════════════════════════════════════════════════

    public const string SaveKey     = "FARM_CONSTRUCTION_SITES";
    public const int    SaveVersion = 1;

    [Serializable]
    private class SiteEntry
    {
        public string itemId;
        public float  x;
        public float  y;
        public int    rot;
        public int    plotId;
        public long   startUnix;   // mốc UTC lúc bấm ✓
        public float  duration;    // giây
    }

    [Serializable]
    private class SitesSave
    {
        public int            saveVersion = SaveVersion;
        public long           maxSeenUnix;   // chống lùi giờ máy
        public List<SiteEntry> list = new List<SiteEntry>();
    }

    // ══════════════════════════════════════════════════════════════════════
    // TRẠNG THÁI
    // ══════════════════════════════════════════════════════════════════════

    private readonly List<ConstructionSite> _sites = new List<ConstructionSite>();

    private long  _maxSeenUnix;
    private long  _sessionAnchorUnix;
    private float _sessionAnchorRealtime;

    private bool _loaded;
    private bool _warnedNoPlacementManager;

    public int ActiveSiteCount => _sites.Count;
    public IReadOnlyList<ConstructionSite> Sites => _sites;

    // ══════════════════════════════════════════════════════════════════════
    // BOOTSTRAP
    // ══════════════════════════════════════════════════════════════════════

    // Enter Play Mode có thể tắt Domain Reload → biến static giữ giá trị cũ.
    // Reset thủ công, nếu không `Instance` sẽ trỏ vào object đã bị huỷ của lần Play trước.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance   = null;
        _siteLayer = null;
        _topLayer  = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // -= trước += : tránh đăng ký trùng khi Domain Reload bị tắt.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureInstance();

    private static void EnsureInstance()
    {
        if (Instance != null) return;

        // Chỉ mọc ở scene có PlacementManager (scene nông trại) — scene menu không cần.
        if (FindFirstObjectByType<PlacementManager>(FindObjectsInactive.Include) == null) return;

        var go = new GameObject("ConstructionManager (tự tạo)");
        go.AddComponent<ConstructionManager>();
    }

    // ══════════════════════════════════════════════════════════════════════
    // VÒNG ĐỜI
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // KHÔNG DontDestroyOnLoad: các công trường là object trong scene nông trại,
        // giữ manager sống qua scene khác sẽ để lại danh sách trỏ vào object đã chết.
        // Sang scene mới, Bootstrap tự tạo lại và nạp lại từ save.
        AnchorClock();
    }

    private void Start()
    {
        StartCoroutine(LoadAfterOneFrame());
    }

    /// <summary>
    /// Chờ 1 frame rồi mới nạp save.
    /// VÌ SAO: `PlacementManager.Start()` gọi LoadBuildings + RefreshOccupancy, và thứ tự
    /// Start giữa hai component là KHÔNG xác định. Nạp sau 1 frame đảm bảo công trình đã
    /// đặt xong trước, rồi công trường mới giữ chỗ ô của mình — không bị RefreshOccupancy
    /// quét mất chỗ giữ.
    /// </summary>
    private IEnumerator LoadAfterOneFrame()
    {
        yield return null;
        LoadSites();
    }

    private void Update()
    {
        if (_sites.Count == 0) return;

        long now = NowUnix();

        for (int i = _sites.Count - 1; i >= 0; i--)
        {
            ConstructionSite site = _sites[i];
            if (site == null) { _sites.RemoveAt(i); continue; }

            site.Tick(now);

            if (!site.IsFinishing && site.RemainingSeconds(now) <= 0f)
                CompleteSite(site);
        }
    }

    // Android giết app KHÔNG gọi OnApplicationQuit → phải lưu ở đây.
    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveSites();
    }

    private void OnApplicationQuit() => SaveSites();

    private void OnDestroy()
    {
        if (Instance != this) return;

        SaveSites();
        Instance = null;
    }

    // ══════════════════════════════════════════════════════════════════════
    // ĐỒNG HỒ — CHỐNG LÙI GIỜ MÁY
    // ══════════════════════════════════════════════════════════════════════

    private void AnchorClock()
    {
        long real = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (real > _maxSeenUnix) _maxSeenUnix = real;

        _sessionAnchorUnix     = _maxSeenUnix;
        _sessionAnchorRealtime = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// "Bây giờ" theo giây UTC, ĐẢM BẢO KHÔNG BAO GIỜ LÙI.
    ///
    /// Lấy max của 3 nguồn:
    ///   1. đồng hồ hệ điều hành,
    ///   2. mốc neo đầu phiên + số giây đã CHƠI (Time.realtimeSinceStartup),
    ///   3. mốc lớn nhất từng thấy (đã lưu vào save).
    ///
    /// Nguồn (2) là điểm khác biệt quan trọng: nếu người chơi vặn giờ máy lùi lại,
    /// (1) và (3) sẽ đóng băng nhưng (2) vẫn tăng → chơi 5 phút là timer vẫn trôi
    /// 5 phút. Nếu chỉ dùng (3) như cách chống-cheat thông thường thì game sẽ đứng
    /// hình cho tới khi đồng hồ máy đuổi kịp — phạt oan cả người chơi thật khi máy
    /// tự đồng bộ NTP lùi vài giây.
    /// </summary>
    public long NowUnix()
    {
        long real    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long session = _sessionAnchorUnix + (long)(Time.realtimeSinceStartup - _sessionAnchorRealtime);

        long best = Math.Max(real, session);
        if (best > _maxSeenUnix) _maxSeenUnix = best;

        return _maxSeenUnix;
    }

    // ══════════════════════════════════════════════════════════════════════
    // API CHÍNH — DEV-1 GỌI VÀO ĐÂY
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// DEV-1 gọi trong `ConfirmPlacement()`.
    /// Trả <c>true</c> = DEV-2 nhận việc: DEV-1 KHÔNG Instantiate, KHÔNG ghi save,
    /// và tự giữ chỗ ô lưới.
    /// Trả <c>false</c> = bỏ qua giai đoạn xây, DEV-1 dựng công trình ngay như cũ.
    ///
    /// 📐 QUY ƯỚC TOẠ ĐỘ VỚI DEV-1 (chốt V7 — ghi giống hệt ở PlacementManager.ConfirmPlacement):
    ///    <paramref name="pos"/> là ĐIỂM NEO của Ghost (pivot ở ĐÁY sprite), ĐÃ SNAP lưới.
    ///    KHÔNG phải tâm khối ô. Art của dự án đặt pivot ở chân công trình nên hai điểm này
    ///    cách nhau nửa chiều cao sprite (chuồng bò: 224 unit ≈ 2.24 ô).
    ///    ConstructionManager tự đổi sang tâm khối ô bằng
    ///    <c>PlacementManager.AnchorToFootprintCenter()</c> để dựng giàn giáo / giữ ô / chạy VFX,
    ///    rồi đổi ngược lại khi Instantiate công trình thật.
    /// </summary>
    /// <param name="pos">ĐIỂM NEO world ĐÃ SNAP sẵn (chân công trình), KHÔNG phải tâm ô.</param>
    /// <param name="rotSteps">Số bước xoay 90°, 0-3.</param>
    public bool TryStartConstruction(PlaceableItemData data, Vector3 pos, int rotSteps, int plotId)
    {
        if (data == null || data.prefabToBuild == null) return false;

        // Hợp đồng §3: 0 giây = hiện ngay, không qua giai đoạn xây.
        if (data.buildTimeSeconds <= 0f) return false;

        if (maxConcurrentSites > 0 && _sites.Count >= maxConcurrentSites)
        {
            // Trả false → DEV-1 dựng công trình ngay. Chọn vậy vì tiền ĐÃ BỊ TRỪ ở Shop
            // trước khi tới đây; chặn hẳn sẽ làm người chơi mất tiền mà không có gì.
            Debug.LogWarning($"[Construction] Đã đủ {maxConcurrentSites} công trường cùng lúc " +
                             $"— '{data.itemName}' được dựng ngay thay vì xếp hàng xây.");
            return false;
        }

        rotSteps &= 3;
        Vector2Int size = PlacementManager.GridSizeOf(data, rotSteps);

        // ⚠️ KHÔNG snap lại. `pos` DEV-1 truyền vào ĐÃ snap sẵn (theo mốc lưới của điểm neo).
        // Snap lần hai làm công trình cạnh CHẴN lệch đúng NỬA Ô (+50 unit):
        // SnapCenter không idempotent với đầu vào đã là tâm khối chẵn, vì
        // ox = Floor(x/CELL − N*0.5 + 0.5) sẽ nhảy thêm 1 ô khi x đã nằm trên đường kẻ.
        Vector3 anchor = pos;

        ConstructionSite site = SpawnSite(data, anchor, rotSteps, plotId,
                                          NowUnix(), data.buildTimeSeconds);
        if (site == null) return false;

        _sites.Add(site);
        SaveSites();

        if (verboseLog)
            Debug.Log($"[Construction] Bắt đầu xây '{data.itemName}' {size.x}×{size.y} ô — " +
                      $"neo {anchor}, tâm ô {site.CenterWorld} — {data.buildTimeSeconds}s.");
        return true;
    }

    /// <summary>
    /// Huỷ một công trường (không dùng trong luồng chính, để sẵn cho tool/QA gỡ kẹt).
    /// Có hoàn tiền thì trả lại `goldPrice`, và LUÔN nhả chỗ ô đã giữ.
    /// </summary>
    public void CancelConstruction(ConstructionSite site, bool refund)
    {
        if (site == null) return;

        if (refund && site.Data != null && FarmEconomyManager.Instance != null && site.Data.goldPrice > 0)
            FarmEconomyManager.Instance.AddGold(site.Data.goldPrice);

        if (PlacementManager.Instance != null)
            PlacementManager.Instance.ReleaseConstructionCells(site.CenterWorld);

        _sites.Remove(site);
        Destroy(site.gameObject);
        SaveSites();
    }

    // ══════════════════════════════════════════════════════════════════════
    // RUSH
    // ══════════════════════════════════════════════════════════════════════

    public int GetRushCost(ConstructionSite site)
    {
        if (site == null) return 0;

        // Số cứng do designer đặt trong asset thì ưu tiên tuyệt đối.
        if (site.Data != null && site.Data.rushGemCost > 0) return site.Data.rushGemCost;

        return RushCostFor(site.RemainingSeconds(NowUnix()), rushBaseCost, rushSqrtFactor);
    }

    public bool CanAfford(int cost)
    {
        if (cost <= 0) return true;

        FarmEconomyManager eco = FarmEconomyManager.Instance;
        if (eco == null) return false;

        return RushUsesGems ? eco.Gems >= cost : eco.Gold >= cost;
    }

    /// <summary>
    /// Trừ tiền rồi kết thúc ngay công trường.
    /// KHÔNG BAO GIỜ mất tiền khi thất bại: mọi nhánh trả false đều xảy ra TRƯỚC lời gọi
    /// SpendGold/SpendGems, và bản thân hai hàm đó cũng tự kiểm tra số dư lần nữa.
    /// </summary>
    public bool TryRush(ConstructionSite site)
    {
        if (site == null || site.IsFinishing) return false;

        FarmEconomyManager eco = FarmEconomyManager.Instance;
        if (eco == null)
        {
            site.ShowMessage("Thiếu FarmEconomyManager — chưa trừ tiền được!");
            Debug.LogError("[Construction] Không tìm thấy FarmEconomyManager.Instance khi rush.");
            return false;
        }

        int cost = GetRushCost(site);
        string coinName = RushUsesGems ? "kim cương" : "vàng";

        if (!CanAfford(cost))
        {
            int have = RushUsesGems ? eco.Gems : eco.Gold;
            site.ShowMessage($"Không đủ {coinName}! Cần {cost}, đang có {have}.");
            return false;
        }

        bool paid = RushUsesGems ? eco.SpendGems(cost) : eco.SpendGold(cost);
        if (!paid)
        {
            site.ShowMessage($"Không đủ {coinName}!");
            return false;
        }

        site.FinishImmediately(NowUnix());
        SaveSites();

        if (verboseLog)
            Debug.Log($"[Construction] Rush '{site.Data?.itemName}' hết {cost} {coinName}.");
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    // HOÀN THÀNH
    // ══════════════════════════════════════════════════════════════════════

    private void CompleteSite(ConstructionSite site)
    {
        if (site == null || site.IsFinishing) return;

        // Không có PlacementManager thì công trình sẽ KHÔNG được ghi save → thà chờ.
        if (PlacementManager.Instance == null)
        {
            if (!_warnedNoPlacementManager)
            {
                _warnedNoPlacementManager = true;
                Debug.LogWarning("[Construction] Công trình đã xây xong nhưng chưa có " +
                                 "PlacementManager để ghi save — tạm hoãn hoàn thành.");
            }
            return;
        }

        site.MarkFinishing();
        site.HideConstructionVisuals();

        PlaceableItemData data   = site.Data;
        Vector3           center = site.CenterWorld;   // TÂM ô  → VFX ăn mừng
        Vector3           anchor = site.AnchorWorld;   // NEO    → chỗ Instantiate prefab
        int               rot    = site.RotationSteps;
        Vector2Int        size   = site.GridSize;

        // VFX phủ đúng vùng ô nên phải nhận TÂM; công trình thật thì mọc từ NEO.
        ConstructionCompleteFX.Play(
            center, size, SiteSortingLayerName, SiteBaseOrder + 60,
            ResolveCompleteVfxPrefab(), completeVfxScale, artKit,
            () => SpawnFinishedBuilding(site, data, anchor, rot));
    }

    /// <param name="anchor">ĐIỂM NEO — đúng vị trí Ghost của DEV-1 đã hiện lúc bấm ✓.</param>
    private void SpawnFinishedBuilding(ConstructionSite site, PlaceableItemData data,
                                       Vector3 anchor, int rot)
    {
        int plotId = site != null ? site.PlotId : 0;

        if (data != null && data.prefabToBuild != null)
        {
            GameObject spawned = Instantiate(data.prefabToBuild, anchor, PlacementManager.RotationOf(rot));

            // ⚠ BẮT BUỘC (hợp đồng §3.3): không gọi thì công trình KHÔNG được lưu,
            // ô lưới không được nhả, và mất trắng khi tắt game.
            if (PlacementManager.Instance != null)
                PlacementManager.Instance.RegisterCompletedBuilding(data, spawned, rot);
            else
                Debug.LogError("[Construction] PlacementManager biến mất giữa chừng — " +
                               "công trình đã hiện nhưng CHƯA được lưu.");
        }

        OnConstructionComplete?.Invoke(data, anchor, rot, plotId);

        if (site != null)
        {
            _sites.Remove(site);
            Destroy(site.gameObject);
        }

        SaveSites();
    }

    /// <summary>
    /// Prefab VFX ăn mừng: ưu tiên cái Edric gán; không có thì MƯỢN prefab confetti mà
    /// LevelUpPopupUI trong scene đang dùng (Assets/_Game/Farm/Prefabs/VFX/LevelUp/...).
    /// Prefab đó nằm ngoài Resources nên không Load bằng đường dẫn được — phải đi qua
    /// một instance trong scene. Field bên đó là private nên dùng reflection, bọc null-check.
    /// </summary>
    private GameObject ResolveCompleteVfxPrefab()
    {
        if (completeVfxPrefab != null) return completeVfxPrefab;

        var popup = FindFirstObjectByType<LevelUpPopupUI>(FindObjectsInactive.Include);
        if (popup == null) return null;

        FieldInfo f = typeof(LevelUpPopupUI).GetField(
            "vfxConfettiPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        completeVfxPrefab = f != null ? f.GetValue(popup) as GameObject : null;
        return completeVfxPrefab;
    }

    // ══════════════════════════════════════════════════════════════════════
    // TẠO CÔNG TRƯỜNG
    // ══════════════════════════════════════════════════════════════════════

    /// <param name="anchor">ĐIỂM NEO (chân công trình). Hàm tự đổi sang tâm khối ô.</param>
    private ConstructionSite SpawnSite(PlaceableItemData data, Vector3 anchor, int rotSteps,
                                       int plotId, long startUnix, float duration)
    {
        string label = data.prefabToBuild != null ? data.prefabToBuild.name : data.itemID;
        var go = new GameObject($"Construction_{label}");

        // Transform của công trường = TÂM KHỐI Ô.
        // ConstructionSiteVisuals.Build dựng thảm đất / giàn giáo / công nhân quanh
        // localPosition = 0 và phủ đúng gridSize×CELL, nên nếu để transform ở điểm neo thì
        // cả giàn giáo tụt xuống chân nhà ~2 ô và lệch hẳn khỏi vùng ô đã giữ chỗ.
        Vector3 center = PlacementManager.AnchorToFootprintCenter(anchor, data, rotSteps & 3);
        go.transform.position = new Vector3(center.x, center.y, 0f);

        var site = go.AddComponent<ConstructionSite>();

        // Truyền CẢ hai: `artKit` là nguồn chính, `workerSprite` chỉ là ô cũ dùng làm
        // dự phòng khi kit chưa có công nhân (giữ nguyên scene Edric đã set từ vòng 1).
        site.Initialize(this, data, rotSteps, plotId, startUnix, duration,
                        workerSprite, SiteSortingLayerName, SiteBaseOrder, artKit);
        return site;
    }

    // ══════════════════════════════════════════════════════════════════════
    // LƯU / NẠP
    // ══════════════════════════════════════════════════════════════════════

    private List<SiteEntry> _lastGoodEntries = new List<SiteEntry>();

    public void SaveSites()
    {
        var live = new List<SiteEntry>();
        bool sawDestroyed = false;

        for (int i = 0; i < _sites.Count; i++)
        {
            ConstructionSite s = _sites[i];

            // "fake null" của Unity = object đã bị Destroy (thường là lúc scene teardown).
            if (s == null) { sawDestroyed = true; continue; }
            if (s.Data == null) continue;

            live.Add(new SiteEntry
            {
                itemId    = s.Data.itemID,
                // Lưu ĐIỂM NEO, không lưu tâm ô: cùng hệ với FARM_PLACED_BUILDINGS của DEV-1,
                // và save cũ (trước khi bù pivot) cũng chính là điểm neo → đọc lại vẫn đúng.
                x         = s.AnchorWorld.x,
                y         = s.AnchorWorld.y,
                rot       = s.RotationSteps,
                plotId    = s.PlotId,
                startUnix = s.StartUnix,
                duration  = s.Duration
            });
        }

        // ⚠ BẪY LỚN NHẤT CỦA HỆ SAVE NÀY:
        // khi tắt game / đổi scene, Unity huỷ object theo THỨ TỰ KHÔNG XÁC ĐỊNH. Nếu các
        // công trường bị huỷ trước manager thì vòng lặp trên đọc ra danh sách RỖNG và ta
        // sẽ ghi đè save bằng con số 0 → mất sạch tiến độ đang xây. Phát hiện tình huống
        // đó thì ghi lại BẢN TỐT GẦN NHẤT. An toàn tuyệt đối vì mọi thay đổi nội dung
        // (bắt đầu xây / rush / xây xong) đều gọi SaveSites ngay lúc object còn sống.
        List<SiteEntry> entries;
        if (sawDestroyed && live.Count < _sites.Count)
        {
            // Chưa từng có bản tốt nào (teardown ngay sau khi load) → thà KHÔNG ghi
            // còn hơn ghi đè bằng danh sách rỗng.
            if (_lastGoodEntries.Count == 0) return;
            entries = _lastGoodEntries;
        }
        else
        {
            entries = live;
            _lastGoodEntries = live;
        }

        var save = new SitesSave
        {
            saveVersion = SaveVersion,
            maxSeenUnix = _maxSeenUnix,
            list        = entries
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void LoadSites()
    {
        if (_loaded) return;
        _loaded = true;

        if (!PlayerPrefs.HasKey(SaveKey)) return;

        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return;

        SitesSave save;
        try
        {
            save = JsonUtility.FromJson<SitesSave>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Construction] Save hỏng, bỏ qua: {e.Message}");
            return;
        }

        if (save?.list == null) return;

        if (save.saveVersion > SaveVersion)
        {
            Debug.LogWarning($"[Construction] Save version {save.saveVersion} mới hơn code " +
                             $"({SaveVersion}) — vẫn thử đọc, có thể thiếu trường.");
        }

        // Mốc chống lùi giờ phải nạp TRƯỚC khi tính thời gian còn lại của bất kỳ site nào.
        if (save.maxSeenUnix > _maxSeenUnix) _maxSeenUnix = save.maxSeenUnix;
        AnchorClock();

        long now = NowUnix();
        int restored = 0;

        foreach (SiteEntry e in save.list)
        {
            if (e == null) continue;

            PlaceableItemData data = FindItemById(e.itemId);
            if (data == null || data.prefabToBuild == null)
            {
                Debug.LogWarning($"[Construction] Bỏ qua công trường '{e.itemId}' — " +
                                 "không tra được data trong ShopManager.");
                continue;
            }

            Vector3    anchor = new Vector3(e.x, e.y, 0f);   // save lưu ĐIỂM NEO
            int        rot    = e.rot & 3;
            Vector2Int size   = PlacementManager.GridSizeOf(data, rot);

            ConstructionSite site = SpawnSite(data, anchor, rot, e.plotId,
                                              e.startUnix, Mathf.Max(0.1f, e.duration));
            if (site == null) continue;

            _sites.Add(site);
            restored++;

            // Giữ lại chỗ ô. Luồng đặt mới do ConfirmPlacement() của DEV-1 giữ,
            // nhưng luồng KHÔI PHỤC này không đi qua đó → phải tự giữ.
            // Truyền site.CenterWorld (TÂM ô) chứ không phải anchor — nếu truyền neo thì
            // vùng giữ tụt xuống ~2 ô và người chơi đặt đè thẳng lên giàn giáo.
            ConstructionBridge.ReserveCells(site.CenterWorld, size);
        }

        // Ghi lại ngay sau khi nạp: vừa cập nhật maxSeenUnix, vừa nạp `_lastGoodEntries`
        // để cơ chế chống-ghi-đè-rỗng trong SaveSites() có bản tốt để dựa vào.
        SaveSites();

        if (restored > 0)
        {
            Debug.Log($"[Construction] Khôi phục {restored} công trường đang xây " +
                      $"(thời gian offline đã được tính, mốc hiện tại = {now}).");
        }
    }

    /// <summary>Xoá toàn bộ công trường + save (dùng cho nút reset của dev tool).</summary>
    public void ClearAllSites()
    {
        for (int i = _sites.Count - 1; i >= 0; i--)
        {
            if (_sites[i] == null) continue;

            if (PlacementManager.Instance != null)
                PlacementManager.Instance.ReleaseConstructionCells(_sites[i].CenterWorld);

            Destroy(_sites[i].gameObject);
        }

        _sites.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Tra data theo itemID qua ShopManager (cùng nguồn DEV-1 dùng trong LoadBuildings,
    /// nên hai save luôn nhìn thấy cùng một tập item).
    /// </summary>
    private static PlaceableItemData FindItemById(string itemId)
    {
        if (ShopManager.Instance == null || string.IsNullOrEmpty(itemId)) return null;

        PlaceableItemData Match(List<BaseItemData> list)
        {
            if (list == null) return null;
            foreach (BaseItemData item in list)
            {
                if (item is PlaceableItemData p && p.itemID == itemId) return p;
            }
            return null;
        }

        return Match(ShopManager.Instance.buildingList) ?? Match(ShopManager.Instance.decorList);
    }
}
