using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TỔ THỢ (1–3 người) QUANH MỘT CÔNG TRÌNH — đây là API Lead/DEV-D gọi.
/// ════════════════════════════════════════════════════════════════════
///
/// CÁCH DÙNG NGẮN NHẤT:
/// <code>
///   var crew = BuilderWorkerCrew.AttachTo(decorGO, ctrl.VisualBounds, cfg);
///   // cfg null hoặc cfg.enabled == false ⇒ crew == null, KHÔNG có gì được tạo (§9)
/// </code>
///
/// AUTO-WIRE: nếu <c>host</c> (hoặc cha của nó) có <see cref="DecorGrowthController"/>,
/// crew TỰ subscribe 4 event và tự diễn theo §6 CONTRACT — bên gọi không cần làm gì thêm:
/// <code>
///   OnStageChanged(stage 1|2) → SetHammering()
///   OnGiftBoxReady            → SetIdleAtGift()    (đứng im ở celebrate frame 0)
///   OnRevealStarted           → SetCelebrating()
///   OnRevealFinished          → DismissWithFade()
/// </code>
/// (KHÔNG dùng OnStateChanged — xem ghi chú về DecorState bên dưới.)
/// Không có DecorGrowthController (ví dụ nhà village) ⇒ crew nằm im ở Hidden,
/// <see cref="HouseWorkerBridge"/> sẽ POLL rồi ra lệnh.
///
/// ⚠ VÌ SAO FILE NÀY KHÔNG BAO GIỜ VIẾT CHỮ <c>DecorState</c>:
///   Bản API chốt trên CONTRACT khai enum đó ở cấp global, nhưng code DEV-A giao lại khai
///   NHỎ BÊN TRONG controller (<c>DecorGrowthController.DecorState</c>). Hai cách viết đó
///   loại trừ nhau: gõ tên nào cũng có nguy cơ CS0246 khi Lead gộp. Nên crew CHỈ đọc
///   <c>CurrentStage</c> (int 1..5) — con số này đã được §5.4 CONTRACT chốt cứng theo ô
///   spritesheet, không phụ thuộc enum nằm ở đâu:
///     stage 1,2 = đang xây · stage 3 = hoàn thiện · stage 4 = hộp quà · stage 5 = hộp bung
///   Nhờ vậy file này biên dịch được với CẢ HAI cách khai enum, Lead không phải sửa gì.
///   (Cũng vì thế crew không nghe <c>OnStateChanged</c> — chữ ký của nó bắt buộc phải
///   gõ tên enum. Bốn event còn lại đã phủ đủ toàn bộ hành vi §6.)
///
/// VỊ TRÍ THỢ — nguyên tắc: KHÔNG BAO GIỜ CHE MẶT CÔNG TRÌNH.
/// Cả 3 điểm đều nằm ở mép DƯỚI hoặc ngoài 2 bên, không có điểm nào bên trong bounds:
/// <code>
///   điểm 1 = (min.x - padding, min.y)              — bên trái, sát chân
///   điểm 2 = (max.x + padding, min.y)              — bên phải, sát chân
///   điểm 3 = (center.x, min.y - padding * 0.5f)    — chính giữa, thấp hơn chân
/// </code>
/// Thợ ở bên PHẢI tâm công trình thì <c>faceLeft = true</c> để nhìn VÀO công trình.
///
/// LỆCH NHỊP: thợ i có <c>phaseOffset01 = (i * phaseSpreadSeconds * hammerFps) / 12f</c> mod 1.
/// Với 0.4s @ 10fps ⇒ 0, 4/12, 8/12 — ba con đập lệch hẳn 1/3 vòng, không thành "một khối".
///
/// KHÔNG bao giờ AddComponent&lt;DecorGrowthController&gt; hay sửa nó — chỉ NGHE event.
///
/// [Worker]
/// </summary>
[DisallowMultipleComponent]
public class BuilderWorkerCrew : MonoBehaviour
{
    private const int SO_FRAME_MOT_VONG = 12;   // cả 2 sheet thợ đều 12 frame (§5.2/§5.3)

    /// <summary>
    /// Bounds phải lệch quá 10% mới xếp lại thợ. NGUỒN DUY NHẤT của ngưỡng này —
    /// <see cref="HouseWorkerBridge"/> cũng gọi <see cref="BoundsChangedSignificantly"/>
    /// nên hai đường (decor + nhà village) không bao giờ lệch nhau.
    /// </summary>
    public const float BOUNDS_CHANGE_THRESHOLD = 0.10f;

