using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class HHDecorRendererFader : MonoBehaviour
{
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float time = 0.5f;
    [FormerlySerializedAs("renderer")]
    public SpriteRenderer RendererToHide;
    public float finalAlpha = 0.2f;
    public Tilemap tilemap;

    private Color initialColor;
    private Color workingColor;
    private Coroutine fadeRoutine;

    private void Start()
    {
        curve.preWrapMode = WrapMode.Once;
        curve.postWrapMode = WrapMode.ClampForever;

        if (RendererToHide != null)
            initialColor = RendererToHide.color;
        else if (tilemap != null)
            initialColor = tilemap.color;
        else
            initialColor = Color.white;

        workingColor = initialColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FadeTo(finalAlpha);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FadeTo(initialColor.a);
    }

    private void FadeTo(float alpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(AnimCurve(workingColor.a, alpha));
    }

    private IEnumerator AnimCurve(float from, float to)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, time);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            workingColor.a = Mathf.Lerp(from, to, t);

            if (tilemap != null)
                tilemap.color = workingColor;
            if (RendererToHide != null)
                RendererToHide.color = workingColor;

            yield return null;
        }
    }
}
