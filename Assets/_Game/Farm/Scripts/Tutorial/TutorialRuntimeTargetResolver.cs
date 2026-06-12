using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    void LateUpdate()
    {
        UpdateProxyPositions();
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

        var bridge = GetComponent<TutorialStepTriggerBridge>();

        SetupRicePlotProxy(bridge);
        SetupFlowerPotProxy(bridge);
    }

    private void SetupRicePlotProxy(TutorialStepTriggerBridge bridge)
    {
        Transform t = null;
        if (bridge != null) t = bridge.GetFirstRicePlotTransform();

        if (t == null)
        {
            // Fallback: find first Normal PlotController in scene
            var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
            System.Array.Sort(all, (a, b) => a.PlotId.CompareTo(b.PlotId));
            foreach (var p in all) { if (p.Category == PlotCategory.Normal) { t = p.transform; break; } }
        }

        if (t == null) { Debug.LogWarning("[TutorialTargetResolver] Rice plot not found — tutorial_plot_01 skipped."); return; }
        CreateWorldProxy("tutorial_plot_01", t);
    }

    private void SetupFlowerPotProxy(TutorialStepTriggerBridge bridge)
    {
        Transform t = null;
        if (bridge != null) t = bridge.GetFirstFlowerPotTransform();

        if (t == null)
        {
            var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
            foreach (var p in all) { if (p.Category == PlotCategory.Flower) { t = p.transform; break; } }
        }

        if (t == null) { Debug.LogWarning("[TutorialTargetResolver] Flower pot not found — tutorial_flower_01 skipped."); return; }
        CreateWorldProxy("tutorial_flower_01", t);
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

            Vector3 screen = _cam.WorldToScreenPoint(worldT.position);
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
