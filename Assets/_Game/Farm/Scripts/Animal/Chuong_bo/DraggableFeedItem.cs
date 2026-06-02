using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableFeedItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cáº¥u hÃ¬nh â€” gÃ¡n trong Inspector")]
    public string feedItemId;
    public float feedDuration;

    [Header("Tham chiáº¿u UI")]
    public TMP_Text txtFeedAmount;
    public Image imgFeedIcon;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private CanvasGroup canvasGroup;
    private Canvas ghostCanvas;   // screen-space overlay Ä‘á»ƒ ghost theo Ä‘Ãºng cursor
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
        FarmInputLock.IsDraggingSeed = true; // khoÃ¡ map pan khi kÃ©o thá»©c Äƒn

        // Táº¡o canvas Screen Space Overlay riÃªng â€” position = screen pixels, khÃ´ng bá»‹ lá»‡ch
        GameObject canvasGo = new GameObject("_FeedGhostCanvas");
        ghostCanvas = canvasGo.AddComponent<Canvas>();
        ghostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ghostCanvas.sortingOrder = 999;

        ghostObj = new GameObject("GhostFeed");
        ghostObj.transform.SetParent(canvasGo.transform, false);

        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite = imgFeedIcon != null ? imgFeedIcon.sprite : null;
        ghostImg.raycastTarget = false;

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(80f, 80f);
        ghostRect.anchorMin = ghostRect.anchorMax = Vector2.zero;
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = eventData.position; // screen pixels â†’ Ä‘Ãºng ngay cursor

        CanvasGroup ghostCG = ghostObj.AddComponent<CanvasGroup>();
        ghostCG.alpha = 0.85f;
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
        FarmInputLock.IsDraggingSeed = false; // má»Ÿ khoÃ¡ map pan

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

        Collider2D hit = Physics2D.OverlapPoint(world2);
        if (hit == null) return false;

        PenDropTarget target = hit.GetComponent<PenDropTarget>();
        if (target == null) return false;

        return target.ReceiveFoodDrop(feedItemId);
    }
}
