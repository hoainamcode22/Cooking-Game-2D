using System.Collections;
using UnityEngine;

/// <summary>
/// Gắn lên Tutorial_Camera.
/// TutorialManager gọi StartCoroutine(ZoomIn()) song song với animation mây trong intro.
/// </summary>
public class TutorialCameraZoom : MonoBehaviour
{
    [Tooltip("Để trống → tự dùng Camera.main")]
    [SerializeField] private Camera _camera;

    [SerializeField] public float startSize = 8f;
    [SerializeField] public float endSize   = 4f;
    [SerializeField] public float duration  = 1.5f;
    [SerializeField] private AnimationCurve _ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    public IEnumerator ZoomIn()
    {
        if (_camera == null)
        {
            Debug.LogWarning("[TutorialCameraZoom] Không tìm thấy camera.");
            yield break;
        }

        _camera.orthographicSize = startSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _camera.orthographicSize = Mathf.Lerp(startSize, endSize,
                _ease.Evaluate(Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        _camera.orthographicSize = endSize;
    }

    public void ResetZoom()
    {
        if (_camera != null) _camera.orthographicSize = startSize;
    }
}
