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

    [SerializeField] private int targetPlantCount   = 6;
    [SerializeField] private int targetHarvestCount = 6;

    // =========================================================================
    // Flower pots
    // =========================================================================
    [Header("Flower Pot Targets (chậu hoa hướng dương)")]
    [Tooltip("Để trống = đếm mọi Flower plot. Gán để chỉ đếm đúng chậu tutorial.")]
    [SerializeField] private List<PlotController> tutorialFlowerPots = new List<PlotController>();

    [SerializeField] private int targetFlowerPlantCount   = 6;
    [SerializeField] private int targetFlowerHarvestCount = 6;

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
            bool count = tutorialFlowerPots.Count == 0 || tutorialFlowerPots.Contains(plot);
            if (count) _flowerPlantedIds.Add(plot.PlotId);

            if (!_allFlowerPlantsNotified && _flowerPlantedIds.Count >= targetFlowerPlantCount)
            {
                _allFlowerPlantsNotified = true;
                TutorialManager.Instance?.NotifyAllFlowerPlotsPlanted();
            }
        }
        else
        {
            bool count = tutorialPlots.Count == 0 || tutorialPlots.Contains(plot);
            if (count) _ricePlantedIds.Add(plot.PlotId);

            if (!_allRicePlantsNotified && _ricePlantedIds.Count >= targetPlantCount)
            {
                _allRicePlantsNotified = true;
                TutorialManager.Instance?.NotifyAllPlotsPlanted();
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
            bool count = tutorialFlowerPots.Count == 0 || tutorialFlowerPots.Contains(plot);
            if (count) _flowerHarvestedIds.Add(plot.PlotId);

            if (!_allFlowerHarvestsNotified && _flowerHarvestedIds.Count >= targetFlowerHarvestCount)
            {
                _allFlowerHarvestsNotified = true;
                TutorialManager.Instance?.NotifyAllFlowerPlotsHarvested();
            }
        }
        else
        {
            bool count = tutorialPlots.Count == 0 || tutorialPlots.Contains(plot);
            if (count) _riceHarvestedIds.Add(plot.PlotId);

            if (!_allRiceHarvestsNotified && _riceHarvestedIds.Count >= targetHarvestCount)
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

    private List<PlotController> FindNormalPlotsByName()
    {
        var result = new List<PlotController>();
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        System.Array.Sort(all, (a, b) => a.PlotId.CompareTo(b.PlotId));
        foreach (var p in all)
        {
            if (p.Category == PlotCategory.Normal) result.Add(p);
            if (result.Count >= 6) break;
        }
        return result;
    }

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
