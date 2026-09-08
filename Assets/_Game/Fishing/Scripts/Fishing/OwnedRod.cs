using System;

namespace FarmGame.Fishing
{
    /// <summary>Một cần đang sở hữu: rodItemId = RodData.itemID, durabilityLeft = số lần QUĂNG còn lại. JsonUtility serialize được.</summary>
    [Serializable]
    public class OwnedRod
    {
        public string rodItemId;
        public int durabilityLeft;
    }
}
