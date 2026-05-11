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

    // Unity gọi OnMouseDown khi collider của chính GO này được click.
    // Không cần tự kiểm tra raycast / OverlapPoint nữa.
    private void OnMouseDown()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (TrainManager.Instance == null) return;

        // Không xử lý khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        Debug.Log($"[TrainSlot {slotIndex}] OnMouseDown — state={TrainManager.Instance.State}");
        TrainManager.Instance.OnWagonSlotClicked(this);
    }
}
