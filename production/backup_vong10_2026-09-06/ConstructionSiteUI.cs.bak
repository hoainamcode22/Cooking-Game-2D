using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI NỔI TRÊN ĐẦU CÔNG TRƯỜNG (N3 — khớp ảnh 2 / video f_045).
/// Dựng 100 % bằng code trên một **World Space Canvas**, từ trên xuống:
///   1. TÊN CÔNG TRÌNH  — trắng, IN HOA, viền đậm (TMP outline)
///   2. Thanh thời gian — nền tối bo góc + icon đồng hồ + `52Sek` / `1M59Sek`
///   3. Nút rush xanh lá — icon tiền + số, bấm được
///
/// ĐƠN VỊ: canvas để localScale = 1 nên **1 "pixel" UI = 1 world unit**, giống hệt cách
/// prefab Placement_Ghost làm (root scale 100 × canvas scale 0.01 = 1). Nhờ vậy mọi con số
/// dưới đây so sánh trực tiếp được với `PlacementManager.CELL = 100`.
///
/// KHÔNG BỊ CÔNG TRÌNH KHÁC CHE: canvas đẩy lên sorting layer cao nhất có trong project
/// ("Foreground") với sortingOrder rất lớn — công trình chạy ở "Objects"/"CongTrinh" nên
/// không bao giờ vẽ đè lên được.
/// </summary>
public class ConstructionSiteUI : MonoBehaviour
{
    // ── Kích thước (world unit) ──────────────────────────────────────────────
    private const float CanvasMinWidth = 470f;
    private const float CanvasHeight   = 300f;
    private const float GapAboveRoof   = 26f;

    private Canvas          _canvas;
    private RectTransform   _rect;
    private Image           _namePlate;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _costText;
    private Image           _costIcon;
    private Image           _rushBg;
    private Button          _rushButton;
    private TextMeshProUGUI _toastText;
    private Coroutine       _toastRoutine;
    private Camera          _cam;

    /// <summary>Bộ ô art — giữ lại vì icon tiền đổi sprite lúc chạy (xu ⇄ kim cương).</summary>
    private ConstructionArtKit _kit;

    /// <summary>Màu gốc của nền nút rush (art thật = trắng, placeholder = xanh nhận dạng).</summary>
    private Color _rushBaseColor = Color.white;

    /// <summary>Nhãn tên ô của icon tiền — phải đổi chữ khi chuyển xu ⇄ kim cương.</summary>
    private TMP_Text _costIconLabel;

    /// <summary>Bấm nút rush. ConstructionSite gán khi dựng.</summary>
    public System.Action OnRushClicked;

    // ─────────────────────────────────────────────────────────────────────────

    public static ConstructionSiteUI Build(Transform parent, float worldW, float worldH,
                                           string sortingLayer, int sortingOrder,
                                           ConstructionArtKit artKit = null)
    {
        var go = new GameObject("Construction_UI", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        // Đặt vào layer "UI" (5) giống prefab Placement_Ghost — đã chắc chắn được camera
        // chính render và EventSystem bắt click, khỏi phải đoán culling mask.
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) go.layer = uiLayer;

        var ui = go.AddComponent<ConstructionSiteUI>();
        ui._kit = artKit;
        ui.Construct(worldW, worldH, sortingLayer, sortingOrder);
        return ui;
    }

    private void Construct(float worldW, float worldH, string sortingLayer, int sortingOrder)
    {
        _rect = (RectTransform)transform;

        // ⚠ THỨ TỰ QUAN TRỌNG: phải AddComponent<Canvas> và chuyển sang WorldSpace TRƯỚC,
        // rồi mới đặt kích thước. Canvas vừa thêm mặc định là ScreenSpaceOverlay và nó
        // "drive" RectTransform về đúng cỡ màn hình — đặt sizeDelta trước sẽ bị nuốt mất.
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode       = RenderMode.WorldSpace;
        _canvas.sortingLayerName = sortingLayer;
        _canvas.sortingOrder     = sortingOrder;
        _cam = Camera.main;
        _canvas.worldCamera = _cam;

        gameObject.AddComponent<GraphicRaycaster>();

        // pivot dưới-giữa: mép dưới canvas nằm ngay trên nóc công trình,
        // nội dung nở LÊN TRÊN nên nhà cao hay thấp UI cũng không đè vào mái.
        _rect.pivot         = new Vector2(0.5f, 0f);
        _rect.sizeDelta     = new Vector2(Mathf.Max(CanvasMinWidth, worldW), CanvasHeight);
        _rect.localPosition = new Vector3(0f, worldH * 0.5f + GapAboveRoof, 0f);
        _rect.localRotation = Quaternion.identity;
        _rect.localScale    = Vector3.one;

        // ── 1. NỀN TÊN + TÊN CÔNG TRÌNH ──────────────────────────────────────
        // Nền dựng TRƯỚC chữ: UGUI vẽ theo thứ tự con, con đầu nằm dưới cùng.
        bool namePlateIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.NamePlateBg,
            ConstructionSpriteFactory.Panel(96, 64, 26), out Sprite plateSpr, out Color plateCol);

