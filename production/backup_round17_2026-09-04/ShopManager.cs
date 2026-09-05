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

    private List<BaseItemData> currentActiveList;
    private int currentTabIndex = 0;
    private bool popupInputLockHeld;
    private Coroutine toastRoutine;

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
                parentCanvas.sortingOrder = 150;
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
        if (shopPanel != null) shopPanel.SetActive(false);
        TutorialManager.Instance?.NotifyCloseShop();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
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
