using System.Collections;
using UnityEngine;

/// <summary>
/// Hay Day style opening — "thu hoạch trước khi trồng".
///
/// Lần chơi ĐẦU TIÊN (save mới): ép ô lúa đầu tiên của tutorial vào trạng thái
/// chín sẵn (Ready + rice) để player được thu hoạch ngay ở bước mở đầu
/// (step L1L2_04b_FirstHarvest), trước khi tự tay gieo hạt.
///
/// An toàn:
///   - Chỉ chạy 1 lần — PlayerPrefs flag (giống pattern StarterInventorySetup).
///   - Chỉ chạy trên save mới hoàn toàn (chưa có STARTER_ITEMS_GIVEN lúc Awake).
///   - Dùng đúng API chuẩn của PlotController: TryPlant() → CompleteInstantly()
///     (cùng đường Save/RefreshVisual như gameplay thật, không hack state).
///   - Sau khi player thu hoạch ô chín sẵn → reset bộ đếm của
///     TutorialStepTriggerBridge để intro-harvest KHÔNG tính vào
///     WaitForAllPlotsHarvested (6 ô phải đếm từ 0).
///   - Không thoả điều kiện → bỏ qua êm, không phá tutorial.
///
/// Gắn cùng GameObject với TutorialManager + TutorialStepTriggerBridge
/// (SetupTutorialL1L2Tool tự thêm).
/// </summary>
public class TutorialPrePlant : MonoBehaviour
{
    private const string PREF_KEY         = "TUTORIAL_PREPLANT_DONE";
    private const string STARTER_PREF_KEY = "STARTER_ITEMS_GIVEN"; // của StarterInventorySetup

    [Tooltip("cropId ứng viên cho lúa — thử lần lượt qua FarmManager.GetCropById")]
    [SerializeField] private string[] _riceCropIdCandidates = { "rice", "lua", "hat_lua", "seed_rice" };

    [Tooltip("Thời gian tối đa chờ plot/manager sẵn sàng (giây)")]
    [SerializeField] private float _setupTimeout = 4f;

    [Header("Debug")]
    [SerializeField] private bool _forceResetFlagOnPlay = false;

    private PlotController _prePlantedPlot;
    private bool           _resetPending;
    private bool           _isFreshSave;

    private void Awake()
    {
        // Đọc TRƯỚC khi StarterInventorySetup.Start set flag — tránh phụ thuộc thứ tự Start.
        _isFreshSave = !PlayerPrefs.HasKey(STARTER_PREF_KEY);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (_forceResetFlagOnPlay)
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            _isFreshSave = true;
            Debug.Log("[TutorialPrePlant] DEBUG: reset flag");
        }
#endif
        if (PlayerPrefs.HasKey(PREF_KEY)) return;

        if (!_isFreshSave)
        {
            // Save cũ (đã từng nhận starter items) — không bao giờ pre-plant nữa.
            PlayerPrefs.SetInt(PREF_KEY, 1);
            LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
            return;
        }

        // Chỉ chạy khi tutorial thật sự sẽ chạy (cùng GameObject với TutorialManager).
        if (GetComponent<TutorialManager>() == null) return;

