using System;
using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Dịch vụ phòng. Vòng 1: LocalRoomService (offline, bot). Vòng online: FirebaseRoomService (RTDB rooms/{roomId}).
    /// UI/Player CHỈ nói chuyện qua interface này (lấy từ FishingNetHub.Room). Mọi callback chạy trên main thread.
    /// </summary>
    public interface IRoomService
    {
        bool IsConnected { get; }
        string LocalPlayerId { get; }
        /// <summary>null khi chưa vào phòng.</summary>
        string CurrentRoomId { get; }

        event Action OnRoomListChanged;
        event Action<PlayerNetState> OnPlayerJoined;
        event Action<PlayerNetState> OnPlayerUpdated;
        event Action<string> OnPlayerLeft;

        /// <summary>Lấy danh sách phòng (async). Offline trả ngay trong cùng frame.</summary>
        void RequestRoomList(Action<List<RoomInfo>> onResult);

        /// <summary>Vào phòng với trạng thái ban đầu của mình. onResult(ok, errorVi).</summary>
        void JoinRoom(string roomId, PlayerNetState self, Action<bool, string> onResult);

        void LeaveRoom();

        /// <summary>Đẩy trạng thái của mình. Caller tự giới hạn tần suất theo FishingConfig.netPushRate.</summary>
        void PushLocalState(PlayerNetState self);

        /// <summary>Snapshot người chơi khác trong phòng (KHÔNG gồm mình). Không giữ tham chiếu lâu.</summary>
        IReadOnlyList<PlayerNetState> GetRemotePlayers();

        /// <summary>Gọi mỗi frame từ 1 MonoBehaviour (RemotePlayersManager) để dịch vụ offline mô phỏng bot.</summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Scene báo vùng đi lại + các điểm câu (world) để dịch vụ OFFLINE cho bot đi/câu đúng chỗ.
        /// Bản online bỏ qua (no-op). RemotePlayersManager gọi 1 lần sau khi scene dựng xong.
        /// </summary>
        void ConfigureLocalSimulation(Rect walkArea, IReadOnlyList<Vector2> fishingSpots);
    }
}
