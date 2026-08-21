using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ═══════════════════════════════════════════════════════════════════════════
//  M0-2 — SaveBootstrap: object tự dựng (KHÔNG cần kéo thả vào scene nào),
//  DontDestroyOnLoad, chịu trách nhiệm:
//    • nạp save.json lúc vào game, mồi cho SaveAdapters (PrimeFromDisk),
//    • phục hồi chuyến TÀU khi TrainManager (per-scene) init xong,
//    • auto-save: debounce khi state đổi (nghe event các manager) + định kỳ
//      + OnApplicationPause/Focus/Quit,
//    • (tuỳ chọn, mặc định TẮT) phục hồi khoá PlayerPrefs thiếu từ save.json.
//
//  Thoát Play Mode trong Editor do SaveDebugTool (Editor/) hook — xem file đó.
// ═══════════════════════════════════════════════════════════════════════════
public class SaveBootstrap : MonoBehaviour
{
    /// <summary>Giây gom các thay đổi liên tiếp thành một lần ghi file.</summary>
    private const float DebounceSeconds = 5f;

    /// <summary>Lưới an toàn cho hệ không có event (ô đất tự Save nội bộ, chuồng, tàu).</summary>
    private const float PeriodicSeconds = 60f;

    /// <summary>Chờ TrainManager init tối đa (tàu chạy từ điểm ẩn về ga mất vài giây).</summary>
    private const float TrainWaitTimeoutSeconds = 30f;

    /// <summary>
    /// TỰ phục hồi khoá PlayerPrefs bị thiếu từ save.json ngay lúc vào game.
    /// MẶC ĐỊNH FALSE — bật lên là tool "CHƠI LẠI TỪ ĐẦU" (PlayerPrefs.DeleteAll)
    /// sẽ bị save.json bơm ngược dữ liệu cũ. Chỉ bật khi flow reset đã xoá cả
    /// save.json (SaveSystem.DeleteSave). Xem SAVE_DESIGN.md §3.3.
    /// </summary>
    public const bool AutoRestoreMissingPrefs = false;

    private static SaveBootstrap _instance;

    private static bool  _dirty;
    private static float _saveDueAt;      // mốc hẹn của LẦN ĐÁNH DẤU ĐẦU (không dời — tránh debounce vô hạn)

    private float _nextPeriodicAt;
    private Object _trainInstanceRestored;   // TrainManager nào đã được áp snapshot rồi

    // ═════════════════════════════════════════════════════════════════════
    //  KHỞI ĐỘNG
    // ═════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    // "Enter Play Mode Options" không reload domain → static giữ giá trị phiên trước.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _dirty = false;
        _saveDueAt = 0f;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (_instance != null) return;

        var go = new GameObject("SaveSystem(Auto)");
        _instance = go.AddComponent<SaveBootstrap>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        // Nạp save.json một lần cho cả phiên: mồi mirror/snapshot cũ cho SaveAdapters
        // để lần capture đầu (có thể xảy ra ở scene bếp) không làm rớt dữ liệu hệ vắng mặt.
        FarmSaveData fromDisk = SaveSystem.Load();
        SaveAdapters.PrimeFromDisk(fromDisk);

        if (fromDisk != null)
            Debug.Log($"[Save] Đã nạp save.json (v{fromDisk.saveVersion}, lưu lúc {fromDisk.savedAtUtc} UTC).");
        else
            Debug.Log("[Save] Chưa có save.json — sẽ tạo ở lần lưu đầu tiên.");

#pragma warning disable 162, 429 // nhánh hằng const — cố ý, xem chú thích AutoRestoreMissingPrefs
        if (AutoRestoreMissingPrefs && fromDisk != null)
            SaveSystem.RestoreMissingPrefs(fromDisk);
#pragma warning restore 162, 429

        _nextPeriodicAt = Time.realtimeSinceStartup + PeriodicSeconds;

        // Có chuyến tàu chờ phục hồi → khoá chụp tàu sống cho tới khi áp xong
        // (TryRestoreTrainWhenReady sẽ mở lại ở mọi lối ra).
        if (CoSnapshotTauCanAp())
            SaveAdapters.TrainAdapter.LiveCaptureEnabled = false;

        // Event static — hook một lần là đủ (pattern -= rồi += để an toàn khi re-Awake).
        MissionProgressTracker.OnProgressChanged -= HandleMissionProgress;
        MissionProgressTracker.OnProgressChanged += HandleMissionProgress;
        FarmManager.OnPlotPlantedEvent   -= HandlePlotEvent;
        FarmManager.OnPlotPlantedEvent   += HandlePlotEvent;
        FarmManager.OnPlotHarvestedEvent -= HandlePlotEvent;
        FarmManager.OnPlotHarvestedEvent += HandlePlotEvent;

        SceneManager.sceneLoaded += HandleSceneLoaded;

        StartCoroutine(HookSceneManagers());
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        _instance = null;

        MissionProgressTracker.OnProgressChanged -= HandleMissionProgress;
        FarmManager.OnPlotPlantedEvent   -= HandlePlotEvent;
        FarmManager.OnPlotHarvestedEvent -= HandlePlotEvent;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Manager per-scene (Warehouse, FarmManager, Train…) vừa được dựng lại → hook lại
        // + thử áp snapshot tàu cho instance mới. Trong lúc chờ áp, cấm chụp tàu "sống"
        // để auto-save không ghi đè chuyến thật bằng chuyến trắng vừa init.
        _trainInstanceRestored = null;
        if (CoSnapshotTauCanAp())
            SaveAdapters.TrainAdapter.LiveCaptureEnabled = false;

