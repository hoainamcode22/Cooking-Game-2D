using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SeedDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Seed Data")]
    [SerializeField] private CropData cropData;

    [Header("UI Refs")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtSoLuong;
    [SerializeField] private Canvas   rootCanvas;

    private RectTransform rectTransform;
    private CanvasGroup   canvasGroup;
    private Vector2       startAnchoredPosition;
    private bool          canDrag;

    public string   CropId   => cropData != null ? cropData.cropId   : string.Empty;
    public CropData CropData => cropData;

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        // BUG FIX: prefab gán iconImage sai vào child "Image" (alpha=0, invisible).
        // Child hiển thị thật là "Icon_item" — override về đây để SetData ghi đúng chỗ.
        Transform iconChild = transform.Find("Icon_item");
        if (iconChild != null)
        {
            if (iconChild.TryGetComponent(out Image img))
                iconImage = img;
            else
                Debug.LogWarning($"[SeedDragItem] '{name}': Icon_item không có Image component!");
        }
        else
        {
            Debug.LogWarning($"[SeedDragItem] '{name}': Không tìm thấy child 'Icon_item' — giữ iconImage từ inspector.");
        }
    }

    private void OnEnable()
    {
        // Lắng nghe kho thay đổi để refresh số lượng hiển thị real-time
        if (WarehouseManager.Instance != null)
            WarehouseManager.Instance.OnWarehouseChanged += RefreshStockDisplay;

        RefreshStockDisplay();
    }

    private void OnDisable()
    {
        if (WarehouseManager.Instance != null)
            WarehouseManager.Instance.OnWarehouseChanged -= RefreshStockDisplay;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetData(CropData data)
    {
        cropData = data;

        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            if (data.icon == null)
                Debug.LogWarning($"[SeedDragItem] CropData '{data.cropId}' thiếu icon sprite.");
        }

        if (txtName != null)
            txtName.text = data.displayName;

        // Refresh đọc số lượng thực từ Warehouse (không còn dùng harvestAmount)
        RefreshStockDisplay();

        Debug.Log($"[SeedDragItem] SetData OK: {data.cropId} | seedItemId={data.seedItemId}");
    }

    // ── Stock Display ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc số hạt giống thực tế từ WarehouseManager và cập nhật hiển thị.
    /// Làm mờ item nếu hết hàng.
    /// </summary>
    public void RefreshStockDisplay()
    {
        if (cropData == null) return;

        int stock = GetCurrentStock();

        if (txtSoLuong != null)
        {
            txtSoLuong.text  = "x" + stock;
            // Đổi màu đỏ khi hết hàng để người chơi dễ nhận biết
            txtSoLuong.color = stock > 0 ? Color.white : Color.red;
        }

        // Làm mờ toàn bộ item khi hết hàng — alpha 0.4 thay vì ẩn hẳn
        if (canvasGroup != null)
            canvasGroup.alpha = stock > 0 ? 1f : 0.4f;
    }

    // ── Drag Handlers ─────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Không cho kéo nếu chưa gán data
        if (cropData == null)
        {
            canDrag = false;
            Debug.LogWarning($"[SeedDragItem] {name}: chưa gán CropData");
            return;
        }

        // Không cho kéo nếu hết hạt giống trong kho
        if (GetCurrentStock() <= 0)
        {
            canDrag = false;
            Debug.Log($"[SeedDragItem] Không đủ hạt giống '{cropData.displayName}' trong kho để trồng.");
            return;
        }

        canDrag = true;
        startAnchoredPosition = rectTransform.anchoredPosition;

        // Ẩn item gốc trong lúc kéo — popup vẫn active để OnDrag/OnEndDrag tiếp tục nhận
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;

        PlantDragController.Instance?.StartPlantDrag(cropData);
    }

    // Giữ OnDrag để Unity EventSystem tiếp tục tracking drag pointer.
    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || rootCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
    }

    // Khi thả — restore item, thông báo PlantDragController kết thúc.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag) return;

        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = startAnchoredPosition;

        PlantDragController.Instance?.EndPlantDrag();

        // Refresh lại sau khi thả (số lượng đã bị trừ trong lúc drag)
        RefreshStockDisplay();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int GetCurrentStock()
    {
        if (cropData == null || string.IsNullOrEmpty(cropData.seedItemId))
            return 0;

        if (WarehouseManager.Instance == null)
            return 0;

        return WarehouseManager.Instance.GetAmount(cropData.seedItemId);
    }
}
