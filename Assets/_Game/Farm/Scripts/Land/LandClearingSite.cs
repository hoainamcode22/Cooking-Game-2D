using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ============================================================================
/// CONG TRUONG DON O DAT — giai doan giua "da tra tien" va "dat mo ra"
/// ============================================================================
///
/// Sau khi nguoi choi mua o dat:
///   1. Site nay duoc tao o TAM lo dat.
///   2. Dem nguoc clearSeconds (luu theo giay UNIX nen tat game van chay tiep).
///   3. Sinh N cong nhan (BuilderWorker) rai deu BEN TRONG khung hang rao.
///   4. Sinh bui bam quanh vien lo (VFX ParticleSystem tao bang code).
///   5. Het gio (hoac rush bang kim cuong) -> goi LandExpansionManager.Unlock()
///      -> tile hang rao + lop phu bi xoa -> dat mo rong.
///
/// LUU TIEN DO: PlayerPrefs "LAND_CLEAR_<regionId>" = thoi diem UNIX hoan thanh.
/// Nho vay dong game giua chung, mo lai van dem tiep dung.
/// </summary>
public class LandClearingSite : MonoBehaviour
{
    private const string PrefsPrefix = "LAND_CLEAR_";

    public static event Action<LandRegionData> OnClearStarted;
    public static event Action<LandRegionData> OnClearFinished;

    [Header("Trang thai (chi doc)")]
    [SerializeField] private string regionId;
    [SerializeField] private long finishUnix;

    private LandRegionData _region;
    private LandExpansionManager _manager;
    private readonly List<GameObject> _spawned = new List<GameObject>();
    private GameObject _dustRoot;
    private bool _done;

    // ─────────────────────────────────────────────────────────────────────
    public LandRegionData Region => _region;
    public bool IsRunning => !_done && RemainingSeconds > 0;
    public int RemainingSeconds => Mathf.Max(0, (int)(finishUnix - NowUnix()));
    public float Progress01
    {
        get
        {
            if (_region == null || _region.clearSeconds <= 0) return 1f;
            return Mathf.Clamp01(1f - RemainingSeconds / (float)_region.clearSeconds);
        }
    }

    /// <summary>Kim cuong de xong ngay — 1 gem / 60 giay con lai, toi thieu 1.</summary>
    public int RushGemCost
    {
        get
        {
            if (_region != null && _region.rushGemCost > 0) return _region.rushGemCost;
            return Mathf.Max(1, Mathf.CeilToInt(RemainingSeconds / 60f));
        }
    }

    private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private static string KeyOf(string id) => PrefsPrefix + id;

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Co cong truong dang chay cho khu nay khong (doc tu save).</summary>
    public static bool HasPending(string regionId)
        => PlayerPrefs.HasKey(KeyOf(regionId));

    /// <summary>Giay con lai theo save (dung khi site chua duoc tao).</summary>
    public static int PendingRemaining(string regionId)
    {
        if (!HasPending(regionId)) return 0;
        long fin = long.Parse(PlayerPrefs.GetString(KeyOf(regionId), "0"));
        return Mathf.Max(0, (int)(fin - NowUnix()));
    }

    /// <summary>
    /// Tao cong truong cho mot khu. Neu save da co thi tiep tuc dem, khong reset.
    /// </summary>
    public static LandClearingSite Create(LandRegionData region, LandExpansionManager manager,
                                          Transform parent = null)
    {
        if (region == null || manager == null) return null;

        var go = new GameObject($"LandClearing_{region.regionId}");
        go.transform.SetParent(parent != null ? parent : manager.transform);
        go.transform.position = region.CenterWorld();

        var site = go.AddComponent<LandClearingSite>();
        site._region = region;
        site._manager = manager;
        site.regionId = region.regionId;

        if (HasPending(region.regionId))
        {
            site.finishUnix = long.Parse(PlayerPrefs.GetString(KeyOf(region.regionId), "0"));
        }
        else
        {
            site.finishUnix = NowUnix() + Mathf.Max(0, region.clearSeconds);
            PlayerPrefs.SetString(KeyOf(region.regionId), site.finishUnix.ToString());
            PlayerPrefs.Save();
        }

        site.BuildVisuals();
        OnClearStarted?.Invoke(region);

        if (site.RemainingSeconds <= 0) site.Finish();
        return site;
    }

