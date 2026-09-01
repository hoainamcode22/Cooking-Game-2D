using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng "kim cương bay về HUD" — anh em sinh đôi của CoinFlyFX:
/// nghe FarmEconomyManager.OnGemAddedFx, spawn vài viên gem UI tại con trỏ,
/// bung nhẹ rồi bay về icon kim cương/Diamond_Container trên HUD, thu nhỏ, nảy mẩy mẩy và tự huỷ.
/// </summary>
public class GemFlyFX : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Canvas canvas;               // HUD canvas (chứa icon kim cương)
    [SerializeField] private RectTransform targetGemIcon; // đích bay — icon kim cương/Diamond_Container trên HUD
    [SerializeField] private Sprite gemSprite;            // sprite gem (mặc định: lấy từ icon đích)

    [Header("Tuning")]
    [SerializeField] private int maxGems = 5;
    [SerializeField] private float flyDuration = 0.65f;

    private const float GemSize      = 34f;
    private const float BurstRadius  = 55f;
    private const float BurstTime    = 0.12f;
    private const float StaggerDelay = 0.05f;
    private const float EndScale     = 0.45f;

    private static Sprite fallbackSprite;
    private readonly List<GameObject> liveGems = new List<GameObject>(8);
    private WaitForSeconds staggerWait;
    private bool triedAutoFind;

    /// <summary>Lưới an toàn: chưa chạy Setup tool thì tự gắn cạnh CoinFlyFX (auto-find icon gem).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GemFlyFX>(FindObjectsInactive.Include) != null) return;
        var coinFx = FindFirstObjectByType<CoinFlyFX>(FindObjectsInactive.Include);
        if (coinFx != null) coinFx.gameObject.AddComponent<GemFlyFX>();
    }

    private void OnEnable() => FarmEconomyManager.OnGemAddedFx += HandleGemAdded;

    private void OnDisable()
    {
        FarmEconomyManager.OnGemAddedFx -= HandleGemAdded;
        for (int i = 0; i < liveGems.Count; i++)
            if (liveGems[i] != null) Destroy(liveGems[i]);
        liveGems.Clear();
    }

    private void HandleGemAdded(int amount)
    {
        if (!isActiveAndEnabled || amount <= 0) return;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        AutoFindTargetIfNeeded();

        Vector2 startScreen;
        var pointer = Pointer.current;
        startScreen = pointer != null
            ? pointer.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height / 3f);

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 endScreen = targetGemIcon != null
            ? RectTransformUtility.WorldToScreenPoint(uiCam, targetGemIcon.position)
            : new Vector2(Screen.width * 0.88f, Screen.height * 0.94f);

        var canvasRect = canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        // Gem quý hiếm hơn vàng → vài viên là đủ cảm giác
        int count = Mathf.Clamp(amount / 3 + 1, 1, Mathf.Max(1, maxGems));

        Sprite sprite = gemSprite;
        if (sprite == null && targetGemIcon != null)
        {
            var targetImg = targetGemIcon.GetComponent<Image>();
            if (targetImg != null) sprite = targetImg.sprite;
        }
        if (sprite == null) sprite = GetFallbackSprite();

        StartCoroutine(SpawnBurst(count, sprite, startLocal, endLocal));
    }

    /// <summary>Tự tìm icon kim cương trên HUD theo tên (chạy 1 lần) — Setup tool có thể gán đè.</summary>
    private void AutoFindTargetIfNeeded()
    {
        if (targetGemIcon != null || triedAutoFind) return;
        triedAutoFind = true;

        var diamondContainer = GameObject.Find("Diamond_Container");
        if (diamondContainer != null)
        {
            targetGemIcon = diamondContainer.GetComponent<RectTransform>();
            return;
        }

        var iconDiamond = GameObject.Find("Icon_Diamond");
        if (iconDiamond != null)
        {
            targetGemIcon = iconDiamond.GetComponent<RectTransform>();
            return;
        }

        if (canvas != null)
        {
            var rects = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in rects)
            {
                string n = rt.gameObject.name.ToLowerInvariant();
                if ((n.Contains("kimcuong") || n.Contains("gem") || n.Contains("diamond"))
                    && rt.GetComponent<Image>() != null)
                {
                    targetGemIcon = rt;
                    return;
                }
            }
        }
    }

    private IEnumerator SpawnBurst(int count, Sprite sprite, Vector2 startLocal, Vector2 endLocal)
    {
        staggerWait ??= new WaitForSeconds(StaggerDelay);
        for (int i = 0; i < count; i++)
        {
            RectTransform gem = CreateGem(sprite);
            Vector2 burstPos = startLocal + Random.insideUnitCircle * BurstRadius;
            StartCoroutine(FlyGem(gem, startLocal, burstPos, endLocal));
            yield return staggerWait;
        }
    }

    private RectTransform CreateGem(Sprite sprite)
    {
        var go = new GameObject("GemFx", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(GemSize, GemSize);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        liveGems.Add(go);
        return rt;
    }

    private IEnumerator FlyGem(RectTransform gem, Vector2 from, Vector2 burstPos, Vector2 to)
    {
        gem.anchoredPosition = from;
        float spin = Random.Range(-160f, 160f);

        float t = 0f;
        while (t < BurstTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / BurstTime);
            k = k * (2f - k);
            gem.anchoredPosition = Vector2.Lerp(from, burstPos, k);
            gem.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }

        float dur = Mathf.Max(0.05f, flyDuration);
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            k = k * k * (3f - 2f * k); // smoothstep
            gem.anchoredPosition = Vector2.LerpUnclamped(burstPos, to, k);
            float s = Mathf.Lerp(1f, EndScale, Mathf.Clamp01(t / dur));
            gem.localScale = new Vector3(s, s, 1f);
            gem.Rotate(0f, 0f, spin * Time.deltaTime);
            yield return null;
        }

        // Chạm đích: hiệu ứng mẩy mẩy trên Diamond_Container
        if (targetGemIcon != null)
        {
            JuicyPulseFX.Play(targetGemIcon, 1.20f, 0.22f);
        }

        liveGems.Remove(gem.gameObject);
        Destroy(gem.gameObject);
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;

        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear
        };

        var bright = new Color(0.35f, 0.85f, 1f, 1f);
        var deep   = new Color(0.10f, 0.45f, 0.85f, 1f);
        var clear  = new Color(0f, 0f, 0f, 0f);
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                float r = size * 0.5f;
                tex.SetPixel(x, y, d <= r - 2f ? bright : d <= r ? deep : clear);
            }
        tex.Apply();

        fallbackSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        return fallbackSprite;
    }
}
