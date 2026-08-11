using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn vào Prefab KhungHatGiong — hiển thị thông tin 1 item trong Shop.
/// ShopManager sẽ gọi Setup() sau khi Instantiate prefab này.
/// </summary>
public class ShopItemUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // ── Tham chiếu UI ────────────────────────────────────────────────────────
    [Header("UI References")]
    public Image    imgIcon;            // Hình ảnh sản phẩm
    public TMP_Text txtName;            // Tên sản phẩm
    public TMP_Text txtPrice;           // Tổng giá tiền (số lượng × đơn giá)
    public TMP_Text txtQuantity;        // Số lượng đang chọn
    public Image    imgCurrencyIcon;    // Icon loại tiền — đổi giữa Vàng / Kim Cương
    public Button   btnPlus;            // Tăng số lượng
    public Button   btnMinus;           // Giảm số lượng
    public Button   btnBuy;             // Xác nhận mua

    // ── Icon tiền tệ — kéo thả trong Inspector ────────────────────────────────
    [Header("Icon Tiền Tệ")]
    public Sprite iconGold;             // Sprite biểu tượng Vàng
    public Sprite iconDiamond;          // Sprite biểu tượng Kim Cương

    // ── Biến logic nội bộ ────────────────────────────────────────────────────
    private BaseItemData currentData;       // Data của item đang hiển thị

    /// <summary>Data item đang hiển thị (cho tutorial tìm đúng item Ngô để chỉ tay + bao xám).</summary>
    public BaseItemData Data => currentData;
    private int          currentQuantity = 1;

    /// <summary>Số lượng đang chọn (cho tutorial biết user đã bấm + đủ 8 chưa).</summary>
    public int CurrentQuantity => currentQuantity;
    private bool         isDiamondItem;     // true = trả bằng Kim Cương, false = Vàng

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        // Tắt raycastTarget trên mọi Graphic không phải Selectable (Button/Toggle...)
        // để nền/khung không chặn click của Nút Mua
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.GetComponent<Selectable>() == null)
                graphic.raycastTarget = false;
        }

        // Đăng ký sự kiện một lần, tránh đăng ký lại mỗi lần Setup()
        btnPlus .onClick.AddListener(IncreaseQuantity);
        btnMinus.onClick.AddListener(DecreaseQuantity);
        btnBuy  .onClick.AddListener(BuyItem);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo UI cho một item — được gọi bởi ShopManager sau khi Instantiate.
    /// </summary>
    public void Setup(BaseItemData data)
    {
        currentData     = data;
        currentQuantity = 1;

        txtName.text   = data.itemName;
        imgIcon.sprite = data.itemIcon;

        // Ưu tiên Kim Cương nếu có diamondPrice > 0
        isDiamondItem          = data.diamondPrice > 0;
        imgCurrencyIcon.sprite = isDiamondItem ? iconDiamond : iconGold;

        // Ẩn nút +/- cho vật phẩm xây dựng/trang trí (chỉ mua 1 cái mỗi lần)
        bool isPlaceable = data is PlaceableItemData;
        if (btnPlus     != null) btnPlus    .gameObject.SetActive(!isPlaceable);
        if (btnMinus    != null) btnMinus   .gameObject.SetActive(!isPlaceable);
        if (txtQuantity != null) txtQuantity.gameObject.SetActive(!isPlaceable);

        UpdateUI();

        // Cập nhật trạng thái lock theo level — ShopLevelLockUI tự ẩn/hiện overlay
        GetComponent<ShopLevelLockUI>()?.Refresh(data);
    }

    // ── Tăng / Giảm số lượng ─────────────────────────────────────────────────

    /// <summary>Tăng số lượng — gắn vào btnPlus qua Awake.</summary>
    public void IncreaseQuantity()
    {
        currentQuantity++;
        UpdateUI();
    }

    /// <summary>Giảm số lượng, tối thiểu là 1 — gắn vào btnMinus qua Awake.</summary>
    public void DecreaseQuantity()
    {
        if (currentQuantity > 1)
            currentQuantity--;

        UpdateUI();
    }

    // ── Mua hàng ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Xử lý mua: trừ tiền từ FarmEconomyManager → thêm vào WarehouseManager.
    /// Gắn vào btnBuy qua Awake.
    /// </summary>
    public void BuyItem()
    {
        int totalCost = GetTotalCost();

        bool success = isDiamondItem
            ? FarmEconomyManager.Instance.SpendGems(totalCost)
            : FarmEconomyManager.Instance.SpendGold(totalCost);

        if (!success)
        {
            return;
        }

        // Tiến độ nhiệm vụ mua hàng (chuồng heo=108, chuồng bò=106...);
        // mua CropData = mua hạt giống → tính thêm BuySeed.
        // Dùng SỐ LƯỢNG THẬT ĐÃ TRẢ TIỀN (công trình luôn = 1, xem GetTotalCost),
        // không dùng `currentQuantity`: báo 3 mà chỉ đặt 1 là tiến độ nhiệm vụ sai.
        int boughtQty = GetChargedQuantity();
        MissionProgressTracker.ReportEvent(MissionEventType.BuyShopItem, currentData.itemID, boughtQty);
        if (currentData is CropData)
            MissionProgressTracker.ReportEvent(MissionEventType.BuySeed, currentData.itemID, boughtQty);

        // Vật phẩm xây dựng / trang trí → đóng Shop và chuyển sang chế độ đặt
        if (currentData is PlaceableItemData placeable && placeable.prefabToBuild != null)
        {
            ShopManager.Instance.CloseShop();
            PlacementManager.Instance.StartPlacingNewObject(placeable);
            return;
        }

        // Hạt giống / vật phẩm thông thường → thêm vào kho
        WarehouseManager.Instance.AddItem(
            currentData.itemID,
            currentData.itemName,
            currentData.itemIcon,
            currentQuantity
        );

        // Tutorial L2: báo đã mua hạt giống (bước "mua ngô")
        if (currentData is CropData crop)
            TutorialManager.Instance?.NotifyBuySeed(currentData.itemID, crop.cropId, currentQuantity);
    }

    // ── Button Feedback ───────────────────────────────────────────────────────

    private Coroutine scaleRoutine;

    public void OnPointerDown(PointerEventData _) => ScaleTo(Vector3.one * 0.92f);
    public void OnPointerUp(PointerEventData _)   => ScaleTo(Vector3.one);

    private void ScaleTo(Vector3 target)
    {
        if (scaleRoutine != null) StopCoroutine(scaleRoutine);
        scaleRoutine = StartCoroutine(LerpScale(target));
    }

    private IEnumerator LerpScale(Vector3 target)
    {
        Vector3 from = transform.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.08f;
            transform.localScale = Vector3.Lerp(from, target, t);
            yield return null;
        }
        transform.localScale = target;
    }

    // ── Cập nhật hiển thị ────────────────────────────────────────────────────

    /// <summary>Đồng bộ txtQuantity và txtPrice theo currentQuantity.</summary>
    private void UpdateUI()
    {
        txtQuantity.text = currentQuantity.ToString();
        txtPrice.text    = GetTotalCost().ToString();
    }

    /// <summary>
    /// Số tiền THẬT phải trả cho lần bấm Mua này. Một chỗ tính duy nhất để nhãn giá
    /// và lúc trừ tiền không bao giờ lệch nhau.
    ///
    /// Hai điểm khác bản cũ:
    ///  • Ô đất lấy giá LUỸ TIẾN qua <see cref="PlotPurchasePricing"/> (F10).
    ///  • Công trình / trang trí luôn tính SỐ LƯỢNG = 1. Bản cũ nhân với `currentQuantity`
    ///    trong khi `BuyItem` chỉ chuyển sang chế độ đặt ĐÚNG MỘT vật → bấm "+" lên 3 rồi
    ///    Mua là mất tiền 3 công trình mà chỉ nhận 1. Đó là mất tiền thật của người chơi.
    /// </summary>
    private int GetTotalCost()
    {
        if (currentData == null) return 0;

        int unitPrice = isDiamondItem
            ? currentData.diamondPrice
            : PlotPurchasePricing.EffectiveGoldPrice(currentData);

        return GetChargedQuantity() * unitPrice;
    }

    /// <summary>Số lượng THẬT bị tính tiền. Công trình / trang trí luôn 1 (xem GetTotalCost).</summary>
    private int GetChargedQuantity()
    {
        bool placeable = currentData is PlaceableItemData p && p.prefabToBuild != null;
        return placeable ? 1 : Mathf.Max(1, currentQuantity);
    }
}
