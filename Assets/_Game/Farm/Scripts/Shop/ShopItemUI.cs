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

    // [IL2CPP] KHONG dung new CultureInfo("vi-VN"): build bat Invariant Globalization se nem
    // CultureNotFoundException. Dung NumberFormatInfo tu khai bao — giong TownshipHUDController.cs.
    private static readonly System.Globalization.NumberFormatInfo DinhDangTien =
        new System.Globalization.NumberFormatInfo
        {
            NumberGroupSeparator   = ".",
            NumberDecimalSeparator = ",",
            NumberGroupSizes       = new[] { 3 },
        };
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

    // ── [EN-fit 2026-09-17] Cấu hình GỐC của nhãn khoá ───────────────────────
    // Chụp ĐÚNG MỘT LẦN, trước khi ta động vào. Chụp sau khi đã bật autosize thì
    // fontSize đọc được là cỡ ĐÃ BỊ BÓP, lưu vào là mất vĩnh viễn cỡ chữ thật.
    private bool    daLuuNhanKhoa;
    private bool    autoSizeNhanKhoaGoc;
    private float   coChuNhanKhoaGoc;
    private float   coChuMinNhanKhoaGoc;
    private float   coChuMaxNhanKhoaGoc;
    private TextOverflowModes tranNhanKhoaGoc;
    private Vector2 kichThuocNhanKhoaGoc;
    private Vector2 viTriNhanKhoaGoc;

    // Lề hai bên nhãn khoá tính từ bề ngang lớp phủ (296 - 28*2 = 240 trên thẻ chuẩn).
    private const float LE_NGANG_NHAN_KHOA = 28f;
    // Đủ chỗ cho HAI dòng ở cỡ 22 (line-height ~26). Hộp gốc 36 chỉ đủ một dòng.
    private const float CAO_NHAN_KHOA_TOI_THIEU = 56f;
    // Tâm Y mới: dưới ổ khoá (mép dưới ở -4) và trên nút Mua (-158..-106).
    private const float Y_NHAN_KHOA = -36f;
    // Sàn cỡ chữ khi autosize co lại — dưới mức này là không đọc nổi trên điện thoại.
    private const float CO_CHU_KHOA_SAN = 14f;

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

        if (txtName != null) txtName.text = Loc.T(data.itemName);   // [2026-09-23] ten decor/cong trinh tieng Viet -> dich
        if (imgIcon != null)
        {
            imgIcon.sprite = data.itemIcon;
            imgIcon.enabled = data.itemIcon != null;
        }

        isDiamondItem = data.diamondPrice > 0;
        if (imgCurrencyIcon != null)
            imgCurrencyIcon.sprite = isDiamondItem ? iconDiamond : iconGold;

        // Công trình & Trang trí: ẩn stepper, hiện "Mua 1 cái / lần"
        // [Hồ Câu vòng 14] Cần câu cũng là món 1 cái/lần (có độ bền riêng, GrantRod chỉ cấp 1 cần)
        // nên dùng chung đường ẩn stepper — tránh Sếp bấm +5 rồi bị trừ tiền 5 lần mà chỉ nhận 1 cần.
        bool isPlaceable = data is PlaceableItemData || (data != null && data.GetType().Name == "RodData");
        if (stepperRoot != null) stepperRoot.SetActive(!isPlaceable);
        if (placeableNote != null) placeableNote.SetActive(isPlaceable);

        // Kiểm tra cấp độ mở khoá
        int playerLevel = PlayerProgressManager.Instance != null ? PlayerProgressManager.Instance.Level : 1;
        int unlockLvl = GetUnlockLevel(data);
        isLocked = unlockLvl > 1 && playerLevel < unlockLvl;

        if (lockOverlayRoot != null)
            lockOverlayRoot.SetActive(isLocked);

        if (lockLevelText != null)
            lockLevelText.text = Loc.TF("Mở ở cấp {0}", unlockLvl);

        // [EN-fit 2026-09-17] Vừa đặt chữ xong ⇒ căn lại lớp phủ khoá ngay. Xem ApBoCucKhoa().
        ApBoCucKhoa(isPlaceable);

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

        // [Hồ Câu vòng 14] Null-check hệ tiền: scene lạ (hoặc Play thẳng scene con) không có FarmEconomyManager
        // thì đường cũ ném NullReference ngay dòng này.
        FarmEconomyManager kinhTe = FarmEconomyManager.Instance;
        if (kinhTe == null)
        {
            Debug.LogWarning("[Shop] Không có FarmEconomyManager — bỏ qua lệnh mua.");
            ShopManager.Instance?.ShowToast("Chưa sẵn sàng!");
            return;
        }

        // 🟢 V11 — CÔNG TRÌNH ĐÒI NGUYÊN LIỆU (gỗ/đá/kính/đinh do tàu lửa mang về).
        // Kiểm tra TRƯỚC khi trừ tiền để không bao giờ mất tiền mà không được hàng.
        var placeableCheck = currentData as PlaceableItemData;
        if (placeableCheck != null && placeableCheck.RequiresMaterials)
        {
            if (!BuildMaterials.HasAll(placeableCheck.materialCosts))
            {
                string thieu = BuildMaterials.Describe(placeableCheck.materialCosts, true);
                ShopManager.Instance?.ShowToast(Loc.TF("Thiếu nguyên liệu: {0}", thieu));
                return;
            }
        }

        bool success = isDiamondItem
            ? kinhTe.SpendGems(totalCost)
            : kinhTe.SpendGold(totalCost);

        if (!success)
        {
            ShopManager.Instance?.ShowToast("Không đủ tiền!");
            return;
        }

        // Tiền đã trừ xong -> trừ nguyên liệu. Nếu hụt (do đổi giữa chừng) thì hoàn tiền.
        if (placeableCheck != null && placeableCheck.RequiresMaterials)
        {
            if (!BuildMaterials.TrySpend(placeableCheck.materialCosts))
            {
                if (isDiamondItem) kinhTe.AddGems(totalCost); else kinhTe.AddGold(totalCost);
                ShopManager.Instance?.ShowToast("Thiếu nguyên liệu!");
                return;
            }
        }

        // Báo cáo tiến độ nhiệm vụ
        int boughtQty = GetChargedQuantity();
        MissionProgressTracker.ReportEvent(MissionEventType.BuyShopItem, currentData.itemID, boughtQty);
        if (currentData is CropData)
            MissionProgressTracker.ReportEvent(MissionEventType.BuySeed, currentData.itemID, boughtQty);

        // [Hồ Câu vòng 14] Cần câu KHÔNG vào kho hạt giống — vào FishingGearState (có độ bền, cần đang cầm).
        // Tiền ĐÃ bị trừ ở đầu hàm này, nên phải gọi GrantRod (KHÔNG trừ tiền).
        // TUYỆT ĐỐI không gọi FishingGearState.TryBuy ở đây: TryBuy tự trừ tiền lần nữa ⇒ mất tiền 2 lần.
        // TryBuy vẫn giữ nguyên cho Quầy Cá (FishCounterPopupUI) — bên đó tự lo tiền.
        if (currentData != null && currentData.GetType().Name == "RodData")
        {
            var gearStateType = System.Type.GetType("FarmGame.Fishing.FishingGearState, Assembly-CSharp");
            if (gearStateType != null)
            {
                var instProp = gearStateType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                object gearInst = instProp?.GetValue(null);
                var grantMethod = gearStateType.GetMethod("GrantRod", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (gearInst != null && grantMethod != null)
                {
                    object[] rArgs = new object[] { currentData, string.Empty };
                    bool ok = (bool)grantMethod.Invoke(gearInst, rArgs);
                    string lyDo = rArgs[1] as string ?? string.Empty;

                    if (!ok)
                    {
                        // Hoàn đúng số vừa trừ — không để người chơi mất tiền vì data hỏng.
                        if (FarmEconomyManager.Instance != null)
                        {
                            if (isDiamondItem) { FarmEconomyManager.Instance.AddGems(totalCost); }
                            else { FarmEconomyManager.Instance.AddGold(totalCost); }
                        }
                        Debug.Log("[Fishing] Shop: cấp cần " + currentData.itemID + " thất bại (" + lyDo + ") — đã hoàn tiền.");
                        ShopManager.Instance?.ShowToast(string.IsNullOrEmpty(lyDo) ? Loc.T("Mua cần thất bại") : Loc.T(lyDo));
                        ShopManager.Instance?.RefreshCurrencyBalances();
                        return;
                    }

                    // lyDo rỗng = cần mới; có chữ = đã sở hữu, GrantRod trả "Đã thay cần mới".
                    string thongBao = string.IsNullOrEmpty(lyDo)
                        ? Loc.TF("Đã mua {0}!", Loc.T(currentData.itemName))
                        : Loc.T(lyDo);
                    ShopManager.Instance?.ShowToast(thongBao);
                    ShopManager.Instance?.RefreshCurrencyBalances();
                    return;
                }
            }
        }

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
        ShopManager.Instance?.ShowToast(Loc.TF("Đã mua {0}{1}!", qtyStr, Loc.T(currentData.itemName)));
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
            txtPrice.text = cost.ToString("N0", DinhDangTien);

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
        // [Hồ Câu vòng 14] Cần câu luôn tính đúng 1 cái/lần (GrantRod chỉ cấp 1 cần).
        bool isRod = currentData != null && currentData.GetType().Name == "RodData";
        return (placeable || isRod) ? 1 : Mathf.Max(1, currentQuantity);
    }

    // ── [EN-fit 2026-09-17] LỚP PHỦ KHOÁ — CHỐNG TRÀN CHỮ TIẾNG ANH ──────────
    // Số đo lấy TỪ SCENE (ShopItem_Template trong SCN_Farm.unity), không phải đoán:
    //   • Thẻ (ShopItem_Template) và Lock_Overlay : 296 × 335, cùng tâm.
    //   • Lock_Badge (ổ khoá)  : (0, +25) 58 × 58  ⇒ mép dưới ổ khoá ở y = -4.
    //   • Txt_LockLevel        : (0, -25) 260 × 36, cỡ 22, enableAutoSizing = 0,
    //                            overflowMode = Overflow.
    //   • Stepper_Row (hàng -/+) : (0, -74) trong Card_Inner (Card_Inner ở y = +33)
    //                            ⇒ trong hệ toạ độ thẻ là y = -41, cao 38 ⇒ chiếm
    //                            dải -60 .. -22.
    //
    // HỎNG THẾ NÀO: "Mở ở cấp 6" ngắn nên lọt một dòng trong hộp cao 36. Tiếng Anh
    // dài hơn ~40%, hộp 36px chỉ đủ MỘT dòng nên câu tự xuống dòng thứ hai và dòng đó
    // rơi thẳng vào dải -43 .. -22 — trùng hàng -/+. Lớp phủ chỉ là màn mờ alpha 0.65
    // nên hàng -/+ hiện xuyên qua chữ, thành ra chữ chồng nút.
    //
    // CÁCH XỬ LÝ:
    //   1. ẨN hàng -/+ và nhãn "Mua 1 cái / lần" khi thẻ đang khoá. Chúng VÔ TÁC DỤNG
    //      lúc khoá — IncreaseQuantity / DecreaseQuantity / BuyItem đều mở đầu bằng
    //      `if (isLocked) return;` ⇒ ẩn vừa đúng nghĩa vừa dẹp luôn va chạm hình ảnh.
    //   2. Nới hộp chữ theo BỀ NGANG LỚP PHỦ (đọc lúc chạy, không hard-code 296), chừa
    //      lề hai bên, và cho cao đủ HAI dòng; dời tâm xuống -36 để không đụng ổ khoá
    //      phía trên lẫn nút Mua phía dưới.
    //   3. Bật autosize CHỈ ĐƯỢC NHỎ LẠI (trần = cỡ gốc, sàn 14) và đổi Overflow →
    //      Truncate, để chữ không bao giờ thoát ra ngoài mép thẻ nữa.
    //
    // VÌ SAO NẰM Ở CODE: vòng này cấm sửa .prefab/.unity. Sửa thẳng trong scene gọn hơn
    // và bỏ hẳn được hàm này — danh sách việc chỉnh tay đã ghi trong báo cáo cho Sếp.
    private void ApBoCucKhoa(bool isPlaceable)
    {
        // Hàng -/+ và nhãn "Mua 1 cái / lần" không bấm được khi khoá ⇒ ẩn hẳn.
        if (stepperRoot != null)   stepperRoot.SetActive(!isPlaceable && !isLocked);
        if (placeableNote != null) placeableNote.SetActive(isPlaceable && !isLocked);

        if (lockLevelText == null) return;

        RectTransform rtChu = lockLevelText.rectTransform;
        if (rtChu == null) return;

        if (!daLuuNhanKhoa)
        {
            daLuuNhanKhoa        = true;
            autoSizeNhanKhoaGoc  = lockLevelText.enableAutoSizing;
            coChuNhanKhoaGoc     = lockLevelText.fontSize;
            coChuMinNhanKhoaGoc  = lockLevelText.fontSizeMin;
            coChuMaxNhanKhoaGoc  = lockLevelText.fontSizeMax;
            tranNhanKhoaGoc      = lockLevelText.overflowMode;
            kichThuocNhanKhoaGoc = rtChu.sizeDelta;
            viTriNhanKhoaGoc     = rtChu.anchoredPosition;
        }

        if (!isLocked)
        {
            // Thẻ có thể được Setup lại cho món khác đã mở khoá ⇒ trả về nguyên trạng.
            lockLevelText.enableAutoSizing = autoSizeNhanKhoaGoc;
            lockLevelText.fontSize         = coChuNhanKhoaGoc;
            lockLevelText.fontSizeMin      = coChuMinNhanKhoaGoc;
            lockLevelText.fontSizeMax      = coChuMaxNhanKhoaGoc;
            lockLevelText.overflowMode     = tranNhanKhoaGoc;
            rtChu.sizeDelta                = kichThuocNhanKhoaGoc;
            rtChu.anchoredPosition         = viTriNhanKhoaGoc;
            return;
        }

        // Bề ngang bám theo lớp phủ (anchorMin == anchorMax nên rect.width = sizeDelta.x,
        // đọc được ngay, không cần chờ layout). Thiếu lớp phủ thì lùi về số đo gốc.
        RectTransform rtPhu = lockOverlayRoot != null
                            ? lockOverlayRoot.GetComponent<RectTransform>()
                            : null;
        float rongPhu = rtPhu != null ? rtPhu.rect.width : 0f;
        if (rongPhu < 1f) rongPhu = kichThuocNhanKhoaGoc.x;

        float rongChu = Mathf.Max(80f, rongPhu - LE_NGANG_NHAN_KHOA * 2f);
        float caoChu  = Mathf.Max(CAO_NHAN_KHOA_TOI_THIEU, kichThuocNhanKhoaGoc.y);

        rtChu.sizeDelta        = new Vector2(rongChu, caoChu);
        rtChu.anchoredPosition = new Vector2(viTriNhanKhoaGoc.x, Y_NHAN_KHOA);

        float coGoc = coChuNhanKhoaGoc > 0f ? coChuNhanKhoaGoc : 22f;
        lockLevelText.enableAutoSizing = true;
        lockLevelText.fontSizeMax      = coGoc;                              // không to hơn thiết kế
        lockLevelText.fontSizeMin      = Mathf.Min(CO_CHU_KHOA_SAN, coGoc);  // sàn, và luôn <= trần
        lockLevelText.overflowMode     = TextOverflowModes.Truncate;
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
