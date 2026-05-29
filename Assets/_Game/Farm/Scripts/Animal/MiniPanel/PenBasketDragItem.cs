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

    private RectTransform  rectTransform;
    private Vector2        originalAnchoredPos;
    private CanvasGroup    canvasGroup;
    private Canvas         ghostCanvas;  // screen-space overlay để ghost theo đúng cursor
    private GameObject     ghostObj;

    private void Start()
    {
        rectTransform       = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        FarmInputLock.IsDraggingSeed = true; // khoá map pan khi kéo rổ

        // Canvas Screen Space Overlay riêng — ghost position = screen pixels
        GameObject canvasGo = new GameObject("_BasketGhostCanvas");
        ghostCanvas = canvasGo.AddComponent<Canvas>();
        ghostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ghostCanvas.sortingOrder = 999;

        ghostObj = new GameObject("GhostBasket");
        ghostObj.transform.SetParent(canvasGo.transform, false);

        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite        = basketImage != null ? basketImage.sprite : null;
        ghostImg.raycastTarget = false;

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta  = new Vector2(80f, 80f);
        ghostRect.anchorMin  = ghostRect.anchorMax = Vector2.zero;
        ghostRect.pivot      = new Vector2(0.5f, 0.5f);
        ghostRect.position   = eventData.position; // screen pixels → đúng ngay cursor

        CanvasGroup ghostCG = ghostObj.AddComponent<CanvasGroup>();
        ghostCG.alpha          = 0.85f;
        ghostCG.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostObj != null)
            ghostObj.GetComponent<RectTransform>().position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        FarmInputLock.IsDraggingSeed = false; // mở khoá map pan

        if (ghostCanvas != null)
        {
            Destroy(ghostCanvas.gameObject);
            ghostCanvas = null;
            ghostObj = null;
        }

        rectTransform.anchoredPosition = originalAnchoredPos;

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
