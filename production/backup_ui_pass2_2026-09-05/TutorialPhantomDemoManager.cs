using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HỆ THỐNG LIVE PHANTOM DEMO (ẢO ẢNH HƯỚNG DẪN TRỰC QUAN)
/// ══════════════════════════════════════════════════════════════════════════
/// Hiển thị ảo ảnh bàn tay mờ + icon hạt giống / liềm thu hoạch / kim cương
/// thực hiện thao tác mẫu trực tiếp trên scene (như một video demo in-game)
/// để người chơi nhìn mẫu là hiểu ngay cách thao tác kéo thả hoặc bấm nút.
/// </summary>
public class TutorialPhantomDemoManager : MonoBehaviour
{
    private static TutorialPhantomDemoManager _instance;
    public static TutorialPhantomDemoManager Instance => _instance;

    [Header("Phantom UI Setup")]
    [SerializeField] private CanvasGroup _phantomGroup;
    [SerializeField] private RectTransform _phantomHand;
    [SerializeField] private Image _handImage;
    [SerializeField] private RectTransform _phantomItem;
    [SerializeField] private Image _itemImage;
    [SerializeField] private Sprite _defaultHandSprite;
    [SerializeField] private Sprite _sickleSprite;
    [SerializeField] private Sprite _riceSeedSprite;

    private Coroutine _currentDemoCo;
    private bool _isDemoRunning = false;
    public bool IsDemoRunning => _isDemoRunning;

    void Awake()
    {
        _instance = this;
        EnsureUI();
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void EnsureUI()
    {
        if (_phantomGroup != null) return;

        // Tìm hoặc tạo container Phantom trên Canvas_Popup hoặc Canvas của Tutorial
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            var popCv = GameObject.Find("Canvas_Popup");
            if (popCv != null) canvas = popCv.GetComponent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null) return;

        Transform existing = canvas.transform.Find("Tutorial_Phantom_Demo_Layer");
        GameObject rootGo;
        if (existing != null)
        {
            rootGo = existing.gameObject;
        }
        else
        {
            rootGo = new GameObject("Tutorial_Phantom_Demo_Layer", typeof(RectTransform));
            rootGo.transform.SetParent(canvas.transform, false);
            var rt = rootGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        Canvas ownCanvas = rootGo.GetComponent<Canvas>();
        if (ownCanvas == null)
        {
            ownCanvas = rootGo.AddComponent<Canvas>();
        }
        if (ownCanvas != null)
        {
            ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = 450;
        }

        if (rootGo.GetComponent<GraphicRaycaster>() == null)
            rootGo.AddComponent<GraphicRaycaster>();

        _phantomGroup = rootGo.GetComponent<CanvasGroup>();
        if (_phantomGroup == null)
            _phantomGroup = rootGo.AddComponent<CanvasGroup>();

        _phantomGroup.alpha = 0f;
        _phantomGroup.blocksRaycasts = false;
        _phantomGroup.interactable = false;

        // Bàn tay ảo ảnh
        Transform handTf = rootGo.transform.Find("Phantom_Hand");
        if (handTf == null)
        {
            var hGo = new GameObject("Phantom_Hand", typeof(RectTransform), typeof(Image));
            hGo.transform.SetParent(rootGo.transform, false);
            _phantomHand = hGo.GetComponent<RectTransform>();
            _phantomHand.sizeDelta = new Vector2(96f, 96f);
            _phantomHand.pivot = new Vector2(0.36f, 0.9f); // đầu ngón tay
            _handImage = hGo.GetComponent<Image>();
            _handImage.raycastTarget = false;
        }
        else
        {
            _phantomHand = (RectTransform)handTf;
            _handImage = handTf.GetComponent<Image>();
        }

        if (_handImage != null && _handImage.sprite == null)
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.HandPointerRT != null)
                _handImage.sprite = TutorialManager.Instance.HandPointerRT.GetComponent<Image>()?.sprite;
        }

