using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn chuyển scene. NÂNG CẤP HÌNH ẢNH 2026-08-29 (Sếp yêu cầu):
/// trước đây là 2 tấm panel TRẮNG TRƠN + chữ "Loading..." tiếng Anh — thô so với game.
///
/// Bản mới, cùng API cũ (LoadScene/UnloadScene giữ nguyên chữ ký — KHÔNG nơi gọi nào phải sửa):
///   · CloudWipe = 2 CÁNH CỬA GỖ (WoodBoard_Frame 9-slice, tông nâu ấm) khép vào giữa,
///     có nhún nhẹ khi chạm nhau. BoardDrop = tấm bảng gỗ rơi xuống nảy (giữ ease cũ).
///   · Giữa màn: ĐĨA PHỞ lắc lư như đang được bưng đi + "Đang tải" với dấu chấm chạy
///     + THANH TIẾN ĐỘ đồng vàng (#D9A441) đọc từ AsyncOperation.progress — người chơi
///     thấy máy đang làm việc thật chứ không đứng hình.
///   · Dòng MẸO ngẫu nhiên ở dưới — chờ load cũng học được gì đó.
///   · Asset nạp từ Resources/UI_ChuyenCanh (bản sao guid riêng, ~230 KB — không đụng
///     file gốc của art). Thiếu asset thì TỰ RƠI VỀ panel màu trơn như bản cũ, không lỗi.
/// Chữ dùng font Việt thống nhất Resources/Fonts/Baloo2 SDF.
/// </summary>
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

    // ── phần thêm 2026-08-29 ──
    private CanvasGroup _centerGroup;          // đĩa + chữ + thanh tiến độ + mẹo
    private RectTransform _dishRect;
    private Image _dishImage;
    private Image _barFill;
    private RectTransform _barFrame;
    private TextMeshProUGUI _tipText;
    private Sprite _sprBoard, _sprDish;
    private TMP_FontAsset _fontVi;

    private bool _isTransitioning;
    private float _transitionDuration = 0.45f;

    private static readonly string[] Tips =
    {
        "Mẹo: nguyên liệu nấu được món mới chuyển sang bếp được — hạt giống thì không.",
        "Mẹo: món nấu đúng CẢ nguyên liệu lẫn hương vị mới đạt 100 điểm.",
        "Mẹo: bí đỏ và dưa hấu trồng lâu hơn nhưng bán đắt hơn hẳn.",
        "Mẹo: đơn hàng dân làng trả thêm vàng so với bán thẳng ở chợ.",
        "Mẹo: gia vị mua ở chợ — muối, tiêu, chanh, ớt — đều mang vào bếp được.",
        "Mẹo: cây chín sẽ đung đưa nhè nhẹ, nhìn ruộng là biết cây nào thu được.",
        "Mẹo: giá ở chợ đắt hơn tự nấu — tiền công nấu nằm trong đó.",
        "Mẹo: mở sổ công thức trong bếp để xem món nào sắp mở khoá.",
    };

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
        // Asset trang trí — thiếu cái nào thì phần đó tự ẩn/về màu trơn, không bao giờ lỗi.
        _sprBoard = Resources.Load<Sprite>("UI_ChuyenCanh/WoodBoard_Frame");
        _sprDish  = Resources.Load<Sprite>("UI_ChuyenCanh/MonAn_ChuyenCanh");
        _fontVi   = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");

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

        // ── cụm giữa màn: đĩa + chữ + thanh tiến độ + mẹo ─────────────────────
        GameObject center = new GameObject("Center_Group", typeof(RectTransform), typeof(CanvasGroup));
        center.transform.SetParent(canvasObj.transform, false);
        _centerGroup = center.GetComponent<CanvasGroup>();
        RectTransform crt = (RectTransform)center.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(700, 500);

        GameObject dishObj = new GameObject("Img_Dish", typeof(RectTransform), typeof(Image));
        dishObj.transform.SetParent(center.transform, false);
        _dishImage = dishObj.GetComponent<Image>();
        _dishImage.sprite = _sprDish;
        _dishImage.preserveAspect = true;
        _dishImage.raycastTarget = false;
        _dishImage.enabled = _sprDish != null;
        _dishRect = (RectTransform)dishObj.transform;
        _dishRect.anchorMin = _dishRect.anchorMax = new Vector2(0.5f, 0.5f);
        _dishRect.anchoredPosition = new Vector2(0, 90);
        _dishRect.sizeDelta = new Vector2(190, 190);

        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(center.transform, false);
        _loadingText = textObj.AddComponent<TextMeshProUGUI>();
        if (_fontVi != null) _loadingText.font = _fontVi;
        _loadingText.text = "Đang tải";
        _loadingText.fontSize = 52;
        _loadingText.alignment = TextAlignmentOptions.Center;
        _loadingText.color = new Color(0.95f, 0.88f, 0.72f);
        _loadingText.fontStyle = FontStyles.Bold;
        _loadingText.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, -45);
        textRect.sizeDelta = new Vector2(600, 70);

        // Thanh tiến độ: khung gỗ tối + ruột đồng vàng #D9A441 (fill từ trái sang)
        GameObject frame = new GameObject("Bar_Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(center.transform, false);
        Image frameImg = frame.GetComponent<Image>();
        frameImg.color = new Color(0.20f, 0.11f, 0.05f, 0.9f);
        frameImg.raycastTarget = false;
        _barFrame = (RectTransform)frame.transform;
        _barFrame.anchorMin = _barFrame.anchorMax = new Vector2(0.5f, 0.5f);
        _barFrame.anchoredPosition = new Vector2(0, -105);
        _barFrame.sizeDelta = new Vector2(420, 22);

        GameObject fill = new GameObject("Bar_Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(frame.transform, false);
        _barFill = fill.GetComponent<Image>();
        _barFill.color = new Color(0.85f, 0.64f, 0.25f); // đồng vàng #D9A441
        _barFill.raycastTarget = false;
        RectTransform fr = (RectTransform)fill.transform;
        fr.anchorMin = new Vector2(0, 0);
        fr.anchorMax = new Vector2(0, 1);
        fr.pivot = new Vector2(0, 0.5f);
        fr.anchoredPosition = new Vector2(3, 0);
        fr.sizeDelta = new Vector2(0, -6);

        GameObject tipObj = new GameObject("Txt_Tip");
        tipObj.transform.SetParent(center.transform, false);
        _tipText = tipObj.AddComponent<TextMeshProUGUI>();
        if (_fontVi != null) _tipText.font = _fontVi;
        _tipText.fontSize = 24;
        _tipText.alignment = TextAlignmentOptions.Center;
        _tipText.color = new Color(0.85f, 0.76f, 0.60f, 0.95f);
        _tipText.raycastTarget = false;
        _tipText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform tipRect = tipObj.GetComponent<RectTransform>();
        tipRect.anchorMin = tipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tipRect.anchoredPosition = new Vector2(0, -170);
        tipRect.sizeDelta = new Vector2(860, 70);

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
        _centerGroup.alpha = 0f;                 // cụm giữa chỉ hiện khi cửa đã khép
        _tipText.text = Tips[UnityEngine.Random.Range(0, Tips.Length)];
        if (_barFill != null) _barFill.rectTransform.sizeDelta = new Vector2(0, -6);

        yield return StartCoroutine(AnimateIn(type));

        AsyncOperation op = loadAction.Invoke();

        float shown = 0f;   // tiến độ hiển thị — đuổi theo tiến độ thật cho mượt
        float dotT = 0f; int dots = 0;
        while (op != null && !op.isDone)
        {
            float dt = Time.unscaledDeltaTime;

            // hiện dần cụm giữa
            if (_centerGroup.alpha < 1f)
                _centerGroup.alpha = Mathf.MoveTowards(_centerGroup.alpha, 1f, dt * 5f);

            // đĩa lắc lư như đang được bưng
            float tt = Time.unscaledTime;
            if (_dishRect != null)
            {
                _dishRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(tt * 3.2f) * 9f);
                float s = 1f + Mathf.Sin(tt * 2.1f) * 0.04f;
                _dishRect.localScale = new Vector3(s, s, 1f);
            }

            // dấu chấm chạy: Đang tải → Đang tải. → .. → ...
            dotT += dt;
            if (dotT >= 0.35f)
            {
                dotT = 0f; dots = (dots + 1) % 4;
                _loadingText.text = "Đang tải" + new string('.', dots);
            }

            // thanh tiến độ (LoadSceneAsync dừng ở 0.9 rồi nhảy xong — chia lại cho 0..1)
            float real = Mathf.Clamp01(op.progress / 0.9f);
            shown = Mathf.MoveTowards(shown, real, dt * 1.5f);
            if (_barFill != null && _barFrame != null)
                _barFill.rectTransform.sizeDelta =
                    new Vector2((_barFrame.sizeDelta.x - 6f) * shown, -6f);

            yield return null;
        }

        // đầy thanh + nghỉ một nhịp ngắn cho mắt kịp thấy "xong"
        if (_barFill != null && _barFrame != null)
            _barFill.rectTransform.sizeDelta = new Vector2(_barFrame.sizeDelta.x - 6f, -6f);
        yield return new WaitForSecondsRealtime(0.12f);

        _centerGroup.alpha = 0f;
        if (_dishRect != null) { _dishRect.localRotation = Quaternion.identity; _dishRect.localScale = Vector3.one; }
        _loadingText.text = "Đang tải";

        yield return StartCoroutine(AnimateOut(type));

        _transitionCanvas.gameObject.SetActive(false);
        _isTransitioning = false;
    }

    private void SetupPanelsForType(TransitionType type)
    {
        // Ván gỗ 9-slice cho cả hai kiểu; thiếu sprite thì về màu trơn như bản cũ.
        ApplyWood(_panel1Image);
        ApplyWood(_panel2Image);

        if (type == TransitionType.CloudWipe)
        {
            // Cánh cửa trái — nới 24px cho hai cánh chớm đè lên nhau, không hở khe sáng
            _panel1.anchorMin = new Vector2(0, 0);
            _panel1.anchorMax = new Vector2(0.5f, 1);
            _panel1.sizeDelta = new Vector2(24, 0);
            _panel1.anchoredPosition = new Vector2(-980, 0);
            _panel1.gameObject.SetActive(true);

            // Cánh cửa phải
            _panel2.anchorMin = new Vector2(0.5f, 0);
            _panel2.anchorMax = new Vector2(1, 1);
            _panel2.sizeDelta = new Vector2(24, 0);
            _panel2.anchoredPosition = new Vector2(980, 0);
            _panel2.gameObject.SetActive(true);
        }
        else if (type == TransitionType.BoardDrop)
        {
            // Tấm bảng gỗ phủ kín, rơi từ trên xuống
            _panel1.anchorMin = new Vector2(0, 0);
            _panel1.anchorMax = new Vector2(1, 1);
            _panel1.sizeDelta = Vector2.zero;
            _panel1.anchoredPosition = new Vector2(0, 1080);
            _panel1.gameObject.SetActive(true);

            _panel2.gameObject.SetActive(false);
        }
    }

    private void ApplyWood(Image img)
    {
        if (_sprBoard != null)
        {
            img.sprite = _sprBoard;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.82f, 0.66f, 0.47f); // nhuộm nâu ấm lên vân gỗ
        }
        else
        {
            img.sprite = null;
            img.color = new Color(0.55f, 0.27f, 0.07f); // fallback: nâu trơn như bản cũ
        }
    }

    private IEnumerator AnimateIn(TransitionType type)
    {
        float t = 0;
        while (t < _transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / _transitionDuration);

            if (type == TransitionType.CloudWipe)
            {
                // khép cửa có "sập nhẹ" ở cuối (overshoot nhỏ rồi ngồi yên)
                float p = EaseOutBackSmall(progress);
                _panel1.anchoredPosition = Vector2.LerpUnclamped(new Vector2(-980, 0), Vector2.zero, p);
                _panel2.anchoredPosition = Vector2.LerpUnclamped(new Vector2(980, 0), Vector2.zero, p);
            }
            else if (type == TransitionType.BoardDrop)
            {
                float bounceProgress = EaseOutBounce(progress);
                _panel1.anchoredPosition = Vector2.Lerp(new Vector2(0, 1080), Vector2.zero, bounceProgress);
            }

            yield return null;
        }
        _panel1.anchoredPosition = Vector2.zero;
        if (type == TransitionType.CloudWipe) _panel2.anchoredPosition = Vector2.zero;
    }

    private IEnumerator AnimateOut(TransitionType type)
    {
        float t = 0;
        while (t < _transitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / _transitionDuration);

            if (type == TransitionType.CloudWipe)
            {
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                _panel1.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(-980, 0), smoothProgress);
                _panel2.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(980, 0), smoothProgress);
            }
            else if (type == TransitionType.BoardDrop)
            {
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                _panel1.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(0, 1080), smoothProgress);
            }

            yield return null;
        }
    }

    /// <summary>EaseOutBack dịu (overshoot ~4%) — cửa gỗ khép vào hơi nhún rồi đứng im.</summary>
    private float EaseOutBackSmall(float x)
    {
        const float c1 = 0.7f;
        const float c3 = c1 + 1f;
        float f = x - 1f;
        return 1f + c3 * f * f * f + c1 * f * f;
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
