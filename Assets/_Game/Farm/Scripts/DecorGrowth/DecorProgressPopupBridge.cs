using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Decor5] Cầu nối popup TIẾN ĐỘ cho đồ trang trí / chuồng / công trình.
///
/// VẤN ĐỀ: `CropProcessPopupUI` chỉ có OpenForPlot / OpenForPen / OpenForHouse và
/// DEV-A KHÔNG được sửa file đó (CONTRACT §0.4). Vì vậy bridge này TỰ DỰNG một popup
/// riêng bằng code, sao lại đúng bố cục 360x84 của CropProcessPopupUI:
///     Txt_CropName · Track_Bar > Progress_Fill + Txt_TimeRemaining · Btn_SpeedUp > Icon_Diamond + Txt_GemCost
/// Tên node giữ y nguyên để DEV-D / Sếp nhìn Hierarchy là thấy quen ngay.
///
/// KHÔNG BAO GIỜ crash khi thiếu art: mọi Resources.Load đều có nhánh fallback vẽ khung
/// bằng Image màu phẳng theo màu studio (burgundy #8E1F3B + đồng vàng #D9A441).
///
/// Canvas riêng "Canvas_DecorProgress", ScreenSpaceOverlay, sortingOrder 310 —
/// thấp hơn popup chính của game nhưng vẫn nằm trên world.
/// </summary>
public static class DecorProgressPopupBridge
{
    // Màu studio (dùng khi không load được sprite khung).
    private const string HexBurgundy = "#8E1F3B";
    private const string HexGold = "#D9A441";
    private const string HexTrack = "#5A1226";

    private const float PanelWidth = 360f;
    private const float PanelHeight = 84f;
    private const int CanvasSortingOrder = 310;

    // Các đường Resources thử lần lượt; không có cái nào thì dùng màu phẳng.
    private static readonly string[] PanelSpriteCandidates =
    {
        "UI/DecorProgress_BG", "UI/popup_progress_bg", "UI/khung_tiendo"
    };
    private static readonly string[] GemSpriteCandidates =
    {
        "UI/kimcuong", "Icons/kimcuong", "UI/icon_diamond"
    };
    private static readonly string[] ButtonSpriteCandidates =
    {
        "UI/btn_gem", "UI/btn_diamond", "UI/btn_green"
    };

    private static Canvas _canvas;
    private static RectTransform _canvasRect;
    private static RectTransform _panel;
    private static TMP_Text _txtName;
    private static TMP_Text _txtTime;
    private static TMP_Text _txtGem;
    private static Image _fill;
    private static RectTransform _fillRt;
    private static Image _blocker;
    private static Button _btnGem;
    private static Driver _driver;

    private static DecorGrowthController _current;

    /// <summary>
    /// Popup đang mở hay không.
    /// [FIX 2026-09-06] KHÔNG dùng `_panel` nữa: `_panel` chỉ được gán bên trong `Build()`,
    /// và `Build()` KHÔNG có nơi nào gọi (đã grep toàn repo — DEAD CODE, xem ghi chú tại khai
    /// báo `Build()` bên dưới) ⇒ `_panel` luôn null ⇒ `IsOpen` từng vĩnh viễn `false`, làm hỏng
    /// mọi guard đóng popup ở DecorGrowthController (dòng 400/486/439 cũ) và
    /// DecorGrowthBootstrap (dòng 577 cũ). Popup THẬT SỰ đang hiển thị qua
    /// `BuildingProcessPopupUI` (xem `OpenFor`/`Close` ngay dưới) nên phải đọc trạng thái
    /// từ đó — `BuildingProcessPopupUI.IsOpen` đã có sẵn (`_root != null && _root.activeSelf`).
    /// </summary>
    public static bool IsOpen => _current != null
        && BuildingProcessPopupUI.Instance != null
        && BuildingProcessPopupUI.Instance.IsOpen;

    /// <summary>Canvas + driver đã được dựng chưa (DEV-D / QA kiểm nhanh).</summary>
    public static bool IsBuilt => _canvas != null && _panel != null && _driver != null && _blocker != null;

    /// <summary>Popup đang mở CHO ĐÚNG vật này hay không (controller dùng để tự đóng popup của mình).</summary>
    public static bool IsOpenFor(DecorGrowthController decor) => IsOpen && _current == decor;

    /// <summary>Vật đang được popup theo dõi (null nếu đóng).</summary>
    public static DecorGrowthController Current => IsOpen ? _current : null;

    // ── API ──────────────────────────────────────────────────────────────────

