using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gắn vào icon rổ thu hoạch trong PenMiniPanel (World Space Canvas).
/// Kéo thả vào collider chuồng → gọi PenDropTarget.ReceiveBasketDrop().
/// </summary>
public class PenBasketDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Icon rổ")]
    [SerializeField] private Image basketImage;

    private Canvas         rootCanvas;
    private RectTransform  rectTransform;
    private Vector2        originalAnchoredPos;
    private CanvasGroup    canvasGroup;
    private GameObject     ghostObj;

    private void Start()
    {
        rectTransform      = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;

        // Root canvas của World Space panel
        rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;

        // Ghost icon theo ngón tay
        ghostObj = new GameObject("GhostBasket");

        // Nếu panel là World Space Canvas, ghost cũng nằm trong cùng canvas
        ghostObj.transform.SetParent(rootCanvas.transform, false);
        ghostObj.transform.SetAsLastSibling();

        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite        = basketImage != null ? basketImage.sprite : null;
        ghostImg.raycastTarget = false;

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(80f, 80f);
        ghostRect.position  = rectTransform.position;

        CanvasGroup ghostCG = ghostObj.AddComponent<CanvasGroup>();
        ghostCG.alpha          = 0.7f;
        ghostCG.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostObj != null)
            ghostObj.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (ghostObj != null)
        {
            Destroy(ghostObj);
            ghostObj = null;
        }

        rectTransform.anchoredPosition = originalAnchoredPos;

        // Tìm PenDropTarget tại vị trí drop trong world
        PenDropTarget target = FindDropTarget(eventData.position);
        if (target != null)
        {
            bool ok = target.ReceiveBasketDrop();
            if (!ok)
                Debug.Log("[PenBasketDragItem] Drop rổ bị từ chối — chuồng chưa sẵn sàng");
        }
    }

    private PenDropTarget FindDropTarget(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Vector3 world  = cam.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world.x, world.y);

        // LayerMask mặc định (tất cả layer) — nếu cần hạn chế thì gán layer riêng cho chuồng
        Collider2D hit = Physics2D.OverlapPoint(world2);
        if (hit == null) return null;

        return hit.GetComponent<PenDropTarget>();
    }
}
