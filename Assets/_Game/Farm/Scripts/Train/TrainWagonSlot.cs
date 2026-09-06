using TMPro;
using UnityEngine;

// ——— Slot mode enum ———————————————————————————————————————————————

public enum TrainWagonSlotMode
{
    Empty,        // Toa trống — không hiện gì
    CargoRequest, // Chờ nạp hàng — hiện icon + currentAmount/requiredAmount
    Reward        // Chờ thu hoạch — hiện icon + x(amount)
}

// ——— Runtime slot data —————————————————————————————————————————————

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

    [Header("Icon Fit — icon hàng nằm gọn trên toa (yêu cầu Sếp 2026-08-26)")]
    [Tooltip("Bề rộng icon tối đa = tỉ lệ này x bề rộng vùng click toa (BoxCollider2D). Mọi loại hàng (thịt, lúa, đá, kính...) đều bị co về cùng 1 cỡ. Đặt 0 = giữ size gốc như cũ.")]
    [SerializeField] private float iconFitRatio = 0.45f;

    // ——— Runtime ——————————————————————————————————————————————————————
    private TrainWagonSlotData _data;
    private BoxCollider2D      _col;
    private Vector3            _iconBaseScale = Vector3.one;

    // ——————————————————————————————————————————————————————————————————

    void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        if (iconSprite != null) _iconBaseScale = iconSprite.transform.localScale;
        // Ẩn cargo icon mặc định — chỉ hiện sau khi chất hàng lần đầu
        if (iconSprite != null) iconSprite.enabled = false;
    }

    // ——— Public API (gọi từ TrainManager) ————————————————————————————

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

    // ─── Display helpers ────────────────────────────────────────────────────

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

        FitIconToWagon();
        SetLabel($"{data.currentAmount}/{data.requiredAmount}");

        // Toa đầy → tắt collider (không cho click thêm)
        _col.enabled = !data.IsCargoComplete;
    }

    private void ShowReward(TrainWagonSlotData data)
    {
        if (emptyRoot != null) emptyRoot.SetActive(false);
        SetIcon(data.icon);
        FitIconToWagon();
        SetLabel($"x{data.rewardAmount}");

        _col.enabled = true;
    }

    /// <summary>
    /// Chuẩn hoá icon về CÙNG 1 cỡ nhỏ gọn trên toa — mọi loại hàng bằng nhau.
    /// </summary>
    private void FitIconToWagon()
    {
        if (iconSprite == null || iconSprite.sprite == null || _col == null) return;
        if (iconFitRatio <= 0f) return; // 0 = giữ hành vi cũ

        iconSprite.transform.localScale = _iconBaseScale;

        float target = _col.size.x * Mathf.Abs(transform.lossyScale.x) * iconFitRatio;

        Vector2 sb = iconSprite.sprite.bounds.size;
        float lossy = Mathf.Abs(iconSprite.transform.lossyScale.x);
        float current = Mathf.Max(sb.x, sb.y) * lossy;

        if (current <= 0.0001f || target <= 0.0001f) return;

        iconSprite.transform.localScale = _iconBaseScale * (target / current);
    }

    private void SetIcon(Sprite sprite)
    {
        if (iconSprite == null) return;
        if (sprite != null)
        {
            iconSprite.sprite  = sprite;
            iconSprite.enabled = true;
        }
    }

    private void SetLabel(string text)
    {
        if (txtLabel != null) txtLabel.text = text;
    }

    void Update()
    {
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (TrainManager.Instance == null) return;
        if (Camera.main == null) return;

        bool clicked = InputBridge.IsPointerDownThisFrame
                    || (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                    || (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                    || Input.GetMouseButtonDown(0);
        if (!clicked) return;

        if (FarmInputLock.ConTroTrenUiThat()) return;
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen()) return;

        Vector2 screenPos = InputBridge.PointerPosition;
        if (screenPos == Vector2.zero)
        {
            if (UnityEngine.InputSystem.Mouse.current != null) screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            else screenPos = (Vector2)Input.mousePosition;
        }
        Vector3 world3 = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));
        Vector2 worldPos = new Vector2(world3.x, world3.y);

        if (_col == null) _col = GetComponent<BoxCollider2D>();
        if (_col != null && _col.enabled && _col.OverlapPoint(worldPos))
        {
            TrainManager.Instance.OnWagonSlotClicked(this);
        }
    }

    // Unity gọi OnMouseDown khi collider của chính GO này được click (legacy fallback).
    private void OnMouseDown()
    {
        // Chặn click xuyên khi đang ở Bếp (scene phụ load additive) / đang mở popup.
        if (FarmInputLock.BlockWorldClickBySceneOrPopup) return;
        if (!enabled || !gameObject.activeInHierarchy) return;
        if (TrainManager.Instance == null) return;

        // Không xử lý khi đang có popup mở
        if (PopupManager.Instance != null && PopupManager.Instance.IsAnyPopupOpen())
            return;

        TrainManager.Instance.OnWagonSlotClicked(this);
    }
}
