using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Icon nổi theo chuột khi kéo hạt giống.
/// Gắn lên một GameObject trên Screen Space Overlay canvas.
/// </summary>
public class FloatingDragIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private bool isFollowing;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    public void Show(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;

        gameObject.SetActive(true);
        isFollowing = true;
    }

    public void Hide()
    {
        isFollowing = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isFollowing || canvasRect == null)
            return;

        // Lấy canvas gốc để convert screen → local position
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            uiCam,
            out Vector2 localPos);

        rectTransform.anchoredPosition = localPos;
    }
}
