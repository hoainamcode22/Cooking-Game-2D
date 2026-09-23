using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WarehouseCategory
{
    NongSan = 0,    // 🌾 Nông sản (Crops)
    ChanNuoi = 1,   // 🐔 Chăn nuôi (Animal products)
    MonAn = 2,      // 🍲 Món ăn & Chế biến (Cooked dishes & Processed goods)
    TatCa = 3       // [2026-09-23] Tab "Tất cả" — hien moi vat pham
}

public class WarehousePopupUI : MonoBehaviour
{
    private const string WarehouseLevelPrefsKey = FarmInventoryManager.WarehouseLevelPrefsKey;
    private const int WarehouseBaseCapacity = FarmInventoryManager.SlotsPerWarehouseLevel;
    private const int WarehouseMaxLevel = FarmInventoryManager.MaxWarehouseLevel;

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button btnClose;

    [Header("Category Tabs")]
    [SerializeField] private Button btnTabNongSan;
    [SerializeField] private Button btnTabChanNuoi;
    [SerializeField] private Button btnTabMonAn;
    [SerializeField] private Image imgTabNongSan;
    [SerializeField] private Image imgTabChanNuoi;
    [SerializeField] private Image imgTabMonAn;
    [SerializeField] private TMP_Text txtTabNongSan;
    [SerializeField] private TMP_Text txtTabChanNuoi;
    [SerializeField] private TMP_Text txtTabMonAn;
    [SerializeField] private RectTransform rectTabNongSan;
    [SerializeField] private RectTransform rectTabChanNuoi;
    [SerializeField] private RectTransform rectTabMonAn;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Sprite tabInactiveSprite;
    [Tooltip("Icon cho tab 'Tat ca' (mac dinh: icon kho cua nut KHO tren HUD).")]
    [SerializeField] private Sprite iconTabTatCa;

    [Header("Slots Grid")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform itemGridContainer;
    [SerializeField] private int minDisplaySlots = 8; // 4x2 slots view

    [Header("Slot Sprites")]
    [SerializeField] private Sprite slotNormalSprite;
    [SerializeField] private Sprite slotSelectedSprite;
    [SerializeField] private Sprite slotEmptySprite;

    [Header("Capacity Bar")]
    [SerializeField] private Image imgCapacityFill;
    [SerializeField] private TMP_Text txtCapacity;

    [Header("Right Detail Panel")]
    [SerializeField] private GameObject detailPanelRoot;
    [SerializeField] private Image imgDetailIcon;
    [SerializeField] private TMP_Text txtDetailTitle;
    [SerializeField] private TMP_Text txtDetailDesc;
    [SerializeField] private TMP_Text txtTransferCount;
    [SerializeField] private Button btnMinus;
    [SerializeField] private Button btnPlus;
    [SerializeField] private Button btnMax;
    [SerializeField] private Button btnTransferKitchen;

    [Header("Upgrade Footer Box")]
    [SerializeField] private TMP_Text txtUpgradeInfo;
    [SerializeField] private Button btnUpgrade;

    [Header("Databases")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();
    [SerializeField] private List<InventoryItemData> extraItemDatabase = new List<InventoryItemData>();
    [SerializeField] private List<string> cookedDishIds = new List<string>();

    private List<WarehouseSlotUI> slots = new List<WarehouseSlotUI>();
    private Dictionary<string, CropData> cropLookup = new Dictionary<string, CropData>();
    private Dictionary<string, InventoryItemData> extraItemLookup = new Dictionary<string, InventoryItemData>();

    private WarehouseCategory currentCategory = WarehouseCategory.TatCa;

    // [2026-09-23] Tab "Tat ca" (clone tu Tab_NongSan neu scene chua co) + 4 tab xep deu.
    private RectTransform rectTabTatCa; private Image imgTabTatCa; private TMP_Text txtTabTatCa;
    public const float TAB_RONG = 236f, TAB_CACH = 14f;
    private static float ViTriTab(int i) => (i - 1.5f) * (TAB_RONG + TAB_CACH);   // -375,-125,125,375 (hang 1008)
    private const float ICON_TAB_SCALE = 1.9f;   // icon 38 x 1.9 = 72px, nam gon trong tab (truoc 2.5 = 95px de sang tab ben)

    /// <summary>
    /// Dam bao co tab "Tat ca" dung dau hang va 4 tab cung rong 190 cach nhau 10.
    /// Dung duoc ca o Edit mode (tool "Tools/Kho/Them tab Tat ca") lan luc chay. Goi nhieu lan an toan.
    /// </summary>
    public static RectTransform DamBaoTabTatCa(RectTransform nongSan, RectTransform chanNuoi, RectTransform monAn)
        => DamBaoTabTatCa(nongSan, chanNuoi, monAn, null);

    public static RectTransform DamBaoTabTatCa(RectTransform nongSan, RectTransform chanNuoi, RectTransform monAn, Sprite iconTatCa)
    {
        if (nongSan == null || nongSan.parent == null) return null;
        Transform hang = nongSan.parent;
        var tab = hang.Find("Tab_All") as RectTransform;
        if (tab == null)
        {
            var go = UnityEngine.Object.Instantiate(nongSan.gameObject, hang, false);
            go.name = "Tab_All";
            tab = (RectTransform)go.transform;
            var nut = go.GetComponent<Button>();
            if (nut != null) nut.onClick = new Button.ButtonClickedEvent();
            foreach (var im in tab.GetComponentsInChildren<Image>(true))
                if (im.gameObject != go && im.name.Contains("Icon")) im.gameObject.SetActive(false);
            var lb = tab.GetComponentInChildren<TMP_Text>(true);
            if (lb != null)
            {
                lb.text = "Tất cả";
                var lrt = lb.rectTransform;
                lrt.anchoredPosition = new Vector2(0f, lrt.anchoredPosition.y);
            }
        }
        tab.SetSiblingIndex(0);
        // Icon tab "Tat ca" = icon kho (bat lai neu truoc do da an)
        foreach (var im in tab.GetComponentsInChildren<Image>(true))
        {
            if (im.transform == tab || !im.name.Contains("Icon")) continue;
            im.gameObject.SetActive(true);
            if (iconTatCa != null) { im.sprite = iconTatCa; im.preserveAspect = true; }
        }
        RectTransform[] ds = { tab, nongSan, chanNuoi, monAn };
        for (int i = 0; i < ds.Length; i++)
        {
            var r = ds[i]; if (r == null) continue;
            r.sizeDelta = new Vector2(TAB_RONG, r.sizeDelta.y);
            r.anchoredPosition = new Vector2(ViTriTab(i), r.anchoredPosition.y);
            if (r.childCount > 0)
            {
                var noiDung = r.GetChild(0) as RectTransform;
                if (noiDung != null) noiDung.sizeDelta = new Vector2(TAB_RONG - 14f, noiDung.sizeDelta.y);
            }
            var chu = r.GetComponentInChildren<TMP_Text>(true);
            if (chu != null)
            {
                chu.enableAutoSizing = true; chu.fontSizeMin = 15f; chu.fontSizeMax = 24f;
                chu.textWrappingMode = TextWrappingModes.NoWrap;
                var crt = chu.rectTransform;
                // chu nam tu sau icon (x = -rong/2 + 60) toi mep phai tru 10
                float trai = -TAB_RONG * 0.5f + 60f, phai = TAB_RONG * 0.5f - 10f;
                crt.anchoredPosition = new Vector2((trai + phai) * 0.5f, crt.anchoredPosition.y);
                crt.sizeDelta = new Vector2(phai - trai, crt.sizeDelta.y);
                chu.alignment = TextAlignmentOptions.Center;
            }
            foreach (var im in r.GetComponentsInChildren<Image>(true))
                if (im.transform != r && im.name.Contains("Icon"))
                {
                    var irt = (RectTransform)im.transform;
                    irt.localScale = Vector3.one * ICON_TAB_SCALE;
                    irt.anchoredPosition = new Vector2(-(TAB_RONG * 0.5f) + 32f, irt.anchoredPosition.y);
                }
        }
        return tab;
    }

    private void GanTabTatCa()
    {
        rectTabTatCa = DamBaoTabTatCa(rectTabNongSan, rectTabChanNuoi, rectTabMonAn, iconTabTatCa);
        if (rectTabTatCa == null) return;
        imgTabTatCa = rectTabTatCa.GetComponent<Image>();
        txtTabTatCa = rectTabTatCa.GetComponentInChildren<TMP_Text>(true);
        var nut = rectTabTatCa.GetComponent<Button>();
        if (nut != null) { nut.onClick.RemoveAllListeners(); nut.onClick.AddListener(() => SetCategory(WarehouseCategory.TatCa)); }
    }
    private string selectedItemId;
    private int transferQuantity = 1;
    private int warehouseLevel = 1;
    private int slotCapacity = 25;
    private bool popupInputLockHeld;

    private static readonly HashSet<string> AnimalItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "egg", "milk", "chickenmeat", "beef", "pork", "long_vu", "thit_bo", "thit_heo", "thit_ga", "trung", "sua"
    };

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            parentCanvas.gameObject.SetActive(true);

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (slotPrefab != null && slotPrefab.transform.parent == itemGridContainer)
            slotPrefab.SetActive(false);

        LoadWarehouseProgress();
        BuildLookups();
        WireButtons();
        GanTabTatCa();
    }

