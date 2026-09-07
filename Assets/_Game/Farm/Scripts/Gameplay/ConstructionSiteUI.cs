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
/// dưới đây so sánh trực tiếp được với CỠ Ô THẬT: 🟢 V10 một ô = IsoGrid.CellWidth x
/// IsoGrid.CellHeight = 300 x 150 world (KHÔNG còn lưới vuông 100). worldW/worldH mà
/// ConstructionSite truyền vào giờ là IsoGrid.FootprintWorldSize(gridSize).
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
        // V11 — CỐ Ý KHÔNG ĐỔI Ô NÀY. Hai lý do đo được:
        //  1. Kit chưa gán ô NamePlateBg ⇒ ConstructionArtKit.Resolve trả về MÀU NHẬN DẠNG
        //     C_NamePlate = TÍM (0.55,0.35,0.85, α 0.85). Gắn art thật vào nhánh dự phòng
        //     là art bị nhuộm tím, xấu hơn hiện tại.
        //  2. Tấm nền này ĐANG BỊ ẨN (SetActive ở dưới: namePlateIsArt || showEmptyPlate,
        //     cả hai đều false khi kit trống) ⇒ đổi sprite dự phòng KHÔNG thay đổi gì trên
        //     màn. Bật nó lên là đổi THIẾT KẾ (tên đang là chữ trắng viền đậm, đặt lên
        //     ruy băng vàng thì tương phản tụt) — Sếp không yêu cầu, không tự làm.
        // Muốn có nền tên bằng art thật thì gán ô NamePlateBg trong ArtKit — xem CẦN SẾP.
        bool namePlateIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.NamePlateBg,
            ConstructionSpriteFactory.Panel(96, 64, 26), out Sprite plateSpr, out Color plateCol);

        _namePlate = NewImage(_rect, "Nen_TenCongTrinh", plateSpr, plateCol);
        Place(_namePlate.rectTransform, new Vector2(0f, 226f),
              new Vector2(Mathf.Max(320f, _rect.sizeDelta.x * 0.86f), 76f));
        _namePlate.type = Image.Type.Sliced;
        CanhBaoNenKhong9Slice("namePlateBg", plateSpr, Mathf.Max(320f, _rect.sizeDelta.x * 0.86f), 76f);
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
        // V11 — ART THẬT: timer_box_dark (96x48, border 16/16/16/16, CÓ bản copy trong
        // Assets/Resources/UI/Standard ⇒ build KHÔNG null). Đây đúng là "hộp đồng hồ"
        // của bộ Township mà popup Cài đặt / popup Tàu đang dùng ⇒ thanh giờ ở công trường
        // hết lạc tông so với phần còn lại của game.
        //
        // 🔴 MÀU PHẢI TỰ QUYẾT, KHÔNG DÙNG `barCol`: khi kit chưa gán ô TimerBarBg thì
        // Resolve trả về MÀU NHẬN DẠNG C_TimerBar = (0.15,0.15,0.18, α 0.85). Nhân nó vào
        // art đã bake nâu-đen (45,30,18) ⇒ (7,4,3) tức ĐEN ĐẶC, mất hẳn vành nâu
        // (110,75,40) của art. Đúng cùng một cái bẫy đã làm card đặt công trình ra màu bùn.
        // ĐÚNG CÁCH: art thật ⇒ TRẮNG; chỉ tôn trọng màu của kit khi kit CÓ gán ô.
        // (Cùng khuôn với `_rushBaseColor = rushIsArt ? rushCol : Color.white` ở nút rush.)
        bool timerIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.TimerBarBg,
            UIStandardSprites.RowDark ?? ConstructionSpriteFactory.Panel(96, 64, 26),
            out Sprite barSpr, out Color barCol);
        Color barMau = timerIsArt ? barCol : Color.white;

        // 🔴 V11 — SỬA CHỮ ĐÈ (đúng lỗi trong ảnh Sếp gửi: số giờ đè lên "…Sek").
        //
        // ĐO BẢN CŨ, gốc toạ độ là TÂM THANH (bar 252 rộng ⇒ x ∈ [−126, +126]):
        //   icon đồng hồ  48 tại x = −84  ⇒ x ∈ [−108, −60]
        //   ô chữ giờ    180 tại x = +22  ⇒ x ∈ [ −68, +112]
        //   ⇒ HAI Ô CHỒNG NHAU 8 px (−68 nằm bên trong [−108,−60]).
        // Chữ căn GIỮA ô + overflowMode = Overflow nên chuỗi dài tràn ĐỀU HAI BÊN: chuỗi
        // xấu nhất FormatTime sinh ra là "59M59Sek" (8 ký tự ≈ 176 px ở cỡ 40) tràn 8 px
        // mỗi bên ⇒ mép trái chữ tới −76, LỌT VÀO icon. Đó là chữ đè.
        //
        // BẢN MỚI (bar 288 rộng ⇒ x ∈ [−144, +144]):
        //   icon đồng hồ  44 tại x = −96  ⇒ x ∈ [−118, −74]   (lề trái 26)
        //   ô chữ giờ    168 tại x = +30  ⇒ x ∈ [ −54, +114]   (lề phải 30)
        //   ⇒ HỞ 20 px giữa icon và ô chữ. Chuỗi xấu nhất 176 px tràn 4 px mỗi bên ⇒ mép
        //     trái chữ tới −58, vẫn còn 16 px cách icon. HẾT ĐÈ ở mọi chuỗi FormatTime.
        //
        // Thanh chỉ nới 252 → 288 (+36), vẫn nhỏ hơn CanvasMinWidth 470 nên bảng KHÔNG
        // to bè thêm; phần còn lại là xếp lại icon/chữ chứ không phải phình bảng.
        var bar = NewImage(_rect, "Bar_ThoiGian", barSpr, barMau);
        Place(bar.rectTransform, new Vector2(0f, 140f), new Vector2(288f, 68f));
        bar.type = Image.Type.Sliced;
        bar.raycastTarget = false;
        CanhBaoNenKhong9Slice("timerBarBg", barSpr, 288f, 68f);

        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.ClockIcon,
            ConstructionSpriteFactory.ClockIcon(), out Sprite clockSpr, out Color clockCol);

        var clock = NewImage(bar.rectTransform, "Icon_DongHo", clockSpr, clockCol);
        PlaceCenter(clock.rectTransform, new Vector2(-96f, 0f), new Vector2(44f, 44f));
        clock.raycastTarget = false;

        ConstructionSiteVisuals.AttachSlotLabel(bar.rectTransform,
            ConstructionArtKit.Slot.TimerBarBg, _kit);
        ConstructionSiteVisuals.AttachSlotLabel(clock.rectTransform,
            ConstructionArtKit.Slot.ClockIcon, _kit);

        _timeText = NewText(bar.rectTransform, "Text_ThoiGian", "", 40f, Color.white);
        PlaceCenter(_timeText.rectTransform, new Vector2(30f, 0f), new Vector2(168f, 54f));
        _timeText.fontStyle = FontStyles.Bold;
        AddOutline(_timeText, new Color(0f, 0f, 0f, 0.85f), 0.18f);

        // ── 3. NÚT RUSH ──────────────────────────────────────────────────────
        // 🔴 V11 — CÙNG LOẠI LỖI ĐÈ, ĐO ĐƯỢC 9 px, ở ngay nút rush bên dưới.
        // BẢN CŨ (nút 196 ⇒ x ∈ [−98, +98]):
        //   icon tiền 46 tại x = −52 ⇒ x ∈ [−75, −29]
        //   ô chữ giá 120 tại x = +22 ⇒ x ∈ [−38, +82]   ⇒ CHỒNG 9 px.
        // BẢN MỚI (nút 224 ⇒ x ∈ [−112, +112]):
        //   icon tiền 44 tại x = −66 ⇒ x ∈ [−88, −44]   (lề trái 24)
        //   ô chữ giá 112 tại x = +30 ⇒ x ∈ [−26, +86]   (lề phải 26)
        //   ⇒ HỞ 18 px. Giá 4 chữ số ("1240" ≈ 88 px ở cỡ 40) căn giữa +30 ⇒ x ∈ [−14, +74],
        //     nằm gọn trong ô, không tràn về phía icon.
        var btnGo = new GameObject("Btn_Rush", typeof(RectTransform));
        btnGo.transform.SetParent(_rect, false);
        Place((RectTransform)btnGo.transform, new Vector2(0f, 46f), new Vector2(224f, 84f));

        // Nút xanh thủ tục ĐÃ tự có màu xanh trong texture. Nếu tô thêm màu nhận dạng
        // C_RushBtn nữa thì thành xanh đè xanh, tối sì → placeholder giữ trắng, chỉ khi
        // Edric gán art thật (art thường là hình trắng/xám) mới cần tint.
        // V11 — ART THẬT ĐI TRƯỚC: btn_green_3d (96x48, border 16/16/16/16, CÓ bản copy
        // trong Assets/Resources/UI/Standard ⇒ build KHÔNG null). Nút thủ tục GreenButton()
        // tụt xuống hàng dự phòng khi Load trả null.
        //
        // VÌ SAO btn_green_3d MÀ KHÔNG PHẢI btn_big_green: nút rush cao 84. btn_big_green
        // có border 48 ⇒ 48 + 48 = 96 > 84 ⇒ hai vành DỌC chồng lên nhau, Unity bóp góc bo
        // và art ra méo. btn_green_3d border 16 ⇒ 32 ≤ 84, dư chỗ. ĐÃ KIỂM CẢ HAI TRỤC cho
        // mọi sprite 9-slice dùng ở vòng này — luật là tổng border mỗi trục phải ≤ cạnh đó.
        //
        // btn_green_3d ĐÃ BAKE xanh (108,191,46) nên _rushBaseColor phải là TRẮNG — y hệt
        // lý do bên card đặt công trình. `rushIsArt` chỉ đúng khi ArtKit của Sếp gán ô riêng.
        Sprite rushMacDinh = UIStandardSprites.BtnGreen3D
                          ?? ConstructionSpriteFactory.GreenButton(160, 72, 26);
        bool rushIsArt = ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.RushButtonBg,
            rushMacDinh, out Sprite rushSpr, out Color rushCol);
        _rushBaseColor = rushIsArt ? rushCol : Color.white;

        _rushBg = btnGo.AddComponent<Image>();
        _rushBg.sprite = rushSpr;
        _rushBg.color  = _rushBaseColor;
        _rushBg.type   = Image.Type.Sliced;
        _rushBg.raycastTarget = true;
        CanhBaoNenKhong9Slice("rushButtonBg", rushSpr, 224f, 84f);

        _rushButton = btnGo.AddComponent<Button>();
        _rushButton.targetGraphic = _rushBg;
        _rushButton.transition    = Selectable.Transition.ColorTint;
        _rushButton.onClick.AddListener(HandleRushClicked);

        // Mặc định là XU; SetTimeAndCost đổi sang kim cương nếu rushCurrency = Gems.
        ConstructionArtKit.ResolveSafe(_kit, ConstructionArtKit.Slot.CoinIcon,
            ConstructionSpriteFactory.CoinIcon(), out Sprite coinSpr, out Color coinCol);

        _costIcon = NewImage(_rushBg.rectTransform, "Icon_Tien", coinSpr, coinCol);
        PlaceCenter(_costIcon.rectTransform, new Vector2(-66f, 0f), new Vector2(44f, 44f));
        _costIcon.raycastTarget = false;

        ConstructionSiteVisuals.AttachSlotLabel(_rushBg.rectTransform,
            ConstructionArtKit.Slot.RushButtonBg, _kit);
        _costIconLabel = ConstructionSiteVisuals.AttachSlotLabel(_costIcon.rectTransform,
            ConstructionArtKit.Slot.CoinIcon, _kit);

        _costText = NewText(_rushBg.rectTransform, "Text_Gia", "", 40f, Color.white);
        PlaceCenter(_costText.rectTransform, new Vector2(30f, 2f), new Vector2(112f, 54f));
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
    /// 🔴 V11 — LƯỚI CHẶN CHO MỘT LỖI ĐÃ XẢY RA HAI LẦN TRONG DỰ ÁN NÀY.
    ///
    /// LUẬT: một sprite có spriteBorder 0/0/0/0 mà bị vẽ bằng `Image.Type.Sliced` thì
    /// UGUI KHÔNG CHIA VÀNH được, nó tụt về KÉO GIÃN PHẲNG cả ảnh. Ảnh vuông kéo vào ô
    /// dẹt là méo hẳn, mà KHÔNG có lỗi nào được in ra — chỉ nhìn mới biết.
    ///
    /// HAI LẦN ĐÃ XẢY RA (đo được, KHÔNG phải phỏng đoán):
    ///   1. Vòng 10: ô `priceBarBg` bị gán btn_CloseRanking.png (1179x211, border 0)
    ///      → Dev V đã ghi lại. Ô đó nay đã trống nên hết lỗi.
    ///   2. HIỆN TẠI, trong ConstructionArtKit.asset:
    ///      · namePlateBg  = Sprite_clock_icon.png (94x89,   border 0) ← ICON ĐỒNG HỒ!
    ///      · timerBarBg   = Sprite_clock_icon.png (94x89,   border 0) ← CÙNG icon đó!
    ///      · rushButtonBg = vang-removebg-preview_0 (256x256, border 0) ← ĐỐNG TIỀN VÀNG!
    ///      Ba ô này đang bị kéo giãn thành nền bảng ⇒ nền tên là icon đồng hồ dài 404 px,
    ///      nút rush là đống tiền bị bóp còn 1/3 chiều cao. KHÔNG SỬA ĐƯỢC TỪ CODE —
    ///      .asset là file của Sếp, xem mục CẦN SẾP.
    ///
    /// Hàm này CHỈ IN CẢNH BÁO trong Editor, không đổi hình gì: mục đích là lần sau ai
    /// kéo nhầm sprite vào ô 9-slice thì Console nói ngay, khỏi phải chờ Sếp nhìn ra.
    /// Thân hàm bọc #if UNITY_EDITOR nhưng CHỮ KÝ thì không — để 3 chỗ gọi khỏi phải bọc
    /// #if theo, và bản build chỉ còn một hàm rỗng bị JIT loại bỏ.
    /// </summary>
    private static void CanhBaoNenKhong9Slice(string tenO, Sprite spr, float rongVe, float caoVe)
    {
#if UNITY_EDITOR
        if (spr == null) return;
        if (spr.border.sqrMagnitude > 0.01f) return;   // có vành ⇒ Sliced chạy đúng

        // Chỉ kêu khi thực sự BỊ MÉO: ảnh gần đúng tỉ lệ ô vẽ thì kéo giãn cũng không sao.
        float tlAnh = spr.rect.height > 0.5f ? spr.rect.width / spr.rect.height : 1f;
        float tlOVe = caoVe > 0.5f ? rongVe / caoVe : 1f;
        if (tlOVe <= tlAnh * 1.35f && tlOVe >= tlAnh * 0.74f) return;

        { Debug.LogWarning($"[ConstructionSiteUI] O art '{tenO}' dang gan sprite '{spr.name}' {spr.rect.width:0}x{spr.rect.height:0} co spriteBorder 0/0/0/0 nhung duoc ve Image.Type.Sliced o {rongVe:0}x{caoVe:0} (ti le anh {tlAnh:0.00} vs o ve {tlOVe:0.00}) -> border 0 lam Sliced TUT VE KEO GIAN PHANG nen anh bi bop meo. Sua trong Assets/_Game/Farm/ScriptableObjects/ConstructionArtKit.asset: xoa o nay (de None) de code dung art du phong, hoac dat spriteBorder trong Import Settings cua sprite."); }
#endif
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
