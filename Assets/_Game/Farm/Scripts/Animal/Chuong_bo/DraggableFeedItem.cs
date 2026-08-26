using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableFeedItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cấu hình — gán trong Inspector")]
    public string feedItemId;
    public float feedDuration;

    [Header("Tham chiếu UI")]
    public TMP_Text txtFeedAmount;
    public Image imgFeedIcon;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private CanvasGroup canvasGroup;
    private Canvas ghostCanvas;   // screen-space overlay để ghost theo đúng cursor
    private GameObject ghostObj;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        FarmInputLock.IsDraggingSeed = true; // khoá map pan khi kéo thức ăn

        // Tạo canvas Screen Space Overlay riêng — position = screen pixels, không bị lệch
        GameObject canvasGo = new GameObject("_FeedGhostCanvas");
        ghostCanvas = canvasGo.AddComponent<Canvas>();
        ghostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ghostCanvas.sortingOrder = 999;

        ghostObj = new GameObject("GhostFeed");
        ghostObj.transform.SetParent(canvasGo.transform, false);

        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite = imgFeedIcon != null ? imgFeedIcon.sprite : null;
        ghostImg.preserveAspect = true;
        ghostImg.raycastTarget = false;

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        // Phóng to ghost để dễ thấy khi kéo thả vào chuồng
        ghostRect.sizeDelta = new Vector2(120f, 120f);
        ghostRect.anchorMin = ghostRect.anchorMax = Vector2.zero;
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = eventData.position;

        CanvasGroup ghostCG = ghostObj.AddComponent<CanvasGroup>();
        ghostCG.alpha = 0.92f;
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

        TryDropOnPenTarget(eventData.position);
    }

    private bool TryDropOnPenTarget(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 world  = cam.ScreenToWorldPoint(screenPos);
        Vector2 world2 = new Vector2(world.x, world.y);

        Collider2D[] hits = Physics2D.OverlapPointAll(world2);
        if (hits != null)
        {
            foreach (var hit in hits)
            {
                PenDropTarget target = hit.GetComponent<PenDropTarget>() ?? hit.GetComponentInParent<PenDropTarget>();
                if (target != null)
                {
                    Debug.Log($"[FeedDrop] ✅ Thả trúng PenDropTarget={target.name}, food={feedItemId}");
                    return target.ReceiveFoodDrop(feedItemId);
                }
            }
        }

        // Fallback: Nếu thả gần chuồng cha — tăng bán kính 4→6 để chuồng bò/heo lớn vẫn nhận
        var parentPen = GetComponentInParent<PenMiniPanelUI>();
        if (parentPen != null)
        {
            float dist = Vector2.Distance(world2, parentPen.transform.position);
            Debug.Log($"[FeedDrop] Fallback check dist={dist:F2} to {parentPen.name}, food={feedItemId}");
            if (dist < 6f)
            {
                var target = parentPen.GetComponent<PenDropTarget>() ?? parentPen.GetComponentInChildren<PenDropTarget>() ?? parentPen.GetComponentInParent<PenDropTarget>();
                if (target != null)
                {
                    Debug.Log($"[FeedDrop] ✅ Fallback thả vào {target.name}");
                    return target.ReceiveFoodDrop(feedItemId);
                }
            }
        }

        // Fallback cuối: tìm tất cả PenDropTarget trong scene, chọn cái gần nhất trong 6 unit
        var allTargets = Object.FindObjectsByType<PenDropTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PenDropTarget closest = null;
        float closestDist = 6f;
        foreach (var t in allTargets)
        {
            float d = Vector2.Distance(world2, (Vector2)t.transform.position);
            if (d < closestDist) { closestDist = d; closest = t; }
        }
        if (closest != null)
        {
            Debug.Log($"[FeedDrop] ✅ Scene fallback thả vào {closest.name}, dist={closestDist:F2}");
            return closest.ReceiveFoodDrop(feedItemId);
        }

        Debug.LogWarning($"[FeedDrop] ❌ Không tìm thấy PenDropTarget nào gần vị trí drop, food={feedItemId}");
        return false;
    }
}
