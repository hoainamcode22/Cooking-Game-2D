using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggablePigFeedItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Cấu hình — gán trong Inspector")]
    public string feedItemId;
    public float feedDuration;

    [Header("Tham chiếu UI")]
    public TMP_Text txtFeedAmount;
    public Image imgFeedIcon;

    private Canvas rootCanvas;
    private RectTransform rectTransform;
    private Vector2 originalAnchoredPos;
    private CanvasGroup canvasGroup;
    private GameObject ghostObj;

    // Track slot đang được highlight để reset đúng lúc
    private PigSlotUI currentHighlightedSlot;

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && !rootCanvas.isRootCanvas)
            rootCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();

        rectTransform = GetComponent<RectTransform>();
        originalAnchoredPos = rectTransform.anchoredPosition;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Begin drag: " + gameObject.name);
        canvasGroup.blocksRaycasts = false;

        // Tạo ghost icon bám theo ngón tay
        ghostObj = new GameObject("GhostFeed");
        ghostObj.transform.SetParent(rootCanvas.transform, false);
        ghostObj.transform.SetAsLastSibling();

        Image ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite = imgFeedIcon != null ? imgFeedIcon.sprite : null;
        ghostImg.raycastTarget = false;

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(100f, 100f);
        ghostRect.position = rectTransform.position;

        CanvasGroup ghostCG = ghostObj.AddComponent<CanvasGroup>();
        ghostCG.alpha = 0.65f;
        ghostCG.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostObj != null)
            ghostObj.transform.position = eventData.position;

        // Cập nhật highlight slot gần nhất mỗi frame kéo
        UpdateSlotHighlight(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // Tắt highlight trước khi xử lý feed
        ClearHighlight();

        if (ghostObj != null)
        {
            Destroy(ghostObj);
            ghostObj = null;
        }

        rectTransform.anchoredPosition = originalAnchoredPos;
        TryFeedSlotAtPosition(eventData);
    }

    // ─── Highlight Logic ─────────────────────────────────────────

    private void UpdateSlotHighlight(PointerEventData eventData)
    {
        PigSlotUI nearest = GetNearestValidSlot(eventData, out float dist);
        PigSlotUI toHighlight = (nearest != null && dist < 200f) ? nearest : null;

        if (toHighlight == currentHighlightedSlot) return;

        // Reset slot cũ, bật slot mới
        if (currentHighlightedSlot != null)
            currentHighlightedSlot.SetDragHighlight(false);

        currentHighlightedSlot = toHighlight;

        if (currentHighlightedSlot != null)
            currentHighlightedSlot.SetDragHighlight(true);
    }

    private void ClearHighlight()
    {
        if (currentHighlightedSlot != null)
        {
            currentHighlightedSlot.SetDragHighlight(false);
            currentHighlightedSlot = null;
        }
    }

    // ─── Feed Detection ──────────────────────────────────────────

    private void TryFeedSlotAtPosition(PointerEventData eventData)
    {
        PigSlotUI bestSlot = GetNearestValidSlot(eventData, out float bestDist);

        Debug.Log($"[DragPig] End: best={(bestSlot != null ? bestSlot.name : "none")} dist={bestDist:F0}");

        if (bestSlot != null && bestDist < 200f)
        {
            Debug.Log($"[DragPig] HIT slot {bestSlot.name} dist={bestDist:F0}");
            bestSlot.StartFeeding(feedDuration > 0 ? feedDuration : 10f);
        }
        else
        {
            Debug.Log($"[DragPig] No slot hit. bestDist={bestDist:F0}");
        }
    }

    private PigSlotUI GetNearestValidSlot(PointerEventData eventData, out float bestDist)
    {
        PigSlotUI[] allSlots = FindObjectsByType<PigSlotUI>(FindObjectsSortMode.None);
        PigSlotUI best = null;
        bestDist = float.MaxValue;

        foreach (var slot in allSlots)
        {
            if (slot == null || !slot.CanFeed()) continue;

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera, slot.transform.position);
            float d = Vector2.Distance(eventData.position, screenPos);

            if (d < bestDist)
            {
                bestDist = d;
                best = slot;
            }
        }
        return best;
    }
}
