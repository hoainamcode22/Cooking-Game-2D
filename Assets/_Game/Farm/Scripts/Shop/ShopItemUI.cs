using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Prefab ShopItem_Template — hiển thị thông tin 1 item trong Shop theo thẻ mẫu 3a.
/// Hỗ trợ chuyển tiếp drag event lên ScrollRect cha để kéo vuốt cuộn mượt mà.
/// </summary>
public class ShopItemUI : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    // ── Tham chiếu UI ────────────────────────────────────────────────────────
    [Header("UI References")]
    public TMP_Text txtName;            // Tên sản phẩm (2 dòng cố định)
    public Image    imgIcon;            // Hình ảnh sản phẩm (84x84)
    public Image    imgCirclePlate;     // Đĩa tròn kem phía sau icon (112x112)
    public GameObject stepperRoot;      // Hàng stepper +/-
    public TMP_Text txtQuantity;        // Số lượng đang chọn
    public Button   btnMinus;           // Giảm số lượng
    public Button   btnPlus;            // Tăng số lượng
    public GameObject placeableNote;    // Nhãn "Mua 1 cái / lần" cho công trình/trang trí

    [Header("Buy Button References")]
    public Button   btnBuy;             // Nút xác nhận mua = Nút giá
    public Image    imgBuyBackground;   // Background của nút mua (xanh lá / xanh dương / xám)
    public Image    imgCurrencyIcon;    // Icon loại tiền (Vàng / Kim Cương)
    public TMP_Text txtPrice;           // Tổng giá tiền hiển thị

    [Header("Lock Overlay References")]
    public GameObject lockOverlayRoot;  // Overlay làm mờ khi chưa đủ level
    public TMP_Text   lockLevelText;    // Text "Mở ở cấp X"

    // ── Sprites ──────────────────────────────────────────────────────────────
    [Header("Sprites")]
    public Sprite iconGold;             // Sprite Vàng
    public Sprite iconDiamond;          // Sprite Kim Cương
    public Sprite btnBuyGoldSprite;     // Nút mua Vàng (Xanh lá)
    public Sprite btnBuyGemSprite;      // Nút mua Gem (Xanh dương)
    public Sprite btnBuyLockedSprite;   // Nút mua Khoá (Xám)

    // ── Biến logic nội bộ ────────────────────────────────────────────────────
    private BaseItemData currentData;
    private int currentQuantity = 1;
    private bool isDiamondItem;
    private bool isLocked;
    private ScrollRect parentScrollRect;

    public BaseItemData Data => currentData;
    public int CurrentQuantity => currentQuantity;
    public bool IsLocked => isLocked;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureParentScrollRect();

        if (btnPlus != null)  btnPlus.onClick.AddListener(IncreaseQuantity);
        if (btnMinus != null) btnMinus.onClick.AddListener(DecreaseQuantity);
        if (btnBuy != null)   btnBuy.onClick.AddListener(BuyItem);
    }

    private void EnsureParentScrollRect()
    {
        if (parentScrollRect == null)
            parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    // ── Chuyển tiếp Drag & Scroll lên ScrollRect cha (PC & Mobile cảm ứng) ────

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        EnsureParentScrollRect();
        if (parentScrollRect != null) parentScrollRect.OnInitializePotentialDrag(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        EnsureParentScrollRect();
        if (parentScrollRect != null) parentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EnsureParentScrollRect();
        if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EnsureParentScrollRect();
        if (parentScrollRect != null) parentScrollRect.OnEndDrag(eventData);
    }

    public void OnScroll(PointerEventData eventData)
    {
        EnsureParentScrollRect();
        if (parentScrollRect != null) parentScrollRect.OnScroll(eventData);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(BaseItemData data)
    {
        currentData     = data;
        currentQuantity = 1;

        if (parentScrollRect == null)
            parentScrollRect = GetComponentInParent<ScrollRect>();

        if (data == null) return;

        if (txtName != null) txtName.text = data.itemName;
        if (imgIcon != null)
        {
            imgIcon.sprite = data.itemIcon;
            imgIcon.enabled = data.itemIcon != null;
        }

        isDiamondItem = data.diamondPrice > 0;
        if (imgCurrencyIcon != null)
            imgCurrencyIcon.sprite = isDiamondItem ? iconDiamond : iconGold;

        // Công trình & Trang trí: ẩn stepper, hiện "Mua 1 cái / lần"
        bool isPlaceable = data is PlaceableItemData;
        if (stepperRoot != null) stepperRoot.SetActive(!isPlaceable);
        if (placeableNote != null) placeableNote.SetActive(isPlaceable);

        // Kiểm tra cấp độ mở khoá
        int playerLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
        int unlockLvl = GetUnlockLevel(data);
        isLocked = unlockLvl > 1 && playerLevel < unlockLvl;

        if (lockOverlayRoot != null)
            lockOverlayRoot.SetActive(isLocked);

        if (lockLevelText != null)
            lockLevelText.text = $"Mở ở cấp {unlockLvl}";

        UpdateUI();
    }

    public void IncreaseQuantity()
    {
        if (isLocked) return;
        currentQuantity = Mathf.Min(99, currentQuantity + 1);
        UpdateUI();
    }

    public void DecreaseQuantity()
    {
        if (isLocked) return;
        if (currentQuantity > 1)
            currentQuantity--;
        UpdateUI();
    }

    public void BuyItem()
    {
        if (isLocked || currentData == null) return;

        int totalCost = GetTotalCost();

        bool success = isDiamondItem
            ? FarmEconomyManager.Instance.SpendGems(totalCost)
            : FarmEconomyManager.Instance.SpendGold(totalCost);

        if (!success)
        {
            ShopManager.Instance?.ShowToast("Không đủ tiền!");
            return;
        }

        // Báo cáo tiến độ nhiệm vụ
        int boughtQty = GetChargedQuantity();
        MissionProgressTracker.ReportEvent(MissionEventType.BuyShopItem, currentData.itemID, boughtQty);
        if (currentData is CropData)
            MissionProgressTracker.ReportEvent(MissionEventType.BuySeed, currentData.itemID, boughtQty);

        // Công trình / Trang trí -> Chuyển sang chế độ đặt
        if (currentData is PlaceableItemData placeable && placeable.prefabToBuild != null)
        {
            ShopManager.Instance.CloseShop();
            PlacementManager.Instance.StartPlacingNewObject(placeable);
            return;
        }

        // Hạt giống / Nông sản -> Thêm vào kho
        WarehouseManager.Instance.AddItem(
            currentData.itemID,
            currentData.itemName,
            currentData.itemIcon,
            currentQuantity
        );

        // Tutorial L2: Báo đã mua hạt giống
        if (currentData is CropData crop)
            TutorialManager.Instance?.NotifyBuySeed(currentData.itemID, crop.cropId, currentQuantity);

        // Hiện Toast mua hàng thành công
        string qtyStr = (currentData is PlaceableItemData) ? "" : $"x{currentQuantity} ";
        ShopManager.Instance?.ShowToast($"Đã mua {qtyStr}{currentData.itemName}!");
        ShopManager.Instance?.RefreshCurrencyBalances();

        // Reset số lượng về 1 sau khi mua
        currentQuantity = 1;
        UpdateUI();
    }

    // ── Cập nhật hiển thị ────────────────────────────────────────────────────

    private void UpdateUI()
    {
        if (txtQuantity != null)
            txtQuantity.text = currentQuantity.ToString();

        int cost = GetTotalCost();
        if (txtPrice != null)
            txtPrice.text = cost.ToString("N0", new System.Globalization.CultureInfo("vi-VN"));

        // Cập nhật màu sắc nút Mua
        if (btnBuy != null)
            btnBuy.interactable = !isLocked;

        if (imgBuyBackground != null)
        {
            if (isLocked)
                imgBuyBackground.sprite = btnBuyLockedSprite;
            else
                imgBuyBackground.sprite = isDiamondItem ? btnBuyGemSprite : btnBuyGoldSprite;
        }
    }

    private int GetTotalCost()
    {
        if (currentData == null) return 0;

        int unitPrice = isDiamondItem
            ? currentData.diamondPrice
            : PlotPurchasePricing.EffectiveGoldPrice(currentData);

        return GetChargedQuantity() * unitPrice;
    }

    private int GetChargedQuantity()
    {
        bool placeable = currentData is PlaceableItemData p && p.prefabToBuild != null;
        return placeable ? 1 : Mathf.Max(1, currentQuantity);
    }

    private static int GetUnlockLevel(BaseItemData item)
    {
        if (item == null) return 1;
        var f = item.GetType().GetField("unlockLevel",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(int))
            return Mathf.Max(1, (int)f.GetValue(item));
        return 1;
    }
}
