using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained floating drag icon — tạo Screen Space Overlay canvas riêng,
/// không phụ thuộc canvas cha hay Inspector. Gọi Show/Hide từ PlantDragController.
/// </summary>
public class FloatingDragIcon : MonoBehaviour
{
    // Inspector field giữ nguyên để không break serialization cũ
    [SerializeField] private Image iconImage;

    private Canvas        ghostCanvas;
    private RectTransform ghostRect;
    private bool          isFollowing;

    public void Show(Sprite icon)
    {
        Hide(); // dọn cũ nếu có

        // Tạo overlay canvas riêng — luôn đúng bất kể canvas cha là gì
        var go = new GameObject("_FloatingDragCanvas");
        ghostCanvas               = go.AddComponent<Canvas>();
        ghostCanvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        ghostCanvas.sortingOrder  = 9999; // luôn hiện trên cùng mọi canvas

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
        gameObject.SetActive(true); // bật Update() để ghost di chuyển theo cursor
        Debug.Log($"[FloatingDragIcon] Show icon={(icon != null ? icon.name : "NULL")}");
    }

    public void Hide()
    {
        isFollowing = false;
        gameObject.SetActive(false); // tắt Update()

        if (ghostCanvas != null)
        {
            Destroy(ghostCanvas.gameObject);
            ghostCanvas = null;
            ghostRect   = null;
        }

        Debug.Log("[FloatingDragIcon] Hide");
    }

    private void Update()
    {
        if (!isFollowing || ghostRect == null) return;
        ghostRect.position = InputBridge.PointerPosition;
    }
}