    private BuilderWorkerConfig _cfg;
    private readonly List<BuilderWorker> _workers = new List<BuilderWorker>();

    private Bounds _bounds;
    private bool   _dismissed;

    private DecorGrowthController _decor;
    private bool _decorWired;

    /// <summary>
    /// Công trình này chạy chế độ WORKER-ONLY của DEV-A (chuồng / máy — KHÔNG có art 5
    /// stage): sprite giữ nguyên suốt, <c>CurrentStage</c> chỉ ra 1 hoặc 2, và hệ BỎ QUA
    /// hẳn giai đoạn hộp quà. Thợ vì thế TUYỆT ĐỐI không được vào IdleAtGift — không có
    /// hộp nào để chờ, thợ mà đứng im thì trông như bị treo.
    /// </summary>
    private bool _workerOnly;

    /// <summary>Số thợ đang sống trong tổ.</summary>
    public int WorkerCount => _workers.Count;

    /// <summary>Bounds công trình mà tổ đang bám theo (world space).</summary>
    public Bounds HostBounds => _bounds;

    /// <summary>Công trình chủ đang ở chế độ WORKER-ONLY (chuồng/máy, không có hộp quà).</summary>
    public bool IsWorkerOnlyHost => _workerOnly;

    // ── Tạo tổ ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn một tổ thợ vào <paramref name="host"/>.
    /// </summary>
    /// <param name="host">GameObject công trình. Tổ được tạo làm CON của nó nên bị
    /// Destroy cùng công trình — không sợ rác.</param>
    /// <param name="hostBounds">Bounds world của công trình (dùng để xếp thợ).</param>
    /// <param name="cfg">Cấu hình. <b>null hoặc !enabled ⇒ trả null NGAY</b> (§9 CONTRACT).</param>
    /// <param name="forcedCount">&gt;0 = ép số thợ; 0 = tự tính theo diện tích chân.</param>
    /// <returns>Tổ vừa tạo, tổ đã có sẵn, hoặc null nếu feature flag tắt.</returns>
    public static BuilderWorkerCrew AttachTo(GameObject host, Bounds hostBounds,
                                             BuilderWorkerConfig cfg, int forcedCount = 0)
    {
        if (cfg == null || !cfg.enabled) return null;   // FEATURE FLAG — thoát trước mọi cấp phát
        if (host == null) return null;

        // Đã có tổ rồi thì trả lại tổ đó, tuyệt đối không tạo trùng (2 hook cùng gọi)
        BuilderWorkerCrew daCo = host.GetComponentInChildren<BuilderWorkerCrew>(true);
        if (daCo != null)
        {
            daCo.RefreshLayout(hostBounds);
            return daCo;
        }

        GameObject go = new GameObject("BuilderCrew");
        go.transform.SetParent(host.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // [KHU_TRU_SCALE_HOST — Lead 2026-09-03] Prefab decor cũ có transform scale = 100
        // (đo thật: "Bù nhìn 1.prefab" m_LocalScale 100,100). Crew là CON của host nên nếu
        // để localScale = 1, mọi thợ bên trong bị nhân ×100: thợ 170 unit thành 17.000 unit
        // che kín map (bug Sếp báo 2026-09-02), offset đứng ±150 unit thành ±15.000.
        // Khử tại GỐC crew: localScale = 1/lossyScale(host) ⇒ không gian crew = không gian
        // world, thợ cao đúng workerWorldHeight (170 = bằng shipper), đứng đúng chỗ,
        // BẤT KỂ host scale 1 (4 prefab decor mới) hay 100 (prefab cũ) hay nhà.
        Vector3 lsHost = host.transform.lossyScale;
        go.transform.localScale = new Vector3(
            1f / Mathf.Max(0.0001f, Mathf.Abs(lsHost.x)),
            1f / Mathf.Max(0.0001f, Mathf.Abs(lsHost.y)),
            1f);

        BuilderWorkerCrew crew = go.AddComponent<BuilderWorkerCrew>();
        crew.KhoiTao(host, hostBounds, cfg, forcedCount);
        return crew;
    }

    private void KhoiTao(GameObject host, Bounds hostBounds, BuilderWorkerConfig cfg, int forcedCount)
    {
        _cfg    = cfg;
        _bounds = hostBounds;

        int count = forcedCount > 0
            ? Mathf.Clamp(forcedCount, Mathf.Min(cfg.minWorkers, cfg.maxWorkers),
                                       Mathf.Max(cfg.minWorkers, cfg.maxWorkers))
            : cfg.WorkerCountForFootprint(hostBounds.size);

        count = Mathf.Max(0, count);

        for (int i = 0; i < count; i++)
        {
            BuilderWorker w = TaoMotThoMoi(i);
            if (w != null) _workers.Add(w);
        }

        ApDungViTri();
        NoiDayVaoDecor(host);
    }

    /// <summary>
    /// Dựng một con thợ. Prefab lấy theo <c>workerPrefabs[i % 3]</c>; ô null hoặc mảng
    /// rỗng ⇒ dựng GameObject trống rồi để <see cref="BuilderWorker"/> tự AddComponent
    /// SpriteRenderer + SpriteSequencePlayer. KHÔNG crash khi thiếu art.
    /// </summary>
    private BuilderWorker TaoMotThoMoi(int i)
    {
        GameObject prefab = null;
        if (_cfg.workerPrefabs != null && _cfg.workerPrefabs.Length > 0)
            prefab = _cfg.workerPrefabs[i % Mathf.Max(1, _cfg.workerPrefabs.Length)];

        GameObject inst;
        if (prefab != null)
        {
            inst = Instantiate(prefab, transform);
            inst.name = $"BuilderWorker_{i}";
        }
        else
        {
            inst = new GameObject($"BuilderWorker_{i}");
            inst.transform.SetParent(transform, false);
        }

        inst.transform.localRotation = Quaternion.identity;

        BuilderWorker w = inst.GetComponent<BuilderWorker>();
        if (w == null) w = inst.AddComponent<BuilderWorker>();

        w.Setup(_cfg, i, false, PhaseChoThoThu(i));
        return w;
    }

    /// <summary>Lệch pha của thợ thứ i, theo công thức chốt trong CONTRACT.</summary>
    private float PhaseChoThoThu(int i)
    {
        float fps = Mathf.Max(0.01f, _cfg.hammerFps);
        return Mathf.Repeat((i * _cfg.phaseSpreadSeconds * fps) / SO_FRAME_MOT_VONG, 1f);
    }

    // ── Điều khiển mode ──────────────────────────────────────────────────────

    /// <summary>Tất cả thợ đập búa (giai đoạn Building — stage 1, 2).</summary>
    public void SetHammering()  => DoiModeCaTo(BuilderWorker.WorkerMode.Hammering);

    /// <summary>Tất cả thợ ĐỔI sheet ăn mừng rồi ĐỨNG IM (giai đoạn hộp quà).</summary>
    public void SetIdleAtGift() => DoiModeCaTo(BuilderWorker.WorkerMode.IdleAtGift);

    /// <summary>Tất cả thợ nhảy ăn mừng (suốt 3.5s pháo hoa).</summary>
    public void SetCelebrating() => DoiModeCaTo(BuilderWorker.WorkerMode.Celebrating);

    private void DoiModeCaTo(BuilderWorker.WorkerMode mode)
    {
        if (_dismissed) return;

        for (int i = 0; i < _workers.Count; i++)
        {
            BuilderWorker w = _workers[i];
            if (w != null) w.SetMode(mode);
        }
    }

    /// <summary>
    /// Cho cả tổ mờ dần rồi biến mất, sau đó tự Destroy GameObject "BuilderCrew".
    /// Gọi nhiều lần cũng chỉ chạy một lần.
    /// </summary>
    public void DismissWithFade()
    {
        if (_dismissed) return;
        _dismissed = true;

        float fadeOut = _cfg != null ? _cfg.fadeOutSeconds : 0.35f;

        for (int i = 0; i < _workers.Count; i++)
        {
            BuilderWorker w = _workers[i];
            if (w != null) w.FadeOutAndDestroy();
        }

        _workers.Clear();
        Destroy(gameObject, fadeOut + 0.1f);
    }

    /// <summary>
    /// Xếp lại thợ theo bounds mới. Cần gọi khi công trình ĐỔI SPRITE theo stage
    /// (nhà village to dần) — nếu không thợ sẽ đứng lơ lửng giữa tường.
    /// </summary>
    public void RefreshLayout(Bounds hostBounds)
    {
        if (_dismissed) return;
        _bounds = hostBounds;
        ApDungViTri();
    }

    /// <summary>
    /// Chỉ xếp lại thợ khi bounds ĐÃ ĐỔI ĐÁNG KỂ (&gt; <see cref="BOUNDS_CHANGE_THRESHOLD"/>).
    /// Đây là hàm mà cả 2 đường điều phối đều dùng, nên decor và nhà village hành xử giống nhau.
    /// </summary>
    /// <returns>true nếu đã thực sự xếp lại.</returns>
    public bool RefreshLayoutIfChanged(Bounds hostBounds)
    {
        if (_dismissed) return false;
        if (!BoundsChangedSignificantly(_bounds, hostBounds)) return false;

        RefreshLayout(hostBounds);
        return true;
    }

    /// <summary>
    /// Lệch &gt; 10% kích thước, hoặc tâm dịch &gt; 10% cạnh dài nhất ⇒ coi là đáng kể.
    /// So theo TỈ LỆ chứ không theo unit tuyệt đối vì map này 1 ô = 100 unit, công trình
    /// từ 100 tới 800 unit — một ngưỡng tuyệt đối sẽ quá nhạy với decor to và quá tù với decor nhỏ.
    /// </summary>
    public static bool BoundsChangedSignificantly(Bounds cu, Bounds moi)
    {
        Vector3 a = cu.size;
        Vector3 c = moi.size;

        // Bounds cũ rỗng (crew vừa tạo, chưa có sprite) ⇒ luôn coi là đổi
        if (a.x <= 0.0001f || a.y <= 0.0001f) return true;

        float dx = Mathf.Abs(c.x - a.x) / Mathf.Max(1f, Mathf.Abs(a.x));
        float dy = Mathf.Abs(c.y - a.y) / Mathf.Max(1f, Mathf.Abs(a.y));
        if (dx > BOUNDS_CHANGE_THRESHOLD || dy > BOUNDS_CHANGE_THRESHOLD) return true;

        float canhDai = Mathf.Max(1f, Mathf.Max(Mathf.Abs(a.x), Mathf.Abs(a.y)));
        return Vector2.Distance(cu.center, moi.center) > canhDai * BOUNDS_CHANGE_THRESHOLD;
    }

    // ── Xếp vị trí ───────────────────────────────────────────────────────────

    private void ApDungViTri()
    {
        if (_workers.Count == 0) return;

        float padding = _cfg != null ? _cfg.placementRadiusPadding : 40f;
        Vector3[] diem = TinhViTri(_bounds, padding);

        // Layer resolve an toàn — KHÔNG hardcode (§2/§7 CONTRACT)
        string layer = TouristSortingLayers.Resolve(TouristSortingLayers.Visitor);

        for (int i = 0; i < _workers.Count; i++)
        {
            BuilderWorker w = _workers[i];
            if (w == null) continue;

            Vector3 p = diem[i % diem.Length];
            w.SetFaceLeft(p.x > _bounds.center.x + 0.001f);   // ở bên phải ⇒ nhìn sang trái
            w.PlaceAt(p, layer, i);                            // i = tie-break order
        }
    }

    /// <summary>
    /// Ba điểm đứng quanh công trình (xem sơ đồ đầu file). Hàm static + public để
    /// Editor Tool của DEV-D vẽ preview mà không cần dựng crew thật.
    /// </summary>
    public static Vector3[] TinhViTri(Bounds b, float padding)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;
        Vector3 c   = b.center;
        float z = c.z;

        return new Vector3[]
        {
            new Vector3(min.x - padding, min.y,                  z),
            new Vector3(max.x + padding, min.y,                  z),
            new Vector3(c.x,             min.y - padding * 0.5f,  z)
        };
    }

