using UnityEngine;

public enum RewardType { Coin, Diamond }

[CreateAssetMenu(fileName = "MissionData", menuName = "Game/Mission Data")]
public class MissionData : ScriptableObject
{
    public Sprite missionIcon;
    public string missionName;
    public int targetAmount;
    public Sprite rewardIcon;
    public int rewardAmount;
    public RewardType rewardType;
}
