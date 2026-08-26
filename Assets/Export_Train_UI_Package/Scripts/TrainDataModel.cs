using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExportTrainUIPackage
{
    public enum TrainState
    {
        WaitingForLoad = 1,       // State 1: Tàu đậu ở ga, người chơi nạp hàng vào 4 toa
        ShipDeparting = 2,        // State 2: Tàu xuất bến chạy ra khỏi ga
        Processing = 3,           // State 3: Tàu đang vận chuyển, đếm ngược thời gian
        RewardArriving = 4,       // State 4: Tàu về ga
        RewardReadyToCollect = 5, // State 5/6: Tàu đậu ở ga chờ thu nhận thưởng 4 toa
        RewardDeparting = 6       // State 6: Tàu rời ga sau khi nhận thưởng
    }

    [System.Serializable]
    public class CargoRequirement
    {
        public string itemId;
        public string itemName;
        public string iconPath;
        public int currentAmount;
        public int targetAmount;
        public bool isComplete => currentAmount >= targetAmount;

        public CargoRequirement(string id, string name, string icon, int current, int target)
        {
            itemId = id;
            itemName = name;
            iconPath = icon;
            currentAmount = current;
            targetAmount = target;
        }
    }

    [System.Serializable]
    public class RewardItem
    {
        public string rewardId;
        public string rewardName;
        public string iconPath;
        public int amount;
        public bool isCollected;

        public RewardItem(string id, string name, string icon, int count)
        {
            rewardId = id;
            rewardName = name;
            iconPath = icon;
            amount = count;
            isCollected = false;
        }
    }

    public static class TrainItemDatabase
    {
        // 4 Toa hàng hóa giao đi — ITEM ID KHỚP ĐÚNG VỚI GAME (xem TrainCargoData.asset)
        public static readonly List<CargoRequirement> SampleCrops = new List<CargoRequirement>()
        {
            new CargoRequirement("rice",     "Lúa",       "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/iconlua.png", 0, 4),
            new CargoRequirement("ngo",      "Ngô",       "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/bapcai.png",  0, 6),
            new CargoRequirement("egg",      "Trứng",     "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/conga.png",   0, 5),
            new CargoRequirement("beef",     "Thịt bò",   "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/thitheo.png", 0, 5),
        };

        // 4 Toa phần thưởng nhận về (Vật liệu nâng cấp kho/xây dựng & Tiền tệ đưa thẳng vào Kho và Ví)
        public static readonly List<RewardItem> SampleRewards = new List<RewardItem>()
        {
            new RewardItem("da", "Đá xây dựng", "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/UI board Small  stone.png", 12),
            new RewardItem("go", "Gỗ tấm", "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/UI board Small  parchment.png", 15),
            new RewardItem("kinh", "Kính", "Assets/Assetsgame/Fantasy Wooden GUI  Free/PNG/UI board Small  stone.png", 8),
            new RewardItem("gold", "Tiền vàng", "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/Icon_vang.png", 450),
            new RewardItem("gem", "Kim cương", "Assets/thietke/Redesign popup nhiệm vụ game/UnifiedTaskPopup_Redesign/assets/kimcuong.png", 8),
        };
    }
}
