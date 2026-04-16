using System.Collections;
using UnityEngine;

/// <summary>
/// State machine trung tâm — quản lý logic 2 tàu:
///
///   TrainVisualRoot  (Reward Train  / tàu CŨ)
///     → xuất hiện từ hầm, chạy về ga, trả reward cho user
///
///   TrainVisualRoot2 (Shipping Train / tàu MỚI)
///     → đứng ga chờ user nạp hàng, rồi chạy vào hầm và ẩn
///
/// 6-state machine:
///   WaitingForLoad       → tàu MỚI snap tại ga, user nạp hàng
///   ShipDeparting        → tàu MỚI: ga → hầm → (teleport về pointHiddenShip) → ẩn
///   Processing           → 2 tàu ẩn, timer chạy
///   RewardArriving       → tàu CŨ snap tại hầm, chạy về ga
///   RewardReadyToCollect → tàu CŨ tại ga, user thu reward
///   RewardDeparting      → tàu CŨ: ga → pointHiddenReward → (teleport về hầm) → ẩn
///                          → tàu MỚI snap thẳng về ga → WaitingForLoad
/// </summary>
public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }

    // ─── Inspector ───────────────────────────────────────────────

    [Header("Data (ScriptableObjects)")]
    [SerializeField] private TrainCargoData  cargoData;
    [SerializeField] private TrainRewardData rewardData;

    [Header("Shipping Train — tàu MỚI (TrainVisualRoot2) nhận hàng đi")]
    [Tooltip("TrainPathFollower gắn trên ShippingTrain GO (quản lý TrainVisualRoot2)")]
    [SerializeField] private TrainPathFollower shippingPathFollower;

    [Header("Reward Train — tàu CŨ (TrainVisualRoot) mang reward về")]
    [Tooltip("TrainPathFollower gắn trên RewardTrain GO (quản lý TrainVisualRoot)")]
    [SerializeField] private TrainPathFollower rewardPathFollower;

    [Header("Waypoints — Shipping Train (tàu MỚI)")]
    [Tooltip("Off-screen trái — nơi tàu MỚI spawn vào đầu chuyến (trừ chuyến 1)")]
    [SerializeField] private Transform pointHiddenShip;
    [Tooltip("Trên ga — nơi tàu MỚI đứng chờ user nạp hàng")]
    [SerializeField] private Transform pointStationShip;
    [Tooltip("Cửa hầm — nơi tàu MỚI chạy tới rồi ẩn")]
    [SerializeField] private Transform pointTunnelShip;

    [Header("Waypoints — Reward Train (tàu CŨ)")]
    [Tooltip("Cửa hầm — nơi tàu CŨ xuất hiện sau khi timer xong")]
    [SerializeField] private Transform pointTunnelReward;
    [Tooltip("Trên ga — nơi tàu CŨ dừng để user thu reward")]
    [SerializeField] private Transform pointStationReward;
    [Tooltip("Off-screen phải — nơi tàu CŨ chạy ra rồi ẩn sau khi user thu xong")]
    [SerializeField] private Transform pointHiddenReward;

    [Header("Wagon Slots — Shipping Train (4 toa tàu MỚI)")]
    [Tooltip("4 TrainWagonSlot gắn trên wagon của TrainVisualRoot2, theo thứ tự 0..3")]
    [SerializeField] private TrainWagonSlot[] shippingWagonSlots;

    [Header("Wagon Slots — Reward Train (4 toa tàu CŨ)")]
    [Tooltip("4 TrainWagonSlot gắn trên wagon của TrainVisualRoot, theo thứ tự 0..3")]
    [SerializeField] private TrainWagonSlot[] rewardWagonSlots;

    [Header("Popups")]
    [Tooltip("Popup nạp hàng — Popup_item_Train")]
    [SerializeField] private TrainLoadPopupUI    loadPopup;
    [Tooltip("Popup trạng thái / timer — Popup_train")]
    [SerializeField] private TrainProcessPopupUI processPopup;

    [Header("FX — Reward Collection")]
    [Tooltip("Prefab HarvestFlyItemFX — bay từ toa tàu CŨ về kho")]
    [SerializeField] private GameObject itemFlyFXPrefab;
    [Tooltip("Prefab ExpFlyToAvatarFX — bay từ toa tàu CŨ về avatar")]
    [SerializeField] private GameObject expFlyFXPrefab;
    [Tooltip("Vị trí icon kho trên HUD (đích của item FX)")]
    [SerializeField] private Transform  warehouseTargetTransform;
    [Tooltip("Vị trí icon avatar/EXP trên HUD (đích của exp FX)")]
    [SerializeField] private Transform  expTargetTransform;

    [Header("Config")]
    [Tooltip("EXP thưởng mỗi lần thu 1 slot reward")]
    [SerializeField] private int   expPerReward        = 10;
    [Tooltip("Thời gian xử lý trong hầm (giây). KHÔNG tính thời gian tàu di chuyển.")]
    [SerializeField] private float tripDurationSeconds = 4f;

    // ─── Runtime ─────────────────────────────────────────────────

    public TrainState          State    { get; private set; }
    public TrainWagonSlotData[] SlotData { get; private set; }

    private int               _tripIndex      = 0;
    private TrainRewardItem[] _pendingRewards;
    private float             _tripEndTime;
    private bool              _timerActive    = false;

    /// Thời gian còn lại của Processing timer (giây).
    public float TripRemainingTime => Mathf.Max(0f, _tripEndTime - Time.time);

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Auto-find popup nếu chưa gán trong Inspector
        if (loadPopup == null)
        {
            loadPopup = FindFirstObjectByType<TrainLoadPopupUI>(FindObjectsInactive.Include);
            if (loadPopup == null)
                Debug.LogError("[Train] Không tìm thấy TrainLoadPopupUI! Kéo Popup_Item_Train vào Inspector.");
        }
        if (processPopup == null) processPopup = FindFirstObjectByType<TrainProcessPopupUI>(FindObjectsInactive.Include);

        // Kiểm tra các field bắt buộc
        if (shippingPathFollower == null)
            Debug.LogError("[Train] shippingPathFollower chưa gán! Kéo PathFollower của tàu MỚI (TrainVisualRoot2) vào.");
        if (rewardPathFollower == null)
            Debug.LogError("[Train] rewardPathFollower chưa gán! Kéo PathFollower của tàu CŨ (TrainVisualRoot) vào.");
        if (shippingPathFollower != null && shippingPathFollower == rewardPathFollower)
            Debug.LogError("[Train] BUG INSPECTOR: shippingPathFollower và rewardPathFollower đang trỏ vào CÙNG 1 object! Phải gán 2 object khác nhau.");

        if (pointHiddenShip   == null) Debug.LogError("[Train] pointHiddenShip chưa gán!");
        if (pointStationShip  == null) Debug.LogError("[Train] pointStationShip chưa gán!");
        if (pointTunnelShip   == null) Debug.LogError("[Train] pointTunnelShip chưa gán!");
        if (pointTunnelReward == null) Debug.LogError("[Train] pointTunnelReward chưa gán!");
        if (pointStationReward== null) Debug.LogError("[Train] pointStationReward chưa gán!");
        if (pointHiddenReward == null) Debug.LogError("[Train] pointHiddenReward chưa gán!");

        // Khởi tạo sau 1 frame để chắc chắn tất cả Start() khác đã chạy
        StartCoroutine(InitAfterFrame());
    }

    private IEnumerator InitAfterFrame()
    {
        yield return null; // chờ 1 frame

        // Tàu CŨ ẩn hoàn toàn lúc đầu
        rewardPathFollower?.HideTrain();
        HideAllRewardSlots();

        // Tạo chuyến đầu tiên
        GenerateNewTrip();

        // Chặng 1: HiddenShip → StationShip
        // Tàu MỚI xuất hiện kín đáo tại HiddenShip rồi chạy ra ga
        ShowShippingAtHiddenThenMoveToStation(() =>
        {
            ChangeState(TrainState.WaitingForLoad);
            RefreshAllShippingSlots();
        });
    }

    void Update()
    {
        // Timer tạm tắt — flow chạy liền không đợi
        // if (!_timerActive) return;
        // processPopup?.UpdateTimer(TripRemainingTime);
        // if (TripRemainingTime <= 0f)
        // {
        //     _timerActive = false;
        //     OnProcessingTimerExpired();
        // }
    }

    // ─── Public API (gọi từ TrainWagonSlot & TrainLoadPopupUI) ───

    /// Điểm vào duy nhất từ TrainWagonSlot.OnMouseDown().
    /// Routing theo state — TrainManager tự quyết định làm gì với click đó.
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
                Debug.Log($"[Train] Slot {idx} click bỏ qua — state={State}");
                break;
        }
    }

    /// Người chơi click toa của tàu MỚI → mở popup nạp hàng.
    public void OnCargoSlotClicked(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex))            return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete)                         return;

        if (loadPopup == null)
        {
            Debug.LogWarning("[Train] loadPopup == null — kéo Popup_item_Train vào Inspector.");
            return;
        }

        Debug.Log($"[Train] Mở popup toa {slotIndex} — {slot.displayName} {slot.currentAmount}/{slot.requiredAmount}");
        loadPopup.OpenForCargoSlot(slotIndex, slot);
    }

    /// Nút "Thêm" trong popup nạp hàng — trừ 1 item kho, tăng currentAmount.
    public void TryAddOneItemToSlot(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex))            return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete)                         return;

        if (!TrainInventoryAdapter.HasItem(slot.itemId, 1))
        {
            Debug.LogWarning($"[Train] Không đủ '{slot.displayName}' trong kho.");
            return; // Popup vẫn mở để user tự đóng
        }

        if (!TrainInventoryAdapter.RemoveItem(slot.itemId, 1)) return;

        slot.currentAmount++;
        Debug.Log($"[Train] Nạp toa {slotIndex}: {slot.displayName} {slot.currentAmount}/{slot.requiredAmount}");

        loadPopup?.RefreshPopup();
        RefreshShippingSlotUI(slotIndex);

        if (slot.IsCargoComplete)
        {
            loadPopup?.ClosePopup();
            CheckAllLoaded();
        }
    }

    /// Người chơi click toa của tàu CŨ để thu reward.
    public void CollectReward(int slotIndex)
    {
        if (State != TrainState.RewardReadyToCollect) return;
        if (!IsValidSlot(slotIndex))                  return;

        var slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.Reward) return;
        if (slot.isCollected)                       return;

        // Đánh dấu trước — ngăn double-click trong lúc FX đang chạy
        slot.isCollected = true;

        TrainInventoryAdapter.AddItem(slot.itemId, slot.displayName, slot.icon, slot.rewardAmount);

        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(expPerReward);

        Debug.Log($"[Train] Thu reward toa {slotIndex}: {slot.displayName} x{slot.rewardAmount} (+{expPerReward} EXP)");

        SpawnItemFlyFX(slotIndex, slot);
        SpawnExpFlyFX(slotIndex);
        RefreshRewardSlotUI(slotIndex);
        CheckAllCollected();
    }

    // ─── Flow: WaitingForLoad → ShipDeparting ────────────────────

    private void CheckAllLoaded()
    {
        if (SlotData == null) return;
        foreach (var s in SlotData)
            if (s.mode == TrainWagonSlotMode.CargoRequest && !s.IsCargoComplete) return;

        Debug.Log("[Train] Tất cả toa đầy → tàu MỚI khởi hành vào hầm");

        // Tắt collider (giữ visual cargo trên toa suốt hành trình)
        DisableAllShippingSlotInteractions();
        ChangeState(TrainState.ShipDeparting);

        // Chặng 2: StationShip → TunnelShip
        SendShippingFromStationToTunnel();
    }

    // ─── Flow: ShipDeparting → Processing ────────────────────────

    private void OnShippingReachedTunnel()
    {
        Debug.Log("[Train] Tàu MỚI tới hầm → ẩn, teleport về HiddenShip, tàu CŨ xuất hiện ngay");

        // 1. Ẩn tàu shipping
        shippingPathFollower.HideTrain();
        HideAllShippingSlots();

        // 2. Teleport shipping về vị trí ẩn (sẵn sàng chuyến sau)
        Vector3 hiddenPos   = pointHiddenShip.position;
        Vector3 stationPos  = pointStationShip.position;
        Vector3 backwardDir = (hiddenPos - stationPos).normalized;
        shippingPathFollower.SnapToPosition(hiddenPos, backwardDir);

        // 3. Áp reward vào slot data
        ApplyRewardsToSlots();

        // 4. Tàu reward xuất hiện từ hầm chạy về ga NGAY LẬP TỨC
        ChangeState(TrainState.RewardArriving);
        RefreshAllRewardSlots();
        DisableAllRewardSlotInteractions();
        ShowRewardAtTunnelThenMoveToStation(OnRewardArrivedAtStation);
    }

    // StartProcessingTimer() — tạm giữ để bật lại nếu cần timer sau này
    // private void StartProcessingTimer()
    // {
    //     _tripEndTime = Time.time + tripDurationSeconds;
    //     _timerActive = true;
    //     processPopup?.Show(tripDurationSeconds);
    // }

    // ─── Flow: Processing → RewardArriving ───────────────────────

    private void OnProcessingTimerExpired()
    {
        Debug.Log("[Train] Timer hết → tàu CŨ xuất hiện từ hầm");

        processPopup?.Hide();

        // Áp reward vào SlotData trước khi tàu hiện
        ApplyRewardsToSlots();

        ChangeState(TrainState.RewardArriving);

        // Refresh reward slots nhưng tắt collider (tàu đang di chuyển)
        RefreshAllRewardSlots();
        DisableAllRewardSlotInteractions();

        // Chặng 3: TunnelReward → StationReward
        ShowRewardAtTunnelThenMoveToStation(OnRewardArrivedAtStation);
    }

    // ─── Flow: RewardArriving → RewardReadyToCollect ─────────────

    private void OnRewardArrivedAtStation()
    {
        Debug.Log("[Train] Tàu CŨ về ga → RewardReadyToCollect");

        ChangeState(TrainState.RewardReadyToCollect);

        // Refresh lại để bật collider cho user click
        RefreshAllRewardSlots();
    }

    // ─── Flow: RewardReadyToCollect → RewardDeparting ────────────

    private void CheckAllCollected()
    {
        if (SlotData == null) return;
        foreach (var s in SlotData)
            if (s.mode == TrainWagonSlotMode.Reward && !s.isCollected) return;

        Debug.Log("[Train] Thu hết reward → tàu CŨ rời ga");

        HideAllRewardSlots();
        ChangeState(TrainState.RewardDeparting);

        // Chặng 4: StationReward → HiddenReward
        SendRewardFromStationToHidden();
    }

    // ─── Flow: RewardDeparting → WaitingForLoad ──────────────────

    private void OnRewardReachedHidden()
    {
        Debug.Log("[Train] Tàu CŨ đã khuất → ẩn, teleport về TunnelReward, tàu MỚI xuất hiện");

        // 1. Ẩn tàu reward
        rewardPathFollower.HideTrain();
        HideAllRewardSlots();

        // 2. Teleport reward về hầm (sẵn sàng chuyến sau)
        Vector3 tunnelPos   = pointTunnelReward.position;
        Vector3 stationPos  = pointStationReward.position;
        Vector3 backwardDir = (tunnelPos - stationPos).normalized;
        rewardPathFollower.SnapToPosition(tunnelPos, backwardDir);

        // 3. Tạo chuyến mới
        _tripIndex++;
        GenerateNewTrip();

        // 4. Tàu shipping xuất hiện từ HiddenShip chạy về ga NGAY
        ShowShippingAtHiddenThenMoveToStation(() =>
        {
            ChangeState(TrainState.WaitingForLoad);
            RefreshAllShippingSlots();
        });
    }

    // ─── Point-role helpers (4 chặng gameplay) ───────────────────

    /// Chặng 1 — Shipping spawn: HiddenShip → StationShip
    /// Tàu MỚI xuất hiện kín đáo tại HiddenShip rồi chạy ra ga chờ nạp hàng.
    private void ShowShippingAtHiddenThenMoveToStation(System.Action onArrived = null)
    {
        if (shippingPathFollower == null) return;

        Vector3 hiddenPos  = pointHiddenShip  != null ? pointHiddenShip.position  : transform.position;
        Vector3 stationPos = pointStationShip != null ? pointStationShip.position : transform.position;

        // backwardDir = ngược chiều chạy (HiddenShip → StationShip)
        // wagon trải về phía sau HiddenShip, khuất tầm nhìn
        Vector3 backwardDir = (hiddenPos - stationPos).normalized;

        // ShowTrain TRƯỚC để GO active, sau đó SnapToPosition + MoveTo mới hoạt động
        shippingPathFollower.ShowTrain();
        shippingPathFollower.SnapToPosition(hiddenPos, backwardDir);
        shippingPathFollower.MoveTo(stationPos, onArrived);
    }

    /// Chặng 2 — Shipping depart: StationShip → TunnelShip
    /// Tàu MỚI rời ga chạy vào hầm rồi ẩn.
    private void SendShippingFromStationToTunnel()
    {
        shippingPathFollower.MoveTo(pointTunnelShip.position, OnShippingReachedTunnel);
    }

    /// Chặng 3 — Reward arrive: TunnelReward → StationReward
    /// Tàu CŨ xuất hiện tại cửa hầm rồi chạy về ga để user nhận reward.
    private void ShowRewardAtTunnelThenMoveToStation(System.Action onArrived = null)
    {
        if (rewardPathFollower == null) return;

        Vector3 tunnelPos  = pointTunnelReward  != null ? pointTunnelReward.position  : transform.position;
        Vector3 stationPos = pointStationReward != null ? pointStationReward.position : transform.position;

        // backwardDir = ngược chiều chạy (TunnelReward → StationReward)
        Vector3 backwardDir = (tunnelPos - stationPos).normalized;

        // ShowTrain TRƯỚC để GO active, sau đó SnapToPosition + MoveTo mới hoạt động
        rewardPathFollower.ShowTrain();
        rewardPathFollower.SnapToPosition(tunnelPos, backwardDir);
        rewardPathFollower.MoveTo(stationPos, onArrived);
    }

    /// Chặng 4 — Reward leave: StationReward → HiddenReward
    /// Tàu CŨ rời ga chạy ra điểm khuất rồi ẩn.
    private void SendRewardFromStationToHidden()
    {
        rewardPathFollower.MoveTo(pointHiddenReward.position, OnRewardReachedHidden);
    }

    // ─── Trip generation ─────────────────────────────────────────

    /// Tạo SlotData và _pendingRewards từ preset.
    /// Chỉ setup data, không đổi state, không refresh UI.
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

    /// Chuyển SlotData từ CargoRequest → Reward dùng _pendingRewards.
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

    // ─── State ───────────────────────────────────────────────────

    private void ChangeState(TrainState newState)
    {
        State = newState;
        Debug.Log($"[Train] ── State: {newState} ──");

        // Ẩn process popup mọi state ngoại trừ Processing
        // (Processing tự show trong StartProcessing)
        if (newState != TrainState.Processing)
            processPopup?.Hide();
    }

    // ─── UI helpers — Shipping slots ─────────────────────────────

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

    // ─── UI helpers — Reward slots ───────────────────────────────

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

    // ─── FX ──────────────────────────────────────────────────────

    private void SpawnItemFlyFX(int slotIndex, TrainWagonSlotData slot)
    {
        if (itemFlyFXPrefab == null || warehouseTargetTransform == null) return;

        Vector3 pos = GetRewardSlotWorldPos(slotIndex);
        var fx = Instantiate(itemFlyFXPrefab, pos, Quaternion.identity);
        fx.GetComponent<HarvestFlyItemFX>()
          ?.Play(slot.icon, pos, warehouseTargetTransform.position);
    }

    private void SpawnExpFlyFX(int slotIndex)
    {
        if (expFlyFXPrefab == null || expTargetTransform == null) return;

        Vector3 pos = GetRewardSlotWorldPos(slotIndex);
        var fx = Instantiate(expFlyFXPrefab, pos, Quaternion.identity);
        fx.GetComponent<ExpFlyToAvatarFX>()
          ?.Play(pos, expTargetTransform.position);
    }

    private Vector3 GetRewardSlotWorldPos(int i)
    {
        if (rewardWagonSlots != null && i < rewardWagonSlots.Length && rewardWagonSlots[i] != null)
            return rewardWagonSlots[i].GetWorldPosition();
        return transform.position;
    }

    // ─── Helpers ─────────────────────────────────────────────────

    private bool IsValidSlot(int i) =>
        SlotData != null && i >= 0 && i < SlotData.Length && SlotData[i] != null;

    private TrainCargoPreset GetCargoPreset(int index)
    {
        if (cargoData?.presets?.Count > 0)
            return cargoData.presets[index % cargoData.presets.Count];

        Debug.LogWarning("[Train] Chưa gán TrainCargoData — dùng fallback preset.");
        return new TrainCargoPreset
        {
            slots = new TrainCargoRequirement[]
            {
                new TrainCargoRequirement { itemId = "lua",   displayName = "Lúa",   requiredAmount = 4 },
                new TrainCargoRequirement { itemId = "bap",   displayName = "Bắp",   requiredAmount = 3 },
                new TrainCargoRequirement { itemId = "trung", displayName = "Trứng", requiredAmount = 2 },
                new TrainCargoRequirement { itemId = "nam",   displayName = "Nấm",   requiredAmount = 2 },
            }
        };
    }

    private TrainRewardPreset GetRewardPreset(int index)
    {
        if (rewardData?.presets?.Count > 0)
            return rewardData.presets[index % rewardData.presets.Count];

        Debug.LogWarning("[Train] Chưa gán TrainRewardData — dùng fallback preset.");
        return new TrainRewardPreset
        {
            slots = new TrainRewardItem[]
            {
                new TrainRewardItem { itemId = "da",   displayName = "Đá",   rewardAmount = 2 },
                new TrainRewardItem { itemId = "gach", displayName = "Gạch", rewardAmount = 1 },
                new TrainRewardItem { itemId = "dinh", displayName = "Đinh", rewardAmount = 3 },
                new TrainRewardItem { itemId = "kim",  displayName = "Kim",  rewardAmount = 1 },
            }
        };
    }
}
