using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIJuiceFeedback : MonoBehaviour, IPointerDownHandler
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
    [SerializeField] private float pressScale = 0.93f;
    [SerializeField] private float bounceScale = 1.06f;
    [SerializeField] private float pressTime = 0.05f;
    [SerializeField] private float bounceTime = 0.07f;
    [SerializeField] private float settleTime = 0.06f;

    private Vector3 baseScale;
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
        PlayFeedback();
    }

    public void PlayFeedback()
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

        routine = StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        Vector3 s0 = baseScale;
        Vector3 s1 = baseScale * pressScale;
        Vector3 s2 = baseScale * bounceScale;

        yield return LerpScale(s0, s1, pressTime);
        yield return LerpScale(s1, s2, bounceTime);
        yield return LerpScale(s2, s0, settleTime);

        target.localScale = baseScale;
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