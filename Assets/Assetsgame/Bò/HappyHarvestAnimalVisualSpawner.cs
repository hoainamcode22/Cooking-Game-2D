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

        private const string AnimalSortingLayer = "CongTrinh";
        private const int AnimalBaseSortingOrder = 500;

        private void ApplySorting(GameObject visual)
        {
            SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return;

            // Tìm order nhỏ nhất trong animal để tính offset tương đối
            int minOrder = int.MaxValue;
            foreach (SpriteRenderer sr in renderers)
                if (sr.sortingOrder < minOrder) minOrder = sr.sortingOrder;

            // Base = CongTrinh/510, các bộ phận giữ nguyên khoảng cách nhau
            // Ví dụ: thân=0 → 510, đầu=2 → 512, mắt=4 → 514
            int baseOrder = AnimalBaseSortingOrder + sortingOrderOffset;

            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingLayerName = AnimalSortingLayer;
                renderer.sortingOrder = baseOrder + (renderer.sortingOrder - minOrder);
            }
        }
    }
}
