using UnityEngine;

/// <summary>
/// State machine trung tâm của hệ thống tàu hàng.
///
/// States:
///   WaitingForLoad        → người chơi click toa (world BoxCollider2D) → popup nạp hàng
///   Departing             → tàu chạy Point_01 → Point_02 → Point_00
///   ReturningWithReward   → tàu quay về Point_00 → Point_01
///   RewardReadyToCollect  → người chơi click từng toa để thu reward + FX
///
/// Sau khi thu hết reward:
///   tàu chạy Point_01 → Point_02 → Point_01 (quay đầu) → GenerateNewTrip → WaitingForLoad
/// </summary>
public class TrainManager : MonoBehaviour
{
    public static TrainManager Instance { get; private set; }

    // ─── Inspector ───────────────────────────────────────────────

    [Header("Data (ScriptableObjects)")]
    [SerializeField] private TrainCargoData  cargoData;
    [SerializeField] private TrainRewardData rewardData;

    [Header("Movement")]
    [SerializeField] private TrainPathFollower pathFollower;

    [Header("World Wagon Slots — WorldSlot_01..04 (index 0..3)")]
    [Tooltip("Kéo WorldSlot_01..04 vào đây theo thứ tự toa 0..3. Mỗi slot phải có BoxCollider2D.")]
    [SerializeField] private TrainWagonSlot[] wagonSlots;

    [Header("Load Popup — Popup_Item_Train")]
    [SerializeField] private TrainLoadPopupUI loadPopup;

    [Header("Process Popup — Popup_train (hiện khi tàu đang đi)")]
    [SerializeField] private TrainProcessPopupUI processPopup;

    [Header("FX — Reward Collection")]
    [Tooltip("Prefab chứa HarvestFlyItemFX — bay từ toa về kho")]
    [SerializeField] private GameObject itemFlyFXPrefab;
    [Tooltip("Prefab chứa ExpFlyToAvatarFX — bay từ toa về avatar")]
    [SerializeField] private GameObject expFlyFXPrefab;
    [Tooltip("Vị trí icon kho trên HUD — điểm đích của item fly FX")]
    [SerializeField] private Transform  warehouseTargetTransform;
    [Tooltip("Vị trí icon EXP / avatar trên HUD — điểm đích của exp fly FX")]
    [SerializeField] private Transform  expTargetTransform;

    [Header("EXP thưởng mỗi lần thu 1 slot reward")]
    [SerializeField] private int expPerReward = 10;

    [Header("Trip Duration")]
    [SerializeField] private float tripDurationSeconds = 300f;

    // ─── Runtime ─────────────────────────────────────────────────

    public TrainState State { get; private set; } = TrainState.WaitingForLoad;

    /// Dữ liệu runtime từng toa (dùng chung cho CargoRequest và Reward).
    public TrainWagonSlotData[] SlotData { get; private set; }

    private int               _tripIndex      = 0;
    private TrainRewardItem[] _pendingRewards; // lưu reward preset để áp dụng khi tàu về
    private float             _tripEndTime;

    public float TripRemainingTime => Mathf.Max(0f, _tripEndTime - Time.time);

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Auto-find nếu Inspector chưa gán
        if (loadPopup   == null) loadPopup   = FindObjectOfType<TrainLoadPopupUI>(true);
        if (processPopup == null) processPopup = FindObjectOfType<TrainProcessPopupUI>(true);

        if (loadPopup   == null) Debug.LogWarning("[Train] Không tìm thấy TrainLoadPopupUI trong scene — kéo Popup_item_Train vào field 'Load Popup'.");
        if (processPopup == null) Debug.LogWarning("[Train] Không tìm thấy TrainProcessPopupUI trong scene — kéo Popup_train vào field 'Process Popup'.");

        if (pathFollower != null)
        {
            pathFollower.onArrivedAtPoint00            = OnArrivedAtPoint00;
            pathFollower.onArrivedAtPoint01AfterReturn = OnArrivedAfterReturn;
            pathFollower.onResetMoveDone               = OnResetMoveDone;
        }

