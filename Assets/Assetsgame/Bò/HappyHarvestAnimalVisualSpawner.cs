using UnityEngine;
using UnityEngine.Rendering;

namespace Assetsgame.Animals
{
    public class HappyHarvestAnimalVisualSpawner : MonoBehaviour
    {
        [Header("Animal Setup")]
        [SerializeField] private GameObject animalPrefab;
        [SerializeField] private string legacyChildName;
        [SerializeField] private string spawnedChildName = "HappyHarvestAnimal";
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private int sortingOrderOffset = 50;
        [SerializeField, Min(1)] private int animalCount = 2;
        [SerializeField] private float horizontalSpacing = 1.3f;

        [Header("Pen Movement Bounds")]
        [SerializeField] private Vector2 walkBoundsMin = new Vector2(-1.15f, -0.6f);
        [SerializeField] private Vector2 walkBoundsMax = new Vector2(1.15f, 0.45f);

        [Header("Audio Clips (Tiếng kêu đói)")]
        [SerializeField] private AudioClip[] soundClips;

        private void Awake()
        {
            DisableLegacyVisual();
            SpawnVisuals();
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

        private void SpawnVisuals()
        {
            if (animalPrefab == null)
                return;

            int count = Mathf.Max(1, animalCount);
            for (int i = 0; i < count; i++)
            {
                string childName = count == 1 ? spawnedChildName : $"{spawnedChildName}_{i + 1}";
                Transform existing = transform.Find(childName);
                if (existing != null)
                {
                    ConfigureAnimal(existing.gameObject, i, count);
                    continue;
                }

                GameObject visual = Instantiate(animalPrefab, transform);
                visual.name = childName;

                float centeredIndex = i - (count - 1) * 0.5f;
                visual.transform.localPosition = localPosition + Vector3.right * (centeredIndex * horizontalSpacing);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = localScale;

                ConfigureAnimal(visual, i, count);
            }
        }

        private void ConfigureAnimal(GameObject visual, int index, int totalCount)
        {
            // 1. SortingGroup trên root để gom toàn bộ chi thành 1 khối
            SortingGroup sg = visual.GetComponent<SortingGroup>();
            if (sg == null) sg = visual.AddComponent<SortingGroup>();
            sg.sortingLayerName = "CongTrinh";
            sg.sortingOrder = 600 + sortingOrderOffset + index * 5;

            // 2. Gắn và cấu hình LivestockAI
            LivestockAI ai = visual.GetComponent<LivestockAI>();
            if (ai == null) ai = visual.AddComponent<LivestockAI>();

            ai.localBoundsMin = walkBoundsMin;
            ai.localBoundsMax = walkBoundsMax;
            ai.sortingLayerName = "CongTrinh";
            ai.baseSortingOrder = 600 + sortingOrderOffset + index * 5;

            if (soundClips != null && soundClips.Length > 0)
            {
                ai.soundClips = soundClips;
            }
        }
    }
}
