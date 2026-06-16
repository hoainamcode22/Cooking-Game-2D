using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Loại vùng mask tutorial (ô lúa / chậu hoa).</summary>
public enum TutorialAreaKind { None, Rice, Flower }

/// <summary>
/// Auto-registers tutorial targets at runtime — gắn cùng GameObject với TutorialManager.
///
/// Targets tự động đăng ký:
///   "seed_rice"          → SeedDragItem có CropData.cropId thuộc RICE_ALIASES
///   "seed_huong_duong"   → SeedDragItem có CropData.cropId thuộc FLOWER_ALIASES
///   "tutorial_plot_01"   → Proxy canvas theo dõi world-position ô lúa đầu tiên
///   "tutorial_flower_01" → Proxy canvas theo dõi world-position chậu hoa đầu tiên
///
/// Không sửa logic kéo hạt giống — chỉ đọc và đăng ký target.
/// Chạy nhiều lần safe (idempotent).
/// </summary>
public class TutorialRuntimeTargetResolver : MonoBehaviour
{
    // Crop ID aliases — khớp với CropData.cropId (không phải seed itemId)
    private static readonly string[] RICE_ALIASES = {
        "rice", "Rice", "lua", "Lua", "hat_lua", "seed_rice"
    };
    private static readonly string[] FLOWER_ALIASES = {
        "huong_duong", "Huong_Duong", "hoa_huong_duong", "seed_huong_duong", "sunflower"
    };

    [Tooltip("Canvas để tạo world-proxy UI elements. Nếu null, setup tool sẽ gán.")]
    [SerializeField] private Canvas _tutorialCanvas;

    // State
    private Camera _cam;

    // World-to-canvas proxies: Transform thế giới → RectTransform proxy trên canvas
    private readonly Dictionary<Transform, RectTransform> _worldProxies
        = new Dictionary<Transform, RectTransform>();

    // id (vd "tutorial_plot_03") → PlotController, để bàn tay biết ô nào CÒN VIỆC (trống/chín).
    private static readonly Dictionary<string, PlotController> _plotById
        = new Dictionary<string, PlotController>();

    /// <summary>Ô/chậu còn việc? plant → cần trống (IsEmpty); harvest → cần chín (IsReady).
    /// Không rõ (chưa map) → trả true để vẫn hiện tay (an toàn).</summary>
    public static bool IsPlotPending(string id, bool needReady)
    {
        if (!_plotById.TryGetValue(id, out var pc) || pc == null) return true;
        return needReady ? pc.IsReady : pc.IsEmpty;
    }

    // Ô lúa & chậu hoa (đã xếp ĐÚNG THỨ TỰ user gửi) + mask vùng xám bao quanh
    private readonly List<Transform> _ricePlots  = new List<Transform>();
    private readonly List<Transform> _flowerPots = new List<Transform>();
    private UnmaskRaycastFilter _areaDim;
    private TutorialAreaKind _areaKind = TutorialAreaKind.None;

    // Thứ tự 8 ô đất — theo đúng pos user gửi (transform.position). Tay quét theo thứ tự này.
    private static readonly Vector2[] RICE_ORDER = {
        new Vector2(2098.474f,  -810.379f), new Vector2(1877.763f,  -933.307f),
        new Vector2(2109.649f, -1056.234f), new Vector2(2333.154f,  -938.895f),
        new Vector2(2344.329f, -1165.193f), new Vector2(2562.245f, -1050.647f),
        new Vector2(2579.571f, -1284.817f), new Vector2(2789.774f, -1165.786f),
    };
    // Thứ tự 6 chậu hoa — theo đúng pos user gửi.
    private static readonly Vector2[] FLOWER_ORDER = {
        new Vector2(1596f, -1287f), new Vector2(1778f, -1284f), new Vector2(1944f, -1281f),
        new Vector2(1945f, -1161f), new Vector2(1778f, -1161f), new Vector2(1599f, -1161f),
    };

    [Header("Plots Area Mask (nền xám bao các ô)")]
    [Tooltip("Padding theo % kích thước vùng 6 ô")]
    [SerializeField] private float _areaScreenPadPct = 0.18f;
    [Tooltip("Padding cố định thêm vào (px)")]
    [SerializeField] private float _areaScreenPadPx  = 70f;

