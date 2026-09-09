using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tên + icon để vẽ một vật phẩm. Tra một lần rồi dùng lại, không đụng AssetDatabase lúc chạy.</summary>
public struct MarketItemVisual
{
    public string DisplayName;
    public Sprite Icon;
}

/// <summary>
/// ══════════════════════════════════════════════════════════════════════════
///  BỘ NÃO CỦA CHỢ — dữ liệu, kinh tế, đóng/mở popup
/// ══════════════════════════════════════════════════════════════════════════
///
/// Phần VẼ nằm ở <see cref="MarketBoardUI"/>. Tách đôi vì bài học
/// UnifiedTaskPopupUI 1433 dòng: gộp dữ liệu với UI vào một file thì đến lúc
/// đổi bố cục là phải đọc lại cả logic kinh tế.
///
/// ── BA LỖI ĐÃ SỬA Ở BẢN NÀY ─────────────────────────────────────────────
/// LỖI 1 — popup tự đóng: MarketPopupUI.Start() gọi popupRoot.SetActive(false)
///          trong khi Start() chỉ chạy lúc popup vừa được bật. Dòng đó đã bị bỏ
///          và MarketPopupUI giờ chỉ còn là lớp vỏ uỷ quyền sang đây.
/// LỖI 2 — mua chùa: CanSpendGold cũ trả true khi FarmEconomyManager.Instance == null.
///          Giờ trả FALSE — không có ví thì không được tiêu.
/// LỖI 3 — hạt giống vào sai kho: mọi thứ đều đổ vào FarmInventoryManager.
///          Giờ phân loại qua MarketPriceTable.IsSeed (tra danh mục, KHÔNG dùng
///          StartsWith("seed") vì ca_rot / khoai_tay không có tiền tố đó).
/// </summary>
public class MarketManager : MonoBehaviour
{
    public static MarketManager Instance { get; private set; }

    [Header("Dữ liệu")]
    [SerializeField] private MarketDatabase_SO marketDatabase;
    [Tooltip("Số thẻ hàng NPC sinh ra mỗi chu kỳ. Video tham chiếu dùng lưới 4×3 = 12.")]
    [SerializeField] private int itemCountPerRefresh = 12;

    [Header("Nguồn icon / tên hiển thị")]
    [Tooltip("Tự điền bằng Tools/Farm/Chợ/Nạp lại nguồn icon cho MarketManager.")]
    [SerializeField] private List<CropData> cropDatabase = new List<CropData>();
    [Tooltip("Tự điền bằng Tools/Farm/Chợ/Nạp lại nguồn icon cho MarketManager.")]
    [SerializeField] private List<InventoryItemData> itemDatabase = new List<InventoryItemData>();