        _namePlate = NewImage(_rect, "Nen_TenCongTrinh", plateSpr, plateCol);
        Place(_namePlate.rectTransform, new Vector2(0f, 226f),
              new Vector2(Mathf.Max(320f, _rect.sizeDelta.x * 0.86f), 76f));
        _namePlate.type = Image.Type.Sliced;
        _namePlate.raycastTarget = false;

        // Ô còn TRỐNG thì theo tooltip của kit là "chỉ có chữ, không nền" → ẩn hẳn,
        // giao diện mặc định giữ nguyên như vòng 1. Chỉ hiện tấm tím nhận dạng khi
        // Edric bật chế độ dựng nền (nhãn tên ô / ép màu placeholder) để căn vị trí.
        bool showEmptyPlate = ConstructionArtKit.WantLabels(_kit)
                           || (_kit != null && _kit.forcePlaceholderColors);
        _namePlate.gameObject.SetActive(namePlateIsArt || showEmptyPlate);

        _nameText = NewText(_rect, "Text_TenCongTrinh", "", 52f, Color.white);
        Place(_nameText.rectTransform, new Vector2(0f, 226f), new Vector2(_rect.sizeDelta.x + 120f, 68f));
        _nameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        AddOutline(_nameText, new Color(0.09f, 0.06f, 0.03f, 1f), 0.32f);

        ConstructionSiteVisuals.AttachSlotLabel(_namePlate.rectTransform,
            ConstructionArtKit.Slot.NamePlateBg, _kit);