        StartCoroutine(HookSceneManagers());
    }

    private static bool CoSnapshotTauCanAp()
    {
        FarmSaveData known = SaveAdapters.LastKnown;
        return known != null && known.train != null && known.train.restorable;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HOOK EVENT các manager (instance event — phải đợi Instance sẵn sàng)
    // ═════════════════════════════════════════════════════════════════════

    private IEnumerator HookSceneManagers()
    {
        // Đợi vài frame cho Awake/Start của các manager trong scene chạy xong.
        yield return null;
        yield return null;

        // DontDestroyOnLoad managers — hook lặp lại vô hại nhờ -=/+=.
        if (FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
            FarmEconomyManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
        }
        if (PlayerProgressManager.Instance != null)
        {
            PlayerProgressManager.Instance.OnExpChanged   -= HandleExpChanged;
            PlayerProgressManager.Instance.OnExpChanged   += HandleExpChanged;
            PlayerProgressManager.Instance.OnLevelChanged -= HandleLevelChanged;
            PlayerProgressManager.Instance.OnLevelChanged += HandleLevelChanged;
        }
        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.OnInventoryChanged -= MarkDirty;
            FarmInventoryManager.Instance.OnInventoryChanged += MarkDirty;
        }

        // Per-scene managers.
        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.OnWarehouseChanged -= MarkDirty;
            WarehouseManager.Instance.OnWarehouseChanged += MarkDirty;
        }

        // TÀU: đợi init xong (InitAfterFrame cho tàu chạy về ga rồi mới WaitingForLoad).
        yield return StartCoroutine(TryRestoreTrainWhenReady());
    }

    private IEnumerator TryRestoreTrainWhenReady()
    {
        // Chốt dữ liệu NGAY LÚC BẮT ĐẦU: LastKnown có thể bị CaptureAll thay mới giữa chừng.
        FarmSaveData known = SaveAdapters.LastKnown;

        if (known == null || known.train == null || !known.train.restorable)
        {
            // Không có gì để áp (hoặc chưa duyệt patch TrainManager) → chụp sống thoải mái.
            SaveAdapters.TrainAdapter.LiveCaptureEnabled = true;
            yield break;
        }

        float start    = Time.realtimeSinceStartup;
        float deadline = start + TrainWaitTimeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            var tm = TrainManager.Instance;

            if (tm == null)
            {
                // 3 giây mà vẫn không có TrainManager = scene này không có tàu (bếp).
                // Thoát êm; tm == null thì Capture tự giữ snapshot cũ nên mở lại cờ vô hại.
                if (Time.realtimeSinceStartup - start > 3f)
                {
                    SaveAdapters.TrainAdapter.LiveCaptureEnabled = true;
                    yield break;
                }
                yield return new WaitForSecondsRealtime(0.5f);
                continue;
            }

            if (ReferenceEquals(tm, _trainInstanceRestored))
            {
                SaveAdapters.TrainAdapter.LiveCaptureEnabled = true;
                yield break;   // instance này đã áp rồi
            }

            if (tm.State == TrainState.WaitingForLoad && tm.SlotData != null)
            {
                if (SaveAdapters.TrainAdapter.TryRestore(known.train))
                {
                    _trainInstanceRestored = tm;
                    Debug.Log("[Save] Train: đã áp snapshot chuyến tàu từ save.");
                }
                SaveAdapters.TrainAdapter.LiveCaptureEnabled = true;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        // Hết giờ chờ (tàu kẹt init / thiếu ref trong Inspector) — mở lại chụp sống để
        // save không bị đóng băng vĩnh viễn ở snapshot cũ.
        Debug.LogWarning("[Save] Train: chờ tàu init quá " + TrainWaitTimeoutSeconds +
                         "s — bỏ qua phục hồi chuyến, tiếp tục chụp trạng thái hiện tại.");
        SaveAdapters.TrainAdapter.LiveCaptureEnabled = true;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUTO-SAVE
    // ═════════════════════════════════════════════════════════════════════

    private void HandleCurrencyChanged(int gold, int gems) => MarkDirty();
    private void HandleExpChanged(int cur, int req)        => MarkDirty();
    private void HandleLevelChanged(int level)             => MarkDirty();
    private static void HandleMissionProgress(string key, int value) => MarkDirty();
    private static void HandlePlotEvent(PlotController plot)         => MarkDirty();

    /// <summary>Đánh dấu "có gì đó đổi, hẹn lưu". Rẻ; gọi từ bất kỳ đâu cũng được.</summary>
    public static void MarkDirty()
    {
        if (_dirty) return;
        _dirty = true;
        // Mốc đặt tại lần đánh dấu ĐẦU TIÊN — thao tác liên tục (thu hoạch cả ruộng)
        // vẫn tới hạn ghi, không rơi vào bẫy debounce vô hạn (học từ LuuGopPrefs).
        _saveDueAt = Time.realtimeSinceStartup + DebounceSeconds;
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;

        if (_dirty && now >= _saveDueAt)
        {
            _dirty = false;
            SaveSystem.Save("auto");
            _nextPeriodicAt = now + PeriodicSeconds;   // vừa lưu xong thì dời lưới định kỳ
            return;
        }

        if (now >= _nextPeriodicAt)
        {
            _nextPeriodicAt = now + PeriodicSeconds;
            SaveSystem.Save("periodic");
        }
    }

    // Người chơi mobile hầu như không "thoát" game — họ thu app. Thiếu nhánh pause/focus
    // là mất dữ liệu khi hệ điều hành giết tiến trình (cùng lý do LuuGopPrefs flush ở đây).
    private void OnApplicationPause(bool paused) { if (paused)  { _dirty = false; SaveSystem.Save("pause"); } }
    private void OnApplicationFocus(bool focus)  { if (!focus)  { _dirty = false; SaveSystem.Save("focus"); } }
    private void OnApplicationQuit()             {               _dirty = false; SaveSystem.Save("quit");   }
}
