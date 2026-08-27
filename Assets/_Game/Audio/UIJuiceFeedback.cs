using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIJuiceFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SoundType
    {
        None,
        UIButton,
        Ingredient
    }

    [SerializeField] private RectTransform target;
    [SerializeField] private SoundType soundType = SoundType.UIButton;

    [Header("Scale Animation")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float bounceScale = 1.1f;
    [SerializeField] private float pressTime = 0.05f;
    [SerializeField] private float bounceTime = 0.07f;
    [SerializeField] private float settleTime = 0.06f;

    [Header("Hover (nhún nhẹ khi đưa trỏ vào — Sếp 2026-08-27)")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float hoverTime = 0.08f;

    private Vector3 baseScale;
    private bool isPressed;
    private Coroutine routine;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        if (target != null)
            baseScale = target.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (target == null) return;

        if (AudioManager.Instance != null)
        {
            switch (soundType)
            {
                case SoundType.UIButton:
                    AudioManager.Instance.PlayUIClick();
                    break;
                case SoundType.Ingredient:
                    AudioManager.Instance.PlayIngredientPop();
                    break;
            }
        }

        if (routine != null)
            StopCoroutine(routine);

        isPressed = true;
        routine = StartCoroutine(PressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (target == null) return;

        if (routine != null)
            StopCoroutine(routine);

        isPressed = false;
        routine = StartCoroutine(BounceRoutine());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target == null || isPressed) return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(LerpToRoutine(baseScale * hoverScale, hoverTime));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target == null || isPressed) return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(LerpToRoutine(baseScale, hoverTime));
    }

    /// <summary>Đổi loại âm thanh từ code (field là SerializeField private) — thẻ nguyên liệu dùng tiếng "pop".</summary>
    public void SetSound(SoundType type)
    {
        soundType = type;
    }

    private IEnumerator LerpToRoutine(Vector3 to, float duration)
    {
        yield return LerpScale(target.localScale, to, duration);
    }

    // Tương thích API cũ nếu có script khác trực tiếp gọi PlayFeedback
    public void PlayFeedback()
    {
        OnPointerDown(null);
        OnPointerUp(null); // Gọi liên tiếp để mô phỏng 1 chu kỳ nhanh nếu gọi qua code
    }

    private IEnumerator PressRoutine()
    {
        Vector3 s0 = target.localScale;
        Vector3 s1 = baseScale * pressScale;

        yield return LerpScale(s0, s1, pressTime);
    }

    private IEnumerator BounceRoutine()
    {
        Vector3 s0 = target.localScale;
        Vector3 s1 = baseScale * bounceScale;

        yield return LerpScale(s0, s1, bounceTime);
        yield return LerpScale(s1, baseScale, settleTime);
    }

    private IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            target.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }

        target.localScale = to;
    }
}