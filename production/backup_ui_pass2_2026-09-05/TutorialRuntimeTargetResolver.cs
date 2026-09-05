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
    private static readonly Vector2 ShopCloseTargetPosition = new Vector2(857.7f, 266.8f);

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

    // [WP-A1] id chưa map đã cảnh báo — chỉ báo 1 lần / id để không spam Console mỗi frame.
    private static readonly HashSet<string> _idChuaMapDaBao = new HashSet<string>();

    /// <summary>Ô/chậu còn việc? plant → cần trống (IsEmpty); harvest → cần chín (IsReady).
    /// [WP-A1] Không rõ (chưa map) → trả FALSE + cảnh báo 1 lần. Trước đây trả true ⇒ tay cứ chỉ
    /// vào một ô "ma" không tồn tại trong gate ⇒ người chơi tưởng còn việc mà bước không qua.</summary>
    public static bool IsPlotPending(string id, bool needReady)
    {
        if (!_plotById.TryGetValue(id, out var pc) || pc == null)
        {
            if (_idChuaMapDaBao.Add(id))
                Debug.LogWarning($"[Tutorial][Gate] IsPlotPending: id '{id}' chưa map tới PlotController " +
                                 "→ coi là KHÔNG còn việc (tay bỏ qua ô này).");
            return false;
        }
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
    // Tutorial L2 — chuồng tutorial + thức ăn + rổ + nút Gem (B8–B13)
    // =========================================================================
    private IEnumerator PenScanLoop()
    {
        var wait = new WaitForSeconds(0.3f);
        while (true)
        {
            // Chuồng tutorial → world-proxy để tay chỉ giữa chuồng.
            // Dò theo TutorialManager.TenChuongCanDo (gồm cả bản "(Clone)") thay vì gõ
            // cứng một tên: chuồng có thể là object dựng sẵn trong scene HOẶC bản clone
            // do người chơi vừa mua, và trước đây chỉ bắt được trường hợp đầu.
            if (TutorialManager.GetTargetRect("tutorial_pen") == null)
            {
                foreach (string ten in TutorialManager.TenChuongCanDo)
                {
                    var pen = GameObject.Find(ten);
                    if (pen == null) continue;
                    CreateWorldProxy("tutorial_pen", pen.transform);
                    break;
                }
            }

            // Thức ăn + rổ: lấy TRỰC TIẾP từ chuồng ĐANG MỞ (slot UI THẬT đang hiển thị).
            // Trước đây quét toàn cục DraggableFeedItem — nhưng item đó có thể nằm ở popup khác
            // (inactive) → không bắt được → drag hint THIẾU ĐÍCH → KHÔNG HIỆN TAY. Dùng slot của
            // chuồng đang mở là chắc nhất; chỉ fallback quét DraggableFeedItem khi không có chuồng mở.
            PenMiniPanelUI openPen = null;
            foreach (var p in Object.FindObjectsByType<PenMiniPanelUI>(FindObjectsSortMode.None))
                if (p != null && p.IsPanelOpen()) { openPen = p; break; }

            // tutorial_feed = ô thức ăn ĐẦU (lúa) của chuồng đang mở (state Idle → slot1 active).
            if (openPen != null && openPen.FirstFeedSlotRect != null
                && openPen.FirstFeedSlotRect.gameObject.activeInHierarchy)
            {
                RetargetIfNeeded("tutorial_feed", openPen.FirstFeedSlotRect.gameObject);
            }
            else
            {
                DraggableFeedItem rice = null, leftmost = null;
                float bestX = float.MaxValue;
                foreach (var f in Object.FindObjectsByType<DraggableFeedItem>(FindObjectsSortMode.None))
                {
                    if (f == null || !f.gameObject.activeInHierarchy) continue;
                    string id = (f.feedItemId ?? "").ToLowerInvariant();
                    if (rice == null && (id.Contains("rice") || id.Contains("lua"))) rice = f;
                    float x = f.transform.position.x;
                    if (x < bestX) { bestX = x; leftmost = f; }
                }
                var pick = rice != null ? rice : leftmost;
                if (pick != null) RetargetIfNeeded("tutorial_feed", pick.gameObject);
            }

            // tutorial_basket = rổ thu hoạch của chuồng đang mở (state Ready → basket active).
            if (openPen != null && openPen.BasketSlotRect != null
                && openPen.BasketSlotRect.gameObject.activeInHierarchy)
            {
                RetargetIfNeeded("tutorial_basket", openPen.BasketSlotRect.gameObject);
            }
            else
            {
                PenBasketDragItem pick = null;
                foreach (var b in Object.FindObjectsByType<PenBasketDragItem>(FindObjectsSortMode.None))
                    if (b != null && b.gameObject.activeInHierarchy) { pick = b; break; }
                if (pick != null) RetargetIfNeeded("tutorial_basket", pick.gameObject);
            }

            // Nút Gem hoàn tất ('btn_PenGem' + OnClick → PenMiniPanelUI.TrySpeedUpGem) — chỉ cái ĐANG ACTIVE.
            {
                var g = GameObject.Find("btn_PenGem");   // Find chỉ trả object đang active
                if (g != null) RetargetIfNeeded("tutorial_pen_gem", g);
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
            RefreshShopTargets();
            yield return wait;
        }
    }

    public void RefreshShopTargets()
    {
        if (ShopManager.Instance == null || !ShopManager.Instance.IsOpen)
            return;

        if (TutorialManager.GetTargetRect("shop_corn") == null
            || TutorialManager.GetTargetRect("shop_corn_plus") == null
            || TutorialManager.GetTargetRect("shop_corn_buy") == null)
            TryRegisterCornShopItem();

        // Nút đóng CỦA SHOP — tìm trong shopPanel để chỉ tay chính xác (tránh trùng "Btn_Close" khác).
        if (TutorialManager.GetTargetRect("shop_close") == null && ShopManager.Instance.shopPanel != null)
        {
            var closeGo = FindCloseButton(ShopManager.Instance.shopPanel.transform);
            if (closeGo != null) AddRuntimeTarget(closeGo, "shop_close");
        }
    }

    private void TryRegisterCornShopItem()
    {
        foreach (var item in Object.FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None))
        {
            var data = item.Data;
            if (data == null) continue;
            bool isCorn = string.Equals(data.itemID, "seed_ngo", System.StringComparison.OrdinalIgnoreCase)
                || (data is CropData c && string.Equals(c.cropId, "ngo", System.StringComparison.OrdinalIgnoreCase));
            if (!isCorn) continue;

            AddRuntimeTarget(item.gameObject, "shop_corn");
            if (item.btnPlus != null) AddRuntimeTarget(item.btnPlus.gameObject, "shop_corn_plus");
            if (item.btnBuy  != null) AddRuntimeTarget(item.btnBuy.gameObject,  "shop_corn_buy");
            Debug.Log("[TutorialTargetResolver] Registered shop_corn (Ngô) + ＋/Mua.");
            return;
        }
    }

    // Tìm nút đóng trong shopPanel: Button có tên chứa "close"/"dong"/"x" (robust với Btn_Close, BtnClose...).
    private static GameObject FindCloseButton(Transform root)
    {
        if (root == null) return null;
        UnityEngine.UI.Button best = null;
        float bestScore = float.MaxValue;

        foreach (var btn in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            string n = btn.name.ToLowerInvariant();
            if (!n.Contains("close") && !n.Contains("dong") && n != "btn_x" && n != "x")
                continue;

            float score = 0f;
            RectTransform rt = btn.transform as RectTransform;
            if (rt != null)
                score = (rt.anchoredPosition - ShopCloseTargetPosition).sqrMagnitude;

            if (best == null || score < bestScore)
            {
                best = btn;
                bestScore = score;
            }
        }
        return best != null ? best.gameObject : null;
    }

    private static void AddRuntimeTarget(GameObject go, string id)
    {
        if (go == null) return;
        var tt = go.GetComponent<TutorialTarget>();
        if (tt == null) tt = go.AddComponent<TutorialTarget>();
        tt.SetTargetId(id);
    }

    /// <summary>Trỏ id → go, NHƯNG chỉ đăng ký lại khi cần: target hiện tại chưa có / đã inactive
    /// (chuồng đóng) / khác go đang active. Tránh giữ target cũ làm bàn tay trỏ sai chỗ.</summary>
    private static void RetargetIfNeeded(string id, GameObject go)
    {
        if (go == null) return;
        var cur = TutorialManager.GetTargetRect(id);
        if (cur != null && cur.gameObject == go && cur.gameObject.activeInHierarchy) return;
        AddRuntimeTarget(go, id);
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

        // [WP-A1] Lấy ĐÚNG tập ô mà gate đếm (Normal + IsUnlocked + không phải chậu hoa) rồi xếp theo RICE_ORDER.
        var ordered = OrderByTargets(LayTransform(TutorialStepTriggerBridge.LayODatLua()), RICE_ORDER);

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
            idx++;   // [WP-A1] không cắt ở 8 — tay phải phủ đúng tập ô của gate
        }
    }

    // =========================================================================
    // Plots Area Mask — nền xám bao quanh 6 ô đất (TutorialManager bật/tắt)
    // =========================================================================
    public void EnableAreaMask(TutorialAreaKind kind, UnmaskRaycastFilter dim)
    {
        // TẮT mask thì phải XOÁ LUÔN cái lỗ đang khoét.
        //
        // Trước đây chỉ đặt _areaKind = None: UpdatePlotsAreaMask() ngừng cập nhật,
        // NHƯNG UnmaskRaycastFilter vẫn giữ _useScreenRect = true và LateUpdate của nó
        // tiếp tục vẽ lại vùng sáng CŨ mỗi frame (nó dùng _screenRectCenterPx đã cache).
        // Kết quả: sang bước sau vẫn còn một ô sáng lơ lửng quanh mấy chậu hoa, trông
        // như tutorial đang chờ ở đó trong khi thật ra đã đi tiếp — rất dễ tưởng bị treo.
        if (kind == TutorialAreaKind.None)
        {
            if (_areaDim != null) _areaDim.ClearHole();
            _areaKind = TutorialAreaKind.None;
            return;
        }

        _areaKind = kind;
        _areaDim  = dim;
    }

    private void UpdatePlotsAreaMask()
    {
        if (_areaKind == TutorialAreaKind.None || _areaDim == null) return;
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

        var plots = _areaKind == TutorialAreaKind.Flower ? _flowerPots : _ricePlots;

        // KHÔNG có ô nào để bao (canvas tutorial null, hoặc log "plots not found") thì
        // phải TẮT lớp tối. Trước đây chỉ `return` được, vì lớp tối vẫn giữ lỗ cũ nên
        // click còn xuyên qua. Nay EnableAreaMask(None) đã ClearHole() ⇒ không còn lỗ ⇒
        // UnmaskRaycastFilter.IsRaycastLocationValid trả true = CHẶN SẠCH mọi click.
        // Bỏ mặc ở đây sẽ khoá cứng cả game, không cách nào bấm gì nữa.
        if (plots.Count == 0)
        {
            if (_areaDim.gameObject.activeSelf)
            {
                _areaDim.ClearHole();
                _areaDim.gameObject.SetActive(false);
                Debug.LogWarning($"[TutorialTargetResolver] Mask {_areaKind}: không có ô nào " +
                                 "để bao → đã TẮT lớp tối để không chặn click của người chơi.");
            }
            return;
        }

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

        // Ô đất: collider ngay trên gốc → tâm chuẩn (giữ nguyên hành vi cũ).
        var col = t.GetComponent<Collider2D>();
        if (col != null) return col.bounds.center;

        // Chuồng & vật thể nhiều mảnh (hàng rào): GỘP bounds TẤT CẢ renderer con → tâm hình học.
        // Tránh lấy renderer đầu tiên (1 cọc rào) làm tâm → tay chỉ bị lệch ra mép chuồng.
        var rends = t.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.center;
        }

        // Dự phòng: collider ở con (vùng click chuồng) rồi transform.
        var childCol = t.GetComponentInChildren<Collider2D>();
        if (childCol != null) return childCol.bounds.center;

        return t.position;
    }

    private void SetupFlowerPotProxy(TutorialStepTriggerBridge bridge)
    {
        _flowerPots.Clear();

        // [WP-A1] Lấy ĐÚNG tập chậu mà gate đếm (Flower + IsUnlocked) rồi xếp theo FLOWER_ORDER.
        var ordered = OrderByTargets(LayTransform(TutorialStepTriggerBridge.LayChauHoa()), FLOWER_ORDER);

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
            idx++;   // [WP-A1] không cắt ở 6 — tay phải phủ đúng tập chậu của gate
        }
    }

    // [WP-A1] Đổi danh sách PlotController (từ TutorialStepTriggerBridge) sang Transform để xếp thứ tự.
    // Thay cho FindPlotsByCategory cũ (lọc riêng, không xét IsUnlocked ⇒ tay lệch gate).
    private static List<Transform> LayTransform(List<PlotController> plots)
    {
        var result = new List<Transform>(plots.Count);
        foreach (var p in plots)
            if (p != null) result.Add(p.transform);
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
