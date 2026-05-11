using UnityEngine;

namespace Village
{
    /// <summary>
    /// ScriptableObject asset for each item type that can appear in house orders.
    /// Create via: Right-click → Village → Order Item Definition
    ///
    /// CRITICAL — itemId must match the key used by FarmInventoryManager EXACTLY:
    ///   • Same case  ("Rice" ≠ "rice")
    ///   • No leading/trailing spaces
    ///   • No invisible characters
    ///
    /// To find what keys FarmInventoryManager uses, enter Play mode and look for the
    /// "[FarmInventory] Item: XXXX" lines logged by VillageOrderManager at startup.
    /// Your itemId here must match those XXXX values character-for-character.
    /// </summary>
    [CreateAssetMenu(fileName = "NewOrderItem", menuName = "Village/Order Item Definition")]
    public class OrderItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must match the key used by FarmInventoryManager.AddItem() exactly (case-sensitive)")]
        public string itemId;
        public string displayName;
        public Sprite icon;

        [Header("Category")]
        public OrderCategory category;

        [Header("Unlock")]
        [Tooltip("Minimum player level required for this item to appear in orders")]
        public int unlockLevel = 1;

        [Header("Generation")]
        [Range(1, 100)]
        [Tooltip("Higher weight = more likely to be selected")]
        public int weight    = 10;
        public int minAmount = 1;
        public int maxAmount = 5;

        [Header("Rewards per Unit")]
        public int goldPerUnit = 10;
        public int expPerUnit  = 5;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-trim itemId in the Editor so accidental spaces never silently break delivery.
            if (itemId != null && itemId != itemId.Trim())
            {
                itemId = itemId.Trim();
                Debug.LogWarning($"[OrderItemDefinition] '{name}': itemId had leading/trailing whitespace — auto-trimmed to '{itemId}'.");
            }

            if (string.IsNullOrEmpty(itemId))
                Debug.LogWarning($"[OrderItemDefinition] '{name}': itemId is empty. Set it to the key used by FarmInventoryManager.");
        }
#endif
    }
}
