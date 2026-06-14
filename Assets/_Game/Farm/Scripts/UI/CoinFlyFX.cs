using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng "coin bay về ví" kiểu casual: khi nhận vàng (FarmEconomyManager.OnGoldAddedFx),
/// spawn vài đồng xu UI tại vị trí con trỏ (hoặc giữa màn hình), bung nhẹ ra rồi bay về
/// icon vàng trên HUD, thu nhỏ dần và tự huỷ. Không cần prefab — UI Image thuần runtime.
/// Null-safety: thiếu canvas → bỏ qua (log 1 lần); thiếu targetGoldIcon → bay về góc
/// phải-trên màn hình; thiếu coinSprite → tự vẽ đồng xu vàng tròn 16x16.
/// </summary>
public class CoinFlyFX : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Canvas canvas;                // HUD canvas (chứa icon vàng)
    [SerializeField] private RectTransform targetGoldIcon; // đích bay — icon vàng trên HUD
    [SerializeField] private Sprite coinSprite;            // sprite đồng xu (mặc định: lấy từ icon vàng)

    [Header("Tuning")]
    [SerializeField] private int maxCoins = 6;
    [SerializeField] private float flyDuration = 0.7f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private const float CoinSize     = 36f;   // px trên canvas
    private const float BurstRadius  = 60f;   // bán kính bung ra lúc spawn
    private const float BurstTime    = 0.12f; // thời gian bung
    private const float StaggerDelay = 0.05f; // mỗi xu cách nhau
    private const float EndScale     = 0.45f; // thu nhỏ khi về ví

    private static Sprite fallbackSprite;     // đồng xu vẽ runtime, cache dùng chung
    private readonly List<GameObject> liveCoins = new List<GameObject>(8);
    private WaitForSeconds staggerWait;
    private bool warnedMissingCanvas;

    private void OnEnable() => FarmEconomyManager.OnGoldAddedFx += HandleGoldAdded;

    private void OnDisable()
    {
        FarmEconomyManager.OnGoldAddedFx -= HandleGoldAdded;

        // Coroutine chết khi disable → dọn xu đang bay dở để không kẹt trên HUD
        for (int i = 0; i < liveCoins.Count; i++)
        {
            if (liveCoins[i] != null)
                Destroy(liveCoins[i]);
        }
        liveCoins.Clear();
    }

    private void HandleGoldAdded(int amount)
    {
        if (!isActiveAndEnabled || amount <= 0)
            return;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            if (!warnedMissingCanvas)
            {
                Debug.LogWarning("[CoinFlyFX] Thiếu Canvas — bỏ qua hiệu ứng coin bay.");
                warnedMissingCanvas = true;
            }
            return;
        }

        // Điểm xuất phát: vị trí con trỏ (Input System); fallback giữa màn hình, 1/3 dưới.
        Vector2 startScreen;
        var pointer = Pointer.current;
        if (pointer != null)
            startScreen = pointer.position.ReadValue();
        else
            startScreen = new Vector2(Screen.width * 0.5f, Screen.height / 3f);

        // Điểm đích: icon vàng HUD; fallback góc phải-trên màn hình (hoạt động cả khi chưa gắn).
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 endScreen = targetGoldIcon != null
            ? RectTransformUtility.WorldToScreenPoint(uiCam, targetGoldIcon.position)
            : new Vector2(Screen.width * 0.85f, Screen.height * 0.92f);

        var canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        int count = Mathf.Clamp(amount / 15 + 1, 1, Mathf.Max(1, maxCoins));
        Sprite sprite = coinSprite != null ? coinSprite : GetFallbackSprite();

        StartCoroutine(SpawnBurst(count, sprite, startLocal, endLocal));
    }

    private IEnumerator SpawnBurst(int count, Sprite sprite, Vector2 startLocal, Vector2 endLocal)
    {
        staggerWait ??= new WaitForSeconds(StaggerDelay);

        for (int i = 0; i < count; i++)
        {
            RectTransform coin = CreateCoin(sprite);
            Vector2 burstPos = startLocal + Random.insideUnitCircle * BurstRadius;
            StartCoroutine(FlyCoin(coin, startLocal, burstPos, endLocal));
            yield return staggerWait;
        }
    }

    private RectTransform CreateCoin(Sprite sprite)
    {
        var go = new GameObject("CoinFx", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CoinSize, CoinSize);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        liveCoins.Add(go);
        return rt;
    }

    private IEnumerator FlyCoin(RectTransform coin, Vector2 from, Vector2 burstPos, Vector2 to)
    {
        coin.anchoredPosition = from;
        float spin = Random.Range(-200f, 200f); // xoay nhẹ cho sinh động

        // Pha 1: bung nhẹ ra khỏi điểm xuất phát
        float t = 0f;
        while (t < BurstTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / BurstTime);
            k = k * (2f - k); // ease-out
            coin.anchoredPosition = Vector2.Lerp(from, burstPos, k);
            coin.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }

        // Pha 2: bay về ví, thu nhỏ dần
        float dur = Mathf.Max(0.05f, flyDuration);
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float raw = Mathf.Clamp01(t / dur);
            float k = ease != null && ease.length > 1 ? ease.Evaluate(raw) : raw;
            coin.anchoredPosition = Vector2.LerpUnclamped(burstPos, to, k);
            float s = Mathf.Lerp(1f, EndScale, raw);
            coin.localScale = new Vector3(s, s, 1f);
            coin.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }

        liveCoins.Remove(coin.gameObject);
        Destroy(coin.gameObject);
    }

    // Đồng xu dự phòng: texture 16x16 vàng, viền sậm, alpha tròn — tạo 1 lần rồi cache.
    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
            return fallbackSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };

        var gold  = new Color(1f, 0.84f, 0.2f, 1f);
        var rim   = new Color(0.85f, 0.58f, 0.05f, 1f);
        var clear = new Color(0f, 0f, 0f, 0f);
        float c = (size - 1) * 0.5f;
        float r = size * 0.5f - 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                tex.SetPixel(x, y, d <= r - 1.5f ? gold : d <= r ? rim : clear);
            }
        }
        tex.Apply();

        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return fallbackSprite;
    }
}
