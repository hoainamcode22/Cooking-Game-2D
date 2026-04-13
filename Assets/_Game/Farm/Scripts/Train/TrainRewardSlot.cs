using UnityEngine;

/// <summary>
/// Runtime data của 1 slot reward sau khi chuyến tàu hoàn thành.
/// </summary>
[System.Serializable]
public class TrainRewardSlot
{
    public string rewardItemId;
    public string rewardDisplayName;
    public Sprite rewardIcon;
    public int rewardAmount;
    public bool isCollected;
}