    // ── Auto-wire DecorGrowthController (chỉ NGHE, không sửa) ────────────────

    private void NoiDayVaoDecor(GameObject host)
    {
        _decor = host.GetComponent<DecorGrowthController>();
        if (_decor == null) _decor = host.GetComponentInParent<DecorGrowthController>();
        if (_decor == null) return;   // nhà village → HouseWorkerBridge lo

        // Chế độ WORKER-ONLY (chuồng/máy): DEV-A bỏ qua hộp quà, đi thẳng Building → Reveal
        _workerOnly = _decor.IsWorkerOnlyMode;

        _decor.OnStageChanged   += HandleStageChanged;
        _decor.OnGiftBoxReady   += HandleGiftBoxReady;
        _decor.OnRevealStarted  += HandleRevealStarted;
        _decor.OnRevealFinished += HandleRevealFinished;
        _decorWired = true;

        // Đồng bộ NGAY trạng thái hiện tại: crew có thể được gắn giữa ván (load save).
        // Đọc CurrentStage (int) thay vì State (enum) — xem ghi chú đầu file.
        DongBoTheoStage(_decor.CurrentStage);
    }

    private void OnDestroy()
    {
        // -= bắt buộc: DecorGrowthController sống lâu hơn crew (crew bị Destroy khi
        // DismissWithFade) → không hủy đăng ký là leak + NullReference ở lần bắn sau.
        if (_decorWired && _decor != null)
        {
            _decor.OnStageChanged   -= HandleStageChanged;
            _decor.OnGiftBoxReady   -= HandleGiftBoxReady;
            _decor.OnRevealStarted  -= HandleRevealStarted;
            _decor.OnRevealFinished -= HandleRevealFinished;
            _decorWired = false;
        }

        StopAllCoroutines();
    }

