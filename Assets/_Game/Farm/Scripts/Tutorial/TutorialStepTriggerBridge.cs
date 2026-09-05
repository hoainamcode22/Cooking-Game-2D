using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridge giữa các sự kiện game (plant/harvest) và TutorialManager.
///
/// Theo dõi riêng biệt:
///   - 6 ô đất lúa (rice plots) → NotifyAllPlotsPlanted / NotifyAllPlotsHarvested
///   - N chậu hoa (flower pots) → NotifyAllFlowerPlotsPlanted / NotifyAllFlowerPlotsHarvested
///
/// Attach component này lên Tutorial_Manager hoặc Tutorial_System.
/// </summary>
public class TutorialStepTriggerBridge : MonoBehaviour
{
    // =========================================================================
    // Rice plots
    // =========================================================================
    [Header("Rice Plot Targets (6 ô đất lúa đầu game)")]
    [Tooltip("Để trống = đếm mọi Normal plot. Gán để chỉ đếm đúng 6 ô tutorial.")]
    [SerializeField] private List<PlotController> tutorialPlots = new List<PlotController>();

    // Lưu ý: KHÔNG còn field targetPlantCount/targetHarvestCount. Gate đã đổi sang
    // AllRiceFieldPlanted() / NoUnlockedPlanted() — kiểm tra "hết ô trống / hết cây"
    // thay vì đếm số, để không kẹt khi số ô unlock lệch với con số cấu hình.

    // =========================================================================
    // Flower pots
    // =========================================================================
    [Header("Flower Pot Targets (chậu hoa hướng dương)")]
    [Tooltip("Để trống = đếm mọi Flower plot. Gán để chỉ đếm đúng chậu tutorial.")]
    [SerializeField] private List<PlotController> tutorialFlowerPots = new List<PlotController>();

    // Tương tự chậu hoa: gate dùng AllUnlockedNonEmpty(Flower) / NoUnlockedPlanted(Flower).

    // =========================================================================
    // Runtime counters
    // =========================================================================
    private readonly HashSet<int> _ricePlantedIds    = new HashSet<int>();
    private readonly HashSet<int> _riceHarvestedIds  = new HashSet<int>();
    private readonly HashSet<int> _flowerPlantedIds  = new HashSet<int>();
    private readonly HashSet<int> _flowerHarvestedIds= new HashSet<int>();

    private bool _allRicePlantsNotified;
    private bool _allRiceHarvestsNotified;
    private bool _allFlowerPlantsNotified;
    private bool _allFlowerHarvestsNotified;

    // =========================================================================
    // Unity Lifecycle
    // =========================================================================
    private void OnEnable()
    {
        FarmManager.OnPlotPlantedEvent   += HandlePlotPlanted;
        FarmManager.OnPlotHarvestedEvent += HandlePlotHarvested;
    }

    private void OnDisable()
    {
        FarmManager.OnPlotPlantedEvent   -= HandlePlotPlanted;
        FarmManager.OnPlotHarvestedEvent -= HandlePlotHarvested;
    }

    /// <summary>Reset đếm trồng/thu hoạch ô đất — để TÁI SỬ DỤNG 8 ô cho cây mới (vd trồng Ngô ở L2).</summary>
    public void ResetRiceTracking()
    {
        _ricePlantedIds.Clear();
        _riceHarvestedIds.Clear();
        _allRicePlantsNotified   = false;
        _allRiceHarvestsNotified = false;
    }

    // =========================================================================
    // [WP-A1] NGUỒN DUY NHẤT về "ô nào thuộc tutorial"
    // Gate (AllRiceFieldPlanted / NoUnlockedPlanted…) và BÀN TAY (TutorialRuntimeTargetResolver)
    // trước đây mỗi bên tự lọc một kiểu ⇒ tay chỉ vào ô mà gate không đếm (hoặc ngược lại) ⇒ kẹt.
    // Nay cả hai cùng gọi LayODatLua()/LayChauHoa() ⇒ tập ô của tay == tập ô của gate.
    // =========================================================================

