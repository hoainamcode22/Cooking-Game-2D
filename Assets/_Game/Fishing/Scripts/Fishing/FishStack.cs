using System;

namespace FarmGame.Fishing
{
    /// <summary>Một loại cá trong giỏ + số con. fishId đã chuẩn hoá (trim + lower). JsonUtility serialize được.</summary>
    [Serializable]
    public class FishStack
    {
        public string fishId;
        public int amount;
    }
}