        GenerateNewTrip();
    }

    void Update()
    {
        if (State == TrainState.Departing || State == TrainState.ReturningWithReward)
            processPopup?.UpdateTimer(TripRemainingTime);
    }

    // ─── Public API (gọi từ TrainWagonSlot & TrainLoadPopupUI) ───

    /// <summary>
    /// Người chơi click 1 toa đang chờ nạp hàng → mở popup nạp hàng.
    /// Gọi từ TrainWagonSlot.OnMouseDown() khi State == WaitingForLoad.
    /// </summary>
    public void OnCargoSlotClicked(int slotIndex)
    {
        Debug.Log($"[TrainManager] OnCargoSlotClicked({slotIndex}) — loadPopup={loadPopup}");
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex)) return;

        TrainWagonSlotData slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete) return;

        if (loadPopup == null)
        {
            Debug.LogWarning("[Train] loadPopup == null! Kéo Popup_item_Train vào field 'Load Popup' trên TrainManager trong Inspector.");
            return;
        }

        Debug.Log($"[Train] Mở popup cho toa {slotIndex} — {slot.displayName} {slot.currentAmount}/{slot.requiredAmount}");
        loadPopup.OpenForCargoSlot(slotIndex, slot);
    }

    /// <summary>
    /// Nút "Thêm" trong popup: trừ 1 item từ kho, tăng currentAmount.
    /// Gọi từ TrainLoadPopupUI.OnThemHangClicked().
    /// Khi toa đầy: đóng popup và kiểm tra xem tất cả toa đã đủ chưa.
    /// </summary>
    public void TryAddOneItemToSlot(int slotIndex)
    {
        if (State != TrainState.WaitingForLoad) return;
        if (!IsValidSlot(slotIndex)) return;

        TrainWagonSlotData slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.CargoRequest) return;
        if (slot.IsCargoComplete) return;
        Debug.Log($"[Train] TryAdd slot={slotIndex} itemId='{slot.itemId}' hasItem={TrainInventoryAdapter.HasItem(slot.itemId, 1)}");

        if (!TrainInventoryAdapter.HasItem(slot.itemId, 1))
        {
            Debug.LogWarning($"[Train] Không đủ '{slot.displayName}' trong kho để nạp.");
            // Popup vẫn mở, người chơi tự đóng
            return;
        }

        if (!TrainInventoryAdapter.RemoveItem(slot.itemId, 1)) return;

        slot.currentAmount++;
        Debug.Log($"[Train] Nạp toa {slotIndex}: {slot.displayName} {slot.currentAmount}/{slot.requiredAmount}");

        loadPopup?.RefreshPopup();
        RefreshSlotUI(slotIndex);

        if (slot.IsCargoComplete)
        {
            Debug.Log($"[Train] Toa {slotIndex} đã đầy hàng.");
            loadPopup?.ClosePopup();
            CheckAllLoaded();
        }
    }

    /// <summary>
    /// Người chơi click 1 toa reward → thu item vào kho + FX + EXP.
    /// Gọi từ TrainWagonSlot.OnMouseDown() khi State == RewardReadyToCollect.
    /// Sau khi thu hết tất cả toa: tàu tiếp tục quay đầu rồi về WaitingForLoad.
    /// </summary>
    public void CollectReward(int slotIndex)
    {
        if (State != TrainState.RewardReadyToCollect) return;
        if (!IsValidSlot(slotIndex)) return;

        TrainWagonSlotData slot = SlotData[slotIndex];
        if (slot.mode != TrainWagonSlotMode.Reward) return;
        if (slot.isCollected) return;

        // Đánh dấu collected trước (ngăn double-click trong thời gian FX)
        slot.isCollected = true;

        // Cộng item vào kho
        TrainInventoryAdapter.AddItem(slot.itemId, slot.displayName, slot.icon, slot.rewardAmount);

        // Cộng EXP
        if (PlayerProgressManager.Instance != null)
            PlayerProgressManager.Instance.AddExp(expPerReward);

        Debug.Log($"[Train] Thu reward toa {slotIndex}: {slot.displayName} x{slot.rewardAmount} (+{expPerReward} EXP)");

        // FX bay về kho và về avatar
        SpawnItemFlyFX(slotIndex, slot);
        SpawnExpFlyFX(slotIndex);

        // Ẩn slot visual ngay lập tức
        RefreshSlotUI(slotIndex);

        // Kiểm tra đã thu hết chưa → nếu hết thì tiếp tục luồng
        CheckAllCollected();
    }

    // ─── FX ──────────────────────────────────────────────────────

    private void SpawnItemFlyFX(int slotIndex, TrainWagonSlotData slot)
    {
        if (itemFlyFXPrefab == null || warehouseTargetTransform == null)
        {
            Debug.Log($"[Train] TODO: Gán itemFlyFXPrefab và warehouseTargetTransform trong Inspector.");
            return;
        }

        Vector3 spawnPos = GetSlotWorldPos(slotIndex);
        GameObject fx = Instantiate(itemFlyFXPrefab, spawnPos, Quaternion.identity);
        HarvestFlyItemFX flyFX = fx.GetComponent<HarvestFlyItemFX>();

        if (flyFX != null)
            flyFX.Play(slot.icon, spawnPos, warehouseTargetTransform.position);
        else
            Debug.LogWarning("[Train] itemFlyFXPrefab thiếu component HarvestFlyItemFX.");
    }

    private void SpawnExpFlyFX(int slotIndex)
    {
        if (expFlyFXPrefab == null || expTargetTransform == null)
        {
            Debug.Log("[Train] TODO: Gán expFlyFXPrefab và expTargetTransform trong Inspector.");
            return;
        }

        Vector3 spawnPos = GetSlotWorldPos(slotIndex);
        GameObject fx = Instantiate(expFlyFXPrefab, spawnPos, Quaternion.identity);
        ExpFlyToAvatarFX expFX = fx.GetComponent<ExpFlyToAvatarFX>();

        if (expFX != null)
            expFX.Play(spawnPos, expTargetTransform.position);
        else
            Debug.LogWarning("[Train] expFlyFXPrefab thiếu component ExpFlyToAvatarFX.");
    }

    private Vector3 GetSlotWorldPos(int slotIndex)
    {
        if (wagonSlots != null && slotIndex < wagonSlots.Length && wagonSlots[slotIndex] != null)
            return wagonSlots[slotIndex].GetWorldPosition();
        return transform.position;
    }

    // ─── Trip generation ─────────────────────────────────────────

    private void GenerateNewTrip()
    {
        TrainCargoPreset  cargoPre  = GetCargoPreset(_tripIndex);
        TrainRewardPreset rewardPre = GetRewardPreset(_tripIndex);

        int presetCount = Mathf.Max(
            cargoPre.slots  != null ? cargoPre.slots.Length  : 0,
            rewardPre.slots != null ? rewardPre.slots.Length : 0);
        int slotCount = wagonSlots != null ? wagonSlots.Length : presetCount;
        slotCount = Mathf.Max(slotCount, presetCount);

        SlotData        = new TrainWagonSlotData[slotCount];
        _pendingRewards = new TrainRewardItem[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            bool hasCargo = cargoPre.slots != null && i < cargoPre.slots.Length;
            TrainCargoRequirement req = hasCargo ? cargoPre.slots[i] : null;

            bool hasReward = rewardPre.slots != null && i < rewardPre.slots.Length;
            _pendingRewards[i] = hasReward ? rewardPre.slots[i] : null;

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

        ChangeState(TrainState.WaitingForLoad);
        RefreshAllUI();
    }

    /// Khi tàu về: chuyển từng slot sang mode Reward với dữ liệu _pendingRewards.
    private void ApplyRewardsToSlots()
    {
        if (SlotData == null || _pendingRewards == null) return;

        for (int i = 0; i < SlotData.Length; i++)
        {
            TrainRewardItem rew = i < _pendingRewards.Length ? _pendingRewards[i] : null;

            if (rew == null)
            {
                SlotData[i].mode = TrainWagonSlotMode.Empty;
                continue;
            }

            SlotData[i].mode         = TrainWagonSlotMode.Reward;
            SlotData[i].itemId       = rew.itemId;
            SlotData[i].displayName  = rew.displayName;
            SlotData[i].icon         = rew.icon;
            SlotData[i].rewardAmount = rew.rewardAmount;
            SlotData[i].isCollected  = false;
        }
    }

    // ─── State transitions ───────────────────────────────────────

    /// Kiểm tra nếu tất cả toa đã đủ hàng → khởi hành.
    private void CheckAllLoaded()
    {
        if (SlotData == null) return;
        foreach (var slot in SlotData)
            if (slot.mode == TrainWagonSlotMode.CargoRequest && !slot.IsCargoComplete) return;

        Debug.Log("[Train] Tất cả toa đã đầy hàng → Khởi hành!");
        // Chỉ tắt tương tác (collider), KHÔNG ẩn visual — cargo image hiển thị suốt hành trình
        DisableAllSlotInteractions();
        ChangeState(TrainState.Departing);
        pathFollower?.DepartToProcess();
    }

    /// Tắt collider tất cả slot để ngăn click trong khi tàu đang chạy,
    /// nhưng giữ nguyên cargo visual trên toa.
    private void DisableAllSlotInteractions()
    {
        if (wagonSlots == null) return;
        foreach (var slot in wagonSlots)
            slot?.DisableInteraction();
    }

    // PathFollower callback: tàu tới Point_02 (cửa hầm / đích đến)
    private void OnArrivedAtPoint00()
    {
        Debug.Log("[Train] Tàu tới cửa hầm → Hiện phần thưởng ngay, bắt đầu quay về ga...");
        // Áp reward data và hiện icon ngay lập tức — user thấy hàng trên toa suốt hành trình về
        ApplyRewardsToSlots();
        ChangeState(TrainState.ReturningWithReward);
        RefreshAllUI();
        // Tắt collider để không click được trong lúc tàu đang chạy về
        DisableAllSlotInteractions();
        pathFollower?.ReturnToWait();
    }

    // PathFollower callback: tàu về Point_00 (ga tàu)
    private void OnArrivedAfterReturn()
    {
        Debug.Log("[Train] Tàu về ga → Sẵn sàng thu hoạch!");
        // Reward đã hiện từ lúc rời hầm — chỉ cần đổi state và bật lại collider
        ChangeState(TrainState.RewardReadyToCollect);
        RefreshAllUI();
    }

    /// Kiểm tra nếu tất cả slot reward đã được thu → tàu quay đầu rồi new trip.
    private void CheckAllCollected()
    {
        if (SlotData == null) return;
        foreach (var slot in SlotData)
            if (slot.mode == TrainWagonSlotMode.Reward && !slot.isCollected) return;

        Debug.Log("[Train] Thu hết phần thưởng → Tàu quay đầu tại ga, tạo chuyến mới...");
        HideAllSlots();
        // Point_00 (ga) → Point_01 (quay đầu) → Point_00 (ga)
        pathFollower?.ResetMove();
    }

    // PathFollower callback: tàu hoàn thành quay đầu → tạo chuyến mới
    private void OnResetMoveDone()
    {
        _tripIndex++;
        GenerateNewTrip();
    }

    // ─── UI helpers ──────────────────────────────────────────────

    private void ChangeState(TrainState newState)
    {
        State = newState;
        Debug.Log($"[Train] ── {newState} ──");

        // Đồng bộ Popup_train với state tàu
        if (newState == TrainState.Departing)
        {
            _tripEndTime = Time.time + tripDurationSeconds;
            processPopup?.Show(tripDurationSeconds);
        }
        else if (newState == TrainState.ReturningWithReward)
        {
            processPopup?.Show(0f);
        }
        else
        {
            processPopup?.Hide();
        }
    }

    private void RefreshAllUI()
    {
        if (wagonSlots == null || SlotData == null) return;
        for (int i = 0; i < wagonSlots.Length; i++)
        {
            if (wagonSlots[i] == null) continue;
            if (i < SlotData.Length)
                wagonSlots[i].Refresh(SlotData[i]);
            else
                wagonSlots[i].Hide();
        }
    }

    private void RefreshSlotUI(int index)
    {
        if (wagonSlots == null || index >= wagonSlots.Length || wagonSlots[index] == null) return;
        if (index < SlotData.Length)
            wagonSlots[index].Refresh(SlotData[index]);
    }

    private void HideAllSlots()
    {
        if (wagonSlots == null) return;
        foreach (var slot in wagonSlots)
            slot?.Hide();
    }

    // ─── Data helpers ────────────────────────────────────────────

    private bool IsValidSlot(int i) =>
        SlotData != null && i >= 0 && i < SlotData.Length && SlotData[i] != null;

    private TrainCargoPreset GetCargoPreset(int index)
    {
        if (cargoData != null && cargoData.presets != null && cargoData.presets.Count > 0)
            return cargoData.presets[index % cargoData.presets.Count];

        Debug.LogWarning("[Train] Chưa gán TrainCargoData — dùng fallback.");
        return new TrainCargoPreset
        {
            slots = new TrainCargoRequirement[]
            {
                new TrainCargoRequirement { itemId = "lua",   displayName = "Lúa",   requiredAmount = 4 },
                new TrainCargoRequirement { itemId = "bap",   displayName = "Bắp",   requiredAmount = 3 },
                new TrainCargoRequirement { itemId = "trung", displayName = "Trứng", requiredAmount = 2 },
                new TrainCargoRequirement { itemId = "nam",   displayName = "Nấm",   requiredAmount = 2 }
            }
        };
    }

    private TrainRewardPreset GetRewardPreset(int index)
    {
        if (rewardData != null && rewardData.presets != null && rewardData.presets.Count > 0)
            return rewardData.presets[index % rewardData.presets.Count];

        Debug.LogWarning("[Train] Chưa gán TrainRewardData — dùng fallback.");
        return new TrainRewardPreset
        {
            slots = new TrainRewardItem[]
            {
                new TrainRewardItem { itemId = "da",   displayName = "Đá",   rewardAmount = 2 },
                new TrainRewardItem { itemId = "gach", displayName = "Gạch", rewardAmount = 1 },
                new TrainRewardItem { itemId = "dinh", displayName = "Đinh", rewardAmount = 3 },
                new TrainRewardItem { itemId = "kim",  displayName = "Kim",  rewardAmount = 1 }
            }
        };
    }
}
