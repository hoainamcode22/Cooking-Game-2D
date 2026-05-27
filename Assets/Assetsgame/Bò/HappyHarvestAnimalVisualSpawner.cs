using UnityEngine;

namespace Assetsgame.Animals
{
    public class HappyHarvestAnimalVisualSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject animalPrefab;
        [SerializeField] private string legacyChildName;
        [SerializeField] private string spawnedChildName = "HappyHarvestAnimal";
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private int sortingOrderOffset = 10;

        private void Awake()
        {
            DisableLegacyVisual();
            SpawnVisual();
        }

        private void DisableLegacyVisual()
        {
            if (string.IsNullOrEmpty(legacyChildName))
                return;

            Transform legacy = transform.Find(legacyChildName);
            if (legacy == null)
                return;

            foreach (Renderer renderer in legacy.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private void SpawnVisual()
        {
            if (animalPrefab == null || transform.Find(spawnedChildName) != null)
                return;

            GameObject visual = Instantiate(animalPrefab, transform);
            visual.name = spawnedChildName;
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = localScale;

            ApplySorting(visual);
        }

        private void ApplySorting(GameObject visual)
        {
            SpriteRenderer buildingRenderer = GetComponent<SpriteRenderer>();
            if (buildingRenderer == null)
                buildingRenderer = GetComponentInChildren<SpriteRenderer>(true);

            string sortingLayerName = buildingRenderer != null ? buildingRenderer.sortingLayerName : "Default";
            int sortingOrder = buildingRenderer != null ? buildingRenderer.sortingOrder + sortingOrderOffset : sortingOrderOffset;

            foreach (SpriteRenderer renderer in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingLayerName = sortingLayerName;
                renderer.sortingOrder = sortingOrder;
            }
        }
    }
}