    /// <summary>
    /// Đúng theo §10: CHỈ stage 1 và 2 mới bật đập búa. Cố ý KHÔNG xử lý stage 3/4/5 ở
    /// đây — ba giai đoạn đó đã có event riêng (OnGiftBoxReady / OnRevealStarted /
    /// OnRevealFinished) và chúng bắn đúng thời điểm hơn ComputeStage().
    ///
    /// BẮT BUỘC xếp lại thợ ở đây (bug A2): crew nhận bounds đúng MỘT lần lúc được gắn,
    /// mà lúc đó decor đang ở stage 1 — "vật liệu rời", ô sprite NHỎ NHẤT trong 5 stage.
    /// Sang stage 2 công trình phình ra ⇒ 3 thợ nằm lọt trong lòng công trình.
    /// </summary>
    private void HandleStageChanged(DecorGrowthController ctrl, int stage)
    {
        XepLaiTheoBounds(ctrl);
        if (stage == 1 || stage == 2) SetHammering();
    }

    /// <summary>
    /// Hộp quà (stage 4) là sprite KHÁC HẲN 3 stage xây ⇒ phải đo lại bounds.
    /// Ở WORKER-ONLY thì event này lẽ ra không bắn (DEV-A đi thẳng BeginReveal) —
    /// vẫn chặn ở đây để nếu luồng đổi thì thợ không bị đứng im vô nghĩa.
    /// </summary>
    private void HandleGiftBoxReady(DecorGrowthController ctrl)
    {
        XepLaiTheoBounds(ctrl);

        if (_workerOnly)
        {
            SetHammering();   // chuồng/máy: không có hộp quà, cứ đập tiếp tới lúc Reveal
            return;
        }

        SetIdleAtGift();
    }