    public static void OpenFor(DecorGrowthController decor)
    {
        if (decor == null) return;
        if (decor.State != DecorGrowthController.DecorState.Building) return;

        // Tránh 2 popup tiến độ chồng nhau.
        if (CropProcessPopupUI.Instance != null && CropProcessPopupUI.Instance.IsOpen)
            CropProcessPopupUI.Instance.ClosePopup();

        _current = decor;
        BuildingProcessPopupUI.GetOrCreate().Open(decor);
    }

    public static void Close()
    {
        _current = null;
        if (BuildingProcessPopupUI.Instance != null)
            BuildingProcessPopupUI.Instance.Close();
    }

    // ── Dựng UI bằng code ────────────────────────────────────────────────────

    // DEAD CODE — không dùng, giữ để tham chiếu.
    // [FIX 2026-09-06] Không có nơi nào trong repo gọi Build() (đã grep xác nhận). Vì vậy
    // _panel/_canvas/_driver/_blocker bên dưới không bao giờ được gán — bản dựng UI thật
    // đang chạy qua BuildingProcessPopupUI, không phải qua Build() này. KHÔNG xoá hàm này:
    // nó vẫn còn giá trị tham khảo (bố cục 360x84, màu chuẩn burgundy/gold) — chỉ đừng gọi
    // nó ở bất kỳ đâu khác.
    private static void Build()
    {
        if (_panel != null) return;

        var canvasGo = new GameObject("Canvas_DecorProgress");
        Object.DontDestroyOnLoad(canvasGo);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CanvasSortingOrder;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        _driver = canvasGo.AddComponent<Driver>();
        _canvasRect = canvasGo.GetComponent<RectTransform>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts/Baloo2 SDF");

        Color burgundy = Hex(HexBurgundy, new Color(0.557f, 0.122f, 0.231f));
        Color gold = Hex(HexGold, new Color(0.851f, 0.643f, 0.255f));
        Color track = Hex(HexTrack, new Color(0.353f, 0.071f, 0.149f));

        // ── QA A1: LỚP CHẶN full-screen, trong suốt, raycastTarget = true ──
        // Tạo TRƯỚC panel nên nó nằm DƯỚI khung popup nhưng TRÊN mọi thứ khác trong
        // canvas này. Nó ăn hết click UI khi popup mở ⇒ nút kim cương không còn "xuyên"
        // xuống HUD/world. Click vào nó = đóng popup.
        //
        // ⚠ GIỚI HẠN KHÔNG THỂ SỬA TỪ PHÍA TÔI: HouseGrowthController tự poll
        //   Input.GetMouseButtonDown/Up trong Update() của chính nó và KHÔNG kiểm
        //   EventSystem / popup / EditMode. Raycast blocker của uGUI không chặn được
        //   một vòng poll Input thô như vậy. Vì thế khi popup này mở, DecorClickRouter
        //   đã tự tắt toàn bộ input world (xem Decor5Runtime.Update), nhưng NHÀ VILLAGE
        //   vẫn có thể nhận click xuyên qua. Sửa dứt điểm cần 3 dòng guard trong
        //   HouseGrowthController.CheckInputClick() — file đó KHÔNG thuộc phần của tôi.
        RectTransform blockRt = NewRect("Blocker_DecorProgress", _canvasRect, Vector2.zero);
        blockRt.anchorMin = Vector2.zero;
        blockRt.anchorMax = Vector2.one;
        blockRt.offsetMin = Vector2.zero;
        blockRt.offsetMax = Vector2.zero;
        _blocker = blockRt.gameObject.AddComponent<Image>();
        _blocker.color = new Color(0f, 0f, 0f, 0f);   // trong suốt hoàn toàn
        _blocker.raycastTarget = true;
        var blockBtn = blockRt.gameObject.AddComponent<Button>();
        blockBtn.transition = Selectable.Transition.None;
        blockBtn.onClick.AddListener(Close);

        // ── Panel gốc 360x84, pivot đáy giữa để neo lên đầu vật ──
        _panel = NewRect("Panel_DecorProgress", _canvasRect, new Vector2(PanelWidth, PanelHeight));
        _panel.pivot = new Vector2(0.5f, 0f);
        _panel.anchorMin = new Vector2(0.5f, 0.5f);
        _panel.anchorMax = new Vector2(0.5f, 0.5f);

        var bg = _panel.gameObject.AddComponent<Image>();
        // [WP-D1] Ưu tiên sprite chuẩn UIStandardSprites.PanelPaper; sau đó các đường Resources cũ; cuối cùng màu phẳng.
        Sprite bgSprite = UIStandardSprites.PanelPaper ?? LoadFirstSprite(PanelSpriteCandidates);
        if (bgSprite != null)
        {
            bg.sprite = bgSprite;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = burgundy;   // fallback màu phẳng — không crash khi thiếu art
        }

        // ── Txt_CropName ──
        RectTransform nameRt = NewRect("Txt_CropName", _panel, new Vector2(336f, 28f));
        nameRt.anchoredPosition = new Vector2(0f, 62f);
        _txtName = MakeText(nameRt, font, 22f, gold, TextAlignmentOptions.Center);

        // ── Track_Bar ──
        RectTransform trackRt = NewRect("Track_Bar", _panel, new Vector2(244f, 30f));
        trackRt.anchoredPosition = new Vector2(-46f, 28f);
        var trackImg = trackRt.gameObject.AddComponent<Image>();
        // [WP-D1] Máng thanh tiến độ = UIStandardSprites.BarTrack (Sliced); null → màu phẳng như cũ.
        Sprite trackSprite = UIStandardSprites.BarTrack;
        if (trackSprite != null)
        {
            trackImg.sprite = trackSprite;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = Color.white;
        }
        else
        {
            trackImg.color = track;
        }

        // ── QA R4: KHÔNG dùng Image.Type.Filled ──────────────────────────────────
        // Unity Image.OnPopulateMesh mở đầu bằng `if (activeSprite == null) { base...; return; }`
        // ⇒ với sprite null thì fillAmount bị BỎ QUA HOÀN TOÀN và thanh luôn đầy 100%.
        // Ở đây popup dựng bằng màu phẳng (không có sprite), nên tiến độ được điều khiển
        // bằng anchorMax.x của RectTransform — đúng tỷ lệ, không phụ thuộc sprite nào.
        // Track có padding 3px, fill là con của một khung padding để bề rộng % tính đúng.
        RectTransform fillAreaRt = NewRect("Fill_Area", trackRt, Vector2.zero);
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.offsetMin = new Vector2(3f, 3f);
        fillAreaRt.offsetMax = new Vector2(-3f, -3f);

        _fillRt = NewRect("Progress_Fill", fillAreaRt, Vector2.zero);
        _fillRt.anchorMin = Vector2.zero;
        _fillRt.anchorMax = new Vector2(0f, 1f);
        _fillRt.offsetMin = Vector2.zero;
        _fillRt.offsetMax = Vector2.zero;
        _fillRt.pivot = new Vector2(0f, 0.5f);
        _fill = _fillRt.gameObject.AddComponent<Image>();
        // [WP-D1] Ruột = UIStandardSprites.BarFill (Sliced). VẪN giữ cơ chế anchorMax.x (QA R4) — không dùng Filled —
        // nên sprite hay màu phẳng đều chạy đúng tỷ lệ; null → màu vàng đồng như cũ.
        Sprite fillSprite = UIStandardSprites.BarFill;
        if (fillSprite != null)
        {
            _fill.sprite = fillSprite;
            _fill.type = Image.Type.Sliced;
            _fill.color = Color.white;
        }
        else
        {
            _fill.color = gold;
        }
        _fill.raycastTarget = false;

        RectTransform timeRt = NewRect("Txt_TimeRemaining", trackRt, Vector2.zero);
        timeRt.anchorMin = Vector2.zero;
        timeRt.anchorMax = Vector2.one;
        timeRt.offsetMin = Vector2.zero;
        timeRt.offsetMax = Vector2.zero;
        _txtTime = MakeText(timeRt, font, 18f, Color.white, TextAlignmentOptions.Center);

        // ── Btn_SpeedUp ──
        RectTransform btnRt = NewRect("Btn_SpeedUp", _panel, new Vector2(84f, 44f));
        btnRt.anchoredPosition = new Vector2(130f, 28f);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        // [WP-D1] Nền nút kim cương chuẩn = UIStandardSprites.BtnGem; sau đó các đường Resources cũ; cuối cùng màu vàng.
        Sprite btnSprite = UIStandardSprites.BtnGem ?? LoadFirstSprite(ButtonSpriteCandidates);
        if (btnSprite != null)
        {
            btnImg.sprite = btnSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
        }
        else
        {
            btnImg.color = gold;
        }

        _btnGem = btnRt.gameObject.AddComponent<Button>();
        _btnGem.targetGraphic = btnImg;
        _btnGem.onClick.RemoveAllListeners();
        _btnGem.onClick.AddListener(OnGemClick);

        RectTransform gemIconRt = NewRect("Icon_Diamond", btnRt, new Vector2(24f, 24f));
        gemIconRt.anchoredPosition = new Vector2(-24f, 0f);
        var gemImg = gemIconRt.gameObject.AddComponent<Image>();
        // [WP-D1] Icon kim cương chuẩn = UIStandardSprites.IconGem; sau đó các đường Resources cũ; cuối cùng màu phẳng.
        Sprite gemSprite = UIStandardSprites.IconGem ?? LoadFirstSprite(GemSpriteCandidates);
        if (gemSprite != null) { gemImg.sprite = gemSprite; gemImg.preserveAspect = true; }
        else gemImg.color = new Color(0.42f, 0.83f, 0.96f);   // xanh kim cương, fallback
        gemImg.raycastTarget = false;

        RectTransform gemTxtRt = NewRect("Txt_GemCost", btnRt, new Vector2(48f, 28f));
        gemTxtRt.anchoredPosition = new Vector2(12f, 0f);
        _txtGem = MakeText(gemTxtRt, font, 20f, new Color(0.18f, 0.09f, 0.05f), TextAlignmentOptions.Center);
        _txtGem.raycastTarget = false;

        _panel.gameObject.SetActive(false);
        _blocker.gameObject.SetActive(false);
    }