    [Header("Làm mới — CHỈ DÙNG VÀNG, không gem, không đồng tiền thứ ba")]
    [SerializeField] private int refreshDurationSeconds = 300;
    [Tooltip("Giá làm mới lần đầu trong ngày. Lần sau nhân lên theo số lần đã trả.")]
    [SerializeField] private int baseGoldRefreshCost = 150;
    [Tooltip("Trần giá làm mới trong ngày.")]
    [SerializeField] private int maxGoldRefreshCost = 900;

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Tooltip("Vào game thì ĐÓNG bảng tin chợ (scene đang lưu Panel_Dim/Popup_Board ở trạng thái bật). " +
             "Bấm vào chợ mới mở. Tắt ô này nếu muốn quay lại hành vi cũ.")]
    [SerializeField] private bool closeBoardOnSceneStart = true;
    [SerializeField] private Button buttonClose;

    private readonly Dictionary<string, MarketItemVisual> visualLookup =
        new Dictionary<string, MarketItemVisual>();

    private LocalMarketProvider provider;
    private MarketRefreshTimer  refreshTimer;
    private Coroutine           openAnimationCoroutine;
    private bool                popupInputLockHeld;

    /// <summary>Nguồn hàng. Kiểu interface để sau này đổi sang ServerMarketProvider không phải sửa UI.</summary>
    public IMarketProvider Provider => provider;

    public MarketRefreshTimer RefreshTimer => refreshTimer;

    /// <summary>Bắn khi hàng đổi (làm mới hoặc vừa mua xong) — MarketBoardUI vẽ lại.</summary>
    public event System.Action OnMarketChanged;

    // ══════════════════════════════════════════════════════════════════════
    //  VÒNG ĐỜI
    // ══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveAllListeners();
            buttonClose.onClick.AddListener(CloseMarketPopup);
        }

        if (popupRoot != null)
        {
            if (popupRoot.TryGetComponent(out UnityEngine.UI.Button dimBtn))
            {
                dimBtn.onClick.RemoveListener(CloseMarketPopup);
                dimBtn.onClick.AddListener(CloseMarketPopup);
            }
        }

        BuildVisualLookup();

        refreshTimer = new MarketRefreshTimer(refreshDurationSeconds, baseGoldRefreshCost, maxGoldRefreshCost);
        refreshTimer.OnCycleElapsed += HandleCycleElapsed;

        if (marketDatabase == null)
        {
            // Thiếu asset = chợ trống trơn mà không có dấu hiệu gì. Phải kêu lên.
            Debug.LogError("[Chợ] MarketManager chưa gán MarketDatabase — bảng tin sẽ không có hàng. " +
                           "Chạy Tools/Farm/Chợ/3 · Dựng lại UI Bảng Tin Chợ để tự gán.", this);
        }

        provider = new LocalMarketProvider(marketDatabase);
        provider.OnListingsChanged += HandleProviderChanged;

        MarketPlayerListingBridge.OnPlayerListingsChanged += HandleProviderChanged;

        // Sinh hàng NGAY ở Awake chứ không đợi mở popup: bảng tin phải có sẵn hàng
        // trước khi UI vẽ khung đầu tiên, nếu không người chơi thấy nháy "chưa có vật phẩm".
        RegenerateListings();
    }

    /// <summary>
    /// [FIX 2026-09-03 — lệnh Sếp] Vào game KHÔNG bật sẵn bảng tin chợ.
    /// Scene đang lưu Panel_Dim + Popup_Board active nên popup hiện ngay khi load.
    /// Đóng ở Start() (sau khi MỌI Awake đã chạy: Instance đã set, hàng đã sinh,
    /// MarketBoardUI đã wire nút) — không đụng file scene.
    ///
    /// CHỐT AN TOÀN chống tái diễn "LỖI 1" ghi ở đầu file (Start của popup chạy
    /// lần đầu tiên NGƯỜI CHƠI mở → đóng sập ngay trước mặt họ): dùng cờ tĩnh
    /// s_daDongBangTinLanDau thay cho đồng hồ thực (Time.timeSinceLevelLoad), vì
    /// máy chậm có thể load scene lâu hơn bất kỳ ngưỡng thời gian cố định nào.
    /// Cờ được reset về false ngay khi vào Play/scene mới (xem
    /// ResetDaDongBangTinLanDauKhiVaoScene bên dưới), nên LẦN Start() ĐẦU TIÊN
    /// sau khi load scene luôn là lần đóng "câm" — dù máy chậm bao nhiêu giây
    /// cũng đúng. Mọi lần Start() sau đó (do người chơi tự mở popup, nếu về sau
    /// scene được lưu ở trạng thái tắt) sẽ KHÔNG bị đóng, vì cờ đã được bật.
    /// Đóng "câm": SetActive(false) thẳng, không animation, không toast.
    /// </summary>
    private void Start()
    {
        if (closeBoardOnSceneStart && IsOpen && !s_daDongBangTinLanDau)
        {
            popupRoot.SetActive(false);
            ReleasePopupInputBlock();
            Debug.Log("[Market] Đã đóng bảng tin chợ đang bật sẵn trong scene (lần Start đầu tiên).");
        }

        s_daDongBangTinLanDau = true;
    }

    /// <summary>
    /// Cờ đáng tin thay cho đồng hồ thực (Time.timeSinceLevelLoad): true nghĩa là
    /// lần Start() đầu tiên của MarketManager kể từ khi vào Play/scene mới đã
    /// trôi qua. Là static nên sống dai qua Domain Reload tắt — bắt buộc phải
    /// reset thủ công mỗi khi vào Play/scene mới, xem
    /// ResetDaDongBangTinLanDauKhiVaoScene bên dưới.
    /// </summary>
    private static bool s_daDongBangTinLanDau = false;

    /// <summary>
    /// Reset cờ s_daDongBangTinLanDau về false mỗi khi vào Play/scene mới, để
    /// giá trị static không "dính" từ lần chạy trước qua Domain Reload tắt.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDaDongBangTinLanDauKhiVaoScene()
    {
        s_daDongBangTinLanDau = false;
    }

    private void OnDestroy()
    {
        if (refreshTimer != null)
            refreshTimer.OnCycleElapsed -= HandleCycleElapsed;

        if (provider != null)
            provider.OnListingsChanged -= HandleProviderChanged;

        MarketPlayerListingBridge.OnPlayerListingsChanged -= HandleProviderChanged;

        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (IsOpen)
            AcquirePopupInputBlock();
    }

    private void OnDisable()
    {
        ReleasePopupInputBlock();
    }

    private void Update()
    {
        // Đồng hồ tự bù chu kỳ đã trôi khi game tắt, xem MarketRefreshTimer.Tick
        if (refreshTimer != null)
            refreshTimer.Tick();
    }

    private void HandleCycleElapsed()
    {
        RegenerateListings();
    }

    private void HandleProviderChanged()
    {
        OnMarketChanged?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ĐÓNG / MỞ POPUP
    // ══════════════════════════════════════════════════════════════════════

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    public void OpenMarketPopup()
    {
        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (popupRoot != null)
        {
            // Bật luôn cả chuỗi cha đang tắt. Canvas_MarketPopup có thể bị tool
            // DisableStartupPopups tắt ở tầng trên, bật mỗi popupRoot là vẫn không thấy gì.
            Transform parent = popupRoot.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);
                parent = parent.parent;
            }

            // Đảm bảo Canvas của Chợ luôn hiển thị trên HUD (order 125 > 100)
            Canvas cv = popupRoot.GetComponentInParent<Canvas>();
            if (cv != null && cv.sortingOrder < 125)
            {
                cv.overrideSorting = true;
                cv.sortingOrder = 125;
            }

            popupRoot.SetActive(true);
        }

        AcquirePopupInputBlock();
        PlayOpenAnimation();

        OnMarketChanged?.Invoke();
    }

    public void CloseMarketPopup()
    {
        ReleasePopupInputBlock();

        if (popupRoot != null)
            popupRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  LÀM MỚI
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Sinh lại hàng NPC theo hạt của chu kỳ hiện tại.</summary>
    public void RegenerateListings()
    {
        if (provider == null || refreshTimer == null)
            return;

        // KHÔNG gọi OnMarketChanged ở đây: provider đã bắn OnListingsChanged
        // và HandleProviderChanged chuyển tiếp sang OnMarketChanged. Gọi thêm lần nữa
        // là UI vẽ lại hai lần và hiệu ứng hiện so le bị khởi động lại giữa chừng.
        provider.RegenerateNpcListings(itemCountPerRefresh, GetPlayerLevel(), refreshTimer.CurrentCycleSeed);
    }

    /// <summary>
    /// Làm mới MIỄN PHÍ — chỉ khi đồng hồ đã chạy hết.
    /// Bản cũ cho bấm lúc nào cũng được nên nút trả tiền thành vô nghĩa.
    /// </summary>
    public bool RefreshNowFree()
    {
        if (refreshTimer == null || !refreshTimer.CanRefreshFree())
            return false;

        refreshTimer.ForceNewCycle();
        return true;
    }

    /// <summary>Làm mới NGAY bằng VÀNG. Trả false nếu không đủ vàng hoặc không có ví.</summary>
    public bool RefreshNowWithGold()
    {
        if (refreshTimer == null)
            return false;

        // Đồng hồ đã hết thì làm mới miễn phí, đừng lấy tiền của người chơi
        if (refreshTimer.CanRefreshFree())
        {
            refreshTimer.ForceNewCycle();
            return true;
        }

        int cost = refreshTimer.GetGoldRefreshCost();
        if (!CanSpendGold(cost))
            return false;

        if (!SpendGold(cost))
            return false;

        refreshTimer.RegisterPaidRefresh();
        refreshTimer.ForceNewCycle();
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  MUA HÀNG
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mua một mặt hàng trên bảng tin. Trả mã lý do để UI hiện thông báo đúng.
    ///
    /// Thứ tự các bước KHÔNG được đảo: kiểm tra đủ → trừ tiền → cộng kho → đánh dấu bán.
    /// Cộng kho trước khi trừ tiền là mở cửa cho lỗi mua chùa nếu trừ tiền thất bại.
    /// </summary>
    public MarketBuyResult TryBuyListing(string listingId)
    {
        if (provider == null || string.IsNullOrEmpty(listingId))
            return MarketBuyResult.ListingNotFound;

        MarketListing listing = provider.GetListing(listingId);
        if (listing == null)
            return MarketBuyResult.ListingNotFound;

        if (listing.Status != MarketListingStatus.Active)
            return MarketBuyResult.ListingNotActive;

        if (listing.IsPlayerListing)
            return MarketBuyResult.OwnListing;

        int totalPrice = listing.TotalPrice;

        if (!CanSpendGold(totalPrice))
            return MarketBuyResult.NotEnoughGold;

        // Hạt giống đi WarehouseManager, mọi thứ khác đi FarmInventoryManager.
        // Thiếu kho tương ứng thì DỪNG TRƯỚC khi trừ tiền — không thì mất vàng mà không có hàng.
        bool isSeed = MarketPriceTable.IsSeed(listing.ItemId);
        if (isSeed && WarehouseManager.Instance == null)
            return MarketBuyResult.InventoryMissing;
        if (!isSeed && FarmInventoryManager.Instance == null)
            return MarketBuyResult.InventoryMissing;

        // TESTER-F8 — LỖI MẤT TIỀN NGƯỜI CHƠI.
        // F8 làm `FarmInventoryManager.AddItem` TỪ CHỐI loại mới khi kho hết slot và trả
        // về false, nhưng `GiveItemToCorrectStorage` bỏ qua giá trị trả về. Vàng đã bị
        // `SpendGold` trừ ở dòng dưới ⇒ người chơi TRẢ TIỀN MÀ KHÔNG NHẬN ĐƯỢC HÀNG.
        // Đúng theo chính chú thích ngay trên hàm này ("kiểm tra đủ → trừ tiền → cộng
        // kho"), phép kiểm sức chứa phải nằm ở bước "kiểm tra đủ", tức TRƯỚC SpendGold.
        // Kho hạt (`WarehouseManager`) không có hệ slot nên chỉ kiểm nhánh không phải hạt.
        if (!isSeed && !FarmInventoryManager.Instance.CanAddItem(listing.ItemId))
            return MarketBuyResult.InventoryFull;

        if (!SpendGold(totalPrice))
            return MarketBuyResult.NotEnoughGold;

        GiveItemToCorrectStorage(listing.ItemId, listing.Quantity, isSeed);

        if (isSeed)
            MissionProgressTracker.ReportEvent(MissionEventType.BuySeed, listing.ItemId, listing.Quantity);
        else
            MissionProgressTracker.ReportEvent(MissionEventType.BuyShopItem, listing.ItemId, listing.Quantity);

        provider.MarkListingSold(listingId);
        return MarketBuyResult.Success;
    }

    /// <summary>
    /// LỖI 3 — điểm phân luồng kho.
    ///
    /// Quy ước dự án: WarehouseManager CHỈ chứa hạt giống (khoá theo seedItemId),
    /// FarmInventoryManager chứa nông sản / sản phẩm chuồng-máy / món ăn / gia vị.
    /// Bỏ nhầm kho là hạt nằm trong kho nông sản → mở túi hạt ra trồng thì báo hết hạt.
    ///
    /// ⚠️ Phân loại bằng danh mục trong MarketPriceTable, TUYỆT ĐỐI không bằng
    /// itemId.StartsWith("seed"): `ca_rot` và `khoai_tay` là hạt giống nhưng
    /// không mang tiền tố đó (xem Hat_giong/Ca_Rot.asset, Hat_giong/Khoai_Tay.asset).
    /// </summary>
    private void GiveItemToCorrectStorage(string itemId, int quantity, bool isSeed)
    {
        if (isSeed)
        {
            MarketItemVisual visual = ResolveVisual(itemId);
            WarehouseManager.Instance.AddItem(itemId, visual.DisplayName, visual.Icon, quantity);
            return;
        }

        FarmInventoryManager.Instance.AddItem(itemId, quantity);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VÍ TIỀN
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// LỖI 2 — bản cũ `return true` khi không có FarmEconomyManager, tức là thiếu
    /// manager thì mua gì cũng miễn phí. Giờ không có ví = không tiêu được.
    /// </summary>
    public bool CanSpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (FarmEconomyManager.Instance == null)
            return false;

        return FarmEconomyManager.Instance.Gold >= amount;
    }

    private bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (FarmEconomyManager.Instance == null)
            return false;

        return FarmEconomyManager.Instance.SpendGold(amount);
    }

    public int GetPlayerGold()
    {
        return FarmEconomyManager.Instance != null ? FarmEconomyManager.Instance.Gold : 0;
    }

    public static int GetPlayerLevel()
    {
        return FarmLevelManager.Instance != null ? FarmLevelManager.Instance.CurrentLevel : 1;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ICON / TÊN HIỂN THỊ
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tra icon + tên. Không tìm thấy thì lấy tên từ bảng giá — thà hiện
    /// "Phở Bò Tái" không icon còn hơn hiện "pho_bo_tai".
    /// </summary>
    public MarketItemVisual ResolveVisual(string itemID)
    {
        string key = NormalizeKey(itemID);
        if (!string.IsNullOrEmpty(key) && visualLookup.TryGetValue(key, out MarketItemVisual visual))
        {
            if (string.IsNullOrEmpty(visual.DisplayName))
                visual.DisplayName = MarketPriceTable.GetDisplayName(itemID);
            return visual;
        }

        return new MarketItemVisual
        {
            DisplayName = MarketPriceTable.GetDisplayName(itemID),
            Icon        = null
        };
    }

    private void BuildVisualLookup()
    {
        visualLookup.Clear();

        for (int i = 0; i < cropDatabase.Count; i++)
        {
            CropData crop = cropDatabase[i];
            if (crop == null)
                continue;

            Sprite cropIcon = crop.icon != null ? crop.icon : crop.harvestIcon;

            // Hạt và nông sản dùng chung icon cây trồng, nhưng TÊN phải khác nhau:
            // "Hạt Lúa" và "Lúa" là hai vật phẩm nằm ở hai kho khác nhau, trùng tên
            // thì người chơi không phân biệt được mình vừa mua cái gì.
            AddVisual(crop.seedItemId, new MarketItemVisual
            {
                DisplayName = MarketPriceTable.GetDisplayName(crop.seedItemId),
                Icon        = cropIcon
            });

            MarketItemVisual harvestVisual = new MarketItemVisual
            {
                DisplayName = string.IsNullOrEmpty(crop.displayName) ? crop.cropId : crop.displayName,
                Icon        = cropIcon
            };

            AddVisual(crop.harvestItemId, harvestVisual);
            AddVisual(crop.cropId, harvestVisual);
            AddVisual(crop.itemID, new MarketItemVisual
            {
                DisplayName = MarketPriceTable.GetDisplayName(crop.itemID),
                Icon        = cropIcon
            });
        }

        // Chạy SAU crop để InventoryItemData thắng khi trùng khoá — nông sản trong kho
        // có icon riêng đẹp hơn sprite cây trên ruộng
        for (int i = 0; i < itemDatabase.Count; i++)
        {
            InventoryItemData item = itemDatabase[i];
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            OverwriteVisual(item.itemId, new MarketItemVisual
            {
                DisplayName = string.IsNullOrEmpty(item.displayName) ? item.itemId : item.displayName,
                Icon        = item.icon
            });
        }
    }

    private void AddVisual(string itemID, MarketItemVisual visual)
    {
        string key = NormalizeKey(itemID);
        if (string.IsNullOrEmpty(key) || visualLookup.ContainsKey(key))
            return;

        visualLookup.Add(key, visual);
    }

    private void OverwriteVisual(string itemID, MarketItemVisual visual)
    {
        string key = NormalizeKey(itemID);
        if (string.IsNullOrEmpty(key))
            return;

        // Icon rỗng thì giữ icon cũ — ba asset máy chế biến chưa gán icon,
        // ghi đè bằng null sẽ xoá mất icon crop đang dùng tạm
        if (visual.Icon == null && visualLookup.TryGetValue(key, out MarketItemVisual old) && old.Icon != null)
            visual.Icon = old.Icon;

        visualLookup[key] = visual;
    }

    private static string NormalizeKey(string key)
    {
        return key == null ? string.Empty : key.Trim().ToLowerInvariant();
    }

#if UNITY_EDITOR
    /// <summary>Editor tool dùng để nạp toàn bộ CropData / InventoryItemData trong dự án.</summary>
    public void EditorSetVisualSources(List<CropData> crops, List<InventoryItemData> items)
    {
        cropDatabase = crops ?? new List<CropData>();
        itemDatabase = items ?? new List<InventoryItemData>();
        BuildVisualLookup();
    }
#endif

    // ══════════════════════════════════════════════════════════════════════
    //  KHOÁ INPUT + HIỆU ỨNG MỞ
    // ══════════════════════════════════════════════════════════════════════

    private void EnsurePopupRaycastBlock()
    {
        if (popupRoot == null)
            return;

        Canvas parentCanvas = popupRoot.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, true);

        FarmInputLock.SetPopupRaycastBlock(popupRoot, true);
    }

    private void AcquirePopupInputBlock()
    {
        if (popupRoot == null)
            popupRoot = transform.parent != null ? transform.parent.gameObject : gameObject;

        FarmInputLock.IsMarketPopupOpen = true;
        EnsurePopupRaycastBlock();

        if (popupInputLockHeld) return;
        FarmInputLock.RegisterPopupOpen();
        popupInputLockHeld = true;
    }

    private void ReleasePopupInputBlock()
    {
        FarmInputLock.IsMarketPopupOpen = false;
        FarmInputLock.SetPopupRaycastBlock(popupRoot, false);

        Canvas parentCanvas = popupRoot != null
            ? popupRoot.GetComponentInParent<Canvas>()
            : GetComponentInParent<Canvas>();

        if (parentCanvas != null)
            FarmInputLock.SetPopupRaycastBlock(parentCanvas.gameObject, false);

        if (!popupInputLockHeld) return;
        FarmInputLock.RegisterPopupClose();
        popupInputLockHeld = false;
    }

    private void PlayOpenAnimation()
    {
        if (!gameObject.activeInHierarchy)
            return;   // StartCoroutine trên object đang tắt sẽ ném lỗi

        if (openAnimationCoroutine != null)
            StopCoroutine(openAnimationCoroutine);

        openAnimationCoroutine = StartCoroutine(OpenScaleRoutine(transform));
    }

    private IEnumerator OpenScaleRoutine(Transform target)
    {
        if (target == null)
            yield break;

        Vector3 startScale = Vector3.one * 0.92f;
        Vector3 endScale   = Vector3.one;
        float   duration   = 0.12f;
        float   elapsed    = 0f;

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
        openAnimationCoroutine = null;
    }
}
