using System;

namespace FarmGame.Fishing
{
    /// <summary>
    /// Trạng thái 1 người chơi trong phòng — map thẳng sang Firebase RTDB rooms/{roomId}/players/{playerId}.
    /// Chỉ kiểu nguyên thuỷ (JsonUtility + Firebase đều đọc được). Vị trí world unit; dir = (int)FacingDir; phase = (int)FishingPhase.
    /// bubbleUntilUnix là LONG giây Unix (không bao giờ dùng float cho Unix).
    /// </summary>
    [Serializable]
    public class PlayerNetState
    {
        public string playerId;
        public string displayName;
        public string characterId;   // PlayerF / PlayerM
        public int avatarIndex;      // avatar hồ sơ farm (AvatarProfilePopupUI) để hiện góc trái
        public int level;
        public float x;
        public float y;
        public int dir;
        public bool moving;
        public int phase;
        public string bubbleText;
        public long bubbleUntilUnix;
        public bool privateMode;     // bật riêng tư ⇒ người lạ không thấy bubble
        public bool isBot;           // offline giả lập
        public long lastSeenUnix;

        public PlayerNetState Clone() { return (PlayerNetState)MemberwiseClone(); }

        public void CopyFrom(PlayerNetState o)
        {
            playerId = o.playerId; displayName = o.displayName; characterId = o.characterId; avatarIndex = o.avatarIndex; level = o.level;
            x = o.x; y = o.y; dir = o.dir; moving = o.moving; phase = o.phase; bubbleText = o.bubbleText; bubbleUntilUnix = o.bubbleUntilUnix;
            privateMode = o.privateMode; isBot = o.isBot; lastSeenUnix = o.lastSeenUnix;
        }
    }
}