    private static RectTransform NewRect(string name, RectTransform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        if (size != Vector2.zero) rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    private static TMP_Text MakeText(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;          // null → để TMP tự lấy font default
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;   // enableWordWrapping đã Obsolete trong TMP của project
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.text = "";
        return t;
    }

    private static Sprite LoadFirstSprite(string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            Sprite s = Resources.Load<Sprite>(paths[i]);
            if (s != null) return s;
        }
        return null;
    }

    private static Color Hex(string hex, Color fallback)
    {
        Color c;
        return ColorUtility.TryParseHtmlString(hex, out c) ? c : fallback;
    }

    // ── Cập nhật mỗi frame ───────────────────────────────────────────────────

    private static void Tick()
    {
        if (!IsOpen) return;

        if (_current == null || _current.State != DecorGrowthController.DecorState.Building)
        {
            Close();
            return;
        }

        Refresh();
        UpdatePosition();
    }

    private static void Refresh()
    {
        if (_current == null) return;

        if (_txtName != null) _txtName.text = _current.DisplayName.ToUpper();

        if (_txtTime != null)
        {
            float rem = _current.RemainingSeconds;
            int m = Mathf.FloorToInt(rem / 60f);
            int s = Mathf.FloorToInt(rem % 60f);
            _txtTime.text = $"{m:00}:{s:00}";
        }

        int cost = _current.SpeedUpGemCost;
        if (_txtGem != null) _txtGem.text = cost.ToString();
        SetFill(_current.Progress);

        // Khoá nút khi không còn gì để mua (còn <0.5s) hoặc chưa có ví tiền.
        if (_btnGem != null)
            _btnGem.interactable = cost > 0 && FarmEconomyManager.Instance != null;
    }

