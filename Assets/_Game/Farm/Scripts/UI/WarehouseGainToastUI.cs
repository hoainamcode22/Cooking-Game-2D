using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh kho kiểu Township (video tham khảo 2026-08-26): khi nhận BẤT KỲ vật phẩm nào
/// (thu hoạch, chuồng, chợ, tàu, thưởng nhiệm vụ — mọi nguồn đi qua FarmInventoryManager.AddItem),
/// một pill [icon nhà kho | fill bar | 25/30] trượt hiện ra mép trên màn hình,
/// bar nảy nhẹ + text "+N" bay lên, rồi tự ẩn sau 2.5s không có gì mới.
/// Số hiển thị = UsedSlots/SlotCapacity (slot THEO LOẠI — duyệt 2026-08-26, phương án A).
/// Kho đầy (OnAddRejectedByCapacity) → pill flash đỏ + "KHO ĐẦY!".
/// Tự build hierarchy runtime nếu chưa được tool setup — không bắt buộc prefab.
/// </summary>
public class WarehouseGainToastUI : MonoBehaviour
{
    public static WarehouseGainToastUI Instance { get; private set; }

    public RectTransform PanelRect
    {
        get
        {
            EnsureBuilt();
            return _panel;
        }
    }

    [Header("Wiring (Setup tool gán — thiếu thì tự tìm/tự vẽ)")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private Sprite panelSprite;    // pill nền (popup_panel_paper)
    [SerializeField] private Sprite iconSprite;     // icon nhà kho (lấy từ HUD nếu trống)
    [SerializeField] private Sprite barTrackSprite; // rãnh bar (progress_track_bar)
    [SerializeField] private Sprite barFillSprite;  // fill bar (progress_fill_green)

    [Header("Layout")]
    [Tooltip("Vị trí pill so với mép TRÊN-GIỮA canvas")]
    [SerializeField] private Vector2 anchoredPos = new Vector2(150f, -130f);
    [SerializeField] private Vector2 panelSize   = new Vector2(250f, 64f);

    [Header("Timing")]
    [SerializeField] private float showDuration = 0.28f;
    [SerializeField] private float idleBeforeHide = 2.5f;

    // ─── Runtime refs (build 1 lần) ───────────────────────────────
    private RectTransform _panel;
    private Image  _imgPanel, _imgIcon, _imgTrack, _imgFill;
    private TMP_Text _txtCount;
    private CanvasGroup _cg;
    private float _hideAt;
    private float _shownFill;
    private int _currentDisplayUsed = -1;
    private Coroutine _showRoutine, _pulseRoutine;
    private bool _visible;

    /// <summary>Lưới an toàn: scene chưa được tool setup thì tự sinh toast (sprite fallback màu phẳng).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<WarehouseGainToastUI>(FindObjectsInactive.Include) != null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("WarehouseGainToast", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.AddComponent<WarehouseGainToastUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        FarmInventoryManager.OnItemAddedFx        += HandleItemAdded;
        FarmInventoryManager.OnAddRejectedByCapacity += HandleRejected;
    }

    private void OnDisable()
    {
        FarmInventoryManager.OnItemAddedFx        -= HandleItemAdded;
        FarmInventoryManager.OnAddRejectedByCapacity -= HandleRejected;
    }

    private void Update()
    {
        if (_visible && Time.unscaledTime >= _hideAt)
            HideToast();
    }

    // ─── Event handlers & Progressive Harvest Increments ─────────

    public void OnHarvestItemArrived(Sprite icon = null)
    {
        if (!EnsureBuilt()) return;
        ShowToast();

        var inv = FarmInventoryManager.Instance;
        int cap = inv != null ? Mathf.Max(1, inv.SlotCapacity) : 50;
        int actualUsed = inv != null ? inv.UsedSlots : 0;

        if (_currentDisplayUsed < 0)
            _currentDisplayUsed = Mathf.Max(0, actualUsed - 1);

        _currentDisplayUsed++;
        if (_currentDisplayUsed > actualUsed)
            _currentDisplayUsed = actualUsed;

        UpdateDisplayValues(_currentDisplayUsed, cap, animate: true);
        SpawnPlusText("+1", new Color(0.30f, 0.62f, 0.12f));
        JuicyPulseFX.Play(_panel, 1.18f, 0.22f);
    }

