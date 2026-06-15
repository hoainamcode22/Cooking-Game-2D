using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lia camera đến vùng trọng tâm tutorial (6 ô lúa / chậu hoa).
/// Gắn cùng GameObject với TutorialManager.
///
/// QUAN TRỌNG:
///  - Không tự ghi orthographicSize/position. Toàn bộ đi qua
///    CameraController.CinematicFocus() để camera có 1 chủ duy nhất → hết giật.
///  - Tâm focus tính theo TÂM NHÌN THẤY của ô (collider/renderer bounds center),
///    KHÔNG dùng transform.position vì gốc transform nằm dưới đáy tile → lệch.
///
/// TutorialManager gọi:
///   _cameraFocus.FocusOnRice(bridge)    — khi tới bước trồng lúa (L1L2_04)
///   _cameraFocus.FocusOnFlower(bridge)  — khi chuyển sang phase hoa
///   _cameraFocus.RestoreCamera()        — khi tutorial kết thúc
/// </summary>
public class TutorialCameraFocus : MonoBehaviour
{
    [Tooltip("Để trống → tự lấy CameraController trên Camera.main")]
    [SerializeField] private CameraController _camController;

    [Header("Zoom sizes (theo scale thật: default 750 / min 400 / max 1500)")]
    [Tooltip("OrthoSize khi focus vào 6 ô lúa (nhỏ hơn = zoom gần hơn)")]
    [SerializeField] private float _riceZoom   = 460f;
    [Tooltip("OrthoSize khi focus vào chậu hoa")]
    [SerializeField] private float _flowerZoom = 460f;

    // Giá trị scale cũ (vd 3.5) có thể còn sót trong Inspector → nếu < ngưỡng này thì bỏ qua.
    private const float MIN_VALID_ZOOM = 50f;
    private const float DEFAULT_ZOOM   = 460f;

    // Trạng thái gốc trước khi tutorial focus (để restore khi kết thúc)
    private Vector3 _originalPosition;
    private float   _originalZoom;
    private bool    _savedOriginal;

    // =========================================================================
    void Awake()
    {
        if (_camController == null && Camera.main != null)
            _camController = Camera.main.GetComponent<CameraController>();
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Lia camera vào trung tâm 6 ô lúa (theo tâm nhìn thấy) và zoom hợp lý.</summary>
    public void FocusOnRice(TutorialStepTriggerBridge bridge)
    {
        // Center theo TẤT CẢ ô Normal (8 ô) để khung hình + mask khớp với tay quét.
        Vector3 center = AverageVisualCenter(FindPlots(PlotCategory.Normal));

        if (center == Vector3.zero && bridge != null)
            center = bridge.GetRicePlotsWorldCenter();

        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Rice plots not found — skipping focus.");
            return;
        }

        SaveOriginal();
        float zoom = SanitizeZoom(_riceZoom);
        Focus(center, zoom);
        Debug.Log($"[TutorialCameraFocus] FocusOnRice center={center} zoom={zoom}");
    }

    /// <summary>Lia camera vào trung tâm các chậu hoa.</summary>
    public void FocusOnFlower(TutorialStepTriggerBridge bridge)
    {
        // Center theo TẤT CẢ chậu hoa (6 chậu) — không chỉ 1 chậu như trước.
        Vector3 center = AverageVisualCenter(FindPlots(PlotCategory.Flower));

        if (center == Vector3.zero && bridge != null)
            center = bridge.GetFlowerPotsWorldCenter();

        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Flower pots not found — skipping focus.");
            return;
        }
        SaveOriginal();
        float zoom = SanitizeZoom(_flowerZoom);
        Focus(center, zoom);
        Debug.Log($"[TutorialCameraFocus] Focus on flower pots center: {center} zoom={zoom}");
    }

    /// <summary>Trả camera về vị trí/zoom ban đầu và trả input cho người chơi.</summary>
    public void RestoreCamera()
    {
        if (_camController == null) return;

        if (_savedOriginal)
            _camController.CinematicFocus(_originalPosition, _originalZoom, false);

        _camController.EndCinematic();
        Debug.Log("[TutorialCameraFocus] Restoring camera + trả input cho người chơi.");
    }

    // =========================================================================
    // Internal
    // =========================================================================
    private void Focus(Vector3 worldPos, float zoom)
    {
        if (_camController == null)
        {
            Debug.LogWarning("[TutorialCameraFocus] Không tìm thấy CameraController trên Camera.main.");
            return;
        }
        _camController.CinematicFocus(worldPos, zoom, true);
    }

    private static float SanitizeZoom(float z) => z < MIN_VALID_ZOOM ? DEFAULT_ZOOM : z;

    /// <summary>Trung bình tâm-nhìn-thấy của danh sách ô. Vector3.zero nếu rỗng.</summary>
    private static Vector3 AverageVisualCenter(List<Transform> plots)
    {
        if (plots == null || plots.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; int n = 0;
        foreach (var t in plots)
        {
            if (t == null) continue;
            sum += PlotVisualCenter(t);
            n++;
        }
        return n > 0 ? sum / n : Vector3.zero;
    }

    /// <summary>Tìm mọi PlotController transform theo category.</summary>
    private static List<Transform> FindPlots(PlotCategory cat)
    {
        var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
        var list = new List<Transform>();
        foreach (var p in all) if (p != null && p.Category == cat) list.Add(p.transform);
        return list;
    }

    /// <summary>Tâm nhìn thấy của 1 ô = tâm collider/renderer (đã gồm offset+scale).</summary>
    private static Vector3 PlotVisualCenter(Transform t)
    {
        if (t == null) return Vector3.zero;
        var col = t.GetComponent<Collider2D>();
        if (col != null) return col.bounds.center;
        var rend = t.GetComponentInChildren<Renderer>();
        if (rend != null) return rend.bounds.center;
        return t.position;
    }

    private void SaveOriginal()
    {
        if (_savedOriginal || _camController == null) return;
        _originalPosition = _camController.CurrentPosition;
        _originalZoom     = _camController.CurrentSize;
        _savedOriginal    = true;
    }
}
