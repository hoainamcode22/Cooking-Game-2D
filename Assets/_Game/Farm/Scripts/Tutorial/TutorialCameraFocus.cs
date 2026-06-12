using System.Collections;
using UnityEngine;

/// <summary>
/// Di chuyển camera đến vùng trọng tâm tutorial (ô lúa / chậu hoa).
/// Gắn cùng GameObject với TutorialManager.
///
/// TutorialManager gọi:
///   _cameraFocus.FocusOnRice(bridge)    — khi tutorial bắt đầu phase lúa
///   _cameraFocus.FocusOnFlower(bridge)  — khi chuyển sang phase hoa
///   _cameraFocus.RestoreCamera()        — khi tutorial kết thúc
/// </summary>
public class TutorialCameraFocus : MonoBehaviour
{
    [Tooltip("Để trống → Camera.main")]
    [SerializeField] private Camera _camera;

    [Header("Zoom targets")]
    [Tooltip("OrthoSize khi focus vào ô lúa (nhỏ hơn = zoom in hơn)")]
    [SerializeField] private float _riceZoom   = 5f;
    [Tooltip("OrthoSize khi focus vào chậu hoa")]
    [SerializeField] private float _flowerZoom = 5f;

    [Header("Pan settings")]
    [SerializeField] private float _panDuration = 0.7f;
    [SerializeField] private AnimationCurve _ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Z Offset (2D — giữ nguyên độ sâu camera)")]
    [SerializeField] private float _cameraZ = -10f;

    // Trạng thái gốc trước khi tutorial focus
    private Vector3 _originalPosition;
    private float   _originalZoom;
    private bool    _savedOriginal;

    private Coroutine _panCoroutine;

    // =========================================================================
    void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>Focus vào trung tâm 6 ô lúa.</summary>
    public void FocusOnRice(TutorialStepTriggerBridge bridge)
    {
        Vector3 center = Vector3.zero;
        if (bridge != null) center = bridge.GetRicePlotsWorldCenter();

        // Fallback: find any Normal PlotController
        if (center == Vector3.zero)
        {
            var all = Object.FindObjectsByType<PlotController>(FindObjectsSortMode.None);
            System.Array.Sort(all, (a, b) => a.PlotId.CompareTo(b.PlotId));
            Vector3 sum = Vector3.zero; int cnt = 0;
            foreach (var p in all)
            {
                if (p.Category == PlotCategory.Normal) { sum += p.transform.position; cnt++; }
                if (cnt >= 6) break;
            }
            if (cnt > 0) center = sum / cnt;
        }

        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Rice plots not found — skipping focus.");
            return;
        }

        SaveOriginal();
        PanToWorld(center, _riceZoom);
        Debug.Log($"[TutorialCameraFocus] FocusOnRice center={center} zoom={_riceZoom}");
    }

    /// <summary>Focus vào trung tâm 2 chậu hoa.</summary>
    public void FocusOnFlower(TutorialStepTriggerBridge bridge)
    {
        if (bridge == null) return;
        Vector3 center = bridge.GetFlowerPotsWorldCenter();
        if (center == Vector3.zero)
        {
            Debug.LogWarning("[TutorialCameraFocus] Flower pots center = zero — bridge chua assign flower pots?");
            return;
        }
        PanToWorld(center, _flowerZoom);
        Debug.Log($"[TutorialCameraFocus] Focus on flower pots center: {center}");
    }

    /// <summary>Trả camera về vị trí/zoom ban đầu khi tutorial kết thúc.</summary>
    public void RestoreCamera()
    {
        if (!_savedOriginal) return;
        PanToWorld(_originalPosition, _originalZoom);
        Debug.Log("[TutorialCameraFocus] Restoring camera to original position.");
    }

    // =========================================================================
    // Internal
    // =========================================================================
    private void SaveOriginal()
    {
        if (_savedOriginal) return;
        if (_camera == null) return;
        _originalPosition = _camera.transform.position;
        _originalZoom     = _camera.orthographicSize;
        _savedOriginal    = true;
    }

    private void PanToWorld(Vector3 worldPos, float zoom)
    {
        if (_camera == null) return;
        Vector3 target = new Vector3(worldPos.x, worldPos.y, _cameraZ);
        if (_panCoroutine != null) StopCoroutine(_panCoroutine);
        _panCoroutine = StartCoroutine(PanRoutine(target, zoom));
    }

    private IEnumerator PanRoutine(Vector3 targetPos, float targetZoom)
    {
        Vector3 startPos  = _camera.transform.position;
        float   startZoom = _camera.orthographicSize;
        float   elapsed   = 0f;

        while (elapsed < _panDuration)
        {
            elapsed += Time.deltaTime;
            float t = _ease.Evaluate(Mathf.Clamp01(elapsed / _panDuration));
            _camera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            _camera.orthographicSize   = Mathf.Lerp(startZoom, targetZoom, t);
            yield return null;
        }

        _camera.transform.position = targetPos;
        _camera.orthographicSize   = targetZoom;
        _panCoroutine = null;
    }
}
