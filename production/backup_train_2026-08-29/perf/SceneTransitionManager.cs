using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public enum TransitionType { CloudWipe, BoardDrop }

    private Canvas _transitionCanvas;
    private RectTransform _panel1;
    private Image _panel1Image;
    private RectTransform _panel2;
    private Image _panel2Image;
    private TextMeshProUGUI _loadingText;

    private bool _isTransitioning;
    private float _transitionDuration = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("SceneTransitionManager");
            obj.AddComponent<SceneTransitionManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupProceduralUI();
    }

    private void SetupProceduralUI()
    {
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        _transitionCanvas = canvasObj.AddComponent<Canvas>();
        _transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _transitionCanvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        _panel1 = CreatePanel("Panel1", canvasObj.transform, out _panel1Image);
        _panel2 = CreatePanel("Panel2", canvasObj.transform, out _panel2Image);

        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(canvasObj.transform, false);
        _loadingText = textObj.AddComponent<TextMeshProUGUI>();
        _loadingText.text = "Loading...";
        _loadingText.fontSize = 80;
        _loadingText.alignment = TextAlignmentOptions.Center;
        _loadingText.color = Color.white;
        _loadingText.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(600, 200);

        _transitionCanvas.gameObject.SetActive(false);
    }

    private RectTransform CreatePanel(string name, Transform parent, out Image img)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);
        img = panelObj.AddComponent<Image>();
        return panelObj.GetComponent<RectTransform>();
    }

    public void LoadScene(string sceneName, TransitionType type, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(type, () => SceneManager.LoadSceneAsync(sceneName, mode)));
    }

    public void UnloadScene(string sceneName, TransitionType type)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(type, () => SceneManager.UnloadSceneAsync(sceneName)));
    }

    private IEnumerator TransitionRoutine(TransitionType type, Func<AsyncOperation> loadAction)
    {
        _isTransitioning = true;
        _transitionCanvas.gameObject.SetActive(true);

        SetupPanelsForType(type);
        yield return StartCoroutine(AnimateIn(type));

        AsyncOperation op = loadAction.Invoke();
        
        // Loop squash/stretch until scene is fully loaded
        while (op != null && !op.isDone)
        {
            float time = Time.time * 6f;
            float scaleX = 1f + Mathf.Sin(time) * 0.15f;
            float scaleY = 1f + Mathf.Cos(time) * 0.15f;
            _loadingText.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
            yield return null;
        }

        _loadingText.rectTransform.localScale = Vector3.one;

        yield return StartCoroutine(AnimateOut(type));

        _transitionCanvas.gameObject.SetActive(false);
        _isTransitioning = false;
    }

    private void SetupPanelsForType(TransitionType type)
    {
        if (type == TransitionType.CloudWipe)
        {
            // Left Panel (White)
            _panel1.anchorMin = new Vector2(0, 0);
            _panel1.anchorMax = new Vector2(0.5f, 1);
            _panel1.sizeDelta = Vector2.zero;
            _panel1.anchoredPosition = new Vector2(-960, 0);
            _panel1Image.color = Color.white;
            _panel1.gameObject.SetActive(true);

            // Right Panel (White)
            _panel2.anchorMin = new Vector2(0.5f, 0);
            _panel2.anchorMax = new Vector2(1, 1);
            _panel2.sizeDelta = Vector2.zero;
            _panel2.anchoredPosition = new Vector2(960, 0);
            _panel2Image.color = Color.white;
            _panel2.gameObject.SetActive(true);

            _loadingText.color = Color.black; // Text contrast on white
        }
        else if (type == TransitionType.BoardDrop)
        {
            // Full screen panel (Brown)
            _panel1.anchorMin = new Vector2(0, 0);
            _panel1.anchorMax = new Vector2(1, 1);
            _panel1.sizeDelta = Vector2.zero;
            _panel1.anchoredPosition = new Vector2(0, 1080);
            _panel1Image.color = new Color(0.55f, 0.27f, 0.07f); // Wood brown
            _panel1.gameObject.SetActive(true);

            _panel2.gameObject.SetActive(false);

            _loadingText.color = Color.white;
        }
    }

    private IEnumerator AnimateIn(TransitionType type)
    {
        float t = 0;
        while (t < _transitionDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _transitionDuration);

            if (type == TransitionType.CloudWipe)
            {
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                _panel1.anchoredPosition = Vector2.Lerp(new Vector2(-960, 0), Vector2.zero, smoothProgress);
                _panel2.anchoredPosition = Vector2.Lerp(new Vector2(960, 0), Vector2.zero, smoothProgress);
            }
            else if (type == TransitionType.BoardDrop)
            {
                // Elastic/Bounce Drop
                float bounceProgress = EaseOutBounce(progress);
                _panel1.anchoredPosition = Vector2.Lerp(new Vector2(0, 1080), Vector2.zero, bounceProgress);
            }

            yield return null;
        }
    }

    private IEnumerator AnimateOut(TransitionType type)
    {
        float t = 0;
        while (t < _transitionDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / _transitionDuration);

            if (type == TransitionType.CloudWipe)
            {
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                _panel1.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(-960, 0), smoothProgress);
                _panel2.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(960, 0), smoothProgress);
            }
            else if (type == TransitionType.BoardDrop)
            {
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                _panel1.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0, 1080), smoothProgress);
            }

            yield return null;
        }
    }

    private float EaseOutBounce(float x)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (x < 1f / d1)
            return n1 * x * x;
        else if (x < 2f / d1)
            return n1 * (x -= 1.5f / d1) * x + 0.75f;
        else if (x < 2.5f / d1)
            return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        else
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
    }
}
