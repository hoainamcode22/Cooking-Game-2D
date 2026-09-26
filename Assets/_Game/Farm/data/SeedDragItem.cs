using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class SeedDragItem : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // â”€â”€ Drag mode â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

    // Threshold pixel để phân biệt scroll ngang vs kéo trồng (hạ xuống 10f để nhạy bén mượt mà)
    private const float kDragThreshold = 10f;

    public string   CropId   => cropData != null ? cropData.cropId   : string.Empty;
    public CropData CropData => cropData;
    private string  CropLogName => cropData != null
        ? (!string.IsNullOrEmpty(cropData.displayName) ? cropData.displayName : cropData.cropId)
        : "NULL";

    // ── Vòng đời Unity ──────────────────────────────────────────────────

    private void Awake()
    {
        rectTransform    = GetComponent<RectTransform>();
        canvasGroup      = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        parentScrollRect = GetComponentInParent<ScrollRect>();

        // Override iconImage về child "Icon_item" — tránh prefab gán sai Image ẩn
        Transform iconChild = transform.Find("Icon_item");
        if (iconChild != null)
        {
            if (iconChild.TryGetComponent(out Image img))
                iconImage = img;
        }
    }

    private void OnEnable()
    {
        dragMode = DragMode.None;
        scrollBeginForwarded = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (WarehouseManager.Instance != null)
        {
            WarehouseManager.Instance.OnWarehouseChanged -= RefreshStockDisplay;
            WarehouseManager.Instance.OnWarehouseChanged += RefreshStockDisplay;
        }

        if (FarmInventoryManager.Instance != null)
        {
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshStockDisplay;
            FarmInventoryManager.Instance.OnInventoryChanged += RefreshStockDisplay;
        }

        RefreshStockDisplay();
    }

    private void OnDisable()
    {
        dragMode = DragMode.None;
        scrollBeginForwarded = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (WarehouseManager.Instance != null)
            WarehouseManager.Instance.OnWarehouseChanged -= RefreshStockDisplay;

        if (FarmInventoryManager.Instance != null)
            FarmInventoryManager.Instance.OnInventoryChanged -= RefreshStockDisplay;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void SetData(CropData data)
    {
        cropData = data;

        if (iconImage != null && data != null)
        {
            iconImage.sprite = data.FinalStageSprite != null ? data.FinalStageSprite : data.icon;
            iconImage.preserveAspect = true;
            RectTransform rt = iconImage.rectTransform;
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(76f, 76f);
            }
        }

        if (txtName != null && data != null)
            txtName.text = Loc.T(data.displayName);

        RefreshStockDisplay();
    }

    // ── Stock Display ────────────────────────────────────────────────────

    public void RefreshStockDisplay()
    {
        if (cropData == null) return;

        int stock = GetCurrentStock();

        if (txtSoLuong != null)
        {
            txtSoLuong.text  = "x" + stock;
            txtSoLuong.color = stock > 0 ? SeedPanelSkin.MauSoCon : SeedPanelSkin.MauSoHet;   // [2026-09-25] doc ro tren the be
        }

        // Không thay alpha trong lúc đang kéo Plant
        if (dragMode != DragMode.Plant && canvasGroup != null)
            canvasGroup.alpha = stock > 0 ? 1f : 0.8f;   // [2026-09-25] het hat van sang ro, chi so luong do
    }

    // ── Drag Handlers ────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos        = eventData.position;
        dragMode              = DragMode.None;
        scrollBeginForwarded  = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - pointerDownPos;

        if (dragMode == DragMode.None)
        {
            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);

            // Ưu tiên cao: ngón tay di chuyển hướng lên về phía ruộng (delta.y > 8px)
            // hoặc kéo có hướng dọc rõ rệt -> vào ngay chế độ trồng Plant
            if (delta.y >= 8f || (ay >= kDragThreshold && ay >= ax * 0.7f))
            {
                dragMode = DragMode.Plant;
                BeginPlantMode();
            }
            // Chỉ khi vuốt thuần ngang và không hướng lên trên mới scroll khay
            else if (ax >= kDragThreshold && ax > ay * 1.3f && delta.y < 8f)
            {
                dragMode = DragMode.Scroll;

                if (parentScrollRect != null && !scrollBeginForwarded)
                {
                    scrollBeginForwarded = true;
                    parentScrollRect.OnBeginDrag(eventData);
                }
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
        switch (dragMode)
        {
            case DragMode.Scroll:
                parentScrollRect?.OnEndDrag(eventData);
                break;

            case DragMode.Plant:
                FarmInputLock.IsDraggingSeed = false;

                // Restore alpha/raycast trước khi EndPlantDrag
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                }
                PlantDragController.Instance?.EndPlantDrag();
                RefreshStockDisplay();
                break;

            case DragMode.None:
                break;
        }

        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
    }

    // ── Plant mode start ──────────────────────────────────────────────────

    private void BeginPlantMode()
    {
        if (cropData == null)
        {
            dragMode = DragMode.None;
            return;
        }

        if (GetCurrentStock() <= 0)
        {
            dragMode = DragMode.None;
            return;
        }

        // Khóa map pan khi bắt đầu kéo seed
        FarmInputLock.IsDraggingSeed = true;

        // Giữ UI: Thẻ trong khay mờ nhẹ (0.55f) biểu thị đang nhấc, KHÔNG ẩn mất tăm (alpha = 0f)
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0.55f;
            canvasGroup.blocksRaycasts = false;
        }

        PlantDragController.Instance?.StartPlantDrag(cropData);
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private int GetCurrentStock()
    {
        if (cropData == null)
            return 0;

        string s1 = cropData.seedItemId;
        string s2 = cropData.itemID;
        string s3 = cropData.cropId;

        if (FarmInventoryManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(s1))
            {
                int c = FarmInventoryManager.Instance.GetAmount(s1);
                if (c > 0) return c;
            }
            if (!string.IsNullOrEmpty(s2) && s2 != s1)
            {
                int c = FarmInventoryManager.Instance.GetAmount(s2);
                if (c > 0) return c;
            }
            if (!string.IsNullOrEmpty(s3) && s3 != s1 && s3 != s2)
            {
                int c = FarmInventoryManager.Instance.GetAmount(s3);
                if (c > 0) return c;
                c = FarmInventoryManager.Instance.GetAmount("seed_" + s3);
                if (c > 0) return c;
            }
        }

        if (WarehouseManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(s1))
            {
                int c = WarehouseManager.Instance.GetAmount(s1);
                if (c > 0) return c;
            }
            if (!string.IsNullOrEmpty(s2) && s2 != s1)
            {
                int c = WarehouseManager.Instance.GetAmount(s2);
                if (c > 0) return c;
            }
        }

        return 0;
    }
}
