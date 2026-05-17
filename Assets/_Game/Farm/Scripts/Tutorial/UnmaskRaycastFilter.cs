using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Dim_Background Image.
/// Nhiệm vụ kép:
///   1. Cập nhật shader DimWithHole để vẽ lỗ tại vị trí target (LateUpdate).
///   2. ICanvasRaycastFilter: click trong lỗ xuyên qua, click ngoài bị chặn.
/// </summary>
[RequireComponent(typeof(Image))]
public class UnmaskRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    private Image         _image;
    private Material      _matInstance;
    private Canvas        _rootCanvas;
    private RectTransform _currentTarget;
    private bool          _useCircle;
    private float         _paddingPx;

    private static readonly int ID_HoleCenter = Shader.PropertyToID("_HoleCenter");
    private static readonly int ID_HoleSize   = Shader.PropertyToID("_HoleSize");
    private static readonly int ID_CircleHole = Shader.PropertyToID("_CircleHole");

    void Awake()
    {
        _image      = GetComponent<Image>();
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        if (_image.material != null)
            _matInstance = new Material(_image.material);
        else
            Debug.LogWarning("[UnmaskRaycastFilter] Image chưa có material DimWithHole.");

        _image.material = _matInstance;
        ClearHole();
    }

    void OnDestroy()
    {
        if (_matInstance != null) Destroy(_matInstance);
    }

    public void SetTarget(RectTransform target, bool useCircle, float paddingPx)
    {
        _currentTarget = target;
        _useCircle     = useCircle;
        _paddingPx     = paddingPx;
    }

    public void ClearHole()
    {
        _currentTarget = null;
        if (_matInstance == null) return;
        _matInstance.SetVector(ID_HoleCenter, new Vector4(-9f, -9f, 0f, 0f));
        _matInstance.SetVector(ID_HoleSize,   Vector4.zero);
    }

    void LateUpdate()
    {
        if (_matInstance == null || _currentTarget == null) return;
        if (!_currentTarget.gameObject.activeInHierarchy) return;

        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        _currentTarget.GetWorldCorners(corners);

        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        screenMin -= Vector2.one * _paddingPx;
        screenMax += Vector2.one * _paddingPx;

        float sw = Screen.width;
        float sh = Screen.height;

        var centerUV = new Vector2(
            (screenMin.x + screenMax.x) * 0.5f / sw,
            (screenMin.y + screenMax.y) * 0.5f / sh);

        var sizeUV = new Vector2(
            (screenMax.x - screenMin.x) / sw,
            (screenMax.y - screenMin.y) / sh);

        _matInstance.SetVector(ID_HoleCenter, new Vector4(centerUV.x, centerUV.y, 0f, 0f));
        _matInstance.SetVector(ID_HoleSize,   new Vector4(sizeUV.x,   sizeUV.y,   0f, 0f));
        _matInstance.SetFloat(ID_CircleHole,  _useCircle ? 1f : 0f);
    }

    // false = element này không chặn raycast tại điểm này (click xuyên qua).
    // true  = element chặn raycast (click bị giữ lại ở lớp tối).
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_currentTarget == null) return true;

        return !RectTransformUtility.RectangleContainsScreenPoint(
            _currentTarget, screenPoint, eventCamera);
    }
}