    /// <summary>Tên ô có dấu hiệu là chậu hoa (Chauhoa/Pot/Hoa) — bị loại khỏi ruộng lúa.</summary>
    private static bool LaChauHoaTheoTen(PlotController p)
    {
        string n = p.name.ToLower();
        return n.Contains("chau") || n.Contains("pot") || n.Contains("hoa");
    }

    /// <summary>
    /// Ô RUỘNG lúa hợp lệ cho tutorial: Category Normal + IsUnlocked + KHÔNG phải chậu hoa theo tên.
    /// Xếp theo PlotId tăng dần. Không giới hạn số lượng (trước đây FindNormalPlotsByName cắt ở 6).
    /// </summary>
    public static List<PlotController> LayODatLua()
    {
        var result = new List<PlotController>();
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        System.Array.Sort(all, (a, b) => a.PlotId.CompareTo(b.PlotId));
        foreach (var p in all)
        {
            if (p == null || p.Category != PlotCategory.Normal || !p.IsUnlocked) continue;
            if (LaChauHoaTheoTen(p)) continue;
            result.Add(p);
        }
        return result;
    }

    /// <summary>Chậu hoa hợp lệ cho tutorial: Category Flower + IsUnlocked. Xếp theo PlotId tăng dần.</summary>
    public static List<PlotController> LayChauHoa()
    {
        var result = new List<PlotController>();
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        System.Array.Sort(all, (a, b) => a.PlotId.CompareTo(b.PlotId));
        foreach (var p in all)
        {
            if (p == null || p.Category != PlotCategory.Flower || !p.IsUnlocked) continue;
            result.Add(p);
        }
        return result;
    }

    /// <summary>Tập ô mà gate đếm cho từng loại: Normal → LayODatLua(), Flower → LayChauHoa().</summary>
    private static List<PlotController> LayODatTheoLoai(PlotCategory cat)
        => cat == PlotCategory.Flower ? LayChauHoa() : LayODatLua();

    /// <summary>
    /// [WP-A1] Reset MỌI latch một-lần (lúa/hoa, trồng/thu hoạch) + bộ đếm.
    /// TutorialManager gọi khi VÀO một bước chờ quét ô, để gate của bước đó có thể bắn lại.
    /// </summary>
    public void ResetAllTracking() => ResetCounters();

    /// <summary>
    /// [WP-A1] Kiểm tra lại gate NGAY lúc Manager vào bước chờ — chữa lỗi "tay kẹt":
    /// người chơi trồng/thu hoạch xong TRƯỚC khi bước chờ bắt đầu ⇒ event đã bắn (và bị Manager bỏ),
    /// latch đã set ⇒ không bao giờ bắn lại. Nếu điều kiện đã đạt: set latch + gọi Notify tương ứng.
    /// Trả về true nếu đã bắn Notify. Action không thuộc 4 gate quét ô → false.
    /// </summary>
    public bool KiemTraLaiGate(TutorialWaitAction a)
    {
        var tm = TutorialManager.Instance;
        bool dat;
        switch (a)
        {
            case TutorialWaitAction.WaitForAllPlotsPlanted:
                dat = !_allRicePlantsNotified && AllRiceFieldPlanted();
                if (dat) { _allRicePlantsNotified = true; tm?.NotifyAllPlotsPlanted(); }
                break;
            case TutorialWaitAction.WaitForAllPlotsHarvested:
                dat = !_allRiceHarvestsNotified && NoUnlockedPlanted(PlotCategory.Normal);
                if (dat) { _allRiceHarvestsNotified = true; tm?.NotifyAllPlotsHarvested(); }
                break;
            case TutorialWaitAction.WaitForAllFlowerPlotsPlanted:
                dat = !_allFlowerPlantsNotified && AllUnlockedNonEmpty(PlotCategory.Flower);
                if (dat) { _allFlowerPlantsNotified = true; tm?.NotifyAllFlowerPlotsPlanted(); }
                break;
            case TutorialWaitAction.WaitForAllFlowerPlotsHarvested:
                dat = !_allFlowerHarvestsNotified && NoUnlockedPlanted(PlotCategory.Flower);
                if (dat) { _allFlowerHarvestsNotified = true; tm?.NotifyAllFlowerPlotsHarvested(); }
                break;
            default:
                return false;
        }
        Debug.Log($"[Tutorial][Gate] Kiểm tra lại '{a}' lúc vào bước → " +
                  (dat ? "ĐÃ ĐẠT — bắn Notify ngay." : "chưa đạt — chờ event trồng/thu hoạch."));
        return dat;
    }

