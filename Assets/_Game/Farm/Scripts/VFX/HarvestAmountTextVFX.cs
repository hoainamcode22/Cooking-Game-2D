using System.Collections;
using TMPro;
using UnityEngine;

public class HarvestAmountTextVFX : MonoBehaviour
{
    [Header("Template")]
    [Tooltip("Child TextTemplate (TextMeshPro). Nếu null sẽ tự tìm.")]
    [SerializeField] private TextMeshPro textTemplate;

    [Header("Bay lên")]
    [SerializeField] private float floatHeightMin = 90f;
    [SerializeField] private float floatHeightMax = 170f;
    [SerializeField] private float spreadXMin     = -100f;
    [SerializeField] private float spreadXMax     =  100f;
    [SerializeField] private float durationMin    = 0.7f;
    [SerializeField] private float durationMax    = 1.1f;
    [SerializeField] private float spawnDelay     = 0.07f;

    [Header("Text Style")]
    [SerializeField] private float fontSize  = 15f;
    [SerializeField] private float worldScale = 80f;
    [SerializeField] private Color textColor = new Color(0.2f, 0.95f, 0.3f, 1f);

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int    sortingOrder     = 8100;

    private void Awake()
    {
        if (textTemplate == null)
            textTemplate = GetComponentInChildren<TextMeshPro>(true);
    }

    public void Play(int amount, Vector3 worldPosition, int count = 4)
    {

        if (textTemplate == null)
        {
            Debug.LogWarning("[VFX_DEBUG] HarvestAmountTextVFX: textTemplate NULL — gán TextMeshPro child vào slot TextTemplate trong prefab.");
            Destroy(gameObject, 1f);
            return;
        }

        textTemplate.gameObject.SetActive(false);
        transform.position = worldPosition;

        StartCoroutine(SpawnSequence(amount, worldPosition, count));
    }

    private IEnumerator SpawnSequence(int amount, Vector3 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(SpawnOne(amount, origin));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    private IEnumerator SpawnOne(int amount, Vector3 origin)
    {
        GameObject go = new GameObject("HarvestAmountText");
        go.transform.SetParent(transform);
        go.SetActive(true);
        go.transform.localScale = GetLocalScaleForWorldSize(worldScale);

        float startX = origin.x + Random.Range(spreadXMin, spreadXMax);
        go.transform.position = new Vector3(startX, origin.y, origin.z - 0.1f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text               = $"+{amount}";
        tmp.fontSize           = fontSize;
        tmp.color              = textColor;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.sortingLayerID     = SortingLayer.NameToID(sortingLayerName);
        tmp.sortingOrder       = sortingOrder;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        Vector3 startPos = go.transform.position;
        float   riseY    = Random.Range(floatHeightMin, floatHeightMax);
        Vector3 endPos   = startPos + new Vector3(Random.Range(-0.2f, 0.2f), riseY, 0f);

        float duration = Random.Range(durationMin, durationMax);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            float t    = elapsed / duration;
            float ease = 1f - (1f - t) * (1f - t);

            go.transform.position = Vector3.Lerp(startPos, endPos, ease);

            float scaleF = t < 0.15f
                ? Mathf.Lerp(0.5f, 1.2f, t / 0.15f)
                : Mathf.Lerp(1.2f, 1.0f, (t - 0.15f) / 0.85f);
            go.transform.localScale = GetLocalScaleForWorldSize(worldScale * scaleF);

            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
            tmp.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (go != null) Destroy(go);

        if (transform.childCount <= 1)
            Destroy(gameObject);
    }

    private Vector3 GetLocalScaleForWorldSize(float targetWorldScale)
    {
        Vector3 parentScale = transform.lossyScale;
        return new Vector3(
            targetWorldScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            targetWorldScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            targetWorldScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z))
        );
    }
}
