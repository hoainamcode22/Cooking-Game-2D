using System;
using System.Collections.Generic;
using System.Globalization;   // F6: NumberStyles / CultureInfo.InvariantCulture cho TryParse
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

    /// <summary>
    /// Nhu OnClearStarted nhung dua thang CHINH CAI SITE ra.
    /// VI SAO CAN THEM: OnClearStarted ban trong Create(), TRUOC khi
    /// LandExpansionManager kip cat site vao _clearingSites — nen ai nghe
    /// OnClearStarted roi goi ClearingSiteOf() se luon nhan null.
    /// Su kien cu giu nguyen, khong doi chu ky, khong lam hong ai dang nghe.
    /// </summary>
    public static event Action<LandClearingSite> OnClearSiteStarted;

    [Header("Trang thai (chi doc)")]
    [SerializeField] private string regionId;
    [SerializeField] private long finishUnix;

    [Header("Co lui ve ban cu (Vong 25 — moi thay doi deu co duong lui)")]
    [Tooltip("Bat = dung lai cach cu: tu Instantiate tung tho roi de nguyen o mode Hidden. " +
             "Tat (mac dinh) = dung BuilderWorkerCrew — 3 tho dung quanh lo va CO dap bua.")]
    [SerializeField] private bool dungCachCu = false;

    [Tooltip("Bat = dung lai bui cach cu: 14 dom dat o TAM o vien, kich thuoc co dinh 34/55. " +
             "Tat (mac dinh) = bui bam doc CANH ngoai cua o vien, kich thuoc theo be rong o luoi.")]
    [SerializeField] private bool dungBuiCachCu = false;

    [Tooltip("[2026-09-25] Bat = moi tho dung 1 goc lo dat (3-4 nguoi). Tat = cum 3 nguoi nhu cu.")]
    [SerializeField] private bool dungGocLoDat = true;

    [Tooltip("[2026-09-25] Bat = bui + khoi + da / la / go bay tung quanh tho luc don lo.")]
    [SerializeField] private bool hieuUngDonDat = true;

    /// <summary>Ep MOI site tao ra sau day dung cach cu — duong lui mot dong khi crew tro chung.</summary>
    public static bool EpDungCachCu = false;

    /// <summary>Ep MOI site tao ra sau day dung bui cach cu.</summary>
    public static bool EpDungBuiCachCu = false;

    /// <summary>So tho Sep chot cho mot lo dat (yeu cau 2026-09-10: dung 3 nguoi).</summary>
    private const int SoThoSepChot = 3;

    /// <summary>Gia tri MAC DINH cua LandRegionData.workerCount. Bang gia tri nay = Sep chua dat tay.</summary>
    private const int WorkerCountMacDinhTrongData = 4;

    /// <summary>Toi da bao nhieu he hat bui tren mot duong vien — chan lo 100+ o sinh hang tram he.</summary>
    private const int SoDiemVienToiDa = 48;

    /// <summary>Be rong mot o ma cac hang so bui cu (34 / 55 / 26 / 90) duoc chinh theo.</summary>
    private const float DonViOThietKeCu = 100f;

    private LandRegionData _region;
    private LandExpansionManager _manager;
    private readonly List<GameObject> _spawned = new List<GameObject>();
    private BuilderWorkerCrew _crew;
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

    /// <summary>
    /// F6: doc moc thoi gian ket thuc tu PlayerPrefs MOT CACH AN TOAN.
    /// Truoc day dung long.Parse truc tiep: save hong / bi cat / ghi boi ban cu / may
    /// dat locale khac (dau phan cach khac) deu nem FormatException lam sap luon ca
    /// luong mo dat. Moi site anh em trong project da dung TryParse — day la cho sot.
    /// Luon doc bang InvariantCulture vi luc ghi cung la ToString() mac dinh cua long.
    /// </summary>
    private static long ReadFinishUnix(string regionId)
    {
        string raw = PlayerPrefs.GetString(KeyOf(regionId), "0");
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long fin))
            return fin;

        Debug.LogWarning($"[LandClearing] Moc thoi gian cua khu '{regionId}' hong ('{raw}') — coi nhu da xong (0).");
        return 0L;
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>Co cong truong dang chay cho khu nay khong (doc tu save).</summary>
    public static bool HasPending(string regionId)
        => PlayerPrefs.HasKey(KeyOf(regionId));

    /// <summary>Giay con lai theo save (dung khi site chua duoc tao).</summary>
    public static int PendingRemaining(string regionId)
    {
        if (!HasPending(regionId)) return 0;
        long fin = ReadFinishUnix(regionId);
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
        // Site duoc tao bang code nen field serialize khong the chinh trong Inspector truoc
        // khi chay — hai co static ben duoi la duong bat/tat that su cho ca game.
        site.dungCachCu = EpDungCachCu;
        site.dungBuiCachCu = EpDungBuiCachCu;

        if (HasPending(region.regionId))
        {
            site.finishUnix = ReadFinishUnix(region.regionId);
        }
        else
        {
            site.finishUnix = NowUnix() + Mathf.Max(0, region.clearSeconds);
            PlayerPrefs.SetString(KeyOf(region.regionId), site.finishUnix.ToString());
            PlayerPrefs.Save();
        }

        site.BuildVisuals();
        OnClearStarted?.Invoke(region);
        OnClearSiteStarted?.Invoke(site);

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

        // Cho to tho mo dan roi tu bien mat (DismissWithFade tu chan goi lai lan hai).
        // Goi o day, TRUOC Unlock, de tho van con thay trong luc dat vua mo ra.
        if (_crew != null) _crew.DismissWithFade();

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

        // Duong moi truoc; that bai (thieu config / tat feature flag) thi ROI VE cach cu
        // chu khong de lo dat trong khong co ai — Sep nhin vao chi thay "khong co gi xay ra".
        if (dungCachCu || !SpawnWorkerCrew()) SpawnWorkers();

        SpawnBorderDust();
    }

    // ── Duong MOI: BuilderWorkerCrew ─────────────────────────────────────
    /// <summary>
    /// So tho thuc su cho lo nay. Sep chot 3 nguoi, nhung neu ai do da dat tay
    /// workerCount trong asset lo (khac gia tri mac dinh 4) thi ton trong so do.
    /// LUU Y: BuilderWorkerCrew con kep lai theo cfg.minWorkers..maxWorkers (dang 1..3),
    /// nen dat 5 hay 6 trong data van chi ra toi da 3 tho.
    /// </summary>
    private int SoThoChoLo()
    {
        int n = _region != null ? _region.workerCount : SoThoSepChot;
        if (n <= 0 || n == WorkerCountMacDinhTrongData) return SoThoSepChot;
        return n;
    }

    /// <summary>
    /// Bounds world bao ca lo dat. AllCells() chi cho TAM tung o, nen phai no them
    /// nua o moi phia de bounds phu dung vung NHIN THAY chu khong phai chum diem tam.
    /// </summary>
    private bool TryLotBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool coO = false;

        foreach (var c in _region.AllCells())
        {
            Vector3 p = IsoGrid.CellCenterToWorld(c);
            if (!coO) { bounds = new Bounds(p, Vector3.zero); coO = true; }
            else bounds.Encapsulate(p);
        }
        if (!coO) return false;

        // Expand() cong vao TONG kich thuoc => moi phia duoc them nua o. Dung y muon.
        bounds.Expand(new Vector3(IsoGrid.CellWidth, IsoGrid.CellHeight, 0f));
        return true;
    }

    /// <summary>
    /// Dung to tho bang BuilderWorkerCrew (co san, da dung cho nha va decor).
    /// Tra false neu khong dung duoc — goi y quay ve SpawnWorkers().
    /// </summary>
    private bool SpawnWorkerCrew()
    {
        var cfg = Resources.Load<BuilderWorkerConfig>("BuilderWorkerConfig");
        if (cfg == null) return false;
        if (!TryLotBounds(out Bounds bounds)) return false;

        // [2026-09-25] Sep: tho dung 3-4 GOC lo dat cho can doi (truoc day dung cum 3 nguoi giua lo).
        Vector3[] goc = dungGocLoDat ? TinhGocLo(bounds) : null;
        _crew = goc != null ? BuilderWorkerCrew.AttachToTaiDiem(gameObject, bounds, cfg, goc)
                            : BuilderWorkerCrew.AttachTo(gameObject, bounds, cfg, SoThoChoLo());
        if (_crew == null) return false;   // cfg.enabled = false => AttachTo tra null ngay
        if (goc != null && hieuUngDonDat) gameObject.AddComponent<LandClearDebrisFX>().Init(goc, bounds);

        // BAT BUOC. AttachTo de moi tho o mode Hidden (SpriteRenderer tat) va cho
        // "nguoi dieu phoi" ra lenh. Khong goi dong nay = 3 tho VO HINH — dung loi
        // ma SpawnWorkers() ban cu dang mac phai.
        _crew.SetHammering();
        return true;
    }

    /// <summary>
    /// 4 goc hinh thoi cua lo (trai / phai / tren / duoi) lay tu TAM o ngoai cung, lui vao tam 12%.
    /// Lo nho (duoi 4 o) chi 3 goc (bo goc tren - nam sau lung, bi che). null neu khong co o.
    /// </summary>
    private Vector3[] TinhGocLo(Bounds b)
    {
        bool co = false;
        Vector3 trai = Vector3.zero, phai = Vector3.zero, tren = Vector3.zero, duoi = Vector3.zero;
        int so = 0;
        foreach (var c in _region.AllCells())
        {
            Vector3 p = IsoGrid.CellCenterToWorld(c);
            so++;
            if (!co) { trai = phai = tren = duoi = p; co = true; continue; }
            if (p.x < trai.x) trai = p;
            if (p.x > phai.x) phai = p;
            if (p.y > tren.y) tren = p;
            if (p.y < duoi.y) duoi = p;
        }
        if (!co) return null;
        Vector3 tam = b.center; tam.z = trai.z;
        System.Func<Vector3, Vector3> vao = v => Vector3.Lerp(v, tam, 0.12f);
        if (so < 4) return new[] { vao(trai), vao(phai), vao(duoi) };
        return new[] { vao(trai), vao(phai), vao(tren), vao(duoi) };
    }

    // ── Duong CU: giu nguyen de con cho lui ve ───────────────────────────
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
        EnsureDustRoot();
        if (dungBuiCachCu) { SpawnBorderDustCu(); return; }
        SpawnAlongOutline(_region.BorderCells());
    }

    private void EnsureDustRoot()
    {
        if (_dustRoot != null) return;
        _dustRoot = new GameObject("Dust_Border");
        _dustRoot.transform.SetParent(transform, false);
    }

    /// <summary>
    /// Rai bui doc DUONG VIEN NGOAI cua mot tap o luoi — dung cho hang rao lo dat,
    /// nhung viet chung de tinh nang sau (vung cam, vung dang xay...) dung lai duoc.
    ///
    /// Khac ban cu o hai cho:
    ///   • Diem phun nam tren CANH NGOAI cua o (tam o + nua o ve phia khong co hang xom),
    ///     nen bui ve dung duong bien chu khong phai mot hang cham ben trong.
    ///   • Kich thuoc / toc do hat tinh theo IsoGrid.CellWidth (mot o = 300 x 150 world),
    ///     khong con dinh 34 / 55 von chinh cho nhan vat cao ~100 unit.
    /// </summary>
    public void SpawnAlongOutline(IEnumerable<Vector2Int> cells)
    {
        if (cells == null) return;
        EnsureDustRoot();

        // Vector nua o theo hai truc luoi, doi sang world (khong tu che ma tran iso).
        Vector3 goc = IsoGrid.CellFloatToWorld(Vector2.zero);
        Vector3 nuaX = (IsoGrid.CellFloatToWorld(new Vector2(1f, 0f)) - goc) * 0.5f;
        Vector3 nuaY = (IsoGrid.CellFloatToWorld(new Vector2(0f, 1f)) - goc) * 0.5f;

        // HashSet chi de TRA CUU hang xom. Duyet theo danh sach goc de thu tu diem
        // con bam theo duong quet cua BorderCells() — neu duyet HashSet thi buoc nhay
        // ben duoi se boc mot nhum diem ngau nhien thay vi rai deu quanh vien.
        var danhSach = new List<Vector2Int>(cells);
        var tapO = new HashSet<Vector2Int>(danhSach);
        var diem = new List<Vector3>();

        foreach (var c in danhSach)
        {
            Vector3 tam = IsoGrid.CellCenterToWorld(c);
            if (!tapO.Contains(c + Vector2Int.right)) diem.Add(tam + nuaX);
            if (!tapO.Contains(c + Vector2Int.left))  diem.Add(tam - nuaX);
            if (!tapO.Contains(c + Vector2Int.up))    diem.Add(tam + nuaY);
            if (!tapO.Contains(c + Vector2Int.down))  diem.Add(tam - nuaY);
        }
        if (diem.Count == 0) return;

        // Chan tran so he hat: lo ~100 o co the ra hang tram canh ngoai.
        int step = Mathf.Max(1, Mathf.CeilToInt(diem.Count / (float)SoDiemVienToiDa));

        float oW = Mathf.Max(1f, IsoGrid.CellWidth);
        float k = oW / DonViOThietKeCu;            // o 300 unit => k = 3
        float size = 34f * k;                      // dam bui to theo o
        float banKinh = 55f * k * 0.5f;            // hep lai mot nua: bam sat duong bien

        for (int i = 0; i < diem.Count; i += step)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(_dustRoot.transform, false);
            go.transform.position = diem[i];
            MakeDust(go, 0.9f, size, false, banKinh, 26f * k, 90f * k);
        }
    }

    /// <summary>Ban bui cu — 14 dom o TAM o vien. Giu de lui ve khi dungBuiCachCu = true.</summary>
    private void SpawnBorderDustCu()
    {
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
        float k = dungBuiCachCu ? 1f : Mathf.Max(1f, IsoGrid.CellWidth) / DonViOThietKeCu;

        var cells = new List<Vector2Int>(_region.AllCells());
        int step = Mathf.Max(1, cells.Count / 10);
        for (int i = 0; i < cells.Count; i += step)
        {
            var go = new GameObject("Dust_Finish");
            go.transform.position = IsoGrid.CellCenterToWorld(cells[i]);
            MakeDust(go, 2.2f, 90f * k, true, 55f * k, 26f * k, 90f * k);
            Destroy(go, 1.4f);
        }
    }

    /// <summary>Tao mot ParticleSystem bui don gian bang code (khong can prefab).</summary>
    /// <remarks>Chu ky cu — giu nguyen so lieu goc de ban cu chay y het truoc.</remarks>
    private static void MakeDust(GameObject host, float rate, float size, bool burst = false)
        => MakeDust(host, rate, size, burst, 55f, 26f, 90f);

    /// <summary>Ban day du: goi y duoc ca ban kinh phun va toc do hat theo do lon o luoi.</summary>
    private static void MakeDust(GameObject host, float rate, float size, bool burst,
                                 float banKinh, float tocDoThuong, float tocDoBurst)
    {
        var ps = host.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.1f;
        main.startSpeed = burst ? tocDoBurst : tocDoThuong;
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
        sh.radius = banKinh;

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
        // To tho: mo dan chu khong Destroy thang — crew tu huy GameObject sau khi fade.
        // Idempotent (co _dismissed ben trong) nen Finish() goi truoc cung khong sao.
        if (_crew != null) { _crew.DismissWithFade(); _crew = null; }

        foreach (var g in _spawned) if (g != null) Destroy(g);
        _spawned.Clear();
        if (_dustRoot != null) Destroy(_dustRoot);
    }

    private void OnDestroy() => CleanupVisuals();
}
