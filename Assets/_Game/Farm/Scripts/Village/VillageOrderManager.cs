using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Village
{

    public class VillageOrderManager : MonoBehaviour
    {
        public static VillageOrderManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Houses — drag all HouseOrderController GameObjects here")]
        [SerializeField] private List<HouseOrderController> houses = new List<HouseOrderController>();

        [Header("Item Pool — drag all OrderItemDefinition assets here")]
        [SerializeField] private List<OrderItemDefinition> availableItems = new List<OrderItemDefinition>();

        [Header("Order Config")]
        [SerializeField] private float cooldownDuration       = 60f;
        [SerializeField] private float replenishCheckInterval = 5f;

        [Tooltip("Chance (0–1) that an order contains 2 items instead of 1")]
        [SerializeField] [Range(0f, 1f)] private float twoItemChance = 0.5f;

        // ── Mock Fallback ─────────────────────────────────────────────────────

        [Header("Mock Fallback")]
        [Tooltip("Force mock even when real managers are present (useful for isolated testing)")]
        [SerializeField] private bool forceMock       = false;
        [SerializeField] private int  mockPlayerLevel = 5;

        [Tooltip("Keys must match your OrderItemDefinition.itemId values (auto-lowercased)")]
        [SerializeField] private List<MockInventoryEntry> mockInventoryList = new List<MockInventoryEntry>
        {
            new MockInventoryEntry { itemId = "rice",    amount = 30 },
            new MockInventoryEntry { itemId = "corn",    amount = 20 },
            new MockInventoryEntry { itemId = "tomato",  amount = 15 },
            new MockInventoryEntry { itemId = "cabbage", amount = 10 },
            new MockInventoryEntry { itemId = "egg",     amount = 18 },
            new MockInventoryEntry { itemId = "milk",    amount = 12 },
            new MockInventoryEntry { itemId = "pepper",  amount =  8 },
            new MockInventoryEntry { itemId = "sugar",   amount =  6 },
        };

        // ── Debug ─────────────────────────────────────────────────────────────

        [Header("Debug")]
        [Tooltip("Logs every inventory key queried + result. Leave ON until delivery works correctly.")]
        [SerializeField] private bool verboseInventoryLog = true;

        // ── Runtime ───────────────────────────────────────────────────────────

        private readonly Dictionary<string, int> mockInventory = new Dictionary<string, int>();

        private int  mockGold;
        private int  mockExp;

        private bool hasWarnedNoInventory;
        private bool hasWarnedNoEconomy;
        private bool hasWarnedNoProgress;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildMockInventory();
        }

        private void Start()
        {
            if (FarmInventoryManager.Instance != null)
            {
                Debug.Log("[VillageOrderManager] ↓ Real inventory keys at startup ↓");
                FarmInventoryManager.Instance.DebugPrintInventory();
            }
            else
            {
                Debug.Log("[VillageOrderManager] FarmInventoryManager not found — using mock inventory. " +
                          "Mock keys: " + string.Join(", ", mockInventory.Keys.Select(k => $"'{k}'")));
            }

            InitializeHouses();
            ReplenishOrders();
            StartCoroutine(ReplenishRoutine());
        }

        private void BuildMockInventory()
        {
            mockInventory.Clear();
            foreach (var entry in mockInventoryList)
            {
                if (string.IsNullOrWhiteSpace(entry.itemId)) continue;
                string key = NormalizeKey(entry.itemId);
                if (mockInventory.ContainsKey(key))
                    mockInventory[key] += entry.amount;
                else
                    mockInventory[key]  = entry.amount;
            }
        }

        // ── Order Management ──────────────────────────────────────────────────

        private IEnumerator ReplenishRoutine()
        {
            var wait = new WaitForSeconds(replenishCheckInterval);
            while (true) { yield return wait; ReplenishOrders(); }
        }

        public void ReplenishOrders()
        {
            // Mọi nhà Idle đều nhận order — user cần thấy yêu cầu của tất cả nhà
            // để quyết định đi farm nguyên liệu nào rồi giao.
            var eligible = houses
                .Where(h => h != null && h.CurrentState == OrderState.Idle)
                .ToList();

            if (eligible.Count == 0) return;

            int assigned = 0;
            foreach (var house in eligible)
            {
                var order = GenerateOrder(house.HouseId);
                if (order == null) continue;

                house.AssignOrder(order);
                assigned++;
            }

            if (assigned > 0)
                Debug.Log($"[VillageOrderManager] +{assigned} orders assigned. Active: {CountActiveOrders()}/{houses.Count}");
        }

        private HouseOrderRuntime GenerateOrder(int houseId)
        {
            int playerLevel = GetPlayerLevel();
            var pool = availableItems
                .Where(i => i != null && i.unlockLevel <= playerLevel)
                .ToList();

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[VillageOrderManager] No items unlocked at player level {playerLevel}.");
                return null;
            }

            var def1    = WeightedRandom(pool);
            int amount1 = UnityEngine.Random.Range(def1.minAmount, def1.maxAmount + 1);

            var item1 = new OrderItemLine
            {
                itemId         = NormalizeKey(def1.itemId),
                displayName    = def1.displayName,
                icon           = def1.icon,
                requiredAmount = amount1
            };

            int totalGold = amount1 * def1.goldPerUnit;
            int totalExp  = amount1 * def1.expPerUnit;

            OrderItemLine item2  = null;
            bool          tryTwo = pool.Count > 1 && UnityEngine.Random.value < twoItemChance;

            if (tryTwo)
            {
                var pool2 = pool.Where(i => NormalizeKey(i.itemId) != item1.itemId).ToList();
                if (pool2.Count > 0)
                {
                    var def2    = WeightedRandom(pool2);
                    int amount2 = UnityEngine.Random.Range(def2.minAmount, def2.maxAmount + 1);

                    item2 = new OrderItemLine
                    {
                        itemId         = NormalizeKey(def2.itemId),
                        displayName    = def2.displayName,
                        icon           = def2.icon,
                        requiredAmount = amount2
                    };

                    totalGold += amount2 * def2.goldPerUnit;
                    totalExp  += amount2 * def2.expPerUnit;
                }
            }

            string genItems = item2 != null
                ? $"'{item1.itemId}' x{amount1} + '{item2.itemId}' x{item2.requiredAmount}"
                : $"'{item1.itemId}' x{amount1}";
            Debug.Log($"[VillageOrderManager] Generated order for house {houseId}: {genItems}");

            return new HouseOrderRuntime
            {
                houseId     = houseId,
                item1       = item1,
                item2       = item2,
                rewardGold  = totalGold,
                rewardExp   = totalExp,
                state       = OrderState.Active,
                createdTime = Time.time
            };
        }

        public void DeliverOrder(HouseOrderController house)
        {
            Debug.Log($"[VillageOrderManager] DeliverOrder() — house={(house != null ? house.gameObject.name : "NULL")}");

            if (house == null)
            {
                Debug.LogError("[VillageOrderManager] DeliverOrder: house is null.");
                return;
            }

            var order = house.CurrentOrder;
            if (order == null)
            {
                Debug.LogError("[VillageOrderManager] DeliverOrder: house.CurrentOrder is null — " +
                               $"house '{house.gameObject.name}' state={house.CurrentState}");
                return;
            }

            LogDeliveryAttempt(order);

            if (!HasEnoughForOrder(order))
            {
                Debug.LogWarning("[VillageOrderManager] Delivery BLOCKED — not enough items. " +
                                 "See [InventoryAdapter] lines above for exact owned/needed values.");
                return;
            }

            // ── Remove items ──────────────────────────────────────────────────
            Debug.Log($"[VillageOrderManager] Removing item1: '{order.item1.itemId}' x{order.item1.requiredAmount}");
            bool ok1 = RemoveItem(order.item1.itemId, order.item1.requiredAmount);
            if (!ok1)
            {
                Debug.LogError($"[VillageOrderManager] RemoveItem FAILED for '{order.item1.itemId}' " +
                               $"x{order.item1.requiredAmount} — delivery aborted.");
                return;
            }

            if (order.HasSecondItem)
            {
                Debug.Log($"[VillageOrderManager] Removing item2: '{order.item2.itemId}' x{order.item2.requiredAmount}");
                bool ok2 = RemoveItem(order.item2.itemId, order.item2.requiredAmount);
                if (!ok2)
                    Debug.LogError($"[VillageOrderManager] RemoveItem FAILED for '{order.item2.itemId}' " +
                                   $"x{order.item2.requiredAmount}. Item1 already removed — inventory inconsistent.");
            }

            // ── Add rewards ───────────────────────────────────────────────────
            Debug.Log($"[Deliver] +Gold: {order.rewardGold}  +EXP: {order.rewardExp}");
            AddGold(order.rewardGold);
            AddExp(order.rewardExp);

            // ── Log success ───────────────────────────────────────────────────
            string items = order.HasSecondItem
                ? $"{order.item1.requiredAmount}x '{order.item1.itemId}' + {order.item2.requiredAmount}x '{order.item2.itemId}'"
                : $"{order.item1.requiredAmount}x '{order.item1.itemId}'";
            Debug.Log($"[VillageOrderManager] ✓ DELIVERY SUCCESS [{items}] → +{order.rewardGold}g +{order.rewardExp}xp  house='{house.gameObject.name}'");

            // ── Post-delivery: start cooldown, then auto-assign new order ─────
            house.StartCooldown(cooldownDuration, completedHouse =>
            {
                if (completedHouse != null)
                {
                    Debug.Log($"[VillageOrderManager] Cooldown ended for '{completedHouse.gameObject.name}' — replenishing.");
                    ReplenishOrders();
                }
            });

            ReplenishOrders();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public void RegisterHouse(HouseOrderController house)
        {
            if (house == null || houses.Contains(house)) return;
            house.Initialize(houses.Count);
            houses.Add(house);
            Debug.Log($"[VillageOrderManager] Registered new house: '{house.gameObject.name}' id={house.HouseId}");

            // Gán order trực tiếp cho nhà mới — ReplenishOrders có thể skip do needed==0
            // khi số order hiện tại đã đạt maxActiveOrders.
            var order = GenerateOrder(house.HouseId);
            if (order != null)
                house.AssignOrder(order);

            // Cân bằng lại các nhà cũ sau 1 giây (frame đầu tiên đã ổn định)
            Invoke(nameof(ReplenishOrders), 1f);
        }

        public bool IsRegistered(HouseOrderController house) =>
            house != null && houses.Contains(house);

        public void UnregisterHouse(HouseOrderController house)
        {
            if (house == null) return;
            house.ClearOrder();
            houses.Remove(house);
            Debug.Log($"[VillageOrderManager] Unregistered placeholder: '{house.gameObject.name}'");
        }

        private void InitializeHouses()
        {
            for (int i = 0; i < houses.Count; i++)
            {
                if (houses[i] != null)
                    houses[i].Initialize(i);
            }
            Debug.Log($"[VillageOrderManager] {houses.Count} houses initialized.");
        }

        private int CountActiveOrders() =>
            houses.Count(h => h != null && h.CurrentState == OrderState.Active);

        private OrderItemDefinition WeightedRandom(List<OrderItemDefinition> pool)
        {
            int total = pool.Sum(i => i.weight);
            int roll  = UnityEngine.Random.Range(0, total);
            int acc   = 0;
            foreach (var item in pool)
            {
                acc += item.weight;
                if (roll < acc) return item;
            }
            return pool[pool.Count - 1];
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Key Normalization ─────────────────────────────────────────────────

        /// <summary>Normalize an inventory key: trim whitespace + lowercase.</summary>
        private static string NormalizeKey(string key) =>
            key?.Trim().ToLower() ?? string.Empty;

        // ═════════════════════════════════════════════════════════════════════
        // INVENTORY ADAPTER LAYER
        // ═════════════════════════════════════════════════════════════════════
        public bool HasEnoughForOrder(HouseOrderRuntime order)
        {
            if (order?.item1 == null) return false;

            int owned1 = GetPlayerItemAmount(order.item1.itemId);
            bool ok1   = owned1 >= order.item1.requiredAmount;

            Debug.Log($"[VillageOrder] HasEnough item1='{order.item1.itemId}'  owned={owned1}  need={order.item1.requiredAmount}  ok={ok1}");
            if (!ok1) return false;

            if (order.HasSecondItem)
            {
                int owned2 = GetPlayerItemAmount(order.item2.itemId);
                bool ok2   = owned2 >= order.item2.requiredAmount;

                Debug.Log($"[VillageOrder] HasEnough item2='{order.item2.itemId}'  owned={owned2}  need={order.item2.requiredAmount}  ok={ok2}");
                if (!ok2) return false;
            }

            return true;
        }

        public bool HasEnough(string itemId, int amount) =>
            GetPlayerItemAmount(itemId) >= amount;

        public int GetPlayerItemAmount(string itemId)
        {
            // BUG-FIX (key mismatch): normalize before every lookup.
            string normalizedKey = NormalizeKey(itemId);

            if (!forceMock && FarmInventoryManager.Instance != null)
            {
                int result = FarmInventoryManager.Instance.GetAmount(normalizedKey);

                // Always log when verboseInventoryLog is on — this is the primary debug signal.
                if (verboseInventoryLog)
                    Debug.Log($"[InventoryAdapter] Looking for key: '{normalizedKey}', found: {result}");

                // When the result is 0, dump all real keys — makes key-mismatch trivial to spot.
                if (result == 0 && verboseInventoryLog)
                {
                    Debug.LogWarning($"[InventoryAdapter] Key '{normalizedKey}' returned 0. " +
                                     "Current inventory keys:");
                    FarmInventoryManager.Instance.DebugPrintInventory();
                }

                return result;
            }

            // ── Mock fallback ─────────────────────────────────────────────────
            if (!forceMock && !hasWarnedNoInventory)
            {
                hasWarnedNoInventory = true;
                Debug.LogWarning("[InventoryAdapter] FarmInventoryManager.Instance is null — using mock inventory. " +
                                 "Set forceMock=true if this is intentional.");
            }

            mockInventory.TryGetValue(normalizedKey, out int v);

            if (!mockInventory.ContainsKey(normalizedKey))
            {
                Debug.LogWarning($"[InventoryAdapter] MOCK: key '{normalizedKey}' not found. " +
                                 "Available mock keys: " +
                                 string.Join(", ", mockInventory.Keys.Select(k => $"'{k}'")));
            }
            else if (verboseInventoryLog)
            {
                Debug.Log($"[InventoryAdapter] Looking for key: '{normalizedKey}', found: {v} (MOCK)");
            }

            return v;
        }

        // Returns true on success. Caller must check and handle false.
        private bool RemoveItem(string itemId, int amount)
        {
            // BUG-FIX (key mismatch): normalize before every lookup.
            string normalizedKey = NormalizeKey(itemId);

            if (!forceMock && FarmInventoryManager.Instance != null)
            {
                bool success = FarmInventoryManager.Instance.RemoveItem(normalizedKey, amount);
                if (success)
                    Debug.Log($"[InventoryAdapter] RemoveItem('{normalizedKey}', {amount}) OK");
                else
                    Debug.LogError($"[InventoryAdapter] RemoveItem('{normalizedKey}', {amount}) FAILED — " +
                                   "key missing or insufficient quantity in FarmInventoryManager.");
                return success;
            }

            // MOCK ↓
            if (!mockInventory.ContainsKey(normalizedKey))
            {
                Debug.LogWarning($"[MOCK] RemoveItem: key '{normalizedKey}' not in mock inventory.");
                return false;
            }

            int before = mockInventory[normalizedKey];
            if (before < amount)
            {
                Debug.LogWarning($"[MOCK] RemoveItem: '{normalizedKey}' has {before}, need {amount}.");
                return false;
            }

            mockInventory[normalizedKey] = before - amount;
            Debug.Log($"[MOCK] RemoveItem '{normalizedKey}': {before} → {mockInventory[normalizedKey]}");
            return true;
        }

        private void AddGold(int amount)
        {
            if (!forceMock && FarmEconomyManager.Instance != null)
            {
                Debug.Log($"[VillageOrderManager] AddGold via FarmEconomyManager: +{amount}");
                FarmEconomyManager.Instance.AddGold(amount);
                return;
            }

            // ── Fallback ──────────────────────────────────────────────────────
            if (forceMock)
            {
                mockGold += amount;
                Debug.Log($"[MOCK] Gold +{amount} → {mockGold}  (forceMock=true)");
            }
            else
            {
                // FarmEconomyManager not in scene — gold is LOST unless you add the manager.
                if (!hasWarnedNoEconomy)
                {
                    hasWarnedNoEconomy = true;
                    Debug.LogError("[VillageOrderManager] AddGold FAILED — FarmEconomyManager.Instance is null! " +
                                   "Gold will NOT be added. Add FarmEconomyManager to the scene.");
                }
                mockGold += amount;
                Debug.LogWarning($"[VillageOrderManager] Gold +{amount} went to MOCK (not visible to player). " +
                                 $"Mock total: {mockGold}");
            }
        }

        private void AddExp(int amount)
        {
            if (!forceMock && PlayerProgressManager.Instance != null)
            {
                Debug.Log($"[VillageOrderManager] AddExp via PlayerProgressManager: +{amount}");
                PlayerProgressManager.Instance.AddExp(amount);
                return;
            }

            // ── Fallback ──────────────────────────────────────────────────────
            if (forceMock)
            {
                mockExp += amount;
                Debug.Log($"[MOCK] EXP +{amount} → {mockExp}  (forceMock=true)");
            }
            else
            {
                // PlayerProgressManager not in scene — EXP is LOST unless you add the manager.
                if (!hasWarnedNoProgress)
                {
                    hasWarnedNoProgress = true;
                    Debug.LogError("[VillageOrderManager] AddExp FAILED — PlayerProgressManager.Instance is null! " +
                                   "EXP will NOT be added. Add PlayerProgressManager to the scene.");
                }
                mockExp += amount;
                Debug.LogWarning($"[VillageOrderManager] EXP +{amount} went to MOCK (not visible to player). " +
                                 $"Mock total: {mockExp}");
            }
        }

        private int GetPlayerLevel()
        {
            if (!forceMock && PlayerProgressManager.Instance != null)
                return PlayerProgressManager.Instance.Level;
            return mockPlayerLevel;
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        private void LogDeliveryAttempt(HouseOrderRuntime order)
        {
            int o1 = GetPlayerItemAmount(order.item1.itemId);
            string msg = $"[VillageOrder] DELIVERY ATTEMPT  " +
                         $"item1='{order.item1.itemId}' owned={o1} need={order.item1.requiredAmount}";

            if (order.HasSecondItem)
            {
                int o2 = GetPlayerItemAmount(order.item2.itemId);
                msg += $"  |  item2='{order.item2.itemId}' owned={o2} need={order.item2.requiredAmount}";
            }

            msg += $"  |  forceMock={forceMock}" +
                   $"  |  FarmInventory={(FarmInventoryManager.Instance != null ? "FOUND" : "NULL")}";

            Debug.Log(msg);
        }

        [ContextMenu("Debug: Print Inventory Now")]
        private void DebugPrintInventoryNow()
        {
            if (FarmInventoryManager.Instance != null)
                FarmInventoryManager.Instance.DebugPrintInventory();
            else
                Debug.Log("[VillageOrderManager] FarmInventoryManager not found. Mock: " +
                          string.Join(", ", mockInventory.Select(kv => $"'{kv.Key}'={kv.Value}")));
        }
    }

    // ── Helper types ──────────────────────────────────────────────────────────

    [Serializable]
    public class MockInventoryEntry
    {
        public string itemId;
        public int    amount;
    }
}
