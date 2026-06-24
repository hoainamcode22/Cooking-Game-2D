using System.Collections;
using UnityEngine;

/// <summary>
/// Animate bàn tay theo chuyển động "kéo" từ target này sang target khác, lặp vòng.
///
/// TutorialManager gọi StartDragHint / StopDragHint tại mỗi bước tutorial.
/// Tự lấy RectTransform từ TutorialManager.HandPointerRT nếu không được gán.
///
/// Không phá drag logic thật — chỉ di chuyển UI hand pointer.
/// </summary>
public class TutorialDragHintAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _travelDuration = 0.55f;
    [SerializeField] private float _loopInterval   = 0.6f;
    [SerializeField] private AnimationCurve _ease  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _tapScale        = 0.82f;

    [Tooltip("Vị trí ĐẦU NGÓN TAY trên ảnh hand (0-1). tutorial_hand trỏ XUỐNG → đầu ngón ~ (0.36, 0.1).")]
    [SerializeField] private Vector2 _fingertipNormalized = new Vector2(0.36f, 0.1f);

    [SerializeField] private RectTransform _hand;
    private Coroutine     _loop;
    private string        _fromId;
    private string        _toId;
    private static readonly Vector3[] _cornerBuf = new Vector3[4];

    public bool IsRunning => _loop != null;

    // =========================================================================
    void Start()
    {
        // Lazy-resolve hand pointer from TutorialManager on same GO
        if (_hand == null)
        {
            var mgr = GetComponent<TutorialManager>();
            if (mgr != null) _hand = mgr.HandPointerRT;
        }
    }

    // =========================================================================
    // Public API
    // =========================================================================

    public void StartDragHint(string fromTargetId, string toTargetId)
    {
        // Resolve hand pointer lazily
        if (_hand == null)
        {
            var mgr = GetComponent<TutorialManager>();
            if (mgr != null) _hand = mgr.HandPointerRT;
        }

        if (_hand == null)
        {
            Debug.LogWarning("[TutorialDragHint] Hand pointer not found on TutorialManager — cannot animate.");
            return;
        }

        // Already running same hint? No-op.
        if (_fromId == fromTargetId && _toId == toTargetId && _loop != null) return;

        StopDragHint();
        _fromId = fromTargetId;
        _toId   = toTargetId;
        _loop   = StartCoroutine(DragLoop());
        Debug.Log($"[TutorialDragHint] Start: '{fromTargetId}' → '{toTargetId}'");
    }

    public void StopDragHint()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        if (_hand != null) _hand.gameObject.SetActive(false);
    }

    // =========================================================================
    // Animation
    // =========================================================================

    private IEnumerator DragLoop()
    {
        var waitRetry = new WaitForSeconds(_loopInterval);

        while (true)
        {
            RectTransform fromRT = TutorialManager.GetTargetRect(_fromId);
            RectTransform toRT   = TutorialManager.GetTargetRect(_toId);

            if (fromRT == null)
            {
                Debug.LogWarning($"[TutorialDragHint] '{_fromId}' not registered yet — retrying...");
                yield return waitRetry;
                continue;
            }
            if (toRT == null)
            {
                Debug.LogWarning($"[TutorialDragHint] '{_toId}' not registered yet — retrying...");
                yield return waitRetry;
                continue;
            }

            _hand.gameObject.SetActive(true);

            // Tâm hình học (không phải pivot) của item nguồn & đích
            Vector3 fromC = TargetCenter(fromRT);
            Vector3 toC   = TargetCenter(toRT);

            // 1. Đặt ĐẦU NGÓN TAY lên item nguồn (ô lúa)
            _hand.localScale = Vector3.one;
            PlaceHandFingertipAt(fromC);

            // 2. Press tap
            yield return ScaleHand(Vector3.one, new Vector3(_tapScale, _tapScale, 1f), 0.08f);

            // 3. Drag to TO (đầu ngón tay bám theo)
            yield return MoveHand(fromC, toC, _travelDuration);

            // 4. Release
            yield return ScaleHand(new Vector3(_tapScale, _tapScale, 1f), Vector3.one, 0.08f);

            // 5. Snap về nguồn, GIỮ HIỆN (không chớp tắt), chờ rồi lặp
            _hand.localScale = Vector3.one;
            PlaceHandFingertipAt(fromC);
            yield return waitRetry;
        }
    }

    private IEnumerator MoveHand(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = _ease.Evaluate(Mathf.Clamp01(elapsed / duration));
            PlaceHandFingertipAt(Vector3.Lerp(from, to, t));
            yield return null;
        }
        PlaceHandFingertipAt(to);
    }

    /// <summary>Tâm hình học world-space của 1 RectTransform (gồm width/height) — tránh lệch do pivot.</summary>
    private static Vector3 TargetCenter(RectTransform rt)
    {
        rt.GetWorldCorners(_cornerBuf);
        return (_cornerBuf[0] + _cornerBuf[2]) * 0.5f;
    }

    /// <summary>Đặt hand sao cho ĐẦU NGÓN TAY (theo width/height ảnh hand) trùng worldCenter.</summary>
    private void PlaceHandFingertipAt(Vector3 worldCenter)
    {
        _hand.position = worldCenter;
        Rect r = _hand.rect;
        Vector2 fingerFromPivot = new Vector2(
            (_fingertipNormalized.x - _hand.pivot.x) * r.width,
            (_fingertipNormalized.y - _hand.pivot.y) * r.height);
        Vector2 scaled = new Vector2(
            fingerFromPivot.x * _hand.localScale.x,
            fingerFromPivot.y * _hand.localScale.y);
        _hand.anchoredPosition -= scaled;   // kéo đầu ngón tay về đúng tâm item
    }

    private IEnumerator ScaleHand(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _hand.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _hand.localScale = to;
    }
}
