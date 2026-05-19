using UnityEngine.SceneManagement;

/// <summary>
/// Centralized input lock flags for the farm scene.
/// Set flags to prevent map pan while popup or drag is active.
/// No MonoBehaviour needed — all static.
/// </summary>
public static class FarmInputLock
{
    /// <summary>True while the seed selection popup is visible.</summary>
    public static bool IsSeedPopupOpen  { get; set; }

    /// <summary>True while the player is dragging a seed icon.</summary>
    public static bool IsDraggingSeed   { get; set; }

    /// <summary>True while the player is dragging the sickle tool.</summary>
    public static bool IsDraggingSickle { get; set; }

    // Resets all flags when entering Play mode (SubsystemRegistration = rất sớm, trước scene đầu tiên)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitOnPlayMode()
    {
        ResetAll();

        // Đăng ký callback reset khi mỗi scene mới được load trong phiên chơi
        // (SubsystemRegistration chỉ chạy 1 lần khi bắt đầu Play, không chạy lại khi đổi scene)
        SceneManager.sceneLoaded -= OnSceneLoaded; // tránh đăng ký trùng
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset tất cả flag khi vào scene mới — tránh trạng thái cũ từ scene trước bị mang sang
        ResetAll();
    }

    /// <summary>Reset tất cả flag về trạng thái mặc định (không chặn input).</summary>
    public static void ResetAll()
    {
        IsSeedPopupOpen  = false;
        IsDraggingSeed   = false;
        IsDraggingSickle = false;
    }

    /// <summary>True when map panning should be blocked.</summary>
    public static bool BlockMapPan =>
        IsSeedPopupOpen || IsDraggingSeed || IsDraggingSickle
        || (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen());

    /// <summary>True when map zoom should be blocked (e.g. sickle/harvest mode active).</summary>
    public static bool BlockMapZoom => IsDraggingSickle;
}
