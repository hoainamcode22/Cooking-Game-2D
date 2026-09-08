using System;
using System.Collections.Generic;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Bạn bè · kết nối · mời · tặng quà. Vòng 1: LocalFriendService (lưu PlayerPrefs, bot chấp nhận ngay).
    /// Vòng online: Firestore users/{uid}/friends.
    /// </summary>
    public interface IFriendService
    {
        IReadOnlyList<FriendEntry> Friends { get; }
        event Action OnFriendsChanged;
        event Action<InviteInfo> OnInviteReceived;
        /// <summary>Nhận quà: (từ ai, loại, itemId (fish_… hoặc rỗng), số lượng).</summary>
        event Action<string, GiftKind, string, int> OnGiftReceived;

        bool IsFriend(string playerId);
        FriendEntry Find(string playerId);
        RelationshipKind GetRelationship(string playerId);

        /// <summary>Gửi lời mời kết bạn. Offline: chấp nhận ngay và thêm vào Friends.</summary>
        void SendFriendRequest(PlayerNetState target);
        void RemoveFriend(string playerId);
        /// <summary>Đặt kết nối (Hẹn hò/Bạn bè/Chị em). Chỉ khi đã là bạn.</summary>
        bool SetRelationship(string playerId, RelationshipKind kind);

        /// <summary>Mời bạn vào phòng mình đang ở.</summary>
        bool InviteToRoom(string playerId, string roomId);

        /// <summary>Tặng quà: trừ của mình (gem qua FarmEconomyManager, cá qua FishBasket) rồi gửi. Trả false + lý do tiếng Việt.</summary>
        bool SendGift(string playerId, GiftKind kind, string itemId, int amount, out string errorVi);

        /// <summary>Chế độ riêng tư của mình (người lạ không thấy bubble).</summary>
        bool PrivateMode { get; set; }
    }
}