    // Lúa coi là trồng xong khi mọi Ô RUỘNG (Normal, KHÔNG phải chậu hoa) đã có cây.
    // Loại trừ chậu hoa (Chauhoa) — trước đây gate tính cả chậu trống nên kẹt mãi.
    // [WP-A1] Dùng LayODatLua() — cùng tập ô với bàn tay.
    private static bool AllRiceFieldPlanted()
    {
        int total = 0;
        var empties = new System.Text.StringBuilder();
        foreach (var p in LayODatLua())
        {
            total++;
            if (p.IsEmpty) empties.Append(p.name).Append(' ');
        }
        if (empties.Length > 0)
        {
            Debug.Log($"[Tutorial] Lúa: còn ô RUỘNG trống → {empties}(chưa qua bước)");
            return false;
        }
        Debug.Log($"[Tutorial] Lúa: ĐỦ {total} ô ruộng đã trồng → QUA bước!");
        return total > 0;
    }

    // "Đã TRỒNG hết" = KHÔNG còn ô trống nào (unlocked). Chắc hơn đếm số → tránh kẹt vì lệch số ô.
    // [WP-A1] Dùng LayODatTheoLoai(cat) — cùng tập ô với bàn tay.
    private static bool AllUnlockedNonEmpty(PlotCategory cat)
    {
        bool any = false;
        foreach (var p in LayODatTheoLoai(cat))
        {
            any = true;
            if (p.IsEmpty) return false;   // còn ô trống → chưa trồng xong
        }
        return any;
    }

    // "Đã THU HOẠCH hết" = KHÔNG còn ô nào đang có cây (unlocked) → tất cả đã về Empty.
    // [WP-A1] Dùng LayODatTheoLoai(cat) — cùng tập ô với bàn tay.
    private static bool NoUnlockedPlanted(PlotCategory cat)
    {
        bool any = false;
        foreach (var p in LayODatTheoLoai(cat))
        {
            any = true;
            if (p.IsPlanted) return false;  // còn cây → chưa thu hoạch xong
        }
        return any;
    }

    // =========================================================================
    // Event Handlers
    // =========================================================================
    private void HandlePlotPlanted(PlotController plot)
    {
        if (plot == null) return;

        // Notify single plant (WaitForPlant — dùng cho cả lúa lẫn hoa, step đầu tiên)
        TutorialManager.Instance?.NotifyPlant();

        if (plot.Category == PlotCategory.Flower)
        {
            _flowerPlantedIds.Add(plot.PlotId);

            if (!_allFlowerPlantsNotified && AllUnlockedNonEmpty(PlotCategory.Flower))
            {
                _allFlowerPlantsNotified = true;
                TutorialManager.Instance?.NotifyAllFlowerPlotsPlanted();
            }
        }
        else
        {
            _ricePlantedIds.Add(plot.PlotId);

            if (!_allRicePlantsNotified && AllRiceFieldPlanted())
            {
                _allRicePlantsNotified = true;
                TutorialManager.Instance?.NotifyAllPlotsPlanted();
                Debug.Log("[Tutorial] >>> NotifyAllPlotsPlanted FIRED (qua L1L2_06)");
            }
        }
    }