    /// <summary>
    /// Đặt tiến độ 0..1 bằng anchorMax.x — bề rộng thật của thanh vàng = p * bề rộng track.
    /// KHÔNG dùng fillAmount (xem QA R4).
    /// </summary>
    private static void SetFill(float p)
    {
        if (_fillRt == null) return;
        float k = Mathf.Clamp01(p);
        _fillRt.anchorMin = new Vector2(0f, 0f);
        _fillRt.anchorMax = new Vector2(k, 1f);
        _fillRt.offsetMin = Vector2.zero;
        _fillRt.offsetMax = Vector2.zero;
    }

    /// <summary>Tỷ lệ bề rộng thanh tiến độ hiện tại (0..1) — QA/DEV-D kiểm nhanh.</summary>
    public static float FillRatio => _fillRt != null ? _fillRt.anchorMax.x : 0f;

    private static void UpdatePosition()
    {
        if (_current == null || _panel == null || _canvasRect == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = _current.transform.position + Vector3.up * (_current.VisualBounds.size.y + 40f);
        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z < 0f) return;

        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out local))
            _panel.anchoredPosition = local;
    }

    private static void OnGemClick()
    {
        if (_current == null) { Close(); return; }
        if (_current.State != DecorGrowthController.DecorState.Building) { Close(); return; }

        // Thất bại (thiếu gem / thiếu manager) → controller đã ShowHint, popup ở lại để thử tiếp.
        if (_current.TrySpeedUpWithGem()) Close();
    }

    /// <summary>
    /// Driver per-frame. Nested private class trong CHÍNH class bridge để file này vẫn chỉ có
    /// MỘT top-level type (CONTRACT §7: 1 class / 1 file) — không sinh file thứ 6 ngoài §10.
    /// </summary>
    private class Driver : MonoBehaviour
    {
        private void LateUpdate()
        {
            Tick();
        }
    }
}
