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

    /// <summary>True when map panning should be blocked.</summary>
    public static bool BlockMapPan =>
        IsSeedPopupOpen || IsDraggingSeed || IsDraggingSickle
        || (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen());

    /// <summary>True when map zoom should be blocked (e.g. sickle/harvest mode active).</summary>
    public static bool BlockMapZoom => IsDraggingSickle;
}
