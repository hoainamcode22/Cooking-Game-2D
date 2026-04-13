/// <summary>
/// Enum trạng thái của chuyến tàu.
/// </summary>
public enum TrainState
{
    WaitingForLoad,       // Tàu ở Point_01, chờ người chơi nạp hàng vào các toa
    Departing,            // Tàu đang chạy Point_01 → Point_02 → Point_00
    ReturningWithReward,  // Tàu đang quay về Point_00 → Point_01 (mang phần thưởng)
    RewardReadyToCollect  // Tàu về Point_01, chờ người chơi click từng toa để thu reward
}
