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
    [SerializeField] private float _loopInterval   = 1.0f;
    [SerializeField] private AnimationCurve _ease  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float _tapScale        = 0.82f;

    private RectTransform _hand;
    private Coroutine     _loop;
    private string        _fromId;
    private string        _toId;

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

            // 1. Snap to FROM
            _hand.position = fromRT.position;

            // 2. Press tap
            yield return ScaleHand(Vector3.one, new Vector3(_tapScale, _tapScale, 1f), 0.08f);

            // 3. Drag to TO
            yield return MoveHand(fromRT.position, toRT.position, _travelDuration);

            // 4. Release
            yield return ScaleHand(new Vector3(_tapScale, _tapScale, 1f), Vector3.one, 0.08f);

            // 5. Hide hand, wait, repeat
            _hand.gameObject.SetActive(false);
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
            _hand.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        _hand.position = to;
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
