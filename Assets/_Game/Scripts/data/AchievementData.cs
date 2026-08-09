using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct AchievementTier
{
    public int threshold;
    public int rewardGold;
    public int rewardGems;
    public int rewardExp;
}

[CreateAssetMenu(fileName = "AchievementData", menuName = "Game/Data/Achievement Data")]
public class AchievementData : ScriptableObject
{
    public string achievementId;
    public string achievementName; // Named achievementName to avoid hiding Object.name
    public List<AchievementTier> tiers = new List<AchievementTier>();
}
