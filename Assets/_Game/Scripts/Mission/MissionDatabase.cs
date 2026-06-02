using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionDatabase", menuName = "Game/Mission Database")]
public class MissionDatabase : ScriptableObject
{
    [Header("Danh sÃ¡ch nhiá»‡m vá»¥ (kÃ©o vÃ o hoáº·c dÃ¹ng dá»¯ liá»‡u máº«u bÃªn dÆ°á»›i)")]
    public List<MissionData> missions = new List<MissionData>();

#if UNITY_EDITOR
    [ContextMenu("Táº¡o 15 nhiá»‡m vá»¥ máº«u (chá»‰ dÃ¹ng khi test)")]
    public void GenerateSampleMissions()
    {
        var samples = new List<(string name, int target, int reward, RewardType type)>
        {
            ("Thu hoáº¡ch 10 lÃºa",         10,  50,  RewardType.Coin),
            ("Thu hoáº¡ch 5 ngÃ´",           5,  40,  RewardType.Coin),
            ("Thu hoáº¡ch 8 cÃ  rá»‘t",        8,  60,  RewardType.Coin),
            ("Thu hoáº¡ch 3 dÆ°a háº¥u",       3,  80,  RewardType.Coin),
            ("Thu hoáº¡ch 6 cÃ  chua",       6,  55,  RewardType.Coin),
            ("ChÄƒn nuÃ´i 5 con gÃ ",        5,  70,  RewardType.Coin),
            ("ChÄƒn nuÃ´i 3 con heo",       3, 100,  RewardType.Coin),
            ("ChÄƒn nuÃ´i 2 con bÃ²",        2, 120,  RewardType.Coin),
            ("Thu tháº­p 20 trá»©ng gÃ ",     20,  90,  RewardType.Coin),
            ("Thu tháº­p 10 lÃ­t sá»¯a",      10, 110,  RewardType.Coin),
            ("Mua 1 cÃ¡i cuá»‘c má»›i",        1,   5,  RewardType.Diamond),
            ("NÃ¢ng cáº¥p kho lÃªn cáº¥p 2",    1,  10,  RewardType.Diamond),
            ("Trá»“ng cÃ¢y 15 láº§n",         15,  75,  RewardType.Coin),
            ("TÆ°á»›i nÆ°á»›c 20 Ã´ Ä‘áº¥t",       20,  65,  RewardType.Coin),
            ("BÃ¡n hÃ ng 5 láº§n á»Ÿ chá»£",      5,   8,  RewardType.Diamond),
        };

        missions.Clear();
        foreach (var s in samples)
        {
            var data = CreateInstance<MissionData>();
            data.name        = s.name;
            data.missionName = s.name;
            data.targetAmount  = s.target;
            data.rewardAmount  = s.reward;
            data.rewardType    = s.type;
            missions.Add(data);
        }

    }
#endif
}
