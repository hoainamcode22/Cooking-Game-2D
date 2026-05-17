using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Icon nổi theo con trỏ khi kéo hạt giống.
/// Dùng InputBridge.PointerPosition — hoạt động đúng trên cả Mouse và Touch.
/// </summary>
public class FloatingDragIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private RectTransform _rt;
    private RectTransform _canvasRect;
    private Canvas        _canvas;
    private bool          _isFollowing;

    private void Awake()
    {
        _rt     = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        if (_canvas != null)
            _canvasRect = _canvas.GetComponent<RectTransform>();

        gameObject.SetActive(false);
    }

    public void Show(Sprite icon)
    {
        if (iconImage != null) iconImage.sprite = icon;
        gameObject.SetActive(true);
        _isFollowing = true;
    }

    public void Hide()
    {
        _isFollowing = false;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_isFollowing || _canvasRect == null) return;

        Camera uiCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            InputBridge.PointerPosition,   // Mouse và Touch đều đúng
            uiCam,
            out Vector2 localPos);

        _rt.anchoredPosition = localPos;
    }
}
