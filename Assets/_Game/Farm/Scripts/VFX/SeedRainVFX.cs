using System.Collections;
using UnityEngine;

public class SeedRainVFX : MonoBehaviour
{
    [Header("Template")]
    [Tooltip("Child IconTemplate (SpriteRenderer). Nếu null sẽ tự tìm.")]
    [SerializeField] private SpriteRenderer iconTemplate;

    [Header("Rơi")]
    [SerializeField] private float spawnHeightMin  = 1.5f;
    [SerializeField] private float spawnHeightMax  = 2.8f;
    [SerializeField] private float spreadXMin      = -0.8f;
    [SerializeField] private float spreadXMax      =  0.8f;
    [SerializeField] private float durationMin     = 0.5f;
    [SerializeField] private float durationMax     = 0.9f;

    [Header("Scale")]
    [SerializeField] private float iconScale = 0.35f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int    sortingOrder     = 300;

    private void Awake()
    {
        if (iconTemplate == null)
            iconTemplate = GetComponentInChildren<SpriteRenderer>(true);
    }

    public void Play(Sprite seedIcon, Vector3 worldPosition, int count = 8)
    {
        Debug.Log($"[VFX_DEBUG] SeedRainVFX Play ENTER | icon={(seedIcon != null ? seedIcon.name : "NULL")} | pos={worldPosition} | count={count} | iconTemplate={iconTemplate != null}");

        if (iconTemplate == null)
        {
            Debug.LogWarning("[VFX_DEBUG] SeedRainVFX: iconTemplate NULL — gán SpriteRenderer child vào slot IconTemplate trong prefab.");
            Destroy(gameObject, 1f);
            return;
        }

        iconTemplate.gameObject.SetActive(false);
        transform.position = worldPosition;

        for (int i = 0; i < count; i++)
            StartCoroutine(SpawnOne(seedIcon, worldPosition));
    }

    private IEnumerator SpawnOne(Sprite icon, Vector3 origin)
    {
        // Clone iconTemplate — SetActive(true) bắt buộc để icon hiện
        GameObject go = Instantiate(iconTemplate.gameObject, transform);
        go.SetActive(true);

        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = icon;
        sr.enabled          = true;
        sr.color            = Color.white;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder     = sortingOrder;

        go.transform.localScale = Vector3.one * iconScale;

        // Vị trí xuất phát: cao hơn origin, lệch ngang ngẫu nhiên
        float startX = origin.x + Random.Range(spreadXMin, spreadXMax);
        float startY = origin.y + Random.Range(spawnHeightMin, spawnHeightMax);
        go.transform.position = new Vector3(startX, startY, origin.z - 0.1f);

        // Đích: gần origin
        float endX = origin.x + Random.Range(-0.3f, 0.3f);
        float endY = origin.y + Random.Range(-0.1f, 0.2f);
        Vector3 startPos = go.transform.position;
        Vector3 endPos   = new Vector3(endX, endY, origin.z - 0.1f);

        float duration = Random.Range(durationMin, durationMax);
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            if (go == null) yield break;

            float t      = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);

            go.transform.position = Vector3.Lerp(startPos, endPos, smooth);

            // Scale pulse
            float scalePulse = iconScale * (1f + 0.25f * Mathf.Sin(Mathf.PI * t));
            go.transform.localScale = Vector3.one * scalePulse;

            // Fade out nửa sau
            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            sr.color = new Color(1f, 1f, 1f, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (go != null) Destroy(go);

        // Tự hủy khi hết icon (chỉ còn template inactive)
        if (transform.childCount <= 1)
            Destroy(gameObject);
    }
}
