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
        [SerializeField] private Vector3 localPosition = new Vector3(0f, 1.85f, 0f);
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private int sortingOrderOffset = 50;
        [SerializeField, Min(1)] private int animalCount = 2;
        [SerializeField] private float horizontalSpacing = 1.3f;

        [Header("Pen Movement Bounds")]
        [SerializeField] private Vector2 walkBoundsMin = new Vector2(-1.25f, 1.25f);
        [SerializeField] private Vector2 walkBoundsMax = new Vector2(1.25f, 2.50f);

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
            // [FIX] KHONG con hardcode "CongTrinh" (layer khong ton tai) o day nua - LivestockAI.Awake/
            // UpdateDynamicSorting da tu giai layer THAT (TouristSortingLayers.Visitor) va ap dung moi
            // frame cho chinh SortingGroup nay roi, ghi de "CongTrinh" tai day chi lam vo tac dung fix.
            SortingGroup sg = visual.GetComponent<SortingGroup>();
            if (sg == null) sg = visual.AddComponent<SortingGroup>();
            // [FIX DEV D 2026-09-06] Kep san FenceSortingOrderFloor NGAY tai day. Truoc day dong
            // nay gan order tho: neu ai do go sortingOrderOffset AM tren Inspector (vi du -200 =>
            // order 400) thi con vat chim duoi rao (order 500) trong dung frame dau, phai cho
            // LivestockAI.Update kip kep lai o frame sau => nhap nhay 1 frame. Kep luon cho dut diem.
            int orderGoc = Mathf.Max(600 + sortingOrderOffset + index * 5,
                                     LivestockAI.FenceSortingOrderFloor);
            sg.sortingOrder = orderGoc;

            // 2. Gắn và cấu hình LivestockAI
            LivestockAI ai = visual.GetComponent<LivestockAI>();
            if (ai == null) ai = visual.AddComponent<LivestockAI>();

            ai.localBoundsMin = walkBoundsMin;
            ai.localBoundsMax = walkBoundsMax;
            // [FIX] Bo gan cung "CongTrinh" - de LivestockAI.sortingLayerName mac dinh rong ("")
            // tu giai qua TouristSortingLayers.Visitor (xem LivestockAI.cs).
            ai.baseSortingOrder = orderGoc;

            if (soundClips != null && soundClips.Length > 0)
            {
                ai.soundClips = soundClips;
            }
        }
    }
}
