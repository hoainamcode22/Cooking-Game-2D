using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  UI BẢNG TIN CHỢ (A8)
/// ══════════════════════════════════════════════════════════════════════════
///
/// Chỉ VẼ, không tính toán kinh tế — dữ liệu lấy từ MarketManager.Provider.
///
/// ⚠️ Script này KHÔNG được tạo GameObject nào ngoài việc Instantiate prefab.
/// Toàn bộ hierarchy do Tools/Farm/Chợ/Dựng lại UI Bảng Tin Chợ sinh trong Editor,
/// nên chủ dự án mở scene ra là sửa được từng ô — khác hẳn UnifiedTaskPopupUI
/// 1433 dòng dựng UI bằng code mà không ai đụng nổi.
/// </summary>
public class MarketBoardUI : MonoBehaviour
{
    [Header("Đồng hồ & làm mới")]
    [SerializeField] private TMP_Text textTimer;
    [SerializeField] private Image    fillTimer;
    [SerializeField] private Button   buttonRefresh;
    [SerializeField] private TMP_Text textRefreshCost;
    [SerializeField] private TMP_Text textRefreshLabel;
    [SerializeField] private Image    imageRefreshBackground;

    [Header("Ví")]
    [SerializeField] private TMP_Text textGold;

    [Header("Dải lọc danh mục (dọc, bên trái)")]
    [SerializeField] private RectTransform      categoryContent;
    [SerializeField] private MarketCategoryTabUI categoryTabPrefab;

    [Header("Lưới thẻ hàng")]
    [SerializeField] private RectTransform       listingContent;
    [SerializeField] private MarketListingCardUI listingCardPrefab;
    [SerializeField] private ScrollRect          listingScroll;

    [Header("Trạng thái rỗng")]
    [SerializeField] private GameObject panelEmpty;
    [SerializeField] private TMP_Text   textEmpty;

    [Header("Thông báo ngắn")]
    [SerializeField] private GameObject panelToast;
    [SerializeField] private TMP_Text   textToast;

    [Header("Đóng")]
    [SerializeField] private Button buttonClose;

    private readonly List<MarketCategoryTabUI>  spawnedTabs  = new List<MarketCategoryTabUI>();
    private readonly List<MarketListingCardUI>  spawnedCards = new List<MarketListingCardUI>();

    private MarketCategory selectedCategory = MarketCategory.All;
    private bool           tabsBuilt;
    private float          toastHideAt;

    private MarketManager  cachedManager;
    private bool           subscribed;

    // Bộ nhớ đệm để KHÔNG cấp phát chuỗi mỗi frame.
    // ToString("N0") chạy 60 lần/giây trên 2 nhãn là rác GC vô ích trong lúc popup mở.
    private int lastGoldShown       = int.MinValue;
    private int lastTimerSecond     = int.MinValue;
    private int lastRefreshCostShown = int.MinValue;

    /// <summary>
    /// Lấy manager một cách chịu lỗi.
    /// MarketManager nằm CÙNG GameObject nên Awake của nó chạy trước, nhưng nếu sau này
    /// ai đó tách hai script ra hai object thì thứ tự không còn bảo đảm — GetComponentInParent
    /// là đường lui để bảng tin không im lặng trống trơn.
    /// </summary>
    private MarketManager Manager
    {
        get
        {
            if (cachedManager != null)
                return cachedManager;

            cachedManager = MarketManager.Instance != null
                ? MarketManager.Instance
                : GetComponentInParent<MarketManager>();

            return cachedManager;
        }
    }

    /// <summary>Trễ giữa hai thẻ khi hiện so le. 12 thẻ × 0.045s ≈ 0.5s — đủ thấy, không đủ chán.</summary>
    private const float StaggerStep = 0.045f;

    private const string EmptyMessage = "CHƯA CÓ VẬT PHẨM NÀO ĐƯỢC ĐĂNG BÁN";