    // =========================================================================
    void Awake()
    {
        _cam = Camera.main;
    }

    void Start()
    {
        if (_tutorialCanvas == null)
            _tutorialCanvas = FindTutorialCanvas();

        StartCoroutine(SetupPlotProxiesNextFrame());
        StartCoroutine(SeedScanLoop());
        StartCoroutine(ShopCornScanLoop());
        StartCoroutine(PenScanLoop());
    }

    // =========================================================================
    // Tutorial L2 — chuồng Pen_03 + thức ăn + rổ + nút Gem (B8–B13)
    // =========================================================================
    private IEnumerator PenScanLoop()
    {
        var wait = new WaitForSeconds(0.3f);
        while (true)
        {
            // Pen_03 (world object luôn có trong scene) → world-proxy để tay chỉ giữa chuồng
            if (TutorialManager.GetTargetRect("tutorial_pen") == null)
            {
                var pen = GameObject.Find("Pen_03");
                if (pen != null) CreateWorldProxy("tutorial_pen", pen.transform);
            }

            // Thức ăn kéo-thả (sinh khi mở panel chuồng, state Idle)
            if (TutorialManager.GetTargetRect("tutorial_feed") == null)
                foreach (var f in Object.FindObjectsByType<DraggableFeedItem>(FindObjectsSortMode.None))
                    if (f != null && f.gameObject.activeInHierarchy)
                    { AddRuntimeTarget(f.gameObject, "tutorial_feed"); break; }

            // Rổ thu hoạch (sinh khi state Ready)
            if (TutorialManager.GetTargetRect("tutorial_basket") == null)
                foreach (var b in Object.FindObjectsByType<PenBasketDragItem>(FindObjectsSortMode.None))
                    if (b != null && b.gameObject.activeInHierarchy)
                    { AddRuntimeTarget(b.gameObject, "tutorial_basket"); break; }

            // Nút Gem hoàn tất (prefab cần đặt tên 'btn_PenGem' + OnClick → PenMiniPanelUI.TrySpeedUpGem)
            if (TutorialManager.GetTargetRect("tutorial_pen_gem") == null)
            {
                var g = GameObject.Find("btn_PenGem");
                if (g != null) AddRuntimeTarget(g, "tutorial_pen_gem");
            }

            yield return wait;
        }
    }

    // =========================================================================
    // Tutorial L2 — đăng ký item Ngô trong shop (sinh runtime khi shop mở)
    // =========================================================================
    private IEnumerator ShopCornScanLoop()
    {
        var wait = new WaitForSeconds(0.3f);
        while (true)
        {
            if (ShopManager.Instance != null && ShopManager.Instance.IsOpen
                && TutorialManager.GetTargetRect("shop_corn") == null)
                TryRegisterCornShopItem();
            yield return wait;
        }
    }

