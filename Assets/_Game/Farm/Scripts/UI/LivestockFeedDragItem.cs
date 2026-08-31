using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LivestockFeedDragItem : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image imgIcon;
    [SerializeField] private TMP_Text txtName;
    [SerializeField] private TMP_Text txtStock;

    private string _foodItemId;
    private Sprite _foodSprite;
    private string _displayName;
    private PenMiniPanelUI _targetPen;
    private CanvasGroup _canvasGroup;
    private bool _isDragging;

    public string FoodItemId => _foodItemId;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string foodItemId, Sprite foodSprite, string displayName, PenMiniPanelUI targetPen)
    {
        _foodItemId   = foodItemId;
        _foodSprite   = foodSprite;
        _displayName  = displayName;
        _targetPen    = targetPen;

        if (imgIcon != null)
        {
            imgIcon.sprite = foodSprite;
            imgIcon.preserveAspect = true;
        }

        if (txtName != null)
        {
            txtName.text = !string.IsNullOrEmpty(displayName) ? displayName : foodItemId;
        }

        RefreshStock();
    }

    public void RefreshStock()
    {
        if (string.IsNullOrEmpty(_foodItemId)) return;

        int stock = 0;
        if (FarmInventoryManager.Instance != null)
        {
            stock = FarmInventoryManager.Instance.GetItemCount(_foodItemId);
        }

        if (txtStock != null)
        {
            txtStock.text = stock.ToString();
            txtStock.color = stock > 0 ? Color.white : new Color(1f, 0.4f, 0.4f, 1f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Nhấn xuống
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(_foodItemId)) return;

        int stock = FarmInventoryManager.Instance != null ? FarmInventoryManager.Instance.GetItemCount(_foodItemId) : 0;
        if (stock <= 0)
        {
            FarmUIManager.Instance?.ShowHint($"Chưa có {_displayName} trong kho. Hãy chế biến tại Máy Xay Thức Ăn hoặc thu hoạch nông sản!");
            return;
        }

        _isDragging = true;
        FarmInputLock.IsDraggingSeed = true;

        if (_canvasGroup != null) _canvasGroup.alpha = 0.5f;

        if (FarmUIManager.Instance != null && _foodSprite != null)
        {
            FarmUIManager.Instance.ShowFloatingDragIcon(_foodSprite);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Floating icon tự update theo chuột / touch
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;
        FarmInputLock.IsDraggingSeed = false;

        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        if (FarmUIManager.Instance != null)
        {
            FarmUIManager.Instance.HideFloatingDragIcon();
        }

        TryDropOnPen(eventData.position);
    }

    private void TryDropOnPen(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
        worldPos.z = 0f;

        // 1. Thử kiểm tra targetPen chỉ định trước
        if (_targetPen != null)
        {
            float dist = Vector2.Distance(worldPos, _targetPen.transform.position);
            // Chuồng có bán kính khoảng 250-350 unit trên map lớn (hoặc 2.5-4 unit)
            float checkDist = 350f;
            var box = _targetPen.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                checkDist = Mathf.Max(box.size.x, box.size.y) * 1.5f;
            }

            if (dist <= checkDist || (box != null && box.OverlapPoint(worldPos)))
            {
                if (_targetPen.TryFeed(_foodItemId, worldPos))
                {
                    FarmUIManager.Instance?.HideLivestockFeedPopup();
                    return;
                }
            }
        }

        // 2. Raycast kiểm tra các chuồng khác dưới vị trí thả
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);
        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var pen = hits[i].GetComponent<PenMiniPanelUI>() ?? hits[i].GetComponentInParent<PenMiniPanelUI>();
                if (pen == null)
                {
                    var dropTarget = hits[i].GetComponent<PenDropTarget>() ?? hits[i].GetComponentInParent<PenDropTarget>();
                    if (dropTarget != null) pen = dropTarget.GetComponentInParent<PenMiniPanelUI>();
                }

                if (pen != null && pen.CurrentState == PenMiniPanelUI.PenState.Idle)
                {
                    if (pen.TryFeed(_foodItemId, worldPos))
                    {
                        FarmUIManager.Instance?.HideLivestockFeedPopup();
                        return;
                    }
                }
            }
        }

        // Không trúng chuồng nào thì đóng popup nhẹ nhàng
        FarmUIManager.Instance?.HideLivestockFeedPopup();
    }
}
