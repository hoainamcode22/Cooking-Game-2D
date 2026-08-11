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
    /// <summary>
    /// F5 — PHẢI đủ 4 slot, bằng số toa tàu.
    ///
    /// VÌ SAO sửa từ 3 lên 4: `TrainManager.ApplyRewardsToSlots()` đặt toa nào không có
    /// `_pendingRewards[i]` về `TrainWagonSlotMode.Empty`. Tàu có 4 toa mà preset chỉ khai
    /// 3 slot ⇒ **toa số 4 LUÔN trống**, người chơi nạp hàng cả 4 toa mà chỉ nhận thưởng 3.
    /// Mặc định 3 chính là cái bẫy: designer thêm preset mới là lại thiếu một toa.
    /// </summary>
    [Tooltip("Đúng 4 slot reward — bằng số toa tàu (xem TrainManager.rewardWagonSlots).")]
    public TrainRewardItem[] slots = new TrainRewardItem[4];
}

[Serializable]
public class TrainRewardItem
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    [Min(1)] public int rewardAmount = 1;
}
