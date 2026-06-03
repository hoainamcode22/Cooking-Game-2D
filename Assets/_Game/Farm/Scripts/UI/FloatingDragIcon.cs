using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained floating drag icon â€” táº¡o Screen Space Overlay canvas riÃªng,
/// khÃ´ng phá»¥ thuá»™c canvas cha hay Inspector. Gá»i Show/Hide tá»« PlantDragController.
/// </summary>
public class FloatingDragIcon : MonoBehaviour
{
    // Inspector field giá»¯ nguyÃªn Ä‘á»ƒ khÃ´ng break serialization cÅ©
    [SerializeField] private Image iconImage;

    private Canvas        ghostCanvas;
    private RectTransform ghostRect;
    private bool          isFollowing;

    public void Show(Sprite icon)
    {
        Hide(); // dá»n cÅ© náº¿u cÃ³

        // Táº¡o overlay canvas riÃªng â€” luÃ´n Ä‘Ãºng báº¥t ká»ƒ canvas cha lÃ  gÃ¬
        var go = new GameObject("_FloatingDragCanvas");
        ghostCanvas               = go.AddComponent<Canvas>();
        ghostCanvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        ghostCanvas.overrideSorting = true;
        ghostCanvas.sortingOrder  = 9999; // luÃ´n hiá»‡n trÃªn cÃ¹ng má»i canvas

        var imgGo    = new GameObject("Icon");
        imgGo.transform.SetParent(go.transform, false);

        var img               = imgGo.AddComponent<Image>();
        img.sprite            = icon;
        img.raycastTarget     = false;
        img.preserveAspect    = true;

        ghostRect             = imgGo.GetComponent<RectTransform>();
        ghostRect.sizeDelta   = new Vector2(80f, 80f);
        ghostRect.anchorMin   = ghostRect.anchorMax = Vector2.zero;
        ghostRect.pivot       = new Vector2(0.5f, 0.5f);
        ghostRect.position    = InputBridge.PointerPosition;

        var cg                = imgGo.AddComponent<CanvasGroup>();
        cg.alpha              = 0.9f;
        cg.blocksRaycasts     = false;

        isFollowing = true;
        Canvas.willRenderCanvases -= FollowPointer;
        Canvas.willRenderCanvases += FollowPointer;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isFollowing = false;
        Canvas.willRenderCanvases -= FollowPointer;
        gameObject.SetActive(false);

        if (ghostCanvas != null)
        {
            Destroy(ghostCanvas.gameObject);
            ghostCanvas = null;
            ghostRect   = null;
        }
    }

    private void OnDestroy()
    {
        Canvas.willRenderCanvases -= FollowPointer;
    }

    private void FollowPointer()
    {
        if (!isFollowing || ghostRect == null) return;
        ghostRect.position = InputBridge.PointerPosition;
    }
}
