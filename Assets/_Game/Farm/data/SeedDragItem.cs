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

    // Threshold pixel Ä‘á»ƒ phÃ¢n biá»‡t scroll ngang vs kÃ©o trá»“ng
private const float kDragThreshold = 25f;

    public string   CropId   => cropData != null ? cropData.cropId   : string.Empty;
    public CropData CropData => cropData;
    private string  CropLogName => cropData != null
        ? (!string.IsNullOrEmpty(cropData.displayName) ? cropData.displayName : cropData.cropId)
        : "NULL";

    // â”€â”€ VÃ²ng Ä‘á» i Unity â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void Awake()
    {
        rectTransform    = GetComponent<RectTransform>();
        canvasGroup      = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        parentScrollRect = GetComponentInParent<ScrollRect>();

        // Override iconImage vá»  child "Icon_item" â€” trÃ¡nh prefab gÃ¡n sai Image áº©n
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

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void SetData(CropData data)
    {
        cropData = data;

        if (iconImage != null && data != null)
        {
            iconImage.sprite = data.icon;
        }

        if (txtName != null && data != null)
            txtName.text = data.displayName;

        RefreshStockDisplay();

    }

    // â”€â”€ Stock Display â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void RefreshStockDisplay()
    {
        if (cropData == null) return;

        int stock = GetCurrentStock();

        if (txtSoLuong != null)
        {
            txtSoLuong.text  = "x" + stock;
            txtSoLuong.color = stock > 0 ? Color.white : Color.red;
        }

        // KhÃ´ng thay alpha trong lÃºc Ä‘ang kÃ©o Plant (alpha Ä‘ang = 0 Ä‘á»ƒ áº©n item)
        if (dragMode != DragMode.Plant && canvasGroup != null)
            canvasGroup.alpha = stock > 0 ? 1f : 0.4f;
    }

    // â”€â”€ Drag Handlers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos        = eventData.position;
        dragMode              = DragMode.None;
        scrollBeginForwarded  = false;
        string cropName = CropLogName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ChÆ°a xÃ¡c Ä‘á»‹nh hÆ°á»›ng kÃ©o â€” chá» OnDrag tÃ­nh delta
        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
        string cropName = CropLogName;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - pointerDownPos;
        string cropName = CropLogName;

        if (dragMode == DragMode.None)
        {
            float ax = Mathf.Abs(delta.x);
            float ay = Mathf.Abs(delta.y);

            if (ax >= kDragThreshold && ax > ay)
            {
                // KÃ©o NGANG â†’ scroll danh sÃ¡ch
                dragMode = DragMode.Scroll;

                if (parentScrollRect != null && !scrollBeginForwarded)
                {
                    scrollBeginForwarded = true;
                    parentScrollRect.OnBeginDrag(eventData);
                }
            }
            else if (ay >= kDragThreshold && ay > ax)
            {
                // KÃ©o Dá»ŒC/XUá»NG â†’ báº¯t Ä‘áº§u trá»“ng
                dragMode = DragMode.Plant;
                BeginPlantMode();
            }
        }

        if (dragMode == DragMode.Scroll && parentScrollRect != null)
        {
            parentScrollRect.OnDrag(eventData);
        }
        // DragMode.Plant: PlantDragController.Update() tá»± sweep theo PointerWorldPosition
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        string cropName = CropLogName;

        switch (dragMode)
        {
            case DragMode.Scroll:
                parentScrollRect?.OnEndDrag(eventData);
                break;

            case DragMode.Plant:
                // Má»Ÿ khÃ³a map pan khi tháº£ seed
                FarmInputLock.IsDraggingSeed = false;

                // Restore alpha/raycast trÆ°á»›c khi EndPlantDrag (trÃ¡nh RefreshStockDisplay nháº§m)
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                }
                PlantDragController.Instance?.EndPlantDrag();
                RefreshStockDisplay();
                break;

            case DragMode.None:
                // Drag khÃ´ng Ä‘á»§ threshold (tap hoáº·c micro-drag) â€” khÃ´ng lÃ m gÃ¬
                break;
        }

        dragMode             = DragMode.None;
        scrollBeginForwarded = false;
    }

    // â”€â”€ Plant mode start â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void BeginPlantMode()
    {
        string cropName = CropLogName;

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

        // KhÃ³a map pan khi báº¯t Ä‘áº§u kÃ©o seed â€” Scroll mode KHÃ”NG set cá» nÃ y
        FarmInputLock.IsDraggingSeed = true;

        // áº¨n item gá»‘c â€” popup váº«n active Ä‘á»ƒ OnDrag/OnEndDrag tiáº¿p tá»¥c nháº­n event
        if (canvasGroup != null)
        {
            canvasGroup.alpha          = 0f;
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
