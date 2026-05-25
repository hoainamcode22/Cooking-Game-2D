using UnityEngine;

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

    /// <summary>True while the market popup is open.</summary>
    public static bool IsMarketPopupOpen { get; set; }

    private static int _popupOpenCount = 0;

    /// <summary>True when map panning should be blocked.</summary>
    public static bool BlockMapPan =>
        IsSeedPopupOpen || IsDraggingSeed || IsDraggingSickle || _popupOpenCount > 0
        || (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen());

    /// <summary>True when map zoom should be blocked (e.g. sickle/harvest mode active).</summary>
    public static bool BlockMapZoom => IsDraggingSickle;

    public static void RegisterPopupOpen()  => _popupOpenCount++;
    public static void RegisterPopupClose() => _popupOpenCount = Mathf.Max(0, _popupOpenCount - 1);

    public static void SetPopupRaycastBlock(GameObject target, bool block)
    {
        if (target == null) return;
        var canvas = target.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (canvas != null) canvas.enabled = !block;
    }

    public static void ResetAll()
    {
        IsSeedPopupOpen   = false;
        IsDraggingSeed    = false;
        IsDraggingSickle  = false;
        IsMarketPopupOpen = false;
        _popupOpenCount   = 0;
    }
}
