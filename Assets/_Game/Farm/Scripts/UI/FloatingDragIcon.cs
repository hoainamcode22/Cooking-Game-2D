using UnityEngine;
using UnityEngine.UI;

public class FloatingDragIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private RectTransform rt;
    private RectTransform canvasRect;
    private Canvas        canvas;
    private bool          isFollowing;

    private void Awake()
    {
        rt     = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (iconImage != null)
            iconImage.raycastTarget = false;

        gameObject.SetActive(false);
    }

    public void Show(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite        = icon;
            iconImage.raycastTarget = false;
        }

        gameObject.SetActive(true);
        isFollowing = true;

        Debug.Log($"[FloatingDragIcon] Show icon={(icon != null ? icon.name : "NULL")}");
    }

    public void Hide()
    {
        isFollowing = false;
        gameObject.SetActive(false);

        Debug.Log("[FloatingDragIcon] Hide");
    }

    private void Update()
    {
        if (!isFollowing || rt == null || canvasRect == null) return;

        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            InputBridge.PointerPosition,
            uiCam,
            out Vector2 localPos);

        rt.anchoredPosition = localPos;
    }
}
