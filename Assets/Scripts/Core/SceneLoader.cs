using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Singleton quản lý toàn bộ việc chuyển scene bất đồng bộ.
/// Tự tạo instance nếu chưa có — không cần kéo thả vào scene.
/// Hỗ trợ loading overlay tuỳ chọn (gán qua Inspector hoặc để trống).
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ─── Singleton lazy-init: không cần prefab, không lo null ────────────────
    private static SceneLoader _instance;
    public static SceneLoader Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("[SceneLoader]");
            _instance = go.AddComponent<SceneLoader>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    [Header("Loading Overlay (tuỳ chọn — để trống nếu chưa có UI)")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;
    [SerializeField] private Slider progressBar;

    // Thời gian fade in/out overlay (giây)
    private const float FadeDuration = 0.15f;

    public bool IsLoading { get; private set; }

    // ─── Sự kiện để các script khác lắng nghe nếu cần ───────────────────────
    public static event Action<string> OnSceneLoadStart;
    public static event Action<string> OnSceneLoadComplete;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        ForceHideOverlay();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Load scene thay thế scene hiện tại (không additive).</summary>
    public void LoadScene(string sceneName)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoader] Đang load scene khác, bỏ qua yêu cầu: {sceneName}");
            return;
        }
        StartCoroutine(LoadFullAsync(sceneName));
    }

    /// <summary>Load scene chồng lên scene hiện tại (Additive).</summary>
    public void LoadSceneAdditive(string sceneName, Action onComplete = null)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoader] Đang load scene khác, bỏ qua yêu cầu additive: {sceneName}");
            return;
        }
        StartCoroutine(LoadAdditiveAsync(sceneName, onComplete));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  COROUTINES CHÍNH
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load thay thế toàn scene — dùng cho Home→Farm, Home→Cooking.
    /// allowSceneActivation = false giữ Main Thread không bị block cho đến khi
    /// tài nguyên load xong 90%, sau đó mới kích hoạt trong 1 frame sạch.
    /// </summary>
    private IEnumerator LoadFullAsync(string sceneName)
    {
        IsLoading = true;
        OnSceneLoadStart?.Invoke(sceneName);
        Debug.Log($"[SceneLoader] Bắt đầu load: {sceneName}");

        yield return StartCoroutine(FadeOverlay(true));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Chờ load tài nguyên (0% → 90%), Main Thread vẫn chạy bình thường
        while (op.progress < 0.9f)
        {
            SetProgress(op.progress / 0.9f);
            yield return null;
        }

        SetProgress(1f);

        // Cho 1 frame để UI cập nhật progress = 100% trước khi chuyển scene
        yield return null;

        // Kích hoạt scene — từ đây Unity mới thực sự "đổi" sang scene mới
        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        // Chờ thêm 1 frame để tất cả Awake/Start trong scene mới hoàn tất
        yield return null;

        // Diệt EventSystem trùng lặp nếu có (DontDestroyOnLoad có thể mang EventSystem cũ sang)
        DestroyDuplicateEventSystems();

        yield return StartCoroutine(FadeOverlay(false));
        ForceHideOverlay();

        IsLoading = false;
        OnSceneLoadComplete?.Invoke(sceneName);
        Debug.Log($"[SceneLoader] Load xong: {sceneName}");
    }

    /// <summary>
    /// Load chồng (Additive) — dùng cho Farm → Cooking Scene.
    /// Không cần fade overlay vì Farm scene vẫn hiển thị xuyên suốt.
    /// </summary>
    private IEnumerator LoadAdditiveAsync(string sceneName, Action onComplete)
    {
        IsLoading = true;
        OnSceneLoadStart?.Invoke(sceneName);
        Debug.Log($"[SceneLoader] Bắt đầu load additive: {sceneName}");

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;

        yield return null; // 1 frame để Awake/Start trong scene additive hoàn tất

        DestroyDuplicateEventSystems();
        ForceHideOverlay();

        IsLoading = false;
        OnSceneLoadComplete?.Invoke(sceneName);
        onComplete?.Invoke();
        Debug.Log($"[SceneLoader] Load additive xong: {sceneName}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private void SetProgress(float normalized)
    {
        if (progressBar != null)
            progressBar.value = Mathf.Clamp01(normalized);
    }

    private IEnumerator FadeOverlay(bool fadeIn)
    {
        if (loadingCanvasGroup == null)
            yield break;

        loadingCanvasGroup.gameObject.SetActive(true);

        if (fadeIn)
        {
            // Khi fade vào: chặn toàn bộ raycast để che quá trình load
            loadingCanvasGroup.blocksRaycasts = true;
            loadingCanvasGroup.interactable   = false;
        }
        else
        {
            // Khi fade ra: ngay lập tức trả lại raycast cho scene mới
            // TRƯỚC khi bắt đầu fade animation (tránh input bị block trong lúc overlay tàng hình)
            loadingCanvasGroup.blocksRaycasts = false;
            loadingCanvasGroup.interactable   = false;
        }

        float start   = fadeIn ? 0f : 1f;
        float target  = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / FadeDuration);
            yield return null;
        }

        loadingCanvasGroup.alpha = target;

        if (!fadeIn)
            ForceHideOverlay();
    }

    /// <summary>
    /// Đảm bảo overlay bị ẩn và không chặn raycast — gọi khi chắc chắn scene đã load xong.
    /// Safety net phòng trường hợp FadeOverlay(false) bị gián đoạn.
    /// </summary>
    private void ForceHideOverlay()
    {
        if (loadingCanvasGroup == null) return;
        loadingCanvasGroup.alpha          = 0f;
        loadingCanvasGroup.blocksRaycasts = false;
        loadingCanvasGroup.interactable   = false;
        loadingCanvasGroup.gameObject.SetActive(false);
    }

    /// <summary>
    /// Tìm và xoá các EventSystem trùng lặp.
    /// Ưu tiên giữ EventSystem thuộc scene thường (không phải DontDestroyOnLoad).
    /// Các EventSystem thừa bị Destroy hoàn toàn để tránh xung đột input.
    /// </summary>
    private static void DestroyDuplicateEventSystems()
    {
#if UNITY_2023_1_OR_NEWER
        EventSystem[] all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
#else
        EventSystem[] all = FindObjectsOfType<EventSystem>();
#endif
        if (all.Length <= 1) return;

        // Ưu tiên giữ EventSystem của scene hiện tại (không phải DontDestroyOnLoad từ scene cũ)
        EventSystem toKeep = null;
        foreach (EventSystem es in all)
        {
            if (!es.isActiveAndEnabled) continue;
            if (toKeep == null)
            {
                toKeep = es;
                continue;
            }
            bool esIsScene   = es.gameObject.scene.name != "DontDestroyOnLoad";
            bool keepIsScene = toKeep.gameObject.scene.name != "DontDestroyOnLoad";
            if (esIsScene && !keepIsScene)
                toKeep = es; // ưu tiên EventSystem của scene thực
        }

        foreach (EventSystem es in all)
        {
            if (es == toKeep) continue;
            Debug.LogWarning($"[SceneLoader] EventSystem trùng lặp bị xóa: '{es.gameObject.name}' (scene: {es.gameObject.scene.name})");
            Destroy(es.gameObject);
        }
    }
}
