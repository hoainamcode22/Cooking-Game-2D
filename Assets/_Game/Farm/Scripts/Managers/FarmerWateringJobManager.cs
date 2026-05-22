using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager phân công nhiệm vụ tưới nước cho NPC PFB_FarmerWatering.
///
/// Cấu trúc song song với FarmerJobManager (cuốc đất) — KHÔNG sửa/đụng đến
/// FarmerJobManager hay FarmerBehavior.
///
/// Flow:
///   FarmManager.OnPlotPlantedEvent → HandlePlotPlanted → TryDispatch
///   Nếu có watering-farmer rảnh → AssignWateringTask ngay
///   Nếu không → giữ plot trong pendingJobs
///   Khi NPC xong việc → OnWateringJobComplete → TryDispatch (lần tiếp)
/// </summary>
public class FarmerWateringJobManager : MonoBehaviour
{
    public static FarmerWateringJobManager Instance { get; private set; }

    [Header("NPC tưới nước (để trống = tự tìm trong scene khi Start)")]
    [SerializeField] private List<FarmerWateringBehavior> wateringFarmers = new();

    private readonly HashSet<int>           trackedPlotIds = new();
    private readonly Queue<PlotController>  pendingJobs    = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-find nếu chưa gán tay trong Inspector
        if (wateringFarmers.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            var found = FindObjectsByType<FarmerWateringBehavior>(FindObjectsSortMode.None);
#else
            var found = FindObjectsOfType<FarmerWateringBehavior>();
#endif
            wateringFarmers.AddRange(found);
        }

        foreach (var f in wateringFarmers)
            if (f != null)
                f.OnJobComplete += OnWateringJobComplete;

        Debug.Log($"[WateringJobManager] Ready với {wateringFarmers.Count} watering farmer(s)");
    }

    private void OnEnable()  => FarmManager.OnPlotPlantedEvent += HandlePlotPlanted;
    private void OnDisable() => FarmManager.OnPlotPlantedEvent -= HandlePlotPlanted;

    private void OnDestroy()
    {
        foreach (var f in wateringFarmers)
            if (f != null)
                f.OnJobComplete -= OnWateringJobComplete;
    }

    // ── Nhận event gieo hạt ───────────────────────────────────────────────────

    private void HandlePlotPlanted(PlotController plot)
    {
        if (plot == null) return;

        if (trackedPlotIds.Contains(plot.PlotId))
        {
            Debug.Log($"[WateringJobManager] Plot {plot.PlotId} đã trong queue — bỏ qua");
            return;
        }

        if (!plot.IsGrowing)
        {
            Debug.Log($"[WateringJobManager] Plot {plot.PlotId} không Growing — bỏ qua");
            return;
        }

        trackedPlotIds.Add(plot.PlotId);
        pendingJobs.Enqueue(plot);
        Debug.Log($"[WateringJobManager] Plot {plot.PlotId} vào queue tưới | pending={pendingJobs.Count}");

        TryDispatch();
    }

    // ── Phân công NPC rảnh ────────────────────────────────────────────────────

    private void TryDispatch()
    {
        while (pendingJobs.Count > 0)
        {
            // Tìm NPC tưới rảnh đầu tiên
            FarmerWateringBehavior freeFarmer = null;
            foreach (var f in wateringFarmers)
            {
                if (f != null && f.enabled && f.gameObject.activeInHierarchy && !f.IsBusy)
                {
                    freeFarmer = f;
                    break;
                }
            }

            if (freeFarmer == null)
            {
                Debug.Log($"[WateringJobManager] Không có NPC rảnh — {pendingJobs.Count} task đang chờ");
                return;
            }

            PlotController plot = pendingJobs.Dequeue();

            // Plot không còn hợp lệ (đã harvest trong lúc chờ)
            if (plot == null || !plot.IsGrowing)
            {
                int staleId = plot != null ? plot.PlotId : -1;
                trackedPlotIds.Remove(staleId);
                Debug.Log($"[WateringJobManager] Drop stale task plot {staleId} — không còn Growing");
                continue;
            }

            Debug.Log($"[WateringJobManager] Giao tưới plot {plot.PlotId} → NPC '{freeFarmer.name}'");
            freeFarmer.AssignWateringTask(plot);
        }
    }

    // ── Callback khi NPC hoàn thành task ─────────────────────────────────────

    private void OnWateringJobComplete(FarmerWateringBehavior farmer, int completedPlotId)
    {
        trackedPlotIds.Remove(completedPlotId);
        Debug.Log($"[WateringJobManager] NPC '{farmer.name}' xong tưới plot {completedPlotId} | pending={pendingJobs.Count}");
        TryDispatch();
    }
}