        // Item ảo ảnh theo tay (hạt giống / liềm)
        Transform itemTf = rootGo.transform.Find("Phantom_Item");
        if (itemTf == null)
        {
            var iGo = new GameObject("Phantom_Item", typeof(RectTransform), typeof(Image));
            iGo.transform.SetParent(rootGo.transform, false);
            _phantomItem = iGo.GetComponent<RectTransform>();
            _phantomItem.sizeDelta = new Vector2(64f, 64f);
            _phantomItem.pivot = new Vector2(0.5f, 0.5f);
            _itemImage = iGo.GetComponent<Image>();
            _itemImage.raycastTarget = false;
        }
        else
        {
            _phantomItem = (RectTransform)itemTf;
            _itemImage = itemTf.GetComponent<Image>();
        }
    }

    void Update()
    {
        // Khi người chơi chạm màn hình thực hiện thao tác thật -> tự động ẩn demo mượt mà
        if (_isDemoRunning && Input.GetMouseButtonDown(0))
        {
            // Người chơi bắt đầu chạm làm thật -> cho demo tạm mờ đi
            StartCoroutine(FadeOutQuick());
        }
    }

    private IEnumerator FadeOutQuick()
    {
        if (_phantomGroup == null) yield break;
        float a = _phantomGroup.alpha;
        while (a > 0f)
        {
            a -= Time.unscaledDeltaTime * 4f;
            _phantomGroup.alpha = Mathf.Clamp01(a);
            yield return null;
        }
    }

    // =========================================================================
    // Public Demos
    // =========================================================================

    /// <summary>
    /// ẢO ẢNH GIEO HẠT: Bàn tay + hạt giống lướt từ khay hạt giống vào ô đất làm mẫu.
    /// </summary>
    public void PlayPlantPhantom(Sprite seedSprite, string fromTargetId = "seed_rice", string toPlotId = "tutorial_plot_01")
    {
        StopDemo();
        EnsureUI();
        _isDemoRunning = true;
        _currentDemoCo = StartCoroutine(PlantRoutine(seedSprite, fromTargetId, toPlotId));
    }

    /// <summary>
    /// ẢO ẢNH TĂNG TỐC: Bàn tay chạm vào ô lúa -> chạm vào nút kim cương để chín ngay.
    /// </summary>
    public void PlaySpeedUpPhantom(string plotId = "tutorial_plot_01")
    {
        StopDemo();
        EnsureUI();
        _isDemoRunning = true;
        _currentDemoCo = StartCoroutine(SpeedUpRoutine(plotId));
    }

    /// <summary>
    /// ẢO ẢNH THU HOẠCH: Bàn tay cầm liềm quẹt qua các ô lúa chín làm mẫu.
    /// </summary>
    public void PlayHarvestPhantom(string startPlotId = "tutorial_plot_01", string nextPlotId = "tutorial_plot_02")
    {
        StopDemo();
        EnsureUI();
        _isDemoRunning = true;
        _currentDemoCo = StartCoroutine(HarvestRoutine(startPlotId, nextPlotId));
    }

    public void StopDemo()
    {
        _isDemoRunning = false;
        if (_currentDemoCo != null)
        {
            StopCoroutine(_currentDemoCo);
            _currentDemoCo = null;
        }
        if (_phantomGroup != null)
        {
            _phantomGroup.alpha = 0f;
        }
    }

    // =========================================================================
    // Routines
    // =========================================================================

    private IEnumerator PlantRoutine(Sprite seedSprite, string fromId, string toId)
    {
        while (_isDemoRunning)
        {
            RectTransform fromRt = TutorialManager.GetTargetRect(fromId);
            RectTransform toRt = TutorialManager.GetTargetRect(toId);

            if (fromRt == null || toRt == null)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                continue;
            }

            Vector3 startPos = fromRt.position;
            Vector3 endPos = toRt.position;

            if (_itemImage != null)
            {
                _itemImage.gameObject.SetActive(true);
                if (seedSprite == null && FarmManager.Instance != null)
                {
                    var c = FarmManager.Instance.GetCropById(fromId) ?? FarmManager.Instance.GetCropById(fromId.Replace("seed_", ""));
                    if (c != null) seedSprite = c.icon;
                }
                if (seedSprite != null) _itemImage.sprite = seedSprite;
            }

            // 1. Fade-in tại vị trí khay hạt giống
            _phantomHand.position = startPos + new Vector3(20f, -20f, 0f);
            _phantomItem.position = startPos;
            _phantomHand.localScale = Vector3.one * 1.15f;
            _phantomItem.localScale = Vector3.one * 1.15f;

            yield return FadeTo(0.85f, 0.25f);

            // 2. Nhấn giữ (press)
            float pressDur = 0.15f;
            float elapsed = 0f;
            while (elapsed < pressDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / pressDur;
                _phantomHand.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 0.9f, t);
                _phantomItem.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 0.9f, t);
                yield return null;
            }

            // 3. Kéo lướt sang ô đất
            float dragDur = 0.7f;
            elapsed = 0f;
            while (elapsed < dragDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dragDur));
                Vector3 cur = Vector3.Lerp(startPos, endPos, t);
                cur.y += Mathf.Sin(t * Mathf.PI) * 25f;

                _phantomHand.position = cur + new Vector3(15f, -15f, 0f);
                _phantomItem.position = cur;
                yield return null;
            }

            // 4. Thả hạt xuống ô đất (scale nhẹ nhú lên)
            _phantomItem.position = endPos;
            _phantomHand.position = endPos + new Vector3(15f, -15f, 0f);

            elapsed = 0f;
            float releaseDur = 0.2f;
            while (elapsed < releaseDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / releaseDur;
                _phantomItem.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one * 1.25f, t);
                yield return null;
            }

            // 5. Fade-out
            yield return FadeTo(0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private IEnumerator SpeedUpRoutine(string plotId)
    {
        while (_isDemoRunning)
        {
            RectTransform plotRt = TutorialManager.GetTargetRect(plotId);
            if (plotRt == null)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                continue;
            }

            if (_itemImage != null) _itemImage.gameObject.SetActive(false);

            // 1. Fade-in và chạm vào ô đất
            Vector3 plotPos = plotRt.position;
            _phantomHand.position = plotPos;
            _phantomHand.localScale = Vector3.one * 1.1f;
            yield return FadeTo(0.85f, 0.25f);

            // Tap tap
            yield return ScaleHand(Vector3.one * 1.1f, Vector3.one * 0.85f, 0.12f);
            yield return ScaleHand(Vector3.one * 0.85f, Vector3.one * 1.0f, 0.12f);

            yield return new WaitForSecondsRealtime(0.2f);

            // 2. Tìm nút kim cương nếu popup đã mở, hoặc demo vị trí nút tăng tốc
            RectTransform speedBtn = FindSpeedButton();
            Vector3 targetBtnPos = speedBtn != null ? speedBtn.position : plotPos + new Vector3(0f, 100f, 0f);

            // Lướt sang nút kim cương
            float moveDur = 0.45f;
            float elapsed = 0f;
            Vector3 fromPos = _phantomHand.position;
            while (elapsed < moveDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDur);
                _phantomHand.position = Vector3.Lerp(fromPos, targetBtnPos, t);
                yield return null;
            }

            // Tap vào nút kim cương
            yield return ScaleHand(Vector3.one, Vector3.one * 0.8f, 0.12f);
            yield return ScaleHand(Vector3.one * 0.8f, Vector3.one, 0.12f);

            yield return new WaitForSecondsRealtime(0.25f);

            // 3. Fade-out
            yield return FadeTo(0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.6f);
        }
    }

    private IEnumerator HarvestRoutine(string startPlotId, string nextPlotId)
    {
        while (_isDemoRunning)
        {
            RectTransform p1 = TutorialManager.GetTargetRect(startPlotId);
            RectTransform p2 = TutorialManager.GetTargetRect(nextPlotId);

            if (p1 == null)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                continue;
            }

            Vector3 pos1 = p1.position;
            Vector3 pos2 = p2 != null ? p2.position : pos1 + new Vector3(120f, 0f, 0f);

            // Hiển thị liềm ảo ảnh
            if (_itemImage != null)
            {
                _itemImage.gameObject.SetActive(true);
                if (_sickleSprite != null) _itemImage.sprite = _sickleSprite;
                _phantomItem.sizeDelta = new Vector2(64f, 64f);
            }

            // 1. Fade-in tại ô lúa chín đầu tiên
            _phantomHand.position = pos1 + new Vector3(25f, -20f, 0f);
            _phantomItem.position = pos1;
            yield return FadeTo(0.85f, 0.25f);

            // 2. Quét liềm qua các ô lúa
            float sweepDur = 0.65f;
            float elapsed = 0f;
            while (elapsed < sweepDur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / sweepDur);
                Vector3 cur = Vector3.Lerp(pos1, pos2, t);
                cur.y += Mathf.Sin(t * Mathf.PI) * 18f;

                _phantomHand.position = cur + new Vector3(25f, -20f, 0f);
                _phantomItem.position = cur;
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            // 3. Fade-out
            yield return FadeTo(0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.6f);
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (_phantomGroup == null) yield break;
        float startAlpha = _phantomGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _phantomGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        _phantomGroup.alpha = targetAlpha;
    }

    private IEnumerator ScaleHand(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            _phantomHand.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        _phantomHand.localScale = to;
    }

    private RectTransform FindSpeedButton()
    {
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            string n = b.name.ToLowerInvariant();
            if (n.Contains("speedup") || n.Contains("rutnang") || n.Contains("gem"))
                return b.GetComponent<RectTransform>();
        }
        return null;
    }
}
