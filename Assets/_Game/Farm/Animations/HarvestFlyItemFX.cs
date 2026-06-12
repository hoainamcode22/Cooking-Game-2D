using System;
using System.Collections;
using UnityEngine;

public class HarvestFlyItemFX : MonoBehaviour
{
    [Header("Refs (World Prefab)")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Transform visualRoot;

    [Header("Timing")]
    [SerializeField] private float dropDuration = 0.12f;
    [SerializeField] private float groundStayDuration = 2.0f;
    [SerializeField] private float flyDuration = 0.65f;

    [Header("World Motion")]
    [SerializeField] private float scatterRadius = 120f;
    [SerializeField] private float dropDownOffset = 35f;

    [Header("Scale (World)")]
    [SerializeField] private Vector3 startScale = new Vector3(0.55f, 0.55f, 0.55f);
    [SerializeField] private Vector3 normalScale = new Vector3(0.85f, 0.85f, 0.85f);

    [Header("Misc")]
    [SerializeField] private bool destroyOnFinish = true;

    private Action onArrived;
    private Coroutine routine;

    private void Reset()
    {
        visualRoot = transform;
        iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Awake()
    {
        if (visualRoot == null)
            visualRoot = transform;

        if (iconRenderer == null)
            iconRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void ClearIconImmediate()
    {
        if (iconRenderer == null)
            iconRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (iconRenderer == null)
            return;

        iconRenderer.sprite = null;
        iconRenderer.enabled = false;
    }

    private void OnDisable() => StopRoutineIfRunning();

    private void OnDestroy() => StopRoutineIfRunning();

    private void StopRoutineIfRunning()
    {
        if (routine == null)
            return;

        try { StopCoroutine(routine); }
        finally { routine = null; }
    }

    public void Play(Sprite icon, Vector3 worldSpawnPos, Vector3 worldTargetPos, Action arrivedCallback = null)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopRoutineIfRunning();

        if (iconRenderer == null)
            iconRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (iconRenderer != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.enabled = icon != null;
        }
        else
        {
        }

        onArrived = arrivedCallback;
        transform.position = worldSpawnPos;

        if (visualRoot != null)
            visualRoot.localScale = startScale;

        routine = StartCoroutine(CoPlay(worldSpawnPos, worldTargetPos));
    }

    private IEnumerator CoPlay(Vector3 worldSpawnPos, Vector3 worldTargetPos)
    {
        Vector2 scatter = UnityEngine.Random.insideUnitCircle * scatterRadius;
        Vector3 groundPos = worldSpawnPos + new Vector3(scatter.x, scatter.y - dropDownOffset, 0f);

        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float t = dropDuration <= 0f ? 1f : Mathf.Clamp01(timer / dropDuration);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.LerpUnclamped(worldSpawnPos, groundPos, ease);

            if (visualRoot != null)
                visualRoot.localScale = Vector3.LerpUnclamped(startScale, normalScale, ease);

            yield return null;
        }

        transform.position = groundPos;
        if (visualRoot != null)
            visualRoot.localScale = normalScale;

        if (groundStayDuration > 0f)
            yield return new WaitForSeconds(groundStayDuration);

        timer = 0f;
        Vector3 flyStart = transform.position;

        while (timer < flyDuration)
        {
            timer += Time.deltaTime;
            float t = flyDuration <= 0f ? 1f : Mathf.Clamp01(timer / flyDuration);
            float ease = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.LerpUnclamped(flyStart, worldTargetPos, ease);
            yield return null;
        }

        transform.position = worldTargetPos;

        try { onArrived?.Invoke(); }
        catch (Exception) { }

        routine = null;

        if (destroyOnFinish)
            Destroy(gameObject);
    }
}
