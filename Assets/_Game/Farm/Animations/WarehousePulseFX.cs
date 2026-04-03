using System;
using System.Collections;
using UnityEngine;

public class WarehousePulseFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform visualRoot;       // Transform gốc của phần hình ảnh (dùng để rung/scale)
    [SerializeField] private SpriteRenderer glowRenderer; // SpriteRenderer hiệu ứng phát sáng

    [Header("Shake")]
    [SerializeField] private float duration = 0.18f;      // Thời gian hiệu ứng pulse (giây)
    [SerializeField] private float shakeStrength = 0.06f; // Biên độ rung theo trục X
    [SerializeField] private float scaleBoost = 0.05f;    // Mức độ phóng to tối đa khi pulse

    [Header("Glow")]
    [SerializeField] private float glowMaxAlpha = 0.85f;  // Alpha tối đa của hiệu ứng glow

    private Coroutine pulseRoutine;        // Coroutine đang chạy (dùng để tránh chồng chéo)
    private Vector3 originalLocalPos;      // Vị trí local ban đầu trước khi rung
    private Vector3 originalLocalScale;    // Scale local ban đầu trước khi phóng to

    // Khởi tạo: lưu vị trí/scale gốc và ẩn glow
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

    // Gọi từ ngoài để kích hoạt hiệu ứng pulse (nếu đang chạy thì restart)
    public void PlayPulse()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        pulseRoutine = StartCoroutine(CoPulse());
    }

    // Coroutine thực hiện hiệu ứng rung + glow rồi reset về trạng thái ban đầu
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

