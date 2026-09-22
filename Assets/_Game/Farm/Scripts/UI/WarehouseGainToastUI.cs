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
    [SerializeField] private Sprite panelSprite;       // khung gỗ chính (storage_main_frame)
    [SerializeField] private Sprite iconSprite;        // icon nhà kho 3D (storage_barn_house)
    [SerializeField] private Sprite headerBadgeSprite; // badge gỗ trên có icon hòm (storage_header_badge)
    [SerializeField] private Sprite crateBadgeSprite;  // huy hiệu tròn xanh có hòm gỗ (storage_crate_icon)
    [SerializeField] private Sprite barTrackSprite;    // rãnh bar (storage_bar_track)
    [SerializeField] private Sprite barFillSprite;     // fill bar (storage_bar_fill)

    [Header("Layout")]
    [Tooltip("Vị trí pill so với mép TRÊN-GIỮA canvas")]
    [SerializeField] private Vector2 anchoredPos = new Vector2(120f, -60f);
    [SerializeField] private Vector2 panelSize   = new Vector2(340f, 110f);

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
        SpawnPlusText("STORAGE FULL!", new Color(0.86f, 0.22f, 0.16f));
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
            _txtCount.text  = $"{used}/{cap}";
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

#if UNITY_EDITOR
        if (panelSprite == null)
            panelSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_main_frame.png");
        if (iconSprite == null)
            iconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_barn_house.png");
        if (headerBadgeSprite == null)
            headerBadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_header_badge.png");
        if (crateBadgeSprite == null)
            crateBadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_crate_icon.png");
        if (barTrackSprite == null)
            barTrackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_bar_track.png");
        if (barFillSprite == null)
            barFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Farm/Sprites/StorageHUD/storage_bar_fill.png");
