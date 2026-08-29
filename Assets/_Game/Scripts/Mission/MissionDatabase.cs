using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionDatabase", menuName = "Game/Mission Database")]
public class MissionDatabase : ScriptableObject
{
    [Header("Danh sách nhiệm vụ (kéo vào hoặc dùng dữ liệu mẫu bên dưới)")]
    public List<MissionData> missions = new List<MissionData>();

#if UNITY_EDITOR
    [ContextMenu("Tạo 15 nhiệm vụ mẫu (chỉ dùng khi test)")]
    public void GenerateSampleMissions()
    {
        var samples = new List<(string name, int target, int reward, RewardType type)>
        {
            ("Thu hoạch 10 lúa",         10,  50,  RewardType.Coin),
            ("Thu hoạch 5 ngô",           5,  40,  RewardType.Coin),
            ("Thu hoạch 8 cà rốt",        8,  60,  RewardType.Coin),
            ("Thu hoạch 3 dưa hấu",       3,  80,  RewardType.Coin),
            ("Thu hoạch 6 cà chua",       6,  55,  RewardType.Coin),
            ("Chăn nuôi 5 con gà",        5,  70,  RewardType.Coin),
            ("Chăn nuôi 3 con heo",       3, 100,  RewardType.Coin),
            ("Chăn nuôi 2 con bò",        2, 120,  RewardType.Coin),
            ("Thu thập 20 trứng gà",     20,  90,  RewardType.Coin),
            ("Thu thập 10 lít sữa",      10, 110,  RewardType.Coin),
            ("Mua 1 cái cuốc mới",        1,   5,  RewardType.Diamond),
            ("Nâng cấp kho lên cấp 2",    1,  10,  RewardType.Diamond),
            ("Trồng cây 15 lần",         15,  75,  RewardType.Coin),
            ("Tưới nước 20 ô đất",       20,  65,  RewardType.Coin),
            ("Bán hàng 5 lần ở chợ",      5,   8,  RewardType.Diamond),
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