    private void Start()
    {
        if (popupRoot != null) popupRoot.SetActive(false);

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void OnDestroy()
    {
        ReleasePopupInputBlock();

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    private void LoadWarehouseProgress()
    {
        warehouseLevel = Mathf.Clamp(PlayerPrefs.GetInt(WarehouseLevelPrefsKey, 1), 1, WarehouseMaxLevel);
        slotCapacity = FarmInventoryManager.CapacityForLevel(warehouseLevel);
    }

    private void SaveWarehouseProgress()
    {
        warehouseLevel = Mathf.Clamp(warehouseLevel, 1, WarehouseMaxLevel);
        PlayerPrefs.SetInt(WarehouseLevelPrefsKey, warehouseLevel);
        LuuGopPrefs.Hen();
        slotCapacity = FarmInventoryManager.CapacityForLevel(warehouseLevel);
    }

    private void BuildLookups()
    {
        cropLookup.Clear();
        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null) continue;
            string key = GetHarvestItemId(crop);
            if (!string.IsNullOrEmpty(key) && !cropLookup.ContainsKey(key))
                cropLookup.Add(key, crop);
            if (!string.IsNullOrEmpty(crop.cropId))
            {
                if (!cropLookup.ContainsKey(crop.cropId))
                    cropLookup.Add(crop.cropId, crop);
                string seedKey = "seed_" + crop.cropId;
                if (!cropLookup.ContainsKey(seedKey))
                    cropLookup.Add(seedKey, crop);
            }
        }

        extraItemLookup.Clear();
        for (int i = 0; i < extraItemDatabase.Count; i++)
        {
            InventoryItemData item = extraItemDatabase[i];
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (!extraItemLookup.ContainsKey(item.itemId))
                extraItemLookup.Add(item.itemId, item);
        }

        // 🟢 VONG 14b — nap danh muc cho BuildMaterials.IconOf().
        // 5 asset nguyen lieu khong nam trong Resources/ nen IconOf() von tra null,
        // moi khung nguyen lieu deu ve o trong. extraItemDatabase o day da chua du
        // ca 5 asset nen nap thang vao, khoi phai di chuyen file.
        BuildMaterials.NapDanhMuc(extraItemDatabase);
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ClosePopup();
            }
        }
    }

    private void WireButtons()
    {
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(ClosePopup);
        }

        if (popupRoot != null)
        {
            Transform dim = popupRoot.transform.Find("Panel_Dim");
            if (dim != null)
            {
                // [2026-09-23] KHONG gan Button len Panel_Dim nua. Panel_Dim la CHA cua toan bo popup,
                // Button tren no (1) bi UIJuiceAutoAttach gan hieu ung nhun -> ca popup phong to,
                // (2) nhan ca click noi len tu ban go -> bam vao cho trong tren bang la dong popup.
                // Thay bang handler chi dong khi diem cham trung CHINH Panel_Dim.
                Button cu = dim.GetComponent<Button>();
                if (cu != null) Destroy(cu);
                var cu2 = dim.GetComponent<UIJuiceFeedback>();
                if (cu2 != null) Destroy(cu2);
                var dong = dim.GetComponent<DimClickClose>();
                if (dong == null) dong = dim.gameObject.AddComponent<DimClickClose>();
                dong.khiDong = ClosePopup;
            }
        }

        if (btnTabNongSan != null) btnTabNongSan.onClick.AddListener(() => SetCategory(WarehouseCategory.NongSan));
        if (btnTabChanNuoi != null) btnTabChanNuoi.onClick.AddListener(() => SetCategory(WarehouseCategory.ChanNuoi));
        if (btnTabMonAn != null) btnTabMonAn.onClick.AddListener(() => SetCategory(WarehouseCategory.MonAn));

        if (btnMinus != null) btnMinus.onClick.AddListener(OnMinusClicked);
        if (btnPlus != null) btnPlus.onClick.AddListener(OnPlusClicked);
        if (btnMax != null) btnMax.onClick.AddListener(OnMaxClicked);
        if (btnTransferKitchen != null) btnTransferKitchen.onClick.AddListener(OnTransferKitchenClicked);

        if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
    }

    public void OpenPopup()
    {
        if (popupRoot != null)
        {
            Transform p = popupRoot.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                    p.gameObject.SetActive(true);
                p = p.parent;
            }

            popupRoot.SetActive(true);
            EnsurePopupRaycastBlock();
            TraVeKichThuocGoc();
        }

        LoadWarehouseProgress();
        BuildLookups();
        SetCategory(currentCategory);
        // [SkinUnifier 2026-09-21] Dong bo nut/vien/ruy bang theo bo cua Shop (chi doi sprite/mau/font).
        PopupSkinUnifier.ApDung(popupRoot != null ? popupRoot.transform : transform);
        // [FIX QA] Chu vua dung xong => xin dich sang tieng Anh ngay (re, da gop chung 1 khung hinh).
        Loc.RequestRescan();
        LocFitSweeper.Sweep();   // [Loc 2026-09-21] quet chu to khung nho sau khi popup dung xong
    }

    // [2026-09-23] Nho scale/vi tri Sep dat trong Hierarchy o lan mo dau, moi lan mo tra lai y nguyen
    // (xoa scale do dang do hieu ung nhun bi SetActive(false) cat ngang de lai).
    private bool _daNhoGoc; private Vector3 _scaleRoot, _scaleDim; private Vector2 _posRoot;
    private void TraVeKichThuocGoc()
    {
        if (popupRoot == null) return;
        var rt = popupRoot.transform as RectTransform;
        var dim = popupRoot.transform.Find("Panel_Dim");
        if (!_daNhoGoc)
        {
            _daNhoGoc = true;
            _scaleRoot = popupRoot.transform.localScale;
            _scaleDim  = dim != null ? dim.localScale : Vector3.one;
            _posRoot   = rt != null ? rt.anchoredPosition : Vector2.zero;
        }
        popupRoot.transform.localScale = _scaleRoot;
        if (dim != null) dim.localScale = _scaleDim;
        if (rt != null) rt.anchoredPosition = _posRoot;
        VuaManHinh(rt, dim as RectTransform);
    }

    /// <summary>
    /// [2026-09-23] Bang kho cao ~930 (khung 866 + ruy bang) > man 16:9 thap (canvas ~850) => day bi cat.
    /// Do dau chan THAT (moi con cua Panel_Dim tru nen mo), co cho vua man + le 24, roi can giua.
    /// </summary>
    private void VuaManHinh(RectTransform rt, RectTransform dim)
    {
        if (rt == null || dim == null) return;
        var khung = rt.parent as RectTransform;
        if (khung == null) return;
        Canvas.ForceUpdateCanvases();
        bool co = false; Bounds b = new Bounds();
        for (int i = 0; i < dim.childCount; i++)
        {
            var c = dim.GetChild(i) as RectTransform;
            if (c == null || !c.gameObject.activeInHierarchy) continue;
            var bc = RectTransformUtility.CalculateRelativeRectTransformBounds(dim, c);
            if (!co) { b = bc; co = true; } else b.Encapsulate(bc);
        }
        if (!co) return;
        const float LE = 24f;
        float sx = Mathf.Abs(_scaleRoot.x) * Mathf.Abs(_scaleDim.x), sy = Mathf.Abs(_scaleRoot.y) * Mathf.Abs(_scaleDim.y);
        float rong = b.size.x * sx, cao = b.size.y * sy;
        if (rong < 1f || cao < 1f) return;
        float heSo = Mathf.Min(1f, (khung.rect.width - LE * 2f) / rong, (khung.rect.height - LE * 2f) / cao);
        popupRoot.transform.localScale = _scaleRoot * heSo;
        // tam dau chan -> tam man hinh
        Vector2 tam = new Vector2(b.center.x * sx, b.center.y * sy) * heSo;
        rt.anchoredPosition = new Vector2(-tam.x, -tam.y);   // popupRoot neo giua canvas
    }

    public void ClosePopup()
    {
        ReleasePopupInputBlock();
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public void SetCategory(WarehouseCategory category)
    {
        currentCategory = category;
        UpdateTabVisuals();

        List<WarehouseViewItem> items = GetItemsForCategory(currentCategory);
        selectedItemId = items.Count > 0 ? items[0].itemId : null;
        transferQuantity = 1;

        if (itemGridContainer != null)
        {
            ScrollRect sr = itemGridContainer.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.verticalNormalizedPosition = 1f;
        }

        RefreshUI();
    }

    private void UpdateTabVisuals()
    {
        Color activeTextColor = new Color(0.36f, 0.20f, 0.09f, 1f);   // #5B3417 bold dark brown
        Color inactiveTextColor = new Color(0.43f, 0.25f, 0.08f, 1f); // #6E4014 warm brown

        bool coTatCa = rectTabTatCa != null;
        UpdateSingleTabVisual(imgTabTatCa, txtTabTatCa, rectTabTatCa, currentCategory == WarehouseCategory.TatCa, activeTextColor, inactiveTextColor, ViTriTab(0));
        UpdateSingleTabVisual(imgTabNongSan, txtTabNongSan, rectTabNongSan, currentCategory == WarehouseCategory.NongSan, activeTextColor, inactiveTextColor, coTatCa ? ViTriTab(1) : -255f);
        UpdateSingleTabVisual(imgTabChanNuoi, txtTabChanNuoi, rectTabChanNuoi, currentCategory == WarehouseCategory.ChanNuoi, activeTextColor, inactiveTextColor, coTatCa ? ViTriTab(2) : 0f);
        UpdateSingleTabVisual(imgTabMonAn, txtTabMonAn, rectTabMonAn, currentCategory == WarehouseCategory.MonAn, activeTextColor, inactiveTextColor, coTatCa ? ViTriTab(3) : 255f);
        if (currentCategory == WarehouseCategory.TatCa && rectTabTatCa != null) rectTabTatCa.SetAsLastSibling();

        // Đảm bảo tab đang active nổi lên trên cùng, không bị tab bên cạnh đè lên viền
        if (currentCategory == WarehouseCategory.NongSan && rectTabNongSan != null) rectTabNongSan.SetAsLastSibling();
        else if (currentCategory == WarehouseCategory.ChanNuoi && rectTabChanNuoi != null) rectTabChanNuoi.SetAsLastSibling();
        else if (currentCategory == WarehouseCategory.MonAn && rectTabMonAn != null) rectTabMonAn.SetAsLastSibling();
    }

    private void UpdateSingleTabVisual(Image img, TMP_Text txt, RectTransform rect, bool isActive, Color activeColor, Color inactiveColor, float posX)
    {
        if (img != null && tabActiveSprite != null && tabInactiveSprite != null)
            img.sprite = isActive ? tabActiveSprite : tabInactiveSprite;

        if (txt != null)
            txt.color = isActive ? activeColor : inactiveColor;

        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(posX, isActive ? 0f : -6f);
        }
    }

    public void RefreshUI()
    {
        RefreshSlots();
        RefreshCapacityBar();
        RefreshDetailPanel();
        RefreshUpgradeBox();
        // [FIX QA] Chu vua dung xong => xin dich sang tieng Anh ngay (re, da gop chung 1 khung hinh).
        Loc.RequestRescan();
        LocFitSweeper.Sweep();   // [Loc 2026-09-21] quet chu to khung nho sau khi popup dung xong
    }

    private void EnsureSlotPool(int totalSlotsToRender)
    {
        if (itemGridContainer == null) return;

        if (slotPrefab != null && slotPrefab.transform.parent == itemGridContainer)
        {
            slotPrefab.SetActive(false);
        }

        if (slots.Count == 0)
        {
            var existing = itemGridContainer.GetComponentsInChildren<WarehouseSlotUI>(true);
            foreach (var s in existing)
            {
                if (slotPrefab != null && s.gameObject == slotPrefab) continue;
                s.SetSprites(slotNormalSprite, slotSelectedSprite, slotEmptySprite);
                slots.Add(s);
            }
        }

        while (slots.Count < totalSlotsToRender)
        {
            GameObject slotGO = null;
            if (slotPrefab != null)
            {
                slotGO = Instantiate(slotPrefab, itemGridContainer);
                slotGO.SetActive(true);
            }
            else
            {
                slotGO = new GameObject("slot_" + (slots.Count + 1), typeof(RectTransform));
                slotGO.transform.SetParent(itemGridContainer, false);
                slotGO.AddComponent<WarehouseSlotUI>();
            }

            WarehouseSlotUI slotUI = slotGO.GetComponent<WarehouseSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSprites(slotNormalSprite, slotSelectedSprite, slotEmptySprite);
                slots.Add(slotUI);
            }
        }
    }

    private void RefreshSlots()
    {
        if (itemGridContainer == null) return;

        List<WarehouseViewItem> categoryItems = GetItemsForCategory(currentCategory);
        int totalSlotsToRender = Mathf.Max(categoryItems.Count, minDisplaySlots);

        EnsureSlotPool(totalSlotsToRender);

        for (int i = 0; i < slots.Count; i++)
        {
            WarehouseSlotUI slotUI = slots[i];
            if (slotUI == null) continue;

            if (i < categoryItems.Count)
            {
                WarehouseViewItem item = categoryItems[i];
                bool isSelected = !string.IsNullOrEmpty(selectedItemId) &&
                                  string.Equals(selectedItemId, item.itemId, StringComparison.OrdinalIgnoreCase);
                slotUI.SetData(item.itemId, item.icon, item.amount, isSelected, OnSlotClicked);
                slotUI.gameObject.SetActive(true);
            }
            else if (i < totalSlotsToRender)
            {
                slotUI.SetEmpty();
                slotUI.gameObject.SetActive(true);
            }
            else
            {
                slotUI.gameObject.SetActive(false);
            }
        }
    }

    private void RefreshCapacityBar()
    {
        int storedKinds = 0;
        if (FarmInventoryManager.Instance != null)
            storedKinds = FarmInventoryManager.Instance.GetOrderedItems().Count;

        if (txtCapacity != null)
            txtCapacity.text = Loc.TF("{0}/{1} Slot", storedKinds, slotCapacity);

        if (imgCapacityFill != null)
        {
            float fill = slotCapacity > 0 ? Mathf.Clamp01((float)storedKinds / slotCapacity) : 0f;
            imgCapacityFill.fillAmount = fill;
        }
    }

    private void RefreshDetailPanel()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null)
        {
            if (detailPanelRoot != null) detailPanelRoot.SetActive(true);
            if (txtDetailTitle != null) txtDetailTitle.text = "Chọn vật phẩm";
            if (txtDetailDesc != null) txtDetailDesc.text = "Nhấp vào vật phẩm ở danh sách bên trái để xem thông tin chi tiết và chuyển sang bếp.";
            if (imgDetailIcon != null) { imgDetailIcon.sprite = null; imgDetailIcon.enabled = false; }
            if (txtTransferCount != null) txtTransferCount.text = "0";
            if (btnMinus != null) btnMinus.interactable = false;
            if (btnPlus != null) btnPlus.interactable = false;
            if (btnMax != null) btnMax.interactable = false;
            if (btnTransferKitchen != null) btnTransferKitchen.interactable = false;
            return;
        }

        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        if (available <= 0)
        {
            selectedItemId = null;
            RefreshDetailPanel();
            return;
        }

        transferQuantity = Mathf.Clamp(transferQuantity, 1, available);

        string displayName = GetItemDisplayName(selectedItemId);
        Sprite icon = GetItemIcon(selectedItemId);
        string description = GetItemDescription(selectedItemId);

        if (txtDetailTitle != null)
            txtDetailTitle.text = Loc.TF("{0} · x{1}", displayName, available);

        if (imgDetailIcon != null)
        {
            imgDetailIcon.sprite = icon;
            imgDetailIcon.enabled = icon != null;
        }

        if (txtDetailDesc != null)
            txtDetailDesc.text = description;

        if (txtTransferCount != null)
            txtTransferCount.text = transferQuantity.ToString();

        bool canTransfer = IsTransferrableToKitchen(selectedItemId);
        if (btnMinus != null) btnMinus.interactable = canTransfer && transferQuantity > 1;
        if (btnPlus != null) btnPlus.interactable = canTransfer && transferQuantity < available;
        if (btnMax != null) btnMax.interactable = canTransfer && transferQuantity < available;
        if (btnTransferKitchen != null)
        {
            btnTransferKitchen.interactable = canTransfer && available > 0;

            // Sếp 2026-08-27: nút phải TỰ NÓI lý do khi không gửi được (hạt giống/món ăn/đồ
            // linh tinh) — tránh hiểu nhầm "gửi bếp bị hỏng" khi thực ra item không nấu được.
            var lbl = btnTransferKitchen.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (lbl != null) lbl.text = canTransfer ? "CHUYỂN BẾP" : "KHÔNG PHẢI ĐỒ NẤU";
        }
        Debug.Log($"[WarehousePopupUI] Chọn '{selectedItemId}' → gửi bếp: {(canTransfer ? "ĐƯỢC" : "KHÔNG (không phải nguyên liệu nấu)")}");
    }

    private void RefreshUpgradeBox()
    {
        if (txtUpgradeInfo != null)
        {
            if (warehouseLevel < WarehouseMaxLevel)
            {
                int nextCap = FarmInventoryManager.CapacityForLevel(warehouseLevel + 1);
                // 🟢 V11 — hiện luôn chi phí nâng cấp (vàng + nguyên liệu) để người chơi biết cần gom gì
                txtUpgradeInfo.text = Loc.TF("Cấp {0} · Sức chứa: {1} Slot (+25)", warehouseLevel, slotCapacity)
                                      + "\n" + WarehouseUpgradeCostTable.Describe(warehouseLevel);
            }
            else
            {
                txtUpgradeInfo.text = Loc.TF("Cấp Tối Đa ({0}) · Sức chứa: {1} Slot", warehouseLevel, slotCapacity);
            }
        }

        // 🟢 VONG 14b — KHONG disable nut khi thieu nguyen lieu nua.
        // Truoc day thieu do la nut xam, bam khong ra gi, nguoi choi khong biet thieu cai gi.
        // Gio luon bam duoc (tru khi da max cap) de con mo khung xem con thieu gi.
        if (btnUpgrade != null)
            btnUpgrade.interactable = warehouseLevel < WarehouseMaxLevel;
    }

    private void AutoSelectFirstItem()
    {
        List<WarehouseViewItem> items = GetItemsForCategory(currentCategory);
        if (items.Count > 0)
        {
            selectedItemId = items[0].itemId;
            transferQuantity = 1;
        }
        else
        {
            selectedItemId = null;
            transferQuantity = 1;
        }
        RefreshUI();
    }

    private void OnSlotClicked(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        selectedItemId = itemId;
        transferQuantity = 1;
        RefreshUI();
    }

    private void OnMinusClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        transferQuantity = Mathf.Max(1, transferQuantity - 1);
        RefreshDetailPanel();
    }

    private void OnPlusClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        transferQuantity = Mathf.Min(available, transferQuantity + 1);
        RefreshDetailPanel();
    }

    private void OnMaxClicked()
    {
        if (string.IsNullOrEmpty(selectedItemId) || FarmInventoryManager.Instance == null) return;
        int available = FarmInventoryManager.Instance.GetAmount(selectedItemId);
        transferQuantity = Mathf.Max(1, available);
        RefreshDetailPanel();
    }

    private void OnTransferKitchenClicked()
    {
        if (string.IsNullOrWhiteSpace(selectedItemId) || transferQuantity <= 0) return; // WhiteSpace: chặn cả id ' ' (Sếp 2026-08-27)
        if (!IsTransferrableToKitchen(selectedItemId))
        {
            Debug.LogWarning($"[WarehousePopupUI] '{selectedItemId}' không phải là nguyên liệu nấu ăn, không thể chuyển sang Bếp!");
            return;
        }
        if (FarmInventoryManager.Instance == null) return;

        // ══ BUG GỐC (có từ trước, tìm ra 2026-08-27): RemoveItem bắn sự kiện "kho đổi" →
        // RefreshUI chạy NGAY GIỮA hàm này → khi chuyển HẾT SẠCH một món (bấm MAX), món đó
        // biến khỏi danh sách → selectedItemId bị reset thành null TRƯỚC khi kịp cộng vào
        // bếp → AddTransferredItem(null) lặng lẽ bỏ qua → "Đã chuyển Nx ''" và hàng bốc hơi
        // (kho nông trại đã trừ, bếp không nhận). Gửi 1 cái thì không sao vì món chưa hết.
        // FIX: CHỐT id + số lượng vào biến cục bộ TRƯỚC khi đụng RemoveItem.
        string rawId = selectedItemId;
        string kitchenId = KitchenIdMap.NormalizeFarmId(rawId);

        int available = FarmInventoryManager.Instance.GetAmount(rawId);
        int amountToTransfer = Mathf.Min(transferQuantity, available);
        if (amountToTransfer <= 0) return;

        // Deduct from farm inventory (có thể gọi ngược RefreshUI — từ đây chỉ dùng biến cục bộ)
        bool removed = FarmInventoryManager.Instance.RemoveItem(rawId, amountToTransfer);
        if (removed)
        {
            // Add to kitchen transfer — lưu id CHUẨN (itemId của InventoryItemData) để bếp
            // nhận diện được cả các món có id kho khác id item (vd 'nam' → 'mushroom').
            if (KitchenTransferManager.Instance != null)
                KitchenTransferManager.Instance.AddTransferredItem(kitchenId, amountToTransfer);

            Debug.Log($"[WarehousePopupUI] Đã chuyển {amountToTransfer}x '{rawId}' (id bếp '{kitchenId}') sang Bếp thành công!");
        }

        // Refresh UI
        int remain = FarmInventoryManager.Instance.GetAmount(rawId);
        if (remain <= 0)
            AutoSelectFirstItem();
        else
        {
            transferQuantity = Mathf.Clamp(transferQuantity, 1, remain);
            RefreshUI();
        }
    }

    private void OnUpgradeClicked()
    {
        if (warehouseLevel >= WarehouseMaxLevel) return;

        // 🟢 VONG 14b — MO KHUNG NGUYEN LIEU thay vi bao loi bang mot dong chu.
        // Truoc day: thieu do -> ShowUpgradeBlockedMessage() roi RefreshUI() ngay dong
        // sau -> RefreshUpgradeBox() ghi de text trong CUNG MOT FRAME -> nguoi choi chi
        // thay chu nhap nhay. Va nut con bi disable nen bam khong ra gi.
        // Gio bam la hien khung: can gi, dang co bao nhieu, thieu cai nao.
        var khung = WarehouseUpgradeReqUI.LayHoacTao(transform, txtUpgradeInfo != null ? txtUpgradeInfo.font : null);
        if (khung != null)
        {
            khung.Mo(warehouseLevel, ThucHienNangCap);
            return;
        }

        ThucHienNangCap();
    }

    /// <summary>Nang cap that su. Tach ra de khung nguyen lieu goi lai duoc.</summary>
    private void ThucHienNangCap()
    {
        if (warehouseLevel >= WarehouseMaxLevel) return;

        // 🟢 V11 — nâng cấp kho giờ TỐN VÀNG + NGUYÊN LIỆU tàu lửa mang về.
        // Trước đây nâng miễn phí nên gỗ/đá/kính/đinh không có chỗ tiêu.
        if (!WarehouseUpgradeCostTable.CanAfford(warehouseLevel, out string reason))
        {
            Debug.Log($"[WarehousePopupUI] Chưa nâng cấp được: {reason}");
            ShowUpgradeBlockedMessage(reason);
            RefreshUI();
            return;
        }

        if (!WarehouseUpgradeCostTable.TryPay(warehouseLevel))
        {
            Debug.LogWarning("[WarehousePopupUI] Trừ chi phí nâng cấp thất bại — huỷ nâng cấp.");
            RefreshUI();
            return;
        }

        warehouseLevel++;
        SaveWarehouseProgress();
        Debug.Log($"[WarehousePopupUI] Nâng cấp kho lên Cấp {warehouseLevel} " +
                  $"(Sức chứa: {slotCapacity} Slot). Chi phí đã trừ.");

        RefreshUI();
    }

    /// <summary>Hiện lý do chưa nâng cấp được lên nhãn thông tin (nếu có).</summary>
    private void ShowUpgradeBlockedMessage(string reason)
    {
        if (txtUpgradeInfo != null) txtUpgradeInfo.text = reason;
    }

    // ── Item Classification & Data Helpers ────────────────────────────────────

    private List<WarehouseViewItem> GetItemsForCategory(WarehouseCategory category)
    {
        List<WarehouseViewItem> result = new List<WarehouseViewItem>();
        if (FarmInventoryManager.Instance == null) return result;

        var allItems = FarmInventoryManager.Instance.GetOrderedItems();

        foreach (var kv in allItems)
        {
            string id = kv.Key;
            int amount = kv.Value;
            if (amount <= 0) continue;
            if (string.IsNullOrWhiteSpace(id))
            {
                // Entry hỏng trong save (id rỗng) — không hiển thị để khỏi bấm gửi nhầm nữa.
                Debug.LogWarning($"[WarehousePopupUI] Kho có entry id RỖNG (x{amount}) trong save — đã ẩn khỏi danh sách. [Sếp 2026-08-27]");
                continue;
            }

            WarehouseCategory itemCat = ClassifyItem(id);
            if (category == WarehouseCategory.TatCa || itemCat == category)
            {
                result.Add(new WarehouseViewItem
                {
                    itemId = id,
                    displayName = GetItemDisplayName(id),
                    icon = GetItemIcon(id),
                    amount = amount
                });
            }
        }

        return result;
    }

    private WarehouseCategory ClassifyItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return WarehouseCategory.NongSan;

        string key = itemId.Trim().ToLowerInvariant();

        // 0. [2026-09-23 Sep] Vat lieu xay dung (go, da, kinh, dinh, son...) KHONG phai nong san
        //    -> xep sang tab thu 2 (Chan nuoi / Livestock) theo yeu cau.
        if (key == "go" || key == "da" || key == "kinh" || key == "dinh" || key == "son" ||
            key == "wood" || key == "stone" || key == "glass" || key == "nail" || key == "paint" ||
            key.StartsWith("vatlieu_") || key.StartsWith("mat_"))
            return WarehouseCategory.ChanNuoi;

        // 1. Check Cooked Dish or Processed Good FIRST (Ưu tiên món ăn lên hàng đầu để các món như "trứng chiên", "bò xào" không bị nuốt vào chăn nuôi)
        if (IsCookedDish(key) || key.StartsWith("item_") || key.StartsWith("dish_") ||
            key.Contains("chien") || key.Contains("xao") || key.Contains("ham") || key.Contains("luoc") ||
            key.Contains("nuong") || key.Contains("sup") || key.Contains("canh") || key.Contains("salad") ||
            key.Contains("pho_") || key.Contains("com_") || key.Contains("banh") || key.Contains("che_bien") ||
            key.Contains("nuoc_mia") || key.Contains("bot_gao") || key.Contains("pho_mai") || key.Contains("op_la"))
        {
            return WarehouseCategory.MonAn;
        }

        // 2. Check StallItemCatalog
        if (StallItemCatalog.Instance != null)
        {
            StallItemCategory cat = StallItemCatalog.Instance.GetCategory(key);
            if (cat == StallItemCategory.NongSan || cat == StallItemCategory.Hoa || cat == StallItemCategory.HatGiong)
                return WarehouseCategory.NongSan;
        }

        // 3. Check Animal product (CHỈ nguyên liệu chăn nuôi thô, dùng so sánh chính xác id để không bắt nhầm món ăn)
        if (AnimalItemIds.Contains(key) ||
            key == "egg" || key == "milk" || key == "beef" || key == "pork" || key == "chicken" ||
            key == "trung" || key == "sua" || key == "thit_bo" || key == "thit_heo" || key == "thit_ga" ||
            key == "long_vu" || key == "chickenmeat" || key == "chicken_meat" ||
            key.StartsWith("cam_") || key.StartsWith("co_tron"))
        {
            return WarehouseCategory.ChanNuoi;
        }

        // 4. Check Crop Database
        if (cropLookup.ContainsKey(key))
        {
            return WarehouseCategory.NongSan;
        }

        // Default to NongSan
        return WarehouseCategory.NongSan;
    }

    private bool IsCookedDish(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        string key = itemId.Trim().ToLowerInvariant();

        for (int i = 0; i < cookedDishIds.Count; i++)
        {
            if (string.IsNullOrEmpty(cookedDishIds[i])) continue;
            if (string.Equals(cookedDishIds[i].Trim(), key, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private string GetItemDisplayName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "";

        string key = itemId.Trim().ToLowerInvariant();

        if (StallItemCatalog.Instance != null)
        {
            string catName = StallItemCatalog.Instance.GetDisplayName(key);
            if (!string.IsNullOrEmpty(catName)) return catName;
        }

        string cleanCropKey = key.StartsWith("seed_") ? key.Substring(5) : (key.StartsWith("seed") ? key.Substring(4) : key);
        if ((cropLookup.TryGetValue(key, out CropData crop) || cropLookup.TryGetValue(cleanCropKey, out crop)) && crop != null)
        {
            if (key.StartsWith("seed_") || key.StartsWith("seed"))
            {
                string baseName = !string.IsNullOrEmpty(crop.displayName) ? crop.displayName : crop.cropId;
                return "Hạt giống " + baseName;
            }
            if (!string.IsNullOrEmpty(crop.displayName)) return crop.displayName;
            if (!string.IsNullOrEmpty(crop.cropId)) return crop.cropId;
        }

        if (extraItemLookup.TryGetValue(key, out InventoryItemData extra) && extra != null)
        {
            if (!string.IsNullOrEmpty(extra.displayName)) return extra.displayName;
            if (!string.IsNullOrEmpty(extra.itemId)) return extra.itemId;
        }

        return FormatFallbackName(itemId);
    }

    private Sprite GetItemIcon(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;

        string key = itemId.Trim().ToLowerInvariant();

        // 1. Master StallItemCatalog
        if (StallItemCatalog.Instance != null)
        {
            Sprite catIcon = StallItemCatalog.Instance.GetIcon(key);
            if (catIcon != null) return catIcon;
        }

        // 2. Crop Lookup (seed -> itemIcon; harvest -> harvestIcon > readySprite > itemIcon)
        Sprite directIcon = GetDirectIconFromDatabases(key);
        if (directIcon != null) return directIcon;

        // 4. OrderBoard resolver
        Sprite obIcon = OrderBoardIconResolver.GetIcon(key);
        if (obIcon != null) return obIcon;

        return null;
    }

    /// <summary>Tra cứu icon trực tiếp từ cơ sở dữ liệu CropData và InventoryItemData đã nạp sẵn.</summary>
    public Sprite GetDirectIconFromDatabases(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        string key = itemId.Trim().ToLowerInvariant();
        string cleanCropKey = key.StartsWith("seed_") ? key.Substring(5) : (key.StartsWith("seed") ? key.Substring(4) : key);
        if ((cropLookup.TryGetValue(key, out CropData crop) || cropLookup.TryGetValue(cleanCropKey, out crop)) && crop != null)
        {
            if (key.StartsWith("seed_") || key.StartsWith("seed"))
            {
                if (crop.itemIcon != null) return crop.itemIcon;
            }
            if (crop.harvestIcon != null) return crop.harvestIcon;
            if (crop.readySprite != null) return crop.readySprite;
            if (crop.itemIcon != null) return crop.itemIcon;
        }

        if (extraItemLookup.TryGetValue(key, out InventoryItemData extra) && extra != null)
        {
            if (extra.icon != null) return extra.icon;
        }

        return null;
    }

    /// <summary>
    /// Bảng id NẤU ĐƯỢC — đã xác minh từng asset InventoryItemData (itemId + cookingData
    /// không rỗng, 2026-08-27). Đây là FALLBACK khi extraItemDatabase trong scene chưa nạp
    /// đủ (Editor giữ scene cũ trong RAM khi file bị sửa ngoài — bug Sếp gặp: 'bapcai' bị
    /// từ chối dù id đúng). Luật Sếp chốt: nông sản trồng ruộng + đồ chăn nuôi (trứng, thịt,
    /// sữa, cá) + nguyên liệu mua chợ (nước mắm...) đều gửi bếp được; hạt giống, vật liệu,
    /// công trình, món ăn thành phẩm thì KHÔNG. Thêm nguyên liệu mới → thêm id vào đây HOẶC
    /// chỉ cần gán cookingData cho asset (nhánh tra database phía dưới tự nhận).
    /// </summary>
    private static readonly HashSet<string> CookableIdsVerified = new HashSet<string>
    {
        // nông sản trồng ruộng
        "bapcai", "cachua", "khoaitay", "carot", "ngo", "sugarcane", "rice", "chili",
        "pepper", "lemon", "mushroom",
        // chăn nuôi
        "beef", "pork", "chicken_meat", "egg", "milk",
        // gia vị / nguyên liệu mua chợ
        "fishsauce", "salt", "soysauce", "herbs", "sugar",
        // quả to (2026-08-27) — đã có IngredientData nên bếp nấu được
        "pumpkin", "watermelon",
    };

    private bool IsTransferrableToKitchen(string itemId)
    {
        // Sếp 2026-08-27 — DANH SÁCH CHO PHÉP (cổng cũ chỉ cấm công trình/hoa nên hạt giống
        // và entry id rỗng lọt qua, item "bốc hơi"). Điều kiện: item có cookingData (tra
        // database) HOẶC nằm trong bảng đã xác minh ở trên (miễn nhiễm scene cũ trong RAM).
        if (string.IsNullOrWhiteSpace(itemId)) return false;
        string raw = KitchenIdMap.NormalizeFarmId(itemId); // alias đã xác minh: 'nam' → 'mushroom'

        InventoryItemData item;
        if (!extraItemLookup.TryGetValue(raw, out item) || item == null)
            extraItemLookup.TryGetValue(raw.ToLowerInvariant(), out item);

        if (item != null && item.cookingData != null)
            return true;

        return CookableIdsVerified.Contains(raw.ToLowerInvariant());
    }

    private string GetItemDescription(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return "";

        string key = itemId.Trim().ToLowerInvariant();

        if (!IsTransferrableToKitchen(itemId))
        {
            if (key.StartsWith("seed_"))
                return "Hạt giống dùng để gieo trồng tại các ô đất nông trại. Không dùng làm nguyên liệu nấu ăn.";
            if (key.StartsWith("hoa_") || key.Contains("flower") || (cropLookup.TryGetValue(key, out var c) && c.cropCategory == CropCategory.Flower))
                return "Hoa trang trí và cảnh quan nông trại. Không chuyển vào bếp nấu ăn.";
            if (key.StartsWith("building_") || key.StartsWith("deco_"))
                return "Công trình / Trang trí nông trại. Không chuyển vào bếp.";
        }

        if (cropLookup.TryGetValue(key, out CropData crop) && crop != null)
        {
            int sellGold = crop.sellGold > 0 ? crop.sellGold : 12;
            return Loc.TF("Nguyên liệu nông sản tươi ngon. Dùng để nấu ăn tại bếp hoặc bán tại quầy. Giá tham khảo {0} vàng/cái.", sellGold);
        }

        if (key.StartsWith("cam_") || key.Contains("co_tron"))
        {
            return "Thức ăn chăn nuôi chất lượng cao dùng cho gia súc, gia cầm trong chuồng.";
        }

        if (AnimalItemIds.Contains(key))
        {
            return "Nông phẩm chăn nuôi chất lượng cao. Cần thiết cho các món ăn dinh dưỡng tại bếp.";
        }

        if (IsCookedDish(key))
        {
            return "Món ăn đã được chế biến thơm ngon. Dùng để phục vụ thực khách tại nhà hàng.";
        }

        return "Vật phẩm lưu trữ trong kho nông trại. Dùng cho chế biến và hoàn thành đơn hàng.";
    }

    private string FormatFallbackName(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        string cleaned = id.Replace("seed_", "").Replace("item_", "").Replace("_", " ");
        if (cleaned.Length > 0)
            return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        return id;
    }

    private string GetHarvestItemId(CropData crop)
    {
        if (crop == null) return "";
        return string.IsNullOrEmpty(crop.harvestItemId) ? crop.cropId : crop.harvestItemId;
    }

    private void EnsurePopupRaycastBlock()
    {
        if (popupRoot == null) return;
        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        if (popupRoot != null)
            FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    private class WarehouseViewItem
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int amount;
    }
}
