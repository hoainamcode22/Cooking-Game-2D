using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrainCargoData", menuName = "Train/Cargo Data")]
public class TrainCargoData : ScriptableObject
{
    [Tooltip("Mỗi phần tử là 1 chuyến với 3 toa yêu cầu hàng hóa khác nhau.")]
    public List<TrainCargoPreset> presets = new List<TrainCargoPreset>();
}

[Serializable]
public class TrainCargoPreset
{
    [Tooltip("Đúng 3 slot, mỗi slot là 1 toa.")]
    public TrainCargoRequirement[] slots = new TrainCargoRequirement[3];
}

[Serializable]
public class TrainCargoRequirement
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    [Min(1)] public int requiredAmount = 1;
}
