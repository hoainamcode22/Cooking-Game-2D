using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "TrainRewardData", menuName = "Train/Reward Data")]
public class TrainRewardData : ScriptableObject
{
    [Tooltip("Mỗi phần tử là reward cho 1 chuyến về. Index khớp với CargoData.presets.")]
    public List<TrainRewardPreset> presets = new List<TrainRewardPreset>();
}

[Serializable]
public class TrainRewardPreset
{
    [Tooltip("Đúng 3 slot reward.")]
    public TrainRewardItem[] slots = new TrainRewardItem[3];
}

[Serializable]
public class TrainRewardItem
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    [Min(1)] public int rewardAmount = 1;
}
