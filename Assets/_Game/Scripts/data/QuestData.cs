using UnityEngine;
using System.Collections.Generic;

public enum QuestKind
{
    Main,
    Daily
}

[System.Serializable]
public struct QuestCondition
{
    public MissionEventType eventType;
    public string targetItemId;
    public int targetAmount;
}

[CreateAssetMenu(fileName = "QuestData", menuName = "Game/Data/Quest Data")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string questName;
    public QuestKind kind;
    public List<QuestCondition> conditions = new List<QuestCondition>();
    public int requiredLevel;
    
    [Header("Rewards")]
    public int rewardGold;
    public int rewardGems;
    public int rewardExp;
}
