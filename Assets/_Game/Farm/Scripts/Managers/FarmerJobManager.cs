using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager trung tâm phân công job cuốc đất cho các farmer NPC.
///
/// Là nơi DUY NHẤT subscribe FarmManager.OnPlotPlantedEvent.
/// FarmerBehavior KHÔNG subscribe event này nữa.
///
/// Flow:
///   OnPlotPlantedEvent → HandlePlotPlanted → TryDispatch
///   Nếu có farmer rảnh → AssignPlotJob ngay
///   Nếu không → giữ plot trong pendingJobs
///   Khi farmer xong việc → OnFarmerJobComplete → TryDispatch (lần tiếp)
/// </summary>
public class FarmerJobManager : MonoBehaviour
{
    public static FarmerJobManager Instance { get; private set; }

    [Header("Farmers (để trống = tự tìm trong scene khi Start)")]
    [SerializeField] private List<FarmerBehavior> farmers = new();

    // Plot đã vào queue hoặc đang được xử lý — tránh assign cùng 1 plot 2 lần
    private readonly HashSet<int> trackedPlotIds = new();
    private readonly Queue<PlotController> pendingJobs = new();

    // ──────────────────────────────────────────────────────────────────────────

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
        // Auto-find farmers nếu chưa gán tay
        if (farmers.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            var found = FindObjectsByType<FarmerBehavior>(FindObjectsSortMode.None);
#else
            var found = FindObjectsOfType<FarmerBehavior>();
#endif
            farmers.AddRange(found);
        }

        // Đăng ký callback OnJobComplete cho từng farmer
        foreach (FarmerBehavior f in farmers)
        {
            if (f != null)
                f.OnJobComplete += OnFarmerJobComplete;
        }

        Debug.Log($"[JobManager] Ready with {farmers.Count} farmer(s)");
    }

    private void OnEnable()
    {
        FarmManager.OnPlotPlantedEvent += HandlePlotPlanted;
    }

    private void OnDisable()
    {
        FarmManager.OnPlotPlantedEvent -= HandlePlotPlanted;
    }

    private void OnDestroy()
    {
        foreach (FarmerBehavior f in farmers)
        {
            if (f != null)
                f.OnJobComplete -= OnFarmerJobComplete;
        }
    }

    // ── Nhận plot mới từ FarmManager event ────────────────────────────────────

    private void HandlePlotPlanted(PlotController plot)
    {
        if (plot == null)
            return;

        // Tránh enqueue 2 lần cùng 1 plot trong cùng 1 lần gieo
        if (trackedPlotIds.Contains(plot.PlotId))
        {
            Debug.Log($"[JobManager] Ignore plot {plot.PlotId} — already tracked");
            return;
        }

        if (!plot.IsGrowing)
        {
            Debug.Log($"[JobManager] Ignore plot {plot.PlotId} — not Growing");
            return;
        }

        trackedPlotIds.Add(plot.PlotId);
        pendingJobs.Enqueue(plot);
        Debug.Log($"[JobManager] Plot {plot.PlotId} added to queue | pending={pendingJobs.Count}");

        TryDispatch();
    }

    // ── Tìm farmer rảnh và giao job ──────────────────────────────────────────

    private void TryDispatch()
    {
        while (pendingJobs.Count > 0)
        {
            // Tìm farmer rảnh đầu tiên
            FarmerBehavior freeFarmer = null;
            foreach (FarmerBehavior f in farmers)
            {
                if (f != null && !f.IsBusy)
                {
                    freeFarmer = f;
                    break;
                }
            }

            if (freeFarmer == null)
            {
                Debug.Log($"[JobManager] No free farmer — {pendingJobs.Count} job(s) waiting");
                return;
            }

            PlotController plot = pendingJobs.Dequeue();

            // Plot không còn hợp lệ (bị harvest trong lúc chờ)
            if (plot == null || !plot.IsGrowing)
            {
                int staleId = plot != null ? plot.PlotId : -1;
                trackedPlotIds.Remove(staleId);
                Debug.Log($"[JobManager] Drop stale job plot {staleId} — no longer Growing");
                continue;
            }

            Debug.Log($"[JobManager] Assign plot {plot.PlotId} → farmer '{freeFarmer.name}'");
            freeFarmer.AssignPlotJob(plot);
        }
    }

    // ── Callback khi farmer hoàn thành job ───────────────────────────────────

    private void OnFarmerJobComplete(FarmerBehavior farmer, int completedPlotId)
    {
        // Xóa track để plot đó có thể được nhận lại nếu người chơi gieo lại
        trackedPlotIds.Remove(completedPlotId);
        Debug.Log($"[JobManager] Farmer '{farmer.name}' done plot {completedPlotId} — try dispatch next | pending={pendingJobs.Count}");
        TryDispatch();
    }
}