    private void HandleRevealStarted(DecorGrowthController ctrl)
    {
        XepLaiTheoBounds(ctrl);   // stage 5 (hộp bung) cũng là sprite khác
        SetCelebrating();
    }

    private void HandleRevealFinished(DecorGrowthController ctrl) => DismissWithFade();

    /// <summary>Đo lại bounds công trình và xếp lại thợ NẾU lệch quá ngưỡng.</summary>
    private void XepLaiTheoBounds(DecorGrowthController ctrl)
    {
        if (ctrl == null) return;
        RefreshLayoutIfChanged(ctrl.VisualBounds);
    }

    /// <summary>
    /// Đồng bộ mode theo CHỈ SỐ STAGE 1..5 (§5.4 + §6 CONTRACT). Chỉ dùng lúc gắn crew
    /// (bao gồm gắn giữa ván khi load save); sau đó event dẫn đường.
    /// Dùng int thay enum để không phụ thuộc DecorState nằm global hay nested.
    /// </summary>
    private void DongBoTheoStage(int stage)
    {
        switch (stage)
        {
            case 1:
            case 2:
                SetHammering();      // đang xây
                break;

            case 4:
                // WORKER-ONLY không bao giờ ra stage 4 (ComputeStage chỉ trả 1|2) — nhưng
                // nếu có thì tuyệt đối KHÔNG đứng im, vì không có hộp quà nào để chờ.
                if (_workerOnly) SetHammering();
                else             SetIdleAtGift();
                break;

            case 5:
                SetCelebrating();    // hộp đang bung + pháo hoa
                break;

            case 3:
                DismissWithFade();   // đã hoàn thiện từ trước → không cần thợ
                break;

            default:
                SetHammering();      // stage lạ → coi như đang xây, an toàn hơn là biến mất
                break;
        }
    }
}