    // ─────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_done) return;
        if (RemainingSeconds <= 0) Finish();
    }

    /// <summary>Hoan thanh ngay bang kim cuong. Tra false neu khong du.</summary>
    public bool TryRushWithGem()
    {
        if (_done) return false;
        int cost = RushGemCost;
        var eco = FarmEconomyManager.Instance;
        if (eco == null || !eco.SpendGems(cost)) return false;
        Finish();
        return true;
    }

    /// <summary>Ket thuc: mo khoa dat, doi VFX, tu huy.</summary>
    public void Finish()
    {
        if (_done) return;
        _done = true;

        PlayerPrefs.DeleteKey(KeyOf(regionId));
        PlayerPrefs.Save();

        PlayFinishBurst();
        if (_manager != null && _region != null) _manager.Unlock(_region);
        OnClearFinished?.Invoke(_region);

        CleanupVisuals();
        Destroy(gameObject, 1.2f);   // cho VFX bui chay not
    }

    // ─────────────────────────────────────────────────────────────────────
    // VISUAL: cong nhan trong lo + bui quanh vien
    // ─────────────────────────────────────────────────────────────────────
    private void BuildVisuals()
    {
        if (_region == null) return;
        SpawnWorkers();
        SpawnBorderDust();
    }

    private void SpawnWorkers()
    {
        var cfg = Resources.Load<BuilderWorkerConfig>("BuilderWorkerConfig");
        var cells = new List<Vector2Int>(_region.AllCells());
        if (cells.Count == 0) return;

        int n = Mathf.Clamp(_region.workerCount, 1, 6);
        // rai deu: chia lo thanh n phan theo thu tu o, lay o giua moi phan
        for (int i = 0; i < n; i++)
        {
            int idx = Mathf.Clamp((int)((i + 0.5f) / n * cells.Count), 0, cells.Count - 1);
            Vector3 pos = IsoGrid.CellCenterToWorld(cells[idx]);

            GameObject wgo = null;
            if (cfg != null && cfg.workerPrefabs != null && cfg.workerPrefabs.Length > 0)
            {
                var prefab = cfg.workerPrefabs[i % cfg.workerPrefabs.Length];
                if (prefab != null) wgo = Instantiate(prefab, pos, Quaternion.identity, transform);
            }
            if (wgo == null) continue;

            wgo.name = $"Worker_{i + 1}";
            var bw = wgo.GetComponent<BuilderWorker>();
            if (bw != null)
            {
                try { bw.Setup(cfg, i, i % 2 == 0, i / (float)n); }
                catch { /* signature khac -> bo qua, worker van hien */ }
            }
            _spawned.Add(wgo);
        }
    }

    /// <summary>Bui bam chay doc VIEN lo dat — cam giac dang don dep ca manh dat.</summary>
    private void SpawnBorderDust()
    {
        _dustRoot = new GameObject("Dust_Border");
        _dustRoot.transform.SetParent(transform, false);

        var border = new List<Vector2Int>(_region.BorderCells());
        if (border.Count == 0) return;

        // lay toi da 14 diem rai deu tren vien de khong ton hieu nang
        int step = Mathf.Max(1, border.Count / 14);
        for (int i = 0; i < border.Count; i += step)
        {
            Vector3 pos = IsoGrid.CellCenterToWorld(border[i]);
            var go = new GameObject("Dust");
            go.transform.SetParent(_dustRoot.transform, false);
            go.transform.position = pos;
            MakeDust(go, 0.9f, 34f);
        }
    }

    private void PlayFinishBurst()
    {
        // bung bui manh mot phat khap lo khi xong
        var cells = new List<Vector2Int>(_region.AllCells());
        int step = Mathf.Max(1, cells.Count / 10);
        for (int i = 0; i < cells.Count; i += step)
        {
            var go = new GameObject("Dust_Finish");
            go.transform.position = IsoGrid.CellCenterToWorld(cells[i]);
            MakeDust(go, 2.2f, 90f, burst: true);
            Destroy(go, 1.4f);
        }
    }

    /// <summary>Tao mot ParticleSystem bui don gian bang code (khong can prefab).</summary>
    private static void MakeDust(GameObject host, float rate, float size, bool burst = false)
    {
        var ps = host.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.1f;
        main.startSpeed = burst ? 90f : 26f;
        main.startSize = size;
        main.startColor = new Color(0.82f, 0.74f, 0.60f, 0.55f);
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;

        var em = ps.emission;
        if (burst)
        {
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });
        }
        else em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 55f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.86f, 0.79f, 0.65f), 0f),
                    new GradientColorKey(new Color(0.78f, 0.70f, 0.56f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.25f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        var sz = ps.sizeOverLifetime;
        sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));

        var r = host.GetComponent<ParticleSystemRenderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
        r.sortingOrder = 800;
    }

    private void CleanupVisuals()
    {
        foreach (var g in _spawned) if (g != null) Destroy(g);
        _spawned.Clear();
        if (_dustRoot != null) Destroy(_dustRoot);
    }

    private void OnDestroy() => CleanupVisuals();
}
