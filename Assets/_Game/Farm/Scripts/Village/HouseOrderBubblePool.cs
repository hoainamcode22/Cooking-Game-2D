using System.Collections.Generic;
using UnityEngine;

namespace Village
{
    /// <summary>
    /// Object pool cho HouseOrderBubble. Singleton, nằm trong Canvas_Popup.
    ///
    /// API:
    ///   HouseOrderBubble b = HouseOrderBubblePool.Instance.Get();
    ///   b.Show(houseTransform, icon1, icon2);
    ///   ...
    ///   HouseOrderBubblePool.Instance.Return(b);
    /// </summary>
    public class HouseOrderBubblePool : MonoBehaviour
    {
        public static HouseOrderBubblePool Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Cách A — kéo OrderPopup2 instances đã tạo sẵn vào đây (khuyên dùng)")]
        [SerializeField] private List<HouseOrderBubble> prewarmPool = new List<HouseOrderBubble>();

        [Header("Cách B — prefab để tự Instantiate khi cần")]
        [SerializeField] private HouseOrderBubble bubblePrefab;
        [SerializeField] private int              poolSize = 5;

        // ── Runtime ───────────────────────────────────────────────────────────

        private readonly Queue<HouseOrderBubble>   freePool = new Queue<HouseOrderBubble>();
        private readonly HashSet<HouseOrderBubble> inUse    = new HashSet<HouseOrderBubble>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Seed từ Inspector list
            foreach (var b in prewarmPool)
            {
                if (b == null) continue;
                b.Hide();
                freePool.Enqueue(b);
            }

            // Instantiate thêm nếu chưa đủ poolSize
            int toCreate = poolSize - freePool.Count;
            for (int i = 0; i < toCreate; i++)
            {
                if (bubblePrefab == null) break;
                var inst = Instantiate(bubblePrefab, transform);
                inst.name = $"OrderPopup2_pool_{freePool.Count + 1}";
                inst.Hide();
                freePool.Enqueue(inst);
            }

            Debug.Log($"[BubblePool] Initialized — {freePool.Count} bubble(s) ready  " +
                      $"(prewarm={prewarmPool.Count}  instantiated={Mathf.Max(0, toCreate)})");

            if (freePool.Count == 0)
                Debug.LogWarning("[BubblePool] Pool rỗng! Kéo OrderPopup2 vào 'Prewarm Pool' " +
                                 "hoặc gán 'Bubble Prefab' trong Inspector của BubblePool.");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Lấy một bubble rảnh từ pool.
        /// Nếu pool cạn và bubblePrefab được gán, tự động mở rộng và log cảnh báo.
        /// Trả về null nếu không còn bubble và không có prefab.
        /// </summary>
        public HouseOrderBubble Get()
        {
            HouseOrderBubble bubble;

            if (freePool.Count > 0)
            {
                bubble = freePool.Dequeue();
            }
            else if (bubblePrefab != null)
            {
                // Pool cạn → mở rộng động
                bubble      = Instantiate(bubblePrefab, transform);
                bubble.name = $"OrderPopup2_pool_extra_{inUse.Count + 1}";
                Debug.LogWarning($"[BubblePool] Pool cạn — instantiated extra bubble '{bubble.name}'. " +
                                 "Tăng poolSize hoặc thêm instance vào prewarmPool.");
            }
            else
            {
                Debug.LogError("[BubblePool] Get() FAILED: pool cạn và bubblePrefab là null. " +
                               "Kéo OrderPopup2 prefab vào field 'Bubble Prefab'.");
                return null;
            }

            inUse.Add(bubble);
            Debug.Log($"[BubblePool] Get() → '{bubble.name}'  free={freePool.Count}  inUse={inUse.Count}");
            return bubble;
        }

        /// <summary>
        /// Trả bubble về pool. Tự động gọi Hide() trước khi enqueue.
        /// </summary>
        public void Return(HouseOrderBubble bubble)
        {
            if (bubble == null) return;

            if (!inUse.Contains(bubble))
            {
                Debug.LogWarning($"[BubblePool] Return: '{bubble.gameObject.name}' không thuộc pool này — bỏ qua.");
                return;
            }

            bubble.Hide();
            inUse.Remove(bubble);
            freePool.Enqueue(bubble);

            Debug.Log($"[BubblePool] Return() ← '{bubble.gameObject.name}'  free={freePool.Count}  inUse={inUse.Count}");
        }

        // ── Debug ─────────────────────────────────────────────────────────────

        [ContextMenu("Debug: Print Pool Status")]
        private void DebugPrintStatus()
        {
            Debug.Log($"[BubblePool] free={freePool.Count}  inUse={inUse.Count}  total={freePool.Count + inUse.Count}");
        }
    }
}
