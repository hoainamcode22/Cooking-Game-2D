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
    private GameObject cachedHomeMenu;

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

        if (btnTabSeed != null)
            btnTabSeed.onClick.AddListener(() => ShowTab(0));
        if (btnTabBuilding != null)
            btnTabBuilding.onClick.AddListener(() => ShowTab(1));
        if (btnTabDecor != null)
            btnTabDecor.onClick.AddListener(() => ShowTab(2));
    }

    private void OnEnable()
    {
        SetHomeMenuVisible(false);
        RefreshCurrencyBalances();
        ShowTab(currentTabIndex);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (!Input.GetMouseButtonDown(0)) return;

        // Tutorial L2 đang điều khiển shop -> Không auto-close
        if (IsShopTutorialStep()) return;

        // Nếu click ngoài vùng Canvas_Popup -> Đóng shop
        if (!IsPointerOverPopupUI(Input.mousePosition))
            CloseShop();
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
        if (shopPanel != null) shopPanel.SetActive(true);
        SetHomeMenuVisible(false);
        AcquirePopupInputBlock();
        if (searchBar != null) searchBar.text = "";
        RefreshCurrencyBalances();
        ShowTab(0);
        TutorialManager.Instance?.NotifyOpenShop();
    }

    public void CloseShop()
    {
        ReleasePopupInputBlock();
        SetHomeMenuVisible(true);
        if (shopPanel != null) shopPanel.SetActive(false);
        TutorialManager.Instance?.NotifyCloseShop();
    }

    private void OnDisable()
    {
        SetHomeMenuVisible(true);
        ReleasePopupInputBlock();
    }

    private void SetHomeMenuVisible(bool visible)
    {
        if (cachedHomeMenu == null)
        {
            cachedHomeMenu = GameObject.Find("HomeMenu");
            if (cachedHomeMenu == null)
            {
                var btn = GameObject.Find("Btn_Home");
                if (btn != null) cachedHomeMenu = btn.transform.parent != null ? btn.transform.parent.gameObject : btn;
            }
        }

        if (cachedHomeMenu != null)
            cachedHomeMenu.SetActive(visible);
    }

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

        foreach (BaseItemData item in currentActiveList.OrderBy(GetUnlockLevel))
        {
            if (item == null) continue;

            bool match = string.IsNullOrEmpty(keyLower)
                      || (item.itemName != null && item.itemName.ToLower().Contains(keyLower));

            if (!match) continue;

            GameObject go = Instantiate(itemPrefab, contentParent);
            go.SetActive(true);
            var ui = go.GetComponent<ShopItemUI>();
            if (ui != null)
                ui.Setup(item);
        }
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
