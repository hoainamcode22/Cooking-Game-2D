using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Thêm hiệu ứng xúc giác lò xo (Juicy Squash & Stretch) + vệt sáng lướt qua (Shimmer)
/// cho mọi nút bấm UI trong game.
/// Tự động gắn vào Button bất kỳ hoặc kéo thả vào Prefab.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class JuicyButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("◆ Độ nảy khi chạm")]
    [SerializeField] private float pressScale = 0.92f;
    [SerializeField] private float popScale = 1.07f;
    [SerializeField] private float animDuration = 0.18f;

    [Header("◆ Kêu gọi hành động (Call To Action)")]
    [Tooltip("Bật chế độ phập phồng mời gọi người chơi bấm vào.")]
    [SerializeField] private bool attentionBreathing = false;
    [SerializeField] private bool enableGlowHalo = false;

    private RectTransform _rect;
    private Vector3 _baseScale;
    private Coroutine _animRoutine;
    private UIGlowPulseFX _glowFx;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _baseScale = _rect.localScale;

        if (enableGlowHalo)
            _glowFx = UIGlowPulseFX.Attach(_rect);
    }

    private void OnEnable()
    {
        _rect.localScale = _baseScale;
        if (attentionBreathing)
            StartCoroutine(RoutineBreathing());
    }

    public void SetAttention(bool active, Color? glowColor = null)
    {
        attentionBreathing = active;
        enableGlowHalo = active;

        if (active)
        {
            if (_glowFx == null) _glowFx = UIGlowPulseFX.Attach(_rect, glowColor);
            else _glowFx.Play();
            StartCoroutine(RoutineBreathing());
        }
        else
        {
            if (_glowFx != null) _glowFx.Stop();
            StopAllCoroutines();
            _rect.localScale = _baseScale;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(RoutineScaleTo(_baseScale * pressScale, 0.08f));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_animRoutine != null) StopCoroutine(_animRoutine);
        _animRoutine = StartCoroutine(RoutineReleasePop());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        JuicyPulseFX.Play(transform, 1.12f, 0.22f);
    }

    private IEnumerator RoutineScaleTo(Vector3 targetScale, float dur)
    {
        Vector3 start = _rect.localScale;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _rect.localScale = Vector3.Lerp(start, targetScale, Mathf.Clamp01(t / dur));
            yield return null;
        }
        _rect.localScale = targetScale;
    }

    private IEnumerator RoutineReleasePop()
    {
        // Giai đoạn 1: Nảy vọt qua gốc (Overshoot 1.07x)
        yield return RoutineScaleTo(_baseScale * popScale, 0.09f);

        // Giai đoạn 2: Dao động đàn hồi về gốc (1.0x)
        yield return RoutineScaleTo(_baseScale, 0.08f);
    }

    private IEnumerator RoutineBreathing()
    {
        while (attentionBreathing)
        {
            float t = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1.0f, 1.06f, t);
            _rect.localScale = _baseScale * scale;
            yield return null;
        }
        _rect.localScale = _baseScale;
    }
}