        // ── 2. THANH THỜI GIAN ───────────────────────────────────────────────
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.TimerBarBg,
            ConstructionSpriteFactory.Panel(96, 64, 26), out Sprite barSpr, out Color barCol);

        var bar = NewImage(_rect, "Bar_ThoiGian", barSpr, barCol);
        Place(bar.rectTransform, new Vector2(0f, 140f), new Vector2(252f, 70f));
        bar.type = Image.Type.Sliced;
        bar.raycastTarget = false;

        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.ClockIcon,
            ConstructionSpriteFactory.ClockIcon(), out Sprite clockSpr, out Color clockCol);

        var clock = NewImage(bar.rectTransform, "Icon_DongHo", clockSpr, clockCol);
        PlaceCenter(clock.rectTransform, new Vector2(-84f, 0f), new Vector2(48f, 48f));
        clock.raycastTarget = false;

        ConstructionSiteVisuals.AttachSlotLabel(bar.rectTransform,
            ConstructionArtKit.Slot.TimerBarBg, _kit);
        ConstructionSiteVisuals.AttachSlotLabel(clock.rectTransform,
            ConstructionArtKit.Slot.ClockIcon, _kit);

        _timeText = NewText(bar.rectTransform, "Text_ThoiGian", "", 40f, Color.white);
        PlaceCenter(_timeText.rectTransform, new Vector2(22f, 0f), new Vector2(180f, 56f));
        _timeText.fontStyle = FontStyles.Bold;
        AddOutline(_timeText, new Color(0f, 0f, 0f, 0.85f), 0.18f);

        // ── 3. NÚT RUSH ──────────────────────────────────────────────────────
        var btnGo = new GameObject("Btn_Rush", typeof(RectTransform));
        btnGo.transform.SetParent(_rect, false);
        Place((RectTransform)btnGo.transform, new Vector2(0f, 46f), new Vector2(196f, 80f));

        // Nút xanh thủ tục ĐÃ tự có màu xanh trong texture. Nếu tô thêm màu nhận dạng
        // C_RushBtn nữa thì thành xanh đè xanh, tối sì → placeholder giữ trắng, chỉ khi
        // Edric gán art thật (art thường là hình trắng/xám) mới cần tint.
        bool rushIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.RushButtonBg,
            ConstructionSpriteFactory.GreenButton(160, 72, 26), out Sprite rushSpr, out Color rushCol);
        _rushBaseColor = rushIsArt ? rushCol : Color.white;

        _rushBg = btnGo.AddComponent<Image>();
        _rushBg.sprite = rushSpr;
        _rushBg.color  = _rushBaseColor;
        _rushBg.type   = Image.Type.Sliced;
        _rushBg.raycastTarget = true;

        _rushButton = btnGo.AddComponent<Button>();
        _rushButton.targetGraphic = _rushBg;
        _rushButton.transition    = Selectable.Transition.ColorTint;
        _rushButton.onClick.AddListener(HandleRushClicked);

        // Mặc định là XU; SetTimeAndCost đổi sang kim cương nếu rushCurrency = Gems.
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.CoinIcon,
            ConstructionSpriteFactory.CoinIcon(), out Sprite coinSpr, out Color coinCol);

        _costIcon = NewImage(_rushBg.rectTransform, "Icon_Tien", coinSpr, coinCol);
        PlaceCenter(_costIcon.rectTransform, new Vector2(-52f, 0f), new Vector2(46f, 46f));
        _costIcon.raycastTarget = false;

        ConstructionSiteVisuals.AttachSlotLabel(_rushBg.rectTransform,
            ConstructionArtKit.Slot.RushButtonBg, _kit);
        _costIconLabel = ConstructionSiteVisuals.AttachSlotLabel(_costIcon.rectTransform,
            ConstructionArtKit.Slot.CoinIcon, _kit);

        _costText = NewText(_rushBg.rectTransform, "Text_Gia", "", 40f, Color.white);
        PlaceCenter(_costText.rectTransform, new Vector2(22f, 2f), new Vector2(120f, 56f));
        _costText.fontStyle = FontStyles.Bold;
        AddOutline(_costText, new Color(0.06f, 0.20f, 0.02f, 1f), 0.24f);

        // ── 4. DÒNG BÁO LỖI (ẩn sẵn) ─────────────────────────────────────────
        _toastText = NewText(_rect, "Text_ThongBao", "", 34f, new Color(1f, 0.42f, 0.35f));
        Place(_toastText.rectTransform, new Vector2(0f, -44f), new Vector2(520f, 52f));
        _toastText.fontStyle = FontStyles.Bold;
        AddOutline(_toastText, new Color(0.12f, 0f, 0f, 1f), 0.26f);
        _toastText.gameObject.SetActive(false);

        // Đồng bộ layer cho mọi con: `new GameObject()` luôn ra layer 0 bất kể cha là gì.
        int layer = gameObject.layer;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = layer;

        StartCoroutine(PopIn());
    }

    // ── Cập nhật nội dung ────────────────────────────────────────────────────

    public void SetBuildingName(string displayName)
    {
        if (_nameText != null)
            _nameText.text = string.IsNullOrEmpty(displayName) ? "CÔNG TRÌNH" : displayName;
    }

    /// <summary>
    /// Cập nhật đồng hồ + giá rush. Gọi mỗi khi con số GIÂY đổi, không gọi mỗi frame
    /// (mỗi lần đổi text là TMP dựng lại mesh — 60 lần/giây cho mỗi công trường là phí).
    /// </summary>
    public void SetTimeAndCost(float remainingSeconds, int rushCost, bool affordable, bool isGem)
    {
        if (_timeText != null)
            _timeText.text = FormatTime(remainingSeconds);

        if (_costText != null)
            _costText.text = rushCost.ToString();

        if (_costIcon != null)
        {
            // Hai ô art KHÁC NHAU cho hai loại tiền — đổi cả sprite lẫn màu nhận dạng.
            ConstructionArtKit.Slot iconSlot = isGem
                ? ConstructionArtKit.Slot.GemIcon
                : ConstructionArtKit.Slot.CoinIcon;

            ConstructionArtKit.ResolveSafe(_kit, iconSlot,
                isGem ? ConstructionSpriteFactory.GemIcon() : ConstructionSpriteFactory.CoinIcon(),
                out Sprite iconSpr, out Color iconCol);

            _costIcon.sprite = iconSpr;
            _costIcon.color  = iconCol;

            if (_costIconLabel != null)
                _costIconLabel.text = ConstructionArtKit.LabelOf(iconSlot);
        }

        // Không đủ tiền → làm xám nhẹ, NHƯNG vẫn bấm được để hiện lời nhắc rõ ràng
        // (bấm vào nút chết không phản hồi gì là trải nghiệm tệ nhất).
        // Nhân vào MÀU GỐC chứ không gán cứng trắng, nếu không art nút rush sẽ mất tint.
        if (_rushBg != null)
            _rushBg.color = affordable
                ? _rushBaseColor
                : new Color(_rushBaseColor.r * 0.62f, _rushBaseColor.g * 0.66f,
                            _rushBaseColor.b * 0.60f, _rushBaseColor.a);
    }

    /// <summary>
    /// Định dạng Township: dưới 60 giây → `52Sek`; từ 60 giây → `1M59Sek`.
    /// Từ 1 giờ trở lên thêm bậc giờ (`2H05M`) — video không có mốc này nhưng
    /// buildTime dài vẫn phải đọc được chứ không hiện "125M03Sek".
    /// </summary>
    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));

        if (total >= 3600)
        {
            int hh = total / 3600;
            int mm = (total % 3600) / 60;
            return $"{hh}H{mm:00}M";
        }
        if (total >= 60)
        {
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm}M{ss:00}Sek";
        }
        return $"{total}Sek";
    }

    public void ShowMessage(string message)
    {
        if (_toastText == null) return;

        _toastText.text = message;
        _toastText.gameObject.SetActive(true);

        if (_toastRoutine != null) StopCoroutine(_toastRoutine);
        _toastRoutine = StartCoroutine(FadeToast());
    }

    public void HideAll()
    {
        gameObject.SetActive(false);
    }

    // ── Vòng đời ─────────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        // LUÔN HƯỚNG CAMERA. Game 2D nên camera gần như không xoay, nhưng nếu công trường
        // bị parent vào vật đã xoay (hoặc sau này có hiệu ứng lắc camera) thì nhãn vẫn thẳng.
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        transform.rotation = _cam.transform.rotation;

        if (_canvas != null && _canvas.worldCamera == null)
            _canvas.worldCamera = _cam;
    }

    private void HandleRushClicked()
    {
        OnRushClicked?.Invoke();
    }

    private IEnumerator PopIn()
    {
        Vector3 target = _rect.localScale;
        float elapsed = 0f;
        const float dur = 0.22f;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            // BackOut: nảy nhẹ quá mốc rồi về — cùng đường cong DEV-1 dùng cho hàng nút.
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            _rect.localScale = Vector3.LerpUnclamped(target * 0.35f, target, e);
            yield return null;
        }

        _rect.localScale = target;
    }

    private IEnumerator FadeToast()
    {
        yield return new WaitForSecondsRealtime(1.6f);

        float elapsed = 0f;
        const float dur = 0.45f;
        Color baseColor = _toastText.color;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(elapsed / dur);
            _toastText.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }

        _toastText.color = baseColor;
        _toastText.gameObject.SetActive(false);
        _toastRoutine = null;
    }

    // ── Tiện ích dựng UI ─────────────────────────────────────────────────────

    private static TextMeshProUGUI NewText(Transform parent, string name, string content,
                                           float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (t.font == null && TMP_Settings.defaultFontAsset != null)
            t.font = TMP_Settings.defaultFontAsset;

        t.text          = content;
        t.fontSize      = fontSize;
        t.color         = color;
        t.alignment     = TextAlignmentOptions.Center;
        t.overflowMode  = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color  = color;
        return img;
    }

    private static void Place(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        // Neo vào ĐÁY-GIỮA canvas: pivot canvas cũng là đáy-giữa nên toạ độ Y đọc thẳng
        // là "cao bao nhiêu so với nóc công trình".
        rt.anchorMin        = new Vector2(0.5f, 0f);
        rt.anchorMax        = new Vector2(0.5f, 0f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        rt.localScale       = Vector3.one;
    }

    /// <summary>
    /// Neo vào TÂM cha — dùng cho con nằm TRONG thanh thời gian / nút rush.
    /// (Nếu dùng nhầm <see cref="Place"/> ở đây thì icon sẽ tụt xuống mép dưới của thanh,
    /// vì Place neo theo đáy canvas chứ không theo tâm cha.)
    /// </summary>
    private static void PlaceCenter(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        rt.localScale       = Vector3.one;
    }

    /// <summary>
    /// Viền chữ đậm kiểu Township. Cùng cách làm với `AddTextOutline` trong
    /// LevelUpPopupTownshipTool.cs: bật keyword outline trên material INSTANCE của TMP
    /// rồi nới mesh padding, nếu không viền sẽ bị cắt cụt ở rìa glyph.
    /// </summary>
    private static void AddOutline(TextMeshProUGUI tmp, Color color, float width)
    {
        if (tmp == null) return;

        Material mat = tmp.fontMaterial;      // TMP tự tạo instance riêng
        if (mat != null) mat.EnableKeyword(ShaderUtilities.Keyword_Outline);

        tmp.outlineColor = color;
        tmp.outlineWidth = width;
        tmp.UpdateMeshPadding();
    }
}
