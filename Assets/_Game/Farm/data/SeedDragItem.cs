using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SeedDragItem : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ── Drag mode ─────────────────────────────────────────────────────────────
    private enum DragMode { None, Scroll, Plant }

    [Header("Seed Data")]
    [SerializeField] private CropData cropData;

    [Header("UI Refs")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtSoLuong;

    private RectTransform rectTransform;
    private CanvasGroup   canvasGroup;
    private ScrollRect    parentScrollRect;

    private Vector2  pointerDownPos;
    private DragMode dragMode             = DragMode.None;
    private bool     scrollBeginForwarded = false;

    // Threshold pixel để phân biệt scroll ngang vs kéo trồng
private const float kDragThreshold = 25f;

    public string   CropId   => cropData != null ? cropData.cropId   : string.Empty;
    public CropData CropData => cropData;
    private string  CropLogName => cropData != null
        ? (!string.IsNullOrEmpty(cropData.displayName) ? cropData.displayName : cropData.cropId)
        : "NULL";

    // ── Vòng đời Unity ───────────────────────────────────────────────────────

    private void Awake()
    {
        rectTransform    = GetComponent<RectTransform>();
        canvasGroup      = GetComponent<CanvasGroup>();
        parentScrollRect = GetComponentInParent<ScrollRect>();

        // Override iconImage về child "Icon_item" — tránh prefab gán sai Image ẩn
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

        RefreshStockDisplay();

        Debug.Log($"[SeedDragItem] SetData OK: {data.cropId} | seedItemId={data.seedItemId}");
    }

    // ── Stock Display ─────────────────────────────────────────────────────────

    public void RefreshStockDisplay()
    {
        if (cropData == null) return;

        int stock = GetCurrentStock();

        if (txtSoLuong != null)
        {
            txtSoLuong.text  = "x" + stock;
            txtSoLuong.color = stock > 0 ? Color.white : Color.red;
        }

        // Không thay alpha trong lúc đang kéo Plant (alpha đang = 0 để ẩn item)
        if (dragMode != DragMode.Plant && canvasGroup != null)
            canvasGroup.alpha = stock > 0 ? 1f : 0.4f;
    }

    // ── Drag Handlers ─────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos        = eventData.position;
        dragMode              = DragMode.None;
        scrollBeginForwarded  = false;
        string cropName = CropLogName;
        Debug.Log($"[SeedDragItem] PointerDown {name} crop={cropName} pos={eventData.position}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Chưa xác định hướng kéo — chờ OnDrag tính delta
        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
        string cropName = CropLogName;
        Debug.Log($"[SeedDragItem] BeginDrag ENTER {name} crop={cropName}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - pointerDownPos;
        string cropName = CropLogName;
        Debug.Log($"[SeedDragItem] Drag {name} crop={cropName} delta={delta} mode={dragMode}");

        if (dragMode == DragMode.None)
        {
            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);

            if (ax >= kDragThreshold && ax > ay)
            {
                // Kéo NGANG → scroll danh sách
                dragMode = DragMode.Scroll;
                Debug.Log($"[SeedDragItem] BeginScrollMode {name} crop={cropName}");

                if (parentScrollRect != null && !scrollBeginForwarded)
                {
                    scrollBeginForwarded = true;
                    parentScrollRect.OnBeginDrag(eventData);
                }
            }
            else if (ay >= kDragThreshold && ay > ax)
            {
                // Kéo DỌC/XUỐNG → bắt đầu trồng
                dragMode = DragMode.Plant;
                Debug.Log($"[SeedDragItem] Mode=Plant {name} crop={cropName}");
                BeginPlantMode();
            }
        }

        if (dragMode == DragMode.Scroll && parentScrollRect != null)
        {
            parentScrollRect.OnDrag(eventData);
        }
        // DragMode.Plant: PlantDragController.Update() tự sweep theo PointerWorldPosition
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        string cropName = CropLogName;
        Debug.Log($"[SeedDragItem] EndDrag {name} crop={cropName} mode={dragMode}");

        switch (dragMode)
        {
            case DragMode.Scroll:
                parentScrollRect?.OnEndDrag(eventData);
                break;

            case DragMode.Plant:
                // Mở khóa map pan khi thả seed
                FarmInputLock.IsDraggingSeed = false;

                // Restore alpha/raycast trước khi EndPlantDrag (tránh RefreshStockDisplay nhầm)
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                }
                PlantDragController.Instance?.EndPlantDrag();
                RefreshStockDisplay();
                break;

            case DragMode.None:
                // Drag không đủ threshold (tap hoặc micro-drag) — không làm gì
                break;
        }

        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
    }

    // ── Plant mode start ──────────────────────────────────────────────────────

    private void BeginPlantMode()
    {
        string cropName = CropLogName;
        Debug.Log($"[SeedDragItem] BeginPlantMode {name} crop={cropName}");

        if (cropData == null)
        {
            dragMode = DragMode.None;
            Debug.LogWarning($"[SeedDragItem] BeginPlantMode: cropData null crop={cropName}");
            return;
        }

        if (GetCurrentStock() <= 0)
        {
            dragMode = DragMode.None;
            Debug.Log($"[SeedDragItem] Không đủ hạt giống '{cropData.displayName}' để trồng. crop={cropName}");
            return;
        }

        // Khóa map pan khi bắt đầu kéo seed — Scroll mode KHÔNG set cờ này
        FarmInputLock.IsDraggingSeed = true;

        // Ẩn item gốc — popup vẫn active để OnDrag/OnEndDrag tiếp tục nhận event
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        PlantDragController.Instance?.StartPlantDrag(cropData);
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
