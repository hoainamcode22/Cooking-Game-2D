using System;
using System.Collections;
using UnityEngine;

public class WarehousePulseFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer glowRenderer;

    [Header("Shake")]
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float shakeStrength = 0.06f;
    [SerializeField] private float scaleBoost = 0.05f;

    [Header("Glow")]
    [SerializeField] private float glowMaxAlpha = 0.85f;

    private Coroutine pulseRoutine;
    private Vector3 originalLocalPos;
    private Vector3 originalLocalScale;

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        originalLocalPos = visualRoot.localPosition;
        originalLocalScale = visualRoot.localScale;

        if (glowRenderer != null)
        {
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }
    }

    public void PlayPulse()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(CoPulse());
    }

    private IEnumerator CoPulse()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float wave = Mathf.Sin(t * Mathf.PI);

            if (visualRoot != null)
            {
                float x = Mathf.Sin(t * 30f) * shakeStrength * (1f - t);
                visualRoot.localPosition = originalLocalPos + new Vector3(x, 0f, 0f);
                visualRoot.localScale = originalLocalScale * (1f + scaleBoost * wave);
            }

            if (glowRenderer != null)
            {
                Color c = glowRenderer.color;
                c.a = glowMaxAlpha * wave;
                glowRenderer.color = c;
            }

            yield return null;
        }

        if (visualRoot != null)
        {
            visualRoot.localPosition = originalLocalPos;
            visualRoot.localScale = originalLocalScale;
        }

        if (glowRenderer != null)
        {
            Color c = glowRenderer.color;
            c.a = 0f;
            glowRenderer.color = c;
        }

        pulseRoutine = null;
    }
}

