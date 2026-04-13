using TMPro;
using UnityEngine;

// ─── Slot mode enum ───────────────────────────────────────────────────────────

public enum TrainWagonSlotMode
{
    Empty,        // Toa trống — không hiện gì
    CargoRequest, // Chờ nạp hàng — hiện icon + currentAmount/requiredAmount
    Reward        // Chờ thu hoạch — hiện icon + x(amount)
}

// ─── Runtime slot data ────────────────────────────────────────────────────────

/// <summary>
/// Runtime data cho 1 toa trong chuyến hiện tại.
/// Dùng chung cho cả chế độ nạp hàng (CargoRequest) và chế độ thu reward (Reward).
/// </summary>
[System.Serializable]
public class TrainWagonSlotData
{
    [Header("Shared")]
    public string itemId;
    public string displayName;
    public Sprite icon;
    public TrainWagonSlotMode mode;
    public bool isCollected;

    [Header("Cargo Request")]
    public int currentAmount;
    public int requiredAmount;

    [Header("Reward")]
    public int rewardAmount;

    public bool IsCargoComplete =>
        mode == TrainWagonSlotMode.CargoRequest && currentAmount >= requiredAmount;
}


[RequireComponent(typeof(BoxCollider2D))]
public class TrainWagonSlot : MonoBehaviour
{
    [Header("Visual References")]
    [Tooltip("SpriteRenderer world-space hiện icon vật phẩm")]
    [SerializeField] private SpriteRenderer iconSprite;
    [SerializeField] private TMP_Text       txtLabel;

    [Header("Optional — hiện khi toa trống")]
    [SerializeField] private GameObject emptyRoot;

    [Header("Config")]
    [Tooltip("0 = Wagon_01 / WorldSlot_01, 1 = Wagon_02, …")]
    [SerializeField] public int slotIndex = 0;

    // ─── Runtime ──────────────────────────────────────────────────
    private TrainWagonSlotData _data;
    private BoxCollider2D      _col;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        // Ẩn cargo icon mặc định — chỉ hiện sau khi chất hàng lần đầu
        if (iconSprite != null) iconSprite.enabled = false;
    }

    // ─── Public API (gọi từ TrainManager) ────────────────────────

    /// <summary>Refresh visual từ slot data mới nhất và bật collider.</summary>
    public void Refresh(TrainWagonSlotData data)
    {
        _data = data;
        gameObject.SetActive(true);

        switch (data.mode)
        {
            case TrainWagonSlotMode.Empty:
                ShowEmpty();
                break;

            case TrainWagonSlotMode.CargoRequest:
                ShowCargo(data);
                break;

            case TrainWagonSlotMode.Reward:
                if (data.isCollected) ShowEmpty();
                else ShowReward(data);
                break;
        }
    }

    /// <summary>Ẩn slot hoàn toàn và vô hiệu hoá collider (khi tàu đang chạy).</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Chỉ vô hiệu collider (ngăn click), GIỮ NGUYÊN visual.
    /// Dùng khi tàu khởi hành — cargo image vẫn hiển thị suốt hành trình.
    /// </summary>
    public void DisableInteraction()
    {
        if (_col != null) _col.enabled = false;
    }

    /// <summary>World-space position của slot — dùng làm điểm spawn FX.</summary>
    public Vector3 GetWorldPosition() => transform.position;

    // ─── Display helpers ──────────────────────────────────────────

    private void ShowEmpty()
    {
        SetIcon(null);
        if (iconSprite != null) iconSprite.enabled = false;
        SetLabel("");
        if (emptyRoot != null) emptyRoot.SetActive(true);
        _col.enabled = false; // toa trống không thể click
    }

    private void ShowCargo(TrainWagonSlotData data)
    {
        bool hasItems = data.currentAmount > 0;

        // emptyRoot: hiện khi chưa có hàng, ẩn khi đã có ít nhất 1 item
        if (emptyRoot != null) emptyRoot.SetActive(!hasItems);

        // iconSprite: ẩn khi currentAmount == 0, hiện ngay khi currentAmount >= 1
        if (iconSprite != null)
        {
            if (hasItems && data.icon != null)
            {
                iconSprite.sprite  = data.icon;
                iconSprite.enabled = true;
            }
            else
            {
                iconSprite.enabled = false;
            }
        }

        SetLabel($"{data.currentAmount}/{data.requiredAmount}");

        // Toa đầy → tắt collider (không cho click thêm)
        _col.enabled = !data.IsCargoComplete;
    }

    private void ShowReward(TrainWagonSlotData data)
    {
        if (emptyRoot != null) emptyRoot.SetActive(false);
        SetIcon(data.icon);
        SetLabel($"x{data.rewardAmount}");

        _col.enabled = true;
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconSprite == null) return;
        if (sprite != null)
        {
            iconSprite.sprite  = sprite;
            iconSprite.enabled = true;
        }
        // Không ẩn icon khi sprite null — giữ nguyên sprite cũ
    }

    private void SetLabel(string text)
    {
        if (txtLabel != null) txtLabel.text = text;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        // Đặt Z = nearClipPlane để ScreenToWorldPoint cho ra đúng world position
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z       = Camera.main.nearClipPlane;
        Vector2 worldPos    = Camera.main.ScreenToWorldPoint(mouseScreen);

        // Log mỗi click khi slot đang active — để debug nhanh
        bool colOk = _col != null && _col.enabled;
        bool hit   = colOk && _col.OverlapPoint(worldPos);
        string modeStr  = _data != null ? _data.mode.ToString() : "null";
        string stateStr = TrainManager.Instance != null ? TrainManager.Instance.State.ToString() : "null";
        Debug.Log($"[TrainSlot {slotIndex}] click world={worldPos:F0} | " +
                  $"col={colOk} size={(_col != null ? _col.size : Vector2.zero):F1} " +
                  $"hit={hit} | mode={modeStr} state={stateStr}");

        if (!hit) return;
        HandleClick();
    }

    private void HandleClick()
    {
        Debug.Log($"[TrainSlot {slotIndex}] HandleClick — State={TrainManager.Instance?.State} data={_data?.mode}");
        if (TrainManager.Instance == null || _data == null) return;

        switch (TrainManager.Instance.State)
        {
            case TrainState.WaitingForLoad:
                if (_data.mode == TrainWagonSlotMode.CargoRequest && !_data.IsCargoComplete)
                    TrainManager.Instance.OnCargoSlotClicked(slotIndex);
                break;

            case TrainState.RewardReadyToCollect:
                if (_data.mode == TrainWagonSlotMode.Reward && !_data.isCollected)
                    TrainManager.Instance.CollectReward(slotIndex);
                break;

            case TrainState.Departing:
                Debug.Log($"[TrainSlot {slotIndex}] Bỏ qua click — tàu đang khởi hành.");
                break;

            case TrainState.ReturningWithReward:
                Debug.Log($"[TrainSlot {slotIndex}] Bỏ qua click — tàu đang trở về.");
                break;
        }
    }
}