    private void HandlePlotHarvested(PlotController plot)
    {
        if (plot == null) return;

        // Notify single harvest (WaitForHarvest)
        TutorialManager.Instance?.NotifyHarvest();

        if (plot.Category == PlotCategory.Flower)
        {
            _flowerHarvestedIds.Add(plot.PlotId);

            if (!_allFlowerHarvestsNotified && NoUnlockedPlanted(PlotCategory.Flower))
            {
                _allFlowerHarvestsNotified = true;
                TutorialManager.Instance?.NotifyAllFlowerPlotsHarvested();
            }
        }
        else
        {
            _riceHarvestedIds.Add(plot.PlotId);

            if (!_allRiceHarvestsNotified && NoUnlockedPlanted(PlotCategory.Normal))
            {
                _allRiceHarvestsNotified = true;
                TutorialManager.Instance?.NotifyAllPlotsHarvested();
            }
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Trả về tọa độ world trung tâm của 6 ô lúa (cho TutorialCameraFocus).</summary>
    public Vector3 GetRicePlotsWorldCenter()
    {
        var list = tutorialPlots.Count > 0 ? tutorialPlots : FindNormalPlotsByName();
        if (list == null || list.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var p in list)
        {
            if (p != null) { sum += p.transform.position; count++; }
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    /// <summary>Trả về Transform của ô lúa đầu tiên (cho TutorialRuntimeTargetResolver).</summary>
    public Transform GetFirstRicePlotTransform()
    {
        foreach (var p in tutorialPlots) { if (p != null) return p.transform; }
        var found = FindNormalPlotsByName();
        return found.Count > 0 ? found[0].transform : null;
    }

    /// <summary>Danh sách Transform của các ô lúa tutorial, sắp theo PlotId (cho proxy plot_01..06 + mask).</summary>
    public List<Transform> GetRicePlotTransforms()
    {
        var src = (tutorialPlots != null && tutorialPlots.Count > 0)
            ? new List<PlotController>(tutorialPlots)
            : FindNormalPlotsByName();

        if (src != null)
            src.Sort((a, b) => (a == null ? 0 : a.PlotId).CompareTo(b == null ? 0 : b.PlotId));

        var result = new List<Transform>();
        if (src != null)
            foreach (var p in src)
                if (p != null) result.Add(p.transform);
        return result;
    }

    /// <summary>Trả về tọa độ world trung tâm của 2 chậu hoa (cho TutorialCameraFocus).</summary>
    public Vector3 GetFlowerPotsWorldCenter()
    {
        var list = tutorialFlowerPots.Count > 0 ? tutorialFlowerPots : null;
        if (list == null || list.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var p in list)
        {
            if (p != null) { sum += p.transform.position; count++; }
        }
        return count > 0 ? sum / count : Vector3.zero;
    }

    /// <summary>Trả về Transform của chậu hoa đầu tiên (cho TutorialRuntimeTargetResolver).</summary>
    public Transform GetFirstFlowerPotTransform()
    {
        foreach (var p in tutorialFlowerPots) { if (p != null) return p.transform; }
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p.Category == PlotCategory.Flower) return p.transform;
        return null;
    }

    // [WP-A1] Không còn cắt ở 6 ô — trả đúng tập ô ruộng mà gate đếm (LayODatLua).
    private List<PlotController> FindNormalPlotsByName() => LayODatLua();

    public void ResetCounters()
    {
        _ricePlantedIds.Clear();
        _riceHarvestedIds.Clear();
        _flowerPlantedIds.Clear();
        _flowerHarvestedIds.Clear();
        _allRicePlantsNotified   = false;
        _allRiceHarvestsNotified = false;
        _allFlowerPlantsNotified   = false;
        _allFlowerHarvestsNotified = false;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Simulate All Rice Planted")]
    private void DbgAllRicePlanted()
    {
        _allRicePlantsNotified = true;
        TutorialManager.Instance?.NotifyAllPlotsPlanted();
    }

    [ContextMenu("Debug: Simulate All Rice Harvested")]
    private void DbgAllRiceHarvested()
    {
        _allRiceHarvestsNotified = true;
        TutorialManager.Instance?.NotifyAllPlotsHarvested();
    }

    [ContextMenu("Debug: Simulate All Flowers Planted")]
    private void DbgAllFlowersPlanted()
    {
        _allFlowerPlantsNotified = true;
        TutorialManager.Instance?.NotifyAllFlowerPlotsPlanted();
    }

    [ContextMenu("Debug: Simulate All Flowers Harvested")]
    private void DbgAllFlowersHarvested()
    {
        _allFlowerHarvestsNotified = true;
        TutorialManager.Instance?.NotifyAllFlowerPlotsHarvested();
    }
#endif
}
