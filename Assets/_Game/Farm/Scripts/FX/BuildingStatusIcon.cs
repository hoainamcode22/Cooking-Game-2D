using UnityEngine;

/// <summary>
/// ICON TRẠNG THÁI NỔI TRÊN ĐẦU CÔNG TRÌNH — khung trắng bo góc + một icon.
/// ═══════════════════════════════════════════════════════════════════════
///
/// 🔴 ĐÂY LÀ KHÁC BIỆT LỚN NHẤT giữa màn hình game này và Township.
/// Tài liệu phân tích (§3 + kết luận 2) chốt: "Sức mạnh nằm ở SỐ LƯỢNG icon nổi, không
/// phải độ phức tạp từng cái. Mỗi hiệu ứng chỉ 2–3 tween. Nhưng màn hình lúc nào cũng có
/// 5–8 icon đang bob. Đó là cái làm game 'đắt tiền'."
/// Township KHÔNG dùng chữ để nói trạng thái — họ dùng một bộ icon nhất quán mà trẻ chưa
/// đọc được chữ cũng đoán ra.
///
/// BỘ ICON (§3 tài liệu):
///     Đang xây          → mũ bảo hộ vàng   (ô art `HardHatDone`, ĐÃ CÓ)
///     Sản phẩm đã xong  → icon sản phẩm    (ô art `IconProductReady`, hoặc sprite riêng)
///     Thưởng XP chờ     → ngôi sao         (ô art `IconStar`)
///     Máy ĐỨNG KHÔNG    → chữ "Z"          (ô art `IconZzz`)  ← chi tiết tinh tế nhất
///
/// DỰNG BẰNG CODE, CÓ Ô ART ĐỂ THAY SAU: y hệt cách 19 ô còn lại của ConstructionArtKit
/// đang làm. Chưa có sprite thì vẽ thủ tục + tô MÀU NHẬN DẠNG, Edric thả art vào ô là nó
/// thay ngay, không cần sửa một dòng code.
///
/// SORTING: đặt trên layer trên cùng (<see cref="ConstructionManager.TopSortingLayerName"/>)
/// với order 31000 — cao hơn công trình và cao hơn UI công trường (30000), thấp hơn nhãn
/// tên ô debug (32000). Nếu không thì công trình đứng phía trước sẽ che mất icon.
///
/// CÁCH DÙNG
///   • Trong Inspector: gắn component lên công trình, chọn `Status`, đặt `Height Above Host`.
///   • Bằng code:  BuildingStatusIcon.AttachTo(go, BuildingStatusIcon.Status.Idle, 420f);
///   • Đổi trạng thái lúc chạy:  icon.SetStatus(BuildingStatusIcon.Status.ProductReady);
/// </summary>
[DisallowMultipleComponent]
public class BuildingStatusIcon : MonoBehaviour
{
    /// <summary>Bốn trạng thái theo ngôn ngữ icon Township (§3). None = ẩn hẳn.</summary>
    public enum Status
    {
        None,           // không có việc gì → ẩn icon
        Building,       // đang thi công    → mũ bảo hộ
        ProductReady,   // hàng đã xong     → icon sản phẩm
        RewardWaiting,  // thưởng XP chờ    → ngôi sao
        Idle            // đứng không       → chữ "Z"
    }

    [Header("◆ TRẠNG THÁI")]

    [Tooltip("Trạng thái đang hiện. Đổi trong Inspector lúc đang Play cũng cập nhật ngay " +
             "(qua OnValidate) — tiện để Edric xem thử 4 icon mà không cần viết code.")]
    [SerializeField] private Status status = Status.None;

    [Header("◆ VỊ TRÍ & CỠ (world unit, 1 ô lưới = 100)")]

    [Tooltip("Icon nổi cao bao nhiêu so với gốc transform của công trình.\n" +
             "Công trường đang xây: phải cao hơn cả cụm UI (nền tên + đồng hồ + nút rush), " +
             "nếu không hai thứ đè nhau. ConstructionSite tự tính giá trị này khi gắn.")]
    [SerializeField] private float heightAboveHost = 300f;

    [Tooltip("Cạnh của khung vuông bo góc. 110 ≈ hơn một ô lưới — cùng tỉ lệ với video mẫu.")]
    [SerializeField] private float frameWorldSize = 110f;

    [Tooltip("Icon chiếm bao nhiêu phần khung. 0.66 để chừa viền trắng quanh icon " +
             "(khung trắng viền dày là dấu hiệu nhận dạng của Township).")]
    [SerializeField] private float iconFillRatio = 0.66f;

    [Header("◆ ART")]

    [Tooltip("Bộ ô art. Để trống = tự lấy từ ConstructionManager trong scene; " +
             "không có luôn thì mọi mảnh là hình vẽ code tô màu nhận dạng.")]
    [SerializeField] private ConstructionArtKit artKit;