    // ══════════════════════════════════════════════════════════════════════
    //  VÒNG ĐỜI
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (buttonRefresh != null)
        {
            buttonRefresh.onClick.RemoveAllListeners();
            buttonRefresh.onClick.AddListener(HandleRefreshClicked);
        }

        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveAllListeners();
            buttonClose.onClick.AddListener(HandleCloseClicked);
        }

        if (textEmpty != null)
            textEmpty.text = EmptyMessage;

        if (panelToast != null)
            panelToast.SetActive(false);

        BuildCategoryTabs();
    }

    private void OnEnable()
    {
        BuildCategoryTabs();
        TrySubscribe();
        Redraw(true);
    }

    private void OnDisable()
    {
        if (subscribed && cachedManager != null)
            cachedManager.OnMarketChanged -= HandleMarketChanged;

        subscribed = false;
    }

    private void Update()
    {
        // Manager có thể chưa Awake ở frame đầu — thử lại tới khi nối được sự kiện
        if (!subscribed)
            TrySubscribe();

        UpdateTimerUI();
        UpdateGoldUI();
        UpdateToast();
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;

        MarketManager manager = Manager;
        if (manager == null)
            return;

        manager.OnMarketChanged += HandleMarketChanged;
        subscribed = true;
        Redraw(true);
    }

    // Đang trong lời gọi mua của CHÍNH màn này. Xem HandleBuyRequested để biết vì sao cần.
    private bool dangTuMua;

    private void HandleMarketChanged()
    {
        // Chỉ vẽ khi đang bật — vẽ lại lúc popup đóng vừa tốn frame vừa dễ ném lỗi coroutine
        if (!isActiveAndEnabled)
            return;

        // Bỏ qua lần báo do chính mình vừa mua. Nếu vẽ lại ngay thì thẻ bị dựng lại từ
        // danh sách mới (đã không còn món vừa mua) → MarkCardSold bên dưới không tìm thấy
        // thẻ nào, hiệu ứng "ĐÃ BÁN" không bao giờ hiện, món biến mất đột ngột.
        if (dangTuMua)
            return;

        Redraw(true);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  DẢI LỌC DANH MỤC
    // ══════════════════════════════════════════════════════════════════════

    private void BuildCategoryTabs()
    {
        if (tabsBuilt || categoryContent == null || categoryTabPrefab == null)
            return;

        // Dọn tab thừa nếu ai đó lỡ để lại trong scene
        for (int i = categoryContent.childCount - 1; i >= 0; i--)
            Destroy(categoryContent.GetChild(i).gameObject);

        spawnedTabs.Clear();

        MarketCategory[] order = MarketCategoryUtil.FilterOrder;
        for (int i = 0; i < order.Length; i++)
        {
            MarketCategoryTabUI tab = Instantiate(categoryTabPrefab, categoryContent);
            tab.gameObject.SetActive(true);
            tab.name = "Tab_" + order[i];
            tab.Bind(order[i], HandleCategorySelected);
            spawnedTabs.Add(tab);
        }

        tabsBuilt = true;
        ApplyTabSelection();
    }

    private void HandleCategorySelected(MarketCategory category)
    {
        if (selectedCategory == category)
            return;

        selectedCategory = category;
        ApplyTabSelection();

        // Đổi tab thì hiện ngay, KHÔNG chạy hiệu ứng so le: người chơi vừa chủ động
        // bấm lọc, bắt chờ nửa giây nữa là bực chứ không phải "sống động"
        Redraw(false);

        if (listingScroll != null)
            listingScroll.verticalNormalizedPosition = 1f;
    }

    private void ApplyTabSelection()
    {
        for (int i = 0; i < spawnedTabs.Count; i++)
        {
            if (spawnedTabs[i] != null)
                spawnedTabs[i].SetSelected(spawnedTabs[i].Category == selectedCategory);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LƯỚI THẺ
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Vẽ lại toàn bộ lưới. animate=false khi chỉ đổi tab.</summary>
    public void Redraw(bool animate)
    {
        if (listingContent == null || listingCardPrefab == null)
            return;

        MarketManager manager = Manager;
        IReadOnlyList<MarketListing> listings = manager != null && manager.Provider != null
            ? manager.Provider.GetListings(selectedCategory)
            : null;

        int count = listings != null ? listings.Count : 0;

        EnsureCardCount(count);

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            MarketListingCardUI card = spawnedCards[i];
            if (card == null)
                continue;

            if (i >= count)
            {
                card.gameObject.SetActive(false);
                continue;
            }

            MarketListing listing = listings[i];
            card.gameObject.SetActive(true);

            MarketItemVisual visual = manager != null
                ? manager.ResolveVisual(listing.ItemId)
                : new MarketItemVisual { DisplayName = MarketPriceTable.GetDisplayName(listing.ItemId) };

            card.Bind(listing, visual, HandleBuyRequested);

            if (animate)
                card.PlayReveal(i * StaggerStep);
            else
                card.ShowImmediate();
        }

        if (panelEmpty != null)
            panelEmpty.SetActive(count == 0);
    }

    /// <summary>
    /// Bảo đảm có đủ thẻ. TÁI DÙNG thẻ cũ thay vì Destroy + Instantiate mỗi lần vẽ:
    /// đổi tab liên tục sẽ sinh rác GC và giật khung hình trên máy yếu.
    /// </summary>
    private void EnsureCardCount(int needed)
    {
        while (spawnedCards.Count < needed)
        {
            MarketListingCardUI card = Instantiate(listingCardPrefab, listingContent);
            card.gameObject.SetActive(false);
            card.name = "Card_" + spawnedCards.Count.ToString("00");
            spawnedCards.Add(card);
        }
    }

    private void HandleBuyRequested(string listingId)
    {
        MarketManager manager = Manager;
        if (manager == null)
            return;

        // Chặn vòng vẽ lại do chính lời gọi này kích hoạt (xem HandleMarketChanged).
        // try/finally để dù TryBuyListing ném lỗi thì cờ vẫn được trả về, không thì
        // bảng tin đứng hình vĩnh viễn vì mọi lần báo sau đều bị bỏ qua.
        MarketBuyResult result;
        dangTuMua = true;
        try     { result = manager.TryBuyListing(listingId); }
        finally { dangTuMua = false; }

        switch (result)
        {
            case MarketBuyResult.Success:
                Sprite boughtSprite = null;
                Vector3 startPos = Vector3.zero;
                for (int i = 0; i < spawnedCards.Count; i++)
                {
                    if (spawnedCards[i] != null && spawnedCards[i].ListingId == listingId)
                    {
                        boughtSprite = spawnedCards[i].ItemSprite;
                        startPos = spawnedCards[i].IconScreenPosition;
                        break;
                    }
                }

                MarkCardSold(listingId);
                ShowToast("Đã mua!");

                if (boughtSprite != null)
                {
                    StartCoroutine(PlayBuyItemFlyToWarehouse(boughtSprite, startPos));
                }
                break;

            case MarketBuyResult.NotEnoughGold:
                ShowToast("Không đủ vàng");
                break;

            case MarketBuyResult.OwnListing:
                ShowToast("Đây là hàng bạn đang bán");
                break;

            case MarketBuyResult.InventoryMissing:
                ShowToast("Kho chưa sẵn sàng");
                break;

            // TESTER-F8 — không gộp vào default: default còn Redraw(false) và báo sai
            // ("vừa có người mua"), người chơi sẽ bấm mua lại mãi mà không hiểu vì sao.
            case MarketBuyResult.InventoryFull:
                ShowToast("Kho đầy — bán bớt hoặc nâng cấp kho");
                break;

            default:
                ShowToast("Món này vừa có người mua");
                Redraw(false);
                break;
        }
    }

    private void MarkCardSold(string listingId)
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null && spawnedCards[i].ListingId == listingId)
            {
                spawnedCards[i].MarkSoldOut();
                return;
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ĐỒNG HỒ + NÚT LÀM MỚI
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateTimerUI()
    {
        MarketManager manager = Manager;
        MarketRefreshTimer timer = manager != null ? manager.RefreshTimer : null;
        if (timer == null)
            return;

        int remainingSecond = Mathf.CeilToInt(timer.SecondsRemaining);
        if (textTimer != null && remainingSecond != lastTimerSecond)
        {
            textTimer.text = timer.FormatRemaining();
            lastTimerSecond = remainingSecond;
        }

        if (fillTimer != null)
            fillTimer.fillAmount = timer.Progress01;

        bool free = timer.CanRefreshFree();
        int  cost = timer.GetGoldRefreshCost();

        if (textRefreshLabel != null)
            textRefreshLabel.text = free ? "LÀM MỚI" : "LÀM MỚI NGAY";

        if (textRefreshCost != null)
        {
            // Hết giờ = miễn phí, ẩn hẳn số vàng thay vì hiện "0" gây hiểu nhầm
            textRefreshCost.gameObject.SetActive(!free);

            if (cost != lastRefreshCostShown)
            {
                textRefreshCost.text = cost.ToString("N0");
                lastRefreshCostShown = cost;
            }

            bool affordable = manager != null && manager.CanSpendGold(cost);
            textRefreshCost.color = affordable ? MarketBoardPalette.TextGold : MarketBoardPalette.ButtonDisabled;
        }

        if (imageRefreshBackground != null)
        {
            bool usable = free || (manager != null && manager.CanSpendGold(cost));
            imageRefreshBackground.color = usable
                ? MarketBoardPalette.ButtonGold
                : MarketBoardPalette.ButtonDisabled;
        }
    }

    private void UpdateGoldUI()
    {
        if (textGold == null)
            return;

        MarketManager goldManager = Manager;
        int gold = goldManager != null ? goldManager.GetPlayerGold() : 0;

        if (gold == lastGoldShown)
            return;

        textGold.text = gold.ToString("N0");
        lastGoldShown = gold;
    }

    private void HandleRefreshClicked()
    {
        MarketManager manager = Manager;
        if (manager == null)
            return;

        MarketRefreshTimer timer = manager.RefreshTimer;
        if (timer == null)
            return;

        if (timer.CanRefreshFree())
        {
            manager.RefreshNowFree();
            return;
        }

        if (!manager.RefreshNowWithGold())
        {
            ShowToast("Không đủ vàng để làm mới");
        }
    }

    private void HandleCloseClicked()
    {
        MarketManager manager = Manager;
        if (manager != null)
            manager.CloseMarketPopup();
        else
            gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  THÔNG BÁO NGẮN
    // ══════════════════════════════════════════════════════════════════════

    private void ShowToast(string message)
    {
        if (panelToast == null || textToast == null)
            return;

        textToast.text = message;
        panelToast.SetActive(true);
        toastHideAt = Time.unscaledTime + 1.6f;
    }

    private void UpdateToast()
    {
        if (panelToast == null || !panelToast.activeSelf)
            return;

        if (Time.unscaledTime >= toastHideAt)
            panelToast.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  HIỆU ỨNG MUA HÀNG: RỚT XUỐNG NẢY LÊN RỒI BAY VÀO KHO (WAREHOUSE)
    // ══════════════════════════════════════════════════════════════════════

    private System.Collections.IEnumerator PlayBuyItemFlyToWarehouse(Sprite sprite, Vector3 worldStartPos)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && rootCanvas.rootCanvas != null)
            rootCanvas = rootCanvas.rootCanvas;

        if (rootCanvas == null)
            yield break;

        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            yield break;

        // Điểm xuất phát trên Canvas
        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(uiCam, worldStartPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, uiCam, out Vector2 startLocal);

        // Đích đến: WarehouseGainToastUI hoặc vị trí mặc định trên HUD
        RectTransform warehouseTarget = null;
        if (WarehouseGainToastUI.Instance != null && WarehouseGainToastUI.Instance.PanelRect != null)
            warehouseTarget = WarehouseGainToastUI.Instance.PanelRect;

        Vector2 endScreen;
        if (warehouseTarget != null)
            endScreen = RectTransformUtility.WorldToScreenPoint(uiCam, warehouseTarget.position);
        else
            endScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.88f);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, uiCam, out Vector2 endLocal);

        // Tạo icon bay
        var flyGo = new GameObject("MarketBoughtItem_Fly", typeof(RectTransform), typeof(Image));
        flyGo.layer = rootCanvas.gameObject.layer;
        var rt = (RectTransform)flyGo.transform;
        rt.SetParent(rootCanvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(85f, 85f);
        rt.SetAsLastSibling();

        var img = flyGo.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        img.preserveAspect = true;

        // ── Pha 1: Rớt xuống nảy nhẹ (Drop & Pop Bounce) ──
        Vector2 dropPos = startLocal + new Vector2(0f, -42f);
        float dropTime = 0.22f;
        float elapsed = 0f;
        while (elapsed < dropTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(elapsed / dropTime);
            float k = FxEase.OutBackRaw(raw, 0.25f);

            rt.anchoredPosition = Vector2.LerpUnclamped(startLocal, dropPos, k);
            float s = Mathf.Lerp(0.85f, 1.25f, FxEase.OutBackRaw(raw, 0.2f));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Khựng nhẹ 0.05s
        float pause = 0.05f;
        while (pause > 0f)
        {
            pause -= Time.unscaledDeltaTime;
            yield return null;
        }

        // ── Pha 2: Bay hình vòng cung về kho (Bezier Arc Fly) ──
        Vector2 dir = endLocal - dropPos;
        Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
        float bend = Random.Range(50f, 90f) * (Random.value < 0.5f ? -1f : 1f);
        Vector2 control = (dropPos + endLocal) * 0.5f + perp * bend + new Vector2(0f, 60f);

        float flyTime = 0.55f;
        elapsed = 0f;
        while (elapsed < flyTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float raw = Mathf.Clamp01(elapsed / flyTime);
            float k = raw * raw * (3f - 2f * raw); // smoothstep

            float u = 1f - k;
            Vector2 curPos = (u * u) * dropPos + (2f * u * k) * control + (k * k) * endLocal;
            rt.anchoredPosition = curPos;

            // Thu nhỏ dần từ 1.25x -> 0.45x khi chui vào kho
            float s = Mathf.Lerp(1.25f, 0.45f, raw);
            rt.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        // ── Pha 3: Chạm kho: kích hoạt nảy kho + hiện thanh kho Toast ──
        if (WarehouseGainToastUI.Instance != null)
        {
            WarehouseGainToastUI.Instance.OnHarvestItemArrived(sprite);
        }
        if (warehouseTarget != null)
        {
            JuicyPulseFX.Play(warehouseTarget, 1.25f, 0.20f);
        }

        Destroy(flyGo);
    }
}
