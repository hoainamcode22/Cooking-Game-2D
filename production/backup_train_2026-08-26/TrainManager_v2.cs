using System.Collections;
using UnityEngine;

/// <summary>
/// State machine trung tÃ¢m â€” quáº£n lÃ½ logic 2 tÃ u:
///
///   TrainVisualRoot  (Reward Train  / tÃ u CÅ¨)
///     â†’ xuáº¥t hiá»‡n tá»« háº§m, cháº¡y vá» ga, tráº£ reward cho user
///
///   TrainVisualRoot2 (Shipping Train / tÃ u Má»šI)
///     â†’ Ä‘á»©ng ga chá» user náº¡p hÃ ng, rá»“i cháº¡y vÃ o háº§m vÃ  áº©n
///
/// 6-state machine:
///   WaitingForLoad       â†’ tÃ u Má»šI snap táº¡i ga, user náº¡p hÃ ng
///   ShipDeparting        â†’ tÃ u Má»šI: ga â†’ háº§m â†’ (teleport vá» pointHiddenShip) â†’ áº©n
///   Processing           â†’ 2 tÃ u áº©n, timer cháº¡y
///   RewardArriving       â†’ tÃ u CÅ¨ snap táº¡i háº§m, cháº¡y vá» ga
///   RewardReadyToCollect â†’ tÃ u CÅ¨ táº¡i ga, user thu reward
///   RewardDeparting      â†’ tÃ u CÅ¨: ga â†’ pointHiddenReward â†’ (teleport vá» háº§m) â†’ áº©n
///                          â†’ tÃ u Má»šI snap tháº³ng vá» ga â†’ WaitingForLoad
/// </summary>
public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }

    // â”€â”€â”€ Inspector â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Header("Data (ScriptableObjects)")]
    [SerializeField] private TrainCargoData  cargoData;
    [SerializeField] private TrainRewardData rewardData;

    [Header("Shipping Train â€” tÃ u Má»šI (TrainVisualRoot2) nháº­n hÃ ng Ä‘i")]
    [Tooltip("TrainPathFollower gáº¯n trÃªn ShippingTrain GO (quáº£n lÃ½ TrainVisualRoot2)")]
    [SerializeField] private TrainPathFollower shippingPathFollower;

    [Header("Reward Train â€” tÃ u CÅ¨ (TrainVisualRoot) mang reward vá»")]
    [Tooltip("TrainPathFollower gáº¯n trÃªn RewardTrain GO (quáº£n lÃ½ TrainVisualRoot)")]
    [SerializeField] private TrainPathFollower rewardPathFollower;

    [Header("Waypoints â€” Shipping Train (tÃ u Má»šI)")]
    [Tooltip("Off-screen trÃ¡i â€” nÆ¡i tÃ u Má»šI spawn vÃ o Ä‘áº§u chuyáº¿n (trá»« chuyáº¿n 1)")]
    [SerializeField] private Transform pointHiddenShip;
    [Tooltip("TrÃªn ga â€” nÆ¡i tÃ u Má»šI Ä‘á»©ng chá» user náº¡p hÃ ng")]
    [SerializeField] private Transform pointStationShip;
    [Tooltip("Cá»­a háº§m â€” nÆ¡i tÃ u Má»šI cháº¡y tá»›i rá»“i áº©n")]
    [SerializeField] private Transform pointTunnelShip;

    [Header("Waypoints â€” Reward Train (tÃ u CÅ¨)")]
    [Tooltip("Cá»­a háº§m â€” nÆ¡i tÃ u CÅ¨ xuáº¥t hiá»‡n sau khi timer xong")]
    [SerializeField] private Transform pointTunnelReward;
    [Tooltip("TrÃªn ga â€” nÆ¡i tÃ u CÅ¨ dá»«ng Ä‘á»ƒ user thu reward")]
    [SerializeField] private Transform pointStationReward;
    [Tooltip("Off-screen pháº£i â€” nÆ¡i tÃ u CÅ¨ cháº¡y ra rá»“i áº©n sau khi user thu xong")]
    [SerializeField] private Transform pointHiddenReward;

    [Header("Wagon Slots â€” Shipping Train (4 toa tÃ u Má»šI)")]
    [Tooltip("4 TrainWagonSlot gáº¯n trÃªn wagon cá»§a TrainVisualRoot2, theo thá»© tá»± 0..3")]
    [SerializeField] private TrainWagonSlot[] shippingWagonSlots;

    [Header("Wagon Slots â€” Reward Train (4 toa tÃ u CÅ¨)")]
    [Tooltip("4 TrainWagonSlot gáº¯n trÃªn wagon cá»§a TrainVisualRoot, theo thá»© tá»± 0..3")]
    [SerializeField] private TrainWagonSlot[] rewardWagonSlots;

    [Header("Popups")]
    [Tooltip("Popup náº¡p hÃ ng â€” Popup_item_Train")]
    [SerializeField] private TrainLoadPopupUI    loadPopup;
    [Tooltip("Popup tráº¡ng thÃ¡i / timer â€” Popup_train")]
    [SerializeField] private TrainProcessPopupUI processPopup;

    [Header("FX â€” Reward Collection")]
    [Tooltip("Prefab HarvestFlyItemFX â€” bay tá»« toa tÃ u CÅ¨ vá» kho")]
    [SerializeField] private GameObject itemFlyFXPrefab;
    [Tooltip("Prefab ExpFlyToAvatarFX â€” bay tá»« toa tÃ u CÅ¨ vá» avatar")]
    [SerializeField] private GameObject expFlyFXPrefab;
    [Tooltip("Vá»‹ trÃ­ icon kho trÃªn HUD (Ä‘Ã­ch cá»§a item FX)")]
    [SerializeField] private Transform  warehouseTargetTransform;
    [Tooltip("Vá»‹ trÃ­ icon avatar/EXP trÃªn HUD (Ä‘Ã­ch cá»§a exp FX)")]
    [SerializeField] private Transform  expTargetTransform;

    [Header("Config")]
    [Tooltip("EXP thÆ°á»Ÿng má»—i láº§n thu 1 slot reward")]
    [SerializeField] private int   expPerReward        = 10;

    [Tooltip("Thời gian 1 chuyến vận chuyển (giây). Đã duyệt 2026-08-26: 10-15 phút (600-900).")]
    [SerializeField] private float tripDurationSeconds = 600f;

    [Tooltip("Vàng thưởng chốt chuyến khi thu đủ mọi toa (đã duyệt: 50-100).")]
    [SerializeField] private int   goldBonusPerTrip    = 80;

    // â”€â”€â”€ Runtime â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public TrainState          State    { get; private set; }
    public TrainWagonSlotData[] SlotData { get; private set; }

    private int               _tripIndex      = 0;
    private TrainRewardItem[] _pendingRewards;

    /// <summary>Sự kiện đổi state — UI package đăng ký để đồng bộ view.</summary>
    public event System.Action<TrainState> OnStateChanged;

    /// <summary>Tổng thời gian 1 chuyến vận chuyển (giây).</summary>
    public float TripTotalDuration => tripDurationSeconds;

    /// <summary>Snapshot hàng đã gửi của chuyến hiện tại (cho popup 'Đang vận chuyển').</summary>
    public TrainWagonSlotData[] LastSentCargo => _lastSentCargo;

    /// <summary>Giá kim cương tăng tốc hiện tại — đồng nhất công thức hệ xây dựng.</summary>
    public int SpeedUpCost => State == TrainState.Processing
        ? Mathf.Max(1, ConstructionManager.RushCostFor(TripRemainingTime))
        : 0;

    /// <summary>
    /// Thời gian còn lại của chuyến (giây) — timer THẬT theo đồng hồ hệ thống,
    /// chạy nền kể cả khi đóng popup hoặc tắt game (persist qua PlayerPrefs).
    /// </summary>
    public float TripRemainingTime => State == TrainState.Processing
        ? Mathf.Max(0f, (float)(_tripEndUnix - NowUnix()))
        : 0f;

    private double _tripEndUnix;
    private TrainWagonSlotData[] _lastSentCargo;

    private static double NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Timer chuyến tàu — tính theo unix time nên tự "chạy" cả khi offline/đóng popup
        if (State == TrainState.Processing && NowUnix() >= _tripEndUnix)
            FinishProcessing();
    }

    void Start()
    {
        // Auto-find popup ká»ƒ cáº£ khi inactive
        if (loadPopup == null)
            loadPopup = FindFirstObjectByType<TrainLoadPopupUI>(FindObjectsInactive.Include);
        if (processPopup == null)
            processPopup = FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);

        if (loadPopup == null)
            Debug.LogError("[Train] KhÃ´ng tÃ¬m tháº¥y TrainLoadPopupUI! KÃ©o Popup_Item_Train vÃ o Inspector.");

        bool hasError = false;
        if (shippingPathFollower == null) { Debug.LogError("[Train] shippingPathFollower chÆ°a gÃ¡n!"); hasError = true; }
        if (rewardPathFollower   == null) { Debug.LogError("[Train] rewardPathFollower chÆ°a gÃ¡n!");   hasError = true; }
        if (shippingPathFollower != null && shippingPathFollower == rewardPathFollower)
        { Debug.LogError("[Train] BUG: shippingPathFollower vÃ  rewardPathFollower trá» cÃ¹ng 1 object!"); hasError = true; }
        if (pointHiddenShip    == null) { Debug.LogError("[Train] pointHiddenShip chÆ°a gÃ¡n!");    hasError = true; }
        if (pointStationShip   == null) { Debug.LogError("[Train] pointStationShip chÆ°a gÃ¡n!");   hasError = true; }
        if (pointTunnelShip    == null) { Debug.LogError("[Train] pointTunnelShip chÆ°a gÃ¡n!");    hasError = true; }
        if (pointTunnelReward  == null) { Debug.LogError("[Train] pointTunnelReward chÆ°a gÃ¡n!");  hasError = true; }
        if (pointStationReward == null) { Debug.LogError("[Train] pointStationReward chÆ°a gÃ¡n!"); hasError = true; }
        if (pointHiddenReward  == null) { Debug.LogError("[Train] pointHiddenReward chÆ°a gÃ¡n!");  hasError = true; }

        if (hasError)
        {
            Debug.LogError("[Train] Khá»Ÿi táº¡o bá»‹ huá»· vÃ¬ thiáº¿u references. Kiá»ƒm tra Inspector rá»“i nháº¥n 'Reset Train' (chuá»™t pháº£i TrainManager).");
            return;
        }

        StartCoroutine(InitAfterFrame());
    }

    [ContextMenu("Reset Train / Hiá»‡n láº¡i tÃ u")]
    public void ResetTrain()
    {
        StopAllCoroutines();
        PlayerPrefs.DeleteKey(SaveKey);
        shippingPathFollower?.ShowTrain();
        rewardPathFollower?.HideTrain();
        HideAllRewardSlots();
        _tripIndex = 0;
        GenerateNewTrip();
        ShowShippingAtHiddenThenMoveToStation(() =>
        {
            ChangeState(TrainState.WaitingForLoad);
            RefreshAllShippingSlots();
        });
    }

    private IEnumerator InitAfterFrame()
    {
        yield return null;

        // Đảm bảo tàu MỚI hiện trước khi HideTrain() làm bất cứ điều gì
        shippingPathFollower.ShowTrain();

        // Tàu CŨ ẩn hoàn toàn lúc đầu
        rewardPathFollower.HideTrain();
        HideAllRewardSlots();

        // Khôi phục chuyến dở dang (timer chạy nền cả khi tắt game)
        if (TryRestoreTrainState())
            yield break;

        GenerateNewTrip();

        ShowShippingAtHiddenThenMoveToStation(() =>
        {
            ChangeState(TrainState.WaitingForLoad);
            RefreshAllShippingSlots();
        });
    }

    // â”€â”€â”€ Public API (gá»i tá»« TrainWagonSlot & TrainLoadPopupUI) â”€â”€â”€

    /// Äiá»ƒm vÃ o duy nháº¥t tá»« TrainWagonSlot.OnMouseDown().
    /// Routing theo state â€” TrainManager tá»± quyáº¿t Ä‘á»‹nh lÃ m gÃ¬ vá»›i click Ä‘Ã³.
    public void OnWagonSlotClicked(TrainWagonSlot slot)
    {
        int idx = slot.slotIndex;

        switch (State)
        {
            case TrainState.WaitingForLoad:
                if (IsValidSlot(idx) &&
                    SlotData[idx].mode == TrainWagonSlotMode.CargoRequest &&
                    !SlotData[idx].IsCargoComplete)
                    OnCargoSlotClicked(idx);
                break;

            case TrainState.RewardReadyToCollect:
                if (IsValidSlot(idx) &&
                    SlotData[idx].mode == TrainWagonSlotMode.Reward &&
                    !SlotData[idx].isCollected)
                    CollectReward(idx);
                break;

            default:
                break;
        }
    }

    /// NgÆ°á»i chÆ¡i click toa cá»§a tÃ u Má»šI â†’ má»Ÿ popup náº¡p hÃ ng.
    public void OnCargoSlotClicked(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex))            return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete)                         return;

        // Ưu tiên popup nạp hàng mới (Export_Train_UI_Package) — cùng đọc SlotData, 1 nguồn sự thật
        var pkgLoad = ExportTrainUIPackage.TrainLoadPopupUI.Instance;
        if (pkgLoad == null)
            pkgLoad = FindFirstObjectByType<ExportTrainUIPackage.TrainLoadPopupUI>(FindObjectsInactive.Include);
        if (pkgLoad != null)
        {
            pkgLoad.OpenForWagon(slotIndex);
            return;
        }

        if (loadPopup == null)
        {
            Debug.LogWarning("[Train] loadPopup == null — kéo Popup_item_Train vào Inspector.");
            return;
        }

        loadPopup.OpenForCargoSlot(slotIndex, slot);
    }

    /// NÃºt "ThÃªm" trong popup náº¡p hÃ ng â€” trá»« 1 item kho, tÄƒng currentAmount.
    public void TryAddOneItemToSlot(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex))            return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete)                         return;

        if (!TrainInventoryAdapter.HasItem(slot.itemId, 1))
        {
            FarmUIManager.Instance?.ShowHint($"Bạn chưa đủ {slot.displayName} — trồng/sản xuất thêm nhé!");
            return; // Popup vẫn mở để user tự đóng
        }

        if (!TrainInventoryAdapter.RemoveItem(slot.itemId, 1)) return;

        slot.currentAmount++;

        MissionProgressTracker.ReportEvent(MissionEventType.LoadTrainCargo, slot.itemId, 1);

        loadPopup?.RefreshPopup();
        RefreshShippingSlotUI(slotIndex);
        SaveTrainState();

        if (slot.IsCargoComplete)
        {
            loadPopup?.ClosePopup();
            CheckAllLoaded();
        }
    }

    /// <summary>Nút "NẠP TẤT CẢ" — nạp tối đa có thể từ kho vào toa. Trả về số đã nạp.</summary>
    public int TryLoadAllToSlot(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return 0;
        if (!IsValidSlot(slotIndex))            return 0;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return 0;
        if (slot.IsCargoComplete)                         return 0;

        int needed  = slot.requiredAmount - slot.currentAmount;
        int inStock = FarmInventoryManager.Instance != null
            ? FarmInventoryManager.Instance.GetAmount(slot.itemId) : 0;
        int toAdd   = Mathf.Min(needed, inStock);

        if (toAdd <= 0)
        {
            FarmUIManager.Instance?.ShowHint($"Bạn chưa đủ {slot.displayName} — trồng/sản xuất thêm nhé!");
            return 0;
        }

        if (!TrainInventoryAdapter.RemoveItem(slot.itemId, toAdd)) return 0;

        slot.currentAmount += toAdd;
        MissionProgressTracker.ReportEvent(MissionEventType.LoadTrainCargo, slot.itemId, toAdd);

        loadPopup?.RefreshPopup();
        RefreshShippingSlotUI(slotIndex);
        SaveTrainState();

        if (slot.IsCargoComplete)
        {
            loadPopup?.ClosePopup();
            CheckAllLoaded();
        }
        return toAdd;
    }

    /// NgÆ°á»i chÆ¡i click toa cá»§a tÃ u CÅ¨ Ä‘á»ƒ thu reward.
    public void CollectReward(int slotIndex)
    {
        if (State != TrainState.RewardReadyToCollect) return;
        if (!IsValidSlot(slotIndex))                  return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.Reward) return;
        if (slot.isCollected)                       return;

        // ÄÃ¡nh dáº¥u trÆ°á»›c â€” ngÄƒn double-click trong lÃºc FX Ä‘ang cháº¡y
        // F8 — kho có sức chứa THẬT. Kiểm TRƯỚC khi đánh dấu đã thu: nếu kho từ chối thì
        // thưởng bốc hơi mà toa đã mang dấu "đã thu" vĩnh viễn — mất cả chuyến tàu.
        if (!TrainInventoryAdapter.CanAddItem(slot.itemId))
        {
            FarmUIManager.Instance?.ShowHint("Kho đầy — bán bớt hoặc nâng cấp kho rồi thu thưởng tàu.");
            return;
        }

        slot.isCollected = true;

        TrainInventoryAdapter.AddItem(slot.itemId, slot.displayName, slot.icon, slot.rewardAmount);

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(expPerReward);


        SpawnItemFlyFX(slotIndex, slot);
        SpawnExpFlyFX(slotIndex);
        RefreshRewardSlotUI(slotIndex);
        SaveTrainState();
        CheckAllCollected();
    }

    // â”€â”€â”€ Flow: WaitingForLoad â†’ ShipDeparting â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void CheckAllLoaded()
    {
        if (State != TrainState.WaitingForLoad) return;
        if (SlotData == null) return;
        foreach (var s in SlotData)
            if (s.mode == TrainWagonSlotMode.CargoRequest && !s.IsCargoComplete) return;

        // Tắt collider (giữ visual cargo trên toa suốt hành trình)
        DisableAllShippingSlotInteractions();
        ChangeState(TrainState.ShipDeparting);
        SaveTrainState(); // M1: đóng cửa sổ hở — thoát game lúc tàu đang vào hầm vẫn khôi phục đúng

        // Chặng 2: StationShip -> TunnelShip
        SendShippingFromStationToTunnel();
    }

    // â”€â”€â”€ Flow: ShipDeparting â†’ Processing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnShippingReachedTunnel()
    {

        // 1. áº¨n tÃ u shipping
        shippingPathFollower.HideTrain();
        HideAllShippingSlots();

        // 2. Teleport shipping vá» vá»‹ trÃ­ áº©n (sáºµn sÃ ng chuyáº¿n sau)
        Vector3 hiddenPos   = pointHiddenShip.position;
        Vector3 stationPos  = pointStationShip.position;
        Vector3 backwardDir = (hiddenPos - stationPos).normalized;
        shippingPathFollower.SnapToPosition(hiddenPos, backwardDir);

        // 3. Chốt "hàng đã gửi" cho popup 'Đang vận chuyển'
        SnapshotSentCargo();

        // 4. Bắt đầu đếm ngược chuyến vận chuyển (timer thật, chạy nền + offline)
        StartProcessing(tripDurationSeconds);
    }
    // ─── Flow: Processing (timer thật — khôi phục có duyệt 2026-08-26) ───────────

    /// <summary>Bắt đầu state Processing với timer thật — persist để chạy nền và offline.</summary>
    private void StartProcessing(float durationSeconds)
    {
        _tripEndUnix = NowUnix() + Mathf.Max(1f, durationSeconds);
        ChangeState(TrainState.Processing);
        SaveTrainState();
    }

    /// <summary>Timer hết (hoặc tăng tốc) → áp thưởng, tàu CŨ ra khỏi hầm.</summary>
    private void FinishProcessing()
    {
        if (State != TrainState.Processing) return;

        ApplyRewardsToSlots();

        ChangeState(TrainState.RewardArriving);
        RefreshAllRewardSlots();
        DisableAllRewardSlotInteractions();
        ShowRewardAtTunnelThenMoveToStation(OnRewardArrivedAtStation);
        SaveTrainState();
    }

    /// <summary>
    /// Tăng tốc chuyến đang vận chuyển bằng kim cương.
    /// Giá = ConstructionManager.RushCostFor(thời gian còn lại) — đồng nhất hệ xây dựng.
    /// Trả false (kèm hint) nếu không đủ kim cương.
    /// </summary>
    public bool TrySpeedUp()
    {
        if (State != TrainState.Processing) return false;

        int cost = SpeedUpCost;
        if (FarmEconomyManager.Instance == null || !FarmEconomyManager.Instance.SpendGems(cost))
        {
            FarmUIManager.Instance?.ShowHint("Không đủ kim cương để tăng tốc tàu.");
            return false;
        }

        _tripEndUnix = NowUnix();
        FinishProcessing();
        return true;
    }

    /// <summary>Chụp lại hàng đã nạp trước khi SlotData bị ApplyRewardsToSlots() ghi đè.</summary>
    private void SnapshotSentCargo()
    {
        if (SlotData == null) { _lastSentCargo = null; return; }
        _lastSentCargo = new TrainWagonSlotData[SlotData.Length];
        for (int i = 0; i < SlotData.Length; i++)
        {
            var s = SlotData[i];
            if (s == null) continue;
            _lastSentCargo[i] = new TrainWagonSlotData
            {
                itemId         = s.itemId,
                displayName    = s.displayName,
                icon           = s.icon,
                mode           = s.mode,
                currentAmount  = s.currentAmount,
                requiredAmount = s.requiredAmount
            };
        }
    }


    // â”€â”€â”€ Flow: RewardArriving â†’ RewardReadyToCollect â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnRewardArrivedAtStation()
    {
        ChangeState(TrainState.RewardReadyToCollect);

        // Refresh lại để bật collider cho user click
        RefreshAllRewardSlots();

        FarmUIManager.Instance?.ShowHint("Tàu đã về ga — chạm vào ga tàu để nhận hàng!");
        SaveTrainState();
    }

    // â”€â”€â”€ Flow: RewardReadyToCollect â†’ RewardDeparting â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void CheckAllCollected()
    {
        if (SlotData == null) return;
        foreach (var s in SlotData)
            if (s.mode == TrainWagonSlotMode.Reward && !s.isCollected) return;

        // Vàng thưởng chốt chuyến (đã duyệt 2026-08-26: vật liệu + ít vàng)
        if (goldBonusPerTrip > 0 && FarmEconomyManager.Instance != null)
        {
            FarmEconomyManager.Instance.AddGold(goldBonusPerTrip);
            FarmUIManager.Instance?.ShowHint($"+{goldBonusPerTrip} vàng thưởng chuyến tàu!");
        }

        HideAllRewardSlots();
        ChangeState(TrainState.RewardDeparting);

        // Chặng 4: StationReward -> HiddenReward
        SendRewardFromStationToHidden();
        SaveTrainState();
    }

    // â”€â”€â”€ Flow: RewardDeparting â†’ WaitingForLoad â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void OnRewardReachedHidden()
    {

        // 1. áº¨n tÃ u reward
        rewardPathFollower.HideTrain();
        HideAllRewardSlots();

        // 2. Teleport reward vá» háº§m (sáºµn sÃ ng chuyáº¿n sau)
        Vector3 tunnelPos   = pointTunnelReward.position;
        Vector3 stationPos  = pointStationReward.position;
        Vector3 backwardDir = (tunnelPos - stationPos).normalized;
        rewardPathFollower.SnapToPosition(tunnelPos, backwardDir);

        // 3. Táº¡o chuyáº¿n má»›i
        _tripIndex++;
        GenerateNewTrip();

        // 4. TÃ u shipping xuáº¥t hiá»‡n tá»« HiddenShip cháº¡y vá» ga NGAY
        ShowShippingAtHiddenThenMoveToStation(() =>
        {
            ChangeState(TrainState.WaitingForLoad);
            RefreshAllShippingSlots();
            SaveTrainState();
        });
    }

    // â”€â”€â”€ Point-role helpers (4 cháº·ng gameplay) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// Cháº·ng 1 â€” Shipping spawn: HiddenShip â†’ StationShip
    /// TÃ u Má»šI xuáº¥t hiá»‡n kÃ­n Ä‘Ã¡o táº¡i HiddenShip rá»“i cháº¡y ra ga chá» náº¡p hÃ ng.
    private void ShowShippingAtHiddenThenMoveToStation(System.Action onArrived = null)
    {
        if (shippingPathFollower == null) return;

        Vector3 hiddenPos  = pointHiddenShip  != null ? pointHiddenShip.position  : transform.position;
        Vector3 stationPos = pointStationShip != null ? pointStationShip.position : transform.position;

        // backwardDir = ngÆ°á»£c chiá»u cháº¡y (HiddenShip â†’ StationShip)
        // wagon tráº£i vá» phÃ­a sau HiddenShip, khuáº¥t táº§m nhÃ¬n
        Vector3 backwardDir = (hiddenPos - stationPos).normalized;

        // ShowTrain TRÆ¯á»šC Ä‘á»ƒ GO active, sau Ä‘Ã³ SnapToPosition + MoveTo má»›i hoáº¡t Ä‘á»™ng
        shippingPathFollower.ShowTrain();
        shippingPathFollower.SnapToPosition(hiddenPos, backwardDir);
        shippingPathFollower.MoveTo(stationPos, onArrived);
    }

    /// Cháº·ng 2 â€” Shipping depart: StationShip â†’ TunnelShip
    /// TÃ u Má»šI rá»i ga cháº¡y vÃ o háº§m rá»“i áº©n.
    private void SendShippingFromStationToTunnel()
    {
        shippingPathFollower.MoveTo(pointTunnelShip.position, OnShippingReachedTunnel);
    }

    /// Cháº·ng 3 â€” Reward arrive: TunnelReward â†’ StationReward
    /// TÃ u CÅ¨ xuáº¥t hiá»‡n táº¡i cá»­a háº§m rá»“i cháº¡y vá» ga Ä‘á»ƒ user nháº­n reward.
    private void ShowRewardAtTunnelThenMoveToStation(System.Action onArrived = null)
    {
        if (rewardPathFollower == null) return;

        Vector3 tunnelPos  = pointTunnelReward  != null ? pointTunnelReward.position  : transform.position;
        Vector3 stationPos = pointStationReward != null ? pointStationReward.position : transform.position;

        // backwardDir = ngÆ°á»£c chiá»u cháº¡y (TunnelReward â†’ StationReward)
        Vector3 backwardDir = (tunnelPos - stationPos).normalized;

        // ShowTrain TRÆ¯á»šC Ä‘á»ƒ GO active, sau Ä‘Ã³ SnapToPosition + MoveTo má»›i hoáº¡t Ä‘á»™ng
        rewardPathFollower.ShowTrain();
        rewardPathFollower.SnapToPosition(tunnelPos, backwardDir);
        rewardPathFollower.MoveTo(stationPos, onArrived);
    }

    /// Cháº·ng 4 â€” Reward leave: StationReward â†’ HiddenReward
    /// TÃ u CÅ¨ rá»i ga cháº¡y ra Ä‘iá»ƒm khuáº¥t rá»“i áº©n.
    private void SendRewardFromStationToHidden()
    {
        rewardPathFollower.MoveTo(pointHiddenReward.position, OnRewardReachedHidden);
    }

    // â”€â”€â”€ Trip generation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// Táº¡o SlotData vÃ  _pendingRewards tá»« preset.
    /// Chá»‰ setup data, khÃ´ng Ä‘á»•i state, khÃ´ng refresh UI.
    private void GenerateNewTrip()
    {
        var cargoPre  = GetCargoPreset(_tripIndex);
        var rewardPre = GetRewardPreset(_tripIndex);

        int slotCount = Mathf.Max(
            shippingWagonSlots != null ? shippingWagonSlots.Length : 0,
            cargoPre.slots     != null ? cargoPre.slots.Length     : 0);
        slotCount = Mathf.Max(slotCount, 1);

        SlotData        = new TrainWagonSlotData[slotCount];
        _pendingRewards = new TrainRewardItem[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            bool hasCargo  = cargoPre.slots  != null && i < cargoPre.slots.Length;
            bool hasReward = rewardPre.slots != null && i < rewardPre.slots.Length;

            var req = hasCargo  ? cargoPre.slots[i]  : null;
            _pendingRewards[i]  = hasReward ? rewardPre.slots[i] : null;

            SlotData[i] = new TrainWagonSlotData
            {
                itemId         = hasCargo ? req.itemId         : "",
                displayName    = hasCargo ? req.displayName    : "",
                icon           = hasCargo ? req.icon           : null,
                currentAmount  = 0,
                requiredAmount = hasCargo ? req.requiredAmount : 0,
                rewardAmount   = 0,
                mode           = hasCargo ? TrainWagonSlotMode.CargoRequest : TrainWagonSlotMode.Empty,
                isCollected    = false
            };
        }
    }

    /// Chuyá»ƒn SlotData tá»« CargoRequest â†’ Reward dÃ¹ng _pendingRewards.
    private void ApplyRewardsToSlots()
    {
        if (SlotData == null || _pendingRewards == null) return;

        for (int i = 0; i < SlotData.Length; i++)
        {
            var rew = i < _pendingRewards.Length ? _pendingRewards[i] : null;
            if (rew == null) { SlotData[i].mode = TrainWagonSlotMode.Empty; continue; }

            SlotData[i].mode         = TrainWagonSlotMode.Reward;
            SlotData[i].itemId       = rew.itemId;
            SlotData[i].displayName  = rew.displayName;
            SlotData[i].icon         = rew.icon;
            SlotData[i].rewardAmount = rew.rewardAmount;
            SlotData[i].isCollected  = false;
        }
    }

    // â”€â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ChangeState(TrainState newState)
    {
        State = newState;

        // Ẩn process popup cũ ở mọi state ngoại trừ Processing
        if (newState != TrainState.Processing)
            processPopup?.Hide();

        OnStateChanged?.Invoke(newState);
    }

    // â”€â”€â”€ UI helpers â€” Shipping slots â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void RefreshAllShippingSlots()
    {
        if (shippingWagonSlots == null || SlotData == null) return;
        for (int i = 0; i < shippingWagonSlots.Length; i++)
        {
            if (shippingWagonSlots[i] == null) continue;
            if (i < SlotData.Length) shippingWagonSlots[i].Refresh(SlotData[i]);
            else                     shippingWagonSlots[i].Hide();
        }
    }

    private void RefreshShippingSlotUI(int i)
    {
        if (shippingWagonSlots == null || i >= shippingWagonSlots.Length) return;
        if (shippingWagonSlots[i] == null || i >= SlotData.Length)        return;
        shippingWagonSlots[i].Refresh(SlotData[i]);
    }

    private void HideAllShippingSlots()
    {
        if (shippingWagonSlots == null) return;
        foreach (var s in shippingWagonSlots) s?.Hide();
    }

    private void DisableAllShippingSlotInteractions()
    {
        if (shippingWagonSlots == null) return;
        foreach (var s in shippingWagonSlots) s?.DisableInteraction();
    }

    // â”€â”€â”€ UI helpers â€” Reward slots â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void RefreshAllRewardSlots()
    {
        if (rewardWagonSlots == null || SlotData == null) return;
        for (int i = 0; i < rewardWagonSlots.Length; i++)
        {
            if (rewardWagonSlots[i] == null) continue;
            if (i < SlotData.Length) rewardWagonSlots[i].Refresh(SlotData[i]);
            else                     rewardWagonSlots[i].Hide();
        }
    }

    private void RefreshRewardSlotUI(int i)
    {
        if (rewardWagonSlots == null || i >= rewardWagonSlots.Length) return;
        if (rewardWagonSlots[i] == null || i >= SlotData.Length)      return;
        rewardWagonSlots[i].Refresh(SlotData[i]);
    }

    private void HideAllRewardSlots()
    {
        if (rewardWagonSlots == null) return;
        foreach (var s in rewardWagonSlots) s?.Hide();
    }

    private void DisableAllRewardSlotInteractions()
    {
        if (rewardWagonSlots == null) return;
        foreach (var s in rewardWagonSlots) s?.DisableInteraction();
    }

    // â”€â”€â”€ FX â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void SpawnItemFlyFX(int slotIndex, TrainWagonSlotData slot)
    {
        Vector3 pos = GetRewardSlotWorldPos(slotIndex);

        // F5 — 4 ref FX trong Inspector đang RỖNG (itemFlyFXPrefab, expFlyFXPrefab,
        // warehouseTargetTransform, expTargetTransform) nên thu thưởng tàu không có một
        // hiệu ứng nào: người chơi bấm mà không thấy gì, tưởng nút chết.
        //
        // Thay vì bắt ai đó nhớ kéo 4 ref vào scene, dùng lại HarvestFeedbackSpawner —
        // chính hệ FX mà ruộng và chuồng đã dùng, đã có sẵn đích kho/avatar. Ref trong
        // Inspector vẫn được ƯU TIÊN nếu có ai gán, nên không mất đường tuỳ biến.
        if (itemFlyFXPrefab != null && warehouseTargetTransform != null)
        {
            var fx = Instantiate(itemFlyFXPrefab, pos, Quaternion.identity);
            fx.GetComponent<HarvestFlyItemFX>()
              ?.Play(slot.icon, pos, warehouseTargetTransform.position);
            return;
        }

        HarvestFeedbackSpawner.Instance?.SpawnHarvestFly(slot.icon, pos, Mathf.Max(1, slot.rewardAmount));
    }

    private void SpawnExpFlyFX(int slotIndex)
    {
        Vector3 pos = GetRewardSlotWorldPos(slotIndex);

        if (expFlyFXPrefab != null && expTargetTransform != null)
        {
            var fx = Instantiate(expFlyFXPrefab, pos, Quaternion.identity);
            fx.GetComponent<ExpFlyToAvatarFX>()
              ?.Play(pos, expTargetTransform.position);
            return;
        }

        // Dự phòng: cùng hệ FX với ruộng/chuồng (xem giải thích ở SpawnItemFlyFX).
        HarvestFeedbackSpawner.Instance?.SpawnExpFly(pos, expPerReward);
    }

    private Vector3 GetRewardSlotWorldPos(int i)
    {
        if (rewardWagonSlots != null && i < rewardWagonSlots.Length && rewardWagonSlots[i] != null)
            return rewardWagonSlots[i].GetWorldPosition();
        return transform.position;
    }

    // â”€â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private bool IsValidSlot(int i) =>
        SlotData != null && i >= 0 && i < SlotData.Length && SlotData[i] != null;

    private TrainCargoPreset GetCargoPreset(int index)
    {
        if (cargoData?.presets?.Count > 0)
            return cargoData.presets[index % cargoData.presets.Count];

        Debug.LogWarning("[Train] ChÆ°a gÃ¡n TrainCargoData â€” dÃ¹ng fallback preset.");
        return new TrainCargoPreset
        {
            slots = new TrainCargoRequirement[]
            {
                new TrainCargoRequirement { itemId = "lua",   displayName = "LÃºa",   requiredAmount = 4 },
                new TrainCargoRequirement { itemId = "bap",   displayName = "Báº¯p",   requiredAmount = 3 },
                new TrainCargoRequirement { itemId = "trung", displayName = "Trá»©ng", requiredAmount = 2 },
                new TrainCargoRequirement { itemId = "nam",   displayName = "Náº¥m",   requiredAmount = 2 },
            }
        };
    }

    private TrainRewardPreset GetRewardPreset(int index)
    {
        if (rewardData?.presets?.Count > 0)
            return rewardData.presets[index % rewardData.presets.Count];

        Debug.LogWarning("[Train] ChÆ°a gÃ¡n TrainRewardData â€” dÃ¹ng fallback preset.");
        return new TrainRewardPreset
        {
            slots = new TrainRewardItem[]
            {
                new TrainRewardItem { itemId = "da",   displayName = "ÄÃ¡",   rewardAmount = 2 },
                new TrainRewardItem { itemId = "gach", displayName = "Gáº¡ch", rewardAmount = 1 },
                new TrainRewardItem { itemId = "dinh", displayName = "Äinh", rewardAmount = 3 },
                new TrainRewardItem { itemId = "kim",  displayName = "Kim",  rewardAmount = 1 },
            }
        };
    }
    // ─── Persistence — chuyến tàu sống sót qua tắt game (duyệt 2026-08-26) ───────

    private const string SaveKey = "train_trip_state_v1";

    [System.Serializable]
    private class TrainSaveData
    {
        public int    state;
        public int    tripIndex;
        public double tripEndUnix;
        public int[]  currentAmounts;
        public bool[] collected;
    }

    private void SaveTrainState()
    {
        try
        {
            var d = new TrainSaveData
            {
                state       = (int)State,
                tripIndex   = _tripIndex,
                tripEndUnix = _tripEndUnix,
            };
            if (SlotData != null)
            {
                d.currentAmounts = new int[SlotData.Length];
                d.collected      = new bool[SlotData.Length];
                for (int i = 0; i < SlotData.Length; i++)
                {
                    d.currentAmounts[i] = SlotData[i] != null ? SlotData[i].currentAmount : 0;
                    d.collected[i]      = SlotData[i] != null && SlotData[i].isCollected;
                }
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Train] Không lưu được trạng thái tàu: {e.Message}");
        }
    }

    /// <summary>true nếu khôi phục được chuyến dở dang (tự set state + visual tương ứng).</summary>
    private bool TryRestoreTrainState()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return false;

        TrainSaveData d;
        try   { d = JsonUtility.FromJson<TrainSaveData>(PlayerPrefs.GetString(SaveKey)); }
        catch { return false; }
        if (d == null) return false;

        _tripIndex   = Mathf.Max(0, d.tripIndex);
        _tripEndUnix = d.tripEndUnix;
        GenerateNewTrip();

        var saved = (TrainState)d.state;
        switch (saved)
        {
            case TrainState.WaitingForLoad:
                if (d.currentAmounts != null && SlotData != null)
                    for (int i = 0; i < SlotData.Length && i < d.currentAmounts.Length; i++)
                        SlotData[i].currentAmount = d.currentAmounts[i];
                ShowShippingAtHiddenThenMoveToStation(() =>
                {
                    ChangeState(TrainState.WaitingForLoad);
                    RefreshAllShippingSlots();
                    CheckAllLoaded(); // M1: save cũ đã đủ hàng → tự khởi hành, không kẹt ga
                });
                return true;

            case TrainState.ShipDeparting:
                // Thoát game giữa animation ga→hầm: timer CHƯA từng chạy → bắt đầu chuyến đủ giờ
                SnapshotSentCargo();
                shippingPathFollower.HideTrain();
                HideAllShippingSlots();
                shippingPathFollower.SnapToPosition(pointHiddenShip.position,
                    (pointHiddenShip.position - pointStationShip.position).normalized);
                StartProcessing(tripDurationSeconds);
                return true;

            case TrainState.Processing:
                // Đang vận chuyển — 2 tàu ẩn, Update() sẽ tự kết thúc nếu đã hết giờ (kể cả offline)
                SnapshotSentCargo();
                shippingPathFollower.HideTrain();
                HideAllShippingSlots();
                shippingPathFollower.SnapToPosition(pointHiddenShip.position,
                    (pointHiddenShip.position - pointStationShip.position).normalized);
                ChangeState(TrainState.Processing);
                return true;

            case TrainState.RewardArriving:
            case TrainState.RewardReadyToCollect:
                SnapshotSentCargo();
                shippingPathFollower.HideTrain();
                HideAllShippingSlots();
                shippingPathFollower.SnapToPosition(pointHiddenShip.position,
                    (pointHiddenShip.position - pointStationShip.position).normalized);
                ApplyRewardsToSlots();
                if (d.collected != null && SlotData != null)
                    for (int i = 0; i < SlotData.Length && i < d.collected.Length; i++)
                        SlotData[i].isCollected = d.collected[i];
                RefreshAllRewardSlots();
                DisableAllRewardSlotInteractions();
                ShowRewardAtTunnelThenMoveToStation(() =>
                {
                    OnRewardArrivedAtStation();
                    CheckAllCollected(); // save hiếm: đã thu hết nhưng chưa kịp rời ga → tự rời
                });
                return true;

            default:
                // RewardDeparting — coi như chuyến đã xong, sang chuyến mới
                _tripIndex++;
                GenerateNewTrip();
                ShowShippingAtHiddenThenMoveToStation(() =>
                {
                    ChangeState(TrainState.WaitingForLoad);
                    RefreshAllShippingSlots();
                    SaveTrainState();
                });
                return true;
        }
    }
}
