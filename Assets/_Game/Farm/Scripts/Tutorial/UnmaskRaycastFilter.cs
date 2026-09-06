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

    // Chế độ "lỗ theo screen-rect tường minh" (px) — dùng cho vùng bao quanh nhiều ô (6 ô đất).
    private bool    _useScreenRect;
    private Vector2 _screenRectCenterPx;
    private Vector2 _screenRectSizePx;

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

    /// <summary>
    /// [FIX 2026-09-06] Lop toi CO dang khoet lo khong.
    /// Lop toi bat ma KHONG co lo = chan 100% click toan man hinh (IsRaycastLocationValid tra true
    /// khap noi) ⇒ nguoi choi khong bam duoc gi. TutorialManager dung co nay lam luoi an toan.
    /// </summary>
    public bool CoLoKhoet => _useScreenRect || _currentTarget != null;

    public void SetTarget(RectTransform target, bool useCircle, float paddingPx)
    {
        _useScreenRect = false;
        _currentTarget = target;
        _useCircle     = useCircle;
        _paddingPx     = paddingPx;
    }

    /// <summary>Khoét lỗ theo 1 vùng screen-space tường minh (px). Dùng cho vùng bao quanh 6 ô đất.</summary>
    public void SetScreenRect(Vector2 centerPx, Vector2 sizePx, bool useCircle)
    {
        _useScreenRect      = true;
        _currentTarget      = null;
        _screenRectCenterPx = centerPx;
        _screenRectSizePx   = sizePx;
        _useCircle          = useCircle;
    }

    public void ClearHole()
    {
        _currentTarget = null;
        _useScreenRect = false;
        if (_matInstance == null) return;
        _matInstance.SetVector(ID_HoleCenter, new Vector4(-9f, -9f, 0f, 0f));
        _matInstance.SetVector(ID_HoleSize,   Vector4.zero);
    }

    void LateUpdate()
    {
        if (_matInstance == null) return;

        // Chế độ vùng bao tường minh (6 ô đất) — tính UV trực tiếp từ screen px.
        if (_useScreenRect)
        {
            float sw0 = Screen.width;
            float sh0 = Screen.height;
            var cUV = new Vector2(_screenRectCenterPx.x / sw0, _screenRectCenterPx.y / sh0);
            var sUV = new Vector2(_screenRectSizePx.x   / sw0, _screenRectSizePx.y   / sh0);
            _matInstance.SetVector(ID_HoleCenter, new Vector4(cUV.x, cUV.y, 0f, 0f));
            _matInstance.SetVector(ID_HoleSize,   new Vector4(sUV.x, sUV.y, 0f, 0f));
            _matInstance.SetFloat(ID_CircleHole,  _useCircle ? 1f : 0f);
            return;
        }

        if (_currentTarget == null) return;
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
        // 1. Vùng bao nhiều ô (screen-rect) CHỈ là hiệu ứng tối — KHÔNG chặn click ở đâu cả,
        // để user vẫn click ô + bảng chọn hạt + liềm bình thường khi đang trồng/thu hoạch.
        if (_useScreenRect) return false;

        // 2. Khi khay hạt giống hoặc popup tương tác đang mở — KHÔNG chặn raycast để người chơi kéo thả hạt / liềm
        if (FarmInputLock.IsSeedPopupOpen) return false;

        // 3. Nếu tutorial manager đang ở các bước kéo hạt, thu hoạch, cho ăn — không chặn click gameplay
        if (TutorialManager.Instance != null && TutorialManager.Instance.LaBuocGameplayMoKhongKhoaRaycast())
            return false;

        if (_currentTarget == null) return true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _currentTarget, screenPoint, eventCamera, out Vector2 localPoint))
            return true;

        Rect rect = _currentTarget.rect;
        if (_paddingPx > 0f)
        {
            rect.xMin -= _paddingPx;
            rect.xMax += _paddingPx;
            rect.yMin -= _paddingPx;
            rect.yMax += _paddingPx;
        }

        return !rect.Contains(localPoint);
    }
}
