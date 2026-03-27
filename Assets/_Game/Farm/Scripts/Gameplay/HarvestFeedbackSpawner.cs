using TMPro;
using UnityEngine;

public class HarvestFeedbackSpawner : MonoBehaviour
{
    public static HarvestFeedbackSpawner Instance { get; private set; }

    [SerializeField] private FloatingHarvestText prefab;

    private void Awake()
    {
        Instance = this;
    }

    public void Spawn(Vector3 worldPosition, string content)
    {
        if (prefab == null)
            return;

        FloatingHarvestText item = Instantiate(prefab, worldPosition, Quaternion.identity);
        item.Setup(content);
    }
}