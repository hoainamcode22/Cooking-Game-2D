using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng sóng nước ánh sáng (Touch / Tap Ripple FX) tự động xuất hiện tại mỗi điểm chạm của người chơi.
/// Tự động khởi tạo và chạy ngầm toàn game, không cần kéo thả setup.
/// </summary>
public class GlobalTouchRippleFX : MonoBehaviour
{
    private static GlobalTouchRippleFX _instance;
    private Canvas _overlayCanvas;
    private Sprite _rippleSprite;
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("[GlobalTouchRippleFX]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<GlobalTouchRippleFX>();
    }

    private void Awake()
    {
        _instance = this;
        CreateOverlayCanvas();
        GenerateRippleSprite();
    }

    private void Update()
    {
        // Nhận diện chạm chuột / cảm ứng
        if (Input.GetMouseButtonDown(0))
        {
            SpawnRipple(Input.mousePosition);
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    SpawnRipple(t.position);
                }
            }
        }
    }

    private void CreateOverlayCanvas()
    {
        var canvasGo = new GameObject("TouchRipple_Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        _overlayCanvas = canvasGo.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 32000; // Vẽ trên cùng

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        var cg = canvasGo.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    private void GenerateRippleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.46f;
        float thickness = size * 0.12f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float ringDist = Mathf.Abs(dist - (radius - thickness * 0.5f));
                float alpha = Mathf.Clamp01(1f - (ringDist / (thickness * 0.5f)));
                alpha = Mathf.Pow(alpha, 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        _rippleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void SpawnRipple(Vector2 screenPos)
    {
        if (_overlayCanvas == null) return;

        GameObject rippleGo = GetPooledRipple();
        rippleGo.transform.position = screenPos;
        rippleGo.SetActive(true);

        StartCoroutine(RoutineAnimateRipple(rippleGo));
    }

    private GameObject GetPooledRipple()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }

        var go = new GameObject("RippleFX", typeof(RectTransform));
        go.transform.SetParent(_overlayCanvas.transform, false);

        var img = go.AddComponent<Image>();
        img.sprite = _rippleSprite;
        img.color = new Color(1f, 0.95f, 0.7f, 0.8f);
        img.raycastTarget = false;

        return go;
    }

    private IEnumerator RoutineAnimateRipple(GameObject rippleGo)
    {
        RectTransform rt = (RectTransform)rippleGo.transform;
        Image img = rippleGo.GetComponent<Image>();

        float dur = 0.28f;
        float elapsed = 0f;
        float startSize = 20f;
        float endSize = 110f;
        Color startCol = new Color(1f, 0.92f, 0.65f, 0.75f);

        while (elapsed < dur && rippleGo != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);

            float size = Mathf.Lerp(startSize, endSize, 1f - (1f - t) * (1f - t)); // Ease out
            rt.sizeDelta = new Vector2(size, size);

            float alpha = Mathf.Lerp(startCol.a, 0f, t * t);
            img.color = new Color(startCol.r, startCol.g, startCol.b, alpha);

            yield return null;
        }

        if (rippleGo != null)
        {
            rippleGo.SetActive(false);
            _pool.Enqueue(rippleGo);
        }
    }
}
