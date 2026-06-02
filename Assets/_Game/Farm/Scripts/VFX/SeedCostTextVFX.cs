using System.Collections;
using TMPro;
using UnityEngine;

public class SeedCostTextVFX : MonoBehaviour
{
    [Header("Template")]
    [Tooltip("Child TextTemplate (TextMeshPro). Náº¿u null sáº½ tá»± tÃ¬m.")]
    [SerializeField] private TextMeshPro textTemplate;

    [Header("Bay lÃªn")]
    [SerializeField] private float floatHeightMin = 0.6f;
    [SerializeField] private float floatHeightMax = 1.2f;
    [SerializeField] private float spreadXMin     = -0.6f;
    [SerializeField] private float spreadXMax     =  0.6f;
    [SerializeField] private float durationMin    = 0.6f;
    [SerializeField] private float durationMax    = 0.9f;
    [SerializeField] private float spawnDelay     = 0.06f;

    [Header("Text Style")]
    [SerializeField] private float fontSize   = 5f;
    [SerializeField] private Color textColor  = new Color(1f, 0.35f, 0.15f, 1f);

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int    sortingOrder     = 300;

    private void Awake()
    {
        if (textTemplate == null)
            textTemplate = GetComponentInChildren<TextMeshPro>(true);
    }

    public void Play(int amount, Vector3 worldPosition, int count = 5)
    {

        if (textTemplate == null)
        {
            Debug.LogWarning("[VFX_DEBUG] SeedCostTextVFX: textTemplate NULL â€” gÃ¡n TextMeshPro child vÃ o slot TextTemplate trong prefab.");
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
        GameObject go = new GameObject("SeedCostText");
        go.transform.SetParent(transform);
        go.SetActive(true);
        go.transform.localScale = Vector3.one;

        float startX = origin.x + Random.Range(spreadXMin, spreadXMax);
        go.transform.position = new Vector3(startX, origin.y + 0.45f, origin.z - 0.1f);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text               = $"-{amount}";
        tmp.fontSize           = fontSize;
        tmp.color              = textColor;
        tmp.alignment          = TextAlignmentOptions.Center;
        tmp.sortingLayerID     = SortingLayer.NameToID(sortingLayerName);
        tmp.sortingOrder       = sortingOrder;
        tmp.enableWordWrapping = false;

        Vector3 startPos = go.transform.position;
        float   riseY    = Random.Range(floatHeightMin, floatHeightMax);
        Vector3 endPos   = startPos + new Vector3(Random.Range(-0.15f, 0.15f), riseY, 0f);

        float duration = Random.Range(durationMin, durationMax);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            float t    = elapsed / duration;
            float ease = 1f - (1f - t) * (1f - t);

            go.transform.position = Vector3.Lerp(startPos, endPos, ease);

            float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
            tmp.color = new Color(textColor.r, textColor.g, textColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (go != null) Destroy(go);

        if (transform.childCount <= 1)
            Destroy(gameObject);
    }
}
