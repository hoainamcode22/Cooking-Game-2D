using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FarmInputLock
{
    /// <summary>True while the seed selection popup is visible.</summary>
    public static bool IsSeedPopupOpen  { get; set; }

    /// <summary>True while the player is dragging a seed icon.</summary>
    public static bool IsDraggingSeed   { get; set; }

    /// <summary>True while the player is dragging the sickle tool.</summary>
    public static bool IsDraggingSickle { get; set; }

    /// <summary>True while the generated Market popup is visible.</summary>
    public static bool IsMarketPopupOpen { get; set; }

    private static int popupLockCount;
    private static int suppressWorldClickUntilFrame = -1;

    public static bool IsPopupOpen => popupLockCount > 0;

    // Resets all flags when entering Play mode (SubsystemRegistration = rất sớm, trước scene đầu tiên)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitOnPlayMode()
    {
        ResetAll();

        // Đăng ký callback reset khi mỗi scene mới được load trong phiên chơi
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetAll();
    }

    /// <summary>Reset tất cả flag về trạng thái mặc định (không chặn input).</summary>
    public static void ResetAll()
    {
        IsSeedPopupOpen  = false;
        IsDraggingSeed   = false;
        IsDraggingSickle = false;
        IsMarketPopupOpen = false;
        popupLockCount = 0;
        suppressWorldClickUntilFrame = -1;
    }

    /// <summary>True when map panning should be blocked.</summary>
    public static bool BlockMapPan =>
        IsSeedPopupOpen || IsDraggingSeed || IsDraggingSickle || IsMarketPopupOpen || IsPopupOpen || IsWorldClickSuppressed
        || (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen());

    /// <summary>True when map zoom should be blocked (e.g. sickle/harvest mode active).</summary>
    public static bool BlockMapZoom => IsDraggingSickle;

    public static void RegisterPopupOpen()
    {
        popupLockCount++;
    }

    public static void RegisterPopupClose()
    {
        if (popupLockCount > 0)
            popupLockCount--;

        SuppressWorldClickForCurrentFrame();
    }

    public static void SuppressWorldClickForCurrentFrame()
    {
        suppressWorldClickUntilFrame = Mathf.Max(suppressWorldClickUntilFrame, Time.frameCount);
    }

    private static bool IsWorldClickSuppressed => Time.frameCount <= suppressWorldClickUntilFrame;

    public static void SetPopupRaycastBlock(GameObject popupRoot, bool isBlocking)
    {
        if (popupRoot == null)
            return;

        if (isBlocking && popupRoot.GetComponent<UIRaycastBlocker>() == null)
            popupRoot.AddComponent<UIRaycastBlocker>();

        Image image = popupRoot.GetComponent<Image>();
        if (image == null && popupRoot.GetComponent<RectTransform>() != null)
        {
            image = popupRoot.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
        }

        if (image != null)
            image.raycastTarget = isBlocking;

        CanvasGroup canvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = popupRoot.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = isBlocking;
        canvasGroup.interactable = isBlocking;
    }
}