    [Tooltip("Sprite SẢN PHẨM riêng của công trình này (bình sữa / ổ bánh / phô mai…).\n" +
             "Để trống = dùng ô art chung `IconProductReady`. Ưu tiên ô này vì mỗi xưởng " +
             "ra một loại hàng khác nhau — đó là điều làm màn hình Township phong phú.")]
    [SerializeField] private Sprite productSprite;

    [Header("◆ NHỊP NHẤP NHÔ")]

    [Tooltip("BẬT = tự gắn FloatingIconBob (y ±6px / 1.2s, scale 1↔1.06 lệch pha).\n" +
             "TẮT chỉ khi muốn icon đứng yên hoàn toàn.")]
    [SerializeField] private bool addBob = true;

    // Quy đổi: thông số bob đo bằng 'px' video; ở world này 1 ô = 100 unit và khung icon
    // là 110 unit, nên 1 'px' ≈ 2.5 unit để giữ ĐÚNG TỈ LỆ bob/khung của video.
    private const float PixelToWorldUnit = 2.5f;

    private const string RootName  = "Status_Icon";
    private const int    FrameOrder = 31000;

    private Transform      _root;
    private SpriteRenderer _frame;
    private SpriteRenderer _icon;
    private bool           _built;
    private bool           _labelsDone;   // nhãn debug chỉ gắn MỘT LẦN, xem Apply()

    /// <summary>Trạng thái đang hiện.</summary>
    public Status CurrentStatus => status;

    // ════════════════════════════════════════════════════════════════════════
    // API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gắn icon lên một công trình bất kỳ và bật luôn trạng thái.
    /// Trả về component đã gắn (dùng lại nếu host đã có sẵn một cái).
    /// </summary>
    public static BuildingStatusIcon AttachTo(GameObject host, Status status,
                                              float heightAbove = 300f,
                                              ConstructionArtKit kit = null)
    {
        if (host == null) return null;

        BuildingStatusIcon icon = host.GetComponent<BuildingStatusIcon>();
        if (icon == null) icon = host.AddComponent<BuildingStatusIcon>();

        icon.heightAboveHost = heightAbove;
        if (kit != null) icon.artKit = kit;
        icon.SetStatus(status);
        return icon;
    }

    /// <summary>Đổi trạng thái. Dựng phần hình nếu chưa có, ẩn hẳn khi None.</summary>
    public void SetStatus(Status next)
    {
        status = next;
        EnsureBuilt();
        Apply();
    }

    /// <summary>Gán sprite sản phẩm riêng (gọi trước khi bật ProductReady).</summary>
    public void SetProductSprite(Sprite sprite)
    {
        productSprite = sprite;
        if (status == Status.ProductReady) Apply();
    }

    /// <summary>Ẩn/hiện cả cụm icon mà KHÔNG mất trạng thái đã đặt.</summary>
    public void SetVisible(bool visible)
    {
        if (_root != null) _root.gameObject.SetActive(visible && status != Status.None);
    }

