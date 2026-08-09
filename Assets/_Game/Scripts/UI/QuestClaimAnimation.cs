using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuestClaimAnimation : MonoBehaviour
{
    public static QuestClaimAnimation Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject goldParticlePrefab;
    [SerializeField] private GameObject gemParticlePrefab;
    [SerializeField] private GameObject floatingTextPrefab;
    
    [Header("Targets (e.g. Top Bar UI)")]
    [SerializeField] private Transform goldTarget;
    [SerializeField] private Transform gemTarget;

    [Header("Animation Settings")]
    [SerializeField] private float jumpPower = 50f;
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private int particlesPerReward = 5; // e.g. generate 5 coins to simulate claiming

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayClaimFX(Vector3 startPos, int goldAmount, int gemsAmount)
    {
        if (goldAmount > 0)
        {
            SpawnParticles(goldParticlePrefab, startPos, goldTarget, particlesPerReward);
            SpawnFloatingText(startPos, $"+{goldAmount} Gold", Color.yellow);
        }
        
        if (gemsAmount > 0)
        {
            StartCoroutine(DelayedGemSpawnRoutine(startPos, gemsAmount));
        }
    }

    private IEnumerator DelayedGemSpawnRoutine(Vector3 startPos, int gemsAmount)
    {
        yield return new WaitForSeconds(0.15f);
        SpawnParticles(gemParticlePrefab, startPos, gemTarget, particlesPerReward);
        SpawnFloatingText(startPos + Vector3.up * 30f, $"+{gemsAmount} Gems", Color.cyan);
    }

    private void SpawnParticles(GameObject prefab, Vector3 startPos, Transform target, int count)
    {
        if (prefab == null) return;
        
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.transform.position = startPos;
            
            // Scatter slightly
            Vector3 randomOffset = new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0);
            Vector3 jumpPos = startPos + randomOffset;
            
            StartCoroutine(ParticleAnimRoutine(obj, startPos, jumpPos, target));
        }
    }

    private IEnumerator ParticleAnimRoutine(GameObject obj, Vector3 startPos, Vector3 jumpPos, Transform target)
    {
        float jumpDur = 0.4f;
        float elapsed = 0f;
        while (elapsed < jumpDur)
        {
            if (obj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDur;
            float easeT = 1f - (1f - t) * (1f - t); // OutQuad
            Vector3 pos = Vector3.Lerp(startPos, jumpPos, easeT);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpPower;
            obj.transform.position = pos;
            yield return null;
        }

        if (target != null)
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            elapsed = 0f;
            Vector3 moveStart = obj.transform.position;
            Vector3 startScale = obj.transform.localScale;
            Vector3 endScale = startScale * 0.5f;
            while (elapsed < duration)
            {
                if (obj == null || target == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; // InOutCubic
                obj.transform.position = Vector3.Lerp(moveStart, target.position, easeT);
                obj.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            elapsed = 0f;
            Vector3 startScale = obj.transform.localScale;
            while (elapsed < 0.3f)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                obj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / 0.3f);
                yield return null;
            }
        }
        
        if (obj != null) Destroy(obj);
    }

    private void SpawnFloatingText(Vector3 pos, string text, Color color)
    {
        if (floatingTextPrefab == null) return;
        
        GameObject textObj = Instantiate(floatingTextPrefab, transform);
        textObj.transform.position = pos;
        
        if (textObj.TryGetComponent<TMPro.TextMeshProUGUI>(out var tmp))
        {
            tmp.text = text;
            tmp.color = color;
            StartCoroutine(FloatingTextRoutine(textObj, tmp, pos.y, pos.y + 100f));
        }
        else
        {
            Destroy(textObj);
        }
    }

    private IEnumerator FloatingTextRoutine(GameObject textObj, TMPro.TextMeshProUGUI tmp, float startY, float endY)
    {
        float dur = 1f;
        float elapsed = 0f;
        Color startColor = tmp.color;
        Color endColor = startColor;
        endColor.a = 0f;
        Vector3 startPos = textObj.transform.position;
        
        while (elapsed < dur)
        {
            if (textObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            
            float easeMoveT = 1f - Mathf.Pow(1f - t, 3f); // OutCubic
            Vector3 pos = startPos;
            pos.y = Mathf.Lerp(startY, endY, easeMoveT);
            textObj.transform.position = pos;
            
            float easeFadeT = t == 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f); // InExpo
            tmp.color = Color.Lerp(startColor, endColor, easeFadeT);
            
            yield return null;
        }
        
        if (textObj != null) Destroy(textObj);
    }
}
