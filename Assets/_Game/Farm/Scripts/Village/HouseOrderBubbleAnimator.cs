using System.Collections;
using UnityEngine;

namespace Village
{
    /// <summary>
    /// Thêm animation nhẹ nhàng cho bubble đơn hàng trên nhà dân.
    /// Thêm component này vào cùng GameObject với HouseOrderBubble.
    ///
    /// Animations:
    ///   - Float: bong bóng trôi lên xuống liên tục (sine wave)
    ///   - Pop-in: scale 0 → 1 với overshoot khi bubble xuất hiện
    ///   - Idle bounce: scale nhẹ để thu hút sự chú ý
    /// </summary>
    public class HouseOrderBubbleAnimator : MonoBehaviour
    {
        [Header("Float (lên xuống liên tục)")]
        [SerializeField] private float floatAmplitude = 4f;
        [SerializeField] private float floatSpeed     = 1.2f;
        [SerializeField] private bool  enableFloat    = true;

        [Header("Pop-in khi bubble xuất hiện")]
        [SerializeField] private float popInDuration  = 0.35f;
        [SerializeField] private bool  enablePopIn    = true;

        [Header("Idle Bounce (thỉnh thoảng nảy nhẹ)")]
        [SerializeField] private float bounceInterval = 3.5f;
        [SerializeField] private float bounceScale    = 1.12f;
        [SerializeField] private float bounceDuration = 0.2f;
        [SerializeField] private bool  enableBounce   = true;

        // Runtime
        private Vector3    _basePosition;
        private float      _floatOffset;
        private Coroutine  _popInRoutine;
        private Coroutine  _bounceRoutine;
        private bool       _isAnimating;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            // Ghi nhớ vị trí gốc mỗi lần bật — bong bóng có thể bị dịch chuyển theo nhà
            _basePosition = _rectTransform != null
                ? (Vector3)_rectTransform.anchoredPosition
                : transform.localPosition;

            _floatOffset  = Random.Range(0f, Mathf.PI * 2f); // phase random để các nhà không đồng bộ
            _isAnimating  = true;

            if (enablePopIn)
            {
                if (_popInRoutine != null) StopCoroutine(_popInRoutine);
                _popInRoutine = StartCoroutine(PopInRoutine());
            }

            if (enableBounce)
            {
                if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
                _bounceRoutine = StartCoroutine(BounceLoopRoutine());
            }
        }

        private void OnDisable()
        {
            _isAnimating = false;
            // Reset về scale/vị trí gốc để lần sau bật lại sạch
            transform.localScale = Vector3.one;
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = (Vector2)_basePosition;
            else
                transform.localPosition = _basePosition;
        }

        private void Update()
        {
            if (!_isAnimating || !enableFloat) return;

            float yOffset = Mathf.Sin(Time.time * floatSpeed + _floatOffset) * floatAmplitude;

            if (_rectTransform != null)
            {
                Vector2 pos = (Vector2)_basePosition;
                pos.y += yOffset;
                _rectTransform.anchoredPosition = pos;
            }
            else
            {
                Vector3 pos = _basePosition;
                pos.y += yOffset;
                transform.localPosition = pos;
            }
        }

        // ── Pop-in animation ───────────────────────────────────────────────────

        private IEnumerator PopInRoutine()
        {
            transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < popInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / popInDuration);
                float s = EaseOutBack(t);
                transform.localScale = Vector3.one * s;
                yield return null;
            }

            transform.localScale = Vector3.one;
            _popInRoutine = null;
        }

        // ── Idle bounce loop ───────────────────────────────────────────────────

        private IEnumerator BounceLoopRoutine()
        {
            // Chờ ngẫu nhiên để các nhà không bounce đồng thời
            yield return new WaitForSeconds(Random.Range(0f, bounceInterval));

            while (true)
            {
                yield return new WaitForSeconds(bounceInterval);

                if (!_isAnimating) yield break;

                // Scale up
                float half = bounceDuration * 0.5f;
                float t    = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / half;
                    float s = Mathf.Lerp(1f, bounceScale, Mathf.SmoothStep(0f, 1f, t));
                    transform.localScale = Vector3.one * s;
                    yield return null;
                }

                // Scale down
                t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / half;
                    float s = Mathf.Lerp(bounceScale, 1f, Mathf.SmoothStep(0f, 1f, t));
                    transform.localScale = Vector3.one * s;
                    yield return null;
                }

                transform.localScale = Vector3.one;
            }
        }

        // ── Easing ────────────────────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

#if UNITY_EDITOR
        [ContextMenu("Preview Pop-in")]
        private void DebugPopIn()
        {
            if (_popInRoutine != null) StopCoroutine(_popInRoutine);
            _popInRoutine = StartCoroutine(PopInRoutine());
        }
#endif
    }
}
