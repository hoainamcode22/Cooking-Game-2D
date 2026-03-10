using System.Collections;
using UnityEngine;

public class CookingFX : MonoBehaviour
{
    [SerializeField] private RectTransform dishImage;
    [SerializeField] private RectTransform cookButton;
    [SerializeField] private RectTransform resultPanel;

    [Header("Cook")]
    [SerializeField] private float cookDuration = 0.8f;
    [SerializeField] private float shakeAngle = 5f;
    [SerializeField] private float dishPulse = 0.05f;

    private Vector3 dishBaseScale;
    private Quaternion dishBaseRot;
    private Vector3 cookBtnBaseScale;
    private Vector3 resultBaseScale;

    private void Awake()
    {
        if (dishImage != null)
        {
            dishBaseScale = dishImage.localScale;
            dishBaseRot = dishImage.localRotation;
        }

        if (cookButton != null)
            cookBtnBaseScale = cookButton.localScale;

        if (resultPanel != null)
            resultBaseScale = resultPanel.localScale;
    }

    public void PlayCookFX()
    {
        StopAllCoroutines();
        StartCoroutine(CookRoutine());
    }

    public void PlayResultFX()
    {
        if (resultPanel != null)
            StartCoroutine(Bounce(resultPanel, resultBaseScale, 1.1f, 0.22f));
    }

    private IEnumerator CookRoutine()
    {
        if (dishImage == null)
            yield break;

        float t = 0f;
        while (t < cookDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / cookDuration);

            float angle = Mathf.Sin(p * 10f * Mathf.PI) * shakeAngle;
            float scale = 1f + Mathf.Abs(Mathf.Sin(p * 8f * Mathf.PI)) * dishPulse;

            dishImage.localRotation = Quaternion.Euler(0f, 0f, angle);
            dishImage.localScale = dishBaseScale * scale;

            if (cookButton != null)
            {
                float btnScale = 1f + Mathf.Abs(Mathf.Sin(p * 6f * Mathf.PI)) * 0.03f;
                cookButton.localScale = cookBtnBaseScale * btnScale;
            }

            yield return null;
        }

        dishImage.localRotation = dishBaseRot;
        dishImage.localScale = dishBaseScale;

        if (cookButton != null)
            cookButton.localScale = cookBtnBaseScale;
    }

    private IEnumerator Bounce(RectTransform target, Vector3 baseScale, float maxScale, float duration)
    {
        float half = duration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            target.localScale = Vector3.Lerp(baseScale, baseScale * maxScale, p);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / half);
            target.localScale = Vector3.Lerp(baseScale * maxScale, baseScale, p);
            yield return null;
        }

        target.localScale = baseScale;
    }
}