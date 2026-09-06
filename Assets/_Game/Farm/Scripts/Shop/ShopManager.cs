using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("UI Roots")]
    public GameObject shopPanel;
    public Transform contentParent;
    public GameObject itemPrefab;
    public TMP_InputField searchBar;
    public Button btnClose;

    [Header("Tab Buttons")]
    public Button btnTabSeed;
    public Button btnTabBuilding;
    public Button btnTabDecor;

    [Header("Tab Images")]
    public Image imgTabSeed;
    public Image imgTabBuilding;
    public Image imgTabDecor;

    [Header("Tab Texts")]
    public TMP_Text txtTabSeed;
    public TMP_Text txtTabBuilding;
    public TMP_Text txtTabDecor;

    [Header("Tab Sprites")]
    public Sprite tabActiveSprite;
    public Sprite tabInactiveSprite;

    [Header("Currency Displays")]
    public TMP_Text txtGoldBalance;
    public TMP_Text txtGemBalance;

    [Header("Toast Notification")]
    public GameObject toastRoot;
    public TMP_Text txtToast;

    [Header("Dữ liệu theo Tab")]
    public List<BaseItemData> seedList = new List<BaseItemData>();
    public List<BaseItemData> buildingList = new List<BaseItemData>();
    public List<BaseItemData> decorList = new List<BaseItemData>();

    // ── [Decor5] AN MON DECOR CHUA CO ART 5 STAGE ─────────────────
    // 15/19 decor co du bo art 5 stage nen mua ve la co cam giac XAY (vat lieu roi ->
    // xay nua -> hoan thien -> hop qua -> phao hoa). 4 mon con lai chua duoc ve art,
    // DecorGrowthConfig.ShouldApply() tra false cho chung nen mua xong no dat THANG ra
    // world ⇒ trai nghiem lech han so voi 15 mon kia, nguoi choi tuong la loi.
    //
    // Co nay chi AN O SHOP. decorList KHONG bi doi: PlacementManager.FindItemById va
    // ConstructionManager.FindItemById van tra cuu qua chinh list nay khi khoi phuc
    // do da dat tu PlayerPrefs ⇒ ai lo mua roi thi mon trong world van con nguyen.
    //
    // TU PHUC HOI: dieu kien an doc DONG tu DecorGrowthConfig, KHONG hard-code id nao.
    // Sep nap art xong, chay Tools/Farm/DecorStageArtTool de them stageSet la mon do
    // TU HIEN LAI, khong phai sua dong code nao.
    [Header("[Decor5] An mon decor thieu art")]
    [Tooltip("Bat: an khoi shop nhung decor CHUA co du bo art 5 stage trong DecorGrowthConfig.\n" +
             "Tat: hien lai toan bo. Dieu kien doc dong tu config nen nap art xong mon tu hien lai.")]
    public bool anMonThieuArt = true;

    private List<BaseItemData> currentActiveList;
    private int currentTabIndex = 0;
    private bool popupInputLockHeld;
    private Coroutine toastRoutine;

    // ── Nhớ sortingOrder gốc của Canvas cha khi mở Shop ───────────────────────
    // OpenShop() phải TẠM nâng/hạ order của Canvas cha (Canvas_Popup) để lớp phủ
    // hướng dẫn (Tutorial_Canvas) vẽ được ĐÈ LÊN shop trong bước tutorial L2.
    // Trước đây hàm ghi đè thẳng số 150 và KHÔNG BAO GIỜ trả lại giá trị cũ, nên
    // sau lần mở shop đầu tiên Canvas_Popup kẹt vĩnh viễn ở 150: mọi popup hệ thống
    // khác nằm chung Canvas_Popup bị tụt xuống dưới các panel khác cho tới khi
    // load lại scene. Ba field dưới đây giữ đúng Canvas nào đã bị sửa và giá trị
    // cũ của nó, để CloseShop() trả lại nguyên trạng.
    private Canvas canvasDaDoiOrder;      // Canvas cha đã bị đổi order (thường là Canvas_Popup)
    private int    orderGocCuaCanvasCha;  // giá trị sortingOrder trước khi shop đụng vào
    private bool   dangGiuOrderShop;      // true = đang mượn order, chưa trả lại

    public bool IsOpen => shopPanel != null && shopPanel.activeSelf;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);
        if (toastRoot != null) toastRoot.SetActive(false);

        if (searchBar != null)
            searchBar.onValueChanged.AddListener(OnSearchTextChanged);

        if (btnClose != null)
            btnClose.onClick.AddListener(CloseShop);

        // Đóng shop khi bấm vào màn tối ngoài khung popup (nếu có Panel_Dim)
        Transform dimTrans = shopPanel != null ? shopPanel.transform.Find("Panel_Dim") : null;
        if (dimTrans != null)
        {
            Button dimBtn = dimTrans.GetComponent<Button>();
            if (dimBtn == null) dimBtn = dimTrans.gameObject.AddComponent<Button>();
            dimBtn.onClick.RemoveAllListeners();
            dimBtn.onClick.AddListener(CloseShop);
        }

        if (btnTabSeed != null)
            btnTabSeed.onClick.AddListener(() => ShowTab(0));
        if (btnTabBuilding != null)
            btnTabBuilding.onClick.AddListener(() => ShowTab(1));
        if (btnTabDecor != null)
            btnTabDecor.onClick.AddListener(() => ShowTab(2));

        LogMonBiAn();
    }

    private void OnEnable()
    {
        RefreshCurrencyBalances();
        ShowTab(currentTabIndex);
    }

    private static bool IsShopTutorialStep()
    {
        var n = TutorialManager.Instance != null ? TutorialManager.Instance.CurrentStepName : null;
        return n == "L2_01_GotoShop" || n == "L2_02_UnlockCorn"
            || n == "L2_03_BuyCorn"  || n == "L2_04_CloseShop";
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void OpenShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Canvas parentCanvas = shopPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                // Chỉ ghi nhớ ở LẦN MƯỢN ĐẦU TIÊN. Nếu OpenShop() bị gọi hai lần liên
                // tiếp mà chưa CloseShop(), lần thứ hai sẽ đọc được 150 (giá trị do
                // chính ta ghi) và lưu đè lên giá trị gốc — đúng cái bẫy phải tránh.
                if (!dangGiuOrderShop)
                {
                    canvasDaDoiOrder     = parentCanvas;
                    orderGocCuaCanvasCha = parentCanvas.sortingOrder;
                    dangGiuOrderShop     = true;
                }

                // [VÒNG 17] Trước đây hardcode 150. Sau khi UILayerApplyTool nâng các Canvas
                // nhóm Panel lên 200/210/220, số 150 khiến shop TỤT XUỐNG DƯỚI kho và chợ —
                // mở shop mà bị kho đè. Nay lấy 230 = mức cao nhất của nhóm Panel:
                //     Market 220  <  SHOP 230  <  Tutorial 250  <  Popup 300
                // Vẫn giữ đúng tính chất cũ: shop nằm DƯỚI lớp Tutorial, nên lớp phủ hướng dẫn
                // của bước L2 (mua Ngô) vẫn vẽ đè lên shop được.
                parentCanvas.sortingOrder = UILayers.Panel + 3 * UILayers.BuocTrongLop;   // = 230
            }
        }
        AcquirePopupInputBlock();
        if (searchBar != null) searchBar.text = "";
        RefreshCurrencyBalances();
        ShowTab(0);
        TutorialManager.Instance?.NotifyOpenShop();
    }

    public void CloseShop()
    {
        ReleasePopupInputBlock();
        TraLaiOrderCanvasCha();
        if (shopPanel != null) shopPanel.SetActive(false);
        TutorialManager.Instance?.NotifyCloseShop();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
        // Lưới an toàn: shopPanel có thể bị tắt bằng đường khác (SetActive trực tiếp,
        // đổi scene, popup manager đóng hàng loạt) mà không đi qua CloseShop().
        // Cờ dangGiuOrderShop khiến việc gọi hai lần là vô hại.
        TraLaiOrderCanvasCha();
    }

    /// <summary>
    /// Trả sortingOrder của Canvas cha về đúng giá trị TRƯỚC khi shop mượn.
    /// Cố ý KHÔNG gán lại một hằng số nào cả: giá trị đúng là giá trị đã đọc được
    /// lúc mở, vì Canvas_Popup có thể được các hệ thống khác chỉnh trong lúc chơi.
    /// </summary>
    private void TraLaiOrderCanvasCha()
    {
        if (!dangGiuOrderShop) return;

        if (canvasDaDoiOrder != null)
            canvasDaDoiOrder.sortingOrder = orderGocCuaCanvasCha;

        canvasDaDoiOrder = null;
        dangGiuOrderShop = false;
    }

    // ── ĐÃ GỠ SetHomeMenuVisible (21/08) ─────────────────────────────────────
    //
    // Hàm cũ ẩn/hiện "HomeMenu"/"Btn_Home" mỗi lần mở/đóng shop. Hai object đó đã bị
    // XOÁ KHỎI SCENE khi chuyển sang thanh tab đáy màn hình (CỬA HÀNG / KHO / BẢNG TIN
    // CHỢ / NẤU ĂN), nên hàm chỉ còn gây hại:
    //
    //  1) GameObject.Find bị gọi từ OnDisable. Start() tắt shopPanel; ShopManager nằm
    //     TRÊN CHÍNH shopPanel nên SetActive(false) kéo OnDisable chạy ĐỒNG BỘ ngay trong
    //     lòng SetActive — Unity cấm Find trong lúc đang vô hiệu hoá cây object, bắn
    //     "Assertion failed on expression: 'go.IsActive()'" ×2 mỗi lần vào Play.
    //  2) Find theo tên không bao giờ trúng (object đã xoá) ⇒ toàn bộ hàm là no-op.
    //
    // Nếu sau này cần ẩn UI nào đó khi mở shop: thêm field [SerializeField] GameObject,
    // kéo thả trong Inspector, và BẬT/TẮT nó trong OpenShop/CloseShop — KHÔNG Find theo
    // tên, KHÔNG đụng gì trong OnDisable.

    private void AcquirePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(shopPanel, true);

        if (!popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupOpen();
            popupInputLockHeld = true;
        }
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.SetPopupRaycastBlock(shopPanel, false);

        if (popupInputLockHeld)
        {
            FarmInputLock.RegisterPopupClose();
            popupInputLockHeld = false;
        }
    }

    public void ShowTab(int tabIndex)
    {
        currentTabIndex = tabIndex;

        switch (tabIndex)
        {
            case 0: currentActiveList = seedList;     break;
            case 1: currentActiveList = buildingList; break;
            case 2: currentActiveList = decorList;    break;
            default:
                return;
        }

        UpdateTabVisuals();
        RenderItems(searchBar != null ? searchBar.text : "");
    }

    private void UpdateTabVisuals()
    {
        Color activeTextColor = new Color(0.36f, 0.20f, 0.09f, 1f);   // #5B3417
        Color inactiveTextColor = new Color(0.43f, 0.25f, 0.08f, 1f); // #6E4014

        // Tab 0: Seed
        UpdateSingleTab(imgTabSeed, txtTabSeed, currentTabIndex == 0, activeTextColor, inactiveTextColor);
        // Tab 1: Building
        UpdateSingleTab(imgTabBuilding, txtTabBuilding, currentTabIndex == 1, activeTextColor, inactiveTextColor);
        // Tab 2: Decor
        UpdateSingleTab(imgTabDecor, txtTabDecor, currentTabIndex == 2, activeTextColor, inactiveTextColor);
    }

    private void UpdateSingleTab(Image imgTab, TMP_Text txtTab, bool isActive, Color activeCol, Color inactiveCol)
    {
        if (imgTab != null)
        {
            if (isActive && tabActiveSprite != null)
                imgTab.sprite = tabActiveSprite;
            else if (!isActive && tabInactiveSprite != null)
                imgTab.sprite = tabInactiveSprite;

            RectTransform rt = imgTab.rectTransform;
            Vector2 pos = rt.anchoredPosition;
            pos.y = isActive ? 0f : -6f;
            rt.anchoredPosition = pos;
        }

        if (txtTab != null)
            txtTab.color = isActive ? activeCol : inactiveCol;
    }

    public void RefreshCurrencyBalances()
    {
        if (FarmEconomyManager.Instance != null)
        {
            if (txtGoldBalance != null)
                txtGoldBalance.text = FarmEconomyManager.Instance.Gold.ToString("N0", new System.Globalization.CultureInfo("vi-VN"));
            if (txtGemBalance != null)
                txtGemBalance.text = FarmEconomyManager.Instance.Gems.ToString("N0", new System.Globalization.CultureInfo("vi-VN"));
        }
    }

    public void ShowToast(string message)
    {
        if (toastRoot == null || txtToast == null) return;

        txtToast.text = message;
        toastRoot.SetActive(true);

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(CoHideToast(1.8f));
    }

    private IEnumerator CoHideToast(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (toastRoot != null) toastRoot.SetActive(false);
    }

    // ── Cuộn tới item (cho tutorial mua Ngô) ─────────────────────────────────

    public void ScrollItemIntoView(string itemId)
    {
        if (!IsOpen || contentParent == null || string.IsNullOrEmpty(itemId)) return;
        var sr = contentParent.GetComponentInParent<ScrollRect>();
        if (sr == null) return;

        foreach (Transform child in contentParent)
        {
            var ui = child.GetComponent<ShopItemUI>();
            if (ui != null && ui.Data != null && ui.Data.itemID == itemId)
            {
                StartCoroutine(CoScrollTo(sr, child as RectTransform));
                return;
            }
        }
    }

    private IEnumerator CoScrollTo(ScrollRect sr, RectTransform target)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        RectTransform content  = sr.content;
        RectTransform viewport = sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
        if (content == null || target == null) yield break;

        Vector3 tw    = target.TransformPoint(target.rect.center);
        Vector2 tInVp = viewport.InverseTransformPoint(tw);
        Vector2 delta = viewport.rect.center - tInVp;

        Vector2 from = content.anchoredPosition;
        Vector2 to   = from;
        if (sr.horizontal) to.x += delta.x;
        if (sr.vertical)   to.y += delta.y;

        float t = 0f; const float dur = 0.3f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            content.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        content.anchoredPosition = to;
    }

    // ── Tìm kiếm & Render Items ──────────────────────────────────────────────

    private void OnSearchTextChanged(string keyword)
    {
        RenderItems(keyword);
    }

    /// <summary>
    /// Mon decor nay co bi an khoi shop khong.
    /// Dieu kien soi guong dung nhanh DECOR cua DecorGrowthConfig.ShouldApply():
    /// he stage BAT + applyToDecor + khong nam trong excludedItemIDs + KHONG co
    /// stageSet hop le ⇒ an. Moi ve trai deu doc dong tu config.
    /// </summary>
    private bool BiAnViThieuArt(BaseItemData item)
    {
        if (!anMonThieuArt) return false;

        // Chi xet DecorData. Cong trinh / chuong / may chay WORKER-ONLY, khong can art.
        DecorData decor = item as DecorData;
        if (decor == null) return false;

        DecorGrowthConfig cfg = DecorGrowthBootstrap.Config;

        // He 5 stage TAT (chua co asset, hoac Sep bo tick enabled) ⇒ KHONG an gi ca:
        // luc do MOI mon deu dat thang, khong mon nao bi lech trai nghiem.
        if (cfg == null || !cfg.enabled) return false;
        if (!cfg.applyToDecor) return false;

        int id = DecorGrowthConfig.ItemIdOf(decor);

        // Loai tru tuong minh (Dat, Chau Hoa 1..4): dat thang la DUNG thiet ke, dung an.
        if (cfg.IsExcludedItem(id)) return false;

        DecorStageSet set = cfg.FindSet(id);
        return set == null || !set.IsValid;
    }

    /// <summary>Log 1 dong luc khoi dong: da an nhung mon nao khoi shop.</summary>
    private void LogMonBiAn()
    {
        if (decorList == null) return;

        var ten = new List<string>();
        for (int i = 0; i < decorList.Count; i++)
        {
            BaseItemData it = decorList[i];
            if (it == null) continue;
            if (BiAnViThieuArt(it)) ten.Add(string.IsNullOrEmpty(it.itemName) ? it.itemID : it.itemName);
        }

        if (ten.Count > 0)
        {
            Debug.Log("[Shop] An " + ten.Count + " mon decor thieu art 5 stage: " + string.Join(", ", ten));
        }
    }

    private void RenderItems(string keyword)
    {
        if (contentParent == null) return;

        // Xoá các card đã sinh trước đó
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Transform child = contentParent.GetChild(i);
            if (child.gameObject != itemPrefab)
                Destroy(child.gameObject);
        }

        if (currentActiveList == null || itemPrefab == null)
            return;

        string keyLower = string.IsNullOrEmpty(keyword) ? "" : keyword.ToLower().Trim();

        // 2026-08-27 (Sếp): "hạt giống ra hạt giống, hoa ra hoa" + xếp theo độ hiếm.
        //   1. Nhóm: hạt rau củ TRƯỚC, hạt hoa SAU (hết cảnh xen kẽ theo cấp như trước).
        //   2. Trong nhóm: theo cấp mở khoá → cây mua được luôn nằm trên cây còn khoá.
        //   3. Cùng cấp: cây lâu chín/hiếm hơn xếp sau (growSeconds là thước đo độ hiếm
        //      sẵn có trong data — trùng khớp với thứ tự "cây cho món khó thì xếp sau").
        int cellCount = 0;
        int lastGroup = -1;

        foreach (BaseItemData item in currentActiveList
                                        .OrderBy(GroupRank)
                                        .ThenBy(GetUnlockLevel)
                                        .ThenBy(RarityRank)
                                        .ThenBy(NameKey))
        {
            if (item == null) continue;

            // An mon decor chua co art 5 stage. CHI bo qua o hien thi, KHONG dung
            // decorList nen do da dat trong world khong he bi anh huong.
            if (BiAnViThieuArt(item)) continue;

            bool match = string.IsNullOrEmpty(keyLower)
                      || (item.itemName != null && item.itemName.ToLower().Contains(keyLower));

            if (!match) continue;

            // Sang nhóm khác (rau củ → hoa): chèn ô trống cho hết hàng đang dở, để nhóm
            // mới BẮT ĐẦU Ở HÀNG MỚI. Grid ô 296×335 nên không đặt được dòng tiêu đề
            // (một ô tiêu đề sẽ cao 335px, trông như lỗi) — khoảng trống là cách phân
            // nhóm gọn nhất mà không phải sửa scene. [Sếp 2026-08-27]
            int group = GroupRank(item);
            if (lastGroup >= 0 && group != lastGroup)
                PadRowWithSpacers(ref cellCount);
            lastGroup = group;

            GameObject go = Instantiate(itemPrefab, contentParent);
            go.SetActive(true);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
                ui.Setup(item);
            cellCount++;
        }
    }

    /// <summary>Số cột của GridLayoutGroup đang dùng cho danh sách (mặc định 4).</summary>
    private int GridColumns()
    {
        GridLayoutGroup grid = contentParent != null
                             ? contentParent.GetComponent<GridLayoutGroup>() : null;
        if (grid != null && grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, grid.constraintCount);
        return 4;
    }

    /// <summary>Chèn ô rỗng (không vẽ gì) cho tới hết hàng hiện tại.</summary>
    private void PadRowWithSpacers(ref int cellCount)
    {
        int cols = GridColumns();
        int guard = 0;
        while (cellCount % cols != 0 && guard++ < 16)
        {
            GameObject spacer = new GameObject("GroupSpacer", typeof(RectTransform));
            spacer.transform.SetParent(contentParent, false);
            cellCount++;
        }
    }

    /// <summary>0 = hạt rau củ, 1 = hạt hoa, 2 = còn lại (công trình/trang trí).</summary>
    private static int GroupRank(BaseItemData item)
    {
        CropData crop = item as CropData;
        if (crop == null) return 2;
        return crop.cropCategory == CropCategory.Flower ? 1 : 0;
    }

    /// <summary>Độ hiếm trong cùng một cấp: thời gian lớn càng dài càng hiếm → xếp sau.</summary>
    private static int RarityRank(BaseItemData item)
    {
        CropData crop = item as CropData;
        return crop != null ? crop.growSeconds : 0;
    }

    private static string NameKey(BaseItemData item)
        => item != null && item.itemName != null ? item.itemName : "";

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

    // ── UI Raycast ──────────────────────────────────────────────────────────

    private bool IsPointerOverPopupUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            Transform t = results[i].gameObject.transform;
            Canvas parentCanvas = t.GetComponentInParent<Canvas>();

            if (parentCanvas != null && parentCanvas.name == "Canvas_Popup")
                return true;
        }

        return false;
    }
}