#endif

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
        if (panelSprite != null) { _imgPanel.sprite = panelSprite; _imgPanel.type = Image.Type.Simple; _imgPanel.preserveAspect = true; _imgPanel.color = Color.white; }
        else { _imgPanel.color = new Color(0.96f, 0.90f, 0.78f, 0.96f); }
        _imgPanel.raycastTarget = false;

        _cg = _panel.GetComponent<CanvasGroup>();
        if (_cg == null) _cg = _panel.gameObject.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = false;
        _cg.interactable = false;

        // A. Icon nhà kho 3D (Img_BarnHouse hoặc Img_Icon)
        var barnTr = _panel.Find("Img_BarnHouse") ?? _panel.Find("Img_Icon");
        if (barnTr == null) barnTr = FindOrCreate(_panel, "Img_BarnHouse");
        _imgIcon = barnTr.GetComponent<Image>();
        if (_imgIcon == null) _imgIcon = barnTr.gameObject.AddComponent<Image>();
        if (iconSprite != null) _imgIcon.sprite = iconSprite;
        _imgIcon.preserveAspect = true;
        _imgIcon.color = Color.white;
        _imgIcon.raycastTarget = false;
        var bRt = (RectTransform)barnTr;
        bRt.anchorMin = bRt.anchorMax = new Vector2(0f, 0.5f);
        bRt.pivot = new Vector2(0.5f, 0.5f);
        bRt.anchoredPosition = new Vector2(-15f, 6f);
        bRt.sizeDelta = new Vector2(145f, 145f);

        // B. Header Badge ("Storage")
        var headerTr = _panel.Find("Badge_Header");
        if (headerTr == null)
        {
            headerTr = FindOrCreate(_panel, "Badge_Header");
            var imgH = headerTr.gameObject.AddComponent<Image>();
            imgH.sprite = headerBadgeSprite;
            imgH.preserveAspect = true;
            imgH.color = Color.white;
            imgH.raycastTarget = false;
            var hRt = (RectTransform)headerTr;
            hRt.anchorMin = hRt.anchorMax = new Vector2(0f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = new Vector2(135f, -16f);
            hRt.sizeDelta = new Vector2(175f, 40f);

            var txtHTr = FindOrCreate(hRt, "Txt_Title");
            var txtH = txtHTr.gameObject.AddComponent<TextMeshProUGUI>();
            var viF = GetViFont();
            if (viF != null) txtH.font = viF;
            txtH.text = "Storage";
            txtH.fontSize = 20;
            txtH.fontStyle = FontStyles.Bold;
            txtH.alignment = TextAlignmentOptions.Center;
            txtH.color = Color.white;
            txtH.outlineColor = new Color32(0x38, 0x1F, 0x0C, 0xFF);
            txtH.outlineWidth = 0.22f;
            txtH.raycastTarget = false;
            var thRt = (RectTransform)txtHTr;
            thRt.anchorMin = Vector2.zero;
            thRt.anchorMax = Vector2.one;
            thRt.offsetMin = new Vector2(36f, 0f);
            thRt.offsetMax = new Vector2(-8f, 0f);
        }

        // C. Progress Bar Container
        var pcTr = _panel.Find("Progress_Container") as RectTransform;
        if (pcTr == null)
        {
            pcTr = (RectTransform)FindOrCreate(_panel, "Progress_Container");
            pcTr.anchorMin = pcTr.anchorMax = new Vector2(0f, 1f);
            pcTr.pivot = new Vector2(0f, 1f);
            pcTr.anchoredPosition = new Vector2(135f, -64f);
            pcTr.sizeDelta = new Vector2(240f, 36f);
        }

        // Bar track (Rãnh capsule màu nâu lõm sâu)
        var trackTr = pcTr.Find("Bar_Track") ?? _panel.Find("Bar_Track");
        if (trackTr == null) trackTr = FindOrCreate(pcTr, "Bar_Track");
        _imgTrack = trackTr.GetComponent<Image>();
        if (_imgTrack == null) _imgTrack = trackTr.gameObject.AddComponent<Image>();
        if (barTrackSprite == null)
        {
#if UNITY_EDITOR
            barTrackSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/progress_track.png")
                          ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_track_bar.png");
#endif
        }
        if (barTrackSprite != null) { _imgTrack.sprite = barTrackSprite; _imgTrack.type = Image.Type.Sliced; _imgTrack.color = new Color32(0x38, 0x1F, 0x0C, 0xFF); }
        else { _imgTrack.color = new Color32(0x38, 0x1F, 0x0C, 0xFF); }
        _imgTrack.raycastTarget = false;
        var tRt = (RectTransform)trackTr;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(16f, 2f);
        tRt.offsetMax = new Vector2(-4f, -2f);

        // Bar fill (Thanh fill trắng có tô màu xanh dynamic)
        var fillTr = trackTr.Find("Bar_Fill");
        if (fillTr == null) fillTr = FindOrCreate(trackTr as RectTransform, "Bar_Fill");
        _imgFill = fillTr.GetComponent<Image>();
        if (_imgFill == null) _imgFill = fillTr.gameObject.AddComponent<Image>();
        if (barFillSprite == null)
        {
#if UNITY_EDITOR
            barFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assetsgame/popup/ui_svg_perfect/generated_sprites/progress_fill.png")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Export_Train_UI_Package/Sprites/progress_fill_green.png");
#endif
        }
        if (barFillSprite != null) _imgFill.sprite = barFillSprite;
        _imgFill.type = Image.Type.Filled;
        _imgFill.fillMethod = Image.FillMethod.Horizontal;
        _imgFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _imgFill.color = new Color32(0x4C, 0xCE, 0x15, 0xFF); // Xanh lá tươi rực rỡ
        _imgFill.raycastTarget = false;
        var fRt = (RectTransform)fillTr;
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(3f, 3f);
        fRt.offsetMax = new Vector2(-3f, -3f);

        // Circular Crate Badge
        var crateBadgeTr = pcTr.Find("Img_CrateBadge");
        if (crateBadgeTr == null && crateBadgeSprite != null)
        {
            crateBadgeTr = FindOrCreate(pcTr, "Img_CrateBadge");
            var imgCrate = crateBadgeTr.gameObject.AddComponent<Image>();
            imgCrate.sprite = crateBadgeSprite;
            imgCrate.preserveAspect = true;
            imgCrate.color = Color.white;
            imgCrate.raycastTarget = false;
            var cRt = (RectTransform)crateBadgeTr;
            cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(16f, 0f);
            cRt.sizeDelta = new Vector2(46f, 46f);
        }

        // Text 18/25
        var txtTr = trackTr.Find("Txt_Count");
        if (txtTr == null) txtTr = FindOrCreate(trackTr as RectTransform, "Txt_Count");
        _txtCount = txtTr.GetComponent<TextMeshProUGUI>();
        if (_txtCount == null) _txtCount = txtTr.gameObject.AddComponent<TextMeshProUGUI>();
        var viFont = GetViFont();
        if (viFont != null) _txtCount.font = viFont;
        _txtCount.fontSize = 19;
        _txtCount.fontStyle = FontStyles.Bold;
        _txtCount.alignment = TextAlignmentOptions.Center;
        _txtCount.color = Color.white;
        _txtCount.outlineColor = new Color32(0x1A, 0x49, 0x06, 0xFF);
        _txtCount.outlineWidth = 0.22f;
        _txtCount.raycastTarget = false;
        var tcRt = (RectTransform)txtTr;
        tcRt.anchorMin = Vector2.zero;
        tcRt.anchorMax = Vector2.one;
        tcRt.offsetMin = new Vector2(14f, 0f);
        tcRt.offsetMax = Vector2.zero;

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
