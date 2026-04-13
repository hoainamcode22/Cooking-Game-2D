using UnityEngine;

/// <summary>
/// Runtime data của 1 toa hàng yêu cầu trong chuyến hiện tại.
/// </summary>
[System.Serializable]
public class TrainCargoSlot
{
    public string requestItemId;
    public string requestDisplayName;
    public Sprite requestIcon;
    public int requiredAmount;
    public int loadedAmount;

    public bool IsFullyLoaded => loadedAmount >= requiredAmount;
}