    // ════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        // Start (không Awake): artKit thường lấy từ ConstructionManager.Instance, mà
        // Instance chỉ được gán trong Awake của manager → đọc ở Awake là 50/50 ra null.
        EnsureBuilt();
        Apply();
    }

    private void EnsureBuilt()
    {
        if (_built && _root != null) return;

        if (artKit == null && ConstructionManager.Instance != null)
            artKit = ConstructionManager.Instance.ArtKit;

        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            _root = existing;
        }
        else
        {
            var go = new GameObject(RootName);
            go.layer = gameObject.layer;
            _root = go.transform;
            _root.SetParent(transform, false);
        }

        // CHUẨN HOÁ SCALE: prefab công trình có thể có root scale bất kỳ (ghost dùng 100).
        // Chia ngược scale của host để BÊN TRONG _root, 1 đơn vị = 1 world unit — nhờ vậy
        // mọi con số ở trên đọc thẳng ra world unit, không phải nhân/chia theo từng prefab.
        float sx = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        _root.localScale    = new Vector3(1f / sx, 1f / sy, 1f);
        _root.localRotation = Quaternion.identity;
        _root.localPosition = new Vector3(0f, heightAboveHost / sy, 0f);

        string layer = ConstructionManager.TopSortingLayerName;

        _frame = MakeRenderer(_root, "Khung", FrameOrder, layer);
        _icon  = MakeRenderer(_root, "Icon",   FrameOrder + 1, layer);

        // Nhịp nhấp nhô gắn lên _root để KHUNG VÀ ICON đi cùng nhau. Gắn riêng từng cái là
        // icon và khung lệch pha nhau, trông như icon bị rơi ra khỏi khung.
        if (addBob)
        {
            var bob = _root.GetComponent<FloatingIconBob>();
            if (bob == null) bob = _root.gameObject.AddComponent<FloatingIconBob>();
            bob.Configure(6f, 1.2f, PixelToWorldUnit);
        }

        _built = true;
    }

    private void Apply()
    {
        if (_root == null) return;

        if (status == Status.None)
        {
            _root.gameObject.SetActive(false);
            return;
        }
        _root.gameObject.SetActive(true);

        // ── KHUNG TRẮNG BO GÓC ───────────────────────────────────────────────
        ConstructionArtKit.ResolveSafe(artKit, ConstructionArtKit.Slot.IconFrameBg,
            ConstructionSpriteFactory.Panel(96, 96, 26), out Sprite frameSpr, out Color frameCol);

        if (_frame != null)
        {
            _frame.sprite = frameSpr;
            _frame.color  = frameCol;
            Fit(_frame, frameWorldSize, frameWorldSize);
        }

        // ── ICON THEO TRẠNG THÁI ─────────────────────────────────────────────
        ConstructionArtKit.Slot slot;
        Sprite fallback;
        switch (status)
        {
            case Status.Building:
                slot = ConstructionArtKit.Slot.HardHatDone;
                fallback = ConstructionSpriteFactory.HardHat();
                break;
            case Status.ProductReady:
                slot = ConstructionArtKit.Slot.IconProductReady;
                fallback = ConstructionSpriteFactory.MilkBottle();
                break;
            case Status.RewardWaiting:
                slot = ConstructionArtKit.Slot.IconStar;
                fallback = ConstructionSpriteFactory.Star();
                break;
            default: // Status.Idle
                slot = ConstructionArtKit.Slot.IconZzz;
                fallback = ConstructionSpriteFactory.LetterZ();
                break;
        }

        ConstructionArtKit.ResolveSafe(artKit, slot, fallback,
                                       out Sprite iconSpr, out Color iconCol);

        // Sprite sản phẩm RIÊNG của công trình thắng cả ô art chung: mỗi xưởng ra một loại
        // hàng, dùng chung một bình sữa cho tất cả là mất hết thông tin.
        if (status == Status.ProductReady && productSprite != null)
        {
            iconSpr = productSprite;
            iconCol = Color.white;
        }

        if (_icon != null)
        {
            _icon.sprite = iconSpr;
            _icon.color  = iconCol;

            // Giữ đúng tỉ lệ gốc của sprite (bình sữa cao hơn rộng) rồi mới thu vào khung.
            float box = frameWorldSize * Mathf.Clamp01(iconFillRatio);
            Vector2 sz = iconSpr != null ? (Vector2)iconSpr.bounds.size : Vector2.one;
            float k = Mathf.Min(sz.x > 0.0001f ? box / sz.x : box,
                                sz.y > 0.0001f ? box / sz.y : box);
            _icon.transform.localScale    = new Vector3(k, k, 1f);
            _icon.transform.localPosition = Vector3.zero;
        }

        // NHÃN DEBUG CHỈ GẮN MỘT LẦN: Apply() được gọi lại mỗi lần đổi trạng thái (và một
        // lần nữa ở Start), mà AttachSlotLabel luôn Instantiate một node chữ mới → không có
        // cổng chặn thì mỗi lần đổi Status lại dính thêm một nhãn đè lên nhau.
        // Chỉ tốn công khi Edric bật `showSlotLabels`; build thật không chạy vào đây.
        if (!_labelsDone && ConstructionArtKit.WantLabels(artKit))
        {
            _labelsDone = true;
            if (_frame != null)
                ConstructionSiteVisuals.AttachSlotLabel(_frame.transform,
                    ConstructionArtKit.Slot.IconFrameBg, artKit);
            if (_icon != null)
                ConstructionSiteVisuals.AttachSlotLabel(_icon.transform, slot, artKit);
        }
    }

    // ── Tiện ích ─────────────────────────────────────────────────────────────

    private SpriteRenderer MakeRenderer(Transform parent, string name, int order, string layer)
    {
        Transform t = parent.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = layer;
        sr.sortingOrder     = order;
        sr.drawMode         = SpriteDrawMode.Simple;
        return sr;
    }

    /// <summary>Kéo giãn transform để sprite phủ đúng w×h world unit (giống Fit của ConstructionSiteVisuals).</summary>
    private static void Fit(SpriteRenderer sr, float w, float h)
    {
        if (sr == null || sr.sprite == null) return;
        Vector2 sz = sr.sprite.bounds.size;
        sr.transform.localScale = new Vector3(
            sz.x > 0.0001f ? w / sz.x : w,
            sz.y > 0.0001f ? h / sz.y : h,
            1f);
        sr.transform.localPosition = Vector3.zero;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        frameWorldSize  = Mathf.Max(8f, frameWorldSize);
        iconFillRatio   = Mathf.Clamp(iconFillRatio, 0.1f, 1f);

        // Đang Play thì cập nhật ngay để Edric đổi Status trong Inspector là thấy liền.
        if (Application.isPlaying && _built) Apply();
    }
#endif
}
