using UnityEngine;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Điểm lấy dịch vụ mạng duy nhất. Mặc định OFFLINE (Local*). Vòng online gọi FishingNetHub.Use(firebaseRoom, firebaseChat, firebaseFriend)
    /// một lần lúc khởi động. CHỦ FILE: Lead. Dev D cài 3 class Local* với constructor KHÔNG tham số.
    /// </summary>
    public static class FishingNetHub
    {
        private static IRoomService _room;
        private static IChatService _chat;
        private static IFriendService _friends;

        public static IRoomService Room { get { EnsureOffline(); return _room; } }
        public static IChatService Chat { get { EnsureOffline(); return _chat; } }
        public static IFriendService Friends { get { EnsureOffline(); return _friends; } }
        public static bool IsOffline { get; private set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _room = null; _chat = null; _friends = null; IsOffline = true; }

        /// <summary>Cắm bộ dịch vụ khác (Firebase). Gọi trước khi bất kỳ UI nào chạm vào Hub.</summary>
        public static void Use(IRoomService room, IChatService chat, IFriendService friends, bool offline = false)
        {
            _room = room; _chat = chat; _friends = friends; IsOffline = offline;
            Debug.Log(FishingIds.LogTag + " NetHub dùng bộ dịch vụ: " + (offline ? "OFFLINE" : room.GetType().Name));
        }

        private static void EnsureOffline()
        {
            if (_room != null && _chat != null && _friends != null) { return; }
            var room = new LocalRoomService();
            var chat = new LocalChatService(room);
            var friends = new LocalFriendService(room);
            Use(room, chat, friends, offline: true);
        }
    }
}