    private void TryRegisterCornShopItem()
    {
        foreach (var item in Object.FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None))
        {
            var data = item.Data;
            if (data == null) continue;
            bool isCorn = data.itemID == "seed_ngo" || (data is CropData c && c.cropId == "ngo");
            if (!isCorn) continue;

            AddRuntimeTarget(item.gameObject, "shop_corn");
            if (item.btnPlus != null) AddRuntimeTarget(item.btnPlus.gameObject, "shop_corn_plus");
            if (item.btnBuy  != null) AddRuntimeTarget(item.btnBuy.gameObject,  "shop_corn_buy");
            Debug.Log("[TutorialTargetResolver] Registered shop_corn (Ngô) + ＋/Mua.");
            return;
        }
    }

    private static void AddRuntimeTarget(GameObject go, string id)
    {
        if (go == null) return;
        var tt = go.GetComponent<TutorialTarget>();
        if (tt == null) tt = go.AddComponent<TutorialTarget>();
        tt.SetTargetId(id);
    }

    void LateUpdate()
    {
        UpdateProxyPositions();
        UpdatePlotsAreaMask();
    }

    // =========================================================================
    // Seed Target Registration
    // =========================================================================

    private IEnumerator SeedScanLoop()
    {
        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            // Re-scan whenever target drops out of registry (seed panel closed/reopened destroys & recreates SeedDragItems)
            if (TutorialManager.GetTargetRect("seed_rice") == null)
                TryScanSeed("seed_rice", RICE_ALIASES);
            if (TutorialManager.GetTargetRect("seed_huong_duong") == null)
                TryScanSeed("seed_huong_duong", FLOWER_ALIASES);
            yield return wait;
        }
    }

    private void TryScanSeed(string targetId, string[] aliases)
    {
        var allSeeds = Object.FindObjectsByType<SeedDragItem>(FindObjectsSortMode.None);
        foreach (var seed in allSeeds)
        {
            var cropId = seed.CropId;
            if (string.IsNullOrEmpty(cropId)) continue;
            foreach (var alias in aliases)
            {
                if (string.Equals(cropId, alias, System.StringComparison.OrdinalIgnoreCase))
                {
                    RegisterSeed(seed, targetId);
                    return;
                }
            }
        }
    }

    private void RegisterSeed(SeedDragItem seed, string targetId)
    {
        if (seed.GetComponent<RectTransform>() == null) return;

        var tt = seed.GetComponent<TutorialTarget>();
        if (tt == null) tt = seed.gameObject.AddComponent<TutorialTarget>();
        tt.SetTargetId(targetId);

        Debug.Log($"[TutorialTargetResolver] Registered '{targetId}' → '{seed.gameObject.name}' (cropId={seed.CropId})");
    }

    // =========================================================================
    // World-Space Plot Proxies
    // =========================================================================

    private IEnumerator SetupPlotProxiesNextFrame()
    {
        yield return null; // wait one frame for scene fully ready

        if (_tutorialCanvas == null)
        {
            Debug.LogWarning("[TutorialTargetResolver] No tutorial canvas — world proxies skipped.");
            yield break;
        }

        _plotById.Clear();

        var bridge = GetComponent<TutorialStepTriggerBridge>();

        SetupRicePlotProxy(bridge);
        SetupFlowerPotProxy(bridge);
    }

    private void SetupRicePlotProxy(TutorialStepTriggerBridge bridge)
    {
        _ricePlots.Clear();

        // Lấy TẤT CẢ ô Normal rồi xếp theo đúng thứ tự pos user gửi (RICE_ORDER).
        var ordered = OrderByTargets(FindPlotsByCategory(PlotCategory.Normal), RICE_ORDER);

        if (ordered.Count == 0)
        {
            Debug.LogWarning("[TutorialTargetResolver] Rice plots not found — tutorial_plot_xx skipped.");
            return;
        }

        int idx = 1;
        foreach (var t in ordered)
        {
            if (t == null) continue;
            _ricePlots.Add(t);
            CreateWorldProxy($"tutorial_plot_{idx:00}", t);   // tutorial_plot_01 … tutorial_plot_08
            idx++;
            if (idx > 8) break;
        }
    }

    // =========================================================================
    // Plots Area Mask — nền xám bao quanh 6 ô đất (TutorialManager bật/tắt)
    // =========================================================================
    public void EnableAreaMask(TutorialAreaKind kind, UnmaskRaycastFilter dim)
    {
        _areaKind = kind;
        if (kind != TutorialAreaKind.None) _areaDim = dim;
    }

    private void UpdatePlotsAreaMask()
    {
        if (_areaKind == TutorialAreaKind.None || _areaDim == null) return;
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        var plots = _areaKind == TutorialAreaKind.Flower ? _flowerPots : _ricePlots;
        if (plots.Count == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        int n = 0;
        foreach (var t in plots)
        {
            if (t == null) continue;
            Vector3 s = _cam.WorldToScreenPoint(PlotVisualCenter(t));
            if (s.z < 0f) continue;
            minX = Mathf.Min(minX, s.x); maxX = Mathf.Max(maxX, s.x);
            minY = Mathf.Min(minY, s.y); maxY = Mathf.Max(maxY, s.y);
            n++;
        }
        if (n == 0) return;

        float w = maxX - minX;
        float h = maxY - minY;
        float padX = w * _areaScreenPadPct + _areaScreenPadPx;
        float padY = h * _areaScreenPadPct + _areaScreenPadPx;

        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        Vector2 size   = new Vector2(w + padX * 2f, h + padY * 2f);

        _areaDim.SetScreenRect(center, size, false);
    }

    /// <summary>
    /// Tâm "nhìn thấy" của ô đất = tâm collider/renderer (đã gồm offset+scale).
    /// KHÔNG dùng transform.position vì gốc transform nằm dưới đáy tile → tay/mask bị lệch xuống.
    /// </summary>
    private static Vector3 PlotVisualCenter(Transform t)
    {
        if (t == null) return Vector3.zero;
        var col = t.GetComponent<Collider2D>();
        if (col != null) return col.bounds.center;
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;
        return t.position;
    }

    private void SetupFlowerPotProxy(TutorialStepTriggerBridge bridge)
    {
        _flowerPots.Clear();

        // Lấy TẤT CẢ chậu Flower rồi xếp theo đúng thứ tự pos user gửi (FLOWER_ORDER).
        var ordered = OrderByTargets(FindPlotsByCategory(PlotCategory.Flower), FLOWER_ORDER);

        if (ordered.Count == 0)
        {
            Debug.LogWarning("[TutorialTargetResolver] Flower pots not found — tutorial_flower_xx skipped.");
            return;
        }

        int idx = 1;
        foreach (var t in ordered)
        {
            if (t == null) continue;
            _flowerPots.Add(t);
            CreateWorldProxy($"tutorial_flower_{idx:00}", t);  // tutorial_flower_01 … tutorial_flower_06
            idx++;
            if (idx > 6) break;
        }
    }

    // Tìm mọi PlotController theo category.
    private static List<Transform> FindPlotsByCategory(PlotCategory cat)
    {
        var result = new List<Transform>();
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p != null && p.Category == cat) result.Add(p.transform);
        return result;
    }

    // Xếp candidates theo đúng thứ tự target: mỗi target gán với ô gần nhất CHƯA dùng.
    private static List<Transform> OrderByTargets(List<Transform> candidates, Vector2[] targets)
    {
        var pool   = new List<Transform>(candidates);
        var result = new List<Transform>();
        foreach (var tg in targets)
        {
            Transform best = null; float bestD = float.MaxValue;
            foreach (var c in pool)
            {
                if (c == null) continue;
                float d = ((Vector2)c.position - tg).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            if (best != null) { result.Add(best); pool.Remove(best); }
        }
        foreach (var c in pool) if (c != null) result.Add(c); // ô dư (nếu có) thêm cuối
        return result;
    }

    private void CreateWorldProxy(string targetId, Transform worldTarget)
    {
        // Avoid duplicates
        if (_worldProxies.ContainsKey(worldTarget)) return;

        var proxyGo = new GameObject($"TutorialProxy_{targetId}", typeof(RectTransform));
        proxyGo.transform.SetParent(_tutorialCanvas.transform, false);
        proxyGo.layer = gameObject.layer;

        var rt = proxyGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 80f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        var tt = proxyGo.AddComponent<TutorialTarget>();
        tt.SetTargetId(targetId);

        _worldProxies[worldTarget] = rt;

        var pc = worldTarget.GetComponent<PlotController>();
        if (pc != null) _plotById[targetId] = pc;   // để bàn tay biết ô còn trống/đã chín

        Debug.Log($"[TutorialTargetResolver] Registered '{targetId}' → world-proxy for '{worldTarget.name}'");
    }

    private void UpdateProxyPositions()
    {
        if (_cam == null) { _cam = Camera.main; return; }

        foreach (var pair in _worldProxies)
        {
            var worldT = pair.Key;
            var proxyRT = pair.Value;
            if (worldT == null || proxyRT == null) continue;

            Vector3 screen = _cam.WorldToScreenPoint(PlotVisualCenter(worldT));
            bool behindCam = screen.z < 0f;
            proxyRT.gameObject.SetActive(!behindCam);
            if (!behindCam) proxyRT.position = screen;
        }
    }

    // =========================================================================
    private static Canvas FindTutorialCanvas()
    {
        var all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in all) if (c.name == "Canvas_TutorialOverlay") return c;
        foreach (var c in all) if (c.name.Contains("Tutorial")) return c;
        Canvas best = null; int bestOrder = int.MinValue;
        foreach (var c in all)
            if (c.sortingOrder > bestOrder) { bestOrder = c.sortingOrder; best = c; }
        return best;
    }
}
