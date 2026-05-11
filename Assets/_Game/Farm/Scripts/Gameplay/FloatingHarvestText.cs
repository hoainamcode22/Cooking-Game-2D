using TMPro;
using UnityEngine;

public class FloatingHarvestText : MonoBehaviour
{
    [SerializeField] private TMP_Text txt;
    [SerializeField] private float lifetime   = 3.0f;   // DEBUG: tune to ~1.2s after confirmed visible
    [SerializeField] private float moveHeight = 2.0f;   // world units to float upward
    [SerializeField] private float holdRatio  = 0.4f;
    [SerializeField] private float punchPeak  = 1.25f;
    [SerializeField] private float punchTime  = 0.1f;

    private Vector3 startPos;
    private float   timer;

    private void Awake()
    {
        // TextMeshPro (3D) stores its own _SortingLayerID and _SortingOrder fields
        // that override the MeshRenderer every frame. Must set via TMP's API, not MeshRenderer.
        if (txt is TextMeshPro tmp3d)
        {
            int fxID = SortingLayer.NameToID("FX");
            if (fxID != 0)
            {
                tmp3d.sortingLayerID = fxID;
            }
            else
            {
                Debug.LogWarning("[PlantText] 'FX' sorting layer not found in Tags & Layers — text will use Default layer. Add 'FX' in Project Settings → Tags & Layers → Sorting Layers.");
            }
            tmp3d.sortingOrder = 7000;

            var mr = GetComponent<MeshRenderer>();
            Debug.Log($"[PlantText] Awake | TMP sortingLayerID={tmp3d.sortingLayerID} order={tmp3d.sortingOrder}" +
                      $" | MeshRenderer layer='{(mr != null ? mr.sortingLayerName : "none")}' order={(mr != null ? mr.sortingOrder : -1)}");
        }
        else
        {
            Debug.LogWarning($"[PlantText] txt is not TextMeshPro (3D). Type={txt?.GetType().Name ?? "NULL"}. Sorting not applied.");
        }
    }

    public void Setup(string content)
    {
        if (txt == null)
        {
            Debug.LogError($"[PlantText] txt is NULL on '{gameObject.name}'. Wire the TextMeshPro component in the prefab.");
            return;
        }

        txt.text     = content;
        txt.color    = Color.yellow;
        txt.fontSize = 8f;

        // Black outline for readability
        var mat = txt.fontMaterial;
        if (mat != null)
        {
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        }

        transform.localScale = Vector3.one;
        startPos = transform.position;

        Debug.Log($"[PlantText] Setup | text='{content}' pos={transform.position}" +
                  $" scale={transform.localScale} fontSize={txt.fontSize}");
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifetime);

        transform.position = startPos + Vector3.up * (moveHeight * Mathf.SmoothStep(0f, 1f, t));

        // DEBUG: no fade — destroy when lifetime ends
        if (t >= 1f)
            Destroy(gameObject);
    }
}