        StartCoroutine(SetupRoutine());
    }

    private void OnDestroy()
    {
        FarmManager.OnPlotHarvestedEvent -= HandlePlotHarvested;
    }

    // =========================================================================
    // Pre-plant
    // =========================================================================
    private IEnumerator SetupRoutine()
    {
        // Đợi 1 frame cho mọi Awake/Start (PlotController.Load, FarmManager unlock) chạy xong.
        yield return null;

        float          deadline = Time.time + _setupTimeout;
        PlotController plot     = null;
        CropData       rice     = null;

        while (Time.time < deadline)
        {
            if (plot == null) plot = FindFirstTutorialRicePlot();
            if (rice == null) rice = ResolveRiceCrop();

            if (plot != null && rice != null && plot.IsEmpty)
                break;

            yield return null;
        }

        if (plot == null || rice == null || !plot.IsEmpty)
        {
            Debug.LogWarning("[TutorialPrePlant] Bo qua pre-plant — dieu kien chua du: " +
                $"plot={(plot != null ? plot.name : "null")}, " +
                $"rice={(rice != null ? rice.cropId : "null")}, " +
                $"plotEmpty={(plot != null && plot.IsEmpty)}");
            StartCoroutine(SkipFirstHarvestStepWhenReached());
            yield break;
        }

        // Empty → Growing → Ready, dùng đúng API + save path chuẩn của PlotController.
        if (!plot.TryPlant(rice))
        {
            Debug.LogWarning($"[TutorialPrePlant] TryPlant that bai tren {plot.name} — bo qua.");
            StartCoroutine(SkipFirstHarvestStepWhenReached());
            yield break;
        }
        plot.CompleteInstantly();

        _prePlantedPlot = plot;
        // Bridge subscribe trong OnEnable (sớm hơn) → handler của bridge chạy trước handler này.
        FarmManager.OnPlotHarvestedEvent += HandlePlotHarvested;

        PlayerPrefs.SetInt(PREF_KEY, 1);
        LuuGopPrefs.Hen();     // gộp lưu, xem LuuGopPrefs
        Debug.Log($"[TutorialPrePlant] '{plot.name}' da chin san lua (Hay Day opening). Flag set.");
    }

    /// <summary>
    /// FAILSAFE chống kẹt: pre-plant không thành công (save cũ / plot bận / thiếu data)
    /// → khi tutorial tới step "L1L2_04b_FirstHarvest" (WaitForHarvest cho ô chín sẵn),
    /// tự bắn NotifyHarvest lặp lại cho tới khi step được bỏ qua. Theo dõi tối đa 10 phút.
    /// </summary>
    private IEnumerator SkipFirstHarvestStepWhenReached()
    {
        const string STEP_NAME = "L1L2_04b_FirstHarvest";
        float deadline = Time.time + 600f;
        var   wait     = new WaitForSeconds(0.3f);
        bool  fired    = false;

        while (Time.time < deadline)
        {
            var mgr = TutorialManager.Instance;
            if (mgr == null) yield break;

            bool onStep = mgr.CurrentStepName == STEP_NAME;
            if (onStep)
            {
                mgr.NotifyHarvest(); // bắn lặp lại — typing/transition đều không sót
                fired = true;
            }
            else if (fired)
            {
                Debug.Log("[TutorialPrePlant] Failsafe: da bo qua step 04b (khong co o chin san).");
                yield break;
            }
            yield return wait;
        }
    }

    private PlotController FindFirstTutorialRicePlot()
    {
        var bridge = GetComponent<TutorialStepTriggerBridge>();
        Transform t = bridge != null ? bridge.GetFirstRicePlotTransform() : null;
        return t != null ? t.GetComponent<PlotController>() : null;
    }

    private CropData ResolveRiceCrop()
    {
        if (FarmManager.Instance == null) return null;

        foreach (var id in _riceCropIdCandidates)
        {
            if (string.IsNullOrEmpty(id)) continue;
            CropData crop = FarmManager.Instance.GetCropById(id);
            if (crop != null) return crop;
        }
        return null;
    }

    // =========================================================================
    // Counter reset sau intro-harvest
    // =========================================================================
    private void HandlePlotHarvested(PlotController plot)
    {
        if (_resetPending || plot == null || plot != _prePlantedPlot) return;
        _resetPending = true;
        // Defer 1 frame: chắc chắn TutorialStepTriggerBridge đã xử lý xong event này
        // (NotifyHarvest cho step 04b) rồi mới xoá id khỏi bộ đếm.
        StartCoroutine(ResetBridgeCountersNextFrame());
    }

    private IEnumerator ResetBridgeCountersNextFrame()
    {
        yield return null;

        FarmManager.OnPlotHarvestedEvent -= HandlePlotHarvested;

        var bridge = GetComponent<TutorialStepTriggerBridge>();
        if (bridge != null)
        {
            bridge.ResetCounters();
            Debug.Log("[TutorialPrePlant] Intro-harvest xong — da reset bridge counters (6 o lua dem tu 0).");
        }
        _prePlantedPlot = null;
    }
}
