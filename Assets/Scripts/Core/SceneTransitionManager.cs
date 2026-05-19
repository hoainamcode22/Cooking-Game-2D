using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản gia duy nhất chịu trách nhiệm chuyển cảnh Farm ↔ Cooking.
/// - Bật màn đen (overlay) để block toàn bộ input trong khi chuyển.
/// - Điều phối đúng thứ tự: hide/show Farm canvas+camera, load/unload scene,
///   dọn dẹp EventSystem trùng, reset mọi input lock và popup khi về Farm.
/// - Không một UI Button nào được gọi SceneManager trực tiếp.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private const string CookingSceneName = "SampleScene";
    private const float  FadeDuration     = 0.2f;

    private CanvasGroup overlayGroup;
    private bool        isTransitioning;

    /// <summary>True khi đang trong quá trình chuyển scene (màn đen đang kéo xuống/lên).</summary>
    public bool IsTransitioning => isTransitioning;

    // ── Singleton ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    // ── Overlay (tạo bằng code, không cần Prefab) ─────────────────────────────

    private void BuildOverlay()
    {
        // Canvas full-screen, sort order cao nhất để che mọi thứ
        var canvasGo = new GameObject("[TransitionOverlay]");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Panel đen che toàn màn hình
        var imgGo = new GameObject("BlackPanel");
        imgGo.transform.SetParent(canvasGo.transform, false);
        var img  = imgGo.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        var rect = imgGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayGroup = canvasGo.AddComponent<CanvasGroup>();
        overlayGroup.alpha          = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable   = false;
    }

    private IEnumerator FadeOverlay(bool fadeIn)
    {
        float start = fadeIn ? 0f : 1f;
        float end   = fadeIn ? 1f : 0f;
        float t     = 0f;

        // Bật block raycast ngay từ đầu fade-in để không ai click được trong lúc chuyển
        if (fadeIn)
        {
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable   = false;
        }

        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(start, end, t / FadeDuration);
            yield return null;
        }

        overlayGroup.alpha = end;

        // Sau fade-out: trả lại raycast cho scene
        if (!fadeIn)
        {
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable   = false;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void GoToCooking()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Đang chuyển cảnh, bỏ qua GoToCooking.");
            return;
        }
        if (SceneManager.GetSceneByName(CookingSceneName).isLoaded)
        {
            Debug.LogWarning("[SceneTransitionManager] SampleScene đã load rồi.");
            return;
        }
        StartCoroutine(GoToCookingRoutine());
    }

    public void ReturnToFarm()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Đang chuyển cảnh, bỏ qua ReturnToFarm.");
            return;
        }
        StartCoroutine(ReturnToFarmRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>Farm → Cooking</summary>
    private IEnumerator GoToCookingRoutine()
    {
        isTransitioning = true;
        Debug.Log("[SceneTransitionManager] → Bắt đầu vào Bếp");

        // 1. Kéo màn đen xuống — block toàn bộ input
        yield return StartCoroutine(FadeOverlay(true));

        // 2. Ẩn toàn bộ Farm UI + Camera (dữ liệu vẫn còn nguyên)
        FarmUIManager.Instance?.EnterCookingMode();

        // 3. Load SampleScene Additive
        AsyncOperation op = SceneManager.LoadSceneAsync(CookingSceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // 4. Chờ 1 frame để Awake/Start của scene Bếp chạy xong
        yield return null;

        // 5. Xóa EventSystem trùng (giữ của Farm, xóa của Cooking)
        DestroyDuplicateEventSystems();

        // 6. Mở màn lên — Bếp hiện ra
        yield return StartCoroutine(FadeOverlay(false));

        isTransitioning = false;
        Debug.Log("[SceneTransitionManager] → Vào Bếp xong");
    }

    /// <summary>Cooking → Farm</summary>
    private IEnumerator ReturnToFarmRoutine()
    {
        isTransitioning = true;
        Debug.Log("[SceneTransitionManager] → Bắt đầu về Farm");

        // 1. Kéo màn đen xuống — block toàn bộ input
        yield return StartCoroutine(FadeOverlay(true));

        // 2. Unload SampleScene
        Scene cookingScene = SceneManager.GetSceneByName(CookingSceneName);
        if (cookingScene.IsValid() && cookingScene.isLoaded)
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(cookingScene);
            while (!op.isDone) yield return null;
        }

        // 3. Chờ 1 frame sau khi Unload hoàn tất
        yield return null;

        // 4. Bật lại Farm UI + Camera
        FarmUIManager.Instance?.ExitCookingMode();

        // 5. HARD RESET: dọn sạch mọi Popup, RaycastBlocker, InputLock còn sót
        if (FarmUIManager.Instance != null)
            FarmUIManager.Instance.ForceCloseAllPopups();

        FarmInputLock.ResetAll();
        Time.timeScale = 1f;

        // 6. Đảm bảo EventSystem không bị mất (safety net)
        EnsureEventSystem();

        // 7. Thêm 1 frame để Farm UI hoàn tất việc rebuild layout
        yield return null;

        // 8. Mở màn lên — Farm hiện ra, nút bấm mượt
        yield return StartCoroutine(FadeOverlay(false));

        isTransitioning = false;
        Debug.Log("[SceneTransitionManager] → Về Farm xong");
    }

    // ── EventSystem Helpers ───────────────────────────────────────────────────

    /// <summary>Xóa EventSystem trùng lặp. Ưu tiên giữ cái thuộc Farm scene.</summary>
    private static void DestroyDuplicateEventSystems()
    {
#if UNITY_2023_1_OR_NEWER
        EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        EventSystem[] all = FindObjectsOfType<EventSystem>(true);
#endif
        if (all.Length <= 1) return;

        EventSystem toKeep = null;
        foreach (EventSystem es in all)
        {
            if (!es.isActiveAndEnabled) continue;

            if (toKeep == null) { toKeep = es; continue; }

            // Ưu tiên EventSystem thuộc scene thật (không phải DontDestroyOnLoad)
            bool esInScene   = es.gameObject.scene.name     != "DontDestroyOnLoad";
            bool keepInScene = toKeep.gameObject.scene.name != "DontDestroyOnLoad";

            // Trong 2 scene thật, ưu tiên cái thuộc Farm (không phải SampleScene)
            if (esInScene && !keepInScene)
                toKeep = es;
            else if (esInScene && keepInScene
                     && es.gameObject.scene.name != CookingSceneName)
                toKeep = es;
        }

        foreach (EventSystem es in all)
        {
            if (es == toKeep) continue;
            Debug.LogWarning(
                $"[SceneTransitionManager] Xóa EventSystem trùng: '{es.gameObject.name}'" +
                $" (scene: {es.gameObject.scene.name})");
            Destroy(es.gameObject);
        }
    }

    /// <summary>Nếu không còn EventSystem nào (bị xóa nhầm), tự tạo lại.</summary>
    private static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        EventSystem[] all = FindObjectsOfType<EventSystem>(true);
#endif
        if (all.Length > 0) return;

        var go = new GameObject("EventSystem [Auto-Restored]");
        go.AddComponent<EventSystem>();

        // Hỗ trợ cả Input System mới lẫn Input Manager cũ
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        Debug.LogWarning("[SceneTransitionManager] EventSystem bị mất — đã tự tạo lại.");
    }
}
