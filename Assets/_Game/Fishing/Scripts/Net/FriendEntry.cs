using System;

namespace FarmGame.Fishing
{
    /// <summary>1 dòng trong danh sách bạn — map sang users/{uid}/friends/{friendId}.</summary>
    [Serializable]
    public class FriendEntry
    {
        public string playerId;
        public string displayName;
        public int avatarIndex;
        public int level;
        public bool online;
        /// <summary>Phòng đang ở (rỗng = không ở hồ câu). Có giá trị ⇒ nút "Mời"/"Tới" bật.</summary>
        public string roomId;
        public RelationshipKind relation = RelationshipKind.Friend;
        public bool privateMode;
    }
}
