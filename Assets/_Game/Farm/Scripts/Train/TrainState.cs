/// <summary>
/// Trạng thái hệ thống 2 tàu.
///
/// TrainVisualRoot  = Reward Train  = tàu CŨ  = xuất hiện từ hầm, đem reward về ga
/// TrainVisualRoot2 = Shipping Train = tàu MỚI = đứng ga nhận hàng, chạy vào hầm
///
/// Flow vòng lặp:
///   WaitingForLoad → ShipDeparting → Processing → RewardArriving
///   → RewardReadyToCollect → RewardDeparting → WaitingForLoad
///
/// Kỹ thuật ẩn/teleport thay cho quay đầu:
///   ShipDeparting  : tàu MỚI  ga → hầm → (teleport về pointHiddenShip) → ẩn
///   RewardDeparting: tàu CŨ   ga → pointHiddenReward → (teleport về pointTunnelReward) → ẩn
///   Chuyến mới     : tàu MỚI  snap thẳng tại pointStationShip → hiện → WaitingForLoad
/// </summary>
public enum TrainState
{
    WaitingForLoad,          // TrainVisualRoot2 đứng ga, chờ user nạp hàng
    ShipDeparting,           // TrainVisualRoot2: ga → hầm → teleport về hidden → ẩn
    Processing,              // 2 tàu ẩn, timer đang đếm ngược
    RewardArriving,          // TrainVisualRoot: hầm → ga
    RewardReadyToCollect,    // TrainVisualRoot ở ga, chờ user thu reward
    RewardDeparting,         // TrainVisualRoot: ga → hidden → teleport về hầm → ẩn
}
