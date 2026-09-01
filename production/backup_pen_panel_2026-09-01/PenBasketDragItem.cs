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
            target.ReceiveBasketDrop();
        }
    }

    private PenDropTarget FindDropTarget(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Vector3 world  = cam.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world.x, world.y);

        // Quét tất cả colliders để tránh bị livestock hay hàng rào che mất
        Collider2D[] hits = Physics2D.OverlapPointAll(world2);
        if (hits != null)
        {
            foreach (var hit in hits)
            {
                var target = hit.GetComponent<PenDropTarget>() ?? hit.GetComponentInParent<PenDropTarget>();
                if (target != null) return target;
            }
        }

        // Fallback: Nếu thả gần vị trí chuồng (tăng bán kính 4→6 cho chuồng bò/heo lớn)
        var parentPen = GetComponentInParent<PenMiniPanelUI>();
        if (parentPen != null)
        {
            float dist = Vector2.Distance(world2, parentPen.transform.position);
            if (dist < 6f)
            {
                return parentPen.GetComponent<PenDropTarget>() ?? parentPen.GetComponentInChildren<PenDropTarget>() ?? parentPen.GetComponentInParent<PenDropTarget>();
            }
        }

        // Fallback cuối: tìm PenDropTarget gần nhất trong scene (bán kính 6 unit)
        var allTargets = Object.FindObjectsByType<PenDropTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PenDropTarget closest = null;
        float closestDist = 6f;
        foreach (var t in allTargets)
        {
            float d = Vector2.Distance(world2, (Vector2)t.transform.position);
            if (d < closestDist) { closestDist = d; closest = t; }
        }
        return closest;
    }
}
