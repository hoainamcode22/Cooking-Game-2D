using UnityEngine;

namespace Village
{
    /// <summary>
    /// One required item line within an order (either slot 1 or slot 2).
    /// </summary>
    [System.Serializable]
    public class OrderItemLine
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int    requiredAmount;
    }

    /// <summary>
    /// Runtime snapshot of a single active house order.
    /// item1 is always present. item2 is null when the order has only one item.
    /// </summary>
    [System.Serializable]
    public class HouseOrderRuntime
    {
        public int           houseId;
        public OrderItemLine item1;          // always set
        public OrderItemLine item2;          // null → single-item order
        public int           rewardGold;     // combined total for both items
        public int           rewardExp;      // combined total for both items
        public OrderState    state;
        public float         createdTime;

        public bool HasSecondItem => item2 != null;
    }
}