    private void HandleItemAdded(string itemId, int amount)
    {
        if (!EnsureBuilt()) return;
        ShowToast();
        RefreshNumbers(animate: true);
        SpawnPlusText($"+{amount}", new Color(0.30f, 0.62f, 0.12f));
        JuicyPulseFX.Play(_panel, 1.18f, 0.22f);
    }

    private void HandleRejected(string itemId)
    {
        if (!EnsureBuilt()) return;
        ShowToast();
        RefreshNumbers(animate: false);
        SpawnPlusText("KHO ĐẦY!", new Color(0.86f, 0.22f, 0.16f));
        StartCoroutine(RoutineFlashRed());
    }

    // ─── Show / hide ──────────────────────────────────────────────

    private void ShowToast()
    {
        _hideAt = Time.unscaledTime + idleBeforeHide;
        if (_visible) return;
        _visible = true;

        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(RoutineShow(true));
    }

    private void HideToast()
    {
        if (!_visible) return;
        _visible = false;

        if (_showRoutine != null) StopCoroutine(_showRoutine);
        _showRoutine = StartCoroutine(RoutineShow(false));
    }

    private IEnumerator RoutineShow(bool show)
    {
        _panel.gameObject.SetActive(true);
        float t = 0f;
        float from      = _cg.alpha;
        float to        = show ? 1f : 0f;
        Vector2 posFrom = _panel.anchoredPosition;
        Vector2 posShown  = anchoredPos;
        Vector2 posHidden = anchoredPos + new Vector2(0f, 60f);
        Vector2 posTo   = show ? posShown : posHidden;

        while (t < showDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / showDuration);
            k = show ? 1f - (1f - k) * (1f - k) : k * k; // ease-out khi hiện, ease-in khi ẩn
            _cg.alpha = Mathf.Lerp(from, to, k);
            _panel.anchoredPosition = Vector2.Lerp(posFrom, posTo, k);

            if (show) // nảy overshoot nhẹ kiểu Township
                _panel.localScale = Vector3.one * (0.85f + 0.15f * Mathf.Sin(k * Mathf.PI * 0.5f) + 0.08f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        _cg.alpha = to;
        _panel.anchoredPosition = posTo;
        _panel.localScale = Vector3.one;
        if (!show) _panel.gameObject.SetActive(false);
    }

    // ─── Numbers / fill ───────────────────────────────────────────

    public void UpdateDisplayValues(int used, int cap, bool animate)
    {
        _currentDisplayUsed = used;
        if (_txtCount != null)
        {
            _txtCount.text  = $"Kho: {used}/{cap}";
            _txtCount.color = used >= cap ? new Color(0.96f, 0.13f, 0.18f) : Color.white;
        }

        float target = Mathf.Clamp01((float)used / Mathf.Max(1, cap));
        if (_imgFill != null)
        {
            if (target >= 1f) _imgFill.color = new Color(0.96f, 0.13f, 0.18f); // Đỏ khi đầy
            else if (target >= 0.8f) _imgFill.color = new Color(0.98f, 0.55f, 0.09f); // Cam khi gần đầy
            else _imgFill.color = new Color(0.32f, 0.77f, 0.10f); // Xanh lá chuẩn
        }

        if (!animate || !_panel.gameObject.activeInHierarchy)
        {
            _shownFill = target;
            if (_imgFill != null) _imgFill.fillAmount = target;
        }
        else
        {
            StartCoroutine(RoutineFillTo(target));
        }
    }

    private void RefreshNumbers(bool animate)
    {
        var inv = FarmInventoryManager.Instance;
        if (inv == null) return;

        int used = inv.UsedSlots;
        int cap  = Mathf.Max(1, inv.SlotCapacity);
        UpdateDisplayValues(used, cap, animate);
    }

    private IEnumerator RoutineFillTo(float target)
    {
        float from = _shownFill;
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _shownFill = Mathf.Lerp(from, target, Mathf.Clamp01(t / dur));
            if (_imgFill != null) _imgFill.fillAmount = _shownFill;
            yield return null;
        }
        _shownFill = target;
        if (_imgFill != null) _imgFill.fillAmount = target;
    }

