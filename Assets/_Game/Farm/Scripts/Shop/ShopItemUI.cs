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
    private int          currentQuantity = 1;
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
        int unitPrice = isDiamondItem ? currentData.diamondPrice : currentData.goldPrice;
        int totalCost = currentQuantity * unitPrice;

        bool success = isDiamondItem
            ? FarmEconomyManager.Instance.SpendGems(totalCost)
            : FarmEconomyManager.Instance.SpendGold(totalCost);

        if (!success)
        {
            return;
        }

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

        int unitPrice = isDiamondItem ? currentData.diamondPrice : currentData.goldPrice;
        int totalCost = currentQuantity * unitPrice;

        txtPrice.text = totalCost.ToString();
    }
}
