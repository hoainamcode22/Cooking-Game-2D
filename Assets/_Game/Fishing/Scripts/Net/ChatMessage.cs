using System;

namespace FarmGame.Fishing
{
    /// <summary>1 tin chat trong phòng — map sang rooms/{roomId}/chat/{pushId}. unix = giây Unix (long).</summary>
    [Serializable]
    public class ChatMessage
    {
        public string senderId;
        public string senderName;
        public string text;
        public long unix;
    }
}