    // ─── Juice ────────────────────────────────────────────────────

    private void Pulse()
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        if (_panel.gameObject.activeInHierarchy)
            _pulseRoutine = StartCoroutine(RoutinePulse());
    }

    private IEnumerator RoutinePulse()
    {
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / dur) * Mathf.PI);
            _panel.localScale = Vector3.one * (1f + 0.09f * k);
            yield return null;
        }
        _panel.localScale = Vector3.one;
    }

    private IEnumerator RoutineFlashRed()
    {
        if (_imgPanel == null) yield break;
        Color baseCol = _imgPanel.color;
        Color red     = new Color(1f, 0.72f, 0.68f, baseCol.a);
        float t = 0f, dur = 0.5f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _imgPanel.color = Color.Lerp(red, baseCol, Mathf.Clamp01(t / dur));
            yield return null;
        }
        _imgPanel.color = baseCol;
    }

    private void SpawnPlusText(string text, Color color)
    {
        if (_panel == null || !_panel.gameObject.activeInHierarchy) return;

        var go = new GameObject("Txt_Plus", typeof(RectTransform));
        go.transform.SetParent(_panel, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        var plusFont = GetViFont();
        if (plusFont != null) txt.font = plusFont;
        txt.text = text;
        txt.fontSize = 26;
        txt.fontStyle = FontStyles.Bold;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;
        txt.raycastTarget = false;

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.72f, 1f);
        rt.anchoredPosition = new Vector2(0f, 6f);
        rt.sizeDelta = new Vector2(160f, 34f);

        StartCoroutine(RoutinePlusText(txt, rt));
    }

    private IEnumerator RoutinePlusText(TMP_Text txt, RectTransform rt)
    {
        Vector2 from = rt.anchoredPosition;
        float t = 0f, dur = 0.85f;
        while (t < dur && txt != null)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.anchoredPosition = from + new Vector2(0f, 42f * (1f - (1f - k) * (1f - k)));
            txt.alpha = k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f;
            yield return null;
        }
        if (txt != null) Destroy(txt.gameObject);
    }

    // ─── Build hierarchy (idempotent — tool gọi trong Editor, runtime tự gọi khi cần) ───

    public bool EnsureBuilt()
    {
        if (_panel != null) return true;

        if (canvas == null)
        {
            var spawner = HarvestFeedbackSpawner.Instance;
            if (spawner != null && spawner.WarehouseTarget != null)
                canvas = spawner.WarehouseTarget.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return false;
        }

        if (iconSprite == null)
        {
            var spawner = HarvestFeedbackSpawner.Instance;
            if (spawner != null && spawner.WarehouseTarget != null)
            {
                var img = spawner.WarehouseTarget.GetComponent<Image>();
                if (img != null) iconSprite = img.sprite;
            }
#if UNITY_EDITOR
            if (iconSprite == null)
                iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/bocaycoitrangtri/ICON_HUB/icon_warehouse_v2_1786984374562-removebg-preview.png");
            if (panelSprite == null)
                panelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/popup_panel_paper.png");
            if (barTrackSprite == null)
                barTrackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_track_bar.png");
            if (barFillSprite == null)
                barFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_fill_green.png");
#endif
        }

        // Panel pill
        var panelTr = transform.Find("Panel_WarehouseToast") as RectTransform;
        if (panelTr == null)
        {
            var go = new GameObject("Panel_WarehouseToast", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            panelTr = (RectTransform)go.transform;
        }
        _panel = panelTr;
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 1f);
        _panel.pivot = new Vector2(0.5f, 1f);
        _panel.anchoredPosition = anchoredPos;
        _panel.sizeDelta = panelSize;

        _imgPanel = _panel.GetComponent<Image>();
        if (_imgPanel == null) _imgPanel = _panel.gameObject.AddComponent<Image>();
        if (panelSprite != null) { _imgPanel.sprite = panelSprite; _imgPanel.type = Image.Type.Sliced; _imgPanel.color = Color.white; }
        else { _imgPanel.color = new Color(0.96f, 0.90f, 0.78f, 0.96f); }
        _imgPanel.raycastTarget = false;

        _cg = _panel.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = _panel.gameObject.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = false;
        _cg.interactable = false;

        // Icon nhà kho — lồi trái
        var iconTr = FindOrCreate(_panel, "Img_Icon");
        _imgIcon = iconTr.GetComponent<Image>();
        if (_imgIcon == null) _imgIcon = iconTr.gameObject.AddComponent<Image>();
        _imgIcon.sprite = iconSprite;
        _imgIcon.enabled = iconSprite != null;
        _imgIcon.preserveAspect = true;
        _imgIcon.raycastTarget = false;
        var iRt = (RectTransform)iconTr;
        iRt.anchorMin = iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = new Vector2(30f, 2f);
        iRt.sizeDelta = new Vector2(54f, 54f);

        // Bar track
        var trackTr = FindOrCreate(_panel, "Bar_Track");
        _imgTrack = trackTr.GetComponent<Image>();
        if (_imgTrack == null) _imgTrack = trackTr.gameObject.AddComponent<Image>();
        if (barTrackSprite != null) { _imgTrack.sprite = barTrackSprite; _imgTrack.type = Image.Type.Sliced; _imgTrack.color = Color.white; }
        else { _imgTrack.color = new Color(0.42f, 0.27f, 0.13f, 1f); }
        _imgTrack.raycastTarget = false;
        var tRt = (RectTransform)trackTr;
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(1f, 0.5f);
        tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.offsetMin = new Vector2(64f, -14f);
        tRt.offsetMax = new Vector2(-14f, 14f);

        // Bar fill
        var fillTr = FindOrCreate(trackTr as RectTransform, "Bar_Fill");
        _imgFill = fillTr.GetComponent<Image>();
        if (_imgFill == null) _imgFill = fillTr.gameObject.AddComponent<Image>();
        if (barFillSprite != null) { _imgFill.sprite = barFillSprite; _imgFill.color = Color.white; }
        else { _imgFill.color = new Color(0.45f, 0.78f, 0.22f, 1f); }
        _imgFill.type = Image.Type.Filled;
        _imgFill.fillMethod = Image.FillMethod.Horizontal;
        _imgFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _imgFill.raycastTarget = false;
        var fRt = (RectTransform)fillTr;
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(3f, 3f);
        fRt.offsetMax = new Vector2(-3f, -3f);

        // Text 25/30
        var txtTr = FindOrCreate(trackTr as RectTransform, "Txt_Count");
        _txtCount = txtTr.GetComponent<TextMeshProUGUI>();
        if (_txtCount == null) _txtCount = txtTr.gameObject.AddComponent<TextMeshProUGUI>();
        var viFont = GetViFont();
        if (viFont != null) _txtCount.font = viFont;
        _txtCount.fontSize = 20;
        _txtCount.fontStyle = FontStyles.Bold;
        _txtCount.alignment = TextAlignmentOptions.Center;
        _txtCount.raycastTarget = false;
        var cRt = (RectTransform)txtTr;
        cRt.anchorMin = Vector2.zero;
        cRt.anchorMax = Vector2.one;
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;

        RefreshNumbers(animate: false);
        _cg.alpha = 0f;
        _panel.gameObject.SetActive(false);
        _visible = false;
        return true;
    }

    // Font mặc định của TMP thiếu dấu tiếng Việt (Ầ, Đ...) — mượn font từ text có sẵn trên HUD.
    private static TMP_FontAsset _viFont;

    private TMP_FontAsset GetViFont()
    {
        if (_viFont != null) return _viFont;
        if (canvas == null) return null;
        foreach (var txt in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (txt != null && txt != _txtCount && txt.font != null)
            {
                _viFont = txt.font;
                break;
            }
        }
        return _viFont;
    }

    private static Transform FindOrCreate(RectTransform parent, string name)
    {
        var tr = parent.Find(name);
        if (tr == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            tr = go.transform;
        }
        return tr;
    }
}
